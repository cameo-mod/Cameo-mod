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
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.World | SystemActors.EditorWorld)]
	[Desc("Renders a screen-space heat-haze distortion effect that displaces the framebuffer.",
		"Vertical-biased (heat rises), not radial. Add to the world actor.",
		"Deliberately separate from GlowRenderer: glow blends light, this displaces pixels.")]
	public class HeatDistortionRendererInfo : TraitInfo
	{
		[Desc("Gaussian falloff radius of each distortion in screen pixels.")]
		public readonly float DistortionRadius = 80f;

		[Desc("Peak pixel displacement at the centre of a distortion.")]
		public readonly float DistortionStrength = 12f;

		public override object Create(ActorInitializer init) { return new HeatDistortionRenderer(this); }
	}

	public sealed class HeatDistortionRenderer : IRenderPostProcessPass, INotifyActorDisposing
	{
		const int MaxDistortionsPerBatch = 16;

		static readonly string[] CentersKeys = Enumerable.Range(0, MaxDistortionsPerBatch).Select(i => $"DistortionCenters[{i}]").ToArray();
		static readonly string[] RadiiKeys = Enumerable.Range(0, MaxDistortionsPerBatch).Select(i => $"DistortionRadii[{i}]").ToArray();
		static readonly string[] StrengthsKeys = Enumerable.Range(0, MaxDistortionsPerBatch).Select(i => $"DistortionStrengths[{i}]").ToArray();

		readonly HeatDistortionRendererInfo info;
		readonly Renderer renderer;
		readonly IShader shader;
		readonly IVertexBuffer<RenderPostProcessPassVertex> buffer;

		readonly List<(WPos Center, float Scale)> pendingDistortions = new();
		readonly List<(WPos Center, float Scale, int FramesRemaining, int TotalFrames, int FadeInFrames)> fadingDistortions = new();

		readonly float[] centers = new float[MaxDistortionsPerBatch * 2];
		readonly float[] radii = new float[MaxDistortionsPerBatch];
		readonly float[] strengths = new float[MaxDistortionsPerBatch];

		// Render-frame counter driving the shimmer animation. Render-only, so its value is irrelevant to sync.
		float time;

		public HeatDistortionRenderer(HeatDistortionRendererInfo info)
		{
			this.info = info;
			renderer = Game.Renderer;
			shader = renderer.CreateShader(new RenderPostProcessPassShaderBindings("heat_distortion"));
			buffer = renderer.CreateVertexBuffer(new RenderPostProcessPassVertex[]
			{
				new(-1, -1), new(1, -1), new(1, 1),
				new(1, 1), new(-1, 1), new(-1, -1)
			}, false);
		}

		public void RegisterDistortion(WPos center, float scale = 1f, int fadeFrames = 0, int fadeInFrames = 0)
		{
			if (fadeFrames > 0)
			{
				fadingDistortions.Add((center, scale, fadeFrames, fadeFrames, fadeInFrames));
				return;
			}

			pendingDistortions.Add((center, scale));
		}

		PostProcessPassType IRenderPostProcessPass.Type => PostProcessPassType.AfterActors;
		bool IRenderPostProcessPass.Enabled => pendingDistortions.Count > 0 || fadingDistortions.Count > 0;

		void IRenderPostProcessPass.Draw(WorldRenderer wr)
		{
			time += 1f / 60f;

			var downscale = renderer.WorldDownscaleFactor;
			var topLeft = wr.Viewport.TopLeft;

			float2 ToFb(WPos pos)
			{
				var screenPx = wr.ScreenPxPosition(pos);
				return new float2(
					(screenPx.X - topLeft.X) * downscale,
					(screenPx.Y - topLeft.Y) * downscale);
			}

			// Collect all distortions for this frame into one flat list so they can be batched together.
			var batch = new List<(WPos Center, float Scale)>(pendingDistortions.Count + fadingDistortions.Count);
			foreach (var d in pendingDistortions)
				batch.Add(d);
			pendingDistortions.Clear();

			for (var i = fadingDistortions.Count - 1; i >= 0; i--)
			{
				var d = fadingDistortions[i];
				var framesPassed = d.TotalFrames - d.FramesRemaining;
				float fadeScale;
				if (d.FadeInFrames > 0 && framesPassed < d.FadeInFrames)
					fadeScale = (float)framesPassed / d.FadeInFrames;
				else
				{
					var fadeOutTotal = d.TotalFrames - d.FadeInFrames;
					fadeScale = fadeOutTotal > 0 ? (float)d.FramesRemaining / fadeOutTotal : 1f;
				}

				batch.Add((d.Center, d.Scale * fadeScale));

				if (d.FramesRemaining <= 1)
					fadingDistortions.RemoveAt(i);
				else
					fadingDistortions[i] = (d.Center, d.Scale, d.FramesRemaining - 1, d.TotalFrames, d.FadeInFrames);
			}

			// Draw distortions in fixed-size batches. Each batch takes one framebuffer snapshot and runs
			// a single shader pass that loops over all sources, accumulating displacement before sampling.
			for (var offset = 0; offset < batch.Count; offset += MaxDistortionsPerBatch)
			{
				var batchSize = Math.Min(MaxDistortionsPerBatch, batch.Count - offset);

				for (var i = 0; i < batchSize; i++)
				{
					var d = batch[offset + i];
					var p = ToFb(d.Center);

					centers[i * 2] = p.X;
					centers[i * 2 + 1] = p.Y;
					radii[i] = info.DistortionRadius * d.Scale;
					strengths[i] = info.DistortionStrength * d.Scale;
				}

				shader.SetTexture("WorldTexture", Game.Renderer.WorldBufferSnapshot());

				// ANGLE/ES rejects glUniformXfv with count > 1 on array uniforms, so set each element individually.
				for (var i = 0; i < batchSize; i++)
				{
					shader.SetVec(CentersKeys[i], centers[i * 2], centers[i * 2 + 1]);
					shader.SetVec(RadiiKeys[i], radii[i]);
					shader.SetVec(StrengthsKeys[i], strengths[i]);
				}

				shader.SetVec("DistortionCount", (float)batchSize);
				shader.SetVec("Time", time);
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
