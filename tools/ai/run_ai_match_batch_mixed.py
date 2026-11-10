#!/usr/bin/env python3
"""Successor wrapper of tools/ai/run_ai_match_batch.py for MIXED-faction teams (MIXED-RUNNER-01, Frontrunner, claude-sonnet-5-5).

run_ai_match_batch.py gives every team ONE faction. This wrapper adds one option:

    --team-factions F0,F1,...,Fn-1    the faction of each seat of a team (n = --team-size, 2..8)

Seat i of team A and seat i of team B both play Fi (identical seat order on both teams); team A binds slots 0..n-1, team B slots n..2n-1, exactly as
the original does for --team-size n. Example, the user's 4v4 Ice Cold matchup, every seat hard:

    python tools/ai/run_ai_match_batch_mixed.py --team-factions td_gdi,td_nod,ra1_allies,ra1_soviets --team-size 4 \\
        --map mods/cameo/maps/ice_cold.oramap --bot-a hard --bot-b hard --repeats 1 --render headless --support-dir <dir>

Everything else is the ORIGINAL code, imported unmodified and run in-process: argument parsing, the LC7 fingerprint, engine runtime provenance,
the CAMEO_DEV_SEED environment (inherited unchanged: this wrapper never sets or reads it), caps, retries, logs, the roster identity checks and the
scoreboards. Per-seat factions go through the original patch_mp_block (the team-map path; patch_player_block is the BotA/BotB 1v1 template path)
and render_side_actors. Without --team-factions this wrapper is a plain pass-through to the original main(). The original file is never edited.

--runner-dir DIR (optional): import run_ai_match_batch.py from DIR instead of from this file's own directory. tools/ai is part of the runner's LC7
fingerprint paths, so a copy of this wrapper placed INSIDE a build tree would make that tree's fingerprint dirty (mod_dirty) or change its commit;
keep the wrapper outside the tree and point --runner-dir at the tree's tools/ai (REPO_ROOT, the engine and mod.config then come from that tree).

Mechanism: only build_matchups is replaced (in this process, for the duration of main). The original builds ONE pairing (the first seat's
faction against itself, team_size n, `--repeats` repeats); the replacement rewrites each team's per-seat factions and the variant name. --factions is
derived from --team-factions (the distinct factions in seat order); --factions together with --team-factions is refused. --team-size defaults to
the number of factions and must equal it. In mixed mode the batch fingerprint config also carries `team_factions` (the seat order), so a mixed batch
never shares a fingerprint id with a plain --factions batch.

One game per invocation: both teams are identical, so a repeat is the same layout and (CAMEO_DEV_SEED pinned) the same game. --repeats is therefore 1
(appended when absent) and any other value is refused; for N games run N invocations, each with its own CAMEO_DEV_SEED and --support-dir. Inside one
invocation the original already gives every game attempt a fresh SupportDir (games/<batch>/match-...), so telemetry files are never shared. A
different bot per team (--bot-a/--bot-b) is swapped by running twice with the two arguments exchanged, not with --swap-bots.

`--self-test` (no game, no engine, no OpenRA process): checks the argument rules and the matchup builder, renders the variant map.yaml of the
4v4 Ice Cold matchup from mods/cameo/maps/ice_cold.oramap into a temp dir and prints the per-seat factions, bots, homes, stances and starting
actors, then drives the original main() with --dry-run (engine provenance stubbed) to prove the wiring. Exit 0 = all checks passed.
"""
from __future__ import annotations

import argparse
import contextlib
import importlib.util
import inspect
import io
import os
import pathlib
import re
import shutil
import sys
import tempfile
import zipfile


