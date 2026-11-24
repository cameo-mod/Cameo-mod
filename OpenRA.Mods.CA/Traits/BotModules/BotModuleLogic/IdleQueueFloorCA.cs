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

namespace OpenRA.Mods.CA.Traits
{
	// INFANTRY-IDLE-01 (USER P0): the idle queue's last-resort pick. Pure, no random draw.
	public static class IdleQueueFloorCA
	{
		/// <summary>Index of the best candidate or -1: highest weight first, then cheapest, then ordinal name.</summary>
		public static int Pick(IReadOnlyList<(string Name, int Cost, int Weight)> candidates)
		{
			var best = -1;
			for (var i = 0; i < candidates.Count; i++)
				if (best < 0 || Better(candidates[i], candidates[best]))
					best = i;

			return best;
		}

		static bool Better((string Name, int Cost, int Weight) a, (string Name, int Cost, int Weight) b)
		{
			if (a.Weight != b.Weight)
				return a.Weight > b.Weight;

			if (a.Cost != b.Cost)
				return a.Cost < b.Cost;

			return string.CompareOrdinal(a.Name, b.Name) < 0;
		}
	}
}
