# Trait unification: one plain-name Cameo implementation per mechanic

Owner: Boss (Devin). Task `01a10dcd-55b6-7d10-96a8-4a60b7d99d56`.
Status: specification for coordinator review; no implementation or merge approval.
This specification also covers the lead's extension to capabilities not yet ported.
The separate bot-meta-learning specification remains frozen and is not amended here.

## 1. Evidence, scope and provenance

The measured Cameo revision is **`b6f522e78d41c68649e66e315363a42fc0bb683c`**.
Sources were read from immutable git objects, with engine sources from the exact
`mod.config` pin **`d5d8b2a6853bff3b5a00cc5db7d86b03d4f30684`** in `cameo-engine`.
This is the coordinator's explicitly selected `origin/inc/2026_10_05` revision,
including the later squad fixes, not a claim about a subsequently moving tip.
The main checkout was `master@0fd6ec67e`, based on INC-e.
An initial capture of that main checkout was corrected before this specification:
66 source files changed or were added when `fe4459c9c` was substituted; the lead's
subsequent `b6f522e78` ruling updated only 11 changed/added source files and
`mods/cameo/ai/ai.yaml`. Donor and pinned-engine captures stayed frozen.
No shared checkout was switched.

**Master comparison:** `0fd6ec67e` itself changes only documentation relative to its
parent `3ba05ede7`, but its tree lacks the later trait changes present in `b6f522e78`.

| Source | Captured commit | Source roots |
|---|---|---|
| Cameo mod | `b6f522e78d41c68649e66e315363a42fc0bb683c` | `OpenRA.Mods.CA`, `OpenRA.Mods.Cameo`, `OpenRA.Mods.Fransbot` |
| Loaded engine source | `d5d8b2a6853bff3b5a00cc5db7d86b03d4f30684` | `OpenRA.Game`, `OpenRA.Mods.AS`, `Common`, `Cnc`, `D2k` |
| Combined Arms | `f31049d2d09aaba8a5bf4ba9904e0795d72cd82e` | `CAmod/OpenRA.Mods.CA` |
| Romanov's Vengeance | `231059963663dcb07d07b56f607b41d173795da9` | `Romanovs-Vengeance/OpenRA.Mods.RA2` |
| Shattered Paradise | `124b7054a636a9c51d8edc87b3171bf63b92556f` | `Shattered-Paradise-SDK/OpenRA.Mods.Sp` |
| Crystallized Nexus | `30cf70a666ca250ac79ccb6a7924745075f94a57` | `crystallized-nexus/.modsdk/OpenRA.Mods.CN` |
| Generals Alpha | `70154e6a9691190aeec3b9d0db3c55ba216311de` | `Generals-Alpha/OpenRA.Mods.GenSDK` |
| Upstream OpenRA | `f3ec7f8e1593b482f85fd101652deb740c33dee6` | `OpenRA.Game`, `Common`, `Cnc`, `D2k` |

The local `cameo-engine` checkout HEAD was `486374dcc7cb53319bc1b1dc14fbf698b3b0d8fb`;
that HEAD was **not** substituted for the loaded pin. Donor checkouts were clean.
These are local captured revisions, not a claim that their remote branches are current.
Inherited engine mechanics are inventoried once through the pinned engine and upstream
OpenRA. This is not an audit of every historical engine fork behind each donor.

The full inventory is split into readable companions:

- [Declaration inventory and exact direct-field deltas](TRAIT_UNIFICATION_INVENTORY_2026_10_05.md):
  variant → base/family anchor → source → mounted/source-only/donor state → resolved USE
  → field types and initializers → default donor → plain destination → merge risk.
  It includes same-name donor implementations, not just suffixed names.
- [Uncovered donor declaration inventory](TRAIT_UNIFICATION_UNCOVERED_2026_10_05.md):
  one row per unmatched donor declaration, grouped by capability, with its description
  and source location. A name gap is not proof of a missing mechanic; see §5.
- [Frozen evidence archive](TRAIT_UNIFICATION_EVIDENCE_2026_10_05.zip):
  inventory JSON, source text and hashes, source diffs, manifest paths, resolved usage,
  and the 152 individual MissileCA locations. Diff names match inventory anchors.

The corrected capture contains 2,317 YAML-facing declaration candidates in 4,504
source files. The comparison appendix has 1,754 rows, including family baselines;
73 loaded declarations have the requested CA/AS/RV/SP/TA-style tags. The uncovered
intake has 263 donor declarations: CA 123, CN 77, SP 32, RV 10 and Generals 21.
These are declaration counts, not counts of unique missing gameplay capabilities.

USE means resolved non-template **trait instances**, projectile instances on concrete
weapons, or warhead instances. Two `Trait@instances` on one actor count twice.
It does not mean active traits in a live match. An assembly-shadowed copy has zero USE.
Global-manifest resolution excludes local map overrides and packed map archives;
those are a mandatory migration surface, not silently included in these counts.

The existing `tools/audit/type_merge_inventory.py` is a useful starting point, but
its scanner skips donor classes whose names already occur locally, omits CN and the
core Game assembly, and reports direct declaration syntax. This capture includes
those sources. Field deltas are exact **direct declarations**: inherited fields are
not “removed”, and `new WDist(86)` versus `new(86)` does not change the numeric default.
The archive provides implementation diffs; a source diff alone does not prove runtime
equivalence. The family contracts and tests below are required before migration.
Source-text hashes use the decoded, UTF-8-serialized capture; they are not git blob
IDs. Non-UTF-8 donor files decoded as Windows-1252 are listed in the capture.

