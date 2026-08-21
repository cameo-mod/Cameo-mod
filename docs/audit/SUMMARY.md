# Baseline Audit — Summary

_One page. Details: [FINDINGS.md](FINDINGS.md) · raw tables: [baseline/](baseline/) ·
faction map: [../factions/MATRIX.md](../factions/MATRIX.md)._

Recurring code-health audits and their cadence are tracked in
[`PERIODIC.md`](PERIODIC.md) and [`periodic.json`](periodic.json).

## Counts by bug class

> **Note:** Counts below were generated from the baseline audit run. Significant work since then (weapon template splicing, renames, armor normalization, ContentPack migration) may have changed some counts. Re-run `tools/audit/run_all.sh` for current numbers.

| class | what | count (live tree) | severity profile |
|---|---|---|---|
| B8 | crash-class content | **0** distinct (was 3+ — fixed 2026-07-14: ts_nod_ticktank voxel, magicnuke sequence, ra2_cgtbnkbb/ctoutpbb missing assets; 2026-07-15: CABAL CreateEffect Image: fields removed, impact animations consolidated in misc.yaml, map actors renamed; 2026-07-24: RA2 weapons migrated to ContentPack, Yuri weapons headers restored, Naxis Kübelwagen encoding fixed, nuclearflash shader created) | crash |
| B1 | cross-faction leaks | 10 L1 + 13 L3 (+1,106 shared needing owners) | balance |
| B2 | illegal inherits | **328** concrete→concrete, 24 cross-faction, 0 dangling | balance-risk |
| B5 | AI wiring | **200** ids defined nowhere, 620 unloaded refs, 26 factions with unwired units | balance |
| B3 | upgrade direction | 12 anti-buff combos (2 suspicious, 1 verify, rest intended drawbacks), 4 dead upgrades, 5 dead-wiring families on 300–1,042 actors each | balance |
| B4 | upgrade coverage | 15 tracked upgrades, ~40 real uncovered combat slots (CABAL backup systems: avatar+widow done; `cabal_legion` does not exist — was renamed/removed) | balance |
| B6 | art/sequence refs | 11 missing images, 11 missing sequences, 542 orphan images | cosmetic→crash-risk |
| B7 | metadata rot | 24 duplicate-tooltip groups, 0 missing tooltips | cosmetic |
| B9 | numeric drift | bounds screen **clean** (TB23 fix held); 163 outlier leads | balance-minor |
| B10 | dead content | 345 orphan weapons, 542 orphan images, 16 dead conditions | hygiene |
| B11 | asset norms | 3,632 / 8,776 WAVs off-norm (mono/16-bit/22050 Hz); 131 PNGs over budget | hygiene |
| B12 | localization | 0 unresolved Fluent refs, 233 orphaned messages, ≤10% Fluent coverage | cosmetic |
| R2 | stacked multipliers | **757** units over the 2.0× budget; worst 36× (RA2 Allies) | balance |
| W | weapon uniqueness (DESIGN §10) | 36 same-faction + 42 cross-faction shared weapons; 95 carrier-only (IFV borrow, informational) | design/identity |
| G | garrison weapons (DESIGN §11) | **clean** (G1/G2/G3 = 0 after 2026-07-10 fixes; 30 design exceptions) | crash-free/balance |

## Top 20 findings

