# Epic Vehicle infantry rebalance proposal

Anchor spec: HP=4000000, Speed=60, Range=8500, eff-DPS=20000, Cost=10000

Converter law: cost pinned, range clamped to band + made unique, eff-DPS trimmed to Δ≤1 via 100-grid warhead Damage; unconditional FirepowerMultiplier is retired.

Uniqueness separates **raw warhead Damage (the 5-stat law as written)**. The levers run coarsest-first — Damage (100 grid) → Speed (1) → Range (10) — because one Damage step is a whole shot and running it last threw away everything the fine levers had achieved.

| actor | faction | HP | spd | rng | cost | dmg/wh×n | rl | burst | legacy FP% | eff DPS | price | Δ | flags |
|---|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|---|
| `ixian_ixprojector` | d2k_ixian | 90000 | 70 | 8560 | 5000 | 100×1 | 0 | 1 | 100 | 0.0 | 1231 | -3769.4 |  |
| `futuretech_futuretank` | redalert2mod_futuretech | 90000 | 70 | 7500 | 10000 | 1296100×2 | 65 | 2 | 25 | 69125.3 | 10000 | -0.2 | fp-debt |
| `naxis_nokana` | redalert2mod_naxis | 80000 | 60 | 8480 | 3000 | 200×1 | 0 | 1 | 100 | 0.0 | 1106 | -1893.7 |  |
| `naxis_ratte` | redalert2mod_naxis | 75000 | 50 | 6820 | 8000 | 4620700×1 | 145 | 2 | 100 | 59621.9 | 8000 | +0.0 |  |
| `schwarzermond_dalek` | redalert2mod_schwarzermond | 95000 | 55 | 8060 | 9000 | 1508200×3 | 76 | 1 | 50 | 59534.2 | 9000 | -0.0 | fp-debt |
| `latinsyndicate_nuketruck` | redalert2mod_syndicate | 60000 | 65 | 8540 | 3000 | 300×1 | 0 | 1 | 100 | 0.0 | 1135 | -1865.1 |  |
| `latinsyndicate_topolm` | redalert2mod_syndicate | 80000 | 60 | 8520 | 6000 | 400×1 | 0 | 1 | 100 | 0.0 | 1719 | -4281.4 |  |
| `tkm_bigshiee` | redalert2mod_tkm | 70000 | 50 | 7240 | 5000 | 1147900×2 | 83 | 2 | 100 | 53390.7 | 5000 | -0.1 |  |
| `tkm_sandmarine` | redalert2mod_tkm | 70000 | 60 | 6800 | 5000 | 1224800×2 | 129 | 3 | 100 | 54435.6 | 5000 | -0.1 |  |
| `tkm_t30` | redalert2mod_tkm | 65000 | 65 | 10200 | 5000 | 1549000×2 | 80 | 1 | 100 | 38725.0 | 5000 | -0.0 |  |
| `ra1_allies_chronotank` | redalert_allies | 75000 | 70 | 6810 | 2000 | 500×1 | 54 | 4 | 100 | 31.7 | 2365 | +365.3 | OVERPRICED@min-dps |
| `japan_exorcistoitank` | redalert_japan | 85000 | 65 | 6840 | 10000 | 2853700×1 | 45 | 1 | 100 | 63415.6 | 10000 | -0.1 |  |
| `japan_shogunexecutioner` | redalert_japan | 85000 | 70 | 6830 | 10000 | 18835400×1 | 300 | 1 | 100 | 62784.7 | 10000 | -0.0 |  |
| `ra1_soviets_madtank` | redalert_soviets | 100000 | 70 | 8500 | 3000 | 600×1 | 0 | 1 | 100 | 0.0 | 1231 | -1768.8 |  |
| `ra1_soviets_monstertank` | redalert_soviets | 1000000 | 45 | 8500 | 10000 | 80000×1 | 103 | 2 | 100 | 1185.2 | 2164 | -7835.6 | anchor |
| `protoss_idol` | starcraft_protoss | 97500 | 55 | 8490 | 2800 | 700×1 | 0 | 1 | 223 | 0.0 | 1862 | -938.1 | fp-debt |
| `zerg_hermit` | starcraft_zerg | 62500 | 70 | 7120 | 6000 | 225600×1 | 30 | 5 | 40 | 37600.0 | 6001 | +0.6 | fp-debt |
| `td_gdi_defenserig` | tiberiandawn_gdi | 100000 | 60 | 8510 | 5000 | 800×1 | 0 | 1 | 100 | 0.0 | 875 | -4125.0 |  |
| `cabal_coredefender` | tiberiansun_cabal | 100000 | 50 | 6870 | 15000 | 6151100×1 | 70 | 1 | 100 | 87872.9 | 15000 | -0.0 |  |
| `forgotten_chemicalmammothtank` | tiberiansun_forgotten | 100000 | 60 | 6860 | 5000 | 511600×2 | 60 | 2 | 100 | 31975.0 | 5000 | -0.2 |  |
| `forgotten_experimentalmammothtank` | tiberiansun_forgotten | 95000 | 55 | 6850 | 6000 | 392500×3 | 52 | 2 | 100 | 42053.6 | 6000 | +0.2 |  |
| `forgotten_nomadbarracks` | tiberiansun_forgotten | 95000 | 65 | 8300 | 6500 | 232400×1 | 6 | 1 | 100 | 38733.3 | 6501 | +0.5 |  |
| `ts_gdi_mammothmkii` | tiberiansun_gdi | 65000 | 50 | 8470 | 8000 | 900×1 | 0 | 1 | 100 | 0.0 | 1347 | -6653.2 |  |
| `ts_gdi_mammothprototype` | tiberiansun_gdi | 100000 | 65 | 8530 | 4000 | 1000×1 | 0 | 1 | 100 | 0.0 | 1557 | -2442.6 |  |

