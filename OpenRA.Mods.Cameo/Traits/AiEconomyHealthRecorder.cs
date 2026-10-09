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
			// Until the accepted terminal-outcome seam is restacked and verified,
			// absence of a cancellation record is not evidence of complete coverage.
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
			if (!raw.TryWrite(JsonSerializer.Serialize(new { schema = 2, seq = rawSequence++, tick, kind,
				game_uid = gameUid, player = self.Owner.InternalName, evidence = value })))
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
			// f7e1 correlates intent and removal; it does not prove a terminal cause.
			if (transition.Kind is BotQueueTransitionKind.Cancelled or BotQueueTransitionKind.Removed)
				state.MarkIncomplete();
		}

		void INotifyResourceAccepted.OnResourceAccepted(Actor actor, Actor refinery, string resourceType, int count, int value)
		{
			if (state == null || finished || refinery.Owner != self.Owner)
				return;
			WriteRaw("resource-accepted", self.World.WorldTick, new { refinery = refinery.ActorID,
				resource_type = resourceType, count, value, accepted_credit = AiEconomyHealthState.AcceptedDelivery(value),
				harvester = (uint?)null });
		}
	}
	internal readonly record struct EconomyHealthIdentity(int Sequence, int Tick, string Kind,
		string GameUid, string Player, string MapUid, string Faction, string Profile,
		bool ProfileSupported, int Dropped);

	// The runtime recorder and consumer-fit tests use the same serializer.
	internal static class AiEconomyHealthSchema
	{
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
	}

}
