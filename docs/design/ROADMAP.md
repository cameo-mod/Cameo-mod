# Cameo Roadmap — detailed work queue (rebuilt 2026-07-12)

_The living work queue, resumable by any agent. Rule zero: crashes and
bugs ALWAYS jump the queue. Ordering within a section: **quickest wins
first, then by severity**. Effort: S < 1h, M = one session, L = multi-
session. Every completed item gets its commit hash; every new order
lands here first. Goal stated 2026-07-12: **finish the CABAL faction**._

---

## P0 — Crashes (always first)

- [x] Voice-set rename crashes (`1616a26d2`); pink menu (`e956d2280`);
  boot crashes crab-junk/shadowteam/stale-DLL (`28ae47612`). LAW:
  launch-game.cmd to menu before EVERY commit (CLAUDE.md gate).

---

## CABAL — the push to finish (P1 highest priority)

### CA. Quick bug fixes (S each, do first — mostly sound/weapon/offset)

- [ ] **Crab deals almost no damage** — investigate + fix (should sit
  between a zergling and an ultralisk: fast + tanky bruiser). Rebalance
  its claws with the formula. `CabalCrabClaws` currently ^SwordWeapon
  16000@25 — verify it actually resolves onto the unit and hits.
- [ ] **Hunter Killer mk1 weapon regressed** — was a blue laser, now a
  big red one (like the TD Nod Obelisk). Restore the blue CABAL laser.
- [ ] **Cyborg Infantry wrong sound** — plays the template default now;
  restore its original VoiceSet/Report from before.
- [ ] **Eliminator 800 (T800) weapon sound** — must match the Gatling
  Trooper. Currently a gatling+chaingun combo — likely two armaments
  firing at once or a Report/Burst clash. (Tied to the gatling-logic
  refit in CB.)
- [ ] **Mantis sound** = TS Nod Laser Turret weapon sound.
- [ ] **Core Defender offset** — muzzle far too low; move it much
  higher. Weapon should be a very slow but very powerful laser.
- [ ] **Missile turn + altitude** (^CabalMissile): turn 128 → **80**;
  add a high **CruiseAltitude (~6000) + TerrainHeightAware** so the
  missile climbs high like SP instead of leveling off after one tick.

### CB. Weapon/offset reworks (S/M)

- [ ] **Rocket-launcher offsets & counts** (dual = two launchers L/R):
  - **Rocket Cyborg** → SINGLE rocket only (revert its dual weapon).
  - **Ascended** → dual rockets + **dual offset** (`128,-64,256,
    128,64,256` pattern).
  - **Cyborg Reaper** → dual rockets (like Ascended) + dual offset;
    still keeps its net.
  - **Manticore** → dual **laser** anti-ground (two offsets) + anti-air
    **rockets**, both at the SAME range (line-breaker rule, DESIGN §3).
  - Only Ascended, Cyborg Reaper, Manticore get double rockets.
- [ ] **Eliminator 800 gatling logic** = the Gatling Trooper's spin-up:
  drop the AmmoPool/ammo-tier armament switching, use ONE ground weapon
  + ONE AA weapon + `GrantConditionOnAttack@gatling` (RequiredShots
  1..10, RevokeDelay 80, MaxInstances 10) + ten
  `ReloadDelayMultiplier@GattlingSpeedN` (Modifier 90). Also give it the
  **dune autogun-turret muzzle** (DATA.R16 @3671, d2keffect palette).
- [ ] **Dissolver rework** (several orders converge here):
  - Remove the **cloak**; make the warhead apply the **Schwarzer Mond
    Rocket Trooper corrosion** effect (more damage taken + DoT) as its
    +0.25 special (replaces cloak+vampire).
  - Weapon = **TankDestroyer + LightChemical** combo (anti-tank),
    even-spread damage; keep the OLD dissolver **sound** (not the
    template sounds) and its old projectile (already restored).
  - **New dissolve impact effect** (see CD) — NOT the light-chemical
    effect.
  - Rebalance with the formula for the new weapon class.

### CC. Infantry turn-rate + armor rework (M) — design decision 2026-07-12

- [ ] All CABAL infantry get a **vehicle-style turn rate** (they are
  AttackFrontal, i.e. must face the target — EXCEPT the Dissolver which
  is Turreted). To compensate the slower turn, give them a **damage-
  resistance buff**: armor like the **FutureTech droids** (Scout /
  Shotgun / Cannon / Missile Droid) — a combination of an infantry AND
  a vehicle armor type with **incoming damage ×2** on the second armor,
  so the two average to a resilience boost. Use those droids as the
  exact reference. (Maintainer is concurrently reworking Ordos armor
  types — coordinate.)

