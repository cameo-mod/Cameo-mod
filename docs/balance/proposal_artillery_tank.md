# Artillery Tank infantry rebalance proposal

Anchor spec: HP=140000, Speed=85, Range=12000, eff-DPS=525, Cost=700

Converter law: cost pinned, range clamped to band + made unique, eff-DPS trimmed to Δ≤1 via 100-grid warhead Damage; unconditional FirepowerMultiplier is retired.

Uniqueness separates **raw warhead Damage (the 5-stat law as written)**. The levers run coarsest-first — Damage (100 grid) → Speed (1) → Range (10) — because one Damage step is a whole shot and running it last threw away everything the fine levers had achieved.

| actor | faction | HP | spd | rng | cost | dmg/wh×n | rl | burst | legacy FP% | eff DPS | price | Δ | flags |
|---|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|---|
| `ixian_ixcombatsiege` | d2k_ixian | 80000 | 80 | 12000 | 1200 | 24000×3 | 80 | 1 | 100 | 300.0 | 381 | -819.0 | anchor |
| `ordos_cobratank` | d2k_ordos | 45000 | 90 | 9630 | 1500 | 95900×4 | 80 | 1 | 100 | 4795.0 | 1500 | +0.4 |  |
| `ordos_pythontank` | d2k_ordos | 70000 | 95 | 9610 | 2500 | 105700×4 | 123 | 2 | 100 | 6874.8 | 2500 | +0.2 |  |
| `asianalliance_howitzer` | redalert2mod_asianalliance | 42500 | 70 | 14400 | 1600 | 642000×2 | 160 | 1 | 100 | 8025.0 | 1600 | -0.0 |  |
| `schwarzermond_lunargrille` | redalert2mod_schwarzermond | 62500 | 100 | 12020 | 600 | 100×1 | 0 | 1 | 100 | 0.0 | 214 | -385.6 |  |
| `schwarzermond_mars` | redalert2mod_schwarzermond | 32500 | 95 | 11990 | 2000 | 200×1 | 0 | 1 | 125 | 0.0 | 115 | -1885.5 | fp-debt |
| `japan_waveforceartillery` | redalert_japan | 47500 | 85 | 12030 | 2500 | 300×1 | 0 | 1 | 50 | 0.0 | 143 | -2356.9 | fp-debt |
| `ra1_soviets_grad` | redalert_soviets | 50000 | 100 | 13790 | 1400 | 26800×2 | 115 | 8 | 100 | 2858.7 | 1400 | -0.0 |  |
| `terran_siegetank` | starcraft_terran | 100000 | 95 | 9600 | 2800 | 55000×3 | 37 | 1 | 100 | 4459.5 | 2801 | +0.8 |  |
| `td_gdi_archerartillery` | tiberiandawn_gdi | 35000 | 100 | 14000 | 750 | 109000×2 | 140 | 1 | 100 | 1557.1 | 750 | -0.2 |  |
| `forgotten_missilevan` | tiberiansun_forgotten | 37500 | 75 | 10450 | 1200 | 76000×1 | 32 | 2 | 100 | 3800.0 | 1200 | -0.1 |  |
| `forgotten_mlrs` | tiberiansun_forgotten | 40000 | 80 | 11920 | 2750 | 80400×1 | 34 | 4 | 100 | 8040.0 | 2750 | +0.2 |  |
| `ts_gdi_juggernaut` | tiberiansun_gdi | 30000 | 90 | 11980 | 1400 | 400×1 | 0 | 1 | 100 | 0.0 | 103 | -1297.1 |  |
| `ts_nod_artillery` | tiberiansun_nod | 25000 | 85 | 12000 | 1300 | 500×1 | 0 | 1 | 100 | 0.0 | 148 | -1152.1 |  |

**Worst |Δ| among non-anchor members: 2356.9** (goal ≤1).

## Uniqueness check (5 raw stats — soft/protected excluded)

- **Speed duplicates**: {90: ['ordos_cobratank', 'ts_gdi_juggernaut'], 95: ['ordos_pythontank', 'schwarzermond_mars', 'terran_siegetank'], 100: ['schwarzermond_lunargrille', 'ra1_soviets_grad', 'td_gdi_archerartillery'], 85: ['japan_waveforceartillery', 'ts_nod_artillery']}
- **raw ReloadDelay duplicates** (design choice — retune coarse Damage/reload by hand): {0: ['schwarzermond_lunargrille', 'schwarzermond_mars', 'japan_waveforceartillery', 'ts_gdi_juggernaut', 'ts_nod_artillery']}

## Required YAML edits (per unit)

- `ordos_cobratank`: HP 45000, Speed 90, Range 9630, each offensive warhead Damage 95900 (×4 = SUM 383600), ReloadDelay 80, Burst 1
- `ordos_pythontank`: HP 70000, Speed 95, Range 9610, each offensive warhead Damage 105700 (×4 = SUM 422800), ReloadDelay 123, Burst 2
- `asianalliance_howitzer`: HP 42500, Speed 70, Range 14400, each offensive warhead Damage 642000 (×2 = SUM 1284000), ReloadDelay 160, Burst 1
- `schwarzermond_lunargrille`: HP 62500, Speed 100, Range 12020, each offensive warhead Damage 100 (×1 = SUM 100), ReloadDelay 0, Burst 1, residual Δ -385.6 (cost pinned at 600)
- `schwarzermond_mars`: HP 32500, Speed 95, Range 11990, each offensive warhead Damage 200 (×1 = SUM 200), ReloadDelay 0, Burst 1, **DELETE the unconditional FirepowerMultiplier (125%)** — the Damage above already includes it (W17), residual Δ -1885.5 (cost pinned at 2000)
- `japan_waveforceartillery`: HP 47500, Speed 85, Range 12030, each offensive warhead Damage 300 (×1 = SUM 300), ReloadDelay 0, Burst 1, **DELETE the unconditional FirepowerMultiplier (50%)** — the Damage above already includes it (W17), residual Δ -2356.9 (cost pinned at 2500)
- `ra1_soviets_grad`: HP 50000, Speed 100, Range 13790, each offensive warhead Damage 26800 (×2 = SUM 53600), ReloadDelay 115, Burst 8
- `terran_siegetank`: HP 100000, Speed 95, Range 9600, each offensive warhead Damage 55000 (×3 = SUM 165000), ReloadDelay 37, Burst 1
- `td_gdi_archerartillery`: HP 35000, Speed 100, Range 14000, each offensive warhead Damage 109000 (×2 = SUM 218000), ReloadDelay 140, Burst 1
- `forgotten_missilevan`: HP 37500, Speed 75, Range 10450, each offensive warhead Damage 76000 (×1 = SUM 76000), ReloadDelay 32, Burst 2
- `forgotten_mlrs`: HP 40000, Speed 80, Range 11920, each offensive warhead Damage 80400 (×1 = SUM 80400), ReloadDelay 34, Burst 4
- `ts_gdi_juggernaut`: HP 30000, Speed 90, Range 11980, each offensive warhead Damage 400 (×1 = SUM 400), ReloadDelay 0, Burst 1, residual Δ -1297.1 (cost pinned at 1400)
- `ts_nod_artillery`: HP 25000, Speed 85, Range 12000, each offensive warhead Damage 500 (×1 = SUM 500), ReloadDelay 0, Burst 1, residual Δ -1152.1 (cost pinned at 1300)
