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
using System.IO;
using System.Linq;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	/// <summary>
	/// CONSOLIDATE-03 P7 (b), the pure policy seam of the rank rows (design 01 section 3; Opus review changes 3 and 5; Sol FIX_STATIC on db515678: claim identity and effective rank on both sides). Not wired into a
	/// decision (the rows wait for the baseline counters of phase (a)).
	/// </summary>
	[TestFixture]
	public class DutyArbiterTest
	{
		const string Bots = "OpenRA.Mods.CA/Traits/BotModules/";

		static string RepoFile(string relative)
		{
			var dir = new DirectoryInfo(AppContext.BaseDirectory);
			while (dir != null && !File.Exists(Path.Combine(dir.FullName, "mod.config")))
				dir = dir.Parent;

			Assert.That(dir, Is.Not.Null, "repository root (mod.config) not found above " + AppContext.BaseDirectory);
			return File.ReadAllText(Path.Combine(dir.FullName, relative)).Replace("\r\n", "\n");
		}

		static readonly ArmyDuty[] Duties = Enum.GetValues<ArmyDuty>().Where(d => d != ArmyDuty.None).ToArray();

		static DutyArbiter.DutyClaim C(ArmyDuty duty, long identity = 1, bool core = false) => new(duty, identity, core);

		[Test]
		public void AHigherRankTakesFromALowerOneAndEqualRanksNever()
		{
			Assert.That(DutyArbiter.MayTake(C(ArmyDuty.AllyDefend), C(ArmyDuty.TeamPush)), Is.True, "R2 over R5");
			Assert.That(DutyArbiter.MayTake(C(ArmyDuty.OwnBaseDefend), C(ArmyDuty.Raid)), Is.True, "R3 over R6");
			Assert.That(DutyArbiter.MayTake(C(ArmyDuty.TeamPush), C(ArmyDuty.Idle, 0)), Is.True, "R5 over R7");
			Assert.That(DutyArbiter.MayTake(C(ArmyDuty.Raid), C(ArmyDuty.TeamPush)), Is.False, "R6 never takes from R5");
			Assert.That(DutyArbiter.MayTake(C(ArmyDuty.OwnBaseDefend), C(ArmyDuty.AllyDefend)), Is.False, "R3 never takes from R2");
			Assert.That(DutyArbiter.MayTake(C(ArmyDuty.Stalemate), C(ArmyDuty.TeamPush)), Is.False, "equal ranks (R5, R5) never");
			Assert.That(DutyArbiter.MayTake(C(ArmyDuty.PincerArm), C(ArmyDuty.AllIn)), Is.False);
			Assert.That(DutyArbiter.MayTake(C(ArmyDuty.StandaloneAttack), C(ArmyDuty.Raid)), Is.False, "equal ranks (R6, R6)");
			Assert.That(DutyArbiter.MayTake(C(ArmyDuty.Raid), C(ArmyDuty.None, 0)), Is.True, "no holder");
		}

		[Test]
		public void TheSameClaimRefreshesButAnotherSquadOrOrderOfTheSameDutyIsAnEqualRankClaim()
		{
			// same duty AND same identity: the claim refreshing itself, not a take
			Assert.That(DutyArbiter.MayTake(C(ArmyDuty.Raid, 7), C(ArmyDuty.Raid, 7)), Is.True);
			Assert.That(DutyArbiter.MayTake(C(ArmyDuty.TeamPush, 3), C(ArmyDuty.TeamPush, 3)), Is.True, "the same order, again");
			Assert.That(DutyArbiter.MayTake(C(ArmyDuty.OwnBaseDefend, 2, true), C(ArmyDuty.OwnBaseDefend, 2, true)), Is.True, "the same active core defence");

			// same duty, different squad or order: equal rank, never (the enum-equality shortcut of db515678 let this through)
			Assert.That(DutyArbiter.MayTake(C(ArmyDuty.Raid, 8), C(ArmyDuty.Raid, 7)), Is.False, "another raid squad");
			Assert.That(DutyArbiter.MayTake(C(ArmyDuty.TeamPush, 4), C(ArmyDuty.TeamPush, 3)), Is.False, "another team order");
			Assert.That(DutyArbiter.MayTake(C(ArmyDuty.AllyDefend, 9), C(ArmyDuty.AllyDefend, 5)), Is.False, "another ally call");
			Assert.That(DutyArbiter.MayTake(C(ArmyDuty.OwnBaseDefend, 1), C(ArmyDuty.OwnBaseDefend, 2)), Is.False, "another own-base defence squad");

			// the same identity under a different duty is a different claim: the rank decides
			Assert.That(DutyArbiter.MayTake(C(ArmyDuty.AllyDefend, 7), C(ArmyDuty.Raid, 7)), Is.True);
			Assert.That(DutyArbiter.MayTake(C(ArmyDuty.Raid, 7), C(ArmyDuty.AllyDefend, 7)), Is.False);
		}

		[Test]
		public void TheGateAgreesWithTheLedgersTakeKindForEveryPairOfDifferentDuties()
		{
			foreach (var claimant in Duties)
				foreach (var holder in Duties)
				{
					if (claimant == holder)
						continue;

					var ledger = new ArmyDutyLedger();
					ledger.Claim(1, holder, 0, "r", "t");
					var kind = ledger.Claim(1, claimant, 5000, "r", "t").Kind;
					Assert.That(DutyArbiter.MayTake(C(claimant, 2), C(holder, 1)), Is.EqualTo(kind == ArmyDutyLedger.ClaimKind.TakeLower), claimant + " over " + holder);
				}
		}

		[Test]
		public void AnActiveCoreDefenceHoldsAtRankOneOnBothSidesOfTheGate()
		{
			Assert.That(DutyArbiter.EffectiveRank(ArmyDuty.OwnBaseDefend, true), Is.EqualTo(1));
			Assert.That(DutyArbiter.EffectiveRank(ArmyDuty.OwnBaseDefend, false), Is.EqualTo(3));
			Assert.That(DutyArbiter.EffectiveRank(ArmyDuty.AllyDefend, true), Is.EqualTo(2), "the core flag changes only the own-base rank");
			Assert.That(DutyArbiter.EffectiveRank(C(ArmyDuty.OwnBaseDefend, 1, true)), Is.EqualTo(1));

			// claimant side: a core under attack outranks an ally call, an ordinary own-base claim does not
			Assert.That(DutyArbiter.MayTake(C(ArmyDuty.OwnBaseDefend, 2, true), C(ArmyDuty.AllyDefend, 5)), Is.True);
			Assert.That(DutyArbiter.MayTake(C(ArmyDuty.OwnBaseDefend, 2, false), C(ArmyDuty.AllyDefend, 5)), Is.False);

			// holder side (Sol): the holder's active core defence is R1, so an ally call (R2) is refused; the same holder without the core flag (R3) is taken
			Assert.That(DutyArbiter.MayTake(C(ArmyDuty.AllyDefend, 5), C(ArmyDuty.OwnBaseDefend, 2, true)), Is.False, "active core defence holds at R1");
			Assert.That(DutyArbiter.MayTake(C(ArmyDuty.AllyDefend, 5), C(ArmyDuty.OwnBaseDefend, 2, false)), Is.True, "an ordinary own-base defence (R3) yields to R2");

			// two own-base claims: only a core under attack may take from an ordinary one, never the other way
			Assert.That(DutyArbiter.MayTake(C(ArmyDuty.OwnBaseDefend, 3, true), C(ArmyDuty.OwnBaseDefend, 2, false)), Is.True);
			Assert.That(DutyArbiter.MayTake(C(ArmyDuty.OwnBaseDefend, 2, false), C(ArmyDuty.OwnBaseDefend, 3, true)), Is.False);
			Assert.That(DutyArbiter.MayTake(C(ArmyDuty.OwnBaseDefend, 3, true), C(ArmyDuty.OwnBaseDefend, 2, true)), Is.False, "two active core defences: equal R1");
		}

		[Test]
		public void TheAllInClaimsEverythingFromRankFourDownAndNeverFromRankOneToThree()
		{
			foreach (var holder in new[] { ArmyDuty.GuardLend, ArmyDuty.TeamPush, ArmyDuty.PincerArm, ArmyDuty.AllIn, ArmyDuty.Stalemate, ArmyDuty.AttackLiveness, ArmyDuty.StandaloneAttack, ArmyDuty.Raid, ArmyDuty.Idle, ArmyDuty.None })
				Assert.That(DutyArbiter.AllInMayTake(C(holder)), Is.True, holder.ToString());

			foreach (var holder in new[] { ArmyDuty.OwnBaseDefend, ArmyDuty.AllyDefend })
				Assert.That(DutyArbiter.AllInMayTake(C(holder)), Is.False, holder.ToString());

			Assert.That(DutyArbiter.AllInMayTake(C(ArmyDuty.OwnBaseDefend, 1, true)), Is.False, "an active core defence is R1");
		}

		[Test]
		public void EveryRefusedUnitIsLoggedWithTheEffectiveRankThatHoldsIt()
		{
			var refusals = DutyArbiter.AllInRefusals(new (uint, DutyArbiter.DutyClaim)[]
			{
				(30, C(ArmyDuty.Idle, 0)), (12, C(ArmyDuty.AllyDefend, 5)), (7, C(ArmyDuty.OwnBaseDefend, 2)), (5, C(ArmyDuty.Raid)), (9, C(ArmyDuty.OwnBaseDefend, 2, true)),
			});
			Assert.That(refusals, Is.EqualTo(new[] { "7:R3", "9:R1", "12:R2" }), "sorted by unit id, each with its holder's effective rank (an active core defence is R1)");
			Assert.That(DutyArbiter.AllInRefusals(Array.Empty<(uint, DutyArbiter.DutyClaim)>()), Is.Empty);
		}

		[Test]
		public void AnUnseenActorIsNeverReadAsARiskInput()
		{
			var seen = new DutyArbiter.ObservedThreat(1, 700, true, false);
			var remembered = new DutyArbiter.ObservedThreat(2, 300, false, true);
			Assert.That(DutyArbiter.ObservedRisk(new[] { seen, remembered }), Is.EqualTo(1000));
			Assert.That(DutyArbiter.ObservedRisk(Array.Empty<DutyArbiter.ObservedThreat>()), Is.EqualTo(0));

			var unseen = new DutyArbiter.ObservedThreat(3, 5000, false, false);
			Assert.Throws<InvalidOperationException>(() => DutyArbiter.ObservedRisk(new[] { seen, unseen }), "an actor the bot neither sees nor remembers must fail the planner, not count");
		}

		[Test]
		public void EligibilityNeedsTheEtaAndTheObservedRiskInBounds()
		{
			Assert.That(DutyArbiter.Eligible(400, 600, 900, 1000), Is.True);
			Assert.That(DutyArbiter.Eligible(601, 600, 0, 1000), Is.False, "too far");
			Assert.That(DutyArbiter.Eligible(100, 600, 1001, 1000), Is.False, "too risky on the way");
			Assert.That(DutyArbiter.Eligible(-1, 600, 0, 1000), Is.False, "no valid ETA");
			Assert.That(DutyArbiter.Eligible(600, 600, 1000, 1000), Is.True, "bounds are inclusive");
		}

		[Test]
		public void TheSeamIsEngineFreeAndNotWiredIntoAnyDecisionYet()
		{
			var seam = RepoFile(Bots + "BotModuleLogic/DutyArbiter.cs");
			foreach (var banned in new[] { "Random", "World", "Actor ", "Player", "FindActors", "Squad" })
				Assert.That(seam, Does.Not.Contain(banned), banned);

			// no squad-manager file calls it yet: the rows wait for the baseline counters
			var root = new DirectoryInfo(AppContext.BaseDirectory);
			while (root != null && !File.Exists(Path.Combine(root.FullName, "mod.config")))
				root = root.Parent;

			Assert.That(root, Is.Not.Null);
			var files = Directory.GetFiles(Path.Combine(root.FullName, "OpenRA.Mods.CA", "Traits", "BotModules"), "SquadManagerBotModuleCA*.cs");
			Assert.That(files, Is.Not.Empty);
			foreach (var file in files)
				Assert.That(File.ReadAllText(file), Does.Not.Contain("DutyArbiter."), file);
		}
	}
}
