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
using System.Linq;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.CA.Traits.BotModules.Squads;

namespace OpenRA.Mods.Cameo.Test
{
	/// <summary>
	/// CONSOLIDATE-02 Q7 (CONSOLIDATE_02_SQUAD_DESIGN.md 4.1 Step 2; row sq_pincer_push under sq_pipeline_02): the pincer is decided at PUSH time on the blob that arrived, and the
	/// two arms are shared out so that both hold the arm bar: whole squads first, then one squad divided (X10: three seats, one squad each, a 2:1 share used to collapse the
	/// pincer or leave it unplanned). The arm plan is pure; the order and the wiring are pinned.
	/// </summary>
	[TestFixture]
	public class Squad02Q7Test
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

		static string Between(string text, string from, string to)
		{
			var start = text.IndexOf(from, StringComparison.Ordinal);
			Assert.That(start, Is.GreaterThanOrEqualTo(0), from);
			var end = text.IndexOf(to, start, StringComparison.Ordinal);
			Assert.That(end, Is.GreaterThan(start), to);
			return text[start..end];
		}

		static ArmSquad Squad(uint key, int units, long value) => new(key, units, value);

		static IEnumerable<List<T>> Permutations<T>(List<T> items)
		{
			if (items.Count <= 1)
			{
				yield return new List<T>(items);
				yield break;
			}

			for (var i = 0; i < items.Count; i++)
			{
				var rest = new List<T>(items);
				rest.RemoveAt(i);
				foreach (var tail in Permutations(rest))
				{
					tail.Insert(0, items[i]);
					yield return tail;
				}
			}
		}

		static int ArmOf(ArmPlan plan, uint key) => plan.Places.Single(p => p.Key == key).Arm;

		[Test]
		public void WholeSquadsThatShareOutEvenlyAreNeverDivided()
		{
			// Four squads of 12 units / 1200: two per arm, both arms hold the 12 / 1200 bar.
			var plan = PincerArms.Balance(new[] { Squad(1, 12, 1200), Squad(2, 12, 1200), Squad(3, 12, 1200), Squad(4, 12, 1200) }, 12, 1200);
			Assert.That(plan, Is.Not.Null);
			Assert.That(plan.Split, Is.Null, "nothing is divided when whole squads already hold both arm bars");
			Assert.That((plan.Units0, plan.Value0, plan.Units1, plan.Value1), Is.EqualTo((24, 2400L, 24, 2400L)));
			Assert.That(plan.Places.Select(p => p.Key), Is.EqualTo(new uint[] { 1, 2, 3, 4 }), "the places are listed in key order");

			// Two squads of 12: one arm each.
			var pair = PincerArms.Balance(new[] { Squad(7, 12, 1200), Squad(9, 12, 1200) }, 12, 1200);
			Assert.That(pair.Split, Is.Null);
			Assert.That(ArmOf(pair, 7), Is.Not.EqualTo(ArmOf(pair, 9)));
		}

		[Test]
		public void ThreeSeatsWithOneSquadEachAreBalancedByDividingOneSquadInsteadOfCollapsing()
		{
			// X10: three seats, 10 units / 1000 each; whole squads give 20 / 10, the 10 is below the 12 / 1200 arm bar. One squad is divided: 5 units go to the light arm.
			var plan = PincerArms.Balance(new[] { Squad(1, 10, 1000), Squad(2, 10, 1000), Squad(3, 10, 1000) }, 12, 1200);
			Assert.That(plan, Is.Not.Null, "30 units / 3000 hold two arm bars: the pincer is possible");
			Assert.That(plan.Split, Is.Not.Null);
			Assert.That(plan.Split.Value.Key, Is.EqualTo(1u), "the biggest squad of the heavy arm, the lowest key among equals");
			Assert.That(plan.Split.Value.Units, Is.EqualTo(5), "the most even share that holds both bars");
			Assert.That(plan.Split.Value.ToArm, Is.EqualTo(1 - ArmOf(plan, 1)), "the divided units go to the other arm");
			Assert.That((plan.Units0, plan.Value0, plan.Units1, plan.Value1), Is.EqualTo((15, 1500L, 15, 1500L)));
			Assert.That(PincerArms.Holds(plan.Units0, plan.Value0, 12, 1200) && PincerArms.Holds(plan.Units1, plan.Value1, 12, 1200), Is.True);
		}

