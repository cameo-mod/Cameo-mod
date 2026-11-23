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
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;
using Util = OpenRA.Mods.Common.Util;

namespace OpenRA.Mods.Cameo.Projectiles
{
	// Ported from Shattered Paradise (OpenRA.Mods.Sp/Projectiles/InstantHitWIthFakeBullets.cs, GPLv3)
	// at the maintainer's request 2026-08-22: "bullets should have that instant hit with tracer thingy
	// from Shattered Paradise ... make sure the instant hit with tracer still allows us to have
	// contrails like we have now with bullet weapons."
	[Desc("HITSCAN. The warhead lands on the FIRST tick; the bullets you see are FAKE — purely",
		"cosmetic sprites that fly out afterwards carrying trails and contrails and cannot change",
		"the outcome. A rifle round crosses its range in milliseconds, so modelling it as a",
		"travelling projectile let fast units outrun bullets and made a burst aimed at a mover land",
		"behind it. `InstantHit` resolves instantly but is documented \"instant, INVISIBLE\"; `Bullet`",
		"is visible but travels. This is the pair of them.",
		"⚠ FakeBulletNumber defaults to 0, which draws NOTHING — set it (usually to Burst) or the",
		"weapon fires invisibly. FakeBulletInaccuracy is cosmetic scatter only and uses",
		"Game.CosmeticRandom, never SharedRandom, so it cannot desync a multiplayer game.")]
	public class InstantHitWithFakeBulletsInfo : IProjectileInfo
	{
		[Desc("The maximum/constant/incremental inaccuracy used in conjunction with the InaccuracyType property.")]
		public readonly WDist Inaccuracy = WDist.Zero;

		[Desc("Controls the way inaccuracy is calculated. Possible values are 'Maximum' - scale from 0 to max with range, 'PerCellIncrement' - scale from 0 with range and 'Absolute' - use set value regardless of range.")]
		public readonly InaccuracyType InaccuracyType = InaccuracyType.Maximum;

		[Desc("Projectile can be blocked.")]
		public readonly bool Blockable = false;

		[Desc("The width of the projectile.")]
		public readonly WDist Width = new(1);

		[Desc("Scan radius for actors with projectile-blocking trait. If set to a negative value (default), it will automatically scale",
			"to the blocker with the largest health shape. Only set custom values if you know what you're doing.")]
		public readonly WDist BlockerScanRadius = new(-1);

		[Desc("Fake bullet number.")]
		public readonly int FakeBulletNumber = 0;

		[Desc("Fake bullet spawn interval. Only useful when FakeBulletNumber > 0")]
		public readonly int FakeBulletSpawnInterval = 3;

		[Desc("Fake bullet speed")]
		public readonly int FakeBulletSpeed = 1024;

		[Desc("Fake bullet inaccuracy, use set value regardless of range")]
		public readonly WDist FakeBulletInaccuracy = WDist.Zero;

		[Desc("Image to display.")]
		public readonly string Image = null;

		[SequenceReference(nameof(Image), allowNullImage: true)]
		[Desc("Loop sequence of Image from this list while this projectile is moving.")]
		public readonly string Sequence = "idle";

		[Desc("The palette used to draw this projectile.")]
		[PaletteReference(nameof(IsPlayerPalette))]
		public readonly string Palette = "effect";

		public readonly bool IsPlayerPalette = false;

		[Desc("Trail animation.")]
		public readonly string TrailImage = null;

		[Desc("Loop sequence of TrailImage from this list while this projectile is moving.")]
		[SequenceReference(nameof(TrailImage), allowNullImage: true)]
		public readonly string TrailSequence = "idle";

		[Desc("Interval in ticks between each spawned Trail animation.")]
		public readonly int TrailInterval = 2;

		[Desc("Palette used to render the trail sequence.")]
		[PaletteReference(nameof(TrailUsePlayerPalette))]
		public readonly string TrailPalette = "effect";

