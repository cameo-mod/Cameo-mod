# audit_survivability_pricing — E1: the baseline shield is priced at ZERO

| bucket | actors | priced today | belongs to |
|---|--:|---|---|
| spawns with a pool (**always-on**) | 56 | ✖ nothing | **E1 — this report** |
| empty capacity, needs `shieldgen` | 1335 | ✔ correctly nothing | — (it has no shield) |
| pool behind an upgrade | 215 | ✖ nothing | E5 (upgrade pricing) |

Shield row mean Versus **185.63**, so one shield point is **0.5387 HP** BEFORE any shield-gated `DamageMultiplier` — measured off the live ladder every run, never frozen. The Shield row takes **1.601%** of all roster raw damage at baseline.

⚠ **Every one of these 56 actors also carries `DamageMultiplier@shielded: 150`**, so it takes 150% damage WHILE the shield holds — the deliberate counterweight to having one. That divides the pool's worth: a shield point is really **0.3591 HP**, and the roster-wide gap is 38.6% rather than the 57.8% a shield-only reading gives. `shield_damage_multiplier` and `shield_hp_per_point` are published per actor.

## The gap

* Raw HP across these 56 actors: **12,812,500**
* Effective HP once the pool is counted: **20,208,971** (**+57.7%**)
* Implied price change if the formula read effective HP: median **×1.375**, max **×1.749**

⚠ **Retiring the 150% multiplier is a BUFF that must be paid for.** The numbers above already account for it, so they price the game AS IT IS. Delete `DamageMultiplier@shielded` and a shield point jumps from 0.359 to 0.539 HP — the same pool becomes 1.5x more valuable and the implied price rises again. Re-extract AFTER the deletion and price once, or these units get charged for durability they no longer have (or keep durability they were never charged for).

## Per actor

