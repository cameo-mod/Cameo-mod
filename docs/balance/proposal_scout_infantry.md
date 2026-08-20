# Scout infantry rebalance proposal

Anchor spec: HP=20000, Speed=60, Range=5000, eff-DPS=60, Cost=100

Converter law: cost pinned, range clamped to band + made unique, eff-DPS trimmed to Δ≤1 via 2000-grid warhead Damage × 1% FirepowerMultiplier.

| actor | faction | HP | spd | rng | cost | dmg/wh×n | rl | burst | FP% | eff DPS | price | Δ | flags |
|---|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|---|
| `ixian_lightinfantry` | d2k_ixian | 32000 | 56 | 5450 | 150 | 2000×2 | 20 | 1 | 47 | 70.5 | 150 | -0.0 | shared-wpn? |
| `ordos_lightinfantry` | d2k_ordos | 31000 | 62 | 5480 | 120 | 2000×2 | 20 | 1 | 29 | 43.5 | 119 | -0.9 | shared-wpn? |
| `ra2_allies_gi` | redalert2_allies | 50000 | 50 | 4630 | 200 | 2000×1 | 15 | 3 | 30 | 71.1 | 200 | +0.0 |  |
| `ra2_soviets_conscript` | redalert2_soviets | 26000 | 58 | 4900 | 100 | 2000×1 | 18 | 1 | 57 | 47.5 | 100 | +0.0 |  |
| `asianalliance_asianmilitia` | redalert2mod_asianalliance | 28000 | 53 | 4560 | 110 | 4000×1 | 50 | 1 | 103 | 61.8 | 110 | +0.2 |  |
| `futuretech_scoutdroid` | redalert2mod_futuretech | 33000 | 70 | 5500 | 200 | 2000×2 | 40 | 4 | 27 | 82.2 | 198 | -2.1 |  |
| `naxis_coneheadsknights` | redalert2mod_naxis | 22000 | 72 | 4540 | 1000 | 22000×1 | 18 | 1 | 98 | 898.3 | 998 | -1.7 | shared-wpn? |
| `naxis_naxiriflerecruit` | redalert2mod_naxis | 21000 | 48 | 5250 | 75 | 6000×1 | 100 | 1 | 89 | 40.0 | 75 | +0.0 |  |
| `naxis_naxiriflesoldier` | redalert2mod_naxis | 20000 | 60 | 5000 | 100 | 4000×1 | 50 | 1 | 100 | 60.0 | 100 | +0.0 | anchor |
| `undead.nax` | redalert2mod_naxis | 14000 | 52 | 5430 | 100 | 8000×1 | 75 | 1 | 105 | 84.0 | 100 | -0.0 | soft shared-wpn? |
| `latinsyndicate_latinmilitia` | redalert2mod_syndicate | 29000 | 51 | 5400 | 130 | 2000×1 | 22 | 3 | 46 | 69.0 | 130 | +0.0 |  |
| `tkm_marine` | redalert2mod_tkm | 18000 | 60 | 5390 | 300 | 2000×1 | 16 | 5 | 71 | 266.2 | 300 | +0.0 |  |
| `tkm_rifleman` | redalert2mod_tkm | 23000 | 61 | 5040 | 120 | 6000×1 | 75 | 1 | 115 | 69.0 | 120 | +0.0 |  |
| `ra1_soviets_ak47conscript` | redalert_soviets | 43000 | 71 | 4740 | 200 | 2000×2 | 11 | 3 | 10 | 70.0 | 200 | +0.0 |  |
| `ra1_soviets_rifleinfantry` | redalert_soviets | 34000 | 54 | 4700 | 100 | 2000×1 | 50 | 3 | 50 | 37.5 | 100 | -0.0 |  |
| `ra1_allies_rifleinfantry` | shared_redalert | 30000 | 55 | 5490 | 100 | 2000×1 | 50 | 3 | 48 | 37.2 | 100 | -0.0 |  |
| `zerg_spithid` | starcraft_zerg | 39000 | 72 | 4500 | 300 | 2000×3 | 15 | 1 | 49 | 147.0 | 302 | +1.9 |  |
| `td_gdi_minigunner` | tiberiandawn_gdi | 25000 | 63 | 5410 | 100 | 2000×1 | 50 | 4 | 40 | 40.7 | 100 | +0.0 |  |
| `td_nod_minigunner` | tiberiandawn_nod | 24000 | 67 | 4580 | 100 | 2000×1 | 50 | 4 | 44 | 47.1 | 100 | -0.0 |  |
| `forgotten_mutant` | tiberiansun_forgotten | 46000 | 65 | 5240 | 160 | 2000×1 | 18 | 2 | 26 | 43.3 | 160 | -0.0 | shared-wpn? |
| `forgotten_mutant_wild` | tiberiansun_forgotten | 44000 | 66 | 5280 | 160 | 2000×1 | 18 | 2 | 27 | 45.0 | 160 | -0.0 | shared-wpn? |
| `forgotten_mutantsoldier` | tiberiansun_forgotten | 40000 | 60 | 5000 | 250 | 8000×1 | 50 | 1 | 100 | 120.0 | 250 | +0.0 | verifier |
| `ts_gdi_lightinfantry` | tiberiansun_gdi | 17000 | 64 | 4520 | 120 | 2000×1 | 12 | 1 | 77 | 96.2 | 120 | -0.0 | shared-wpn? |
| `ts_nod_lightinfantry` | tiberiansun_nod | 15000 | 59 | 4530 | 120 | 2000×1 | 12 | 1 | 90 | 112.5 | 120 | -0.0 | shared-wpn? |