		[Desc("Use the Player Palette to render the trail sequence.")]
		public readonly bool TrailUsePlayerPalette = false;

		[Desc("When set, display a line behind the fake bullet. Length is measured in ticks after appearing.")]
		public readonly int ContrailLength = 0;

		[Desc("Time (in ticks) after which the line should appear. Controls the distance to the actor.")]
		public readonly int ContrailDelay = 1;

		[Desc("Equivalent to sequence ZOffset. Controls Z sorting.")]
		public readonly int ContrailZOffset = 2047;

		[Desc("Thickness of the emitted line at the start of the contrail.")]
		public readonly WDist ContrailStartWidth = new(64);

		[Desc("Thickness of the emitted line at the end of the contrail. Will default to " + nameof(ContrailStartWidth) + " if left undefined")]
		public readonly WDist? ContrailEndWidth = null;

		[Desc("RGB color at the contrail start.")]
		public readonly Color ContrailStartColor = Color.White;

		[Desc("Use player remap color instead of a custom color at the contrail the start.")]
		public readonly bool ContrailStartColorUsePlayerColor = false;

		[Desc("The alpha value [from 0 to 255] of color at the contrail the start.")]
		public readonly int ContrailStartColorAlpha = 255;

		[Desc("RGB color at the contrail end. Will default to " + nameof(ContrailStartColor) + " if left undefined")]
		public readonly Color? ContrailEndColor;

		[Desc("Use player remap color instead of a custom color at the contrail end.")]
		public readonly bool ContrailEndColorUsePlayerColor = false;

		[Desc("The alpha value [from 0 to 255] of color at the contrail end.")]
		public readonly int ContrailEndColorAlpha = 0;

		[Desc("Length of a moving streak drawn behind each fake bullet instead of a sprite and contrail. Zero (default) keeps the old fake bullet rendering. Set ContrailLength: 0 with it.",
			"The ProjectileStreak* keys have the same names and meaning as on Bullet; the streak is render only and never changes the hit.")]
		public readonly WDist ProjectileStreakLength = WDist.Zero;

		[Desc("Maximum amount added to or subtracted from ProjectileStreakLength, picked per shot for the look only (no random generator, never changes the hit).")]
		public readonly WDist ProjectileStreakLengthVariation = WDist.Zero;

		[Desc("Width of the streak at its thickest.")]
		public readonly WDist ProjectileStreakWidth = new(32);

		[Desc("Color of the streak.")]
		public readonly Color ProjectileStreakColor = Color.White;

		[Desc("Width of the streak at its tail. Defaults to ProjectileStreakWidth.")]
		public readonly WDist? ProjectileStreakTailWidth = null;

		[Desc("Width at the tip of the leading taper. Defaults to ProjectileStreakWidth.")]
		public readonly WDist? ProjectileStreakHeadWidth = null;

		[Desc("Length of the leading taper. Zero disables it.")]
		public readonly WDist ProjectileStreakHeadTaperLength = WDist.Zero;

		[Desc("Color at the streak tail. Defaults to ProjectileStreakColor.")]
		public readonly Color? ProjectileStreakTailColor = null;

		[Desc("Alpha [0 to 255] of ProjectileStreakTailColor. Values outside the range are clamped.")]
		public readonly int ProjectileStreakTailColorAlpha = 255;

		[Desc("Equivalent to sequence ZOffset. Controls Z sorting.")]
		public readonly int ProjectileStreakZOffset = 2047;

		public IProjectile Create(ProjectileArgs args) { return new InstantHitWithFakeBullets(this, args); }
	}

	public class InstantHitWithFakeBullets : IProjectile
	{
		readonly ProjectileArgs args;
		readonly InstantHitWithFakeBulletsInfo info;
		readonly FakeBulletWrapper[] fakeBullets;
		readonly World world;
		readonly string animPalette;
		readonly string trailPalette;

		Target target;
		bool notImpacted = true;
		int ticks;
		WPos fakeBulletEndBasePos;
		readonly bool streak;
		readonly ProjectileStreakStyle streakStyle;
		int renderedWorldTick = -1;
		long renderMoveStart;
		int renderMoveElapsed;

