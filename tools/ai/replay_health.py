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

# Schema 3 is the current anonymous logger format. Keep the allow-lists for the
# row and nested objects whose fields either select evidence or feed the health
# verdict. An additive change to one of these shapes must be reviewed here
# instead of being silently ignored by an older checker.
SITUATION_V3_FIELDS = {
    "schema", "kind", "record_id", "game_uid", "map_uid", "seat", "player",
    "faction", "bot_type", "tick", "urgency", "personality_current",
    "personality_candidate", "main_target", "main_target_score", "mission",
    "mission_assignment", "hints", "demand", "own", "expansion", "enemies",
}
OWN_V3_FIELDS = {
    "army_value", "defence_value", "buildings", "harvesters", "kills_cost_window",
    "deaths_cost_window", "squad_count", "squad_units", "losses_by_role",
    "away_losses_by_role", "combat_ratio_pct", "combat_ratio_defended_pct",
    "production_window", "production_per_game_min", "econ_destroyed_window",
    "econ_destroyed", "attacks_launched", "first_attack_tick", "attacks_per_game_min",
    "regions_fresh", "regions_total", "regions_enemy_presence", "guerrilla_lead",
    "enemy_production_per_game_min", "steamroller_lead", "rush_lead", "banked_cash",
    "brownout_ticks", "idle_production_ticks", "production_queues", "repair_orders",
    "repair_sweep_orders", "repair_toggles_avoided", "utility_turtlerush",
    "utility_techrushexpansion", "utility_steamrollerguerrilla", "director_tension",
    "director_phase", "team_allied_bots", "team_army_value", "team_max_tension",
    "team_defend_requests", "team_shared_target", "team_any_climax",
    "coalition_phase", "coalition_main_target", "fb_fronts", "fb_fronts_without_radar",
    "fb_radar_union_cells", "fb_radar_approach_cells", "threats", "scale_targets",
    "build_order", "bandit", "contacts",
}
EXPANSION_V3_FIELDS = {
    "fields_known", "fields_in_reach", "fields_served", "fields_harvested",
    "anchors_spreader", "anchors_field", "refineries", "excess_refineries",
    "unassigned_refineries", "anchor_dist_mean", "anchor_dist_max",
    "refineries_per_anchor_max", "anchors_in_reach_unserved", "fields_in_reach_unserved",
    "fields_in_reach_unserved_ids", "conyards", "outposts", "crawl_target", "mcv_site",
    "crawl_mcv_angle", "coverage_milli",
}
PLACEMENT_FIELDS = {
    "schema", "kind", "record_id", "game_uid", "map_uid", "seed", "seat", "player",
    "faction", "bot_type", "personality", "tick", "queued_tick", "placed_tick",
    "actor", "cell", "category", "reason", "anchor_cell", "anchor_dist", "anchor_kind",
    "field_id", "resource_gap", "tier", "cause",
}
MATCH_V3_FIELDS = {
    "schema", "record_id", "recorded_utc", "mod_version", "game_uid", "map_uid",
    "map_title", "duration_ticks", "timestep", "player", "stats", "seats", "allies",
    "opponents", "opponent_signatures", "ownership", "arsenal", "order_gate",
}
MATCH_PLAYER_V3_FIELDS = {
    "seat", "name", "bot_type", "faction", "team", "handicap", "spawn", "home",
    "outcome", "personality", "personality_switches", "personality_timeline",
    "composition", "composition_switches", "composition_timeline", "episode_timeline",
}
MATCH_STATS_V3_FIELDS = {
    "units_killed", "units_lost", "buildings_killed", "buildings_lost", "kills_cost",
    "deaths_cost", "army_value", "assets_value", "resources_earned", "resources_spent",
    "stats_timeline_fields", "stats_timeline",
}

# PLAYTEST-TELEMETRY-V2 (maintainer request 2026-10-12): the per-player channel
# covers every seat in cameo-ai-players-<sha256(game_uid)>.jsonl. Two record
# shapes share the file, distinguished by 'schema'.
PLAYER_SAMPLE_V1_FIELDS = {
    "schema", "version", "game_uid", "record_id", "tick", "seat", "bot_type",
    "faction", "team", "harvesters", "refineries", "conyards", "expansions",
    "mcv_deploys", "production_buildings", "tech_tier", "idle_production_ticks",
    "buildings", "units", "army", "unit_types",
}
PLAYER_SAMPLE_ARMY_FIELDS = {"infantry", "vehicle", "air", "naval", "defence"}
PLAYER_EVENT_V1_FIELDS = {
    "schema", "version", "game_uid", "record_id", "tick", "seat", "kind",
    "seq", "actor", "actor_id", "cell", "start_dist", "cause", "to_seat", "units",
}
PLAYER_EVENT_KINDS = {
    "building_placed", "structure_lost", "conyard_deployed", "conyard_packed",
    "first_unit_enemy_territory", "attack_wave_start", "superweapon_built",
    "superweapon_fired",
}
# WBL-P1 CONVERSION-DIAGNOSTICS (lead task 2026-10-12): bot seats get a third
# record shape in the same channel — a 250-tick production-queue sample with
# per-category conversion counters and the reason each queue is not producing.
BOT_QUEUE_V1_FIELDS = {
    "schema", "version", "game_uid", "record_id", "tick", "seat", "banked",
    "spent_delta", "requested", "accepted", "completed", "skipped", "spend",
    "queues",
}
BOT_QUEUE_CATEGORY_FIELDS = {"unit", "defence", "crawl_link", "tech", "economy"}
BOT_QUEUE_ROW_FIELDS = {"queue", "group", "state", "item", "reason", "reserved_by"}
BOT_QUEUE_STATES = {"idle", "busy", "paused", "disabled"}
BOT_QUEUE_REASONS = {
    "queue_disabled", "producing", "paused", "cash_reserved", "no_cash",
    "saturation_stop", "prerequisite_missing", "unit_cap", "no_desire",
}
BOT_QUEUE_SKIP_REASONS = {"should_build", "no_queue", "duplicate", "in_production"}
MAX_BOT_QUEUE_ROWS = 32
MAX_PLAYER_FILES = 64


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


