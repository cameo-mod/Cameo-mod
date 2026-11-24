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
	/// TEAM-LEAD-02 commit 2 (tl_finish_weakest; USER P0, V01 timeout RCA 2026-11-24): after the late-escalation start the team's push target and its
	/// joint harass target are the WEAKEST known enemy seat - the fewest known buildings - and stay on it until it is dead. V01: seat 9 sat at 1-3
	/// buildings for 35000 ticks and was never killed.
	/// </summary>
	[TestFixture]
	public class TeamLead02FinishWeakestTest
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

		static IReadOnlyList<(int Seat, int Buildings)> Seats(params (int, int)[] seats) => seats;

		[Test]
		public void TheWeakestKnownSeatIsTheOneWithTheFewestKnownBuildingsAndATieGoesToTheLowerSeat()
		{
			Assert.That(TeamLeadCA.PickWeakest(Seats((9, 2), (4, 11), (7, 6)), -1, -1, 25000, 12000), Is.EqualTo(9));
			Assert.That(TeamLeadCA.PickWeakest(Seats((9, 3), (4, 3), (7, 6)), -1, -1, 25000, 12000), Is.EqualTo(4), "a tie goes to the lower seat id");
			Assert.That(TeamLeadCA.PickWeakest(Seats((9, 0), (4, 11)), -1, -1, 25000, 12000), Is.EqualTo(4), "a seat with no known building cannot be targeted");
			Assert.That(TeamLeadCA.PickWeakest(Seats((9, 0), (4, 0)), -1, -1, 25000, 12000), Is.EqualTo(-1), "nothing known = nothing to finish");
			Assert.That(TeamLeadCA.PickWeakest(Seats(), -1, -1, 25000, 12000), Is.EqualTo(-1));
		}

		[Test]
		public void TheTeamFinishesTheFocusSeatBeforeItSwitchesEvenWhenAnotherBecomesWeaker()
		{
			// Focus on seat 7 (6 buildings); seat 4 drops to 2: the team stays on 7 until it is dead or out of known buildings.
			Assert.That(TeamLeadCA.PickWeakest(Seats((9, 5), (4, 2), (7, 6)), 7, 25000, 26000, 12000), Is.EqualTo(7));
			Assert.That(TeamLeadCA.PickWeakest(Seats((9, 5), (4, 2), (7, 1)), 7, 25000, 30000, 12000), Is.EqualTo(7), "still weakest, still the focus");

			// The focus seat died (it is no longer listed): the next weakest is taken.
			Assert.That(TeamLeadCA.PickWeakest(Seats((9, 5), (4, 2)), 7, 25000, 31000, 12000), Is.EqualTo(4));

			// The focus seat has no known building left (all remembered ones were seen destroyed): same.
			Assert.That(TeamLeadCA.PickWeakest(Seats((9, 5), (4, 2), (7, 0)), 7, 25000, 31000, 12000), Is.EqualTo(4));
		}

		[Test]
		public void AFocusHeldTooLongWithoutAKillRotatesToTheNextWeakestOnceAndStaysIfItIsTheOnlyOne()
		{
			Assert.That(TeamLeadCA.PickWeakest(Seats((9, 5), (4, 2), (7, 6)), 7, 25000, 36999, 12000), Is.EqualTo(7), "one tick before the limit");
			Assert.That(TeamLeadCA.PickWeakest(Seats((9, 5), (4, 2), (7, 6)), 7, 25000, 37000, 12000), Is.EqualTo(4), "an unreachable seat does not block the team forever");
			Assert.That(TeamLeadCA.PickWeakest(Seats((7, 6)), 7, 25000, 40000, 12000), Is.EqualTo(7), "no other seat known: it stays");
		}

		[Test]
		public void TheTeamBoardKeepsTheFocusAndStartsWithNone()
		{
			var team = new TeamLeadTeam();
			Assert.That(team.FocusSeat, Is.EqualTo(-1));
			Assert.That(team.FocusHasTarget, Is.False);
			Assert.That(team.FocusTargetId, Is.EqualTo(0u));
		}

		[Test]
		public void TheSwitchDefaultsOffIsOnInSixPersonalitiesNotClassicAndHasItsOwnRow()
		{
			var info = RepoFile(Bots + "SquadManagerBotModuleCAInfo.TeamLead.cs");
			Assert.That(info, Does.Contain("public readonly bool TeamLeadFinishWeakestEnabled = false;"));
			Assert.That(info, Does.Contain("public readonly int TeamLeadFocusMaxTicks = 12000;"));

			var yaml = RepoFile("mods/cameo/ai/ai.yaml");
			var classic = yaml.IndexOf("SquadManagerBotModuleCA@classic:", StringComparison.Ordinal);
			Assert.That(classic, Is.GreaterThan(0));
			Assert.That(Regex.Matches(yaml[..classic], @"\n\t\tTeamLeadFinishWeakestEnabled: true").Count, Is.EqualTo(6));
			Assert.That(yaml[classic..], Does.Not.Contain("TeamLeadFinishWeakestEnabled"), "@classic keeps the default (off), byte-identical");

			Assert.That(AblationIds.TeamLeadFinishWeakest, Is.EqualTo("tl_finish_weakest"));
			Assert.That(AblationIds.TeamLeadFinishWeakest, Is.Not.EqualTo(AblationIds.TeamLeadCadence));
		}

		[Test]
		public void TheFocusIsActiveOnlyAfterTheLateStartAndFeedsThePushAndTheHarassGroup()
		{
			var wiring = RepoFile(Bots + "SquadManagerBotModuleCA.TeamLead.cs");
			Assert.That(wiring, Does.Contain("TeamLeadOn && Info.TeamLeadFinishWeakestEnabled && World.WorldTick >= Info.LateEscalationStartTick"));
			Assert.That(wiring, Does.Contain("!ablateFinishWeakest.Off(Player, AblationIds.TeamLeadFinishWeakest)"));
			Assert.That(wiring, Does.Contain("if (!TeamLeadFocusTarget(from, now, out target, out targetId))"), "the leader's own squad order takes the focus first");
			Assert.That(wiring, Does.Contain("if (TeamLeadFocusTarget(from, now, out target, out actorId))"), "so does the published push");
			Assert.That(wiring, Does.Contain("var focus = TeamLeadFocusPlayer();"), "and the joint harass group");
			Assert.That(wiring, Does.Contain("FindClosestEnemy(s.Units[0].Actor.Location, SquadValueOf(s), focus, s)"));
			Assert.That(wiring, Does.Contain("TeamLeadCA.PickWeakest(seats, team.FocusSeat, team.FocusSince, now, Info.TeamLeadFocusMaxTicks)"));
		}

		[Test]
		public void TheKnownBuildingsAreFogHonestAndTheFocusRefreshesOncePerTeamCheck()
		{
			var wiring = RepoFile(Bots + "SquadManagerBotModuleCA.TeamLead.cs");
			var start = wiring.IndexOf("Dictionary<Player, List<(uint Id, WPos Pos)>> TeamLeadKnownBuildings()", StringComparison.Ordinal);
			Assert.That(start, Is.GreaterThan(0));
			var body = wiring[start..wiring.IndexOf("Player TeamLeadFocusPlayer()", start, StringComparison.Ordinal)];
			Assert.That(body, Does.Contain("a.CanBeViewedByPlayer(Player)"), "a live actor counts only if the bot can see it now");
			Assert.That(body, Does.Contain("Player.FrozenActorLayer"), "everything else is what the bot remembers");
			Assert.That(body, Does.Contain("LineBuildInfo"), "walls do not make a seat strong");
			Assert.That(body, Does.Contain("TeamLeadFocusEnemy("), "living enemy seats only");
			Assert.That(wiring, Does.Contain("if (now - team.FocusEvalTick >= Info.TeamLeadCheckTicks)"), "no heavy scan outside the team check");
		}
	}
}
