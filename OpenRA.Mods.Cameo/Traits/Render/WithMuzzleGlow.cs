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
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits.Render
{
	[Desc("Registers a short-lived GlowRenderer muzzle flash when a weapon fires: a warm plume cone that is",
		"brightest at the barrel and narrows forward, plus an optional hot core, giving tank cannons a",
		"directional muzzle-flash glow. Respects the \"Weapon Glow Effects\" setting.")]
	public class WithMuzzleGlowInfo : ConditionalTraitInfo
	{
		[Desc("Only flash for these armament names. Leave empty to flash for every armament on the actor.")]
		public readonly HashSet<string> Armaments = new();

		[Desc("Color of the warm plume cone.")]
		public readonly Color Color = Color.FromArgb(230, 255, 138, 60);

		[Desc("Length of the plume cone from the muzzle along the firing direction.")]
		public readonly WDist Length = new(768);

		[Desc("Plume glow radius scale at the muzzle (bright wide end).")]
		public readonly float StartScale = 0.9f;

		[Desc("Plume glow radius scale at the cone tip (narrow dissipating end).")]
		public readonly float EndScale = 0.3f;

		[Desc("Plume brightness multiplier, independent of glow radius.")]
		public readonly float Intensity = 1.6f;

		[Desc("Extra brightness ramped toward the cone tip. Keep at 0 for a muzzle flash (a far light",
			"pool reads as a searchlight, not a flash).")]
		public readonly float EndpointBoost = 0f;

		[Desc("Render frames the plume takes to fade out.")]
		public readonly int FadeFrames = 5;

		[Desc("Render frames the plume takes to fade in (0 = instant pop).")]
		public readonly int FadeInFrames = 0;

		[Desc("Emit a small hot core point-glow at the muzzle for a white-hot center. One extra fading glow per shot.")]
		public readonly bool EnableCore = true;

		[Desc("Color of the hot core.")]
		public readonly Color CoreColor = Color.FromArgb(255, 255, 246, 210);

		[Desc("Radius scale of the hot core point-glow.")]
		public readonly float CoreScale = 0.45f;

		[Desc("Brightness multiplier of the hot core.")]
		public readonly float CoreIntensity = 2.2f;

		[Desc("Render frames the hot core takes to fade out (shorter than the plume fakes an exponential decay).")]
		public readonly int CoreFadeFrames = 3;

		[Desc("Brighten the tank sprite (and ground) under a soft circle at the muzzle, like a flash of light",
			"hitting it. Strongest at the center, falling off to the edge; lifts shadows without washing to white.")]
		public readonly bool EnableFlashLight = true;

		[Desc("Strength of the sprite-brightening flash (radial gamma lift). 0 disables.")]
		public readonly float FlashBrightness = 1.1f;

		[Desc("Radius scale of the sprite-brightening circle at the muzzle.")]
		public readonly float FlashRadiusScale = 0.8f;

		[Desc("Render frames the sprite-brightening flash takes to fade out.")]
		public readonly int FlashFadeFrames = 4;

		[Desc("Per-shot random variation of plume length and overall brightness, as a percentage (0 = identical every shot).")]
		public readonly int JitterPercent = 15;

		[Desc("Minimum ticks between flashes from this trait (0 = flash on every shot). Use to tame very rapid-fire weapons.")]
		public readonly int MinInterval = 0;

		public override object Create(ActorInitializer init) { return new WithMuzzleGlow(this); }
	}

	public class WithMuzzleGlow : ConditionalTrait<WithMuzzleGlowInfo>, INotifyAttack
	{
		int lastFlashTick = int.MinValue;

		public WithMuzzleGlow(WithMuzzleGlowInfo info)
			: base(info) { }

		void INotifyAttack.Attacking(Actor self, in Target target, Armament a, Barrel barrel)
		{
			if (IsTraitDisabled || !Game.Settings.Graphics.LaserGlow)
				return;

			if (barrel == null || (Info.Armaments.Count > 0 && !Info.Armaments.Contains(a.Info.Name)))
				return;

			// Only flash for armaments that define a muzzle sequence — the same convention WithMuzzleOverlay
			// uses to decide a gun has a muzzle effect. This skips target painters / designator lasers and
			// other non-gun armaments (which set no MuzzleSequence) without any per-actor configuration.
			if (string.IsNullOrEmpty(a.Info.MuzzleSequence))
				return;

			if (Info.MinInterval > 0 && self.World.WorldTick - lastFlashTick < Info.MinInterval)
				return;

			lastFlashTick = self.World.WorldTick;

			var glowRenderer = self.World.WorldActor.TraitOrDefault<GlowRenderer>();
			if (glowRenderer == null)
				return;

			// Cosmetic-only jitter — the glow is render-only and never synced, so Game.CosmeticRandom is safe.
			var lengthJitter = 1f;
			var brightnessJitter = 1f;
			if (Info.JitterPercent > 0)
			{
				var j = Info.JitterPercent;
				lengthJitter = 1f + Game.CosmeticRandom.Next(-j, j + 1) / 100f;
				brightnessJitter = 1f + Game.CosmeticRandom.Next(-j, j + 1) / 100f;
			}

			var source = self.CenterPosition + a.MuzzleOffset(self, barrel);
			var yaw = a.MuzzleOrientation(self, barrel).Yaw;

			// Yaw 0 faces north (-Y); project the plume forward out of the barrel.
			var length = (int)(Info.Length.Length * lengthJitter);
			var coneTip = source + new WVec(0, -length, 0).Rotate(WRot.FromYaw(yaw));

			// Plume: bright/wide at the muzzle (StartScale), narrowing forward (EndScale), no far pool.
			glowRenderer.RegisterGlow(source, coneTip, Info.Color, Info.StartScale,
				fadeFrames: Info.FadeFrames, fadeInFrames: Info.FadeInFrames,
				intensity: Info.Intensity * brightnessJitter, scaleEnd: Info.EndScale, endpointBoost: Info.EndpointBoost);

			// Hot core: a radial point-glow at the muzzle (source == target) for a white-hot center.
			if (Info.EnableCore)
				glowRenderer.RegisterGlow(source, source, Info.CoreColor, Info.CoreScale,
					fadeFrames: Info.CoreFadeFrames, intensity: Info.CoreIntensity * brightnessJitter);

			// Flash light: brighten the sprite/ground under a soft circle at the muzzle. No added color
			// (transparent), only the radial gamma lift via selfBrighten.
			if (Info.EnableFlashLight && Info.FlashBrightness > 0f)
				glowRenderer.RegisterGlow(source, source, Color.Transparent, Info.FlashRadiusScale,
					fadeFrames: Info.FlashFadeFrames, selfBrighten: Info.FlashBrightness * brightnessJitter);
		}

		void INotifyAttack.PreparingAttack(Actor self, in Target target, Armament a, Barrel barrel) { }
	}
}
