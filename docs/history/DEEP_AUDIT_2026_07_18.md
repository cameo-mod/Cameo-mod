# Deep Audit Findings Report — 2026-07-18

Comprehensive exploratory audit of the Cameo mod project. All findings are
identified-only; no fixes have been applied.

---

## CRASH-CLASS BUGS (B8)

### C1. `tsvissml` SpawnActorOnDeath references commented-out actor
**Severity: CRASH**
**Files:** `mods/cameo/rules/tiberiansun.yaml:539-558, 573-577`

The `tsvissml` actor definition is commented out (lines 539-558), but
`tsvislrg` (Big Visceroid) still has:
```yaml
SpawnActorOnDeath@Division1:
    Actor: tsvissml
    OwnerType: Victim
SpawnActorOnDeath@Division2:
    Actor: tsvissml
    OwnerType: Victim
```
When a Big Visceroid dies, the game tries to spawn `tsvissml` which does not
exist as an actor. This will crash the game.

Also affects `tsmonstermaker1` at line 232 which spawns `tsvissml` with
`Probability: 6`.

---

## PALETTE MISMATCH BUGS (B6/B7)

### P1. `ra2player` vs `playerra2` death palette mismatch across TS infantry
**Severity: HIGH (visual corruption on death)**
**Root cause:** `^TSInfantry` template correctly sets `DeathSequencePalette: playerra2`,
but many TS infantry actors override it back to `ra2player` (the old palette name).

Both `ra2player` and `playerra2` are valid but **different** palettes:
- `ra2player` = `PlayerColorPalette@ra2player` (base `ra2`, remap indices 16-31)
- `playerra2` = `PlayerColorPalette@ra2` (base `ra2unit`, baseName `playerra2`)

TS actors use `PlayerPalette: playerra2` for rendering, so death animations
must also use `playerra2`. Using `ra2player` causes death animations to render
with the wrong player color mapping.

**Affected actors (loaded files):**

