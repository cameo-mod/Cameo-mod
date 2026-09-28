#!/usr/bin/env python3
"""Run repeated headless AI-vs-AI matches and harvest match-log records.

This is the Stage D data tap of the AI architecture: the schema-2 records in
cameo-ai-matches.jsonl only become useful at volume, and volume needs a
harness — a script and a map rotation, not engine work.

Each matchup is a (faction x bot) vs (faction x bot) pairing on a generated
copy of mods/cameo/maps/ai_duel_gate_20260928. The variant's map.yaml gets
its two duelists patched (Bot/Faction) and their starting forces written into
the Actors: section — headless Launch.Map runs a Local server, so no lobby
bot seating exists and the duelists are map-side players (Playable: False +
Bot:), which SpawnStartingUnits cannot serve; the harness therefore resolves
each faction's StartingUnits group from the mod yaml and pre-places it.

The local client occupies the map's declared-NonCombatant "Referee" slot —
lobby clients ignore PlayerReference.NonCombatant, so the match writer checks
the declared flag and keeps it out of every bot's opponents/allies. The
referee's "empty" start class loses its conquest objective on the first tick,
so it never blocks game-over. Launch.Benchmark exits the process on GameOver;
ConquestVictoryConditions decides on elimination and the map's locked
TimeLimitManager is the stalemate failsafe (a timed-out duel records both
bots "lost" — an honest draw, not an engine-invented winner).

The fixture locks `gamespeed` to `insane` (10 ms timestep): bot tests run at
4x target rate so matches can be iterated in quick succession — a decisive
match resolves ~4x sooner in wall time. The minute-based cap scales to
time_limit*6000 ticks at that speed, so under CPU contention a timeout match
can legitimately run long; the stall detector (debug.log goes quiet after
its first write this run — arming skips the load phase) kills hung matches
in ~2 minutes while a generous wall bound protects slow-but-live ones. Run
bot batches serially and prefer an idle machine for consistent timings.

On a shared box a bigger hazard is external kills: another agent's
taskkill/Stop-Process sweep terminates every OpenRA.exe by name, which
surfaces as a bare "exit=N" mid-match — no exception log, no appended
records. Those matches get one automatic retry (--retries); real crashes
write exception-*.log and are never retried. Per-match results also append
to <support>/batch_results.jsonl as they land, so a batch whose driver is
itself swept still leaves usable evidence.

Usage:
    python tools/ai/run_ai_match_batch.py [options]

    --factions td_gdi,td_nod        factions for the matrix (default: td_gdi,td_nod)
    --bot-a hard --bot-b hard       difficulty types per side (default: hard)
    --repeats 2                     matches per matchup (spawn sides alternate)
    --time-limit 30                 per-match cap in minutes (engine options only)
    --support-dir PATH              batch support dir (default: %TEMP%/ai-match-batch-<ts>)
    --dry-run                       print the matrix + variants, launch nothing

Exit codes: 0 = batch complete, 2 = no usable records collected, 1 = failure.
"""

from __future__ import annotations

import argparse
import itertools
import json
import os
import pathlib
import re
import shutil
import subprocess
import sys
import tempfile
import threading
import time

REPO_ROOT = pathlib.Path(__file__).resolve().parents[2]

TEMPLATE_MAP = REPO_ROOT / "mods" / "cameo" / "maps" / "ai_duel_gate_20260928"
MATCH_LOG = "cameo-ai-matches.jsonl"
CONFIG_KEYS = {"MOD_ID", "ENGINE_DIRECTORY"}
BENCHMARK_PREFIX = "ai-duel-batch-"

# The mod.yaml MapFolders entry for user maps resolves through this literal
# directory name inside the support dir.
USER_MAP_DIR = os.path.join("maps", "cameo", "{DEV_VERSION}")

# Engine TimeLimitManager only accepts these minute values (mod.yaml
# TimeLimitOptions). --time-limit must be one of them.
VALID_TIME_LIMITS = {0, 10, 20, 30, 40, 60, 90}