def _load_base(runner_dir: str | None):
    """The ORIGINAL run_ai_match_batch module: this file's own directory by default, or --runner-dir (peeked from argv before the real parse)."""
    if runner_dir is None:
        own = pathlib.Path(__file__).resolve().parent
        sys.path.insert(0, str(own))
        try:
            import run_ai_match_batch as module
        except ModuleNotFoundError as exc:
            if exc.name != "run_ai_match_batch":
                raise
            raise SystemExit(f"ASSERTION FAILED: no run_ai_match_batch.py next to this wrapper ({own}); keep the wrapper beside the original or pass --runner-dir <tree>/tools/ai")
        return module
    target = pathlib.Path(runner_dir).resolve() / "run_ai_match_batch.py"
    if not target.is_file():
        raise SystemExit(f"ASSERTION FAILED: --runner-dir {runner_dir!r} has no run_ai_match_batch.py")
    sys.path.insert(0, str(target.parent))
    spec = importlib.util.spec_from_file_location("run_ai_match_batch", target)
    module = importlib.util.module_from_spec(spec)
    sys.modules["run_ai_match_batch"] = module
    spec.loader.exec_module(module)
    return module


def _peek_runner_dir(argv: list[str]) -> str | None:
    for i, tok in enumerate(argv):
        if tok == "--runner-dir":
            return argv[i + 1] if i + 1 < len(argv) else None
        if tok.startswith("--runner-dir="):
            return tok.split("=", 1)[1]
    return None


base = _load_base(_peek_runner_dir(sys.argv[1:]))  # the original, unmodified

_ORIGINAL_BUILD_MATCHUPS = base.build_matchups
_ORIGINAL_COMPUTE_FINGERPRINT = base.compute_fingerprint
_STATE: dict = {"team_factions": None}

EXPECTED_BUILD_SIGNATURE = ["factions", "bots_a", "bots_b", "repeats", "swap_bots", "team_size"]
EXPECTED_FINGERPRINT_SIGNATURE = ["map_source", "engine", "config", "repo_root"]
REPEATS_REFUSAL = ("--team-factions runs exactly one game per invocation (--repeats 1): both teams are identical, so a repeat is the same layout and, with "
                   "CAMEO_DEV_SEED pinned, the same game; run the wrapper again with another CAMEO_DEV_SEED and --support-dir for another game")
ICE_COLD = base.REPO_ROOT / "mods" / "cameo" / "maps" / "ice_cold.oramap"
USER_SETUP = ["td_gdi", "td_nod", "ra1_allies", "ra1_soviets"]


# ---------------------------------------------------------------- the option

def parse_team_factions(raw: str) -> list[str]:
    """'a,b,c,d' -> ['a','b','c','d'] (one faction per seat, 2..8). Every faction must have a StartingUnits group (class light or none)."""
    seats = [f.strip() for f in raw.split(",")]
    if not 2 <= len(seats) <= 8:
        base.fail(f"--team-factions needs 2 to 8 comma-separated faction ids, one per seat (got {len(seats)}: {raw!r})")
    for faction in seats:
        if not re.fullmatch(r"[a-z][a-z0-9_]*", faction):
            base.fail(f"--team-factions entry {faction!r} is not a faction id (lower-case letters, digits, underscore)")
        if not (base.starting_unit_group(faction, "light") or base.starting_unit_group(faction, "none")):
            base.fail(f"--team-factions: faction {faction!r} has no StartingUnits group for class light or none in the mod rules")
    return seats


def distinct(seats: list[str]) -> list[str]:
    return list(dict.fromkeys(seats))


def variant_name(seats: list[str], bots_a: list[str], bots_b: list[str]) -> str:
    n = len(seats)
    return f"ai_{n}v{n}_mixed_{'-'.join(seats)}_{'_'.join(bots_a)}__vs__{'_'.join(bots_b)}".replace(" ", "_")