| File | Actor | PlayerPalette | DeathSequencePalette (wrong) |
|---|---|---|---|
| `rules/tiberiansun.yaml` | TSE1 | playerra2 | ra2player |
| `rules/tiberiansun.yaml` | TSENGINEER | playerra2 | ra2player |
| `rules/tiberiansun.yaml` | TSE3 | playerra2 | ra2player |
| `ContentPacks/TiberianSun/GDI/yaml/infantry.yaml` | ts_gdi_medic | playerra2 | ra2player |
| `ContentPacks/TiberianSun/GDI/yaml/infantry.yaml` | ts_gdi_discthrower | playerra2 | ra2player |
| `ContentPacks/TiberianSun/GDI/yaml/infantry.yaml` | ts_gdi_falconenforcer | playerra2 | ra2player |
| `ContentPacks/TiberianSun/GDI/yaml/infantry.yaml` | ts_gdi_riottrooper | playerra2 | ra2player |
| `ContentPacks/TiberianSun/GDI/yaml/infantry.yaml` | ts_gdi_jumpjetinfantry | playerra2 | ra2player |
| `ContentPacks/TiberianSun/GDI/yaml/infantry.yaml` | ts_gdi_railguncommando | playerra2 | ra2player |
| `ContentPacks/TiberianSun/Nod/yaml/infantry.yaml` | ts_nod_chameleonspy | playerra2 | ra2player |
| `ContentPacks/TiberianSun/Nod/yaml/infantry.yaml` | ts_nod_elitecadre | playerra2 | ra2player |
| `ContentPacks/TiberianSun/Forgotten/yaml/infantry.yaml` | forgotten_mutanthijacker | playerra2 | ra2player |
| `ContentPacks/TiberianSun/Forgotten/yaml/infantry.yaml` | forgotten_mutantsniper | playerra2 | ra2player |
| `ContentPacks/TiberianSun/Forgotten/yaml/infantry.yaml` | forgotten_ghoststalker | playerra2 | ra2player |
| `ContentPacks/TiberianSun/Forgotten/yaml/infantry.yaml` | forgotten_mutant | playerra2 | ra2player |
| `ContentPacks/TiberianSun/Forgotten/yaml/infantry.yaml` | forgotten_mutantsoldier | playerra2 | ra2player |
| `ContentPacks/TiberianSun/Forgotten/yaml/infantry.yaml` | forgotten_mutantsergeant | playerra2 | ra2player |
| `ContentPacks/TiberianSun/Forgotten/yaml/infantry.yaml` | forgotten_zombiemutant | playerra2 | ra2player |
| `ContentPacks/TiberianSun/Forgotten/yaml/infantry.yaml` | forgotten_runnershotgal | playerra2 | ra2player |
| `ContentPacks/TiberianSun/Forgotten/yaml/infantry.yaml` | forgotten_chemsprayinfantry | playerra2 | ra2player |
| `ContentPacks/TiberianSun/Forgotten/yaml/infantry.yaml` | forgotten_mutantmortarman | playerra2 | ra2player |
| `ContentPacks/TiberianSun/Forgotten/yaml/infantry.yaml` | forgotten_visceroid | playerra2 | ra2player |
| `ContentPacks/TiberianSun/Forgotten/yaml/infantry.yaml` | forgotten_tiberianfiend | playerra2 | ra2player |
| `ContentPacks/TiberianSun/Forgotten/yaml/infantry.yaml` | forgotten_viniferafiend | playerra2 | ra2player |
| `ContentPacks/RedAlert2Mod/TKM/yaml/infantry.yaml` | tkm_marine | playerra2 | ra2player |
| `ContentPacks/RedAlert2Mod/TKM/yaml/infantry.yaml` | tkm_rifleman | playerra2 | ra2player |
| `ContentPacks/RedAlert2Mod/TKM/yaml/infantry.yaml` | tkm_thermonaut | playerra2 | ra2player |
| `ContentPacks/RedAlert2Mod/TKM/yaml/infantry.yaml` | tkm_thermonaut (2nd) | playerra2 | ra2player |
| `ContentPacks/RedAlert2Mod/TKM/yaml/infantry.yaml` | tkm_trooper | playerra2 | ra2player |

