#!/usr/bin/env python3
"""Fail-closed single-switch A/B campaign planner and evidence analyzer.

This tool deliberately separates `dry-run` from `run`. A run requires a frozen,
approved manifest and an explicit --execute; the dry-run never starts OpenRA.
"""
from __future__ import annotations

import argparse
import collections
import hashlib
import json
import math
import pathlib
import random
import subprocess
import sys
from typing import Any

SCHEMA = 1
SWITCHES = ("BU_harvester_logistics", "U_ut4_expansion_appetite", "AF_harvester_spread")
MAPS = {
    "small_gate": ("mods/cameo/maps/_ra_pitfight.oramap", "f50f2399a38346c501f1b1ff91614f7eb9c73b9d0b8eadeffaa067a5bcf4c4d8"),
    "open_economy": ("mods/cameo/maps/_ra_ore-gardens.oramap", "e74bfaacaf0accfbaf34624e10e0708e61468867c1164aa24927c4f24a750842"),
    "chokepoint": ("mods/cameo/maps/_ra_siberian-pass.oramap", "dd1143299340094a604a5e2a02474dd095916f4f0dd75e9de0a1be23910bdd75"),
}
MAX_MANIFEST = 128 * 1024
MAX_INPUT = 512 * 1024 * 1024
MAX_LINE = 4 * 1024 * 1024
MAX_ROWS = 200000


def _pairs(pairs):
    out = {}
    for k, v in pairs:
        if k in out:
            raise ValueError(f"duplicate JSON key: {k}")
        out[k] = v
    return out


def read_json(path: pathlib.Path, limit: int = MAX_MANIFEST):
    data = path.read_bytes()
    if len(data) > limit:
        raise ValueError(f"file exceeds {limit} bytes: {path}")
    return json.loads(data, object_pairs_hook=_pairs)


def sha256(path: pathlib.Path) -> str:
    h = hashlib.sha256()
    with path.open("rb") as f:
        for b in iter(lambda: f.read(1024 * 1024), b""):
            h.update(b)
    return h.hexdigest()


def require(condition: bool, message: str):
    if not condition:
        raise ValueError(message)


def validate_manifest(m: dict[str, Any], root: pathlib.Path, *, executable=False):
    require(isinstance(m, dict), "manifest must be object")
    require(m.get("schema") == SCHEMA, "manifest schema")
    require(m.get("switches") == list(SWITCHES), "switch list/order must match approved spec")
    require(m.get("pairs_per_stratum") == 20, "campaign requires exactly 20 matched pairs per switch/map")
    require(m.get("team_size") == 1 and m.get("bot_type") == "hard", "campaign scope requires hard-bot 1v1 mirrors")
    require(m.get("map_strata") == [{"name": n, "path": p, "sha256": h} for n, (p, h) in MAPS.items()],
            "map strata/path/hash mismatch")
    pins = m.get("pins")
    require(isinstance(pins, dict), "pins missing")
    for k in ("source_commit", "engine_version"):
        v = pins.get(k)
        require(isinstance(v, str) and len(v) == 40 and all(c in "0123456789abcdef" for c in v.lower()), f"invalid {k}")
    pinned = subprocess.run(["git", "-C", str(root), "cat-file", "-e", f"{pins['source_commit']}^{{commit}}"],
                            capture_output=True, text=True, timeout=20)
    require(pinned.returncode == 0, "frozen source commit is absent from this repository")
    engine_version = root / "engine" / "VERSION"
    if engine_version.exists():
        require(engine_version.read_text(encoding="ascii").strip().lower() == pins["engine_version"].lower(),
                "engine VERSION differs from frozen pin")
    require(type(m.get("world_tick_cap")) is int and m["world_tick_cap"] == 45000, "world cap must be 45000")
    require(type(m.get("wall_timeout_s")) is int and m["wall_timeout_s"] == 3000, "wall timeout must be 3000")
    require(type(m.get("stall_timeout_s")) is int and m["stall_timeout_s"] == 180, "stall timeout must be 180")
    require(type(m.get("memory_limit_bytes")) is int and m["memory_limit_bytes"] == 8 * 1024**3, "memory ceiling must be 8 GiB")
    require(m.get("serial_workers") == 1, "exactly one serial worker required")
    if executable:
        require(m.get("execution_approved") is True, "manifest is not execution-approved")
        receipts = m.get("approval_receipts")
        require(isinstance(receipts, list) and len(receipts) >= 2 and all(isinstance(x, str) and x.strip() for x in receipts),
                "lead and maintainer approval receipts required")
    for entry in m["map_strata"]:
        p = (root / pathlib.PurePosixPath(entry["path"])).resolve()
        require(root.resolve() in p.parents, "map path escapes repo")
        require(p.is_file() and sha256(p) == entry["sha256"], f"map package hash mismatch: {entry['name']}")
    return True


