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
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits
{
	// ---------------------------------------------------------------------------
	// World-level trait: owns the TerrainSpriteLayer and IRenderOverlay
	// ---------------------------------------------------------------------------

	[TraitLocation(SystemActors.World)]
	[Desc("Renders creep ground overlay tiles for all active WithCreepOverlay buildings.",
		"Must be placed on the World actor before ResourceRenderer so creep appears below resources.")]
	public class CreepLayerInfo : TraitInfo, Requires<ITiledTerrainRendererInfo>
	{
		[Desc("Image containing the creep sequence.")]
		public readonly string Image = "sczergsoil";

		[SequenceReference(nameof(Image))]
		[Desc("Sequence to use for the creep tile.")]
		public readonly string Sequence = "idle";

		[PaletteReference]
		[Desc("Palette to render the creep in.")]
		public readonly string Palette = TileSet.TerrainPaletteInternalName;

		public override object Create(ActorInitializer init) { return new CreepLayer(init.Self, this); }
	}

	public class CreepLayer : IRenderOverlay, IWorldLoaded, INotifyActorDisposing
	{
		readonly CreepLayerInfo info;
		readonly World world;
		readonly Dictionary<CPos, int> refCount = new();
		readonly Dictionary<CPos, bool> dirty = new();

		TerrainSpriteLayer render;
		Sprite creepSprite;
		float creepScale;
		PaletteReference paletteReference;
		bool disposed;

		public CreepLayer(Actor self, CreepLayerInfo info)
		{
			this.info = info;
			world = self.World;
		}

		void IWorldLoaded.WorldLoaded(World w, WorldRenderer wr)
		{
			var seq = w.Map.Sequences.GetSequence(info.Image, info.Sequence);
			creepSprite = seq.GetSprite(0);
			creepScale = seq.Scale;

			var emptySprite = new Sprite(creepSprite.Sheet, Rectangle.Empty, TextureChannel.Alpha);
			render = new TerrainSpriteLayer(w, wr, emptySprite, creepSprite.BlendMode, true);
			paletteReference = wr.Palette(info.Palette);
		}

		public void AddCells(IEnumerable<CPos> cells)
		{
			foreach (var cell in cells)
			{
				if (!world.Map.Contains(cell))
					continue;

				refCount.TryGetValue(cell, out var count);
				refCount[cell] = count + 1;
				if (count == 0)
					dirty[cell] = true;
			}
		}

		public void RemoveCells(IEnumerable<CPos> cells)
		{
			foreach (var cell in cells)
			{
				if (!refCount.TryGetValue(cell, out var count))
					continue;

				if (count <= 1)
				{
					refCount.Remove(cell);
					dirty[cell] = false;
				}
				else
					refCount[cell] = count - 1;
			}
		}

		void IRenderOverlay.Render(WorldRenderer wr)
		{
			foreach (var kv in dirty)
			{
				if (kv.Value)
					render.Update(kv.Key, creepSprite, paletteReference, creepScale);
				else
					render.Clear(kv.Key);
			}

			dirty.Clear();

			render.Draw(wr.Viewport);
		}

		void INotifyActorDisposing.Disposing(Actor self)
		{
			if (disposed)
				return;

			render?.Dispose();
			disposed = true;
		}
	}

	// ---------------------------------------------------------------------------
	// Per-building trait: registers cells with CreepLayer
	// ---------------------------------------------------------------------------

	[Desc("Registers creep overlay tiles with the CreepLayer world trait.",
		"Add to structures that grant a buildable area (e.g. Hatchery, Creep Colony).")]
	public class WithCreepOverlayInfo : ConditionalTraitInfo, Requires<BuildingInfo>
	{
		[Desc("Radius in cells around the building footprint to cover with creep.")]
		public readonly int Adjacent = 3;

		public override object Create(ActorInitializer init) { return new WithCreepOverlay(init.Self, this); }
	}

	public class WithCreepOverlay : ConditionalTrait<WithCreepOverlayInfo>, INotifyAddedToWorld, INotifyRemovedFromWorld
	{
		readonly BuildingInfo bi;
		List<CPos> cells;

		public WithCreepOverlay(Actor self, WithCreepOverlayInfo info)
			: base(info)
		{
			bi = self.Info.TraitInfo<BuildingInfo>();
		}

		List<CPos> ComputeCells(Actor self)
		{
			var adjacent = Info.Adjacent;
			var location = self.Location;
			var map = self.World.Map;
			var footprintTiles = bi.Tiles(location).ToList();

			var scanStart = map.Clamp(location - new CVec(adjacent, adjacent));
			var scanEnd = map.Clamp(location + bi.Dimensions + new CVec(adjacent, adjacent));

			var radiusSq = adjacent * adjacent;
			var result = new List<CPos>();
			for (var y = scanStart.Y; y < scanEnd.Y; y++)
			{
				for (var x = scanStart.X; x < scanEnd.X; x++)
				{
					var cell = new CPos(x, y);

					// Skip cells that Zerg buildings cannot be placed on.
					if (map.Ramp[cell] != 0)
						continue;

					var terrainType = map.GetTerrainInfo(cell).Type;
					if (!bi.TerrainTypes.Contains(terrainType))
						continue;

					foreach (var ft in footprintTiles)
					{
						var dx = cell.X - ft.X;
						var dy = cell.Y - ft.Y;
						if (dx * dx + dy * dy <= radiusSq)
						{
							result.Add(cell);
							break;
						}
					}
				}
			}

			return result;
		}

		void INotifyAddedToWorld.AddedToWorld(Actor self)
		{
			cells = ComputeCells(self);
			self.World.WorldActor.Trait<CreepLayer>().AddCells(cells);
		}

		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
		{
			if (cells != null)
				self.World.WorldActor.Trait<CreepLayer>().RemoveCells(cells);
	}
	}
}
