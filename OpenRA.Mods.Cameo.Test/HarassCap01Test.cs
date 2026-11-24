#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	/// <summary>
	/// HARASS-CAP-01 (V02 RCA, USER P0 2026-11-24): the raiders held a large part of every seat's army while the team blob never reached the push
	/// bar. The cap (tl_harass_cap), the minimum raid (tl_harass_min) and the raider recall (tl_raider_recall) are pure rules plus thin wiring.
	/// </summary>
	[TestFixture]
	public class HarassCap01Test
	{
		const string Bots = "OpenRA.Mods.CA/Traits/BotModules/";

		static string RepoFile(string relative)
		{
			var dir = new DirectoryInfo(AppContext.BaseDirectory);
			while (dir != null && !File.Exists(Path.Combine(dir.FullName, "mod.config")))
				dir = dir.Parent;

			Assert.That(dir, Is.Not.Null, "repository root (mod.config) not found above " + AppContext.BaseDirectory);
			return File.ReadAllText(Path.Combine(dir.FullName, relative));
		}

		// Every unit that arrives is offered to the raiders first (the worst case: all of them are raider types). Returns the raiders at the end.
		static int Grow(int units, int capPercent, int minRaid, out int firstRaiderAtTotal)
		{
			var raiders = 0;
			firstRaiderAtTotal = -1;
			for (var total = 1; total <= units; total++)
			{
				if (HarassCapCA.Allows(raiders, total, capPercent, minRaid))
				{
					raiders++;
					if (firstRaiderAtTotal < 0)
						firstRaiderAtTotal = total;
				}

				Assert.That(raiders * 100L, Is.LessThanOrEqualTo(total * (long)capPercent), "the raiders never exceed the cap (total " + total + ")");
			}

			return raiders;
		}

		// ---- the cap ----

		[Test]
		public void AUnitBeyondTheCapIsRefusedAndOneWithinItIsTaken()
		{
			Assert.That(HarassCapCA.Allows(1, 8, 25), Is.True, "2 of 8 = exactly 25%");
			Assert.That(HarassCapCA.Allows(2, 8, 25), Is.False, "3 of 8 is over 25%");
			Assert.That(HarassCapCA.Allows(0, 3, 25), Is.False, "1 of 3 is over 25%: a small army raids not at all");
			Assert.That(HarassCapCA.Allows(0, 4, 25), Is.True);
			Assert.That(HarassCapCA.Allows(2, 8, 40), Is.True, "3 of 8 = 37.5%");
			Assert.That(HarassCapCA.Allows(3, 8, 40), Is.False, "4 of 8 = 50% is over 40%");
			Assert.That(HarassCapCA.Allows(3, 10, 40), Is.True, "the fourth of 10 = 40%");
			Assert.That(HarassCapCA.Allows(4, 10, 40), Is.False);
		}

		[Test]
		public void TheEdgesOfTheCapPercent()
		{
			Assert.That(HarassCapCA.Allows(500, 10, 100), Is.True, "100 = no cap");
			Assert.That(HarassCapCA.Allows(0, 1000, 0), Is.False, "0 = no raiders");
			Assert.That(HarassCapCA.Allows(0, 1000, -5), Is.False, "a negative percent is 0");
			Assert.That(HarassCapCA.Allows(0, 0, 25), Is.False, "an empty seat (total 0) is never the case: the candidate counts, but 0 stays safe");
			Assert.That(HarassCapCA.MaxRaiders(60, 25), Is.EqualTo(15));
			Assert.That(HarassCapCA.MaxRaiders(60, 40), Is.EqualTo(24));
			Assert.That(HarassCapCA.MaxRaiders(7, 25), Is.EqualTo(1));
			Assert.That(HarassCapCA.MaxRaiders(60, 100), Is.EqualTo(60));
			Assert.That(HarassCapCA.MaxRaiders(-3, 25), Is.EqualTo(0));
		}

		[Test]
		public void V02ASeatWhoseEveryNewUnitIsARaiderTypeKeepsThreeQuartersInTheBlob()
		{
			// 40 and 60 units, every one of them a raider type (the V02 seats had 15-40 units with most in guerrilla squads).
			var raiders40 = Grow(40, 25, 0, out _);
			Assert.That(raiders40, Is.EqualTo(10), "25% of 40");
			Assert.That(40 - raiders40, Is.GreaterThanOrEqualTo(30), "the pool (the blob) keeps at least 75%");

			var raiders60 = Grow(60, 25, 0, out _);
			Assert.That(raiders60, Is.EqualTo(15));
			Assert.That(60 - raiders60, Is.EqualTo(45));
		}

		[Test]
		public void TheGuerrillaPersonalityRunsAtFortyPercent()
		{
			Assert.That(Grow(40, 40, 0, out _), Is.EqualTo(16));
			Assert.That(Grow(100, 40, 0, out _), Is.EqualTo(40));
		}

		[Test]
		public void WithTheMinimumRaidNoRaiderIsTakenBeforeTheCapCanHoldAWholeRaid()
		{
			// 25% x 12 = 3: the first raider joins when the seat has 12 combat units, not before; then the other two of the first raid follow.
			var raiders = Grow(30, 25, 3, out var first);
			Assert.That(first, Is.EqualTo(12), "a squad that could never reach 3 units would only hold units back");
			Assert.That(raiders, Is.EqualTo(7), "25% of 30, rounded down");

			// The guerrilla personality (40%) starts at 8 units.
			Grow(30, 40, 3, out var firstGuerrilla);
			Assert.That(firstGuerrilla, Is.EqualTo(8));

			// Off (minRaid 0): the first raider as soon as the cap holds one (4 units at 25%).
			Grow(30, 25, 0, out var firstPlain);
			Assert.That(firstPlain, Is.EqualTo(4));
		}

		[Test]
		public void RaidersAlreadyPastTheMinimumAreNotHeldBackByTheMinimumClause()
		{
			// A raid of 3 survives, the army shrinks: the clause only guards the FIRST raid; the cap alone decides the rest.
			Assert.That(HarassCapCA.Allows(3, 16, 25, 3), Is.True, "4 of 16 = 25%");
			Assert.That(HarassCapCA.Allows(3, 15, 25, 3), Is.False, "4 of 15 is over 25%");
		}

		// ---- the minimum raid ----

		[Test]
		public void ARaidLaunchesWithTheMinimumOtherwiseItMergesHoldsOrGoesToTheBlob()
		{
			Assert.That(HarassCapCA.RaidDecision(3, 3, 0, 1500, false), Is.EqualTo(HarassCapCA.RaidAction.Go));
			Assert.That(HarassCapCA.RaidDecision(7, 3, 0, 1500, true), Is.EqualTo(HarassCapCA.RaidAction.Go), "a big squad never merges");
			Assert.That(HarassCapCA.RaidDecision(2, 3, 10, 1500, true), Is.EqualTo(HarassCapCA.RaidAction.Merge), "a bigger quiet squad with room takes it");
			Assert.That(HarassCapCA.RaidDecision(2, 3, 10, 1500, false), Is.EqualTo(HarassCapCA.RaidAction.Hold));
			Assert.That(HarassCapCA.RaidDecision(2, 3, 1499, 1500, false), Is.EqualTo(HarassCapCA.RaidAction.Hold));
			Assert.That(HarassCapCA.RaidDecision(2, 3, 1500, 1500, false), Is.EqualTo(HarassCapCA.RaidAction.ToPool), "after the hold its units go to the blob");
			Assert.That(HarassCapCA.RaidDecision(1, 3, 99999, 1500, false), Is.EqualTo(HarassCapCA.RaidAction.ToPool));
		}

		[Test]
		public void TheEdgesOfTheMinimumRaid()
		{
			Assert.That(HarassCapCA.RaidDecision(1, 0, 0, 1500, false), Is.EqualTo(HarassCapCA.RaidAction.Go), "minimum 0 or 1: every squad is big enough");
			Assert.That(HarassCapCA.RaidDecision(1, 1, 0, 1500, false), Is.EqualTo(HarassCapCA.RaidAction.Go));
			Assert.That(HarassCapCA.RaidDecision(2, 3, 0, 0, false), Is.EqualTo(HarassCapCA.RaidAction.ToPool), "a zero hold pools at once: no unit is held back for ever");
			Assert.That(HarassCapCA.RaidDecision(2, 3, 0, -10, false), Is.EqualTo(HarassCapCA.RaidAction.ToPool));
		}

		// ---- the recall and the order that survives a switch ----

		[Test]
		public void TheRecallStartsAtTheLateEscalationTickAndOnlyWithItsSwitch()
		{
			Assert.That(HarassCapCA.RecallActive(true, 24999, 25000), Is.False);
			Assert.That(HarassCapCA.RecallActive(true, 25000, 25000), Is.True);
			Assert.That(HarassCapCA.RecallActive(true, 60000, 25000), Is.True);
			Assert.That(HarassCapCA.RecallActive(false, 60000, 25000), Is.False);
		}

		[Test]
		public void V02TheOrderThatLostItsOnlySquadToAPersonalitySwitchIsGivenTheGraceAgain()
		{
			// V02: order 6003, its only reporting squad reported until the switch at 6190, the order was cancelled at ~6430 (no_reports, grace 500).
			const int grace = 500;
			var issue = 6003;
			Assert.That(HarassCapCA.NoReportSince(issue, -1), Is.EqualTo(issue), "no report yet: counts from the issue");
			Assert.That(HarassCapCA.NoReportSince(issue, 6190), Is.EqualTo(6190), "counts from the last report");
			Assert.That(HarassCapCA.NoReportSince(issue, 5000), Is.EqualTo(issue), "a report older than the order cannot move the start back");

			Assert.That(6600 - HarassCapCA.NoReportSince(issue, 6190) > grace, Is.False, "the new module still has until 6690 to re-join");
			Assert.That(6700 - HarassCapCA.NoReportSince(issue, 6190) > grace, Is.True, "an order nobody re-joins still dies");
		}

		// ---- wiring pins ----

		[Test]
		public void TheSwitchesDefaultOffAreOnInSixPersonalitiesNotClassicAndHaveTheirOwnRows()
		{
			var info = RepoFile(Bots + "SquadManagerBotModuleCAInfo.HarassCap.cs");
			Assert.That(info, Does.Contain("public readonly bool HarassCapEnabled = false;"));
			Assert.That(info, Does.Contain("public readonly int HarassCapPercent = 25;"));
			Assert.That(info, Does.Contain("public readonly bool HarassMinEnabled = false;"));
			Assert.That(info, Does.Contain("public readonly int HarassMinUnits = 3;"));
			Assert.That(info, Does.Contain("public readonly int HarassMinHoldTicks = 1500;"));
			Assert.That(info, Does.Contain("public readonly bool TeamLeadRaiderRecallEnabled = false;"));

			var yaml = RepoFile("mods/cameo/ai/ai.yaml");
			var classic = yaml.IndexOf("SquadManagerBotModuleCA@classic:", StringComparison.Ordinal);
			Assert.That(classic, Is.GreaterThan(0));
			foreach (var key in new[] { "HarassCapEnabled", "HarassMinEnabled", "TeamLeadRaiderRecallEnabled" })
			{
				Assert.That(Regex.Matches(yaml[..classic], @"\n\t\t" + key + ": true").Count, Is.EqualTo(6), key);
				Assert.That(yaml[classic..], Does.Not.Contain(key), key + ": @classic keeps the default (off)");
			}

			// 25 stays the default; only the guerrilla personality raises the cap, to 40.
			Assert.That(Regex.Matches(yaml, @"\n\t\tHarassCapPercent: ").Count, Is.EqualTo(1));
			var guerrilla = yaml.IndexOf("SquadManagerBotModuleCA@guerrilla:", StringComparison.Ordinal);
			Assert.That(guerrilla, Is.GreaterThan(0));
			Assert.That(yaml.IndexOf("\n\t\tHarassCapPercent: 40", StringComparison.Ordinal), Is.GreaterThan(guerrilla));
			Assert.That(yaml.IndexOf("\n\t\tHarassCapPercent: 40", StringComparison.Ordinal), Is.LessThan(classic));

			Assert.That(AblationIds.TeamLeadHarassCap, Is.EqualTo("tl_harass_cap"));
			Assert.That(AblationIds.TeamLeadHarassMin, Is.EqualTo("tl_harass_min"));
			Assert.That(AblationIds.TeamLeadRaiderRecall, Is.EqualTo("tl_raider_recall"));
		}

		[Test]
		public void EveryGuerrillaAndHarassAssignmentAsksTheCapFirstAndTheSwitchesAreGatedByInfoAndRow()
		{
			var main = RepoFile(Bots + "SquadManagerBotModuleCA.cs");
			Assert.That(main, Does.Contain("&& guerrillaRoll && HarassCapAllows() && OpenGuerrillaSquad(bot) is { } airGuerrilla)"));
			Assert.That(main, Does.Contain("&& guerrillaRoll && HarassCapAllows() && OpenGuerrillaSquad(bot) is { } guerrillaForce)"));
			Assert.That(main, Does.Contain("else if (Info.HarasserTypes.Contains(a.Info.Name) && HarassCapAllows())"));
			Assert.That(main, Does.Contain("Info.JoinGuerrilla > 0 && HarassCapAllows() ? OpenGuerrillaSquad(bot) : null;"), "a released defender is capped too");
			Assert.That(main, Does.Contain("else if (Info.HarasserTypes.Contains(name) && HarassCapAllows())"));
			Assert.That(main, Does.Contain("HarassMinOn ? openSquads.MaxByOrDefault(s => s.Units.Count) : openSquads.MinByOrDefault(s => s.Units.Count);"), "off = the smallest first, as before");

			var hold = main.IndexOf("if (HarassMinHolds(s))", StringComparison.Ordinal);
			Assert.That(hold, Is.GreaterThan(0));
			Assert.That(hold, Is.LessThan(main.IndexOf("if (TeamLeadOn)\n", hold, StringComparison.Ordinal)), "the minimum raid is asked before the team feature");
			Assert.That(main, Does.Contain("TeamLeadSwitchTakeover();"));

			var cap = RepoFile(Bots + "SquadManagerBotModuleCA.HarassCap.cs");
			Assert.That(cap, Does.Contain("internal bool HarassCapOn => Info.HarassCapEnabled && !ablateHarassCap.Off(Player, AblationIds.TeamLeadHarassCap);"));
			Assert.That(cap, Does.Contain("internal bool HarassMinOn => Info.HarassMinEnabled && !ablateHarassMin.Off(Player, AblationIds.TeamLeadHarassMin);"));
			Assert.That(cap, Does.Contain("Info.TeamLeadRaiderRecallEnabled && !ablateRaiderRecall.Off(Player, AblationIds.TeamLeadRaiderRecall)"));
			Assert.That(cap, Does.Contain("if (!HarassCapOn)\n\t\t\t\treturn true;"), "off = every unit is taken as before");
			Assert.That(cap, Does.Contain("DiagnosticSquadEvent(s, \"raid_merge\""));
			Assert.That(cap, Does.Contain("DiagnosticSquadEvent(s, \"raid_pool\""));
		}

		[Test]
		public void TheRecallJoinsIdleAndMarchingRaidersToTheOpenOrderAndTheReportGraceCountsFromTheLastReport()
		{
			var wiring = RepoFile(Bots + "SquadManagerBotModuleCA.TeamLead.cs");
			Assert.That(wiring, Does.Contain("return s.Type is SquadCAType.Guerrilla or SquadCAType.Harass && TeamLeadRaiderRecallOn"));
			Assert.That(wiring, Does.Contain("(AggTeamIsQuietState(state) || state is GroundUnitsAttackMoveStateCA) && !AggTeamSquadDefends(s);"), "a squad in a fight finishes it");
			Assert.That(wiring, Does.Contain("s.Type is SquadCAType.Guerrilla or SquadCAType.Harass && TeamLeadWaveSquad(s) && TeamLeadOpenOrder() is { } recalled && !TeamLeadRaiderInContact(s))"),
				"CM FIX: a marching raider squad with an enemy in its scan radius is in contact and is never pulled out of it");
			Assert.That(wiring, Does.Contain("s.FuzzyStateMachine?.CurrentState is GroundUnitsAttackMoveStateCA"));
			Assert.That(wiring, Does.Contain("FindClosestEnemy(s.Units[0].Actor, WDist.FromCells(Info.AttackScanRadius), s) != null;"), "the same scan the gather state makes");
			Assert.That(wiring, Does.Contain("TeamLeadRaiderRecallOn && !s.IsHunting"), "a finish-mode hunting squad keeps its hunt");
			var blob = wiring[wiring.IndexOf("(int Units, long Value) TeamLeadBlob()", StringComparison.Ordinal)..wiring.IndexOf("bool TeamLeadWaveSquad(SquadCA s)", StringComparison.Ordinal)];
			Assert.That(blob, Does.Not.Contain("TeamLeadRaiderInContact"), "the blob count never runs the enemy scan");
			Assert.That(wiring.IndexOf("TeamLeadOpenOrder() is { } recalled", StringComparison.Ordinal), Is.LessThan(wiring.IndexOf("s.Type is SquadCAType.Guerrilla or SquadCAType.Harass && TeamLeadJointHarassOn", StringComparison.Ordinal)), "the recall is asked before the joint harass group");
			Assert.That(wiring, Does.Contain("\"raider_recall seq=\""));
			Assert.That(wiring, Does.Contain("order.LastReportTick = World.WorldTick;"));
			Assert.That(wiring, Does.Contain("TeamLeadOrderSurviveOn ? HarassCapCA.NoReportSince(order.IssueTick, order.LastReportTick) : order.IssueTick"), "survive row off = the old grace");
			Assert.That(wiring, Does.Contain("void TeamLeadSwitchTakeover()"));
			Assert.That(wiring, Does.Contain("assignRolesTicks = BotPeriod.F1(World.WorldTick, 0, Info.AssignRolesInterval);"));

			// The Rush rule is the old one, untouched.
			Assert.That(wiring, Does.Contain("return AggTeamIsQuietState(state) && !AggTeamSquadDefends(s);"));
		}
	}
}
