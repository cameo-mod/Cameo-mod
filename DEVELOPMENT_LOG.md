# Development Log

## 2026-08-23 — Ixian giant multi-warhead 3-way split (boot-gated)

- Converted `D2K_TowerMissile` and `mtank_pri2` in
  `mods/cameo/ContentPacks/D2k/Ixian/yaml/weapons.yaml` from the old mixed
  `^Grenade`/`^MediumFlameWeapon`/`^FlakWeapon`/`^D2KMissile` full-stack pattern to
  explicit `Inherits@wh` / `Inherits@wh2` / `Inherits@wh3` (and `@wh4` for the tower)
  / `Inherits@proj` / `Inherits@fx`.
- Removed legacy full-stack inherits (`^Grenade`, `^MediumFlameWeapon`, `^FlakWeapon`,
  `^D2KMissile`). Both weapons were added to the `docs/design/WEAPON_3WAY_SPLIT.md`
  exception allow-list because their resolved giant multi-warhead identity requires
  more than two warhead layers (Demolition + Flame + Flak + MissileAP for the tower;
  Demolition + Flame + MissileAP for the tank).
- Added four D2K Shared templates in `mods/cameo/ContentPacks/D2k/Shared/yaml/weapons.yaml`:
  `^Projectile_Missile_Heavy_D2K_TowerMissile`,
  `^Projectile_Missile_Heavy_D2K_mtank_pri2`,
  `^Effect_MissileAP_Heavy_D2K_TowerMissile`, and
  `^Effect_MissileAP_Heavy_D2K_mtank_pri2`.
- Preserved resolved `Damage`, `Versus`, `Spread`, `Falloff`, `DamageTypes`,
  `PhysicalState`, `ReloadDelay`, `Range`, `MinRange`, `Report`, `ValidTargets`,
  `TargetActorCenter`, `Burst`/`BurstDelays`, `Projectile` visuals/turn behaviour,
  `Concrete`, glow, smudges, shield-hit, air/water effects, and the mixed
  Demolition/Flame/Flak/MissileAP warhead contributions on the tower.
- Verification:
  - `scratchpad/verify_ixian.py` (equivalent to `tools/audit/review_resolve_diff.py`)
    OK for both weapons
  - `tools/audit/effect_audit.py` → 0 duplicate `DamagesConcrete`
  - `tools/audit/find_empty_warhead.py` → 0 empty warheads
  - `tools/audit/find_orphan_old_keys.py` → 0 real bugs
  - `tools/audit/audit_warhead_split.py` at/below baseline (952)
  - `tools/audit/audit_balance_drift.py` → 32 ledgers clean (re-extracted via `extract_stats.py`)
  - `launch-game.cmd` reached `MenuPostProcessEffect.PostWorldLoaded`; no new `exception-*.log`

- **Post-audit correction:** `tools/audit/review_resolve_diff.py` compared
  the core behavioural invariants, but a full resolved-vs-baseline diff
  (`scratchpad/compare_full.py`) showed the per-weapon `Versus` and warhead
  overrides still lived inside the weapon nodes. Restructured the two Ixian
  weapons so every `Versus` row lives in dedicated D2K Shared
  `^Warhead_*_D2K_TowerMissile` / `^Warhead_*_D2K_mtank_pri2` templates (with all
  plating rows present, missing ones at the 100% default), and the weapon nodes
  only carry `Inherits@wh`/`Inherits@wh2`/`Inherits@wh3` (and `@wh4` for the
  tower) plus `Inherits@proj`/`Inherits@fx`. This eliminates the `-Key:` removal
  hacks while preserving the resolved baseline exactly. Re-extracted all balance
  ledgers (`extract_stats.py`) and re-ran `audit_balance_drift.py` (clean).

## 2026-08-23 — D2K Rocket Trooper family 3-way split (boot-gated)

- Converted `D2K_Rocket_Trooper` (`mods/cameo/weapons/d2k.yaml`),
  `D2K_Rocket_Trooper1`/`D2K_Rocket_Trooper2` (`mods/cameo/ContentPacks/D2k/Ixian/yaml/weapons.yaml`),
  and `D2K_Rocket_Trooper_AA`/`D2K_Rocket_Trooper_AGOnly` (`mods/cameo/ContentPacks/D2k/Ordos/yaml/weapons.yaml`)
  from the old `Inherits: ^D2KRocket` / `Inherits: ^D2K_Cannon` full-stack pattern to explicit
  `Inherits@wh` / `Inherits@proj` / `Inherits@fx`.
- Removed legacy full-stack inherits (`^D2KRocket`, `^D2K_Cannon`). The triple-warhead
  Rocket Troopers were added to the `docs/design/WEAPON_3WAY_SPLIT.md` exception
  allow-list because their resolved damage identity requires three warhead layers.
- Added six D2K Shared templates in `mods/cameo/ContentPacks/D2k/Shared/yaml/weapons.yaml`:
  `^Projectile_Missile_Medium_D2K_Rocket_Trooper`,
  `^Projectile_Missile_Light_D2K_Rocket_Trooper1`,
  `^Projectile_Missile_Light_D2K_Rocket_Trooper_AA`,
  `^Projectile_Grenade_Light_D2K_Rocket_Trooper2`,
  `^Projectile_Grenade_Light_D2K_Rocket_Trooper_AGOnly`,
  and `^Effect_MissileAP_Heavy_D2K_Rocket_Trooper`.
- Preserved `Damage`, `Versus`, `Spread`, `ReloadDelay`, `Range`, `Report`, `ValidTargets`,
  `Projectile` visuals/turn behaviour, `Concrete`, glow, smudges, shield-hit, air/water
  effects, and the mixed Demolition/Railgun/Cannon warhead contribution on Trooper2/AGOnly.