		[Test]
		public void OneBigSquadCanFieldBothArmsWhenItHoldsTwoBars()
		{
			var plan = PincerArms.Balance(new[] { Squad(5, 30, 3000) }, 12, 1200);
			Assert.That(plan, Is.Not.Null, "a solo seat's one squad is divided in two");
			Assert.That(plan.Split.Value, Is.EqualTo(new ArmSplit(5, 15, 1)));
			Assert.That((plan.Units0, plan.Units1), Is.EqualTo((15, 15)));

			Assert.That(PincerArms.Balance(new[] { Squad(5, 23, 2300) }, 12, 1200), Is.Null, "23 units cannot give two arms of 12");
			Assert.That(PincerArms.Balance(new[] { Squad(5, 30, 2399) }, 12, 1200), Is.Null, "value below twice the arm bar: no pincer");
		}

		[Test]
		public void ABlobBelowTwoArmBarsGetsNoPlanAndOneUnitSquadsAreNeverDivided()
		{
			Assert.That(PincerArms.Balance(new[] { Squad(1, 10, 1000), Squad(2, 10, 1000) }, 12, 1200), Is.Null, "20 units cannot give two arms of 12");
			Assert.That(PincerArms.Balance(Array.Empty<ArmSquad>(), 12, 1200), Is.Null);
			Assert.That(PincerArms.Balance(new[] { Squad(1, 0, 0), Squad(2, 0, 0) }, 1, 0), Is.Null, "a squad without units takes no part");

			// 24 squads of one unit: shared out whole, 12 per arm; no squad of one unit is ever divided.
			var singles = Enumerable.Range(1, 24).Select(i => Squad((uint)i, 1, 100)).ToArray();
			var plan = PincerArms.Balance(singles, 12, 1200);
			Assert.That(plan, Is.Not.Null);
			Assert.That(plan.Split, Is.Null);
			Assert.That((plan.Units0, plan.Units1), Is.EqualTo((12, 12)));

			// 23 single units and a bigger squad of 1: nothing to divide and a short arm - no plan.
			Assert.That(PincerArms.Balance(singles.Take(23).ToArray(), 12, 1200), Is.Null);
		}

		[Test]
		public void AnArmOfValueButFewUnitsIsDividedTheOtherWay()
		{
			// One heavy-value tank squad (6 units, 3000) and one big cheap infantry squad (20 units, 2000): value says the tanks are the heavy arm, units say the infantry is. The tank
			// arm is short of units (6 of 8), so the infantry squad gives it two.
			var plan = PincerArms.Balance(new[] { Squad(1, 6, 3000), Squad(2, 20, 2000) }, 8, 1000);
			Assert.That(plan, Is.Not.Null);
			Assert.That(PincerArms.Holds(plan.Units0, plan.Value0, 8, 1000) && PincerArms.Holds(plan.Units1, plan.Value1, 8, 1000), Is.True);
			Assert.That(plan.Split.Value, Is.EqualTo(new ArmSplit(2, 2, ArmOf(plan, 1))), "the squad with the spare units is the one divided; its units join the tank arm");
			Assert.That((plan.Units0 + plan.Units1, plan.Value0 + plan.Value1), Is.EqualTo((26, 5000L)), "nothing is lost in the division");
		}

		[Test]
		public void TheSameBlobGivesTheSameArmsInEveryInputOrder()
		{
			var shared = new List<ArmSquad> { Squad(11, 10, 1000), Squad(12, 10, 1000), Squad(13, 7, 900), Squad(14, 9, 1100) };
			var divided = new List<ArmSquad> { Squad(21, 10, 1000), Squad(22, 10, 1000), Squad(23, 10, 1000), Squad(24, 1, 100) };
			Assert.That(PincerArms.Balance(shared, 12, 1200).Split, Is.Null, "this blob shares out whole");
			Assert.That(PincerArms.Balance(divided, 12, 1200).Split, Is.Not.Null, "this one needs a division");

			foreach (var blob in new[] { shared, divided })
			{
				var expected = PincerArms.Balance(blob, 12, 1200);
				foreach (var order in Permutations(blob))
				{
					var plan = PincerArms.Balance(order, 12, 1200);
					Assert.That(plan.Places, Is.EqualTo(expected.Places));
					Assert.That(plan.Split, Is.EqualTo(expected.Split));
					Assert.That((plan.Units0, plan.Value0, plan.Units1, plan.Value1), Is.EqualTo((expected.Units0, expected.Value0, expected.Units1, expected.Value1)));
				}
			}
		}