| pack | actor | HP | shield pool | effective HP | ×HP | cost | implied ×price |
|---|---|--:|--:|--:|--:|--:|--:|
| redalert2mod_consortium | `steelconsortium_defenderbot` | 210,000 | 420,000 | 436,258 | ×2.077 | 3,200 | ×1.749 |
| redalert2mod_consortium | `steelconsortium_skyhammer` | 120,000 | 240,000 | 249,290 | ×2.077 | 4,500 | ×1.741 |
| redalert2mod_consortium | `steelconsortium_katytank` | 275,000 | 550,000 | 571,290 | ×2.077 | 3,800 | ×1.707 |
| redalert2mod_consortium | `steelconsortium_stalker` | 140,000 | 280,000 | 290,838 | ×2.077 | 4,000 | ×1.690 |
| redalert2mod_consortium | `steelconsortium_whiterabbit` | 150,000 | 300,000 | 311,613 | ×2.077 | 4,500 | ×1.567 |
| starcraft_protoss | `protoss_arbitertribunal` | 250,000 | 250,000 | 384,677 | ×1.539 | 2,500 | ×1.539 |
| starcraft_protoss | `protoss_observatory` | 250,000 | 250,000 | 384,677 | ×1.539 | 2,500 | ×1.539 |
| starcraft_protoss | `protoss_pylon` | 250,000 | 250,000 | 384,677 | ×1.539 | 1,000 | ×1.539 |
| starcraft_protoss | `protoss_templararchives` | 250,000 | 250,000 | 384,677 | ×1.539 | 2,500 | ×1.539 |
| starcraft_protoss | `protoss_roboticsfacility` | 400,000 | 400,000 | 615,484 | ×1.539 | 2,000 | ×1.539 |
| starcraft_protoss | `protoss_fleetbeacon` | 1,000,000 | 1,000,000 | 1,538,709 | ×1.539 | 10,000 | ×1.539 |
| starcraft_protoss | `protoss_nexus` | 1,000,000 | 1,000,000 | 1,538,709 | ×1.539 | 5,000 | ×1.539 |
| starcraft_protoss | `protoss_assimilator` | 300,000 | 300,000 | 461,613 | ×1.539 | 3,000 | ×1.539 |
| starcraft_protoss | `protoss_cyberneticscore` | 150,000 | 150,000 | 230,806 | ×1.539 | 1,500 | ×1.539 |
| starcraft_protoss | `protoss_forge` | 150,000 | 150,000 | 230,806 | ×1.539 | 1,500 | ×1.539 |
| starcraft_protoss | `protoss_stargate` | 300,000 | 300,000 | 461,613 | ×1.539 | 1,500 | ×1.539 |
| starcraft_protoss | `protoss_citadelofadun` | 200,000 | 200,000 | 307,742 | ×1.539 | 2,000 | ×1.539 |
| starcraft_protoss | `protoss_gateway` | 200,000 | 200,000 | 307,742 | ×1.539 | 1,000 | ×1.539 |
| starcraft_protoss | `protoss_roboticssupportbay` | 200,000 | 200,000 | 307,742 | ×1.539 | 2,000 | ×1.539 |
| starcraft_protoss | `protoss_mobilenexus` | 300,000 | 300,000 | 461,613 | ×1.539 | 5,000 | ×1.490 |
| redalert2mod_consortium | `steel_cruiser_f` | 37,500 | 75,000 | 77,903 | ×2.077 | 125 | ×1.451 |
| starcraft_protoss | `protoss_starshipsovereign` | 750,000 | 750,000 | 1,154,032 | ×1.539 | 10,000 | ×1.445 |
| redalert2mod_consortium | `steelconsortium_supportshieldgenerator` | 100,000 | 200,000 | 207,742 | ×2.077 | 3,000 | ×1.431 |
| redalert2mod_consortium | `steelconsortium_empressstation` | 1,500,000 | 1,500,000 | 2,308,063 | ×1.539 | 5,000 | ×1.426 |
| starcraft_protoss | `protoss_archon` | 350,000 | 350,000 | 538,548 | ×1.539 | 5,600 | ×1.406 |
| starcraft_protoss | `protoss_epigraph` | 200,000 | 200,000 | 307,742 | ×1.539 | 2,600 | ×1.393 |
| starcraft_protoss | `protoss_shuttle` | 100,000 | 100,000 | 153,871 | ×1.539 | 6,000 | ×1.392 |
| starcraft_protoss | `protoss_carrier` | 300,000 | 300,000 | 461,613 | ×1.539 | 3,000 | ×1.377 |
| starcraft_protoss | `protoss_idol` | 350,000 | 350,000 | 538,548 | ×1.539 | 2,800 | ×1.374 |
| starcraft_protoss | `protoss_arbiter` | 225,000 | 225,000 | 346,210 | ×1.539 | 4,800 | ×1.369 |
| starcraft_protoss | `protoss_atreus` | 250,000 | 250,000 | 384,677 | ×1.539 | 2,400 | ×1.363 |
| starcraft_protoss | `protoss_zeratul` | 250,000 | 250,000 | 384,677 | ×1.539 | 4,000 | ×1.362 |
| starcraft_protoss | `protoss_corsair` | 125,000 | 125,000 | 192,339 | ×1.539 | 2,100 | ×1.357 |
| redalert2mod_consortium | `steelconsortium_cloudbreaker` | 250,000 | 250,000 | 384,677 | ×1.539 | 5,000 | ×1.354 |
| starcraft_protoss | `protoss_reaver` | 275,000 | 275,000 | 423,145 | ×1.539 | 2,700 | ×1.342 |
| redalert2mod_consortium | `steel_cougar` | 25,000 | 50,000 | 51,935 | ×2.077 | 1,200 | ×1.314 |
| starcraft_protoss | `protoss_voidray` | 70,000 | 70,000 | 107,710 | ×1.539 | 1,400 | ×1.314 |
| starcraft_protoss | `protoss_scout` | 75,000 | 75,000 | 115,403 | ×1.539 | 1,500 | ×1.313 |
| redalert2mod_consortium | `steel_hummer` | 45,000 | 67,500 | 81,363 | ×1.808 | 2,000 | ×1.312 |
| redalert2mod_consortium | `steel_oldqtnk` | 112,500 | 112,500 | 173,105 | ×1.539 | 2,400 | ×1.307 |
| starcraft_protoss | `protoss_shieldbattery` | 100,000 | 100,000 | 153,871 | ×1.539 | 1,000 | ×1.269 |
| starcraft_protoss | `protoss_positron` | 60,000 | 60,000 | 92,322 | ×1.539 | 1,200 | ×1.261 |
| starcraft_protoss | `protoss_dragoon` | 75,000 | 75,000 | 115,403 | ×1.539 | 1,200 | ×1.242 |
| starcraft_protoss | `protoss_gladius` | 100,000 | 100,000 | 153,871 | ×1.539 | 1,800 | ×1.239 |
| starcraft_protoss | `protoss_patriarch` | 75,000 | 75,000 | 115,403 | ×1.539 | 4,000 | ×1.236 |
| redalert2mod_consortium | `steel_cobra` | 325,000 | 162,500 | 412,540 | ×1.269 | 3,600 | ×1.199 |
| starcraft_protoss | `protoss_probe` | 45,000 | 45,000 | 69,242 | ×1.539 | 500 | ×1.188 |
| starcraft_protoss | `protoss_analogue` | 60,000 | 60,000 | 92,322 | ×1.539 | 1,200 | ×1.187 |
| starcraft_protoss | `protoss_amaranth` | 70,000 | 70,000 | 107,710 | ×1.539 | 1,200 | ×1.179 |
| starcraft_protoss | `protoss_observer` | 12,500 | 12,500 | 19,234 | ×1.539 | 500 | ×1.169 |
| starcraft_protoss | `protoss_darktemplar` | 50,000 | 50,000 | 76,935 | ×1.539 | 600 | ×1.160 |
| starcraft_protoss | `protoss_zealot` | 40,000 | 40,000 | 61,548 | ×1.539 | 300 | ×1.145 |
| starcraft_protoss | `protoss_legionnaire` | 60,000 | 60,000 | 92,322 | ×1.539 | 700 | ×1.141 |
| starcraft_protoss | `protoss_adept` | 30,000 | 30,000 | 46,161 | ×1.539 | 650 | ×1.129 |
| starcraft_protoss | `protoss_manifold` | 25,000 | 25,000 | 38,468 | ×1.539 | 600 | ×1.118 |
| starcraft_protoss | `protoss_photoncannon` | 200,000 | 200,000 | 307,742 | ×1.539 | 2,000 | ×1.014 |
