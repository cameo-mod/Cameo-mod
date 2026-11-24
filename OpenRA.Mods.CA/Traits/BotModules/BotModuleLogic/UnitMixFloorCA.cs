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
	/// <summary>The army categories the value floor knows. Other = armed units that are neither infantry nor aircraft (they only count toward the total).</summary>
	public enum UnitMixCategory
	{
		None,
		Infantry,
		Aircraft,
		Other,
	}

	// UNIT-MIX-01 (USER 11-24): the army keeps a minimum share of VALUE in infantry (and in aircraft once an airfield exists). Below the floor that category's queue is served first on the
	// next pass, inside the existing cash and unit rules. Pure decisions, no state.
	public static class UnitMixFloorCA
	{
		/// <summary>
		/// True while the category holds less than <paramref name="floorPct"/> percent of the army value. An empty army (total 0) is below the floor of the infantry
		/// (<paramref name="needsArmy"/> false) so the first barracks pick is not left waiting, but never of aircraft (needsArmy true): no air before there is an army.
		/// </summary>
		public static bool BelowFloor(long categoryValue, long totalValue, int floorPct, bool needsArmy)
		{
			if (floorPct <= 0)
				return false;

			if (totalValue <= 0)
				return !needsArmy;

			return categoryValue * 100 < (long)floorPct * totalValue;
		}

		/// <summary>Adds an own unit (or queued item) of the given cost to the census.</summary>
		public static void Add(Dictionary<UnitMixCategory, long> census, UnitMixCategory category, int cost)
		{
			if (category == UnitMixCategory.None)
				return;

			census[category] = census.GetValueOrDefault(category) + Math.Max(0, cost);
		}

		public static long Total(Dictionary<UnitMixCategory, long> census) =>
			census.GetValueOrDefault(UnitMixCategory.Infantry) + census.GetValueOrDefault(UnitMixCategory.Aircraft) + census.GetValueOrDefault(UnitMixCategory.Other);

		/// <summary>
		/// The categories whose queues are served first this pass: infantry when a barracks queue exists and its value share is below its floor, aircraft when an
		/// airfield queue exists and its share is below its floor. Infantry first.
		/// </summary>
		public static List<UnitMixCategory> Deficits(Dictionary<UnitMixCategory, long> census, bool hasInfantryQueue, int infantryFloorPct, bool hasAircraftQueue, int aircraftFloorPct)
		{
			var total = Total(census);
			var result = new List<UnitMixCategory>(2);
			if (hasInfantryQueue && BelowFloor(census.GetValueOrDefault(UnitMixCategory.Infantry), total, infantryFloorPct, false))
				result.Add(UnitMixCategory.Infantry);

			if (hasAircraftQueue && BelowFloor(census.GetValueOrDefault(UnitMixCategory.Aircraft), total, aircraftFloorPct, true))
				result.Add(UnitMixCategory.Aircraft);

			return result;
		}
	}
}
