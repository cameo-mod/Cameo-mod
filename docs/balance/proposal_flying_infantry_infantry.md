# Flying Infantry infantry rebalance proposal

Anchor spec: HP=18000, Speed=80, Range=5000, eff-DPS=250, Cost=600

Converter law: cost pinned, range clamped to band + made unique, eff-DPS trimmed to Δ≤1 via 2000-grid warhead Damage × 1% FirepowerMultiplier.

| actor | faction | HP | spd | rng | cost | dmg/wh×n | rl | burst | FP% | eff DPS | price | Δ | flags |
|---|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|---|
| `ra2_allies_rocketeer` | redalert2_allies | 18000 | 0 | 5000 | 600 | 8000×4 | 14 | 1 | 100 | 500.0 | 400 | -200.0 | anchor |
| `yuri_cosmonaut` | redalert2_yuri | 28000 | 0 | 5000 | 1100 | 12000×6 | 11 | 1 | 100 | 1045.5 | 755 | -344.9 | verifier |
| `naxis_skymage` | redalert2mod_naxis | 48000 | 67 | 5000 | 1200 | 2000×4 | 26 | 6 | 35 | 388.0 | 1199 | -0.5 |  |
| `japan_rocketangel` | redalert_japan | 24000 | 66 | 4590 | 900 | 2000×6 | 34 | 4 | 44 | 554.4 | 900 | +0.4 |  |
| `zerg_shriek` | starcraft_zerg | 14000 | 69 | 4000 | 500 | 2000×1 | 11 | 1 | 117 | 159.5 | 500 | +0.1 |  |
| `zerg_swarmling` | starcraft_zerg | 40000 | 70 | 5980 | 800 | 2000×8 | 80 | 1 | 58 | 119.6 | 798 | -2.3 |  |
| `cabal_cyborg_assassin` | tiberiansun_cabal | 30000 | 64 | 5970 | 1000 | 4000×1 | 18 | 2 | 92 | 350.5 | 1000 | +0.2 |  |
| `cabal_orb_drone` | tiberiansun_cabal | 25000 | 65 | 5990 | 600 | 4000×1 | 20 | 1 | 90 | 180.0 | 600 | -0.3 |  |
| `ts_gdi_jumpjetinfantry` | tiberiansun_gdi | 34000 | 68 | 5080 | 700 | 2000×2 | 25 | 1 | 115 | 184.0 | 700 | -0.0 |  |

**Worst |Δ| among non-anchor members: 2.3** (goal ≤1).

## Uniqueness check (5 raw stats — soft/protected excluded)

- All 5-stat uniqueness checks passed (HP, Speed, Range, raw ReloadDelay, effective damage-per-shot).

## Required YAML edits (per unit)

- `naxis_skymage`: HP 48000, Speed 67, Range 5000, each offensive warhead Damage 2000 (×4 = SUM 8000), ReloadDelay 26, Burst 6, FirepowerMultiplier@NAXISSKYMAGE 35
- `japan_rocketangel`: HP 24000, Speed 66, Range 4590, each offensive warhead Damage 2000 (×6 = SUM 12000), ReloadDelay 34, Burst 4, FirepowerMultiplier@JAPANROCKETANGEL 44
- `zerg_shriek`: HP 14000, Speed 69, Range 4000, each offensive warhead Damage 2000 (×1 = SUM 2000), ReloadDelay 11, Burst 1, FirepowerMultiplier@ZERGSHRIEK 117
- `zerg_swarmling`: HP 40000, Speed 70, Range 5980, each offensive warhead Damage 2000 (×8 = SUM 16000), ReloadDelay 80, Burst 1, FirepowerMultiplier@ZERGSWARMLING 58, residual Δ -2.3 (cost pinned at 800)
- `cabal_cyborg_assassin`: HP 30000, Speed 64, Range 5970, each offensive warhead Damage 4000 (×1 = SUM 4000), ReloadDelay 18, Burst 2, FirepowerMultiplier@CABALCYBORGASSASSIN 92
- `cabal_orb_drone`: HP 25000, Speed 65, Range 5990, each offensive warhead Damage 4000 (×1 = SUM 4000), ReloadDelay 20, Burst 1, FirepowerMultiplier@CABALORBDRONE 90
- `ts_gdi_jumpjetinfantry`: HP 34000, Speed 68, Range 5080, each offensive warhead Damage 2000 (×2 = SUM 4000), ReloadDelay 25, Burst 1, FirepowerMultiplier@TSGDIJUMPJETINFANTRY 115
