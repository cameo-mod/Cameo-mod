# audit_survivability_pricing — E1: the baseline shield is priced at ZERO

| bucket | actors | priced today | belongs to |
|---|--:|---|---|
| spawns with a pool (**always-on**) | 56 | ✖ nothing | **E1 — this report** |
| empty capacity, needs `shieldgen` | 1266 | ✔ correctly nothing | — (it has no shield) |
| pool behind an upgrade | 233 | ✖ nothing | E5 (upgrade pricing) |

Shield row mean Versus **180.28**, so one shield point is **0.5547 HP** BEFORE any shield-gated `DamageMultiplier` — measured off the live ladder every run, never frozen. The Shield row takes **1.444%** of all roster raw damage at baseline.

⚠ **Every one of these 56 actors also carries `DamageMultiplier@shielded: 150`**, so it takes 150% damage WHILE the shield holds — the deliberate counterweight to having one. That divides the pool's worth: a shield point is really **0.3698 HP**, and the roster-wide gap is 38.6% rather than the 57.8% a shield-only reading gives. `shield_damage_multiplier` and `shield_hp_per_point` are published per actor.

## The gap

* Raw HP across these 56 actors: **12,812,500**
* Effective HP once the pool is counted: **20,428,252** (**+59.4%**)
* Implied price change if the formula read effective HP: median **×1.386**, max **×1.771**

⚠ **Retiring the 150% multiplier is a BUFF that must be paid for.** The numbers above already account for it, so they price the game AS IT IS. Delete `DamageMultiplier@shielded` and a shield point jumps from 0.370 to 0.555 HP — the same pool becomes 1.5x more valuable and the implied price rises again. Re-extract AFTER the deletion and price once, or these units get charged for durability they no longer have (or keep durability they were never charged for).

## Per actor

