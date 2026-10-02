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
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits.BotModules.Squads
{
	// CV (AI_ARCHITECTURE 12.7a, unified with ATK-1 2026-10-02): between contact and the first
	// shot a Rush squad deploys into a range-matched concave arc (ConcaveEvalCA plans it), then
	// commits with staggered AttackMove orders so every member reaches its own firing range on
	// the same tick, then hands over to GroundUnitsAttackState. Orders only; every order spends
	// one micro action. Tunables and the on/off switch come from the IBotAssaultFormation
	// provider (AssaultFormationBotModule): no enabled provider means this state never runs.
	class GroundUnitsConcaveStateCA : GroundStateBaseCA, IState
	{
		const int ReplanMinTicks = 25;
		const int ReplanAnchorCells = 3;
		const int FormedRadius = 1536;
		const int MaxFormOrders = 3;
		const int CommitRetryTicks = 60;
		static readonly BitSet<TargetableType> InfantryTargetTypes = new("Infantry");

		enum CommitReason { None, Formed, UnderFire }

		sealed class Placed
		{
			public Actor Actor;
			public int Range;
			public int Speed;
			public WPos Slot;
			public int Orders;
			public int LastHp;
		}

		sealed class Pending
		{
			public Actor Actor;
			public int DueTick;
		}

		List<Placed> placed;
		readonly HashSet<uint> plannedIds = [];
		readonly List<Pending> pending = [];
		AssaultFormationSettings settings;
		IBotAssaultFormation provider;
		CPos cooldownCell;
		WPos planAnchor;
		WPos anchor;
		int frontDepth;
		int lastPlanTick = int.MinValue / 2;
		int formStartTick;
		bool committed;

		public void Activate(SquadCA owner)
		{
			formStartTick = owner.World.WorldTick;

			// Entry already consulted the provider, so it resolves here; if it vanished since
			// (condition flipped), fall back to the plain advance.
			var members = Eligible(owner);
			if (members.Count == 0 || !TryGetSettings(owner, CooldownCell(owner, members), out settings, out provider))
			{
				owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsAttackMoveStateCA(), false);
				return;
			}

			// A re-entry is not allowed until the provider's cooldown has run; record now so an
			// abort or a commit both restart it (RecordFanout again) from their own moment.
			cooldownCell = CooldownCell(owner, members);
			provider.RecordFanout(owner, cooldownCell);
		}

		public void Deactivate(SquadCA owner) { }

		// The provider consult shared by the entry hook and Activate: the enabled
		// IBotAssaultFormation for this squad's bot and the settings it would run with. The
		// provider's own per-squad same-ground cooldown may decline. Cheap and side-effect free.
		internal static bool TryGetSettings(SquadCA owner, CPos cell, out AssaultFormationSettings settings, out IBotAssaultFormation provider)
		{
			settings = default;
			provider = owner.Bot.Player.PlayerActor.TraitsImplementing<IBotAssaultFormation>().FirstEnabledTraitOrDefault();
			return provider != null && provider.TryGetAssaultFormation(owner, cell, out settings);
		}

		// The ground the cooldown is keyed on: the squad target, else the frontline centroid.
		static CPos CooldownCell(SquadCA owner, List<Actor> members)
		{
			if (owner.IsTargetValid)
				return owner.World.Map.CellContaining(owner.Target.CenterPosition);

			return owner.World.Map.CellContaining(Centroid(members));
		}

		// The trigger, read from the attack-move state before its own scan: a Rush squad with an
		// enabled provider, enough weaponed ground members, off the provider's cooldown, and an
		// observed enemy (or the squad target) within FanoutTriggerCells of the frontline centroid.
		public static bool ShouldEnter(SquadCA owner)
		{
			// Cheapest gates first: classic and switch-off bots have no enabled provider and pay nothing more.
			if (owner.Type != SquadCAType.Rush
				|| owner.Bot.Player.PlayerActor.TraitsImplementing<IBotAssaultFormation>().FirstEnabledTraitOrDefault() == null)
				return false;

			var members = Eligible(owner);
			if (members.Count == 0)
				return false;

			if (!TryGetSettings(owner, CooldownCell(owner, members), out var settings, out _) || members.Count < settings.MinSquadSize)
				return false;

			// Formation is for the approach, not the fight: if a visible enemy is
			// already inside scan range of any member (our fight or a
			// neighbour's), the attack-move state's all-in path handles it.
			if (GroundUnitsAttackMoveStateCA.ContactNearSquad(owner, WDist.FromCells(owner.SquadManager.Info.AttackScanRadius)))
				return false;

			var centroid = Centroid(members);
			var contact = WDist.FromCells(settings.FanoutTriggerCells);
			if (owner.IsTargetValid
				&& (owner.Target.CenterPosition - centroid).HorizontalLengthSquared <= (long)contact.Length * contact.Length)
				return true;

			return GatherEnemies(owner, centroid, contact).Count > 0;
		}

		public void Tick(SquadCA owner)
		{
			if (!owner.IsValid)
				return;

			var tick = owner.World.WorldTick;
			if (committed)
			{
				TickCommit(owner, tick);
				return;
			}

			var members = Eligible(owner);
			if (members.Count == 0)
			{
				Abort(owner);
				return;
			}

			var centroid = Centroid(members);
			var contact = WDist.FromCells(settings.FanoutTriggerCells);
			var enemies = GatherEnemies(owner, centroid, contact);
			if (!TryAnchor(owner, enemies, centroid, out var newAnchor, out var newDepth))
			{
				Abort(owner);
				return;
			}

			anchor = newAnchor;
			frontDepth = newDepth;

			var replanRange = WDist.FromCells(ReplanAnchorCells);

			// Re-plan when the anchor moved, or when an eligible member joined while forming (a
			// late joiner gets a slot); both are rate-limited by ReplanMinTicks.
			if (placed == null
				|| (tick - lastPlanTick >= ReplanMinTicks
					&& ((anchor - planAnchor).HorizontalLengthSquared > (long)replanRange.Length * replanRange.Length
						|| members.Any(m => !plannedIds.Contains(m.ActorID)))))
			{
				if (!Plan(owner, members, centroid))
				{
					Abort(owner);
					return;
				}

				lastPlanTick = tick;
				planAnchor = anchor;
			}

			var reason = ShouldCommit(owner, tick, enemies, members);
			if (reason != CommitReason.None)
			{
				Commit(owner, members, enemies, tick, reason == CommitReason.UnderFire);
				TickCommit(owner, tick);
				return;
			}

			foreach (var p in placed)
			{
				if (owner.SquadManager.unitCannotBeOrdered(p.Actor))
					continue;

				// The first form order goes out even to a member still on the march AttackMove;
				// re-issues only for members that are idle, off their slot and under the retry cap.
				if (p.Orders > 0 && (!p.Actor.IsIdle || p.Orders >= MaxFormOrders))
					continue;

				if ((p.Actor.CenterPosition - p.Slot).HorizontalLengthSquared <= (long)FormedRadius * FormedRadius)
					continue;

				if (!owner.SquadManager.TryConsumeMicroActions())
					continue;

				p.Orders++;
				owner.Bot.QueueOrder(new Order("Move", p.Actor, Target.FromCell(owner.World, owner.World.Map.CellContaining(p.Slot)), false));
			}
		}

		bool Plan(SquadCA owner, List<Actor> members, WPos centroid)
		{
			var rules = owner.World.Map.Rules;
			var inputs = new List<ConcaveMember>(members.Count);
			var profiles = new List<BotUnitProfile>(members.Count);
			foreach (var m in members)
			{
				var profile = BotUnitProfiles.Get(rules, m.Info);
				profiles.Add(profile);
				inputs.Add(new ConcaveMember(m.ActorID, m.CenterPosition, profile.MaxRange.Length, profile.Speed,
					profile.TargetTypes.Overlaps(InfantryTargetTypes)));
			}

			var p = new ConcaveParams(
				WDist.FromCells(settings.StageMarginCells).Length,
				WDist.FromCells(settings.RankBandCells).Length,
				settings.Spacing,
				settings.MinSpacing,
				settings.ArcDegrees,
				settings.RankGap);

			var slots = ConcaveEvalCA.Plan(anchor, centroid, frontDepth, inputs, p);

			var result = new List<Placed>(members.Count);
			var old = placed?.ToDictionary(o => o.Actor);
			plannedIds.Clear();
			foreach (var m in members)
				plannedIds.Add(m.ActorID);

			var valid = 0;
			for (var i = 0; i < members.Count; i++)
			{
				if (!TrySnap(owner.World, members[i], slots[i].Pos, settings.SlotReachCells, out var cell))
					continue;

				valid++;
				var slot = owner.World.Map.CenterOfCell(cell);
				if (old != null && old.TryGetValue(members[i], out var prev))
				{
					// Slot moved at most a cell: keep the record (and its order count); else a fresh order.
					if ((prev.Slot - slot).HorizontalLengthSquared > 1024L * 1024)
					{
						prev.Slot = slot;
						prev.Orders = 0;
					}

					result.Add(prev);
					continue;
				}

				result.Add(new Placed
				{
					Actor = members[i],
					Range = profiles[i].MaxRange.Length,
					Speed = profiles[i].Speed,
					Slot = slot,
					LastHp = HpOf(members[i]),
				});
			}

			// A choke or a cliff edge: too few usable slots, engage as today.
			if (valid * 100 < members.Count * settings.MinValidSlotPct)
				return false;

			placed = result;
			return true;
		}

		// The nearest cell within reachCells of the slot the member's locomotor can enter, stay in
		// and path to. Mirrors HarvesterBotModuleCA's retreat-cell check.
		static bool TrySnap(World world, Actor actor, WPos slot, int reachCells, out CPos cell)
		{
			cell = default;
			var mobile = actor.TraitOrDefault<Mobile>();
			var target = world.Map.CellContaining(slot);
			if (mobile == null || !world.Map.Contains(target))
				return false;

			bool Valid(CPos c) => world.Map.Contains(c)
				&& mobile.CanEnterCell(c, check: BlockedByActor.Immovable)
				&& mobile.CanStayInCell(c)
				&& mobile.PathFinder.PathMightExistForLocomotorBlockedByImmovable(mobile.Locomotor, actor.Location, c);

			cell = mobile.NearestCell(target, Valid, 0, reachCells);
			return Valid(cell);
		}

		CommitReason ShouldCommit(SquadCA owner, int tick, List<Actor> enemies, List<Actor> members)
		{
			var info = owner.SquadManager.Info;

			// Battle joined anywhere near the squad — a member under fire or an
			// allied fight beside the forming arc: commit now. The under-fire
			// path zeroes every stagger delay so the whole squad attack-moves on
			// the same tick instead of staging one prong at a time (Lanchester).
			var memberPositions = new List<WPos>(members.Count);
			foreach (var m in members)
				memberPositions.Add(m.CenterPosition);

			var enemyPositions = new List<WPos>(enemies.Count);
			foreach (var e in enemies)
				enemyPositions.Add(e.CenterPosition);

			if (SquadMicroEvalCA.ContactNear(memberPositions, enemyPositions, WDist.FromCells(info.AttackScanRadius)))
				return CommitReason.UnderFire;

			// Evaluate every member first (LastHp must update for all), then decide.
			var near = 0;
			var damaged = false;
			var enemyInRange = false;
			foreach (var p in placed)
			{
				if ((p.Actor.CenterPosition - p.Slot).HorizontalLengthSquared <= (long)FormedRadius * FormedRadius)
					near++;

				var hp = HpOf(p.Actor);
				if (hp < p.LastHp)
					damaged = true;
				p.LastHp = hp;

				// The enemy engaged us: never keep forming under fire.
				if (!enemyInRange)
					foreach (var e in enemies)
						if ((e.CenterPosition - p.Actor.CenterPosition).HorizontalLengthSquared <= (long)p.Range * p.Range)
						{
							enemyInRange = true;
							break;
						}
			}

			if (damaged || enemyInRange)
				return CommitReason.UnderFire;

			if (tick - formStartTick >= settings.StageDeadlineTicks || near * 100 >= placed.Count * settings.AssemblePercent)
				return CommitReason.Formed;

			return CommitReason.None;
		}

		// Staggered arrival: member i attack-moves max(t) - t_i ticks after the commit, where
		// t_i is the time from where it stands to its own firing range.
		void Commit(SquadCA owner, List<Actor> members, List<Actor> enemies, int tick, bool underFire)
		{
			committed = true;
			provider.RecordFanout(owner, cooldownCell);

			// The attack state fights owner.TargetActor: the observed enemy nearest the anchor.
			Actor nearest = null;
			var best = long.MaxValue;
			foreach (var e in enemies)
			{
				var d = (e.CenterPosition - anchor).HorizontalLengthSquared;
				if (d < best)
				{
					best = d;
					nearest = e;
				}
			}

			if (nearest != null)
				owner.TargetActor = nearest;

			var distances = placed.ConvertAll(p => ConcaveEvalCA.DistanceToRange(p.Actor.CenterPosition, anchor, frontDepth, p.Range));
			var speeds = placed.ConvertAll(p => p.Speed);
			var delays = ConcaveEvalCA.CommitDelays(distances, speeds, underFire);
			for (var i = 0; i < placed.Count; i++)
				pending.Add(new Pending { Actor = placed[i].Actor, DueTick = tick + delays[i] });

			// Weaponed members without a usable slot join at once.
			var placedActors = placed.Select(p => p.Actor).ToHashSet();
			foreach (var m in members)
				if (!placedActors.Contains(m))
					pending.Add(new Pending { Actor = m, DueTick = tick });
		}

		void TickCommit(SquadCA owner, int tick)
		{
			for (var i = pending.Count - 1; i >= 0; i--)
			{
				var p = pending[i];
				if (owner.SquadManager.unitCannotBeOrdered(p.Actor) || tick > p.DueTick + CommitRetryTicks)
				{
					pending.RemoveAt(i);
					continue;
				}

				if (tick < p.DueTick)
					continue;

				// Denied by the action budget: the member keeps its last order and retries next tick.
				if (!owner.SquadManager.TryConsumeMicroActions())
					continue;

				owner.Bot.QueueOrder(new Order("AttackMove", p.Actor, Target.FromPos(anchor), false));
				pending.RemoveAt(i);
			}

			if (pending.Count == 0)
				owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsAttackState(), false);
		}

		void Abort(SquadCA owner)
		{
			provider?.RecordFanout(owner, cooldownCell);
			owner.FuzzyStateMachine.ChangeState(owner, new GroundUnitsAttackMoveStateCA(), false);
		}

		// Anchor: centroid of the observed enemies in contact, else the squad target. Front depth:
		// how far the enemy line already sits toward us along anchor -> frontline centroid.
		static bool TryAnchor(SquadCA owner, List<Actor> enemies, WPos centroid, out WPos anchorPos, out int depth)
		{
			depth = 0;
			anchorPos = default;
			if (enemies.Count == 0)
			{
				if (!owner.IsTargetValid)
					return false;

				anchorPos = owner.Target.CenterPosition;
				return true;
			}

			long x = 0, y = 0;
			foreach (var e in enemies)
			{
				x += e.CenterPosition.X;
				y += e.CenterPosition.Y;
			}

			anchorPos = new WPos((int)(x / enemies.Count), (int)(y / enemies.Count), 0);
			var axis = centroid - anchorPos;
			var axisLen = (long)axis.HorizontalLength;
			if (axisLen > 0)
			{
				long front = 0;
				foreach (var e in enemies)
				{
					var d = e.CenterPosition - anchorPos;
					front = Math.Max(front, ((long)d.X * axis.X + (long)d.Y * axis.Y) / axisLen);
				}

				depth = (int)front;
			}

			return true;
		}

		// Observed ARMED enemy units around the frontline centroid (a lone harvester is no reason to deploy). IsPreferredObservedEnemyUnit is the
		// only enemy filter: a unit the bot cannot currently see or legitimately remember is not here.
		static List<Actor> GatherEnemies(SquadCA owner, WPos centroid, WDist radius)
		{
			return owner.World.FindActorsInCircle(centroid, radius)
				.Where(owner.SquadManager.IsPreferredObservedEnemyUnit)
				.Where(e => BotUnitProfiles.Get(owner.World.Map.Rules, e.Info).MaxRange.Length > 0).ToList();
		}

		// Orderable, mobile ground members that have a weapon and are not scouts.
		static List<Actor> Eligible(SquadCA owner)
		{
			var roleMap = owner.SquadManager.ActorRoles;
			var rules = owner.World.Map.Rules;
			var result = new List<Actor>();
			foreach (var u in owner.Units)
			{
				var a = u.Actor;
				if (owner.SquadManager.unitCannotBeOrdered(a) || a.TraitOrDefault<Mobile>() == null)
					continue;

				if (roleMap != null && roleMap.TryGetValue(a.Info.Name, out var roles) && roles.Contains(BotUnitRole.Scout))
					continue;

				if (BotUnitProfiles.Get(rules, a.Info).MaxRange.Length <= 0)
					continue;

				result.Add(a);
			}

			return result;
		}

		static WPos Centroid(List<Actor> members)
		{
			long x = 0, y = 0;
			foreach (var m in members)
			{
				x += m.CenterPosition.X;
				y += m.CenterPosition.Y;
			}

			return new WPos((int)(x / members.Count), (int)(y / members.Count), 0);
		}

		static int HpOf(Actor a)
		{
			return a.TraitOrDefault<IHealth>()?.HP ?? 0;
		}
	}
}
