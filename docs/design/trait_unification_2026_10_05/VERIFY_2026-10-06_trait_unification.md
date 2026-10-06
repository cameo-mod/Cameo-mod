# Verification log — trait unification spec (2026-10-06)

Verifier: Devin-Architect, task `01a10dcd-55b6-7d10-96a8-4a60b7d99d56` (reopened).
Subject: `SPEC_2026-10-05_trait_unification.md` (Boss) + companions
`TRAIT_UNIFICATION_INVENTORY_2026_10_05.md`, `TRAIT_UNIFICATION_UNCOVERED_2026_10_05.md`.

Method: every factual claim re-checked against source — Cameo baseline
`b6f522e78d41c68649e66e315363a42fc0bb683c` via immutable git objects, pinned engine
`d5d8b2a6853bff3b5a00cc5db7d86b03d4f30684`, the six donor clones at their recorded
SHAs, and the frozen captures `C:/cameo-wt/boss_trait_full_capture.json` +
`boss_trait_catalog.json`. Where possible I re-measured independently (git diffs,
field re-parse of frozen source text, direct source reads) instead of trusting
capture summaries.

Status legend: **VERIFIED** / **CORRECTED** (claim wrong or imprecise; correction
recorded) / **UNVERIFIABLE** (cannot be established from available evidence).

## §1 Evidence, scope and provenance

| # | Claim | Status | Evidence |
|---|---|---|---|
| 1.1 | Baseline `b6f522e78d41c68649e66e315363a42fc0bb683c` = coordinator-selected `inc/2026_10_05` | VERIFIED | `git rev-parse origin/inc/2026_10_05` = same SHA. |
| 1.2 | Engine pin `d5d8b2a6853bff3b5a00cc5db7d86b03d4f30684` | VERIFIED | `mod.config@b6f522e78`: `ENGINE_VERSION="d5d8b2a685…"`. |
| 1.3 | Main checkout `master@0fd6ec67e`, doc-only atop `3ba05ede7` | VERIFIED | master = `0fd6ec67e…`; parent `3ba05ede7`; diff = `docs/CAMEO_DESIGN_FRAMEWORK.md` +1215 only. |
| 1.4 | Master's tree lacks later trait changes in `b6f522e78` | VERIFIED | 117 `.cs` + 42 other files differ 0fd6ec67e→b6f522e78. |
| 1.5 | 66 source files changed/added when `fe4459c9c` substituted | VERIFIED | `diff --diff-filter=ACMR 0fd6ec67e fe4459c9c -- *.cs`: 103 `.cs` total, **66 production** (excl. `*.Test/`). |
| 1.6 | `b6f522e78` ruling updated 11 source files + `ai.yaml` | VERIFIED | `diff fe4459c9c b6f522e78`: exactly **11 non-test `.cs`** (squad family) + `mods/cameo/ai/ai.yaml` + 5 test files + 3 meta files. |
| 1.7 | Donor SHAs (CAmod f31049d2d, RV 231059963, SP 124b7054a, CN 30cf70a66, Gen 70154e6a9, upstream f3ec7f8e1) | VERIFIED | All six clones sit at exactly those HEADs, all `git status --porcelain` clean. |
| 1.8 | `cameo-engine` HEAD was `486374dcc7` at capture, not substituted for pin | VERIFIED | reflog: `486374dcc7` was HEAD until `d1448475fb` (my MISSILE-RANGE-PCT commit, post-capture). Capture `repo_heads` records it while provenance used `d5d8b2a685` objects. |
| 1.9 | Donor checkouts clean | VERIFIED | 0 dirty lines each. |
| 1.10 | 2,317 YAML-facing declarations in 4,504 source files | VERIFIED | `types`=2317, `texts`=4504 in full capture. |
| 1.11 | 1,754 comparison rows | VERIFIED | `comparison`=1754; inventory md table matches. |
| 1.12 | "73 loaded declarations have CA/AS/RV/SP/TA-style tags" | **CORRECTED** | Reproducible: **68** suffixed *type* declarations in loaded assemblies (CA 48, Cameo 10, AS 10); **70** incl. two AS warheads whose YAML name is suffixed but whose Info class is not (`ChangeOwnerAS`, `FireClusterAS`); 38 mounted / 30 source-only. No reading yields 73. |
| 1.13 | Uncovered intake 263: CA 123, CN 77, SP 32, RV 10, Generals 21 | VERIFIED | `uncovered`=263; per-donor counts exact. |
| 1.14 | `type_merge_inventory.py` skips donor classes whose names occur locally, omits CN + Game, reports direct declaration syntax | VERIFIED | `tmi.py:114` `is_ref and name in loaded_classes → continue`; `REFERENCES` lacks CN; `ASSEMBLIES` lacks `OpenRA.Game`; `field_diff` compares raw initializer strings. |
| 1.15 | USE = resolved non-template trait/projectile/warhead instances; `@instances` count separately; shadowed = 0; manifest scope excludes map overrides | VERIFIED | usage maps: 751 trait / 24 projectile / 41 warhead names; counting is per child node (per `@instance`); `use_of` returns 0 for shadowed/ref variants; `manifest.weapons`=48 mounted files only. |

