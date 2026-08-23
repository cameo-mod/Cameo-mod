# audit_survivability_pricing — E1: the baseline shield is priced at ZERO

| bucket | actors | priced today | belongs to |
|---|--:|---|---|
| spawns with a pool (**always-on**) | 56 | ✖ nothing | **E1 — this report** |
| empty capacity, needs `shieldgen` | 1163 | ✔ correctly nothing | — (it has no shield) |
| pool behind an upgrade | 210 | ✖ nothing | E5 (upgrade pricing) |

Shield row mean Versus **187.44**, so one shield point is **0.5335 HP** BEFORE any shield-gated `DamageMultiplier` — measured off the live ladder every run, never frozen. The Shield row takes **1.467%** of all roster raw damage at baseline.

⚠ **Every one of these 56 actors also carries `DamageMultiplier@shielded: 150`**, so it takes 150% damage WHILE the shield holds — the deliberate counterweight to having one. That divides the pool's worth: a shield point is really **0.3557 HP**, and the roster-wide gap is 38.6% rather than the 57.8% a shield-only reading gives. `shield_damage_multiplier` and `shield_hp_per_point` are published per actor.

## The gap

* Raw HP across these 56 actors: **12,812,500**
* Effective HP once the pool is counted: **20,137,464** (**+57.2%**)
* Implied price change if the formula read effective HP: median **×1.374**, max **×1.743**

⚠ **Retiring the 150% multiplier is a BUFF that must be paid for.** The numbers above already account for it, so they price the game AS IT IS. Delete `DamageMultiplier@shielded` and a shield point jumps from 0.356 to 0.534 HP — the same pool becomes 1.5x more valuable and the implied price rises again. Re-extract AFTER the deletion and price once, or these units get charged for durability they no longer have (or keep durability they were never charged for).

## Per actor

