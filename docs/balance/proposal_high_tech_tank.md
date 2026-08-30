# High Tech Tank infantry rebalance proposal

Anchor spec: HP=700000, Speed=65, Range=6500, eff-DPS=2000, Cost=2000

Converter law: cost pinned, range clamped to band + made unique, eff-DPS trimmed to Δ≤1 via 100-grid warhead Damage; unconditional FirepowerMultiplier is retired.

Uniqueness separates **raw warhead Damage (the 5-stat law as written)**. The levers run coarsest-first — Damage (100 grid) → Speed (1) → Range (10) — because one Damage step is a whole shot and running it last threw away everything the fine levers had achieved.

| actor | faction | HP | spd | rng | cost | dmg/wh×n | rl | burst | legacy FP% | eff DPS | price | Δ | flags |
|---|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|---|
| `duelist_tank.ixian` | d2k_ixian | 100000 | 55 | 7010 | 1800 | 107700×6 | 140 | 1 | 50 | 4615.7 | 1800 | -0.3 | fp-debt |
| `ordos_deviatortank` | d2k_ordos | 80000 | 75 | 7070 | 1600 | 148200×2 | 98 | 1 | 100 | 3024.5 | 1600 | +0.1 |  |
| `ordos_lasertank` | d2k_ordos | 77500 | 78 | 7280 | 2400 | 551600×1 | 55 | 1 | 100 | 10029.1 | 2400 | -0.1 |  |
| `ra2_allies_heavymiragetank` | redalert2_allies | 100000 | 60 | 6110 | 2000 | 294000×1 | 51 | 1 | 100 | 5764.7 | 2000 | -0.0 |  |
| `ra2_allies_miragetank` | redalert2_allies | 95000 | 65 | 6030 | 1600 | 168700×1 | 39 | 1 | 100 | 4325.6 | 1600 | -0.0 |  |
| `ra2_soviets_apocalypsetank` | redalert2_soviets | 80000 | 52 | 7000 | 1750 | 159600×1 | 63 | 2 | 125 | 4694.1 | 1750 | +0.1 | fp-debt |
| `yuri_mastermind` | redalert2_yuri | 62500 | 75 | 6980 | 1500 | 100×1 | 75 | 1 | 100 | 0.0 | 283 | -1217.1 |  |
| `steelconsortium_katytank` | redalert2mod_consortium | 70000 | 65 | 7250 | 3800 | 272000×2 | 50 | 2 | 100 | 18133.3 | 3800 | +0.1 |  |
| `futuretech_oriontank` | redalert2mod_futuretech | 95000 | 60 | 7500 | 2400 | 184300×2 | 60 | 1 | 100 | 6143.3 | 2400 | -0.1 |  |
| `naxis_maus` | redalert2mod_naxis | 85000 | 75 | 6680 | 4200 | 1233200×1 | 99 | 1 | 100 | 12456.6 | 4200 | +0.0 |  |
| `naxis_shoekarn` | redalert2mod_naxis | 85000 | 78 | 5370 | 2500 | 717200×1 | 54 | 1 | 100 | 13281.5 | 2500 | +0.2 |  |
| `japan_hovercraftflametank` | redalert_japan | 90000 | 78 | 5980 | 1700 | 13300×1 | 60 | 30 | 100 | 4483.1 | 1699 | -0.7 |  |
| `japan_oitank` | redalert_japan | 90000 | 70 | 5390 | 7000 | 1145700×1 | 45 | 1 | 100 | 25460.0 | 7000 | +0.1 |  |
| `ra1_soviets_heavyteslatank` | redalert_soviets | 72500 | 70 | 6650 | 3500 | 709900×1 | 80 | 1 | 100 | 8873.8 | 3500 | -0.2 |  |
| `ra1_soviets_mammothtank` | redalert_soviets | 97500 | 60 | 6410 | 2000 | 287000×1 | 95 | 2 | 100 | 5572.8 | 2000 | +0.1 |  |
| `ra1_soviets_siegemammothtank` | redalert_soviets | 100000 | 75 | 6660 | 4000 | 1228600×1 | 125 | 2 | 100 | 18201.5 | 4000 | +0.1 |  |
| `protoss_atreus` | starcraft_protoss | 75000 | 70 | 5210 | 2400 | 41000×2 | 28 | 4 | 100 | 8200.0 | 2401 | +1.0 |  |
| `terran_goliath` | starcraft_terran | 95000 | 78 | 6510 | 1600 | 200×1 | 0 | 1 | 33 | 0.0 | 444 | -1156.2 | fp-debt |
| `terran_goliathmk2` | starcraft_terran | 100000 | 55 | 7800 | 2400 | 300×1 | 0 | 1 | 33 | 0.0 | 405 | -1994.9 | fp-debt |
| `zerg_goremaw` | starcraft_zerg | 60000 | 70 | 5200 | 1500 | 118500×1 | 30 | 1 | 100 | 3950.0 | 1500 | +0.2 |  |
| `td_gdi_mammothtank` | tiberiandawn_gdi | 225000 | 60 | 6500 | 1600 | 8000×1 | 72 | 2 | 100 | 200.0 | 543 | -1057.2 | anchor |
| `td_gdi_mammothtankmkiii` | tiberiandawn_gdi | 95000 | 55 | 6340 | 3000 | 1081100×1 | 102 | 2 | 100 | 19656.4 | 3000 | -0.1 |  |
| `td_nod_chemicalstealthtank` | tiberiandawn_nod | 67500 | 78 | 6960 | 1800 | 221600×1 | 60 | 1 | 100 | 3693.3 | 1800 | +0.1 |  |
| `cabal_avatar` | tiberiansun_cabal | 100000 | 52 | 6330 | 7500 | 1768300×1 | 70 | 1 | 100 | 25261.4 | 7500 | -0.0 |  |
| `forgotten_scoopertank` | tiberiansun_forgotten | 95000 | 65 | 6220 | 2250 | 208800×1 | 60 | 2 | 100 | 6735.5 | 2250 | -0.4 |  |
| `ts_nod_stealthtank` | tiberiansun_nod | 65000 | 70 | 6380 | 1750 | 91600×2 | 60 | 2 | 100 | 5909.7 | 1750 | +0.1 |  |

