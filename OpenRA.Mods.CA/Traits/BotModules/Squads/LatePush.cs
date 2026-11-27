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
using System.Collections.Generic;

namespace OpenRA.Mods.CA.Traits.BotModules.Squads
{
	/// <summary>
	/// LATEPUSH-01 P1 (design 01 D1 + D2, design 02 R-A to R-D): the pure half of the age-scaled team attack minimum (row lp_age_min) and of the commit fraction (row lp_commit). Integer maths only (percent and permille),
	/// no world access, no RNG.
	/// The bar is <c>M = ceil(base x (1 + t/AgeTicks) x K_map x L)</c>: the base is the WAVE-MIN bar the leader uses today and the age factor is fixed and not capped (R-B); <c>K_map</c> is the bounded map context
	/// (distance of the leader's last picked target over the learned reference distance, clamped); <c>L</c> is the ONE learned multiplier (R-C), never a second one on top of the aggression values. The reachable bar
	/// never exceeds a share of what the team can send and never falls below the WAVE-MIN floor. The commit fraction is the share of the eligible army one order commits.
	/// </summary>
	public static class LatePush
	{
		/// <summary>K_map clamp, percent (design 01 D1: [0.8, 1.4]).</summary>
		public const int MapPercentMin = 80;
		public const int MapPercentMax = 140;

		/// <summary>The clamp of the learned multiplier L, percent.</summary>
		public const int LearnedPercentMin = 50;
		public const int LearnedPercentMax = 200;

		/// <summary>The clamp of the learned multiplier of the commit base (L_commit), percent.</summary>
		public const int CommitLearnedMin = 50;
		public const int CommitLearnedMax = 150;

		/// <summary>The clamp of the learned map reference distance, cells (R-A).</summary>
		public const int MapRefMin = 32;
		public const int MapRefMax = 128;

		/// <summary>1 + t / ageTicks in permille (for the DEV rows; the bar and the share use the exact fraction). Not capped (the user's age factor keeps rising after the SquadValue ramp stops).</summary>
		public static long AgePermille(int tick, int ageTicks) => 1000 + (1000L * Math.Max(0, tick) / Math.Max(1, ageTicks));

		/// <summary>K_map in percent: the distance to the last picked target over the reference distance, clamped to [MapPercentMin, MapPercentMax]. A negative distance (no target picked yet) is neutral, 100.</summary>
		public static int MapPercent(int distanceCells, int refCells) =>
			distanceCells < 0 ? 100 : (int)Math.Clamp(100L * distanceCells / Math.Max(1, refCells), MapPercentMin, MapPercentMax);

		/// <summary>The age-scaled bar: ceil(tierBase x (ageTicks + t) / ageTicks x K_map x L), exact in integers (no rounding before the ceiling). A base of 0 or less (no limit) stays 0.</summary>
		public static int Bar(int tierBase, int tick, int ageTicks, int mapPercent, int learnedPercent)
		{
			if (tierBase <= 0)
				return 0;

			var age = Math.Max(1, ageTicks);
			var den = (long)age * 100 * 100;
			var num = (long)tierBase * (age + (long)Math.Max(0, tick)) * Math.Clamp(mapPercent, MapPercentMin, MapPercentMax) * Math.Clamp(learnedPercent, LearnedPercentMin, LearnedPercentMax);
			return (int)Math.Min(int.MaxValue, (num + den - 1) / den);
		}

		/// <summary>
		/// The bar the leader applies: the age-scaled bar, never above capPercent of the eligible army (so a shrinking team can still push) and never below the WAVE-MIN floor. With no eligible army reported the floor
		/// is the bar (the old behaviour).
		/// </summary>
		public static int EffectiveBar(int bar, int waveMin, int capPercent, long eligible)
		{
			var floor = Math.Max(0, waveMin);
			if (eligible <= 0)
				return floor;

			var cap = (int)Math.Min(int.MaxValue, ((Math.Max(0, capPercent) * Math.Max(0, eligible)) + 99) / 100);
			return Math.Max(floor, Math.Min(bar, cap));
		}

