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
using OpenRA.Mods.CA.Traits.BotModules.Squads;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public class AttackForceEvalTest
	{
		// Hotspot #3 (2026-10-04): CreateAttackForce decision atoms — the launch gate's two
		// arms, the Defend-hold window boundary, the raid-steering overcommit cap, and the
		// fire-support escort quota. Every arm and boundary of each rule is pinned.

		[Test]
		public void MaxIdleUnitsLaunchesRegardlessOfOtherGates()
		{
			// The overflow arm ignores value and count gates entirely.
			Assert.That(AttackForceEvalCA.ShouldLaunch(
				poolCount: 10, maxIdleUnits: 10, idleUnitsValue: 0,
				requiredValue: 5000, requiredSize: 20, valueOnlyLaunch: false, squadValue: 0), Is.True);

			// One below max: the overflow arm does not fire, and the other gates
			// decide — unmet value gate means no launch.
			Assert.That(AttackForceEvalCA.ShouldLaunch(
				poolCount: 9, maxIdleUnits: 10, idleUnitsValue: 400,
				requiredValue: 5000, requiredSize: 20, valueOnlyLaunch: false, squadValue: 0), Is.False);
		}

		[Test]
		public void ValueGateRequiresCountGate()
		{
			// Value met but count gate not — no launch.
			Assert.That(AttackForceEvalCA.ShouldLaunch(
				poolCount: 5, maxIdleUnits: 20, idleUnitsValue: 1000,
				requiredValue: 1000, requiredSize: 10, valueOnlyLaunch: false, squadValue: 0), Is.False);

			// Value and count both met — launch.
			Assert.That(AttackForceEvalCA.ShouldLaunch(
				poolCount: 10, maxIdleUnits: 20, idleUnitsValue: 1000,
				requiredValue: 1000, requiredSize: 10, valueOnlyLaunch: false, squadValue: 0), Is.True);
		}

		[Test]
		public void ValueOnlyAttackLaunchWaivesCountGate()
		{
			// Waived only when a squad value is configured (SquadValue > 0).
			Assert.That(AttackForceEvalCA.ShouldLaunch(
				poolCount: 2, maxIdleUnits: 20, idleUnitsValue: 1000,
				requiredValue: 1000, requiredSize: 10, valueOnlyLaunch: true, squadValue: 500), Is.True);
			Assert.That(AttackForceEvalCA.ShouldLaunch(
				poolCount: 2, maxIdleUnits: 20, idleUnitsValue: 1000,
				requiredValue: 1000, requiredSize: 10, valueOnlyLaunch: true, squadValue: 0), Is.False);

			// valueOnlyLaunch off + squadValue set: the waiver does NOT apply —
			// the count gate falls back to requiredSize.
			Assert.That(AttackForceEvalCA.ShouldLaunch(
				poolCount: 2, maxIdleUnits: 20, idleUnitsValue: 1000,
				requiredValue: 1000, requiredSize: 10, valueOnlyLaunch: false, squadValue: 500), Is.False);
			Assert.That(AttackForceEvalCA.ShouldLaunch(
				poolCount: 10, maxIdleUnits: 20, idleUnitsValue: 1000,
				requiredValue: 1000, requiredSize: 10, valueOnlyLaunch: false, squadValue: 500), Is.True);
		}

		[Test]
		public void ValueGateIsInclusive()
		{
			Assert.That(AttackForceEvalCA.ShouldLaunch(
				poolCount: 10, maxIdleUnits: 20, idleUnitsValue: 999,
				requiredValue: 1000, requiredSize: 10, valueOnlyLaunch: false, squadValue: 0), Is.False);
			Assert.That(AttackForceEvalCA.ShouldLaunch(
				poolCount: 10, maxIdleUnits: 20, idleUnitsValue: 1000,
				requiredValue: 1000, requiredSize: 10, valueOnlyLaunch: false, squadValue: 0), Is.True);
		}

		[Test]
		public void DefendHoldWindowIsInclusive()
		{
			Assert.That(AttackForceEvalCA.DefendHoldActive(heldTicks: 100, holdTicks: 100), Is.True);
			Assert.That(AttackForceEvalCA.DefendHoldActive(heldTicks: 101, holdTicks: 100), Is.False);
		}

		[Test]
		public void NegativeHoldTicksClampsToZero()
		{
			// A disabled window holds only "in the same tick" — heldTicks 0 still <= 0.
			Assert.That(AttackForceEvalCA.DefendHoldActive(heldTicks: 0, holdTicks: -5), Is.True);
			Assert.That(AttackForceEvalCA.DefendHoldActive(heldTicks: 1, holdTicks: -5), Is.False);
		}

		[Test]
		public void RaidSteerCapTakesLargerOfRatioAndFloor()
		{
			Assert.That(AttackForceEvalCA.RaidSteerCap(10000, 150, 2000), Is.EqualTo(15000));
			Assert.That(AttackForceEvalCA.RaidSteerCap(500, 150, 2000), Is.EqualTo(2000));
		}

		[Test]
		public void RaidSteerCapClampsAtIntMax()
		{
			// A deep pool times a large percent overflows int — the cap clamps.
			Assert.That(AttackForceEvalCA.RaidSteerCap(int.MaxValue, 200, 0), Is.EqualTo(int.MaxValue));
		}

		[Test]
		public void EscortQuotaIsMinOfPerArtilleryAndThird()
		{
			Assert.That(AttackForceEvalCA.EscortsNeeded(artilleryCount: 2, assaultCount: 30, perArtillery: 3), Is.EqualTo(6));
			Assert.That(AttackForceEvalCA.EscortsNeeded(artilleryCount: 10, assaultCount: 9, perArtillery: 3), Is.EqualTo(3));
			Assert.That(AttackForceEvalCA.EscortsNeeded(artilleryCount: 0, assaultCount: 30, perArtillery: 3), Is.EqualTo(0));
		}
	}
}
