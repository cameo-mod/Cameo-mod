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
using OpenRA.Mods.CA.Traits.BotModules.Squads;

namespace OpenRA.Mods.Cameo.Test
{
	/// <summary>
	/// RAID-HARV-01 P2b: the launch, the squad's raid step and the release, plus the four call sites in existing files. The step needs a world, so the rules are pinned in the source (order of the release, the phase table as the only writer of the
	/// phase, the fail-closed allied branch, the fog rules of every read) and the pure piece it uses (the octant of the probe ring) is tested against a brute-force reference.
	/// </summary>
	[TestFixture]
	public class RaidHarvP2bTest
	{
		const string Bots = "OpenRA.Mods.CA/Traits/BotModules/";

		static string RepoRoot()
		{
			var dir = new DirectoryInfo(AppContext.BaseDirectory);
			while (dir != null && !File.Exists(Path.Combine(dir.FullName, "mod.config")))
				dir = dir.Parent;

			Assert.That(dir, Is.Not.Null, "repository root (mod.config) not found above " + AppContext.BaseDirectory);
			return dir.FullName;
		}

		static string RepoFile(string relative) => File.ReadAllText(Path.Combine(RepoRoot(), relative)).Replace("\r\n", "\n");

		static string Flat(string text) => Regex.Replace(text, @"\s+", " ");

		static int Count(string text, string term) => text.Split(new[] { term }, StringSplitOptions.None).Length - 1;

		static string Between(string text, string from, string to)
		{
			var start = text.IndexOf(from, StringComparison.Ordinal);
			Assert.That(start, Is.GreaterThanOrEqualTo(0), from);
			var end = text.IndexOf(to, start + from.Length, StringComparison.Ordinal);
			Assert.That(end, Is.GreaterThan(start), to);
			return text[start..end];
		}

		[Test]
		public void TheProbeRingKeepsEightDistinctOctantsAndTheOppositeOffsetIsFourAway()
		{
			// The eight compass offsets fall in eight different octants.
			var compass = new[] { (5, 0), (5, 5), (0, 5), (-5, 5), (-5, 0), (-5, -5), (0, -5), (5, -5) };
			for (var i = 0; i < compass.Length; i++)
				Assert.That(RaidRoute.Octant(compass[i].Item1, compass[i].Item2), Is.EqualTo(i), compass[i].ToString());

			// Brute force over a square: in range, symmetric under a half turn, and mirrored offsets agree (an offset and its mirror on the x axis are octants i and (8 - i) % 8).
			for (var dx = -12; dx <= 12; dx++)
				for (var dy = -12; dy <= 12; dy++)
				{
					if (dx == 0 && dy == 0)
						continue;

					var o = RaidRoute.Octant(dx, dy);
					Assert.That(o, Is.InRange(0, 7), dx + "," + dy);
					Assert.That(RaidRoute.Octant(-dx, -dy), Is.EqualTo((o + 4) % 8), "half turn " + dx + "," + dy);
					Assert.That(RaidRoute.Octant(dx, -dy), Is.EqualTo((8 - o) % 8), "mirror " + dx + "," + dy);
				}
		}

