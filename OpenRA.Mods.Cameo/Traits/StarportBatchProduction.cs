#region Copyright & License Information
/*
 * Cameo mod
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits
{
	[TraitLocation(SystemActors.Player)]
	[Desc("Player-owned rolling production queue that seals completed units into FIFO Starport delivery batches.")]
	public class StarportBatchProductionQueueInfo : ParallelProductionQueueInfo, Requires<TechTreeInfo>, Requires<PlayerResourcesInfo>
	{
		[Desc("Maximum number of Starport orders that advance independently each tick.")]
		public readonly int ParallelProductionSlots = 1;

		[Desc("Ticks after the first completed unit before the collecting batch is sealed.")]
		public readonly int CollectionDelay = 875;

		[Desc("Ticks between sealing a batch and dispatching its frigate.")]
		public readonly int DispatchDelay = 250;

		[Desc("Maximum number of units in one delivery batch.")]
		public readonly int MaxBatchSize = 20;

		[Desc("Maximum number of sealed batches waiting for a frigate. The active and collecting batches are excluded.")]
		public readonly int MaxPendingBatches = 3;

		public override object Create(ActorInitializer init)
		{
			if (ParallelProductionSlots < 1)
				throw new YamlException("StarportBatchProductionQueue ParallelProductionSlots must be at least 1.");

			return new StarportBatchProductionQueue(init, this);
		}
	}

	sealed class StarportBatchCandidate
	{
		public readonly ActorInfo Actor;
		public readonly string Faction;
		public readonly int Resources;
		public readonly int Cash;

		public StarportBatchCandidate(ActorInfo actor, string faction, int resources, int cash)
		{
			Actor = actor;
			Faction = faction;
			Resources = resources;
			Cash = cash;
		}
	}

	sealed class StarportDeliveryBatch
	{
		public readonly int Id;
		public readonly List<StarportBatchCandidate> Candidates;
		public readonly int DispatchTick;

		public StarportDeliveryBatch(int id, List<StarportBatchCandidate> candidates, int dispatchTick)
		{
			Id = id;
			Candidates = candidates;
			DispatchTick = dispatchTick;
		}
	}

	public class StarportBatchProductionQueue : ParallelProductionQueue, IBuildLimitReservations
	{
		static readonly ActorInfo[] NoItems = [];

		readonly StarportBatchProductionQueueInfo info;
		readonly List<StarportBatchCandidate> collecting = [];
		readonly LinkedList<StarportDeliveryBatch> pending = [];
		readonly List<TraitPair<StarportBatchAirdrop>> producers = [];

		StarportDeliveryBatch activeBatch;

		[VerifySync]
		int collectionRemaining = -1;

		[VerifySync]
		int nextBatchId;

		[VerifySync]
		int stateHash;

		[VerifySync]
		int dispatchRetryRemaining;

		public int CollectionRemaining => collectionRemaining;
		public int CollectingCount => collecting.Count;
		public int MaxBatchSize => info.MaxBatchSize;
		public int PendingBatchCount => pending.Count;
		public int ActiveBatchCount => activeBatch?.Candidates.Count ?? 0;
		bool CollectionWindowExpired => collecting.Count != 0 && collectionRemaining == 0;

		public int DispatchRemaining
		{
			get
			{
				if (pending.First == null)
					return -1;

				return Math.Max(0, pending.First.Value.DispatchTick - Actor.World.WorldTick);
			}
		}

		public StarportBatchProductionQueue(ActorInitializer init, StarportBatchProductionQueueInfo info)
			: base(init, info)
		{
			this.info = info;
		}

		protected override void Tick(Actor self)
		{
			RefreshProducers();
			Enabled = IsValidFaction && producers.Count != 0;

			if (!Enabled)
			{
				ClearQueue();
				CancelAllBatches();
				UpdateStateHash();
				return;
			}

			if (collectionRemaining > 0 && !developerMode.FastBuild)
				collectionRemaining--;
			else if (collectionRemaining > 0)
				collectionRemaining = 0;

			if (collectionRemaining == 0)
				TrySealCollection();

			if (dispatchRetryRemaining > 0)
				dispatchRetryRemaining--;

			TryDispatch();
			TickInner(self, producers.All(p => p.Trait.IsTraitPaused));
			UpdateStateHash();
		}

			protected override void TickInner(Actor self, bool allProductionPaused)
			{
				CancelUnbuildableItems();

				if (allProductionPaused)
					return;

				// Match the independent timers used by LarvaProductionQueue, but use
				// configured slots instead of physical larvae. Waiting items do not
				// start their timer until an earlier active slot becomes available.
				var activeSlots = 0;
				foreach (var item in Queue.ToArray())
				{
					if (item.Paused)
						continue;

					if (activeSlots++ >= info.ParallelProductionSlots)
						break;

					// Do not take the final payment or mark the item complete while
					// there is nowhere to put it. A completed item retains its slot
					// until it can enter the collection batch.
					if ((item.Done || (item.Started && item.RemainingTime == 1)) && !CanAcceptCandidate())
						continue;

					item.Tick(playerResources);
				}
			}

			public override bool IsProducing(ProductionItem item)
			{
				var activeSlots = 0;
				foreach (var candidate in Queue)
				{
					if (candidate.Paused)
						continue;

					if (activeSlots++ >= info.ParallelProductionSlots)
						return false;

					if (candidate == item)
						return true;
				}

				return false;
			}

			public override int RemainingTimeActual(ProductionItem item)
			{
				return item.RemainingTimeActual;
			}

			protected override void BeginProduction(ProductionItem item, bool hasPriority)
			{
				base.BeginProduction(item, hasPriority);

				if (Info.InfiniteBuildLimit < 0 || !Queue.Any(i => i.Item == item.Item && i.Infinite))
					return;

				// Infinite production must keep every configured slot occupied instead
				// of collapsing back to the stock single repeating item.
				var totalOfType = Queue.Count(i => i.Item == item.Item);
				for (var i = totalOfType; i < info.ParallelProductionSlots; i++)
					Queue.Add(new ProductionItem(this, item.Item, item.TotalCost, playerPower, item.OnComplete) { Infinite = true });
			}

			protected override void CancelProduction(string itemName, uint numberToCancel)
			{
				var infiniteItems = Queue.Where(i => i.Item == itemName && i.Infinite).ToList();
				if (infiniteItems.Count > 0)
				{
					foreach (var item in infiniteItems)
						item.Infinite = false;

					// Base cancellation handles and refunds the first item. Remove the
					// extra parallel infinite items atomically so none can restart.
					foreach (var extra in infiniteItems.Skip(1))
					{
						if (extra.ResourcesPaid > 0)
						{
							playerResources.GiveResources(extra.ResourcesPaid);
							extra.RemainingCost += extra.ResourcesPaid;
						}

						playerResources.GiveCash(extra.TotalCost - extra.RemainingCost);
						Queue.Remove(extra);
					}
				}

				base.CancelProduction(itemName, numberToCancel);
			}

		public override IEnumerable<ActorInfo> AllItems()
		{
			return Enabled ? base.AllItems() : NoItems;
		}

		public override IEnumerable<ActorInfo> BuildableItems()
		{
			return Enabled ? base.BuildableItems() : NoItems;
		}

		public override TraitPair<Production> MostLikelyProducer()
		{
			var producer = FindProducer();
			return producer.Trait == null
				? default
				: new TraitPair<Production>(producer.Actor, producer.Trait);
		}

		public int ReservedCount(ActorInfo actor)
		{
			var count = collecting.Count(c => c.Actor == actor);
			foreach (var batch in pending)
				count += batch.Candidates.Count(c => c.Actor == actor);
			if (activeBatch != null)
				count += activeBatch.Candidates.Count(c => c.Actor == actor);
			return count;
		}

		protected override bool BuildUnit(ActorInfo unit)
		{
			if (!CanAcceptCandidate())
				return false;

			var item = Queue.FirstOrDefault(i => i.Done && i.Item == unit.Name);
			if (item == null)
				return false;

			if (collecting.Count == 0)
				collectionRemaining = info.CollectionDelay;

			collecting.Add(new StarportBatchCandidate(unit, Faction, item.ResourcesPaid, item.TotalCost - item.ResourcesPaid));
			EndProduction(item);

			if (collecting.Count == info.MaxBatchSize)
				TrySealCollection();

			return true;
		}

		bool CanAcceptCandidate()
		{
			// Once the collection window expires, keep later completions in the
			// production queue until this batch can claim a pending slot.
			if (CollectionWindowExpired)
				return false;

			if (collecting.Count >= info.MaxBatchSize)
				return false;

			return collecting.Count + 1 < info.MaxBatchSize || pending.Count < info.MaxPendingBatches;
		}

		bool TrySealCollection()
		{
			if (collecting.Count == 0)
			{
				collectionRemaining = -1;
				return true;
			}

			if (pending.Count >= info.MaxPendingBatches)
				return false;

			pending.AddLast(new StarportDeliveryBatch(
				nextBatchId++,
				[.. collecting],
				Actor.World.WorldTick + info.DispatchDelay));
			collecting.Clear();
			collectionRemaining = -1;
			return true;
		}

		void TryDispatch()
		{
			if (activeBatch != null || pending.First == null || dispatchRetryRemaining > 0)
				return;

			var batch = pending.First.Value;
			if (batch.DispatchTick > Actor.World.WorldTick)
				return;

			var producer = FindProducer(producee: batch.Candidates[0].Actor, requireFreeExit: true);
			if (producer.Trait == null)
			{
				foreach (var p in producers)
					if (!p.Trait.IsTraitPaused)
						p.Trait.NotifyBlockedExits(p.Actor, batch.Candidates[0].Actor, Info.Type);
				dispatchRetryRemaining = producers.Count == 0
					? 1
					: producers.Min(p => Math.Max(1, p.Trait.Info.BlockedRetryDelay));
				return;
			}

			dispatchRetryRemaining = 0;
			pending.RemoveFirst();
			activeBatch = batch;
			if (!producer.Trait.DeliverBatch(producer.Actor, batch.Id, this))
			{
				activeBatch = null;
				pending.AddFirst(batch);
			}
		}

		void RefreshProducers()
		{
			producers.Clear();
			foreach (var p in Actor.World.ActorsWithTrait<StarportBatchAirdrop>())
				if (p.Actor.Owner == Actor.Owner &&
					p.Trait.IsAvailable(p.Actor) &&
					p.Trait.Info.Produces.Contains(Info.Type))
					producers.Add(p);
		}

		internal TraitPair<StarportBatchAirdrop> FindProducer(
			Actor exclude = null,
			ActorInfo producee = null,
			bool requireFreeExit = false,
			bool allowPaused = false)
		{
			TraitPair<StarportBatchAirdrop> SelectProducer(IEnumerable<TraitPair<StarportBatchAirdrop>> choices)
			{
				return choices
					.Where(p => p.Actor.Owner == Actor.Owner &&
						p.Trait.IsAvailable(p.Actor) &&
						p.Actor != exclude &&
						(allowPaused || !p.Trait.IsTraitPaused) &&
						p.Trait.Info.Produces.Contains(Info.Type))
					.Where(p => !requireFreeExit || producee == null ||
						p.Trait.HasAvailableExit(p.Actor, producee, Info.Type))
					.OrderBy(p => p.Trait.IsTraitPaused)
					.ThenByDescending(p => p.Actor.TraitOrDefault<PrimaryBuilding>()?.IsPrimary == true)
					.ThenByDescending(p => p.Actor.ActorID)
					.FirstOrDefault();
			}

			var producer = SelectProducer(producers);
			return producer.Trait != null
				? producer
				: SelectProducer(Actor.World.ActorsWithTrait<StarportBatchAirdrop>());
		}

		internal StarportBatchCandidate FirstCandidate(int batchId)
		{
			return activeBatch != null && activeBatch.Id == batchId && activeBatch.Candidates.Count != 0
				? activeBatch.Candidates[0]
				: null;
		}

		internal bool IsActiveBatch(int batchId)
		{
			return activeBatch != null && activeBatch.Id == batchId;
		}

		internal void CandidateDelivered(int batchId)
		{
			if (!IsActiveBatch(batchId) || activeBatch.Candidates.Count == 0)
				return;

			activeBatch.Candidates.RemoveAt(0);
		}

		internal void DeliveryFinished(int batchId)
		{
			if (!IsActiveBatch(batchId) || activeBatch.Candidates.Count != 0)
				return;

			activeBatch = null;
		}

		internal void DeliveryFailed(int batchId)
		{
			if (!IsActiveBatch(batchId))
				return;

			var failed = activeBatch;
			activeBatch = null;
			Refund(failed.Candidates);
		}

		internal void DeliveryDeferred(int batchId, int retryDelay)
		{
			if (!IsActiveBatch(batchId))
				return;

			var deferred = activeBatch;
			activeBatch = null;
			pending.AddFirst(deferred);
			dispatchRetryRemaining = Math.Max(dispatchRetryRemaining, Math.Max(1, retryDelay));
		}

		void CancelAllBatches()
		{
			if (collecting.Count == 0 && pending.Count == 0 && activeBatch == null)
				return;

			var refund = new List<StarportBatchCandidate>(collecting);
			foreach (var batch in pending)
				refund.AddRange(batch.Candidates);
			if (activeBatch != null)
				refund.AddRange(activeBatch.Candidates);

			collecting.Clear();
			pending.Clear();
			activeBatch = null;
			collectionRemaining = -1;
			dispatchRetryRemaining = 0;
			Refund(refund);
		}

		void Refund(IEnumerable<StarportBatchCandidate> candidates)
		{
			foreach (var candidate in candidates)
			{
				playerResources.RefundResources(candidate.Resources);
				playerResources.RefundCash(candidate.Cash);
			}
		}

		void UpdateStateHash()
		{
			unchecked
			{
				var hash = collectionRemaining;
				hash = hash * 31 + collecting.Count;
				hash = hash * 31 + pending.Count;
				hash = hash * 31 + (activeBatch?.Id ?? -1);
				hash = hash * 31 + (activeBatch?.Candidates.Count ?? 0);
				hash = hash * 31 + dispatchRetryRemaining;
				foreach (var candidate in collecting)
					hash = HashCandidate(hash, candidate);
				foreach (var batch in pending)
				{
					hash = hash * 31 + batch.Id;
					hash = hash * 31 + batch.DispatchTick;
					hash = hash * 31 + batch.Candidates.Count;
					foreach (var candidate in batch.Candidates)
						hash = HashCandidate(hash, candidate);
				}

				if (activeBatch != null)
					foreach (var candidate in activeBatch.Candidates)
						hash = HashCandidate(hash, candidate);

				stateHash = hash;
			}
		}

		static int HashCandidate(int hash, StarportBatchCandidate candidate)
		{
			unchecked
			{
				foreach (var c in candidate.Actor.Name)
					hash = hash * 31 + c;
				foreach (var c in candidate.Faction)
					hash = hash * 31 + c;
				hash = hash * 31 + candidate.Resources;
				return hash * 31 + candidate.Cash;
			}
		}
	}

	[Desc("Delivers sealed Starport batches with a single frigate and unloads them through available exits.")]
	public class StarportBatchAirdropInfo : ProductionInfo
	{
		[FieldLoader.Require]
		[ActorReference(typeof(AircraftInfo))]
		[Desc("Cargo aircraft used for delivery.")]
		public readonly string ActorType = null;

		[Desc("Spawn the frigate from the player baseline.")]
		public readonly bool BaselineSpawn = true;

		[Desc("Direction the frigate should face while landing.")]
		public readonly WAngle Facing = new(256);

		[Desc("Offset used for landing at the Starport.")]
		public readonly WVec LandOffset = WVec.Zero;

		[Desc("Ticks to wait after landing before the first unit is created.")]
		public readonly int BeforeUnloadDelay = 8;

		[Desc("Ticks between successfully created units.")]
		public readonly int BetweenUnloadDelay = 5;

		[Desc("Ticks between retries while every exit is blocked.")]
		public readonly int BlockedRetryDelay = 5;

		[Desc("Ticks to wait after unloading before leaving.")]
		public readonly int AfterUnloadDelay = 25;

		[NotificationReference("Speech")]
		[Desc("Speech notification to play when a frigate begins unloading.")]
		public readonly string ReadyAudio = "Reinforce";

		[FluentReference(optional: true)]
		[Desc("Text notification to display when a frigate begins unloading.")]
		public readonly string ReadyTextNotification = null;

		public override object Create(ActorInitializer init) { return new StarportBatchAirdrop(init, this); }
	}

	public class StarportBatchAirdrop : Production, INotifySold, INotifyTransform, INotifyActorDisposing
	{
		RallyPoint rallyPoint;

		[VerifySync]
		int exitCursor;

		[VerifySync]
		bool unavailable;

		public new StarportBatchAirdropInfo Info => (StarportBatchAirdropInfo)base.Info;

		public StarportBatchAirdrop(ActorInitializer init, StarportBatchAirdropInfo info)
			: base(init, info) { }

		protected override void Created(Actor self)
		{
			rallyPoint = self.TraitOrDefault<RallyPoint>();
			base.Created(self);
		}

		public bool IsAvailable(Actor self)
		{
			return !unavailable && self.IsInWorld && !self.IsDead && !self.WillDispose && !IsTraitDisabled;
		}

		void INotifySold.Selling(Actor self) { unavailable = true; }
		void INotifySold.Sold(Actor self) { }
		void INotifyTransform.BeforeTransform(Actor self) { unavailable = true; }
		void INotifyTransform.OnTransform(Actor self) { }
		void INotifyTransform.AfterTransform(Actor toActor) { }
		void INotifyActorDisposing.Disposing(Actor self) { unavailable = true; }

		public bool HasAvailableExit(Actor self, ActorInfo producee, string productionType)
		{
			return AvailableExits(self, producee, productionType).Any();
		}

		public void NotifyBlockedExits(Actor self, ActorInfo producee, string productionType)
		{
			if (HasAvailableExit(self, producee, productionType))
				return;

			foreach (var exit in self.Exits(productionType))
				self.NotifyBlocker(self.Location + exit.Info.ExitCell);
		}

		internal Exit SelectAvailableExit(Actor self, ActorInfo producee, string productionType)
		{
			var exits = AvailableExits(self, producee, productionType).ToArray();
			if (exits.Length == 0)
				return null;

			var exit = exits[exitCursor % exits.Length];
			exitCursor++;
			return exit;
		}

		IEnumerable<Exit> AvailableExits(Actor self, ActorInfo producee, string productionType)
		{
			if (!IsAvailable(self))
				return [];

			var mobileInfo = producee.TraitInfoOrDefault<MobileInfo>();
			return self.Exits(productionType)
				.OrderByDescending(e => e.Info.Priority)
				.ThenBy(e => e.Info.ExitCell.X)
				.Where(e => mobileInfo == null ||
					mobileInfo.CanEnterCell(self.World, self, self.Location + e.Info.ExitCell, ignoreActor: self));
		}

		public bool DeliverBatch(Actor producer, int batchId, StarportBatchProductionQueue queue)
		{
			if (!IsAvailable(producer) || IsTraitPaused)
				return false;

			var owner = producer.Owner;
			var map = owner.World.Map;
			var aircraftInfo = map.Rules.Actors[Info.ActorType].TraitInfo<AircraftInfo>();
			CPos startPos;
			WAngle spawnFacing;

			if (Info.BaselineSpawn)
			{
				var bounds = map.Bounds;
				var center = new MPos(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2).ToCPos(map);
				var spawnVec = owner.HomeLocation - center;
				startPos = spawnVec.LengthSquared == 0
					? map.ChooseClosestEdgeCell(producer.Location)
					: owner.HomeLocation + spawnVec *
						Exts.ISqrt((bounds.Height * bounds.Height + bounds.Width * bounds.Width) / (4 * spawnVec.LengthSquared));
				spawnFacing = new WVec((producer.Location - startPos).X, (producer.Location - startPos).Y, 0).Yaw;
			}
			else
			{
				startPos = map.ChooseClosestEdgeCell(producer.Location);
				spawnFacing = map.FacingBetween(startPos, producer.Location, WAngle.Zero);
			}

			owner.World.AddFrameEndTask(w =>
			{
				if (!queue.IsActiveBatch(batchId))
					return;

				var candidate = queue.FirstCandidate(batchId);
				if (candidate == null)
					return;

				var target = producer;
				var targetTrait = this;
				if (producer.Owner != owner ||
					!IsAvailable(producer) ||
					IsTraitPaused ||
					!HasAvailableExit(producer, candidate.Actor, queue.Info.Type))
				{
					var replacement = queue.FindProducer(
						producee: candidate.Actor,
						requireFreeExit: true);
					if (replacement.Trait == null)
					{
						queue.DeliveryDeferred(batchId, Info.BlockedRetryDelay);
						return;
					}

					target = replacement.Actor;
					targetTrait = replacement.Trait;
				}

				targetTrait.BeginDelivery(target);
				var transport = w.CreateActor(Info.ActorType,
				[
					new CenterPositionInit(w.Map.CenterOfCell(startPos) +
						new WVec(WDist.Zero, WDist.Zero, aircraftInfo.CruiseAltitude)),
					new OwnerInit(owner),
					new FacingInit(spawnFacing)
				]);

				transport.QueueActivity(new DeliverStarportBatch(
					transport, target, targetTrait, batchId, queue, deliveryStarted: true));
			});

			return true;
		}

		internal void BeginDelivery(Actor producer)
		{
			foreach (var notify in producer.TraitsImplementing<INotifyDelivery>())
				notify.IncomingDelivery(producer);
		}

		internal void EndDelivery(Actor producer)
		{
			foreach (var notify in producer.TraitsImplementing<INotifyDelivery>())
				notify.Delivered(producer);
		}

		internal void ScheduleCandidate(
			Actor producer,
			StarportBatchCandidate candidate,
			Exit exit,
			string productionType,
			Func<bool> canSpawn,
			Action<bool> completed)
		{
			var expectedOwner = producer.Owner;
			producer.World.AddFrameEndTask(w =>
			{
				if (!canSpawn() ||
					!IsAvailable(producer) ||
					IsTraitPaused ||
					producer.Owner != expectedOwner ||
					!AvailableExits(producer, candidate.Actor, productionType).Contains(exit))
				{
					completed(false);
					return;
				}

				var exitCell = producer.Location + exit.Info.ExitCell;
				var spawn = producer.CenterPosition + exit.Info.SpawnOffset;
				var to = w.Map.CenterOfCell(exitCell);
				WAngle initialFacing;
				if (exit.Info.Facing.HasValue)
					initialFacing = exit.Info.Facing.Value;
				else
				{
					var delta = to - spawn;
					var facing = candidate.Actor.TraitInfoOrDefault<IFacingInfo>();
					initialFacing = delta.HorizontalLengthSquared == 0 && facing != null
						? facing.GetInitialFacing()
						: delta.Yaw;
				}

				var destinations = rallyPoint != null && rallyPoint.Path.Count > 0
					? rallyPoint.Path.ToArray()
					: [exitCell];
				var inits = new TypeDictionary
				{
					new OwnerInit(producer.Owner),
					new FactionInit(BuildableInfo.GetInitialFaction(candidate.Actor, candidate.Faction)),
					new LocationInit(exitCell),
					new CenterPositionInit(spawn),
					new FacingInit(initialFacing),
					new CreationActivityDelayInit(exit.Info.ExitDelay),
					new RallyPointInit(destinations)
				};

				var buildable = BuildableInfo.GetTraitForQueue(candidate.Actor, productionType);
				var produced = new List<Actor>();
				if (buildable == null)
					produced.Add(w.CreateActor(candidate.Actor.Name, inits));
				else
				{
					for (var n = 0; n < buildable.BuildAmount; n++)
					{
						produced.Add(w.CreateActor(candidate.Actor.Name, inits));
						foreach (var additional in buildable.AdditionalActors)
							produced.Add(w.CreateActor(additional.ToLowerInvariant(), inits));
					}
				}

				foreach (var unit in produced)
				{
					foreach (var notify in producer.TraitsImplementing<INotifyProduction>())
						notify.UnitProduced(producer, unit, exitCell);

					foreach (var notify in w.ActorsWithTrait<INotifyOtherProduction>())
						notify.Trait.UnitProducedByOther(notify.Actor, producer, unit, productionType, inits);
				}

				completed(true);
			});
		}
	}

	sealed class DeliverStarportBatch : Activity
	{
		Actor producer;
		StarportBatchAirdrop production;
		readonly int batchId;
		readonly StarportBatchProductionQueue queue;
		bool spawnScheduled;
		bool deliveryStarted;
		bool readyToUnload;
		bool arrivalNotified;
		bool finishing;
		int retryDelay;

		public DeliverStarportBatch(
			Actor transport,
			Actor producer,
			StarportBatchAirdrop production,
			int batchId,
			StarportBatchProductionQueue queue,
			bool deliveryStarted)
		{
			this.producer = producer;
			this.production = production;
			this.batchId = batchId;
			this.queue = queue;
			this.deliveryStarted = deliveryStarted;
			ChildHasPriority = false;
		}

		protected override void OnFirstRun(Actor self)
		{
			QueueApproach(self, waitBeforeUnload: true);
		}

		void QueueApproach(Actor self, bool waitBeforeUnload)
		{
			readyToUnload = false;
			ChildActivity?.Cancel(self);
			QueueChild(new Land(self, Target.FromActor(producer), WDist.Zero, production.Info.LandOffset, production.Info.Facing));
			if (waitBeforeUnload && production.Info.BeforeUnloadDelay > 0)
				QueueChild(new Wait(production.Info.BeforeUnloadDelay));
			QueueChild(new CallFunc(() => readyToUnload = true));
		}

		protected override void OnLastRun(Actor self)
		{
			EndDelivery();

			if (production.Info.AfterUnloadDelay > 0)
				Queue(new Wait(production.Info.AfterUnloadDelay));
			Queue(new FlyOffMap(self, Target.FromCell(self.World, self.World.Map.ChooseClosestEdgeCell(self.Location))));
			Queue(new RemoveSelf());
		}

		protected override void OnActorDispose(Actor self)
		{
			EndDelivery();
			queue.DeliveryFailed(batchId);
		}

		void EndDelivery()
		{
			if (!deliveryStarted)
				return;

			if (!producer.Disposed)
				production.EndDelivery(producer);
			deliveryStarted = false;
		}

		void Retarget(Actor self, TraitPair<StarportBatchAirdrop> replacement, bool waitBeforeUnload)
		{
			EndDelivery();
			producer = replacement.Actor;
			production = replacement.Trait;
			production.BeginDelivery(producer);
			deliveryStarted = true;
			QueueApproach(self, waitBeforeUnload);
		}

		bool FinishAfterChildren(Actor self)
		{
			if (!finishing)
			{
				finishing = true;
				ChildActivity?.Cancel(self);
			}

			return TickChild(self);
		}

		void NotifyArrival(Actor self)
		{
			if (arrivalNotified)
				return;

			arrivalNotified = true;
			Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech",
				production.Info.ReadyAudio, self.Owner.Faction.InternalName);
			TextNotificationsManager.AddTransientLine(self.Owner, production.Info.ReadyTextNotification);
		}

		public override bool Tick(Actor self)
		{
			if (finishing)
				return TickChild(self);

			if (!queue.IsActiveBatch(batchId))
				return FinishAfterChildren(self);

			var candidate = queue.FirstCandidate(batchId);
			if (candidate == null)
			{
				queue.DeliveryFinished(batchId);
				return FinishAfterChildren(self);
			}

			if (producer.Owner != self.Owner || !production.IsAvailable(producer))
			{
				var replacement = queue.FindProducer(allowPaused: true);
				if (replacement.Trait == null)
				{
					queue.DeliveryFailed(batchId);
					return FinishAfterChildren(self);
				}

				Retarget(self, replacement, waitBeforeUnload: true);
				return false;
			}

			if (!TickChild(self) || !readyToUnload)
				return false;

			if (production.IsTraitPaused || spawnScheduled)
				return false;

			if (retryDelay > 0)
			{
				retryDelay--;
				return false;
			}

			var exit = production.SelectAvailableExit(producer, candidate.Actor, queue.Info.Type);
			if (exit == null)
			{
				production.NotifyBlockedExits(producer, candidate.Actor, queue.Info.Type);
				var replacement = queue.FindProducer(producer, candidate.Actor, requireFreeExit: true);
				if (replacement.Trait != null)
					Retarget(self, replacement, waitBeforeUnload: false);

				retryDelay = production.Info.BlockedRetryDelay;
				return false;
			}

			NotifyArrival(self);
			spawnScheduled = true;
			production.ScheduleCandidate(
				producer,
				candidate,
				exit,
				queue.Info.Type,
				() => queue.IsActiveBatch(batchId) && queue.FirstCandidate(batchId) == candidate,
				success =>
				{
					spawnScheduled = false;
					if (success)
					{
						queue.CandidateDelivered(batchId);
						retryDelay = production.Info.BetweenUnloadDelay;
					}
					else
						retryDelay = production.Info.BlockedRetryDelay;
				});

			return false;
		}
	}
}
