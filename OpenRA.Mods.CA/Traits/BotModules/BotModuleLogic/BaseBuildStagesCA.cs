#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Mods.CA.Traits
{
	// CONSOLIDATE-01 c4 (CONSOLIDATE_01_DESIGN.md 3.1, stages S2b and S6): the pure decisions of the stages that need no world. No state, no random.

	/// <summary>S6 passive income (USER: passive income is built up to its limit, R8).</summary>
	public static class PassiveIncomeCA
	{
		/// <summary>
		/// The passive-income (cash trickler) building types still wanted, in the order to try them: every type whose owned + in-flight count is under its
		/// cap, the cheapest first, then by name (ordinal). The cap is the type's BuildingLimits value when it has one (a limit of 0 allows none), else 1:
		/// a type with no configured limit is built once, never spammed. A type the queue cannot build is not offered by the caller.
		/// </summary>
		public static IReadOnlyList<string> Order(IReadOnlyList<(string Name, int Cost, int Count, int Limit)> types)
		{
			if (types == null || types.Count == 0)
				return Array.Empty<string>();

			return types
				.Where(t => t.Name != null && t.Count < (t.Limit >= 0 ? t.Limit : 1))
				.OrderBy(t => t.Cost)
				.ThenBy(t => t.Name, StringComparer.Ordinal)
				.Select(t => t.Name)
				.ToArray();
		}
	}

	/// <summary>S2b planner wants: what the refinery-law planner asks the base builder for, in the order they go.</summary>
	public static class PlannerWantsCA
	{
		/// <summary>
		/// The due MCV's missing prerequisite first (the next expansion's tech gate), then the front defence of the crawl, then the crawl link it
		/// precedes (CRAWL-SAFETY: the front defence rides ahead of the link). Null and repeated names are dropped.
		/// </summary>
		public static IReadOnlyList<string> Order(string mcvPrerequisite, string frontDefence, string linkBuilding)
		{
			var order = new List<string>(3);
			foreach (var want in new[] { mcvPrerequisite, frontDefence, linkBuilding })
				if (want != null && !order.Contains(want))
					order.Add(want);

			return order;
		}
	}
}
