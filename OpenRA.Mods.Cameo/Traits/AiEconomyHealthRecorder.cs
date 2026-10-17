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
	[Desc("Opt-in, record-only economy health capture. Do not mount in ordinary games.")]
	public sealed class AiEconomyHealthRecorderInfo : TraitInfo
	{
		public override object Create(ActorInitializer init) => new AiEconomyHealthRecorder(init.Self);
	}

	public sealed class AiEconomyHealthRecorder : IBotBuildQueueObserver, INotifyResourceAccepted
	{
		readonly Actor self;
		AiEconomyHealthState state;
		AiEconomyHealthLogWriter health;
		AiEconomyHealthLogWriter raw;
		HashSet<string> groups;
		HashSet<uint> resolvedEpisodes;
		List<EconomyQueueObservation> observations;
		List<EconomyQueueSnapshot> snapshots;
		string gameUid;
		int sequence;
		int rawSequence;
		int lastPulse = -1;
		int queueIndexTick = -1;
		bool finished;
		readonly EconomyAcceptedTick accepted = new();

		// Own-queue canonical index, rebuilt at most once per world tick on the first proven
		// cancellation: one enumeration of world production queues amortizes every cancel in
		// that tick instead of scanning per event. A queue created mid-tick after the index was
		// built resolves to null — fail-closed UNKNOWN, never a wrong binding.
		IReadOnlyDictionary<(uint Producer, string Group), string> queueIndex;

		public AiEconomyHealthRecorder(Actor self) { this.self = self; }

		internal void Activate(string uid, AiEconomyHealthLogWriter healthWriter, AiEconomyHealthLogWriter rawWriter)
		{
			gameUid = uid;
			health = healthWriter;
			raw = rawWriter;
			state = new AiEconomyHealthState();
			// Activation/census/pulse/terminal ordering validated by admitted run C
			// (game uid 25060589-136e-46b8-9d78-4c4ad7828688, 2026-10-12). 'complete'
			// stays a one-way latch: every runtime failure path below still marks it.
			observations = new List<EconomyQueueObservation>(AiEconomyHealthState.MaximumQueues);
			snapshots = new List<EconomyQueueSnapshot>(AiEconomyHealthState.MaximumQueues);
			resolvedEpisodes = new HashSet<uint>();
			groups = self.Info.TraitInfos<BaseBuilderBotModuleCAInfo>()
				.SelectMany(i => i.BuildingQueues).ToHashSet(StringComparer.Ordinal);
			if (groups.Count == 0 || string.IsNullOrEmpty(uid))
				state.MarkIncomplete();
		}

		internal void Observe(int tick, bool terminal = false)
		{
			if (state == null || finished)
				return;
			FlushAccepted(tick, terminal);
			observations.Clear();
			foreach (var pair in self.World.ActorsWithTrait<ProductionQueue>())
			{
				var queue = pair.Trait;
				if (pair.Actor.Owner != self.Owner || !groups.Contains(queue.Info.Group))
					continue;
				if (observations.Count == AiEconomyHealthState.MaximumQueues)
				{
					state.MarkIncomplete();
					break;
				}
				var head = queue.CurrentItem();
				var status = AiEconomyHealthState.QueueState(queue.Enabled, head != null,
					head?.Done ?? false, head?.Paused ?? false);
				observations.Add(new EconomyQueueObservation(queue, head,
					$"{pair.Actor.ActorID}:{queue.Info.Type}:{queue.Info.Group}", head?.Item ?? "", status,
					!pair.Actor.IsDead && !pair.Actor.Disposed,
					queue.Enabled ? "observed" : "producer-disabled"));
			}
			state.Observe(tick, observations);
			// Scheduled pulses are %50-only; a terminal observe may add one off-boundary
			// pulse before the end record. The consumer's <=50-tick watermark allows it.
			if ((AiEconomyHealthState.PulseDue(tick) || terminal) && tick != lastPulse)
				Pulse(tick);
			if (terminal)
			{
				WriteHealth(AiEconomyHealthSchema.End(Identity(tick, "end"),
					state.Complete && health.Complete && raw.Complete, SeatState()));
				finished = true;
			}
		}

		string SeatState() => self.Owner.WinState switch
		{
			WinState.Won => "won",
			WinState.Lost => "lost",
			_ => "active",
		};

		void Pulse(int tick)
		{
			state.CopySnapshots(snapshots);
			var resources = self.TraitOrDefault<PlayerResources>();
			if (resources == null)
				state.MarkIncomplete();
			WriteHealth(AiEconomyHealthSchema.Pulse(Identity(tick, "pulse"),
				self.Owner.WinState == WinState.Undefined, state.Complete, snapshots,
				resources?.Cash ?? 0, resources?.Resources ?? 0, resources?.ResourceCapacity ?? 0,
				resources?.Spent ?? 0, SeatState()));
			lastPulse = tick;
		}

		EconomyHealthIdentity Identity(int tick, string kind) => new(sequence++, tick, kind, gameUid,
			self.Owner.InternalName, self.World.Map.Uid, self.Owner.Faction.InternalName,
			self.Owner.BotType ?? "unknown", groups.Count > 0,
			state.Complete && health.Complete && raw.Complete ? 0 : 1);

		void WriteHealth(string json)
		{
			if (!health.TryWrite(json))
				state.MarkIncomplete();
		}

		void WriteRaw(string kind, int tick, object value)
		{
			if (!raw.TryWrite(AiEconomyHealthSchema.Raw(rawSequence++, tick, gameUid,
				self.Owner.InternalName, kind, value)))
				state.MarkIncomplete();
		}

		void FlushAccepted(int tick, bool terminal = false)
		{
			if (accepted.TryTakeBefore(tick, terminal, out var acceptedTick, out var evidence))
				WriteRaw("resource-accepted-tick", acceptedTick, evidence);
		}

		void IBotBuildQueueObserver.OnQueueTransition(in BotQueueTransition transition)
		{
			if (state == null || finished)
				return;
			FlushAccepted(transition.Tick);
			WriteRaw("queue-observation", transition.Tick, new
			{
				item = transition.ItemId, queue = transition.Queue, producer = transition.ProducerActorId,
				episode = transition.EpisodeId, kind = transition.Kind.ToString(), reason = transition.Reason.ToString(),
				player_active = transition.PlayerActive, producer_live = transition.ProducerLive,
				cancellation_class = transition.CancellationClass.ToString(), category = transition.Category.ToString()
			});
			var evidence = AiEconomyQueueEvidence.Classify(in transition,
				resolvedEpisodes.Contains(transition.EpisodeId));
			if (evidence == EconomyQueueEvidenceKind.Unknown)
				state.MarkIncomplete();
			else if (evidence == EconomyQueueEvidenceKind.ProvenCancellation)
			{
				var queueId = CancellationQueueId(in transition);
				if (AiEconomyQueueEvidence.TryCancellation(in transition, queueId, out var cancellation))
					WriteHealth(AiEconomyHealthSchema.Cancel(Identity(transition.Tick, "cancel"), cancellation));
				else if (transition.CancellationClass is not (BotQueueCancellationClass.Destruction
					or BotQueueCancellationClass.Elimination))
					state.MarkIncomplete();
			}

			// Episode resolution bookkeeping for Removed classification: cancel intent,
			// proven cancellation and proven placement resolve an episode — a later Removed
			// is the dequeue of an outcome already on record. PlacementOrdered does NOT
			// resolve: its proof window is still armed, so that Removed stays UNKNOWN.
			// Bounded like every other recorder set; overflow fails closed.
			if (transition.EpisodeId != 0)
			{
				if (transition.Kind == BotQueueTransitionKind.Removed)
					resolvedEpisodes.Remove(transition.EpisodeId);
				else if ((transition.Kind is BotQueueTransitionKind.CancelOrdered
						or BotQueueTransitionKind.Cancelled or BotQueueTransitionKind.Placed)
					&& !resolvedEpisodes.Contains(transition.EpisodeId))
				{
					if (resolvedEpisodes.Count >= AiEconomyHealthState.MaximumQueues)
						state.MarkIncomplete();
					else
						resolvedEpisodes.Add(transition.EpisodeId);
				}
			}

			// Proven Placed remains raw outcome evidence. The pinned economy consumer
			// accepts pulse/cancel/end only; queue census observes the new state separately.
		}

		string CancellationQueueId(in BotQueueTransition transition)
		{
			if (queueIndexTick != transition.Tick)
			{
				queueIndexTick = transition.Tick;
				queueIndex = AiEconomyQueueEvidence.BuildCancellationQueueIndex(
					OwnConfiguredQueues(), AiEconomyHealthState.MaximumQueues);
			}
			return AiEconomyQueueEvidence.ResolveCancellationQueueId(
				queueIndex, transition.ProducerActorId, transition.Queue);
		}

		IEnumerable<(uint Producer, string QueueType, string Group)> OwnConfiguredQueues()
		{
			foreach (var pair in self.World.ActorsWithTrait<ProductionQueue>())
			{
				var queue = pair.Trait;
				if (pair.Actor.Owner == self.Owner && groups.Contains(queue.Info.Group))
					yield return (pair.Actor.ActorID, queue.Info.Type, queue.Info.Group);
			}
		}

		void INotifyResourceAccepted.OnResourceAccepted(Actor actor, Actor refinery, string resourceType, int count, int value)
		{
			if (state == null || finished || refinery.Owner != self.Owner)
				return;
			var tick = self.World.WorldTick;
			FlushAccepted(tick);
			if (!accepted.Add(tick, refinery.ActorID, resourceType, count, value))
				state.MarkIncomplete();
		}
	}

	// Record-only rollup: callback rate cannot increase the number of emitted records.
	// Observe may precede deliveries in a tick, so only advance/terminal drains that tick.
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
