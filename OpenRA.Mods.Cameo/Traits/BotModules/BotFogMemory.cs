#region Copyright & License Information
/*
 * Copyright (c) OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits.BotModules
{
	/// <summary>
	/// Coarse spatial value memory over the map: one bucket per region per enemy player.
	/// Values persist while the region is dark and are overwritten when it is observed again
	/// (Fransbot-style "threat map", see docs/design/AI_FRANSBOT_RESEARCH.md §2).
	/// Published read-only on each BotSituation; consumers must not mutate.
	/// </summary>
	public sealed class RegionMemory
	{
		public sealed class Region
		{
			public int ArmyValue;
			public int DefenceValue;
			public int AntiAirValue;
			public int EconomyValue;
			public int LastSeenTick;
			public bool EverSeen;
		}

		public readonly int CellSize;
		public readonly int Columns;
		public readonly int Rows;
		public readonly CPos Origin;
		readonly Dictionary<OpenRA.Player, Region[]> byEnemy = new();

		public RegionMemory(Map map, int cellSize)
			: this(map.AllCells.TopLeft, map.AllCells.BottomRight, cellSize) { }

		internal RegionMemory(CPos topLeft, CPos bottomRight, int cellSize)
		{
			CellSize = Math.Max(1, cellSize);
			Origin = topLeft;
			Columns = Math.Max(1, (bottomRight.X - Origin.X) / CellSize + 1);
			Rows = Math.Max(1, (bottomRight.Y - Origin.Y) / CellSize + 1);
		}

		public int CellCount => Columns * Rows;

		public IReadOnlyDictionary<OpenRA.Player, Region[]> ByEnemy => byEnemy;

		public int IndexOf(CPos cell)
		{
			var col = Math.Clamp((cell.X - Origin.X) / CellSize, 0, Columns - 1);
			var row = Math.Clamp((cell.Y - Origin.Y) / CellSize, 0, Rows - 1);
			return row * Columns + col;
		}

		public CPos CenterOf(int index)
		{
			var row = index / Columns;
			var col = index - row * Columns;
			return new CPos(Origin.X + col * CellSize + CellSize / 2, Origin.Y + row * CellSize + CellSize / 2);
		}

		internal void SetRegions(OpenRA.Player enemy, Region[] regions)
		{
			byEnemy[enemy] = regions;
		}

		public int KnownRegionCount(OpenRA.Player enemy)
		{
			return byEnemy.TryGetValue(enemy, out var regions) ? CountKnown(regions) : 0;
		}

		internal static int CountKnown(Region[] regions)
		{
			var count = 0;
			foreach (var region in regions)
				if (region != null && region.EverSeen)
					count++;
			return count;
		}
	}

	/// <summary>
	/// One remembered enemy actor: position, value and the classification the profile needs,
	/// captured at the last sighting. Stored per ActorID so a unit seen somewhere new moves
	/// its value instead of counting twice.
	/// </summary>
	internal sealed class ObservedActor
	{
		public uint ActorID;
		public CPos Location;
		public int LastSeenTick;
		public int Value;
		public bool Building;
		public bool BaseBuilding;
		public bool Combat;
		public bool Defence;
		public bool Tech;
		public bool Production;
		public bool Refinery;
		public bool Harvester;
		public bool Cloaked;
		public bool Aircraft;
		public bool AntiAir;
		public bool Infantry;
		public bool Vehicle;
		public bool Naval;
	}

	/// <summary>
	/// The bot player's fogged picture of the enemy: a per-enemy last-seen table fed only by
	/// what the player can actually see (live actors passing CanBeViewedByPlayer, plus the
	/// engine's FrozenActorLayer which already remembers fogged buildings).
	/// Unsynced; read and written only from IBotTick.
	/// </summary>
	internal sealed class BotFogMemory
	{
		static readonly BitSet<TargetableType> AirTargetTypes = new("Air");

		readonly OpenRA.Player viewer;
		readonly MasterAiBotModuleInfo info;
		readonly Dictionary<OpenRA.Player, Dictionary<uint, ObservedActor>> tables = new();

		public BotFogMemory(OpenRA.Player viewer, MasterAiBotModuleInfo info)
		{
			this.viewer = viewer;
			this.info = info;
		}

		/// <summary>
		/// Refresh the enemy's table from the actors of theirs visible this tick and the
		/// frozen-actor layer, then forget anything contradicted by current visibility
		/// (a remembered cell now visible without the actor) or the unit timeout.
		/// Returns the number of enemy harvesters visible right now.
		/// </summary>
		public int Observe(OpenRA.Player enemy, IEnumerable<Actor> enemyActors, int tick)
		{
			var table = Table(enemy);
			var visibleIds = new HashSet<uint>();
			var visibleHarvesters = 0;
			foreach (var actor in enemyActors)
			{
				if (!actor.Info.HasTraitInfo<IOccupySpaceInfo>() || !actor.CanBeViewedByPlayer(viewer))
					continue;

				visibleIds.Add(actor.ActorID);
				var record = Classify(actor.Info, actor.ActorID, actor.Location, actor.GetEnabledTargetTypes(), tick, info);
				record.Cloaked = actor.Info.HasTraitInfo<CloakInfo>();
				table[actor.ActorID] = record;
				if (record.Harvester)
					visibleHarvesters++;
			}

			var frozen = viewer.FrozenActorLayer;
			if (frozen != null)
			{
				foreach (var fa in frozen.FrozenActorsInRegion(viewer.World.Map.AllCells))
				{
					if (!fa.IsValid || fa.Owner != enemy || table.ContainsKey(fa.ID))
						continue;

					var record = Classify(fa.Info, fa.ID, viewer.World.Map.CellContaining(fa.CenterPosition), fa.TargetTypes, tick, info);
					record.Cloaked = fa.Info.HasTraitInfo<CloakInfo>();
					table[fa.ID] = record;
				}
			}

			var shroud = viewer.Shroud;
			List<uint> forgotten = null;
			foreach (var pair in table)
			{
				if (visibleIds.Contains(pair.Key))
					continue;

				var record = pair.Value;
				if (shroud.IsVisible(record.Location) ||
					(!record.Building && tick - record.LastSeenTick > info.ObservationTimeoutTicks))
				{
					forgotten ??= new List<uint>();
					forgotten.Add(pair.Key);
				}
			}

			if (forgotten != null)
				foreach (var id in forgotten)
					table.Remove(id);

			return visibleHarvesters;
		}

		public IReadOnlyCollection<ObservedActor> Remembered(OpenRA.Player enemy)
		{
			return Table(enemy).Values;
		}

		Dictionary<uint, ObservedActor> Table(OpenRA.Player enemy)
		{
			if (!tables.TryGetValue(enemy, out var table))
				tables[enemy] = table = new Dictionary<uint, ObservedActor>();
			return table;
		}

		internal static ObservedActor Classify(ActorInfo actorInfo, uint actorID, CPos location,
			BitSet<TargetableType> targetTypes, int tick, MasterAiBotModuleInfo info)
		{
			var building = actorInfo.HasTraitInfo<BuildingInfo>();
			var hasAttack = actorInfo.HasTraitInfo<AttackBaseInfo>();
			return new ObservedActor
			{
				ActorID = actorID,
				Location = location,
				LastSeenTick = tick,
				Value = actorInfo.TraitInfoOrDefault<ValuedInfo>()?.Cost ?? 0,
				Building = building,
				BaseBuilding = building && actorInfo.HasTraitInfo<BaseBuildingInfo>(),
				Combat = hasAttack && !building && !actorInfo.HasTraitInfo<HarvesterInfo>(),
				Defence = building && (hasAttack || targetTypes.Overlaps(info.DefenceTargetTypes)),
				Tech = building && actorInfo.HasTraitInfo<ProvidesPrerequisiteInfo>(),
				Production = building && actorInfo.HasTraitInfo<ProductionInfo>(),
				Refinery = building && actorInfo.HasTraitInfo<RefineryInfo>(),
				Harvester = actorInfo.HasTraitInfo<HarvesterInfo>(),
				Aircraft = actorInfo.HasTraitInfo<AircraftInfo>(),
				AntiAir = HasAntiAirWeapon(actorInfo),
				Infantry = targetTypes.Overlaps(info.InfantryTargetTypes),
				Vehicle = targetTypes.Overlaps(info.VehicleTargetTypes),
				Naval = targetTypes.Overlaps(info.NavalTargetTypes)
			};
		}

		static bool HasAntiAirWeapon(ActorInfo actorInfo)
		{
			foreach (var armament in actorInfo.TraitInfos<ArmamentInfo>())
				if (armament.WeaponInfo != null && armament.WeaponInfo.IsValidTarget(AirTargetTypes))
					return true;
			return false;
		}
	}
}
