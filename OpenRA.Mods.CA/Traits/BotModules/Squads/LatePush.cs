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

		/// <summary>The clamp of the learned army cap (knob latepush.army_cap), permille of the team's eligible army: 0.5 to 1.0 (user decision, audit A6).</summary>
		public const int ArmyCapMin = 500;
		public const int ArmyCapMax = 1000;

		/// <summary>The prior of the learned army cap, permille (user decision: 80 percent, not 60).</summary>
		public const int ArmyCapPrior = 800;

		/// <summary>
		/// LATEPUSH-01 P2 (row lp_army_cap): the same bar as <see cref="EffectiveBar"/> with the cap in permille (the learned share). With a cap of 10 x the percent it is the same number as EffectiveBar, so the row off
		/// keeps the P1 bar exactly.
		/// </summary>
		public static int EffectiveBarPermille(int bar, int waveMin, int capPermille, long eligible)
		{
			var floor = Math.Max(0, waveMin);
			if (eligible <= 0)
				return floor;

			var cap = (int)Math.Min(int.MaxValue, (Math.Clamp(capPermille, 0, 1000) * Math.Max(0, eligible) + 999) / 1000);
			return Math.Max(floor, Math.Min(bar, cap));
		}

		/// <summary>
		/// LATEPUSH-01 P2 (row lp_all_in_publish, design 01 D3): the all-in publish is ready when the team can send, as one claim (100 percent of what the seats report as eligible), the WAVE-MIN floor; the ordinary blob count is never
		/// less than the claim. No age factor, no cap: the age-scaled minimum is for ordinary pushes.
		/// </summary>
		public static (int Units, long Value) AllInClaim(int blobUnits, long blobValue, int eligibleUnits, long eligibleValue) =>
			(Math.Max(blobUnits, eligibleUnits), Math.Max(blobValue, eligibleValue));

		/// <summary>
		/// LATEPUSH-01 P2 (row lp_reinforce, design 01 D5): the ONE reinforcement of an order is due when the seat's pushed squads hold less than belowPercent of what they committed and the team is not behind (the observed
		/// army ratio is at least minRatioPercent; an unseen enemy reads 100, so only an observed deficit holds it back).
		/// </summary>
		public static bool ReinforceDue(int alive, int committed, int belowPercent, int ratioPercent, int minRatioPercent) =>
			committed > 0 && alive * 100L < committed * (long)Math.Max(0, belowPercent) && ratioPercent >= minRatioPercent;

		/// <summary>The size of the one reinforcement group: what the squads lost, from the joinable pool, never negative.</summary>
		public static int ReinforceSend(int committed, int alive, int joinablePool) => Math.Max(0, Math.Min(joinablePool, committed - alive));

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
		public const string ArmyCapKey = "latepush.army_cap";

		readonly int learnedPrior;
		readonly int mapRefPrior;
		readonly int armyCapPrior;
		readonly Dictionary<string, int> overrides = new(StringComparer.Ordinal);

		public LatePushTable(int learnedPrior, int mapRefPrior, int armyCapPrior = LatePush.ArmyCapPrior)
		{
			this.learnedPrior = learnedPrior;
			this.mapRefPrior = mapRefPrior;
			this.armyCapPrior = armyCapPrior;
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

		/// <summary>The learned army cap (latepush.army_cap), permille of the eligible army, [ArmyCapMin, ArmyCapMax]; the prior is 800.</summary>
		public int ArmyCapPermille => Math.Clamp(overrides.TryGetValue(ArmyCapKey, out var v) ? v : armyCapPrior, LatePush.ArmyCapMin, LatePush.ArmyCapMax);
	}
}
