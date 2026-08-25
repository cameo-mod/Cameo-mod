# audit_survivability_pricing — E1: the baseline shield is priced at ZERO

| bucket | actors | priced today | belongs to |
|---|--:|---|---|
| spawns with a pool (**always-on**) | 56 | ✖ nothing | **E1 — this report** |
| empty capacity, needs `shieldgen` | 1168 | ✔ correctly nothing | — (it has no shield) |
| pool behind an upgrade | 214 | ✖ nothing | E5 (upgrade pricing) |

Shield row mean Versus **190.80**, so one shield point is **0.5241 HP** BEFORE any shield-gated `DamageMultiplier` — measured off the live ladder every run, never frozen. The Shield row takes **1.439%** of all roster raw damage at baseline.

⚠ **Every one of these 56 actors also carries `DamageMultiplier@shielded: 150`**, so it takes 150% damage WHILE the shield holds — the deliberate counterweight to having one. That divides the pool's worth: a shield point is really **0.3494 HP**, and the roster-wide gap is 38.6% rather than the 57.8% a shield-only reading gives. `shield_damage_multiplier` and `shield_hp_per_point` are published per actor.

## The gap

* Raw HP across these 56 actors: **12,812,500**
* Effective HP once the pool is counted: **20,008,396** (**+56.2%**)
* Implied price change if the formula read effective HP: median **×1.367**, max **×1.729**

⚠ **Retiring the 150% multiplier is a BUFF that must be paid for.** The numbers above already account for it, so they price the game AS IT IS. Delete `DamageMultiplier@shielded` and a shield point jumps from 0.349 to 0.524 HP — the same pool becomes 1.5x more valuable and the implied price rises again. Re-extract AFTER the deletion and price once, or these units get charged for durability they no longer have (or keep durability they were never charged for).

## Per actor

