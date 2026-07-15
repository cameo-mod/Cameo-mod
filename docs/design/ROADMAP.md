# Cameo Roadmap — detailed work queue (rebuilt 2026-07-13)

_The living work queue, resumable by any agent. Rule zero: crashes and
bugs ALWAYS jump the queue. Ordering within a section: **quickest wins
first, then by severity**. Effort: S < 1h, M = one session, L = multi-
session. Every completed item gets its commit hash; every new order
lands here first. Goal: **finish the CABAL faction**, then the dune
factions, everything through the balance workbook._

> **Multi-agent repo.** Three contributors touch this tree: the
> maintainer (AedisToru), **333ggg** (i333ggg@yandex.ru — works Starcraft
> vultures, TS GDI riot troopers, `cabal.xlsx` rows), and **Devin AI**
> (leaves a log at `C:\Users\AedisToru\Documents\DevinCameoProject\
> DEVELOPMENT_LOG.md`). ALWAYS `git add <files>` scoped, never `-A`.
> Verify others' commits before building on them. Devin's 2026-07-12
> sound pass (obelcor3/samshot1 fixes) was reviewed and TRUSTED
> 2026-07-13; keep it. 333ggg's mine commits are self-contained (SC +
> GDI), unrelated to CABAL.

---

## P0 — Crashes (always first)

- [x] Voice-set rename crashes (`1616a26d2`); pink menu (`e956d2280`);
  boot crashes crab-junk/shadowteam/stale-DLL (`28ae47612`). LAW:
  launch-game.cmd to menu before EVERY commit (CLAUDE.md gate).
- [x] **ts_nod_ticktank voxel sequence crash** (`4bfd1bcaf`): `ts_nod_ticktank`
  and `ts_nod_attackcycle` had no `idle:` sequence filename — the voxel files
  are `tsttnk.vxl` and `tsbike.vxl` (old TS names), but the sequence entries
  only had `idle:` with no filename. Fixed by adding `idle: tsttnk` and
  `idle: tsbike` respectively in `voxels.yaml`.
- [x] **magicnuke sequence crash** (`4bfd1bcaf`): CABAL neutron weapons
  (`CabalCommandoPlasmaNeutron`, `CabalCommandoPlasmaMk2Neutron`,
  `CabalRavagerPlasmaNeutron`) had `Image: magicnuke` in their
  `CreateEffect` warheads. The `magicnuke` image has sequences `magicnuke`,
  `magicnuke_med`, `magicnuke_small`, `magicnuke_micro` — but `Image:
  magicnuke` makes the engine look for a sequence named `magicnuke_med`
  inside image `magicnuke`, which doesn't exist (the sequences are defined
  under the `magicnuke` image key with those names). Removing `Image:
  magicnuke` lets the engine use the `Explosions:` field directly against
  the sequence set. The `CabalMagicNuke` weapon (line ~1847) already
  worked correctly because it only had `Explosions: magicnuke` without
  `Image:`.
- [x] **ra2_cgtbnkbb.shp not found crash** (`4bfd1bcaf`): Asset was renamed
  to `ra2_cgtbnkbib.shp` (bb→bib convention) but YAML references in
  `redalert2.yaml` were not updated. Fixed all 3 references.
- [x] **ra2_ctoutpbb.shp not found** (`4bfd1bcaf`): Renamed to
  `ra2_ctoutp_bib.shp`, updated 4 YAML references in `redalert2.yaml`.
- [x] **tamrefbb.shp reference** (`4bfd1bcaf`): Renamed to `tamref_bib.shp`,
  updated reference in Forgotten `sequences.yaml`.
- [x] **mk→make asset renames** (`4bfd1bcaf`): 8 construction animation
  files renamed from `_mk.shp` to `_make.shp` (ra2_cgoildmk, ra2_ntyardmk,
  tambarmk, tampowrmk, tamradrmk, tamrefmk, tamtechmk, tsnttmplmk) with
  all YAML references updated.
- [x] **Weapon rename task backlogged** (`4bfd1bcaf`): Full research and
  tooling documented in `docs/backlog_weapon_rename.md` for future
  continuation.
- [x] **CABAL Orb Drone carrier-slave crash** (`ec63784bd`):
  `cabal_orb_drone` had `CarrierSlave`+`HasParent` traits while also being
  buildable from the cyborg factory. When built independently, no master is
  linked, causing `NullReferenceException` in `CarrierSlave.EnterSpawner`.
  Split into `cabal_orb_drone` (standalone, no slave traits) and
  `cabal_orb_drone_slave` (non-buildable, inherits base + CarrierSlave).
  Updated `CarrierMaster` on `cabal_hunter_drone_carrier` to spawn the slave.
  Pattern follows RA1 Japan `zerofighter`/`japancarrier`.