- Verification:
  - `tools/audit/review_resolve_diff.py` OK for all five weapons
  - `tools/audit/effect_audit.py` → 0 duplicate `DamagesConcrete`
  - `tools/audit/find_empty_warhead.py` → 0 empty warheads
  - `tools/audit/find_orphan_old_keys.py` → 0 real bugs
  - `tools/audit/audit_balance_drift.py` → 32 ledgers clean (re-extracted via `extract_stats.py`)
  - `launch-game.cmd` reached `MenuPostProcessEffect.PostWorldLoaded`; no new `exception-*.log`

## 2026-08-23 — Documentation review + doc_claims reconciliation

- Completed a full discrepancy review of design/instruction/audit documents
  (`docs/research/doc_review.md` generated for inspection).
- Reconciled `docs/audit/doc_claims.yaml` with live measurements:
  `multi_main_fired_weapons` 975→939, `meters_filling_before_death` 118→122,
  `corrosion_meter_actors` 783→785, `w24_multi_main_fed` 386→385,
  `physical_state_fired_weapons` 449→450.
- `python tools/audit/audit_doc_claims.py` now passes (16/16 claims clean).
- Updated `docs/design/ROADMAP.md` to reflect live W2 status (`^LightFlameWeapon`
  still has 28 inheritors, not ready/done) and current generator drift
  (`verify_generator_sync.py` reports drift = 10 + `^Warhead_Sniper_Light` not emitted).
- Identified next D2K 3-way split targets after `DevBullet`/`PlasBullet`:
  `D2K_Rocket_Trooper` family (in progress by subagent) and Ixian giant multi
  (`D2K_TowerMissile`, `mtank_pri2` in
  `mods/cameo/ContentPacks/D2k/Ixian/yaml/weapons.yaml`).
- Outstanding cross-cutting drift (not D2K): `tools/balance/verify_generator_sync.py`
  reports 9 chemical warhead blocks out of sync with `gen_weapon_template.py`
  (`PhysicalStates` vs `PhysicalStateName`, `Corrosion` scale, `TiberiumDeath`
  vs `ExplosionDeath`). Pending maintainer/generator alignment before splicing.


## 2026-08-20 — D2K Devastator/Plasma cannon 3-way split (boot-gated)

- Converted `DevBullet` and `PlasBullet` in `mods/cameo/weapons/d2k.yaml` from the old
  `Inherits: ^D2K_Cannon` / `Inherits: DevBullet` pattern to explicit
  `Inherits@wh` / `Inherits@proj` / `Inherits@fx`.
- Added `^Warhead_CannonHE_Heavy_D2K_DevBullet`, `^Projectile_Shell_Heavy_D2K_DevBullet`,
  and `^Effect_CannonHE_Heavy_D2K_DevBullet` in
  `mods/cameo/ContentPacks/D2k/Shared/yaml/weapons.yaml`.
- Preserved `Spread: 666`, `Damage: 80000`, `Versus`, `DamageTypes`, `HealthPercentageDamage`,
  `Concrete: 3333`, `Glow`, `d2k_shockwave` impact sound/animation, `Projectile` speed/image,
  `Range`, `ReloadDelay`, `Report`, and all `EffectAir`/`EffectWater`/shield/smudges.
- Fixed the duplicate ground effect: the old `Warhead@3Eff: d2k_shockwave` and inherited
  `Warhead@Effect: d2k_small_napalm` were merged into a single `Warhead@Effect: d2k_shockwave`
  with `ValidTargets: Ground, Ship`.
- `PlasBullet` now shares the same three D2K Shared layers, overriding `ReloadDelay`,
  `Projectile` speed/image, and main warhead `Damage`/`Spread` only.
- Regenerated `d2k_harkonnen` balance ledger and derived sidecar.
- Verification:
  - `tools/audit/effect_audit.py` → 0 duplicate `DamagesConcrete`
  - `tools/audit/find_empty_warhead.py` → 0 empty warheads
  - `tools/audit/find_orphan_old_keys.py` → 0 real bugs
  - `tools/audit/audit_balance_drift.py` → 32 ledgers clean
  - `launch-game.cmd` reached `MenuPostProcessEffect.PostWorldLoaded`; no new
    `exception-*.log`

## 2026-08-22 — W24 cluster 9: D2K-rocket six-weapon split (boot-gated)

- Converted `GoliathRockets_AA`, `WraithRockets_AA`, `SunDogRockets`, `MissileTurret` (`mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml`), `ScoutRockets_AA` (`mods/cameo/ContentPacks/StarCraft/Protoss/yaml/weapons.yaml`), and `HeavyOrdosCombatTankRockets` (`mods/cameo/ContentPacks/D2k/Ordos/yaml/weapons.yaml`) to the single `^D2KRocket` archetype.
- Removed `^Chaingun`, `^FlakWeapon`, `^LightMissile`, `^MediumMissile` inherits and their old main/percentage warheads.
- Collapsed five identical damage warheads per weapon into one `Warhead@MissileAP_Heavy` with totals 30000/10000/10000/20000/10000/10000 and percentage twins 15/5/5/10/5/5.
- Preserved `Range`, `ReloadDelay`, `Report`, `ValidTargets`, `Burst`/`BurstDelays`, local `Projectile` overrides (including Wraith/HeavyOrdos `ContrailStartColor`/`ContrailEndColor` and launch angles), and restored the flak-bullet contrail visual fields (`ContrailZOffset`, `ContrailStartColor`, `ContrailEndColor`, `ContrailStartWidth`, `ContrailEndWidth`) as local overrides because `^Projectile_Missile_Heavy` drops them.
- Added local `Warhead@EffectWater: CreateEffect` (`Explosions: small_splash`) on all six because `^D2KRocket` (via `^Effect_MissileAP_Heavy`) does not define a water effect.
- Lowered `BROADCAST_BASELINE` in `tools/audit/audit_warhead_split.py` from 958 to 952.
- Regenerated balance ledgers and derived sidecars for affected factions (`d2k_ordos`, `starcraft_protoss`, `starcraft_terran`).
- Regenerated `docs/audit/latest/phase_b_survey.md`.
- Verification:
  - `tools/audit/review_resolve_diff.py` OK for all six
  - `find_empty_warhead.py` = 0
  - `find_orphan_old_keys.py` = 0 real bugs
  - `audit_warhead_split.py` at/below baseline (952)
  - `audit_physical_state_warheads.py` PASS
  - `audit_balance_drift.py` clean
  - `sweep_areadamage.py` dry-run no cluster changes
  - `extract_stats.py` clean
  - `launch-game.cmd` reached `MenuPostProcessEffect.PostWorldLoaded`; no new `exception-*.log`