def make_jobs(m: dict[str, Any]):
    jobs = []
    n = m["pairs_per_stratum"]
    seeds = m.get("seeds")
    require(isinstance(seeds, list) and len(seeds) == n and len(set(seeds)) == n and
            all(type(s) is int and 0 <= s <= 2**31 - 1 for s in seeds), "manifest needs 20 distinct integer seeds")
    for switch in SWITCHES:
        for mp in m["map_strata"]:
            for pair, seed in enumerate(seeds):
                # The two rows are the side-swapped members of one matched pair.
                jobs.append({"switch": switch, "map": mp["name"], "map_path": mp["path"],
                            "map_sha256": mp["sha256"], "seed": seed, "pair": pair,
                            "team_size": 1, "bot_type": "hard", "game_in_pair": 0,
                            "control_side": "A", "treatment_side": "B"})
                jobs.append({"switch": switch, "map": mp["name"], "map_path": mp["path"],
                            "map_sha256": mp["sha256"], "seed": seed, "pair": pair,
                            "team_size": 1, "bot_type": "hard", "game_in_pair": 1,
                            "control_side": "B", "treatment_side": "A"})
    # Stable pseudo-random order keyed by campaign id, preserving the frozen job set.
    rng = random.Random(int(hashlib.sha256(m["campaign_id"].encode()).hexdigest()[:16], 16))
    rng.shuffle(jobs)
    return jobs


def _load_jsonl(paths):
    rows = []
    total = 0
    for path in paths:
        p = pathlib.Path(path)
        files = [p] if p.is_file() else sorted(p.rglob("cameo-ai-matches.jsonl"))
        for f in files:
            if not f.is_file():
                continue
            total += f.stat().st_size
            if total > MAX_INPUT:
                raise ValueError("input size cap exceeded")
            with f.open("rb") as stream:
                for line in stream:
                    if len(line) > MAX_LINE or not line.endswith(b"\n"):
                        raise ValueError(f"oversize/truncated JSONL row: {f}")
                    if line.strip():
                        rows.append(json.loads(line, object_pairs_hook=_pairs))
                        if len(rows) > MAX_ROWS:
                            raise ValueError("input row cap exceeded")
    return rows


def classify_game(records, receipt):
    """Classify one game only when receipt and seat evidence agree."""
    uid_set = {r.get("game_uid") for r in records}
    if len(uid_set) != 1 or None in uid_set or not isinstance(receipt, dict):
        return "INVALID_UNKNOWN", "game_uid/receipt missing"
    seats = receipt.get("seats")
    if not isinstance(seats, list) or not seats:
        return "INVALID_UNKNOWN", "resolved seat proof missing"
    map_name = receipt.get("map")
    if map_name not in MAPS or receipt.get("map_package_sha256") != MAPS[map_name][1]:
        return "INVALID_UNKNOWN", "map package identity missing/mismatched"
    seed = receipt.get("seed")
    if (type(seed) is not int or receipt.get("invocation_seed") != seed or
            receipt.get("server_seed") != seed or
            receipt.get("server_seed_log_line") != f"CAMEO DEV SEED pinned - RandomSeed={seed} (parity harness)"):
        return "INVALID_UNKNOWN", "seed invocation/server pin evidence missing/mismatched"
    manifest_sha = receipt.get("manifest_sha256")
    if not isinstance(manifest_sha, str) or len(manifest_sha) != 64 or any(c not in "0123456789abcdef" for c in manifest_sha.lower()):
        return "INVALID_UNKNOWN", "campaign manifest pin missing"
    require_fields = ("home", "arm", "bot_type", "faction")
    if any(not isinstance(s, dict) or any(not isinstance(s.get(k), str) or not s[k] for k in require_fields)
           for s in seats):
        return "INVALID_UNKNOWN", "seat proof missing home/arm/bot/faction"
    if receipt.get("seat_proof_source") != "generated_map.yaml" or not isinstance(receipt.get("map_yaml_sha256"), str):
        return "INVALID_UNKNOWN", "generated map.yaml proof missing"
    proof = {s["home"]: s for s in seats}
    homes = [((r.get("player") or {}).get("home")) for r in records]
    if len(proof) != len(seats) or len(set(homes)) != len(homes) or any(h not in proof for h in homes):
        return "INVALID_UNKNOWN", "map.yaml seat proof does not match records"
    arms = {proof[h]["arm"] for h in homes}
    if arms != {"control", "treatment"}:
        return "INVALID_UNKNOWN", "both resolved arms not represented"
    for h, r in zip(homes, records):
        player = r.get("player") or {}
        if any(player.get(k) != proof[h][k] for k in ("bot_type", "faction")):
            return "INVALID_UNKNOWN", "player record disagrees with generated map.yaml seat proof"
    status = receipt.get("status")
    cap_evidence = (receipt.get("cap_marker") is True and
                    receipt.get("cap_tick") == receipt.get("world_tick_cap") and
                    receipt.get("support_complete") is True and receipt.get("replay_complete") is True)
    outcomes = [(proof[h]["arm"], (r.get("player") or {}).get("outcome")) for h, r in zip(homes, records)]
    natural = status == "NATURAL_END" and receipt.get("end_reason") == "natural" and all(o in ("won", "lost") for _, o in outcomes)
    if status in ("CRASH", "INCOMPLETE", "MEMORY_KILL", "WALL_TIMEOUT", "STALL", "PROCESS_DIED"):
        return "INCOMPLETE_UNKNOWN", status
    if status == "CENSORED_CAP" and cap_evidence:
        return "CENSORED_CAP", "explicit cap marker"
    if status == "CENSORED_CAP":
        return "INVALID_UNKNOWN", "cap receipt lacks exact cap evidence"
    if not natural:
        return "INVALID_UNKNOWN", "natural end evidence missing"
    winners = [arm for arm, outcome in outcomes if outcome == "won"]
    if len(winners) != 1:
        return "INVALID_UNKNOWN", "outcomes lack exactly one winner"
    return ("TREATMENT_WIN" if winners[0] == "treatment" else "CONTROL_WIN"), "natural end"


