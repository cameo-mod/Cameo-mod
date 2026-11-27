#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits
{
	// RAID-HARV-01 P3 (DEV only): the raid_harv_seat / raid_harv_signal rows of the harvester raid, one queued jsonl file per game (Logs/cameo-ai-raidharv-<game uid>.jsonl). The sim thread only admits a row and appends it to the batch
	// (AiQueuedJsonlWriter.TryWrite); the batch is handed to the shared write queue every FlushTicks and at game end (never blocks, no I/O on the tick path). Post-game, tools/ai/extract_raid_harv_signals.py turns the file into the
	// fitter's exposure records. An official build has no such trait, no file and no row.
	[TraitLocation(SystemActors.World)]
	[Desc("Development-only raid_harv_seat / raid_harv_signal row writer (RAID-HARV-01). Observation only: it writes rows handed over by the squad manager, never reads the game state.")]
	public sealed class RaidHarvLogWriterDevInfo : TraitInfo
	{
		public override object Create(ActorInitializer init) => new RaidHarvLogWriterDev();
	}

	public sealed class RaidHarvLogWriterDev : IWorldLoaded, ITick, IGameOver, INotifyActorDisposing
	{
		public const int FlushTicks = 750;

		AiQueuedJsonlWriter writer;
		int nextFlushTick;

		void IWorldLoaded.WorldLoaded(World world, WorldRenderer renderer)
		{
			if (!world.WorldActor.Trait<OpenRA.Mods.CA.Traits.CameoLogHub>().Sink.Enabled)
				return;

			if (!AiMatchLogWriter.Eligible(world.Type, world.IsReplay, world.IsLoadingGameSave, Game.IsHost))
				return;

			// The game uid is both the file name and the game_id of every row: only the id charset of the fitter (letters, digits, _ . -) is accepted.
			var uid = world.LobbyInfo.GlobalSettings.GameUid;
			if (string.IsNullOrEmpty(uid) || uid.Length > 128)
				return;

			foreach (var c in uid)
				if (!(c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9' or '_' or '.' or '-'))
					return;

			try
			{
				writer = new AiQueuedJsonlWriter($"cameo-ai-raidharv-{uid}.jsonl", "raidharv");
			}
			catch (Exception e)
			{
				writer = null;
				Log.Write("debug", "Raid rows unavailable: " + e.GetType().Name);
			}
		}

		/// <summary>Sim thread: admission and a StringBuilder append only.</summary>
		public void Write(string row)
		{
			writer?.TryWrite(row);
		}

		void ITick.Tick(Actor self)
		{
			if (writer == null || self.World.WorldTick < nextFlushTick)
				return;

			nextFlushTick = self.World.WorldTick + FlushTicks;
			writer.Flush(self.World.WorldTick);
		}

		void IGameOver.GameOver(World world)
		{
			if (writer == null)
				return;

			writer.Close(world.WorldTick);
			writer = null;
			AiLogWriteQueue.FlushSharedForGameEnd(); // bounded drain, as the sibling DEV writers do
		}

		void INotifyActorDisposing.Disposing(Actor self)
		{
			writer?.Close(self.World.WorldTick);
			writer = null; // hand-off only, never a drain
		}
	}
}
