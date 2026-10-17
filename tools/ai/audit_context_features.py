#!/usr/bin/env python3
"""Grammar-closed validator for anonymous context JSONL, before ingestion.

It accepts exactly the v1 wire schema, including missing flags. This is the
context-record grammar only; the integrated AI-log audit must call it for these
record types after the reviewed P0 grammar lands. It never guesses which strings
look like names and never accepts unknown record shapes.
"""
from __future__ import annotations

import argparse
import json
from pathlib import Path

MAX_RECORD_BYTES = 16 * 1024
MAX_LEARNED_BYTES = 12 * 1024 * 1024
SCHEMAS = {
    'map_features': (
        'aspect_permille',
        'backline_depth_cells',
        'blocked_share_permille',
        'buildable_frontier_permille',
        'buildable_near_start_permille',
        'chokepoint_count',
        'chokepoint_width_mean_cells',
        'chokepoint_width_min_cells',
        'chokepoint_width_p90_cells',
        'closest_enemy_path_cells',
        'component_count',
        'defence_arc_permille',
        'elevation_spread',
        'enemy_path_spread_permille',
        'expansion_to_enemy_ratio_permille',
        'frontier_path_spread_permille',
        'furthest_enemy_path_cells',
        'height_cells',
        'minimum_cut_cells',
        'naval_route_share_permille',
        'nearest_ally_path_cells',
        'nearest_capturable_path_cells',
        'nearest_chokepoint_path_cells',
        'nearest_expansion_path_cells',
        'nearest_oil_path_cells',
        'nearest_poi_path_cells',
        'nearest_resource_path_cells',
        'nearest_tech_path_cells',
        'nearest_water_path_cells',
        'openness_permille',
        'own_start_degree',
        'own_start_water_access_permille',
        'perimeter_cells',
        'playable_cells',
        'player_count',
        'poi_contested_permille',
        'poi_value_per_player',
        'reachable_field_count',
        'resource_centre_permille',
        'resource_cluster_permille',
        'resource_contested_permille',
        'resource_corner_permille',
        'resource_edge_permille',
        'resource_field_count',
        'resource_field_path_spread_permille',
        'resource_mean_field_path_cells',
        'resource_near_spawn_permille',
        'resource_value_per_playable_cell_milli',
        'resource_value_per_player',
        'route_detour_permille',
        'spreader_count',
        'start_to_field_ratio_permille',
        'start_to_poi_ratio_permille',
        'team_count',
        'total_resource_value',
        'water_share_permille',
        'width_cells',
    ),
    'opponent_features': (
        'aggression_permille',
        'air_share_permille',
        'army_value_acceleration',
        'army_value_slope',
        'attack_value_share_permille',
        'command_burst_permille',
        'concentration_permille',
        'confidence_permille',
        'construction_completion_rate',
        'contact_ticks',
        'counter_role_latency_ticks',
        'coverage_permille',
        'defence_response_rate_permille',
        'economy_share_permille',
        'expansion_commitment_permille',
        'expansion_distance_ratio_permille',
        'field_claim_rate',
        'fire_uptime_permille',
        'first_pressure_tick',
        'focus_convergence_permille',
        'formation_spread_permille',
        'forward_commitment_permille',
        'front_entropy_permille',
        'greed_permille',
        'harass_frequency_per_10000t',
        'largest_front_share_permille',
        'low_hp_retreat_rate_permille',
        'military_to_econ_permille',
        'multi_front_response_permille',
        'naval_share_permille',
        'observation_age_ticks',
        'outpost_commitment_permille',
        'overkill_waste_permille',
        'production_to_army_lag_ticks',
        'resource_exposure_permille',
        'resource_security_permille',
        'retarget_after_contact_ticks',
        'role_entropy_permille',
        'role_share_air',
        'role_share_artillery',
        'role_share_defence',
        'role_share_infantry',
        'role_share_naval',
        'role_share_other',
        'role_share_vehicle',
        'route_repeat_permille',
        'seen_actor_samples',
        'seen_regions',
        'split_response_permille',
        'stealth_share_permille',
        'tech_depth_permille',
        'tech_investment_permille',
        'tech_sequence_distance_permille',
        'threat_response_latency_ticks',
        'unit_mix_novelty_permille',
        'visible_action_transitions_per_1000t',
        'visible_bank_pressure_permille',
        'visible_econ_growth_per_1000t',
        'visible_expansion_completion_rate',
        'visible_expansion_starts_per_10000t',
        'visible_production_growth_per_1000t',
    ),
}


def unique_object(pairs):
    result = {}
    for key, value in pairs:
        if key in result:
            raise ValueError("duplicate JSON key")
        result[key] = value
    return result


def validate_line(line: str) -> dict:
    if len(line.encode("utf-8")) > MAX_RECORD_BYTES:
        raise ValueError("context record exceeds wire budget")
    record = json.loads(line, object_pairs_hook=unique_object)
    if type(record) is not dict or set(record) != {"record", "schema", "features"}:
        raise ValueError("unknown context record grammar")
    kind = record["record"]
    if type(kind) is not str or kind not in SCHEMAS or type(record["schema"]) is not int or record["schema"] != 1:
        raise ValueError("unknown context schema")
    fields = record["features"]
    if type(fields) is not dict or set(fields) != set(SCHEMAS[kind]):
        raise ValueError("context fields must match the closed vocabulary")
    for measurement in fields.values():
        if type(measurement) is not dict or set(measurement) != {"value", "present", "confidence_permille"}:
            raise ValueError("unknown feature measurement grammar")
        value, present, confidence = (measurement[k] for k in ("value", "present", "confidence_permille"))
        if type(value) is not int or not -(2**31) <= value < 2**31 or type(present) is not bool:
            raise ValueError("invalid integer measurement or missing bit")
        if type(confidence) is not int or not 0 <= confidence <= 1000:
            raise ValueError("invalid integer confidence")
        if not present and (value != -1 or confidence != 0):
            raise ValueError("missing measurement carries data")
    return record


def check_learned_size(root: Path) -> int:
    # All files count, regardless of suffix. A model cannot escape the cap by renaming.
    total = sum(p.stat().st_size for p in sorted(root.rglob("*")) if p.is_file()) if root.exists() else 0
    if total > MAX_LEARNED_BYTES:
        raise ValueError("learned artifacts exceed 12 MiB")
    return total


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("logs", nargs="*", type=Path)
    parser.add_argument("--learned-root", type=Path)
    args = parser.parse_args()
    records = 0
    for path in sorted(args.logs):
        for number, line in enumerate(path.read_text(encoding="utf-8").splitlines(), 1):
            if not line.strip():
                continue
            try:
                validate_line(line)
            except (ValueError, TypeError) as error:
                parser.exit(1, f"{path}:{number}: {error}\n")
            records += 1
    try:
        size = check_learned_size(args.learned_root) if args.learned_root else 0
    except ValueError as error:
        parser.exit(1, f"{error}\n")
    print(f"CONTEXT_AUDIT=PASS records={records} learned_bytes={size}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
