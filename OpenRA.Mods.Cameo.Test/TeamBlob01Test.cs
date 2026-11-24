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
using OpenRA.Mods.CA.Traits.BotModules.Squads;

namespace OpenRA.Mods.Cameo.Test
{
	/// <summary>
	/// TEAM-BLOB-01 (V02 RCA, USER P0 2026-11-24): V02 published 4 orders in 60000 ticks because the team's idle blob (0-9 units) almost never
	/// reached the WAVE-MIN bar, and the "1500-tick fallback" never fired (it still needed a ready blob). The no-order guarantee is real now
	/// (tl_fallback_publish), an order survives a leader re-election (tl_order_survive) and every cancel / push / abandon says why.
	/// </summary>
	[TestFixture]
	public class TeamBlob01Test
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

		static TeamLeadCA.PublishWhy Why(int sinceOrder, int units, int fallback = 1500, int floor = 3) =>
			TeamLeadCA.FallbackPublish(sinceOrder, fallback, units, floor);

		// ---- the no-order guarantee: a rally order, never an attack (USER rule 2026-11-24: nothing below WAVE-MIN ever attacks) ----

		[Test]
		public void TheGuaranteeCanOnlyPublishARallyOrderNeverAnAttack()
		{
			// The only attack publish is Ready (the OrderDue path). The old Fallback / Forced steps, which sent a 3-11 unit blob out, are gone.
			Assert.That(Enum.GetNames(typeof(TeamLeadCA.PublishWhy)), Is.EquivalentTo(new[] { "None", "Ready", "Gather" }));
			Assert.That(Why(1500, 12), Is.EqualTo(TeamLeadCA.PublishWhy.Gather));
			Assert.That(Why(3000, 11), Is.EqualTo(TeamLeadCA.PublishWhy.Gather), "V02: 6-11 units for 3000 ticks used to be a forced attack");
			Assert.That(Why(9000, 40), Is.EqualTo(TeamLeadCA.PublishWhy.Gather));
		}

		[Test]
		public void BeforeTheFallbackNothingIsPublishedByTheGuarantee()
		{
			Assert.That(Why(1499, 20), Is.EqualTo(TeamLeadCA.PublishWhy.None));
			Assert.That(Why(0, 20), Is.EqualTo(TeamLeadCA.PublishWhy.None));
		}

		[Test]
		public void AnAlmostEmptyBlobAndAZeroFallbackPublishNothing()
		{
			Assert.That(Why(5000, 2), Is.EqualTo(TeamLeadCA.PublishWhy.None), "nothing to gather");
			Assert.That(Why(5000, 0), Is.EqualTo(TeamLeadCA.PublishWhy.None));
			Assert.That(Why(5000, 3), Is.EqualTo(TeamLeadCA.PublishWhy.Gather), "the join floor");
			Assert.That(Why(5000, 20, fallback: 0), Is.EqualTo(TeamLeadCA.PublishWhy.None), "0 turns the guarantee off");
		}

		[Test]
		public void ARallyOrderIsGatherOnlyAndIsPromotedOnlyWhenTheGatheredSquadsPassTheBars()
		{
			var order = new TeamLeadOrder(9, "Multi0", new WPos(1024, 1024, 0), new WPos(9000, 9000, 0), 42, 3000);
			Assert.That(order.GatherOnly, Is.False, "an order is an attack order unless the fallback makes it a rally order");
			order.MakeGatherOnly();
			Assert.That(order.GatherOnly, Is.True);

			var a = new object();
			var b = new object();
			order.Report(a, 2400, true, 5);
			order.Report(b, 2000, false, 6);
			var (units, value) = order.CommittedTotals();
			Assert.That((units, value), Is.EqualTo((11, 4400L)));

			// WAVE-MIN 12 units / 6000 value: 11 units and 4400 value are below it, so the rally order stays.
			Assert.That(TeamLeadCA.BlobReady(units, value, 3, true, 12, 6000), Is.False);
			order.Report(b, 3700, true, 7);
			(units, value) = order.CommittedTotals();
			Assert.That(TeamLeadCA.BlobReady(units, value, 3, true, 12, 6000), Is.True, "12 units and 6100 value pass both bars");

			order.Promote(4200, new WPos(5000, 5000, 0), 77);
			Assert.That(order.GatherOnly, Is.False);
			Assert.That(order.IssueTick, Is.EqualTo(4200), "the gather window restarts");
			Assert.That(order.TargetActorId, Is.EqualTo(77u));
			Assert.That(order.Target, Is.EqualTo(new WPos(5000, 5000, 0)));
			Assert.That(order.Seq, Is.EqualTo(9));
			Assert.That(order.ReportCount, Is.EqualTo(2), "the gathered squads stay in the order");
			Assert.That(order.Pushed, Is.False, "promotion is not a push");
		}

