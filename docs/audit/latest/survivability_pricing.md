# audit_survivability_pricing — E1: the baseline shield is priced at ZERO

| bucket | actors | priced today | belongs to |
|---|--:|---|---|
| spawns with a pool (**always-on**) | 56 | ✖ nothing | **E1 — this report** |
| empty capacity, needs `shieldgen` | 1224 | ✔ correctly nothing | — (it has no shield) |
| pool behind an upgrade | 231 | ✖ nothing | E5 (upgrade pricing) |

Shield row mean Versus **175.92**, so one shield point is **0.5684 HP** BEFORE any shield-gated `DamageMultiplier` — measured off the live ladder every run, never frozen. The Shield row takes **1.522%** of all roster raw damage at baseline.

⚠ **Every one of these 56 actors also carries `DamageMultiplier@shielded: 150`**, so it takes 150% damage WHILE the shield holds — the deliberate counterweight to having one. That divides the pool's worth: a shield point is really **0.3790 HP**, and the roster-wide gap is 38.6% rather than the 57.8% a shield-only reading gives. `shield_damage_multiplier` and `shield_hp_per_point` are published per actor.

## The gap

* Raw HP across these 56 actors: **12,812,500**
* Effective HP once the pool is counted: **20,617,210** (**+60.9%**)
* Implied price change if the formula read effective HP: median **×1.396**, max **×1.790**

⚠ **Retiring the 150% multiplier is a BUFF that must be paid for.** The numbers above already account for it, so they price the game AS IT IS. Delete `DamageMultiplier@shielded` and a shield point jumps from 0.379 to 0.568 HP — the same pool becomes 1.5x more valuable and the implied price rises again. Re-extract AFTER the deletion and price once, or these units get charged for durability they no longer have (or keep durability they were never charged for).

## Per actor

