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

using System.Collections.Generic;
using NUnit.Framework;
using OpenRA.Mods.CA;
using OpenRA.Mods.CA.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	/// <summary>
	/// CONSOLIDATE-03 P7 phase (a) (design 01 5e100b94 + Opus review changes): the write-only army duty ledger. Pure: units are actor ids, no engine state. Nothing reads the ledger in phase (a).
	/// </summary>
	[TestFixture]
	public class ArmyDutyLedgerTest
	{
		static readonly ArmyDuty[] All =
		[
			ArmyDuty.OwnBaseDefend, ArmyDuty.AllyDefend, ArmyDuty.GuardLend, ArmyDuty.TeamPush, ArmyDuty.PincerArm,
			ArmyDuty.AllIn, ArmyDuty.Stalemate, ArmyDuty.AttackLiveness, ArmyDuty.Raid, ArmyDuty.Idle,
		];

		[Test]
		public void TheRanksFollowTheTargetOrderWithTheOpusChanges()
		{
			Assert.That(ArmyDutyLedger.Rank(ArmyDuty.None), Is.EqualTo(0));
			Assert.That(ArmyDutyLedger.Rank(ArmyDuty.AllyDefend), Is.LessThan(ArmyDutyLedger.Rank(ArmyDuty.OwnBaseDefend)), "R2 ally falls above R3 own-base threat");
			Assert.That(ArmyDutyLedger.Rank(ArmyDuty.OwnBaseDefend), Is.LessThan(ArmyDutyLedger.Rank(ArmyDuty.GuardLend)), "R3 above the R4 guard");
			Assert.That(ArmyDutyLedger.Rank(ArmyDuty.GuardLend), Is.LessThan(ArmyDutyLedger.Rank(ArmyDuty.TeamPush)));
			foreach (var order in new[] { ArmyDuty.TeamPush, ArmyDuty.PincerArm, ArmyDuty.AllIn, ArmyDuty.AttackLiveness })
				Assert.That(ArmyDutyLedger.Rank(order), Is.EqualTo(5), order + " is a team order, R5");

			Assert.That(ArmyDutyLedger.Rank(ArmyDuty.Stalemate), Is.EqualTo(5), "Opus change 1: the stalemate breaker is an attack order (R5), not R7");
			Assert.That(ArmyDutyLedger.Rank(ArmyDuty.Raid), Is.EqualTo(6));
			Assert.That(ArmyDutyLedger.Rank(ArmyDuty.Idle), Is.EqualTo(7), "only idle, reserve and preposition stay at R7");
			foreach (var duty in All)
				Assert.That(ArmyDutyLedger.Rank(duty), Is.InRange(2, 7), duty.ToString());
		}

		[Test]
		public void TheCoreDefinitionNeedsAHitCoreBuildingAndAnObservedForceAtTheThreshold()
		{
			Assert.That(ArmyDutyLedger.IsCoreUnderAttack(true, false, false, 1000, 1000), Is.True);
			Assert.That(ArmyDutyLedger.IsCoreUnderAttack(false, true, false, 5000, 1000), Is.True);
			Assert.That(ArmyDutyLedger.IsCoreUnderAttack(false, false, true, 1000, 1000), Is.True);
			Assert.That(ArmyDutyLedger.IsCoreUnderAttack(true, true, true, 999, 1000), Is.False, "below the observed threat threshold");
			Assert.That(ArmyDutyLedger.IsCoreUnderAttack(false, false, false, 9999, 1000), Is.False, "no core building hit");
		}

		[Test]
		public void AFirstClaimIsNewAndARepeatByTheSameDutyKeepsTheSinceTick()
		{
			var ledger = new ArmyDutyLedger();
			var first = ledger.Claim(7, ArmyDuty.Raid, 100, "harass", "AddToHarassSquad");
			Assert.That(first.Kind, Is.EqualTo(ArmyDutyLedger.ClaimKind.New));
			Assert.That(first.Steal, Is.False);

			var again = ledger.Claim(7, ArmyDuty.Raid, 400, "harass", "AddToHarassSquad");
			Assert.That(again.Kind, Is.EqualTo(ArmyDutyLedger.ClaimKind.Same));
			Assert.That(ledger.TryGet(7, out var entry), Is.True);
			Assert.That((entry.Duty, entry.Since, entry.Caller), Is.EqualTo((ArmyDuty.Raid, 100, "AddToHarassSquad")), "a refresh keeps the original tick");
			Assert.That(ledger.Count, Is.EqualTo(1));
		}

		[Test]
		public void AHigherRankTakingFromALowerIsAllowedAndAnEqualOrLowerOneIsAViolation()
		{
			var ledger = new ArmyDutyLedger();
			ledger.Claim(1, ArmyDuty.Raid, 0, "r", "a");
			var take = ledger.Claim(1, ArmyDuty.AllyDefend, 10, "d", "b");
			Assert.That(take.Kind, Is.EqualTo(ArmyDutyLedger.ClaimKind.TakeLower));
			Assert.That(take.Previous.Duty, Is.EqualTo(ArmyDuty.Raid));

			var equal = ledger.Claim(2, ArmyDuty.TeamPush, 0, "p", "a");
			Assert.That(ledger.Claim(2, ArmyDuty.AllIn, 5, "all-in", "b").Kind, Is.EqualTo(ArmyDutyLedger.ClaimKind.TakeEqualOrHigher), "equal ranks never take from each other");
			Assert.That(equal.Kind, Is.EqualTo(ArmyDutyLedger.ClaimKind.New));

			ledger.Claim(3, ArmyDuty.OwnBaseDefend, 0, "d", "a");
			Assert.That(ledger.Claim(3, ArmyDuty.AllIn, 5, "all-in", "b").Kind, Is.EqualTo(ArmyDutyLedger.ClaimKind.TakeEqualOrHigher), "Opus change 3: all-in does not take from the own-base defence");
			Assert.That(ledger.Violations, Is.EqualTo(2));
		}

		[Test]
		public void AStealIsADifferentDutyWithinTheWindow()
		{
			var ledger = new ArmyDutyLedger();
			ledger.Claim(1, ArmyDuty.Raid, 1000, "r", "a");
			Assert.That(ledger.Claim(1, ArmyDuty.AllyDefend, 1000 + ArmyDutyLedger.StealWindowTicks, "d", "b").Steal, Is.True, "inside the window");
			ledger.Claim(2, ArmyDuty.Raid, 1000, "r", "a");
			Assert.That(ledger.Claim(2, ArmyDuty.AllyDefend, 1001 + ArmyDutyLedger.StealWindowTicks, "d", "b").Steal, Is.False, "later than the window");
			Assert.That(ledger.Steals, Is.EqualTo(1));
		}

		[Test]
		public void ALentUnitThatADefendDutyDraftsBackInsideTheWindowIsCounted()
		{
			var ledger = new ArmyDutyLedger();
			ledger.Claim(5, ArmyDuty.GuardLend, 100, "lend", "ArmyGuardLendTo");
			ledger.Release(5, 100, out _);
			ledger.NoteLend(5, 100);
			var drafted = ledger.Claim(5, ArmyDuty.AllyDefend, 600, "d", "DefendAllyRespond");
			Assert.That(drafted.DraftBack, Is.True);
			Assert.That(ledger.DraftBacks, Is.EqualTo(1));

			ledger.NoteLend(6, 100);
			Assert.That(ledger.Claim(6, ArmyDuty.TeamPush, 200, "p", "TeamLeadJoin").DraftBack, Is.False, "only the defend duties draft back");
			ledger.NoteLend(8, 0);
			Assert.That(ledger.Claim(8, ArmyDuty.OwnBaseDefend, 1 + ArmyDutyLedger.LendWindowTicks, "d", "ArbiterProtectOwn").DraftBack, Is.False, "after the window it is a normal claim");
		}

		[Test]
		public void AReleaseDropsTheEntryAndTheCountersAndHeartbeatAreSummed()
		{
			var ledger = new ArmyDutyLedger();
			ledger.Claim(1, ArmyDuty.Idle, 0, "pool", "FindNewUnits");
			Assert.That(ledger.Release(1, 50, out var previous), Is.True);
			Assert.That(previous.Duty, Is.EqualTo(ArmyDuty.Idle));
			Assert.That(ledger.Release(1, 60, out _), Is.False, "a second release finds nothing");
			Assert.That(ledger.Count, Is.EqualTo(0));

			ledger.NoteHeartbeatDoubles(3);
			ledger.NoteHeartbeatDoubles(-2);
			Assert.That(ledger.HeartbeatDoubles, Is.EqualTo(3));
			Assert.That(ledger.CountersDetail(), Is.EqualTo("claims=1;releases=2;steals=0;violations=0;draftbacks=0;heartbeat_doubles=3;held=0"));
		}

		[Test]
		public void TheTraceDetailsAreFixedAndCarryNoComma()
		{
			var kinds = new Dictionary<ArmyDutyLedger.ClaimKind, int> { [ArmyDutyLedger.ClaimKind.New] = 2, [ArmyDutyLedger.ClaimKind.TakeLower] = 1 };
			var claim = ArmyDutyLedger.ClaimDetail(ArmyDuty.AllyDefend, "DefendAllyRespond", "ally_attacked", kinds, 1, 0, new uint[] { 30, 4, 12 }, "0badf00d");
			Assert.That(claim, Is.EqualTo("duty=AllyDefend;rank=2;caller=DefendAllyRespond;reason=ally_attacked;new=2;same=0;take=1;viol=0;steals=1;draftbacks=0;units=4|12|30;pool=0badf00d"));

			var release = ArmyDutyLedger.ReleaseDetail("ReleaseDefenders", "defence_over", new Dictionary<ArmyDuty, int> { [ArmyDuty.OwnBaseDefend] = 3, [ArmyDuty.AllyDefend] = 1 }, new uint[] { 9, 2 }, "0badf00d");
			Assert.That(release, Is.EqualTo("caller=ReleaseDefenders;reason=defence_over;prev=OwnBaseDefend:3|AllyDefend:1;units=2|9;pool=0badf00d"));
			Assert.That(claim + release, Does.Not.Contain(","));
		}

		[Test]
		public void ThePoolHashDependsOnTheOrderOfTheIds()
		{
			Assert.That(ArmyDutyLedger.PoolHash(new uint[] { 1, 2, 3 }), Is.EqualTo(ArmyDutyLedger.PoolHash(new uint[] { 1, 2, 3 })));
			Assert.That(ArmyDutyLedger.PoolHash(new uint[] { 1, 2, 3 }), Is.Not.EqualTo(ArmyDutyLedger.PoolHash(new uint[] { 3, 2, 1 })), "the pool order matters for equivalence");
			Assert.That(ArmyDutyLedger.PoolHash(new uint[] { 1, 2, 3 }), Is.EqualTo(BotRng.Fnv1a("1,2,3,").ToString("x8")));
			Assert.That(ArmyDutyLedger.PoolHash([]), Has.Length.EqualTo(8));
		}
	}
}
