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
	/// TEAM-LEAD-02 commit 1 (tl_leader_cadence; USER P0, V01 timeout RCA 2026-11-24): the leader publishes the team push from its own team check
	/// whenever the combined blob reaches the WAVE-MIN tier, on the old attack cadence at the latest every fallback window, at once when a member's
	/// idle army hits the tier limit; every member forms its wave for an open order right away.
	/// </summary>
	[TestFixture]
	public class TeamLead02CadenceTest
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

		// --- the policy ---------------------------------------------------------------------------------------------------------------

		[Test]
		public void TheLeaderPublishesAtLeastAsOftenAsTheOldCadenceAndNeverSlowerThanTheFallback()
		{
			Assert.That(TeamLeadCA.PublishInterval(400, 1500), Is.EqualTo(400), "a fast personality keeps its own cadence");
			Assert.That(TeamLeadCA.PublishInterval(2500, 1500), Is.EqualTo(1500), "the fallback caps a slow cadence");
			Assert.That(TeamLeadCA.PublishInterval(0, 1500), Is.EqualTo(1), "never zero or negative");
			Assert.That(TeamLeadCA.PublishInterval(500, 0), Is.EqualTo(1));
		}

		[Test]
		public void TheCombinedBlobMustReachTheWaveMinimumTierButNotEachMember()
		{
			// Tier 24 units / 12000: 6 members with 4 units / 2200 each are 24 / 13200 = ready; none of them alone is.
			Assert.That(TeamLeadCA.BlobReady(24, 13200, 3, true, 24, 12000), Is.True);
			Assert.That(TeamLeadCA.BlobReady(23, 13200, 3, true, 24, 12000), Is.False, "one unit short of the tier");
			Assert.That(TeamLeadCA.BlobReady(24, 11999, 3, true, 24, 12000), Is.False, "one credit short of the tier");
			Assert.That(TeamLeadCA.BlobReady(4, 2200, 3, true, 24, 12000), Is.False, "a single member is not the blob");

			// No tier limit (Noob): the floor of the join minimum still applies, never an empty order.
			Assert.That(TeamLeadCA.BlobReady(3, 0, 3, false, 0, 0), Is.True);
			Assert.That(TeamLeadCA.BlobReady(2, 9999, 3, false, 0, 0), Is.False);
			Assert.That(TeamLeadCA.BlobReady(0, 0, 0, false, 0, 0), Is.False, "a zero floor is still one unit");
		}

		[Test]
		public void AnOrderIsDueOnlyWithoutAnOpenOrderAndWithAReadyBlobAndTheIntervalOver()
		{
			Assert.That(TeamLeadCA.OrderDue(false, true, 100, -1, 1500), Is.True, "the first order has no interval to wait for");
			Assert.That(TeamLeadCA.OrderDue(true, true, 100, -1, 1500), Is.False, "an open order is never replaced");
			Assert.That(TeamLeadCA.OrderDue(false, false, 100, -1, 1500), Is.False, "WAVE-MIN: the blob is not ready");
			Assert.That(TeamLeadCA.OrderDue(false, true, 1599, 100, 1500), Is.False);
			Assert.That(TeamLeadCA.OrderDue(false, true, 1600, 100, 1500), Is.True, "exactly at the interval");
			Assert.That(TeamLeadCA.FallbackForced(1600, 100, 1500), Is.True);
			Assert.That(TeamLeadCA.FallbackForced(1599, 100, 1500), Is.False);
			Assert.That(TeamLeadCA.FallbackForced(5000, -1, 1500), Is.False, "no order yet is not a fallback");
		}

		[Test]
		public void AMemberAtTheTierIdleLimitMakesTheLeaderPublishAtOnceButTheBlobStillHasToBeReady()
		{
			// Last order 200 ticks ago, interval 1500: not due, unless a member sits at its limit.
			Assert.That(TeamLeadCA.OrderDue(false, true, 300, 100, 1500), Is.False);
			Assert.That(TeamLeadCA.OrderDue(false, true, 300, 100, 1500, anyAtCap: true), Is.True, "at once");
			Assert.That(TeamLeadCA.OrderDue(false, false, 300, 100, 1500, anyAtCap: true), Is.False, "WAVE-MIN still applies to the combined blob");
			Assert.That(TeamLeadCA.OrderDue(true, true, 300, 100, 1500, anyAtCap: true), Is.False, "an open order is not replaced either");

			var team = new TeamLeadTeam();
			team.Publish("a", 12, 5000, 1000, atCap: false);
			team.Publish("b", 50, 20000, 1000, atCap: true);
			Assert.That(team.AnyAtCap(1100, 300), Is.True);
			Assert.That(team.AnyAtCap(1301, 300), Is.False, "a stale report is ignored");
			team.Publish("b", 10, 4000, 1400, atCap: false);
			Assert.That(team.AnyAtCap(1400, 300), Is.False, "the member's army fell below its limit");
		}

		/// <summary>
		/// The acceptance test of the P0: a team with a large idle army must launch, and keep launching, within a bounded number of ticks.
		/// Six members hold 8 idle units (3000) each = 48 / 18000 against a tier of 24 / 12000; the leader's check runs every 100 ticks.
		/// </summary>
		[Test]
		public void ATeamWithALargeIdleArmyLaunchesWithinOneCheckAndAgainWithinTheFallbackPlusTheOrderWindow()
		{
			const int check = 100;
			const int cadence = 2500;
			const int fallback = 1500;
			const int gatherMin = 300;
			const int gatherMax = 1500;
			const int lateJoin = 600;
			var interval = TeamLeadCA.PublishInterval(cadence, fallback);

			var team = new TeamLeadTeam();
			var launches = new System.Collections.Generic.List<int>();
			for (var now = 0; now <= 30000; now += check)
			{
				foreach (var key in new[] { "a", "b", "c", "d", "e", "f" })
					team.Publish(key, 8, 3000, now);

				// Order upkeep (what TeamLeadMaintainOrder does): the gather ends at its maximum window.
				var order = team.Order;
				if (order != null && !order.Cancelled && !order.Pushed && TeamLeadCA.PushDue(now, order.IssueTick, gatherMin, gatherMax, 1, 0, 70))
					order.PushTick = now;

				var open = order != null && order.Open(now, lateJoin);
				var blob = team.Pool(now, 300);
				var ready = TeamLeadCA.BlobReady(blob.Units, blob.Value, 3, true, 24, 12000);
				if (TeamLeadCA.OrderDue(open, ready, now, team.LastOrderTick, interval, team.AnyAtCap(now, 300)))
				{
					team.Issue("a", default, default, 0, now);
					launches.Add(now);
				}
			}

			Assert.That(launches[0], Is.LessThanOrEqualTo(check), "the first push goes out at the first check with a ready blob");
			Assert.That(launches.Count, Is.GreaterThanOrEqualTo(10), "a push roughly every 2000-3000 ticks over 30000 ticks, not 0-3 per 5000");
			for (var i = 1; i < launches.Count; i++)
				Assert.That(launches[i] - launches[i - 1], Is.LessThanOrEqualTo(gatherMax + lateJoin + check),
					"never longer apart than the gather window, the late-join window and one check");
		}

		[Test]
		public void ABlobBelowTheTierNeverLaunchesHoweverLongItWaits()
		{
			var team = new TeamLeadTeam();
			for (var now = 0; now <= 20000; now += 100)
			{
				team.Publish("a", 10, 4000, now);
				team.Publish("b", 9, 4000, now);
				var blob = team.Pool(now, 300);
				Assert.That(TeamLeadCA.OrderDue(false, TeamLeadCA.BlobReady(blob.Units, blob.Value, 3, true, 24, 12000), now, -1, 1, anyAtCap: false), Is.False);
			}
		}

		// --- wiring pins --------------------------------------------------------------------------------------------------------------

		[Test]
		public void TheSwitchDefaultsOffIsOnInSixPersonalitiesNotClassicAndHasItsOwnRow()
		{
			var info = RepoFile(Bots + "SquadManagerBotModuleCAInfo.TeamLead.cs");
			Assert.That(info, Does.Contain("public readonly bool TeamLeadCadenceEnabled = false;"));
			Assert.That(info, Does.Contain("public readonly int TeamLeadFallbackTicks = 1500;"));

			var yaml = RepoFile("mods/cameo/ai/ai.yaml");
			var classic = yaml.IndexOf("SquadManagerBotModuleCA@classic:", StringComparison.Ordinal);
			Assert.That(classic, Is.GreaterThan(0));
			Assert.That(Regex.Matches(yaml[..classic], @"\n\t\tTeamLeadCadenceEnabled: true").Count, Is.EqualTo(6));
			Assert.That(yaml[classic..], Does.Not.Contain("TeamLeadCadenceEnabled"), "@classic keeps the default (off), byte-identical");

			Assert.That(AblationIds.TeamLeadCadence, Is.EqualTo("tl_leader_cadence"));
			Assert.That(AblationIds.TeamLeadCadence, Is.Not.EqualTo(AblationIds.TeamLead));
		}

		[Test]
		public void TheLeaderPublishesFromItsOwnCheckAndEveryMemberJoinsAtOnce()
		{
			var wiring = RepoFile(Bots + "SquadManagerBotModuleCA.TeamLead.cs");
			Assert.That(wiring, Does.Contain("TeamLeadActive && Info.TeamLeadCadenceEnabled && !ablateCadence.Off(Player, AblationIds.TeamLeadCadence)"));
			Assert.That(Regex.IsMatch(wiring, @"TeamLeadCadenceOn\)\s*TeamLeadPublish\(team, now\);"), Is.True,
				"the leader's team check publishes, no squad needed");
			Assert.That(wiring, Does.Contain("TeamLeadJoin(bot, now)"));
			Assert.That(wiring, Does.Contain("CreateAttackForce(bot);"), "the member forms its wave through the one launch path");
			Assert.That(wiring, Does.Contain("TeamLeadCA.BlobReady("));
			Assert.That(wiring, Does.Contain("TeamLeadCA.OrderDue(false, ready, now, team.LastOrderTick, interval, team.AnyAtCap("));
			Assert.That(wiring, Does.Contain("TeamLeadCA.PublishInterval(TeamLeadCadenceTicks(), Info.TeamLeadFallbackTicks)"));
			Assert.That(wiring, Does.Contain("MinAttackDelayResetTicks(Info, utilityAxesProviders)"), "the cadence is the old per-bot attack cadence");
			Assert.That(wiring, Does.Contain("LateDelay("), "late escalation shortens it");
			Assert.That(wiring, Does.Contain("TeamLeadCadenceActive && TeamLeadIdleAtLimit()"), "a member at its tier idle limit triggers the push");
			Assert.That(wiring, Does.Contain("teamLeadUnitBuilder?.IdleAtLimit ?? false"), "the unit builder owns the tier limit");
			Assert.That(wiring, Does.Not.Contain("IdleBaseUnitsMaximum"), "no second copy of the limit here");
			Assert.That(wiring, Does.Contain("WaveMinCA.Limits(minUnits, minValue)"), "WAVE-MIN still applies to the combined blob");
			Assert.That(wiring, Does.Not.Contain("IssueOrder"));
		}

		[Test]
		public void TheBlobCountsFormedQuietSquadsAndTheGatherGraceCoversASquadlessOrder()
		{
			var wiring = RepoFile(Bots + "SquadManagerBotModuleCA.TeamLead.cs");
			Assert.That(wiring, Does.Contain("TeamLeadCadenceActive ? TeamLeadBlob() : (unitsHangingAroundTheBase.Count, TeamLeadPoolValue())"),
				"switch off = the TEAM-LEAD-01 pool exactly");
			Assert.That(wiring, Does.Contain("bool TeamLeadWaveSquad(SquadCA s)"));
			Assert.That(wiring, Does.Contain("if (!TeamLeadOn || !TeamLeadWaveSquad(s))"), "the join path and the blob use one definition of a team-wave squad");
			Assert.That(wiring, Does.Contain("(Info.TeamLeadCadenceEnabled ? 5 : 2) * Info.TeamLeadCheckTicks"),
				"a published order has no squad yet: its no-report grace is longer, TEAM-LEAD-01 keeps 2 checks");
		}

		[Test]
		public void AWaveFormedForTheOpenOrderIsNotHeldForADefendCardAndTheLeaderJoinsItsOwnOrder()
		{
			var manager = RepoFile(Bots + "SquadManagerBotModuleCA.cs");
			Assert.That(manager, Does.Contain("if (!(teamFollowerLaunch && TeamLeadCadenceOn) && AttackForceEvalCA.DefendHoldActive(heldTicks, Info.MissionDefendHoldTicks))"),
				"the Defend-card hold of CreateAttackForce never blocks the team push");
			Assert.That(manager, Does.Contain("var teamFollowerLaunch = teamLaunch && (!TeamLeadIsLeader || (TeamLeadCadenceOn && TeamLeadOpenOrder() != null));"));
			Assert.That(manager, Does.Contain("TeamLeadTick(bot);"));
		}

		[Test]
		public void APushWhoseTargetCannotBeResolvedGoesToTheOrdersPositionInsteadOfStandingDown()
		{
			var wiring = RepoFile(Bots + "SquadManagerBotModuleCA.TeamLead.cs");
			var start = wiring.IndexOf("internal bool TeamLeadResolveTarget(", StringComparison.Ordinal);
			Assert.That(start, Is.GreaterThan(0));
			var body = wiring[start..];
			Assert.That(body, Does.Contain("if (!TeamLeadCadenceOn)\n\t\t\t\treturn false;").Or.Contain("if (!TeamLeadCadenceOn)\r\n\t\t\t\treturn false;"), "switch off = abandon as before");
			Assert.That(body, Does.Contain("Player.FrozenActorLayer?.FromID(order.TargetActorId)"));
			Assert.That(body, Does.Contain("squad.Target = Target.FromCell(World, cell);"));
			Assert.That(body, Does.Contain("squad.HuntCell = cell;"), "on arrival the squad looks for the next target like a finished hunt");
		}
	}
}
