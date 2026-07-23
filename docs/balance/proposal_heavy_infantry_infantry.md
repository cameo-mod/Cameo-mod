# Heavy Infantry infantry rebalance proposal

Anchor spec: HP=50000, Speed=50, Range=5000, eff-DPS=1000, Cost=800

Converter law: cost pinned, range clamped to band + made unique, eff-DPS trimmed to Δ≤1 via 2000-grid warhead Damage × 1% FirepowerMultiplier.

| actor | faction | HP | spd | rng | cost | dmg/wh×n | rl | burst | FP% | eff DPS | price | Δ | flags |
|---|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|---|
| `ixian_shockinfantry` | d2k_ixian | 34000 | 46 | 5450 | 500 | 22000×1 | 45 | 1 | 98 | 598.9 | 499 | -1.4 |  |
| `ixian_storminfantry` | d2k_ixian | 44000 | 44 | 5440 | 800 | 34000×2 | 66 | 1 | 100 | 1159.1 | 799 | -1.0 |  |
| `ordos_chemicaltrooper` | d2k_ordos | 28000 | 52 | 5180 | 400 | 40000×1 | 75 | 1 | 100 | 400.0 | 400 | -0.3 |  |
| `ra2_soviets_desolator` | redalert2_soviets | 30000 | 59 | 5400 | 700 | 70000×1 | 45 | 1 | 99 | 1540.0 | 693 | -7.4 |  |
| `ra2_soviets_teslatrooper` | redalert2_soviets | 48000 | 41 | 5230 | 500 | 36000×1 | 100 | 1 | 102 | 367.2 | 500 | +0.1 |  |
| `yuri_biotrooper` | redalert2_yuri | 96000 | 56 | 4820 | 400 | 2000×1 | 50 | 6 | 146 | 250.3 | 400 | -0.2 |  |
| `asianalliance_asianflametrooper` | redalert2mod_asianalliance | 26000 | 53 | 4550 | 400 | 6000×1 | 38 | 6 | 98 | 499.2 | 400 | +0.1 |  |
| `asianalliance_plasmatrooper` | redalert2mod_asianalliance | 63000 | 42 | 5210 | 500 | 2000×8 | 34 | 1 | 135 | 635.3 | 501 | +1.0 |  |
| `steelconsortium_quantummissiletrooper` | redalert2mod_consortium | 65000 | 54 | 5380 | 1150 | 8000×4 | 48 | 2 | 109 | 1453.3 | 1144 | -6.3 |  |
| `futuretech_cannondroid` | redalert2mod_futuretech | 81000 | 60 | 4920 | 525 | 2000×4 | 25 | 1 | 5 | 17.3 | 525 | +0.1 |  |
| `naxis_naxiflamer` | redalert2mod_naxis | 23000 | 49 | 4840 | 225 | 2000×1 | 60 | 3 | 21 | 14.3 | 225 | +0.1 |  |
| `naxis_naximachinegunners` | redalert2mod_naxis | 61000 | 57 | 5020 | 600 | 2000×1 | 0 | 1 | 5 | 0.0 | 320 | -280.2 |  |
| `naxis_panzerfausttrooper` | redalert2mod_naxis | 35000 | 47 | 5420 | 400 | 6000×6 | 135 | 1 | 110 | 322.7 | 399 | -1.1 |  |
| `naxis_panzerschreck` | redalert2mod_naxis | 95000 | 43 | 5410 | 600 | 8000×6 | 124 | 1 | 112 | 476.9 | 600 | -0.4 |  |
| `schwarzermond_noidmgarmor` | redalert2mod_schwarzermond | 50000 | 55 | 4530 | 500 | 2000×5 | 100 | 5 | 81 | 337.5 | 500 | +0.0 |  |
| `schwarzermond_ubermensch` | redalert2mod_schwarzermond | 64000 | 60 | 5390 | 700 | 6000×2 | 30 | 2 | 103 | 749.1 | 698 | -2.2 |  |
| `latinsyndicate_latinflametrooper` | redalert2mod_syndicate | 55000 | 56 | 5430 | 500 | 2000×3 | 44 | 4 | 47 | 201.4 | 499 | -0.9 | shared-wpn? |
| `tkm_juggernaut` | redalert2mod_tkm | 37000 | 53 | 4510 | 650 | 8000×1 | 8 | 1 | 97 | 970.0 | 649 | -0.9 |  |
| `japan_japaneseflamethrower` | redalert_japan | 15000 | 51 | 4540 | 200 | 2000×1 | 35 | 15 | 9 | 41.3 | 200 | +0.2 |  |
| `japan_tankbuster` | redalert_japan | 47000 | 48 | 5190 | 400 | 6000×3 | 96 | 1 | 87 | 183.5 | 400 | -0.3 |  |
| `ra1_soviets_flamethrower` | redalert_soviets | 16000 | 58 | 5010 | 200 | 2000×1 | 24 | 1 | 6 | 5.0 | 216 | +16.5 | OVERPRICED@min-dps |
| `ra1_soviets_shocktrooper` | redalert_soviets | 40000 | 40 | 5000 | 600 | 20000×1 | 40 | 1 | 100 | 500.0 | 374 | -226.0 | anchor |
| `ra1_soviets_zapper` | redalert_soviets | 60000 | 30 | 5000 | 1200 | 32000×1 | 32 | 1 | 100 | 1000.0 | 580 | -620.5 | verifier |
| `protoss_adept` | starcraft_protoss | 29000 | 58 | 5260 | 650 | 16000×2 | 40 | 1 | 105 | 945.0 | 650 | -0.2 |  |
| `terran_marauder` | starcraft_terran | 90000 | 53 | 4990 | 1000 | 2000×1 | 0 | 1 | 7 | 0.0 | 512 | -488.4 |  |
| `td_gdi_sonicmissilesoldier` | tiberiandawn_gdi | 25000 | 55 | 5370 | 400 | 10000×5 | 125 | 1 | 94 | 399.5 | 398 | -1.6 |  |
| `td_nod_blackhandflamer` | tiberiandawn_nod | 36000 | 60 | 4960 | 600 | 6000×1 | 46 | 6 | 97 | 529.1 | 600 | -0.2 |  |
| `cabal_cyborgcommando` | tiberiansun_cabal | 250000 | 40 | 5220 | 5000 | 118000×3 | 90 | 1 | 101 | 4303.7 | 5001 | +1.2 |  |
| `cabal_cyborgcommandov2` | tiberiansun_cabal | 400000 | 45 | 5500 | 10000 | 218000×3 | 90 | 1 | 100 | 7872.2 | 9966 | -33.9 |  |
| `cabal_cyborginfantry` | tiberiansun_cabal | 45000 | 50 | 5490 | 500 | 12000×2 | 60 | 1 | 108 | 378.0 | 500 | +0.0 |  |
| `cabal_devout` | tiberiansun_cabal | 77000 | 55 | 5490 | 1400 | 14000×2 | 45 | 2 | 102 | 1041.2 | 1399 | -1.1 |  |
| `cabal_dissolver` | tiberiansun_cabal | 49000 | 60 | 5130 | 725 | 2000×2 | 4 | 1 | 90 | 675.0 | 725 | +0.1 |  |
| `cabal_enlighted` | tiberiansun_cabal | 78000 | 60 | 5120 | 1600 | 10000×4 | 25 | 1 | 96 | 1536.0 | 1599 | -0.8 |  |
| `forgotten_tiberianfiend` | tiberiansun_forgotten | 79000 | 59 | 5470 | 1000 | 4000×2 | 36 | 3 | 124 | 651.0 | 998 | -2.0 |  |
| `forgotten_tiberianfiend_wild` | tiberiansun_forgotten | 80000 | 59 | 5470 | 1000 | 4000×2 | 36 | 3 | 122 | 640.5 | 999 | -1.1 |  |
| `forgotten_viniferafiend` | tiberiansun_forgotten | 100000 | 60 | 5450 | 2000 | 6000×4 | 36 | 3 | 91 | 1535.6 | 2003 | +2.8 |  |
| `ts_gdi_zonetrooper` | tiberiansun_gdi | 82000 | 59 | 5360 | 1500 | 62000×1 | 60 | 1 | 99 | 1278.7 | 1493 | -7.1 |  |
| `ts_nod_toxintrooper` | tiberiansun_nod | 31000 | 58 | 5350 | 850 | 36000×1 | 54 | 3 | 100 | 1350.0 | 846 | -4.5 |  |
| `wc2_humans_dwarvenrifleman` | warcraft2_humans | 24000 | 57 | 5340 | 600 | 16000×3 | 60 | 1 | 105 | 945.0 | 597 | -3.0 |  |