## 2. MissileCA: a dormant-content dependency, not a live missing type

At the pinned revision, the shared MiniYAML parser finds **152** `Projectile:
MissileCA` declarations in **22** files. **Zero** of these files are in the recursively
included manifest weapon list, and **zero** resolved concrete weapons use `MissileCA`.
The loaded assemblies contain no `MissileCAInfo`. The donor implementation exists at
`CAmod/OpenRA.Mods.CA/Projectiles/MissileCA.cs:26`.

| Unmounted `mods/cameo/weapons/` file | Parsed declarations |
|---|---:|
| shockwave.yaml | 28 |
| missiles.yaml | 22 |
| generals.yaml | 18 |
| starwars.yaml | 15 |
| wz2100.yaml | 11 |
| advacewars.yaml | 10 |
| advancewars.yaml | 9 |
| heroes.yaml | 7 |
| sow.yaml | 6 |
| valentine.yaml | 5 |
| wh40k.yaml | 3 |
| halloween.yaml, monsters.yaml, tiberiaalliances.yaml, tomorrow.yaml, worms.yaml, xmas.yaml, z.yaml | 2 each |
| infected.yaml, lostunits.yaml, mindustry.yaml, sc2k.yaml | 1 each |

Both spellings `advacewars.yaml` and `advancewars.yaml` are real separate files.
They must not be coalesced during conversion. The present global ruleset never asks
the object factory to create MissileCA, which explains why this does not itself
break current startup. Mounting those files or a map that refers to their weapons
would introduce a real unresolved type dependency. Do not fix this by silently
changing every occurrence to today's Missile: donor guidance, impact and field
semantics first need the compatibility mapping in §4. This conclusion is static
manifest/source evidence; no boot was run for this specification.

## 3. Destination architecture and ownership

The maintainer's plain-name goal supersedes the older TYPE_MERGE_PLAN suggestion to
give AS collisions another suffixed name. A final family has one Info/runtime pair
with the plain name. Former names may exist only as temporary migration aliases
that instantiate that same implementation, not independent behavior forks.

**Use a small `OpenRA.Mods.Cameo.Unified` assembly containing only migrated families,
with namespaces under `OpenRA.Mods.Cameo`.** Insert it before AS/CA in the manifest;
retain the relative order of all existing assemblies. This is a proposed project,
not an existing one. Its limited type inventory makes precedence review tractable.
Do not simply move the entire existing Cameo assembly ahead of AS: that changes
unrelated duplicate type resolutions. Existing Cameo types move into the new
assembly only as their family migrates; never compile the same type into both.

Dependency direction is explicit: Contracts depends only on engine contracts;
Unified depends on Contracts and the necessary engine libraries. A transitional
AS/CA base-class reference is allowed only when that assembly has no reverse
reference to Unified. Move the typed consumer/helper closure or replace it with
a Contracts interface instead of introducing a project-reference cycle. Existing
Cameo callers may depend on Unified, so Unified must not depend back on Cameo.

This design is necessary because `mods/cameo/mod.yaml:431` orders AS → CA → Cameo
→ Cnc → D2k → Common → Fransbot. `ObjectCreator` also prepends **OpenRA.Game**,
before any manifest assembly (`engine/OpenRA.Game/ObjectCreator.cs:36–43`).
Two families need different treatment:

| Case | Required implementation route |
|---|---|
| Plain name collides only with mod assemblies | Selective unified assembly wins factory lookup; remove vendored sibling after all consumers migrate. |
| `FactionCA` → core `Faction` | Ordinary mod shadow cannot win over Game. Add the optional `Game = null` metadata through the canonical engine clone/pin pipeline, then update the Cameo consumers and YAML. |
| Engine code uses concrete donor types through `Trait<T>`, `Requires<TInfo>`, constructor parameters or activities | Factory-name equality is insufficient. Preserve assignability by deriving the compatible engine Info/runtime where viable; otherwise move the typed consumer closure or add a narrow engine interface patch. |
| Runtime API, renderer, pathfinder or serialization hook is missing | Separate canonical engine patch, reviewed and pinned before the dependent mod family. Never edit the ignored `engine/` directory. |
| New donor mechanic has no engine coupling | Implement directly in the unified assembly under its plain functional name, with donor dependencies ported explicitly. |

Before each family lands, record both **factory resolution** and **CLR assignability**
for every typed consumer. A shadow can load successfully yet fail an engine
`TraitOrDefault<OldRuntime>()` lookup. Keep the family blocked until both are proved.
Namespaces, assembly names, actor YAML names and order strings are separate migration
surfaces; changing one does not automatically migrate the others.

### 3.1 Cameo-origin contracts and misleading CA names

The new bot contracts sit in CA partly to avoid CA → Cameo → CA dependency cycles.
That placement is architectural history, not evidence they were imported from CA.
Move shared contracts first into a dependency-light **`OpenRA.Mods.Cameo.Contracts`**
assembly; CA, Fransbot, Cameo and Unified can all depend on it. It must not depend
back on gameplay implementations. Preserve contract member names and enum values.

Move these current `OpenRA.Mods.CA.Traits` contracts to the corresponding Cameo
contracts namespace; their plain interface names already satisfy the naming goal:

`IBotActionBudget`, `IBotArmyStaging`, `IBotAssaultFormation`, `IBotBuildOrderKnobs`,
`IBotCaptureClaimSource`, `IBotCaptureTransportProvider`, `IBotCoalition`,
`IBotCombatVeto`, `IBotDefensePlacementAdvisor`, `IBotDirector`,
`IBotEnemyCompositionProvider`, `IBotEngagementPriors`, `IBotExpansionTargetProvider`,
`IBotFoggedEnemyProvider`, `IBotFrontBackAdvisor`, `IBotInMatchAdaptation`,
`IBotMainTargetProvider`, `IBotPersonalityLeadProvider`, `IBotPlacementAdvisor`,
`IBotPlacementObserver`, `IBotProductionWeight`, `IBotProductionWidth`,
`IBotProtectionRequestProvider`, `IBotRegionThreatProvider`,
`IBotRememberedDefenceProvider`, `IBotRequestPauseBuildingProduction`,
`IBotRouteThreatRouter`, `IBotScaleTargets`, `IBotSiegeAdvisor`,
`IBotSiegeFailureMemory`, `IBotStealthDoctrine`, `IBotTeamMember`,
`IBotThreatAnalysis`, `IBotThreatPredictionProvider`, `IBotUnitLeases`,
`IBotUnitRoles`, `IBotUtilityAxes`, and `IHasParallelQueueSlots`.
Move their value records/enums with them: leases/purposes, placement picks/classes,
mission assignments/events, team broadcasts, coalition directives, scale targets,
role/utility axes and protection requests. Split runtime helpers out if their
dependencies would make Contracts depend on a gameplay assembly.

| Cameo-owned implementation/helper | Plain target / treatment |
|---|---|
| `ConcaveEvalCA` | `ConcaveEval`; introduced in Cameo `a75b65609`, not an upstream CA mechanic. |
| `SquadMicroEvalCA` | `SquadMicroEval`; Cameo introduction `aecb69059`. |
| `AttackForceEvalCA`, `PrepositionDecisionEvalCA`, `SquadPoolFixesEvalCA` | `AttackForceEval`, `PrepositionDecisionEval`, `SquadPoolFixesEval`; retain the squad corrections added in the selected integration, with no behavioral change during renaming. |
| `GroundUnitsConcaveStateCA`, `StealthHelpersCA`, `StealthUnitsIdleStateCA`, `StealthApproachStateCA`, `StealthFleeStateCA` | Remove CA suffix while migrating the squad helper closure; preserve behavior and state transitions. |
| `FighterIdleStateCA`, `GunshipCASStateCA`, `BomberIdleStateCA` | Remove the final CA source suffix; `CAS` inside GunshipCAS means close air support and is not a source tag. |
| `BotDifficultyLadder`, `BotLimits`, `BotLimitsResolver`, `BotMission*`, `BotTargetTags` | Already plain names; move Cameo-owned namespace/project placement, not their semantic identity. |
| `AdaptiveCounterProduction`, `AirLimits`, `BotCombatPredictor`, `DerivedUnitWeights`, `SpreadRules`, `SiegeEvaluatorBotModule` | Move with bot implementation consumers; do not put engine-dependent evaluators into the contracts-only project. |
| `PlugSpawnerBotModuleCA` | Merge its Cameo modifications into `PlugSpawnerBotModule`; donor AS has an actual counterpart, so this is not merely an original helper rename. |

`IBotFrontBackAdvisor` was introduced by Cameo `63cd1c372`;
`IBotUnitLeases` by `f7239386c`. These are positive history checks. Mere absence of
a same-path file in today's CA donor is not sufficient authorship evidence.
Preserve donor copyright/license notices when relocating imported code.

## 4. Family contracts: preserve the union and every current behavior

The companion inventory supplies the full per-declaration field/default table.
Its baseline column is a comparison anchor; its **default donor** is the mounted
variant with greatest USE, with manifest precedence breaking ties. With no mounted
variant, use the plain baseline. This makes the selection explicit and reproducible.
Every other user's old effective values must be materialized during conversion.
The following table records the functional conflicts requiring more than renaming.
“Absent” below means absent from that variant, not a request to delete functionality.

### 4.1 Projectiles first

