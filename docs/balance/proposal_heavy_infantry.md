# Heavy Infantry infantry rebalance proposal

Anchor spec: HP=50000, Speed=50, Range=5000, eff-DPS=1000, Cost=800

Converter law: cost pinned, range clamped to band + made unique, eff-DPS trimmed to Δ≤1 via 100-grid warhead Damage; unconditional FirepowerMultiplier is retired.

Uniqueness separates **raw warhead Damage (the 5-stat law as written)**. The levers run coarsest-first — Damage (100 grid) → Speed (1) → Range (10) — because one Damage step is a whole shot and running it last threw away everything the fine levers had achieved.

| actor | faction | HP | spd | rng | cost | dmg/wh×n | rl | burst | legacy FP% | eff DPS | price | Δ | flags |
|---|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|---|
| `ixian_shockinfantry` | d2k_ixian | 34000 | 46 | 5480 | 500 | 51500×1 | 45 | 1 | 100 | 1144.4 | 500 | +0.0 |  |
| `ixian_storminfantry` | d2k_ixian | 44000 | 44 | 5430 | 800 | 129100×1 | 66 | 1 | 100 | 1956.1 | 800 | -0.1 |  |
| `ordos_chemicaltrooper` | d2k_ordos | 28000 | 52 | 5170 | 400 | 9600×1 | 75 | 1 | 100 | 128.0 | 400 | -0.0 |  |
| `ra2_soviets_desolator` | redalert2_soviets | 30000 | 59 | 5440 | 700 | 44700×1 | 45 | 1 | 100 | 993.3 | 700 | -0.0 |  |
| `ra2_soviets_teslatrooper` | redalert2_soviets | 48000 | 54 | 5220 | 500 | 2300×1 | 100 | 1 | 100 | 23.0 | 500 | +0.1 |  |
| `yuri_biotrooper` | redalert2_yuri | 96000 | 56 | 4770 | 400 | 400×1 | 50 | 6 | 100 | 34.3 | 400 | +0.0 |  |
| `asianalliance_asianflametrooper` | redalert2mod_asianalliance | 26000 | 53 | 4810 | 400 | 1500×1 | 38 | 6 | 50 | 169.8 | 400 | +0.1 | fp-debt |
| `asianalliance_plasmatrooper` | redalert2mod_asianalliance | 63000 | 42 | 5120 | 500 | 1300×8 | 34 | 1 | 100 | 305.9 | 500 | -0.1 |  |
| `steelconsortium_quantummissiletrooper` | redalert2mod_consortium | 65000 | 58 | 4850 | 1150 | 5900×4 | 48 | 2 | 100 | 983.3 | 1150 | -0.1 |  |
| `futuretech_cannondroid` | redalert2mod_futuretech | 81000 | 60 | 5360 | 525 | 7300×1 | 25 | 1 | 100 | 292.0 | 525 | -0.1 |  |
| `naxis_naxiflamer` | redalert2mod_naxis | 23000 | 49 | 4720 | 225 | 500×1 | 60 | 3 | 100 | 22.7 | 225 | -0.0 |  |
| `naxis_naximachinegunners` | redalert2mod_naxis | 59000 | 57 | 5030 | 600 | 100×1 | 0 | 1 | 100 | 0.0 | 418 | -182.1 |  |
| `naxis_panzerfausttrooper` | redalert2mod_naxis | 35000 | 47 | 5410 | 400 | 5100×2 | 135 | 1 | 100 | 75.6 | 399 | -0.7 |  |
| `naxis_panzerschreck` | redalert2mod_naxis | 95000 | 43 | 5270 | 600 | 11900×2 | 124 | 1 | 100 | 191.9 | 600 | +0.1 |  |
| `schwarzermond_noidmgarmor` | redalert2mod_schwarzermond | 50000 | 55 | 4830 | 500 | 200×6 | 100 | 5 | 100 | 50.0 | 500 | +0.0 |  |
| `schwarzermond_ubermensch` | redalert2mod_schwarzermond | 64000 | 60 | 5140 | 700 | 2300×3 | 30 | 2 | 100 | 418.2 | 700 | -0.1 |  |
| `latinsyndicate_latinflametrooper` | redalert2mod_syndicate | 55000 | 56 | 5330 | 500 | 8900×1 | 44 | 4 | 100 | 635.7 | 500 | +0.1 | shared-wpn? |
| `tkm_juggernaut` | redalert2mod_tkm | 37000 | 53 | 4500 | 650 | 9900×1 | 8 | 1 | 100 | 1237.5 | 651 | +0.9 |  |
| `japan_japaneseflamethrower` | redalert_japan | 15000 | 51 | 4520 | 200 | 200×1 | 35 | 15 | 100 | 61.2 | 273 | +73.4 | OVERPRICED@min-dps |
| `japan_tankbuster` | redalert_japan | 47000 | 48 | 5180 | 400 | 100×3 | 96 | 1 | 100 | 3.1 | 423 | +23.3 | OVERPRICED@min-dps |
| `ra1_soviets_flamethrower` | redalert_soviets | 16000 | 58 | 5020 | 200 | 600×1 | 24 | 1 | 100 | 25.0 | 297 | +96.8 | OVERPRICED@min-dps |
| `ra1_soviets_shocktrooper` | redalert_soviets | 40000 | 40 | 5000 | 600 | 20000×1 | 40 | 1 | 100 | 500.0 | 499 | -101.3 | anchor |
| `ra1_soviets_zapper` | redalert_soviets | 60000 | 41 | 4990 | 1200 | 48600×1 | 32 | 1 | 100 | 1518.8 | 1200 | +0.1 |  |
| `protoss_adept` | starcraft_protoss | 29000 | 58 | 5260 | 650 | 10700×2 | 40 | 1 | 100 | 535.0 | 650 | +0.0 |  |
| `terran_marauder` | starcraft_terran | 90000 | 53 | 4980 | 1000 | 700×1 | 0 | 1 | 100 | 0.0 | 682 | -318.0 |  |
| `td_gdi_sonicmissilesoldier` | tiberiandawn_gdi | 25000 | 55 | 5310 | 400 | 16900×1 | 125 | 1 | 100 | 135.2 | 400 | +0.0 |  |
| `td_nod_blackhandflamer` | tiberiandawn_nod | 36000 | 60 | 5010 | 600 | 2600×1 | 46 | 6 | 100 | 236.4 | 600 | +0.2 |  |
| `cabal_cyborgcommando` | tiberiansun_cabal | 100000 | 40 | 5230 | 5000 | 624900×1 | 90 | 1 | 100 | 6943.3 | 5000 | -0.3 |  |
| `cabal_cyborgcommandov2` | tiberiansun_cabal | 99000 | 45 | 5500 | 10000 | 1772600×1 | 90 | 1 | 100 | 19695.6 | 10000 | -0.1 |  |
| `cabal_cyborginfantry` | tiberiansun_cabal | 45000 | 50 | 5490 | 500 | 3500×2 | 60 | 1 | 100 | 116.7 | 501 | +0.6 |  |
| `cabal_devout` | tiberiansun_cabal | 77000 | 55 | 5050 | 1400 | 8400×2 | 45 | 2 | 100 | 700.0 | 1400 | -0.3 |  |
| `cabal_dissolver` | tiberiansun_cabal | 49000 | 60 | 5190 | 725 | 1300×1 | 4 | 1 | 100 | 325.0 | 725 | +0.0 |  |
| `cabal_enlighted` | tiberiansun_cabal | 78000 | 60 | 5150 | 1600 | 8100×3 | 25 | 1 | 100 | 972.0 | 1600 | +0.4 |  |
| `forgotten_tiberianfiend` | tiberiansun_forgotten | 79000 | 59 | 5110 | 1000 | 7400×1 | 36 | 3 | 100 | 555.0 | 1000 | -0.1 |  |
| `forgotten_tiberianfiend_wild` | tiberiansun_forgotten | 80000 | 59 | 5470 | 1000 | 4200×1 | 36 | 3 | 100 | 315.0 | 1001 | +0.6 |  |
| `forgotten_viniferafiend` | tiberiansun_forgotten | 98000 | 60 | 5380 | 2000 | 18000×1 | 36 | 3 | 100 | 1350.0 | 2000 | -0.1 |  |
| `ts_gdi_zonetrooper` | tiberiansun_gdi | 82000 | 59 | 5420 | 1500 | 79100×1 | 60 | 1 | 100 | 1318.3 | 1500 | +0.0 |  |
| `ts_nod_toxintrooper` | tiberiansun_nod | 31000 | 56 | 5240 | 850 | 29900×1 | 54 | 3 | 100 | 1495.0 | 850 | +0.1 |  |
| `wc2_humans_dwarvenrifleman` | warcraft2_humans | 24000 | 57 | 5400 | 600 | 10700×3 | 60 | 1 | 50 | 535.0 | 600 | -0.0 | fp-debt |

