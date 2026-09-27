# audit_survivability_pricing — E1: the baseline shield is priced at ZERO

| bucket | actors | priced today | belongs to |
|---|--:|---|---|
| spawns with a pool (**always-on**) | 56 | ✖ nothing | **E1 — this report** |
| empty capacity, needs `shieldgen` | 1266 | ✔ correctly nothing | — (it has no shield) |
| pool behind an upgrade | 233 | ✖ nothing | E5 (upgrade pricing) |

Shield row mean Versus **99.41**, so one shield point is **1.0060 HP** BEFORE any shield-gated `DamageMultiplier` — measured off the live ladder every run, never frozen. The Shield row takes **1.673%** of all roster raw damage at baseline.

⚠ **Every one of these 56 actors also carries `DamageMultiplier@shielded: 150`**, so it takes 150% damage WHILE the shield holds — the deliberate counterweight to having one. That divides the pool's worth: a shield point is really **0.6707 HP**, and the roster-wide gap is 38.6% rather than the 57.8% a shield-only reading gives. `shield_damage_multiplier` and `shield_hp_per_point` are published per actor.

## The gap

* Raw HP across these 56 actors: **12,812,500**
* Effective HP once the pool is counted: **28,329,223** (**+121.1%**)
* Implied price change if the formula read effective HP: median **×1.787**, max **×2.571**

⚠ **Retiring the 150% multiplier is a BUFF that must be paid for.** The numbers above already account for it, so they price the game AS IT IS. Delete `DamageMultiplier@shielded` and a shield point jumps from 0.671 to 1.006 HP — the same pool becomes 1.5x more valuable and the implied price rises again. Re-extract AFTER the deletion and price once, or these units get charged for durability they no longer have (or keep durability they were never charged for).

## Per actor

