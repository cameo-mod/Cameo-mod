# Tank Destroyer infantry rebalance proposal

Anchor spec: HP=150000, Speed=70, Range=7500, eff-DPS=900, Cost=600

Converter law: cost pinned, range clamped to band + made unique, eff-DPS trimmed to Δ≤1 via 100-grid warhead Damage; unconditional FirepowerMultiplier is retired.

Uniqueness separates **raw warhead Damage (the 5-stat law as written)**. The levers run coarsest-first — Damage (100 grid) → Speed (1) → Range (10) — because one Damage step is a whole shot and running it last threw away everything the fine levers had achieved.

| actor | faction | HP | spd | rng | cost | dmg/wh×n | rl | burst | legacy FP% | eff DPS | price | Δ | flags |
|---|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|---|
| `ordos_tankdestroyer` | d2k_ordos | 80000 | 84 | 8500 | 2200 | 895000×1 | 72 | 1 | 100 | 12430.6 | 2200 | -0.1 |  |
| `ra2_allies_tankdestroyer` | redalert2_allies | 95000 | 65 | 7040 | 1500 | 879200×1 | 160 | 1 | 100 | 5495.0 | 1500 | -0.0 |  |
| `naxis_hetzer` | redalert2mod_naxis | 75000 | 75 | 7500 | 1300 | 0×1 | 0 | 1 | 100 | 0.0 | 182 | -1117.9 | anchor |
| `naxis_jagdpanzer` | redalert2mod_naxis | 100000 | 56 | 9000 | 2000 | 100×1 | 0 | 1 | 100 | 0.0 | 187 | -1813.3 |  |
| `ra1_allies_alliedtankdestroyer` | redalert_allies | 97500 | 60 | 6820 | 1200 | 114100×2 | 60 | 1 | 100 | 3803.3 | 1200 | +0.3 |  |

**Worst |Δ| among non-anchor members: 1813.3** (goal ≤1).

## Uniqueness check (5 raw stats — soft/protected excluded)

- All 5-stat uniqueness checks passed (HP, Speed, Range, raw ReloadDelay, effective damage-per-shot).

## Required YAML edits (per unit)

- `ordos_tankdestroyer`: HP 80000, Speed 84, Range 8500, each offensive warhead Damage 895000 (×1 = SUM 895000), ReloadDelay 72, Burst 1
- `ra2_allies_tankdestroyer`: HP 95000, Speed 65, Range 7040, each offensive warhead Damage 879200 (×1 = SUM 879200), ReloadDelay 160, Burst 1
- `naxis_jagdpanzer`: HP 100000, Speed 56, Range 9000, each offensive warhead Damage 100 (×1 = SUM 100), ReloadDelay 0, Burst 1, residual Δ -1813.3 (cost pinned at 2000)
- `ra1_allies_alliedtankdestroyer`: HP 97500, Speed 60, Range 6820, each offensive warhead Damage 114100 (×2 = SUM 228200), ReloadDelay 60, Burst 1
