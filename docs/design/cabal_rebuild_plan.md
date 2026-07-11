# CABAL rebuild plan — 333ggg's concept (cabal.xlsx) vs the live roster

_Working plan for executing the CABAL concept workbook. Sheet stats are
formula-validated (same system as `cameo_armor_system.xlsx`); the sheet
wins on mismatch. Status: mapping done 2026-07-11; execution follows
the design picks at the bottom._

## Concept → existing actor mapping

| concept (sheet stats) | existing actor (game stats) | action |
|---|---|---|
| Cyborg 500 · 45k HP · 50spd · 20000@60 | cabal_cyborginfantry (300 · 35k · 56) | rebalance to sheet |
| cnc4 Cyborg 750 · 60k · 24000@48 | — | NEW research (Cyborg→cnc4, Forgotten-chem pattern) |
| Rocket Cyborg 650 · 45k · 24000@60 | closest: cabal_devout? | map or NEW (pick) |
| cnc4 Rocket 900 · 60k · 24000@50 | — | NEW research |
| Dissolver 725 · 50k · 50spd · K=1.5 | cabal_dissolver (750 · 28k · 70) | rebalance; K question: vampire ✓ implemented, cloak NOT — add cloak or K=1.25 |
| Hacker 1250 · 30k · 80spd | cabal_hackercyborg (1200 · 60k · 56) | rebalance |
| T800 1250 · 85k · 32000@32 | cabal_eliminator800 (inf, 1000 · 16k) | rebalance + naming decision |
| T1000 1500 · 100k · 32000@30 | cabal_eliminator1000 (a 250k VEHICLE!) | design pick: concept T1000 is an infantry research of T800; existing eliminator1000 is a different vehicle |
| Cyborg Commando 5000 · 250k | cabal_cyborgcommando (4000 · 200k) | rebalance |
| Commando V2 10000 · 400k (research) | cabal_cyborgcommandov2 (8000 · 400k) | rebalance + wire as research |
| Tarantula 1000 · 110k · 70spd · 16000@48 | cabal_tarantula (1500 · 250k · 75) | rebalance (large) |
| Crab 675 · 50k · 100spd · 16000@25 · rng 1.6 | — | NEW (short-range fast brawler) |
| cnc4 Avatar 2250 · 175k · 32000@40 | cabal_heavyspider? (1200 · 80k) | design pick: rebuild heavyspider as Avatar or NEW |
| Widow 2400 · 100k · 40000@50 (Avatar research) | — | NEW research unit |
| Mantis 500 · 35k · 120spd | cabal_mantis (600 · 40k · 160) | rebalance |
| Spider — TWO variants in sheet: adv-scout 900 OR fire-support 1200 | cabal_laserspider / cabal_spidertankdrone | design pick: which role, which actor |
| Cyborg Reaper 1100 · 70k · AA support | cabal_cyborgreaper (1000 · 75k) | rebalance |
| Heavy Reaper 1400 · 100k (research) | — | NEW research |
| Artillery Walker 1250 · 50k · 70spd · 48000@64 · rng 11.58 | cabal_artilleryspider (1600 · 200k · 60) | rebalance (large) |
| Core 12500 · 1.5M · 160000@70 | cabal_coredefender (10000 · 1M) | rebalance |
| Hunter Killer mk1 1500 · 35k · 160spd | cabal_hunterkillermk1 (1000 · 22.5k · 145) | rebalance |
| Hunter Killer mk2 3000 · 300k · 50spd (SPACESHIP class!) | cabal_hunterkillermk2 (2400 · 60k · 145) | rebalance — mk2 becomes a slow heavy spaceship |
| Cyborg Pillbox 800 · 85k · 12000@18 | cabal_pillbox (600 · 110k) | rebalance |
| Obelisk of Darkness 1200 · 120k · rng 12.66 — **as the AA defense** | cabal_obeliskofdarkness (1350 · 242.5k) | rebalance + role move to AA (sheet section) |
| "Obelisk of Balls" 2400 · 220k · 140000@90 · K=0.75 advanced defense | cabal_heavycabalobelisk (2600 · 300k)? | design pick: same unit? display name needed (proposals below); K=0.75 = negative special? |
| "Nuke or smth" | cabal_missilesilo | exists, keep |
| Scarab APC (tree, no stats row) | cabal_scarabapc (2600) | keep or add sheet row |
| Carryall (tree, no stats row) | cabal_overkillcarryall | keep |
| Engineer / Harvester / T4 stealth gen | exist | keep |

