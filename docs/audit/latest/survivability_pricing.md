# audit_survivability_pricing — E1: the baseline shield is priced at ZERO

| bucket | actors | priced today | belongs to |
|---|--:|---|---|
| spawns with a pool (**always-on**) | 56 | ✖ nothing | **E1 — this report** |
| empty capacity, needs `shieldgen` | 1227 | ✔ correctly nothing | — (it has no shield) |
| pool behind an upgrade | 231 | ✖ nothing | E5 (upgrade pricing) |

Shield row mean Versus **177.23**, so one shield point is **0.5642 HP** BEFORE any shield-gated `DamageMultiplier` — measured off the live ladder every run, never frozen. The Shield row takes **1.483%** of all roster raw damage at baseline.

⚠ **Every one of these 56 actors also carries `DamageMultiplier@shielded: 150`**, so it takes 150% damage WHILE the shield holds — the deliberate counterweight to having one. That divides the pool's worth: a shield point is really **0.3762 HP**, and the roster-wide gap is 38.6% rather than the 57.8% a shield-only reading gives. `shield_damage_multiplier` and `shield_hp_per_point` are published per actor.

## The gap

* Raw HP across these 56 actors: **12,812,500**
* Effective HP once the pool is counted: **20,559,511** (**+60.5%**)
* Implied price change if the formula read effective HP: median **×1.393**, max **×1.784**

⚠ **Retiring the 150% multiplier is a BUFF that must be paid for.** The numbers above already account for it, so they price the game AS IT IS. Delete `DamageMultiplier@shielded` and a shield point jumps from 0.376 to 0.564 HP — the same pool becomes 1.5x more valuable and the implied price rises again. Re-extract AFTER the deletion and price once, or these units get charged for durability they no longer have (or keep durability they were never charged for).

## Per actor

