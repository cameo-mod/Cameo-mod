# Grenadier infantry rebalance proposal

Anchor spec: HP=8000, Speed=75, Range=5500, eff-DPS=120, Cost=200

Converter law: cost pinned, range clamped to band + made unique, eff-DPS trimmed to Δ≤1 via 2000-grid warhead Damage × 1% FirepowerMultiplier.

| actor | faction | HP | spd | rng | cost | dmg/wh×n | rl | burst | FP% | eff DPS | price | Δ | flags |
|---|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|---|
| `steelconsortium_hoverboardgrenadier` | redalert2mod_consortium | 22000 | 90 | 5990 | 650 | 2000×4 | 25 | 1 | 55 | 187.0 | 650 | -0.1 |  |
| `latinsyndicate_grenademonkey` | redalert2mod_syndicate | 30000 | 80 | 6000 | 400 | 2000×3 | 60 | 1 | 54 | 58.5 | 399 | -0.5 |  |
| `ra1_soviets_grenadier` | redalert_soviets | 9000 | 75 | 5790 | 200 | 2000×2 | 40 | 1 | 115 | 100.6 | 200 | +0.1 |  |
| `ra1_soviets_molotovconscript` | shared_redalert | 11000 | 67 | 5500 | 200 | 16000×2 | 50 | 1 | 100 | 280.0 | 475 | +274.5 | verifier |
| `td_gdi_empgrenadier` | tiberiandawn_gdi | 32000 | 60 | 5700 | 500 | 2000×8 | 111 | 1 | 68 | 104.1 | 500 | +0.0 |  |
| `td_gdi_grenadier` | tiberiandawn_gdi | 8000 | 75 | 5500 | 200 | 16000×1 | 42 | 1 | 100 | 381.0 | 454 | +253.7 | anchor shared-wpn? |
| `ts_gdi_discthrower` | tiberiansun_gdi | 12000 | 61 | 5950 | 300 | 4000×2 | 50 | 1 | 108 | 172.8 | 300 | -0.1 | shared-wpn? |

**Worst |Δ| among non-anchor members: 0.5** (goal ≤1).

## Uniqueness check (5 raw stats — soft/protected excluded)

- All 5-stat uniqueness checks passed (HP, Speed, Range, raw ReloadDelay, effective damage-per-shot).

## Required YAML edits (per unit)

- `steelconsortium_hoverboardgrenadier`: HP 22000, Speed 90, Range 5990, each offensive warhead Damage 2000 (×4 = SUM 8000), ReloadDelay 25, Burst 1, FirepowerMultiplier@STEELCONSORTIUMHOVERBOARDGRENADIER 55
- `latinsyndicate_grenademonkey`: HP 30000, Speed 80, Range 6000, each offensive warhead Damage 2000 (×3 = SUM 6000), ReloadDelay 60, Burst 1, FirepowerMultiplier@LATINSYNDICATEGRENADEMONKEY 54
- `ra1_soviets_grenadier`: HP 9000, Speed 75, Range 5790, each offensive warhead Damage 2000 (×2 = SUM 4000), ReloadDelay 40, Burst 1, FirepowerMultiplier@RA1SOVIETSGRENADIER 115
- `td_gdi_empgrenadier`: HP 32000, Speed 60, Range 5700, each offensive warhead Damage 2000 (×8 = SUM 16000), ReloadDelay 111, Burst 1, FirepowerMultiplier@TDGDIEMPGRENADIER 68
- `ts_gdi_discthrower`: HP 12000, Speed 61, Range 5950, each offensive warhead Damage 4000 (×2 = SUM 8000), ReloadDelay 50, Burst 1, FirepowerMultiplier@TSGDIDISCTHROWER 108
