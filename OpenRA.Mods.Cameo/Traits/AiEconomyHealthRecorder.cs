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
				var status = head == null ? "idle" : head.Done ? "ready" : head.Paused ? "paused" : "producing";
				observations.Add(new EconomyQueueObservation(queue, head,
					$"{pair.Actor.ActorID}:{queue.Info.Type}:{queue.Info.Group}", head?.Item ?? "", status,
					!pair.Actor.IsDead && !pair.Actor.Disposed && queue.Enabled,
					queue.Enabled ? "observed" : "producer-disabled"));
			}
			state.Observe(tick, observations);
			if ((AiEconomyHealthState.PulseDue(tick) || terminal) && tick != lastPulse)
				Pulse(tick);
			if (terminal)
			{
				WriteHealth("end", tick, new { complete = state.Complete && health.Complete && raw.Complete });
				finished = true;
				health.Dispose();
				raw.Dispose();
			}
		}

		void Pulse(int tick)
		{
			state.CopySnapshots(snapshots);
			var resources = self.TraitOrDefault<PlayerResources>();
			if (resources == null)
				state.MarkIncomplete();
			WriteHealth("pulse", tick, new
			{
				player_active = self.Owner.WinState == WinState.Undefined,
				queues_complete = state.Complete,
				queues = snapshots.Select(q => new { queue_id = q.QueueId, item_id = q.ItemId, item = q.Item,
					state = q.State, state_since_tick = q.StateSinceTick, producer_live = q.ProducerLive, reason = q.Reason }),
				cash = resources?.Cash ?? 0, resources = resources?.Resources ?? 0,
				capacity = resources?.ResourceCapacity ?? 0, net_spent = resources?.Spent ?? 0,
				gross_spent = (int?)null, gross_spend_complete = false
			});
			lastPulse = tick;
		}

		void WriteHealth(string kind, int tick, object fields)
		{
			var record = JsonSerializer.SerializeToElement(fields);
			var data = new Dictionary<string, object>
			{
				["schema"] = 2, ["seq"] = sequence++, ["tick"] = tick, ["kind"] = kind,
				["game_uid"] = gameUid, ["player"] = self.Owner.InternalName,
				["map_uid"] = self.World.Map.Uid, ["faction"] = self.Owner.Faction.InternalName,
				["profile"] = self.Owner.BotType ?? "unknown",
				["profile_supported"] = groups.Count > 0,
				["dropped"] = state.Complete && health.Complete && raw.Complete ? 0 : 1
			};
			foreach (var field in record.EnumerateObject())
				data.Add(field.Name, field.Value);
			if (!health.TryWrite(JsonSerializer.Serialize(data)))
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
}
