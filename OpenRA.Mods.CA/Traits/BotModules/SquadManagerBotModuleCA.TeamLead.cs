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
using OpenRA.Mods.CA.Traits.BotModules;
using OpenRA.Mods.CA.Traits.BotModules.Squads;
using OpenRA.Mods.Common.Pathfinder;
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
		AblationLatch ablateCadence;
		AblationLatch ablateFinishWeakest;
		AblationLatch ablatePincer;
		AblationLatch ablateFallbackPublish;
		AblationLatch ablateOrderSurvive;
		AblationLatch ablatePushBar;
		AblationLatch ablateGatherHold;
		AblationLatch ablateGraceHold;
		AblationLatch ablateRouteAll;
		AblationLatch ablateDefendRespectsOrder;
		AblationLatch ablateBoundedRetarget;
		AblationLatch ablateJoinAtForm;
		AblationLatch ablateBarJoinable;
		AblationLatch ablateJoinMerge;
		AblationLatch ablateGraceRally;
		AblationLatch ablateGatherRange;
		AblationLatch ablateHuntViaOrder;
		AblationLatch ablateIssueFloor;
		AblationLatch ablateRallyMid;
		AblationLatch ablatePincerDouble;
		AblationLatch ablateBarArrivedOnly;
		int teamLeadNextCheck;
		string teamLeadKey;
		TeamLeadTeam teamLeadTeam;
		bool teamLeadEngaged;
		bool teamLeadIsLeader;
		int teamLeadJoinedSeq;

		/// <summary>TEAM-LEAD-01 is on for this bot: the Info switch (with the AggTeam code it builds on) and the DEV ablation row.</summary>
		internal bool TeamLeadActive => Info.TeamLeadEnabled && Info.AggTeamEnabled && !ablateTeamLead.Off(Player, AblationIds.TeamLead);

		// TEAM-LEAD-02 (P0 FIX): the leader publishes the push on cadence (own switch + row); the weakest known enemy seat is finished first after the late start.
		bool TeamLeadCadenceActive => TeamLeadActive && Info.TeamLeadCadenceEnabled && !ablateCadence.Off(Player, AblationIds.TeamLeadCadence);

		internal bool TeamLeadCadenceOn => teamLeadEngaged && TeamLeadCadenceActive;

		internal bool TeamLeadFinishWeakestOn => TeamLeadOn && Info.TeamLeadFinishWeakestEnabled && World.WorldTick >= Info.LateEscalationStartTick
			&& !ablateFinishWeakest.Off(Player, AblationIds.TeamLeadFinishWeakest);

		// TEAM-PINCER-01: a team push with a big blob splits into two prongs on opposite sides of the target (own switch + row). Read by the leader
		// when it issues the order; the members follow the order's own prong data, so a member needs no switch of its own.
		internal bool TeamLeadPincerOn => TeamLeadOn && Info.TeamLeadPincerEnabled && !ablatePincer.Off(Player, AblationIds.TeamLeadPincer);

		// TEAM-BLOB-01: the no-order guarantee (needs the cadence publish) and the order that survives a leader re-election; each with its own switch + row.
		bool TeamLeadFallbackPublishOn => TeamLeadCadenceOn && Info.TeamLeadFallbackPublishEnabled && !ablateFallbackPublish.Off(Player, AblationIds.TeamLeadFallbackPublish);

		// TEAM-BLOB-01 push bar: no push (window or arrival) while the squads at the rally are below the WAVE-MIN bars; own switch + row.
		internal bool TeamLeadPushBarOn => TeamLeadOn && Info.TeamLeadPushBarEnabled && !ablatePushBar.Off(Player, AblationIds.TeamLeadPushBar);

		// TEAM-GATHER-01 (V04 RCA): a gathering squad keeps its order on contact, a held order is never cancelled for missing reports, and the M13 / stalemate
		// launches join an open order; each with its own switch + row.
		internal bool TeamLeadGatherHoldOn => TeamLeadOn && Info.TeamLeadGatherHoldEnabled && !ablateGatherHold.Off(Player, AblationIds.TeamLeadGatherHold);

		bool TeamLeadGraceHoldOn => TeamLeadOn && Info.TeamLeadGraceHoldEnabled && !ablateGraceHold.Off(Player, AblationIds.TeamLeadGraceHold);

		bool TeamLeadRouteAllOn => TeamLeadOn && Info.TeamLeadRouteAllEnabled && !ablateRouteAll.Off(Player, AblationIds.TeamLeadRouteAll);

		/// <summary>A team order is open and the route-all row is on: the liveness (M13) wave and the stalemate breaker wave hold, their units join the order.</summary>
		internal bool TeamLeadRoutesLaunch() => TeamLeadRouteAllOn && TeamLeadOpenOrder() != null;

		internal bool TeamLeadDefendRespectsOrderOn => TeamLeadOn && Info.TeamLeadDefendRespectsOrderEnabled && !ablateDefendRespectsOrder.Off(Player, AblationIds.TeamLeadDefendRespectsOrder);

		internal bool TeamLeadBoundedRetargetOn => TeamLeadOn && Info.TeamLeadBoundedRetargetEnabled && !ablateBoundedRetarget.Off(Player, AblationIds.TeamLeadBoundedRetarget);

		bool TeamLeadJoinAtFormOn => TeamLeadOn && Info.TeamLeadJoinAtFormEnabled && !ablateJoinAtForm.Off(Player, AblationIds.TeamLeadJoinAtForm);

		bool TeamLeadBarJoinableOn => TeamLeadOn && Info.TeamLeadBarJoinableEnabled && !ablateBarJoinable.Off(Player, AblationIds.TeamLeadBarJoinable);

		bool TeamLeadJoinMergeOn => TeamLeadOn && Info.TeamLeadJoinMergeEnabled && !ablateJoinMerge.Off(Player, AblationIds.TeamLeadJoinMerge);

		bool TeamLeadGraceRallyOn => TeamLeadOn && Info.TeamLeadGraceRallyEnabled && !ablateGraceRally.Off(Player, AblationIds.TeamLeadGraceRally);

		internal bool TeamLeadGatherRangeOn => TeamLeadOn && Info.TeamLeadGatherRangeEnabled && !ablateGatherRange.Off(Player, AblationIds.TeamLeadGatherRange);

		internal bool TeamLeadHuntViaOrderOn => TeamLeadOn && Info.TeamLeadHuntViaOrderEnabled && !ablateHuntViaOrder.Off(Player, AblationIds.TeamLeadHuntViaOrder);

		bool TeamLeadIssueFloorOn => TeamLeadOn && Info.TeamLeadIssueFloorEnabled && !ablateIssueFloor.Off(Player, AblationIds.TeamLeadIssueFloor);

		bool TeamLeadRallyMidOn => TeamLeadOn && Info.TeamLeadRallyMidEnabled && !ablateRallyMid.Off(Player, AblationIds.TeamLeadRallyMid);

		bool TeamLeadPincerDoubleOn => TeamLeadOn && Info.TeamLeadPincerDoubleEnabled && !ablatePincerDouble.Off(Player, AblationIds.TeamLeadPincerDouble);

		// CONSOLIDATE-02 D1: the push bar is the arrived blob against WAVE-MIN and a share of the COMMITTED units, not half of the team idle army; own switch + row.
		bool TeamLeadBarArrivedOnlyOn => TeamLeadOn && Info.TeamLeadBarArrivedOnlyEnabled && !ablateBarArrivedOnly.Off(Player, AblationIds.TeamLeadBarArrivedOnly);

		// The arrived bar replaces the old unit bar only while WAVE-MIN is active, so the two rows never compound in an ablation run (WAVE-MIN off = the old bar).
		bool TeamLeadArrivedBarOn => TeamLeadBarArrivedOnlyOn && WaveMinActive;

		// ---- TEAM-GATHER-02 ----

		// Ally defence leaves a squad that gathers for the open order, or was formed a moment ago (its first team pass has not run), to the order.
		internal bool TeamLeadKeepsSquad(SquadCA s) => TeamLeadCA.SquadKeptForOrder(TeamLeadDefendRespectsOrderOn,
			s.FuzzyStateMachine?.CurrentState is TeamLeadGatherStateCA, World.WorldTick, s.FormedTick, Info.TeamLeadFreshSquadTicks);

		// A pool unit that can join a ground wave: live, armed, ground (Mobile), neither artillery nor fire support (those never enter the Rush squad).
		bool TeamLeadJoinable(UnitWposWrapper u) => !unitCannotBeOrdered(u.Actor) && u.Actor.Info.HasTraitInfo<AttackBaseInfo>() && u.Actor.Info.HasTraitInfo<MobileInfo>()
			&& !Info.ArtilleryTypes.Contains(u.Actor.Info.Name) && !Info.FireSupportTypes.Contains(u.Actor.Info.Name);

		// This seat's Rush squad that gathers for the open order (the biggest one): new joinable units merge into it.
		SquadCA TeamLeadGatheringSquad()
		{
			SquadCA best = null;
			foreach (var s in Squads)
				if (s.IsValid && s.Type == SquadCAType.Rush && s.FuzzyStateMachine?.CurrentState is TeamLeadGatherStateCA && (best == null || s.Units.Count > best.Units.Count))
					best = s;

			return best;
		}

		// tl_join_merge: every joinable pool unit walks to the rally inside the seat's gathering squad (no new one-unit squads). False = no gathering squad or nothing to merge.
		bool TeamLeadMergeIntoGatherSquad(IBot bot, TeamLeadOrder order)
		{
			var squad = TeamLeadGatheringSquad();
			if (squad == null)
				return false;

			var joinable = unitsHangingAroundTheBase.Where(TeamLeadJoinable).ToList();
			if (joinable.Count == 0)
				return false;

			// CONSOLIDATE-02 Q5 (sq_grouped_join): no unit walks to the rally alone. The pool joins as a batch of at least the join floor (the same floor a new squad needs), all on
			// this tick; a smaller pool waits at home for its batch (the caller then stops at the same floor).
			if (ArmyGroupedJoinOn && !TeamLeadCA.BatchReady(joinable.Count, AllInCA.JoinMinUnits(order.AllIn, Info.TeamLeadJoinMinUnits, Info.TeamLeadAllInJoinMinUnits)))
				return false;

			var cell = World.Map.CellContaining(TeamLeadRallyOf(squad, order));
			foreach (var u in joinable)
			{
				squad.Units.Add(u);
				unitsHangingAroundTheBase.Remove(u);
				bot.QueueOrder(new Order("Move", u.Actor, Target.FromCell(World, cell), false));
			}

			ReconcileSquadLeases();
			foreach (var n in notifyIdleBaseUnits)
				n.UpdatedIdleBaseUnits(unitsHangingAroundTheBase);

			DiagnosticSquadEvent(squad, "team_merge", "seq=" + order.Seq + " units=" + joinable.Count);
			return true;
		}

		// A team Rush squad: not the stalemate breaker and not the liveness (M13) wave, which keep their own targeting.
		bool TeamLeadTeamSquad(SquadCA s) => s.Type == SquadCAType.Rush && !s.M13Response
			&& !ReferenceEquals(s, stalemateSquad) && !ReferenceEquals(s, m13IntentSquad) && !ReferenceEquals(s, m13ResponseSquad);

		// gt_bounded_retarget: a team Rush squad retargets only around its fight. A squad on its own finish hunt keeps it, unless the hunt goes through the order.
		internal bool TeamLeadBoundsRetarget(SquadCA s) => TeamLeadBoundedRetargetOn && TeamLeadTeamSquad(s) && (!s.IsHunting || TeamLeadHuntViaOrderOn) && !TeamLeadKeepsGoing(s);

		// A squad of a pushed order, or one at WAVE-MIN size or more, is exempt from the bound: the blob keeps going until the focus seat is dead (Opus audit: a bounded pushed
		// blob went idle after its first cleared area, was dismissed by the closed order, and the leader re-issued a rally at home).
		// CONSOLIDATE-02 Q4 (sq_order_engaged): under the persistent order a pushed squad keeps going while its order lives, whatever its size; never after the order is done.
		internal bool TeamLeadKeepsGoing(SquadCA s) => ArmyOrderEngagedOn ? ArmyKeepsGoing(s) : TeamLeadBoundedRetargetOn && TeamLeadTeamSquad(s)
			&& TeamLeadCA.KeepsGoing(s.TeamPushSeq > 0, s.Units.Count, WaveMinActive ? WaveMinBars().Units : 0);

		// The seat the pushed blob works on: the finish-the-weakest focus seat, else the owner of the last order target.
		Player TeamLeadGoOnPlayer()
		{
			var focus = TeamLeadFocusPlayer();
			if (focus != null)
				return focus;

			var order = teamLeadTeam?.Order;
			if (order == null || order.Cancelled || order.TargetActorId == 0)
				return null;

			var targeted = World.GetActorById(order.TargetActorId);
			return targeted != null && targeted.Owner != null && TeamLeadFocusEnemy(targeted.Owner) ? targeted.Owner : null;
		}

		// The next target of a squad that keeps going: the nearest visible enemy of the focus seat, else a remembered (fogged) building of that seat, else the nearest
		// visible enemy anywhere (the old pick), else (no focus seat) any remembered enemy building. False = nothing known at all: the caller stands the squad idle, never flees.
		internal bool TeamLeadGoOn(SquadCA s, Actor leader)
		{
			// CONSOLIDATE-02 Q4 (sq_order_engaged): a squad of the live pushed order takes the order's target (the leader re-picks it for the whole blob, X3); the own pick
			// below is only the fallback while the order target is gone or is the one this squad froze on.
			if (ArmyOrderEngagedOn && ArmyEngagedOrderOf(s) is { } engaged && ArmyFollowOrder(s, engaged))
				return true;

			var seat = TeamLeadGoOnPlayer();
			// The target that froze this blob (a cliff, water or wall between) is skipped for a while, so the retarget really picks another one.
			var skip = s.TeamSkipActor != 0 && World.WorldTick < s.TeamSkipUntil ? s.TeamSkipActor : 0u;
			var pick = skip != 0
				? TeamLeadNearestEnemyExcept(leader, seat, skip) ?? (seat != null ? TeamLeadNearestEnemyExcept(leader, null, skip) : null)
				: seat != null ? FindClosestEnemy(leader.Location, SquadValueOf(s), seat, s) : null;
			if (pick == null && seat != null && FoggedScans && FindFrozenEnemyTarget(leader.CenterPosition, -1, s, seat) is { } frozenSeat && frozenSeat.ID != skip)
			{
				s.Target = Target.FromFrozenActor(frozenSeat);
				return true;
			}

			if (skip == 0)
				pick ??= FindClosestEnemy(leader, s);
			if (pick != null)
			{
				s.TargetActor = pick;
				return true;
			}

			if (seat == null && FoggedScans && FindFrozenEnemyTarget(leader.CenterPosition, -1, s) is { } frozen && frozen.ID != skip)
			{
				s.Target = Target.FromFrozenActor(frozen);
				return true;
			}

			return false;
		}

		// The nearest visible preferred enemy (of <seat>, or any when null) other than <skipId> within TeamLeadSkipSearchCells of the leader: used only while a skip is active (a blob
		// froze on a target); a bounded circle query through the actor map, never a scan of every actor.
		Actor TeamLeadNearestEnemyExcept(Actor leader, Player seat, uint skipId) =>
			World.FindActorsInCircle(leader.CenterPosition, WDist.FromCells(Info.TeamLeadSkipSearchCells))
				.Where(a => a.ActorID != skipId && (seat == null || a.Owner == seat) && IsPreferredEnemyUnit(a) && IsNotHiddenUnit(a))
				.ClosestToIgnoringPath(leader.CenterPosition);

		// The stuck rule fired for a squad: a pushed blob skips the target it froze on and takes the next enemy (TeamLeadGoOn), instead of going idle with nothing changed.
		internal bool TeamLeadStuckRetarget(SquadCA s, Actor leader)
		{
			if (!TeamLeadKeepsGoing(s))
				return false;

			// A blob sieging a fogged building has a frozen target and no TargetActor: skip that building (its id) too.
			var frozenId = s.Target.Type == TargetType.FrozenActor && s.Target.FrozenActor != null ? s.Target.FrozenActor.ID : 0u;
			var skipId = TeamLeadCA.SkipIdOf(s.TargetActor?.ActorID ?? 0u, frozenId);
			if (skipId != 0)
			{
				s.TeamSkipActor = skipId;
				s.TeamSkipUntil = World.WorldTick + Info.TeamLeadSkipTicks;
			}

			return TeamLeadGoOn(s, leader);
		}

		// A pushed blob that stands within the scan radius of its valid target is sieging it (it does not move): the attack state stuck rule leaves it alone for at most
		// TeamLeadSiegeMaxTicks without a move (nobody may be able to fire at it); then TeamLeadStuckRetarget picks another target.
		internal bool TeamLeadSieging(SquadCA s, int stuckTicks)
		{
			if (!s.IsValid || s.TeamPushSeq <= 0 || !s.IsTargetValid || !TeamLeadKeepsGoing(s))
				return false;

			var scan = (long)WDist.FromCells(Info.AttackScanRadius).Length;
			return TeamLeadCA.SiegeHolds(true, true, (s.Units[0].Actor.CenterPosition - s.Target.CenterPosition).HorizontalLengthSquared, scan * scan, stuckTicks, Info.TeamLeadSiegeMaxTicks);
		}

		// tl_pincer_double: a pincer that is due to push with less than twice the WAVE-MIN bars gathered collapses into the single blob (each prong must meet WAVE-MIN).
		bool TeamLeadPincerThinCollapse(TeamLeadOrder order, int now)
		{
			// CONSOLIDATE-02 Q7 (sq_pincer_push): every pincer of the row is gated per arm, whatever the wide switch says (the plan shared the arms out to hold it).
			if (ArmyPincerPushOn)
				return TeamLeadPincerArmCollapse(order, now);

			// PINCER-02 (tl_pincer_wide): the gate is read per arm (each arm holds its arm bar) instead of the total of twice the bars.
			if (TeamLeadPincerWideOn)
				return TeamLeadPincerArmCollapse(order, now);

			if (!TeamLeadPincerDoubleOn || !WaveMinActive)
				return false;

			var (minUnits, minValue) = WaveMinBars();
			var (units, value) = order.CommittedTotals();
			if (TeamLeadCA.PincerGathered(units, value, minUnits, minValue))
				return false;

			order.PincerCollapsed = true;
			DiagnosticTeamEvent("team_pincer_collapse", "seq=" + order.Seq + " why=thin gathered=" + units + "/" + value + " bars=" + minUnits + "/" + minValue + " age=" + (now - order.IssueTick));
			return true;
		}

		// tl_rally_mid: the rally between the centroid of the allied bases and the target, snapped to a legal ground cell the leader sample can reach. False = keep the old rally.
		bool TeamLeadMidRally(WPos target, out WPos rally)
		{
			rally = default;
			var baseOf = new Dictionary<Player, WPos>();
			foreach (var a in World.ActorsHavingTrait<BaseBuilding>().Where(a => a.IsInWorld && !a.IsDead).OrderBy(a => a.ActorID))
				baseOf.TryAdd(a.Owner, a.CenterPosition);

			long x = 0;
			long y = 0;
			var n = 0;
			foreach (var p in World.Players)
			{
				if (p.NonCombatant || p.WinState != WinState.Undefined || (p != Player && Player.RelationshipWith(p) != PlayerRelationship.Ally))
					continue;

				WPos at;
				if (baseOf.TryGetValue(p, out var found))
					at = found;
				else if (p.HomeLocation != CPos.Zero)
					at = World.Map.CenterOfCell(p.HomeLocation);
				else
					continue;
				x += at.X;
				y += at.Y;
				n++;
			}

			if (n == 0)
				return false;

			var mid = TeamLeadCA.RallyBetween(new WPos((int)(x / n), (int)(y / n), 0), target, Info.TeamLeadRallyMidPercent);
			var map = World.Map;
			// TEAM-GATHER-02 c3: the staging cell is the leader own one; it counts only close to the mid point, else the mid point snapped to a legal ground cell is the rally.
			var staged = ArmyStaging?.StagingCellNear(map.CellContaining(mid));
			if (staged.HasValue && !TeamLeadCA.StagingNearMid(mid, map.CenterOfCell(staged.Value), Info.TeamLeadRallyMidStagingCells))
				staged = null;
			var sample = TeamLeadPincerSample();
			var mobile = sample?.TraitOrDefault<Mobile>();
			if (mobile == null)
			{
				rally = staged.HasValue ? map.CenterOfCell(staged.Value) : mid;
				return true;
			}

			bool Legal(CPos c) => map.Contains(c) && mobile.Locomotor.MovementCostForCell(c) != PathGraph.MovementCostForUnreachableCell;
			var cell = staged ?? mobile.NearestCell(map.CellContaining(mid), Legal, 1, 5);
			if (!Legal(cell) || !World.WorldActor.Trait<IPathFinder>().PathExistsForLocomotor(mobile.Locomotor, sample.Location, cell))
				return false;

			rally = map.CenterOfCell(cell);
			return true;
		}

		// tl_hunt_via_order: a team Rush squad does not hunt its own grid cell; the leader's order target is the one hunt cell.
		internal bool TeamLeadHuntsViaOrder(SquadCA s) => TeamLeadHuntViaOrderOn && TeamLeadTeamSquad(s);

		// The distance in cells inside which a gathering squad stops to fight: the squad's maximum weapon range (tl_gather_range), between the fixed
		// TeamLeadGatherFightCells and AttackScanRadius; the fixed distance when the row is off.
		internal WDist TeamLeadFightRange(SquadCA squad)
		{
			var fixedRange = WDist.FromCells(Info.TeamLeadGatherFightCells);
			if (!TeamLeadGatherRangeOn)
				return fixedRange;

			var max = 0;
			foreach (var u in squad.Units)
				foreach (var a in u.Actor.TraitsImplementing<AttackBase>())
					if (!a.IsTraitDisabled)
						max = System.Math.Max(max, a.GetMaximumRange().Length);

			return new WDist(System.Math.Clamp(max, fixedRange.Length, System.Math.Max(fixedRange.Length, WDist.FromCells(Info.AttackScanRadius).Length)));
		}

		bool TeamLeadOrderSurviveOn => TeamLeadOn && Info.TeamLeadOrderSurviveEnabled && !ablateOrderSurvive.Off(Player, AblationIds.TeamLeadOrderSurvive);

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
				if (!TeamLeadBarJoinableOn || TeamLeadJoinable(u))
					value += UnitValue(u.Actor);
			return value;
		}

		/// <summary>
		/// TEAM-BLOB-01 (tl_order_survive): a personality switch disables the old module (it throws every squad back to the pool) and enables this one mid-game.
		/// This module then takes the units over at once - its first role-assignment pass and its first team check are due on the next tick instead of
		/// after the start-up stagger - so it re-joins the open order quickly; the order's no-report grace counts from the last report (see TeamLeadMaintainOrder).
		/// </summary>
		void TeamLeadSwitchTakeover()
		{
			if (World.WorldTick <= 100 || !Info.TeamLeadOrderSurviveEnabled || !TeamLeadActive || ablateOrderSurvive.Off(Player, AblationIds.TeamLeadOrderSurvive))
				return;

			teamLeadNextCheck = 0;
			assignRolesTicks = BotPeriod.F1(World.WorldTick, 0, Info.AssignRolesInterval);
			DiagnosticTeamEvent("team_takeover", "tick=" + World.WorldTick);
		}

		/// <summary>One team pass, at most every TeamLeadCheckTicks per bot: publish the own pool; the first bot of the team to arrive in a window also elects and keeps the order.</summary>
		void TeamLeadTick(IBot bot)
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
			// TEAM-LEAD-02: the blob is every idle army unit of the member - the pool plus its formed quiet squads, not the pool alone.
			var (poolUnits, poolValue) = TeamLeadCadenceActive ? TeamLeadBlob() : (unitsHangingAroundTheBase.Count, TeamLeadPoolValue());
			team.Publish(teamLeadKey, poolUnits, poolValue, now, TeamLeadCadenceActive && TeamLeadIdleAtLimit());
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

			teamLeadAliveSeats = seats.Count(q => q.Alive);
			teamLeadEngaged = TeamLeadCA.Engaged(seats, team.Election, TeamLeadSoloOn ? 1 : 2);
			// CONSOLIDATE-02 Q4 (sq_launch_arbiter): the one launcher needs a leader, so a solo bot with no allied bot leads itself.
			if (!teamLeadEngaged && ArmyLaunchArbiterOn)
				teamLeadEngaged = TeamLeadCA.Engaged(seats, team.Election, 1);
			teamLeadIsLeader = teamLeadEngaged && team.Election.Leader == teamLeadKey;

			if (!teamLeadEngaged)
				return;

			// TEAM-LEAD-02: the leader keeps the finish focus and publishes the push from its own team check (no formed squad of its own
			// needed); every member forms its wave for an open order right away instead of waiting for its own attack-force timer.
			if (teamLeadIsLeader)
			{
				if (TeamLeadFinishWeakestOn)
					TeamLeadFocusTarget(AggTeamHomePos(), now, out _, out _);
				if (TeamLeadCadenceOn)
					TeamLeadPublish(team, now);

				// CONSOLIDATE-02 Q4 (sq_order_engaged): the leader re-picks the target of the pushed order when it is gone; every squad of the blob follows it.
				if (ArmyOrderEngagedOn)
					ArmyRetargetEngaged(now);
			}

			if (TeamLeadCadenceOn)
				TeamLeadJoin(bot, now);
		}

		// The member's idle army: the pool plus the units of its formed quiet wave squads (the squads TeamLeadPrepare joins to an order or dismisses).
		(int Units, long Value) TeamLeadBlob()
		{
			var units = TeamLeadBarJoinableOn ? unitsHangingAroundTheBase.Count(TeamLeadJoinable) : unitsHangingAroundTheBase.Count;
			var value = TeamLeadPoolValue();
			foreach (var s in Squads)
			{
				if (!TeamLeadWaveSquad(s))
					continue;

				units += s.Units.Count;
				value += SquadValueOf(s);
			}

			return (units, value);
		}

		// A quiet team-wave squad: Rush, idle, not one of the squads that keep their own job (breaker, M13 liveness) and not defending. HARASS-CAP-01
		// (tl_raider_recall, from the late start): a guerrilla/harass squad that is idle or still marching (no contact yet) counts as one too.
		bool TeamLeadWaveSquad(SquadCA s)
		{
			if (!s.IsValid || ReferenceEquals(s, stalemateSquad) || s.M13Response || ReferenceEquals(s, m13IntentSquad) || ReferenceEquals(s, m13ResponseSquad))
				return false;

			var state = s.FuzzyStateMachine?.CurrentState;
			if (s.Type == SquadCAType.Rush)
				return AggTeamIsQuietState(state) && !AggTeamSquadDefends(s);

			// A hunting squad (finish mode) has its own job; one in a fight finishes it (see TeamLeadRaiderInContact for the marching ones).
			return s.Type is SquadCAType.Guerrilla or SquadCAType.Harass && (TeamLeadRaiderRecallOn || TeamLeadAllInOrderOpen) && !s.IsHunting
				&& (AggTeamIsQuietState(state) || state is GroundUnitsAttackMoveStateCA) && !AggTeamSquadDefends(s);
		}

		// A marching raider squad (GroundUnitsAttackMoveStateCA) with an enemy inside its scan radius is already in contact: its own state switches to the
		// fight on its next tick, and the recall must not pull it out of it with Move orders. The same scan the gather state makes. Asked only when the
		// recall actually joins an order, never for the blob count.
		bool TeamLeadRaiderInContact(SquadCA s) =>
			s.FuzzyStateMachine?.CurrentState is GroundUnitsAttackMoveStateCA
			&& FindClosestEnemy(s.Units[0].Actor, WDist.FromCells(Info.AttackScanRadius), s) != null;

		// The member's idle army is at its tier's idle-unit limit (25 Noob .. 75 God, Hard 50): the unit builder stops queuing units there, so an idle
		// army at the limit is starving production. The unit builder owns the number (IdleAtLimit); the leader pushes at once when any member is at it.
		UnitBuilderBotModuleCA teamLeadUnitBuilder;

		bool TeamLeadIdleAtLimit()
		{
			teamLeadUnitBuilder ??= Player.PlayerActor.TraitsImplementing<UnitBuilderBotModuleCA>().FirstEnabledTraitOrDefault();
			return teamLeadUnitBuilder?.IdleAtLimit ?? false;
		}

		// The old per-bot attack cadence: the MinimumAttackForceDelay reset of CreateAttackForce, scaled the same way (late escalation included).
		int TeamLeadCadenceTicks() => LateDelay(BotPersonalityLeads.Scaled(
			MinAttackDelayResetTicks(Info, utilityAxesProviders),
			BotPersonalityLeads.Lean(leadProviders, "rush")));

		// Leader only. No open order, a ready blob (WAVE-MIN tier on the combined blob), cadence due: publish the order now.
		void TeamLeadPublish(TeamLeadTeam team, int now)
		{
			// V07 ALL-IN-01 (tl_all_in): the trigger that fired (cap, idle, won) or null; null and free of work with the switch off.
			var allIn = TeamLeadAllInWhy(team, now);

			// TEAM-BLOB-01: a rally (gather-only) order waits until the squads gathered for it pass the WAVE-MIN bars, then it becomes the attack order.
			if (TeamLeadOpenOrder() is { GatherOnly: true } rally)
			{
				TeamLeadPromoteRally(rally, now);
				return;
			}

			// ALL-IN-01: an attack order that is still open turns all-in when the trigger fires; its members then send every unit.
			if (allIn != null && TeamLeadOpenOrder() is { AllIn: false } open)
			{
				open.MarkAllIn(allIn);
				TeamLeadAllInRecord("all_in_start", open, "why=" + allIn + " upgraded=1");
			}

			if (now < initialAttackDelay || TeamLeadOpenOrder() != null)
				return;

			// TEAM-GATHER-02 (tl_grace_rally): no new order right after a cancel (V04: a rally order was re-published every ~500 ticks).
			if (TeamLeadGraceRallyOn && TeamLeadCA.RepublishHeld(now, team.LastCancelTick, Info.TeamLeadRepublishHoldTicks))
				return;

			var blob = team.Pool(now, Info.TeamLeadPoolMaxAgeTicks);
			var (minUnits, minValue) = WaveMinActive ? WaveMinBars() : (0, 0);
			var ready = TeamLeadCA.BlobReady(blob.Units, blob.Value, Info.TeamLeadJoinMinUnits, WaveMinCA.Limits(minUnits, minValue), minUnits, minValue);
			var interval = TeamLeadCA.PublishInterval(TeamLeadCadenceTicks(), Info.TeamLeadFallbackTicks);
			var why = TeamLeadCA.OrderDue(false, ready, now, team.LastOrderTick, interval, team.AnyAtCap(now, Info.TeamLeadPoolMaxAgeTicks) || allIn != null)
				? TeamLeadCA.PublishWhy.Ready
				: TeamLeadCA.PublishWhy.None;
			// USER rule (2026-11-24): nothing below WAVE-MIN ever attacks. The ready path above is the only attack publish; when it says no the blob is below the
			// bars, and the fallback re-publishes the RALLY order only (gather-only, see TeamLeadPromoteRally).
			if (why == TeamLeadCA.PublishWhy.None && TeamLeadFallbackPublishOn)
				why = TeamLeadCA.FallbackPublish(BlobLaunch01CA.TicksSinceOrder(now, team.LastOrderTick, initialAttackDelay), Info.TeamLeadFallbackTicks,
					blob.Units, Info.TeamLeadJoinMinUnits);
			// TEAM-GATHER-02 (tl_join_merge): a rally order only for a team army that already reaches WAVE-MIN; below that floor nothing is published and nothing churns.
			// (The ceil(idle / 2) term of BlobLaunch01CA.MinUnits is always met by the team own count here, so the unit bar of this floor is the WAVE-MIN units.)
			if (why == TeamLeadCA.PublishWhy.Gather && TeamLeadJoinMergeOn
				&& !TeamLeadCA.RallyFloorPassed(blob.Units, blob.Value, BlobLaunch01CA.MinUnits(minUnits, blob.Units), minValue))
				why = TeamLeadCA.PublishWhy.None;

			if (why == TeamLeadCA.PublishWhy.None)
				return;

			var home = AggTeamHomePos();
			teamLeadAllInPicking = allIn != null && why == TeamLeadCA.PublishWhy.Ready;
			var picked = TeamLeadPickTarget(home, now, out var target, out var targetId);
			teamLeadAllInPicking = false;
			if (!picked)
				return;

			var forced = TeamLeadCA.FallbackForced(now, team.LastOrderTick, Info.TeamLeadFallbackTicks);
			var order = TeamLeadIssue(home, target, targetId, now);
			if (why == TeamLeadCA.PublishWhy.Gather)
				order.MakeGatherOnly();
			else if (allIn != null)
				order.MarkAllIn(allIn);
			// Q1 fix-up (NIT 4): a gather-only order is a rally, not a launch; only attack orders are team launches.
			if (why != TeamLeadCA.PublishWhy.Gather)
				LaunchSourceRecord(null, "L20", "Team", blob.Units);

			DiagnosticTeamEvent("team_order", "seq=" + order.Seq + " why=" + why.ToString().ToLowerInvariant() + " blob=" + blob.Units + "/" + blob.Value + " target=" + targetId
				+ " pincer=" + order.HasProngs + " leader=" + teamLeadKey);
			if (order.AllIn)
				TeamLeadAllInRecord("all_in_start", order, "why=" + order.AllInWhy + " blob=" + blob.Units + "/" + blob.Value + " target=" + targetId + " hunt=" + (targetId == 0 ? 1 : 0));
			AIUtils.BotDebug("AI ({0}): team push published seq {1} at {2}: blob {3}/{4}, target {5}{6}", Player.ClientIndex, order.Seq, now, blob.Units, blob.Value,
				targetId, forced ? " (fallback)" : "");
		}

		// Leader only. The squads of a rally order have reported what they hold; once that passes the WAVE-MIN bars (units AND value, the same bars as the ready
		// path) the order becomes the attack order: its gather window restarts and it takes the leader's current target. Below the bars it stays a rally order.
		void TeamLeadPromoteRally(TeamLeadOrder order, int now)
		{
			var (units, value) = order.CommittedTotals();
			var (minUnits, minValue) = WaveMinActive ? WaveMinBars() : (0, 0);
			if (!TeamLeadCA.BlobReady(units, value, Info.TeamLeadJoinMinUnits, WaveMinCA.Limits(minUnits, minValue), minUnits, minValue))
				return;

			if (!TeamLeadPickTarget(AggTeamHomePos(), now, out var target, out var targetId))
			{
				target = order.Target;
				targetId = order.TargetActorId;
			}

			order.Promote(now, target, targetId);
			DiagnosticTeamEvent("team_promote", "seq=" + order.Seq + " committed=" + units + "/" + value + " target=" + targetId + " leader=" + teamLeadKey);
		}

		// Every member (the leader too) forms its wave for the open order at once. Retried each window until the pool actually goes into a squad.
		void TeamLeadJoin(IBot bot, int now)
		{
			var order = TeamLeadOpenOrder();

			// CONSOLIDATE-02 Q4 (Opus D3): after the push a persistent order takes reinforcements only as a WAVE-MIN group (never a unit that walks to the target alone).
			if (order is { Persistent: true, Pushed: true })
			{
				ArmyReinforce(bot, order, now);
				return;
			}

			if (order == null || (!AllInCA.JoinsAgain(order.AllIn, teamLeadJoinedSeq, order.Seq) && !order.GatherOnly && !order.Extending) || now < initialAttackDelay)
				return;

			// A rally order takes the new units again and again (they walk to the rally and wait there), but only real groups: no one-unit squads.
			unitsHangingAroundTheBase.RemoveAll(u => unitCannotBeOrdered(u.Actor));
			// TEAM-GATHER-02 (tl_join_merge): for a rally or held order the joinable units merge into the seat's gathering squad; a new squad forms only from a real group.
			if (TeamLeadJoinMergeOn && (order.GatherOnly || order.Extending || order.AllIn))
			{
				if (TeamLeadMergeIntoGatherSquad(bot, order))
					return;

				if (unitsHangingAroundTheBase.Count(TeamLeadJoinable) < AllInCA.JoinMinUnits(order.AllIn, Info.TeamLeadJoinMinUnits, Info.TeamLeadAllInJoinMinUnits))
					return;
			}

			// An all-in order counts the joinable units only (two artillery pieces would form an empty squad each check).
			if ((order.AllIn ? unitsHangingAroundTheBase.Count(TeamLeadJoinable) : unitsHangingAroundTheBase.Count)
				< AllInCA.JoinMinUnits(order.AllIn, teamLeadIsLeader && !order.GatherOnly && !order.Extending ? Info.TeamLeadLeaderMinUnits : Info.TeamLeadJoinMinUnits, Info.TeamLeadAllInJoinMinUnits))
				return;

			// ALL-IN-01: a pushed all-in order takes a re-join only as a WAVE-MIN group; a smaller one waits in the pool (never a squad that walks to the target alone).
			if (order.AllIn && order.Pushed && !TeamLeadAllInRejoinGroup())
				return;

			var before = unitsHangingAroundTheBase.Count;
			CreateAttackForce(bot);
			if (order.AllIn && unitsHangingAroundTheBase.Count < before)
				TeamLeadAllInRecord("all_in_join", order, "sent=" + (before - unitsHangingAroundTheBase.Count) + " left=" + unitsHangingAroundTheBase.Count);
			if (unitsHangingAroundTheBase.Count < before && (!TeamLeadJoinMergeOn || Squads.Any(q => q.IsValid && q.Type == SquadCAType.Rush && q.FormedTick == now)))
				teamLeadJoinedSeq = order.Seq;
		}

		// Where the order goes, from the leader's own view: the weakest seat after the late start, else the nearest known enemy (the main target's
		// visible actors, any visible enemy, then a remembered building).
		bool TeamLeadPickTarget(WPos from, int now, out WPos target, out uint actorId)
		{
			// CONSOLIDATE-02 Q6 (sq_target_pick): the same tiers in the TargetPick order of the MAIN purpose.
			if (ArmyTargetPickOn)
				return TeamLeadPickMain(from, now, out target, out actorId);

			if (TeamLeadFocusTarget(from, now, out target, out actorId))
				return true;

			var mainTarget = EffectiveMainTarget();
			var seen = mainTarget != null ? FindClosestEnemy(World.Map.CellContaining(from), TeamLeadOpenRisk, mainTarget) : null;
			if (seen == null)
			{
				var source = unitsHangingAroundTheBase.Count > 0 ? unitsHangingAroundTheBase[0].Actor : activeUnits.FirstOrDefault(a => !a.IsDead && a.IsInWorld);
				seen = source != null ? FindClosestEnemy(source) : null;
			}

			if (seen != null)
			{
				target = seen.CenterPosition;
				actorId = seen.ActorID;
				return true;
			}

			if (FoggedScans && FindFrozenEnemyTarget(from, -1) is { } frozen)
			{
				target = frozen.CenterPosition;
				actorId = frozen.ID;
				return true;
			}

			// TEAM-GATHER-02 (tl_hunt_via_order): late game, nothing known: the blob hunts ONE cell, the order target, instead of each squad hunting its own.
			// ALL-IN-01: an all-in order with no known target hunts too (whatever the finish mode says), and from a ground unit: the first pool unit may be an aircraft with no locomotor.
			if (((TeamLeadHuntViaOrderOn && FinishModeActive) || teamLeadAllInPicking)
				&& FinishHuntCell(teamLeadAllInPicking ? TeamLeadHuntSample() : unitsHangingAroundTheBase.Count > 0 ? unitsHangingAroundTheBase[0].Actor : activeUnits.FirstOrDefault(a => !a.IsDead && a.IsInWorld)) is { } hunt)
			{
				target = World.Map.CenterOfCell(hunt);
				actorId = 0;
				return true;
			}

			actorId = 0;
			return false;
		}

		// No risk gate on the leader's pick: the bars of the order are the wave minimum and the gather, not the squad risk gate.
		const int TeamLeadOpenRisk = 1_000_000_000;

		TeamLeadOrder TeamLeadIssue(WPos from, WPos target, uint targetActorId, int now)
		{
			var staged = ArmyStaging?.StagingCellNear(World.Map.CellContaining(from));
			var rally = staged.HasValue ? World.Map.CenterOfCell(staged.Value) : TeamLeadCA.RallyPoint(from, target, Info.TeamLeadRallyForwardCells);
			// TEAM-GATHER-02 c2 (tl_rally_mid): not the leader own staging cell; a point toward the target from the allied base centroid, so followers do not walk across the map.
			if (TeamLeadRallyMidOn && TeamLeadMidRally(target, out var mid))
				rally = mid;
			var order = teamLeadTeam.Issue(teamLeadKey, rally, target, targetActorId, now);
			// CONSOLIDATE-02 Q4 (sq_order_engaged): the order stays open through Push and Engaged until the blob is spent (Done).
			if (ArmyOrderEngagedOn)
				order.MakePersistent();
			TeamLeadPincerPlan(order, from, target, now);
			return order;
		}

		// TEAM-PINCER-01 (leader only, once per order, no per-tick work): a blob of at least twice the WAVE-MIN tier splits into two prongs with the
		// SAME target. Each prong gets its own staging point, one on each side of the target (terrain-checked for the leader's own locomotor with
		// the pathfinder - the terrain is public knowledge, so nothing here reads what the bot has not seen). No target far enough, no legal
		// staging point on either side, or two points too close together = no prongs: the order is the single blob it always was.
		void TeamLeadPincerPlan(TeamLeadOrder order, WPos from, WPos target, int now)
		{
			if (!TeamLeadPincerOn)
				return;

			// CONSOLIDATE-02 Q7 (sq_pincer_push): the pincer is decided at push time on the blob that arrived (TeamLeadPincerAtPush), not here on the pool at issue.
			if (ArmyPincerPushOn)
				return;

			var blob = teamLeadTeam.Pool(now, Info.TeamLeadPoolMaxAgeTicks);
			var (minUnits, minValue) = WaveMinActive ? WaveMinBars() : (0, 0);
			var wide = TeamLeadPincerWideOn;
			var blobOk = wide
				? PincerWideCA.PincerBlob(blob.Units, blob.Value, Info.TeamLeadJoinMinUnits, WaveMinCA.Limits(minUnits, minValue), minUnits, minValue, Info.TeamLeadPincerArmPercent)
				: TeamLeadCA.PincerBlob(blob.Units, blob.Value, Info.TeamLeadJoinMinUnits, WaveMinCA.Limits(minUnits, minValue), minUnits, minValue);
			if (!blobOk || !TeamLeadCA.PincerTargetFar(from, target, Info.TeamLeadPincerMinTargetCells))
				return;

			var sample = TeamLeadPincerSample();
			var mobile = sample?.TraitOrDefault<Mobile>();
			if (mobile == null)
				return;

			var map = World.Map;
			var pathFinder = World.WorldActor.Trait<IPathFinder>();
			var home = sample.Location;
			bool Legal(CPos c) => map.Contains(c) && mobile.Locomotor.MovementCostForCell(c) != PathGraph.MovementCostForUnreachableCell;

			// A staging point outside the scan radius: a gathering squad fights what it sees there and leaves the order.
			var standOff = System.Math.Max(Info.TeamLeadPincerStandOffCells, Info.AttackScanRadius + 4);
			CPos? Stage(int side)
			{
				foreach (var candidate in TeamLeadCA.PincerStaging(from, target, side, standOff, Info.TeamLeadPincerSpreadDegrees))
				{
					var cell = mobile.NearestCell(map.CellContaining(candidate), Legal, 1, 3);
					if (Legal(cell) && pathFinder.PathExistsForLocomotor(mobile.Locomotor, home, cell))
						return cell;
				}

				return null;
			}

			CPos? first;
			CPos? second;
			if (wide)
			{
				// PINCER-02: the first legal PAIR of the fanned-out candidates whose real approach separation is 90-180 degrees (no exact opposite sides).
				var listA = PincerWideCA.Candidates(from, target, -1, standOff, Info.TeamLeadPincerSpreadDegrees);
				var listB = PincerWideCA.Candidates(from, target, 1, standOff, Info.TeamLeadPincerSpreadDegrees);
				WPos? Resolve(List<WPos> list, int i)
				{
					var cell = mobile.NearestCell(map.CellContaining(list[i]), Legal, 1, 3);
					return Legal(cell) && pathFinder.PathExistsForLocomotor(mobile.Locomotor, home, cell) ? map.CenterOfCell(cell) : null;
				}

				if (PincerWideCA.PickPair(target, listA.Count, i => Resolve(listA, i), listB.Count, i => Resolve(listB, i), Info.TeamLeadPincerMinAngleDegrees,
					Info.TeamLeadPincerMinSeparationCells, out var pointA, out var pointB, out _))
				{
					first = map.CellContaining(pointA);
					second = map.CellContaining(pointB);
				}
				else
					first = second = null;
			}
			else
			{
				first = Stage(-1);
				second = Stage(1);
			}

			if (first == null || second == null)
			{
				AIUtils.BotDebug("AI ({0}): pincer skipped at {1}: no legal staging point on {2}", Player.ClientIndex, now, first == null ? "side A" : "side B");
				return;
			}

			var rally0 = map.CenterOfCell(first.Value);
			var rally1 = map.CenterOfCell(second.Value);
			if (!TeamLeadCA.PincerSeparated(rally0, rally1, Info.TeamLeadPincerMinSeparationCells))
			{
				AIUtils.BotDebug("AI ({0}): pincer skipped at {1}: staging points too close", Player.ClientIndex, now);
				return;
			}

			order.SetProngs(rally0, rally1);
			TeamLeadPincerRecord("pincer_planned", order, "blob=" + blob.Units + "/" + blob.Value);
			AIUtils.BotDebug("AI ({0}): pincer published seq {1} at {2}: blob {3}/{4}, staging {5} / {6}", Player.ClientIndex, order.Seq, now, blob.Units, blob.Value,
				first.Value, second.Value);
		}

		// A ground unit of the leader's idle army (the pool first, then its quiet wave squads): the locomotor the staging points are checked for.
		Actor TeamLeadPincerSample()
		{
			foreach (var u in unitsHangingAroundTheBase)
				if (!unitCannotBeOrdered(u.Actor) && u.Actor.TraitOrDefault<Mobile>() != null)
					return u.Actor;

			foreach (var s in Squads)
			{
				if (!TeamLeadWaveSquad(s))
					continue;

				foreach (var u in s.Units)
					if (!unitCannotBeOrdered(u.Actor) && u.Actor.TraitOrDefault<Mobile>() != null)
						return u.Actor;
			}

			return null;
		}

		/// <summary>Where this squad gathers for the order: its prong's staging point on a pincer (the first ask shares the squad out), else the order's one rally point.</summary>
		internal WPos TeamLeadRallyOf(SquadCA squad, TeamLeadOrder order)
		{
			if (!order.HasProngs)
				return order.Rally;

			// A collapsed pincer keeps the squads where they were sent; one that arrives after the collapse gathers at the plain rally.
			var prong = order.PincerCollapsed ? order.ProngOrNone(squad) : order.ProngOf(squad, SquadValueOf(squad));
			return prong < 0 ? order.Rally : order.ProngRally(prong);
		}

		bool TeamLeadFocusEnemy(Player p) =>
			p != Player && p.WinState == WinState.Undefined && !p.NonCombatant && Player.RelationshipWith(p) == PlayerRelationship.Enemy;

		/// <summary>
		/// The finish-the-weakest target (switch ON, after the late start): the known building of the team's focus seat nearest to
		/// <paramref name="from"/>. The focus is the weakest known enemy seat and stays until it dies; the pick is refreshed once per team check.
		/// False = not active, or no enemy building known.
		/// </summary>
		bool TeamLeadFocusTarget(WPos from, int now, out WPos target, out uint actorId)
		{
			target = default;
			actorId = 0;
			var team = teamLeadTeam;
			if (team == null || !TeamLeadFinishWeakestOn)
				return false;

			if (TeamLeadCA.FocusDue(now, team.FocusEvalTick, Info.TeamLeadCheckTicks))
			{
				team.FocusEvalTick = now;
				TeamLeadRefreshFocus(team, from, now);
			}

			if (!team.FocusHasTarget)
				return false;

			target = team.FocusTarget;
			actorId = team.FocusTargetId;
			return true;
		}

		void TeamLeadRefreshFocus(TeamLeadTeam team, WPos from, int now)
		{
			var known = TeamLeadKnownBuildings();
			var seats = new List<(int Seat, int Buildings)>(known.Count);
			foreach (var kv in known)
				seats.Add(((int)kv.Key.PlayerActor.ActorID, kv.Value.Count));

			var pick = TeamLeadCA.PickWeakest(seats, team.FocusSeat, team.FocusSince, now, Info.TeamLeadFocusMaxTicks);
			if (pick != team.FocusSeat)
			{
				AIUtils.BotDebug("AI ({0}): team finishes seat {1} (weakest, {2} known buildings) at {3}", Player.ClientIndex, pick,
					seats.Where(s => s.Seat == pick).Select(s => s.Buildings).DefaultIfEmpty(0).First(), now);
				team.FocusSeat = pick;
				team.FocusSince = now;
			}
			else if (pick >= 0 && now - team.FocusSince >= Info.TeamLeadFocusMaxTicks)
				team.FocusSince = now;

			team.FocusHasTarget = false;
			if (pick < 0)
				return;

			foreach (var kv in known)
			{
				if ((int)kv.Key.PlayerActor.ActorID != pick)
					continue;

				var best = long.MaxValue;
				foreach (var (id, pos) in kv.Value)
				{
					var d = (long)(pos - from).HorizontalLengthSquared;
					if (d >= best)
						continue;

					best = d;
					team.FocusHasTarget = true;
					team.FocusTarget = pos;
					team.FocusTargetId = id;
				}

				break;
			}
		}

		// Fog-honest: the enemy buildings (walls aside) the bot sees now or remembers in its frozen-actor layer, per living enemy seat.
		Dictionary<Player, List<(uint Id, WPos Pos)>> TeamLeadKnownBuildings()
		{
			var known = new Dictionary<Player, List<(uint Id, WPos Pos)>>();
			var counted = new HashSet<uint>();

			void Add(Player owner, uint id, WPos pos)
			{
				if (!counted.Add(id))
					return;
				if (!known.TryGetValue(owner, out var list))
					known[owner] = list = new List<(uint, WPos)>();
				list.Add((id, pos));
			}

			foreach (var a in World.ActorsHavingTrait<Building>())
			{
				if (a.IsDead || !a.IsInWorld || a.Owner == null || !TeamLeadFocusEnemy(a.Owner) || a.Info.HasTraitInfo<LineBuildInfo>() || !a.CanBeViewedByPlayer(Player))
					continue;

				Add(a.Owner, a.ActorID, a.CenterPosition);
			}

			var layer = Player.FrozenActorLayer;
			if (layer != null)
				foreach (var fa in layer.FrozenActorsInRegion(World.Map.AllCells))
				{
					if (!fa.IsValid || fa.Hidden || !TeamLeadFocusEnemy(fa.Owner) || !fa.Info.HasTraitInfo<BuildingInfo>() || fa.Info.HasTraitInfo<LineBuildInfo>())
						continue;

					Add(fa.Owner, fa.ID, fa.CenterPosition);
				}

			return known;
		}

		// The team's finish focus as a player, or null (not active, none picked, or the seat is gone).
		Player TeamLeadFocusPlayer()
		{
			var seat = teamLeadTeam?.FocusSeat ?? -1;
			if (seat < 0 || !TeamLeadFinishWeakestOn)
				return null;

			foreach (var p in World.Players)
				if ((int)p.PlayerActor.ActorID == seat)
					return TeamLeadFocusEnemy(p) ? p : null;

			return null;
		}

		// Order upkeep (once per window per team): cancel a stale or orphaned order, release the blob when it is gathered or the window is over.
		void TeamLeadMaintainOrder(TeamLeadTeam team, int now)
		{
			var order = team.Order;
			if (order == null || order.Cancelled)
				return;

			// CONSOLIDATE-02 Q4 (sq_order_engaged): a persistent pushed order lives on (Push -> Engaged -> Done); any other pushed order is left to its squads as before.
			if (order.Pushed)
			{
				if (order.Persistent)
					ArmyMaintainEngaged(team, order, now);
				return;
			}

			// A report stays only while its squad is in the gather state (tl_gather_hold keeps a fighting squad in it: the fight is no state change).
			order.Prune(o => o is SquadCA q && q.IsValid && q.FuzzyStateMachine?.CurrentState is TeamLeadGatherStateCA);
			// An order published by the leader's team check has no squad yet: the members form theirs on their next check and report from the gather
			// state one squad pass later, so the no-report grace is longer while TEAM-LEAD-02 publishes (Info switch; the row only changes the publish).
			var reportGrace = (Info.TeamLeadCadenceEnabled ? 5 : 2) * Info.TeamLeadCheckTicks;

			// TEAM-BLOB-01: a leader re-election hands the order to the new leader; the squads gathering for it carry on.
			if (order.LeaderKey != team.Election.Leader && team.Election.Leader != null && TeamLeadOrderSurviveOn)
			{
				DiagnosticTeamEvent("team_handover", "seq=" + order.Seq + " from=" + order.LeaderKey + " to=" + team.Election.Leader);
				order.HandOver(team.Election.Leader);
			}

			// CONSOLIDATE-02 D1 (tl_bar_arrived_only): a held push that reached its timeout goes out when the arrived blob passes WAVE-MIN, instead of being dissolved below.
			if (order.LeaderKey == team.Election.Leader && !order.GatherOnly && TeamLeadBarTimeoutPush(order, now))
				return;

			var cancel = order.LeaderKey != team.Election.Leader ? "leader_changed"
				: order.Extending && !order.Persistent && TeamLeadCA.PushBarDissolve(now, order.PushBarSince, Info.TeamLeadPushBarMaxTicks) ? "below_bar"
				: order.Extending && order.Persistent && TeamLeadCA.PushBarDissolve(now, order.PushBarSince, TeamLeadCA.PersistentGatherCap(Info.ArmyGatherMaxTicks, Info.TeamLeadPushBarMaxTicks)) ? "gather_cap"
				: !order.GatherOnly && !order.Extending && now - order.IssueTick > Info.TeamLeadOrderMaxTicks ? "max_age"
				: TeamLeadCA.GraceHolds(TeamLeadGraceHoldOn, order.Extending) ? null
				: TeamLeadCA.RallyGraceHolds(TeamLeadGraceRallyOn, order.GatherOnly) ? (now - order.IssueTick > Info.TeamLeadOrderMaxTicks ? "max_age" : null)
				: order.ReportCount == 0 && now - (TeamLeadOrderSurviveOn ? HarassCapCA.NoReportSince(order.IssueTick, order.LastReportTick) : order.IssueTick) > reportGrace ? "no_reports"
				: null;
			if (cancel != null)
			{
				order.Cancelled = true;
				order.CancelReason = cancel;
				if (cancel != "leader_changed")
					team.LastCancelTick = now;
				DiagnosticTeamEvent("team_cancel", "seq=" + order.Seq + " why=" + cancel + " age=" + (now - order.IssueTick) + " reports=" + order.ReportCount + " leader=" + order.LeaderKey
					+ " elected=" + (team.Election.Leader ?? "none"));
				return;
			}

			// TEAM-BLOB-01: a rally order only gathers; the leader's publish pass promotes it (TeamLeadPromoteRally) or it is cancelled for another reason.
			if (order.GatherOnly)
				return;

			// TEAM-PINCER-01: two prongs hold at their own staging points; both push on the same tick, when both are ready or the first ready one
			// has waited the hold for the other. A prong too light (squads missing or lost) collapses the pincer: one blob, the rules below.
			if (order.Pincer)
			{
				var (committed0, arrived0) = order.Totals(0);
				var (committed1, arrived1) = order.Totals(1);
				if (now - order.IssueTick >= Info.TeamLeadGatherMinTicks && !TeamLeadCA.PincerBalanced(committed0, committed1, Info.TeamLeadPincerMinSharePercent))
				{
					order.PincerCollapsed = true;
					AIUtils.BotDebug("AI ({0}): pincer seq {1} collapsed at {2}: prongs {3}/{4}", Player.ClientIndex, order.Seq, now, committed0, committed1);
				}
				else
				{
					var ready0 = TeamLeadCA.ProngReady(committed0, arrived0, Info.TeamLeadArrivePercent);
					var ready1 = TeamLeadCA.ProngReady(committed1, arrived1, Info.TeamLeadArrivePercent);
					if (ready0 || ready1)
						order.NoteReady(now);

					if (TeamLeadCA.PincerPushDue(now, order.IssueTick, Info.TeamLeadGatherMinTicks, Info.TeamLeadPincerGatherMaxTicks, Info.TeamLeadPincerHoldTicks,
						ready0, ready1, order.FirstReadyTick) && !TeamLeadPincerThinCollapse(order, now) && !TeamLeadPushHeld(order, now))
					{
						order.PushTick = now;
						TeamLeadAllInPushRecord(order, now);
						order.PushReason = ready0 && ready1 ? "pincer_ready" : now - order.IssueTick >= Info.TeamLeadPincerGatherMaxTicks ? "pincer_window" : "pincer_hold";
						DiagnosticTeamEvent("team_push", "seq=" + order.Seq + " why=" + order.PushReason + " age=" + (now - order.IssueTick) + " prongs=" + committed0 + "/" + committed1);
						TeamLeadPincerRecord("pincer_fired", order, "why=" + order.PushReason + " prongs=" + order.ProngUnits(0) + "/" + committed0 + "," + order.ProngUnits(1) + "/" + committed1);
						AIUtils.BotDebug("AI ({0}): pincer seq {1} push at {2}: ready {3}/{4}, prongs {5}/{6}", Player.ClientIndex, order.Seq, now, ready0, ready1, committed0, committed1);
					}

					return;
				}
			}

			var (committed, arrived) = order.Totals();
			if (TeamLeadCA.PushDue(now, order.IssueTick, Info.TeamLeadGatherMinTicks, Info.TeamLeadGatherMaxTicks, committed, arrived, Info.TeamLeadArrivePercent)
				&& !TeamLeadPushHeld(order, now))
			{
				// CONSOLIDATE-02 Q7 (sq_pincer_push): the push is due and the push bar passed - an arrived blob that can field two arms becomes a pincer instead of pushing.
				if (ArmyPincerPushOn && TeamLeadPincerAtPush(order, now))
					return;

				order.PushTick = now;
				TeamLeadAllInPushRecord(order, now);
				order.PushReason = now - order.IssueTick >= Info.TeamLeadGatherMaxTicks ? "window" : "arrived";
				DiagnosticTeamEvent("team_push", "seq=" + order.Seq + " why=" + order.PushReason + " age=" + (now - order.IssueTick) + " committed=" + committed + " arrived=" + arrived);
			}
		}

		// TEAM-BLOB-01 push bar (USER rule 2026-11-24: bots never trickle): a push that is due is held back while the squads standing at the rally are below the
		// WAVE-MIN units or value bar (the same bars as the ready path). The order stays at the rally and keeps merging new groups; after TeamLeadPushBarMaxTicks
		// of hold it is dissolved (cancel reason below_bar) - a small blob never pushes. True = hold, do not push now.
		bool TeamLeadPushHeld(TeamLeadOrder order, int now)
		{
			if (!TeamLeadPushBarOn)
				return false;

			var (units, value) = order.ArrivedTotals();
			var (minUnits, minValue) = WaveMinActive ? WaveMinBars() : (0, 0);

			// The team's idle army = what waits in the blob plus what the order already holds (gathered or on the way); half of it is the blob launch minimum's second term.
			// CONSOLIDATE-02 D1 (tl_bar_arrived_only): that term is dropped; the bar is the arrived units against WAVE-MIN and a share of the units COMMITTED to the order
			// (the squads assigned to it, never the home pool). The idle count stays for the hold record.
			var committedUnits = order.CommittedTotals().Units;
			var teamIdle = (teamLeadTeam?.Pool(now, Info.TeamLeadPoolMaxAgeTicks).Units ?? 0) + committedUnits;
			var needUnits = TeamLeadArrivedBarOn ? TeamLeadCA.ArrivedBarUnits(minUnits, committedUnits, Info.TeamLeadBarArrivedPercent) : BlobLaunch01CA.MinUnits(minUnits, teamIdle);
			if (TeamLeadCA.PushBarPassed(units, value, Info.TeamLeadJoinMinUnits, needUnits, minValue))
				return false;

			if (!order.Extending)
				DiagnosticTeamEvent("team_push_hold", "seq=" + order.Seq + " arrived=" + units + "/" + value + " bars=" + needUnits + "/" + minValue + " idle=" + teamIdle + " age=" + (now - order.IssueTick));

			// TEAM-GATHER-01 (tl_grace_hold): a held push counts as life of the order, so the no-report grace cannot run out under it.
			if (TeamLeadGraceHoldOn)
				order.LastReportTick = now;

			order.HoldPush(now);
			return true;
		}

		// CONSOLIDATE-02 D1 (tl_bar_arrived_only): the gather timeout of the push bar (TeamLeadPushBarMaxTicks after the first hold). The held order pushes the blob that has
		// arrived when it passes the plain WAVE-MIN bars (a pincer too: its prongs push from where they stand); below WAVE-MIN the cancel below dissolves it as before.
		bool TeamLeadBarTimeoutPush(TeamLeadOrder order, int now)
		{
			if (!TeamLeadArrivedBarOn)
				return false;

			var (units, value) = order.ArrivedTotals();
			var (minUnits, minValue) = WaveMinBars();
			if (!TeamLeadCA.BarTimeoutPush(now, order.PushBarSince, Info.TeamLeadPushBarMaxTicks, units, value, Info.TeamLeadJoinMinUnits, minUnits, minValue))
				return false;

			order.PushTick = now;
			TeamLeadAllInPushRecord(order, now);
			order.PushReason = "bar_timeout";
			DiagnosticTeamEvent("team_push", "seq=" + order.Seq + " why=" + order.PushReason + " age=" + (now - order.IssueTick) + " committed=" + order.CommittedTotals().Units
				+ " arrived=" + units + " pincer=" + (order.Pincer ? 1 : 0));
			return true;
		}

		// TEAM-BLOB-01 telemetry (DEV only; the writer is null in a release build): one record for a team-level fact (order, push, cancel, handover).
		// squadId: a squad-level fact joins that squad's rows (DiagnosticSquadId); null keeps the team row.
		void DiagnosticTeamEvent(string evt, string reason, string squadId = null)
		{
			var w = DiagnosticWriter;
			if (w == null)
				return;

			w.TryEnqueueSquadEvent(new BotPassiveSquadEventSnapshot
			{
				Tick = World.WorldTick, SquadId = squadId ?? ((Info.InstanceName ?? "sq") + "-team"), Generation = 1, Event = evt, Reason = reason, LeaseOwner = LeaseOwner
			});
		}

		/// <summary>The team's open order, or null.</summary>
		TeamLeadOrder TeamLeadOpenOrder()
		{
			var order = teamLeadTeam?.Order;
			// Push bar on: nothing advances alone, so a share formed after the push does not follow it (it waits for the next order).
			return order != null && order.Open(World.WorldTick, TeamLeadPushBarOn ? 0 : Info.TeamLeadLateJoinTicks) ? order : null;
		}

		/// <summary>
		/// CreateAttackForce asks this before it forms a wave. True = the wave is not formed now. A follower forms one only for an open order
		/// (it never attacks on its own); the leader needs a real contribution of its own. Defence, DEFEND-ALLY, retaliation, scouts and drops
		/// never come through CreateAttackForce.
		/// </summary>
		bool TeamLeadHoldsLaunch(out bool teamLaunch)
		{
			teamLaunch = false;

			// CONSOLIDATE-02 Q4 (sq_launch_arbiter): the one launcher. No ground wave forms outside the open team order, the leader's included (X4); after the push only a
			// WAVE-MIN group joins (D3). Before the first team check nothing forms at all.
			if (ArmyLaunchArbiterOn)
			{
				teamLaunch = true;
				return !TeamLeadOn || !ArmyMayJoin();
			}

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
			// CONSOLIDATE-02 Q4 (sq_order_engaged): a squad of the pushed order reports what it holds (the order's Done test) and follows the leader's retarget.
			if (ArmyOrderEngagedOn)
				ArmyEngagedStep(s);

			// HARASS-CAP-01 (tl_raider_recall): from the late start an idle or marching raider squad joins the open team order (TeamLeadWaveSquad is false
			// for a raider unless the recall is on); with no order open it raids as before.
			// CONSOLIDATE-02 Q4: a pushed persistent order takes no raider (it would walk to the target alone, D3); the raiders join the next order's rally.
			if (!ArmyRaidersStayOut() && s.IsValid && s.Type is SquadCAType.Guerrilla or SquadCAType.Harass && TeamLeadWaveSquad(s) && TeamLeadOpenOrder() is { } recalled && !TeamLeadRaiderInContact(s))
			{
				DiagnosticSquadEvent(s, "team_join", reason: "raider_recall seq=" + recalled.Seq);
				s.FuzzyStateMachine.ChangeState(s, new TeamLeadGatherStateCA(recalled), false);
				return true;
			}

			// Guerrilla and harass squads of every bot form one joint group with its own target, apart from the main blob.
			if (s.IsValid && s.Type is SquadCAType.Guerrilla or SquadCAType.Harass && TeamLeadJointHarassOn)
			{
				TeamLeadShareHarassTarget(s);
				return false;
			}

			// The liveness (M13) response and urgent-attack squads and the breaker squad keep their own job: they are not team waves.
			if (!TeamLeadOn || !TeamLeadWaveSquad(s))
				return false;

			// CONSOLIDATE-02 Q4 (sq_launch_arbiter): a quiet team squad joins the open order, re-engages the pushed one it belongs to, or goes back to the pool; it never
			// issues an order of its own (L19) and never launches from its Idle state (L09-L11).
			if (ArmyLaunchArbiterOn)
				return ArmyPrepare(s);

			// TEAM-GATHER-02 c3 (gt_bounded_retarget): a squad of a pushed order is not taken back into a team order and its blob is not dismissed (the closed order has no place
			// for it): it goes on to the next enemy of the focus seat. With nothing known at all it waits idle (the hunt via order), still not dismissed.
			var goesOn = s.TeamPushSeq > 0 && TeamLeadKeepsGoing(s);
			if (goesOn)
			{
				// Throttled to once per TeamLeadGoOnTicks per squad: with a skip active and nothing visible the pick scans every actor (TeamLeadNearestEnemyExcept), so the cost
				// is one scan per pushed idle squad per interval, never per tick.
				if (!TeamLeadCA.GoOnDue(World.WorldTick, s.TeamGoOnTick, Info.TeamLeadGoOnTicks))
					return false;

				s.TeamGoOnTick = World.WorldTick;
				if (TeamLeadGoOn(s, s.Units[0].Actor))
				{
					DiagnosticSquadEvent(s, "team_goon", reason: "seq=" + s.TeamPushSeq);
					s.FuzzyStateMachine.ChangeState(s, new GroundUnitsAttackMoveStateCA(), false);
					return true;
				}
			}

			var now = World.WorldTick;
			var order = TeamLeadOpenOrder();
			// A pushed squad with nothing known to attack may still join an order that is open, but it never issues a new rally from the leader (CM: no re-issue at home).
			if (order == null && teamLeadIsLeader && !goesOn)
				order = TeamLeadTryIssue(s, now);

			if (order != null)
			{
				DiagnosticSquadEvent(s, "team_join", reason: teamLeadIsLeader ? "leader" : "follower");
				s.TeamPushSeq = 0;
				s.FuzzyStateMachine.ChangeState(s, new TeamLeadGatherStateCA(order), false);
				if (order.HasProngs)
					DiagnosticSquadEvent(s, "team_prong", reason: "prong=" + order.ProngOrNone(s) + (order.PincerCollapsed ? " collapsed" : ""));
				return true;
			}

			if (teamLeadIsLeader || goesOn)
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

			// TEAM-LEAD-02: after the late start the group harasses the weakest enemy seat (the nearest of its actors); none seen = the nearest enemy as before.
			var focus = TeamLeadFocusPlayer();
			var pick = (focus != null ? FindClosestEnemy(s.Units[0].Actor.Location, SquadValueOf(s), focus, s) : null) ?? FindClosestEnemy(s.Units[0].Actor, s);
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

			// TEAM-GATHER-02 c2 (tl_issue_floor): a leader squad below WAVE-MIN issues an order only when the team joinable army reaches WAVE-MIN, else nothing is issued.
			if (TeamLeadIssueFloorOn && WaveMinActive)
			{
				var (minUnits, minValue) = WaveMinBars();
				var pool = teamLeadTeam.Pool(now, Info.TeamLeadPoolMaxAgeTicks);
				if (!TeamLeadCA.LeaderMayIssue(s.Units.Count, SquadValueOf(s), pool.Units, pool.Value, minUnits, minValue))
					return null;
			}

			var from = s.CenterPosition;
			WPos target;
			uint targetId;

			// TEAM-LEAD-02: the weakest seat first (after the late start), else the squad's own nearest-enemy pick.
			if (!TeamLeadFocusTarget(from, now, out target, out targetId))
			{
				var intent = AggTeamPushIntent(s, now);
				if (intent == null)
					return null;

				target = intent.Target;
				targetId = intent.TargetActorId;
			}

			var order = TeamLeadIssue(from, target, targetId, now);
			DiagnosticSquadEvent(s, "team_order", reason: "seq=" + order.Seq + " why=leader_squad pincer=" + order.HasProngs + " leader=" + teamLeadKey);
			LaunchSourceRecord(s, "L19", "Team", s.Units.Count);
			return order;
		}

		// ---- called by TeamLeadGatherStateCA (same assembly) ----

		/// <summary>The squad's contribution to the order: its value, and whether enough of it stands at the rally point.</summary>
		internal void TeamLeadReport(SquadCA squad, TeamLeadOrder order)
		{
			var radiusSquared = (long)WDist.FromCells(Info.TeamLeadArriveCells).LengthSquared;
			var rally = TeamLeadRallyOf(squad, order);
			var near = squad.Units.Count(u => (u.Actor.CenterPosition - rally).HorizontalLengthSquared <= radiusSquared);
			// TEAM-GATHER-02 c2 (tl_join_merge): arrival per unit, so units merged in at home do not undo the arrival of the units already at the rally.
			order.Report(squad, SquadValueOf(squad), near * 100 >= squad.Units.Count * Info.TeamLeadArrivePercent, squad.Units.Count, TeamLeadJoinMergeOn ? near : -1);
			order.LastReportTick = World.WorldTick;
		}

		// TEAM-BLOB-01 telemetry: a gathering squad fights what it meets (and leaves the order); a pushed one goes for the target.
		internal void TeamLeadNoteContact(SquadCA squad, TeamLeadOrder order) => DiagnosticSquadEvent(squad, "team_contact", "seq=" + order.Seq + " pushed=" + order.Pushed);

		internal void TeamLeadNoteGo(SquadCA squad, TeamLeadOrder order)
		{
			squad.TeamPushSeq = order.Seq;
			squad.TeamTargetSeq = order.TargetSeq;
			DiagnosticSquadEvent(squad, "team_go", "seq=" + order.Seq + " why=" + order.PushReason);
		}

		/// <summary>The order is gone or the squad cannot follow it: a follower returns its units to the pool, the leader's squad plays on as before.</summary>
		internal void TeamLeadAbandon(SquadCA squad, string why = null)
		{
			DiagnosticSquadEvent(squad, "team_abandon", why);

			// CONSOLIDATE-02 Q4 (sq_launch_arbiter): the leader's squad goes back to the pool too; an Idle leader squad would launch alone (X4).
			if (teamLeadIsLeader && !ArmyLaunchArbiterOn)
			{
				squad.FuzzyStateMachine.ChangeState(squad, new GroundUnitsIdleStateCA(), true);
				return;
			}

			DismissSquad(squad);
		}

		/// <summary>The blob is released: point the squad at the order's target (same actor for every member, else the nearest visible enemy there).</summary>
		internal bool TeamLeadResolveTarget(SquadCA squad, TeamLeadOrder order)
		{
			if (AggTeamResolveTarget(squad, new TeamAttackIntent(order.Target, order.Rally, order.IssueTick, order.PushTick, order.IssueTick, SquadValueOf(squad), order.TargetActorId)))
				return true;

			if (!TeamLeadCadenceOn)
				return false;

			// TEAM-LEAD-02: the pushed target is not in this bot's view (the leader saw it, this bot has not). The blob still goes: to the remembered
			// building if this bot has it, else to the order's position; on arrival the squad looks for the next target like a finished hunt.
			var remembered = order.TargetActorId != 0 ? Player.FrozenActorLayer?.FromID(order.TargetActorId) : null;
			if (remembered != null && remembered.IsValid && !remembered.Hidden)
			{
				squad.Target = Target.FromFrozenActor(remembered);
				return true;
			}

			var cell = World.Map.CellContaining(order.Target);
			squad.Target = Target.FromCell(World, cell);
			squad.HuntCell = cell;
			return true;
		}
	}
}