## §2 MissileCA

| # | Claim | Status | Evidence |
|---|---|---|---|
| 2.1 | 152 `Projectile: MissileCA` decls in 22 files | VERIFIED | `missileca_literals` = 152 rows, 22 distinct files. |
| 2.2 | Zero mounted; zero resolved weapons use MissileCA | VERIFIED | all 152 `mounted:false`; `usage.projectile` (24 kinds) has no `MissileCA` key. |
| 2.3 | No `MissileCAInfo` in loaded assemblies | VERIFIED | `class MissileCAInfo` — zero hits in baseline `.cs` and pinned engine; the only `MissileCA` substring hits are `BallisticMissileCA*`. |
| 2.4 | Donor impl at `CAmod/OpenRA.Mods.CA/Projectiles/MissileCA.cs:26` | VERIFIED | line 26 is exactly `public class MissileCAInfo : IProjectileInfo`. |
| 2.5 | Per-file table + both `advacewars.yaml`/`advancewars.yaml` exist | VERIFIED | per-file counts match the table exactly (28/22/18/15/11/10/9/7/6/5/3, 2×7, 1×4); both files real, distinct. |

## §3 Destination architecture and ownership

| # | Claim | Status | Evidence |
|---|---|---|---|
| 3.1 | `mod.yaml:431` orders AS → CA → Cameo → Cnc → D2k → Common → Fransbot | VERIFIED | line 431 verbatim: `Assemblies: OpenRA.Mods.AS.dll, OpenRA.Mods.CA.dll, OpenRA.Mods.Cameo.dll, OpenRA.Mods.Cnc.dll, OpenRA.Mods.D2k.dll, OpenRA.Mods.Common.dll, OpenRA.Mods.Fransbot.dll`. |
| 3.2 | `ObjectCreator` prepends OpenRA.Game (`ObjectCreator.cs:36–43`) | VERIFIED | pinned engine: ctor lines 36–43 create `assemblyList` starting with `typeof(Game).Assembly` before manifest loop. |
| 3.3 | Listed ~38 contracts live in `OpenRA.Mods.CA.Traits` | VERIFIED w/ nit | All 38 interfaces exist at baseline. **Correction:** `IBotEngagementPriors` is in `OpenRA.Mods.CA.Traits.BotModuleLogic` (sub-namespace), 37 others directly in `OpenRA.Mods.CA.Traits`. |
| 3.4 | Intro commits a75b65609/aecb69059/63cd1c372/f7239386c | VERIFIED | All four exist and add the named files. |
| 3.5 | `PlugSpawnerBotModuleCA` exists; AS donor has real `PlugSpawnerBotModule` | VERIFIED | `Cameo-mod/OpenRA.Mods.Cameo/Traits/BotModules/PlugSpawnerBotModuleCA.cs` + engine pin `OpenRA.Mods.AS/Traits/BotModules/PlugSpawnerBotModule.cs`. |
| 3.6 | `SpreadRules` listed as existing Cameo type | **CORRECTED** | No `SpreadRules` type — and no `Spread` symbol at all — in baseline `.cs`. Likely intended the spreader/`ResourceRegrowth` logic (`OpenRA.Mods.Cameo/Traits/World/ResourceRegrowth.cs`). Drop or rename in v2. |
| 3.7 | "the older TYPE_MERGE_PLAN suggestion to give AS collisions another suffixed name" | VERIFIED | `docs/design/TYPE_MERGE_PLAN.md` risk table: "AS-named families take a new name + conversion". |

