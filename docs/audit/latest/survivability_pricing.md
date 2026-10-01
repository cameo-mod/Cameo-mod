# audit_survivability_pricing — E1: the baseline shield is priced at ZERO

| bucket | actors | priced today | belongs to |
|---|--:|---|---|
| spawns with a pool (**always-on**) | 56 | ✖ nothing | **E1 — this report** |
| empty capacity, needs `shieldgen` | 1335 | ✔ correctly nothing | — (it has no shield) |
| pool behind an upgrade | 215 | ✖ nothing | E5 (upgrade pricing) |

Shield row mean Versus **184.71**, so one shield point is **0.5414 HP** BEFORE any shield-gated `DamageMultiplier` — measured off the live ladder every run, never frozen. The Shield row takes **1.605%** of all roster raw damage at baseline.

⚠ **Every one of these 56 actors also carries `DamageMultiplier@shielded: 150`**, so it takes 150% damage WHILE the shield holds — the deliberate counterweight to having one. That divides the pool's worth: a shield point is really **0.3609 HP**, and the roster-wide gap is 38.6% rather than the 57.8% a shield-only reading gives. `shield_damage_multiplier` and `shield_hp_per_point` are published per actor.

## The gap

* Raw HP across these 56 actors: **12,812,500**
* Effective HP once the pool is counted: **26,877,864** (**+109.8%**)
* Implied price change if the formula read effective HP: median **×1.713**, max **×2.424**

⚠ **Retiring the 150% multiplier is a BUFF that must be paid for.** The numbers above already account for it, so they price the game AS IT IS. Delete `DamageMultiplier@shielded` and a shield point jumps from 0.361 to 0.541 HP — the same pool becomes 1.5x more valuable and the implied price rises again. Re-extract AFTER the deletion and price once, or these units get charged for durability they no longer have (or keep durability they were never charged for).

## Per actor

