#!/usr/bin/env python3
"""Offline/live-prefix health triage. Does not launch games or modify a campaign.

Exit 0 = no structural stop signal in the supported evidence; 20 = BLOCK;
21 = UNKNOWN/incomplete evidence. Warnings and losing matches do not cause BLOCK.
This is a symptom detector, not proof of a specific source bug or gameplay approval.
"""
from __future__ import annotations

import argparse
from collections import deque
import hashlib
import json
import os
from pathlib import Path
import tempfile

POLICY_VERSION = "startup-economy-v1"
PROFILES = {"td_gdi", "td_nod", "ra1_allies", "ra1_soviets", "japan"}
MAX_FILE_BYTES = 128 * 1024 * 1024
MAX_LINE_BYTES = 1024 * 1024
MAX_ROWS = 200000
REFERENCE_BOTS = {"classic", "classic_hard"}


class EvidenceError(ValueError):
    pass


def strict_json(raw):
    def unique(pairs):
        result = {}
        for key, value in pairs:
            if key in result:
                raise EvidenceError(f"duplicate JSON key: {key}")
            result[key] = value
        return result
    def invalid_constant(value):
        raise EvidenceError(f"non-finite JSON constant: {value}")
    try:
        return json.loads(raw, object_pairs_hook=unique, parse_constant=invalid_constant)
    except RecursionError as error:
        raise EvidenceError("JSON nesting exceeds parser depth limit") from error


def identity(value):
    if not isinstance(value, str) or not value.strip():
        raise EvidenceError("identity must be a nonempty string")
    return value


def atomic_report(path, encoded, support):
    """Publish a new report atomically; never overwrite any existing artifact."""
    target = path.resolve()
    source = support.resolve()
    for directory in (source / "Logs", source / "Replays"):
        if target.is_relative_to(directory.resolve()):
            raise EvidenceError("report output cannot be inside capture Logs/Replays")
    if target == (source / "batch_summary.json").resolve() or path.exists() or path.is_symlink():
        raise EvidenceError("report output must be a new file, not an existing capture/report")
    temp_path = None
    try:
        with tempfile.NamedTemporaryFile(mode="w", encoding="utf-8", dir=target.parent,
                                         prefix=".replay-health-", delete=False) as stream:
            temp_path = Path(stream.name)
            stream.write(encoded)
            stream.flush()
            os.fsync(stream.fileno())
        # Exclusive atomic publication: a concurrent creator cannot be overwritten.
        os.link(temp_path, target)
    finally:
        if temp_path is not None:
            temp_path.unlink(missing_ok=True)


def integer(value):
    if isinstance(value, bool) or not isinstance(value, int) or value < 0:
        raise EvidenceError("expected nonnegative integer")
    return value


def read_jsonl(path, live=False):
    """Ignore only an unterminated final line in live mode, never a malformed row."""
    if not path.exists():
        return [], {"path": str(path), "missing": True}
    if path.stat().st_size > MAX_FILE_BYTES:
        raise EvidenceError(f"file size limit: {path}")
    rows, digest, size, partial = [], hashlib.sha256(), 0, False
    with path.open("rb") as stream:
        while True:
            raw = stream.readline(MAX_LINE_BYTES + 1)
            if not raw:
                break
            size += len(raw)
            if len(raw) > MAX_LINE_BYTES or size > MAX_FILE_BYTES:
                raise EvidenceError(f"input bound: {path}")
            if live and not raw.endswith(b"\n"):
                partial = True
                break
            digest.update(raw)
            if not raw.strip():
                continue
            try:
                row = strict_json(raw)
            except EvidenceError:
                raise
            except (ValueError, UnicodeError) as error:
                raise EvidenceError(f"invalid JSONL: {path}, row {len(rows)+1}") from error
            if not isinstance(row, dict):
                raise EvidenceError(f"non-object row: {path}")
            rows.append(row)
            if len(rows) > MAX_ROWS:
                raise EvidenceError(f"row limit: {path}")
    return rows, {"path": str(path), "consumed_sha256": digest.hexdigest(),
                  "rows": len(rows), "partial_tail_deferred": partial}