**Worst |Δ| among non-anchor members: 2.1** (goal ≤1).

## Uniqueness check (5 raw stats — soft/protected excluded)

- **Speed duplicates**: {72: ['naxis_coneheadsknights', 'zerg_spithid']}
- **raw ReloadDelay duplicates** (design choice — retune coarse Damage/reload by hand): {20: ['ixian_lightinfantry', 'ordos_lightinfantry'], 15: ['ra2_allies_gi', 'zerg_spithid'], 18: ['ra2_soviets_conscript', 'naxis_coneheadsknights', 'forgotten_mutant', 'forgotten_mutant_wild'], 50: ['asianalliance_asianmilitia', 'ra1_soviets_rifleinfantry', 'ra1_allies_rifleinfantry', 'td_gdi_minigunner', 'td_nod_minigunner'], 12: ['ts_gdi_lightinfantry', 'ts_nod_lightinfantry']}

## Required YAML edits (per unit)

- `ixian_lightinfantry`: HP 32000, Speed 56, Range 5450, each offensive warhead Damage 2000 (×2 = SUM 4000), ReloadDelay 20, Burst 1, FirepowerMultiplier@IXIANLIGHTINFANTRY 47
- `ordos_lightinfantry`: HP 31000, Speed 62, Range 5480, each offensive warhead Damage 2000 (×2 = SUM 4000), ReloadDelay 20, Burst 1, FirepowerMultiplier@ORDOSLIGHTINFANTRY 29
- `ra2_allies_gi`: HP 50000, Speed 50, Range 4630, each offensive warhead Damage 2000 (×1 = SUM 2000), ReloadDelay 15, Burst 3, FirepowerMultiplier@RA2ALLIESGI 30
- `ra2_soviets_conscript`: HP 26000, Speed 58, Range 4900, each offensive warhead Damage 2000 (×1 = SUM 2000), ReloadDelay 18, Burst 1, FirepowerMultiplier@RA2SOVIETSCONSCRIPT 57
- `asianalliance_asianmilitia`: HP 28000, Speed 53, Range 4560, each offensive warhead Damage 4000 (×1 = SUM 4000), ReloadDelay 50, Burst 1, FirepowerMultiplier@ASIANALLIANCEASIANMILITIA 103
- `futuretech_scoutdroid`: HP 33000, Speed 70, Range 5500, each offensive warhead Damage 2000 (×2 = SUM 4000), ReloadDelay 40, Burst 4, FirepowerMultiplier@FUTURETECHSCOUTDROID 27, residual Δ -2.1 (cost pinned at 200)
- `naxis_coneheadsknights`: HP 22000, Speed 72, Range 4540, each offensive warhead Damage 22000 (×1 = SUM 22000), ReloadDelay 18, Burst 1, FirepowerMultiplier@NAXISCONEHEADSKNIGHTS 98, residual Δ -1.7 (cost pinned at 1000)
- `naxis_naxiriflerecruit`: HP 21000, Speed 48, Range 5250, each offensive warhead Damage 6000 (×1 = SUM 6000), ReloadDelay 100, Burst 1, FirepowerMultiplier@NAXISNAXIRIFLERECRUIT 89
- `undead.nax`: HP 14000, Speed 52, Range 5430, each offensive warhead Damage 8000 (×1 = SUM 8000), ReloadDelay 75, Burst 1, FirepowerMultiplier@UNDEAD.NAX 105
- `latinsyndicate_latinmilitia`: HP 29000, Speed 51, Range 5400, each offensive warhead Damage 2000 (×1 = SUM 2000), ReloadDelay 22, Burst 3, FirepowerMultiplier@LATINSYNDICATELATINMILITIA 46
- `tkm_marine`: HP 18000, Speed 60, Range 5390, each offensive warhead Damage 2000 (×1 = SUM 2000), ReloadDelay 16, Burst 5, FirepowerMultiplier@TKMMARINE 71
- `tkm_rifleman`: HP 23000, Speed 61, Range 5040, each offensive warhead Damage 6000 (×1 = SUM 6000), ReloadDelay 75, Burst 1, FirepowerMultiplier@TKMRIFLEMAN 115
- `ra1_soviets_ak47conscript`: HP 43000, Speed 71, Range 4740, each offensive warhead Damage 2000 (×2 = SUM 4000), ReloadDelay 11, Burst 3, FirepowerMultiplier@RA1SOVIETSAK47CONSCRIPT 10
- `ra1_soviets_rifleinfantry`: HP 34000, Speed 54, Range 4700, each offensive warhead Damage 2000 (×1 = SUM 2000), ReloadDelay 50, Burst 3, FirepowerMultiplier@RA1SOVIETSRIFLEINFANTRY 50
- `ra1_allies_rifleinfantry`: HP 30000, Speed 55, Range 5490, each offensive warhead Damage 2000 (×1 = SUM 2000), ReloadDelay 50, Burst 3, FirepowerMultiplier@RA1ALLIESRIFLEINFANTRY 48
- `zerg_spithid`: HP 39000, Speed 72, Range 4500, each offensive warhead Damage 2000 (×3 = SUM 6000), ReloadDelay 15, Burst 1, FirepowerMultiplier@ZERGSPITHID 49, residual Δ +1.9 (cost pinned at 300)
- `td_gdi_minigunner`: HP 25000, Speed 63, Range 5410, each offensive warhead Damage 2000 (×1 = SUM 2000), ReloadDelay 50, Burst 4, FirepowerMultiplier@TDGDIMINIGUNNER 40
- `td_nod_minigunner`: HP 24000, Speed 67, Range 4580, each offensive warhead Damage 2000 (×1 = SUM 2000), ReloadDelay 50, Burst 4, FirepowerMultiplier@TDNODMINIGUNNER 44
- `forgotten_mutant`: HP 46000, Speed 65, Range 5240, each offensive warhead Damage 2000 (×1 = SUM 2000), ReloadDelay 18, Burst 2, FirepowerMultiplier@FORGOTTENMUTANT 26
- `forgotten_mutant_wild`: HP 44000, Speed 66, Range 5280, each offensive warhead Damage 2000 (×1 = SUM 2000), ReloadDelay 18, Burst 2, FirepowerMultiplier@FORGOTTENMUTANTWILD 27
- `ts_gdi_lightinfantry`: HP 17000, Speed 64, Range 4520, each offensive warhead Damage 2000 (×1 = SUM 2000), ReloadDelay 12, Burst 1, FirepowerMultiplier@TSGDILIGHTINFANTRY 77
- `ts_nod_lightinfantry`: HP 15000, Speed 59, Range 4530, each offensive warhead Damage 2000 (×1 = SUM 2000), ReloadDelay 12, Burst 1, FirepowerMultiplier@TSNODLIGHTINFANTRY 90
