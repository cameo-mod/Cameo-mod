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
using OpenRA.Effects;
using OpenRA.Graphics;
using OpenRA.Primitives;

namespace OpenRA.Mods.Common.Effects
{
	public class PulsatingSpriteEffect : IEffect, ISpatiallyPartitionable
	{
		enum PulseStage { Grow, Hold, Shrink, Done }

		readonly World world;
		readonly string palette;
		readonly Animation anim;
		readonly Func<WPos> posFunc;
		readonly Func<WAngle> facingFunc;
		readonly bool visibleThroughFog;
		readonly string sequence;
		readonly float startScale;
		readonly float peakScale;
		readonly float endScale;
		readonly int growTicks;
		readonly int holdTicks;
		readonly int shrinkTicks;
		readonly bool loop;

		WPos pos;
		int delay;
		bool initialized;
		float currentScale;
		PulseStage stage = PulseStage.Grow;
		int stageTick;

		public PulsatingSpriteEffect(WPos pos, World world, string image, string sequence, string palette,
			float startScale, float peakScale, float endScale,
			int growTicks, int holdTicks, int shrinkTicks, bool loop,
			bool visibleThroughFog = false, int delay = 0)
			: this(() => pos, () => WAngle.Zero, world, image, sequence, palette,
				startScale, peakScale, endScale, growTicks, holdTicks, shrinkTicks, loop, visibleThroughFog, delay)
		{
		}

		public PulsatingSpriteEffect(Func<WPos> posFunc, World world, string image, string sequence, string palette,
			float startScale, float peakScale, float endScale,
			int growTicks, int holdTicks, int shrinkTicks, bool loop,
			bool visibleThroughFog = false, int delay = 0)
			: this(posFunc, () => WAngle.Zero, world, image, sequence, palette,
				startScale, peakScale, endScale, growTicks, holdTicks, shrinkTicks, loop, visibleThroughFog, delay)
		{
		}

		public PulsatingSpriteEffect(Func<WPos> posFunc, Func<WAngle> facingFunc, World world, string image, string sequence, string palette,
			float startScale, float peakScale, float endScale,
			int growTicks, int holdTicks, int shrinkTicks, bool loop,
			bool visibleThroughFog = false, int delay = 0)
		{
			this.world = world;
			this.posFunc = posFunc;
			this.facingFunc = facingFunc ?? (() => WAngle.Zero);
			this.palette = palette;
			this.sequence = sequence;
			this.visibleThroughFog = visibleThroughFog;
			this.delay = delay;
			this.startScale = startScale;
			this.peakScale = peakScale;
			this.endScale = endScale;
			this.growTicks = growTicks;
			this.holdTicks = holdTicks;
			this.shrinkTicks = shrinkTicks;
			this.loop = loop;

			pos = posFunc();
			anim = new Animation(world, image, this.facingFunc);
			currentScale = startScale;
		}

		public void Tick(World world)
		{
			if (delay-- > 0)
				return;

			if (!initialized)
			{
				anim.PlayThen(sequence, () => world.AddFrameEndTask(w => { w.Remove(this); w.ScreenMap.Remove(this); }));
				world.ScreenMap.Add(this, pos, anim.Image);
				initialized = true;
			}
			else
			{
				anim.Tick();
				UpdateScale();

				pos = posFunc();
				world.ScreenMap.Update(this, pos, anim.Image);
			}
		}

		void UpdateScale()
		{
			switch (stage)
			{
				case PulseStage.Grow:
					if (growTicks <= 0)
					{
						currentScale = peakScale;
						AdvanceStage(PulseStage.Hold);
						return;
					}

					currentScale = Lerp(startScale, peakScale, stageTick, growTicks);
					stageTick++;
					if (stageTick >= growTicks)
						AdvanceStage(PulseStage.Hold);

					break;
				case PulseStage.Hold:
					currentScale = peakScale;
					if (holdTicks <= 0)
					{
						AdvanceStage(PulseStage.Shrink);
						return;
					}

					stageTick++;
					if (stageTick >= holdTicks)
						AdvanceStage(PulseStage.Shrink);

					break;
				case PulseStage.Shrink:
					if (shrinkTicks <= 0)
					{
						currentScale = endScale;
						CompleteCycle();
						return;
					}

					currentScale = Lerp(peakScale, endScale, stageTick, shrinkTicks);
					stageTick++;
					if (stageTick >= shrinkTicks)
						CompleteCycle();

					break;
				case PulseStage.Done:
					currentScale = endScale;
					break;
			}
		}

		static float Lerp(float from, float to, int tick, int duration)
		{
			if (duration <= 0)
				return to;

			var progress = Math.Min(1f, tick / (float)duration);
			return from + (to - from) * progress;
		}

		void AdvanceStage(PulseStage next)
		{
			stage = next;
			stageTick = 0;
		}

		void CompleteCycle()
		{
			if (loop)
				AdvanceStage(PulseStage.Grow);
			else
				stage = PulseStage.Done;
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			if (!initialized || (!visibleThroughFog && world.FogObscures(pos)))
				return SpriteRenderable.None;

			var sequence = anim.CurrentSequence;
			if (sequence == null)
				return SpriteRenderable.None;

			var facing = facingFunc();
			var tintModifiers = sequence.IgnoreWorldTint ? TintModifiers.IgnoreWorldTint : TintModifiers.None;
			var alpha = sequence.GetAlpha(anim.CurrentFrame);
			var (image, rotation) = sequence.GetSpriteWithRotation(anim.CurrentFrame, facing);
			var scale = sequence.Scale * currentScale;

			var imageRenderable = new SpriteRenderable(
				image, pos, WVec.Zero, sequence.ZOffset, wr.Palette(palette),
				scale, alpha, float3.Ones, tintModifiers, anim.IsDecoration, rotation);

			var shadow = sequence.GetShadow(anim.CurrentFrame, facing);
			if (shadow != null)
			{
				var height = world.Map.DistanceAboveTerrain(pos).Length;

				var shadowRenderable = new SpriteRenderable(
					shadow, pos, -new WVec(0, 0, height), sequence.ShadowZOffset + height, wr.Palette(palette),
					scale, 1f, float3.Ones, tintModifiers, true, rotation);

				return new IRenderable[] { shadowRenderable, imageRenderable };
			}

			return new IRenderable[] { imageRenderable };
		}
	}
}
