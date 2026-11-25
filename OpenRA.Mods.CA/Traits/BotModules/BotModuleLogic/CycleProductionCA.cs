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
	// PROD-BUSY / ESSENTIALS-01 (USER 11-24): above the tier line the bot turns cash into more units, so the extras are additional production buildings,
	// cycled across the types. Pure pick, no random draw.
	public static class CycleProductionCA
	{
		/// <summary>
		/// Index of the production building to add, or -1 for none. The type with the fewest buildings (owned + in production) goes first, so the
		/// types are cycled: one barracks, one war factory, one airfield, one naval yard, then round again. Among equals the next name after
		/// <paramref name="lastPicked"/> in ordinal order wins (wrapping), so a type whose placement failed is not retried forever.
		/// </summary>
		public static int Pick(IReadOnlyList<(string Name, int Count)> candidates, string lastPicked)
		{
			var best = -1;
			for (var i = 0; i < candidates.Count; i++)
				if (best < 0 || Better(candidates[i], candidates[best], lastPicked))
					best = i;

			return best;
		}

		/// <summary>
		/// CONSOLIDATE-01 c6 (stage S8): every candidate in the order repeated <see cref="Pick"/> calls take them (the first is exactly the Pick), so the planner
		/// falls through to the next type of the cycle when the first fails a feasibility test. Pure, no random draw.
		/// </summary>
		public static IReadOnlyList<string> Order(IReadOnlyList<(string Name, int Count)> candidates, string lastPicked)
		{
			var rest = new List<(string Name, int Count)>(candidates);
			var names = new List<string>(rest.Count);
			while (rest.Count > 0)
			{
				var index = Pick(rest, lastPicked);
				names.Add(rest[index].Name);
				rest.RemoveAt(index);
			}

			return names;
		}

		static bool Better((string Name, int Count) a, (string Name, int Count) b, string lastPicked)
		{
			if (a.Count != b.Count)
				return a.Count < b.Count;

			var aAfter = lastPicked != null && string.CompareOrdinal(a.Name, lastPicked) > 0;
			var bAfter = lastPicked != null && string.CompareOrdinal(b.Name, lastPicked) > 0;
			if (aAfter != bAfter)
				return aAfter;

			return string.CompareOrdinal(a.Name, b.Name) < 0;
		}
	}
}
