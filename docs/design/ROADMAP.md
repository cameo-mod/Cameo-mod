# Cameo Roadmap — ordered by importance, then effort

_The living work queue. Rule zero: crashes and bugs ALWAYS jump the
queue (design 2026-07-11). Effort: S < 1h agent work, M = one focused
session, L = multi-session project. Keep this file current — every
completed item gets its commit hash; every new order lands here first._

## P0 — Crashes / bugs (always first)

- [x] Voice-set rename crashes — 29 refs fixed, A4 audit guards the
  class (`1616a26d2`, root cause of tjk-ws's `fa99c28db`)
- [x] Pink main-menu text — resolved by the C#-derived chrome/fluent
  key reverts (`e956d2280`); confirmed fixed in-game 2026-07-11

## P1 — Quick wins (S effort, high value)

- [ ] **Dissolver: corrosion instead of cloak** — remove the added
  cloak; apply the Schwarzer Mond Rocket Trooper corrosion effect
  (more damage taken + damage over time) as the +0.25 special (S)
- [ ] **CABAL weapon quality pass** per design guidance (S/M):
  - plasma/energy weapons = railgun/tesla/laser/flame/chemical mixes
    (+cannon/missile by type); pure cannons ONLY for real cannon tanks
  - CabalReaperMissiles += Grenade + Shrapnel (HE rockets)
  - CabalArtilleryWalkerShell = cannon classes + Grenade/Shrapnel/
    HeavyBomb (LAW: all artillery = cannon mix + those 3 explosive
    types, scaled by cost/tier)
  - CABAL color identity: ALL lasers dark blue/purple; artillery gets
    the Archer-style cone contrail fading purple→dark blue +
    blue_smokey trail; unique projectile + impact effects per weapon
    (Dissolver spray gets its old projectile back + new impact)
  - CabalDissolverSpray = TankDestroyer + LightChemical (main anti-tank)
  - drop `@2` on Inherits if bare duplicates are the house style
    (verify merge semantics first — ShtoraLaser uses three bare
    `Inherits:` keys and resolves all)
- [ ] **Clean workbook output**: new xlsx with only the relevant tabs,
  unit names corrected to EXACT in-game tooltip names (Consortium
  Artillery Tank row → Hammerhead), law: sheet names == tooltips (M)
- [ ] **Neutron Shells upgrade tradeoff** (Tarantula: + light+medium
  flame/chem AoE; Artillery Walker: + medium+heavy flame/chem +
  HeavyBomb + NuclearWarhead) with a fire-rate tradeoff — ❓ clarify:
  "reduce the reload delay by half" vs "lower fire rate" conflict —
  reload delay ×2 (slower) assumed (S, after clarification)

## P2 — Medium projects (M effort)

- [ ] **CABAL Batch 2b remainder**: platedarmorcyborg→cabal_rocketcyborg
  rename (devout = its promotion upgrade); new units Crab/Widow/Avatar/
  Heavy Reaper on placeholder art; cnc4/T1000/V2 researches;
  heavycabalobelisk→cabal_obeliskprime; Obelisk of Darkness → AA role;
  pillbox rework (❓ option pick pending: nanite bunker / firewall node /
  assimilation / classic+turret); NEW green-plasma turret (Nod Laser
  Turret art — needs the classic plasma ball asset, see SP item);
  eliminator1000 → hero infantry; descriptions + AI wiring (DoD)
- [ ] **Weapon-design research + tier/theme law**: deep research on
  existing weapons per type/lore; codify the tier rules given by design
  (T1 = 0.75–1.0 classes e.g. scout cars smallarms-only; T2 ≈ 1.0
  except AoE families which lag a tier; T3+ = heavy classes + higher
  AoE; highest AoE = high promotions / T4/5 / doctrines e.g. Nuclear
  V2; combo units like Siege Tank mix many warheads); then ASK design
  the prepared questions (M)
- [ ] **Soviet units**: Gorynych (inferno doctrine research, replaces
  Heatwave Tank which moves to Nuclear War doctrine; RA1-style fireball
  projectile) + Stalin Fist (Industrial Efficiency epic mobile war
  factory, shared BuildLimit both forms — option A) via the formula (M)
- [ ] **Ordos Outpost laser upgrade**: mirror the Ixian Tungsten Needle
  pattern (research: all bullet weapons get an upgraded twin) but
  adding the LASER warhead + ordos laser tank effect (raider, light
  infantry, swarmer drone, every bullet weapon); icon art needed —
  propose a recolor of an existing laser icon (M)
- [ ] **Hacker + Ixian Projector rework**: projector gets predator-laser
  targeting sound (replaces tesla noise), DOUBLE weapon + EMP damage,
  holograms via carrier master/slave logic (ground-carrier experiment),
  marks targets; Hacker gets the weak 10-range charge-up version —
  second mechanic pick pending (❓ false telemetry / production hack /
  leech / drone hijack / turret takeover) (M/L)
- [ ] **TS authenticity pass**: Shattered Paradise as reference for all
  TS projectiles/explosions/sounds/trails (classic rocket trail to all
  TS rockets; green plasma ball for Commando/Mk2/new turret) — needs
  the SP zip locally (repo reachable but raw-file browsing is slow) (M)
- [ ] **FutureTech promotion-tier audit**: every promotion unit's tech
  requirement must equal its promotion tier (prospector mk2 currently
  needs battle lab = lockout bug — drop to war factory + promotion);
  add the check to the audit suite (S code + M fixes)

## P3 — Large projects (L effort)

- [ ] **FutureTech T5 restructure** (clean commit before starting):
  robot control center becomes T2 (required by transmission center);
  all 3 tech buildings shift a tier up; promotions re-matched;
  formula M-discounts recomputed on tier changes; NEW separate inherit
  for the robot low-power/control-center disable effect (was reusing
  ^PromotionUnitBuff — can't inherit twice); energizers work on ALL
  robots, not just droids
- [ ] **Balance normalization passes** (sheet wins): 165 sheet↔game
  mismatches; TechTier M auto-fix under the nice-number law; turn-speed
  normalization (51 F10 + 33 F19); F11 firing-slow injection for
  artillery; 30 non-TS even-spread violations
- [ ] **TS Shared pack move** (script ready: strays→owners, rest→
  Shared, weapons/sequences relocation, monolith retired)
- [ ] **TS GDI/Nod description passes** (fixes the 16 raw-key F1s) +
  remaining factions per MIGRATION.md
- [ ] **Remaining theme renames + splits** (RA1, RA2, SC, WC2, TKM,
  Outpost2) with the A1–A4 audit after every pass
- [ ] **Formula v2**: per-class baselines (infantry low-end fix +
  defense L removal), AA/projectile-speed/AoE pricing, per-ability
  special values (memory: cameo-formula-future-tasks)
- [ ] **Deferred**: Latin Syndicate engineer-terrorist merge (DESIGN
  §13 — blocked on promotion tree + bot module conflict); dynamic
  faction loading end-game (per-pack ai.yaml, assets into packs,
  unused-file audit)