1. **`tatacitus` NukePower fires nonexistent `TSChemTacticalMissile`** — FIXED: changed to existing `TSTacticalChemMissile` with valid `tsnodmmsil` image (tiberiaalliances.yaml).
2. **RA2 Allies hero-infantry stack measures 36×** fresh-self power (Assault Squad + Vanguard + Infiltrators + Chromium/Prismatic lines) — worst in game; Yuri 30× behind it.
3. **ai.yaml: 200 references defined nowhere** — incl. `ra2naclon`, `nax2_chrono` (CABAL refs `tsgtcnstcabalb`/`tsntpulscabal` already removed).
4. **Stale "BuildingFractions Dune Universe" block** uses pre-ContentPacks names — entire section steers nothing.
5. **`raider.ordos` not in any AI build list** — FIXED: added to Dune Universe `UnitsToBuild` with weight 7 (also `runner.steel`, `orion.futu`, `yrrobo.futu`, 5 Naxis units still pending).
6. **`ra_doctrine_teslatech` doubles reload (Modifier 200) on 2 actors** — suspected Dark-Armament-class inversion; verify.
7. **`up_energizedarrows` has ReloadDelayMultiplier 125** on one actor — suspected inversion; verify.
8. **328 concrete→concrete inherits** — the Slave-Miner bug factory; Phase-1 queue, full grouped list in FINDINGS.
9. **13 L3 leaks**: CABAL/Forgotten/TS-Nod buildings inherit GDI/Nod concrete actors (tscabaltech→tsgttech etc.).
10. **Modern Fire Control Systems covers 15/33 of TS GDI** — all aircraft + half the infantry lack the roster-wide hook.
11. **WC2 tower upgrade names** (guard↔cannon swap) — FIXED; remaining 24 duplicate-tooltip groups still under review.
12. **Dead-wiring families on 1,042 actors each** (`usabombardament`, `usaholdtheline`, `usasearchndestroy`, `upsubliminal(2)`) + `upra2deso` on 302 — Generals-era hooks granted by nothing.
13. **3 player-visible raw Fluent keys** — STALE/RESOLVED: current `audit_fluent.py` F1 shows 0 unresolved refs.
14. **`wc2_orc_eye_of_kilrogg` TurnSpeed 2048** — FIXED: reduced to 28 (bounds screen still clean; high vision range remains by design as scout).
15. **345 orphan weapons + 542 orphan sequence images** — RAM/load-time dead weight.
16. **CABAL absent from Random AND Tournament pools, still titled "(WIP)"** — FIXED: CABAL added to both pools, WIP label removed.
17. **CABAL post-TB23 full stack = 3.9×** — over the 2.0 budget but sane; trim one Research multiplier or cap rank scaling.
18. **_old.yaml deprecated files removed** — tiberiansunold + warcraft2old rules/sequences/weapons deleted (20,919 lines).

## Recommended fix order (per MASTER_REPORT §4)

1. **B5 AI wiring** (items 3–5) — restore bot competence for pool factions; delete/fence the 620 unloaded refs.
2. **B3/B4 verify+fix** (items 6, 7, 10) and transcribe the remaining 526 `upgrades_intent.yaml` entries.
3. **B2+B1 structurally** via §12 Phase-1 per-faction migration (items 8–9), `dump_resolved.py`-verified; turn `audit_inherits` blocking in CI as factions land.
4. **B7/B9/B12 quick wins** (items 11, 13, 14) — ideal AI-agent batch work.
5. **B10/B11 hygiene** (items 15, 18) — orphan purge + per-directory WAV normalization; deprecated *_old.yaml files already removed.
6. **R2 rebalance** (items 2, 17) with tournament telemetry before touching Consortium-family numbers.

## Superweapon documentation audit (2026-07-25)

Full cross-reference of YAML superweapon/support power traits vs `FACTIONS.md` completed.
Raw data: [`latest/superweapon_audit.yaml`](latest/superweapon_audit.yaml).

**14 findings** (1 HIGH, 2 MEDIUM, 8 LOW, 3 INFO):
- **SW-001 (HIGH)**: Harkonnen Palace has `^PrimarySuperweapon` + `SupportPowerChargeBar` but **no power trait** — Death Hand Missile unimplemented (parked faction).
- **SW-002 (MED)**: Forgotten superweapon was "Tiberian Wildlife Rampage" in docs but YAML implements `NukePowerCA` (nuclear missile). FIXED in FACTIONS.md.
- **SW-003 (MED)**: CABAL listed "Data Worm, Satellite Hack" but YAML has `NukePowerCA` (CabalMagicNuke) + `FireArmamentPower` (Data Worm). Satellite Hack not found. FIXED.
- **SW-004–011 (LOW)**: Missing support powers in FACTIONS.md — Nod TS Cluster Missile, RA1 Allies Chrono Reinforcements, RA2 Allies Force Shield, Latin Syndicate EMP/Traitors, Schwarzer Mond name mismatch, Ordos name mismatch, WC2 Humans Slow/Invisibility, WC2 Orcs Bloodlust/Haste. ALL FIXED.
- **SW-012–014 (INFO)**: Protoss reuses SteelIonCannon weapon, Consortium missing Federation Support Teleport in ref table, TS GDI missing Drop Pods in ref table. ALL FIXED.

**Outpost 2 verified**: Supernova Missile IS implemented in `rules/outpost2.yaml` (`NukePower`, charge 9000). FACTIONS.md was correct.

