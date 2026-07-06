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
		// Must stay equal to MAX_BEAMS in postprocess_glow.frag (a mismatch silently drops the extra beams —
		// SetVec on an out-of-range uniform name no-ops). Each beam costs ~13 fragment-uniform vectors, so 20
		// => ~261. That is over the GLES2 guaranteed minimum (224) but well within the renderers OpenRA
		// targets (ANGLE/D3D11 ~1024, desktop GL far more). Keep <= 17 if a raw GLES2 target is ever added.
		const int MaxBeamsPerBatch = 20;

		// Per batch the draw quad is shrunk to the beams' screen bounding box, so fragments outside the lit
		// region are never shaded (the pass no longer runs full-screen). Each beam expands the box by the
		// distance at which its exp(-(d/r)^e) falloff — scaled by the beam's peak brightness — falls below
		// ~1/255, i.e. r * ln(255*peak)^(1/e). Exact per edge exponent: the default e>=2 gives a tight
		// ~2-radius margin (cheaper than a fixed pad), while soft exponents widen the box so the clipped edge
		// stays invisible. Floored so a faint glow still gets a small margin; capped so a pathologically soft
		// exponent cannot grow the box back to full-screen.
		const float PadRadiiFloor = 2f;
		const float PadRadiiCap = 12f;

		// Hard upper bound on the number of queued (pending + fading) glows. Draw is the only place
		// these lists are drained, and it runs only on the render tick. If rendering stops (window
		// minimized) or merely falls behind (replay running faster than it can draw), the simulation
		// keeps registering glows with nothing to drain them. Capping the lists means the worst case is
		// a handful of extra shader batches in one frame instead of an unbounded flush that freezes the game.
		const int MaxActiveEffects = 64;

		// Fade timing follows logical game ticks, not render frames, so a glow lasts the same wall-clock
		// duration regardless of framerate (and freezes while the game is paused).
		// The fadeFrames/fadeInFrames values in YAML were tuned as render frames at ReferenceFps; convert
		// them to ticks (at normal-speed sim rate) so the original look is preserved at that framerate.
		const float ReferenceFps = 60f;
		const float TicksPerSecond = 1000f / 40f; // normal game speed = 40ms timestep = 25 ticks/sec
		const float FramesToTicks = TicksPerSecond / ReferenceFps;

		static readonly string[] BeamStartsKeys = Enumerable.Range(0, MaxBeamsPerBatch).Select(i => $"BeamStarts[{i}]").ToArray();
		static readonly string[] BeamEndsKeys = Enumerable.Range(0, MaxBeamsPerBatch).Select(i => $"BeamEnds[{i}]").ToArray();
		static readonly string[] GlowColorsKeys = Enumerable.Range(0, MaxBeamsPerBatch).Select(i => $"GlowColors[{i}]").ToArray();
		static readonly string[] GlowIntensitiesKeys = Enumerable.Range(0, MaxBeamsPerBatch).Select(i => $"GlowIntensities[{i}]").ToArray();
		static readonly string[] GlowRadiiKeys = Enumerable.Range(0, MaxBeamsPerBatch).Select(i => $"GlowRadii[{i}]").ToArray();
		static readonly string[] GlowRadiiEndKeys = Enumerable.Range(0, MaxBeamsPerBatch).Select(i => $"GlowRadiiEnd[{i}]").ToArray();
		static readonly string[] EndpointBoostsKeys = Enumerable.Range(0, MaxBeamsPerBatch).Select(i => $"EndpointBoosts[{i}]").ToArray();
		static readonly string[] SelfBrightensKeys = Enumerable.Range(0, MaxBeamsPerBatch).Select(i => $"SelfBrightens[{i}]").ToArray();
		static readonly string[] GlowFadeEndsKeys = Enumerable.Range(0, MaxBeamsPerBatch).Select(i => $"GlowFadeEnds[{i}]").ToArray();
		static readonly string[] EdgeExponentStartsKeys = Enumerable.Range(0, MaxBeamsPerBatch).Select(i => $"EdgeExponentStarts[{i}]").ToArray();
		static readonly string[] EdgeExponentEndsKeys = Enumerable.Range(0, MaxBeamsPerBatch).Select(i => $"EdgeExponentEnds[{i}]").ToArray();
		static readonly string[] EndpointSquashesKeys = Enumerable.Range(0, MaxBeamsPerBatch).Select(i => $"EndpointSquashes[{i}]").ToArray();
		static readonly string[] PoolRadiiKeys = Enumerable.Range(0, MaxBeamsPerBatch).Select(i => $"PoolRadii[{i}]").ToArray();

		readonly GlowRendererInfo info;
		readonly Renderer renderer;
		readonly IShader shader;
		readonly IVertexBuffer<RenderPostProcessPassVertex> buffer;

		readonly List<(WPos Source, WPos Target, Color Color, float Scale, float ScaleEnd, float Intensity, float EndpointBoost, float SelfBrighten, float FadeEnd, float EdgeExponentStart, float EdgeExponentEnd, float EndpointSquash, float PoolScale)> pendingGlows = new();
		readonly List<(WPos Source, WPos Target, Color Color, float Scale, float ScaleEnd, float Intensity, float EndpointBoost, float SelfBrighten, float FadeEnd, float EdgeExponentStart, float EdgeExponentEnd, float EndpointSquash, float PoolScale, float TicksRemaining, float TotalTicks, float FadeInTicks)> fadingGlows = new();
		readonly Dictionary<WPos, int> glowsPerSource = new();

		// World tick at the previous Draw; used to advance fades by elapsed ticks rather than render frames.
		int lastWorldTick = -1;

		readonly float[] beamStarts = new float[MaxBeamsPerBatch * 2];
		readonly float[] beamEnds = new float[MaxBeamsPerBatch * 2];
		readonly float[] glowColors = new float[MaxBeamsPerBatch * 3];
		readonly float[] glowIntensities = new float[MaxBeamsPerBatch];
		readonly float[] glowRadii = new float[MaxBeamsPerBatch];
		readonly float[] glowRadiiEnd = new float[MaxBeamsPerBatch];
		readonly float[] endpointBoosts = new float[MaxBeamsPerBatch];
		readonly float[] selfBrightens = new float[MaxBeamsPerBatch];
		readonly float[] glowFadeEnds = new float[MaxBeamsPerBatch];
		readonly float[] edgeExponentStarts = new float[MaxBeamsPerBatch];
		readonly float[] edgeExponentEnds = new float[MaxBeamsPerBatch];
		readonly float[] endpointSquashes = new float[MaxBeamsPerBatch];
		readonly float[] poolRadii = new float[MaxBeamsPerBatch];

		// Reused per batch: the draw quad is resized to the batch's bounding box instead of full-screen.
		readonly RenderPostProcessPassVertex[] quadVerts = new RenderPostProcessPassVertex[6];

		public GlowRenderer(GlowRendererInfo info)
		{
			this.info = info;
			renderer = Game.Renderer;
			shader = renderer.CreateShader(new RenderPostProcessPassShaderBindings("glow"));

			// Dynamic: SetData rewrites these six vertices every batch to cover only the beams' bounding box.
			buffer = renderer.CreateVertexBuffer(new RenderPostProcessPassVertex[6], true);
		}

		// intensity is a brightness-only multiplier (independent of scale, which also drives radius).
		// scaleEnd tapers the radius from scale (at source) to scaleEnd (at target) to form a cone;
		// pass -1 to keep a uniform-radius beam.
		// selfBrighten (>0) applies a radial gamma lift to the scene's own pixels under the glow (no
		// added color), brightening shadows/midtones without blowing out highlights — a localized light.
		// fadeEnd dims the body brightness toward the target (1 = no change).
		// edgeExponentStart/edgeExponentEnd control the falloff exponent at the source/target (2 = the
		// original Gaussian; higher gives a crisper edge, lower a softer/more diffuse one) and are
		// interpolated along the segment the same way scale/scaleEnd are.
		// endpointBoost (>0) adds a separate endpoint pool centred on the target at that peak brightness,
		// combined with the body via a soft-max; 0 (the default for every non-searchlight caller) disables
		// the pool entirely. poolScale is the pool's radius scale (independent of the body's scaleEnd) and
		// endpointSquash (default 1) flattens the pool vertically into a ground-hugging ellipse.
		public void RegisterGlow(WPos source, WPos target, Color color, float scale = 1f, int fadeFrames = 0, int fadeInFrames = 0, float intensity = 1f, float scaleEnd = -1f, float endpointBoost = 0f, float selfBrighten = 0f, float fadeEnd = 1f, float edgeExponentStart = 2f, float edgeExponentEnd = 2f, float endpointSquash = 1f, float poolScale = 0f)
		{
			// Render-only cosmetic state that is drained exclusively by Draw (render tick). While the
			// window is minimized the render tick never runs, so nothing drains these lists, yet the
			// simulation keeps detonating warheads and ticking searchlights that call this. Dropping the
			// registration while suspended is what keeps the lists from growing without bound and flushing
			// in one frame on restore. Sync is unaffected: the simulation never reads this state.
			if (Game.Renderer.WindowIsSuspended)
				return;

			if (scaleEnd < 0f)
				scaleEnd = scale;

			// If no glow is currently active, Draw has been idle (it only runs while effects exist) so
			// lastWorldTick is stale; reset it, otherwise the first Draw would advance the fade by the whole
			// idle gap and instantly expire this glow.
			if (pendingGlows.Count == 0 && fadingGlows.Count == 0)
				lastWorldTick = -1;

			if (fadeFrames > 0)
			{
				// Defensive bound for the render-starved case: drop the oldest, most-faded
				// glow rather than let the list grow without limit.
				if (fadingGlows.Count >= MaxActiveEffects)
					fadingGlows.RemoveAt(0);

				var totalTicks = fadeFrames * FramesToTicks;
				var fadeInTicks = fadeInFrames * FramesToTicks;
				fadingGlows.Add((source, target, color, scale, scaleEnd, intensity, endpointBoost, selfBrighten, fadeEnd, edgeExponentStart, edgeExponentEnd, endpointSquash, poolScale, totalTicks, totalTicks, fadeInTicks));
				return;
			}

			// Cap at 2 simultaneous beam glows per source position per frame.
			// Rapid-fire weapons that produce more than 2 beams per tick skip the extras.
			glowsPerSource.TryGetValue(source, out var count);
			if (count >= 2)
				return;

			if (pendingGlows.Count >= MaxActiveEffects)
				return;

			glowsPerSource[source] = count + 1;
			pendingGlows.Add((source, target, color, scale, scaleEnd, intensity, endpointBoost, selfBrighten, fadeEnd, edgeExponentStart, edgeExponentEnd, endpointSquash, poolScale));
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
			var batch = new List<(WPos Source, WPos Target, Color Color, float Scale, float ScaleEnd, float Intensity, float EndpointBoost, float SelfBrighten, float FadeEnd, float EdgeExponentStart, float EdgeExponentEnd, float EndpointSquash, float PoolScale)>(pendingGlows.Count + fadingGlows.Count);
			foreach (var g in pendingGlows)
				batch.Add(g);
			pendingGlows.Clear();
			glowsPerSource.Clear();

			// Advance fades by the number of game ticks since the last Draw. At high framerates multiple
			// frames render per tick (elapsed == 0, glow holds steady); while paused WorldTick is frozen.
			var worldTick = wr.World.WorldTick;
			var ticksElapsed = lastWorldTick < 0 ? 0f : Math.Max(0, worldTick - lastWorldTick);
			lastWorldTick = worldTick;

			for (var i = fadingGlows.Count - 1; i >= 0; i--)
			{
				var glow = fadingGlows[i];
				var ticksPassed = glow.TotalTicks - glow.TicksRemaining;
				float fadeScale;
				if (glow.FadeInTicks > 0 && ticksPassed < glow.FadeInTicks)
					fadeScale = ticksPassed / glow.FadeInTicks;
				else
				{
					var fadeOutTotal = glow.TotalTicks - glow.FadeInTicks;
					fadeScale = fadeOutTotal > 0 ? glow.TicksRemaining / fadeOutTotal : 1f;
				}

				fadeScale = Math.Clamp(fadeScale, 0f, 1f);
				batch.Add((glow.Source, glow.Target, glow.Color, glow.Scale * fadeScale, glow.ScaleEnd * fadeScale, glow.Intensity, glow.EndpointBoost, glow.SelfBrighten * fadeScale, glow.FadeEnd, glow.EdgeExponentStart, glow.EdgeExponentEnd, glow.EndpointSquash, glow.PoolScale));

				var remaining = glow.TicksRemaining - ticksElapsed;
				if (remaining <= 0f)
					fadingGlows.RemoveAt(i);
				else
					fadingGlows[i] = (glow.Source, glow.Target, glow.Color, glow.Scale, glow.ScaleEnd, glow.Intensity, glow.EndpointBoost, glow.SelfBrighten, glow.FadeEnd, glow.EdgeExponentStart, glow.EdgeExponentEnd, glow.EndpointSquash, glow.PoolScale, remaining, glow.TotalTicks, glow.FadeInTicks);
			}

			// Draw glows in fixed-size batches. Each batch takes one framebuffer snapshot and runs
			// a single shader pass that loops over all beams, applying screen-blend math iteratively.
			var fbW = (float)renderer.WorldFrameBufferSize.Width;
			var fbH = (float)renderer.WorldFrameBufferSize.Height;
			for (var offset = 0; offset < batch.Count; offset += MaxBeamsPerBatch)
			{
				var batchSize = Math.Min(MaxBeamsPerBatch, batch.Count - offset);

				// Screen-space bounding box of this batch's beams (framebuffer px), expanded per beam by the
				// glow/pool radius so the draw quad can be shrunk to just the lit region instead of full-screen.
				var minX = float.MaxValue;
				var minY = float.MaxValue;
				var maxX = float.MinValue;
				var maxY = float.MinValue;

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

					// Brightness uses the average scale so a tapered cone is not dimmed by its narrow source;
					// for uniform beams (Scale == ScaleEnd) this is identical to the source scale.
					var brightnessScale = (g.Scale + g.ScaleEnd) * 0.5f;
					glowIntensities[i] = info.GlowIntensity * brightnessScale * g.Intensity * (g.Color.A / 255f);
					glowRadii[i] = info.GlowRadius * g.Scale;
					glowRadiiEnd[i] = info.GlowRadius * g.ScaleEnd;
					endpointBoosts[i] = g.EndpointBoost;
					selfBrightens[i] = g.SelfBrighten;
					glowFadeEnds[i] = g.FadeEnd;
					edgeExponentStarts[i] = g.EdgeExponentStart;
					edgeExponentEnds[i] = g.EdgeExponentEnd;
					endpointSquashes[i] = g.EndpointSquash;
					poolRadii[i] = info.GlowRadius * g.PoolScale;

					// Widen the box by the beam's own visible reach: softer edge exponents and brighter peaks
					// (body intensity, or the endpoint pool when EndpointBoost > 1) extend further before the
					// contribution drops below ~1/255. eMin is the tighter (widest-reaching) of the two exponents.
					var peak = glowIntensities[i] * Math.Max(1f, endpointBoosts[i]);
					var eMin = Math.Max(0.25f, Math.Min(edgeExponentStarts[i], edgeExponentEnds[i]));
					var falloffRadii = peak > 1f / 255f ? (float)Math.Pow(Math.Log(255f * peak), 1.0 / eMin) : 0f;
					falloffRadii = Math.Clamp(falloffRadii, PadRadiiFloor, PadRadiiCap);
					var pad = falloffRadii * Math.Max(Math.Max(glowRadii[i], glowRadiiEnd[i]), poolRadii[i]);
					minX = Math.Min(minX, Math.Min(p1.X, p2.X) - pad);
					minY = Math.Min(minY, Math.Min(p1.Y, p2.Y) - pad);
					maxX = Math.Max(maxX, Math.Max(p1.X, p2.X) + pad);
					maxY = Math.Max(maxY, Math.Max(p1.Y, p2.Y) + pad);
				}

				// Clamp the box to the framebuffer; skip the whole batch if it is entirely offscreen.
				minX = Math.Max(0f, minX);
				minY = Math.Max(0f, minY);
				maxX = Math.Min(fbW, maxX);
				maxY = Math.Min(fbH, maxY);
				if (maxX <= minX || maxY <= minY)
					continue;

				// Map the box (framebuffer px — the same space as gl_FragCoord and the beam coordinates) to NDC
				// and upload it as the draw quad. The passthrough vertex shader makes NDC == vertex position, so
				// only fragments inside the box execute the glow shader instead of every pixel of the buffer.
				var nx0 = minX / fbW * 2f - 1f;
				var nx1 = maxX / fbW * 2f - 1f;
				var ny0 = minY / fbH * 2f - 1f;
				var ny1 = maxY / fbH * 2f - 1f;
				quadVerts[0] = new RenderPostProcessPassVertex(nx0, ny0);
				quadVerts[1] = new RenderPostProcessPassVertex(nx1, ny0);
				quadVerts[2] = new RenderPostProcessPassVertex(nx1, ny1);
				quadVerts[3] = new RenderPostProcessPassVertex(nx1, ny1);
				quadVerts[4] = new RenderPostProcessPassVertex(nx0, ny1);
				quadVerts[5] = new RenderPostProcessPassVertex(nx0, ny0);
				buffer.SetData(quadVerts, 6);

				shader.SetTexture("WorldTexture", Game.Renderer.GetRenderBufferSnapshot());

				// ANGLE/ES rejects glUniformXfv with count > 1 on array uniforms, so set each element individually.
				for (var i = 0; i < batchSize; i++)
				{
					shader.SetVec(BeamStartsKeys[i], beamStarts[i * 2], beamStarts[i * 2 + 1]);
					shader.SetVec(BeamEndsKeys[i], beamEnds[i * 2], beamEnds[i * 2 + 1]);
					shader.SetVec(GlowColorsKeys[i], glowColors[i * 3], glowColors[i * 3 + 1], glowColors[i * 3 + 2]);
					shader.SetVec(GlowIntensitiesKeys[i], glowIntensities[i]);
					shader.SetVec(GlowRadiiKeys[i], glowRadii[i]);
					shader.SetVec(GlowRadiiEndKeys[i], glowRadiiEnd[i]);
					shader.SetVec(EndpointBoostsKeys[i], endpointBoosts[i]);
					shader.SetVec(SelfBrightensKeys[i], selfBrightens[i]);
					shader.SetVec(GlowFadeEndsKeys[i], glowFadeEnds[i]);
					shader.SetVec(EdgeExponentStartsKeys[i], edgeExponentStarts[i]);
					shader.SetVec(EdgeExponentEndsKeys[i], edgeExponentEnds[i]);
					shader.SetVec(EndpointSquashesKeys[i], endpointSquashes[i]);
					shader.SetVec(PoolRadiiKeys[i], poolRadii[i]);
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