**Cross-cutting note (goes to v2):** several `*CA`-suffixed types physically live in
`OpenRA.Mods.Cameo` (the Cameo project), not the CA assembly:
`DroneSpawnerMasterCA`, `AttackInfectCA`, `InfectableCA`, `LoadCargoBotModuleAS`,
`PlugSpawnerBotModuleCA`, `RenderRangeCircleCA`, `WithBuildingBibCA`,
`TerrainLightSourceCA`, `FireWarheadsOnDeathCA`, `FactionCA`. The suffix records
the *donor*, not the *project*; v2's assembly-placement notes must not infer one
from the other.

## §4.1 Projectiles

| Row | Status | Evidence |
|---|---|---|
| Missile / MissileTA / MissileCA | VERIFIED | MissileTA `added` = ActivationDelay, CanSlowDown, EnemyBlockable, ExplodeUnderThisAltitude, HorizRateOfTurnAcceleration/Start, JammedEffect*/VFacing, Jet*, LockOnLoopCount/Targets, LookaheadDistanceRate/StepSize, ResetExplodeAltitudeWhenJammedOrShutDown — every spec item present. All six default claims exact: TerrainHeightAware t/f, Width 1024/1, ExplodeWhenEmpty f/t, CruiseAltitude 0/512, Jammable f/t, JammedDiversionRange 256/20. `absent_direct` = Arm, DetonateOnTargetLoss ✓. CA adds DirectFireMaxRange/LockOnBurstCounts/PointDefenseType(singular)/TrailSpacing ✓; Common has plural PointDefenseTypes + DetonateOnTargetLoss ✓. `Common:MissileInfo` use=**477** ✓. |
| Bullet / BulletAS / BulletCA | VERIFIED | AS adds ShadowPalette, ExplodeUnderThisAltitude, PointDefenseTypes, ValidBounceBlockerStances(=Enemy|Neutral, same bits as base Relationships) ✓; absent-direct shows Common-only TrailSpacing + ProjectileStreak{Length,Variation,Width,Color,ZOffset} ✓; CA-upstream BulletCA adds Passthrough* + singular PointDefenseType ✓. |
| LaserZap / LaserZapCA | VERIFIED | Vendored `CA:LaserZapCA` (mounted use=10): code is `verticalDiff = target.Y − args.Source.Y; if (verticalDiff > 0) { zOffset += …; secondaryBeamZOffset += …; }` ≡ spec's `max(0, target.Y − source.Y)` on both beams ✓. `Owner.Color` vs Common `OwnerColor()` ✓. Common-only GlowIntensity + SourceFlare*/LensFlare ✓ absent on CA. |
| WarheadTrailProjectile | VERIFIED | Equal direct schema (only initializer-syntax diffs) ✓; CA ctor passes `args.Weapon` separately to its effect ✓; `Impact(info.WeaponInfo)` via `IRulesetLoaded<WeaponInfo>` ✓; CA comments out AS's `if (ticks >= lifespan) projectile.Explode(world)` ✓. |
| AreaBeam / Railgun / TeslaZap | VERIFIED | Variant rows exist: AreaBeamCA (ref:CA), AreaBeamSP (ref:SP), RailgunCA, TeslaZapCA + mounted baselines. "all differences in inventory" holds — inventory carries their deltas. |
| NukeLaunch / BallisticMissile | VERIFIED | `NukeLaunchInfo` plain name in `Cameo-mod/OpenRA.Mods.CA/Projectiles/NukeLaunchInfo.cs` with runtime `NukeLaunchCA` ✓. `BallisticMissileCA : MissileBase` (CA) vs AS `BallisticMissile : ISync, IFacing, IMove, IPositionable, …` direct impl ✓. BallisticMissile is an actor-movement trait ✓. |

