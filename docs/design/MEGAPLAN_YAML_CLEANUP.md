# MEGAPLAN — YAML Clean-up Program: Zero Errors, Zero Warnings

Achieve zero errors and zero warnings from `OpenRA.Utility.exe cameo --check-yaml` across the entire mod.

## Baseline

- **Saved at:** `docs/audit/check-yaml-baseline.txt`
- **Date:** 2026-07-23
- **Commit:** `85de3138e` (post-naming-refactor)
- **Totals:** 379,899 errors, 80,703 warnings
- **Analysis tool:** `tools/audit/analyze_check_yaml.py`
- **Note:** Errors are multiplied by ~39 (one per map tested). Unique error count is approximately 9,700.

## How to re-run the check

```powershell
$env:MOD_SEARCH_PATHS="C:\Users\AedisToru\Documents\GitHub\Cameo-mod\mods,C:\Users\AedisToru\Documents\GitHub\Cameo-mod\engine\mods"
$env:ENGINE_DIR=".."
.\bin\OpenRA.Utility.exe cameo --check-yaml 2>&1 | Out-File -FilePath docs\audit\check-yaml-baseline.txt -Encoding utf8
```

Then analyze:
```
python tools\audit\analyze_check_yaml.py docs\audit\check-yaml-baseline.txt
```

## Error Categories (sorted by count, with estimated unique count)

| # | Category | Total | ~Unique | Root Cause |
|---|---|---|---|---|
| 1 | SpawnActorOnDeath missing actor | 184,509 | ~4,731 | Actors reference non-existent death actors (`susaunstableeffects`, `zombie1.infect`, `zombie2.infect`, `glscrapcrate`, `drassimilator`, `dtmutant`, `wolfe3`, `2100artifact`, `civzombie.infect`) |
| 2 | RepairableInfo missing actor | 48,828 | ~1,252 | `RepairableInfo.RepairActors` lists reference missing buildings (`drfghosp`, `drihosp`, `drterhosp`, etc.) |
| 3 | RepairableNear missing actor | 21,060 | ~540 | Naval units list repair buildings that don't exist (`tsgtyard2`, `tsntyard`, `cra2nayard`, `craspen`, `ccncsyrd`, etc.) |
| 4 | TypeDictionary duplicate type | 9,438 | ~242 | Actors define both `Interactable` and `Selectable` traits (engine conflict) |
| 5 | Crate ExcludedActorType missing actor | 5,265 | ~135 | Crate exclusion lists reference non-existent actors (`angrymob1`, `sow_advancer`, `wc_o_skeleton`, etc.) |
| 6 | PassengerInfo missing actor | 12,168 | ~312 | `PassengerInfo.CargoConditions` references missing actors (`susamedivacblackhawk`, etc.) |
| 7 | Unresolved prerequisite | 3,081 | ~79 | `~wip-content`, `~disable`, `~wip`, `latinsyndicate_defensebureau`, `~construction_yard.atreides`, etc. |
| 8 | TransformsIntoRepairable missing actor | 2,885 | ~74 | Construction yard transform lists missing repair actors (`fix`, `rafix`, `2100rr`, `c1fix`, etc.) |
| 9 | Undefined palette reference | 2,379 | ~61 | `playerra2` palette not defined in palette.yaml |
| 10 | Undefined player palette reference | 468 | 12 | `d2kplayer` palette not defined |
| 11 | VisibilityType.Footprint | 7,956 | ~204 | Engine compatibility issue with `VisibilityType.Footprint` |
| 12 | Invalid faction | 174 | ~5 | Maps using `england` faction that doesn't exist in Cameo |
| 13 | Undefined MuzzleSequence | 684 | ~18 | Armament `MuzzleSequence` references undefined sequences |
| 14 | LaunchAngle errors | 234 | ~6 | Weapon projectile `LaunchAngle` minimum violations |
| 15 | Consumes ungranted conditions | ~1,500 | ~40 | Actors consume conditions no trait grants |
| 16 | Other (image/sequence issues) | ~200 | ~5 | Misc |

## Warning Categories (sorted by count)