		[Test]
		public void AReportWithoutUnitsAddsNothingToTheUnitTotalSoOldCallersStayValueOnly()
		{
			var order = new TeamLeadOrder(1, "Multi0", new WPos(0, 0, 0), new WPos(1, 1, 0), 0, 0);
			order.Report(new object(), 1000, true);
			Assert.That(order.CommittedTotals(), Is.EqualTo((0, 1000L)));
			Assert.That(order.Totals(), Is.EqualTo((1000L, 1000L)));
		}

		/// <summary>
		/// The V02 team from its real state: an idle blob that grows slowly and sits at 6-11 units for thousands of ticks while the bars are 12 units / 6000 value.
		/// Whatever the leader does, nothing is ever pushed below the bars; the rally order is re-published while it waits and is promoted the tick the squads pass them.
		/// </summary>
		[Test]
		public void TheV02TeamNeverPushesBelowTheBarsAndRalliesInsteadUntilItPassesThem()
		{
			const int MinUnits = 12;
			const int MinValue = 6000;
			const int Start = 3000;
			const int UnitValue = 500;
			TeamLeadOrder open = null;
			var lastOrder = -1;
			var rallyOrders = 0;
			var attackOrders = 0;
			var promotions = 0;
			var firstAttackTick = -1;
			var firstRallyTick = -1;
			for (var now = Start; now <= 60000; now += 100)
			{
				// The blob: 0 -> 14 units over the first 40000 ticks (below the bar for a long time), losses now and then.
				var blobUnits = Math.Min(14, (now - Start) / 2800);
				if (now % 7000 == 0)
					blobUnits = Math.Max(0, blobUnits - 4);
				var blobValue = (long)blobUnits * UnitValue;

				if (open != null && open.GatherOnly)
				{
					// All of the blob has joined the rally order and reported (the members re-join every check).
					open.Report(open, blobValue, true, blobUnits);
					var (cu, cv) = open.CommittedTotals();
					if (TeamLeadCA.BlobReady(cu, cv, 3, true, MinUnits, MinValue))
					{
						open.Promote(now, new WPos(1, 1, 0), 1);
						promotions++;
						Assert.That(cu, Is.GreaterThanOrEqualTo(MinUnits), "a promotion never pushes below the unit bar");
						Assert.That(cv, Is.GreaterThanOrEqualTo(MinValue), "...or the value bar");
						firstAttackTick = firstAttackTick < 0 ? now : firstAttackTick;
						open = null;
					}

					continue;
				}

				var since = BlobLaunch01CA.TicksSinceOrder(now, lastOrder, Start);
				var ready = TeamLeadCA.BlobReady(blobUnits, blobValue, 3, true, MinUnits, MinValue);
				var why = TeamLeadCA.OrderDue(false, ready, now, lastOrder, 1500)
					? TeamLeadCA.PublishWhy.Ready
					: TeamLeadCA.FallbackPublish(since, 1500, blobUnits, 3);
				if (why == TeamLeadCA.PublishWhy.None)
					continue;

				lastOrder = now;
				if (why == TeamLeadCA.PublishWhy.Ready)
				{
					Assert.That(blobUnits, Is.GreaterThanOrEqualTo(MinUnits), "an attack order needs the unit bar");
					Assert.That(blobValue, Is.GreaterThanOrEqualTo(MinValue), "an attack order needs the value bar");
					attackOrders++;
					firstAttackTick = firstAttackTick < 0 ? now : firstAttackTick;
				}
				else
				{
					rallyOrders++;
					firstRallyTick = firstRallyTick < 0 ? now : firstRallyTick;
					open = new TeamLeadOrder(rallyOrders, "Multi0", new WPos(0, 0, 0), new WPos(1, 1, 0), 0, now);
					open.MakeGatherOnly();
				}
			}

			Assert.That(rallyOrders, Is.GreaterThanOrEqualTo(1), "while the blob is below the bars the fallback rallies the team");
			Assert.That(firstRallyTick, Is.LessThan(firstAttackTick), "the rally comes first, the attack only after the bars are passed");
			Assert.That(attackOrders + promotions, Is.GreaterThanOrEqualTo(1), "the team does attack once the blob passes the bars");
		}

