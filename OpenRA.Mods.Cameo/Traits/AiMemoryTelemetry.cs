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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits
{
	[TraitLocation(SystemActors.World)]
	[Desc("Bounded private-bytes/GC telemetry channel (FINDINGS S8): periodic per-game " +
		"cameo-ai-memory-*.jsonl rows so a post-hoc growth curve exists for watchdog kills. " +
		"Record-only observation of process/GC counters plus per-bot retention probes; " +
		"no orders, no synced-state writes, no gameplay effects.")]
	public sealed class AiMemoryTelemetryInfo : TraitInfo
	{
		[Desc("World ticks between memory samples.")]
		public readonly int SampleIntervalTicks = 250;

		public override object Create(ActorInitializer init) { return new AiMemoryTelemetry(this); }
	}

	public sealed class AiMemoryTelemetry : IWorldLoaded, ITick, IGameOver, INotifyActorDisposing
	{
		readonly AiMemoryTelemetryInfo info;
		AiEconomyHealthLogWriter writer;
		Process process;
		string uid;
		bool ended;

		public AiMemoryTelemetry(AiMemoryTelemetryInfo info)
		{
			this.info = info;
		}

		void IWorldLoaded.WorldLoaded(World world, WorldRenderer renderer)
		{
			if (info.SampleIntervalTicks <= 0)
				return;

			if (!AiMatchLogWriter.Eligible(world.Type, world.IsReplay, world.IsLoadingGameSave, Game.IsHost))
				return;

			uid = world.LobbyInfo.GlobalSettings.GameUid;
			if (string.IsNullOrEmpty(uid) || uid.Length > 128)
				return;

			try
			{
				var directory = Path.Combine(Platform.SupportDir, "Logs");
				Directory.CreateDirectory(directory);
				var key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(uid))).ToLowerInvariant();
				writer = new AiEconomyHealthLogWriter(Path.Combine(directory, $"cameo-ai-memory-{key}.jsonl"));
				process = Process.GetCurrentProcess();
			}
			catch (Exception e) when (e is IOException or UnauthorizedAccessException)
			{
				writer?.Dispose();
				writer = null;
				Log.Write("debug", "Memory telemetry unavailable: " + e.GetType().Name);
				return;
			}

			Sample(world);
		}

		void ITick.Tick(Actor self)
		{
			if (writer == null || ended)
				return;

			if (self.World.WorldTick % info.SampleIntervalTicks != 0)
				return;

			Sample(self.World);
		}

		void Sample(World world)
		{
			process.Refresh();
			var gc = GC.GetGCMemoryInfo();
			var b = new StringBuilder(1024);
			AiMatchLogWriter.AppendObjectStart(b);
			AiMatchLogWriter.AppendString(b, "schema", "cameo-memory", true);
			AiMatchLogWriter.AppendNumber(b, "version", 1);
			AiMatchLogWriter.AppendString(b, "game_uid", uid);
			AiMatchLogWriter.AppendNumber(b, "tick", world.WorldTick);
			AiMatchLogWriter.AppendNumber(b, "timestep", world.Timestep);
			AiMatchLogWriter.AppendNumber(b, "private_bytes", process.PrivateMemorySize64);
			AiMatchLogWriter.AppendNumber(b, "working_set_bytes", process.WorkingSet64);
			AiMatchLogWriter.AppendNumber(b, "paged_bytes", process.PagedMemorySize64);
			AiMatchLogWriter.AppendNumber(b, "virtual_bytes", process.VirtualMemorySize64);
			AiMatchLogWriter.AppendNumber(b, "gc_total_memory_bytes", GC.GetTotalMemory(false));
			AiMatchLogWriter.AppendNumber(b, "gc_heap_bytes", gc.HeapSizeBytes);
			AiMatchLogWriter.AppendNumber(b, "gc_fragmented_bytes", gc.FragmentedBytes);
			AiMatchLogWriter.AppendNumber(b, "gc_committed_bytes", gc.TotalCommittedBytes);
			AiMatchLogWriter.AppendNumber(b, "gc_memory_load_bytes", gc.MemoryLoadBytes);
			AiMatchLogWriter.AppendNumber(b, "gc_high_load_threshold_bytes", gc.HighMemoryLoadThresholdBytes);
			AiMatchLogWriter.AppendNumber(b, "gc_promoted_bytes", gc.PromotedBytes);
			AiMatchLogWriter.AppendNumber(b, "gc_pinned_objects", gc.PinnedObjectsCount);
			AiMatchLogWriter.AppendNumber(b, "gc_finalization_pending", gc.FinalizationPendingCount);
			AiMatchLogWriter.AppendNumber(b, "gc_gen0_collections", GC.CollectionCount(0));
			AiMatchLogWriter.AppendNumber(b, "gc_gen1_collections", GC.CollectionCount(1));
			AiMatchLogWriter.AppendNumber(b, "gc_gen2_collections", GC.CollectionCount(2));

			AiMatchLogWriter.AppendArrayPropertyStart(b, "gc_generation_bytes");
			var firstGen = true;
			foreach (var g in gc.GenerationInfo)
			{
				if (!firstGen)
					b.Append(',');
				firstGen = false;
				b.Append(g.SizeAfterBytes);
			}

			b.Append(']');

			var actorCount = 0;
			foreach (var actor in world.Actors)
				if (actor.IsInWorld)
					actorCount++;
			AiMatchLogWriter.AppendNumber(b, "world_actors", actorCount);

			AiMatchLogWriter.AppendArrayPropertyStart(b, "bots");
			var firstBot = true;
			foreach (var player in world.Players.Where(AiMatchLogWriter.IsLoggableBot))
			{
				var probes = player.PlayerActor.TraitsImplementing<OpenRA.Mods.CA.Traits.IAiMemoryTelemetryProbe>();
				var emitted = false;
				foreach (var probe in probes)
				{
					foreach (var pair in probe.MemoryProbeCounts)
					{
						if (!emitted)
						{
							if (!firstBot)
								b.Append(',');
							firstBot = false;
							emitted = true;
							AiMatchLogWriter.AppendObjectStart(b);
							AiMatchLogWriter.AppendString(b, "seat", AiMatchLogWriter.SeatKey(world, player), true);
						}

						AiMatchLogWriter.AppendNumber(b, pair.Key, pair.Value);
					}
				}

				if (emitted)
					b.Append('}');
			}

			b.Append(']');
			b.Append('}');
			writer.TryWrite(b.ToString());
		}

		void IGameOver.GameOver(World world)
		{
			if (writer == null || ended)
				return;

			ended = true;
			Sample(world);
			writer.Dispose();
		}

		void INotifyActorDisposing.Disposing(Actor self)
		{
			ended = true;
			writer?.Dispose();
		}
	}
}