def fail(message: str, record=None) -> None:
    print(f"ASSERTION FAILED: {message}")
    if record is not None:
        print(f"Offending record: {json.dumps(record, sort_keys=True)}")
    raise SystemExit(1)


def read_config(path: pathlib.Path, values: dict[str, str]) -> None:
    if not path.is_file():
        return

    assignment = re.compile(r"^\s*([A-Za-z_][A-Za-z0-9_]*)\s*=\s*(.*?)\s*$")
    for line in path.read_text(encoding="utf-8").splitlines():
        match = assignment.match(line)
        if not match or match.group(1) not in CONFIG_KEYS:
            continue

        value = match.group(2)
        if len(value) >= 2 and value[0] == value[-1] and value[0] in "\"'":
            value = value[1:-1]
        values[match.group(1)] = value


def load_config() -> tuple[str, pathlib.Path]:
    values: dict[str, str] = {}
    read_config(REPO_ROOT / "mod.config", values)
    read_config(REPO_ROOT / "user.config", values)

    mod_id = values.get("MOD_ID")
    engine_directory = values.get("ENGINE_DIRECTORY")
    if not mod_id:
        fail("mod.config/user.config did not define MOD_ID")
    if not engine_directory:
        fail("mod.config/user.config did not define ENGINE_DIRECTORY")

    engine = pathlib.Path(engine_directory)
    if not engine.is_absolute():
        engine = REPO_ROOT / engine
    return mod_id, engine.resolve()


def build_matchups(factions: list[str], bot_a: str, bot_b: str, repeats: int) -> list[dict]:
    """One unordered faction pair per matchup; repeats alternate spawn sides.

    Spawn side is a real axis (corner map geometry is asymmetric), so repeat
    parity swaps which faction occupies BotA/BotB. Mirrors (same faction both
    sides) are included: mirror records are honest 1v1 data.
    """
    matchups = []
    for index, (fa, fb) in enumerate(itertools.combinations_with_replacement(factions, 2)):
        for repeat in range(repeats):
            if repeat % 2 == 0:
                a, b = fa, fb
            else:
                a, b = fb, fa
            matchups.append(
                {
                    "index": index,
                    "repeat": repeat,
                    "side_a": {"faction": a, "bot": bot_a},
                    "side_b": {"faction": b, "bot": bot_b},
                    "variant": f"ai_duel_{a}_{bot_a}_vs_{b}_{bot_b}".replace(" ", "_"),
                }
            )
    return matchups


def patch_player_block(text: str, ref: str, bot: str, faction: str) -> str:
    """Rewrite the Bot/Faction lines inside one PlayerReference block and
    return (text, home_location).

    The template is ours and keeps sentinel values (Bot: hard,
    Faction: td_gdi/td_nod), so a block-scoped first-match replace is exact —
    and every patch asserts the expected line existed exactly once.
    """
    marker = f"\tPlayerReference@{ref}:"
    start = text.find(marker)
    if start < 0:
        fail(f"template map is missing {marker}")
    end = text.find("\n\tPlayerReference@", start + len(marker))
    block_end = len(text) if end < 0 else end
    block = text[start:block_end]

    home_match = re.search(r"^\t\tHomeLocation: (\d+),(\d+)$", block, re.MULTILINE)
    if not home_match:
        fail(f"{marker} block is missing HomeLocation", block)
    home = (int(home_match.group(1)), int(home_match.group(2)))

    for key, value in (("Bot", bot), ("Faction", faction)):
        pattern = re.compile(rf"^\t\t{key}: .+$", re.MULTILINE)
        found = pattern.findall(block)
        if len(found) != 1:
            fail(f"{marker} block does not contain exactly one '{key}:' line", block)
        block = pattern.sub(f"\t\t{key}: {value}", block, count=1)

    return text[:start] + block + text[block_end:], home


# The StartingUnitsInfo TraitInfos live on World across rules/*.yaml and the
# ContentPacks — the same source the engine resolves, scanned read-only.
UNIT_GROUP_SEARCH = (
    REPO_ROOT / "mods" / "cameo" / "rules",
    REPO_ROOT / "mods" / "cameo" / "ContentPacks",
)
UNIT_GROUP_RE = re.compile(r"^\s*StartingUnits@\w+:\n((?:[ \t]+[^\n]*\n)*)", re.MULTILINE)