		// ---- the order survives a leader re-election ----

		[Test]
		public void AHandedOverOrderKeepsItsSquadsAndItsClockAndOnlyTheLeaderChanges()
		{
			var order = new TeamLeadOrder(7, "Multi2", new WPos(1024, 1024, 0), new WPos(9000, 9000, 0), 42, 6003);
			var squad = new object();
			order.Report(squad, 4160, true);
			Assert.That(order.LeaderKey, Is.EqualTo("Multi2"));

			order.HandOver("Multi5");
			Assert.That(order.LeaderKey, Is.EqualTo("Multi5"));
			Assert.That(order.Cancelled, Is.False, "a hand-over is not a cancel");
			Assert.That(order.ReportCount, Is.EqualTo(1), "the gathered squads stay");
			Assert.That(order.IssueTick, Is.EqualTo(6003), "the order keeps its age: the gather window does not restart");
			Assert.That(order.Seq, Is.EqualTo(7));
			Assert.That(order.TargetActorId, Is.EqualTo(42u));
		}

		// ---- wiring pins ----

		[Test]
		public void TheSwitchesDefaultOffAreOnInSixPersonalitiesNotClassicAndHaveTheirOwnRows()
		{
			var info = RepoFile(Bots + "SquadManagerBotModuleCAInfo.TeamLead.cs");
			Assert.That(info, Does.Contain("public readonly bool TeamLeadFallbackPublishEnabled = false;"));
			Assert.That(info, Does.Not.Contain("TeamLeadForcedPublishTicks"), "the forced attack step is gone: nothing below WAVE-MIN attacks");
			Assert.That(info, Does.Contain("RALLY order"));
			Assert.That(info, Does.Contain("public readonly bool TeamLeadOrderSurviveEnabled = false;"));

			var yaml = RepoFile("mods/cameo/ai/ai.yaml");
			var classic = yaml.IndexOf("SquadManagerBotModuleCA@classic:", StringComparison.Ordinal);
			Assert.That(classic, Is.GreaterThan(0));
			foreach (var key in new[] { "TeamLeadFallbackPublishEnabled", "TeamLeadOrderSurviveEnabled" })
			{
				Assert.That(Regex.Matches(yaml[..classic], @"\n\t\t" + key + ": true").Count, Is.EqualTo(6), key);
				Assert.That(yaml[classic..], Does.Not.Contain(key), key + ": @classic keeps the default (off)");
			}

			Assert.That(AblationIds.TeamLeadFallbackPublish, Is.EqualTo("tl_fallback_publish"));
			Assert.That(AblationIds.TeamLeadOrderSurvive, Is.EqualTo("tl_order_survive"));
		}

