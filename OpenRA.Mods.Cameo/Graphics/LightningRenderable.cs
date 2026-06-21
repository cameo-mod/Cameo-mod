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
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Support;

namespace OpenRA.Mods.Cameo.Graphics
{
	// Draws an instant electric bolt as a procedural fractal arc: the channel is built by recursive
	// midpoint displacement (each segment splits at its midpoint, nudged perpendicular, with the nudge
	// shrinking each generation) so every wiggle sprouts smaller self-similar sub-wiggles, like real
	// lightning. A white-hot additive core, an additive blue glow, tapering branches and glowing plasma
	// orbs (with radiating spark hairs) at both ends complete it. The geometry is built in screen space
	// each frame (cosmetic, CosmeticRandom-driven, so it flickers and is not part of the simulation).
	public sealed class LightningRenderable : IRenderable, IFinalizedRenderable
	{
		readonly WVec length;
		readonly int generations;
		readonly float roughness;
		readonly WDist amplitude;
		readonly int branches;
		readonly WDist branchLength;
		readonly WDist nodeRadius;
		readonly int nodeHairs;
		readonly WDist nodeHairLength;
		readonly Color coreColor;
		readonly Color glowColor;
		readonly WDist coreWidth;
		readonly WDist glowWidth;
		readonly int glowAlpha;
		readonly float glowScale;
		readonly float glowIntensity;

		public LightningRenderable(WPos pos, int zOffset, in WVec length,
			int generations, float roughness, WDist amplitude,
			int branches, WDist branchLength, WDist nodeRadius, int nodeHairs, WDist nodeHairLength,
			Color coreColor, Color glowColor, WDist coreWidth,
			WDist glowWidth, int glowAlpha, float glowScale, float glowIntensity)
		{
			Pos = pos;
			ZOffset = zOffset;
			this.length = length;
			this.generations = generations;
			this.roughness = roughness;
			this.amplitude = amplitude;
			this.branches = branches;
			this.branchLength = branchLength;
			this.nodeRadius = nodeRadius;
			this.nodeHairs = nodeHairs;
			this.nodeHairLength = nodeHairLength;
			this.coreColor = coreColor;
			this.glowColor = glowColor;
			this.coreWidth = coreWidth;
			this.glowWidth = glowWidth;
			this.glowAlpha = glowAlpha;
			this.glowScale = glowScale;
			this.glowIntensity = glowIntensity;
		}

		public WPos Pos { get; }
		public int ZOffset { get; }
		public bool IsDecoration => true;

		LightningRenderable With(WPos pos, int zOffset) =>
			new(pos, zOffset, length, generations, roughness, amplitude,
				branches, branchLength, nodeRadius, nodeHairs, nodeHairLength,
				coreColor, glowColor, coreWidth, glowWidth, glowAlpha, glowScale, glowIntensity);

		public IRenderable WithZOffset(int newOffset) => With(Pos, newOffset);
		public IRenderable OffsetBy(in WVec vec) => With(Pos + vec, ZOffset);
		public IRenderable AsDecoration() => this;

		public IFinalizedRenderable PrepareRender(WorldRenderer wr) => this;
		public void RenderDebugGeometry(WorldRenderer wr) { }
		public Rectangle ScreenBounds(WorldRenderer wr) => Rectangle.Empty;

