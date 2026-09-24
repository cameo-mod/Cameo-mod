# audit_survivability_pricing — E1: the baseline shield is priced at ZERO

| bucket | actors | priced today | belongs to |
|---|--:|---|---|
| spawns with a pool (**always-on**) | 56 | ✖ nothing | **E1 — this report** |
| empty capacity, needs `shieldgen` | 1266 | ✔ correctly nothing | — (it has no shield) |
| pool behind an upgrade | 233 | ✖ nothing | E5 (upgrade pricing) |

Shield row mean Versus **88.08**, so one shield point is **1.1354 HP** BEFORE any shield-gated `DamageMultiplier` — measured off the live ladder every run, never frozen. The Shield row takes **1.715%** of all roster raw damage at baseline.

⚠ **Every one of these 56 actors also carries `DamageMultiplier@shielded: 150`**, so it takes 150% damage WHILE the shield holds — the deliberate counterweight to having one. That divides the pool's worth: a shield point is really **0.7569 HP**, and the roster-wide gap is 38.6% rather than the 57.8% a shield-only reading gives. `shield_damage_multiplier` and `shield_hp_per_point` are published per actor.

## The gap

* Raw HP across these 56 actors: **12,812,500**
* Effective HP once the pool is counted: **21,296,708** (**+66.2%**)
* Implied price change if the formula read effective HP: median **×1.430**, max **×1.859**

⚠ **Retiring the 150% multiplier is a BUFF that must be paid for.** The numbers above already account for it, so they price the game AS IT IS. Delete `DamageMultiplier@shielded` and a shield point jumps from 0.757 to 1.135 HP — the same pool becomes 1.5x more valuable and the implied price rises again. Re-extract AFTER the deletion and price once, or these units get charged for durability they no longer have (or keep durability they were never charged for).

## Per actor