		[Test]
		public void ThePublishAsksTheReadyRuleFirstAndTheGuaranteeOnlyWhenThatSaysNo()
		{
			var wiring = RepoFile(Bots + "SquadManagerBotModuleCA.TeamLead.cs");
			var start = wiring.IndexOf("void TeamLeadPublish(", StringComparison.Ordinal);
			var body = wiring[start..wiring.IndexOf("void TeamLeadJoin(", start, StringComparison.Ordinal)];
			Assert.That(body.IndexOf("TeamLeadCA.OrderDue(", StringComparison.Ordinal), Is.GreaterThan(0));
			Assert.That(body.IndexOf("TeamLeadCA.FallbackPublish(", StringComparison.Ordinal), Is.GreaterThan(body.IndexOf("TeamLeadCA.OrderDue(", StringComparison.Ordinal)));
			Assert.That(body, Does.Contain("if (why == TeamLeadCA.PublishWhy.None && TeamLeadFallbackPublishOn)"));
			Assert.That(body, Does.Not.Contain("MinUnits(minUnits, blob.Units)"), "no unit bar of its own: the fallback never attacks");
			Assert.That(body.IndexOf("TeamLeadPromoteRally(rally, now);", StringComparison.Ordinal), Is.LessThan(body.IndexOf("if (now < initialAttackDelay || TeamLeadOpenOrder() != null)", StringComparison.Ordinal)), "a rally order is promoted before the open-order guard");
			Assert.That(body, Does.Contain("if (why == TeamLeadCA.PublishWhy.Gather)\n\t\t\t\torder.MakeGatherOnly();"));
			Assert.That(body, Does.Contain("BlobLaunch01CA.TicksSinceOrder("));
			Assert.That(body, Does.Contain("if (now < initialAttackDelay || TeamLeadOpenOrder() != null)"), "never while an order is open or before the initial attack delay");
			Assert.That(body, Does.Contain("DiagnosticTeamEvent(\"team_order\""));
			Assert.That(wiring, Does.Contain("bool TeamLeadFallbackPublishOn => TeamLeadCadenceOn && Info.TeamLeadFallbackPublishEnabled && !ablateFallbackPublish.Off(Player, AblationIds.TeamLeadFallbackPublish);"));
		}

		[Test]
		public void ALeaderChangeHandsTheOrderOverOnlyWithTheRowOnAndEveryCancelSaysWhy()
		{
			var wiring = RepoFile(Bots + "SquadManagerBotModuleCA.TeamLead.cs");
			var start = wiring.IndexOf("void TeamLeadMaintainOrder(", StringComparison.Ordinal);
			var body = wiring[start..wiring.IndexOf("TeamLeadOrder TeamLeadOpenOrder()", start, StringComparison.Ordinal)];
			Assert.That(body, Does.Contain("if (order.LeaderKey != team.Election.Leader && team.Election.Leader != null && TeamLeadOrderSurviveOn)"));
			Assert.That(body, Does.Contain("order.HandOver(team.Election.Leader);"));
			foreach (var why in new[] { "leader_changed", "max_age", "no_reports" })
				Assert.That(body, Does.Contain("\"" + why + "\""), why);
			Assert.That(body, Does.Contain("order.CancelReason = cancel;"));
			Assert.That(body, Does.Contain("DiagnosticTeamEvent(\"team_cancel\""));
			Assert.That(body, Does.Contain("DiagnosticTeamEvent(\"team_push\""));
			Assert.That(wiring, Does.Contain("bool TeamLeadOrderSurviveOn => TeamLeadOn && Info.TeamLeadOrderSurviveEnabled && !ablateOrderSurvive.Off(Player, AblationIds.TeamLeadOrderSurvive);"));
		}

