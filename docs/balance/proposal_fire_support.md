# Fire Support infantry rebalance proposal

Anchor spec: HP=120000, Speed=90, Range=10000, eff-DPS=2100, Cost=1400

Converter law: cost pinned, range clamped to band + made unique, eff-DPS trimmed to Δ≤1 via 100-grid warhead Damage; unconditional FirepowerMultiplier is retired.

Uniqueness separates **raw warhead Damage (the 5-stat law as written)**. The levers run coarsest-first — Damage (100 grid) → Speed (1) → Range (10) — because one Damage step is a whole shot and running it last threw away everything the fine levers had achieved.

| actor | faction | HP | spd | rng | cost | dmg/wh×n | rl | burst | legacy FP% | eff DPS | price | Δ | flags |
|---|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|---|
| `ixian_stormraider` | d2k_ixian | 60000 | 105 | 10020 | 2200 | 100×1 | 0 | 1 | 50 | 0.0 | 563 | -1636.6 | fp-debt |
| `ra2_allies_prismtank` | redalert2_allies | 57500 | 85 | 11110 | 2000 | 108900×3 | 125 | 1 | 100 | 2613.6 | 2000 | +0.3 |  |
| `ra2_soviets_teslatank` | redalert2_soviets | 80000 | 90 | 8040 | 1800 | 97300×1 | 55 | 2 | 100 | 3243.3 | 1799 | -0.6 |  |
| `yuri_magnetron` | redalert2_yuri | 42500 | 108 | 10030 | 1300 | 200×1 | 0 | 1 | 100 | 0.0 | 634 | -666.4 |  |
| `asianalliance_heavyrailguntank` | redalert2mod_asianalliance | 100000 | 72 | 10070 | 2200 | 613400×1 | 75 | 1 | 100 | 8178.7 | 2200 | +0.0 |  |
| `asianalliance_railguntank` | redalert2mod_asianalliance | 97500 | 75 | 8890 | 1500 | 339100×1 | 60 | 1 | 100 | 5651.7 | 1500 | +0.2 |  |
| `futuretech_beehivedronecarrier` | redalert2mod_futuretech | 90000 | 100 | 10060 | 2100 | 300×1 | 0 | 1 | 100 | 0.0 | 240 | -1860.5 |  |
| `futuretech_energizer` | redalert2mod_futuretech | 52500 | 105 | 10040 | 1000 | 400×1 | 0 | 1 | 100 | 0.0 | 192 | -808.3 |  |
| `futuretech_gunstrider` | redalert2mod_futuretech | 87500 | 108 | 8300 | 2500 | 134400×1 | 12 | 1 | 100 | 11200.0 | 2499 | -0.7 |  |
| `naxis_imperialturbotank` | redalert2mod_naxis | 85000 | 100 | 8060 | 950 | 43500×1 | 28 | 1 | 100 | 1553.6 | 950 | -0.2 |  |
| `naxis_nop03sarubia` | redalert2mod_naxis | 67500 | 95 | 10000 | 1400 | 500×1 | 0 | 1 | 100 | 0.0 | 319 | -1081.0 |  |
| `schwarzermond_crystaltank` | redalert2mod_schwarzermond | 77500 | 85 | 9970 | 1700 | 600×1 | 0 | 1 | 100 | 0.0 | 527 | -1173.0 |  |
| `schwarzermond_korruptesbiest` | redalert2mod_schwarzermond | 75000 | 80 | 8160 | 3500 | 55300×1 | 34 | 8 | 100 | 9216.7 | 3499 | -0.6 |  |
| `latinsyndicate_missiletruck` | redalert2mod_syndicate | 30000 | 75 | 10000 | 1000 | 8000×1 | 108 | 8 | 100 | 496.1 | 397 | -602.7 | anchor shared-wpn? |
| `japan_nanodronebuggy` | redalert_japan | 32500 | 108 | 8070 | 1200 | 141900×1 | 77 | 1 | 33 | 1842.9 | 1200 | +0.2 | fp-debt |
| `japan_waveforcetank` | redalert_japan | 62500 | 105 | 9340 | 1800 | 80400×2 | 60 | 1 | 100 | 2680.0 | 1800 | +0.5 |  |
| `ra1_soviets_heatraytank` | redalert_soviets | 65000 | 95 | 9990 | 1900 | 700×1 | 0 | 1 | 25 | 0.0 | 648 | -1252.2 | fp-debt |
| `ra1_soviets_teslatank` | redalert_soviets | 37500 | 90 | 8050 | 1700 | 522000×1 | 80 | 1 | 100 | 6525.0 | 1700 | +0.1 |  |
| `protoss_reaver` | starcraft_protoss | 82500 | 100 | 12000 | 2700 | 800×1 | 0 | 1 | 100 | 0.0 | 784 | -1915.9 |  |
| `zerg_lurker` | starcraft_zerg | 72500 | 72 | 9960 | 1300 | 900×1 | 0 | 1 | 100 | 0.0 | 626 | -673.6 |  |
| `zerg_sporemaw` | starcraft_zerg | 25000 | 108 | 8000 | 1000 | 75700×1 | 40 | 1 | 100 | 1892.5 | 1000 | -0.1 |  |
| `td_gdi_exosuit` | tiberiandawn_gdi | 55000 | 80 | 10010 | 1200 | 1000×1 | 0 | 1 | 120 | 0.0 | 208 | -992.2 | fp-debt |
| `td_nod_ssmlauncher` | tiberiandawn_nod | 20000 | 75 | 8940 | 800 | 318500×1 | 225 | 2 | 100 | 2548.0 | 800 | -0.1 |  |
| `cabal_laserspider` | tiberiansun_cabal | 50000 | 80 | 8110 | 1200 | 201200×1 | 90 | 1 | 100 | 2235.6 | 1200 | +0.0 |  |
| `cabal_mantis` | tiberiansun_cabal | 47500 | 108 | 8100 | 500 | 600×2 | 40 | 1 | 100 | 30.0 | 592 | +91.6 | OVERPRICED@min-dps |
| `cabal_spidercnc4` | tiberiansun_cabal | 35000 | 85 | 8090 | 1500 | 290300×1 | 80 | 1 | 100 | 3628.8 | 1500 | +0.2 |  |
| `forgotten_tankkiller` | tiberiansun_forgotten | 95000 | 90 | 8080 | 900 | 37000×1 | 90 | 1 | 100 | 411.1 | 900 | +0.1 |  |
| `forgotten_warriortank` | tiberiansun_forgotten | 92500 | 95 | 8210 | 2000 | 252300×1 | 55 | 1 | 100 | 4587.3 | 2000 | -0.1 |  |
| `ts_gdi_wolverine` | tiberiansun_gdi | 27500 | 72 | 8020 | 550 | 11300×1 | 33 | 2 | 100 | 645.7 | 551 | +0.7 | shared-wpn? |
| `ts_gdi_wolverinemkii` | tiberiansun_gdi | 40000 | 75 | 8010 | 950 | 37400×1 | 40 | 2 | 100 | 1781.0 | 950 | +0.1 |  |

