# Support infantry rebalance proposal

Anchor spec: HP=5000, Speed=50, Range=0, eff-DPS=0, Cost=500

Converter law: cost pinned, range clamped to band + made unique, eff-DPS trimmed to Δ≤1 via 100-grid warhead Damage; unconditional FirepowerMultiplier is retired.

Uniqueness separates **raw warhead Damage (the 5-stat law as written)**. The levers run coarsest-first — Damage (100 grid) → Speed (1) → Range (10) — because one Damage step is a whole shot and running it last threw away everything the fine levers had achieved.

| actor | faction | HP | spd | rng | cost | dmg/wh×n | rl | burst | legacy FP% | eff DPS | price | Δ | flags |
|---|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|---|
| `ra2_allies_chronolegionnaire` | redalert2_allies | 35000 | 46 | 10 | 1500 | 100×1 | 0 | 1 | 100 | 0.0 | 1118 | -382.4 |  |
| `ra2_allies_engineer` | redalert2_allies | 1000 | 57 | 0 | 500 | 200×1 | 0 | 1 | 100 | 0.0 | 280 | -220.5 |  |
| `ra2_allies_ra2spy` | redalert2_allies | 15000 | 59 | 2960 | 500 | 300×1 | 0 | 1 | 100 | 0.0 | 1059 | +559.0 | OVERPRICED@min-dps |
| `ra2_soviets_crazyivan` | redalert2_soviets | 14000 | 58 | 2950 | 600 | 400×1 | 0 | 1 | 100 | 0.0 | 1066 | +466.1 | OVERPRICED@min-dps |
| `ra2_soviets_engineer` | redalert2_soviets | 13000 | 58 | 3030 | 500 | 500×1 | 0 | 1 | 100 | 0.0 | 1077 | +577.3 | OVERPRICED@min-dps |
| `yuri_clone` | redalert2_yuri | 7000 | 52 | 3070 | 500 | 600×1 | 0 | 1 | 100 | 0.0 | 558 | +58.0 | OVERPRICED@min-dps |
| `yuri_engineer` | redalert2_yuri | 6000 | 47 | 3090 | 500 | 700×1 | 0 | 1 | 100 | 0.0 | 538 | +37.8 | OVERPRICED@min-dps |
| `asianalliance_engineer` | redalert2mod_asianalliance | 23000 | 51 | 3140 | 500 | 800×1 | 0 | 1 | 100 | 0.0 | 1574 | +1073.8 | OVERPRICED@min-dps |
| `steelconsortium_engineer` | redalert2mod_consortium | 12000 | 56 | 3050 | 500 | 900×1 | 0 | 1 | 100 | 0.0 | 985 | +485.3 | OVERPRICED@min-dps |
| `futuretech_engineer` | redalert2mod_futuretech | 5000 | 49 | 50 | 500 | 1000×1 | 0 | 1 | 100 | 0.0 | 494 | -5.8 |  |
| `futuretech_repairdroid` | redalert2mod_futuretech | 50000 | 45 | 5020 | 800 | 600×2 | 50 | 10 | 100 | 203.4 | 1815 | +1014.9 | OVERPRICED@min-dps |
| `futuretech_spyfutu` | redalert2mod_futuretech | 3000 | 59 | 40 | 1000 | 1100×1 | 0 | 1 | 100 | 0.0 | 168 | -832.2 |  |
| `naxis_portableflak` | redalert2mod_naxis | 20000 | 56 | 3000 | 400 | 1300×1 | 0 | 1 | 100 | 0.0 | 1500 | +1100.0 | OVERPRICED@min-dps |
| `naxis_slaveoverseer` | redalert2mod_naxis | 19000 | 60 | 5620 | 500 | 1400×1 | 75 | 1 | 100 | 18.7 | 1462 | +961.8 | OVERPRICED@min-dps shared-wpn? |
| `latinsyndicate_engineer` | redalert2mod_syndicate | 3000 | 52 | 30 | 500 | 1500×1 | 0 | 1 | 100 | 0.0 | 391 | -109.0 |  |
| `latinsyndicate_narco` | redalert2mod_syndicate | 28000 | 59 | 3020 | 756 | 1600×1 | 0 | 1 | 105 | 0.0 | 1576 | +819.9 | OVERPRICED@min-dps fp-debt |
| `tkm_engineer` | redalert2mod_tkm | 11000 | 56 | 2930 | 500 | 1700×1 | 0 | 1 | 100 | 0.0 | 921 | +421.0 | OVERPRICED@min-dps |
| `ra1_allies_mechanic` | redalert_allies | 18000 | 53 | 5010 | 500 | 900×2 | 75 | 10 | 100 | 214.3 | 964 | +464.2 | OVERPRICED@min-dps |
| `ra1_allies_medic` | redalert_allies | 17000 | 54 | 4990 | 500 | 1000×2 | 75 | 1 | 100 | 26.7 | 1271 | +771.3 | OVERPRICED@min-dps |
| `ra1_allies_raspy` | redalert_allies | 1000 | 58 | 20 | 500 | 1342177300×1 | 80 | 1 | 100 | 16777216.2 | 216 | -284.1 |  |
| `engineer` | shared_d2k | 5000 | 50 | 0 | 500 | 0×1 | 0 | 1 | 100 | 0.0 | 500 | +0.0 | anchor |
| `ra1_engineer` | shared_redalert | 16000 | 48 | 2970 | 500 | 1900×1 | 0 | 1 | 100 | 0.0 | 1108 | +608.0 | OVERPRICED@min-dps |
| `E6` | shared_tiberiandawn | 24000 | 50 | 3150 | 500 | 2100×1 | 0 | 1 | 100 | 0.0 | 1608 | +1108.3 | OVERPRICED@min-dps |
| `protoss_hightemplar` | starcraft_protoss | 34000 | 60 | 2980 | 800 | 2200×1 | 0 | 1 | 100 | 0.0 | 2183 | +1383.4 | OVERPRICED@min-dps |
| `terran_medic` | starcraft_terran | 60000 | 59 | 2940 | 600 | 2300×1 | 0 | 1 | 100 | 0.0 | 4256 | +3655.8 | OVERPRICED@min-dps |
| `zerg_defiler` | starcraft_zerg | 80000 | 60 | 6000 | 1400 | 2400×1 | 175 | 1 | 100 | 13.7 | 6515 | +5114.6 | OVERPRICED@min-dps |
| `cabal_engineer` | tiberiansun_cabal | 22000 | 40 | 3130 | 800 | 2500×1 | 0 | 1 | 100 | 0.0 | 1263 | +463.3 | OVERPRICED@min-dps |
| `cabal_hackercyborg` | tiberiansun_cabal | 30000 | 60 | 3120 | 1250 | 2600×1 | 0 | 1 | 100 | 0.0 | 2267 | +1016.7 | OVERPRICED@min-dps |
| `forgotten_engineer` | tiberiansun_forgotten | 21000 | 55 | 4320 | 600 | 2700×1 | 25 | 1 | 100 | 108.0 | 1543 | +942.5 | OVERPRICED@min-dps shared-wpn? |
| `forgotten_mutanthijacker` | tiberiansun_forgotten | 25000 | 60 | 3100 | 750 | 2800×1 | 0 | 1 | 100 | 0.0 | 1612 | +862.4 | OVERPRICED@min-dps |
| `ts_gdi_engineer` | tiberiansun_gdi | 10000 | 54 | 4290 | 600 | 2900×1 | 25 | 1 | 100 | 116.0 | 835 | +235.0 | OVERPRICED@min-dps shared-wpn? |
| `ts_gdi_medic` | tiberiansun_gdi | 9000 | 54 | 4980 | 500 | 1500×2 | 75 | 1 | 100 | 40.0 | 773 | +272.7 | OVERPRICED@min-dps |
| `ts_nod_engineer` | tiberiansun_nod | 8000 | 52 | 4300 | 600 | 3100×1 | 25 | 1 | 100 | 124.0 | 693 | +92.7 | OVERPRICED@min-dps shared-wpn? |