def analyze(situations, placements, matches, game_uid, player, *, live=False,
            grace=3000, persistence=1500, max_gap=750):
    """Inspect one explicitly selected game/player; never combine unrelated matches."""
    for threshold in (grace, persistence, max_gap):
        if not isinstance(threshold, int) or isinstance(threshold, bool) or threshold <= 0:
            raise EvidenceError("all policy tick thresholds must be positive integers")
    identity(game_uid)
    identity(player)
    rows = [r for r in situations if r.get("game_uid") == game_uid and r.get("player") == player]
    placed = [r for r in placements if r.get("game_uid") == game_uid]
    finals = [r for r in matches if r.get("game_uid") == game_uid and
              isinstance(r.get("player"), dict) and r["player"].get("name") == player]
    report = {"policy": POLICY_VERSION, "game_uid": game_uid, "player": player,
              "status": "UNKNOWN", "findings": [], "metrics": {},
              "earliest_block_tick": None, "scope": "telemetry symptom triage, not replay semantic validation"}
    def finding(code, severity, evidence):
        report["findings"].append({"code": code, "severity": severity, "evidence": evidence})
    if not rows:
        finding("MISSING_SITUATIONS", "UNKNOWN", "No snapshots for selected game/player")
        return report
    if len(finals) > 1:
        raise EvidenceError("duplicate selected terminal records")
    faction, bot, map_uid = rows[0].get("faction"), rows[0].get("bot_type"), rows[0].get("map_uid")
    for value in (faction, bot, map_uid):
        identity(value)
    if faction not in PROFILES:
        finding("UNSUPPORTED_ECONOMY_PROFILE", "UNKNOWN", faction)
        return report
    snapshots, previous = [], -1
    for r in rows:
        if type(r.get("schema")) is not int or r.get("schema") != 2 or r.get("kind") != "situation":
            raise EvidenceError("unsupported situation schema/kind")
        if (r.get("faction"), r.get("bot_type"), r.get("map_uid")) != (faction, bot, map_uid):
            raise EvidenceError("identity drift within selected snapshots")
        tick = integer(r.get("tick"))
        if tick <= previous:
            raise EvidenceError("duplicate or non-monotonic selected snapshot ticks")
        previous = tick
        own, expansion = r.get("own"), r.get("expansion")
        if not isinstance(own, dict) or not isinstance(expansion, dict):
            raise EvidenceError("missing own/expansion state")
        snapshots.append({"tick": tick, "refineries": integer(expansion.get("refineries")),
                          "harvesters": integer(own.get("harvesters")),
                          "conyards": integer(expansion.get("conyards")),
                          "bank": integer(own.get("banked_cash"))})
    own_ref_ticks, peer_ref_ticks, own_lifecycle_ticks = [], [], []
    for r in placed:
        if type(r.get("schema")) is not int or r.get("schema") != 1 or r.get("kind") not in {"placement", "refinery_acquired", "refinery_lost"} or r.get("map_uid") != map_uid:
            raise EvidenceError("unsupported placement schema/kind or mismatched map")
        tick = integer(r.get("tick"))
        if r.get("kind") != "placement":
            # Loss/acquisition proves prior ownership; it is not a placement.
            if r.get("player") == player:
                own_lifecycle_ticks.append(tick)
            continue
        if r.get("category") == "refinery":
            if r.get("player") == player:
                own_ref_ticks.append(tick)
            elif r.get("faction") == faction and r.get("bot_type") in REFERENCE_BOTS:
                peer_ref_ticks.append(tick)
    final = finals[0] if finals else None
    if final:
        if type(final.get("schema")) is not int or final.get("schema") != 2 or final.get("map_uid") != map_uid or (
                final["player"].get("faction"), final["player"].get("bot_type")) != (faction, bot):
            raise EvidenceError("terminal schema or identity drift")
        if integer(final.get("duration_ticks")) < snapshots[-1]["tick"]:
            raise EvidenceError("snapshot after terminal duration")
    report["metrics"].update({"faction": faction, "bot_type": bot, "map_uid": map_uid,
                              "snapshots": len(snapshots), "last_tick": snapshots[-1]["tick"],
                              "first_refinery_placement_tick": min(own_ref_ticks, default=None),
                              "max_observed_refineries": max(s["refineries"] for s in snapshots),
                              "max_observed_harvesters": max(s["harvesters"] for s in snapshots)})
    # Require persistent zero-economy while the construction yard remains, AND a
    # same-faction reference placement in this match. This is a sentinel health
    # alarm, not a claim that the opponent is a fair combat comparator.
    run_start, last_tick = None, None
    own_seen = False
    # Only earliest observations are needed; no per-snapshot event scan.
    own_first = min(own_ref_ticks + own_lifecycle_ticks, default=None)
    peer_first = min(peer_ref_ticks, default=None)
    for s in snapshots:
        own_seen |= s["refineries"] > 0 or (own_first is not None and own_first <= s["tick"])
        peer_seen = peer_first is not None and peer_first <= s["tick"]
        stalled = (s["tick"] >= grace and not own_seen and peer_seen and
                   s["refineries"] == 0 and s["harvesters"] == 0 and s["conyards"] > 0)
        if not stalled:
            run_start, last_tick = None, None
            continue
        if run_start is None or s["tick"] - last_tick > max_gap:
            run_start = s["tick"]
        last_tick = s["tick"]
        if s["tick"] - run_start >= persistence:
            report["earliest_block_tick"] = s["tick"]
            finding("STARTUP_ECONOMY_STALL", "BLOCK", {
                "start_tick": run_start, "observed_tick": s["tick"],
                "persistence_ticks": s["tick"]-run_start,
                "refineries": 0, "harvesters": 0, "conyards": s["conyards"],
                "reference_first_refinery_tick": min(peer_ref_ticks),
                "cause": "unknown; inspect queue rejection, claims, resources and path diagnostics"})
            break
    # Weak behavior remains a result, not an exclusion trigger.
    high = [s for s in snapshots if s["tick"] >= grace and s["bank"] >= 25000]
    if len(high) >= 3:
        finding("CREDIT_FLOAT", "WARN", {"samples": len(high), "max_bank": max(s["bank"] for s in high),
                                           "cause": "unknown; legitimate reservation/tech/producer gates may apply"})
    if final:
        stats = final.get("stats", {})
        if not isinstance(stats, dict):
            raise EvidenceError("terminal stats must be an object")
        for key in ("resources_earned", "resources_spent", "kills_cost", "deaths_cost"):
            if key in stats:
                report["metrics"][key] = integer(stats[key])
        report["metrics"]["recorded_outcome"] = final["player"].get("outcome")
        finding("OUTCOME_NOT_END_REASON", "INFO", "Outcome labels alone cannot distinguish cap, crash or natural defeat")
        if stats.get("deaths_cost", 0) > stats.get("kills_cost", 0) > 0:
            finding("NEGATIVE_VALUE_EXCHANGE", "WARN", {"kills_cost": stats["kills_cost"],
                                                       "deaths_cost": stats["deaths_cost"]})
        timeline = stats.get("stats_timeline", [])
        fields = "tick,earned,spent,army_value,assets_value,kills_cost,deaths_cost,banked,idle_queues"
        if not isinstance(timeline, list) or len(timeline) > MAX_ROWS:
            raise EvidenceError("stats timeline must be a bounded list")
        if timeline:
            if stats.get("stats_timeline_fields") != fields or not isinstance(timeline, list):
                raise EvidenceError("unsupported stats timeline fields")
            previous_tick = -1
            previous_spent = 0
            for point in timeline:
                if not isinstance(point, list) or len(point) != 9:
                    raise EvidenceError("invalid stats timeline point")
                for value in point:
                    integer(value)
                if point[0] <= previous_tick or point[0] > final["duration_ticks"]:
                    raise EvidenceError("non-monotonic or post-terminal stats timeline")
                if point[2] < previous_spent:
                    raise EvidenceError("cumulative spending decreased")
                previous_tick = point[0]
                previous_spent = point[2]
            report["metrics"]["economy_combat_timeline"] = [dict(zip(fields.split(","), p)) for p in timeline]
            # A diagnosis request, not proof of a bug. Spend includes more than
            # non-power production; idle_queues is not a cumulative duration.
            left, bank_minima = 0, deque()
            for right, point in enumerate(timeline):
                if point[7] < 25000 or (right and point[0] - timeline[right-1][0] > max_gap):
                    left = right if point[7] >= 25000 else right + 1
                    bank_minima.clear()
                if point[7] < 25000:
                    continue
                while bank_minima and timeline[bank_minima[-1]][7] >= point[7]:
                    bank_minima.pop()
                bank_minima.append(right)
                while left < right and point[0] - timeline[left+1][0] >= 3000:
                    left += 1
                while bank_minima and bank_minima[0] < left:
                    bank_minima.popleft()
                if point[0] - timeline[left][0] >= 3000:
                    delta = point[2] - timeline[left][2]
                    if 0 <= delta <= 1000:
                        finding("SPENDING_STALL_SUSPECTED", "WARN", {
                            "start_tick": timeline[left][0], "end_tick": point[0],
                            "min_bank": timeline[bank_minima[0]][7], "spend_delta": delta,
                            "cause": "unknown; inspect per-category suppression, producers, reservations and tech holds"})
                        break
    if report["earliest_block_tick"] is not None:
        report["status"] = "BLOCK"
    elif not live and not final:
        finding("MISSING_TERMINAL_RECORD", "UNKNOWN", "Final-mode analysis requires a selected terminal record")
    elif not live and snapshots[-1]["tick"] < grace + persistence:
        finding("SHORT_HEALTH_WINDOW", "UNKNOWN", "Match ended before policy observation window")
    elif not own_seen:
        finding("NO_CONFIRMED_STARTUP_REFINERY", "UNKNOWN", "No positive refinery evidence and no supported persistent sentinel failure")
    else:
        report["status"] = "OBSERVED_HEALTHY"
        finding("STARTUP_REFINERY_OBSERVED", "INFO", "Economy startup observed; does not certify useful routes, strength or every queue")
    return report


