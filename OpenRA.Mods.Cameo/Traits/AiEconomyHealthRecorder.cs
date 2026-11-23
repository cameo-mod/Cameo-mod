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
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits
{
	[TraitLocation(SystemActors.Player)]
	[Desc("Release-compatible no-op registration; observation is provided by the DEV mod.")]
	public sealed class AiEconomyHealthRecorderInfo : TraitInfo
	{
		public override object Create(ActorInitializer init) => new AiEconomyHealthRecorder(init.Self);
	}

	// The release rules retain this registration; observation lives in the DEV assembly.
	public sealed class AiEconomyHealthRecorder
	{
		public AiEconomyHealthRecorder(Actor self) { }
	}

	internal sealed class EconomyAcceptedTick
	{
		internal const int MaximumSamples = 1;
		internal const int MaximumTypeCharacters = 64;
		readonly List<object> samples = new(MaximumSamples);
		int tick = -1;
		long events, count, value, creditedValue, creditedEvents;
		bool textTruncated;

		internal bool Add(int atTick, uint refinery, string type, int units, int credited)
		{
			if (tick != -1 && tick != atTick)
				return false;
			try
			{
				// Compute first: an overflow must not publish plausible partial totals.
				var nextEvents = checked(events + 1);
				var nextCount = checked(count + units);
				var nextValue = checked(value + credited);
				var nextCredit = checked(creditedValue + (AiEconomyHealthState.AcceptedDelivery(credited) ? credited : 0));
				var nextCreditEvents = checked(creditedEvents + (AiEconomyHealthState.AcceptedDelivery(credited) ? 1 : 0));
				events = nextEvents;
				count = nextCount;
				value = nextValue;
				creditedValue = nextCredit;
				creditedEvents = nextCreditEvents;
				tick = atTick;
				if (samples.Count < MaximumSamples)
				{
					var truncated = type != null && type.Length > MaximumTypeCharacters;
					textTruncated |= truncated;
					samples.Add(new { refinery, resource_type = truncated ? type[..MaximumTypeCharacters] : type,
						resource_type_truncated = truncated, count = units, value = credited,
						accepted_credit = AiEconomyHealthState.AcceptedDelivery(credited), harvester = (uint?)null });
				}
				return true;
			}
			catch (OverflowException) { return false; }
		}

		internal bool TryTakeBefore(int atTick, bool terminal, out int emittedTick, out object evidence)
		{
			emittedTick = tick;
			evidence = null;
			if (tick == -1 || (!terminal && tick >= atTick))
				return false;
			evidence = new { event_count = events, count, value, credited_value = creditedValue,
				accepted_event_count = creditedEvents, accepted_credit = creditedEvents > 0,
				harvester = (uint?)null, detail_truncated = textTruncated || events > samples.Count,
				sample_count = samples.Count, samples = samples.ToArray() };
			tick = -1;
			events = count = value = creditedValue = creditedEvents = 0;
			textTruncated = false;
			samples.Clear();
			return true;
		}
	}
	internal readonly record struct EconomyHealthIdentity(int Sequence, int Tick, string Kind,
		string GameUid, string Player, string MapUid, string Faction, string Profile,
		bool ProfileSupported, int Dropped);

	// The runtime recorder and consumer-fit tests use the same serializer.
	internal static class AiEconomyHealthSchema
	{
		internal static object AcceptedResourceEvidence(uint refinery, string type, int count, int value) =>
			new { refinery, resource_type = type, count, value,
				accepted_credit = AiEconomyHealthState.AcceptedDelivery(value), harvester = (uint?)null };

		internal static string Raw(int sequence, int tick, string gameUid, string player, string kind, object evidence) =>
			JsonSerializer.Serialize(new { schema = 2, seq = sequence, tick, kind,
				game_uid = gameUid, player, evidence });

		static Dictionary<string, object> Common(EconomyHealthIdentity id) => new()
		{
			["schema"] = 2, ["seq"] = id.Sequence, ["tick"] = id.Tick, ["kind"] = id.Kind,
			["game_uid"] = id.GameUid, ["player"] = id.Player, ["map_uid"] = id.MapUid,
			["faction"] = id.Faction, ["profile"] = id.Profile,
			["profile_supported"] = id.ProfileSupported, ["dropped"] = id.Dropped
		};

		internal static string Pulse(EconomyHealthIdentity id, bool active, bool queuesComplete,
			IReadOnlyList<EconomyQueueSnapshot> queues, int cash, int resources, int capacity,
			int netSpent, string seatState = "active")
		{
			var record = Common(id);
			record.Add("player_active", active);
			record.Add("seat_state", seatState);
			record.Add("queues_complete", queuesComplete);
			record.Add("queues", queues.Select(q => new { queue_id = q.QueueId, item_id = q.ItemId,
				item = q.Item, state = q.State, state_since_tick = q.StateSinceTick,
				producer_live = q.ProducerLive, reason = q.Reason }));
			record.Add("cash", cash);
			record.Add("resources", resources);
			record.Add("capacity", capacity);
			record.Add("net_spent", netSpent);
			record.Add("gross_spent", null);
			record.Add("gross_spend_complete", false);
			return JsonSerializer.Serialize(record);
		}

		internal static string End(EconomyHealthIdentity id, bool complete, string seatState = "active")
		{
			var record = Common(id);
			record.Add("seat_state", seatState);
			record.Add("complete", complete);
			return JsonSerializer.Serialize(record);
		}

		internal static string Cancel(EconomyHealthIdentity id, EconomyCancellationEvidence evidence)
		{
			var record = Common(id);
			record.Add("queue_id", evidence.QueueId);
			record.Add("item_id", evidence.ItemId);
			record.Add("item", evidence.Item);
			record.Add("reason", evidence.Reason);
			record.Add("player_active", evidence.PlayerActive);
			record.Add("producer_live", evidence.ProducerLive);
			record.Add("cancellation_class", evidence.CancellationClass);
			return JsonSerializer.Serialize(record);
		}
	}

}
