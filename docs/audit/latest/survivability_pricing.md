# audit_survivability_pricing — E1: the baseline shield is priced at ZERO

| bucket | actors | priced today | belongs to |
|---|--:|---|---|
| spawns with a pool (**always-on**) | 56 | ✖ nothing | **E1 — this report** |
| empty capacity, needs `shieldgen` | 1152 | ✔ correctly nothing | — (it has no shield) |
| pool behind an upgrade | 210 | ✖ nothing | E5 (upgrade pricing) |

Shield row mean Versus **185.25**, so one shield point is **0.5398 HP** BEFORE any shield-gated `DamageMultiplier` — measured off the live ladder every run, never frozen. The Shield row takes **1.432%** of all roster raw damage at baseline.

⚠ **Every one of these 56 actors also carries `DamageMultiplier@shielded: 150`**, so it takes 150% damage WHILE the shield holds — the deliberate counterweight to having one. That divides the pool's worth: a shield point is really **0.3599 HP**, and the roster-wide gap is 38.6% rather than the 57.8% a shield-only reading gives. `shield_damage_multiplier` and `shield_hp_per_point` are published per actor.

## The gap

* Raw HP across these 56 actors: **12,812,500**
* Effective HP once the pool is counted: **17,753,570** (**+38.6%**)
* Implied price change if the formula read effective HP: median **×1.252**, max **×1.501**

⚠ **Retiring the 150% multiplier is a BUFF that must be paid for.** The numbers above already account for it, so they price the game AS IT IS. Delete `DamageMultiplier@shielded` and a shield point jumps from 0.360 to 0.540 HP — the same pool becomes 1.5x more valuable and the implied price rises again. Re-extract AFTER the deletion and price once, or these units get charged for durability they no longer have (or keep durability they were never charged for).

## Per actor

