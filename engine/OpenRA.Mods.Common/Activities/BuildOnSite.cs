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
using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	// Activity to move to a location and construct a building there.
	public class BuildOnSite : Activity
	{
		readonly World world;
		readonly Target centerBuildingTarget;
		readonly CPos centerTarget;
		readonly Order order;
		readonly string faction;
		readonly BuildingInfo buildingInfo;
		readonly ActorInfo buildingActor;
		readonly WDist minRange;
		readonly IMove move;
		readonly IMoveInfo moveInfo;
		readonly PlaceBuildingInfo placeBuildingInfo;
		readonly PlayerResources playerResources;
		readonly DeveloperMode developerMode;
		readonly ProductionQueue queue;
		readonly ProductionItem item;

		public BuildOnSite(World world, Actor self, Order order, string faction, BuildingInfo buildingInfo, ProductionQueue queue, ProductionItem item)
		{
			this.buildingInfo = buildingInfo;
			this.world = world;
			this.order = order;
			this.faction = faction;
			centerBuildingTarget = order.Target;
			centerTarget = world.Map.CellContaining(centerBuildingTarget.CenterPosition);
			minRange = buildingInfo.BuilderRange;
			buildingActor = world.Map.Rules.Actors.FirstOrDefault(x => x.Key == order.TargetString).Value;
			move = self.Trait<IMove>();
			moveInfo = self.Info.TraitInfo<IMoveInfo>();
			placeBuildingInfo = self.Owner.PlayerActor.Info.TraitInfo<PlaceBuildingInfo>();
			playerResources = self.Owner.PlayerActor.Trait<PlayerResources>();
			developerMode = self.Owner.PlayerActor.Trait<DeveloperMode>();
			this.queue = queue;
			this.item = item;
		}

		public override bool Tick(Actor self)
		{
			if (IsCanceling || self.IsDead)
				return true;

			// Move towards the target cell
			if (!centerBuildingTarget.IsInRange(self.CenterPosition, minRange))
			{
				QueueChild(move.MoveWithinRange(centerBuildingTarget, minRange, targetLineColor: moveInfo.GetTargetLineColor()));
				return false;
			}

			if (!world.CanPlaceBuilding(centerTarget, buildingActor, buildingInfo, self))
			{

				// Game.Debug("Attempting to clear site");
				// Try clear the area
				foreach (var ord in ClearBlockersOrders(self))
					world.IssueOrder(ord);
				// Game.Debug("Issued 1 order to clear site");
				Game.Sound.PlayNotification(world.Map.Rules, self.Owner, "Speech", placeBuildingInfo.CannotPlaceNotification, faction);
				TextNotificationsManager.AddTransientLine(self.Owner, placeBuildingInfo.CannotPlaceTextNotification);

				return true;
			}

			self.World.AddFrameEndTask(w =>
			{

				// if (!order.Queued)
				// self.CancelActivity();
				var canQueue = CanQueue(self, out var notification, out var textNotification);

				if (!canQueue)
				{
					Game.Sound.PlayNotification(world.Map.Rules, self.Owner, "Speech", notification, world.LocalPlayer.Faction.InternalName);
					TextNotificationsManager.AddTransientLine(self.Owner, textNotification);
					self.CancelActivity();
				}
				else
				{
					playerResources.TakeCash(item.TotalCost);

					var building = w.CreateActor(true, order.TargetString, new TypeDictionary
					{
						new LocationInit(centerTarget),
						new OwnerInit(order.Player),
						new FactionInit(faction),
						new PlaceBuildingInit()
					});

					foreach (var s in buildingInfo.BuildSounds)
						Game.Sound.PlayToPlayer(SoundType.World, order.Player, s, building.CenterPosition);

					Game.Sound.PlayNotification(world.Map.Rules, self.Owner, "Speech", buildingInfo.PlacedAudio, faction);
					TextNotificationsManager.AddTransientLine(self.Owner, buildingInfo.PlacedTextNotification);
				}
			});

			if (buildingInfo.RemoveBuilder)
			{
				self.QueueActivity(new RemoveSelf());
			}

			if (self != null)
				foreach (var nbp in self.TraitsImplementing<INotifyBuildingPlaced>())
					nbp.BuildingPlaced(self);

			return true;
		}

		// Copied from PlaceBuildingOrderGenerator, triplicated in BuildOnSite and BuilderUnitBuildingOrderGenerator
		IEnumerable<Order> ClearBlockersOrders(Actor ownerActor)
		{
			return AIUtils.ClearBlockersOrders(buildingInfo.Tiles(centerTarget).ToList(), ownerActor.Owner);
		}

		// duplicated from ProductionQueue
		public bool CanQueue(Actor self, out string notificationAudio, out string notificationText)
		{
			notificationAudio = queue.Info.BlockedAudio;
			notificationText = queue.Info.BlockedTextNotification;

			var bi = BuildableInfo.GetTraitForQueue(buildingActor, queue.Info.Type);
			if (bi == null)
				return false;

			var buildableNames = queue.BuildableItems().Select(b => b.Name).ToHashSet();

			if (!developerMode.AllTech)
			{
				if (item.TotalCost > playerResources.GetCashAndResources())
				{
					notificationAudio = playerResources.Info.InsufficientFundsNotification;
					notificationText = playerResources.Info.InsufficientFundsTextNotification;

					return false;
				}

				if (!buildableNames.Contains(item.Item))
				{
					notificationAudio = queue.Info.BlockedAudio;
					notificationText = queue.Info.BlockedTextNotification;

					return false;
				}

				if (bi.BuildLimit > 0)
				{
					var owned = self.Owner.World.ActorsHavingTrait<Buildable>()
						.Count(a => a.Info.Name == buildingActor.Name && a.Owner == self.Owner);
					if (owned >= bi.BuildLimit)
						return false;
				}
			}

			// notificationAudio = buildingInfo.PlacedAudio;
			// notificationText = buildingInfo.PlacedTextNotification;
			return true;
		}
	}
}
