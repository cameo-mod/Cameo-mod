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
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	/// <summary>
	/// V07 (USER master replay 2026-11-24, via Opus): PINCER-02 (tl_pincer_wide), ALL-IN-01 (tl_all_in, with the solo case), TEAM-DEFEND units (tl_defend_team).
	/// One switch per row (release, default off, ON in the six personalities, never @classic) and one DEV row each. The pure rules are tested here, the wiring by pins.
	/// </summary>
	[TestFixture]
	public class V07Test
	{
		const string Bots = "OpenRA.Mods.CA/Traits/BotModules/";
		const int Cell = 1024;

		static string RepoFile(string relative)
		{
			var dir = new DirectoryInfo(AppContext.BaseDirectory);
			while (dir != null && !File.Exists(Path.Combine(dir.FullName, "mod.config")))
				dir = dir.Parent;

			Assert.That(dir, Is.Not.Null, "repository root (mod.config) not found above " + AppContext.BaseDirectory);
			return File.ReadAllText(Path.Combine(dir.FullName, relative)).Replace("\r\n", "\n");
		}

		static readonly WPos Target = new(50 * Cell, 50 * Cell, 0);
		static readonly WPos Home = new(50 * Cell, 150 * Cell, 0);

		// A point `cells` from the target, `degrees` round from the line target-to-home (negative = the other side).
		static WPos Around(int degrees, int cells)
		{
			var line = new WVec(0, cells * Cell, 0);
			var p = Target + line.Rotate(WRot.FromYaw(WAngle.FromDegrees(degrees)));
			return new WPos(p.X, p.Y, 0);
		}

		// ---- PINCER-02 ----

		[Test]
		public void TheApproachSeparationIsTheAngleAtTheTargetAndAnythingFrom90To180Counts()
		{
			Assert.That(PincerWideCA.SeparationDegrees(Target, Around(45, 24), Around(-45, 24)), Is.InRange(89, 91), "two 45 degree flanks = 90");
			Assert.That(PincerWideCA.SeparationDegrees(Target, Around(70, 24), Around(-70, 24)), Is.InRange(139, 141), "the default spread = 140");
			Assert.That(PincerWideCA.SeparationDegrees(Target, Around(90, 24), Around(-90, 24)), Is.InRange(179, 180), "opposite sides = 180");
			Assert.That(PincerWideCA.SeparationDegrees(Target, Around(120, 24), Around(-120, 24)), Is.InRange(119, 121), "round the back: the shorter way (240 -> 120)");
			Assert.That(PincerWideCA.SeparationDegrees(Target, Around(10, 24), Around(10, 30)), Is.LessThanOrEqualTo(1), "same side = one approach");
			Assert.That(PincerWideCA.SeparationDegrees(Target, Target, Around(70, 24)), Is.EqualTo(0), "a point on the target has no bearing");

			Assert.That(PincerWideCA.AngleOk(89, 90), Is.False);
			Assert.That(PincerWideCA.AngleOk(90, 90), Is.True);
			Assert.That(PincerWideCA.AngleOk(135, 90), Is.True, "any separation between 90 and 180 counts, no exact opposite sides");
			Assert.That(PincerWideCA.AngleOk(180, 90), Is.True);
			Assert.That(PincerWideCA.AngleOk(59, 10), Is.False, "the minimum never goes below the floor of 60");
			Assert.That(PincerWideCA.AngleOk(60, 10), Is.True);
			Assert.That(PincerWideCA.AngleOk(180, 400), Is.True, "nor above 180");
		}

		[Test]
		public void TheArmBarIsAShareOfTheWaveMinBarAndTheBlobGateEqualsTheOldOneAt100()
		{
			Assert.That(PincerWideCA.ArmBar(8, 100), Is.EqualTo(8));
			Assert.That(PincerWideCA.ArmBar(8, 75), Is.EqualTo(6));
			Assert.That(PincerWideCA.ArmBar(9, 75), Is.EqualTo(7), "rounds up");
			Assert.That(PincerWideCA.ArmBar(8, 10), Is.EqualTo(4), "never below half");
			Assert.That(PincerWideCA.ArmBar(8, 400), Is.EqualTo(8), "never above the bar");
			Assert.That(PincerWideCA.ArmBar(1, 50), Is.EqualTo(1));
			Assert.That(PincerWideCA.ArmBar(0, 75), Is.EqualTo(0), "no bar stays no bar");

			foreach (var (units, value) in new[] { (15, 1500L), (16, 1599L), (16, 1600L), (40, 9000L), (3, 100L), (8, 8000L) })
			{
				Assert.That(PincerWideCA.PincerBlob(units, value, 3, true, 8, 800, 100), Is.EqualTo(TeamLeadCA.PincerBlob(units, value, 3, true, 8, 800)),
					"100 percent = the TEAM-PINCER-01 gate: " + units + "/" + value);
				Assert.That(PincerWideCA.PincerBlob(units, value, 3, false, 0, 0, 75), Is.EqualTo(TeamLeadCA.PincerBlob(units, value, 3, false, 0, 0)),
					"no WAVE-MIN tier = the join floor only: " + units);
			}

			Assert.That(TeamLeadCA.PincerBlob(12, 1200, 3, true, 8, 800), Is.False, "the old gate: twice the bars");
			Assert.That(PincerWideCA.PincerBlob(12, 1200, 3, true, 8, 800, 75), Is.True, "75 percent per arm = 6/600 each, 12/1200 together");
			Assert.That(PincerWideCA.PincerBlob(11, 1200, 3, true, 8, 800, 75), Is.False, "one unit short of two arm bars");
			Assert.That(PincerWideCA.PincerBlob(12, 1199, 3, true, 8, 800, 75), Is.False, "one cash short of two arm bars");
			Assert.That(PincerWideCA.PincerBlob(5, 99999, 3, true, 8, 800, 75), Is.False, "never less than twice the join floor and the arm bars");
		}

		[Test]
		public void BothArmsMustHoldTheArmBarOnTheirOwnUnitsAndValue()
		{
			Assert.That(PincerWideCA.ArmsHold(6, 600, 6, 600, 8, 800, 75), Is.True);
			Assert.That(PincerWideCA.ArmsHold(5, 600, 6, 600, 8, 800, 75), Is.False, "an arm short of units");
			Assert.That(PincerWideCA.ArmsHold(6, 600, 6, 599, 8, 800, 75), Is.False, "an arm short of value");
			Assert.That(PincerWideCA.ArmsHold(12, 1200, 2, 100, 8, 800, 75), Is.False, "a rich total does not make up for a thin arm");
			Assert.That(PincerWideCA.ArmsHold(7, 700, 5, 500, 8, 800, 100), Is.False, "100 = a full WAVE-MIN per arm");
			Assert.That(PincerWideCA.ArmsHold(0, 0, 0, 0, 0, 0, 75), Is.True, "no bars: nothing to hold");
		}

		[Test]
		public void TheCandidateFanStartsWithThePincer01ListAndAddsWiderAndNarrowerApproaches()
		{
			var old = TeamLeadCA.PincerStaging(Home, Target, -1, 24, 70);
			var fan = PincerWideCA.Candidates(Home, Target, -1, 24, 70);
			Assert.That(old.Count, Is.EqualTo(6));
			Assert.That(fan.Count, Is.EqualTo(16), "6 old + 10 more (90/80/100/60/110/50/120 at the stand-off, 90/80/100 pulled in)");
			for (var i = 0; i < old.Count; i++)
				Assert.That(fan[i], Is.EqualTo(old[i]), "a free map gives the same staging points as before: " + i);

			Assert.That(PincerWideCA.Candidates(Home, Target, -1, 24, 90).Count, Is.EqualTo(14), "a candidate the old list holds is not repeated");
			Assert.That(PincerWideCA.Candidates(Target, Target, -1, 24, 70), Is.Empty, "no line target-to-home, no candidates");

			var a = PincerWideCA.Candidates(Home, Target, -1, 24, 70);
			var b = PincerWideCA.Candidates(Home, Target, 1, 24, 70);
			Assert.That(PincerWideCA.SeparationDegrees(Target, a[0], b[0]), Is.InRange(139, 141), "the preferred pair is the 140 degree pincer");
			Assert.That(((a[0] - Target).HorizontalLength + 512) / Cell, Is.EqualTo(24), "at the stand-off");

			var spread = new HashSet<int>();
			for (var i = 0; i < a.Count; i++)
				spread.Add(PincerWideCA.SeparationDegrees(Target, a[i], b[i]));
			Assert.That(spread.Count, Is.GreaterThan(5), "the fan offers many separations");
		}

		[Test]
		public void ThePairSearchTakesTheFirstLegalPairWhoseRealSeparationIs90To180AndAsksEachCandidateOnce()
		{
			var a = PincerWideCA.Candidates(Home, Target, -1, 24, 70);
			var b = PincerWideCA.Candidates(Home, Target, 1, 24, 70);
			var askedA = new int[a.Count];
			var askedB = new int[b.Count];
			WPos? Free(List<WPos> list, int[] asked, int i)
			{
				asked[i]++;
				return list[i];
			}

			Assert.That(PincerWideCA.PickPair(Target, a.Count, i => Free(a, askedA, i), b.Count, i => Free(b, askedB, i), 90, 16, out var pa, out var pb, out var angle), Is.True);
			Assert.That(pa, Is.EqualTo(a[0]));
			Assert.That(pb, Is.EqualTo(b[0]));
			Assert.That(angle, Is.InRange(139, 141));
			Assert.That(askedA[0] + askedB[0], Is.EqualTo(2), "rank 0 first");
			Assert.That(askedA[3] + askedB[3] + askedA[5] + askedB[5], Is.EqualTo(0), "a free map asks nothing further");

			// Side A has no legal point at the preferred angle or the next two: the search takes the next one instead of giving the pincer up.
			var blockedA = new[] { 0, 1, 2 };
			var cnt = 0;
			WPos? Blocked(int i)
			{
				cnt++;
				return Array.IndexOf(blockedA, i) >= 0 ? null : a[i];
			}

			Assert.That(PincerWideCA.PickPair(Target, a.Count, Blocked, b.Count, i => b[i], 90, 16, out pa, out pb, out angle), Is.True, "side A blocked at its best three angles");
			Assert.That(pb, Is.EqualTo(b[0]), "side B keeps its preferred point");
			Assert.That(angle, Is.GreaterThanOrEqualTo(90));
			Assert.That(cnt, Is.LessThanOrEqualTo(a.Count), "each candidate is asked at most once");

			// Only narrow approaches are legal (two points 40 degrees round on each side = 80 degrees apart): that is one approach, not a pincer.
			WPos? Narrow(int degrees) => Around(degrees, 24);
			Assert.That(PincerWideCA.PickPair(Target, 1, _ => Narrow(-40), 1, _ => Narrow(40), 90, 16, out _, out _, out angle), Is.False);
			Assert.That(PincerWideCA.PickPair(Target, 1, _ => Narrow(-40), 1, _ => Narrow(40), 60, 16, out _, out _, out angle), Is.True, "yaml 60 accepts the 80 degree pair");
			Assert.That(angle, Is.InRange(79, 81));

			// Not exactly opposite, still a pincer: 100 and 130 degrees round on the two sides = 130 apart (the long way is 230).
			Assert.That(PincerWideCA.PickPair(Target, 1, _ => Narrow(-100), 1, _ => Narrow(130), 90, 16, out _, out _, out angle), Is.True);
			Assert.That(angle, Is.InRange(129, 131));

			// Both blocked everywhere: no pincer.
			Assert.That(PincerWideCA.PickPair(Target, a.Count, _ => null, b.Count, i => b[i], 90, 16, out _, out _, out _), Is.False);
			Assert.That(PincerWideCA.PickPair(Target, 0, _ => null, 0, _ => null, 90, 16, out _, out _, out _), Is.False, "no candidates, no pair");

			// Wide apart in angle but too close together in cells: one blob.
			Assert.That(PincerWideCA.PickPair(Target, 1, _ => Around(-60, 6), 1, _ => Around(60, 6), 90, 16, out _, out _, out _), Is.False, "13 cells apart is under 16");
		}

		[Test]
		public void PincerWideIsOffByDefaultOnInTheSixPersonalitiesAndNeverInClassic()
		{
			var info = RepoFile(Bots + "SquadManagerBotModuleCAInfo.V07.cs");
			Assert.That(info, Does.Contain("public readonly bool TeamLeadPincerWideEnabled = false;"));
			Assert.That(info, Does.Contain("public readonly int TeamLeadPincerMinAngleDegrees = 90;"));
			Assert.That(info, Does.Contain("public readonly int TeamLeadPincerArmPercent = 100;"));
			Assert.That(AblationIds.TeamLeadPincerWide, Is.EqualTo("tl_pincer_wide"));

			var yaml = RepoFile("mods/cameo/ai/ai.yaml");
			var classic = yaml.IndexOf("SquadManagerBotModuleCA@classic:", StringComparison.Ordinal);
			Assert.That(classic, Is.GreaterThan(0));
			Assert.That(Regex.Matches(yaml[..classic], "\n\t\tTeamLeadPincerWideEnabled: true").Count, Is.EqualTo(6));
			Assert.That(yaml[classic..], Does.Not.Contain("TeamLeadPincerWide"), "never in @classic");
		}

		[Test]
		public void PincerWideIsWiredBehindItsOwnSwitchAndLogsTheFiredPincerWithItsAngle()
		{
			var v07 = RepoFile(Bots + "SquadManagerBotModuleCA.V07.cs");
			Assert.That(v07, Does.Contain("internal bool TeamLeadPincerWideOn => TeamLeadPincerOn && Info.TeamLeadPincerWideEnabled && !ablatePincerWide.Off(Player, AblationIds.TeamLeadPincerWide);"));
			Assert.That(v07, Does.Contain("PincerWideCA.ArmsHold(units0, value0, units1, value1, minUnits, minValue, Info.TeamLeadPincerArmPercent)"));
			Assert.That(v07, Does.Contain("DiagnosticTeamEvent(evt, \"seq=\" + order.Seq + \" angle=\" + angle"));

			var wiring = RepoFile(Bots + "SquadManagerBotModuleCA.TeamLead.cs");
			Assert.That(wiring, Does.Contain("if (TeamLeadPincerWideOn)\n\t\t\t\treturn TeamLeadPincerArmCollapse(order, now);"), "the per-arm gate replaces the total gate");
			Assert.That(wiring, Does.Contain("PincerWideCA.PincerBlob(blob.Units, blob.Value, Info.TeamLeadJoinMinUnits"), "the plan gate reads the arm bar");
			Assert.That(wiring, Does.Contain(": TeamLeadCA.PincerBlob(blob.Units, blob.Value, Info.TeamLeadJoinMinUnits"), "the old gate when the switch is off");
			Assert.That(wiring, Does.Contain("PincerWideCA.PickPair(target, listA.Count"), "the fanned pair search");
			Assert.That(wiring, Does.Contain("first = Stage(-1);\n\t\t\t\tsecond = Stage(1);"), "the old staging when the switch is off");
			Assert.That(wiring, Does.Contain("TeamLeadPincerRecord(\"pincer_fired\", order,"));
			Assert.That(wiring, Does.Contain("TeamLeadPincerRecord(\"pincer_planned\", order,"));
			Assert.That(RepoFile(Bots + "BotModuleLogic/TeamLeadCA.cs"), Does.Contain("public int ProngUnits(int prong)"));
		}

		// ---- ALL-IN-01 + SOLO-LEAD ----

		[Test]
		public void TheAllInTriggersAreCapIdleHugeAndNearlyWonInThatOrder()
		{
			Assert.That(AllInCA.IdleHuge(60, 60), Is.True);
			Assert.That(AllInCA.IdleHuge(59, 60), Is.False);
			Assert.That(AllInCA.IdleHuge(500, 0), Is.False, "0 = this trigger is off");

			// 100 ticks past the start, ours 8000, theirs 2000 over 5 structures: 400 percent exactly.
			Assert.That(AllInCA.NearlyWon(15000, 15000, 8000, 2000, 5, 400), Is.True);
			Assert.That(AllInCA.NearlyWon(15000, 15000, 7999, 2000, 5, 400), Is.False, "one cash short of the percent");
			Assert.That(AllInCA.NearlyWon(14999, 15000, 90000, 100, 1, 400), Is.False, "a scouted early outpost is not a won game");
			Assert.That(AllInCA.NearlyWon(20000, 15000, 90000, 0, 0, 400), Is.False, "no known enemy structure = nothing to compare (blind)");
			Assert.That(AllInCA.NearlyWon(20000, 15000, 0, 0, 3, 400), Is.False, "no army, no all-in");
			Assert.That(AllInCA.NearlyWon(20000, 15000, 1000, 2000, 3, 10), Is.False, "the percent never goes below 100: half the enemy value is not won");
			Assert.That(AllInCA.NearlyWon(20000, 15000, 2000, 2000, 3, 10), Is.True, "equal value passes the 100 floor");

			Assert.That(AllInCA.Why(true, true, true), Is.EqualTo("cap"));
			Assert.That(AllInCA.Why(false, true, true), Is.EqualTo("idle"));
			Assert.That(AllInCA.Why(false, false, true), Is.EqualTo("won"));
			Assert.That(AllInCA.Why(false, false, false), Is.Null);

			Assert.That(AllInCA.JoinMinUnits(true, 6, 2), Is.EqualTo(2), "an all-in order takes the small group");
			Assert.That(AllInCA.JoinMinUnits(true, 6, 0), Is.EqualTo(1), "never below one unit");
			Assert.That(AllInCA.JoinMinUnits(false, 6, 2), Is.EqualTo(6), "an ordinary order keeps its minimum");
			Assert.That(AllInCA.JoinsAgain(true, 5, 5), Is.True, "an all-in order takes the units that arrived since");
			Assert.That(AllInCA.JoinsAgain(false, 5, 5), Is.False, "an ordinary order is joined once");
			Assert.That(AllInCA.JoinsAgain(false, 4, 5), Is.True, "a new order is joined");
		}

		[Test]
		public void AnOrderKeepsTheFirstAllInReasonAndIsOrdinaryUntilMarked()
		{
			var team = new TeamLeadTeam();
			var order = team.Issue("a", Home, Target, 7, 100);
			Assert.That(order.AllIn, Is.False);
			Assert.That(order.AllInWhy, Is.Null);
			order.MarkAllIn("idle");
			order.MarkAllIn("cap");
			Assert.That(order.AllIn, Is.True);
			Assert.That(order.AllInWhy, Is.EqualTo("idle"), "the first trigger is the record");
		}

		[Test]
		public void ATeamOfOneEngagesOnlyWithTheSoloRuleAndThenLeadsItselfThroughAnAllInOrder()
		{
			var alone = new List<TeamLeadCA.Seat> { new("bot", 100, true) };
			var election = TeamLeadCA.Elect(alone, TeamLeadCA.Election.None, 200, 15, 1500);
			Assert.That(election.Leader, Is.EqualTo("bot"), "the one seat is elected");
			Assert.That(TeamLeadCA.TeamKey(alone), Is.EqualTo("bot"));

			Assert.That(TeamLeadCA.Engaged(alone, election), Is.False, "the old rule: two living bots");
			Assert.That(TeamLeadCA.Engaged(alone, election, 1), Is.True, "tl_solo_lead: a team of one leads itself");
			Assert.That(TeamLeadCA.Engaged(alone, TeamLeadCA.Election.None, 1), Is.False, "no leader yet = not engaged");
			Assert.That(TeamLeadCA.Engaged(new List<TeamLeadCA.Seat> { new("bot", 100, false) }, election, 1), Is.False, "a dead seat does not engage");
			Assert.That(TeamLeadCA.Engaged(alone, election, 0), Is.True, "the minimum never goes below one living seat");
			var two = new List<TeamLeadCA.Seat> { new("a", 10, true), new("b", 20, true) };
			Assert.That(TeamLeadCA.Engaged(two, TeamLeadCA.Elect(two, TeamLeadCA.Election.None, 200, 15, 1500)), Is.True, "two living bots engage as before");

			// The solo leader: its own pool is the team pool; 70 idle units trigger the all-in, the order publishes at once, and it is marked.
			var team = new TeamLeadTeam();
			team.Publish("bot", 70, 9000, 300);
			var pool = team.Pool(300, 400);
			Assert.That(pool.Units, Is.EqualTo(70));
			var why = AllInCA.Why(team.AnyAtCap(300, 400), AllInCA.IdleHuge(pool.Units, 60), false);
			Assert.That(why, Is.EqualTo("idle"));
			Assert.That(TeamLeadCA.BlobReady(pool.Units, pool.Value, 3, true, 8, 800), Is.True);
			Assert.That(TeamLeadCA.OrderDue(false, true, 300, 290, 1500, why != null), Is.True, "an all-in publishes without waiting for the interval");
			Assert.That(TeamLeadCA.OrderDue(false, true, 300, 290, 1500, false), Is.False, "an ordinary order waits for it");
			var order = team.Issue("bot", Home, Target, 7, 300);
			order.MarkAllIn(why);
			Assert.That(order.AllIn, Is.True);
			order.Report("squad", 9000, true, 70, 70);
			Assert.That(order.CommittedTotals(), Is.EqualTo((70, 9000L)), "the solo leader's own squad reports to its own order");

			// A cap on the one seat triggers it too.
			team.Publish("bot", 20, 2500, 400, true);
			Assert.That(AllInCA.Why(team.AnyAtCap(400, 400), AllInCA.IdleHuge(team.Pool(400, 400).Units, 60), false), Is.EqualTo("cap"));
		}

		[Test]
		public void AllInAndSoloLeadAreOffByDefaultOnInTheSixPersonalitiesAndNeverInClassic()
		{
			var info = RepoFile(Bots + "SquadManagerBotModuleCAInfo.V07.cs");
			Assert.That(info, Does.Contain("public readonly bool TeamLeadSoloEnabled = false;"));
			Assert.That(info, Does.Contain("public readonly bool TeamLeadAllInEnabled = false;"));
			Assert.That(info, Does.Contain("public readonly int TeamLeadAllInIdleUnits = 60;"));
			Assert.That(info, Does.Contain("public readonly int TeamLeadAllInWinPercent = 400;"));
			Assert.That(info, Does.Contain("public readonly int TeamLeadAllInStartTick = 15000;"));
			Assert.That(info, Does.Contain("public readonly int TeamLeadAllInJoinMinUnits = 2;"));
			Assert.That(AblationIds.TeamLeadAllIn, Is.EqualTo("tl_all_in"));
			Assert.That(AblationIds.TeamLeadSoloLead, Is.EqualTo("tl_solo_lead"));

			var yaml = RepoFile("mods/cameo/ai/ai.yaml");
			var classic = yaml.IndexOf("SquadManagerBotModuleCA@classic:", StringComparison.Ordinal);
			Assert.That(classic, Is.GreaterThan(0));
			foreach (var key in new[] { "TeamLeadSoloEnabled", "TeamLeadAllInEnabled" })
			{
				Assert.That(Regex.Matches(yaml[..classic], "\n\t\t" + key + ": true").Count, Is.EqualTo(6), key);
				Assert.That(yaml[classic..], Does.Not.Contain(key), key + " never in @classic");
			}
		}

		[Test]
		public void AllInAndSoloLeadAreWiredBehindTheirOwnSwitchesAndLogTheDecision()
		{
			var v07 = RepoFile(Bots + "SquadManagerBotModuleCA.V07.cs");
			Assert.That(v07, Does.Contain("internal bool TeamLeadAllInOn => TeamLeadOn && Info.TeamLeadAllInEnabled && !ablateAllIn.Off(Player, AblationIds.TeamLeadAllIn);"));
			Assert.That(v07, Does.Contain("bool TeamLeadSoloOn => TeamLeadActive && Info.TeamLeadSoloEnabled && !ablateSoloLead.Off(Player, AblationIds.TeamLeadSoloLead);"));
			Assert.That(v07, Does.Contain("internal bool TeamLeadAllInOrderOpen => TeamLeadOpenOrder() is { AllIn: true };"));
			Assert.That(v07, Does.Contain("if (!TeamLeadAllInOn)\n\t\t\t\treturn AllInReturn(now, null);"), "free of work with the switch off");
			Assert.That(v07, Does.Contain("TeamLeadPincerSample() ?? activeUnits.FirstOrDefault(a => !a.IsDead && a.IsInWorld && a.TraitOrDefault<Mobile>() != null)"), "the hunt starts from a ground unit");
			Assert.That(v07, Does.Contain("if (TeamLeadOpenOrder() is { } open && (open.AllIn || open.GatherOnly))\n\t\t\t\treturn AllInReturn(now, null);"), "no nearly-won scan for an order that needs no trigger");
			Assert.That(v07, Does.Contain("\"all_in_push\""));
			Assert.That(v07, Does.Contain("idle_home="));

			var wiring = RepoFile(Bots + "SquadManagerBotModuleCA.TeamLead.cs");
			Assert.That(wiring, Does.Contain("teamLeadEngaged = TeamLeadCA.Engaged(seats, team.Election, TeamLeadSoloOn ? 1 : 2);"), "a team of one engages only with the solo rule");
			Assert.That(wiring, Does.Contain("var allIn = TeamLeadAllInWhy(team, now);"));
			Assert.That(wiring, Does.Contain("|| allIn != null)"), "an all-in skips the interval wait");
			Assert.That(wiring, Does.Contain("if (allIn != null && TeamLeadOpenOrder() is { AllIn: false } open)"), "an open attack order turns all-in");
			Assert.That(wiring, Does.Contain("else if (allIn != null)\n\t\t\t\torder.MarkAllIn(allIn);"));
			Assert.That(wiring, Does.Contain("teamLeadAllInPicking = allIn != null && why == TeamLeadCA.PublishWhy.Ready;"));
			Assert.That(wiring, Does.Contain("(!AllInCA.JoinsAgain(order.AllIn, teamLeadJoinedSeq, order.Seq) && !order.GatherOnly && !order.Extending)"), "members re-join an all-in order");
			Assert.That(wiring, Does.Contain("AllInCA.JoinMinUnits(order.AllIn, Info.TeamLeadJoinMinUnits, Info.TeamLeadAllInJoinMinUnits)"));
			Assert.That(wiring, Does.Contain("(order.AllIn ? unitsHangingAroundTheBase.Count(TeamLeadJoinable) : unitsHangingAroundTheBase.Count)"), "an all-in order counts joinable units only");
			Assert.That(wiring, Does.Contain("(TeamLeadRaiderRecallOn || TeamLeadAllInOrderOpen)"), "raiders join an all-in order whatever the clock");
			Assert.That(wiring, Does.Contain("((TeamLeadHuntViaOrderOn && FinishModeActive) || teamLeadAllInPicking || StalemateScoutHunts)"), "an all-in order with no known target hunts");
			Assert.That(wiring, Does.Contain("TeamLeadAllInRecord(\"all_in_start\""));
			Assert.That(wiring, Does.Contain("TeamLeadAllInRecord(\"all_in_join\""));
			Assert.That(System.Text.RegularExpressions.Regex.Matches(wiring, "TeamLeadAllInPushRecord\\(order, now\\);").Count, Is.EqualTo(3), "all three push sites (pincer, plain, CONSOLIDATE-02 D1 bar_timeout)");
			Assert.That(RepoFile(Bots + "BotModuleLogic/TeamLeadCA.cs"), Does.Contain("public static bool Engaged(IReadOnlyList<Seat> seats, Election election, int minSeats = 2)"));
		}

		[Test]
		public void AfterThePushAnAllInRejoinIsAWaveMinGroupOrItWaits()
		{
			// Before the push the units merge into the gathering squad or gather with the blob: the gate does not apply.
			Assert.That(AllInCA.RejoinAllowed(false, 2, 200, true, 8, 800), Is.True);
			// After the push: units AND value must reach the WAVE-MIN bars; two units walking to the target alone are a trickle.
			Assert.That(AllInCA.RejoinAllowed(true, 2, 200, true, 8, 800), Is.False, "a pair does not form a squad of its own after the push");
			Assert.That(AllInCA.RejoinAllowed(true, 7, 5000, true, 8, 800), Is.False, "one unit short of WAVE-MIN");
			Assert.That(AllInCA.RejoinAllowed(true, 8, 799, true, 8, 800), Is.False, "one cash short of WAVE-MIN");
			Assert.That(AllInCA.RejoinAllowed(true, 8, 800, true, 8, 800), Is.True, "a WAVE-MIN group re-joins");
			Assert.That(AllInCA.RejoinAllowed(true, 2, 200, false, 0, 0), Is.True, "no WAVE-MIN tier: nothing to hold");
		}

		[Test]
		public void TheNearlyWonScanRunsOnceAnIntervalAndTheResultIsReused()
		{
			Assert.That(AllInCA.ScanDue(16000, -1, 250), Is.True, "never scanned");
			Assert.That(AllInCA.ScanDue(16100, 16000, 250), Is.False);
			Assert.That(AllInCA.ScanDue(16249, 16000, 250), Is.False);
			Assert.That(AllInCA.ScanDue(16250, 16000, 250), Is.True);
			Assert.That(AllInCA.ScanDue(16001, 16000, 0), Is.True, "an interval below 1 still rescans every tick, never divides");

			var info = RepoFile(Bots + "SquadManagerBotModuleCAInfo.V07.cs");
			Assert.That(info, Does.Contain("public readonly int TeamLeadAllInScanIntervalTicks = 250;"));
			var v07 = RepoFile(Bots + "SquadManagerBotModuleCA.V07.cs");
			Assert.That(v07, Does.Contain("if (AllInCA.ScanDue(now, allInScanTick, Info.TeamLeadAllInScanIntervalTicks))"));
			Assert.That(v07, Does.Contain("allInEnemyValue = TeamLeadKnownEnemyStructures(out allInEnemyStructures);"));
			Assert.That(v07, Does.Contain("var counted = allInCounted;\n\t\t\tcounted.Clear();"), "no new set per scan");
			Assert.That(v07, Does.Not.Contain("new HashSet<uint>()"), "the set is a field");

			var wiring = RepoFile(Bots + "SquadManagerBotModuleCA.TeamLead.cs");
			Assert.That(wiring, Does.Contain("if (order.AllIn && order.Pushed && !TeamLeadAllInRejoinGroup())\n\t\t\t\treturn;"), "a pushed all-in order takes only a WAVE-MIN re-join group");
			Assert.That(v07, Does.Contain("AllInCA.RejoinAllowed(true, units, value, WaveMinCA.Limits(minUnits, minValue), minUnits, minValue)"));
			Assert.That(wiring.IndexOf("TeamLeadAllInRejoinGroup()", StringComparison.Ordinal), Is.LessThan(wiring.IndexOf("CreateAttackForce(bot);\n\t\t\tif (order.AllIn", StringComparison.Ordinal)),
				"the gate runs before the squad is formed");
		}
	}
}
