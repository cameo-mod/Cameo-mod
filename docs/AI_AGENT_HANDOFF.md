# AI Agent Handoff — 2026-07-24 Session

## Current Branch & State

- **Branch:** `fix/ra2-weapons-migration` (branched from `master` at merge commit `4f325e0cf`)
- **Master status:** Clean, pushed, up to date with `origin/master`
- **Previous branch:** `fix/yaml-lint-cleanup` — merged to master. Fixed: missing weapon headers (MachineGun, VenomLaser, Dragon, KirovExplode, RA2BrutePunchE, ra120mmThermobaricTargetingComputer, ra120mm2ThermobaricTargetingComputer, ^RA2RailgunWeapon), invalid -Selectable: removals from bridge actors.

## What This Branch Fixes

### 1. RA2 Weapons Migration (ContentPack vs redalert2.yaml)

**Root cause:** The ContentPack `mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml` was created as a migration of `mods/cameo/weapons/redalert2.yaml`, but only the templates (`^RA2*` prefixed) were migrated. **134 weapon definitions** (RA2CarrierTarget, RA2BrutePunch, RA2OspreyTarget, MigMissiles, V3Launch, etc.) were never copied to the ContentPack. Since `redalert2.yaml` is commented out in `mod.yaml` (line 295: `# cameo|weapons/redalert2.yaml  # migrated to ContentPacks/RedAlert2/Shared`), these weapons are missing from the ruleset, causing `Parent type RA2CarrierTarget not found` errors.

**Fix applied:** Replaced the ContentPack weapons.yaml with a full copy of `redalert2.yaml` (which has all 134 weapons + all templates + correct headers). Then applied the lint fixes from the previous lint commit (d42ad53a1) to the copy:

- Removed `ValidTargets: Ground, Ship` from `^RA2FlakWeapon` Warhead@Effect
- Removed `-TrailInterval:` from `^RA2LightMissile` and `^RA2MediumMissile`
- Added `MaximumLaunchAngle: 200` to `RA2GIRocketsG` (already in redalert2.yaml)
- Removed `Warhead@EffectWater:` from `RA2MirageGun` (already in redalert2.yaml)
- Fixed `TeslaArmorDischargeDummy`: removed `Projectile: LightningZap`, changed `-Image: DRAGON` to `-Image:`, changed `-TrailImage: smokey` to `-TrailImage:`
- Fixed `-Warhead@TeslaArc: FireShrapnel` to `-Warhead@TeslaArc:` (NegativeRemoval)
- Removed `-Warhead@Effect2:` from RA2HornetMissile, MigMissiles, RA2Terrorist, IvanBomb
- Removed `-Projectile:` from RA2SCUD
- Removed `Range: 20000` from YRBoomerSCUD
- Fixed `-Projectile: Missile` to `-Projectile:` in RA2TorpTube (restored `Projectile: Missile` + `Image: Patriot` after the removal line)
- Fixed `-Warhead@Smudge: LeaveSmudge` to `-Warhead@Smudge:` (NegativeRemoval)
- Fixed `-Warhead@EffectAir: CreateEffect` to `-Warhead@EffectAir:` (NegativeRemoval)
- Removed `Warhead@HeavyBomb: SpreadDamage` from RA2Terrorist
- Removed `Range: 1000` from LightningBolt

### 2. Yuri Weapons YAML — Missing Headers (NegativeRemoval bug)

**File:** `mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml`

The lint commit (d42ad53a1) accidentally removed several weapon/warhead headers while doing NegativeRemoval cleanup. The bodies remained but became orphaned, causing YAML parse errors.

**Headers restored:**
- `RA2DiskSteal:` — weapon definition header (line 355)
- `Warhead@Cloud: SpawnSmokeParticle` — in RA2Chemspray (line 848). This was causing the `Sequences` field error because `SpawnSmokeParticle` warhead's child nodes were orphaned under a `-Warhead@Effect:` removal line.
- `Warhead@MediumChemicalWeaponPercentage: HealthPercentageDamage` — in RA2Chemspray (line 844)
- `Warhead@LaserWeapon: SpreadDamage` — in RA2Magnet (line 384)
- `Warhead@FlakWeapon: SpreadDamage` — in RA2Virusgun2 (line 774)
- `Warhead@Smudge: LeaveSmudge` — in RA2CosmonautLaser (line 932)

**Legitimate lint fixes left in place (not restored):**
- `StartBurstReport: vmagatta.wav` removed from RA2Magnet
- `StartBurstReport: flamtnk1.aud` removed from RA2Chemspray
- `ImpactActors: false` removed from RA2PsychicJab Warhead@Effect
- `-Warhead@Smudge:` removal line removed from RA2Magnet (after -Warhead@EffectAir:)
- `-Warhead@Effect2:` removed from RA2LasherToxicMortar
- `-Warhead@HeavyChemicalWeaponFriendlyFire: SpreadDamage` changed to `-Warhead@HeavyChemicalWeaponFriendlyFire:` (NegativeRemoval fix)

### 3. Missing Shader File — postprocess_nuclearflash.frag

**File:** `engine/glsl/postprocess_nuclearflash.frag` (new file, NOT tracked by .gitignore in the mod repo but lives in the engine submodule)