**WIP faction superweapons discovered** (not in FACTIONS.md): Warzone 2100 (IonCannonPower + AirstrikePower + NukePower), Worms (Sheep Strike + Concrete Donkey), Win98 (Demo Disk Strike + Red Ring of Death), Warcraft 1 (Rain of Fire + Poison Cloud), WH40K (8 Deep Strike variants + Marauder Bomber + Inquisition).

## War Economy SpeedMultiplier bug (2026-08-03)

**FIXED**: `SpeedMultiplier@ra1_soviets_upgrade_wareconomy` in `ContentPacks/RedAlert/Shared/yaml/upgrades.yaml:134` used `Prerequisites` instead of `RequiresCondition`. Since `SpeedMultiplierInfo` has no `Prerequisites` field, the engine ignored it, making the trait **always active** at 110% — even without the War Economy upgrade researched.

**Impact**: Every unit inheriting `^WarEconomyTeamUpgradeRA1` (all harvesters via `^HarvesterTemplate`, all refineries via `^Refinery`) got a permanent +10% speed boost. For the Noid Harvester (base speed 50), this produced a displayed speed of 55 instead of 50. When combined with the Gap Generator shroud effect (80%), the result was 50 × 80% × 110% = 44.

**Fix**: Changed `Prerequisites` → `RequiresCondition` on line 135. The trait is now properly conditional on the `ra1_soviets_upgrade_wareconomy` condition granted by `GrantConditionOnPrerequisite`.

**Audit**: Swept all YAML files for the same pattern (`SpeedMultiplier` with `Prerequisites` but no `RequiresCondition`) — no other SpeedMultiplier instances found. **Superseded**: the bug class later proved to cover ALL conditional multipliers — see next section.

## Empty warhead type NRE (2026-08-04)

**FIXED**: Boot crashed with `NullReferenceException` in `WeaponInfo.LoadWarheads` (`ObjectCreator.CreateBasic` on the abstract `Warhead` base class) because two weapons had `Warhead@` nodes with **no type value**:

- `RA2MirageGun` — `Warhead@Effect:` → set to `CreateEffect` (`mods/cameo/weapons/redalert2.yaml`)
- `TSSAPCMissiles` — `Warhead@GrenadeFriendlyFire:` → set to `SpreadDamage` (`mods/cameo/weapons/tiberiansun.yaml`)
- `HighV` — `Warhead@Bullet_Medium_Percentage:` → set to `HealthPercentageDamage` (`mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml`)

**Mechanism**: An empty `Key:` line parses to a null value. The merge's null-fallback (`overrideNodes.Value ?? existingNodes.Value`) only rescues the node when a same-key ancestor has a value; these two nodes had none. The engine constructs `WeaponInfo` for **every** top-level weapon node — including unused `^templates` — so a typeless warhead anywhere in the resolved ruleset is a boot crash, and `LoadWarheads` then calls `Game.CreateObject<IWarhead>(null + "Warhead")`, which resolves to the abstract `Warhead` class and NREs.

**Audit**: New `tools/audit/audit_empty_warheads.py` resolves the full manifest weapon set via the shared `miniyaml.Ruleset` and flags any resolved node whose key starts with `Warhead` but has no type (plus empty `Projectile:` as a suspect). 4,202 weapons checked, 0 remaining findings. **`utility --check-yaml` does NOT catch this class** — run the audit after bulk warhead/weapon edits. Boot-gate passed after the fix.

## Conditional-multiplier `Prerequisites:` sweep (2026-08-04, follow-up to War Economy bug)

**FIXED**: The War Economy bug class was wider than `SpeedMultiplier`. Every `ConditionalTrait`-based multiplier (`FirepowerMultiplier`, `DamageMultiplier`, `SpeedMultiplier`, `RangeMultiplier`, `ReloadDelayMultiplier`, `InaccuracyMultiplier`, `RevealsShroudMultiplier`, `DetectCloakedMultiplier`, ...) has **no `Prerequisites` field** — a `Prerequisites:` line inside such a block is silently ignored by the loader, making the multiplier **always active**. (`ProductionCostMultiplier` / `ProductionTimeMultiplier` legitimately support `Prerequisites` and are unaffected.)

Three more always-active `FirepowerMultiplier` instances were found and fixed (`Prerequisites:` → `RequiresCondition:`):

