# Artillery infantry rebalance proposal

Anchor spec: HP=60000, Speed=75, Range=15000, eff-DPS=500, Cost=500

Converter law: cost pinned, range clamped to band + made unique, eff-DPS trimmed to Δ≤1 via 100-grid warhead Damage; unconditional FirepowerMultiplier is retired.

Uniqueness separates **raw warhead Damage (the 5-stat law as written)**. The levers run coarsest-first — Damage (100 grid) → Speed (1) → Range (10) — because one Damage step is a whole shot and running it last threw away everything the fine levers had achieved.

| actor | faction | HP | spd | rng | cost | dmg/wh×n | rl | burst | legacy FP% | eff DPS | price | Δ | flags |
|---|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|---|
| `ixian_ixsiegetank` | d2k_ixian | 65000 | 65 | 12050 | 2050 | 78500×2 | 120 | 3 | 100 | 3270.8 | 2049 | -0.8 |  |
| `ordos_deviatorartillery` | d2k_ordos | 45000 | 85 | 12030 | 1250 | 92000×2 | 150 | 3 | 100 | 2760.0 | 1250 | -0.4 |  |
| `ra2_soviets_v3rocketlauncher` | redalert2_soviets | 27500 | 70 | 15000 | 900 | 100×1 | 0 | 1 | 125 | 0.0 | 135 | -764.7 | fp-debt |
| `asianalliance_viper` | redalert2mod_asianalliance | 77500 | 90 | 12080 | 700 | 71200×1 | 60 | 1 | 100 | 1186.7 | 700 | +0.2 |  |
| `steelconsortium_dagger` | redalert2mod_consortium | 25000 | 70 | 18000 | 1300 | 45400×1 | 50 | 4 | 100 | 3632.0 | 1300 | -0.5 |  |
| `steelconsortium_hammerheadartillerytank` | redalert2mod_consortium | 42500 | 75 | 15010 | 1000 | 200×1 | 0 | 1 | 100 | 0.0 | 116 | -884.2 |  |
| `futuretech_athenacannon` | redalert2mod_futuretech | 70000 | 60 | 15040 | 2200 | 300×1 | 0 | 1 | 100 | 0.0 | 202 | -1998.5 |  |
| `naxis_brummbar` | redalert2mod_naxis | 90000 | 85 | 15020 | 1100 | 400×1 | 0 | 1 | 100 | 0.0 | 201 | -899.3 |  |
| `naxis_donnerschlag` | redalert2mod_naxis | 80000 | 90 | 14990 | 2300 | 500×1 | 0 | 1 | 125 | 0.0 | 281 | -2019.5 | fp-debt |
| `naxis_grille` | redalert2mod_naxis | 47500 | 90 | 14980 | 800 | 600×1 | 0 | 1 | 100 | 0.0 | 204 | -596.2 |  |
| `latinsyndicate_burrito` | redalert2mod_syndicate | 52500 | 80 | 12040 | 1800 | 88700×1 | 122 | 8 | 125 | 4730.7 | 1800 | +0.4 | fp-debt |
| `tkm_dronepodtruck` | redalert2mod_tkm | 50000 | 65 | 17720 | 1600 | 27000×1 | 5 | 1 | 100 | 5400.0 | 1600 | +0.1 |  |
| `tkm_tornadoglauncher` | redalert2mod_tkm | 35000 | 90 | 12440 | 1200 | 125600×1 | 157 | 8 | 100 | 5431.4 | 1200 | -0.1 |  |
| `ra1_allies_alliedartillery` | redalert_allies | 20000 | 60 | 15000 | 600 | 30000×1 | 80 | 1 | 100 | 375.0 | 238 | -361.8 | anchor |
| `japan_ballista` | redalert_japan | 55000 | 75 | 13520 | 1150 | 85500×2 | 176 | 5 | 75 | 4275.0 | 1150 | -0.0 | fp-debt |
| `ra1_soviets_nuclearv2launcher` | redalert_soviets | 37500 | 85 | 12550 | 2300 | 1692600×1 | 158 | 1 | 100 | 10712.7 | 2300 | -0.1 |  |
| `ra1_soviets_v1rockettruck` | redalert_soviets | 22500 | 80 | 12020 | 850 | 36300×2 | 115 | 4 | 100 | 2233.8 | 850 | -0.0 |  |
| `ra1_soviets_v2rocketlauncher` | redalert_soviets | 30000 | 80 | 14110 | 1600 | 292300×2 | 120 | 1 | 100 | 4871.7 | 1600 | +0.0 |  |
| `td_nod_artillery` | tiberiandawn_nod | 17500 | 60 | 12350 | 400 | 59200×2 | 112 | 1 | 100 | 1057.1 | 400 | +0.1 | shared-wpn? |
| `td_nod_chemicalssmlauncher` | tiberiandawn_nod | 32500 | 75 | 12330 | 1200 | 560800×1 | 142 | 1 | 100 | 3949.3 | 1200 | -0.1 |  |
| `td_nod_specterartillery` | tiberiandawn_nod | 15000 | 90 | 12640 | 900 | 140100×2 | 78 | 1 | 100 | 3592.3 | 900 | -0.2 |  |
| `cabal_artilleryspider` | tiberiansun_cabal | 72500 | 70 | 12070 | 1250 | 104300×1 | 64 | 1 | 100 | 1629.7 | 1250 | +0.3 |  |
| `ts_gdi_juggernautmkii` | tiberiansun_gdi | 40000 | 80 | 12860 | 2200 | 175600×1 | 64 | 3 | 100 | 7525.7 | 2200 | -0.5 |  |
| `wc2_humans_ballista` | warcraft2_humans | 57500 | 80 | 14450 | 900 | 74700×3 | 180 | 1 | 100 | 1245.0 | 900 | +0.2 |  |
| `wc2_humans_siegeengine` | warcraft2_humans | 60000 | 70 | 12010 | 1800 | 354600×1 | 100 | 1 | 100 | 3546.0 | 1800 | -0.0 | shared-wpn? |
| `wc2_orcs_catapult` | warcraft2_orcs | 62500 | 65 | 13400 | 800 | 145600×2 | 240 | 1 | 100 | 1213.3 | 800 | -0.1 |  |
| `wc2_orcs_siegeengine` | warcraft2_orcs | 67500 | 70 | 12000 | 1800 | 320800×1 | 100 | 1 | 100 | 3208.0 | 1800 | -0.1 | shared-wpn? |

