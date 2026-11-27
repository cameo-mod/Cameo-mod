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

		static string RepoFile(string relative)
		{
			var dir = new DirectoryInfo(AppContext.BaseDirectory);
			while (dir != null && !File.Exists(Path.Combine(dir.FullName, "mod.config")))
				dir = dir.Parent;

			Assert.That(dir, Is.Not.Null, "repository root (mod.config) not found above " + AppContext.BaseDirectory);
			return File.ReadAllText(Path.Combine(dir.FullName, relative)).Replace("\r\n", "\n");
		}

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
			Assert.That(run, Does.Contain("var killed = run.LastActor is { IsDead: true } && Player.Shroud.IsVisible(run.LastSeenCell);"));

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
