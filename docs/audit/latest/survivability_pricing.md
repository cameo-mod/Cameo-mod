# audit_survivability_pricing — E1: the baseline shield is priced at ZERO

| bucket | actors | priced today | belongs to |
|---|--:|---|---|
| spawns with a pool (**always-on**) | 56 | ✖ nothing | **E1 — this report** |
| empty capacity, needs `shieldgen` | 1266 | ✔ correctly nothing | — (it has no shield) |
| pool behind an upgrade | 233 | ✖ nothing | E5 (upgrade pricing) |

Shield row mean Versus **162.19**, so one shield point is **0.6166 HP** BEFORE any shield-gated `DamageMultiplier` — measured off the live ladder every run, never frozen. The Shield row takes **1.478%** of all roster raw damage at baseline.

⚠ **Every one of these 56 actors also carries `DamageMultiplier@shielded: 150`**, so it takes 150% damage WHILE the shield holds — the deliberate counterweight to having one. That divides the pool's worth: a shield point is really **0.4110 HP**, and the roster-wide gap is 38.6% rather than the 57.8% a shield-only reading gives. `shield_damage_multiplier` and `shield_hp_per_point` are published per actor.

## The gap

* Raw HP across these 56 actors: **12,812,500**
* Effective HP once the pool is counted: **21,277,733** (**+66.1%**)
* Implied price change if the formula read effective HP: median **×1.429**, max **×1.857**

⚠ **Retiring the 150% multiplier is a BUFF that must be paid for.** The numbers above already account for it, so they price the game AS IT IS. Delete `DamageMultiplier@shielded` and a shield point jumps from 0.411 to 0.617 HP — the same pool becomes 1.5x more valuable and the implied price rises again. Re-extract AFTER the deletion and price once, or these units get charged for durability they no longer have (or keep durability they were never charged for).

## Per actor

