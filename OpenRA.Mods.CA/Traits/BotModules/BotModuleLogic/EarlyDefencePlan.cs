#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

namespace OpenRA.Mods.CA.Traits
{
	/// <summary>
	/// CONSOLIDATE-03 P1 (a), Sol item 7: the ONE early-defence plan (named input EarlyDefence of BuildingPickArbiter). S4 (the anti-rush set) and the MODE-BIAS antirush
	/// profile read the same answer, so the set size can no longer drift between them. Memory: designer-early-defence-all-bots-2026-11-24 (every bot builds 2-3 anti-rush
	/// defences early; 11-26: the count and timing are learned per map, enemy and personality, 2-3 = the prior). P1 (a) is the prior only (AntiRushCA, unchanged);
	/// P1 (b) row early_def_learn scales it (EARLY_DEF_LEARN_01_DESIGN.md).
	/// </summary>
	public static class EarlyDefencePlan
	{
		/// <summary>The early-defence count: the prior, AntiRushBaseMin..AntiRushBaseMax on the straight tier line (AntiRushCA.SetSize).</summary>
		public static int Count(int armyShareTargetPermille, int min, int max) => AntiRushCA.SetSize(armyShareTargetPermille, min, max);

		/// <summary>Whether the set is wanted now: a production building stands and fewer ground defences than <paramref name="count"/> are owned or queued (AntiRushCA.Wants).</summary>
		public static bool Wanted(bool hasProduction, int groundDefencesOwnedOrQueued, int count) => AntiRushCA.Wants(hasProduction, groundDefencesOwnedOrQueued, count);
	}
}
