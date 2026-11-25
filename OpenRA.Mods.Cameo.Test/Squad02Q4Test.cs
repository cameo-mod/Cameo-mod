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
	/// CONSOLIDATE-02 Q4 (CONSOLIDATE_02_SQUAD_DESIGN.md 4.1 Step 2; rows sq_launch_arbiter and sq_order_engaged under sq_pipeline_02): the team order is the only ground
	/// launcher and lives Gather -> Push -> Engaged -> Done. The design scenarios flip to expected-new: X1/X11 (the D1 arrived bar stays the one bar), X3 (the blob keeps
	/// one target after the first kill), X4 (no solo leader launch), X5 (the breaker never launches, Opus D4), X12 (after the push only WAVE-MIN groups join, Opus D3).
	/// </summary>
	[TestFixture]
	public class Squad02Q4Test
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

		static TeamLeadOrder Order() => new(1, "a", new WPos(1024, 1024, 0), new WPos(40960, 40960, 0), 77, 1000);

		// X3: the order closed at push (Open(now, 0)), so each seat's squad went its own way after the first kill. A persistent order stays open until Done.
		[Test]
		public void APersistentOrderStaysOpenThroughPushAndEngagedUntilItIsDone()
		{
			var plain = Order();
			plain.PushTick = 2000;
			Assert.That(plain.Open(2001, 0), Is.False, "the old order closes at the push");

			var order = Order();
			order.MakePersistent();
			Assert.That(order.Phase, Is.EqualTo(TeamLeadOrderPhase.Gather));
			Assert.That(order.Open(1500, 0), Is.True);

			order.PushTick = 2000;
			Assert.That(order.Phase, Is.EqualTo(TeamLeadOrderPhase.Push));
			Assert.That(order.Open(9000, 0), Is.True, "open after the push");

			var squad = new object();
			order.ReportEngaged(squad, 14, 9000, 2100, false);
			Assert.That(order.Phase, Is.EqualTo(TeamLeadOrderPhase.Push), "marching is not contact");
			order.ReportEngaged(squad, 13, 8400, 2200, true);
			Assert.That(order.Phase, Is.EqualTo(TeamLeadOrderPhase.Engaged));
			Assert.That(order.EngagedTick, Is.EqualTo(2200));

			order.Close("done_spent");
			Assert.That(order.Phase, Is.EqualTo(TeamLeadOrderPhase.Done));
			Assert.That(order.Open(2300, 0), Is.False);
			Assert.That(order.CancelReason, Is.EqualTo("done_spent"));
		}

		[Test]
		public void TheLeaderRetargetMovesTheWholeBlobAndTheEngagedTotalsForgetSilentSquads()
		{
			var order = Order();
			order.MakePersistent();
			order.PushTick = 2000;
			Assert.That(order.TargetSeq, Is.Zero);

			order.Retarget(new WPos(30720, 30720, 0), 88);
			Assert.That(order.TargetActorId, Is.EqualTo(88u));
			Assert.That(order.TargetSeq, Is.EqualTo(1), "a squad pointing at seq 0 is re-pointed");
			Assert.That(order.Rally, Is.EqualTo(new WPos(1024, 1024, 0)), "the rally and the push stay");
			Assert.That(order.Pushed, Is.True);

			var a = new object();
			var b = new object();
			order.ReportEngaged(a, 10, 6000, 2500, true);
			order.ReportEngaged(b, 4, 2000, 2900, false);
			Assert.That(order.EngagedTotals(3000, 500), Is.EqualTo((14, 8000L)));
			Assert.That(order.EngagedTotals(3100, 500), Is.EqualTo((4, 2000L)), "a squad silent for longer than the grace is gone (dead, merged or dismissed)");
			Assert.That(order.EngagedTotals(9000, 500), Is.EqualTo((0, 0L)), "the blob is spent");
			order.ReportEngaged(a, 0, 0, 9000, false);
			Assert.That(order.EngagedTotals(9000, 500), Is.EqualTo((0, 0L)), "an empty squad counts for nothing");
		}

		// Opus D4: the breaker no longer launches; its trigger is an all-in input.
		[Test]
		public void TheStalemateSignalIsAnAllInInputForAWindow()
		{
			Assert.That(AllInCA.WhyStalemate, Is.EqualTo("stalemate"));
			Assert.That(AllInCA.StalemateSignal(5000, -1, 1500), Is.False, "never raised");
			Assert.That(AllInCA.StalemateSignal(5000, 4000, 1500), Is.True);
			Assert.That(AllInCA.StalemateSignal(5500, 4000, 1500), Is.True, "the edge is inside");
			Assert.That(AllInCA.StalemateSignal(5501, 4000, 1500), Is.False, "stale");
			Assert.That(AllInCA.StalemateSignal(3999, 4000, 1500), Is.False, "from the future");
			Assert.That(AllInCA.Why(false, false, false), Is.Null, "the other triggers stay as they were");
		}

		// Opus ruling: a solo bot with no allies leads itself under the one launcher.
		[Test]
		public void ASoloBotLeadsItself()
		{
			var seats = new[] { new TeamLeadCA.Seat("solo", 10, true) };
			var election = TeamLeadCA.Elect(seats, TeamLeadCA.Election.None, 100, 25, 1000);
			Assert.That(election.Leader, Is.EqualTo("solo"));
			Assert.That(TeamLeadCA.Engaged(seats, election, 2), Is.False, "the old team rule");
			Assert.That(TeamLeadCA.Engaged(seats, election, 1), Is.True, "the arbiter engages a team of one");

			var tl = RepoFile(Bots + "SquadManagerBotModuleCA.TeamLead.cs");
			Assert.That(tl, Does.Contain("if (!teamLeadEngaged && ArmyLaunchArbiterOn)\n\t\t\t\tteamLeadEngaged = TeamLeadCA.Engaged(seats, team.Election, 1);"));
		}

		// D1 stays the one bar (X1 / X11): arrived >= max(WAVE-MIN, 75% of committed), and the timeout pushes at WAVE-MIN; a persistent order below it keeps gathering.
		[Test]
		public void TheArrivedBarIsTheOneBarAndAPersistentOrderBelowItIsNeverDissolved()
		{
			Assert.That(TeamLeadCA.ArrivedBarUnits(12, 60, 75), Is.EqualTo(45));
			Assert.That(TeamLeadCA.ArrivedBarUnits(12, 10, 75), Is.EqualTo(12));

			var tl = RepoFile(Bots + "SquadManagerBotModuleCA.TeamLead.cs");
			var maintain = Between(tl, "void TeamLeadMaintainOrder(", "bool TeamLeadPushHeld(");
			Assert.That(maintain, Does.Contain(": order.Extending && !order.Persistent && TeamLeadCA.PushBarDissolve("), "never dissolved for being small");
			Assert.That(maintain, Does.Contain("if (order.Pushed)\n\t\t\t{\n\t\t\t\tif (order.Persistent)\n\t\t\t\t\tArmyMaintainEngaged(team, order, now);\n\t\t\t\treturn;\n\t\t\t}"));
			Assert.That(tl, Does.Contain("if (ArmyOrderEngagedOn)\n\t\t\t\torder.MakePersistent();"), "only an order issued under the row is persistent");
		}

		// X4 / X12: the one launcher.
		[Test]
		public void NoWaveFormsOutsideTheOpenOrderAndAfterThePushOnlyAWaveMinGroupJoins()
		{
			var tl = RepoFile(Bots + "SquadManagerBotModuleCA.TeamLead.cs");
			var hold = Between(tl, "bool TeamLeadHoldsLaunch(out bool teamLaunch)", "(int Units, long Value) TeamLeadLaunchPool(");
			Assert.That(hold, Does.Contain("if (ArmyLaunchArbiterOn)\n\t\t\t{\n\t\t\t\tteamLaunch = true;\n\t\t\t\treturn !TeamLeadOn || !ArmyMayJoin();\n\t\t\t}"));
			Assert.That(hold.IndexOf("if (ArmyLaunchArbiterOn)", StringComparison.Ordinal), Is.LessThan(hold.IndexOf("if (!TeamLeadOn)", StringComparison.Ordinal)),
				"before the first team check nothing forms");

			var pipe = RepoFile(Bots + "SquadManagerBotModuleCA.Pipeline02.cs");
			var mayJoin = Between(pipe, "bool ArmyMayJoin()", "bool ArmyPrepare(SquadCA s)");
			Assert.That(mayJoin, Does.Contain("if (order == null || World.WorldTick < initialAttackDelay)\n\t\t\t\treturn false;"), "no order, no wave (the leader too)");
			Assert.That(mayJoin, Does.Contain("if (order.Pushed)\n\t\t\t\treturn TeamLeadAllInRejoinGroup();"), "D3: a WAVE-MIN group after the push");

			var prepare = Between(pipe, "bool ArmyPrepare(SquadCA s)", "void ArmyReinforce(");
			Assert.That(prepare, Does.Contain("if (order != null && (!order.Pushed || ArmyWaveMinGroup(s.Units.Count, SquadValueOf(s))))"));
			Assert.That(prepare, Does.Contain("DiagnosticSquadEvent(s, \"team_hold\", order == null ? \"no_order\" : \"below_wave_min\");\n\t\t\tDismissSquad(s);\n\t\t\treturn true;"),
				"X4: a quiet squad with no order goes back to the pool, never to an Idle launch");
			Assert.That(prepare, Does.Not.Contain("TeamLeadTryIssue"), "L19: no order is issued from a squad");

			Assert.That(tl, Does.Contain("if (ArmyLaunchArbiterOn)\n\t\t\t\treturn ArmyPrepare(s);"));
			Assert.That(tl, Does.Contain("if (teamLeadIsLeader && !ArmyLaunchArbiterOn)"), "the leader's abandoned squad is not set Idle");
			Assert.That(tl, Does.Contain("if (order is { Persistent: true, Pushed: true })\n\t\t\t{\n\t\t\t\tArmyReinforce(bot, order, now);\n\t\t\t\treturn;\n\t\t\t}"));

			var reinforce = Between(pipe, "void ArmyReinforce(", "void ArmyMaintainEngaged(");
			Assert.That(reinforce, Does.Contain("if (!TeamLeadAllInRejoinGroup())\n\t\t\t\treturn;"));
		}

		// L06 / L08 / L09 / T09: the other launchers are bypassed under the row.
		[Test]
		public void TheAggTeamLaunchTheBreakerWaveAndTheIdleLaunchAreBypassed()
		{
			var agg = RepoFile(Bots + "SquadManagerBotModuleCA.AggTeam.cs");
			var tick = Between(agg, "void AggTeamTick(IBot bot)", "List<UnitWposWrapper> AggTeamTakeReserve()");
			Assert.That(tick.IndexOf("if (ArmyLaunchArbiterOn)\n\t\t\t\treturn;", StringComparison.Ordinal), Is.LessThan(tick.IndexOf("AggTeamMergeTiny(now);", StringComparison.Ordinal)));
			Assert.That(tick.IndexOf("AggTeamApplyDetachment(bot, defence);", StringComparison.Ordinal), Is.LessThan(tick.IndexOf("if (ArmyLaunchArbiterOn)", StringComparison.Ordinal)),
				"the defence plan (Q3 arbiter) still applies");
			Assert.That(Between(agg, "void AggTeamPrepare(SquadCA s)", "TeamAttackIntent intent;"), Does.Contain("if (ArmyLaunchArbiterOn)\n\t\t\t\treturn;"));

			var stalemate = RepoFile(Bots + "SquadManagerBotModuleCA.Stalemate.cs");
			var hook = stalemate.IndexOf("if (ArmyLaunchArbiterOn)\n\t\t\t{\n\t\t\t\tArmyNoteStalemate(t);", StringComparison.Ordinal);
			Assert.That(hook, Is.GreaterThan(0));
			Assert.That(hook, Is.LessThan(stalemate.IndexOf("StalemateCommit(bot, seat, t, admitted, pool, n0, v0);", StringComparison.Ordinal)), "before the commit: the breaker never launches");
			Assert.That(stalemate, Does.Contain("DiagnosticJobEnd(t, \"hold\", \"all_in_input\""));

			var v07 = RepoFile(Bots + "SquadManagerBotModuleCA.V07.cs");
			Assert.That(v07, Does.Contain("?? (ArmyLaunchArbiterOn && AllInCA.StalemateSignal(now, team.StalemateSignalTick, Info.ArmyStalemateSignalTicks) ? AllInCA.WhyStalemate : null);"));

			var sm = RepoFile(Bots + "SquadManagerBotModuleCA.cs");
			Assert.That(sm, Does.Contain("if (ArmyLaunchArbiterOn && !TeamLeadOn && s.Type == SquadCAType.Rush && AggTeamIsQuietState(s.FuzzyStateMachine?.CurrentState))"));
			Assert.That(sm, Does.Contain("if (ArmyLaunchArbiterOn && teamLaunch)\n\t\t\t\tteamFollowerLaunch = true;"));
		}

		// X3: after the first kill the pushed squads take the order's target, which the leader re-picks; a done order lets them go (no own wandering wave).
		[Test]
		public void PushedSquadsFollowTheLeadersTargetWhileTheOrderLives()
		{
			var tl = RepoFile(Bots + "SquadManagerBotModuleCA.TeamLead.cs");
			Assert.That(tl, Does.Contain("internal bool TeamLeadKeepsGoing(SquadCA s) => ArmyOrderEngagedOn ? ArmyKeepsGoing(s) : TeamLeadBoundedRetargetOn && TeamLeadTeamSquad(s)"));
			var goOn = Between(tl, "internal bool TeamLeadGoOn(SquadCA s, Actor leader)", "var seat = TeamLeadGoOnPlayer();");
			Assert.That(goOn, Does.Contain("if (ArmyOrderEngagedOn && ArmyEngagedOrderOf(s) is { } engaged && ArmyFollowOrder(s, engaged))\n\t\t\t\treturn true;"));
			Assert.That(tl, Does.Contain("squad.TeamPushSeq = order.Seq;\n\t\t\tsquad.TeamTargetSeq = order.TargetSeq;"));

			var pipe = RepoFile(Bots + "SquadManagerBotModuleCA.Pipeline02.cs");
			Assert.That(pipe, Does.Contain("return order != null && order.Persistent && order.Pushed && order.Seq == s.TeamPushSeq ? order : null;"), "a done order has no members");
			var step = Between(pipe, "void ArmyEngagedStep(SquadCA s)", "bool ArmyWaveMinGroup(");
			Assert.That(step, Does.Contain("if (s.TeamTargetSeq != order.TargetSeq && state is GroundUnitsAttackMoveStateCA && ArmyFollowOrder(s, order))"), "a marching squad is re-pointed, a fight finishes first");
			var retarget = Between(pipe, "void ArmyRetargetEngaged(int now)", "void ArmyNoteStalemate(");
			Assert.That(retarget, Does.Contain("order.LeaderKey != teamLeadKey"), "the leader's view only");
			Assert.That(retarget, Does.Contain("ArmyCloseOrder(order, now, \"done_no_target\");"));
		}

		[Test]
		public void TheRowsAndParametersAreRegisteredAndTheMasterSwitchStaysOffInCSharp()
		{
			Assert.That(AblationIds.SqLaunchArbiter, Is.EqualTo("sq_launch_arbiter"));
			Assert.That(AblationIds.SqOrderEngaged, Is.EqualTo("sq_order_engaged"));

			var info = RepoFile(Bots + "SquadManagerBotModuleCAInfo.Pipeline02.cs");
			Assert.That(info, Does.Contain("public readonly bool SquadPipeline02Enabled = false;"));
			Assert.That(info, Does.Contain("public readonly int ArmyEngagedMaxTicks = 6000;"));
			Assert.That(info, Does.Contain("public readonly int ArmyStalemateSignalTicks = 1500;"));

			var pipe = RepoFile(Bots + "SquadManagerBotModuleCA.Pipeline02.cs");
			Assert.That(pipe, Does.Contain("internal bool ArmyLaunchArbiterOn => ArmyPipelineOn && TeamLeadActive && !ablateSqLaunchArbiter.Off(Player, AblationIds.SqLaunchArbiter);"));
			Assert.That(pipe, Does.Contain("internal bool ArmyOrderEngagedOn => ArmyLaunchArbiterOn && !ablateSqOrderEngaged.Off(Player, AblationIds.SqOrderEngaged);"));
		}

		[Test]
		public void TheMasterSwitchIsArmedInTheSixPersonalitiesNeverInClassic()
		{
			var yaml = RepoFile("mods/cameo/ai/ai.yaml");
			var classic = yaml.IndexOf("SquadManagerBotModuleCA@classic:", StringComparison.Ordinal);
			Assert.That(classic, Is.GreaterThan(0));
			Assert.That(Regex.Matches(yaml[..classic], "\n\t\tSquadPipeline02Enabled: true").Count, Is.EqualTo(6));
			Assert.That(yaml[classic..], Does.Not.Contain("SquadPipeline02"), "never in @classic");
		}
	}
}