**Worst |Δ| among non-anchor members: 1915.9** (goal ≤1).

## Uniqueness check (5 raw stats — soft/protected excluded)

- **Speed duplicates**: {105: ['ixian_stormraider', 'futuretech_energizer', 'japan_waveforcetank'], 85: ['ra2_allies_prismtank', 'schwarzermond_crystaltank', 'cabal_spidercnc4'], 90: ['ra2_soviets_teslatank', 'ra1_soviets_teslatank', 'forgotten_tankkiller'], 108: ['yuri_magnetron', 'futuretech_gunstrider', 'japan_nanodronebuggy', 'zerg_sporemaw', 'cabal_mantis'], 72: ['asianalliance_heavyrailguntank', 'zerg_lurker', 'ts_gdi_wolverine'], 75: ['asianalliance_railguntank', 'td_nod_ssmlauncher', 'ts_gdi_wolverinemkii'], 100: ['futuretech_beehivedronecarrier', 'naxis_imperialturbotank', 'protoss_reaver'], 95: ['naxis_nop03sarubia', 'ra1_soviets_heatraytank', 'forgotten_warriortank'], 80: ['schwarzermond_korruptesbiest', 'td_gdi_exosuit', 'cabal_laserspider']}
- **raw ReloadDelay duplicates** (design choice — retune coarse Damage/reload by hand): {0: ['ixian_stormraider', 'yuri_magnetron', 'futuretech_beehivedronecarrier', 'futuretech_energizer', 'naxis_nop03sarubia', 'schwarzermond_crystaltank', 'ra1_soviets_heatraytank', 'protoss_reaver', 'zerg_lurker', 'td_gdi_exosuit'], 55: ['ra2_soviets_teslatank', 'forgotten_warriortank'], 60: ['asianalliance_railguntank', 'japan_waveforcetank'], 80: ['ra1_soviets_teslatank', 'cabal_spidercnc4'], 40: ['zerg_sporemaw', 'cabal_mantis', 'ts_gdi_wolverinemkii'], 90: ['cabal_laserspider', 'forgotten_tankkiller']}