| pack | actor | HP | shield pool | effective HP | ×HP | cost | implied ×price |
|---|---|--:|--:|--:|--:|--:|--:|
| redalert2mod_consortium | `steelconsortium_defenderbot` | 210,000 | 420,000 | 448,746 | ×2.137 | 3,200 | ×1.790 |
| redalert2mod_consortium | `steelconsortium_skyhammer` | 120,000 | 240,000 | 256,426 | ×2.137 | 4,500 | ×1.783 |
| redalert2mod_consortium | `steelconsortium_katytank` | 275,000 | 550,000 | 587,643 | ×2.137 | 3,800 | ×1.746 |
| redalert2mod_consortium | `steelconsortium_stalker` | 140,000 | 280,000 | 299,164 | ×2.137 | 4,000 | ×1.728 |
| redalert2mod_consortium | `steelconsortium_whiterabbit` | 150,000 | 300,000 | 320,533 | ×2.137 | 4,500 | ×1.600 |
| starcraft_protoss | `protoss_fleetbeacon` | 1,000,000 | 1,000,000 | 1,568,442 | ×1.568 | 10,000 | ×1.568 |
| starcraft_protoss | `protoss_nexus` | 1,000,000 | 1,000,000 | 1,568,442 | ×1.568 | 5,000 | ×1.568 |
| starcraft_protoss | `protoss_arbitertribunal` | 250,000 | 250,000 | 392,110 | ×1.568 | 2,500 | ×1.568 |
| starcraft_protoss | `protoss_citadelofadun` | 200,000 | 200,000 | 313,688 | ×1.568 | 2,000 | ×1.568 |
| starcraft_protoss | `protoss_gateway` | 200,000 | 200,000 | 313,688 | ×1.568 | 1,000 | ×1.568 |
| starcraft_protoss | `protoss_observatory` | 250,000 | 250,000 | 392,110 | ×1.568 | 2,500 | ×1.568 |
| starcraft_protoss | `protoss_pylon` | 250,000 | 250,000 | 392,110 | ×1.568 | 1,000 | ×1.568 |
| starcraft_protoss | `protoss_roboticsfacility` | 400,000 | 400,000 | 627,377 | ×1.568 | 2,000 | ×1.568 |
| starcraft_protoss | `protoss_roboticssupportbay` | 200,000 | 200,000 | 313,688 | ×1.568 | 2,000 | ×1.568 |
| starcraft_protoss | `protoss_templararchives` | 250,000 | 250,000 | 392,110 | ×1.568 | 2,500 | ×1.568 |
| starcraft_protoss | `protoss_assimilator` | 300,000 | 300,000 | 470,533 | ×1.568 | 3,000 | ×1.568 |
| starcraft_protoss | `protoss_cyberneticscore` | 150,000 | 150,000 | 235,266 | ×1.568 | 1,500 | ×1.568 |
| starcraft_protoss | `protoss_forge` | 150,000 | 150,000 | 235,266 | ×1.568 | 1,500 | ×1.568 |
| starcraft_protoss | `protoss_stargate` | 300,000 | 300,000 | 470,533 | ×1.568 | 1,500 | ×1.568 |
| starcraft_protoss | `protoss_mobilenexus` | 300,000 | 300,000 | 470,533 | ×1.568 | 5,000 | ×1.517 |
| redalert2mod_consortium | `cruiser_f.steel` | 37,500 | 75,000 | 80,133 | ×2.137 | 125 | ×1.482 |
| starcraft_protoss | `protoss_starshipsovereign` | 750,000 | 750,000 | 1,176,332 | ×1.568 | 10,000 | ×1.469 |
| redalert2mod_consortium | `steelconsortium_supportshieldgenerator` | 100,000 | 200,000 | 213,688 | ×2.137 | 3,000 | ×1.455 |
| redalert2mod_consortium | `steelconsortium_empressstation` | 1,500,000 | 1,500,000 | 2,352,663 | ×1.568 | 5,000 | ×1.450 |
| starcraft_protoss | `protoss_archon` | 350,000 | 350,000 | 548,955 | ×1.568 | 5,600 | ×1.428 |
| starcraft_protoss | `protoss_epigraph` | 200,000 | 200,000 | 313,688 | ×1.568 | 2,600 | ×1.417 |
| starcraft_protoss | `protoss_shuttle` | 100,000 | 100,000 | 156,844 | ×1.568 | 6,000 | ×1.413 |
| starcraft_protoss | `protoss_carrier` | 300,000 | 300,000 | 470,533 | ×1.568 | 3,000 | ×1.398 |
| starcraft_protoss | `protoss_idol` | 350,000 | 350,000 | 548,955 | ×1.568 | 2,800 | ×1.394 |
| starcraft_protoss | `protoss_atreus` | 250,000 | 250,000 | 392,110 | ×1.568 | 2,400 | ×1.392 |
| starcraft_protoss | `protoss_arbiter` | 225,000 | 225,000 | 352,900 | ×1.568 | 4,800 | ×1.390 |
| starcraft_protoss | `protoss_zeratul` | 250,000 | 250,000 | 392,110 | ×1.568 | 4,000 | ×1.383 |
| starcraft_protoss | `protoss_corsair` | 125,000 | 125,000 | 196,055 | ×1.568 | 2,100 | ×1.378 |
| redalert2mod_consortium | `steelconsortium_cloudbreaker` | 250,000 | 250,000 | 392,110 | ×1.568 | 5,000 | ×1.374 |
| starcraft_protoss | `protoss_reaver` | 275,000 | 275,000 | 431,322 | ×1.568 | 2,700 | ×1.362 |
| redalert2mod_consortium | `cougar.steel` | 25,000 | 50,000 | 53,422 | ×2.137 | 1,200 | ×1.331 |
| starcraft_protoss | `protoss_voidray` | 70,000 | 70,000 | 109,791 | ×1.568 | 1,400 | ×1.331 |
| starcraft_protoss | `protoss_scout` | 75,000 | 75,000 | 117,633 | ×1.568 | 1,500 | ×1.330 |
| redalert2mod_consortium | `hummer.steel` | 45,000 | 67,500 | 83,370 | ×1.853 | 2,000 | ×1.328 |
| redalert2mod_consortium | `oldqtnk.steel` | 112,500 | 112,500 | 176,450 | ×1.568 | 2,400 | ×1.324 |
| starcraft_protoss | `protoss_shieldbattery` | 100,000 | 100,000 | 156,844 | ×1.568 | 1,000 | ×1.284 |
| starcraft_protoss | `protoss_positron` | 60,000 | 60,000 | 94,106 | ×1.568 | 1,200 | ×1.275 |
| starcraft_protoss | `protoss_dragoon` | 75,000 | 75,000 | 117,633 | ×1.568 | 1,200 | ×1.255 |
| starcraft_protoss | `protoss_gladius` | 100,000 | 100,000 | 156,844 | ×1.568 | 1,800 | ×1.253 |
| starcraft_protoss | `protoss_patriarch` | 75,000 | 75,000 | 117,633 | ×1.568 | 4,000 | ×1.250 |
| redalert2mod_consortium | `cobra.steel` | 325,000 | 162,500 | 417,372 | ×1.284 | 3,600 | ×1.210 |
| starcraft_protoss | `protoss_probe` | 45,000 | 45,000 | 70,580 | ×1.568 | 500 | ×1.200 |
| starcraft_protoss | `protoss_analogue` | 60,000 | 60,000 | 94,106 | ×1.568 | 1,200 | ×1.199 |
| starcraft_protoss | `protoss_amaranth` | 70,000 | 70,000 | 109,791 | ×1.568 | 1,200 | ×1.190 |
| starcraft_protoss | `protoss_observer` | 12,500 | 12,500 | 19,606 | ×1.568 | 500 | ×1.179 |
| starcraft_protoss | `protoss_darktemplar` | 50,000 | 50,000 | 78,422 | ×1.568 | 600 | ×1.170 |
| starcraft_protoss | `protoss_zealot` | 40,000 | 40,000 | 62,738 | ×1.568 | 300 | ×1.155 |
| starcraft_protoss | `protoss_legionnaire` | 60,000 | 60,000 | 94,106 | ×1.568 | 700 | ×1.150 |
| starcraft_protoss | `protoss_adept` | 30,000 | 30,000 | 47,053 | ×1.568 | 650 | ×1.137 |
| starcraft_protoss | `protoss_manifold` | 25,000 | 25,000 | 39,211 | ×1.568 | 600 | ×1.125 |
| starcraft_protoss | `protoss_photoncannon` | 200,000 | 200,000 | 313,688 | ×1.568 | 2,000 | ×1.015 |
