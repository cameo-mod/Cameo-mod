# audit_survivability_pricing — E1: the baseline shield is priced at ZERO

| bucket | actors | priced today | belongs to |
|---|--:|---|---|
| spawns with a pool (**always-on**) | 56 | ✖ nothing | **E1 — this report** |
| empty capacity, needs `shieldgen` | 1163 | ✔ correctly nothing | — (it has no shield) |
| pool behind an upgrade | 210 | ✖ nothing | E5 (upgrade pricing) |

Shield row mean Versus **186.79**, so one shield point is **0.5354 HP** BEFORE any shield-gated `DamageMultiplier` — measured off the live ladder every run, never frozen. The Shield row takes **1.460%** of all roster raw damage at baseline.

⚠ **Every one of these 56 actors also carries `DamageMultiplier@shielded: 150`**, so it takes 150% damage WHILE the shield holds — the deliberate counterweight to having one. That divides the pool's worth: a shield point is really **0.3569 HP**, and the roster-wide gap is 38.6% rather than the 57.8% a shield-only reading gives. `shield_damage_multiplier` and `shield_hp_per_point` are published per actor.

## The gap

* Raw HP across these 56 actors: **12,812,500**
* Effective HP once the pool is counted: **20,162,956** (**+57.4%**)
* Implied price change if the formula read effective HP: median **×1.375**, max **×1.746**

⚠ **Retiring the 150% multiplier is a BUFF that must be paid for.** The numbers above already account for it, so they price the game AS IT IS. Delete `DamageMultiplier@shielded` and a shield point jumps from 0.357 to 0.535 HP — the same pool becomes 1.5x more valuable and the implied price rises again. Re-extract AFTER the deletion and price once, or these units get charged for durability they no longer have (or keep durability they were never charged for).

## Per actor