## Execution rules (from DESIGN.md §12)

Sheet wins; every changed weapon = own weapon inheriting the sealed
class templates; even spread; FF twins 50/50; Percentage = 1 per 2000;
nice-number law (prices 25s, damage 2000s, HP 2500/1000 steps, speed
5s); promotion/research units inherit ^PromotionUnitBuff (not modeled
in sheet); descriptions per §7 as part of the pass.

## Design picks needed before execution

1. **Spider role**: advanced scout (900) or fire support (1200)? And is
   it cabal_laserspider or cabal_spidertankdrone (the other one is then
   freed or cut)?
2. **T800/T1000 vs eliminator800/eliminator1000**: concept has T1000 as
   an infantry research of the T800; the live eliminator1000 is an
   unrelated 250k-HP vehicle. Rename/rebuild how?
3. **"Obelisk of Balls"**: map to cabal_heavycabalobelisk? Display-name
   proposals: "Obelisk of Annihilation", "Twin Obelisk", "Obelisk
   Prime" (or keep the meme?). Also: its K=0.75 is below 1 — intended
   as a NEGATIVE special (drawback), or a typo for 1.75?
4. **Avatar**: rebuild cabal_heavyspider into the cnc4 Avatar, or new
   actor and keep the heavyspider separately?
5. **Rocket Cyborg**: is cabal_devout the intended base, or a new unit?
6. **Dissolver K=1.5**: add the missing innate cloak (vampire is
   already implemented), or drop the sheet to K=1.25?


## Batch 2b work order (design 2026-07-11)

- **Engineer**: gets the repair beam + ammo system per the Naxis
  engineering truck / Schwarzer Mond repair bot / Terran SCV pattern.
- **Rocket Cyborg**: cabal_platedarmorcyborg RENAMES to
  cabal_rocketcyborg (sheet row 650/45k/24000@60); **cabal_devout is
  its promotion upgrade** (cnc4 Rocket row 900/60k/24000@50).
- **Hacker**: weak Ixian-projector-style disabling weapon (charge-up
  first), ~10 range, low attack speed, formula-priced. Sound: the GDI
  Predator laser targeting sound, NOT tesla. Additional hacking
  mechanic: open brainstorm (vehicle mind-control too Yuri-like).
- **Ixian Projector rework** (D2k Ixian): predator-laser sound; DOUBLE
  damage on weapon and EMP; holograms become carrier-master/slave
  drones (aircraft-carrier logic on a ground unit — experiment):
  projector damages + EMP-disables + marks the target, holograms
  attack it. Replaces deploy + mob-spawner.
- **New units**: Crab/Widow/Avatar/Heavy Reaper launch with placeholder
  art from base units (no art yet).
- **Pillbox**: it is a BUNKER (building + infantry inside; the
  garrison's weapon is the defense; cost = initial load). References:
  RA2 Soviet Battle Bunker, Terran bunker, RA1/RA2 pillboxes. Options
  for a unique CABAL version to be proposed.
- **New early turret**: Nod Laser Turret art, fires the CLASSIC GREEN
  PLASMA BALL. The same green plasma projectile goes on Cyborg
  Commando and Commando Mk2 (asset exists somewhere in the files).
- **TS authenticity pass**: find the classic TS rocket trail(s), apply
  to all TS rocket units; reference the Shattered Paradise mod for
  correct TS projectile/explosion/sound effects across all TS units.

## Appendix: 333ggg's cell annotations (extracted 2026-07-11)

cabal.xlsx carries 26 cell comments — the design intent per unit.
Key ones (Лист1 tree unless noted):
- Cyborg (B2): "basic heavy infantry, probably manual vision range
  needed"; its pillbox neighbor (E2): "tankier but less dps ts laser
  turret" → pillbox = laser-turret style.
- Rocket Cyborg (B3): "basic anti-air anti-tank heavy infantry" ✓.
- Mantis (C3): "scout without turret, slightly better stats than
  average scout" ✓ (ranged frontal gun, NOT melee — confirmed by
  design 2026-07-11 after a brief melee detour).