**Root cause:** The custom trait `NuclearFlashRenderer` in `OpenRA.Mods.Cameo/Traits/World/NuclearFlashRenderer.cs` calls `base("nuclearflash", PostProcessPassType.AfterWorld)` which makes `RenderPostProcessPassBase` look for `postprocess_nuclearflash.frag` in `engine/glsl/`. This file was never created, causing a crash when the game tries to load post-processing shaders.

**Fix:** Created the shader file with proper uniforms matching what the C# code sets:
- `LightPosition` (vec2) — screen-space position of the nuclear blast
- `LightRadius` (float) — radius of the flash effect
- `LightColor` (vec3) — color of the flash
- `Brightness` (float) — brightness intensity (decreases over time)
- `Darkness` (float) — darkness amount for surrounding area (decreases over time)
- `SourceTexture` (sampler2D) — the rendered frame buffer (set by RenderPostProcessPassBase)

The shader brightens pixels near the blast center and darkens the surrounding area, with a smooth falloff using a squared clamp.

**NOTE:** This file is in the `engine/` directory which is .gitignored. It must be created manually after `make all` fetches the engine. Consider adding it to a post-fetch script or documenting this requirement.

### 4. Engine VERSION File

The `engine/VERSION` file is not tracked by git. It must contain the same hash as `mod.config`'s `ENGINE_VERSION` (`b1d04ea76a1ee6c3c3f538bc40b6b77c8b6a977a`). If `make all` is run, it sets this correctly. If the file gets corrupted or reverted, `utility.cmd` and the game will fail with "Required engine files not found."

## Remaining Issues (NOT yet fixed on this branch)

### P0: naxiww2kübelwagenmachinegun weapon not found

**Error:** `Actor type naxis_kbelwagen: Weapons Ruleset does not contain an entry 'naxiww2kübelwagenmachinegun'`

**Cause:** The Naxis faction actor `naxis_kbelwagen` references weapon `NaxiWW2KübelwagenMachinegun` (with non-ASCII `ü` character) in `mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/vehicles.yaml` line 152. The weapon definition likely uses a different name or encoding. Need to:
1. Find the weapon definition in Naxis weapons.yaml
2. Check if the name matches exactly (including encoding)
3. Fix the reference or definition to use ASCII-only names (per DESIGN.md naming rules)

### P0: postprocess_nuclearflash.frag not in git

The shader file lives in `engine/glsl/` which is .gitignored. It needs to be either:
1. Added to the engine repo (if we control it), or
2. Created by a post-fetch script in the mod repo, or
3. Documented as a manual step after `make all`

### Pre-existing: RA2 weapons not fully migrated

Even after this branch's fixes, the `mods/cameo/weapons/redalert2.yaml` file is still present but commented out in `mod.yaml`. The ContentPack now has a full copy of its contents. Consider:
1. Removing `redalert2.yaml` from `mod.yaml` comments entirely
2. Eventually deleting `mods/cameo/weapons/redalert2.yaml` once all references are confirmed migrated
3. The ContentPack `content.yaml` still has a `Weapons:` entry pointing to the ContentPack weapons.yaml — this is correct and should remain

## What Needs To Be Done Next

1. **Fix the `naxiww2kübelwagenmachinegun` error** — find and fix the weapon name mismatch
2. **Run `make all`** to ensure engine is synced
3. **Run `utility.cmd cameo --check-yaml`** to find any remaining errors
4. **Fix any remaining errors** until check-yaml passes (0 errors minimum)
5. **Boot-gate test:** Run `launch-game.cmd`, wait for main menu, kill process, check for exception logs
6. **Update documentation:**
   - `docs/design/ROADMAP.md` — mark YAML lint cleanup as complete, add new findings
   - `docs/audit/SUMMARY.md` — update crash-class count
   - `docs/LESSONS_LEARNED.md` — add lessons about NegativeRemoval header removal bug and ContentPack migration
   - `docs/Cameo_Knowledge_Base_Manual.md` — document NuclearFlashRenderer shader requirement
7. **Commit all changes** with descriptive commit message
8. **Push branch, create PR, merge to master**
9. **Switch to master after merge**

## Key Files Modified

- `mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml` — replaced with full redalert2.yaml + lint fixes
- `mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml` — restored 6 missing headers
- `engine/glsl/postprocess_nuclearflash.frag` — new shader file (not in git, must be recreated after engine fetch)

## Key Files NOT Modified (but relevant)

- `mods/cameo/mod.yaml` — `redalert2.yaml` still commented out (line 295). The ContentPack weapons.yaml now has all weapons, so this is correct.
- `mods/cameo/ContentPacks/RedAlert2/Shared/content.yaml` — still has `Weapons:` entry pointing to ContentPack weapons.yaml. This is correct.
- `mods/cameo/weapons/redalert2.yaml` — original file, still present but not loaded. Can be deleted in a future cleanup.

## Binding Rules Reminder

1. **Always boot-gate before committing** — launch game, wait for main menu, check for exception logs
2. **Always update docs before committing** — ROADMAP, SUMMARY, LESSONS_LEARNED, Knowledge Base
3. **Always fetch/pull/merge before committing**
4. **Use PRs, don't push directly to master**
5. **Commit titles must be self-explanatory to ALL developers**
6. **Merge to master when task is complete, don't leave work stranded on feature branches**
7. **`utility.cmd cameo --check-yaml` is NOT a boot-gate substitute** — it's a linting tool only
8. **Run `make all` after engine pin changes** — ensures engine is synced and built