- `FirepowerMultiplier@ra1_soviets_doctrine_conscription` (Modifier 110) — `mods/cameo/ContentPacks/RedAlert/Soviets/yaml/templates.yaml`
- `FirepowerMultiplier@global_conscription_buff` (Modifier 110) — `mods/cameo/rules/defaults.yaml`
- `FirepowerMultiplier@selectcolin` (Modifier 80) — `mods/cameo/rules/advancewars.yaml`

**Impact before fix**: Conscription gave +10% firepower permanently (not just while the doctrine was active); the same permanent-always-on behaviour applied to `global_conscription_buff` and Advance Wars CO Colin's firepower debuff.

**Sweep method**: scanned every `*.yaml` under `mods/cameo` for a `Prerequisites:` line whose parent block is any `*Multiplier@` trait other than `Production*` — 0 remaining instances after the fixes. The UK-economy / France-siege / RA2 commando-doctrine templates from the earlier audit notes are not present on this branch; nothing else was actionable here.

## W24 cluster 8 — 227mm / GDIRigMissilePod / MammothTusk (2026-08-20)

**Converted** three legacy mixed-stack missile weapons to the 3-way split (one warhead, one projectile, one effect):

- `227mm` (`mods/cameo/weapons/tiberiandawn.yaml`) → `^Warhead_MissileHE_Medium` + `^Projectile_Missile_Medium` + `^Effect_MissileHE_Medium`.
- `GDIRigMissilePod` (`mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml`) → `^Warhead_MissileHE_Heavy` + `^Projectile_Missile_Medium` + `^Effect_MissileHE_Medium`.
- `MammothTusk` (`mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml`) → `^Warhead_MissileHE_Heavy` + `^Projectile_Missile_Heavy` + `^Effect_MissileHE_Heavy`.

**Preserved:** resolved per-shot damage totals (8000/32000/24000), percentage twins, `Range`, `ReloadDelay`, `Burst`, `ValidTargets`, local `Projectile` overrides (`Speed`, `Inaccuracy`, launch angles), impact sounds, target filters, and `ImpactActors`. Added local `Warhead@EffectWater` because the new `^Effect_MissileHE_*` templates do not provide one.

**Resolver diff:** `tools/audit/review_resolve_diff.py` clean. The flak bullet contrail colors (`ContrailStartColor: FF884400`, `ContrailEndColor: 000000FF`) were restored to the three non-AMT parent projectiles as a resolved-behaviour preserve.

**Audits:** `find_empty_warhead.py` 0; `audit_warhead_split` broadcast count 958, baseline lowered 965→958; `audit_physical_state_warheads` PASS; `audit_balance_drift` clean; `audit_duplicate_inherits` no new findings for the cluster; `utility --check-yaml` pre-existing errors/warnings unchanged.

**Boot-gate:** `launch-game.cmd` reached `MenuPostProcessEffect.PostWorldLoaded`; no new `exception-*.log` after the run.

## W24 cluster 9 — D2K rocket family (GoliathRockets_AA, WraithRockets_AA, SunDogRockets, MissileTurret, ScoutRockets_AA, HeavyOrdosCombatTankRockets) (2026-08-22)

**Converted** six mixed-stack D2K-rocket weapons; later corrected so four anti-air weapons use `^Warhead_MissileAA_Heavy` and `SunDogRockets` uses `^Warhead_MissileAP_Heavy` (all on `^Projectile_Missile_Heavy_D2K` with D2K impact effects):

- `GoliathRockets_AA` (`mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml`) → 5×6000 + 5×3 = `Damage: 30000` / `Damage: 15`.
- `WraithRockets_AA` (`mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml`) → 5×2000 + 5×1 = `Damage: 10000` / `Damage: 5`.
- `SunDogRockets` (`mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml`) → 5×2000 + 5×1 = `Damage: 10000` / `Damage: 5`.
- `MissileTurret` (`mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml`) → 5×4000 + 5×2 = `Damage: 20000` / `Damage: 10`.
- `ScoutRockets_AA` (`mods/cameo/ContentPacks/StarCraft/Protoss/yaml/weapons.yaml`) → 5×2000 + 5×1 = `Damage: 10000` / `Damage: 5`.
- `HeavyOrdosCombatTankRockets` (`mods/cameo/ContentPacks/D2k/Ordos/yaml/weapons.yaml`) → 5×2000 + 5×1 = `Damage: 10000` / `Damage: 5`.

