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
using OpenRA.Graphics;
using OpenRA.Mods.AS.Traits;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	// TODO: remove all the Render*Circle duplication
	sealed class RenderSpectreCircleInfo : ConditionalTraitInfo
	{
		[Desc("Range circle color.")]
		public readonly Color Color = Color.White;

		[Desc("Range circle line width.")]
		public readonly float Width = 1;

		[Desc("Range circle border color.")]
		public readonly Color BorderColor = Color.FromArgb(96, Color.Black);

		[Desc("Range circle border width.")]
		public readonly float BorderWidth = 3;

		public override object Create(ActorInitializer init) { return new RenderSpectreCircle(this); }
	}

	sealed class RenderSpectreCircle : ConditionalTrait<RenderSpectreCircleInfo>, IRenderAnnotationsWhenSelected
	{
		readonly RenderSpectreCircleInfo info;

		public RenderSpectreCircle(RenderSpectreCircleInfo info)
			: base(info)
		{
			this.info = info;
		}

		public IEnumerable<IRenderable> RenderAnnotations(Actor self, WorldRenderer wr)
		{
			if (IsTraitDisabled)
				yield break;

			if (!self.Owner.IsAlliedWith(self.World.RenderPlayer))
				yield break;

			var attackSpectre = self.TraitOrDefault<AttackSpectre>();
			if (attackSpectre != null)
			{
				yield return new RangeCircleAnnotationRenderable(
					attackSpectre.TargetLocation,
					attackSpectre.Info.TargetRadius,
					0,
					info.Color,
					info.Width,
					info.BorderColor,
					info.BorderWidth);
			}
		}

		bool IRenderAnnotationsWhenSelected.SpatiallyPartitionable => false;
	}
}
