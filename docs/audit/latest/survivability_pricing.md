# audit_survivability_pricing — E1: the baseline shield is priced at ZERO

| bucket | actors | priced today | belongs to |
|---|--:|---|---|
| spawns with a pool (**always-on**) | 56 | ✖ nothing | **E1 — this report** |
| empty capacity, needs `shieldgen` | 1163 | ✔ correctly nothing | — (it has no shield) |
| pool behind an upgrade | 210 | ✖ nothing | E5 (upgrade pricing) |

Shield row mean Versus **189.09**, so one shield point is **0.5289 HP** BEFORE any shield-gated `DamageMultiplier` — measured off the live ladder every run, never frozen. The Shield row takes **1.473%** of all roster raw damage at baseline.

⚠ **Every one of these 56 actors also carries `DamageMultiplier@shielded: 150`**, so it takes 150% damage WHILE the shield holds — the deliberate counterweight to having one. That divides the pool's worth: a shield point is really **0.3526 HP**, and the roster-wide gap is 38.6% rather than the 57.8% a shield-only reading gives. `shield_damage_multiplier` and `shield_hp_per_point` are published per actor.

## The gap

* Raw HP across these 56 actors: **12,812,500**
* Effective HP once the pool is counted: **20,073,673** (**+56.7%**)
* Implied price change if the formula read effective HP: median **×1.371**, max **×1.736**

⚠ **Retiring the 150% multiplier is a BUFF that must be paid for.** The numbers above already account for it, so they price the game AS IT IS. Delete `DamageMultiplier@shielded` and a shield point jumps from 0.353 to 0.529 HP — the same pool becomes 1.5x more valuable and the implied price rises again. Re-extract AFTER the deletion and price once, or these units get charged for durability they no longer have (or keep durability they were never charged for).

## Per actor

