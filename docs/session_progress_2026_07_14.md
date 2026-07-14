# Session Progress Notes — 2026-07-14

_Comprehensive state-of-work document for any AI agent resuming from this
point. Read this BEFORE touching any YAML file._

## Quick start checklist for resuming agent

1. Read `docs/DESIGN.md` fully (non-negotiable per user).
2. Read `docs/audit/SUMMARY.md` for known-issue state.
3. Read `docs/design/ROADMAP.md` for the work queue (crashes jump queue).
4. Read this document for session-specific context.
5. Read `docs/backlog_weapon_rename.md` if working on weapon renaming.
6. Boot test protocol: snapshot exception logs, launch game, wait 30s,
   kill, verify `perf.log` ends with `MenuPostProcessEffect.PostWorldLoaded`
   and NO new `exception-*.log`.

## Commits this session

### `4bfd1bcaf` — Fix ts_nod_ticktank and magicnuke crashes + asset renames

**Files changed (16):**
- `mods/cameo/sequences/voxels.yaml` — Added `idle: tsttnk` to
  `ts_nod_ticktank` and `idle: tsbike` to `ts_nod_attackcycle`. These
  voxel sequence entries had empty `idle:` keys with no filename, so the
  engine couldn't find the voxel model. The .vxl files use the original
  TS compressed names (`tsttnk.vxl`, `tsbike.vxl`), not the renamed
  actor IDs.
- `mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml` — Removed
  `Image: magicnuke` from 3 `CreateEffect` warheads
  (`CabalCommandoPlasmaNeutron`, `CabalCommandoPlasmaMk2Neutron`,
  `CabalRavagerPlasmaNeutron`). The `Image:` field was causing the engine
  to look for explosion sequences inside the `magicnuke` image, but the
  sequences (`magicnuke_med`, `magicnuke_small`) are defined at the
  top level of the sequence set, not inside an image. The working
  `CabalMagicNuke` weapon only used `Explosions:` without `Image:`.
- `mods/cameo/sequences/redalert2.yaml` — Updated `ra2_cgtbnkbb.shp` →
  `ra2_cgtbnkbib.shp` (3 refs), `ra2_ctoutpbb.shp` →
  `ra2_ctoutp_bib.shp` (4 refs), `ra2_ntyardmk.shp` →
  `ra2_ntyardmake.shp`, `ra2_cgoildmk.shp` → `ra2_cgoildmake.shp` (2 refs).
- `mods/cameo/sequences/tiberiansun.yaml` — Updated `tsnttmplmk.shp` →
  `tsnttmplmake.shp`.
- `mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/sequences.yaml` —
  Updated `tambarmk.shp` → `tambarmake.shp`, `tampowrmk.shp` →
  `tampowrmake.shp`, `tamradrmk.shp` → `tamradrmake.shp`, `tamrefmk.shp`
  → `tamrefmake.shp`, `tamrefbb.shp` → `tamref_bib.shp`, `tamtechmk.shp`
  → `tamtechmake.shp`.
- 10 asset files renamed via `git mv` (bb→bib, mk→make convention).
- `docs/backlog_weapon_rename.md` — New document with full weapon rename
  research and tooling notes.

## Uncommitted changes (in working tree)

### CABAL Backup Systems upgrade coverage (IN PROGRESS)

**File:** `mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/vehicles.yaml`

**Changes made (not yet committed):**
- `cabal_legion` (line ~970): Added `SpawnActorOnDeath@backup` trait:
  ```yaml
  SpawnActorOnDeath@backup:
      Actor: cabal_legion_backup
      RequiresCondition: cabal_upgrade_backupsystems
  ```
  `cabal_legion` already inherited `^cabal_upgrade_backupsystems` but had
  no spawn-on-death trait, so the backup upgrade did nothing for it.

- `cabal_avatar` (line ~861): Added `Inherits@BACKUP:
  ^cabal_upgrade_backupsystems` AND `SpawnActorOnDeath@backup` trait:
  ```yaml
  Inherits@BACKUP: ^cabal_upgrade_backupsystems
  ...
  SpawnActorOnDeath@backup:
      Actor: cabal_avatar_backup
      RequiresCondition: cabal_upgrade_backupsystems
  ```
  `cabal_avatar` was completely missing backup systems coverage.

