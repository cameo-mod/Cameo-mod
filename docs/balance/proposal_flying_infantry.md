# Flying Infantry infantry rebalance proposal

Anchor spec: HP=18000, Speed=80, Range=5000, eff-DPS=250, Cost=600

Converter law: cost pinned, range clamped to band + made unique, eff-DPS trimmed to Δ≤1 via 100-grid warhead Damage; unconditional FirepowerMultiplier is retired.

Uniqueness separates **raw warhead Damage (the 5-stat law as written)**. The levers run coarsest-first — Damage (100 grid) → Speed (1) → Range (10) — because one Damage step is a whole shot and running it last threw away everything the fine levers had achieved.

| actor | faction | HP | spd | rng | cost | dmg/wh×n | rl | burst | legacy FP% | eff DPS | price | Δ | flags |
|---|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|---|
| `ra2_allies_rocketeer` | redalert2_allies | 18000 | 0 | 5000 | 600 | 8000×1 | 14 | 1 | 100 | 571.4 | 443 | -157.1 | anchor |
| `yuri_cosmonaut` | redalert2_yuri | 28000 | 90 | 4780 | 1100 | 3400×1 | 11 | 1 | 50 | 309.1 | 1100 | +0.3 | fp-debt |
| `naxis_skymage` | redalert2mod_naxis | 48000 | 80 | 4720 | 1200 | 700×3 | 26 | 6 | 50 | 273.9 | 1200 | +0.3 | fp-debt |
| `japan_rocketangel` | redalert_japan | 24000 | 75 | 4590 | 900 | 4100×1 | 34 | 4 | 50 | 410.0 | 900 | -0.1 | fp-debt |
| `zerg_shriek` | starcraft_zerg | 14000 | 95 | 4270 | 500 | 800×1 | 11 | 1 | 100 | 72.7 | 500 | +0.1 |  |
| `zerg_swarmling` | starcraft_zerg | 40000 | 95 | 5960 | 800 | 3700×1 | 80 | 1 | 100 | 46.2 | 800 | +0.1 |  |
| `cabal_cyborgassassin` | tiberiansun_cabal | 30000 | 65 | 5970 | 1000 | 2900×1 | 18 | 2 | 100 | 276.2 | 1000 | -0.5 |  |
| `cabal_orbdrone` | tiberiansun_cabal | 25000 | 70 | 5640 | 600 | 2600×1 | 20 | 1 | 100 | 130.0 | 600 | +0.0 |  |
| `ts_gdi_jumpjetinfantry` | tiberiansun_gdi | 34000 | 85 | 5080 | 700 | 2300×1 | 25 | 1 | 100 | 92.0 | 700 | +0.2 |  |

**Worst |Δ| among non-anchor members: 0.5** (goal ≤1).

## Uniqueness check (5 raw stats — soft/protected excluded)

- **Speed duplicates**: {95: ['zerg_shriek', 'zerg_swarmling']}
- **raw ReloadDelay duplicates** (design choice — retune coarse Damage/reload by hand): {11: ['yuri_cosmonaut', 'zerg_shriek']}

## Required YAML edits (per unit)

- `yuri_cosmonaut`: HP 28000, Speed 90, Range 4780, each offensive warhead Damage 3400 (×1 = SUM 3400), ReloadDelay 11, Burst 1, **DELETE the unconditional FirepowerMultiplier (50%)** — the Damage above already includes it (W17)
- `naxis_skymage`: HP 48000, Speed 80, Range 4720, each offensive warhead Damage 700 (×3 = SUM 2100), ReloadDelay 26, Burst 6, **DELETE the unconditional FirepowerMultiplier (50%)** — the Damage above already includes it (W17)
- `japan_rocketangel`: HP 24000, Speed 75, Range 4590, each offensive warhead Damage 4100 (×1 = SUM 4100), ReloadDelay 34, Burst 4, **DELETE the unconditional FirepowerMultiplier (50%)** — the Damage above already includes it (W17)
- `zerg_shriek`: HP 14000, Speed 95, Range 4270, each offensive warhead Damage 800 (×1 = SUM 800), ReloadDelay 11, Burst 1
- `zerg_swarmling`: HP 40000, Speed 95, Range 5960, each offensive warhead Damage 3700 (×1 = SUM 3700), ReloadDelay 80, Burst 1
- `cabal_cyborgassassin`: HP 30000, Speed 65, Range 5970, each offensive warhead Damage 2900 (×1 = SUM 2900), ReloadDelay 18, Burst 2
- `cabal_orbdrone`: HP 25000, Speed 70, Range 5640, each offensive warhead Damage 2600 (×1 = SUM 2600), ReloadDelay 20, Burst 1
- `ts_gdi_jumpjetinfantry`: HP 34000, Speed 85, Range 5080, each offensive warhead Damage 2300 (×1 = SUM 2300), ReloadDelay 25, Burst 1
