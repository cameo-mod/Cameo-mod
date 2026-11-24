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
using System.Text.RegularExpressions;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	/// <summary>TEAM-LEAD-01 (USER rule, 2026-11-23): election, order lifecycle and the wiring pins of the team-leader layer.</summary>
	[TestFixture]
	public class TeamLeadTest
	{
		static TeamLeadCA.Seat Seat(string key, long score, bool alive = true) => new(key, score, alive);

		// --- election ----------------------------------------------------------------------------------------------------------------

		[Test]
		public void TheFirstLeaderIsTheHighestScoreAndATieGoesToTheLowerKey()
		{
			var seats = new[] { Seat("b", 50), Seat("a", 80), Seat("c", 80) };
			var e = TeamLeadCA.Elect(seats, TeamLeadCA.Election.None, 100, 15, 1500);

			Assert.That(e.Leader, Is.EqualTo("a"));
			Assert.That(e.SinceTick, Is.EqualTo(100));

			var zero = TeamLeadCA.Elect(new[] { Seat("y", 0), Seat("x", 0) }, TeamLeadCA.Election.None, 5, 15, 1500);
			Assert.That(zero.Leader, Is.EqualTo("x"), "all scores zero: the stable lowest key leads");
		}

		[Test]
		public void ADeadOrMissingLeaderIsReplacedAtOnceInsideTheHold()
		{
			var current = new TeamLeadCA.Election("a", 1000);
			var dead = new[] { Seat("a", 900, alive: false), Seat("b", 40), Seat("c", 60) };
			var e = TeamLeadCA.Elect(dead, current, 1010, 15, 1500);
			Assert.That(e, Is.EqualTo(new TeamLeadCA.Election("c", 1010)), "10 ticks after the election, but the leader is dead");

			var gone = TeamLeadCA.Elect(new[] { Seat("b", 40), Seat("c", 60) }, current, 1010, 15, 1500);
			Assert.That(gone.Leader, Is.EqualTo("c"), "a leader no longer among the seats counts as gone");

			Assert.That(TeamLeadCA.Elect(new[] { Seat("a", 5, alive: false) }, current, 2000, 15, 1500), Is.EqualTo(TeamLeadCA.Election.None));
		}

		[Test]
		public void AChallengerNeedsTheHoldToHaveEndedAndAFifteenPercentLead()
		{
			var current = new TeamLeadCA.Election("a", 1000);

			// Inside the hold nobody takes the post, however big the lead.
			Assert.That(TeamLeadCA.Elect(new[] { Seat("a", 100), Seat("b", 1000) }, current, 2499, 15, 1500).Leader, Is.EqualTo("a"));

			// After the hold: 114 does not beat 100 by 15 percent, 115 does.
			Assert.That(TeamLeadCA.Elect(new[] { Seat("a", 100), Seat("b", 114) }, current, 2500, 15, 1500).Leader, Is.EqualTo("a"));
			var swapped = TeamLeadCA.Elect(new[] { Seat("a", 100), Seat("b", 115) }, current, 2500, 15, 1500);
			Assert.That(swapped, Is.EqualTo(new TeamLeadCA.Election("b", 2500)), "the new leader starts its own hold");

			// A tie at the top never moves the post, whatever the keys.
			Assert.That(TeamLeadCA.Elect(new[] { Seat("a", 100), Seat("0", 100) }, current, 9000, 15, 1500).Leader, Is.EqualTo("a"));

			// A leader at zero score gives way to any positive challenger once the hold is over.
			Assert.That(TeamLeadCA.Elect(new[] { Seat("a", 0), Seat("b", 1) }, current, 2500, 15, 1500).Leader, Is.EqualTo("b"));
		}

		[Test]
		public void TheTeamKeyIsTheLowestKeyOfAllSeatsEvenDeadOnesSoADeathNeverRenamesTheTeam()
		{
			Assert.That(TeamLeadCA.TeamKey(new[] { Seat("m", 1), Seat("c", 1, alive: false), Seat("q", 1) }), Is.EqualTo("c"));
			Assert.That(TeamLeadCA.TeamKey(Array.Empty<TeamLeadCA.Seat>()), Is.Null);
		}

		[Test]
		public void ATeamIsEngagedOnlyWithALeaderAndTwoLivingBots()
		{
			var election = new TeamLeadCA.Election("a", 0);
			Assert.That(TeamLeadCA.Engaged(new[] { Seat("a", 1), Seat("b", 1) }, election), Is.True);
			Assert.That(TeamLeadCA.Engaged(new[] { Seat("a", 1), Seat("b", 1, alive: false) }, election), Is.False, "one living bot has nobody to command");
			Assert.That(TeamLeadCA.Engaged(new[] { Seat("a", 1) }, election), Is.False, "a 1v1 seat plays as before");
			Assert.That(TeamLeadCA.Engaged(new[] { Seat("a", 1), Seat("b", 1) }, TeamLeadCA.Election.None), Is.False);
		}

		// --- the gather, the push and the launch rules --------------------------------------------------------------------------------

		[Test]
		public void TheBlobPushesEarlyOnlyAfterTheMinimumAndWithEnoughOfItAtTheRallyPoint()
		{
			// issued at 1000, window 300..1500, 70 percent
			Assert.That(TeamLeadCA.PushDue(1200, 1000, 300, 1500, 1000, 1000, 70), Is.False, "before the minimum nothing pushes");
			Assert.That(TeamLeadCA.PushDue(1300, 1000, 300, 1500, 1000, 690, 70), Is.False, "69 percent has not arrived");
			Assert.That(TeamLeadCA.PushDue(1300, 1000, 300, 1500, 1000, 700, 70), Is.True);
			Assert.That(TeamLeadCA.PushDue(1400, 1000, 300, 1500, 0, 0, 70), Is.False, "nothing committed yet");
			Assert.That(TeamLeadCA.PushDue(2500, 1000, 300, 1500, 1000, 0, 70), Is.True, "the window is over, whoever is on the way");
		}

		[Test]
		public void TheRallyPointLiesForwardOfTheLeaderAndNeverPastTheTarget()
		{
			var from = new WPos(0, 0, 0);
			var rally = TeamLeadCA.RallyPoint(from, new WPos(100 * 1024, 0, 0), 12);
			Assert.That(rally.X, Is.EqualTo(12 * 1024));
			Assert.That(rally.Y, Is.EqualTo(0));

			var near = TeamLeadCA.RallyPoint(from, new WPos(5 * 1024, 0, 0), 12);
			Assert.That(near, Is.EqualTo(from), "a target closer than the forward distance leaves the rally at the leader");
			Assert.That(TeamLeadCA.RallyPoint(from, from, 12), Is.EqualTo(from));
		}

		[Test]
		public void AFollowerFormsAWaveOnlyForAnOpenOrderAndTheLeaderOnlyWithAContribution()
		{
			Assert.That(TeamLeadCA.FollowerMayLaunch(orderOpen: false, poolUnits: 30, minUnits: 3), Is.False, "no solo attack, however big the pool");
			Assert.That(TeamLeadCA.FollowerMayLaunch(orderOpen: true, poolUnits: 2, minUnits: 3), Is.False);
			Assert.That(TeamLeadCA.FollowerMayLaunch(orderOpen: true, poolUnits: 3, minUnits: 3), Is.True);
			Assert.That(TeamLeadCA.LeaderMayLaunch(2, 3), Is.False, "never an empty team wave");
			Assert.That(TeamLeadCA.LeaderMayLaunch(3, 3), Is.True);
		}

		[Test]
		public void TheCombinedBlobCountsOnlyFreshPublicationsOfEverySeat()
		{
			var pools = new[] { (Units: 10, Value: 5000L, Tick: 900), (Units: 4, Value: 1200L, Tick: 950), (Units: 99, Value: 99999L, Tick: 200) };
			var (units, value) = TeamLeadCA.TeamPool(pools, 1000, 300);

			Assert.That(units, Is.EqualTo(14));
			Assert.That(value, Is.EqualTo(6200));
			Assert.That(WaveMinCA.Passes(units, value, 12, 6000), Is.True, "the leader's tier (hard 12 / 6000) is met by the team, not by one pool");
			Assert.That(WaveMinCA.Passes(10, 5000, 12, 6000), Is.False);
		}

		// --- the order -----------------------------------------------------------------------------------------------------------------

		[Test]
		public void AnOrderStaysOpenWhileGatheringAndForTheLateJoinWindowAfterThePushNotWhenCancelled()
		{
			var order = new TeamLeadOrder(1, "a", new WPos(1024, 0, 0), new WPos(9 * 1024, 0, 0), 77, 1000);
			Assert.That(order.Open(1100, 600), Is.True);
			Assert.That(order.Pushed, Is.False);

			order.PushTick = 2000;
			Assert.That(order.Pushed, Is.True);
			Assert.That(order.Open(2600, 600), Is.True, "late joiners may still be formed");
			Assert.That(order.Open(2601, 600), Is.False);

			order.Cancelled = true;
			Assert.That(order.Open(2001, 600), Is.False);
		}

		[Test]
		public void TheOrderTotalsTheCommittedAndTheArrivedValueAndForgetsSquadsThatLeft()
		{
			var order = new TeamLeadOrder(1, "a", default, default, 0, 0);
			var s1 = new object();
			var s2 = new object();
			var s3 = new object();
			order.Report(s1, 600, arrived: true);
			order.Report(s2, 300, arrived: false);
			order.Report(s3, 100, arrived: true);

			Assert.That(order.Totals(), Is.EqualTo((Committed: 1000L, Arrived: 700L)));

			order.Report(s2, 300, arrived: true);
			Assert.That(order.Totals().Arrived, Is.EqualTo(1000), "a repeated report replaces the old one");

			order.Forget(s1);
			order.Prune(o => ReferenceEquals(o, s2));
			Assert.That(order.ReportCount, Is.EqualTo(1));
			Assert.That(order.Totals(), Is.EqualTo((Committed: 300L, Arrived: 300L)));
		}

		[Test]
		public void TheBoardGivesOneStatePerTeamAndNumbersTheOrders()
		{
			var team = new TeamLeadTeam();
			team.Publish("a", 6, 3000, 100);
			team.Publish("b", 5, 2000, 120);
			Assert.That(team.Pool(150, 300), Is.EqualTo((11, 5000L)));
			Assert.That(team.Pool(430, 300), Is.EqualTo((0, 0L)), "stale publications drop out");

			var first = team.Issue("a", default, default, 0, 130);
			var second = team.Issue("a", default, default, 0, 900);
			Assert.That(second.Seq, Is.EqualTo(first.Seq + 1));
			Assert.That(team.Order, Is.SameAs(second));
		}

		// --- wiring pins ---------------------------------------------------------------------------------------------------------------

		static string RepoFile(string relative)
		{
			var dir = new DirectoryInfo(AppContext.BaseDirectory);
			while (dir != null && !File.Exists(Path.Combine(dir.FullName, "mod.config")))
				dir = dir.Parent;

			Assert.That(dir, Is.Not.Null, "repository root (mod.config) not found above " + AppContext.BaseDirectory);
			return File.ReadAllText(Path.Combine(dir.FullName, relative));
		}

		const string Bots = "OpenRA.Mods.CA/Traits/BotModules/";

		[Test]
		public void TheSwitchDefaultsOffAndIsOnInSixPersonalitiesNotClassic()
		{
			var info = RepoFile(Bots + "SquadManagerBotModuleCAInfo.TeamLead.cs");
			Assert.That(info, Does.Contain("public readonly bool TeamLeadEnabled = false;"));
			Assert.That(info, Does.Contain("public readonly int TeamLeadCheckTicks = 100;"), "the only periodic work runs every 100 ticks");
			Assert.That(info, Does.Contain("public readonly int TeamLeadLeadPercent = 15;"));
			Assert.That(info, Does.Contain("public readonly int TeamLeadHoldTicks = 1500;"));

			var yaml = RepoFile("mods/cameo/ai/ai.yaml");
			var classic = yaml.IndexOf("SquadManagerBotModuleCA@classic:", StringComparison.Ordinal);
			Assert.That(classic, Is.GreaterThan(0));
			Assert.That(Regex.Matches(yaml[..classic], @"\n\t\tTeamLeadEnabled: true").Count, Is.EqualTo(6));
			Assert.That(yaml[classic..], Does.Not.Contain("TeamLeadEnabled"), "@classic keeps the default (off), byte-identical");

			Assert.That(RepoFile(Bots + "BotModuleLogic/AblationIds.TeamLead.cs"), Does.Contain("public const string TeamLead = \"tl_team_lead\";"));
		}

		[Test]
		public void TheFeatureNeedsAggTeamAndTheRowAndOnlyTheEngagedTeamChangesAnything()
		{
			var wiring = RepoFile(Bots + "SquadManagerBotModuleCA.TeamLead.cs");
			Assert.That(wiring, Does.Contain("Info.TeamLeadEnabled && Info.AggTeamEnabled && !ablateTeamLead.Off(Player, AblationIds.TeamLead)"));
			Assert.That(wiring, Does.Contain("TeamLeadCA.Engaged(seats, team.Election, TeamLeadSoloOn ? 1 : 2)"), "a team of one bot plays as before (two living bots needed) unless the tl_solo_lead switch is on");
			Assert.That(wiring, Does.Contain("BotPeriod.CarryDue(ref teamLeadNextCheck, now, Info.TeamLeadCheckTicks)"), "at most one pass per check interval");
			Assert.That(wiring, Does.Contain("m.TeamLeadActive"), "only bots that can take orders hold a seat");
			Assert.That(wiring, Does.Not.Contain("IssueOrder"), "orders go through the squads and bot.QueueOrder only");
		}

		[Test]
		public void TheHooksSitInFrontOfTheLaunchAndTheSquadPassAndNowhereElse()
		{
			var manager = RepoFile(Bots + "SquadManagerBotModuleCA.cs");
			Assert.That(manager, Does.Contain("if (Info.TeamLeadEnabled)\n\t\t\t\tTeamLeadTick(bot);").Or.Contain("if (Info.TeamLeadEnabled)\r\n\t\t\t\tTeamLeadTick(bot);"));

			var create = manager.IndexOf("void CreateAttackForce(IBot bot, bool aggForce = false)", StringComparison.Ordinal);
			Assert.That(create, Is.GreaterThan(0));
			var body = manager[create..];
			Assert.That(body.IndexOf("TeamLeadHoldsLaunch(out teamLaunch)", StringComparison.Ordinal), Is.InRange(1, 400),
				"the hold is the first thing CreateAttackForce does");
			Assert.That(body, Does.Contain("if (!teamFollowerLaunch && WaveBelowMinimum())"), "a follower's wave is part of the leader's order");
			Assert.That(body, Does.Contain("if (!teamLaunch && WaveMixHolds())"));
			Assert.That(body, Does.Contain("if (!teamLaunch && !StagedCompositionReady())"));

			Assert.That(Regex.IsMatch(manager, @"if \(TeamLeadOn\)\s*\{\s*if \(TeamLeadPrepare\(s\)\)[\s\S]{0,200}else if \(Info\.AggTeamEnabled\)\s*AggTeamPrepare\(s\);"),
				"the team layer replaces the voluntary join only while engaged");

			Assert.That(RepoFile(Bots + "SquadManagerBotModuleCA.WaveMin.cs"), Does.Contain("TeamLeadLaunchPool(unitsHangingAroundTheBase.Count, value)"),
				"WAVE-MIN measures the combined blob for the leader");

			Assert.That(RepoFile(Bots + "SquadManagerBotModuleCA.DefendAlly.cs"), Does.Contain("or TeamLeadGatherStateCA"),
				"defence and ally help may draft units from a gathering squad");
		}

		[Test]
		public void TheGatherStateWalksWithMoveNotAttackMoveAndFightsOnlyOnContact()
		{
			var state = RepoFile(Bots + "Squads/States/TeamLeadGatherStateCA.cs");
			Assert.That(state, Does.Contain("new Order(\"Move\", u.Actor"));
			Assert.That(state, Does.Not.Contain("new Order(\"AttackMove\""), "the army walks to the rally point, it does not fight its way there");
			Assert.That(state, Does.Contain("owner.Bot.QueueOrder("));
			Assert.That(state, Does.Contain("if (!order.Pushed)\n\t\t\t\treturn;").Or.Contain("if (!order.Pushed)\r\n\t\t\t\treturn;"), "nothing attacks before the push");
			Assert.That(state, Does.Contain("order.Forget(owner)"));
		}

		// --- no stall: the leader's hold and its order use one bar, and a leader that cannot lead hands over (Code Monkey FIX) ---------------

		[Test]
		public void TheLeaderFormsTheTeamWaveWithOneIdleUnitOfItsOwn()
		{
			Assert.That(TeamLeadCA.LeaderMayLaunch(1, 1), Is.True, "the launch bars read the team pool; the leader only has to bring one unit");
			Assert.That(TeamLeadCA.LeaderMayLaunch(0, 1), Is.False, "never an empty squad");
			Assert.That(TeamLeadCA.LeaderMayLaunch(0, 0), Is.False, "a zero bar still needs one unit");
		}

		[Test]
		public void AStalledLeaderHandsOverToTheLargestPoolButOnlyToOneThatHoldsMoreThanItself()
		{
			Assert.That(TeamLeadCA.Stalled(4500, 0, 4500), Is.True);
			Assert.That(TeamLeadCA.Stalled(4499, 0, 4500), Is.False);

			var seats = new[] { Seat("a", 900), Seat("b", 100), Seat("c", 50), Seat("d", 10, alive: false) };
			var current = new TeamLeadCA.Election("a", 0);
			var pools = new Dictionary<string, int> { ["a"] = 0, ["b"] = 12, ["c"] = 20, ["d"] = 99 };

			Assert.That(TeamLeadCA.Handover(seats, current, k => pools[k], 4500), Is.EqualTo(new TeamLeadCA.Election("c", 4500)),
				"the largest pool leads; a dead seat never does");

			pools["c"] = 12;
			Assert.That(TeamLeadCA.Handover(seats, current, k => pools[k], 4500).Leader, Is.EqualTo("b"), "a tie goes to the lower key");

			pools["a"] = 12;
			Assert.That(TeamLeadCA.Handover(seats, current, k => pools[k], 4500), Is.EqualTo(current), "nobody holds more than the leader: it stays");

			Assert.That(TeamLeadCA.Handover(new[] { Seat("a", 1) }, current, k => 0, 4500), Is.EqualTo(current), "no other seat");
		}

		[Test]
		public void TheLeaderHoldAndTheOrderIssueUseTheSameBarAndTheStallHandoverIsWired()
		{
			var wiring = RepoFile(Bots + "SquadManagerBotModuleCA.TeamLead.cs");
			Assert.That(wiring, Does.Contain("TeamLeadCA.LeaderMayLaunch(unitsHangingAroundTheBase.Count, Info.TeamLeadLeaderMinUnits)"));
			Assert.That(wiring, Does.Contain("s.Units.Count < Info.TeamLeadLeaderMinUnits"), "an order can always be issued by a leader that may launch");
			Assert.That(wiring, Does.Contain("TeamLeadCA.Handover(seats, team.Election"));
			Assert.That(wiring, Does.Contain("initialAttackDelay"), "the stall clock starts with the first attack tick");

			var info = RepoFile(Bots + "SquadManagerBotModuleCAInfo.TeamLead.cs");
			Assert.That(info, Does.Contain("public readonly int TeamLeadLeaderMinUnits = 1;"));
			Assert.That(info, Does.Contain("public readonly int TeamLeadStallTicks = 4500;"));
		}

		// --- joint guerrilla / harass group (second commit) -----------------------------------------------------------------------------

		[Test]
		public void TheJointHarassTargetLivesForTheWindowAndNeverBeforeItIsPublished()
		{
			Assert.That(TeamLeadCA.HarassLive(100, -1, 1500), Is.False, "never published");
			Assert.That(TeamLeadCA.HarassLive(1600, 100, 1500), Is.True, "exactly at the end of the window");
			Assert.That(TeamLeadCA.HarassLive(1601, 100, 1500), Is.False, "expired: the next squad without a target publishes a new one");

			var team = new TeamLeadTeam();
			Assert.That(team.HarassTargetId, Is.EqualTo(0u));
			Assert.That(team.HarassTick, Is.EqualTo(-1));
		}

		[Test]
		public void TheJointHarassGroupHasItsOwnRowAndSitsBehindTheTeamSwitch()
		{
			Assert.That(AblationIds.TeamLeadJointHarass, Is.EqualTo("tl_joint_harass"));
			Assert.That(AblationIds.TeamLeadJointHarass, Is.Not.EqualTo(AblationIds.TeamLead));

			var wiring = RepoFile(Bots + "SquadManagerBotModuleCA.TeamLead.cs");
			Assert.That(wiring, Does.Contain("TeamLeadOn && !ablateJointHarass.Off(Player, AblationIds.TeamLeadJointHarass)"));
			Assert.That(wiring, Does.Contain("s.Type is SquadCAType.Guerrilla or SquadCAType.Harass && TeamLeadJointHarassOn"));
			Assert.That(wiring, Does.Contain("s.TargetActor = shared;"), "the squad takes the shared target and plays on as before");
			Assert.That(wiring, Does.Not.Contain("new Order(\"AttackMove\""), "the harass group is steered by its target, not by extra orders");

			Assert.That(RepoFile(Bots + "SquadManagerBotModuleCAInfo.TeamLead.cs"), Does.Contain("public readonly int TeamLeadHarassTicks = 1500;"));
		}

		[Test]
		public void TheLiveFilesNeverLeakEnemyStateIntoTheTeamBoard()
		{
			var board = RepoFile(Bots + "BotModuleLogic/TeamLeadCA.cs");
			Assert.That(board, Does.Not.Contain("Actor "), "the board holds positions, ids, values and keys only");
			Assert.That(new[] { "FrozenActor", "World.Actors", "FindActorsInCircle" }.Any(board.Contains), Is.False);
		}
	}
}
