# Grenadier infantry rebalance proposal

Anchor spec: HP=8000, Speed=75, Range=5500, eff-DPS=120, Cost=200

Converter law: cost pinned, range clamped to band + made unique, eff-DPS trimmed to Δ≤1 via 2000-grid warhead Damage × 1% FirepowerMultiplier.

| actor | faction | HP | spd | rng | cost | dmg/wh×n | rl | burst | FP% | eff DPS | price | Δ | flags |
|---|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|---|
| `ordos_mortartrooper` | d2k_ordos | 20000 | 61 | 5970 | 600 | 6000×4 | 80 | 1 | 92 | 276.0 | 601 | +1.1 |  |
| `steelconsortium_hoverboardgrenadier` | redalert2mod_consortium | 22000 | 90 | 5950 | 650 | 2000×4 | 25 | 1 | 55 | 187.0 | 647 | -3.1 |  |
| `latinsyndicate_grenademonkey` | redalert2mod_syndicate | 30000 | 80 | 5990 | 400 | 2000×3 | 60 | 1 | 54 | 58.5 | 399 | -0.8 |  |
| `latinsyndicate_mortarbike` | redalert2mod_syndicate | 28000 | 90 | 5980 | 500 | 2000×6 | 67 | 1 | 47 | 87.7 | 498 | -2.4 |  |
| `ra1_soviets_grenadier` | redalert_soviets | 9000 | 75 | 5790 | 200 | 2000×2 | 40 | 1 | 115 | 100.6 | 200 | +0.1 |  |
| `ra1_soviets_mortarsoldier` | redalert_soviets | 16000 | 62 | 5960 | 500 | 6000×4 | 88 | 1 | 103 | 263.4 | 498 | -1.7 |  |
| `ra1_soviets_molotovconscript` | shared_redalert | 11000 | 67 | 5500 | 200 | 16000×2 | 50 | 1 | 100 | 280.0 | 475 | +274.5 | verifier |
| `td_gdi_empgrenadier` | tiberiandawn_gdi | 32000 | 63 | 5700 | 500 | 2000×8 | 111 | 1 | 64 | 98.0 | 500 | +0.2 |  |
| `td_gdi_grenadier` | tiberiandawn_gdi | 8000 | 75 | 5500 | 200 | 16000×1 | 42 | 1 | 100 | 381.0 | 454 | +253.7 | anchor shared-wpn? |
| `forgotten_mutantmortarman` | tiberiansun_forgotten | 29000 | 60 | 6000 | 500 | 6000×2 | 88 | 1 | 92 | 141.1 | 499 | -1.0 |  |
| `ts_gdi_discthrower` | tiberiansun_gdi | 12000 | 64 | 5920 | 300 | 4000×2 | 50 | 1 | 104 | 166.4 | 300 | +0.1 | shared-wpn? |
| `wc2_humans_mortarteam` | warcraft2_humans | 40000 | 81 | 5910 | 800 | 6000×4 | 200 | 1 | 114 | 136.8 | 800 | +0.1 |  |

**Worst |Δ| among non-anchor members: 3.1** (goal ≤1).

## Uniqueness check (5 raw stats — soft/protected excluded)

- **Speed duplicates**: {90: ['steelconsortium_hoverboardgrenadier', 'latinsyndicate_mortarbike']}
- **raw ReloadDelay duplicates** (design choice — retune coarse Damage/reload by hand): {88: ['ra1_soviets_mortarsoldier', 'forgotten_mutantmortarman']}

## Required YAML edits (per unit)

- `ordos_mortartrooper`: HP 20000, Speed 61, Range 5970, each offensive warhead Damage 6000 (×4 = SUM 24000), ReloadDelay 80, Burst 1, FirepowerMultiplier@ORDOSMORTARTROOPER 92, residual Δ +1.1 (cost pinned at 600)
- `steelconsortium_hoverboardgrenadier`: HP 22000, Speed 90, Range 5950, each offensive warhead Damage 2000 (×4 = SUM 8000), ReloadDelay 25, Burst 1, FirepowerMultiplier@STEELCONSORTIUMHOVERBOARDGRENADIER 55, residual Δ -3.1 (cost pinned at 650)
- `latinsyndicate_grenademonkey`: HP 30000, Speed 80, Range 5990, each offensive warhead Damage 2000 (×3 = SUM 6000), ReloadDelay 60, Burst 1, FirepowerMultiplier@LATINSYNDICATEGRENADEMONKEY 54
- `latinsyndicate_mortarbike`: HP 28000, Speed 90, Range 5980, each offensive warhead Damage 2000 (×6 = SUM 12000), ReloadDelay 67, Burst 1, FirepowerMultiplier@LATINSYNDICATEMORTARBIKE 47, residual Δ -2.4 (cost pinned at 500)
- `ra1_soviets_grenadier`: HP 9000, Speed 75, Range 5790, each offensive warhead Damage 2000 (×2 = SUM 4000), ReloadDelay 40, Burst 1, FirepowerMultiplier@RA1SOVIETSGRENADIER 115
- `ra1_soviets_mortarsoldier`: HP 16000, Speed 62, Range 5960, each offensive warhead Damage 6000 (×4 = SUM 24000), ReloadDelay 88, Burst 1, FirepowerMultiplier@RA1SOVIETSMORTARSOLDIER 103, residual Δ -1.7 (cost pinned at 500)
- `td_gdi_empgrenadier`: HP 32000, Speed 63, Range 5700, each offensive warhead Damage 2000 (×8 = SUM 16000), ReloadDelay 111, Burst 1, FirepowerMultiplier@TDGDIEMPGRENADIER 64
- `forgotten_mutantmortarman`: HP 29000, Speed 60, Range 6000, each offensive warhead Damage 6000 (×2 = SUM 12000), ReloadDelay 88, Burst 1, FirepowerMultiplier@FORGOTTENMUTANTMORTARMAN 92
- `ts_gdi_discthrower`: HP 12000, Speed 64, Range 5920, each offensive warhead Damage 4000 (×2 = SUM 8000), ReloadDelay 50, Burst 1, FirepowerMultiplier@TSGDIDISCTHROWER 104
- `wc2_humans_mortarteam`: HP 40000, Speed 81, Range 5910, each offensive warhead Damage 6000 (×4 = SUM 24000), ReloadDelay 200, Burst 1, FirepowerMultiplier@WC2HUMANSMORTARTEAM 114