**Worst |Δ| among non-anchor members: 2019.5** (goal ≤1).

## Uniqueness check (5 raw stats — soft/protected excluded)

- **Speed duplicates**: {65: ['ixian_ixsiegetank', 'tkm_dronepodtruck', 'wc2_orcs_catapult'], 85: ['ordos_deviatorartillery', 'naxis_brummbar', 'ra1_soviets_nuclearv2launcher'], 70: ['ra2_soviets_v3rocketlauncher', 'steelconsortium_dagger', 'cabal_artilleryspider', 'wc2_humans_siegeengine', 'wc2_orcs_siegeengine'], 90: ['asianalliance_viper', 'naxis_donnerschlag', 'naxis_grille', 'tkm_tornadoglauncher', 'td_nod_specterartillery'], 75: ['steelconsortium_hammerheadartillerytank', 'japan_ballista', 'td_nod_chemicalssmlauncher'], 60: ['futuretech_athenacannon', 'td_nod_artillery'], 80: ['latinsyndicate_burrito', 'ra1_soviets_v1rockettruck', 'ra1_soviets_v2rocketlauncher', 'ts_gdi_juggernautmkii', 'wc2_humans_ballista']}
- **raw ReloadDelay duplicates** (design choice — retune coarse Damage/reload by hand): {120: ['ixian_ixsiegetank', 'ra1_soviets_v2rocketlauncher'], 0: ['ra2_soviets_v3rocketlauncher', 'steelconsortium_hammerheadartillerytank', 'futuretech_athenacannon', 'naxis_brummbar', 'naxis_donnerschlag', 'naxis_grille'], 64: ['cabal_artilleryspider', 'ts_gdi_juggernautmkii'], 100: ['wc2_humans_siegeengine', 'wc2_orcs_siegeengine']}

## Required YAML edits (per unit)