		[Test]
		public void AnArmNeverFallsBelowTheJoinFloorWhateverTheWaveMinBar()
		{
			Assert.That(PincerArms.FloorUnits(8, 3), Is.EqualTo(8));
			Assert.That(PincerArms.FloorUnits(2, 3), Is.EqualTo(3));
			Assert.That(PincerArms.FloorUnits(0, 0), Is.EqualTo(1));
			Assert.That(PincerArms.FloorUnits(-5, 3), Is.EqualTo(3));

			// No WAVE-MIN bar (0 / 0): the floor alone keeps an arm from being empty.
			var plan = PincerArms.Balance(new[] { Squad(1, 6, 600) }, PincerArms.FloorUnits(0, 3), 0);
			Assert.That(plan, Is.Not.Null);
			Assert.That((plan.Units0, plan.Units1), Is.EqualTo((3, 3)));
			Assert.That(PincerArms.Balance(new[] { Squad(1, 5, 500) }, PincerArms.FloorUnits(0, 3), 0), Is.Null, "5 units cannot give two arms of the join floor 3");
		}

		[Test]
		public void TheOrderBecomesAPincerInPlaceAndRestartsItsGatherWindow()
		{
			var order = new TeamLeadOrder(4, "leader", new WPos(1000, 1000, 0), new WPos(40000, 1000, 0), 77, 100);
			var a = new object();
			var b = new object();
			order.Report(a, 1000, true, 10, 10);
			order.Report(b, 1000, true, 10, 10);
			order.HoldPush(900);
			Assert.That(order.Extending, Is.True);
			Assert.That(order.Pincer, Is.False);
			Assert.That(order.Reported().Select(r => (r.Units, r.Value)), Is.EqualTo(new[] { (10, 1000L), (10, 1000L) }));

			order.BeginPincer(1500, new WPos(20000, -9000, 0), new WPos(20000, 11000, 0));
			Assert.That(order.Pincer, Is.True);
			Assert.That(order.HasProngs, Is.True);
			Assert.That(order.IssueTick, Is.EqualTo(1500), "the pincer's gather window runs from now (the push-bar hold the blob passed is over)");
			Assert.That(order.Extending, Is.False);
			Assert.That(order.FirstReadyTick, Is.EqualTo(-1));
			Assert.That(order.Pushed, Is.False);

			order.AssignProng(a, 0, 1000);
			order.AssignProng(b, 1, 1000);
			Assert.That(order.ProngOrNone(a), Is.EqualTo(0));
			Assert.That(order.ProngOrNone(b), Is.EqualTo(1));
			Assert.That(order.Totals(0), Is.EqualTo((1000L, 1000L)));
			Assert.That(order.ProngUnits(1), Is.EqualTo(10));
			Assert.That(order.ProngOf(b, 5), Is.EqualTo(1), "the plan's assignment is never re-shared by the first ask");
		}

		[Test]
		public void ADivisionIsTakenOnceByTheSquadThatOwnsItAndForgottenWithTheSquad()
		{
			var order = new TeamLeadOrder(1, "leader", new WPos(0, 0, 0), new WPos(1000, 0, 0), 0, 0);
			var squad = new object();
			var other = new object();
			order.RequestSplit(squad, 5, 1);
			Assert.That(order.TakeSplit(other, out _, out _), Is.False, "another squad has nothing to give");
			Assert.That(order.TakeSplit(squad, out var units, out var toProng), Is.True);
			Assert.That((units, toProng), Is.EqualTo((5, 1)));
			Assert.That(order.TakeSplit(squad, out _, out _), Is.False, "executed once");

			order.RequestSplit(squad, 3, 0);
			order.Forget(squad);
			Assert.That(order.TakeSplit(squad, out _, out _), Is.False, "a squad that left the order gives nothing");
		}

