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
	// PERF-BOTTICK-01 row (Opus 11-26).
	public static partial class AblationIds
	{
		// PERF-BOTTICK-01: the unit roll draws only among the pool members that pass the same cash/afford check that refuses a pick after the roll; none passes = the queue's fill ends
		// for this visit. V08F4: about 150 rolls per started unit, nearly all refused for cash. Row off = today's roll over the whole pool, refused picks re-rolled.
		public const string UnitAffordablePool = "ub_affordable_pool";
	}
}
