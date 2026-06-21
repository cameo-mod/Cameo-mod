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

using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits
{
	[TraitLocation(SystemActors.World)]
	[Desc("Folds the local player's end-of-game statistics into the persistent, per-faction",
		"statistics file shown in the main-menu Statistics window. Place on the world actor.")]
	public class StatisticsRecorderInfo : TraitInfo
	{
		public override object Create(ActorInitializer init) { return new StatisticsRecorder(); }
	}

	public class StatisticsRecorder : IGameOver
	{
		bool recorded;

		void IGameOver.GameOver(World world)
		{
			// GameOver can only fire once per world, but guard anyway.
			if (recorded)
				return;

			recorded = true;

			// Only completed games with a real local player are counted. Replays are watched,
			// not played; a dedicated server has no local player. In multiplayer each client
			// records its own player, which is exactly what we want for local statistics.
			if (world.IsReplay)
				return;

			var player = world.LocalPlayer;
			if (player == null || player.Spectating || player.NonCombatant || player.IsBot)
				return;

			var stats = player.PlayerActor.TraitOrDefault<PlayerStatistics>();
			if (stats == null)
				return;

			var resources = player.PlayerActor.TraitOrDefault<PlayerResources>();
			var won = player.WinState == WinState.Won;

			CameoStatistics.Record(
				player.Faction.InternalName,
				won,
				stats.UnitsKilled,
				stats.BuildingsKilled,
				stats.UnitsDead,
				stats.BuildingsDead,
				resources?.Earned ?? 0,
				resources?.Spent ?? 0);
		}
	}
}
