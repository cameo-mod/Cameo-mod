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
	// V07 TEAM-DEFEND row (USER master replay 2026-11-24, via Opus). Owner: Frontrunner. Off = the pre-change behaviour of every bot, byte for byte.
	public static partial class AblationIds
	{
		// TEAM-DEFEND unit side: a serious attack on an own or allied actor raises one team alarm; every teammate sends its pool, idle and home-guard units; a pushed blob keeps
		// its order unless the ally would die. Info switch: TeamLeadDefendTeamEnabled.
		public const string TeamLeadDefendTeam = "tl_defend_team";
	}
}
