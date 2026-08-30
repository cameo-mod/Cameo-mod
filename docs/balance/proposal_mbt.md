# Mbt infantry rebalance proposal

Anchor spec: HP=240000, Speed=95, Range=5500, eff-DPS=600, Cost=800

Converter law: cost pinned, range clamped to band + made unique, eff-DPS trimmed to Δ≤1 via 100-grid warhead Damage; unconditional FirepowerMultiplier is retired.

Uniqueness separates **raw warhead Damage (the 5-stat law as written)**. The levers run coarsest-first — Damage (100 grid) → Speed (1) → Range (10) — because one Damage step is a whole shot and running it last threw away everything the fine levers had achieved.

| actor | faction | HP | spd | rng | cost | dmg/wh×n | rl | burst | legacy FP% | eff DPS | price | Δ | flags |
|---|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|---|
| `combat_tank.atreides` | d2k_atreides | 95000 | 85 | 5140 | 600 | 25600×1 | 25 | 1 | 100 | 1024.0 | 600 | -0.1 | shared-wpn? |
| `combat_tank.harkonnen` | d2k_harkonnen | 90000 | 95 | 5110 | 600 | 54600×1 | 55 | 1 | 100 | 992.7 | 600 | -0.1 |  |
| `ixian_heavykodatank` | d2k_ixian | 85000 | 110 | 6020 | 1100 | 24200×3 | 40 | 1 | 100 | 1815.0 | 1100 | +0.3 |  |
| `ixian_kodatank` | d2k_ixian | 80000 | 114 | 5540 | 800 | 15300×3 | 35 | 1 | 100 | 1311.4 | 800 | -0.1 |  |
| `ixian_mongoose` | d2k_ixian | 80000 | 114 | 5030 | 1300 | 100×1 | 0 | 1 | 100 | 0.0 | 217 | -1083.5 |  |
| `ordos_combatautoguntank` | d2k_ordos | 97500 | 100 | 6270 | 1500 | 8200×3 | 24 | 4 | 25 | 3280.0 | 1500 | -0.1 | fp-debt |
| `ordos_heavycombattank` | d2k_ordos | 65000 | 105 | 5990 | 950 | 59900×1 | 40 | 1 | 100 | 1497.5 | 950 | +0.1 |  |
| `ra2_allies_grizzlytank` | redalert2_allies | 90000 | 80 | 5290 | 750 | 55100×1 | 45 | 1 | 160 | 1224.4 | 750 | +0.4 | fp-debt |
| `ra2_soviets_rhinoheavytank` | redalert2_soviets | 90000 | 80 | 5370 | 850 | 76800×1 | 54 | 1 | 100 | 1422.2 | 850 | -0.2 |  |
| `asianalliance_lynxtank` | redalert2mod_asianalliance | 100000 | 90 | 5360 | 850 | 70800×1 | 55 | 1 | 100 | 1287.3 | 850 | +0.4 |  |
| `ptnk.asian` | redalert2mod_asianalliance | 60000 | 95 | 6470 | 2400 | 17700×8 | 25 | 2 | 100 | 9440.0 | 2400 | +0.1 |  |
| `steelconsortium_mako` | redalert2mod_consortium | 100000 | 76 | 6040 | 900 | 10800×4 | 25 | 1 | 100 | 1728.0 | 900 | -0.4 |  |
| `steelconsortium_quantumtank` | redalert2mod_consortium | 55000 | 114 | 6000 | 1600 | 112600×2 | 65 | 1 | 100 | 3464.6 | 1601 | +0.6 |  |
| `futuretech_guardiantank` | redalert2mod_futuretech | 85000 | 105 | 6490 | 850 | 111000×1 | 85 | 1 | 100 | 1305.9 | 850 | -0.1 |  |
| `naxis_kingtigerheavytank` | redalert2mod_naxis | 70000 | 110 | 5020 | 2000 | 199300×1 | 50 | 1 | 100 | 3986.0 | 2000 | -0.0 |  |
| `tiger.nax` | redalert2mod_naxis | 100000 | 100 | 5500 | 800 | 10000×1 | 50 | 1 | 100 | 200.0 | 329 | -471.2 | anchor |
| `schwarzermond_lunartiger` | redalert2mod_schwarzermond | 55000 | 76 | 5100 | 950 | 251100×1 | 80 | 1 | 100 | 3138.8 | 950 | -0.1 |  |
| `latinsyndicate_smokertank` | redalert2mod_syndicate | 70000 | 105 | 6480 | 1800 | 190200×1 | 65 | 1 | 50 | 2926.2 | 1800 | -0.1 | fp-debt |
| `tkm_abrams` | redalert2mod_tkm | 97500 | 105 | 4980 | 1000 | 200×1 | 0 | 1 | 100 | 0.0 | 157 | -843.5 |  |
| `tkm_t72m` | redalert2mod_tkm | 95000 | 100 | 6210 | 900 | 76700×1 | 42 | 1 | 100 | 1826.2 | 900 | -0.3 |  |
| `tkm_technicaltank` | redalert2mod_tkm | 47500 | 105 | 5000 | 700 | 300×1 | 0 | 1 | 100 | 0.0 | 159 | -540.7 |  |
| `tkm_trenchtank` | redalert2mod_tkm | 95000 | 95 | 6450 | 2500 | 312800×2 | 75 | 1 | 100 | 8341.3 | 2500 | +0.1 |  |
| `ra1_allies_alliedmediumtank` | redalert_allies | 60000 | 95 | 5150 | 700 | 57800×1 | 47 | 1 | 100 | 1229.8 | 700 | -0.4 |  |
| `ra1_allies_alliedtigerheavytank` | redalert_allies | 100000 | 90 | 5910 | 1300 | 60400×2 | 60 | 1 | 100 | 2013.3 | 1299 | -0.8 |  |
| `japan_chihaheavytank` | redalert_japan | 75000 | 114 | 5690 | 1200 | 63400×2 | 56 | 1 | 100 | 2264.3 | 1200 | +0.4 |  |
| `japan_igomediumtank` | redalert_japan | 75000 | 110 | 5240 | 800 | 64500×1 | 52 | 1 | 100 | 1240.4 | 800 | +0.3 |  |
| `ra1_soviets_hammertank` | redalert_soviets | 100000 | 85 | 6460 | 1500 | 51000×2 | 85 | 2 | 100 | 2266.7 | 1500 | +0.0 |  |
| `ra1_soviets_heavytank` | redalert_soviets | 95000 | 90 | 5470 | 1000 | 64600×1 | 76 | 2 | 100 | 1595.1 | 1000 | -0.0 |  |
| `ra1_soviets_kotinnucleartank` | redalert_soviets | 95000 | 85 | 6430 | 1800 | 58900×2 | 96 | 2 | 100 | 2356.0 | 1800 | -0.5 |  |
| `protoss_dragoon` | starcraft_protoss | 65000 | 100 | 4990 | 1200 | 23700×4 | 40 | 1 | 50 | 2370.0 | 1200 | +0.3 | fp-debt |
| `terran_matador` | starcraft_terran | 97500 | 110 | 5920 | 1700 | 8200×1 | 18 | 13 | 100 | 2538.1 | 1699 | -1.0 |  |
| `zerg_ultralisk` | starcraft_zerg | 97000 | 85 | 3500 | 4400 | 240500×1 | 15 | 1 | 100 | 16033.3 | 4399 | -0.6 |  |
| `td_gdi_battletank` | tiberiandawn_gdi | 95000 | 114 | 5440 | 900 | 88600×1 | 72 | 1 | 105 | 1230.6 | 900 | -0.2 | fp-debt |
| `td_gdi_predatortank` | tiberiandawn_gdi | 50000 | 110 | 5880 | 1250 | 131700×1 | 70 | 1 | 105 | 1881.4 | 1250 | -0.3 | fp-debt |
| `cabal_tarantula` | tiberiansun_cabal | 100000 | 76 | 5280 | 1000 | 83400×1 | 48 | 1 | 100 | 1737.5 | 1000 | +0.4 |  |
| `cabal_widow` | tiberiansun_cabal | 95000 | 80 | 6500 | 3500 | 120000×2 | 40 | 1 | 100 | 6000.0 | 3499 | -1.3 |  |
| `forgotten_rattytank` | tiberiansun_forgotten | 90000 | 100 | 5560 | 600 | 18000×1 | 24 | 1 | 100 | 750.0 | 600 | +0.0 |  |
| `ts_gdi_titan` | tiberiansun_gdi | 100000 | 100 | 6180 | 950 | 54500×1 | 44 | 1 | 100 | 1238.6 | 950 | -0.4 | shared-wpn? |
| `ts_gdi_titanmkii` | tiberiansun_gdi | 90000 | 95 | 6200 | 1600 | 125900×1 | 50 | 1 | 100 | 2518.0 | 1600 | +0.4 |  |
| `ts_nod_ticktank` | tiberiansun_nod | 95000 | 90 | 5320 | 800 | 31800×1 | 26 | 1 | 100 | 1223.1 | 800 | +0.0 | shared-wpn? |
| `wc2_humans_knight` | warcraft2_humans | 99000 | 85 | 3570 | 1600 | 64800×1 | 12 | 1 | 100 | 5400.0 | 1600 | +0.1 |  |
| `wc2_orcs_ogre` | warcraft2_orcs | 98000 | 90 | 3510 | 1800 | 122200×1 | 20 | 1 | 100 | 6110.0 | 1800 | +0.2 |  |