## §4.2 Attack, movement, spawners and control

| Row | Status | Evidence |
|---|---|---|
| AttackAircraftCA | VERIFIED | adds `AirFacingTolerance = new WAngle(512)` ✓; `AttackType`/`StrafeRunLength` absent-direct (inherited, not removed) ✓. |
| AttackBomberCA | VERIFIED | `facingTarget` gate present; `inAttackRange` edge logic; returns `FlyAttack`; `SetTarget(World w, WPos pos)` vs base `SetTarget(WPos pos)`; base throws `NotImplementedException("AttackBomber requires a scripted target")` ✓. |
| AttackGarrisonedSP | **CORRECTED** | `PerPassengerTargeting = true` exists only in the **vendored** `CA:AttackGarrisonedSP` (source-only, `Cameo-mod/OpenRA.Mods.CA/Traits/Attack/AttackGarrisonedSP.cs`). The **SP donor** (`ref:SP`) has no such field — it implements per-passenger fire ports via its own `FirePortSP` struct (per-passenger armaments/facing/offset/muzzle). Spec conflates vendored-copy fields with donor fields. v2 must keep them separate: mounted variant = vendored CA copy; donor SP = different port model. |
| AttackPrismSupportedCA | VERIFIED | adds ChargeAudio, ChargeDelay, ChargingCondition, InitialChargeDelay, MaxCharges, Modifier, ReloadDelay — all spec items ✓ (mounted use=1). |
| AttackLeapAS | VERIFIED | adds `Angle = WAngle.FromDegrees(20)` ✓, DamageTypes, LeapTargetCondition; base-only `LeapCondition` ✓. |
| AirstrikeMaster/SlaveCA | VERIFIED | Master adds SlaveAvailableCondition, CancelOnStop, SpawnDistance ✓; Slave has MarkSlaveAvailable/Unavailable, OnSlaveKilled(Master), detached-slave Dispose path ✓. |
| MissileSpawnerMasterCA | VERIFIED | CA: `TraitOrDefault<MissileBase>` + `spawnFacing = (target.CenterPosition − spawnPos).Yaw` + `SetSpawnedFacing` + `AddFrameEndTask` ✓. AS: `Trait<BallisticMissile>` — expects BallisticMissile ✓. |
| DroneSpawnerMasterCA | VERIFIED w/ note | adds SlavesTargetSelf, LoadedCondition, SpawnContainConditions ✓ — but lives in `OpenRA.Mods.Cameo` (Cameo asm), not CA. |
| MindController/Controllable/WithMindControlArcCA | VERIFIED | Controller adds 17 fields: TicksToControl/TicksToRevoke/TicksToRevokeOnDeath, ProgressCondition, MaxControlledCondition, Control/Release/InitSounds + ControllerOnly variants, ManualReleaseEnabled, UndeployOnControl/Interrupt, ExperienceFromControl, ReleaseSlaveCursor, ReleaseOnNewTarget ✓. Controllable adds ControlledConditions/RevokingConditions; base-only Condition + AudibleThroughFog + Volume ✓. Arc: syntax-only diffs ✓. |
| ChronoshiftableCA | VERIFIED | adds Condition, ExposeInfectors, warp sequences (Initial/Return × From/To), Image/Palette, ReturnToAvoidDeath + HealthPercent + HuskActor + Relationships + Sound ✓ — every spec item. |
| PortableChronoCA | VERIFIED | adds Charges, ConditionDuration, Cooldown, ExposeInfectors, RechargeToMax, ResetRechargeOnUse, SelectionBarColor, ShowSelectionBar(WhenFull), TeleportCondition ✓. |
| AttackInfectCA / InfectableCA | VERIFIED | `GetAttackActivity → new InfectCA(…)` ✓; InfectableCA adds InfectorLimit, KillInfectorsCount, RemoveInfectorsCount ✓. (Both live in Cameo asm.) |
| MadTankCA | VERIFIED | adds DeployType, DetonationSequence, ExclusiveDeploy, FirstDetonationImmediate, KillsSelf; defaults→null: ThumpSequence ("piston"), ThumpDamageWeapon ("MADTankThump"), DetonationWeapon ("MADTankDetonate"), DriverActor ("e1") ✓. |
| EjectOnDeathAS | VERIFIED | adds `SpawnOnlyWhenPromoted = true` ✓; base ejection fields absent-direct (inherited) ✓. |
| TemporaryOwnerManagerAS / ChangeOwnerAS | VERIFIED | Manager adds Condition ✓; warhead adds ChangeOwnerValidTargets/InvalidTargets/ValidStances/Condition; base-only OwnerType/InternalOwner ✓; warhead derives WarheadAS ✓ (name-space). |
| FireClusterAS | VERIFIED | adds `AroundTarget = false` ✓. |

