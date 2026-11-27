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
using System.Numerics;

namespace OpenRA.Mods.Cameo.Traits.BotModules
{
	/// <summary>
	/// B4 OBSERVED-STYLE (DESIGN_OBSERVED_STYLE_02 sections 1-2, features F1-F10 of _01): the feature extractor. It recomputes every feature from the
	/// allowlisted <see cref="FogStyleActor"/> list, the <see cref="StyleTracker"/> history and the tick, and nothing else. Output = bucketed integers
	/// (log2 buckets or fixed-width share buckets); "not seen" = <see cref="Unknown"/> (-1), never zero. No feature reads TypeName (pinned): the fitter must
	/// not be able to key on names, and a faction cannot leak in through them.
	///
	/// Field to feature mapping (the table the design asks for in the code header):
	///   F1  first_out_tick/value : tracker first combat unit outside the base (nearest remembered building farther than 20 cells), its Value.
	///   F2  army_pm_m3/6/10/15   : tracker army value at the first update at or after minute 3/6/10/15, divided by the minute.
	///   F3  prod_per_tech        : Production buildings x1000 / (tech-only buildings + 1); first_tech/first_prod tick from the tracker.
	///   F4  yards, refineries, mcvs, clusters (greedy anchor groups of BaseBuilding or Refinery buildings, link distance 24 cells) and the tracker's 2nd-yard tick.
	///   F5  defence_share (Defence building value over building value) and defence_count.
	///   F6  max_tech_value       : highest Value of a tech-only building (tech-up depth proxy).
	///   F7  inf/veh/air/naval shares of combat value, mean combat unit Value.
	///   F8  groups_3000, group_median, group_max, gone_count: tracker attack-group events per 3000 ticks and combat units seen then gone without contact.
	///   F9  harv_per_ref (Harvester count over Refinery count), ref_per_yard.
	///   F10 coverage (share of remembered buildings last seen within 3000 ticks) and since_building (ticks since the newest building sighting): NOT style
	///       features, they set the classifier's confidence in a later phase.
	/// </summary>
	internal static class StyleFeatures
	{
		public const int Unknown = -1;

		/// <summary>Coverage window of F10 (ticks).</summary>
		public const int CoverageTicks = 3000;

		/// <summary>The expansion-cluster link distance (cells) of F4.</summary>
		public const int ClusterLinkCells = 24;

		/// <summary>The feature names, in output order. The row writer, the tests and the offline fitter read this list.</summary>
		public static readonly string[] Names =
		{
			"f1_first_out_tick", "f1_first_out_value",
			"f2_army_pm_m3", "f2_army_pm_m6", "f2_army_pm_m10", "f2_army_pm_m15",
			"f3_prod_per_tech", "f3_first_tech_tick", "f3_first_prod_tick",
			"f4_yards", "f4_refineries", "f4_mcvs", "f4_second_yard_tick", "f4_clusters",
			"f5_defence_share", "f5_defence_count",
			"f6_max_tech_value",
			"f7_inf_share", "f7_veh_share", "f7_air_share", "f7_naval_share", "f7_mean_unit_value",
			"f8_groups_3000", "f8_group_median", "f8_group_max", "f8_gone_count",
			"f9_harv_per_ref", "f9_ref_per_yard",
			"f10_coverage", "f10_since_building",
		};

		public static int Count => Names.Length;

		/// <summary>Index of f10_coverage, the classifier's confidence input (not a style feature).</summary>
		public const int CoverageIndex = 28;

		/// <summary>0 for v &lt;= 0, else 1 + floor(log2 v), capped at 15.</summary>
		public static int Bucket(long v)
		{
			if (v <= 0)
				return 0;

			var log = 63 - BitOperations.LeadingZeroCount((ulong)v);
			return Math.Min(15, 1 + log);
		}

		/// <summary>A fixed-width share bucket: permille / width, capped.</summary>
		static int Share(long numerator, long denominator, int width, int cap) =>
			denominator <= 0 ? Unknown : (int)Math.Min(cap, numerator * 1000 / denominator / width);

		static int Chebyshev(CPos a, CPos b) => Math.Max(Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));