		[Test]
		public void ARallyOrderNeverPushesNeverAgesOutAndIsJoinedAgainAndAgainByRealGroups()
		{
			var wiring = RepoFile(Bots + "SquadManagerBotModuleCA.TeamLead.cs");
			var start = wiring.IndexOf("void TeamLeadMaintainOrder(", StringComparison.Ordinal);
			var body = wiring[start..wiring.IndexOf("TeamLeadOrder TeamLeadOpenOrder()", start, StringComparison.Ordinal)];
			Assert.That(body, Does.Contain(": !order.GatherOnly && !order.Extending && now - order.IssueTick > Info.TeamLeadOrderMaxTicks ? \"max_age\""), "a rally order waits as long as it takes");
			var gatherReturn = body.IndexOf("if (order.GatherOnly)\n\t\t\t\treturn;", StringComparison.Ordinal);
			Assert.That(gatherReturn, Is.GreaterThan(0));
			Assert.That(gatherReturn, Is.LessThan(body.IndexOf("if (order.Pincer)", StringComparison.Ordinal)), "no push (single blob or pincer) for a rally order");
			Assert.That(gatherReturn, Is.LessThan(body.IndexOf("TeamLeadCA.PushDue(", StringComparison.Ordinal)));
			Assert.That(body, Does.Contain("order.Prune("), "a rally order still drops squads that left it");

			var join = wiring[wiring.IndexOf("void TeamLeadJoin(", StringComparison.Ordinal)..wiring.IndexOf("bool TeamLeadPickTarget(", StringComparison.Ordinal)];
			Assert.That(join, Does.Contain("(order.Seq == teamLeadJoinedSeq && !order.GatherOnly && !order.Extending)"), "an attack order is joined once, a rally (or held) order again and again");
			Assert.That(join, Does.Contain("teamLeadIsLeader && !order.GatherOnly && !order.Extending ? Info.TeamLeadLeaderMinUnits : Info.TeamLeadJoinMinUnits"), "no one-unit rally squads");

			Assert.That(wiring, Does.Contain("void TeamLeadPromoteRally(TeamLeadOrder order, int now)"));
			Assert.That(wiring, Does.Contain("var (units, value) = order.CommittedTotals();"));
			Assert.That(wiring, Does.Contain("TeamLeadCA.BlobReady(units, value, Info.TeamLeadJoinMinUnits, WaveMinCA.Limits(minUnits, minValue), minUnits, minValue)"), "the same bars as the ready path");
			Assert.That(wiring, Does.Contain("order.Promote(now, target, targetId);"));
			Assert.That(wiring, Does.Contain("DiagnosticTeamEvent(\"team_promote\""));
			Assert.That(wiring, Does.Contain("order.Report(squad, SquadValueOf(squad), near * 100 >= squad.Units.Count * Info.TeamLeadArrivePercent, squad.Units.Count);"));
		}

		// ---- the push bar: a due push waits while the squads at the rally are below the bar; the hold has a hard cap ----

		[Test]
		public void TheHoldDissolvesTheOrderOnlyAfterTheCapAndNeverBeforeAHoldStarted()
		{
			Assert.That(TeamLeadCA.PushBarDissolve(99999, -1, 3000), Is.False, "never held = never dissolved");
			Assert.That(TeamLeadCA.PushBarDissolve(3099, 100, 3000), Is.False);
			Assert.That(TeamLeadCA.PushBarDissolve(3100, 100, 3000), Is.True, "3000 ticks after the first held push");
			Assert.That(TeamLeadCA.PushBarDissolve(100, 100, 0), Is.False, "a zero cap counts as 1 tick, not as a free pass");
			Assert.That(TeamLeadCA.PushBarDissolve(101, 100, 0), Is.True);
		}

		[Test]
		public void TheBarIsTheBlobLaunchMinimumForUnitsAndTheWaveMinValue()
		{
			// WAVE-MIN 12 units / 6000 value, team idle army 40: half of it = 20 > 12, so 20 units must stand at the rally; 19 do not.
			var need = BlobLaunch01CA.MinUnits(12, 40);
			Assert.That(need, Is.EqualTo(20));
			Assert.That(TeamLeadCA.PushBarPassed(20, 6000, 3, need, 6000), Is.True);
			Assert.That(TeamLeadCA.PushBarPassed(19, 9000, 3, need, 6000), Is.False, "one unit short of the unit bar");
			Assert.That(TeamLeadCA.PushBarPassed(30, 5999, 3, need, 6000), Is.False, "one value short of the value bar");

			// A small army: the WAVE-MIN tier is the bar (half of 16 = 8 < 12).
			Assert.That(BlobLaunch01CA.MinUnits(12, 16), Is.EqualTo(12));
			Assert.That(TeamLeadCA.PushBarPassed(11, 9000, 3, 12, 6000), Is.False);
			Assert.That(TeamLeadCA.PushBarPassed(12, 6000, 3, 12, 6000), Is.True);

			// No tier: half of the army, never less than the join floor.
			Assert.That(TeamLeadCA.PushBarPassed(2, 0, 3, BlobLaunch01CA.MinUnits(0, 4), 0), Is.False);
			Assert.That(TeamLeadCA.PushBarPassed(3, 0, 3, BlobLaunch01CA.MinUnits(0, 4), 0), Is.True);
		}