def mixed_build_matchups(factions, bots_a, bots_b, repeats, swap_bots=False, team_size=1):
    """Replacement for run_ai_match_batch.build_matchups while --team-factions is active."""
    seats = _STATE["team_factions"]
    if seats is None:
        return _ORIGINAL_BUILD_MATCHUPS(factions, bots_a, bots_b, repeats, swap_bots=swap_bots, team_size=team_size)
    if team_size != len(seats):
        base.fail(f"--team-factions lists {len(seats)} seats but --team-size is {team_size}")
    if list(factions) != distinct(seats):
        base.fail(f"--factions {list(factions)} does not match the distinct factions of --team-factions {distinct(seats)}")
    if repeats != 1:
        base.fail(f"{REPEATS_REFUSAL} (got {repeats})")
    matchups = _ORIGINAL_BUILD_MATCHUPS([seats[0]], bots_a, bots_b, repeats, swap_bots=swap_bots, team_size=team_size)
    for m in matchups:
        m["team_a"] = [{"bot": member["bot"], "faction": faction} for member, faction in zip(m["team_a"], seats)]
        m["team_b"] = [{"bot": member["bot"], "faction": faction} for member, faction in zip(m["team_b"], seats)]
        m["variant"] = variant_name(seats, [x["bot"] for x in m["team_a"]], [x["bot"] for x in m["team_b"]])
    return matchups


def install_fingerprint_wrapper() -> None:
    """Mixed mode only: every batch and per-match fingerprint config also carries `team_factions` (the seat order). Delegates to the current compute_fingerprint."""
    inner = base.compute_fingerprint

    def mixed_compute_fingerprint(map_source, engine, config, *args, **kwargs):
        seats = _STATE["team_factions"]
        if seats is not None:
            config = dict(config, team_factions=list(seats))
        return inner(map_source, engine, config, *args, **kwargs)

    base.compute_fingerprint = mixed_compute_fingerprint


def prepare(argv: list[str]):
    """-> (seats or None, argv for the original main()). Raises SystemExit (base.fail) on a refused combination."""
    pre = argparse.ArgumentParser(add_help=False, allow_abbrev=False)
    pre.add_argument("--team-factions", default=None)
    pre.add_argument("--runner-dir", default=None)      # consumed here: _load_base already used it
    pre.add_argument("--self-test", action="store_true")
    ns, rest = pre.parse_known_args(argv)
    for flag in ("--team-factions", "--runner-dir"):
        if sum(1 for tok in argv if tok.split("=", 1)[0] == flag) > 1:
            base.fail(f"{flag} given more than once (the last one would silently win)")
    if ns.self_test:
        base.fail("--self-test must be the only argument")
    if ns.team_factions is None:
        return None, rest
    seats = parse_team_factions(ns.team_factions)
    size_probe = argparse.ArgumentParser(add_help=False, allow_abbrev=False)
    size_probe.add_argument("--team-size", type=int, default=None)
    size_ns, _ = size_probe.parse_known_args(rest)
    repeats_probe = argparse.ArgumentParser(add_help=False, allow_abbrev=False)
    repeats_probe.add_argument("--repeats", type=int, default=None)
    repeats_ns, _ = repeats_probe.parse_known_args(rest)
    for tok in rest:
        key = tok.split("=", 1)[0]
        if tok.startswith("--") and len(key) >= 3 and "--factions".startswith(key):
            base.fail("--factions cannot be combined with --team-factions (the faction list is derived from it)")
        if tok.startswith("--") and len(key) >= 4 and key != "--team-size" and "--team-size".startswith(key):
            base.fail("spell --team-size in full when --team-factions is used (an abbreviation could hide a size conflict)")
        if tok.startswith("--") and len(key) >= 5 and key != "--repeats" and "--repeats".startswith(key):
            base.fail("spell --repeats in full when --team-factions is used (an abbreviation could hide a repeat count)")
    for flag in ("--team-size", "--repeats"):
        if sum(1 for tok in rest if tok.split("=", 1)[0] == flag) > 1:
            base.fail(f"{flag} given more than once (the last one would silently win)")
    if repeats_ns.repeats is not None and repeats_ns.repeats != 1:
        base.fail(f"{REPEATS_REFUSAL} (got {repeats_ns.repeats})")
    if size_ns.team_size is not None and size_ns.team_size != len(seats):
        base.fail(f"--team-factions lists {len(seats)} seats but --team-size is {size_ns.team_size}")
    out = list(rest)
    if size_ns.team_size is None:
        out += ["--team-size", str(len(seats))]
    if repeats_ns.repeats is None:
        out += ["--repeats", "1"]
    out += ["--factions", ",".join(distinct(seats))]
    return seats, out


