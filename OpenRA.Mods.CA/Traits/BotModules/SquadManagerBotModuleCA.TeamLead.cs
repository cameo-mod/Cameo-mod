#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.CA.Traits.BotModules.Squads;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	// TEAM-LEAD-01 (USER rule, 2026-11-23). World wiring of TeamLeadCA. Reached only behind Info.TeamLeadEnabled (and AggTeamEnabled): with the
	// switch off no seat is published, no election runs, no squad is held and no order is issued, so the bot plays exactly as before.
	// The periodic work is one pass every TeamLeadCheckTicks (election, order upkeep, own pool); everything else reads cached values.
	public partial class SquadManagerBotModuleCA
	{
		AblationLatch ablateTeamLead;
		AblationLatch ablateJointHarass;
		int teamLeadNextCheck;
		string teamLeadKey;
		TeamLeadTeam teamLeadTeam;
		bool teamLeadEngaged;
		bool teamLeadIsLeader;

		/// <summary>TEAM-LEAD-01 is on for this bot: the Info switch (with the AggTeam code it builds on) and the DEV ablation row.</summary>
		internal bool TeamLeadActive => Info.TeamLeadEnabled && Info.AggTeamEnabled && !ablateTeamLead.Off(Player, AblationIds.TeamLead);

		/// <summary>The feature is in force for this bot right now: active, and the team has a leader and at least two living bots.</summary>
		internal bool TeamLeadOn => teamLeadEngaged && TeamLeadActive;

		/// <summary>The joint guerrilla/harass group is on: the team feature in force, and its own DEV row (row off = every squad picks its own target).</summary>
		internal bool TeamLeadJointHarassOn => TeamLeadOn && !ablateJointHarass.Off(Player, AblationIds.TeamLeadJointHarass);

		/// <summary>This bot is the elected leader of an engaged team.</summary>
		internal bool TeamLeadIsLeader => TeamLeadOn && teamLeadIsLeader;

		static string TeamLeadKeyOf(Player p) => p.InternalName ?? "#" + p.ClientIndex;

		// A seat counts only if its bot can take orders: it runs a squad manager with this feature on (a Fransbot or a classic bot never leads).
		static bool TeamLeadSeatCapable(Player p) =>
			p.PlayerActor.TraitsImplementing<SquadManagerBotModuleCA>().Any(m => !m.IsTraitDisabled && m.TeamLeadActive);

		// The in-game score the election reads: the player's experience counter (the same number the observer panel shows).
		static long TeamLeadScoreOf(Player p) => p.PlayerActor.TraitOrDefault<PlayerStatistics>()?.Experience ?? 0;

		List<TeamLeadCA.Seat> TeamLeadSeats()
		{
			var seats = new List<TeamLeadCA.Seat>();
			foreach (var p in World.Players)
			{
				if (!p.IsBot || p.NonCombatant || (p != Player && !Player.IsAlliedWith(p)) || !TeamLeadSeatCapable(p))
					continue;

				seats.Add(new TeamLeadCA.Seat(TeamLeadKeyOf(p), TeamLeadScoreOf(p), p.WinState == WinState.Undefined));
			}

			return seats;
		}

		long TeamLeadPoolValue()
		{
			long value = 0;
			foreach (var u in unitsHangingAroundTheBase)
				value += UnitValue(u.Actor);
			return value;
		}

		/// <summary>One team pass, at most every TeamLeadCheckTicks per bot: publish the own pool; the first bot of the team to arrive in a window also elects and keeps the order.</summary>
		void TeamLeadTick()
		{
			if (!TeamLeadActive)
			{
				teamLeadEngaged = false;
				return;
			}

			var now = World.WorldTick;
			if (!BotPeriod.CarryDue(ref teamLeadNextCheck, now, Info.TeamLeadCheckTicks))
				return;

			teamLeadKey ??= TeamLeadKeyOf(Player);
			var seats = TeamLeadSeats();
			if (seats.Count == 0)
			{
				teamLeadEngaged = false;
				return;
			}

			var team = teamLeadTeam = TeamLeadBoard.For(World, TeamLeadCA.TeamKey(seats));
			team.Publish(teamLeadKey, unitsHangingAroundTheBase.Count, TeamLeadPoolValue(), now);
			if (now >= team.NextEvalTick)
			{
				team.NextEvalTick = now + Info.TeamLeadCheckTicks;
				var before = team.Election.Leader;
				team.Election = TeamLeadCA.Elect(seats, team.Election, now, Info.TeamLeadLeadPercent, Info.TeamLeadHoldTicks);
				if (team.Election.Leader != before)
					AIUtils.BotDebug("AI ({0}): team leader {1} -> {2} at {3}", Player.ClientIndex, before ?? "none", team.Election.Leader ?? "none", now);

				// A leader that cannot form a wave (no idle units, no known target) must not stall a team of holding followers: after
				// TeamLeadStallTicks without any order the post goes to the seat with the largest idle pool (the stall window restarts either way).
				if (team.Election.Leader != null
					&& TeamLeadCA.Stalled(now, System.Math.Max(System.Math.Max(team.LastOrderTick, team.Election.SinceTick), initialAttackDelay), Info.TeamLeadStallTicks))
				{
					team.LastOrderTick = now;
					team.Election = TeamLeadCA.Handover(seats, team.Election, k => team.PoolUnitsOf(k, now, Info.TeamLeadPoolMaxAgeTicks), now);
					if (team.Election.Leader != before)
						AIUtils.BotDebug("AI ({0}): team leader stalled, handed over to {1} at {2}", Player.ClientIndex, team.Election.Leader, now);
				}

				TeamLeadMaintainOrder(team, now);
			}

			teamLeadEngaged = TeamLeadCA.Engaged(seats, team.Election);
			teamLeadIsLeader = teamLeadEngaged && team.Election.Leader == teamLeadKey;
		}

		// Order upkeep (once per window per team): cancel a stale or orphaned order, release the blob when it is gathered or the window is over.
		void TeamLeadMaintainOrder(TeamLeadTeam team, int now)
		{
			var order = team.Order;
			if (order == null || order.Cancelled || order.Pushed)
				return;

			order.Prune(o => o is SquadCA q && q.IsValid && q.FuzzyStateMachine?.CurrentState is TeamLeadGatherStateCA);
			if (order.LeaderKey != team.Election.Leader || now - order.IssueTick > Info.TeamLeadOrderMaxTicks
				|| (order.ReportCount == 0 && now - order.IssueTick > 2 * Info.TeamLeadCheckTicks))
			{
				order.Cancelled = true;
				return;
			}

			var (committed, arrived) = order.Totals();
			if (TeamLeadCA.PushDue(now, order.IssueTick, Info.TeamLeadGatherMinTicks, Info.TeamLeadGatherMaxTicks, committed, arrived, Info.TeamLeadArrivePercent))
				order.PushTick = now;
		}

		/// <summary>The team's open order, or null.</summary>
		TeamLeadOrder TeamLeadOpenOrder()
		{
			var order = teamLeadTeam?.Order;
			return order != null && order.Open(World.WorldTick, Info.TeamLeadLateJoinTicks) ? order : null;
		}

		/// <summary>
		/// CreateAttackForce asks this before it forms a wave. True = the wave is not formed now. A follower forms one only for an open order
		/// (it never attacks on its own); the leader needs a real contribution of its own. Defence, DEFEND-ALLY, retaliation, scouts and drops
		/// never come through CreateAttackForce.
		/// </summary>
		bool TeamLeadHoldsLaunch(out bool teamLaunch)
		{
			teamLaunch = false;
			if (!TeamLeadOn)
				return false;

			if (teamLeadIsLeader)
			{
				teamLaunch = true;
				return !TeamLeadCA.LeaderMayLaunch(unitsHangingAroundTheBase.Count, Info.TeamLeadLeaderMinUnits);
			}

			teamLaunch = true;
			return !TeamLeadCA.FollowerMayLaunch(TeamLeadOpenOrder() != null, unitsHangingAroundTheBase.Count, Info.TeamLeadJoinMinUnits);
		}

		/// <summary>The idle pool the launch bars measure: the whole team's for the leader, the own pool otherwise.</summary>
		(int Units, long Value) TeamLeadLaunchPool(int ownUnits, long ownValue)
		{
			if (!TeamLeadIsLeader || teamLeadTeam == null)
				return (ownUnits, ownValue);

			var team = teamLeadTeam.Pool(World.WorldTick, Info.TeamLeadPoolMaxAgeTicks);
			return (System.Math.Max(ownUnits, team.Units), System.Math.Max(ownValue, team.Value));
		}

		/// <summary>
		/// Before a quiet attack squad updates: it joins the open order, or the leader issues one with it, or - a follower with no order - it is
		/// stood down (no attack of its own). True = the squad was handled and must not update this pass.
		/// </summary>
		bool TeamLeadPrepare(SquadCA s)
		{
			// Guerrilla and harass squads of every bot form one joint group with its own target, apart from the main blob.
			if (s.IsValid && s.Type is SquadCAType.Guerrilla or SquadCAType.Harass && TeamLeadJointHarassOn)
			{
				TeamLeadShareHarassTarget(s);
				return false;
			}

			// The liveness (M13) response and urgent-attack squads and the breaker squad keep their own job: they are not team waves.
			if (!TeamLeadOn || s.Type != SquadCAType.Rush || !s.IsValid || ReferenceEquals(s, stalemateSquad) || s.M13Response
				|| ReferenceEquals(s, m13IntentSquad) || ReferenceEquals(s, m13ResponseSquad)
				|| !AggTeamIsQuietState(s.FuzzyStateMachine?.CurrentState) || AggTeamSquadDefends(s))
				return false;

			var now = World.WorldTick;
			var order = TeamLeadOpenOrder();
			if (order == null && teamLeadIsLeader)
				order = TeamLeadTryIssue(s, now);

			if (order != null)
			{
				DiagnosticSquadEvent(s, "team_join", reason: teamLeadIsLeader ? "leader" : "follower");
				s.FuzzyStateMachine.ChangeState(s, new TeamLeadGatherStateCA(order), false);
				return true;
			}

			if (teamLeadIsLeader)
				return false;

			DiagnosticSquadEvent(s, "team_hold");
			DismissSquad(s);
			return true;
		}

		// A quiet guerrilla/harass squad without a target takes the team's live harass target, so all of them hit the same one. With none live,
		// its own pick becomes the group's target. A shared target this bot cannot see is left alone (the squad plays as before); it never
		// replaces a live one, so the target does not flap between bots with different views.
		void TeamLeadShareHarassTarget(SquadCA s)
		{
			var team = teamLeadTeam;
			if (team == null || s.IsTargetValid || !AggTeamIsQuietState(s.FuzzyStateMachine?.CurrentState))
				return;

			var now = World.WorldTick;
			if (TeamLeadCA.HarassLive(now, team.HarassTick, Info.TeamLeadHarassTicks) && team.HarassTargetId != 0)
			{
				var shared = World.GetActorById(team.HarassTargetId);
				if (shared != null && !shared.IsDead && shared.IsInWorld)
				{
					if (IsPreferredObservedEnemyUnit(shared))
						s.TargetActor = shared;
					return;
				}
			}

			var pick = FindClosestEnemy(s.Units[0].Actor, s);
			if (pick == null)
				return;

			team.HarassTargetId = pick.ActorID;
			team.HarassTick = now;
			s.TargetActor = pick;
			DiagnosticSquadEvent(s, "team_harass", reason: "target=" + pick.ActorID);
		}

		TeamLeadOrder TeamLeadTryIssue(SquadCA s, int now)
		{
			// The same bar as the launch hold (TeamLeadLeaderMinUnits), so a leader that may form the team wave can always issue its order.
			if (teamLeadTeam == null || s.Units.Count < Info.TeamLeadLeaderMinUnits)
				return null;

			var intent = AggTeamPushIntent(s, now);
			if (intent == null)
				return null;

			var from = s.CenterPosition;
			var staged = ArmyStaging?.StagingCellNear(World.Map.CellContaining(from));
			var rally = staged.HasValue ? World.Map.CenterOfCell(staged.Value) : TeamLeadCA.RallyPoint(from, intent.Target, Info.TeamLeadRallyForwardCells);
			var order = teamLeadTeam.Issue(teamLeadKey, rally, intent.Target, intent.TargetActorId, now);
			DiagnosticSquadEvent(s, "team_order", reason: "seq=" + order.Seq);
			return order;
		}

		// ---- called by TeamLeadGatherStateCA (same assembly) ----

		/// <summary>The squad's contribution to the order: its value, and whether enough of it stands at the rally point.</summary>
		internal void TeamLeadReport(SquadCA squad, TeamLeadOrder order)
		{
			var radiusSquared = (long)WDist.FromCells(Info.TeamLeadArriveCells).LengthSquared;
			var near = squad.Units.Count(u => (u.Actor.CenterPosition - order.Rally).HorizontalLengthSquared <= radiusSquared);
			order.Report(squad, SquadValueOf(squad), near * 100 >= squad.Units.Count * Info.TeamLeadArrivePercent);
		}

		/// <summary>The order is gone or the squad cannot follow it: a follower returns its units to the pool, the leader's squad plays on as before.</summary>
		internal void TeamLeadAbandon(SquadCA squad)
		{
			DiagnosticSquadEvent(squad, "team_abandon");
			if (teamLeadIsLeader)
			{
				squad.FuzzyStateMachine.ChangeState(squad, new GroundUnitsIdleStateCA(), true);
				return;
			}

			DismissSquad(squad);
		}

		/// <summary>The blob is released: point the squad at the order's target (same actor for every member, else the nearest visible enemy there).</summary>
		internal bool TeamLeadResolveTarget(SquadCA squad, TeamLeadOrder order) =>
			AggTeamResolveTarget(squad, new TeamAttackIntent(order.Target, order.Rally, order.IssueTick, order.PushTick, order.IssueTick, SquadValueOf(squad), order.TargetActorId));
	}
}