		/// <summary>Writes the Count bucketed features into <paramref name="output"/> (length at least Count). The tracker must already hold this update.</summary>
		public static void Extract(ReadOnlySpan<FogStyleActor> actors, int tick, StyleTracker tracker, Span<int> output)
		{
			if (output.Length < Count)
				throw new ArgumentException("output too short", nameof(output));

			output.Slice(0, Count).Fill(Unknown);

			var buildings = 0;
			var yards = 0;
			var refineries = 0;
			var production = 0;
			var techOnly = 0;
			var defence = 0;
			var mcvs = 0;
			var harvesters = 0;
			long buildingValue = 0;
			long defenceValue = 0;
			var maxTech = 0;
			var recent = 0;
			var newest = int.MinValue;
			long combatValue = 0;
			long inf = 0, veh = 0, air = 0, nav = 0;
			var combatUnits = 0;

			for (var i = 0; i < actors.Length; i++)
			{
				ref readonly var a = ref actors[i];
				if (a.Building)
				{
					buildings++;
					buildingValue += a.Value;
					if (a.BaseBuilding)
						yards++;

					if (a.Refinery)
						refineries++;

					if (a.Production)
						production++;

					if (a.Defence)
					{
						defence++;
						defenceValue += a.Value;
					}

					if (a.Tech && !a.BaseBuilding && !a.Production && !a.Refinery && !a.Defence)
					{
						techOnly++;
						maxTech = Math.Max(maxTech, a.Value);
					}

					if (tick - a.LastSeenTick <= CoverageTicks)
						recent++;

					newest = Math.Max(newest, a.LastSeenTick);
				}
				else
				{
					if (a.Mcv)
						mcvs++;

					if (a.Harvester)
						harvesters++;

					if (a.Combat)
					{
						combatUnits++;
						combatValue += a.Value;
						if (a.Aircraft)
							air += a.Value;
						else if (a.Naval)
							nav += a.Value;
						else if (a.Infantry)
							inf += a.Value;
						else if (a.Vehicle)
							veh += a.Value;
					}
				}
			}

			// F1
			if (tracker.FirstOutsideTick >= 0)
			{
				output[0] = Bucket(tracker.FirstOutsideTick / 150);
				output[1] = Bucket(tracker.FirstOutsideValue / 10);
			}

			// F2
			for (var c = 0; c < 4; c++)
				if (tracker.ArmyAtCheckpoint[c] >= 0)
					output[2 + c] = Bucket(tracker.ArmyAtCheckpoint[c] / ((c == 0 ? 3 : c == 1 ? 6 : c == 2 ? 10 : 15)));

			// F3, F4, F5, F6, F9 and F10 need at least one remembered building; with none, they stay unknown
			if (buildings > 0)
			{
				output[6] = Bucket(production * 1000L / (techOnly + 1));
				output[9] = Bucket(yards);
				output[10] = Bucket(refineries);
				output[14] = Share(defenceValue, buildingValue, 50, 20);
				output[15] = Bucket(defence);
				output[16] = Bucket(maxTech / 50);
				output[27] = Share(refineries, Math.Max(1, yards), 500, 8);
				output[28] = (int)Math.Min(10, recent * 10L / buildings);
				output[29] = Bucket((tick - newest) / 150);
				output[13] = Clusters(actors);
			}

			if (tracker.FirstTechTick >= 0)
				output[7] = Bucket(tracker.FirstTechTick / 150);

			if (tracker.FirstProductionTick >= 0)
				output[8] = Bucket(tracker.FirstProductionTick / 150);

			if (tracker.SecondYardTick >= 0)
				output[12] = Bucket(tracker.SecondYardTick / 150);

			if (actors.Length > 0)
				output[11] = Bucket(mcvs);

			// F7
			if (combatValue > 0)
			{
				output[17] = Share(inf, combatValue, 125, 8);
				output[18] = Share(veh, combatValue, 125, 8);
				output[19] = Share(air, combatValue, 125, 8);
				output[20] = Share(nav, combatValue, 125, 8);
				output[21] = Bucket(combatValue / combatUnits / 10);
			}

			// F8 (needs a viewer base to measure contact against)
			if (tracker.FirstSeenTick >= 0 && tracker.ContactKnown)
			{
				tracker.Groups(tick, out var groups, out var median, out var max);
				output[22] = Bucket(groups);
				output[23] = Bucket(median);
				output[24] = Bucket(max);
				output[25] = Bucket(tracker.SeenThenGone);
			}

			// F9
			if (refineries > 0)
				output[26] = Share(harvesters, refineries, 250, 8);
		}

		// Expansion clusters: greedy anchor groups (a building joins the first anchor within ClusterLinkCells, else it starts one; at most 16) of yard or refinery buildings.
		static int Clusters(ReadOnlySpan<FogStyleActor> actors)
		{
			Span<CPos> anchors = stackalloc CPos[16];
			var n = 0;
			for (var i = 0; i < actors.Length; i++)
			{
				ref readonly var a = ref actors[i];
				if (!a.Building || !(a.BaseBuilding || a.Refinery))
					continue;

				var joined = false;
				for (var g = 0; g < n && !joined; g++)
					joined = Chebyshev(a.Cell, anchors[g]) <= ClusterLinkCells;

				if (!joined && n < anchors.Length)
					anchors[n++] = a.Cell;
			}

			return Bucket(n);
		}
	}
}
