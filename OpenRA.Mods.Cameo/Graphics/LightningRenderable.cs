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
	// Draws an instant electric bolt as a procedural plasma arc: a jagged channel with a mix of
	// sharp and soft turns, a white-hot additive core, an additive blue glow, and a few tapering
	// branches. The geometry is built in screen space each frame (cosmetic, CosmeticRandom-driven,
	// so it flickers and is not part of the simulation), oriented from the emitter to the target.
	//
	// All sizes (swing amplitude, branch length, kink spacing) are FIXED world distances rather than
	// fractions of the bolt's length, so the bolt reads at a consistent size whether it reaches a
	// close or a distant target. The turn count scales with length to keep the kink spacing constant.
	public sealed class LightningRenderable : IRenderable, IFinalizedRenderable
	{
		readonly WVec length;
		readonly int segments;
		readonly WDist segmentLength;
		readonly WDist amplitude;
		readonly float softness;
		readonly float roundFraction;
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
			int segments, WDist segmentLength, WDist amplitude, float softness, float roundFraction,
			int branches, WDist branchLength, WDist nodeRadius, int nodeHairs, WDist nodeHairLength,
			Color coreColor, Color glowColor, WDist coreWidth,
			WDist glowWidth, int glowAlpha, float glowScale, float glowIntensity)
		{
			Pos = pos;
			ZOffset = zOffset;
			this.length = length;
			this.segments = segments;
			this.segmentLength = segmentLength;
			this.amplitude = amplitude;
			this.softness = softness;
			this.roundFraction = roundFraction;
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
			new(pos, zOffset, length, segments, segmentLength, amplitude, softness, roundFraction,
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

			if (Game.Settings.Graphics.LaserGlow && glowScale > 0f)
				wr.World.WorldActor.TraitOrDefault<GlowRenderer>()
					?.RegisterGlow(Pos, Pos + length, glowColor, glowScale, intensity: glowIntensity);

			var cr = Game.Renderer.WorldRgbaColorRenderer;
			var src = wr.Screen3DPosition(Pos);
			var tgt = wr.Screen3DPosition(Pos + length);

			// All widths/sizes come from fixed world distances projected to screen, so they stay
			// constant regardless of the bolt's reach (matching the existing core/glow widths).
			var coreScreen = wr.ScreenVector(new WVec(coreWidth, WDist.Zero, WDist.Zero))[0];
			var glowScreen = wr.ScreenVector(new WVec(glowWidth, WDist.Zero, WDist.Zero))[0];
			var ampScreen = Math.Abs(wr.ScreenVector(new WVec(amplitude, WDist.Zero, WDist.Zero))[0]);
			var branchScreen = Math.Abs(wr.ScreenVector(new WVec(branchLength, WDist.Zero, WDist.Zero))[0]);

			// Turn count scales with world length so the spacing between kinks stays constant.
			var segLenWorld = Math.Max(1, segmentLength.Length);
			var n = Math.Clamp((int)Math.Round(length.Length / (double)segLenWorld), 3, Math.Max(3, segments));

			var dx = tgt.X - src.X;
			var dy = tgt.Y - src.Y;
			var screenLen = (float)Math.Sqrt(dx * dx + dy * dy);

			// For very short bolts the fixed swing could exceed the bolt's own on-screen length and
			// tangle it; clamp the swing so it never dominates a near-point-blank shot.
			var maxDev = Math.Min(ampScreen, 0.45f * screenLen);

			var rnd = Game.CosmeticRandom;
			var main = BuildPath(src, tgt, maxDev, n, rnd);

			// Additive glow passes (wide+faint, then narrower+brighter) build a plasma-like bloom
			// where the channel overlaps itself, then a hot near-white core on top.
			// NOTE: segments are drawn DISCONNECTED (no miter joins). RgbaColorRenderer's connected-line
			// path mitres corners via a line intersection that "behaves badly" for near-parallel segments;
			// our deliberately sharp turns occasionally double back, which blew the mitre vertex out to
			// near-infinity and streaked a giant quad across the screen. Independent butt-capped segments
			// avoid that entirely, and the joints are invisible on a thin additive bolt.
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

		List<float3> BuildPath(in float3 src, in float3 tgt, float maxDev, int n, MersenneTwister rnd)
		{
			var dx = tgt.X - src.X;
			var dy = tgt.Y - src.Y;
			var len = (float)Math.Sqrt(dx * dx + dy * dy);
			if (len < 1f)
				return new List<float3> { src, tgt };

			var perpX = -dy / len;
			var perpY = dx / len;

			var ax = new float[n + 1];
			var ay = new float[n + 1];
			var az = new float[n + 1];
			for (var i = 0; i <= n; i++)
			{
				var t = i / (float)n;
				if (i > 0 && i < n)
					t += (rnd.NextFloat() * 2f - 1f) * 0.4f / n;     // jitter interior anchors along the axis

				var bx = src.X + dx * t;
				var by = src.Y + dy * t;
				var bz = src.Z + (tgt.Z - src.Z) * t;
				var dev = (i == 0 || i == n ? 0.1f : 1f) * (rnd.NextFloat() * 2f - 1f) * maxDev;
				ax[i] = bx + perpX * dev;
				ay[i] = by + perpY * dev;
				az[i] = bz;
			}

			var pts = new List<float3> { new(ax[0], ay[0], az[0]) };
			for (var i = 1; i < n; i++)
			{
				if (rnd.NextFloat() < softness)
				{
					// Soft turn: a short quadratic arc through the anchor, rounding the corner.
					var aX = ax[i] + (ax[i - 1] - ax[i]) * roundFraction;
					var aY = ay[i] + (ay[i - 1] - ay[i]) * roundFraction;
					var aZ = az[i] + (az[i - 1] - az[i]) * roundFraction;
					var bX = ax[i] + (ax[i + 1] - ax[i]) * roundFraction;
					var bY = ay[i] + (ay[i + 1] - ay[i]) * roundFraction;
					var bZ = az[i] + (az[i + 1] - az[i]) * roundFraction;
					const int steps = 6;
					for (var s = 0; s <= steps; s++)
					{
						var tq = s / (float)steps;
						var mt = 1f - tq;
						pts.Add(new float3(
							mt * mt * aX + 2f * mt * tq * ax[i] + tq * tq * bX,
							mt * mt * aY + 2f * mt * tq * ay[i] + tq * tq * bY,
							mt * mt * aZ + 2f * mt * tq * az[i] + tq * tq * bZ));
					}
				}
				else
					pts.Add(new float3(ax[i], ay[i], az[i]));        // hard turn: sharp corner
			}

			pts.Add(new float3(ax[n], ay[n], az[n]));
			return pts;
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
	}
}