**Worst |Δ| among non-anchor members: 318.0** (goal ≤1).

## Uniqueness check (5 raw stats — soft/protected excluded)

- **Speed duplicates**: {59: ['ra2_soviets_desolator', 'forgotten_tiberianfiend', 'forgotten_tiberianfiend_wild', 'ts_gdi_zonetrooper'], 56: ['yuri_biotrooper', 'latinsyndicate_latinflametrooper', 'ts_nod_toxintrooper'], 53: ['asianalliance_asianflametrooper', 'tkm_juggernaut', 'terran_marauder'], 58: ['steelconsortium_quantummissiletrooper', 'ra1_soviets_flamethrower', 'protoss_adept'], 60: ['futuretech_cannondroid', 'schwarzermond_ubermensch', 'td_nod_blackhandflamer', 'cabal_dissolver', 'cabal_enlighted', 'forgotten_viniferafiend'], 57: ['naxis_naximachinegunners', 'wc2_humans_dwarvenrifleman'], 55: ['schwarzermond_noidmgarmor', 'td_gdi_sonicmissilesoldier', 'cabal_devout']}
- **raw ReloadDelay duplicates** (design choice — retune coarse Damage/reload by hand): {45: ['ixian_shockinfantry', 'ra2_soviets_desolator', 'cabal_devout'], 100: ['ra2_soviets_teslatrooper', 'schwarzermond_noidmgarmor'], 25: ['futuretech_cannondroid', 'cabal_enlighted'], 60: ['naxis_naxiflamer', 'cabal_cyborginfantry', 'ts_gdi_zonetrooper', 'wc2_humans_dwarvenrifleman'], 0: ['naxis_naximachinegunners', 'terran_marauder'], 90: ['cabal_cyborgcommando', 'cabal_cyborgcommandov2'], 36: ['forgotten_tiberianfiend', 'forgotten_tiberianfiend_wild', 'forgotten_viniferafiend']}

