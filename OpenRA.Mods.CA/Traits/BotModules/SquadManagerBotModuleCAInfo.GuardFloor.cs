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
	// CONSOLIDATE-03 P7 (b) (row sq_guard_floor). Only read behind guard-lend (the CONSOLIDATE-02 one launcher), so @classic and every block without it play as before.
	public partial class SquadManagerBotModuleCAInfo
	{
		[Desc("CONSOLIDATE-03 P7 (b) (row sq_guard_floor; needs sq_guard_lend): the home guard keeps GuardFloorPercent percent of its units at home and lends only the units above that minimum; during an open all-in the floor is 0.",
			"Off in code and classic.")]
		public readonly bool GuardFloorEnabled = false;

		[Desc("The personality PRIOR of the guard floor, percent of the guard kept at home (0-100): turtle high, rush and steamroller low. A learner overrides it per player (GuardFloorTable key guardfloor.pct).")]
		public readonly int GuardFloorPercent = 0;

		[Desc("The PRIOR of the lend-return window in ticks (a learnable input of the guard floor table, key guardfloor.return; 0 = no return).")]
		public readonly int GuardFloorReturnTicks = 3000;
	}
}