- [x] **RA2 corpse death_d crash** (`ac3ba04b7`): `RA2CorpseSpawner` and
  `RA2FlyingBody` CreateEffect warheads lost `Image: ra2corpse` during CE2
  cleanup, causing engine to look for `death_a`-`death_f` sequences in the
  default `explosion` image where they don't exist. Restored `Image: ra2corpse`
  per corpse-spawner exception in DESIGN.md §8.

### P0 — Completed (2026-07-14 session)

- [x] **CABAL Backup Systems upgrade coverage (legion, avatar)**
  (`d4be72f8f`): Added `SpawnActorOnDeath@backup` to `cabal_legion` and
  `cabal_avatar`; added `Inherits@BACKUP` to `cabal_avatar`; created
  `cabal_legion_backup` and `cabal_avatar_backup` actors in
  `rules/tiberiansun.yaml`; added `Repairable` trait to
  `cabal_artilleryspider_backup`.
- [x] **Backup husk repair/reanimate** (`d4be72f8f`): `Repairable` trait
  added to `cabal_artilleryspider_backup` (was missing — present on
  manticore and tarantula backups already).
- [x] **CABAL infantry death palette break** (`a2b4de333`): All 8 CABAL
  infantry actors and the `^TSInfantry` template had `WithDeathAnimation`
  with `PlayerPalette: playerra2` but no `DeathSequencePalette`. The
  `DeathSequencePalette` field controls which palette the death sequence
  frames render with; without it, the engine defaults to a non-player
  palette, causing visible color breakage on death. Fixed by adding
  `DeathSequencePalette: ra2player` to `^TSInfantry` template and all 8
  CABAL infantry overrides (cyborginfantry, rocketcyborg, devout,
  ascended, hackercyborg, cyborgcommando, cyborgcommandov2,
  eliminator800).
- [x] **TS GDI building death palette break** (`b417c6f96`): The
  `^BaseBuilding` template in `defaults.yaml` had `WithDeathAnimation`
  with `DeathSequence: dead` but no `DeathSequencePalette` — same root
  cause as the infantry palette bug. Fixed by adding
  `DeathSequencePalette: ra2player` to `^BaseBuilding` and to the
  `WithDeathAnimation@BIB` overrides on GDI and CABAL service depots.

---

## CABAL — recently completed (this push)

- [x] Confident quick fixes: missile arc, HK mk1 blue laser, Core
  Defender offset, Mantis sound (`87a716b41`).
- [x] Crab → **Ravager** infantry plasma line-breaker + plasma bullet
  effect (`e4ac0ce40`, `b31113a6d`). Crab id retired.
- [x] CABAL weapons get their own firing sounds (`1281a71f5`).
- [x] Rocket-launcher offsets/counts + Manticore dual laser (`c4691e758`).
- [x] Mantis + Laser Spider → AttackFrontal fire support (`cc6a290db`).
- [x] Dissolver: cloak → corrosion (`corroded` cond) + TankDestroyer +
  LightChemical combo + new `cabal_dissolveimpact` effect (`de25b469d`);
  effect re-rendered to fit its frame (`45b8f0caa`).
- [x] Eliminator 800: real `^GatlingSpeedUpUnitBehavior` spin-up (drop
  the AmmoPool hack), single ground + Air-only twin, dune autogun muzzle
  @3671 (`33c13a553`).
- [x] All CABAL infantry: vehicle-style turn rate 2×Speed/5 (`f98bf8155`).
- [x] Devin sound pass (uncommitted, verified, keep): DarkObeliskLaser /
  CabalCommandoPlasma / Mk2 → obelcor3.aud; Reaper/TwinBazooka/rocket
  weapons → samshot1.aud; Core Defender offset raise; magicnuke Tick tune.
- [x] Effect-naming: CABAL authored weapons already clean. `TS90mm_bluenuke`
  `@3Eff` is NOT a violation — it overrides `^TSCannonEffect`'s own
  `@3Eff`. Mod-wide sweep still pending (CE).

---

## CABAL — new orders 2026-07-13 (the big batch)

### N1. Green-plasma / neutron-shell gating (`7a0d0025d`)
- [x] New art: `cabal_greenplasma.png` (weak green plasma projectile) +
  `cabal_greenplasmaimpact.png` (green impact burst), both border-safe
  RGBA PngSheets.
- [x] **Neutron-shell gates every magicnuke weapon.** Non-upgraded
  (`!cabal_upgrade_neutronnuclearcatalyst`) = green plasma projectile +
  green impact; upgraded = the blue magicnuke. Pattern already on
  Artillery Spider + Tarantula (basic armament `!cond`, `Armament@Upgraded`
  `cond`); extend the same split to Cyborg Commando, Commando Mk2, and
  the Ravager. Consider updating the upgrade description (it now empowers
  the whole plasma line, not just Artillery+Tarantula).
- [x] **Magicnuke sizes scaled to power, all 4 used** (`magicnuke_micro`
  0.2 < `_small` 0.25 < `_med` 0.5 < `magicnuke` 1.0):
  - micro → TS90mm_bluenuke (~12k)
  - small → TS120mm_bluenuke (Tarantula, ~24k), CabalRavagerPlasma (~32k)
  - med   → Commando plasma (~50k), TS155mm_bluenuke (Artillery, ~60k)
  - **magicnuke (biggest) → the new CABAL superweapon ONLY** (below).
