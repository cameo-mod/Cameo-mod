using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits
{
	[TraitLocation(SystemActors.World)]
	[Desc("Development-only insurance telemetry.")]
	public sealed class InsuranceTelemetryDevInfo : TraitInfo
	{
		public override object Create(ActorInitializer init) => new InsuranceTelemetryDev();
	}

	// SPK-01 (DEV only). The sim thread only formats rows and appends them to a batch (no I/O). A batch is handed to the
	// shared AiLogWriteQueue worker, which owns the append, the mutex and the retry. Batches go out every 150 ticks, at
	// 2048 queued rows, and at GameOver. A refused hand-off is sticky: the file stops there and stays UNKNOWN.
	public sealed class InsuranceTelemetryDev : IWorldLoaded, ITick, IGameOver, INotifyActorDisposing
	{
		internal const int FlushEveryTicks = 150;
		internal const int PendingRowLimit = 2048;

		sealed class Participant
		{
			internal int Slot;
			internal int Seq;
			internal string Difficulty;
			internal string Faction;
			internal PlayerResources Resources;
			internal InsuranceLedger Ledger;
		}

		readonly Dictionary<int, Participant> participants = [];
		readonly StringBuilder batch = new();
		AiLogFileAppender appender;
		string uid;
		int timestep;
		bool ended;
		bool complete = true;
		bool failed;
		int tickCount;
		int lastTick;
		int batchRows;
		long batchBytes;
		int committedRows;
		long committedBytes;

		// Counted, never silent: a data row refused by the row budget, or a batch refused by the queue, adds to this count.
		// GameOver then writes "dropped" rows instead of "end" rows, so the stream never claims coverage it does not have.
		long droppedRows;

		// LOW-1: once the budget has refused a data row, every later data row is refused too. A smaller row that
		// still fits would leave a hole, and the reader would then see cumulative values above the file's sum.
		bool budgetClosed;

		void IWorldLoaded.WorldLoaded(World world, WorldRenderer renderer)
		{
			if (!world.WorldActor.Trait<OpenRA.Mods.CA.Traits.CameoLogHub>().Sink.Enabled)
				return;

			if (!AiMatchLogWriter.Eligible(world.Type, world.IsReplay, world.IsLoadingGameSave, Game.IsHost)) return;
			uid = world.LobbyInfo.GlobalSettings.GameUid;
			timestep = world.Timestep;
			if (string.IsNullOrEmpty(uid) || uid.Length > 128) return;
			lastTick = world.WorldTick;
			try
			{
				var directory = Path.Combine(Platform.SupportDir, "Logs");
				Directory.CreateDirectory(directory);
				var key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(uid))).ToLowerInvariant();
				var fileName = $"cameo-ai-insurance-{key}.jsonl";

				// One exclusive create per match, once at load (not per row): existing evidence is never overwritten.
				using (new FileStream(Path.Combine(directory, fileName), FileMode.CreateNew, FileAccess.Write, FileShare.Read)) { }
				appender = new AiLogFileAppender(fileName, "insurance");
			}
			catch (Exception e) when (e is IOException or UnauthorizedAccessException)
			{
				Log.Write("debug", "Insurance telemetry unavailable: " + e.GetType().Name);
				return;
			}

			var slot = 0;
			foreach (var player in world.Players)
			{
				var currentSlot = slot++;
				if (!AiMatchLogWriter.IsLoggableBot(player)) continue;
				var resources = player.PlayerActor.TraitOrDefault<PlayerResources>();
				if (participants.Count == 64 || resources == null) { complete = false; continue; }
				var p = new Participant { Slot = currentSlot, Difficulty = InsuranceSchema.Difficulty(player.BotType),
					Faction = player.Faction.InternalName, Resources = resources,
					Ledger = new InsuranceLedger(world.WorldTick, resources.Earned) };
				participants.Add(currentSlot, p);
			}

			foreach (var p in participants.Values) Write(p, world.WorldTick, "start", "none", 0, 0);
		}

		// A data row. It is built, then admitted only if the budget keeps room for every participant's terminal row.
		void Write(Participant p, int tick, string kind, string reason, int requested, long credited)
		{
			if (failed || appender == null) return;
			lastTick = tick;
			var row = InsuranceSchema.Row(uid, p.Slot, p.Difficulty, p.Faction, p.Seq, tick,
				kind, reason, requested, credited, p.Ledger, complete, timestep);
			var bytes = InsuranceRowBudget.RowBytes(row);
			if (budgetClosed || !InsuranceRowBudget.DataRowFits(committedRows + batchRows, committedBytes + batchBytes, bytes,
				participants.Count, AiEconomyHealthLogWriter.MaximumRecords, AiEconomyHealthLogWriter.MaximumFileBytes))
			{
				budgetClosed = true;
				droppedRows++;
				return;
			}

			Queue(row, bytes);
			p.Seq++;
			if (batchRows >= PendingRowLimit)
				FlushRows(false);
		}

		// A terminal row (end or dropped). Its room is reserved by the data budget, so it is never refused by it.
		void WriteTerminal(Participant p, int tick, string kind, string reason, int requested, bool rowComplete)
		{
			lastTick = tick;
			var row = InsuranceSchema.Row(uid, p.Slot, p.Difficulty, p.Faction, p.Seq, tick,
				kind, reason, requested, 0, p.Ledger, rowComplete, timestep);
			Queue(row, InsuranceRowBudget.RowBytes(row));
			p.Seq++;
		}

		void Queue(string row, long bytes)
		{
			batch.Append(row).Append('\n');
			batchRows++;
			batchBytes += bytes;
		}

		// Hands the batch to the shared queue. Never blocks; a refusal is counted and makes the stream sticky-failed.
		// final: the GameOver batch, exempt from the queue's text budget.
		void FlushRows(bool final)
		{
			if (appender == null || batchRows == 0)
				return;

			if (!failed)
			{
				var result = appender.TryAppend(batch.ToString(), lastTick, out _, final);
				if (result == AiLogAppendResult.Appended)
				{
					committedRows += batchRows;
					committedBytes += batchBytes;
				}
				else
				{
					failed = true;
					droppedRows += batchRows;
				}
			}
			else
				droppedRows += batchRows;

			batch.Clear();
			batchRows = 0;
			batchBytes = 0;
		}

		internal void Record(int playerIndex, int tick, string reason, int requested, long before, long after)
		{
			if (appender == null || ended || !participants.TryGetValue(playerIndex, out var p)) return;
			var amount = p.Ledger.Credit(requested, before, after, p.Resources.Earned, p.Resources.Cash);
			Write(p, tick, "payout", reason, requested, amount);
		}

		void ITick.Tick(Actor self)
		{
			if (appender == null || ended) return;
			lastTick = self.World.WorldTick;
			foreach (var p in participants.Values) p.Ledger.Observe(p.Resources.Earned, p.Resources.Cash);
			if (++tickCount % FlushEveryTicks == 0)
				FlushRows(false);
		}

		void IGameOver.GameOver(World world)
		{
			if (appender == null || ended) return;
			foreach (var p in participants.Values)
			{
				p.Ledger.Observe(p.Resources.Earned, p.Resources.Cash);
				if (droppedRows == 0)
					WriteTerminal(p, world.WorldTick, "end", "none", 0, complete);
				else
					WriteTerminal(p, world.WorldTick, "dropped", "row_budget", (int)Math.Min(droppedRows, int.MaxValue), false);
			}

			FlushRows(true);
			// MED-1: the terminal rows must reach the file before GameOver returns (bounded wait), as the sibling DEV writers do.
			AiLogWriteQueue.FlushSharedForGameEnd();
			ended = true;
		}

		void INotifyActorDisposing.Disposing(Actor self)
		{
			ended = true;
			FlushRows(false); // Queued complete data rows only. Disposal cannot fabricate a covered terminal.
		}
	}
}