		[Test]
		public void TheHoldIsRecordedOnceAndOnlyTheSquadsAtTheRallyCountTowardTheBar()
		{
			var order = new TeamLeadOrder(4, "Multi1", new WPos(0, 0, 0), new WPos(1, 1, 0), 0, 1000);
			Assert.That(order.Extending, Is.False);
			order.Report(new object(), 3000, true, 6);
			order.Report(new object(), 2500, false, 5);
			Assert.That(order.ArrivedTotals(), Is.EqualTo((6, 3000L)), "a squad still on the way does not count");
			Assert.That(order.CommittedTotals(), Is.EqualTo((11, 5500L)));

			order.HoldPush(2500);
			order.HoldPush(2900);
			Assert.That(order.Extending, Is.True);
			Assert.That(order.PushBarSince, Is.EqualTo(2500), "the cap runs from the FIRST held push");
		}

		/// <summary>Opus's case: three followers with 4 / 10 / 6 units (V03: each share of 4-10 units went alone). All hold at the rally until 20 have gathered, then all move on one tick.</summary>
		[Test]
		public void ThreeFollowerSharesOf4And10And6HoldAtTheRallyUntil20HaveGatheredThenAdvanceTogether()
		{
			var order = new TeamLeadOrder(1, "Multi0", new WPos(0, 0, 0), new WPos(1, 1, 0), 0, 0);
			var share4 = new object();
			var share10 = new object();
			var share6 = new object();
			const int TeamIdle = 40; // blob left at the bases + the three shares
			var need = BlobLaunch01CA.MinUnits(12, TeamIdle);
			Assert.That(need, Is.EqualTo(20));

			// The shares reach the rally at 200, 500 and 900 and report there (4 + 10 = 14 units, then 20).
			var arrivals = new[] { (Tick: 200, Squad: share4, Units: 4, Value: 1600L), (Tick: 500, Squad: share10, Units: 10, Value: 4000L), (Tick: 900, Squad: share6, Units: 6, Value: 2400L) };
			var pushTick = -1;
			var pushedUnits = 0;
			for (var now = 0; now <= 3000 && pushTick < 0; now += 100)
			{
				foreach (var a in arrivals)
					if (now >= a.Tick)
						order.Report(a.Squad, a.Value, true, a.Units);
					else
						order.Report(a.Squad, a.Value, false, a.Units);

				if (order.Extending && TeamLeadCA.PushBarDissolve(now, order.PushBarSince, 3000))
					Assert.Fail("dissolved although the shares gathered in time");

				var (committed, arrived) = order.Totals();
				if (!TeamLeadCA.PushDue(now, order.IssueTick, 300, 1500, committed, arrived, 70))
					continue;

				var (units, value) = order.ArrivedTotals();
				if (!TeamLeadCA.PushBarPassed(units, value, 3, need, 6000))
				{
					order.HoldPush(now);
					continue;
				}

				pushTick = now;
				pushedUnits = units;
			}

			Assert.That(order.Extending, Is.True, "the 4 and the 10 were held, not sent alone");
			Assert.That(pushTick, Is.EqualTo(900), "the push is on the check the last share reports in: 4 + 10 + 6 = 20 units, 8000 value");
			Assert.That(pushedUnits, Is.EqualTo(20));

			// One order, one push tick: every gathered squad advances on it (PushTick is the order's, not the squad's), and a share formed after it finds no open order.
			order.PushTick = pushTick;
			Assert.That(order.Open(pushTick + 1, 0), Is.False, "push bar on: no late joiner follows the push alone");
			Assert.That(order.Open(pushTick, 0), Is.True);
		}