- [x] **Artillery Spider projectile rework** (`901a9018f`): Archer/Specter-style
  ballistic shell with visible blue contrail; upgraded version uses CABAL
  purple → dark-blue thicker contrail and adds Tesla/Magic/Railgun/Chemical
  warheads. Spreadsheet synced.

### N2. CABAL superweapon (biggest magicnuke) (`1f8b58820`)
- [x] New nuke support power, **same values as the Ixian EMP Nuke**
  (`supercomputer.ixian` `NukePowerCA` firing `PulseMissile`:
  ChargeInterval 10500, MissileWeapons PulseMissile, MissileDelay 25,
  CameraRange/CircleRanges 10000, etc.) but with the **biggest magicnuke**
  as the missile/impact animation (+ a new sound, see S-rules).
- [x] **Fired from the CABAL Core**, using **TD Nod Temple of Nod logic**,
  **plus an add-on that adds the missile silo**. (Find the Temple-of-Nod
  NukePower pattern; the "add-on = missile silo" is a prerequisite
  building/attachment that unlocks or houses the silo.)

### N3. CABAL Core = money structure (`7a0d0025d`)
- [x] Turn the CABAL Core into a **special money-generator structure like
  the Asian Military Academy**: **double the income of the Oil Derrick**,
  and it **also counts as an Oil Derrick** (provides that prerequisite /
  captured-tech behavior). It also launches the N2 superweapon.

### N4. Commando plasma weapons + CABAL Obelisk plasma-laser (high-impact + warhead combos)
- [x] DarkObeliskLaser, CabalCommandoPlasma, CabalCommandoPlasmaMk2: keep
  **obelcor3.aud** (do NOT change the sound). All three already use **long
  ReloadDelay + heavy Damage**.
- [x] The **two Commando plasma weapons** already carry the large-AoE triad:
  base = **Cannon + Flame + Chemical**; on the **neutron-shell upgrade**
  they add **Tesla + Magic + Railgun** warheads.
- [x] **CABAL Heavy Obelisk** (`TSCABALObeliskLaserFire`) made unique from
  TS Nod Obelisk: converted to **plasma-laser** = **Laser + Flame + Chemical**
  with matching percentage twins; removed inherited TS Nod upgrade armament;
  paired `cabal_laserimpact_l` effect + `obelmod1.aud`/`drtelectro.wav` sound.
- [x] Warhead audit pass: fixed `CabalMagicNuke`/`TS90mm_bluenuke` effect
  warhead naming, duplicate `Warhead@1Dam` in `TSCyCannon`, and incorrect
  `HealthPercentageDamage` twin on `TSHunterKillerLasers`.

### N5. Laser beam visual rework (DESIGN law — see below) (`6f43f5639`)
- [x] Every CABAL laser: **two beam colors** (inner + outer), a **mix of
  purple + dark blue**, **not too thin**. Beam **width scales with
  damage** (Mantis + all others currently too thin; Core Defender a touch
  too thick but must still scale). **Color also scales with damage**
  (scale BOTH colors so bigger damage looks more dangerous).
- [x] **Laser Spider → obelmod1.aud** (TS Obelisk sound) — FIX from the
  obelray1.aud I set. Smaller lasers → **laser turret sounds** (lastur1.aud).
- [x] **Manticore double laser**: too thin → **spread the two beams out
  more**; rebalance with **more range + more armor** (range/armor deferred to
  balance sheet per DESIGN §3).
- [x] **3 levels of laser ground-impact effect** (purple/blue, scaled by
  damage), applied to ALL laser weapons; each needs a new sound.

### N6. New CABAL effects + sounds
- [x] Audio audit: all CABAL weapons have Report + ImpactSounds (via
  inheritance or direct). Only CabalOverkillDroneLauncher was missing
  a Report — fixed (`5437d4f63`).
- [x] Effect-warhead naming: CABAL had 1 violation (CabalBerserkerBlades
  @3Eff -> @Effect) — fixed (`63c859fde`).
- [ ] New explosion effect for ALL CABAL missiles (+ new sound) — needs
  custom art/audio from maintainer.
- [ ] Plasma-weapon sounds: prefer NEW/unique; cross-check Shattered
  Paradise references. (Cannot synthesize quality .wav here — assign
  unique existing mod sounds and flag any that truly need new custom
  audio for the maintainer to source.)

### N7. Weapon-mount offsets (`7a0d0025d`)
- [x] **Ascended + Devout**: increase the **second (Y) value** of each
  triple offset ~**2×** so their weapons sit further left/right.

