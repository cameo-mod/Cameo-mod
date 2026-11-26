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
	/// CONSOLIDATE-03 P7 (b), the pure policy seam of the rank rows (design 01 section 3; Opus review changes 3 and 5). Not wired into a decision (the rows wait for the baseline counters of phase (a)).
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

		[Test]
		public void AHigherRankTakesFromALowerOneAndEqualRanksNever()
		{
			Assert.That(DutyArbiter.MayTake(ArmyDuty.AllyDefend, ArmyDuty.TeamPush), Is.True, "R2 over R5");
			Assert.That(DutyArbiter.MayTake(ArmyDuty.OwnBaseDefend, ArmyDuty.Raid), Is.True, "R3 over R6");
			Assert.That(DutyArbiter.MayTake(ArmyDuty.TeamPush, ArmyDuty.Idle), Is.True, "R5 over R7");
			Assert.That(DutyArbiter.MayTake(ArmyDuty.Raid, ArmyDuty.TeamPush), Is.False, "R6 never takes from R5");
			Assert.That(DutyArbiter.MayTake(ArmyDuty.OwnBaseDefend, ArmyDuty.AllyDefend), Is.False, "R3 never takes from R2");
			Assert.That(DutyArbiter.MayTake(ArmyDuty.Stalemate, ArmyDuty.TeamPush), Is.False, "equal ranks (R5, R5) never");
			Assert.That(DutyArbiter.MayTake(ArmyDuty.PincerArm, ArmyDuty.AllIn), Is.False);
			Assert.That(DutyArbiter.MayTake(ArmyDuty.StandaloneAttack, ArmyDuty.Raid), Is.False, "equal ranks (R6, R6)");
			Assert.That(DutyArbiter.MayTake(ArmyDuty.Raid, ArmyDuty.None), Is.True, "no holder");
			Assert.That(DutyArbiter.MayTake(ArmyDuty.Raid, ArmyDuty.Raid), Is.True, "the same duty refreshes its own claim");
		}

		[Test]
		public void TheGateAgreesWithTheLedgersTakeKindForEveryPair()
		{
			foreach (var claimant in Duties)
				foreach (var holder in Duties)
				{
					if (claimant == holder)
						continue;

					var ledger = new ArmyDutyLedger();
					ledger.Claim(1, holder, 0, "r", "t");
					var kind = ledger.Claim(1, claimant, 5000, "r", "t").Kind;
					Assert.That(DutyArbiter.MayTake(claimant, holder), Is.EqualTo(kind == ArmyDutyLedger.ClaimKind.TakeLower), claimant + " over " + holder);
				}
		}

		[Test]
		public void AnOwnBaseDefenceOfAFallingCoreIsRankOneAndOtherwiseRankThree()
		{
			Assert.That(DutyArbiter.EffectiveRank(ArmyDuty.OwnBaseDefend, true), Is.EqualTo(1));
			Assert.That(DutyArbiter.EffectiveRank(ArmyDuty.OwnBaseDefend, false), Is.EqualTo(3));
			Assert.That(DutyArbiter.EffectiveRank(ArmyDuty.AllyDefend, true), Is.EqualTo(2), "the core flag changes only the own-base rank");
			Assert.That(DutyArbiter.MayTake(ArmyDuty.OwnBaseDefend, ArmyDuty.AllyDefend, true), Is.True, "a core under attack outranks an ally call");
			Assert.That(DutyArbiter.MayTake(ArmyDuty.OwnBaseDefend, ArmyDuty.AllyDefend, false), Is.False);
		}

		[Test]
		public void TheAllInClaimsEverythingFromRankFourDownAndNeverFromRankOneToThree()
		{
			foreach (var holder in new[] { ArmyDuty.GuardLend, ArmyDuty.TeamPush, ArmyDuty.PincerArm, ArmyDuty.AllIn, ArmyDuty.Stalemate, ArmyDuty.AttackLiveness, ArmyDuty.StandaloneAttack, ArmyDuty.Raid, ArmyDuty.Idle, ArmyDuty.None })
				Assert.That(DutyArbiter.AllInMayTake(holder), Is.True, holder.ToString());

			foreach (var holder in new[] { ArmyDuty.OwnBaseDefend, ArmyDuty.AllyDefend })
				Assert.That(DutyArbiter.AllInMayTake(holder), Is.False, holder.ToString());
		}

		[Test]
		public void EveryRefusedUnitIsLoggedWithTheRankThatHoldsIt()
		{
			var refusals = DutyArbiter.AllInRefusals(new (uint, ArmyDuty)[]
			{
				(30, ArmyDuty.Idle), (12, ArmyDuty.AllyDefend), (7, ArmyDuty.OwnBaseDefend), (5, ArmyDuty.Raid), (9, ArmyDuty.OwnBaseDefend),
			});
			Assert.That(refusals, Is.EqualTo(new[] { "7:R3", "9:R3", "12:R2" }), "sorted by unit id, each with its holder's rank");
			Assert.That(DutyArbiter.AllInRefusals(Array.Empty<(uint, ArmyDuty)>()), Is.Empty);
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