		/// <summary>The window push of a ready order whose squads at the rally are below the bar: it waits, keeps merging, then pushes or dissolves.</summary>
		[Test]
		public void ASmallArrivedGroupIsHeldThenPushesWhenItPassesTheBarOrIsDissolvedAtTheCap()
		{
			const int Need = 12;
			const int MinValue = 6000;

			// (a) 8 units / 4000 value at the rally for ever: due at the early arrival, held, dissolved 3000 ticks later. It never pushes.
			var order = new TeamLeadOrder(1, "Multi0", new WPos(0, 0, 0), new WPos(1, 1, 0), 0, 0);
			var pushed = false;
			var dissolvedAt = -1;
			for (var now = 0; now <= 9000 && dissolvedAt < 0; now += 100)
			{
				order.Report(order, 4000, true, 8);
				if (order.Extending && TeamLeadCA.PushBarDissolve(now, order.PushBarSince, 3000))
				{
					dissolvedAt = now;
					break;
				}

				var (committed, arrived) = order.Totals();
				if (!TeamLeadCA.PushDue(now, order.IssueTick, 300, 1500, committed, arrived, 70))
					continue;

				var (au, av) = order.ArrivedTotals();
				if (!TeamLeadCA.PushBarPassed(au, av, 3, Need, MinValue))
				{
					order.HoldPush(now);
					continue;
				}

				pushed = true;
			}

			Assert.That(pushed, Is.False, "8 units never push against a 12 unit bar");
			Assert.That(order.PushBarSince, Is.EqualTo(300), "held from the first due check (the early arrival at the gather minimum)");
			Assert.That(dissolvedAt, Is.EqualTo(3300), "dissolved 3000 ticks after the first held push");

			// (b) the same order with a unit merging in every 300 ticks: it pushes the check the arrived units AND value pass the bar, before the cap.
			order = new TeamLeadOrder(2, "Multi0", new WPos(0, 0, 0), new WPos(1, 1, 0), 0, 0);
			var pushTick = -1;
			for (var now = 0; now <= 9000 && pushTick < 0; now += 100)
			{
				var units = 8 + now / 300;
				order.Report(order, units * 500L, true, units);
				if (order.Extending && TeamLeadCA.PushBarDissolve(now, order.PushBarSince, 3000))
					Assert.Fail("dissolved although the bar was passed in time");

				var (committed, arrived) = order.Totals();
				if (!TeamLeadCA.PushDue(now, order.IssueTick, 300, 1500, committed, arrived, 70))
					continue;

				var (au, av) = order.ArrivedTotals();
				if (!TeamLeadCA.PushBarPassed(au, av, 3, Need, MinValue))
				{
					order.HoldPush(now);
					continue;
				}

				pushTick = now;
				Assert.That(au, Is.GreaterThanOrEqualTo(Need));
				Assert.That(av, Is.GreaterThanOrEqualTo(MinValue));
			}

			Assert.That(pushTick, Is.EqualTo(1200), "8 + 1200/300 = 12 units = 6000 value");
			Assert.That(order.Extending, Is.True, "it was held first");

			// (c) V03 saw a window push with committed 0 / arrived 0: nothing at the rally is below every bar, so it is held (and the order dies at the cap).
			Assert.That(TeamLeadCA.PushBarPassed(0, 0, 3, Need, MinValue), Is.False);
		}