def economy(record):
    timeline = record.get("stats_timeline")
    if not isinstance(timeline, list) or not timeline:
        return {"status": "UNKNOWN"}
    last = timeline[-1]
    if not isinstance(last, dict) or any(type(last.get(k)) is not int for k in ("earned", "spent", "banked", "tick")):
        return {"status": "UNKNOWN"}
    result = {"status": "RECORDED", **{k: last[k] for k in ("earned", "spent", "banked", "tick")}}
    result["idle_queues"] = last.get("idle_queues") if type(last.get("idle_queues")) is int else None
    events = record.get("campaign_events")
    if isinstance(events, list) and all(isinstance(e, dict) and isinstance(e.get("kind"), str) and
                                        type(e.get("tick")) is int for e in events):
        result["events"] = events
    else:
        result["events"] = None
    return result


def summarize(rows, receipts):
    by_game = collections.defaultdict(list)
    for r in rows:
        by_game[r.get("game_uid")].append(r)
    games = []
    for uid, recs in sorted(by_game.items(), key=lambda x: str(x[0])):
        receipt = receipts.get(uid)
        verdict, why = classify_game(recs, receipt)
        games.append({"game_uid": uid, "verdict": verdict, "evidence": why,
                      "seats": [{"home": (r.get("player") or {}).get("home"),
                                 "arm": next((s.get("arm") for s in (receipt or {}).get("seats", [])
                                              if s.get("home") == (r.get("player") or {}).get("home")), None),
                                 "faction": (r.get("player") or {}).get("faction"),
                                 "outcome": (r.get("player") or {}).get("outcome"),
                                 "economy": economy(r)} for r in recs]})
    pairs = collections.defaultdict(dict)
    for g in games:
        r = receipts.get(g["game_uid"], {})
        key = (r.get("switch"), r.get("map"), r.get("seed"), r.get("pair"))
        if all(v is not None for v in key):
            pairs[key][r.get("game_in_pair")] = g
    stratum = collections.defaultdict(lambda: {"planned_pairs": 0, "complete_natural_pairs": 0,
                                               "censored_games": 0, "incomplete_games": 0,
                                               "unknown_games": 0, "pair_differences": [],
                                               "income": {"control_earned": 0, "treatment_earned": 0,
                                                          "control_spent": 0, "treatment_spent": 0,
                                                          "control_banked": 0, "treatment_banked": 0,
                                                          "control_idle_queues": 0, "treatment_idle_queues": 0,
                                                          "idle_queue_samples": 0, "missing_seats": 0},
                                               "event_metrics": {"status": "UNKNOWN", "counts": {}, "first_tick": {}}})
    for key, games_in_pair in pairs.items():
        switch, map_name, _, _ = key
        s = stratum[(switch, map_name)]
        s["planned_pairs"] += 1
        if set(games_in_pair) == {0, 1}:
            game_receipts = []
            for index in (0, 1):
                g = games_in_pair[index]
                game_receipts.append(receipts.get(g["game_uid"], {}))
            a, b = game_receipts
            if not (a.get("control_side") == "A" and a.get("treatment_side") == "B" and
                    b.get("control_side") == "B" and b.get("treatment_side") == "A"):
                for g in games_in_pair.values():
                    g["verdict"] = "INVALID_UNKNOWN"
                    g["evidence"] = "matched pair side-swap proof missing"
        for g in games_in_pair.values():
            if g["verdict"] == "CENSORED_CAP": s["censored_games"] += 1
            elif g["verdict"] == "INCOMPLETE_UNKNOWN": s["incomplete_games"] += 1
            elif g["verdict"] == "INVALID_UNKNOWN": s["unknown_games"] += 1
        if set(games_in_pair) != {0, 1} or any(g["verdict"] not in ("TREATMENT_WIN", "CONTROL_WIN") for g in games_in_pair.values()):
            continue
        # Each game contains one seat per arm; win is binary. Side swap is receipt-verified above.
        wins = {}
        for idx, g in games_in_pair.items():
            wins[idx] = 1 if g["verdict"] == "TREATMENT_WIN" else 0
            for seat in g["seats"]:
                arm = seat["arm"]
                econ = seat["economy"]
                if arm not in ("control", "treatment") or econ["status"] != "RECORDED":
                    s["income"]["missing_seats"] += 1
                    continue
                for k in ("earned", "spent", "banked"):
                    s["income"][f"{arm}_{k}"] += econ[k]
                if econ.get("idle_queues") is not None:
                    s["income"][f"{arm}_idle_queues"] += econ["idle_queues"]
                    s["income"]["idle_queue_samples"] += 1
                events = econ.get("events")
                if events is not None:
                    s["event_metrics"]["status"] = "RECORDED"
                    for event in events:
                        kind = event["kind"]
                        s["event_metrics"]["counts"][kind] = s["event_metrics"]["counts"].get(kind, 0) + 1
                        s["event_metrics"]["first_tick"][kind] = min(event["tick"], s["event_metrics"]["first_tick"].get(kind, event["tick"]))
        s["complete_natural_pairs"] += 1
        s["pair_differences"].append((wins[0] + wins[1] - 1))  # paired treatment-minus-control win difference
    out = []
    for (switch, map_name), s in sorted(stratum.items()):
        vals = s.pop("pair_differences")
        n = len(vals)
        mean = sum(vals) / n if n else None
        # Paired bootstrap percentile interval, deterministic to make receipts reproducible.
        if n:
            rng = random.Random(f"{switch}:{map_name}:{n}")
            boot = sorted(sum(rng.choice(vals) for _ in range(n)) / n for _ in range(4000))
            ci = [boot[int(.025 * (len(boot) - 1))], boot[int(.975 * (len(boot) - 1))]]
        else:
            ci = None
        s["paired_win_effect"] = mean
        s["paired_bootstrap_95ci"] = ci
        s["underpowered"] = s["complete_natural_pairs"] < 20
        inc = s["income"]
        for arm in ("control", "treatment"):
            spent = inc[f"{arm}_spent"]
            inc[f"{arm}_earned_spent_ratio"] = inc[f"{arm}_earned"] / spent if spent else None
        out.append({"switch": switch, "map": map_name, **s})
    return {"schema": 1, "games": games, "strata": out,
            "note": "Only natural end pairs enter win effect; cap/incomplete/unknown remain separately counted. Seats are clustered within games."}