### N8. Armor combo (was CC; DONE)
- [x] Cyborg Commando + V2: Heroic/Superheavy dual-armor applied.
- [x] Eliminator 800: Flak/Heavy dual-armor applied.
- [x] Berserker: Heroic/Superheavy via `^HeroInfantryTemplate` + `^TSCyborgDualArmorHeavy`.
- [x] All 11 CABAL infantry verified: every unit has Armor@Secondary +
  DamageMultiplier@Secondary: 200 (some via `^TSCyborgDualArmor*` templates).

### N9. Role + tier + promotion rebalance (L, sheet-first) — MOSTLY DONE
- [x] **3×4 promotion grid fully populated**: Devout, Ascended, Beholder,
  CCV2 (infantry); Spider CNC4, Heavy Reaper, Widow, Core Defender
  (vehicles); Wasp Striker, Super Hunter Killer, Overkill Fortress,
  Mothership (aircraft).
- [x] **T1000 removed**; Beholder moved from Consortium to CABAL.
- [x] **All Omega variants removed** (HK2 Omega, Mothership Omega).
- [x] **Berserker refactored** to hero infantry (`^HeroInfantryTemplate`),
  T4, HP 800k, DPS 7500, cost 10000, from Cyborg Factory, requires Core.
- [x] **Overkill Fortress rebuilt** as Farasha-style carrier with drones.
- [x] **HK1 + Super Hunter Killer**: dual rockets + dual lasers.
- [x] **Carryall renamed**, unarmed transport.
- [x] **Spreadsheet synced**: 35 rows, all TechTier/UnitClass/Special
  values legal per DESIGN.md (1.0/0.75/0.5, epic=1.0/0.3), obsolete rows
  deleted, missing units added, names updated.
- [x] **Husk names fixed** (Carryall, Hunter Killer, Overkill Fortress,
  Overkill Drone).
- [x] **Design doc updated** (CABAL_FACTION_DESIGN.md reflects all changes).
- [x] **Template role audit**: fixed Engineer→^MechanicTemplate,
  Eliminator 800→^HeavyInfantryTemplate, Carryall→^UnarmedTransportHelicopterTemplate,
  Scarab APC→^SupportVehicleTemplate + ^CargoVehicle (`81bad88d2`).
- [x] **Balance formula audit**: all 30 CABAL units [OK] — 0 ABSURD, 0 HIGH,
  0 formula-broken. Fixed 7 problem units (Legion, Mothership, RocketCyborg,
  Wasp, WaspStriker, Ascended, Beholder) + dissolver crash (missing crippled
  sequences + wrong icon palette) (`50f3db5e4`); fixed 3 formula-broken
  workbook rows 27-29 (`160a6491a`).
- [x] **Repair Drone** added as buildable support aircraft (`94a58b2a7`);
  spreadsheet row added, icon uses carrier icon placeholder.
- [x] **Open question**: Overkill Fortress vs Overkill Carrier final name.

### N10. Upgrades audit
- [x] Reviewed every CABAL upgrade for meaningful consumption. Removed the
  meaningless `cabal_upgrade_clusterwarhead` (no actor, building, or template
  consumed it; also removed its Fluent description and AI entry).
  All other upgrades are wired: conditions granted by templates are
  inherited and used by at least one actor or support power. Kept the
  neutron-shell twins untouched.

### N11. Descriptions + AI
- [x] All CABAL units have Fluent descriptions (converted 8 inline \n
  descriptions to Fluent keys per DESIGN.md §7, `1f580f6e0`; plus 2 more
  fixed: cabal_refinery + cabal_mobileconstructionvehicle).
- [x] AI wiring: all CABAL units in UnitsToBuild list with weights.
  cabal_engineer added to CapturingActorTypes; stale tscyc2.cabal removed.
- [x] CABAL added to global Random + RandomTournament faction pools;
  "(WIP)" suffix removed from faction name.
- [x] Fluent key naming fixed: actor-cabal_core/actor-cabal_techcenter
  → underscores (actor_cabal_core/actor_cabal_techcenter).
- [x] Building name capitalization fixed: "Cabal Tech Center" → "CABAL
  Tech Center", "Heavy Cabal Obelisk" → "Heavy CABAL Obelisk".
- [x] Manticore description updated: removed trap net references (trap
  weapon removed from unit).

### CE (carried). Effect-warhead naming sweep, mod-wide
- [x] CABAL: 1 violation fixed (CabalBerserkerBlades @3Eff -> @Effect,
  `63c859fde`). CABAL is fully compliant.
- [x] Mod-wide: 202 renames across 40 files via scripted sweep
  (`2ad0f35e1`). Audit: `tools/audit/audit_effect_warhead_names.py`
  (0 violations). Template override names preserved; suffixed variants
  (@Effect2, @EffectAir2, etc.) recognized as canonical.

### CE2. CreateEffect Image field audit + explosion sequence consolidation
- [x] **CABAL CreateEffect Image: removal** (2026-07-15): Removed explicit
  `Image:` fields from all CABAL `CreateEffect` warheads in
  `CABAL/yaml/weapons.yaml`. All impact animations now use the default
  `explosion` image (engine default when `Image:` is omitted).
