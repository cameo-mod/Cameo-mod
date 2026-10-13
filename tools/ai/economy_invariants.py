#!/usr/bin/env python3
"""Mandatory economy checks with complete-tail validation and fixture provenance.

Only economic findings before the first terminal seat state are scored. Every
record, including the terminal tail, must still pass schema/census validation.
Fixture termination is accepted only when a SHA-pinned map Lua source contains
the scheduled primary-objective failure and its CapTick matches the terminal
tick within one health-pulse interval (50 ticks). Exit 0: observed healthy;
20: hard block; 21: unknown/incomplete evidence.
"""
import argparse
from collections import deque
import hashlib
import json
from pathlib import Path
import re

from replay_health import EvidenceError, atomic_report, identity, integer, read_jsonl

POLICY = "economy-invariants-v2"
FIXTURE_TICK_TOLERANCE = 50


def read_fixture_evidence(script_path, expected_sha256, tolerance=FIXTURE_TICK_TOLERANCE):
    """Verify a pinned cap Lua and extract its pre-registered objective tick."""
    path = Path(script_path)
    if not re.fullmatch(r"[0-9a-fA-F]{64}", str(expected_sha256 or "")):
        raise EvidenceError("missing/invalid fixture script SHA-256")
    if type(tolerance) is not int or tolerance < 0 or tolerance > FIXTURE_TICK_TOLERANCE:
        raise EvidenceError("invalid fixture tick tolerance")
    try:
        data = path.read_bytes()
    except OSError as error:
        raise EvidenceError("fixture script unavailable") from error
    digest = hashlib.sha256(data).hexdigest()
    if digest.lower() != expected_sha256.lower():
        raise EvidenceError("fixture script SHA-256 mismatch")
    try:
        text = data.decode("utf-8-sig")
    except UnicodeError as error:
        raise EvidenceError("fixture script is not UTF-8") from error

    cap = re.search(r"(?m)^\s*local\s+CapTick\s*=\s*(\d+)\s*$", text)
    callback = re.search(
        r"Trigger\.AfterDelay\s*\(\s*CapTick\s*,\s*function\s*\(\s*\)(.*?)\n\s*end\s*\)",
        text, re.DOTALL)
    if (cap is None or callback is None
            or not re.search(r"FailPrimaryObjectives\s*\(", callback.group(1))
            or not re.search(r"MarkFailedObjective\s*\(", text)):
        raise EvidenceError("fixture script lacks a verifiable objective terminal trigger")
    expected_tick = int(cap.group(1))
    if expected_tick <= 0:
        raise EvidenceError("invalid fixture CapTick")
    return {"verified": True, "expected_tick": expected_tick,
            "tolerance_ticks": tolerance, "trigger": "scheduled primary-objective failure",
            "source_path": str(path.resolve()), "source_sha256": digest}


