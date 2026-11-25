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
	/// <summary>CONSOLIDATE-02 Step 3: why a target is wanted. Each purpose has one fixed order of tiers.</summary>
	public enum TargetPurpose : byte
	{
		/// <summary>The blob's target: the finish focus, then what is seen, then what is remembered, then one hunt cell.</summary>
		Main,

		/// <summary>A raid: the team's shared harass target, high-value targets, then as Main.</summary>
		Harass,

		/// <summary>A defence squad: the nearest armed attacker around what is defended.</summary>
		Defence,

		/// <summary>A state's own pick inside the fight radius, the only picker a state may call (Q9).</summary>
		Local
	}

	/// <summary>One step of a purpose's order. A flag set, so a caller can allow only some tiers (DF-4 asks for the shared harass target or a remembered building).</summary>
	[Flags]
	public enum TargetTier
	{
		None = 0,

		/// <summary>The weakest enemy seat the team finishes (after the late start): its nearest known building.</summary>
		Focus = 1,

		/// <summary>The live joint harass target of the team.</summary>
		SharedHarass = 2,

		/// <summary>A high-value target (harvester, artillery, ...), when the caller's priority roll passed.</summary>
		HighValue = 4,

		/// <summary>The nearest enemy the bot sees now (the main-target seat first; structures first for artillery).</summary>
		Visible = 8,

		/// <summary>The nearest armed enemy seen around what is defended.</summary>
		ArmedAttacker = 16,

		/// <summary>Any enemy seen around what is defended, or the one that reported the hit.</summary>
		Attacker = 32,

		/// <summary>An enemy seen inside the fight radius.</summary>
		InRadius = 64,

		/// <summary>An enemy building the frozen-actor layer remembers.</summary>
		Frozen = 128,

		/// <summary>The one unseen grid cell the team hunts (late game, nothing known).</summary>
		Hunt = 256,

		All = 511
	}

	/// <summary>An enemy the caller offers to the picker. <paramref name="Observed"/> is the caller's own sight of it; an unobserved one is never picked.</summary>
	public readonly record struct TargetSeen(uint Id, long DistSq, bool Observed, bool Armed, bool Structure);

	/// <summary>
	/// CONSOLIDATE-02 Q6 (row sq_target_pick under the master switch sq_pipeline_02): the one target order, pure (design 4.1 Step 3). The pickers that grew one per layer
	/// (the state-machine FindNewTarget, the leader's pick, the raid share, the DF-4 retarget, the defence squads' target) now follow one order per purpose and one nearest
	/// rule, so a purpose is decided in one place and tested without a world. The tiers are resolved by the caller from what the bot observes: nothing here reads an actor,
	/// an enemy the bot does not see is never offered a chance (<see cref="Nearest"/>), and the picks are total orders, no RNG.
	/// </summary>
	public static class TargetPick
	{
		static readonly TargetTier[] MainOrder = { TargetTier.Focus, TargetTier.Visible, TargetTier.Frozen, TargetTier.Hunt };
		static readonly TargetTier[] HarassOrder = { TargetTier.SharedHarass, TargetTier.HighValue, TargetTier.Visible, TargetTier.Frozen, TargetTier.Hunt };
		static readonly TargetTier[] DefenceOrder = { TargetTier.ArmedAttacker, TargetTier.Attacker };
		static readonly TargetTier[] LocalOrder = { TargetTier.InRadius };

		/// <summary>The tiers of <paramref name="purpose"/> in order, restricted to <paramref name="allowed"/> (the same order).</summary>
		public static IReadOnlyList<TargetTier> Order(TargetPurpose purpose, TargetTier allowed = TargetTier.All)
		{
			var order = purpose switch
			{
				TargetPurpose.Main => MainOrder,
				TargetPurpose.Harass => HarassOrder,
				TargetPurpose.Defence => DefenceOrder,
				_ => LocalOrder
			};

			if (allowed == TargetTier.All)
				return order;

			var kept = new List<TargetTier>(order.Length);
			foreach (var tier in order)
				if ((allowed & tier) != 0)
					kept.Add(tier);

			return kept;
		}

		/// <summary>Walks <paramref name="order"/> and returns the first tier <paramref name="tryTier"/> resolves (it stops there), or null when none does.</summary>
		public static TargetTier? Resolve(IReadOnlyList<TargetTier> order, Func<TargetTier, bool> tryTier)
		{
			foreach (var tier in order)
				if (tryTier(tier))
					return tier;

			return null;
		}

		/// <summary>
		/// The index of the pick among <paramref name="seen"/>, or -1. An enemy that is not observed is never picked (fog honesty). The order is: structures first when
		/// <paramref name="structuresFirst"/>, armed first when <paramref name="armedFirst"/>, then the nearest, then the lowest id, so the same list gives the same pick on every
		/// client.
		/// </summary>
		public static int Nearest(IReadOnlyList<TargetSeen> seen, bool structuresFirst = false, bool armedFirst = false)
		{
			var best = -1;
			for (var i = 0; i < seen.Count; i++)
			{
				if (!seen[i].Observed)
					continue;

				if (best < 0 || Better(seen[i], seen[best], structuresFirst, armedFirst))
					best = i;
			}

			return best;
		}

		static bool Better(TargetSeen a, TargetSeen b, bool structuresFirst, bool armedFirst)
		{
			if (structuresFirst && a.Structure != b.Structure)
				return a.Structure;

			if (armedFirst && a.Armed != b.Armed)
				return a.Armed;

			if (a.DistSq != b.DistSq)
				return a.DistSq < b.DistSq;

			return a.Id < b.Id;
		}

		/// <summary>Artillery prefers buildings (Step 3): the global preference, or a squad that is half artillery or more.</summary>
		public static bool PrefersStructures(bool global, int artilleryUnits, int totalUnits) =>
			global || (totalUnits > 0 && artilleryUnits * 2 >= totalUnits);

		/// <summary>The LOCAL purpose sees only enemies within the fight radius of the squad.</summary>
		public static bool WithinFight(long distSq, long radiusSq) => distSq <= radiusSq;
	}
}
