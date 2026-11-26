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
	// RM-03 (design RM_03_YARD_RAMP_DESIGN_02 49BFE42A, Opus 11-26). Owner: Workhorse. Off = the planner's MCV target of the base tip (208f57b66).
	public static partial class AblationIds
	{
		// The expansion planner raises its construction-yard target to a demand floor read from an elapsed-tick x own-difficulty table (UseRampedYardDemand).
		// A floor only: never a ceiling, never a second personality multiplier, never a bypass of the free-far-field or production gates.
		public const string YardRamp = "rm03_yard_ramp";
	}
}
