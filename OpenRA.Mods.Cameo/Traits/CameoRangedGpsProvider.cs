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
using OpenRA.Mods.AS.Traits;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits
{
	/// <summary>
	/// Cameo shadow provider: identical registration with the engine
	/// RangedGpsWatcher (so Granted / GrantedAllies accounting is unchanged) but
	/// its proximity trigger populates CameoRangedGpsDot.Providers — the shadow
	/// dot whose render gate counts ALLIED providers in range (RADAR-ALLY).
	/// Subclassed (not copied) so watcher bookkeeping, owner-change handling and
	/// the ConditionalTrait lifecycle stay engine-owned.
	/// </summary>
	[Desc("Cameo RangedGpsProvider: feeds CameoRangedGpsDot; watcher accounting unchanged.")]
	public class CameoRangedGpsProviderInfo : RangedGpsProviderInfo
	{
		public override object Create(ActorInitializer init) { return new CameoRangedGpsProvider(init.Self, this); }
	}

	public class CameoRangedGpsProvider : RangedGpsProvider, ITick
	{
		readonly Actor self;
		readonly List<Actor> actorsInRange = [];
		int proximityTrigger;
		WPos prevPosition;

		public CameoRangedGpsProvider(Actor self, CameoRangedGpsProviderInfo info)
			: base(self, info)
		{
			this.self = self;
		}

		// RADAR-A: the provider-proximity channel for the bot contacts module.
		// The list contents are populated by the synced proximity trigger, so
		// reading them in bot code is as deterministic as the render effect's
		// per-actor Providers read — same data, other direction.
		internal Actor Self => self;
		internal IReadOnlyList<Actor> ActorsInRange => actorsInRange;

		void ActorEntered(Actor other)
		{
			var dot = other.TraitOrDefault<CameoRangedGpsDot>();
			if (dot != null)
			{
				actorsInRange.Add(other);
				dot.Providers.Add(self);
			}
		}

		void ActorLeft(Actor other)
		{
			if (other.IsDead)
			{
				actorsInRange.Remove(other);
				return;
			}

			var dot = other.TraitOrDefault<CameoRangedGpsDot>();
			if (dot != null)
			{
				actorsInRange.Remove(other);
				dot.Providers.Remove(self);
			}
		}

		protected override void TraitEnabled(Actor self)
		{
			// Register with the shared watcher exactly like the engine provider
			// (Granted/GrantedAllies accounting is engine-owned and unchanged),
			// but track OUR OWN proximity trigger for cameo dots — base's private
			// trigger is never created, so the ITick re-implementation below can
			// move ours without touching base state.
			Watcher.ActivateGps(this, self.Owner);
			proximityTrigger = self.World.ActorMap.AddProximityTrigger(self.CenterPosition, Info.Range, WDist.Zero, ActorEntered, ActorLeft);
		}

		protected override void TraitDisabled(Actor self)
		{
			Watcher.DeactivateGps(this, self.Owner);
			self.World.ActorMap.RemoveProximityTrigger(proximityTrigger);
			foreach (var a in actorsInRange)
				if (!a.IsDead)
					a.Trait<CameoRangedGpsDot>().Providers.Remove(self);

			actorsInRange.Clear();
		}

		// Re-implements the interface (base's explicit ITick impl is replaced for
		// this trait): only the cameo trigger needs position tracking.
		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled || !self.IsInWorld || self.CenterPosition == prevPosition)
				return;

			self.World.ActorMap.UpdateProximityTrigger(proximityTrigger, self.CenterPosition, Info.Range, WDist.Zero);
			prevPosition = self.CenterPosition;
		}
	}
}