**Removed:** `^Chaingun`, `^FlakWeapon`, `^LightMissile`, `^MediumMissile` inherits and their `Warhead@Chaingun`, `Warhead@FlakWeapon`, `Warhead@LightMissile`, `Warhead@MediumMissile` main/percentage blocks.

**Preserved:** `Range`, `ReloadDelay`, `Burst`, `BurstDelays`, `ValidTargets`, `Report`, local `Projectile` overrides (`Speed`, `Inaccuracy`, launch angles, Wraith/HeavyOrdos `ContrailStartColor`/`ContrailEndColor`), and resolved water-splash behaviour by adding a local `Warhead@EffectWater: CreateEffect` (`Explosions: small_splash`, `ValidTargets: Water, Underwater`, `InvalidTargets: Ship, Structure, Bridge`) because `^D2KRocket` (via `^Effect_MissileAP_Heavy`) does not define one. Also restored the flak-bullet contrail visual fields (`ContrailZOffset`, `ContrailStartColor`, `ContrailEndColor`, `ContrailStartWidth`, `ContrailEndWidth`) as local `Projectile` overrides because `^Projectile_Missile_Heavy` drops them.

**Key finding:** `^D2KRocket` weapons that previously resolved through `^FlakWeapon` need explicit local `ContrailStartColor`/`ContrailEndColor`/`ContrailZOffset`/`ContrailStartWidth`/`ContrailEndWidth` overrides to preserve the resolved old projectile appearance, and a local `Warhead@EffectWater` to keep the water splash.

**Resolver diff:** `tools/audit/review_resolve_diff.py` OK (behavioural invariants preserved) for all six, with no CreateEffect changes; the only resolved delta is the damage-warhead multiset collapsing to one warhead whose sum equals the old total.

**Audits:** `find_empty_warhead.py` 0; `find_orphan_old_keys.py` 0 real bugs; `audit_warhead_split` broadcast count 952, baseline lowered 958→952; `audit_physical_state_warheads` PASS; `audit_balance_drift` clean; `sweep_areadamage.py` dry-run no changes in cluster; `extract_stats` regenerated ledgers; `run_all.py` has pre-existing unrelated failures (`audit_inherits`, `audit_upgrades`, `audit_sequences`, `audit_fluent`, `audit_basebuilder_crates`, `audit_buildable_order`, `audit_weapon_suffixes`) unchanged.

**Correction (2026-08-22+):** `GoliathRockets_AA`, `WraithRockets_AA`, `ScoutRockets_AA`, and `MissileTurret` are `^Warhead_MissileAA_Heavy`; `SunDogRockets` is `^Warhead_MissileAP_Heavy`; the earlier `^D2KRocket` / `^Warhead_MissileAP_Heavy` classification in this summary and in `docs/design/BALANCE_PROGRAM_PLAN.md` was stale.

**Boot-gate:** `launch-game.cmd` reached `MenuPostProcessEffect.PostWorldLoaded`; no new `exception-*.log`.

## W24 cluster 10 — HammerheadArtillery (2026-08-24)

**Converted** `HammerheadArtillery` in `mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml` from the old `^RA2Grenade` + `^HeavyBomb` + `^SteelMediumCannon` 3-main stack to a 2-warhead 3-way split:
- `Inherits@wh: ^Warhead_Demolition_Heavy` (merged `Demolition_Light` 11111 + `HeavyBomb` 11111 into `Damage: 22222`; `Demolition_Heavy_Percentage` `Damage: 22`).
- `Inherits@wh2: ^Warhead_CannonHE_Medium` (`Damage: 11111`; `CannonHE_Medium_Percentage` `Damage: 11`).
- `Inherits@proj: ^Projectile_Shell_Medium` with local `Bullet` overrides (`Image: 120MM`, `Speed: 333`, `LaunchAngle: 111`, `Inaccuracy: 1111`, `Blockable: false`, blue contrail).
- `Inherits@fx: ^Effect_Demolition_Heavy` with local effect/smudge/glow/shield/concrete overrides.

