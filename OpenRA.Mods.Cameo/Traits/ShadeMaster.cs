#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using OpenRA.Activities;
using OpenRA.Mods.AS.Activities;
using OpenRA.Mods.AS.Traits;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits
{
	[Desc("Teleports to the slave if it is killed.")]
	public class ShadeMasterInfo : BaseSpawnerMasterInfo
	{
		[GrantedConditionReference]
		[Desc("The condition to grant to self while spawned units are loaded.",
			"Condition can stack with multiple spawns.")]
		public readonly string LoadedCondition = null;

		[Desc("Conditions to grant when specified actors are contained inside the transport.",
			"A dictionary of [actor id]: [condition].")]
		public readonly Dictionary<string, string> SpawnContainConditions = new();

		[Desc("This trait lose all energy when the teleport use this teleport type,",
			"and only trigger teleport effect of this teleport type")]
		public readonly string TeleportType = "RA2ChronoPower";

		[Desc("Max distance when destination is unavaliable for this actor")]
		public readonly int MaxSearchCellDistance = 4;

		[GrantedConditionReference]
		public IEnumerable<string> LinterSpawnContainConditions { get { return SpawnContainConditions.Values; } }

		public override void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			base.RulesetLoaded(rules, ai);

			if (Actors == null || Actors.Length == 0)
				throw new YamlException($"Actors is null or empty for DroneSpawner for actor type {ai.Name}!");

			if (InitialActorCount > Actors.Length || InitialActorCount < -1)
				throw new YamlException("DroneSpawner can't have more InitialActorCount than the actors defined!");
		}

		public override object Create(ActorInitializer init) { return new ShadeMaster(init, this); }
	}

	public class ShadeMaster : BaseSpawnerMaster, INotifyOwnerChanged, ITick
	{
		class ShadeSlaveEntry : BaseSpawnerSlaveEntry
		{
			public new ShadeSlave SpawnerSlave;
			public CVec GatherOffsetCell = CVec.Zero;
		}

		public new ShadeMasterInfo Info { get; }

		ShadeSlaveEntry[] slaveEntries;
		int spawnReplaceTicks;

		readonly Dictionary<string, Stack<int>> spawnContainTokens = new();
		readonly Stack<int> loadedTokens = new();

		public ShadeMaster(ActorInitializer init, ShadeMasterInfo info)
			: base(init, info)
		{
			Info = info;
		}

		public override BaseSpawnerSlaveEntry[] CreateSlaveEntries(BaseSpawnerMasterInfo info)
		{
			slaveEntries = new ShadeSlaveEntry[info.Actors.Length]; // For this class to use

			for (var i = 0; i < slaveEntries.Length; i++)
				slaveEntries[i] = new ShadeSlaveEntry();

			return slaveEntries; // For the base class to use
		}

		public override void InitializeSlaveEntry(Actor slave, BaseSpawnerSlaveEntry entry)
		{
			var se = entry as ShadeSlaveEntry;
			base.InitializeSlaveEntry(slave, se);

			se.SpawnerSlave = slave.Trait<ShadeSlave>();
		}

		void ITick.Tick(Actor self)
		{
			if (!self.IsInWorld)
				return;

			// Time to respawn something.
			if (!IsTraitPaused)
			{
				if (spawnReplaceTicks < 0)
				{
					// If there's something left to spawn, restart the timer.
					if (SelectEntryToSpawn(slaveEntries) != null)
						spawnReplaceTicks = Info.RespawnTicks;
				}
				else if (spawnReplaceTicks == 0)
				{
					Replenish(self, slaveEntries);
					SpawnReplenishedSlaves(self);
					spawnReplaceTicks--;
				}
				else
					spawnReplaceTicks--;
			}
		}

		void SpawnReplenishedSlaves(Actor self)
		{
			foreach (var se in slaveEntries)
				if (se.IsValid && !se.Actor.IsInWorld)
				{
					SpawnIntoWorld(self, se.Actor, self.CenterPosition + se.Offset.Rotate(self.Orientation));
				}
		}

		public override void OnSlaveKilled(Actor self, Actor slave)
		{
			if (spawnContainTokens.TryGetValue(slave.Info.Name, out var spawnContainToken) && spawnContainToken.Count > 0)
				self.RevokeCondition(spawnContainToken.Pop());

			if (loadedTokens.Count > 0 && Info.LoadedCondition != null)
				self.RevokeCondition(loadedTokens.Pop());
		}

		public override void SpawnIntoWorld(Actor self, Actor slave, WPos centerPosition)
		{
			var exit = self.RandomExitOrDefault(self.World, null);
			SetSpawnedFacing(slave, null);

			self.World.AddFrameEndTask(w =>
			{
				if (self.IsDead)
					return;

				var spawnOffset = exit == null ? WVec.Zero : exit.Info.SpawnOffset;
				var positionable = slave.Trait<IPositionable>();
				positionable.SetPosition(slave, centerPosition + spawnOffset.Rotate(self.Orientation));
				positionable.SetCenterPosition(slave, centerPosition + spawnOffset.Rotate(self.Orientation));

				var location = self.World.Map.CellContaining(centerPosition + spawnOffset.Rotate(self.Orientation));

				var mv = slave.Trait<IMove>();

				slave.QueueActivity(mv.MoveTo(location, 0));

				w.Add(slave);

				if (Info.SpawnContainConditions.TryGetValue(slave.Info.Name, out var spawnContainCondition))
					spawnContainTokens.GetOrAdd(slave.Info.Name).Push(self.GrantCondition(spawnContainCondition));

				if (!string.IsNullOrEmpty(Info.LoadedCondition))
					loadedTokens.Push(self.GrantCondition(Info.LoadedCondition));
			});
		}

		public void TeleportToSlave(Actor self, Actor slave)
		{
			var directDestination = slave.Location;

			self.QueueActivity(false,
					new RA2Teleport(
						self, Info.TeleportType, directDestination, [directDestination], Info.MaxSearchCellDistance));
		}

		protected override void TraitResumed(Actor self) { spawnReplaceTicks = 0; }
	}
}