**Worst |Δ| among non-anchor members: 488.4** (goal ≤1).

## Uniqueness check (5 raw stats — soft/protected excluded)

- **Speed duplicates**: {59: ['ra2_soviets_desolator', 'forgotten_tiberianfiend', 'forgotten_tiberianfiend_wild', 'ts_gdi_zonetrooper'], 56: ['yuri_biotrooper', 'latinsyndicate_latinflametrooper'], 53: ['asianalliance_asianflametrooper', 'tkm_juggernaut', 'terran_marauder'], 60: ['futuretech_cannondroid', 'schwarzermond_ubermensch', 'td_nod_blackhandflamer', 'cabal_dissolver', 'cabal_enlighted', 'forgotten_viniferafiend'], 57: ['naxis_naximachinegunners', 'wc2_humans_dwarvenrifleman'], 55: ['schwarzermond_noidmgarmor', 'td_gdi_sonicmissilesoldier', 'cabal_devout'], 58: ['ra1_soviets_flamethrower', 'protoss_adept', 'ts_nod_toxintrooper']}
- **Range duplicates**: {5450: ['ixian_shockinfantry', 'forgotten_viniferafiend'], 5490: ['cabal_cyborginfantry', 'cabal_devout'], 5470: ['forgotten_tiberianfiend', 'forgotten_tiberianfiend_wild']}
- **raw ReloadDelay duplicates** (design choice — retune coarse Damage/reload by hand): {45: ['ixian_shockinfantry', 'ra2_soviets_desolator', 'cabal_devout'], 100: ['ra2_soviets_teslatrooper', 'schwarzermond_noidmgarmor'], 25: ['futuretech_cannondroid', 'cabal_enlighted'], 60: ['naxis_naxiflamer', 'cabal_cyborginfantry', 'ts_gdi_zonetrooper', 'wc2_humans_dwarvenrifleman'], 0: ['naxis_naximachinegunners', 'terran_marauder'], 90: ['cabal_cyborgcommando', 'cabal_cyborgcommandov2'], 36: ['forgotten_tiberianfiend', 'forgotten_tiberianfiend_wild', 'forgotten_viniferafiend']}