**Worst |Δ| among non-anchor members: 1994.9** (goal ≤1).

## Uniqueness check (5 raw stats — soft/protected excluded)

- **HP duplicates**: {100000: ['duelist_tank.ixian', 'ra2_allies_heavymiragetank', 'ra1_soviets_siegemammothtank', 'terran_goliathmk2', 'cabal_avatar'], 80000: ['ordos_deviatortank', 'ra2_soviets_apocalypsetank'], 95000: ['ra2_allies_miragetank', 'futuretech_oriontank', 'terran_goliath', 'td_gdi_mammothtankmkiii', 'forgotten_scoopertank'], 85000: ['naxis_maus', 'naxis_shoekarn'], 90000: ['japan_hovercraftflametank', 'japan_oitank']}
- **Speed duplicates**: {55: ['duelist_tank.ixian', 'terran_goliathmk2', 'td_gdi_mammothtankmkiii'], 75: ['ordos_deviatortank', 'yuri_mastermind', 'naxis_maus', 'ra1_soviets_siegemammothtank'], 78: ['ordos_lasertank', 'naxis_shoekarn', 'japan_hovercraftflametank', 'terran_goliath', 'td_nod_chemicalstealthtank'], 60: ['ra2_allies_heavymiragetank', 'futuretech_oriontank', 'ra1_soviets_mammothtank'], 65: ['ra2_allies_miragetank', 'steelconsortium_katytank', 'forgotten_scoopertank'], 52: ['ra2_soviets_apocalypsetank', 'cabal_avatar'], 70: ['japan_oitank', 'ra1_soviets_heavyteslatank', 'protoss_atreus', 'zerg_goremaw', 'ts_nod_stealthtank']}
- **raw ReloadDelay duplicates** (design choice — retune coarse Damage/reload by hand): {60: ['futuretech_oriontank', 'japan_hovercraftflametank', 'td_nod_chemicalstealthtank', 'forgotten_scoopertank', 'ts_nod_stealthtank'], 0: ['terran_goliath', 'terran_goliathmk2']}

## Required YAML edits (per unit)