		[Test]
		public void TheFourCallSitesAreGuardedAndLeaveTheOldLinesAsTheyWere()
		{
			var pick = RepoFile(Bots + "SquadManagerBotModuleCA.TargetPick.cs");
			Assert.That(Count(pick, "TryRaidHarvPick("), Is.EqualTo(2), "the shared tier and the top of the high-value tier");

			// The top of TargetHighValueFor: the raid first, the old body below it unchanged.
			var highValue = Between(pick, "bool TargetHighValueFor(SquadCA squad, WPos from, int risk)", "bool TargetVisibleFor(");
			Assert.That(Flat(highValue), Does.Contain("{ // RAID-HARV-01: the harvester raid first (row raid_harv); false = the old high-value path below, unchanged (row off: never entered). if (RaidHarvOn && TryRaidHarvPick(squad, false)) return true; var target = risk >= 0 ? FindHighValueTarget(from, risk) : FindHighValueTarget(from); if (target == null) return false; squad.TargetActor = target; return true; }"));

			// The shared tier: a shared harvester goes to the raid; every other shared target keeps the old two lines.
			var shared = Between(pick, "bool TargetSharedHarassFor(SquadCA squad)", "/// <summary>The team's live joint harass target");
			Assert.That(Flat(shared), Does.Contain("if (RaidHarvOn && RaidIsHarvester(shared)) return TryRaidHarvPick(squad, true); squad.TargetActor = shared; return true; }"));

			// The two idle states stop their own launch once the pick has put the squad on a raid, directly after the pick.
			var states = RepoFile(Bots + "Squads/States/GroundStatesCA.cs");
			Assert.That(Count(states, "owner.SquadManager.RaidHarvLaunched(owner)"), Is.EqualTo(2));
			Assert.That(Flat(states), Does.Contain("if (!owner.IsTargetValid && !FindNewTarget(owner, true, riskCheck: true)) return; // RAID-HARV-01: the pick put this squad on a harvester raid (its state changed); nothing more to launch from here. if (owner.SquadManager.RaidHarvLaunched(owner)) return;"));
			Assert.That(Flat(states), Does.Contain("if (!owner.IsTargetValid && !FindNewTarget(owner, highValueCheck: true, riskCheck: true)) return; // RAID-HARV-01: the pick put this squad on a harvester raid (its state changed); nothing more to launch from here. if (owner.SquadManager.RaidHarvLaunched(owner)) return;"));

			// The leader's pass sits first in the leader block of the team check; the team-share line skips a shared harvester only with the row on.
			var team = RepoFile(Bots + "SquadManagerBotModuleCA.TeamLead.cs");
			Assert.That(Count(team, "RaidLeaderPass(team, now);"), Is.EqualTo(1));
			Assert.That(Flat(team), Does.Contain("if (teamLeadIsLeader) { // RAID-HARV-01: the leader approves a seat's proposed harvester raid once the routed values reach its requirement (row off: returns at once). RaidLeaderPass(team, now); if (TeamLeadFinishWeakestOn)"));
			Assert.That(Flat(team), Does.Contain("if (IsPreferredObservedEnemyUnit(shared) && !(RaidHarvOn && RaidIsHarvester(shared))) s.TargetActor = shared;"));
			Assert.That(Count(team, "RaidHarv"), Is.EqualTo(Count(team, "RaidHarvOn")), "no other raid-module name in the team-lead file than the row switch");
		}

		// Opus ruling 1 (P2b): both pick sites go through the SAME gate function, row off returns the old path at both sites, and exactly these two sites exist, no third.
		[Test]
		public void TheGateFunctionHasExactlyTwoCallersAndEachIsBehindTheRowSwitch()
		{
			var hits = new List<string>();
			foreach (var project in new[] { "OpenRA.Mods.CA", "OpenRA.Mods.Cameo" })
				foreach (var file in Directory.EnumerateFiles(Path.Combine(RepoRoot(), project), "*.cs", SearchOption.AllDirectories))
				{
					var n = Count(File.ReadAllText(file), "TryRaidHarvPick(");
					if (n > 0)
						hits.Add(Path.GetFileName(file) + ":" + n);
				}

			hits.Sort(StringComparer.Ordinal);
			Assert.That(hits, Is.EqualTo(new[] { "SquadManagerBotModuleCA.RaidHarvRun.cs:1", "SquadManagerBotModuleCA.TargetPick.cs:2" }), "the definition and the two call sites");

			var pick = Flat(RepoFile(Bots + "SquadManagerBotModuleCA.TargetPick.cs"));
			Assert.That(Count(pick, "if (RaidHarvOn && TryRaidHarvPick(squad, false)) return true;"), Is.EqualTo(1), "site 1 (the top of the high-value tier): the row switch is the first operand");
			Assert.That(Count(pick, "if (RaidHarvOn && RaidIsHarvester(shared)) return TryRaidHarvPick(squad, true);"), Is.EqualTo(1), "site 2 (the shared tier): the row switch is the first operand");

			// Row off: the gate function is never entered, so both sites fall through to their old lines (pinned in TheFourCallSitesAreGuardedAndLeaveTheOldLinesAsTheyWere).
			Assert.That(RepoFile(Bots + "SquadManagerBotModuleCA.RaidHarv.cs"), Does.Contain("internal bool RaidHarvOn => Info.RaidHarvEnabled && !ablateRaidHarv.Off(Player, AblationIds.RaidHarv);"));
			Assert.That(RepoFile("OpenRA.Mods.CA/Traits/BotModules/SquadManagerBotModuleCAInfo.RaidHarv.cs"), Does.Contain("RaidHarvEnabled = false;"));
		}

