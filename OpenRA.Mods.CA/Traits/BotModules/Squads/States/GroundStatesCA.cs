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
using OpenRA.Primitives;
using OpenRA.Traits;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.CA.Traits.BotModules.Squads
{
	abstract class GroundStateBaseCA : StateBaseCA
	{
		protected virtual bool ShouldFlee(SquadCA owner)
		{
			// Never suicide (maintainer 2026-09-28): with CP, turn back once the visible fight is predicted lost.
			if (owner.SquadManager.Info.UseCombatPredictor)
				return ShouldFlee(owner, enemies => owner.SquadManager.PredictsLoss(owner, enemies));

			return ShouldFlee(owner, enemies => !AttackOrFleeFuzzyCA.Default.CanAttack(owner.Units.ConvertAll(u => u.Actor), enemies));
		}

		// IMPL02-AGG-CORE (switch ON only, A2/A4): a committed squad that the legacy rule wants to pull out stays unless it is
		// decisively out-numbered with its coalition and engaged allies (review C1); the window only names the hold. OFF: false.
		protected static bool AggressionHolds(SquadCA owner) =>
			owner.SquadManager.AggressionOn && owner.SquadManager.AggressionHoldsInFight(owner) != AggressionPolicyCA.HoldReason.None;

		// IMPL02-AGG-CORE (switch ON only, A2 on the march): a remembered-defence verdict (siege Retreat, committed veto) is
		// cancelled only by the ratio reasons (Coalition, AllyEngaged; review C3), judged against what it observes at its target.
		// FREEZE9 CF seam: never for a squad that holds a unit the agg-team defence plan keeps home (CG off: never true).
		// OFF: false.
		protected static bool AggressionHoldsMarch(SquadCA owner) =>
			owner.SquadManager.AggressionOn && owner.IsTargetValid && !owner.SquadManager.AggressionSquadDefends(owner)
			&& owner.SquadManager.AggressionHoldsInFight(owner, owner.Target.CenterPosition) != AggressionPolicyCA.HoldReason.None;

		protected Actor FindClosestEnemy(SquadCA owner)
		{
			return owner.SquadManager.FindClosestEnemy(owner.Units.First().Actor, owner);
		}

		// 6c: pre-commit checks route through the risk-gated overloads — the squad's
		// unit value is compared to the remembered threat at each candidate's region.
		protected Actor FindClosestEnemy(SquadCA owner, bool riskCheck)
		{
			return riskCheck
				? owner.SquadManager.FindClosestEnemy(owner.Units.First().Actor, owner.SquadManager.SquadValueOf(owner), owner)
				: owner.SquadManager.FindClosestEnemy(owner.Units.First().Actor, owner);
		}

		protected Actor FindHighValueTarget(SquadCA owner)
		{
			return owner.SquadManager.FindHighValueTarget(owner.Units.First().Actor.CenterPosition);
		}

		protected Actor FindHighValueTarget(SquadCA owner, bool riskCheck)
		{
			return riskCheck
				? owner.SquadManager.FindHighValueTarget(owner.Units.First().Actor.CenterPosition, owner.SquadManager.SquadValueOf(owner))
				: owner.SquadManager.FindHighValueTarget(owner.Units.First().Actor.CenterPosition);
		}

		protected bool FindNewTarget(SquadCA owner, bool highValueCheck = false, bool riskCheck = false)
		{
			if (highValueCheck)
			{
				var highValueTargetRoll = BotRng.For(owner.Bot, nameof(GroundStateBaseCA)).Next(0, 100);

				if (owner.SquadManager.Info.HighValueTargetPriority > highValueTargetRoll)
				{
					var highValueTarget = FindHighValueTarget(owner, riskCheck);
					if (highValueTarget != null)
					{
						owner.TargetActor = highValueTarget;
						return true;
					}
				}
			}

			var closestEnemy = FindClosestEnemy(owner, riskCheck);
			if (closestEnemy != null)
			{
				owner.TargetActor = closestEnemy;
				return true;
			}

			// 6d fogged fallback: nothing visible — commit to an enemy building the
			// FrozenActorLayer remembers. The record self-invalidates when the
			// cell is re-observed empty, so a stale memory can't trap the squad.
			if (owner.SquadManager.FoggedScans)
			{
				var frozen = owner.SquadManager.FindFrozenEnemyTarget(
					owner.Units.First().Actor.CenterPosition,
					riskCheck ? owner.SquadManager.SquadValueOf(owner) : -1);

				if (frozen != null)
				{
					owner.Target = Target.FromFrozenActor(frozen);
					return true;
				}
			}

			// STALEMATE-01 finish mode (switch ON only, late game): nothing visible and nothing remembered - hunt the least recently
			// visited unseen grid cell the leader can reach instead of holding (a seat with no known building must still be found).
			if (owner.SquadManager.FinishModeActive && Stalemate01CA.Hunts(owner.Type) && !owner.SquadManager.TeamLeadHuntsViaOrder(owner))
			{
				var cell = owner.SquadManager.FinishHuntCell(owner.Units.First().Actor);
				if (cell != null)
				{
					owner.Target = Target.FromCell(owner.World, cell.Value);
					owner.HuntCell = cell;
					return true;
				}
			}

			return false;
		}
	}

	class GroundUnitsIdleStateCA : GroundStateBaseCA, IState
	{
		Actor leader;

		// IMPL02-AGG-CORE (switch ON only): the tick the current forward regroup began, -1 = not regrouping.
		int regroupSinceTick = -1;

		public void Activate(SquadCA owner) { }

		public void Tick(SquadCA owner)
		{
			if (!owner.IsValid)
				return;

			// The idle squad is committing to a proactive attack — gate it on the
			// remembered threat at the target's region (6c). Mid-fight retargets in
			// GroundUnitsAttackState stay ungated.
			if (!owner.IsTargetValid && !FindNewTarget(owner, true, riskCheck: true))
				return;

			if (owner.SquadManager.unitCannotBeOrdered(leader))
				leader = GetPathfindLeader(owner, owner.SquadManager.Info.SuggestedGroundLeaderLocomotor).Actor;

			var enemyUnits = owner.World.FindActorsInCircle(owner.Target.CenterPosition, WDist.FromCells(owner.SquadManager.Info.IdleScanRadius))
				.Where(owner.SquadManager.IsPreferredObservedEnemyUnit).ToList();

			if (enemyUnits.Count == 0)
			{
				// A FrozenActor target is itself the point of the attack — nothing
				// visible nearby doesn't mean nothing is there.
				if (owner.Target.Type == TargetType.FrozenActor || owner.IsHunting)
					{
						owner.SquadManager.LaunchSourceRecord("L09", owner.Type.ToString(), owner.Units.Count);
						owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsAttackMoveStateCA(), false);
					}
				else
					Retreat(owner, flee: false, rearm: true, repair: true);

				return;
			}

			// IMPL02-AGG-CORE (switch ON only): coalition bands, the forward regroup and the tiny-squad hold replace the
			// squad-alone engage-or-flee call below. AggressionOn is false on the OFF arm, which keeps the legacy path as is.
			if (owner.SquadManager.AggressionOn)
			{
				AggressionTick(owner, enemyUnits);
				return;
			}

			var engage = owner.SquadManager.Info.UseCombatPredictor
				? owner.SquadManager.PredictsWin(owner, enemyUnits)
				: AttackOrFleeFuzzyCA.Default.CanAttack(owner.Units.ConvertAll(u => u.Actor), enemyUnits);

			// INC-N combat veto (§12.31): the veto cancels a commit the provider predicts loses — the squad
			// takes the same retreat path as a losing fuzzy call. No provider = false, nothing changes.
			if (engage && owner.SquadManager.VetoEngage(owner, enemyUnits, alreadyCommitted: false, out _))
				engage = false;

			if (engage)
			{
				// 6f: assault waves stage before committing so slow units catch
				// up and the attack arrives as one wave, not a trickle.
				if (owner.Type == SquadCAType.Rush && owner.SquadManager.Info.StageBeforeAssault)
					{
						owner.SquadManager.LaunchSourceRecord("L09", owner.Type.ToString(), owner.Units.Count);
						owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsStageStateCA(), false);
					}
				else
					{
						owner.SquadManager.LaunchSourceRecord("L09", owner.Type.ToString(), owner.Units.Count);
						owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsAttackMoveStateCA(), false);
					}
			}

			// BOTPASSIVE (switch ON only): the live breaker wave defers this predicted-loss disengagement (guard_bypass).
			else if (owner.SquadManager.StalemateDefersRetreat(owner, "idle_engage"))
				{
					owner.SquadManager.LaunchSourceRecord("L09", owner.Type.ToString(), owner.Units.Count);
					owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsAttackMoveStateCA(), false);
				}
			else
			{
				owner.SquadManager.NoteRetreatStart(owner, "idle_engage");

				// A10 RETREAT-RALLY (switch ON only; both calls return at once while off): mark the flight, then rally to a friendly army if one qualifies.
				owner.SquadManager.RetreatRallyNoteFlight(owner);
				if (owner.SquadManager.TryRetreatRally(owner, "idle_engage", out var plan))
				{
					owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsRallyStateCA(plan), false);
					return;
				}

				Retreat(owner, flee: true, rearm: true, repair: true);
			}
		}

		// IMPL02-AGG-CORE A1-A3 (switch ON only). A1: the ratio counts the squad's coalition (AggressionFriends) and the
		// attack bar is AggressionEngageRatioPct. A2: Flee below AggressionFleeRatioPct, a bounded forward regroup in between.
		// A3: a tiny squad attacks only in the attack band and otherwise holds - it never runs the flee loop.
		void AggressionTick(SquadCA owner, List<Actor> enemyUnits)
		{
			var manager = owner.SquadManager;
			var info = manager.Info;

			// An idle squad is not committed; the attack band below starts a fresh commit.
			SquadManagerBotModuleCA.AggressionUncommit(owner);

			// FREEZE9 CF seam (Opus): a squad that holds a unit the agg-team defence plan keeps home starts no attack from idle;
			// it holds (no order, as the A3 hold) until the plan releases the unit. CG off: never true.
			if (manager.AggressionSquadDefends(owner))
			{
				regroupSinceTick = -1;
				return;
			}

			var friends = manager.AggressionFriends(owner, owner.Target.CenterPosition);
			var ratio = manager.AggressionRatio(owner, friends, enemyUnits);
			var band = AggressionPolicyCA.Band(ratio, manager.AggressionParam(nameof(SquadManagerBotModuleCAInfo.AggressionFleeRatioPct), owner), manager.AggressionParam(nameof(SquadManagerBotModuleCAInfo.AggressionEngageRatioPct), owner));

			// FREEZE10 DIAG-FIX-01 (CG seam; unchanged with CG off): the flee band is re-judged with the allied squads that are fighting
			// beside this one counted, as the in-fight rule does, so an idle squad does not abandon an engaged ally. The ratio that decided
			// is the one recorded (A6) for the retreat_start row.
			if (band == AggressionBandCA.Flee)
				(band, ratio) = manager.AggressionIdleAlliedBand(owner, friends, enemyUnits, band, ratio);

			var tiny = AggressionPolicyCA.IsTiny(owner.Units.Count, manager.SquadValueOf(owner),
				manager.AggressionParam(nameof(SquadManagerBotModuleCAInfo.AggressionMinDispatchUnits), owner), manager.AggressionParam(nameof(SquadManagerBotModuleCAInfo.AggressionMinDispatchValue), owner));

			// INC-N combat veto (§12.31), as on the legacy path: asked only for a commit, it turns it into a regroup (or a hold).
			// The finishing push ignores the veto, so it is not asked then (review L4).
			var vetoed = band == AggressionBandCA.Attack && !manager.AggressionFinishActive
				&& manager.VetoEngage(owner, enemyUnits, alreadyCommitted: false, out _);
			var decision = AggressionPolicyCA.IdleDecision(band, tiny, vetoed, manager.AggressionFinishActive);

			if (decision == AggressionBandCA.Attack)
			{
				regroupSinceTick = -1;
				manager.AggressionCommit(owner);
				if (owner.Type == SquadCAType.Rush && info.StageBeforeAssault)
					{
						owner.SquadManager.LaunchSourceRecord("L10", owner.Type.ToString(), owner.Units.Count);
						owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsStageStateCA(), false);
					}
				else
					{
						owner.SquadManager.LaunchSourceRecord("L10", owner.Type.ToString(), owner.Units.Count);
						owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsAttackMoveStateCA(), false);
					}

				return;
			}

			if (decision == null)
			{
				// A3 hold: no order, no retreat; the squad grows (guerrilla joins) or the band changes.
				regroupSinceTick = -1;
				return;
			}

			var site = "idle_engage";
			if (decision == AggressionBandCA.Regroup)
			{
				if (regroupSinceTick < 0)
					regroupSinceTick = owner.World.WorldTick;

				if (owner.World.WorldTick - regroupSinceTick < manager.AggressionParam(nameof(SquadManagerBotModuleCAInfo.AggressionRegroupMaxTicks), owner))
				{
					RegroupForward(owner);
					return;
				}

				// The bounded regroup ran out without reaching the attack band: the legacy retreat below, under its own reason
				// (Opus ruling d, review C2), so CHECK01 counts an expired regroup apart from a Flee-band retreat.
				regroupSinceTick = -1;
				site = "idle_regroup_expired";
			}

			// The flee band, or an expired regroup: the legacy predicted-loss retreat (breaker deferral first).
			if (manager.StalemateDefersRetreat(owner, site))
				owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsAttackMoveStateCA(), false);
			else
			{
				// A6 telemetry: what the coalition saw, for the retreat_start row (an idle squad has no commit, so no trade).
				manager.AggressionRecordEvidence(owner, band, ratio, enemyUnits, null);
				manager.NoteRetreatStart(owner, site);

				// A10 RETREAT-RALLY (switch ON only; both calls return at once while off): mark the flight, then rally to a friendly army if one qualifies.
				manager.RetreatRallyNoteFlight(owner);
				if (manager.TryRetreatRally(owner, site, out var plan))
				{
					owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsRallyStateCA(plan), false);
					return;
				}

				Retreat(owner, flee: true, rearm: true, repair: true);
			}
		}

		// A2: move to the forward regroup point (IdleScanRadius + AggressionRegroupMarginCells from the target, toward the
		// squad), never home. An unreachable point holds the squad where it is.
		void RegroupForward(SquadCA owner)
		{
			var manager = owner.SquadManager;
			var info = manager.Info;
			var map = owner.World.Map;
			var standoff = WDist.FromCells(info.IdleScanRadius + manager.AggressionParam(nameof(SquadManagerBotModuleCAInfo.AggressionRegroupMarginCells), owner)).Length;
			var point = AggressionPolicyCA.RegroupPoint(owner.CenterPosition, owner.Target.CenterPosition, standoff);
			var cell = map.Clamp(map.CellContaining(point));
			if (leader == null || !CheckReachability(leader, cell))
				return;

			var rally = map.CenterOfCell(cell);
			var radiusSquared = (long)WDist.FromCells(info.StageRadiusCells).LengthSquared;
			var movers = owner.Units.Select(u => u.Actor)
				.Where(a => (a.CenterPosition - rally).LengthSquared > radiusSquared).ToList();
			if (movers.Count > 0)
				QueueDeduped(owner, "Move", SquadOrderKey.ForCell("Move", cell), Target.FromCell(owner.World, cell), movers);
		}

		public void Deactivate(SquadCA owner) { }
	}

	class GroundUnitsStageStateCA : GroundStateBaseCA, IState
	{
		CPos stagingCell;
		int stageDeadlineTick;
		bool staged;

		public void Activate(SquadCA owner)
		{
			// WorldTick deadline — squad Update() runs on AttackForceInterval,
			// not per tick, so a decrementing counter would wait 50-100x too long.
			stageDeadlineTick = owner.World.WorldTick + owner.SquadManager.Info.StageTimeoutTicks;

			// Rally at the own building nearest the target; without one there is
			// nowhere to stage, so commit directly. A building farther from the
			// target than the squad already is would stage it backwards — commit
			// immediately in that case too.
			var buildings = owner.World.ActorsHavingTrait<Building>()
				.Where(a => a.Owner == owner.Bot.Player).ToList();

			if (buildings.Count == 0 || !owner.IsTargetValid)
			{
				{
					owner.SquadManager.LaunchSourceRecord("L11", owner.Type.ToString(), owner.Units.Count);
					owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsAttackMoveStateCA(), false);
				}
				return;
			}

			var targetPos = owner.Target.CenterPosition;
			var targetCell = owner.World.Map.CellContaining(targetPos);
			var nearest = buildings.MinBy(b => (b.Location - targetCell).LengthSquared);

			var squadPos = owner.CenterPosition;
			var buildingDist = (nearest.CenterPosition - targetPos).LengthSquared;
			var squadDist = (squadPos - targetPos).LengthSquared;
			if (buildingDist >= squadDist)
			{
				{
					owner.SquadManager.LaunchSourceRecord("L11", owner.Type.ToString(), owner.Units.Count);
					owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsAttackMoveStateCA(), false);
				}
				return;
			}

			stagingCell = nearest.Location;
			staged = true;

			foreach (var u in owner.Units)
				owner.Bot.QueueOrder(new Order("Move", u.Actor, Target.FromCell(owner.World, stagingCell), false));
		}

		public void Tick(SquadCA owner)
		{
			if (!owner.IsValid)
				return;

			if (!staged || !owner.IsTargetValid)
			{
				owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsIdleStateCA(), true);
				return;
			}

			// Contact during staging: commit to the fight at the rally point.
			var enemyActor = owner.SquadManager.FindClosestEnemy(owner.Units[0].Actor,
				WDist.FromCells(owner.SquadManager.Info.AttackScanRadius), owner);
			if (enemyActor != null)
			{
				owner.TargetActor = enemyActor;
				owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsAttackState(), false);
				return;
			}

			var radiusSquared = (long)WDist.FromCells(owner.SquadManager.Info.StageRadiusCells).LengthSquared;
			var rally = owner.World.Map.CenterOfCell(stagingCell);
			var assembled = owner.Units.Count(u =>
				(u.Actor.CenterPosition - rally).LengthSquared <= radiusSquared);

			if (assembled * 100 >= owner.Units.Count * owner.SquadManager.Info.StageAssemblePercent ||
				owner.World.WorldTick >= stageDeadlineTick)
				{
					owner.SquadManager.LaunchSourceRecord("L11", owner.Type.ToString(), owner.Units.Count);
					owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsAttackMoveStateCA(), false);
				}
		}

		public void Deactivate(SquadCA owner) { }
	}

	class SupportUnitsIdleStateCA : GroundStateBaseCA, IState
	{
		const int HoldTicks = 250;
		int holdTicks;

		public void Activate(SquadCA owner) { }

		public void Tick(SquadCA owner)
		{
			if (!owner.IsValid)
				return;

			var parent = owner.Parent;
			if (parent == null || !parent.IsValid)
			{
				parent = owner.SquadManager.FindAttachableAssault(owner);
				owner.Parent = parent;
			}

			if (parent == null)
			{
				// No assault to support: hold near home so medics stop charging
				// into the attack force like they did as generic ground units.
				if (--holdTicks <= 0)
				{
					GoToRandomOwnBuilding(owner);
					holdTicks = HoldTicks;
				}

				return;
			}

			// Trail the assault: units beyond SupportFollowRangeCells get a move
			// order toward it. Their own AutoTarget heals/repairs in reach.
			var followRangeSquared = (long)WDist.FromCells(owner.SquadManager.Info.SupportFollowRangeCells).LengthSquared;
			var parentPos = parent.CenterPosition;
			foreach (var u in owner.Units)
			{
				if ((u.Actor.CenterPosition - parentPos).LengthSquared <= followRangeSquared)
					continue;

				if (!owner.SquadManager.Info.UseSquadOrderDedup
					|| owner.OrderChanged(u.Actor, SquadOrderKey.ForCell("Move", owner.World.Map.CellContaining(parentPos))))
					owner.Bot.QueueOrder(new Order("Move", u.Actor, Target.FromPos(parentPos), false));
			}
		}

		public void Deactivate(SquadCA owner) { }
	}

	class GroundUnitsAttackMoveStateCA : GroundStateBaseCA, IState
	{
		const int MaxMakeWayPossibility = 4;
		const int MaxSquadStuckPossibility = 6;
		const int MakeWayTicks = 3;
		const int KickStuckTicks = 4;

		// Give tolerance for AI grouping team at start
		int shouldMakeWayPossibility = -(MaxMakeWayPossibility * 6);
		int shouldKickStuckPossibility = -(MaxSquadStuckPossibility * 6);
		int makeWay = 0;
		int kickStuck = 0;

		UnitWposWrapper leader = new(null);

		// 12.7 formation: rear-frontline stall tracking (fransbot chokepoint rule)
		WPos formationRearPos;
		int formationRearStallTicks;
		int formationRearHp = -1;

		// Formation hysteresis (2026-10-04, user-reported stutter): the hold/push split is a
		// dead band — a member holds only when it exceeds the lead by FormationHoldHysteresisCells
		// and resumes once it falls back under the lead. Orders are re-issued only when a member's
		// class or its quantized order target actually changed: per-tick identical Stop/AttackMove
		// orders cancelled the unit's MoveTo each tick and produced the stop-start stutter.
		enum FormationClass { Push, Hold, AntiAir, Scout, Trail, Retreat, RetreatRear, HurryUp }

		readonly Dictionary<Actor, (FormationClass Class, CPos Target)> formationOrders = new();
		bool leaderWaiting;
		Actor leaderOrderActor;
		CPos? leaderOrderCell;

		// Indirect/harass routing state
		List<CPos> currentRoute;
		int currentWaypointIndex;
		int lastWaypointUpdateTick;
		Target lastRoutingTarget;

		public void Activate(SquadCA owner)
		{
			// A fresh march carries no formation memory: every member re-orders once on
			// entry, then transitions only on real class/target changes.
			formationOrders.Clear();
			leaderWaiting = false;
			leaderOrderActor = null;
			leaderOrderCell = null;
		}

		// "The fight is on" — ONE definition shared by contact-first all-in (this state) and the
		// concave (§12.7a entry and commit): a visible enemy and a member are within weapon range of
		// each other, EITHER side's MaxRange. Seeing an enemy is not a fight: the concave stages just
		// outside range on purpose, so a sight-radius test would cancel it before it formed
		// (coordinator 2026-10-02). One bounded scan at the centroid (scan + spread, fog-honest via
		// VisibleEnemiesNear), then a per-pair range check. Returns the engaged enemy nearest to
		// any member, or null.
		internal static Actor NearestEngagedEnemy(SquadCA owner, WDist scanDist)
		{
			var rules = owner.World.Map.Rules;
			var members = new List<(WPos Pos, long Range)>(owner.Units.Count);
			long cx = 0, cy = 0;
			foreach (var u in owner.Units)
			{
				var p = u.Actor.CenterPosition;
				members.Add((p, BotUnitProfiles.Get(rules, u.Actor.Info).MaxRange.Length));
				cx += p.X;
				cy += p.Y;
			}

			if (members.Count == 0)
				return null;

			var centroid = new WPos((int)(cx / members.Count), (int)(cy / members.Count), 0);
			var spread = 0L;
			foreach (var m in members)
				spread = Math.Max(spread, (m.Pos - centroid).HorizontalLength);

			var bound = scanDist + new WDist((int)Math.Min(spread, int.MaxValue - scanDist.Length));
			var enemies = owner.SquadManager.VisibleEnemiesNear(centroid, bound);
			Actor nearest = null;
			var best = long.MaxValue;
			foreach (var e in enemies)
			{
				var enemyRange = (long)BotUnitProfiles.Get(rules, e.Info).MaxRange.Length;
				foreach (var m in members)
				{
					var reach = Math.Max(m.Range, enemyRange);
					var d = (e.CenterPosition - m.Pos).HorizontalLengthSquared;
					if (d <= reach * reach && d < best)
					{
						best = d;
						nearest = e;
					}
				}
			}

			return nearest;
		}

		public void Tick(SquadCA owner)
		{
			// Basic check
			if (!owner.IsValid)
				return;

			// Initialize leader. Optimize pathfinding by using a leader with specific locomotor.
			if (owner.SquadManager.unitCannotBeOrdered(leader.Actor))
				leader = GetPathfindLeader(owner, owner.SquadManager.Info.SuggestedGroundLeaderLocomotor);

			// STALEMATE-01 finish mode: a hunt cell reached with nothing found - take the next target (visible, remembered, or the next cell).
			if (owner.IsHunting && leader.Actor != null && Stalemate01CA.Arrived(
				(leader.Actor.Location - owner.HuntCell.Value).LengthSquared, owner.SquadManager.Info.FinishHuntArriveCells)
				&& !FindNewTarget(owner))
			{
				// TEAM-GATHER-02 (tl_hunt_via_order): the blob's hunt cell is reached and nothing is in view: back to idle, where the team pass takes the next order.
				owner.FuzzyStateMachine.ChangeState(owner, owner.SquadManager.TeamLeadHuntsViaOrder(owner) ? (IState)new GroundUnitsIdleStateCA() : new GroundUnitsFleeStateCA(), false);
				return;
			}

			if (!owner.IsTargetValid || !CheckReachability(leader.Actor, owner.World.Map.CellContaining(owner.Target.CenterPosition)))
			{
				// Harassers retarget high-value first when the target is gone (HV
				// roll + risk gate + frozen fallback live inside FindNewTarget); a
				// valid-but-unreachable target keeps the plain closest-enemy pick
				// so the squad cannot HV-reroll itself into a thrash loop.
				if (owner.Type == SquadCAType.Harass && !owner.IsTargetValid && !FindNewTarget(owner, highValueCheck: true, riskCheck: true))
				{
					owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsFleeStateCA(), false);
					return;
				}
				else if (owner.Type != SquadCAType.Harass || owner.IsTargetValid)
				{
					// TEAM-GATHER-02 (gt_bounded_retarget): a team Rush squad that lost its target looks for the next enemy only around its fight; with none there it goes back
					// to idle, where the team pass re-joins the open order or stands it down, instead of marching alone to the nearest enemy anywhere.
					var bounded = owner.SquadManager.TeamLeadBoundsRetarget(owner);
					if (owner.SquadManager.TeamLeadKeepsGoing(owner))
					{
						// TEAM-GATHER-02 c2/c3: the pushed blob keeps going: the next enemy of the focus seat (a remembered building of it when none is visible). With nothing known at
						// all it stands idle (the hunt via order / the team pass), it never flees.
						if (!owner.SquadManager.TeamLeadGoOn(owner, leader.Actor))
						{
							owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsIdleStateCA(), false);
							return;
						}
					}
					else
					{
						var targetActor = bounded
							? owner.SquadManager.FindClosestEnemy(leader.Actor, WDist.FromCells(owner.SquadManager.Info.AttackScanRadius), owner)
							: owner.SquadManager.FindClosestEnemy(leader.Actor, owner);
						if (targetActor != null)
							owner.TargetActor = targetActor;
						else if (bounded)
						{
							owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsIdleStateCA(), false);
							return;
						}
						else
						{
							owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsFleeStateCA(), false);
							return;
						}
					}
				}
			}

			// Switch to "GroundUnitsAttackState" if we encounter enemy units.
			var attackScanRadius = WDist.FromCells(owner.SquadManager.Info.AttackScanRadius);

			// Contact-first all-in (§12.7b, maintainer 2026-10-02): the concave, the assault fan and the
			// march holds are for BEFORE the first shot. Once the fight is on for ANY member (a visible
			// enemy and a member within weapon range of each other), the squad commits wholesale (Lanchester: staging while the local fight runs feeds
			// the enemy one prong at a time). It commits THROUGH the attack state, which attack-moves every
			// member on the same tick AND keeps focus fire, kiting and pull-back (coordinator 2026-10-02:
			// a grouped AttackMove from this state every tick never left it, so a Rush squad in contact
			// skipped the shipped group-A micro). ContactFirstAllIn is false on classic, the A/B reference.
			if (owner.SquadManager.Info.ContactFirstAllIn && owner.Type == SquadCAType.Rush && owner.IsTargetValid)
			{
				var contact = NearestEngagedEnemy(owner, attackScanRadius);
				if (contact != null)
				{
					owner.TargetActor = contact;
					owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsAttackState(), false);
					return;
				}
			}

			// CV (12.7a, unified with ATK-1 and the assault fan): before the first shot a Rush squad
			// deploys - the army shape (provider-armed concave against observed enemies) or the
			// objective shape (FormationMovement prongs around the squad target). ShouldEnter chooses.
			if (GroundUnitsConcaveStateCA.ShouldEnter(owner, out var deployShape))
			{
				owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsConcaveStateCA(deployShape), false);
				return;
			}

			var enemyActor = owner.SquadManager.FindClosestEnemy(leader.Actor, attackScanRadius, owner);
			if (enemyActor != null)
			{
				owner.TargetActor = enemyActor;
				if (owner.Type == SquadCAType.Guerrilla)
					{
						owner.SquadManager.LaunchSourceRecord("L15", owner.Type.ToString(), owner.Units.Count);
						owner.FuzzyStateMachine.ChangeState(owner, new GuerrillaUnitsHitState(), false);
					}
				else
					owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsAttackState(), false);
				return;
			}

			// CA-2 siege consult (§12.6): with no live enemy in reach, an advisor
			// may hold the squad at the remembered-defence stand-off line or pull
			// it out before a losing trade. Advance (or no advisor) changes nothing.
			if (owner.Type == SquadCAType.Rush || owner.Type == SquadCAType.Guerrilla || owner.Type == SquadCAType.Harass)
			{
				var siegeVerdict = owner.SquadManager.EvaluateSiege(owner, out var standOffCell);
				// SIEGE-FIX-01 S6: a siege retreat never dismisses the squad. It tries the retreat rally once, else pulls back to the stand-off line and holds.
				if (siegeVerdict == SiegeVerdict.Retreat && !AggressionHoldsMarch(owner))
				{
					owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsSiegeRetreatStateCA(standOffCell), false);
					return;
				}

				if (siegeVerdict == SiegeVerdict.StandOff && leader.Actor != null && owner.World.Map.Contains(standOffCell))
				{
					// Re-order only when the leader drifts off the hold line.
					if ((owner.World.Map.CellContaining(leader.Actor.CenterPosition) - standOffCell).LengthSquared > 4)
						foreach (var u in owner.Units)
							owner.Bot.QueueOrder(new Order("Move", u.Actor, Target.FromCell(owner.World, standOffCell), false));
					return;
				}

				// INC-N combat veto (§12.31): an already-committed approach can still be cancelled at the
				// lower abort line — the squad retreats instead of marching into the remembered wall.
				// No provider = false, nothing changes.
				if (siegeVerdict == SiegeVerdict.Advance && owner.IsTargetValid)
				{
					var vetoEnemies = owner.World.FindActorsInCircle(owner.Target.CenterPosition, WDist.FromCells(owner.SquadManager.Info.IdleScanRadius))
						.Where(owner.SquadManager.IsPreferredObservedEnemyUnit).ToList();
					// SIEGE-FIX-01 S7 (Opus ruling): the siege has already priced the remembered defences for this march, so the abort is skipped
					// only when it served a priced Advance AND no enemy is seen near the target; otherwise INC-N runs as before.
					if (!SiegeEvaluatorBotModule.SkipsMarchAbort(owner.SquadManager.SiegePricedAdvance(owner), vetoEnemies.Count))
						if (owner.SquadManager.VetoEngage(owner, vetoEnemies, alreadyCommitted: true, out _) && !AggressionHoldsMarch(owner))
						{
							owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsFleeStateCA(), false);
							return;
						}
				}
			}

			// Since units have different movement speeds, they get separated while approaching the target.
			// Let them regroup into tighter formation towards "leader".
			//
			// "occupiedArea" means the space the squad units will occupy (if 1 per Cell).
			// leader only stop when scope of "lookAround" is not covered all units;
			// units in "unitsHurryUp"  will catch up, which keep the team tight while not stuck.
			//
			// Imagining "occupiedArea" takes up a a place shape like square,
			// we need to draw a circle to cover the the enitire circle.
			var occupiedArea = (long)WDist.FromCells(owner.Units.Count).Length * 1024;

			// Kick stuck units: Kick stuck units that is blocked
			if (kickStuck > 0)
			{
				var stopUnits = new List<Actor>();
				var otherUnits = new List<Actor>();

				// Check if it is the leader stuck
				if (leader.Actor.CenterPosition == leader.WPos && !IsAttackingAndTryAttack(leader.Actor).IsFiring)
				{
					stopUnits.Add(leader.Actor);

					// GetPathfindLeader may return a fresh wrapper rather than the
					// instance stored in Units — remove by actor identity.
					owner.Units.RemoveAll(u => u.Actor == leader.Actor);
					owner.SquadManager.ReturnToIdlePool(leader.Actor);
					AIUtils.BotDebug("AI ({0}): Kick leader from squad.", owner.Bot.Player.ClientIndex);
				}

				// Check if it is the units stuck
				else
				{
					for (var i = 0; i < owner.Units.Count; i++)
					{
						var u = owner.Units[i];

						if (u.Actor == leader.Actor)
							continue;

						// If unit that is not in valid distance from leader nor firing at enemy,
						// we will check if it can reach the leader, or stuck due to unknow reason
						if ((u.Actor.CenterPosition - leader.Actor.CenterPosition).HorizontalLengthSquared >= 5 * occupiedArea
							&& (u.Actor.CenterPosition == u.WPos
							|| !AIUtils.PathExist(u.Actor, leader.Actor.Location, leader.Actor)))
						{
							stopUnits.Add(u.Actor);
							owner.Units.RemoveAt(i);
							owner.SquadManager.ReturnToIdlePool(u.Actor);
							i--;
						}
						else
						{
							u.WPos = u.Actor.CenterPosition;
							otherUnits.Add(u.Actor);
						}
					}

					if (stopUnits.Count > 0)
						AIUtils.BotDebug("AI ({0}): Kick ({1}) from squad.", owner.Bot.Player.ClientIndex, stopUnits.Count);
				}

				if (owner.Units.Count == 0)
					return;

				if (kickStuck > 1)
				{
					leader = GetPathfindLeader(owner, owner.SquadManager.Info.SuggestedGroundLeaderLocomotor);
					leader.WPos = leader.Actor.CenterPosition;
					owner.Bot.QueueOrder(new Order("AttackMove", leader.Actor, Target.FromPos(owner.Target.CenterPosition), false));
					owner.Bot.QueueOrder(new Order("Stop", null, false, groupedActors: stopUnits.ToArray()));
					owner.Bot.QueueOrder(new Order("AttackMove", null, Target.FromCell(owner.World, leader.Actor.Location), false, groupedActors: otherUnits.ToArray()));
					kickStuck--;
				}
				else if (kickStuck == 1)
				{
					shouldMakeWayPossibility = 0;
					shouldKickStuckPossibility = 0;
					leader = GetPathfindLeader(owner, owner.SquadManager.Info.SuggestedGroundLeaderLocomotor);

					// The end of "kickStuck": stop the leader for position record next tick
					owner.Bot.QueueOrder(new Order("Stop", leader.Actor, false));
					kickStuck = 0;
				}

				return;
			}

			// Make way for leader: Make sure the guide unit has not been blocked by the rest of the squad.
			if (makeWay > 0)
			{
				if (makeWay > 1)
				{
					var others = owner.Units.Where(u => u.Actor != leader.Actor).Select(u => u.Actor);
					owner.Bot.QueueOrder(new Order("Scatter", null, false, groupedActors: others.ToArray()));
					owner.Bot.QueueOrder(new Order("AttackMove", leader.Actor, Target.FromPos(owner.Target.CenterPosition), false));
					makeWay--;
				}
				else if (makeWay == 1)
				{
					shouldMakeWayPossibility = 0;
					shouldKickStuckPossibility = MaxSquadStuckPossibility / 2;

					// The end of "makeWay": stop the leader for position record next tick
					// set "makeWay" to -1 to inform that squad just make way for leader
					owner.Bot.QueueOrder(new Order("Stop", leader.Actor, false));
					makeWay = -1;
				}

				return;
			}

			// "leaderStopCheck" to see if leader move.
			// "leaderWaitCheck" to see if leader should wait squad members that left behind.
			var leaderStopCheck = leader.Actor.CenterPosition == leader.WPos;
			var leaderWaitCheck = owner.Units.Any(u => (u.Actor.CenterPosition - leader.Actor.CenterPosition).HorizontalLengthSquared > occupiedArea * 5);

			// To find out the stuck problem of the squad and deal with it.
			// 1. If leader cannot move and leader should wait, there may be squad members stuck.
			// 2. If leader cannot move but leader should go, leader is stuck.
			// -- Try make way for leader
			// -- If make way cannot solve this problem, we kick stuck unit
			// 3. If leader can move and leader should go, we consider this squad has no problem on stuck.
			(shouldMakeWayPossibility, shouldKickStuckPossibility) = MarchEvalCA.StuckCountersNext(
				leaderStopCheck, leaderWaitCheck, makeWay == -1,
				shouldMakeWayPossibility, shouldKickStuckPossibility);

			// Check if we need to make way for leader or kick stuck units
			var stuckAction = MarchEvalCA.StuckActionFor(shouldMakeWayPossibility,
				shouldKickStuckPossibility, MaxMakeWayPossibility, MaxSquadStuckPossibility);
			if (stuckAction == MarchStuckAction.MakeWay)
			{
				AIUtils.BotDebug("AI ({0}): Make way for squad leader.", owner.Bot.Player.ClientIndex);
				makeWay = MakeWayTicks;
			}
			else if (stuckAction == MarchStuckAction.KickStuck)
			{
				AIUtils.BotDebug("AI ({0}): Kick stuck units from squad.", owner.Bot.Player.ClientIndex);
				kickStuck = KickStuckTicks;
			}

			// Compute indirect/harass route when target changes
			if (!owner.Target.Equals(lastRoutingTarget))
			{
				lastRoutingTarget = owner.Target;
				currentRoute = null;
				currentWaypointIndex = 0;
				lastWaypointUpdateTick = owner.World.WorldTick;

				var targetCell = owner.World.Map.CellContaining(owner.Target.CenterPosition);

				// 6e risk routing: coarse waypoints that skirt remembered threat,
				// when a router answers. Guerrillas and harassers keep their harass
				// routes — unpredictability is the point there.
				if (owner.Type != SquadCAType.Guerrilla && owner.Type != SquadCAType.Harass)
					currentRoute = owner.SquadManager.RouteAroundThreat(leader.Actor, targetCell);

				var locomotor = leader.Actor.TraitOrDefault<Mobile>()?.Locomotor;
				if (currentRoute == null && locomotor != null)
				{
					// The roll stays lazy inside the chance gate (Func) — an eager draw would
					// desync the shared random stream whenever the gate is off.
					var (maxRoutes, useIndirectRoutes) = MarchEvalCA.RouteParams(owner.Type,
						owner.SquadManager.Info.HarassRouteCount, owner.SquadManager.Info.IndirectRouteChance,
						() => BotRng.For(owner.Bot, nameof(GroundUnitsAttackMoveStateCA)).Next(100));

					if (maxRoutes > 2 || useIndirectRoutes)
					{
						// CA F2p2 (2bad89a77): plan the flank from the own building closest to the target (leader included), not from the leader.
						var startCell = leader.Actor.Location;
						if (owner.SquadManager.Info.RouteFromNearestOwnBuilding)
						{
							var startActor = owner.SquadManager.OwnBaseBuildings.Concat(new[] { leader.Actor })
								.ClosestToIgnoringPath(owner.Target.CenterPosition);
							if (startActor != null)
								startCell = startActor.Location;
						}

						var routes = AIUtils.FindDistinctRoutes(owner.World, locomotor, startCell, owner.World.Map.CellContaining(owner.Target.CenterPosition), maxRoutes);
						if (routes.Count == 0 && startCell != leader.Actor.Location)
							routes = AIUtils.FindDistinctRoutes(owner.World, locomotor, leader.Actor.Location, owner.World.Map.CellContaining(owner.Target.CenterPosition), maxRoutes);

						if (owner.Type == SquadCAType.Guerrilla || owner.Type == SquadCAType.Harass)
							routes = routes.Skip(Math.Max(0, routes.Count - 2)).Take(2).ToList();
						else if (useIndirectRoutes)
							routes = routes.Skip(1).ToList();

						if (routes.Count > 0)
						{
							var chosen = routes.Random(BotRng.For(owner.Bot, nameof(GroundUnitsAttackMoveStateCA)));
							currentRoute = chosen.Skip(1).ToList();
							currentWaypointIndex = 0;
							lastWaypointUpdateTick = owner.World.WorldTick;
						}
					}
				}
			}

			// Advance through route waypoints
			if (currentRoute != null && currentWaypointIndex < currentRoute.Count - 1)
			{
				if (MarchEvalCA.AdvanceWaypoint(
					(leader.Actor.Location - currentRoute[currentWaypointIndex]).LengthSquared,
					owner.World.WorldTick, lastWaypointUpdateTick))
				{
					currentWaypointIndex++;
					lastWaypointUpdateTick = owner.World.WorldTick;
				}
			}

			// Determine move target (waypoint or direct)
			var routeTarget = currentRoute != null && currentRoute.Count > 1 && currentWaypointIndex < currentRoute.Count
				? Target.FromCell(owner.World, currentRoute[currentWaypointIndex])
				: Target.FromPos(owner.Target.CenterPosition);

			// Record current position of the squad leader
			leader.WPos = leader.Actor.CenterPosition;

			// Leader will wait squad members that left behind, unless
			// next tick is kick stuck unit (we need leader move in advance).
			if (owner.SquadManager.Info.UseFormationHysteresis)
			{
				// Hysteresis on the wait (AR-S 2026-10-04): latch the wait when a member trails
				// beyond occupiedArea * 5 and only release once everyone is back within
				// occupiedArea * 3 — the old per-tick re-issue stopped and restarted the leader
				// on every squad tick. A freshly armed kickStuck also releases: the kick block
				// drives the leader for its duration, matching the old code's detection-tick
				// AttackMove.
				if (!leaderWaiting && MarchEvalCA.LeaderWaitLatches(leaderWaitCheck, kickStuck > 0))
				{
					leaderWaiting = true;
					leaderOrderCell = null;
					owner.Bot.QueueOrder(new Order("Stop", leader.Actor, false));
				}
				else if (leaderWaiting && !MarchEvalCA.LeaderWaitHolds(
					owner.Units.Any(u => (u.Actor.CenterPosition - leader.Actor.CenterPosition).HorizontalLengthSquared > occupiedArea * 3),
					kickStuck > 0))
				{
					leaderWaiting = false;
				}

				if (!leaderWaiting)
				{
					// The leader churns too: re-issue only when the leader itself or the route
					// cell changed — the old per-tick re-issue repathed it every squad tick.
					var leaderCell = owner.World.Map.CellContaining(routeTarget.CenterPosition);
					if (leaderOrderActor != leader.Actor || leaderOrderCell != leaderCell)
					{
						leaderOrderActor = leader.Actor;
						leaderOrderCell = leaderCell;
						owner.Bot.QueueOrder(new Order("AttackMove", leader.Actor, routeTarget, false));
					}
				}
			}
			else if (leaderWaitCheck && kickStuck <= 0)
			{
				owner.Bot.QueueOrder(new Order("Stop", leader.Actor, false));
			}
			else
			{
				owner.Bot.QueueOrder(new Order("AttackMove", leader.Actor, routeTarget, false));
			}

			// 12.7: assault squads keep formation steps - frontline leads, anti-air
			// inside, the rest trails the frontline centroid. Other squad types keep
			// the plain straggler catch-up (guerrilla/harass mobility is doctrinal).
			// (The final-approach assault fan is the concave state's objective shape, 12.7a.)
			if (owner.SquadManager.Info.FormationMovement && owner.Type == SquadCAType.Rush
				&& IssueFormationOrders(owner, leader, routeTarget))
				return;

			var unitsHurryUp = owner.Units.Where(u => (u.Actor.CenterPosition - leader.Actor.CenterPosition).HorizontalLengthSquared >= occupiedArea * 2).Select(u => u.Actor).ToArray();
			if (owner.SquadManager.Info.UseFormationHysteresis)
			{
				// Same dedup lattice as the march (AR-S 2026-10-04): a straggler keeps its
				// in-flight catch-up until the leader's cell actually moved — the per-tick
				// re-issue repathed it into the move-one-tile/stand pattern. A member that
				// catches up is left to run its order out, exactly like the old code.
				var hurryCell = leader.Actor.Location;
				PruneStaleFormationOrders(owner);
				var hurryChanged = new List<Actor>();
				foreach (var a in unitsHurryUp)
				{
					if (formationOrders.TryGetValue(a, out var prev)
						&& prev.Class == FormationClass.HurryUp && prev.Target == hurryCell)
						continue;

					formationOrders[a] = (FormationClass.HurryUp, hurryCell);
					hurryChanged.Add(a);
				}

				if (hurryChanged.Count > 0)
					owner.Bot.QueueOrder(new Order("AttackMove", null, Target.FromCell(owner.World, hurryCell), false, groupedActors: hurryChanged.ToArray()));
			}
			else
			{
				owner.Bot.QueueOrder(new Order("AttackMove", null, Target.FromCell(owner.World, leader.Actor.Location), false, groupedActors: unitsHurryUp));
			}
		}

		// Drop the memory of units that left the squad so a rejoining member starts clean.
		void PruneStaleFormationOrders(SquadCA owner)
		{
			if (formationOrders.Count == 0)
				return;

			var members = new HashSet<Actor>(owner.Units.Select(u => u.Actor));
			foreach (var stale in formationOrders.Keys.Where(a => !members.Contains(a)).ToList())
				formationOrders.Remove(stale);
		}

		// 12.7 formation step, once per squad tick (orders only, no activities):
		// frontline leads toward the route target at the pace of its slowest
		// member - anyone FormationMaxLeadCells ahead of the rear frontline
		// projection holds; anti-air sits on the frontline centroid; every other
		// ground unit aims FormationTrailCells behind the centroid. Scouts are
		// never constrained - they run ahead on their own. Returns false when no
		// usable role/frontline data exists so the caller keeps the old path.
		bool IssueFormationOrders(SquadCA owner, UnitWposWrapper leader, Target routeTarget)
		{
			var roleMap = owner.SquadManager.ActorRoles;
			if (roleMap == null || roleMap.Count == 0)
				return false;

			var frontline = new List<UnitWposWrapper>();
			var antiAir = new List<UnitWposWrapper>();
			var scouts = new List<UnitWposWrapper>();
			var trailing = new List<UnitWposWrapper>();

			foreach (var u in owner.Units)
			{
				if (u.Actor == leader.Actor)
					continue;

				var roles = roleMap.TryGetValue(u.Actor.Info.Name, out var actorRoles) ? actorRoles : null;
				switch (MarchEvalCA.BucketFor(roles))
				{
					case MarchBucket.Frontline: frontline.Add(u); break;
					case MarchBucket.Scout: scouts.Add(u); break;
					case MarchBucket.AntiAir: antiAir.Add(u); break;
					default: trailing.Add(u); break;
				}
			}

			if (frontline.Count == 0)
				return false;

			// Axis of advance: frontline centroid -> route target.
			var routePos = routeTarget.CenterPosition;
			long cx = leader.Actor.CenterPosition.X, cy = leader.Actor.CenterPosition.Y;
			foreach (var u in frontline)
			{
				cx += u.Actor.CenterPosition.X;
				cy += u.Actor.CenterPosition.Y;
			}

			var centroid = new WPos((int)(cx / (frontline.Count + 1)), (int)(cy / (frontline.Count + 1)), 0);
			var axis = routePos - centroid;
			if (!MarchEvalCA.AxisUsable(axis.HorizontalLengthSquared))
				return false;

			var axisLen = axis.HorizontalLength;
			var maxLead = (long)WDist.FromCells(owner.SquadManager.Info.FormationMaxLeadCells).Length;

			// Distance-to-go along the axis for every frontline member; the slowest
			// (largest remaining) gates how far ahead the others may run. The
			// projection is computed in long: WVec.Dot returns int and overflows
			// beyond ~45 cells of span, which is most real routes.
			var slowestRemaining = long.MinValue;
			UnitWposWrapper rear = null;
			foreach (var u in frontline)
			{
				var delta = routePos - u.Actor.CenterPosition;
				var rem = MarchEvalCA.AxisRemaining(delta.X, delta.Y, delta.Z, axis.X, axis.Y, axis.Z, axisLen);
				if (rem > slowestRemaining)
				{
					slowestRemaining = rem;
					rear = u;
				}
			}

			// Fransbot donor rule: a rear member stalled in a chokepoint grants the
			// leaders the wider FormationMaxStalledLeadCells allowance; the normal
			// lead resumes the tick it moves again. MI watches the stall's HP:
			// a stalled rear that is losing HP is under fire and becomes the
			// pull-back trigger below.
			var lead = maxLead;
			var stalledRearUnderFire = false;
			if (rear != null)
			{
				var sameRearPos = rear.Actor.CenterPosition == formationRearPos;
				var rearHealth = sameRearPos ? rear.Actor.TraitOrDefault<IHealth>() : null;
				var (stallTicks, prevHp, underFire) = MarchEvalCA.RearStallNext(sameRearPos,
					formationRearStallTicks, rearHealth != null, rearHealth?.HP ?? 0, formationRearHp);
				formationRearStallTicks = stallTicks;
				formationRearHp = prevHp;
				stalledRearUnderFire = underFire;
				if (!sameRearPos)
					formationRearPos = rear.Actor.CenterPosition;

				lead = MarchEvalCA.LeadForStall(formationRearStallTicks, maxLead,
					WDist.FromCells(owner.SquadManager.Info.FormationMaxStalledLeadCells).Length);
			}

			// AR-S (switch BJ_squad_hysteresis): armed = dead band + transition-only orders;
			// unarmed = zero-width band (the old hard cut) and the order re-issued every tick,
			// which keeps the classic order stream bit-identical.
			var armed = owner.SquadManager.Info.UseFormationHysteresis;
			var hysteresis = armed ? (long)WDist.FromCells(owner.SquadManager.Info.FormationHoldHysteresisCells).Length : 0;

			var holdFront = new List<Actor>();
			var pushFront = new List<Actor>();
			var pullFront = new List<Actor>();
			Actor pullRear = null;
			var retreatPct = owner.SquadManager.Info.SquadMicroRetreatPct;
			foreach (var u in frontline)
			{
				var delta = routePos - u.Actor.CenterPosition;
				var rem = MarchEvalCA.AxisRemaining(delta.X, delta.Y, delta.Z, axis.X, axis.Y, axis.Z, axisLen);
				var wasHolding = formationOrders.TryGetValue(u.Actor, out var prev) && prev.Class == FormationClass.Hold;

				// AR-S armed path: a wounded or under-fire member is pulled back this
				// tick instead of being pushed — the old code issued the formation
				// AttackMove and the micro Move in the same tick, so the unit flip-flopped
				// between them every squad tick (the dominant AttackMove<->Move stutter).
				if (armed && owner.SquadManager.Info.SquadMicroEnabled)
				{
					var health = u.Actor.TraitOrDefault<IHealth>();
					if (stalledRearUnderFire && u == rear)
					{
						pullRear = u.Actor;
						continue;
					}

					if (health != null && SquadMicroEvalCA.ShouldPullBack(health.HP, health.MaxHP, retreatPct))
					{
						pullFront.Add(u.Actor);
						continue;
					}
				}

				if (SquadMicroEvalCA.ClassifyHolding(slowestRemaining - rem, lead, hysteresis, wasHolding))
					holdFront.Add(u.Actor);
				else
					pushFront.Add(u.Actor);
			}

			// The trail line doubles as the pull-back rally behind the
			// formation anchor for MI below.
			var trailPos = SquadMicroEvalCA.PullBackPoint(centroid, routePos,
				WDist.FromCells(owner.SquadManager.Info.FormationTrailCells));

			var routeCell = owner.World.Map.CellContaining(routeTarget.CenterPosition);
			var centroidCell = owner.World.Map.CellContaining(centroid);
			var trailCell = owner.World.Map.CellContaining(trailPos);

			// The stalled rear pulls back past itself (break contact in the chokepoint),
			// not to the shared trail line.
			var rearPullPos = WPos.Zero;
			var rearPullCell = CPos.Zero;
			if (pullRear != null)
			{
				rearPullPos = SquadMicroEvalCA.PullBackPoint(pullRear.CenterPosition, routePos,
					WDist.FromCells(owner.SquadManager.Info.FormationTrailCells));
				rearPullCell = owner.World.Map.CellContaining(rearPullPos);
			}

			// Order churn guard (2026-10-04 stutter fix): queue an order only for members whose
			// class or quantized target actually changed since the last squad tick. Identical
			// re-issued orders cancelled the unit's active MoveTo every tick — the visible
			// stop-start "stutter step" of the whole march. A held member's stored target stays
			// at CPos.Zero: route drift must not re-fire its Stop.
			var orderChanges = new List<(Actor Actor, FormationClass Class, CPos Target)>(
				frontline.Count + antiAir.Count + scouts.Count + trailing.Count);
			foreach (var a in pushFront)
				orderChanges.Add((a, FormationClass.Push, routeCell));

			foreach (var a in holdFront)
				orderChanges.Add((a, FormationClass.Hold, CPos.Zero));

			foreach (var a in pullFront)
				orderChanges.Add((a, FormationClass.Retreat, trailCell));

			if (pullRear != null)
				orderChanges.Add((pullRear, FormationClass.RetreatRear, rearPullCell));

			foreach (var u in antiAir)
				orderChanges.Add((u.Actor, FormationClass.AntiAir, centroidCell));

			foreach (var u in scouts)
				orderChanges.Add((u.Actor, FormationClass.Scout, routeCell));

			foreach (var u in trailing)
			{
				// Armed path: a wounded trailing member falls back to the trail
				// line (Move) instead of the trail AttackMove — same dedup lattice.
				if (armed && owner.SquadManager.Info.SquadMicroEnabled
					&& u.Actor.TraitOrDefault<IHealth>() is { } th
					&& SquadMicroEvalCA.ShouldPullBack(th.HP, th.MaxHP, retreatPct))
				{
					orderChanges.Add((u.Actor, FormationClass.Retreat, trailCell));
					continue;
				}

				orderChanges.Add((u.Actor, FormationClass.Trail, trailCell));
			}

			var stopChanged = new List<Actor>();
			var pushChanged = new List<Actor>();
			var antiAirChanged = new List<Actor>();
			var scoutChanged = new List<Actor>();
			var trailChanged = new List<Actor>();
			var retreatChanged = new List<Actor>();
			var retreatRearChanged = new List<Actor>();
			foreach (var change in orderChanges)
			{
				if (armed
					&& formationOrders.TryGetValue(change.Actor, out var prev)
					&& prev.Class == change.Class && prev.Target == change.Target)
					continue;

				// Pull-backs stay budgeted (MI per-order cap); a denied member keeps
				// its previous order — its memory only updates on a real issue.
				if (armed
					&& (change.Class == FormationClass.Retreat || change.Class == FormationClass.RetreatRear)
					&& !owner.SquadManager.TryConsumeMicroActions())
					continue;

				formationOrders[change.Actor] = (change.Class, change.Target);
				switch (change.Class)
				{
					case FormationClass.Hold: stopChanged.Add(change.Actor); break;
					case FormationClass.AntiAir: antiAirChanged.Add(change.Actor); break;
					case FormationClass.Scout: scoutChanged.Add(change.Actor); break;
					case FormationClass.Trail: trailChanged.Add(change.Actor); break;
					case FormationClass.Retreat: retreatChanged.Add(change.Actor); break;
					case FormationClass.RetreatRear: retreatRearChanged.Add(change.Actor); break;
					default: pushChanged.Add(change.Actor); break;
				}
			}

			PruneStaleFormationOrders(owner);

			if (pushChanged.Count > 0)
				owner.Bot.QueueOrder(new Order("AttackMove", null, routeTarget, false, groupedActors: pushChanged.ToArray()));
			if (stopChanged.Count > 0)
				owner.Bot.QueueOrder(new Order("Stop", null, false, groupedActors: stopChanged.ToArray()));
			if (antiAirChanged.Count > 0)
				owner.Bot.QueueOrder(new Order("AttackMove", null, Target.FromCell(owner.World, centroidCell), false, groupedActors: antiAirChanged.ToArray()));
			if (scoutChanged.Count > 0)
				owner.Bot.QueueOrder(new Order("AttackMove", null, routeTarget, false, groupedActors: scoutChanged.ToArray()));
			if (trailChanged.Count > 0)
				owner.Bot.QueueOrder(new Order("AttackMove", null, Target.FromPos(trailPos), false, groupedActors: trailChanged.ToArray()));
			if (retreatChanged.Count > 0)
				owner.Bot.QueueOrder(new Order("Move", null, Target.FromPos(trailPos), false, groupedActors: retreatChanged.ToArray()));
			if (retreatRearChanged.Count > 0)
				owner.Bot.QueueOrder(new Order("Move", null, Target.FromPos(rearPullPos), false, groupedActors: retreatRearChanged.ToArray()));

			// MI pull-back (budgeted per order): members at/below
			// SquadMicroRetreatPct fall back to the trail line — behind the
			// formation anchor, still with the squad. A stalled rear that is
			// taking fire pulls back past itself to break contact in the
			// chokepoint. Queued after the formation orders so the micro order
			// wins for that member this tick; a denied member keeps its
			// formation order. The armed path instead folds pulls into the
			// deduped order stream above (Retreat/RetreatRear) so a pulled
			// member is never pushed in the same tick.
			if (owner.SquadManager.Info.SquadMicroEnabled && !armed)
			{
				foreach (var u in frontline)
				{
					var hp = u.Actor.TraitOrDefault<IHealth>();
					if (hp == null || !SquadMicroEvalCA.ShouldPullBack(hp.HP, hp.MaxHP, retreatPct))
						continue;

					if (!owner.SquadManager.TryConsumeMicroActions())
						break;

					owner.Bot.QueueOrder(new Order("Move", u.Actor, Target.FromPos(trailPos), false));
				}

				foreach (var u in trailing)
				{
					var hp = u.Actor.TraitOrDefault<IHealth>();
					if (hp == null || !SquadMicroEvalCA.ShouldPullBack(hp.HP, hp.MaxHP, retreatPct))
						continue;

					if (!owner.SquadManager.TryConsumeMicroActions())
						break;

					owner.Bot.QueueOrder(new Order("Move", u.Actor, Target.FromPos(trailPos), false));
				}

				if (stalledRearUnderFire && rear != null && owner.SquadManager.TryConsumeMicroActions())
					owner.Bot.QueueOrder(new Order("Move", rear.Actor,
						Target.FromPos(SquadMicroEvalCA.PullBackPoint(rear.Actor.CenterPosition, routePos,
							WDist.FromCells(owner.SquadManager.Info.FormationTrailCells))), false));
			}

			return true;
		}

		public void Deactivate(SquadCA owner) { }
	}

	class GroundUnitsAttackState : GroundStateBaseCA, IState
	{
		int lastUpdatedTick;
		CPos? lastLeaderLocation;
		Actor lastTarget;

		public void Activate(SquadCA owner)
		{
			// IMPL02-AGG-CORE (switch ON only, A2): a fight that did not start from the idle attack band commits here.
			if (owner.SquadManager.AggressionOn && owner.AggressionCommitTick < 0)
				owner.SquadManager.AggressionCommit(owner);
		}

		public void Tick(SquadCA owner)
		{
			if (!owner.IsValid)
				return;

			// TEAM-GATHER-02 (gt_bounded_retarget): a team Rush squad whose target is gone fights on only around its leader, else goes back to idle (see the AttackMove state).
			if (!owner.IsTargetValid && owner.SquadManager.TeamLeadBoundsRetarget(owner))
			{
				var near = owner.SquadManager.FindClosestEnemy(owner.Units[0].Actor, WDist.FromCells(owner.SquadManager.Info.AttackScanRadius), owner);
				if (near != null)
				{
					owner.TargetActor = near;
					return;
				}

				owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsIdleStateCA(), false);
				return;
			}

			// TEAM-GATHER-02 c2/c3 (gt_bounded_retarget): the pushed blob keeps going: the next enemy of the order focus seat, until that seat is dead.
			if (!owner.IsTargetValid && owner.SquadManager.TeamLeadKeepsGoing(owner))
			{
				if (owner.SquadManager.TeamLeadGoOn(owner, owner.Units[0].Actor))
					return;

				// Nothing known at all (visible or remembered): stand idle, the team pass or the hunt via order takes over; the blob never flees.
				owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsIdleStateCA(), false);
				return;
			}

			if (!owner.IsTargetValid && !FindNewTarget(owner))
			{
				// CA F2p2 (2bad89a77): rather than fleeing, take an opportunity target near the leader, else resume AttackMove.
				// FindClosestEnemy(leader, range) is the observed overload (visible or remembered enemies only).
				if (owner.SquadManager.Info.UseUpstreamStateTweaks)
				{
					var opportunity = owner.SquadManager.FindClosestEnemy(owner.Units[0].Actor, WDist.FromCells(owner.SquadManager.Info.AttackScanRadius), owner);
					if (opportunity != null)
					{
						owner.TargetActor = opportunity;
						return;
					}

					owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsAttackMoveStateCA(), true);
					return;
				}

				owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsFleeStateCA(), true);
				return;
			}

			var leader = owner.Units[0].Actor;
			if (leader.Location != lastLeaderLocation)
			{
				lastLeaderLocation = leader.Location;
				lastUpdatedTick = owner.World.WorldTick;
			}

			if (owner.TargetActor != lastTarget)
			{
				lastTarget = owner.TargetActor;
				lastUpdatedTick = owner.World.WorldTick;
			}

			// HACK: Drop back to the idle state if we haven't moved in 2.5 seconds
			// This works around the squad being stuck trying to attack-move to a location
			// that they cannot path to, generating expensive pathfinding calls each tick.
			// TEAM-GATHER-02 c5: a pushed blob in which any unit is actually attacking is not stuck: its stuck clock restarts, so a real siege is not cut off by the siege cap.
			if (owner.World.WorldTick > lastUpdatedTick + 63 && owner.SquadManager.TeamLeadKeepsGoing(owner) && owner.Units.Any(u => IsAttackingAndTryAttack(u.Actor).IsFiring))
				lastUpdatedTick = owner.World.WorldTick;

			// TEAM-GATHER-02 c3/c4: a pushed blob sieging its target (inside the scan radius of it) does not move and is not stuck, for at most TeamLeadSiegeMaxTicks; then it
			// skips that target and takes the next enemy of the focus seat.
			if (owner.World.WorldTick > lastUpdatedTick + (owner.SquadManager.Info.UseUpstreamStateTweaks ? 100 : 63) // CA F2p2 (9a68fea15)
				&& !owner.SquadManager.TeamLeadSieging(owner, owner.World.WorldTick - lastUpdatedTick))
			{
				if (owner.SquadManager.TeamLeadStuckRetarget(owner, leader))
				{
					lastUpdatedTick = owner.World.WorldTick;
					return;
				}

				owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsIdleStateCA(), true);
				return;
			}

			// MI order half (SquadMicroEnabled, Rush only — its own A/B cell):
			// one focus target per squad tick, members that can reach it
			// concentrate fire, damaged members pull back behind the squad,
			// outranging members kite. Candidates come through
			// IsPreferredObservedEnemyUnit — only what the bot can currently see
			// or legitimately remember. Budget denial or no pick degrades to the
			// plain AttackMove below.
			var micro = owner.SquadManager.Info.SquadMicroEnabled && owner.Type == SquadCAType.Rush
				&& owner.TargetActor != null;
			Actor focus = null;
			BotUnitProfile focusProfile = null;
			if (micro)
			{
				var targets = owner.World.FindActorsInCircle(owner.TargetActor.CenterPosition,
						WDist.FromCells(owner.SquadManager.Info.AttackScanRadius))
					.Where(owner.SquadManager.IsPreferredObservedEnemyUnit).ToList();

				if (targets.Count > 0)
				{
					var eff = owner.SquadManager.Info.UseEffectiveDamageModel;
					var viewer = owner.SquadManager.Player;
					var squadProfiles = owner.Units.ConvertAll(u => BotUnitProfiles.Get(u.Actor, viewer, eff));
					var pick = SquadMicroEvalCA.PickFocusTarget(squadProfiles,
						targets.ConvertAll(t => eff ? BotUnitProfiles.Get(t, viewer, true) : LiveHpProfile(owner.World, t, false)), eff);

					if (pick >= 0)
					{
						focus = targets[pick];
						focusProfile = eff ? BotUnitProfiles.Get(focus, viewer, true) : LiveHpProfile(owner.World, focus, false);
					}
				}
			}

			foreach (var a in owner.Units)
			{
				if (BusyAttack(a.Actor))
					continue;

				if (micro && TryIssueMicroOrder(owner, a.Actor, focus, focusProfile))
					continue;

				if (!owner.SquadManager.Info.UseSquadOrderDedup
					|| (owner.TargetActor != null && owner.OrderChanged(a.Actor, SquadOrderKey.ForActor("AttackMove", owner.TargetActor))))
					owner.Bot.QueueOrder(new Order("AttackMove", a.Actor, Target.FromActor(owner.TargetActor), false));
			}

			// BOTPASSIVE (switch ON only): the live breaker wave defers this predicted-loss disengagement (guard_bypass).
			// IMPL02-AGG-CORE (switch ON only, A2/A4): AggressionHolds can only cancel a legacy flight, never start one.
			if (ShouldFlee(owner) && !AggressionHolds(owner) && !owner.SquadManager.StalemateDefersRetreat(owner, "attack_should_flee"))
			{
				owner.SquadManager.NoteRetreatStart(owner, "attack_should_flee");
				owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsFleeStateCA(), true);
			}
		}

		// One member's micro order (the order half of SquadMicroEvalCA),
		// priority pull-back > kite standoff > focus-fire. Spends one
		// IBotActionBudget action per issued order; false means the member
		// keeps the default AttackMove.
		static bool TryIssueMicroOrder(SquadCA owner, Actor unit, Actor focus, BotUnitProfile focusProfile)
		{
			var assignedTarget = focus ?? owner.TargetActor;

			var health = unit.TraitOrDefault<IHealth>();
			if (health != null && SquadMicroEvalCA.ShouldPullBack(health.HP, health.MaxHP, owner.SquadManager.Info.SquadMicroRetreatPct))
			{
				if (!owner.SquadManager.TryConsumeMicroActions())
					return false;

				// Retreat behind the squad centre — regroup with the squad, not a rout home.
				var waypoint = SquadMicroEvalCA.PullBackPoint(owner.CenterPosition, assignedTarget.CenterPosition,
					WDist.FromCells(owner.SquadManager.Info.FormationTrailCells));
				owner.Bot.QueueOrder(new Order("Move", unit, Target.FromPos(waypoint), false));
				return true;
			}

			var eff = owner.SquadManager.Info.UseEffectiveDamageModel;
			var viewer = owner.SquadManager.Player;
			var ownProfile = eff ? BotUnitProfiles.Get(unit, viewer, true) : BotUnitProfiles.Get(owner.World.Map.Rules, unit.Info, false);
			var targetProfile = eff ? BotUnitProfiles.Get(assignedTarget, viewer, true) : BotUnitProfiles.Get(owner.World.Map.Rules, assignedTarget.Info, false);
			var standoff = SquadMicroEvalCA.KiteStandoff(ownProfile, targetProfile,
				WDist.FromCells(owner.SquadManager.Info.SquadMicroKiteMarginCells));
			if (standoff is WDist standoffDist)
			{
				// Kite only when inside the target's reply range — beyond it
				// AttackMove already holds the unit at its own weapon range.
				var dist = (unit.CenterPosition - assignedTarget.CenterPosition).HorizontalLength;
				if (dist < targetProfile.MaxRange.Length)
				{
					if (!owner.SquadManager.TryConsumeMicroActions())
						return false;

					var waypoint = SquadMicroEvalCA.PullBackPoint(unit.CenterPosition, assignedTarget.CenterPosition,
						new WDist((int)Math.Max(0, standoffDist.Length - dist)));
					owner.Bot.QueueOrder(new Order("Move", unit, Target.FromPos(waypoint), false));
					return true;
				}
			}

			if (focus != null && focusProfile != null
				&& ownProfile.DamagePerTickAgainst(focusProfile, eff) > 0
				&& (focus.CenterPosition - unit.CenterPosition).HorizontalLengthSquared
					<= (long)ownProfile.MaxRange.Length * ownProfile.MaxRange.Length)
			{
				if (!owner.SquadManager.TryConsumeMicroActions())
					return false;

				owner.Bot.QueueOrder(new Order("Attack", unit, Target.FromActor(focus), false));
				return true;
			}

			return false;
		}

		// A rules profile with the actor's CURRENT hit points — PickFocusTarget's
		// time-to-kill must see a near-dead target as near-dead, not at the
		// pristine MaxHP the cache stores.
		static BotUnitProfile LiveHpProfile(World world, Actor actor, bool useEffective)
		{
			var profile = BotUnitProfiles.Get(world.Map.Rules, actor.Info, useEffective);
			var health = actor.TraitOrDefault<IHealth>();
			if (health == null || health.HP == profile.Hp)
				return profile;

			return new BotUnitProfile(profile.Name, profile.Cost, health.HP, profile.Armor, profile.Speed,
				profile.IsAircraft, profile.IsBuilding, profile.TargetTypes, profile.Weapons);
		}

		public void Deactivate(SquadCA owner) { }
	}

	// SIEGE-FIX-01 S6: the retreat the siege advisor asks for. One rally decision on entry (the same A10 episode rule as the Flee state);
	// when the rally refuses the squad is NOT dismissed: it pulls back to the stand-off cell and holds there while the advisor still says
	// Retreat (the evaluator's re-engage cooldown ends that verdict once the squad has grown). Any other verdict returns it to AttackMove.
	class GroundUnitsSiegeRetreatStateCA : GroundStateBaseCA, IState
	{
		readonly CPos holdCell;
		bool decided;
		int startTick = -1;

		public GroundUnitsSiegeRetreatStateCA(CPos holdCell)
		{
			this.holdCell = holdCell;
		}

		public void Activate(SquadCA owner)
		{
			startTick = owner.World.WorldTick;
		}

		public void Tick(SquadCA owner)
		{
			if (!owner.IsValid)
				return;

			if (!decided)
			{
				decided = true;
				owner.SquadManager.RetreatRallyNoteFlight(owner);
				if (owner.SquadManager.TryRetreatRally(owner, "siege_retreat", out var plan))
				{
					owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsRallyStateCA(plan), false);
					return;
				}
			}

			var verdict = owner.SquadManager.EvaluateSiege(owner, out var cell);
			var leader = owner.Units.FirstOrDefault(u => u.Actor != null && !u.Actor.IsDead && u.Actor.IsInWorld);
			if (verdict != SiegeVerdict.Retreat || leader.Actor == null)
			{
				owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsAttackMoveStateCA(), false);
				return;
			}

			// Opus ruling: the pull-back hold is capped by the tier's StandOffMaxTicks line. On expiry the squad is released to the
			// normal regroup/rally-home path (the legacy retreat orders, never a squad dismissal); the evaluator's re-engage cooldown keeps running.
			if (SiegeEvaluatorBotModule.HoldExpired(owner.World.WorldTick, startTick, owner.SquadManager.SiegeHoldCapTicks()))
			{
				Retreat(owner, flee: true, rearm: true, repair: true);
				owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsIdleStateCA(), false);
				return;
			}

			// Contact: the AttackMove state owns engagement (same enemy scan it runs before its own siege consult).
			if (owner.SquadManager.FindClosestEnemy(leader.Actor, WDist.FromCells(owner.SquadManager.Info.AttackScanRadius), owner) != null)
			{
				owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsAttackMoveStateCA(), false);
				return;
			}

			// The cell follows the latest evaluation (the squad may have grown, defences may have moved); re-order only when the leader drifts off it.
			var hold = owner.World.Map.Contains(cell) ? cell : holdCell;
			if (owner.World.Map.Contains(hold) &&
				(owner.World.Map.CellContaining(leader.Actor.CenterPosition) - hold).LengthSquared > 4)
				foreach (var u in owner.Units)
					if (u.Actor != null)
						owner.Bot.QueueOrder(new Order("Move", u.Actor, Target.FromCell(owner.World, hold), false));
		}

		// No dismissal: the squad keeps its units and its identity.
		public void Deactivate(SquadCA owner) { }
	}

	class GroundUnitsFleeStateCA : GroundStateBaseCA, IState
	{
		// A10 RETREAT-RALLY: baseOnly = the legacy flight with the rally refused (the rally state's downgrade); handoff = the squad was
		// given to the rally state, so Deactivate must not dismiss it. Every entry creates a new state object: no reset is needed.
		readonly bool baseOnly;
		bool handoff;

		public GroundUnitsFleeStateCA(bool baseOnly = false)
		{
			this.baseOnly = baseOnly;
		}

		public void Activate(SquadCA owner) { }

		public void Tick(SquadCA owner)
		{
			if (!owner.IsValid)
				return;

			owner.SquadManager.RetreatRallyNoteFlight(owner);
			if (!baseOnly && owner.SquadManager.TryRetreatRally(owner, "flee_state", out var plan))
			{
				handoff = true;
				owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsRallyStateCA(plan), false);
				return;
			}

			GoToRandomOwnBuilding(owner, fleeing: true);
			owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsIdleStateCA(), true);
		}

		public void Deactivate(SquadCA owner)
		{
			if (handoff)
				return;

			owner.SquadManager.DismissSquad(owner);
		}
	}

	class HarasserUnitsIdleStateCA : GroundStateBaseCA, IState
	{
		public void Activate(SquadCA owner) { }

		public void Tick(SquadCA owner)
		{
			if (!owner.IsValid)
				return;

			// The harasser launch quorum (upstream CA): a trickle of one or two
			// raiders is a waste — wait for a squad that can hurt a harvester line.
			if (!ShouldHarass(owner.Units.Count, owner.SquadManager.Info.HarassMinLaunchSize, BotRng.For(owner.Bot, nameof(HarasserUnitsIdleStateCA)).Next(100)))
				return;

			// High-value targets first (harvester lines, expansions), through the
			// 6c risk gate — a harasser raid still should not suicide.
			if (!owner.IsTargetValid && !FindNewTarget(owner, highValueCheck: true, riskCheck: true))
				return;

			{
				owner.SquadManager.LaunchSourceRecord("L14", owner.Type.ToString(), owner.Units.Count);
				owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsAttackMoveStateCA(), true);
			}
		}

		internal static bool ShouldHarass(int count, int minSize, int roll)
		{
			if (count < minSize)
				return false;

			// Just past the quorum the launch is a roll; a full wing always goes.
			if (count == minSize)
				return roll < 5;

			if (count == minSize + 1)
				return roll < 10;

			return true;
		}

		public void Deactivate(SquadCA owner) { }
	}

	class GuerrillaUnitsHitState : GroundStateBaseCA, IState
	{
		// Use it to find if entire squad cannot reach the attack position
		int tryAttackTick;

		Actor leader;
		int tryAttack = 0;
		bool isFirstTick = true; // Only record HP and do not retreat at first tick
		int squadsize = 0;

		public void Activate(SquadCA owner)
		{
			tryAttackTick = owner.SquadManager.Info.AttackScanRadius;
		}

		public void Tick(SquadCA owner)
		{
			// Basic check
			if (!owner.IsValid)
				return;

			if (owner.SquadManager.unitCannotBeOrdered(leader))
				leader = owner.Units[0].Actor;

			owner.SquadManager.SetAirStrikeTarget(owner.TargetActor);
			var isDefaultLeader = true;

			// Rescan target to prevent being ambushed and die without fight
			// If there is no threat around, return to AttackMove state for formation
			var attackScanRadius = WDist.FromCells(owner.SquadManager.Info.AttackScanRadius);
			var closestEnemy = owner.SquadManager.FindClosestEnemy(leader, attackScanRadius, owner);

			var healthChange = false;
			var cannotRetaliate = true;
			var followingUnits = new List<Actor>();
			var attackingUnits = new List<Actor>();

			if (closestEnemy == null)
			{
				owner.TargetActor = owner.SquadManager.FindClosestEnemy(leader, owner);
				owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsAttackMoveStateCA(), false);
				return;
			}
			else
			{
				if (owner.TargetActor != closestEnemy)
				{
					// Refresh tryAttack when target switched
					tryAttack = 0;
					owner.TargetActor = closestEnemy;
				}

				for (var i = 0; i < owner.Units.Count; i++)
				{
					var u = owner.Units[i];
					var (isFiring, tryAttacking) = IsAttackingAndTryAttack(u.Actor);

					var health = u.Actor.TraitOrDefault<IHealth>();

					if (health != null)
					{
						var healthWPos = new WPos(0, 0, (int)health.DamageState); // HACK: use WPos.Z storage HP
						if (u.WPos.Z != healthWPos.Z)
						{
							if (u.WPos.Z < healthWPos.Z)
								healthChange = true;
							u.WPos = healthWPos;
						}
					}

					if (owner.TargetActor != null && (tryAttacking || isFiring) &&
						(u.Actor.CenterPosition - owner.TargetActor.CenterPosition).HorizontalLengthSquared <
						(leader.CenterPosition - owner.TargetActor.CenterPosition).HorizontalLengthSquared)
					{
						isDefaultLeader = false;
						leader = u.Actor;
					}

					if (isFiring && tryAttack != 0)
					{
						// Make there is at least one follow and attack target, AFTER first trying on attack
						if (isDefaultLeader)
						{
							leader = u.Actor;
							isDefaultLeader = false;
						}

						cannotRetaliate = false;
					}
					else if (CanAttackTarget(u.Actor, owner.TargetActor))
					{
						if (tryAttack > tryAttackTick && tryAttacking)
						{
							// Make there is at least one follow and attack target even when approach max tryAttackTick
							if (isDefaultLeader)
							{
								leader = u.Actor;
								isDefaultLeader = false;
								attackingUnits.Add(u.Actor);
								continue;
							}

							followingUnits.Add(u.Actor);
							continue;
						}

						attackingUnits.Add(u.Actor);
						cannotRetaliate = false;
					}
					else
						followingUnits.Add(u.Actor);
				}
			}

			// Because ShouldFlee(owner) cannot retreat units while they cannot even fight
			// a unit that they cannot target. Therefore, use `cannotRetaliate` here to solve this bug.
			if (cannotRetaliate)
				owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsFleeStateCA(), true);

			tryAttack++;

			var unitlost = squadsize > owner.Units.Count;
			squadsize = owner.Units.Count;

			if (owner.TargetActor != null && (healthChange || unitlost) && !isFirstTick)
			{
				var friendlyUnits = owner.World.FindActorsInCircle(owner.TargetActor.CenterPosition, WDist.FromCells(owner.SquadManager.Info.AttackScanRadius)).Where(owner.SquadManager.IsValidAllyUnit);
				if (friendlyUnits.Count() < squadsize + owner.SquadManager.Info.SquadSize)
					owner.FuzzyStateMachine.ChangeState(owner, new GuerrillaUnitsRunState(), true);
			}

			QueueDeduped(owner, "AttackMove", SquadOrderKey.ForCell("AttackMove", leader.Location),
				Target.FromCell(owner.World, leader.Location), followingUnits);
			if (!owner.SquadManager.Info.UseSquadOrderDedup || owner.TargetActor != null)
				QueueDeduped(owner, "AttackMove", SquadOrderKey.ForActor("AttackMove", owner.TargetActor),
					Target.FromActor(owner.TargetActor), attackingUnits);

			isFirstTick = false;
		}

		public void Deactivate(SquadCA owner) { }
	}

	class GuerrillaUnitsRunState : GroundStateBaseCA, IState
	{
		public const int HitTicks = 2;
		internal int Hit = HitTicks;
		bool ordered;

		public void Activate(SquadCA owner) { ordered = false; }

		public void Tick(SquadCA owner)
		{
			if (!owner.IsValid)
				return;

			if (Hit-- <= 0)
			{
				Hit = HitTicks;
				owner.FuzzyStateMachine.ChangeState(owner, new GuerrillaUnitsHitState(), true);
				return;
			}

			if (!ordered)
			{
				owner.Bot.QueueOrder(new Order("Move", null, Target.FromCell(owner.World, HomeLocation(owner)), false, groupedActors: owner.Units.Select(u => u.Actor).ToArray()));
				ordered = true;
			}
		}

		public void Deactivate(SquadCA owner) { }
	}
	// 6f (CN A2/A6): an artillery squad trails an assault squad and bombards only
	// what the assault can see — its parent's target is fog-honest by
	// construction (observed actor or remembered frozen building).
	class ArtilleryUnitsIdleStateCA : GroundStateBaseCA, IState
	{
		public void Activate(SquadCA owner) { }

		public void Tick(SquadCA owner)
		{
			if (!owner.IsValid)
				return;

			var parent = owner.Parent;
			if (parent == null || !parent.IsValid)
			{
				parent = owner.SquadManager.FindAttachableAssault(owner);
				owner.Parent = parent;
			}

			// No assault to escort: stop babysitting and act as one.
			if (parent == null)
			{
				owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsIdleStateCA(), false);
				return;
			}

			if (!parent.IsTargetValid)
			{
				// Parent is still forming up — trail it.
				owner.Target = Target.Invalid;
				owner.Bot.QueueOrder(new Order("AttackMove", null, Target.FromPos(parent.CenterPosition), false,
					groupedActors: owner.Units.Select(u => u.Actor).ToArray()));
				return;
			}

			owner.Target = parent.Target;
			var targetPos = owner.Target.CenterPosition;
			var anchor = SquadManagerBotModuleCA.HangBackAnchor(parent.CenterPosition, targetPos,
				WDist.FromCells(owner.SquadManager.Info.ArtilleryHangBackCells).Length);

			// ARTY-01 (switch ON only): per-unit target priority, stand-off, defence avoidance and kiting. Off = the loop below.
			if (owner.SquadManager.ArtyMicroActive)
			{
				TickMicro(owner, anchor);
				return;
			}

			foreach (var u in owner.Units)
				LegacyUnitOrder(owner, u, anchor);
		}

		// The pre-ARTY-01 per-unit order (also the fallback for a unit the micro cannot plan for).
		static void LegacyUnitOrder(SquadCA owner, UnitWposWrapper u, WPos anchor)
		{
			// Longest range over the enabled attack traits; TraitOrDefault<AttackBase>
			// throws on the many actors that carry more than one.
			var range = WDist.Zero;
			foreach (var attack in u.Actor.TraitsImplementing<AttackBase>())
			{
				if (attack.IsTraitDisabled)
					continue;

				var r = attack.GetMaximumRangeVersusTarget(owner.Target);
				if (r > range)
					range = r;
			}

			// In range: bombard the shared target. Out of range: move to the
			// hang-back anchor, NOT toward the target — the assault squad does
			// the closing so artillery keeps its range advantage.
			if (range > WDist.Zero && owner.Target.IsInRange(u.Actor.CenterPosition, range))
				owner.Bot.QueueOrder(new Order("Attack", u.Actor, owner.Target, false));
			else
				owner.Bot.QueueOrder(new Order("AttackMove", u.Actor, Target.FromPos(anchor), false));
		}

		struct ArtyMemory
		{
			public int NextThinkTick;
			public uint LastAttackId;
		}

		static readonly BitSet<TargetableType> WallTargetType = new("Wall");

		readonly Dictionary<uint, ArtyMemory> memory = [];
		readonly List<ArtyCandidate> candidates = [];
		readonly List<Actor> candidateActors = [];
		readonly List<ArtyDefence> defences = [];
		readonly List<ArtyCandidate> structures = []; // ARTY-TARGET-01 (c2): seen or remembered structures near the anchor
		readonly List<Actor> structureActors = []; // the actor behind each structure, null when only remembered
		readonly List<ArtyDefence> assaultDefences = []; // the armed ones among structures (their reach covers the firing points)
		bool structuresGathered; // c3: the structure scan runs at most once per squad per think, shared by its units

		// ARTY-01: each unit decides for itself, at most once per ArtyOrderCooldownTicks. Priority per unit:
		// kite from a hostile unit it outranges, back off a defence that reaches it, attack the best target in range
		// (defences, then structures, then units), otherwise approach the squad target to its firing position.
		void TickMicro(SquadCA owner, WPos anchor)
		{
			var manager = owner.SquadManager;
			var info = manager.Info;
			var now = owner.World.WorldTick;
			var rules = owner.World.Map.Rules;
			var rangeMargin = WDist.FromCells(info.ArtyRangeMarginCells).Length;
			var defenceMargin = WDist.FromCells(info.ArtyDefenceMarginCells).Length;
			var kiteMargin = WDist.FromCells(info.ArtyKiteMarginCells).Length;
			var threatRadius = WDist.FromCells(info.ArtyThreatRadiusCells).Length;
			var maxStep = WDist.FromCells(info.ArtyMaxStepCells).Length;

			structuresGathered = false;
			if (memory.Count > owner.Units.Count * 2 + 8)
				memory.Clear();

			foreach (var u in owner.Units)
			{
				var unit = u.Actor;
				if (manager.unitCannotBeOrdered(unit))
					continue;

				memory.TryGetValue(unit.ActorID, out var mem);
				if (now < mem.NextThinkTick)
					continue;

				mem.NextThinkTick = now + info.ArtyOrderCooldownTicks;
				memory[unit.ActorID] = mem;

				var rangeLen = BotUnitProfiles.Get(rules, unit.Info).MaxRange.Length;
				if (rangeLen <= 0)
				{
					LegacyUnitOrder(owner, u, anchor);
					continue;
				}

				var pos = unit.CenterPosition;
				var scanLen = rangeLen + Math.Max(kiteMargin, defenceMargin) + WDist.FromCells(4).Length;
				GatherEnemies(owner, unit, pos, scanLen, threatRadius, kiteMargin, rangeLen);

				// 1. Kite: the nearest hostile unit that closes in, when this unit outranges it.
				if (TryKite(owner, unit, pos, rangeLen, kiteMargin, maxStep, ref mem))
				{
					memory[unit.ActorID] = mem;
					continue;
				}

				// 2. Never stand inside a defence's reach: back off to outrange it again.
				if (TryDefenceBackoff(owner, unit, pos, rangeLen, rangeMargin, defenceMargin, ref mem))
				{
					memory[unit.ActorID] = mem;
					continue;
				}

				// 3. Attack the best target in range.
				var pick = ArtyMicroCA.PickTarget(pos, rangeLen, candidates, manager.ArtyStructFirstActive);
				if (pick >= 0)
				{
					var target = candidateActors[pick];
					if (mem.LastAttackId != target.ActorID || !BusyAttack(unit))
					{
						if (!manager.TryConsumeMicroActions())
							continue;

						owner.Bot.QueueOrder(new Order("Attack", unit, Target.FromActor(target), false));
						mem.LastAttackId = target.ActorID;
						memory[unit.ActorID] = mem;
					}

					continue;
				}

				// 4. Nothing in range to shoot now. ARTY-TARGET-01 (c2): with the structure-first switch the artillery aims at structures - a seen or
				// remembered defence first, then another structure, near its anchor - and a unit squad target is taken only when no structure is known.
				var targetIsUnit = owner.Target.Type == TargetType.Actor && owner.Target.Actor != null && !owner.Target.Actor.Info.HasTraitInfo<BuildingInfo>();
				if (manager.ArtyStructFirstActive && (targetIsUnit || !owner.Target.IsValidFor(unit))
					&& TryStructuralAssault(owner, unit, pos, anchor, rangeLen, rangeMargin, defenceMargin, ref mem))
				{
					memory[unit.ActorID] = mem;
					continue;
				}

				// The squad target (possibly a remembered building) when it is in range, else approach it.
				if (!owner.Target.IsValidFor(unit))
				{
					LegacyUnitOrder(owner, u, anchor);
					continue;
				}

				var targetPos = owner.Target.CenterPosition;
				if ((targetPos - pos).HorizontalLengthSquared <= (long)rangeLen * rangeLen)
				{
					if (mem.LastAttackId == uint.MaxValue && BusyAttack(unit))
						continue;

					if (!manager.TryConsumeMicroActions())
						continue;

					owner.Bot.QueueOrder(new Order("Attack", unit, owner.Target, false));
					mem.LastAttackId = uint.MaxValue;
					memory[unit.ActorID] = mem;
					continue;
				}

				var plan = ArtyMicroCA.PlanApproach(pos, targetPos, rangeLen, rangeMargin, defences, defenceMargin);
				if (plan.Kind == ArtyApproachKind.Move)
				{
					if (!manager.TryConsumeMicroActions())
						continue;

					owner.Bot.QueueOrder(new Order("Move", unit, Target.FromPos(plan.Point), false));
					mem.LastAttackId = 0;
					memory[unit.ActorID] = mem;
				}
			}
		}

		// Fills candidates/candidateActors/defences with what this unit can see within scanLen: observed enemies from the actor
		// layer, plus remembered enemy defences (FrozenActorLayer). Enemies only; fog-honest.
		void GatherEnemies(SquadCA owner, Actor unit, WPos pos, int scanLen, int threatRadius, int kiteMargin, int ownRangeLen)
		{
			candidates.Clear();
			candidateActors.Clear();
			defences.Clear();

			var manager = owner.SquadManager;
			var rules = owner.World.Map.Rules;
			foreach (var e in owner.World.FindActorsInCircle(pos, new WDist(scanLen)))
			{
				if (e == unit || !manager.IsPreferredObservedEnemyUnit(e) || e.GetEnabledTargetTypes().Overlaps(WallTargetType))
					continue;

				var isBuilding = e.Info.HasTraitInfo<BuildingInfo>();
				var armed = e.Info.HasTraitInfo<AttackBaseInfo>();
				var reach = armed ? BotUnitProfiles.Get(rules, e.Info).MaxRange.Length : 0;
				if (isBuilding)
				{
					var kind = armed ? ArtyKind.Defence : ArtyKind.Structure;
					candidates.Add(new ArtyCandidate(e.ActorID, e.CenterPosition, kind, false));
					candidateActors.Add(e);
					if (armed)
						defences.Add(new ArtyDefence(e.CenterPosition, reach));
				}
				else
				{
					var d = (e.CenterPosition - pos).HorizontalLength;
					var threatens = armed && (d <= threatRadius || d <= reach + kiteMargin);
					candidates.Add(new ArtyCandidate(e.ActorID, e.CenterPosition, ArtyKind.Unit, threatens));
					candidateActors.Add(e);
				}
			}

			var layer = owner.SquadManager.Player.FrozenActorLayer;
			if (layer == null)
				return;

			foreach (var fa in layer.FrozenActorsInCircle(owner.World, pos, new WDist(scanLen)))
			{
				if (!fa.IsValid || fa.Hidden || fa.Owner == null || (fa.Actor != null && fa.Actor.IsInWorld && manager.IsNotHiddenUnit(fa.Actor))
					|| owner.SquadManager.Player.RelationshipWith(fa.Owner) != PlayerRelationship.Enemy
					|| !fa.Info.HasTraitInfo<BuildingInfo>() || !fa.Info.HasTraitInfo<AttackBaseInfo>())
					continue;

				defences.Add(new ArtyDefence(fa.CenterPosition, BotUnitProfiles.Get(rules, fa.Info).MaxRange.Length));
			}
		}

		// ARTY-TARGET-01 (c2): the structure-first aim. Attacks the nearest structure in range, or moves to its firing point (stand-off at max range,
		// never into a known defence's reach). Returns false only when no structure is seen or remembered near the anchor: the caller then falls back.
		bool TryStructuralAssault(SquadCA owner, Actor unit, WPos pos, WPos anchor, int rangeLen, int rangeMargin, int defenceMargin, ref ArtyMemory mem)
		{
			if (!structuresGathered)
			{
				GatherStructures(owner, anchor);
				structuresGathered = true;
			}

			// c4: a remembered-only structure in range has no live target to fire at, so it is skipped and the next known structure is taken.
			var pick = ArtyMicroCA.PickStructure(anchor, structures, i => structureActors[i] != null || (structures[i].Pos - pos).HorizontalLengthSquared > (long)rangeLen * rangeLen);
			if (pick < 0)
				return false;

			var targetPos = structures[pick].Pos;
			if ((targetPos - pos).HorizontalLengthSquared <= (long)rangeLen * rangeLen)
			{
				var actor = structureActors[pick];
				if (actor == null)
					return false; // unreachable after the c4 filter above; kept as a guard

				if (mem.LastAttackId == actor.ActorID && BusyAttack(unit))
					return true; // already firing at it

				if (owner.SquadManager.TryConsumeMicroActions())
				{
					owner.Bot.QueueOrder(new Order("Attack", unit, Target.FromActor(actor), false));
					mem.LastAttackId = actor.ActorID;
				}

				return true;
			}

			var plan = ArtyMicroCA.PlanApproach(pos, targetPos, rangeLen, rangeMargin, assaultDefences, defenceMargin);
			if (plan.Kind == ArtyApproachKind.Move && owner.SquadManager.TryConsumeMicroActions())
			{
				owner.Bot.QueueOrder(new Order("Move", unit, Target.FromPos(plan.Point), false));
				mem.LastAttackId = 0;
			}

			return true; // Hold or Blocked: the unit stays where it is
		}

		// Fills structures (seen enemy buildings and remembered ones) and defences (armed ones among them) within ArtyAssaultScanCells of the anchor. Walls are not targets.
		void GatherStructures(SquadCA owner, WPos anchor)
		{
			structures.Clear();
			structureActors.Clear();
			assaultDefences.Clear();

			var manager = owner.SquadManager;
			var rules = owner.World.Map.Rules;
			var scanLen = WDist.FromCells(manager.Info.ArtyAssaultScanCells);
			foreach (var e in owner.World.FindActorsInCircle(anchor, scanLen))
			{
				if (!e.Info.HasTraitInfo<BuildingInfo>() || !manager.IsPreferredObservedEnemyUnit(e) || e.GetEnabledTargetTypes().Overlaps(WallTargetType))
					continue;

				var armed = e.Info.HasTraitInfo<AttackBaseInfo>();
				structures.Add(new ArtyCandidate(e.ActorID, e.CenterPosition, armed ? ArtyKind.Defence : ArtyKind.Structure, false));
				structureActors.Add(e);
				if (armed)
					assaultDefences.Add(new ArtyDefence(e.CenterPosition, BotUnitProfiles.Get(rules, e.Info).MaxRange.Length));
			}

			var layer = manager.Player.FrozenActorLayer;
			if (layer == null)
				return;

			foreach (var fa in layer.FrozenActorsInCircle(owner.World, anchor, scanLen))
			{
				if (!fa.IsValid || fa.Hidden || fa.Owner == null || (fa.Actor != null && fa.Actor.IsInWorld && manager.IsNotHiddenUnit(fa.Actor))
					|| manager.Player.RelationshipWith(fa.Owner) != PlayerRelationship.Enemy || !fa.Info.HasTraitInfo<BuildingInfo>() || fa.TargetTypes.Overlaps(WallTargetType))
					continue;

				var armed = fa.Info.HasTraitInfo<AttackBaseInfo>();
				structures.Add(new ArtyCandidate(0, fa.CenterPosition, armed ? ArtyKind.Defence : ArtyKind.Structure, false));
				structureActors.Add(null);
				if (armed)
					assaultDefences.Add(new ArtyDefence(fa.CenterPosition, BotUnitProfiles.Get(rules, fa.Info).MaxRange.Length));
			}
		}

		bool TryKite(SquadCA owner, Actor unit, WPos pos, int rangeLen, int kiteMargin, int maxStep, ref ArtyMemory mem)
		{
			var rules = owner.World.Map.Rules;
			Actor nearest = null;
			var nearestSq = long.MaxValue;
			for (var i = 0; i < candidates.Count; i++)
			{
				if (candidates[i].Kind != ArtyKind.Unit || !candidates[i].Threatens)
					continue;

				var d = (candidates[i].Pos - pos).HorizontalLengthSquared;
				if (d < nearestSq)
				{
					nearestSq = d;
					nearest = candidateActors[i];
				}
			}

			if (nearest == null)
				return false;

			var standoff = ArtyMicroCA.KiteStandoff(rangeLen, BotUnitProfiles.Get(rules, nearest.Info).MaxRange.Length, kiteMargin);
			if (standoff == null)
				return false;

			var point = ArtyMicroCA.KitePoint(pos, nearest.CenterPosition, standoff.Value, maxStep);
			if (point == null || !owner.SquadManager.TryConsumeMicroActions())
				return false;

			owner.Bot.QueueOrder(new Order("Move", unit, Target.FromPos(point.Value), false));
			mem.LastAttackId = 0;
			return true;
		}

		bool TryDefenceBackoff(SquadCA owner, Actor unit, WPos pos, int rangeLen, int rangeMargin, int defenceMargin, ref ArtyMemory mem)
		{
			for (var i = 0; i < defences.Count; i++)
			{
				var point = ArtyMicroCA.DefenceBackoff(pos, defences[i], rangeLen, rangeMargin, defenceMargin);
				if (point == null)
					continue;

				if (!owner.SquadManager.TryConsumeMicroActions())
					return false;

				owner.Bot.QueueOrder(new Order("Move", unit, Target.FromPos(point.Value), false));
				mem.LastAttackId = 0;
				return true;
			}

			return false;
		}

		public void Deactivate(SquadCA owner) { }
	}

	// §12.4a: fire-support squads screen the artillery (or assault) squad they are
	// parented to. Members hold at the protected squad's position and AttackMove
	// back into screen range — escorts fight whatever threatens the parent instead
	// of drifting to the front or idling at home.
	class FireSupportUnitsIdleStateCA : GroundStateBaseCA, IState
	{
		const int HoldTicks = 250;
		const int OrderCooldownTicks = 25;
		int holdTicks;
		int orderCooldown;

		public void Activate(SquadCA owner) { }

		public void Tick(SquadCA owner)
		{
			if (!owner.IsValid)
				return;

			var parent = owner.Parent;
			if (parent == null || !parent.IsValid)
			{
				// Re-attach to the biggest artillery squad first, else an assault.
				parent = owner.SquadManager.Squads
					.Where(s => s.Type == SquadCAType.Artillery && s.IsValid && s != owner)
					.MaxByOrDefault(s => s.Units.Count)
					?? owner.SquadManager.FindAttachableAssault(owner);
				owner.Parent = parent;
			}

			if (parent == null)
			{
				// Nothing to screen: hold near home like the support state rather
				// than trickling into the enemy alone.
				if (--holdTicks <= 0)
				{
					GoToRandomOwnBuilding(owner);
					holdTicks = HoldTicks;
				}

				return;
			}

			// Re-check follow range on a cooldown - issuing AttackMove every tick
			// resets the unit's current activity before it can close the distance.
			if (--orderCooldown > 0)
				return;

			orderCooldown = OrderCooldownTicks;
			var followRangeSquared = (long)WDist.FromCells(owner.SquadManager.Info.SupportFollowRangeCells).LengthSquared;
			var parentPos = parent.CenterPosition;
			foreach (var u in owner.Units)
			{
				if ((u.Actor.CenterPosition - parentPos).LengthSquared <= followRangeSquared)
					continue;

				owner.Bot.QueueOrder(new Order("AttackMove", u.Actor, Target.FromPos(parentPos), false));
			}
		}

		public void Deactivate(SquadCA owner) { }
	}

}
