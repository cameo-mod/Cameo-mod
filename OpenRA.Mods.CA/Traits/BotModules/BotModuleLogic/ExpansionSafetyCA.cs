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
	// pb_expansion_refinery_safety (USER 11-24): the refinery of an MCV expansion goes into the very-next slot ONLY while the new yard is safe. Pure rule.
	public static class ExpansionSafetyCA
	{
		/// <summary>
		/// Safe = no threat was ever recorded for the yard, or the last one is at least <paramref name="quietTicks"/> old. A threat is the yard taking
		/// damage from an enemy, or a visible enemy combat unit within the scan radius (both record the tick; the scan renews it while the unit stays).
		/// </summary>
		public static bool Safe(int now, int lastThreatTick, int quietTicks) =>
			lastThreatTick < 0 || now - lastThreatTick >= quietTicks;

		/// <summary>A yard further than <paramref name="mainBaseCells"/> cells from the main base centre is an expansion yard.</summary>
		public static bool IsExpansionYard(long distSqCells, int mainBaseCells) => distSqCells > (long)mainBaseCells * mainBaseCells;
	}
}
