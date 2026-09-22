# audit_survivability_pricing — E1: the baseline shield is priced at ZERO

| bucket | actors | priced today | belongs to |
|---|--:|---|---|
| spawns with a pool (**always-on**) | 56 | ✖ nothing | **E1 — this report** |
| empty capacity, needs `shieldgen` | 1266 | ✔ correctly nothing | — (it has no shield) |
| pool behind an upgrade | 233 | ✖ nothing | E5 (upgrade pricing) |

Shield row mean Versus **181.69**, so one shield point is **0.5504 HP** BEFORE any shield-gated `DamageMultiplier` — measured off the live ladder every run, never frozen. The Shield row takes **1.444%** of all roster raw damage at baseline.

⚠ **Every one of these 56 actors also carries `DamageMultiplier@shielded: 150`**, so it takes 150% damage WHILE the shield holds — the deliberate counterweight to having one. That divides the pool's worth: a shield point is really **0.3669 HP**, and the roster-wide gap is 38.6% rather than the 57.8% a shield-only reading gives. `shield_damage_multiplier` and `shield_hp_per_point` are published per actor.

## The gap

* Raw HP across these 56 actors: **12,812,500**
* Effective HP once the pool is counted: **20,397,391** (**+59.2%**)
* Implied price change if the formula read effective HP: median **×1.385**, max **×1.768**

⚠ **Retiring the 150% multiplier is a BUFF that must be paid for.** The numbers above already account for it, so they price the game AS IT IS. Delete `DamageMultiplier@shielded` and a shield point jumps from 0.367 to 0.550 HP — the same pool becomes 1.5x more valuable and the implied price rises again. Re-extract AFTER the deletion and price once, or these units get charged for durability they no longer have (or keep durability they were never charged for).

## Per actor

