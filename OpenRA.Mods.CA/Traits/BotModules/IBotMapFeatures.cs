#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace OpenRA.Mods.CA.Traits.BotModules
{
	/// <summary>Read-only public map attributes. This interface does not select actions.</summary>
	public interface IBotMapFeatures { ContextFeatureVector MapFeatures { get; } }

	/// <summary>Transient opposing-seat observations within this world only, never a cross-match key.</summary>
	public interface IBotOpponentFeatures
	{
		bool TryObservedFeatures(int currentEnemySeat, out ContextFeatureVector features);
	}

	public enum ContextFeatureKind { Map, Opponent }

	/// <summary>Zero is measured zero; absent values have an explicit missing bit and no confidence.</summary>
	public readonly record struct ContextFeatureMeasurement(int Value, bool Present, int ConfidencePermille)
	{
		public static ContextFeatureMeasurement Missing => new(-1, false, 0);
		public static ContextFeatureMeasurement Observed(int value, int confidence = 1000) =>
			new(value, true, Math.Clamp(confidence, 0, 1000));
	}

	/// <summary>Immutable closed schema. No actor/player/map identifiers, names or coordinates can become fields.</summary>
	public sealed class ContextFeatureVector
	{
		public const int Version = 1;
		public const int MaximumSerializedBytes = 16 * 1024;
		public const int MaximumLearnedBytes = 12 * 1024 * 1024;
		static readonly ReadOnlyCollection<string> mapFields = Array.AsReadOnly(new[]
		{
			"aspect_permille",
			"backline_depth_cells",
			"blocked_share_permille",
			"buildable_frontier_permille",
			"buildable_near_start_permille",
			"chokepoint_count",
			"chokepoint_width_mean_cells",
			"chokepoint_width_min_cells",
			"chokepoint_width_p90_cells",
			"closest_enemy_path_cells",
			"component_count",
			"defence_arc_permille",
			"elevation_spread",
			"enemy_path_spread_permille",
			"expansion_to_enemy_ratio_permille",
			"frontier_path_spread_permille",
			"furthest_enemy_path_cells",
			"height_cells",
			"minimum_cut_cells",
			"naval_route_share_permille",
			"nearest_ally_path_cells",
			"nearest_capturable_path_cells",
			"nearest_chokepoint_path_cells",
			"nearest_expansion_path_cells",
			"nearest_oil_path_cells",
			"nearest_poi_path_cells",
			"nearest_resource_path_cells",
			"nearest_tech_path_cells",
			"nearest_water_path_cells",
			"openness_permille",
			"own_start_degree",
			"own_start_water_access_permille",
			"perimeter_cells",
			"playable_cells",
			"player_count",
			"poi_contested_permille",
			"poi_value_per_player",
			"reachable_field_count",
			"resource_centre_permille",
			"resource_cluster_permille",
			"resource_contested_permille",
			"resource_corner_permille",
			"resource_edge_permille",
			"resource_field_count",
			"resource_field_path_spread_permille",
			"resource_mean_field_path_cells",
			"resource_near_spawn_permille",
			"resource_value_per_playable_cell_milli",
			"resource_value_per_player",
			"route_detour_permille",
			"spreader_count",
			"start_to_field_ratio_permille",
			"start_to_poi_ratio_permille",
			"team_count",
			"total_resource_value",
			"water_share_permille",
			"width_cells",
		});
		static readonly ReadOnlyCollection<string> opponentFields = Array.AsReadOnly(new[]
		{
			"aggression_permille",
			"air_share_permille",
			"army_value_acceleration",
			"army_value_slope",
			"attack_value_share_permille",
			"command_burst_permille",
			"concentration_permille",
			"confidence_permille",
			"construction_completion_rate",
			"contact_ticks",
			"counter_role_latency_ticks",
			"coverage_permille",
			"defence_response_rate_permille",
			"economy_share_permille",
			"expansion_commitment_permille",
			"expansion_distance_ratio_permille",
			"field_claim_rate",
			"fire_uptime_permille",
			"first_pressure_tick",
			"focus_convergence_permille",
			"formation_spread_permille",
			"forward_commitment_permille",
			"front_entropy_permille",
			"greed_permille",
			"harass_frequency_per_10000t",
			"largest_front_share_permille",
			"low_hp_retreat_rate_permille",
			"military_to_econ_permille",
			"multi_front_response_permille",
			"naval_share_permille",
			"observation_age_ticks",
			"outpost_commitment_permille",
			"overkill_waste_permille",
			"production_to_army_lag_ticks",
			"resource_exposure_permille",
			"resource_security_permille",
			"retarget_after_contact_ticks",
			"role_entropy_permille",
			"role_share_air",
			"role_share_artillery",
			"role_share_defence",
			"role_share_infantry",
			"role_share_naval",
			"role_share_other",
			"role_share_vehicle",
			"route_repeat_permille",
			"seen_actor_samples",
			"seen_regions",
			"split_response_permille",
			"stealth_share_permille",
			"tech_depth_permille",
			"tech_investment_permille",
			"tech_sequence_distance_permille",
			"threat_response_latency_ticks",
			"unit_mix_novelty_permille",
			"visible_action_transitions_per_1000t",
			"visible_bank_pressure_permille",
			"visible_econ_growth_per_1000t",
			"visible_expansion_completion_rate",
			"visible_expansion_starts_per_10000t",
			"visible_production_growth_per_1000t",
		});

		public ContextFeatureKind Kind { get; }
		public IReadOnlyDictionary<string, ContextFeatureMeasurement> Fields { get; }
		public static IReadOnlyList<string> SchemaFields(ContextFeatureKind kind) => kind switch
		{
			ContextFeatureKind.Map => mapFields,
			ContextFeatureKind.Opponent => opponentFields,
			_ => throw new ArgumentOutOfRangeException(nameof(kind))
		};

		public ContextFeatureVector(ContextFeatureKind kind,
			IEnumerable<KeyValuePair<string, ContextFeatureMeasurement>> observations = null)
		{
			Kind = kind;
			var fields = new SortedDictionary<string, ContextFeatureMeasurement>(StringComparer.Ordinal);
			foreach (var name in SchemaFields(kind))
				fields.Add(name, ContextFeatureMeasurement.Missing);
			var assigned = new HashSet<string>(StringComparer.Ordinal);
			foreach (var pair in observations ?? [])
			{
				if (!fields.ContainsKey(pair.Key) || !assigned.Add(pair.Key))
					throw new ArgumentException("Unknown or duplicate context feature.", nameof(observations));
				if (pair.Value.ConfidencePermille < 0 || pair.Value.ConfidencePermille > 1000)
					throw new ArgumentException("Invalid feature confidence.", nameof(observations));
				fields[pair.Key] = pair.Value.Present ? pair.Value : ContextFeatureMeasurement.Missing;
			}

			Fields = new ReadOnlyDictionary<string, ContextFeatureMeasurement>(fields);
		}
	}
}