| pack | actor | HP | shield pool | effective HP | ×HP | cost | implied ×price |
|---|---|--:|--:|--:|--:|--:|--:|
| redalert2mod_consortium | `steelconsortium_defenderbot` | 210,000 | 420,000 | 442,966 | ×2.109 | 3,200 | ×1.771 |
| redalert2mod_consortium | `steelconsortium_skyhammer` | 120,000 | 240,000 | 253,123 | ×2.109 | 4,500 | ×1.764 |
| redalert2mod_consortium | `steelconsortium_katytank` | 275,000 | 550,000 | 580,074 | ×2.109 | 3,800 | ×1.728 |
| redalert2mod_consortium | `steelconsortium_stalker` | 140,000 | 280,000 | 295,310 | ×2.109 | 4,000 | ×1.710 |
| redalert2mod_consortium | `steelconsortium_whiterabbit` | 150,000 | 300,000 | 316,404 | ×2.109 | 4,500 | ×1.585 |
| starcraft_protoss | `protoss_cyberneticscore` | 150,000 | 150,000 | 233,202 | ×1.555 | 1,500 | ×1.555 |
| starcraft_protoss | `protoss_forge` | 150,000 | 150,000 | 233,202 | ×1.555 | 1,500 | ×1.555 |
| starcraft_protoss | `protoss_roboticsfacility` | 400,000 | 400,000 | 621,872 | ×1.555 | 2,000 | ×1.555 |
| starcraft_protoss | `protoss_fleetbeacon` | 1,000,000 | 1,000,000 | 1,554,680 | ×1.555 | 10,000 | ×1.555 |
| starcraft_protoss | `protoss_nexus` | 1,000,000 | 1,000,000 | 1,554,680 | ×1.555 | 5,000 | ×1.555 |
| starcraft_protoss | `protoss_assimilator` | 300,000 | 300,000 | 466,404 | ×1.555 | 3,000 | ×1.555 |
| starcraft_protoss | `protoss_stargate` | 300,000 | 300,000 | 466,404 | ×1.555 | 1,500 | ×1.555 |
| starcraft_protoss | `protoss_arbitertribunal` | 250,000 | 250,000 | 388,670 | ×1.555 | 2,500 | ×1.555 |
| starcraft_protoss | `protoss_observatory` | 250,000 | 250,000 | 388,670 | ×1.555 | 2,500 | ×1.555 |
| starcraft_protoss | `protoss_pylon` | 250,000 | 250,000 | 388,670 | ×1.555 | 1,000 | ×1.555 |
| starcraft_protoss | `protoss_templararchives` | 250,000 | 250,000 | 388,670 | ×1.555 | 2,500 | ×1.555 |
| starcraft_protoss | `protoss_citadelofadun` | 200,000 | 200,000 | 310,936 | ×1.555 | 2,000 | ×1.555 |
| starcraft_protoss | `protoss_gateway` | 200,000 | 200,000 | 310,936 | ×1.555 | 1,000 | ×1.555 |
| starcraft_protoss | `protoss_roboticssupportbay` | 200,000 | 200,000 | 310,936 | ×1.555 | 2,000 | ×1.555 |
| starcraft_protoss | `protoss_mobilenexus` | 300,000 | 300,000 | 466,404 | ×1.555 | 5,000 | ×1.504 |
| redalert2mod_consortium | `cruiser_f.steel` | 37,500 | 75,000 | 79,101 | ×2.109 | 125 | ×1.470 |
| starcraft_protoss | `protoss_starshipsovereign` | 750,000 | 750,000 | 1,166,010 | ×1.555 | 10,000 | ×1.458 |
| redalert2mod_consortium | `steelconsortium_supportshieldgenerator` | 100,000 | 200,000 | 210,936 | ×2.109 | 3,000 | ×1.444 |
| redalert2mod_consortium | `steelconsortium_empressstation` | 1,500,000 | 1,500,000 | 2,332,020 | ×1.555 | 5,000 | ×1.439 |
| starcraft_protoss | `protoss_archon` | 350,000 | 350,000 | 544,138 | ×1.555 | 5,600 | ×1.418 |
| starcraft_protoss | `protoss_epigraph` | 200,000 | 200,000 | 310,936 | ×1.555 | 2,600 | ×1.405 |
| starcraft_protoss | `protoss_shuttle` | 100,000 | 100,000 | 155,468 | ×1.555 | 6,000 | ×1.403 |
| starcraft_protoss | `protoss_carrier` | 300,000 | 300,000 | 466,404 | ×1.555 | 3,000 | ×1.388 |
| starcraft_protoss | `protoss_idol` | 350,000 | 350,000 | 544,138 | ×1.555 | 2,800 | ×1.384 |
| starcraft_protoss | `protoss_arbiter` | 225,000 | 225,000 | 349,803 | ×1.555 | 4,800 | ×1.380 |
| starcraft_protoss | `protoss_atreus` | 250,000 | 250,000 | 388,670 | ×1.555 | 2,400 | ×1.374 |
| starcraft_protoss | `protoss_zeratul` | 250,000 | 250,000 | 388,670 | ×1.555 | 4,000 | ×1.374 |
| starcraft_protoss | `protoss_corsair` | 125,000 | 125,000 | 194,335 | ×1.555 | 2,100 | ×1.368 |
| redalert2mod_consortium | `steelconsortium_cloudbreaker` | 250,000 | 250,000 | 388,670 | ×1.555 | 5,000 | ×1.365 |
| starcraft_protoss | `protoss_reaver` | 275,000 | 275,000 | 427,537 | ×1.555 | 2,700 | ×1.353 |
| redalert2mod_consortium | `cougar.steel` | 25,000 | 50,000 | 52,734 | ×2.109 | 1,200 | ×1.323 |
| starcraft_protoss | `protoss_voidray` | 70,000 | 70,000 | 108,828 | ×1.555 | 1,400 | ×1.323 |
| starcraft_protoss | `protoss_scout` | 75,000 | 75,000 | 116,601 | ×1.555 | 1,500 | ×1.322 |
| redalert2mod_consortium | `hummer.steel` | 45,000 | 67,500 | 82,441 | ×1.832 | 2,000 | ×1.320 |
| redalert2mod_consortium | `oldqtnk.steel` | 112,500 | 112,500 | 174,902 | ×1.555 | 2,400 | ×1.316 |
| starcraft_protoss | `protoss_shieldbattery` | 100,000 | 100,000 | 155,468 | ×1.555 | 1,000 | ×1.277 |
| starcraft_protoss | `protoss_positron` | 60,000 | 60,000 | 93,281 | ×1.555 | 1,200 | ×1.269 |
| starcraft_protoss | `protoss_dragoon` | 75,000 | 75,000 | 116,601 | ×1.555 | 1,200 | ×1.249 |
| starcraft_protoss | `protoss_gladius` | 100,000 | 100,000 | 155,468 | ×1.555 | 1,800 | ×1.247 |
| starcraft_protoss | `protoss_patriarch` | 75,000 | 75,000 | 116,601 | ×1.555 | 4,000 | ×1.244 |
| redalert2mod_consortium | `cobra.steel` | 325,000 | 162,500 | 415,136 | ×1.277 | 3,600 | ×1.205 |
| starcraft_protoss | `protoss_probe` | 45,000 | 45,000 | 69,961 | ×1.555 | 500 | ×1.195 |
| starcraft_protoss | `protoss_analogue` | 60,000 | 60,000 | 93,281 | ×1.555 | 1,200 | ×1.194 |
| starcraft_protoss | `protoss_amaranth` | 70,000 | 70,000 | 108,828 | ×1.555 | 1,200 | ×1.185 |
| starcraft_protoss | `protoss_observer` | 12,500 | 12,500 | 19,434 | ×1.555 | 500 | ×1.174 |
| starcraft_protoss | `protoss_darktemplar` | 50,000 | 50,000 | 77,734 | ×1.555 | 600 | ×1.166 |
| starcraft_protoss | `protoss_zealot` | 40,000 | 40,000 | 62,187 | ×1.555 | 300 | ×1.151 |
| starcraft_protoss | `protoss_legionnaire` | 60,000 | 60,000 | 93,281 | ×1.555 | 700 | ×1.147 |
| starcraft_protoss | `protoss_adept` | 30,000 | 30,000 | 46,640 | ×1.555 | 650 | ×1.133 |
| starcraft_protoss | `protoss_manifold` | 25,000 | 25,000 | 38,867 | ×1.555 | 600 | ×1.122 |
| starcraft_protoss | `protoss_photoncannon` | 200,000 | 200,000 | 310,936 | ×1.555 | 2,000 | ×1.014 |