| Family / donors → plain name | Exact differences to preserve | Migration and risk |
|---|---|---|
| Common Missile, AS MissileTA, upstream MissileCA → `Missile` | TA adds slowdown, lookahead distance/step, lock-on loop/target filters, enemy blocking, initial/accelerating horizontal turn, activation delay, jet animation, jammed facing/effect, altitude cutoff/reset. Its defaults differ: TerrainHeightAware true vs false; Width 1024 vs 1; ExplodeWhenEmpty false vs true; CruiseAltitude 0 vs 512; Jammable false vs true; JammedDiversionRange 256 vs 20. TA has no direct Arm or DetonateOnTargetLoss fields. CA adds LockOnBurstCounts, TrailSpacing, DirectFireMaxRange and singular PointDefenseType; pinned Common has plural PointDefenseTypes and DetonateOnTargetLoss. | Keep current Common defaults for its 477 resolved weapons. Materialize the listed TA defaults and retain its guidance algorithm as an explicit semantic mode until trajectory parity is proved. Map singular PointDefenseType to a singleton plural set; reject simultaneous contradictory forms. Do not interpret activation delay as arming delay. High sync/collision risk. |
| Common Bullet, AS BulletAS, upstream BulletCA → `Bullet` | AS adds shadow palette, altitude detonation and point-defense types; its ValidBounceBlockerStances corresponds to Common ValidBounceBlockerRelationships. Pinned Common also has trail spacing and streak length/variation/width/color/Z offset. CA's newer features remain in the field appendix. | Map Stances → Relationships, preserving the relationship bit mask; retain all streak/trail/point-defense options. Keep ballistic RNG draws, bounce decisions and impact tick stable. High risk. |
| Common LaserZap, vendored/upstream LaserZapCA → `LaserZap` | Vendored CA adds `max(0, target.Y - args.Source.Y)` to both beam Z offsets. This is **Y**, not target altitude. It uses Owner.Color where Common uses OwnerColor(). Common adds GlowIntensity and source-flare controls, absent from the CA copy. | Add `TargetDepthOffset` false and `UseOwnerColorOverride` true as semantic options; convert CA users to true/false respectively. Preserve Common flare/glow fields and CA's effect draw order. Test both render modes; damage timing unchanged. Medium rendering, high accidental type-routing risk. |
| AS WarheadTrailProjectile, CA copy → `WarheadTrailProjectile` | Equal direct schemas conceal different logic: CA passes the firing weapon separately to its effect, routes Impact through configured WeaponInfo, and suppresses AS's lifespan explosion loop. | Model `ImpactWeaponSource` and `ExplodeAtLifespan` separately; AS and CA users receive their existing choices explicitly. Include both effect classes in the merge. Do not “deduplicate” solely from equal fields. High damage-count/timing risk. |
| AreaBeam, Railgun, TeslaZap and their upstream CA/AS/SP/CN candidates | All direct fields and source differences are in the inventory/archive, including variants never loaded by Cameo. | Union field schema, explicit old defaults, preserve collision sampling, falloff, impact ordering and render paths. One family per checkpoint; new behavior stays unused until a separate content change. High risk. |
| NukeLaunch / NukeLaunchCA runtime and actor-based BallisticMissile variants | `NukeLaunchInfo` is already plain although its runtime is named CA. BallisticMissile is an actor movement trait, not the Missile projectile. The CA actor implementation uses MissileBase; AS directly implements movement/facing. | Keep the two mechanics separate. Rename the runtime only after callers migrate. Port BallisticMissile/old RV variants with their MissileBase and activity closures; do not feed them through projectile-field conversion. High risk. |

Proposed new semantic fields above are design decisions, not fields claimed to
exist today. All existing field types/default expressions are preserved in the
appendix. A migration emits those semantic choices; it never infers them later
from a deleted `CA`/`AS` suffix. Internal algorithm strategies may coexist within
the single implementation where they represent real configurable behavior.

### 4.2 Attack, movement, spawners and control

| Variants → family | Functional delta / union decision | Risk |
|---|---|---|
| AttackAircraftCA → AttackAircraft | Adds AirFacingTolerance=512; inherit AttackAircraftInfo fields rather than treating AttackType/StrafeRunLength as removed. Preserve separate air-facing tolerance semantics. | High |
| AttackBomberCA → AttackBomber | CA gates firing on facingTarget and supplies FlyAttack instead of the base scripted-target exception; SetTarget signature also differs. Preserve scripted and order-driven operation explicitly. | High |
| AttackGarrisonedSP → AttackGarrisoned | Adds PerPassengerTargeting=true and SP fire-port behavior; retain shared-target operation and port geometry. | High |
| AttackPrismSupportedCA → AttackPrismSupported | Adds charge capacity/reload/initial/continued charge delays, charge sound, modifier and charging condition. Merge the controller and support-link typed dependencies together. | High |
| AttackLeapAS → AttackLeap | Adds Angle=20 degrees, DamageTypes and LeapTargetCondition; the base has LeapCondition. These conditions affect different actors and must remain distinct. | High |
| AirstrikeMasterCA / AirstrikeSlaveCA → AirstrikeMaster / AirstrikeSlave | Master adds available-slave condition, CancelOnStop and SpawnDistance. Slave tracks busy/available state, notifies master on death and removes detached slaves. Merge master/slave/activity callbacks atomically. | High |
| MissileSpawnerMasterCA → MissileSpawnerMaster | CA targets MissileBase, derives facing from target and sets spawned facing/position at frame end; AS expects BallisticMissile. Preserve both supported movement capabilities behind a common contract. | High |
| DroneSpawnerMasterCA → DroneSpawnerMaster | Adds SlavesTargetSelf, LoadedCondition and SpawnContainConditions. Preserve spawn containment grants and targeting independently. | High |
| MindControllerCA / MindControllableCA / WithMindControlArcCA → plain trio | Controller adds acquisition/revoke/death delays, progress/max conditions, separate control/release/init sounds, manual release, undeploy and experience options. Controllable adds controller-keyed controlled/revoking condition maps where AS has one Condition and sound audibility/volume. Arc binds the corresponding controller types. | High; atomic trio, token ownership/release tests |
| ChronoshiftableCA → Chronoshiftable | Adds initial/return warp effects and condition; return-to-avoid-death policy, health threshold, sound/relationships/husk and ExposeInfectors. Preserve normal return behavior when disabled. | High |
| PortableChronoCA → PortableChrono | Adds teleport condition/duration, charge count, recharge-to-max/reset, cooldown, infection exposure and selection bar options. Charge versus cooldown timing must not be conflated. | High |
| AttackInfectCA / InfectableCA → AttackInfect / Infectable | Attack swaps to InfectCA activity; target adds infector limit and remove/kill counts. Migrate activities and infection interfaces with traits; preserve enter/kill/remove ordering. | High |
| MadTankCA → MadTank | Adds detonation animation, immediate-first detonation, deploy type/exclusivity and KillsSelf. Existing ThumpSequence/weapon, DetonationWeapon and DriverActor defaults become null in CA. Materialize defaults for base users. | High |
| EjectOnDeathAS → EjectOnDeath | Inherits base ejection fields and adds SpawnOnlyWhenPromoted=true. New unified default follows mounted base; AS conversion explicitly opts into promotion gating. | Medium |
| TemporaryOwnerManagerAS / ChangeOwnerAS → plain manager/warhead | Adds condition handling; AS warhead adds target/relationship filters and uses WarheadAS, while Common has OwnerType/InternalOwner. Preserve ownership source, duration and condition restoration independently. | High |
| FireClusterAS → FireCluster | Adds AroundTarget=false. Retain origin/target-centered scatter with identical random draw order. | High |