## Required YAML edits (per unit)

- `ixian_stormraider`: HP 60000, Speed 105, Range 10020, each offensive warhead Damage 100 (×1 = SUM 100), ReloadDelay 0, Burst 1, **DELETE the unconditional FirepowerMultiplier (50%)** — the Damage above already includes it (W17), residual Δ -1636.6 (cost pinned at 2200)
- `ra2_allies_prismtank`: HP 57500, Speed 85, Range 11110, each offensive warhead Damage 108900 (×3 = SUM 326700), ReloadDelay 125, Burst 1
- `ra2_soviets_teslatank`: HP 80000, Speed 90, Range 8040, each offensive warhead Damage 97300 (×1 = SUM 97300), ReloadDelay 55, Burst 2
- `yuri_magnetron`: HP 42500, Speed 108, Range 10030, each offensive warhead Damage 200 (×1 = SUM 200), ReloadDelay 0, Burst 1, residual Δ -666.4 (cost pinned at 1300)
- `asianalliance_heavyrailguntank`: HP 100000, Speed 72, Range 10070, each offensive warhead Damage 613400 (×1 = SUM 613400), ReloadDelay 75, Burst 1
- `asianalliance_railguntank`: HP 97500, Speed 75, Range 8890, each offensive warhead Damage 339100 (×1 = SUM 339100), ReloadDelay 60, Burst 1
- `futuretech_beehivedronecarrier`: HP 90000, Speed 100, Range 10060, each offensive warhead Damage 300 (×1 = SUM 300), ReloadDelay 0, Burst 1, residual Δ -1860.5 (cost pinned at 2100)
- `futuretech_energizer`: HP 52500, Speed 105, Range 10040, each offensive warhead Damage 400 (×1 = SUM 400), ReloadDelay 0, Burst 1, residual Δ -808.3 (cost pinned at 1000)
- `futuretech_gunstrider`: HP 87500, Speed 108, Range 8300, each offensive warhead Damage 134400 (×1 = SUM 134400), ReloadDelay 12, Burst 1
- `naxis_imperialturbotank`: HP 85000, Speed 100, Range 8060, each offensive warhead Damage 43500 (×1 = SUM 43500), ReloadDelay 28, Burst 1
- `naxis_nop03sarubia`: HP 67500, Speed 95, Range 10000, each offensive warhead Damage 500 (×1 = SUM 500), ReloadDelay 0, Burst 1, residual Δ -1081.0 (cost pinned at 1400)
- `schwarzermond_crystaltank`: HP 77500, Speed 85, Range 9970, each offensive warhead Damage 600 (×1 = SUM 600), ReloadDelay 0, Burst 1, residual Δ -1173.0 (cost pinned at 1700)
- `schwarzermond_korruptesbiest`: HP 75000, Speed 80, Range 8160, each offensive warhead Damage 55300 (×1 = SUM 55300), ReloadDelay 34, Burst 8
- `japan_nanodronebuggy`: HP 32500, Speed 108, Range 8070, each offensive warhead Damage 141900 (×1 = SUM 141900), ReloadDelay 77, Burst 1, **DELETE the unconditional FirepowerMultiplier (33%)** — the Damage above already includes it (W17)
- `japan_waveforcetank`: HP 62500, Speed 105, Range 9340, each offensive warhead Damage 80400 (×2 = SUM 160800), ReloadDelay 60, Burst 1
- `ra1_soviets_heatraytank`: HP 65000, Speed 95, Range 9990, each offensive warhead Damage 700 (×1 = SUM 700), ReloadDelay 0, Burst 1, **DELETE the unconditional FirepowerMultiplier (25%)** — the Damage above already includes it (W17), residual Δ -1252.2 (cost pinned at 1900)
- `ra1_soviets_teslatank`: HP 37500, Speed 90, Range 8050, each offensive warhead Damage 522000 (×1 = SUM 522000), ReloadDelay 80, Burst 1
- `protoss_reaver`: HP 82500, Speed 100, Range 12000, each offensive warhead Damage 800 (×1 = SUM 800), ReloadDelay 0, Burst 1, residual Δ -1915.9 (cost pinned at 2700)
- `zerg_lurker`: HP 72500, Speed 72, Range 9960, each offensive warhead Damage 900 (×1 = SUM 900), ReloadDelay 0, Burst 1, residual Δ -673.6 (cost pinned at 1300)
- `zerg_sporemaw`: HP 25000, Speed 108, Range 8000, each offensive warhead Damage 75700 (×1 = SUM 75700), ReloadDelay 40, Burst 1
- `td_gdi_exosuit`: HP 55000, Speed 80, Range 10010, each offensive warhead Damage 1000 (×1 = SUM 1000), ReloadDelay 0, Burst 1, **DELETE the unconditional FirepowerMultiplier (120%)** — the Damage above already includes it (W17), residual Δ -992.2 (cost pinned at 1200)
- `td_nod_ssmlauncher`: HP 20000, Speed 75, Range 8940, each offensive warhead Damage 318500 (×1 = SUM 318500), ReloadDelay 225, Burst 2
- `cabal_laserspider`: HP 50000, Speed 80, Range 8110, each offensive warhead Damage 201200 (×1 = SUM 201200), ReloadDelay 90, Burst 1
- `cabal_mantis`: HP 47500, Speed 108, Range 8100, each offensive warhead Damage 600 (×2 = SUM 1200), ReloadDelay 40, Burst 1, residual Δ +91.6 (cost pinned at 500)
- `cabal_spidercnc4`: HP 35000, Speed 85, Range 8090, each offensive warhead Damage 290300 (×1 = SUM 290300), ReloadDelay 80, Burst 1
- `forgotten_tankkiller`: HP 95000, Speed 90, Range 8080, each offensive warhead Damage 37000 (×1 = SUM 37000), ReloadDelay 90, Burst 1
- `forgotten_warriortank`: HP 92500, Speed 95, Range 8210, each offensive warhead Damage 252300 (×1 = SUM 252300), ReloadDelay 55, Burst 1
- `ts_gdi_wolverine`: HP 27500, Speed 72, Range 8020, each offensive warhead Damage 11300 (×1 = SUM 11300), ReloadDelay 33, Burst 2
- `ts_gdi_wolverinemkii`: HP 40000, Speed 75, Range 8010, each offensive warhead Damage 37400 (×1 = SUM 37400), ReloadDelay 40, Burst 2
