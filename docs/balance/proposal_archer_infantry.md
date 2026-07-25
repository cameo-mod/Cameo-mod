# Archer infantry rebalance proposal

Anchor spec: HP=20000, Speed=70, Range=7000, eff-DPS=200, Cost=500

Converter law: cost pinned, range clamped to band + made unique, eff-DPS trimmed to Δ≤1 via 2000-grid warhead Damage × 1% FirepowerMultiplier.

| actor | faction | HP | spd | rng | cost | dmg/wh×n | rl | burst | FP% | eff DPS | price | Δ | flags |
|---|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|---|
| `asianalliance_veteranarcher` | redalert2mod_asianalliance | 14000 | 68 | 7000 | 450 | 12000×6 | 68 | 3 | 100 | 529.4 | 799 | +348.9 | verifier |
| `japan_archermaiden` | redalert_japan | 20000 | 72 | 7000 | 500 | 40000×1 | 100 | 1 | 100 | 300.0 | 657 | +156.5 | anchor |
| `wc2_humans_elvenarcher` | warcraft2_humans | 25000 | 75 | 6980 | 600 | 2000×6 | 25 | 1 | 42 | 201.6 | 600 | -0.0 | shared-wpn? |
| `wc2_humans_highelvenarcher` | warcraft2_humans | 35000 | 84 | 7490 | 1100 | 2000×9 | 35 | 1 | 54 | 297.6 | 1094 | -5.7 | shared-wpn? |

**Worst |Δ| among non-anchor members: 5.7** (goal ≤1).

## Uniqueness check (5 raw stats — soft/protected excluded)

- All 5-stat uniqueness checks passed (HP, Speed, Range, raw ReloadDelay, effective damage-per-shot).

## Required YAML edits (per unit)

- `wc2_humans_elvenarcher`: HP 25000, Speed 75, Range 6980, each offensive warhead Damage 2000 (×6 = SUM 12000), ReloadDelay 25, Burst 1, FirepowerMultiplier@WC2HUMANSELVENARCHER 42
- `wc2_humans_highelvenarcher`: HP 35000, Speed 84, Range 7490, each offensive warhead Damage 2000 (×9 = SUM 18000), ReloadDelay 35, Burst 1, FirepowerMultiplier@WC2HUMANSHIGHELVENARCHER 54, residual Δ -5.7 (cost pinned at 1100)