| pack | actor | HP | shield pool | effective HP | ×HP | cost | implied ×price |
|---|---|--:|--:|--:|--:|--:|--:|
| redalert2mod_consortium | `steelconsortium_defenderbot` | 210,000 | 420,000 | 434,070 | ×2.067 | 3,200 | ×1.743 |
| redalert2mod_consortium | `steelconsortium_skyhammer` | 120,000 | 240,000 | 248,040 | ×2.067 | 4,500 | ×1.735 |
| redalert2mod_consortium | `steelconsortium_katytank` | 275,000 | 550,000 | 568,425 | ×2.067 | 3,800 | ×1.700 |
| redalert2mod_consortium | `steelconsortium_stalker` | 140,000 | 280,000 | 289,380 | ×2.067 | 4,000 | ×1.683 |
| redalert2mod_consortium | `steelconsortium_whiterabbit` | 150,000 | 300,000 | 310,050 | ×2.067 | 4,500 | ×1.565 |
| starcraft_protoss | `protoss_arbitertribunal` | 250,000 | 250,000 | 383,375 | ×1.534 | 2,500 | ×1.534 |
| starcraft_protoss | `protoss_observatory` | 250,000 | 250,000 | 383,375 | ×1.534 | 2,500 | ×1.534 |
| starcraft_protoss | `protoss_pylon` | 250,000 | 250,000 | 383,375 | ×1.534 | 1,000 | ×1.534 |
| starcraft_protoss | `protoss_templararchives` | 250,000 | 250,000 | 383,375 | ×1.534 | 2,500 | ×1.534 |
| starcraft_protoss | `protoss_roboticsfacility` | 400,000 | 400,000 | 613,400 | ×1.534 | 2,000 | ×1.534 |
| starcraft_protoss | `protoss_fleetbeacon` | 1,000,000 | 1,000,000 | 1,533,501 | ×1.534 | 10,000 | ×1.534 |
| starcraft_protoss | `protoss_nexus` | 1,000,000 | 1,000,000 | 1,533,501 | ×1.534 | 5,000 | ×1.534 |
| starcraft_protoss | `protoss_assimilator` | 300,000 | 300,000 | 460,050 | ×1.534 | 3,000 | ×1.534 |
| starcraft_protoss | `protoss_cyberneticscore` | 150,000 | 150,000 | 230,025 | ×1.534 | 1,500 | ×1.534 |
| starcraft_protoss | `protoss_forge` | 150,000 | 150,000 | 230,025 | ×1.534 | 1,500 | ×1.534 |
| starcraft_protoss | `protoss_stargate` | 300,000 | 300,000 | 460,050 | ×1.534 | 1,500 | ×1.534 |
| starcraft_protoss | `protoss_citadelofadun` | 200,000 | 200,000 | 306,700 | ×1.534 | 2,000 | ×1.534 |
| starcraft_protoss | `protoss_gateway` | 200,000 | 200,000 | 306,700 | ×1.534 | 1,000 | ×1.534 |
| starcraft_protoss | `protoss_roboticssupportbay` | 200,000 | 200,000 | 306,700 | ×1.534 | 2,000 | ×1.534 |
| redalert2mod_consortium | `cruiser_f.steel` | 37,500 | 75,000 | 77,513 | ×2.067 | 125 | ×1.523 |
| starcraft_protoss | `protoss_mobilenexus` | 300,000 | 300,000 | 460,050 | ×1.534 | 5,000 | ×1.485 |
| starcraft_protoss | `protoss_starshipsovereign` | 750,000 | 750,000 | 1,150,126 | ×1.534 | 10,000 | ×1.446 |
| redalert2mod_consortium | `steelconsortium_supportshieldgenerator` | 100,000 | 200,000 | 206,700 | ×2.067 | 3,000 | ×1.427 |
| redalert2mod_consortium | `steelconsortium_empressstation` | 1,500,000 | 1,500,000 | 2,300,251 | ×1.534 | 5,000 | ×1.422 |
| starcraft_protoss | `protoss_archon` | 350,000 | 350,000 | 536,725 | ×1.534 | 5,600 | ×1.402 |
| starcraft_protoss | `protoss_epigraph` | 200,000 | 200,000 | 306,700 | ×1.534 | 2,600 | ×1.391 |
| starcraft_protoss | `protoss_shuttle` | 100,000 | 100,000 | 153,350 | ×1.534 | 6,000 | ×1.388 |
| starcraft_protoss | `protoss_arbiter` | 225,000 | 225,000 | 345,038 | ×1.534 | 4,800 | ×1.375 |
| starcraft_protoss | `protoss_carrier` | 300,000 | 300,000 | 460,050 | ×1.534 | 3,000 | ×1.373 |
| starcraft_protoss | `protoss_idol` | 350,000 | 350,000 | 536,725 | ×1.534 | 2,800 | ×1.371 |
| starcraft_protoss | `protoss_atreus` | 250,000 | 250,000 | 383,375 | ×1.534 | 2,400 | ×1.367 |
| starcraft_protoss | `protoss_zeratul` | 250,000 | 250,000 | 383,375 | ×1.534 | 4,000 | ×1.361 |
| redalert2mod_consortium | `cougar.steel` | 25,000 | 50,000 | 51,675 | ×2.067 | 1,200 | ×1.360 |
| starcraft_protoss | `protoss_corsair` | 125,000 | 125,000 | 191,688 | ×1.534 | 2,100 | ×1.354 |
| redalert2mod_consortium | `steelconsortium_cloudbreaker` | 250,000 | 250,000 | 383,375 | ×1.534 | 5,000 | ×1.351 |
| starcraft_protoss | `protoss_reaver` | 275,000 | 275,000 | 421,713 | ×1.534 | 2,700 | ×1.337 |
| redalert2mod_consortium | `hummer.steel` | 45,000 | 67,500 | 81,011 | ×1.800 | 2,000 | ×1.324 |
| starcraft_protoss | `protoss_scout` | 75,000 | 75,000 | 115,013 | ×1.534 | 1,500 | ×1.310 |
| redalert2mod_consortium | `oldqtnk.steel` | 112,500 | 112,500 | 172,519 | ×1.534 | 2,400 | ×1.304 |
| starcraft_protoss | `protoss_voidray` | 70,000 | 70,000 | 107,345 | ×1.534 | 1,400 | ×1.302 |
| starcraft_protoss | `protoss_shieldbattery` | 100,000 | 100,000 | 153,350 | ×1.534 | 1,000 | ×1.267 |
| starcraft_protoss | `protoss_positron` | 60,000 | 60,000 | 92,010 | ×1.534 | 1,200 | ×1.260 |
| starcraft_protoss | `protoss_dragoon` | 75,000 | 75,000 | 115,013 | ×1.534 | 1,200 | ×1.248 |
| starcraft_protoss | `protoss_patriarch` | 75,000 | 75,000 | 115,013 | ×1.534 | 4,000 | ×1.240 |
| starcraft_protoss | `protoss_gladius` | 100,000 | 100,000 | 153,350 | ×1.534 | 1,800 | ×1.237 |
| redalert2mod_consortium | `cobra.steel` | 325,000 | 162,500 | 411,694 | ×1.267 | 3,600 | ×1.197 |
| starcraft_protoss | `protoss_analogue` | 60,000 | 60,000 | 92,010 | ×1.534 | 1,200 | ×1.191 |
| starcraft_protoss | `protoss_probe` | 45,000 | 45,000 | 69,008 | ×1.534 | 500 | ×1.191 |
| starcraft_protoss | `protoss_amaranth` | 70,000 | 70,000 | 107,345 | ×1.534 | 1,200 | ×1.181 |
| starcraft_protoss | `protoss_observer` | 12,500 | 12,500 | 19,169 | ×1.534 | 500 | ×1.168 |
| starcraft_protoss | `protoss_darktemplar` | 50,000 | 50,000 | 76,675 | ×1.534 | 600 | ×1.160 |
| starcraft_protoss | `protoss_zealot` | 40,000 | 40,000 | 61,340 | ×1.534 | 300 | ×1.150 |
| starcraft_protoss | `protoss_legionnaire` | 60,000 | 60,000 | 92,010 | ×1.534 | 700 | ×1.145 |
| starcraft_protoss | `protoss_adept` | 30,000 | 30,000 | 46,005 | ×1.534 | 650 | ×1.125 |
| starcraft_protoss | `protoss_manifold` | 25,000 | 25,000 | 38,338 | ×1.534 | 600 | ×1.117 |
| starcraft_protoss | `protoss_photoncannon` | 200,000 | 200,000 | 306,700 | ×1.534 | 2,000 | ×1.063 |
