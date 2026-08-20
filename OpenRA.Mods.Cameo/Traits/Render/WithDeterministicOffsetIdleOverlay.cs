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
using OpenRA.Graphics;
using OpenRA.Mods.Common;
using OpenRA.Mods.Cameo.Traits;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits.Render
{
	[Desc("Renders a decorative animation offset by this actor's DeterministicCellOffset.")]
	public class WithDeterministicOffsetIdleOverlayInfo : PausableConditionalTraitInfo, IRenderActorPreviewSpritesInfo,
		Requires<RenderSpritesInfo>, Requires<BodyOrientationInfo>, Requires<DeterministicCellOffsetInfo>
	{
		[Desc("Image used for this decoration. Defaults to the actor's type.")]
		public readonly string Image = null;

		[SequenceReference(nameof(Image), allowNullImage: true)]
		[Desc("Animation to play when the actor is created.")]
		public readonly string StartSequence = null;

		[SequenceReference(nameof(Image), allowNullImage: true)]
		[Desc("Sequence name to use.")]
		public readonly string Sequence = "idle-overlay";

		[Desc("Additional position relative to body, applied on top of DeterministicCellOffset.")]
		public readonly WVec Offset = WVec.Zero;

		[PaletteReference(nameof(IsPlayerPalette))]
		[Desc("Custom palette name.")]
		public readonly string Palette = null;

		[Desc("Custom palette is a player palette BaseName.")]
		public readonly bool IsPlayerPalette = false;

		public readonly bool IsDecoration = false;

		public override object Create(ActorInitializer init) { return new WithDeterministicOffsetIdleOverlay(init.Self, this); }

		public IEnumerable<IActorPreview> RenderPreviewSprites(ActorPreviewInitializer init, string image, int facings, PaletteReference p)
		{
			if (!EnabledByDefault)
				yield break;

			if (Palette != null)
				p = init.WorldRenderer.Palette(IsPlayerPalette ? Palette + init.Get<OwnerInit>().InternalName : Palette);

			Func<WAngle> facing;
			var dynamicFacingInit = init.GetOrDefault<DynamicFacingInit>();
			if (dynamicFacingInit != null)
				facing = dynamicFacingInit.Value;
			else
			{
				var f = init.GetValue<FacingInit, WAngle>(WAngle.Zero);
				facing = () => f;
			}

			var anim = new Animation(init.World, Image ?? image, facing) { IsDecoration = IsDecoration };
			anim.PlayRepeating(RenderSprites.NormalizeSequence(anim, init.GetDamageState(), Sequence));

			var body = init.Actor.TraitInfo<BodyOrientationInfo>();
			WRot Orientation() => body.QuantizeOrientation(WRot.FromYaw(facing()), facings);
			WVec OffsetFunc() => body.LocalToWorld(Offset.Rotate(Orientation()));
			int ZOffset()
			{
				var tmpOffset = OffsetFunc();
				return tmpOffset.Y + tmpOffset.Z + 1;
			}

			yield return new SpriteActorPreview(anim, OffsetFunc, ZOffset, p);
		}
	}

	public class WithDeterministicOffsetIdleOverlay : PausableConditionalTrait<WithDeterministicOffsetIdleOverlayInfo>,
		INotifyDamageStateChanged
	{
		readonly Animation overlay;

		public WithDeterministicOffsetIdleOverlay(Actor self, WithDeterministicOffsetIdleOverlayInfo info)
			: base(info)
		{
			var rs = self.Trait<RenderSprites>();
			var body = self.Trait<BodyOrientation>();
			var facing = self.TraitOrDefault<IFacing>();
			var cellOffset = self.Trait<DeterministicCellOffset>();

			var image = info.Image ?? rs.GetImage(self);
			overlay = new Animation(self.World, image,
				facing == null ? () => WAngle.Zero : (body == null ? () => facing.Facing : () => body.QuantizeFacing(facing.Facing)),
				() => IsTraitPaused)
			{
				IsDecoration = info.IsDecoration
			};

			if (info.StartSequence != null)
				overlay.PlayThen(RenderSprites.NormalizeSequence(overlay, self.GetDamageState(), info.StartSequence),
					() => overlay.PlayRepeating(RenderSprites.NormalizeSequence(overlay, self.GetDamageState(), info.Sequence)));
			else
				overlay.PlayRepeating(RenderSprites.NormalizeSequence(overlay, self.GetDamageState(), info.Sequence));

			var anim = new AnimationWithOffset(overlay,
				() => cellOffset.Offset + body.LocalToWorld(info.Offset.Rotate(body.QuantizeOrientation(self.Orientation))),
				() => IsTraitDisabled,
				p => RenderUtils.ZOffsetFromCenter(self, p, 1));

			rs.Add(anim, info.Palette, info.IsPlayerPalette);
		}

		void INotifyDamageStateChanged.DamageStateChanged(Actor self, AttackInfo e)
		{
			overlay.ReplaceAnim(RenderSprites.NormalizeSequence(overlay, e.DamageState, overlay.CurrentSequence.Name));
		}
	}
}