# Support actors ring the base actor, mirroring SpawnStartingUnits' annulus
# on a controlled flat fixture. Minimum distance 4 keeps the MCV deploy
# footprint clear — a tighter ring once left a bot unable to deploy.
SUPPORT_RING = [(4, 0), (0, 4), (-4, 0), (0, -4), (4, 3), (-4, 3), (4, -3), (-4, -3),
                (3, 4), (-3, 4), (3, -4), (-3, -4)]

_group_cache: dict[tuple[str, str], tuple[str, list[str]] | None] = {}


def starting_unit_group(faction: str, cls: str) -> tuple[str, list[str]] | None:
    """Resolve (base_actor, support_actors) for a faction+class from the mod's
    StartingUnits blocks. Multiple groups per class are engine-randomized; the
    harness deterministically picks the first in scan order (fixtures do not
    need coverage of every variant)."""
    key = (faction, cls)
    if key in _group_cache:
        return _group_cache[key]

    files = sorted(UNIT_GROUP_SEARCH[0].glob("*.yaml"))
    files += sorted(UNIT_GROUP_SEARCH[1].rglob("*.yaml"))
    found = None
    for path in files:
        for match in UNIT_GROUP_RE.finditer(path.read_text(encoding="utf-8", errors="replace")):
            block = match.group(1)
            cls_match = re.search(r"^\s*Class:\s*(\S+)\s*$", block, re.MULTILINE)
            fac_match = re.search(r"^\s*Factions:\s*(.+)$", block, re.MULTILINE)
            if not cls_match or not fac_match or cls_match.group(1) != cls:
                continue
            if faction not in (f.strip() for f in fac_match.group(1).split(",")):
                continue
            base = re.search(r"^\s*BaseActor:\s*(\S+)\s*$", block, re.MULTILINE)
            supports = re.search(r"^\s*SupportActors:\s*(.+)$", block, re.MULTILINE)
            found = (
                base.group(1) if base else None,
                [s.strip() for s in supports.group(1).split(",") if s.strip()] if supports else [],
            )
            break
        if found is not None:
            break

    _group_cache[key] = found
    return found


ACTOR_SENTINEL = "\t# AI_DUEL_BOT_UNITS"


def render_side_actors(ref: str, faction: str, home: tuple[int, int]) -> str:
    """Map Actors nodes owning the faction's starting force to the map-side
    bot. Prefer 'light' (MCV + escorts — a real skirmish start); fall back to
    'none' (MCV only); fail loudly when a faction has neither."""
    group = starting_unit_group(faction, "light") or starting_unit_group(faction, "none")
    if group is None or group[0] is None:
        fail(f"faction {faction} has no StartingUnits group for class light or none")

    base, supports = group
    lines = [
        f"\t{ref}_base: {base}",
        "\t\tOwner: " + ref,
        f"\t\tLocation: {home[0]},{home[1]}",
    ]
    for index, actor in enumerate(supports[: len(SUPPORT_RING)]):
        dx, dy = SUPPORT_RING[index]
        lines += [
            f"\t{ref}_u{index}: {actor}",
            "\t\tOwner: " + ref,
            f"\t\tLocation: {home[0] + dx},{home[1] + dy}",
        ]
    return "\n".join(lines) + "\n"


def inject_starting_actors(text: str, sides: list[tuple[str, str, tuple[int, int]]]) -> str:
    """Replace the AI_DUEL_BOT_UNITS sentinel with generated Actors nodes."""
    if text.count(ACTOR_SENTINEL) != 1:
        fail("template map.yaml must contain exactly one AI_DUEL_BOT_UNITS sentinel")
    rendered = "".join(render_side_actors(ref, faction, home) for ref, faction, home in sides)
    return text.replace(ACTOR_SENTINEL, rendered.rstrip("\n"))


