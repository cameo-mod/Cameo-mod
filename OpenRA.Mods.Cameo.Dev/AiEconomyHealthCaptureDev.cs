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
using System.IO;
using System.Text;
using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits
{
	[TraitLocation(SystemActors.World)]
	[Desc("Development-only economy health capture.")]
	public sealed class AiEconomyHealthCaptureDevInfo : TraitInfo
	{
		public override object Create(ActorInitializer init) => new AiEconomyHealthCaptureDev();
	}

	public sealed class AiEconomyHealthCaptureDev : IWorldLoaded, ITick, IGameOver, INotifyActorDisposing
	{
		const int MaximumPlayers = 64;
		readonly List<AiEconomyHealthRecorderDev> recorders = [];
		// PERF-SPK-01: batches are handed to the shared queue every FlushEveryTicks ticks (enqueue-only, never blocks).
		const int FlushEveryTicks = 150;
		AiQueuedJsonlWriter health;
		AiQueuedJsonlWriter raw;
		int tickCount;
		bool ended;

		void IWorldLoaded.WorldLoaded(World world, WorldRenderer renderer)
		{
			if (!world.WorldActor.Trait<OpenRA.Mods.CA.Traits.CameoLogHub>().Sink.Enabled)
				return;

			if (!AiMatchLogWriter.Eligible(world.Type, world.IsReplay, world.IsLoadingGameSave, Game.IsHost))
				return;
			var uid = world.LobbyInfo.GlobalSettings.GameUid;
			try
			{
				var directory = Path.Combine(Platform.SupportDir, "Logs");
				Directory.CreateDirectory(directory);
				health = new AiQueuedJsonlWriter(directory, "cameo-ai-economy-health.jsonl", "economy-health");
				raw = new AiQueuedJsonlWriter(directory, "cameo-ai-economy-raw.jsonl", "economy-raw");
			}
			catch (Exception e) when (e is IOException or UnauthorizedAccessException)
			{
				health = null;
				raw = null;
				Log.Write("debug", "Economy health capture unavailable: " + e.GetType().Name);
				return;
			}

			foreach (var player in world.Players)
			{
				if (!AiMatchLogWriter.IsLoggableBot(player))
					continue;
				var recorder = player.PlayerActor.TraitOrDefault<AiEconomyHealthRecorderDev>();
				if (recorder == null || recorders.Count == MaximumPlayers)
					continue;
				recorder.Activate(uid, health, raw);
				recorders.Add(recorder);
				recorder.Observe(world.WorldTick);
			}
		}

		void ITick.Tick(Actor self)
		{
			if (ended)
				return;
			foreach (var recorder in recorders)
				recorder.Observe(self.World.WorldTick);
			if (++tickCount % FlushEveryTicks == 0)
			{
				health?.Flush(self.World.WorldTick);
				raw?.Flush(self.World.WorldTick);
			}
		}

		void IGameOver.GameOver(World world)
		{
			if (ended)
				return;
			ended = true;
			// PERF-SPK-01: drain and check before any terminal row, so a worker failure seen here reads as incomplete
			// (the recorders read Complete while they write the end record).
			health?.BeginTerminal(world.WorldTick);
			raw?.BeginTerminal(world.WorldTick);
			foreach (var recorder in recorders)
				recorder.Observe(world.WorldTick, terminal: true);
			health?.EndTerminal(world.WorldTick);
			raw?.EndTerminal(world.WorldTick);
			AiLogWriteQueue.FlushSharedForGameEnd();
		}

		void INotifyActorDisposing.Disposing(Actor self)
		{
			// Leaving a world early closes handles without inventing an end-of-match watermark. Hand-off only, never a drain.
			ended = true;
			health?.Close(self.World.WorldTick);
			raw?.Close(self.World.WorldTick);
		}
	}

}