| pack | actor | HP | shield pool | effective HP | ×HP | cost | implied ×price |
|---|---|--:|--:|--:|--:|--:|--:|
| redalert2mod_consortium | `steelconsortium_defenderbot` | 210,000 | 420,000 | 361,147 | ×1.720 | 3,200 | ×1.501 |
| redalert2mod_consortium | `steelconsortium_skyhammer` | 120,000 | 240,000 | 206,370 | ×1.720 | 4,500 | ×1.496 |
| redalert2mod_consortium | `steelconsortium_katytank` | 275,000 | 550,000 | 472,931 | ×1.720 | 3,800 | ×1.472 |
| redalert2mod_consortium | `steelconsortium_stalker` | 140,000 | 280,000 | 240,765 | ×1.720 | 4,000 | ×1.461 |
| redalert2mod_consortium | `steelconsortium_whiterabbit` | 150,000 | 300,000 | 257,962 | ×1.720 | 4,500 | ×1.381 |
| redalert2mod_consortium | `cruiser_f.steel` | 37,500 | 75,000 | 64,491 | ×1.720 | 125 | ×1.375 |
| starcraft_protoss | `protoss_arbitertribunal` | 250,000 | 250,000 | 339,968 | ×1.360 | 2,500 | ×1.360 |
| starcraft_protoss | `protoss_assimilator` | 300,000 | 300,000 | 407,962 | ×1.360 | 3,000 | ×1.360 |
| starcraft_protoss | `protoss_citadelofadun` | 200,000 | 200,000 | 271,975 | ×1.360 | 2,000 | ×1.360 |
| starcraft_protoss | `protoss_cyberneticscore` | 150,000 | 150,000 | 203,981 | ×1.360 | 1,500 | ×1.360 |
| starcraft_protoss | `protoss_fleetbeacon` | 1,000,000 | 1,000,000 | 1,359,874 | ×1.360 | 10,000 | ×1.360 |
| starcraft_protoss | `protoss_forge` | 150,000 | 150,000 | 203,981 | ×1.360 | 1,500 | ×1.360 |
| starcraft_protoss | `protoss_gateway` | 200,000 | 200,000 | 271,975 | ×1.360 | 1,000 | ×1.360 |
| starcraft_protoss | `protoss_nexus` | 1,000,000 | 1,000,000 | 1,359,874 | ×1.360 | 5,000 | ×1.360 |
| starcraft_protoss | `protoss_observatory` | 250,000 | 250,000 | 339,968 | ×1.360 | 2,500 | ×1.360 |
| starcraft_protoss | `protoss_pylon` | 250,000 | 250,000 | 339,968 | ×1.360 | 1,000 | ×1.360 |
| starcraft_protoss | `protoss_roboticsfacility` | 400,000 | 400,000 | 543,950 | ×1.360 | 2,000 | ×1.360 |
| starcraft_protoss | `protoss_roboticssupportbay` | 200,000 | 200,000 | 271,975 | ×1.360 | 2,000 | ×1.360 |
| starcraft_protoss | `protoss_stargate` | 300,000 | 300,000 | 407,962 | ×1.360 | 1,500 | ×1.360 |
| starcraft_protoss | `protoss_templararchives` | 250,000 | 250,000 | 339,968 | ×1.360 | 2,500 | ×1.360 |
| starcraft_protoss | `protoss_mobilenexus` | 300,000 | 300,000 | 407,962 | ×1.360 | 5,000 | ×1.327 |
| starcraft_protoss | `protoss_starshipsovereign` | 750,000 | 750,000 | 1,019,906 | ×1.360 | 10,000 | ×1.301 |
| redalert2mod_consortium | `steelconsortium_supportshieldgenerator` | 100,000 | 200,000 | 171,975 | ×1.720 | 3,000 | ×1.288 |
| redalert2mod_consortium | `steelconsortium_empressstation` | 1,500,000 | 1,500,000 | 2,039,811 | ×1.360 | 5,000 | ×1.285 |
| starcraft_protoss | `protoss_archon` | 350,000 | 350,000 | 475,956 | ×1.360 | 5,600 | ×1.271 |
| starcraft_protoss | `protoss_epigraph` | 200,000 | 200,000 | 271,975 | ×1.360 | 2,600 | ×1.263 |
| starcraft_protoss | `protoss_shuttle` | 100,000 | 100,000 | 135,987 | ×1.360 | 6,000 | ×1.262 |
| starcraft_protoss | `protoss_arbiter` | 225,000 | 225,000 | 305,972 | ×1.360 | 4,800 | ×1.253 |
| starcraft_protoss | `protoss_carrier` | 300,000 | 300,000 | 407,962 | ×1.360 | 3,000 | ×1.252 |
| starcraft_protoss | `protoss_idol` | 350,000 | 350,000 | 475,956 | ×1.360 | 2,800 | ×1.249 |
| redalert2mod_consortium | `cougar.steel` | 25,000 | 50,000 | 42,994 | ×1.720 | 1,200 | ×1.245 |
| starcraft_protoss | `protoss_atreus` | 250,000 | 250,000 | 339,968 | ×1.360 | 2,400 | ×1.244 |
| starcraft_protoss | `protoss_corsair` | 125,000 | 125,000 | 169,984 | ×1.360 | 2,100 | ×1.241 |
| starcraft_protoss | `protoss_zeratul` | 250,000 | 250,000 | 339,968 | ×1.360 | 4,000 | ×1.238 |
| redalert2mod_consortium | `steelconsortium_cloudbreaker` | 250,000 | 250,000 | 339,968 | ×1.360 | 5,000 | ×1.236 |
| starcraft_protoss | `protoss_reaver` | 275,000 | 275,000 | 373,965 | ×1.360 | 2,700 | ×1.226 |
| redalert2mod_consortium | `hummer.steel` | 45,000 | 67,500 | 69,292 | ×1.540 | 2,000 | ×1.226 |
| starcraft_protoss | `protoss_scout` | 75,000 | 75,000 | 101,991 | ×1.360 | 1,500 | ×1.210 |
| redalert2mod_consortium | `oldqtnk.steel` | 112,500 | 112,500 | 152,986 | ×1.360 | 2,400 | ×1.205 |
| starcraft_protoss | `protoss_voidray` | 70,000 | 70,000 | 95,191 | ×1.360 | 1,400 | ×1.204 |
| starcraft_protoss | `protoss_shieldbattery` | 100,000 | 100,000 | 135,987 | ×1.360 | 1,000 | ×1.180 |
| starcraft_protoss | `protoss_positron` | 60,000 | 60,000 | 81,592 | ×1.360 | 1,200 | ×1.176 |
| starcraft_protoss | `protoss_dragoon` | 75,000 | 75,000 | 101,991 | ×1.360 | 1,200 | ×1.167 |
| starcraft_protoss | `protoss_patriarch` | 75,000 | 75,000 | 101,991 | ×1.360 | 4,000 | ×1.162 |
| starcraft_protoss | `protoss_gladius` | 100,000 | 100,000 | 135,987 | ×1.360 | 1,800 | ×1.160 |
| redalert2mod_consortium | `cobra.steel` | 325,000 | 162,500 | 383,480 | ×1.180 | 3,600 | ×1.133 |
| starcraft_protoss | `protoss_amaranth` | 70,000 | 70,000 | 95,191 | ×1.360 | 1,200 | ×1.113 |
| starcraft_protoss | `protoss_observer` | 12,500 | 12,500 | 16,998 | ×1.360 | 500 | ×1.113 |
| starcraft_protoss | `protoss_probe` | 45,000 | 45,000 | 61,194 | ×1.360 | 500 | ×1.113 |
| starcraft_protoss | `protoss_analogue` | 60,000 | 60,000 | 81,592 | ×1.360 | 1,200 | ×1.110 |
| starcraft_protoss | `protoss_darktemplar` | 50,000 | 50,000 | 67,994 | ×1.360 | 600 | ×1.096 |
| starcraft_protoss | `protoss_legionnaire` | 60,000 | 60,000 | 81,592 | ×1.360 | 700 | ×1.085 |
| starcraft_protoss | `protoss_adept` | 30,000 | 30,000 | 40,796 | ×1.360 | 650 | ×1.083 |
| starcraft_protoss | `protoss_zealot` | 40,000 | 40,000 | 54,395 | ×1.360 | 300 | ×1.082 |
| starcraft_protoss | `protoss_manifold` | 25,000 | 25,000 | 33,997 | ×1.360 | 600 | ×1.079 |
| starcraft_protoss | `protoss_photoncannon` | 200,000 | 200,000 | 271,975 | ×1.360 | 2,000 | ×1.042 |
