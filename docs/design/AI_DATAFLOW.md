# AI dataflow — the whole stack, end to end

2026-10-02. The one-page map of every bot module, who publishes, who consumes,
who decides, who orders. Pairs with `AI_MODULE_MAP.md` (which list the files);
this doc is about the *flow* — the round trip from observation to order to
telemetry that the branch-coverage audit walks.

## The round trip

```
                    ┌─────────────────────────────────────────────┐
                    │  OBSERVE (once per bot tick)                │
                    │                                             │
                    │  BotSituation  ── world scan, own economy,  │
                    │  ├── BotFogMemory (ZoneMemory/RegionMemory  │
                    │  │   + IBotZoneTopology ← TacticalMapBotModule)
                    │  │   ── remembered buildings/army, staleness│
                    │  ├── BotInfluenceLayers (IBotInfluenceMap)  │
                    │  │   ── ThreatGround/Air/AntiGround fields  │
                    │  ├── BotThreatTracker ── live sightings     │
                    │  └── CombatAnalysisBotModule (IBotThreatAnalysis)
                    │                                             │
                    │  OUTPUT: Situation snapshot (facts, no      │
                    │  decisions) + TeamBroadcast published       │
                    └──────────────┬──────────────────────────────┘
                                   │
                    ┌──────────────▼──────────────────────────────┐
                    │  SYNTHESIZE (strategy, from Situation only) │
                    │                                             │
                    │  BotUtilityAxes      ── rest-on-axis poles  │
                    │  MasterAiBotModule   ── personality drives  │
                    │    target axes (aggression/risk/tech/expand)│
                    │  BotDirector         ── tension pacing      │
                    │  BotLearnedPriors    ── kill/death ledger   │
                    │  IBotTeamMember      ── allied broadcasts   │
                    │    (ExpansionClaim, RequestsDefence, axes)  │
                    │                                             │
                    │  OUTPUT: utility positions, personality,    │
                    │  pace, allied blackboard                    │
                    └──────────────┬──────────────────────────────┘
                                   │
                    ┌──────────────▼──────────────────────────────┐
                    │  PLAN (candidate decisions, no orders)      │
                    │                                             │
                    │  ExpansionPlannerBotModule ── field scores, │
                    │    crawl target, refinery claims, MCV want  │
                    │  DefenseCoveragePlanner   ── placement       │
                    │    advisor (DEF-3: follows remote fronts)   │
                    │  SiegeEvaluatorBotModule  ── siege windows  │
                    │  EngineerBotModule        ── capture jobs   │
                    │  BridgeRepairBotModule    ── bridge jobs    │
                    │                                             │
                    │  OUTPUT: advisory targets + provider impls  │
                    │  (IBotExpansionTargetProvider,              │
                    │   IBotDefensePlacementAdvisor,              │
                    │   IBotSiegeAdvisor, IBotProtectionRequestProvider)
                    └──────────────┬──────────────────────────────┘
                                   │
                    ┌──────────────▼──────────────────────────────┐
                    │  ARBITRATE (commit = order emission)        │
                    │                                             │
                    │  UnitBuilderBotModuleCA   ── production     │
                    │    (IBotEnemyCompositionProvider,           │
                    │     IBotProductionWeight, IBotUnitRoles,    │
                    │     IBotPersonalityLeadProvider)            │
                    │  BaseBuilderBotModuleCA   ── buildings      │
                    │    (+ queue manager: refinery claims, MCV   │
                    │     deployment, defence placement)          │
                    │  SquadManagerBotModuleCA  ── squads         │
                    │    (IBotDirector, IBotFoggedEnemyProvider,  │
                    │     IBotMissionProvider, IBotSiegeAdvisor,  │
                    │     IBotUtilityAxes, IBotRegionThreatProvider,
                    │     IBotThreatPredictionProvider,           │
                    │     IBotRouteThreatRouter, IBotActionBudget)│
                    │                                             │
                    │  LEASES: BotUnitLeaseRegistry arbitrates    │
                    │  unit claims; BotOwnershipWatchdog verifies │
                    │  one-owner-per-actor; HumanPaceBotModule is │
                    │  the IBotActionBudget ceiling; BotOrderGate │
                    │  is the only order egress.                  │
                    └──────────────┬──────────────────────────────┘
                                   │
                    ┌──────────────▼──────────────────────────────┐
                    │  EXECUTE (orders only, never mutations)     │
                    │                                             │
                    │  24 squad FSM states (idle→stage→attack→    │
                    │  flee, ground/air/navy/support)             │
                    │  HarvesterBotModuleCA, McvExpansionManager  │
                    │  (engine), repairs, PlugSpawner (F),        │
                    │  deploy, capture, bridge, beacon respond    │
                    └──────────────┬──────────────────────────────┘
                                   │
                    ┌──────────────▼──────────────────────────────┐
                    │  VERIFY + FEED BACK                         │
                    │                                             │
                    │  AiMissionLogWriter → cameo-ai-missions.jsonl│
                    │  BotMissionLog → IBotMissionOutcomeSink →   │
                    │    BotSituation (future leader picks)       │
                    │  BotModuleFieldDump, per-module timing,     │
                    │  FogCanary (audit_fog_honesty)              │
                    │                                             │
                    │  Loops back: outcomes re-shape memory,      │
                    │  utility axes, production weights           │
                    └─────────────────────────────────────────────┘
```

