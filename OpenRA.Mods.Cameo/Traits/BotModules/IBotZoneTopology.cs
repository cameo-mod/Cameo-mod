#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 *
 * Written for the ZG zone-graph port (crystallized-nexus CNTacticalMapBotModule,
 * GPLv3, Copyright (c) The Crystallized Nexus Developers).
 */
#endregion

using System.Collections.Generic;

namespace OpenRA.Mods.Cameo.Traits.BotModules
{
	/// <summary>
	/// What kind of terrain feature a <see cref="ZoneChokepoint"/> marks. First letter is the
	/// overlay label (P/R/B), matching the donor's convention.
	/// </summary>
	public enum ZoneChokepointType { Passage, Ramp, Bridge }

	/// <summary>
	/// One detected access point in the static topology: the representative cell, the
	/// pathfinder abstract domain it sits in, its kind and a base defensive weight.
	/// (Port of the donor's CNChokepoint.)
	/// </summary>
	public sealed class ZoneChokepoint
	{
		public readonly CPos Cell;
		public readonly uint Domain;
		public readonly ZoneChokepointType Type;
		public readonly int BaseWeight;

		public ZoneChokepoint(CPos cell, uint domain, ZoneChokepointType type, int baseWeight)
		{
			Cell = cell;
			Domain = domain;
			Type = type;
			BaseWeight = baseWeight;
		}
	}

	/// <summary>
	/// A resolved wall-to-wall corridor through a chokepoint: the full passable width the
	/// region cut is made along. <see cref="Zone.GateCorridorIndices"/> indexes into the
	/// module's corridor list. (Port of the donor's CNSealableCorridor; renamed because the
	/// "sealable" use — can it be plugged by one wall line — is ZG-b territory logic, while
	/// the corridor itself is a terrain fact.)
	/// </summary>
	public sealed class ZoneGateCorridor
	{
		public readonly CPos Center;

		/// <summary>True when the wall line sealing this corridor runs along the X axis (cells vary in X).</summary>
		public readonly bool WallRunsHorizontal;

		public readonly CPos[] Cells;

		public int Width => Cells.Length;

		public ZoneGateCorridor(CPos center, bool wallRunsHorizontal, CPos[] cells)
		{
			Center = center;
			WallRunsHorizontal = wallRunsHorizontal;
			Cells = cells;
		}
	}

	/// <summary>
	/// One cell of the map's shape: a connected pocket of ground bounded by chokepoint
	/// corridors and cliff ramps, independent of who (if anyone) holds it. Computed once,
	/// shared between bots via the static registry in TacticalMapBotModule.
	/// (Port of the donor's CNRegion.)
	/// </summary>
	public sealed class Zone
	{
		/// <summary>Position of this zone in <see cref="IBotZoneTopology.Regions"/>.</summary>
		public readonly int Id;

		public readonly CPos[] Cells;

		/// <summary>Indices into the module's gate-corridor list for the gates bounding this zone.</summary>
		public readonly int[] GateCorridorIndices;

		/// <summary>Ids of zones sharing a barrier cell with this one.</summary>
		public readonly int[] AdjacentRegionIds;

		/// <summary>Cells carrying a resource type - a rough size of what this zone's ground is worth.</summary>
		public readonly int ResourceCellCount;

		/// <summary>Passable, non-ramp cells - a rough size of how much of this zone can be built on.</summary>
		public readonly int BuildableCellCount;

		/// <summary>Cells with a non-zone neighbour (a gate, a wall, the map edge) - the outline, for drawing.</summary>
		public readonly CPos[] BoundaryCells;

		public int Size => Cells.Length;

		public Zone(int id, CPos[] cells, int[] gateCorridorIndices, int[] adjacentRegionIds,
			int resourceCellCount, int buildableCellCount, CPos[] boundaryCells)
		{
			Id = id;
			Cells = cells;
			GateCorridorIndices = gateCorridorIndices;
			AdjacentRegionIds = adjacentRegionIds;
			ResourceCellCount = resourceCellCount;
			BuildableCellCount = buildableCellCount;
			BoundaryCells = boundaryCells;
		}
	}

	/// <summary>
	/// The static terrain topology every bot reads: the map's passable ground cut into
	/// <see cref="Zone"/> regions by the chokepoint corridors (plus cliff ramps) that gate it.
	/// <para>
	/// Terrain-only and fog-honest by construction: derived from the map's tiles, heights and the
	/// hierarchical pathfinder's connectivity domains, never from enumerating actors. Implemented
	/// by <c>TacticalMapBotModule</c>, built once per (world, locomotor) and shared between bots.
	/// </para>
	/// <para>
	/// Consumers are the ZG-c stack (<c>BotFogMemory</c>, <c>RegionRouter</c>, <c>BotSituation</c>):
	/// they locate the module via <c>player.PlayerActor.Trait&lt;TacticalMapBotModule&gt;()</c>.
	/// On a non-host client (or a replay) the topology is never built and every answer is empty.
	/// </para>
	/// </summary>
	public interface IBotZoneTopology
	{
		/// <summary>
		/// The map's regions — connected pockets of passable ground bounded by chokepoint corridors
		/// and cliff ramps. Unowned terrain facts; ids are positions in this list and are invalidated
		/// whenever <see cref="Generation"/> changes (a bridge re-cut reshuffles them).
		/// </summary>
		IReadOnlyList<Zone> Regions { get; }

		/// <summary>The detected chokepoints (passages, cliff ramps, bridges) that gate the regions.</summary>
		IReadOnlyList<ZoneChokepoint> Chokepoints { get; }

		/// <summary>Region id at a cell, or -1 if it is a gate/barrier cell or outside any region.</summary>
		int RegionIdAt(CPos cell);

		/// <summary>
		/// The generation this bot has ADOPTED, bumped whenever the shared topology is re-cut
		/// (a destroyed or repaired bridge). Anything cached per region id must be dropped when this
		/// changes — after a re-cut nothing says id 4 is the same ground it was. -1 while unbuilt.
		/// </summary>
		int Generation { get; }

		/// <summary>
		/// Whether ground units can cross a cell, by the same locomotor the chokepoints and regions
		/// were scanned with — so a caller walks the map the way these answers were derived.
		/// </summary>
		bool IsPassableCell(CPos cell);
	}
}
