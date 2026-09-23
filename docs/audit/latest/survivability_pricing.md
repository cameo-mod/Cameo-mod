# audit_survivability_pricing — E1: the baseline shield is priced at ZERO

| bucket | actors | priced today | belongs to |
|---|--:|---|---|
| spawns with a pool (**always-on**) | 56 | ✖ nothing | **E1 — this report** |
| empty capacity, needs `shieldgen` | 1266 | ✔ correctly nothing | — (it has no shield) |
| pool behind an upgrade | 233 | ✖ nothing | E5 (upgrade pricing) |

Shield row mean Versus **160.48**, so one shield point is **0.6231 HP** BEFORE any shield-gated `DamageMultiplier` — measured off the live ladder every run, never frozen. The Shield row takes **1.493%** of all roster raw damage at baseline.

⚠ **Every one of these 56 actors also carries `DamageMultiplier@shielded: 150`**, so it takes 150% damage WHILE the shield holds — the deliberate counterweight to having one. That divides the pool's worth: a shield point is really **0.4154 HP**, and the roster-wide gap is 38.6% rather than the 57.8% a shield-only reading gives. `shield_damage_multiplier` and `shield_hp_per_point` are published per actor.

## The gap

* Raw HP across these 56 actors: **12,812,500**
* Effective HP once the pool is counted: **21,367,888** (**+66.8%**)
* Implied price change if the formula read effective HP: median **×1.434**, max **×1.866**

⚠ **Retiring the 150% multiplier is a BUFF that must be paid for.** The numbers above already account for it, so they price the game AS IT IS. Delete `DamageMultiplier@shielded` and a shield point jumps from 0.415 to 0.623 HP — the same pool becomes 1.5x more valuable and the implied price rises again. Re-extract AFTER the deletion and price once, or these units get charged for durability they no longer have (or keep durability they were never charged for).

## Per actor