| pack | actor | HP | shield pool | effective HP | ×HP | cost | implied ×price |
|---|---|--:|--:|--:|--:|--:|--:|
| redalert2mod_consortium | `steelconsortium_defenderbot` | 210,000 | 420,000 | 446,981 | ×2.128 | 3,200 | ×1.784 |
| redalert2mod_consortium | `steelconsortium_skyhammer` | 120,000 | 240,000 | 255,418 | ×2.128 | 4,500 | ×1.777 |
| redalert2mod_consortium | `steelconsortium_katytank` | 275,000 | 550,000 | 585,332 | ×2.128 | 3,800 | ×1.741 |
| redalert2mod_consortium | `steelconsortium_stalker` | 140,000 | 280,000 | 297,987 | ×2.128 | 4,000 | ×1.723 |
| redalert2mod_consortium | `steelconsortium_whiterabbit` | 150,000 | 300,000 | 319,272 | ×2.128 | 4,500 | ×1.595 |
| starcraft_protoss | `protoss_cyberneticscore` | 150,000 | 150,000 | 234,636 | ×1.564 | 1,500 | ×1.564 |
| starcraft_protoss | `protoss_forge` | 150,000 | 150,000 | 234,636 | ×1.564 | 1,500 | ×1.564 |
| starcraft_protoss | `protoss_roboticsfacility` | 400,000 | 400,000 | 625,696 | ×1.564 | 2,000 | ×1.564 |
| starcraft_protoss | `protoss_fleetbeacon` | 1,000,000 | 1,000,000 | 1,564,240 | ×1.564 | 10,000 | ×1.564 |
| starcraft_protoss | `protoss_nexus` | 1,000,000 | 1,000,000 | 1,564,240 | ×1.564 | 5,000 | ×1.564 |
| starcraft_protoss | `protoss_assimilator` | 300,000 | 300,000 | 469,272 | ×1.564 | 3,000 | ×1.564 |
| starcraft_protoss | `protoss_stargate` | 300,000 | 300,000 | 469,272 | ×1.564 | 1,500 | ×1.564 |
| starcraft_protoss | `protoss_arbitertribunal` | 250,000 | 250,000 | 391,060 | ×1.564 | 2,500 | ×1.564 |
| starcraft_protoss | `protoss_observatory` | 250,000 | 250,000 | 391,060 | ×1.564 | 2,500 | ×1.564 |
| starcraft_protoss | `protoss_pylon` | 250,000 | 250,000 | 391,060 | ×1.564 | 1,000 | ×1.564 |
| starcraft_protoss | `protoss_templararchives` | 250,000 | 250,000 | 391,060 | ×1.564 | 2,500 | ×1.564 |
| starcraft_protoss | `protoss_citadelofadun` | 200,000 | 200,000 | 312,848 | ×1.564 | 2,000 | ×1.564 |
| starcraft_protoss | `protoss_gateway` | 200,000 | 200,000 | 312,848 | ×1.564 | 1,000 | ×1.564 |
| starcraft_protoss | `protoss_roboticssupportbay` | 200,000 | 200,000 | 312,848 | ×1.564 | 2,000 | ×1.564 |
| starcraft_protoss | `protoss_mobilenexus` | 300,000 | 300,000 | 469,272 | ×1.564 | 5,000 | ×1.513 |
| redalert2mod_consortium | `cruiser_f.steel` | 37,500 | 75,000 | 79,818 | ×2.128 | 125 | ×1.479 |
| starcraft_protoss | `protoss_starshipsovereign` | 750,000 | 750,000 | 1,173,180 | ×1.564 | 10,000 | ×1.466 |
| redalert2mod_consortium | `steelconsortium_supportshieldgenerator` | 100,000 | 200,000 | 212,848 | ×2.128 | 3,000 | ×1.451 |
| redalert2mod_consortium | `steelconsortium_empressstation` | 1,500,000 | 1,500,000 | 2,346,360 | ×1.564 | 5,000 | ×1.446 |
| starcraft_protoss | `protoss_archon` | 350,000 | 350,000 | 547,484 | ×1.564 | 5,600 | ×1.425 |
| starcraft_protoss | `protoss_epigraph` | 200,000 | 200,000 | 312,848 | ×1.564 | 2,600 | ×1.412 |
| starcraft_protoss | `protoss_shuttle` | 100,000 | 100,000 | 156,424 | ×1.564 | 6,000 | ×1.410 |
| starcraft_protoss | `protoss_carrier` | 300,000 | 300,000 | 469,272 | ×1.564 | 3,000 | ×1.395 |
| starcraft_protoss | `protoss_idol` | 350,000 | 350,000 | 547,484 | ×1.564 | 2,800 | ×1.391 |
| starcraft_protoss | `protoss_arbiter` | 225,000 | 225,000 | 351,954 | ×1.564 | 4,800 | ×1.387 |
| starcraft_protoss | `protoss_atreus` | 250,000 | 250,000 | 391,060 | ×1.564 | 2,400 | ×1.381 |
| starcraft_protoss | `protoss_zeratul` | 250,000 | 250,000 | 391,060 | ×1.564 | 4,000 | ×1.380 |
| starcraft_protoss | `protoss_corsair` | 125,000 | 125,000 | 195,530 | ×1.564 | 2,100 | ×1.375 |
| redalert2mod_consortium | `steelconsortium_cloudbreaker` | 250,000 | 250,000 | 391,060 | ×1.564 | 5,000 | ×1.371 |
| starcraft_protoss | `protoss_reaver` | 275,000 | 275,000 | 430,166 | ×1.564 | 2,700 | ×1.359 |
| redalert2mod_consortium | `cougar.steel` | 25,000 | 50,000 | 53,212 | ×2.128 | 1,200 | ×1.329 |
| starcraft_protoss | `protoss_voidray` | 70,000 | 70,000 | 109,497 | ×1.564 | 1,400 | ×1.328 |
| starcraft_protoss | `protoss_scout` | 75,000 | 75,000 | 117,318 | ×1.564 | 1,500 | ×1.328 |
| redalert2mod_consortium | `hummer.steel` | 45,000 | 67,500 | 83,086 | ×1.846 | 2,000 | ×1.326 |
| redalert2mod_consortium | `oldqtnk.steel` | 112,500 | 112,500 | 175,977 | ×1.564 | 2,400 | ×1.322 |
| starcraft_protoss | `protoss_shieldbattery` | 100,000 | 100,000 | 156,424 | ×1.564 | 1,000 | ×1.282 |
| starcraft_protoss | `protoss_positron` | 60,000 | 60,000 | 93,854 | ×1.564 | 1,200 | ×1.273 |
| starcraft_protoss | `protoss_dragoon` | 75,000 | 75,000 | 117,318 | ×1.564 | 1,200 | ×1.253 |
| starcraft_protoss | `protoss_gladius` | 100,000 | 100,000 | 156,424 | ×1.564 | 1,800 | ×1.251 |
| starcraft_protoss | `protoss_patriarch` | 75,000 | 75,000 | 117,318 | ×1.564 | 4,000 | ×1.248 |
| redalert2mod_consortium | `cobra.steel` | 325,000 | 162,500 | 416,689 | ×1.282 | 3,600 | ×1.208 |
| starcraft_protoss | `protoss_probe` | 45,000 | 45,000 | 70,391 | ×1.564 | 500 | ×1.198 |
| starcraft_protoss | `protoss_analogue` | 60,000 | 60,000 | 93,854 | ×1.564 | 1,200 | ×1.198 |
| starcraft_protoss | `protoss_amaranth` | 70,000 | 70,000 | 109,497 | ×1.564 | 1,200 | ×1.189 |
| starcraft_protoss | `protoss_observer` | 12,500 | 12,500 | 19,553 | ×1.564 | 500 | ×1.177 |
| starcraft_protoss | `protoss_darktemplar` | 50,000 | 50,000 | 78,212 | ×1.564 | 600 | ×1.169 |
| starcraft_protoss | `protoss_zealot` | 40,000 | 40,000 | 62,570 | ×1.564 | 300 | ×1.154 |
| starcraft_protoss | `protoss_legionnaire` | 60,000 | 60,000 | 93,854 | ×1.564 | 700 | ×1.149 |
| starcraft_protoss | `protoss_adept` | 30,000 | 30,000 | 46,927 | ×1.564 | 650 | ×1.136 |
| starcraft_protoss | `protoss_manifold` | 25,000 | 25,000 | 39,106 | ×1.564 | 600 | ×1.124 |
| starcraft_protoss | `protoss_photoncannon` | 200,000 | 200,000 | 312,848 | ×1.564 | 2,000 | ×1.015 |
