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

using System;
using System.Collections.Generic;

namespace OpenRA.Mods.CA.Traits
{
	// UPGRADE-QUEUE-01: the pure choice of the upgrade service, so it is testable without a world.
	public static class UpgradeServiceCA
	{
		/// <summary>One buildable item of a queue. Weight is the faction's UnitsToBuild weight, or -1 when the table does not list it.</summary>
		public readonly record struct Candidate(string Name, int Cost, int Weight);

		/// <summary>
		/// Index of the item to start, or -1. Free items (cost 0, promotions) are always eligible and rank first. A paid item needs
		/// <paramref name="bank"/> to cover its cost and no pause vote. Items in <paramref name="excluded"/> (already in production in any queue)
		/// are never chosen. Among the eligible: free first, then higher weight, then lower cost, then a seeded stable hash of the name (ties only), then name (ordinal). No random draw.
		/// </summary>
		public static int Choose(IReadOnlyList<Candidate> items, ISet<string> excluded, int bank, bool paused, int tieSeed = 0)
		{
			var best = -1;
			for (var i = 0; i < items.Count; i++)
			{
				var c = items[i];
				if (excluded.Contains(c.Name))
					continue;

				var free = c.Cost <= 0;
				if (!free && (paused || c.Cost > bank))
					continue;

				if (best < 0 || Better(c, items[best], tieSeed))
					best = i;
			}

			return best;
		}

		static bool Better(Candidate a, Candidate b, int tieSeed)
		{
			var aFree = a.Cost <= 0;
			var bFree = b.Cost <= 0;
			if (aFree != bFree)
				return aFree;

			if (a.Weight != b.Weight)
				return a.Weight > b.Weight;

			if (a.Cost != b.Cost)
				return a.Cost < b.Cost;

			// Exact ties (e.g. mutually exclusive free choices): a stable per-bot order, so the bots do not all take the first name.
			var ha = Mix(a.Name, tieSeed);
			var hb = Mix(b.Name, tieSeed);
			if (ha != hb)
				return ha < hb;

			return string.CompareOrdinal(a.Name, b.Name) < 0;
		}

		// FNV-1a over the name, seeded; deterministic across runs and machines (no string.GetHashCode).
		static uint Mix(string name, int seed)
		{
			var h = 2166136261u ^ (uint)seed;
			foreach (var ch in name)
				h = (h ^ ch) * 16777619u;

			return h;
		}
	}
}
