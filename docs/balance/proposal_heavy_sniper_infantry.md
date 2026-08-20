# Heavy Sniper infantry rebalance proposal

Anchor spec: HP=25000, Speed=80, Range=8000, eff-DPS=400, Cost=700

Converter law: cost pinned, range clamped to band + made unique, eff-DPS trimmed to Δ≤1 via 2000-grid warhead Damage × 1% FirepowerMultiplier.

| actor | faction | HP | spd | rng | cost | dmg/wh×n | rl | burst | FP% | eff DPS | price | Δ | flags |
|---|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|---|
| `yuri_virus` | redalert2_yuri | 12000 | 52 | 8000 | 700 | 24000×2 | 74 | 1 | 100 | 324.3 | 415 | -285.4 | verifier |
| `ra1_soviets_dragunovantimaterialsniper` | redalert_soviets | 20000 | 64 | 7030 | 422 | 8000×5 | 85 | 1 | 94 | 470.0 | 422 | -0.2 |  |

**Worst |Δ| among non-anchor members: 0.2** (goal ≤1).

## Uniqueness check (5 raw stats — soft/protected excluded)

- All 5-stat uniqueness checks passed (HP, Speed, Range, raw ReloadDelay, effective damage-per-shot).

## Required YAML edits (per unit)

- `ra1_soviets_dragunovantimaterialsniper`: HP 20000, Speed 64, Range 7030, each offensive warhead Damage 8000 (×5 = SUM 40000), ReloadDelay 85, Burst 1, FirepowerMultiplier@RA1SOVIETSDRAGUNOVANTIMATERIALSNIPER 94