### P2. `^BaseBuilding` template default uses `ra2player`
**Severity: HIGH (affects all buildings that don't override)**
**File:** `mods/cameo/rules/defaults.yaml:3720`

```yaml
^BaseBuilding:
    WithDeathAnimation:
        DeathSequence: dead
        DeathSequencePalette: ra2player
```

All buildings inheriting `^BaseBuilding` get `ra2player` as the default death
palette. TS buildings that use `PlayerPalette: playerra2` must override this.
Some do (e.g. `cabal_radar`), but many TS/CABAL/Forgotten buildings with BIB
death animations still use `ra2player`:

| File | Actor | Issue |
|---|---|---|
| `ContentPacks/TiberianSun/GDI/yaml/buildings.yaml` | ts_gdi_servicedepot | BIB DeathSequencePalette: ra2player |
| `ContentPacks/TiberianSun/CABAL/yaml/buildings.yaml` | cabal_techcenter | BIB DeathSequencePalette: ra2player |

### P3. `ts_nod_chameleonspy` duplicate RenderSprites with conflicting palettes
**Severity: MEDIUM (dead code + palette confusion)**
**File:** `ContentPacks/TiberianSun/Nod/yaml/infantry.yaml:144-150`

```yaml
RenderSprites:
    PlayerPalette: ra2player      # dead — overridden by next block
WithDeathAnimation:
    DeathSequencePalette: ra2player  # wrong — should be playerra2
RenderSprites:
    PlayerPalette: playerra2      # this one wins
```

The first `RenderSprites` block is dead code. The `WithDeathAnimation`
between them uses `ra2player` which doesn't match the effective
`PlayerPalette: playerra2`.

### P4. `ts_nod_elitecadre` duplicate RenderSprites with conflicting palettes
**Severity: MEDIUM**
**File:** `ContentPacks/TiberianSun/Nod/yaml/infantry.yaml`

Same pattern as P3 — two `RenderSprites` blocks with `ra2player` then
`playerra2`.

### P5. `forgotten_visceroid` duplicate RenderSprites with conflicting palettes
**Severity: MEDIUM**
**File:** `ContentPacks/TiberianSun/Forgotten/yaml/infantry.yaml`

Two `RenderSprites` blocks: first `playerra2`, second `player`. The second
wins, but the first is dead code.

---

## DUPLICATE TRAIT BLOCKS (B6)

### D1. `^Heroes` template has duplicate `WithDeathAnimation`
**Severity: LOW (second overrides first, losing death type config)**
**File:** `mods/cameo/rules/heroes.yaml:3179, 3188`

```yaml
WithDeathAnimation:     # line 3179 — overridden by line 3188
    # (no settings — defaults)
...
WithDeathAnimation:     # line 3188 — this one wins
    UseDeathTypeSuffix: False
```

The first `WithDeathAnimation` at line 3179 is dead code. The second at
line 3188 overrides it.

### D2. `cabal_techcenter` has duplicate `WithDeathAnimation`
**Severity: LOW**
**File:** `ContentPacks/TiberianSun/CABAL/yaml/buildings.yaml:254-261`

```yaml
WithDeathAnimation@BIB:
    DeathSequence: dead-ground
    DeathSequencePalette: ra2player   # wrong palette
WithDeathAnimation:
    DeathSequencePalette: playerra2   # correct palette
```

The `@BIB` suffixed trait and the non-suffixed trait are separate traits
(both exist), but the BIB one uses the wrong palette.

### D3. `cabal_cyborgreaper` has duplicate `WithDeathAnimation`
**Severity: LOW**
**File:** `ContentPacks/TiberianSun/CABAL/yaml/vehicles.yaml`

### D4. `cabal_heavyreaper` has duplicate `WithDeathAnimation`
**Severity: LOW**
**File:** `ContentPacks/TiberianSun/CABAL/yaml/vehicles.yaml`

---

## ENGINE VERSION MISMATCH (B8)

### E1. `SourceFlare` and `GlowIntensity` on LaserZap projectiles — silently ignored
**Severity: MEDIUM (intended visual effects not rendered)**
**Files:**
- `mods/cameo/weapons/tiberiansun.yaml:483,513,569-573`
- `mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml:533-538,55`
- `mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml:79-84,163-168`

The `SourceFlare` and `GlowIntensity` properties are used on `LaserZap`
projectiles in multiple weapon definitions. These fields do not exist in the
currently checked-out engine version (`7ba39d9`). The `mod.config` specifies
engine version `b89ae60` which presumably supports them.

OpenRA's `FieldLoader` silently ignores unknown fields during normal YAML
loading, so the game boots without crashing. However, the intended visual
effects (source flares and glow on laser beams) are not rendered.

**Fix:** Sync the engine directory to the version specified in `mod.config`
(`b89ae60`), or remove these fields until the engine is updated.

---

## AI WIRING ISSUES (B5)

### A1. `cabal_enlighted` not in any AI build list
**Severity: MEDIUM (AI never builds this unit)**
**File:** `mods/cameo/ai/ai.yaml`

The CABAL Enlighted infantry unit is defined in
`ContentPacks/TiberianSun/CABAL/yaml/infantry.yaml:493` but is not referenced
in any AI squad composition. The AI will never build this unit.

---

## UPGRADE INVERSION ISSUES (B9)

### U1. Many upgrades have inverted multiplier directions
**Severity: MEDIUM (upgrades weaken instead of strengthen, or vice versa)**
**File:** Various (from `audit_upgrades.py`)

The audit found many upgrade multipliers that appear inverted — e.g.
`FirepowerMultiplier` values below 100 (which weakens firepower) on upgrades
that should strengthen, and `ReloadDelayMultiplier` values above 100 (which
slows reload) on upgrades that should speed up.

**Key examples (no intent entry in audit):**

| Upgrade | Actor | Trait | Value | Issue |
|---|---|---|---|---|
| asianalliance_doctrine_heavypulverizerweapons | asianalliance_pulverizer | FirepowerMultiplier | 75 | <100 = weaker |
| japan_upgrade_advancedplasmaweapons | japan_tankbuster | FirepowerMultiplier@DualBeam | 50 | <100 = weaker |
| ra1_soviets_doctrine_teslaandexperimentaltech | ra1_soviets_btr80 | ReloadDelayMultiplier@Gatling | 200 | >100 = slower |
| ra2_soviets_doctrine_heavyarmorplatings | ra2_soviets_kirovairship | SpeedMultiplier | 90 | <100 = slower |
| ra2_soviets_doctrine_reactivearmor | ra2_soviets_apocalypsetank | SpeedMultiplier | 95 | <100 = slower |

**Note:** Some of these may be intentional design decisions (e.g. heavy armor
trades speed for protection). The audit flags them because they have "no
intent entry" — no documentation explaining the inversion. Each needs design
review.

---

## ORPHANED/DANGLING REFERENCES (B10)

### O1. `tsvissml` — actor referenced but commented out
**Severity: CRASH** (see C1 above)

### O2. 527 orphaned fluent actor-* messages
**Severity: LOW (hygiene)**
**File:** Various `.ftl` files

527 fluent messages for actors that no longer exist. These are dead
translations consuming memory.

### O3. 548 unreferenced sequence images
**Severity: LOW (hygiene)**
**File:** Various sequence files

548 sequence image definitions not referenced by any live actor or weapon.
Includes many template sequences (`^ra2infantry`, `^tsbasicinfantry`, etc.)
that may be inherited but not directly referenced.

### O4. Orphaned weapons (sample)
**Severity: LOW (hygiene)**

22 weapons defined but never referenced by any actor armament, including:
`psireveal`, `ra120mmirak`, `ra1_allies_alliedsniper`, `tkmabramscannon`,
`wc2_tower_axe`, `wc2tornadoTest`.

### O5. Orphaned conditions (sample)
**Severity: LOW (hygiene)**

Conditions granted but never consumed:
`armory-rank`, `chaosgas && !untargetable`, `defensebot`, `disable_movement`,
`emptesla`, `harkonnenexplode`, `littlebuilderenable`, `propaganda`,
`ra2_soviets_doctrine_conscription`, `shade-ready`, `up_tsunami.asian`,
`yuri_doctrine_psioniclegion`

---

## WEAPON UNIQUENESS ISSUES (W)

### W1. 38 same-faction duplicate weapons
**Severity: LOW (design concern)**
**File:** Various

38 cases where distinct actors in the same faction share the same weapon
identifier. Some may be intentional (e.g. `medicheal` shared between medic
and medivac), but others may be copy-paste errors.

### W2. 34 cross-faction duplicate weapons
**Severity: LOW (design concern)**

34 cases where actors in different factions share the same weapon. Notable:
`d2k_rocket_trooper` shared between Ixian and Ordos, `lmg` shared between
Ixian and Ordos.

---

## GARRISON WEAPON ISSUES (G)

### G1. 6 melee infantry lack garrison weapons
**Severity: MEDIUM (units useless when garrisoned)**
**File:** `ContentPacks/Warcraft2/*/yaml/infantry.yaml`

WC2 melee units (`wc2_humans_footman`, `wc2_humans_warcraft3footman`,
`wc2_orcs_grunt`, `wc2_orcs_warcraft3grunt`) have no garrison weapon. When
garrisoned in a structure, they cannot attack. Also affects
`wc2_humans_highelfpriest` and `wc2_humans_highelfsorceress` whose weapons
may not have garrison variants.

---

## MINIMUM RANGE INCONSISTENCIES (B9)

### M1. 14 weapons with non-standard MinRange values
**Severity: LOW**

14 weapons have `MinRange` values that don't match the expected
`round(Range/5)` formula. Notable:
- `MagicOrbHailstormSpawner`: MinRange 5000 vs expected 2000
- `BallistaSingleShotAir`: MinRange 1500 vs expected 1845
- `BallistaTowerMultiShot`: MinRange 2703 vs expected 1845

---

## EFFECT WARHEAD NAMING (B7)

### N1. One CreateEffect warhead naming violation
**Severity: LOW**
**File:** `maps/survival/Weapons.yaml:28`

`PortableIoncannon` has `Warhead@0Eff: CreateEffect` — the `@0Eff` suffix
violates the naming convention (should be `@Eff` or `@1Eff`).

---

## SUMMARY BY SEVERITY

| Severity | Count | Bug IDs |
|---|---|---|
| CRASH | 1 | C1 |
| HIGH | 2 | P1, P2 |
| MEDIUM | 6 | P3, P4, P5, E1, A1, U1, G1 |
| LOW | 7 | D1-D4, O2-O5, W1-W2, M1, N1 |

## ADDITIONAL FINDINGS (continued audit)

### D5. `wc2_humans_militiapeasant` duplicate Voiced trait
**Severity: LOW (dead code — both identical)**
**File:** `ContentPacks/Warcraft2/Humans/yaml/infantry.yaml:38-39, 49-50`

Two `Voiced:` blocks with identical `VoiceSet: wc2voicehumanpeasant`. The second
is redundant dead code.

### D6. `schwarzermond_drone_husk` duplicate RenderSprites
**Severity: LOW**
**File:** `ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/aircraft.yaml:105-106`

Two `RenderSprites` blocks — first sets `Image: schwarzermond_drone`, second
sets `PlayerPalette: playerra2`. Should be merged into one block.

### D7. `naxis_interceptor_husk` duplicate RenderSprites
**Severity: LOW**
**File:** `ContentPacks/RedAlert2Mod/Naxis/yaml/aircraft.yaml:106-108`

Same pattern as D6 — two `RenderSprites` blocks that should be merged.

### P6. GDI promotion prerequisite mismatches — 4 units unbuildable
**Severity: HIGH (gameplay-breaking)**
**File:** `ContentPacks/TiberianSun/GDI/yaml/promotions.yaml`

Two GDI promotions provide prerequisite tokens that don't match what consuming
actors/promotions require:

- **`ts_gdi_promotion_unlockzonetrooper`** provides `zonetrooper` (line 41),
  but `ts_gdi_zonetrooper` requires `~ts_gdi_promotion_unlockzonetrooper`
  (infantry.yaml:270) and `ts_gdi_promotion_unlockmammothmkii` requires
  `ts_gdi_promotion_unlockzonetrooper` (line 51).
  → **Zone Trooper and Mammoth MKII can never be built/unlocked.**

- **`ts_gdi_promotion_unlockzoneorca`** provides `uupzoneorca` (line 69),
  but `ts_gdi_zoneorcafighter` requires `~ts_gdi_promotion_unlockzoneorca`
  (aircraft.yaml:95) and `ts_gdi_promotion_unlockhammerhead` requires
  `ts_gdi_promotion_unlockzoneorca` (line 79).
  → **Zone Orca Fighter and Hammerhead can never be built/unlocked.**

Fix: Change `Prerequisite: zonetrooper` → `Prerequisite: ts_gdi_promotion_unlockzonetrooper`
and `Prerequisite: uupzoneorca` → `Prerequisite: ts_gdi_promotion_unlockzoneorca`.

### U2. CABAL upgrade coverage gaps
**Severity: LOW (may be intentional)**
**File:** `ContentPacks/TiberianSun/CABAL/yaml/`

- `cabal_upgrade_darkarmament` covers 10/16 infantry — 6 uncovered:
  `cabal_beholder`, `cabal_cyborg_assassin`, `cabal_dissolver`, `cabal_engineer`,
  `cabal_hackercyborg`, `cabal_orb_drone`
- `cabal_upgrade_firewallprotocol` / `fullassimilation` / `networkedcombatprotocols`
  cover 33/38 roster — 5 uncovered: `cabal_constructionyard`, `cabal_dissolver`,
  `cabal_mobileconstructionvehicle`, `cabal_tiberiumharvester`, `tsprobe`

Non-combat units (engineer, harvester, probe, MCV, construction yard) may be
intentionally excluded. `cabal_dissolver` and `cabal_hackercyborg` being
uncovered from dark armament may be a gap if they have weapons.

---

## PREVIOUSLY REPORTED ISSUES NOW RESOLVED

The following issues from prior audit sessions have been verified as fixed:

- **ixian_koda_tank missing icon sequence** — `icon:` sequence now defined at
  `ContentPacks/D2k/Ixian/yaml/sequences.yaml:1372`
- **Starcraft alien ranks** — All three factions now have separate decorations:
  `^ZergRankDecoration`, `^TerranRankDecoration`, `^ProtossRankDecoration`
- **interceptor.nax rename** — Now `naxis_interceptor` in Naxis content pack
- **drone.nax move** — Now `schwarzermond_drone` in SchwarzerMond content pack
- **Dog immunity for WC2 peasants** — `Targetable@DogImmune` confirmed present
  on `wc2_humans_militiapeasant` (line 36-37)
- **CABAL Obelisk range** — `TSCABALObeliskLaserFire` now has `Range: 12288`
- **CABAL Obelisk detection circle** — `WithRangeCircle` present with `Range: 12c0`
- **CABAL Obelisk sight range** — `RevealsShroud: 7c0`, matching Nod obelisk
- **Artillery spider magicnuke explosion** — `TS120mm_bluenuke` has
  `Warhead@3Eff: CreateEffect` with `Explosions: magicnuke_med`, sequence and
  PNG asset both exist
- **Repair drone** — `cabal_repair_drone` has `TargetRelationships: Ally`,
  `ForceTargetRelationships: Ally`, weapon inherits `^RepairWeapon` with
  `ValidRelationships: Ally` — appears correctly wired
- **All loaded sequence filenames** — 0 missing asset files in loaded sequence
  definitions
- **No weapons with 0 damage** — Clean
- **No actors with RevealsShroud Range: 0** — Clean
- **No mismatched DeathSequencePalette vs CrushedSequencePalette** — Clean
- **No DeathPaletteIsPlayerPalette: true with non-player palette** — Clean
- **No circular inheritance in loaded files** — `^AnyMissile` ↔ `^MissileWeapon`
  cycle exists only in unloaded `weapons/missiles.yaml`
- **No missing Inherits references in loaded non-chrome files** — Clean
- **No undefined CarrierMaster actors** — Clean
- **No undefined SpawnActorOnDeath actors in loaded files** (except C1 `tsvissml`)
- **Template conformance (T1/T2)** — Clean (T2b: 6 informational icon offsets)
- **No prerequisite mismatches in loaded files** (except P6 GDI promotions) —
  all other actors inherit `ProvidesPrerequisite@buildingname:` from templates
- **No undefined weapon references in loaded files** — Clean
- **No undefined TransformOnCondition targets in loaded files** — Clean
- **No conditions consumed but never granted** — Clean
- **No actors with HP: 0 in loaded files** — Clean
- **No buildable actors missing Queue in loaded files** — Clean

---

## RECOMMENDED PRIORITY

1. **C1** — Fix `tsvissml` crash (uncomment actor or remove SpawnActorOnDeath)
2. **P6** — Fix GDI promotion prerequisite mismatches (4 units unbuildable)
3. **P1** — Fix `ra2player` → `playerra2` death palette on ~30 TS/TKM infantry
4. **P2** — Fix `^BaseBuilding` default death palette or override on all TS buildings
5. **P3-P5** — Remove duplicate RenderSprites blocks with conflicting palettes
6. **E1** — Sync engine to `b89ae60` or remove unsupported `SourceFlare`/`GlowIntensity`
7. **A1** — Add `cabal_enlighted` to AI build lists
8. **G1** — Add garrison weapons for WC2 melee units
9. **U1** — Review and document inverted upgrade multipliers
10. **D1-D7** — Remove duplicate trait blocks (low priority, hygiene)
11. **O2-O5** — Clean up orphaned content (low priority)
