#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Xml.Linq;
using OpenRA.Mods.AS.Traits;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Radar;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	class BaseBuilderQueueManagerCA
	{
		public readonly string Category;
		public int WaitTicks;

		readonly BaseBuilderBotModuleCA baseBuilder;
		readonly World world;
		readonly Player player;
		readonly PowerManager playerPower;
		readonly PlayerResources playerResources;
		readonly IResourceLayer resourceLayer;

		Actor[] playerBuildings;
		int failCount;
		int failRetryTicks;
		string lastFailedBuilding;
		int checkForBasesTicks;
		int cachedBases;

		// FIX-RA-REFINERY R1: the latch keeps its own baseline — cachedBases belongs to the water
		// check and was being clobbered by it, and the old capture site was guarded by
		// `BaseExpansionModules == null` which never runs (the array is never null).
		int latchedBuildings;
		int latchedProviders;
		int minimumExcessPower;
		int minCashRequirement;
		CPos? baseCenterKeepsFailing = null;

		// ECON-A: ChooseBuildLocation set this when an expansion-demand cell came from the
		// defence-placement advisor — TickQueue's advisedDefense local predates the demand path
		// and can't be reached from inside the helper. Folded into the same telemetry flag.
		bool demandAdvisedDefense = false;

		// ECON-A-FIX (R5/R6): the demand pick deferred until the StartProduction order is actually
		// admitted — binding at pick time lets a veto (cash floor, army-first) strand a bound item
		// that never queues, and an anchor may only be reserved once the order is going out.
		(ExpansionDemand Demand, bool IsRefinery, RefineryAnchorClaim? Claim) pendingDemandPick;

		bool itemQueuedThisTick = false;

		// An empty tolerance list in yaml would make ImmutableArray.Random throw.
		int RandomTolerance(ImmutableArray<int> values)
		{
			return values.IsDefaultOrEmpty ? 0 : values.Random(BotRng.For(player));
		}
		bool limitBuildRadius = false;

		WaterCheck waterState = WaterCheck.NotChecked;
		readonly Dictionary<string, int> activeBuildingIntervals = new Dictionary<string, int>();

		BotLimits botLimits;
		int productionTypeLimit = 0;
		int buildingDelayModifier = 100;
		int buildingIntervalModifier = 100;

		// Cameo (§12.20): lazy — resolved on first TickQueue, an OR-of-vetoes pause on new building
		// production. Empty = nothing pauses, upstream-identical.
		IBotRequestPauseBuildingProduction[] pauseBuilding;

		// REPLAY-HEALTH-LOGGER (ACK-2, task 01a12023): record-only queue-transition dispatch.
		// Everything here is lazy — with no observer trait on the PlayerActor the seam is one
		// null check per Tick and no state is ever allocated (Sol review F5).
		IBotBuildQueueObserver[] queueObservers;
		BotQueueEpisodeTracker queueEpisodes;
		Dictionary<ProductionQueue, BotQueueWatch> queueWatches;
		HashSet<ProductionItem> queueProbeScratch;

		public BaseBuilderQueueManagerCA(BaseBuilderBotModuleCA baseBuilder, string category, Player p, PowerManager pm,
			PlayerResources pr, IResourceLayer rl)
		{
			this.baseBuilder = baseBuilder;
			world = p.World;
			player = p;
			playerPower = pm;
			playerResources = pr;
			resourceLayer = rl;
			Category = category;
			failRetryTicks = baseBuilder.Info.StructureProductionResumeDelay;
			minimumExcessPower = baseBuilder.Info.MinimumExcessPower;
			minCashRequirement = baseBuilder.Info.DefenseQueues.Contains(Category) ? baseBuilder.Info.DefenseProductionMinCashRequirement : baseBuilder.Info.BuildingProductionMinCashRequirement;
			if (baseBuilder.Info.NavalProductionTypes.Count == 0)
				waterState = WaterCheck.DontCheck;
			limitBuildRadius = world.WorldActor.TraitOrDefault<MapBuildRadius>().BuildRadiusEnabled;
		}

		public void SetBotLimits(BotLimits limits)
		{
			botLimits = limits;
			if (botLimits == null)
			{
				productionTypeLimit = 0;
				buildingDelayModifier = 100;
				buildingIntervalModifier = 100;
				return;
			}

			productionTypeLimit = botLimits.Info.ProductionTypeLimit;
			buildingDelayModifier = botLimits.Info.BuildingDelayModifier;
			buildingIntervalModifier = botLimits.Info.BuildingIntervalModifier;
		}

		// REPLAY-HEALTH-LOGGER (ACK-2, Sol review F1/F3/F5): a registered observer turns the
		// seam on — observers resolve once, the tracker and per-queue watches allocate lazily.
		bool ObserveQueueTransitions()
		{
			queueObservers ??= player.PlayerActor.TraitsImplementing<IBotBuildQueueObserver>().ToArray();
			if (queueObservers.Length == 0)
				return false;

			queueEpisodes ??= new BotQueueEpisodeTracker();
			queueWatches ??= new Dictionary<ProductionQueue, BotQueueWatch>();
			queueProbeScratch ??= new HashSet<ProductionItem>();
			return true;
		}

		// Every-tick item-list reconciliation (F2/F3/F4): runs before the latch and WaitTicks
		// early returns so Started/Ready carry the exact observation tick and terminal records
		// fire when the item actually leaves the queue. A queue that drops out of FindQueues
		// (producer dead -> trait disabled) still resolves its contents as removals, so a
		// destroyed producer never silently swallows records. Bounded by queue count and
		// length — trait-list iteration only, no scans, orders, or RNG.
		void ProbeQueueTransitions()
		{
			var tick = world.WorldTick;
			foreach (var queue in AIUtils.FindQueues(player, Category))
			{
				if (!queueWatches.TryGetValue(queue, out var watch))
					queueWatches[queue] = watch = new BotQueueWatch();

				watch.SeenTick = tick;
				var now = queue.AllQueued();

				queueProbeScratch.Clear();
				foreach (var it in now)
					queueProbeScratch.Add(it);

				foreach (var old in watch.Items)
				{
					var oldItem = (ProductionItem)old;
					if (!queueProbeScratch.Contains(oldItem))
						ResolveRemovedItem(queue, watch, oldItem, queueSeen: true);
				}

				// Expire requests whose item is still queued: the order was rejected or
				// lost. Done after the removal diff so a leaving item still carries the
				// request's intent on its Removed record.
				watch.Pending.RemoveAll(p => p.Stale(tick));

				foreach (var it in now)
				{
					if (watch.Items.Contains(it))
						continue;

					var episode = queueEpisodes.BeginEpisode(it);
					EmitQueueTransition(queue, queue.Actor, it, it.Item, QueueCategoryOf(it.Item),
						BotQueueTransitionKind.Queued, BotQueueTransitionReason.None,
						BotQueueCancellationClass.None, episode);
				}

				var head = now.FirstOrDefault();
				if (head != null && head.Done && queueEpisodes.AnnounceReady(head))
					EmitQueueTransition(queue, queue.Actor, head, head.Item, QueueCategoryOf(head.Item),
						BotQueueTransitionKind.Ready, BotQueueTransitionReason.None);

				watch.Items.Clear();
				foreach (var it in now)
					watch.Items.Add(it);
			}

			// Stale watches: the queue left FindQueues (disabled or destroyed producer) —
			// every remaining item resolves as a removal on this observation tick.
			List<ProductionQueue> dead = null;
			foreach (var kv in queueWatches)
			{
				var watch = kv.Value;
				if (watch.SeenTick == tick)
					continue;

				foreach (var old in watch.Items)
					ResolveRemovedItem(kv.Key, watch, (ProductionItem)old, queueSeen: false);

				watch.Items.Clear();
				watch.Pending.Clear();
				watch.SeenTick = tick;

				// A dead producer can never re-enable its queue — drop the watch and its
				// dedupe entry so history stays bounded to live producers (F5 pruning).
				if (kv.Key.Actor.IsDead || kv.Key.Actor.Disposed)
				{
					dead ??= new List<ProductionQueue>();
					dead.Add(kv.Key);
					queueEpisodes.DropProducer(kv.Key.Actor.ActorID);
				}
			}

			if (dead != null)
				foreach (var queue in dead)
					queueWatches.Remove(queue);
		}

		// The item left its queue: with the queue live-observed this tick, a pending request
		// resolves to its recorded kind — the removal is engine-side proof the order applied
		// (a rejected order leaves the item queued instead). With the queue stale (disabled
		// or dead producer) request+removal is only co-occurrence, so the record keeps the
		// intent (reason) but resolves as Removed — never an invented terminal. No request at
		// all means engine cleanup or a module-side cancel: Removed, classified event-locally
		// (Elimination / Destruction / None = UNKNOWN).
		void ResolveRemovedItem(ProductionQueue queue, BotQueueWatch watch, ProductionItem oldItem, bool queueSeen)
		{
			var episode = queueEpisodes.EpisodeOf(oldItem);
			queueEpisodes.EndEpisode(oldItem);

			var pending = FindPending(watch, oldItem);
			var live = !queue.Actor.IsDead && !queue.Actor.Disposed;

			if (pending != null)
			{
				watch.Pending.Remove(pending);

				// A placement can't resolve when its producer died with the order in flight,
				// nothing resolves to its recorded kind once the queue itself is stale, and a
				// request older than MaxInFlightTicks is a rejected/lost order — all keep the
				// intent reason but report Removed (Sol: intent + UNKNOWN over invented
				// terminal proof). Removed always classifies event-locally; a dead producer
				// also reclassifies a confirmed production cancel as Destruction.
				var kind = pending.Kind;
				var cls = pending.Class;
				if (!queueSeen || pending.Stale(world.WorldTick) || (kind == BotQueueTransitionKind.Placed && !live))
				{
					kind = BotQueueTransitionKind.Removed;
					cls = player.WinState != WinState.Undefined ? BotQueueCancellationClass.Elimination
						: !live ? BotQueueCancellationClass.Destruction
						: BotQueueCancellationClass.None;
				}
				else if (!live && cls == BotQueueCancellationClass.Production)
					cls = BotQueueCancellationClass.Destruction;

				EmitQueueTransition(queue, queue.Actor, oldItem, oldItem.Item, QueueCategoryOf(oldItem.Item),
					kind, pending.Reason, cls, episode);
			}
			else
			{
				var cls = player.WinState != WinState.Undefined ? BotQueueCancellationClass.Elimination
					: !live ? BotQueueCancellationClass.Destruction
					: BotQueueCancellationClass.None;
				EmitQueueTransition(queue, queue.Actor, oldItem, oldItem.Item, QueueCategoryOf(oldItem.Item),
					BotQueueTransitionKind.Removed, BotQueueTransitionReason.None, cls, episode);
			}
		}

		static BotQueuePendingTerminal FindPending(BotQueueWatch watch, object item)
		{
			var name = (item as ProductionItem)?.Item;
			foreach (var p in watch.Pending)
				if (p.Matches(item, name))
					return p;

			return null;
		}

		// A manager-issued terminal is a REQUEST — the record resolves on the tick the item
		// leaves the queue (F2). Last decision wins: a later request for the same item
		// supersedes the earlier pending entry.
		void NotePendingTerminal(ProductionQueue queue, ProductionItem itemRef, string itemName,
			BotQueueTransitionKind kind, BotQueueTransitionReason reason, BotQueueCancellationClass cls)
		{
			if (queueWatches == null || !queueWatches.TryGetValue(queue, out var watch))
				return;

			watch.Pending.RemoveAll(p => ReferenceEquals(p.Item, itemRef) || (p.Item == null && p.ItemName == itemName));
			watch.Pending.Add(new BotQueuePendingTerminal { Item = itemRef, ItemName = itemName, Kind = kind, Reason = reason, Class = cls, IssuedTick = world.WorldTick });
		}

		/// <summary>REPLAY-HEALTH-LOGGER (F4): a demand-path cancel issued outside the manager
		/// (BaseBuilderBotModuleCA.CancelDemandItem) still resolves through the queue probe —
		/// registering it here makes the record Cancelled/DemandCancel instead of
		/// Removed/UNKNOWN.</summary>
		public void NoteExternalCancel(Actor producer, string item)
		{
			if (queueWatches == null)
				return;

			foreach (var kv in queueWatches)
			{
				if (kv.Key.Actor != producer)
					continue;

				// Same binding rule as the manager's own cancels: CancelProduction resolves
				// the LAST same-name item in queue order (a HashSet's LastOrDefault would be
				// bucket order, not the tail). Last decision wins — never an unbounded append.
				var target = kv.Key.AllQueued().LastOrDefault(i => i.Item == item);
				if (target == null)
					continue;

				kv.Value.Pending.RemoveAll(p => p.ItemName == item);
				kv.Value.Pending.Add(new BotQueuePendingTerminal
				{
					Item = target, ItemName = item, Kind = BotQueueTransitionKind.Cancelled,
					Reason = BotQueueTransitionReason.DemandCancel, Class = BotQueueCancellationClass.Production,
					IssuedTick = world.WorldTick
				});
			}
		}

		// One record-only transition to every registered observer. Event-local state is
		// resolved here, at the emit tick — a producer-less transition (latch resume)
		// carries ProducerActorId 0 and ProducerLive null = UNKNOWN; an item unobserved by
		// the probe carries EpisodeId 0 = UNKNOWN.
		void EmitQueueTransition(ProductionQueue queue, Actor producer, object itemRef, string item, BuildingType category,
			BotQueueTransitionKind kind, BotQueueTransitionReason reason,
			BotQueueCancellationClass cancellationClass = BotQueueCancellationClass.None,
			uint episodeOverride = 0)
		{
			if (!ObserveQueueTransitions())
				return;

			var producerId = producer?.ActorID ?? 0;
			if (!queueEpisodes.EmitGate(producerId, itemRef, item, kind, reason))
				return;

			var transition = new BotQueueTransition(world.WorldTick, item,
				queue?.Info.Group ?? Category, category, producerId,
				episodeOverride != 0 ? episodeOverride : queueEpisodes.EpisodeOf(itemRef),
				kind, reason,
				player.WinState == WinState.Undefined,
				producer == null ? (bool?)null : !producer.IsDead && !producer.Disposed,
				cancellationClass);
			foreach (var observer in queueObservers)
				observer.OnQueueTransition(in transition);
		}

		BuildingType QueueCategoryOf(string item)
		{
			if (item == null || baseBuilder.Info == null)
				return BuildingType.Building;
			if (baseBuilder.Info.RefineryTypes.Contains(item))
				return BuildingType.Refinery;
			if (baseBuilder.Info.DefenseTypes.Contains(item))
				return BuildingType.Defense;
			if (baseBuilder.Info.FragileTypes.Contains(item))
				return BuildingType.Fragile;
			return BuildingType.Building;
		}

		// Scale targets (DESIGN 19.10): an enabled provider's production target replaces BotLimits.ProductionTypeLimit
		// (0 stays "no limit" when there are no BotLimits at all).
		int ProductionTypeLimit => productionTypeLimit > 0 && baseBuilder.TryGetScaleTarget("production", out var scaled)
			? scaled : productionTypeLimit;

		// BotLimits carries the per-difficulty value on the DESIGN §19.1 line; negative there means the module's own.
		// Build-order knobs (12.25): greed raises the cash the production priority override waits for (economy first), production lowers it.
		int NewProductionCashThreshold
		{
			get
			{
				var threshold = botLimits != null && botLimits.Info.NewProductionCashThreshold >= 0
					? botLimits.Info.NewProductionCashThreshold : baseBuilder.Info.NewProductionCashThreshold;
				var knobs = baseBuilder.BuildOrderKnobs;
				if (knobs == null || threshold <= 0)
					return threshold;

				return DivideByKnob(BotBuildOrderKnobs.Scale(threshold, knobs.KnobMilli(BuildOrderKnob.Greed)), knobs.KnobMilli(BuildOrderKnob.Production));
			}
		}

		public void Tick(IBot bot)
		{
			// REPLAY-HEALTH-LOGGER (F3): the probe reconciles queue state every tick so
			// transition records carry exact observation ticks — it must run before the
			// latch and WaitTicks early returns below. No observer = one lazy-resolve check.
			if (ObserveQueueTransitions())
				ProbeQueueTransitions();

			foreach (KeyValuePair<string, int> i in activeBuildingIntervals.ToList())
			{
				activeBuildingIntervals[i.Key]--;
				if (activeBuildingIntervals[i.Key] <= 0)
					activeBuildingIntervals.Remove(i.Key);
			}

			// If we can't place any structures, give a nudge to BaseExpansionModules and hope it gets fixed.
			if (failCount >= baseBuilder.Info.MaximumFailedPlacementAttempts)
			{
				if (baseBuilder.BaseExpansionModules != null && baseCenterKeepsFailing != null &&
					baseBuilder.RelocationHoldConyard == null)
				{
					// we should not give a nudge for defence
					if (!baseBuilder.Info.DefenseTypes.Contains(lastFailedBuilding))
					{
						var stuckConyard = baseBuilder.ConstructionYardBuildings.Actors
							.Where(a => (a.Location - baseCenterKeepsFailing.Value).LengthSquared <= baseBuilder.Info.MaxBaseRadius * baseBuilder.Info.MaxBaseRadius)
							.OrderBy(a => a.ActorID)
							.MinByOrDefault(a => (a.Location - baseCenterKeepsFailing.Value).LengthSquared);

						if (stuckConyard != null)
						{
							baseBuilder.RelocationHoldConyard = stuckConyard;
							foreach (var queue in stuckConyard.TraitsImplementing<ProductionQueue>())
							{
								foreach (var item in queue.AllQueued().ToArray())
								{
									// The record resolves on the tick the item actually
									// leaves the queue (F2); queues outside this manager's
									// category surface as Removed on their own manager.
									NotePendingTerminal(queue, item, item.Item, BotQueueTransitionKind.Cancelled,
										BotQueueTransitionReason.RelocationLatch, BotQueueCancellationClass.Production);
									bot.QueueOrder(Order.CancelProduction(queue.Actor, item.Item, 1));
								}
							}

							foreach (var be in baseBuilder.BaseExpansionModules)
								be.UpdateExpansionParams(bot, false, true, stuckConyard);

							EmitQueueTransition(null, stuckConyard, null, lastFailedBuilding, QueueCategoryOf(lastFailedBuilding),
								BotQueueTransitionKind.Held, BotQueueTransitionReason.RelocationLatch);
							failCount = 0;
							failRetryTicks = baseBuilder.Info.StructureProductionResumeDelay;
							return;
						}
					}

					EmitQueueTransition(null, null, null, lastFailedBuilding, QueueCategoryOf(lastFailedBuilding),
						BotQueueTransitionKind.Resumed, BotQueueTransitionReason.SaturationLatch);
					failCount = 0;
					failRetryTicks = baseBuilder.Info.StructureProductionResumeDelay;
				}

				// When the nudge above can't act — no BaseExpansionModules, no failing centre, or a
				// relocation hold live — only bother resetting failCount when either
				// a) the number of buildings has decreased since the snapshot taken at saturation,
				// or b) number of BaseProviders (construction yard or similar) has increased since then.
				// Otherwise the probe waits another resume delay.
				// (BaseExpansionModules is a .ToArray() — never null; Length==0 covers "no modules".)
				// FIX-RA-REFINERY: the widened gate serves every un-nudgeable case; R1 moved the
				// baseline off cachedBases (the water checker's field) onto latched* fields captured
				// unconditionally at saturation, and rearmed the timer per episode.
				else if (BaseBuilderQueueEvalCA.LatchRecoveryApplies(baseBuilder.BaseExpansionModules.Length,
					baseCenterKeepsFailing != null, baseBuilder.RelocationHoldConyard != null)
					&& BaseBuilderQueueEvalCA.LatchProbeDue(ref failRetryTicks, baseBuilder.Info.StructureProductionResumeDelay))
				{
					var currentBuildings = world.ActorsHavingTrait<Building>().Count(a => a.Owner == player);
					var baseProviders = world.ActorsHavingTrait<BaseProvider>().Count(a => a.Owner == player);

					if (BaseBuilderQueueEvalCA.LatchProbeReleases(currentBuildings, latchedBuildings, baseProviders, latchedProviders))
					{
						// FIX-RA-REFINERY diagnostics: observe the latch release — once per resume
						// delay at most, never per tick.
						AIUtils.BotDebug($"{player} placement latch released (buildings {latchedBuildings} -> {currentBuildings}, providers {latchedProviders} -> {baseProviders}) at tick {world.WorldTick}");
						EmitQueueTransition(null, null, null, lastFailedBuilding, QueueCategoryOf(lastFailedBuilding),
							BotQueueTransitionKind.Resumed, BotQueueTransitionReason.SaturationLatch);
						failCount = 0;
					}
				}

				if (failCount >= baseBuilder.Info.MaximumFailedPlacementAttempts)
					return;
			}

			if (waterState == WaterCheck.NotChecked)
			{
				if (AIUtils.IsAreaAvailable<BaseProvider>(world, player, world.Map, baseBuilder.Info.MaxBaseRadius, baseBuilder.Info.WaterTerrainTypes))
					waterState = WaterCheck.EnoughWater;
				else
				{
					waterState = WaterCheck.NotEnoughWater;
					checkForBasesTicks = baseBuilder.Info.CheckForNewBasesDelay;
				}
			}

			if (waterState == WaterCheck.NotEnoughWater && --checkForBasesTicks <= 0)
			{
				var currentBases = world.ActorsHavingTrait<BaseProvider>().Count(a => a.Owner == player);

				if (currentBases > cachedBases)
				{
					cachedBases = currentBases;
					waterState = WaterCheck.NotChecked;
				}
				else
					checkForBasesTicks = baseBuilder.Info.CheckForNewBasesDelay;
			}

			// Only update once per second or so
			if (WaitTicks > 0)
				return;

			playerBuildings = world.ActorsHavingTrait<Building>().Where(a => a.Owner == player).ToArray();
			var excessPowerBonus = baseBuilder.Info.ExcessPowerIncrement * (playerBuildings.Count() / baseBuilder.Info.ExcessPowerIncreaseThreshold.Clamp(1, int.MaxValue));
			minimumExcessPower = (baseBuilder.Info.MinimumExcessPower + excessPowerBonus).Clamp(baseBuilder.Info.MinimumExcessPower, baseBuilder.Info.MaximumExcessPower);

			// Build-order knobs (12.25): power_margin scales the surplus target (no provider = unchanged).
			var knobs = baseBuilder.BuildOrderKnobs;
			if (knobs != null)
				minimumExcessPower = BotBuildOrderKnobs.Scale(minimumExcessPower, knobs.KnobMilli(BuildOrderKnob.PowerMargin));

			// BP-2 (§19.15): the advisor's radar power reserve — one Refresh-priced read per queue tick.
			radarPowerMargin = FrontBackAdvisor()?.RadarPowerMargin ?? 0;

			// PERF: Queue only one actor at a time per category
			itemQueuedThisTick = false;
			var active = false;
			foreach (var queue in AIUtils.FindQueues(player, Category))
			{
				if (TickQueue(bot, queue))
					active = true;
			}

			// Add a random factor so not every AI produces at the same tick early in the game.
			// Minimum should not be negative as delays in HackyAI could be zero.
			var randomFactor = BotRng.For(player).Next(0, baseBuilder.Info.StructureProductionRandomBonusDelay);

			WaitTicks = active ? baseBuilder.Info.StructureProductionActiveDelay + randomFactor
				: baseBuilder.Info.StructureProductionInactiveDelay + randomFactor;

			// Build-order knobs (12.25): tempo shortens (above 1000) or lengthens the general build interval.
			if (knobs != null)
				WaitTicks = DivideByKnob(WaitTicks, knobs.KnobMilli(BuildOrderKnob.Tempo));
		}

		bool TickQueue(IBot bot, ProductionQueue queue)
		{
			if (queue.Actor == baseBuilder.RelocationHoldConyard)
				return false;

			var currentBuilding = queue.AllQueued().FirstOrDefault();

			// Waiting to build something
			if (currentBuilding == null && failCount < baseBuilder.Info.MaximumFailedPlacementAttempts)
			{
				pendingDemandPick = default;
				var item = ChooseBuildingToBuild(queue);
				if (item == null)
					return false;

				var demandPick = pendingDemandPick;

				// ECON-A-FIX (R5): producer+name exclusivity — an ordinary pick must never duplicate a
				// demand-bound name on this producer: CancelProduction removes the LAST same-name item,
				// so a duplicate would refund or free the wrong one. The demand's own pick (and an
				// ordinary same-name duplicate of an ORDINARY item, legal before) pass unchanged.
				if (baseBuilder.ExpansionPrebuildEnabled && demandPick.Demand == null
					&& baseBuilder.DemandForQueuedItem(item.Name, queue.Actor) != null)
					return false;

				// We shouldn't be queueing new buildings (other than refineries) when we're low on cash.
				// REF-1 B1/B2: under the refinery law the planner's own wants (the crawl link and the due MCV's
				// prerequisite) ride at their own cost floor — a crawl link IS the economy investment, and the
				// reserve-sized gate starved the frontier in the trace.
				var lawWants = baseBuilder.RefineryLawProvider();
				var plannerWant = lawWants != null
					&& (item.Name == lawWants.WantedLinkBuilding || item.Name == lawWants.WantedMcvPrerequisite);

				// ECON-A: a demand-bound item rides at its own cost floor like the planner wants —
				// the reserve gate would push it past the ETA it was scheduled to meet.
				if (BaseBuilderQueueEvalCA.BlockedByCash(playerResources.GetCashAndResources(), minCashRequirement,
						baseBuilder.Info.RefineryTypes.Contains(item.Name), plannerWant || demandPick.Demand != null,
						queue.GetProductionCost(item), itemQueuedThisTick))
					return false;

				// Cameo (§12.20): the army-first vote - a provider (ArmyFirstBotModule, the one owner) can hold new
				// non-essential buildings so cash flows to unit production while it needs to. Construction yards,
				// refineries, power and the first production building are essential and always pass.
				pauseBuilding ??= player.PlayerActor.TraitsImplementing<IBotRequestPauseBuildingProduction>().ToArray();
				if (pauseBuilding.Length > 0)
				{
					var essential = IsArmyFirstEssential(item);
					if (pauseBuilding.Any(p => p.PausesBuilding(item, essential)))
						return false;
				}

				// ECON-A-FIX (R6): the demand's refinery reserves its claim's anchor as the order is
				// admitted — taken off every other claim while the item is in flight. A pick whose
				// anchor is no longer reservable (served, parked, pending, or another demand got there)
				// never reaches the queue and retries next sweep.
				if (demandPick.Demand != null && demandPick.IsRefinery && lawWants != null
					&& (demandPick.Claim == null
						|| !lawWants.TryReserveRefineryAnchor(demandPick.Claim.Value.Anchor, demandPick.Demand,
							world.WorldTick + baseBuilder.Info.ExpansionDemandIdleTicks)))
				{
					pendingDemandPick = default;
					return false;
				}

				baseBuilder.RecordOpeningStructureQueued(queue, item);
				baseBuilder.BuildOrderKnobs?.NotifyQueued(item.Name);
				bot.QueueOrder(Order.StartProduction(queue.Actor, item.Name, 1));
				queuedAt[queue.Actor.ActorID] = (item.Name, world.WorldTick);
				if (demandPick.Demand != null)
				{
					// ECON-A-FIX (R5): bind on the admitted order, not the pick — the recorded in-flight
					// order and this binding are the same (producer, item) token CancelProduction needs.
					if (demandPick.IsRefinery)
						demandPick.Demand.BindRefinery(item.Name, queue.Actor, world.WorldTick, demandPick.Claim);
					else
						demandPick.Demand.BindDefence(item.Name, queue.Actor, world.WorldTick);

					pendingDemandPick = default;
				}

				baseBuilder.RecordProducerOrder(queue.Actor, item.Name);
				itemQueuedThisTick = true;
				SetBuildingInterval(item.Name);

				// REPLAY-HEALTH-LOGGER (F2/F3): no emit here — the probe records Started on
				// the tick the engine applies the order, not the request tick.
			}
			else if (currentBuilding != null && currentBuilding.Done)
			{
				// REPLAY-HEALTH-LOGGER (F3): Ready also comes from the probe — the exact tick
				// the head item flips Done, not this WaitTicks-gated sweep.

				// ECON-A (§4 Ready-hold): a bound demand item sits at Queue[0] until its MCV deploys —
				// returning false holds it without spending failure budget (the same mechanic the REF-1
				// crawl hold and the BP-2 front/back hold use below). One held item per queue is the
				// engine's own guarantee: only the head item produces, and the head is the held item.
				var heldDemand = baseBuilder.ExpansionPrebuildEnabled
					? baseBuilder.DemandForQueuedItem(currentBuilding.Item, queue.Actor)
					: null;
				if (heldDemand != null && !heldDemand.Deployed)
				{
					EmitQueueTransition(queue, queue.Actor, currentBuilding, currentBuilding.Item, QueueCategoryOf(currentBuilding.Item),
						BotQueueTransitionKind.Held, BotQueueTransitionReason.DemandHold);
					return false;
				}

				// Production is complete
				// Choose the placement logic
				// HACK: HACK HACK HACK
				// TODO: Derive this from BuildingCommonNames instead
				var type = BuildingType.Building;
				var placeDefenseTowardsEnemyChance = baseBuilder.Info.PlaceDefenseTowardsEnemyChance;

				CPos? location = null;
				var actorVariant = 0;
				string orderString = "PlaceBuilding";

				// Check if we've hit the limit for this building already, if so cancel it
				if (baseBuilder.TryGetBuildingLimit(currentBuilding.Item, out var currentLimit))
				{
					if (BaseBuilderQueueEvalCA.LimitReached(AIUtils.CountBuildingByCommonName(new HashSet<string> { currentBuilding.Item }, player), currentLimit))
					{
						AIUtils.BotDebug($"{player} has already has enough {currentBuilding.Item}; cancelling production");

						// CancelProduction removes the LAST same-name item — bind that
						// instance, not the head, so the removal resolves to this request.
						var cancelTarget = queue.AllQueued().LastOrDefault(i => i.Item == currentBuilding.Item) ?? currentBuilding;
						NotePendingTerminal(queue, cancelTarget, currentBuilding.Item, BotQueueTransitionKind.Cancelled,
							BotQueueTransitionReason.LimitReached, BotQueueCancellationClass.Production);
						bot.QueueOrder(Order.CancelProduction(queue.Actor, currentBuilding.Item, 1));
					}
				}

				// Check if Building is a plug for other Building
				var actorInfo = world.Map.Rules.Actors[currentBuilding.Item];
				var plugInfo = actorInfo.TraitInfoOrDefault<PlugInfo>();
				var valueInfo = actorInfo.TraitInfoOrDefault<ValuedInfo>();
				var distanceToBaseIsImportant = true;
				CPos? advisedDefense = null;
				var refineryDefers = false;
				refineryClaimed = false;
				lastFrontBackPick = null;
				frontBackHold = false;
				demandAdvisedDefense = false;
				if (plugInfo != null)
				{
					var possibleBuilding = world.ActorsWithTrait<Pluggable>().FirstOrDefault(a =>
						a.Actor.Owner == player && a.Trait.AcceptsPlug(plugInfo.Type));

					if (possibleBuilding.Actor != null)
					{
						orderString = "PlacePlug";
						location = possibleBuilding.Actor.Location + possibleBuilding.Trait.Info.Offset;
					}
				}
				else
				{
					var law = baseBuilder.RefineryLawProvider();

					if (heldDemand != null)
					{
						// ECON-A (§5): the MCV deployed — the held refinery takes the refinery-law claim
						// path biased to the demand's own outpost, the defence places at the outpost.
						// The class is decided by the binding itself; no random draw is consumed on
						// this path (SPEC §8).
						type = heldDemand.RefineryItem == currentBuilding.Item && heldDemand.RefineryProducer == queue.Actor
							? BuildingType.Refinery
							: BuildingType.Defense;
					}
					else
					{
						// Check if Building is a defense and if we should place it towards the enemy or not.
						// REF-1 B1 (§12.24 v2): the planner's crawl want is always a BaseCrawl placement — the want
						// exists to close the gap to the target field, so the chance roll and cost threshold that
						// gate organic crawl never apply to it (no random draw is consumed on this path).
						var lawLink = law != null && currentBuilding.Item == law.WantedLinkBuilding;
						var isRefinery = false;
						var isFragile = false;
						var hasAttackBase = false;
						var defenseRoll = false;
						var organicCrawlRoll = false;
						if (!lawLink)
						{
							if (baseBuilder.Info.RefineryTypes.Contains(actorInfo.Name))
								isRefinery = true;
							else if (baseBuilder.Info.FragileTypes.Contains(actorInfo.Name))
								isFragile = true;
							else if (actorInfo.HasTraitInfo<AttackBaseInfo>())
							{
								hasAttackBase = true;

								// Cameo: an active advisor picks the cell itself, skipping the roll (no random draw is consumed);
								// without one, or when it has no answer, the code below is unchanged.
								advisedDefense = AdvisedDefenseCell(actorInfo, distanceToBaseIsImportant, queue.Actor);
								if (advisedDefense == null)
								{
									if (baseBuilder.Info.AntiAirTypes.Contains(actorInfo.Name))
										placeDefenseTowardsEnemyChance = (int)Math.Ceiling(placeDefenseTowardsEnemyChance / 1.5);

									defenseRoll = BotRng.For(player).Next(100) < placeDefenseTowardsEnemyChance;
								}
							}
							// REF-1 (B1 maintainer ruling): a crawl placement must extend the buildable area —
							// under the law, buildings without GivesBuildableArea (silos) never take the
							// organic crawl roll and place at home instead; the GBA check precedes the draw
							// so it consumes no randoms on the law path.
							else
							{
								organicCrawlRoll = !limitBuildRadius && valueInfo != null && valueInfo.Cost < baseBuilder.Info.BaseCrawlCostThreshold
									&& RefineryLawCrawlRoll.LegalLink(law != null, actorInfo)
									&& BotRng.For(player).Next(100) < baseBuilder.Info.BaseCrawlChance;
							}
						}

						type = BaseBuilderQueueEvalCA.ClassifyPlacement(lawLink, isRefinery, isFragile, hasAttackBase, defenseRoll, organicCrawlRoll);
					}

					// FIX-RA-REFINERY: decided here so the null-location branch below can tell a
					// law refinery's "retry later" from an ordinary placement failure.
					refineryDefers = BaseBuilderQueueEvalCA.RefineryDefers(law != null, type);

					// REF-1 B1 (crawl-trace §8): under the law a crawl placement with no aim holds — returning
					// keeps the produced building queued (and spends no failure budget) instead of wasting the
					// link on an un-aimed fallback cell.
					if (BaseBuilderQueueEvalCA.CrawlHold(type, law != null, law?.CrawlTargetEdge != null, baseBuilder.ExpansionTarget() != null))
					{
						EmitQueueTransition(queue, queue.Actor, currentBuilding, currentBuilding.Item, type,
							BotQueueTransitionKind.Held, BotQueueTransitionReason.CrawlHold);
						return false;
					}

					if (advisedDefense != null)
						location = advisedDefense;
					else
					{
						(location, baseCenterKeepsFailing, actorVariant) = ChooseBuildLocation(currentBuilding.Item, distanceToBaseIsImportant, queue.Actor, type, heldDemand);

						// BP-2 (§19.15): the front/back advisor's hold — no legal cell and no fallback
						// (a radar on a front with no defence line waits; it never goes forward). Same
						// semantics as the REF-1 crawl hold above: queued, no failure budget spent.
						if (frontBackHold)
						{
							EmitQueueTransition(queue, queue.Actor, currentBuilding, currentBuilding.Item, type,
								BotQueueTransitionKind.Held, BotQueueTransitionReason.FrontBackHold);
							return false;
						}
					}
				}

				if (location == null)
				{
					// FIX-RA-REFINERY: under the law a siteless refinery defers — cancelled for its
					// refund while the standing request re-queues it on a later sweep. Spending
					// shared failure budget here let one unreachable field saturate the latch and
					// starve every other structure indefinitely.
					if (refineryDefers)
					{
						AIUtils.BotDebug($"{player} defers {currentBuilding.Item}: no legal refinery site this sweep");
						var cancelTarget = queue.AllQueued().LastOrDefault(i => i.Item == currentBuilding.Item) ?? currentBuilding;
						NotePendingTerminal(queue, cancelTarget, currentBuilding.Item, BotQueueTransitionKind.Cancelled,
							BotQueueTransitionReason.NoRefinerySite, BotQueueCancellationClass.Production);
						bot.QueueOrder(Order.CancelProduction(queue.Actor, currentBuilding.Item, 1));
					}

					// If we just reached the maximum fail count, cache the number of current structures
					else if (++failCount >= baseBuilder.Info.MaximumFailedPlacementAttempts)
					{
						AIUtils.BotDebug($"{player} has nowhere to place {currentBuilding.Item}");
						var cancelTarget = queue.AllQueued().LastOrDefault(i => i.Item == currentBuilding.Item) ?? currentBuilding;
						NotePendingTerminal(queue, cancelTarget, currentBuilding.Item, BotQueueTransitionKind.Cancelled,
							BotQueueTransitionReason.SaturationLatch, BotQueueCancellationClass.Production);
						bot.QueueOrder(Order.CancelProduction(queue.Actor, currentBuilding.Item, 1));
						EmitQueueTransition(queue, queue.Actor, currentBuilding, currentBuilding.Item, QueueCategoryOf(currentBuilding.Item),
							BotQueueTransitionKind.Held, BotQueueTransitionReason.SaturationLatch);
						lastFailedBuilding = currentBuilding.Item;

						// FIX-RA-REFINERY R1: the latch snapshot is taken at every saturation —
						// the old `BaseExpansionModules == null` guard never ran (the array is
						// never null), which left the recovery probe comparing stale or
						// water-check-owned baselines. The probe timer re-arms per episode.
						latchedBuildings = world.ActorsHavingTrait<Building>().Count(a => a.Owner == player);
						latchedProviders = world.ActorsHavingTrait<BaseProvider>().Count(a => a.Owner == player);
						failRetryTicks = baseBuilder.Info.StructureProductionResumeDelay;
					}
				}
				else
				{
					failCount = 0;
					NotifyPlacement(currentBuilding.Item, location.Value, queue.Actor.ActorID, orderString, type, advisedDefense != null || demandAdvisedDefense, lastFrontBackPick);

					// REPLAY-HEALTH-LOGGER (F2): the place order is in flight — the Placed
					// record resolves on the tick the item actually leaves the queue.
					NotePendingTerminal(queue, currentBuilding, currentBuilding.Item, BotQueueTransitionKind.Placed,
						BotQueueTransitionReason.None, BotQueueCancellationClass.None);

					bot.QueueOrder(new Order(orderString, player.PlayerActor, Target.FromCell(world, location.Value), false)
					{
						// Building to place
						TargetString = currentBuilding.Item,

						// Actor variant will always be small enough to safely pack in a CPos
						ExtraLocation = new CPos(actorVariant, 0),

						// Actor ID to associate the placement with
						ExtraData = queue.Actor.ActorID,
						SuppressVisualFeedback = true
					});

					// After succesfuly placing a building, nudge BaseExpansionModules to expand.
					// We want to avoid expanding too often, so we make a judgement by counting buildings.
					if (baseBuilder.Info.ProductionTypes.Contains(currentBuilding.Item)
						|| baseBuilder.Info.FragileTypes.Contains(currentBuilding.Item) || baseBuilder.Info.RefineryTypes.Contains(currentBuilding.Item))
					{
						var numRef = baseBuilder.RefineryBuildings.Actors.Count(a => !a.IsDead) + (baseBuilder.Info.RefineryTypes.Contains(currentBuilding.Item) ? 1 : 0);

						var numProd = baseBuilder.ProductionBuildings.Actors.Count(a => !a.IsDead) + (baseBuilder.Info.ProductionTypes.Contains(currentBuilding.Item) ? 1 : 0);

						var numTech = playerBuildings.Count(a => baseBuilder.Info.FragileTypes.Contains(a.Info.Name))
							+ (baseBuilder.Info.FragileTypes.Contains(currentBuilding.Item) ? 1 : 0);

						var tolerateOnCash = playerResources.GetCashAndResources() / Math.Max(baseBuilder.Info.PerExpansionTolerateOnCash, 1);

						// REF-1 B4 (§12.24 v2): under the refinery law the raw refinery total is replaced by
						// coverage — nudge an expansion when every anchor in reach is served and unserved anchors
						// still exist beyond reach. Classic/switch-off keep the old count.
						if (RefineryLawNudge.Due(baseBuilder.RefineryLawProvider(), numRef,
								baseBuilder.Info.InititalMinimumRefineryCount + baseBuilder.Info.AdditionalMinimumRefineryCount)
							&& numProd > 0 && BaseBuilderQueueEvalCA.ExpansionBalance(numProd, numTech, RandomTolerance(baseBuilder.Info.ExpansionTolerate), tolerateOnCash, numRef))
						{
							var undeployEvenNoBase = BaseBuilderQueueEvalCA.ExpansionBalance(numProd, numTech, RandomTolerance(baseBuilder.Info.ForceExpansionTolerate), tolerateOnCash, numRef);

							foreach (var be in baseBuilder.BaseExpansionModules)
								be.UpdateExpansionParams(bot, true, undeployEvenNoBase, null);
						}
					}
					return true;
				}
			}

			return true;
		}

		// Cameo (12.24 FE-0 / 12.25 BO-0): record-only telemetry. The queue tick per producer and the refinery-claim flag are read by
		// NotifyPlacement only; nothing here feeds a decision.
		readonly Dictionary<uint, (string Item, int Tick)> queuedAt = new();
		bool refineryClaimed;
		IBotPlacementObserver[] placementObservers;

		// BP-2 (§19.15): the front/back advisor seam. The trait array is cached once; IsActive is
		// re-checked per call so a condition-disabled planner never claims a class. lastFrontBackPick
		// carries the pick diagnostics of the placement attempt in flight to the placement log;
		// frontBackHold means "wait — no legal cell, do not fall back" (the produced building stays
		// queued and spends no failure budget, like the REF-1 crawl hold). radarPowerMargin is the
		// advisor's power reserve for the radars it plans, refreshed once per queue tick.
		IBotFrontBackAdvisor[] frontBackAdvisors;
		FrontBackPick? lastFrontBackPick;
		bool frontBackHold;
		int radarPowerMargin;

		IBotFrontBackAdvisor FrontBackAdvisor()
		{
			frontBackAdvisors ??= player.PlayerActor.TraitsImplementing<IBotFrontBackAdvisor>().ToArray();
			return frontBackAdvisors.FirstOrDefault(a => a.IsActive);
		}

		void NotifyPlacement(string item, CPos cell, uint producerId, string orderString, BuildingType type, bool advisedDefence, FrontBackPick? frontBackPick)
		{
			placementObservers ??= world.WorldActor.TraitsImplementing<IBotPlacementObserver>().ToArray();
			if (placementObservers.Length == 0)
				return;

			var reason = orderString != "PlaceBuilding" ? "other"
				: advisedDefence || type == BuildingType.Defense ? "defence"
				: type == BuildingType.BaseCrawl ? "crawl"
				: type == BuildingType.Refinery && refineryClaimed ? "refinery_claim"
				: "base";

			// BP-2 (§19.15): the advisor's class label for every placement while one is active. The
			// queue's own type wins for crawl (a crawl link is role, not identity — Classify sees a
			// plain building); every other class is the advisor's rules-derived verdict.
			FrontBackClass? frontBackClass = null;
			var frontBack = FrontBackAdvisor();
			if (frontBack != null)
				frontBackClass = type == BuildingType.BaseCrawl ? FrontBackClass.Crawl
					: world.Map.Rules.Actors.TryGetValue(item, out var placedInfo) ? frontBack.Classify(placedInfo)
					: FrontBackClass.Building;

			var queued = queuedAt.TryGetValue(producerId, out var q) && q.Item == item ? q.Tick : world.WorldTick;
			foreach (var observer in placementObservers)
				observer.BuildingPlaced(player, world.WorldTick, item, cell, reason, queued, frontBackClass, frontBackPick);
		}

		ActorInfo GetProducibleBuilding(IReadOnlySet<string> actors, IEnumerable<ActorInfo> buildables, Func<ActorInfo, int> orderBy = null)
		{
			var available = buildables.Where(actor =>
			{
				// Are we able to build this?
				if (!actors.Contains(actor.Name))
					return false;

				return BaseBuilderQueueEvalCA.LimitAdmits(baseBuilder.TryGetBuildingLimit(actor.Name, out var limit), limit,
					playerBuildings.Count(a => a.Info.Name == actor.Name),
					baseBuilder.BuildingsBeingProduced.TryGetValue(actor.Name, out var beingProduced) ? beingProduced : 0);
			});

			if (orderBy != null)
				return available.MaxByOrDefault(orderBy);

			return available.RandomOrDefault(BotRng.For(player));
		}

		// BP-2 (§19.15): a rules-derived radar provider — same two-trait check the Cameo planner runs
		// (the assemblies cannot share the helper; keep the expression identical).
		static bool IsRadarProvider(ActorInfo info)
		{
			return info.HasTraitInfo<RangedGpsProviderInfo>() || info.HasTraitInfo<ProvidesRadarInfo>();
		}

		// Owned + already-in-production radar providers — the count the advisor's absolute target is
		// met against, so a queued provider is never double-wanted.
		int OwnedRadarProviders()
		{
			var owned = playerBuildings.Count(a => IsRadarProvider(a.Info));
			if (baseBuilder.BuildingsBeingProduced != null)
				owned += baseBuilder.BuildingsBeingProduced
					.Where(kv => world.Map.Rules.Actors.TryGetValue(kv.Key, out var produced) && IsRadarProvider(produced))
					.Sum(kv => kv.Value);
			return owned;
		}

		// Radar providers the queue can actually start: trait-filtered and under the same
		// BuildingLimits rule every other want honours.
		List<ActorInfo> GetRadarProducibles(IEnumerable<ActorInfo> buildables)
		{
			var list = new List<ActorInfo>();
			foreach (var actor in buildables)
			{
				if (!IsRadarProvider(actor))
					continue;

				if (baseBuilder.TryGetBuildingLimit(actor.Name, out var limit)
					&& playerBuildings.Count(a => a.Info.Name == actor.Name)
						+ (baseBuilder.BuildingsBeingProduced.TryGetValue(actor.Name, out var beingProduced) ? beingProduced : 0) >= limit)
					continue;

				list.Add(actor);
			}

			return list;
		}

		bool HasSufficientPowerForActor(ActorInfo actorInfo)
		{
			return playerPower == null || (actorInfo.TraitInfos<PowerInfo>().Where(i => i.EnabledByDefault)
				.Sum(p => p.Amount) + playerPower.ExcessPower) >= ScaledBaseMinimumExcessPower();
		}

		// Build-order knobs (12.25): power_margin scales the configured surplus floor too.
		// BP-2 (§19.15): the front/back advisor's radar reserve rides the same floor — a queued
		// radar's drain is held back so an owned or planned provider is never built blind.
		int ScaledBaseMinimumExcessPower()
		{
			var knobs = baseBuilder.BuildOrderKnobs;
			var floor = knobs == null ? baseBuilder.Info.MinimumExcessPower
				: BotBuildOrderKnobs.Scale(baseBuilder.Info.MinimumExcessPower, knobs.KnobMilli(BuildOrderKnob.PowerMargin));
			return floor + radarPowerMargin;
		}

		// Build-order knobs (12.25): the milli multiplier of a building's fraction by its rules-derived category (1000 = none).
		int FractionMilli(string actorName)
		{
			var knobs = baseBuilder.BuildOrderKnobs;
			if (knobs == null)
				return BuildOrderKnob.Neutral;

			var knob = BuildOrderCategory.KnobFor(knobs.CategoryOf(actorName));
			return knob == null ? BuildOrderKnob.Neutral : knobs.KnobMilli(knob);
		}

		// Build-order knobs (12.25): the opening's next wanted building, preferred over the fraction table. It runs AFTER the
		// low-power and refinery priority overrides, so an opening never blocks a power or refinery emergency, and it respects
		// the same limits and the power check as every other choice. Null = no opening, nothing wanted here, or nothing buildable
		// in this queue (another queue's manager may take the step).
		ActorInfo ChooseOpeningBuilding(IBotBuildOrderKnobs knobs, IEnumerable<ActorInfo> buildableThings, ActorInfo power)
		{
			var wanted = knobs.OpeningWanted;
			if (wanted == null)
				return null;

			ActorInfo best = null;
			var bestFraction = -1;
			foreach (var candidate in buildableThings)
			{
				var name = candidate.Name;
				if (!BuildOrderCategory.Satisfies(wanted, knobs.CategoryOf(name)))
					continue;

				if (baseBuilder.Info.NavalProductionTypes.Contains(name) || baseBuilder.Info.RefineryTypes.Contains(name) && baseBuilder.HasMaxRefineriesFor(candidate))
					continue;

				var count = playerBuildings.Count(a => a.Info.Name == name) +
					(baseBuilder.BuildingsBeingProduced.TryGetValue(name, out var num) ? num : 0);

				if (botLimits != null && baseBuilder.Info.ProductionTypes.Contains(name) && BaseBuilderQueueEvalCA.LimitReached(count, ProductionTypeLimit))
					continue;

				if (baseBuilder.TryGetBuildingLimit(name, out var limit) && BaseBuilderQueueEvalCA.LimitReached(count, limit))
					continue;

				var fraction = baseBuilder.Info.BuildingFractions != null && baseBuilder.Info.BuildingFractions.TryGetValue(name, out var f) ? f : 0;
				if (best == null || BaseBuilderQueueEvalCA.BetterOpeningCandidate(fraction, bestFraction, name, best.Name))
				{
					best = candidate;
					bestFraction = fraction;
				}
			}

			if (best == null)
				return null;

			if (!HasSufficientPowerForActor(best))
				return power != null && power.TraitInfos<PowerInfo>().Where(i => i.EnabledByDefault).Sum(pi => pi.Amount) > 0 ? power : null;

			AIUtils.BotDebug("{0} decided to build {1}: opening step {2}", player, best.Name, wanted);
			return best;
		}

		/// <summary>
		/// ECON-A (SPEC_2026-10-05_econ_logistics Part A §3): the item an expansion demand wants on this
		/// queue, or null. Refinery first — the shortest-build refinery the queue can make, gated by the
		/// same HasMaxRefineriesFor anchor law every other refinery honours — then the demand's defence
		/// pick. Both wait for their ETA window (finish ~= etaTick); an already-due item starts now.
		/// Deterministic: demands iterate in Mcv.ActorID order; a second demand never binds the same
		/// item name on the same producer (the hold tracks producer+item, not the order itself).
		/// </summary>
		ActorInfo ChooseExpansionDemandItem(ProductionQueue queue, IEnumerable<ActorInfo> buildableThings)
		{
			var demands = baseBuilder.ExpansionDemands;
			if (demands.Count == 0)
				return null;

			var now = world.WorldTick;
			var buildables = buildableThings as IReadOnlyList<ActorInfo> ?? buildableThings.ToList();
			foreach (var demand in demands.Values.OrderBy(d => d.Mcv.ActorID))
			{
				if (demand.Deployed)
					continue;

				if (demand.RefineryItem == null)
				{
					var refinery = buildables
						.Where(a => baseBuilder.Info.RefineryTypes.Contains(a.Name))
						.OrderBy(a => queue.GetBuildTime(a, BuildableInfo.GetTraitForQueue(a, queue.Info.Type)))
						.ThenBy(a => a.Name, StringComparer.Ordinal)
						.FirstOrDefault();
					if (refinery != null && DemandRefineryQuota(demand, refinery))
					{
						var buildTime = queue.GetBuildTime(refinery, BuildableInfo.GetTraitForQueue(refinery, queue.Info.Type));

						// ECON-A-FIX (R5): the pick stands only while the (producer, name) token stays
						// unambiguous — no same-name item queued or still in flight, none bound to
						// another demand. The binding itself happens at order admission, not here.
						if (BaseBuilderQueueEvalCA.ExpansionDue(demand.EtaTick, buildTime, now)
							&& BaseBuilderQueueEvalCA.DemandItemUnambiguous(
								baseBuilder.ProducerHoldsItem(queue.Actor, refinery.Name, now),
								DemandItemHeldElsewhere(demand, refinery.Name, queue.Actor)))
						{
							pendingDemandPick = (demand, true, baseBuilder.DemandClaimFor(demand));
							return refinery;
						}
					}
				}

				if (demand.DefenceType != null && demand.DefenceItem == null)
				{
					var defence = buildables.FirstOrDefault(a => a.Name == demand.DefenceType);
					if (defence != null)
					{
						var buildTime = queue.GetBuildTime(defence, BuildableInfo.GetTraitForQueue(defence, queue.Info.Type));
						if (BaseBuilderQueueEvalCA.ExpansionDue(demand.EtaTick, buildTime, now)
							&& baseBuilder.DefenceHoldPermitted(queue, demand, now)
							&& BaseBuilderQueueEvalCA.DemandItemUnambiguous(
								baseBuilder.ProducerHoldsItem(queue.Actor, defence.Name, now),
								DemandItemHeldElsewhere(demand, defence.Name, queue.Actor)))
						{
							pendingDemandPick = (demand, false, null);
							return defence;
						}
					}
				}
			}

			return null;
		}

		/// <summary>
		/// ECON-A-FIX (R6): the demand's refinery quota — under the refinery law it IS the demand's own
		/// reservable claim, ranked from the outpost with the committed yard's footprint counting as
		/// frontier (an expansion beyond today's reach is not gated by the wall it removes). Without the
		/// law the classic yard-based cap stands.
		/// </summary>
		bool DemandRefineryQuota(ExpansionDemand demand, ActorInfo refinery)
		{
			return baseBuilder.RefineryLawProvider() != null
				? baseBuilder.DemandClaimFor(demand) != null
				: !baseBuilder.HasMaxRefineriesFor(refinery);
		}

		// Another demand already holds this item name on this producer — a second binding would fight
		// over the same queued item (the hold tracks producer+item, not the order itself).
		bool DemandItemHeldElsewhere(ExpansionDemand demand, string item, Actor producer)
		{
			return baseBuilder.ExpansionDemands.Values.Any(other => !ReferenceEquals(other, demand)
				&& ((other.RefineryProducer == producer && other.RefineryItem == item)
					|| (other.DefenceProducer == producer && other.DefenceItem == item)));
		}

		static int DivideByKnob(int ticks, int milli) => milli == BuildOrderKnob.Neutral ? ticks : (int)(ticks * 1000L / Math.Max(1, milli));

		// Build-order knobs (12.25): a delay or interval in ticks for one building. tempo divides every one (above 1000 = faster);
		// a tech/superweapon building's also divides by the tech knob, a refinery's by greed. No provider = unchanged.
		int KnobTicks(int ticks, string actorName)
		{
			var knobs = baseBuilder.BuildOrderKnobs;
			if (knobs == null)
				return ticks;

			ticks = DivideByKnob(ticks, knobs.KnobMilli(BuildOrderKnob.Tempo));
			var knob = BuildOrderCategory.KnobFor(knobs.CategoryOf(actorName));
			if (knob == BuildOrderKnob.Tech || knob == BuildOrderKnob.Greed)
				ticks = DivideByKnob(ticks, knobs.KnobMilli(knob));

			return ticks;
		}

		// Cameo (army-first): buildings that stay allowed while the army-first vote holds
		// the rest. Construction yards, refineries and power are always essential; the first
		// production building must pass too — no factory means no army, so blocking it deadlocks.
		bool IsArmyFirstEssential(ActorInfo actorInfo)
		{
			var name = actorInfo.Name;
			if (baseBuilder.Info.ConstructionYardTypes.Contains(name)
				|| baseBuilder.Info.RefineryTypes.Contains(name)
				|| baseBuilder.Info.PowerTypes.Contains(name))
				return true;

			// One queued-but-unplaced factory is enough to break the deadlock — don't let the
			// gate admit a second one while the first is still producing.
			if (baseBuilder.Info.ProductionTypes.Contains(name)
				&& !baseBuilder.HasAdequateProductionCount()
				&& !baseBuilder.BuildingsBeingProduced.Keys.Any(baseBuilder.Info.ProductionTypes.Contains))
				return true;

			return false;
		}

		ActorInfo ChooseBuildingToBuild(ProductionQueue queue)
		{
			var buildableThings = queue.BuildableItems();
			var availableCash = playerResources.GetCashAndResources();

			// This gets used quite a bit, so let's cache it here
			var power = GetProducibleBuilding(baseBuilder.Info.PowerTypes, buildableThings,
				a => a.TraitInfos<PowerInfo>().Where(i => i.EnabledByDefault).Sum(p => p.Amount));
			var openingBarracksPolicyActive = BaseBuilderQueueEvalCA.OpeningBarracksPolicy(botLimits != null,
				botLimits != null && botLimits.Info.PrioritizeBarracksBeforeRefinery,
				baseBuilder.Info.BarracksBeforeRefineryFactions.Contains(player.Faction.InternalName),
				baseBuilder.OpeningBarracksPriorityCompleted);

			// Wait for the queued opening structure to be placed before allowing another construction queue to proceed.
			if (openingBarracksPolicyActive && (baseBuilder.HasQueuedPowerPlant() || baseBuilder.HasQueuedBarracks()))
				return null;

			// First priority is to get out of a low power situation
			if (playerPower != null && playerPower.ExcessPower < minimumExcessPower && power != null && power.TraitInfos<PowerInfo>().Where(i => i.EnabledByDefault).Sum(p => p.Amount) > 0)
			{
					AIUtils.BotDebug("{0} decided to build {1}: Priority override (low power)", queue.Actor.Owner, power.Name);
					return power;
			}

			if (openingBarracksPolicyActive && !baseBuilder.HasBuiltOrQueuedBarracks())
			{
				// Finish the opening power plant before starting the barracks.
				if (!baseBuilder.HasCompletedPowerPlant())
				{
					if (!baseBuilder.HasQueuedPowerPlant() && power != null && queue.GetProductionCost(power) <= availableCash)
					{
						AIUtils.BotDebug("{0} decided to build {1}: Priority override (opening power)", queue.Actor.Owner, power.Name);
						return power;
					}
				}
				else
				{
					var barracks = GetProducibleBuilding(baseBuilder.Info.BarracksTypes, buildableThings);

					if (barracks != null && queue.GetProductionCost(barracks) <= availableCash)
					{
						if (HasSufficientPowerForActor(barracks))
						{
							AIUtils.BotDebug("{0} decided to build {1}: Priority override (opening barracks)", queue.Actor.Owner, barracks.Name);
							return barracks;
						}

						if (power != null && queue.GetProductionCost(power) <= availableCash)
						{
							AIUtils.BotDebug("{0} decided to build {1}: Priority override (opening barracks would cause low power)", queue.Actor.Owner, power.Name);
							return power;
						}
					}
				}
			}

			// Next is to build up a strong economy
			if (!baseBuilder.HasAdequateRefineryCount())
			{
				var refinery = GetProducibleBuilding(baseBuilder.Info.RefineryTypes, buildableThings);

				// RefineryLimit is shared across every faction the player owns, so always allow
				// a faction's own first refinery through even if that global cap has already
				// been reached by a different faction's construction yard.
				if (refinery != null && !baseBuilder.HasMaxRefineriesFor(refinery))
				{
					var pick = BaseBuilderQueueEvalCA.PickOrPower(refinery, power, HasSufficientPowerForActor);
					if (pick != null)
					{
						AIUtils.BotDebug("{0} decided to build {1}: Priority override ({2})", queue.Actor.Owner, pick.Name,
							pick == refinery ? "refinery" : "would be low power");
						return pick;
					}
				}
			}

			// REF-1 B1/B2 (§12.24 v2): the planner's own wants, after refineries but before the fraction roll —
			// the due MCV's missing prerequisite, then the cheapest crawl-eligible link building while the crawl
			// target is out of reach. Only under the refinery law (the provider publishes nulls otherwise), and
			// only when the wanted building isn't already in production.
			var law = baseBuilder.RefineryLawProvider();
			if (law != null)
			{
				foreach (var want in new[] { law.WantedMcvPrerequisite, law.WantedLinkBuilding })
				{
					if (want == null || (baseBuilder.BuildingsBeingProduced?.ContainsKey(want) ?? false))
						continue;

					var pick = GetProducibleBuilding(new HashSet<string> { want }, buildableThings, a => 0);
					if (pick == null)
						continue;

					var wantPick = BaseBuilderQueueEvalCA.PickOrPower(pick, power, HasSufficientPowerForActor);
					if (wantPick != null)
					{
						AIUtils.BotDebug("{0} decided to build {1}: Priority override ({2})", queue.Actor.Owner, wantPick.Name,
							wantPick == pick ? "REF-1 planner want" : "planner want would be low power");
						return wantPick;
					}
				}
			}

			// Build-order knobs (12.25): the active opening's next wanted building.
			var buildOrderKnobs = baseBuilder.BuildOrderKnobs;
			if (buildOrderKnobs != null)
			{
				var opening = ChooseOpeningBuilding(buildOrderKnobs, buildableThings, power);
				if (opening != null)
					return opening;
			}

			// ECON-A (SPEC_2026-10-05_econ_logistics Part A §3): an expansion demand injects its
			// refinery (then defence) just above ordinary wants — every essential override above
			// (power, the opening, the economy refinery, the planner's own wants) still wins.
			// Items schedule so finishTick ~= etaTick: they wait until eta - buildTime <= now.
			if (baseBuilder.ExpansionPrebuildEnabled)
			{
				var demandPick = ChooseExpansionDemandItem(queue, buildableThings);
				if (demandPick != null)
					return demandPick;
			}

			// Make sure that we can spend as fast as we are earning
			if (NewProductionCashThreshold > 0 && playerResources.GetCashAndResources() > NewProductionCashThreshold)
			{
				var production = GetProducibleBuilding(baseBuilder.Info.ProductionTypes, buildableThings);

				if (production != null && (ProductionTypeLimit <= 0 || !BaseBuilderQueueEvalCA.LimitReached(playerBuildings.Count(a => a.Info.Name == production.Name), ProductionTypeLimit)))
				{
					var pick = BaseBuilderQueueEvalCA.PickOrPower(production, power, HasSufficientPowerForActor);
					if (pick != null)
					{
						AIUtils.BotDebug("{0} decided to build {1}: Priority override ({2})", queue.Actor.Owner, pick.Name,
							pick == production ? "production" : "would be low power");
						return pick;
					}
				}
			}

			// Only consider building this if there is enough water inside the base perimeter and there are close enough adjacent buildings
			if (waterState == WaterCheck.EnoughWater && NewProductionCashThreshold > 0
				&& playerResources.Resources > NewProductionCashThreshold
				&& AIUtils.IsAreaAvailable<GivesBuildableArea>(world, player, world.Map, baseBuilder.Info.CheckForWaterRadius, baseBuilder.Info.WaterTerrainTypes))
			{
				var navalproduction = GetProducibleBuilding(baseBuilder.Info.NavalProductionTypes, buildableThings);
				var navalPick = BaseBuilderQueueEvalCA.PickOrPower(navalproduction, power, HasSufficientPowerForActor);
				if (navalPick != null)
				{
					AIUtils.BotDebug("{0} decided to build {1}: Priority override ({2})", queue.Actor.Owner, navalPick.Name,
						navalPick == navalproduction ? "navalproduction" : "would be low power");
					return navalPick;
				}
			}

			// Create some head room for resource storage if we really need it
			// REF-1 (maintainer): under the law the 80% override spammed silos — a healthy refinery
			// economy keeps storage above 80% permanently, so the override won every pick. Silos stay
			// wanted only when storage is nearly full and no silo is already in production; otherwise
			// the queue spends on production/defence instead.
			var wantSilo = RefineryLawSilo.Wanted(law != null, playerResources.Resources, playerResources.ResourceCapacity,
				baseBuilder.BuildingsBeingProduced?.Keys.Any(baseBuilder.Info.SiloTypes.Contains) ?? false);

			if (wantSilo)
			{
				var silo = GetProducibleBuilding(baseBuilder.Info.SiloTypes, buildableThings);
				var siloPick = BaseBuilderQueueEvalCA.PickOrPower(silo, power, HasSufficientPowerForActor);
				if (siloPick != null)
				{
					AIUtils.BotDebug("{0} decided to build {1}: Priority override ({2})", queue.Actor.Owner, siloPick.Name,
						siloPick == silo ? "silo" : "would be low power");
					return siloPick;
				}
			}

			// BP-2 (§19.15): the front/back advisor's radar want — one provider per defended front plus
			// justified extras. It slots after the economy overrides (production, naval, silo) and before
			// the fraction roll: radar never starves spending, but a met need never eats the build slot.
			var frontBack = FrontBackAdvisor();
			if (frontBack != null && frontBack.WantedRadarProviders > OwnedRadarProviders())
			{
				var radar = frontBack.PreferredRadarProvider(GetRadarProducibles(buildableThings));
				var radarPick = BaseBuilderQueueEvalCA.PickOrPower(radar, power, HasSufficientPowerForActor);
				if (radarPick != null)
				{
					AIUtils.BotDebug("{0} decided to build {1}: Priority override ({2})", queue.Actor.Owner, radarPick.Name,
						radarPick == radar ? "radar for front" : "radar would be low power");
					return radarPick;
				}
			}

			// Build everything else
			foreach (var frac in baseBuilder.Info.BuildingFractions.OrderBy(kv => kv.Key).Shuffle(BotRng.For(player)))
			{
				var name = frac.Key;

				// Does this building have initial delay, if so have we passed it?
				if (baseBuilder.Info.BuildingDelays != null &&
					baseBuilder.Info.BuildingDelays.TryGetValue(name, out var delay) &&
					BaseBuilderQueueEvalCA.StillDelayed(KnobTicks(delay * buildingDelayModifier / 100, name), world.WorldTick))
					continue;

				// Does this building have an interval which hasn't elapsed yet?
				if (BaseBuilderQueueEvalCA.IntervalBlocks(
						baseBuilder.Info.BuildingIntervals != null && baseBuilder.Info.BuildingIntervals.ContainsKey(name),
						activeBuildingIntervals.ContainsKey(name)))
					continue;

				// Can we build this structure?
				if (!buildableThings.Any(b => b.Name == name))
					continue;

				// Check the number of this structure and its variants
				var actorInfo = world.Map.Rules.Actors[name];
				var buildingVariantInfo = actorInfo.TraitInfoOrDefault<PlaceBuildingVariantsInfo>();
				var variants = buildingVariantInfo?.Actors ?? [];

				var count = playerBuildings.Count(a =>
					a.Info.Name == name || variants.Contains(a.Info.Name)) +
					(baseBuilder.BuildingsBeingProduced.TryGetValue(name, out var num) ? num : 0);

				// Do we want to build this structure?
				// Build-order knobs (12.25): production/tech/defence/support/greed scale the fraction of their categories (milli = 1000 without a provider).
				if (!BaseBuilderQueueEvalCA.FractionAdmits(count, frac.Value, FractionMilli(name), playerBuildings.Length))
					continue;

				if (botLimits != null && baseBuilder.Info.ProductionTypes.Contains(name) && BaseBuilderQueueEvalCA.LimitReached(count, ProductionTypeLimit))
				{
					AIUtils.BotDebug("{0} decided to build {1} but limit of {2} already reached)", queue.Actor.Owner, name, ProductionTypeLimit);
					continue;
				}

				if (baseBuilder.TryGetBuildingLimit(name, out var limit) && BaseBuilderQueueEvalCA.LimitReached(count, limit))
				{
					AIUtils.BotDebug("{0} decided to build {1} but limit of {2} already reached)", queue.Actor.Owner, name, limit);
					continue;
				}

				if (baseBuilder.Info.RefineryTypes.Contains(name) && baseBuilder.HasMaxRefineriesFor(actorInfo))
					continue;

				// If we're considering to build a naval structure, check whether there is enough water inside the base perimeter
				// and any structure providing buildable area close enough to that water.
				// TODO: Extend this check to cover any naval structure, not just production.
				if (BaseBuilderQueueEvalCA.NavalBlocked(baseBuilder.Info.NavalProductionTypes.Contains(name),
						waterState == WaterCheck.NotEnoughWater,
						() => AIUtils.IsAreaAvailable<GivesBuildableArea>(world, player, world.Map, baseBuilder.Info.CheckForWaterRadius, baseBuilder.Info.WaterTerrainTypes)))
					continue;

				// Will this put us into low power?
				var actor = world.Map.Rules.Actors[name];
				if (BaseBuilderQueueEvalCA.LowPowerBlocked(playerPower != null, playerPower?.ExcessPower ?? 0, minimumExcessPower, HasSufficientPowerForActor(actor)))
				{
					// Try building a power plant instead
					if (power != null && power.TraitInfos<PowerInfo>().Where(i => i.EnabledByDefault).Sum(pi => pi.Amount) > 0)
					{
						if (playerPower.PowerOutageRemainingTicks > 0)
							AIUtils.BotDebug("{0} decided to build {1}: Priority override (is low power)", queue.Actor.Owner, power.Name);
						else
							AIUtils.BotDebug("{0} decided to build {1}: Priority override (would be low power)", queue.Actor.Owner, power.Name);

						return power;
					}
				}

				// Lets build this
				AIUtils.BotDebug("{0} decided to build {1}: Desired is {2} ({3} / {4}); current is {5} / {4}",
					queue.Actor.Owner, name, frac.Value, frac.Value * playerBuildings.Length, playerBuildings.Length, count);
				return actor;
			}

			// Too spammy to keep enabled all the time, but very useful when debugging specific issues.
			// AIUtils.BotDebug("{0} couldn't decide what to build for queue {1}.", queue.Actor.Owner, queue.Info.Group);
			return null;
		}

		// REF-1 (§12.24 v2): the cells of `zone` dilated by one Chebyshev ring — the gap-0 zone of a field's resource
		// cells is the cells themselves plus their 8-neighbours; dilating twice gives the gap-1 ring.
		static HashSet<CPos> Dilate(IEnumerable<CPos> cells)
		{
			var result = new HashSet<CPos>(cells);
			foreach (var c in cells)
				for (var dy = -1; dy <= 1; dy++)
					for (var dx = -1; dx <= 1; dx++)
						result.Add(new CPos(c.X + dx, c.Y + dy));

			return result;
		}

		// REF-1 (§12.24 v2): the harvester dock cell must not sit on valuable resources, and needs at least one on-map
		// neighbour outside the footprint so the dock stays reachable — a refinery walled off the field is useless.
		// FIX-RA-REFINERY: "the footprint" here means the impassable cells — BuildingInfo.Tiles also
		// yields the passable '=' bib, which walled the RA proc's dock inside its own footprint and
		// failed the exit check at every legal site.
		enum DockAccess { Clear, OnResource, NoExit }

		DockAccess CheckDockAccess(ActorInfo actorInfo, BuildingInfo bi, CPos topLeft, WVec dockOffset, IReadOnlySet<string> valuable)
		{
			var dock = world.Map.CellContaining(world.Map.CenterOfCell(topLeft) + bi.CenterOffset(world) + dockOffset);
			if (valuable != null && resourceLayer != null && valuable.Contains(resourceLayer.GetResource(dock).Type))
				return DockAccess.OnResource;

			var footprint = new HashSet<CPos>(BaseBuilderQueueEvalCA.DockBlockingFootprint(bi.Footprint, topLeft));
			return BaseBuilderQueueEvalCA.DockHasExit(dock, footprint, world.Map.Contains) ? DockAccess.Clear : DockAccess.NoExit;
		}

		// REF-1 (§12.24 v2): the placeable cell NEAREST the anchor whose footprint sits flush on the claim's resource
		// cells — gap 0 (a footprint cell on or 8-adjacent to a resource cell), gap 1 only when no gap-0 cell is
		// placeable, never more (the caller retries later instead of falling back home). Candidates are the annulus
		// cells ordered by distance to the anchor (deterministic: distance, then X, then Y), so the winning cell is
		// the nearest one meeting the law. Anchors with no known resource cells (a lone spreader) keep the plain
		// nearest-placeable behaviour.
		(CPos? Location, int Gap, int Variant) LawRefineryPlacement(string actorType, bool distanceToBaseIsImportant, Actor producer,
			CPos anchor, IReadOnlyCollection<CPos> fieldCells, int claimRadius)
		{
			var actorInfo = world.Map.Rules.Actors[actorType];
			var bi = actorInfo.TraitInfoOrDefault<BuildingInfo>();
			if (bi == null)
				return (null, -1, 0);

			HashSet<CPos> zone0 = null, zone1 = null;
			if (fieldCells != null && fieldCells.Count > 0)
			{
				zone0 = Dilate(fieldCells);
				zone1 = Dilate(zone0);
				zone1.ExceptWith(zone0);
			}

			// Every dock counts: nexus/hatchery/assimilator carry DockHost + @DOCK2 + @DOCK3,
			// and checking only the first instance can reject a cell whose other docks are fine.
			// No DockHost falls back to a zero offset, exactly as before.
			var dockOffsets = actorInfo.TraitInfos<DockHostInfo>().Select(d => d.DockOffset).DefaultIfEmpty(WVec.Zero).ToList();
			var valuable = baseBuilder.ResourceMapModule?.Info.ValuableResourceTypes;

			CPos? gap1 = null;

			// FIX-RA-REFINERY diagnostics: tally the per-cell rejection reasons so a failed sweep
			// emits one bounded line — the reason chain (buildability / too-far / dock-on-resource /
			// no-exit / gap-illegal) instead of a per-cell dump.
			var scanned = 0;
			var rejectedBuild = 0;
			var rejectedFar = 0;
			var rejectedDockResource = 0;
			var rejectedDockExit = 0;
			var rejectedGap = 0;
			foreach (var cell in world.Map.FindTilesInAnnulus(anchor, 0, claimRadius)
				.OrderBy(c => (c - anchor).LengthSquared).ThenBy(c => c.X).ThenBy(c => c.Y))
			{
				scanned++;
				if (!world.CanPlaceBuilding(cell, actorInfo, bi, null))
				{
					rejectedBuild++;
					continue;
				}

				if (distanceToBaseIsImportant && !bi.IsCloseEnoughToBase(world, player, actorInfo, producer, cell))
				{
					rejectedFar++;
					continue;
				}

				// A cell fails the dock check only when every dock is unreachable; a dock that lost
				// solely to the valuable-resource rule keeps the resource signature for the tally.
				var dockOk = false;
				var dockAllResource = true;
				foreach (var off in dockOffsets)
				{
					var access = CheckDockAccess(actorInfo, bi, cell, off, valuable);
					if (access == DockAccess.Clear)
					{
						dockOk = true;
						break;
					}

					if (access != DockAccess.OnResource)
						dockAllResource = false;
				}

				if (!dockOk)
				{
					if (dockAllResource)
						rejectedDockResource++;
					else
						rejectedDockExit++;

					continue;
				}

				if (zone0 == null)
					return (cell, -1, 0);

				var footprint = bi.Tiles(cell).ToList();
				var gap = BaseBuilderQueueEvalCA.FootprintGap(footprint, zone0, zone1);
				if (gap == 0)
					return (cell, 0, 0);

				if (gap == 1 && gap1 == null)
					gap1 = cell;
				else if (gap < 0)
					rejectedGap++;
			}

			if (gap1.HasValue)
				return (gap1.Value, 1, 0);

			AIUtils.BotDebug($"{player} law refinery {actorType}: no site at anchor {anchor} — {scanned} scanned, {rejectedBuild} unbuildable, {rejectedFar} too-far, {rejectedDockResource} dock-on-resource, {rejectedDockExit} dock-no-exit, {rejectedGap} gap-illegal at tick {world.WorldTick}");
			return (null, -1, 0);
		}

		// Find the buildable cell that is closest to pos and centered around center.
		// The building gap belongs to the placement advisor (BuildingGapRule.Resolve; no advisor = no gap);
		// defense-style callers pass defenseGap so walls/turrets can still form tighter lines.
		(CPos? Location, CPos Center, int Variant) findPos(string actorType, bool distanceToBaseIsImportant, Actor producer, CPos center, CPos target, int minRange, int maxRange, int distanceRequirement = 0, bool sortMax = false, bool defenseGap = false, CPos? anchorTieBreak = null, bool anchorOrder = false, bool bypassAdvisor = false)
		{
			var actorInfo = world.Map.Rules.Actors[actorType];
			var actorVariant = 0;
			var buildingVariantInfo = actorInfo.TraitInfoOrDefault<PlaceBuildingVariantsInfo>();
			var bi = actorInfo.TraitInfoOrDefault<BuildingInfo>();
			if (bi == null)
				return (null, center, 0);

			var cells = world.Map.FindTilesInAnnulus(center, minRange, maxRange);

			// Sort by distance to target if we have one
			if (center != target || anchorOrder)
			{
				cells = sortMax ? cells.OrderByDescending(c => (c - target).LengthSquared) : cells.OrderBy(c => (c - target).LengthSquared);

				// FE-1 (§12.24): the refinery claim takes the placeable cell nearest its anchor; ties go to the cell
				// closest to the field's resource cells. OrderBy is stable, so every other caller is unchanged.
				if (anchorOrder && anchorTieBreak.HasValue)
				{
					var tie = anchorTieBreak.Value;
					cells = ((IOrderedEnumerable<CPos>)cells).ThenBy(c => (c - tie).LengthSquared);
				}

				// Rotate building if we have a Facings in buildingVariantInfo.
				// If we don't have Facings in buildingVariantInfo, use a random variant
				if (buildingVariantInfo?.Actors != null)
				{
					if (BaseBuilderQueueEvalCA.PicksRandomVariant(true, buildingVariantInfo.Facings != null))
						actorVariant = BotRng.For(player).Next(buildingVariantInfo.Actors.Length + 1);
					else
					{
						// The rotation Y point to upside vertically, so -Y = Y(rotation)
						actorVariant = BaseBuilderQueueEvalCA.PickFacingVariant(
							world.Map.CenterOfCell(target) - world.Map.CenterOfCell(center), buildingVariantInfo.Facings);
					}
				}
			}
			else
			{
				cells = cells.Shuffle(BotRng.For(player));

				if (buildingVariantInfo?.Actors != null)
					actorVariant = BotRng.For(player).Next(buildingVariantInfo.Actors.Length + 1);
			}

			if (actorVariant != 0)
			{
				actorInfo = world.Map.Rules.Actors[buildingVariantInfo.Actors[actorVariant - 1]];
				bi = actorInfo.TraitInfoOrDefault<BuildingInfo>();
			}

			// Cameo (building spacing): cells within `gap` of an own building's footprint are
			// off-limits so a new building keeps that much empty space to existing structures.
			// The buffer is built once per search from the per-tick playerBuildings cache —
			// never relaxed inside this call; an exhausted annulus returns null and the
			// caller retries later. AllowInvalidPlacement actors bypass CanPlaceBuilding
			// entirely, so they keep bypassing the spacing rule too — same opt-out semantic.
			var advisor = player.PlayerActor.TraitsImplementing<IBotPlacementAdvisor>().FirstOrDefault(a => a.IsActive);

			// Refineries are owned by field proximity (EX-2 claims, SP-1 fix 1ba1db9b8): spacing never touches them,
			// the hard gap included — a gap-pushed refinery reads its field as unserved and the builder stacks another.
			var isRefinery = world.Map.Rules.Actors.TryGetValue(actorType, out var placedInfo) && placedInfo.HasTraitInfo<RefineryInfo>();
			var gap = isRefinery ? 0 : BuildingGapRule.Resolve(advisor, defenseGap);
			var ownBuildingBuffer = gap > 0 ? OwnBuildingBufferCells(gap) : null;

			// Cameo (§12.20): an advisor that ranks re-ranks a bounded prefix of placeable, gap-valid cells
			// (spread-out bases instead of first-valid packing). No ranking advisor = first valid cell wins,
			// exactly as upstream.
			// REF-1 B1 (crawl-trace §8): an aimed placement keeps its distance sort — the spacing advisor's
			// re-rank would override the aim, so anchor-order and bypassAdvisor calls skip it.
			if (advisor != null && advisor.RanksCandidates && !anchorOrder && !bypassAdvisor)
			{
				var candidates = new List<CPos>();
				foreach (var cell in cells)
				{
					if (!BaseBuilderQueueEvalCA.PlacementCellAdmitted(
							() => world.CanPlaceBuilding(cell, actorInfo, bi, null),
							() => !distanceToBaseIsImportant || bi.IsCloseEnoughToBase(world, player, actorInfo, producer, cell),
							() => distanceRequirement <= 0 || (cell - target).LengthSquared <= distanceRequirement * distanceRequirement,
							() => ownBuildingBuffer == null || bi.AllowInvalidPlacement || !bi.Tiles(cell).Any(ownBuildingBuffer.Contains)))
						continue;

					candidates.Add(cell);
					if (candidates.Count >= baseBuilder.Info.PlacementAdvisorCandidates)
						break;
				}

				if (candidates.Count > 0)
				{
					var chosen = advisor.ChooseCell(actorInfo, candidates,
						c => world.CanPlaceBuilding(c, actorInfo, bi, null));
					return (BaseBuilderQueueEvalCA.AdvisorPick(chosen, candidates), center, actorVariant);
				}
			}
			else
			{
				foreach (var cell in cells)
				{
					if (!BaseBuilderQueueEvalCA.PlacementCellAdmitted(
							() => world.CanPlaceBuilding(cell, actorInfo, bi, null),
							() => !distanceToBaseIsImportant || bi.IsCloseEnoughToBase(world, player, actorInfo, producer, cell),
							() => distanceRequirement <= 0 || (cell - target).LengthSquared <= distanceRequirement * distanceRequirement,
							() => ownBuildingBuffer == null || bi.AllowInvalidPlacement || !bi.Tiles(cell).Any(ownBuildingBuffer.Contains)))
						continue;

					return (cell, center, actorVariant);
				}
			}

			return (null, center, 0);
		}

		// Cameo (building spacing): every cell within `gap` cells of an own building's footprint —
		// the buffer zone a new footprint must not overlap. Symmetric check: a candidate is rejected
		// when one of its footprint cells lands in this set.
		HashSet<CPos> OwnBuildingBufferCells(int gap)
		{
			if (playerBuildings == null)
				return new HashSet<CPos>();

			return BuildingGapRule.BufferCells(
				playerBuildings.SelectMany(b => b.Info.TraitInfoOrDefault<BuildingInfo>()?.Tiles(b.Location) ?? Enumerable.Empty<CPos>()), gap);
		}

		// Cameo: ask the first ACTIVE defence-placement advisor (resolved on each use, so a late-enabled one is seen)
		// for a cell. canPlace is the check findPos applies.
		CPos? AdvisedDefenseCell(ActorInfo actorInfo, bool distanceToBaseIsImportant, Actor producer, CPos? baseCenterOverride = null)
		{
			var advisor = player.PlayerActor.TraitsImplementing<IBotDefensePlacementAdvisor>().FirstOrDefault(a => a.IsActive);
			var bi = actorInfo.TraitInfoOrDefault<BuildingInfo>();
			if (advisor == null || bi == null)
				return null;

			var baseCenter = baseCenterOverride ?? baseBuilder.GetBaseCenterForActor(actorInfo);

			// The advisor's candidate cells get the same spacing rule as findPos defenses.
			var gap = BuildingGapRule.Resolve(player.PlayerActor.TraitsImplementing<IBotPlacementAdvisor>().FirstOrDefault(a => a.IsActive), true);
			var ownBuildingBuffer = gap > 0 ? OwnBuildingBufferCells(gap) : null;

			return advisor.ChooseDefenseCell(actorInfo, baseBuilder.Info.AntiAirTypes.Contains(actorInfo.Name), baseCenter,
				cell => world.CanPlaceBuilding(cell, actorInfo, bi, null)
					&& (!distanceToBaseIsImportant || bi.IsCloseEnoughToBase(world, player, actorInfo, producer, cell))
					&& (ownBuildingBuffer == null || bi.AllowInvalidPlacement || !bi.Tiles(cell).Any(ownBuildingBuffer.Contains)));
		}

		// BP-2 (§19.15): every cell in the base annulus that already passes the checks findPos applies —
		// CanPlaceBuilding, IsCloseEnoughToBase, the spacing-advisor gap. The advisor picks among these;
		// an empty list means "no legal cell anywhere", which the planner reads as its hold case.
		List<CPos> AdvisorLegalCells(ActorInfo actorInfo, bool distanceToBaseIsImportant, Actor producer, CPos? baseCenterOverride = null)
		{
			var bi = actorInfo.TraitInfoOrDefault<BuildingInfo>();
			var legal = new List<CPos>();
			if (bi == null)
				return legal;

			var baseCenter = baseCenterOverride ?? baseBuilder.GetBaseCenterForActor(actorInfo);
			var spacingAdvisor = player.PlayerActor.TraitsImplementing<IBotPlacementAdvisor>().FirstOrDefault(a => a.IsActive);
			var gap = BuildingGapRule.Resolve(spacingAdvisor, false);
			var ownBuildingBuffer = gap > 0 ? OwnBuildingBufferCells(gap) : null;

			foreach (var cell in world.Map.FindTilesInAnnulus(baseCenter, baseBuilder.Info.MinBaseRadius,
				Math.Max(baseBuilder.Info.MaxBaseRadius, baseBuilder.Info.MaximumDefenseRadius)))
			{
				// The same admission gate findPos applies, minus the requirement-distance clause —
				// the advisor's own front/back distance semantics cover that axis.
				if (!BaseBuilderQueueEvalCA.PlacementCellAdmitted(
						() => world.CanPlaceBuilding(cell, actorInfo, bi, null),
						() => !distanceToBaseIsImportant || bi.IsCloseEnoughToBase(world, player, actorInfo, producer, cell),
						() => true,
						() => ownBuildingBuffer == null || bi.AllowInvalidPlacement || !bi.Tiles(cell).Any(ownBuildingBuffer.Contains)))
					continue;

				legal.Add(cell);
			}

			return legal;
		}

		// The variant an advisor-claimed placement uses: findPos's non-facing random draw, 0 otherwise
		// (a front-side facing is the advisor's to encode later; the default facing is honest today).
		int FrontBackVariant(ActorInfo actorInfo)
		{
			var variants = actorInfo.TraitInfoOrDefault<PlaceBuildingVariantsInfo>();
			return BaseBuilderQueueEvalCA.PicksRandomVariant(variants?.Actors != null,
				variants != null && variants.Facings != null)
					? BotRng.For(player).Next(variants.Actors.Length + 1) : 0;
		}

		(CPos? Location, CPos? BaseCenter, int Variant) ChooseBuildLocation(string actorType, bool distanceToBaseIsImportant, Actor producer, BuildingType type, ExpansionDemand demand = null)
		{
			var actorInfo = world.Map.Rules.Actors[actorType];
			var baseCenter = baseBuilder.GetBaseCenterForActor(actorInfo);

			// BP-2 (§19.15): an active front/back advisor owns the Radar, Production and Valuable cells —
			// the classes the Building/Fragile names only approximated. The pick carries the diagnostics
			// for the placement log. Hold keeps the produced building queued (a radar on a front with no
			// defence line waits — it never goes forward); a null cell without Hold falls back to the
			// classic path below. Every other class — and every placement with no active advisor — is
			// untouched, draw for draw.
			if (type == BuildingType.Building || type == BuildingType.Fragile)
			{
				var frontBack = FrontBackAdvisor();
				if (frontBack != null)
				{
					var cls = frontBack.Classify(actorInfo);
					if (cls == FrontBackClass.Radar || cls == FrontBackClass.Production || cls == FrontBackClass.Valuable)
					{
						var pick = frontBack.ChooseCell(cls, actorInfo, baseCenter, AdvisorLegalCells(actorInfo, distanceToBaseIsImportant, producer));
						lastFrontBackPick = pick;
						if (pick.Hold)
						{
							frontBackHold = true;
							return (null, null, 0);
						}

						if (pick.Cell != null)
							return (pick.Cell, baseCenter, FrontBackVariant(actorInfo));
					}
				}
			}

			switch (type)
			{
				case BuildingType.Defense:

					// ECON-A (SPEC_2026-10-05 Part A §5): the held defence places at the new outpost —
					// the front/back advisor's Defence class first (BP-SPEC §12.35), then the
					// defence-placement advisor around the outpost, then the classic defence scan
					// centred there, aimed at the field the refinery serves. A null cell runs the
					// normal placement failure path — the item retries like every other building.
					if (demand != null)
					{
						var outpost = demand.Deployed ? demand.DeployedYardLoc : demand.ConyardLoc;
						var demandFrontBack = FrontBackAdvisor();
						if (demandFrontBack != null)
						{
							var pick = demandFrontBack.ChooseCell(FrontBackClass.Defence, actorInfo, outpost,
								AdvisorLegalCells(actorInfo, distanceToBaseIsImportant, producer, outpost));
							lastFrontBackPick = pick;
							if (pick.Hold)
							{
								frontBackHold = true;
								return (null, null, 0);
							}

							if (pick.Cell != null)
								return (pick.Cell, outpost, FrontBackVariant(actorInfo));
						}

						var outpostAdvised = AdvisedDefenseCell(actorInfo, distanceToBaseIsImportant, producer, outpost);
						if (outpostAdvised != null)
						{
							demandAdvisedDefense = true;
							return (outpostAdvised, outpost, 0);
						}

						return findPos(actorType, distanceToBaseIsImportant, producer, outpost, demand.ResourceLoc,
							baseBuilder.Info.MinimumDefenseRadius, baseBuilder.Info.MaximumDefenseRadius, defenseGap: true);
					}

					// Build near the closest enemy structure
					var defenseCenter = baseBuilder.DefenseCenter ?? baseCenter;
					var closestEnemy = world.ActorsHavingTrait<Building>()
						.Where(a => !a.Disposed && player.RelationshipWith(a.Owner) == PlayerRelationship.Enemy)
						.ClosestToIgnoringPath(world.Map.CenterOfCell(defenseCenter));

					var targetCell = closestEnemy != null ? closestEnemy.Location : baseCenter;
					return findPos(actorType, distanceToBaseIsImportant, producer, defenseCenter, targetCell, baseBuilder.Info.MinimumDefenseRadius, baseBuilder.Info.MaximumDefenseRadius,
						defenseGap: true);

				case BuildingType.Fragile:
					// Build away from where enemy last attacked
					var fragileDefenseCenter = baseBuilder.DefenseCenter ?? baseCenter;
					return findPos(actorType, distanceToBaseIsImportant, producer, baseCenter, fragileDefenseCenter, baseBuilder.Info.MinBaseRadius,
						distanceToBaseIsImportant ? baseBuilder.Info.MaxBaseRadius : world.Map.Grid.MaximumTileSearchRange, sortMax: true);

				case BuildingType.Refinery:

					// ECON-A: a deployed demand's refinery serves its own outpost — the entry keyed on
					// its MCV is the request, so the law's claim and the classic paths below need no
					// special-casing (they already consume RequestedRefineries on success). A same-named
					// bound twin can place after the entry was consumed — drop the stale key to null
					// rather than let the indexer reads below throw.
					var requestRef = demand?.Mcv ?? BaseBuilderQueueEvalCA.FirstRequestedRefinery(baseBuilder.RequestedRefineries);
					if (requestRef != null && !baseBuilder.RequestedRefineries.ContainsKey(requestRef))
						requestRef = null;

					// REF-1 (§12.24 v2, DESIGN §19.1b): under the refinery law EVERY refinery path routes through the
					// provider's claim — the first refinery, the MCV-requested one (its yard's nearest unserved field
					// via `near`), and the planner's claim alike. Placement sits flush to the claim field's resource
					// cells (gap 0, gap 1 only when 0 is unplaceable, never more); a failed claim retries later — the
					// old base-centre path below is unreachable while the law is active, because a refinery stacked
					// at home serving no anchor is exactly what the law forbids.
					var law = baseBuilder.RefineryLawProvider();
					if (law != null)
					{
						var near = requestRef != null ? baseBuilder.RequestedRefineries[requestRef].ConyardLoc : (CPos?)null;

						// ECON-A-FIX (R6): a bound demand's refinery re-adopts the anchor it reserved at
						// queue admission — held out of every other claim in between. A lost or stale
						// hold takes whatever the law offers now; the law's claim order stands either way.
						RefineryAnchorClaim? claim = null;
						if (demand?.ReservedClaim is { } reserved
							&& law.RefineryAnchorReserved(reserved.Anchor, demand))
							claim = reserved;
						else
							claim = law.NextRefineryClaim(near);

						if (claim is { } c)
						{
							var placed = LawRefineryPlacement(actorType, distanceToBaseIsImportant, producer,
								c.Anchor, c.ResourceCells, law.ExpansionTargetClaimRadius);
							if (placed.Location != null)
							{
								Log.Write("debug", $"AI ({player.ClientIndex}): REF-1 refinery {actorType} at {placed.Location.Value} claims anchor {c.Anchor} field {c.FieldId} tier {c.Tier} gap {placed.Gap} at tick {world.WorldTick}");
								law.RefineryClaimCommitted(c.Anchor);
								refineryClaimed = true;
								if (requestRef != null)
									baseBuilder.RequestedRefineries.Remove(requestRef);
								return (placed.Location, c.Anchor, placed.Variant);
							}
						}
						else
						{
							// FIX-RA-REFINERY diagnostics: the other law-null reason — nothing claimed
							// this sweep. Bounded: one line per failed placement attempt.
							AIUtils.BotDebug($"{player} law refinery {actorType}: no claim available at tick {world.WorldTick}");
						}

						return (null, null, 0);
					}

					// Cameo (AI_ARCHITECTURE §12.13, EX-2): the planner's field is in reach and unclaimed, so the refinery goes
					// there, close enough to count as claiming it. A refinery the MCV module requested keeps priority.
					var claimer = requestRef == null ? baseBuilder.ExpansionWantsRefinery() : null;
					if (claimer != null)
					{
						// EX-2c: the claim field may differ from the crawl aim — any free field already in
						// reach qualifies, so an outpost yard claims its local field without waiting to
						// become the crawl target. Null falls back to the expansion target (EX-2).
						var field = BaseBuilderQueueEvalCA.ClaimFieldOrTarget(claimer.RefineryClaimTarget, claimer.ExpansionTarget.Value);

						// The annulus must be around the FIELD, not baseCenter: a crawled-to field sits beyond
						// baseCenter + MaxBaseRadius + claimRadius, so centering on the base yields zero candidate
						// cells, the claim silently fails, and the fallback drops the refinery back home.
						var claim = findPos(actorType, distanceToBaseIsImportant, producer, field, baseCenter,
							0, claimer.ExpansionTargetClaimRadius);
						if (claim.Location != null)
						{
							Log.Write("debug", $"AI ({player.ClientIndex}): EX-2 refinery {actorType} at {claim.Location.Value} claims field {field} at tick {world.WorldTick}");
							refineryClaimed = true;
							return claim;
						}
					}

					// Try and place the refinery near a resource field
					if (resourceLayer != null)
					{
						// If we have failed to place to the requested refinery point, try and place it near the base center
						var resourceBaseCenter = BaseBuilderQueueEvalCA.ResourcePickCenter(failCount, baseCenter,
							requestRef != null && failCount <= 0 ? baseBuilder.RequestedRefineries[requestRef].ConyardLoc : null,
							baseBuilder.ResourceConyardCenter);

						// If we have a ResourceMapModule, only consider the resource types it considers valuable
						// Otherwise consider any resource type
						var nearbyResources = world.Map
							.FindTilesInAnnulus(resourceBaseCenter, baseBuilder.Info.MinBaseRadius, baseBuilder.Info.MaxBaseRadius)
							.Where(c => baseBuilder.ResourceMapModule != null ?
							baseBuilder.ResourceMapModule.Info.ValuableResourceTypes.Contains(resourceLayer.GetResource(c).Type)
							: resourceLayer.GetResource(c).Type != null);

						// Find the closest refinery we have if we have any when not failing to place for the first time
						var closestRefinery = failCount <= 0
							? baseBuilder.RefineryBuildings.Actors.Where(a => !a.IsDead).OrderBy(a => a.ActorID).ClosestToIgnoringPath(world.Map.CenterOfCell(resourceBaseCenter))
							: null;

						IEnumerable<CPos> resourcesShouldCheck = null;

						if (closestRefinery == null)
							resourcesShouldCheck = nearbyResources.Shuffle(BotRng.For(player)).Take(baseBuilder.Info.MaxResourceCellsToCheck);
						else if (requestRef != null)
						{
							resourcesShouldCheck = nearbyResources.OrderBy(c => (c - baseBuilder.RequestedRefineries[requestRef].ResourceLoc).LengthSquared)
								.Take(baseBuilder.Info.MaxResourceCellsToCheck);
						}
						else
						{
							// Cameo (§12.13, EX-2): an expansion planner wants every field served — sample only cells
							// of fields no own refinery covers yet, so refineries spread to new fields instead of
							// stacking on the far edge of the home field. No provider or no resource map (classic):
							// today's ordering, unchanged.
							var candidates = nearbyResources;
							if (baseBuilder.HasExpansionGuidance && baseBuilder.ResourceMapModule != null)
							{
								var ownRefineryCells = baseBuilder.RefineryBuildings.Actors
									.Where(a => !a.IsDead)
									.Select(a => a.Location)
									.ToList();
								candidates = BaseBuilderBotModuleCA.PreferUnservedResourceCells(
									nearbyResources,
									c => baseBuilder.ResourceMapModule.FindClosestIndiceFromCPos(c).ResourceCellsCenter,
									ownRefineryCells, baseBuilder.Info.RefineryUnservedRadiusCells);
							}

							resourcesShouldCheck = candidates.OrderByDescending(c => (c - closestRefinery.Location).LengthSquared)
								.Take(baseBuilder.Info.MaxResourceCellsToCheck);
						}

						foreach (var r in resourcesShouldCheck)
						{
							var found = findPos(actorType, distanceToBaseIsImportant, producer, resourceBaseCenter, r, baseBuilder.Info.MinBaseRadius, baseBuilder.Info.MaxBaseRadius);
							if (found.Location != null)
							{
								if (baseBuilder.RequestedRefineries.Count > 0)
									baseBuilder.RequestedRefineries.Remove(requestRef);
								return found;
							}
						}
					}

					if (baseBuilder.RequestedRefineries.Count > 0)
						baseBuilder.RequestedRefineries.Remove(requestRef);

					// Try and find a free spot somewhere else in the base
					return findPos(actorType, distanceToBaseIsImportant, producer, baseCenter, baseCenter, baseBuilder.Info.MinBaseRadius, baseBuilder.Info.MaxBaseRadius);

				case BuildingType.BaseCrawl:

					// Cameo (AI_ARCHITECTURE §12.13, EX-1): walk toward the planner's target field, one building at a
					// time (findPos takes the placeable cell nearest the target), instead of a random resource cell or
					// the enemy building found by scanning every building on the map.
					var expansionTarget = baseBuilder.ExpansionTarget();
					if (expansionTarget != null)
					{
						// REF-1 B1: under the law the aim is the target field's resource EDGE nearest our frontier —
						// every building placed to close the gap; the field centre remains the aim when no edge is
						// published (classic, switch-off, fields without cells). The re-rank advisor is bypassed
						// on this aimed path so the distance sort survives (crawl-trace §8).
						var crawlLaw = baseBuilder.RefineryLawProvider();
						var crawlAim = crawlLaw?.CrawlTargetEdge ?? expansionTarget;
						var toward = findPos(actorType, distanceToBaseIsImportant, producer, baseCenter, crawlAim.Value,
							baseBuilder.Info.MinBaseRadius, baseBuilder.Info.BaseCrawlRadius, bypassAdvisor: crawlLaw != null);
						if (toward.Location != null)
						{
							Log.Write("debug", $"AI ({player.ClientIndex}): EX-1 BaseCrawl {actorType} at {toward.Location.Value} toward field {crawlAim.Value} at tick {world.WorldTick}");
							return toward;
						}
					}

					// Try and place the refinery near a resource field
					if (resourceLayer != null)
					{
						var nearbyResources = world.Map.FindTilesInAnnulus(baseCenter, baseBuilder.Info.MinBaseRadius, baseBuilder.Info.BaseCrawlRadius)
							.Where(a => resourceLayer.GetResource(a).Type != null)
							.Shuffle(BotRng.For(player)).Take(baseBuilder.Info.MaxResourceCellsToCheck);

						foreach (var r in nearbyResources)
						{
							var found = findPos(actorType, distanceToBaseIsImportant, producer, baseCenter, r, baseBuilder.Info.MinBaseRadius, baseBuilder.Info.MaxBaseRadius);
							if (found.Location != null)
								return found;
						}
					}

					// Try and find a free spot somewhere else in the base
					var crawlDefenseCenter = baseBuilder.DefenseCenter ?? baseCenter;
					closestEnemy = world.ActorsHavingTrait<Building>()
						.Where(a => !a.Disposed && player.RelationshipWith(a.Owner) == PlayerRelationship.Enemy)
						.ClosestToIgnoringPath(world.Map.CenterOfCell(crawlDefenseCenter));

					targetCell = closestEnemy != null ? closestEnemy.Location : baseCenter;

					// Defense-style placement (defense center, defense radii, toward the enemy):
					// gets the tighter defense gap like BuildingType.Defense.
					return findPos(actorType, distanceToBaseIsImportant, producer, crawlDefenseCenter, targetCell, baseBuilder.Info.MinimumDefenseRadius, baseBuilder.Info.MaximumDefenseRadius,
						defenseGap: true);

				case BuildingType.Building:
					return findPos(actorType, distanceToBaseIsImportant, producer, baseCenter, baseCenter, baseBuilder.Info.MinBaseRadius,
						distanceToBaseIsImportant ? baseBuilder.Info.MaxBaseRadius : world.Map.Grid.MaximumTileSearchRange);
			}

			// Can't find a build location
			return (null, null, 0);
		}

		void SetBuildingInterval(string name)
		{
			if (baseBuilder.Info.BuildingIntervals == null || !baseBuilder.Info.BuildingIntervals.ContainsKey(name))
				return;

			activeBuildingIntervals[name] = KnobTicks(baseBuilder.Info.BuildingIntervals[name] * buildingIntervalModifier / 100, name);
		}
	}
}