### 4.3 Conditions, economy, support powers and presentation

| Variants → family | Functional delta / union decision | Risk |
|---|---|---|
| GrantConditionOnAttackCA | RequiresActorTarget=false added; base selection-bar fields absent from CA. Preserve shot counting, actor-target restriction and optional bar. | Medium |
| GrantConditionOnBotOwnerCA | **Empty Bots means all bots** in CA; base creation requires membership. CA also checks IsBot on ownership change. Add `MatchAllBotsWhenEmpty`; map CA users true, base users false. | High; takeover and ownership parity |
| GrantConditionOnPrerequisiteCA + ManagerCA | CA registers on add/remove-world rather than creation/disposal and defers condition updates to frame end; manager stores corresponding CA consumers. Base includes guards absent from CA. Preserve callback phase explicitly; port the pair together and test death/capture/re-entry. | High |
| InfiltrateForSupportPowerCA | Base has player experience and both text notifications absent from the CA copy. Keep these fields and preserve proxy/power activation behavior; consider newer donor InfiltrateToCreateProxyActor as an extension. | High |
| PeriodicProducerCA | Immediate=false and ResetTraitOnOwnerChange=false added. Preserve initial-production tick and ownership-change timer behavior. | Medium |
| ProductionAirdropCA | IncomingAudio, spawn policy and proportional-speed controls added; ReadyAudio null vs Reinforce. Base has ready text, baseline spawn, before/after waits and landing offset. Keep all scheduling and notification choices. | High |
| ProductionParadropCA | Adds spawn policy, facing and proportional-speed controls; ActorType ra1_badger vs badr. Materialize actor identity; do not replace it with a universal aircraft. | High |
| ProductionQueueFromSelectionCA | AllySelection=false added. Preserve own-only default and optional ally selection. | Medium |
| ReloadAmmoPoolCA | DelayOnFire, DelayAfterReset, ReloadWhenAmmoReaches=-1 and bar/color controls added. Preserve first/subsequent reload tick and burst interaction. | High |
| ResourcePurifierCA | MinAmount=250 added. Preserve threshold behavior without changing income values or balance multipliers. | Medium |
| RevealOnFireCA | GroundPosition=false added. Preserve air/ground reveal origin and armament filters. | Medium |
| AnnounceOnKillAS | OnlyToOwner=false added. Base compares Game.RunTime milliseconds with Interval=5000; AS compares WorldTick with Interval×25, default 5→125 ticks. Preserve an explicit cosmetic cooldown clock and interval; these defaults are not universally equivalent at different game speeds. | Medium |
| AmbientSoundCA | Adds initial/final phases and initial length. Base updates sound volume under fog; CA skips starting inaudible sounds. CA's VolumeMultiplier=1f is passed to non-looping playback but not its looped calls. Preserve these distinctions explicitly rather than merely renaming Volume. | Medium |
| AttackSoundsCA | AudibleThroughFog=false and Armaments primary/secondary added. Preserve target/armament and fog audibility filters. | Medium |
| DetonateWeaponPowerCA | Postprocess effect, beacon actor, multi-color circles; activation sequence null vs active. AS uses singular TargetCircleColor. Normalize singular to one-element list only after preserving range/color association. | High |
| GrantExternalConditionPowerCA | Scalar Condition/Duration/Footprint/Dimensions differs from base level-keyed dictionaries: Dictionary<int,string>, Dictionary<int,int>, Dictionary<int,string>, Dictionary<int,CVec>. CA also has target filters/count limits, visibility, effects, active state, range/altitude and prerequisite groupings. | High; preserve all base levels; map the scalar form to explicit non-level-dependent behavior, reject ambiguous mixed schema |
| NukePowerCA | BeaconActor added; collection initializer syntax differs. Include NukeLaunch implementation and beacon lifecycle, not just Info fields. | High |
| AirstrikePowerAS / AirstrikePowerRV | AS uses scalar SquadSize plus Mission and level-keyed GuardDurations/GuardingConditions; base SquadSizes is Dictionary<int,int>. RV adds ActivationDelay=0. Preserve attack/guard missions, every upgrade level and delay. | High |
| FrozenUnderFogUpdatedByGpsAS / GpsDotAS | First binds GpsASWatcher and IOnGpsASRefreshed; dot adds Sequence, VisibleInShroud and Range where base uses String. Merge watcher/provider/callback closure; don't equate glyph and sequence semantics. | High; fog/intelligence |
| CloakPaletteEffectCA | Palette=cloak plus skipped/erased palette indexes. Keep all configurable index policies. | Medium |
| LeavesTrailsCA | Outside-map handling differs: CA uses terrain type Invalid rather than immediately returning. Preserve edge-of-map trail behavior explicitly. | Medium |
| RenderShroudCircleCA | Contrast color/width, visibility, relationship and player-color/alpha fields; base border fields use different names. Match rendering roles before renaming border→contrast. | Medium |
| WithMuzzleOverlayCA | IsPlayerPalette=false added. Preserve palette selection and muzzle offsets. | Medium |
| WithNameTagDecorationCA | ColorSource, relationship/team colors and contrast choices replace the base UsePlayerColor boolean. Convert the boolean to the corresponding color mode. | Medium |
| WithProductionDoorOverlayCA | Additional OpenDoor entry point and IOccupySpaceInfo-based eligibility instead of runtime IPositionable. Preserve both production notification and explicit opening callers. | Medium |
| WithRangeCircleCA / RenderRangeCircleCA | First adds player alpha but lacks base RenderOnGround; second adds armament filtering. Retain independent rendering position/alpha/filter choices. | Medium |
| WithBuildingBibCA / TerrainLightSourceCA | CA bib is conditional; light follows TraitEnabled/TraitDisabled instead of add/remove-world. Preserve activation lifecycle, including removal while enabled. | Medium |
| TurnOnIdleCA | CA uses ITick and IFacing with actor idle check; base uses idle notifications and Mobile disabled/paused checks plus speed modifiers. Equal fields do not mean equal turning behavior. | High movement/order compatibility |
| FireWarheadsOnDeathCA | Inherits base fields and adds WeaponName; do not drop inherited Weapon/EmptyWeapon/chance/death filters. Preserve weapon selection and death-impact order. | High |
| FactionCA | Adds Game=null to core FactionInfo for grouping. Use the core engine exception in §3. | High type-resolution risk |