**Worst |Δ| among non-anchor members: 6653.2** (goal ≤1).

## Uniqueness check (5 raw stats — soft/protected excluded)

- **HP duplicates**: {90000: ['ixian_ixprojector', 'futuretech_futuretank'], 80000: ['naxis_nokana', 'latinsyndicate_topolm'], 75000: ['naxis_ratte', 'ra1_allies_chronotank'], 95000: ['schwarzermond_dalek', 'forgotten_experimentalmammothtank', 'forgotten_nomadbarracks'], 70000: ['tkm_bigshiee', 'tkm_sandmarine'], 65000: ['tkm_t30', 'ts_gdi_mammothmkii'], 85000: ['japan_exorcistoitank', 'japan_shogunexecutioner'], 100000: ['ra1_soviets_madtank', 'td_gdi_defenserig', 'cabal_coredefender', 'forgotten_chemicalmammothtank', 'ts_gdi_mammothprototype']}
- **Speed duplicates**: {70: ['ixian_ixprojector', 'futuretech_futuretank', 'ra1_allies_chronotank', 'japan_shogunexecutioner', 'ra1_soviets_madtank', 'zerg_hermit'], 60: ['naxis_nokana', 'latinsyndicate_topolm', 'tkm_sandmarine', 'td_gdi_defenserig', 'forgotten_chemicalmammothtank'], 50: ['naxis_ratte', 'tkm_bigshiee', 'cabal_coredefender', 'ts_gdi_mammothmkii'], 55: ['schwarzermond_dalek', 'protoss_idol', 'forgotten_experimentalmammothtank'], 65: ['latinsyndicate_nuketruck', 'tkm_t30', 'japan_exorcistoitank', 'forgotten_nomadbarracks', 'ts_gdi_mammothprototype']}
- **raw ReloadDelay duplicates** (design choice — retune coarse Damage/reload by hand): {0: ['ixian_ixprojector', 'naxis_nokana', 'latinsyndicate_nuketruck', 'latinsyndicate_topolm', 'ra1_soviets_madtank', 'protoss_idol', 'td_gdi_defenserig', 'ts_gdi_mammothmkii', 'ts_gdi_mammothprototype']}

## Required YAML edits (per unit)

