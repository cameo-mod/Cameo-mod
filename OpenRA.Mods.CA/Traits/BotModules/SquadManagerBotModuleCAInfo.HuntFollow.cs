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
	public partial class SquadManagerBotModuleCAInfo
	{
		[Desc("CONSOLIDATE-02 Q8b (row sq_hunt_follow, needs sq_hunt_one_cell and sq_order_engaged): a pushed hunt order moves on to the team's next hunt cell when the one it hunts is explored, at most once per this many ticks (counted from its push or its last move). Range 30-3000.")]
		public readonly int ArmyHuntFollowMinTicks = 150;
	}
}