def write_variant(template: Path, dest: pathlib.Path, matchup: dict, time_limit: int) -> None:
    if dest.exists():
        shutil.rmtree(dest)
    shutil.copytree(template, dest)

    map_yaml = dest / "map.yaml"
    text = map_yaml.read_text(encoding="utf-8")
    text, home_a = patch_player_block(text, "BotA", matchup["side_a"]["bot"], matchup["side_a"]["faction"])
    text, home_b = patch_player_block(text, "BotB", matchup["side_b"]["bot"], matchup["side_b"]["faction"])
    text = inject_starting_actors(
        text,
        [
            ("BotA", matchup["side_a"]["faction"], home_a),
            ("BotB", matchup["side_b"]["faction"], home_b),
        ],
    )
    # Map.ComputeUID hashes file bytes: two variants with identical patched
    # content share one uid, so MapCache merges them into a single preview and
    # Launch.Map's name lookup can only find whichever dir enumerated last.
    # A per-destination comment salt keeps every variant's uid unique.
    text += f"\n# ai-match-batch variant: {dest.name}\n"
    map_yaml.write_text(text, encoding="utf-8")

    rules_yaml = dest / "rules.yaml"
    rules = rules_yaml.read_text(encoding="utf-8")
    patched, count = re.subn(r"TimeLimitDefault: \d+", f"TimeLimitDefault: {time_limit}", rules)
    if count != 1:
        fail("variant rules.yaml is missing exactly one TimeLimitDefault line")
    rules_yaml.write_text(patched, encoding="utf-8")


def output_tail(output: str) -> str:
    lines = output.splitlines()
    return "\n".join(lines[-40:]) if len(lines) > 40 else output


def run_match(
    executable: pathlib.Path,
    engine: pathlib.Path,
    args: list[str],
    timeout: int,
    stall_log: pathlib.Path | None = None,
    stall_timeout: int = 120,
) -> tuple[int, str, str]:
    """Returns (exit_code, output, status).

    status is one of:
      "ok"       — the process exited 0 (match resolved, records written)
      "timeout"  — the wall bound was hit and we terminated it
      "stalled"  — stall_log went quiet for stall_timeout seconds; the world
                   is hung, not slow, so we terminated it
      "exit=N"   — the process exited nonzero on its own. The engine's own
                   exits are 0 (Success) and -1 (fatal error + exception
                   log); a bare nonzero exit — especially code 1, the
                   TerminateProcess signature — means an external kill
                   (e.g. another agent's taskkill/Stop-Process sweep
                   targeting every OpenRA.exe by name).

    A fixture match that is alive writes to debug.log continuously (AI module
    timing lines every ~300 ticks), so a quiet stall_log means a hang. The
    detector arms only after the first log write newer than spawn: mod
    loading can legitimately take a minute under CPU contention, and the log
    file is shared across matches in a batch — the previous match's mtime
    must not arm the next match's window.
    """
    process = subprocess.Popen(
        [str(executable), *args],
        cwd=engine,
        stdout=subprocess.PIPE,
        stderr=subprocess.STDOUT,
        text=True,
        encoding="utf-8",
        errors="replace",
    )

    output_parts = []

    def drain():
        for line in process.stdout:
            output_parts.append(line)

    pump = threading.Thread(target=drain, daemon=True)
    pump.start()

    timed_out = False
    stalled = False
    deadline = time.monotonic() + timeout
    spawn_time = time.time()
    last_write = spawn_time
    log_seen = False
    while process.poll() is None:
        time.sleep(5)
        if stall_log is not None:
            try:
                mtime = stall_log.stat().st_mtime
            except OSError:
                mtime = 0
            if mtime > spawn_time:
                log_seen = True
                last_write = max(last_write, mtime)
            if log_seen and time.time() - last_write > stall_timeout:
                stalled = True
                break
        if time.monotonic() > deadline:
            timed_out = True
            break

    if stalled or timed_out:
        process.terminate()
        try:
            process.wait(timeout=15)
        except subprocess.TimeoutExpired:
            process.kill()
            process.wait()

    pump.join(timeout=10)
    if timed_out:
        status = "timeout"
    elif stalled:
        status = "stalled"
    elif process.returncode == 0:
        status = "ok"
    else:
        status = f"exit={process.returncode}"
    return process.returncode, "".join(output_parts), status