		public void Render(WorldRenderer wr)
		{
			if (length.Length == 0)
				return;

			if (wr.World.FogObscures(Pos) && wr.World.FogObscures(Pos + length))
				return;

			var cr = Game.Renderer.WorldRgbaColorRenderer;
			var src = wr.Screen3DPosition(Pos);
			var tgt = wr.Screen3DPosition(Pos + length);

			// All widths/sizes come from fixed world distances projected to screen, so they stay
			// constant regardless of the bolt's reach (matching the existing core/glow widths).
			var coreScreen = wr.ScreenVector(new WVec(coreWidth, WDist.Zero, WDist.Zero))[0];
			var glowScreen = wr.ScreenVector(new WVec(glowWidth, WDist.Zero, WDist.Zero))[0];
			var ampScreen = Math.Abs(wr.ScreenVector(new WVec(amplitude, WDist.Zero, WDist.Zero))[0]);
			var branchScreen = Math.Abs(wr.ScreenVector(new WVec(branchLength, WDist.Zero, WDist.Zero))[0]);

			var dx = tgt.X - src.X;
			var dy = tgt.Y - src.Y;
			var screenLen = (float)Math.Sqrt(dx * dx + dy * dy);

			// The largest (first-generation) displacement; capped so a near-point-blank bolt doesn't
			// swing wider than it is long.
			var maxDev = Math.Min(ampScreen, 0.45f * screenLen);

			var rnd = Game.CosmeticRandom;
			var main = LightningGeometry.MidpointPath(src, tgt, maxDev, generations, roughness, rnd);

			// Soft gaussian bloom that FOLLOWS the bolt: the GlowRenderer only glows straight segments,
			// so we approximate the jagged path with a few short beams chained along it (instead of one
			// straight muzzle-to-target capsule). Gated by the "Weapon Glow Effects" setting; the segment
			// count is capped per bolt so the post-process batch count stays bounded no matter how many
			// bolts fire at once. Screen points are projected back to ground positions because the
			// GlowRenderer batches in world space.
			if (Game.Settings.Graphics.LaserGlow && glowScale > 0f && main.Count > 1)
			{
				var glow = wr.World.WorldActor.TraitOrDefault<GlowRenderer>();
				if (glow != null)
				{
					const int MaxGlowSegments = 8;
					var stepN = Math.Max(1, (main.Count - 1) / MaxGlowSegments);
					var prev = wr.ProjectedPosition(main[0].XY.ToInt2());
					for (var i = stepN; i < main.Count; i += stepN)
					{
						var cur = wr.ProjectedPosition(main[i].XY.ToInt2());
						glow.RegisterGlow(prev, cur, glowColor, glowScale, intensity: glowIntensity);
						prev = cur;
					}

					var end = wr.ProjectedPosition(main[^1].XY.ToInt2());
					if (end != prev)
						glow.RegisterGlow(prev, end, glowColor, glowScale, intensity: glowIntensity);
				}
			}

			// Thin additive bolt drawn FROM the path: a tight inner glow and a hot near-white core. This
			// is the always-on baseline (kept thin so it doesn't fatten the bolt); the soft wide halo is
			// the gaussian GlowRenderer bloom above, which only kicks in with the Weapon Glow setting.
			// NOTE: segments are drawn DISCONNECTED (no miter joins). RgbaColorRenderer's connected-line
			// path mitres corners via a line intersection that "behaves badly" for near-parallel segments;
			// the sharp fractal kinks would blow that mitre vertex out to near-infinity and streak a giant
			// quad across the screen. Independent butt-capped segments avoid that, and the joints are
			// invisible on a thin additive bolt.
			var glowOuter = Color.FromArgb(glowAlpha, glowColor);
			var glowInner = Color.FromArgb(Math.Min(255, glowAlpha + 50), glowColor);
			cr.DrawLine(main, glowScreen, glowOuter, false, BlendMode.Additive);
			cr.DrawLine(main, glowScreen * 0.5f, glowInner, false, BlendMode.Additive);
			cr.DrawLine(main, coreScreen, coreColor, false, BlendMode.Additive);

			var branchDev = maxDev * 0.6f;
			for (var b = 0; b < branches; b++)
			{
				var t = 0.2f + 0.6f * rnd.NextFloat();
				var root = new float3(src.X + dx * t, src.Y + dy * t, src.Z + (tgt.Z - src.Z) * t);
				var ang = (float)Math.Atan2(dy, dx) + (rnd.NextFloat() < 0.5f ? -1f : 1f) * (0.4f + 0.7f * rnd.NextFloat());
				var blen = branchScreen * (0.7f + 0.6f * rnd.NextFloat());
				DrawBranch(cr, BuildBranch(root, ang, blen, branchDev, rnd), coreScreen, glowScreen);
			}

			// Glowing plasma "balls" with radiating spark hairs where the bolt grounds: one at the
			// firing point, one at the impact point.
			if (nodeRadius.Length > 0 && nodeHairs > 0)
			{
				var nodeScreen = Math.Abs(wr.ScreenVector(new WVec(nodeRadius, WDist.Zero, WDist.Zero))[0]);
				var hairLenScreen = Math.Abs(wr.ScreenVector(new WVec(nodeHairLength, WDist.Zero, WDist.Zero))[0]);
				var hairDev = nodeScreen * 0.35f;
				DrawNode(cr, src, nodeScreen, hairLenScreen, hairDev, coreScreen, glowScreen, rnd);
				DrawNode(cr, tgt, nodeScreen, hairLenScreen, hairDev, coreScreen, glowScreen, rnd);
			}
		}

