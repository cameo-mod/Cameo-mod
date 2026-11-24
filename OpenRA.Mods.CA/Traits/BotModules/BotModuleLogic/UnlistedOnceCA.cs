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
	// UPGRADE-PRODUCERS-01 (USER P0). Pure decisions of the "every structure type once" rule for types without a BuildingFractions row.
	public static class UnlistedOnceCA
	{
		/// <summary>
		/// Whether a structure type is covered: it has no fraction row, is a real building, is no wall/fake/defence/anti-air/proxy and has no rule of its own
		/// (construction yard, refinery, power plant, silo).
		/// </summary>
		public static bool Eligible(bool inFractions, bool isBuilding, bool wallOrFake, bool hasOwnRule, bool defence, string name) =>
			!inFractions && isBuilding && !wallOrFake && !hasOwnRule && !defence
			&& name != null && name.IndexOf("proxy", StringComparison.OrdinalIgnoreCase) < 0;

		/// <summary>Index of the cheapest candidate (then ordinal name) or -1.</summary>
		public static int Pick(IReadOnlyList<(string Name, int Cost)> candidates)
		{
			var best = -1;
			for (var i = 0; i < candidates.Count; i++)
			{
				if (best < 0 || candidates[i].Cost < candidates[best].Cost
					|| (candidates[i].Cost == candidates[best].Cost && string.CompareOrdinal(candidates[i].Name, candidates[best].Name) < 0))
					best = i;
			}

			return best;
		}
	}
}