### CD. New CABAL effects (M) — RGBA PngSheet, DESIGN §8 method

- [ ] **Dissolve impact effect** — a corrosive "armor melting" splash
  for the Dissolver's impact (Effect warhead). FUTURE effects must add
  **per-frame randomness/distortion/offsets** (see the standing rule
  below) — the current rocket trail is too perfectly spherical; keep
  the rocket trail as-is but apply randomness to all NEW effects.

### CE. Effect-warhead naming sweep (M) — LAW, see DESIGN

- [ ] Rename EVERY `CreateEffect` warhead to **`Warhead@Effect`** (the
  ground/default one) across all weapons (currently `@2Eff`, `@3Eff`,
  `@DissolveEffect`, etc.). Allowed siblings: `@EffectAir` (weapon hits
  air), `@EffectWater` (weapon hits water); HeavyBomb's two are the
  other exception. Same-named warheads overwrite, so this guarantees
  ONE impact per surface — no doubled effects. (Rule in DESIGN §8.)

### CF. Formula rebalance of ALL CABAL unit stats (L)

- [ ] Run every CABAL unit through the balance formula (sheet-first),
  fixing the ones flagged: **Crab** (no damage), **Laser Spider**
  (Nod Obelisk-of-Light sound + very long reload + high damage), and
  the rest. Port 333ggg's cabal.xlsx rows into the workbook.

### CG. Missing units + promotions + upgrades (M/L)

- [ ] **Missing units** (stats from 333ggg rows + annotations):
  cnc4 Spider (fire-support laser, enters Widow, r33=1200), Widow
  (carrier boosted by ≤4 spiders, r34=2400), T1000 (promotion of
  T800/eliminator800, r9=1500), Commando V2 promotion wiring (unit
  exists), Avatar (r20=2250, placeholder art), Core Defender promotion.
- [ ] **Promotion trees** (design screenshot; right tree empty like TS
  GDI): left (infantry) Devout 1 → Ascended 4 → T1000 7 → CybCom v2 10;
  middle (vehicles) Spider CNC4 2 → Manticore 5 → Widow 8 → Core
  Defender 11. Chain the middle column once Spider CNC4 exists.
- [ ] **Upgrade suite restructure** (Networked Cabal Protocol table,
  cabal_rebuild_plan.md Appendix 2): radar tier = Backup Systems,
  Reclamation Protocol, Neutron Nuclear Catalyst (KEEP the praised
  neutron-shell twins); lab tier = Mobility Matrix, **Advanced Beam
  Cannons** (NEW), **Proton Dissolution** (NEW), Overcharged Servos.
  Existing extras (dark armament, radar hack): ❓ map or retire.
- [ ] **Reaper web upgrade** — SP's Improved Reaper Nets, via the
  existing `snared` condition warhead (Zerg Corruptor pattern).

### CH. Descriptions + AI (M)

- [ ] Fluent descriptions for all CABAL units; AI wiring (squads,
  upgrades, promotions).

### CI. Open design picks (❓ ask before building)

- Artillery Spider tier (333ggg: "maybe T3?"); pillbox flavor ("tankier
  but less dps ts laser turret"); T5-promotion pricing (Cryo Legionaire
  3500 @ 0.75).

---

## Dune factions (D2K) — split + naming + upgrades (P2)

- [ ] **Split dune Light Infantry + Rocket Trooper per faction** like
  the RA1 factions did (a neutral base template → per-faction Ixian and
  Ordos actors), so each faction's upgrades apply separately.
- [ ] **Ordos Light Infantry gets Laser Cartridges** (the upgrade the
  Swarmer + Raider buggy already have) once it's its own actor.
- [ ] **Rename Ordos "Armor-Piercing Rounds" → "Rapid Fire Armor-
  Piercing Belts"** (actor id `up_...`, template, condition, sequence,
  icon file — full rename).
- [ ] **Apply the no-hyphen naming scheme to all dune factions** (ids,
  files, fluent keys).

---

## Content-pack folder restructure (P2/P3, L) — ❓ folder-name pending

