#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System;
using System.Linq;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.AS.Traits;
using OpenRA.Traits;
using System.Collections.Generic;

namespace OpenRA.Mods.Cameo.Traits
{
	[Desc("Can be slaved to a drone spawner.")]
	public class SlaveMinerSpawnerSlaveInfo : BaseSpawnerSlaveInfo
	{
		[Desc("Aircraft slaves outside of this range from master while moving will be call back")]
		public readonly int MovingCallBackCellDistance = 2;

		[Desc("Slaves will follow master instead of attack while target outside of this range")]
		public readonly WDist AttackCallBackDistance = WDist.FromCells(10);

		[GrantedConditionReference]
		[Desc("The condition to grant to slaves when freed.")]
		public readonly string FreedCondition = null;

		[Desc("Player relationships the attacking player needs to free the slave.")]
		public readonly PlayerRelationship FreeingRelationships = PlayerRelationship.Neutral | PlayerRelationship.Enemy;

		[Desc("Sound to play when the slave is freed.")]
		public readonly string FreedSound = null;

		public override object Create(ActorInitializer init) { return new SlaveMinerSpawnerSlave(this); }
	}

	public class SlaveMinerSpawnerSlave : BaseSpawnerSlave, INotifyHarvestAction, INotifyDockClientMoving, INotifyAddedToWorld
	{
		public IMove[] Moves { get; private set; }
		public IPositionable Positionable { get; private set; }
		public bool IsAircraft;
		public readonly SlaveMinerSpawnerSlaveInfo Info;
		Actor currentActor;
		Actor masterActor;
		public readonly Predicate<Actor> InvalidActor;
		CPos harvestedField;
		IDockHost masterDock;
		DockClientManager dockClient;
		bool freed;

		// Called by the master on a bounded cadence. A free-will slave whose activity queue
		// emptied (a manual order that finished, a cancelled dock trip, ...) would otherwise
		// stand still forever; put it back to work without touching slaves that are busy.
		public void RescueIdle(Actor self, Actor master)
		{
			var harvester = self.TraitOrDefault<Harvester>();
			var action = SlaveIdleRescue.Decide(
				self.IsIdle, self.IsInWorld, self.IsDead, self.Owner == master.Owner,
				freed, harvester != null && !harvester.IsTraitDisabled, harvester != null && harvester.IsFull);

			switch (action)
			{
				case SlaveIdleRescueAction.Harvest:
					self.QueueActivity(new FindAndDeliverResources(self));
					break;

				case SlaveIdleRescueAction.DeliverThenHarvest:
					if (!DeliverToMaster(self))
					{
						// FindAndDeliverResources waits whenever ReservedHost is set, so drop a stale
						// reservation and let it queue its own MoveToDock.
						dockClient.UnreserveHost();
						self.QueueActivity(new FindAndDeliverResources(self));
					}

					break;
			}
		}

		public bool IsMoving(CPos gatherlocation)
		{
			if (IsAircraft)
			{
				if (!InvalidActor(currentActor) && !InvalidActor(masterActor) &&
					(currentActor.Location - gatherlocation).LengthSquared > Info.MovingCallBackCellDistance * Info.MovingCallBackCellDistance)
					return false;

				return true;
			}

			var groundmove = Array.Exists(Moves, m => m.IsTraitEnabled() && (m.CurrentMovementTypes.HasFlag(MovementType.Horizontal) || m.CurrentMovementTypes.HasFlag(MovementType.Vertical)));
			return groundmove;
		}

		public SlaveMinerSpawnerSlave(SlaveMinerSpawnerSlaveInfo info)
			: base(info)
		{
			InvalidActor = a => a == null || a.IsDead || !a.IsInWorld;
			Info = info;
		}

		protected override void Created(Actor self)
		{
			base.Created(self);

			currentActor = self;

			Moves = self.TraitsImplementing<IMove>().ToArray();

			var positionables = self.TraitsImplementing<IPositionable>();
			if (positionables.Count() != 1)
				throw new InvalidOperationException($"Actor {self} has multiple (or no) traits implementing IPositionable.");

			Positionable = positionables.First();

			IsAircraft = self.Info.HasTraitInfo<AircraftInfo>();

			dockClient = self.Trait<DockClientManager>();
		}

		public override void LinkMaster(Actor self, Actor master, BaseSpawnerMaster spawnerMaster)
		{
			base.LinkMaster(self, master, spawnerMaster);
			masterActor = master;
		}
		void INotifyAddedToWorld.AddedToWorld(Actor self)
		{
			UpdateMasterDock(self);
		}

		void UpdateMasterDock(Actor self)
		{
			if (masterActor == null)
			{
				SetMasterDock(null);
				return;
			}
			var dock = dockClient.AvailableDockHosts(masterActor, default, true).ClosestDock(self, dockClient);
			// Cache the preferred dock, but don't reserve it until MoveToDock starts.
			// FindAndDeliverResources treats any existing reservation as an active
			// delivery and waits instead of creating a MoveToDock activity.
			SetMasterDock(dock.HasValue ? dock.Value.Trait : null);
		}

		void SetMasterDock(IDockHost dock)
		{
			if (masterDock != dock && dockClient.ReservedHost == masterDock)
				dockClient.UnreserveHost();

			masterDock = dock;
		}

