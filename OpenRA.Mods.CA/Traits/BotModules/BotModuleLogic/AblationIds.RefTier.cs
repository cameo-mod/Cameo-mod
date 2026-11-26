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
	// REF-TIER-01 rows (USER BINDING 11-26, one switch RefTierFloor on the base builder). Owner: Frontrunner.
	public static partial class AblationIds
	{
		// ref_tier_floor off = no tier zone cap lift and no interval outrank (REF2-FIRST-01, the time floor and the planner's own caps are unchanged).
		public const string RefTierFloor = "ref_tier_floor";

		// ref_tier_personality off = the tier target without the personality multiplier (100 percent for every personality).
		public const string RefTierPersonality = "ref_tier_personality";
	}
}
