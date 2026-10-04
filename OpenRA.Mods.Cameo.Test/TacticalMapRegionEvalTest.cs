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

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OpenRA.Mods.Cameo.Traits.BotModules;

namespace OpenRA.Mods.Cameo.Test
{
	// The extracted decision seams of TacticalMapBotModule.BuildRegions (arch-review hotspot #4) on
	// synthetic CPos grids: ramp classification/seal/pieces, the merge-drop selection, the boundary
	// pass. Every branch is exercised — the seams are what decide, BuildRegions is plumbing.
	[TestFixture]
	public class TacticalMapRegionEvalTest
	{
		static HashSet<CPos> Cells(params (int X, int Y)[] xy) => new(xy.Select(p => new CPos(p.X, p.Y)));

		[Test]
		public void ClassifySlopeKeepsOnlyPassableTransitionsAndSeedsFromCliff()
		{
			var all = Cells((0, 0), (1, 0), (2, 0), (3, 0), (4, 0));
			var (slope, seeds) = TacticalMapRegionEval.ClassifySlope(
				all,
				c => c != new CPos(4, 0),            // (4,0) impassable
				c => c != new CPos(0, 0),            // (0,0) passable but not a transition
				c => c == new CPos(2, 0));           // only (2,0) has a cliff above

			Assert.That(slope, Is.EquivalentTo(Cells((1, 0), (2, 0), (3, 0))));
			Assert.That(seeds, Is.EquivalentTo(Cells((2, 0))));
		}

		[Test]
		public void GrowRampSealExpandsAcrossSlopeWithinTheBound()
		{
			// A 1-cell-wide slope strip 0..9; seed at x=4; spread 2 -> covers 2..6, no further.
			var slope = Cells(Enumerable.Range(0, 10).Select(x => (x, 0)).ToArray());
			var ramp = TacticalMapRegionEval.GrowRampSeal(slope, Cells((4, 0)), 2);
			Assert.That(ramp, Is.EquivalentTo(Cells((2, 0), (3, 0), (4, 0), (5, 0), (6, 0))));
		}

		[Test]
		public void GrowRampSealNeverLeavesTheSlope()
		{
			// The seed's neighbour off-slope is reachable in the flood but not in the seal.
			var slope = Cells((0, 0), (1, 0));
			var ramp = TacticalMapRegionEval.GrowRampSeal(slope, Cells((0, 0)), 5);
			Assert.That(ramp, Is.EquivalentTo(Cells((0, 0), (1, 0))));
			Assert.That(ramp.Contains(new CPos(0, 1)), Is.False);
		}

		[Test]
		public void TerrainCensusCountsEachClassAndTheHeightHistogram()
		{
			var all = Cells((0, 0), (1, 0), (2, 0), (3, 0));
			var census = TacticalMapRegionEval.TerrainCensus(
				all,
				c => c != new CPos(3, 0),
				c => c == new CPos(1, 0) ? (byte)2 : (byte)1,
				c => c == new CPos(2, 0) ? (byte)5 : (byte)0,
				c => c == new CPos(0, 0),
				c => c == new CPos(2, 0));

			Assert.That(census.PassableCells, Is.EqualTo(3));
			Assert.That(census.SlopeTileCells, Is.EqualTo(1));
			Assert.That(census.TransitionCells, Is.EqualTo(1));
			Assert.That(census.CliffAdjacentCells, Is.EqualTo(1));
			Assert.That(census.Heights[1], Is.EqualTo(2));
			Assert.That(census.Heights[2], Is.EqualTo(1));
		}