def roster_lines(seats: list[str]) -> list[str]:
    n = len(seats)
    lines = [f"mixed teams ({n}v{n}): identical seat order on both teams (team A slots 0..{n - 1}, team B slots {n}..{2 * n - 1})",
             f"one game per invocation (--repeats 1): set CAMEO_DEV_SEED and --support-dir per game",
             "  seat   team A    team B    faction"]
    lines += [f"  {i:>4}   Multi{i:<4}  Multi{n + i:<4}  {faction}" for i, faction in enumerate(seats)]
    return lines


def main(argv: list[str] | None = None) -> int:
    argv = list(sys.argv[1:] if argv is None else argv)
    if "--self-test" in argv:
        if argv != ["--self-test"]:
            base.fail("--self-test must be the only argument")
        return self_test()
    if "-h" in argv or "--help" in argv:
        print(__doc__.split("`--self-test`")[0].rstrip() + "\n\n--- original options follow ---\n")
    seats, new_argv = prepare(argv)
    sys.argv = [sys.argv[0]] + new_argv
    if seats is None:
        return base.main()
    _STATE["team_factions"] = seats
    base.build_matchups = mixed_build_matchups
    install_fingerprint_wrapper()
    print("\n".join(roster_lines(seats)))
    return base.main()


# ---------------------------------------------------------------- self-test (no game, no engine process)

def _block(text: str, ref: str) -> dict:
    m = re.search(rf"(?m)^\tPlayerReference@{ref}:\n((?:\t\t[^\n]*\n)*)", text)
    if not m:
        raise AssertionError(f"PlayerReference@{ref} block missing")
    fields = {}
    for line in m.group(1).splitlines():
        key, _, value = line.strip().partition(":")
        fields[key.strip()] = value.strip()
    return fields


def _expect_exit(fn, *args) -> bool:
    buf = io.StringIO()
    try:
        with contextlib.redirect_stdout(buf):
            fn(*args)
    except SystemExit as exc:
        return exc.code not in (0, None)
    return False


