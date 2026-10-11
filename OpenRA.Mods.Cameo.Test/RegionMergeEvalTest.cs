#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using NUnit.Framework;
using OpenRA.Mods.Cameo.Traits.BotModules;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public class RegionMergeEvalTest
	{
		// Hotspot #4 (2026-10-04): the BuildRegions merge loop's decisions — round bounds,
		// piece eligibility, the undersized measurement, and disjoint-claim batching.

		[Test]
		public void MergeContinuesRespectsBothBounds()
		{
			Assert.That(RegionMergeEval.MergeContinues(minRegionSize: 0, round: 0, pieceCount: 10), Is.False);
			Assert.That(RegionMergeEval.MergeContinues(minRegionSize: 60, round: 10, pieceCount: 10), Is.False);
			Assert.That(RegionMergeEval.MergeContinues(minRegionSize: 60, round: 9, pieceCount: 10), Is.True);
		}

		[Test]
		public void EligibilityNeedsEveryFlag()
		{
			// Inactive or non-droppable pieces never merge.
			Assert.That(RegionMergeEval.IsMergeEligible(false, true, 2, 10, 60), Is.False);
			Assert.That(RegionMergeEval.IsMergeEligible(true, false, 2, 10, 60), Is.False);

			// A piece touching fewer than two regions holds nothing apart.
			Assert.That(RegionMergeEval.IsMergeEligible(true, true, 1, 10, 60), Is.False);
			Assert.That(RegionMergeEval.IsMergeEligible(true, true, 0, int.MaxValue, 60), Is.False);

			// The boundary is strict-less-than: exactly minRegionSize is big enough to stand.
			Assert.That(RegionMergeEval.IsMergeEligible(true, true, 2, 60, 60), Is.False);
			Assert.That(RegionMergeEval.IsMergeEligible(true, true, 2, 59, 60), Is.True);
		}

		[Test]
		public void SmallestTouchingSizeIsMinOrSentinel()
		{
			Assert.That(RegionMergeEval.SmallestTouchingSize(new[] { 30, 5, 90 }), Is.EqualTo(5));
			Assert.That(RegionMergeEval.SmallestTouchingSize(new int[0]), Is.EqualTo(int.MaxValue));
		}

		[Test]
		public void RoundDropsAreWorstFirstAndClaimDisjoint()
		{
			// Piece 0 touches regions {1,2} (worst, separated 10); piece 1 touches {2,3}
			// (shares region 2 — must wait); piece 2 touches {4,5} (disjoint — same round).
			var eligible = new List<(int Index, int Separated, HashSet<int> Touching)>
			{
				(1, 20, new HashSet<int> { 2, 3 }),
				(2, 30, new HashSet<int> { 4, 5 }),
				(0, 10, new HashSet<int> { 1, 2 }),
			};

			var drops = RegionMergeEval.SelectRoundDrops(eligible);

			Assert.That(drops, Is.EqualTo(new[] { 0, 2 }));
			// Piece 1 is left for next round — its measurement would have been spoiled by 0's drop.
		}

		[Test]
		public void DisjointPiecesAllDropTogether()
		{
			var eligible = new List<(int Index, int Separated, HashSet<int> Touching)>
			{
				(0, 10, new HashSet<int> { 1, 2 }),
				(1, 15, new HashSet<int> { 3, 4 }),
				(2, 20, new HashSet<int> { 5, 6 }),
			};

			Assert.That(RegionMergeEval.SelectRoundDrops(eligible).Count, Is.EqualTo(3));
		}

		[Test]
		public void EmptyEligibleDropsNothing()
		{
			var eligible = new List<(int Index, int Separated, HashSet<int> Touching)>();
			Assert.That(RegionMergeEval.SelectRoundDrops(eligible), Is.Empty);
		}
	}
}