		struct FakeBulletWrapper
		{
			public int Time;
			public int OverallTime;
			public ContrailRenderable Contrail;
			public Animation Anim;
			public WPos EndPos;
			public WPos Pos;
			public WPos SourcePos;
			public WAngle Facing;
		}

		public InstantHitWithFakeBullets(InstantHitWithFakeBulletsInfo info, ProjectileArgs args)
		{
			this.args = args;
			this.info = info;
			world = args.SourceActor.World;
			streak = info.ProjectileStreakLength.Length > 0;
			streakStyle = new ProjectileStreakStyle(info.ProjectileStreakWidth, info.ProjectileStreakTailWidth,
				info.ProjectileStreakHeadWidth, info.ProjectileStreakHeadTaperLength, info.ProjectileStreakColor,
				info.ProjectileStreakTailColor, info.ProjectileStreakTailColorAlpha, info.ProjectileStreakZOffset);

			animPalette = info.Palette;
			if (info.IsPlayerPalette)
				animPalette += args.SourceActor.Owner.InternalName;

			trailPalette = info.TrailPalette;
			if (info.TrailUsePlayerPalette)
				trailPalette += args.SourceActor.Owner.InternalName;

			if (info.FakeBulletNumber > 0)
			{
				fakeBullets = new FakeBulletWrapper[info.FakeBulletNumber];
				var startcolor = Color.FromArgb(info.ContrailStartColorAlpha, info.ContrailStartColor);
				var endcolor = Color.FromArgb(info.ContrailEndColorAlpha, info.ContrailEndColor ?? startcolor);
				for (var i = 0; i < fakeBullets.Length; i++)
				{
					var contrail = info.ContrailLength <= 0 ? null : new ContrailRenderable(world, args.SourceActor,
						startcolor, info.ContrailStartColorUsePlayerColor,
						endcolor, info.ContrailEndColor == null ? info.ContrailStartColorUsePlayerColor : info.ContrailEndColorUsePlayerColor,
						info.ContrailStartWidth,
						info.ContrailEndWidth ?? info.ContrailStartWidth,
						info.ContrailLength, info.ContrailDelay, info.ContrailZOffset);

					fakeBullets[i] = new FakeBulletWrapper
					{
						Time = -1,
						OverallTime = 0,
						Contrail = contrail,
						EndPos = WPos.Zero
					};
				}
			}

			if (args.Weapon.TargetActorCenter)
				target = args.GuidedTarget;
			else if (info.Inaccuracy.Length > 0)
			{
				var maxInaccuracyOffset = Util.GetProjectileInaccuracy(info.Inaccuracy.Length, info.InaccuracyType, args);
				var inaccuracyOffset = WVec.FromPDF(args.SourceActor.World.SharedRandom, 2) * maxInaccuracyOffset / 1024;
				target = Target.FromPos(args.PassiveTarget + inaccuracyOffset);
			}
			else
				target = Target.FromPos(args.PassiveTarget);
		}