def self_test() -> int:
    results: list[tuple[str, bool, str]] = []

    def check(name: str, ok: bool, detail: str = "") -> None:
        results.append((name, bool(ok), detail))
        print(f"  {'PASS' if ok else 'FAIL'}  {name}" + (f"  [{detail}]" if detail and not ok else ""))

    seats = list(USER_SETUP)
    print("MIXED-RUNNER-01 self-test (no game, no engine process)")

    # 1. the original surface this wrapper relies on
    check("original build_matchups signature unchanged", list(inspect.signature(_ORIGINAL_BUILD_MATCHUPS).parameters) == EXPECTED_BUILD_SIGNATURE,
          str(list(inspect.signature(_ORIGINAL_BUILD_MATCHUPS).parameters)))
    check("original compute_fingerprint signature unchanged", list(inspect.signature(_ORIGINAL_COMPUTE_FINGERPRINT).parameters) == EXPECTED_FINGERPRINT_SIGNATURE,
          str(list(inspect.signature(_ORIGINAL_COMPUTE_FINGERPRINT).parameters)))
    check("original entry points exist", all(callable(getattr(base, n, None)) for n in
          ("main", "fail", "patch_mp_block", "write_variant", "render_side_actors", "starting_unit_group", "split_spawn_sides", "mp_spawn_cells",
           "compute_fingerprint", "prepare_match_support")) and getattr(base, "USER_MAP_DIR", None) is not None)
    check("ice_cold.oramap present", ICE_COLD.is_file(), str(ICE_COLD))

    # 2. the option and its refusals
    check("parse_team_factions keeps seat order", parse_team_factions(",".join(seats)) == seats)
    check("parse_team_factions allows repeated factions", parse_team_factions("td_gdi,td_gdi,ra1_allies") == ["td_gdi", "td_gdi", "ra1_allies"])
    for bad in ("", "td_gdi", "td_gdi,,td_nod", "td_gdi,no_such_faction", "TD_GDI,td_nod", ",".join(["td_gdi"] * 9)):
        check(f"parse_team_factions refuses {bad!r}", _expect_exit(parse_team_factions, bad))
    got, argv = prepare(["--team-factions", ",".join(seats), "--map", "m"])
    check("prepare derives --team-size, --repeats 1 and --factions", got == seats and argv == ["--map", "m", "--team-size", "4", "--repeats", "1", "--factions", ",".join(seats)], str(argv))
    got, argv = prepare(["--team-factions", "td_gdi,td_gdi,td_nod,td_nod", "--team-size=4", "--repeats=1"])
    check("prepare keeps an equal explicit --team-size and --repeats 1 and de-duplicates --factions", got == ["td_gdi", "td_gdi", "td_nod", "td_nod"]
          and argv == ["--team-size=4", "--repeats=1", "--factions", "td_gdi,td_nod"], str(argv))
    got, argv = prepare(["--repeats", "2"])
    check("prepare passes through without --team-factions", got is None and argv == ["--repeats", "2"])
    got, argv = prepare(["--runner-dir", "D", "--repeats", "2"])
    check("prepare drops --runner-dir (used at import) in pass-through mode", got is None and argv == ["--repeats", "2"], str(argv))
    got, argv = prepare(["--team-factions", ",".join(seats), "--runner-dir=D", "--map", "m"])
    check("prepare drops --runner-dir in mixed mode", got == seats and "--runner-dir=D" not in argv and "D" not in argv, str(argv))
    check("_peek_runner_dir reads both spellings", _peek_runner_dir(["--runner-dir", "X"]) == "X" and _peek_runner_dir(["--runner-dir=Y"]) == "Y"
          and _peek_runner_dir(["--repeats", "1"]) is None and _peek_runner_dir(["--runner-dir"]) is None)
    own = pathlib.Path(__file__).resolve().parent
    saved_module = sys.modules.get("run_ai_match_batch")
    try:
        check("_load_base(own dir) returns the same original module", _load_base(str(own)).REPO_ROOT == base.REPO_ROOT)
    finally:
        sys.modules["run_ai_match_batch"] = saved_module
    check("_load_base refuses a dir without the runner", _expect_exit(_load_base, str(tempfile.gettempdir()) + "/no_such_runner_dir_xyz"))
    saved_path, saved_mod, saved_file = list(sys.path), sys.modules.pop("run_ai_match_batch", None), globals()["__file__"]
    empty = pathlib.Path(tempfile.mkdtemp(prefix="mixed_runner_norunner_"))
    try:
        sys.path[:] = [p for p in sys.path if pathlib.Path(p or ".").resolve() != own]
        globals()["__file__"] = str(empty / "wrapper_copy.py")
        no_runner_exit = _expect_exit(_load_base, None)
    finally:
        sys.path[:] = saved_path
        sys.modules["run_ai_match_batch"] = saved_mod
        globals()["__file__"] = saved_file
        shutil.rmtree(empty, ignore_errors=True)
    check("_load_base with no runner beside the wrapper exits with a message (no bare ModuleNotFoundError)", no_runner_exit)
    for bad in (["--team-factions", ",".join(seats), "--factions", "td_gdi"], ["--team-factions", ",".join(seats), "--factions=td_gdi"],
                ["--team-factions", ",".join(seats), "--fa", "td_gdi"], ["--team-factions", ",".join(seats), "--team-size", "2"],
                ["--team-factions", ",".join(seats), "--team-size=8"], ["--team-factions", ",".join(seats), "--team-s", "4"],
                ["--self-test", "--repeats", "1"],
                ["--team-factions", ",".join(seats), "--repeats", "2"], ["--team-factions", ",".join(seats), "--repeats=4"],
                ["--team-factions", ",".join(seats), "--repeats", "0"], ["--team-factions", ",".join(seats), "--rep", "2"],
                ["--team-factions", ",".join(seats), "--repeats", "1", "--repeats", "1"],
                ["--team-factions", ",".join(seats), "--team-size", "4", "--team-size", "4"],
                ["--team-factions", ",".join(seats), "--team-factions", ",".join(seats)],
                ["--team-factions", ",".join(seats), "--runner-dir", "A", "--runner-dir=B"], ["--runner-dir", "A", "--runner-dir", "B"]):
        check(f"prepare refuses {' '.join(bad)}", _expect_exit(prepare, bad))

    # 3. the matchup builder
    _STATE["team_factions"] = seats
    try:
        ms = mixed_build_matchups(distinct(seats), ["hard"] * 4, ["hard"] * 4, 1, False, 4)
        check("one repeat gives one matchup of one mixed variant", len(ms) == 1 and "_mixed_" in ms[0]["variant"])
        for bad_repeats in (0, 2, 4):
            check(f"builder refuses --repeats {bad_repeats}", _expect_exit(mixed_build_matchups, distinct(seats), ["hard"] * 4, ["hard"] * 4, bad_repeats, False, 4))
        check("both teams carry the factions in seat order", all([x["faction"] for x in m["team_a"]] == seats == [x["faction"] for x in m["team_b"]] for m in ms))
        check("all seats are hard, slots 0..3 vs 4..7", all(all(x["bot"] == "hard" for x in m["team_a"] + m["team_b"]) and m["slots_a"] == [0, 1, 2, 3] and m["slots_b"] == [4, 5, 6, 7] for m in ms))
        check("team size must equal the seat count", _expect_exit(mixed_build_matchups, distinct(seats), ["hard"] * 3, ["hard"] * 3, 1, False, 3))
        check("--factions must match the derived list", _expect_exit(mixed_build_matchups, ["td_gdi"], ["hard"] * 4, ["hard"] * 4, 1, False, 4))
        ms2 = mixed_build_matchups(distinct(seats), ["hard"] * 4, ["classic"] * 4, 1, True, 4)
        check("different bots per team keep the faction seat order and give another variant name",
              [x["bot"] for x in ms2[0]["team_a"]] == ["hard"] * 4 and [x["bot"] for x in ms2[0]["team_b"]] == ["classic"] * 4
              and [x["faction"] for x in ms2[0]["team_a"]] == seats == [x["faction"] for x in ms2[0]["team_b"]] and ms2[0]["variant"] != ms[0]["variant"])
    finally:
        _STATE["team_factions"] = None

    # 4. the rendered variant map.yaml (original write_variant -> patch_mp_block + render_side_actors)
    tmp = pathlib.Path(tempfile.mkdtemp(prefix="mixed_runner_selftest_"))
    try:
        if ICE_COLD.is_file():
            _STATE["team_factions"] = seats
            try:
                m = mixed_build_matchups(distinct(seats), ["hard"] * 4, ["hard"] * 4, 1, False, 4)[0]
            finally:
                _STATE["team_factions"] = None
            dest = tmp / "variant"
            base.write_variant(ICE_COLD, dest, {k: m[k] for k in ("team_size", "team_a", "team_b", "slots_a", "slots_b")}, 3)
            text = (dest / "map.yaml").read_text(encoding="utf-8")
            with zipfile.ZipFile(ICE_COLD) as archive:
                homes = base.split_spawn_sides(base.mp_spawn_cells(archive.read("map.yaml").decode("utf-8"))[:8], 4)[:8]
            print(f"  variant {m['variant']}")
            print("  seat  ref     faction       bot   home      allies                 base actor")
            blocks, ok_rows, problems = {}, True, []
            for i in range(8):
                ref = f"Multi{i}"
                b = _block(text, ref)
                blocks[ref] = b
                want = seats[i % 4]
                group = base.starting_unit_group(want, "light") or base.starting_unit_group(want, "none")
                actor = re.search(rf"(?m)^\t{ref}_base: (\S+)\n\t\tOwner: {ref}\n", text)
                row_ok = (b.get("Faction") == want and b.get("Bot") == "hard" and b.get("Playable") == "False" and b.get("HomeLocation") == f"{homes[i][0]},{homes[i][1]}"
                          and actor is not None and group is not None and actor.group(1) == group[0])
                team_refs = [f"Multi{j}" for j in range(8) if j // 4 == i // 4 and j != i]
                enemy_refs = [f"Multi{j}" for j in range(8) if j // 4 != i // 4]
                allies = [s.strip() for s in b.get("Allies", "").split(",") if s.strip()]
                enemies = [s.strip() for s in b.get("Enemies", "").split(",") if s.strip()]
                row_ok = row_ok and all(r in allies for r in team_refs) and not any(r in allies for r in enemy_refs) and all(r in enemies for r in enemy_refs)
                if not row_ok:
                    ok_rows = False
                    problems.append(ref)
                print(f"  {i:>4}  {ref:<6}  {b.get('Faction', '?'):<12}  {b.get('Bot', '?'):<5} {b.get('HomeLocation', '?'):<8}  {','.join(allies):<21}  {actor.group(1) if actor else '?'}")
            check("every Multi seat has its faction, hard bot, home, stances and faction base actor", ok_rows, "bad seats: " + ",".join(problems))
            fa = [blocks[f"Multi{i}"].get("Faction") for i in range(4)]
            fb = [blocks[f"Multi{i}"].get("Faction") for i in range(4, 8)]
            check("seat order identical on both teams", fa == fb == seats, f"{fa} vs {fb}")
            check("four different factions per team (the user's setup)", len(set(fa)) == 4 and len(set(fb)) == 4)
            check("eight distinct homes", len({blocks[f'Multi{i}'].get('HomeLocation') for i in range(8)}) == 8)
            try:
                roster = base.planned_economy_roster(dest / "map.yaml", {"team_a": m["team_a"], "team_b": m["team_b"]})
                roster_ok = len(roster) == 8 and sorted(f for _n, f, _b, _h in roster) == sorted(seats * 2)
            except ValueError as exc:
                roster_ok = False
                print(f"  planned_economy_roster refused the variant: {exc}")
            check("original planned_economy_roster accepts the mixed variant", roster_ok)

        # 5. the original main() end to end up to --dry-run (engine provenance stubbed; no variant is written, nothing is launched)
        fake_engine = tmp / "engine"
        (fake_engine / "bin").mkdir(parents=True)
        (fake_engine / "bin" / "OpenRA.exe").write_bytes(b"")
        seen_config: list[dict] = []
        fp = {"mod_commit": None, "mod_dirty": None, "engine_version": None, "mod_dlls_sha256": None, "runtime_engine": {}, "ai_yaml_sha256": None,
              "ai_yaml_files": 0, "mod_yaml_sha256": None, "mod_yaml_files": 0, "map_sha256": None}

        def fake_fingerprint(map_source, engine, config, *args, **kwargs):
            seen_config.append(dict(config))
            return dict(fp, config=dict(config))
        saved = (base.load_config, base.stale_mod_assemblies, base.compute_fingerprint, base.build_matchups, sys.argv)

        def drive(argv_in):
            seen_config.clear()
            buf = io.StringIO()
            rc_ = None
            try:
                base.load_config = lambda: ("cameo", fake_engine)
                base.stale_mod_assemblies = lambda engine: []
                base.compute_fingerprint = fake_fingerprint
                with contextlib.redirect_stdout(buf):
                    rc_ = main(argv_in)
            finally:
                base.load_config, base.stale_mod_assemblies, base.compute_fingerprint, base.build_matchups, sys.argv = saved
                _STATE["team_factions"] = None
            return rc_, buf.getvalue(), (dict(seen_config[0]) if seen_config else {})

        mixed_argv = ["--team-factions", ",".join(seats), "--map", str(ICE_COLD), "--bot-a", "hard", "--bot-b", "hard", "--dry-run",
                      "--support-dir", str(tmp / "support")]
        seed_before = os.environ.get("CAMEO_DEV_SEED")
        try:
            os.environ["CAMEO_DEV_SEED"] = "5101"
            rc, out, cfg = drive(mixed_argv)
            seed_kept = os.environ.get("CAMEO_DEV_SEED") == "5101"
            os.environ.pop("CAMEO_DEV_SEED", None)
            rc_unset, _out_unset, _cfg_unset = drive(mixed_argv)
            seed_still_unset = "CAMEO_DEV_SEED" not in os.environ
        finally:
            if seed_before is None:
                os.environ.pop("CAMEO_DEV_SEED", None)
            else:
                os.environ["CAMEO_DEV_SEED"] = seed_before
        rc_pt, out_pt, cfg_pt = drive(["--map", str(ICE_COLD), "--factions", "td_gdi,td_nod", "--team-size", "4", "--bot-a", "hard", "--bot-b", "hard",
                                       "--repeats", "2", "--dry-run", "--support-dir", str(tmp / "support_pt")])
        print("  --- original main() --dry-run output (mixed) ---")
        for line in out.splitlines():
            print("  | " + line)
        check("original main() ran to --dry-run and returned 0", rc == 0, f"rc={rc}")
        check("dry-run lists 1 match over 1 mixed variant (--repeats 1 appended)", "batch: 1 match(es) across 1 variant(s)" in out and out.count("match 0.") == 1 and "_mixed_" in out, out[:200])
        check("fingerprint config shows the 4 factions, the seat order, team size 4, hard bots, repeats 1", cfg.get("factions") == seats and cfg.get("team_factions") == seats
              and cfg.get("team_size") == 4 and cfg.get("bots_a") == ["hard"] * 4 and cfg.get("bots_b") == ["hard"] * 4 and cfg.get("repeats") == 1, str(cfg))
        check("CAMEO_DEV_SEED is untouched (kept when set, still unset when unset)", rc_unset == 0 and seed_kept and seed_still_unset and os.environ.get("CAMEO_DEV_SEED") == seed_before)
        check("pass-through batch is unchanged: no team_factions key, --repeats 2 kept", rc_pt == 0 and "team_factions" not in cfg_pt and cfg_pt.get("repeats") == 2, str(cfg_pt))
        check("main refuses --repeats 2 in mixed mode before any engine step", _expect_exit(main, ["--team-factions", ",".join(seats), "--repeats", "2", "--dry-run"]))
        check("dry-run wrote no variant", not (tmp / "support" / base.USER_MAP_DIR).exists())
        check("the original functions are restored after the run", base.build_matchups is _ORIGINAL_BUILD_MATCHUPS and base.compute_fingerprint is _ORIGINAL_COMPUTE_FINGERPRINT)

        # 6. per-game SupportDir (the original's prepare_match_support): a distinct leaf per game attempt, reuse refused
        pergame, vroot = tmp / "pergame", tmp / "variants_src"
        (vroot / "v1").mkdir(parents=True)
        (vroot / "v1" / "map.yaml").write_text("x\n", encoding="utf-8")
        leaf_a = base.prepare_match_support(pergame, vroot, "v1", 0, 0, 0, "batch1")
        leaf_b = base.prepare_match_support(pergame, vroot, "v1", 1, 0, 0, "batch1")
        check("every game attempt gets its own SupportDir, Logs dir and map copy", leaf_a != leaf_b and (leaf_a / "Logs").is_dir() and (leaf_b / "Logs").is_dir()
              and (leaf_a / base.USER_MAP_DIR / "v1" / "map.yaml").is_file() and (leaf_b / base.USER_MAP_DIR / "v1" / "map.yaml").is_file())
        try:
            base.prepare_match_support(pergame, vroot, "v1", 0, 0, 0, "batch1")
            reused = True
        except FileExistsError:
            reused = False
        check("a per-game SupportDir is never reused", not reused)
    finally:
        shutil.rmtree(tmp, ignore_errors=True)

    failed = [name for name, ok, _ in results if not ok]
    print(f"self-test: {len(results) - len(failed)}/{len(results)} checks passed" + ("" if not failed else "; FAILED: " + "; ".join(failed)))
    return 0 if not failed else 1


if __name__ == "__main__":
    sys.exit(main())
