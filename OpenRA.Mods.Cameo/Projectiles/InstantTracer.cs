#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Projectiles
{
	[Desc("Hitscan round: the warhead lands on the FIRST tick, with a short tracer dash drawn",
		"flying to the target purely as decoration (maintainer 2026-08-22: \"instant hit with",
		"bullet animation for tracers ... tracers will be very small and short\").",
		"A real rifle round crosses its range in a few milliseconds, so modelling it as a",
		"travelling projectile means fast units can outrun bullets and a burst that was aimed",
		"at a moving target lands behind it. `InstantHit` already resolves instantly but is",
		"documented as \"instant, INVISIBLE\", and `Bullet` is visible but travels — this is",
		"the pair of them: instant damage, visible streak.",
		"Unlike LaserZap, which draws the WHOLE source-to-target beam at once, the dash here is",
		"TracerLength long and its leading edge sweeps from source to target across Duration",
		"ticks. That is the visual difference between a laser and a tracer round.")]
	public class InstantTracerInfo : IProjectileInfo
	{
		[Desc("The maximum/constant/incremental inaccuracy used in conjunction with InaccuracyType.")]
		public readonly WDist Inaccuracy = WDist.Zero;

		[Desc("Controls the way inaccuracy is calculated. Possible values are 'Maximum' (default),",
			"'PerCellIncrement' and 'Absolute'.")]
		public readonly InaccuracyType InaccuracyType = InaccuracyType.Maximum;

		[Desc("Length of the visible dash. Keep it SHORT — this is a tracer, not a beam.",
			"256 is a quarter of a cell.")]
		public readonly WDist TracerLength = new(256);

		[Desc("The width of the tracer.")]
		public readonly WDist Width = new(16);

		[Desc("Ticks the dash takes to sweep from source to target. Purely cosmetic — the",
			"warhead has already landed on tick 0 — so keep it small or the streak lingers.")]
		public readonly int Duration = 3;

		[Desc("Colour of the tracer.")]
		public readonly Color Color = Color.FromArgb(255, 255, 220, 130);

		[Desc("Draw the tracer in the firing player's colour instead of Color.")]
		public readonly bool UsePlayerColor = false;

		[Desc("The shape of the dash. Accepts Cylindrical or Flat.")]
		public readonly BeamRenderableShape Shape = BeamRenderableShape.Flat;

		[Desc("Equivalent to sequence ZOffset. Controls Z sorting.")]
		public readonly int ZOffset = 0;

		[Desc("Intensity of the glow around the tracer. 0 disables it.")]
		public readonly int GlowIntensity = 0;

		[Desc("Fade the tracer out over its Duration instead of cutting it.")]
		public readonly bool FadeOut = true;

		[Desc("Impact is blocked by actors with BlocksProjectiles between source and target.")]
		public readonly bool Blockable = false;

		[Desc("Optional sprite drawn at the head of the tracer, so an existing bullet animation",
			"(e.g. `50CAL`) is kept rather than replaced by a bare dash. Empty = dash only.")]
		public readonly string Image = null;

		[Desc("Sequence of Image to play at the tracer head.")]
		public readonly string Sequence = "idle";

		[Desc("Palette to render the tracer sprite in.")]
		public readonly string Palette = "effect";

		[Desc("Draw the coloured dash as well as the sprite. False = sprite only.")]
		public readonly bool DrawDash = true;

		[Desc("Image containing the muzzle flash sequence.")]
		public readonly string LaunchEffectImage = null;

		[Desc("Muzzle flash sequence to play at the source.")]
		public readonly string LaunchEffectSequence = null;

		[Desc("Palette to render the muzzle flash in.")]
		public readonly string LaunchEffectPalette = "effect";

		public IProjectile Create(ProjectileArgs args)
		{
			var c = UsePlayerColor ? args.SourceActor.OwnerColor() : Color;
			return new InstantTracer(this, args, c);
		}
	}

	public class InstantTracer : IProjectile, ISync
	{
		readonly ProjectileArgs args;
		readonly InstantTracerInfo info;
		readonly Color color;
		readonly Animation anim;
		int ticks;

		[VerifySync]
		WPos target;

		[VerifySync]
		WPos source;

		public InstantTracer(InstantTracerInfo info, ProjectileArgs args, Color color)
		{
			this.args = args;
			this.info = info;
			this.color = color;
			target = args.PassiveTarget;
			source = args.Source;

			if (!string.IsNullOrEmpty(info.Image))
			{
				anim = new Animation(args.SourceActor.World, info.Image);
				anim.PlayRepeating(info.Sequence);
			}

			if (info.Inaccuracy.Length > 0)
			{
				var maxInaccuracyOffset = Common.Util.GetProjectileInaccuracy(info.Inaccuracy.Length, info.InaccuracyType, args);
				target += WVec.FromPDF(args.SourceActor.World.SharedRandom, 2) * maxInaccuracyOffset / 1024;
			}
		}

		public void Tick(World world)
		{
			if (ticks == 0)
			{
				source = args.CurrentSource();

				if (info.Blockable && BlocksProjectiles.AnyBlockingActorsBetween(
					world, args.SourceActor.Owner, source, target, info.Width, out var blockedPos))
					target = blockedPos;

				if (!string.IsNullOrEmpty(info.LaunchEffectImage) && !string.IsNullOrEmpty(info.LaunchEffectSequence))
					world.AddFrameEndTask(w => w.Add(new SpriteEffect(args.CurrentSource, args.CurrentMuzzleFacing, world,
						info.LaunchEffectImage, info.LaunchEffectSequence, info.LaunchEffectPalette)));

				// The whole point: damage resolves NOW, on the tick the shot is fired. Everything
				// after this is decoration and cannot change the outcome.
				var warheadArgs = new WarheadArgs(args)
				{
					ImpactOrientation = new WRot(WAngle.Zero, Common.Util.GetVerticalAngle(source, target), args.CurrentMuzzleFacing()),
					ImpactPosition = target,
				};

				args.Weapon.Impact(Target.FromPos(target), warheadArgs);
			}

			anim?.Tick();

			if (++ticks >= info.Duration)
				world.AddFrameEndTask(w => w.Remove(this));
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			if (wr.World.FogObscures(target) && wr.World.FogObscures(source))
				yield break;

			if (ticks >= info.Duration)
				yield break;

			var path = target - source;

			// Leading edge sweeps source -> target across Duration; the dash is the TracerLength
			// behind it, clipped at the muzzle so it grows out of the barrel rather than
			// appearing detached in mid-air on the first tick.
			var lead = source + path * (ticks + 1) / info.Duration;
			var travelled = (lead - source).Length;
			var tailLength = travelled < info.TracerLength.Length ? travelled : info.TracerLength.Length;
			var dash = path.Length > 0 ? path * tailLength / path.Length : WVec.Zero;
			var tail = lead - dash;

			var rc = info.FadeOut
				? Color.FromArgb((info.Duration - ticks) * color.A / info.Duration, color)
				: color;

			if (info.DrawDash)
				yield return new BeamRenderable(tail, info.ZOffset, dash, info.Shape, info.Width, rc, info.GlowIntensity);

			if (anim != null)
				foreach (var r in anim.Render(lead, wr.Palette(info.Palette)))
					yield return r;
		}
	}
}
