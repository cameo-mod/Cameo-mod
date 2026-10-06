#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License,
 * either version 3 of the License, or (at your option) any later version.
 * For more information, see COPYING.
 */
#endregion

using System;

namespace OpenRA.Mods.CA.Traits
{
	/// <summary>
	/// Pure rules for BU_harvester_logistics (SPEC_2026-10-05_econ_logistics Part B): the per-refinery reservation
	/// size and the re-route hysteresis margin. Integer-only; the module supplies the data, these helpers own the math.
	/// </summary>
	public static class HarvesterLogistics
	{
		/// <summary>
		/// Harvesters a refinery-served field keeps for its spreader: enough to cover the field's cells at
		/// cells-per-harvester, clamped to the per-field cap (0 or negative cap = unlimited).
		/// </summary>
		public static int Reservation(int resourceCells, int cellsPerHarvester, int cap)
		{
			if (resourceCells <= 0 || cellsPerHarvester <= 0)
				return 0;

			var r = (resourceCells + cellsPerHarvester - 1) / cellsPerHarvester;
			return cap > 0 ? Math.Min(r, cap) : r;
		}

		/// <summary>
		/// Re-route only when the target field beats the current field's yield by at least marginPercent
		/// (integer; 25 = +25%). An empty current field accepts any field with cells.
		/// </summary>
		public static bool MarginPasses(int currentYield, int targetYield, int marginPercent)
		{
			if (currentYield <= 0)
				return targetYield > 0;

			return (long)(targetYield - currentYield) * 100 >= (long)currentYield * marginPercent;
		}
	}
}
