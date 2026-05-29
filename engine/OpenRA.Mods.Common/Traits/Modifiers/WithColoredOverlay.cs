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
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Display a colored overlay when a timed condition is active.")]
	public class WithColoredOverlayInfo : ConditionalTraitInfo
	{
		[Desc("Color to overlay.")]
		public readonly Color Color = Color.FromArgb(128, 128, 0, 0);

		[Desc("Skip applying the color overlay to voxel renderables.")]
		public readonly bool SkipVoxels = false;

		[Desc("Override alpha for voxel renderables. Use -1 to use the same alpha as sprites.")]
		public readonly int VoxelAlpha = 30;

		public override object Create(ActorInitializer init) { return new WithColoredOverlay(this); }
	}

	public class WithColoredOverlay : ConditionalTrait<WithColoredOverlayInfo>, IRenderModifier
	{
		readonly float3 tint;
		readonly float alpha;

		public WithColoredOverlay(WithColoredOverlayInfo info)
			: base(info)
		{
			tint = new float3(info.Color.R, info.Color.G, info.Color.B) / 255f;
			alpha = info.Color.A / 255f;
		}

		IEnumerable<IRenderable> IRenderModifier.ModifyRender(Actor self, WorldRenderer wr, IEnumerable<IRenderable> r)
		{
			if (IsTraitDisabled)
				return r;

			return ModifiedRender(r);
		}

		IEnumerable<IRenderable> ModifiedRender(IEnumerable<IRenderable> r)
		{
			foreach (var a in r)
			{
					var isVoxel = a.GetType().Name == "ModelRenderable";

				if (!a.IsDecoration && a is IModifyableRenderable ma)
				{
					if (Info.SkipVoxels && isVoxel)
					{
						yield return a;
						continue;
					}

					if (isVoxel && Info.VoxelAlpha >= 0)
					{
						// For voxels: use OverlayTint mode — adds tint colour on top of model without
						// replacing colours, and draws shadow untinted (handled in ModelRenderable)
						// Scale tint RGB by VoxelAlpha so the additive strength is controlled by VoxelAlpha
						var voxelStrength = Info.VoxelAlpha / 255f;
						var scaledTint = new float3(tint.X * voxelStrength, tint.Y * voxelStrength, tint.Z * voxelStrength);
						// Pass alpha=2f as sentinel — shader sees vTint.a > 1.0 and uses additive mode
						yield return ma.WithTint(scaledTint, TintModifiers.OverlayTint).WithAlpha(2f);
					}
					else
					{
						// For sprites: yield original + tinted overlay copy
						yield return a;
						yield return ma.WithTint(tint, ma.TintModifiers | TintModifiers.ReplaceColor).WithAlpha(alpha);
					}
				}
				else
				{
					yield return a;
				}
			}
		}

		IEnumerable<Rectangle> IRenderModifier.ModifyScreenBounds(Actor self, WorldRenderer wr, IEnumerable<Rectangle> bounds)
		{
			return bounds;
		}
	}
}
