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
using OpenRA.Mods.CA.Traits.BotModules.Squads;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public class MarchEvalTest
	{
		// Hotspot #2 (2026-10-04): GroundUnitsAttackMoveStateCA's decisions — the stuck
		// detector truth table, the make-way-before-kick escalation, the leader-wait
		// latch, route params (incl. the laziness contract on the random draw),
		// waypoint advance, role bucketing, axis projection and rear-stall tracking.

		[Test]
		public void StuckCountersTruthTable()
		{
			// stop ∧ wait → kick counter grows (members may be stuck behind)
			Assert.That(MarchEvalCA.StuckCountersNext(true, true, false, 2, 1), Is.EqualTo((2, 2)));

			// stop ∧ ¬wait → makeWay counter grows, unless a make-way just ended (-1 sentinel),
			// which escalates to the kick counter for the rest of the march
			Assert.That(MarchEvalCA.StuckCountersNext(true, false, false, 2, 1), Is.EqualTo((3, 1)));
			Assert.That(MarchEvalCA.StuckCountersNext(true, false, true, 2, 1), Is.EqualTo((2, 2)));

			// ¬stop ∧ ¬wait → healthy march, both reset
			Assert.That(MarchEvalCA.StuckCountersNext(false, false, false, 2, 1), Is.EqualTo((0, 0)));

			// ¬stop ∧ wait → UNCHANGED: a moving leader on a strung-out march is progress;
			// the leader-wait latch holds the line, the counters neither arm nor decay
			Assert.That(MarchEvalCA.StuckCountersNext(false, true, false, 2, 1), Is.EqualTo((2, 1)));
		}

		[Test]
		public void MakeWayBeatsKickWhenBothCross()
		{
			// Both over max in one tick → the cheaper fix runs first; kick keeps accumulating.
			Assert.That(MarchEvalCA.StuckActionFor(4, 6, 4, 6), Is.EqualTo(MarchStuckAction.MakeWay));
			Assert.That(MarchEvalCA.StuckActionFor(3, 6, 4, 6), Is.EqualTo(MarchStuckAction.KickStuck));
			Assert.That(MarchEvalCA.StuckActionFor(3, 5, 4, 6), Is.EqualTo(MarchStuckAction.None));
		}

		[Test]
		public void RouteParamsAreTypeScopedAndLazy()
		{
			var rolls = 0;
			int Roll() { rolls++; return 50; }

			Assert.That(MarchEvalCA.RouteParams(SquadCAType.Harass, 5, 0, Roll), Is.EqualTo((5, false)));
			Assert.That(MarchEvalCA.RouteParams(SquadCAType.Guerrilla, 5, 0, Roll), Is.EqualTo((3, false)));
			Assert.That(rolls, Is.EqualTo(0), "harass/guerrilla must not touch the random stream");

			// Chance gate closed → no draw either (bit-identical random stream).
			Assert.That(MarchEvalCA.RouteParams(SquadCAType.Rush, 5, 0, Roll), Is.EqualTo((2, false)));
			Assert.That(rolls, Is.EqualTo(0));

			// Gate open: roll inside → indirect plan (7 routes); roll outside → default 2.
			Assert.That(MarchEvalCA.RouteParams(SquadCAType.Rush, 5, 60, Roll), Is.EqualTo((7, true)));
			Assert.That(rolls, Is.EqualTo(1));
			Assert.That(MarchEvalCA.RouteParams(SquadCAType.Rush, 5, 40, Roll), Is.EqualTo((2, false)));
			Assert.That(rolls, Is.EqualTo(2));
		}

		[Test]
		public void WaypointAdvancesNearOrTimedOut()
		{
			Assert.That(MarchEvalCA.AdvanceWaypoint(15, 100, 0), Is.True);   // < 16 → near
			Assert.That(MarchEvalCA.AdvanceWaypoint(16, 100, 0), Is.False);  // boundary excluded
			Assert.That(MarchEvalCA.AdvanceWaypoint(100, 700, 0), Is.True);  // tick > last + 625
			Assert.That(MarchEvalCA.AdvanceWaypoint(100, 625, 0), Is.False); // exactly at the cap
		}

		[Test]
		public void LeaderWaitLatchAndHold()
		{
			Assert.That(MarchEvalCA.LeaderWaitLatches(true, false), Is.True);
			Assert.That(MarchEvalCA.LeaderWaitLatches(true, true), Is.False);   // kick episode drives the leader
			Assert.That(MarchEvalCA.LeaderWaitLatches(false, false), Is.False);

			Assert.That(MarchEvalCA.LeaderWaitHolds(true, false), Is.True);
			Assert.That(MarchEvalCA.LeaderWaitHolds(false, false), Is.False);   // everyone caught up → release
			Assert.That(MarchEvalCA.LeaderWaitHolds(true, true), Is.False);     // kick overrides the wait
		}

		[Test]
		public void BucketPrecedenceClaimsBeforeResidual()
		{
			Assert.That(MarchEvalCA.BucketFor(new HashSet<string> { "anti_air", "frontline" }), Is.EqualTo(MarchBucket.Frontline));
			Assert.That(MarchEvalCA.BucketFor(new HashSet<string> { "scout", "anti_air" }), Is.EqualTo(MarchBucket.Scout));
			Assert.That(MarchEvalCA.BucketFor(new HashSet<string> { "anti_air" }), Is.EqualTo(MarchBucket.AntiAir));
			Assert.That(MarchEvalCA.BucketFor(new HashSet<string> { "artillery" }), Is.EqualTo(MarchBucket.Trailing));
			Assert.That(MarchEvalCA.BucketFor(null), Is.EqualTo(MarchBucket.Trailing));
		}

		[Test]
		public void AxisProjectionIsLongMath()
		{
			// The projection along a unit axis: a delta 10 cells ahead scores the axis length.
			Assert.That(MarchEvalCA.AxisRemaining(10240, 0, 0, 1, 0, 0, 1), Is.EqualTo(10240));
			// Perpendicular deltas score zero; behind the centroid goes negative.
			Assert.That(MarchEvalCA.AxisRemaining(0, 500, 0, 1, 0, 0, 1), Is.EqualTo(0));
			Assert.That(MarchEvalCA.AxisRemaining(-1024, 0, 0, 1, 0, 0, 1), Is.EqualTo(-1024));
		}

		[Test]
		public void RearStallTracksPositionAndHp()
		{
			// Movement resets and unprimes the HP watch.
			Assert.That(MarchEvalCA.RearStallNext(false, 20, true, 500, 800), Is.EqualTo((0, -1, false)));

			// Same position ticks up; first primed tick can't flag under-fire (prevHp -1).
			Assert.That(MarchEvalCA.RearStallNext(true, 0, true, 500, -1), Is.EqualTo((1, 500, false)));

			// HP drop while stalled → under fire (the pull-back trigger).
			Assert.That(MarchEvalCA.RearStallNext(true, 30, true, 450, 500), Is.EqualTo((31, 450, true)));

			// No health → ticks but HP watch untouched.
			Assert.That(MarchEvalCA.RearStallNext(true, 5, false, 0, -1), Is.EqualTo((6, -1, false)));
		}

		[Test]
		public void StalledRearWidensLeadAtThreshold()
		{
			Assert.That(MarchEvalCA.LeadForStall(MarchEvalCA.StalledRearTicks - 1, 100, 300), Is.EqualTo(100));
			Assert.That(MarchEvalCA.LeadForStall(MarchEvalCA.StalledRearTicks, 100, 300), Is.EqualTo(300));
		}
	}
}
