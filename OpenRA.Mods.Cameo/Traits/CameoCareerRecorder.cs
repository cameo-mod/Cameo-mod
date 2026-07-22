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
		int retryCount;
		int nextAttemptTick;
		PendingCameoCareerMatch pending;

		void ITick.Tick(Actor self)
		{
			if (recorded || self.World.WorldTick < nextAttemptTick)
				return;

			if (pending == null)
				return;

			TryPersist(self.World.WorldTick);
		}

		void INotifyWinStateChanged.OnPlayerWon(OpenRA.Player player)
		{
			CaptureAndPersist(player, "Won");
		}

		void INotifyWinStateChanged.OnPlayerLost(OpenRA.Player player)
		{
			CaptureAndPersist(player, "Lost");
		}

		void CaptureAndPersist(OpenRA.Player player, string outcome)
		{
			if (recorded || pending != null)
				return;

			pending = Capture(player, outcome);
			if (pending != null)
				TryPersist(player.World.WorldTick);
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

		static PendingCameoCareerMatch Capture(OpenRA.Player player, string outcome)
		{
			var world = player.World;
			if (world.Type != WorldType.Regular || world.IsReplay ||
				player != world.LocalPlayer || player.Spectating || player.NonCombatant || player.IsBot)
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
				UnitsKilled = stats.UnitsKilled,
				BuildingsKilled = stats.BuildingsKilled,
				UnitsLost = stats.UnitsDead,
				BuildingsLost = stats.BuildingsDead,
				ResourcesEarned = resources?.Earned ?? 0,
				ResourcesSpent = resources?.Spent ?? 0
			};

			return new PendingCameoCareerMatch(new CameoCareerRepository(Platform.SupportDir), recordId, match);
		}
	}
}
