#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System.Collections.Generic;

namespace OpenRA.Mods.CA.Traits
{
	// CONSOLIDATE-01 c1 (B07/X11). Pure decision for the per-tick in-flight recount of BaseBuilderBotModuleCA.
	public static class InFlightRecountCA
	{
		/// <summary>
		/// True when this tick recounts the buildings in flight. Armed: always (B07 fix). Not armed: the old rule, only while the bank
		/// is at least <paramref name="minCash"/> (BuildingProductionMinCashRequirement).
		/// </summary>
		public static bool Recounts(bool armed, int bank, int minCash) => armed || bank >= minCash;

		/// <summary>Counts one head item of a building queue (the old TryGetValue / Add pair, unchanged).</summary>
		public static void Record(Dictionary<string, int> counts, string item)
		{
			if (counts.TryGetValue(item, out var value))
				counts[item] = ++value;
			else
				counts.Add(item, 1);
		}
	}
}
