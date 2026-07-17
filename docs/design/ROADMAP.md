# Cameo Roadmap — detailed work queue (rebuilt 2026-07-13)

_The living work queue, resumable by any agent. Rule zero: crashes and
bugs ALWAYS jump the queue. Ordering within a section: **quickest wins
first, then by severity**. Effort: S < 1h, M = one session, L = multi-
session. Every completed item gets its commit hash; every new order
lands here first. Goal: **finish the CABAL faction**, then the dune
factions, everything through the balance workbook. Faction reference:
[FACTIONS.md](../FACTIONS.md)._

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

- [x] **CABAL Backup Systems upgrade coverage (avatar, widow)**
  (`d4be72f8f`): Added `SpawnActorOnDeath@backup` to `cabal_avatar` and
  `cabal_widow`; added `Inherits@BACKUP` to `cabal_avatar`; created
  `cabal_avatar_backup` and `cabal_widow_backup` actors in
  `rules/tiberiansun.yaml`; added `Repairable` trait to
  `cabal_artilleryspider_backup`.
  **NOTE (2026-07-16):** The original session plan referenced `cabal_legion`
  and `cabal_legion_backup`, but no `cabal_legion` actor exists in the
  current tree (it was likely renamed or removed during the N9 rebalance).
  `cabal_widow_backup` was created instead. If a `cabal_legion` actor is
  re-added later, it will need its own backup actor.
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
- [x] **TD building death palette fix** (`d72194748`): `^BaseBuilding`
  template sets `DeathSequencePalette: ra2player` globally, but TD
  buildings use `PlayerPalette: player_rgba` — mismatch causes wrong
  colors on death. Fixed by overriding `DeathSequencePalette: player_rgba`
  in `^TDBuilding` and `^TDDefense` templates. Also fixed 3 CABAL infantry
  (rocketcyborg, hackercyborg, eliminator800) that had `ra2player` instead
  of `playerra2` as their death palette (mismatch with their
  `PlayerPalette: playerra2`).
- [ ] **TS-only death palette audit** (Effort: M): The broken commit
  `9579827e9` was reverted, but the original fixes in `a2b4de333` and
  `b417c6f96` may have set wrong palette values for some TS actors. Need
  a smarter audit script that only checks TS content packs and reports
  mismatches between `DeathSequencePalette` and `PlayerPalette`. Do NOT
  touch TD, D2k, RA1, RA2, TKM files.

