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
	// CONSOLIDATE-01 c5 rows (CONSOLIDATE_01_DESIGN.md 3.3). Owner: Frontrunner.
	public static partial class AblationIds
	{
		// R3: stage S4 of the staged planner, the anti-rush base defence set (Info BbAntiRushSet). Off = S4 offers nothing.
		public const string BbAntiRushSet = "bb_antirush_set";

		// R6: stage S9, the extra defences after a seen threat (Info BbExtraDefence). Off = S9 offers nothing and the defences stay in the S10 fraction roll,
		// which carries the same defence cap. The cap itself keeps its row af2_defence_cap, the need gate af2_defence_need_gate, RATIO-2X its row.
		public const string BbExtraDefence = "bb_extra_defence";
	}
}
