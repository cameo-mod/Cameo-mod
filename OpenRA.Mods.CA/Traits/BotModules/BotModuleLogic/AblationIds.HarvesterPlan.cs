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
	// CONSOLIDATE-03 P9 (b) (CONSOLIDATE_03_P9_DESIGN_01.md).
	public static partial class AblationIds
	{
		// The harvester module's need counts the harvesters already in a production queue (Info HvModuleCountsQueued on the unit builder). Off = live harvesters only, as before.
		public const string HvModuleCountsQueued = "hv_module_counts_queued";
	}
}
