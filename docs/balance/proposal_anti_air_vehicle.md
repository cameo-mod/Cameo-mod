# Anti Air Vehicle infantry rebalance proposal

Anchor spec: HP=170000, Speed=110, Range=6000, eff-DPS=1250, Cost=1000

Converter law: cost pinned, range clamped to band + made unique, eff-DPS trimmed to Δ≤1 via 100-grid warhead Damage; unconditional FirepowerMultiplier is retired.

Uniqueness separates **raw warhead Damage (the 5-stat law as written)**. The levers run coarsest-first — Damage (100 grid) → Speed (1) → Range (10) — because one Damage step is a whole shot and running it last threw away everything the fine levers had achieved.

| actor | faction | HP | spd | rng | cost | dmg/wh×n | rl | burst | legacy FP% | eff DPS | price | Δ | flags |
|---|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|---|
| `ra2_soviets_flaktrack` | redalert2_soviets | 47500 | 110 | 5980 | 900 | 100×1 | 0 | 1 | 75 | 0.0 | 236 | -663.8 | fp-debt |
| `yuri_gatlingtank` | redalert2_yuri | 40000 | 125 | 5960 | 1100 | 200×1 | 0 | 1 | 75 | 0.0 | 262 | -837.7 | fp-debt |
| `asianalliance_pulverizer` | redalert2mod_asianalliance | 85000 | 90 | 6060 | 1400 | 300×1 | 0 | 1 | 100 | 0.0 | 283 | -1116.8 |  |
| `steelconsortium_barracuda` | redalert2mod_consortium | 37500 | 125 | 5970 | 1100 | 400×1 | 0 | 1 | 50 | 0.0 | 238 | -862.2 | fp-debt |
| `naxis_kubelwagen` | redalert2mod_naxis | 42500 | 115 | 6040 | 850 | 500×1 | 0 | 1 | 100 | 0.0 | 203 | -646.6 |  |
| `wirbelwind.nax` | redalert2mod_naxis | 87500 | 130 | 5360 | 1800 | 16700×3 | 47 | 6 | 100 | 4486.6 | 1801 | +0.8 |  |
| `latinsyndicate_diablo` | redalert2mod_syndicate | 45000 | 125 | 6000 | 1200 | 8000×1 | 16 | 1 | 100 | 500.0 | 390 | -809.7 | anchor |
| `tkm_flakbus` | redalert2mod_tkm | 100000 | 130 | 7200 | 1800 | 600×1 | 0 | 1 | 100 | 0.0 | 180 | -1620.0 |  |
| `ra1_allies_alliedheavyaatank` | redalert_allies | 97500 | 100 | 6030 | 1250 | 700×1 | 0 | 1 | 25 | 0.0 | 315 | -934.9 | fp-debt |
| `japan_armoredcar` | redalert_japan | 50000 | 130 | 6050 | 700 | 800×1 | 0 | 1 | 125 | 0.0 | 265 | -435.0 | fp-debt |
| `ra1_soviets_flaktruck` | redalert_soviets | 30000 | 120 | 6010 | 800 | 900×1 | 0 | 1 | 100 | 0.0 | 221 | -578.8 |  |
| `ra1_soviets_gatlingtank` | redalert_soviets | 75000 | 105 | 5990 | 1100 | 1000×1 | 0 | 1 | 100 | 0.0 | 241 | -859.5 |  |
| `forgotten_m113adats` | tiberiansun_forgotten | 55000 | 95 | 5600 | 950 | 20900×2 | 54 | 4 | 100 | 2786.7 | 950 | -0.4 |  |

**Worst |Δ| among non-anchor members: 1620.0** (goal ≤1).

## Uniqueness check (5 raw stats — soft/protected excluded)

- **Speed duplicates**: {125: ['yuri_gatlingtank', 'steelconsortium_barracuda'], 130: ['wirbelwind.nax', 'tkm_flakbus', 'japan_armoredcar']}
- **raw ReloadDelay duplicates** (design choice — retune coarse Damage/reload by hand): {0: ['ra2_soviets_flaktrack', 'yuri_gatlingtank', 'asianalliance_pulverizer', 'steelconsortium_barracuda', 'naxis_kubelwagen', 'tkm_flakbus', 'ra1_allies_alliedheavyaatank', 'japan_armoredcar', 'ra1_soviets_flaktruck', 'ra1_soviets_gatlingtank']}

## Required YAML edits (per unit)

- `ra2_soviets_flaktrack`: HP 47500, Speed 110, Range 5980, each offensive warhead Damage 100 (×1 = SUM 100), ReloadDelay 0, Burst 1, **DELETE the unconditional FirepowerMultiplier (75%)** — the Damage above already includes it (W17), residual Δ -663.8 (cost pinned at 900)
- `yuri_gatlingtank`: HP 40000, Speed 125, Range 5960, each offensive warhead Damage 200 (×1 = SUM 200), ReloadDelay 0, Burst 1, **DELETE the unconditional FirepowerMultiplier (75%)** — the Damage above already includes it (W17), residual Δ -837.7 (cost pinned at 1100)
- `asianalliance_pulverizer`: HP 85000, Speed 90, Range 6060, each offensive warhead Damage 300 (×1 = SUM 300), ReloadDelay 0, Burst 1, residual Δ -1116.8 (cost pinned at 1400)
- `steelconsortium_barracuda`: HP 37500, Speed 125, Range 5970, each offensive warhead Damage 400 (×1 = SUM 400), ReloadDelay 0, Burst 1, **DELETE the unconditional FirepowerMultiplier (50%)** — the Damage above already includes it (W17), residual Δ -862.2 (cost pinned at 1100)
- `naxis_kubelwagen`: HP 42500, Speed 115, Range 6040, each offensive warhead Damage 500 (×1 = SUM 500), ReloadDelay 0, Burst 1, residual Δ -646.6 (cost pinned at 850)
- `wirbelwind.nax`: HP 87500, Speed 130, Range 5360, each offensive warhead Damage 16700 (×3 = SUM 50100), ReloadDelay 47, Burst 6
- `tkm_flakbus`: HP 100000, Speed 130, Range 7200, each offensive warhead Damage 600 (×1 = SUM 600), ReloadDelay 0, Burst 1, residual Δ -1620.0 (cost pinned at 1800)
- `ra1_allies_alliedheavyaatank`: HP 97500, Speed 100, Range 6030, each offensive warhead Damage 700 (×1 = SUM 700), ReloadDelay 0, Burst 1, **DELETE the unconditional FirepowerMultiplier (25%)** — the Damage above already includes it (W17), residual Δ -934.9 (cost pinned at 1250)
- `japan_armoredcar`: HP 50000, Speed 130, Range 6050, each offensive warhead Damage 800 (×1 = SUM 800), ReloadDelay 0, Burst 1, **DELETE the unconditional FirepowerMultiplier (125%)** — the Damage above already includes it (W17), residual Δ -435.0 (cost pinned at 700)
- `ra1_soviets_flaktruck`: HP 30000, Speed 120, Range 6010, each offensive warhead Damage 900 (×1 = SUM 900), ReloadDelay 0, Burst 1, residual Δ -578.8 (cost pinned at 800)
- `ra1_soviets_gatlingtank`: HP 75000, Speed 105, Range 5990, each offensive warhead Damage 1000 (×1 = SUM 1000), ReloadDelay 0, Burst 1, residual Δ -859.5 (cost pinned at 1100)
- `forgotten_m113adats`: HP 55000, Speed 95, Range 5600, each offensive warhead Damage 20900 (×2 = SUM 41800), ReloadDelay 54, Burst 4
