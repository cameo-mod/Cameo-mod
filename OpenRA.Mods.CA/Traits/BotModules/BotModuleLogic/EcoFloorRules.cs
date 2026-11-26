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

namespace OpenRA.Mods.CA.Traits
{
	/// <summary>
	/// ECO-TIMING-FLOOR (row eco_timing_floor, HANS_PROFILE_DESIGN_01 row 1, Opus V08G): a time floor on the refinery count. At least 2 own refineries by tick T2 (Hans: 3700) and at
	/// least 3 by T3 (5000), once fields are in reach; the floor lifts only the per-zone refinery cap ("adequate"), never the ally-served, radius or reach verdicts. The ticks are the
	/// Hard values; every other tier is <see cref="HardRank"/> - r steps of EcoFloorStepTicks later or earlier (equal steps on the ladder). Pure, no world.
	/// </summary>
	public static class EcoFloorRules
	{
		/// <summary>The rank of the Hard tier on the difficulty ladder (Easiest = 0 .. God = 9); the Hans values are Hard's.</summary>
		public const int HardRank = 4;

		/// <summary>
		/// The refineries wanted at <paramref name="tick"/>: 3 from T3 - lead, 2 from T2 - lead, else 0 (no floor yet). The lead is the time a refinery needs to be built and placed, so the
		/// floor starts the want early enough for the count to stand by T2 / T3.
		/// </summary>
		public static int RefineryTarget(int tick, int tick2, int tick3, int leadTicks) =>
			tick >= tick3 - leadTicks ? 3 : tick >= tick2 - leadTicks ? 2 : 0;

		/// <summary>The floor lifts the per-zone cap by one while fewer own refineries stand than the floor target.</summary>
		public static bool Lifts(bool rowOn, int ownRefineries, int target) => rowOn && target > 0 && ownRefineries < target;
	}
}
