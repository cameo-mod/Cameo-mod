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
	// V07 rows (Opus 11-24). One row per feature; row off = the build without that feature.
	public static partial class AblationIds
	{
		// EARLY-DEF-01: every bot builds 2-3 ground defences (scaled by tier) right after its first production building; they are essentials at low cash and exempt
		// from the defence-need gate and the seen-threat rule (those always meant extra defences only). The AF2 cap and RATIO-2X still count them.
		public const string AntiRushBase = "bb_antirush_base";

		// TEAM-DEFEND-02: a ready turret is placed at its maximum weapon range from the current attackers of an own or allied base (solo and team), on the defended side.
		public const string TeamTurretSupport = "tm_ally_turret_support";
	}
}