## 2026-08-22 — W24 cluster 5: Tiberian Sun tiberium bazookas (boot-gated)

- Converted `TSTibBazooka` (Nod) and `TSChemBazooka` (Forgotten) to the 3-way split
  using `^Warhead_MissileAP_Light`, `^Projectile_Missile_Light`, `^Effect_MissileAP_Light`.
- Removed old `^LightChemicalWeapon` and `^LightMissile` inherits.
- Collapsed `6000` chemical + `24000` missile damage into one `Damage: 30000` main and
  `3` + `12` percentage into a single `Damage: 15` percentage warhead.
- Preserved the `Corrosion` physical state by keeping `PhysicalStateName: Corrosion` and
  scaling the amount to the merged warhead (`PhysicalStateScale: 20`) so the post-armor
  corrosion matches the old 6000-damage chemical contribution.
- Preserved ally-damage proportion with `FriendlyFireDamage: 90` on both main and
  percentage warheads.
- Preserved `spittrail` missile trail, `small_poof` ground effect, `med_explosion_air`
  air effect, `Concrete: 100`, shield-hit duration 6, and all smudges.
- Kept `TSChemBazooka`'s `SpawnSmokeParticle` cloud warhead.
- Fixed an attempted `-Warhead@EffectWater:` removal that failed because
  `^Effect_MissileAP_Light` does not define that key.
- Regenerated balance ledgers and derived sidecars for affected factions.
- Updated `docs/design/BALANCE_PROGRAM_PLAN.md` W24 status line.
- Verification:
  - `find_empty_warhead.py` = 0
  - `find_orphan_old_keys.py` = 0 real bugs
  - `audit_warhead_split.py` at/below baseline
  - `audit_physical_state_warheads.py` PASS
  - `audit_balance_drift.py` clean
  - `review_resolve_diff.py` OK for both weapons
  - `launch-game.cmd` reached `MenuPostProcessEffect.PostWorldLoaded`; no new
    `exception-*.log` after the fix.

## 2026-08-22 — W24 cluster 4: Dragon SAM (boot-gated)

- Converted `Dragon` in `mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml` to the
  3-way split using `^Warhead_MissileAA_Heavy`, `^Projectile_Flak_Heavy`, `^Effect_Flak_Heavy`.
- Removed old `^HeavyAAWeapon`, `^HeavyMissile`, and `^ImpactGlow` inherits; moved the
  `GlowImpact` warhead into the local effect layer.
- Preserved the homing `Missile` projectile with `Image: MISSILE`, `TrailImage: smokey`,
  inaccuracy 150, speed 500, launch/turn behavior, and the AA-only `ValidTargets: Air`.
- Collapsed two 6000-damage warheads into one `Damage: 12000` main and `Damage: 6`
  percentage, preserving `ValidRelationships: Neutral, Enemy`.
- Preserved `big_frag` / `small_building` / `small_splash` impact effects, shield-hit
  duration 10, concrete damage 200, and all smudge behavior.
- Lowered `BROADCAST_BASELINE` in `tools/audit/audit_warhead_split.py` from 972 to 970.
- Regenerated balance ledgers and derived sidecars for affected factions.
- Updated `docs/design/BALANCE_PROGRAM_PLAN.md` W24 status line.
- Verification:
  - `find_empty_warhead.py` = 0
  - `find_orphan_old_keys.py` = 0 real bugs
  - `audit_warhead_split.py` at/below baseline
  - `audit_balance_drift.py` clean
  - `review_resolve_diff.py` OK for `dragon`
  - `launch-game.cmd` reached `MenuPostProcessEffect.PostWorldLoaded`; no new
    `exception-*.log`.

## 2026-08-22 — W24 cluster 3: FutureTech missile javelins (boot-gated)

- Converted `FutureJavelinRockets`, its children (`_elite`, `Deployed`, `Deployed_elite`),
  and `Future_MultiMissile_Javelin` to `^Warhead_MissileAP_Light` with the 3-way split.
  Removed old `^LightMissile`, `^FlakWeapon`, `^MediumMissile`, `^ShrapnelWeapon`, and
  `^D2KRocket` inherits. Preserved resolved `d2k_RPG` projectile image/trail, `ROCKET1.WAV`
  report, ranges, reload delays, burst offsets, and all impact effects.
- Collapsed five duplicate damage warheads per weapon into one `Damage: 10000` main and a
  single `Damage: 5` percentage warhead.
- Lowered `BROADCAST_BASELINE` in `tools/audit/audit_warhead_split.py` from 977 to 972.
- Regenerated balance ledgers and derived sidecars for affected factions.
- Updated `docs/design/BALANCE_PROGRAM_PLAN.md` W24 status line.
- Verification:
  - `find_empty_warhead.py` = 0
  - `find_orphan_old_keys.py` = 0 real bugs
  - `audit_warhead_split.py` at/below baseline
  - `audit_balance_drift.py` clean
  - `review_resolve_diff.py` OK for all five weapons
  - `launch-game.cmd` reached `MenuPostProcessEffect.PostWorldLoaded`; no new
    `exception-*.log`.

## 2026-08-22 — W24 cluster 2 + weapon-family corrections (boot-gated)

- Corrected `wc2cannontowerFire` to `CannonHE_Heavy` and `wc2dragonFireVisible` to
  `Flame_Heavy` after maintainer review; preserved resolved projectile/effect behaviour.