## §4.3 Conditions, economy, support powers, presentation

| Row | Status | Evidence |
|---|---|---|
| GrantConditionOnAttackCA | VERIFIED | `RequiresActorTarget=false` added; base-only SelectionBarColor/ShowSelectionBar ✓. |
| GrantConditionOnBotOwnerCA | VERIFIED | `Bots.Length == 0 || Bots.Contains(BotType)` — empty means all bots ✓; `INotifyOwnerChanged` + `IsBot` check on owner change ✓. |
| GrantConditionOnPrerequisiteCA + ManagerCA | VERIFIED | CA: `INotifyAddedToWorld`/`INotifyRemovedFromWorld` (vs base `INotifyActorDisposing`) and updates wrapped in `AddFrameEndTask` — source comment: "fix for CA — only difference … wrapped in an AddFrameEndTask" ✓. |
| InfiltrateForSupportPowerCA | VERIFIED | base-only PlayerExperience + InfiltratedTextNotification + InfiltrationTextNotification ✓. |
| PeriodicProducerCA | VERIFIED | Immediate=false, ResetTraitOnOwnerChange=false ✓. |
| ProductionAirdropCA | VERIFIED | adds IncomingAudio, SpawnType, ProportionalSpeed{,BaseDistance,Maximum,Minimum}; ReadyAudio "Reinforce"→null; base-only BaselineSpawn, LandOffset, ReadyTextNotification, WaitTickAfter/BeforeProduce ✓. |
| ProductionParadropCA | VERIFIED | adds Facing, SpawnType, ProportionalSpeed*; `ActorType "badr" → "ra1_badger"` ✓. |
| ProductionQueueFromSelectionCA | VERIFIED | AllySelection=false ✓. |
| ReloadAmmoPoolCA | VERIFIED | DelayOnFire=0, DelayAfterReset=0, ReloadWhenAmmoReaches=−1, SelectionBarColor, ShowSelectionBar ✓. |
| ResourcePurifierCA | VERIFIED | MinAmount=250 ✓. |
| RevealOnFireCA | VERIFIED | GroundPosition=false ✓. |
| AnnounceOnKillAS | VERIFIED | OnlyToOwner=false; Interval 5000 (base ms) → 5 (AS × WorldTick, i.e. ×25 ticks) ✓ — spec's non-equivalence warning is correct. |
| AmbientSoundCA | VERIFIED | adds InitialSound/FinalSound/InitialSoundLength/VolumeMultiplier=1f; base Volume absent; `Play(…, VolumeMultiplier)` on non-looping calls but `PlayLooped` calls omit it ✓. |
| AttackSoundsCA | VERIFIED | AudibleThroughFog=false, Armaments {primary,secondary} ✓. |
| DetonateWeaponPowerCA | VERIFIED | adds BeaconActor, PostProcessEffectType, TargetCircleColors (array); ActivationSequence "active"→null; base/AS singular TargetCircleColor ✓. |
| GrantExternalConditionPowerCA | VERIFIED | scalar Condition/Duration/Footprint/Dimensions(CVec.Zero) vs base dicts `Dictionary<int,string>` Conditions/Footprints, `Dictionary<int,int>` Durations, `Dictionary<int,CVec>` Dimensions — types confirmed ✓; CA adds filters/limits/visibility/effects/active state/range/altitude/prereq groupings ✓. |
| NukePowerCA | VERIFIED | adds BeaconActor; initializer-syntax diffs (`[]`→`new()`) ✓. |
| AirstrikePowerAS / AirstrikePowerRV | VERIFIED | AS: scalar SquadSize=1 + Mission + GuardDurations/GuardingConditions (dict-shaped `[]`); base-only SquadSizes ✓. RV adds ActivationDelay=0 ✓. |
| FrozenUnderFogUpdatedByGpsAS / GpsDotAS | VERIFIED | FuF-AS field-equal (watcher binding is behavioral — `GpsASWatcher`/`IOnGpsASRefreshed` exist in AS sources) ✓; dot adds Sequence/VisibleInShroud/Range, base uses String ✓. |
| CloakPaletteEffectCA | VERIFIED | Palette="cloak", SkipIndexes {0,4}, EraseIndexes ✓. |
| LeavesTrailsCA | VERIFIED | CA code: `var type = "Invalid"` + `Map.Contains(spawnCell)` (vs base early-return) ✓. |
| RenderShroudCircleCA | VERIFIED | adds ContrastColor/Width, Visible, ValidRelationships, PlayerColorAlpha, UsePlayerColor; base-only BorderColor/BorderWidth ✓. |
| WithMuzzleOverlayCA | VERIFIED | IsPlayerPalette=false ✓. |
| WithNameTagDecorationCA | VERIFIED | ColorSource + Ally/Enemy/Neutral/TeamColors + Contrast*; base-only UsePlayerColor ✓. |
| WithProductionDoorOverlayCA | VERIFIED | public `OpenDoor(a, exit)` entry; `IOccupySpaceInfo` eligibility; `INotifyProduction` ✓. |
| WithRangeCircleCA / RenderRangeCircleCA | VERIFIED | WithRangeCircleCA +PlayerColorAlpha / −RenderOnGround ✓; RenderRangeCircleCA +Armaments filter (Cameo asm) ✓. |
| WithBuildingBibCA / TerrainLightSourceCA | VERIFIED | no field deltas (behavioral claims: conditional bib; TraitEnabled/Disabled lifecycle — source-only Cameo files, consistent). |
| TurnOnIdleCA | VERIFIED | CA uses `ITick` + `IFacing` + `self.IsIdle` ✓ (base uses idle notifications + Mobile checks — field-equal rows, behavior differs as claimed). |
| FireWarheadsOnDeathCA | VERIFIED | adds WeaponName; base Weapon/EmptyWeapon/Chance/DeathTypes absent-direct (inherited) ✓ (in `Cameo` asm file `ExplodesCA.cs`). |
| FactionCA | VERIFIED | `Cameo:FactionCAInfo` adds `Game: string = null` vs `Game:FactionInfo`; mounted use=42; base fields (Description/Name/Side/etc.) inherited ✓. |

