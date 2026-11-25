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
	// V08 rows (Opus 11-25).
	public static partial class AblationIds
	{
		// ENG-ROUTE-01 (V08 RCA 8c37f899, a 2549 ms SafeRoute visit): the engineer's safe-route search is bounded (expansion budget per call, calls per tick, a danger cache that lives several ticks).
		// Row off = the unbounded search.
		public const string EngRouteBudget = "er_route_budget";
	}
}
