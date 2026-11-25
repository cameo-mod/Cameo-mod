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
	// CONSOLIDATE-01 c3 (CONSOLIDATE_01_DESIGN.md 3.1 stage S5, section 4 row C3): the pure parts of "every listed building once". No world, no state, no random.

	/// <summary>One buildable structure type as S5 sees it. Count = owned + in flight; Depth and RoleRank come from <see cref="TechDepthCA"/> and <see cref="CoreOnceCA.RoleRank"/>.</summary>
	public readonly record struct CoreOnceInput(string Name, bool Buildable, bool InFractions, bool FirstCopyKind, bool OwnRuleStage, int Count, int Depth, int RoleRank, int Cost);

	/// <summary>S5: the core set (R1, R11) and its order.</summary>
	public static class CoreOnceCA
	{
		/// <summary>The rank of an opening role, in the opening order's own numbering (OpeningRole): Power 1, Barracks 2, Refinery 3, War factory 4, Radar 5, Airfield 6, passive income 8, any other type 100.</summary>
		public static int RoleRank(bool power, bool barracks, bool refinery, bool warFactory, bool radar, bool airfield, bool trickler)
		{
			if (power)
				return (int)OpeningRole.Power;
			if (barracks)
				return (int)OpeningRole.Barracks;
			if (refinery)
				return (int)OpeningRole.Refinery;
			if (warFactory)
				return (int)OpeningRole.WarFactory;
			if (radar)
				return (int)OpeningRole.Radar;
			if (airfield)
				return (int)OpeningRole.Airfield;
			if (trickler)
				return (int)OpeningRole.PassiveIncome;

			return 100;
		}

		/// <summary>
		/// Whether a type is in the S5 set: buildable by this player now (a doctrine alternative the player did not pick is not buildable, so it is simply
		/// absent, never a missing core building), none owned or in flight, listed in BuildingFractions or a first-copy kind (no wall, defence, proxy), and
		/// not a type with a stage of its own (construction yard, refinery: S2 and S7 keep their laws).
		/// </summary>
		public static bool InSet(CoreOnceInput t) =>
			t.Name != null && t.Buildable && t.Count <= 0 && !t.OwnRuleStage && (t.InFractions || t.FirstCopyKind);

		/// <summary>The S5 candidates in order: tech depth (nearest the yard first), then opening role, then cost, then ordinal name (deterministic).</summary>
		public static IReadOnlyList<string> Order(IEnumerable<CoreOnceInput> types)
		{
			if (types == null)
				return Array.Empty<string>();

			return types
				.Where(InSet)
				.OrderBy(t => t.Depth)
				.ThenBy(t => t.RoleRank)
				.ThenBy(t => Math.Max(0, t.Cost))
				.ThenBy(t => t.Name, StringComparer.Ordinal)
				.Select(t => t.Name)
				.ToArray();
		}
	}

	/// <summary>The prerequisite graph, as a depth from the construction yard. Built once per player (BaseBuilderBotModuleCA.CoreTechDepth) and read per sweep.</summary>
	public static class TechDepthCA
	{
		/// <summary>
		/// One raw Buildable prerequisite to the prerequisite name it needs. Null for a negation (a token with '!', e.g. "~!doctrine": it requires the
		/// ABSENCE of something and adds no depth). A "~" prefix only hides the item until met and is stripped. Names are compared in lower case.
		/// </summary>
		public static string Positive(string raw)
		{
			if (string.IsNullOrWhiteSpace(raw))
				return null;

			var s = raw.Trim();
			if (s.IndexOf('!') >= 0)
				return null;

			s = s.TrimStart('~');
			return s.Length == 0 ? null : s.ToLowerInvariant();
		}

		/// <summary>
		/// The depth of every actor in <paramref name="prereqsOf"/> (actor name to its raw Buildable prerequisites). A roots entry (the construction yards)
		/// is 0. Otherwise depth = 1 + the deepest needed prerequisite, where a prerequisite is met by its NEAREST provider (providersOf: prerequisite name
		/// to the actors that give it, lower-case keys). A prerequisite nobody provides (a doctrine upgrade, a player-level token) adds no depth. A cycle is
		/// cut at the actor being resolved. Result keys are the actor names as given; an actor outside prereqsOf is absent.
		/// </summary>
		public static IReadOnlyDictionary<string, int> Compute(IReadOnlyDictionary<string, IReadOnlyList<string>> prereqsOf,
			IReadOnlyDictionary<string, IReadOnlyList<string>> providersOf, IReadOnlyCollection<string> roots)
		{
			var depth = new Dictionary<string, int>(StringComparer.Ordinal);
			var visiting = new HashSet<string>(StringComparer.Ordinal);

			// Returns -1 for "not resolvable here" (a cycle member being resolved): the caller then ignores that provider.
			int Resolve(string name)
			{
				if (depth.TryGetValue(name, out var known))
					return known;

				if (roots != null && roots.Contains(name))
					return depth[name] = 0;

				// A cycle is cut here (-1). A member resolved while its cycle is cut caches the depth found without that edge, so a depth inside a cycle
				// depends on the traversal order. That order is fixed (ordinal keys in Compute), so the result is deterministic. Cycles are not expected in the rules (not verified).
				if (!visiting.Add(name))
					return -1;

				var best = 0;
				if (prereqsOf.TryGetValue(name, out var raws) && raws != null)
				{
					foreach (var raw in raws)
					{
						var need = Positive(raw);
						if (need == null || !providersOf.TryGetValue(need, out var providers) || providers == null)
							continue;

						var nearest = -1;
						foreach (var provider in providers)
						{
							if (provider == name)
								continue;

							var d = Resolve(provider);
							if (d >= 0 && (nearest < 0 || d < nearest))
								nearest = d;
						}

						if (nearest >= 0)
							best = Math.Max(best, nearest + 1);
					}
				}

				visiting.Remove(name);
				return depth[name] = best;
			}

			foreach (var name in prereqsOf.Keys.OrderBy(k => k, StringComparer.Ordinal))
				Resolve(name);

			return depth;
		}
	}
}
