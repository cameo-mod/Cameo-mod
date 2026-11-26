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
	// CONSOLIDATE-03 P8 (b) (CONSOLIDATE_03_P8_DESIGN_01.md).
	public static partial class AblationIds
	{
		// An MCV lease whose MCV has not changed cell for McvLeaseStallTicks ends and its field is parked (Info McvLeaseStallRelease on the expansion planner). Off = a live MCV
		// holds its lease until it is gone.
		public const string McvLeaseStallRelease = "mcv_lease_stall_release";
	}
}