- [x] **CABAL impact animations moved to misc.yaml** (2026-07-15):
  `cabal_greenplasmaimpact`, `cabal_missileexplosion`,
  `cabal_laserimpact_s`, `cabal_laserimpact_m`, `cabal_laserimpact_l`,
  `cabal_dissolveimpact` moved from `CABAL/yaml/sequences.yaml` to
  `sequences/misc.yaml` under the `explosion:` key. Removed the old
  top-level definitions from the CABAL sequences file.
- [x] **Mod-wide CE-only Image: fixes** (2026-07-15): Moved CE-only
  image `wc2_building_collapse` under `explosion:` in misc.yaml; removed
  `Image:` from 7 CE warheads in `weapons/warcraft2.yaml`. Removed
  redundant `Image: explosion` from `weapons/halloween.yaml`. Shared
  images (used by both CE and other traits) keep their `Image:` field
  per the shared-image exception in DESIGN.md §8. `ra2corpse` reverted —
  corpse spawner needs `Image:` for random-pick from its own
  sub-sequences (corpse-spawner exception, DESIGN.md §8).
- [x] **DESIGN.md updated** (2026-07-15): Added rules to §8 documenting
  that `CreateEffect` must never carry `Image:` (CE-only), the
  shared-image exception, and that all impact animations must live in
  `misc.yaml` under `explosion:`.
- [x] **Audit tooling** (2026-07-15): `tools/audit_createeffect_image.py`
  flags all CE `Image:` fields; `tools/audit_ce_image_usage.py`
  classifies CE-only vs shared.
- [ ] **Future**: If a shared image's non-CE references are ever removed,
  it becomes CE-only and should be moved under `explosion:` at that time.

### CE3. Map actor renaming (delivery + deliverycoop)
- [x] **Actor rename in new maps** (2026-07-15): Commit
  `e6ad4ded5fa08c6b41fde63a256f2f5c15917241` added new maps
  (`delivery/map.yaml`, `deliverycoop/map.yaml`) with old compressed
  actor names. All 2257 actor references in both map.yaml files and 90
  string references in lua scripts renamed to new §1-compliant ids using
  `tools/rename_map_actors.py` with the `tools/rename/rename_map_*.yaml`
  mapping files. Terrain decorations (t01, v01, boxes01, brik, etc.) left
  as-is since they still exist with those names.
- [x] **DESIGN.md updated** (2026-07-15): Added §14 documenting map actor
  naming rules and the rename procedure.

---

## Dune factions (D2K) — split + naming + upgrades (P2)

- [x] **Split dune Light Infantry + Rocket Trooper per faction** (neutral
  base template → per-faction Ixian/Ordos actors) so upgrades apply
  separately (`b180aef36`).
- [x] **Ordos Light Infantry gets Laser Cartridges** once it's its own actor
  (`b180aef36`).
- [x] **Rename Ordos "Armor-Piercing Rounds" → "Rapid Fire Armor-Piercing
  Belts"** (actor id, template, condition, sequence, icon — full rename)
  (`b180aef36`).
- [x] No-hyphen naming scheme across all dune factions.
  Verified 2026-07-14: no hyphenated actor IDs, weapon IDs, or asset
  references in any D2k ContentPack yaml. All hyphens found are
  engine-defined conditions/sequence names (build-incomplete, damaged-idle,
  etc.) which are engine-owned and stay as-is per DESIGN.md §1.
- Note: 7 Ordos armor-rework files are the maintainer's live WIP — leave.

---

## Content-pack folder restructure (P2/P3, L)

- [x] Every content pack: `content.yaml` at root + one **`yaml`** folder
  (rules+weapons+sequences merged) + an empty **`files`** folder. Shared
  assets → per-GAME `Shared/files/`. DONE 2026-07-14: all packs
  restructured, boot-tested, committed.
- [x] **`mod.yaml` package hierarchy** (2026-07-15): per-faction
  `files/` packages are mounted first, then per-game `Shared/files/`,
  then top-level `ContentPacks/Shared/files/`, then legacy `bits/`. This
  lets new content shadow old content without breaking old cameo fallback.
- [x] **CABAL asset migration** (2026-07-15, `68cdd5ebb`/`472209150`): 128
  CABAL-unique assets moved into
  `ContentPacks/TiberianSun/CABAL/files/{icons,sprites,voxels}` and
  referenced with package prefixes.
- [x] **Cross-game shared asset migration** (2026-07-15, `e1b153d9c`/`472209150`):
  38 single-file cross-game shared assets moved into
  `ContentPacks/Shared/files/sprites/` and referenced with
  `shared_sprites|<name>` across all affected ContentPacks.
- [x] **TiberianSun intra-game shared asset migration** (2026-07-15,
  `6835a04`): 21 TS-only shared assets moved into
  `ContentPacks/TiberianSun/Shared/files/{icons,sprites,voxels}` and
  referenced with `ts_shared_*|<name>` prefixes.
