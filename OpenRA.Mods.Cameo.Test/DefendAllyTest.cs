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
using OpenRA.Mods.CA.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	/// <summary>DEFEND-ALLY-01 (USER P0): the pure half of the allied-defence response.</summary>
	[TestFixture]
	public class DefendAllyTest
	{
		const int Cell = 1024;

		static WPos P(int cellsX, int cellsY = 0) => new(cellsX * Cell, cellsY * Cell, 0);

		[Test]
		public void OnlyALossOfHitPointsCountsAsAnAttack()
		{
			Assert.That(DefendAllyCA.Dropped(100, 90), Is.True);
			Assert.That(DefendAllyCA.Dropped(100, 100), Is.False, "unchanged");
			Assert.That(DefendAllyCA.Dropped(90, 100), Is.False, "repair or heal is not an attack");
		}

		[Test]
		public void TheScanRunsOnceTheIntervalHasPassed()
		{
			Assert.That(DefendAllyCA.Due(99, 100), Is.False);
			Assert.That(DefendAllyCA.Due(100, 100), Is.True);
			Assert.That(DefendAllyCA.Due(250, 100), Is.True);
		}

		[Test]
		public void RespondersAreGroupedIntoOneCentrePerBucketInBucketOrder()
		{
			// Bucket side 12 cells: three units near the origin share one bucket, two units near x=40 another.
			var positions = new List<WPos> { P(41, 1), P(1, 1), P(3, 5), P(5, 3), P(43, 3) };
			var centres = DefendAllyCA.ClusterCentres(positions, 12 * Cell);

			Assert.That(centres.Count, Is.EqualTo(2));
			Assert.That(centres[0], Is.EqualTo(new WPos(3 * Cell, 3 * Cell, 0)), "mean of (1,1) (3,5) (5,3)");
			Assert.That(centres[1], Is.EqualTo(new WPos(42 * Cell, 2 * Cell, 0)), "mean of (41,1) (43,3)");
		}

		[Test]
		public void ClusteringDoesNotDependOnTheInputOrder()
		{
			var a = new List<WPos> { P(1, 1), P(30, 30), P(2, 2), P(31, 29) };
			var b = new List<WPos> { P(31, 29), P(2, 2), P(30, 30), P(1, 1) };
			Assert.That(DefendAllyCA.ClusterCentres(a, 12 * Cell), Is.EqualTo(DefendAllyCA.ClusterCentres(b, 12 * Cell)));
			Assert.That(DefendAllyCA.ClusterCentres([], 12 * Cell), Is.Empty);
		}

		[Test]
		public void TheNearestEnemyInsideTheScanRadiusIsTheAttackerAndTiesGoToTheLowerId()
		{
			var friendly = P(0);
			var enemies = new List<(uint Id, WPos Pos)> { (7, P(9)), (3, P(5)), (5, P(0, 5)), (1, P(30)) };
			Assert.That(DefendAllyCA.NearestAttacker(friendly, enemies, 12 * Cell), Is.EqualTo(1), "id 3 and id 5 are both 5 cells away; the lower id wins");

			Assert.That(DefendAllyCA.NearestAttacker(friendly, [(1, P(30))], 12 * Cell), Is.EqualTo(-1), "outside the radius");
			Assert.That(DefendAllyCA.NearestAttacker(friendly, [(1, P(12))], 12 * Cell), Is.EqualTo(0), "the radius edge is inclusive");
			Assert.That(DefendAllyCA.NearestAttacker(friendly, [], 12 * Cell), Is.EqualTo(-1));
		}

		[Test]
		public void AResponderIsInRangeUpToAndIncludingTheRadius()
		{
			Assert.That(DefendAllyCA.InRadius(P(25), P(0), 25 * Cell), Is.True);
			Assert.That(DefendAllyCA.InRadius(P(26), P(0), 25 * Cell), Is.False);
			Assert.That(DefendAllyCA.InRadius(P(15, 20), P(0), 25 * Cell), Is.True, "3-4-5: exactly 25 cells");
			Assert.That(DefendAllyCA.InRadius(P(0), P(0), 25 * Cell), Is.True);
		}
	}
}
