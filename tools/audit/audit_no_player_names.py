#!/usr/bin/env python3
"""Check emitted AI JSONL and learned data against the anonymous-seat grammar.

The grammar fixes identity-bearing descriptors to seat_N and rejects unknown
descriptor shapes; it does not scan for a list of forbidden words.
"""
from __future__ import annotations

import argparse
import json
import re
import sys
from pathlib import Path

SEAT = re.compile(r"seat_[1-9][0-9]*\Z")
MATCH_FIELDS = {"schema", "record_id", "recorded_utc", "mod_version", "game_uid", "map_uid", "map_title",
                "duration_ticks", "timestep", "player", "takeover", "stats", "arsenal", "ownership",
                "order_gate", "opponents", "allies", "seats", "opponent_signatures"}
SEAT_FIELDS = {"seat", "faction", "home"}
RELATION_FIELDS = SEAT_FIELDS | {"bot_type", "team", "handicap", "outcome"}
PLAYER_FIELDS = {"seat", "bot_type", "faction", "team", "handicap", "spawn", "home", "outcome",
                 "personality", "personality_switches", "personality_timeline", "composition",
                 "composition_switches", "composition_timeline", "episode_timeline", "priors_state"}
SITUATION_FIELDS = {"schema", "kind", "record_id", "game_uid", "map_uid", "seat", "faction", "bot_type", "tick",
                    "urgency", "personality_current", "personality_candidate", "main_target", "main_target_score",
                    "mission", "mission_assignment", "hints", "demand", "own", "enemies", "threats", "scale_targets",
                    "build_order", "contacts", "bandit", "expansion"}
PLACEMENT_FIELDS = {"schema", "kind", "game_uid", "record_id", "map_uid", "seed", "seat", "faction", "bot_type",
                    "personality", "tick", "queued_tick", "placed_tick", "actor", "cell", "category", "reason",
                    "anchor_kind", "anchor_cell", "anchor_dist", "field_id", "cause", "front_back_class", "front_back_pick"}
MISSION_FIELDS = {"schema", "recorded_utc", "game_uid", "map_uid", "map_title", "seat", "faction", "bot",
                  "mission_id", "record_kind", "event", "attempt", "attempt_id", "state", "terminal", "reason",
                  "by", "tick", "type", "region", "target_cell", "unit_cell", "units", "value"}


def descriptor_ok(obj: object, allowed: set[str], required: set[str] = SEAT_FIELDS) -> bool:
    return isinstance(obj, dict) and required <= obj.keys() and obj.keys() <= allowed and SEAT.fullmatch(str(obj.get("seat", ""))) is not None


def match_ok(row: dict) -> bool:
    if row.get("schema") != 3 or row.keys() - MATCH_FIELDS:
        return False
    if not isinstance(row.get("record_id"), str) or not re.fullmatch(r"[^|]+\|seat_[1-9][0-9]*", row["record_id"]):
        return False
    if not isinstance(row.get("player"), dict) or row["player"].keys() - PLAYER_FIELDS or not SEAT.fullmatch(str(row["player"].get("seat", ""))):
        return False
    if any(not descriptor_ok(x, RELATION_FIELDS) for x in row.get("seats", [])):
        return False
    for key in ("opponents", "allies"):
        if any(not descriptor_ok(x, RELATION_FIELDS) for x in row.get(key, [])):
            return False
    for x in row.get("opponent_signatures", []):
        if not isinstance(x, dict) or x.keys() != {"seat", "seen", "truth"} or not SEAT.fullmatch(str(x["seat"])):
            return False
        seen_fields = {"faction", "army_value", "infantry_value", "vehicle_value", "air_value", "naval_value",
                       "defence_value", "building_count", "harvester_count", "known_regions", "last_seen_tick"}
        if x["seen"] is not None and (not isinstance(x["seen"], dict) or x["seen"].keys() - seen_fields):
            return False
        if not isinstance(x["truth"], dict) or x["truth"].keys() != {"faction", "outcome"}:
            return False
    return True


def validate_logs(paths: list[Path]) -> list[str]:
    errors = []
    for path in paths:
        if not path.is_file():
            continue
        for n, line in enumerate(path.read_text(encoding="utf-8").splitlines(), 1):
            if not line.strip():
                continue
            try:
                row = json.loads(line)
            except json.JSONDecodeError:
                errors.append(f"{path}:{n}: invalid JSON")
                continue
            schema = row.get("schema") if isinstance(row, dict) else None
            if not isinstance(row, dict):
                ok = False
            elif schema == 3 and "player" in row:
                ok = match_ok(row)
            elif schema == 3 and row.get("kind") == "situation":
                ok = row.keys() <= SITUATION_FIELDS and SEAT.fullmatch(str(row.get("seat", ""))) is not None
            elif schema == 2 and row.get("kind") in {"placement", "refinery_lost", "refinery_acquired"}:
                ok = row.keys() <= PLACEMENT_FIELDS and SEAT.fullmatch(str(row.get("seat", ""))) is not None
            elif schema == "mission-card/2":
                ok = row.keys() <= MISSION_FIELDS and SEAT.fullmatch(str(row.get("seat", ""))) is not None
            elif schema == "engagement/2":
                ok = ("seat" in row and SEAT.fullmatch(str(row.get("seat"))) is not None
                      and "player" not in row and "name" not in row and "controller_client" not in row
                      and isinstance(row.get("record_id"), str)
                      and re.fullmatch(r"[^|]+\|seat_[1-9][0-9]*\|.+", row["record_id"]) is not None)
            else:
                ok = False
            if not ok:
                errors.append(f"{path}:{n}: record does not conform to anonymous-seat schema")
    return errors


def learned_paths(root: Path) -> list[Path]:
    return sorted((root / "mods/cameo/ai/learned").glob("*.yaml"))


def validate_learned(paths: list[Path]) -> list[str]:
    errors = []
    roots = {"arsenal_priors.yaml": "BotArsenalPriors:", "build_order_knobs.yaml": "BotBuildOrderKnobs:",
             "plan_bandits.yaml": "BotPlanBandits:", "opponent_signatures.yaml": 'schema: "cameo-opponent-signatures/1"'}
    for path in paths:
        text = path.read_text(encoding="utf-8")
        first = next((line.strip() for line in text.splitlines() if line.strip() and not line.lstrip().startswith("#")), "")
        if path.name not in roots or first != roots[path.name]:
            errors.append(f"{path}: unknown learned-file grammar")
        if path.name == "opponent_signatures.yaml":
            accepted = re.compile(r'^(?:schema: "cameo-opponent-signatures/1"|source_schema: 3|clusters:|  \[\]|'
                                  r'  - id: "sig_[0-9]{4}"|    faction: "[A-Za-z0-9_.-]+"|    count: [0-9]+|'
                                  r'    bands: \[[0-9, ]*\]|    center:|      [a-z_]+: -?[0-9]+)$')
            if any(line.strip() and not line.lstrip().startswith("#") and not accepted.fullmatch(line)
                   for line in text.splitlines()):
                errors.append(f"{path}: learned signature does not conform to its field grammar")
    return errors


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--repo", type=Path, default=Path(__file__).resolve().parents[2])
    ap.add_argument("--logs", type=Path, nargs="*", default=[])
    args = ap.parse_args()
    logs = list(args.logs)
    errors = validate_logs(logs) + validate_learned(learned_paths(args.repo))
    if errors:
        print("\n".join(errors), file=sys.stderr)
        return 1
    print(f"anonymous log grammar: PASS ({len(logs)} log inputs, {len(learned_paths(args.repo))} learned files)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
