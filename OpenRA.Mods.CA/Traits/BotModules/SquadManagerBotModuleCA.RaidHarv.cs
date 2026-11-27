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
	/// <summary>The outcome of planning an explored-only raid route: a usable route (waypoints, ETA) or a hold with its reason and the scout aim that asks the scout module to look where the explored lane ends.</summary>
	internal sealed class RaidRoutePlan
	{
		public bool Ok;
		public string Hold = "";
		public int EtaTicks;
		public List<CPos> Waypoints = new();
		public List<CPos> ScoutAim = new();
	}

	/// <summary>The target a raid pick resolved to: a harvester this bot sees NOW (id, cell, value).</summary>
	internal readonly record struct RaidCandidate(uint TargetId, CPos Cell, int Value);

	// RAID-HARV-01 P2a (row raid_harv; design 01, design 02, deltas 01 and 02): the squad manager's side of the native harvester raid, WITHOUT call sites (P2b hooks the pick and the orders). Fog honesty is structural here:
	//  - every actor id the raid dereferences goes through RaidTargetResolve (alive, in the world, an enemy, CanBeViewedByPlayer) and that is the ONLY GetActorById of the raid files;
	//  - the enemy economy is read through the IBotObservedEnemyProvider seam (visible-only harvester sightings, refineries from a valid Hidden/Shrouded-free snapshot), never by enumerating the world's actors;
	//  - the route is SELECTED by the engine pathfinder with a cost that excludes every unexplored cell and terrain-only blocking (BlockedByActor.None), so no hidden blocker or unexplored cell chooses or scores it; no explored lane = hold
	//    and the scout is asked (IBotScoutInterestProvider) about the unexplored edge nearest the target;
	//  - with an active ally only a valid team-leader joint assignment launches (RaidHarvGroup.Decide); the leader alone approves a proposal and never dereferences the proposed id.
	public partial class SquadManagerBotModuleCA : IBotScoutInterestProvider
	{
		AblationLatch ablateRaidHarv;
		IBotObservedEnemyProvider[] raidObservedProviders;
		IRaidKeyValues raidKeys;
		IPathFinder raidPathFinder;
		readonly RaidIncidents raidIncidents = new();
		List<CPos> raidScoutCells = new();
		int raidScoutTick = -1;

		/// <summary>raid_harv: the Info switch (off in code, classic and every block until the A/B passes) and the DEV row.</summary>
		internal bool RaidHarvOn => Info.RaidHarvEnabled && !ablateRaidHarv.Off(Player, AblationIds.RaidHarv);

		/// <summary>A raid number of this seat in whole units: P1 reads the yaml priors; the learned snapshot of the shared core replaces the source in P3.</summary>
		internal int RaidValue(RaidKey key) => (raidKeys ??= RaidKeyPriors.FromInfo(Info)).Read(key);

		int RaidSeatKey => (int)Player.PlayerActor.ActorID;

		/// <summary>True when at least one other seat is an active ally (a combatant, not defeated): then only a team-leader joint assignment launches the raid. The predicate is the rally code's, pinned equal.</summary>
		bool RaidHasActiveAllies()
		{
			foreach (var p in World.Players)
				if (RaidHarvGroup.ActiveAlly(p == Player, Player.RelationshipWith(p) == PlayerRelationship.Ally, p.NonCombatant, p.WinState != WinState.Undefined))
					return true;

			return false;
		}

		/// <summary>
		/// The R2 resolver: the actor behind an id ONLY while it is alive, in the world, an enemy and visible to this player now; otherwise null and the caller falls back to the sighting record or releases. The visibility test runs first, so nothing
		/// of an unseen actor (cell, info, owner) is read. The only GetActorById of the raid files.
		/// </summary>
		internal Actor RaidTargetResolve(uint id)
		{
			if (id == 0)
				return null;

			var actor = World.GetActorById(id);
			if (actor == null || !actor.CanBeViewedByPlayer(Player) || !actor.IsInWorld)
				return null;

			if (actor.IsDead || actor.Owner == null || Player.RelationshipWith(actor.Owner) != PlayerRelationship.Enemy)
				return null;

			return actor;
		}

		/// <summary>The enemy economy this bot has observed (harvester sightings, valid refinery snapshots) from every enabled provider; empty without a provider.</summary>
		List<BotObservedEnemy> RaidObservedEconomy()
		{
			raidObservedProviders ??= Player.PlayerActor.TraitsImplementing<IBotObservedEnemyProvider>().ToArray();
			var list = new List<BotObservedEnemy>();
			foreach (var provider in raidObservedProviders)
				if (provider != null && provider.IsTraitEnabled())
					list.AddRange(provider.ObservedEnemyEconomy());

			return list;
		}

		/// <summary>
		/// The raid's target: the nearest enemy harvester this bot sees NOW (resolved through R2; a stale sighting is not a target, it is a recon aim). Ties by actor id. Null = none in view.
		/// </summary>
		internal RaidCandidate? RaidPickCandidate(WPos from)
		{
			RaidCandidate? best = null;
			long bestD = long.MaxValue;
			foreach (var seen in RaidObservedEconomy())
			{
				if (!seen.Harvester)
					continue;

				var actor = RaidTargetResolve(seen.ActorId);
				if (actor == null)
					continue;

				var d = (actor.CenterPosition - from).HorizontalLengthSquared;
				if (d < bestD || (d == bestD && best.HasValue && seen.ActorId < best.Value.TargetId))
				{
					bestD = d;
					best = new RaidCandidate(actor.ActorID, actor.Location, actor.Info.TraitInfoOrDefault<ValuedInfo>()?.Cost ?? 0);
				}
			}

			return best;
		}

		/// <summary>
		/// The lane aim of a lost target: the nearest observed refinery (valid snapshot, never Hidden or Shrouded) to the most recent harvester sighting. Null without a sighting or a refinery: no lane, no recon aim, the raid holds.
		/// </summary>
		internal CPos? RaidLaneAim()
		{
			var economy = RaidObservedEconomy();
			BotObservedEnemy? last = null;
			foreach (var seen in economy)
				if (seen.Harvester && (!last.HasValue || seen.LastSeenTick > last.Value.LastSeenTick || (seen.LastSeenTick == last.Value.LastSeenTick && seen.ActorId < last.Value.ActorId)))
					last = seen;

			if (!last.HasValue)
				return null;

			CPos? aim = null;
			uint aimId = 0;
			long bestD = long.MaxValue;
			foreach (var seen in economy)
			{
				if (!seen.Refinery)
					continue;

				long dx = seen.Location.X - last.Value.Location.X;
				long dy = seen.Location.Y - last.Value.Location.Y;
				var d = (dx * dx) + (dy * dy);
				if (d < bestD || (d == bestD && seen.ActorId < aimId))
				{
					bestD = d;
					aim = seen.Location;
					aimId = seen.ActorId;
				}
			}

			return aim;
		}

		/// <summary>
		/// What the package must reach at a cell: the target value x the learned margin or the observed defence x (100 + risk margin), whichever is larger. The observed defence is the merged remembered threat at the cell plus the cost of the
		/// armed enemy actors this player SEES within the learned defence radius (visibility is tested before anything of the actor is read).
		/// </summary>
		internal long RaidRequired(CPos cell, int targetValue)
		{
			var defence = threatProviders.MergedThreatAt(cell);
			var radius = WDist.FromCells(RaidValue(RaidKey.DefenceRadius));
			foreach (var a in World.FindActorsInCircle(World.Map.CenterOfCell(cell), radius))
			{
				if (!a.CanBeViewedByPlayer(Player) || !a.IsInWorld)
					continue;

				if (a.IsDead || a.Owner == null || Player.RelationshipWith(a.Owner) != PlayerRelationship.Enemy || !a.Info.HasTraitInfo<AttackBaseInfo>())
					continue;

				defence += a.Info.TraitInfoOrDefault<ValuedInfo>()?.Cost ?? 0;
			}

			return RaidHarvPackage.Required(targetValue, RaidValue(RaidKey.ReqMargin), defence, Info.AttackRiskMargin);
		}

		/// <summary>
		/// Plans the explored-only route of a package to a target cell (design 02 R4). The engine search is given a cost that excludes every unexplored cell and terrain-only blocking; the explored path is cut into waypoints and each waypoint
		/// passes the risk gate (remembered threat and this raid's own incident memory) with the package value. No explored lane, or an unexplored target, or a failing waypoint = a hold with its reason and the scout aim (a flood over explored,
		/// terrain-legal cells nearest the target; no path is ever searched through unknown terrain to find it).
		/// </summary>
		internal RaidRoutePlan RaidPlanRoute(IReadOnlyList<Actor> movers, CPos target, int packageValue)
		{
			var plan = new RaidRoutePlan();
			var mobiles = movers.Select(m => m.TraitOrDefault<Mobile>()).ToList();
			if (mobiles.Count == 0 || mobiles.Any(m => m == null))
			{
				plan.Hold = "no mobile mover";
				return plan;
			}

			var lead = movers[0];
			var leadMobile = mobiles[0];
			var map = World.Map;
			var shroud = Player.Shroud;
			bool Explored(CPos c) => map.Contains(c) && shroud.IsExplored(c);
			bool Legal(CPos c) => map.Contains(c) && leadMobile.Locomotor.MovementCostForCell(c) != PathGraph.MovementCostForUnreachableCell;

			void Aim()
			{
				var flood = RaidRoute.FloodExplored(lead.Location, c => Explored(c) && Legal(c), 400);
				plan.ScoutAim = RaidRoute.ScoutAim(flood, target, Explored, map.Contains, 4);
			}

			if (!Explored(target))
			{
				plan.Hold = "target not explored";
				Aim();
				return plan;
			}

			raidPathFinder ??= World.WorldActor.Trait<IPathFinder>();
			var path = raidPathFinder.FindPathToTargetCells(lead, lead.Location, new[] { target }, BlockedByActor.None,
				RaidRoute.ExploredOnlyCost(Explored, PathGraph.PathCostForInvalidPath));
			if (path == null || path.Count == 0)
			{
				plan.Hold = "no explored lane";
				Aim();
				return plan;
			}

			// The engine lists the path target first; the raid walks it start first, without the cell it stands on.
			var travel = new List<CPos>(path.Count);
			for (var i = path.Count - 1; i >= 0; i--)
				if (path[i] != lead.Location)
					travel.Add(path[i]);

			if (!RaidRoute.AllExplored(travel, Explored))
			{
				plan.Hold = "route not explored";
				Aim();
				return plan;
			}

			var now = World.WorldTick;
			var waypoints = RaidRoute.Waypoints(travel, RaidValue(RaidKey.WaypointSpacing));
			foreach (var wp in waypoints)
				if (!PassesRiskGate(wp, packageValue) || raidIncidents.Near(wp, now, RaidValue(RaidKey.IncidentDecay), 2))
				{
					plan.Hold = "risk at waypoint";
					Aim();
					return plan;
				}

			var eta = RaidRoute.Eta(lead.Location, travel, c => mobiles.Min(m => m.MovementSpeedForCell(c)));
			if (eta == int.MaxValue)
			{
				plan.Hold = "no speed on route";
				return plan;
			}

			plan.Ok = true;
			plan.EtaTicks = eta;
			plan.Waypoints = waypoints;
			return plan;
		}

		// ---- the team side ------------------------------------------------------------------------------------------------------

		/// <summary>This seat's route-ready offer for ONE target, published only after its own explored-only plan succeeded (replaces its previous offer).</summary>
		void RaidPublishRoute(TeamLeadTeam team, uint targetId, int value, int units, int etaTicks, int now) =>
			team.PublishRaidRoute(new RaidContribution(RaidSeatKey, targetId, value, units, etaTicks, now));

		/// <summary>This seat's proposal of a target to the leader: the id as a name to compare and this seat's own last observation as a hint. A live proposal of another seat is not replaced; a stale one is.</summary>
		void RaidProposeTarget(TeamLeadTeam team, uint targetId, int req, CPos lastSeenCell, int lastSeenTick, int now)
		{
			if (RaidHarvGroup.ProposalFresh(team.RaidProposal, now, Info.TeamLeadHarassTicks) && team.RaidProposal.Value.ProposerSeat != RaidSeatKey)
				return;

			team.RaidProposal = new RaidProposal(RaidSeatKey, targetId, req, lastSeenCell, lastSeenTick, now);
		}

		/// <summary>
		/// The team leader's pass (in the leader's team check): a fresh proposal whose ROUTED value for that id reaches its requirement becomes the joint assignment (the existing HarassTargetId/HarassTick) unless a live shared target exists.
		/// Only the leader writes it, and it never dereferences the proposed id: it compares ids and values; every member resolves the actor through R2 before it reads anything of it.
		/// </summary>
		void RaidLeaderPass(TeamLeadTeam team, int now)
		{
			if (!RaidHarvOn || !teamLeadIsLeader)
				return;

			if (!RaidHarvGroup.ProposalFresh(team.RaidProposal, now, Info.TeamLeadHarassTicks))
			{
				team.RaidProposal = null;
				return;
			}

			var proposal = team.RaidProposal.Value;
			var routed = RaidHarvGroup.RoutedValue(team.RaidRoutes(), proposal.TargetId, now, Info.TeamLeadHarassTicks, Info.HarassMinUnits);
			if (!RaidHarvGroup.LeaderApproves(teamLeadIsLeader, true, routed, proposal.Req))
				return;

			if (!TeamLeadCA.HarassLive(now, team.HarassTick, Info.TeamLeadHarassTicks) || team.HarassTargetId == 0)
			{
				team.HarassTargetId = proposal.TargetId;
				team.HarassTick = now;
			}

			team.RaidProposal = null;
		}

		// ---- the scout request ------------------------------------------------------------------------------------------------------

		/// <summary>Asks the scout module to look at these cells (a request, no vision granted); replaced on every hold, stale after the lost-recon budget.</summary>
		void RaidAskScout(List<CPos> cells, int now)
		{
			raidScoutCells = cells ?? new List<CPos>();
			raidScoutTick = now;
		}

		IReadOnlyList<CPos> IBotScoutInterestProvider.ScoutInterestCells =>
			raidScoutTick >= 0 && World.WorldTick - raidScoutTick <= RaidValue(RaidKey.LostRecon) ? raidScoutCells : Array.Empty<CPos>();
	}
}