**Preserved** `Range: 11111`, `MinRange: 2220`, `ReloadDelay: 111`, `Report: vdesatta.wav, vdesattb.wav`, `120MM` projectile, all smudge/RA2Scorch/crater/dune behaviour, `Concrete: 150`, shell-style shield-hit sound, `steel_blueexp` + `blue_building_napalm` visual sequence, and the per-shot damage total (33333 flat + 33 percentage).

**Resolver diff:** `tools/audit/review_resolve_diff.py` OK (only the expected damage-multiset collapse); all projectile/effect invariants preserved.

**Audits:** `find_empty_warhead.py` 0; `find_orphan_old_keys.py` 0 real bugs; `audit_warhead_split` broadcast count 946, baseline lowered 950→946; `audit_physical_state_warheads` PASS; `audit_balance_drift` clean; `extract_stats` regenerated ledgers; `verify_generator_sync` drift 0.

**Boot-gate:** `launch-game.cmd` reached `MenuPostProcessEffect.PostWorldLoaded`; no new `exception-*.log`.

## W24 cluster 11 — NuclearMaverick (2026-08-21)

**Converted** `NuclearMaverick` in `mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml` from the old full-stack `^NuclearWarhead` to a 3-way split finish conversion:
- `Inherits@wh: ^Warhead_MissileHE_Heavy`
- `Inherits@wh2: ^Warhead_Nuclear_Super` (main `AreaDamage` 10-tick shockwave, `Damage: 2000`, `MaxRadius: 9000`; percentage `AreaDamagePercentage`, `Damage: 1`, `Spread: 500`, `MaxRadius: 4500`)
- `Inherits@proj: ^Projectile_Missile_Heavy`
- `Inherits@fx: ^Effect_Nuclear_Super` then `Inherits@fx2: ^Effect_MissileHE_Heavy` so the missile effect layer wins for `Concrete`, `ShieldHit`, `EffectAir`, etc., while the nuclear smudges and `ShieldHitEffectNuclear` remain.

**Preserved** per-shot totals (40000 flat + 20 percentage) and the old `SpreadDamage`/`HealthPercentageDamage` shape (falloff 100→10, `AffectsParent: false`, `ValidRelationships: Enemy`, `FireDeath, Incendiary` damage types) while adopting the canonical `^Warhead_Nuclear_Super` Versus profile. Local `Effect: nuke_small`, `Projectile: Missile`, `Burst`, `Range`, `Report`, and contrail all unchanged.

**Audits:** `find_empty_warhead.py` 0; `find_orphan_old_keys.py` 0 real bugs; `audit_warhead_split` broadcast count 945, baseline lowered 946→945; `audit_balance_drift` clean; `extract_stats` regenerated ledgers; `verify_generator_sync` drift 0; `audit_doc_claims` 16/16 clean.

**Boot-gate:** `launch-game.cmd` reached `MenuPostProcessEffect.PostWorldLoaded`; no new `exception-*.log`.

## W24 cluster 12 — ThermobaricNuclearMaverick (2026-08-21)

**Converted** `ThermobaricNuclearMaverick` in `mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml` from a broken duplicate `Inherits@2` stack (`^NuclearWarhead` and `^Warhead_Flame_Heavy` sharing the same inherit key) to a clean 3-way split:
- `Inherits@wh: ^Warhead_MissileHE_Heavy`
- `Inherits@wh2: ^Warhead_Nuclear_Super` (main `AreaDamage` 10-tick shockwave, `Damage: 1400`, `MaxRadius: 9000`; percentage `AreaDamagePercentage`, `Damage: 1`, `Ticks: 7`, `Spread: 500`, `MaxRadius: 4500`)
- `Inherits@wh3: ^Warhead_Flame_Heavy`
- `Inherits@proj: ^Projectile_Missile_Heavy`
- `Inherits@fx: ^Effect_Flame_Heavy` then `Inherits@fx2: ^Effect_Nuclear_Super` so the nuclear effect layer wins for `Concrete`, `ShieldHit`, `Smudge1/2/3`, and `ShieldHitEffectNuclear`, while the flame smudge, glow, napalm `Effect2`, and `GroundFire` remain.

**Preserved** total per-shot damage (`42000` flat + `21%` percentage) and the old `SpreadDamage`/`HealthPercentageDamage` shape (`AffectsParent: false`, `ValidRelationships: Enemy`, `FireDeath, Incendiary` damage types) while fixing the duplicate-inherit bug. `ContrailEndColor`, `ContrailLength`, `Effect: nuke_small`, `Effect2: large_napalm`, `GlowScale`, `Delay`, and missile projectile all unchanged.

