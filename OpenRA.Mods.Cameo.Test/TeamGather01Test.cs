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
	/// TEAM-GATHER-01 (V04 RCA, USER order 2026-11-24): V04 published 26 team orders and none ever pushed. Gathering squads left their order on the first contact
	/// (tl_gather_hold), a held order was cancelled for missing reports (tl_grace_hold), and the liveness and stalemate-breaker launches attacked beside the order
	/// (tl_route_all). Each fix has its own switch (release, default off, ON in the six personalities) and DEV row.
	/// </summary>
	[TestFixture]
	public class TeamGather01Test
	{
		const string Bots = "OpenRA.Mods.CA/Traits/BotModules/";

		// Source text with LF endings (a Windows checkout has CRLF).
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

		[Test]
		public void AGatheringSquadKeepsItsOrderOnContactOnlyBeforeThePush()
		{
			Assert.That(TeamLeadCA.GatherHoldsOnContact(true, false), Is.True, "gather hold on, order not pushed: the contact does not end the order");
			Assert.That(TeamLeadCA.GatherHoldsOnContact(true, true), Is.False, "after the push the old contact rule applies (the squad attacks)");
			Assert.That(TeamLeadCA.GatherHoldsOnContact(false, false), Is.False, "row off = the old contact rule");
			Assert.That(TeamLeadCA.GatherHoldsOnContact(false, true), Is.False);
		}

		[Test]
		public void AHeldOrderIsNeverCancelledForMissingReportsButAnOrderThatIsNotHeldStillIs()
		{
			Assert.That(TeamLeadCA.GraceHolds(true, true), Is.True, "held (extending) + row on: the no-report grace cannot cancel it");
			Assert.That(TeamLeadCA.GraceHolds(true, false), Is.False, "an order that is not held keeps the no-report grace");
			Assert.That(TeamLeadCA.GraceHolds(false, true), Is.False, "row off = the old behaviour");

			// The V04 shape: held at 1500, no squad reporting; the hard cap, not the grace, ends it at 3000 ticks after the first hold.
			var order = new TeamLeadOrder(1, "Multi0", new WPos(0, 0, 0), new WPos(1, 1, 0), 0, 1500);
			order.HoldPush(1600);
			Assert.That(order.Extending, Is.True);
			Assert.That(order.ReportCount, Is.EqualTo(0));
			Assert.That(TeamLeadCA.GraceHolds(true, order.Extending), Is.True);
			Assert.That(TeamLeadCA.PushBarDissolve(4599, order.PushBarSince, 3000), Is.False);
			Assert.That(TeamLeadCA.PushBarDissolve(4600, order.PushBarSince, 3000), Is.True, "the dissolve stays at 3000 ticks after the first hold");
		}

		[Test]
		public void AReportOfALiveSquadSurvivesAFightAndIsDroppedOnlyWhenTheSquadIsGone()
		{
			var order = new TeamLeadOrder(1, "Multi0", new WPos(0, 0, 0), new WPos(1, 1, 0), 0, 1500);
			var fighting = new object();
			var gone = new object();
			order.Report(fighting, 900, false, 4);
			order.Report(gone, 600, true, 3);
			Assert.That(order.ReportCount, Is.EqualTo(2));
			Assert.That(order.CommittedTotals().Units, Is.EqualTo(7), "the squad that stopped to fight still counts as committed");

			order.Prune(o => ReferenceEquals(o, fighting));
			Assert.That(order.ReportCount, Is.EqualTo(1));
			Assert.That(order.CommittedTotals().Units, Is.EqualTo(4));
		}

		[Test]
		public void TheGatherStateReportsAtJoinFightsInPlaceThenWalksOnAndNeverUsesAttackMove()
		{
			var gather = RepoFile(Bots + "Squads/States/TeamLeadGatherStateCA.cs");
			var activate = Between(gather, "public void Activate(", "public void Tick(");
			Assert.That(activate, Does.Contain("if (manager.TeamLeadGatherHoldOn)\n\t\t\t\tmanager.TeamLeadReport(owner, order);"), "reports at join, in Activate");

			var tick = Between(gather, "public void Tick(", "public void Deactivate(");
			Assert.That(tick, Does.Contain("TeamLeadCA.GatherHoldsOnContact(manager.TeamLeadGatherHoldOn, order.Pushed)"));
			Assert.That(tick, Does.Contain("WDist.FromCells(manager.Info.TeamLeadGatherFightCells)"), "the fight range, not the scan radius");
			Assert.That(tick, Does.Contain("new Order(\"Stop\", u.Actor, false)"), "idle units fire at what is in weapon range");
			Assert.That(tick, Does.Contain("fighting = false;\n\t\t\t\t\tActivate(owner);"), "back to the rally when the fight is over");
			Assert.That(tick, Does.Contain("manager.Info.TeamLeadGatherFightMaxTicks"), "a stand-off nobody can win is dropped");
			Assert.That(tick, Does.Contain("owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsAttackState(), false);"), "the old contact rule stays for the pushed order and the row off");
			Assert.That(gather, Does.Not.Contain("\"AttackMove\""), "a gathering squad walks with Move");

			var fightBranch = Between(tick, "if (TeamLeadCA.GatherHoldsOnContact(", "else\n");
			Assert.That(fightBranch, Does.Not.Contain("ChangeState("), "a fight before the push never changes the squad's state");

			var deactivate = gather[gather.IndexOf("public void Deactivate(", StringComparison.Ordinal)..];
			Assert.That(deactivate, Does.Contain("if (!owner.SquadManager.TeamLeadGatherHoldOn)\n\t\t\t\torder.Forget(owner);"), "the report outlives a state change");
			Assert.That(Regex.Matches(tick, @"order\.Forget\(owner\);").Count, Is.EqualTo(2), "a squad that abandons the order takes its report with it");
		}

		[Test]
		public void TheOrderUpkeepKeepsReportsOfLiveSquadsHoldsTheGraceAndRefreshesItOnAHeldPush()
		{
			var wiring = RepoFile(Bots + "SquadManagerBotModuleCA.TeamLead.cs");
			var upkeep = Between(wiring, "void TeamLeadMaintainOrder(", "bool TeamLeadPushHeld(");
			Assert.That(upkeep, Does.Contain("if (TeamLeadGatherHoldOn)\n\t\t\t\torder.Prune(o => o is SquadCA q && q.IsValid && Squads.Contains(q));"));
			Assert.That(upkeep, Does.Contain("order.Prune(o => o is SquadCA q && q.IsValid && q.FuzzyStateMachine?.CurrentState is TeamLeadGatherStateCA);"), "row off = the old prune");
			Assert.That(upkeep, Does.Contain(": TeamLeadCA.GraceHolds(TeamLeadGraceHoldOn, order.Extending) ? null\n"));
			Assert.That(upkeep.IndexOf("TeamLeadCA.GraceHolds(", StringComparison.Ordinal), Is.LessThan(upkeep.IndexOf("\"no_reports\"", StringComparison.Ordinal)), "before the no_reports test");
			Assert.That(upkeep.IndexOf("\"below_bar\"", StringComparison.Ordinal), Is.LessThan(upkeep.IndexOf("TeamLeadCA.GraceHolds(", StringComparison.Ordinal)), "the dissolve test comes first");

			var held = Between(wiring, "bool TeamLeadPushHeld(", "void DiagnosticTeamEvent(");
			Assert.That(held, Does.Contain("if (TeamLeadGraceHoldOn)\n\t\t\t\torder.LastReportTick = now;\n\n\t\t\torder.HoldPush(now);"));

			Assert.That(wiring, Does.Contain("internal bool TeamLeadGatherHoldOn => TeamLeadOn && Info.TeamLeadGatherHoldEnabled && !ablateGatherHold.Off(Player, AblationIds.TeamLeadGatherHold);"));
			Assert.That(wiring, Does.Contain("bool TeamLeadGraceHoldOn => TeamLeadOn && Info.TeamLeadGraceHoldEnabled && !ablateGraceHold.Off(Player, AblationIds.TeamLeadGraceHold);"));
			Assert.That(wiring, Does.Contain("bool TeamLeadRouteAllOn => TeamLeadOn && Info.TeamLeadRouteAllEnabled && !ablateRouteAll.Off(Player, AblationIds.TeamLeadRouteAll);"));
			Assert.That(wiring, Does.Contain("internal bool TeamLeadRoutesLaunch() => TeamLeadRouteAllOn && TeamLeadOpenOrder() != null;"));
		}

		[Test]
		public void TheLivenessWaveAndTheStalemateBreakerJoinAnOpenOrderAndDefenceStaysExempt()
		{
			var squads = RepoFile(Bots + "SquadManagerBotModuleCA.cs");
			var urgent = Between(squads, "bool TryM13UrgentAttack(", "void ObserveOffensiveWaveActivity(");
			Assert.That(urgent, Does.Contain("if (TeamLeadRoutesLaunch())\n\t\t\t\treturn false;"));
			Assert.That(urgent.IndexOf("TeamLeadRoutesLaunch()", StringComparison.Ordinal), Is.LessThan(urgent.IndexOf("BlobAttackGateHolds(", StringComparison.Ordinal)));
			Assert.That(urgent.IndexOf("TeamLeadRoutesLaunch()", StringComparison.Ordinal), Is.LessThan(urgent.IndexOf("RegisterNewSquad(", StringComparison.Ordinal)), "before any wave is formed");

			var stalemate = RepoFile(Bots + "SquadManagerBotModuleCA.Stalemate.cs");
			var admit = Between(stalemate, "if (TeamLeadRoutesLaunch())", "StalemateCommit(bot, seat, t, admitted, pool, n0, v0);");
			Assert.That(admit, Does.Contain("DiagnosticJobEnd(t, \"hold\", \"team_order\""));
			Assert.That(admit.IndexOf("BlobAttackGateHolds(", StringComparison.Ordinal), Is.GreaterThan(admit.IndexOf("TeamLeadRoutesLaunch()", StringComparison.Ordinal)));

			foreach (var exempt in new[] { "SquadManagerBotModuleCA.DefendAlly.cs" })
				Assert.That(RepoFile(Bots + exempt), Does.Not.Contain("TeamLeadRoutesLaunch"), "defence and ally defence are never routed");
		}

		[Test]
		public void TheThreeSwitchesDefaultOffAreOnInSixPersonalitiesNotClassicAndHaveTheirRows()
		{
			var info = RepoFile(Bots + "SquadManagerBotModuleCAInfo.TeamLead.cs");
			foreach (var line in new[]
			{
				"public readonly bool TeamLeadGatherHoldEnabled = false;", "public readonly int TeamLeadGatherFightCells = 6;", "public readonly int TeamLeadGatherFightMaxTicks = 450;",
				"public readonly bool TeamLeadGraceHoldEnabled = false;", "public readonly bool TeamLeadRouteAllEnabled = false;"
			})
				Assert.That(info, Does.Contain(line));

			var yaml = RepoFile("mods/cameo/ai/ai.yaml");
			var classic = yaml.IndexOf("SquadManagerBotModuleCA@classic:", StringComparison.Ordinal);
			foreach (var key in new[] { "TeamLeadGatherHoldEnabled", "TeamLeadGraceHoldEnabled", "TeamLeadRouteAllEnabled" })
			{
				Assert.That(Regex.Matches(yaml[..classic], "\n\t\t" + key + ": true").Count, Is.EqualTo(6), key);
				Assert.That(yaml[classic..], Does.Not.Contain(key), key + " never in @classic");
			}

			Assert.That(AblationIds.TeamLeadGatherHold, Is.EqualTo("tl_gather_hold"));
			Assert.That(AblationIds.TeamLeadGraceHold, Is.EqualTo("tl_grace_hold"));
			Assert.That(AblationIds.TeamLeadRouteAll, Is.EqualTo("tl_route_all"));
		}
	}
}
