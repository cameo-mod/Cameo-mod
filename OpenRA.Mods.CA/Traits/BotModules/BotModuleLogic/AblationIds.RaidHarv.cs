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
	// RAID-HARV-01 P1 row (design 01 D7, design 02 section 8).
	public static partial class AblationIds
	{
		// raid_harv: the native harvester raid (observed harvesters, routed group package, explored-only route, recon safety). Row off = the old harass high-value path (building raids), byte for byte.
		public const string RaidHarv = "raid_harv";
	}
}