## Required YAML edits (per unit)

- `ixian_shockinfantry`: HP 34000, Speed 46, Range 5450, each offensive warhead Damage 22000 (×1 = SUM 22000), ReloadDelay 45, Burst 1, FirepowerMultiplier@IXIANSHOCKINFANTRY 98, residual Δ -1.4 (cost pinned at 500)
- `ixian_storminfantry`: HP 44000, Speed 44, Range 5440, each offensive warhead Damage 34000 (×2 = SUM 68000), ReloadDelay 66, Burst 1, FirepowerMultiplier@IXIANSTORMINFANTRY 100, residual Δ -1.0 (cost pinned at 800)
- `ordos_chemicaltrooper`: HP 28000, Speed 52, Range 5180, each offensive warhead Damage 40000 (×1 = SUM 40000), ReloadDelay 75, Burst 1, FirepowerMultiplier@ORDOSCHEMICALTROOPER 100
- `ra2_soviets_desolator`: HP 30000, Speed 59, Range 5400, each offensive warhead Damage 70000 (×1 = SUM 70000), ReloadDelay 45, Burst 1, FirepowerMultiplier@RA2SOVIETSDESOLATOR 99, residual Δ -7.4 (cost pinned at 700)
- `ra2_soviets_teslatrooper`: HP 48000, Speed 41, Range 5230, each offensive warhead Damage 36000 (×1 = SUM 36000), ReloadDelay 100, Burst 1, FirepowerMultiplier@RA2SOVIETSTESLATROOPER 102
- `yuri_biotrooper`: HP 96000, Speed 56, Range 4820, each offensive warhead Damage 2000 (×1 = SUM 2000), ReloadDelay 50, Burst 6, FirepowerMultiplier@YURIBIOTROOPER 146
- `asianalliance_asianflametrooper`: HP 26000, Speed 53, Range 4550, each offensive warhead Damage 6000 (×1 = SUM 6000), ReloadDelay 38, Burst 6, FirepowerMultiplier@ASIANALLIANCEASIANFLAMETROOPER 98
- `asianalliance_plasmatrooper`: HP 63000, Speed 42, Range 5210, each offensive warhead Damage 2000 (×8 = SUM 16000), ReloadDelay 34, Burst 1, FirepowerMultiplier@ASIANALLIANCEPLASMATROOPER 135, residual Δ +1.0 (cost pinned at 500)
- `steelconsortium_quantummissiletrooper`: HP 65000, Speed 54, Range 5380, each offensive warhead Damage 8000 (×4 = SUM 32000), ReloadDelay 48, Burst 2, FirepowerMultiplier@STEELCONSORTIUMQUANTUMMISSILETROOPER 109, residual Δ -6.3 (cost pinned at 1150)
- `futuretech_cannondroid`: HP 81000, Speed 60, Range 4920, each offensive warhead Damage 2000 (×4 = SUM 8000), ReloadDelay 25, Burst 1, FirepowerMultiplier@FUTURETECHCANNONDROID 5
- `naxis_naxiflamer`: HP 23000, Speed 49, Range 4840, each offensive warhead Damage 2000 (×1 = SUM 2000), ReloadDelay 60, Burst 3, FirepowerMultiplier@NAXISNAXIFLAMER 21
- `naxis_naximachinegunners`: HP 61000, Speed 57, Range 5020, each offensive warhead Damage 2000 (×1 = SUM 2000), ReloadDelay 0, Burst 1, FirepowerMultiplier@NAXISNAXIMACHINEGUNNERS 5, residual Δ -280.2 (cost pinned at 600)
- `naxis_panzerfausttrooper`: HP 35000, Speed 47, Range 5420, each offensive warhead Damage 6000 (×6 = SUM 36000), ReloadDelay 135, Burst 1, FirepowerMultiplier@NAXISPANZERFAUSTTROOPER 110, residual Δ -1.1 (cost pinned at 400)
- `naxis_panzerschreck`: HP 95000, Speed 43, Range 5410, each offensive warhead Damage 8000 (×6 = SUM 48000), ReloadDelay 124, Burst 1, FirepowerMultiplier@NAXISPANZERSCHRECK 112
- `schwarzermond_noidmgarmor`: HP 50000, Speed 55, Range 4530, each offensive warhead Damage 2000 (×5 = SUM 10000), ReloadDelay 100, Burst 5, FirepowerMultiplier@SCHWARZERMONDNOIDMGARMOR 81
- `schwarzermond_ubermensch`: HP 64000, Speed 60, Range 5390, each offensive warhead Damage 6000 (×2 = SUM 12000), ReloadDelay 30, Burst 2, FirepowerMultiplier@SCHWARZERMONDUBERMENSCH 103, residual Δ -2.2 (cost pinned at 700)
- `latinsyndicate_latinflametrooper`: HP 55000, Speed 56, Range 5430, each offensive warhead Damage 2000 (×3 = SUM 6000), ReloadDelay 44, Burst 4, FirepowerMultiplier@LATINSYNDICATELATINFLAMETROOPER 47
- `tkm_juggernaut`: HP 37000, Speed 53, Range 4510, each offensive warhead Damage 8000 (×1 = SUM 8000), ReloadDelay 8, Burst 1, FirepowerMultiplier@TKMJUGGERNAUT 97
- `japan_japaneseflamethrower`: HP 15000, Speed 51, Range 4540, each offensive warhead Damage 2000 (×1 = SUM 2000), ReloadDelay 35, Burst 15, FirepowerMultiplier@JAPANJAPANESEFLAMETHROWER 9
- `japan_tankbuster`: HP 47000, Speed 48, Range 5190, each offensive warhead Damage 6000 (×3 = SUM 18000), ReloadDelay 96, Burst 1, FirepowerMultiplier@JAPANTANKBUSTER 87
- `ra1_soviets_flamethrower`: HP 16000, Speed 58, Range 5010, each offensive warhead Damage 2000 (×1 = SUM 2000), ReloadDelay 24, Burst 1, FirepowerMultiplier@RA1SOVIETSFLAMETHROWER 6, residual Δ +16.5 (cost pinned at 200)
- `protoss_adept`: HP 29000, Speed 58, Range 5260, each offensive warhead Damage 16000 (×2 = SUM 32000), ReloadDelay 40, Burst 1, FirepowerMultiplier@PROTOSSADEPT 105
- `terran_marauder`: HP 90000, Speed 53, Range 4990, each offensive warhead Damage 2000 (×1 = SUM 2000), ReloadDelay 0, Burst 1, FirepowerMultiplier@TERRANMARAUDER 7, residual Δ -488.4 (cost pinned at 1000)
- `td_gdi_sonicmissilesoldier`: HP 25000, Speed 55, Range 5370, each offensive warhead Damage 10000 (×5 = SUM 50000), ReloadDelay 125, Burst 1, FirepowerMultiplier@TDGDISONICMISSILESOLDIER 94, residual Δ -1.6 (cost pinned at 400)
- `td_nod_blackhandflamer`: HP 36000, Speed 60, Range 4960, each offensive warhead Damage 6000 (×1 = SUM 6000), ReloadDelay 46, Burst 6, FirepowerMultiplier@TDNODBLACKHANDFLAMER 97
- `cabal_cyborgcommando`: HP 250000, Speed 40, Range 5220, each offensive warhead Damage 118000 (×3 = SUM 354000), ReloadDelay 90, Burst 1, FirepowerMultiplier@CABALCYBORGCOMMANDO 101, residual Δ +1.2 (cost pinned at 5000)
- `cabal_cyborgcommandov2`: HP 400000, Speed 45, Range 5500, each offensive warhead Damage 218000 (×3 = SUM 654000), ReloadDelay 90, Burst 1, FirepowerMultiplier@CABALCYBORGCOMMANDOV2 100, residual Δ -33.9 (cost pinned at 10000)
- `cabal_cyborginfantry`: HP 45000, Speed 50, Range 5490, each offensive warhead Damage 12000 (×2 = SUM 24000), ReloadDelay 60, Burst 1, FirepowerMultiplier@CABALCYBORGINFANTRY 108
- `cabal_devout`: HP 77000, Speed 55, Range 5490, each offensive warhead Damage 14000 (×2 = SUM 28000), ReloadDelay 45, Burst 2, FirepowerMultiplier@CABALDEVOUT 102, residual Δ -1.1 (cost pinned at 1400)
- `cabal_dissolver`: HP 49000, Speed 60, Range 5130, each offensive warhead Damage 2000 (×2 = SUM 4000), ReloadDelay 4, Burst 1, FirepowerMultiplier@CABALDISSOLVER 90
- `cabal_enlighted`: HP 78000, Speed 60, Range 5120, each offensive warhead Damage 10000 (×4 = SUM 40000), ReloadDelay 25, Burst 1, FirepowerMultiplier@CABALENLIGHTED 96
- `forgotten_tiberianfiend`: HP 79000, Speed 59, Range 5470, each offensive warhead Damage 4000 (×2 = SUM 8000), ReloadDelay 36, Burst 3, FirepowerMultiplier@FORGOTTENTIBERIANFIEND 124, residual Δ -2.0 (cost pinned at 1000)
- `forgotten_tiberianfiend_wild`: HP 80000, Speed 59, Range 5470, each offensive warhead Damage 4000 (×2 = SUM 8000), ReloadDelay 36, Burst 3, FirepowerMultiplier@FORGOTTENTIBERIANFIENDWILD 122, residual Δ -1.1 (cost pinned at 1000)
- `forgotten_viniferafiend`: HP 100000, Speed 60, Range 5450, each offensive warhead Damage 6000 (×4 = SUM 24000), ReloadDelay 36, Burst 3, FirepowerMultiplier@FORGOTTENVINIFERAFIEND 91, residual Δ +2.8 (cost pinned at 2000)
- `ts_gdi_zonetrooper`: HP 82000, Speed 59, Range 5360, each offensive warhead Damage 62000 (×1 = SUM 62000), ReloadDelay 60, Burst 1, FirepowerMultiplier@TSGDIZONETROOPER 99, residual Δ -7.1 (cost pinned at 1500)
- `ts_nod_toxintrooper`: HP 31000, Speed 58, Range 5350, each offensive warhead Damage 36000 (×1 = SUM 36000), ReloadDelay 54, Burst 3, FirepowerMultiplier@TSNODTOXINTROOPER 100, residual Δ -4.5 (cost pinned at 850)
- `wc2_humans_dwarvenrifleman`: HP 24000, Speed 57, Range 5340, each offensive warhead Damage 16000 (×3 = SUM 48000), ReloadDelay 60, Burst 1, FirepowerMultiplier@WC2HUMANSDWARVENRIFLEMAN 105, residual Δ -3.0 (cost pinned at 600)
