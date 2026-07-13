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

### N2. CABAL superweapon (biggest magicnuke)
- [ ] New nuke support power, **same values as the Ixian EMP Nuke**
  (`supercomputer.ixian` `NukePowerCA` firing `PulseMissile`:
  ChargeInterval 10500, MissileWeapons PulseMissile, MissileDelay 25,
  CameraRange/CircleRanges 10000, etc.) but with the **biggest magicnuke**
  as the missile/impact animation (+ a new sound, see S-rules).
- [ ] **Fired from the CABAL Core**, using **TD Nod Temple of Nod logic**,
  **plus an add-on that adds the missile silo**. (Find the Temple-of-Nod
  NukePower pattern; the "add-on = missile silo" is a prerequisite
  building/attachment that unlocks or houses the silo.)

### N3. CABAL Core = money structure (`7a0d0025d`)
- [x] Turn the CABAL Core into a **special money-generator structure like
  the Asian Military Academy**: **double the income of the Oil Derrick**,
  and it **also counts as an Oil Derrick** (provides that prerequisite /
  captured-tech behavior). It also launches the N2 superweapon.

### N4. Commando plasma weapons (high-impact + warhead combos)
- [ ] DarkObeliskLaser, CabalCommandoPlasma, CabalCommandoPlasmaMk2: keep
  **obelcor3.aud** (do NOT change the sound). Make all three **high
  impact: long ReloadDelay + heavy Damage**.
- [ ] The **two Commando plasma weapons** get extra large-AoE warheads:
  base = **Cannon + Flame + Chemical**; on the **neutron-shell upgrade**
  add **Tesla + Magic + Railgun** warheads.

### N5. Laser beam visual rework (DESIGN law — see below)
- [ ] Every CABAL laser: **two beam colors** (inner + outer), a **mix of
  purple + dark blue**, **not too thin**. Beam **width scales with
  damage** (Mantis + all others currently too thin; Core Defender a touch
  too thick but must still scale). **Color also scales with damage**
  (scale BOTH colors so bigger damage looks more dangerous).
- [ ] **Laser Spider → obelmod1.aud** (TS Obelisk sound) — FIX from the
  obelray1.aud I set. Smaller lasers → **laser turret sounds** (lastur1.aud).
- [ ] **Manticore double laser**: too thin → **spread the two beams out
  more**; rebalance with **more range + more armor**.
- [ ] **3 levels of laser ground-impact effect** (purple/blue, scaled by
  damage), applied to ALL laser weapons; each needs a new sound.

### N6. New CABAL effects + sounds
- [ ] **New explosion effect for ALL CABAL missiles** (+ new sound).
- [ ] Laser impact effects (N5) + green plasma impact (N1) each need a
  paired new sound (DESIGN: effect + sound always defined together).
- [ ] Plasma-weapon sounds: prefer NEW/unique; cross-check Shattered
  Paradise references. (Cannot synthesize quality .wav here — assign
  unique existing mod sounds and flag any that truly need new custom
  audio for the maintainer to source.)

### N7. Weapon-mount offsets (`7a0d0025d`)
- [x] **Ascended + Devout**: increase the **second (Y) value** of each
  triple offset ~**2×** so their weapons sit further left/right.

### N8. Armor combo (was CC; still pending, sheet-coupled)
- [ ] Give every CABAL infantry the **FutureTech-droid dual-armor combo**:
  base infantry `Armor:` (`-RequiresCondition:` to stay on) + a second
  vehicle-class `Armor@X:` + `DamageMultiplier@X: 200` (damage ×2). Pick
  the vehicle class per unit by role. Doubling incoming damage halves
  effective HP → must be re-priced with the formula (couple with N9).
  Reference: Cannon/Missile/Scout/Shotgun Droid in FutureTech infantry.yaml.

### N9. Role + tier + promotion rebalance (L, sheet-first)
- [ ] **Every CABAL unit maps to exactly ONE template role** from
  `defaults.yaml` (^ScoutVehicleTemplate, ^FireSupportTemplate,
  ^MainBattleTankTemplate, ^HeavyInfantryTemplate, etc.). Assign the role,
  set stats from the workbook, apply the number (sheet-first dual-write).
- [ ] **Tech tiers**: better units = higher tier; **fill every tier
  evenly, none empty**. Promotions increase in tech level; **promotion
  trees make sense and are grouped thematically** (see design screenshot;
  left = infantry column, middle = vehicles).
- [ ] Missing units to slot into the tiers/promotions: cnc4 Spider
  (fire-support laser → Widow), Widow (carrier boosted by ≤4 spiders),
  T1000 (promo of Eliminator 800), Commando V2 wiring, Avatar, Core
  Defender promo. Stats from 333ggg's cabal.xlsx rows.

### N10. Upgrades audit
- [ ] Review EVERY CABAL upgrade: does it do something meaningful? Remove
  or repurpose the meaningless ones. Keep the praised neutron-shell twins.
  Confirm each granted condition is actually consumed (cf. the Ordos
  laser-upgrade no-op bug).

### N11. Descriptions + AI
- [ ] Fluent descriptions for all CABAL units (DESIGN description scheme);
  AI wiring (squads, upgrades, promotions).

### CE (carried). Effect-warhead naming sweep, mod-wide
- [ ] Beyond CABAL: rename stray `CreateEffect` warheads to the
  per-surface canonical set (`@Effect` / `@EffectAir` / `@EffectWater` /
  `@ShieldHitEffect`). NOTE: a child that overrides its template's own
  effect-warhead name (e.g. `@3Eff` from `^TSCannonEffect`) is CORRECT,
  not a violation. DESIGN §8.

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
- [ ] No-hyphen naming scheme across all dune factions.
- Note: 7 Ordos armor-rework files are the maintainer's live WIP — leave.

---

## Content-pack folder restructure (P2/P3, L)

- [ ] Every content pack: `content.yaml` at root + one **`yaml`** folder
  (rules+weapons+sequences merged) + an empty **`files`** folder. Shared
  assets → per-GAME `Shared/files/`. NOW: only make the yaml folder + move
  yaml in + empty files/; asset migration later. Runbook: docs/MIGRATION.md.

## Cross-faction shared-effect independence (LONG-TERM, L)

- [ ] Give each faction its own effects, or share only PER GAME. Prereq
  for true dynamic per-faction loading. DESIGN + MIGRATION.

---

## Phase B — CABAL effects & art polish
- SP-recipe projectiles/contrails (art our own); dark-blue/purple identity;
  promotion icons for placeholders; SP-like reports from TS material.

## Phase C — Balance & consistency (other factions)
- Infantry offset sweep beyond TS; TS rocket launch-angle sweep beyond
  CABAL; clean workbook (port CABAL rows); 165 sheet↔game mismatches;
  FutureTech .futu→futuretech_ rename; Soviet Gorynych/Stalin Fist.

## Phase D — SP-ification of the other TS factions (after CABAL)
- TS GDI, Nod, Forgotten, then Scrin — SP-recipe weapons/effects, workbook stats.

## Phase E — Platform & engine (background, L)
- **Port `AttackGarrisonedSP`** (one fire port per passenger) + convert all
  `AttackGarrisoned`/`AttackOpenTopped` units to per-passenger independent
  targeting (blocker: they use single-instance `AttackFollow`). End of queue.
- SP engine-trait ports; TS Shared pack move; Formula v2; dynamic faction
  loading end-game (per-pack ai.yaml, assets into packs, unused-file audit).

---

## Standing rules recorded (see DESIGN.md / memory)

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
