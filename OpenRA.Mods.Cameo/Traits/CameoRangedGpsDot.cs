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
using OpenRA.Mods.Cameo.Effects;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits
{
	/// <summary>
	/// Cameo shadow of Mods.AS RangedGpsDot (engine types are internal-sealed).
	/// Identical behaviour except the render gate also counts providers owned by
	/// an ALLY of the viewing player (RADAR-ALLY: allied radars share their GPS
	/// discs, matching the classic GpsWatcher ally semantics). The Providers list
	/// is populated by CameoRangedGpsProvider proximity triggers — never read by
	/// synced code, consumed only by the per-client render effect.
	/// </summary>
	[Desc("Cameo RangedGpsDot: dot visible to own AND allied ranged GPS providers.")]
	public sealed class CameoRangedGpsDotInfo : ConditionalTraitInfo
	{
		[Desc("Sprite collection for symbols.")]
		public readonly string Image = "gpsdot";

		[SequenceReference(nameof(Image))]
		[Desc("Sprite used for this actor.")]
		public readonly string Sequence = "idle";

		[PaletteReference(true)]
		public readonly string IndicatorPalettePrefix = "player";

		public readonly bool VisibleInShroud = true;

		public override object Create(ActorInitializer init) { return new CameoRangedGpsDot(this); }
	}

	public sealed class CameoRangedGpsDot : ConditionalTrait<CameoRangedGpsDotInfo>, INotifyAddedToWorld, INotifyRemovedFromWorld
	{
		CameoRangedGpsDotEffect effect;
		public readonly List<Actor> Providers = [];

		public CameoRangedGpsDot(CameoRangedGpsDotInfo info)
			: base(info) { }

		protected override void Created(Actor self)
		{
			effect = new CameoRangedGpsDotEffect(self, this);

			base.Created(self);
		}

		void INotifyAddedToWorld.AddedToWorld(Actor self)
		{
			self.World.AddFrameEndTask(w => w.Add(effect));
		}

		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
		{
			self.World.AddFrameEndTask(w => w.Remove(effect));
		}
	}
}
