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
using System.Linq;
using OpenRA.Effects;
using OpenRA.Graphics;
using OpenRA.Mods.AS.Traits;
using OpenRA.Primitives;

namespace OpenRA.Mods.AS.Graphics
{
	public class TintedCell : IRenderable, IFinalizedRenderable, IEffect
	{
		public int Ticks = 0;
		readonly TintedCellsLayer layer;
		readonly CPos cpos;
		readonly WPos wpos;

		public int Level { get; private set; }
		public int ZOffset { get { return layer.Info.ZOffset; } }

		public TintedCell(TintedCellsLayer layer, CPos cpos, WPos wpos)
		{
			this.layer = layer;
			this.cpos = cpos;
			this.wpos = wpos;
		}

		public TintedCell(TintedCell src)
		{
			Ticks = src.Ticks;
			Level = src.Level;
			layer = src.layer;
			cpos = src.cpos;
			wpos = src.wpos;
		}

		public IRenderable WithPalette(PaletteReference newPalette) { return this; }
		public IRenderable WithZOffset(int newOffset) { return this; }
		public IRenderable OffsetBy(in WVec vec) { return this; }
		public IRenderable AsDecoration() { return this; }

		public PaletteReference Palette { get { return null; } }
		public bool IsDecoration { get { return false; } }

		WPos IRenderable.Pos { get { return wpos; } }

		IFinalizedRenderable IRenderable.PrepareRender(WorldRenderer wr) { return this; }

		bool firstTime = true;
		float3[] screen;
		float3 center;
		int alpha;
		public void Render(WorldRenderer wr)
		{
			if (firstTime)
			{
				var map = wr.World.Map;
				var terrainInfo = map.Rules.TerrainInfo;
				var uv = cpos.ToMPos(map);

				if (!map.Height.Contains(uv))
					return;

				var tile = map.Tiles[uv];
				var ti = terrainInfo.GetTerrainInfo(tile);
				var ramp = ti != null ? ti.RampType : 0;

				var corners = map.Grid.Ramps[ramp].Corners;
				screen = corners.Select(c => wr.Screen3DPxPosition(wpos + c - new WVec(0, 0, map.Grid.Ramps[ramp].CenterHeightOffset) + layer.Info.Offset)).ToArray();
				center = new float3((screen[0].X + screen[1].X) / 2f, (screen[1].Y + screen[2].Y) / 2f, screen[1].Z);
				firstTime = false;
			}

			if (layer == null || screen == null)
				return;

			var selfLevel = layer.GetTileLevel(cpos);
			SetLevel(selfLevel);

			if (Level == 0)
				return;

			var topLevel = layer.GetTileLevel(new CPos(cpos.X, cpos.Y - 1));
			var rightLevel = layer.GetTileLevel(new CPos(cpos.X + 1, cpos.Y));
			var leftLevel = layer.GetTileLevel(new CPos(cpos.X - 1, cpos.Y));
			var bottomLevel = layer.GetTileLevel(new CPos(cpos.X, cpos.Y + 1));

			var tintedNeighbors = 0;
			if (topLevel > 0)
				tintedNeighbors++;
			if (rightLevel > 0)
				tintedNeighbors++;
			if (leftLevel > 0)
				tintedNeighbors++;
			if (bottomLevel > 0)
				tintedNeighbors++;

			var anyTriangle = false;

			if (tintedNeighbors >= 3)
			{
				anyTriangle |= RenderTriangleSegment(screen[0], screen[1], (selfLevel + topLevel) / 2);
				anyTriangle |= RenderTriangleSegment(screen[1], screen[2], (selfLevel + rightLevel) / 2);
				anyTriangle |= RenderTriangleSegment(screen[0], screen[3], (selfLevel + leftLevel) / 2);
				anyTriangle |= RenderTriangleSegment(screen[2], screen[3], (selfLevel + bottomLevel) / 2);
			}
			else
			{
				if (topLevel > 0)
					anyTriangle |= RenderTriangleSegment(screen[0], screen[1], (selfLevel + topLevel) / 2);
				if (rightLevel > 0)
					anyTriangle |= RenderTriangleSegment(screen[1], screen[2], (selfLevel + rightLevel) / 2);
				if (leftLevel > 0)
					anyTriangle |= RenderTriangleSegment(screen[0], screen[3], (selfLevel + leftLevel) / 2);
				if (bottomLevel > 0)
					anyTriangle |= RenderTriangleSegment(screen[2], screen[3], (selfLevel + bottomLevel) / 2);
			}

			if (!anyTriangle)
				Game.Renderer.WorldRgbaColorRenderer.FillRect(screen[0], screen[1], screen[2], screen[3], Color.FromArgb(alpha, layer.Info.Color));
		}

		bool RenderTriangleSegment(in float3 edgeA, in float3 edgeB, int blendLevel)
		{
			var triangleAlpha = AlphaForLevel(blendLevel);
			if (triangleAlpha <= 0 || layer == null)
				return false;

			Game.Renderer.WorldRgbaColorRenderer.FillTriangle(center, edgeA, edgeB, Color.FromArgb(triangleAlpha, layer.Info.Color));
			return true;
		}

		int AlphaForLevel(int value)
		{
			if (layer == null)
				return 0;

			var clamped = value.Clamp(0, layer.Info.MaxLevel);
			return layer.Info.Darkest + layer.TintLevel * clamped / 255;
		}

		public void SetLevel(int value)
		{
			Level = value;

			if (layer == null)
				return;

			// Saturate the visualization to MaxLevel
			var level = Level.Clamp(0, layer.Info.MaxLevel);

			// Linear interpolation
			alpha = layer.Info.Darkest + layer.TintLevel * level / 255;
		}

		public void RenderDebugGeometry(WorldRenderer wr) { }
		public Rectangle ScreenBounds(WorldRenderer wr) { return Rectangle.Empty; }
		public void Tick(World world) { }
		IEnumerable<IRenderable> IEffect.Render(WorldRenderer r) { yield return this; }
	}
}
