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

using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Cameo.Traits.BotModules;

namespace OpenRA.Mods.Cameo.Test
{
	// DEF-LANES-01 P1 (user 11-28): the per-lane attack memory, its decay, the extra budget and the coverage/lane split. Pure state, no world.
	[TestFixture]
	public class DefenseLanesTest
	{
		[Test]
		public void BucketsFollowTheBearing()
		{
			Assert.That(DefenseLanes.Bucket(10, 0, 8), Is.EqualTo(0));
			Assert.That(DefenseLanes.Bucket(10, 10, 8), Is.EqualTo(1));
			Assert.That(DefenseLanes.Bucket(0, 10, 8), Is.EqualTo(2));
			Assert.That(DefenseLanes.Bucket(-10, 0, 8), Is.EqualTo(4));
			Assert.That(DefenseLanes.Bucket(0, -10, 8), Is.EqualTo(6));
			Assert.That(DefenseLanes.Bucket(10, -1, 8), Is.EqualTo(0));
			Assert.That(DefenseLanes.Bucket(0, 0, 8), Is.EqualTo(0));
		}

		[Test]
		public void DirectionPointsBackIntoItsBucket()
		{
			for (var b = 0; b < 8; b++)
			{
				var (x, y) = DefenseLanes.Direction(b, 8, 20);
				Assert.That(DefenseLanes.Bucket(x, y, 8), Is.EqualTo(b));
			}
		}

		[Test]
		public void AttacksAndDamageRaiseTheLane()
		{
			var lanes = new DefenseLanes(8);
			Assert.That(lanes.Heaviest(), Is.EqualTo(-1));
			lanes.Hit(3, 100, 5000, 50, 10, 250, 1000);
			Assert.That(lanes.Weight(3), Is.EqualTo(50));

			// The same attack (inside the window): no attack gain; the damage carry reaches one step.
			lanes.Hit(3, 200, 5000, 50, 10, 250, 1000);
			Assert.That(lanes.Weight(3), Is.EqualTo(60));

			// A new attack after a quiet window.
			lanes.Hit(3, 450, 0, 50, 10, 250, 1000);
			Assert.That(lanes.Weight(3), Is.EqualTo(110));
			lanes.Hit(5, 450, 0, 50, 10, 250, 1000);
			Assert.That(lanes.Heaviest(), Is.EqualTo(3));
			Assert.That(lanes.Total(), Is.EqualTo(160));

			// The cap.
			lanes.Hit(5, 451, 10000000, 50, 10, 250, 1000);
			Assert.That(lanes.Weight(5), Is.EqualTo(1000));
		}

		[Test]
		public void QuietLanesDecay()
		{
			var lanes = new DefenseLanes(8);
			lanes.Hit(1, 0, 0, 1000, 0, 250, 1000);
			lanes.Decay(749, 100, 750);
			Assert.That(lanes.Weight(1), Is.EqualTo(1000));
			lanes.Decay(750, 100, 750);
			Assert.That(lanes.Weight(1), Is.EqualTo(900));
			lanes.Decay(2250, 100, 750);
			Assert.That(lanes.Weight(1), Is.EqualTo(729));
			lanes.Decay(750 * 200, 100, 750);
			Assert.That(lanes.Weight(1), Is.EqualTo(0));
			Assert.That(lanes.Heaviest(), Is.EqualTo(-1));
		}

		[Test]
		public void BudgetAndSplit()
		{
			Assert.That(DefenseLanes.ExtraPermille(0, 100, 1000), Is.EqualTo(0));
			Assert.That(DefenseLanes.ExtraPermille(50, 100, 1000), Is.EqualTo(150));
			Assert.That(DefenseLanes.ExtraPermille(5000, 100, 1000), Is.EqualTo(1000));

			// No extra budget = the factor unchanged; +100% budget halves it.
			Assert.That(DefenceLaneBudget.Apply(2000, 0), Is.EqualTo(2000));
			Assert.That(DefenceLaneBudget.Apply(2000, 1000), Is.EqualTo(1000));
			Assert.That(DefenceLaneBudget.Apply(1500, 500), Is.EqualTo(1000));

			// 50%: lane, coverage, lane, coverage.
			Assert.That(DefenseLanes.LaneTurn(0, 0, 50), Is.True);
			Assert.That(DefenseLanes.LaneTurn(1, 0, 50), Is.False);
			Assert.That(DefenseLanes.LaneTurn(1, 1, 50), Is.True);
			Assert.That(DefenseLanes.LaneTurn(0, 5, 0), Is.False);
			Assert.That(DefenseLanes.LaneTurn(5, 0, 100), Is.True);
		}
	}
}
