#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Traits
{
	sealed class AttackOrderPowerInfo : SupportPowerInfo, Requires<AttackBaseInfo>
	{
		[Desc("Range circle color.")]
		public readonly Color CircleColor = Color.Red;

		[Desc("Range circle line width.")]
		public readonly float CircleWidth = 1;

		[Desc("Range circle border color.")]
		public readonly Color CircleBorderColor = Color.FromArgb(96, Color.Black);

		[Desc("Range circle border width.")]
		public readonly float CircleBorderWidth = 3;

		public override object Create(ActorInitializer init) { return new AttackOrderPower(init.Self, this); }
	}

	sealed class AttackOrderPower : SupportPower, INotifyCreated, INotifyBurstComplete
	{
		readonly AttackOrderPowerInfo info;
		AttackBase attack;

		public AttackOrderPower(Actor self, AttackOrderPowerInfo info)
			: base(self, info)
		{
			this.info = info;
		}

		public override void SelectTarget(Actor self, string order, SupportPowerManager manager)
		{
			self.World.OrderGenerator = new SelectAttackPowerTarget(self, order, manager, info.Cursor, MouseButton.Left, attack);
		}

		public override void Activate(Actor self, Order order, SupportPowerManager manager)
		{
			base.Activate(self, order, manager);
			PlayLaunchSounds();

			attack.AttackTarget(order.Target, AttackSource.Default, false, false, true);
		}

		protected override void Created(Actor self)
		{
			attack = self.Trait<AttackBase>();

			base.Created(self);
		}

		void INotifyBurstComplete.FiredBurst(Actor self, in Target target, Armament a)
		{
			if (attack != null)
				attack.OnStopOrder(self);
			else
				self.CancelActivity();
		}
	}

	public class SelectAttackPowerTarget : OrderGenerator
	{
		readonly SupportPowerManager manager;
		readonly SupportPowerInstance instance;
		readonly string order;
		readonly string cursor;
		readonly string cursorBlocked;
		readonly MouseButton expectedButton;
		readonly AttackBase attack;

		public SelectAttackPowerTarget(Actor self, string order, SupportPowerManager manager, string cursor, MouseButton button, AttackBase attack)
		{
			// Clear selection if using Left-Click Orders
			if (Game.Settings.Game.UseClassicMouseStyle)
				manager.Self.World.Selection.Clear();

			instance = manager.GetPowersForActor(self).FirstOrDefault();
			this.manager = manager;
			this.order = order;
			this.cursor = cursor;
			expectedButton = button;
			this.attack = attack;
			cursorBlocked = cursor + "-blocked";
		}

		bool TryGetRanges(World world, AttackBase attackTrait, out WDist minRange, out WDist maxRange)
		{
			minRange = WDist.Zero;
			maxRange = WDist.Zero;

			if (attackTrait == null)
				return false;

			try
			{
				minRange = attackTrait.GetMinimumRange();
				maxRange = attackTrait.GetMaximumRange();
				return true;
			}
			catch
			{
				world.CancelInputMode();
				return false;
			}
		}

		AttackBase ResolveAttack(World world)
		{
			if (attack != null && !attack.IsTraitDisabled)
				return attack;

			if (instance == null)
			{
				world.CancelInputMode();
				return null;
			}

			foreach (var power in instance.Instances)
			{
				var actor = power?.Self;
				if (actor == null || !actor.IsInWorld)
					continue;

				var attackTrait = actor.TraitsImplementing<AttackBase>()
					.FirstOrDefault(a => a != null && !a.IsTraitDisabled);

				if (attackTrait != null)
					return attackTrait;
			}

			world.CancelInputMode();
			return null;
		}

		bool TryGetActiveInstances(World world,
			out SupportPowerInstance powerInstance,
			out IReadOnlyCollection<Actor> activeActors)
		{
			powerInstance = instance;
			activeActors = null;
			if (powerInstance == null)
			{
				world.CancelInputMode();
				return false;
			}

			var active = powerInstance.Instances?
				.Where(i => i != null
					&& !i.IsTraitPaused
					&& i.Self != null
					&& !i.Self.IsDead
					&& i.Self.IsInWorld
					&& i.Self.OccupiesSpace != null)
				.Select(i => i.Self)
				.ToList();
			if (active == null || active.Count == 0)
				return false;

			activeActors = active;
			return true;
		}

		bool IsValidTarget(World world, CPos cell)
		{
			var currentAttack = ResolveAttack(world);
			if (currentAttack == null)
				return false;

			if (!TryGetRanges(world, currentAttack, out _, out var maxRange))
				return false;

			if (!TryGetActiveInstances(world, out _, out var activeActors))
				return false;

			var pos = world.Map.CenterOfCell(cell);
			var range = maxRange.LengthSquared;

			if (!world.Map.Contains(cell))
				return false;

			foreach (var a in activeActors)
			{
				if (a == null || a.World == null || !a.IsInWorld)
					continue;

				var occupies = a.OccupiesSpace;
				if (occupies == null)
					continue;

				var center = occupies.CenterPosition;
				if ((center - pos).HorizontalLengthSquared < range)
					return true;
			}

			return false;
		}

		protected override IEnumerable<Order> OrderInner(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			world.CancelInputMode();
			if (mi.Button == expectedButton && IsValidTarget(world, cell))
				yield return new Order(order, manager.Self, Target.FromCell(world, cell), false)
				{
					SuppressVisualFeedback = true
				};
		}

		protected override void Tick(World world)
		{
			// Cancel the OG if we can't use the power
			if (!manager.Powers.TryGetValue(order, out var p) || !p.Active || !p.Ready || instance == null)
				world.CancelInputMode();
		}

		protected override IEnumerable<IRenderable> Render(WorldRenderer wr, World world) { yield break; }
		protected override IEnumerable<IRenderable> RenderAboveShroud(WorldRenderer wr, World world) { yield break; }

		protected override IEnumerable<IRenderable> RenderAnnotations(WorldRenderer wr, World world)
		{
			var currentAttack = ResolveAttack(world);
			if (currentAttack == null)
				yield break;

			if (!TryGetRanges(world, currentAttack, out var minRange, out var maxRange))
				yield break;

			if (!TryGetActiveInstances(world, out var currentInstance, out var activeActors))
				yield break;

			var info = currentInstance.Info as AttackOrderPowerInfo;
			if (info == null)
				yield break;

			foreach (var actor in activeActors)
			{
				if (actor == null || !actor.IsInWorld || actor.World == null)
					continue;

				var occupies = actor.OccupiesSpace;
				if (occupies == null)
					continue;

				var center = occupies.CenterPosition;

				yield return new RangeCircleAnnotationRenderable(
					center,
					minRange,
					0,
					info.CircleColor,
					info.CircleWidth,
					info.CircleBorderColor,
					info.CircleBorderWidth);

				yield return new RangeCircleAnnotationRenderable(
					center,
					maxRange,
					0,
					info.CircleColor,
					info.CircleWidth,
					info.CircleBorderColor,
					info.CircleBorderWidth);
			}
		}

		protected override string GetCursor(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			return IsValidTarget(world, cell) ? cursor : cursorBlocked;
		}
	}
}