| pack | actor | HP | shield pool | effective HP | ×HP | cost | implied ×price |
|---|---|--:|--:|--:|--:|--:|--:|
| redalert2mod_consortium | `steelconsortium_defenderbot` | 210,000 | 420,000 | 469,532 | ×2.236 | 3,200 | ×1.859 |
| redalert2mod_consortium | `steelconsortium_skyhammer` | 120,000 | 240,000 | 268,304 | ×2.236 | 4,500 | ×1.850 |
| redalert2mod_consortium | `steelconsortium_katytank` | 275,000 | 550,000 | 614,863 | ×2.236 | 3,800 | ×1.811 |
| redalert2mod_consortium | `steelconsortium_stalker` | 140,000 | 280,000 | 313,021 | ×2.236 | 4,000 | ×1.791 |
| redalert2mod_consortium | `steelconsortium_whiterabbit` | 150,000 | 300,000 | 335,380 | ×2.236 | 4,500 | ×1.651 |
| starcraft_protoss | `protoss_roboticsfacility` | 400,000 | 400,000 | 647,173 | ×1.618 | 2,000 | ×1.618 |
| starcraft_protoss | `protoss_fleetbeacon` | 1,000,000 | 1,000,000 | 1,617,932 | ×1.618 | 10,000 | ×1.618 |
| starcraft_protoss | `protoss_nexus` | 1,000,000 | 1,000,000 | 1,617,932 | ×1.618 | 5,000 | ×1.618 |
| starcraft_protoss | `protoss_citadelofadun` | 200,000 | 200,000 | 323,586 | ×1.618 | 2,000 | ×1.618 |
| starcraft_protoss | `protoss_gateway` | 200,000 | 200,000 | 323,586 | ×1.618 | 1,000 | ×1.618 |
| starcraft_protoss | `protoss_roboticssupportbay` | 200,000 | 200,000 | 323,586 | ×1.618 | 2,000 | ×1.618 |
| starcraft_protoss | `protoss_arbitertribunal` | 250,000 | 250,000 | 404,483 | ×1.618 | 2,500 | ×1.618 |
| starcraft_protoss | `protoss_assimilator` | 300,000 | 300,000 | 485,380 | ×1.618 | 3,000 | ×1.618 |
| starcraft_protoss | `protoss_cyberneticscore` | 150,000 | 150,000 | 242,690 | ×1.618 | 1,500 | ×1.618 |
| starcraft_protoss | `protoss_forge` | 150,000 | 150,000 | 242,690 | ×1.618 | 1,500 | ×1.618 |
| starcraft_protoss | `protoss_observatory` | 250,000 | 250,000 | 404,483 | ×1.618 | 2,500 | ×1.618 |
| starcraft_protoss | `protoss_pylon` | 250,000 | 250,000 | 404,483 | ×1.618 | 1,000 | ×1.618 |
| starcraft_protoss | `protoss_stargate` | 300,000 | 300,000 | 485,380 | ×1.618 | 1,500 | ×1.618 |
| starcraft_protoss | `protoss_templararchives` | 250,000 | 250,000 | 404,483 | ×1.618 | 2,500 | ×1.618 |
| starcraft_protoss | `protoss_mobilenexus` | 300,000 | 300,000 | 485,380 | ×1.618 | 5,000 | ×1.562 |
| redalert2mod_consortium | `cruiser_f.steel` | 37,500 | 75,000 | 83,845 | ×2.236 | 125 | ×1.524 |
| starcraft_protoss | `protoss_starshipsovereign` | 750,000 | 750,000 | 1,213,449 | ×1.618 | 10,000 | ×1.510 |
| redalert2mod_consortium | `steelconsortium_supportshieldgenerator` | 100,000 | 200,000 | 223,586 | ×2.236 | 3,000 | ×1.494 |
| redalert2mod_consortium | `steelconsortium_empressstation` | 1,500,000 | 1,500,000 | 2,426,898 | ×1.618 | 5,000 | ×1.489 |
| starcraft_protoss | `protoss_archon` | 350,000 | 350,000 | 566,276 | ×1.618 | 5,600 | ×1.465 |
| starcraft_protoss | `protoss_epigraph` | 200,000 | 200,000 | 323,586 | ×1.618 | 2,600 | ×1.451 |
| starcraft_protoss | `protoss_shuttle` | 100,000 | 100,000 | 161,793 | ×1.618 | 6,000 | ×1.449 |
| starcraft_protoss | `protoss_carrier` | 300,000 | 300,000 | 485,380 | ×1.618 | 3,000 | ×1.432 |
| starcraft_protoss | `protoss_idol` | 350,000 | 350,000 | 566,276 | ×1.618 | 2,800 | ×1.429 |
| starcraft_protoss | `protoss_arbiter` | 225,000 | 225,000 | 364,035 | ×1.618 | 4,800 | ×1.424 |
| starcraft_protoss | `protoss_atreus` | 250,000 | 250,000 | 404,483 | ×1.618 | 2,400 | ×1.417 |
| starcraft_protoss | `protoss_zeratul` | 250,000 | 250,000 | 404,483 | ×1.618 | 4,000 | ×1.416 |
| starcraft_protoss | `protoss_corsair` | 125,000 | 125,000 | 202,242 | ×1.618 | 2,100 | ×1.410 |
| redalert2mod_consortium | `steelconsortium_cloudbreaker` | 250,000 | 250,000 | 404,483 | ×1.618 | 5,000 | ×1.406 |
| starcraft_protoss | `protoss_reaver` | 275,000 | 275,000 | 444,931 | ×1.618 | 2,700 | ×1.393 |
| redalert2mod_consortium | `cougar.steel` | 25,000 | 50,000 | 55,897 | ×2.236 | 1,200 | ×1.360 |
| starcraft_protoss | `protoss_voidray` | 70,000 | 70,000 | 113,255 | ×1.618 | 1,400 | ×1.360 |
| starcraft_protoss | `protoss_scout` | 75,000 | 75,000 | 121,345 | ×1.618 | 1,500 | ×1.359 |
| redalert2mod_consortium | `hummer.steel` | 45,000 | 67,500 | 86,710 | ×1.927 | 2,000 | ×1.357 |
| redalert2mod_consortium | `oldqtnk.steel` | 112,500 | 112,500 | 182,017 | ×1.618 | 2,400 | ×1.352 |
| starcraft_protoss | `protoss_shieldbattery` | 100,000 | 100,000 | 161,793 | ×1.618 | 1,000 | ×1.309 |
| starcraft_protoss | `protoss_positron` | 60,000 | 60,000 | 97,076 | ×1.618 | 1,200 | ×1.299 |
| starcraft_protoss | `protoss_dragoon` | 75,000 | 75,000 | 121,345 | ×1.618 | 1,200 | ×1.278 |
| starcraft_protoss | `protoss_gladius` | 100,000 | 100,000 | 161,793 | ×1.618 | 1,800 | ×1.275 |
| starcraft_protoss | `protoss_patriarch` | 75,000 | 75,000 | 121,345 | ×1.618 | 4,000 | ×1.272 |
| redalert2mod_consortium | `cobra.steel` | 325,000 | 162,500 | 425,414 | ×1.309 | 3,600 | ×1.228 |
| starcraft_protoss | `protoss_probe` | 45,000 | 45,000 | 72,807 | ×1.618 | 500 | ×1.217 |
| starcraft_protoss | `protoss_analogue` | 60,000 | 60,000 | 97,076 | ×1.618 | 1,200 | ×1.216 |
| starcraft_protoss | `protoss_amaranth` | 70,000 | 70,000 | 113,255 | ×1.618 | 1,200 | ×1.206 |
| starcraft_protoss | `protoss_observer` | 12,500 | 12,500 | 20,224 | ×1.618 | 500 | ×1.194 |
| starcraft_protoss | `protoss_darktemplar` | 50,000 | 50,000 | 80,897 | ×1.618 | 600 | ×1.185 |
| starcraft_protoss | `protoss_zealot` | 40,000 | 40,000 | 64,717 | ×1.618 | 300 | ×1.168 |
| starcraft_protoss | `protoss_legionnaire` | 60,000 | 60,000 | 97,076 | ×1.618 | 700 | ×1.163 |
| starcraft_protoss | `protoss_adept` | 30,000 | 30,000 | 48,538 | ×1.618 | 650 | ×1.148 |
| starcraft_protoss | `protoss_manifold` | 25,000 | 25,000 | 40,448 | ×1.618 | 600 | ×1.136 |
| starcraft_protoss | `protoss_photoncannon` | 200,000 | 200,000 | 323,586 | ×1.618 | 2,000 | ×1.016 |
