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
			return code is "noob" or "easiest" or "veryeasy" or "easy" or "medium" or "hard" or "veryhard"
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

	// Release producer boundary. The optional sink receives only detached scalar values.
	public sealed class InsuranceTelemetry
	{
		internal static void Payout(Actor self, string reason, int requested, long before, long after)
		{
			var world = self.World;
			var hub = world.WorldActor.TraitOrDefault<OpenRA.Mods.CA.Traits.CameoLogHub>();
			if (hub != null && hub.Sink.Enabled)
				hub.Sink.InsurancePayout(Array.IndexOf(world.Players, self.Owner), world.WorldTick, reason, requested, before, after);
		}
	}
}