def load_receipts(paths):
    out = {}
    for path in paths:
        p = pathlib.Path(path)
        files = [p] if p.is_file() else sorted(p.rglob("cell_receipt.json"))
        for f in files:
            r = read_json(f, MAX_LINE)
            uid = r.get("game_uid")
            if not isinstance(uid, str) or uid in out:
                raise ValueError("missing/duplicate receipt game_uid")
            out[uid] = r
    return out


def dry_run(args):
    m = read_json(args.manifest)
    raw = args.manifest.read_bytes()
    actual_sha = hashlib.sha256(raw).hexdigest()
    require(actual_sha == args.manifest_sha256, "manifest file SHA mismatch")
    validate_manifest(m, args.repo_root, executable=False)
    jobs = make_jobs(m)
    # File/tree pins are checked, but the manifest must remain explicitly non-executable here.
    require(m.get("execution_approved") is False, "dry-run manifest must not authorize execution")
    require(math.isfinite(args.minutes_per_game) and args.minutes_per_game > 0, "minutes-per-game must be finite and positive")
    require(math.isfinite(args.setup_hours) and args.setup_hours >= 0, "setup-hours must be finite and nonnegative")
    result = {"mode": "NO_LAUNCH_DRY_RUN", "manifest_sha256": actual_sha,
              "source_commit": m["pins"]["source_commit"], "jobs": len(jobs),
              "games": 2 * 3 * 3 * m["pairs_per_stratum"], "pairs_per_switch_map": m["pairs_per_stratum"],
              "switches": list(SWITCHES), "maps": [x["name"] for x in m["map_strata"]],
              "first_jobs": jobs[:12], "wall_estimate_hours": estimate_hours(args.minutes_per_game, len(jobs), args.setup_hours),
              "launches": 0, "execution_authorized": False,
              "manifest_state": "fixture_only_not_campaign_executable",
              "blocking_gates": ["reviewed integration pin and executable/YAML/patch hashes not supplied",
                                 "seat-specific control/treatment bot binding and generated map.yaml producer not implemented",
                                 "run backend intentionally disabled in tooling-first revision"]}
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({k: result[k] for k in ("mode", "jobs", "games", "wall_estimate_hours", "launches", "execution_authorized")}))
    return 0