| pack | actor | HP | shield pool | effective HP | ×HP | cost | implied ×price |
|---|---|--:|--:|--:|--:|--:|--:|
| redalert2mod_consortium | `steelconsortium_defenderbot` | 210,000 | 420,000 | 684,656 | ×3.260 | 3,200 | ×2.571 |
| redalert2mod_consortium | `steelconsortium_skyhammer` | 120,000 | 240,000 | 391,232 | ×3.260 | 4,500 | ×2.555 |
| redalert2mod_consortium | `steelconsortium_katytank` | 275,000 | 550,000 | 896,573 | ×3.260 | 3,800 | ×2.484 |
| redalert2mod_consortium | `steelconsortium_stalker` | 140,000 | 280,000 | 456,437 | ×3.260 | 4,000 | ×2.447 |
| redalert2mod_consortium | `steelconsortium_whiterabbit` | 150,000 | 300,000 | 489,040 | ×3.260 | 4,500 | ×2.191 |
| starcraft_protoss | `protoss_arbitertribunal` | 250,000 | 250,000 | 532,533 | ×2.130 | 2,500 | ×2.130 |
| starcraft_protoss | `protoss_observatory` | 250,000 | 250,000 | 532,533 | ×2.130 | 2,500 | ×2.130 |
| starcraft_protoss | `protoss_pylon` | 250,000 | 250,000 | 532,533 | ×2.130 | 1,000 | ×2.130 |
| starcraft_protoss | `protoss_templararchives` | 250,000 | 250,000 | 532,533 | ×2.130 | 2,500 | ×2.130 |
| starcraft_protoss | `protoss_roboticsfacility` | 400,000 | 400,000 | 852,053 | ×2.130 | 2,000 | ×2.130 |
| starcraft_protoss | `protoss_fleetbeacon` | 1,000,000 | 1,000,000 | 2,130,133 | ×2.130 | 10,000 | ×2.130 |
| starcraft_protoss | `protoss_nexus` | 1,000,000 | 1,000,000 | 2,130,133 | ×2.130 | 5,000 | ×2.130 |
| starcraft_protoss | `protoss_assimilator` | 300,000 | 300,000 | 639,040 | ×2.130 | 3,000 | ×2.130 |
| starcraft_protoss | `protoss_cyberneticscore` | 150,000 | 150,000 | 319,520 | ×2.130 | 1,500 | ×2.130 |
| starcraft_protoss | `protoss_forge` | 150,000 | 150,000 | 319,520 | ×2.130 | 1,500 | ×2.130 |
| starcraft_protoss | `protoss_stargate` | 300,000 | 300,000 | 639,040 | ×2.130 | 1,500 | ×2.130 |
| starcraft_protoss | `protoss_citadelofadun` | 200,000 | 200,000 | 426,026 | ×2.130 | 2,000 | ×2.130 |
| starcraft_protoss | `protoss_gateway` | 200,000 | 200,000 | 426,026 | ×2.130 | 1,000 | ×2.130 |
| starcraft_protoss | `protoss_roboticssupportbay` | 200,000 | 200,000 | 426,026 | ×2.130 | 2,000 | ×2.130 |
| starcraft_protoss | `protoss_mobilenexus` | 300,000 | 300,000 | 639,040 | ×2.130 | 5,000 | ×2.027 |
| redalert2mod_consortium | `steel_cruiser_f` | 37,500 | 75,000 | 122,260 | ×3.260 | 125 | ×1.958 |
| starcraft_protoss | `protoss_starshipsovereign` | 750,000 | 750,000 | 1,597,600 | ×2.130 | 10,000 | ×1.933 |
| redalert2mod_consortium | `steelconsortium_supportshieldgenerator` | 100,000 | 200,000 | 326,026 | ×3.260 | 3,000 | ×1.904 |
| redalert2mod_consortium | `steelconsortium_empressstation` | 1,500,000 | 1,500,000 | 3,195,199 | ×2.130 | 5,000 | ×1.894 |
| starcraft_protoss | `protoss_archon` | 350,000 | 350,000 | 745,546 | ×2.130 | 5,600 | ×1.853 |
| starcraft_protoss | `protoss_epigraph` | 200,000 | 200,000 | 426,026 | ×2.130 | 2,600 | ×1.825 |
| starcraft_protoss | `protoss_shuttle` | 100,000 | 100,000 | 213,013 | ×2.130 | 6,000 | ×1.822 |
| starcraft_protoss | `protoss_carrier` | 300,000 | 300,000 | 639,040 | ×2.130 | 3,000 | ×1.790 |
| starcraft_protoss | `protoss_idol` | 350,000 | 350,000 | 745,546 | ×2.130 | 2,800 | ×1.784 |
| starcraft_protoss | `protoss_arbiter` | 225,000 | 225,000 | 479,280 | ×2.130 | 4,800 | ×1.775 |
| starcraft_protoss | `protoss_atreus` | 250,000 | 250,000 | 532,533 | ×2.130 | 2,400 | ×1.763 |
| starcraft_protoss | `protoss_zeratul` | 250,000 | 250,000 | 532,533 | ×2.130 | 4,000 | ×1.761 |
| starcraft_protoss | `protoss_corsair` | 125,000 | 125,000 | 266,267 | ×2.130 | 2,100 | ×1.749 |
| redalert2mod_consortium | `steelconsortium_cloudbreaker` | 250,000 | 250,000 | 532,533 | ×2.130 | 5,000 | ×1.743 |
| starcraft_protoss | `protoss_reaver` | 275,000 | 275,000 | 585,786 | ×2.130 | 2,700 | ×1.719 |
| redalert2mod_consortium | `steel_cougar` | 25,000 | 50,000 | 81,507 | ×3.260 | 1,200 | ×1.659 |
| starcraft_protoss | `protoss_voidray` | 70,000 | 70,000 | 149,109 | ×2.130 | 1,400 | ×1.658 |
| starcraft_protoss | `protoss_scout` | 75,000 | 75,000 | 159,760 | ×2.130 | 1,500 | ×1.656 |
| redalert2mod_consortium | `steel_hummer` | 45,000 | 67,500 | 121,284 | ×2.695 | 2,000 | ×1.653 |
| redalert2mod_consortium | `steel_oldqtnk` | 112,500 | 112,500 | 239,640 | ×2.130 | 2,400 | ×1.644 |
| starcraft_protoss | `protoss_shieldbattery` | 100,000 | 100,000 | 213,013 | ×2.130 | 1,000 | ×1.565 |
| starcraft_protoss | `protoss_positron` | 60,000 | 60,000 | 127,808 | ×2.130 | 1,200 | ×1.548 |
| starcraft_protoss | `protoss_dragoon` | 75,000 | 75,000 | 159,760 | ×2.130 | 1,200 | ×1.508 |
| starcraft_protoss | `protoss_gladius` | 100,000 | 100,000 | 213,013 | ×2.130 | 1,800 | ×1.502 |
| starcraft_protoss | `protoss_patriarch` | 75,000 | 75,000 | 159,760 | ×2.130 | 4,000 | ×1.497 |
| redalert2mod_consortium | `steel_cobra` | 325,000 | 162,500 | 508,647 | ×1.565 | 3,600 | ×1.417 |
| starcraft_protoss | `protoss_probe` | 45,000 | 45,000 | 95,856 | ×2.130 | 500 | ×1.396 |
| starcraft_protoss | `protoss_analogue` | 60,000 | 60,000 | 127,808 | ×2.130 | 1,200 | ×1.395 |
| starcraft_protoss | `protoss_amaranth` | 70,000 | 70,000 | 149,109 | ×2.130 | 1,200 | ×1.378 |
| starcraft_protoss | `protoss_observer` | 12,500 | 12,500 | 26,627 | ×2.130 | 500 | ×1.355 |
| starcraft_protoss | `protoss_darktemplar` | 50,000 | 50,000 | 106,507 | ×2.130 | 600 | ×1.338 |
| starcraft_protoss | `protoss_zealot` | 40,000 | 40,000 | 85,205 | ×2.130 | 300 | ×1.306 |
| starcraft_protoss | `protoss_legionnaire` | 60,000 | 60,000 | 127,808 | ×2.130 | 700 | ×1.298 |
| starcraft_protoss | `protoss_adept` | 30,000 | 30,000 | 63,904 | ×2.130 | 650 | ×1.272 |
| starcraft_protoss | `protoss_manifold` | 25,000 | 25,000 | 53,253 | ×2.130 | 600 | ×1.248 |
| starcraft_protoss | `protoss_photoncannon` | 200,000 | 200,000 | 426,026 | ×2.130 | 2,000 | ×1.029 |