		// Opus ruling 2 (P2b): no defer-on-propose. A pending proposal keeps the proposer's normal harass behaviour and produces ZERO raid orders; the fail-closed Hold still applies to the raid.
		[Test]
		public void APendingProposalProducesNoRaidOrderAndTheProposerFallsBackToItsNormalBehaviour()
		{
			// The pure decision: with an active ally and no approved assignment the answer is Propose, which is neither a launch nor an order.
			Assert.That(RaidHarvGroup.Decide(true, true, false, 5000, 900, true), Is.EqualTo(RaidLaunch.Propose));
			Assert.That(RaidHarvGroup.Decide(true, true, false, 5000, 900, true), Is.Not.AnyOf(RaidLaunch.Solo, RaidLaunch.Join));
			Assert.That(RaidHarvGroup.Decide(true, true, true, 5000, 900, true), Is.EqualTo(RaidLaunch.Join), "only the leader's approval turns it into a launch");

			// Source: everything the evaluation does BEFORE the launch gate issues no order, changes no state and starts no run; Propose returns false to the pick (the old path runs).
			var run = RepoFile(Bots + "SquadManagerBotModuleCA.RaidHarvRun.cs");
			var before = Between(run, "bool RaidEvaluate(SquadCA picker, RaidCandidate cand, int now)", "if (decision is not (RaidLaunch.Solo or RaidLaunch.Join))");
			foreach (var forbidden in new[] { "QueueOrder", "RaidIssueMoves(", "RaidStart(", "ChangeState(", "new Order(", "RaidRelease(", "TargetActor =" })
				Assert.That(before, Does.Not.Contain(forbidden), forbidden);

			Assert.That(before, Does.Contain("RaidProposeTarget("));
			Assert.That(Flat(run), Does.Contain("if (decision is not (RaidLaunch.Solo or RaidLaunch.Join)) return false;"), "Propose and Hold return false: the caller's old path runs");

			// The helpers of the P2a file (offer, proposal, leader pass, scout request) issue no order either.
			var p2a = RepoFile(Bots + "SquadManagerBotModuleCA.RaidHarv.cs");
			foreach (var forbidden in new[] { "QueueOrder", "new Order(", "ChangeState(", "RaidIssueMoves(" })
				Assert.That(p2a, Does.Not.Contain(forbidden), forbidden);
		}

