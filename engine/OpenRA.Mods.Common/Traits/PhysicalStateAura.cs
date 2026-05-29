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
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Applies continuous changes to a named PhysicalState of actors within range.")]
	public class PhysicalStateAuraInfo : ConditionalTraitInfo
	{
		[FieldLoader.Require]
		[Desc("Name of the PhysicalState to modify on nearby actors.")]
		public readonly string PhysicalStateName = null;

		[Desc("Amount to add per tick to the PhysicalState. Can be negative to subtract.")]
		public readonly int AmountPerTick = 1;

		[Desc("The range in which to affect actors.")]
		public readonly WDist Range = WDist.FromCells(3);

		[Desc("The maximum vertical range above terrain to affect actors.",
		"Ignored if 0 (actors are affected regardless of vertical distance).")]
		public readonly WDist MaximumVerticalOffset = WDist.Zero;

		[Desc("What player relationships are affected.")]
		public readonly PlayerRelationship ValidRelationships = PlayerRelationship.Enemy;

		[Desc("Whether to affect the actor that has this aura.")]
		public readonly bool AffectsParent = false;

		[Desc("Tick interval for applying the effect.")]
		public readonly int Interval = 25;

		public override object Create(ActorInitializer init) { return new PhysicalStateAura(init.Self, this); }
	}

	public class PhysicalStateAura : ConditionalTrait<PhysicalStateAuraInfo>,
		ITick, INotifyAddedToWorld, INotifyRemovedFromWorld, INotifyOtherProduction
	{
		readonly Actor self;
		readonly HashSet<Actor> affectedActors = new();

		int proximityTrigger;
		int tickCount;
		WPos cachedPosition;
		WDist cachedRange;
		WDist desiredRange;
		WDist cachedVRange;
		WDist desiredVRange;

		public PhysicalStateAura(Actor self, PhysicalStateAuraInfo info)
			: base(info)
		{
			this.self = self;
			cachedRange = WDist.Zero;
			cachedVRange = WDist.Zero;
		}

		void INotifyAddedToWorld.AddedToWorld(Actor self)
		{
			cachedPosition = self.CenterPosition;
			proximityTrigger = self.World.ActorMap.AddProximityTrigger(cachedPosition, cachedRange, cachedVRange, ActorEntered, ActorExited);
		}

		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
		{
			self.World.ActorMap.RemoveProximityTrigger(proximityTrigger);
			affectedActors.Clear();
		}

		protected override void TraitEnabled(Actor self)
		{
			desiredRange = Info.Range;
			desiredVRange = Info.MaximumVerticalOffset;
		}

		protected override void TraitDisabled(Actor self)
		{
			desiredRange = WDist.Zero;
			desiredVRange = WDist.Zero;
			affectedActors.Clear();
		}

		void ITick.Tick(Actor self)
		{
			if (self.CenterPosition != cachedPosition || desiredRange != cachedRange || desiredVRange != cachedVRange)
			{
				cachedPosition = self.CenterPosition;
				cachedRange = desiredRange;
				cachedVRange = desiredVRange;
				self.World.ActorMap.UpdateProximityTrigger(proximityTrigger, cachedPosition, cachedRange, cachedVRange);
			}

			if (IsTraitDisabled || Info.AmountPerTick == 0)
				return;

			if (++tickCount >= Info.Interval)
			{
				tickCount = 0;
				ApplyEffect();
			}
		}

		void ApplyEffect()
		{
			var actorsToRemove = new List<Actor>();

			foreach (var actor in affectedActors)
			{
				if (actor.Disposed || actor.IsDead)
				{
					actorsToRemove.Add(actor);
					continue;
				}

				var physicalState = actor.TraitsImplementing<PhysicalState>()
					.FirstOrDefault(ps => ps.Name == Info.PhysicalStateName);

				physicalState?.ApplyChange(Info.AmountPerTick, self);
			}

			foreach (var actor in actorsToRemove)
				affectedActors.Remove(actor);
		}

		void ActorEntered(Actor a)
		{
			if (a.Disposed || self.Disposed)
				return;

			if (a == self && !Info.AffectsParent)
				return;

			if (affectedActors.Contains(a))
				return;

			var relationship = self.Owner.RelationshipWith(a.Owner);
			if (!Info.ValidRelationships.HasRelationship(relationship))
				return;

			var physicalState = a.TraitsImplementing<PhysicalState>()
				.FirstOrDefault(ps => ps.Name == Info.PhysicalStateName);

			if (physicalState != null)
				affectedActors.Add(a);
		}

		void ActorExited(Actor a)
		{
			affectedActors.Remove(a);
		}

		void INotifyOtherProduction.UnitProducedByOther(Actor self, Actor producer, Actor produced, string productionType, TypeDictionary init)
		{
			if (produced.OccupiesSpace == null)
				return;

			if (IsTraitDisabled)
				return;

			if ((produced.CenterPosition - self.CenterPosition).HorizontalLengthSquared <= Info.Range.LengthSquared)
			{
				if (!Info.ValidRelationships.HasRelationship(self.Owner.RelationshipWith(produced.Owner)))
					return;

				var physicalState = produced.TraitsImplementing<PhysicalState>()
					.FirstOrDefault(ps => ps.Name == Info.PhysicalStateName);

				if (physicalState != null)
					affectedActors.Add(produced);
			}
		}
	}
}
