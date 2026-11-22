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
	[Desc("Development-only economy health observer.")]
	public sealed class AiEconomyHealthRecorderDevInfo : TraitInfo
	{
		public override object Create(ActorInitializer init) => new AiEconomyHealthRecorderDev(init.Self);
	}

	public sealed class AiEconomyHealthRecorderDev : IBotBuildQueueObserver, INotifyResourceAccepted, ITick
	{
		readonly Actor self;
		AiEconomyHealthState state;
		AiQueuedJsonlWriter health;
		AiQueuedJsonlWriter raw;
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

		// SPEND-LEDGER-01 Step 1: record-only production attribution, one pass per Observe.
		readonly AiEconomySpendLedger spend = new();
		readonly List<ProductionItem> spendItems = new();
		readonly List<ProductionItem> spendItemsPrevious = new();
		long spentAtPulse;

		// Own-queue canonical index, rebuilt at most once per world tick on the first proven
		// cancellation: one enumeration of world production queues amortizes every cancel in
		// that tick instead of scanning per event. A queue created mid-tick after the index was
		// built resolves to null — fail-closed UNKNOWN, never a wrong binding.
		IReadOnlyDictionary<(uint Producer, string Group), string> queueIndex;

		// STAGGER_EVAL_PLAN_04 F-A: world tick at which each own building-queue head was FIRST seen Done by this ungated
		// per-tick observer (the bot's own Ready stamp is taken inside the gated bot tick). Keyed by producer, queue group and
		// item name; an entry lives only while that item is the Done head, so the map is bounded by the queue count.
		readonly Dictionary<(uint Producer, string Queue, string Item), int> trueReady = new();
		readonly List<(uint Producer, string Queue, string Item)> trueReadyKeys = new();
		readonly HashSet<(uint Producer, string Queue, string Item)> trueReadyNow = new();

		public AiEconomyHealthRecorderDev(Actor self) { this.self = self; }

		// DEV only: no orders, no RNG, no gameplay state; reads queue heads and writes this recorder's own dictionary.
		void ITick.Tick(Actor actor)
		{
			if (state == null || finished)
				return;

			var tick = self.World.WorldTick;
			trueReadyNow.Clear();
			foreach (var pair in self.World.ActorsWithTrait<ProductionQueue>())
			{
				if (pair.Actor.Owner != self.Owner || !groups.Contains(pair.Trait.Info.Group))
					continue;

				var head = pair.Trait.CurrentItem();
				if (head == null || !head.Done)
					continue;

				var key = (pair.Actor.ActorID, pair.Trait.Info.Group, head.Item);
				trueReadyNow.Add(key);
				trueReady.TryAdd(key, tick);
			}

			if (trueReady.Count == trueReadyNow.Count)
				return;

			trueReadyKeys.Clear();
			foreach (var key in trueReady.Keys)
				if (!trueReadyNow.Contains(key))
					trueReadyKeys.Add(key);

			foreach (var key in trueReadyKeys)
				trueReady.Remove(key);
		}

		internal void Activate(string uid, AiQueuedJsonlWriter healthWriter, AiQueuedJsonlWriter rawWriter)
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
			spentAtPulse = self.TraitOrDefault<PlayerResources>()?.Spent ?? 0;
			if (groups.Count == 0 || string.IsNullOrEmpty(uid))
				state.MarkIncomplete();
		}

		internal void Observe(int tick, bool terminal = false)
		{
			if (state == null || finished)
				return;
			FlushAccepted(tick, terminal);
			observations.Clear();
			spendItems.Clear();
			foreach (var pair in self.World.ActorsWithTrait<ProductionQueue>())
			{
				var queue = pair.Trait;
				if (pair.Actor.Owner != self.Owner)
					continue;
				// Every own production queue feeds the spend ledger (units, aircraft, research, buildings);
				// only the health observations below are filtered by group.
				spendItems.AddRange(queue.AllQueued());
				if (!groups.Contains(queue.Info.Group))
					continue;
				if (observations.Count == AiEconomyHealthState.MaximumQueues)
				{
					state.MarkIncomplete();
					spend.MarkIncomplete();
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
			SpendObserve();
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
			WriteSpendLedger(tick, resources);
			lastPulse = tick;
		}

		void SpendObserve()
		{
			var now = new HashSet<ProductionItem>(spendItems, ReferenceEqualityComparer.Instance);
			var removed = spendItemsPrevious.Where(i => !now.Contains(i)).Select(SpendSample).ToList();
			spend.Observe(spendItems.Select(SpendSample), removed);
			spendItemsPrevious.Clear();
			spendItemsPrevious.AddRange(spendItems);
		}

		static SpendItemSample SpendSample(ProductionItem item) => new(item, item.TotalCost, item.RemainingCost);

		void WriteSpendLedger(int tick, PlayerResources resources)
		{
			var spent = resources?.Spent ?? 0;
			var netSpentDelta = (long)spent - spentAtPulse;
			spentAtPulse = spent;
			var production = spend.TakePulse();
			var complete = resources != null && spent != int.MinValue && spend.Complete;
			WriteRaw("spend_ledger", tick, AiEconomySpendLedger.Evidence(lastPulse, tick, production, netSpentDelta, complete));
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
			if (transition.Kind is BotQueueTransitionKind.Ready or BotQueueTransitionKind.PlacementOrdered)
			{
				// F-A: true_ready_tick = first ungated observation of Done (-1 when this observer never saw it Done).
				var trueTick = trueReady.TryGetValue((transition.ProducerActorId, transition.Queue, transition.ItemId), out var seen) ? seen : -1;
				WriteRaw("queue-observation", transition.Tick, new
				{
					item = transition.ItemId, queue = transition.Queue, producer = transition.ProducerActorId,
					episode = transition.EpisodeId, kind = transition.Kind.ToString(), reason = transition.Reason.ToString(),
					player_active = transition.PlayerActive, producer_live = transition.ProducerLive,
					cancellation_class = transition.CancellationClass.ToString(), category = transition.Category.ToString(),
					true_ready_tick = trueTick
				});
			}
			else
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
}
