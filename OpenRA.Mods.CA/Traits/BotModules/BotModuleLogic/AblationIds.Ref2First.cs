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
	// REF2-FIRST-01 rows (USER BINDING 11-26, one switch Ref2First on the base builder). Owner: Sonnet.
	public static partial class AblationIds
	{
		// ref2_before_radar off = a radar provider is offered while ref2 is due, as before.
		public const string Ref2BeforeRadar = "ref2_before_radar";

		// ref2_before_prod2 off = the 2nd production building of a role is offered while ref2 is due, as before.
		public const string Ref2BeforeProd2 = "ref2_before_prod2";

		// ref2_floor off = no zone cap lift for the second refinery and no interval outrank, as before (the time floor eco_timing_floor is unchanged).
		public const string Ref2Floor = "ref2_floor";
	}
}
