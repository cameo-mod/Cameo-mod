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

	public class CreepLayer : IRenderOverlay, IWorldLoaded, INotifyActorDisposing, ITick
	{
		readonly CreepLayerInfo info;
		readonly World world;

		// How many buildings cover each cell.
		readonly Dictionary<CPos, int> refCount = new();

		// Sprite layer dirty tracking (true = draw, false = clear).
		readonly Dictionary<CPos, bool> dirty = new();
		readonly Queue<CPos> cleanDirty = new();

		// Ring-removal events scheduled for a future tick by RemovedFromWorld.
		readonly List<(IEnumerable<CPos> Cells, long StartTick)> pendingRemovals = new();

		long currentTick;
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

		// Called by WithCreepOverlay.RemovedFromWorld to peel rings off over time.
		// rings should already be ordered outermost-first for reverse-spread.
		public void ScheduleRemoveRings(List<List<CPos>> rings, int ticksBetweenRings)
		{
			for (var i = 0; i < rings.Count; i++)
				pendingRemovals.Add((rings[i], currentTick + (long)(i * ticksBetweenRings)));
		}

		void ITick.Tick(Actor self)
		{
			currentTick++;

			for (var i = pendingRemovals.Count - 1; i >= 0; i--)
			{
				if (pendingRemovals[i].StartTick <= currentTick)
				{
					RemoveCells(pendingRemovals[i].Cells);
					pendingRemovals.RemoveAt(i);
				}
			}
		}

		void IRenderOverlay.Render(WorldRenderer wr)
		{
			foreach (var kv in dirty)
			{
				if (world.FogObscures(kv.Key))
					continue;

				if (kv.Value)
					render.Update(kv.Key, creepSprite, paletteReference, creepScale);
				else
					render.Clear(kv.Key);

				cleanDirty.Enqueue(kv.Key);
			}

			while (cleanDirty.Count > 0)
				dirty.Remove(cleanDirty.Dequeue());

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
	// Per-building trait: registers cells with CreepLayer ring by ring
	// ---------------------------------------------------------------------------

	[Desc("Registers creep overlay tiles with the CreepLayer world trait.",
		"Add to structures that grant a buildable area (e.g. Hatchery, Creep Colony).")]
	public class WithCreepOverlayInfo : ConditionalTraitInfo, Requires<BuildingInfo>
	{
		[Desc("Radius in cells around the building footprint to cover with creep.")]
		public readonly int Adjacent = 3;

		[Desc("Minimum ticks to wait before adding each successive ring. 0 = all rings added at once.")]
		public readonly int MinSpreadInterval = 10;

		[Desc("Maximum ticks to wait before adding each successive ring.")]
		public readonly int MaxSpreadInterval = 50;

		public override object Create(ActorInitializer init) { return new WithCreepOverlay(init.Self, this); }
	}

	public class WithCreepOverlay : ConditionalTrait<WithCreepOverlayInfo>, INotifyAddedToWorld, INotifyRemovedFromWorld, ITick
	{
		readonly BuildingInfo bi;
		readonly Random random = new();
		List<List<CPos>> rings;
		int ringsAdded;
		int nextRingIn;

		public WithCreepOverlay(Actor self, WithCreepOverlayInfo info)
			: base(info)
		{
			bi = self.Info.TraitInfo<BuildingInfo>();
		}

		// Groups eligible cells into concentric rings by integer distance from the footprint.
		List<List<CPos>> ComputeRings(Actor self)
		{
			var adjacent = Info.Adjacent;
			var location = self.Location;
			var map = self.World.Map;
			var footprintTiles = bi.Tiles(location).ToList();

			var scanStart = map.Clamp(location - new CVec(adjacent, adjacent));
			var scanEnd = map.Clamp(location + bi.Dimensions + new CVec(adjacent, adjacent));

			var radiusSq = adjacent * adjacent;
			var cellsByRing = new Dictionary<int, List<CPos>>();

			for (var y = scanStart.Y; y < scanEnd.Y; y++)
			{
				for (var x = scanStart.X; x < scanEnd.X; x++)
				{
					var cell = new CPos(x, y);

					if (map.Ramp[cell] != 0)
						continue;

					// Use the raw tile terrain type, ignoring CustomTerrain (set by resources like ore/gems).
					// Resources are temporary and should not block creep; cliffs/water are permanent and should.
					var tileInfo = map.Rules.TerrainInfo.GetTerrainInfo(map.Tiles[cell.ToMPos(map)]);
					var rawTerrainType = map.Rules.TerrainInfo.TerrainTypes[tileInfo.TerrainType].Type;
					if (!bi.TerrainTypes.Contains(rawTerrainType))
						continue;

					var minDistSq = int.MaxValue;
					foreach (var ft in footprintTiles)
					{
						var dx = cell.X - ft.X;
						var dy = cell.Y - ft.Y;
						var dSq = dx * dx + dy * dy;
						if (dSq < minDistSq)
							minDistSq = dSq;
					}

					if (minDistSq > radiusSq)
						continue;

					var ring = (int)Math.Sqrt(minDistSq);
					if (!cellsByRing.TryGetValue(ring, out var list))
						cellsByRing[ring] = list = new List<CPos>();
					list.Add(cell);
				}
			}

			return cellsByRing.OrderBy(kv => kv.Key).Select(kv => kv.Value).ToList();
		}

		void INotifyAddedToWorld.AddedToWorld(Actor self)
		{
			rings = ComputeRings(self);
			ringsAdded = 0;
			nextRingIn = 0;

			var creepLayer = self.World.WorldActor.Trait<CreepLayer>();

			if (Info.MinSpreadInterval <= 0)
			{
				foreach (var ring in rings)
					creepLayer.AddCells(ring);
				ringsAdded = rings.Count;
			}
			else if (rings.Count > 0)
			{
				// The innermost ring (footprint + immediate neighbours) appears right away.
				creepLayer.AddCells(rings[0]);
				ringsAdded = 1;
				nextRingIn = random.Next(Info.MinSpreadInterval, Info.MaxSpreadInterval + 1);
			}
		}

		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
		{
			if (rings == null || ringsAdded == 0)
				return;

			// Reverse the rings so the outermost disappears first.
			var addedRings = rings.Take(ringsAdded).Reverse().ToList();
			self.World.WorldActor.Trait<CreepLayer>().ScheduleRemoveRings(addedRings, Info.MinSpreadInterval);
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled || rings == null || ringsAdded >= rings.Count || Info.MinSpreadInterval <= 0)
				return;

			if (--nextRingIn > 0)
				return;

			self.World.WorldActor.Trait<CreepLayer>().AddCells(rings[ringsAdded++]);
			if (ringsAdded < rings.Count)
				nextRingIn = random.Next(Info.MinSpreadInterval, Info.MaxSpreadInterval + 1);
		}
	}
}
