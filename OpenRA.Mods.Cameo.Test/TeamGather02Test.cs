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
	/// TEAM-GATHER-02 (AUDIT_SQUAD_01 / AUDIT_CONFIG_01, USER order via Opus): TEAM-GATHER-01 still left 22 of 29 joined V04 squads in the attack state through
	/// ally defence, a lost target sent a squad alone to the nearest enemy anywhere, the push bar grew with the idle army, and the rally order churned. One switch
	/// per row (release, default off, ON in the six personalities, never @classic) and one DEV row each.
	/// </summary>
	[TestFixture]
	public class TeamGather02Test
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

		[Test]
		public void APushedBlobKeepsGoingAfterItsAreaIsClearedInsteadOfStandingDown()
		{
			Assert.That(TeamLeadCA.KeepsGoing(true, 4, 8), Is.True, "a pushed squad with at least half of WAVE-MIN keeps going");
			Assert.That(TeamLeadCA.KeepsGoing(true, 3, 8), Is.False, "a pushed remnant below half of WAVE-MIN merges back and re-joins");
			Assert.That(TeamLeadCA.KeepsGoing(true, 1, 0), Is.True, "WAVE-MIN off: a pushed squad keeps going whatever its size");
			Assert.That(TeamLeadCA.KeepsGoing(false, 8, 8), Is.True, "a squad at WAVE-MIN size keeps going");
			Assert.That(TeamLeadCA.KeepsGoing(false, 7, 8), Is.False, "a small unpushed squad stays bounded");
			Assert.That(TeamLeadCA.KeepsGoing(false, 40, 0), Is.False, "WAVE-MIN off: only the pushed order exempts");

			var states = RepoFile(Bots + "Squads/States/GroundStatesCA.cs");
			Assert.That(states, Does.Contain("if (!owner.SquadManager.TeamLeadGoOn(owner, leader.Actor))"), "AttackMove: the next enemy of the focus seat");
			Assert.That(states, Does.Contain("if (!owner.IsTargetValid && owner.SquadManager.TeamLeadKeepsGoing(owner))"), "Attack state: the same");

			var wiring = RepoFile(Bots + "SquadManagerBotModuleCA.TeamLead.cs");
			Assert.That(wiring, Does.Contain("&& !TeamLeadKeepsGoing(s);"), "the bound never applies to a squad that keeps going");
			Assert.That(wiring, Does.Contain("TeamLeadCA.KeepsGoing(s.TeamPushSeq > 0, s.Units.Count, WaveMinActive ? WaveMinBars().Units : 0)"));
			Assert.That(wiring, Does.Contain("squad.TeamPushSeq = order.Seq;"), "set when the squad is pushed");
			Assert.That(wiring, Does.Contain("s.TeamPushSeq = 0;"), "cleared when it joins the next order");
			Assert.That(wiring, Does.Contain(": seat != null ? FindClosestEnemy(leader.Location, SquadValueOf(s), seat, s) : null;"), "the focus seat first");
			Assert.That(wiring, Does.Contain("pick ??= FindClosestEnemy(leader, s);"), "then the old nearest pick");
			Assert.That(wiring, Does.Contain("var focus = TeamLeadFocusPlayer();\n\t\t\tif (focus != null)\n\t\t\t\treturn focus;"));
			Assert.That(RepoFile(Bots + "Squads/SquadCA.cs"), Does.Contain("public int TeamPushSeq;"));
		}

		[Test]
		public void ArrivalIsCountedPerUnitSoUnitsMergedInAtHomeDoNotUndoIt()
		{
			var order = new TeamLeadOrder(1, "Multi0", new WPos(0, 0, 0), new WPos(1, 1, 0), 0, 1500);
			var merged = new object();
			var whole = new object();

			// The V04 shape: 6 of the squad 10 units stand at the rally, 4 were merged in at home. The old whole-squad flag (70%) says not arrived.
			order.Report(merged, 1000, false, 10);
			Assert.That(order.ArrivedTotals(), Is.EqualTo((0, 0L)), "old rule: nothing counts");

			order.Report(merged, 1000, false, 10, 6);
			Assert.That(order.ArrivedTotals(), Is.EqualTo((6, 600L)), "per unit: 6 units and 60% of the value count toward the bar");
			Assert.That(order.Totals(), Is.EqualTo((1000L, 600L)));

			order.Report(whole, 500, true, 5);
			Assert.That(order.ArrivedTotals(), Is.EqualTo((11, 1100L)), "a squad reported without a per-unit count still counts as a whole");
			Assert.That(order.Totals(), Is.EqualTo((1500L, 1100L)));

			order.Report(merged, 1000, false, 10, 99);
			Assert.That(order.ArrivedTotals().Units, Is.EqualTo(15), "never more than the squad units");

			var wiring = RepoFile(Bots + "SquadManagerBotModuleCA.TeamLead.cs");
			Assert.That(wiring, Does.Contain("squad.Units.Count, TeamLeadJoinMergeOn ? near : -1);"));
		}

		[Test]
		public void ALeaderBelowWaveMinNeedsTheTeamArmyAndAPincerNeedsTwoWaveMinProngs()
		{
			// tl_issue_floor: (squad units, squad value, team units, team value, bars).
			Assert.That(TeamLeadCA.LeaderMayIssue(8, 4000, 8, 4000, 8, 4000), Is.True, "the leader squad itself reaches the bars");
			Assert.That(TeamLeadCA.LeaderMayIssue(3, 1500, 12, 6000, 8, 4000), Is.True, "a small leader squad: the team joinable army reaches the bars");
			Assert.That(TeamLeadCA.LeaderMayIssue(3, 1500, 7, 6000, 8, 4000), Is.False, "neither reaches the bars");
			Assert.That(TeamLeadCA.LeaderMayIssue(3, 1500, 12, 3000, 8, 4000), Is.False, "the team value is below the value bar");
			Assert.That(TeamLeadCA.LeaderMayIssue(1, 100, 1, 100, 0, 0), Is.True, "WAVE-MIN off: no floor");

			// tl_pincer_double: both prongs meet WAVE-MIN only with twice the bars gathered.
			Assert.That(TeamLeadCA.PincerGathered(16, 8000, 8, 4000), Is.True);
			Assert.That(TeamLeadCA.PincerGathered(15, 9000, 8, 4000), Is.False, "units below twice the bar");
			Assert.That(TeamLeadCA.PincerGathered(20, 7999, 8, 4000), Is.False, "value below twice the bar");

			var wiring = RepoFile(Bots + "SquadManagerBotModuleCA.TeamLead.cs");
			Assert.That(wiring, Does.Contain("if (TeamLeadIssueFloorOn && WaveMinActive)"));
			Assert.That(wiring, Does.Contain("!TeamLeadCA.LeaderMayIssue(s.Units.Count, SquadValueOf(s), pool.Units, pool.Value, minUnits, minValue)"));
			Assert.That(wiring, Does.Contain("ready0, ready1, order.FirstReadyTick) && !TeamLeadPincerThinCollapse(order, now) && !TeamLeadPushHeld(order, now))"));
			Assert.That(wiring, Does.Contain("if (TeamLeadCA.PincerGathered(units, value, minUnits, minValue))\n\t\t\t\treturn false;\n\n\t\t\torder.PincerCollapsed = true;"));
		}

		[Test]
		public void TheRallyLiesBetweenTheTeamBasesAndTheTargetNotAtTheLeaderStagingCell()
		{
			var rally = TeamLeadCA.RallyBetween(new WPos(0, 0, 0), new WPos(10240, 5120, 0), 35);
			Assert.That(rally, Is.EqualTo(new WPos(3584, 1792, 0)), "35% of the way");
			Assert.That(TeamLeadCA.RallyBetween(new WPos(100, 200, 0), new WPos(900, 200, 0), 0), Is.EqualTo(new WPos(100, 200, 0)));
			Assert.That(TeamLeadCA.RallyBetween(new WPos(100, 200, 0), new WPos(900, 200, 0), 100), Is.EqualTo(new WPos(900, 200, 0)));
			Assert.That(TeamLeadCA.RallyBetween(new WPos(100, 200, 0), new WPos(900, 200, 0), 250), Is.EqualTo(new WPos(900, 200, 0)), "never past the target");

			var wiring = RepoFile(Bots + "SquadManagerBotModuleCA.TeamLead.cs");
			Assert.That(wiring, Does.Contain("if (TeamLeadRallyMidOn && TeamLeadMidRally(target, out var mid))"));
			Assert.That(wiring, Does.Contain("TeamLeadCA.RallyBetween(new WPos((int)(x / n), (int)(y / n), 0), target, Info.TeamLeadRallyMidPercent)"));
			Assert.That(wiring, Does.Contain("PathExistsForLocomotor(mobile.Locomotor, sample.Location, cell)"), "the rally is reachable for the leader sample");
		}

		[Test]
		public void ASiegingPushedBlobStaysPushedAndIsNeverFledOrDismissed()
		{
			// The stuck rule (no move for 100 ticks) must not drop a pushed blob that stands at its target shooting it.
			var scan = 12L * 1024;
			Assert.That(TeamLeadCA.SiegeHolds(true, true, 8L * 1024 * 8 * 1024, scan * scan, 120, 300), Is.True, "pushed, valid target, 8 cells away: sieging");
			Assert.That(TeamLeadCA.SiegeHolds(true, true, 8L * 1024 * 8 * 1024, scan * scan, 300, 300), Is.True, "still inside the cap");
			Assert.That(TeamLeadCA.SiegeHolds(true, true, 8L * 1024 * 8 * 1024, scan * scan, 301, 300), Is.False, "an unreachable target within range: after 300 ticks without a move the blob retargets");
			Assert.That(TeamLeadCA.SkipActive(77, 5000, 4000, 77), Is.True, "the target that froze the blob is skipped");
			Assert.That(TeamLeadCA.SkipActive(77, 5000, 5000, 77), Is.False, "until the skip runs out");
			Assert.That(TeamLeadCA.SkipActive(77, 5000, 4000, 78), Is.False, "other targets are fine");
			Assert.That(TeamLeadCA.SkipActive(0, 5000, 4000, 0), Is.False, "no skip");
			Assert.That(TeamLeadCA.SiegeHolds(true, true, 30L * 1024 * 30 * 1024, scan * scan, 120, 300), Is.False, "30 cells away: really stuck, the old rule applies");
			Assert.That(TeamLeadCA.SiegeHolds(true, false, 0, scan * scan, 120, 300), Is.False, "no valid target");
			Assert.That(TeamLeadCA.SiegeHolds(false, true, 0, scan * scan, 120, 300), Is.False, "not a pushed blob");

			var states = RepoFile(Bots + "Squads/States/GroundStatesCA.cs");
			Assert.That(states, Does.Contain("+ (owner.SquadManager.Info.UseUpstreamStateTweaks ? 100 : 63) // CA F2p2 (9a68fea15)\n\t\t\t\t&& !owner.SquadManager.TeamLeadSieging(owner, owner.World.WorldTick - lastUpdatedTick))"), "the stuck rule leaves a sieging blob alone, for a while");
			Assert.That(states, Does.Contain("if (owner.SquadManager.TeamLeadStuckRetarget(owner, leader))\n\t\t\t\t{\n\t\t\t\t\tlastUpdatedTick = owner.World.WorldTick;"), "after the cap the blob skips the frozen target and takes the next one");

			// Nothing known at all: idle, never Flee, in both states.
			var keeps = states[states.IndexOf("if (owner.SquadManager.TeamLeadKeepsGoing(owner))", StringComparison.Ordinal)..];
			keeps = keeps[..keeps.IndexOf("else\n", StringComparison.Ordinal)];
			Assert.That(keeps, Does.Contain("new GroundUnitsIdleStateCA()"));
			Assert.That(keeps, Does.Not.Contain("Flee"), "the pushed blob never flees");
			Assert.That(states, Does.Not.Contain("TeamLeadGoOnTarget"));

			var wiring = RepoFile(Bots + "SquadManagerBotModuleCA.TeamLead.cs");
			Assert.That(wiring, Does.Contain("if (pick == null && seat != null && FoggedScans && FindFrozenEnemyTarget(leader.CenterPosition, -1, s, seat) is { } frozenSeat && frozenSeat.ID != skip)"), "a remembered building of the focus seat (not the one that froze the blob)");
			Assert.That(wiring, Does.Contain("if (seat == null && FoggedScans && FindFrozenEnemyTarget(leader.CenterPosition, -1, s) is { } frozen && frozen.ID != skip)"));
			Assert.That(wiring, Does.Contain("if (!s.IsValid || s.TeamPushSeq <= 0 || !s.IsTargetValid || !TeamLeadKeepsGoing(s))\n\t\t\t\treturn false;"));

			// TeamLeadPrepare: a pushed squad attacks on (never joins a new order first, never dismissed); nothing known = it waits, still not dismissed.
			Assert.That(wiring, Does.Contain("var goesOn = s.TeamPushSeq > 0 && TeamLeadKeepsGoing(s);"));
			Assert.That(wiring, Does.Contain("if (goesOn && TeamLeadGoOn(s, s.Units[0].Actor))"));
			Assert.That(wiring, Does.Contain("s.TeamSkipActor = s.TargetActor.ActorID;"));
			Assert.That(wiring, Does.Contain("? TeamLeadNearestEnemyExcept(leader, seat, skip) ?? (seat != null ? TeamLeadNearestEnemyExcept(leader, null, skip) : null)"));
			Assert.That(wiring, Does.Contain("TeamLeadCA.SiegeHolds(true, true,"));
			Assert.That(wiring, Does.Contain("stuckTicks, Info.TeamLeadSiegeMaxTicks);"));
			Assert.That(wiring, Does.Contain("if (order == null && teamLeadIsLeader && !goesOn)\n\t\t\t\torder = TeamLeadTryIssue(s, now);"), "no rally re-issued from a pushed squad");
			Assert.That(wiring, Does.Contain("if (teamLeadIsLeader || goesOn)\n\t\t\t\treturn false;\n\n\t\t\tDiagnosticSquadEvent(s, \"team_hold\");"));
		}

		[Test]
		public void TheMidRallyIsNotPulledBackToTheLeaderStagingCellAndSkipsPlayersWithoutABase()
		{
			var mid = new WPos(10240, 5120, 0);
			Assert.That(TeamLeadCA.StagingNearMid(mid, new WPos(10240 + 9 * 1024, 5120, 0), 10), Is.True, "9 cells from the mid point: close enough");
			Assert.That(TeamLeadCA.StagingNearMid(mid, new WPos(10240 + 10 * 1024, 5120, 0), 10), Is.True, "exactly 10 cells");
			Assert.That(TeamLeadCA.StagingNearMid(mid, new WPos(10240 + 11 * 1024, 5120, 0), 10), Is.False, "11 cells: the leader own cell, not used");
			Assert.That(TeamLeadCA.StagingNearMid(mid, mid, 0), Is.True);

			var wiring = RepoFile(Bots + "SquadManagerBotModuleCA.TeamLead.cs");
			Assert.That(wiring, Does.Contain("if (staged.HasValue && !TeamLeadCA.StagingNearMid(mid, map.CenterOfCell(staged.Value), Info.TeamLeadRallyMidStagingCells))\n\t\t\t\tstaged = null;"));
			Assert.That(wiring, Does.Contain("else if (p.HomeLocation != CPos.Zero)"), "a player without a base building and without a start location is skipped");
			Assert.That(RepoFile(Bots + "SquadManagerBotModuleCAInfo.TeamLead.cs"), Does.Contain("public readonly int TeamLeadRallyMidStagingCells = 10;"));
		}

		[Test]
		public void AllyDefenceLeavesASquadInTheOrderOrJustFormedAlone()
		{
			Assert.That(TeamLeadCA.SquadKeptForOrder(true, true, 5000, 100, 100), Is.True, "gathering for the open order: kept");
			Assert.That(TeamLeadCA.SquadKeptForOrder(true, false, 5050, 5000, 100), Is.True, "formed 50 ticks ago: its first team pass has not run");
			Assert.That(TeamLeadCA.SquadKeptForOrder(true, false, 5100, 5000, 100), Is.False, "formed 100 ticks ago: free again");
			Assert.That(TeamLeadCA.SquadKeptForOrder(true, false, 5050, -1, 100), Is.False, "no formation tick (a loaded save): free");
			Assert.That(TeamLeadCA.SquadKeptForOrder(false, true, 5000, 4990, 100), Is.False, "row off = the old rule, any idle or gathering squad can be sent");
		}

		[Test]
		public void ARallyOrderIsOnlyPublishedForAnArmyAtWaveMinAndIsNotRepublishedAtOnce()
		{
			Assert.That(TeamLeadCA.RallyFloorPassed(8, 4000, 8, 4000), Is.True);
			Assert.That(TeamLeadCA.RallyFloorPassed(7, 9000, 8, 4000), Is.False, "units below the WAVE-MIN units bar");
			Assert.That(TeamLeadCA.RallyFloorPassed(20, 3999, 8, 4000), Is.False, "value below the WAVE-MIN value bar");
			Assert.That(TeamLeadCA.RallyFloorPassed(0, 0, 0, 0), Is.False, "an empty army never passes");

			Assert.That(TeamLeadCA.RepublishHeld(1000, -1, 1500), Is.False, "no cancel yet");
			Assert.That(TeamLeadCA.RepublishHeld(2499, 1000, 1500), Is.True);
			Assert.That(TeamLeadCA.RepublishHeld(2500, 1000, 1500), Is.False, "1500 ticks after the cancel the leader may publish again");

			Assert.That(TeamLeadCA.RallyGraceHolds(true, true), Is.True, "rally order + row on: no no_reports cancel");
			Assert.That(TeamLeadCA.RallyGraceHolds(true, false), Is.False, "an attack order keeps the grace rule");
			Assert.That(TeamLeadCA.RallyGraceHolds(false, true), Is.False, "row off = the old behaviour");

			var team = new TeamLeadTeam();
			Assert.That(team.LastCancelTick, Is.EqualTo(-1));
		}

		[Test]
		public void AllyDefenceAndTheSquadFormationAreWired()
		{
			var ally = RepoFile(Bots + "SquadManagerBotModuleCA.DefendAlly.cs");
			Assert.That(ally, Does.Contain("bool DefendAllyFreeSquad(SquadCA s) => IsIdleResponderSquad(s) && !TeamLeadKeepsSquad(s);"));
			Assert.That(ally, Does.Contain("if (DefendAllyFreeSquad(s))\n\t\t\t\t\tdefendAllyResponders.Add(s.CenterPosition);"));
			Assert.That(ally, Does.Contain("s == protectSq || !DefendAllyFreeSquad(s) ||"));
			Assert.That(ally, Does.Not.Contain("!IsIdleResponderSquad(s) ||"), "the respond loop uses the free-squad rule, not the bare state rule");

			var squads = RepoFile(Bots + "SquadManagerBotModuleCA.cs");
			Assert.That(squads, Does.Contain("ret.FormedTick = World.WorldTick;\n\t\t\tSquads.Add(ret);"));
			Assert.That(squads, Does.Contain("if (TeamLeadJoinAtFormOn && attackForce.IsValid && TeamLeadOpenOrder() != null)\n\t\t\t\t\tTeamLeadPrepare(attackForce);"));
			Assert.That(RepoFile(Bots + "Squads/SquadCA.cs"), Does.Contain("public int FormedTick = -1;"));
		}

		[Test]
		public void ALostTargetRetargetsOnlyAroundTheFightThenGoesIdle()
		{
			var states = RepoFile(Bots + "Squads/States/GroundStatesCA.cs");
			Assert.That(states, Does.Contain("var bounded = owner.SquadManager.TeamLeadBoundsRetarget(owner);"));
			Assert.That(states, Does.Contain("? owner.SquadManager.FindClosestEnemy(leader.Actor, WDist.FromCells(owner.SquadManager.Info.AttackScanRadius), owner)"));
			Assert.That(states, Does.Contain("else if (bounded)\n\t\t\t\t\t\t{\n\t\t\t\t\t\t\towner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsIdleStateCA(), false);"));
			Assert.That(states, Does.Contain("if (!owner.IsTargetValid && owner.SquadManager.TeamLeadBoundsRetarget(owner))"), "the attack state is bounded too");
			Assert.That(states, Does.Contain("TeamLeadHuntsViaOrder(owner)"), "the finish hunt and its arrival go through the order");

			var wiring = RepoFile(Bots + "SquadManagerBotModuleCA.TeamLead.cs");
			Assert.That(wiring, Does.Contain("bool TeamLeadTeamSquad(SquadCA s) => s.Type == SquadCAType.Rush && !s.M13Response"));
			Assert.That(wiring, Does.Contain("&& !ReferenceEquals(s, stalemateSquad) && !ReferenceEquals(s, m13IntentSquad) && !ReferenceEquals(s, m13ResponseSquad);"), "the breaker and the liveness wave keep their own targeting");
			Assert.That(wiring, Does.Contain("internal bool TeamLeadBoundsRetarget(SquadCA s) => TeamLeadBoundedRetargetOn && TeamLeadTeamSquad(s) && (!s.IsHunting || TeamLeadHuntViaOrderOn) && !TeamLeadKeepsGoing(s);"));
			Assert.That(wiring, Does.Contain("internal bool TeamLeadHuntsViaOrder(SquadCA s) => TeamLeadHuntViaOrderOn && TeamLeadTeamSquad(s);"));
		}

		[Test]
		public void TheBarAndThePoolCountOnlyJoinableUnitsAndUnitsMergeIntoTheGatheringSquad()
		{
			var wiring = RepoFile(Bots + "SquadManagerBotModuleCA.TeamLead.cs");
			Assert.That(wiring, Does.Contain("if (!TeamLeadBarJoinableOn || TeamLeadJoinable(u))\n\t\t\t\t\tvalue += UnitValue(u.Actor);"));
			Assert.That(wiring, Does.Contain("var units = TeamLeadBarJoinableOn ? unitsHangingAroundTheBase.Count(TeamLeadJoinable) : unitsHangingAroundTheBase.Count;"));
			Assert.That(wiring, Does.Contain("HasTraitInfo<AttackBaseInfo>() && u.Actor.Info.HasTraitInfo<MobileInfo>()"));
			Assert.That(wiring, Does.Contain("!Info.ArtilleryTypes.Contains(u.Actor.Info.Name) && !Info.FireSupportTypes.Contains(u.Actor.Info.Name)"));

			// Merge: a rally or held order takes the joinable units into the gathering squad; a new squad needs a real group; the join counts only for a formed squad.
			Assert.That(wiring, Does.Contain("if (TeamLeadJoinMergeOn && (order.GatherOnly || order.Extending))"));
			Assert.That(wiring, Does.Contain("if (TeamLeadMergeIntoGatherSquad(bot, order))\n\t\t\t\t\treturn;"));
			Assert.That(wiring, Does.Contain("if (unitsHangingAroundTheBase.Count(TeamLeadJoinable) < Info.TeamLeadJoinMinUnits)\n\t\t\t\t\treturn;"));
			Assert.That(wiring, Does.Contain("(!TeamLeadJoinMergeOn || Squads.Any(q => q.IsValid && q.Type == SquadCAType.Rush && q.FormedTick == now)))\n\t\t\t\tteamLeadJoinedSeq = order.Seq;"));

			// The rally publish floor: BlobLaunch01CA.MinUnits bar and the value bar, joinable units only.
			Assert.That(wiring, Does.Contain("!TeamLeadCA.RallyFloorPassed(blob.Units, blob.Value, BlobLaunch01CA.MinUnits(minUnits, blob.Units), minValue))"));
		}

		[Test]
		public void ARallyOrderKeepsItsGraceUpToTheMaxAgeAndNoOrderFollowsACancelAtOnce()
		{
			var wiring = RepoFile(Bots + "SquadManagerBotModuleCA.TeamLead.cs");
			Assert.That(wiring, Does.Contain(": TeamLeadCA.RallyGraceHolds(TeamLeadGraceRallyOn, order.GatherOnly) ? (now - order.IssueTick > Info.TeamLeadOrderMaxTicks ? \"max_age\" : null)"));
			Assert.That(wiring, Does.Contain("if (cancel != \"leader_changed\")\n\t\t\t\t\tteam.LastCancelTick = now;"));
			Assert.That(wiring, Does.Contain("if (TeamLeadGraceRallyOn && TeamLeadCA.RepublishHeld(now, team.LastCancelTick, Info.TeamLeadRepublishHoldTicks))\n\t\t\t\treturn;"));
		}

		[Test]
		public void TheGatherFightUsesTheSquadWeaponRangeAndTheLateHuntIsOneCellForTheBlob()
		{
			var gather = RepoFile(Bots + "Squads/States/TeamLeadGatherStateCA.cs");
			Assert.That(gather, Does.Contain("manager.TeamLeadFightRange(owner)"));
			Assert.That(gather, Does.Not.Contain("WDist.FromCells(manager.Info.TeamLeadGatherFightCells)"), "the fixed distance lives in TeamLeadFightRange now");
			Assert.That(gather, Does.Not.Contain("\"AttackMove\""), "the gather state still never queues an AttackMove order");

			var wiring = RepoFile(Bots + "SquadManagerBotModuleCA.TeamLead.cs");
			Assert.That(wiring, Does.Contain("if (!TeamLeadGatherRangeOn)\n\t\t\t\treturn fixedRange;"));
			Assert.That(wiring, Does.Contain("max = System.Math.Max(max, a.GetMaximumRange().Length);"));
			Assert.That(wiring, Does.Contain("System.Math.Clamp(max, fixedRange.Length, System.Math.Max(fixedRange.Length, WDist.FromCells(Info.AttackScanRadius).Length))"));

			// Late hunt: the order target is the one hunt cell (actor id 0 = a position, which TeamLeadResolveTarget turns into the squads' HuntCell).
			Assert.That(wiring, Does.Contain("if (TeamLeadHuntViaOrderOn && FinishModeActive"));
			Assert.That(wiring, Does.Contain("target = World.Map.CenterOfCell(hunt);\n\t\t\t\tactorId = 0;\n\t\t\t\treturn true;"));
			Assert.That(RepoFile(Bots + "Squads/States/GroundStatesCA.cs"), Does.Contain("Stalemate01CA.Hunts(owner.Type) && !owner.SquadManager.TeamLeadHuntsViaOrder(owner))"));
		}

		[Test]
		public void EverySwitchIsOffByDefaultOnInTheSixPersonalitiesAndNeverInClassicAndTheGuerrillaCapIs25()
		{
			var info = RepoFile(Bots + "SquadManagerBotModuleCAInfo.TeamLead.cs");
			var keys = new[]
			{
				"TeamLeadDefendRespectsOrderEnabled", "TeamLeadBoundedRetargetEnabled", "TeamLeadJoinAtFormEnabled", "TeamLeadBarJoinableEnabled",
				"TeamLeadJoinMergeEnabled", "TeamLeadGraceRallyEnabled", "TeamLeadGatherRangeEnabled", "TeamLeadHuntViaOrderEnabled",
				"TeamLeadIssueFloorEnabled", "TeamLeadRallyMidEnabled", "TeamLeadPincerDoubleEnabled"
			};

			foreach (var key in keys)
				Assert.That(info, Does.Contain("public readonly bool " + key + " = false;"), key);

			Assert.That(info, Does.Contain("public readonly int TeamLeadFreshSquadTicks = 100;"));
			Assert.That(info, Does.Contain("public readonly int TeamLeadRepublishHoldTicks = 1500;"));
			Assert.That(info, Does.Contain("public readonly int TeamLeadSiegeMaxTicks = 300;"));
			Assert.That(info, Does.Contain("public readonly int TeamLeadSkipTicks = 900;"));
			Assert.That(info, Does.Contain("public readonly int TeamLeadRallyMidPercent = 35;"));

			var yaml = RepoFile("mods/cameo/ai/ai.yaml");
			var classic = yaml.IndexOf("SquadManagerBotModuleCA@classic:", StringComparison.Ordinal);
			Assert.That(classic, Is.GreaterThan(0));
			foreach (var key in keys)
			{
				Assert.That(Regex.Matches(yaml[..classic], "\n\t\t" + key + ": true").Count, Is.EqualTo(6), key);
				Assert.That(yaml[classic..], Does.Not.Contain(key), key + " never in @classic");
			}

			// C-10: the guerrilla personality's harass cap is 25 like the others (USER 25% rule).
			var guerrilla = yaml.IndexOf("SquadManagerBotModuleCA@guerrilla:", StringComparison.Ordinal);
			var cap = yaml.IndexOf("\n\t\tHarassCapPercent: 25", StringComparison.Ordinal);
			Assert.That(cap, Is.GreaterThan(guerrilla));
			Assert.That(cap, Is.LessThan(classic));
			Assert.That(yaml, Does.Not.Contain("HarassCapPercent: 40"));

			Assert.That(AblationIds.TeamLeadDefendRespectsOrder, Is.EqualTo("tl_defend_respects_order"));
			Assert.That(AblationIds.TeamLeadBoundedRetarget, Is.EqualTo("gt_bounded_retarget"));
			Assert.That(AblationIds.TeamLeadJoinAtForm, Is.EqualTo("tl_join_at_form"));
			Assert.That(AblationIds.TeamLeadBarJoinable, Is.EqualTo("tl_bar_joinable"));
			Assert.That(AblationIds.TeamLeadJoinMerge, Is.EqualTo("tl_join_merge"));
			Assert.That(AblationIds.TeamLeadGraceRally, Is.EqualTo("tl_grace_rally"));
			Assert.That(AblationIds.TeamLeadGatherRange, Is.EqualTo("tl_gather_range"));
			Assert.That(AblationIds.TeamLeadHuntViaOrder, Is.EqualTo("tl_hunt_via_order"));
			Assert.That(AblationIds.TeamLeadIssueFloor, Is.EqualTo("tl_issue_floor"));
			Assert.That(AblationIds.TeamLeadRallyMid, Is.EqualTo("tl_rally_mid"));
			Assert.That(AblationIds.TeamLeadPincerDouble, Is.EqualTo("tl_pincer_double"));
		}
	}
}