		static List<float3> BuildBranch(in float3 root, float ang, float length, float devScreen, MersenneTwister rnd)
		{
			var dirX = (float)Math.Cos(ang);
			var dirY = (float)Math.Sin(ang);
			var perpX = -dirY;
			var perpY = dirX;
			const int n = 4;

			var pts = new List<float3>();
			for (var i = 0; i <= n; i++)
			{
				var t = i / (float)n;
				var dev = (i == 0 || i == n ? 0f : 1f) * (rnd.NextFloat() * 2f - 1f) * devScreen * (1f - t);
				pts.Add(new float3(
					root.X + dirX * length * t + perpX * dev,
					root.Y + dirY * length * t + perpY * dev,
					root.Z));
			}

			return pts;
		}

		void DrawBranch(RgbaColorRenderer cr, List<float3> pts, float coreScreen, float glowScreen)
		{
			var count = pts.Count - 1;
			for (var i = 0; i < count; i++)
			{
				var fade = 1f - i / (float)count;                    // taper brightness toward the tip
				var ga = (int)(glowAlpha * 0.7f * fade);
				var ca = (int)(255 * fade);
				if (ga > 0)
					cr.DrawLine(pts[i], pts[i + 1], glowScreen * 0.5f, Color.FromArgb(ga, glowColor), BlendMode.Additive);
				if (ca > 0)
					cr.DrawLine(pts[i], pts[i + 1], coreScreen * 0.7f, Color.FromArgb(ca, coreColor), BlendMode.Additive);
			}
		}

		void DrawNode(RgbaColorRenderer cr, in float3 c, float radius, float hairLen, float hairDev,
			float coreScreen, float glowScreen, MersenneTwister rnd)
		{
			// Soft additive orb: a faint wide halo, a brighter mid, then a hot near-white centre.
			DrawDisc(cr, c, radius, Color.FromArgb(45, glowColor));
			DrawDisc(cr, c, radius * 0.6f, Color.FromArgb(95, glowColor));
			DrawDisc(cr, c, radius * 0.32f, Color.FromArgb(230, coreColor));

			// Short jagged filaments radiating evenly around the orb (each slot jittered so it flickers).
			for (var i = 0; i < nodeHairs; i++)
			{
				var ang = (i + (rnd.NextFloat() - 0.5f)) / nodeHairs * 2f * (float)Math.PI;
				var len = hairLen * (0.6f + 0.7f * rnd.NextFloat());
				DrawBranch(cr, BuildBranch(c, ang, len, hairDev, rnd), coreScreen, glowScreen);
			}
		}

		static void DrawDisc(RgbaColorRenderer cr, in float3 c, float radius, Color color)
		{
			if (radius < 0.5f)
				return;

			cr.FillEllipse(
				new float3(c.X - radius, c.Y - radius, c.Z),
				new float3(c.X + radius, c.Y + radius, c.Z),
				color, BlendMode.Additive);
		}
	}
}