		[Test]
		public void ThePushBarGatesBothPushPathsAndTheHoldHasItsOwnCancelAndNoMaxAge()
		{
			var wiring = RepoFile(Bots + "SquadManagerBotModuleCA.TeamLead.cs");
			var start = wiring.IndexOf("void TeamLeadMaintainOrder(", StringComparison.Ordinal);
			var body = wiring[start..wiring.IndexOf("TeamLeadOrder TeamLeadOpenOrder()", start, StringComparison.Ordinal)];
			Assert.That(body, Does.Contain("order.FirstReadyTick) && !TeamLeadPushHeld(order, now))"), "the pincer push");
			Assert.That(body, Does.Contain("committed, arrived, Info.TeamLeadArrivePercent)\n\t\t\t\t&& !TeamLeadPushHeld(order, now))"), "the single-blob push (window and early arrival)");
			Assert.That(body, Does.Contain(": order.Extending && TeamLeadCA.PushBarDissolve(now, order.PushBarSince, Info.TeamLeadPushBarMaxTicks) ? \"below_bar\""));
			Assert.That(body, Does.Contain(": !order.GatherOnly && !order.Extending && now - order.IssueTick > Info.TeamLeadOrderMaxTicks ? \"max_age\""), "the hold has its own cap");

			var held = wiring[wiring.IndexOf("bool TeamLeadPushHeld(", StringComparison.Ordinal)..wiring.IndexOf("void DiagnosticTeamEvent(", StringComparison.Ordinal)];
			Assert.That(held, Does.Contain("if (!TeamLeadPushBarOn)\n\t\t\t\treturn false;"), "off = every due push goes");
			Assert.That(held, Does.Contain("order.ArrivedTotals()"), "the squads at the rally, not the whole blob");
			Assert.That(held, Does.Contain("BlobLaunch01CA.MinUnits(minUnits, teamIdle)"), "the blob launch minimum");
			Assert.That(held, Does.Contain("TeamLeadCA.PushBarPassed(units, value, Info.TeamLeadJoinMinUnits, needUnits, minValue)"));
			Assert.That(held, Does.Contain("DiagnosticTeamEvent(\"team_push_hold\""));
			Assert.That(wiring, Does.Contain("internal bool TeamLeadPushBarOn => TeamLeadOn && Info.TeamLeadPushBarEnabled && !ablatePushBar.Off(Player, AblationIds.TeamLeadPushBar);"));
			Assert.That(wiring, Does.Contain("order.Open(World.WorldTick, TeamLeadPushBarOn ? 0 : Info.TeamLeadLateJoinTicks)"), "no late joiner follows a push alone");

			var join = wiring[wiring.IndexOf("void TeamLeadJoin(", StringComparison.Ordinal)..wiring.IndexOf("bool TeamLeadPickTarget(", StringComparison.Ordinal)];
			Assert.That(join, Does.Contain("(order.Seq == teamLeadJoinedSeq && !order.GatherOnly && !order.Extending)"), "a held order keeps merging new groups");
		}

		[Test]
		public void ThePushBarSwitchDefaultsOffIsOnInSixPersonalitiesNotClassicAndHasItsRow()
		{
			var info = RepoFile(Bots + "SquadManagerBotModuleCAInfo.TeamLead.cs");
			Assert.That(info, Does.Contain("public readonly bool TeamLeadPushBarEnabled = false;"));
			Assert.That(info, Does.Contain("public readonly int TeamLeadPushBarMaxTicks = 3000;"));

			var yaml = RepoFile("mods/cameo/ai/ai.yaml");
			var classic = yaml.IndexOf("SquadManagerBotModuleCA@classic:", StringComparison.Ordinal);
			Assert.That(Regex.Matches(yaml[..classic], @"\n\t\tTeamLeadPushBarEnabled: true").Count, Is.EqualTo(6));
			Assert.That(yaml[classic..], Does.Not.Contain("TeamLeadPushBarEnabled"));
			Assert.That(AblationIds.TeamLeadPushBar, Is.EqualTo("tl_push_bar"));
		}

		[Test]
		public void TheGatherStateSaysWhyASquadLeavesTheOrderAndWhenItGoes()
		{
			var gather = RepoFile(Bots + "Squads/States/TeamLeadGatherStateCA.cs");
			Assert.That(gather, Does.Contain("\"not_engaged\""));
			Assert.That(gather, Does.Contain("\"cancelled:\" + (order.CancelReason ?? \"unknown\")"));
			Assert.That(gather, Does.Contain("\"resolve_failed\""));
			Assert.That(gather, Does.Contain("manager.TeamLeadNoteContact(owner, order);"));
			Assert.That(gather, Does.Contain("manager.TeamLeadNoteGo(owner, order);"));

			var wiring = RepoFile(Bots + "SquadManagerBotModuleCA.TeamLead.cs");
			Assert.That(wiring, Does.Contain("internal void TeamLeadAbandon(SquadCA squad, string why = null)"));
			Assert.That(wiring, Does.Contain("DiagnosticSquadEvent(squad, \"team_abandon\", why);"));
			Assert.That(new[] { "team_order", "team_push", "team_cancel", "team_handover", "team_contact", "team_go" }.All(e => wiring.Contains("\"" + e + "\"")), Is.True);
		}
	}
}
