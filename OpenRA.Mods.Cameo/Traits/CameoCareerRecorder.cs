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
	[TraitLocation(SystemActors.World)]
	[Desc("Records the local human player's final win or loss in the persistent Cameo career.",
		"Place on the world actor.")]
	public class CameoCareerRecorderInfo : TraitInfo
	{
		public override object Create(ActorInitializer init) { return new CameoCareerRecorder(); }
	}

	public class CameoCareerRecorder : ITick
	{
		bool recorded;

		void ITick.Tick(Actor self)
		{
			if (recorded)
				return;

			var world = self.World;
			var player = world.LocalPlayer;
			if (world.Type != WorldType.Regular || world.IsReplay || player == null ||
				player.Spectating || player.NonCombatant || player.IsBot)
				return;

			var outcome = player.WinState switch
			{
				WinState.Won => "Won",
				WinState.Lost => "Lost",
				_ => null
			};
			if (outcome == null)
				return;

			var stats = player.PlayerActor.TraitOrDefault<PlayerStatistics>();
			if (stats == null)
				return;

			// Guard before persistence so the final outcome can only create one match in this world.
			recorded = true;
			var resources = player.PlayerActor.TraitOrDefault<PlayerResources>();
			var gameUid = world.LobbyInfo.GlobalSettings.GameUid ?? "";
			var recordId = !string.IsNullOrEmpty(gameUid) ? gameUid : Guid.NewGuid().ToString("D");

			new CameoCareerRepository(Platform.SupportDir).Append(recordId, new CareerMatchRecord
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
			});
		}
	}
}
