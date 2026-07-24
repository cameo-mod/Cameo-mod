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
using System.Globalization;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits
{
	[TraitLocation(SystemActors.Player)]
	[Desc("Records the local human player's notified win or loss in the persistent Cameo career.",
		"Place on the player actor.")]
	public class CameoCareerRecorderInfo : TraitInfo
	{
		public override object Create(ActorInitializer init) { return new CameoCareerRecorder(); }
	}

	public class CameoCareerRecorder : ITick, INotifyWinStateChanged
	{
		bool recorded;
		bool outcomeNotified;
		bool captureScheduled;
		int retryCount;
		int nextAttemptTick;
		PendingCameoCareerMatch pending;

		void ITick.Tick(Actor self)
		{
			var world = self.World;
			if (recorded || !outcomeNotified || self != world.LocalPlayer?.PlayerActor ||
				world.WorldTick < nextAttemptTick)
				return;

			pending ??= Capture(world);
			if (pending == null)
				return;

			TryPersist(world.WorldTick);
		}

		void INotifyWinStateChanged.OnPlayerWon(OpenRA.Player player)
		{
			ScheduleCapture(player);
		}

		void INotifyWinStateChanged.OnPlayerLost(OpenRA.Player player)
		{
			ScheduleCapture(player);
		}

		void ScheduleCapture(OpenRA.Player player)
		{
			if (player != player.World.LocalPlayer || recorded)
				return;

			outcomeNotified = true;
			if (captureScheduled)
				return;

			captureScheduled = true;
			player.World.AddFrameEndTask(world =>
			{
				captureScheduled = false;
				if (recorded)
					return;

				pending ??= Capture(world);
				if (pending != null)
					TryPersist(world.WorldTick);
			});
		}

		internal void FinalizeMatch(World world)
		{
			if (recorded)
				return;

			pending ??= Capture(world);
			for (var i = 0; pending != null && i < 5 && !recorded; i++)
				TryPersist(world.WorldTick);
		}

		void TryPersist(int worldTick)
		{
			var result = pending.TryAppend();
			recorded = pending.IsTerminal;
			if (!recorded && result == CameoCareerAppendResult.RetryableFailure)
			{
				retryCount++;
				nextAttemptTick = worldTick + Math.Min(1 << Math.Min(retryCount - 1, 5), 30);
			}
		}

		static PendingCameoCareerMatch Capture(World world)
		{
			var player = world.LocalPlayer;
			if (world.Type != WorldType.Regular || world.IsReplay || player == null ||
				player.Spectating || player.NonCombatant || player.IsBot)
				return null;

			var outcome = FinalOutcome(player.WinState);
			if (outcome == null)
				return null;

			var stats = player.PlayerActor.TraitOrDefault<PlayerStatistics>();
			if (stats == null)
				return null;

			var resources = player.PlayerActor.TraitOrDefault<PlayerResources>();
			var gameUid = world.LobbyInfo.GlobalSettings.GameUid ?? "";
			var recordId = !string.IsNullOrEmpty(gameUid) ? gameUid : Guid.NewGuid().ToString("D");

			var match = new CareerMatchRecord
			{
				RecordedUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
				Outcome = outcome,
				Faction = player.Faction.InternalName,
				GameUid = gameUid,
				MapUid = world.Map.Uid,
				MapTitle = world.Map.Title,
				ModVersion = Game.ModData.Manifest.Metadata.Version,
				DurationTicks = world.WorldTick,
				GameTimestep = world.Timestep,
				UnitsKilled = stats.UnitsKilled,
				BuildingsKilled = stats.BuildingsKilled,
				UnitsLost = stats.UnitsDead,
				BuildingsLost = stats.BuildingsDead,
				ResourcesEarned = resources?.Earned ?? 0,
				ResourcesSpent = resources?.Spent ?? 0,
				EnemyAssetsDestroyed = stats.KillsCost,
				AssetsOwned = stats.AssetsValue
			};

			return new PendingCameoCareerMatch(new CameoCareerRepository(Platform.SupportDir), recordId, match);
		}

		internal static string FinalOutcome(WinState winState)
		{
			return winState switch
			{
				WinState.Won => "Won",
				WinState.Lost => "Lost",
				_ => null
			};
		}
	}

	[TraitLocation(SystemActors.World)]
	[Desc("Flushes the local player's pending Cameo career result when the game ends.",
		"Place on the world actor.")]
	public class CameoCareerFinalizerInfo : TraitInfo
	{
		public override object Create(ActorInitializer init) { return new CameoCareerFinalizer(); }
	}

	public class CameoCareerFinalizer : IGameOver
	{
		void IGameOver.GameOver(World world)
		{
			world.LocalPlayer?.PlayerActor.TraitOrDefault<CameoCareerRecorder>()?.FinalizeMatch(world);
		}
	}
}
