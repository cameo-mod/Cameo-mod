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
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Mods.Cameo.Graphics;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Projectiles
{
	[Desc("Instant-hit electric bolt drawn as a procedural plasma arc (mixed sharp/soft turns,",
		"a hot core, additive glow and occasional branches). A more realistic alternative to TeslaZap.")]
	public class LightningZapInfo : IProjectileInfo
	{
		[Desc("Number of fractal subdivision passes. The channel starts as a straight line and each pass",
			"splits every segment at its midpoint, so the segment count is 2^Generations. More = finer",
			"self-similar detail (and more vertices). Clamped to 1..8.")]
		public readonly int Generations = 6;

		[Desc("How quickly the wiggle shrinks each subdivision pass (0..1). The first pass displaces by",
			"`" + nameof(Amplitude) + "`, the next by Amplitude*Roughness, and so on. Higher = jaggier",
			"at small scales, lower = smoother.")]
		public readonly float Roughness = 0.55f;

		[Desc("Size of the largest (first-pass) wiggle, as a fixed world distance, so the bolt reads at a",
			"consistent size whether it reaches a close or a distant target. Larger = wider swings.")]
		public readonly WDist Amplitude = new(640);

		[Desc("Number of branching offshoots.")]
		public readonly int Branches = 2;

		[Desc("Length of a branching offshoot, as a fixed world distance.")]
		public readonly WDist BranchLength = new(1024);

		[Desc("Colour of the hot core.")]
		public readonly Color CoreColor = Color.FromArgb(255, 255, 255);

		[Desc("Colour of the surrounding glow.")]
		public readonly Color GlowColor = Color.FromArgb(110, 170, 255);

		[Desc("Width of the bright core.")]
		public readonly WDist CoreWidth = new(36);

		[Desc("Width of the glow halo around the core.")]
		public readonly WDist GlowWidth = new(90);

		[Desc("Additive alpha of the glow pass (0-255). Higher = brighter bolt.")]
		public readonly int GlowAlpha = 185;

		[Desc("Radius of the glowing plasma ball drawn at the firing point and impact point.",
			"Set to 0 to disable the endpoint nodes.")]
		public readonly WDist NodeRadius = new(192);

		[Desc("Number of short jagged spark filaments (\"hairs\") radiating from each endpoint node.")]
		public readonly int NodeHairs = 7;

		[Desc("Length of each endpoint spark filament, as a fixed world distance.")]
		public readonly WDist NodeHairLength = new(384);

		[Desc("How long (in ticks) to draw the bolt. ~40ms per tick. The geometry re-randomises every",
			"render frame, so the bolt still flickers within this window.")]
		public readonly int Duration = 3;

		[Desc("How long (in ticks) until applying damage. Can't be longer than `" + nameof(Duration) + "`.")]
		public readonly int DamageDuration = 1;

		[Desc("Follow the targeted actor when it moves.")]
		public readonly bool TrackTarget = true;

		[Desc("Controls Z sorting. Defaults high so the bolt draws on top of buildings/units it",
			"crosses (the world render sorts by Pos.Y + Pos.Z + ZOffset) instead of being occluded.")]
		public readonly int ZOffset = 8192;

		[Desc("Radius scale of the soft screen-space gaussian bloom that follows the bolt (chained",
			"GlowRenderer segments, only with the Weapon Glow Effects setting). 0 disables it.")]
		public readonly float GlowScale = 1f;

		[Desc("Brightness multiplier for the screen-space gaussian bloom. Kept modest because the bloom",
			"is built from several overlapping segments along the path that accumulate.")]
		public readonly float GlowIntensity = 0.8f;

		public IProjectile Create(ProjectileArgs args) { return new LightningZap(this, args); }
	}

	public class LightningZap : IProjectile, ISync
	{
		readonly ProjectileArgs args;
		readonly LightningZapInfo info;
		int ticksUntilRemove;
		int damageDuration;

		[VerifySync]
		WPos target;

		public LightningZap(LightningZapInfo info, ProjectileArgs args)
		{
			this.args = args;
			this.info = info;
			ticksUntilRemove = info.Duration;
			damageDuration = info.DamageDuration > info.Duration ? info.Duration : info.DamageDuration;
			target = args.PassiveTarget;
		}

		public void Tick(World world)
		{
			if (ticksUntilRemove-- <= 0)
				world.AddFrameEndTask(w => w.Remove(this));

			if (info.TrackTarget && args.GuidedTarget.IsValidFor(args.SourceActor))
			{
				var newTarget = args.Weapon.TargetActorCenter
					? args.GuidedTarget.CenterPosition
					: args.GuidedTarget.Positions.ClosestToIgnoringPath(args.Source);

				// Guard against a degenerate origin: an empty Positions set resolves to WPos.Zero
				// (MinByOrDefault), which would streak the bolt from the muzzle to the map corner
				// and span the whole screen. Keep the last good target instead.
				if (newTarget != WPos.Zero)
					target = newTarget;
			}

			if (damageDuration-- > 0)
				args.Weapon.Impact(Target.FromPos(target), new WarheadArgs(args));
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			var length = target - args.Source;

			// Sanity guard: never draw a bolt longer than the weapon could plausibly reach.
			// A degenerate/bogus target (e.g. a tracked actor resolving to an off-map position)
			// would otherwise streak the arc clear across the screen. The weapon's own range,
			// doubled to allow for muzzle offset, is a generous upper bound for any real shot.
			var maxLength = args.Weapon.Range.Length * 2;
			if (maxLength > 0 && length.Length > maxLength)
				yield break;

			yield return new LightningRenderable(args.Source, info.ZOffset, length,
				info.Generations, info.Roughness, info.Amplitude,
				info.Branches, info.BranchLength, info.NodeRadius, info.NodeHairs, info.NodeHairLength,
				info.CoreColor, info.GlowColor, info.CoreWidth,
				info.GlowWidth, info.GlowAlpha, info.GlowScale, info.GlowIntensity);
		}
	}
}