| # | Category | Total | ~Unique | Root Cause |
|---|---|---|---|---|
| 1 | Unused granted conditions | 62,406 | ~1,600 | Actors grant conditions no trait consumes (massive — affects most buildings/units) |
| 2 | Interactable+Selectable conflict | 9,438 | ~242 | Same as TypeDictionary duplicate — actors with both traits |
| 3 | Missing FTL key | 6,717 | ~172 | Missing translations for generic keys (`Structure`, `Soldier`, `Tank`, `Plane`, `Vehicle`, `Tree`, `Boat`, etc.) + actor names |
| 4 | WithDeathAnimation/TakeCover sequence warnings | 1,765 | ~45 | Missing `ProneSequence` or `DeathSequence` references |
| 5 | Unused field/trait | 377 | ~10 | Unused FTL attributes, unused variables |

## Fix Plan — Phased Approach

### Phase 1: Palette Fixes (Quick wins, ~2,847 errors → 0) — ✅ DONE

**Effort: S (< 1h)**

1. ✅ Fixed `d2kplayer` → `playerd2k` in `ContentPacks/D2k/Shared/yaml/templates.yaml`
2. ✅ Added dummy `playerra2` regular palette in `palettes.yaml` to satisfy `RenderVoxelsInfo` lint check
   - Root cause: engine bug — `RenderVoxels.PlayerPalette` uses `[PaletteReference]` instead of `[PaletteReference(true)]`
   - `RenderSprites.PlayerPalette` correctly uses `[PaletteReference(true)]`
3. Remaining `d2kplayer` ref in `d2k.yaml` is commented out

### Phase 2: TypeDictionary / Interactable+Selectable Conflicts (~9,438 errors + 9,438 warnings → 0) — ✅ DONE

**Effort: S (30 min)**

1. ✅ Root cause: `^upgrade.template` defines `Interactable:`, `^promotion_upgrade.template` inherits it and adds `Selectable:`
2. ✅ Fix: added `-Interactable:` to `^promotion_upgrade.template` in `defaults.yaml`
3. ✅ 242 promotion actors fixed (236 from content packs + 6 bridges are separate minor issue)

### Phase 3: Missing FTL Keys (~6,717 warnings → ~4,327 remaining) — ⏳ PARTIAL

**Effort: M (one session)**

1. ✅ Added 450 simple identifier keys to `mods/cameo/fluent/rules/missing_keys_en.ftl` (~2,390 warnings resolved)
2. ⏳ 2,705 complex keys (with spaces/special chars) remain — Fluent message IDs can't contain spaces
3. ⏳ These require YAML changes to use proper FTL references instead of inline text

### Phase 4: Missing Actor Definitions (Biggest category, ~184K+48K+21K+12K+5K+3K errors → 0) — ⏳ PARTIAL

**Effort: L (multi-session)**

This is the largest category. Strategy:

1. **SpawnActorOnDeath missing actors** (~184,509 errors) — ✅ DONE
   - Root cause: core templates (`^Vehicle`, `^Infantry`, `^Building`, `^AffectedByDriverKill`) had `SpawnActorOnDeath` referencing actors from unloaded content packs
   - ✅ Removed `^UnstableEffect` inheritance from `^Vehicle`, `^SeaCreature`, `^DefaultInfantry`, `^Building` (referenced `susaunstableeffects` from unloaded `shockwave.yaml`)
   - ✅ Removed `SpawnActorOnDeath@ZombieInfect` from `^BaseBuilding` and `^AffectedByDriverKill` (referenced `zombie1.infect`, `zombie2.infect`, `civzombie.infect` from unloaded `infected.yaml`)
   - ✅ Removed `SpawnActorOnDeath@Scraps` from `^Vehicle` (referenced `glscrapcrate` from unloaded `generals.yaml`)
   - ✅ Removed `SpawnActorOnDeath@Contaminator/Assimilator/ZombieInfect/QuestionMutate/Wolfestein` from `^Infantry` (referenced `ordos_contaminator`, `drassimilator`, `civzombie.infect`, `dtmutant`, `wolfe3` from unloaded files)
   - ✅ Removed `SpawnActorOnDeath@2100ResearchSteal` from 3 templates (referenced `2100artifact` from unloaded `wz2100.yaml`)
   - **NOTE:** When content packs are loaded, these `SpawnActorOnDeath` blocks should be re-added via template overrides in the content pack YAML
