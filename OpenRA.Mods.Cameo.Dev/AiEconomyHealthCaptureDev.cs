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
		AiEconomyHealthLogWriter health;
		AiEconomyHealthLogWriter raw;
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
				health = new AiEconomyHealthLogWriter(Path.Combine(directory, "cameo-ai-economy-health.jsonl"));
				raw = new AiEconomyHealthLogWriter(Path.Combine(directory, "cameo-ai-economy-raw.jsonl"));
			}
			catch (Exception e) when (e is IOException or UnauthorizedAccessException)
			{
				health?.Dispose();
				raw?.Dispose();
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
			if (!ended)
				foreach (var recorder in recorders)
					recorder.Observe(self.World.WorldTick);
		}

		void IGameOver.GameOver(World world)
		{
			if (ended)
				return;
			ended = true;
			foreach (var recorder in recorders)
				recorder.Observe(world.WorldTick, terminal: true);
			health?.Dispose();
			raw?.Dispose();
		}

		void INotifyActorDisposing.Disposing(Actor self)
		{
			// Leaving a world early closes handles without inventing an end-of-match watermark.
			ended = true;
			health?.Dispose();
			raw?.Dispose();
		}
	}

}