		// Opus ruling 3 (P2b): a Join lasts TeamLeadHarassTicks (the existing freshness constant); on expiry without re-approval the release sequence runs; the leader may re-approve a live raid in its pass BEFORE expiry.
		[Test]
		public void AJoinLastsTheHarassFreshnessAndTheLeaderMayReapproveALiveRaidBeforeItLapses()
		{
			// The pure rule: every input must hold (brute force over all combinations against the stated formula).
			foreach (var leader in new[] { false, true })
				foreach (var approved in new uint[] { 0, 77 })
					foreach (var assigned in new uint[] { 0, 77, 88 })
						foreach (var live in new[] { false, true })
							foreach (var routed in new long[] { 0, 899, 900, 5000 })
								foreach (var req in new[] { 0, 900 })
								{
									var expected = leader && approved != 0 && approved == assigned && live && req > 0 && routed >= req;
									Assert.That(RaidHarvGroup.LeaderRefreshes(leader, approved, assigned, live, routed, req), Is.EqualTo(expected), $"{leader} {approved} {assigned} {live} {routed} {req}");
								}

			// Timeline with the real team record and the pure rules: approved at tick 0 (req 900, freshness 1500); a seat's heartbeat at every leader pass until tick 500; the leader passes every 100 ticks.
			const int Freshness = 1500;
			var team = new TeamLeadTeam { HarassTargetId = 77, HarassTick = 0, RaidApprovedTarget = 77, RaidApprovedReq = 900 };
			var lastRefresh = 0;
			for (var now = 100; now <= 4000; now += 100)
			{
				if (now <= 500)
					team.PublishRaidRoute(new RaidContribution(1, 77, 1000, 3, 300, now));

				var live = TeamLeadCA.HarassLive(now, team.HarassTick, Freshness) && team.HarassTargetId != 0;
				var routed = RaidHarvGroup.RoutedValue(team.RaidRoutes(), team.RaidApprovedTarget, now, Freshness, 3);
				if (RaidHarvGroup.LeaderRefreshes(true, team.RaidApprovedTarget, team.HarassTargetId, live, routed, team.RaidApprovedReq))
				{
					team.HarassTick = now;
					lastRefresh = now;
				}
			}

			// The last heartbeat (tick 500) keeps the contributions fresh until tick 2000 (500 + 1500): the leader refreshes through its pass at 2000 and not after.
			Assert.That(lastRefresh, Is.EqualTo(2000));
			Assert.That(TeamLeadCA.HarassLive(3500, team.HarassTick, Freshness), Is.True, "still live exactly TeamLeadHarassTicks after the last refresh");
			Assert.That(TeamLeadCA.HarassLive(3501, team.HarassTick, Freshness), Is.False, "lapses without a re-approval: every joint raid then meets AssignmentLost and releases");

			// Without any re-approval the assignment lasts exactly TeamLeadHarassTicks.
			Assert.That(TeamLeadCA.HarassLive(1500, 0, Freshness), Is.True);
			Assert.That(TeamLeadCA.HarassLive(1501, 0, Freshness), Is.False);

			// Source: the leader pass refreshes only through the pure rule (ids and values, no actor), the approval records target and requirement, a lapsed or replaced assignment clears the approval, and the members' heartbeat publishes the running raid's numbers.
			var p2a = RepoFile(Bots + "SquadManagerBotModuleCA.RaidHarv.cs");
			var leaderPass = Between(p2a, "void RaidLeaderPass(TeamLeadTeam team, int now)", "// ---- the scout request");
			Assert.That(Flat(leaderPass), Does.Contain("if (RaidHarvGroup.LeaderRefreshes(teamLeadIsLeader, team.RaidApprovedTarget, team.HarassTargetId, live, RaidHarvGroup.RoutedValue(team.RaidRoutes(), team.RaidApprovedTarget, now, Info.TeamLeadHarassTicks, Info.HarassMinUnits), team.RaidApprovedReq)) team.HarassTick = now;"));
			Assert.That(Flat(leaderPass), Does.Contain("if (team.RaidApprovedTarget != 0 && (!live || team.HarassTargetId != team.RaidApprovedTarget)) { team.RaidApprovedTarget = 0; team.RaidApprovedReq = 0; }"));
			Assert.That(leaderPass, Does.Contain("team.RaidApprovedTarget = proposal.TargetId;"));
			Assert.That(leaderPass, Does.Contain("team.RaidApprovedReq = proposal.Req;"));
			Assert.That(Count(leaderPass, "team.HarassTick = now;"), Is.EqualTo(2), "the refresh and the approval: the only two writers of the tick in the raid module");
			Assert.That(leaderPass, Does.Not.Contain("GetActorById"));
			Assert.That(leaderPass, Does.Not.Contain("RaidTargetResolve"));

			var run = RepoFile(Bots + "SquadManagerBotModuleCA.RaidHarvRun.cs");
			Assert.That(Flat(run), Does.Contain("if (run.Joint) RaidHeartbeat(run, now);"));
			var beat = Between(run, "void RaidHeartbeat(RaidRun run, int now)", "bool RaidOrderDue(");
			Assert.That(beat, Does.Contain("st.Run.Phase == RaidPhase.Retreat"), "retreating squads contribute nothing");
			Assert.That(beat, Does.Contain("RaidPublishRoute(team, run.TargetId, value, units, eta, now);"));
			Assert.That(beat, Does.Not.Contain("HarassTick"), "a member never writes the assignment");
			Assert.That(beat, Does.Not.Contain("GetActorById"));

			// The lapse itself is the existing release path: the assignment-lost event for a joint raid, then Seq bump, Stop, normal state (pinned in TheReleaseBumpsTheSequenceThenStopsThenReturnsToTheNormalState).
			var release = Between(run, "RaidEvent? RaidReleaseEvent(RaidRun run, int now)", "/// <summary>One tick of a raid squad");
			Assert.That(Flat(release), Does.Contain("if (team.HarassTargetId != run.TargetId || !TeamLeadCA.HarassLive(now, team.HarassTick, Info.TeamLeadHarassTicks)) return RaidEvent.AssignmentLost;"));
		}