		public void Tick(World world)
		{
			ticks++;

			if (notImpacted)
			{
				notImpacted = false;

				// If GuidedTarget has become invalid due to getting killed the same tick,
				// we need to set target to args.PassiveTarget to prevent target.CenterPosition below from crashing.
				if (target.Type == TargetType.Invalid)
					target = Target.FromPos(args.PassiveTarget);

				// Check for blocking actors
				if (info.Blockable && BlocksProjectiles.AnyBlockingActorsBetween(world, args.SourceActor.Owner, args.Source, target.CenterPosition, info.Width, out var blockedPos))
					target = Target.FromPos(blockedPos);

				fakeBulletEndBasePos = target.CenterPosition;

				var warheadArgs = new WarheadArgs(args)
				{
					ImpactOrientation = new WRot(WAngle.Zero, Util.GetVerticalAngle(args.Source, target.CenterPosition), args.Facing),
					ImpactPosition = target.CenterPosition,
				};

				args.Weapon.Impact(target, warheadArgs);
			}

			var allFakeBulletDone = true;

			// Fake bullet generate/render position process
			if (fakeBullets != null)
			{
				for (var i = 0; i < fakeBullets.Length; i++)
				{
					// 1. Generate the bullet when time has come
					if (ticks >= i * info.FakeBulletSpawnInterval && fakeBullets[i].OverallTime == 0 && !args.SourceActor.IsDead)
					{
						allFakeBulletDone = false;

						// Fake bullet contains only renderable objects,
						// it won't affect game logic & network sync so we DON'T use SharedRandom here.
						fakeBullets[i].SourcePos = args.CurrentSource();
						var fakeInaccOffset = WVec.FromPDF(Game.CosmeticRandom, 2) * info.FakeBulletInaccuracy.Length / 1024;
						var vec = fakeBulletEndBasePos + fakeInaccOffset - fakeBullets[i].SourcePos;
						var time = Math.Max(vec.Length / info.FakeBulletSpeed, 1);
						fakeBullets[i].Time = time;
						fakeBullets[i].OverallTime = time;
						fakeBullets[i].Pos = fakeBullets[i].SourcePos;
						fakeBullets[i].EndPos = fakeBulletEndBasePos + fakeInaccOffset;
						fakeBullets[i].Facing = vec.Pitch;

						/* Another method to get pitch
						 * Note: OpenRA defines north as -y.
						 * fakeBullets[i].Facing = vec.LengthSquared == 0 ? WAngle.Zero : WAngle.ArcTan(vec.Z, vec.HorizontalLength);
						*/

						if (!string.IsNullOrEmpty(info.Image))
						{
							var facing = fakeBullets[i].Facing;
							fakeBullets[i].Anim = new Animation(world, info.Image, () => facing);
							fakeBullets[i].Anim.PlayRepeating(info.Sequence);
						}
					}

					// 2. Process the exsting bullet when it does not expire.
					if (fakeBullets[i].Time > 0 && fakeBullets[i].OverallTime != 0)
					{
						allFakeBulletDone = false;
						var currentPos = WPos.Lerp(fakeBullets[i].SourcePos, fakeBullets[i].EndPos, fakeBullets[i].OverallTime - fakeBullets[i].Time, fakeBullets[i].OverallTime);
						fakeBullets[i].Pos = currentPos;
						fakeBullets[i].Contrail?.Update(currentPos);
						fakeBullets[i].Anim?.Tick();

						if (!string.IsNullOrEmpty(info.TrailImage) && (info.TrailInterval == 0 || ((fakeBullets[i].OverallTime - fakeBullets[i].Time) % info.TrailInterval == 0)))
						{
							var pos = fakeBullets[i].Pos;
							var facing = fakeBullets[i].Facing;
							world.AddFrameEndTask(w => w.Add(new SpriteEffect(pos, facing, world,
								info.TrailImage, info.TrailSequence, trailPalette)));
						}

						if (--fakeBullets[i].Time <= 0 && info.ContrailLength > 0)
						{
							var contrail = fakeBullets[i].Contrail;
							var pos = fakeBullets[i].EndPos;
							world.AddFrameEndTask(w => w.Add(new ContrailFader(pos, contrail)));
						}
					}
				}

				// 3. Process the exsting bullet when it does not expire.
				if (ticks <= (fakeBullets.Length - 1) * info.FakeBulletSpawnInterval)
					allFakeBulletDone = false;
			}

			if (allFakeBulletDone)
				world.AddFrameEndTask(w => w.Remove(this));
		}