def read_appended_records(log_path: pathlib.Path, before_length: int) -> list[dict]:
    if not log_path.is_file() or log_path.stat().st_size <= before_length:
        return []
    appended = log_path.read_bytes()[before_length:].decode("utf-8")
    records = []
    for line in appended.splitlines():
        line = line.strip()
        if not line:
            continue
        try:
            record = json.loads(line)
        except json.JSONDecodeError:
            continue
        if isinstance(record, dict):
            records.append(record)
    return records


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--factions", default="td_gdi,td_nod", help="comma-separated faction internal names")
    parser.add_argument("--bot-a", default="hard", help="bot difficulty for side A (default: hard)")
    parser.add_argument("--bot-b", default="hard", help="bot difficulty for side B (default: hard)")
    parser.add_argument("--repeats", type=int, default=1, help="matches per matchup; sides alternate")
    parser.add_argument("--time-limit", type=int, default=30, choices=sorted(VALID_TIME_LIMITS))
    parser.add_argument("--support-dir", type=pathlib.Path, default=None)
    parser.add_argument("--retries", type=int, default=1,
                        help="extra attempts per match that dies without an exception log "
                             "(external kill signature); crashes with an exception are not retried")
    parser.add_argument("--stall-timeout", type=int, default=120,
                        help="seconds of debug.log silence before a live match counts as hung")
    parser.add_argument("--dry-run", action="store_true")
    parser.add_argument("--keep-variants", action="store_true", help="do not delete variant map dirs on success")
    args = parser.parse_args()

    factions = [f.strip() for f in args.factions.split(",") if f.strip()]
    if len(factions) < 1:
        fail("--factions needs at least one faction id")
    if args.repeats < 1:
        fail("--repeats must be >= 1")
    if not TEMPLATE_MAP.is_dir():
        fail(f"template map missing: {TEMPLATE_MAP}")

    mod_id, engine = load_config()
    executable = engine / "bin" / "OpenRA.exe"
    if not executable.is_file():
        fail(f"OpenRA executable is missing: {executable}; run make.cmd all first")

    support = args.support_dir or pathlib.Path(tempfile.gettempdir()) / f"ai-match-batch-{int(time.time())}"
    support = support.resolve()
    variants_root = support / USER_MAP_DIR
    logs_dir = support / "Logs"
    logs_dir.mkdir(parents=True, exist_ok=True)
    log_path = logs_dir / MATCH_LOG

    matchups = build_matchups(factions, args.bot_a, args.bot_b, args.repeats)

    # One variant dir per distinct matchup (repeats reuse it).
    variants = {}
    for m in matchups:
        variants.setdefault(
            m["variant"],
            {"a": m["side_a"], "b": m["side_b"]},
        )

    print(f"batch: {len(matchups)} match(es) across {len(variants)} variant(s), support={support}")
    for name, v in variants.items():
        print(f"  variant {name}: {v['a']['faction']}({v['a']['bot']}) vs {v['b']['faction']}({v['b']['bot']})")

    if args.dry_run:
        for m in matchups:
            print(f"  match {m['index']}.{m['repeat']}: {m['variant']}")
        return 0

    for name, v in variants.items():
        write_variant(TEMPLATE_MAP, variants_root / name, {"side_a": v["a"], "side_b": v["b"]}, args.time_limit)

    exceptions_before = {p.name for p in logs_dir.glob("exception-*.log")} if logs_dir.is_dir() else set()

    # The fixture locks gamespeed to insane (10 ms timestep), so the minute
    # cap is time_limit*6000 ticks — a CPU-contended box ticks well below the
    # 100 tps target. Bound wall time at a pessimistic sustained 20 tps; the
    # stall detector in run_match ends genuinely hung matches in ~2 minutes,
    # so a generous bound here only ever waits on a match still progressing.
    cap_ticks = args.time_limit * 6000
    timeout = cap_ticks // 20 + 300

    results = []
    progress_path = support / "batch_results.jsonl"
    for run, m in enumerate(matchups, 1):
        before = log_path.stat().st_size if log_path.is_file() else 0
        launch_args = [
            f"Game.Mod={mod_id}",
            "Engine.EngineDir=..",
            f"Engine.ModSearchPaths={REPO_ROOT / 'mods'},{engine / 'mods'}",
            f"Engine.SupportDir={support}",
            f"Launch.Map={m['variant']}",
            f"Launch.Benchmark={BENCHMARK_PREFIX}",
        ]

        attempt = 0
        while True:
            attempt += 1
            exc_before = {p.name for p in logs_dir.glob("exception-*.log")}
            print(f"[{run}/{len(matchups)}] {m['variant']} (repeat {m['repeat']}, attempt {attempt}) ...", flush=True)
            started = time.time()
            exit_code, output, status = run_match(
                executable, engine, launch_args, timeout,
                stall_log=logs_dir / "debug.log",
                stall_timeout=args.stall_timeout,
            )
            elapsed = int(time.time() - started)
            records = read_appended_records(log_path, before)
            new_exc = sorted(p.name for p in logs_dir.glob("exception-*.log") if p.name not in exc_before)

            # A nonzero exit with no exception log and no appended records is
            # the external-kill signature (TerminateProcess → exit 1; the
            # engine itself only ever returns 0 or -1-with-exception). Retry
            # those; a real crash writes exception-*.log and would just fail
            # the same way again.
            if not (status.startswith("exit=") and not records and not new_exc and attempt <= args.retries):
                break
            print(f"    -> {status} in {elapsed}s, no records/exception — external kill? retry {attempt}/{args.retries}",
                  flush=True)

        outcome = sorted(
            (r.get("player", {}).get("faction"), r.get("player", {}).get("outcome"))
            for r in records
        )
        result = {
            "variant": m["variant"],
            "status": status,
            "exit_code": exit_code,
            "attempts": attempt,
            "wall_seconds": elapsed,
            "records": len(records),
            "outcomes": outcome,
        }
        if new_exc:
            result["new_exceptions"] = new_exc
        results.append(result)
        # Durable per-match progress: a batch killed mid-run (the batch
        # process itself is as sweepable as the matches) still leaves this
        # evidence for postmortem and resume-by-rerun.
        with progress_path.open("a", encoding="utf-8") as f:
            f.write(json.dumps(result) + "\n")
        print(f"    -> {status} in {elapsed}s, {len(records)} record(s): {outcome}", flush=True)
        if status != "ok":
            print(f"    OpenRA output tail:\n{output_tail(output)}")

    new_exceptions = sorted(p.name for p in logs_dir.glob("exception-*.log") if p.name not in exceptions_before)
    summary = {
        "support_dir": str(support),
        "matches": len(matchups),
        "completed": sum(1 for r in results if r["status"] == "ok"),
        "timed_out": sum(1 for r in results if r["status"] == "timeout"),
        "stalled": sum(1 for r in results if r["status"] == "stalled"),
        "died": sum(1 for r in results if r["status"].startswith("exit=")),
        "retried": sum(1 for r in results if r["attempts"] > 1),
        "new_exceptions": new_exceptions,
        "results": results,
    }
    (support / "batch_summary.json").write_text(json.dumps(summary, indent=2), encoding="utf-8")

    print(
        f"\nbatch done: {summary['completed']}/{summary['matches']} clean, "
        f"{summary['timed_out']} timeout, {summary['stalled']} stalled, "
        f"{summary['died']} died ({summary['retried']} retried), exceptions={new_exceptions or 'none'}"
    )
    print(f"records appended to {log_path}; aggregate with tools/ai/aggregate_ai_matches.py")

    if not args.keep_variants:
        for name in variants:
            shutil.rmtree(variants_root / name, ignore_errors=True)

    if new_exceptions:
        return 1
    if not any(r["records"] for r in results):
        return 2
    return 0 if all(r["status"] == "ok" for r in results) else 1


if __name__ == "__main__":
    sys.exit(main())