		/// <summary>
		/// The share of the eligible army one order commits, percent: f_time = min(capPercent, basePercent x L_commit x age); then f = f_time + (100 - f_time) x clamp((r - 100) / (allInRatio - 100), 0, 1), r = the
		/// observed own/enemy army ratio in percent (100 = unknown, time only). At r of the all-in ratio or more it is 100.
		/// </summary>
		public static int CommitPercent(int tick, int ageTicks, int basePercent, int capPercent, int ratioPercent, int allInRatioPercent, int learnedPercent = 100)
		{
			var cap = Math.Clamp(capPercent, 0, 100);
			var age = Math.Max(1, ageTicks);
			var f0 = (long)Math.Max(0, basePercent) * Math.Clamp(learnedPercent, CommitLearnedMin, CommitLearnedMax) / 100;
			var time = (int)Math.Min(cap, f0 * (age + (long)Math.Max(0, tick)) / age);
			var span = Math.Max(1, allInRatioPercent - 100);
			var ahead = Math.Clamp(ratioPercent - 100, 0, span);
			return Math.Clamp(time + (int)((100L - time) * ahead / span), 0, 100);
		}

		/// <summary>The eligible army of a seat: the free units plus the guard units above the floor.</summary>
		public static int Eligible(int freeUnits, int guardSpareUnits) => Math.Max(0, freeUnits) + Math.Max(0, guardSpareUnits);

		/// <summary>
		/// The guard units one order lends: ceil(f x E) - free, clamped to [0, spare], with E = free + spare. Never above the units above the floor, never negative. The join minimum (a squad needs a real group) is
		/// applied by GuardFloor.Lend afterwards, the one lend contract.
		/// </summary>
		public static int LendPlan(int commitPercent, int freeUnits, int guardSpareUnits)
		{
			var spare = Math.Max(0, guardSpareUnits);
			if (spare == 0)
				return 0;

			var free = Math.Max(0, freeUnits);
			var want = (int)(((long)Math.Clamp(commitPercent, 0, 100) * (free + spare) + 99) / 100);
			return Math.Clamp(want - free, 0, spare);
		}
	}

	/// <summary>
	/// The learnable numbers of the late push: the multiplier L on the age-scaled bar (percent), the multiplier of the commit base (percent) and the reference distance of K_map (cells, R-A: a learned entry, the clamp of K_map
	/// unchanged). The yaml fills the priors (L per personality block); a learner overrides any of them under a stable key; clearing an override restores the prior. Every value is clamped, so the table never holds a
	/// runaway number. One table per module instance, so every own personality keeps its own (the 4-value aggregation is the learner's job, P3).
	/// </summary>
	public sealed class LatePushTable
	{
		public const string LearnedKey = "latepush.l";
		public const string CommitKey = "latepush.lcommit";
		public const string MapRefKey = "latepush.kref";

		readonly int learnedPrior;
		readonly int mapRefPrior;
		readonly Dictionary<string, int> overrides = new(StringComparer.Ordinal);

		public LatePushTable(int learnedPrior, int mapRefPrior)
		{
			this.learnedPrior = learnedPrior;
			this.mapRefPrior = mapRefPrior;
		}

		/// <summary>The learner's overrides, by key (read only).</summary>
		public IReadOnlyDictionary<string, int> Overrides => overrides;

		public void Override(string key, int value) => overrides[key] = value;

		public void ClearOverride(string key) => overrides.Remove(key);

		public void ClearOverrides() => overrides.Clear();

		/// <summary>L, percent, [LearnedPercentMin, LearnedPercentMax].</summary>
		public int LearnedPercent => Math.Clamp(overrides.TryGetValue(LearnedKey, out var v) ? v : learnedPrior, LatePush.LearnedPercentMin, LatePush.LearnedPercentMax);

		/// <summary>L_commit, percent, [CommitLearnedMin, CommitLearnedMax]; the prior is 100.</summary>
		public int CommitPercent => Math.Clamp(overrides.TryGetValue(CommitKey, out var v) ? v : 100, LatePush.CommitLearnedMin, LatePush.CommitLearnedMax);

		/// <summary>The K_map reference distance, cells, [MapRefMin, MapRefMax].</summary>
		public int MapRefCells => Math.Clamp(overrides.TryGetValue(MapRefKey, out var v) ? v : mapRefPrior, LatePush.MapRefMin, LatePush.MapRefMax);
	}
}