- Converted W24 cluster: `SporemawShoot`, `wc2demolitionsquadExplode`,
  `wc2mageFireballVisible`/`wc2mageFireballExplosion`, and child `wc2ogremageRunes_Hit`
  to `^Warhead_CannonAP_Light` with one warhead, one projectile, and one effect inherit.
- Moved Protoss `Inherits@corr: ^Corrodible` into `^LargeProtoss` and removed six
  redundant per-unit corrosion inherits (dragoon/archon now covered).
- Lowered `BROADCAST_BASELINE` in `tools/audit/audit_warhead_split.py` from 981 to 977.
- Regenerated balance ledgers and derived sidecars for affected factions.
- Updated `docs/design/BALANCE_PROGRAM_PLAN.md` W24 status line.
- Verification:
  - `find_empty_warhead.py` = 0
  - `find_orphan_old_keys.py` = 0 real bugs
  - `audit_warhead_split.py` at/below baseline (977)
  - `audit_balance_drift.py` clean
  - `review_resolve_diff.py` OK for all cluster weapons; `wc2ogremageRunes_Hit` intentionally
    collapsed from 10 inherited damage warheads + 1 child warhead to a single `Damage: 11250`
    main (expected Damage multiset flag).
  - `launch-game.cmd` reached `MenuPostProcessEffect.PostWorldLoaded` with no new
    `exception-*.log`.

## 2026-08-21 — Cryo/Inferno promoted to blend families (package 3)

- `tools/balance/gen_weapon_template.py`:
  - Removed `Cryo` / `Inferno` from `INHERIT_FAMILIES`.
  - Added `Cryo` = Laser×Prism and `Inferno` = Flame×Prism to `BLEND_FAMILIES`.
  - Updated `COMPOSITION` (`Cryo` energy 0.55 / thermo 0.25 / kinetic 0.20) and
    `COMPOSITION_OVERRIDE` (`Inferno` thermo 0.65 / energy 0.35).
  - Updated `PHYSICS_RANK` (`Cryo` 0.75, `Inferno` 0.57) and the blend-header comment.
  - Fixed blend header to print `no PhysicalStates` for empty state maps.
- Regenerated all 97 `^Warhead_*` templates in `mods/cameo/weapons/weapons.yaml`
  via `splice_templates.py --all`; `verify_generator_sync.py` reports drift = 1
  (the pre-existing hand-authored `^Warhead_Sniper_Light` only).
- Regenerated 32 balance ledgers and derived sidecars with `extract_stats.py`.
- Updated `docs/design/PHYSICAL_STATE_SYSTEM.md`, `docs/design/PLATING_COMPOSITION_REFINEMENT.md`,
  and `docs/design/BALANCE_PROGRAM_PLAN.md` to reflect the new family model.
- Verification: `extract_stats.py --check` 0 drift; `audit_balance_drift.py` clean;
  `audit_physical_state_warheads.py` PASS; `audit_armor_upgrade_harm.py` clean;
  `test_plating_composition.py` 10/10; `test_physical_state_price.py` 17/17;
  `find_empty_warhead.py` 0; `find_orphan_old_keys.py` 0 real bugs.
- Boot-gate: `launch-game.cmd` reached `MenuPostProcessEffect.PostWorldLoaded`,
  `exception-*.log` count 183 → 183 (no new exceptions).

## 2026-08-20 — Computed prerequisite-chain tech tier

- Added `tools/balance/tier_chain.py` with `TierChain(model)` resolving buildable
  prerequisites to a total building-chain cost `C`, restricted to the actor's
  own ContentPack leaf plus the same game's `Shared` pack. Cheapest valid provider
  selected per token; buildings deduplicated across branches; cycles are broken.
- `TierChain` indexes `Building` actors with `Valued.Cost` and both their actor
  name and `ProvidesPrerequisite` tokens as providers.
- `tools/balance/formula.py` now exports `TIER_B` (9500.0), `TIER_S` (8250.0),
  and `tier_multiplier(C)`. Docstrings updated to distinguish absolute
  (`class_anchor_price`) and relative (`class_baseline_price`) usage.
- `tools/balance/extract_stats.py` attaches `tier_chain_cost` and `tier_multiplier`
  to each buildable actor's `_derived` blob; manual `design.tech_tier` values are
  never overwritten.
- `tools/balance/fit_class.py` uses the absolute tier in `unit_inputs()`, preferring
  a manual `design.tech_tier` and falling back to the derived `tier_multiplier`.
- `tools/balance/propose_class_rebalance.py` computes per-class relative tier
  `f(C)/f(C_anchor)` for `class_baseline_price`; the anchor's manual `tech_tier`
  is used as the denominator when present.
- `tools/balance/build_workbook.py` writes the absolute `TechTier` to the
  spreadsheet and divides by the anchor's absolute tier inside the class-baseline
  `Price` and `RangeSolve` formulas.
- `tools/balance/check_band.py` loads derived sidecars, computes absolute unit
  tier, and uses the relative tier for `class_baseline_price` while keeping the
  absolute tier for `class_anchor_price`.
- Regenerated all 32 raw ledgers and 32 derived sidecars with `extract_stats.py`.
- Verified: `td_nod_lasertrooper` → `tier_chain_cost = 27000.0`, `tier_multiplier =
  0.3204`; its closure contains only Nod and Shared buildings (no GDI).
- `extract_stats.py --check` reports 0 drifted; `audit_balance_drift.py` is clean.
- `build_workbook.py` and `propose_class_rebalance.py --class mbt` run without
  errors; `fit_class.py --class scout --anchor naxis_naxiriflesoldier` produces
  a candidate and was reverted so `class_anchors.json` is unchanged.
- Updated `docs/design/tier_chain_validation.md`, `docs/design/ROADMAP.md`, and this log.
- Building-plug addons (`Plug:` trait) are not counted as separate actor-name
  providers, so `wc2_orcs_deathknight` resolves to $15,000 (Great Hall +
  Temple of the Damned) rather than double-counting the Fortress upgrade plug.