def row_identity(row):
    """A row's own selectable identity: top-level 'seat' (newer privacy schema) or 'player'."""
    return row.get("seat", row.get("player"))


def reject_unknown_fields(row, allowed, label):
    unknown = set(row) - allowed
    if unknown:
        raise EvidenceError(f"unsupported {label} field(s): {', '.join(sorted(unknown))}")


def player_identity(player_dict):
    """The identity inside a nested terminal/match 'player' object: 'seat' or legacy 'name'."""
    return player_dict.get("seat", player_dict.get("name"))


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
    rows = [r for r in situations if r.get("game_uid") == game_uid and row_identity(r) == player]
    placed = [r for r in placements if r.get("game_uid") == game_uid]
    finals = [r for r in matches if r.get("game_uid") == game_uid and
              isinstance(r.get("player"), dict) and player_identity(r["player"]) == player]
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
        schema = r.get("schema")
        if type(schema) is not int or schema not in {2, 3} or r.get("kind") != "situation":
            raise EvidenceError("unsupported situation schema/kind")
        if schema == 3:
            reject_unknown_fields(r, SITUATION_V3_FIELDS, "schema-3 situation")
            if "seat" in r and "player" in r and r["seat"] != r["player"]:
                raise EvidenceError("conflicting situation seat/player identity")
            own_row, expansion_row = r.get("own"), r.get("expansion")
            if isinstance(own_row, dict):
                reject_unknown_fields(own_row, OWN_V3_FIELDS, "schema-3 own-state")
            if isinstance(expansion_row, dict):
                reject_unknown_fields(expansion_row, EXPANSION_V3_FIELDS, "schema-3 expansion")
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
        schema = r.get("schema")
        if type(schema) is not int or schema not in {1, 2} or r.get("kind") not in {"placement", "refinery_acquired", "refinery_lost"} or r.get("map_uid") != map_uid:
            raise EvidenceError("unsupported placement schema/kind or mismatched map")
        if schema == 2:
            reject_unknown_fields(r, PLACEMENT_FIELDS, "schema-2 placement")
            if "seat" in r and "player" in r and r["seat"] != r["player"]:
                raise EvidenceError("conflicting placement seat/player identity")
        tick = integer(r.get("tick"))
        if r.get("kind") != "placement":
            # Loss/acquisition proves prior ownership; it is not a placement.
            if row_identity(r) == player:
                own_lifecycle_ticks.append(tick)
            continue
        if r.get("category") == "refinery":
            if row_identity(r) == player:
                own_ref_ticks.append(tick)
            elif r.get("faction") == faction and r.get("bot_type") in REFERENCE_BOTS:
                peer_ref_ticks.append(tick)
    final = finals[0] if finals else None
    if final:
        schema = final.get("schema")
        if type(schema) is not int or schema not in {2, 3} or final.get("map_uid") != map_uid or (
                final["player"].get("faction"), final["player"].get("bot_type")) != (faction, bot):
            raise EvidenceError("terminal schema or identity drift")
        if schema == 3:
            reject_unknown_fields(final, MATCH_V3_FIELDS, "schema-3 match")
            reject_unknown_fields(final["player"], MATCH_PLAYER_V3_FIELDS, "schema-3 match player")
            if "seat" in final["player"] and "name" in final["player"] and final["player"]["seat"] != final["player"]["name"]:
                raise EvidenceError("conflicting terminal seat/name identity")
            stats_row = final.get("stats")
            if isinstance(stats_row, dict):
                reject_unknown_fields(stats_row, MATCH_STATS_V3_FIELDS, "schema-3 match stats")
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