		[Test]
		public void TheRaidStateHoldsTheRunAndHandsEveryTickToTheManager()
		{
			var state = RepoFile(Bots + "Squads/States/RaidHarvState.cs");
			Assert.That(state, Does.Contain("owner.SquadManager.RaidTick(owner, Run, () => HomeLocation(owner));"));
			Assert.That(state, Does.Contain("public void Deactivate(SquadCA owner) => Run.Intent.Release(null, null);"), "a state left by anything else ends the run and issues nothing");
			Assert.That(state, Does.Not.Contain("QueueOrder"), "the state issues no order; the manager's step does");
		}

		[Test]
		public void TheLaunchIsFailClosedWithAlliesAndNeverSoloWithOne()
		{
			var run = RepoFile(Bots + "SquadManagerBotModuleCA.RaidHarvRun.cs");
			Assert.That(run, Does.Contain("var allies = RaidHasActiveAllies();"));
			Assert.That(Count(run, "RaidHarvGroup.Decide(false,"), Is.EqualTo(1), "the only solo decision");
			Assert.That(Count(run, "RaidHarvGroup.Decide(true,"), Is.EqualTo(1));
			var solo = run.IndexOf("RaidHarvGroup.Decide(false,", StringComparison.Ordinal);
			var guard = run.LastIndexOf("if (!allies)", solo, StringComparison.Ordinal);
			var allied = run.IndexOf("RaidHarvGroup.Decide(true,", StringComparison.Ordinal);
			Assert.That(guard, Is.GreaterThan(0).And.LessThan(solo), "the solo decision sits under if (!allies)");
			Assert.That(allied, Is.GreaterThan(solo));
			Assert.That(run.IndexOf("else\n\t\t\t{", solo, StringComparison.Ordinal), Is.LessThan(allied), "the allied decision is the else branch");

			// The allied branch publishes this seat's routed contribution and proposes; only Solo and Join launch.
			Assert.That(Flat(run), Does.Contain("if (decision == RaidLaunch.Propose) RaidProposeTarget(team, cand.TargetId, (int)Math.Min(req, int.MaxValue), cand.Cell, now, now);"));
			Assert.That(Flat(run), Does.Contain("if (decision is not (RaidLaunch.Solo or RaidLaunch.Join)) return false;"));
			Assert.That(Flat(run), Does.Contain("RaidStart(m.Squad, m.Plan, m.Value, cand, actor, decision == RaidLaunch.Join, packageValue, now);"));

			// Solo takes the smallest ETA-ordered prefix (no half raid); no launch during the recall or an all-in order; every pick resolves its candidate through R2 first.
			Assert.That(run, Does.Contain("RaidHarvPackage.TrySelect(offers, req, Info.HarassMinUnits, out var package)"));
			Assert.That(Flat(run), Does.Contain("if (!RaidHarvOn || now < raidNextEvalTick || !RaidSquadQuiet(squad) || TeamLeadRaiderRecallOn || TeamLeadAllInOrderOpen) return false;"));
			Assert.That(Flat(run), Does.Contain("var actor = RaidTargetResolve(cand.TargetId); if (actor == null) return false;"));
			Assert.That(Flat(run), Does.Contain("if (members.Any(m => m.Plan.Waypoints.Any(wp => !PassesRiskGate(wp, packageValue))))"), "every waypoint of every member passes the gate with the PACKAGE value");

			// The shared target comes from the resolver, never from a direct id read.
			Assert.That(Flat(run), Does.Contain("var actor = RaidTargetResolve(team.HarassTargetId); if (!RaidIsHarvester(actor)) return null;"));
		}

