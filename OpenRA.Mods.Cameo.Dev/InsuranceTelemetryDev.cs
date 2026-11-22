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

	public sealed class InsuranceTelemetryDev : IWorldLoaded, ITick, IGameOver, INotifyActorDisposing
	{
		// SPK-01 (DEV only): rows are batched on the sim thread and flushed once per batch, not once per record.
		// A full batch or the periodic tick flush writes the pending lines and flushes once; GameOver and disposal drain it.
		internal const int FlushEveryTicks = 150;
		internal const int PendingRowLimit = 2048;

		// Bytes held back for the terminal rows (one end or dropped row per participant). Rows are far below 1 KiB.
		const int TerminalRowBytes = 1024;

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
		readonly List<string> pending = [];
		AiEconomyHealthLogWriter writer;
		string uid;
		int timestep;
		bool ended;
		bool complete = true;
		long pendingChars;
		int tickCount;

		// SPK-01: counted, never silent. Every data row refused by the record or byte budget is counted here, and the
		// terminal rows then carry the count as "dropped" instead of an end row (so the stream never claims coverage).
		long droppedRows;

		void IWorldLoaded.WorldLoaded(World world, WorldRenderer renderer)
		{
			if (!world.WorldActor.Trait<OpenRA.Mods.CA.Traits.CameoLogHub>().Sink.Enabled)
				return;

			if (!AiMatchLogWriter.Eligible(world.Type, world.IsReplay, world.IsLoadingGameSave, Game.IsHost)) return;
			uid = world.LobbyInfo.GlobalSettings.GameUid;
			timestep = world.Timestep;
			if (string.IsNullOrEmpty(uid) || uid.Length > 128) return;
			try
			{
				var directory = Path.Combine(Platform.SupportDir, "Logs");
				Directory.CreateDirectory(directory);
				var key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(uid))).ToLowerInvariant();
				writer = new AiEconomyHealthLogWriter(Path.Combine(directory, $"cameo-ai-insurance-{key}.jsonl"), flushEachRecord: false);
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

		// Data rows. The budget reserves room for the terminal rows (two per participant), so a refusal here is a counted drop.
		void Write(Participant p, int tick, string kind, string reason, int requested, long credited)
		{
			if (!writer.Complete) return; // Failed stream stays UNKNOWN; stop allocating output rows.
			if (writer.Records + pending.Count + 2 * participants.Count >= AiEconomyHealthLogWriter.MaximumRecords ||
				writer.Bytes + pendingChars + 2L * participants.Count * TerminalRowBytes >= AiEconomyHealthLogWriter.MaximumFileBytes)
			{
				droppedRows++;
				return;
			}

			Append(p, tick, kind, reason, requested, credited, complete);
			if (pending.Count >= PendingRowLimit)
				FlushRows();
		}

		// Builds one row and queues it. Terminal rows bypass the data budget (their room is reserved).
		void Append(Participant p, int tick, string kind, string reason, int requested, long credited, bool rowComplete)
		{
			var row = InsuranceSchema.Row(uid, p.Slot, p.Difficulty, p.Faction, p.Seq++, tick,
				kind, reason, requested, credited, p.Ledger, rowComplete && writer.Complete, timestep);
			pending.Add(row);
			pendingChars += row.Length;
		}

		// Writes the queued lines in order and flushes once. A refused write is sticky in the writer; the rows not written
		// are counted as drops, and the stream stays UNKNOWN for the reader.
		void FlushRows()
		{
			if (writer == null)
				return;

			for (var i = 0; i < pending.Count; i++)
			{
				if (!writer.TryWrite(pending[i]))
				{
					droppedRows += pending.Count - i;
					break;
				}
			}

			pending.Clear();
			pendingChars = 0;
			writer.FlushPending();
		}

		internal void Record(int playerIndex, int tick, string reason, int requested, long before, long after)
		{
			if (writer == null || ended || !participants.TryGetValue(playerIndex, out var p)) return;
			var amount = p.Ledger.Credit(requested, before, after, p.Resources.Earned, p.Resources.Cash);
			Write(p, tick, "payout", reason, requested, amount);
		}

		void ITick.Tick(Actor self)
		{
			if (writer == null || ended) return;
			foreach (var p in participants.Values) p.Ledger.Observe(p.Resources.Earned, p.Resources.Cash);
			if (++tickCount % FlushEveryTicks == 0)
				FlushRows();
		}

		void IGameOver.GameOver(World world)
		{
			if (writer == null || ended) return;
			foreach (var p in participants.Values)
			{
				p.Ledger.Observe(p.Resources.Earned, p.Resources.Cash);
				if (droppedRows == 0)
					Append(p, world.WorldTick, "end", "none", 0, 0, true);
				else
					Append(p, world.WorldTick, "dropped", "row_budget", (int)Math.Min(droppedRows, int.MaxValue), 0, false);
			}

			FlushRows();
			ended = true;
			writer.Dispose();
		}

		void INotifyActorDisposing.Disposing(Actor self)
		{
			ended = true;
			FlushRows(); // Flushes complete rows already queued. Disposal cannot fabricate a covered terminal.
			writer?.Dispose();
		}
	}
}