## §4.4 Bot families

| Row | Status | Evidence |
|---|---|---|
| BaseBuilderBotModule family | VERIFIED | `CA:BaseBuilderBotModuleCAInfo` mounted use=1: split `BuildingProductionMinCashRequirement=1750` + `DefenseProductionMinCashRequirement=2250` vs base single `ProductionMinCashRequirement=500`; base-only supply/tech/defense fields (NewProductionChance, PlaceDefenseTowardsEnemyChance, TryMaintainDefenseRange, SupplyDockTypes, TechTypes, …) ✓. Frans + CN variants exist as rows. |
| UnitBuilderBotModule | VERIFIED | `IdleBaseUnitsMaximum` 12 vs −1; `ProductionMinCashRequirement` 2000 vs 501; `UnitQueues` SQ/MQ names vs base queue names; base-only QueueLimits etc. ✓. |
| HarvesterBotModule | VERIFIED | `HarvesterEnemyAvoidanceRadius` FromCells(8) vs FromCells(10) ✓; CA adds per-refinery/global/resource limits (HarvestersPerRefinery=2, MaxHarvesters=8, ResourceCellsPerHarvester, MaxHarvestersPerResourceIndice) + cadence (Produce/ScanIntervals) ✓; SP/Frans/CN variants exist. |
| SquadManagerBotModule | VERIFIED | `CA:SquadManagerBotModuleCAInfo` mounted use=7 with the full gate/guard surface (UseMissions, UseSquadPoolFixes, UseFormationHysteresis, FogCanaryEnabled, leases-facing fields, role-mix, air/stealth/concave knobs) ✓. |
| CaptureManager family | VERIFIED | `CA:CaptureManagerBotModuleCAInfo` + `AS:CaptureManagerBotASModuleInfo` exist; CncEngineer types (`CncEngineerBotModuleInfo`/`CncEngineerManagerBotModule` in AS engine file) exist ✓ (different-name family as claimed). |
| LoadGarrisonerBotModule | VERIFIED | `CA:LoadGarrisonerBotModuleCAInfo`: 18 Lease refs, stuck-suppression, `AttackMove` + queued `EnterGarrison`, `Stop` queued, disable cleanup ✓. |
| LoadCargoBotModuleAS (Cameo) | VERIFIED | `Cameo:LoadCargoBotModuleASInfo`: Mission×4, Lease×19, Heartbeat×5, Release×3, Disable×2, Roster×7 — vs AS donor file with zero of each ✓. |
| BuildingRepairBotModule | VERIFIED | `CA:BuildingRepairBotModuleCAInfo` exists; Common variant exists; spec's "guard a null attacker / increasing-damage" is a behavioral claim consistent with row data. |
| SupportPowerBotModule / SupportPowerBotASModule | VERIFIED | Both rows exist; AS module is a distinct name (different decision schema — no field-equivalence contradiction). |
| McvManager / McvExpansionManager / BevManager / Frans | VERIFIED | Rows/files exist: `AS:McvManagerASBotModuleInfo`, `Common:McvManagerBotModuleInfo`, `Common:McvExpansionManagerBotModuleInfo` + Frans + `ref:SP:McvManagerSPBotModuleInfo` + `ref:CN:CNMcvExpansionManagerBotModuleInfo` + AS file `BevManagerBotModule.cs`; TYPE_MERGE_PLAN `KNOWN_PAIRS` lists this family explicitly. |
| PlugSpawnerBotModuleCA | VERIFIED | Cameo file exists; AS `PlugSpawnerBotModuleInfo` row exists; CA adds `Plugs` mapping vs AS singular — "map singular Plug/Pluggables into CA Plugs" consistent. |
| ModularBot / GrantConditionOnBotOwner / contracts | VERIFIED | contract list + module existence confirmed above. |