		[Test]
		public void TheFogRulesHoldInEveryReadOfTheRunFile()
		{
			var run = RepoFile(Bots + "SquadManagerBotModuleCA.RaidHarvRun.cs");

			// No raw lookups: the resolver of the P2a file is the only path from an id to an actor; no world enumeration; no terrain path search here (the planner owns it).
			Assert.That(run, Does.Not.Contain("GetActorById("));
			Assert.That(run, Does.Not.Contain("World.Actors"));
			Assert.That(run, Does.Not.Contain("ActorsHavingTrait"));
			Assert.That(run, Does.Not.Contain("FindActorsInCircle"));
			Assert.That(run, Does.Not.Contain("FrozenActorLayer"));
			Assert.That(run, Does.Not.Contain("FindPath"));
			Assert.That(run, Does.Not.Contain("BlockedByActor"));

			// An unseen target keeps only its last seen cell: the squad's actor target is replaced by the cell the moment the resolver fails.
			Assert.That(Flat(run), Does.Contain("else if (squad.Target.Type == TargetType.Actor) squad.Target = Target.FromCell(World, run.LastSeenCell);"));

			// A kill counts only when the squad's player watched the cell where the target was last seen.
			Assert.That(run, Does.Contain("var killed = Player.Shroud.IsVisible(run.LastSeenCell) && run.LastActor is { IsDead: true };"), "the cell's visibility is tested before the stale actor is read (CM F1)");

			// Counts of the raid's actors (join minimum, offers, heartbeat) skip dead units until the manager prunes them (CM N1).
			Assert.That(run, Does.Not.Contain(".Units.Count"), "alive counts only: RaidMovers(s).Count");
			Assert.That(Count(run, "RaidMovers("), Is.GreaterThanOrEqualTo(8));

			// The probe ring is a flood over explored, terrain-legal cells.
			Assert.That(Flat(run), Does.Contain("RaidRoute.FloodExplored(lead.Location, x => Explored(x) && Legal(x), 600)"));
			Assert.That(run, Does.Contain("bool Explored(CPos c) => map.Contains(c) && shroud.IsExplored(c);"));
		}

