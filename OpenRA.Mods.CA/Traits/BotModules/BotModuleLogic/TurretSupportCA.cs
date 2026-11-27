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
	// TEAM-DEFEND-02 turrets (USER 11-24): a ready turret is placed where it covers the CURRENT attackers: as close as possible to EXACTLY its maximum weapon range from them, on the
	// defended (own or allied) side, never next to the enemy. The same rule serves the solo case (1v1, own base attacked) and the team case (an ally's base attacked). Pure, no state.
	public static class TurretSupportCA
	{
		/// <summary>
		/// The building with the most attackers near it (ties: the lower id), when at least <paramref name="minAttackers"/> stand near it; -1 when none does.
		/// Input is (building id, attackers near it).
		/// </summary>
		public static int Focus(IReadOnlyList<(uint BuildingId, int Attackers)> buildings, int minAttackers)
		{
			var best = -1;
			for (var i = 0; i < buildings.Count; i++)
			{
				if (buildings[i].Attackers < Math.Max(1, minAttackers))
					continue;

				if (best < 0 || buildings[i].Attackers > buildings[best].Attackers
					|| (buildings[i].Attackers == buildings[best].Attackers && buildings[i].BuildingId < buildings[best].BuildingId))
					best = i;
			}

			return best;
		}

		/// <summary>The mean position of the attackers (world units).</summary>
		public static (long X, long Y) Centroid(IReadOnlyList<(long X, long Y)> positions)
		{
			if (positions.Count == 0)
				return (0, 0);

			long x = 0, y = 0;
			foreach (var p in positions)
			{
				x += p.X;
				y += p.Y;
			}

			return (x / positions.Count, y / positions.Count);
		}

		/// <summary>The smallest distance a turret keeps from the attackers: the lesser of the gap and three fifths of its range (so a short-range turret still has a band to stand in).</summary>
		public static long MinSafe(long rangeLen, long gapLen) => Math.Min(gapLen, rangeLen * 6 / 10);

		/// <summary>
		/// Index of the legal cell to place the turret on, or -1 (hold). A cell is usable when its distance to the attackers lies in [minSafe, range]: inside the weapon range, never next to the
		/// enemy. Pass 1 takes the defended-side cells (not farther from the defended anchor than from the attackers), pass 2 any usable cell. Within a pass the cell whose distance is
		/// closest to exactly the range wins; then the one nearer the anchor, then the lower index.
		/// </summary>
		public static int Pick(IReadOnlyList<(long X, long Y)> legal, (long X, long Y) attackers, (long X, long Y) anchor, long rangeLen, long minSafe)
		{
			for (var pass = 0; pass < 2; pass++)
			{
				var best = -1;
				long bestAtt = -1;
				long bestAnchor = long.MaxValue;
				for (var i = 0; i < legal.Count; i++)
				{
					var dAtt = Dist2(legal[i], attackers);
					if (dAtt < minSafe * minSafe || dAtt > rangeLen * rangeLen)
						continue;

					var dAnchor = Dist2(legal[i], anchor);
					if (pass == 0 && dAnchor > dAtt)
						continue;

					if (dAtt > bestAtt || (dAtt == bestAtt && dAnchor < bestAnchor))
					{
						best = i;
						bestAtt = dAtt;
						bestAnchor = dAnchor;
					}
				}

				if (best >= 0)
					return best;
			}

			return -1;
		}

		/// <summary>
		/// TURRET-NOHOLD-01: index of the fallback cell when <see cref="Pick"/> found none inside the weapon range, or -1 when no legal cell is usable. Any legal cell not closer to the attackers
		/// than <paramref name="minSafe"/> qualifies; the one nearest the attackers wins (the closest approach the safe gap allows), then the one nearer the defended anchor, then the lower index.
		/// </summary>
		public static int FallbackPick(IReadOnlyList<(long X, long Y)> legal, (long X, long Y) attackers, (long X, long Y) anchor, long minSafe)
		{
			var best = -1;
			var bestAtt = long.MaxValue;
			var bestAnchor = long.MaxValue;
			for (var i = 0; i < legal.Count; i++)
			{
				var dAtt = Dist2(legal[i], attackers);
				if (dAtt < minSafe * minSafe)
					continue;

				var dAnchor = Dist2(legal[i], anchor);
				if (dAtt < bestAtt || (dAtt == bestAtt && dAnchor < bestAnchor))
				{
					best = i;
					bestAtt = dAtt;
					bestAnchor = dAnchor;
				}
			}

			return best;
		}

		static long Dist2((long X, long Y) a, (long X, long Y) b)
		{
			var dx = a.X - b.X;
			var dy = a.Y - b.Y;
			return dx * dx + dy * dy; // squared: integer only, the order is all that matters
		}
	}
}
