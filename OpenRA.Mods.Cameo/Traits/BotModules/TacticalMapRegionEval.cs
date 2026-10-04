#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING. In addition, please see <http://www.gnu.org/licenses/>.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Mods.Cameo.Traits.BotModules
{
	/// <summary>
	/// The pure decision seams of <see cref="TacticalMapBotModule.BuildRegions"/> (arch-review hotspot #4):
	/// ramp-seal classification and growth, ramp-piece construction, merge-drop selection and region
	/// boundary extraction. Everything here is a pure function of cells + per-cell predicates, so the
	/// whole merge decision tree is testable on synthetic grids without a Map, a World or a ruleset.
	/// The module keeps the map plumbing (passability, heights, the flood itself); these statics own
	/// the decisions. One authority: BuildRegions calls these, nothing else re-implements them.
	/// </summary>
	public static class TacticalMapRegionEval
	{
		public readonly struct Census
		{
			public readonly int PassableCells;
			public readonly int SlopeTileCells;
			public readonly int TransitionCells;
			public readonly int CliffAdjacentCells;
			public readonly IReadOnlyDictionary<byte, int> Heights;

			public Census(int passable, int slopeTiles, int transitions, int cliffAdjacent, IReadOnlyDictionary<byte, int> heights)
			{
				PassableCells = passable;
				SlopeTileCells = slopeTiles;
				TransitionCells = transitions;
				CliffAdjacentCells = cliffAdjacent;
				Heights = heights;
			}
		}

		/// <summary>
		/// Which passable height-transition cells are slope, and which of those are cliff-seeded ramps.
		/// Seeded from the cliff, so a hillside nowhere near one is never barrier however much it slopes.
		/// </summary>
		public static (HashSet<CPos> Slope, HashSet<CPos> RampSeeds) ClassifySlope(
			IEnumerable<CPos> cells, Func<CPos, bool> isPassable, Func<CPos, bool> isTransition, Func<CPos, bool> hasCliffAbove)
		{
			var slope = new HashSet<CPos>();
			var rampSeeds = new HashSet<CPos>();
			foreach (var c in cells)
			{
				if (!isPassable(c) || !isTransition(c))
					continue;

				slope.Add(c);
				if (hasCliffAbove(c))
					rampSeeds.Add(c);
			}

			return (slope, rampSeeds);
		}

		/// <summary>
		/// Bounded growth of the ramp seeds across the connected slope: slope tiles are not rare on
		/// rolling terrain and an unbounded run swallows half a map, so the bound closes a ramp up to
		/// roughly twice <paramref name="maxSpread"/> across while leaving open hillside alone.
		/// </summary>
		public static HashSet<CPos> GrowRampSeal(HashSet<CPos> slope, HashSet<CPos> rampSeeds, int maxSpread)
		{
			var ramp = new HashSet<CPos>(rampSeeds);
			var queue = new Queue<(CPos Cell, int Depth)>();
			foreach (var seed in rampSeeds)
				queue.Enqueue((seed, 0));

			while (queue.Count > 0)
			{
				var (cell, depth) = queue.Dequeue();
				if (depth >= maxSpread)
					continue;

				foreach (var dir in CVec.Directions)
				{
					var next = cell + dir;
					if (slope.Contains(next) && ramp.Add(next))
						queue.Enqueue((next, depth + 1));
				}
			}

			return ramp;
		}

		/// <summary>
		/// What the map is actually made of, once per build, for the terrain debug line — the three
		/// failed ramp-seal attempts were each argued from a different guess about the terrain.
		/// </summary>
		public static Census TerrainCensus(
			IEnumerable<CPos> cells, Func<CPos, bool> isPassable, Func<CPos, byte> heightOf,
			Func<CPos, byte> rampIndexOf, Func<CPos, bool> isTransition, Func<CPos, bool> hasCliffAbove)
		{
			var passable = 0;
			var slopeTiles = 0;
			var transitions = 0;
			var cliffAdjacent = 0;
			var heights = new Dictionary<byte, int>();
			foreach (var c in cells)
			{
				if (!isPassable(c))
					continue;

				passable++;
				var h = heightOf(c);
				heights[h] = heights.GetValueOrDefault(h) + 1;
				if (rampIndexOf(c) != 0)
					slopeTiles++;
				if (isTransition(c))
					transitions++;
				if (hasCliffAbove(c))
					cliffAdjacent++;
			}

			return new Census(passable, slopeTiles, transitions, cliffAdjacent, heights);
		}

		/// <summary>
		/// One barrier piece per physical ramp: each connected ramp run plus its one-ring dilation of
		/// passable cells. The dilation closes the two one-cell-thin leaks — diagonal corner-cuts through
		/// 8-directional movement, and the flat lead-in/lead-out cells at a ramp's ends that are not
		/// height transitions at all. Per ramp rather than one combined blob so each can be dropped on
		/// its own — though ramps are in fact never droppable (a dropped ramp merges high ground into
		/// low, the one separation the ramp barrier exists for).
		/// </summary>
		public static List<HashSet<CPos>> RampPieces(HashSet<CPos> rampCells, Func<CPos, bool> inMapAndPassable)
		{
			var pieces = new List<HashSet<CPos>>();
			var visited = new HashSet<CPos>();
			foreach (var start in rampCells)
			{
				if (!visited.Add(start))
					continue;

				var component = ConnectedComponent(start, rampCells, visited);
				var piece = new HashSet<CPos>(component);
				foreach (var cell in component)
					foreach (var dir in CVec.Directions)
					{
						var n = cell + dir;
						if (inMapAndPassable(n))
							piece.Add(n);
					}

				pieces.Add(piece);
			}

			return pieces;
		}

		/// <summary>One orthogonally-connected run inside <paramref name="cells"/>, seeded at
		/// <paramref name="start"/>. <paramref name="visited"/> is caller-owned and shared across calls —
		/// callers add the seed before calling so already-claimed cells never re-expand.</summary>
		public static List<CPos> ConnectedComponent(CPos start, HashSet<CPos> cells, HashSet<CPos> visited)
		{
			var comp = new List<CPos> { start };
			var queue = new Queue<CPos>();
			queue.Enqueue(start);
			while (queue.Count > 0)
			{
				var c = queue.Dequeue();
				foreach (var d in CVec.Directions)
				{
					var n = c + d;
					if (cells.Contains(n) && visited.Add(n))
					{
						comp.Add(n);
						queue.Enqueue(n);
					}
				}
			}

			return comp;
		}

		/// <summary>
		/// The cells of a finished region that sit on its outline: any cell with a neighbour that is not
		/// (in the map AND in this region). Read off the finished cell set — a neighbour absent during
		/// the flood could mean "already ours" or "someone else's", and only the finished set tells the
		/// two apart.
		/// </summary>
		public static List<CPos> BoundaryCells(IReadOnlyCollection<CPos> cellSet, Func<CPos, bool> inMap)
		{
			var boundary = new List<CPos>();
			foreach (var cell in cellSet)
			{
				var isBoundary = false;
				foreach (var dir in CVec.Directions)
				{
					var n = cell + dir;
					if (inMap(n) && cellSet.Contains(n))
						continue;

					isBoundary = true;
					break;
				}

				if (isBoundary)
					boundary.Add(cell);
			}

			return boundary;
		}

		/// <summary>
		/// Which active droppable barrier pieces may go this round: those separating at least two regions
		/// (fewer means nothing on the far side to merge into) where the smallest separated region is
		/// under <paramref name="minRegionSize"/>. <paramref name="touchingOf"/> resolves a piece's
		/// neighbouring region ids; <paramref name="sizeOf"/> resolves a region's cell count.
		/// </summary>
		public static List<(int Index, int Separated, HashSet<int> Touching)> EligibleDrops(
			IReadOnlyList<bool> active, IReadOnlyList<bool> droppable,
			Func<int, HashSet<int>> touchingOf, Func<int, int> sizeOf, int minRegionSize)
		{
			var eligible = new List<(int Index, int Separated, HashSet<int> Touching)>();
			for (var i = 0; i < active.Count; i++)
			{
				if (!active[i] || !droppable[i])
					continue;

				var touching = touchingOf(i);
				if (touching.Count < 2)
					continue;

				var separated = int.MaxValue;
				foreach (var id in touching)
					separated = Math.Min(separated, sizeOf(id));

				if (separated < minRegionSize)
					eligible.Add((i, separated, touching));
			}

			return eligible;
		}

		/// <summary>
		/// The independent drops of one merge round, worst (smallest separated region) first, claiming
		/// the touched regions as each drop is taken: two pieces sharing a touched region cannot go
		/// together — the second one's undersized reading was measured against a fill the first drop is
		/// about to change, and dropping is monotone so a wrong drop never comes back. Pieces with
		/// disjoint touched sets spoil nothing for each other and batch freely.
		/// </summary>
		public static List<int> PickMergeDrops(List<(int Index, int Separated, HashSet<int> Touching)> eligible)
		{
			eligible.Sort((a, b) => a.Separated.CompareTo(b.Separated));

			var claimed = new HashSet<int>();
			var dropped = new List<int>();
			foreach (var (index, _, touching) in eligible)
			{
				if (touching.Overlaps(claimed))
					continue;

				claimed.UnionWith(touching);
				dropped.Add(index);
			}

			return dropped;
		}
	}
}
