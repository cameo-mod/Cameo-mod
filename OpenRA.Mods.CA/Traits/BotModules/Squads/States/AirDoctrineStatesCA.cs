#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits.BotModules.Squads
{
	// CA-5 (AI_ARCHITECTURE.md 12.8): role-specific idle states for the doctrine
	// squads. They pick targets per role; AirAttackStateCA still flies the strike
	// (threat routing, rearm cycle) and AirFleeStateCA returns here via
	// IdleStateFor.

	class FighterIdleStateCA : AirStateBaseCA, IState
	{
		public void Activate(SquadCA owner) { }

		public void Tick(SquadCA owner)
		{
			if (!owner.IsValid)
				return;

			if (ShouldFlee(owner))
			{
				owner.FuzzyStateMachine.ChangeState(owner, new AirFleeStateCA(), true);
				return;
			}

			var e = FindFighterTarget(owner);
			if (e == null)
				return;

			owner.TargetActor = e;
			owner.FuzzyStateMachine.ChangeState(owner, new AirAttackStateCA(), true);
		}

		// Air superiority first: hunt visible enemy aircraft, preferring the
		// configured big threats; otherwise pick off an enemy unit isolated from
		// its army - few armed allies nearby and light anti-air.
		static Actor FindFighterTarget(SquadCA owner)
		{
			var squadManager = owner.SquadManager;
			var leader = owner.Units[0].Actor;
			var pos = leader.CenterPosition;

			var aircraft = owner.World.Actors
				.Where(a => squadManager.IsPreferredEnemyAircraft(a) && squadManager.IsNotHiddenUnit(a) && a.IsTargetableBy(leader))
				.ToList();

			var target = aircraft.Where(a => squadManager.Info.BigAirThreats.Contains(a.Info.Name)).ClosestToIgnoringPath(pos)
				?? aircraft.ClosestToIgnoringPath(pos);
			if (target != null)
				return target;

			var candidates = owner.World.Actors
				.Where(a => squadManager.IsPreferredObservedEnemyUnit(a) && squadManager.IsNotHiddenUnit(a)
					&& squadManager.IsAirSquadTargetType(a, owner) && a.IsTargetableBy(leader))
				.ToList();

			foreach (var c in squadManager.PreferSquadTargets(candidates, owner, squadManager.TagsOf)
				.OrderBy(c => (c.CenterPosition - pos).LengthSquared))
			{
				if (IsIsolated(owner, c))
					return c;
			}

			return null;
		}

		static bool IsIsolated(SquadCA owner, Actor target)
		{
			var squadManager = owner.SquadManager;
			var near = owner.World.FindActorsInCircle(target.CenterPosition, WDist.FromCells(squadManager.Info.DangerScanRadius))
				.Where(a => squadManager.IsPreferredObservedEnemyUnit(a))
				.ToList();

			return near.Count(a => a.Info.HasTraitInfo<AttackBaseInfo>()) <= squadManager.Info.FighterPickoffMaxEscorts
				&& CountAntiAirUnits(near, owner) < owner.Units.Count;
		}

		public void Deactivate(SquadCA owner) { }
	}

	class GunshipCASStateCA : AirStateBaseCA, IState
	{
		public void Activate(SquadCA owner) { }

		public void Tick(SquadCA owner)
		{
			if (!owner.IsValid)
				return;

			if (ShouldFlee(owner))
			{
				owner.FuzzyStateMachine.ChangeState(owner, new AirFleeStateCA(), true);
				return;
			}

			// 12.8 CAS: attach to the main assault and engage what the frontline
			// engages. The largest ground-combat squad stands in for the frontline.
			var anchor = owner.SquadManager.Squads
				.Where(s => s.IsValid && s != owner &&
					(s.Type == SquadCAType.Rush || s.Type == SquadCAType.Protection ||
					 s.Type == SquadCAType.FireSupport || s.Type == SquadCAType.Guerrilla))
				.MaxByOrDefault(s => s.Units.Count);

			var leader = owner.Units[0].Actor;

			if (anchor == null)
			{
				// No frontline yet: hunt like the generic air wing.
				var fallback = FindDefenselessTarget(owner);
				if (fallback == null)
					return;

				owner.TargetActor = fallback;
				owner.FuzzyStateMachine.ChangeState(owner, new AirAttackStateCA(), true);
				return;
			}

			var anchorPos = anchor.CenterPosition;
			var anchorTarget = anchor.TargetActor;
			var target = anchor.IsTargetValid && anchorTarget != null && NearToPosSafely(owner, anchorTarget.CenterPosition)
				? anchorTarget
				: FindCasTarget(owner, anchorPos);

			if (target != null)
			{
				owner.TargetActor = target;
				owner.FuzzyStateMachine.ChangeState(owner, new AirAttackStateCA(), true);
				return;
			}

			// Nothing to engage: hover over the frontline. A unit already flying
			// there (Fly) is left alone; hovering/landed ones get the move.
			var casRadius = WDist.FromCells(owner.SquadManager.Info.GunshipCASRadiusCells).Length;
			if ((leader.CenterPosition - anchorPos).HorizontalLength <= casRadius)
				return;

			var anchorCell = owner.World.Map.CellContaining(anchorPos);
			foreach (var u in owner.Units)
			{
				var current = u.Actor.CurrentActivity;
				if (current == null || current is FlyIdle)
					owner.Bot.QueueOrder(new Order("Move", u.Actor, Target.FromCell(owner.World, anchorCell), false));
			}
		}

		static Actor FindCasTarget(SquadCA owner, WPos anchorPos)
		{
			var squadManager = owner.SquadManager;
			var leader = owner.Units[0].Actor;
			var radius = WDist.FromCells(squadManager.Info.GunshipCASRadiusCells);

			var candidates = owner.World.FindActorsInCircle(anchorPos, radius)
				.Where(a => squadManager.IsPreferredObservedEnemyUnit(a) && squadManager.IsNotHiddenUnit(a)
					&& squadManager.IsAirSquadTargetType(a, owner) && a.IsTargetableBy(leader))
				.ToList();

			if (candidates.Count == 0)
				return null;

			return squadManager.PreferSquadTargets(candidates, owner, squadManager.TagsOf)
				.Where(a => NearToPosSafely(owner, a.CenterPosition))
				.ClosestToIgnoringPath(anchorPos);
		}

		public void Deactivate(SquadCA owner) { }
	}

	class BomberIdleStateCA : AirStateBaseCA, IState
	{
		public void Activate(SquadCA owner) { }

		public void Tick(SquadCA owner)
		{
			if (!owner.IsValid)
				return;

			if (ShouldFlee(owner))
			{
				owner.FuzzyStateMachine.ChangeState(owner, new AirFleeStateCA(), true);
				return;
			}

			// 12.8: strike teams gather before flying - a short-staffed team holds.
			if (owner.Units.Count < owner.SquadManager.Info.BomberSquadMinSize)
				return;

			var target = FindBomberTarget(owner);

			// A full team does not wait on a tagged target - take an opportunity
			// target like the generic air wing would.
			if (target == null && owner.Units.Count >= owner.SquadManager.Info.BomberSquadMaxSize)
				target = FindDefenselessTarget(owner);

			if (target == null)
				return;

			owner.TargetActor = target;
			owner.FuzzyStateMachine.ChangeState(owner, new AirAttackStateCA(), true);
		}

		// Tag-priority targets (12.8's strike list) the team can hit and whose
		// position is not saturated with anti-air. The strike itself routes over
		// the remembered air-threat layer in AirAttackStateCA.
		static Actor FindBomberTarget(SquadCA owner)
		{
			var squadManager = owner.SquadManager;
			var leader = owner.Units[0].Actor;
			var pos = leader.CenterPosition;

			var candidates = owner.World.Actors
				.Where(a => squadManager.IsPreferredObservedEnemyUnit(a) && squadManager.IsNotHiddenUnit(a)
					&& squadManager.IsAirSquadTargetType(a, owner) && a.IsTargetableBy(leader)
					&& squadManager.TagsOf(a) is { } tags && tags.Overlaps(owner.PriorityTags))
				.OrderBy(a => (a.CenterPosition - pos).LengthSquared);

			foreach (var c in candidates)
				if (NearToPosSafely(owner, c.CenterPosition))
					return c;

			return null;
		}

		public void Deactivate(SquadCA owner) { }
	}
}
