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

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("This unit has access to build queues.")]
	public class ProductionInfo : PausableConditionalTraitInfo
	{
		[FieldLoader.Require]
		[Desc("e.g. Infantry, Vehicles, Aircraft, Buildings")]
		public readonly string[] Produces = [];

		[Desc("When owner is changed, should the Faction be updated to the new owner's faction?")]
		public readonly bool UpdateFactionOnOwnerChange = false;

		[Desc("Produce new actors directly into this actor's cargo instead of spawning them on the map.")]
		public readonly bool ProduceIntoCargo = false;

		[Desc("When producing into cargo, also attempt to load into the cargo of passengers that themselves have cargo.")]
		public readonly bool ProduceIntoCargoOfCargo = false;

		[Desc("Priority value when multiple cargo-producing traits compete. Higher values are preferred.")]
		public readonly int CargoPriority = 0;

		public override object Create(ActorInitializer init) { return new Production(init, this); }
	}

	public class Production : PausableConditionalTrait<ProductionInfo>, INotifyOwnerChanged
	{
		RallyPoint rp;

		public string Faction { get; private set; }

		public Production(ActorInitializer init, ProductionInfo info)
			: base(info)
		{
			Faction = init.GetValue<FactionInit, string>(init.Self.Owner.Faction.InternalName);
		}

		protected override void Created(Actor self)
		{
			rp = self.TraitOrDefault<RallyPoint>();
			base.Created(self);
		}

		public virtual void DoProduction(Actor self, ActorInfo producee, ExitInfo exitinfo, string productionType, TypeDictionary inits)
		{
			var exit = CPos.Zero;
			var exitLocations = new List<CPos>();

			// Clone the initializer dictionary for the new actor
			var td = new TypeDictionary();
			foreach (var init in inits)
				td.Add(init);

			if (exitinfo != null && self.OccupiesSpace != null && producee.HasTraitInfo<IOccupySpaceInfo>())
			{
				exit = self.Location + exitinfo.ExitCell;
				var spawn = self.CenterPosition + exitinfo.SpawnOffset;
				var to = self.World.Map.CenterOfCell(exit);

				WAngle initialFacing;
				if (!exitinfo.Facing.HasValue)
				{
					var delta = to - spawn;
					if (delta.HorizontalLengthSquared == 0)
					{
						var fi = producee.TraitInfoOrDefault<IFacingInfo>();
						initialFacing = fi != null ? fi.GetInitialFacing() : WAngle.Zero;
					}
					else
						initialFacing = delta.Yaw;
				}
				else
					initialFacing = exitinfo.Facing.Value;

				exitLocations = rp != null && rp.Path.Count > 0 ? rp.Path : [exit];

				td.Add(new LocationInit(exit));
				td.Add(new CenterPositionInit(spawn));
				td.Add(new FacingInit(initialFacing));
				if (exitinfo != null)
				{
					td.Add(new CreationActivityDelayInit(exitinfo.ExitDelay));
					td.Add(new RallyPointInit(exitLocations.ToArray()));
				}
			}

			self.World.AddFrameEndTask(w =>
			{
				var newUnit = self.World.CreateActor(producee.Name, td);
				if (!self.IsDead)
					foreach (var t in self.TraitsImplementing<INotifyProduction>())
						t.UnitProduced(self, newUnit, exit);

				var notifyOthers = self.World.ActorsWithTrait<INotifyOtherProduction>();
				foreach (var notify in notifyOthers)
					notify.Trait.UnitProducedByOther(notify.Actor, self, newUnit, productionType, td);
			});
		}

		protected virtual Exit SelectExit(Actor self, ActorInfo producee, string productionType, Func<Exit, bool> p)
		{
			if (rp == null || rp.Path.Count == 0)
				return self.RandomExitOrDefault(self.World, productionType, p);

			return self.NearestExitOrDefault(self.World.Map.CenterOfCell(rp.Path[0]), productionType, p);
		}

		protected Exit SelectExit(Actor self, ActorInfo producee, string productionType)
		{
			return SelectExit(self, producee, productionType, e => CanUseExit(self, producee, e.Info));
		}

		public virtual bool Produce(Actor self, ActorInfo producee, string productionType, TypeDictionary inits, int refundableValue)
		{
			if (IsTraitDisabled || IsTraitPaused)
				return false;

			if (Info.ProduceIntoCargo && TryProduceIntoCargo(self, producee, productionType, inits))
				return true;

			// Pick a spawn/exit point pair
			var exit = SelectExit(self, producee, productionType);
			if (exit != null || self.OccupiesSpace == null || !producee.HasTraitInfo<IOccupySpaceInfo>())
			{
				ProduceActors(self, producee, productionType, inits, exit?.Info);
				return true;
			}

			return false;
		}

		bool TryProduceIntoCargo(Actor self, ActorInfo producee, string productionType, TypeDictionary inits)
		{
			var passengerInfo = producee.TraitInfoOrDefault<PassengerInfo>();
			if (passengerInfo == null)
				return false;

			var candidates = GatherCargoTargets(self, passengerInfo.Weight);
			if (candidates == null)
				return false;

			var td = new TypeDictionary();
			foreach (var init in inits)
				td.Add(init);

			var (transport, cargo) = candidates.Value;
			var newUnit = self.World.CreateActor(false, producee.Name, td);
			cargo.Load(transport, newUnit);

			self.World.AddFrameEndTask(w =>
			{
				if (!self.IsDead)
					foreach (var t in self.TraitsImplementing<INotifyProduction>())
						t.UnitProduced(self, newUnit, transport.Location);

				var notifyOthers = self.World.ActorsWithTrait<INotifyOtherProduction>();
				foreach (var notify in notifyOthers)
					notify.Trait.UnitProducedByOther(notify.Actor, self, newUnit, productionType, td);
			});

			return true;
		}

		(Actor Transport, Cargo Cargo)? GatherCargoTargets(Actor self, int weight)
		{
			var cargos = self.TraitsImplementing<Cargo>().ToArray();
			foreach (var cargo in cargos)
				if (cargo.HasSpace(weight))
					return (self, cargo);

			if (!Info.ProduceIntoCargoOfCargo)
				return null;

			foreach (var cargo in cargos)
				foreach (var passenger in cargo.Passengers)
				{
					if (passenger == null || passenger.IsDead)
						continue;

					var passengerCargo = passenger.TraitOrDefault<Cargo>();
					if (passengerCargo != null && passengerCargo.HasSpace(weight))
						return (passenger, passengerCargo);
				}

			return null;
		}

		public virtual void ProduceActors(Actor self, ActorInfo producee, string productionType, TypeDictionary inits, ExitInfo exit)
		{
			var buildable = BuildableInfo.GetTraitForQueue(producee, productionType);
			if (buildable != null)
			{
				var additionalActors = buildable.AdditionalActors;
				for (var n = 0; n < buildable.BuildAmount; n++)
				{
					DoProduction(self, producee, exit, productionType, inits);
					if (additionalActors.Length != 0)
					{
						foreach (var additionalActor in additionalActors)
							DoProduction(self, self.World.Map.Rules.Actors[additionalActor.ToLowerInvariant()], exit, productionType, inits);
					}
				}
			}
			else
				DoProduction(self, producee, exit, productionType, inits);
		}

		static bool CanUseExit(Actor self, ActorInfo producee, ExitInfo s)
		{
			var mobileInfo = producee.TraitInfoOrDefault<MobileInfo>();

			self.NotifyBlocker(self.Location + s.ExitCell);

			return mobileInfo == null ||
				mobileInfo.CanEnterCell(self.World, self, self.Location + s.ExitCell, ignoreActor: self);
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			if (Info.UpdateFactionOnOwnerChange)
				Faction = self.Owner.Faction.InternalName;
		}
	}

	public class RallyPointInit : ValueActorInit<CPos[]>, ISingleInstanceInit
	{
		public RallyPointInit(CPos[] value)
			: base(value) { }
	}
}
