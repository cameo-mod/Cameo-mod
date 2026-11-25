#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;

namespace OpenRA.Mods.CA.Traits
{
	// EARLY-DEF-01 (USER 11-24): every bot builds a small anti-rush defence set right after its first production building. Pure decisions, no state.
	public static class AntiRushCA
	{
		/// <summary>The tier line is 500 + 25 r permille (r = 0..10); an unset line (-1) gives the minimum.</summary>
		public static int TierRung(int armyShareTargetPermille) =>
			armyShareTargetPermille < 500 ? 0 : Math.Min(10, (armyShareTargetPermille - 500) / 25);

		/// <summary>The size of the base set, min at tier 0 and max at tier 10, rounded to the nearest whole defence.</summary>
		public static int SetSize(int armyShareTargetPermille, int min, int max)
		{
			var lo = Math.Max(0, min);
			var hi = Math.Max(lo, max);
			return lo + ((hi - lo) * TierRung(armyShareTargetPermille) + 5) / 10;
		}

		/// <summary>The base set is wanted once a production building stands and fewer ground defences than the set size are owned or queued.</summary>
		public static bool Wants(bool hasProduction, int groundDefencesOwnedOrQueued, int setSize) =>
			hasProduction && setSize > 0 && groundDefencesOwnedOrQueued < setSize;

		/// <summary>The type to build next: the one with the fewest owned or queued copies (a varied set), then the cheapest, then by name. -1 = no candidate.</summary>
		public static int Pick(IReadOnlyList<(string Name, int Cost, int OwnedOrQueued)> candidates)
		{
			var best = -1;
			for (var i = 0; i < candidates.Count; i++)
			{
				if (best < 0)
				{
					best = i;
					continue;
				}

				var c = candidates[i];
				var b = candidates[best];
				var less = c.OwnedOrQueued != b.OwnedOrQueued ? c.OwnedOrQueued < b.OwnedOrQueued
					: c.Cost != b.Cost ? c.Cost < b.Cost
					: string.CompareOrdinal(c.Name, b.Name) < 0;
				if (less)
					best = i;
			}

			return best;
		}
	}
}
