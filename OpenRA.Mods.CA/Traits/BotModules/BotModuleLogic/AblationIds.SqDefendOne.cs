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
	// CONSOLIDATE-03 P7 (b) row (design 01 5e100b94 contradiction 2, Opus review change 4).
	public static partial class AblationIds
	{
		// sq_defend_one: ONE ally-defend rule. The team alarm (TeamDefendTick, copy 2) drafts the pool units inside the ally-defence radius of the attacked friendly with the DEFEND-ALLY draft limit (copy 1) and
		// shares the response cooldown with the local scan, so one attack is answered by one draft, not by two with different limits and pick rules. Row off = both copies as before.
		public const string SqDefendOne = "sq_defend_one";
	}
}
