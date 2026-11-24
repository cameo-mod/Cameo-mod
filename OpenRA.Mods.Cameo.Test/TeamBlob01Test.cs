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
			Assert.That(body, Does.Contain(": !order.GatherOnly && now - order.IssueTick > Info.TeamLeadOrderMaxTicks ? \"max_age\""), "a rally order waits as long as it takes");
			var gatherReturn = body.IndexOf("if (order.GatherOnly)\n\t\t\t\treturn;", StringComparison.Ordinal);
			Assert.That(gatherReturn, Is.GreaterThan(0));
			Assert.That(gatherReturn, Is.LessThan(body.IndexOf("if (order.Pincer)", StringComparison.Ordinal)), "no push (single blob or pincer) for a rally order");
			Assert.That(gatherReturn, Is.LessThan(body.IndexOf("TeamLeadCA.PushDue(", StringComparison.Ordinal)));
			Assert.That(body, Does.Contain("order.Prune("), "a rally order still drops squads that left it");

			var join = wiring[wiring.IndexOf("void TeamLeadJoin(", StringComparison.Ordinal)..wiring.IndexOf("bool TeamLeadPickTarget(", StringComparison.Ordinal)];
			Assert.That(join, Does.Contain("(order.Seq == teamLeadJoinedSeq && !order.GatherOnly)"), "an attack order is joined once, a rally order again and again");
			Assert.That(join, Does.Contain("teamLeadIsLeader && !order.GatherOnly ? Info.TeamLeadLeaderMinUnits : Info.TeamLeadJoinMinUnits"), "no one-unit rally squads");

			Assert.That(wiring, Does.Contain("void TeamLeadPromoteRally(TeamLeadOrder order, int now)"));
			Assert.That(wiring, Does.Contain("var (units, value) = order.CommittedTotals();"));
			Assert.That(wiring, Does.Contain("TeamLeadCA.BlobReady(units, value, Info.TeamLeadJoinMinUnits, WaveMinCA.Limits(minUnits, minValue), minUnits, minValue)"), "the same bars as the ready path");
			Assert.That(wiring, Does.Contain("order.Promote(now, target, targetId);"));
			Assert.That(wiring, Does.Contain("DiagnosticTeamEvent(\"team_promote\""));
			Assert.That(wiring, Does.Contain("order.Report(squad, SquadValueOf(squad), near * 100 >= squad.Units.Count * Info.TeamLeadArrivePercent, squad.Units.Count);"));
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