| pack | actor | HP | shield pool | effective HP | ×HP | cost | implied ×price |
|---|---|--:|--:|--:|--:|--:|--:|
| redalert2mod_consortium | `steelconsortium_defenderbot` | 210,000 | 420,000 | 468,951 | ×2.233 | 3,200 | ×1.857 |
| redalert2mod_consortium | `steelconsortium_skyhammer` | 120,000 | 240,000 | 267,972 | ×2.233 | 4,500 | ×1.848 |
| redalert2mod_consortium | `steelconsortium_katytank` | 275,000 | 550,000 | 614,103 | ×2.233 | 3,800 | ×1.810 |
| redalert2mod_consortium | `steelconsortium_stalker` | 140,000 | 280,000 | 312,634 | ×2.233 | 4,000 | ×1.790 |
| redalert2mod_consortium | `steelconsortium_whiterabbit` | 150,000 | 300,000 | 334,965 | ×2.233 | 4,500 | ×1.650 |
| starcraft_protoss | `protoss_roboticsfacility` | 400,000 | 400,000 | 646,620 | ×1.617 | 2,000 | ×1.617 |
| starcraft_protoss | `protoss_fleetbeacon` | 1,000,000 | 1,000,000 | 1,616,550 | ×1.617 | 10,000 | ×1.617 |
| starcraft_protoss | `protoss_nexus` | 1,000,000 | 1,000,000 | 1,616,550 | ×1.617 | 5,000 | ×1.617 |
| starcraft_protoss | `protoss_arbitertribunal` | 250,000 | 250,000 | 404,138 | ×1.617 | 2,500 | ×1.617 |
| starcraft_protoss | `protoss_observatory` | 250,000 | 250,000 | 404,138 | ×1.617 | 2,500 | ×1.617 |
| starcraft_protoss | `protoss_pylon` | 250,000 | 250,000 | 404,138 | ×1.617 | 1,000 | ×1.617 |
| starcraft_protoss | `protoss_templararchives` | 250,000 | 250,000 | 404,138 | ×1.617 | 2,500 | ×1.617 |
| starcraft_protoss | `protoss_assimilator` | 300,000 | 300,000 | 484,965 | ×1.617 | 3,000 | ×1.617 |
| starcraft_protoss | `protoss_citadelofadun` | 200,000 | 200,000 | 323,310 | ×1.617 | 2,000 | ×1.617 |
| starcraft_protoss | `protoss_cyberneticscore` | 150,000 | 150,000 | 242,482 | ×1.617 | 1,500 | ×1.617 |
| starcraft_protoss | `protoss_forge` | 150,000 | 150,000 | 242,482 | ×1.617 | 1,500 | ×1.617 |
| starcraft_protoss | `protoss_gateway` | 200,000 | 200,000 | 323,310 | ×1.617 | 1,000 | ×1.617 |
| starcraft_protoss | `protoss_roboticssupportbay` | 200,000 | 200,000 | 323,310 | ×1.617 | 2,000 | ×1.617 |
| starcraft_protoss | `protoss_stargate` | 300,000 | 300,000 | 484,965 | ×1.617 | 1,500 | ×1.617 |
| starcraft_protoss | `protoss_mobilenexus` | 300,000 | 300,000 | 484,965 | ×1.617 | 5,000 | ×1.560 |
| redalert2mod_consortium | `cruiser_f.steel` | 37,500 | 75,000 | 83,741 | ×2.233 | 125 | ×1.523 |
| starcraft_protoss | `protoss_starshipsovereign` | 750,000 | 750,000 | 1,212,413 | ×1.617 | 10,000 | ×1.509 |
| redalert2mod_consortium | `steelconsortium_supportshieldgenerator` | 100,000 | 200,000 | 223,310 | ×2.233 | 3,000 | ×1.493 |
| redalert2mod_consortium | `steelconsortium_empressstation` | 1,500,000 | 1,500,000 | 2,424,825 | ×1.617 | 5,000 | ×1.488 |
| starcraft_protoss | `protoss_archon` | 350,000 | 350,000 | 565,792 | ×1.617 | 5,600 | ×1.464 |
| starcraft_protoss | `protoss_epigraph` | 200,000 | 200,000 | 323,310 | ×1.617 | 2,600 | ×1.450 |
| starcraft_protoss | `protoss_shuttle` | 100,000 | 100,000 | 161,655 | ×1.617 | 6,000 | ×1.448 |
| starcraft_protoss | `protoss_carrier` | 300,000 | 300,000 | 484,965 | ×1.617 | 3,000 | ×1.431 |
| starcraft_protoss | `protoss_idol` | 350,000 | 350,000 | 565,792 | ×1.617 | 2,800 | ×1.428 |
| starcraft_protoss | `protoss_arbiter` | 225,000 | 225,000 | 363,724 | ×1.617 | 4,800 | ×1.423 |
| starcraft_protoss | `protoss_atreus` | 250,000 | 250,000 | 404,138 | ×1.617 | 2,400 | ×1.416 |
| starcraft_protoss | `protoss_zeratul` | 250,000 | 250,000 | 404,138 | ×1.617 | 4,000 | ×1.415 |
| starcraft_protoss | `protoss_corsair` | 125,000 | 125,000 | 202,069 | ×1.617 | 2,100 | ×1.410 |
| redalert2mod_consortium | `steelconsortium_cloudbreaker` | 250,000 | 250,000 | 404,138 | ×1.617 | 5,000 | ×1.405 |
| starcraft_protoss | `protoss_reaver` | 275,000 | 275,000 | 444,551 | ×1.617 | 2,700 | ×1.392 |
| redalert2mod_consortium | `cougar.steel` | 25,000 | 50,000 | 55,828 | ×2.233 | 1,200 | ×1.360 |
| starcraft_protoss | `protoss_voidray` | 70,000 | 70,000 | 113,158 | ×1.617 | 1,400 | ×1.359 |
| starcraft_protoss | `protoss_scout` | 75,000 | 75,000 | 121,241 | ×1.617 | 1,500 | ×1.358 |
| redalert2mod_consortium | `hummer.steel` | 45,000 | 67,500 | 86,617 | ×1.925 | 2,000 | ×1.356 |
| redalert2mod_consortium | `oldqtnk.steel` | 112,500 | 112,500 | 181,862 | ×1.617 | 2,400 | ×1.352 |
| starcraft_protoss | `protoss_shieldbattery` | 100,000 | 100,000 | 161,655 | ×1.617 | 1,000 | ×1.308 |
| starcraft_protoss | `protoss_positron` | 60,000 | 60,000 | 96,993 | ×1.617 | 1,200 | ×1.299 |
| starcraft_protoss | `protoss_dragoon` | 75,000 | 75,000 | 121,241 | ×1.617 | 1,200 | ×1.277 |
| starcraft_protoss | `protoss_gladius` | 100,000 | 100,000 | 161,655 | ×1.617 | 1,800 | ×1.274 |
| starcraft_protoss | `protoss_patriarch` | 75,000 | 75,000 | 121,241 | ×1.617 | 4,000 | ×1.271 |
| redalert2mod_consortium | `cobra.steel` | 325,000 | 162,500 | 425,189 | ×1.308 | 3,600 | ×1.228 |
| starcraft_protoss | `protoss_probe` | 45,000 | 45,000 | 72,745 | ×1.617 | 500 | ×1.216 |
| starcraft_protoss | `protoss_analogue` | 60,000 | 60,000 | 96,993 | ×1.617 | 1,200 | ×1.215 |
| starcraft_protoss | `protoss_amaranth` | 70,000 | 70,000 | 113,158 | ×1.617 | 1,200 | ×1.206 |
| starcraft_protoss | `protoss_observer` | 12,500 | 12,500 | 20,207 | ×1.617 | 500 | ×1.194 |
| starcraft_protoss | `protoss_darktemplar` | 50,000 | 50,000 | 80,828 | ×1.617 | 600 | ×1.184 |
| starcraft_protoss | `protoss_zealot` | 40,000 | 40,000 | 64,662 | ×1.617 | 300 | ×1.167 |
| starcraft_protoss | `protoss_legionnaire` | 60,000 | 60,000 | 96,993 | ×1.617 | 700 | ×1.163 |
| starcraft_protoss | `protoss_adept` | 30,000 | 30,000 | 48,496 | ×1.617 | 650 | ×1.148 |
| starcraft_protoss | `protoss_manifold` | 25,000 | 25,000 | 40,414 | ×1.617 | 600 | ×1.135 |
| starcraft_protoss | `protoss_photoncannon` | 200,000 | 200,000 | 323,310 | ×1.617 | 2,000 | ×1.016 |
