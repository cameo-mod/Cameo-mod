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
	internal sealed class InsuranceLedger
	{
		readonly int initialEarned;
		int lastEarned;
		internal long Total { get; private set; }
		internal bool Complete { get; private set; }
		internal InsuranceLedger(int tick, int earned)
		{
			initialEarned = lastEarned = earned;
			Complete = tick == 0 && earned >= 0 && earned < int.MaxValue;
		}

		internal void Observe(int earned, int cash)
		{
			if (earned < lastEarned || earned == int.MaxValue || cash == int.MaxValue) Complete = false;
			lastEarned = earned;
		}

		internal long Credit(int requested, long before, long after, int earned, int cash)
		{
			Observe(earned, cash);
			var credited = after - before;
			if (requested <= 0 || credited < 0 || credited > requested || Total > long.MaxValue - credited)
			{
				Complete = false;
				return 0;
			}
			Total += credited;
			return credited;
		}

		internal long? Income => Complete && (long)lastEarned - initialEarned >= Total
			? (long)lastEarned - initialEarned : null;
	}

	internal static class InsuranceSchema
	{
		internal static string Difficulty(string type)
		{
			// No arbitrary bot/player labels in the public stream.
			var code = type == "classic" ? "hard" : type;
			return code is "easiest" or "veryeasy" or "easy" or "medium" or "hard" or "veryhard"
				or "brutal" or "challenger" or "unbeatable" or "cameogod" ? code : "unknown";
		}

		internal static string Row(string uid, int slot, string difficulty, string faction, int seq, int tick,
			string kind, string reason, int requested, long credited, InsuranceLedger ledger, bool complete, int timestep = 1) =>
			JsonSerializer.Serialize(new
			{
				schema = "cameo-insurance", version = 1, game_uid = uid, slot, difficulty, faction,
				seq, tick, timestep, kind, reason, requested, credited, cumulative = ledger.Total,
				complete = complete && ledger.Complete,
				income_basis = "engine_earned_delta_excludes_starting_cash_and_refunds",
				income = kind == "end" && complete ? ledger.Income : null,
				income_share = kind == "end" && complete && ledger.Income is > 0
					? (double?)ledger.Total / ledger.Income.Value : null
			});
	}

	[TraitLocation(SystemActors.World)]
	[Desc("Default-on observational insurance payout ledger. No gameplay effects.")]
	public sealed class InsuranceTelemetryInfo : TraitInfo
	{
		public override object Create(ActorInitializer init) => new InsuranceTelemetry();
	}

	public sealed class InsuranceTelemetry : IWorldLoaded, ITick, IGameOver, INotifyActorDisposing
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

		readonly Dictionary<OpenRA.Player, Participant> participants = [];
		AiEconomyHealthLogWriter writer;
		string uid;
		int timestep;
		bool ended;
		bool complete = true;

		void IWorldLoaded.WorldLoaded(World world, WorldRenderer renderer)
		{
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
				participants.Add(player, p);
			}
			foreach (var p in participants.Values) Write(p, world.WorldTick, "start", "none", 0, 0);
		}

		void Write(Participant p, int tick, string kind, string reason, int requested, long credited)
		{
			if (!writer.Complete) return; // Failed stream stays UNKNOWN; stop allocating output rows.
			writer.TryWrite(InsuranceSchema.Row(uid, p.Slot, p.Difficulty, p.Faction, p.Seq++, tick,
				kind, reason, requested, credited, p.Ledger, complete && writer.Complete, timestep));
		}

		internal static void Payout(Actor self, string reason, int requested, long before, long after)
		{
			self.World.WorldActor.TraitOrDefault<InsuranceTelemetry>()?.Record(self, reason, requested, before, after);
		}

		void Record(Actor self, string reason, int requested, long before, long after)
		{
			if (writer == null || ended || !participants.TryGetValue(self.Owner, out var p)) return;
			var amount = p.Ledger.Credit(requested, before, after, p.Resources.Earned, p.Resources.Cash);
			Write(p, self.World.WorldTick, "payout", reason, requested, amount);
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