		void ClearMasterBinding()
		{
			masterActor = null;
			SetMasterDock(null);
		}

		public void UpdateOnTransform(Actor self)
		{
			UpdateMasterDock(self);
			self.QueueActivity(false, new FindAndDeliverResources(self));
		}

		public void Move(Actor self, CPos location)
		{
			// And tell attack bases to stop attacking.
			if (Moves.Length == 0)
				return;

			foreach (var mv in Moves)
				if (mv.IsTraitEnabled())
				{
					if (IsAircraft)
						self.QueueActivity(mv.MoveTo(location, 0));
					else
						self.QueueActivity(mv.MoveTo(location, 2));
					break;
				}
		}

		void INotifyDockClientMoving.MovingToDock(Actor self, Actor hostActor, IDockHost host, bool forceEnter)
		{
			if (DeliverToMaster(self))
				return;
			else if (hostActor == null)
			{
				harvestedField = self.World.Map.CellContaining(self.CenterPosition);
				self.QueueActivity(new FindAndDeliverResources(self, harvestedField));
			}
			else
				self.QueueActivity(new MoveToDock(self, hostActor, host, true));
		}

		void INotifyDockClientMoving.MovementCancelled(Actor self) { Reset(self); }

		void INotifyHarvestAction.MovingToResources(Actor self, CPos targetCell) { Reset(self); }

		void INotifyHarvestAction.MovementCancelled(Actor self) { Reset(self); }

		void INotifyHarvestAction.Harvested(Actor self, string resourceType) { }

		void Reset(Actor self)
		{
			DeliverToMaster(self);
		}

		bool DeliverToMaster(Actor self)
		{
			if (masterActor == null || masterDock == null)
				return false;

			if (SlaveIdleRescue.ShouldAppendDeliveryPair(true, HasQueuedHarvest(self)))
			{
				self.QueueActivity(new MoveToDock(self, masterActor, masterDock, true));
				self.QueueActivity(new FindAndDeliverResources(self));
			}

			return true;
		}

		static bool HasQueuedHarvest(Actor self)
		{
			for (var a = self.CurrentActivity?.NextActivity; a != null; a = a.NextActivity)
				if (a is FindAndDeliverResources)
					return true;

			return false;
		}

		public override void OnMasterKilled(Actor self, Actor attacker, SpawnerSlaveDisposal disposal)
		{
			// Grant MasterDead condition.
			if (!string.IsNullOrEmpty(Info.MasterDeadCondition))
				self.GrantCondition(Info.MasterDeadCondition);

			var killer = attacker ?? self;

			switch (disposal)
			{
				case SpawnerSlaveDisposal.KillSlaves:
					self.Kill(killer, Info.DamageTypes);
					break;
				case SpawnerSlaveDisposal.GiveSlavesToAttacker:
					ClearMasterBinding();
					dockClient.UnreserveHost();
					self.CancelActivity();
					self.ChangeOwner(killer.Owner);
					if (!string.IsNullOrEmpty(Info.FreedCondition) && Info.FreeingRelationships.HasRelationship(killer.Owner.RelationshipWith(self.Owner)))
					{
						freed = true;
						self.GrantCondition(Info.FreedCondition);
						Game.Sound.Play(SoundType.World, Info.FreedSound, self.CenterPosition);
					}
					self.QueueActivity(new FindAndDeliverResources(self));
					break;
				case SpawnerSlaveDisposal.DoNothing:
					ClearMasterBinding();
					dockClient.UnreserveHost();
					self.CancelActivity();
					self.QueueActivity(new FindAndDeliverResources(self));
					break;
				default:
					break;
			}
		}

		public override void OnMasterOwnerChanged(Actor self, OpenRA.Player oldOwner, OpenRA.Player newOwner, SpawnerSlaveDisposal disposal)
		{
			switch (disposal)
			{
				case SpawnerSlaveDisposal.KillSlaves:
					self.Kill(self, Info.DamageTypes);
					break;
				case SpawnerSlaveDisposal.GiveSlavesToAttacker:
					self.ChangeOwner(newOwner);
					UpdateOnTransform(self);
					break;
				case SpawnerSlaveDisposal.DoNothing:
					ClearMasterBinding();
					dockClient.UnreserveHost();
					self.CancelActivity();
					self.QueueActivity(new FindAndDeliverResources(self));
					break;
				default:
					break;
			}
		}

		public override void OnOwnerChanged(Actor self, OpenRA.Player oldOwner, OpenRA.Player newOwner)
		{
			if (Master == null)
			{
				ClearMasterBinding();
				dockClient.UnreserveHost();
				return;
			}

			if (!Master.IsDead)
			{
				// When the refinery changes hands, the spawned slaves change owner too.
				// Keep the binding when they now belong to their still-live master.
				if (Master.Owner != newOwner)
				{
					ClearMasterBinding();
					dockClient.UnreserveHost();
					self.CancelActivity();
					self.QueueActivity(new FindAndDeliverResources(self));
				}

				return;
			}

			// This function got triggered because the master got mind controlled and
			// thus triggered slave.ChangeOwner().
			// In this case, do nothing.
			if (Master.Owner == newOwner)
				return;

			if (Info.AllowOwnerChange)
			{
				ClearMasterBinding();
				dockClient.UnreserveHost();
				return;
			}

			self.Kill(self, Info.DamageTypes);
		}
	}
}