### 4.4 Bot families and classic behavior

| Family | Required union / compatibility boundary |
|---|---|
| BaseBuilderBotModule (CA/Common/Frans/CN) | CA adds production classes, refinery law, advisor placement and split building/defense cash thresholds; Common supply/tech/defense-range options must survive. Preserve current classic branch order and RNG draws. New donor strategies remain individually selected, not silently enabled. |
| UnitBuilderBotModule | Preserve CA compositions, derived weights, air superiority, queue/count controls and the Common queue-limit/supply collector path. Defaults differ: idle maximum 12 vs -1, cash threshold 2000 vs 501, SQ/MQ queues vs base queue names. Materialize per old user. |
| HarvesterBotModule | Preserve CA per-refinery/global/resource limits, production cadence and 8-cell avoidance versus Common 10; retain SP/Frans/CN capabilities behind the correct resource/collector contracts. |
| SquadManagerBotModule | Retain Cameo missions, leases, role mixing, tactical/formation/air/stealth/concave behavior and all increment gates. Do not replace the Cameo implementation with the newer CA file. Every off combination preserves its old order/RNG stream. Include squad state/activity helpers in the family closure. |
| CaptureManagerBotModule / CaptureManagerBotASModule / CncEngineer variants | Preserve priority-capture chance/types, avoidance and leases alongside base engineer behavior. Different names are already a known same-job family in TYPE_MERGE_PLAN; the name-based appendix alone does not settle consolidation. |
| LoadGarrisonerBotModule | CA adds expiring stuck suppression and AttackMove then queued EnterGarrison. The pinned integration also claims/renews/releases Garrison leases, cleans up on disable, and queues Stop before release. Preserve these integration fixes. CN threat-matched passenger swaps are an extension of this existing capability. |
| LoadCargoBotModuleAS (Cameo) / LoadCargoBotModule (AS) | Same direct fields, but the Cameo variant adds Mission leases, heartbeat/release/disable cleanup and per-instance roster fallback. Both YAML names have mounted users. Consolidate to LoadCargoBotModule without losing those integration changes or activating leases for classic where no registry exists. |
| BuildingRepairBotModule | CA reacts to increasing damage, guards a null attacker and differs from Common's cooldown/bulk-repair path and Light threshold. Preserve the currently selected repair implementation and donor options; do not reintroduce a retired sibling as a second owner. |
| SupportPowerBotModule / SupportPowerBotASModule | AS uses its decision type and target search rather than Common's coarse/fine search. Equal outer field names hide different decision schemas/search behavior. Merge nested decision types and target selection together. |
| McvManagerASBotModule / McvManager / McvExpansionManager / BevManager / Frans expansion | Preserve construction-yard count/cadence options (including old misspellings), anchor claims, expansion leases and refinery law. These are a documented different-name family; one expansion owner per bot. |
| PlugSpawnerBotModuleCA | Map singular Plug/Pluggables into the CA Plugs mapping, preserving IgnoreCost=false and actor ownership checks. No multiple production owners. |
| ModularBot, GrantConditionOnBotOwner and bot contracts | Preserve order admission, grouped-order validation, synchronized identity, lease ownership and takeover parity. Resolve conditional providers at use time; never cache a disabled provider in construction. |

