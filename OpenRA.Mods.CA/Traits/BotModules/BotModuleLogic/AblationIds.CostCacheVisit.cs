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
	// PERF-BOTTICK-01 FIX 4 row (Opus 11-26).
	public static partial class AblationIds
	{
		// PERF-BOTTICK-01 FIX 4: the unit builder memoises ProductionQueue.GetProductionCost for one bot visit (ProductionCostCache). Row off = every call reads the cost live, as before.
		public const string CostCacheVisit = "cost_cache_visit";
	}
}
