# Commando infantry rebalance proposal

Anchor spec: HP=80000, Speed=65, Range=8000, eff-DPS=1200, Cost=3000

Converter law: cost pinned, range clamped to band + made unique, eff-DPS trimmed to Δ≤1 via 100-grid warhead Damage; unconditional FirepowerMultiplier is retired.

Uniqueness separates **raw warhead Damage (the 5-stat law as written)**. The levers run coarsest-first — Damage (100 grid) → Speed (1) → Range (10) — because one Damage step is a whole shot and running it last threw away everything the fine levers had achieved.

| actor | faction | HP | spd | rng | cost | dmg/wh×n | rl | burst | legacy FP% | eff DPS | price | Δ | flags |
|---|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|---|
| `ordos_facedancer` | d2k_ordos | 89000 | 74 | 7260 | 5000 | 18700×3 | 90 | 1 | 100 | 623.3 | 4999 | -0.5 |  |
| `ra2_allies_tanyaii` | redalert2_allies | 50000 | 76 | 8330 | 3000 | 6900×1 | 12 | 2 | 100 | 920.0 | 3000 | -0.0 |  |
| `ra2_soviets_boris` | redalert2_soviets | 90000 | 59 | 8310 | 3000 | 800×1 | 18 | 4 | 100 | 133.3 | 3000 | +0.4 |  |
| `yuri_yurix` | redalert2_yuri | 20000 | 77 | 9000 | 4000 | 100×1 | 125 | 1 | 100 | 0.0 | 803 | -3196.6 |  |
| `asianalliance_asiancommando` | redalert2mod_asianalliance | 49000 | 75 | 8920 | 3000 | 12300×3 | 25 | 1 | 100 | 1476.0 | 3000 | +0.3 |  |
| `steelconsortium_steelrunner` | redalert2mod_consortium | 60000 | 57 | 7120 | 3000 | 900×3 | 8 | 2 | 100 | 450.0 | 3000 | +0.1 |  |
| `steelconsortium_stalker` | redalert2mod_consortium | 97500 | 78 | 8950 | 4000 | 86400×1 | 103 | 1 | 100 | 838.8 | 4000 | +0.0 |  |
| `steelconsortium_whiterabbit` | redalert2mod_consortium | 92500 | 52 | 7990 | 4500 | 200×1 | 0 | 1 | 100 | 0.0 | 2384 | -2116.3 |  |
| `futuretech_cryolegionnaire` | redalert2mod_futuretech | 99000 | 62 | 8540 | 3500 | 58600×1 | 30 | 1 | 50 | 1953.3 | 3500 | +0.0 | fp-debt |
| `schwarzermond_parzival` | redalert2mod_schwarzermond | 96000 | 61 | 7210 | 3000 | 7300×1 | 60 | 1 | 100 | 121.7 | 3000 | +0.0 |  |
| `latinsyndicate_freedomfighter` | redalert2mod_syndicate | 77000 | 55 | 7530 | 3000 | 4000×1 | 42 | 5 | 50 | 400.0 | 3000 | +0.0 | fp-debt |
| `tkm_von` | redalert2mod_tkm | 51000 | 73 | 8650 | 3000 | 2400×3 | 25 | 1 | 100 | 288.0 | 3000 | +0.1 |  |
| `ra1_allies_tanya` | redalert_allies | 44000 | 68 | 8220 | 3000 | 1500×1 | 7 | 2 | 100 | 428.6 | 3000 | -0.0 |  |
| `japan_exorcist` | redalert_japan | 74000 | 56 | 7610 | 3000 | 5000×1 | 30 | 3 | 100 | 300.0 | 3000 | -0.0 |  |
| `ra1_soviets_volkov` | redalert_soviets | 97000 | 58 | 7240 | 10000 | 14300×2 | 15 | 2 | 100 | 2860.0 | 10000 | -0.0 |  |
| `protoss_patriarch` | starcraft_protoss | 75000 | 70 | 8140 | 4000 | 20400×1 | 75 | 2 | 100 | 544.0 | 4000 | +0.0 |  |
| `protoss_zeratul` | starcraft_protoss | 98000 | 72 | 7930 | 4000 | 6200×1 | 24 | 1 | 100 | 258.3 | 4000 | +0.0 |  |
| `terran_jimraynor` | starcraft_terran | 94000 | 66 | 7300 | 4000 | 1000×2 | 12 | 5 | 100 | 500.0 | 3999 | -0.7 |  |
| `zerg_kerrigan` | starcraft_zerg | 91000 | 71 | 7030 | 4000 | 7400×1 | 20 | 1 | 100 | 370.0 | 4000 | +0.0 |  |
| `td_gdi_commando` | tiberiandawn_gdi | 80000 | 65 | 8000 | 3000 | 40000×1 | 25 | 1 | 100 | 1600.0 | 3583 | +583.3 | anchor |
| `td_gdi_havoc` | tiberiandawn_gdi | 95000 | 77 | 7980 | 4000 | 300×1 | 0 | 1 | 120 | 0.0 | 3033 | -966.6 | fp-debt |
| `td_nod_commando` | tiberiandawn_nod | 79000 | 65 | 8070 | 3000 | 30100×1 | 25 | 1 | 100 | 1204.0 | 2999 | -0.8 |  |
| `td_nod_lasercommando` | tiberiandawn_nod | 57000 | 75 | 7750 | 5000 | 4400×1 | 8 | 1 | 100 | 550.0 | 5000 | +0.0 |  |
| `cabal_berserker` | tiberiansun_cabal | 85000 | 60 | 7180 | 10000 | 68300×1 | 30 | 3 | 100 | 5122.5 | 9999 | -0.7 |  |
| `forgotten_ghoststalker` | tiberiansun_forgotten | 100000 | 67 | 8080 | 4000 | 6300×1 | 25 | 1 | 100 | 252.0 | 4000 | -0.0 |  |
| `ts_gdi_railguncommando` | tiberiansun_gdi | 93000 | 64 | 7880 | 5000 | 17400×1 | 52 | 3 | 100 | 870.0 | 5000 | +0.0 |  |
| `ts_nod_shotguncommando` | tiberiansun_nod | 92000 | 69 | 7960 | 3000 | 400×7 | 20 | 1 | 100 | 140.0 | 3000 | -0.0 |  |