| pack | actor | HP | shield pool | effective HP | ×HP | cost | implied ×price |
|---|---|--:|--:|--:|--:|--:|--:|
| redalert2mod_consortium | `steelconsortium_defenderbot` | 210,000 | 420,000 | 430,122 | ×2.048 | 3,200 | ×1.729 |
| redalert2mod_consortium | `steelconsortium_skyhammer` | 120,000 | 240,000 | 245,784 | ×2.048 | 4,500 | ×1.722 |
| redalert2mod_consortium | `steelconsortium_katytank` | 275,000 | 550,000 | 563,255 | ×2.048 | 3,800 | ×1.687 |
| redalert2mod_consortium | `steelconsortium_stalker` | 140,000 | 280,000 | 286,748 | ×2.048 | 4,000 | ×1.671 |
| redalert2mod_consortium | `steelconsortium_whiterabbit` | 150,000 | 300,000 | 307,230 | ×2.048 | 4,500 | ×1.555 |
| starcraft_protoss | `protoss_arbitertribunal` | 250,000 | 250,000 | 381,025 | ×1.524 | 2,500 | ×1.524 |
| starcraft_protoss | `protoss_observatory` | 250,000 | 250,000 | 381,025 | ×1.524 | 2,500 | ×1.524 |
| starcraft_protoss | `protoss_pylon` | 250,000 | 250,000 | 381,025 | ×1.524 | 1,000 | ×1.524 |
| starcraft_protoss | `protoss_templararchives` | 250,000 | 250,000 | 381,025 | ×1.524 | 2,500 | ×1.524 |
| starcraft_protoss | `protoss_assimilator` | 300,000 | 300,000 | 457,230 | ×1.524 | 3,000 | ×1.524 |
| starcraft_protoss | `protoss_stargate` | 300,000 | 300,000 | 457,230 | ×1.524 | 1,500 | ×1.524 |
| starcraft_protoss | `protoss_roboticsfacility` | 400,000 | 400,000 | 609,640 | ×1.524 | 2,000 | ×1.524 |
| starcraft_protoss | `protoss_fleetbeacon` | 1,000,000 | 1,000,000 | 1,524,100 | ×1.524 | 10,000 | ×1.524 |
| starcraft_protoss | `protoss_nexus` | 1,000,000 | 1,000,000 | 1,524,100 | ×1.524 | 5,000 | ×1.524 |
| starcraft_protoss | `protoss_citadelofadun` | 200,000 | 200,000 | 304,820 | ×1.524 | 2,000 | ×1.524 |
| starcraft_protoss | `protoss_cyberneticscore` | 150,000 | 150,000 | 228,615 | ×1.524 | 1,500 | ×1.524 |
| starcraft_protoss | `protoss_forge` | 150,000 | 150,000 | 228,615 | ×1.524 | 1,500 | ×1.524 |
| starcraft_protoss | `protoss_gateway` | 200,000 | 200,000 | 304,820 | ×1.524 | 1,000 | ×1.524 |
| starcraft_protoss | `protoss_roboticssupportbay` | 200,000 | 200,000 | 304,820 | ×1.524 | 2,000 | ×1.524 |
| redalert2mod_consortium | `cruiser_f.steel` | 37,500 | 75,000 | 76,808 | ×2.048 | 125 | ×1.514 |
| starcraft_protoss | `protoss_mobilenexus` | 300,000 | 300,000 | 457,230 | ×1.524 | 5,000 | ×1.476 |
| starcraft_protoss | `protoss_starshipsovereign` | 750,000 | 750,000 | 1,143,075 | ×1.524 | 10,000 | ×1.438 |
| redalert2mod_consortium | `steelconsortium_supportshieldgenerator` | 100,000 | 200,000 | 204,820 | ×2.048 | 3,000 | ×1.419 |
| redalert2mod_consortium | `steelconsortium_empressstation` | 1,500,000 | 1,500,000 | 2,286,150 | ×1.524 | 5,000 | ×1.415 |
| starcraft_protoss | `protoss_archon` | 350,000 | 350,000 | 533,435 | ×1.524 | 5,600 | ×1.395 |
| starcraft_protoss | `protoss_epigraph` | 200,000 | 200,000 | 304,820 | ×1.524 | 2,600 | ×1.384 |
| starcraft_protoss | `protoss_shuttle` | 100,000 | 100,000 | 152,410 | ×1.524 | 6,000 | ×1.381 |
| starcraft_protoss | `protoss_arbiter` | 225,000 | 225,000 | 342,923 | ×1.524 | 4,800 | ×1.368 |
| starcraft_protoss | `protoss_carrier` | 300,000 | 300,000 | 457,230 | ×1.524 | 3,000 | ×1.367 |
| starcraft_protoss | `protoss_idol` | 350,000 | 350,000 | 533,435 | ×1.524 | 2,800 | ×1.364 |
| starcraft_protoss | `protoss_atreus` | 250,000 | 250,000 | 381,025 | ×1.524 | 2,400 | ×1.360 |
| starcraft_protoss | `protoss_zeratul` | 250,000 | 250,000 | 381,025 | ×1.524 | 4,000 | ×1.355 |
| redalert2mod_consortium | `cougar.steel` | 25,000 | 50,000 | 51,205 | ×2.048 | 1,200 | ×1.353 |
| starcraft_protoss | `protoss_corsair` | 125,000 | 125,000 | 190,512 | ×1.524 | 2,100 | ×1.348 |
| redalert2mod_consortium | `steelconsortium_cloudbreaker` | 250,000 | 250,000 | 381,025 | ×1.524 | 5,000 | ×1.345 |
| starcraft_protoss | `protoss_reaver` | 275,000 | 275,000 | 419,128 | ×1.524 | 2,700 | ×1.331 |
| redalert2mod_consortium | `hummer.steel` | 45,000 | 67,500 | 80,377 | ×1.786 | 2,000 | ×1.318 |
| starcraft_protoss | `protoss_scout` | 75,000 | 75,000 | 114,308 | ×1.524 | 1,500 | ×1.305 |
| redalert2mod_consortium | `oldqtnk.steel` | 112,500 | 112,500 | 171,461 | ×1.524 | 2,400 | ×1.299 |
| starcraft_protoss | `protoss_voidray` | 70,000 | 70,000 | 106,687 | ×1.524 | 1,400 | ×1.297 |
| starcraft_protoss | `protoss_shieldbattery` | 100,000 | 100,000 | 152,410 | ×1.524 | 1,000 | ×1.262 |
| starcraft_protoss | `protoss_positron` | 60,000 | 60,000 | 91,446 | ×1.524 | 1,200 | ×1.256 |
| starcraft_protoss | `protoss_dragoon` | 75,000 | 75,000 | 114,308 | ×1.524 | 1,200 | ×1.243 |
| starcraft_protoss | `protoss_patriarch` | 75,000 | 75,000 | 114,308 | ×1.524 | 4,000 | ×1.236 |
| starcraft_protoss | `protoss_gladius` | 100,000 | 100,000 | 152,410 | ×1.524 | 1,800 | ×1.233 |
| redalert2mod_consortium | `cobra.steel` | 325,000 | 162,500 | 410,166 | ×1.262 | 3,600 | ×1.193 |
| starcraft_protoss | `protoss_analogue` | 60,000 | 60,000 | 91,446 | ×1.524 | 1,200 | ×1.188 |
| starcraft_protoss | `protoss_probe` | 45,000 | 45,000 | 68,584 | ×1.524 | 500 | ×1.188 |
| starcraft_protoss | `protoss_amaranth` | 70,000 | 70,000 | 106,687 | ×1.524 | 1,200 | ×1.178 |
| starcraft_protoss | `protoss_observer` | 12,500 | 12,500 | 19,051 | ×1.524 | 500 | ×1.165 |
| starcraft_protoss | `protoss_darktemplar` | 50,000 | 50,000 | 76,205 | ×1.524 | 600 | ×1.158 |
| starcraft_protoss | `protoss_zealot` | 40,000 | 40,000 | 60,964 | ×1.524 | 300 | ×1.148 |
| starcraft_protoss | `protoss_legionnaire` | 60,000 | 60,000 | 91,446 | ×1.524 | 700 | ×1.142 |
| starcraft_protoss | `protoss_adept` | 30,000 | 30,000 | 45,723 | ×1.524 | 650 | ×1.123 |
| starcraft_protoss | `protoss_manifold` | 25,000 | 25,000 | 38,102 | ×1.524 | 600 | ×1.115 |
| starcraft_protoss | `protoss_photoncannon` | 200,000 | 200,000 | 304,820 | ×1.524 | 2,000 | ×1.062 |
