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
	// REF-DEMAND-01.
	public static partial class AblationIds
	{
		// The refinery law's demand = resource zones near an own yard and reachable from it, a per-zone cap and friendly spacing (Info RefDemand). Off = the ReachCells rule, unchanged.
		public const string RefDemand = "ref_demand";
	}
}