		[Test]
		public void TheRowIsRegisteredAndWiredBehindThePincerSwitchAndTheMasterRow()
		{
			Assert.That(AblationIds.SqPincerPush, Is.EqualTo("sq_pincer_push"));
			var wiring = RepoFile(Bots + "SquadManagerBotModuleCA.PincerPush.cs");
			Assert.That(wiring, Does.Contain("AblationLatch ablateSqPincerPush;"));
			Assert.That(wiring, Does.Contain("internal bool ArmyPincerPushOn => ArmyPipelineOn && TeamLeadPincerOn && !ablateSqPincerPush.Off(Player, AblationIds.SqPincerPush);"));

			var pure = RepoFile(Bots + "Squads/PincerArms.cs");
			Assert.That(pure, Does.Not.Contain("Random"), "no RNG: the same blob gives the same arms");
			Assert.That(pure, Does.Not.Contain("World"));
			Assert.That(pure, Does.Not.Contain("Actor "), "no actor is read; the caller offers numbers");
		}

		[Test]
		public void ThePincerIsDecidedAtPushTimeOnTheArrivedBlobWithTheOldGatesAndFogHonestStaging()
		{
			var wiring = RepoFile(Bots + "SquadManagerBotModuleCA.PincerPush.cs");
			var decide = Between(wiring, "bool TeamLeadPincerAtPush(", "// The first legal PAIR");

			var order = new[]
			{
				"if (order.HasProngs || order.GatherOnly || order.Cancelled)",
				"var (units, value) = order.ArrivedTotals();",
				"PincerWideCA.PincerBlob(units, value, Info.TeamLeadJoinMinUnits, WaveMinCA.Limits(minUnits, minValue), minUnits, minValue, armPercent)",
				"TeamLeadCA.PincerTargetFar(order.Rally, order.Target, Info.TeamLeadPincerMinTargetCells)",
				"var plan = PincerArms.Balance(squads, floorUnits, PincerWideCA.ArmBar(minValue, armPercent));",
				"TeamLeadPincerStagingAtPush(order, out var rally0, out var rally1)",
				"order.BeginPincer(now, rally0, rally1);",
				"order.AssignProng(byKey[place.Key].Squad, place.Arm, byKey[place.Key].Value);",
				"order.RequestSplit(byKey[split.Key].Squad, split.Units, split.ToArm);"
			};

			var last = -1;
			foreach (var line in order)
			{
				var at = decide.IndexOf(line, StringComparison.Ordinal);
				Assert.That(at, Is.GreaterThan(last), line);
				last = at;
			}

			Assert.That(decide, Does.Contain("PincerArms.FloorUnits(PincerWideCA.ArmBar(minUnits, armPercent), Info.TeamLeadJoinMinUnits)"), "an arm is never below the join floor");
			Assert.That(decide, Does.Contain("DiagnosticTeamEvent(\"pincer_skipped\""), "a pincer that was not possible says why (DEV)");

			var staging = Between(wiring, "bool TeamLeadPincerStagingAtPush(", "// A ground unit that stands with the blob");
			Assert.That(staging, Does.Contain("PincerWideCA.Candidates(order.Rally, order.Target, -1, standOff, Info.TeamLeadPincerSpreadDegrees)"), "measured from where the blob stands");
			Assert.That(staging, Does.Contain("PincerWideCA.PickPair(order.Target, listA.Count"));
			Assert.That(staging, Does.Contain("Info.AttackScanRadius + 4"), "outside the scan radius of a gathering squad");
			Assert.That(staging, Does.Contain("PathExistsForLocomotor"), "a staging point must be reachable");
			Assert.That(staging, Does.Contain("TeamLeadCA.PincerSeparated("));
			foreach (var banned in new[] { "FindActors", "FindClosestEnemy", "FrozenActorLayer", "World.Actors", "CanBeViewedByPlayer", "ActorsWithTrait" })
				Assert.That(staging, Does.Not.Contain(banned), "the staging search reads terrain and the pathfinder only, never " + banned);
		}

