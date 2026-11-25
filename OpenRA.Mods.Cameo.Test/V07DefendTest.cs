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
	/// V07 TEAM-DEFEND, unit side (tl_defend_team; USER master replay 2026-11-24, via Opus): a serious attack on an own or allied actor raises one team alarm, every other
	/// teammate sends pool, idle and home-guard units, a pushed blob keeps its order unless the ally would die. One switch (release, default off, ON in the six personalities,
	/// never @classic) and one DEV row. The pure rules are tested here, the wiring by pins.
	/// </summary>
	[TestFixture]
	public class V07DefendTest
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

		static TeamDefendCA.Alarm AlarmOf(int tick, string victim, long attack, long defence = 0) =>
			new(tick, victim, 11, new WPos(1000, 1000, 0), 22, new WPos(2000, 2000, 0), attack, defence);

		[Test]
		public void AnAttackIsSeriousWhenTheEnemiesAroundTheVictimCostEnough()
		{
			Assert.That(TeamDefendCA.Serious(599, 600), Is.False, "a lone scout is not an alarm");
			Assert.That(TeamDefendCA.Serious(600, 600), Is.True);
			Assert.That(TeamDefendCA.Serious(2400, 600), Is.True);
			Assert.That(TeamDefendCA.Serious(0, 0), Is.False, "no armed enemy, no alarm, whatever the minimum");
			Assert.That(TeamDefendCA.Serious(1, 0), Is.True, "a minimum below one reads as one");
		}

		[Test]
		public void TheAllyWouldDieWhenTheAttackIsWorthMoreThanItsDefence()
		{
			Assert.That(TeamDefendCA.WouldDie(1001, 1000, 100), Is.True);
			Assert.That(TeamDefendCA.WouldDie(1000, 1000, 100), Is.False, "equal value: the ally holds");
			Assert.That(TeamDefendCA.WouldDie(500, 0, 100), Is.True, "no defence at all");
			Assert.That(TeamDefendCA.WouldDie(0, 0, 100), Is.False);
			Assert.That(TeamDefendCA.WouldDie(251, 1000, 25), Is.True, "the percent is read: 25 percent of 1000 = 250");
			Assert.That(TeamDefendCA.WouldDie(250, 1000, 25), Is.False);
			Assert.That(TeamDefendCA.WouldDie(251, 1000, 1), Is.True, "the percent never goes below 25");
			Assert.That(TeamDefendCA.WouldDie(3999, 1000, 9999), Is.False, "nor above 400");
			Assert.That(TeamDefendCA.WouldDie(4001, 1000, 9999), Is.True);
		}

		[Test]
		public void TheAlarmIsInForceForItsTtlAndTheBiggerAttackOrTheSameSeatTakesTheBoard()
		{
			Assert.That(TeamDefendCA.Active(1000, 1000, 450), Is.True);
			Assert.That(TeamDefendCA.Active(1450, 1000, 450), Is.True, "the last tick of the ttl");
			Assert.That(TeamDefendCA.Active(1451, 1000, 450), Is.False);
			Assert.That(TeamDefendCA.Active(999, 1000, 450), Is.False, "an alarm from the future is not in force");
			Assert.That(TeamDefendCA.Active(1001, 1000, 0), Is.True, "a ttl below one reads as one");

			var held = AlarmOf(1000, "a", 1000);
			Assert.That(TeamDefendCA.Replaces(null, AlarmOf(1100, "b", 100), 1100, 450), Is.True, "no alarm yet");
			Assert.That(TeamDefendCA.Replaces(held, AlarmOf(1100, "a", 300), 1100, 450), Is.True, "the same seat: a refresh with the newest numbers, even a smaller attack");
			Assert.That(TeamDefendCA.Replaces(held, AlarmOf(1100, "b", 1001), 1100, 450), Is.True, "another seat with the bigger attack");
			Assert.That(TeamDefendCA.Replaces(held, AlarmOf(1100, "b", 1000), 1100, 450), Is.False, "a tie keeps the alarm in force");
			Assert.That(TeamDefendCA.Replaces(held, AlarmOf(1100, "b", 10), 1100, 450), Is.False, "a smaller attack elsewhere does not take the board");
			Assert.That(TeamDefendCA.Replaces(held, AlarmOf(1500, "b", 10), 1500, 450), Is.True, "the old alarm ran out");
		}

		[Test]
		public void AFreeSquadAlwaysAnswersAndAPushedBlobOnlyWhenTheAllyWouldDie()
		{
			Assert.That(TeamDefendCA.SquadResponds(true, false, false), Is.True, "an idle, staging or gathering squad answers");
			Assert.That(TeamDefendCA.SquadResponds(true, false, true), Is.True);
			Assert.That(TeamDefendCA.SquadResponds(false, true, false), Is.False, "a pushed blob keeps its order");
			Assert.That(TeamDefendCA.SquadResponds(false, true, true), Is.True, "unless the ally would die");
			Assert.That(TeamDefendCA.SquadResponds(false, false, true), Is.False, "a squad that is marching or fighting keeps its job");
			Assert.That(TeamDefendCA.SquadResponds(false, false, false), Is.False);
		}

		[Test]
		public void TheAlarmLivesOnTheSharedTeamBoardAndTheSoloBotRaisesNone()
		{
			var team = new TeamLeadTeam();
			Assert.That(team.DefendAlarm, Is.Null, "no alarm until a bot raises one");
			team.DefendAlarm = AlarmOf(300, "ally", 1500, 400);
			Assert.That(team.DefendAlarm!.Value.VictimKey, Is.EqualTo("ally"));
			Assert.That(team.DefendAlarm!.Value.AttackerId, Is.EqualTo(22u));
			Assert.That(TeamDefendCA.WouldDie(team.DefendAlarm.Value.AttackValue, team.DefendAlarm.Value.DefenceValue, 100), Is.True);

			// The solo bot (a 1v1, the last survivor) has nobody to call: both raise paths stand down below two living seats.
			var module = RepoFile(Bots + "SquadManagerBotModuleCA.V07Defend.cs");
			Assert.That(Regex.Matches(module, "teamLeadAliveSeats < 2").Count, Is.EqualTo(2), "scan and own-asset raise");
		}

		[Test]
		public void TeamDefendIsOffByDefaultOnInTheSixPersonalitiesAndNeverInClassic()
		{
			var info = RepoFile(Bots + "SquadManagerBotModuleCAInfo.V07Defend.cs");
			Assert.That(info, Does.Contain("public readonly bool TeamLeadDefendTeamEnabled = false;"));
			Assert.That(info, Does.Contain("public readonly int TeamLeadDefendTeamMinAttackValue = 600;"));
			Assert.That(info, Does.Contain("public readonly int TeamLeadDefendTeamAlarmTicks = 450;"));
			Assert.That(info, Does.Contain("public readonly int TeamLeadDefendTeamSendTicks = 50;"));
			Assert.That(info, Does.Contain("public readonly int TeamLeadDefendTeamRaiseTicks = 50;"));
			Assert.That(info, Does.Contain("public readonly int TeamLeadDefendTeamDiePercent = 100;"));
			Assert.That(AblationIds.TeamLeadDefendTeam, Is.EqualTo("tl_defend_team"));

			var yaml = RepoFile("mods/cameo/ai/ai.yaml");
			var classic = yaml.IndexOf("SquadManagerBotModuleCA@classic:", StringComparison.Ordinal);
			Assert.That(classic, Is.GreaterThan(0));
			Assert.That(Regex.Matches(yaml[..classic], "\n\t\tTeamLeadDefendTeamEnabled: true").Count, Is.EqualTo(6));
			Assert.That(yaml[classic..], Does.Not.Contain("TeamLeadDefendTeam"), "never in @classic");
		}

		[Test]
		public void TeamDefendIsWiredBehindItsOwnSwitchAndLogsTheAlarmAndTheSend()
		{
			var module = RepoFile(Bots + "SquadManagerBotModuleCA.V07Defend.cs");
			Assert.That(module, Does.Contain("internal bool TeamDefendOn => TeamLeadOn && Info.TeamLeadDefendTeamEnabled && !ablateDefendTeam.Off(Player, AblationIds.TeamLeadDefendTeam);"));
			Assert.That(module, Does.Contain("if (!TeamDefendOn || teamLeadTeam?.DefendAlarm is not { } alarm)\n\t\t\t\treturn;"), "free of work with the switch off");
			Assert.That(module, Does.Contain("alarm.VictimKey == teamLeadKey"), "the victim's own bot keeps its own defence");
			Assert.That(module, Does.Contain("TeamDefendCA.SquadResponds(free, blob, wouldDie)"));
			Assert.That(module, Does.Contain("s.TeamPushSeq > 0 && s.FuzzyStateMachine?.CurrentState is GroundUnitsAttackMoveStateCA"), "a pushed blob is a Rush squad of a pushed order in the attack-move state");
			Assert.That(module, Does.Contain("var free = DefendAllyFreeSquad(s);"), "the free squads of DEFEND-ALLY-01 (an order's gatherers keep their hold)");
			Assert.That(module, Does.Contain("IsDefenderDraftable(u, leases)"), "leases are honoured");
			Assert.That(module, Does.Contain("home.Target.CenterPosition"), "a home fight keeps the units");
			Assert.That(module, Does.Contain("\"team_defend_alarm\""));
			Assert.That(module, Does.Contain("\"team_defend_send\""));
			Assert.That(module, Does.Contain("ReconcileSquadLeases();"), "the draft is claimed at once");

			Assert.That(RepoFile(Bots + "SquadManagerBotModuleCA.DefendAlly.cs"), Does.Contain("TeamDefendScan(now);\n\n\t\t\tif (defendAllyAttacked.Count == 0 || now < defendAllyNextResponse)"),
				"the alarm look comes before the response cooldown");
			var squad = RepoFile(Bots + "SquadManagerBotModuleCA.cs");
			Assert.That(squad, Does.Contain("DefendAllyTick(bot);\n\n\t\t\t// V07 tl_defend_team: every teammate answers the team alarm of an attacked ally (free of work with the switch off).\n\t\t\tTeamDefendTick(bot);"));
			Assert.That(squad, Does.Contain("ProtectOwn(e.Attacker);\n\n\t\t\t\t// V07 tl_defend_team"), "the victim's own side raises the alarm too");
			Assert.That(squad, Does.Contain("TeamDefendRaiseOwn(self);"));
			Assert.That(RepoFile(Bots + "BotModuleLogic/TeamLeadCA.cs"), Does.Contain("public TeamDefendCA.Alarm? DefendAlarm;"));
		}
	}
}
