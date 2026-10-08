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
using OpenRA.Mods.Cameo.Traits.BotModules;

namespace OpenRA.Mods.Cameo.Test.TestFixtures
{
	/// <summary>
	/// Engine-free stand-in for TacticalMapBotModule: a fixed cell-to-zone map
	/// plus an explicit adjacency list. <see cref="NearestSearchRadius"/> controls
	/// the nearest-region contract: 0 resolves only cells inside a zone
	/// (InfluenceLayers' exact semantics); the default 3 reproduces
	/// ZoneRegionMemory's expanding ring (own id first, then a small expanding
	/// scan, then -1).
	/// </summary>
	public sealed class FakeZoneTopology : IBotZoneTopology
	{
		readonly Dictionary<CPos, int> idByCell = new();

		public readonly List<Zone> ZoneList = new();
		public int Generation { get; set; }
		public int NearestSearchRadius { get; set; } = 3;

		public IReadOnlyList<Zone> Regions => ZoneList;
		public IReadOnlyList<ZoneChokepoint> Chokepoints => Array.Empty<ZoneChokepoint>();
		public IReadOnlyList<ZoneTerritoryDoor> TerritoryDoors => Array.Empty<ZoneTerritoryDoor>();
		public IReadOnlyCollection<CPos> Territory => Array.Empty<CPos>();

		public int RegionIdAt(CPos cell) => idByCell.GetValueOrDefault(cell, -1);

		public int NearestRegionId(CPos cell)
		{
			var id = RegionIdAt(cell);
			if (id >= 0)
				return id;

			for (var radius = 1; radius <= NearestSearchRadius; radius++)
			{
				for (var dx = -radius; dx <= radius; dx++)
				{
					for (var dy = -radius; dy <= radius; dy++)
					{
						if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != radius)
							continue;

						id = RegionIdAt(cell + new CVec(dx, dy));
						if (id >= 0)
							return id;
					}
				}
			}

			return -1;
		}

		public bool IsPassableCell(CPos cell) => idByCell.ContainsKey(cell);
		public Player RegionOwner(int regionId) => null;
		public bool IsInTerritory(CPos cell) => false;

		public void AddZone(IEnumerable<CPos> cells, int[] adjacent = null, int resourceCells = 0)
		{
			var id = ZoneList.Count;
			var cellArray = cells.ToArray();
			ZoneList.Add(new Zone(id, cellArray, [], adjacent ?? [], resourceCells, cellArray.Length, []));
			foreach (var cell in cellArray)
				idByCell[cell] = id;
		}

		// A re-cut: the shared Regions list is refilled in place and every zone id
		// re-derived, exactly like the module's bridge-change rebuild.
		public void Recut()
		{
			ZoneList.Clear();
			idByCell.Clear();
		}
	}
}