- [ ] Give EVERY content pack the SAME structure: at the faction root,
  `content.yaml` (the central include dictionary) + ONE folder holding
  all yaml (rules + weapons + sequences merged — the current
  rules/ + weapons/ + sequences/ split collapses into it) + an (empty
  for now) `files/` folder for sprites/voxels/sounds/icons.
  - Yaml-folder name = **`yaml`** (design decision 2026-07-12); sibling
    asset folder = **`files`**.
  - Shared assets go in a per-GAME `Shared/files/` folder.
  - NOW: only create the yaml folder + move yaml into it + keep
    content.yaml at root; create empty `files/`; leave the asset
    migration for later. Research the existing pack layout first, apply
    identically everywhere. Runbook + naming in docs/MIGRATION.md.

---

## Cross-faction shared-effect independence (LONG-TERM, L)

- [ ] Many factions currently cross-reference each other's effect
  sprites/weapons. LONG-TERM goal (not a quick fix): give each faction
  its own effects, or at minimum share effects only PER GAME (not
  across games). Prerequisite for true dynamic per-faction loading.
  Recorded in DESIGN + MIGRATION.

---

## Phase B — CABAL effects & art polish (after the stat/mechanics work)

- SP-recipe projectiles on every CABAL weapon (contrails, palettes —
  behaviour like SP, art OUR OWN; shattered_paradise_research.md §3).
- CABAL colour identity: dark blue/purple lasers everywhere, Archer
  cone contrails on artillery, unique impact per weapon.
- New art: plasma-ball sprites, CABAL promotion icons for placeholders.
- Sound pass: recreate SP-like reports from TS-base material.

---

## Phase C — Balance & consistency (other factions)

- [ ] Infantry offset sweep beyond TS (15 non-TS armaments lack
  LocalOffset; DESIGN §3).
- [ ] TS rocket launch-angle sweep beyond CABAL (Nod/GDI/Forgotten;
  each needs its turn rate checked vs close-range overshoot).
- [x] FutureTech re-pricing (T3=0.75; epics L=0.3/M=1.0). Queued
  sheet-cell edits still pending in the workbook (see git history).
- [ ] Clean workbook (fresh xlsx, tooltip-exact row names, formula-
  input stats; port CABAL rows).
- [ ] Balance normalization (165 sheet↔game mismatches, F10/F19 turn
  speeds, F11 firing-slow, 30 non-TS even-spread, CABAL selfheal grid).
- [ ] F22 findings: Consortium up_cruiser, Syndicate up_burrito/
  up_lars/up_topol, TS GDI unlockkodiak.
- [ ] FutureTech follow-ups: .futu → futuretech_ rename + descriptions;
  StartingUnits fix; F22 refinement.
- [ ] Soviet units: Gorynych + Stalin Fist; Hacker/Ixian Projector
  rework; neutron-shells reload clarification.

---

## Phase D — SP-ification of the other TS factions (after CABAL)

CABAL first, then TS GDI, Nod, Forgotten, and eventually Scrin. Per
faction: SP-recipe weapons/effects, authenticity pass, mechanics
inspiration, always through the workbook for stats.

---

## Phase E — Platform & engine (background, L)

- SP engine-trait ports (research §4): ArmamentsChargeBar,
  SpreadDamageWithCondition, InstantHitWithFakeBullets, GuardsSelection,
  corpse pair, FirestromSP, WeaponWeather/CloudSpawner, SpawnSparks, AI.
- TS Shared pack move (script ready); remaining theme renames + splits
  (RA1, RA2, SC, WC2, TKM, Outpost2) with A1–A4 audits.
- Formula v2: per-class baselines, AA/projectile-speed/AoE pricing,
  per-ability special values.
- Dynamic faction loading end-game: per-pack ai.yaml, assets into packs,
  unused-file audit (docs/MIGRATION.md).
- Deferred: Latin Syndicate engineer-terrorist merge (DESIGN §13).

---

## Standing rules recorded this pass (see DESIGN.md / memory)

- **Effect-warhead naming**: every `CreateEffect` warhead is
  `Warhead@Effect` (exception: HeavyBomb's two effects). DESIGN §12.
- **Rocket-trail randomness**: NEW animated effects add per-frame
  distortion/offset/variation (the first CABAL trail was too spherical;
  it stays, the rule is for future effects).
- **Content-pack structure**: one yaml folder + files folder +
  content.yaml at root; cross-faction effects to be de-shared long-term.
