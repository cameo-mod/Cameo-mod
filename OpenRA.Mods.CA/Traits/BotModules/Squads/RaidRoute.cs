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

namespace OpenRA.Mods.CA.Traits.BotModules.Squads
{
	/// <summary>
	/// RAID-HARV-01 delta 02 section 5 / design 02 R4: the pure half of the explored-only raid route. The ROUTE SELECTION is made by the engine pathfinder with a cost that excludes every unexplored cell and with terrain-only blocking (the
	/// module passes <see cref="ExploredOnlyCost"/> and BlockedByActor.None), so no route is chosen or scored from a hidden blocker or an unexplored cell; the moves are issued as explored waypoints and the engine's local replanning between
	/// two waypoints is allowed. This class cuts a path into waypoints, verifies it, prices it (ETA) and builds the scout aim when no explored lane exists. No world access.
	/// </summary>
	public static class RaidRoute
	{
		/// <summary>The length of one orthogonal and one diagonal step in world distance units (1024 per cell).</summary>
		public const int OrthogonalStep = 1024;
		public const int DiagonalStep = 1448;

		/// <summary>The path-search cost that makes the engine search exclude a cell: 0 for an explored cell, the engine's invalid-cost value (passed in, so this class has no engine reference) for an unexplored one.</summary>
		public static Func<CPos, int> ExploredOnlyCost(Func<CPos, bool> explored, int invalidCost) => c => explored(c) ? 0 : invalidCost;

		/// <summary>The index of the first unexplored cell of a path, or -1 when every cell is explored.</summary>
		public static int FirstUnexplored(IReadOnlyList<CPos> path, Func<CPos, bool> explored)
		{
			for (var i = 0; i < path.Count; i++)
				if (!explored(path[i]))
					return i;

			return -1;
		}

		/// <summary>True when every cell of a non-empty path is explored (the verifier of a produced route).</summary>
		public static bool AllExplored(IReadOnlyList<CPos> path, Func<CPos, bool> explored) => path.Count > 0 && FirstUnexplored(path, explored) < 0;

		/// <summary>
		/// The waypoints of a path in TRAVEL order (the module reverses the pathfinder's target-first list): every <paramref name="spacing"/>-th cell, and always the last cell (the target or strike cell). An empty path has none.
		/// </summary>
		public static List<CPos> Waypoints(IReadOnlyList<CPos> path, int spacing)
		{
			var result = new List<CPos>();
			if (path == null || path.Count == 0)
				return result;

			var step = Math.Max(1, spacing);
			for (var i = step - 1; i < path.Count - 1; i += step)
				result.Add(path[i]);

			result.Add(path[path.Count - 1]);
			return result;
		}

		/// <summary>
		/// The ETA in ticks of a path from <paramref name="from"/>: per step the world distance over the movement speed (world distance units per tick) at the step's cell, rounded up; the caller passes the SLOWEST package member's speed at
		/// the cell, so the group ETA is the group's. A cell with no speed makes the path unusable (int.MaxValue). An empty path is 0.
		/// </summary>
		public static int Eta(CPos from, IReadOnlyList<CPos> path, Func<CPos, int> speedAtCell)
		{
			long total = 0;
			var prev = from;
			foreach (var cell in path)
			{
				var speed = speedAtCell(cell);
				if (speed <= 0)
					return int.MaxValue;

				var dx = Math.Abs(cell.X - prev.X);
				var dy = Math.Abs(cell.Y - prev.Y);
				var distance = (long)(DiagonalStep * Math.Min(dx, dy)) + (long)(OrthogonalStep * (Math.Max(dx, dy) - Math.Min(dx, dy)));
				total += (distance + speed - 1) / speed;
				if (total >= int.MaxValue)
					return int.MaxValue;

				prev = cell;
			}

			return (int)total;
		}

		static readonly (int X, int Y)[] Neighbours = { (0, -1), (1, -1), (1, 0), (1, 1), (0, 1), (-1, 1), (-1, 0), (-1, -1) };

		/// <summary>
		/// The cells reachable from <paramref name="start"/> through cells the predicate accepts (explored AND passable), breadth first in a fixed neighbour order, at most <paramref name="maxCells"/> cells; the start is always the first cell.
		/// The flood is the scout aim's source: no search ever passes through unknown terrain.
		/// </summary>
		public static List<CPos> FloodExplored(CPos start, Func<CPos, bool> passableExplored, int maxCells)
		{
			var order = new List<CPos> { start };
			var seen = new HashSet<CPos> { start };
			var limit = Math.Max(1, maxCells);
			for (var head = 0; head < order.Count && order.Count < limit; head++)
			{
				var cell = order[head];
				foreach (var (dx, dy) in Neighbours)
				{
					var next = new CPos(cell.X + dx, cell.Y + dy);
					if (order.Count >= limit || !seen.Add(next) || !passableExplored(next))
						continue;

					order.Add(next);
				}
			}

			return order;
		}

		/// <summary>The cell nearest to <paramref name="target"/> by squared distance (ties by X, then Y).</summary>
		public static CPos? NearestToTarget(IEnumerable<CPos> cells, CPos target)
		{
			CPos? best = null;
			long bestD = long.MaxValue;
			foreach (var c in cells)
			{
				long dx = c.X - target.X;
				long dy = c.Y - target.Y;
				var d = (dx * dx) + (dy * dy);
				if (d < bestD || (d == bestD && best.HasValue && (c.X < best.Value.X || (c.X == best.Value.X && c.Y < best.Value.Y))))
				{
					best = c;
					bestD = d;
				}
			}

			return best;
		}

		/// <summary>
		/// The scout aim when no explored lane reaches the target: the unexplored neighbours of the explored-reachable cells nearest to the target (nearest cell first, at most <paramref name="maxAim"/> aim cells), in a fixed order. It is published
		/// as scout interest (a request, no vision); no path is searched through unknown terrain to find it.
		/// </summary>
		public static List<CPos> ScoutAim(IReadOnlyList<CPos> reachable, CPos target, Func<CPos, bool> explored, Func<CPos, bool> inMap, int maxAim)
		{
			var aim = new List<CPos>();
			var seen = new HashSet<CPos>();
			var ordered = reachable.Select(c => (Cell: c, D: Dist2(c, target))).OrderBy(t => t.D).ThenBy(t => t.Cell.X).ThenBy(t => t.Cell.Y);
			foreach (var (cell, _) in ordered)
			{
				foreach (var (dx, dy) in Neighbours)
				{
					var n = new CPos(cell.X + dx, cell.Y + dy);
					if (aim.Count >= maxAim)
						return aim;

					if (inMap(n) && !explored(n) && seen.Add(n))
						aim.Add(n);
				}

				if (aim.Count >= maxAim)
					break;
			}

			return aim;
		}

		static long Dist2(CPos a, CPos b)
		{
			long dx = a.X - b.X;
			long dy = a.Y - b.Y;
			return (dx * dx) + (dy * dy);
		}
	}
}