- Crab (C4): "fast melee vehicle" ✓ (regular unit, zergling↔ultralisk).
- Dissolver (B5): "heavy anti-tank infantry, TD warhead and lifesteal".
- Tarantula (C5): "heavy main battle tank with heavy but slow gun".
- Scarab APC (C6): "apc. I leave its balance to you".
- Hacker (B7): "heavy infantry with emp weapon".
- Artillery Spider (C7): "heavy artillery. Maybe tier 3?" — ❓ open.
- HK mk1 (D7): "another MG helicopter"; Obelisk of Darkness (E7):
  "fast hitscan flak aa defense with laser projectile".
- Heavy Reaper (C8): "sturdy anti-air cyborg with root mechanic" ✓
  (root = TSReaperTrap web).
- cnc4 Spider (C9): "longer range fire support laser unit. Can enter
  widow to boost its laser. Potentially web laser mechanic".
- T1000 (B10): "heavy infantry with plasma (tesla+chemical maybe?)";
  Avatar (C10): "heavy walker with plasma(?) weaponry"; HK mk2 (D10):
  "medium spaceship with beam cannons, plasma upgrade"; Obelisk Prime
  (E10): "heavy defense with td+laser warhead".
- Commando V2 (B11): plasma tesla+chem; Widow (C11): "heavy fire
  support vehicle. Laser can be boosted by up to 4 spiders inside";
  superweapon (E11): "dune emp nuke would fit".
- Tier IV (C12): "Superheavy walker"; stealth generator (E12):
  "should give some kind of buff rather than stealth around self".
- Лист2 B26/B33: both Spider variants "similar to cnc4" / "beam cannon".

Plasma class ruling from the annotations: plasma ≈ Tesla + Chemical
warhead mix (B10/B11) — use for T1000, Commando plasma, Avatar.

## Appendix 2: design screenshots 2026-07-11 — promotion trees + upgrades

Promotion tree (right column = empty placeholders like TS GDI):

| left (infantry) | middle (vehicles) | right |
|---|---|---|
| Devout (1) | Spider CNC4 (2) | - |
| Ascended (4) | Heavy Reaper -> Manticore (5) | - |
| T1000 (7) | Widow (8) | - |
| CybCom v2 (10) | Core Defender (11) | - |

Upgrade suite "Networked Cabal Protocol":
- Radar tier: Backup Systems (reclaim vehicles from husks),
  Reclamation Protocol (gives HP and regen), Neutron Nuclear Catalyst
  (KEEP the existing neutron-shell twins unchanged — design praise —
  optionally extend to more units).
- Lab tier: Mobility Matrix (speed+HP of walkers), Advanced Beam
  Cannons (upgrade: defenses, HK mk2, spider, mantis, widow; new to:
  MG cyborgs, tarantula, spider tank drone, HK mk1), Proton
  Dissolution (upgrade: cyborg commando, T800/1000, avatar; gives
  weapon to: HK mk2, artillery, rocket cyborgs, reapers), Overcharged
  Servos (attack speed: reapers, avatar, HK mk2, tarantula, artillery,
  rocket cyborgs, T800/100, cyborg commando).

Sequencing ruling: prerequisites + stats FIRST (faction fully in
game), effects and art AFTER (SP-behaviour recipes, own art only).
