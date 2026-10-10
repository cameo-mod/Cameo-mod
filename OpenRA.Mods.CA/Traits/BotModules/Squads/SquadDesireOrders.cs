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
		// Maintainer override: every declared squad chassis uses the same learned controller.
		// Invalid/unrecognized enum values are not silently treated as supported roles.
		public static bool Supports(SquadCAType type) => Enum.IsDefined(type);

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

	// The live adapter supplies the same fog-filtered observations and manager guards.
	// Tests replace world access, not the controller or its order-emission loop.
	internal interface ISquadDesireExecution<T>
	{
		IReadOnlyList<T> Members { get; }
		CPos Location(T member);
		bool IsIdle(T member);
		CPos Home { get; }
		CPos? Reinforcement { get; }
		(SquadDesireStance Stance, bool Safe, CPos? Target) Evaluate(IBotSquadDesire provider);
		void QueueOrder(T member, string order, CPos cell);
	}

	internal sealed class LiveSquadDesireExecution : ISquadDesireExecution<Actor>
	{
		readonly SquadCA squad;
		readonly Actor[] members;
		public LiveSquadDesireExecution(SquadCA squad)
		{
			this.squad = squad;
			members = squad.Units.Select(u => u.Actor).Where(a => !squad.SquadManager.unitCannotBeOrdered(a))
				.OrderBy(a => a.ActorID).ToArray();
		}

		public IReadOnlyList<Actor> Members => members;
		public CPos Location(Actor member) => member.Location;
		public bool IsIdle(Actor member) => member.IsIdle;
		public CPos Home => squad.SquadManager.DesireHome;
		public CPos? Reinforcement => squad.SquadManager.FindAttachableAssault(squad)?.Units[0].Actor.Location;
		public void QueueOrder(Actor member, string order, CPos cell) =>
			squad.Bot.QueueOrder(new Order(order, member, Target.FromCell(squad.World, cell), false));

		public (SquadDesireStance Stance, bool Safe, CPos? Target) Evaluate(IBotSquadDesire provider)
		{
			var manager = squad.SquadManager;
			var world = squad.World;
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
			return (stance, safe, commitTarget?.Location);
		}
	}

	// Runs at the manager's regular cadence. Production and regressions share this controller.
	internal sealed class SquadDesireController<T> where T : notnull
	{
		readonly Dictionary<T, (string Order, CPos Cell)> orders = new();
		CPos? assemblyCell;
		SquadDesireDestination? previousDestination;

		public void Tick(ISquadDesireExecution<T> execution, IBotSquadDesire provider)
		{
			var members = execution.Members;
			if (members.Count == 0)
				return;
			var evaluation = execution.Evaluate(provider);
			var plan = SquadDesireOrders.Plan(evaluation.Stance, evaluation.Safe, evaluation.Target != null);
			if (plan.Destination == SquadDesireDestination.Assembly && previousDestination != plan.Destination)
				assemblyCell = new CPos((int)(members.Sum(a => (long)execution.Location(a).X) / members.Count),
					(int)(members.Sum(a => (long)execution.Location(a).Y) / members.Count));
			previousDestination = plan.Destination;
			// Preserve the existing reinforcement/home reads on every armed evaluation.
			var reinforcement = execution.Reinforcement;
			var home = execution.Home;
			var cell = plan.Destination switch
			{
				SquadDesireDestination.Enemy or SquadDesireDestination.Raid => evaluation.Target.Value,
				SquadDesireDestination.Assembly => assemblyCell ?? execution.Location(members[0]),
				SquadDesireDestination.Reinforcement => reinforcement ?? home,
				_ => home
			};
			foreach (var member in members)
			{
				var key = (plan.Order, cell);
				if (orders.TryGetValue(member, out var previous) && previous == key
					&& (!execution.IsIdle(member) || (execution.Location(member) - cell).LengthSquared <= 4))
					continue;
				orders[member] = key;
				execution.QueueOrder(member, plan.Order, cell);
			}
			foreach (var removed in orders.Keys.Where(a => !members.Contains(a)).ToArray())
				orders.Remove(removed);
		}
	}
}
