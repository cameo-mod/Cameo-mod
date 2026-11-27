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
using OpenRA.Mods.CA;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Radar;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	partial class BaseBuilderQueueManagerCA
	{
		public readonly string Category;
		public BotCooldown WaitCooldown;
		public bool WaitCooldownStarted { get; private set; }

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

		// Cash-starvation escape: per producer, the tick its wanted item first sat under the cash floor.
		// The run ends the moment the floor stops blocking, the producer is busy/held/latched/relocating,
		// nothing is wanted, or the producer leaves the live set, so the escape only fires for an
		// uninterrupted run of floor-blocked sweeps (CashStarvationClock). Integer ticks, no RNG.
		readonly CashStarvationClock cashClock = new CashStarvationClock();
		public int TrackedCashStarvationRuns => cashClock.Tracked;
		public List<MiniYamlNode> SaveCashStarvationRuns() => cashClock.SaveState();
		public void RestoreCashStarvationRuns(IEnumerable<MiniYamlNode> runs) => cashClock.RestoreState(runs);
		readonly HashSet<uint> sweptProducers = new HashSet<uint>();
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

		// PROD-BUSY-01 (Sonnet MED): cost of the starts this sweep already ordered. Those orders are not paid yet, so with
		// pb_all_building_producers on the next producer sees the bank net of them (the cash floor and the cost check hold).
		int orderedCostThisSweep;

		// An empty tolerance list in yaml would make ImmutableArray.Random throw.
		int RandomTolerance(ImmutableArray<int> values)
		{
			return values.IsDefaultOrEmpty ? 0 : values.Random(BotRng.For(player, nameof(BaseBuilderQueueManagerCA)));
		}
		bool limitBuildRadius = false;

		WaterCheck waterState = WaterCheck.NotChecked;
		readonly Dictionary<string, int> activeBuildingIntervals = new Dictionary<string, int>();

		BotLimits botLimits;
		int productionTypeLimit = 0;
		int buildingDelayModifier = 100;
		int buildingIntervalModifier = 100;

		// PUSH-1 (bank overflow): the conditioned policy arming the overflow rung — profiles that
		// never carry the grant resolve null here and run the legacy ladder bit-identically.
		BotOverflowPolicy overflowPolicy;
		AblationLatch ablateOverflow; // ABLATE-01 hook pi_overflow (DEV provider only; release = on)
		AblationLatch ablateTechChain; // ABLATE-01 hook techchain_exempt (DEV provider only; release = on)

		// Cameo (§12.20): lazy — resolved on first TickQueue, an OR-of-vetoes pause on new building
		// production. Empty = nothing pauses, upstream-identical.
		IBotRequestPauseBuildingProduction[] pauseBuilding;

		// TF-2: lazy — the unit side's mobile-production reservation. Null = classic (floor 0,
		// no spend feed, admission unchanged).
		IBotMobileSpendReserve mobileReserve;

		// RATIO-2X-01: lazy — the situation owner's mobile army value and the defence classification, and the ablation latch of
		// the increment row. Null provider (classic, no MasterAiBotModule) = the ratio is inert. The live+queued static value is
		// cached per world tick (the recorded window is added fresh on every read).
		IBotMobileStaticValues staticValues;
		bool staticValuesResolved; // a missing provider is cached too: classic runs the rule inert without a lookup per call
		AblationLatch ablateRatio;

		// PROD-BUSY-01 rows (DEV provider only; release = on).
		AblationLatch ablateAllProducers, ablateIdleSweep, ablateEssentials, ablateCycle, ablateSustained, ablateSafety, ablateUnlistedOnce, ablateEssentialPass;
		bool EssentialPassOn => baseBuilder.EssentialPassArmed && !ablateEssentialPass.Off(player, AblationIds.Af2EssentialPass);
		AblationLatch ablateOpeningStep, ablateSustainedKeeps, ablatePrefilter, ablateDefenceNeed, ablatePowerCap, ablateDefenceFirstCopy, ablateFloorExempt;
		AblationLatch ablateReadyWatchdog, ablateNoSpaceBan, ablateSafetyAll; // BASE-FIX-02
		bool ReadyWatchdogOn => baseBuilder.ReadyWatchdogArmed && !ablateReadyWatchdog.Off(player, AblationIds.ReadyWatchdogAll);
		bool NoSpaceBanOn => baseBuilder.NoSpaceBanArmed && !ablateNoSpaceBan.Off(player, AblationIds.NoSpaceBan);
		bool SafetyAllOn => baseBuilder.RefinerySafetyAllClaimsArmed && !ablateSafetyAll.Off(player, AblationIds.RefinerySafetyAllClaims);

		// BASE-06: how long the READY item at the head of a queue has been held (per queue actor, restarts when the item changes or the hold was not seen for 300 ticks).
		readonly Dictionary<uint, (string Item, int Since, int Last)> readyHoldSince = new();
		// BASE-07: type -> (ban end tick, base providers when it was set).
		readonly Dictionary<string, (int Until, int Providers)> noSpaceBan = new();

		int ReadyHeldTicks(ProductionQueue queue, string item)
		{
			var now = world.WorldTick;
			var known = readyHoldSince.TryGetValue(queue.Actor.ActorID, out var e) && e.Item == item;
			var (since, last) = BaseFix02CA.TouchHold(known, known ? e.Since : 0, known ? e.Last : 0, now, 300);
			readyHoldSince[queue.Actor.ActorID] = (item, since, last);
			return now - since;
		}

		bool NoSpaceBanned(string name)
		{
			var banned =
				NoSpaceBanOn && noSpaceBan.TryGetValue(name, out var ban)
				&& BaseFix02CA.BanActive(true, world.WorldTick, ban.Until, world.ActorsHavingTrait<BaseProvider>().Count(a => a.Owner == player), ban.Providers);
			if (banned)
				TechReject(name, "nospace");

			return banned;
		}
		bool DefenceFirstCopyOn => baseBuilder.DefenceFirstCopyArmed && !ablateDefenceFirstCopy.Off(player, AblationIds.Af2DefenceFirstCopy);
		bool FloorExemptOn => baseBuilder.FloorEssentialsExemptArmed && !ablateFloorExempt.Off(player, AblationIds.FloorEssentialsExempt);
		bool OpeningStepOn => baseBuilder.OpeningStepArmed && !ablateOpeningStep.Off(player, AblationIds.OpeningStandardOrder);
		bool SustainedKeepsOn => baseBuilder.SustainedKeepsEssentialsArmed && !ablateSustainedKeeps.Off(player, AblationIds.SustainedKeepsEssentials);
		bool PrefilterOn => baseBuilder.PrefilterArmed && !ablatePrefilter.Off(player, AblationIds.PrefilterPicks);
		bool DefenceNeedOn => baseBuilder.DefenceNeedArmed && !ablateDefenceNeed.Off(player, AblationIds.Af2DefenceNeedGate);
		AblationLatch ablateTurretSupport; // TEAM-DEFEND-02
		bool TurretSupportOn => baseBuilder.TeamTurretSupportArmed && !ablateTurretSupport.Off(player, AblationIds.TeamTurretSupport);
		AblationLatch ablateAntiRush; // EARLY-DEF-01
		bool AntiRushOn => baseBuilder.AntiRushBaseArmed && !ablateAntiRush.Off(player, AblationIds.AntiRushBase);
		bool PowerCapOn => baseBuilder.PowerMarginCapArmed && !ablatePowerCap.Off(player, AblationIds.PowerMarginCap);
		bool UnlistedOnceOn => baseBuilder.UnlistedOnceArmed && !ablateUnlistedOnce.Off(player, AblationIds.Af2UnlistedOnce);
		AblationLatch ablateRefineryEconomyGate;
		bool RefineryEconomyGateOn => baseBuilder.RefineryEconomyGateArmed && !ablateRefineryEconomyGate.Off(player, AblationIds.RefineryEconomyGate);

		// NO-CANCEL-01 (row bb_no_cancel, designs _17/_17B): the row and its ablation latch live on the base builder (one latch for the demand unwind and
		// these sites). Every removal site below keeps its existing condition and adds NoCancelGatesCA.MayCancel with &&, so the row can only withhold a
		// removal; row off = today byte for byte.
		bool NoCancelRowOn => baseBuilder.NoCancelArmed;
		readonly Dictionary<string, bool> unlistedKinds = new();
		string lastCycledProduction;

		// SUSTAINED-FIX-01: the item the SUSTAINED-CASH rung returned in this ChooseBuildingToBuild call (null otherwise). TickQueue rides it at its own cost
		// floor and in the essential-building class, like a planner want: the bank is floating, so the army/building share vote must not hold it.
		string sustainedPickName;
		bool AllProducersOn => baseBuilder.AllBuildingProducersArmed && !ablateAllProducers.Off(player, AblationIds.ProdBusyAllBuildingProducers);
		bool SustainedActive => baseBuilder.SustainedCashArmed && !ablateSustained.Off(player, AblationIds.SustainedCashProduction)
			&& !baseBuilder.Info.DefenseQueues.Contains(Category)
			&& baseBuilder.SustainedCash.Active(world.WorldTick, baseBuilder.Info.SustainedCashTicks);
		bool SafetyOn => baseBuilder.ExpansionSafetyArmed && !ablateSafety.Off(player, AblationIds.ExpansionRefinerySafety);
		bool CycleOn => baseBuilder.CycleProductionArmed && !ablateCycle.Off(player, AblationIds.CycleProduction);
		bool EssentialsOn => baseBuilder.EssentialsLowCashArmed && !ablateEssentials.Off(player, AblationIds.EssentialsLowCash);
		bool IdleSweepOn => baseBuilder.BuildingIdleSweepArmed && !ablateIdleSweep.Off(player, AblationIds.ProdBusyBuildingIdleSweep);
		int staticLiveTick = -1;
		long staticLiveValue;

		// REPLAY-HEALTH-LOGGER (ACK-2, task 01a12023): record-only queue-transition dispatch.
		// Everything here is lazy — with no observer trait on the PlayerActor the seam is one
		// null check per Tick and no state is ever allocated (Sol review F5).
		IBotBuildQueueObserver[] queueObservers;
		BotQueueEpisodeTracker queueEpisodes;
		Dictionary<ProductionQueue, BotQueueWatch> queueWatches;
		HashSet<ProductionItem> queueProbeScratch;

		// S2 F3: when another manager of the same category exists (ai.yaml lists BuildingAddons in both
		// BuildingQueues and DefenseQueues), the first-created one owns the observation state; this
		// manager borrows it so each queue item is observed and emitted once. Null = this manager owns
		// its own state. Observation only: every manager still ticks and decides on its own.
		readonly BaseBuilderQueueManagerCA observationOwner;

		public BaseBuilderQueueManagerCA(BaseBuilderBotModuleCA baseBuilder, string category, Player p, PowerManager pm,
			PlayerResources pr, IResourceLayer rl, BaseBuilderQueueManagerCA observationOwner = null)
		{
			this.observationOwner = observationOwner;
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

		public void SetOverflowPolicy(BotOverflowPolicy policy)
		{
			overflowPolicy = policy;
		}

		// REPLAY-HEALTH-LOGGER (ACK-2, Sol review F1/F3/F5): a registered observer turns the
		// seam on — observers resolve once, the tracker and per-queue watches allocate lazily.
		bool ObserveQueueTransitions()
		{
			if (observationOwner != null)
			{
				if (!observationOwner.ObserveQueueTransitions())
					return false;

				queueObservers = observationOwner.queueObservers;
				queueEpisodes = observationOwner.queueEpisodes;
				queueWatches = observationOwner.queueWatches;
				queueProbeScratch = observationOwner.queueProbeScratch;
				return true;
			}

			queueObservers ??= player.PlayerActor.TraitsImplementing<IBotBuildQueueObserver>().ToArray();
			if (queueObservers.Length == 0)
				return false;

			queueEpisodes ??= new BotQueueEpisodeTracker();
			queueWatches ??= new Dictionary<ProductionQueue, BotQueueWatch>();
			queueProbeScratch ??= new HashSet<ProductionItem>();
			return true;
		}

		// Every-tick item-list reconciliation (F2/F3/F4): runs before the latch and queue-sweep cooldown
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
				watch.GoneSince.Clear();
				watch.EvictedAtFirstGone.Clear();
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
				// request's intent on its Removed record, and before the proof scans so
				// an expired request can never claim a late signature. Armed entries are
				// not dropped silently — they resolve through the proof scan below.
				watch.Pending.RemoveAll(p => p.Stale(tick) && !p.AwaitingProof);

				// The only provable cancellation: a cancel request on an Infinite item sees
				// item.Infinite flipped to false while the item STAYS queued — the unique
				// signature of CancelProductionInner (EndProduction removals are
				// indistinguishable from engine cleanup). The item keeps its episode.
				List<BotQueuePendingTerminal> proven = null;
				foreach (var p in watch.Pending)
					if (p.Item is ProductionItem pi && queueProbeScratch.Contains(pi) && p.CancelProvenByFlagFlip())
						(proven ??= new List<BotQueuePendingTerminal>()).Add(p);
				if (proven != null)
					foreach (var p in proven)
					{
						watch.Pending.Remove(p);
						EmitQueueTransition(queue, queue.Actor, p.Item, p.ItemName, QueueCategoryOf(p.ItemName),
							BotQueueTransitionKind.Cancelled, p.Reason, p.Class, queueEpisodes.EpisodeOf(p.Item));
					}

				// Armed placement requests awaiting actor proof: the actor landing on the
				// ordered cell resolves Placed; expiry or elimination resolves Removed
				// carrying the intent reason (schema-2 UNKNOWN).
				List<(BotQueuePendingTerminal Pending, BotQueuePendingResolution Outcome)> outcomes = null;
				var live = !queue.Actor.IsDead && !queue.Actor.Disposed;
				foreach (var p in watch.Pending)
				{
					if (!p.AwaitingProof)
						continue;

					var res = p.ResolveWhileAwaiting(tick, PlacementProof(p), player.WinState != WinState.Undefined);
					if (res != BotQueuePendingResolution.Unresolved)
						(outcomes ??= new List<(BotQueuePendingTerminal, BotQueuePendingResolution)>()).Add((p, res));
				}

				if (outcomes != null)
					foreach (var (p, res) in outcomes)
					{
						watch.Pending.Remove(p);
						EmitTerminalOutcome(queue, p.Item, p.ItemName, p, res == BotQueuePendingResolution.EmitRequested, live, queueSeen: true);
					}

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

			// Stale watches: the queue left FindQueues (disabled or destroyed producer). S2 F2: an item
			// still in the queue is unobserved, not emptied: nothing resolves and the watch is kept.
			// An item really gone while the producer reads live and owned is held for the bounded
			// StaleResolveGraceTicks (sell and transform clear the queue while the actor still reads
			// live; the disposal that follows turns the removal into a proven Destruction). A dead or
			// disposed producer, a captured one and an eliminated seat resolve on this tick as before.
			List<ProductionQueue> dead = null;
			foreach (var kv in queueWatches)
			{
				var watch = kv.Value;
				if (watch.SeenTick == tick)
					continue;

				var staleLive = !kv.Key.Actor.IsDead && !kv.Key.Actor.Disposed;
				var eliminated = player.WinState != WinState.Undefined;

				if (watch.Items.Count > 0)
				{
					var staleOwned = kv.Key.Actor.Owner == player;
					queueProbeScratch.Clear();
					if (staleLive && staleOwned && !eliminated)
						foreach (var it in kv.Key.AllQueued())
							queueProbeScratch.Add(it);

					var resolve = watch.StaleSweep(o => queueProbeScratch.Contains((ProductionItem)o),
						eliminated, staleLive, staleOwned, tick,
						o => ConsumeUnbuildableEviction(kv.Key, (ProductionItem)o, queueEpisodes.EpisodeOf(o)),
							o => ProducerSetDisabled(kv.Key));
					if (resolve != null)
						foreach (var old in resolve)
						{
							ResolveRemovedItem(kv.Key, watch, (ProductionItem)old, queueSeen: false);
							watch.Items.Remove(old);
							watch.GoneSince.Remove(old);
							watch.EvictedAtFirstGone.Remove(old);
							watch.QueueDisabledAtFirstGone.Remove(old);
						}
				}

				// Requests left over after the item flush: armed proof entries get one
				// last actor check (a builder unit's BuildOnSite survives its producer's
				// death), everything else resolves Removed carrying its intent reason so
				// a registered request's fate is never silently cleared. Only once no item
				// is still held or kept: a request bound to such an item resolves with it.
				if (watch.Items.Count == 0)
				{
					foreach (var p in watch.Pending)
					{
						var res = p.AwaitingProof && p.ResolveWhileAwaiting(tick, PlacementProof(p), eliminated) == BotQueuePendingResolution.EmitRequested;
						EmitTerminalOutcome(kv.Key, p.Item, p.ItemName, p, res, staleLive, queueSeen: false);
					}

					watch.Pending.Clear();
				}

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

		// The item left its queue. Removal alone NEVER proves a terminal cause (VP 01a121b6):
		// the engine's own cleanup paths remove items too — tech-cap refunds, infinite trims,
		// competing cancels, dead-producer disable — so request+removal is correlation, not
		// proof. A Placed request emits Placed only on actor-lifecycle proof: an own, live
		// actor of the ordered type occupying the ordered cell (or the plug slot occupied).
		// Without proof the request stays armed for the bounded window — a builder unit's
		// BuildOnSite lands late or never — and ends Removed + intent reason. A cancel
		// request can never be proven by disappearance (cleanup is indistinguishable), so it
		// always resolves Removed + intent reason. The Removed's cause class is resolved
		// event-locally at the emit tick (Elimination/Destruction/Captured are proven;
		// Unbuildable is a diagnostic predicate serialized on the row but never
		// evidence-admitted — Sol adjudication 2026-10-14; see ResolveRemovalCause); a
		// live-queue removal with no proven cause latches UNKNOWN, schema-2 non-pass.
		void ResolveRemovedItem(ProductionQueue queue, BotQueueWatch watch, ProductionItem oldItem, bool queueSeen)
		{
			var pending = FindPending(watch, oldItem);
			var live = !queue.Actor.IsDead && !queue.Actor.Disposed;

			// RC01-OUTER01 (E1): the engine's eviction receipt is bound and consumed at the FIRST
			// removal detection, keyed by the exact ProductionItem reference and the episode taken
			// before the episode ends. The receipt expires 64 ticks after the eviction, and a Placed
			// request can await actor proof far longer, so every later emit reads the stored result
			// (E2) and never asks the engine again.
			var episode = queueEpisodes.EpisodeOf(oldItem);
			// S2-RECEIPT-01: a stale item's receipt was already bound at its first-gone sweep (the grace window
			// outlasts the 64-tick receipt); a first detection on the live path still asks the engine here.
			var evicted = watch.EvictedAtFirstGone.Remove(oldItem) || ConsumeUnbuildableEviction(queue, oldItem, episode);
			// S2-RECEIPT-02: the ClearQueue flag is bound at first-gone on the stale path; a live first detection reads it now.
			var producerDisabled = watch.QueueDisabledAtFirstGone.Remove(oldItem) || (queueSeen && ProducerSetDisabled(queue));

			if (pending != null)
			{
				pending.UnbuildableEvicted = evicted;
				pending.ProducerDisabled = producerDisabled;
				var resolution = pending.ResolveOnRemoval(world.WorldTick, queueSeen, PlacementProof(pending));
				if (resolution == BotQueuePendingResolution.KeepAwaitingProof)
				{
					// The item is gone but the actor may still land — the request and its
					// episode stay armed for the bounded proof window, which restarts now.
					pending.AwaitingProof = true;
					pending.IssuedTick = world.WorldTick;
					return;
				}

				watch.Pending.Remove(pending);
				EmitTerminalOutcome(queue, oldItem, pending.ItemName, pending,
					resolution == BotQueuePendingResolution.EmitRequested, live, queueSeen);
			}
			else
			{
				queueEpisodes.EndEpisode(oldItem);
				EmitQueueTransition(queue, queue.Actor, oldItem, oldItem.Item, QueueCategoryOf(oldItem.Item),
					BotQueueTransitionKind.Removed, BotQueueTransitionReason.None,
					ResolveRemovalCause(queue, oldItem.Item, evicted, live, queueSeen, producerDisabled), episode);
			}
		}

		// RC01-OUTER01 (E1): bind, then consume, the engine's eviction receipt for this exact item
		// and episode. True only for a receipt the engine recorded for this ProductionItem; a
		// name-only match, a zero episode or an owner mismatch fails closed (false).
		bool ConsumeUnbuildableEviction(ProductionQueue queue, ProductionItem item, uint episode)
		{
			if (item == null || episode == 0)
				return false;

			var tick = world.WorldTick;
			return queue.TryBindUnbuildableEviction(item, player, episode, tick)
				&& queue.TryConsumeUnbuildableEviction(item, player, episode, tick);
		}

		// Terminal emit for a resolved request: proof emits the requested kind (Placed); the
		// unproven path emits Removed carrying the request's reason so the intent stays on
		// record, classified event-locally — never the request's class, which would assert
		// the unproven cause.
		void EmitTerminalOutcome(ProductionQueue queue, object itemRef, string itemName, BotQueuePendingTerminal pending,
			bool proven, bool live, bool queueSeen)
		{
			var episode = queueEpisodes.EpisodeOf(itemRef);
			queueEpisodes.EndEpisode(itemRef);
			// E4: a proven placement retires any eviction receipt still held for this exact item,
			// so a stale receipt cannot attach to a later same-name item.
			if (proven && itemRef is ProductionItem provenItem)
				queue.RetireUnbuildableEviction(provenItem, world.WorldTick);

			// E2: the receipt result was bound at first detection; the emit path never asks the engine.
			var cls = proven ? BotQueueCancellationClass.None
				: ResolveRemovalCause(queue, itemName, pending.UnbuildableEvicted, live, queueSeen, pending.ProducerDisabled);
			EmitQueueTransition(queue, queue.Actor, itemRef, itemName, QueueCategoryOf(itemName),
				proven ? pending.Kind : BotQueueTransitionKind.Removed, pending.Reason, cls, episode);
		}

		// QUEUE-REMOVAL-DISCRIMINATOR (01a13686): event-local removal cause — resolved only
		// from observable state at the emit tick, never from intent or correlation. The
		// buildable probe still surfaces the engine's own eviction predicate for
		// diagnosis — CancelUnbuildableItems diffs BuildableItems names — but Sol's
		// 2026-10-14 adjudication rules it NON-PROBATIVE: the routine is cached-set
		// gated and prefers migration, so an uninstrumented removal plus prerequisite
		// loss reads identically. A receipt-backed eviction emits EvictedUnbuildable (E1/E6); a bare
		// Unbuildable emits as a diagnostic class only; the
		// evidence seam latches it UNKNOWN like None. Proven mechanism causes
		// (unconditional ClearQueue paths) observe; every other live-queue removal
		// stays None = UNKNOWN.
		BotQueueCancellationClass ResolveRemovalCause(ProductionQueue queue, string itemName, bool evicted, bool live, bool queueSeen,
			bool producerDisabled)
		{
			var owned = queue.Actor.Owner == player;
			var buildable = queueSeen && live && owned
				&& queue.BuildableItems().Any(b => b.Name == itemName);
			return BaseBuilderQueueEvalCA.RemovalCauseClass(
				player.WinState != WinState.Undefined, live, owned, queueSeen, buildable, evicted, producerDisabled);
		}

		// S2-RECEIPT-02: the exact ClearQueue condition (ProductionQueue.Tick): no production trait of this queue type is
		// enabled. The engine then refunds and clears every item and wipes the eviction receipts in the same call.
		static bool ProducerSetDisabled(ProductionQueue queue) =>
			!queue.Actor.TraitsImplementing<Production>().Any(p => p.Info.Produces.Contains(queue.Info.Type) && !p.IsTraitDisabled);

		// Actor-lifecycle proof for a Placed request — the lead's "building actor added to
		// the world" criterion: an own, live actor of the ordered type occupying the ordered
		// cell. Normal placement lands inside the same frame-end task as the removal, so it
		// is already provable at the next probe tick; a builder unit lands whenever
		// BuildOnSite completes (or never, on failure). A plug order creates no actor — its
		// proof is the host slot's installed plug state, read on the host captured at
		// order time (live and owned): a requirement-free slot refusing the type it
		// accepted is positive evidence (EnablePlug is the sole writer of active); a
		// requirement-keyed slot refusing a type its host accepted AT ORDER TIME is too,
		// because the install is what makes the requirement false (S2 F1). An unknown-type
		// slot, a host that did not accept at order time, or a dead or disowned host stays
		// UNKNOWN and expires Removed.
		bool PlacementProof(BotQueuePendingTerminal p)
		{
			if (p.Kind != BotQueueTransitionKind.Placed || p.Site == null)
				return false;

			var site = p.Site.Value;
			if (p.PlugType != null)
			{
				// S2 F1: evaluated on the host captured at order time, and only while that host is
				// present, live and owned. The slot at the ordered site is the one the order named.
				var host = p.PlugHostId != 0 ? world.GetActorById(p.PlugHostId) : null;
				if (!BaseBuilderQueueEvalCA.PlugHostEligible(host != null,
					host != null && !host.IsDead && !host.Disposed, host != null && host.Owner == player))
					return false;

				return host.TraitsImplementing<Pluggable>().Any(t => host.Location + t.Info.Offset == site
					&& p.PlugProven(true,
						t.Info.Conditions.ContainsKey(p.PlugType),
						t.Info.Requirements.ContainsKey(p.PlugType),
						t.AcceptsPlug(p.PlugType)));
			}

			// Occupied-cell proof first: the actor lands inside the same frame-end task
			// the item leaves in, so the building influencing the ordered cell proves
			// the placement. PLACEMENT-PROOF-LOCATION-FIX (01a131ba): buildings whose
			// footprint marks the top-left '_' (every ^TDPROC refinery,
			// td_gdi_repairfacility) never influence their own Location cell — Tiles()
			// skips '_' — so the site scan can never see them. Their Location still IS
			// the ordered site; a live own name-matching actor at that Location proves
			// the placement without weakening the cell-occupancy evidence.
			if (world.ActorMap.GetActorsAt(site).Any(a => BaseBuilderQueueEvalCA.OccupiedSiteProven(
				!a.IsDead, a.Owner == player, PlacedNameMatches(p.ItemName, a))))
				return true;

			return world.ActorsHavingTrait<Building>().Any(a => BaseBuilderQueueEvalCA.LocationSiteProven(
				!a.IsDead, a.Owner == player, PlacedNameMatches(p.ItemName, a),
				a.Location == site));
		}

		bool PlacedNameMatches(string itemName, Actor placed)
		{
			if (placed.Info.Name == itemName)
				return true;

			// PlaceBuildingVariants: the order may carry a variant override — the created
			// actor's name differs from the queue item name but is a registered variant.
			var info = world.Map.Rules.Actors[itemName].TraitInfoOrDefault<PlaceBuildingVariantsInfo>();
			return info?.Actors != null && info.Actors.Contains(placed.Info.Name);
		}

		static BotQueuePendingTerminal FindPending(BotQueueWatch watch, object item)
		{
			var name = (item as ProductionItem)?.Item;
			foreach (var p in watch.Pending)
				if (p.Matches(item, name))
					return p;

			return null;
		}

		// A manager-issued terminal is a REQUEST — the intent record emits now, at the
		// decision tick, and the outcome resolves separately on removal/proof (VP 01a121b6:
		// request intent and proven outcome are distinct records, never fused). Last
		// decision wins: a later request for the same item supersedes the earlier pending.
		void NotePendingTerminal(ProductionQueue queue, ProductionItem itemRef, string itemName,
			BotQueueTransitionKind kind, BotQueueTransitionReason reason, BotQueueCancellationClass cls,
			CPos? site = null, string plugType = null, uint plugHostId = 0, bool plugAcceptedAtOrder = false)
		{
			if (queueWatches == null || !queueWatches.TryGetValue(queue, out var watch))
				return;

			watch.Pending.RemoveAll(p => ReferenceEquals(p.Item, itemRef) || (p.Item == null && p.ItemName == itemName));
			watch.Pending.Add(new BotQueuePendingTerminal
			{
				Item = itemRef, ItemName = itemName, Kind = kind, Reason = reason, Class = cls,
				IssuedTick = world.WorldTick, Site = site, PlugType = plugType,
				PlugHostId = plugHostId, PlugAcceptedAtOrder = plugAcceptedAtOrder,
				WasInfinite = (itemRef as ProductionItem)?.Infinite ?? false
			});

			EmitQueueTransition(queue, queue.Actor, itemRef, itemName, QueueCategoryOf(itemName),
				kind == BotQueueTransitionKind.Placed ? BotQueueTransitionKind.PlacementOrdered : BotQueueTransitionKind.CancelOrdered,
				reason, cls, queueEpisodes.EpisodeOf(itemRef));
		}

		/// <summary>REPLAY-HEALTH-LOGGER (F4): a demand-path cancel issued outside the manager
		/// (BaseBuilderBotModuleCA.CancelDemandItem) registers its intent here — the record is
		/// a non-terminal CancelOrdered; the bound item's later removal resolves Removed with
		/// the DemandCancel reason (disappearance alone never proves a cancel).</summary>
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
					IssuedTick = world.WorldTick, WasInfinite = target.Infinite
				});

				EmitQueueTransition(kv.Key, producer, target, item, QueueCategoryOf(item),
					BotQueueTransitionKind.CancelOrdered, BotQueueTransitionReason.DemandCancel,
					BotQueueCancellationClass.Production, queueEpisodes.EpisodeOf(target));
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

		// Sonnet adjudication of 666ddd0bd (default-off defect): the protection regime —
		// and therefore the certificate requirement — exists ONLY under the active refinery
		// law AND only for the protected (refinery) class. With no law provider, or for any
		// non-member item, the automatic cancel paths run exactly like master.
		bool ProtectionApplies(string itemName) =>
			BaseBuilderQueueEvalCA.PlacementProtectionApplies(
				baseBuilder.RefineryLawProvider() != null,
				BaseBuilderQueueEvalCA.RetainedHighValueItem(baseBuilder.Info.RefineryTypes, itemName));

		// 6722A484/BF5B8247(c)/A966ED70(E3): a protected cancel proceeds only when the live
		// queue state proves the order will settle the caller-intended instance AND the
		// certificate re-verifies against the live world at the cancel point. The order
		// dispatches to EVERY enabled queue on the producer and removes the LAST same-name
		// on each, so intent requires a unique same-name entry across enabled queues whose
		// live resolution is `intended` — a head-A/tail-B pair or enabled sibling-queue
		// entry holds without settlement.
		bool CertificateAuthorizes(ProductionQueue queue, string itemName, ProductionItem intended)
		{
			var enabledSameName = queue.Actor.TraitsImplementing<ProductionQueue>()
				.Where(q => q.Enabled)
				.Sum(q => q.AllQueued().Count(i => i.Item == itemName));
			var resolved = queue.AllQueued().LastOrDefault(i => i.Item == itemName);
			if (!BaseBuilderQueueEvalCA.CancelOrderTargetsIntended(enabledSameName, ReferenceEquals(resolved, intended)))
				return false;

			var law = baseBuilder.RefineryLawProvider();
			return law != null && law.RefineryFallbackCertificateCurrent(queue.Actor, itemName);
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
			TechLogCompletions(); // TECH-LOG-01 (DEV only; no-op without the sink)

			// REPLAY-HEALTH-LOGGER (F3): the probe reconciles queue state every tick so
			// transition records carry exact observation ticks — it must run before the
			// latch and queue-sweep cooldown early returns below. No observer = one lazy-resolve check.
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
							var noCancelOn = NoCancelRowOn;
							foreach (var queue in stuckConyard.TraitsImplementing<ProductionQueue>())
							{
								var latchSnapshot = queue.AllQueued().ToArray();
								foreach (var item in latchSnapshot)
								{
									// E91BA74C/D831BBCB/6722A484 + Sonnet 666ddd0bd default-off fix:
									// the mass-cancel consults protection ONLY while the law is
									// active AND the item is a protected (refinery) member — every
									// other item cancels unconditionally exactly like master. A
									// protected item — in-progress or completed alike (E1) — stays
									// queued unless a live exhaustion certificate authorizes the settle.
									if (ProtectionApplies(item.Item) && !CertificateAuthorizes(queue, item.Item, item))
									{
										EmitQueueTransition(queue, stuckConyard, item, item.Item, QueueCategoryOf(item.Item),
											BotQueueTransitionKind.Held, BotQueueTransitionReason.ProtectionHold);
										continue;
									}

									// NO-CANCEL-01 (bb_no_cancel, site 2, design _17 2): the latch tripped because the base keeps offering no cell, which is
									// proof only for an entry that is waiting for one (READY). Row on: an in-progress or merely queued entry is left to finish
									// (a Held record instead of the cancel); the nudge to the expansion modules and RelocationHoldConyard below stay as they are.
									if (!NoCancelGatesCA.MayCancel(noCancelOn, item.Done, true))
									{
										EmitQueueTransition(queue, stuckConyard, item, item.Item, QueueCategoryOf(item.Item),
											BotQueueTransitionKind.Held, BotQueueTransitionReason.RelocationLatch);
										baseBuilder.TraceNoCancel(noCancelOn, "CANCEL-SUPPRESSED", 2, item.Item, queue.Actor);
										continue;
									}

									// The record resolves on the tick the item actually
									// leaves the queue (F2); queues outside this manager's
									// category surface as Removed on their own manager.
									baseBuilder.TraceNoCancel(noCancelOn, "CANCEL-UNPLACEABLE", 2, item.Item, queue.Actor);
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
				{
					cashClock.ResetAll();
					return;
				}
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

			// Only update once per second or so.
			if (!BaseBuilderQueueEvalCA.QueueSweepDue(WaitCooldown, world.WorldTick))
				return;

			playerBuildings = world.ActorsHavingTrait<Building>().Where(a => a.Owner == player).ToArray();
			// CONSOLIDATE-03 P4: the required margin comes from PowerArbiter (growth by building group, BASE-13 cap, then the build-order power_margin knob, 12.25; no provider = unchanged).
			minimumExcessPower = PowerArbiter.Required(baseBuilder.Info.MinimumExcessPower, baseBuilder.Info.MaximumExcessPower, baseBuilder.Info.ExcessPowerIncrement,
				baseBuilder.Info.ExcessPowerIncreaseThreshold, playerBuildings.Count(), PowerCapOn, baseBuilder.Info.PowerMarginCap, BuildOrderPowerMilli());

			// BP-2 (§19.15): the advisor's radar power reserve — one Refresh-priced read per queue tick.
			radarPowerMargin = FrontBackAdvisor()?.RadarPowerMargin ?? 0;

			// PERF: Queue only one actor at a time per category
			itemQueuedThisTick = false;
			orderedCostThisSweep = 0;
			var active = false;
			sweptProducers.Clear();
			foreach (var queue in AIUtils.FindQueues(player, Category))
			{
				sweptProducers.Add(queue.Actor.ActorID);
				if (TickQueue(bot, queue))
					active = true;
			}

			// A producer that left FindQueues (disabled or destroyed) must not keep a stale starvation run.
			cashClock.Retain(sweptProducers);

			// E1 observational slice: drop reservations whose producer died/left/changed owner.
			// Observational only — releases ledger entries, never world state (flag-gated inside).
			baseBuilder.StorageObservationReconcile();

			// TF-2 F2: the sweep-end flush — a producer that left FindQueues (destroyed or
			// disabled) can never reconcile, so its tracked items settle here once instead of
			// holding the floor forever. The live set spans EVERY building+defense category:
			// sibling managers share this ledger, and the flush is idempotent across them.
			// FindQueues is per-tick cached, so the union scan costs one enumeration.
			if (mobileReserve != null)
			{
				var liveOwners = new HashSet<object>();
				foreach (var category in baseBuilder.Info.BuildingQueues.Concat(baseBuilder.Info.DefenseQueues).Distinct())
					foreach (var liveQueue in AIUtils.FindQueues(player, category))
						liveOwners.Add(liveQueue.Actor);
				mobileReserve.FlushBuildingSpendOwners(liveOwners);
			}

			// Add a random factor so not every AI produces at the same tick early in the game.
			// Minimum should not be negative as delays in HackyAI could be zero.
			var randomFactor = BotRng.For(player, nameof(BaseBuilderQueueManagerCA)).Next(0, baseBuilder.Info.StructureProductionRandomBonusDelay);

			var waitTicks = ProdBusyCA.ShortSweepDelay(active, IdleSweepOn) ? baseBuilder.Info.StructureProductionActiveDelay + randomFactor
				: baseBuilder.Info.StructureProductionInactiveDelay + randomFactor;

			// Build-order knobs (12.25): tempo shortens (above 1000) or lengthens the general build interval.
			var knobs = baseBuilder.BuildOrderKnobs;
			if (knobs != null)
				waitTicks = DivideByKnob(waitTicks, knobs.KnobMilli(BuildOrderKnob.Tempo));

			BaseBuilderQueueEvalCA.StartQueueSweepCooldown(ref WaitCooldown, world.WorldTick, waitTicks);
			WaitCooldownStarted = true;
		}

		bool TickQueue(IBot bot, ProductionQueue queue)
		{
			if (queue.Actor == baseBuilder.RelocationHoldConyard)
			{
				cashClock.Reset(queue.Actor.ActorID);
				return false;
			}

			// TF-2: feed the unit side's spend ledger — every in-flight item with total/remaining
			// cost and its army-first flag, so the paid-share metric is real and an item that
			// leaves the queue (produced, refunded, cancelled, producer gone) settles exactly once.
			// F1: a LIST — same-name queued buildings must count per instance.
			mobileReserve ??= player.PlayerActor.TraitsImplementing<IBotMobileSpendReserve>().FirstEnabledTraitOrDefault();
			if (mobileReserve != null)
			{
				var inflight = new List<(string Item, int Total, int Remaining, bool Essential)>();
				foreach (var i in queue.AllQueued())
					// An admitted tech-chain item is queued, so the admission test (not-queued) would book it non-essential. An in-flight
					// tech-chain item stays essential for the ledger: it was admitted as the first copy.
					inflight.Add((i.Item, i.TotalCost, i.RemainingCost,
						IsArmyFirstEssential(world.Map.Rules.Actors[i.Item]) || InTechChainList(i.Item)));
				mobileReserve.ReconcileBuildingSpend(queue.Actor, inflight);
			}

			var currentBuilding = queue.AllQueued().FirstOrDefault();

			// A busy producer (item queued/ready) or a placement-latched one takes no new item this sweep.
			if (currentBuilding != null || failCount >= baseBuilder.Info.MaximumFailedPlacementAttempts)
				cashClock.Reset(queue.Actor.ActorID);

			// Waiting to build something
			if (currentBuilding == null && failCount < baseBuilder.Info.MaximumFailedPlacementAttempts)
			{
				pendingDemandPick = default;

				// CONSOLIDATE-01 c2 (bb_pipeline_01): armed, the staged planner picks and admits (BaseBuilderQueueManagerCA.Pipeline01.cs).
				// Off (and classic): the ladder and vetoes below, unchanged.
				if (PipelineOn)
					return TickQueuePlanned(bot, queue);

				var item = ChooseBuildingToBuild(queue);

				// REF2-FIRST-01 (rows ref2_before_radar, ref2_before_prod2): a NEW radar or 2nd production start waits while ref2 is due (nothing in flight is cancelled).
				if (item != null && Ref2Holds(item, ref2Name => queue.BuildableItems().Any(ref2Item => ref2Item.Name == ref2Name)))
					item = null;

				if (item == null)
				{
					cashClock.Reset(queue.Actor.ActorID);
					return false;
				}

				TraceThresholdShadow(queue, item);

				var demandPick = pendingDemandPick;

				// ECON-A-FIX (R5): producer+name exclusivity — an ordinary pick must never duplicate a
				// demand-bound name on this producer: CancelProduction removes the LAST same-name item,
				// so a duplicate would refund or free the wrong one. The demand's own pick (and an
				// ordinary same-name duplicate of an ORDINARY item, legal before) pass unchanged.
				if (baseBuilder.ExpansionPrebuildEnabled && demandPick.Demand == null
					&& baseBuilder.DemandForQueuedItem(item.Name, queue.Actor) != null)
				{
					cashClock.Reset(queue.Actor.ActorID);
					return false;
				}

				// We shouldn't be queueing new buildings (other than refineries) when we're low on cash.
				// REF-1 B1/B2: under the refinery law the planner's own wants (the crawl link and the due MCV's
				// prerequisite) ride at their own cost floor — a crawl link IS the economy investment, and the
				// reserve-sized gate starved the frontier in the trace.
				var lawWants = baseBuilder.RefineryLawProvider();
				var plannerWant = lawWants != null
					&& (item.Name == lawWants.WantedLinkBuilding || item.Name == lawWants.WantedMcvPrerequisite
						|| item.Name == lawWants.WantedFrontDefence);

				// ECON-A: a demand-bound item rides at its own cost floor like the planner wants —
				// the reserve gate would push it past the ETA it was scheduled to meet.
				// Cash-starvation escape: units draining the bank under the floor every sweep must not
				// hold construction out forever — after BuildingCashStarvationTicks floor-blocked ticks
				// the item rides at its own cost (the same exemption the planner wants get).
				// The clock counts only ELIGIBLE sweeps — the cash floor is the ONLY veto: no sibling queue took
				// this tick and no pause / mobile-reserve veto applies (both are side-effect-free queries). It is
				// keyed by (producer, wanted item), so a different item never inherits elapsed ticks, and every
				// ineligible sweep ends the run.
				pauseBuilding ??= player.PlayerActor.TraitsImplementing<IBotRequestPauseBuildingProduction>().ToArray();
				mobileReserve ??= player.PlayerActor.TraitsImplementing<IBotMobileSpendReserve>().FirstEnabledTraitOrDefault();
				var reserveFloor = mobileReserve?.MobileSpendFloor ?? 0;
				var cashNow = CashLedger.BuildingBank(AllProducersOn, playerResources.GetCashAndResources(), orderedCostThisSweep);
				var isRefinery = baseBuilder.Info.RefineryTypes.Contains(item.Name);
				var itemEssential = IsArmyFirstEssential(item);
				var antiRushPick = antiRushPickName != null && item.Name == antiRushPickName; // EARLY-DEF-01
				// C-07 / BASE-09: an army-first essential rides at its own cost, the building cash floor does not hold it.
				var exempt = plannerWant || demandPick.Demand != null || (sustainedPickName != null && item.Name == sustainedPickName)
					|| (openingPickName != null && item.Name == openingPickName)
					|| antiRushPick
					|| (FloorExemptOn && itemEssential);
				var cost = baseBuilder.ProductionCost(queue, item);

				// PROD-IDLE_PLAN_03 section 2.3 class of this building order: K1/K2 essentials, K4 other
				// essential buildings (extra refineries, planner and demand wants), K6 everything else.
				var buildingClass = itemEssential ? RecoverySpendPolicyCA.Admission.OpeningOrEssential
					: isRefinery || exempt ? RecoverySpendPolicyCA.Admission.EssentialBuilding
					: RecoverySpendPolicyCA.Admission.General;
				var queuedBlocks = ProdBusyCA.QueuedThisSweepBlocks(AllProducersOn, itemQueuedThisTick);
				var nonCashVeto = pauseBuilding.Any(p => p.PausesBuilding(item, itemEssential, buildingClass, cost))
					|| BaseBuilderQueueEvalCA.MobileSpendBlocks(cashNow, cost, reserveFloor, itemEssential || antiRushPick);
				cashClock.Observe(queue.Actor.ActorID, item.Name,
					BaseBuilderQueueEvalCA.StarvationEligible(
						BaseBuilderQueueEvalCA.FloorBlocks(cashNow, minCashRequirement, isRefinery, exempt, cost),
						cashNow >= cost, queuedBlocks, nonCashVeto),
					world.WorldTick);
				var starved = cashClock.Starved(queue.Actor.ActorID, item.Name, world.WorldTick, baseBuilder.Info.BuildingCashStarvationTicks);
				if (BaseBuilderQueueEvalCA.BlockedByCash(cashNow, minCashRequirement, isRefinery, exempt, cost, queuedBlocks, starved))
					return false;

				// Passed the cash gate: an admission (or a later non-cash veto below) ends the run either way.
				cashClock.Reset(queue.Actor.ActorID);

				// ESSENTIALS-01: below the tier MaximiseProductionCash line only essentials start (side-effect free; the pick retries next sweep).
				// The sustained pick is exempt from the share/floor votes but NOT from the low-cash hold: a bank that dipped under the tier line holds it.
				if (EssentialsOn && EssentialsHolds(item, isRefinery, exempt && item.Name != sustainedPickName && item.Name != openingPickName, cashNow))
				{
					pendingDemandPick = default;
					return false;
				}

				// Cameo (§12.20): the army-first vote - a provider (ArmyFirstBotModule, the one owner) can hold new
				// non-essential buildings so cash flows to unit production while it needs to. Construction yards,
				// refineries, power and the first production building are essential and always pass.
				if (pauseBuilding.Length > 0 || reserveFloor > 0)
				{
					var essential = IsArmyFirstEssential(item);
					if (pauseBuilding.Any(p => p.PausesBuilding(item, essential, buildingClass, cost)))
						return false;

					// TF-2: the reservation — optional spend (optional defences, crawl links) may
					// not push cash below the reserved mobile share; essentials keep the old floor.
					if (BaseBuilderQueueEvalCA.MobileSpendBlocks(playerResources.GetCashAndResources(),
							baseBuilder.ProductionCost(queue, item), reserveFloor, essential || antiRushPick))
					{
						pendingDemandPick = default;
						return false;
					}
				}

				// RATIO-2X-01 (ARMY_FIRST_02_DESIGN_04/05): a static-defence start must keep the mobile army at 2x the static defence
				// value, unless it is the first copy of its type (O7) or the bank floats with no other way to spend (FLOAT). A hold is
				// side-effect free (nothing is queued, claimed or cancelled; the pick retries next sweep), so it sits before every claim.
				// Decided before any admission side effect, recorded only at the emitted order below.
				var ratioVerdict = RatioGate(item, out var ratioDefence);
				if (MobileStaticRatioCA.Holds(ratioVerdict))
				{
					pendingDemandPick = default;
					return false;
				}

				// ECON-A-FIX (R6): the demand's refinery reserves its claim's anchor as the order is
				// admitted — taken off every other claim while the item is in flight. A pick whose
				// anchor is no longer reservable (served, parked, pending, or another demand got there)
				// never reaches the queue and retries next sweep.
				// REPAIR-B3 (SPEC §3): the claim's WHOLE covered set binds atomically under the current
				// coverage model version — a contested member or a stale-model set refuses the whole
				// admission, so two queues can never split the set and each admit a duplicate.
				if (demandPick.Demand != null && demandPick.IsRefinery && lawWants != null
					&& !BaseBuilderQueueEvalCA.RefineryReservationAdmits(
						lawWants, demandPick.Claim, demandPick.Demand,
						world.WorldTick + baseBuilder.Info.ExpansionDemandIdleTicks))
				{
					pendingDemandPick = default;
					return false;
				}

				// TF-1: the planner's crawl-link want is claimed atomically at admission — the first
				// queue through holds the pending claim, a second queue in the same window is refused
				// here (the published want alone admits nothing). The claim becomes paid only when the
				// item is seen in this producer's queue; an intent that never arrives drops uncharged.
				if (lawWants != null && item.Name == lawWants.WantedLinkBuilding
					&& !lawWants.TryClaimCrawlLink(queue.Actor, item.Name, baseBuilder.ProductionCost(queue, item)))
				{
					pendingDemandPick = default;
					return false;
				}

				// CRAWL-SAFETY (S3): the front defence is claimed atomically at admission like the
				// crawl link — one defence in flight at the frontier, never two queues at once.
				if (lawWants != null && item.Name == lawWants.WantedFrontDefence
					&& !lawWants.TryClaimFrontDefence(queue.Actor, item.Name, baseBuilder.ProductionCost(queue, item)))
				{
					pendingDemandPick = default;
					return false;
				}

				baseBuilder.RecordOpeningStructureQueued(queue, item);
				baseBuilder.BuildOrderKnobs?.NotifyQueued(item.Name);

				// E1 observational slice (lead-accepted, observational ONLY): for storage-capable
				// items record the commitment and emit the would-be verdict — a refused-looking
				// verdict still emits identically; nothing here gates the order. The reservation
				// binds to the exact order instance via IBotOrderTracking when the bot offers it
				// (Cameo ModularBot); otherwise it stays pending untracked — conservative either
				// way, see StorageAdmissionObservation for the permanent double-count contract.
				var storageReservation = baseBuilder.StorageAdmissionObserving
					? baseBuilder.StorageObservation.ObserveEmitting(world, playerResources, queue, item.Name, cost, baseBuilder.Info)
					: null;
				var produceOrder = Order.StartProduction(queue.Actor, item.Name, 1);
				if (storageReservation == null || !(bot is IBotOrderTracking tracking && tracking.QueueOrderTracked(produceOrder, storageReservation)))
					bot.QueueOrder(produceOrder);

				if (storageReservation != null)
					baseBuilder.StorageObservation.NoteEmitted(storageReservation, world.WorldTick);

				queuedAt[queue.Actor.ActorID] = (item.Name, world.WorldTick);

				// RATIO-2X-01: the admitted defence start is recorded for the next few ticks (the order is not an actor or a queue
				// item yet) and for the first-copy ledger; exempt starts are counted for the DEV log only.
				if (ratioDefence)
				{
					baseBuilder.StaticRatio.Record(world.WorldTick, item.Name, ValuedCost(item));
#if CAMEO_DEV_LOGGING
					if (ratioVerdict == RatioVerdict.ExemptFirst || ratioVerdict == RatioVerdict.ExemptFloat)
						Log.Write("debug", $"AI ({player.ClientIndex}): RATIO-EXEMPT kind {(ratioVerdict == RatioVerdict.ExemptFirst ? "first" : "float")} start {item.Name} tick {world.WorldTick}");
#endif
				}

				// PROD-IDLE PR-3: the admitted building is recorded before the next candidate is tested, so
				// two admissions in one pass cannot both pass an old share snapshot (PLAN_03 section 3).
				foreach (var p in pauseBuilding)
					p.BuildingAdmitted(item, cost);

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
				orderedCostThisSweep += Math.Max(0, cost);

				// PROD-BUSY-01 (Sonnet FIX): with every producer served in one sweep, a start must count as "being produced" at once, so a
				// sibling producer of the same sweep cannot pick the same building (each building once, limits, the 2x ratio first copy).
				if (AllProducersOn)
					baseBuilder.BuildingsBeingProduced[item.Name] = (baseBuilder.BuildingsBeingProduced.TryGetValue(item.Name, out var inFlight) ? inFlight : 0) + 1;
				SetBuildingInterval(item.Name);

				// REPLAY-HEALTH-LOGGER (F2/F3): no emit here — the probe records Started on
				// the tick the engine applies the order, not the request tick.
			}
			else if (currentBuilding != null && currentBuilding.Done)
			{
				// REPLAY-HEALTH-LOGGER (F3): Ready also comes from the probe — the exact tick
				// the head item flips Done, not this cooldown-gated sweep.

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
						// NO-CANCEL-01 (bb_no_cancel, site 3, design _17 2): the building is complete and legal to place, so the limit is a choice, never an
						// unplaceable proof (proof = false). Row on: no cancel, no hold; it falls through to the normal placement below as an ordinary item
						// (a paid-for building is never refunded for a limit; if it then has no cell it ends at the saturation site, class (b)). Opus ruling: this is intended (binding READY rule, the money is spent); a cap overshoot of at most one per item is accepted, caps stop new STARTS, not placements. Row off =
						// the block below byte for byte.
						var noCancelOn = NoCancelRowOn;
						var limitMayCancel = NoCancelGatesCA.MayCancel(noCancelOn, currentBuilding.Done, false);
						AIUtils.BotDebug(limitMayCancel
							? $"{player} has already has enough {currentBuilding.Item}; cancelling production"
							: $"{player} has already has enough {currentBuilding.Item}; placing the completed building (bb_no_cancel)");

						// E91BA74C/D831BBCB + Sonnet 666ddd0bd default-off fix: the limit
						// cancel consults protection ONLY while the law is active AND the
						// item is a protected (refinery) member — every other item cancels
						// unconditionally exactly like master. A protected item needs a live
						// certificate for the settled instance (E3) or it holds and keeps
						// trying to place.
						if (!limitMayCancel)
						{
							baseBuilder.TraceNoCancel(noCancelOn, "LIMIT-OVERRIDE", 3, currentBuilding.Item, queue.Actor);
						}
						else if (!ProtectionApplies(currentBuilding.Item)
							|| CertificateAuthorizes(queue, currentBuilding.Item, currentBuilding))
						{
							// Master-identical binding (A966ED70 E3 + default-off identity):
							// CancelProduction removes the LAST same-name item — record that
							// instance, not the head. Under the cert the resolved last IS
							// currentBuilding (unique enabled same-name); on the unconditional
							// path twins resolve exactly as master.
							var cancelTarget = BaseBuilderQueueEvalCA.CancelBindTarget(
								queue.AllQueued(), i => i.Item, currentBuilding.Item, currentBuilding);
							NotePendingTerminal(queue, cancelTarget, currentBuilding.Item, BotQueueTransitionKind.Cancelled,
								BotQueueTransitionReason.LimitReached, BotQueueCancellationClass.Production);
							bot.QueueOrder(Order.CancelProduction(queue.Actor, currentBuilding.Item, 1));
						}
						else
						{
							EmitQueueTransition(queue, queue.Actor, currentBuilding, currentBuilding.Item, QueueCategoryOf(currentBuilding.Item),
								BotQueueTransitionKind.Held, BotQueueTransitionReason.ProtectionHold);
						}
					}
				}

				// REFSCAN D-5 (sec 2, 3.5): an item a site-search episode owns is placed by the episode from the
				// bot module tick (BaseBuilderBotModuleCA.BotTick), never here — no claim ladder, no park and no
				// fallback poll on its behalf, and nothing on its path waits on the queue-sweep cooldown.
				if (baseBuilder.RefinerySearchOwns(currentBuilding))
					return false;

				// Check if Building is a plug for other Building
				var actorInfo = world.Map.Rules.Actors[currentBuilding.Item];
				var plugInfo = actorInfo.TraitInfoOrDefault<PlugInfo>();
				var valueInfo = actorInfo.TraitInfoOrDefault<ValuedInfo>();
				var distanceToBaseIsImportant = true;
				CPos? advisedDefense = null;
				var refineryDefers = false;
				refineryClaimed = false;
				refineryExhausted = false;
				refineryHoldReason = BotQueueTransitionReason.NoRefinerySite;
				lastFrontBackPick = null;
				frontBackHold = false;
				demandAdvisedDefense = false;
				uint plugHostId = 0;
				var plugAcceptedAtOrder = false;
				if (plugInfo != null)
				{
					// Maintainer ruling (playtest repair): the normal base builder picks a
					// random eligible installation slot — synced BotRng over ActorID-ordered
					// candidates, never first-match and never unsynced LocalRandom.
					var plugCandidates = world.ActorsWithTrait<Pluggable>()
						.Where(a => a.Actor.Owner == player && a.Trait.AcceptsPlug(plugInfo.Type))
						.OrderBy(a => a.Actor.ActorID)
						.ToArray();

					if (plugCandidates.Length > 0)
					{
						var possibleBuilding = plugCandidates[BotRng.For(player).Next(plugCandidates.Length)];
						orderString = "PlacePlug";
						location = possibleBuilding.Actor.Location + possibleBuilding.Trait.Info.Offset;

						// S2 F1 (observer capture only): the chosen host and whether it accepted at order time.
						plugHostId = possibleBuilding.Actor.ActorID;
						plugAcceptedAtOrder = possibleBuilding.Trait.AcceptsPlug(plugInfo.Type);
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

						// CRAWL-SAFETY (S3): the planner's front defence is a Defense placement aimed at
						// the frontier cell — the chance roll and advisor that gate organic defences do
						// not apply to it (no random draw consumed, same as lawLink).
						var lawFrontDefence = law != null && currentBuilding.Item == law.WantedFrontDefence;
						var isRefinery = false;
						var isFragile = false;
						var hasAttackBase = false;
						var defenseRoll = false;
						var organicCrawlRoll = false;
						if (!lawLink && !lawFrontDefence)
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

									defenseRoll = BotRng.For(player, nameof(BaseBuilderQueueManagerCA)).Next(100) < placeDefenseTowardsEnemyChance;
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
									&& BotRng.For(player, nameof(BaseBuilderQueueManagerCA)).Next(100) < baseBuilder.Info.BaseCrawlChance;
							}
						}

						type = lawFrontDefence ? BuildingType.Defense
							: BaseBuilderQueueEvalCA.ClassifyPlacement(lawLink, isRefinery, isFragile, hasAttackBase, defenseRoll, organicCrawlRoll);
					}

					// FIX-RA-REFINERY / G2 (maintainer ruling 2026-10-16): decided here so the
					// null-location branch below can tell a refinery's "retry later" from an
					// ordinary placement failure. A refinery retains WITHOUT the law too — a
					// law-less refinery has no exhaustion proof available, so it may never take
					// the saturation cancel; it re-probes generic placement every sweep instead.
					// (Covers the law claim paths and the classic/demand refinery alike:
					// classification maps RefineryTypes items to BuildingType.Refinery.)
					refineryDefers = type == BuildingType.Refinery;

					// REF-1 B1 (crawl-trace §8): under the law a crawl placement with no aim holds — returning
					// keeps the produced building queued (and spends no failure budget) instead of wasting the
					// link on an un-aimed fallback cell.
					if (BaseBuilderQueueEvalCA.CrawlHold(type, law != null, law?.CrawlTargetEdge != null, baseBuilder.ExpansionTarget() != null))
					{
						// BASE-06: after the watchdog ticks the READY building is placed at a base cell instead of waiting for a crawl aim.
						if (ReadyWatchdogOn && BaseFix02CA.HoldExpired(true, world.WorldTick, world.WorldTick - ReadyHeldTicks(queue, currentBuilding.Item), baseBuilder.Info.ReadyWatchdogTicks))
						{
							AIUtils.BotDebug($"{player} places {currentBuilding.Item} at a base cell: crawl hold watchdog at tick {world.WorldTick}");
							type = BuildingType.Building;
						}
						else
						{
							EmitQueueTransition(queue, queue.Actor, currentBuilding, currentBuilding.Item, type,
								BotQueueTransitionKind.Held, BotQueueTransitionReason.CrawlHold);
							return false;
						}
					}

					// TEAM-DEFEND-02 (tm_ally_turret_support): a turret placed while an own or allied base is attacked goes where it covers the attackers (solo and team alike).
					var (supportCell, supportHold) = ChooseSupportTurretCell(actorInfo, queue.Actor);
					if (supportHold && ReadyWatchdogOn && !BaseFix02CA.HoldExpired(true, world.WorldTick, world.WorldTick - ReadyHeldTicks(queue, currentBuilding.Item), baseBuilder.Info.ReadyWatchdogTicks))
					{
						EmitQueueTransition(queue, queue.Actor, currentBuilding, currentBuilding.Item, type,
							BotQueueTransitionKind.Held, BotQueueTransitionReason.FrontBackHold);
						return false;
					}

					if (supportCell != null)
						location = supportCell;
					else if (advisedDefense != null)
						location = advisedDefense;
					else
					{
						(location, baseCenterKeepsFailing, actorVariant) = ChooseBuildLocation(currentBuilding.Item, distanceToBaseIsImportant, queue.Actor, type, heldDemand);

						// BP-2 (§19.15): the front/back advisor's hold — no legal cell and no fallback
						// (a radar on a front with no defence line waits; it never goes forward). Same
						// semantics as the REF-1 crawl hold above: queued, no failure budget spent.
						if (frontBackHold && !(ReadyWatchdogOn && BaseFix02CA.HoldExpired(true, world.WorldTick, world.WorldTick - ReadyHeldTicks(queue, currentBuilding.Item), baseBuilder.Info.ReadyWatchdogTicks)))
						{
							EmitQueueTransition(queue, queue.Actor, currentBuilding, currentBuilding.Item, type,
								BotQueueTransitionKind.Held, BotQueueTransitionReason.FrontBackHold);
							return false;
						}
					}
				}

				// REFSCAN F2 (sec 3.4): every non-episode PlaceBuilding/PlacePlug passes the reservation mask at
				// this single emission point, before NotifyPlacement. A blocked placement takes the null-location
				// branch for its class (a refinery defers; anything else counts a failure). Empty mask = no-op.
				if (location != null && baseBuilder.PlacementMasked(currentBuilding.Item, location.Value))
				{
					AIUtils.BotDebug($"{player} {currentBuilding.Item} at {location.Value}: reserved by a refinery search at tick {world.WorldTick}");
					location = null;
				}

				if (location == null)
				{
					// FIX-RA-REFINERY: under the law a siteless refinery defers — it must not spend
					// shared failure budget here, or one unreachable field would saturate the latch
					// and starve every other structure indefinitely. REF-2 P1a: the defer is now a
					// hold (the completed item is retained); the old cancel ate anchors and refunds
					// through the re-queue loop while no site existed.
					if (refineryDefers)
					{
						// Maintainer ruling 2026-10-16: cancellation is permitted ONLY on a
						// proven total-domain exhaustion — the tiered fallback's TERMINAL
						// pass enumerated the whole map unconditionally (provider squares
						// serve the bounded tiers only, never the certificate) under the
						// fully-relaxed bound and found no legal cell. Same cancel machinery
						// as a saturation cancel, but never on a budget — only on that proof.
						if (refineryExhausted)
						{
							// BF5B8247(c)/6722A484: the verdict alone is stale evidence — the
							// certificate re-verifies the exact atom sequence against the live
							// world at the cancel point; any drift (or a no-provider scan that
							// could never certify) fails closed to the deferred hold.
							// NO-CANCEL-01 (bb_no_cancel, site 4, class (b)): the certificate is the unplaceable proof; the guard adds the READY read
							// (row on) and can only withhold the cancel, which then ends in the stale-certificate hold below.
							if (CertificateAuthorizes(queue, currentBuilding.Item, currentBuilding)
								&& NoCancelGatesCA.MayCancel(NoCancelRowOn, currentBuilding.Done, true))
							{
								AIUtils.BotDebug($"{player} cancels {currentBuilding.Item}: refinery domain exhausted at tick {world.WorldTick}");
								baseBuilder.TraceNoCancel(NoCancelRowOn, "CANCEL-UNPLACEABLE", 4, currentBuilding.Item, queue.Actor);
								var cancelTarget = BaseBuilderQueueEvalCA.CancelBindTarget(
									queue.AllQueued(), i => i.Item, currentBuilding.Item, currentBuilding);
								NotePendingTerminal(queue, cancelTarget, currentBuilding.Item, BotQueueTransitionKind.Cancelled,
									BotQueueTransitionReason.RefineryDomainExhausted, BotQueueCancellationClass.Production);
								bot.QueueOrder(Order.CancelProduction(queue.Actor, currentBuilding.Item, 1));
								EmitQueueTransition(queue, queue.Actor, currentBuilding, currentBuilding.Item, QueueCategoryOf(currentBuilding.Item),
									BotQueueTransitionKind.Held, BotQueueTransitionReason.RefineryDomainExhausted);
								return false;
							}

							AIUtils.BotDebug($"{player} holds {currentBuilding.Item}: exhaustion certificate stale at tick {world.WorldTick}");
							EmitQueueTransition(queue, queue.Actor, currentBuilding, currentBuilding.Item, QueueCategoryOf(currentBuilding.Item),
								BotQueueTransitionKind.Held, BotQueueTransitionReason.ProtectionHold);
							return false;
						}

						// REF-2 P1a (retained-refinery): a completed refinery is NEVER cancelled
						// for being unplaceable while any tier of the ladder remains — it holds
						// at the head and every later sweep re-probes preferred claims then the
						// tiered widened fallback. The reason distinguishes no-claim-offered,
						// still-scanning and drained-pass holds; EmitGate emits once per reason
						// transition, no failure budget is spent.
						AIUtils.BotDebug($"{player} defers {currentBuilding.Item}: {refineryHoldReason} at tick {world.WorldTick}");
						EmitQueueTransition(queue, queue.Actor, currentBuilding, currentBuilding.Item, type,
							BotQueueTransitionKind.Held, refineryHoldReason);
						return false;
					}

					// If we just reached the maximum fail count, cache the number of current structures
					else if (++failCount >= baseBuilder.Info.MaximumFailedPlacementAttempts)
					{
						AIUtils.BotDebug($"{player} has nowhere to place {currentBuilding.Item}");

						// E91BA74C/D831BBCB + Sonnet 666ddd0bd default-off fix: the
						// saturation cancel consults protection ONLY while the law is active
						// AND the item is a protected (refinery) member — every other item
						// cancels unconditionally exactly like master. A protected item needs
						// a live exhaustion certificate or it holds (the latch snapshot and
						// retry timer still arm, so recovery probes keep running while it waits).
						// NO-CANCEL-01 (bb_no_cancel, site 5, class (b)): the failure budget (this many consecutive full searches without a cell) is the
						// unplaceable proof; the guard adds the READY read (row on) and can only withhold.
						if ((!ProtectionApplies(currentBuilding.Item)
							|| CertificateAuthorizes(queue, currentBuilding.Item, currentBuilding))
							&& NoCancelGatesCA.MayCancel(NoCancelRowOn, currentBuilding.Done, true))
						{
							baseBuilder.TraceNoCancel(NoCancelRowOn, "CANCEL-UNPLACEABLE", 5, currentBuilding.Item, queue.Actor);

							// Master-identical binding: CancelProduction removes the LAST
							// same-name item — record that instance (equal to
							// currentBuilding under the cert; the twin case resolves
							// exactly as master on the unconditional path).
							var cancelTarget = BaseBuilderQueueEvalCA.CancelBindTarget(
								queue.AllQueued(), i => i.Item, currentBuilding.Item, currentBuilding);
							NotePendingTerminal(queue, cancelTarget, currentBuilding.Item, BotQueueTransitionKind.Cancelled,
								BotQueueTransitionReason.SaturationLatch, BotQueueCancellationClass.Production);
							bot.QueueOrder(Order.CancelProduction(queue.Actor, currentBuilding.Item, 1));
							EmitQueueTransition(queue, queue.Actor, currentBuilding, currentBuilding.Item, QueueCategoryOf(currentBuilding.Item),
								BotQueueTransitionKind.Held, BotQueueTransitionReason.SaturationLatch);
						}
						else
						{
							EmitQueueTransition(queue, queue.Actor, currentBuilding, currentBuilding.Item, QueueCategoryOf(currentBuilding.Item),
								BotQueueTransitionKind.Held, BotQueueTransitionReason.ProtectionHold);
						}
						lastFailedBuilding = currentBuilding.Item;

						// BASE-07: this type has nowhere to go; keep it out of the extras until the ban ends or a new base provider appears.
						if (NoSpaceBanOn)
							noSpaceBan[currentBuilding.Item] = (world.WorldTick + baseBuilder.Info.NoSpaceBanTicks, world.ActorsHavingTrait<BaseProvider>().Count(a => a.Owner == player));

						// REF-FIELD-02 c2: a vehicle factory with nowhere to go releases the war-factory wait before ref2 (read only by row rf_economy_gate).
						if (baseBuilder.Info.VehiclesFactoryTypes.Contains(currentBuilding.Item))
							baseBuilder.RefineryGateFactoryPlacementFailed = true;

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
					readyHoldSince.Remove(queue.Actor.ActorID);
					NotifyPlacement(currentBuilding.Item, location.Value, queue.Actor.ActorID, orderString, type, advisedDefense != null || demandAdvisedDefense, lastFrontBackPick);

					// REPLAY-HEALTH-LOGGER (F2): the place order is in flight — the intent
					// emits as PlacementOrdered now; Placed resolves only on actor-lifecycle
					// proof (the building on the ordered cell / the plug slot occupied), so
					// the request carries its site and plug type. A builder unit's
					// BuildOnSite can still land after the item leaves the queue.
					NotePendingTerminal(queue, currentBuilding, currentBuilding.Item, BotQueueTransitionKind.Placed,
						BotQueueTransitionReason.None, BotQueueCancellationClass.None,
						location.Value, orderString == "PlacePlug" ? plugInfo?.Type : null,
						orderString == "PlacePlug" ? plugHostId : 0, orderString == "PlacePlug" && plugAcceptedAtOrder);

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
					NudgeExpansionAfterPlacement(bot, currentBuilding.Item);
					return true;
				}
			}

			return true;
		}

		// After succesfuly placing a building, nudge BaseExpansionModules to expand.
		// We want to avoid expanding too often, so we make a judgement by counting buildings.
		// (REFSCAN D-5: extracted verbatim so the episode emission nudges exactly like the queue sweep.)
		void NudgeExpansionAfterPlacement(IBot bot, string item)
		{
			if (baseBuilder.Info.ProductionTypes.Contains(item)
				|| baseBuilder.Info.FragileTypes.Contains(item) || baseBuilder.Info.RefineryTypes.Contains(item))
			{
				var numRef = baseBuilder.RefineryBuildings.Actors.Count(a => !a.IsDead) + (baseBuilder.Info.RefineryTypes.Contains(item) ? 1 : 0);

				var numProd = baseBuilder.ProductionBuildings.Actors.Count(a => !a.IsDead) + (baseBuilder.Info.ProductionTypes.Contains(item) ? 1 : 0);

				var numTech = playerBuildings.Count(a => baseBuilder.Info.FragileTypes.Contains(a.Info.Name))
					+ (baseBuilder.Info.FragileTypes.Contains(item) ? 1 : 0);

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
		}

		/// <summary>
		/// REFSCAN D-5 (sec 2, 4.2): an episode's placement through this manager's emission — the same order,
		/// the same NotifyPlacement / PlacementOrdered records, the same claim commit (CommitRefineryClaim at the
		/// site for a class-0 claim, RefinerySiteCommitted for a class-1 site) and the same expansion nudge as the
		/// queue sweep's refinery path. Called from the bot module tick, not behind the queue-sweep cooldown.
		/// </summary>
		internal void EmitEpisodePlacement(IBot bot, ProductionQueue queue, ProductionItem item, CPos cell,
			RefineryAnchorClaim? claim, ExpansionDemand demand)
		{
			var requestRef = demand?.Mcv ?? BaseBuilderQueueEvalCA.FirstRequestedRefinery(baseBuilder.RequestedRefineries);
			if (requestRef != null && !baseBuilder.RequestedRefineries.ContainsKey(requestRef))
				requestRef = null;

			var law = baseBuilder.RefineryLawProvider();
			if (law != null)
			{
				// N-2 (587A0D62): the helper returns false only for a null site; an episode always passes a cell.
				if (claim is { } c)
					_ = BaseBuilderQueueEvalCA.CommitRefineryClaimOrPark(law, c, cell, demand);
				else
					law.RefinerySiteCommitted(cell);
			}

			if (requestRef != null)
				baseBuilder.RequestedRefineries.Remove(requestRef);

			refineryClaimed = true;
			failCount = 0;
			NotifyPlacement(item.Item, cell, queue.Actor.ActorID, "PlaceBuilding", BuildingType.Refinery, false, null);
			NotePendingTerminal(queue, item, item.Item, BotQueueTransitionKind.Placed,
				BotQueueTransitionReason.None, BotQueueCancellationClass.None, cell);

			bot.QueueOrder(new Order("PlaceBuilding", player.PlayerActor, Target.FromCell(world, cell), false)
			{
				TargetString = item.Item,
				ExtraLocation = new CPos(0, 0),
				ExtraData = queue.Actor.ActorID,
				SuppressVisualFeedback = true
			});

			// The nudge reads the building list the sweep refreshes; the episode runs outside the sweep.
			playerBuildings = world.ActorsHavingTrait<Building>().Where(a => a.Owner == player).ToArray();
			NudgeExpansionAfterPlacement(bot, item.Item);
		}

		/// <summary>
		/// REFSCAN sec 4.3 / 3.6: the episode's only cancel authority — the _08 exhaustion certificate, re-verified
		/// live with the two-verdict gate, through the same binding and records as the queue sweep's cancel.
		/// </summary>
		internal bool EpisodeCertifiedCancel(IBot bot, ProductionQueue queue, ProductionItem item)
		{
			// NO-CANCEL-01 (bb_no_cancel, site 6, class (b)): the certificate is the proof; the guard adds the READY read (row on).
			if (!CertificateAuthorizes(queue, item.Item, item) || !NoCancelGatesCA.MayCancel(NoCancelRowOn, item.Done, true))
				return false;

			baseBuilder.TraceNoCancel(NoCancelRowOn, "CANCEL-UNPLACEABLE", 6, item.Item, queue.Actor);
			AIUtils.BotDebug($"{player} cancels {item.Item}: refinery search domain exhausted at tick {world.WorldTick}");
			var cancelTarget = BaseBuilderQueueEvalCA.CancelBindTarget(queue.AllQueued(), i => i.Item, item.Item, item);
			NotePendingTerminal(queue, cancelTarget, item.Item, BotQueueTransitionKind.Cancelled,
				BotQueueTransitionReason.RefineryDomainExhausted, BotQueueCancellationClass.Production);
			bot.QueueOrder(Order.CancelProduction(queue.Actor, item.Item, 1));
			EmitQueueTransition(queue, queue.Actor, item, item.Item, QueueCategoryOf(item.Item),
				BotQueueTransitionKind.Held, BotQueueTransitionReason.RefineryDomainExhausted);
			return true;
		}

		// Cameo (12.24 FE-0 / 12.25 BO-0): record-only telemetry. The queue tick per producer and the refinery-claim flag are read by
		// NotifyPlacement only; nothing here feeds a decision.
		readonly Dictionary<uint, (string Item, int Tick)> queuedAt = new();
		bool refineryClaimed;

		// REF-2 P1a (retained-refinery): the reason a refinery holds at the head —
		// NoRefineryClaim (no offer and an empty widened domain), RefineryScanDeferred (the
		// fallback scan is still materializing under its cell budget), NoRefinerySite (a
		// drained pass / failed claims — also the law-less refinery's generic-retry hold),
		// or RefineryDomainExhausted (the proven-exhaustion hold preceding the only
		// permitted cancel). Set inside ChooseBuildLocation's law branch before the
		// null-location defer emits it; reset beside refineryClaimed each placement run.
		BotQueueTransitionReason refineryHoldReason = BotQueueTransitionReason.NoRefinerySite;

		// REF-2 P1a (maintainer ruling 2026-10-16): the fallback pass proved total-domain
		// exhaustion — the unrestricted tier (or an empty domain) enumerated every cell of the
		// declared domain with no legal site. The ONLY flag under which a completed refinery
		// may take the cancel path; reset beside refineryClaimed each placement run.
		bool refineryExhausted;
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

			return available.RandomOrDefault(BotRng.For(player, nameof(BaseBuilderQueueManagerCA)));
		}

		// PUSH-1: production types still under their caps for the overflow rung. The type cap counts
		// standing buildings of the name or its PlaceBuildingVariants plus the name's in-flight
		// production — the fraction path's semantics; the general BuildingLimits rule keeps its own
		// standing/producing count (GetProducibleBuilding's admit test). Each survivor is resolved
		// through PickOrPower BEFORE the draw: an infeasible candidate contributes the power
		// fallback (identical to draw-then-substitute), and a set of only infeasible candidates
		// with no fallback resolves empty — a decline that consumes zero draws (Sol review).
		// CONSOLIDATE-01 c6: the staged planner passes resolvePower = false (its A5 swaps the power plant in itself), which returns the production types as they are.
		List<ActorInfo> OverflowProductionCandidates(IEnumerable<ActorInfo> buildableThings, ActorInfo power, bool resolvePower = true)
		{
			playerBuildings ??= player.PlayerActor.World.Actors
				.Where(a => a.Owner == player.PlayerActor.Owner && !a.Disposed)
				.ToArray();

			var candidates = new List<ActorInfo>();
			foreach (var actor in buildableThings)
			{
				var name = actor.Name;
				if (!baseBuilder.Info.ProductionTypes.Contains(name) || NoSpaceBanned(name))
					continue;

				var producing = baseBuilder.BuildingsBeingProduced.TryGetValue(name, out var beingProduced) ? beingProduced : 0;
				if (!BaseBuilderQueueEvalCA.LimitAdmits(baseBuilder.TryGetBuildingLimit(name, out var limit), limit,
						playerBuildings.Count(a => a.Info.Name == name), producing))
					continue;

				var variants = actor.TraitInfoOrDefault<PlaceBuildingVariantsInfo>()?.Actors ?? [];
				var standingOrVariants = playerBuildings.Count(a => a.Info.Name == name || variants.Contains(a.Info.Name));
				if (!BaseBuilderQueueEvalCA.OverflowTypeAdmits(standingOrVariants, producing, ProductionTypeLimit))
					continue;

				var resolved = resolvePower ? BaseBuilderQueueEvalCA.PickOrPower(actor, power, HasSufficientPowerForActor) : actor;
				if (resolved != null)
					candidates.Add(resolved);
			}

			return candidates;
		}

		// BP-2 (§19.15): a rules-derived radar provider — same two-trait check the Cameo planner runs
		// (the assemblies cannot share the helper; keep the expression identical).
		internal static bool IsRadarProvider(ActorInfo info)
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
			var candidatePower = actorInfo.TraitInfos<PowerInfo>().Where(i => i.EnabledByDefault).Sum(p => p.Amount);
			var excess = PowerArbiter.Effective(playerPower?.ExcessPower ?? 0, baseBuilder.InFlightPower, baseBuilder.PwInflightAdmitArmed);
			var floor = ScaledBaseMinimumExcessPower();
			var powerOk = PowerArbiter.Admits(playerPower != null, candidatePower, excess, floor);
			PowerObserved(PowerArbiter.SiteAdmit, excess, floor, candidatePower, !powerOk);
			if (!powerOk)
				TechReject(actorInfo.Name, "power");

			return powerOk;
		}

		// Build-order knobs (12.25): power_margin scales the configured surplus floor too.
		// BP-2 (§19.15): the front/back advisor's radar reserve rides the same floor — a queued
		// radar's drain is held back so an owned or planned provider is never built blind.
		int ScaledBaseMinimumExcessPower()
		{
			return PowerArbiter.Floor(baseBuilder.Info.MinimumExcessPower, BuildOrderPowerMilli(), radarPowerMargin);
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

				// S2 (FINDINGS_2026-10-12 §4): the opening obeys the same air-intel
				// gate — no dedicated AA before the first fog-fair air sighting.
				if (!BaseBuilderQueueEvalCA.AirDefenceAdmits(baseBuilder.IsGatedAirDefence(name), baseBuilder.DedicatedAntiAirAllowed))
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
		/// BZ_production_scaling: the starved-category production pick. Gate 1 — the bank held over the
		/// difficulty line for the whole persist window AND every unit-production queue busy. Gate 2 —
		/// net income per producer over the difficulty line. Either admits a production building for
		/// the least-provisioned queue type (then the same ProductionTypeLimit and power checks as the
		/// cash-threshold override apply verbatim). Deterministic: no RNG.
		/// </summary>
		ActorInfo ChooseProductionScalingItem(ProductionQueue queue, IEnumerable<ActorInfo> buildableThings, ActorInfo power)
		{
			var types = baseBuilder.ProductionQueueTypes;
			if (types.Count == 0)
				return null;

			var producers = new int[types.Count];
			var busyQueues = 0;
			var totalQueues = 0;
			for (var i = 0; i < types.Count; i++)
			{
				var qs = AIUtils.FindQueues(player, types[i]).ToArray();
				producers[i] = qs.Length;
				totalQueues += qs.Length;
				busyQueues += qs.Count(q => q is IHasParallelQueueSlots p ? p.AvailableSlots <= 0 : q.AllQueued().Any());
			}

			if (!((ProductionScalingCA.CashHeld(baseBuilder.ScalingCashHeldTicks, baseBuilder.ScalingPersistBar)
					&& ProductionScalingCA.AllBusy(busyQueues, totalQueues))
				|| ProductionScalingCA.SpareIncomeMet(baseBuilder.ScalingNetIncomePerTick, totalQueues, baseBuilder.ScalingSpareBar)))
				return null;

			foreach (var i in Enumerable.Range(0, types.Count)
				.Where(i => producers[i] > 0)
				.OrderBy(i => producers[i]))
			{
				var type = types[i];
				var names = new HashSet<string>(baseBuilder.Info.ProductionTypes
					.Where(n => world.Map.Rules.Actors.TryGetValue(n, out var ai)
						&& ai.TraitInfos<ProductionQueueInfo>().Any(q => q.Type == type)));

				// a => 0: first-in-enumeration order — deterministic, unlike the RNG default.
				var production = GetProducibleBuilding(names, buildableThings, a => 0);
				if (production == null)
					continue;

				if (ProductionTypeLimit > 0 && BaseBuilderQueueEvalCA.LimitReached(playerBuildings.Count(a => a.Info.Name == production.Name), ProductionTypeLimit))
					continue;

				var pick = BaseBuilderQueueEvalCA.PickOrPower(production, power, HasSufficientPowerForActor);
				if (pick == null)
					continue;

				AIUtils.BotDebug("{0} decided to build {1}: BZ production scaling ({2})", queue.Actor.Owner, pick.Name,
					pick == production ? $"starved {type}" : $"starved {type} would be low power");
				return pick;
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

		// BINDING tech chain (11-23, TECHCHAIN-EXEMPT-01): the faction's T2 and T3 gate passes the army-first pause and the
		// mobile reserve while none is owned or queued. Ablation row techchain_exempt: off = today's army-first gate.
		// List membership first: off-list names never reach the owned count, which allocates a set per call.
		// TECH-LOG-01 (DEV only): the first completion of each building type for this seat, and the base-builder picks rejected for radar and
		// tech-chain types (one row per type per TechLogCA.RejectThrottleTicks). Rows go through the log sink; release has the null sink.
		readonly HashSet<string> techFirstSeen = new();
		readonly Dictionary<string, int> techRejectNextTick = new();

		ICameoLogSink TechSink => world.WorldActor.TraitOrDefault<CameoLogHub>()?.Sink;
		bool TechLogOn => TechSink?.Enabled == true;

		void TechLogCompletions()
		{
			if (playerBuildings == null || !TechLogOn)
				return;

			foreach (var a in playerBuildings)
				if (!a.IsDead && a.IsInWorld && techFirstSeen.Add(a.Info.Name))
					TechSink.TechLogEvent(Array.IndexOf(world.Players, player), player.Faction?.InternalName ?? "", world.WorldTick, "first_completed", a.Info.Name, "");
		}

		void TechReject(string name, string reason)
		{
			if (!TechLogOn || !(InTechChainList(name) || (world.Map.Rules.Actors.TryGetValue(name, out var info) && IsRadarProvider(info))))
				return;

			var now = world.WorldTick;
			if (!TechLogCA.RejectRowDue(now, techRejectNextTick.GetValueOrDefault(name, int.MinValue)))
				return;

			techRejectNextTick[name] = now + TechLogCA.RejectThrottleTicks;
			TechSink.TechLogEvent(Array.IndexOf(world.Players, player), player.Faction?.InternalName ?? "", now, "rejected", name, reason);
		}

		bool InTechChainList(string name) =>
			baseBuilder.Info.TechChainTypes.Contains(name) && !ablateTechChain.Off(player, AblationIds.TechChainExempt);

		bool TechChainExempt(string name) =>
			InTechChainList(name)
			&& TechChainExemptCA.Exempt(true,
				AIUtils.CountBuildingByCommonName(new HashSet<string> { name }, player),
				baseBuilder.BuildingsBeingProduced.ContainsKey(name));

		// ESSENTIALS-01: the tier line is BotLimits.MaximiseProductionCashRequirement (Hard 3500); no BotLimits or a negative value = no line (inert).
		int TierLine => botLimits != null && botLimits.Info.MaximiseProductionCashRequirement >= 0 ? botLimits.Info.MaximiseProductionCashRequirement : 0;

		// PROD-BUSY cycle: the candidates are the same cap-filtered, power-resolved production buildings the overflow rung uses, plus the naval yard when
		// the base has water; the pick is CycleProductionCA (fewest first, then the next type in name order), no random draw.
		// SUSTAINED-FIX-01: a candidate the later admission votes would hold is dropped HERE (admits), so the pick never returns an item that the
		// TickQueue vetoes after the rung has already pre-empted the tech chain and the rest of the order: with none left the order falls through.
		ActorInfo ChooseCycledProduction(IEnumerable<ActorInfo> buildableThings, ActorInfo power, Func<ActorInfo, bool> admits = null)
		{
			var candidates = OverflowProductionCandidates(buildableThings, power);
			if (waterState == WaterCheck.EnoughWater
				&& AIUtils.IsAreaAvailable<GivesBuildableArea>(world, player, world.Map, baseBuilder.Info.CheckForWaterRadius, baseBuilder.Info.WaterTerrainTypes))
			{
				var naval = GetProducibleBuilding(baseBuilder.Info.NavalProductionTypes, buildableThings);
				if (naval != null)
				{
					var producing = baseBuilder.BuildingsBeingProduced.TryGetValue(naval.Name, out var n) ? n : 0;
					var navalVariants = naval.TraitInfoOrDefault<PlaceBuildingVariantsInfo>()?.Actors ?? [];
					var navalStanding = playerBuildings.Count(a => a.Info.Name == naval.Name || navalVariants.Contains(a.Info.Name));
					if (BaseBuilderQueueEvalCA.LimitAdmits(baseBuilder.TryGetBuildingLimit(naval.Name, out var limit), limit,
							playerBuildings.Count(a => a.Info.Name == naval.Name), producing)
						&& BaseBuilderQueueEvalCA.OverflowTypeAdmits(navalStanding, producing, ProductionTypeLimit))
					{
						var resolved = BaseBuilderQueueEvalCA.PickOrPower(naval, power, HasSufficientPowerForActor);
						if (resolved != null)
							candidates.Add(resolved);
					}
				}
			}

			if (admits != null)
				candidates.RemoveAll(c => !admits(c));

			if (candidates.Count == 0)
				return null;

			var counted = new List<(string Name, int Count)>(candidates.Count);
			foreach (var c in candidates)
				counted.Add((c.Name, playerBuildings.Count(a => a.Info.Name == c.Name)
					+ (baseBuilder.BuildingsBeingProduced.TryGetValue(c.Name, out var inProd) ? inProd : 0)));

			var index = CycleProductionCA.Pick(counted, lastCycledProduction);
			if (index < 0)
				return null;

			lastCycledProduction = candidates[index].Name;
			return candidates[index];
		}

		/// <summary>
		/// SUSTAINED-FIX-01: whether the admission votes TickQueue asks after the pick (the pause votes: army-first, share, reserve; the mobile reserve) would let
		/// <paramref name="item"/> through as an order of <paramref name="admission"/>. Side-effect-free queries only.
		/// </summary>
		bool VotesAdmit(ProductionQueue queue, ActorInfo item, RecoverySpendPolicyCA.Admission admission)
		{
			pauseBuilding ??= player.PlayerActor.TraitsImplementing<IBotRequestPauseBuildingProduction>().ToArray();
			mobileReserve ??= player.PlayerActor.TraitsImplementing<IBotMobileSpendReserve>().FirstEnabledTraitOrDefault();
			var essential = IsArmyFirstEssential(item);
			var cost = baseBuilder.ProductionCost(queue, item);
			var admitted = !pauseBuilding.Any(p => p.PausesBuilding(item, essential, admission, cost))
				&& !BaseBuilderQueueEvalCA.MobileSpendBlocks(playerResources.GetCashAndResources(), cost, mobileReserve?.MobileSpendFloor ?? 0, essential);
			if (!admitted)
				TechReject(item.Name, "vote");

			return admitted;
		}

		bool EssentialsHolds(ActorInfo item, bool isRefinery, bool plannerOrDemand, int bank)
		{
			var line = TierLine;
			if (line <= 0 || bank >= line)
				return false;

			var name = item.Name;
			return EssentialsLowCashCA.Holds(true, bank, line, !OwnedOrQueued(item), TechChainExempt(name), isRefinery,
				baseBuilder.Info.PowerTypes.Contains(name) && PowerShortRaw(PowerArbiter.SiteEssential),
				item.HasTraitInfo<CashTricklerInfo>(), plannerOrDemand);
		}

		/// <summary>
		/// UPGRADE-PRODUCERS-01: whether <paramref name="info"/> is a structure type the each-building-once rule must cover although it has no
		/// BuildingFractions row: not a wall, defence, anti-air, fake or proxy, and not a type with its own rule (yard, refinery, power, silo). Cached per name.
		/// </summary>
		bool UnlistedOnceKind(ActorInfo info) =>
			!(baseBuilder.Info.BuildingFractions != null && baseBuilder.Info.BuildingFractions.ContainsKey(info.Name)) && FirstCopyKind(info);

		bool BuildingStillDelayed(string name)
		{
			var delayed =
				baseBuilder.Info.BuildingDelays != null && baseBuilder.Info.BuildingDelays.TryGetValue(name, out var delay)
				&& BaseBuilderQueueEvalCA.StillDelayed(KnobTicks(delay * buildingDelayModifier / 100, name), world.WorldTick);
			if (delayed)
				TechReject(name, "delay");

			return delayed;
		}

		// BASE-02: owned or in-production count of a set of building types.
		int OwnedOrQueuedCount(IReadOnlyCollection<string> types)
		{
			var n = playerBuildings != null ? playerBuildings.Count(a => types.Contains(a.Info.Name)) : 0;
			foreach (var kv in baseBuilder.BuildingsBeingProduced)
				if (types.Contains(kv.Key))
					n += kv.Value;

			return n;
		}

		// EARLY-DEF-01 (bb_antirush_base): the anti-rush defence picked in this ChooseBuildingToBuild call (null otherwise). TickQueue treats it as an essential: it rides at its
		// own cost (no defence cash floor, no low-cash hold, no mobile-spend reserve); the AF2 cap and the RATIO first-copy rule still apply to it.
		string antiRushPickName;

		// The base set: right after the first production building, 2-3 ground defences (by tier) whatever the threat picture, one type at a time, the least-owned type first.
		// Only a defence the bank can pay now is picked, so a merged queue never stalls on saving for it. Null = the ladder goes on.
		ActorInfo ChooseAntiRushBase(ProductionQueue queue, IEnumerable<ActorInfo> buildableThings, ActorInfo power, int availableCash)
		{
			if (!AntiRushOn || playerBuildings == null)
				return null;

			var b = baseBuilder.Info;
			var have = playerBuildings.Count(a => IsGroundDefence(a.Info));
			foreach (var kv in baseBuilder.BuildingsBeingProduced)
				if (world.Map.Rules.Actors.TryGetValue(kv.Key, out var queued) && IsGroundDefence(queued))
					have += kv.Value;

			var size = AntiRushCA.SetSize(botLimits != null ? botLimits.Info.ArmyShareTargetPermille : -1, b.AntiRushBaseMin, b.AntiRushBaseMax);
			if (!AntiRushCA.Wants(playerBuildings.Any(a => b.ProductionTypes.Contains(a.Info.Name)), have, size))
				return null;

			var infos = new List<ActorInfo>();
			var candidates = new List<(string Name, int Cost, int OwnedOrQueued)>();
			foreach (var info in buildableThings)
			{
				if (!IsGroundDefence(info) || NoSpaceBanned(info.Name) || baseBuilder.ProductionCost(queue, info) > availableCash)
					continue;

				var count = OwnedOrQueuedCount([info.Name]);
				if (baseBuilder.TryGetBuildingLimit(info.Name, out var limit) && BaseBuilderQueueEvalCA.LimitReached(count, limit))
					continue;

				infos.Add(info);
				candidates.Add((info.Name, ValuedCost(info), count));
			}

			var index = AntiRushCA.Pick(candidates);
			if (index < 0)
				return null;

			var pick = infos[index];
			// V07 crash fix: PickOrPower returns null when the pick starves power and no power plant is buildable now.
			var item = BaseBuilderQueueEvalCA.PickOrPower(pick, power, HasSufficientPowerForActor);
			if (item == null)
				return null;

			if (item == pick)
				antiRushPickName = pick.Name;

			AIUtils.BotDebug("{0} decided to build {1}: Priority override (anti-rush base set {2}/{3})", player, item.Name, have, size);
			return item;
		}

		// BASE-02 (opening_standard_order): the standard order's item picked in this ChooseBuildingToBuild call (null otherwise). TickQueue rides it at its own cost
		// floor like a planner want; its low-cash hold and admission votes are checked BEFORE it is picked (OpeningAdmits), so a held step never shadows the ladder.
		string openingPickName;
		readonly Dictionary<(OpeningRole Role, int Required), int> openingStepSince = new();

		bool OpeningAdmits(ProductionQueue queue, ActorInfo item) =>
			VotesAdmit(queue, item, RecoverySpendPolicyCA.Admission.General)
			&& !(EssentialsOn && EssentialsHolds(item, false, false, CashLedger.BuildingBank(AllProducersOn, playerResources.GetCashAndResources(), orderedCostThisSweep)));

		// The cheapest building of <types> the faction can build now: within its limits, its production-type limit and its delay; first copies only when asked.
		ActorInfo OpeningCheapest(ProductionQueue queue, IEnumerable<ActorInfo> buildable, Func<ActorInfo, bool> inRole, bool firstCopyOnly)
		{
			ActorInfo best = null;
			var bestCost = int.MaxValue;
			foreach (var item in buildable)
			{
				if (!inRole(item) || (firstCopyOnly && OwnedOrQueued(item)) || BuildingStillDelayed(item.Name) || NoSpaceBanned(item.Name))
					continue;

				var count = playerBuildings.Count(a => a.Info.Name == item.Name);
				var producing = baseBuilder.BuildingsBeingProduced.TryGetValue(item.Name, out var n) ? n : 0;
				if (!BaseBuilderQueueEvalCA.LimitAdmits(baseBuilder.TryGetBuildingLimit(item.Name, out var limit), limit, count, producing))
					continue;

				if (baseBuilder.Info.ProductionTypes.Contains(item.Name) && botLimits != null && BaseBuilderQueueEvalCA.LimitReached(count + producing, ProductionTypeLimit))
					continue;

				if (NavalNeedsWater(item))
					continue;

				var cost = Math.Max(0, baseBuilder.ProductionCost(queue, item));
				if (best == null || cost < bestCost || (cost == bestCost && string.CompareOrdinal(item.Name, best.Name) < 0))
				{
					best = item;
					bestCost = cost;
				}
			}

			return best;
		}

		// BASE-02: the user's standard order. Each role is mapped through the existing building lists (power, barracks, refinery, vehicle factory, the other production
		// types as the airfield role, radar providers, the tech chain, cash tricklers); a role the faction lacks is skipped. Null = the normal ladder goes on.
		ActorInfo ChooseOpeningStandard(ProductionQueue queue, IEnumerable<ActorInfo> buildableThings, ActorInfo power)
		{
			openingPickName = null;
			var b = baseBuilder.Info;
			var now = world.WorldTick;
			if (!OpeningStepOn || b.DefenseQueues.Contains(Category) || now >= b.OpeningStandardMaxTicks || playerBuildings == null)
				return null;

			// An expansion yard (its refinery goes first) and a threat defence want interrupt the order; they run in the ladder below.
			if (OwnedOrQueuedCount(b.ConstructionYardTypes) > 1)
				return null;

			var law = baseBuilder.RefineryLawProvider();
			if (law != null && law.WantedFrontDefence != null)
				return null;

			var buildable = buildableThings as IReadOnlyList<ActorInfo> ?? buildableThings.ToList();
			var airRole = new HashSet<string>(b.ProductionTypes.Except(b.BarracksTypes).Except(b.VehiclesFactoryTypes).Except(b.NavalProductionTypes));
			var picks = new Dictionary<OpeningRole, ActorInfo>();

			(bool Can, int Have) Status(OpeningRole role)
			{
				ActorInfo pick;
				int have;
				switch (role)
				{
					case OpeningRole.Power:
						have = OwnedOrQueuedCount(b.PowerTypes);
						pick = power;
						break;
					case OpeningRole.Barracks:
						have = OwnedOrQueuedCount(b.BarracksTypes);
						pick = OpeningCheapest(queue, buildable, a => b.BarracksTypes.Contains(a.Name), false);
						break;
					case OpeningRole.Refinery:
						have = OwnedOrQueuedCount(b.RefineryTypes);
						// CM FIX: the slot is open only when the wanted-refinery rung will really pick one (adequate count, the max and the expansion safety all close it): a closed slot is skipped, never waited on.
						pick = ChooseWantedRefinery(queue, buildable, power);
						break;
					case OpeningRole.WarFactory:
						have = OwnedOrQueuedCount(b.VehiclesFactoryTypes);
						pick = OpeningCheapest(queue, buildable, a => b.VehiclesFactoryTypes.Contains(a.Name), false);
						break;
					case OpeningRole.Radar:
						have = OwnedRadarProviders();
						var radars = GetRadarProducibles(buildable);
						pick = OpeningCheapest(queue, radars, a => true, true);
						break;
					case OpeningRole.Airfield:
						have = OwnedOrQueuedCount(airRole);
						pick = OpeningCheapest(queue, buildable, a => airRole.Contains(a.Name), false);
						break;
					case OpeningRole.TechCenter:
						have = OwnedOrQueuedCount(b.TechChainTypes);
						pick = OpeningCheapest(queue, buildable, a => b.TechChainTypes.Contains(a.Name), true);
						break;
					default:
						have = playerBuildings.Count(a => a.Info.HasTraitInfo<CashTricklerInfo>())
							+ baseBuilder.BuildingsBeingProduced.Count(kv => kv.Value > 0 && world.Map.Rules.Actors.TryGetValue(kv.Key, out var info) && info.HasTraitInfo<CashTricklerInfo>());
						pick = OpeningCheapest(queue, buildable, a => a.HasTraitInfo<CashTricklerInfo>(), true);
						break;
				}

				if (pick == null)
					return (false, have);

				picks[role] = pick;

				// The refinery is picked by the wanted-refinery rung (its own placement law); every other step must pass the admission votes and the low-cash hold now.
				return (role == OpeningRole.Refinery || role == OpeningRole.Power || OpeningAdmits(queue, pick), have);
			}

			bool Stuck(OpeningRole role, int required)
			{
				if (!openingStepSince.TryGetValue((role, required), out var since))
				{
					openingStepSince[(role, required)] = now;
					return false;
				}

				// Cumulative since the step first became the next step (a cash hold does not reset it): after OpeningStepStuckTicks the order goes on without it.
				return now - since > b.OpeningStepStuckTicks;
			}

			var order = BaseFix01CA.StandardOrder(b.OpeningSecondBarracksFirstFactions.Contains(player.Faction.InternalName));
			var next = BaseFix01CA.NextRole(order, Status, Stuck, (role, required) => openingStepSince.Remove((role, required)));
			if (next == OpeningRole.None || next == OpeningRole.Refinery)
				return null;

			var item = BaseBuilderQueueEvalCA.PickOrPower(picks[next], power, HasSufficientPowerForActor);
			if (item != null)
			{
				openingPickName = item.Name;
				AIUtils.BotDebug("{0} decided to build {1}: Priority override (standard opening {2})", player, item.Name, next);
			}

			return item;
		}

		// BASE-05: a pick the admission votes would hold is dropped here so the ladder goes on to the lower rungs (off = never filtered).
		bool Admitted(ProductionQueue queue, ActorInfo pick) =>
			pick == null || !PrefilterOn || VotesAdmit(queue, pick, RecoverySpendPolicyCA.Admission.General);

		// BASE-04: ground defence = a defence type that is no anti-air (anti-air has its own intel gate).
		bool IsGroundDefence(ActorInfo info)
		{
			var b = baseBuilder.Info;
			if (b.AntiAirTypes.Contains(info.Name))
				return false;

			if (!staticValuesResolved)
			{
				staticValues = player.PlayerActor.TraitsImplementing<IBotMobileStaticValues>().FirstOrDefault();
				staticValuesResolved = true;
			}

			return b.DefenseTypes.Contains(info.Name) || (staticValues != null && staticValues.IsStaticDefence(info));
		}

		// BASE-04: a threat was seen (latched for the game): the threat analysis reports an active threat or a nemesis, or an enemy armed unit we can see stands
		// within DefenceNeedRadius of a construction yard. Re-checked every 25 ticks; observation only, no hidden state is read.
		bool defenceThreatSeen;
		int defenceThreatCheckTick = -1000;
		IBotThreatAnalysis defenceThreatAnalysis;
		bool defenceThreatResolved;

		bool DefenceThreatSeen()
		{
			if (defenceThreatSeen)
				return true;

			var now = world.WorldTick;
			if (now - defenceThreatCheckTick < 25)
				return false;

			defenceThreatCheckTick = now;
			if (!defenceThreatResolved)
			{
				defenceThreatAnalysis = player.PlayerActor.TraitsImplementing<IBotThreatAnalysis>().FirstOrDefault();
				defenceThreatResolved = true;
			}

			if (defenceThreatAnalysis != null && (defenceThreatAnalysis.HasActiveThreat() || defenceThreatAnalysis.GetNemesis() != null))
				return defenceThreatSeen = true;

			if (playerBuildings == null)
				return false;

			var radius = WDist.FromCells(baseBuilder.Info.DefenceNeedRadius);
			foreach (var yard in playerBuildings)
			{
				if (!baseBuilder.Info.ConstructionYardTypes.Contains(yard.Info.Name))
					continue;

				foreach (var a in world.FindActorsInCircle(yard.CenterPosition, radius))
				{
					if (!a.IsDead && a.IsInWorld && player.RelationshipWith(a.Owner) == PlayerRelationship.Enemy
						&& a.Info.HasTraitInfo<AttackBaseInfo>() && a.CanBeViewedByPlayer(player))
						return defenceThreatSeen = true;
				}
			}

			return false;
		}

		/// <summary>A naval production structure while the base has no water to place it on (same test as the fraction roll); never essential, never picked.</summary>
		bool NavalNeedsWater(ActorInfo info) =>
			BaseBuilderQueueEvalCA.NavalBlocked(baseBuilder.Info.NavalProductionTypes.Contains(info.Name), waterState == WaterCheck.NotEnoughWater,
				() => AIUtils.IsAreaAvailable<GivesBuildableArea>(world, player, world.Map, baseBuilder.Info.CheckForWaterRadius, baseBuilder.Info.WaterTerrainTypes));

		/// <summary>ESSENTIAL-PASS-01: the structure kinds whose first copy is essential: UnlistedOnceKind's classification without the fraction-row test. Cached per name.</summary>
		bool FirstCopyKind(ActorInfo info)
		{
			if (unlistedKinds.TryGetValue(info.Name, out var kind))
				return kind;

			if (!staticValuesResolved)
			{
				staticValues = player.PlayerActor.TraitsImplementing<IBotMobileStaticValues>().FirstOrDefault();
				staticValuesResolved = true;
			}

			var b = baseBuilder.Info;
			var name = info.Name;
			kind = UnlistedOnceCA.Eligible(
				inFractions: false,
				isBuilding: info.HasTraitInfo<BuildingInfo>(),
				wallOrFake: info.HasTraitInfo<LineBuildInfo>() || info.TraitInfos<GivesBuildableAreaInfo>().Any(g => g.AreaTypes.Contains("fake")),
				hasOwnRule: b.ConstructionYardTypes.Contains(name) || b.RefineryTypes.Contains(name) || b.PowerTypes.Contains(name) || b.SiloTypes.Contains(name),
				defence: b.DefenseTypes.Contains(name) || b.AntiAirTypes.Contains(name) || (staticValues != null && staticValues.IsStaticDefence(info)),
				name: name);
			unlistedKinds[name] = kind;
			return kind;
		}

		// UPGRADE-PRODUCERS-01: the cheapest buildable first copy of an unlisted structure type (UnlistedOnceCA.Pick), within the build limits, the naval
		// water check and the power rule (power instead when the pick would leave power short). Only the building queues, and only once a production building stands.
		ActorInfo ChooseUnlistedOnce(ProductionQueue queue, IEnumerable<ActorInfo> buildableThings, ActorInfo power) =>
			ChooseFirstCopyOnce(queue, buildableThings, power, false);

		/// <summary>
		/// BASE-03: the same pick with <paramref name="includeListed"/> = every first-copy kind (the fraction-listed types and the tech chain too), used by the
		/// sustained rung. BuildingDelays and BuildingIntervals are honoured there, so a techrush-guarded building never comes early.
		/// </summary>
		ActorInfo ChooseFirstCopyOnce(ProductionQueue queue, IEnumerable<ActorInfo> buildableThings, ActorInfo power, bool includeListed)
		{
			if (!(includeListed ? SustainedKeepsOn : UnlistedOnceOn) || baseBuilder.Info.DefenseQueues.Contains(Category) || !baseBuilder.HasAdequateProductionCount())
				return null;

			var infos = new List<ActorInfo>();
			var candidates = new List<(string Name, int Cost)>();
			foreach (var item in buildableThings)
			{
				if (!(includeListed ? FirstCopyKind(item) : UnlistedOnceKind(item)) || OwnedOrQueued(item) || NoSpaceBanned(item.Name))
					continue;

				if (includeListed && (BuildingStillDelayed(item.Name) || (baseBuilder.Info.BuildingIntervals != null && baseBuilder.Info.BuildingIntervals.ContainsKey(item.Name)
					&& activeBuildingIntervals.ContainsKey(item.Name))))
					continue;

				if (baseBuilder.TryGetBuildingLimit(item.Name, out var limit) && BaseBuilderQueueEvalCA.LimitReached(0, limit))
					continue;

				if (baseBuilder.Info.ProductionTypes.Contains(item.Name) && botLimits != null && BaseBuilderQueueEvalCA.LimitReached(0, ProductionTypeLimit))
					continue;

				if (NavalNeedsWater(item))
					continue;

				infos.Add(item);
				candidates.Add((item.Name, Math.Max(0, baseBuilder.ProductionCost(queue, item))));
			}

			var index = UnlistedOnceCA.Pick(candidates);
			return index < 0 ? null : BaseBuilderQueueEvalCA.PickOrPower(infos[index], power, HasSufficientPowerForActor);
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

			// BINDING tech chain (11-23): the faction's T2 and T3 gate passes the army-first pause and the mobile reserve while none is owned or queued.
			if (TechChainExempt(name))
				return true;

			// ESSENTIAL-PASS-01: the first copy of every building type and every cash trickler passes the share, reserve and mobile-spend votes.
			if (EssentialPassOn && !NavalNeedsWater(actorInfo))
			{
				// the owned/queued scan only runs for an eligible kind (Haiku NIT)
				var firstCopyKind = FirstCopyKind(actorInfo);
				if (EssentialPassCA.Essential(firstCopyKind, firstCopyKind && OwnedOrQueued(actorInfo), actorInfo.HasTraitInfo<CashTricklerInfo>()))
					return true;
			}

			// UPGRADE-PRODUCERS-01: the first copy of a structure type with no fraction row (upgrade/research producers, radar, tech centres) is essential.
			if (UnlistedOnceOn && UnlistedOnceKind(actorInfo) && !NavalNeedsWater(actorInfo) && !OwnedOrQueued(actorInfo))
				return true;

			// One queued-but-unplaced factory is enough to break the deadlock — don't let the
			// gate admit a second one while the first is still producing.
			if (baseBuilder.Info.ProductionTypes.Contains(name)
				&& !baseBuilder.HasAdequateProductionCount()
				&& !baseBuilder.BuildingsBeingProduced.Keys.Any(baseBuilder.Info.ProductionTypes.Contains))
				return true;

			return false;
		}

		// Record-only economy trace (A/B sentinel triage 2026-10-13): why the NewProductionCashThreshold
		// override did not run while the bank sat over it. Bounded — the first 8 lines per kind, then every
		// 25th — written to the "debug" channel; moves no order, RNG or state the sim reads.
		readonly Dictionary<string, int> econTraceCounts = new Dictionary<string, int>();

		bool EconTraceDue(string kind)
		{
			econTraceCounts.TryGetValue(kind, out var n);
			econTraceCounts[kind] = ++n;
			return n <= 8 || n % 25 == 0;
		}

		string EconTraceState() =>
			$"tick {world.WorldTick} bank {playerResources.GetCashAndResources()} threshold {NewProductionCashThreshold}"
			+ $" excess {playerPower?.ExcessPower} minExcess {minimumExcessPower} buildings {playerBuildings.Count()}"
			+ $" plants {playerBuildings.Count(a => baseBuilder.Info.PowerTypes.Contains(a.Info.Name))}"
			+ $" production {playerBuildings.Count(a => baseBuilder.Info.ProductionTypes.Contains(a.Info.Name))}";

		void TraceLowPowerOverride(ActorInfo power)
		{
			if (EconTraceDue("lowpower"))
			{
#if CAMEO_DEV_LOGGING
				Log.Write("debug", $"AI ({player.ClientIndex}): ECON-TRACE lowpower-override {power.Name} {EconTraceState()}");
#endif
			}
		}

		void TraceThresholdOverride(string outcome, ActorInfo pick)
		{
			if (EconTraceDue("threshold-" + outcome))
			{
#if CAMEO_DEV_LOGGING
				Log.Write("debug", $"AI ({player.ClientIndex}): ECON-TRACE threshold-override {outcome} {pick?.Name ?? "-"} {EconTraceState()}");
#endif
			}
		}

		// PUSH-1: the conditioned overflow rung's outcomes — armed/declined/picked, same bounded writer.
		void TraceOverflow(string outcome, ActorInfo pick)
		{
			if (EconTraceDue("overflow-" + outcome))
			{
#if CAMEO_DEV_LOGGING
				Log.Write("debug", $"AI ({player.ClientIndex}): ECON-TRACE overflow {outcome} {pick?.Name ?? "-"} {EconTraceState()}");
#endif
			}
		}

		// The bank is over the threshold yet an earlier priority took the queue: name the pre-emptor.
		void TraceThresholdShadow(ProductionQueue queue, ActorInfo item)
		{
			if (NewProductionCashThreshold <= 0 || playerResources.GetCashAndResources() <= NewProductionCashThreshold
				|| baseBuilder.Info.ProductionTypes.Contains(item.Name))
				return;

			if (EconTraceDue("shadow-" + item.Name))
			{
#if CAMEO_DEV_LOGGING
				Log.Write("debug", $"AI ({player.ClientIndex}): ECON-TRACE threshold-shadowed-by {item.Name} {EconTraceState()}");
#endif
			}
		}

		// RATIO-2X-01 (ARMY_FIRST_02_DESIGN_04/05) ------------------------------------------------------------------------------
		// The 2x mobile/static ratio at a static-defence start. The pure decision is MobileStaticRatioCA; this is the measure
		// assembly and the two call sites' shared entry. Read-only: no order, no RNG, no queue state; the only write is the
		// shared tracker's record at an emitted start (in TickQueue, right after the order is queued).

		bool RatioActive() => baseBuilder.Info.MobileStaticRatioEnabled && !ablateRatio.Off(player, AblationIds.Ratio2x);

		static int ValuedCost(ActorInfo info) => info.TraitInfoOrDefault<ValuedInfo>()?.Cost ?? 0;

		// The type is owned (incl. its placement variants) or in production: the same count the fraction loop uses.
		bool OwnedOrQueued(ActorInfo info)
		{
			var variants = info.TraitInfoOrDefault<PlaceBuildingVariantsInfo>()?.Actors ?? [];
			return playerBuildings.Any(a => a.Info.Name == info.Name || variants.Contains(a.Info.Name))
				|| (baseBuilder.BuildingsBeingProduced.TryGetValue(info.Name, out var n) && n > 0);
		}

		// S: valued cost of the live static defences plus the defence items in ANY building/defence queue (READY-held included),
		// computed once per world tick; the 3-tick record of admitted starts is added on every read (a stated conservative
		// double-count window while an order becomes a queue item).
		long StaticDefenceValue(int now)
		{
			if (staticLiveTick != now)
			{
				long total = 0;
				foreach (var a in playerBuildings)
					if (staticValues.IsStaticDefence(a.Info))
						total += ValuedCost(a.Info);

				foreach (var category in baseBuilder.Info.BuildingQueues.Concat(baseBuilder.Info.DefenseQueues).Distinct())
					foreach (var q in AIUtils.FindQueues(player, category))
						foreach (var queued in q.AllQueued())
						{
							var queuedInfo = world.Map.Rules.Actors[queued.Item];
							if (staticValues.IsStaticDefence(queuedInfo))
								total += ValuedCost(queuedInfo);
						}

				staticLiveValue = total;
				staticLiveTick = now;
			}

			return staticLiveValue + baseBuilder.StaticRatio.Recorded(now);
		}

		// IsFloating (USER 11-22, Opus T1): ONE definition shared by the RATIO FLOAT exemption and TECHFIRST-01's float rule: storage at or
		// above 50% of capacity (alone: money near the cap is wasted), OR every unit queue saturated AND Cash+Resources at the tier's RAW
		// BankSaturationCash bar (bar 0 = that leg never applies). The two bank legs are cheap ints; the unit side is a cached provider
		// answer read only when the bank leg holds (MobileStaticRatioCA.IsFloating short-circuits).
		bool IsFloating()
		{
			mobileReserve ??= player.PlayerActor.TraitsImplementing<IBotMobileSpendReserve>().FirstEnabledTraitOrDefault();
			return MobileStaticRatioCA.IsFloating(
				MobileStaticRatioCA.StorageFloating(playerResources.Resources, playerResources.ResourceCapacity),
				MobileStaticRatioCA.BankOverBar(playerResources.GetCashAndResources(), botLimits?.Info.BankSaturationCash ?? 0),
				mobileReserve);
		}

		// The verdict for starting `item` now. Inert covers every case the rule does not apply to (switch off, ablated, not a
		// static defence, no usable measure); `isDefence` says whether the start must be recorded either way.
		RatioVerdict RatioGate(ActorInfo item, out bool isDefence)
		{
			isDefence = false;
			if (!RatioActive())
				return RatioVerdict.Inert;

			// Not filtered by IsTraitEnabled: HasSnapshot carries the provider's own enabled state, and a null cached while the
			// provider was still condition-disabled would otherwise pin the rule off for the whole game.
			if (!staticValuesResolved)
			{
				staticValues = player.PlayerActor.TraitsImplementing<IBotMobileStaticValues>().FirstOrDefault();
				staticValuesResolved = true;
			}

			if (staticValues == null || !staticValues.IsStaticDefence(item))
				return RatioVerdict.Inert;

			isDefence = true;
			var now = world.WorldTick;
			if (!MobileStaticRatioCA.MeasureUsable(staticValues.HasSnapshot, staticValues.SnapshotTick, now))
				return RatioVerdict.Inert;

			var mobile = staticValues.MobileValue;
			var staticTotal = StaticDefenceValue(now);
			var cost = ValuedCost(item);

			// The owned/queued count is only needed when the ratio failed (a first copy matters only below the ratio).
			var verdict = MobileStaticRatioCA.Stage1(true, mobile, staticTotal, cost,
				(!MobileStaticRatioCA.Admits(mobile, staticTotal, cost) && baseBuilder.StaticRatio.IsFirstCopy(item.Name, OwnedOrQueued(item))
				&& (!DefenceFirstCopyOn || DefenceThreatSeen()))
				|| (antiRushPickName != null && item.Name == antiRushPickName)); // C-04: the first copy of a defence type is exempt only while a threat is seen; EARLY-DEF-01: the base set is exempt (still recorded below)
			if (verdict == RatioVerdict.NeedsFloat)
				verdict = MobileStaticRatioCA.Float(IsFloating());

#if CAMEO_DEV_LOGGING
			if (verdict != RatioVerdict.Admit && EconTraceDue("ratio-" + verdict + "-" + item.Name))
				Log.Write("debug", $"AI ({player.ClientIndex}): RATIO-{verdict} item {item.Name} cost {cost} M {mobile} S {staticTotal} cash {playerResources.Cash} resources {playerResources.Resources} capacity {playerResources.ResourceCapacity} bankBar {botLimits?.Info.BankSaturationCash ?? 0} tick {now}");
#endif
			return verdict;
		}

		// The refinery the economy rung wants (also the expansion refinery of a new yard under the refinery law: HasAdequateRefineryCount is false while an
		// anchor is claimable), or null. Shared by the economy rung and SUSTAINED-CASH-01 so both pick the same item.
		ActorInfo ChooseWantedRefinery(ProductionQueue queue, IEnumerable<ActorInfo> buildableThings, ActorInfo power)
		{
			// CONSOLIDATE-03 P3: the chain below is the one refinery-want verdict (RefineryWant / RefineryGate); every exit records the gate that held through RefineryHeld.
			var adequacy = baseBuilder.AdequateReason();
			if (adequacy != RefineryAdequacy.Inadequate)
				return RefineryHeld(RefineryWant.ToGate(adequacy));

			// pb_expansion_refinery_safety: a refinery wanted for an expansion yard under threat is dropped from this slot (not held).
			if (SafetyOn)
			{
				if (SafetyAllOn)
				{
					// BASE-12: only when every wanting claim is unsafe is the slot dropped; a safe claim behind an unsafe head claim still goes.
					if (baseBuilder.ExpansionRefineryBlockedByAllUnsafe())
						return RefineryHeld(RefineryGate.UnsafeAll);
				}
				else
				{
					var wantedBy = baseBuilder.ExpansionWantsRefinery();
					var wantedField = wantedBy?.RefineryClaimTarget ?? wantedBy?.ExpansionTarget;
					if (wantedField != null && !baseBuilder.ExpansionRefinerySafe(wantedField.Value))
						return RefineryHeld(RefineryGate.UnsafeField);
				}
			}

			var refinery = GetProducibleBuilding(baseBuilder.Info.RefineryTypes, buildableThings);
			if (refinery == null)
				return RefineryHeld(RefineryGate.NotBuildable);

			// RefineryLimit is shared across every faction the player owns, so always allow
			// a faction's own first refinery through even if that global cap has already
			// been reached by a different faction's construction yard.
			if (baseBuilder.HasMaxRefineriesFor(refinery))
				return RefineryHeld(RefineryGate.MaxRefineries);

			// REF-FIELD-02 (row rf_economy_gate): ref1, the first vehicle factory, then ref2; the interval; the bounded first-copy yield.
			if (RefineryEconomyGateOn && RefineryEconomyHolds(queue, buildableThings))
				return RefineryHeld(RefineryGate.Economy);

			var pick = BaseBuilderQueueEvalCA.PickOrPower(refinery, power, HasSufficientPowerForActor);
			lastRefineryGate = RefineryGate.None;
			RefineryWantObserved(RefineryGate.None, pick != null);
			if (pick != null)
			{
				AIUtils.BotDebug("{0} decided to build {1}: Priority override ({2})", queue.Actor.Owner, pick.Name,
					pick == refinery ? "refinery" : "would be low power");
				if (pick == refinery)
					baseBuilder.RefineryLawProvider()?.RefineryPickOffered(); // ref_park_evaluated_only: the refinery itself was offered (a low-power pick returns the power plant: not an offer)
			}

			return pick;
		}

		// REF-FIELD-02: true = the refinery pick waits this pass. The rise clock and the yield clock live on the module (shared by its queues).
		bool RefineryEconomyHolds(ProductionQueue queue, IEnumerable<ActorInfo> buildableThings)
		{
			var now = world.WorldTick;
			var count = baseBuilder.RefineryCountWithQueued();
			if (count > baseBuilder.RefineryGateLastCount)
				baseBuilder.RefineryGateLastRiseTick = now;

			baseBuilder.RefineryGateLastCount = count;
			if (baseBuilder.RefineryGateFirstStoodTick < 0 && AIUtils.CountActorByCommonName(baseBuilder.RefineryBuildings) > 0)
				baseBuilder.RefineryGateFirstStoodTick = now;

			var b = baseBuilder.Info;
			var factoryBuildable = buildableThings.Any(a => b.VehiclesFactoryTypes.Contains(a.Name));
			var factoryOwnedOrQueued = baseBuilder.VehicleFactoryOwnedOrQueued();
			var factoryWaitReleased = RefineryFieldGateCA.FactoryWaitReleased(b.VehiclesFactoryTypes.Any(NoSpaceBanned),
				baseBuilder.RefineryGateFactoryPlacementFailed, now, baseBuilder.RefineryGateFirstStoodTick, b.RefineryFactoryWaitTicks);
			var yardWithoutRefinery = count > 0 && baseBuilder.YardWithoutRefinery();
			var verdict = RefineryFieldGateCA.Economy(true, count, yardWithoutRefinery, factoryBuildable, factoryOwnedOrQueued, factoryWaitReleased, now,
				baseBuilder.RefineryGateLastRiseTick, baseBuilder.RefineryMinIntervalEffective, false, -1, b.RefineryEssentialYieldTicks);

			// The first-copy scan runs only when the earlier steps admit the refinery and two refineries already stand or are queued.
			if (verdict == RefineryFieldGateCA.EconomyVerdict.Allow && count >= 2)
			{
				var essential = EssentialFirstCopyPending(buildableThings);
				if (!essential)
					baseBuilder.RefineryGateYieldSince = -1;
				else if (baseBuilder.RefineryGateYieldSince < 0)
					baseBuilder.RefineryGateYieldSince = now;

				verdict = RefineryFieldGateCA.Economy(true, count, yardWithoutRefinery, factoryBuildable, factoryOwnedOrQueued, factoryWaitReleased, now,
					baseBuilder.RefineryGateLastRiseTick, baseBuilder.RefineryMinIntervalEffective, essential, baseBuilder.RefineryGateYieldSince, b.RefineryEssentialYieldTicks);
			}

			// CM NIT: logged once per verdict change, not on every call.
			if (verdict != lastRefineryEconomyVerdict)
			{
				lastRefineryEconomyVerdict = verdict;
				if (verdict != RefineryFieldGateCA.EconomyVerdict.Allow)
				{
					AIUtils.BotDebug("{0} holds the refinery pick: {1} (refineries {2})", queue.Actor.Owner, verdict, count);
#if CAMEO_DEV_LOGGING
					// Opus 11-26 (V08G): BotDebug reaches the in-game notification only; this puts the refinery-pick hold into the debug log file (DEV build, once per verdict change).
					Log.Write("debug", $"AI ({player.ClientIndex}): REF-HOLD refinery pick held {verdict} refineries {count} tick {now} queue {Category}");
#endif
				}
			}

			// REF2-FIRST-01 (row ref2_floor): a due ref2 outranks the interval wait only (the first-war-factory wait stays: ref1, war factory, ref2).
			if (verdict == RefineryFieldGateCA.EconomyVerdict.WaitInterval && Ref2OutranksInterval(buildableThings, count))
				return false;

			// REF-TIER-01 (row ref_tier_floor): below the tier target the interval wait is outranked too. The bounded first-copy yield still applies from the 3rd refinery on (tech chain and each building once
			// win over the floor), the first-war-factory wait was already decided above, and nothing here holds a radar or production start (Ref2Holds is the only hold).
			if (verdict == RefineryFieldGateCA.EconomyVerdict.WaitInterval && RefTierOutranksInterval(count))
				return RefTierYieldVerdict(buildableThings, count, now, yardWithoutRefinery, factoryBuildable, factoryOwnedOrQueued, factoryWaitReleased) != RefineryFieldGateCA.EconomyVerdict.Allow;

			return verdict != RefineryFieldGateCA.EconomyVerdict.Allow;
		}

		RefineryFieldGateCA.EconomyVerdict lastRefineryEconomyVerdict = RefineryFieldGateCA.EconomyVerdict.Allow;

		// REF-FIELD-02: a first copy this queue can build is still missing: the tech chain, or a first-copy kind (no wall, defence, fake, proxy or
		// own-rule type) that is not owned or queued, within its build limit and not a naval yard without water.
		bool EssentialFirstCopyPending(IEnumerable<ActorInfo> buildableThings)
		{
			foreach (var item in buildableThings)
			{
				if (baseBuilder.Info.RefineryTypes.Contains(item.Name))
					continue;

				if (TechChainExempt(item.Name))
					return true;

				if (!FirstCopyKind(item) || NavalNeedsWater(item) || OwnedOrQueued(item))
					continue;

				if (baseBuilder.TryGetBuildingLimit(item.Name, out var limit) && BaseBuilderQueueEvalCA.LimitReached(0, limit))
					continue;

				return true;
			}

			return false;
		}

		ActorInfo ChooseBuildingToBuild(ProductionQueue queue)
		{
			sustainedPickName = null;
			openingPickName = null;
			antiRushPickName = null;
			var buildableThings = queue.BuildableItems();
			if (baseBuilder.NoFoundationArmed)
				buildableThings = FoundationExclusion.Without(buildableThings.ToList(), baseBuilder.FoundationTypes); // BUILD-ALL-B: no concrete (UnlistedBuildingsOnce included)
			if (TechLogOn) // TECH-LOG-01: only a tech-chain type this queue offers, and cannot build yet, has an unmet prerequisite
			{
				var techBuildable = buildableThings.Select(b => b.Name).ToHashSet();
				foreach (var prereqType in TechLogCA.PrereqMissing(queue.AllItems().Select(i => i.Name), baseBuilder.Info.TechChainTypes.Contains, techBuildable))
					TechReject(prereqType, "prereq");
			}
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
			if (PowerShortEmergency(PowerArbiter.SiteOverride) && power != null && power.TraitInfos<PowerInfo>().Where(i => i.EnabledByDefault).Sum(p => p.Amount) > 0)
			{
					AIUtils.BotDebug("{0} decided to build {1}: Priority override (low power)", queue.Actor.Owner, power.Name);
					TraceLowPowerOverride(power);
					return power;
			}

			// SUSTAINED-CASH-01 (USER 11-24): the bank has stayed above the tier line for SustainedCashTicks, so the next building is another production
			// building, cycled fewest-first across the types (power only when it would leave power short: the candidates are power-resolved). Order while
			// active: low power (above), the EXPANSION REFINERY (external fields are harvested ASAP; same pick as the economy rung), then this pick, above the
			// opening, the planner wants and the expansion demand. Types at their limit are not candidates; with none left (null) normal building resumes.
			// The defence queue never reaches it.
			if (SustainedActive)
			{
				var expansionRefinery = ChooseWantedRefinery(queue, buildableThings, power);
				if (expansionRefinery != null)
					return expansionRefinery;

				// BASE-03 (Opus ruling): the tech chain and each building once stay above the sustained production pick.
				var keptEssential = ChooseFirstCopyOnce(queue, buildableThings, power, true);
				if (keptEssential != null)
				{
					AIUtils.BotDebug("{0} decided to build {1}: Priority override (essential kept above sustained)", queue.Actor.Owner, keptEssential.Name);
					return keptEssential;
				}

				var sustained = ChooseCycledProduction(buildableThings, power, c => VotesAdmit(queue, c, RecoverySpendPolicyCA.Admission.EssentialBuilding));
				if (sustained != null)
				{
					sustainedPickName = sustained.Name;
					AIUtils.BotDebug("{0} decided to build {1}: Priority override (sustained cash production)", queue.Actor.Owner, sustained.Name);
					return sustained;
				}
			}

			if (openingBarracksPolicyActive && !baseBuilder.HasBuiltOrQueuedBarracks())
			{
				// Finish the opening power plant before starting the barracks.
				if (!baseBuilder.HasCompletedPowerPlant())
				{
					if (!baseBuilder.HasQueuedPowerPlant() && power != null && baseBuilder.ProductionCost(queue, power) <= availableCash)
					{
						AIUtils.BotDebug("{0} decided to build {1}: Priority override (opening power)", queue.Actor.Owner, power.Name);
						return power;
					}
				}
				else
				{
					var barracks = GetProducibleBuilding(baseBuilder.Info.BarracksTypes, buildableThings);

					if (barracks != null && baseBuilder.ProductionCost(queue, barracks) <= availableCash)
					{
						if (HasSufficientPowerForActor(barracks))
						{
							AIUtils.BotDebug("{0} decided to build {1}: Priority override (opening barracks)", queue.Actor.Owner, barracks.Name);
							return barracks;
						}

						if (power != null && baseBuilder.ProductionCost(queue, power) <= availableCash)
						{
							AIUtils.BotDebug("{0} decided to build {1}: Priority override (opening barracks would cause low power)", queue.Actor.Owner, power.Name);
							return power;
						}
					}
				}
			}

			// EARLY-DEF-01 (USER 11-24): the anti-rush base set comes right after the first production building, above the rest of the opening order.
			var antiRush = ChooseAntiRushBase(queue, buildableThings, power, availableCash);
			if (antiRush != null)
				return antiRush;

			// BASE-02 (AUDIT_BASE_01): power, barracks, refinery, war factory: the next missing opening step outranks the refinery pick for every faction.
			var openingStep = ChooseOpeningStandard(queue, buildableThings, power);
			if (openingStep != null)
				return openingStep;

			// Next is to build up a strong economy
			var wantedRefinery = ChooseWantedRefinery(queue, buildableThings, power);
			if (wantedRefinery != null)
				return wantedRefinery;

			// REF-1 B1/B2 (§12.24 v2): the planner's own wants, after refineries but before the fraction roll —
			// the due MCV's missing prerequisite, then the cheapest crawl-eligible link building while the crawl
			// target is out of reach. Only under the refinery law (the provider publishes nulls otherwise), and
			// only when the wanted building isn't already in production.
			var law = baseBuilder.RefineryLawProvider();

			ActorInfo PlannerWantPick(string want)
			{
				if (want == null || (baseBuilder.BuildingsBeingProduced?.ContainsKey(want) ?? false))
					return null;

				var pick = GetProducibleBuilding(new HashSet<string> { want }, buildableThings, a => 0);
				if (pick == null)
					return null;

				var wantPick = BaseBuilderQueueEvalCA.PickOrPower(pick, power, HasSufficientPowerForActor);
				if (wantPick != null)
					AIUtils.BotDebug("{0} decided to build {1}: Priority override ({2})", queue.Actor.Owner, wantPick.Name,
						wantPick == pick ? "REF-1 planner want" : "planner want would be low power");

				return wantPick;
			}

			if (law != null)
			{
				// The due MCV's prerequisite keeps its slot above the overflow rung: a banked bot
				// must still be able to reach the next expansion's tech gate.
				var mcvPick = PlannerWantPick(law.WantedMcvPrerequisite);
				if (mcvPick != null)
					return mcvPick;
			}

			// PUSH-1 (bank overflow): the conditioned policy rung converts a bank over its line into
			// producer width — above the crawl-link/front-defence wants that shadowed the legacy
			// threshold rung, below the safety rungs (opening, low power, refinery law, MCV gate).
			// Candidates are cap-filtered AND power-resolved before a single deterministic draw;
			// any decline — empty set or policy absent — consumes no RNG.
			// PROD-IDLE G1: the line is the tier's BotLimits.OverflowCash (12000 - 800 r) when written, else the
			// carrier's own; the carrier stays the on/off scope (classic has none) and pi_overflow ablates the row.
			if (BaseBuilderQueueEvalCA.OverflowArmed(
					overflowPolicy != null && overflowPolicy.IsTraitEnabled() && !ablateOverflow.Off(player, AblationIds.Overflow),
					availableCash,
					BaseBuilderQueueEvalCA.OverflowLine(botLimits != null ? botLimits.Info.OverflowCash : -1,
						overflowPolicy?.Info.OverflowCash ?? 0)))
			{
				var overflowCandidates = OverflowProductionCandidates(buildableThings, power);
				if (PrefilterOn)
					overflowCandidates.RemoveAll(c => !Admitted(queue, c));

				var pick = BaseBuilderQueueEvalCA.DrawOverflowPick(overflowCandidates,
					BotRng.For(player, nameof(BaseBuilderQueueManagerCA)));
				if (pick == null)
					TraceOverflow("no-candidate", null);
				else
				{
					AIUtils.BotDebug("{0} decided to build {1}: Priority override ({2})", queue.Actor.Owner, pick.Name,
						pick != power ? "overflow production" : "overflow would be low power");
					TraceOverflow(pick != power ? "production" : "power-swap", pick);
					return pick;
				}
			}

			if (law != null)
			{
				// CRAWL-SAFETY: the front defence rides ahead of the link it precedes.
				foreach (var want in new[] { law.WantedFrontDefence, law.WantedLinkBuilding })
				{
					var wantPick = PlannerWantPick(want);
					if (wantPick != null)
						return wantPick;
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

			// UPGRADE-PRODUCERS-01 (USER P0): each building once. A structure type with no BuildingFractions row (the upgrade and research producers, radar,
			// tech centres) is never reached by the fraction roll below, so its first copy is picked here, after every essential, planner and demand rung and
			// above the production cycling and the bank rungs. Row af2_unlisted_once.
			var unlisted = ChooseUnlistedOnce(queue, buildableThings, power);
			if (unlisted != null)
			{
				AIUtils.BotDebug("{0} decided to build {1}: Priority override (unlisted building once)", queue.Actor.Owner, unlisted.Name);
				return unlisted;
			}

			// PROD-BUSY cycle (USER 11-24): above the tier line cash becomes more units, i.e. one more production building per type in turn.
			// Below the planner wants, the opening and the expansion demand, which still win, and below the bank-overflow rung (its own, higher line).
			if (CycleOn)
			{
				var cycleLine = TierLine;
				if (cycleLine > 0 && CashLedger.BuildingBank(AllProducersOn, availableCash, orderedCostThisSweep) > cycleLine)
				{
					var cycled = ChooseCycledProduction(buildableThings, power, c => VotesAdmit(queue, c, RecoverySpendPolicyCA.Admission.General));
					if (cycled != null)
					{
						AIUtils.BotDebug("{0} decided to build {1}: Priority override (cycled production)", queue.Actor.Owner, cycled.Name);
						return cycled;
					}
				}
			}

			// BZ_production_scaling: banked cash held over the difficulty line while every unit
			// queue is busy (or net income per producer above the line) queues another production
			// building for the starved category — same limit and power checks as the override below.
			if (baseBuilder.ProductionScalingEnabled)
			{
				var scalingPick = ChooseProductionScalingItem(queue, buildableThings, power);
				if (scalingPick != null && Admitted(queue, scalingPick))
					return scalingPick;
			}

			// Make sure that we can spend as fast as we are earning
			if (NewProductionCashThreshold > 0 && playerResources.GetCashAndResources() > NewProductionCashThreshold)
			{
				var production = GetProducibleBuilding(baseBuilder.Info.ProductionTypes, buildableThings);

				if (production != null && (ProductionTypeLimit <= 0 || !BaseBuilderQueueEvalCA.LimitReached(playerBuildings.Count(a => a.Info.Name == production.Name), ProductionTypeLimit)))
				{
					var pick = BaseBuilderQueueEvalCA.PickOrPower(production, power, HasSufficientPowerForActor);
					if (pick != null && Admitted(queue, pick))
					{
						AIUtils.BotDebug("{0} decided to build {1}: Priority override ({2})", queue.Actor.Owner, pick.Name,
							pick == production ? "production" : "would be low power");
						TraceThresholdOverride(pick == production ? "production" : "power-swap", pick);
						return pick;
					}
				}

				TraceThresholdOverride(production == null ? "no-producible" : "type-limit-or-no-pick", production);
			}

			// Only consider building this if there is enough water inside the base perimeter and there are close enough adjacent buildings
			if (waterState == WaterCheck.EnoughWater && NewProductionCashThreshold > 0
				&& playerResources.Resources > NewProductionCashThreshold
				&& AIUtils.IsAreaAvailable<GivesBuildableArea>(world, player, world.Map, baseBuilder.Info.CheckForWaterRadius, baseBuilder.Info.WaterTerrainTypes))
			{
				var navalproduction = GetProducibleBuilding(baseBuilder.Info.NavalProductionTypes, buildableThings);
				var navalPick = BaseBuilderQueueEvalCA.PickOrPower(navalproduction, power, HasSufficientPowerForActor);
				if (navalPick != null && Admitted(queue, navalPick))
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
				if (siloPick != null && Admitted(queue, siloPick))
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
				if (radarPick != null && Admitted(queue, radarPick))
				{
					AIUtils.BotDebug("{0} decided to build {1}: Priority override ({2})", queue.Actor.Owner, radarPick.Name,
						radarPick == radar ? "radar for front" : "radar would be low power");
					return radarPick;
				}
			}

			// Build everything else
			foreach (var frac in baseBuilder.Info.BuildingFractions.OrderBy(kv => kv.Key).Shuffle(BotRng.For(player, nameof(BaseBuilderQueueManagerCA))))
			{
				var name = frac.Key;

				// S2 (FINDINGS_2026-10-12 §4): dedicated anti-air waits for fog-fair
				// enemy air intel; the rolled share then flows to the remaining
				// (anti-ground) entries of this same pass.
				if (!BaseBuilderQueueEvalCA.AirDefenceAdmits(baseBuilder.IsGatedAirDefence(name), baseBuilder.DedicatedAntiAirAllowed))
					continue;

				// BASE-07: a type that just failed its placement budget is not rolled again.
				if (NoSpaceBanned(name))
					continue;

				// BASE-12: the roll's refinery obeys the expansion safety test too (every wanting claim unsafe = held).
				if (SafetyOn && SafetyAllOn && baseBuilder.Info.RefineryTypes.Contains(name) && baseBuilder.ExpansionRefineryBlockedByAllUnsafe())
					continue;

				// BASE-04 (AUDIT_BASE_01): no ground defence from the roll before a threat is seen (the planner's front defence and demand defence stay).
				if (DefenceNeedOn && world.Map.Rules.Actors.TryGetValue(name, out var defenceCandidate)
					&& BaseFix01CA.DefenceNeedHolds(true, IsGroundDefence(defenceCandidate), DefenceThreatSeen()))
					continue;

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

				// RATIO-2X-01: a defence the 2x gate would hold anyway must not take the pick — the rolled share flows to the
				// remaining entries of this pass instead of the whole sweep being lost to a held defence. Same decision as the
				// gate in TickQueue (one measure per tick), no state of its own.
				if (MobileStaticRatioCA.Holds(RatioGate(actor, out _)))
					continue;

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
		// REF-2 P1a: the check itself is promoted to BaseBuilderQueueEvalCA.CheckDockAccess so the
		// widened fallback reuses the identical dock rule instead of a diverging copy.

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

			// REF-DEMAND-01 (row ref_demand): with friendly spacing on, a cell within RefinerySpacingCells of a friendly refinery is refused; one
			// legal only at the fallback spacing is kept aside and used only when the full spacing leaves this claim no site (gap 0, then gap 1).
			var spacingLaw = baseBuilder.RefineryLawProvider();
			var spacing = spacingLaw != null && spacingLaw.RefinerySpacingActive;
			CPos? relaxed0 = null, relaxed1 = null;
			var rejectedSpacing = 0;

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

				var spacingTier = spacing ? spacingLaw.RefinerySiteSpacingTier(cell) : RefineryDemand.SpacingAllowed;
				if (spacingTier == RefineryDemand.SpacingRefused)
				{
					rejectedSpacing++;
					continue;
				}

				// A cell fails the dock check only when every dock is unreachable; a dock that lost
				// solely to the valuable-resource rule keeps the resource signature for the tally.
				var dockOk = false;
				var dockAllResource = true;
				foreach (var off in dockOffsets)
				{
					var access = BaseBuilderQueueEvalCA.CheckDockAccess(world, resourceLayer, actorInfo, bi, cell, off, valuable);
					if (access == BaseBuilderQueueEvalCA.DockAccess.Clear)
					{
						dockOk = true;
						break;
					}

					if (access != BaseBuilderQueueEvalCA.DockAccess.OnResource)
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

				if (spacingTier == RefineryDemand.SpacingFallbackOnly)
				{
					var relaxedGap = zone0 == null ? 0 : BaseBuilderQueueEvalCA.FootprintGap(bi.Tiles(cell).ToList(), zone0, zone1);
					if (relaxedGap == 0)
						relaxed0 ??= cell;
					else if (relaxedGap == 1)
						relaxed1 ??= cell;
					else
						rejectedGap++;

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

			// REF-DEMAND-01: the full spacing left no site — the fallback spacing's nearest one (gap 0 first).
			if (relaxed0.HasValue)
				return (relaxed0.Value, zone0 == null ? -1 : 0, 0);

			if (relaxed1.HasValue)
				return (relaxed1.Value, 1, 0);

			AIUtils.BotDebug($"{player} law refinery {actorType}: no site at anchor {anchor} — {scanned} scanned, {rejectedBuild} unbuildable, {rejectedFar} too-far, {rejectedDockResource} dock-on-resource, {rejectedDockExit} dock-no-exit, {rejectedGap} gap-illegal{(spacing ? ", " + rejectedSpacing + " spacing" : "")} at tick {world.WorldTick}");
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
						actorVariant = BotRng.For(player, nameof(BaseBuilderQueueManagerCA)).Next(buildingVariantInfo.Actors.Length + 1);
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
				cells = cells.Shuffle(BotRng.For(player, nameof(BaseBuilderQueueManagerCA)));

				if (buildingVariantInfo?.Actors != null)
					actorVariant = BotRng.For(player, nameof(BaseBuilderQueueManagerCA)).Next(buildingVariantInfo.Actors.Length + 1);
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

		// TEAM-DEFEND-02: the attack picture, refreshed every TurretSupportScanTicks while a turret waits: the own or allied building with the most seen enemy armed units near it (at least
		// TurretSupportMinAttackers), the mean position of those units and the building's position. Observation only: enemies count when the bot (or an ally, shared shroud) sees them.
		int turretFocusTick = -1000;
		(bool Valid, WPos Attackers, WPos Anchor) turretFocus;

		(bool Valid, WPos Attackers, WPos Anchor) TurretSupportFocus()
		{
			var now = world.WorldTick;
			if (now - turretFocusTick < Math.Max(1, baseBuilder.Info.TurretSupportScanTicks))
				return turretFocus;

			turretFocusTick = now;
			turretFocus = default;
			var radius = WDist.FromCells(baseBuilder.Info.TurretSupportRadiusCells).Length;
			var friendly = world.ActorsHavingTrait<Building>()
				.Where(a => a.IsInWorld && !a.IsDead && (a.Owner == player || player.RelationshipWith(a.Owner) == PlayerRelationship.Ally))
				.OrderBy(a => a.ActorID).ToList();
			if (friendly.Count == 0)
				return turretFocus;

			var near = new SortedDictionary<uint, List<WPos>>();
			foreach (var enemy in world.ActorsHavingTrait<Mobile>())
			{
				if (!enemy.IsInWorld || enemy.IsDead || player.RelationshipWith(enemy.Owner) != PlayerRelationship.Enemy
					|| !enemy.CanBeViewedByPlayer(player) || !enemy.Info.HasTraitInfo<AttackBaseInfo>())
					continue;

				Actor nearest = null;
				var bestSq = (long)radius * radius + 1;
				foreach (var b in friendly)
				{
					var d = (b.CenterPosition - enemy.CenterPosition).HorizontalLengthSquared;
					if (d < bestSq)
					{
						bestSq = d;
						nearest = b;
					}
				}

				if (nearest == null)
					continue;

				if (!near.TryGetValue(nearest.ActorID, out var list))
					near[nearest.ActorID] = list = [];

				list.Add(enemy.CenterPosition);
			}

			var ids = near.Keys.ToList();
			var pick = TurretSupportCA.Focus(ids.Select(id => (id, near[id].Count)).ToList(), baseBuilder.Info.TurretSupportMinAttackers);
			if (pick < 0)
				return turretFocus;

			var centroid = TurretSupportCA.Centroid(near[ids[pick]].Select(p => ((long)p.X, (long)p.Y)).ToList());
			var anchor = friendly.First(a => a.ActorID == ids[pick]).CenterPosition;
			return turretFocus = (true, new WPos((int)centroid.X, (int)centroid.Y, 0), anchor);
		}

		// TEAM-DEFEND-02: the cell for a ready ground turret while a base is attacked. (null, false) = no attack or not a turret, the normal placement runs; (cell, false) = place there;
		// (null, true) = an attack but no legal cell inside the weapon range: hold (the READY watchdog places it at a base cell later). Legality is the engine's own placement check, so the
		// lobby build-next-to-an-ally option applies by itself: with it off only own cells are legal and the same rule picks the best of them.
		(CPos? Cell, bool Hold) ChooseSupportTurretCell(ActorInfo actorInfo, Actor producer)
		{
			if (!TurretSupportOn || !actorInfo.HasTraitInfo<AttackBaseInfo>() || !IsGroundDefence(actorInfo))
				return (null, false);

			var bi = actorInfo.TraitInfoOrDefault<BuildingInfo>();
			if (bi == null)
				return (null, false);

			var focus = TurretSupportFocus();
			if (!focus.Valid)
				return (null, false);

			var range = BotUnitProfiles.Get(world.Map.Rules, actorInfo).MaxRange.Length;
			if (range <= 0)
				return (null, false);

			// The engine's own admission gate (placement check, near-base rule with the lobby ally option, spacing gap) over the cells around the attackers; only those inside the weapon range stay.
			var legal = AdvisorLegalCells(actorInfo, true, producer, world.Map.CellContaining(focus.Attackers))
				.Where(cell => (world.Map.CenterOfCell(cell) - focus.Attackers).HorizontalLengthSquared <= (long)range * range).ToList();
			var positions = legal.Select(cell => { var c = world.Map.CenterOfCell(cell); return ((long)c.X, (long)c.Y); }).ToList();

			var index = TurretSupportCA.Pick(positions, (focus.Attackers.X, focus.Attackers.Y), (focus.Anchor.X, focus.Anchor.Y), range,
				TurretSupportCA.MinSafe(range, WDist.FromCells(baseBuilder.Info.TurretSupportMinGapCells).Length));
			return index >= 0 ? (legal[index], false) : (null, true);
		}

		// The variant an advisor-claimed placement uses: findPos's non-facing random draw, 0 otherwise
		// (a front-side facing is the advisor's to encode later; the default facing is honest today).
		int FrontBackVariant(ActorInfo actorInfo)
		{
			var variants = actorInfo.TraitInfoOrDefault<PlaceBuildingVariantsInfo>();
			return BaseBuilderQueueEvalCA.PicksRandomVariant(variants?.Actors != null,
				variants != null && variants.Facings != null)
					? BotRng.For(player, nameof(BaseBuilderQueueManagerCA)).Next(variants.Actors.Length + 1) : 0;
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

					// CRAWL-SAFETY (S3): the planner's front defence stands at the crawl frontier it was
					// wanted for — the defence advisor around the frontier first (a legal cell in buildable
					// area), then the defence annulus centred there. A null cell runs the normal failure path.
					var frontDefenceLaw = baseBuilder.RefineryLawProvider();
					if (frontDefenceLaw?.WantedFrontDefence == actorType && frontDefenceLaw.FrontDefenceCell is { } frontCell)
					{
						var frontAdvised = AdvisedDefenseCell(actorInfo, distanceToBaseIsImportant, producer, frontCell);
						if (frontAdvised != null)
							return (frontAdvised, frontCell, 0);

						return findPos(actorType, distanceToBaseIsImportant, producer, frontCell, frontCell,
							baseBuilder.Info.MinimumDefenseRadius, baseBuilder.Info.MaximumDefenseRadius, defenseGap: true);
					}

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
						var claimsOffered = false;
						if (demand?.ReservedClaim is { } reserved
							&& law.RefineryAnchorReserved(reserved.Anchor, demand))
							claim = reserved;

						// REF-2 P1a: the preferred stage is a bounded offer ladder per sweep — each
						// failed offer commits-or-parks (the park is the only retry bound) and the
						// next offer in the claim order is tried; parked anchors simply leave the
						// order until their cooldown ends.
						for (var attempt = 0; attempt < baseBuilder.Info.RefineryClaimAttemptsPerSweep; attempt++)
						{
							claim ??= law.NextRefineryClaim(near);
							if (claim is not { } c)
								break;

							claimsOffered = true;
							var placed = LawRefineryPlacement(actorType, distanceToBaseIsImportant, producer,
								c.Anchor, c.ResourceCells, law.ExpansionTargetClaimRadius);

							// REPAIR-B3: a legal selected site commits the claim AT THE SITE (the provider
							// publishes pending coverage for the whole in-radius set); an offered claim
							// with no site frees the demand's whole reserved set at once (lead ruling
							// 2026-10-10 — the fail cooldown is the only retry bound) and parks the anchor
							// for that owner instead of holding the set hostage until UntilTick.
							if (BaseBuilderQueueEvalCA.CommitRefineryClaimOrPark(law, c, placed.Location, demand))
							{
#if CAMEO_DEV_LOGGING
								Log.Write("debug", $"AI ({player.ClientIndex}): REF-1 refinery {actorType} at {placed.Location.Value} claims anchor {c.Anchor} field {c.FieldId} tier {c.Tier} gap {placed.Gap} at tick {world.WorldTick}");
#endif
								refineryClaimed = true;
								if (requestRef != null)
									baseBuilder.RequestedRefineries.Remove(requestRef);
								return (placed.Location, c.Anchor, placed.Variant);
							}

							claim = null;
						}

						// REF-2 P1a: preferred offers exhausted — the widened Stage-2 scans the engine's
						// reach domain under a per-sweep cell budget; a Site verdict carries an
						// established global (nearest-resource, X, Y) ordering and commits coverage for
						// exactly the anchors that site's serve radius covers — never an offered anchor.
						var fallback = law.PollRefineryFallbackSite(producer, actorType, distanceToBaseIsImportant, out var fallbackSite);
						if (fallback == RefineryFallbackOutcome.Site)
						{
							law.RefinerySiteCommitted(fallbackSite.Value);
#if CAMEO_DEV_LOGGING
							Log.Write("debug", $"AI ({player.ClientIndex}): REF-2 P1a refinery {actorType} at widened site {fallbackSite.Value} at tick {world.WorldTick}");
#endif
							refineryClaimed = true;
							if (requestRef != null)
								baseBuilder.RequestedRefineries.Remove(requestRef);
							return (fallbackSite, fallbackSite, 0);
						}

						// Maintainer ruling 2026-10-16: the ONLY permitted cancel of a completed
						// refinery rides this flag — the fallback pass provably enumerated the
						// total domain (every provider square, or the whole map when no
						// base-distance rejection applies) at the unrestricted tier and found
						// no legal cell. Every other verdict keeps the item held and retrying.
						if (fallback == RefineryFallbackOutcome.Exhausted)
							refineryExhausted = true;

						if (!claimsOffered && fallback != RefineryFallbackOutcome.Deferred)
						{
							// FIX-RA-REFINERY diagnostics: the other law-null reason — nothing claimed
							// this sweep. Bounded: one line per failed placement attempt.
							AIUtils.BotDebug($"{player} law refinery {actorType}: no claim available at tick {world.WorldTick}");
						}

						refineryHoldReason = BaseBuilderQueueEvalCA.RefineryFallbackHoldReason(fallback, claimsOffered);
						return (null, null, 0);
					}

					// Cameo (AI_ARCHITECTURE §12.13, EX-2): the planner's field is in reach and unclaimed, so the refinery goes
					// there, close enough to count as claiming it. A refinery the MCV module requested keeps priority.
					// BASE-12: with the all-claims row the site follows the first SAFE wanting claim, not the head claim.
					var claimer = requestRef == null ? (SafetyOn && SafetyAllOn ? baseBuilder.ExpansionWantsRefinerySafe() : baseBuilder.ExpansionWantsRefinery()) : null;
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
#if CAMEO_DEV_LOGGING
							Log.Write("debug", $"AI ({player.ClientIndex}): EX-2 refinery {actorType} at {claim.Location.Value} claims field {field} at tick {world.WorldTick}");
#endif
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
							resourcesShouldCheck = nearbyResources.Shuffle(BotRng.For(player, nameof(BaseBuilderQueueManagerCA))).Take(baseBuilder.Info.MaxResourceCellsToCheck);
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

						// TF-1: the bounded path anchors the search at the frontier cell nearest the
						// aim — an admitted link must land forward; without a published anchor (legacy,
						// switch-off) the yard centre stays the search centre as before.
						// crawl_advance_only (CRAWL-LONG-01): the search is anchored on the own frontier tile nearest the aim (not on a random yard, whose radius-50 ring capped a long crawl and flipped
						// between yards) and its best cell must strictly reduce the frontier-to-aim distance. No forward cell: the link is placed at a base cell, the planner parks the target, and
						// neither the random-resource nor the toward-the-enemy fallback below runs (they put plants "all over the place").
						var advanceOnly = crawlLaw != null && crawlLaw.CrawlAdvanceOnly;
						var frontierTiles = advanceOnly ? OwnBuildableAreaTiles(crawlLaw.CrawlAllyAnchor) : null;
						var anchor = advanceOnly && BaseBuilderQueueEvalCA.NearestTile(frontierTiles, crawlAim.Value) is { } frontierTile
							? frontierTile
							: crawlLaw?.CrawlFrontierAnchor ?? baseCenter;
						var toward = findPos(actorType, distanceToBaseIsImportant, producer, anchor, crawlAim.Value,
							advanceOnly && frontierTiles.Count > 0 ? 0 : baseBuilder.Info.MinBaseRadius, baseBuilder.Info.BaseCrawlRadius, bypassAdvisor: crawlLaw != null);
						if (advanceOnly && !(toward.Location != null && actorInfo.HasTraitInfo<GivesBuildableAreaInfo>()
							&& BaseBuilderQueueEvalCA.AdvancesFrontier(actorInfo.TraitInfo<BuildingInfo>().Tiles(toward.Location.Value), frontierTiles, crawlAim.Value)))
						{
							if (crawlLaw.WantedLinkBuilding == actorType)
								crawlLaw.CrawlLinkNoAdvance();
#if CAMEO_DEV_LOGGING
							Log.Write("debug", $"AI ({player.ClientIndex}): CRAWL-ADVANCE {actorType} has no forward cell toward {crawlAim.Value} (best {(toward.Location.HasValue ? toward.Location.Value.ToString() : "none")}, frontier {anchor}) at tick {world.WorldTick}: placed at a base cell");
#endif
							return findPos(actorType, distanceToBaseIsImportant, producer, baseCenter, baseCenter, baseBuilder.Info.MinBaseRadius,
								distanceToBaseIsImportant ? baseBuilder.Info.MaxBaseRadius : world.Map.Grid.MaximumTileSearchRange);
						}

						if (toward.Location != null)
						{
#if CAMEO_DEV_LOGGING
							Log.Write("debug", $"AI ({player.ClientIndex}): EX-1 BaseCrawl {actorType} at {toward.Location.Value} toward field {crawlAim.Value} at tick {world.WorldTick}");
#endif
							return toward;
						}
					}

					// Try and place the refinery near a resource field
					if (resourceLayer != null)
					{
						var nearbyResources = world.Map.FindTilesInAnnulus(baseCenter, baseBuilder.Info.MinBaseRadius, baseBuilder.Info.BaseCrawlRadius)
							.Where(a => resourceLayer.GetResource(a).Type != null)
							.Shuffle(BotRng.For(player, nameof(BaseBuilderQueueManagerCA))).Take(baseBuilder.Info.MaxResourceCellsToCheck);

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

		// crawl_advance_only: the footprint tiles of our buildable-area buildings, the frontier the crawl closes on the aim from (the planner measures the same set).
		// crawl_ally_anchor (light team crawl): with the row on the visible allied buildable-area buildings join the frontier (the engine lets us build next to them with allybuild on).
		List<CPos> OwnBuildableAreaTiles(bool withAllied = false)
		{
			var tiles = new List<CPos>();
			if (playerBuildings == null)
				return tiles;

			foreach (var b in playerBuildings)
			{
				if (b.IsDead || !b.Info.HasTraitInfo<GivesBuildableAreaInfo>())
					continue;

				if (b.Info.TraitInfoOrDefault<BuildingInfo>() is { } info)
					tiles.AddRange(info.Tiles(b.Location));
				else
					tiles.Add(b.Location);
			}

			if (withAllied)
			{
				foreach (var b in world.ActorsHavingTrait<GivesBuildableArea>())
				{
					// The engine skips an area whose provider allows placement only from itself (AnyGivesBuildableArea: OnlyAllowPlacementFromSelf).
					if (b.IsDead || !b.IsInWorld || b.Owner == player || b.Owner.RelationshipWith(player) != PlayerRelationship.Ally
						|| !b.CanBeViewedByPlayer(player) || b.TraitsImplementing<GivesBuildableArea>().All(g => g.Info.OnlyAllowPlacementFromSelf))
						continue;

					if (b.Info.TraitInfoOrDefault<BuildingInfo>() is { } info)
						tiles.AddRange(info.Tiles(b.Location));
					else
						tiles.Add(b.Location);
				}
			}

			return tiles;
		}

		void SetBuildingInterval(string name)
		{
			if (baseBuilder.Info.BuildingIntervals == null || !baseBuilder.Info.BuildingIntervals.ContainsKey(name))
				return;

			activeBuildingIntervals[name] = KnobTicks(baseBuilder.Info.BuildingIntervals[name] * buildingIntervalModifier / 100, name);
		}
	}
}
