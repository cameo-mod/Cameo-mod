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

	/// <summary>One buildable structure type as S5's fallback sees it. Count = owned + in flight; Weight = its BuildingFractions weight, never below 0 (-1 = not listed); Depth comes from <see cref="TechDepthCA"/>.</summary>
	public readonly record struct CoreOnceInput(string Name, bool Buildable, int Weight, bool FirstCopyKind, bool OwnRuleStage, int Count, int Depth, int Cost);

	/// <summary>S5: the depth fallback of "every listed building once". The first pass is the original BuildingFractions roll (BaseBuildPlanCA.RollCandidates).</summary>
	public static class CoreOnceCA
	{
		/// <summary>
		/// Whether a type is in the fallback: buildable by this player now (a doctrine alternative the player did not pick is not buildable, so it is simply
		/// absent, never a missing core building), none owned or in flight, not a type with a stage of its own (construction yard, refinery: S2 and S7 keep
		/// their laws), and a type the roll cannot offer: a BuildingFractions entry with weight 0 or less, or a first-copy kind that is not listed at all.
		/// A listed type with a positive weight is the roll's, never the fallback's (no duplicate).
		/// </summary>
		public static bool InSet(CoreOnceInput t) =>
			t.Name != null && t.Buildable && t.Count <= 0 && !t.OwnRuleStage && t.Weight <= 0 && t.FirstCopyKind; // CM: a listed weight-0 wall/defence is no first copy either

		/// <summary>The fallback candidates in order: tech depth (nearest the yard first), then cost, then ordinal name (deterministic).</summary>
		public static IReadOnlyList<string> Order(IEnumerable<CoreOnceInput> types)
		{
			if (types == null)
				return Array.Empty<string>();

			return types
				.Where(InSet)
				.OrderBy(t => t.Depth)
				.ThenBy(t => Math.Max(0, t.Cost))
				.ThenBy(t => t.Name, StringComparer.Ordinal)
				.Select(t => t.Name)
				.ToArray();
		}
	}

	/// <summary>
	/// CAPTURE-TECH-01 (switch cs_capture_unlock): the per-player prerequisite maps for the capture rule, built once from the rules with NO faction filter (a captured
	/// yard builds the other faction's types). ProvidedBy: building type to every lower-case token it provides. Gates: token to the building types needing it positively.
	/// </summary>
	public sealed class CaptureUnlockMapsCA
	{
		public readonly IReadOnlyDictionary<string, IReadOnlyList<string>> ProvidedBy;
		public readonly IReadOnlyDictionary<string, HashSet<string>> Gates;

		public CaptureUnlockMapsCA(IReadOnlyDictionary<string, IReadOnlyList<string>> providedBy, IReadOnlyDictionary<string, HashSet<string>> gates)
		{
			ProvidedBy = providedBy;
			Gates = gates;
		}

		/// <summary>Builds the maps from actor name to raw Buildable prerequisites and actor name to provided tokens (any case). Pure.</summary>
		public static CaptureUnlockMapsCA Build(IReadOnlyDictionary<string, IReadOnlyList<string>> prereqsOf, IReadOnlyDictionary<string, IReadOnlyList<string>> providesOf)
		{
			var providedBy = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
			if (providesOf != null)
				foreach (var kv in providesOf)
					providedBy[kv.Key] = kv.Value.Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.Trim().ToLowerInvariant()).Distinct().ToArray();

			var gates = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
			if (prereqsOf != null)
			{
				foreach (var kv in prereqsOf)
				{
					if (kv.Value == null)
						continue;

					foreach (var raw in kv.Value)
					{
						var need = TechDepthCA.Positive(raw);
						if (need == null)
							continue;

						if (!gates.TryGetValue(need, out var set))
							gates[need] = set = new HashSet<string>(StringComparer.Ordinal);

						set.Add(kv.Key);
					}
				}
			}

			return new CaptureUnlockMapsCA(providedBy, gates);
		}

		/// <summary>Whether the token gates at least one building type other than <paramref name="self"/>.</summary>
		public bool GatesOther(string token, string self) => Gates.TryGetValue(token, out var set) && set.Any(n => n != self);
	}

	/// <summary>
	/// CAPTURE-TECH-01: a power plant, silo or refinery type (count 0 and buildable are checked by the caller and CoreOnceCA.InSet) is an UNLOCK when it provides a token
	/// the player lacks now that gates another building type, and the player already owns a type of the same category (the opening and S1 build the first one, so a
	/// normal own-faction game is unchanged). Pure: plain inputs only.
	/// </summary>
	public static class CaptureUnlockCA
	{
		public static bool IsUnlock(IEnumerable<string> provided, Func<string, bool> lacks, Func<string, bool> gatesOther, int categoryOwned)
		{
			if (provided == null || lacks == null || gatesOther == null || categoryOwned <= 0)
				return false;

			foreach (var token in provided)
				if (gatesOther(token) && lacks(token))
					return true;

			return false;
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
