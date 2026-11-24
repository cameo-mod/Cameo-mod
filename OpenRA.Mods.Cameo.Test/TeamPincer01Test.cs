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
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	/// <summary>
	/// TEAM-PINCER-01 (tl_pincer; USER P0, 2026-11-24): when the leader publishes a team push and the combined blob holds at least twice the
	/// WAVE-MIN tier, the push splits into two prongs with the SAME target. Each prong stages on its own side of the target; the prongs hold
	/// until both are ready (the assault fan's sync hold, at most 750 ticks), then push together. A blob that is too small, or a prong that is
	/// too light, is the single blob as before. The wave/pincer design is ported in intent from Crystallized Nexus (CNWaveStates and the pincer
	/// wave plan of its squad manager).
	/// </summary>
	[TestFixture]
	public class TeamPincer01Test
	{
		const string Bots = "OpenRA.Mods.CA/Traits/BotModules/";
		const int Cell = TeamLeadCA.CellUnits;

		static string RepoFile(string relative)
		{
			var dir = new DirectoryInfo(AppContext.BaseDirectory);
			while (dir != null && !File.Exists(Path.Combine(dir.FullName, "mod.config")))
				dir = dir.Parent;

			Assert.That(dir, Is.Not.Null, "repository root (mod.config) not found above " + AppContext.BaseDirectory);
			return File.ReadAllText(Path.Combine(dir.FullName, relative));
		}

		static WPos At(int cellX, int cellY) => new(cellX * Cell, cellY * Cell, 0);

		static double Cells(WVec v) => Math.Sqrt((double)v.HorizontalLengthSquared) / Cell;

		// ---- the split: only a blob that can field two WAVE-MIN prongs ----

		[Test]
		public void OnlyABlobOfTwiceTheWaveMinTierSplitsAnythingSmallerStaysOneBlob()
		{
			// Tier 8 units / 2000 value: a prong must still pass it, so the blob needs 16 / 4000.
			Assert.That(TeamLeadCA.PincerBlob(16, 4000, 3, true, 8, 2000), Is.True);
			Assert.That(TeamLeadCA.PincerBlob(15, 4000, 3, true, 8, 2000), Is.False, "one unit short of twice the unit bar");
			Assert.That(TeamLeadCA.PincerBlob(16, 3999, 3, true, 8, 2000), Is.False, "one short of twice the value bar");
			Assert.That(TeamLeadCA.PincerBlob(8, 2000, 3, true, 8, 2000), Is.False, "exactly the single-blob tier is not a pincer");

			// No tier (WAVE-MIN off or a Noob tier): twice the join floor is the bar.
			Assert.That(TeamLeadCA.PincerBlob(6, 0, 3, false, 0, 0), Is.True);
			Assert.That(TeamLeadCA.PincerBlob(5, 0, 3, false, 0, 0), Is.False);
			Assert.That(TeamLeadCA.PincerBlob(0, 0, 0, false, 0, 0), Is.False, "never an empty blob");

			// A tier below the join floor: the floor still decides.
			Assert.That(TeamLeadCA.PincerBlob(5, 900, 3, true, 2, 400), Is.False, "5 units cannot make two prongs of 3");
			Assert.That(TeamLeadCA.PincerBlob(6, 900, 3, true, 2, 400), Is.True);
		}

		[Test]
		public void ATargetThatIsTooCloseOrTwoStagingPointsThatAreTooCloseAreNotAPincer()
		{
			Assert.That(TeamLeadCA.PincerTargetFar(At(10, 50), At(40, 50), 30), Is.True, "30 cells away is far enough");
			Assert.That(TeamLeadCA.PincerTargetFar(At(10, 50), At(39, 50), 30), Is.False);
			Assert.That(TeamLeadCA.PincerSeparated(At(10, 10), At(10, 26), 16), Is.True);
			Assert.That(TeamLeadCA.PincerSeparated(At(10, 10), At(10, 25), 16), Is.False);
		}

		[Test]
		public void TheSquadsAreSharedOutLightestProngFirstAndPlacedOnce()
		{
			Assert.That(TeamLeadCA.PickProng(0, 0, 0, 0), Is.EqualTo(0));
			Assert.That(TeamLeadCA.PickProng(100, 0, 1, 0), Is.EqualTo(1));
			Assert.That(TeamLeadCA.PickProng(100, 100, 1, 1), Is.EqualTo(0), "a full tie goes to prong 0");
			Assert.That(TeamLeadCA.PickProng(100, 100, 2, 1), Is.EqualTo(1), "equal value: the prong with fewer squads");

			var order = new TeamLeadOrder(1, "a", At(20, 50), At(80, 50), 7, 1000);
			order.SetProngs(At(60, 20), At(60, 80));
			var s1 = new object();
			var s2 = new object();
			var s3 = new object();
			var s4 = new object();
			Assert.That(order.ProngOf(s1, 100), Is.EqualTo(0));
			Assert.That(order.ProngOf(s2, 60), Is.EqualTo(1), "the empty prong is the lightest");
			Assert.That(order.ProngOf(s3, 50), Is.EqualTo(1), "60 is still lighter than 100");
			Assert.That(order.ProngOf(s4, 40), Is.EqualTo(0), "100 against 110 - the lighter one again");
			Assert.That(order.ProngOf(s1, 999), Is.EqualTo(0), "a squad is placed once: asking again changes nothing");
			Assert.That(order.ProngOrNone(new object()), Is.EqualTo(-1));

			order.Report(s1, 100, true);
			order.Report(s2, 60, false);
			order.Report(s3, 50, true);
			order.Report(s4, 40, false);
			Assert.That(order.Totals(0), Is.EqualTo((140L, 100L)));
			Assert.That(order.Totals(1), Is.EqualTo((110L, 50L)));
			Assert.That(order.Totals(), Is.EqualTo((250L, 150L)), "the order's own totals still sum every prong");
		}

		[Test]
		public void AVanishedSquadLeavesItsProngSoTheNextOneFillsTheGap()
		{
			var order = new TeamLeadOrder(1, "a", At(20, 50), At(80, 50), 7, 1000);
			order.SetProngs(At(60, 20), At(60, 80));
			var a = new object();
			var b = new object();
			Assert.That(order.ProngOf(a, 100), Is.EqualTo(0));
			Assert.That(order.ProngOf(b, 100), Is.EqualTo(1));

			order.Prune(o => o == b);
			Assert.That(order.ProngOrNone(a), Is.EqualTo(-1));
			Assert.That(order.ProngOf(new object(), 100), Is.EqualTo(0), "prong 0 is empty again");
			Assert.That(order.ProngOrNone(b), Is.EqualTo(1));
		}

		// ---- different approaches: one staging point per side of the target ----

		[Test]
		public void TheTwoStagingPointsSitOnOppositeSidesOfTheTargetAtTheStandOff()
		{
			var home = At(10, 50);
			var target = At(80, 50);
			var a = TeamLeadCA.PincerStaging(home, target, -1, 24, 70);
			var b = TeamLeadCA.PincerStaging(home, target, 1, 24, 70);
			Assert.That(a, Has.Count.EqualTo(6));
			Assert.That(b, Has.Count.EqualTo(a.Count));

			// The preferred point of each side: 24 cells from the target, on the home side of it, 70 degrees off the line.
			Assert.That(Cells(a[0] - target), Is.EqualTo(24).Within(0.5));
			Assert.That(Cells(b[0] - target), Is.EqualTo(24).Within(0.5));
			Assert.That(a[0].X, Is.LessThan(target.X), "on the home side");
			Assert.That(b[0].X, Is.LessThan(target.X), "on the home side");
			Assert.That(Math.Sign(a[0].Y - target.Y) * Math.Sign(b[0].Y - target.Y), Is.EqualTo(-1), "one prong each side of the line");

			// 2 x 24 x sin(70) = about 45 cells apart: two different approaches, not one.
			Assert.That(Cells(a[0] - b[0]), Is.GreaterThan(40));
			Assert.That(TeamLeadCA.PincerSeparated(a[0], b[0], 16), Is.True);

			// The fall-backs swing closer to the line and pull in, so the terrain gets a choice; none sits on the other side.
			foreach (var c in a)
				Assert.That(Math.Sign(c.Y - target.Y), Is.EqualTo(Math.Sign(a[0].Y - target.Y)));
			foreach (var c in b)
				Assert.That(Math.Sign(c.Y - target.Y), Is.EqualTo(Math.Sign(b[0].Y - target.Y)));
			Assert.That(Cells(a[3] - target), Is.EqualTo(20).Within(0.5), "the pulled-in variants are 4 cells nearer");
		}

		[Test]
		public void TheStagingPointsFollowTheDirectionOfTheTargetInAnyDirection()
		{
			// Target north of home: the points are south of the target, one west and one east of the line.
			var home = At(50, 90);
			var target = At(50, 20);
			var a = TeamLeadCA.PincerStaging(home, target, -1, 24, 70)[0];
			var b = TeamLeadCA.PincerStaging(home, target, 1, 24, 70)[0];
			Assert.That(a.Y, Is.GreaterThan(target.Y));
			Assert.That(b.Y, Is.GreaterThan(target.Y));
			Assert.That(Math.Sign(a.X - target.X) * Math.Sign(b.X - target.X), Is.EqualTo(-1));
			Assert.That(Cells(a - target), Is.EqualTo(24).Within(0.5));
			Assert.That(TeamLeadCA.PincerStaging(target, target, 1, 24, 70), Is.Empty, "no direction, no points");
		}

		[Test]
		public void TheSpreadAndTheStandOffAreClampedToSaneValues()
		{
			var home = At(10, 50);
			var target = At(80, 50);
			var wide = TeamLeadCA.PincerStaging(home, target, 1, 24, 180)[0];
			var narrow = TeamLeadCA.PincerStaging(home, target, 1, 24, 5)[0];
			Assert.That(Cells(wide - narrow), Is.GreaterThan(5), "180 degrees is cut to 90, 5 to 45");
			Assert.That(wide.X, Is.LessThanOrEqualTo(target.X + Cell), "never behind the target");
			Assert.That(Cells(TeamLeadCA.PincerStaging(home, target, 1, 1, 70)[0] - target), Is.EqualTo(4).Within(0.5), "a tiny stand-off is raised to 4 cells");
		}

		// ---- the timed simultaneous arrival: the sync hold ----

		[Test]
		public void AProngIsReadyOnlyWithSquadsAndEnoughOfThemAtItsOwnStagingPoint()
		{
			Assert.That(TeamLeadCA.ProngReady(0, 0, 70), Is.False, "no squads, no prong");
			Assert.That(TeamLeadCA.ProngReady(100, 69, 70), Is.False);
			Assert.That(TeamLeadCA.ProngReady(100, 70, 70), Is.True);
			Assert.That(TeamLeadCA.ProngReady(100, 0, 0), Is.True, "a zero bar is always met once there are squads");
		}

		[Test]
		public void BothProngsPushTogetherWhenBothAreReadyNotBeforeTheGatherMinimum()
		{
			Assert.That(TeamLeadCA.PincerPushDue(1299, 1000, 300, 2250, 750, true, true, 1100), Is.False, "before the gather minimum");
			Assert.That(TeamLeadCA.PincerPushDue(1300, 1000, 300, 2250, 750, true, true, 1100), Is.True, "both ready at the minimum");
			Assert.That(TeamLeadCA.PincerPushDue(1300, 1000, 300, 2250, 750, false, false, -1), Is.False, "nobody there yet");
		}

		[Test]
		public void TheFirstReadyProngHoldsForTheOtherAtMost750TicksThenBothPush()
		{
			Assert.That(TeamLeadCA.PincerPushDue(1849, 1000, 300, 2250, 750, true, false, 1100), Is.False, "749 ticks after the first prong was ready");
			Assert.That(TeamLeadCA.PincerPushDue(1850, 1000, 300, 2250, 750, true, false, 1100), Is.True, "750 ticks: the hold is over");
			Assert.That(TeamLeadCA.PincerPushDue(1850, 1000, 300, 2250, 750, false, true, 1100), Is.True, "whichever prong was first");
			Assert.That(TeamLeadCA.PincerPushDue(1850, 1000, 300, 2250, 5000, true, false, 1100), Is.True, "a longer configured hold is capped at 750");
			Assert.That(TeamLeadCA.PincerHoldCapTicks, Is.EqualTo(750));
			Assert.That(TeamLeadCA.PincerPushDue(3249, 1000, 300, 2250, 750, false, false, -1), Is.False);
			Assert.That(TeamLeadCA.PincerPushDue(3250, 1000, 300, 2250, 750, false, false, -1), Is.True, "the gather window is the outer backstop");
		}

		[Test]
		public void TheOrderRemembersTheFirstReadyTickAndTheHoldRunsFromIt()
		{
			var order = new TeamLeadOrder(1, "a", At(20, 50), At(80, 50), 7, 1000);
			order.SetProngs(At(60, 20), At(60, 80));
			var left = new object();
			var right = new object();
			order.ProngOf(left, 100);
			order.ProngOf(right, 100);

			// Tick 1400: only the left prong stands at its point.
			order.Report(left, 100, true);
			order.Report(right, 100, false);
			var (c0, a0) = order.Totals(0);
			var (c1, a1) = order.Totals(1);
			var ready0 = TeamLeadCA.ProngReady(c0, a0, 70);
			var ready1 = TeamLeadCA.ProngReady(c1, a1, 70);
			Assert.That((ready0, ready1), Is.EqualTo((true, false)));
			order.NoteReady(1400);
			Assert.That(TeamLeadCA.PincerPushDue(1500, order.IssueTick, 300, 2250, 750, ready0, ready1, order.FirstReadyTick), Is.False);

			// Tick 1500 still only the left prong: the first-ready tick does not move.
			order.NoteReady(1500);
			Assert.That(order.FirstReadyTick, Is.EqualTo(1400));
			Assert.That(TeamLeadCA.PincerPushDue(2149, order.IssueTick, 300, 2250, 750, ready0, ready1, order.FirstReadyTick), Is.False);
			Assert.That(TeamLeadCA.PincerPushDue(2150, order.IssueTick, 300, 2250, 750, ready0, ready1, order.FirstReadyTick), Is.True, "1400 + 750");

			// Had the right prong arrived at 1600, the push goes at once on both being ready.
			order.Report(right, 100, true);
			ready1 = TeamLeadCA.ProngReady(order.Totals(1).Committed, order.Totals(1).Arrived, 70);
			Assert.That(TeamLeadCA.PincerPushDue(1600, order.IssueTick, 300, 2250, 750, ready0, ready1, order.FirstReadyTick), Is.True);
		}

		// ---- fall back to the single blob ----

		[Test]
		public void TheLighterProngNeedsItsShareOfTheCommittedValueElseThePincerCollapsesIntoOneBlob()
		{
			Assert.That(TeamLeadCA.PincerBalanced(0, 0, 25), Is.True, "nothing committed yet: undecided");
			Assert.That(TeamLeadCA.PincerBalanced(100, 100, 25), Is.True);
			Assert.That(TeamLeadCA.PincerBalanced(100, 0, 25), Is.False, "one prong has nothing: a single squad cannot be a pincer");
			Assert.That(TeamLeadCA.PincerBalanced(100, 20, 25), Is.False, "20 of 120 is 16%");
			Assert.That(TeamLeadCA.PincerBalanced(100, 34, 25), Is.True, "34 of 134 is 25.4%");
			Assert.That(TeamLeadCA.PincerBalanced(100, 100, 90), Is.True, "the bar is capped at an even split");
		}

		[Test]
		public void ACollapsedOrderIsASingleBlobAgainAndAnOrderWithoutProngsAlwaysWas()
		{
			var plain = new TeamLeadOrder(1, "a", At(20, 50), At(80, 50), 7, 1000);
			Assert.That(plain.HasProngs, Is.False);
			Assert.That(plain.Pincer, Is.False);

			var order = new TeamLeadOrder(2, "a", At(20, 50), At(80, 50), 7, 1000);
			order.SetProngs(At(60, 20), At(60, 80));
			Assert.That(order.HasProngs, Is.True);
			Assert.That(order.Pincer, Is.True);
			Assert.That(order.ProngRally(0), Is.EqualTo(At(60, 20)));
			Assert.That(order.ProngRally(1), Is.EqualTo(At(60, 80)));

			order.PincerCollapsed = true;
			Assert.That(order.HasProngs, Is.True, "the squads keep the prong they were sent to");
			Assert.That(order.Pincer, Is.False, "but the push follows the single-blob rules");
			Assert.That(TeamLeadCA.PushDue(2500, 1000, 300, 1500, 100, 10, 70), Is.True, "the plain gather window applies again");
		}

		[Test]
		public void BothProngsPushTheSameTargetAndNeitherChangesIt()
		{
			// One order carries one target: the prongs only differ in where they stage.
			var order = new TeamLeadOrder(3, "a", At(20, 50), At(80, 50), 7, 1000);
			order.SetProngs(At(60, 20), At(60, 80));
			Assert.That(order.Target, Is.EqualTo(At(80, 50)));
			Assert.That(order.TargetActorId, Is.EqualTo(7u));
		}

		// ---- the wiring ----

		[Test]
		public void TheSwitchDefaultsOffIsOnInSixPersonalitiesNotClassicAndHasItsOwnRow()
		{
			var info = RepoFile(Bots + "SquadManagerBotModuleCAInfo.TeamLead.cs");
			Assert.That(info, Does.Contain("public readonly bool TeamLeadPincerEnabled = false;"));
			Assert.That(info, Does.Contain("public readonly int TeamLeadPincerHoldTicks = 750;"));
			Assert.That(info, Does.Contain("public readonly int TeamLeadPincerGatherMaxTicks = 2250;"));

			var yaml = RepoFile("mods/cameo/ai/ai.yaml");
			var classic = yaml.IndexOf("SquadManagerBotModuleCA@classic:", StringComparison.Ordinal);
			Assert.That(classic, Is.GreaterThan(0));
			Assert.That(Regex.Matches(yaml[..classic], @"\n\t\tTeamLeadPincerEnabled: true").Count, Is.EqualTo(6));
			Assert.That(yaml[classic..], Does.Not.Contain("TeamLeadPincerEnabled"), "@classic keeps the default (off), byte-identical");

			Assert.That(AblationIds.TeamLeadPincer, Is.EqualTo("tl_pincer"));
			Assert.That(new[] { AblationIds.TeamLead, AblationIds.TeamLeadJointHarass, AblationIds.TeamLeadCadence, AblationIds.TeamLeadFinishWeakest, AblationIds.TeamLeadPincer }.Distinct().Count(), Is.EqualTo(5));
		}

		[Test]
		public void TheLeaderPlansThePincerOncePerOrderAndTheMembersOnlyFollowIt()
		{
			var wiring = RepoFile(Bots + "SquadManagerBotModuleCA.TeamLead.cs");
			Assert.That(wiring, Does.Contain("internal bool TeamLeadPincerOn => TeamLeadOn && Info.TeamLeadPincerEnabled && !ablatePincer.Off(Player, AblationIds.TeamLeadPincer);"));

			// One caller (the issue), one definition: no per-tick or per-squad planning.
			Assert.That(Regex.Matches(wiring, @"TeamLeadPincerPlan\(").Count, Is.EqualTo(2));
			Assert.That(wiring, Does.Contain("TeamLeadPincerPlan(order, from, target, now);"));

			var start = wiring.IndexOf("void TeamLeadPincerPlan(", StringComparison.Ordinal);
			Assert.That(start, Is.GreaterThan(0));
			var body = wiring[start..wiring.IndexOf("Actor TeamLeadPincerSample()", start, StringComparison.Ordinal)];
			Assert.That(body, Does.Contain("if (!TeamLeadPincerOn)"));
			Assert.That(body, Does.Contain("TeamLeadCA.PincerBlob("), "the blob must hold twice the WAVE-MIN tier");
			Assert.That(body, Does.Contain("TeamLeadCA.PincerTargetFar("));
			Assert.That(body, Does.Contain("TeamLeadCA.PincerSeparated("), "two staging points that are not two approaches are one blob");
			Assert.That(body, Does.Contain("PathExistsForLocomotor"), "a staging point must be reachable");
			Assert.That(body, Does.Contain("Info.AttackScanRadius + 4"), "outside the scan radius of a gathering squad");
			Assert.That(body, Does.Contain("order.SetProngs("));

			// Fog-honest and cheap: terrain and the pathfinder only - no enemy actor is read, nothing is scanned.
			foreach (var banned in new[] { "FindActors", "FindClosestEnemy", "FrozenActorLayer", "World.Actors", "CanBeViewedByPlayer", "ActorsWithTrait" })
				Assert.That(body, Does.Not.Contain(banned), "the pincer plan must not read " + banned);
		}

		[Test]
		public void TheGatherStateSendsEachSquadToItsProngAndTheReportMeasuresArrivalThere()
		{
			var wiring = RepoFile(Bots + "SquadManagerBotModuleCA.TeamLead.cs");
			var report = wiring[wiring.IndexOf("internal void TeamLeadReport(", StringComparison.Ordinal)..];
			report = report[..report.IndexOf("internal void TeamLeadAbandon(", StringComparison.Ordinal)];
			Assert.That(report, Does.Contain("var rally = TeamLeadRallyOf(squad, order);"));
			Assert.That(report, Does.Not.Contain("order.Rally"), "arrival is measured at the squad's own prong, not at the plain rally");

			var gather = RepoFile(Bots + "Squads/States/TeamLeadGatherStateCA.cs");
			Assert.That(gather, Does.Contain("manager.TeamLeadRallyOf(owner, order)"));
			Assert.That(gather, Does.Contain("order.Pincer"), "the route is only for a pincer");
			Assert.That(gather, Does.Contain("manager.RouteAroundThreat(owner.Units[0].Actor, cell)"), "the existing flank-routing router, when one answers");
			Assert.That(gather, Does.Not.Contain("\"AttackMove\""), "a gathering prong marches with Move, it fights on contact like any gather squad");
		}

		[Test]
		public void TheOrderUpkeepPushesAPincerOnTheSyncRulesAndCollapsesALightProng()
		{
			var wiring = RepoFile(Bots + "SquadManagerBotModuleCA.TeamLead.cs");
			var start = wiring.IndexOf("void TeamLeadMaintainOrder(", StringComparison.Ordinal);
			var body = wiring[start..wiring.IndexOf("TeamLeadOrder TeamLeadOpenOrder()", start, StringComparison.Ordinal)];
			Assert.That(body, Does.Contain("if (order.Pincer)"));
			Assert.That(body, Does.Contain("TeamLeadCA.PincerBalanced("));
			Assert.That(body, Does.Contain("order.PincerCollapsed = true;"));
			Assert.That(body, Does.Contain("TeamLeadCA.ProngReady("));
			Assert.That(body, Does.Contain("order.NoteReady(now);"));
			Assert.That(body, Does.Contain("TeamLeadCA.PincerPushDue("));
			Assert.That(body, Does.Contain("Info.TeamLeadPincerHoldTicks"));

			// The single-blob rule stays below, untouched, for every order that is not a live pincer.
			Assert.That(body.IndexOf("TeamLeadCA.PushDue(", StringComparison.Ordinal), Is.GreaterThan(body.IndexOf("TeamLeadCA.PincerPushDue(", StringComparison.Ordinal)));

			// It still runs once per team check window (>= TeamLeadCheckTicks), not per tick.
			Assert.That(wiring, Does.Contain("TeamLeadMaintainOrder(team, now);"));
			Assert.That(wiring, Does.Contain("if (now >= team.NextEvalTick)"));
		}

		[Test]
		public void TheJointHarassGroupAndTheTargetPickAreUntouched()
		{
			var wiring = RepoFile(Bots + "SquadManagerBotModuleCA.TeamLead.cs");
			var start = wiring.IndexOf("void TeamLeadShareHarassTarget(", StringComparison.Ordinal);
			var harass = wiring[start..wiring.IndexOf("TeamLeadOrder TeamLeadTryIssue(", start, StringComparison.Ordinal)];
			Assert.That(harass, Does.Not.Contain("Pincer"), "the joint harass group does not split");
			Assert.That(harass, Does.Not.Contain("Prong"));

			// The target is chosen before the plan, once, and the order carries that one target for both prongs.
			Assert.That(wiring, Does.Contain("var order = teamLeadTeam.Issue(teamLeadKey, rally, target, targetActorId, now);"));
		}
	}
}
