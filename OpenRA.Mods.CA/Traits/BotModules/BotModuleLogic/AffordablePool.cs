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

using System.Collections.Generic;

namespace OpenRA.Mods.CA.Traits
{
	/// <summary>
	/// PERF-BOTTICK-01 (row ub_affordable_pool): the roll's pool cut down to the members that pass the cash/afford check. The weights are computed over the WHOLE pool first
	/// (floor, counter maximum and composition maximum as today), then each kept member carries its own value unchanged, in pool order.
	/// </summary>
	public static class AffordablePool
	{
		/// <summary>The values at the kept positions, in their order. `kept` is the number of true entries in `keep`.</summary>
		public static T[] Keep<T>(IReadOnlyList<T> values, bool[] keep, int kept)
		{
			var result = new T[kept];
			var at = 0;
			for (var i = 0; i < values.Count; i++)
				if (keep[i])
					result[at++] = values[i];

			return result;
		}

		/// <summary>Counts the true entries.</summary>
		public static int Count(bool[] keep)
		{
			var n = 0;
			foreach (var k in keep)
				if (k)
					n++;

			return n;
		}
	}
}