**Worst |Δ| among non-anchor members: 1083.5** (goal ≤1).

## Uniqueness check (5 raw stats — soft/protected excluded)

- **HP duplicates**: {95000: ['combat_tank.atreides', 'tkm_t72m', 'tkm_trenchtank', 'ra1_soviets_heavytank', 'ra1_soviets_kotinnucleartank', 'td_gdi_battletank', 'cabal_widow', 'ts_nod_ticktank'], 90000: ['combat_tank.harkonnen', 'ra2_allies_grizzlytank', 'ra2_soviets_rhinoheavytank', 'forgotten_rattytank', 'ts_gdi_titanmkii'], 85000: ['ixian_heavykodatank', 'futuretech_guardiantank'], 80000: ['ixian_kodatank', 'ixian_mongoose'], 97500: ['ordos_combatautoguntank', 'tkm_abrams', 'terran_matador'], 65000: ['ordos_heavycombattank', 'protoss_dragoon'], 100000: ['asianalliance_lynxtank', 'steelconsortium_mako', 'ra1_allies_alliedtigerheavytank', 'ra1_soviets_hammertank', 'cabal_tarantula', 'ts_gdi_titan'], 60000: ['ptnk.asian', 'ra1_allies_alliedmediumtank'], 55000: ['steelconsortium_quantumtank', 'schwarzermond_lunartiger'], 70000: ['naxis_kingtigerheavytank', 'latinsyndicate_smokertank'], 75000: ['japan_chihaheavytank', 'japan_igomediumtank']}
- **Speed duplicates**: {85: ['combat_tank.atreides', 'ra1_soviets_hammertank', 'ra1_soviets_kotinnucleartank', 'zerg_ultralisk', 'wc2_humans_knight'], 95: ['combat_tank.harkonnen', 'ptnk.asian', 'tkm_trenchtank', 'ra1_allies_alliedmediumtank', 'ts_gdi_titanmkii'], 110: ['ixian_heavykodatank', 'naxis_kingtigerheavytank', 'japan_igomediumtank', 'terran_matador', 'td_gdi_predatortank'], 114: ['ixian_kodatank', 'ixian_mongoose', 'steelconsortium_quantumtank', 'japan_chihaheavytank', 'td_gdi_battletank'], 100: ['ordos_combatautoguntank', 'tkm_t72m', 'protoss_dragoon', 'forgotten_rattytank', 'ts_gdi_titan'], 105: ['ordos_heavycombattank', 'futuretech_guardiantank', 'latinsyndicate_smokertank', 'tkm_abrams', 'tkm_technicaltank'], 80: ['ra2_allies_grizzlytank', 'ra2_soviets_rhinoheavytank', 'cabal_widow'], 90: ['asianalliance_lynxtank', 'ra1_allies_alliedtigerheavytank', 'ra1_soviets_heavytank', 'ts_nod_ticktank', 'wc2_orcs_ogre'], 76: ['steelconsortium_mako', 'schwarzermond_lunartiger', 'cabal_tarantula']}
- **raw ReloadDelay duplicates** (design choice — retune coarse Damage/reload by hand): {25: ['combat_tank.atreides', 'ptnk.asian', 'steelconsortium_mako'], 55: ['combat_tank.harkonnen', 'asianalliance_lynxtank'], 40: ['ixian_heavykodatank', 'ordos_heavycombattank', 'protoss_dragoon', 'cabal_widow'], 0: ['ixian_mongoose', 'tkm_abrams', 'tkm_technicaltank'], 24: ['ordos_combatautoguntank', 'forgotten_rattytank'], 65: ['steelconsortium_quantumtank', 'latinsyndicate_smokertank'], 85: ['futuretech_guardiantank', 'ra1_soviets_hammertank'], 50: ['naxis_kingtigerheavytank', 'ts_gdi_titanmkii']}

