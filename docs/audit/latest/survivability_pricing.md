# audit_survivability_pricing — E1: the baseline shield is priced at ZERO

| bucket | actors | priced today | belongs to |
|---|--:|---|---|
| spawns with a pool (**always-on**) | 56 | ✖ nothing | **E1 — this report** |
| empty capacity, needs `shieldgen` | 1152 | ✔ correctly nothing | — (it has no shield) |
| pool behind an upgrade | 210 | ✖ nothing | E5 (upgrade pricing) |

Shield row mean Versus **185.25**, so one shield point is **0.5398 HP** — measured off the live ladder every run, never frozen. The Shield row takes **1.432%** of all roster raw damage at baseline.

## The gap

* Raw HP across these 56 actors: **12,812,500**
* Effective HP once the pool is counted: **20,224,106** (**+57.8%**)
* Implied price change if the formula read effective HP: median **×1.378**, max **×1.752**

⚠ The maintainer also noted the Protoss carry a **150% damage multiplier** to compensate for their shields. Pricing the shield is the PREREQUISITE for retiring that multiplier — do both in one pass, or the faction gets charged twice.

## Per actor

| pack | actor | HP | shield pool | effective HP | ×HP | cost | implied ×price |
|---|---|--:|--:|--:|--:|--:|--:|
| redalert2mod_consortium | `steelconsortium_defenderbot` | 210,000 | 420,000 | 436,721 | ×2.080 | 3,200 | ×1.752 |
| redalert2mod_consortium | `steelconsortium_skyhammer` | 120,000 | 240,000 | 249,555 | ×2.080 | 4,500 | ×1.744 |
| redalert2mod_consortium | `steelconsortium_katytank` | 275,000 | 550,000 | 571,896 | ×2.080 | 3,800 | ×1.708 |
| redalert2mod_consortium | `steelconsortium_stalker` | 140,000 | 280,000 | 291,147 | ×2.080 | 4,000 | ×1.691 |
| redalert2mod_consortium | `steelconsortium_whiterabbit` | 150,000 | 300,000 | 311,943 | ×2.080 | 4,500 | ×1.571 |
| redalert2mod_consortium | `cruiser_f.steel` | 37,500 | 75,000 | 77,986 | ×2.080 | 125 | ×1.563 |
| starcraft_protoss | `protoss_cyberneticscore` | 150,000 | 150,000 | 230,972 | ×1.540 | 1,500 | ×1.540 |
| starcraft_protoss | `protoss_forge` | 150,000 | 150,000 | 230,972 | ×1.540 | 1,500 | ×1.540 |
| starcraft_protoss | `protoss_arbitertribunal` | 250,000 | 250,000 | 384,953 | ×1.540 | 2,500 | ×1.540 |
| starcraft_protoss | `protoss_observatory` | 250,000 | 250,000 | 384,953 | ×1.540 | 2,500 | ×1.540 |
| starcraft_protoss | `protoss_pylon` | 250,000 | 250,000 | 384,953 | ×1.540 | 1,000 | ×1.540 |
| starcraft_protoss | `protoss_templararchives` | 250,000 | 250,000 | 384,953 | ×1.540 | 2,500 | ×1.540 |
| starcraft_protoss | `protoss_fleetbeacon` | 1,000,000 | 1,000,000 | 1,539,811 | ×1.540 | 10,000 | ×1.540 |
| starcraft_protoss | `protoss_nexus` | 1,000,000 | 1,000,000 | 1,539,811 | ×1.540 | 5,000 | ×1.540 |
| starcraft_protoss | `protoss_assimilator` | 300,000 | 300,000 | 461,943 | ×1.540 | 3,000 | ×1.540 |
| starcraft_protoss | `protoss_stargate` | 300,000 | 300,000 | 461,943 | ×1.540 | 1,500 | ×1.540 |
| starcraft_protoss | `protoss_citadelofadun` | 200,000 | 200,000 | 307,962 | ×1.540 | 2,000 | ×1.540 |
| starcraft_protoss | `protoss_gateway` | 200,000 | 200,000 | 307,962 | ×1.540 | 1,000 | ×1.540 |
| starcraft_protoss | `protoss_roboticsfacility` | 400,000 | 400,000 | 615,924 | ×1.540 | 2,000 | ×1.540 |
| starcraft_protoss | `protoss_roboticssupportbay` | 200,000 | 200,000 | 307,962 | ×1.540 | 2,000 | ×1.540 |
| starcraft_protoss | `protoss_mobilenexus` | 300,000 | 300,000 | 461,943 | ×1.540 | 5,000 | ×1.491 |
| starcraft_protoss | `protoss_starshipsovereign` | 750,000 | 750,000 | 1,154,858 | ×1.540 | 10,000 | ×1.451 |
| redalert2mod_consortium | `steelconsortium_supportshieldgenerator` | 100,000 | 200,000 | 207,962 | ×2.080 | 3,000 | ×1.432 |
| redalert2mod_consortium | `steelconsortium_empressstation` | 1,500,000 | 1,500,000 | 2,309,717 | ×1.540 | 5,000 | ×1.427 |
| starcraft_protoss | `protoss_archon` | 350,000 | 350,000 | 538,934 | ×1.540 | 5,600 | ×1.406 |
| starcraft_protoss | `protoss_epigraph` | 200,000 | 200,000 | 307,962 | ×1.540 | 2,600 | ×1.395 |
| starcraft_protoss | `protoss_shuttle` | 100,000 | 100,000 | 153,981 | ×1.540 | 6,000 | ×1.393 |
| starcraft_protoss | `protoss_arbiter` | 225,000 | 225,000 | 346,458 | ×1.540 | 4,800 | ×1.379 |
| starcraft_protoss | `protoss_carrier` | 300,000 | 300,000 | 461,943 | ×1.540 | 3,000 | ×1.378 |
| starcraft_protoss | `protoss_idol` | 350,000 | 350,000 | 538,934 | ×1.540 | 2,800 | ×1.374 |
| redalert2mod_consortium | `cougar.steel` | 25,000 | 50,000 | 51,991 | ×2.080 | 1,200 | ×1.368 |
| starcraft_protoss | `protoss_atreus` | 250,000 | 250,000 | 384,953 | ×1.540 | 2,400 | ×1.366 |
| starcraft_protoss | `protoss_corsair` | 125,000 | 125,000 | 192,476 | ×1.540 | 2,100 | ×1.361 |
| starcraft_protoss | `protoss_zeratul` | 250,000 | 250,000 | 384,953 | ×1.540 | 4,000 | ×1.357 |
| redalert2mod_consortium | `steelconsortium_cloudbreaker` | 250,000 | 250,000 | 384,953 | ×1.540 | 5,000 | ×1.354 |
| starcraft_protoss | `protoss_reaver` | 275,000 | 275,000 | 423,448 | ×1.540 | 2,700 | ×1.339 |
| redalert2mod_consortium | `hummer.steel` | 45,000 | 67,500 | 81,437 | ×1.810 | 2,000 | ×1.339 |
| starcraft_protoss | `protoss_scout` | 75,000 | 75,000 | 115,486 | ×1.540 | 1,500 | ×1.315 |
| redalert2mod_consortium | `oldqtnk.steel` | 112,500 | 112,500 | 173,229 | ×1.540 | 2,400 | ×1.308 |
| starcraft_protoss | `protoss_voidray` | 70,000 | 70,000 | 107,787 | ×1.540 | 1,400 | ×1.306 |
| starcraft_protoss | `protoss_shieldbattery` | 100,000 | 100,000 | 153,981 | ×1.540 | 1,000 | ×1.270 |
| starcraft_protoss | `protoss_positron` | 60,000 | 60,000 | 92,389 | ×1.540 | 1,200 | ×1.264 |
| starcraft_protoss | `protoss_dragoon` | 75,000 | 75,000 | 115,486 | ×1.540 | 1,200 | ×1.251 |
| starcraft_protoss | `protoss_patriarch` | 75,000 | 75,000 | 115,486 | ×1.540 | 4,000 | ×1.243 |
| starcraft_protoss | `protoss_gladius` | 100,000 | 100,000 | 153,981 | ×1.540 | 1,800 | ×1.240 |
| redalert2mod_consortium | `cobra.steel` | 325,000 | 162,500 | 412,719 | ×1.270 | 3,600 | ×1.199 |
| starcraft_protoss | `protoss_amaranth` | 70,000 | 70,000 | 107,787 | ×1.540 | 1,200 | ×1.170 |
| starcraft_protoss | `protoss_observer` | 12,500 | 12,500 | 19,248 | ×1.540 | 500 | ×1.170 |
| starcraft_protoss | `protoss_probe` | 45,000 | 45,000 | 69,292 | ×1.540 | 500 | ×1.169 |
| starcraft_protoss | `protoss_analogue` | 60,000 | 60,000 | 92,389 | ×1.540 | 1,200 | ×1.165 |
| starcraft_protoss | `protoss_darktemplar` | 50,000 | 50,000 | 76,991 | ×1.540 | 600 | ×1.143 |
| starcraft_protoss | `protoss_legionnaire` | 60,000 | 60,000 | 92,389 | ×1.540 | 700 | ×1.127 |
| starcraft_protoss | `protoss_adept` | 30,000 | 30,000 | 46,194 | ×1.540 | 650 | ×1.124 |
| starcraft_protoss | `protoss_zealot` | 40,000 | 40,000 | 61,592 | ×1.540 | 300 | ×1.123 |
| starcraft_protoss | `protoss_manifold` | 25,000 | 25,000 | 38,495 | ×1.540 | 600 | ×1.118 |
| starcraft_protoss | `protoss_photoncannon` | 200,000 | 200,000 | 307,962 | ×1.540 | 2,000 | ×1.063 |