def estimate_hours(minutes_per_game: float, games: int = 360, setup_hours: float = 0.0):
    return round((minutes_per_game * games / 60) + setup_hours, 2)


def analyze(args):
    rows = _load_jsonl(args.matches)
    receipts = load_receipts(args.receipts)
    result = summarize(rows, receipts)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
    print(f"games={len(result['games'])} strata={len(result['strata'])} unknown={sum(s['unknown_games'] for s in result['strata'])}")
    return 0 if all(g["verdict"] != "INVALID_UNKNOWN" for g in result["games"]) else 2


def execute(args):
    m = read_json(args.manifest)
    raw_sha = hashlib.sha256(args.manifest.read_bytes()).hexdigest()
    require(raw_sha == args.manifest_sha256, "manifest file SHA mismatch")
    validate_manifest(m, args.repo_root, executable=True)
    require(args.execute is True, "--execute is required")
    require(not args.dry_run, "execution and dry-run are mutually exclusive")
    # This code path is intentionally disabled until the engine's per-cell driver and
    # memory watcher contract are independently tested and pinned by the lead manifest.
    require(m.get("driver_contract_version") == "ab-campaign-driver-v1", "unsupported driver contract")
    raise ValueError("campaign execution backend is not enabled in this tooling revision; no process started")


def main(argv=None):
    p = argparse.ArgumentParser(description=__doc__)
    sub = p.add_subparsers(dest="command", required=True)
    d = sub.add_parser("dry-run", help="validate frozen non-executable manifest and print deterministic 360-game plan")
    d.add_argument("--manifest", type=pathlib.Path, required=True)
    d.add_argument("--manifest-sha256", required=True)
    d.add_argument("--repo-root", type=pathlib.Path, required=True)
    d.add_argument("--output", type=pathlib.Path, required=True)
    d.add_argument("--minutes-per-game", type=float, default=8.5)
    d.add_argument("--setup-hours", type=float, default=5.0)
    d.set_defaults(fn=dry_run)
    a = sub.add_parser("analyze", help="adjudicate receipts + raw match rows")
    a.add_argument("--matches", nargs="+", required=True)
    a.add_argument("--receipts", nargs="+", required=True)
    a.add_argument("--output", type=pathlib.Path, required=True)
    a.set_defaults(fn=analyze)
    x = sub.add_parser("run", help="fail-closed execution gate")
    x.add_argument("--manifest", type=pathlib.Path, required=True)
    x.add_argument("--manifest-sha256", required=True)
    x.add_argument("--repo-root", type=pathlib.Path, required=True)
    x.add_argument("--execute", action="store_true")
    x.add_argument("--dry-run", action="store_true")
    x.set_defaults(fn=execute)
    args = p.parse_args(argv)
    try:
        return args.fn(args)
    except (OSError, ValueError, KeyError, TypeError, json.JSONDecodeError) as e:
        print(f"error: {e}", file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())
