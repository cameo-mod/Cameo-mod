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
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.World | SystemActors.EditorWorld)]
	[Desc("Renders a screen-space additive glow effect for beams and impacts. Add to the world actor.")]
	public class GlowRendererInfo : TraitInfo
	{
		[Desc("Gaussian falloff radius of the glow in screen pixels.")]
		public readonly float GlowRadius = 60f;

		[Desc("Peak additive intensity of the glow (0-1).")]
		public readonly float GlowIntensity = 0.7f;

		public override object Create(ActorInitializer init) { return new GlowRenderer(this); }
	}

	public sealed class GlowRenderer : IRenderPostProcessPass, INotifyActorDisposing
	{
		const int MaxBeamsPerBatch = 16;

		static readonly string[] BeamStartsKeys = Enumerable.Range(0, MaxBeamsPerBatch).Select(i => $"BeamStarts[{i}]").ToArray();
		static readonly string[] BeamEndsKeys = Enumerable.Range(0, MaxBeamsPerBatch).Select(i => $"BeamEnds[{i}]").ToArray();
		static readonly string[] GlowColorsKeys = Enumerable.Range(0, MaxBeamsPerBatch).Select(i => $"GlowColors[{i}]").ToArray();
		static readonly string[] GlowIntensitiesKeys = Enumerable.Range(0, MaxBeamsPerBatch).Select(i => $"GlowIntensities[{i}]").ToArray();
		static readonly string[] GlowRadiiKeys = Enumerable.Range(0, MaxBeamsPerBatch).Select(i => $"GlowRadii[{i}]").ToArray();

		readonly GlowRendererInfo info;
		readonly Renderer renderer;
		readonly IShader shader;
		readonly IVertexBuffer<RenderPostProcessPassVertex> buffer;

		readonly List<(WPos Source, WPos Target, Color Color, float Scale)> pendingGlows = new();
		readonly List<(WPos Source, WPos Target, Color Color, float Scale, int FramesRemaining, int TotalFrames, int FadeInFrames)> fadingGlows = new();
		readonly Dictionary<WPos, int> glowsPerSource = new();

		readonly float[] beamStarts = new float[MaxBeamsPerBatch * 2];
		readonly float[] beamEnds = new float[MaxBeamsPerBatch * 2];
		readonly float[] glowColors = new float[MaxBeamsPerBatch * 3];
		readonly float[] glowIntensities = new float[MaxBeamsPerBatch];
		readonly float[] glowRadii = new float[MaxBeamsPerBatch];

		public GlowRenderer(GlowRendererInfo info)
		{
			this.info = info;
			renderer = Game.Renderer;
			shader = renderer.CreateShader(new RenderPostProcessPassShaderBindings("glow"));
			buffer = renderer.CreateVertexBuffer(new RenderPostProcessPassVertex[]
			{
				new(-1, -1), new(1, -1), new(1, 1),
				new(1, 1), new(-1, 1), new(-1, -1)
			}, false);
		}

		public void RegisterGlow(WPos source, WPos target, Color color, float scale = 1f, int fadeFrames = 0, int fadeInFrames = 0)
		{
			if (fadeFrames > 0)
			{
				fadingGlows.Add((source, target, color, scale, fadeFrames, fadeFrames, fadeInFrames));
				return;
			}

			// Cap at 2 simultaneous beam glows per source position per frame.
			// Rapid-fire weapons that produce more than 2 beams per tick skip the extras.
			glowsPerSource.TryGetValue(source, out var count);
			if (count >= 2)
				return;

			glowsPerSource[source] = count + 1;
			pendingGlows.Add((source, target, color, scale));
		}

		PostProcessPassType IRenderPostProcessPass.Type => PostProcessPassType.AfterActors;
		bool IRenderPostProcessPass.Enabled => pendingGlows.Count > 0 || fadingGlows.Count > 0;

		void IRenderPostProcessPass.Draw(WorldRenderer wr)
		{
			var downscale = renderer.WorldDownscaleFactor;
			var topLeft = wr.Viewport.TopLeft;

			float2 ToFb(WPos pos)
			{
				var screenPx = wr.ScreenPxPosition(pos);
				return new float2(
					(screenPx.X - topLeft.X) * downscale,
					(screenPx.Y - topLeft.Y) * downscale);
			}

			// Collect all glows for this frame into one flat list so they can be batched together.
			var batch = new List<(WPos Source, WPos Target, Color Color, float Scale)>(pendingGlows.Count + fadingGlows.Count);
			foreach (var g in pendingGlows)
				batch.Add(g);
			pendingGlows.Clear();
			glowsPerSource.Clear();

			for (var i = fadingGlows.Count - 1; i >= 0; i--)
			{
				var glow = fadingGlows[i];
				var framesPassed = glow.TotalFrames - glow.FramesRemaining;
				float fadeScale;
				if (glow.FadeInFrames > 0 && framesPassed < glow.FadeInFrames)
					fadeScale = (float)framesPassed / glow.FadeInFrames;
				else
				{
					var fadeOutTotal = glow.TotalFrames - glow.FadeInFrames;
					fadeScale = fadeOutTotal > 0 ? (float)glow.FramesRemaining / fadeOutTotal : 1f;
				}

				batch.Add((glow.Source, glow.Target, glow.Color, glow.Scale * fadeScale));

				if (glow.FramesRemaining <= 1)
					fadingGlows.RemoveAt(i);
				else
					fadingGlows[i] = (glow.Source, glow.Target, glow.Color, glow.Scale, glow.FramesRemaining - 1, glow.TotalFrames, glow.FadeInFrames);
			}

			// Draw glows in fixed-size batches. Each batch takes one framebuffer snapshot and runs
			// a single shader pass that loops over all beams, applying screen-blend math iteratively.
			for (var offset = 0; offset < batch.Count; offset += MaxBeamsPerBatch)
			{
				var batchSize = Math.Min(MaxBeamsPerBatch, batch.Count - offset);

				for (var i = 0; i < batchSize; i++)
				{
					var g = batch[offset + i];
					var p1 = ToFb(g.Source);
					var p2 = ToFb(g.Target);

					beamStarts[i * 2] = p1.X;
					beamStarts[i * 2 + 1] = p1.Y;
					beamEnds[i * 2] = p2.X;
					beamEnds[i * 2 + 1] = p2.Y;
					glowColors[i * 3] = g.Color.R / 255f;
					glowColors[i * 3 + 1] = g.Color.G / 255f;
					glowColors[i * 3 + 2] = g.Color.B / 255f;
					glowIntensities[i] = info.GlowIntensity * g.Scale * (g.Color.A / 255f);
					glowRadii[i] = info.GlowRadius * g.Scale;
				}

				shader.SetTexture("WorldTexture", Game.Renderer.WorldBufferSnapshot());

				// ANGLE/ES rejects glUniformXfv with count > 1 on array uniforms, so set each element individually.
				for (var i = 0; i < batchSize; i++)
				{
					shader.SetVec(BeamStartsKeys[i], beamStarts[i * 2], beamStarts[i * 2 + 1]);
					shader.SetVec(BeamEndsKeys[i], beamEnds[i * 2], beamEnds[i * 2 + 1]);
					shader.SetVec(GlowColorsKeys[i], glowColors[i * 3], glowColors[i * 3 + 1], glowColors[i * 3 + 2]);
					shader.SetVec(GlowIntensitiesKeys[i], glowIntensities[i]);
					shader.SetVec(GlowRadiiKeys[i], glowRadii[i]);
				}

				shader.SetVec("BeamCount", (float)batchSize);
				shader.PrepareRender();
				renderer.DrawBatch(buffer, shader, 0, 6, PrimitiveType.TriangleList);
			}
		}

		void INotifyActorDisposing.Disposing(Actor self)
		{
			buffer.Dispose();
		}
	}
}