The frozen learning spec's phase ownership remains authoritative. CN profile,
region and composition ideas feed its existing interfaces; they do not justify a
second learning coordinator or a competing module that also owns the same actors.

## 5. Capabilities not yet ported

The [uncovered appendix](TRAIT_UNIFICATION_UNCOVERED_2026_10_05.md) is the complete
declaration-level intake from the six donor sources, grouped by capability.
It distinguishes absent source from present-but-unmounted source. It does not
mislabel a differently named implementation as a proven missing mechanic.

Before promoting an intake row to a new trait, compare these known overlaps:
CNGarrisonBotModule ↔ LoadGarrisoner; CNRepairManager ↔ existing repair services;
CN profile/region/tactical modules ↔ Cameo's strategic/tactical contracts;
ClassicAirstrikePower ↔ AirstrikePower; InfiltrateToCreateProxyActor ↔
InfiltrateForSupportPower; CNSteeredMobile ↔ Mobile; SpawnHuskEffectOnDeath ↔
existing death-spawn/projectile-husk mechanisms; old RV missile/spawner names ↔
the current missile actor family. The old RV missile aliases are already grouped
in the declaration comparison. “No same-family declaration” is an honest search
boundary, not a claim that all these mechanics need independent implementations.

Port value order after existing-family consolidation:

1. **Dependency unlocks:** missing projectile options and the small traits needed
   to restore dormant content; port before mounting its weapons.
2. **Cross-faction gameplay capabilities:** CA target-filtered support powers,
   unit conversion and upgrades, resource/experience reclamation; Generals supply
   collector/dock/center pipeline; RV temporal effects. Preserve ownership, fog,
   damage and economy contracts; content activation is a separate review.
3. **AI extensions through existing owners:** CN region/cliff/veinhole behavior,
   threat-aware garrison changes, supply logistics and repair policies. Reuse the
   lease/order gate, fog memory and canonical learning interfaces.
4. **Rendering and environment:** CN voxel dynamics, water/cloud/Tiberium effects,
   day/night and weather; SP weather/corpse effects. Identify required renderer,
   terrain and graphics-setting engine hooks before scheduling a port.
5. **Campaign/UI and optional presentation:** campaign progress, notifications,
   encyclopedia, decorations, voices and palette additions, preserving local-only
   presentation versus synchronized gameplay boundaries.

For every intake row, the implementation ticket must identify its destination
family or establish that it is a distinct mechanic. Every donor field and behavior
is either adopted, already represented, or explicitly blocked by a named dependency.
No feature is silently discarded merely because current Cameo YAML has zero users.
This spec does not authorize enabling cheats, changing balance, or changing classic.

## 6. Migration and compatibility

Use a **family-specific OpenRA UpdateRule** (the existing `UpdateActorNode` and
`UpdateWeaponNode` hooks in `OpenRA.Mods.Common/UpdateRules/UpdateRule.cs`) plus a
manifest/map traversal wrapper. The rule transforms MiniYamlNodeBuilder trees,
not regex text. A script alternative must use the existing MiniYAML parser and
preserve source locations, ordering, comments and cancellation nodes.

The migration manifest has one entry per old fully qualified implementation:
old YAML name, new plain name, source SHA, complete effective field schema,
old/default donor values, field aliases, semantic choices, and affected files/maps.
Do not derive default values from the spelling of C# initializers; load or explicitly
evaluate the pinned Info contract in a test fixture, including inherited fields.

Conversion procedure per family:

1. Capture resolved before-state for every affected actor/weapon and map ruleset.
   Enumerate mounted files, dormant files and packed map overrides separately.
2. Rewrite trait keys **including `-Trait@instance` cancellations**, preserving the
   `@instance` suffix and relative order. Rewrite projectile/warhead **values** only
   in their semantic nodes. Do not globally replace matching text in actor IDs,
   order names, voice IDs, filenames or descriptions.
3. Materialize old effective defaults at the appropriate inherited definition or
   concrete override. Map renamed fields and explicit semantic modes. Fail on
   ambiguous scalar/list combinations or conflicting old/new fields.
4. Detect collisions where two old traits become the same `NewTrait@instance`,
   including inherited removals/re-additions. Require a reviewed instance-key
   mapping; never concatenate both stateful implementations automatically.
5. Compare resolved before/after state with the declared mapping applied, then
   compare executable decision traces. Schema equivalence is necessary but cannot
   prove lifecycle, order, collision or RNG equivalence.
6. Run the conversion twice: second run must produce zero changes. Register source
   parent hashes in `merged_bot_modules.json` and run its audit. Preserve licenses.
7. Delete vendored sibling implementations only when all C# typed callers, YAML
   users, activities, effects, scripting wrappers and maps are migrated. Temporary
   aliases forward to the unified implementation and have an explicit removal date.

Important mappings include PointDefenseType → singleton PointDefenseTypes,
ValidBounceBlockerStances → ValidBounceBlockerRelationships, scalar SquadSize →
an explicit constant-size mode alongside level-keyed SquadSizes, scalar
condition-power fields → an explicit non-level-dependent mode, TargetCircleColor →
one color entry, and old bot misspellings → canonical names with compatibility
aliases during transition. Ambient volume, GPS glyph/sequence, condition timing,
turning behavior and trail impact routing are **semantic** mappings, not simple
renames. New field names use the project's existing C# conventions; newly created
file/identifier assets follow underscore-only repository naming.

