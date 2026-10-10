#!/usr/bin/env python3
"""Diagnostic-only MCV evidence reader. Never an economy BLOCK20 gate."""
import argparse
import hashlib
import json
from pathlib import Path

ROLES = {"construction_mcv", "field_refinery_vehicle", "base_building_vehicle"}
ACTIVITIES = {"unknown", "idle", "busy", "mobile", "deploying", "deployed"}
HOLDS = {"unknown", "none", "policy_hold", "lease_conflict", "no_legal_site", "no_path", "disabled"}
MAX_BYTES, MAX_LINE, MAX_ROWS = 128 * 1024 * 1024, 65536, 200000


def integer(value):
    return type(value) is int


def unique_object(pairs):
    result = {}
    for key, value in pairs:
        if key in result:
            raise ValueError("duplicate JSON key")
        result[key] = value
    return result


def analyze(path, game_uid, player, idle_ticks=None):
    report = {"schema": "cameo-mcv-health-report", "schema_version": 1,
              "status": "UNKNOWN", "diagnostic_only": True, "findings": [],
              "unknown": [], "input": {"path": str(path)}, "idle_ticks": idle_ticks}
    try:
        if idle_ticks is not None and (not integer(idle_ticks) or idle_ticks <= 0):
            raise ValueError("invalid explicit idle threshold")
        if path.stat().st_size > MAX_BYTES:
            raise ValueError("file byte bound")
        with path.open("rb") as stream:
            data = stream.read(MAX_BYTES + 1)
        report["input"]["consumed_sha256"] = hashlib.sha256(data).hexdigest()
        if len(data) > MAX_BYTES or not data or not data.endswith(b"\n"):
            raise ValueError("empty, oversized or unterminated input")
        lines = data.splitlines(keepends=True)
        if len(lines) > MAX_ROWS:
            raise ValueError("row bound")
        rows = []
        for line in lines:
            if len(line) > MAX_LINE:
                raise ValueError("line bound")
            row = json.loads(line, object_pairs_hook=unique_object)
            if not isinstance(row, dict) or row.get("schema") != "cameo-mcv-health" or type(row.get("schema_version")) is not int or row["schema_version"] != 1:
                raise ValueError("unsupported schema")
            if row.get("game_uid") == game_uid and row.get("player") == player:
                rows.append(row)
        if not rows:
            raise ValueError("missing requested participant")
        last_tick = last_pulse = -1
        ended = False
        seen_idle = set()
        for seq, row in enumerate(rows):
            tick, kind = row.get("world_tick"), row.get("kind")
            if not integer(row.get("seq")) or row["seq"] != seq or not integer(tick) or tick < 0 or tick < last_tick or ended:
                raise ValueError("sequence, tick or terminal ordering")
            if kind not in {"start", "transition", "pulse", "end"} or (seq == 0 and (kind != "start" or tick != 0)) or (seq > 0 and kind == "start"):
                raise ValueError("missing tick-zero census or invalid kind")
            if kind in {"start", "pulse", "end"}:
                if last_pulse >= 0 and tick - last_pulse > 50:
                    raise ValueError("pulse gap")
                last_pulse = tick
            elif last_pulse < 0 or tick - last_pulse > 50:
                raise ValueError("pulse gap")
            last_tick = tick
            for key in ("player_active", "supported", "observation_complete", "intent_complete", "order_complete", "hold_complete", "transform_complete", "complete"):
                if type(row.get(key)) is not bool:
                    raise ValueError("missing boolean evidence: " + key)
            if not integer(row.get("dropped")) or row["dropped"] != 0 or not row["supported"] or not row["observation_complete"]:
                report["unknown"].append("unsupported or incomplete census")
            if not all(row[key] for key in ("intent_complete", "order_complete", "hold_complete", "transform_complete")):
                report["unknown"].append("order/hold/transform evidence unavailable")
            role_map = row.get("role_map")
            actors = row.get("actors")
            if not isinstance(role_map, dict) or not role_map or len(role_map) > 128 or not isinstance(actors, list) or len(actors) > 64:
                raise ValueError("missing or oversized role census")
            if any(role not in ROLES for role in role_map.values()):
                report["unknown"].append("unresolved role mapping")
            ids = set()
            for actor in actors:
                if not isinstance(actor, dict):
                    raise ValueError("invalid actor record")
                aid = actor.get("actor_id")
                if not integer(aid) or aid <= 0 or aid in ids:
                    raise ValueError("invalid/duplicate actor identity")
                ids.add(aid)
                if actor.get("activity") not in ACTIVITIES or actor.get("hold") not in HOLDS:
                    raise ValueError("unsupported activity/hold code")
                if actor.get("role") not in ROLES or role_map.get(actor.get("actor_type")) != actor.get("role"):
                    report["unknown"].append("unresolved actor role")
                for key in ("live", "in_world"):
                    if type(actor.get(key)) is not bool:
                        raise ValueError("missing actor liveness")
                if actor.get("observation_tick") != tick or not integer(actor.get("last_cell_change_tick")) or not 0 <= actor["last_cell_change_tick"] <= tick:
                    raise ValueError("invalid observation/progress tick")
                if not integer(actor.get("x")) or not integer(actor.get("y")):
                    raise ValueError("missing cell")
                since = actor.get("idle_since_tick")
                if since is not None and (not integer(since) or not 0 <= since <= tick or actor["activity"] != "idle"):
                    raise ValueError("invalid idle clock")
                if actor["activity"] == "idle" and actor["live"] and actor["in_world"] and since is None:
                    report["unknown"].append("missing idle age")
                if actor["hold"] == "unknown":
                    report["unknown"].append("hold reason unavailable")
                for key in ("order_requested", "order_accepted", "site_intent"):
                    if actor.get(key) is not None and type(actor[key]) is not bool:
                        raise ValueError("unsupported order/intent evidence")
                if actor["activity"] == "deployed" and (not integer(actor.get("proven_transform_actor_id")) or actor["proven_transform_actor_id"] <= 0):
                    report["unknown"].append("deployment outcome unproven")
                if idle_ticks is not None and row["player_active"] and actor["live"] and actor["in_world"] and since is not None and tick - since >= idle_ticks:
                    key = (aid, since)
                    if key not in seen_idle:
                        seen_idle.add(key)
                        report["findings"].append({"code": "PERSISTENT_IDLE_OBSERVED", "actor_id": aid,
                                                   "since_tick": since, "observed_tick": tick,
                                                   "hold": actor["hold"], "causal_conclusion": None})
            if kind == "end":
                ended = True
                if not row["complete"]:
                    report["unknown"].append("terminal evidence incomplete")
        if not ended:
            raise ValueError("missing terminal watermark")
        report["unknown"] = sorted(set(report["unknown"]))
        if not report["unknown"]:
            report["status"] = "DIAGNOSTIC" if report["findings"] else "NO_OBSERVED_SYMPTOM"
    except (OSError, UnicodeError, json.JSONDecodeError, ValueError, TypeError, KeyError) as exc:
        report["unknown"].append(str(exc))
    return report


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("support", type=Path)
    parser.add_argument("--game-uid", required=True)
    parser.add_argument("--player", required=True)
    parser.add_argument("--idle-ticks", type=int, help="explicit diagnostic experiment threshold; no default")
    args = parser.parse_args()
    report = analyze(args.support / "Logs" / "cameo-ai-mcv-health.jsonl", args.game_uid, args.player, args.idle_ticks)
    print(json.dumps(report, sort_keys=True))
    return 21 if report["status"] == "UNKNOWN" else 0


if __name__ == "__main__":
    raise SystemExit(main())