## The decision-ownership table

Every high-level decision has exactly one owner. Modules that influence the
decision do so through a provider seam; the owner emits the order.

| Decision | Owner (emits order) | Advisors (publish only) |
|---|---|---|
| Which resource field to crawl toward | ExpansionPlannerBotModule | ZoneMemory, influence, allied ExpansionClaim |
| When to request an MCV | ExpansionPlannerBotModule (greedy driver, `RequestMcv`) | utility axes (UT-4 appetite), TeamBroadcast claims |
| Where a refinery goes | BaseBuilderBotModuleCA queue manager | `IBotExpansionTargetProvider.RefineryClaimTarget` |
| Where defences go | BaseBuilderBotModuleCA queue manager | `IBotDefensePlacementAdvisor` (DEF-3 fronts) |
| What to produce | UnitBuilderBotModuleCA | composition provider, learned priors, role sets, personality leads |
| What a squad does | SquadManagerBotModuleCA FSM | director pace, fogged enemies, missions, siege windows, route threat, action budget |
| Which enemy to target | SquadManagerBotModuleCA | `IBotMainTargetProvider` (learned-priors-weighted) |
| Capture jobs | EngineerBotModule | `IBotCaptureTransportProvider` (FB2), protection requests |
| Plug production | PlugSpawnerBotModuleCA (F) | plug's own `Buildable.Prerequisites` via TechTree; slot gating via `Pluggable.Requirements` |
| Bridge repair | BridgeRepairBotModule (X) | remembers defended sites, leases repairers |
| Siege window | SiegeEvaluatorBotModule | remembered defences, failure memory |
| Scout routes | ScoutBotModule | threat regions, influence layers, leads |
| Resource-field memory | BotFogMemory / ResourceMapBotModule | zone topology (TacticalMapBotModule) |
| Team posture | BotSituation broadcast | allied claims, role split (TC-2d), defend requests |
| Support powers | SupportPowerBotASModule | (RV2 merge pending — OpenRA copy gated classicbot) |

## Provider → consumer wiring (audit 2026-10-02)

All 31 `IBot*` interfaces verified live — every seam has ≥1 provider and ≥1
consumer. None are dead seams. The two that look unconnected under a naive
`TraitsImplementing` grep — `IBotInfluenceMap` and `IBotZoneTopology` — are
passed via the Situation snapshot and `RegionMemory` constructor respectively.

## Dead code / unreachable path audit

| Layer | Result |
|---|---|
| `IBot*` seams | 31/31 wired (provider AND consumer present) |
| Squad FSM states | 24/24 concrete states instantiated; 3 abstract bases (`*StateBaseCA`) expected |
| Module activation | every `RequiresCondition` has a granting path; `cnX`/`fb`-gated modules reachable via increment-switch arming |
| Info bools | 61 total; 24 yaml-enabled, 20 in switch manifest, the rest are struct-internal fields or dormant-but-referenced flags (`UseMissions`, `UseRiskRouting`, `PublishMissions`, `FogCanaryEnabled`, `SiegeMemoryEnabled`, `WeakIncludesDefence`) — reachable, currently off |
| Module classes with no yaml wiring | 3 found (2026-10-02): `McvManagerBotModuleCA` + `PowerDownBotModuleCA` deleted (superseded by engine `McvExpansionManagerBotModule`/`PowerDownBotModule`); `PlugSpawnerBotModuleCA` **restored and wired** — plugs carry whole tech chains (WC2 keep/castle, Zerg lair/hive, TS uplinks, temple nuke) — gated `genericbot && plug_spawn`, switch group F |
| `Info` bool defaults | corrected 2026-10-02 (EMBER): `UseMissions`, `UseRiskRouting`, `PublishMissions` default `true` in C# — they are LIVE, not dormant. The true dormant surface = switch-gated groups + `FogCanaryEnabled`, `SiegeMemoryEnabled` (yaml:false), `WeakIncludesDefence` (yaml:false) |

### Dormant paths that need a switch group or a removal call

- `FogCanaryEnabled` (SquadManagerBotModuleCA) — fog-canary trap exists, off.
- `SiegeMemoryEnabled` (SiegeEvaluatorBotModule, yaml:false) — failure-memory consumer wired, off.
- `WeakIncludesDefence` (BotSituation, yaml:false) — weak-target definition excludes defences today.

These are the candidates for either a switch letter or an explicit "dead by
design — remove" call. They are *not* unreachable code; they are unarmed
branches.

## Overlapping decision trees (the real coherence problem)

Where two modules can decide the same thing — the merge targets:

| Overlap | Today | Correct end state |
|---|---|---|
| Bridge repair | **RESOLVED (2026-10-02):** `BridgeRepairBotModule` (X) owns hut repair when enabled — `EngineerBotModule` skips its RepairBridge job while an enabled CN module exists on the actor; Engineer remains the fallback when X is off. One owner per gate state, no seam needed: the armed A/B is a clean swap, not an additive layer |
| MCV want | `ExpansionPlannerBotModule.RequestMcv` (greedy) AND engine `McvExpansionManagerBotModule.BuildMCV` (4000-cash gate) both call `RequestUnitProduction` | Verified benign (2026-10-02): both gates dedupe — the engine manager checks `ProductionQueue.AllQueued` + `RequestedProductionCount == 0`, the planner counts `active + queued` against its target. Two want-sources, one guarded queue. The genuinely dead third requester (`McvManagerBotModuleCA`) was deleted |
| Defence placement | Advisor path vs `PlaceDefenseTowardsEnemyChance` fallback | Advisor wins when present (clean alternative, by design) — fine |
| Capture | `CaptureManagerBotModuleCA` (classic) vs `EngineerBotModule` (genericbot) | Already split by bot type — the merged genericbot path is EngineerBotModule owning capture+bridge+transport; classic keeps its CA copy |
| Fransbot stack | 27 vendored modules run whole-cloth under `fransbot` — a parallel architecture, not a merge | Harvest per-module (route B) behind the existing IBot seams; the vendored stack stays a donor only |

## Harvest status (reference mods)

| Parent | Merged in | Still on the table |
|---|---|---|
| OpenRA engine | base of stack; `McvExpansionManager` EX-3 hook `d5d8b2a685` | `SupportPowerBotModule` merge → `SupportPowerBotASModule` (RV2, EMBER INC-ready); `BevManager`/`SharedCargo` parked (DESIGN §19.4) |
| RV (`OpenRA.Mods.AS`) | fully merged, 65 protected symbols | — |
| CA | squad FSM, base/unit builders, compositions | upstream drift sync pending ("AI routing", "harasser squads", air fixes — F2) |
| CN | `CombatAnalysisBotModule` (code); `DeployBotModule` (M), `BridgeRepairBotModule` (X) | `CNTacticalMap` chokepoints (ZG input), waves/pincer attacks, garrison improvements, cliff demolition, veinhole assault (content-blocked), stealth/subterranean/transport states |
| Fransbot | 8 record-only services on `hard` (#656) + `IBotCaptureTransportProvider` consumer wired | ForcePreservationGuard, AirCommander strike logic, **MCV island expansion + transports (13.5k lines)**, SpecOps, sea commander, support coordinator |
| Cameo (own) | fog memory, zones, influence, utility axes, personalities, director, team blackboard, expansion planner, defence coverage | the rest of the plan |

## After every donor is merged — the target pipeline

```
  OBSERVE          SYNTHESIZE         PLAN               ARBITRATE         EXECUTE        VERIFY
  ─────────        ──────────         ─────────          ──────────        ────────       ──────
  BotSituation ──> BotUtilityAxes ──> ExpansionPlanner──> UnitBuilder  ──> squads/  ──> mission
  (facts)          personality        DefenceCoverage    BaseBuilder      workers        log
  FogMemory        director           SiegeEvaluator     SquadManager     orders only    outcomes
  Influence        leads              Engineer/Bridge                     repairs        feedback
  ThreatTracker    team blackboard    Frans tactics      leases/budget    (capture,      into
  ResourceMap      (allied intent)    (air/sea/SpecOps)  order gate       deploy,        memory
  zones                              CN wave/pincer                       transport)
```

The merge rule stays the same one: **one owner per decision**; donors plug in
as `IBot*` providers or advisors, never as second order-emitters. The vendored
`fransbot` stack runs today as a whole separate bot type — the harvest is
bringing each useful module *through* the seams into `genericbot`, not running
two brains side by side.

## What would still improve it

- **Dangling mission outcomes (LC8)** — open attempts never write a terminal
  record at `GameOver` (EMBER's round-trip: 4 dangling `capture:*` in the 2v2
  smoke, 57 dangling across the A/B corpus). Executors should emit
  `Released(match_end)` on world teardown.
- ~~**Bridge-repair double owner**~~ — RESOLVED: EngineerBotModule yields its
  RepairBridge job whenever an enabled BridgeRepairBotModule shares the actor;
  `cn3_bridge_repair` arming is now a clean donor-vs-incumbent swap.
- **MCV want converged** — the dead `McvManagerBotModuleCA` is gone; the two
  live want-sources (planner greedy `RequestMcv`, engine `McvExpansionManager`
  cash gate) both dedupe into the guarded `RequestUnitProduction` queue, so the
  remaining improvement is cosmetic: publish the planner's want as a provider
  the engine manager consults instead of keeping a second gate.
- **CA drift sync** — upstream "AI routing / harasser squads" not yet pulled.
- **`_ra_doubles` seat bias** — seats 2,3 won both 2v2s regardless of team;
  harness-side, document it before any team A/B reads results.