| pack | actor | HP | shield pool | effective HP | ×HP | cost | implied ×price |
|---|---|--:|--:|--:|--:|--:|--:|
| redalert2mod_consortium | `steelconsortium_defenderbot` | 210,000 | 420,000 | 640,259 | ×3.049 | 3,200 | ×2.424 |
| redalert2mod_consortium | `steelconsortium_skyhammer` | 120,000 | 240,000 | 365,862 | ×3.049 | 4,500 | ×2.410 |
| redalert2mod_consortium | `steelconsortium_katytank` | 275,000 | 550,000 | 838,434 | ×3.049 | 3,800 | ×2.345 |
| redalert2mod_consortium | `steelconsortium_stalker` | 140,000 | 280,000 | 426,839 | ×3.049 | 4,000 | ×2.312 |
| redalert2mod_consortium | `steelconsortium_whiterabbit` | 150,000 | 300,000 | 457,328 | ×3.049 | 4,500 | ×2.079 |
| starcraft_protoss | `protoss_roboticsfacility` | 400,000 | 400,000 | 809,770 | ×2.024 | 2,000 | ×2.024 |
| starcraft_protoss | `protoss_assimilator` | 300,000 | 300,000 | 607,328 | ×2.024 | 3,000 | ×2.024 |
| starcraft_protoss | `protoss_stargate` | 300,000 | 300,000 | 607,328 | ×2.024 | 1,500 | ×2.024 |
| starcraft_protoss | `protoss_arbitertribunal` | 250,000 | 250,000 | 506,106 | ×2.024 | 2,500 | ×2.024 |
| starcraft_protoss | `protoss_fleetbeacon` | 1,000,000 | 1,000,000 | 2,024,426 | ×2.024 | 10,000 | ×2.024 |
| starcraft_protoss | `protoss_nexus` | 1,000,000 | 1,000,000 | 2,024,426 | ×2.024 | 5,000 | ×2.024 |
| starcraft_protoss | `protoss_observatory` | 250,000 | 250,000 | 506,106 | ×2.024 | 2,500 | ×2.024 |
| starcraft_protoss | `protoss_pylon` | 250,000 | 250,000 | 506,106 | ×2.024 | 1,000 | ×2.024 |
| starcraft_protoss | `protoss_templararchives` | 250,000 | 250,000 | 506,106 | ×2.024 | 2,500 | ×2.024 |
| starcraft_protoss | `protoss_citadelofadun` | 200,000 | 200,000 | 404,885 | ×2.024 | 2,000 | ×2.024 |
| starcraft_protoss | `protoss_gateway` | 200,000 | 200,000 | 404,885 | ×2.024 | 1,000 | ×2.024 |
| starcraft_protoss | `protoss_roboticssupportbay` | 200,000 | 200,000 | 404,885 | ×2.024 | 2,000 | ×2.024 |
| starcraft_protoss | `protoss_cyberneticscore` | 150,000 | 150,000 | 303,664 | ×2.024 | 1,500 | ×2.024 |
| starcraft_protoss | `protoss_forge` | 150,000 | 150,000 | 303,664 | ×2.024 | 1,500 | ×2.024 |
| starcraft_protoss | `protoss_mobilenexus` | 300,000 | 300,000 | 607,328 | ×2.024 | 5,000 | ×1.931 |
| redalert2mod_consortium | `steel_cruiser_f` | 37,500 | 75,000 | 114,332 | ×3.049 | 125 | ×1.858 |
| starcraft_protoss | `protoss_starshipsovereign` | 750,000 | 750,000 | 1,518,319 | ×2.024 | 10,000 | ×1.846 |
| redalert2mod_consortium | `steelconsortium_supportshieldgenerator` | 100,000 | 200,000 | 304,885 | ×3.049 | 3,000 | ×1.820 |
| redalert2mod_consortium | `steelconsortium_empressstation` | 1,500,000 | 1,500,000 | 3,036,638 | ×2.024 | 5,000 | ×1.811 |
| starcraft_protoss | `protoss_archon` | 350,000 | 350,000 | 708,549 | ×2.024 | 5,600 | ×1.773 |
| starcraft_protoss | `protoss_epigraph` | 200,000 | 200,000 | 404,885 | ×2.024 | 2,600 | ×1.748 |
| starcraft_protoss | `protoss_shuttle` | 100,000 | 100,000 | 202,443 | ×2.024 | 6,000 | ×1.745 |
| starcraft_protoss | `protoss_carrier` | 300,000 | 300,000 | 607,328 | ×2.024 | 3,000 | ×1.716 |
| starcraft_protoss | `protoss_idol` | 350,000 | 350,000 | 708,549 | ×2.024 | 2,800 | ×1.710 |
| starcraft_protoss | `protoss_arbiter` | 225,000 | 225,000 | 455,496 | ×2.024 | 4,800 | ×1.702 |
| starcraft_protoss | `protoss_atreus` | 250,000 | 250,000 | 506,106 | ×2.024 | 2,400 | ×1.691 |
| starcraft_protoss | `protoss_zeratul` | 250,000 | 250,000 | 506,106 | ×2.024 | 4,000 | ×1.689 |
| starcraft_protoss | `protoss_corsair` | 125,000 | 125,000 | 253,053 | ×2.024 | 2,100 | ×1.679 |
| redalert2mod_consortium | `steelconsortium_cloudbreaker` | 250,000 | 250,000 | 506,106 | ×2.024 | 5,000 | ×1.673 |
| starcraft_protoss | `protoss_reaver` | 275,000 | 275,000 | 556,717 | ×2.024 | 2,700 | ×1.651 |
| redalert2mod_consortium | `steel_cougar` | 25,000 | 50,000 | 76,221 | ×3.049 | 1,200 | ×1.598 |
| starcraft_protoss | `protoss_voidray` | 70,000 | 70,000 | 141,710 | ×2.024 | 1,400 | ×1.596 |
| starcraft_protoss | `protoss_scout` | 75,000 | 75,000 | 151,832 | ×2.024 | 1,500 | ×1.595 |
| redalert2mod_consortium | `steel_hummer` | 45,000 | 67,500 | 114,149 | ×2.537 | 2,000 | ×1.593 |
| redalert2mod_consortium | `steel_oldqtnk` | 112,500 | 112,500 | 227,748 | ×2.024 | 2,400 | ×1.584 |
| starcraft_protoss | `protoss_shieldbattery` | 100,000 | 100,000 | 202,443 | ×2.024 | 1,000 | ×1.512 |
| starcraft_protoss | `protoss_positron` | 60,000 | 60,000 | 121,466 | ×2.024 | 1,200 | ×1.497 |
| starcraft_protoss | `protoss_dragoon` | 75,000 | 75,000 | 151,832 | ×2.024 | 1,200 | ×1.460 |
| starcraft_protoss | `protoss_gladius` | 100,000 | 100,000 | 202,443 | ×2.024 | 1,800 | ×1.455 |
| starcraft_protoss | `protoss_patriarch` | 75,000 | 75,000 | 151,832 | ×2.024 | 4,000 | ×1.449 |
| redalert2mod_consortium | `steel_cobra` | 325,000 | 162,500 | 491,469 | ×1.512 | 3,600 | ×1.378 |
| starcraft_protoss | `protoss_probe` | 45,000 | 45,000 | 91,099 | ×2.024 | 500 | ×1.357 |
| starcraft_protoss | `protoss_analogue` | 60,000 | 60,000 | 121,466 | ×2.024 | 1,200 | ×1.356 |
| starcraft_protoss | `protoss_amaranth` | 70,000 | 70,000 | 141,710 | ×2.024 | 1,200 | ×1.341 |
| starcraft_protoss | `protoss_observer` | 12,500 | 12,500 | 25,305 | ×2.024 | 500 | ×1.322 |
| starcraft_protoss | `protoss_darktemplar` | 50,000 | 50,000 | 101,221 | ×2.024 | 600 | ×1.304 |
| starcraft_protoss | `protoss_zealot` | 40,000 | 40,000 | 80,977 | ×2.024 | 300 | ×1.276 |
| starcraft_protoss | `protoss_legionnaire` | 60,000 | 60,000 | 121,466 | ×2.024 | 700 | ×1.269 |
| starcraft_protoss | `protoss_adept` | 30,000 | 30,000 | 60,733 | ×2.024 | 650 | ×1.246 |
| starcraft_protoss | `protoss_manifold` | 25,000 | 25,000 | 50,611 | ×2.024 | 600 | ×1.225 |
| starcraft_protoss | `protoss_photoncannon` | 200,000 | 200,000 | 404,885 | ×2.024 | 2,000 | ×1.026 |
