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
		[Desc("Upper cap on the number of turns along the main channel (the actual count scales with",
			"reach via " + nameof(SegmentLength) + " so the jaggedness looks the same at any range).")]
		public readonly int Segments = 12;

		[Desc("World distance between successive turns. The turn count is derived from the bolt's",
			"reach divided by this, so a close and a distant bolt have the same kink density.",
			"Larger = fewer, broader swings.")]
		public readonly WDist SegmentLength = new(512);

		[Desc("Perpendicular swing of the channel as a fixed world distance, so the bolt reads at a",
			"consistent size whether it reaches a close or a distant target. Larger = wider arcs.")]
		public readonly WDist Amplitude = new(256);

		[Desc("Probability (0..1) that a turn is a soft rounded bend rather than a sharp corner.")]
		public readonly float Softness = 0.3f;

		[Desc("How rounded a soft turn is (0..1).")]
		public readonly float RoundFraction = 0.42f;

		[Desc("Number of branching offshoots.")]
		public readonly int Branches = 2;

		[Desc("Length of a branching offshoot, as a fixed world distance.")]
		public readonly WDist BranchLength = new(1024);

		[Desc("Colour of the hot core.")]
		public readonly Color CoreColor = Color.FromArgb(255, 255, 255);

		[Desc("Colour of the surrounding glow.")]
		public readonly Color GlowColor = Color.FromArgb(110, 170, 255);

		[Desc("Width of the bright core.")]
		public readonly WDist CoreWidth = new(40);

		[Desc("Width of the glow halo around the core.")]
		public readonly WDist GlowWidth = new(160);

		[Desc("Additive alpha of the glow pass (0-255). Higher = brighter bolt.")]
		public readonly int GlowAlpha = 105;

		[Desc("Radius of the glowing plasma ball drawn at the firing point and impact point.",
			"Set to 0 to disable the endpoint nodes.")]
		public readonly WDist NodeRadius = new(192);

		[Desc("Number of short jagged spark filaments (\"hairs\") radiating from each endpoint node.")]
		public readonly int NodeHairs = 7;

		[Desc("Length of each endpoint spark filament, as a fixed world distance.")]
		public readonly WDist NodeHairLength = new(384);

		[Desc("How long (in ticks) to draw the bolt. ~40ms per tick, so 2 = an ~80ms electric crack.",
			"The geometry re-randomises every render frame, so the bolt still flickers within this window.")]
		public readonly int Duration = 2;

		[Desc("How long (in ticks) until applying damage. Can't be longer than `" + nameof(Duration) + "`.")]
		public readonly int DamageDuration = 1;

		[Desc("Follow the targeted actor when it moves.")]
		public readonly bool TrackTarget = true;

		[Desc("Controls Z sorting. Defaults high so the bolt draws on top of buildings/units it",
			"crosses (the world render sorts by Pos.Y + Pos.Z + ZOffset) instead of being occluded.")]
		public readonly int ZOffset = 8192;

		[Desc("Scale of the screen-space glow halo (only with the Weapon Glow Effects setting). 0 disables it.")]
		public readonly float GlowScale = 1f;

		[Desc("Brightness multiplier for the screen-space glow halo.")]
		public readonly float GlowIntensity = 2.1f;

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
				info.Segments, info.SegmentLength, info.Amplitude, info.Softness, info.RoundFraction,
				info.Branches, info.BranchLength, info.NodeRadius, info.NodeHairs, info.NodeHairLength,
				info.CoreColor, info.GlowColor, info.CoreWidth,
				info.GlowWidth, info.GlowAlpha, info.GlowScale, info.GlowIntensity);
		}
	}
}
