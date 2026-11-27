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
using System.Linq;
using OpenRA.Mods.CA.Traits.BotModules.Squads;
using OpenRA.Mods.Common.Pathfinder;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	/// <summary>One squad's raid: the intent (sequence number, release), the phase, the target as this squad last SAW it, the explored waypoints, and the numbers the DEV rows and the learner signals read.</summary>
	internal sealed class RaidRun
	{
		public RaidIntent Intent;
		public int Seq;
		public uint TargetId;
		public bool Joint;
		public RaidPhase Phase = RaidPhase.Travel;

		/// <summary>The target's cell and tick at the last moment it passed the visibility resolver. Everything the raid knows of an unseen target.</summary>
		public CPos LastSeenCell;
		public int LastSeenTick;

		/// <summary>The actor as the resolver last returned it (visible then). Read for IsDead only while its last seen cell is in view (a kill this player watched).</summary>
		public Actor LastActor;

		public List<CPos> Waypoints = new();
		public List<CPos> Trail = new();
		public int WaypointIndex;
		public int PackageValue;
		public int StartTick;
		public int PhaseTick;
		public int EtaTicks;
		public int StartValue;
		public int WindowStartValue;
		public int WindowStartTick;
		public int NextCheckTick;
		public int NextPlanTick;
		public int LastOrderTick = -1;
		public CPos LastOrderCell;
		public int ReconArrivedTick = -1;
		public List<CPos> Probes = new();
		public int ProbeIndex;
		public CPos RetreatCell;
		public int Incidents;
		public int Reacquired;
		public bool KillObserved;
	}

	// RAID-HARV-01 P2b (row raid_harv): the launch decision, the squads' raid steps and the release. The call sites are four lines in existing files (TargetPick: the shared-target tier and the top of TargetHighValueFor; the idle states:
	// "already launched, stop here"; the leader's team check: the leader pass); with the row off none of them does anything and the old paths run byte-identically.
	public partial class SquadManagerBotModuleCA
	{
		/// <summary>Ticks between two launch evaluations of this seat (each one plans a route per available raid squad).</summary>
		const int RaidEvalTicks = 25;

		int raidNextEvalTick;
		int raidNextBeatTick;
		bool raidInertLogged;

		// ---- helpers ------------------------------------------------------------------------------------------------------------------

		static int RaidCheb(CPos a, CPos b) => Math.Max(Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));

		static List<Actor> RaidMovers(SquadCA squad)
		{
			var list = new List<Actor>();
			foreach (var u in squad.Units)
				if (!u.Actor.IsDead && u.Actor.IsInWorld)
					list.Add(u.Actor);

			return list;
		}

		/// <summary>A raid squad that waits for a job: a guerrilla or harass squad in its idle state, not hunting and not a defence response.</summary>
		bool RaidSquadQuiet(SquadCA s) =>
			s.IsValid && (s.Type is SquadCAType.Guerrilla or SquadCAType.Harass) && !s.IsHunting && !s.M13Response
			&& (s.FuzzyStateMachine?.CurrentState is HarasserUnitsIdleStateCA or GroundUnitsIdleStateCA);

		/// <summary>True when the squad has just been put on a raid (the idle state stops its own tick there).</summary>
		internal bool RaidHarvLaunched(SquadCA squad) => squad.FuzzyStateMachine?.CurrentState is RaidHarvState;

		/// <summary>A shared harass target that is an enemy harvester: the raid's to launch, never a direct old order.</summary>
		internal bool RaidIsHarvester(Actor actor) => actor != null && actor.Info.HasTraitInfo<HarvesterInfo>();

		/// <summary>A DEV row of the raid (DEV build only; the release build compiles it to nothing).</summary>
		void RaidRow(SquadCA squad, string evt, string reason)
		{
#if CAMEO_DEV_LOGGING
			DiagnosticSquadEvent(squad, evt, reason);
#endif
		}

		/// <summary>The enemy harvester of the team's live joint assignment, if this bot sees it now (through the R2 resolver).</summary>
		RaidCandidate? RaidSharedHarvester()
		{
			var team = teamLeadTeam;
			if (team == null || !TeamLeadJointHarassOn || team.HarassTargetId == 0 || !TeamLeadCA.HarassLive(World.WorldTick, team.HarassTick, Info.TeamLeadHarassTicks))
				return null;

			var actor = RaidTargetResolve(team.HarassTargetId);
			if (!RaidIsHarvester(actor))
				return null;

			return new RaidCandidate(actor.ActorID, actor.Location, actor.Info.TraitInfoOrDefault<ValuedInfo>()?.Cost ?? 0);
		}

		// ---- the launch ---------------------------------------------------------------------------------------------------------------

		/// <summary>
		/// The single funnel of the pick (the shared-target tier calls it with <paramref name="sharedTier"/>, the top of the high-value tier without). True = the picking squad is now on a raid (its state changed, its target is set) and the
		/// caller must not launch it the old way. False = the old path runs as before: no candidate, not a quiet squad, throttled, or the decision was Hold/Propose.
		/// </summary>
		internal bool TryRaidHarvPick(SquadCA squad, bool sharedTier)
		{
			var now = World.WorldTick;
			if (!RaidHarvOn || now < raidNextEvalTick || !RaidSquadQuiet(squad) || TeamLeadRaiderRecallOn || TeamLeadAllInOrderOpen)
				return false;

			if (!raidInertLogged)
			{
				// One line, once: the row is on but no module provides the observed enemy economy, so no raid can ever start.
				raidInertLogged = true;
				raidObservedProviders ??= Player.PlayerActor.TraitsImplementing<IBotObservedEnemyProvider>().ToArray();
				if (raidObservedProviders.Length == 0)
					RaidRow(squad, "raid_harv_inert", "no observed-enemy provider");
			}

			var cand = sharedTier ? RaidSharedHarvester() : RaidPickCandidate(squad.Units[0].Actor.CenterPosition);
			if (cand == null)
				return false;

			raidNextEvalTick = now + RaidEvalTicks;
			return RaidEvaluate(squad, cand.Value, now);
		}

		bool RaidEvaluate(SquadCA picker, RaidCandidate cand, int now)
		{
			var actor = RaidTargetResolve(cand.TargetId);
			if (actor == null)
				return false;

			var team = teamLeadTeam;
			var allies = RaidHasActiveAllies();
			var jointOn = team != null && TeamLeadJointHarassOn;
			var approved = jointOn && team.HarassTargetId == cand.TargetId && TeamLeadCA.HarassLive(now, team.HarassTick, Info.TeamLeadHarassTicks);
			var req = RaidRequired(cand.Cell, cand.Value);

			// The seat's squads that can go now (the picker always; the others only without a target of their own). Each plans its own explored-only route with the value of all of them as the ceiling of the risk gate.
			var squads = new List<SquadCA>();
			var ceiling = 0;
			foreach (var s in Squads)
			{
				if (!RaidSquadQuiet(s) || (!ReferenceEquals(s, picker) && s.IsTargetValid))
					continue;

				squads.Add(s);
				ceiling += SquadValueOf(s);
			}

			var plans = new List<(SquadCA Squad, RaidRoutePlan Plan, int Value)>();
			RaidRoutePlan firstHold = null;
			foreach (var s in squads)
			{
				var movers = RaidMovers(s);
				if (movers.Count == 0)
					continue;

				var plan = RaidPlanRoute(movers, cand.Cell, ceiling);
				if (plan.Ok)
					plans.Add((s, plan, SquadValueOf(s)));
				else
					firstHold ??= plan;
			}

			if (plans.Count == 0)
			{
				if (firstHold != null)
				{
					RaidAskScout(firstHold.ScoutAim, now);
					raidNextEvalTick = now + RaidValue(RaidKey.MoveRefresh);
					RaidRow(picker, "raid_harv_route", "hold: " + firstHold.Hold + " target=" + cand.TargetId + " aim=" + firstHold.ScoutAim.Count);
				}

				return false;
			}

			List<(SquadCA Squad, RaidRoutePlan Plan, int Value)> members = new();
			RaidLaunch decision;
			if (!allies)
			{
				// Solo: the smallest ETA-ordered prefix of the route-ready squads that reaches the requirement and the join minimum (no half raid).
				var offers = new List<RaidOffer>(plans.Count);
				for (var i = 0; i < plans.Count; i++)
					offers.Add(new RaidOffer(i, plans[i].Value, RaidMovers(plans[i].Squad).Count, plans[i].Plan.EtaTicks));

				var ready = RaidHarvPackage.TrySelect(offers, req, Info.HarassMinUnits, out var package);
				if (ready)
					members = package.Select(o => plans[o.SquadId]).ToList();

				var routed = members.Sum(m => (long)m.Value);
				decision = RaidHarvGroup.Decide(false, jointOn, approved, routed, (int)Math.Min(req, int.MaxValue), ready);
			}
			else
			{
				// With an active ally only the team leader's joint assignment launches: this seat offers everything it has routed for THIS target, the leader approves a proposal once the routed values reach the requirement.
				members = plans;
				var ownValue = members.Sum(m => m.Value);
				var ownUnits = members.Sum(m => RaidMovers(m.Squad).Count);
				var ownReady = ownUnits >= Info.HarassMinUnits;
				if (team != null && jointOn)
				{
					if (ownReady)
						RaidPublishRoute(team, cand.TargetId, ownValue, ownUnits, members.Max(m => m.Plan.EtaTicks), now);
					else
						team.RetractRaidRoute(RaidSeatKey);
				}

				var routed = team != null ? RaidHarvGroup.RoutedValue(team.RaidRoutes(), cand.TargetId, now, Info.TeamLeadHarassTicks, Info.HarassMinUnits) : 0;
				decision = RaidHarvGroup.Decide(true, jointOn, approved, routed, (int)Math.Min(req, int.MaxValue), ownReady);
				if (decision == RaidLaunch.Propose)
					RaidProposeTarget(team, cand.TargetId, (int)Math.Min(req, int.MaxValue), cand.Cell, now, now);
			}

			RaidRow(picker, "raid_harv_pick", decision + " target=" + cand.TargetId + " req=" + req + " squads=" + members.Count + "/" + squads.Count + " allies=" + allies);
			if (decision is not (RaidLaunch.Solo or RaidLaunch.Join))
				return false;

			// The package value is the gate's value: every waypoint of every member must pass with it.
			var packageValue = members.Sum(m => m.Value);
			if (members.Any(m => m.Plan.Waypoints.Any(wp => !PassesRiskGate(wp, packageValue))))
			{
				RaidRow(picker, "raid_harv_route", "hold: package risk target=" + cand.TargetId);
				return false;
			}

			foreach (var m in members)
				RaidStart(m.Squad, m.Plan, m.Value, cand, actor, decision == RaidLaunch.Join, packageValue, now);

			raidNextEvalTick = 0;
			return members.Any(m => ReferenceEquals(m.Squad, picker));
		}

		void RaidStart(SquadCA squad, RaidRoutePlan plan, int value, RaidCandidate cand, Actor target, bool joint, int packageValue, int now)
		{
			var run = new RaidRun
			{
				Intent = new RaidIntent(cand.TargetId, 1),
				TargetId = cand.TargetId,
				Joint = joint,
				LastSeenCell = cand.Cell,
				LastSeenTick = now,
				LastActor = target,
				Waypoints = plan.Waypoints,
				PackageValue = packageValue,
				StartTick = now,
				PhaseTick = now,
				EtaTicks = plan.EtaTicks,
				StartValue = value,
				WindowStartValue = value,
				WindowStartTick = now,
				NextCheckTick = now + 5,
				LastOrderTick = now,
				LastOrderCell = cand.Cell,
			};

			run.Seq = run.Intent.Seq;
			squad.TargetActor = target;
			RaidIssueMoves(squad, plan.Waypoints, false);
			squad.FuzzyStateMachine.ChangeState(squad, new RaidHarvState(run), false);
			RaidRow(squad, "raid_harv_route", "go: waypoints=" + plan.Waypoints.Count + " eta=" + plan.EtaTicks + " package=" + packageValue + " joint=" + joint);
		}

		/// <summary>Moves of the squad along explored waypoints: the first replaces the squad's orders, the others queue behind it; the last is an attack-move when asked (the strike leg).</summary>
		void RaidIssueMoves(SquadCA squad, IReadOnlyList<CPos> cells, bool attackLast)
		{
			var actors = RaidMovers(squad).ToArray();
			if (actors.Length == 0 || cells.Count == 0)
				return;

			for (var i = 0; i < cells.Count; i++)
			{
				var name = attackLast && i == cells.Count - 1 ? "AttackMove" : "Move";
				squad.Bot.QueueOrder(new Order(name, null, Target.FromCell(World, cells[i]), i > 0, groupedActors: actors));
			}
		}

		// ---- the squad's step -----------------------------------------------------------------------------------------------------------

		/// <summary>The release event this squad's raid meets this tick, or null. The assignment rules apply to a raid launched by a joint assignment (a running joint raid is not extended by the last ally's defeat).</summary>
		RaidEvent? RaidReleaseEvent(RaidRun run, int now)
		{
			if (!RaidHarvOn)
				return RaidEvent.AssignmentLost;

			if (TeamLeadRaiderRecallOn || TeamLeadAllInOrderOpen)
				return RaidEvent.AllInRecall;

			if (run.Joint)
			{
				var team = teamLeadTeam;
				if (team == null || !TeamLeadJointHarassOn)
					return RaidEvent.JointOff;

				if (team.HarassTargetId != run.TargetId || !TeamLeadCA.HarassLive(now, team.HarassTick, Info.TeamLeadHarassTicks))
					return RaidEvent.AssignmentLost;
			}

			var actors = 0;
			foreach (var s in Squads)
				if (s.FuzzyStateMachine?.CurrentState is RaidHarvState st && st.Run.TargetId == run.TargetId)
					actors += RaidMovers(s).Count;

			return actors < Info.HarassMinUnits ? RaidEvent.ValueBelowMin : null;
		}

		/// <summary>One tick of a raid squad (called by RaidHarvState). Every phase change goes through the pure RaidRecon table; a release goes through the intent (sequence bump, Stop, normal state).</summary>
		internal void RaidTick(SquadCA squad, RaidRun run, Func<CPos> home)
		{
			var now = World.WorldTick;
			if (!run.Intent.IsCurrent(run.Seq))
			{
				RaidReturnToNormal(squad);
				return;
			}

			var ev = RaidReleaseEvent(run, now);
			if (ev != null)
			{
				RaidAdvance(squad, run, ev.Value, now);
				return;
			}

			if (run.Joint)
				RaidHeartbeat(run, now);

			var movers = RaidMovers(squad);
			if (movers.Count == 0)
				return;

			var lead = movers[0];
			var leadCell = lead.Location;

			// Fog honesty: the target's position is read only from an actor that passes the resolver now; otherwise the raid keeps the last cell it saw.
			var actor = RaidTargetResolve(run.TargetId);
			if (actor != null)
			{
				run.LastActor = actor;
				run.LastSeenCell = actor.Location;
				run.LastSeenTick = now;
				squad.TargetActor = actor;
			}
			else if (squad.Target.Type == TargetType.Actor)
				squad.Target = Target.FromCell(World, run.LastSeenCell);

			// An incident: the squad lost the learned share of its value inside the learned window.
			if (run.Phase != RaidPhase.Retreat && now >= run.NextCheckTick)
			{
				run.NextCheckTick = now + 5;
				var value = SquadValueOf(squad);
				if (now - run.WindowStartTick > RaidValue(RaidKey.IncidentWindow))
				{
					run.WindowStartTick = now;
					run.WindowStartValue = value;
				}
				else if (RaidRecon.IncidentTriggered(run.WindowStartValue, value, RaidValue(RaidKey.IncidentLoss)))
				{
					RaidIncident(squad, run, leadCell, now, "loss");
					return;
				}
			}

			switch (run.Phase)
			{
				case RaidPhase.Travel:
					if (actor != null && RaidCheb(leadCell, actor.Location) <= RaidValue(RaidKey.WaypointSpacing))
						RaidAdvance(squad, run, RaidEvent.InStrikeRange, now);
					else if (actor == null && (RaidCheb(leadCell, run.LastSeenCell) <= 3 || now - run.LastSeenTick > run.EtaTicks + RaidValue(RaidKey.LostRecon)))
						RaidAdvance(squad, run, RaidEvent.TargetLost, now);
					else
						RaidTravelStep(squad, run, movers, leadCell, now);

					break;

				case RaidPhase.Strike:
					if (actor == null)
					{
						var killed = Player.Shroud.IsVisible(run.LastSeenCell) && run.LastActor is { IsDead: true };
						RaidAdvance(squad, run, RaidEvent.TargetLost, now);
						if (killed)
						{
							run.KillObserved = true;
							RaidAdvance(squad, run, RaidEvent.ReconBudgetUsed, now);
						}
					}
					else if (RaidOrderDue(run, now) && (run.LastOrderTick < 0 || RaidCheb(actor.Location, run.LastOrderCell) >= 2 || lead.IsIdle))
					{
						RaidIssueMoves(squad, new[] { actor.Location }, true);
						run.LastOrderTick = now;
						run.LastOrderCell = actor.Location;
					}

					break;

				case RaidPhase.LostRecon:
					if (actor != null)
					{
						run.Reacquired++;
						RaidAdvance(squad, run, RaidEvent.TargetReacquired, now);
					}
					else if (run.ReconArrivedTick >= 0 ? now - run.ReconArrivedTick > RaidValue(RaidKey.LostRecon) : now - run.PhaseTick > RaidValue(RaidKey.LostRecon) + run.EtaTicks)
						RaidAdvance(squad, run, RaidEvent.ReconBudgetUsed, now);
					else
						RaidReconStep(squad, run, movers, lead, now);

					break;

				case RaidPhase.Retreat:
					if (run.LastOrderTick < 0)
						RaidStartRetreat(squad, run, home(), now);
					else if (RaidCheb(leadCell, run.RetreatCell) <= 3 || (now - run.PhaseTick > RaidEvalTicks && lead.IsIdle))
						RaidAdvance(squad, run, RaidEvent.RetreatDone, now);

					break;
			}
		}

		/// <summary>
		/// A running joint raid keeps this seat's contribution for its target fresh (once per move-refresh): what the raiding squads are worth now, in actors and value. The leader's re-approval reads only these numbers, so the assignment
		/// outlives TeamLeadHarassTicks exactly as long as the group is still sufficient; retreating squads count nothing.
		/// </summary>
		void RaidHeartbeat(RaidRun run, int now)
		{
			var team = teamLeadTeam;
			if (team == null || now < raidNextBeatTick)
				return;

			raidNextBeatTick = now + RaidValue(RaidKey.MoveRefresh);
			var value = 0;
			var units = 0;
			var eta = 0;
			foreach (var s in Squads)
			{
				if (s.FuzzyStateMachine?.CurrentState is not RaidHarvState st || st.Run.TargetId != run.TargetId || st.Run.Phase == RaidPhase.Retreat)
					continue;

				value += SquadValueOf(s);
				units += RaidMovers(s).Count;
				eta = Math.Max(eta, st.Run.EtaTicks);
			}

			if (units > 0)
				RaidPublishRoute(team, run.TargetId, value, units, eta, now);
		}

		bool RaidOrderDue(RaidRun run, int now) => run.LastOrderTick < 0 || now - run.LastOrderTick >= RaidValue(RaidKey.MoveRefresh);

		/// <summary>Travel: walks the explored waypoints; at the learned refresh the route is planned again from where the lead is (the target may have moved, the explored area may have grown) and the next waypoints must still pass the risk gate.</summary>
		void RaidTravelStep(SquadCA squad, RaidRun run, List<Actor> movers, CPos leadCell, int now)
		{
			while (run.WaypointIndex < run.Waypoints.Count && RaidCheb(leadCell, run.Waypoints[run.WaypointIndex]) <= 2)
			{
				run.Trail.Add(run.Waypoints[run.WaypointIndex]);
				run.WaypointIndex++;
			}

			if (!RaidOrderDue(run, now))
				return;

			var dest = run.LastSeenCell;
			if (run.LastOrderTick >= 0 && RaidCheb(dest, run.LastOrderCell) < 2 && !movers[0].IsIdle)
			{
				run.LastOrderTick = now;
				return;
			}

			var plan = RaidPlanRoute(movers, dest, run.PackageValue);
			if (!plan.Ok)
			{
				RaidAskScout(plan.ScoutAim, now);
				RaidIncident(squad, run, leadCell, now, "route: " + plan.Hold, false);
				return;
			}

			run.Waypoints = plan.Waypoints;
			run.WaypointIndex = 0;
			run.EtaTicks = plan.EtaTicks;
			RaidIssueMoves(squad, plan.Waypoints, false);
			run.LastOrderTick = now;
			run.LastOrderCell = dest;
		}

		/// <summary>LostRecon: to the last cell the target was seen at (explored by definition), then the explored cells of the ring of the learned radius, one per octant, each reached by an explored-only lane.</summary>
		void RaidReconStep(SquadCA squad, RaidRun run, List<Actor> movers, Actor lead, int now)
		{
			var leadCell = lead.Location;
			if (run.ReconArrivedTick < 0)
			{
				if (RaidCheb(leadCell, run.LastSeenCell) <= 3)
				{
					run.ReconArrivedTick = now;
					run.LastOrderTick = -1;
					run.NextPlanTick = 0;
					RaidBuildProbes(run, lead);
					RaidRow(squad, "raid_harv_recon", "arrived probes=" + run.Probes.Count);
				}
				else
				{
					RaidTravelStep(squad, run, movers, leadCell, now);
					return;
				}
			}

			if (run.Probes.Count == 0 || now < run.NextPlanTick)
				return;

			var probe = run.Probes[run.ProbeIndex % run.Probes.Count];
			if (run.LastOrderTick >= 0 && RaidCheb(leadCell, probe) > 2 && !lead.IsIdle)
				return;

			if (run.LastOrderTick >= 0)
			{
				run.ProbeIndex++;
				probe = run.Probes[run.ProbeIndex % run.Probes.Count];
			}

			run.NextPlanTick = now + RaidEvalTicks;
			var plan = RaidPlanRoute(movers, probe, run.PackageValue);
			run.LastOrderTick = now;
			run.LastOrderCell = probe;
			if (!plan.Ok)
			{
				RaidAskScout(plan.ScoutAim, now);
				return;
			}

			RaidIssueMoves(squad, plan.Waypoints, false);
		}

		/// <summary>The probe cells: explored, terrain-legal cells reachable from the lead at the learned radius around the last seen cell, one per octant (a flood over explored cells; no path through unknown terrain). Without a ring cell the lane aim (the nearest valid refinery snapshot) is the one probe.</summary>
		void RaidBuildProbes(RaidRun run, Actor lead)
		{
			var map = World.Map;
			var shroud = Player.Shroud;
			var mobile = lead.TraitOrDefault<Mobile>();
			bool Explored(CPos c) => map.Contains(c) && shroud.IsExplored(c);
			bool Legal(CPos c) => mobile != null && mobile.Locomotor.MovementCostForCell(c) != PathGraph.MovementCostForUnreachableCell;

			var radius = RaidValue(RaidKey.ReconRadius);
			var byOctant = new CPos?[8];
			foreach (var c in RaidRoute.FloodExplored(lead.Location, x => Explored(x) && Legal(x), 600))
			{
				var dx = c.X - run.LastSeenCell.X;
				var dy = c.Y - run.LastSeenCell.Y;
				var d = Math.Max(Math.Abs(dx), Math.Abs(dy));
				if (d < radius - 1 || d > radius + 1)
					continue;

				var o = RaidRoute.Octant(dx, dy);
				byOctant[o] ??= c;
			}

			run.Probes = byOctant.Where(c => c.HasValue).Select(c => c.Value).ToList();
			run.ProbeIndex = 0;
			if (run.Probes.Count == 0 && RaidLaneAim() is { } lane && Explored(lane))
				run.Probes.Add(lane);
		}

		// ---- incident, retreat, phase changes, release -----------------------------------------------------------------------------------

		void RaidIncident(SquadCA squad, RaidRun run, CPos at, int now, string reason, bool remember = true)
		{
			if (remember)
				raidIncidents.Add(at, now);

			run.Incidents++;
			RaidRow(squad, "raid_harv_recon", "incident " + reason + " at " + at);
			RaidAdvance(squad, run, RaidEvent.Incident, now);
		}

		void RaidStartRetreat(SquadCA squad, RaidRun run, CPos home, int now)
		{
			raidIncidents.Prune(now, RaidValue(RaidKey.IncidentDecay));

			// Back along the waypoints already walked (explored by construction) to the last one with no remembered threat and no incident; the base when there is none.
			var path = new List<CPos>();
			var found = false;
			for (var i = run.Trail.Count - 1; i >= 0; i--)
			{
				path.Add(run.Trail[i]);
				if (PassesRiskGate(run.Trail[i], run.PackageValue) && !raidIncidents.Near(run.Trail[i], now, RaidValue(RaidKey.IncidentDecay), 2))
				{
					run.RetreatCell = run.Trail[i];
					found = true;
					break;
				}
			}

			if (!found)
			{
				path.Add(home);
				run.RetreatCell = home;
			}

			RaidIssueMoves(squad, path, false);
			run.LastOrderTick = now;
			run.LastOrderCell = run.RetreatCell;
		}

		/// <summary>Applies an event to the phase through the pure table and does what the new phase needs.</summary>
		void RaidAdvance(SquadCA squad, RaidRun run, RaidEvent ev, int now)
		{
			var next = RaidRecon.Next(run.Phase, ev);
			if (next == run.Phase)
				return;

			RaidRow(squad, "raid_harv_recon", run.Phase + "->" + next + " by " + ev + " seen=" + run.LastSeenCell);
			run.Phase = next;
			run.PhaseTick = now;
			run.LastOrderTick = -1;
			run.NextPlanTick = 0;
			if (next == RaidPhase.Released)
				RaidRelease(squad, run, ev.ToString(), now);
		}

		/// <summary>The release: the intent's sequence is bumped first (no stale order can follow), the squad's units get a non-queued Stop, and only then the squad returns to its normal idle state.</summary>
		void RaidRelease(SquadCA squad, RaidRun run, string reason, int now)
		{
			var actors = RaidMovers(squad).ToArray();
			var lost = Math.Max(0, run.StartValue - SquadValueOf(squad));
			RaidRow(squad, "raid_harv_result", reason + " kill=" + run.KillObserved + " lost=" + lost + " incidents=" + run.Incidents + " reacquired=" + run.Reacquired + " ticks=" + (now - run.StartTick));

			run.Intent.Release(
				() =>
				{
					if (actors.Length > 0)
						squad.Bot.QueueOrder(new Order("Stop", null, false, groupedActors: actors));
				},
				() => RaidReturnToNormal(squad));

			// The seat's offer goes with its last raid squad.
			var team = teamLeadTeam;
			if (team != null && !Squads.Any(s => !ReferenceEquals(s, squad) && s.FuzzyStateMachine?.CurrentState is RaidHarvState))
				team.RetractRaidRoute(RaidSeatKey);
		}

		void RaidReturnToNormal(SquadCA squad)
		{
			squad.Target = Target.Invalid;
			IState idle = squad.Type == SquadCAType.Harass ? new HarasserUnitsIdleStateCA() : new GroundUnitsIdleStateCA();
			squad.FuzzyStateMachine.ChangeState(squad, idle, true);
		}
	}
}
