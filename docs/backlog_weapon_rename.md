# Weapon Rename Backlog

> Status: **Paused**. Higher-priority crash/bug fixes are being addressed first.
> This document captures the research and tooling so the next agent can pick up exactly where this session left off.

## Goal

Rename weapon IDs to unit-scoped, context-sensitive names so that a weapon can be found by searching for its owning actor.

User’s preferred stopgap naming scheme (2026-07-14):

```
<actor_id>_<original_weapon_name>
```

Example: `ra1_artillery_155mm` instead of `ra1_artillery_cannon` (keep the legacy weapon name like `155mm`, but prefix it with the actor name).

## Why this is paused

Live crashes and faction bugs (CABAL Backup Systems, husk repair/reanimate, cyborg death palettes, missing voxel/sequence references) are now higher priority. The rename must not be resumed until those are stable and committed.

## Current artifacts

- `gen_weapon_rename_map.py` (repo root) — draft script that builds a rename map.
  - Reads `mods/cameo/ContentPacks/**/yaml/weapons.yaml`, `mods/cameo/weapons/*.yaml`, and unit rules.
  - Classifies weapons by warhead/projectile traits and by which actor(s) use them.
  - Proposes `<actor>_<weapon>` style names.
- `tools/rename/rename_map_weapons.yaml` — generated map from the above script.
- `inventory_weapons.py` (repo root) — draft inventory helper.
- `tools/rename/apply.py` — the proven rename-applicator used for actor/asset renames.
  - It can handle `Weapons:` references in rules, `Armament` weapon fields, `Weapon:` in warheads, `ImpactSounds`/`Report`/`Image` should not be touched.
- `tools/audit/dump_resolved.py` and `tools/rename/apply.py` should be used to verify behavior is unchanged after rename.

## Research and constraints discovered

1. **Read order required before touching YAML**: `docs/DESIGN.md` → `docs/audit/SUMMARY.md` → `docs/Cameo_Knowledge_Base_Manual.md` → `docs/MASTER_REPORT.md` (§9/§10/§13).
2. **Naming grammar** (`DESIGN.md` §1):
   - One lowercase group, no hyphens, underscores only.
   - Game prefixes only on collisions.
   - Variant markers: `_husk`, `_sp`, `_r4`, `_wild`, `_mk2`, `_elite`, `_ai`, `_water`.
   - Asset suffixes must be full words (`_make`, `_bib`, not `_mk`/`_bb`).
3. **Weapon construction rules** (`DESIGN.md` §3):
   - Weapons must inherit from class templates (`^`), never from other unit weapons.
   - AA twins must inherit the ground weapon.
   - Weapon grouping order: basic → elite → AA → upgrade variants.
   - Every infantry armament needs `LocalOffset`.
4. **Rename must be behavior-preserving**:
   - Use `dump_resolved.py` before/after; diff should be empty.
   - `apply.py` protects voice sets and audio files; weapon rename is not a voice-set rename.
5. **Audit suite**: `tools/audit/run_all.sh` (or individual `python tools/audit/audit_<name>.py`) should be run before and after the rename.

## Proposed steps to finish

1. **Map every weapon to its owning actor(s)**
   - Use `gen_weapon_rename_map.py` / `inventory_weapons.py` or a new script.
   - For shared weapons (multiple actors), list all owners and decide whether to keep a shared name or create per-actor variants.
   - Per `DESIGN.md` §10, every actor should own its own weapon entries; shared utility weapons (C4, repair, capture) are the only exceptions.
2. **Classify weapon type**
   - By projectile, warhead, damage type, range, etc.
   - This helps avoid accidental cross-actor inheritance.
3. **Generate rename map**
   - Output format compatible with `tools/rename/apply.py`.
   - Use `<actor_id>_<original_weapon_id>` (or plural if multiple mounts, e.g. `cabal_manticore_missiles`).
4. **Review the map manually**
   - Check `tools/rename/rename_map_weapons.yaml`.
   - Fix any non-compliant names or collisions.
5. **Apply the rename**
   - `python tools/rename/apply.py tools/rename/rename_map_weapons.yaml` (or equivalent command).
   - Verify with `git diff --stat` and `dump_resolved.py`.
6. **Update all references**
   - `Armament` `Weapon:` in rules.
   - `SpawnSmokeParticle`/`CreateEffect`/`AirstrikePower`/`ParadropPower` weapon references.
   - `SpawnActorInArea`/`Weapon`? Search for the old weapon IDs.
7. **Boot test**
   - `launch-game.cmd` → main menu, no new `exception-*.log` in `%APPDATA%/OpenRA/Logs`.
   - `perf.log` ends with `MenuPostProcessEffect.PostWorldLoaded`.
8. **Run audit suite**
   - `tools/audit/run_all.sh` or individual weapon/actor audit scripts.
9. **Commit**
   - Scoped `git add` for the rename files and changed YAML only.

## Relevant files

- `mods/cameo/weapons/*.yaml`
- `mods/cameo/ContentPacks/**/yaml/weapons.yaml`
- `mods/cameo/rules/*.yaml`
- `mods/cameo/ContentPacks/**/yaml/*.yaml`
- `mods/cameo/sequences/*.yaml`
- `mods/cameo/ContentPacks/**/yaml/sequences.yaml`
- `tools/rename/apply.py`
- `tools/audit/dump_resolved.py`
- `docs/DESIGN.md`
- `docs/audit/SUMMARY.md`

## Notes from Shattered Paradise husk revival research

- OpenRA base `Husk` trait handles remains positioning, ownership, drag/crush.
- Base `SpawnActorOnDeath` spawns backup husk actors.
- `GrantPeriodicCondition` + `TransformOnCondition` handle reanimation/rebuild.
- `Repairable` + `RepairableNear`/`RepairsUnits` allow husk repair.
- CABAL husk definitions are in:
  - `mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/vehicles.yaml`
  - `mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/husks.yaml`
  - `mods/cameo/rules/tiberiansun.yaml` (backup mode units)
- Fix these first (CABAL Backup Systems, husk repair/reanimate) before resuming the weapon rename.