- `duelist_tank.ixian`: HP 100000, Speed 55, Range 7010, each offensive warhead Damage 107700 (×6 = SUM 646200), ReloadDelay 140, Burst 1, **DELETE the unconditional FirepowerMultiplier (50%)** — the Damage above already includes it (W17)
- `ordos_deviatortank`: HP 80000, Speed 75, Range 7070, each offensive warhead Damage 148200 (×2 = SUM 296400), ReloadDelay 98, Burst 1
- `ordos_lasertank`: HP 77500, Speed 78, Range 7280, each offensive warhead Damage 551600 (×1 = SUM 551600), ReloadDelay 55, Burst 1
- `ra2_allies_heavymiragetank`: HP 100000, Speed 60, Range 6110, each offensive warhead Damage 294000 (×1 = SUM 294000), ReloadDelay 51, Burst 1
- `ra2_allies_miragetank`: HP 95000, Speed 65, Range 6030, each offensive warhead Damage 168700 (×1 = SUM 168700), ReloadDelay 39, Burst 1
- `ra2_soviets_apocalypsetank`: HP 80000, Speed 52, Range 7000, each offensive warhead Damage 159600 (×1 = SUM 159600), ReloadDelay 63, Burst 2, **DELETE the unconditional FirepowerMultiplier (125%)** — the Damage above already includes it (W17)
- `yuri_mastermind`: HP 62500, Speed 75, Range 6980, each offensive warhead Damage 100 (×1 = SUM 100), ReloadDelay 75, Burst 1, residual Δ -1217.1 (cost pinned at 1500)
- `steelconsortium_katytank`: HP 70000, Speed 65, Range 7250, each offensive warhead Damage 272000 (×2 = SUM 544000), ReloadDelay 50, Burst 2
- `futuretech_oriontank`: HP 95000, Speed 60, Range 7500, each offensive warhead Damage 184300 (×2 = SUM 368600), ReloadDelay 60, Burst 1
- `naxis_maus`: HP 85000, Speed 75, Range 6680, each offensive warhead Damage 1233200 (×1 = SUM 1233200), ReloadDelay 99, Burst 1
- `naxis_shoekarn`: HP 85000, Speed 78, Range 5370, each offensive warhead Damage 717200 (×1 = SUM 717200), ReloadDelay 54, Burst 1
- `japan_hovercraftflametank`: HP 90000, Speed 78, Range 5980, each offensive warhead Damage 13300 (×1 = SUM 13300), ReloadDelay 60, Burst 30
- `japan_oitank`: HP 90000, Speed 70, Range 5390, each offensive warhead Damage 1145700 (×1 = SUM 1145700), ReloadDelay 45, Burst 1
- `ra1_soviets_heavyteslatank`: HP 72500, Speed 70, Range 6650, each offensive warhead Damage 709900 (×1 = SUM 709900), ReloadDelay 80, Burst 1
- `ra1_soviets_mammothtank`: HP 97500, Speed 60, Range 6410, each offensive warhead Damage 287000 (×1 = SUM 287000), ReloadDelay 95, Burst 2
- `ra1_soviets_siegemammothtank`: HP 100000, Speed 75, Range 6660, each offensive warhead Damage 1228600 (×1 = SUM 1228600), ReloadDelay 125, Burst 2
- `protoss_atreus`: HP 75000, Speed 70, Range 5210, each offensive warhead Damage 41000 (×2 = SUM 82000), ReloadDelay 28, Burst 4
- `terran_goliath`: HP 95000, Speed 78, Range 6510, each offensive warhead Damage 200 (×1 = SUM 200), ReloadDelay 0, Burst 1, **DELETE the unconditional FirepowerMultiplier (33%)** — the Damage above already includes it (W17), residual Δ -1156.2 (cost pinned at 1600)
- `terran_goliathmk2`: HP 100000, Speed 55, Range 7800, each offensive warhead Damage 300 (×1 = SUM 300), ReloadDelay 0, Burst 1, **DELETE the unconditional FirepowerMultiplier (33%)** — the Damage above already includes it (W17), residual Δ -1994.9 (cost pinned at 2400)
- `zerg_goremaw`: HP 60000, Speed 70, Range 5200, each offensive warhead Damage 118500 (×1 = SUM 118500), ReloadDelay 30, Burst 1
- `td_gdi_mammothtankmkiii`: HP 95000, Speed 55, Range 6340, each offensive warhead Damage 1081100 (×1 = SUM 1081100), ReloadDelay 102, Burst 2
- `td_nod_chemicalstealthtank`: HP 67500, Speed 78, Range 6960, each offensive warhead Damage 221600 (×1 = SUM 221600), ReloadDelay 60, Burst 1
- `cabal_avatar`: HP 100000, Speed 52, Range 6330, each offensive warhead Damage 1768300 (×1 = SUM 1768300), ReloadDelay 70, Burst 1
- `forgotten_scoopertank`: HP 95000, Speed 65, Range 6220, each offensive warhead Damage 208800 (×1 = SUM 208800), ReloadDelay 60, Burst 2
- `ts_nod_stealthtank`: HP 65000, Speed 70, Range 6380, each offensive warhead Damage 91600 (×2 = SUM 183200), ReloadDelay 60, Burst 2