**Worst |Δ| among non-anchor members: 5114.6** (goal ≤1).

## Uniqueness check (5 raw stats — soft/protected excluded)

- **HP duplicates**: {1000: ['ra2_allies_engineer', 'ra1_allies_raspy'], 3000: ['futuretech_spyfutu', 'latinsyndicate_engineer']}
- **Speed duplicates**: {59: ['ra2_allies_ra2spy', 'futuretech_spyfutu', 'latinsyndicate_narco', 'terran_medic'], 58: ['ra2_soviets_crazyivan', 'ra2_soviets_engineer', 'ra1_allies_raspy'], 52: ['yuri_clone', 'latinsyndicate_engineer', 'ts_nod_engineer'], 56: ['steelconsortium_engineer', 'naxis_portableflak', 'tkm_engineer'], 60: ['naxis_slaveoverseer', 'protoss_hightemplar', 'zerg_defiler', 'cabal_hackercyborg', 'forgotten_mutanthijacker'], 54: ['ra1_allies_medic', 'ts_gdi_engineer', 'ts_gdi_medic']}
- **raw ReloadDelay duplicates** (design choice — retune coarse Damage/reload by hand): {0: ['ra2_allies_chronolegionnaire', 'ra2_allies_engineer', 'ra2_allies_ra2spy', 'ra2_soviets_crazyivan', 'ra2_soviets_engineer', 'yuri_clone', 'yuri_engineer', 'asianalliance_engineer', 'steelconsortium_engineer', 'futuretech_engineer', 'futuretech_spyfutu', 'naxis_portableflak', 'latinsyndicate_engineer', 'latinsyndicate_narco', 'tkm_engineer', 'ra1_engineer', 'E6', 'protoss_hightemplar', 'terran_medic', 'cabal_engineer', 'cabal_hackercyborg', 'forgotten_mutanthijacker'], 75: ['naxis_slaveoverseer', 'ra1_allies_mechanic', 'ra1_allies_medic', 'ts_gdi_medic'], 25: ['forgotten_engineer', 'ts_gdi_engineer', 'ts_nod_engineer']}

