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
	// TEAM-DEFEND-02 (design pending/TEAM_DEFEND_02_DESIGN_01.md, user-approved two-tier ally defence). Owner: Frontrunner.
	public static partial class AblationIds
	{
		// tm_defend_two_tier: the team alarm (T2) is answered by EVERY ally with no radius, sized to 1.25 x the seen attack minus the defence there and split by spare value; a base asset damaged
		// alarms at any attack size, an unseen attacker raises a blind alarm at the damaged location. Row off = the alarm path of sq_defend_one (or the older arbiter share / whole pool) exactly as before. T1 (the local 25-cell response) is never touched.
		// Info switch: TeamDefendTwoTierEnabled.
		public const string TeamDefendTwoTier = "tm_defend_two_tier";
	}
}