- `ixian_ixprojector`: HP 90000, Speed 70, Range 8560, each offensive warhead Damage 100 (×1 = SUM 100), ReloadDelay 0, Burst 1, residual Δ -3769.4 (cost pinned at 5000)
- `futuretech_futuretank`: HP 90000, Speed 70, Range 7500, each offensive warhead Damage 1296100 (×2 = SUM 2592200), ReloadDelay 65, Burst 2, **DELETE the unconditional FirepowerMultiplier (25%)** — the Damage above already includes it (W17)
- `naxis_nokana`: HP 80000, Speed 60, Range 8480, each offensive warhead Damage 200 (×1 = SUM 200), ReloadDelay 0, Burst 1, residual Δ -1893.7 (cost pinned at 3000)
- `naxis_ratte`: HP 75000, Speed 50, Range 6820, each offensive warhead Damage 4620700 (×1 = SUM 4620700), ReloadDelay 145, Burst 2
- `schwarzermond_dalek`: HP 95000, Speed 55, Range 8060, each offensive warhead Damage 1508200 (×3 = SUM 4524600), ReloadDelay 76, Burst 1, **DELETE the unconditional FirepowerMultiplier (50%)** — the Damage above already includes it (W17)
- `latinsyndicate_nuketruck`: HP 60000, Speed 65, Range 8540, each offensive warhead Damage 300 (×1 = SUM 300), ReloadDelay 0, Burst 1, residual Δ -1865.1 (cost pinned at 3000)
- `latinsyndicate_topolm`: HP 80000, Speed 60, Range 8520, each offensive warhead Damage 400 (×1 = SUM 400), ReloadDelay 0, Burst 1, residual Δ -4281.4 (cost pinned at 6000)
- `tkm_bigshiee`: HP 70000, Speed 50, Range 7240, each offensive warhead Damage 1147900 (×2 = SUM 2295800), ReloadDelay 83, Burst 2
- `tkm_sandmarine`: HP 70000, Speed 60, Range 6800, each offensive warhead Damage 1224800 (×2 = SUM 2449600), ReloadDelay 129, Burst 3
- `tkm_t30`: HP 65000, Speed 65, Range 10200, each offensive warhead Damage 1549000 (×2 = SUM 3098000), ReloadDelay 80, Burst 1
- `ra1_allies_chronotank`: HP 75000, Speed 70, Range 6810, each offensive warhead Damage 500 (×1 = SUM 500), ReloadDelay 54, Burst 4, residual Δ +365.3 (cost pinned at 2000)
- `japan_exorcistoitank`: HP 85000, Speed 65, Range 6840, each offensive warhead Damage 2853700 (×1 = SUM 2853700), ReloadDelay 45, Burst 1
- `japan_shogunexecutioner`: HP 85000, Speed 70, Range 6830, each offensive warhead Damage 18835400 (×1 = SUM 18835400), ReloadDelay 300, Burst 1
- `ra1_soviets_madtank`: HP 100000, Speed 70, Range 8500, each offensive warhead Damage 600 (×1 = SUM 600), ReloadDelay 0, Burst 1, residual Δ -1768.8 (cost pinned at 3000)
- `protoss_idol`: HP 97500, Speed 55, Range 8490, each offensive warhead Damage 700 (×1 = SUM 700), ReloadDelay 0, Burst 1, **DELETE the unconditional FirepowerMultiplier (223%)** — the Damage above already includes it (W17), residual Δ -938.1 (cost pinned at 2800)
- `zerg_hermit`: HP 62500, Speed 70, Range 7120, each offensive warhead Damage 225600 (×1 = SUM 225600), ReloadDelay 30, Burst 5, **DELETE the unconditional FirepowerMultiplier (40%)** — the Damage above already includes it (W17)
- `td_gdi_defenserig`: HP 100000, Speed 60, Range 8510, each offensive warhead Damage 800 (×1 = SUM 800), ReloadDelay 0, Burst 1, residual Δ -4125.0 (cost pinned at 5000)
- `cabal_coredefender`: HP 100000, Speed 50, Range 6870, each offensive warhead Damage 6151100 (×1 = SUM 6151100), ReloadDelay 70, Burst 1
- `forgotten_chemicalmammothtank`: HP 100000, Speed 60, Range 6860, each offensive warhead Damage 511600 (×2 = SUM 1023200), ReloadDelay 60, Burst 2
- `forgotten_experimentalmammothtank`: HP 95000, Speed 55, Range 6850, each offensive warhead Damage 392500 (×3 = SUM 1177500), ReloadDelay 52, Burst 2
- `forgotten_nomadbarracks`: HP 95000, Speed 65, Range 8300, each offensive warhead Damage 232400 (×1 = SUM 232400), ReloadDelay 6, Burst 1
- `ts_gdi_mammothmkii`: HP 65000, Speed 50, Range 8470, each offensive warhead Damage 900 (×1 = SUM 900), ReloadDelay 0, Burst 1, residual Δ -6653.2 (cost pinned at 8000)
- `ts_gdi_mammothprototype`: HP 100000, Speed 65, Range 8530, each offensive warhead Damage 1000 (×1 = SUM 1000), ReloadDelay 0, Burst 1, residual Δ -2442.6 (cost pinned at 4000)
