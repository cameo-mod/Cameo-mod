# audit_survivability_pricing — E1: the baseline shield is priced at ZERO

| bucket | actors | priced today | belongs to |
|---|--:|---|---|
| spawns with a pool (**always-on**) | 56 | ✖ nothing | **E1 — this report** |
| empty capacity, needs `shieldgen` | 1163 | ✔ correctly nothing | — (it has no shield) |
| pool behind an upgrade | 210 | ✖ nothing | E5 (upgrade pricing) |

Shield row mean Versus **185.08**, so one shield point is **0.5403 HP** BEFORE any shield-gated `DamageMultiplier` — measured off the live ladder every run, never frozen. The Shield row takes **1.436%** of all roster raw damage at baseline.

⚠ **Every one of these 56 actors also carries `DamageMultiplier@shielded: 150`**, so it takes 150% damage WHILE the shield holds — the deliberate counterweight to having one. That divides the pool's worth: a shield point is really **0.3602 HP**, and the roster-wide gap is 38.6% rather than the 57.8% a shield-only reading gives. `shield_damage_multiplier` and `shield_hp_per_point` are published per actor.

## The gap

* Raw HP across these 56 actors: **12,812,500**
* Effective HP once the pool is counted: **20,231,038** (**+57.9%**)
* Implied price change if the formula read effective HP: median **×1.379**, max **×1.753**

⚠ **Retiring the 150% multiplier is a BUFF that must be paid for.** The numbers above already account for it, so they price the game AS IT IS. Delete `DamageMultiplier@shielded` and a shield point jumps from 0.360 to 0.540 HP — the same pool becomes 1.5x more valuable and the implied price rises again. Re-extract AFTER the deletion and price once, or these units get charged for durability they no longer have (or keep durability they were never charged for).

## Per actor