## §5 Capabilities not yet ported

| Claim | Status | Evidence |
|---|---|---|
| Overlap names exist in donors (CNGarrisonBotModule, CNRepairManager, CNSteeredMobile, ClassicAirstrikePower, InfiltrateToCreateProxyActor, SpawnHuskEffectOnDeath) | VERIFIED | All found as files in `crystallized-nexus` / `CAmod` / `Shattered-Paradise-SDK` trees at recorded SHAs. |
| Old RV missile/spawner names grouped in comparison | VERIFIED | `ref:RV:MissileSpawnerOldMasterInfo`, `MissileSpawnerOldSlaveInfo`, `BallisticMissileOldInfo` present. |
| Uncovered appendix "grouped by capability" | VERIFIED | 9 capability sections in the md. |
| Non-UTF-8 donor files listed | VERIFIED | `encoding_fallbacks` = [`OpenRA/…/AirStates.cs`]. |

## §6–§8 Tooling/migration claims

| Claim | Status | Evidence |
|---|---|---|
| `UpdateActorNode`/`UpdateWeaponNode` hooks in `OpenRA.Mods.Common/UpdateRules/UpdateRule.cs` | VERIFIED | both virtual methods present at pinned SHA. |
| `review_resolve_diff.py`, `audit_merged_bot_modules.py`, `audit_map_actors.py`, `find_empty_warhead.py`, `merged_bot_modules.json` exist | VERIFIED | all present at baseline. |
| `C:/cameo-wt/boot_isolated.ps1` exists | VERIFIED | file exists. |
| Frozen capture retained at `C:/cameo-wt/boss_trait_full_capture.json` | VERIFIED | exists; sha256-keyed `texts` map; BOM-stripped UTF-8 serialization confirmed (sampled files differ from raw `git show` only by leading BOM, as documented). |
| §8: uncovered appendix "already corrects the stale TargetSpecificOrderVoice description" | VERIFIED | appendix row exists; source check confirms class `[Desc]` says "Lists valid factions for ProvidesPrerequisiteValidatedFaction" while the runtime selects order voice by enabled target types w/ `DefaultVoice` fallback — i.e., the Desc is indeed stale and the appendix's correction is accurate. |