| pack | actor | HP | shield pool | effective HP | ×HP | cost | implied ×price |
|---|---|--:|--:|--:|--:|--:|--:|
| redalert2mod_consortium | `steelconsortium_defenderbot` | 210,000 | 420,000 | 434,850 | ×2.071 | 3,200 | ×1.746 |
| redalert2mod_consortium | `steelconsortium_skyhammer` | 120,000 | 240,000 | 248,486 | ×2.071 | 4,500 | ×1.738 |
| redalert2mod_consortium | `steelconsortium_katytank` | 275,000 | 550,000 | 569,446 | ×2.071 | 3,800 | ×1.702 |
| redalert2mod_consortium | `steelconsortium_stalker` | 140,000 | 280,000 | 289,900 | ×2.071 | 4,000 | ×1.685 |
| redalert2mod_consortium | `steelconsortium_whiterabbit` | 150,000 | 300,000 | 310,607 | ×2.071 | 4,500 | ×1.567 |
| redalert2mod_consortium | `cruiser_f.steel` | 37,500 | 75,000 | 77,652 | ×2.071 | 125 | ×1.558 |
| starcraft_protoss | `protoss_citadelofadun` | 200,000 | 200,000 | 307,072 | ×1.535 | 2,000 | ×1.535 |
| starcraft_protoss | `protoss_gateway` | 200,000 | 200,000 | 307,072 | ×1.535 | 1,000 | ×1.535 |
| starcraft_protoss | `protoss_roboticssupportbay` | 200,000 | 200,000 | 307,072 | ×1.535 | 2,000 | ×1.535 |
| starcraft_protoss | `protoss_assimilator` | 300,000 | 300,000 | 460,607 | ×1.535 | 3,000 | ×1.535 |
| starcraft_protoss | `protoss_cyberneticscore` | 150,000 | 150,000 | 230,304 | ×1.535 | 1,500 | ×1.535 |
| starcraft_protoss | `protoss_forge` | 150,000 | 150,000 | 230,304 | ×1.535 | 1,500 | ×1.535 |
| starcraft_protoss | `protoss_stargate` | 300,000 | 300,000 | 460,607 | ×1.535 | 1,500 | ×1.535 |
| starcraft_protoss | `protoss_fleetbeacon` | 1,000,000 | 1,000,000 | 1,535,357 | ×1.535 | 10,000 | ×1.535 |
| starcraft_protoss | `protoss_nexus` | 1,000,000 | 1,000,000 | 1,535,357 | ×1.535 | 5,000 | ×1.535 |
| starcraft_protoss | `protoss_roboticsfacility` | 400,000 | 400,000 | 614,143 | ×1.535 | 2,000 | ×1.535 |
| starcraft_protoss | `protoss_arbitertribunal` | 250,000 | 250,000 | 383,839 | ×1.535 | 2,500 | ×1.535 |
| starcraft_protoss | `protoss_observatory` | 250,000 | 250,000 | 383,839 | ×1.535 | 2,500 | ×1.535 |
| starcraft_protoss | `protoss_pylon` | 250,000 | 250,000 | 383,839 | ×1.535 | 1,000 | ×1.535 |
| starcraft_protoss | `protoss_templararchives` | 250,000 | 250,000 | 383,839 | ×1.535 | 2,500 | ×1.535 |
| starcraft_protoss | `protoss_mobilenexus` | 300,000 | 300,000 | 460,607 | ×1.535 | 5,000 | ×1.487 |
| starcraft_protoss | `protoss_starshipsovereign` | 750,000 | 750,000 | 1,151,518 | ×1.535 | 10,000 | ×1.447 |
| redalert2mod_consortium | `steelconsortium_supportshieldgenerator` | 100,000 | 200,000 | 207,072 | ×2.071 | 3,000 | ×1.428 |
| redalert2mod_consortium | `steelconsortium_empressstation` | 1,500,000 | 1,500,000 | 2,303,036 | ×1.535 | 5,000 | ×1.424 |
| starcraft_protoss | `protoss_archon` | 350,000 | 350,000 | 537,375 | ×1.535 | 5,600 | ×1.403 |
| starcraft_protoss | `protoss_epigraph` | 200,000 | 200,000 | 307,072 | ×1.535 | 2,600 | ×1.392 |
| starcraft_protoss | `protoss_shuttle` | 100,000 | 100,000 | 153,536 | ×1.535 | 6,000 | ×1.389 |
| starcraft_protoss | `protoss_arbiter` | 225,000 | 225,000 | 345,455 | ×1.535 | 4,800 | ×1.376 |
| starcraft_protoss | `protoss_carrier` | 300,000 | 300,000 | 460,607 | ×1.535 | 3,000 | ×1.374 |
| starcraft_protoss | `protoss_idol` | 350,000 | 350,000 | 537,375 | ×1.535 | 2,800 | ×1.371 |
| redalert2mod_consortium | `cougar.steel` | 25,000 | 50,000 | 51,768 | ×2.071 | 1,200 | ×1.365 |
| starcraft_protoss | `protoss_atreus` | 250,000 | 250,000 | 383,839 | ×1.535 | 2,400 | ×1.363 |
| starcraft_protoss | `protoss_corsair` | 125,000 | 125,000 | 191,920 | ×1.535 | 2,100 | ×1.358 |
| starcraft_protoss | `protoss_zeratul` | 250,000 | 250,000 | 383,839 | ×1.535 | 4,000 | ×1.354 |
| redalert2mod_consortium | `steelconsortium_cloudbreaker` | 250,000 | 250,000 | 383,839 | ×1.535 | 5,000 | ×1.351 |
| starcraft_protoss | `protoss_reaver` | 275,000 | 275,000 | 422,223 | ×1.535 | 2,700 | ×1.337 |
| redalert2mod_consortium | `hummer.steel` | 45,000 | 67,500 | 81,137 | ×1.803 | 2,000 | ×1.336 |
| starcraft_protoss | `protoss_scout` | 75,000 | 75,000 | 115,152 | ×1.535 | 1,500 | ×1.313 |
| redalert2mod_consortium | `oldqtnk.steel` | 112,500 | 112,500 | 172,728 | ×1.535 | 2,400 | ×1.305 |
| starcraft_protoss | `protoss_voidray` | 70,000 | 70,000 | 107,475 | ×1.535 | 1,400 | ×1.303 |
| starcraft_protoss | `protoss_shieldbattery` | 100,000 | 100,000 | 153,536 | ×1.535 | 1,000 | ×1.268 |
| starcraft_protoss | `protoss_positron` | 60,000 | 60,000 | 92,121 | ×1.535 | 1,200 | ×1.262 |
| starcraft_protoss | `protoss_dragoon` | 75,000 | 75,000 | 115,152 | ×1.535 | 1,200 | ×1.249 |
| starcraft_protoss | `protoss_patriarch` | 75,000 | 75,000 | 115,152 | ×1.535 | 4,000 | ×1.241 |
| starcraft_protoss | `protoss_gladius` | 100,000 | 100,000 | 153,536 | ×1.535 | 1,800 | ×1.238 |
| redalert2mod_consortium | `cobra.steel` | 325,000 | 162,500 | 411,996 | ×1.268 | 3,600 | ×1.197 |
| starcraft_protoss | `protoss_amaranth` | 70,000 | 70,000 | 107,475 | ×1.535 | 1,200 | ×1.169 |
| starcraft_protoss | `protoss_observer` | 12,500 | 12,500 | 19,192 | ×1.535 | 500 | ×1.168 |
| starcraft_protoss | `protoss_probe` | 45,000 | 45,000 | 69,091 | ×1.535 | 500 | ×1.168 |
| starcraft_protoss | `protoss_analogue` | 60,000 | 60,000 | 92,121 | ×1.535 | 1,200 | ×1.164 |
| starcraft_protoss | `protoss_darktemplar` | 50,000 | 50,000 | 76,768 | ×1.535 | 600 | ×1.142 |
| starcraft_protoss | `protoss_legionnaire` | 60,000 | 60,000 | 92,121 | ×1.535 | 700 | ×1.126 |
| starcraft_protoss | `protoss_adept` | 30,000 | 30,000 | 46,061 | ×1.535 | 650 | ×1.123 |
| starcraft_protoss | `protoss_zealot` | 40,000 | 40,000 | 61,414 | ×1.535 | 300 | ×1.122 |
| starcraft_protoss | `protoss_manifold` | 25,000 | 25,000 | 38,384 | ×1.535 | 600 | ×1.117 |
| starcraft_protoss | `protoss_photoncannon` | 200,000 | 200,000 | 307,072 | ×1.535 | 2,000 | ×1.063 |