## Required YAML edits (per unit)

- `combat_tank.atreides`: HP 95000, Speed 85, Range 5140, each offensive warhead Damage 25600 (×1 = SUM 25600), ReloadDelay 25, Burst 1
- `combat_tank.harkonnen`: HP 90000, Speed 95, Range 5110, each offensive warhead Damage 54600 (×1 = SUM 54600), ReloadDelay 55, Burst 1
- `ixian_heavykodatank`: HP 85000, Speed 110, Range 6020, each offensive warhead Damage 24200 (×3 = SUM 72600), ReloadDelay 40, Burst 1
- `ixian_kodatank`: HP 80000, Speed 114, Range 5540, each offensive warhead Damage 15300 (×3 = SUM 45900), ReloadDelay 35, Burst 1
- `ixian_mongoose`: HP 80000, Speed 114, Range 5030, each offensive warhead Damage 100 (×1 = SUM 100), ReloadDelay 0, Burst 1, residual Δ -1083.5 (cost pinned at 1300)
- `ordos_combatautoguntank`: HP 97500, Speed 100, Range 6270, each offensive warhead Damage 8200 (×3 = SUM 24600), ReloadDelay 24, Burst 4, **DELETE the unconditional FirepowerMultiplier (25%)** — the Damage above already includes it (W17)
- `ordos_heavycombattank`: HP 65000, Speed 105, Range 5990, each offensive warhead Damage 59900 (×1 = SUM 59900), ReloadDelay 40, Burst 1
- `ra2_allies_grizzlytank`: HP 90000, Speed 80, Range 5290, each offensive warhead Damage 55100 (×1 = SUM 55100), ReloadDelay 45, Burst 1, **DELETE the unconditional FirepowerMultiplier (160%)** — the Damage above already includes it (W17)
- `ra2_soviets_rhinoheavytank`: HP 90000, Speed 80, Range 5370, each offensive warhead Damage 76800 (×1 = SUM 76800), ReloadDelay 54, Burst 1
- `asianalliance_lynxtank`: HP 100000, Speed 90, Range 5360, each offensive warhead Damage 70800 (×1 = SUM 70800), ReloadDelay 55, Burst 1
- `ptnk.asian`: HP 60000, Speed 95, Range 6470, each offensive warhead Damage 17700 (×8 = SUM 141600), ReloadDelay 25, Burst 2
- `steelconsortium_mako`: HP 100000, Speed 76, Range 6040, each offensive warhead Damage 10800 (×4 = SUM 43200), ReloadDelay 25, Burst 1
- `steelconsortium_quantumtank`: HP 55000, Speed 114, Range 6000, each offensive warhead Damage 112600 (×2 = SUM 225200), ReloadDelay 65, Burst 1
- `futuretech_guardiantank`: HP 85000, Speed 105, Range 6490, each offensive warhead Damage 111000 (×1 = SUM 111000), ReloadDelay 85, Burst 1
- `naxis_kingtigerheavytank`: HP 70000, Speed 110, Range 5020, each offensive warhead Damage 199300 (×1 = SUM 199300), ReloadDelay 50, Burst 1
- `schwarzermond_lunartiger`: HP 55000, Speed 76, Range 5100, each offensive warhead Damage 251100 (×1 = SUM 251100), ReloadDelay 80, Burst 1
- `latinsyndicate_smokertank`: HP 70000, Speed 105, Range 6480, each offensive warhead Damage 190200 (×1 = SUM 190200), ReloadDelay 65, Burst 1, **DELETE the unconditional FirepowerMultiplier (50%)** — the Damage above already includes it (W17)
- `tkm_abrams`: HP 97500, Speed 105, Range 4980, each offensive warhead Damage 200 (×1 = SUM 200), ReloadDelay 0, Burst 1, residual Δ -843.5 (cost pinned at 1000)
- `tkm_t72m`: HP 95000, Speed 100, Range 6210, each offensive warhead Damage 76700 (×1 = SUM 76700), ReloadDelay 42, Burst 1
- `tkm_technicaltank`: HP 47500, Speed 105, Range 5000, each offensive warhead Damage 300 (×1 = SUM 300), ReloadDelay 0, Burst 1, residual Δ -540.7 (cost pinned at 700)
- `tkm_trenchtank`: HP 95000, Speed 95, Range 6450, each offensive warhead Damage 312800 (×2 = SUM 625600), ReloadDelay 75, Burst 1
- `ra1_allies_alliedmediumtank`: HP 60000, Speed 95, Range 5150, each offensive warhead Damage 57800 (×1 = SUM 57800), ReloadDelay 47, Burst 1
- `ra1_allies_alliedtigerheavytank`: HP 100000, Speed 90, Range 5910, each offensive warhead Damage 60400 (×2 = SUM 120800), ReloadDelay 60, Burst 1
- `japan_chihaheavytank`: HP 75000, Speed 114, Range 5690, each offensive warhead Damage 63400 (×2 = SUM 126800), ReloadDelay 56, Burst 1
- `japan_igomediumtank`: HP 75000, Speed 110, Range 5240, each offensive warhead Damage 64500 (×1 = SUM 64500), ReloadDelay 52, Burst 1
- `ra1_soviets_hammertank`: HP 100000, Speed 85, Range 6460, each offensive warhead Damage 51000 (×2 = SUM 102000), ReloadDelay 85, Burst 2
- `ra1_soviets_heavytank`: HP 95000, Speed 90, Range 5470, each offensive warhead Damage 64600 (×1 = SUM 64600), ReloadDelay 76, Burst 2
- `ra1_soviets_kotinnucleartank`: HP 95000, Speed 85, Range 6430, each offensive warhead Damage 58900 (×2 = SUM 117800), ReloadDelay 96, Burst 2
- `protoss_dragoon`: HP 65000, Speed 100, Range 4990, each offensive warhead Damage 23700 (×4 = SUM 94800), ReloadDelay 40, Burst 1, **DELETE the unconditional FirepowerMultiplier (50%)** — the Damage above already includes it (W17)
- `terran_matador`: HP 97500, Speed 110, Range 5920, each offensive warhead Damage 8200 (×1 = SUM 8200), ReloadDelay 18, Burst 13
- `zerg_ultralisk`: HP 97000, Speed 85, Range 3500, each offensive warhead Damage 240500 (×1 = SUM 240500), ReloadDelay 15, Burst 1
- `td_gdi_battletank`: HP 95000, Speed 114, Range 5440, each offensive warhead Damage 88600 (×1 = SUM 88600), ReloadDelay 72, Burst 1, **DELETE the unconditional FirepowerMultiplier (105%)** — the Damage above already includes it (W17)
- `td_gdi_predatortank`: HP 50000, Speed 110, Range 5880, each offensive warhead Damage 131700 (×1 = SUM 131700), ReloadDelay 70, Burst 1, **DELETE the unconditional FirepowerMultiplier (105%)** — the Damage above already includes it (W17)
- `cabal_tarantula`: HP 100000, Speed 76, Range 5280, each offensive warhead Damage 83400 (×1 = SUM 83400), ReloadDelay 48, Burst 1
- `cabal_widow`: HP 95000, Speed 80, Range 6500, each offensive warhead Damage 120000 (×2 = SUM 240000), ReloadDelay 40, Burst 1, residual Δ -1.3 (cost pinned at 3500)
- `forgotten_rattytank`: HP 90000, Speed 100, Range 5560, each offensive warhead Damage 18000 (×1 = SUM 18000), ReloadDelay 24, Burst 1
- `ts_gdi_titan`: HP 100000, Speed 100, Range 6180, each offensive warhead Damage 54500 (×1 = SUM 54500), ReloadDelay 44, Burst 1
- `ts_gdi_titanmkii`: HP 90000, Speed 95, Range 6200, each offensive warhead Damage 125900 (×1 = SUM 125900), ReloadDelay 50, Burst 1
- `ts_nod_ticktank`: HP 95000, Speed 90, Range 5320, each offensive warhead Damage 31800 (×1 = SUM 31800), ReloadDelay 26, Burst 1
- `wc2_humans_knight`: HP 99000, Speed 85, Range 3570, each offensive warhead Damage 64800 (×1 = SUM 64800), ReloadDelay 12, Burst 1
- `wc2_orcs_ogre`: HP 98000, Speed 90, Range 3510, each offensive warhead Damage 122200 (×1 = SUM 122200), ReloadDelay 20, Burst 1