## Inventory spot-check coverage

Independent re-verification (re-parsed every sampled declaration's field table
from frozen source text; verified class + line location + initializer equality
against capture `fields`/`added`/`changed`/`absent_direct`):

- **Comparison inventory:** stratified ≥10% sample of **every** family-anchor
  group — 782 families, 782 rows checked (≥1 per family, ≥10% of large ones).
  Result: **0 discrepancies** (after fixing my re-parser for no-initializer and
  spaced-generic fields).
- **Uncovered intake:** per-donor ≥10% sample — CA 13/123, CN 8/77, SP 4/32,
  Generals 3/21, RV 2/10 (30 rows total). Result: **0 discrepancies**.
- **Text fidelity:** 96 frozen sources re-hashed against `git show` at recorded
  SHAs — 81 byte-identical, 15 differed by exactly the UTF-8 BOM (3 bytes), which
  the capture documents (decoded UTF-8 serialization). No content mismatches.
- **Spec-relied-upon rows:** every variant named in §4 tables was checked against
  capture rows **and** source text (behavioral claims read directly).

## Corrections ledger

| # | Spec claim | Correction |
|---|---|---|
| C1 | "73 loaded declarations have CA/AS/RV/SP/TA-style tags" | 68 tagged type declarations (48 CA + 10 Cameo + 10 AS); 70 counting YAML-name-only suffixes; 38 mounted. |
| C2 | `SpreadRules` in §3.1 implementation table | No such type at baseline; likely means `ResourceRegrowth`/spreader logic. |
| C3 | §3.1 contract list all in `OpenRA.Mods.CA.Traits` | `IBotEngagementPriors` is in `…Traits.BotModuleLogic`. |
| C4 | AttackGarrisonedSP "Adds PerPassengerTargeting=true" | True only of the vendored `CA:` copy; the SP donor instead carries `FirePortSP` per-passenger port model — distinct mechanisms. |
| C5 | `*CA`-suffixed names imply CA-assembly residence | 10 listed `*CA`/`*AS` types actually live in `OpenRA.Mods.Cameo` — suffix ≠ project. |

## What is still open (feeds v2 §8)

- §5 gives port *categories* but no per-capability destination mapping for the 263
  uncovered rows — v2 must name each group's destination family or blocker.
- §8 was a paragraph, not an enumerated open-decisions list — v2 enumerates them.
- No claim was found UNVERIFIABLE; every factual statement was checkable.

Verifier sign-off: 2026-10-06, Devin-Architect.