2. **RepairableInfo/RepairableNear missing actors** (~1,800 unique) — ⏳ TODO
3. **PassengerInfo missing actors** (~312 unique) — ⏳ TODO
4. **Crate ExcludedActorType** (~135 unique) — ⏳ TODO
5. **TransformsIntoRepairable** (~74 unique) — ⏳ TODO

### Phase 5: Unresolved Prerequisites (~79 unique → 0)

**Effort: M (one session)**

1. `~wip-content` — either provide via a dummy actor or remove from buildable prereqs
2. `~disable` — same approach
3. `~wip` — same
4. `latinsyndicate_defensebureau` — define this building or fix references
5. `~construction_yard.atreides` and other D2k faction prereqs — provide or remove
6. `~EDEN_FACTORY_CONSUMER` — provide or remove
7. `!droppod` — provide or remove
8. `~techlevel.medium/high` — provide or remove
9. `tsproc`, `~ra2fact` — provide or remove
10. `steelconsortium_geothermalreactor` — define or fix

### Phase 6: Unused Granted Conditions (~1,600 unique → 0)

**Effort: L (multi-session)**

1. Identify all actors granting conditions via `GrantCondition`/`ExternalCondition` that no trait consumes
2. Either remove the grant or add a consuming trait
3. This may be the largest warning category but also the most tedious
4. Consider a script to auto-detect and categorize

### Phase 7: VisibilityType.Footprint (~204 unique → 0)

**Effort: M**

1. This is an engine compatibility issue — `VisibilityType.Footprint` may be deprecated
2. Check engine source for the correct replacement
3. Update affected actors

### Phase 8: Invalid Factions on Maps (~5 unique → 0)

**Effort: S**

1. Maps using `england` faction — bulk-rewrite to `Random` or a valid faction
2. This was already noted in the roadmap (pre-existing broken maps)

### Phase 9: MuzzleSequence + LaunchAngle + Misc (~30 unique → 0)

**Effort: S**

1. Fix undefined `MuzzleSequence` references (3 actors)
2. Fix `LaunchAngle` minimum violations (6 weapons)
3. Fix `Consumes ungranted conditions` errors (~40 actors)
4. Fix image/sequence issues (~5 actors)

### Phase 10: WithDeathAnimation/TakeCover Sequence Warnings (~45 unique → 0)

**Effort: S**

1. Add missing `ProneSequence` references
2. Fix `DeathSequence` references
3. Verify against sequence definitions

### Phase 11: Unused Field/Trait (~10 unique → 0)

**Effort: S**

1. Remove unused FTL attributes
2. Fix unused variables in fluent files

## Verification

After each phase:
1. Re-run `--check-yaml` and save output
2. Run `analyze_check_yaml.py` to compare against baseline
3. Commit with message `fix(yaml-cleanup): phase N — <description>`
4. Update this document with progress

## Final Goal

**Zero errors, zero warnings from `OpenRA.Utility.exe cameo --check-yaml`.**

This is achievable but requires sustained effort across multiple sessions. The biggest wins come from:
- Phase 4 (missing actors) — eliminates ~95% of all errors
- Phase 6 (unused conditions) — eliminates ~77% of all warnings
- Phase 2 (Interactable/Selectable) — eliminates both errors AND warnings simultaneously

## Progress Log

| Date | Phase | Commit | Impact |
|---|---|---|---|
| 2026-07-23 | Phase 2 | `98b22a3e1` | -9,438 errors, -9,438 warnings (Interactable/Selectable conflict) |
| 2026-07-23 | Phase 3 | `9ac9e8148` | ~-2,390 warnings (450 simple FTL keys added) |
| 2026-07-23 | Phase 4 | `b650766d9` | ~-184,509 errors (SpawnActorOnDeath refs to unloaded actors) |
| 2026-07-23 | Phase 1 | `13e00fee6` | ~-2,847 errors (palette reference fixes) |