- [ ] **Remaining critical cross-game shared assets**: `gunfire2`
  (generic/RA/TD variants), `electro` (7 tileset variants), `dragon`
  (RA sprite vs WC2 sound name collision), and `d2k/DATA.R16` (resource
  package). These must be resolved before `bits/` can be deprecated.
  → active work: cross-game sharing is a release blocker for dynamic
  faction loading, so this jumps the queue within the content-pack section.
- [ ] **AI module split**: per-faction `ai.yaml` is currently blocked by
  OpenRA's YAML merge behavior (trait instances with the same `@name`
  are replaced, not deep-merged). Needs custom trait or engine change.
  → backlog until architecture is designed.
- [ ] **Unused-file audit**: once all referenced assets are out of `bits/`,
  run an audit to identify and delete the ~25,000 unreferenced legacy
  files left in `bits/`.

## Cross-faction shared-effect independence (LONG-TERM, L)

- [x] Top-level `ContentPacks/Shared/files/` created as a temporary
  holding area for cross-game assets (2026-07-15).
- [ ] Duplicate or replace every cross-game shared asset so each game
  owns its own copy, then remove the top-level `Shared/files/` entries.
- [ ] Give each faction its own effects, or share only PER GAME. Prereq
  for true dynamic per-faction loading. DESIGN + MIGRATION.

---

## Phase B — CABAL effects & art polish
- SP-recipe projectiles/contrails (art our own); dark-blue/purple identity;
  promotion icons for placeholders; SP-like reports from TS material.

## Phase C — Balance & consistency (other factions)
- Infantry offset sweep beyond TS; TS rocket launch-angle sweep beyond
  CABAL; clean workbook (port CABAL rows); 165 sheet↔game mismatches;
  [x] FutureTech .futu→futuretech_ rename — 32 asset files renamed, 8
  YAML/FTL files updated (voxels, sequences, ContentPack rules, Fluent).
  Soviet Gorynych/Stalin Fist.

## Phase D — SP-ification of the other TS factions (after CABAL)
- TS GDI, Nod, Forgotten, then Scrin — SP-recipe weapons/effects, workbook stats.

## Phase E — Platform & engine (background, L)
- [x] **Port `AttackGarrisonedSP`** (one fire port per passenger) + convert all
  `AttackGarrisoned`/`AttackOpenTopped` units to per-passenger independent
  targeting. New `AttackGarrisonedSP` trait in `OpenRA.Mods.CA/Traits/Attack/`
  inherits `AttackFollow`, supports both `Cargo`/`Passengers` and
  `Garrisonable`/`Garrisoners`, and adds per-passenger opportunity fire via
  each passenger's `AutoTarget` trait. All 26 YAML usages across rules +
  ContentPacks converted from `AttackGarrisoned`/`AttackOpenTopped` to
  `AttackGarrisonedSP`. `PortYaws`/`PortCones` made optional (default 360°).
  **REVERTED** (`cfa117c78`): AttackGarrisonedSP caused a major regression —
  garrisoned passengers could no longer independently auto-target because
  passenger AutoTarget traits don't function while inside cargo. All 56 YAML
  trait renames reverted to vanilla `AttackGarrisoned`/`AttackOpenTopped`.
  The C# source file is kept for future reference but unreferenced.
- SP engine-trait ports; TS Shared pack move; Formula v2; dynamic faction
  loading end-game (per-pack ai.yaml, assets into packs, unused-file audit).

---

## Standing rules recorded (see DESIGN.md / memory)

- **CreateEffect Image: field** (DESIGN §8, 2026-07-15): a weapon
  `CreateEffect` must NEVER carry an `Image:` field — omit it and the
  engine defaults to the `explosion` image in `misc.yaml`. All impact
  animations live as sub-sequences under `explosion:` in
  `sequences/misc.yaml`, never in faction sequence files.
- **Map actor naming** (DESIGN §14, 2026-07-15): maps must use renamed
  actor ids, not old compressed names. Rename maps in
  `tools/rename/rename_map_*.yaml` are the source of truth. Lua scripts
  must also be updated. Tool: `tools/rename_map_actors.py`.
- **No weapon inheritance between units** (DESIGN §15, reinforced
  2026-07-15): unit-unique weapons must never `Inherits:` from another
  unit's weapon. Copy stats or use a shared `^`-prefixed template. This
  was the root cause of the CreateEffect crash class.
- **CABAL Avatar = 50% Core Defender** (DESIGN §15, 2026-07-15): the
  avatar is a 50%-scaled copy of the Core Defender, not a spider.
- **CABAL husk recovery** (DESIGN §15, 2026-07-15): backup husks are
  immobile, high-HP, repairable, auto-reanimate via
  GrantPeriodicCondition + TransformOnCondition.
- **Effect + sound are always defined together** (DESIGN §8): every new
  impact/projectile effect gets BOTH a new effect sprite AND a new Report/
  ImpactSound — never fall back to the template's default for either.
  Unique-per-faction is the goal.
