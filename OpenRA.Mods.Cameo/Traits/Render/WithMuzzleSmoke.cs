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
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits.Render
{
	[Desc("Spawns a short-lived cosmetic smoke puff at the exact firing barrel position.")]
	public class WithMuzzleSmokeInfo : ConditionalTraitInfo
	{
		[Desc("Only emit smoke for these weapon names. Leave empty to allow every weapon with a muzzle sequence.")]
		public readonly HashSet<string> Weapons = new(StringComparer.OrdinalIgnoreCase);

		[Desc("Sprite image containing the smoke animation.")]
		public readonly string Image = "muzzle_smoke";

		[SequenceReference(nameof(Image))]
		[Desc("Sequence played once for each puff.")]
		public readonly string Sequence = "idle";

		[PaletteReference]
		[Desc("Palette used to render the smoke.")]
		public readonly string Palette = "effect";

		[Desc("Distance beyond the configured muzzle position where the puff begins.")]
		public readonly WDist ForwardOffset = new(48);

		[Desc("Vertical distance the detached puff rises each tick.")]
		public readonly WDist RisePerTick = new(8);

		[Desc("Minimum ticks between puffs from this trait. Zero emits on every matching shot.")]
		public readonly int MinInterval = 0;

		public override object Create(ActorInitializer init) { return new WithMuzzleSmoke(this); }
	}

	public class WithMuzzleSmoke : ConditionalTrait<WithMuzzleSmokeInfo>, INotifyAttack
	{
		int lastPuffTick = int.MinValue;

		public WithMuzzleSmoke(WithMuzzleSmokeInfo info)
			: base(info) { }

		void INotifyAttack.Attacking(Actor self, in Target target, Armament a, Barrel barrel)
		{
			if (IsTraitDisabled || !Game.Settings.Graphics.TankMuzzleFlashes || a == null || barrel == null)
				return;

			if (string.IsNullOrEmpty(a.Info.MuzzleSequence) ||
				(Info.Weapons.Count > 0 && !Info.Weapons.Contains(a.Info.Weapon)))
				return;

			if (Info.MinInterval > 0 && self.World.WorldTick - lastPuffTick < Info.MinInterval)
				return;

			lastPuffTick = self.World.WorldTick;
			var world = self.World;
			var startTick = world.WorldTick;
			var yaw = a.MuzzleOrientation(self, barrel).Yaw;
			var source = self.CenterPosition + a.MuzzleOffset(self, barrel) +
				new WVec(0, -Info.ForwardOffset.Length, 0).Rotate(WRot.FromYaw(yaw));

			world.AddFrameEndTask(w => w.Add(new SpriteEffect(
				() => source + new WVec(0, 0, Math.Max(0, w.WorldTick - startTick) * Info.RisePerTick.Length),
				() => WAngle.Zero, w, Info.Image, Info.Sequence, Info.Palette)));
		}

		void INotifyAttack.PreparingAttack(Actor self, in Target target, Armament a, Barrel barrel) { }
	}
}
