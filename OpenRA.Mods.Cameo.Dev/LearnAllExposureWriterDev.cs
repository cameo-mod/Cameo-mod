#region Copyright & License Information
/*
 * Copyright (c) OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Cameo.Dev;
using OpenRA.Mods.Cameo.Traits.BotModules;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits
{
	[TraitLocation(SystemActors.World)]
	[Desc("LEARN-ALL-02 P1c-2 (DEV only): writes ONE exposure record per bot seat at game close for the learning fitter (tools/ai/fit_learn_all.py): the seat's public match-up context from",
		"LearnAllBotModule plus the per-knob signals the features hand over through IBotLearnSignals. One queued JSONL file per game, written at game over: no tick-path I/O. Never a decision,",
		"never an order, no synced state. The release build has no such trait.")]
	public sealed class LearnAllExposureWriterDevInfo : TraitInfo
	{
		[Desc("Ticks between the context observations (the last authoritative main target and its observed style are kept for the record). Offset by half a period from the module's own refresh.")]
		public readonly int ObserveTicks = 250;

		public override object Create(ActorInitializer init) { return new LearnAllExposureWriterDev(this); }
	}

	public sealed class LearnAllExposureWriterDev : IWorldLoaded, ITick, IGameOver
	{
		readonly LearnAllExposureWriterDevInfo info;
		string uid;
		long seq;
		bool active;
		bool done;

		public LearnAllExposureWriterDev(LearnAllExposureWriterDevInfo info)
		{
			this.info = info;
		}

		void IWorldLoaded.WorldLoaded(World world, WorldRenderer renderer)
		{
			if (!world.WorldActor.Trait<CameoLogHub>().Sink.Enabled)
				return;

			if (!AiMatchLogWriter.Eligible(world.Type, world.IsReplay, world.IsLoadingGameSave, Game.IsHost))
				return;

			uid = world.LobbyInfo.GlobalSettings.GameUid;
			if (!LearnAllExposure.ValidName(uid))
				return;

			// Game order for the fitter: the start time in unix seconds (games of one host never overlap, and the fitter needs a strictly rising seq).
			seq = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
			active = true;
		}

		void ITick.Tick(Actor self)
		{
			if (!active || done)
				return;

			var world = self.World;
			var period = Math.Max(1, info.ObserveTicks);
			if (world.WorldTick % period != period / 2)
				return;

			// Record-only: remember the LAST authoritative main target and its style for the end. ObserveMainTarget reads the providers directly and never touches the module's
			// decision-path cache or refresh phase, so a writer-on game decides exactly as a writer-off one.
			foreach (var p in world.Players)
				if (IsBotSeat(p))
					Module(p)?.ObserveMainTarget();
		}

		void IGameOver.GameOver(World world)
		{
			if (!active || done)
				return;

			done = true;
			var lines = new List<string>();
			var dropped = 0;
			for (var i = 0; i < world.Players.Length; i++)
			{
				var p = world.Players[i];
				if (!IsBotSeat(p))
					continue;

				var module = Module(p);
				if (module == null)
				{
					dropped++;
					continue;
				}

				var outcome = p.WinState == WinState.Won ? 1 : p.WinState == WinState.Lost ? -1 : 0;
				var end = new LearnGameEnd(world.WorldTick, outcome);
				var raw = new List<LearnSignal>();
				foreach (var provider in p.PlayerActor.TraitsImplementing<IBotLearnSignals>())
				{
					if (provider is IDisabledTrait disabled && disabled.IsTraitDisabled)
						continue;

					provider.CollectLearnSignals(end, raw);
				}

				var signals = LearnAllExposure.Sanitize(raw, out var bad);
				dropped += bad;
				var line = LearnAllExposure.Record(uid, seq, LearnAllExposure.SeatId(i), module.SeatContext(), signals);
				if (line != null)
					lines.Add(line);
				else if (raw.Count > 0)
					dropped++;
			}

			if (lines.Count == 0)
			{
				Log.Write("debug", "LearnAll exposure: no record (no armed knob signals), dropped=" + dropped);
				return;
			}

			try
			{
				var writer = new AiQueuedJsonlWriter("learn_all_exposure_" + uid + ".jsonl", "learn_all_exposure");
				foreach (var line in lines)
					writer.TryWrite(line);

				writer.Flush(world.WorldTick);
				writer.Close(world.WorldTick);
				AiLogWriteQueue.FlushSharedForGameEnd(); // bounded drain, as the sibling DEV writers do
				Log.Write("debug", "LearnAll exposure: " + lines.Count + " seats written, dropped=" + dropped + ", failed=" + writer.Failed);
			}

			// Fail closed: an I/O fault (file exists, disk) loses the record and never the game. The fitter simply gets no input for this game.
			catch (Exception e) when (e is IOException or UnauthorizedAccessException)
			{
				Log.Write("debug", "LearnAll exposure unavailable: " + e.GetType().Name);
			}
		}

		static bool IsBotSeat(OpenRA.Player p) => p.IsBot && !p.NonCombatant && !p.Spectating;

		static LearnAllBotModule Module(OpenRA.Player p) => p.PlayerActor.TraitsImplementing<LearnAllBotModule>().FirstOrDefault();
	}
}