## 2026-08-19 — Delivery-weighted physical-state price multiplier wired into fit_class

- `tools/balance/extract_stats.py` now imports `physical_state_price` and calls
  `physical_state_price.actor_multipliers(rs)` once per extraction pass. The resulting
  per-actor record (`physical_state_weight`, `physical_state_multiplier`,
  `physical_state_weapon`) is attached to the actor's `_derived` blob and lifted into
  `docs/balance/derived/*.json` by `split_derived()`.
- `tools/balance/fit_class.py` now applies `formula.physical_state_price_multiplier()`
  in `price_unit()`, using the derived sidecar weight. The helper `physical_state_weight()`
  checks `u["_derived"]`, then the sidecar `du`, then the raw unit, defaulting to 0.
- Regenerated all 32 ledgers and derived sidecars (`extract_stats.py`).
- Verified with `fit_class.py --class line_breaker --anchor td_nod_flametank --use-k`:
  the anchor prices at **1000** against an actual cost of **800** (+25%), matching the
  full E2 ceiling. Non-state anchors (e.g. `mbt` / `tiger.nax`) price at cost0 with no
  surcharge.
- `find_empty_warhead.py` = 0; `audit_physical_state_warheads.py` PASS.
- Updated `docs/design/PHYSICAL_STATE_SYSTEM.md` and `docs/design/ROADMAP.md`.

## 2026-08-18 — ApplyPhysicalState → damage-scaled conversion (flame/chemical, boot-gated)

- Implemented `tools/balance/convert_apply_to_scaled_v2.py` (dry-run by default,
  `--apply` required, block-aware/line-based, no regex, preserves BOM/line endings,
  reports standalone cases).
- Converted legacy templates `^LightFlameWeapon`, `^MediumFlameWeapon`,
  `^HeavyFlameWeapon`, `^LightChemicalWeapon`, `^MediumChemicalWeapon`,
  `^HeavyChemicalWeapon` and all concrete overrides in 34 YAML weapon files:
  - `SpreadDamage` → `AreaDamage`
  - `HealthPercentageDamage` → `AreaDamagePercentage`
  - removed `Range:` from inside converted warheads
  - main warhead: `ValidRelationships: Ally, Neutral, Enemy`,
    `FriendlyFireDamage: 50`, `FriendlyFireSpread: 50`
  - main + percentage warheads: `PhysicalStateName` / `PhysicalStateScale`
    (`Temperature`/`300` for flame, `Corrosion`/`300` for chemical)
  - removed associated FriendlyFire twins and fixed `ApplyPhysicalState` warheads.
- Removed two stale `-Warhead@PhysicalStateMediumFlameWeapon*` removal lines in
  `mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml` that became invalid
  after the template physical-state warheads were removed.
- Verification:
  - `python tools/audit/audit_physical_state_warheads.py` PASS
  - `python tools/audit/find_empty_warhead.py` = 0
  - `utility.cmd cameo --check-yaml` completed without fatal YAML exceptions
    (pre-existing actor/condition warnings unrelated to this change)
  - `launch-game.cmd` reached the main menu (`MenuPostProcessEffect.PostWorldLoaded`
    in `%APPDATA%/OpenRA/Logs/perf.log`; no new `exception-*.log` after the run).
- Standalone `ApplyPhysicalState` cases left untouched: 43 non-target (cryo/non-family)
  blocks reported by the conversion script; flame/chemical `ApplyPhysicalState`
  warheads were removed.
- Note: `tools/audit/audit_physical_state_warheads.py` already expects
  `PhysicalStateScale: 300` in the working tree; do not commit without reviewing
  that diff.


## 2026-08-17 — RA2 effect-template final sweep (Shared/Allies/Yuri/redalert2mod/AsianAlliance/Syndicate, boot-gated)

- Completed the final `ra2_*` inline-effect sweep in the loaded RA2 tree
  (`mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml`,
  `mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml`,
  `mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml`,
  `mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml`,
  `mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml`,
  `mods/cameo/weapons/redalert2mod.yaml`).
- Removed the unused `^Effect_Disk_Ray_RA2` template.
- Updated `^Effect_Psi_Wave_RA2` with `ImpactActors: false` and `AffectsParent: true`
  and wired `PsiWaveX` to it.
- Wired `IonPulseDischarge` to `^Effect_Emp_Fx_RA2` and `ChronoshiftImpact` to
  `^Effect_Chrono_Fd_RA2`, preserving their secondary/glow/distortion warheads.
- Converted `NaxisBlackBomb`, `AsianOilBomb`, and `RA2FreedomAK47` to the
  appropriate `^Effect_*_RA2` inherits.
- Cleaned redundant local `Warhead@Effect` / `-ImpactSounds` blocks from
  `RA2MirageGun` and `RA2HeavyMirageGun`.
- Ensured `RA2PsychicJab` `Inherits@fx` is the last inherit.
- Simplified `DredMissile` and `YRBoomerSCUD` water-effect overrides (removed
  the `gexpwala` typo sound, kept `ImpactActors: false`).
- Fixed `LatinBuggyRocket` and `AsianSmallOilBomb` to a single winning
  `Inherits@fx`.
- Boot crash on `^Effect_Tesla_Impact_RA2` / `^Effect_Tesla_Heavy` circular
  inheritance was fixed by inlining the `^Effect_Tesla_Heavy` `EMPUnit` and
  `ShieldHit` warheads into `^Effect_Tesla_Impact_RA2`, `^Effect_Ion_Ring_RA2`,
  and `^Effect_Psi_Wave_RA2` instead of inheriting them.
- Verification: `find_empty_warhead.py = 0`, `audit_empty_warheads.py = 0`,
  `extract_stats.py` clean, `audit_balance_drift.py` clean,
  `audit_effect_warhead_names.py` 0 violations, `check_effect_audio.py` OK,
  `launch-game.cmd` reached the main menu
  (`MenuPostProcessEffect.PostWorldLoaded` in `perf.log`; no new
  `exception-*.log` after the successful run). One stale exception log from
  the pre-fix boot remains (`exception-2026-08-17T161444Z.log`).