def analyze(rows, game_uid, player, *, fixture_evidence=None):
    identity(game_uid)
    identity(player)
    selected = [r for r in rows if r.get("game_uid") == game_uid and r.get("player") == player]
    report = {"policy": POLICY, "status": "UNKNOWN", "game_uid": game_uid, "player": player,
              "findings": [], "diagnostics": [], "cash_band": {"low": 1000, "high": 10000,
              "band_ticks": 1500, "near_empty_max": 100, "near_empty_ticks": 250,
              "full_storage_ticks": 250},
              "scope": "economic findings are pre-terminal only; schema integrity covers the full stream"}
    if not selected:
        report["error"] = "Missing schema-2 economy health telemetry"
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
    pulses = 0
    float_diagnostic = False
    ended = False
    terminal = None
    previous_seat_state = None
    dropped_seen = False

    try:
        for row in selected:
            if ended:
                raise EvidenceError("record after terminal end record")
            if not isinstance(row, dict):
                raise EvidenceError("invalid health row")
            if type(row.get("schema")) is not int or row["schema"] != 2:
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
            if row.get("profile_supported") is not True:
                raise EvidenceError("unsupported profile")
            dropped = integer(row.get("dropped"))
            if dropped < 0:
                raise EvidenceError("invalid dropped telemetry count")
            if dropped > 0:
                dropped_seen = True
                defect("DROPPED_TELEMETRY", tick, {"dropped": dropped})

            kind = row.get("kind")
            if kind == "cancel":
                # Validate cancellation evidence even after terminal; only its
                # economic-window contribution is disabled there.
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
                if terminal is not None:
                    continue
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
                end_state = row.get("seat_state")
                if end_state not in ("won", "lost"):
                    raise EvidenceError("missing terminal seat state")
                if previous_seat_state not in (None, "active", end_state):
                    raise EvidenceError("terminal seat state disagrees with pulse evidence")
                if terminal is None:
                    terminal = {"tick": tick, "seat_state": end_state}
                elif end_state != terminal["seat_state"]:
                    raise EvidenceError("terminal seat state disagrees with pulse evidence")
                ended = True
                continue

            if kind != "pulse":
                raise EvidenceError("unknown health record kind")
            if last_pulse is None and tick != 0:
                raise EvidenceError("missing capture-start pulse at world tick zero")
            if last_pulse is not None and (tick <= last_pulse or tick - last_pulse > 50):
                raise EvidenceError("pulse coverage gap/duplicate")
            last_pulse = tick
            active = row.get("player_active")
            if type(active) is not bool:
                raise EvidenceError("missing player active state")
            seat_state = row.get("seat_state")
            if seat_state not in ("active", "won", "lost"):
                raise EvidenceError("missing/invalid seat state")
            if (seat_state == "active") != active:
                raise EvidenceError("player_active/seat_state disagreement")
            if previous_seat_state not in (None, "active") and seat_state != previous_seat_state:
                raise EvidenceError("seat state mutated after terminal")
            previous_seat_state = seat_state

            preterminal = terminal is None and seat_state == "active"
            if not preterminal and terminal is None:
                terminal = {"tick": tick, "seat_state": seat_state}
                cancel_windows.clear()

            # Full structural/census validation applies to every pulse, including
            # post-terminal pulses. Only the finding accumulation is windowed.
            queues = row.get("queues")
            if not isinstance(queues, list) or row.get("queues_complete") is not True:
                raise EvidenceError("incomplete building queue census")
            queue_ids = set()
            for queue in queues:
                if not isinstance(queue, dict):
                    raise EvidenceError("invalid queue state")
                queue_id = identity(queue.get("queue_id"))
                if queue_id in queue_ids:
                    raise EvidenceError("duplicate queue in pulse")
                queue_ids.add(queue_id)
                producer_live = queue.get("producer_live")
                if type(producer_live) is not bool:
                    raise EvidenceError("missing live producer state")
                state = queue.get("state")
                if state not in ("ready", "idle", "producing", "paused"):
                    raise EvidenceError("invalid queue state")
                since = integer(queue.get("state_since_tick"))
                if since > tick:
                    raise EvidenceError("queue state starts in future")
                reason = identity(queue.get("reason"))
                if state == "ready":
                    item = identity(queue.get("item"))
                    identity(queue.get("item_id"))
                    if preterminal and active and producer_live and tick - since >= 250:
                        defect("READY_BUILDING_UNPLACED", tick, {"queue_id": queue_id,
                               "item": item, "age_ticks": tick - since, "reason": reason})
                if state == "idle" and preterminal and active and producer_live and tick - since >= 1500:
                    defect("BUILDING_QUEUE_IDLE", tick, {"queue_id": queue_id,
                           "age_ticks": tick - since, "reason": reason})

            cash, resources, capacity = (integer(row.get(k)) for k in ("cash", "resources", "capacity"))
            net_spent = row.get("net_spent")
            if type(net_spent) is not int:
                raise EvidenceError("missing/invalid signed net spend")
            if ("gross_spent" not in row or row["gross_spent"] is not None
                    or row.get("gross_spend_complete") is not False):
                raise EvidenceError("unsupported gross spending evidence")
            if resources > capacity:
                raise EvidenceError("invalid storage")
            report["spending"] = {"net_spent": net_spent, "gross_spent": None,
                                  "gross_spend_complete": False, "tick": tick}
            if not preterminal:
                continue

            pulses += 1
            funds = cash + resources
            flags = {"low": active and funds < 1000, "high": active and funds > 10000,
                     "near_empty": active and funds <= 100,
                     "full": active and capacity > 0 and resources == capacity}
            for name, flag in flags.items():
                if not flag:
                    balances[name] = None
                    continue
                if balances[name] is None:
                    balances[name] = tick
                duration = tick - balances[name]
                threshold = 250 if name in ("near_empty", "full") else 1500
                if duration >= threshold:
                    code = {"low": "FUNDS_BELOW_BAND", "high": "FUNDS_ABOVE_BAND",
                            "near_empty": "NEAR_EMPTY_FUNDS", "full": "STORAGE_FULL"}[name]
                    defect(code, tick, {"duration_ticks": duration, "funds": funds,
                                       "resources": resources, "capacity": capacity})
                    if name == "high" and not float_diagnostic:
                        float_diagnostic = True
                        report["diagnostics"].append({"code": "CASH_FLOAT_SPENDING_UNVERIFIED",
                            "severity": "DIAGNOSTIC", "tick": tick,
                            "duration_ticks": duration, "gross_spent": None,
                            "reason": "net spend can decrease on refunds; low spending not established"})

        if not ended:
            raise EvidenceError("no complete terminal health record; live use requires shared watermark")
        if terminal is None:
            raise EvidenceError("no terminal seat-state evidence")
        if not pulses:
            raise EvidenceError("no pre-terminal pulse observations")
        if fixture_evidence is None:
            raise EvidenceError("missing verified fixture terminal provenance")
        if (not isinstance(fixture_evidence, dict) or fixture_evidence.get("verified") is not True
                or type(fixture_evidence.get("expected_tick")) is not int
                or type(fixture_evidence.get("tolerance_ticks")) is not int
                or not re.fullmatch(r"[0-9a-f]{64}", str(fixture_evidence.get("source_sha256", "")))):
            raise EvidenceError("invalid fixture terminal provenance")
        delta = abs(terminal["tick"] - fixture_evidence["expected_tick"])
        terminal_kind = ("FIXTURE_TERMINAL"
                         if delta <= fixture_evidence["tolerance_ticks"] else "GAME_TERMINAL")
        report["terminal"] = {"kind": terminal_kind, **terminal,
                              "fixture_expected_tick": fixture_evidence["expected_tick"],
                              "fixture_tick_delta": delta,
                              "fixture_source_sha256": fixture_evidence["source_sha256"],
                              "fixture_source_path": fixture_evidence.get("source_path"),
                              "fixture_tolerance_ticks": fixture_evidence["tolerance_ticks"]}
        report["defeated"] = terminal["seat_state"] == "lost"
        report["status"] = "BLOCK" if report["findings"] else "OBSERVED_HEALTHY"
        report["pulses"] = pulses
    except (EvidenceError, TypeError, KeyError) as error:
        report["error"] = str(error)
        if dropped_seen:
            report["status"] = "BLOCK"
    return report


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("support", type=Path)
    parser.add_argument("--game-uid", required=True)
    parser.add_argument("--player", required=True)
    parser.add_argument("--output", type=Path)
    parser.add_argument("--fixture-script", type=Path,
                        help="map Lua source containing the pre-registered objective terminal")
    parser.add_argument("--fixture-script-sha256",
                        help="pre-registered exact SHA-256 of --fixture-script")
    parser.add_argument("--fixture-tick-tolerance", type=int, default=FIXTURE_TICK_TOLERANCE)
    args = parser.parse_args()
    try:
        rows, receipt = read_jsonl(args.support / "Logs/cameo-ai-economy-health.jsonl")
        evidence = None
        if args.fixture_script is not None or args.fixture_script_sha256 is not None:
            if args.fixture_script is None or args.fixture_script_sha256 is None:
                raise EvidenceError("fixture script and pre-registered SHA must be supplied together")
            evidence = read_fixture_evidence(args.fixture_script, args.fixture_script_sha256,
                                             args.fixture_tick_tolerance)
        report = analyze(rows, args.game_uid, args.player, fixture_evidence=evidence)
        report["input"] = receipt
    except (EvidenceError, OSError, ValueError, TypeError) as error:
        report = {"policy": POLICY, "status": "UNKNOWN", "error": str(error)}
    if args.output:
        try:
            atomic_report(args.output, json.dumps(report, allow_nan=False, indent=2) + "\n", args.support)
        except (EvidenceError, OSError, ValueError, TypeError) as error:
            report = {"policy": POLICY, "status": "UNKNOWN", "error": str(error)}
    print(json.dumps(report, allow_nan=False, indent=2))
    return {"BLOCK": 20, "UNKNOWN": 21, "OBSERVED_HEALTHY": 0}.get(report["status"], 21)


if __name__ == "__main__":
    raise SystemExit(main())