- [x] **Shellmap boot crash: "No valid shellmaps available"** (`6a74333d5`):
  the fix-oramap.ps1 rename pass used CASE-INSENSITIVE replaces on map.yaml
  inside the .oramap zips, corrupting shellmap_v2's PlayerReference
  `Allies:` field keys into `ra1_allies:` (invalid field → map excluded
  from the shellmap pool), and renamed display player names without
  updating the lua inside the zips (`Player.GetPlayer("Allies")` → nil →
  lua fatal). desert-shellmap-2 also kept nonexistent factions `soviet`
  (singular, missing from the tool's rename list) and `modjapan`. Fixed
  both maps + hardened the tool (`-creplace`, added soviet/modjapan
  entries). LESSON: .oramap rewrites must be case-sensitive and must
  update embedded lua player/actor strings in the same pass; a mod-wide
  GetPlayer↔player-name scan now shows 0 mismatches.
- [x] **12 more maps broken by the renames** (`2df758574`): mod-wide sweep
  of all 364 maps (invalid Faction values, unknown actor types, orphaned
  Owners, stale lua ids). Fixed: 5 mission maps (ch1-e1, ch1-e1c,
  delivery, deliverycoop, iris-ally-hb) with 25 stale `Faction:` values +
  1 lua id; 5 .oramaps with singular `ra1/ra2_soviet_*` actor types
  (Border conflict, _ra_ore-gardens, _ra_temperal, thelake6people,
  chernobyl); survival.oramap lua (21 ids pluralized);
  desert-shellmap-2-playable orphaned GDI/Nod owners → Neutral.
- [ ] **Pre-existing broken maps found by the sweep (design decisions
  needed, NOT rename-caused)**: (a) ~70 imported maps carry
  `Faction: england`/`ukraine` — factions that never existed in Cameo
  (decide: bulk-rewrite to `Random` or leave — engine may fall back);
  (b) `troublerebels.oramap` references `heavy_inf` (only
  `heavy_inf.ixian` exists — ambiguous); (c) `tiberium-split.oramap`
  references never-defined `split0a/0b/0c/4/8/9` terrain actors (split2/3
  exist); (d) `_d2k_Centerbase` + `_d2k_tournament_spice` reference
  base-D2K generics (`refinery`, `harvester`, `artillery_platform`,
  `combat_siege_tank`, `medium_gun_turret`, `combat_tank_ixian`) that
  Cameo never defined; (e) survival.oramap still has 13 ancient
  `aa_*`/`steel_*` compressed ids — proposed mappings:
  `aa_phoenix→asianalliance_phoenix`,
  `steel_quantumtank→steelconsortium_quantumtank`,
  `steel_katy→steelconsortium_katytank`,
  `steel_mega→steelconsortium_megalodon`,
  `steel_defender→steelconsortium_defenderbot`,
  `aa_samurai→asianalliance_japanesesamurai`,
  `aa_lynx→asianalliance_lynxtank`, `aa_mecha→asianalliance_pulverizermecha`,
  `aa_flam→asianalliance_asiansentryflamer`; unresolved: `aa_archer`,
  `aa_ftnk`, `steel_fedinf`, `steel_qinf`. Effort: S–M once decided.

### New orders 2026-07-17 (second batch)

- [x] **Umlaut transliteration** (2026-07-17): `schwarzermond_bermensch`
  → `schwarzermond_ubermensch` (Ü was dropped instead of transliterated),
  `ÜbermenschLaser(E)` → `UbermenschLaser(E)`, assets git-mv'd. RULE in
  DESIGN §1: Ü→u, Ö→o, Ä→a, ß→ss in ids; display names keep umlauts.

- [ ] **BUG: cameo tileset palettes** — wrong palettes for ALL smudges,
  craters and building bibs on the cameo tileset. Effort: M (tileset
  palette wiring investigation). Reported by maintainer; got lost from
  an earlier queue — do not lose again.
- [x] **SM promotion grid (maintainer's design, image 2026-07-17)** —
  3 columns x 4 ranks implemented in `SchwarzerMond/yaml/promotions.yaml`.
  [Übermensch/Laser Tank(rpl Beetle)/Crystal Tank/Parzival] |
  [Noid MG/Lunar Tiger(rpl Panzer)/Korruptes Biest/Dalek] |
  [Piercer/Haunebu 3(rpl H2)/MARS(rpl Jagerline)/Die Glocke]. Unit
  prerequisites wired to require the matching promotion; replaced units
  disabled when the replacement promotion is bought. Promotion-unit
  `^PromotionUnitBuff` inheritance verified on all grid units. Boot
  test passed (2026-07-17).
- [x] **cabal_plasmaturret not buildable** — root cause: no sequence/
  icon defined for `cabal_plasmaturret`. Added sequence in `ContentPacks/
  TiberianSun/CABAL/yaml/sequences.yaml` and voxel turret mapping in
  `sequences/voxels.yaml`, using TS Nod laser turret assets as placeholder
  (2026-07-17). Boot test passed.
- [x] **cabal_mobilestealthgenerator removed** — CABAL should not have
  it (design 2026-07-17); actor + AI references deleted.
- [ ] **RA1 LEGACY-ID RENAME (ordered 2026-07-17)** — every remaining
  old-style RA1 actor (RAE1, RAE3, RAAPC, PT/DD/CA/SS/MSUB, POWR/APWR/
  KENN/RASILO, naval yards, BADR family, civilians, husk variants,
  proxy actors — 53 ids in RedAlert/Shared) gets its grammar-compliant
  id (ra1_allies_/ra1_soviets_/japan_/shared ra1_). Only `japan` keeps
  no game prefix (exists once). Full pipeline with verification.
- [ ] **Stale copy cleanup**: rules/redalert.yaml + the dead
  ContentPacks/RedAlert/content.yaml wrapper are UNLOADED duplicates
  since PACK-RA2 (mod.yaml now loads RedAlert/Shared) — delete after
  the rename lands (they also pollute audit_packs P1).

### P0/P1 — User-reported issues (2026-07-15/17)

> Golden reference (pre-rename, everything working):
> `C:\Users\AedisToru\AppData\Local\Cameo-IFV\instances\cameo\main` —
> diff against it when a rename regression is suspected. Tester reports
> (NFWRambo) need verification before fixing.

- [x] **SHARED-ASSET RENAME CLASS sweep** (2026-07-17) — audit_asset_files
  re-run on the full tree: A1 rename-broken refs = 0, A2 missing voxels
  = 0 (the brik/chainlink fixes cleared the class in the loaded tree).
  56 A3 informational refs remain in UNLOADED legacy rules (actiblizz,
  darkreign, iok, starwars) + a few possibly-in-mix refs — no action
  while unloaded. Rule added to DESIGN §1: rename only after crossref
  proves ONE user; shared assets keep their names.
- [x] **RA1 Allies reinforcement pad** (2026-07-17) — chain VERIFIED
  intact: pad needs conyard + techcenter + the promotion + derricklimit;
  the promotion itself needs the Rapier Jumpjet promotion + rank1.
  Tester most likely hadn't completed the two-step promotion chain or
  hit the lobby derrick limit. Not a code bug; maintainer to confirm
  in-game.
- [x] **RA1 Allies description listed SOVIET doctrines** (2026-07-17)
  — CONFIRMED + FIXED: `faction_ra_allies.description` in
  fluent/rules/en.ftl carried the 6 Soviet doctrines and doctrine
  feature bullets; replaced with the real Allied research tree
  (Advanced Radar Systems ... GPS Satellite Support).
- [x] **TD GDI APC described as amphibious** (2026-07-17) — CONFIRMED
  + FIXED in FACTIONS.md: locomotor is `tracked` (not amphibious); the
  AA capability is real (APCGunAA).
- [x] **Schwarzer Mond promotions missing** (tester, second report) —
  FIXED 2026-07-17: implemented the 3-column SM promotion grid from the
  maintainer's image in `SchwarzerMond/yaml/promotions.yaml`, wired all
  unit prerequisites, and verified `^PromotionUnitBuff` on promotion units.
  Boot test passed.
- [x] **Warhead wall-capitalization** (2026-07-17) — evidence reversed
  the call: lowercase `wall` IS the standard (all 3 TargetTypes
  definitions + 345 weapon refs lowercase; only 2 refs used `Wall`).
  Normalized the 2 outliers (starcraft, starwars) to lowercase instead
  of churning 348 lines. Convention documented in DESIGN §1.
- [x] **P0 CRASH: missing `futuretech_concretebarrier_brik.shp` during menu load**
  (2026-07-17) — FIXED: corrected `brik:` sequence in `sequences/tiberiandawn.yaml`
  to use `brik.shp` / `brikicon.png` matching release. Boot verified.
- [x] **P0 CRASH: `japan_chainlinkfence_icon.tem` not found in `cycl` sequence**
  (2026-07-17) — FIXED: replaced with `cyclicon.png` matching release in
  `sequences/tiberiandawn.yaml`. Boot verified.
- [x] **P0 BUG: TD GDI vehicle palette issues** — RESOLVED by user confirmation
  (2026-07-17): palettes are correct in current build; tester was likely on an
  older commit without fixes.
- [x] **P0 BUG: All renamed factions missing voice/notification variants** (2026-07-17)
  — ROOT CAUSE: faction rename migration changed `InternalName` values (e.g.
  `gdi`→`td_gdi`, `nod`→`td_nod`, `allies`→`ra1_allies`/`ra2_allies`,
  `soviets`→`ra1_soviets`/`ra2_soviets`, `tsgdi`→`ts_gdi`, `tsnod`→`ts_nod`),
  but audio variant/prefix keys in `voices.yaml`, `notifications.yaml`, and
  `redalert2.yaml` still used the old names. Without a matching key, the engine
  falls back to `DefaultVariant`/`DefaultPrefix` with no faction suffix,
  producing filenames like `vehic1.aud` instead of `vehic1v00.aud` — which
  don't exist, so voices/notifications are silently skipped.
  FIX: added variant entries for all renamed factions to:
  - `voices.yaml`: `GenericVoice`, `VehicleVoice` (td_gdi, td_nod);
    `RAGenericVoice`, `RAVehicleVoice` (ra1_allies, ra2_allies);
    `RussianVehicleVoice` (ra1_soviets, ra2_soviets)
  - `notifications.yaml`: Prefixes section (td_gdi, td_nod, ra1_allies,
    ra1_soviets, ra2_allies, ra2_soviets, ts_gdi, ts_nod)
  - `redalert2.yaml`: `RA2EngineerVoice`, `RA2MCVVoice`, `RA2LanderVoice`
    Prefixes (ra2_allies, ra2_soviets, ra1_allies, ra1_soviets, td_gdi, td_nod)
  Units with explicit `Voiced` traits using non-variant voice sets (e.g.
  `TSVehicle`, `CommandoVoice`, `BattleFortressVoice`) were unaffected.
  Boot verified, no new exceptions.
- [x] **CRASH: ixian_koda_tank missing icon sequence** — VERIFIED 2026-07-16:
  the `icon` sequence already exists in `Ixian/yaml/sequences.yaml` (line 1372,
  `Filename: DATA.R16, Start: 4028`). `audit_sequences.py` reports 0 S2 missing
  sequences. Crash may have been fixed in a prior session.
- [x] **BUG: Repair drone not repairing** — root cause: `AutoTarget:
  EnableTargeting: false` prevented auto-acquisition of repair targets.
  Fixed by removing the override and restoring `InitialStance: Defend,
  ScanRadius: 12` from `^HelicopterTemplate`. Also set
  `PersistentTargeting: true` on `AttackAircraft` to maintain repair
  targeting. Matches working Ixian repair drone pattern.
- [x] **BUG: Tarantula firing offset** (2026-07-17) — FIXED: restored
  release values. `Turreted: Offset` from `-500,0,0` to `-500,1,1`;
  `LocalOffset` from `500,0,250` to `800,300,700` on both armaments.
  The offset had been changed during the CABAL rebalance and broke
  projectile origin alignment.
- [x] **BUG: Artillery spider firing offset** (2026-07-17) — FIXED:
  restored release values. `LocalOffset` from `300,0,800` to
  `-125,1,250,-125,1,250` (dual barrels) on both armaments.
- [x] **BUG: Tarantula upgraded weapon missing correct magicnuke explosion**
  (2026-07-17) — FIXED: `TS120mm_bluenuke` was using `magicnuke_small`
  (Scale 0.25) instead of `magicnuke_med` (Scale 0.5). Per the scaling
  system: `magicnuke` (1.0) = superweapon, `magicnuke_med` (0.5) =
  second biggest (artillery/heavy units), `magicnuke_small` (0.25) =
  third, `magicnuke_micro` (0.2) = fourth. The Tarantula deals the
  most damage among units, so it gets `magicnuke_med`. The Artillery
  Spider's `CabalArtilleryWalkerShellUpgraded` already correctly used
  `magicnuke_med`.
- [x] **RENAME: interceptor.nax → naxis_interceptor** — renamed
  `nax_interceptor.shp` to `naxis_interceptor.shp` in `bits/ra2/mod/`,
  updated all references in Naxis `sequences.yaml`.
- [x] **RENAME/MOVE: drone.nax → schwarzermond_drone** — renamed
  `nax_drone.shp` to `schwarzermond_drone.shp` and `nax_drone_icon.png`
  to `schwarzermond_drone_icon.png`. Updated SchwarzerMond `sequences.yaml`
  and Naxis `sequences.yaml` (interceptor icon reference).
- [x] **BUG: CABAL Obelisk range/detection** — weapon range set to 12288,
  `WithRangeCircle: Range: 12c0` added, `RevealsShroud: Range: 7c0` matches
  Nod obelisk. All three items already present in working copy.
- [x] **BUG: Starcraft alien ranks applied to all SC factions** (2026-07-17)
  — FIXED: verified that separate decorations already exist in code:
  `^ZergRankDecoration` (alienrank), `^TerranRankDecoration` (terranrank),
  `^ProtossRankDecoration` (protossrank). All three sequence definitions
  exist in `sequences/misc.yaml` using `alienranks.png` as placeholder.
  Found and fixed 7 actors missing their faction's decoration:
  `protoss_corsair`, `protoss_positron`, `terran_madcap`,
  `terran_jimraynor`, `terran_goliathmk2`, `zerg_guardian`,
  `zerg_gorekraken`, and `SCINTERCEPTOR`. Updated
  `audit_rank_decoration.py` to recognize the new decoration names and
  correct `StarCraft` path casing. Audit now reports 0 StarCraft issues.
- [x] **RULE: ActorStatValues upgrade list limit** — documented in DESIGN.md §6
  (design 2026-07-17): `ActorStatValues.Upgrades` maximum expanded from 5 to 10.
  Every unit must list all faction upgrades that affect it; team upgrades from
  other factions must never appear. Applied to `ra1_soviets_monstertank`.
- [x] **RULE: Promotion-unit prerequisite formula** — documented in DESIGN.md §18:
  `Buildable.Prerequisites: ~productionbuilding, techbuilding, ~promotion`.
  The `~promotion` token hides the unit until the promotion is bought; tech
  buildings disable but do not hide. Applied the `~promotion` change to ~144
  promotion units across all factions; reverted accidental `~promotion` changes
  in promotion-actor prerequisite chains.
- [x] **RA1 Soviet Monster Tank upgrade coverage** — added all tank/vehicle doctrine
  and upgrade inherits: `^InfernoDoctrineRA1`, `^TeslaExperimentalTechDoctrineRA1`,
  `^TeslaRocketsUpgradeRA1`, `^NuclearRocketsUpgradeRA1`, `^NuclearShellsTeamUpgradeRA1`,
  plus modest `FirepowerMultiplier` traits for the rocket conditions. Added the
  full `ActorStatValues` upgrade list (10 entries). Note: combined firepower
  stack may exceed the 2.0× power-budget rule for an epic unit; monitor in
  playtesting.
- [x] **All-faction promotion construction-yard gates restored** — corrected an
  earlier mistake: promotion actors MUST keep their `~constructionyard`
  prerequisite. Re-added `~constructionyard` to all promotion actors across all
  factions and updated `tools/audit/audit_promotion_gating.py` and DESIGN.md §18
  to enforce this rule. Promotion-units themselves still use
  `~productionbuilding, techbuilding, ~promotion`.
- [x] **Yuri Mastermind turret attack** — added missing `AttackTurreted:` trait to
  `yuri_mastermind`. The actor already had `Turreted:` and `Armament@PRIMARY`,
  but no turret attack activity, so it defaulted to frontal behavior.
- [ ] **BALANCE: Eliminator 800 overpowered** — 7 Eliminator 800s
  destroyed AI base with only 1 loss. Needs rebalancing (part of full
  CABAL rebalance). Effort: M. **Do NOT auto-apply — requires user
  approval per balance policy.**
- [ ] **BALANCE: Warcraft anti-air damage** — Warcraft anti-air damage is
  reportedly too low/unsatisfying. Needs investigation and balance pass
  (warhead values, weapon targeting, or unit stats). Effort: M. **Do NOT
  auto-apply — requires user approval per balance policy.**

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

## Content-pack completion — TOP PRIORITY (ordered 2026-07-16)

_User order (verbatim intent): "Move everything to the new content packs
and verify that everything has been converted correctly! Try your best
reasoning to make sure every actor you move is in the right content
pack. It happened before that some ended up in the wrong section. Also
start moving all the necessary game files into the content packs as
well."_

- [ ] **PACK-RA1: Split RA1 (Allies / Soviets / Japan) out of
  rules/redalert.yaml** into ContentPacks/RedAlert/{Shared,Allies,
  Soviets,Japan} using tools/packs/split_faction.py. Shared concrete
  actors (`RAE1`, `RARE1`, shared `^RA*` templates) go to
  RedAlert/Shared. Verify: registry identity + resolved-closure diff
  empty + boot. NOTE: `RAE1` IS the Allied basic rifleman (user
  correction 2026-07-16) — legacy short ids like RAE1/RARE1 get their
  §1-compliant names during this split's rename step.
- [ ] **PACK-RA2: Split RA2 (Allies / Soviets / Yuri)** from
  rules/redalert2.yaml the same way.
- [x] **PACK-SC** (`4fe295183`): Terran/Zerg/Protoss split, registry-identical, boot-verified.
- [x] **PACK-WC2** (2026-07-17): Humans/Orcs split, registry-identical, boot-verified.
- [x] **PACK-TKM** (2026-07-17): split (ContentPacks/TKM/TKM), registry-identical, boot-verified.
- [ ] **PACK-OP2**: split the Outpost2 monolith (eden/plymouth, WIP factions) — last loaded monolith.
- [ ] **PACK-AUDIT (wrong-section detector)**: new
  `tools/audit/audit_packs.py` that verifies per pack: (a) every actor
  id carries the pack's faction prefix (catches actors landing in the
  wrong pack); (b) actors sit in the correct per-type file (trait
  heuristic: Building→buildings/defenses, Aircraft→aircraft, naval
  Locomotor→naval, husk→husks, upgrade/promotion markers→their files);
  (c) content.yaml lists exactly the yaml files on disk (no drift, no
  nonstandard filenames); (d) pack references resolve inside
  pack+Shared+core only. Run after every split.
- [ ] **PACK-ASSETS: per-faction asset migration** — repeat the CABAL
  pilot for every split pack (identify faction-unique files, move to
  files/{sprites,icons,voxels,sounds}, reference via package prefix,
  boot). Order: follow the pack splits; the four cross-game blockers
  (gunfire2, electro, dragon, DATA.R16) stay tracked above.
- [ ] **PACK-GEN (automatic maintenance)**: `tools/packs/gen_content.py`
  regenerates every pack's content.yaml deterministically from the
  files on disk (sorted, grouped Rules/Weapons/Sequences/FluentMessages);
  audit mode fails on drift. content.yaml becomes machine-maintained.

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
- [x] **Wire D2k factions to `^DuneRankDecoration`** (`5ff288c5c`) — Added
  `Inherits@decoration: ^DuneRankDecoration` to 64 D2k actors across Ixian,
  Ordos, Harkonnen, and Shared yaml files. Audit tool:
  `tools/audit/audit_dune_rank_decoration.py` (0 remaining).
- [x] **Create `^AlienRankDecoration` template** (`b95f5e7f3`) — Created
  template in `rules/starcraft.yaml` using existing `alienrank` sequence
  from `misc.yaml`. Wired to 79 StarCraft actors (Terran, Protoss, Zerg)
  that use `^GainsExperienceTD`. Warcraft2 actors still need a custom
  `wc2rank` image (no sequence exists yet — out of scope).
  **NOTE (2026-07-16):** This commit incorrectly applied `^AlienRankDecoration`
  to ALL Starcraft factions. It should only apply to Zerg. Terran and
  Protoss need separate decorations. See SC-RANKS below for the fix plan.
- [ ] **Create per-faction rank decorations for RA2Mod factions** —
  currently all RA2Mod factions share `ra2rank` via
  `^GainsExperienceRA2`. Eventually each could have a unique rank image
  for faction identity (low priority — shared `ra2rank` is functional).
- [x] **Write `audit_rank_decoration.py`** (`10220c0ee`) — verifies every
  `^GainsExperienceTD` actor has the correct `^*RankDecoration` for its
  faction, verifies `^GainsExperienceRA2` actors do NOT have a separate
  decoration, and checks that rank image sequences exist in `misc.yaml`.
  Current state: 135 issues (mostly SC/WC2/RA2Mod factions that share
  `ra2rank` or lack faction-specific decorations — low priority).
- [ ] **E1: Add missing elite weapons** — Audit (`tools/audit/audit_missing_elite.py`,
  `4d0e8ec85`) found **1256** buildable actors with `GainsExperience` but no
  `Armament@*ELITE*` block. Top factions: rules/redalert (100), rules/starcraft
  (79), rules/wh40k (75), rules/darkreign (68), rules/shockwave (67),
  rules/generals (55), rules/advancewars (52), rules/starwars (45),
  rules/redalert2 (41), TS/Forgotten (37), rules/tkm (36), TS/CABAL (34).
  This is a large multi-session design effort — each elite weapon needs unique
  stats, not a mechanical rename. Needs user direction on scope/priority.
  **NOTE (2026-07-16):** The audit script was updated to only flag
  `^GainsExperienceRA2` actors (per DESIGN.md §16.3 "RA2 system only").
  The count of 1256 was from the old scope — re-run the audit for the
  current RA2-only count. TD/D2k/SC/WC2 actors no longer flagged.
- [x] **E2: Fix missing `rank-elite` conditions** (`ac3ba04b7`) — Only 2
  genuine bugs found (out of 18 flagged; rest use Generals `scrap_create_bonus`
  rank system or upgrade-switch naming). Fixed:
  `asianalliance_plasmatrooper` GARRISONEDELITE and
  `asianalliance_heavyrailguntank` ELITE. Added audit tool
  `tools/audit/audit_elite_gating.py`.
- [x] **E3: Normalize elite weapon naming** (`ab870ddb3`) — Renamed 10
  non-standard elite weapons to `<base>E` convention (38 references across
  12 files): `NaxPlanegun`→`NaxPlanegunE`, `NaxPlaneRockets`→`NaxPlaneRocketsE`,
  `NaxiWW2MachinegunnerElite`→`NaxiWW2MachinegunnerE`, `NaxiBeetleLaser`→`NaxiBeetleLaserE`,
  `NaxiBeetleLaserAA`→`NaxiBeetleLaserAAE`, `NaxCorrosionRocketTrooper`→`NaxCorrosionRocketTrooperE`,
  `TSBikeMissileNashwaElite`→`TSBikeMissileNashwaE`, `V3LaunchElite`→`V3LaunchE`,
  `RA2KirovBomb_nuclear_Elite`→`RA2KirovBomb_nuclear_E`, `CuteKirovBombElite`→`CuteKirovBombE`.
  Remaining 44 are doctrine variants (`_rad`/`_fire`/`_tesla`), upgrade combos,
  or gatling spin-ups — intentionally non-standard. Audit tool:
  `tools/audit/audit_elite_naming.py`.
  **NOTE (2026-07-16):** The `E` suffix convention has been superseded —
  ALL elite weapons must now use `_elite` per DESIGN.md §16.3. The renames
  done here will need to be re-done as `<base>_elite` in WEAPON-SUFFIX-ELITE.
- [x] **E4: Verify base weapon gating** (`ac3ba04b7`) — Fixed the 2 actors
  from E2: added `RequiresCondition: !rank-elite` to
  `asianalliance_heavyrailguntank` PRIMARY and
  `asianalliance_plasmatrooper` GARRISONED so elite replaces, not stacks.

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

## Sequence Filename Standardization

- [ ] **SEQ-RESEARCH: Cross-reference audit** — build a complete map of
  which sequence filenames are used by which actors across all sequence
  YAML files. Identify:
  (a) files used by only one actor (safe to rename),
  (b) files shared across multiple actors (MUST NOT be renamed),
  (c) files in shared namespaces (`shared_sprites|`, `ts_shared_sprites|`,
      `td_shared_sprites|` — never renamed),
  (d) template default filenames in inherited `^` templates (never renamed),
  (e) death/muzzle/parachute files defined in templates (never renamed).
  Output: `tools/audit/sequence_file_crossref.json`.
  Effort: M.
- [ ] **SEQ-MIGRATE: Rename sequence files to match actor + sequence name**
  — per faction, rename actor-owned files so that:
  (a) the idle/body sprite is `<actor_id>.<ext>` and moved to `Defaults:`,
  (b) non-idle sequences use `<actor_id>_<sequence_name>.<ext>` (e.g.,
      `_bib`, `_make`, `_turret`, `_icon`, `_muzzle`, `_active`, `_dead`,
      `_damaged`, `_deploy`, etc.),
  (c) shared files are left untouched,
  (d) Combine sub-images unique to one actor are renamed to
      `<actor_id>_<descriptive_suffix>.<ext>`,
  (e) inherited template defaults are left untouched.
  Use `tools/rename/rename_map_<faction>.yaml` + `tools/rename/apply.py`.
  Verify with `tools/audit/dump_resolved.py` before/after diffs (empty).
  Update `.oramap` files with `tools/fix-oramap.ps1` if needed.
  Effort: L (multi-session, ~18,500 asset files across all factions).
  **Risk assessment**: HIGH — missing a reference causes a crash. Must
  be done one faction at a time with boot tests between each. Shared
  file detection is the critical safety gate. See DESIGN.md §1
  "Sequence filenames must match their actor and sequence name".

- [ ] **WPN-MIGRATE: Rename weapons to include full actor id prefix**
  — per faction, rename actor-specific weapons from PascalCase to
  `<actor_id>_<weapon_descriptive_name>` (e.g., `CabalTarantulaCannon` →
  `cabal_tarantula_cannon`, `RA2KirovBomb` → `ra2_soviets_kirov_bomb`).
  Weapon class templates (`^SmallArms`, `^MediumCannon`, etc.) and
  faction-level templates (`^CabalMissile`, `^RA2RadShell`) keep their
  PascalCase `^` names. Elite variants append `_elite`, EMP variants
  append `_EMP`, AA variants append `_AA`, upgraded variants append
  `_upgraded`. Weapons shared across factions (in Shared/ packs) stay as-is.
  Use `tools/rename/rename_map_<faction>.yaml` + `tools/rename/apply.py`.
  Verify with `tools/audit/dump_resolved.py` before/after diffs (empty).
  Effort: L (multi-session). See DESIGN.md §1 "Weapon names must include
  the full actor id as a prefix".

## Faction Internal Name & Inherits Consistency

- [x] **FACTION-RENAME: Rename faction internal names for consistency**
  — DONE 2026-07-16. Renamed 11 faction InternalNames to match actor
  prefixes, plus WC2 actor prefix rename. All YAML, Python, MD, AI files
  and asset files updated:
  - `gdi` → `td_gdi`, `nod` → `td_nod` (TD factions)
  - `allies` → `ra1_allies`, `soviets` → `ra1_soviets` (RA1 factions)
  - `ra2allies` → `ra2_allies`, `ra2soviets` → `ra2_soviets` (RA2 factions)
  - `tsgdi` → `ts_gdi`, `tsnod` → `ts_nod` (TS factions)
  - `consortium` → `steelconsortium`, `syndicate` → `latinsyndicate` (RA2 mod)
  - `warcraft_humans` → `wc2_humans`, `warcraft_orcs` → `wc2_orcs` (WC2 factions + actors)
  - `asian_alliance` → `asianalliance` (fixed underscore-in-faction-name violation)
  - Already consistent: `schwarzermond`, `naxis`, `futuretech`, `japan`, `yuri`,
    `forgotten`, `cabal`, `terran`, `zerg`, `protoss`, `tkm`, etc.
  - Verified by `audit_consistency_report.py` checks C6-C11 (73 checks, 0 failures).
  - Remaining: `.oramap` map files may need `tools/fix-oramap.ps1` update.
  - Remaining: WC1 factions (`human` → `wc1human`, `orc` → `wc1orc`) not yet done.

- [ ] **INHERITS-PASCAL: Convert camelCase/snake_case inherits to PascalCase**
  — rename all inherits templates that are not yet PascalCase:
  - RA2 Soviet: `^ra2sovietsConscription` → `^RA2SovietsConscription`,
    `^ra2sovietsInfantryConditioning` → `^RA2SovietsInfantryConditioning`,
    `^ra2sovietshockTrooperTraining` → `^RA2SovietsShockTrooperTraining`,
    `^ra2sovietsFireShells` → `^RA2SovietsFireShells`, etc.
  - CABAL: `^cabal_upgrade_radarhack` → `^CabalUpgradeRadarHack`,
    `^cabal_upgrade_backupsystems` → `^CabalUpgradeBackupSystems`,
    `^cabal_upgrade_cyberneticplating` → `^CabalUpgradeCyberneticPlating`,
    `^cabal_upgrade_neutronnuclearcatalyst` → `^CabalUpgradeNeutronNuclearCatalyst`,
    etc.
  - WC2 Humans: `^wc2_humans_upgrade_swordstrength` →
    `^WC2HumansUpgradeSwordStrength`, `^wc2_h_str_navyshield` →
    `^WC2HStrNavyshield`, etc.
  - WC2 Orcs: `^wc2_orcs_upgrade_axestrength` →
    `^WC2OrcsUpgradeAxeStrength`, `^wc2_o_str_navyshield` →
    `^WC2OStrNavyshield`, etc.
  Update all `Inherits:` references across all YAML files.
  Verify with `tools/audit/dump_resolved.py` before/after diffs (empty).
  Effort: M. See DESIGN.md §1 naming convention (PascalCase for inherits).

## Starcraft Rank Decoration Fix

- [ ] **SC-RANKS: Split alien rank decoration per Starcraft faction**
  — commit `b95f5e7f3` applied `^AlienRankDecoration` to ALL Starcraft
  factions (Terran, Protoss, Zerg). It should only apply to Zerg. Fix:
  (a) Zerg actors keep `^AlienRankDecoration` with `alienrank` image.
  (b) Create `^TerranRankDecoration` with a new `terranrank` spritesheet
      (placeholder graphics OK) and wire to all Terran actors.
  (c) Create `^ProtossRankDecoration` with a new `protossrank` spritesheet
      (placeholder graphics OK) and wire to all Protoss actors.
  Add new rank image sequences to `sequences/misc.yaml`.
  Effort: M. See DESIGN.md §16.2 rank decoration table.

## Weapon Suffix Standardization

- [ ] **WEAPON-SUFFIX-ELITE: Migrate legacy E suffix to _elite**
  — per DESIGN.md §16.3, ALL elite weapons must end with `_elite`.
  The legacy capital `E` suffix (e.g. `BorisAKME`, `PrismTankChargeE`,
  `PrismScatterE`, `RA2KirovBomb_nuclear_E`, `RA160mmE`, `MigMissiles_AA_ELITE`)
  is deprecated. Rename all rank-elite gated weapons from `<base>E` to
  `<base>_elite`. **Critical:** only rename weapons that are actually
  gated by `RequiresCondition: rank-elite` — do NOT rename EMP weapons,
  `PrismChargeE` (which is a prism charge variant, not elite), or other
  weapons that merely end with E. Run `audit_weapon_suffixes.py` to
  identify the exact set. Update all `Weapon:` references in armament
  blocks and all `Inherits:` references to renamed weapons.
  Verify with `tools/audit/dump_resolved.py` before/after diffs (empty).
  Effort: M. See DESIGN.md §1 and §16.3.

- [ ] **WEAPON-SUFFIX-EMP: Standardize EMP weapon names to _EMP suffix**
  — per DESIGN.md §1, weapons whose primary function is EMP disable
  must append `_EMP`. Current EMP weapons use inconsistent naming:
  `SteelEmpBomb`, `TSEMPZapWeapon`, `TSEMPMine`, `TSMobileEMP`,
  `TSCABALEMPDisable.anim`, `CorsairEMP`, `ScienceVesselEMP`,
  `PortaTeslaEMP`, `TTankZapEMP`, `TeslaZapemp`, `edenEMP`,
  `plymouthEMP`, `DREMPDevice`, `IxianEmpBomb`, `CHEMPBomb`,
  `SUSAMLRSEMP`, `SUSAEMPMissileDefenderAG`, etc. Rename to
  `<actor_prefix>_<descriptive>_EMP` pattern. **Do NOT confuse EMP
  weapons with elite weapons** — the previous bulk rename (reverted)
  made this mistake. EMP weapons are never gated by `rank-elite`.
  Run `audit_weapon_suffixes.py` X2 section for the full list.
  Verify with `tools/audit/dump_resolved.py` before/after diffs (empty).
  Effort: M. See DESIGN.md §1.

- [ ] **WEAPON-SUFFIX-AA: Standardize anti-air weapon names to _AA suffix**
  — per DESIGN.md §1, weapons whose `ValidTargets` includes only `Air`
  must append `_AA`. Current AA weapons use inconsistent naming:
  `SWLaserJetpackAA` (already correct), `TSGrenadeAA` (already correct),
  but many others like `SWAWingGunAA`, `SWXWingGunAA`, `DTMissileCrawlerAA`,
  `SCUDAA`, `ZToughMissileAA`, `FlakbusAA`, `SCTyrAA`, `SCDevourerAA`,
  `ManifoldMGAA`, `SUSAGladiatorAA`, `SUSAAdvPatriotMissAA`, etc. that
  already use `AA` but not `_AA` (missing underscore). Rename to use
  `_AA` with underscore separator. Run `audit_weapon_suffixes.py` X3
  section for the full list. Exclude dual-purpose weapons (those that
  also target Ground/Water) and weapons with legacy AA keywords (Flak,
  SAM, Interceptor, Patriot) from the rename.
  Verify with `tools/audit/dump_resolved.py` before/after diffs (empty).
  Effort: M. See DESIGN.md §1.