- `python tools/audit/run_all.py` still exits 1 on pre-existing failures
  (`audit_inherits`, `audit_upgrades`, `audit_fluent`, `audit_basebuilder_crates`,
  `audit_buildable_order`, `audit_weapon_suffixes`, `audit_warhead_split`);
  these are unrelated to this effect wiring and pre-date the current sweep.
- Remaining: `SCTyr` in `StarCraft/Terran/yaml/weapons.yaml` still has a
  three-explosion `ra2_*` list with no matching single RA2 template; the
  legacy `mods/cameo/weapons/redalert2.yaml` is excluded from the loaded tree.

## 2026-08-17 — RA2 sprite-named effect template library (foundation + shared/Soviets wiring, boot-gated)

- Generated a complete `^Effect_<family>_<size>_RA2` template library for the
  54 `ra2_*` effect sequences in `mods/cameo/sequences/misc.yaml` and inserted
  it into `mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml`.
- Replaced the old `^Effect_MissileHE_Medium_RA2` with the new
  `^Effect_Explosion_Large_RA2`.
- Wired the shared RA2 weapon stacks to the new templates:
  `^RA2FlakWeapon`, `^RA2LightMissile`, `^RA2MediumMissile`,
  `^RA2HeavyMissile`, `^RA2TankDestroyerCannon`, `^RA2MediumCannon`,
  `^RA2HeavyCannon`, `^RA2Grenade`, `^RA2TeslaWeapon`, `^RA2RailgunWeapon`,
  `^RA2EliteEffects`, `RA2UnitExplode`, `RA2UnitExplodeBig`,
  `RA2BuildingExplode`, `KirovExplode`, `RA2LargeDebris`, `RA2Terrorist`.
- Wired `RA2RTruckRocket` in `mods/cameo/weapons/redalert2mod.yaml`.
- Began Soviets concrete cleanup: `RA2TURRETFLAKAA`, `SeaScorpion_AA`,
  `RA2FLAKAA`, `RA2FlakTrackAAGun`, `RA2KirovBomb`, `RA2KirovBomb_tesla`,
  `RA2120xmm`, `RA160mmE_fire_elite`, `RA160mmE_tesla_elite`,
  `RA2UnitExplodeSmall`.
- Verification: `find_empty_warhead.py = 0`, `extract_stats.py` clean,
  `audit_balance_drift.py` clean, `launch-game.cmd` reached the main menu
  (`MenuPostProcessEffect.PostWorldLoaded` in `perf.log`, no new
  `exception-*.log`).
- Remaining: wire Allies/Yuri/redalert2mod/Shared concrete weapons that still
  have inline `Explosions: ra2_*`; sweep RA2Atomic nuke-ball and Lightning
  Storm ion-ring effects; run `review_resolve_diff.py`; full audit suite has
  pre-existing failures unrelated to this change.

## 2026-08-17 — RA2 effect template sweep continuation (Shared/redalert2mod/Yuri, Floating Disk, boot-gated)

- `mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml`:
  - `RA2Atomic` now uses `Inherits@fx: ^Effect_Nuke_Ball_RA2`; removed local
    `Warhead@Effect`, kept radiation warhead.
  - `^Effect_Ion_Ring_RA2` updated to inherit `^Effect_Tesla_Heavy` and added
    `ImpactActors: false`; `LightningStormDamage` now `Inherits@fx:` from it,
    preserving both `SpawnSmokeParticle` warheads.
  - Added `Warhead@EffectAir` to `^Effect_Tesla_Impact_RA2` and wired
    `TeslaArmorDischargeDummy` to it, removing its local effect blocks.
  - Wired remaining concrete weapons to RA2 effect templates:
    `RA2HoverMissile_elite`, `RA2ThunderboltMissile_elite`,
    `RA2MultiHoverMissile_elite`, `RA2MultiThunderboltMissile_elite`,
    `RA2DroneSparks`, `MigMissiles_fire`, `MigMissiles_tesla`, `RA2SCUDELITE`,
    `RA2DepthCharge` (added `^Effect_Depth_Charge_RA2`).
  - Added `-ImpactSounds:` to `^Effect_Init_Fire_RA2`.
- `mods/cameo/weapons/redalert2mod.yaml`:
  - Wired `AsianHowitzerSplash`, `AsianFlameFragment`, `AsianFlamerTurret`,
    `SteelHoverMissile_elite`, `MeteorFlameFragment` to RA2 effect templates.
- `mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml`:
  - Wired `RA2PsychicJab` to `^Effect_Init_Fire_RA2`.
- Floating Disk muzzle:
  - Added `^RA2DiskMuzzle` in `ContentPacks/RedAlert2/Shared/yaml/sequences.yaml`
    with a `ra2_diskray` sequence.
  - `yuri_floatingdisk` now `Inherits: ^RA2DiskMuzzle` and overrides
    `ra2_diskray` with `Scale: 0.9`, `Offset: 0,35`, `Tick: 100`.
  - `Armament@SECOND` and `Armament@Steal` in
    `ContentPacks/RedAlert2/Yuri/yaml/aircraft.yaml` now use
    `MuzzleSequence: ra2_diskray`.
- Skipped weapons already inheriting wired RA2 stacks (e.g., `^RA2MediumMissile`,
  `^RA2Grenade`, `^RA2TankDestroyerCannon`) and edge cases left for maintainer
  review: `DredMissile`, `NaxTorpTube` (custom water sound + wired parent),
  `NaxiMeteor` (glow fields), `MigMissiles_rad` (sprite `ra2radbang` not
  matching the `ra2_*` underscore convention).
