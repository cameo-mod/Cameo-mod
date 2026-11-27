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
using System.IO;
using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.AS.Traits;
using OpenRA.Mods.CA;
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
		[Desc("Memoises production costs for one BaseBuilder visit when enabled.")]
		public readonly bool CostCacheVisit = false;

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

		[Desc("BINDING tech chain (11-23): this faction's T2 and T3 gate buildings. Exempt from the army-first pause and the mobile reserve while none of them is owned or queued. Empty = no exemption.")]
		public readonly FrozenSet<string> TechChainTypes = FrozenSet<string>.Empty;

		[Desc("Rank the unlisted first-copy fallback by semantic tech tiers.")]
		public readonly bool SemanticTechTiers = false;

		[ActorReference]
		public readonly FrozenSet<string> SemanticRadarTypes = FrozenSet<string>.Empty;

		[ActorReference]
		public readonly FrozenSet<string> SemanticTechCentreTypes = FrozenSet<string>.Empty;

		[ActorReference]
		public readonly FrozenSet<string> SemanticSuperweaponTypes = FrozenSet<string>.Empty;

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

		[Desc("REF-2 P1a (retained-refinery): preferred anchor offers a law-path refinery tries per",
			"placement sweep before the widened fallback takes over — each failed offer commits-or-parks,",
			"so the bound is the only knob on per-sweep claim churn.")]
		public readonly int RefineryClaimAttemptsPerSweep = 4;

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

		[Desc("BZ_production_scaling: queue another production building when banked cash holds above the",
			"difficulty line for the persist window and every unit-production queue is busy, or when net",
			"income per producer exceeds the difficulty line. Armed by the increment switch; false keeps",
			"today's behaviour bit-identical.")]
		public readonly bool UseProductionScaling = false;

		[Desc("BZ: banked cash must exceed this to feed the persist counter (easiest pole; difficulty line via BotDifficultyLadder).")]
		public readonly int ProductionScalingCashEasy = 6000;

		[Desc("BZ: banked cash bar (hardest pole).")]
		public readonly int ProductionScalingCashHard = 2500;

		[Desc("BZ: ticks the cash bar must hold uninterrupted (easiest pole).")]
		public readonly int ProductionScalingPersistEasy = 1500;

		[Desc("BZ: persist window (hardest pole).")]
		public readonly int ProductionScalingPersistHard = 500;

		[Desc("BZ: net income per producer per tick that alone admits a new production building (easiest pole).")]
		public readonly int ProductionScalingSpareEasy = 40;

		[Desc("BZ: net income per producer per tick (hardest pole).")]
		public readonly int ProductionScalingSpareHard = 15;

		[Desc("Only queue construction of a new building when above this requirement.")]
		public readonly int BuildingProductionMinCashRequirement = 1750;

		[Desc("Cash-starvation escape: a building queue whose wanted item has been held by the",
			"BuildingProductionMinCashRequirement floor this many ticks admits it at its own cost instead,",
			"so unit production draining the bank below the floor cannot starve construction forever.",
			"The item still needs cash >= its own cost; power, prerequisites and placement are unchanged.",
			"300 ticks (12 s at 25 tps, roughly two building sweeps) ignores the brief dip after a unit buy",
			"yet ends the observed 1500-tick idle spells quickly. 0 or less disables the escape (floor only).")]
		public readonly int BuildingCashStarvationTicks = 300;

		[Desc("Only queue construction of a new defense when above this requirement.")]
		public readonly int DefenseProductionMinCashRequirement = 2250;

		[Desc("RATIO-2X-01: a new static defence may start only if the mobile army value is at least 2x (static defence value + the new defence), "
			+ "by cost. The first copy of each defence type is exempt, and so is a start while the bank floats: storage at least half full, or every "
			+ "unit queue saturated with Cash+Resources at the tier's bank-saturation bar. A held defence waits and is never cancelled. Off by default; the R2X_mobile_static_ratio increment switch turns it on.")]
		public readonly bool MobileStaticRatioEnabled = false;

		[Desc("DEF-RATIO-PERS-01 (rulebook amendment 3, user 11-27): the RATIO-2X factor per personality (learned later as defence.ratio; these are the priors). Off = 2000 for every personality.")]
		public readonly bool DefRatioPersonality = false;

		[Desc("DEF-RATIO-PERS-01: the rush personality's RATIO-2X factor in permille (2000 = 2.0). Code default 2000.")]
		public readonly int DefRatioRush = 2000;

		[Desc("DEF-RATIO-PERS-01: the guerrilla personality's RATIO-2X factor in permille. Code default 2000.")]
		public readonly int DefRatioGuerrilla = 2000;

		[Desc("DEF-RATIO-PERS-01: the steamroller personality's RATIO-2X factor in permille. Code default 2000.")]
		public readonly int DefRatioSteamroller = 2000;

		[Desc("DEF-RATIO-PERS-01: the expansion personality's RATIO-2X factor in permille. Code default 2000.")]
		public readonly int DefRatioExpansion = 2000;

		[Desc("DEF-RATIO-PERS-01: the tech personality's RATIO-2X factor in permille. Code default 2000.")]
		public readonly int DefRatioTech = 2000;

		[Desc("DEF-RATIO-PERS-01: the turtle personality's RATIO-2X factor in permille. Code default 2000.")]
		public readonly int DefRatioTurtle = 2000;

		[Desc("DEF-RATIO-PERS-01 / audit A4: the early-defence waiver ends this many ticks after the first production building stands (or when the early count is built). Prior 3000 (the existing MaxStartOffsetTicks); the B3 timing prior is not located in the fleet docs.")]
		public readonly int EarlyDefWaiverTicks = 3000;

		[Desc("PROD-BUSY-01 (USER P0): every building/defence producer of a category may start an item in the same sweep (the old rule: one",
			"start per category per sweep, so a second construction yard waited). Cash gates are unchanged. Gated by SwitchCondition; row",
			"pb_all_building_producers.")]
		public readonly bool ServeAllBuildingProducers = false;

		[Desc("PROD-BUSY-01 (USER P0): a sweep that started nothing re-checks after StructureProductionActiveDelay instead of",
			"StructureProductionInactiveDelay, so a queue idles at most the short delay once cash or a prerequisite arrives. Gated by",
			"SwitchCondition; row pb_building_idle_sweep.")]
		public readonly bool BuildingIdleSweepActive = false;

		[Desc("ESSENTIALS-01 (USER 11-24): below the tier MaximiseProductionCash line (BotLimits) the base builder admits only essentials: each available",
			"building once, tech-chain prerequisites, refineries, power while short, passive-income generators, and planner/demand-driven starts (threat",
			"defence). Extras wait for the bank to reach the line. Gated by SwitchCondition; row pb_essentials_low_cash.")]
		public readonly bool EssentialsOnlyBelowTierLine = false;

		[Desc("UPGRADE-PRODUCERS-01 (USER P0, V03: upgrade and research producers such as the GDI communication center were never built, so no upgrade ever",
			"started): every buildable structure type with NO BuildingFractions row is built once when its prerequisites are met (cheapest first), is an",
			"army-first essential while none is owned or queued, and passes the low-cash hold as a first copy. Walls, defences, fakes, proxies, construction",
			"yards, refineries, power plants and silos are excluded (they have their own rules). Gated by SwitchCondition; row af2_unlisted_once.")]
		public readonly bool UnlistedBuildingsOnce = false;

		[Desc("ESSENTIAL-PASS-01 (USER P0, Opus 11-24): the first copy of every building type (not a wall, defence, fake or proxy) and every cash trickler is an",
			"army-first essential, so neither the projected-share and reserve votes nor the mobile-spend reserve hold it (the low-cash hold already passes it).",
			"Gated by SwitchCondition; row af2_essential_pass.")]
		public readonly bool EssentialPassFirstCopies = false;

		[Desc("BASE-02 (USER standard order, a soft guideline): power, barracks, refinery, war factory, refinery, 2nd barracks OR 2nd war factory, radar, airfield, the other 2nd",
			"production building, tech center, 3rd production buildings, passive income. For every faction each step is mapped through the building role lists and skipped when",
			"the faction lacks it. The next missing step outranks the wanted-refinery pick, random extras and sustained cash; power is inserted when short; threat defence and an",
			"expansion yard interrupt it; the planner takes over afterwards. Gated by SwitchCondition; row opening_standard_order.")]
		public readonly bool OpeningStandardOrder = false;

		[Desc("BASE-02: world ticks after which the standard order stops forcing picks.")]
		public readonly int OpeningStandardMaxTicks = 20000;

		[Desc("BASE-02: a step that has been the next step for this many ticks without being satisfied is skipped (no claimable refinery, no space): the order never deadlocks.")]
		public readonly int OpeningStepStuckTicks = 3000;

		[Desc("BASE-02: factions whose 2nd production building is a barracks before the 2nd war factory (every other faction: war factory first).")]
		public readonly FrozenSet<string> OpeningSecondBarracksFirstFactions = FrozenSet<string>.Empty;

		[Desc("BASE-03 (AUDIT_BASE_01, Opus ruling): while SUSTAINED-CASH is active, the first copy of every building type (the tech chain and each building once)",
			"still comes BEFORE the sustained production pick. Gated by SwitchCondition; row pb_sustained_keeps_essentials.")]
		public readonly bool SustainedKeepsEssentials = false;

		[Desc("BASE-05 (AUDIT_BASE_01): the overflow, production-scaling, threshold, naval, silo and radar picks are filtered by the same admission votes TickQueue asks",
			"BEFORE they are returned, so a held pick never ends the sweep and the lower picks still run. Gated by SwitchCondition; row pb_prefilter_picks.")]
		public readonly bool PrefilterBuildingPicks = false;

		[Desc("BASE-04 (AUDIT_BASE_01): ground defences from the fraction roll are built only after a threat is seen (an enemy armed unit seen near a construction yard,",
			"or the threat analysis reports an active threat or a nemesis); threat-driven defence stays. Gated by SwitchCondition; row af2_defence_need_gate.")]
		public readonly bool DefenceNeedGate = false;

		[Desc("BASE-04: cells around a construction yard in which a seen enemy armed unit counts as a threat.")]
		public readonly int DefenceNeedRadius = 28;

		[Desc("BASE-13 (AUDIT_BASE_01): caps the growing power margin (MinimumExcessPower + ExcessPowerIncrement per building group) at PowerMarginCap, never below",
			"MinimumExcessPower, so the low-power rung cannot outrank everything. Gated by SwitchCondition; row pb_power_margin_cap.")]
		public readonly bool PowerMarginCapEnabled = false;

		[Desc("BASE-13: the cap itself (power units).")]
		public readonly int PowerMarginCap = 300;

		[Desc("C-04 (AUDIT_CONFIG_01): the first copy of a defence type no longer skips RATIO-2X (mobile army >= 2x static defence) unless a threat is seen. Gated by",
			"SwitchCondition; row af2_defence_first_copy (shared with the ArmyFirstBotModule cap).")]
		public readonly bool DefenceFirstCopyCounts = false;

		[Desc("EARLY-DEF-01 (USER 11-24): right after the first production building the bot builds a small anti-rush set of ground defences (AntiRushBaseMin at the lowest tier,",
			"AntiRushBaseMax at the highest). The set is an essential at low cash and exempt from the defence-need gate, the seen-threat rule and the defence cash floor; the AF2 cap",
			"and RATIO-2X still count it. Gated by SwitchCondition; row bb_antirush_base.")]
		public readonly bool AntiRushBase = false;

		[Desc("EARLY-DEF-01: the anti-rush set size at the lowest tier.")]
		public readonly int AntiRushBaseMin = 2;

		[Desc("EARLY-DEF-01: the anti-rush set size at the highest tier.")]
		public readonly int AntiRushBaseMax = 3;

		[Desc("CONSOLIDATE-03 P1 (b), row early_def_learn (EARLY_DEF_LEARN_01_DESIGN.md, user 11-26): the anti-rush count = clamp(prior x M(map) x L, 0..EarlyDefCountMax), the prior",
			"being AntiRushBaseMin..Max on the tier line. M = reference / spawn path (EarlyDefReferencePathCells, 0 = neutral); L = the learned 4-value aggregate (neutral until a learned",
			"snapshot exists). Needs BbAntiRushSet; gated by SwitchCondition. Off = the prior, as before.")]
		public readonly bool EarlyDefLearn = false;

		[Desc("early_def_learn: the most early anti-rush defences the learned count may ask for.")]
		public readonly int EarlyDefCountMax = 4;

		[Desc("early_def_learn: the reference spawn-path length in cells for the map factor M (set from EARLY_RUSH_STATS_01). 0 = M neutral.")]
		public readonly int EarlyDefReferencePathCells = 0;

		[Desc("TEAM-DEFEND-02 (USER 11-24): while own or allied buildings are attacked, every ready ground turret is placed where it covers the current attackers: as close as possible to",
			"exactly its maximum weapon range from them, on the defended side, never next to the enemy; the nearest legal in-range cell otherwise; with none the turret is held (the READY",
			"watchdog places it at a base cell later). Solo (own base attacked) and team (an ally attacked) use the same rule; building next to an ally follows the lobby option through the",
			"engine placement check. Gated by SwitchCondition; row tm_ally_turret_support.")]
		public readonly bool TeamTurretSupport = false;

		[Desc("TURRET-NOHOLD-01 (row turret_no_hold, needs TeamTurretSupport for the support-cell case): a READY ground turret is never held. With no legal cell inside its weapon range it takes the",
			"legal cell of the attacked building's cluster nearest the attackers (never closer than the safe gap), and with none the normal own-base placement runs; the front/back advisor's",
			"hold is skipped for a turret at an outpost. Off = the old hold (the turret waits for the READY watchdog). Gated by SwitchCondition.")]
		public readonly bool TurretNoHold = false;

		[Desc("CONSOLIDATE-01 C9 (P7, X6): the unit-production pause for a missing refinery applies only until the first refinery has ever stood (the opening). After that a bot that loses",
			"its refineries keeps building units, because that is exactly when it needs them. Gated by SwitchCondition; row ub_refinery_pause_opening.")]
		public readonly bool RefineryPauseOpeningOnly = false;

		[Desc("TEAM-DEFEND-02: cells around an own or allied building in which a seen enemy armed unit counts as attacking it.")]
		public readonly int TurretSupportRadiusCells = 14;

		[Desc("TEAM-DEFEND-02: at least this many seen enemy armed units near one building count as an attack (a lone scout does not pull the turrets).")]
		public readonly int TurretSupportMinAttackers = 2;

		[Desc("TEAM-DEFEND-02: a turret keeps at least this many cells from the attackers (never next to the enemy), or three fifths of its range when that is smaller.")]
		public readonly int TurretSupportMinGapCells = 4;

		[Desc("TEAM-DEFEND-02: how often the attack picture is refreshed while a turret waits to be placed.")]
		public readonly int TurretSupportScanTicks = 25;

		[Desc("C-07 / BASE-09 (AUDIT_CONFIG_01): army-first essential buildings (yards, refineries, power, tech chain, first copies, upgrade producers) ride at their own",
			"cost like a planner want: the building cash floor (BuildingProductionMinCash) does not hold them. Gated by SwitchCondition; row pb_floor_essentials_exempt.")]
		public readonly bool FloorEssentialsExempt = false;

		[Desc("BASE-06 (AUDIT_BASE_01): a READY building held for crawl or front/back placement is placed at a base cell after ReadyWatchdogTicks (the old 75-tick watchdog covered only",
			"refineries). Gated by SwitchCondition; row ready_watchdog_all.")]
		public readonly bool ReadyWatchdogAll = false;

		[Desc("CONSOLIDATE-01 c1 (B07/X11): the buildings in flight on every building queue (and their power) are recounted every bot tick whatever the bank.",
			"Off = the old recount, which ran only at bank >= BuildingProductionMinCashRequirement, so below it the first-copy, limit and refinery counts",
			"missed buildings already being produced. Gated by SwitchCondition; row bb_recount_in_flight.")]
		public readonly bool RecountInFlightAlways = false;

		[Desc("CONSOLIDATE-01 c2: master switch of the staged build planner (BaseBuildPlanCA, CONSOLIDATE_01_DESIGN.md 3.1). Armed, each building and defence",
			"producer starts the first candidate of the ordered stages (c2: S1 power, S3 opening order, S10 fraction roll) that passes the feasibility tests A1-A7;",
			"a held candidate falls through to the next one, with no veto after the pick, and at most 8 tests per producer per sweep. Off = the old pick ladder",
			"and its post-pick vetoes, unchanged. Gated by SwitchCondition; row bb_pipeline_01.")]
		public readonly bool BbPipeline01 = false;

		[Desc("CONSOLIDATE-01 c4 (R8): stage S6 of the staged planner offers the cash-trickler (passive income) buildings up to their BuildingLimits value (a type with no",
			"limit once) and a silo when storage is nearly full, at reserve 0. Needs BbPipeline01. Off = S6 offers nothing. Gated by SwitchCondition; row bb_passive_income.")]
		public readonly bool BbPassiveIncome = false;

		[Desc("CONSOLIDATE-01 c5 (R3): stage S4 of the staged planner offers the anti-rush base defence set right after the first production building: ground defences, the type with",
			"the fewest copies first, then the cheapest, up to AntiRushBaseMin..AntiRushBaseMax by tier (the same set as AntiRushBase), at reserve 0. RATIO-2X never holds the",
			"set (the start is still recorded); the need gate and the seen-threat rule do not apply; it counts toward the defence cap. Needs BbPipeline01. Off = S4 offers",
			"nothing. Gated by SwitchCondition; row bb_antirush_set.")]
		public readonly bool BbAntiRushSet = false;

		[Desc("CONSOLIDATE-01 c5 (R6): stage S9 of the staged planner offers the extra defences (the armed defences of BuildingFractions, in a synced random order) after a threat is",
			"seen: defence need gate (extras only), the threat-scaled defence cap (DefenceCapBase), RATIO-2X with its FLOAT exemption. Needs BbPipeline01. Off = S9 offers nothing",
			"and the defences stay in the S10 fraction roll, which carries the same cap. Gated by SwitchCondition; row bb_extra_defence.")]
		public readonly bool BbExtraDefence = false;

		[Desc("CAPTURE-TECH-01: stage S5 of the staged planner also offers another faction's power plant, silo or refinery (the player owns one of that category, none of the type)",
			"when it provides a prerequisite the player lacks that gates another building type, so a captured construction yard unlocks its faction's tech. Needs BbPipeline01.",
			"Off = S5 is unchanged. Gated by SwitchCondition; row cs_capture_unlock.")]
		public readonly bool CsCaptureUnlock = false;

		[Desc("CONSOLIDATE-03 P1 (b), row bb_s5_capture_reason: with listed-only first copies (BbBuildAllListed) S5 still offers the capture-unlock types (CsCaptureUnlock) after the",
			"listed roll; the tech-depth fallback stays off. Off = S5 stops after the roll, so the capture rule never fires on the staged path. Needs both rows armed; gated by SwitchCondition.")]
		public readonly bool BbS5CaptureReason = false;

		[Desc("CONSOLIDATE-03 P2 (b), row cl_one_reserve: the S8-S10 building reserve (test A6) is the same personality-lean scaled line as the unit reserve",
			"(RecoverySpendPolicyCA.EffectiveReserve of the tier ProductionCashReserve, the steamroller lean): a trailing lead lowers it, nothing raises it, S1-S7 stay 0.",
			"Needs BbPipeline01. Off = the unscaled tier reserve. Gated by SwitchCondition.")]
		public readonly bool CashOneReserve = false;

		[Desc("TECHCHAIN-PAYGO-01: stages S3 (the tech-centre opening role) and S5 of the staged planner admit a TechChainTypes pick when the bank is above 0 instead of at",
			"its full cost (test A6), because the engine pays production as it goes. At most one tech-chain item in flight; every other pick keeps the full-cost test.",
			"Needs BbPipeline01. Off = A6 unchanged. Gated by SwitchCondition; row bb_techchain_paygo.")]
		public readonly bool BbTechChainPaygo = false;

		[Desc("BUILD-ALL-B (user ruling 2026-11-25): stage S5 of the staged planner builds every BuildingFractions entry that is buildable now once, and nothing else:",
			"no depth fallback of unlisted types (D2k concrete slabs remove themselves, so they never counted as owned and were offered again and again), the refinery",
			"included (a lost last refinery comes back as a first copy). Every S5 first copy rides with any bank above 0 while no other S5 first copy is in flight,",
			"instead of the TechChainTypes list. Needs BbPipeline01. Off = S5 and A6 unchanged. Gated by SwitchCondition; row bb_build_all_listed.")]
		public readonly bool BbBuildAllListed = false;

		[Desc("NO-CANCEL-01 (BS-03 c10 port, designs _17/_17B; Opus V08G): a building in production is never cancelled for a choice. A removal site keeps its existing",
			"condition and adds a guard: only a READY item with proof that no legal cell exists may still be removed (demand unwind, relocation latch, limit reached,",
			"refinery exhaustion, placement saturation, episode certificate). In-progress entries are held or finished instead. Gated by SwitchCondition; row bb_no_cancel (off = the six sites as before).")]
		public readonly bool BbNoCancel = false;

		[Desc("S8-STARVE-01 (Sol 58ADBC25, Opus V08G): under SUSTAINED cash an S8 (production cycle) type that is only OFFERED and then rejected (stage hold, failed feasibility, refused claim)",
			"no longer holds S9 (extra defence) and S10 (fractions) back for the sweep; they run through their ordinary checks. An admitted S8 type still wins first, an empty cycle,",
			"the low-cash cut, the feasibility budget and the S2/S7 refinery stages are unchanged. Gated by SwitchCondition; row bb_s8_rejected_fallthrough (off = the offer alone holds S9 and S10).")]
		public readonly bool BbS8RejectedFallthrough = false;

		[Desc("S8-CASH-HOLD (Opus V08G ruling, S8_STARVE_01 line 35): with bb_s8_rejected_fallthrough armed, an S8 production type that is eligible (buildable, within its limit, not delayed,",
			"no power or space refusal, not rejected earlier this walk) and fails ONLY the cash test while the bank is at least S8HoldMinBankPercent of its cost holds back the S10 fractions of the",
			"building queue for at most S8HoldMaxTicks (reason s8_cash_hold in the DEV rows). S9 defence, units and upgrades are never held. Gated by SwitchCondition; row bb_s8_cash_hold (off = no hold).")]
		public readonly bool BbS8CashHold = false;

		[Desc("S8-CASH-HOLD: the longest a cash-only S8 hold lasts, in ticks, counted from the first sweep that saw the type blocked. Learnable (DEV training only).")]
		public readonly int S8HoldMaxTicks = 750;

		[Desc("CASH-LIVENESS-01 (row cl_cap_spend; user D6, Opus 11-27): while this bot is at its unit-count cap (an IBotItemAwareUnitCap votes PauseUnitProduction,",
			"e.g. BotGlobalUnitBudget) with at least CapSpareCash banked, the S8-S10 building reserve is 0 (it keeps cash for units, which the cap stops) and the S9",
			"extra-defence need gate is waived. The AF2 defence cap, RATIO-2X and the admission votes still apply; upgrades never wait for the cap. Gated by",
			"SwitchCondition. Off in code.")]
		public readonly bool CashLivenessCapSpend = false;

		[Desc("CASH-LIVENESS-01: the bank (cash + resources) a capped bot needs before the cap spend applies. Learnable (DEV training only); clamped 0..20000.")]
		// TODO(LA02 integration, after the playtest): learnables row, prior 1000, clamp 0..20000, switch cl_cap_spend.
		public readonly int CapSpareCash = 1000;

		[Desc("S8-CASH-HOLD: the hold applies only when the bank is at least this percent of the blocked type's cost (an affordable-soon factory). Learnable (DEV training only).")]
		public readonly int S8HoldMinBankPercent = 75;

		[Desc("REF-PREFETCH-FRESH (Sol V08F_CUMREV_SOL_01 finding 2, Opus V08G): a relaxed (fallback spacing) refinery site promoted into the K-list before the building is READY",
			"is dropped at the READY flip (the candidate stream restarts its pass, so the next relaxed use rests on a current certificate), and a newly found strict site",
			"outranks any relaxed one whatever the score classes say. Strict sites and an issued order are untouched. Gated by SwitchCondition; row ref_prefetch_fresh (off = as before).")]
		public readonly bool RefPrefetchFresh = false;

		[Desc("CONSOLIDATE-03 P4 (PowerArbiter): the emergency power picks (stage S1 and the legacy low-power override) count the power of the plants already in production, so no second",
			"emergency plant is chosen while one is in flight. Gated by SwitchCondition; row pw_inflight_emergency (off = the raw surplus, as before).")]
		public readonly bool PwInflightEmergency = false;

		[Desc("CONSOLIDATE-03 P4 (PowerArbiter): a building is admitted by the power test when the plants in production cover its draw. Gated by SwitchCondition;",
			"row pw_inflight_admit (off = the raw surplus, as before).")]
		public readonly bool PwInflightAdmit = false;

		[Desc("BUILD-ALL-B (user ruling 2026-11-25): the base builder never queues a terrain foundation (D2k concrete: a building without health that removes",
			"itself unconditionally, FoundationExclusion) on any path, listed or not. Concrete use is planned by CONCRETE-PLAN-01 after the playtest.",
			"Off = as before. Gated by SwitchCondition; row bb_no_foundation.")]
		public readonly bool BbNoFoundation = false;

		[Desc("QUEUE-SPLIT-01 (user correction 2026-11-25): with a separate defence queue, the building-queue walk of the staged planner skips the defence-only",
			"stages (S4 anti-rush, S9 extra defences) and the defence-queue walk runs only S4, S9 and the S10 roll, never the refinery or economy stages.",
			"A faction whose defences share the building queue keeps today's order. Needs BbPipeline01. Off = as before. Gated by SwitchCondition; row bb_queue_split.")]
		public readonly bool BbQueueSplit = false;

		[Desc("REF-S2-01 (Opus ruling 2026-11-25, V08C RCA: S7 was rarely reached): with REF-DEMAND active, stage S2 of the building queue also serves unmet refinery",
			"zone demand through the wanted-refinery helper (its economy gate ref1 -> war factory -> ref2 and interval stay), one refinery in flight per player.",
			"Off = S2 only for a yard without a refinery. Gated by SwitchCondition; row bb_refinery_s2.")]
		public readonly bool BbRefineryAtS2 = false;

		[Desc("REF2-FIRST-01 (USER BINDING 11-26, Opus): at least 2 refineries early. While fewer than 2 own refineries stand or are in production and a reachable field has room, the second refinery",
			"(1) lifts the planner's zone cap by one, (2) outranks the refinery interval wait (the first-war-factory wait stays: ref1, war factory, ref2), (3) is placed BEFORE a radar provider",
			"(TECH personality exempt; row ref2_before_radar) and BEFORE the 2nd or later production building of a role (RUSH personality exempt; row ref2_before_prod2). Never cancels; the hold ends",
			"when ref2 is queued, after Ref2StallTicks startable without a refinery starting (row ref2_stall_release) or after Ref2FirstBoundTicks. No reachable field under its cap = no hold. Gated by SwitchCondition; rows ref2_before_radar, ref2_before_prod2, ref2_floor, ref2_stall_release.")]
		public readonly bool Ref2First = false;

		[Desc("REF2-FIRST-01: ticks after ref2 first became due at which the radar / 2nd production hold ends for good (the order never deadlocks).")]
		public readonly int Ref2FirstBoundTicks = 3000;

		[Desc("REF2-FIRST-01, row ref2_stall_release: ticks ref2 may stay startable (due, and the first-war-factory wait of the refinery economy gate is over) without a refinery starting before the",
			"radar / 2nd production hold ends for good (the old order runs). The outer Ref2FirstBoundTicks bound stays. Rides the Ref2First switch.")]
		public readonly int Ref2StallTicks = 750;

		[Desc("REF-TIER-01 (USER BINDING 11-26, Opus): the refinery target is a FLOOR for refinery demand, not a hold: RefTierEarlyTarget early, plus RefTierRadarStep once an own radar provider stands, plus",
			"RefTierTechStep once an own tech-centre building (TechChainTypes) stands (2 / 4 / 6), scaled by the personality prior (RefTierExpansionPct for expansion, RefTierRushPreRadarPct for rush while no radar",
			"stands; row ref_tier_personality), rounded and clamped 1..8. While fewer own refineries stand or are in production the planner lifts every zone cap by one and the refinery interval wait is outranked",
			"(the first-war-factory wait, the bounded first-copy yield and the expansion-refinery rules stay). The map caps it: only reachable fields with room and a claimable anchor are lifted. A lost radar or tech",
			"centre drops the tier; refineries above the target are never sold. Gated by SwitchCondition; rows ref_tier_floor, ref_tier_personality.")]
		public readonly bool RefTierFloor = false;

		[Desc("REF-TIER-01: the refinery target before the radar provider stands (the prior the learning can replace).")]
		public readonly int RefTierEarlyTarget = 2;

		[Desc("REF-TIER-01: refineries added to the target once an own radar provider stands.")]
		public readonly int RefTierRadarStep = 2;

		[Desc("REF-TIER-01: refineries added to the target once an own tech-centre building stands.")]
		public readonly int RefTierTechStep = 2;

		[Desc("REF-TIER-01, row ref_tier_personality: the expansion personality's percent of the tier target (125 = x1.25).")]
		public readonly int RefTierExpansionPct = 125;

		[Desc("REF-TIER-01, row ref_tier_personality: the rush personality's percent of the tier target while no radar provider stands (75 = x0.75; 2 x 0.75 rounds back to 2, the ref2 minimum).")]
		public readonly int RefTierRushPreRadarPct = 75;

		[Desc("MODE-BIAS-01 (user ruling 2026-11-25), profile essentials: while the low-cash essentials logic is active (EssentialsOnlyBelowTierLine armed, a tier line, bank under it), the BuildingFractions",
			"weights of the types that logic does not hold (EssentialsLowCashCA.Holds) are raised by MbEssentialsStrength in the staged planner's roll. Never a gate: every entry keeps a positive weight.",
			"Needs BbPipeline01. Off = unchanged. Gated by SwitchCondition; row mb_essentials.")]
		public readonly bool MbEssentials = false;

		[Desc("MODE-BIAS-01, profile techchain: while a TechChainTypes type is buildable and none is owned or queued, those types are raised by MbTechChainStrength. Needs BbPipeline01. Row mb_techchain.")]
		public readonly bool MbTechChain = false;

		[Desc("MODE-BIAS-01, profile antirush: while the anti-rush base set is wanted (AntiRushCA.Wants), the ground defences are raised by MbAntiRushStrength. Needs BbPipeline01. Row mb_antirush.")]
		public readonly bool MbAntiRush = false;

		[Desc("MODE-BIAS-01, profile opening: while the opening standard order is active, the types of its next open role are raised by MbOpeningStrength. Needs BbPipeline01. Row mb_opening.")]
		public readonly bool MbOpening = false;

		[Desc("MODE-BIAS-01: the milli multiplier (1000 = neutral) of each mode bias profile on the weights of its types. Initial values of ModeBiasParams; a learner may override them per player.")]
		public readonly int MbEssentialsStrength = 2000;

		[Desc("MODE-BIAS-01: see MbEssentialsStrength.")]
		public readonly int MbTechChainStrength = 2000;

		[Desc("MODE-BIAS-01: see MbEssentialsStrength.")]
		public readonly int MbAntiRushStrength = 2000;

		[Desc("MODE-BIAS-01: see MbEssentialsStrength.")]
		public readonly int MbOpeningStrength = 2000;

		[Desc("MODE-BIAS-01: the cap (milli) of the composed multiplier of all active profiles on one type.")]
		public readonly int MbStrengthCap = 20000;

		[Desc("CONSOLIDATE-01 c5: the threat-scaled defence cap of the staged planner (the ARMY-FIRST-02 cap, DefenceCapCA): a defence is held while the armed defences owned or in",
			"flight number at least this base plus DefenceCapPerThreat x the threat intensity. -1 = no cap. Row af2_defence_cap (shared with the ArmyFirstBotModule cap).")]
		public readonly int DefenceCapBase = -1;

		[Desc("CONSOLIDATE-01 c5: extra defences allowed at full threat intensity (IBotThreatAnalysis, the highest of the anti-air, anti-armour and anti-infantry intensities, 0..1). Rounded down.")]
		public readonly int DefenceCapPerThreat = 0;

		[Desc("CONSOLIDATE-01 c5: saturation of the threat intensity, in percent of the react threshold (300 = intensity 1 at 3x the threshold).")]
		public readonly int DefenceThreatSaturationPercent = 300;

		[Desc("CONSOLIDATE-01 c5: while the personality is turtle and the game is before TurtleDefenceUntilTick the cap is scaled by this percent. 100 = no turtle bonus.")]
		public readonly int TurtleDefenceCapPercent = 100;

		[Desc("CONSOLIDATE-01 c5: end of the turtle defence bonus (world tick).")]
		public readonly int TurtleDefenceUntilTick = 0;

		[Desc("BASE-06: ticks a READY building may be held before it is placed at a base cell.")]
		public readonly int ReadyWatchdogTicks = 75;

		[Desc("BASE-07: a type that failed its placement budget is banned from the overflow, cycled, each-once, tech-chain, opening and fraction picks for NoSpaceBanTicks, or until a new base provider appears.",
			"Gated by SwitchCondition; row pb_nospace_ban.")]
		public readonly bool NoSpaceBan = false;

		[Desc("BASE-07: how long a no-space ban lasts.")]
		public readonly int NoSpaceBanTicks = 3000;

		[Desc("BASE-12: the expansion-refinery safety test iterates every wanting claim (the first safe one wins) and also gates the fraction roll's refinery. Needs ExpansionRefinerySafety.",
			"Gated by SwitchCondition; row pb_refinery_safety_all_claims.")]
		public readonly bool RefinerySafetyAllClaims = false;

		[Desc("REF-FIELD-02 (USER PRIORITY 11-24, refinery spam; AUDIT_BASE_01 BASE-01): the economy rung's refinery pick waits for the first vehicle",
			"factory after ref1 (ref1, war factory, ref2), keeps RefineryMinIntervalTicks between refineries once every yard has one, and from the third",
			"refinery on yields to a pending first copy for at most RefineryEssentialYieldTicks. Gated by SwitchCondition; row rf_economy_gate.")]
		public readonly bool RefineryEconomyGate = false;

		[Desc("REF-FIELD-02: minimum ticks between two refineries once every construction yard has one.")]
		public readonly int RefineryMinIntervalTicks = 1500;

		[Desc("REF-INTERVAL-LADDER (BS-03 step 4 port, USER VALUE 2100 - 150 r): the minimum ticks between two refineries are read from the difficulty ladder instead of the flat",
			"RefineryMinIntervalTicks: Noob 2250, Easiest 2100, Hard 1500, God 750 (equal steps). Needs RefineryEconomyGate. Gated by SwitchCondition; row rf_interval_ladder (off = the flat value).")]
		public readonly bool RefineryIntervalLadder = false;

		[Desc("REF-INTERVAL-LADDER: the interval at the easiest tier.")]
		public readonly int RefineryMinIntervalEasiest = 2100;

		[Desc("REF-INTERVAL-LADDER: the interval at the hardest tier.")]
		public readonly int RefineryMinIntervalHardest = 750;

		[Desc("REF-INTERVAL-LADDER: the interval for an owner that is not on the ladder (the hard value).")]
		public readonly int RefineryMinIntervalUnranked = 1500;

		[Desc("REF-FIELD-02: longest a refinery pick yields to a pending first copy before it may go ahead anyway.")]
		public readonly int RefineryEssentialYieldTicks = 1500;

		[Desc("REF-FIELD-02 c2 (CM FIX, Opus ruling): the war-factory wait before ref2 ends this many ticks after ref1 completed; it also ends at once when",
			"a vehicle factory type is no-space banned or used up its placement budget (MaximumFailedPlacementAttempts), or none is buildable.")]
		public readonly int RefineryFactoryWaitTicks = 1500;

		[Desc("REF-FIELD-02: a yard with no own refinery within this many cells skips the refinery economy gate (the expansion refinery is next).")]
		public readonly int RefineryYardRadiusCells = 14;

		[Desc("PROD-BUSY (USER 11-24): while the bank stays above the tier MaximiseProductionCash line the base builder adds production buildings, cycled",
			"across the types (barracks, war factory, airfield, naval yard with water access, then again), bounded by the build limits and by the failed-",
			"placement latch; power is added as needed. Gated by SwitchCondition; row pb_cycle_production.")]
		public readonly bool CycleProductionAboveLine = false;

		[Desc("SUSTAINED-CASH-01 (USER 11-24): when the bank stays above the tier MaximiseProductionCash line (BotLimits) for SustainedCashTicks, the NEXT",
			"building started is a production building (the pb_cycle_production pick), whatever the planner wants, the opening or the expansion demand say.",
			"Types at their build limit are skipped; with every type at its limit normal building resumes. Power stays allowed when the production building",
			"would leave power short. The defence queue is untouched. Gated by SwitchCondition; row pb_sustained_cash_production.")]
		public readonly bool SustainedCashProduction = false;

		[Desc("pb_expansion_refinery_safety (USER 11-24): the refinery wanted for an expansion yard goes into the very-next slot only while that yard is SAFE.",
			"Unsafe = the yard took damage from an enemy, or a visible (fog-honest) enemy combat unit is within ExpansionSafetyScanCells of it; it is safe",
			"again ExpansionSafetyQuietTicks after the last threat. While unsafe the refinery is dropped from the slot (not held) and the order goes on.",
			"Gated by SwitchCondition; row pb_expansion_refinery_safety.")]
		public readonly bool ExpansionRefinerySafety = false;

		[Desc("pb_expansion_refinery_safety: quiet ticks after the last threat before the expansion yard counts as safe again.")]
		public readonly int ExpansionSafetyQuietTicks = 750;

		[Desc("pb_expansion_refinery_safety: cells around the expansion yard scanned for visible enemy combat units.")]
		public readonly int ExpansionSafetyScanCells = 12;

		[Desc("SUSTAINED-CASH-01: ticks the bank must stay above the tier line, unbroken.")]
		public readonly int SustainedCashTicks = 1500;

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

		[Desc("E1 observational slice (lead-accepted 2026-10-17; SwitchCondition-gated like the other",
			"increment arms): record storage-capacity commitments (P = P_queued + P_pending) and emit",
			"would-be admission verdicts to the bot debug log. NEVER gates any admission — behaviour,",
			"orders, RNG and save output are unchanged either flag-state. PERMANENT DOUBLE-COUNT: a",
			"successfully emitted reservation never settles on queue-item arrival (no proven order-item",
			"correlation seam exists), so once its item lands the capacity counts TWICE — in P_queued",
			"and in P_pending — for the reservation's lifetime; conservative overcounting that can",
			"overstate would-be refusals but cannot affect actual behavior. In-memory only; resets on load.")]
		public readonly bool StorageAdmissionObservational = false;

		[Desc("E1 observational: base storage-capacity target term for the would-be verdict",
			"(Ctarget = min(ceiling, max(this, S + hysteresis)); admission uses strict <).",
			"0 = unset — terms-only lines, no verdict.")]
		public readonly int StorageAdmissionTarget = 0;

		[Desc("E1 observational: hard capacity ceiling term (admission at the bound is allowed, <=).",
			"0 = unset.")]
		public readonly int StorageAdmissionCeiling = 0;

		[Desc("E1 observational: hysteresis margin folded into Ctarget (max(base, S + this)). 0 = none.")]
		public readonly int StorageAdmissionHysteresis = 0;

		[Desc("E1 observational: cooldown term — the emitted verdict additionally requires the gap since",
			"the previous storage emit be >= this. Observed only; 0 = unset.")]
		public readonly int StorageAdmissionCooldownTicks = 0;

		public override object Create(ActorInitializer init) { return new BaseBuilderBotModuleCA(init.Self, this); }
	}

	public class BaseBuilderBotModuleCA : ConditionalTrait<BaseBuilderBotModuleCAInfo>, IGameSaveTraitData,
		IBotTick, IBotPositionsUpdated, IBotRespondToAttack, IBotRequestPauseUnitProduction, IBotSuggestRefineryProduction, INotifyActorDisposing,
		IBotPlacementVeto, IBotRef2First, IBotRefTier
	{
		public CPos GetRandomBaseCenter()
		{
			var randomConstructionYard = ConstructionYardBuildings.Actors.Where(a => !a.IsDead)
				.OrderBy(a => a.ActorID)
				.RandomOrDefault(BotRng.For(player, nameof(BaseBuilderBotModuleCA)));

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
					.OrderBy(a => a.ActorID)
					.RandomOrDefault(BotRng.For(player, nameof(BaseBuilderBotModuleCA)));

				if (matchingConstructionYard != null)
					return matchingConstructionYard.Location;
			}

			return GetRandomBaseCenter();
		}

		public CPos GetDefenseBaseCenter()
		{
			var defenceConstructionYard = DefenseCenter != null ? ConstructionYardBuildings.Actors.OrderBy(a => (DefenseCenter.Value - a.Location).LengthSquared)
				.ThenBy(a => a.ActorID)
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
		readonly ProductionCostCache costCache = new();
		internal int ProductionCost(ProductionQueue queue, ActorInfo actorInfo) => costCache.Get(queue, actorInfo);
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
		/// <summary>BASE-12: true when at least one provider wants a refinery at a field and none of those fields is safe (a want with no field is not gated).</summary>
		public bool ExpansionRefineryBlockedByAllUnsafe()
		{
			if (expansionTargetProviders == null)
				return false;

			var safe = new List<bool>();
			foreach (var provider in expansionTargetProviders)
			{
				if (!(provider.WantsRefineryAtExpansionTarget && (provider.ExpansionTarget != null || provider.RefineryLawActive)))
					continue;

				var field = provider.RefineryClaimTarget ?? provider.ExpansionTarget;
				safe.Add(field == null || ExpansionRefinerySafe(field.Value));
			}

			return BaseFix02CA.SafetyBlocksAll(safe);
		}

		/// <summary>BASE-12: the first wanting provider whose field is safe (or has no field); null when none. The refinery's SITE follows this claim, not the head claim.</summary>
		public IBotExpansionTargetProvider ExpansionWantsRefinerySafe()
		{
			if (expansionTargetProviders == null)
				return null;

			var wanting = new List<IBotExpansionTargetProvider>();
			var safe = new List<bool>();
			foreach (var provider in expansionTargetProviders)
			{
				if (!(provider.WantsRefineryAtExpansionTarget && (provider.ExpansionTarget != null || provider.RefineryLawActive)))
					continue;

				var field = provider.RefineryClaimTarget ?? provider.ExpansionTarget;
				wanting.Add(provider);
				safe.Add(field == null || ExpansionRefinerySafe(field.Value));
			}

			var index = BaseFix02CA.FirstSafe(safe);
			return index < 0 ? null : wanting[index];
		}

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

		// BZ_production_scaling: same switch-gate as ExpansionPrebuildEnabled — the shared @generic
		// instance serves classic too, so the arm is inert there (bit-identical A/B reference).
		public bool ProductionScalingEnabled => Info.UseProductionScaling && switchesActive;

		// PROD-BUSY-01: switch-gated like the arms above (the shared @generic instance serves classic too).
		public bool AllBuildingProducersArmed => Info.ServeAllBuildingProducers && switchesActive;
		public bool BuildingIdleSweepArmed => Info.BuildingIdleSweepActive && switchesActive;
		public bool ExpansionSafetyArmed => Info.ExpansionRefinerySafety && switchesActive;
		readonly Dictionary<uint, int> yardThreatTick = new();
		int nextYardScanTick;

		/// <summary>Records the last tick an enemy threatened an own expansion yard (a visible combat unit in range); damage is recorded in RespondToAttack.</summary>
		void RecordYardThreats(int now)
		{
			// The base centre is unknown (0,0) until UpdatedBaseCenter has run: then no yard can be told from the main one.
			if (now < nextYardScanTick || initialBaseCenter == default)
				return;

			nextYardScanTick = now + 25;
			var radius = WDist.FromCells(Info.ExpansionSafetyScanCells);
			foreach (var yard in ConstructionYardBuildings.Actors)
			{
				if (yard.IsDead || !yard.IsInWorld)
					continue;

				if (!ExpansionSafetyCA.IsExpansionYard((yard.Location - initialBaseCenter).LengthSquared, 28))
					continue;

				// Fog-honest: only enemy combat units this player can see right now.
				if (world.FindActorsInCircle(yard.CenterPosition, radius).Any(a => IsEnemyGroundUnit(a) && a.Info.HasTraitInfo<AttackBaseInfo>() && a.CanBeViewedByPlayer(player)))
					yardThreatTick[yard.ActorID] = now;
			}
		}

		/// <summary>
		/// False when the own expansion yard nearest to <paramref name="target"/> is unsafe. The main base, and a target with no live yard, are never gated.
		/// </summary>
		public bool ExpansionRefinerySafe(CPos target)
		{
			Actor nearest = null;
			var best = long.MaxValue;
			foreach (var yard in ConstructionYardBuildings.Actors)
			{
				if (yard.IsDead || !yard.IsInWorld)
					continue;

				var d = (yard.Location - target).LengthSquared;
				if (d < best)
				{
					best = d;
					nearest = yard;
				}
			}

			if (nearest == null || initialBaseCenter == default || !ExpansionSafetyCA.IsExpansionYard((nearest.Location - initialBaseCenter).LengthSquared, 28))
				return true;

			return ExpansionSafetyCA.Safe(world.WorldTick, yardThreatTick.TryGetValue(nearest.ActorID, out var t) ? t : -1, Info.ExpansionSafetyQuietTicks);
		}

		public bool SustainedCashArmed => Info.SustainedCashProduction && switchesActive;
		public readonly SustainedCashClock SustainedCash = new();
		public bool CycleProductionArmed => Info.CycleProductionAboveLine && switchesActive;
		public bool EssentialsLowCashArmed => Info.EssentialsOnlyBelowTierLine && switchesActive;
		public bool UnlistedOnceArmed => Info.UnlistedBuildingsOnce && switchesActive;
		public bool EssentialPassArmed => Info.EssentialPassFirstCopies && switchesActive;
		public bool OpeningStepArmed => Info.OpeningStandardOrder && switchesActive;
		public bool SustainedKeepsEssentialsArmed => Info.SustainedKeepsEssentials && switchesActive;
		public bool PrefilterArmed => Info.PrefilterBuildingPicks && switchesActive;
		public bool DefenceNeedArmed => Info.DefenceNeedGate && switchesActive;
		public bool PowerMarginCapArmed => Info.PowerMarginCapEnabled && switchesActive;
		public bool DefenceFirstCopyArmed => Info.DefenceFirstCopyCounts && switchesActive;
		public bool AntiRushBaseArmed => Info.AntiRushBase && switchesActive;
		public bool TeamTurretSupportArmed => Info.TeamTurretSupport && switchesActive;
		public bool TurretNoHoldArmed => Info.TurretNoHold && switchesActive;
		public bool FloorEssentialsExemptArmed => Info.FloorEssentialsExempt && switchesActive;
		public bool ReadyWatchdogArmed => Info.ReadyWatchdogAll && switchesActive;
		AblationLatch ablateRecountInFlight; // CONSOLIDATE-01 c1
		public bool RecountInFlightArmed => Info.RecountInFlightAlways && switchesActive && !ablateRecountInFlight.Off(player, AblationIds.RecountInFlight);
		AblationLatch ablateCostCacheVisit;
		public bool CostCacheOn => switchesActive && Info.CostCacheVisit && !ablateCostCacheVisit.Off(player, AblationIds.CostCacheVisit);
		AblationLatch ablatePipeline01; // CONSOLIDATE-01 c2
		public bool Pipeline01Armed => Info.BbPipeline01 && switchesActive && !ablatePipeline01.Off(player, AblationIds.BbPipeline01);
		// CONSOLIDATE-01 c3: the tech depth of every building type (prerequisite-graph distance from the construction yard), built once on the first S5
		// sweep by the queue manager and shared by this player's producers (BaseBuilderQueueManagerCA.CoreDepths). Null until then.
		public IReadOnlyDictionary<string, int> CoreTechDepth;
		BaseBuilderRuleMetadataCA ruleMetadata;
		internal BaseBuilderRuleMetadataCA RuleMetadata => ruleMetadata ??= BaseBuilderRuleMetadataCA.Capture(world.Map.Rules);
		AblationLatch ablatePassiveIncome; // CONSOLIDATE-01 c4
		public bool PassiveIncomeArmed => Info.BbPassiveIncome && switchesActive && !ablatePassiveIncome.Off(player, AblationIds.BbPassiveIncome);
		AblationLatch ablateAntiRushSet; // CONSOLIDATE-01 c5
		AblationLatch ablateEarlyDefLearn; // CONSOLIDATE-03 P1 (b)
		public bool EarlyDefLearnArmed => Info.EarlyDefLearn && AntiRushSetArmed && !ablateEarlyDefLearn.Off(player, AblationIds.EarlyDefLearn);
		AblationLatch ablateExtraDefence;
		AblationLatch ablateDefenceCap;
		AblationLatch ablateCaptureUnlock; // CAPTURE-TECH-01
		AblationLatch ablateS5CaptureReason; // CONSOLIDATE-03 P1 (b)
		AblationLatch ablateSemanticTechTiers;
		AblationLatch ablateCashOneReserve; // CONSOLIDATE-03 P2 (b)
		public bool CashOneReserveArmed => Info.CashOneReserve && switchesActive && !ablateCashOneReserve.Off(player, AblationIds.ClOneReserve);
		IBotPersonalityLeadProvider[] reserveLeadProviders;

		// The lean the unit reserve reads (UnitBuilderBotModuleCA.ReserveAdmits): the lowest steamroller lead lean of this player's providers, 1.0 when none answer.
		public double ReserveLean() =>
			BotPersonalityLeads.Lean(reserveLeadProviders ??= player.PlayerActor.TraitsImplementing<IBotPersonalityLeadProvider>().ToArray(), "steamroller");
		public bool S5CaptureReasonArmed => Info.BbS5CaptureReason && CaptureUnlockArmed && BuildAllListedArmed && !ablateS5CaptureReason.Off(player, AblationIds.BbS5CaptureReason);
		public bool CaptureUnlockArmed => Info.CsCaptureUnlock && switchesActive && !ablateCaptureUnlock.Off(player, AblationIds.CsCaptureUnlock);
		public bool SemanticTechTiersArmed => Info.SemanticTechTiers && switchesActive && !ablateSemanticTechTiers.Off(player, AblationIds.SemanticTechTiers);
		AblationLatch ablateTechChainPaygo; // TECHCHAIN-PAYGO-01
		public bool TechChainPaygoArmed => Info.BbTechChainPaygo && switchesActive && !ablateTechChainPaygo.Off(player, AblationIds.BbTechChainPaygo);
		AblationLatch ablateBuildAllListed; // BUILD-ALL-B
		public bool BuildAllListedArmed => Info.BbBuildAllListed && switchesActive && !ablateBuildAllListed.Off(player, AblationIds.BbBuildAllListed);
		AblationLatch ablateNoCancel; // NO-CANCEL-01
		public bool NoCancelArmed => Info.BbNoCancel && switchesActive && !ablateNoCancel.Off(player, AblationIds.BbNoCancel);
		AblationLatch ablateS8RejectedFallthrough; // S8-STARVE-01
		public bool S8RejectedFallthroughArmed => Info.BbS8RejectedFallthrough && switchesActive && !ablateS8RejectedFallthrough.Off(player, AblationIds.BbS8RejectedFallthrough);
		AblationLatch ablateS8CashHold; // S8-CASH-HOLD
		public bool S8CashHoldArmed => Info.BbS8CashHold && S8RejectedFallthroughArmed && !ablateS8CashHold.Off(player, AblationIds.BbS8CashHold);
		AblationLatch ablateCapSpend; // CASH-LIVENESS-01
		public bool CapSpendArmed => Info.CashLivenessCapSpend && switchesActive && !ablateCapSpend.Off(player, AblationIds.ClCapSpend);
		IBotRequestPauseUnitProduction[] unitCapVotes;
		int capSpareTick = -1;
		bool capSpare;

		/// <summary>CASH-LIVENESS-01 (cl_cap_spend): this bot is at its unit-count cap with spare cash. Once per tick; false with the row off.</summary>
		public bool UnitCapSpare()
		{
			if (!CapSpendArmed)
				return false;

			var now = world.WorldTick;
			if (now == capSpareTick)
				return capSpare;

			capSpareTick = now;
			unitCapVotes ??= player.PlayerActor.TraitsImplementing<IBotRequestPauseUnitProduction>().Where(v => v is IBotItemAwareUnitCap).ToArray();
			var holds = false;
			foreach (var v in unitCapVotes)
				if (v.IsTraitEnabled() && v.PauseUnitProduction)
				{
					holds = true;
					break;
				}

			return capSpare = CashLiveness.CapSpare(holds, playerResources.GetCashAndResources(), Info.CapSpareCash);
		}

		AblationLatch ablatePwInflightEmergency, ablatePwInflightAdmit; // CONSOLIDATE-03 P4
		public bool PwInflightEmergencyArmed => Info.PwInflightEmergency && switchesActive && !ablatePwInflightEmergency.Off(player, AblationIds.PwInflightEmergency);
		public bool PwInflightAdmitArmed => Info.PwInflightAdmit && switchesActive && !ablatePwInflightAdmit.Off(player, AblationIds.PwInflightAdmit);
		AblationLatch ablateRefPrefetchFresh; // REF-PREFETCH-FRESH
		public bool RefPrefetchFreshArmed => Info.RefPrefetchFresh && switchesActive && !ablateRefPrefetchFresh.Off(player, AblationIds.RefPrefetchFresh);
		AblationLatch ablateRefineryIntervalLadder; // REF-INTERVAL-LADDER
		public bool RefineryIntervalLadderArmed => Info.RefineryIntervalLadder && Info.RefineryEconomyGate && switchesActive && !ablateRefineryIntervalLadder.Off(player, AblationIds.RefineryIntervalLadder);

		/// <summary>The refinery interval this owner uses: the ladder value (2100 - 150 r) when the row is armed, else the flat Info value.</summary>
		public int RefineryMinIntervalEffective => RefineryIntervalLadderArmed
			? BotDifficultyLadder.Scale(Info.RefineryMinIntervalEasiest, Info.RefineryMinIntervalHardest, player, Info.RefineryMinIntervalUnranked)
			: Info.RefineryMinIntervalTicks;

		[System.Diagnostics.Conditional("CAMEO_DEV_LOGGING")]
		public void TraceNoCancel(bool rowOn, string tag, int site, string item, Actor producer)
		{
#if CAMEO_DEV_LOGGING
			if (rowOn)
				Log.Write("debug", $"AI ({player.ClientIndex}): {tag} site={site} item={item} producer={producer.ActorID} tick={world.WorldTick}");
#endif
		}
		AblationLatch ablateNoFoundation; // BUILD-ALL-B
		public bool NoFoundationArmed => Info.BbNoFoundation && switchesActive && !ablateNoFoundation.Off(player, AblationIds.BbNoFoundation);
		AblationLatch ablateQueueSplit; // QUEUE-SPLIT-01
		public bool QueueSplitArmed => Info.BbQueueSplit && switchesActive && !ablateQueueSplit.Off(player, AblationIds.BbQueueSplit);
		AblationLatch ablateRefineryAtS2; // REF-S2-01
		public bool RefineryAtS2Armed => Info.BbRefineryAtS2 && switchesActive && !ablateRefineryAtS2.Off(player, AblationIds.BbRefineryAtS2);
		// REF2-FIRST-01: one switch (Info.Ref2First), four rows. State shared by every queue manager of this module: the tick ref2 first became due (-1 = never) and whether the gate
		// has finished for good (ref2 reached, the stall release fired, or the bound ran out); Ref2StartableSince is the stall clock (-1 = ref2 not startable).
		AblationLatch ablateRef2Radar;
		AblationLatch ablateRef2Prod2;
		AblationLatch ablateRef2Floor;
		public bool Ref2RadarArmed => Info.Ref2First && switchesActive && !ablateRef2Radar.Off(player, AblationIds.Ref2BeforeRadar);
		public bool Ref2Prod2Armed => Info.Ref2First && switchesActive && !ablateRef2Prod2.Off(player, AblationIds.Ref2BeforeProd2);
		public bool Ref2FloorArmed => Info.Ref2First && switchesActive && !ablateRef2Floor.Off(player, AblationIds.Ref2Floor);
		AblationLatch ablateRef2Stall; // ref2_stall_release: the hold ends when ref2 is startable but no refinery starts within Ref2StallTicks
		public bool Ref2StallArmed => Info.Ref2First && switchesActive && !ablateRef2Stall.Off(player, AblationIds.Ref2StallRelease);
		public int Ref2DueSince = -1;
		public int Ref2StartableSince = -1;
		BaseBuilderQueueManagerCA ref2Sampler; // the one building manager that evaluates the stall clock (the clock has this single writer)
		bool ref2SamplerResolved;
		public bool Ref2Finished;

		// REF-TIER-01: one switch (Info.RefTierFloor), two rows. The target is recomputed every RefTier.RecomputeTicks ticks from the COMPLETED own radar providers and tech-centre buildings (the two indexes exist
		// only while the switch is on in the yaml), so a lost tier drops it within that many ticks. Nothing here sells or cancels anything.
		AblationLatch ablateRefTier;
		AblationLatch ablateRefTierPersonality;
		public bool RefTierArmed => Info.RefTierFloor && switchesActive && !ablateRefTier.Off(player, AblationIds.RefTierFloor);
		bool RefTierPersonalityArmed => RefTierArmed && !ablateRefTierPersonality.Off(player, AblationIds.RefTierPersonality);
		int refTierTick = -1;
		int refTierValue;
		IBotCurrentPersonality refTierPersonality;
		bool refTierPersonalityResolved;
		BaseBuilderQueueManagerCA refTierObserver; // DEV only: the one building manager that writes the ref_tier record
		bool refTierObserverResolved;

		public bool RefTierRadarStanding => radarBuildings != null && AIUtils.CountActorByCommonName(radarBuildings) > 0;
		public bool RefTierTechStanding => techChainBuildings != null && AIUtils.CountActorByCommonName(techChainBuildings) > 0;

		/// <summary>The personality percent of the tier target: the prior table of the Info (the one seam the learned value replaces later).</summary>
		public int RefTierPersonalityPct(bool radarStanding)
		{
			if (!refTierPersonalityResolved)
			{
				refTierPersonality = player.PlayerActor.TraitsImplementing<IBotCurrentPersonality>().FirstOrDefault();
				refTierPersonalityResolved = true;
			}

			return RefTier.PersonalityPct(RefTierPersonalityArmed, refTierPersonality?.CurrentPersonality, radarStanding, Info.RefTierExpansionPct, Info.RefTierRushPreRadarPct);
		}

		// DEF-RATIO-PERS-01: the RATIO-2X factor of this bot's personality, in permille. The yaml values are the priors; the learned defence.ratio replaces
		// them once LA02 lands (hasLearned false until then). Switch off or ablated = 2000 for every personality.
		AblationLatch ablateDefRatioPersonality;
		IBotCurrentPersonality defRatioPersonality;
		bool defRatioPersonalityResolved;

		// DEF-RATIO-PERS-01 / audit A4: the world tick the first production building stood (-1 = none yet). The early-defence waiver's tick leg reads it.
		public int EarlyFirstProductionTick = -1;

		public int DefRatioPermille()
		{
			if (!defRatioPersonalityResolved)
			{
				defRatioPersonality = player.PlayerActor.TraitsImplementing<IBotCurrentPersonality>().FirstOrDefault();
				defRatioPersonalityResolved = true;
			}

			var armed = Info.DefRatioPersonality && !ablateDefRatioPersonality.Off(player, AblationIds.DefRatioPersonality);
			var prior = DefenceRatioPersonality.PriorFor(defRatioPersonality?.CurrentPersonality,
				Info.DefRatioRush, Info.DefRatioGuerrilla, Info.DefRatioSteamroller, Info.DefRatioExpansion, Info.DefRatioTech, Info.DefRatioTurtle);
			return DefenceRatioPersonality.Resolve(armed, prior, false, 0);
		}

		public int RefTierTarget
		{
			get
			{
				// Recomputed every RefTier.RecomputeTicks ticks (the two index counts and the personality read are not free, and a tier changes on building time scales).
				var tick = world.WorldTick;
				if (refTierTick >= 0 && tick - refTierTick < RefTier.RecomputeTicks)
					return refTierValue;

				refTierTick = tick;
				var radar = RefTierRadarStanding;
				var refineryLaw = RefineryLawProvider();

				// REF-LEARN-01: the learned value replaces the BASE tier target only; the personality percent and the clamp stay on top of it.
				var tierBase = refineryLaw != null && refineryLaw.RefineryLearnActive
					? refineryLaw.RefineryLearnTierBase(RefTierTechStanding ? 3 : radar ? 2 : 1)
					: RefTier.BaseTarget(Info.RefTierEarlyTarget, Info.RefTierRadarStep, Info.RefTierTechStep, radar, RefTierTechStanding);
				refTierValue = RefTier.Target(tierBase, RefTierPersonalityPct(radar));
				return refTierValue;
			}
		}

		// DEV only (compiled out of a release build with its call): the first building manager writes the ref_tier record, spaced and deduped by its observer.
		[System.Diagnostics.Conditional("CAMEO_DEV_LOGGING")]
		void RefTierObserveDev()
		{
			if (!RefTierArmed)
				return;

			if (!refTierObserverResolved)
			{
				refTierObserverResolved = true;
				foreach (var builder in builders)
					if (builder != null && Info.BuildingQueues.Contains(builder.Category))
					{
						refTierObserver = builder;
						break;
					}
			}

			refTierObserver?.RefTierObserve();
		}
		AblationLatch ablateMbEssentials; // MODE-BIAS-01
		public bool MbEssentialsArmed => Info.MbEssentials && switchesActive && !ablateMbEssentials.Off(player, AblationIds.MbEssentials);
		AblationLatch ablateMbTechChain; // MODE-BIAS-01
		public bool MbTechChainArmed => Info.MbTechChain && switchesActive && !ablateMbTechChain.Off(player, AblationIds.MbTechChain);
		AblationLatch ablateMbAntiRush; // MODE-BIAS-01
		public bool MbAntiRushArmed => Info.MbAntiRush && switchesActive && !ablateMbAntiRush.Off(player, AblationIds.MbAntiRush);
		AblationLatch ablateMbOpening; // MODE-BIAS-01
		public bool MbOpeningArmed => Info.MbOpening && switchesActive && !ablateMbOpening.Off(player, AblationIds.MbOpening);
		// MODE-BIAS-01: the per-player strengths of the profiles (and the cap), read by the planner only through this table. Initialised from the Info on first use.
		ModeBiasParams modeBiasValues;
		public ModeBiasParams ModeBiasValues => modeBiasValues ??= new ModeBiasParams(Info.MbEssentialsStrength, Info.MbTechChainStrength, Info.MbAntiRushStrength, Info.MbOpeningStrength, Info.MbStrengthCap);
		// BUILD-ALL-B: whether a type is a terrain foundation, by name, filled on first sight (the traits never change in a game).
		public readonly Dictionary<string, bool> FoundationTypes = new(StringComparer.Ordinal);
		// CAPTURE-TECH-01: provided tokens and gates of every building type, no faction filter; built once on the first S5 sweep with the switch on. Null until then.
		public CaptureUnlockMapsCA CaptureUnlockMaps;
		public bool AntiRushSetArmed => Info.BbAntiRushSet && switchesActive && !ablateAntiRushSet.Off(player, AblationIds.BbAntiRushSet);
		public bool ExtraDefenceArmed => Info.BbExtraDefence && switchesActive && !ablateExtraDefence.Off(player, AblationIds.BbExtraDefence);
		public bool DefenceCapArmed => Info.DefenceCapBase >= 0 && switchesActive && !ablateDefenceCap.Off(player, AblationIds.Af2DefenceCap);
		public bool NoSpaceBanArmed => Info.NoSpaceBan && switchesActive;
		public bool RefinerySafetyAllClaimsArmed => Info.RefinerySafetyAllClaims && switchesActive;
		public bool RefineryEconomyGateArmed => Info.RefineryEconomyGate && switchesActive;

		// REF-FIELD-02 (row rf_economy_gate): shared by every queue manager of this module. The tick the refinery count (owned + in production)
		// last rose, the count it rose to, and the tick a pending first copy first made the refinery pick yield (-1 = none).
		public int RefineryGateLastRiseTick = -1;
		public int RefineryGateLastCount;
		public int RefineryGateYieldSince = -1;

		// REF-FIELD-02 c2: the tick the first own refinery stood (-1 = not yet), and whether a vehicle factory type used up its placement budget.
		public int RefineryGateFirstStoodTick = -1;
		public bool RefineryGateFactoryPlacementFailed;

		/// <summary>REF-FIELD-02: own refineries standing plus in production.</summary>
		public int RefineryCountWithQueued()
		{
			var n = AIUtils.CountActorByCommonName(RefineryBuildings);
			if (BuildingsBeingProduced != null)
				foreach (var r in Info.RefineryTypes)
					if (BuildingsBeingProduced.TryGetValue(r, out var q))
						n += q;

			return n;
		}

		/// <summary>REF-FIELD-02: an own vehicle factory stands or one is in production.</summary>
		public bool VehicleFactoryOwnedOrQueued()
		{
			if (AIUtils.CountActorByCommonName(factoryBuildings) > 0)
				return true;

			if (BuildingsBeingProduced != null)
				foreach (var f in Info.VehiclesFactoryTypes)
					if (BuildingsBeingProduced.TryGetValue(f, out var q) && q > 0)
						return true;

			return false;
		}

		/// <summary>REF-FIELD-02: an own construction yard stands with no own refinery within RefineryYardRadiusCells (the expansion refinery is next).</summary>
		public bool YardWithoutRefinery()
		{
			var r2 = (long)Info.RefineryYardRadiusCells * Info.RefineryYardRadiusCells;
			foreach (var yard in ConstructionYardBuildings.Actors)
			{
				if (yard.IsDead || !yard.IsInWorld)
					continue;

				var served = false;
				foreach (var refinery in RefineryBuildings.Actors)
				{
					if (!refinery.IsDead && refinery.IsInWorld && (refinery.Location - yard.Location).LengthSquared <= r2)
					{
						served = true;
						break;
					}
				}

				if (!served)
					return true;
			}

			return false;
		}

		// E1 observational slice (lead-accepted, observational ONLY): the switch-gated commitment
		// ledger. Never read by any admission decision — recording and verdict logging only.
		// Null until the first pass while observing; the lazy holder keeps off-state allocation-free.
		StorageAdmissionObservation storageObservation;
		internal bool StorageAdmissionObserving => Info.StorageAdmissionObservational && switchesActive;
		internal StorageAdmissionObservation StorageObservation => storageObservation ??= new StorageAdmissionObservation(player);

		// Per-sweep reconcile — flag-gated on the module so the call site stays unconditional-cheap.
		internal void StorageObservationReconcile()
		{
			if (StorageAdmissionObserving)
				storageObservation?.ReconcilePass();
		}

		public int ScalingCashBar => BotDifficultyLadder.Scale(Info.ProductionScalingCashEasy, Info.ProductionScalingCashHard, player, Info.ProductionScalingCashHard);
		public int ScalingPersistBar => BotDifficultyLadder.Scale(Info.ProductionScalingPersistEasy, Info.ProductionScalingPersistHard, player, Info.ProductionScalingPersistHard);
		public int ScalingSpareBar => BotDifficultyLadder.Scale(Info.ProductionScalingSpareEasy, Info.ProductionScalingSpareHard, player, Info.ProductionScalingSpareHard);

		// BZ: ticks the bank has held above ScalingCashBar — fed by UpdateProductionScaling (BotTick).
		int scalingCashHeldTicks;
		public int ScalingCashHeldTicks => scalingCashHeldTicks;

		// BZ: net income per tick (earned minus spent over the same window as projectedIncomePerTick).
		public long ScalingNetIncomePerTick => projectedIncomePerTick - projectedSpentPerTick;

		// BZ: the unit-production queue types this module's ProductionTypes buildings provide
		// (derived from the ruleset, sorted for a deterministic starved pick).
		string[] productionQueueTypes;
		public IReadOnlyList<string> ProductionQueueTypes => productionQueueTypes ??= Info.ProductionTypes
			.SelectMany(n => world.Map.Rules.Actors.TryGetValue(n, out var ai)
				? ai.TraitInfos<ProductionQueueInfo>().Select(q => q.Type)
				: Enumerable.Empty<string>())
			.Where(t => t != null)
			.Distinct()
			.OrderBy(t => t, StringComparer.Ordinal)
			.ToArray();

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

		// BZ_production_scaling: the matching spend window for the net-surplus signal.
		int incomeWindowStartSpent;
		int projectedSpentPerTick;

		readonly Stack<TraitPair<RallyPoint>> rallyPoints = [];
		BotPeriod assignRallyPoints; // STAGGER-06 B001 F1 K=max(2,Interval)
		BotPeriod checkBestResourceLocation; // STAGGER-06 B003 F1

		BotPeriod sellRefinery; // STAGGER-06 B007 F1
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

		// REFSCAN D-5: the site-first refinery search (TREATMENT); inert while the law provider does not enable it.
		RefinerySiteSearch refinerySearch;
		int currentBuilderIndex = 0;

		public readonly ActorIndex.OwnerAndNamesAndTrait<RefineryInfo> RefineryBuildings;
		readonly ActorIndex.OwnerAndNamesAndTrait<BuildingInfo> powerBuildings;
		public readonly ActorIndex.OwnerAndNamesAndTrait<BuildingInfo> ConstructionYardBuildings;
		readonly ActorIndex.OwnerAndNamesAndTrait<BuildingInfo> barracksBuildings;
		readonly ActorIndex.OwnerAndNamesAndTrait<BuildingInfo> factoryBuildings;
		public readonly ActorIndex.OwnerAndNamesAndTrait<BuildingInfo> ProductionBuildings;
		readonly ActorIndex.OwnerAndNamesAndTrait<BuildingInfo> radarBuildings; // REF-TIER-01: null unless Info.RefTierFloor
		readonly ActorIndex.OwnerAndNamesAndTrait<BuildingInfo> techChainBuildings; // REF-TIER-01: null unless Info.RefTierFloor

		// RATIO-2X-01: the per-player 2x tracker (the 3-tick start record and the first-copy ledger), shared by every queue manager so a
		// sibling queue cannot pass the same old measure. Pure state, no world access (MobileStaticRatioCA).
		public readonly MobileStaticRatioCA StaticRatio = new();

		BotLimits botLimits;
		int queueSweepDisabledFromTick = -1;
		int lastBotTick = -1;
		int refineryLimit;
		IBotScaleTargets[] scaleTargets;
		IBotBuildOrderKnobs[] buildOrderKnobs;

		public PowerManager PlayerPower { get; private set; }
		public int ExcessPower { get; private set; }

		// CONSOLIDATE-03 P4: the power of the plants in production (the part of ExcessPower added by the queue loop), read by PowerArbiter.Effective when the rows are armed.
		public int InFlightPower { get; private set; }

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
			if (info.RefTierFloor)
			{
				// REF-TIER-01: the radar providers of the ruleset (the same trait test as the opening order's radar role) and the tech-centre types, as indexes of the own completed buildings.
				radarBuildings = new ActorIndex.OwnerAndNamesAndTrait<BuildingInfo>(world,
					world.Map.Rules.Actors.Values.Where(a => a.HasTraitInfo<BuildingInfo>() && BaseBuilderQueueManagerCA.IsRadarProvider(a)).Select(a => a.Name).ToFrozenSet(), player);
				techChainBuildings = new ActorIndex.OwnerAndNamesAndTrait<BuildingInfo>(world, info.TechChainTypes, player);
			}
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

			// S2 F3: a category listed in both BuildingQueues and DefenseQueues (BuildingAddons) gets two
			// managers over one queue. The first-created one owns the observation state and the later
			// one borrows it, so each queue item is observed and emitted once. Observation only: both
			// still tick and decide exactly as before.
			var slotCategories = new List<string>(builders.Length);
			BaseBuilderQueueManagerCA CreateManager(string category)
			{
				var ownerSlot = BaseBuilderQueueEvalCA.ObservationOwnerSlot(slotCategories, category);
				slotCategories.Add(category);
				return new BaseBuilderQueueManagerCA(this, category, player, PlayerPower, playerResources, resourceLayer,
					ownerSlot >= 0 ? builders[ownerSlot] : null);
			}

			foreach (var building in Info.BuildingQueues)
				builders[i++] = CreateManager(building);

			foreach (var defense in Info.DefenseQueues)
				builders[i++] = CreateManager(defense);

			refinerySearch = new RefinerySiteSearch(this, world, player, resourceLayer);
		}

		/// <summary>REFSCAN: true iff a site-search episode owns this queue item (the queue sweep must not place it).</summary>
		public bool RefinerySearchOwns(ProductionItem item) => refinerySearch != null && refinerySearch.Owns(item);

		/// <summary>REFSCAN F2: the reservation mask at the queue's emission point (non-episode placements).</summary>
		public bool PlacementMasked(string item, CPos topLeft) =>
			refinerySearch != null && world.Map.Rules.Actors.TryGetValue(item, out var ai) && refinerySearch.Masked(ai, topLeft);

		/// <summary>REFSCAN: the queue manager that owns emission for a queue category (the observation owner first).</summary>
		internal BaseBuilderQueueManagerCA ManagerFor(string category)
		{
			foreach (var b in builders)
				if (b != null && b.Category == category)
					return b;

			return null;
		}

		// REFSCAN D-6 (a), engine 234178b0: the MCV deploy cell choice consults the same mask (own player only).
		bool IBotPlacementVeto.VetoesPlacement(Player p, CPos topLeft, ActorInfo building) =>
			p == player && refinerySearch != null && refinerySearch.Masked(building, topLeft);

		protected override void TraitEnabled(Actor self)
		{
			foreach (var builder in builders)
				if (builder != null)
					BaseBuilderQueueEvalCA.ResumeQueueSweepCooldown(ref builder.WaitCooldown,
						builder.WaitCooldownStarted, world.WorldTick, queueSweepDisabledFromTick);

			queueSweepDisabledFromTick = -1;
			RefreshBotLimits();

			// Avoid all AIs reevaluating assignments on the same tick, randomize their initial evaluation delay.
			assignRallyPoints = BotPeriod.F1(world.WorldTick, BotRng.For(player, nameof(BaseBuilderBotModuleCA)).Next(0, Info.AssignRallyPointsInterval), Math.Max(2, Info.AssignRallyPointsInterval));
			checkBestResourceLocation = BotPeriod.F1(world.WorldTick, BotRng.For(player, nameof(BaseBuilderBotModuleCA)).Next(0, Info.CheckBestResourceLocationInterval), Info.CheckBestResourceLocationInterval);
			sellRefinery = BotPeriod.F1(world.WorldTick, Info.SellRefineryInterval < 0 ? 0 : BotRng.For(player, nameof(BaseBuilderBotModuleCA)).Next(0, Info.SellRefineryInterval), Info.SellRefineryInterval);
		}

		protected override void TraitDisabled(Actor self)
		{
			queueSweepDisabledFromTick = BaseBuilderQueueEvalCA.QueueSweepPauseFromTick(world.WorldTick, lastBotTick);
		}

		void IBotPositionsUpdated.UpdatedBaseCenter(CPos newLocation)
		{
			initialBaseCenter = newLocation;
		}

		void IBotPositionsUpdated.UpdatedDefenseCenter(CPos newLocation)
		{
			DefenseCenter = newLocation;
		}

		bool IBotRequestPauseUnitProduction.PauseUnitProduction => !IsTraitDisabled && !HasMinimalRefineryCount() && !RefineryPauseLapsed();

		// C9 (ub_refinery_pause_opening): latched the first time the minimal refinery count is seen standing; a pure function of synced world state.
		bool refineryEverOwned;
		AblationLatch ablateRefineryPause;

		bool RefineryPauseLapsed()
		{
			if (!Info.RefineryPauseOpeningOnly || !switchesActive || ablateRefineryPause.Off(player, AblationIds.UnitRefineryPauseOpening))
				return false;

			return refineryEverOwned;
		}

		void IBotTick.BotTick(IBot bot)
		{
			costCache.BeginVisit(CostCacheOn);
			try
			{
				BotTickVisit(bot);
			}
			finally
			{
				costCache.EndVisit();
			}
		}

		void BotTickVisit(IBot bot)
		{
			lastBotTick = world.WorldTick;

			if (ExpansionSafetyArmed)
				RecordYardThreats(lastBotTick);

			// SUSTAINED-CASH-01: one observation per bot visit, so a dip below the line resets the run at once.
			if (SustainedCashArmed)
			{
				var sustainedLine = botLimits != null && botLimits.Info.MaximiseProductionCashRequirement >= 0 ? botLimits.Info.MaximiseProductionCashRequirement : 0;
				SustainedCash.Observe(lastBotTick, sustainedLine > 0 && playerResources.GetCashAndResources() > sustainedLine);
			}

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

			if (assignRallyPoints.TryFire(BotVisitClock.Of(bot)))
			{
				foreach (var rp in world.ActorsWithTrait<RallyPoint>().Where(rp => rp.Actor.Owner == player))
					rallyPoints.Push(rp);
			}
			else
			{
				// PERF: Spread out rally point assignments updates across multiple ticks.
				var updateCount = Exts.IntegerDivisionRoundingAwayFromZero(rallyPoints.Count, assignRallyPoints.Remaining(BotVisitClock.Of(bot).VisitTick));
				for (var i = 0; i < updateCount; i++)
				{
					var rp = rallyPoints.Pop();
					if (rp.Actor.Owner == player && !rp.Actor.Disposed)
						SetRallyPoint(bot, rp);
				}
			}

			if (checkBestResourceLocation.TryFire(BotVisitClock.Of(bot)) && resourceLayer != null)
			{

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

				foreach (var conyard in ConstructionYardBuildings.Actors.OrderBy(a => a.ActorID))
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
			InFlightPower = 0;
			for (int i = 0, builderIndex = currentBuilderIndex; i < builders.Length; i++)
			{
				if (++builderIndex >= builders.Length)
					builderIndex = 0;

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
					// CONSOLIDATE-01 c1: armed, the recount runs whatever the bank (InFlightRecountCA.Recounts).
					if (InFlightRecountCA.Recounts(RecountInFlightArmed, playerResources.GetCashAndResources(), Info.BuildingProductionMinCashRequirement))
					{
						foreach (var queue in queues)
						{
							// Record the number of the buildings.
							var producing = queue.AllQueued().FirstOrDefault();
							if (producing == null)
								continue;

							InFlightRecountCA.Record(BuildingsBeingProduced, producing.Item);

							// Record the power of the building.
							var producingPower = producing.ActorInfo.TraitInfos<PowerInfo>().Where(p => p.EnabledByDefault).Sum(pi => pi.Amount);
							ExcessPower += producingPower;
							InFlightPower += producingPower;
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

			if (ProductionScalingEnabled)
			{
				// BZ_production_scaling: the income window is shared with ECON-A — sample it even
				// when only the scaling arm is on, then feed the banked-cash persist counter.
				if (!ExpansionPrebuildEnabled)
					SampleExpansionIncome();

				UpdateProductionScaling();
			}

			// REFSCAN D-5 (sec 2): the search pump and the READY handoff run here, every bot visit, ahead of the
			// queue managers' rotation and queue-sweep cooldown — the placement path never waits on the queue sweep.
			refinerySearch?.Pump(bot, RefineryLawProvider());

			// REF2-FIRST-01, row ref2_stall_release (Haiku FIX, Opus ruling): the stall clock is sampled once per bot visit, unconditionally while armed and not finished, independent of the pick-time
			// callers (a not-startable spell can then never go unseen). Two flag reads on most visits; the sampler itself spaces its evaluations (Ref2First.SampleTicks) and reuses cached values.
			if (Ref2StallArmed && !Ref2Finished)
			{
				if (!ref2SamplerResolved)
				{
					ref2SamplerResolved = true;
					foreach (var builder in builders)
						if (builder != null && Info.BuildingQueues.Contains(builder.Category))
						{
							ref2Sampler = builder;
							break;
						}
				}

				ref2Sampler?.Ref2StallSample();
			}

			// REF-TIER-01 (DEV only): one record per change of the tier state.
			RefTierObserveDev();

			builders[currentBuilderIndex].Tick(bot);

			if (Info.SellRefineryInterval >= 0 && sellRefinery.TryFire(BotVisitClock.Of(bot)))
			{
				SellUselessRefinery(bot);
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

			// PUSH-1: the overflow policy carrier is resolved once like BotLimits; the rung re-checks
			// IsTraitEnabled() each evaluation so a mid-game condition flip disarms correctly.
			var overflowPolicy = player.PlayerActor.TraitsImplementing<BotOverflowPolicy>().FirstOrDefault();

			foreach (var builder in builders)
			{
				builder.SetBotLimits(botLimits);
				builder.SetOverflowPolicy(overflowPolicy);
			}
		}

		void IBotRespondToAttack.RespondToAttack(IBot bot, Actor self, AttackInfo e)
		{
			if (e.Attacker == null || e.Attacker.Disposed)
				return;

			if (e.Attacker.Owner.RelationshipWith(self.Owner) != PlayerRelationship.Enemy)
				return;

			// pb_expansion_refinery_safety: record only; an enemy hit on an own yard marks it unsafe.
			if (ExpansionSafetyArmed && Info.ConstructionYardTypes.Contains(self.Info.Name))
				yardThreatTick[self.ActorID] = world.WorldTick;

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

			if (BotRng.For(player, nameof(BaseBuilderBotModuleCA)).Next(100) < chanceThreshold)
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

			return possibleRallyPoints.Random(BotRng.For(player, nameof(BaseBuilderBotModuleCA)));
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
		public bool HasAdequateRefineryCount() => AdequateReason() != RefineryAdequacy.Inadequate;

		// CONSOLIDATE-03 P3: the first true sub-condition of the old HasAdequateRefineryCount, evaluated in the same short-circuit order (the law provider and ExpansionWantsRefinery are read in the same order).
		public RefineryAdequacy AdequateReason()
		{
			if (Info.RefineryTypes.Count == 0)
				return RefineryAdequacy.NoTypes;

			if (RefineryLawProvider() != null
				? ExpansionWantsRefinery() == null
				: AIUtils.CountActorByCommonName(RefineryBuildings) >= OptimalRefineryCount()
					&& ExpansionWantsRefinery() == null)
				return RefineryAdequacy.Met;

			if (AIUtils.CountActorByCommonName(powerBuildings) == 0)
				return RefineryAdequacy.NoPowerPlant;

			return AIUtils.CountActorByCommonName(ConstructionYardBuildings) == 0 ? RefineryAdequacy.NoYard : RefineryAdequacy.Inadequate;
		}

		int OptimalRefineryCount() =>
			AIUtils.CountActorByCommonName(ProductionBuildings) > 0
			? Info.InititalMinimumRefineryCount + Info.AdditionalMinimumRefineryCount + (AIUtils.CountActorByCommonName(ConstructionYardBuildings) - 1) * Info.RefineriesPerBase
			: Info.InititalMinimumRefineryCount;

		bool HasMinimalRefineryCount()
		{
			var has = AIUtils.CountActorByCommonName(RefineryBuildings) >= Info.InititalMinimumRefineryCount;
			if (has)
				refineryEverOwned = true;

			return has;
		}

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

			var cost = ProductionCost(queue, actorInfo);
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
						.Sum(a => ProductionCost(queue, a.Info));

				if (!openingBarracksCostCommitted)
				{
					var barracks = barracksBuildings.Actors.OrderBy(a => a.ActorID).FirstOrDefault(a => !a.IsDead);
					if (barracks != null)
					{
						openingBarracksCommittedCost = ProductionCost(queue, barracks.Info);
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
						ExpansionMcvLease.Release(leases, mcv);
						foreach (var builder in builders)
							BaseBuilderQueueEvalCA.MakeQueueSweepDue(ref builder.WaitCooldown, now);
					}
					else if (transition == BaseBuilderQueueEvalCA.ExpansionTransformTransition.Relocate)
					{
						// Relocation (conyard→MCV) or another non-yard transform: the journey continues
						// on the replacement actor — re-key both records and the lease so the demand
						// follows it.
						RequestedRefineries.Remove(mcv);
						RequestedRefineries[next] = (demand.ConyardLoc, demand.ResourceLoc);
						ExpansionDemands.Remove(mcv);
						ExpansionMcvLease.Release(leases, mcv);
						demand.Mcv = next;
						ExpansionDemands[next] = demand;
						if (!ExpansionMcvLease.Acquire(leases, next, Info.ExpansionDemandIdleTicks))
						{
							ExpireExpansionDemand(bot, demand);
							continue;
						}
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
					// ECON-A-FIX (R4): the expansion order issuer holds the MCV's lease while the journey is
					// ours — the spec's "active while the MCV holds the expansion lease" contract, claimed
					// at posting and re-claimed every sweep as the heartbeat. A foreign owner's lease
					// (takeover, emergency preempt) makes the claim fail; so does losing the unit.
					if (!ExpansionMcvLease.Acquire(leases, mcv, Info.ExpansionDemandIdleTicks))
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

					if (demand.NextEtaCooldown.Ready(now))
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
				// REPAIR-B3: the renewal refreshes the whole covered set under the current model
				// version — holds on members that drifted out of coverage lapse at their own expiry.
				if (demand.ReservedClaim is { })
					BaseBuilderQueueEvalCA.RenewRefineryReservation(
						RefineryLawProvider(), demand, now + Info.ExpansionDemandIdleTicks);

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
			// REPAIR-B3: owner-keyed release — the admission-time covered set may have drifted since
			// (members taken/covered/re-modelled); a recomputed set could leak a live hold.
			if (demand.ReservedClaim != null)
				BaseBuilderQueueEvalCA.ReleaseRefineryReservations(law, demand);

			// ECON-A-FIX (R4): the traveller's expansion-issuer lease is released on every
			// lapse path (the registry prunes a dead unit's lease on its own cadence regardless).
			ExpansionMcvLease.Release(BotUnitLeases.Of(player), demand.Mcv);

			if (demand.RefineryItem != null)
			{
				// REF-2 P1a (retained-refinery): a completed law-path refinery is never cancelled on
				// expiry — it converts to a normal queued refinery outright, and its own placement
				// sweeps retry preferred claims then the widened fallback forever. The old extra
				// clause `law.NextRefineryClaim(null) != null` gated retention on a claim existing
				// this instant AND silently spent that anchor's offer streak — both dropped.
				var readopted = law != null
					&& ItemDone(demand.RefineryProducer, demand.RefineryItem);
				if (!readopted)
				{
					// F2: held is not settled. A held cancel leaves the entry queued — the
					// demand retires and the queue layer's sweeps own the retained item's
					// placement retry (its protection gate keeps it queued; the placement
					// sweeps place it) — the same re-adoption route a readopted refinery
					// takes. Only Issued/Absent means the queue entry is gone.
					var disposition = CancelDemandItem(bot, demand.RefineryProducer, demand.RefineryItem);
					if (disposition is DemandCancelDisposition.AmbiguousHold or DemandCancelDisposition.ProtectionHold)
						AIUtils.BotDebug("AI ({0}): demand-unwind cancel of {1} on {2} held ({3}); the item stays queued and is re-adopted by the queue layer's placement sweep.",
							player.ClientIndex, demand.RefineryItem, demand.RefineryProducer, disposition);
				}

				demand.UnbindRefinery();
			}

			if (demand.DefenceItem != null)
			{
				var disposition = CancelDemandItem(bot, demand.DefenceProducer, demand.DefenceItem);
				if (disposition is DemandCancelDisposition.AmbiguousHold or DemandCancelDisposition.ProtectionHold)
					AIUtils.BotDebug("AI ({0}): demand-unwind cancel of {1} on {2} held ({3}); the item stays queued and is re-adopted by the queue layer's placement sweep.",
						player.ClientIndex, demand.DefenceItem, demand.DefenceProducer, disposition);

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

		/// <summary>
		/// F2 (Sol adjudication of 9d997eb58): the demand-unwind cancel is name-bound — the
		/// intended ProductionItem is provable ONLY when exactly one same-name entry exists
		/// across the producer's queues on an enabled queue (engine CancelProduction runs on
		/// every enabled ProductionQueue trait and removes the last same-name entry on each,
		/// so anything beyond unique either settles a sibling or removes several entries).
		/// A certificate alone never establishes request identity. Held ≠ settled: a held
		/// item stays queued and the queue layer's sweeps own its placement retry — the
		/// same re-adoption route a retained refinery takes when the demand retires.
		/// </summary>
		DemandCancelDisposition CancelDemandItem(IBot bot, Actor producer, string item)
		{
			if (producer == null || producer.Disposed || producer.IsDead)
				return DemandCancelDisposition.Absent;

			var sameName = 0;
			var onEnabledQueue = 0;
			foreach (var q in producer.TraitsImplementing<ProductionQueue>())
			{
				var n = q.AllQueued().Count(i => i.Item == item);
				sameName += n;
				if (q.Enabled)
					onEnabledQueue += n;
			}

			world.Map.Rules.Actors.TryGetValue(item, out var cancelInfo);
			var disposition = BaseBuilderQueueEvalCA.DemandCancelVerdict(sameName, onEnabledQueue,
				BaseBuilderQueueEvalCA.EvaluatePlacementCancelProtection(cancelInfo,
					Info.RefineryTypes.Contains(item),
					Info.PowerTypes.Contains(item) || Info.BarracksTypes.Contains(item),
					null, null, null, null),
				RefineryLawProvider()?.RefineryFallbackCertificateCurrent(producer, item) ?? false);
			if (disposition != DemandCancelDisposition.Issued)
				return disposition;

			// NO-CANCEL-01 (row bb_no_cancel, site 1, designs _17/_17B): the certificate proves "no legal cell" only for a READY entry, and it is keyed by
			// item name, so it can still be current while the single same-name entry is a new one in progress. Row on: only a Done entry (the instance
			// Order.CancelProduction would remove) may be cancelled; an in-progress one becomes a ProtectionHold (the item stays queued, the demand retires,
			// the queue layer's placement sweep owns it) before any record or order. Row off = the code below byte for byte.
			var noCancelOn = NoCancelArmed;
			var entryDone = false;
			if (noCancelOn)
			{
				foreach (var q in producer.TraitsImplementing<ProductionQueue>())
				{
					if (!q.Enabled)
						continue;

					var bound = BaseBuilderQueueEvalCA.CancelBindTarget(q.AllQueued(), i => i.Item, item, null);
					if (bound != null)
					{
						entryDone = bound.Done;
						break;
					}
				}
			}

			var gated = NoCancelGatesCA.DemandCancelGate(disposition, noCancelOn, entryDone);
			if (gated != DemandCancelDisposition.Issued)
			{
				TraceNoCancel(noCancelOn, "CANCEL-SUPPRESSED", 1, item, producer);
				return gated;
			}

			// REPLAY-HEALTH-LOGGER (F4): register the demand cancel so the owning manager's
			// probe resolves it as Cancelled/DemandCancel instead of an UNKNOWN removal.
			if (builders != null)
				foreach (var b in builders)
					b?.NoteExternalCancel(producer, item);

			TraceNoCancel(noCancelOn, "CANCEL-UNPLACEABLE", 1, item, producer);
			bot.QueueOrder(Order.CancelProduction(producer, item, 1));
			return DemandCancelDisposition.Issued;
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
				// REPAIR-B3: owner-keyed release frees the whole (possibly drifted) reserved set.
				if (demand.ReservedClaim != null)
					BaseBuilderQueueEvalCA.ReleaseRefineryReservations(RefineryLawProvider(), demand);

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
			demand.NextEtaCooldown.Start(now, Info.ExpansionEtaIntervalTicks);
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
				incomeWindowStartSpent = playerResources.Spent;
				return;
			}

			var elapsed = now - incomeWindowStartTick;
			if (elapsed < Info.ExpansionIncomeWindowTicks)
				return;

			projectedIncomePerTick = Math.Max(0, (playerResources.Earned - incomeWindowStartEarned) / elapsed);
			projectedSpentPerTick = Math.Max(0, (playerResources.Spent - incomeWindowStartSpent) / elapsed);
			incomeWindowStartTick = now;
			incomeWindowStartEarned = playerResources.Earned;
			incomeWindowStartSpent = playerResources.Spent;
		}

		/// <summary>BZ_production_scaling: the banked-cash persistence counter — an uninterrupted
		/// run above the difficulty-line bar; any dip below resets it. Ticks only under the arm.</summary>
		void UpdateProductionScaling()
		{
			if (playerResources.GetCashAndResources() > ScalingCashBar)
				scalingCashHeldTicks++;
			else
				scalingCashHeldTicks = 0;
		}

		// S2 (FINDINGS_2026-10-12 §4): dedicated anti-air defences only after the bot has
		// fog-honestly observed an enemy air capability — a remembered aircraft or a
		// remembered aircraft-producing structure. No air-intel provider (classic) or a
		// provider not observing through fog keeps the pre-gate behaviour. Cached per tick:
		// one build evaluation may check several fraction entries.
		int airIntelTick = -1;
		bool airIntelOpen = true;
		HashSet<string> gatedAirDefences;

		internal bool DedicatedAntiAirAllowed
		{
			get
			{
				if (airIntelTick != world.WorldTick)
				{
					airIntelTick = world.WorldTick;
					var provider = player.PlayerActor.TraitsImplementing<IBotEnemyAirIntelProvider>().FirstEnabledTraitOrDefault();
					airIntelOpen = provider == null || !provider.TryGetEnemyAirIntel(out var observed) || observed;
				}

				return airIntelOpen;
			}
		}

		/// <summary>The dedicated-AA members of <see cref="BaseBuilderBotModuleCAInfo.AntiAirTypes"/> — every
		/// armament air-only. Dual-role air/ground defences stay ungated (FINDINGS S2: the FutureTech
		/// multiturret is the ground defence too). Ruleset-derived once.</summary>
		internal bool IsGatedAirDefence(string name)
		{
			if (gatedAirDefences == null)
			{
				gatedAirDefences = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
				foreach (var n in Info.AntiAirTypes)
					if (world.Map.Rules.Actors.TryGetValue(n, out var ai) && BaseBuilderQueueEvalCA.IsDedicatedAntiAir(ai))
						gatedAirDefences.Add(n);
			}

			return gatedAirDefences.Contains(name);
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
				// S2: dedicated anti-air waits for fog-fair enemy air intel — the demand's
				// defence slot then takes the strongest anti-ground pick instead.
				if (!BaseBuilderQueueEvalCA.AirDefenceAdmits(IsGatedAirDefence(name), DedicatedAntiAirAllowed))
					continue;

				if (!world.Map.Rules.Actors.TryGetValue(name, out var info) || !info.HasTraitInfo<AttackBaseInfo>())
					continue;

				int? cost = null;
				foreach (var category in Info.DefenseQueues)
				{
					foreach (var queue in AIUtils.FindQueues(player, category))
					{
						if (!queue.BuildableItems().Any(b => b.Name == name))
							continue;

						var itemCost = ProductionCost(queue, info);
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

			var data = new List<MiniYamlNode>()
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

			var starvationManagers = builders.Select((builder, index) => (builder, index))
				.Where(entry => entry.builder.TrackedCashStarvationRuns > 0)
				.Select(entry => new MiniYamlNode("Manager", "", new List<MiniYamlNode>
				{
					new("Index", FieldSaver.FormatValue(entry.index)),
					new("Category", FieldSaver.FormatValue(entry.builder.Category)),
					new("Runs", "", entry.builder.SaveCashStarvationRuns())
				})).ToList();
			if (starvationManagers.Count > 0)
				data.Add(new MiniYamlNode("CashStarvationClocks", "", starvationManagers));

			return data;
		}

		void IGameSaveTraitData.ResolveTraitData(Actor self, MiniYaml data)
		{
			if (self.World.IsReplay)
				return;

			var starvationClocksNode = data.NodeWithKeyOrDefault("CashStarvationClocks");
			if (starvationClocksNode != null)
			{
				foreach (var builder in builders)
					builder.RestoreCashStarvationRuns(Enumerable.Empty<MiniYamlNode>());

				var restoredManagers = new HashSet<int>();
				foreach (var managerNode in starvationClocksNode.Value.Nodes)
				{
					var fields = managerNode.Value.ToDictionary();
					var index = FieldLoader.GetValue<int>("Index", fields["Index"].Value);
					var category = FieldLoader.GetValue<string>("Category", fields["Category"].Value);
					if (index < 0 || index >= builders.Length || builders[index].Category != category || !restoredManagers.Add(index))
						throw new InvalidDataException("Invalid or duplicate cash-starvation manager save state.");

					var runsNode = managerNode.Value.NodeWithKeyOrDefault("Runs");
					if (runsNode != null)
						builders[index].RestoreCashStarvationRuns(runsNode.Value.Nodes);
				}
			}

			var initialBaseCenterNode = data.NodeWithKeyOrDefault("InitialBaseCenter");
			if (initialBaseCenterNode != null)
				initialBaseCenter = FieldLoader.GetValue<CPos>("InitialBaseCenter", initialBaseCenterNode.Value.Value);

			var defenseCenterNode = data.NodeWithKeyOrDefault("DefenseCenter");
			if (defenseCenterNode != null)
				DefenseCenter = FieldLoader.GetValue<CPos?>("DefenseCenter", defenseCenterNode.Value.Value);

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
			refinerySearch?.Dispose();
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
			demand.NextEtaCooldown.Start(world.WorldTick, 0);

			// ECON-A-FIX (R4): the demand publishes only while the expansion order issuer's lease holds on the
			// traveller — a first claim refused by a foreign owner's lease means this journey is not ours
			// to schedule, so the demand never posts (the plain request record still stands). On an
			// existing demand a failed re-claim is left to the sweep, which expires it the same tick.
			if (!ExpansionMcvLease.Acquire(BotUnitLeases.Of(player), expandActor, Info.ExpansionDemandIdleTicks) && isNew)
				ExpansionDemands.Remove(expandActor);
		}
	}
}