| pack | actor | HP | shield pool | effective HP | ×HP | cost | implied ×price |
|---|---|--:|--:|--:|--:|--:|--:|
| redalert2mod_consortium | `steelconsortium_defenderbot` | 210,000 | 420,000 | 442,021 | ×2.105 | 3,200 | ×1.768 |
| redalert2mod_consortium | `steelconsortium_skyhammer` | 120,000 | 240,000 | 252,584 | ×2.105 | 4,500 | ×1.760 |
| redalert2mod_consortium | `steelconsortium_katytank` | 275,000 | 550,000 | 578,838 | ×2.105 | 3,800 | ×1.725 |
| redalert2mod_consortium | `steelconsortium_stalker` | 140,000 | 280,000 | 294,681 | ×2.105 | 4,000 | ×1.707 |
| redalert2mod_consortium | `steelconsortium_whiterabbit` | 150,000 | 300,000 | 315,730 | ×2.105 | 4,500 | ×1.583 |
| starcraft_protoss | `protoss_arbitertribunal` | 250,000 | 250,000 | 388,108 | ×1.552 | 2,500 | ×1.552 |
| starcraft_protoss | `protoss_observatory` | 250,000 | 250,000 | 388,108 | ×1.552 | 2,500 | ×1.552 |
| starcraft_protoss | `protoss_pylon` | 250,000 | 250,000 | 388,108 | ×1.552 | 1,000 | ×1.552 |
| starcraft_protoss | `protoss_templararchives` | 250,000 | 250,000 | 388,108 | ×1.552 | 2,500 | ×1.552 |
| starcraft_protoss | `protoss_citadelofadun` | 200,000 | 200,000 | 310,486 | ×1.552 | 2,000 | ×1.552 |
| starcraft_protoss | `protoss_gateway` | 200,000 | 200,000 | 310,486 | ×1.552 | 1,000 | ×1.552 |
| starcraft_protoss | `protoss_roboticsfacility` | 400,000 | 400,000 | 620,973 | ×1.552 | 2,000 | ×1.552 |
| starcraft_protoss | `protoss_roboticssupportbay` | 200,000 | 200,000 | 310,486 | ×1.552 | 2,000 | ×1.552 |
| starcraft_protoss | `protoss_assimilator` | 300,000 | 300,000 | 465,730 | ×1.552 | 3,000 | ×1.552 |
| starcraft_protoss | `protoss_cyberneticscore` | 150,000 | 150,000 | 232,865 | ×1.552 | 1,500 | ×1.552 |
| starcraft_protoss | `protoss_forge` | 150,000 | 150,000 | 232,865 | ×1.552 | 1,500 | ×1.552 |
| starcraft_protoss | `protoss_stargate` | 300,000 | 300,000 | 465,730 | ×1.552 | 1,500 | ×1.552 |
| starcraft_protoss | `protoss_fleetbeacon` | 1,000,000 | 1,000,000 | 1,552,432 | ×1.552 | 10,000 | ×1.552 |
| starcraft_protoss | `protoss_nexus` | 1,000,000 | 1,000,000 | 1,552,432 | ×1.552 | 5,000 | ×1.552 |
| starcraft_protoss | `protoss_mobilenexus` | 300,000 | 300,000 | 465,730 | ×1.552 | 5,000 | ×1.502 |
| redalert2mod_consortium | `cruiser_f.steel` | 37,500 | 75,000 | 78,932 | ×2.105 | 125 | ×1.468 |
| starcraft_protoss | `protoss_starshipsovereign` | 750,000 | 750,000 | 1,164,324 | ×1.552 | 10,000 | ×1.456 |
| redalert2mod_consortium | `steelconsortium_supportshieldgenerator` | 100,000 | 200,000 | 210,486 | ×2.105 | 3,000 | ×1.442 |
| redalert2mod_consortium | `steelconsortium_empressstation` | 1,500,000 | 1,500,000 | 2,328,648 | ×1.552 | 5,000 | ×1.437 |
| starcraft_protoss | `protoss_archon` | 350,000 | 350,000 | 543,351 | ×1.552 | 5,600 | ×1.416 |
| starcraft_protoss | `protoss_epigraph` | 200,000 | 200,000 | 310,486 | ×1.552 | 2,600 | ×1.403 |
| starcraft_protoss | `protoss_shuttle` | 100,000 | 100,000 | 155,243 | ×1.552 | 6,000 | ×1.402 |
| starcraft_protoss | `protoss_carrier` | 300,000 | 300,000 | 465,730 | ×1.552 | 3,000 | ×1.386 |
| starcraft_protoss | `protoss_idol` | 350,000 | 350,000 | 543,351 | ×1.552 | 2,800 | ×1.383 |
| starcraft_protoss | `protoss_arbiter` | 225,000 | 225,000 | 349,297 | ×1.552 | 4,800 | ×1.379 |
| starcraft_protoss | `protoss_atreus` | 250,000 | 250,000 | 388,108 | ×1.552 | 2,400 | ×1.373 |
| starcraft_protoss | `protoss_zeratul` | 250,000 | 250,000 | 388,108 | ×1.552 | 4,000 | ×1.372 |
| starcraft_protoss | `protoss_corsair` | 125,000 | 125,000 | 194,054 | ×1.552 | 2,100 | ×1.367 |
| redalert2mod_consortium | `steelconsortium_cloudbreaker` | 250,000 | 250,000 | 388,108 | ×1.552 | 5,000 | ×1.363 |
| starcraft_protoss | `protoss_reaver` | 275,000 | 275,000 | 426,919 | ×1.552 | 2,700 | ×1.352 |
| redalert2mod_consortium | `cougar.steel` | 25,000 | 50,000 | 52,622 | ×2.105 | 1,200 | ×1.322 |
| starcraft_protoss | `protoss_voidray` | 70,000 | 70,000 | 108,670 | ×1.552 | 1,400 | ×1.322 |
| starcraft_protoss | `protoss_scout` | 75,000 | 75,000 | 116,432 | ×1.552 | 1,500 | ×1.321 |
| redalert2mod_consortium | `hummer.steel` | 45,000 | 67,500 | 82,289 | ×1.829 | 2,000 | ×1.319 |
| redalert2mod_consortium | `oldqtnk.steel` | 112,500 | 112,500 | 174,649 | ×1.552 | 2,400 | ×1.315 |
| starcraft_protoss | `protoss_shieldbattery` | 100,000 | 100,000 | 155,243 | ×1.552 | 1,000 | ×1.276 |
| starcraft_protoss | `protoss_positron` | 60,000 | 60,000 | 93,146 | ×1.552 | 1,200 | ×1.268 |
| starcraft_protoss | `protoss_dragoon` | 75,000 | 75,000 | 116,432 | ×1.552 | 1,200 | ×1.248 |
| starcraft_protoss | `protoss_gladius` | 100,000 | 100,000 | 155,243 | ×1.552 | 1,800 | ×1.246 |
| starcraft_protoss | `protoss_patriarch` | 75,000 | 75,000 | 116,432 | ×1.552 | 4,000 | ×1.243 |
| redalert2mod_consortium | `cobra.steel` | 325,000 | 162,500 | 414,770 | ×1.276 | 3,600 | ×1.204 |
| starcraft_protoss | `protoss_probe` | 45,000 | 45,000 | 69,859 | ×1.552 | 500 | ×1.194 |
| starcraft_protoss | `protoss_analogue` | 60,000 | 60,000 | 93,146 | ×1.552 | 1,200 | ×1.193 |
| starcraft_protoss | `protoss_amaranth` | 70,000 | 70,000 | 108,670 | ×1.552 | 1,200 | ×1.185 |
| starcraft_protoss | `protoss_observer` | 12,500 | 12,500 | 19,405 | ×1.552 | 500 | ×1.174 |
| starcraft_protoss | `protoss_darktemplar` | 50,000 | 50,000 | 77,622 | ×1.552 | 600 | ×1.165 |
| starcraft_protoss | `protoss_zealot` | 40,000 | 40,000 | 62,097 | ×1.552 | 300 | ×1.150 |
| starcraft_protoss | `protoss_legionnaire` | 60,000 | 60,000 | 93,146 | ×1.552 | 700 | ×1.146 |
| starcraft_protoss | `protoss_adept` | 30,000 | 30,000 | 46,573 | ×1.552 | 650 | ×1.133 |
| starcraft_protoss | `protoss_manifold` | 25,000 | 25,000 | 38,811 | ×1.552 | 600 | ×1.121 |
| starcraft_protoss | `protoss_photoncannon` | 200,000 | 200,000 | 310,486 | ×1.552 | 2,000 | ×1.014 |