**Audits:** `find_empty_warhead.py` 0; `find_orphan_old_keys.py` 0 real bugs; `audit_warhead_split` broadcast count 944, baseline lowered 945→944; `audit_balance_drift` clean; `extract_stats` regenerated ledgers; `verify_generator_sync` drift 0; `audit_doc_claims` 16/16 clean.

**Boot-gate:** `launch-game.cmd` reached `MenuPostProcessEffect.PostWorldLoaded`; no new `exception-*.log`.

## W24 cluster 15 — JapanesePlasmaBomb (2026-08-21)

**Converted** `JapanesePlasmaBomb` in `mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml` from `Inherits@3: ^HeavyBomb` to the 3-way split `Inherits@wh3: ^Warhead_Demolition_Heavy` + `Inherits@fx2: ^Effect_Demolition_Heavy`, keeping the existing chemical and flame split. Preserved per-shot totals (`10000` flat + `5%` percentage) and the old falloff shape by setting `MaxRadius: 3200`/`1600` on `^Warhead_Demolition_Heavy`'s 6-step falloff, so the resolved 5-step `100, 50, 25, 10, 5` shape is unchanged. Preserved local damage types, projectile (`hakureiring`, `Speed: 250`, `blue_smokey` trail), burst, report, and effects (`blueartexp`, `blue_building_napalm`, `poof` restored via a local `Warhead@Effect1` override, water splash, smudges, shield, ground fire, concrete `250`).

**Audits:** `find_empty_warhead.py` 0; `find_orphan_old_keys.py` 0 real bugs; `audit_warhead_split` count 941; `audit_balance_drift` clean; `extract_stats` regenerated; `review_resolve_diff` OK (behavioural invariants preserved).

**Boot-gate:** `launch-game.cmd` reached `MenuPostProcessEffect.PostWorldLoaded`; no new `exception-*.log`.

## W24 cluster 14 — TorpTubeThermobaric (2026-08-21)

**Converted** `TorpTubeThermobaric` in `mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml` from legacy full-stack templates to the 3-way split:
- `Inherits@wh: ^Warhead_Nuclear_Super`
- `Inherits@wh2: ^Warhead_MissileAP_Heavy`
- `Inherits@proj: ^Projectile_Missile_Heavy`
- `Inherits@fx: ^Effect_Nuclear_Super`
- `Inherits@fx2: ^Effect_MissileAP_Heavy`

**Preserved** the torpedo projectile (`Image: v2`, `TrailImage: bubbles`, `Speed: 150`, water-bound, cloak palette) and `Report: torpedo1.aud`. The bespoke torpedo fields are unchanged because `-Projectile:` removes the inherited projectile and the local node redefines it; the family is declared for bookkeeping. The nuclear half keeps totals `16000` flat and `8%` as `AreaDamage` `1600` × 10 ticks (`MaxRadius: 9000`, `Spread: 1000`) and `AreaDamagePercentage` `1` × 8 ticks (`MaxRadius: 4500`, `Spread: 500`). The missile half keeps totals `16000` flat and `8%` as `AreaDamage` `16000` (`MaxRadius: 4000`, `Spread: 800`) and `AreaDamagePercentage` `8` (`MaxRadius: 2000`, `Spread: 400`). Preserved `AffectsParent: true`, `ValidRelationships: Enemy` on both damage warheads, `FireDeath, Incendiary` for the nuclear warhead, and set `TargetActorCenter: false` to match the old resolved node. Added `-Warhead@Glow:` so neither `^Effect_Nuclear_Super` nor `^Effect_MissileAP_Heavy` introduces a new glow. Effect order keeps `^Effect_Nuclear_Super` first so `^Effect_MissileAP_Heavy` wins for `ShieldHit`, `Concrete` (`200`), `DuneRock`, `DuneSand`, `RA2Crater`, and the non-nuclear `Effect` (`big_frag`) before the local override to `nuke_small`/`kaboom22.aud`/`ImpactActors: true`. A local `Warhead@ShieldHit` override keeps `Duration: 10`.

**Audits:** `find_empty_warhead.py` 0; `find_orphan_old_keys.py` 0 real bugs; `audit_warhead_split` broadcast count 941; `audit_balance_drift` clean; `extract_stats` regenerated ledgers; `review_resolve_diff` reports `OK (behavioural invariants preserved)`.

