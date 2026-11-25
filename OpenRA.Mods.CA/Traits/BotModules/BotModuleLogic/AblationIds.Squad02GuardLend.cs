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
	// PUSHBAR-RCA-01 (V08D: 0 team_push in the 6v6). Row off = the old behaviour: the home guard never lends and the idle army does not count it.
	public static partial class AblationIds
	{
		// A quiet home guard (Protection squad) lends its spare units to the open team order, and the lendable units count in the member's idle army (the leader's ready test).
		public const string SqGuardLend = "sq_guard_lend";
	}
}