For the conflicting Dimensions field, keep the current Common level-keyed
`Dimensions` dictionary and add scalar `FixedDimensions`. A proposed
`UsePowerLevels` flag defaults true for the mounted Common condition/airstrike
powers; CA condition-power and AS scalar-squad conversions explicitly set it false.
Keep the scalar Condition/Duration/Footprint and SquadSize options alongside their
level-keyed forms. Do not map a constant behavior to just level 1: it would break
when the power's upgrade level changes. These are explicit implementation contracts,
not claims that those new fields exist today.

**Saves and replays:** YAML name aliases do not guarantee compatibility. Trait order,
type identity, serialized state, order streams and RNG progression can change.
Keep the old mod/engine package available for old saves/replays; version the new
package and treat cross-version continuation as unsupported until a dedicated
serialization/replay proof exists. Do not rewrite replay bytes or saved runtime
state with the YAML updater. Multiplayer peers must use the same new package.
The initial migration target is same-version deterministic behavior parity, not
an unsupported claim that historical recordings still replay on new binaries.

## 7. Phases and acceptance gates

| Phase | Scope | Specific gate beyond the common gate |
|---|---|---|
| U0 — migration infrastructure | Selective Unified/Contracts projects, resolution inventory, family manifests, updater, aliases, source-hash tracking | Assert factory winner and CLR assignability; reject unknown/dropped fields; nested inheritance, `@instances`, cancellation collisions, dormant files and map overrides; updater idempotence. No gameplay changes. |
| U1 — projectiles | LaserZap, Bullet, Missile, WarheadTrailProjectile; then AreaBeam/Railgun/TeslaZap candidates | Same seed gives same trajectory/impact tick/position/damage sequence and RNG draw progression for each old variant; laser depth/color/flare cases; trail expiration/impact multiplicity; missile target-loss, jam, altitude, point-defense and burst filters. |
| U2 — presentation and simple conditions | Palettes, circles, trails, overlays, sounds, bot-owner condition and prerequisite pair | Enable/disable, remove/re-add, capture, death and spectator/owner visibility matrix; prerequisite callback phase; sound timing/volume; renderer type resolution. |
| U3 — attack/control/spawner families | Leap/bomber/garrison/prism, airstrike/missile/drone spawners, infection, mind control, chrono, death warheads | Master/slave death and owner transfer; condition-token cleanup; frame-end spawn/dispose; classic order timing; typed-engine consumer checks. |
| U4 — economy and support powers | Producers, queues, purifier, ammo, airdrop/paradrop, nuke/detonate/condition/airstrike powers | Cost/resource preservation, no doubled production, exact charge/reload clocks, target filters, support-power decision schema and fog restrictions. |
| U5 — bot closure | Contracts and implementation migration completed across CA/Cameo/Frans, all different-name bot families | All old classic/off configurations produce identical accepted order streams and RNG progression; deny/lease lapse/release paths; null provider/off path; group orders; takeover; conditional-provider timing. Canonical learning phase ownership unchanged. |
| U6 — uncovered capabilities | Intake order in §5; first extend existing families, then add distinct mechanics | Dependency closure and a minimal concrete YAML consumer per capability; old consumers remain unchanged. New gameplay defaults off until the coordinator approves the increment. |
| U7 — retirement | Remove migration aliases/obsolete vendored types, documentation and package cleanup | No reference to retired names across C#, mounted/dormant YAML, map overrides or scripts; pin-aware donor drift audit; old packages retained for recordings. |

Each implementation phase requires focused tests, a C# build, the appropriate
lint/audits and the **isolated boot gate**. The standing instruction is:
`powershell -File C:/cameo-wt/boot_isolated.ps1 -Worktree <worktree>`; report the
actual `BOOT_GATE=PASS`/`FAIL` line and prove the package path is inside that tree.
Rebuild after changes; never use another process's shared log. Spec-only work has
not run this gate and must not report a pass.

Use `review_resolve_diff.py`, `audit_merged_bot_modules.py`, and `audit_map_actors.py`
for their established scopes; include `find_empty_warhead.py` and warhead-field
checks when relevant. A new trait/projectile field loader test must catch unknown
fields as well: the existing warhead-only checker cannot cover them. Do not run
the prohibited `--check-yaml` or `make test` shortcuts. Pure decision seams require
the coordinator's branch-coverage gate; world-bound modules retain their ratchet.
Gameplay, multiplayer smoke and campaign/A-B validation remain with Claude and the
assigned owners. This document provides no such validation result.

## 8. Hand-off and remaining implementation decisions

The family schema/default records and source diffs make every proposed migration
reviewable, but they do not turn 1,000+ declarations into automatically safe ports.
Before authoring a family's implementation, its owner must close the CLR consumer
closure, inherited-field loader validation, and behavioral-mode cases named here.
These are implementation gates, not permission to erase an unreviewed difference.
If a donor description conflicts with code, code wins; the uncovered appendix
already corrects the stale TargetSpecificOrderVoice description from its runtime.

No behavior source, YAML gameplay rule, engine pin, learning spec or shared branch
was changed for this task. Evidence generation used scratch scripts under
`C:/cameo-wt`; deliverables and coordination are in the fleet folder. The complete
capture is also retained as `C:/cameo-wt/boss_trait_full_capture.json`, so subsequent
formatting reads frozen results rather than measuring a moving checkout again.