## Required YAML edits (per unit)

- `ra2_allies_chronolegionnaire`: HP 35000, Speed 46, Range 10, each offensive warhead Damage 100 (×1 = SUM 100), ReloadDelay 0, Burst 1, residual Δ -382.4 (cost pinned at 1500)
- `ra2_allies_engineer`: HP 1000, Speed 57, Range 0, each offensive warhead Damage 200 (×1 = SUM 200), ReloadDelay 0, Burst 1, residual Δ -220.5 (cost pinned at 500)
- `ra2_allies_ra2spy`: HP 15000, Speed 59, Range 2960, each offensive warhead Damage 300 (×1 = SUM 300), ReloadDelay 0, Burst 1, residual Δ +559.0 (cost pinned at 500)
- `ra2_soviets_crazyivan`: HP 14000, Speed 58, Range 2950, each offensive warhead Damage 400 (×1 = SUM 400), ReloadDelay 0, Burst 1, residual Δ +466.1 (cost pinned at 600)
- `ra2_soviets_engineer`: HP 13000, Speed 58, Range 3030, each offensive warhead Damage 500 (×1 = SUM 500), ReloadDelay 0, Burst 1, residual Δ +577.3 (cost pinned at 500)
- `yuri_clone`: HP 7000, Speed 52, Range 3070, each offensive warhead Damage 600 (×1 = SUM 600), ReloadDelay 0, Burst 1, residual Δ +58.0 (cost pinned at 500)
- `yuri_engineer`: HP 6000, Speed 47, Range 3090, each offensive warhead Damage 700 (×1 = SUM 700), ReloadDelay 0, Burst 1, residual Δ +37.8 (cost pinned at 500)
- `asianalliance_engineer`: HP 23000, Speed 51, Range 3140, each offensive warhead Damage 800 (×1 = SUM 800), ReloadDelay 0, Burst 1, residual Δ +1073.8 (cost pinned at 500)
- `steelconsortium_engineer`: HP 12000, Speed 56, Range 3050, each offensive warhead Damage 900 (×1 = SUM 900), ReloadDelay 0, Burst 1, residual Δ +485.3 (cost pinned at 500)
- `futuretech_engineer`: HP 5000, Speed 49, Range 50, each offensive warhead Damage 1000 (×1 = SUM 1000), ReloadDelay 0, Burst 1, residual Δ -5.8 (cost pinned at 500)
- `futuretech_repairdroid`: HP 50000, Speed 45, Range 5020, each offensive warhead Damage 600 (×2 = SUM 1200), ReloadDelay 50, Burst 10, residual Δ +1014.9 (cost pinned at 800)
- `futuretech_spyfutu`: HP 3000, Speed 59, Range 40, each offensive warhead Damage 1100 (×1 = SUM 1100), ReloadDelay 0, Burst 1, residual Δ -832.2 (cost pinned at 1000)
- `naxis_portableflak`: HP 20000, Speed 56, Range 3000, each offensive warhead Damage 1300 (×1 = SUM 1300), ReloadDelay 0, Burst 1, residual Δ +1100.0 (cost pinned at 400)
- `naxis_slaveoverseer`: HP 19000, Speed 60, Range 5620, each offensive warhead Damage 1400 (×1 = SUM 1400), ReloadDelay 75, Burst 1, residual Δ +961.8 (cost pinned at 500)
- `latinsyndicate_engineer`: HP 3000, Speed 52, Range 30, each offensive warhead Damage 1500 (×1 = SUM 1500), ReloadDelay 0, Burst 1, residual Δ -109.0 (cost pinned at 500)
- `latinsyndicate_narco`: HP 28000, Speed 59, Range 3020, each offensive warhead Damage 1600 (×1 = SUM 1600), ReloadDelay 0, Burst 1, **DELETE the unconditional FirepowerMultiplier (105%)** — the Damage above already includes it (W17), residual Δ +819.9 (cost pinned at 756)
- `tkm_engineer`: HP 11000, Speed 56, Range 2930, each offensive warhead Damage 1700 (×1 = SUM 1700), ReloadDelay 0, Burst 1, residual Δ +421.0 (cost pinned at 500)
- `ra1_allies_mechanic`: HP 18000, Speed 53, Range 5010, each offensive warhead Damage 900 (×2 = SUM 1800), ReloadDelay 75, Burst 10, residual Δ +464.2 (cost pinned at 500)
- `ra1_allies_medic`: HP 17000, Speed 54, Range 4990, each offensive warhead Damage 1000 (×2 = SUM 2000), ReloadDelay 75, Burst 1, residual Δ +771.3 (cost pinned at 500)
- `ra1_allies_raspy`: HP 1000, Speed 58, Range 20, each offensive warhead Damage 1342177300 (×1 = SUM 1342177300), ReloadDelay 80, Burst 1, residual Δ -284.1 (cost pinned at 500)
- `ra1_engineer`: HP 16000, Speed 48, Range 2970, each offensive warhead Damage 1900 (×1 = SUM 1900), ReloadDelay 0, Burst 1, residual Δ +608.0 (cost pinned at 500)
- `E6`: HP 24000, Speed 50, Range 3150, each offensive warhead Damage 2100 (×1 = SUM 2100), ReloadDelay 0, Burst 1, residual Δ +1108.3 (cost pinned at 500)
- `protoss_hightemplar`: HP 34000, Speed 60, Range 2980, each offensive warhead Damage 2200 (×1 = SUM 2200), ReloadDelay 0, Burst 1, residual Δ +1383.4 (cost pinned at 800)
- `terran_medic`: HP 60000, Speed 59, Range 2940, each offensive warhead Damage 2300 (×1 = SUM 2300), ReloadDelay 0, Burst 1, residual Δ +3655.8 (cost pinned at 600)
- `zerg_defiler`: HP 80000, Speed 60, Range 6000, each offensive warhead Damage 2400 (×1 = SUM 2400), ReloadDelay 175, Burst 1, residual Δ +5114.6 (cost pinned at 1400)
- `cabal_engineer`: HP 22000, Speed 40, Range 3130, each offensive warhead Damage 2500 (×1 = SUM 2500), ReloadDelay 0, Burst 1, residual Δ +463.3 (cost pinned at 800)
- `cabal_hackercyborg`: HP 30000, Speed 60, Range 3120, each offensive warhead Damage 2600 (×1 = SUM 2600), ReloadDelay 0, Burst 1, residual Δ +1016.7 (cost pinned at 1250)
- `forgotten_engineer`: HP 21000, Speed 55, Range 4320, each offensive warhead Damage 2700 (×1 = SUM 2700), ReloadDelay 25, Burst 1, residual Δ +942.5 (cost pinned at 600)
- `forgotten_mutanthijacker`: HP 25000, Speed 60, Range 3100, each offensive warhead Damage 2800 (×1 = SUM 2800), ReloadDelay 0, Burst 1, residual Δ +862.4 (cost pinned at 750)
- `ts_gdi_engineer`: HP 10000, Speed 54, Range 4290, each offensive warhead Damage 2900 (×1 = SUM 2900), ReloadDelay 25, Burst 1, residual Δ +235.0 (cost pinned at 600)
- `ts_gdi_medic`: HP 9000, Speed 54, Range 4980, each offensive warhead Damage 1500 (×2 = SUM 3000), ReloadDelay 75, Burst 1, residual Δ +272.7 (cost pinned at 500)
- `ts_nod_engineer`: HP 8000, Speed 52, Range 4300, each offensive warhead Damage 3100 (×1 = SUM 3100), ReloadDelay 25, Burst 1, residual Δ +92.7 (cost pinned at 600)