		[Test]
		public void RampPiecesComponentPlusRingAndTwoRunsAreTwoPieces()
		{
			// Run A: (0,0),(1,0). Run B: (9,9) — far away. Dilation adds passable neighbours.
			var ramp = Cells((0, 0), (1, 0), (9, 9));
			var open = new HashSet<CPos>();
			for (var x = -1; x <= 10; x++)
				for (var y = -1; y <= 10; y++)
					open.Add(new CPos(x, y));

			var pieces = TacticalMapRegionEval.RampPieces(ramp, c => open.Contains(c));
			Assert.That(pieces.Count, Is.EqualTo(2));
			var a = pieces.Single(p => p.Contains(new CPos(0, 0)));
			Assert.That(a.Contains(new CPos(2, 0)), Is.True);   // dilation reached one ring out
			Assert.That(a.Contains(new CPos(0, 1)), Is.True);
			Assert.That(a.Contains(new CPos(3, 0)), Is.False);  // but not two
			Assert.That(a.Contains(new CPos(9, 9)), Is.False);
			Assert.That(pieces.Single(p => p.Contains(new CPos(9, 9))).Contains(new CPos(9, 8)), Is.True);
		}

		[Test]
		public void BoundaryCellsMarksRingAndMapEdgeAsBoundary()
		{
			// 3x3 block inside a 5x5 map: the centre is interior, the eight ring cells boundary.
			var cellSet = Cells((1, 1), (2, 1), (3, 1), (1, 2), (2, 2), (3, 2), (1, 3), (2, 3), (3, 3));
			var boundary = TacticalMapRegionEval.BoundaryCells(cellSet, c => c.X >= 0 && c.X <= 4 && c.Y >= 0 && c.Y <= 4);
			Assert.That(boundary.Count, Is.EqualTo(8));
			Assert.That(boundary.Contains(new CPos(2, 2)), Is.False);

			// The same block hard against the map edge: every cell sees a neighbour outside the map.
			var edge = Cells((0, 0), (1, 0), (0, 1), (1, 1));
			var boundary2 = TacticalMapRegionEval.BoundaryCells(edge, c => c.X >= 0 && c.X <= 1 && c.Y >= 0 && c.Y <= 1);
			Assert.That(boundary2.Count, Is.EqualTo(4));
		}

		[Test]
		public void EligibleDropsFiltersActiveDroppableTwoRegionsAndUndersized()
		{
			var active = new[] { true, true, false, true, true };
			var droppable = new[] { true, false, true, true, true };
			var touching = new Dictionary<int, HashSet<int>>
			{
				[0] = new() { 1, 2 },       // eligible: separates 1 and 2, min size 3 < 5
				[1] = new() { 1, 2 },       // not droppable
				[2] = new() { 1, 2 },       // inactive
				[3] = new() { 4 },          // touches one region only — nothing to merge into
				[4] = new() { 1, 3 },       // smallest side is 20 ≥ 5 — stays
			};
			var sizes = new Dictionary<int, int> { [1] = 20, [2] = 3, [3] = 20, [4] = 50 };

			var eligible = TacticalMapRegionEval.EligibleDrops(
				active, droppable, i => touching[i], id => sizes[id], 5);

			Assert.That(eligible.Select(e => e.Index), Is.EquivalentTo(new[] { 0 }));
			Assert.That(eligible[0].Separated, Is.EqualTo(3));
		}

		[Test]
		public void PickMergeDropsTakesWorstFirstAndBatchesOnlyDisjointTouching()
		{
			// p0 separates {1,2} (size 4), p1 {2,3} (size 9), p2 {4,5} (size 9) — p1 and p2 tie on size,
			// and both are disjoint from p0's claim. p3 {1,2} (size 40) must defer a round.
			var eligible = new List<(int Index, int Separated, HashSet<int> Touching)>
			{
				(3, 40, new HashSet<int> { 1, 2 }),
				(0, 4, new HashSet<int> { 1, 2 }),
				(1, 9, new HashSet<int> { 2, 3 }),
				(2, 9, new HashSet<int> { 4, 5 }),
			};

			var dropped = TacticalMapRegionEval.PickMergeDrops(eligible);

			// p0 (worst, 4) goes; p1 loses its claim (shares region 2 with p0's touched set);
			// p2 (disjoint) goes in the same round; p3 also shares {1,2} — deferred.
			Assert.That(dropped, Is.EquivalentTo(new[] { 0, 2 }));
		}

		[Test]
		public void PickMergeDropsEmptyInputIsEmpty()
		{
			Assert.That(TacticalMapRegionEval.PickMergeDrops(new List<(int, int, HashSet<int>)>()), Is.Empty);
		}
	}
}