- **Effect frame-fit**: every rendered effect must sit INSIDE its frame
  (2px border alpha 0) or it clips to a square. Verify with a bordered
  preview. (memory: cameo-custom-effects-pngsheet)
- **Laser beams (DESIGN §3)**: two colors (inner+outer), width AND color
  scale with damage; CABAL = purple + dark blue, never too thin.
- **Obelisk/laser sound map (DESIGN §3)**: obelmod1.aud = TS Obelisk of
  Light / Obelisk of Darkness / CABAL Obelisk; obelcor3.aud = Core
  Defender + DarkObeliskLaser + Commando plasma; obelray1.aud = Tiberian
  DAWN obelisk — NOT allowed on TS units unless specified (SP `^LaserWeapon`
  inherit = the TD version); smaller lasers = lastur1.aud turret sounds.
- **Effect-warhead naming**: one `CreateEffect` per impact surface.
- **Per-frame randomness** on new animated effects.
- **Content-pack structure**: yaml folder + files folder + content.yaml.

### Backlog — Rank decorations & elite weapons (DESIGN §16, 2026-07-15)

- [ ] **Fix TS Nod rank decoration** — 13 TS Nod actors were using
  `^GDIRankDecoration` instead of `^NodRankDecoration`. FIXED in this
  session. Also fixed 4 TS Forgotten actors in `defenses.yaml` and 2
  core `tiberiansun.yaml` Nod units (`ts_nod_attackcycle`,
  `ts_nod_ticktank`).
- [ ] **Wire D2k factions to `^DuneRankDecoration`** — template created
  in `ContentPacks/D2k/Shared/yaml/templates.yaml` but D2k actors not
  yet given `Inherits@decoration: ^DuneRankDecoration`. 6 Ordos actors
  confirmed missing; Ixian/Atreides/Harkonnen need checking.
- [ ] **Create `^AlienRankDecoration` template** — `alienrank` sequence
  exists in `misc.yaml` but no template references it. Determine which
  factions should use it (potentially StarCraft Zerg if they gain
  experience in future, or other alien-themed factions).
- [ ] **Create per-faction rank decorations for RA2Mod factions** —
  currently all RA2Mod factions share `ra2rank` via
  `^GainsExperienceRA2`. Eventually each could have a unique rank image
  for faction identity (low priority — shared `ra2rank` is functional).
- [ ] **Write `audit_rank_decoration.py`** — verify every
  `^GainsExperienceTD` actor has the correct `^*RankDecoration` for its
  faction. Verify `^GainsExperienceRA2` actors do NOT have a separate
  `^*RankDecoration`. Check that rank image sequences exist in
  `misc.yaml`.
- [ ] **E1: Add missing elite weapons** — 217 RA2-styled actors are
  missing `Armament@ELITE` blocks. Each needs a base weapon `E`-suffixed
  variant with `RequiresCondition: rank-elite`. This is a large batch
  job (design work — each elite weapon needs unique stats, not a
  mechanical rename).
- [x] **E2: Fix missing `rank-elite` conditions** (`ac3ba04b7`) — Only 2
  genuine bugs found (out of 18 flagged; rest use Generals `scrap_create_bonus`
  rank system or upgrade-switch naming). Fixed:
  `asian_alliance_plasmatrooper` GARRISONEDELITE and
  `asian_alliance_heavyrailguntank` ELITE. Added audit tool
  `tools/audit/audit_elite_gating.py`.
- [ ] **E3: Normalize elite weapon naming** — 17 elite weapons use
  non-standard names (e.g. `AsianRailTank2`, `NaxPlanegun`,
  `SteelMegaSwordEMP`). Rename to `<baseWeapon>E` convention. Also
  normalize 31 `_elite`-suffixed weapons to `E` suffix when touched.
- [x] **E4: Verify base weapon gating** (`ac3ba04b7`) — Fixed the 2 actors
  from E2: added `RequiresCondition: !rank-elite` to
  `asian_alliance_heavyrailguntank` PRIMARY and
  `asian_alliance_plasmatrooper` GARRISONED so elite replaces, not stacks.

## D2K Sprite Conversion Pipeline

- [x] **D2K-CONV: Conversion script** — `tools/d2k_to_openra.py` written
  and documented in DESIGN.md §17. Combines BMP frames → PNG spritesheet,
  pink→transparent, hue-shift green player color to target hue, embeds
  FrameAmount/FrameSize PNG metadata for OpenRA.
- [x] **D2K-KODA: Koda Tank** — replaced `combat_tank.ixian` with
  `ixian_koda_tank` using new PNG spritesheets (chassis + turret).
  Updated all references in Ixian/Ordos faction.yaml, upgrades.yaml,
  ai.yaml. Muzzle flash still uses DATA.R16. Pending in-game visual
  confirmation.
- [ ] **D2K-CONV-FUTURE: Convert more D2K units** — other D2K units that
  could benefit from custom PNG sprites instead of DATA.R16 remapping.
  Use the same script with appropriate `--hue` per faction.

