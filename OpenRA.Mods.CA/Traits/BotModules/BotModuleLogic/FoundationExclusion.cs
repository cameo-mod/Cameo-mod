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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	// BUILD-ALL-B (user ruling 2026-11-25): no bot building places concrete in V08. A terrain foundation is told apart by its traits, never by a list:
	// a building that has no health and removes itself unconditionally (KillsSelf without RequiresCondition). D2k concrete slabs (^Concrete) lay their
	// tiles and vanish, so they were never counted as owned and the V08 Ixian bot queued about 30 of them. Walls and defences have health and stay.
	// The planned use of concrete is CONCRETE-PLAN-01 (after the playtest).
	public static class FoundationExclusion
	{
		/// <summary>The traits of a terrain foundation: a building without health that removes itself unconditionally.</summary>
		public static bool IsFoundation(bool isBuilding, bool hasHealth, bool killsSelfUnconditionally) => isBuilding && !hasHealth && killsSelfUnconditionally;

		public static bool IsFoundation(ActorInfo info) =>
			info != null && IsFoundation(
				info.HasTraitInfo<BuildingInfo>(),
				info.HasTraitInfo<IHealthInfo>(),
				info.TraitInfos<ConditionalTraitInfo>().Any(t => t.GetType().Name == KillsSelfTraitInfo && t.RequiresCondition == null));

		// The engine's KillsSelfInfo is internal to OpenRA.Mods.Common, so the trait class is matched by its type name.
		const string KillsSelfTraitInfo = "KillsSelfInfo";

		/// <summary>The buildable items without the foundations, in queue order. The list is returned unchanged when nothing is removed.</summary>
		public static List<ActorInfo> Without(List<ActorInfo> buildable, Dictionary<string, bool> cache)
		{
			List<ActorInfo> kept = null;
			for (var i = 0; i < buildable.Count; i++)
			{
				var item = buildable[i];
				if (!cache.TryGetValue(item.Name, out var foundation))
					cache[item.Name] = foundation = IsFoundation(item);

				if (foundation && kept == null)
					kept = buildable.GetRange(0, i);
				else if (!foundation && kept != null)
					kept.Add(item);
			}

			return kept ?? buildable;
		}
	}
}
