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
	// ECO-TIMING-FLOOR.
	public static partial class AblationIds
	{
		// A time floor on the refinery count (>= 2 by tick 3700, >= 3 by 5000 at Hard; Hans profile): while fewer own refineries stand than the floor, the per-zone cap of every zone in reach is one higher
		// (Info EcoTimingFloor, refinery law zones). Off = the zone cap as before.
		public const string EcoTimingFloor = "eco_timing_floor";
	}
}
