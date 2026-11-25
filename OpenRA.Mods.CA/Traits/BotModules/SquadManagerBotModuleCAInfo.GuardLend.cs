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
	// PUSHBAR-RCA-01 (row sq_guard_lend). Only read under the CONSOLIDATE-02 one launcher (SquadPipeline02Enabled), so @classic and every block without it play as before.
	public partial class SquadManagerBotModuleCAInfo
	{
		[Desc("PUSHBAR-RCA-01 (row sq_guard_lend; needs the CONSOLIDATE-02 one launcher): a QUIET home guard (Protection squad with no target, no held rally, no live request, no perceived or predicted threat) " +
			"lends this percent of its units (ground, armed, at the base, at least the join floor, never the last unit) to the open team order that is still gathering, and the lendable units count in the idle army the leader's " +
			"ready test measures. A guard on a task keeps every unit. Range 0-90, 0 = off.")]
		public readonly int ArmyGuardLendPercent = 50;
	}
}