**Worst |Δ| among non-anchor members: 3196.6** (goal ≤1).

## Uniqueness check (5 raw stats — soft/protected excluded)

- **Speed duplicates**: {77: ['yuri_yurix', 'td_gdi_havoc'], 75: ['asianalliance_asiancommando', 'td_nod_lasercommando']}
- **raw ReloadDelay duplicates** (design choice — retune coarse Damage/reload by hand): {12: ['ra2_allies_tanyaii', 'terran_jimraynor'], 25: ['asianalliance_asiancommando', 'tkm_von', 'td_nod_commando', 'forgotten_ghoststalker'], 8: ['steelconsortium_steelrunner', 'td_nod_lasercommando'], 0: ['steelconsortium_whiterabbit', 'td_gdi_havoc'], 30: ['futuretech_cryolegionnaire', 'japan_exorcist', 'cabal_berserker'], 20: ['zerg_kerrigan', 'ts_nod_shotguncommando']}

## Required YAML edits (per unit)

- `ordos_facedancer`: HP 89000, Speed 74, Range 7260, each offensive warhead Damage 18700 (×3 = SUM 56100), ReloadDelay 90, Burst 1
- `ra2_allies_tanyaii`: HP 50000, Speed 76, Range 8330, each offensive warhead Damage 6900 (×1 = SUM 6900), ReloadDelay 12, Burst 2
- `ra2_soviets_boris`: HP 90000, Speed 59, Range 8310, each offensive warhead Damage 800 (×1 = SUM 800), ReloadDelay 18, Burst 4
- `yuri_yurix`: HP 20000, Speed 77, Range 9000, each offensive warhead Damage 100 (×1 = SUM 100), ReloadDelay 125, Burst 1, residual Δ -3196.6 (cost pinned at 4000)
- `asianalliance_asiancommando`: HP 49000, Speed 75, Range 8920, each offensive warhead Damage 12300 (×3 = SUM 36900), ReloadDelay 25, Burst 1
- `steelconsortium_steelrunner`: HP 60000, Speed 57, Range 7120, each offensive warhead Damage 900 (×3 = SUM 2700), ReloadDelay 8, Burst 2
- `steelconsortium_stalker`: HP 97500, Speed 78, Range 8950, each offensive warhead Damage 86400 (×1 = SUM 86400), ReloadDelay 103, Burst 1
- `steelconsortium_whiterabbit`: HP 92500, Speed 52, Range 7990, each offensive warhead Damage 200 (×1 = SUM 200), ReloadDelay 0, Burst 1, residual Δ -2116.3 (cost pinned at 4500)
- `futuretech_cryolegionnaire`: HP 99000, Speed 62, Range 8540, each offensive warhead Damage 58600 (×1 = SUM 58600), ReloadDelay 30, Burst 1, **DELETE the unconditional FirepowerMultiplier (50%)** — the Damage above already includes it (W17)
- `schwarzermond_parzival`: HP 96000, Speed 61, Range 7210, each offensive warhead Damage 7300 (×1 = SUM 7300), ReloadDelay 60, Burst 1
- `latinsyndicate_freedomfighter`: HP 77000, Speed 55, Range 7530, each offensive warhead Damage 4000 (×1 = SUM 4000), ReloadDelay 42, Burst 5, **DELETE the unconditional FirepowerMultiplier (50%)** — the Damage above already includes it (W17)
- `tkm_von`: HP 51000, Speed 73, Range 8650, each offensive warhead Damage 2400 (×3 = SUM 7200), ReloadDelay 25, Burst 1
- `ra1_allies_tanya`: HP 44000, Speed 68, Range 8220, each offensive warhead Damage 1500 (×1 = SUM 1500), ReloadDelay 7, Burst 2
- `japan_exorcist`: HP 74000, Speed 56, Range 7610, each offensive warhead Damage 5000 (×1 = SUM 5000), ReloadDelay 30, Burst 3
- `ra1_soviets_volkov`: HP 97000, Speed 58, Range 7240, each offensive warhead Damage 14300 (×2 = SUM 28600), ReloadDelay 15, Burst 2
- `protoss_patriarch`: HP 75000, Speed 70, Range 8140, each offensive warhead Damage 20400 (×1 = SUM 20400), ReloadDelay 75, Burst 2
- `protoss_zeratul`: HP 98000, Speed 72, Range 7930, each offensive warhead Damage 6200 (×1 = SUM 6200), ReloadDelay 24, Burst 1
- `terran_jimraynor`: HP 94000, Speed 66, Range 7300, each offensive warhead Damage 1000 (×2 = SUM 2000), ReloadDelay 12, Burst 5
- `zerg_kerrigan`: HP 91000, Speed 71, Range 7030, each offensive warhead Damage 7400 (×1 = SUM 7400), ReloadDelay 20, Burst 1
- `td_gdi_havoc`: HP 95000, Speed 77, Range 7980, each offensive warhead Damage 300 (×1 = SUM 300), ReloadDelay 0, Burst 1, **DELETE the unconditional FirepowerMultiplier (120%)** — the Damage above already includes it (W17), residual Δ -966.6 (cost pinned at 4000)
- `td_nod_commando`: HP 79000, Speed 65, Range 8070, each offensive warhead Damage 30100 (×1 = SUM 30100), ReloadDelay 25, Burst 1
- `td_nod_lasercommando`: HP 57000, Speed 75, Range 7750, each offensive warhead Damage 4400 (×1 = SUM 4400), ReloadDelay 8, Burst 1
- `cabal_berserker`: HP 85000, Speed 60, Range 7180, each offensive warhead Damage 68300 (×1 = SUM 68300), ReloadDelay 30, Burst 3
- `forgotten_ghoststalker`: HP 100000, Speed 67, Range 8080, each offensive warhead Damage 6300 (×1 = SUM 6300), ReloadDelay 25, Burst 1
- `ts_gdi_railguncommando`: HP 93000, Speed 64, Range 7880, each offensive warhead Damage 17400 (×1 = SUM 17400), ReloadDelay 52, Burst 3
- `ts_nod_shotguncommando`: HP 92000, Speed 69, Range 7960, each offensive warhead Damage 400 (×7 = SUM 2800), ReloadDelay 20, Burst 1
