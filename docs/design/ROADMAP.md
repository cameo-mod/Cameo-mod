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
- [x] Boot crashes 2026-07-11 (`28ae47612`): cabal_crab junk trait
  line from the rebuild script; ts_nod_shadowteam Buildable mangled in
  the 793281117 merge resolution; stale DLLs after tjk-ws's
  TakeOffOnMake (rebuild needed). LAW since: launch-game.cmd must
  reach the main menu before EVERY commit (see CLAUDE.md commit gate).

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

- [ ] CABAL new-unit art: Crab art EXISTS at bits/ants/crab.shp (+icon);
  Heavy Reaper candidate = tsreapicon.shp + reuse cyborgreaper body;
  only Widow and Avatar need placeholders (design 2026-07-11)
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
  TS rockets; green plasma ball for Commando/Mk2/new turret) — SDK is
  now local; exact recipes decoded in
  **docs/design/shattered_paradise_research.md §3** (trail =
  `TrailImage: small_smoke_trail` + `TrailSequences: idle2`; plasma =
  MissileTA/BulletAS + greenplasma2 + jascgreen + green contrail;
  lasers deep blue 1122FF88 + white core). Assets CC BY-NC — add SP to
  credits.txt when the first asset lands (M)
- [ ] **SP engine-trait ports** (research doc §4; needs design's
  priority pick): ArmamentsChargeBar (Hacker/Projector charge bar),
  SpreadDamageWithCondition (webs/slows/corrosion), GuardsSelection,
  InstantHitWithFakeBullets, nanomachine corpse pair, FirestromSP,
  WeaponWeather/CloudSpawner ion storms, SpawnSparks, garrison/AI bits
  (S each, GPL-compatible)
- [ ] **FutureTech promotion-tier audit**: every promotion unit's tech
  requirement must equal its promotion tier (prospector mk2 currently
  needs battle lab = lockout bug — drop to war factory + promotion);
  add the check to the audit suite (S code + M fixes)

## P3 — Large projects (L effort)

- [x] FutureTech T5 restructure (`1e902df9a`): robot control = T2,
  chain shifted, promotions tier-matched, prospector lockout fixed,
  ^RobotControllable already carried the robot buff (no new template
  needed), energizers on all robots, M-discount cost cuts applied
- [ ] **FutureTech re-pricing per the sheet** (❓ blocked on design):
  my restructure costs were relative scalings of old yaml values — the
  FORBIDDEN method (DESIGN §12 dual-write law, learned 2026-07-11).
  Sheet says Cannon Droid = 350 (M=0.5) vs my 400. Open question for
  design: Naxis T3 got M=0.75, Cannon Droid T3 gets M=0.5 — is the
  rule "promotion units get one tier deeper discount", a new T3=0.5
  table, or per-unit? Then re-derive ALL restructured costs from the
  workbook rows (sheet first, yaml second).
- [ ] **FutureTech follow-ups**: rename pass (.futu -> futuretech_
  grammar) + fluent descriptions (ordered 2026-07-11); StartingUnits
  fix (robots in Light Support violate Tier-1-only + cost targets,
  aggravated by the new costs); F22 refinement (skip promo-to-promo
  chain pairs, debug promo-side tier=0)
- [ ] **F22 findings in other factions**: Consortium up_cruiser,
  Syndicate up_burrito/up_lars/up_topol, TS GDI unlockkodiak — same
  Prospector class: promotions without their unit's tech gate
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