| pack | actor | HP | shield pool | effective HP | ×HP | cost | implied ×price |
|---|---|--:|--:|--:|--:|--:|--:|
| redalert2mod_consortium | `steelconsortium_defenderbot` | 210,000 | 420,000 | 436,933 | ×2.081 | 3,200 | ×1.753 |
| redalert2mod_consortium | `steelconsortium_skyhammer` | 120,000 | 240,000 | 249,676 | ×2.081 | 4,500 | ×1.745 |
| redalert2mod_consortium | `steelconsortium_katytank` | 275,000 | 550,000 | 572,174 | ×2.081 | 3,800 | ×1.708 |
| redalert2mod_consortium | `steelconsortium_stalker` | 140,000 | 280,000 | 291,288 | ×2.081 | 4,000 | ×1.692 |
| redalert2mod_consortium | `steelconsortium_whiterabbit` | 150,000 | 300,000 | 312,095 | ×2.081 | 4,500 | ×1.572 |
| redalert2mod_consortium | `cruiser_f.steel` | 37,500 | 75,000 | 78,024 | ×2.081 | 125 | ×1.564 |
| starcraft_protoss | `protoss_arbitertribunal` | 250,000 | 250,000 | 385,079 | ×1.540 | 2,500 | ×1.540 |
| starcraft_protoss | `protoss_assimilator` | 300,000 | 300,000 | 462,095 | ×1.540 | 3,000 | ×1.540 |
| starcraft_protoss | `protoss_cyberneticscore` | 150,000 | 150,000 | 231,047 | ×1.540 | 1,500 | ×1.540 |
| starcraft_protoss | `protoss_forge` | 150,000 | 150,000 | 231,047 | ×1.540 | 1,500 | ×1.540 |
| starcraft_protoss | `protoss_observatory` | 250,000 | 250,000 | 385,079 | ×1.540 | 2,500 | ×1.540 |
| starcraft_protoss | `protoss_pylon` | 250,000 | 250,000 | 385,079 | ×1.540 | 1,000 | ×1.540 |
| starcraft_protoss | `protoss_stargate` | 300,000 | 300,000 | 462,095 | ×1.540 | 1,500 | ×1.540 |
| starcraft_protoss | `protoss_templararchives` | 250,000 | 250,000 | 385,079 | ×1.540 | 2,500 | ×1.540 |
| starcraft_protoss | `protoss_citadelofadun` | 200,000 | 200,000 | 308,063 | ×1.540 | 2,000 | ×1.540 |
| starcraft_protoss | `protoss_gateway` | 200,000 | 200,000 | 308,063 | ×1.540 | 1,000 | ×1.540 |
| starcraft_protoss | `protoss_roboticsfacility` | 400,000 | 400,000 | 616,126 | ×1.540 | 2,000 | ×1.540 |
| starcraft_protoss | `protoss_roboticssupportbay` | 200,000 | 200,000 | 308,063 | ×1.540 | 2,000 | ×1.540 |
| starcraft_protoss | `protoss_fleetbeacon` | 1,000,000 | 1,000,000 | 1,540,316 | ×1.540 | 10,000 | ×1.540 |
| starcraft_protoss | `protoss_nexus` | 1,000,000 | 1,000,000 | 1,540,316 | ×1.540 | 5,000 | ×1.540 |
| starcraft_protoss | `protoss_mobilenexus` | 300,000 | 300,000 | 462,095 | ×1.540 | 5,000 | ×1.491 |
| starcraft_protoss | `protoss_starshipsovereign` | 750,000 | 750,000 | 1,155,237 | ×1.540 | 10,000 | ×1.451 |
| redalert2mod_consortium | `steelconsortium_supportshieldgenerator` | 100,000 | 200,000 | 208,063 | ×2.081 | 3,000 | ×1.432 |
| redalert2mod_consortium | `steelconsortium_empressstation` | 1,500,000 | 1,500,000 | 2,310,474 | ×1.540 | 5,000 | ×1.428 |
| starcraft_protoss | `protoss_archon` | 350,000 | 350,000 | 539,111 | ×1.540 | 5,600 | ×1.407 |
| starcraft_protoss | `protoss_epigraph` | 200,000 | 200,000 | 308,063 | ×1.540 | 2,600 | ×1.395 |
| starcraft_protoss | `protoss_shuttle` | 100,000 | 100,000 | 154,032 | ×1.540 | 6,000 | ×1.393 |
| starcraft_protoss | `protoss_arbiter` | 225,000 | 225,000 | 346,571 | ×1.540 | 4,800 | ×1.379 |
| starcraft_protoss | `protoss_carrier` | 300,000 | 300,000 | 462,095 | ×1.540 | 3,000 | ×1.378 |
| starcraft_protoss | `protoss_idol` | 350,000 | 350,000 | 539,111 | ×1.540 | 2,800 | ×1.374 |
| redalert2mod_consortium | `cougar.steel` | 25,000 | 50,000 | 52,016 | ×2.081 | 1,200 | ×1.368 |
| starcraft_protoss | `protoss_atreus` | 250,000 | 250,000 | 385,079 | ×1.540 | 2,400 | ×1.366 |
| starcraft_protoss | `protoss_corsair` | 125,000 | 125,000 | 192,540 | ×1.540 | 2,100 | ×1.362 |
| starcraft_protoss | `protoss_zeratul` | 250,000 | 250,000 | 385,079 | ×1.540 | 4,000 | ×1.357 |
| redalert2mod_consortium | `steelconsortium_cloudbreaker` | 250,000 | 250,000 | 385,079 | ×1.540 | 5,000 | ×1.355 |
| starcraft_protoss | `protoss_reaver` | 275,000 | 275,000 | 423,587 | ×1.540 | 2,700 | ×1.340 |
| redalert2mod_consortium | `hummer.steel` | 45,000 | 67,500 | 81,471 | ×1.810 | 2,000 | ×1.339 |
| starcraft_protoss | `protoss_scout` | 75,000 | 75,000 | 115,524 | ×1.540 | 1,500 | ×1.315 |
| redalert2mod_consortium | `oldqtnk.steel` | 112,500 | 112,500 | 173,286 | ×1.540 | 2,400 | ×1.308 |
| starcraft_protoss | `protoss_voidray` | 70,000 | 70,000 | 107,822 | ×1.540 | 1,400 | ×1.306 |
| starcraft_protoss | `protoss_shieldbattery` | 100,000 | 100,000 | 154,032 | ×1.540 | 1,000 | ×1.270 |
| starcraft_protoss | `protoss_positron` | 60,000 | 60,000 | 92,419 | ×1.540 | 1,200 | ×1.264 |
| starcraft_protoss | `protoss_dragoon` | 75,000 | 75,000 | 115,524 | ×1.540 | 1,200 | ×1.251 |
| starcraft_protoss | `protoss_patriarch` | 75,000 | 75,000 | 115,524 | ×1.540 | 4,000 | ×1.243 |
| starcraft_protoss | `protoss_gladius` | 100,000 | 100,000 | 154,032 | ×1.540 | 1,800 | ×1.240 |
| redalert2mod_consortium | `cobra.steel` | 325,000 | 162,500 | 412,801 | ×1.270 | 3,600 | ×1.199 |
| starcraft_protoss | `protoss_amaranth` | 70,000 | 70,000 | 107,822 | ×1.540 | 1,200 | ×1.170 |
| starcraft_protoss | `protoss_observer` | 12,500 | 12,500 | 19,254 | ×1.540 | 500 | ×1.170 |
| starcraft_protoss | `protoss_probe` | 45,000 | 45,000 | 69,314 | ×1.540 | 500 | ×1.169 |
| starcraft_protoss | `protoss_analogue` | 60,000 | 60,000 | 92,419 | ×1.540 | 1,200 | ×1.166 |
| starcraft_protoss | `protoss_darktemplar` | 50,000 | 50,000 | 77,016 | ×1.540 | 600 | ×1.143 |
| starcraft_protoss | `protoss_legionnaire` | 60,000 | 60,000 | 92,419 | ×1.540 | 700 | ×1.127 |
| starcraft_protoss | `protoss_adept` | 30,000 | 30,000 | 46,210 | ×1.540 | 650 | ×1.124 |
| starcraft_protoss | `protoss_zealot` | 40,000 | 40,000 | 61,613 | ×1.540 | 300 | ×1.123 |
| starcraft_protoss | `protoss_manifold` | 25,000 | 25,000 | 38,508 | ×1.540 | 600 | ×1.118 |
| starcraft_protoss | `protoss_photoncannon` | 200,000 | 200,000 | 308,063 | ×1.540 | 2,000 | ×1.063 |