		[Test]
		public void TheStepStartsWithTheCurrentIntentAndOnlyTheTableWritesThePhase()
		{
			var run = RepoFile(Bots + "SquadManagerBotModuleCA.RaidHarvRun.cs");
			var tick = Between(run, "internal void RaidTick(SquadCA squad, RaidRun run, Func<CPos> home)", "bool RaidOrderDue(");

			// A stale step (the intent was released) issues nothing: it only returns the squad to its normal state.
			Assert.That(Flat(tick), Does.Contain("var now = World.WorldTick; if (!run.Intent.IsCurrent(run.Seq)) { RaidReturnToNormal(squad); return; } var ev = RaidReleaseEvent(run, now); if (ev != null) { RaidAdvance(squad, run, ev.Value, now); return; }"));

			// One writer of the phase after launch: RaidAdvance, through the pure table.
			Assert.That(Count(run, "run.Phase = "), Is.EqualTo(1));
			Assert.That(run, Does.Contain("var next = RaidRecon.Next(run.Phase, ev);"));
			Assert.That(Flat(run), Does.Contain("if (next == RaidPhase.Released) RaidRelease(squad, run, ev.ToString(), now);"));

			// All four release events are produced, in this order of precedence.
			var release = Between(run, "RaidEvent? RaidReleaseEvent(RaidRun run, int now)", "/// <summary>One tick of a raid squad");
			var off = release.IndexOf("RaidEvent.AssignmentLost", StringComparison.Ordinal);
			var recall = release.IndexOf("RaidEvent.AllInRecall", StringComparison.Ordinal);
			var joint = release.IndexOf("RaidEvent.JointOff", StringComparison.Ordinal);
			var min = release.IndexOf("RaidEvent.ValueBelowMin", StringComparison.Ordinal);
			Assert.That(new[] { off, recall, joint, min }, Is.All.GreaterThan(0));
			Assert.That(recall, Is.LessThan(joint));
			Assert.That(joint, Is.LessThan(min));
			Assert.That(Flat(release), Does.Contain("if (run.Joint) {"), "the assignment rules apply to a raid that a joint assignment launched");
			Assert.That(Flat(release), Does.Contain("team.HarassTargetId != run.TargetId || !TeamLeadCA.HarassLive(now, team.HarassTick, Info.TeamLeadHarassTicks)"));
			Assert.That(Flat(release), Does.Contain("return actors < Info.HarassMinUnits ? RaidEvent.ValueBelowMin : null;"));
		}

		[Test]
		public void TheReleaseBumpsTheSequenceThenStopsThenReturnsToTheNormalState()
		{
			var run = RepoFile(Bots + "SquadManagerBotModuleCA.RaidHarvRun.cs");
			var release = Between(run, "void RaidRelease(SquadCA squad, RaidRun run, string reason, int now)", "void RaidReturnToNormal(SquadCA squad)");
			var intent = release.IndexOf("run.Intent.Release(", StringComparison.Ordinal);
			var stop = release.IndexOf("new Order(\"Stop\", null, false, groupedActors: actors)", StringComparison.Ordinal);
			var normal = release.IndexOf("() => RaidReturnToNormal(squad));", StringComparison.Ordinal);
			Assert.That(intent, Is.GreaterThan(0));
			Assert.That(stop, Is.GreaterThan(intent), "the Stop is the first callback of the intent's release (after the sequence bump)");
			Assert.That(normal, Is.GreaterThan(stop), "the normal state comes last");
			Assert.That(release, Does.Not.Contain("\"Move\""), "a release issues no move");

			var back = Between(run, "void RaidReturnToNormal(SquadCA squad)", "\n\t}\n}");
			Assert.That(Flat(back), Does.Contain("squad.Target = Target.Invalid; IState idle = squad.Type == SquadCAType.Harass ? new HarasserUnitsIdleStateCA() : new GroundUnitsIdleStateCA(); squad.FuzzyStateMachine.ChangeState(squad, idle, true);"));

			// The seat's offer is retracted with its last raid squad.
			Assert.That(Flat(release), Does.Contain("if (team != null && !Squads.Any(s => !ReferenceEquals(s, squad) && s.FuzzyStateMachine?.CurrentState is RaidHarvState)) team.RetractRaidRoute(RaidSeatKey);"));

			// The pure release helper P1 pinned is the one used.
			Assert.That(RepoFile(Bots + "Squads/RaidRecon.cs"), Does.Contain("Valid = false;\n\t\t\tSeq++;\n\t\t\tstopQueuedOrders?.Invoke();\n\t\t\treturnToNormalState?.Invoke();"));
		}