**STILL NEEDED before commit:**
1. Create `cabal_legion_backup` actor in `mods/cameo/rules/tiberiansun.yaml`
   (after the existing `cabal_tarantula_backup` block, around line 1397).
   Pattern to follow (from `cabal_manticore_backup` at line 1270):
   ```yaml
   cabal_legion_backup:
       Inherits: cabal_legion
       Valued:
           Cost: 3500
       Tooltip:
           Name: Legion (Backup Mode)
       Buildable:
           BuildPaletteOrder: 40
           Prerequisites: ~cabal_mechfactory, ~disabled
           Queue: Disabled
           Description: HuskMode
       Mobile:
           Locomotor: wheeled
           TurnSpeed: 0
           Speed: 0
       Health:
           HP: 300000
       Repairable:
           HpPerStep: 5000
       Voiced:
           VoiceSet: TSCABALVehicles
       RenderSprites:
           Image: tstnkspid
           PlayerPalette: player_rgba
       WithFacingSpriteBody:
           Sequence: stand
       WithMoveAnimation:
           MoveSequence: walk
           ValidMovementTypes: Horizontal, Turn
       -SpawnActorOnDeath@backup:
       GrantPeriodicCondition@rebuild:
           Condition: buildingrebirth
           CooldownDuration: 1500
           PauseOnCondition: chronobeamed
       TransformOnCondition@buildingrebirth:
           RequiresCondition: buildingrebirth
           IntoActor: cabal_legion
           SkipMakeAnims: true
       WithColoredOverlay@backup:
           Color: 000000b4
   ```

2. Create `cabal_avatar_backup` actor similarly, inheriting from
   `cabal_avatar`, with `IntoActor: cabal_avatar` in the
   `TransformOnCondition`. Cost: 2250. Image: `cabal_manticore`.
   PlayerPalette: `player_rgba`.

3. Boot test (launch game, verify no new exception logs, perf.log ends
   with `MenuPostProcessEffect.PostWorldLoaded`).

4. Commit with scoped `git add`.

## Key learnings this session

### Voxel sequence naming

- OpenRA voxel sequences in `voxels.yaml` use the **sequence key** as the
  image/actor name. The `idle:` sub-key can have a filename that points
  to the actual `.vxl` file. If `idle:` has no filename, the engine uses
  the sequence key as the filename.
- TS units were renamed from compressed TS names (`tsttnk`, `tsbike`) to
  descriptive names (`ts_nod_ticktank`, `ts_nod_attackcycle`), but the
  `.vxl`/`.hva` files kept their original compressed names.
- The voxel sequence entries were created with the new descriptive keys
  but `idle:` had no filename, so the engine looked for
  `ts_nod_ticktank.vxl` which doesn't exist. Fix: explicitly set
  `idle: tsttnk` (the old filename) in the sequence.

### CreateEffect warhead Image field

- `CreateEffect` warhead's `Explosions:` field references a **sequence
  name** in the sequence set, NOT a filename.
- If `Image:` is also set, the engine looks for the sequence inside that
  image's sequence definitions. If the sequence is defined at the top
  level of the sequence set (not inside an image), setting `Image:`
  causes a crash.
- The `magicnuke` sequences (`magicnuke`, `magicnuke_med`,
  `magicnuke_small`, `magicnuke_micro`) are defined in `misc.yaml` under
  the `magicnuke:` image key. So `Explosions: magicnuke_med` works
  WITHOUT `Image:` because the engine resolves the sequence name
  directly. Setting `Image: magicnuke` makes it look for
  `magicnuke_med` as a sub-sequence of the `magicnuke` image, which
  fails.
- **Rule: only use `Image:` in `CreateEffect` if the explosion sequence
  is defined inside a different image key than the explosion name.**

### Asset naming convention (bb→bib, mk→make)

- Per DESIGN.md §1, asset suffixes must be full words: `_make` not `_mk`,
  `_bib` not `_bb`.
- The renaming was done for new-style prefixed files (e.g.
  `ra2_cgtbnkbb.shp` → `ra2_cgtbnkbib.shp`), but YAML references were
  not always updated in the same commit, causing `FileNotFoundException`
  crashes at boot.
- ~72 old-style unprefixed `mk` files and ~4 old-style `bb` files still
  need renaming (they are unreferenced, so low priority — see
  `check_mk_refs.py` and `check_bb_refs.py` output).
- Helper scripts `check_mk_refs.py` and `check_bb_refs.py` (in repo root)
  can verify which old-style files are still referenced.

### CABAL Backup Systems pattern