**Boot-gate:** `launch-game.cmd` reached `MenuPostProcessEffect.PostWorldLoaded`; no new `exception-*.log`.

## W24 cluster 13 — MonsterTank120mm (2026-08-21)

**Converted** `MonsterTank120mm` in `mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml` from `Inherits: ^NuclearWarhead` to a clean 3-way split:
- `Inherits@wh: ^Warhead_Nuclear_Super`
- `Inherits@wh2: ^Warhead_CannonHE_Heavy`
- `Inherits@proj: ^Projectile_Shell_Heavy`
- `Inherits@fx: ^Effect_CannonHE_Heavy` then `Inherits@fx2: ^Effect_Nuclear_Super`

**Preserved** total per-shot damage: `CannonHE_Heavy` `40000` flat / `20%`; `Nuclear_Super` main `4000` × 10-tick shockwave (`MaxRadius: 9000`) and percentage `2` × 10-tick (`Spread: 500`, `MaxRadius: 4500`) for the old `20%`. Also preserved `AffectsParent: true`, `ValidRelationships: Enemy`, and `FireDeath, Incendiary` on the nuclear half; `Report: nukemisl.aud`; bullet projectile; and the local `Effect` (`nuke_small`, `kaboom22.aud`, `ImpactActors: true`).

`MonsterTank120mmThermobaric` inherits the same nuclear/cannon split and adds `^Warhead_Flame_Heavy` / `^Projectile_Flame_Heavy` / `^Effect_Flame_Heavy`; resolved totals stay `120000` flat + `60%`.

**Audits:** `find_empty_warhead.py` 0; `find_orphan_old_keys.py` 0 real bugs; `audit_warhead_split` broadcast count 942, baseline lowered 944→942; `audit_balance_drift` clean; `extract_stats` regenerated ledgers; `verify_generator_sync` drift 0; `audit_doc_claims` 16/16 clean.

**Boot-gate:** `launch-game.cmd` reached `MenuPostProcessEffect.PostWorldLoaded`; no new `exception-*.log`.

## Upgrade regression audit + blast-shape reporting (2026-08-22)

**Motivation:** Maintainer review of W24 A2 flagged that `MonsterTank120mm -> MonsterTank120mmThermobaric` felt like a downgrade even though every damage check passed. A proper `AreaDamageWarhead.cs:282` reading confirmed `Damage` is **split across ticks** (`perTickModifier = Ticks > 1 ? 100 / Ticks : 100`) and `review_batch_diff` only checked damage totals, not the warhead shape.

**Added:**
- `tools/audit/audit_upgrade_regression.py` — 314 gated armament pairs found across the ruleset.
  - **12 STRICTLY WEAKER** (e.g. `RA2PatriotThunderboltMissile` vs `RA2Patriot` 0.13×, `TSHellfireSonic` vs `TSHellfire` 0.11× vs Superheavy).
  - **42 ROLE-SHIFTED** — legitimate for specialists, regressions when losses land on the unit's primary armor classes.
  - **5 THIN MARGIN** — never loses, but is only +4–10% where it matters while +100%+ elsewhere (`MonsterTank120mmThermobaric` best 2.26×, worst core gain 1.04× vs Scout).
- `tools/audit/review_batch_diff.py` — added **blast shape** (`Spread`/`Falloff`/`Ticks`/`MaxRadius`) as a reported (non-failing) diff so future retrofits cannot silently flatten an expanding shockwave while preserving damage.

**Measured A2 innocence:** 54 findings before A2, 54 after. A2 did not create new regressions; it deepened a pre-existing `Su57` case from 0.92× to 0.87×.

**Status:** reporting only (`--baseline N` to enable the ratchet once the tree settles).

## Inline effect warheads (2026-08-22)

**Maintainer ruling:** effect warheads (`Warhead@Effect*`) should be **inherited** from `^Effect_*` templates, not declared inline on concrete weapons. **Superweapons** are the only accepted exception (unique multi-animated sequences).

**First scan:** 665 concrete weapons carry 815 inline effect warheads (`CreateEffect` / `EffectAir` / `EffectWater` / `ShieldHit` / etc.) instead of being supplied by an `Inherits@fx` template.

**Status:** guard `tools/audit/audit_inline_effects.py` pending; this is a structural-debt work item.