		// Streak mode (ProjectileStreakLength > 0) is render only. Tick, lifetime and the end of the projectile are the legacy fake bullet
		// code, untouched; the streak is derived here from the state that code already keeps (SourcePos, EndPos, OverallTime, ticks).
		// Bullet i is generated on the tick max(1, i * FakeBulletSpawnInterval) (the first tick has ticks == 1), so its age in ticks is
		// ticks - that tick, and it has flown 'age' of its OverallTime steps. It is shown for age 0 .. OverallTime - 1: every tick of its
		// flight including the last one, where the head reaches EndPos. The legacy sprite hides the last step (Time <= 0); the streak does not,
		// so a flight of one step (distance below 2 * FakeBulletSpeed) is still drawn for that one tick.
		// Set ContrailLength: 0 on streak weapons (the ^Tracer_* templates do): the legacy contrail fader would draw the old line.
		IEnumerable<IRenderable> RenderStreaks(WorldRenderer wr)
		{
			var timestep = Math.Max(wr.World.Timestep, 1);
			if (renderedWorldTick != wr.World.WorldTick)
			{
				renderedWorldTick = wr.World.WorldTick;
				renderMoveStart = Game.RunTime;
				renderMoveElapsed = 0;
			}
			else if (!wr.World.Paused)
				renderMoveElapsed = (int)Math.Clamp(Game.RunTime - renderMoveStart, 0L, timestep);

			for (var i = 0; i < fakeBullets.Length; i++)
			{
				var fb = fakeBullets[i];
				if (fb.OverallTime == 0)
					continue;

				var age = ticks - Math.Max(1, i * info.FakeBulletSpawnInterval);
				if (age < 0 || age >= fb.OverallTime)
					continue;

				var path = fb.EndPos - fb.SourcePos;
				var total = path.Length;
				if (total == 0)
					continue;

				// Head: age ticks plus the smooth part of the current tick, over OverallTime ticks (long math, no int overflow).
				var head = fb.SourcePos + ProjectileStreak.Scale(path, (long)age * timestep + renderMoveElapsed, (long)fb.OverallTime * timestep);

				// The length variation is derived from the shot (source, end, slot), so it is stable across frames and draws no random number.
				var length = ProjectileStreak.VariedLength(info.ProjectileStreakLength.Length, info.ProjectileStreakLengthVariation.Length, StreakRoll(i, fb.SourcePos, fb.EndPos));
				var visibleLength = Math.Min(length, (head - fb.SourcePos).Length);
				if (visibleLength <= 0)
					continue;

				var tail = head - ProjectileStreak.Scale(path, visibleLength, total);
				if (wr.World.FogObscures(head) && wr.World.FogObscures(tail))
					continue;

				foreach (var beam in ProjectileStreak.Render(streakStyle, head, tail, visibleLength))
					yield return beam;
			}
		}

		// Stable per-shot value (splitmix64 finaliser over the shot geometry). Wraps on overflow by design.
		static ulong StreakRoll(int slot, WPos source, WPos end)
		{
			unchecked
			{
				var h = Mix(0x9E3779B97F4A7C15UL, slot);
				h = Mix(h, source.X);
				h = Mix(h, source.Y);
				h = Mix(h, source.Z);
				h = Mix(h, end.X);
				h = Mix(h, end.Y);
				return Mix(h, end.Z);
			}
		}

		static ulong Mix(ulong h, int v)
		{
			unchecked
			{
				h += (ulong)(long)v + 0x9E3779B97F4A7C15UL;
				h = (h ^ (h >> 30)) * 0xBF58476D1CE4E5B9UL;
				h = (h ^ (h >> 27)) * 0x94D049BB133111EBUL;
				return h ^ (h >> 31);
			}
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			if (fakeBullets == null)
				yield break;

			if (streak)
			{
				foreach (var r in RenderStreaks(wr))
					yield return r;

				yield break;
			}

			foreach (var c in fakeBullets)
			{
				if (c.Time <= 0)
					yield break;

				if (info.ContrailLength > 0)
					yield return c.Contrail;

				if (c.Anim != null && !world.FogObscures(c.Pos))
				{
					var palette = wr.Palette(animPalette);
					foreach (var r in c.Anim.Render(c.Pos, palette))
						yield return r;
				}
			}
		}
	}
}