- `ixian_ixsiegetank`: HP 65000, Speed 65, Range 12050, each offensive warhead Damage 78500 (×2 = SUM 157000), ReloadDelay 120, Burst 3
- `ordos_deviatorartillery`: HP 45000, Speed 85, Range 12030, each offensive warhead Damage 92000 (×2 = SUM 184000), ReloadDelay 150, Burst 3
- `ra2_soviets_v3rocketlauncher`: HP 27500, Speed 70, Range 15000, each offensive warhead Damage 100 (×1 = SUM 100), ReloadDelay 0, Burst 1, **DELETE the unconditional FirepowerMultiplier (125%)** — the Damage above already includes it (W17), residual Δ -764.7 (cost pinned at 900)
- `asianalliance_viper`: HP 77500, Speed 90, Range 12080, each offensive warhead Damage 71200 (×1 = SUM 71200), ReloadDelay 60, Burst 1
- `steelconsortium_dagger`: HP 25000, Speed 70, Range 18000, each offensive warhead Damage 45400 (×1 = SUM 45400), ReloadDelay 50, Burst 4
- `steelconsortium_hammerheadartillerytank`: HP 42500, Speed 75, Range 15010, each offensive warhead Damage 200 (×1 = SUM 200), ReloadDelay 0, Burst 1, residual Δ -884.2 (cost pinned at 1000)
- `futuretech_athenacannon`: HP 70000, Speed 60, Range 15040, each offensive warhead Damage 300 (×1 = SUM 300), ReloadDelay 0, Burst 1, residual Δ -1998.5 (cost pinned at 2200)
- `naxis_brummbar`: HP 90000, Speed 85, Range 15020, each offensive warhead Damage 400 (×1 = SUM 400), ReloadDelay 0, Burst 1, residual Δ -899.3 (cost pinned at 1100)
- `naxis_donnerschlag`: HP 80000, Speed 90, Range 14990, each offensive warhead Damage 500 (×1 = SUM 500), ReloadDelay 0, Burst 1, **DELETE the unconditional FirepowerMultiplier (125%)** — the Damage above already includes it (W17), residual Δ -2019.5 (cost pinned at 2300)
- `naxis_grille`: HP 47500, Speed 90, Range 14980, each offensive warhead Damage 600 (×1 = SUM 600), ReloadDelay 0, Burst 1, residual Δ -596.2 (cost pinned at 800)
- `latinsyndicate_burrito`: HP 52500, Speed 80, Range 12040, each offensive warhead Damage 88700 (×1 = SUM 88700), ReloadDelay 122, Burst 8, **DELETE the unconditional FirepowerMultiplier (125%)** — the Damage above already includes it (W17)
- `tkm_dronepodtruck`: HP 50000, Speed 65, Range 17720, each offensive warhead Damage 27000 (×1 = SUM 27000), ReloadDelay 5, Burst 1
- `tkm_tornadoglauncher`: HP 35000, Speed 90, Range 12440, each offensive warhead Damage 125600 (×1 = SUM 125600), ReloadDelay 157, Burst 8
- `japan_ballista`: HP 55000, Speed 75, Range 13520, each offensive warhead Damage 85500 (×2 = SUM 171000), ReloadDelay 176, Burst 5, **DELETE the unconditional FirepowerMultiplier (75%)** — the Damage above already includes it (W17)
- `ra1_soviets_nuclearv2launcher`: HP 37500, Speed 85, Range 12550, each offensive warhead Damage 1692600 (×1 = SUM 1692600), ReloadDelay 158, Burst 1
- `ra1_soviets_v1rockettruck`: HP 22500, Speed 80, Range 12020, each offensive warhead Damage 36300 (×2 = SUM 72600), ReloadDelay 115, Burst 4
- `ra1_soviets_v2rocketlauncher`: HP 30000, Speed 80, Range 14110, each offensive warhead Damage 292300 (×2 = SUM 584600), ReloadDelay 120, Burst 1
- `td_nod_artillery`: HP 17500, Speed 60, Range 12350, each offensive warhead Damage 59200 (×2 = SUM 118400), ReloadDelay 112, Burst 1
- `td_nod_chemicalssmlauncher`: HP 32500, Speed 75, Range 12330, each offensive warhead Damage 560800 (×1 = SUM 560800), ReloadDelay 142, Burst 1
- `td_nod_specterartillery`: HP 15000, Speed 90, Range 12640, each offensive warhead Damage 140100 (×2 = SUM 280200), ReloadDelay 78, Burst 1
- `cabal_artilleryspider`: HP 72500, Speed 70, Range 12070, each offensive warhead Damage 104300 (×1 = SUM 104300), ReloadDelay 64, Burst 1
- `ts_gdi_juggernautmkii`: HP 40000, Speed 80, Range 12860, each offensive warhead Damage 175600 (×1 = SUM 175600), ReloadDelay 64, Burst 3
- `wc2_humans_ballista`: HP 57500, Speed 80, Range 14450, each offensive warhead Damage 74700 (×3 = SUM 224100), ReloadDelay 180, Burst 1
- `wc2_humans_siegeengine`: HP 60000, Speed 70, Range 12010, each offensive warhead Damage 354600 (×1 = SUM 354600), ReloadDelay 100, Burst 1
- `wc2_orcs_catapult`: HP 62500, Speed 65, Range 13400, each offensive warhead Damage 145600 (×2 = SUM 291200), ReloadDelay 240, Burst 1
- `wc2_orcs_siegeengine`: HP 67500, Speed 70, Range 12000, each offensive warhead Damage 320800 (×1 = SUM 320800), ReloadDelay 100, Burst 1
