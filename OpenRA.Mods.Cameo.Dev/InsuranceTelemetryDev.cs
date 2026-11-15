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
		AiEconomyHealthLogWriter writer;
		string uid;
		int timestep;
		bool ended;
		bool complete = true;

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
				writer = new AiEconomyHealthLogWriter(Path.Combine(directory, $"cameo-ai-insurance-{key}.jsonl"));
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

		void Write(Participant p, int tick, string kind, string reason, int requested, long credited)
		{
			if (!writer.Complete) return; // Failed stream stays UNKNOWN; stop allocating output rows.
			writer.TryWrite(InsuranceSchema.Row(uid, p.Slot, p.Difficulty, p.Faction, p.Seq++, tick,
				kind, reason, requested, credited, p.Ledger, complete && writer.Complete, timestep));
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
		}

		void IGameOver.GameOver(World world)
		{
			if (writer == null || ended) return;
			foreach (var p in participants.Values)
			{
				p.Ledger.Observe(p.Resources.Earned, p.Resources.Cash);
				Write(p, world.WorldTick, "end", "none", 0, 0);
			}
			ended = true;
			writer.Dispose();
		}

		void INotifyActorDisposing.Disposing(Actor self)
		{
			ended = true;
			writer?.Dispose(); // Disposal cannot fabricate a covered terminal.
		}
	}
}
