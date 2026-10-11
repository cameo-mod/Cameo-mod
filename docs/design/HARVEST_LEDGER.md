# Harvest ledger

One row per upstream bot/AI file surveyed against Cameo, with its disposition.
Machine inventory: `docs/research/bot-modules-survey.md` (regenerate with
`tools/ai/bot_modules_survey.py`; classification counts: 24 PRESENT-AND-USED,
6 PRESENT-BUT-UNUSED, 8 STALE, 32 ABSENT strict runtime traits).
This file adds the **disposition** layer the survey deliberately does not guess:
*where* a harvest landed, *what it merged with*, *why* a candidate is rejected,
and *which measured gap* an open candidate would close.

Disposition values:

| Disposition | Meaning |
|---|---|
| `HARVESTED` | Code or direct port lives in-tree and is YAML-enabled |
| `MERGED` | Upstream role folded into another module (one authority per decision) |
| `IDEA` | Re-implemented from the source's design; no upstream code copied |
| `REJECTED` | Reviewed and refused — reason recorded |
| `OPEN` | Candidate; must name the consumer/seam/gap it fills before porting |
| `NONE` | Repo surveyed; no qualifying custom bot modules found |

**Harvest rule (coordinator's breadth-without-evidence ruling):** the arch audit
already counts dead-end provider seams — seams harvested but never consumed.
A new port merges only when it names the decision it owns and the consumer it
feeds. Wire-or-drop the existing harvest first; that is what makes the next
harvest mergeable.

## Upstream revisions and licenses

All surveyed upstreams are GPLv3 except Schwerpunkt (no top-level license found —
do not copy code until resolved). SHAs and dates are pinned in
`bot-modules-survey.md` §Revision and license metadata. Fransbot is vendored
in-tree as `OpenRA.Mods.Fransbot` (upstream `main` is `v1.29.55`+4, tip `820dbcb`,
33 commits since the V1.29.23 harvest pin — including the VD-1 "Victory Drive"
opportunity-generator series, a candidate for the strategy layer; H-9 tracks).

## Vanilla OpenRA (`openra` bleed `a520984d`)

| Upstream file | Disposition | Where it landed / why |
|---|---|---|
| `BaseBuilderBotModule` | MERGED | `BaseBuilderBotModuleCA` (in-tree CA fork, extended) |
| `BuildingRepairBotModule` | MERGED | `BuildingRepairBotModuleCA`; then double-owner repair fixed (RV1, #664): genericbot runs merged `BaseRepairBotModule`, DESIGN §19.3 |
| `CaptureManagerBotModule` | MERGED | `CaptureManagerBotModuleCA`; engineer role merged into `EngineerBotModule` (ENG, §19.3); capture-transport service on genericbot via ENG-T (#694) |
| `HarvesterBotModule` | MERGED | `HarvesterBotModuleCA` |
| `McvManagerBotModule` | MERGED | `McvExpansionManagerBotModule` (expansion owner) |
| `McvExpansionManagerBotModule` | HARVESTED | Enabled as-is (`ai.yaml:3141`) |
| `MinelayerBotModule` | HARVESTED | Enabled (`ai.yaml:3443`) |
| `PowerDownBotModule` | HARVESTED | Enabled (`ai.yaml:210`) |
| `ResourceMapBotModule` | HARVESTED | Enabled (`ai.yaml:3450`) |
| `SupportPowerBotModule` | MERGED | `SupportPowerBotASModule` (`ai.yaml:212`); the SupportPowerBotModule+AS pair is the remaining duplicate, tracked as RV2 |
| `SquadManagerBotModule` | MERGED | `SquadManagerBotModuleCA` — most heavily extended harvest (LC1–7 seams, EL-1, AR-S dedup lane) |
| `UnitBuilderBotModule` | MERGED | `UnitBuilderBotModuleCA` |
| `BridgeRepairBotModule` | MERGED | CN3's `BridgeRepairBotModule` port (`X_cn3_bridge_repair`) runs beside `EngineerBotModule`'s own RepairBridge job only when armed |
| `HandicapDamageMultiplier` / `HandicapFirepowerMultiplier` / `HandicapProductionTimeMultiplier` | REJECTED | Multiplier cheats violate the no-cheating design law (difficulty scales via pacing/personality, never cost·speed·damage multipliers) |
| `BevManager` | MERGED (evaluated RV1) | Decision merged into the MCV owner if ever needed; never loaded beside it |
| `SharedCargo` | OPEN | For returning Generals factions' GLA Tunnel Network (DESIGN §19.4) — gated on that faction landing |
| `CncEngineerManager` | MERGED | Folded into `EngineerBotModule`; `classic` keeps the CA capture copy alone |

## Combined Arms (`combined-arms` `ab9e477c`, engine fork `d3976b8b`)

The CA `*CA` modules are the donor layer: vendored in-tree under
`OpenRA.Mods.CA/` and marked STALE by the survey because Cameo's copies have
diverged far past upstream. Their dispositions read against the Cameo-enabled
set above.

| Upstream file | Disposition | Where it landed / why |
|---|---|---|
| `BaseBuilderBotModuleCA` | HARVESTED | In-tree, enabled (`ai.yaml:3458`) |
| `BuildingRepairBotModuleCA` | HARVESTED | Enabled (`ai.yaml:3139`) |
| `CaptureManagerBotModuleCA` | HARVESTED | Enabled (`ai.yaml:3155`) |
| `HarvesterBotModuleCA` | HARVESTED | Enabled (`ai.yaml:3129`) |
| `LoadGarrisonerBotModuleCA` | HARVESTED | Enabled (`ai.yaml:3390`) — garrison-load specialist |
| `LoadCargoBotModule` (AS engine module) | HARVESTED+SPLIT | Enabled (`ai.yaml:3394`); AR-9 split — engine blocks are `classicbot`-only, genericbot runs the lease-aware twin `LoadCargoBotModuleAS` (AS resolves before Cameo in `Assemblies`, so a same-name shadow would be dead code; rosters resolve via @InstanceName fallback) |
| `MCVManagerBotModuleCA` | REJECTED | H-7: role owned by `McvExpansionManagerBotModule`; loading both = duplicate MCV authority |
| `PowerDownBotModuleCA` | REJECTED | H-7: vanilla `PowerDownBotModule` already owns powerdown; CA fork adds nothing measured |
| `AutoDeployManager` | OPEN (low) | H-7: order plumbing for `AutoDeployer` traits; `DeployBotModule` covers unit deploy. Revisit only if an actor gains `AutoDeployer` |
| `SquadManagerBotModuleCA` | HARVESTED | Enabled (`ai.yaml:3172`) |
| `UnitBuilderBotModuleCA` | HARVESTED | Enabled (`ai.yaml:4471`) |
| `UnitCompositionsBotModule` | MERGED | Composition selection lives in `UnitBuilderBotModuleCA`'s pick logic + personality weights |

## Crystallized Nexus (`crystallized-nexus` `30cf70a`, non-profile AI only)

| Upstream file | Disposition | Where it landed / why |
|---|---|---|
| `CombatAnalysisBotModule` | HARVESTED | Code port — `OpenRA.Mods.Cameo/.../CombatAnalysisBotModule.cs` (header-credited) |
| `CNTacticalMapBotModule` | HARVESTED | Code port — `TacticalMapBotModule.cs` (header-credited); region/zone-graph extraction continued under hotspot #4 (`TacticalMapRegionEval`) |
| `CNBaseBuilderBotModule` | MERGED | Role owned by `BaseBuilderBotModuleCA` |
| `CNHarvesterBotModule` | MERGED | Refinery-aware field distribution folded into `HarvesterBotModuleCA` |
| `CNMcvExpansionManagerBotModule` | MERGED | Role owned by `McvExpansionManagerBotModule` |
| `CNResourceMapBotModule` | MERGED | Role owned by `ResourceMapBotModule` |
| `CNSquadManagerBotModule` | MERGED | Template-slot squad idea informed squad logic; role owned by `SquadManagerBotModuleCA` |
| `CNUnitBuilderBotModule` | MERGED | Squad-demand-driven production folded into `UnitBuilderBotModuleCA` |
| `DeployBotModule` | HARVESTED | CN3 done item `M_cn3_deploy` — `DeployBotModule.cs` in-tree |
| `CNBridgeRepairBotModule` | HARVESTED | CN3 done item `X_cn3_bridge_repair` — `BridgeRepairBotModule.cs` in-tree |
| `CNGarrisonBotModule` | IDEA | `GarrisonDefenseBotModule` + `LoadGarrisonerBotModuleCA` own garrison decisions |
| `CNRegionManagerBotModule` | IDEA | Region worth/roles → `RegionRolesBotModule` + `IBotRegionRoles` + zone topology |
| `CNRepairManagerBotModule` | IDEA | `UnitRepairBotModule` (damaged-idle → repair facility) |
| `CNCliffDemolitionBotModule` | REJECTED (content-blocked) | No `CNDestroyableCliff` actors exist in Cameo — nothing to shoot open |
| `CNVeinholeAssaultBotModule` | REJECTED (content-blocked) | `forgotten_veinhole` is a nuke silo, not a force-fire veinhole |
| `CNHandicap*Multiplier` (4 files) | REJECTED | Named-tier multipliers = the same cheat class as the vanilla handicap traits |
| `HeightAdvantageBonus` | OPEN | Range/sight bonus on high ground — not a bot decision; belongs to the combat-systems lane if adopted |
| Stealth / subterranean / transport states | IDEA (partial) | `StealthDoctrineBotModule` covers doctrine; CN3 row tracks what remains |
| Wave / pincer coordination | OPEN | Research-flagged; must name the consumer seam (squad coordinator) before porting |

## Fransbot (vendored `OpenRA.Mods.Fransbot`, harvested against V1.29.23; upstream `main` = v1.29.55+4)

Not a port — a full parallel bot stack kept in-tree behind `Bots: fransbot`
(`mods/cameo/ai/fransbot.yaml`). Two tiers:

**Service tier** — `enable-fransbot || inc3_frans_services` (8 modules, armed on
genericbot under the INC-3 arm; record-only on `hard` per #656):
`FransCombatIntel`, `FransStrategicMap`, `FransRiskModel`, `FransMineCluster`,
`FransEconomicSaturation`, `FransCommanderCore`, `FransCommandBid`,
`FransGeneral`.

**Executor tier** — `enable-fransbot` only (cannot co-run with genericbot;
intra-stack contention resolves via `CommandBid`, not leases — see F6 in
`COORD_2026-10-04_nova_arch_review_wave.md`): `FransbotController`,
`FransBaseBuilder`, `FransMcvExpansionManager`, `FransUnitBuilder`,
`FransHarvester`, `FransSupplyTruck`, `FransGroundCommander` (×5 slots),
`FransSeaCommander`, `FransAirCommander`, `FransDefenseCommander`,
`FransSpecOpsCommander`, `FransTransportCommander` (DISARMED by default —
touches actors directly, 15 CancelActivity sites), `FransSupportCoordinator`,
`FransSupportPower`, `FransMinelayer`, `FransGroundTransfer`.

Helpers (not modules): `FransActorClass`, `FransBotLog`,
`FransGroundDefendForcePreservationGuard`, `FransGroundSecurePackageSelector`,
`RoutineLandNegativeUnionPolicy`.

| Disposition summary | Count |
|---|---:|
| HARVESTED-as-service (inc3-armable) | 8 |
| Executor tier (fransbot-only) | ~19 |
| Upstream drift (V1.29.23 → v1.29.55+4, 33 commits) | tracked by H-9 weekly refresh |

## Shattered Paradise (`124b7054`)

| Upstream file | Disposition | Why |
|---|---|---|
| `HarvesterBotModuleSP` | MERGED | `HarvesterBotModuleCA` |
| `McvManagerSPBotModule` | MERGED | `McvExpansionManagerBotModule` (aircraft-MCV/stuck handling evaluated; no measured gap) |
| `MinelayerBotModuleSP` | MERGED | `MinelayerBotModule` |
| `CashCheater` / `AddCashCheater` | REJECTED | Cash-generation cheats — same barred class as handicap multipliers |
| `UnpackBaseBotModule` | OPEN (low) | Repack-and-move when the base is crowded; no measured gap yet — keep in ledger |

## Generals Alpha (`21217ba`), H-8 ledger-only

| Upstream file | Disposition | Why |
|---|---|---|
| `InitialBaseAndWorkerBotModule` | REJECTED (for now) | Opening-BO role; Cameo openers are scripted via personality/queue weights |
| `GeneralCollectorBotModule` | REJECTED (for now) | Collector role owned by `HarvesterBotModuleCA` |

## OpenHV (`d507035`), OpenDR (`98079a9`), OpenOP2 (`9bba9ee`), OpenE2140, RV, Schwerpunkt, YR

Surveyed — checkout-level inventory in the survey. OpenE2140, Romanov's
Vengeance, Schwerpunkt, and Yuri's Revenge have **no qualifying custom bot
modules** in their source roots (`NONE`). The individually interesting rows:

| Upstream file | Disposition | Why / gap it would close |
|---|---|---|
| `HV ScoutBotModule` | MERGED | Cameo `ScoutBotModule` already owns scouting (LC1 claims scouts) — survey's "cheapest win" is superseded |
| `HV MinerBotModule` | OPEN (low) | Miner deploy logic; overlaps `McvExpansionManagerBotModule` deploy sites — port only if a stuck-miner metric appears |
| `HV BuilderBotModule` / `BaseBotModule` / `CargoBotModule` / `CubePickupBotModule` / `DeployActorBotModule` / `ExternalConditionPowerBotModule` / `PriorityCaptureManagerBotModule` | REJECTED (for now) | Roles covered by existing owners (builder/cargo/crate/deploy/support-power/capture); no measured gap |
| `HV SendUnitToAttackBotModule` | HARVESTED | Enabled (`ai.yaml:3241`) — non-squad attack senders |
| `OpenDR RigBaseBuilderBotModule` | REJECTED (for now) | Forked base-builder; `BaseBuilderBotModuleCA` owns construction — port only if a measured build-order gap appears |
| `OpenOP2 AutoDeployMinersBotModule` | OPEN (low) | Same miner-deploy class as HV `MinerBotModule` |

## Standing rules

1. Every merge of new upstream code names the decision it owns (§19.3) and the
   seam or consumer it feeds — or it does not merge.
2. Cheat-class traits (cash/damage/speed/handicap multipliers) are rejected by
   design law, not by effort.
3. `NONE`/`not-cloned` repos re-check on the weekly upstream sweep (H-9;
   `WORKFLOW.md` §6 status line, owner: EMBER).
4. When a port lands, update this ledger's disposition in the same commit —
   the ledger is only useful while it is the truth.