| pack | actor | HP | shield pool | effective HP | ×HP | cost | implied ×price |
|---|---|--:|--:|--:|--:|--:|--:|
| redalert2mod_consortium | `steelconsortium_defenderbot` | 210,000 | 420,000 | 471,709 | ×2.246 | 3,200 | ×1.866 |
| redalert2mod_consortium | `steelconsortium_skyhammer` | 120,000 | 240,000 | 269,548 | ×2.246 | 4,500 | ×1.857 |
| redalert2mod_consortium | `steelconsortium_katytank` | 275,000 | 550,000 | 617,714 | ×2.246 | 3,800 | ×1.818 |
| redalert2mod_consortium | `steelconsortium_stalker` | 140,000 | 280,000 | 314,473 | ×2.246 | 4,000 | ×1.798 |
| redalert2mod_consortium | `steelconsortium_whiterabbit` | 150,000 | 300,000 | 336,935 | ×2.246 | 4,500 | ×1.657 |
| starcraft_protoss | `protoss_cyberneticscore` | 150,000 | 150,000 | 243,468 | ×1.623 | 1,500 | ×1.623 |
| starcraft_protoss | `protoss_forge` | 150,000 | 150,000 | 243,468 | ×1.623 | 1,500 | ×1.623 |
| starcraft_protoss | `protoss_citadelofadun` | 200,000 | 200,000 | 324,623 | ×1.623 | 2,000 | ×1.623 |
| starcraft_protoss | `protoss_gateway` | 200,000 | 200,000 | 324,623 | ×1.623 | 1,000 | ×1.623 |
| starcraft_protoss | `protoss_roboticsfacility` | 400,000 | 400,000 | 649,247 | ×1.623 | 2,000 | ×1.623 |
| starcraft_protoss | `protoss_roboticssupportbay` | 200,000 | 200,000 | 324,623 | ×1.623 | 2,000 | ×1.623 |
| starcraft_protoss | `protoss_arbitertribunal` | 250,000 | 250,000 | 405,779 | ×1.623 | 2,500 | ×1.623 |
| starcraft_protoss | `protoss_fleetbeacon` | 1,000,000 | 1,000,000 | 1,623,116 | ×1.623 | 10,000 | ×1.623 |
| starcraft_protoss | `protoss_nexus` | 1,000,000 | 1,000,000 | 1,623,116 | ×1.623 | 5,000 | ×1.623 |
| starcraft_protoss | `protoss_observatory` | 250,000 | 250,000 | 405,779 | ×1.623 | 2,500 | ×1.623 |
| starcraft_protoss | `protoss_pylon` | 250,000 | 250,000 | 405,779 | ×1.623 | 1,000 | ×1.623 |
| starcraft_protoss | `protoss_templararchives` | 250,000 | 250,000 | 405,779 | ×1.623 | 2,500 | ×1.623 |
| starcraft_protoss | `protoss_assimilator` | 300,000 | 300,000 | 486,935 | ×1.623 | 3,000 | ×1.623 |
| starcraft_protoss | `protoss_stargate` | 300,000 | 300,000 | 486,935 | ×1.623 | 1,500 | ×1.623 |
| starcraft_protoss | `protoss_mobilenexus` | 300,000 | 300,000 | 486,935 | ×1.623 | 5,000 | ×1.566 |
| redalert2mod_consortium | `cruiser_f.steel` | 37,500 | 75,000 | 84,234 | ×2.246 | 125 | ×1.528 |
| starcraft_protoss | `protoss_starshipsovereign` | 750,000 | 750,000 | 1,217,337 | ×1.623 | 10,000 | ×1.514 |
| redalert2mod_consortium | `steelconsortium_supportshieldgenerator` | 100,000 | 200,000 | 224,623 | ×2.246 | 3,000 | ×1.498 |
| redalert2mod_consortium | `steelconsortium_empressstation` | 1,500,000 | 1,500,000 | 2,434,675 | ×1.623 | 5,000 | ×1.493 |
| starcraft_protoss | `protoss_archon` | 350,000 | 350,000 | 568,091 | ×1.623 | 5,600 | ×1.469 |
| starcraft_protoss | `protoss_epigraph` | 200,000 | 200,000 | 324,623 | ×1.623 | 2,600 | ×1.455 |
| starcraft_protoss | `protoss_shuttle` | 100,000 | 100,000 | 162,312 | ×1.623 | 6,000 | ×1.453 |
| starcraft_protoss | `protoss_carrier` | 300,000 | 300,000 | 486,935 | ×1.623 | 3,000 | ×1.436 |
| starcraft_protoss | `protoss_idol` | 350,000 | 350,000 | 568,091 | ×1.623 | 2,800 | ×1.432 |
| starcraft_protoss | `protoss_arbiter` | 225,000 | 225,000 | 365,201 | ×1.623 | 4,800 | ×1.427 |
| starcraft_protoss | `protoss_atreus` | 250,000 | 250,000 | 405,779 | ×1.623 | 2,400 | ×1.421 |
| starcraft_protoss | `protoss_zeratul` | 250,000 | 250,000 | 405,779 | ×1.623 | 4,000 | ×1.420 |
| starcraft_protoss | `protoss_corsair` | 125,000 | 125,000 | 202,890 | ×1.623 | 2,100 | ×1.414 |
| redalert2mod_consortium | `steelconsortium_cloudbreaker` | 250,000 | 250,000 | 405,779 | ×1.623 | 5,000 | ×1.410 |
| starcraft_protoss | `protoss_reaver` | 275,000 | 275,000 | 446,357 | ×1.623 | 2,700 | ×1.397 |
| redalert2mod_consortium | `cougar.steel` | 25,000 | 50,000 | 56,156 | ×2.246 | 1,200 | ×1.363 |
| starcraft_protoss | `protoss_voidray` | 70,000 | 70,000 | 113,618 | ×1.623 | 1,400 | ×1.363 |
| starcraft_protoss | `protoss_scout` | 75,000 | 75,000 | 121,734 | ×1.623 | 1,500 | ×1.362 |
| redalert2mod_consortium | `hummer.steel` | 45,000 | 67,500 | 87,060 | ×1.935 | 2,000 | ×1.360 |
| redalert2mod_consortium | `oldqtnk.steel` | 112,500 | 112,500 | 182,601 | ×1.623 | 2,400 | ×1.355 |
| starcraft_protoss | `protoss_shieldbattery` | 100,000 | 100,000 | 162,312 | ×1.623 | 1,000 | ×1.312 |
| starcraft_protoss | `protoss_positron` | 60,000 | 60,000 | 97,387 | ×1.623 | 1,200 | ×1.302 |
| starcraft_protoss | `protoss_dragoon` | 75,000 | 75,000 | 121,734 | ×1.623 | 1,200 | ×1.280 |
| starcraft_protoss | `protoss_gladius` | 100,000 | 100,000 | 162,312 | ×1.623 | 1,800 | ×1.277 |
| starcraft_protoss | `protoss_patriarch` | 75,000 | 75,000 | 121,734 | ×1.623 | 4,000 | ×1.274 |
| redalert2mod_consortium | `cobra.steel` | 325,000 | 162,500 | 426,256 | ×1.312 | 3,600 | ×1.230 |
| starcraft_protoss | `protoss_probe` | 45,000 | 45,000 | 73,040 | ×1.623 | 500 | ×1.218 |
| starcraft_protoss | `protoss_analogue` | 60,000 | 60,000 | 97,387 | ×1.623 | 1,200 | ×1.218 |
| starcraft_protoss | `protoss_amaranth` | 70,000 | 70,000 | 113,618 | ×1.623 | 1,200 | ×1.208 |
| starcraft_protoss | `protoss_observer` | 12,500 | 12,500 | 20,289 | ×1.623 | 500 | ×1.196 |
| starcraft_protoss | `protoss_darktemplar` | 50,000 | 50,000 | 81,156 | ×1.623 | 600 | ×1.186 |
| starcraft_protoss | `protoss_zealot` | 40,000 | 40,000 | 64,925 | ×1.623 | 300 | ×1.169 |
| starcraft_protoss | `protoss_legionnaire` | 60,000 | 60,000 | 97,387 | ×1.623 | 700 | ×1.164 |
| starcraft_protoss | `protoss_adept` | 30,000 | 30,000 | 48,694 | ×1.623 | 650 | ×1.150 |
| starcraft_protoss | `protoss_manifold` | 25,000 | 25,000 | 40,578 | ×1.623 | 600 | ×1.137 |
| starcraft_protoss | `protoss_photoncannon` | 200,000 | 200,000 | 324,623 | ×1.623 | 2,000 | ×1.016 |
