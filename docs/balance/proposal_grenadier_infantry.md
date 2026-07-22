# Grenadier infantry rebalance proposal

Anchor spec: HP=8000, Speed=75, Range=5500, eff-DPS=120, Cost=200

Converter law: cost pinned, range clamped to band + made unique, eff-DPS trimmed to Δ≤1 via 2000-grid warhead Damage × 1% FirepowerMultiplier.

| actor | faction | HP | spd | rng | cost | dmg/wh×n | rl | burst | FP% | eff DPS | price | Δ | flags |
|---|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|---|
| `ordos_mortartrooper` | d2k_ordos | 20000 | 40 | 5960 | 600 | 8000×4 | 80 | 1 | 98 | 392.0 | 600 | +0.2 |  |
| `steelconsortium_hoverboardgrenadier` | redalert2mod_consortium | 22000 | 130 | 5940 | 650 | 2000×4 | 25 | 1 | 35 | 119.0 | 644 | -5.8 |  |
| `latinsyndicate_grenademonkey` | redalert2mod_syndicate | 30000 | 79 | 5990 | 400 | 2000×3 | 60 | 1 | 55 | 59.6 | 399 | -1.4 |  |
| `latinsyndicate_mortarbike` | redalert2mod_syndicate | 28000 | 140 | 5980 | 500 | 2000×6 | 67 | 1 | 21 | 39.2 | 497 | -2.8 |  |
| `ra1_soviets_grenadier` | redalert_soviets | 9000 | 76 | 5800 | 200 | 2000×2 | 40 | 1 | 113 | 98.9 | 200 | -0.1 |  |
| `ra1_soviets_mortarsoldier` | redalert_soviets | 16000 | 48 | 5950 | 500 | 8000×4 | 88 | 1 | 96 | 327.3 | 500 | +0.3 |  |
| `ra1_soviets_molotovconscript` | shared_redalert | 11000 | 67 | 5500 | 200 | 16000×2 | 50 | 1 | 100 | 280.0 | 475 | +274.5 | verifier |
| `td_gdi_empgrenadier` | tiberiandawn_gdi | 32000 | 61 | 5670 | 500 | 2000×8 | 111 | 1 | 67 | 102.6 | 500 | +0.1 |  |
| `td_gdi_grenadier` | tiberiandawn_gdi | 8000 | 75 | 5500 | 200 | 16000×1 | 42 | 1 | 100 | 381.0 | 454 | +253.7 | anchor shared-wpn? |
| `forgotten_mutantmortarman` | tiberiansun_forgotten | 29000 | 50 | 6000 | 500 | 6000×2 | 88 | 1 | 113 | 173.4 | 501 | +0.8 |  |
| `ts_gdi_discthrower` | tiberiansun_gdi | 12000 | 60 | 5930 | 300 | 4000×2 | 50 | 1 | 110 | 176.0 | 300 | -0.0 | shared-wpn? |
| `wc2_humans_mortarteam` | warcraft2_humans | 40000 | 80 | 5970 | 800 | 6000×4 | 200 | 1 | 115 | 138.0 | 802 | +1.6 |  |

**Worst |Δ| among non-anchor members: 5.8** (goal ≤1).

## Uniqueness check (5 raw stats — soft/protected excluded)

- **raw ReloadDelay duplicates** (design choice — retune coarse Damage/reload by hand): {88: ['ra1_soviets_mortarsoldier', 'forgotten_mutantmortarman']}

## Required YAML edits (per unit)

- `ordos_mortartrooper`: HP 20000, Speed 40, Range 5960, each offensive warhead Damage 8000 (×4 = SUM 32000), ReloadDelay 80, Burst 1, FirepowerMultiplier@ORDOSMORTARTROOPER 98
- `steelconsortium_hoverboardgrenadier`: HP 22000, Speed 130, Range 5940, each offensive warhead Damage 2000 (×4 = SUM 8000), ReloadDelay 25, Burst 1, FirepowerMultiplier@STEELCONSORTIUMHOVERBOARDGRENADIER 35, residual Δ -5.8 (cost pinned at 650)
- `latinsyndicate_grenademonkey`: HP 30000, Speed 79, Range 5990, each offensive warhead Damage 2000 (×3 = SUM 6000), ReloadDelay 60, Burst 1, FirepowerMultiplier@LATINSYNDICATEGRENADEMONKEY 55, residual Δ -1.4 (cost pinned at 400)
- `latinsyndicate_mortarbike`: HP 28000, Speed 140, Range 5980, each offensive warhead Damage 2000 (×6 = SUM 12000), ReloadDelay 67, Burst 1, FirepowerMultiplier@LATINSYNDICATEMORTARBIKE 21, residual Δ -2.8 (cost pinned at 500)
- `ra1_soviets_grenadier`: HP 9000, Speed 76, Range 5800, each offensive warhead Damage 2000 (×2 = SUM 4000), ReloadDelay 40, Burst 1, FirepowerMultiplier@RA1SOVIETSGRENADIER 113
- `ra1_soviets_mortarsoldier`: HP 16000, Speed 48, Range 5950, each offensive warhead Damage 8000 (×4 = SUM 32000), ReloadDelay 88, Burst 1, FirepowerMultiplier@RA1SOVIETSMORTARSOLDIER 96
- `td_gdi_empgrenadier`: HP 32000, Speed 61, Range 5670, each offensive warhead Damage 2000 (×8 = SUM 16000), ReloadDelay 111, Burst 1, FirepowerMultiplier@TDGDIEMPGRENADIER 67
- `forgotten_mutantmortarman`: HP 29000, Speed 50, Range 6000, each offensive warhead Damage 6000 (×2 = SUM 12000), ReloadDelay 88, Burst 1, FirepowerMultiplier@FORGOTTENMUTANTMORTARMAN 113
- `ts_gdi_discthrower`: HP 12000, Speed 60, Range 5930, each offensive warhead Damage 4000 (×2 = SUM 8000), ReloadDelay 50, Burst 1, FirepowerMultiplier@TSGDIDISCTHROWER 110
- `wc2_humans_mortarteam`: HP 40000, Speed 80, Range 5970, each offensive warhead Damage 6000 (×4 = SUM 24000), ReloadDelay 200, Burst 1, FirepowerMultiplier@WC2HUMANSMORTARTEAM 115, residual Δ +1.6 (cost pinned at 800)