		[Test]
		public void TheOrdersAreExploredWaypointsWithAnAttackMoveOnlyAtTheStrikeLeg()
		{
			var run = RepoFile(Bots + "SquadManagerBotModuleCA.RaidHarvRun.cs");
			var issue = Between(run, "void RaidIssueMoves(", "// ---- the squad's step");
			Assert.That(Flat(issue), Does.Contain("var name = attackLast && i == cells.Count - 1 ? \"AttackMove\" : \"Move\";"));
			Assert.That(Flat(issue), Does.Contain("new Order(name, null, Target.FromCell(World, cells[i]), i > 0, groupedActors: actors)"), "the first order replaces, the rest queue behind it");

			// Only the strike phase asks for the attack-move; travel, recon and retreat pass false.
			Assert.That(Count(run, "RaidIssueMoves(squad, new[] { actor.Location }, true);"), Is.EqualTo(1));
			Assert.That(Count(run, "RaidIssueMoves(squad, plan.Waypoints, false);"), Is.EqualTo(3), "launch, travel refresh and probe: all explored waypoints");
			Assert.That(Count(run, "RaidIssueMoves(squad, path, false);"), Is.EqualTo(1), "the retreat walks the trail back");

			// The retreat goes back along the walked (explored) waypoints to the last one with no threat and no incident; the base otherwise.
			var retreat = Between(run, "void RaidStartRetreat(", "/// <summary>Applies an event to the phase");
			Assert.That(Flat(retreat), Does.Contain("if (PassesRiskGate(run.Trail[i], run.PackageValue) && !raidIncidents.Near(run.Trail[i], now, RaidValue(RaidKey.IncidentDecay), 2))"));
			Assert.That(retreat, Does.Contain("path.Add(home);"));
		}

		[Test]
		public void TheDevRowsExistOnlyInTheDevBuildAndAreNamedAsDesigned()
		{
			var run = RepoFile(Bots + "SquadManagerBotModuleCA.RaidHarvRun.cs");
			Assert.That(Flat(run), Does.Contain("void RaidRow(SquadCA squad, string evt, string reason) { #if CAMEO_DEV_LOGGING DiagnosticSquadEvent(squad, evt, reason); #endif }"));
			Assert.That(Count(run, "DiagnosticSquadEvent("), Is.EqualTo(1), "one call, inside the DEV-only wrapper");
			foreach (var row in new[] { "raid_harv_pick", "raid_harv_route", "raid_harv_recon", "raid_harv_result", "raid_harv_inert" })
				Assert.That(run, Does.Contain("\"" + row + "\""), row);

			// The result row carries the numbers the learner signals read.
			Assert.That(run, Does.Contain("reason + \" kill=\" + run.KillObserved + \" lost=\" + lost + \" incidents=\" + run.Incidents + \" reacquired=\" + run.Reacquired + \" ticks=\" + (now - run.StartTick)"));
		}

		[Test]
		public void TheRaidNumbersAreTheLearnedKeysAndTheCadenceIsBounded()
		{
			var run = RepoFile(Bots + "SquadManagerBotModuleCA.RaidHarvRun.cs");

			// The cadences and radii are the nine keys (priors in P2b, the learned snapshot in P3), not numbers of this file.
			foreach (var key in new[] { "RaidKey.MoveRefresh", "RaidKey.LostRecon", "RaidKey.ReconRadius", "RaidKey.IncidentWindow", "RaidKey.IncidentLoss", "RaidKey.IncidentDecay", "RaidKey.WaypointSpacing" })
				Assert.That(run, Does.Contain(key), key);

			// One evaluation per seat per 25 ticks (each plans a route per available squad); a hold with no lane waits one move-refresh so the scout has time.
			Assert.That(run, Does.Contain("const int RaidEvalTicks = 25;"));
			Assert.That(run, Does.Contain("raidNextEvalTick = now + RaidEvalTicks;"));
			Assert.That(run, Does.Contain("raidNextEvalTick = now + RaidValue(RaidKey.MoveRefresh);"));
		}
	}
}
