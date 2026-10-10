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
		List<EconomyQueueObservation> observations;
		List<EconomyQueueSnapshot> snapshots;
		string gameUid;
		int sequence;
		int rawSequence;
		int lastPulse = -1;
		bool finished;

		public AiEconomyHealthRecorder(Actor self) { this.self = self; }

		internal void Activate(string uid, AiEconomyHealthLogWriter healthWriter, AiEconomyHealthLogWriter rawWriter)
		{
			gameUid = uid;
			health = healthWriter;
			raw = rawWriter;
			state = new AiEconomyHealthState();
			// Outcome mapping is supported, but actual activation/census/terminal ordering
			// and runtime coverage have not been validated. Keep adoption fail-closed.
			state.MarkIncomplete();
			observations = new List<EconomyQueueObservation>(AiEconomyHealthState.MaximumQueues);
			snapshots = new List<EconomyQueueSnapshot>(AiEconomyHealthState.MaximumQueues);
			groups = self.Info.TraitInfos<BaseBuilderBotModuleCAInfo>()
				.SelectMany(i => i.BuildingQueues).ToHashSet(StringComparer.Ordinal);
			if (groups.Count == 0 || string.IsNullOrEmpty(uid))
				state.MarkIncomplete();
		}

		internal void Observe(int tick, bool terminal = false)
		{
			if (state == null || finished)
				return;
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
			if ((AiEconomyHealthState.PulseDue(tick) || terminal) && tick != lastPulse)
				Pulse(tick);
			if (terminal)
			{
				WriteHealth(AiEconomyHealthSchema.End(Identity(tick, "end"),
					state.Complete && health.Complete && raw.Complete));
				finished = true;
			}
		}

		void Pulse(int tick)
		{
			state.CopySnapshots(snapshots);
			var resources = self.TraitOrDefault<PlayerResources>();
			if (resources == null)
				state.MarkIncomplete();
			WriteHealth(AiEconomyHealthSchema.Pulse(Identity(tick, "pulse"),
				self.Owner.WinState == WinState.Undefined, state.Complete, snapshots,
				resources?.Cash ?? 0, resources?.Resources ?? 0, resources?.ResourceCapacity ?? 0,
				resources?.Spent ?? 0));
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

		void IBotBuildQueueObserver.OnQueueTransition(in BotQueueTransition transition)
		{
			if (state == null || finished)
				return;
			WriteRaw("queue-observation", transition.Tick, new
			{
				item = transition.ItemId, queue = transition.Queue, producer = transition.ProducerActorId,
				episode = transition.EpisodeId, kind = transition.Kind.ToString(), reason = transition.Reason.ToString(),
				player_active = transition.PlayerActive, producer_live = transition.ProducerLive,
				cancellation_class = transition.CancellationClass.ToString(), category = transition.Category.ToString()
			});
			var evidence = AiEconomyQueueEvidence.Classify(in transition);
			if (evidence == EconomyQueueEvidenceKind.Unknown)
				state.MarkIncomplete();
			else if (evidence == EconomyQueueEvidenceKind.ProvenCancellation)
			{
				var queueId = CancellationQueueId(in transition);
				if (AiEconomyQueueEvidence.TryCancellation(in transition, queueId, out var cancellation))
					WriteHealth(AiEconomyHealthSchema.Cancel(Identity(transition.Tick, "cancel"), cancellation));
				else
					state.MarkIncomplete();
			}
			// Proven Placed remains raw outcome evidence. The pinned economy consumer
			// accepts pulse/cancel/end only; queue census observes the new state separately.
		}

		string CancellationQueueId(in BotQueueTransition transition)
		{
			string id = null;
			var count = 0;
			foreach (var pair in self.World.ActorsWithTrait<ProductionQueue>())
			{
				var queue = pair.Trait;
				if (pair.Actor.Owner != self.Owner || !groups.Contains(queue.Info.Group))
					continue;
				if (++count > AiEconomyHealthState.MaximumQueues)
					return null;
				if (pair.Actor.ActorID != transition.ProducerActorId || queue.Info.Group != transition.Queue)
					continue;
				// The seam names the group, not the queue trait instance. Ambiguity is UNKNOWN.
				if (id != null)
					return null;
				id = $"{pair.Actor.ActorID}:{queue.Info.Type}:{queue.Info.Group}";
			}
			return id;
		}

		void INotifyResourceAccepted.OnResourceAccepted(Actor actor, Actor refinery, string resourceType, int count, int value)
		{
			if (state == null || finished || refinery.Owner != self.Owner)
				return;
			WriteRaw("resource-accepted", self.World.WorldTick,
				AiEconomyHealthSchema.AcceptedResourceEvidence(refinery.ActorID, resourceType, count, value));
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
			IReadOnlyList<EconomyQueueSnapshot> queues, int cash, int resources, int capacity, int netSpent)
		{
			var record = Common(id);
			record.Add("player_active", active);
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

		internal static string End(EconomyHealthIdentity id, bool complete)
		{
			var record = Common(id);
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
