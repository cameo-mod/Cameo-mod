#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System;

namespace OpenRA.Mods.CA.Traits
{
	/// <summary>
	/// CONSOLIDATE-03 P1 (a), Sol item 7: the ONE early-defence plan (named input EarlyDefence of BuildingPickArbiter). S4 (the anti-rush set) and the MODE-BIAS antirush
	/// profile read the same answer, so the set size can no longer drift between them. Memory: designer-early-defence-all-bots-2026-11-24 (every bot builds 2-3 anti-rush
	/// defences early; 11-26: the count and timing are learned per map, enemy and personality, 2-3 = the prior).
	/// P1 (b), row early_def_learn (EARLY_DEF_LEARN_01_DESIGN.md): the count = clamp(prior x M(map) x L, 0..CountMax), the start = the first production building + a learned
	/// offset (0..3000 ticks). M is a pure function of the shortest ground path between spawns (public map data, no omniscience); L is the 4-value learned aggregate
	/// (G x F_own x E_eff x S_obs)^(1/4), one table per personality. Until EARLY_RUSH_STATS_01 sets the reference distance and a learned snapshot exists, both are neutral
	/// (1000), so the row equals the prior exactly. The DEV learning step is EARLY-DEF-LEARN-01 step 2 (never in a release build, never reset).
	/// </summary>
	public static class EarlyDefencePlan
	{
		/// <summary>Neutral milli factor (x1.0).</summary>
		public const int Neutral = 1000;

		/// <summary>The latest learned start offset after the first production building, in ticks.</summary>
		public const int MaxStartOffsetTicks = 3000;

		/// <summary>The early-defence count: the prior, AntiRushBaseMin..AntiRushBaseMax on the straight tier line (AntiRushCA.SetSize).</summary>
		public static int Count(int armyShareTargetPermille, int min, int max) => AntiRushCA.SetSize(armyShareTargetPermille, min, max);

		/// <summary>
		/// early_def_learn: the prior scaled by the map factor and the learned factor (milli, 1000 = neutral), rounded to the nearest whole defence and clamped to 0..<paramref name="countMax"/>.
		/// Neutral factors give the prior unchanged whenever the prior is inside the clamp.
		/// </summary>
		public static int LearnedCount(int prior, int mapMilli, int learnedMilli, int countMax)
		{
			var scaled = (long)Math.Max(0, prior) * Math.Max(0, mapMilli) * Math.Max(0, learnedMilli);
			var count = (scaled + 500_000L) / 1_000_000L;
			return (int)Math.Clamp(count, 0, Math.Max(0, countMax));
		}

		/// <summary>
		/// M(map): reference / path, in milli, clamped to 500..2000 (a short path = more defences). Unknown path or reference (0 or less) = neutral.
		/// </summary>
		public static int MapMilli(int pathCells, int referenceCells)
		{
			if (pathCells <= 0 || referenceCells <= 0)
				return Neutral;

			return (int)Math.Clamp((long)referenceCells * Neutral / pathCells, 500, 2000);
		}

		/// <summary>The learned start: open once a production building stands and <paramref name="offsetTicks"/> (clamped to 0..3000) have passed since it first stood.</summary>
		public static bool StartOpen(bool hasProduction, int firstProductionTick, int now, int offsetTicks)
		{
			if (!hasProduction)
				return false;

			var offset = Math.Clamp(offsetTicks, 0, MaxStartOffsetTicks);
			return offset == 0 || (firstProductionTick >= 0 && now - firstProductionTick >= offset);
		}

		/// <summary>Whether the set is wanted now: a production building stands and fewer ground defences than <paramref name="count"/> are owned or queued (AntiRushCA.Wants).</summary>
		public static bool Wanted(bool hasProduction, int groundDefencesOwnedOrQueued, int count) => AntiRushCA.Wants(hasProduction, groundDefencesOwnedOrQueued, count);

		/// <summary>
		/// DEF-RATIO-PERS-01 with audit A4 (user 11-26 P07; Orchestrator 11-27): the ONE early-defence waiver. While it holds, an S4 start is exempt from RATIO-2X.
		/// It ends when the early count is built (<paramref name="have"/> at or above <paramref name="count"/>) OR when <paramref name="waiverTicks"/> have passed since the
		/// first production building stood (<paramref name="firstProductionTick"/>, -1 = none yet). The AF2 cap and the per-type limits are not part of the waiver.
		/// </summary>
		public static bool WaiverActive(int have, int count, int firstProductionTick, int now, int waiverTicks)
		{
			var countExhausted = have >= count;
			var tickExhausted = firstProductionTick >= 0 && now - firstProductionTick >= Math.Max(0, waiverTicks);
			return !(countExhausted || tickExhausted);
		}
	}
}