## Schwarzer Mond Faction Design & Upgrades

- [x] **SM-RESEARCH: Finalize promotion intent** — promotions will upgrade
  existing units via `^PromotionUnitBuff` rather than unlocking new actor
  variants. The `Bradley` unit in the promotion image is resolved as the MARS
  hover artillery (`schwarzer_mond_mars`). Added the buff to all combat
  infantry, vehicles, and aircraft. Updated DESIGN.md §18.7 / §18.11.
- [x] **SM-UPGRADE-1: Add upgrade templates** — create `^NaxiCryptofascism`,
  `^NaxiLunarAlloys`, `^NaxiMoonPropaganda` in the appropriate Shared or
  Schwarzer Mond templates file. Update DESIGN.md §18.6 if the template set
  changes.
- [x] **SM-UPGRADE-2: Split laser upgrade** — turn Crystal Lens into a +1-burst
  radar-tier upgrade for all yellow laser weapons; add Amplified Lens as the
  tech-tier +1-burst upgrade for all yellow laser weapons. Update all weapon
  variants and actor armament conditions per DESIGN.md §18.4.
- [x] **SM-UPGRADE-3: Move cannon upgrade to tech tier / rename to Vril Powered
  Weapons** — change `schwarzer_mond_upgrade_vrilpoweredweapons` prerequisite
  from radar to `~schwarzer_mond_techcenter`, keep it in the `Research` queue,
  and rename the display name/template/icon from Green Plasma Shells to Vril
  Powered Weapons.
- [x] **SM-UPGRADE-4: Add Cryptofascism upgrade** — create
  `schwarzer_mond_upgrade_cryptofascism` (tech tier, Research queue) with
  `CashTrickler` 1 credit per 25 ticks per unit. Add icon sequence for
  `nax2_cryptofascismicon.png` in `mods/cameo/bits/ra2/mod/`. Inherit on
  every Schwarzer Mond actor.
- [x] **SM-UPGRADE-5: Wire upgrades to every unit** — ensure every Schwarzer
  Mond actor has at least two relevant upgrade hooks (Cryptofascism + either
  Lunar Alloys, Crystal Lens, Vril Powered Weapons, Moon Propaganda, or
  Helium-3). Do not change unit stats without a spreadsheet pass.
- [x] **SM-DESC: Normalize faction and unit descriptions** — rewrite the
  Schwarzer Mond `faction_ra2_lnaxis` description in the point-based format
  (Difficulty, Early/Mid/Late Game, Playstyle, etc.) and add/update unit
  descriptions for new upgrades. Normalize other RA2Mod factions when touched.
- [x] **SM-LORE: Add Iron Sky / Nazi Moon lore** — document Vril, Helium-3,
  MoonCoin/Reichsmark 2.0 parody in DESIGN.md §18.12 and update upgrade names
  and descriptions to match.
- [x] **SM-HELIUM3: Add Helium-3 Enrichment upgrade** — create
  `schwarzer_mond_upgrade_helium3` (radar tier, Upgrades queue) that increases
  Hydrogen Plant power output by 50% and vehicle/aircraft speed by 25%. Add
  template, icon, and sequence; wire to all vehicles and aircraft.
- [x] **SM-VRILINFUSION: Add Vril Infusion upgrade** — create
  `schwarzer_mond_upgrade_vrilinfusion` (tech tier, Research queue) that gives
  all Schwarzer Mond infantry +25% firepower, +25% speed/turn rate, and 15%
  damage reduction. Add template, icon, sequence, and wire to every infantry
  actor. Update descriptions and intent.
- [x] **SM-1BURST: Re-enable laser upgrades on 1-burst weapons** — add Lunar
  Soldier and Laser Tower to the Crystal Lens / Amplified Lens switch and
  recreate the 1-burst yellow/amplified weapon variants.
- [x] **SM-AUDIT: Run audit suite and rebuild** — audit suite run
  2026-07-15. Schwarzer Mond upgrades: cryptofascism 26/27, lunaralloys
  26/27, moonpropaganda 5/5, vrilinfusion 5/5 (only uncovered: tsprobe
  shared unit). No orphaned SM actors/weapons. No faction leaks. Game
  boots to menu clean.
- [ ] **SM-BALANCE: Spreadsheet pass** — if any base stats change (e.g.
  raising base burst of Lunar Soldier or Laser Tower), update
  `docs/design/cameo_armor_system.xlsx` and the yaml in the same pass.
  Queue if the Excel lock file is present.
- [x] **SM-ARTWORK: Replace copy-pasted icons** — create unique placeholder
  icons for `schwarzer_mond_mars`, `schwarzer_mond_m200bjagerline`,
  `schwarzer_mond_gravitycoretank`, and `schwarzer_mond_blackbomb`. See
  `docs/design/schwarzer_mond_artwork_status.md` for the full status. Final
  production-quality cameo art can replace the placeholders later.