| pack | actor | HP | shield pool | effective HP | ×HP | cost | implied ×price |
|---|---|--:|--:|--:|--:|--:|--:|
| redalert2mod_consortium | `steelconsortium_defenderbot` | 210,000 | 420,000 | 432,119 | ×2.058 | 3,200 | ×1.736 |
| redalert2mod_consortium | `steelconsortium_skyhammer` | 120,000 | 240,000 | 246,925 | ×2.058 | 4,500 | ×1.729 |
| redalert2mod_consortium | `steelconsortium_katytank` | 275,000 | 550,000 | 565,870 | ×2.058 | 3,800 | ×1.694 |
| redalert2mod_consortium | `steelconsortium_stalker` | 140,000 | 280,000 | 288,079 | ×2.058 | 4,000 | ×1.677 |
| redalert2mod_consortium | `steelconsortium_whiterabbit` | 150,000 | 300,000 | 308,656 | ×2.058 | 4,500 | ×1.560 |
| starcraft_protoss | `protoss_assimilator` | 300,000 | 300,000 | 458,656 | ×1.529 | 3,000 | ×1.529 |
| starcraft_protoss | `protoss_cyberneticscore` | 150,000 | 150,000 | 229,328 | ×1.529 | 1,500 | ×1.529 |
| starcraft_protoss | `protoss_forge` | 150,000 | 150,000 | 229,328 | ×1.529 | 1,500 | ×1.529 |
| starcraft_protoss | `protoss_stargate` | 300,000 | 300,000 | 458,656 | ×1.529 | 1,500 | ×1.529 |
| starcraft_protoss | `protoss_citadelofadun` | 200,000 | 200,000 | 305,771 | ×1.529 | 2,000 | ×1.529 |
| starcraft_protoss | `protoss_fleetbeacon` | 1,000,000 | 1,000,000 | 1,528,854 | ×1.529 | 10,000 | ×1.529 |
| starcraft_protoss | `protoss_gateway` | 200,000 | 200,000 | 305,771 | ×1.529 | 1,000 | ×1.529 |
| starcraft_protoss | `protoss_nexus` | 1,000,000 | 1,000,000 | 1,528,854 | ×1.529 | 5,000 | ×1.529 |
| starcraft_protoss | `protoss_roboticsfacility` | 400,000 | 400,000 | 611,542 | ×1.529 | 2,000 | ×1.529 |
| starcraft_protoss | `protoss_roboticssupportbay` | 200,000 | 200,000 | 305,771 | ×1.529 | 2,000 | ×1.529 |
| starcraft_protoss | `protoss_arbitertribunal` | 250,000 | 250,000 | 382,214 | ×1.529 | 2,500 | ×1.529 |
| starcraft_protoss | `protoss_observatory` | 250,000 | 250,000 | 382,214 | ×1.529 | 2,500 | ×1.529 |
| starcraft_protoss | `protoss_pylon` | 250,000 | 250,000 | 382,214 | ×1.529 | 1,000 | ×1.529 |
| starcraft_protoss | `protoss_templararchives` | 250,000 | 250,000 | 382,214 | ×1.529 | 2,500 | ×1.529 |
| redalert2mod_consortium | `cruiser_f.steel` | 37,500 | 75,000 | 77,164 | ×2.058 | 125 | ×1.518 |
| starcraft_protoss | `protoss_mobilenexus` | 300,000 | 300,000 | 458,656 | ×1.529 | 5,000 | ×1.481 |
| starcraft_protoss | `protoss_starshipsovereign` | 750,000 | 750,000 | 1,146,641 | ×1.529 | 10,000 | ×1.442 |
| redalert2mod_consortium | `steelconsortium_supportshieldgenerator` | 100,000 | 200,000 | 205,771 | ×2.058 | 3,000 | ×1.423 |
| redalert2mod_consortium | `steelconsortium_empressstation` | 1,500,000 | 1,500,000 | 2,293,282 | ×1.529 | 5,000 | ×1.419 |
| starcraft_protoss | `protoss_archon` | 350,000 | 350,000 | 535,099 | ×1.529 | 5,600 | ×1.399 |
| starcraft_protoss | `protoss_epigraph` | 200,000 | 200,000 | 305,771 | ×1.529 | 2,600 | ×1.387 |
| starcraft_protoss | `protoss_shuttle` | 100,000 | 100,000 | 152,886 | ×1.529 | 6,000 | ×1.385 |
| starcraft_protoss | `protoss_arbiter` | 225,000 | 225,000 | 343,992 | ×1.529 | 4,800 | ×1.371 |
| starcraft_protoss | `protoss_carrier` | 300,000 | 300,000 | 458,656 | ×1.529 | 3,000 | ×1.370 |
| starcraft_protoss | `protoss_idol` | 350,000 | 350,000 | 535,099 | ×1.529 | 2,800 | ×1.368 |
| starcraft_protoss | `protoss_atreus` | 250,000 | 250,000 | 382,214 | ×1.529 | 2,400 | ×1.364 |
| starcraft_protoss | `protoss_zeratul` | 250,000 | 250,000 | 382,214 | ×1.529 | 4,000 | ×1.358 |
| redalert2mod_consortium | `cougar.steel` | 25,000 | 50,000 | 51,443 | ×2.058 | 1,200 | ×1.357 |
| starcraft_protoss | `protoss_corsair` | 125,000 | 125,000 | 191,107 | ×1.529 | 2,100 | ×1.351 |
| redalert2mod_consortium | `steelconsortium_cloudbreaker` | 250,000 | 250,000 | 382,214 | ×1.529 | 5,000 | ×1.348 |
| starcraft_protoss | `protoss_reaver` | 275,000 | 275,000 | 420,435 | ×1.529 | 2,700 | ×1.334 |
| redalert2mod_consortium | `hummer.steel` | 45,000 | 67,500 | 80,698 | ×1.793 | 2,000 | ×1.321 |
| starcraft_protoss | `protoss_scout` | 75,000 | 75,000 | 114,664 | ×1.529 | 1,500 | ×1.308 |
| redalert2mod_consortium | `oldqtnk.steel` | 112,500 | 112,500 | 171,996 | ×1.529 | 2,400 | ×1.302 |
| starcraft_protoss | `protoss_voidray` | 70,000 | 70,000 | 107,020 | ×1.529 | 1,400 | ×1.300 |
| starcraft_protoss | `protoss_shieldbattery` | 100,000 | 100,000 | 152,886 | ×1.529 | 1,000 | ×1.264 |
| starcraft_protoss | `protoss_positron` | 60,000 | 60,000 | 91,731 | ×1.529 | 1,200 | ×1.258 |
| starcraft_protoss | `protoss_dragoon` | 75,000 | 75,000 | 114,664 | ×1.529 | 1,200 | ×1.246 |
| starcraft_protoss | `protoss_patriarch` | 75,000 | 75,000 | 114,664 | ×1.529 | 4,000 | ×1.238 |
| starcraft_protoss | `protoss_gladius` | 100,000 | 100,000 | 152,886 | ×1.529 | 1,800 | ×1.235 |
| redalert2mod_consortium | `cobra.steel` | 325,000 | 162,500 | 410,939 | ×1.264 | 3,600 | ×1.195 |
| starcraft_protoss | `protoss_analogue` | 60,000 | 60,000 | 91,731 | ×1.529 | 1,200 | ×1.190 |
| starcraft_protoss | `protoss_probe` | 45,000 | 45,000 | 68,798 | ×1.529 | 500 | ×1.190 |
| starcraft_protoss | `protoss_amaranth` | 70,000 | 70,000 | 107,020 | ×1.529 | 1,200 | ×1.179 |
| starcraft_protoss | `protoss_observer` | 12,500 | 12,500 | 19,111 | ×1.529 | 500 | ×1.166 |
| starcraft_protoss | `protoss_darktemplar` | 50,000 | 50,000 | 76,443 | ×1.529 | 600 | ×1.159 |
| starcraft_protoss | `protoss_zealot` | 40,000 | 40,000 | 61,154 | ×1.529 | 300 | ×1.149 |
| starcraft_protoss | `protoss_legionnaire` | 60,000 | 60,000 | 91,731 | ×1.529 | 700 | ×1.143 |
| starcraft_protoss | `protoss_adept` | 30,000 | 30,000 | 45,866 | ×1.529 | 650 | ×1.124 |
| starcraft_protoss | `protoss_manifold` | 25,000 | 25,000 | 38,221 | ×1.529 | 600 | ×1.116 |
| starcraft_protoss | `protoss_photoncannon` | 200,000 | 200,000 | 305,771 | ×1.529 | 2,000 | ×1.062 |
