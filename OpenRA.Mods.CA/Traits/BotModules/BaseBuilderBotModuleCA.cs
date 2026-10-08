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
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.AS.Traits;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Pathfinder;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[TraitLocation(SystemActors.Player)]
	[Desc("Manages AI base construction.")]
	public class BaseBuilderBotModuleCAInfo : ConditionalTraitInfo, NotBefore<ResourceMapBotModuleInfo>, NotBefore<IResourceLayerInfo>
	{
		[Desc("Tells the AI what building types are considered construction yards.")]
		public readonly FrozenSet<string> ConstructionYardTypes = FrozenSet<string>.Empty;

		[Desc("Tells the AI what building types are considered vehicle production facilities.")]
		public readonly FrozenSet<string> VehiclesFactoryTypes = FrozenSet<string>.Empty;

		[Desc("Tells the AI what building types are considered refineries.")]
		public readonly FrozenSet<string> RefineryTypes = FrozenSet<string>.Empty;

		[Desc("Tells the AI what building types are considered power plants.")]
		public readonly FrozenSet<string> PowerTypes = FrozenSet<string>.Empty;

		[Desc("Tells the AI what building types are considered infantry production facilities.")]
		public readonly FrozenSet<string> BarracksTypes = FrozenSet<string>.Empty;

		[Desc("Factions that may prioritize their first barracks before their first refinery when enabled by BotLimits.")]
		public readonly FrozenSet<string> BarracksBeforeRefineryFactions = FrozenSet<string>.Empty;

		[Desc("Tells the AI what building types are considered anti-air defenses.")]
		public readonly FrozenSet<string> AntiAirTypes = FrozenSet<string>.Empty;

		[Desc("Tells the AI what building types are considered defenses.")]
		public readonly FrozenSet<string> DefenseTypes = FrozenSet<string>.Empty;

		[Desc("Tells the AI what building types are considered production facilities.")]
		public readonly FrozenSet<string> ProductionTypes = FrozenSet<string>.Empty;

		[Desc("Tells the AI what building types are considered naval production facilities.")]
		public readonly FrozenSet<string> NavalProductionTypes = FrozenSet<string>.Empty;

		[Desc("Tells the AI what building types are considered silos (resource storage).")]
		public readonly FrozenSet<string> SiloTypes = FrozenSet<string>.Empty;

		[Desc("Tells the AI what building types are considered fragile.")]
		public readonly FrozenSet<string> FragileTypes = FrozenSet<string>.Empty;

		[Desc("Production queues AI uses for buildings.")]
		public readonly FrozenSet<string> BuildingQueues = new HashSet<string> { "Building" }.ToFrozenSet();

		[Desc("Production queues AI uses for defenses.")]
		public readonly FrozenSet<string> DefenseQueues = new HashSet<string> { "Defense" }.ToFrozenSet();

		[Desc("Minimum distance in cells from center of the base when checking for building placement.")]
		public readonly int MinBaseRadius = 2;

		[Desc("Radius in cells around the center of the base to expand.")]
		public readonly int MaxBaseRadius = 20;

		[Desc("Maximum number of extra refineries to build (in addition to RefineriesPerBase per construction yard).")]
		public readonly int MaxExtraRefineries = 1;

		[Desc("Number of refineries per construction yard.")]
		public readonly int RefineriesPerBase = 2;

		[Desc("Minimum excess power the AI should try to maintain.")]
		public readonly int MinimumExcessPower = 0;

		[Desc("The targeted excess power the AI tries to maintain cannot rise above this.")]
		public readonly int MaximumExcessPower = 0;

		[Desc("Increase maintained excess power by this amount for every ExcessPowerIncreaseThreshold of base buildings.")]
		public readonly int ExcessPowerIncrement = 0;

		[Desc("Increase maintained excess power by ExcessPowerIncrement for every N base buildings.")]
		public readonly int ExcessPowerIncreaseThreshold = 1;

		[Desc("Number of refineries to build before building any production building.")]
		public readonly int InititalMinimumRefineryCount = 1;

		[Desc("Number of refineries to build additionally after building any production building.")]
		public readonly int AdditionalMinimumRefineryCount = 1;

		[Desc("Additional delay (in ticks) between structure production checks when there is no active production.",
			"StructureProductionRandomBonusDelay is added to this.")]
		public readonly int StructureProductionInactiveDelay = 125;

		[Desc("Additional delay (in ticks) added between structure production checks when actively building things.",
			"Note: this should be at least as large as the typical order latency to avoid duplicated build choices.")]
		public readonly int StructureProductionActiveDelay = 25;

		[Desc("A random delay (in ticks) of up to this is added to active/inactive production delays.")]
		public readonly int StructureProductionRandomBonusDelay = 10;

		[Desc("Delay (in ticks) until retrying to build structure after the last 3 consecutive attempts failed.")]
		public readonly int StructureProductionResumeDelay = 1500;

		[Desc("After how many failed attempts to place a structure should AI give up and wait",
			"for StructureProductionResumeDelay before retrying.")]
		public readonly int MaximumFailedPlacementAttempts = 3;

		[Desc("How many randomly chosen cells with resources to check when deciding refinery placement.")]
		public readonly int MaxResourceCellsToCheck = 3;

		[Desc("Cameo (AI_ARCHITECTURE §12.13, EX-2): while an expansion target provider is mounted, refinery placement",
			"samples only resource cells of fields no own refinery serves (no refinery within this many cells of the",
			"field's resource centre), so refineries spread over new fields instead of stacking on the same home field.",
			"Classic mounts no provider and keeps the old farthest-from-refinery ordering.")]
		public readonly int RefineryUnservedRadiusCells = 10;

		[Desc("Cameo (AI_ARCHITECTURE §12.20): when an active IBotPlacementAdvisor is mounted, findPos hands it",
			"this many placeable candidate cells to re-rank instead of keeping the first. With no advisor the",
			"placement scan is byte-identical to upstream.")]
		public readonly int PlacementAdvisorCandidates = 24;

		[Desc("Delay (in ticks) until rechecking for new BaseProviders.")]
		public readonly int CheckForNewBasesDelay = 1500;

		[Desc("Chance that the AI will place the defenses in the direction of the closest enemy building.")]
		public readonly int PlaceDefenseTowardsEnemyChance = 100;

		[Desc("Chance that the AI will place buildings to crawl toward resource patches.")]
		public readonly int BaseCrawlChance = 50;

		[Desc("Maximum range at which to basecrawl.")]
		public readonly int BaseCrawlRadius = 50;

		[Desc("Structures cheaper than this will be used to basecrawl.")]
		public readonly int BaseCrawlCostThreshold = 1000;

		[Desc("Minimum range at which to build defensive structures near a combat hotspot.")]
		public readonly int MinimumDefenseRadius = 5;

		[Desc("Maximum range at which to build defensive structures near a combat hotspot.")]
		public readonly int MaximumDefenseRadius = 20;

		[Desc("Try to build another production building if there is too much cash.")]
		public readonly int NewProductionCashThreshold = 10000;

		[Desc("Only queue construction of a new building when above this requirement.")]
		public readonly int BuildingProductionMinCashRequirement = 1750;

		[Desc("Only queue construction of a new defense when above this requirement.")]
		public readonly int DefenseProductionMinCashRequirement = 2250;

		[Desc("Radius in cells around a factory scanned for rally points by the AI.")]
		public readonly int RallyPointScanRadius = 8;

		[Desc("Radius in cells around each building with ProvideBuildableArea",
			"to check for a 3x3 area of water where naval structures can be built.",
			"Should match maximum adjacency of naval structures.")]
		public readonly int CheckForWaterRadius = 8;

		[Desc("Terrain types which are considered water for base building purposes.")]
		public readonly FrozenSet<string> WaterTerrainTypes = new HashSet<string> { "Water" }.ToFrozenSet();

		[Desc("What buildings to the AI should build.", "What integer percentage of the total base must be this type of building.")]
		public readonly FrozenDictionary<string, int> BuildingFractions = null;

		[Desc("What buildings should the AI have a maximum limit to build.")]
		public readonly FrozenDictionary<string, int> BuildingLimits = null;

		[Desc("When should the AI start building specific buildings.")]
		public readonly FrozenDictionary<string, int> BuildingDelays = null;

		[Desc("Minimum duration between building specific buildings.")]
		public readonly FrozenDictionary<string, int> BuildingIntervals = null;

		[Desc("Delay (in ticks) between reassigning rally points.")]
		public readonly int AssignRallyPointsInterval = 100;

		[Desc("Delay (in ticks) for finding a good resource to place a refinery next to.")]
		public readonly int CheckBestResourceLocationInterval = 151;

		[Desc("Interval (in ticks) between checking whether to sell a redundant refinery. Set to -1 to disable.")]
		public readonly int SellRefineryInterval = 5000;

		[Desc("Distance (in cells) for refineries finding redundant refineries.")]
		public readonly int SellRefineryTooCloseCellDistance = 6;

		[Desc("Maximum distance (in cells) from resources before refineries are eligible to be sold.")]
		public readonly int SellRefineryNoResourceDistance = 12;

		[Desc("Maximum refinery count per area. Area size is defined in " + nameof(ResourceMapBotModule) + ".")]
		public readonly int MaxRefineryPerIndice = 2;

		[Desc($"AI will move mcv when those numbers of refinery <= productions + tech - {nameof(ExpansionTolerate)}.")]
		public readonly ImmutableArray<int> ExpansionTolerate = [0, 1];

		[Desc($"AI will move the only mcv when those numbers of refinery <= productions + tech - {nameof(ForceExpansionTolerate)}.")]
		public readonly ImmutableArray<int> ForceExpansionTolerate = [2, 3];

		[Desc("Decrease the expansion tolerate by Cash / this. Used to prevent AI from expanding when it has enough cash.")]
		public readonly int PerExpansionTolerateOnCash = 12000;

		[Desc("Enemy building target types I can ignore construction distance from.")]
		public readonly BitSet<TargetableType> IgnoredEnemyBuildingTargetTypes = default(BitSet<TargetableType>);

		[Desc("Unit target types I should not count when scanning for sell condition .")]
		public readonly BitSet<TargetableType> IgnoredUnitTargetTypes = default(BitSet<TargetableType>);

		[Desc("Radius in cells around building being considered for sale to scan for units")]
		public readonly int SellScanRadius = 8;

		[Desc("ECON-A (SPEC_2026-10-05_econ_logistics Part A): while an expansion MCV drives to its committed",
			"deploy target, pre-build the outpost's refinery (and the strongest affordable defence) so each",
			"reaches Ready ~= the ETA, hold it at the head of its queue, and place it the tick the conyard lands.",
			"Armed by increment switch BT_expansion_prebuild; false keeps today's behaviour bit-identical.")]
		public readonly bool UseExpansionPrebuild = false;

		[ConsumedConditionReference]
		[Desc("Increment switches on this shared instance (UseExpansionPrebuild) take effect only while this",
			"condition is true (e.g. genericbot), so a shared module can be A/B-switched without changing the",
			"classic reference. Null = switches apply to every owner.")]
		public readonly BooleanExpression SwitchCondition = null;

		[Desc("ECON-A: the defence pre-build is skipped while it would hold a Ready item on the bot's only",
			"building-producer queue, unless the deploy ETA is already this close.")]
		public readonly int ExpansionHoldSlack = 500;

		[Desc("ECON-A: a demand expires this many ticks after its MCV stops having a live activity",
			"(stuck, redirected, or dead — the spec's ~100-tick lapse window).")]
		public readonly int ExpansionDemandIdleTicks = 100;

		[Desc("ECON-A: ticks between recomputing a demand's ETA from the MCV's live path (it may detour).")]
		public readonly int ExpansionEtaIntervalTicks = 25;

		[Desc("ECON-A: flat tick allowance for the deploy transform itself (turn-to-facing + placement).",
			"TransformsInfo exposes no duration field, so this is the conservative constant.")]
		public readonly int ExpansionDeployTicks = 25;

		[Desc("ECON-A: projected income for the defence pick's affordability check is measured over the",
			"last window of this many ticks (Earned delta / window length, integers only).")]
		public readonly int ExpansionIncomeWindowTicks = 500;

		public override object Create(ActorInitializer init) { return new BaseBuilderBotModuleCA(init.Self, this); }
	}

	public class BaseBuilderBotModuleCA : ConditionalTrait<BaseBuilderBotModuleCAInfo>, IGameSaveTraitData,
		IBotTick, IBotPositionsUpdated, IBotRespondToAttack, IBotRequestPauseUnitProduction, IBotSuggestRefineryProduction, INotifyActorDisposing
	{
		public CPos GetRandomBaseCenter()
		{
			var randomConstructionYard = ConstructionYardBuildings.Actors.Where(a => !a.IsDead)
				.RandomOrDefault(world.LocalRandom);

			return randomConstructionYard?.Location ?? initialBaseCenter;
		}

		// Resolves the exact construction yard actor type that this building's Prerequisites
		// (after inheritance flattening) requires, or null if it doesn't require one specific
		// construction yard type (e.g. faction-agnostic shared buildings like ra1_powerplant
		// which use a generic "~rafact" token satisfied by any RA1 construction yard).
		public string GetRequiredConstructionYardType(ActorInfo actorInfo)
		{
			var bi = actorInfo.TraitInfoOrDefault<BuildableInfo>();
			if (bi == null)
				return null;

			foreach (var prereq in bi.Prerequisites)
			{
				var name = prereq.Replace("~", string.Empty).Replace("!", string.Empty);
				if (Info.ConstructionYardTypes.Contains(name))
					return name;
			}

			return null;
		}

		// Anchor placement/expansion checks on a construction yard belonging to the same
		// faction as the building being placed, rather than a random construction yard from
		// any faction. This matters when the player owns construction yards from multiple
		// factions at once (e.g. a stray enemy MCV of a different faction deployed into an
		// existing base) - otherwise a crowded main base can starve a smaller secondary base
		// of the same faction from ever finding room to build, since GetRandomBaseCenter()
		// might repeatedly anchor searches on the wrong (unrelated) faction's construction yard.
		public CPos GetBaseCenterForActor(ActorInfo actorInfo)
		{
			var conyardType = GetRequiredConstructionYardType(actorInfo);
			if (conyardType != null)
			{
				var matchingConstructionYard = ConstructionYardBuildings.Actors
					.Where(a => !a.IsDead && a.Info.Name == conyardType)
					.RandomOrDefault(world.LocalRandom);

				if (matchingConstructionYard != null)
					return matchingConstructionYard.Location;
			}

			return GetRandomBaseCenter();
		}

		public CPos GetDefenseBaseCenter()
		{
			var defenceConstructionYard = DefenseCenter != null ? ConstructionYardBuildings.Actors.OrderBy(a => (DefenseCenter.Value - a.Location).LengthSquared)
				.FirstOrDefault(a => !a.IsDead) : null;

			return defenceConstructionYard?.Location ?? GetRandomBaseCenter();
		}

		public CPos? DefenseCenter { get; private set; }

		/// <Summary> Actor, ActorCount </Summary>
		public Dictionary<string, int> BuildingsBeingProduced = [];
		public IBotBaseExpansion[] BaseExpansionModules;
		public ResourceMapBotModule ResourceMapModule;
		public Actor RelocationHoldConyard { get; set; }

		readonly World world;
		readonly Player player;
		PlayerResources playerResources;
		IResourceLayer resourceLayer;
		IPathFinder pathFinder;
		IBotPositionsUpdated[] positionsUpdatedModules;
		IBotExpansionTargetProvider[] expansionTargetProviders;
		CPos initialBaseCenter;
		public CPos? ResourceConyardCenter;

		// Cameo (AI_ARCHITECTURE §12.13, EX-1): the field an expansion planner wants the base to walk toward, if any.
		public CPos? ExpansionTarget()
		{
			if (expansionTargetProviders == null)
				return null;

			foreach (var provider in expansionTargetProviders)
			{
				var target = provider.ExpansionTarget;
				if (target != null)
					return target;
			}

			return null;
		}

		// Cameo (§12.13, EX-2): a provider whose target field is in reach and unclaimed wants a refinery there, so the
		// refinery count is not adequate yet, whatever the fixed optimum says: every field in reach gets one.
		public IBotExpansionTargetProvider ExpansionWantsRefinery()
		{
			if (expansionTargetProviders == null)
				return null;

			foreach (var provider in expansionTargetProviders)
				if (provider.WantsRefineryAtExpansionTarget && (provider.ExpansionTarget != null || provider.RefineryLawActive))
					return provider;

			return null;
		}

		// Cameo (§12.13, EX-2): an expansion planner is mounted at all (genericbot). Classic shares this module but
		// mounts no provider, so refinery placement keyed on this stays bit-identical there.
		public bool HasExpansionGuidance => expansionTargetProviders is { Length: > 0 };

		// Cameo (§12.24, FE-1): the provider enforcing one refinery per anchor, if its switch is on. Null = every refinery
		// rule below is the old one (classic mounts no provider; genericbot with the switch off publishes false).
		public IBotExpansionTargetProvider RefineryLawProvider()
		{
			if (expansionTargetProviders == null)
				return null;

			foreach (var provider in expansionTargetProviders)
				if (provider.RefineryLawActive)
					return provider;

			return null;
		}

		/// <summary>
		/// §12.13 EX-2: keep only the resource cells whose field no own refinery serves — a field counts as served
		/// when a refinery stands within <paramref name="servedRadiusCells"/> of the field's resource centre
		/// (<paramref name="fieldCenterOf"/> maps a cell to its index centre, e.g. ResourceMapBotModule's).
		/// Cell-to-refinery distance alone is wrong: a big field's far edge is still the same field. Own-actor
		/// cells only, so fog-honest. Falls back to the full list when every field in reach is already served.
		/// </summary>
		public static IEnumerable<CPos> PreferUnservedResourceCells(IEnumerable<CPos> candidates,
			Func<CPos, CPos> fieldCenterOf, IReadOnlyCollection<CPos> ownRefineryCells, int servedRadiusCells)
		{
			var list = candidates as IReadOnlyList<CPos> ?? candidates.ToList();
			if (ownRefineryCells.Count == 0)
				return list;

			var radiusSquared = (long)servedRadiusCells * servedRadiusCells;
			var unserved = list.Where(c =>
			{
				var center = fieldCenterOf(c);
				return ownRefineryCells.All(r => (r - center).LengthSquared > radiusSquared);
			}).ToList();
			return unserved.Count > 0 ? unserved : list;
		}

		public Dictionary<Actor, (CPos ConyardLoc, CPos ResourceLoc)> RequestedRefineries = [];

		// ECON-A (SPEC_2026-10-05_econ_logistics Part A): the per-MCV expansion demand metadata that
		// rides alongside the RequestedRefineries record — ETA, defence pick, queue bindings, expiry.
		// Keyed on the same actor and re-keyed with it when a Transform replaces the actor mid-journey.
		public readonly Dictionary<Actor, ExpansionDemand> ExpansionDemands = [];

		// SwitchCondition result; true when no condition is configured (F2 pattern — @generic is shared
		// by genericbot AND classicbot, so the increment switch must not leak into the classic reference).
		bool switchesActive;
		public bool ExpansionPrebuildEnabled => Info.UseExpansionPrebuild && switchesActive;

		// ECON-A-FIX (R4): the lease owner this module claims under — the nameof convention every
		// LC1 holder uses, so a preempt notifies this trait by type name.
		const string LeaseOwner = nameof(BaseBuilderBotModuleCA);

		// ECON-A-FIX (R5): ticks an issued production order may take to land in the producer's queue —
		// the same grace VerifyDemandBinding gives a bound item before it counts as never-queued.
		const int OrderGraceTicks = 30;

		// ECON-A-FIX (R4): cells of slack around the committed deploy cell that still count as "aiming
		// there" — Move re-maps a blocked target cell by up to its ~10-cell nearest-moveable search, so
		// the committed destination can legitimately drift a little from the posted ConyardLoc.
		const int ExpansionJourneySlackCells = 12;

		// ECON-A-FIX (R5): every StartProduction order this module's queues issued, keyed by producer —
		// an order lands in the queue a tick or two after issue, and the (producer, item-name)
		// exclusivity contract must see it while it is still in flight. Entries age out after
		// OrderGraceTicks (VerifyDemandBinding's window).
		readonly Dictionary<uint, List<(string Item, int Tick)>> producerOrdersInFlight = new();

		/// <summary>Record a just-issued StartProduction order so later picks see it before it lands.</summary>
		public void RecordProducerOrder(Actor producer, string item)
		{
			var now = world.WorldTick;
			if (!producerOrdersInFlight.TryGetValue(producer.ActorID, out var log))
				producerOrdersInFlight[producer.ActorID] = log = new List<(string Item, int Tick)>(2);

			log.RemoveAll(e => now - e.Tick > OrderGraceTicks);
			log.Add((item, now));
		}

		/// <summary>
		/// ECON-A-FIX (R5): the producer holds this item name — already queued on any of its queues, or
		/// ordered by us within the order-latency grace window (still in flight). CancelProduction
		/// resolves the LAST same-name item on a queue, so a demand binding is exclusive to exactly
		/// this absence: a same-name duplicate would refund or free the wrong item.
		/// </summary>
		public bool ProducerHoldsItem(Actor producer, string item, int now)
		{
			if (producer == null || producer.Disposed || producer.IsDead)
				return false;

			if (producer.TraitsImplementing<ProductionQueue>().Any(q => q.AllQueued().Any(i => i.Item == item)))
				return true;

			return producerOrdersInFlight.TryGetValue(producer.ActorID, out var log)
				&& log.Any(e => e.Item == item && now - e.Tick <= OrderGraceTicks);
		}

		/// <summary>
		/// ECON-A-FIX (R6): the anchor this demand's refinery would claim — evaluated as if the committed
		/// yard's footprint already stood, so an outpost beyond today's frontier can still reserve (the
		/// reach-only quota must not gate the field its own yard will open). Cached per tick: every
		/// queue manager asks the same question of the same demand.
		/// </summary>
		public RefineryAnchorClaim? DemandClaimFor(ExpansionDemand demand)
		{
			var law = RefineryLawProvider();
			if (law == null || demand == null)
				return null;

			var now = world.WorldTick;
			if (demand.ClaimCacheTick == now)
				return demand.CachedClaim;

			demand.ClaimCacheTick = now;
			return demand.CachedClaim = law.DemandRefineryClaim(demand.ConyardLoc, demand.YardFootprint);
		}

		/// <summary>
		/// ECON-A-FIX (R6): the footprint the traveller leaves behind when it deploys — the committed
		/// yard's BuildingInfo tiles around the deploy cell. An MCV's yard type resolves through its
		/// Transforms trait (the same enabled-transform resolver the MCV manager uses); a relocating
		/// conyard contributes its own.
		/// </summary>
		IReadOnlyCollection<CPos> DeployFootprint(Actor expandActor, CPos loc)
		{
			var info = expandActor.Info;
			if (info.TraitInfoOrDefault<BuildingInfo>() == null)
			{
				var into = expandActor.TraitsImplementing<Transforms>()
					.FirstOrDefault(t => !t.IsTraitDisabled && !t.IsTraitPaused)?.Info.IntoActor;
				info = into != null && world.Map.Rules.Actors.TryGetValue(into, out var yard) ? yard : null;
			}

			var bi = info?.TraitInfoOrDefault<BuildingInfo>();
			return bi != null ? bi.Tiles(loc).ToList() : (IReadOnlyCollection<CPos>)new[] { loc };
		}

		// ECON-A: projected income — the sliding window over Earned (deliveries only; integers).
		int incomeWindowStartTick = -1;
		int incomeWindowStartEarned;
		int projectedIncomePerTick;

		readonly Stack<TraitPair<RallyPoint>> rallyPoints = [];
		int assignRallyPointsTicks;
		int checkBestResourceLocationTicks;
		int sellRefineryTick;
		bool firstTick = true;
		bool openingBarracksPriorityCompleted;
		bool openingStartingCashCaptured;
		bool openingBarracksCostCommitted;
		bool openingRefineryCostCommitted;
		int openingStartingCash;
		int openingPowerCommittedCost;
		int openingBarracksCommittedCost;
		int openingRefineryCommittedCost;
		int openingDefenseCommittedCost;

		readonly BaseBuilderQueueManagerCA[] builders;
		int currentBuilderIndex = 0;

		public readonly ActorIndex.OwnerAndNamesAndTrait<RefineryInfo> RefineryBuildings;
		readonly ActorIndex.OwnerAndNamesAndTrait<BuildingInfo> powerBuildings;
		public readonly ActorIndex.OwnerAndNamesAndTrait<BuildingInfo> ConstructionYardBuildings;
		readonly ActorIndex.OwnerAndNamesAndTrait<BuildingInfo> barracksBuildings;
		readonly ActorIndex.OwnerAndNamesAndTrait<BuildingInfo> factoryBuildings;
		public readonly ActorIndex.OwnerAndNamesAndTrait<BuildingInfo> ProductionBuildings;

		BotLimits botLimits;
		int refineryLimit;
		IBotScaleTargets[] scaleTargets;
		IBotBuildOrderKnobs[] buildOrderKnobs;

		public PowerManager PlayerPower { get; private set; }
		public int ExcessPower { get; private set; }

		public BaseBuilderBotModuleCA(Actor self, BaseBuilderBotModuleCAInfo info)
			: base(info)
		{
			world = self.World;
			player = self.Owner;
			builders = new BaseBuilderQueueManagerCA[info.BuildingQueues.Count + info.DefenseQueues.Count];
			RefineryBuildings = new ActorIndex.OwnerAndNamesAndTrait<RefineryInfo>(world, info.RefineryTypes, player);
			powerBuildings = new ActorIndex.OwnerAndNamesAndTrait<BuildingInfo>(world, info.PowerTypes, player);
			ConstructionYardBuildings = new ActorIndex.OwnerAndNamesAndTrait<BuildingInfo>(world, info.ConstructionYardTypes, player);
			barracksBuildings = new ActorIndex.OwnerAndNamesAndTrait<BuildingInfo>(world, info.BarracksTypes, player);
			factoryBuildings = new ActorIndex.OwnerAndNamesAndTrait<BuildingInfo>(world, info.VehiclesFactoryTypes, player);
			ProductionBuildings = new ActorIndex.OwnerAndNamesAndTrait<BuildingInfo>(world, info.ProductionTypes, player);
			switchesActive = info.SwitchCondition == null;
		}

		public override IEnumerable<VariableObserver> GetVariableObservers()
		{
			foreach (var observer in base.GetVariableObservers())
				yield return observer;

			if (Info.SwitchCondition != null)
				yield return new VariableObserver(SwitchConditionChanged, Info.SwitchCondition.Variables);
		}

		void SwitchConditionChanged(Actor self, IReadOnlyDictionary<string, int> conditions)
		{
			switchesActive = Info.SwitchCondition.Evaluate(conditions);
		}

		// Use for proactive targeting.
		public bool IsEnemyGroundUnit(Actor a)
		{
			if (a == null || a.IsDead || player.RelationshipWith(a.Owner) != PlayerRelationship.Enemy || a.Info.HasTraitInfo<HuskInfo>() || a.Info.HasTraitInfo<AircraftInfo>() || a.Info.HasTraitInfo<CarrierSlaveInfo>())
				return false;

			var targetTypes = a.GetEnabledTargetTypes();
			return !targetTypes.IsEmpty && !targetTypes.Overlaps(Info.IgnoredUnitTargetTypes);
		}

		public bool IsAllyGroundUnit(Actor a)
		{
			if (a == null || a.IsDead || player.RelationshipWith(a.Owner) != PlayerRelationship.Ally || a.Info.HasTraitInfo<HuskInfo>() || a.Info.HasTraitInfo<AircraftInfo>() || a.Info.HasTraitInfo<CarrierSlaveInfo>())
				return false;

			var targetTypes = a.GetEnabledTargetTypes();
			return !targetTypes.IsEmpty && !targetTypes.Overlaps(Info.IgnoredUnitTargetTypes);
		}

		protected override void Created(Actor self)
		{
			PlayerPower = self.Owner.PlayerActor.TraitOrDefault<PowerManager>();
			playerResources = self.Owner.PlayerActor.Trait<PlayerResources>();
			resourceLayer = self.World.WorldActor.TraitOrDefault<IResourceLayer>();
			pathFinder = self.World.WorldActor.TraitOrDefault<IPathFinder>();
			positionsUpdatedModules = self.Owner.PlayerActor.TraitsImplementing<IBotPositionsUpdated>().ToArray();
			BaseExpansionModules = self.Owner.PlayerActor.TraitsImplementing<IBotBaseExpansion>().ToArray();

			var i = 0;

			foreach (var building in Info.BuildingQueues)
				builders[i++] = new BaseBuilderQueueManagerCA(this, building, player, PlayerPower, playerResources, resourceLayer);

			foreach (var defense in Info.DefenseQueues)
				builders[i++] = new BaseBuilderQueueManagerCA(this, defense, player, PlayerPower, playerResources, resourceLayer);
		}

		protected override void TraitEnabled(Actor self)
		{
			RefreshBotLimits();

			// Avoid all AIs reevaluating assignments on the same tick, randomize their initial evaluation delay.
			assignRallyPointsTicks = world.LocalRandom.Next(0, Info.AssignRallyPointsInterval);
			checkBestResourceLocationTicks = world.LocalRandom.Next(0, Info.CheckBestResourceLocationInterval);
			sellRefineryTick = Info.SellRefineryInterval < 0 ? 0 : world.LocalRandom.Next(0, Info.SellRefineryInterval);
		}

		void IBotPositionsUpdated.UpdatedBaseCenter(CPos newLocation)
		{
			initialBaseCenter = newLocation;
		}

		void IBotPositionsUpdated.UpdatedDefenseCenter(CPos newLocation)
		{
			DefenseCenter = newLocation;
		}

		bool IBotRequestPauseUnitProduction.PauseUnitProduction => !IsTraitDisabled && !HasMinimalRefineryCount();

		void IBotTick.BotTick(IBot bot)
		{
			if (firstTick)
			{
				// Conditional traits are initialized after INotifyCreated, so resolve difficulty limits again here.
				RefreshBotLimits();
				if (!openingStartingCashCaptured)
				{
					openingStartingCash = playerResources.GetCashAndResources();
					openingStartingCashCaptured = true;
				}

				ResourceMapModule = bot.Player.PlayerActor.TraitsImplementing<ResourceMapBotModule>().FirstOrDefault(t => t.IsTraitEnabled());
				expansionTargetProviders = bot.Player.PlayerActor.TraitsImplementing<IBotExpansionTargetProvider>().ToArray();
				firstTick = false;
			}

			if (!openingBarracksPriorityCompleted && AIUtils.CountActorByCommonName(barracksBuildings) > 0)
				openingBarracksPriorityCompleted = true;

			if (RelocationHoldConyard != null &&
				(!RelocationHoldConyard.IsInWorld || RelocationHoldConyard.IsDead ||
				!BaseExpansionModules.Any(be => be.IsConyardRelocationPending(RelocationHoldConyard))))
				RelocationHoldConyard = null;

			if (--assignRallyPointsTicks <= 0)
			{
				assignRallyPointsTicks = Math.Max(2, Info.AssignRallyPointsInterval);
				foreach (var rp in world.ActorsWithTrait<RallyPoint>().Where(rp => rp.Actor.Owner == player))
					rallyPoints.Push(rp);
			}
			else
			{
				// PERF: Spread out rally point assignments updates across multiple ticks.
				var updateCount = Exts.IntegerDivisionRoundingAwayFromZero(rallyPoints.Count, assignRallyPointsTicks);
				for (var i = 0; i < updateCount; i++)
				{
					var rp = rallyPoints.Pop();
					if (rp.Actor.Owner == player && !rp.Actor.Disposed)
						SetRallyPoint(bot, rp);
				}
			}

			if (--checkBestResourceLocationTicks <= 0 && resourceLayer != null)
			{
				checkBestResourceLocationTicks = Info.CheckBestResourceLocationInterval;

				// Clear outdated refinery requests that add too many refinery to a map indice
				if (ResourceMapModule != null)
				{
					foreach (var mcv in RequestedRefineries.Keys.ToList())
					{
						if (ResourceMapModule.FindClosestIndiceFromCPos(
							RequestedRefineries[mcv].ResourceLoc).PlayerRefineryCount >= Info.MaxRefineryPerIndice)
							RequestedRefineries.Remove(mcv);
					}
				}

				Actor bestconyard = null;
				var best = int.MinValue;

				foreach (var conyard in ConstructionYardBuildings.Actors)
				{
					if (conyard.IsDead)
						continue;

					if (!world.Map.FindTilesInAnnulus(conyard.Location, Info.MinBaseRadius, Info.MaxBaseRadius)
						.Any(c => ResourceMapModule != null
						? ResourceMapModule.Info.ValuableResourceTypes.Contains(resourceLayer.GetResource(c).Type)
						: resourceLayer.GetResource(c).Type != null))
						continue;

					var refs = world.FindActorsInCircle(conyard.CenterPosition, WDist.FromCells(Info.MaxBaseRadius))
							.Count(a => a.Owner == player && Info.RefineryTypes.Contains(a.Info.Name));

					var suitable = -world.FindActorsInCircle(conyard.CenterPosition, WDist.FromCells(Info.MaxBaseRadius))
							.Count(a => a.Owner.RelationshipWith(player) == PlayerRelationship.Enemy) - refs;

					if (suitable > best)
					{
						best = suitable;
						bestconyard = conyard;
					}
				}

				ResourceConyardCenter = bestconyard?.Location;
			}

			BuildingsBeingProduced.Clear();

			// PERF: We tick only one type of valid queue at a time
			// if AI gets enough cash, it can fill all of its queues with enough ticks
			var findQueue = false;
			ExcessPower = PlayerPower != null ? PlayerPower.ExcessPower : 0;
			for (int i = 0, builderIndex = currentBuilderIndex; i < builders.Length; i++)
			{
				if (++builderIndex >= builders.Length)
					builderIndex = 0;

				--builders[builderIndex].WaitTicks;

				var queues = AIUtils.FindQueues(player, builders[builderIndex].Category).ToArray();
				if (queues.Length != 0)
				{
					if (!findQueue)
					{
						currentBuilderIndex = builderIndex;
						findQueue = true;
					}

					// Record buildings being produced only when AI can produce,
					// and record their power only when AI can produce
					if (playerResources.GetCashAndResources() >= Info.BuildingProductionMinCashRequirement)
					{
						foreach (var queue in queues)
						{
							// Record the number of the buildings.
							var producing = queue.AllQueued().FirstOrDefault();
							if (producing == null)
								continue;

							if (BuildingsBeingProduced.TryGetValue(producing.Item, out var value))
								BuildingsBeingProduced[producing.Item] = ++value;
							else
								BuildingsBeingProduced.Add(producing.Item, 1);

							// Record the power of the building.
							ExcessPower += producing.ActorInfo.TraitInfos<PowerInfo>().Where(p => p.EnabledByDefault).Sum(pi => pi.Amount);
						}
					}
				}
			}

			if (ExpansionPrebuildEnabled)
			{
				// ECON-A: refresh the income window and sweep demand liveness/ETAs before the queue tick
				// so a freshly-deployed yard releases its held items on the same tick.
				SampleExpansionIncome();
				SweepExpansionDemands(bot);
			}

			builders[currentBuilderIndex].Tick(bot);

			if (Info.SellRefineryInterval >= 0 && --sellRefineryTick <= 0)
			{
				SellUselessRefinery(bot);
				sellRefineryTick = Info.SellRefineryInterval;
			}
		}

		/// <summary>
		/// The first enabled build-order knobs provider (AI_ARCHITECTURE 12.25), or null: classic and the switch-off state have none,
		/// and every knob consumer then keeps the unscaled number, bit-identical.
		/// </summary>
		public IBotBuildOrderKnobs BuildOrderKnobs
		{
			get
			{
				buildOrderKnobs ??= player.PlayerActor.TraitsImplementing<IBotBuildOrderKnobs>().ToArray();
				return buildOrderKnobs.FirstEnabled();
			}
		}

		/// <summary>An enabled scale-targets provider's target for the category (DESIGN 19.10); false = keep the BotLimits number.</summary>
		public bool TryGetScaleTarget(string category, out int target)
		{
			scaleTargets ??= player.PlayerActor.TraitsImplementing<IBotScaleTargets>().ToArray();
			return scaleTargets.TryTarget(category, out target);
		}

		/// <summary>
		/// The BuildingLimits entry of a building, with the scale targets applied: a building the provider tags `tech` or
		/// `superweapon` takes its category target in place of its per-actor number; every other entry keeps its value.
		/// False when the building has no entry (no limit).
		/// </summary>
		public bool TryGetBuildingLimit(string actorName, out int limit)
		{
			if (!Info.BuildingLimits.TryGetValue(actorName, out limit))
				return false;

			scaleTargets ??= player.PlayerActor.TraitsImplementing<IBotScaleTargets>().ToArray();
			if (scaleTargets.TryBuilding(actorName, out var scaled))
				limit = scaled;

			return true;
		}

		void RefreshBotLimits()
		{
			botLimits = player.PlayerActor.TraitsImplementing<BotLimits>().FirstEnabledTraitOrDefault();
			refineryLimit = botLimits?.Info.RefineryLimit ?? 0;

			foreach (var builder in builders)
				builder.SetBotLimits(botLimits);
		}

		void IBotRespondToAttack.RespondToAttack(IBot bot, Actor self, AttackInfo e)
		{
			if (e.Attacker == null || e.Attacker.Disposed)
				return;

			if (e.Attacker.Owner.RelationshipWith(self.Owner) != PlayerRelationship.Enemy)
				return;

			if (!e.Attacker.Info.HasTraitInfo<ITargetableInfo>())
				return;

			if (!self.Info.HasTraitInfo<BuildingInfo>())
				return;

			if (ShouldSell(self, e))
			{
				bot.QueueOrder(new Order("Sell", self, Target.FromActor(self), false)
				{
					SuppressVisualFeedback = true
				});
				AIUtils.BotDebug("AI ({0}): Decided to sell {1}", player.ClientIndex, self);
				return;
			}

			// Protect buildings not suitable for selling
			foreach (var n in positionsUpdatedModules)
				n.UpdatedDefenseCenter(e.Attacker.Location);
		}

		bool ShouldSell(Actor self, AttackInfo e)
		{
			if (!self.Info.HasTraitInfo<SellableInfo>())
				return false;

			if (Info.DefenseTypes.Contains(self.Info.Name))
				return false;

			if (e.DamageState == DamageState.Dead || e.DamageState < DamageState.Medium || e.DamageState == e.PreviousDamageState)
				return false;

			var inMainBase = (self.CenterPosition - self.World.Map.CenterOfCell(initialBaseCenter)).Length < WDist.FromCells(28).Length;
			var chanceThreshold = inMainBase ? 95 : 70;

			if (self.World.LocalRandom.Next(100) < chanceThreshold)
				return false;

			if (Info.ConstructionYardTypes.Contains(self.Info.Name) && AIUtils.CountActorByCommonName(ConstructionYardBuildings) <= 1)
				return false;

			if (Info.BarracksTypes.Contains(self.Info.Name) && AIUtils.CountActorByCommonName(barracksBuildings) <= 1)
				return false;

			if (Info.VehiclesFactoryTypes.Contains(self.Info.Name) && AIUtils.CountActorByCommonName(factoryBuildings) <= 1)
				return false;

			var enemyUnits = self.World.FindActorsInCircle(self.CenterPosition, WDist.FromCells(Info.SellScanRadius)).Where(IsEnemyGroundUnit).ToList();

			if (enemyUnits.Count > 5)
			{
				var allyUnits = self.World.FindActorsInCircle(self.CenterPosition, WDist.FromCells(Info.SellScanRadius)).Where(IsAllyGroundUnit).ToList();

				if (enemyUnits.Count >= allyUnits.Count * 2)
					return true;
			}

			return false;
		}

		void SetRallyPoint(IBot bot, TraitPair<RallyPoint> rp)
		{
			var needsRallyPoint = rp.Trait.Path.Count == 0;

			if (!needsRallyPoint)
			{
				var locomotors = LocomotorsForProducibles(rp.Actor);
				needsRallyPoint = !IsRallyPointValid(rp.Actor.Location, rp.Trait.Path[0], locomotors, rp.Actor.Info.TraitInfoOrDefault<BuildingInfo>());
			}

			if (needsRallyPoint)
			{
				bot.QueueOrder(new Order("SetRallyPoint", rp.Actor, Target.FromCell(world, ChooseRallyLocationNear(rp.Actor)), false)
				{
					SuppressVisualFeedback = true
				});
			}
		}

		// Won't work for shipyards...
		CPos ChooseRallyLocationNear(Actor producer)
		{
			var locomotors = LocomotorsForProducibles(producer);

			// DESIGN 19.12: with a staging provider new ground units wait at the front with the defences (the biggest group's
			// point), not at a random cell beside the factory. A producer whose units cannot walk there (shipyard, helipad)
			// keeps the old choice. No provider = the random cell below, bit for bit.
			var staging = player.PlayerActor.TraitsImplementing<IBotArmyStaging>().FirstEnabledTraitOrDefault()?.PrimaryStagingCell;
			if (staging != null && locomotors.Length > 0
				&& (pathFinder == null || locomotors.All(l => pathFinder.PathMightExistForLocomotorBlockedByImmovable(l, producer.Location, staging.Value))))
				return staging.Value;

			var possibleRallyPoints = world.Map.FindTilesInCircle(producer.Location, Info.RallyPointScanRadius)
				.Where(c => IsRallyPointValid(producer.Location, c, locomotors, producer.Info.TraitInfoOrDefault<BuildingInfo>()))
				.ToList();

			if (possibleRallyPoints.Count == 0)
			{
				AIUtils.BotDebug("{0} has no possible rallypoint near {1}", producer.Owner, producer.Location);
				return producer.Location;
			}

			return possibleRallyPoints.Random(world.LocalRandom);
		}

		Locomotor[] LocomotorsForProducibles(Actor producer)
		{
			// Per-actor production
			var productions = producer.TraitsImplementing<Production>();

			// Player-wide production
			if (!productions.Any())
				productions = producer.World.ActorsWithTrait<Production>().Where(x => x.Actor.Owner != producer.Owner).Select(x => x.Trait);

			var produces = productions.SelectMany(p => p.Info.Produces).ToHashSet();
			var locomotors = Array.Empty<Locomotor>();
			if (produces.Count > 0)
			{
				// Per-actor production
				var productionQueues = producer.TraitsImplementing<ProductionQueue>();

				// Player-wide production
				if (!productionQueues.Any())
					productionQueues = producer.Owner.PlayerActor.TraitsImplementing<ProductionQueue>();

				productionQueues = productionQueues.Where(pq => produces.Contains(pq.Info.Type));

				var producibles = productionQueues.SelectMany(pq => pq.BuildableItems());
				var locomotorNames = producibles
					.Select(p => p.TraitInfoOrDefault<MobileInfo>())
					.Where(mi => mi != null)
					.Select(mi => mi.Locomotor)
					.ToHashSet();

				if (locomotorNames.Count != 0)
					locomotors = world.WorldActor.TraitsImplementing<Locomotor>()
						.Where(l => locomotorNames.Contains(l.Info.Name))
						.ToArray();
			}

			return locomotors;
		}

		bool IsRallyPointValid(CPos producerLocation, CPos rallyPointLocation, Locomotor[] locomotors, BuildingInfo buildingInfo)
		{
			return
				(pathFinder == null ||
					locomotors.All(l => pathFinder.PathMightExistForLocomotorBlockedByImmovable(l, producerLocation, rallyPointLocation)))
				&&
				(buildingInfo == null ||
					world.IsCellBuildable(rallyPointLocation, rallyPointLocation, null, buildingInfo));
		}

		// RefineryLimit (via BotLimits) is a single difficulty-scaled cap shared across every
		// construction yard the player owns, regardless of faction. Without the candidate
		// override below, a stray/secondary construction yard of a different faction than the
		// player's main base could never get its own first refinery once the main base alone
		// had already reached the global cap - starving that base's economy (and everything
		// that depends on it) indefinitely. Passing the specific refinery actor being
		// considered lets us always allow a faction's first refinery through.
		public bool HasMaxRefineries => HasMaxRefineriesFor(null);

		public bool HasMaxRefineriesFor(ActorInfo candidate)
		{
			if (candidate != null)
			{
				var conyardType = GetRequiredConstructionYardType(candidate);
				if (conyardType != null)
				{
					var factionHasRefinery = RefineryBuildings.Actors.Any(a => !a.IsDead
						&& GetRequiredConstructionYardType(a.Info) == conyardType);

					if (!factionHasRefinery)
						return false;
				}
			}

			var currentRefineryCount = AIUtils.CountActorByCommonName(RefineryBuildings);

			// REF-1 (§12.24 v2, DESIGN §19.1b): one refinery per anchor, bound 1:1 — a refinery is allowed only while
			// more anchors are claimable (unserved, not parked, not pending) than refineries already in flight. Never
			// compare global totals: duplicate refineries stacked at home must not eat a forward anchor's quota. The
			// yard-based ceiling (RefineriesPerBase x yards + MaxExtraRefineries), BotLimits.RefineryLimit and the
			// §19.10 scale-target cap are all bypassed — coverage is the cap.
			var law = RefineryLawProvider();
			if (law != null)
			{
				var inProduction = 0;
				foreach (var r in Info.RefineryTypes)
					if (BuildingsBeingProduced != null && BuildingsBeingProduced.TryGetValue(r, out var n))
						inProduction += n;

				return law.UnclaimedAnchorsInReach <= inProduction;
			}

			// Scale targets (DESIGN 19.10): an enabled provider's refinery target replaces BotLimits.RefineryLimit.
			var limit = TryGetScaleTarget("refinery", out var scaledRefineries) ? scaledRefineries : refineryLimit;
			if (limit != 0 && currentRefineryCount >= limit)
				return true;

			foreach (var r in Info.RefineryTypes)
			{
				if (BuildingsBeingProduced != null && BuildingsBeingProduced.ContainsKey(r))
					currentRefineryCount += BuildingsBeingProduced[r];
			}

			return currentRefineryCount >= AIUtils.CountActorByCommonName(ConstructionYardBuildings) * Info.RefineriesPerBase + Info.MaxExtraRefineries;
		}

		// Require at least one refinery, unless we can't build it. REF-1 (§12.24 v2): under the refinery law the
		// count is irrelevant — adequate iff no anchor is claimable (unclaimed <= in-flight ends the want).
		public bool HasAdequateRefineryCount() =>
			Info.RefineryTypes.Count == 0 ||
			(RefineryLawProvider() != null
				? ExpansionWantsRefinery() == null
				: AIUtils.CountActorByCommonName(RefineryBuildings) >= OptimalRefineryCount()
					&& ExpansionWantsRefinery() == null) ||
			AIUtils.CountActorByCommonName(powerBuildings) == 0 ||
			AIUtils.CountActorByCommonName(ConstructionYardBuildings) == 0;

		int OptimalRefineryCount() =>
			AIUtils.CountActorByCommonName(ProductionBuildings) > 0
			? Info.InititalMinimumRefineryCount + Info.AdditionalMinimumRefineryCount + (AIUtils.CountActorByCommonName(ConstructionYardBuildings) - 1) * Info.RefineriesPerBase
			: Info.InititalMinimumRefineryCount;

		bool HasMinimalRefineryCount() =>
			AIUtils.CountActorByCommonName(RefineryBuildings) >= Info.InititalMinimumRefineryCount;

		public bool HasAdequateProductionCount() =>
			Info.ProductionTypes.Count == 0 ||
			AIUtils.CountActorByCommonName(ProductionBuildings) > 0;

		public bool HasCompletedPowerPlant() => AIUtils.CountActorByCommonName(powerBuildings) > 0;

		public bool OpeningBarracksPriorityCompleted => openingBarracksPriorityCompleted;

		public bool HasBuiltOrQueuedBarracks() =>
			AIUtils.CountActorByCommonName(barracksBuildings) > 0 || CountQueuedBuildings(Info.BarracksTypes) > 0;

		public bool HasQueuedBarracks() => CountQueuedBuildings(Info.BarracksTypes) > 0;

		public bool HasQueuedPowerPlant() => CountQueuedBuildings(Info.PowerTypes) > 0;

		public bool CanTrainOpeningDefense =>
			UsesBarracksFirstOpening && openingRefineryCostCommitted && !HasMinimalRefineryCount() && HasQueuedRefinery();

		bool UsesBarracksFirstOpening => botLimits != null && botLimits.Info.PrioritizeBarracksBeforeRefinery
			&& Info.BarracksBeforeRefineryFactions.Contains(player.Faction.InternalName);

		bool HasQueuedRefinery() => CountQueuedBuildings(Info.RefineryTypes) > 0;

		public void RecordOpeningStructureQueued(ProductionQueue queue, ActorInfo actorInfo)
		{
			if (!UsesBarracksFirstOpening || openingRefineryCostCommitted)
				return;

			var cost = queue.GetProductionCost(actorInfo);
			if (Info.PowerTypes.Contains(actorInfo.Name))
				openingPowerCommittedCost += cost;
			else if (Info.BarracksTypes.Contains(actorInfo.Name) && !openingBarracksCostCommitted)
			{
				openingBarracksCommittedCost = cost;
				openingBarracksCostCommitted = true;
			}
			else if (Info.RefineryTypes.Contains(actorInfo.Name) && openingBarracksPriorityCompleted)
			{
				// Custom maps may start with opening structures already present instead of producing them.
				if (openingPowerCommittedCost == 0)
					openingPowerCommittedCost = powerBuildings.Actors.Where(a => !a.IsDead)
						.Sum(a => queue.GetProductionCost(a.Info));

				if (!openingBarracksCostCommitted)
				{
					var barracks = barracksBuildings.Actors.FirstOrDefault(a => !a.IsDead);
					if (barracks != null)
					{
						openingBarracksCommittedCost = queue.GetProductionCost(barracks.Info);
						openingBarracksCostCommitted = true;
					}
				}

				openingRefineryCommittedCost = cost;
				openingRefineryCostCommitted = true;
				AIUtils.BotDebug("AI: {0} reserved {1} of {2} starting credits for the opening economy; {3} remain for early defense.",
					player, OpeningStructureCommittedCost, openingStartingCash, OpeningDefenseBudget);
			}
		}

		public bool TryCommitOpeningDefenseCost(int cost)
		{
			if (!CanTrainOpeningDefense || cost <= 0 || openingDefenseCommittedCost + cost > OpeningDefenseBudget)
				return false;

			openingDefenseCommittedCost += cost;
			return true;
		}

		int OpeningStructureCommittedCost =>
			openingPowerCommittedCost + openingBarracksCommittedCost + openingRefineryCommittedCost;

		int OpeningDefenseBudget => Math.Max(0, openingStartingCash - OpeningStructureCommittedCost);

		int CountQueuedBuildings(IReadOnlySet<string> buildingTypes) =>
			Info.BuildingQueues.Concat(Info.DefenseQueues)
				.Distinct()
				.SelectMany(category => AIUtils.FindQueues(player, category))
				.Distinct()
				.SelectMany(queue => queue.AllQueued())
				.Count(item => buildingTypes.Contains(item.Item));

		void SellUselessRefinery(IBot bot)
		{
			// Sell one refinery each time. Perserve at least one refinery
			var refineries = world.ActorsHavingTrait<Refinery>().Where(a => a.Owner == player).ToArray();

			if (refineries.Length <= Info.InititalMinimumRefineryCount + Info.AdditionalMinimumRefineryCount)
				return;

			for (var i = 0; i < refineries.Length; i++)
			{
				// StarCraft and Warcraft headquarters also accept resources. Keep them
				// in the refinery count, but never sell them as redundant drop-off sites.
				if (Info.ConstructionYardTypes.Contains(refineries[i].Info.Name))
				{
					AIUtils.BotDebug("AI ({0}): Preserving headquarters during refinery cleanup: {1}", player.ClientIndex, refineries[i]);
					continue;
				}

				for (var j = i + 1; j < refineries.Length; j++)
				{
					if ((refineries[i].Location - refineries[j].Location).LengthSquared <= Info.SellRefineryTooCloseCellDistance * Info.SellRefineryTooCloseCellDistance)
					{
						bot.QueueOrder(new Order("Sell", refineries[i], Target.FromActor(refineries[i]), false));
						return;
					}
				}

				if (ResourceMapModule != null &&
					!world.Map.FindTilesInAnnulus(refineries[i].Location, 0, Info.SellRefineryNoResourceDistance)
					.Any(c => ResourceMapModule.Info.ValuableResourceTypes.Contains(resourceLayer.GetResource(c).Type))
					&& !world.FindActorsInCircle(refineries[i].CenterPosition, WDist.FromCells(Info.SellRefineryNoResourceDistance))
					.Any(a => ResourceMapModule.Info.ResourceCreatorTypes.Contains(a.Info.Name)))
				{
					bot.QueueOrder(new Order("Sell", refineries[i], Target.FromActor(refineries[i]), false));
					return;
				}
			}
		}

		/// <summary>
		/// ECON-A (§2/§6): per-tick demand lifecycle — expire dead/stuck/claimed-away MCVs, follow a
		/// Transform's ReplacedByActor (deploy = conyard lands; relocate = the journey continues on the
		/// new actor), refresh the liveness window while it is en route, recompute ETAs on cadence, and
		/// verify queue bindings once the order-latency grace has passed. Deterministic: iterates in
		/// ActorID order, never on dictionary enumeration order.
		/// </summary>
		void SweepExpansionDemands(IBot bot)
		{
			if (ExpansionDemands.Count == 0)
				return;

			var now = world.WorldTick;
			var leases = BotUnitLeases.Of(player);
			foreach (var demand in ExpansionDemands.Values.OrderBy(d => d.Mcv.ActorID).ToList())
			{
				var mcv = demand.Mcv;

				// ECON-A-FIX (R3): the transform edge fires exactly once — ReplacedByActor stays readable
				// on the disposed MCV forever, so it is gated on Deployed (the consumed marker). A yard
				// replacement deploys; a live own non-yard relocates the demand onto it; an unusable or
				// foreign-owned replacement (disposed, dead, lost, captured) ends the journey — the
				// demand expires instead of latching onto a corpse or an enemy's yard.
				if (!demand.Deployed && mcv != null && mcv.ReplacedByActor != null)
				{
					var next = mcv.ReplacedByActor;
					var usable = !next.Disposed && !next.IsDead && next.IsInWorld && next.Owner == player;
					var transition = BaseBuilderQueueEvalCA.ExpansionTransform(
						alreadyDeployed: false, hasReplacement: true,
						replacementUsable: usable, replacementYard: usable && Info.ConstructionYardTypes.Contains(next.Info.Name));

					if (transition == BaseBuilderQueueEvalCA.ExpansionTransformTransition.Deploy)
					{
						// Deployed: the conyard exists — the Done items release through the normal
						// placement path this tick (the queue managers see Deployed and stop holding).
						// The window is set once here — every later sweep skips this branch (the
						// transform edge is consumed) — and the builder nudge fires exactly once.
						demand.Deployed = true;
						demand.DeployedYard = next;
						demand.DeployedYardLoc = next.Location;
						demand.ExpiresTick = now + Info.ExpansionDemandIdleTicks;
						leases?.Release(mcv, LeaseOwner);
						foreach (var builder in builders)
							builder.WaitTicks = Math.Min(builder.WaitTicks, 0);
					}
					else if (transition == BaseBuilderQueueEvalCA.ExpansionTransformTransition.Relocate)
					{
						// Relocation (conyard→MCV) or another non-yard transform: the journey continues
						// on the replacement actor — re-key both records and the lease so the demand
						// follows it.
						RequestedRefineries.Remove(mcv);
						RequestedRefineries[next] = (demand.ConyardLoc, demand.ResourceLoc);
						ExpansionDemands.Remove(mcv);
						leases?.Release(mcv, LeaseOwner);
						demand.Mcv = next;
						ExpansionDemands[next] = demand;
						BotUnitLeases.TryClaim(leases, next, LeaseOwner, BotLeasePurpose.McvExpansion, Info.ExpansionDemandIdleTicks);
						demand.ExpiresTick = now + Info.ExpansionDemandIdleTicks;
						continue;
					}
					else
					{
						ExpireExpansionDemand(bot, demand);
						continue;
					}
				}
				else if (!demand.Deployed &&
					(mcv == null || mcv.Disposed || mcv.IsDead || !mcv.IsInWorld || mcv.Owner != player
						|| !RequestedRefineries.ContainsKey(mcv)))
				{
					// ECON-A-FIX (R4): death, disposal, capture and a consumed request all lapse the
					// demand — none of them waits for the idle window.
					ExpireExpansionDemand(bot, demand);
					continue;
				}
				else if (!demand.Deployed)
				{
					// ECON-A-FIX (R4): the demand holds the MCV's McvExpansion lease while the journey is
					// ours — the spec's "active while the MCV holds the expansion lease" contract, claimed
					// at posting and re-claimed every sweep as the heartbeat. A foreign owner's lease
					// (takeover, emergency preempt) makes the claim fail; so does losing the unit.
					if (!BotUnitLeases.TryClaim(leases, mcv, LeaseOwner, BotLeasePurpose.McvExpansion, Info.ExpansionDemandIdleTicks))
					{
						ExpireExpansionDemand(bot, demand);
						continue;
					}

					// ECON-A-FIX2 (R4): the chain's last positional target must still aim at the deploy
					// neighbourhood — a redirected MCV (DeployMcvs reposts only idle ones, so a
					// mid-flight redirect otherwise kept the stale demand forever) lapses here.
					var journey = JourneyState(mcv, demand);
					if (journey == BaseBuilderQueueEvalCA.ExpansionJourneyState.Redirected)
					{
						ExpireExpansionDemand(bot, demand);
						continue;
					}

					// ECON-A-FIX2 (R4): renewal requires a provably ongoing journey — a committed
					// chain on a non-idle traveller, or a unit-less actor (a conyard waiting to
					// undeploy) whose continued existence IS the in-flight relocation. An
					// indeterminate, targetless chain — a WaitFor with a false predicate, a stalled
					// Turn — no longer renews: the outstanding ExpiresTick is the bounded silence
					// grace, so silence cannot keep a demand alive forever.
					if (BaseBuilderQueueEvalCA.JourneyRenewsDemand(journey, mcv.IsIdle, mcv.TraitOrDefault<Mobile>() != null))
						demand.ExpiresTick = now + Info.ExpansionDemandIdleTicks;

					if (now >= demand.ExpiresTick)
					{
						ExpireExpansionDemand(bot, demand);
						continue;
					}

					if (now >= demand.NextEtaTick)
						RefreshExpansionEta(demand);
				}
				else if (demand.DeployedYard == null || demand.DeployedYard.Disposed || demand.DeployedYard.IsDead
					|| !demand.DeployedYard.IsInWorld || demand.DeployedYard.Owner != player)
				{
					// ECON-A-FIX (R3): the committed actor after deploy is the yard — the outpost died,
					// was captured, or undeployed away. The demand ends with it (bound items unwind for
					// their refund) instead of leaking bindings onto a corpse.
					ExpireExpansionDemand(bot, demand);
					continue;
				}

				VerifyDemandBinding(demand, isRefinery: true, now);
				VerifyDemandBinding(demand, isRefinery: false, now);

				// ECON-A-FIX (R6): the bound refinery's anchor reservation renews with the demand — a
				// lost hold (the anchor got served, parked or committed elsewhere) just falls back to a
				// fresh claim at placement; the law's one-refinery-per-anchor rule stands either way.
				if (demand.ReservedClaim is { } reserved)
				{
					var law = RefineryLawProvider();
					if (demand.RefineryItem == null || law == null
						|| !law.TryReserveRefineryAnchor(reserved.Anchor, demand, now + Info.ExpansionDemandIdleTicks))
						demand.ReservedClaim = null;
				}

				// Deployed and fully placed/cleared — done.
				if (demand.Deployed && demand.Unbound && now >= demand.ExpiresTick)
				{
					RequestedRefineries.Remove(demand.Mcv);
					ExpansionDemands.Remove(demand.Mcv);
				}
			}
		}

		/// <summary>
		/// ECON-A-FIX2 (R4): where the traveller's activity chain leaves the journey — Move reports its
		/// destination (or remaining path) through the public GetTargets seam; TransformsIntoMobile
		/// persists the redeploy destination into the emergent MCV's own chain. The last positional
		/// target decides: Committed while it aims inside the deploy slack, Redirected once it aims
		/// elsewhere, Indeterminate while the chain reports no positional target at all.
		/// </summary>
		BaseBuilderQueueEvalCA.ExpansionJourneyState JourneyState(Actor mcv, ExpansionDemand demand)
		{
			var sawTarget = false;
			var lastNear = false;
			foreach (var t in ActivityTargets(mcv, mcv.CurrentActivity))
			{
				if (t.Type == TargetType.Invalid)
					continue;

				sawTarget = true;
				lastNear = (world.Map.CellContaining(t.CenterPosition) - demand.ConyardLoc).Length <= ExpansionJourneySlackCells;
			}

			return BaseBuilderQueueEvalCA.ExpansionJourney(sawTarget, lastNear);
		}

		/// <summary>Every target a chain reports — each activity's own, then its child's, then its next's.</summary>
		static IEnumerable<Target> ActivityTargets(Actor self, Activity activity)
		{
			for (var a = activity; a != null; a = a.NextActivity)
			{
				var targets = a.GetTargets(self);
				if (targets != null)
					foreach (var t in targets)
						yield return t;

				if (a.ChildActivity != null)
					foreach (var t in ActivityTargets(self, a.ChildActivity))
						yield return t;
			}
		}

		/// <summary>
		/// ECON-A (§6): expiry unwinds the demand — a still-building or held item is cancelled for its
		/// normal refund (a Done item refunds the full paid amount), except a held Ready refinery that
		/// the law can re-adopt on another anchor: it converts to a normal queued refinery instead.
		/// </summary>
		void ExpireExpansionDemand(IBot bot, ExpansionDemand demand)
		{
			var law = RefineryLawProvider();

			// ECON-A-FIX (R6): the demand's anchor hold ends with it — released before the re-adopt check
			// so a still-Ready refinery may legally re-claim that very anchor as an ordinary item.
			if (demand.ReservedClaim is { } reserved)
				law?.ReleaseRefineryAnchor(reserved.Anchor, demand);

			// ECON-A-FIX (R4): the traveller's McvExpansion lease is the demand's — released on every
			// lapse path (the registry prunes a dead unit's lease on its own cadence regardless).
			BotUnitLeases.Of(player)?.Release(demand.Mcv, LeaseOwner);

			if (demand.RefineryItem != null)
			{
				var readopted = law != null
					&& ItemDone(demand.RefineryProducer, demand.RefineryItem)
					&& law.NextRefineryClaim(null) != null;
				if (!readopted)
					CancelDemandItem(bot, demand.RefineryProducer, demand.RefineryItem);

				demand.UnbindRefinery();
			}

			if (demand.DefenceItem != null)
			{
				CancelDemandItem(bot, demand.DefenceProducer, demand.DefenceItem);
				demand.UnbindDefence();
			}

			RequestedRefineries.Remove(demand.Mcv);
			ExpansionDemands.Remove(demand.Mcv);
		}

		static bool ItemDone(Actor producer, string item)
		{
			if (producer == null || producer.Disposed || producer.IsDead)
				return false;

			return producer.TraitsImplementing<ProductionQueue>()
				.Any(q => q.AllQueued().Any(i => i.Item == item && i.Done));
		}

		static void CancelDemandItem(IBot bot, Actor producer, string item)
		{
			if (producer == null || producer.Disposed || producer.IsDead)
				return;

			bot.QueueOrder(Order.CancelProduction(producer, item, 1));
		}

		/// <summary>
		/// The bound item left its queue (placed or cancelled elsewhere) — release the binding. Inside
		/// the order-latency grace window an absent item is still the queued order in flight, so the
		/// binding survives until the grace expires.
		/// </summary>
		void VerifyDemandBinding(ExpansionDemand demand, bool isRefinery, int now)
		{
			var item = isRefinery ? demand.RefineryItem : demand.DefenceItem;
			if (item == null)
				return;

			var producer = isRefinery ? demand.RefineryProducer : demand.DefenceProducer;
			var queuedTick = isRefinery ? demand.RefineryQueuedTick : demand.DefenceQueuedTick;

			if (producer != null && !producer.Disposed
				&& producer.TraitsImplementing<ProductionQueue>().Any(q => q.AllQueued().Any(i => i.Item == item)))
				return;

			if (now - queuedTick < OrderGraceTicks)
				return;

			// ECON-A-FIX (R6): an unwound refinery binding releases its anchor hold — the anchor goes
			// back on the market before the demand could re-bind or expire around it.
			if (isRefinery)
			{
				if (demand.ReservedClaim is { } reserved)
					RefineryLawProvider()?.ReleaseRefineryAnchor(reserved.Anchor, demand);

				demand.UnbindRefinery();
			}
			else
			{
				demand.UnbindDefence();
			}
		}

		/// <summary>
		/// ECON-A (§2): etaTick = now + travelEstimate + deployDuration — PathFinder distance for the
		/// MCV's locomotor over its nominal speed, recomputed on cadence (the MCV may detour). An
		/// unreachable path lands the ETA at far-future so the demand idles out through ExpiresTick.
		/// The defence pick is (re)taken here too: null means nothing was affordable — retried on every
		/// recompute; a chosen type is sticky so the queue never thrashes between candidates.
		/// </summary>
		void RefreshExpansionEta(ExpansionDemand demand)
		{
			var now = world.WorldTick;
			var travel = EstimateTravelTicks(demand.Mcv, demand.ConyardLoc);
			demand.EtaTick = travel == int.MaxValue ? int.MaxValue : now + travel + Info.ExpansionDeployTicks;
			demand.NextEtaTick = now + Info.ExpansionEtaIntervalTicks;
			if (demand.DefenceType == null)
				demand.DefenceType = ChooseDemandDefence(demand.EtaTick);
		}

		/// <summary>
		/// ECON-A-FIX (R1): the honest-travel estimate — a terrain-only search (BlockedByActor.None) plus
		/// a custom cell cost that blocks exactly the obstacles we KNOW: own units, occupants of
		/// currently visible cells, and remembered frozen-under-fog footprints. The live Immovable
		/// graph is never consulted, so an unseen enemy building can neither block nor unblock the
		/// estimate — the production schedule provably does not depend on fog-hidden state.
		/// ECON-A-FIX (R2): the result path runs target-to-source; its length is the sum of its own
		/// consecutive segments only — no source-to-first-cell chord.
		/// </summary>
		int EstimateTravelTicks(Actor mcv, CPos target)
		{
			var mobile = mcv.TraitOrDefault<Mobile>();
			if (mobile == null || pathFinder == null)
				return 0;

			var remembered = FrozenBlockedCells(mcv, mobile);
			var path = pathFinder.FindPathToTargetCell(mcv, new[] { mcv.Location }, target,
				BlockedByActor.None, EtaCellCost(mcv, mobile, remembered), ignoreActor: mcv);
			if (path.Count == 0)
				return int.MaxValue;

			return BaseBuilderQueueEvalCA.ExpansionTravelTicks(
				BaseBuilderQueueEvalCA.ExpansionPathLength(path, c => world.Map.CenterOfCell(c)),
				mobile.MovementSpeedForCell(mcv.Location));
		}

		/// <summary>
		/// ECON-A-FIX (R1): the per-cell cost of the honest search — PathCostForInvalidPath only where a
		/// KNOWN immovable obstacle stands (Locomotor.IsBlockedBy's Immovable predicate re-evaluated
		/// over remembered/visible/own occupants instead of the full ActorMap), else 0.
		/// ECON-A-FIX2 (R1): KNOWN is per-actor legality — CanBeViewedByPlayer, not the cell: a revealed
		/// cell does not reveal a cloaked or otherwise hidden actor standing on it.
		/// </summary>
		Func<CPos, int> EtaCellCost(Actor mcv, Mobile mobile, IReadOnlySet<CPos> remembered)
		{
			var crushes = mobile.Locomotor.Info.Crushes;
			return cell =>
			{
				if (remembered != null && remembered.Contains(cell))
					return PathGraph.PathCostForInvalidPath;

				foreach (var other in world.ActorMap.GetActorsAt(cell))
				{
					if (other == mcv)
						continue;

					var otherMobile = other.OccupiesSpace as Mobile;
					var movable = otherMobile != null && !otherMobile.IsTraitDisabled && !otherMobile.IsTraitPaused && !otherMobile.IsImmovable;
					var moving = movable && otherMobile.CurrentMovementTypes.HasMovementType(MovementType.Horizontal);
					var allied = movable && player.RelationshipWith(other.Owner) == PlayerRelationship.Ally;
					var removable = other.TraitOrDefault<ITemporaryBlocker>() is { } tb && tb.CanRemoveBlockage(other, mcv);
					var transit = other.OccupiesSpace is Building building && building.TransitOnlyCells().Contains(cell);
					var crushable = false;
					foreach (var c in other.Crushables)
						if (c.CrushableBy(other, mcv, crushes))
						{
							crushable = true;
							break;
						}

					if (BaseBuilderQueueEvalCA.EtaOccupantBlocks(
						known: other.Owner == player || other.CanBeViewedByPlayer(player), movable, allied, moving, removable, transit, crushable))
						return PathGraph.PathCostForInvalidPath;
				}

				return 0;
			};
		}

		/// <summary>
		/// ECON-A-FIX2 (R1): the cells remembered frozen-under-fog footprints still block for the MCV —
		/// the frozen layer is the legal record of what we last saw, and ONLY that record is read.
		/// fa.Actor is deliberately never dereferenced: the layer hands out the live backing while it
		/// lives, so reading its traits would leak current hidden state — Sol's probe killed the
		/// backing under fog and watched the blocker set change. Hidden records (the occupant was
		/// masked at last sight) contribute nothing. Removable mirrors the live check on remembered
		/// facts: a temporary blocker (gate, energy wall) lifts for a remembered-friendly owner; a
		/// DoesNotBlock lifts when our MCV's own target types overlap its remembered admission set —
		/// both sides of that comparison are legal knowledge.
		/// </summary>
		HashSet<CPos> FrozenBlockedCells(Actor mcv, Mobile mobile)
		{
			var layer = player.PlayerActor.TraitOrDefault<FrozenActorLayer>();
			if (layer == null)
				return null;

			var crushes = mobile.Locomotor.Info.Crushes;
			var mcvTargetTypes = mcv.GetEnabledTargetTypes();
			var mcvMineImmune = mcv.Info.HasTraitInfo<MineImmuneInfo>();
			HashSet<CPos> blocked = null;
			foreach (var fa in layer.FrozenActorsInRegion(world.Map.AllCells))
			{
				if (!fa.IsValid || fa.Hidden || fa.Info.HasTraitInfo<MobileInfo>())
					continue;

				var info = fa.Info;
				var rememberedAllied = fa.Owner != null && player.RelationshipWith(fa.Owner) == PlayerRelationship.Ally;

				var removable = false;
				var crushable = false;
				foreach (var ti in info.TraitsInConstructOrder())
				{
					if (ti is DoesNotBlockInfo dnb)
						removable |= dnb.TargetTypes.IsEmpty || dnb.TargetTypes.Overlaps(mcvTargetTypes);
					else if (ti is ITemporaryBlockerInfo)
						// Gates and energy walls lift for friendly passers — the remembered owner is the fact.
						removable |= rememberedAllied;
					else if (ti is CrateInfo crate)
						crushable |= crushes.Contains(crate.CrushClass);
					else if (ti is MineInfo mine)
						crushable |= mine.CrushClasses.Overlaps(crushes) && !(mine.BlockFriendly && !mcvMineImmune && rememberedAllied);
					else if (ti.GetType().Name.EndsWith("CrushableInfo", StringComparison.Ordinal))
					{
						// CrushableInfo itself is internal to Common — the remembered record is read
						// off its static fields so it and any custom ICrushable info join the same rule.
						var type = ti.GetType();
						var classesOverlap = type.GetField("CrushClasses")?.GetValue(ti) is BitSet<CrushClass> classes && classes.Overlaps(crushes);
						var friendliesCrush = type.GetField("CrushedByFriendlies")?.GetValue(ti) is true;
						crushable |= BaseBuilderQueueEvalCA.RememberedCrushable(classesOverlap, friendliesCrush, rememberedAllied);
					}
				}

				// Transit-only is per-cell — only these footprint cells let the MCV through. The
				// building anchor is its footprint's top-left cell.
				var topLeft = new CPos(fa.Footprint.Min(p => p.U), fa.Footprint.Min(p => p.V));
				var transit = info.TraitInfoOrDefault<BuildingInfo>() is { } bi
					? new HashSet<CPos>(bi.TransitOnlyTiles(topLeft))
					: null;
				foreach (var puv in fa.Footprint)
				{
					var cell = ((MPos)puv).ToCPos(world.Map);
					if (BaseBuilderQueueEvalCA.FrozenOccupantBlocks(transit != null && transit.Contains(cell), removable, crushable))
						(blocked ??= new HashSet<CPos>()).Add(cell);
				}
			}

			return blocked;
		}

		/// <summary>Own cash + resources plus the measured income rate out to the ETA — integers only.</summary>
		int ProjectedCash(int atTick)
		{
			var projected = playerResources.GetCashAndResources() + (long)projectedIncomePerTick * (atTick - world.WorldTick);
			return (int)Math.Min(int.MaxValue, Math.Max(0, projected));
		}

		void SampleExpansionIncome()
		{
			var now = world.WorldTick;
			if (incomeWindowStartTick < 0)
			{
				incomeWindowStartTick = now;
				incomeWindowStartEarned = playerResources.Earned;
				return;
			}

			var elapsed = now - incomeWindowStartTick;
			if (elapsed < Info.ExpansionIncomeWindowTicks)
				return;

			projectedIncomePerTick = Math.Max(0, (playerResources.Earned - incomeWindowStartEarned) / elapsed);
			incomeWindowStartTick = now;
			incomeWindowStartEarned = playerResources.Earned;
		}

		/// <summary>
		/// ECON-A (§1): the strongest affordable defence for the demand — AttackBase buildings from
		/// Info.DefenseTypes that a defence queue can actually produce, affordable at the ETA against
		/// projected income; the eval's DefenceStrength ordering decides, ties on cost then name.
		/// </summary>
		string ChooseDemandDefence(int etaTick)
		{
			if (etaTick == int.MaxValue)
				return null;

			var projected = ProjectedCash(etaTick);
			var candidates = new List<(ActorInfo Info, int Strength, int Cost)>();
			foreach (var name in Info.DefenseTypes)
			{
				if (!world.Map.Rules.Actors.TryGetValue(name, out var info) || !info.HasTraitInfo<AttackBaseInfo>())
					continue;

				int? cost = null;
				foreach (var category in Info.DefenseQueues)
				{
					foreach (var queue in AIUtils.FindQueues(player, category))
					{
						if (!queue.BuildableItems().Any(b => b.Name == name))
							continue;

						var itemCost = queue.GetProductionCost(info);
						cost = cost == null ? itemCost : Math.Min(cost.Value, itemCost);
					}
				}

				if (cost == null)
					continue;

				candidates.Add((info, BaseBuilderQueueEvalCA.DefenceStrength(info), cost.Value));
			}

			return BaseBuilderQueueEvalCA.ChooseExpansionDefence(candidates, projected)?.Name;
		}

		/// <summary>The demand whose binding matches this queued item on this producer, or null.</summary>
		public ExpansionDemand DemandForQueuedItem(string item, Actor producer)
		{
			if (ExpansionDemands.Count == 0)
				return null;

			foreach (var demand in ExpansionDemands.Values)
			{
				if ((demand.RefineryProducer == producer && demand.RefineryItem == item)
					|| (demand.DefenceProducer == producer && demand.DefenceItem == item))
					return demand;
			}

			return null;
		}

		/// <summary>
		/// ECON-A (§4): the defence pre-build may hold its Ready item on any queue — except when that
		/// queue is the bot's only building producer, where the hold is allowed only within HoldSlack
		/// of the deploy ETA (a lone queue's blocked head would starve every building behind it).
		/// </summary>
		public bool DefenceHoldPermitted(ProductionQueue queue, ExpansionDemand demand, int now)
		{
			return BaseBuilderQueueEvalCA.DefenceHoldPermitted(BuildingProducerCount() <= 1,
				demand.EtaTick - now, Info.ExpansionHoldSlack);
		}

		int buildingProducerCountCached = -1;
		int buildingProducerCountTick = -1;
		int BuildingProducerCount()
		{
			var now = world.WorldTick;
			if (buildingProducerCountTick != now)
			{
				buildingProducerCountTick = now;
				buildingProducerCountCached = Info.BuildingQueues.Concat(Info.DefenseQueues).Distinct()
					.SelectMany(category => AIUtils.FindQueues(player, category))
					.Distinct()
					.Count();
			}

			return buildingProducerCountCached;
		}

		List<MiniYamlNode> IGameSaveTraitData.IssueTraitData(Actor self)
		{
			if (IsTraitDisabled)
				return null;

			return new List<MiniYamlNode>()
			{
				new("InitialBaseCenter", FieldSaver.FormatValue(initialBaseCenter)),
				new("DefenseCenter", FieldSaver.FormatValue(DefenseCenter)),
				new("OpeningBarracksPriorityCompleted", FieldSaver.FormatValue(openingBarracksPriorityCompleted)),
				new("OpeningStartingCashCaptured", FieldSaver.FormatValue(openingStartingCashCaptured)),
				new("OpeningStartingCash", FieldSaver.FormatValue(openingStartingCash)),
				new("OpeningPowerCommittedCost", FieldSaver.FormatValue(openingPowerCommittedCost)),
				new("OpeningBarracksCommittedCost", FieldSaver.FormatValue(openingBarracksCommittedCost)),
				new("OpeningBarracksCostCommitted", FieldSaver.FormatValue(openingBarracksCostCommitted)),
				new("OpeningRefineryCommittedCost", FieldSaver.FormatValue(openingRefineryCommittedCost)),
				new("OpeningRefineryCostCommitted", FieldSaver.FormatValue(openingRefineryCostCommitted)),
				new("OpeningDefenseCommittedCost", FieldSaver.FormatValue(openingDefenseCommittedCost))
			};
		}

		void IGameSaveTraitData.ResolveTraitData(Actor self, MiniYaml data)
		{
			if (self.World.IsReplay)
				return;

			var initialBaseCenterNode = data.NodeWithKeyOrDefault("InitialBaseCenter");
			if (initialBaseCenterNode != null)
				initialBaseCenter = FieldLoader.GetValue<CPos>("InitialBaseCenter", initialBaseCenterNode.Value.Value);

			var defenseCenterNode = data.NodeWithKeyOrDefault("DefenseCenter");
			if (defenseCenterNode != null)
				DefenseCenter = FieldLoader.GetValue<CPos>("DefenseCenter", defenseCenterNode.Value.Value);

			var openingBarracksPriorityCompletedNode = data.NodeWithKeyOrDefault("OpeningBarracksPriorityCompleted");
			if (openingBarracksPriorityCompletedNode != null)
				openingBarracksPriorityCompleted = FieldLoader.GetValue<bool>("OpeningBarracksPriorityCompleted",
					openingBarracksPriorityCompletedNode.Value.Value);

			var openingStartingCashCapturedNode = data.NodeWithKeyOrDefault("OpeningStartingCashCaptured");
			if (openingStartingCashCapturedNode != null)
				openingStartingCashCaptured = FieldLoader.GetValue<bool>("OpeningStartingCashCaptured",
					openingStartingCashCapturedNode.Value.Value);

			var openingStartingCashNode = data.NodeWithKeyOrDefault("OpeningStartingCash");
			if (openingStartingCashNode != null)
				openingStartingCash = FieldLoader.GetValue<int>("OpeningStartingCash", openingStartingCashNode.Value.Value);

			var openingPowerCommittedCostNode = data.NodeWithKeyOrDefault("OpeningPowerCommittedCost");
			if (openingPowerCommittedCostNode != null)
				openingPowerCommittedCost = FieldLoader.GetValue<int>("OpeningPowerCommittedCost", openingPowerCommittedCostNode.Value.Value);

			var openingBarracksCommittedCostNode = data.NodeWithKeyOrDefault("OpeningBarracksCommittedCost");
			if (openingBarracksCommittedCostNode != null)
				openingBarracksCommittedCost = FieldLoader.GetValue<int>("OpeningBarracksCommittedCost",
					openingBarracksCommittedCostNode.Value.Value);

			var openingBarracksCostCommittedNode = data.NodeWithKeyOrDefault("OpeningBarracksCostCommitted");
			if (openingBarracksCostCommittedNode != null)
				openingBarracksCostCommitted = FieldLoader.GetValue<bool>("OpeningBarracksCostCommitted",
					openingBarracksCostCommittedNode.Value.Value);

			var openingRefineryCommittedCostNode = data.NodeWithKeyOrDefault("OpeningRefineryCommittedCost");
			if (openingRefineryCommittedCostNode != null)
				openingRefineryCommittedCost = FieldLoader.GetValue<int>("OpeningRefineryCommittedCost",
					openingRefineryCommittedCostNode.Value.Value);

			var openingRefineryCostCommittedNode = data.NodeWithKeyOrDefault("OpeningRefineryCostCommitted");
			if (openingRefineryCostCommittedNode != null)
				openingRefineryCostCommitted = FieldLoader.GetValue<bool>("OpeningRefineryCostCommitted",
					openingRefineryCostCommittedNode.Value.Value);

			var openingDefenseCommittedCostNode = data.NodeWithKeyOrDefault("OpeningDefenseCommittedCost");
			if (openingDefenseCommittedCostNode != null)
				openingDefenseCommittedCost = FieldLoader.GetValue<int>("OpeningDefenseCommittedCost",
					openingDefenseCommittedCostNode.Value.Value);
		}

		void INotifyActorDisposing.Disposing(Actor self)
		{
			RefineryBuildings.Dispose();
			powerBuildings.Dispose();
			ConstructionYardBuildings.Dispose();
			barracksBuildings.Dispose();
		}

		void IBotSuggestRefineryProduction.RequestLocation(CPos refineryLocation, CPos conyardLocation, Actor expandActor)
		{
			if (ResourceMapModule == null)
			{
				RequestedRefineries[expandActor] = (conyardLocation, refineryLocation);
				PostExpansionDemand(expandActor);
				return;
			}

			// Cameo (§12.13, EX-2): with an expansion planner, pending requests to the same field count toward its
			// cap as well — otherwise several queued requests can stack on one index before PlayerRefineryCount has
			// seen a built refinery. Classic mounts no provider and keeps the old check, request for request.
			var indice = ResourceMapModule.FindClosestIndiceFromCPos(refineryLocation);
			var held = indice.PlayerRefineryCount;
			if (HasExpansionGuidance)
				held += RequestedRefineries.Count(r =>
					r.Key != expandActor && ResourceMapModule.FindClosestIndiceFromCPos(r.Value.ResourceLoc) == indice);

			if (held < Info.MaxRefineryPerIndice)
				RequestedRefineries[expandActor] = (conyardLocation, refineryLocation);

			PostExpansionDemand(expandActor);
		}

		/// <summary>
		/// ECON-A (SPEC_2026-10-05_econ_logistics Part A §1): the MCV owner's RequestLocation call IS the
		/// demand post — the module enriches the retained RequestedRefineries record with the ETA, the
		/// defence pick and the expiry window. A re-post (redirect) refreshes the locations and forces
		/// the next ETA recompute. No-op while the switch is off, the request was rejected, or the
		/// actor is a building whose journey has not started (a conyard mid-relocation still deploys).
		/// </summary>
		void PostExpansionDemand(Actor expandActor)
		{
			if (!ExpansionPrebuildEnabled || expandActor == null || !RequestedRefineries.ContainsKey(expandActor))
				return;

			var isNew = !ExpansionDemands.TryGetValue(expandActor, out var demand);
			if (isNew)
			{
				demand = new ExpansionDemand(expandActor);
				ExpansionDemands[expandActor] = demand;
			}

			var request = RequestedRefineries[expandActor];
			demand.ConyardLoc = request.ConyardLoc;
			demand.ResourceLoc = request.ResourceLoc;
			demand.YardFootprint = DeployFootprint(expandActor, request.ConyardLoc);
			demand.ExpiresTick = world.WorldTick + Info.ExpansionDemandIdleTicks;
			demand.NextEtaTick = world.WorldTick;

			// ECON-A-FIX (R4): the demand publishes only while its own McvExpansion lease holds on the
			// traveller — a first claim refused by a foreign owner's lease means this journey is not ours
			// to schedule, so the demand never posts (the plain request record still stands). On an
			// existing demand a failed re-claim is left to the sweep, which expires it the same tick.
			if (!BotUnitLeases.TryClaim(BotUnitLeases.Of(player), expandActor, LeaseOwner,
				BotLeasePurpose.McvExpansion, Info.ExpansionDemandIdleTicks) && isNew)
				ExpansionDemands.Remove(expandActor);
		}
	}
}