## Required YAML edits (per unit)

- `ixian_shockinfantry`: HP 34000, Speed 46, Range 5480, each offensive warhead Damage 51500 (×1 = SUM 51500), ReloadDelay 45, Burst 1
- `ixian_storminfantry`: HP 44000, Speed 44, Range 5430, each offensive warhead Damage 129100 (×1 = SUM 129100), ReloadDelay 66, Burst 1
- `ordos_chemicaltrooper`: HP 28000, Speed 52, Range 5170, each offensive warhead Damage 9600 (×1 = SUM 9600), ReloadDelay 75, Burst 1
- `ra2_soviets_desolator`: HP 30000, Speed 59, Range 5440, each offensive warhead Damage 44700 (×1 = SUM 44700), ReloadDelay 45, Burst 1
- `ra2_soviets_teslatrooper`: HP 48000, Speed 54, Range 5220, each offensive warhead Damage 2300 (×1 = SUM 2300), ReloadDelay 100, Burst 1
- `yuri_biotrooper`: HP 96000, Speed 56, Range 4770, each offensive warhead Damage 400 (×1 = SUM 400), ReloadDelay 50, Burst 6
- `asianalliance_asianflametrooper`: HP 26000, Speed 53, Range 4810, each offensive warhead Damage 1500 (×1 = SUM 1500), ReloadDelay 38, Burst 6, **DELETE the unconditional FirepowerMultiplier (50%)** — the Damage above already includes it (W17)
- `asianalliance_plasmatrooper`: HP 63000, Speed 42, Range 5120, each offensive warhead Damage 1300 (×8 = SUM 10400), ReloadDelay 34, Burst 1
- `steelconsortium_quantummissiletrooper`: HP 65000, Speed 58, Range 4850, each offensive warhead Damage 5900 (×4 = SUM 23600), ReloadDelay 48, Burst 2
- `futuretech_cannondroid`: HP 81000, Speed 60, Range 5360, each offensive warhead Damage 7300 (×1 = SUM 7300), ReloadDelay 25, Burst 1
- `naxis_naxiflamer`: HP 23000, Speed 49, Range 4720, each offensive warhead Damage 500 (×1 = SUM 500), ReloadDelay 60, Burst 3
- `naxis_naximachinegunners`: HP 59000, Speed 57, Range 5030, each offensive warhead Damage 100 (×1 = SUM 100), ReloadDelay 0, Burst 1, residual Δ -182.1 (cost pinned at 600)
- `naxis_panzerfausttrooper`: HP 35000, Speed 47, Range 5410, each offensive warhead Damage 5100 (×2 = SUM 10200), ReloadDelay 135, Burst 1
- `naxis_panzerschreck`: HP 95000, Speed 43, Range 5270, each offensive warhead Damage 11900 (×2 = SUM 23800), ReloadDelay 124, Burst 1
- `schwarzermond_noidmgarmor`: HP 50000, Speed 55, Range 4830, each offensive warhead Damage 200 (×6 = SUM 1200), ReloadDelay 100, Burst 5
- `schwarzermond_ubermensch`: HP 64000, Speed 60, Range 5140, each offensive warhead Damage 2300 (×3 = SUM 6900), ReloadDelay 30, Burst 2
- `latinsyndicate_latinflametrooper`: HP 55000, Speed 56, Range 5330, each offensive warhead Damage 8900 (×1 = SUM 8900), ReloadDelay 44, Burst 4
- `tkm_juggernaut`: HP 37000, Speed 53, Range 4500, each offensive warhead Damage 9900 (×1 = SUM 9900), ReloadDelay 8, Burst 1
- `japan_japaneseflamethrower`: HP 15000, Speed 51, Range 4520, each offensive warhead Damage 200 (×1 = SUM 200), ReloadDelay 35, Burst 15, residual Δ +73.4 (cost pinned at 200)
- `japan_tankbuster`: HP 47000, Speed 48, Range 5180, each offensive warhead Damage 100 (×3 = SUM 300), ReloadDelay 96, Burst 1, residual Δ +23.3 (cost pinned at 400)
- `ra1_soviets_flamethrower`: HP 16000, Speed 58, Range 5020, each offensive warhead Damage 600 (×1 = SUM 600), ReloadDelay 24, Burst 1, residual Δ +96.8 (cost pinned at 200)
- `ra1_soviets_zapper`: HP 60000, Speed 41, Range 4990, each offensive warhead Damage 48600 (×1 = SUM 48600), ReloadDelay 32, Burst 1
- `protoss_adept`: HP 29000, Speed 58, Range 5260, each offensive warhead Damage 10700 (×2 = SUM 21400), ReloadDelay 40, Burst 1
- `terran_marauder`: HP 90000, Speed 53, Range 4980, each offensive warhead Damage 700 (×1 = SUM 700), ReloadDelay 0, Burst 1, residual Δ -318.0 (cost pinned at 1000)
- `td_gdi_sonicmissilesoldier`: HP 25000, Speed 55, Range 5310, each offensive warhead Damage 16900 (×1 = SUM 16900), ReloadDelay 125, Burst 1
- `td_nod_blackhandflamer`: HP 36000, Speed 60, Range 5010, each offensive warhead Damage 2600 (×1 = SUM 2600), ReloadDelay 46, Burst 6
- `cabal_cyborgcommando`: HP 100000, Speed 40, Range 5230, each offensive warhead Damage 624900 (×1 = SUM 624900), ReloadDelay 90, Burst 1
- `cabal_cyborgcommandov2`: HP 99000, Speed 45, Range 5500, each offensive warhead Damage 1772600 (×1 = SUM 1772600), ReloadDelay 90, Burst 1
- `cabal_cyborginfantry`: HP 45000, Speed 50, Range 5490, each offensive warhead Damage 3500 (×2 = SUM 7000), ReloadDelay 60, Burst 1
- `cabal_devout`: HP 77000, Speed 55, Range 5050, each offensive warhead Damage 8400 (×2 = SUM 16800), ReloadDelay 45, Burst 2
- `cabal_dissolver`: HP 49000, Speed 60, Range 5190, each offensive warhead Damage 1300 (×1 = SUM 1300), ReloadDelay 4, Burst 1
- `cabal_enlighted`: HP 78000, Speed 60, Range 5150, each offensive warhead Damage 8100 (×3 = SUM 24300), ReloadDelay 25, Burst 1
- `forgotten_tiberianfiend`: HP 79000, Speed 59, Range 5110, each offensive warhead Damage 7400 (×1 = SUM 7400), ReloadDelay 36, Burst 3
- `forgotten_tiberianfiend_wild`: HP 80000, Speed 59, Range 5470, each offensive warhead Damage 4200 (×1 = SUM 4200), ReloadDelay 36, Burst 3
- `forgotten_viniferafiend`: HP 98000, Speed 60, Range 5380, each offensive warhead Damage 18000 (×1 = SUM 18000), ReloadDelay 36, Burst 3
- `ts_gdi_zonetrooper`: HP 82000, Speed 59, Range 5420, each offensive warhead Damage 79100 (×1 = SUM 79100), ReloadDelay 60, Burst 1
- `ts_nod_toxintrooper`: HP 31000, Speed 56, Range 5240, each offensive warhead Damage 29900 (×1 = SUM 29900), ReloadDelay 54, Burst 3
- `wc2_humans_dwarvenrifleman`: HP 24000, Speed 57, Range 5400, each offensive warhead Damage 10700 (×3 = SUM 32100), ReloadDelay 60, Burst 1, **DELETE the unconditional FirepowerMultiplier (50%)** — the Damage above already includes it (W17)
