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
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits.BotModules.Squads
{
	public enum SquadDesireDestination { Enemy, Home, Assembly, Raid, Reinforcement }
	public readonly record struct SquadDesireOrder(string Order, SquadDesireDestination Destination);

	// Execution policy, not urgency: safety is independent of integrator dwell/bias.
	public static class SquadDesireOrders
	{
		public static bool Supports(SquadCAType type) => type is SquadCAType.Rush or SquadCAType.Guerrilla
			or SquadCAType.Harass or SquadCAType.Protection;

		public static bool CanEngage(int ratioMilli, int retreatRatioPct, int engageMarginPct) =>
			ratioMilli >= 1000 && (long)ratioMilli * 10 >= (long)Math.Max(0, retreatRatioPct) * Math.Max(0, engageMarginPct);

		public static SquadDesireOrder Plan(SquadDesireStance stance, bool canEngage, bool hasTarget)
		{
			if ((stance is SquadDesireStance.Attack or SquadDesireStance.Harass) && !canEngage)
				return new("Move", SquadDesireDestination.Home);
			return stance switch
			{
				SquadDesireStance.Attack => hasTarget ? new("AttackMove", SquadDesireDestination.Enemy)
					: new("Move", SquadDesireDestination.Assembly),
				SquadDesireStance.Defend => new(canEngage ? "AttackMove" : "Move", SquadDesireDestination.Home),
				SquadDesireStance.Retreat => new("Move", SquadDesireDestination.Home),
				SquadDesireStance.Regroup => new("Move", SquadDesireDestination.Assembly),
				SquadDesireStance.Harass => hasTarget ? new("AttackMove", SquadDesireDestination.Raid)
					: new("Move", SquadDesireDestination.Assembly),
				SquadDesireStance.Reinforce => new("Move", SquadDesireDestination.Reinforcement),
				_ => throw new ArgumentOutOfRangeException(nameof(stance))
			};
		}
	}

	// Runs at the manager's regular squad cadence, including while attacking/defending.
	// Specialized air/naval/artillery FSMs retain their existing role controllers.
	sealed class SquadDesireController
	{
		readonly Dictionary<Actor, (string Order, CPos Cell)> orders = new();
		CPos? assemblyCell;
		SquadDesireDestination? previousDestination;

		public void Tick(SquadCA squad, IBotSquadDesire provider)
		{
			var manager = squad.SquadManager;
			var world = squad.World;
			var members = squad.Units.Select(u => u.Actor).Where(a => !manager.unitCannotBeOrdered(a))
				.OrderBy(a => a.ActorID).ToArray();
			if (members.Length == 0)
				return;

			var from = members[0].CenterPosition;
			var enemies = manager.DesireObservedEnemies();
			var target = enemies.OrderBy(a => (a.CenterPosition - from).HorizontalLengthSquared)
				.ThenBy(a => a.ActorID).FirstOrDefault();
			// A raid prefers an exposed non-combat economic target; it never borrows the
			// main assault's target or treats a hidden actor as a retained live target.
			var raid = enemies.Where(a => !a.Info.HasTraitInfo<AttackBaseInfo>())
				.OrderByDescending(a => a.Info.TraitInfoOrDefault<ValuedInfo>()?.Cost ?? 0)
				.ThenBy(a => (a.CenterPosition - from).HorizontalLengthSquared).ThenBy(a => a.ActorID).FirstOrDefault();
			squad.TargetActor = target;
			var targetEnemies = manager.VisibleEnemiesNear(target?.CenterPosition ?? from,
				WDist.FromCells(manager.Info.IdleScanRadius)).Where(a => a.CanBeViewedByPlayer(squad.Bot.Player)).ToList();
			var signals = manager.SquadDesireSignalsFor(squad, targetEnemies);
			var stance = provider.StanceFor(squad, signals);
			var commitTarget = stance == SquadDesireStance.Harass ? raid : target;
			var commitEnemies = stance == SquadDesireStance.Defend
				? manager.VisibleEnemiesNear(world.Map.CenterOfCell(manager.DesireHome), WDist.FromCells(manager.Info.MaxBaseRadius))
					.Where(a => a.CanBeViewedByPlayer(squad.Bot.Player)).ToList()
				: stance == SquadDesireStance.Harass
				? manager.VisibleEnemiesNear(raid?.CenterPosition ?? from, WDist.FromCells(manager.Info.IdleScanRadius))
					.Where(a => a.CanBeViewedByPlayer(squad.Bot.Player)).ToList() : targetEnemies;
			var danger = manager.VisibleEnemiesNear(from, WDist.FromCells(manager.Info.DangerScanRadius))
				.Where(a => a.CanBeViewedByPlayer(squad.Bot.Player)).ToList();
			var safe = manager.DesireCanEngage(squad, commitEnemies) && manager.DesireCanEngage(squad, danger);
			var plan = SquadDesireOrders.Plan(stance, safe, commitTarget != null);

			// Entering regroup latches a mean cell; moving members cannot drag their own
			// rally point away every eval. Reinforce rallies at an existing assault wave;
			// production requests remain the separate BR_squad_reinforce owner's job.
			if (plan.Destination == SquadDesireDestination.Assembly && previousDestination != plan.Destination)
				assemblyCell = new CPos((int)(members.Sum(a => (long)a.Location.X) / members.Length),
					(int)(members.Sum(a => (long)a.Location.Y) / members.Length));
			previousDestination = plan.Destination;
			var reinforcement = manager.FindAttachableAssault(squad);
			var home = manager.DesireHome;
			var cell = plan.Destination switch
			{
				SquadDesireDestination.Enemy => target.Location,
				SquadDesireDestination.Raid => raid.Location,
				SquadDesireDestination.Assembly => assemblyCell ?? members[0].Location,
				SquadDesireDestination.Reinforcement => reinforcement?.Units[0].Actor.Location ?? home,
				_ => home
			};
			// Defensive AttackMove fights on the way home; emergency retreat is Move.
			// Block repeats while travelling and stop issuing once the rally cell is reached.
			foreach (var member in members)
			{
				var key = (plan.Order, cell);
				if (orders.TryGetValue(member, out var previous) && previous == key
					&& (!member.IsIdle || (member.Location - cell).LengthSquared <= 4))
					continue;
				orders[member] = key;
				squad.Bot.QueueOrder(new Order(plan.Order, member, Target.FromCell(world, cell), false));
			}
			foreach (var removed in orders.Keys.Where(a => !members.Contains(a)).ToArray())
				orders.Remove(removed);
		}
	}
}