def validate_bot_queue_row(row):
    """Fail-closed shape check for one cameo-bot-queue v1 row (WBL-P1).
    Shared by the health channel reader and the playtest stats aggregator."""
    reject_unknown_fields(row, BOT_QUEUE_V1_FIELDS, "bot-queue v1")
    if row.get("version") != 1:
        raise EvidenceError("unsupported bot-queue version")
    integer(row.get("tick"))
    integer(row.get("banked"))
    integer(row.get("spent_delta"))
    for key in ("requested", "accepted", "completed", "spend"):
        values = row.get(key)
        if not isinstance(values, dict):
            raise EvidenceError(f"bot-queue {key} must be an object")
        reject_unknown_fields(values, BOT_QUEUE_CATEGORY_FIELDS, f"bot-queue {key}")
        for value in values.values():
            integer(value)
    skipped = row.get("skipped")
    if not isinstance(skipped, dict):
        raise EvidenceError("bot-queue skipped must be an object")
    for reason, count in skipped.items():
        if reason not in BOT_QUEUE_SKIP_REASONS:
            raise EvidenceError(f"unsupported bot-queue skip reason: {reason}")
        integer(count)
    queues = row.get("queues")
    if not isinstance(queues, list) or len(queues) > MAX_BOT_QUEUE_ROWS:
        raise EvidenceError("bot-queue queues must be a bounded list")
    for queue in queues:
        if not isinstance(queue, dict):
            raise EvidenceError("bot-queue queue row must be an object")
        reject_unknown_fields(queue, BOT_QUEUE_ROW_FIELDS, "bot-queue queue")
        identity(queue.get("queue"))
        if queue.get("state") not in BOT_QUEUE_STATES:
            raise EvidenceError(f"unsupported bot-queue state: {queue.get('state')}")
        if "reason" in queue and queue["reason"] not in BOT_QUEUE_REASONS:
            raise EvidenceError(f"unsupported bot-queue reason: {queue['reason']}")
        voters = queue.get("reserved_by", [])
        if not isinstance(voters, list) or len(voters) > MAX_BOT_QUEUE_ROWS:
            raise EvidenceError("bot-queue reserved_by must be a bounded list")
        for voter in voters:
            identity(voter)


def players_channel(logs, game_uid, player, live=False):
    """Validate the PLAYTEST-TELEMETRY-V2 per-player channel for the selected game.

    The file exists only for matches recorded by a build with AiPlayerTelemetry;
    absent channel => absent evidence, never an error. Returns (receipt, metrics)
    or (None, {}) when no channel file exists.
    """
    expected = logs / f"cameo-ai-players-{hashlib.sha256(game_uid.encode('utf-8')).hexdigest()}.jsonl"
    if len(list(logs.glob("cameo-ai-players-*.jsonl"))) > MAX_PLAYER_FILES:
        raise EvidenceError("player-telemetry file bound exceeded")
    if not expected.exists():
        return None, {}
    rows, receipt = read_jsonl(expected, live)
    samples, events, queue_rows, last_tick = 0, {}, 0, -1
    for row in rows:
        schema = row.get("schema")
        if schema == "cameo-player-sample":
            reject_unknown_fields(row, PLAYER_SAMPLE_V1_FIELDS, "player-sample v1")
            if row.get("version") != 1:
                raise EvidenceError("unsupported player-sample version")
            integer(row.get("tick"))
            army = row.get("army")
            if isinstance(army, dict):
                reject_unknown_fields(army, PLAYER_SAMPLE_ARMY_FIELDS, "player-sample army")
                for value in army.values():
                    integer(value)
            types = row.get("unit_types")
            if isinstance(types, dict):
                for value in types.values():
                    integer(value)
            samples += 1
            if row.get("seat") == player:
                last_tick = max(last_tick, row["tick"])
        elif schema == "cameo-player-event":
            reject_unknown_fields(row, PLAYER_EVENT_V1_FIELDS, "player-event v1")
            if row.get("version") != 1:
                raise EvidenceError("unsupported player-event version")
            integer(row.get("tick"))
            if "seq" in row:
                integer(row["seq"])
            if "actor_id" in row:
                integer(row["actor_id"])
            kind = row.get("kind")
            if kind not in PLAYER_EVENT_KINDS:
                raise EvidenceError(f"unsupported player-event kind: {kind}")
            if row.get("seat") == player:
                events[kind] = events.get(kind, 0) + 1
        elif schema == "cameo-bot-queue":
            validate_bot_queue_row(row)
            if row.get("seat") == player:
                queue_rows += 1
        else:
            raise EvidenceError(f"unsupported player-telemetry schema: {schema}")
    metrics = {"player_channel_samples": samples,
               "player_channel_events": events,
               "player_channel_queue_rows": queue_rows,
               "player_channel_last_sample_tick": last_tick if last_tick >= 0 else None}
    return receipt, metrics


def inspect(support, game_uid, player, live=False):
    logs = support / "Logs"
    files, receipts = [], []
    for name in ("cameo-ai-situations.jsonl", "cameo-ai-placements.jsonl", "cameo-ai-matches.jsonl"):
        rows, receipt = read_jsonl(logs / name, live)
        files.append(rows)
        receipts.append(receipt)
    report = analyze(*files, game_uid, player, live=live)
    report["inputs"] = receipts
    channel_receipt, channel_metrics = players_channel(logs, game_uid, player, live)
    if channel_receipt is not None:
        report["inputs"].append(channel_receipt)
        report["metrics"].update(channel_metrics)
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