- Verification: `find_empty_warhead.py = 0`, `extract_stats.py` clean,
  `audit_balance_drift.py` clean, `launch-game.cmd` reached main menu
  (`MenuPostProcessEffect.PostWorldLoaded` in `perf.log`, no new
  `exception-*.log`). `python tools/audit/run_all.py` still reports the same
  pre-existing failures as the prior session.

## 2026-07-18 — BALANCE PIPELINE LIVE (all agents read this)

**NEW LAW: never hand-edit balance numbers in yaml.** The pipeline is
implemented and enforced (`docs/design/BALANCE_PIPELINE.md`, CLAUDE.md
"Balance changes" section, DESIGN §12):
extract_stats.py → docs/balance/*.json (raw-stat ledger, committed) →
build_workbook.py → cameo_balance_v2.xlsx (gitignored workbench) →
import_workbook.py → apply_balance.py --confirm (maintainer order) →
re-extract, audits, boot, commit yaml+ledger together.
`audit_balance_drift` in run_all fails RED whenever yaml and ledger
disagree — hand edits cannot land silently anymore.
Loop PROVEN: exact fixed point + live 1000→1050→1000 round trip.
Phase 5 (per-class anchors via fit_class.py + class_anchors.json)
awaits maintainer anchor picks; the fixed-point test also exposed and
fixed an order-dependent resolver-cache-poisoning bug in
tools/audit/miniyaml.py that affected ALL resolved-value audits.

## 2026-07-18 — Claude session (TKM port + Blackrobe batch)

- TKM CONTRIBUTOR PORT (`3bb6a34b3`): full-repo zip from a community
  contributor analyzed (base = cea431010 with pre-rename-id payload),
  translated through the applied rename_map_tkm, per-actor 3-way
  merged into the pack. Arsenal-tree redesign, GP-25 replaces M203,
  Berezka speed/cloak, engineer field kits, new weapons + warhead .cs
  (DLLs rebuilt). Deviations flagged in the commit (kept warfactory
  ProvidesPrerequisite — his removal would orphan every
  ~tkm_warfactory prereq).
- TKM MOVED into ContentPacks/RedAlert2Mod (`d981d65fe` renames +
  `915714fe8` manifest/mod.yaml — the renames rode the earlier commit
  via the staged index; completion committed immediately). Theme
  folder rename POSTPONED (Blackrobe) — candidates logged in ROADMAP.
- Monster tank Tesla/Thermonuclear rockets (`d981d65fe`): real weapon
  swaps (mammoth logic) replace the imperceptible +10% multipliers;
  duplicate ActorStatValues fixed earlier in `71765570b`.
- Survival (`e8af695eb`): superlinear ramp, wave-size floor (dip fix),
  veteran waves; win-objective fix earlier in `71765570b`. `survival 2`
  copy was deleted by the team (`32669f345`) — main copy carries all.
- SM passive income (Blackrobe): moondairyfarm verified correctly
  wired; the missing piece (ra2oilderrick/ra2ywall conyard provisions)
  is the MAINTAINER'S OTHER SESSION's uncommitted WIP — do not
  double-fix. Laser Beetle/M200B report: wiring verified WAD
  (replacement promotions retire them); if the REPLACEMENTS don't
  appear despite bought promotions, check rank1 granting in-game.
- NEXT: FULL SM REBALANCE (ROADMAP P1, sheet-first, workbook free).

## 2026-07-17 — Claude session SID-20260717-cl4b7e (RA1 legacy rename + two-session repair pass)

**Landed (commits `fdd466494`, `4cf7e6909` + this session's repair commit):**
- RA1 LEGACY-ID RENAME complete: all 52 old-style ids (RAE1, PT/DD/CA,
  SS/MSUB, POWR/APWR/RASILO, BADR family, naval yards, civilians, husks,
  8 upgrade proxies) → grammar-compliant ids; only `japan` unprefixed.
  Applied by tools/rename/apply_ra1_legacy.py (context-scoped successor
  to apply.py). zerofighter collision → japan_zerofighter_slave.
- Umlaut transliteration (schwarzermond_ubermensch), CABAL plasmaturret
  buildable + mobilestealthgenerator removed, stale RA1 monoliths deleted.
- REPAIR PASS after two-session collision (this entry's second half):
  1. 13 explicit `actor_<oldid>.description/.name` yaml refs broke when
     ftl keys renamed (whole-identifier pass can't see through the
     `actor_` prefix) — added a fluent-stem pass to the applicator
     (combined-alternation regexes; 52 sequential re.subs was too slow)
     and fixed all 13. audit_fluent: 17 → 0 unresolved.
  2. warcraft2_en.ftl + tkm_en.ftl were NEVER registered in mod.yaml
     FluentMessages — WC2/TKM faction descriptions showed raw keys.
     Registered both.
  3. 19 audit reports in docs/audit/latest/ were UTF-16-corrupted by a
     concurrent session's PowerShell `>` redirect (10 committed
     corrupted). Regenerated the whole suite via bash run_all.sh (UTF-8).
     Lesson saved to agent memory.
- Verification: full audit suite green (fluent 0 unresolved, consistency
  73/0, packs P2 = known D2k suffix-style backlog only), resolver spot
  checks green (3913 actors / 2365 weapons, zero old ids), FACTIONS.md
  clean of old ids, boot gate to main menu.
- SM promotion grid: implemented by the concurrent session in
  SchwarzerMond/yaml/promotions.yaml with CABAL-pattern gating BUT the
  chains deviate from the maintainer's image; row order under redesign —
  see ROADMAP P2 (sharpened 2026-07-17 with maintainer's MARS/tier
  clarifications + reshuffle proposal). DO NOT touch the grid before the
  maintainer picks an option.
- NOTE for all agents: SCUD/SCUDNUKE (RedAlert/Soviets weapons.yaml) are
  legacy-uppercase WEAPON ids shared with generals/darkreign — WPN-MIGRATE
  scope, intentionally untouched by the actor rename.
- SM PROMOTION GRID FINALIZED (maintainer decision): columns
  infantry | vehicles | air/artillery/support, tier-laddered rows —
  see ROADMAP P2 (RESOLVED) for the binding table. promotions.yaml
  re-chained, `..._promotion_bermensch` → `..._promotion_ubermensch`,
  ^PromotionUnitBuff stripped from 10 non-promotion SM units
  (FutureTech convention: grid units only). Boot green.
- NEW ORDER: FULL SM REBALANCE (sheet-first; post-buff-strip stats;
  38 stat_formulas findings as the seed) — queued as ROADMAP P1.

## 2026-07-16

**Task:** Diagnose ACP connection issue with Claude.
**Done:**
- Confirmed ACP refers to Agent Client Protocol; Claude integration is typically via `claude-agent-acp` / `claude-code-acp` or inside Devin Desktop/Windsurf/Zed/JetBrains.
- Checked Cameo-mod repo: no ACP/Claude config present.
- Checked local environment: `node`, `npm`, `devin`, `claude`, and `claude-agent-acp` are not on PATH for this shell; no Windsurf ACP registry (`~/.windsurf/acp/registry.json`) or Windsurf logs found.
**Diagnosis (after user logs):** Devin Desktop/Windsurf is trying to spawn `npx -y @agentclientprotocol/claude-agent-acp@0.59.0`, but `npx` is not found in the IDE's PATH (`spawn npx ENOENT`). The ACP client needs Node.js installed (>=20.19 for this package) and available to the IDE process.
**Fix applied:**
- Downloaded and extracted Node.js v24.18.0 LTS to `%LOCALAPPDATA%\Programs\nodejs\node-v24.18.0-win-x64`.
- Added the Node `bin` directory to the user `PATH`.
- Set PowerShell execution policy to `RemoteSigned` for the current user so `*.ps1` scripts (including `npx.ps1`) can run.
- Installed `@agentclientprotocol/claude-agent-acp@0.59.0` globally via `npm`.
- Verified `node -v`, `npx -v`, `claude-agent-acp --version`, and `npx -y @agentclientprotocol/claude-agent-acp@0.59.0 --version` all work.
**Next:** Restart Devin Desktop/Windsurf so the IDE process picks up the updated `PATH`, then enable the Claude agent again.

## 2026-08-04 — Balance ledger re-extract

- Refreshed 32 per-faction JSON ledgers from the current resolved ruleset (`python tools/balance/extract_stats.py`).
- Drift check: 0 drifted.
- Multiplier audit: 0 non-integer `Modifier` values (run with `PYTHONIOENCODING=utf-8`).
- Boot-gate: reached main menu (`PostWorldLoaded`), no new `exception-*.log` files.
- Committed updated ledgers + current uncommitted YAML rule sync (Yuri Slave Miner cost/build duration, `^SwarmlingGrinderTemplate` Valued default).

## 2026-08-04 — extract_stats design_weapon_class fix + HighV NRE

- `tools/balance/extract_stats.py`:
  - Removed all remaining `Versus: Shield` heuristics for `design_weapon_class`.
  - `design_weapon_class` is now derived only from `weapon_classes.yaml` sidecar + keyword fallback.
  - Any weapon mixing more than two warhead-class templates returns `design_weapon_class: null` and `weapon_class_source: illegal_mix` (or `allowlist_mix` for deliberate Dune combat-tank / siege exceptions).
  - Dummy weapons with no damage warheads are marked `extraction_note: no_damage_warheads` and `pricing: false` so they do not feed the balance formula.
  - Re-extracted all 32 `docs/balance/*.json` ledgers; `extract_stats.py --check` reports 0 drifted.
- `mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml`:
  - `HighV` `Warhead@Bullet_Medium_Percentage` was missing its warhead type, causing the weapon to be dropped from the ruleset and `td_gdi_guardtower` to fail at boot (`Weapons Ruleset does not contain an entry 'highv'`). Set it to `HealthPercentageDamage` to match `M16AP`.
- Boot-gate: reached main menu (`PostWorldLoaded`); no new `exception-*.log` files.

## 2026-08-04 — extract_stats refine class-template detection

- `tools/balance/extract_stats.py`:
  - Treat `^Projectile_*` and `^Effect_*` split-family templates as non-class
    components, leaving only `^Warhead_*` and legacy class templates as class
    inputs. This removes false `illegal_mix` hits from the new 3-way warhead
    split and lets `design_weapon_class` correctly reflect the weapon's real
    class family.
  - Re-extracted all 32 `docs/balance/*.json` ledgers; `extract_stats.py --check`
    reports 0 drifted.

## 2026-08-04 — extract_stats warhead renames and RA2 Thunderbolt family 3-way split

- `tools/balance/extract_stats.py`:
  - Renamed the weapon-template output from `weapon_types` to `warheads`; it now
    contains only resolved `^Warhead_*` templates (recursed through `^`-parents).
  - Renamed the damage-node output from `warheads` to `damage_warheads`.
  - Updated all balance-tool consumers (`build_workbook.py`, `_requantize_ledgers.py`,
    `_patch_ledgers_from_reports.py`, `fit_class.py`, `import_workbook.py`,
    `apply_balance.py`, `update_ranges.py`, `propose_class_rebalance.py`, `check_band.py`)
    to use the new ledger keys.
- `mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml`:
  - Converted `RA2ThunderboltMissile`, `RA2MultiHoverMissile`, and
    `RA2MultiThunderboltMissile` to the new 3-way split: first and last `Inherits`
    become the two `^Warhead_*` templates, the last also provides `^Projectile_*`
    and `^Effect_*`; middle `Inherits` and re-added `Warhead@` overrides removed.
- `mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml`:
  - Converted `RA2PatriotThunderboltMissile` to the new 3-way split.
- Re-extracted all 32 `docs/balance/*.json` ledgers; `extract_stats.py --check`
  reports 0 drifted.
- Boot-gate: reached main menu (`MenuPostProcessEffect.PostWorldLoaded`); no new
  `exception-*.log` files.
