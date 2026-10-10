#!/usr/bin/env python3
"""Bounded combat evidence transport preflight; never a campaign stop gate.

This checkpoint validates transport coverage only. It deliberately returns
UNKNOWN until eligibility, response and launch semantics have reviewed readers.
"""
import argparse
import hashlib
import json
from pathlib import Path

MAX_BYTES = 128 * 1024 * 1024
MAX_LINE = 65536
MAX_ROWS = 200000
KINDS = {"start", "eligibility_transition", "restraint_transition",
         "dispatch_intent", "dispatch_observed", "pulse", "end"}
IDENTITY = ("game_uid", "player", "map_uid", "faction", "profile")
CHANNELS = ("roster_complete", "eligibility_complete", "dispatch_complete")


def unique_object(pairs):
    result = {}
    for key, value in pairs:
        if key in result:
            raise ValueError("duplicate JSON key")
        result[key] = value
    return result


def reject_constant(value):
    raise ValueError("non-finite JSON number: " + value)


def analyze(path, game_uid, player):
    path = Path(path)
    report = {"schema": "cameo-combat-liveness-report", "schema_version": 1,
              "status": "UNKNOWN", "diagnostic_only": True,
              "coverage": "UNKNOWN", "dispatch_causality": "UNKNOWN",
              "findings": [], "unknown": [], "input": {"path": str(path)}}
    try:
        if not game_uid or not player:
            raise ValueError("missing requested participant identity")
        if path.stat().st_size > MAX_BYTES:
            raise ValueError("file byte bound")
        with path.open("rb") as source:
            data = source.read(MAX_BYTES + 1)
        report["input"]["consumed_sha256"] = hashlib.sha256(data).hexdigest()
        if not data or len(data) > MAX_BYTES or not data.endswith(b"\n"):
            raise ValueError("empty, oversized or unterminated input")
        lines = data.splitlines(keepends=True)
        if len(lines) > MAX_ROWS:
            raise ValueError("row bound")
        rows = []
        for line in lines:
            if len(line) > MAX_LINE:
                raise ValueError("line bound")
            row = json.loads(line.decode("utf-8"), object_pairs_hook=unique_object,
                             parse_constant=reject_constant)
            if not isinstance(row, dict) or row.get("schema") != "cameo-combat-liveness" or type(row.get("schema_version")) is not int or row["schema_version"] != 1:
                raise ValueError("unsupported schema")
            if any(not isinstance(row.get(key), str) or not row[key] or len(row[key]) > 1024 for key in IDENTITY):
                raise ValueError("missing or oversized identity")
            if row["game_uid"] == game_uid and row["player"] == player:
                rows.append(row)
        if not rows:
            raise ValueError("missing requested participant")
        identity = tuple(rows[0][key] for key in IDENTITY)
        last_tick = last_pulse = -1
        ended = False
        incomplete = set()
        for seq, row in enumerate(rows):
            tick, kind = row.get("tick"), row.get("kind")
            if tuple(row[key] for key in IDENTITY) != identity:
                raise ValueError("participant identity drift")
            if type(row.get("seq")) is not int or row["seq"] != seq or type(tick) is not int or tick < 0 or tick < last_tick or ended:
                raise ValueError("sequence, tick or terminal ordering")
            if kind not in KINDS or (seq == 0 and (kind != "start" or tick != 0)) or (seq > 0 and kind == "start"):
                raise ValueError("missing tick-zero start or invalid kind")
            if last_pulse >= 0 and tick - last_pulse > 50:
                raise ValueError("pulse gap")
            if kind in {"start", "pulse", "end"}:
                last_pulse = tick
            if type(row.get("dropped")) is not int or row["dropped"] < 0:
                raise ValueError("invalid dropped count")
            if row["dropped"]:
                incomplete.add("dropped evidence")
            for channel in CHANNELS:
                if type(row.get(channel)) is not bool:
                    raise ValueError("missing channel watermark: " + channel)
                if not row[channel]:
                    incomplete.add(channel)
            if kind == "end":
                if type(row.get("complete")) is not bool:
                    raise ValueError("missing terminal completeness")
                if not row["complete"]:
                    incomplete.add("incomplete terminal")
                ended = True
            last_tick = tick
        if not ended:
            raise ValueError("missing terminal")
        report["unknown"].extend(sorted(incomplete))
        if not incomplete:
            report["coverage"] = "TRANSPORT_COMPLETE"
        report["rows"] = len(rows)
        report["last_tick"] = last_tick
        report["unknown"].append("eligibility/response/launch semantic assessment not implemented")
    except (OSError, ValueError, TypeError, RecursionError) as error:
        report["unknown"].append(str(error))
    return report


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--logs", type=Path, required=True)
    parser.add_argument("--game-uid", required=True)
    parser.add_argument("--player", required=True)
    args = parser.parse_args()
    report = analyze(args.logs / "cameo-ai-combat-liveness.jsonl", args.game_uid, args.player)
    print(json.dumps(report, sort_keys=True))
    return 21  # Transport completeness is never health/adoption approval.


if __name__ == "__main__":
    raise SystemExit(main())
