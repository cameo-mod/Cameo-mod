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
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Common.Traits;
using OpenRA.Orders;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits
{
	[Desc("A support power that transfers cash to an ally by targeting one of their buildings. Attach to the Construction Yard.")]
	public class TransferCashSupportPowerInfo : SupportPowerInfo
	{
		[Desc("Amount of cash to transfer to the targeted ally.")]
		public readonly int Amount = 2000;

		[Desc("Minimum cash the transferring player must keep after the transfer.")]
		public readonly int MinimumReserve = 500;

		public override object Create(ActorInitializer init) { return new TransferCashSupportPower(init.Self, this); }
	}

	public class TransferCashSupportPower : SupportPower
	{
		readonly TransferCashSupportPowerInfo transferInfo;

		public TransferCashSupportPower(Actor self, TransferCashSupportPowerInfo info)
			: base(self, info)
		{
			transferInfo = info;
		}

		public override void SelectTarget(Actor self, string order, SupportPowerManager manager)
		{
			self.World.OrderGenerator = new TransferCashOrderGenerator(order, manager, transferInfo);
		}

		public override void Activate(Actor self, Order order, SupportPowerManager manager)
		{
			if (order.Target.Type != TargetType.Actor)
				return;

			var targetActor = order.Target.Actor;
			if (targetActor == null || targetActor.IsDead)
				return;

			var targetOwner = targetActor.Owner;
			if (targetOwner == self.Owner || !self.Owner.IsAlliedWith(targetOwner))
				return;

			var playerResources = self.Owner.PlayerActor.Trait<PlayerResources>();
			var totalNeeded = transferInfo.Amount + transferInfo.MinimumReserve;
			if (playerResources.GetCashAndResources() < totalNeeded)
				return;

			var allyResources = targetOwner.PlayerActor.Trait<PlayerResources>();
			playerResources.TakeCash(transferInfo.Amount);
			allyResources.GiveCash(transferInfo.Amount);

			// Notify all allies
			var senderName = self.Owner.PlayerName;
			var receiverName = targetOwner.PlayerName;
			var msg = $"{senderName} transferred ${transferInfo.Amount} to {receiverName}.";
			foreach (var p in self.Owner.World.Players)
			{
				if (!p.NonCombatant && (p == self.Owner || self.Owner.IsAlliedWith(p)))
					TextNotificationsManager.AddTransientLine(p, msg);
			}

			base.Activate(self, order, manager);
		}
	}

	sealed class TransferCashOrderGenerator : OrderGenerator
	{
		protected override MouseActionType ActionType => MouseActionType.SupportPower;

		readonly string orderKey;
		readonly SupportPowerManager manager;
		readonly TransferCashSupportPowerInfo info;
		readonly OpenRA.Player localPlayer;

		public TransferCashOrderGenerator(string order, SupportPowerManager manager, TransferCashSupportPowerInfo info)
			: base(manager.Self.World)
		{
			this.orderKey = order;
			this.manager = manager;
			this.info = info;
			localPlayer = manager.Self.World.LocalPlayer;
		}

		protected override IEnumerable<Order> OrderInner(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			var underCursor = world.ScreenMap.ActorsAtMouse(mi)
				.Select(a => a.Actor)
				.FirstOrDefault(a => a.Owner != null && !a.IsDead
					&& a.Owner != localPlayer
					&& localPlayer.IsAlliedWith(a.Owner));

			if (underCursor == null)
				yield break;

			// Don't cancel — allows shift-click style multi-transfer
			yield return new Order(orderKey, manager.Self, Target.FromActor(underCursor), false)
				{ SuppressVisualFeedback = true };
		}

		protected override void Tick(World world)
		{
			if (!manager.Powers.TryGetValue(orderKey, out var p) || !p.Active || !p.Ready)
				world.CancelInputMode();
		}

		protected override IEnumerable<IRenderable> Render(WorldRenderer wr, World world) { yield break; }
		protected override IEnumerable<IRenderable> RenderAboveShroud(WorldRenderer wr, World world) { yield break; }
		protected override IEnumerable<IRenderable> RenderAnnotations(WorldRenderer wr, World world) { yield break; }

		protected override string GetCursor(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			var underCursor = world.ScreenMap.ActorsAtMouse(mi)
				.Select(a => a.Actor)
				.FirstOrDefault(a => a.Owner != null && !a.IsDead
					&& a.Owner != localPlayer
					&& localPlayer.IsAlliedWith(a.Owner));

			return underCursor != null ? info.Cursor : info.BlockedCursor;
		}
	}
}