		[Test]
		public void TheOldPlanAtIssueStepsAsideAndThePushAndTheCollapseRulesStayTheOnesTheOrderAlreadyHas()
		{
			var tl = RepoFile(Bots + "SquadManagerBotModuleCA.TeamLead.cs");

			var plan = Between(tl, "void TeamLeadPincerPlan(", "Actor TeamLeadPincerSample()");
			Assert.That(plan.IndexOf("if (ArmyPincerPushOn)\n\t\t\t\treturn;", StringComparison.Ordinal), Is.GreaterThan(plan.IndexOf("if (!TeamLeadPincerOn)", StringComparison.Ordinal)));
			Assert.That(plan.IndexOf("if (ArmyPincerPushOn)\n\t\t\t\treturn;", StringComparison.Ordinal), Is.LessThan(plan.IndexOf("var blob = ", StringComparison.Ordinal)), "row on = no plan from the pool at issue");

			var maintain = Between(tl, "void TeamLeadMaintainOrder(", "bool TeamLeadPushHeld(");
			var single = maintain[maintain.IndexOf("TeamLeadCA.PushDue(", StringComparison.Ordinal)..];
			var hook = single.IndexOf("if (ArmyPincerPushOn && TeamLeadPincerAtPush(order, now))\n\t\t\t\t\treturn;", StringComparison.Ordinal);
			Assert.That(hook, Is.GreaterThan(single.IndexOf("&& !TeamLeadPushHeld(order, now))", StringComparison.Ordinal)), "the push is due and the push bar passed");
			Assert.That(hook, Is.LessThan(single.IndexOf("order.PushTick = now;", StringComparison.Ordinal)), "before the single blob is released");
			Assert.That(tl.Split("TeamLeadPincerAtPush(").Length - 1, Is.EqualTo(1), "one caller: the single-blob push test");

			var collapse = Between(tl, "bool TeamLeadPincerThinCollapse(", "// tl_rally_mid");
			Assert.That(collapse, Does.Contain("if (ArmyPincerPushOn)\n\t\t\t\treturn TeamLeadPincerArmCollapse(order, now);"), "the per-arm bar, whatever the wide switch says");
			Assert.That(collapse.IndexOf("if (ArmyPincerPushOn)", StringComparison.Ordinal), Is.LessThan(collapse.IndexOf("if (TeamLeadPincerWideOn)", StringComparison.Ordinal)));

			// The pincer branch of the upkeep is untouched: the order that became a pincer runs the same hold and push rules, from its restarted gather window.
			Assert.That(maintain, Does.Contain("TeamLeadCA.PincerPushDue(now, order.IssueTick, Info.TeamLeadGatherMinTicks, Info.TeamLeadPincerGatherMaxTicks, Info.TeamLeadPincerHoldTicks,"));
			Assert.That(RepoFile(Bots + "BotModuleLogic/TeamLeadCA.cs"), Does.Contain("Math.Clamp(holdTicks, 1, PincerHoldCapTicks)"), "the hold never exceeds PincerHoldCapTicks (750)");
			Assert.That(TeamLeadCA.PincerHoldCapTicks, Is.EqualTo(750));
		}

		[Test]
		public void TheGatheringSquadDividesOnceThenWalksToItsArmAndAllRowOffCodeIsUntouched()
		{
			var gather = RepoFile(Bots + "Squads/States/TeamLeadGatherStateCA.cs");
			var tick = Between(gather, "public void Tick(SquadCA owner)", "public void Deactivate(SquadCA owner)");
			Assert.That(tick, Does.Contain("if (manager.ArmyPincerPushOn && order.Pincer && !order.Pushed && !fighting)"));
			Assert.That(tick, Does.Contain("manager.TeamLeadSplitOff(owner, order);"));
			Assert.That(tick, Does.Contain("if (!walkedAsPincer)"));
			Assert.That(Between(gather, "public void Activate(SquadCA owner)", "var manager = owner.SquadManager;"), Does.Contain("walkedAsPincer = order.Pincer;"));
			Assert.That(gather, Does.Not.Contain("\"AttackMove\""), "a gathering arm marches with Move, it fights on contact like any gather squad");

			var split = Between(RepoFile(Bots + "SquadManagerBotModuleCA.PincerPush.cs"), "internal void TeamLeadSplitOff(", "\n\t}\n}");
			Assert.That(split, Does.Contain("if (!order.TakeSplit(squad, out var count, out var toProng) || count <= 0 || squad.Units.Count - count < 1)"), "taken once; the squad keeps at least a unit");
			Assert.That(split, Does.Contain("squad.Units.OrderByDescending(u => u.Actor.ActorID).Take(count).ToList();"), "a total order, no choice");
			Assert.That(split, Does.Contain("var half = new SquadCA(squad.Bot, this, squad.Type, null);"), "the same bot, the same kind of squad, no launch counted");
			Assert.That(split, Does.Contain("order.AssignProng(half, toProng, SquadValueOf(half));"));
			Assert.That(split, Does.Contain("half.FuzzyStateMachine.ChangeState(half, new TeamLeadGatherStateCA(order), false);"));
			Assert.That(split, Does.Not.Contain("RegisterNewSquad"), "a divided squad is not a new wave launch");
		}
	}
}