- The `^cabal_upgrade_backupsystems` template (in
  `ContentPacks/TiberianSun/CABAL/yaml/templates.yaml` line 159) grants
  the `cabal_upgrade_backupsystems` condition when the upgrade is
  researched.
- Each CABAL vehicle that should benefit from backup systems needs:
  1. `Inherits@BACKUP: ^cabal_upgrade_backupsystems` (to receive the
     condition).
  2. `SpawnActorOnDeath@backup` trait with `RequiresCondition:
     cabal_upgrade_backupsystems` and `Actor: <unit>_backup`.
  3. A `<unit>_backup` actor definition in `rules/tiberiansun.yaml` that:
     - Inherits the base unit.
     - Sets Speed/TurnSpeed to 0 (immobile).
     - Has high HP (2-5× base HP).
     - Has `Repairable` trait (for engineer repair).
     - Removes `SpawnActorOnDeath@backup` (via `-SpawnActorOnDeath@backup`).
     - Has `GrantPeriodicCondition@rebuild` + `TransformOnCondition` for
       auto-reanimation after a cooldown.
     - Has `WithColoredOverlay@backup` for visual distinction.
- Currently working backup actors: `cabal_manticore_backup` (line 1270),
  `cabal_artilleryspider_backup` (line 1315), `cabal_tarantula_backup`
  (line 1354).
- Missing backup actors: `cabal_legion_backup`, `cabal_avatar_backup`.
- `cabal_artilleryspider_backup` is missing `Repairable` trait (present
  on manticore and tarantula backups but not artillery spider).

### Weapon rename task (BACKLOGGED)

- Full research and tooling documented in `docs/backlog_weapon_rename.md`.
- Naming scheme: `<actor_id>_<original_weapon_name>`.
- Tooling: `gen_weapon_rename_map.py`, `inventory_weapons.py`,
  `tools/rename/rename_map_weapons.yaml`, `tools/rename/apply.py`.
- Verification: `tools/audit/dump_resolved.py` (diff should be empty
  before/after rename).
- **Do NOT resume until all P0 crashes and CABAL bugs are fixed.**

## Remaining bug queue (in priority order)

1. **CABAL Backup Systems upgrade coverage** — Add `cabal_legion_backup`
   and `cabal_avatar_backup` actors, add `Repairable` to
   `cabal_artilleryspider_backup`, boot test, commit.
2. **Backup husk repair/reanimate** — Verify artillery spider and
   tarantula backup actors can be repaired and reanimate correctly.
3. **Cyborg hacker death palette break** — `cabal_hackercyborg` death
   animation uses wrong palette.
4. **Rocket cyborg death palette break** — `cabal_rocketcyborg` death
   animation uses wrong palette.
5. **TS GDI building death palette break** — TS GDI buildings show wrong
   palette on death.

## Key files reference

| File | Purpose |
|---|---|
| `mods/cameo/sequences/voxels.yaml` | Voxel model sequence definitions |
| `mods/cameo/sequences/misc.yaml` | Explosion/effect sequences (magicnuke etc.) |
| `mods/cameo/sequences/redalert2.yaml` | RA2 sprite sequences |
| `mods/cameo/sequences/tiberiansun.yaml` | TS sprite sequences |
| `mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/vehicles.yaml` | CABAL vehicle actors |
| `mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml` | CABAL weapon definitions |
| `mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/templates.yaml` | CABAL upgrade templates |
| `mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/upgrades.yaml` | CABAL upgrade actor defs |
| `mods/cameo/rules/tiberiansun.yaml` | Backup/husk actor definitions |
| `mods/cameo/bits/ts/` | TS voxel/sprite assets (original compressed names) |
| `mods/cameo/bits/ra2/` | RA2 sprite assets |
| `docs/design/ROADMAP.md` | Living work queue |
| `docs/backlog_weapon_rename.md` | Weapon rename backlog |
| `docs/design/shattered_paradise_research.md` | SP reference research |
| `docs/design/cabal_rebuild_plan.md` | CABAL faction design plan |

## Helper scripts in repo root (untracked)

- `check_bb_refs.py` — Find old-style `_bb` files still referenced in YAML.
- `check_mk_refs.py` — Find old-style `_mk` files still referenced in YAML.
- `gen_weapon_rename_map.py` — Generate weapon rename map (draft).
- `inventory_weapons.py` — Inventory weapons by actor (draft).
- `rename_old_mk.py` — Rename old-style mk files (draft).
- `tools/rename/rename_map_weapons.yaml` — Generated rename map (draft).
