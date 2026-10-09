#!/usr/bin/env python3
"""Mandatory economy defect checks over complete schema-1 health telemetry.

No game launches or source repairs. Exit 0: observed checks passed; 20: defect
review hold; 21: unsupported/incomplete evidence. Cash thresholds provisional.
"""
import argparse
from collections import deque
import json
from pathlib import Path

from replay_health import EvidenceError, atomic_report, identity, integer, read_jsonl

POLICY = "economy-invariants-v1"


def analyze(rows, game_uid, player):
    identity(game_uid)
    identity(player)
    selected = [r for r in rows if r.get("game_uid") == game_uid and r.get("player") == player]
    report = {"policy": POLICY, "status": "UNKNOWN", "game_uid": game_uid, "player": player,
              "findings": [], "cash_band": {"low": 1000, "high": 10000,
              "band_ticks": 1500, "near_empty_max": 100, "near_empty_ticks": 250,
              "full_storage_ticks": 250},
              "scope": "economy defects; reasons are diagnostic, not causal proof"}
    if not selected:
        report["error"] = "Missing schema-1 economy health telemetry"
        return report
    seen = set()
    def defect(code, tick, details):
        key = (code, details.get("queue_id"), details.get("item"))
        if key not in seen:
            seen.add(key)
            report["findings"].append({"code": code, "severity": "DEFECT", "tick": tick, **details})
    sequence, previous_tick, last_pulse = 0, -1, None
    identity_tuple = None
    cancel_windows = {}
    cancelled_ids = set()
    balances = {name: None for name in ("low", "high", "near_empty", "full")}
    last_spent, high_spent, pulses = None, None, 0
    ended = False
    try:
        for row in selected:
            if ended:
                raise EvidenceError("record after terminal health event")
            if type(row.get("schema")) is not int or row["schema"] != 1:
                raise EvidenceError("unsupported health schema")
            if integer(row.get("seq")) != sequence:
                raise EvidenceError("health sequence missing/duplicate")
            sequence += 1
            tick = integer(row.get("tick"))
            if tick < previous_tick:
                raise EvidenceError("health tick decreased")
            previous_tick = tick
            key = tuple(identity(row.get(k)) for k in ("map_uid", "faction", "profile"))
            if identity_tuple is not None and identity_tuple != key:
                raise EvidenceError("health identity drift")
            identity_tuple = key
            if row.get("profile_supported") is not True or integer(row.get("dropped")) != 0:
                raise EvidenceError("unsupported profile or dropped telemetry")
            kind = row.get("kind")
            if kind == "cancel":
                queue = identity(row.get("queue_id"))
                item = identity(row.get("item"))
                item_id = identity(row.get("item_id"))
                if (queue, item_id) in cancelled_ids:
                    raise EvidenceError("duplicate cancellation of one production item")
                cancelled_ids.add((queue, item_id))
                reason = identity(row.get("reason"))
                active = row.get("player_active")
                live = row.get("producer_live")
                cancellation_class = row.get("cancellation_class")
                if type(active) is not bool or type(live) is not bool:
                    raise EvidenceError("missing cancellation activity/liveness evidence")
                if cancellation_class not in ("production", "destruction", "elimination"):
                    raise EvidenceError("missing/invalid cancellation class")
                # Event-local evidence is authoritative; a pulse may precede elimination.
                if not active or cancellation_class == "elimination":
                    cancel_windows.clear()
                    continue
                if not live or cancellation_class == "destruction":
                    continue
                window = cancel_windows.setdefault(item, deque())
                window.append(tick)
                while window and tick - window[0] > 1500:
                    window.popleft()
                if len(window) >= 3:
                    defect("REPEATED_BUILDING_CANCELLATION", tick,
                           {"queue_id": queue, "item": item, "count": len(window),
                            "window_ticks": 1500, "last_reason": reason})
                continue
            if kind == "end":
                if row.get("complete") is not True:
                    raise EvidenceError("incomplete terminal health record")
                if last_pulse is None or tick - last_pulse > 50:
                    raise EvidenceError("terminal health watermark not covered by pulse")
                ended = True
                continue
            if kind != "pulse":
                raise EvidenceError("unknown health record kind")
            if last_pulse is not None and (tick <= last_pulse or tick - last_pulse > 50):
                raise EvidenceError("pulse coverage gap/duplicate")
            last_pulse, pulses = tick, pulses + 1
            queues = row.get("queues")
            if not isinstance(queues, list) or row.get("queues_complete") is not True:
                raise EvidenceError("incomplete building queue census")
            queue_ids = set()
            active = row.get("player_active")
            if type(active) is not bool:
                raise EvidenceError("missing player active state")
            if not active:
                cancel_windows.clear()
            for queue in queues:
                if not isinstance(queue, dict):
                    raise EvidenceError("invalid queue state")
                queue_id = identity(queue.get("queue_id"))
                if queue_id in queue_ids:
                    raise EvidenceError("duplicate queue in pulse")
                queue_ids.add(queue_id)
                if type(queue.get("producer_live")) is not bool:
                    raise EvidenceError("missing live producer state")
                state = queue.get("state")
                if state not in ("ready", "idle", "producing", "paused"):
                    raise EvidenceError("invalid queue state")
                since = integer(queue.get("state_since_tick"))
                if since > tick:
                    raise EvidenceError("queue state starts in future")
                reason = identity(queue.get("reason"))
                if state == "ready":
                    identity(queue.get("item"))
                    identity(queue.get("item_id"))
                    if active and queue["producer_live"] and tick - since >= 250:
                        defect("READY_BUILDING_UNPLACED", tick, {"queue_id": queue_id,
                               "item": queue["item"], "age_ticks": tick-since, "reason": reason})
                if state == "idle" and active and queue["producer_live"] and tick - since >= 1500:
                    defect("BUILDING_QUEUE_IDLE", tick, {"queue_id": queue_id,
                           "age_ticks": tick-since, "reason": reason})
            cash, resources, capacity, spent = (integer(row.get(k)) for k in
                                               ("cash", "resources", "capacity", "spent"))
            if resources > capacity or (last_spent is not None and spent < last_spent):
                raise EvidenceError("invalid storage/cumulative spend")
            last_spent = spent
            funds = cash + resources
            flags = {"low": active and funds < 1000, "high": active and funds > 10000,
                     "near_empty": active and funds <= 100,
                     "full": active and capacity > 0 and resources == capacity}
            for name, flag in flags.items():
                if not flag:
                    balances[name] = None
                    if name == "high":
                        high_spent = None
                    continue
                if balances[name] is None:
                    balances[name] = tick
                    if name == "high":
                        high_spent = spent
                duration = tick - balances[name]
                threshold = 250 if name in ("near_empty", "full") else 1500
                if duration >= threshold:
                    code = {"low": "FUNDS_BELOW_BAND", "high": "FUNDS_ABOVE_BAND",
                            "near_empty": "NEAR_EMPTY_FUNDS", "full": "STORAGE_FULL"}[name]
                    defect(code, tick, {"duration_ticks": duration, "funds": funds,
                                       "resources": resources, "capacity": capacity})
                    if name == "high" and spent - high_spent <= 1000:
                        defect("CASH_FLOAT_WITH_LOW_SPENDING", tick,
                               {"duration_ticks": duration, "spend_delta": spent-high_spent})
        if not ended:
            raise EvidenceError("no complete terminal health record; live use requires shared watermark")
        if not pulses:
            raise EvidenceError("no pulse observations")
        report["status"] = "BLOCK" if report["findings"] else "OBSERVED_HEALTHY"
        report["pulses"] = pulses
    except (EvidenceError, TypeError, KeyError) as error:
        report["error"] = str(error)
    return report


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("support", type=Path)
    parser.add_argument("--game-uid", required=True)
    parser.add_argument("--player", required=True)
    parser.add_argument("--output", type=Path)
    args = parser.parse_args()
    try:
        rows, receipt = read_jsonl(args.support / "Logs/cameo-ai-economy-health.jsonl")
        report = analyze(rows, args.game_uid, args.player)
        report["input"] = receipt
        if args.output:
            atomic_report(args.output, json.dumps(report, allow_nan=False, indent=2) + "\n", args.support)
    except (EvidenceError, OSError, ValueError, TypeError) as error:
        report = {"policy": POLICY, "status": "UNKNOWN", "error": str(error)}
    print(json.dumps(report, allow_nan=False, indent=2))
    return {"BLOCK": 20, "UNKNOWN": 21}.get(report["status"], 0)


if __name__ == "__main__":
    raise SystemExit(main())