def inspect(support, game_uid, player, live=False):
    logs = support / "Logs"
    files, receipts = [], []
    for name in ("cameo-ai-situations.jsonl", "cameo-ai-placements.jsonl", "cameo-ai-matches.jsonl"):
        rows, receipt = read_jsonl(logs / name, live)
        files.append(rows)
        receipts.append(receipt)
    report = analyze(*files, game_uid, player, live=live)
    report["inputs"] = receipts
    report["mode"] = "live-prefix" if live else "final"
    summary = support / "batch_summary.json"
    if summary.exists():
        if summary.stat().st_size > MAX_LINE_BYTES:
            raise EvidenceError("oversized batch summary")
        data = strict_json(summary.read_text(encoding="utf-8"))
        if not isinstance(data, dict):
            raise EvidenceError("batch summary must be an object")
        if "fingerprint" in data and not isinstance(data["fingerprint"], dict):
            raise EvidenceError("summary fingerprint must be an object")
        if "fingerprint_id" in data:
            identity(data["fingerprint_id"])
        report["capture_fingerprint"] = data.get("fingerprint")
        report["capture_fingerprint_id"] = data.get("fingerprint_id")
    return report


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("support", type=Path)
    parser.add_argument("--game-uid", required=True)
    parser.add_argument("--player", required=True)
    parser.add_argument("--live", action="store_true", help="only newline-committed JSONL; missing final record allowed")
    parser.add_argument("--output", type=Path)
    args = parser.parse_args()
    try:
        report = inspect(args.support, args.game_uid, args.player, args.live)
    except (EvidenceError, OSError, ValueError, TypeError, KeyError) as error:
        report = {"policy": POLICY_VERSION, "status": "UNKNOWN", "error": str(error)}
    encoded = json.dumps(report, indent=2, sort_keys=True, allow_nan=False) + "\n"
    if args.output:
        try:
            atomic_report(args.output, encoded, args.support)
        except (EvidenceError, OSError) as error:
            report = {"policy": POLICY_VERSION, "status": "UNKNOWN", "error": str(error)}
            encoded = json.dumps(report, indent=2, sort_keys=True, allow_nan=False) + "\n"
    print(encoded, end="")
    return {"BLOCK": 20, "UNKNOWN": 21}.get(report["status"], 0)


if __name__ == "__main__":
    raise SystemExit(main())
