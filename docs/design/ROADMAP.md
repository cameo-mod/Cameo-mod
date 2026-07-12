# Cameo Roadmap — phased long-term plan (rewritten 2026-07-11 per design order)

_The living work queue. Rule zero: crashes and bugs ALWAYS jump the
queue. Phases run in order; inside a phase, items are sorted by
importance then effort (S < 1h, M = one session, L = multi-session).
Every completed item gets its commit hash; every new order lands here
first. Current focus ruling: **"Right now we just want to have the
faction [CABAL] in game with all prerequisites and stats. The effects
will come after that."**_

## P0 — Crashes / bugs (always first)

- [x] Voice-set rename crashes — 29 refs fixed, A4 audit guards the
  class (`1616a26d2`, root cause of tjk-ws's `fa99c28db`)
- [x] Pink main-menu text — C#-derived chrome/fluent reverts
  (`e956d2280`), confirmed in-game 2026-07-11
- [x] Boot crashes 2026-07-11 (`28ae47612`): cabal_crab junk trait;
  ts_nod_shadowteam merge mangle; stale DLLs after TakeOffOnMake.
  LAW since: launch-game.cmd to main menu before EVERY commit
  (CLAUDE.md commit gate).

## Phase A — CABAL complete in game: prerequisites + stats (NOW, L)

Everything stat/tech-side finished BEFORE any effects work.

- [x] **Reshuffle round 1+2** (2026-07-11): Mantis = fire support
  875 (SmallArms+Laser mix H=1.0, CABAL blue zap, Manta anchor);
  Crab = regular fast-tanky melee 675; Rocket Cyborg 650 (single
  dual-role rocket weapon per the AA law); Ascended 900 = its
  promotion; Devout 750 = Cyborg Infantry promotion (cnc4-mg row);
  Manticore 1400 = NEW line-breaker unit via promotion (Advanced
  Reaper row; missile+grenade+shrapnel mix, Air-only AA at SAME
  range/class/reload/DPS; keeps the net = its K=1.25); Cyborg Reaper
  ground missiles Ground-only + grenade/shrapnel mix (support AA keeps
  1.5× range). Promotions in GDI column layout (left: Devout 1 →
  Ascended 4; middle: Manticore 5). DUAL-WEAPON AA LAW in DESIGN §3.
- [ ] **Promotion trees completion** (M) — target layout (design
  screenshot 2026-07-11; right tree stays empty like TS GDI):
  left (infantry): Devout 1 → Ascended 4 → T1000 7 → CybCom v2 10;
  middle (vehicles): Spider CNC4 2 → Manticore 5 → Widow 8 →
  Core Defender 11. Needs the missing units first (below). Chain
  middle column once Spider CNC4 exists (Manticore currently
  chainless at 5).
- [ ] **Missing units** (M/L, stats from 333ggg rows + annotations):
  cnc4 Spider (fire support laser, can enter Widow, r33 = 1200),
  Widow (carrier boosted by ≤4 spiders inside, r34 = 2400),
  T1000 (replaces T800/eliminator800 via promotion, r9 = 1500),
  Commando V2 promotion wiring (unit exists), Avatar (r20 = 2250,
  placeholder art), Core Defender promotion (unit exists, 12500).
- [ ] **Upgrade suite restructure** per the design table (M):
  radar tier = Backup Systems (reclaim vehicles from husks),
  Reclamation Protocol (HP + regen), Neutron Nuclear Catalyst (KEEP
  the current neutron-shell weapon twins unchanged — design praise —
  optionally extend to more units); lab tier = Mobility Matrix
  (walker speed+HP), **Advanced Beam Cannons** (NEW: upgrades
  defenses/HK mk2/spider/mantis/widow beams; ADDS beams to MG
  cyborgs, tarantula, spider tank drone, HK mk1), **Proton
  Dissolution** (NEW: upgrades plasma of commando/T800/T1000/avatar;
  ADDS plasma to HK mk2, artillery, rocket cyborgs, reapers),
  Overcharged Servos (attack speed for reapers/avatar/HK mk2/
  tarantula/artillery/rocket cyborgs/T800:100/commando). Existing
  upgrades not in the table (dark armament, radar hack, …): ❓ map or
  retire — ask design.
- [ ] **StartingUnits fix** (S): CABAL light/heavy support still spawn
  tsbike/tsttnk (Nod placeholders) — swap to mantis/crab per "Mantis
  is a regular starting unit".
- [ ] **Descriptions pass** (fluent keys) + **AI wiring** (squads,
  upgrades, promotions) (M).
- [ ] ❓ open design picks: Artillery Spider tier (333ggg: "maybe
  T3?"); pillbox flavor (annotation: "tankier but less dps ts laser
  turret"); Ordos light infantry laser twin (shared d2k trooper —
  cross-pack condition or Ordos-own actor?); T5-promotion pricing
  (Cryo Legionaire 3500 @ 0.75).

## Phase B — CABAL effects & art (after A, M/L)

- SP-recipe projectiles on every CABAL weapon (LaunchAngle, contrail
  colors/lengths, palettes — behaviour identical to SP, art OUR OWN;
  docs/design/shattered_paradise_research.md §3 has the recipes;
  ASSET LAW: nothing taken from SP, TS-base or new art only).
- New art: plasma ball sprites, smoke-trail sprite if base TS lacks
  one, Ordos laser upgrade icon (currently reuses the laser tank
  icon), CABAL promotion icons where units are placeholders.
- CABAL color identity: dark blue/purple lasers everywhere, Archer
  cone contrails on artillery, unique impact effects per weapon.
- Sound pass: recreate SP-like reports from TS-base material.
- Reaper web upgrade (snared-condition warhead, Zerg Corruptor
  pattern) as a CABAL research (SP: Improved Reaper Nets).

## Phase C — Balance & consistency (parallelizable with B)

- [ ] **Infantry offset sweep beyond TS** (S): 15 non-TS infantry
  armaments still lack `LocalOffset` (DESIGN §3 rule); apply the
  128,0,256 default. Offenders: contaminator/saboteur/fremen_creep
  (D2k), samurai/alligator/fedinf/engi.futu/litt/frank/conehead/
  engi.nax2/mili/ra2terror + hmg/quadflak dummies (RA2Mod). Skip pure
  Targeting dummies where meaningless.
- [ ] **TS rocket launch-angle sweep beyond CABAL** (M): apply the
  vertical-launch + turn-128 recipe (DESIGN §3) to all TS-theme rocket
  weapons (Nod/GDI/Forgotten), each needing its turn rate checked so
  it doesn't overshoot close targets like the Guardian GI did.

- [x] FutureTech re-pricing per sheet, T3 = 0.75 confirmed; epics
  L=0.3 / M=1.0 (DESIGN §12). Queued sheet-cell edits (Excel was
  open): Tanks!M47=0.75,S47=525; M94=1.0,S94=400; M97=1.0,S97=1600;
  M103=0.5,S103=2400; Vehicles!M162=0.5,S162=400;
  Aircraft!M51:M52=0.5,S51:S52=900; epic relabel Tanks!L121=0.3,
  M121=1.0; Harbinger rows flagged (L=1/M=0.75 ≠ epic 0.3).
- [ ] **Clean workbook** (M): fresh xlsx, rows named EXACTLY like
  in-game tooltips, stats = formula inputs; port 333ggg's CABAL rows
  (incl. restated Mantis: L=1 fire support, 875, 12000@40, R 7.082).
- [ ] Balance normalization passes (165 sheet↔game mismatches, F10/F19
  turn speeds, F11 firing-slow, 30 non-TS even-spread violations,
  CABAL selfheal step grid) (L).
- [ ] F22 findings other factions: Consortium up_cruiser, Syndicate
  up_burrito/up_lars/up_topol, TS GDI unlockkodiak (S each).
- [ ] FutureTech follow-ups: rename pass (.futu → futuretech_) +
  fluent descriptions (ordered); StartingUnits fix; F22 refinement (M).
- [ ] Soviet units: Gorynych + Stalin Fist (M); Hacker/Ixian Projector
  rework (M/L); neutron shells reload clarification (S).

## Phase D — SP-ification of the other TS factions (after CABAL)

Order per design 2026-07-11: **CABAL first, then TS GDI, Nod,
Forgotten — and the Scrin faction once it exists.** Per faction:
weapon/effect recipes from the SP research doc, authenticity pass
(classic rocket trails, plasma/laser identities), mechanics
inspiration (corpse/husk feedback, ion storms), always through the
Armor System workbook for stats.

## Phase E — Platform & engine (background, L)

- SP engine-trait ports (research §4; priority pick pending):
  ArmamentsChargeBar, SpreadDamageWithCondition,
  InstantHitWithFakeBullets, GuardsSelection, corpse pair,
  FirestromSP, WeaponWeather/CloudSpawner, SpawnSparks, AI modules.
- TS Shared pack move (script ready); TS GDI/Nod description passes;
  remaining theme renames + splits (RA1, RA2, SC, WC2, TKM, Outpost2)
  with A1–A4 after every pass.
- Formula v2: per-class baselines, AA/projectile-speed/AoE pricing,
  per-ability special values.
- Dynamic faction loading end-game: per-pack ai.yaml, assets into
  packs, unused-file audit (docs/MIGRATION.md runbook).
- Deferred: Latin Syndicate engineer-terrorist merge (DESIGN §13).
