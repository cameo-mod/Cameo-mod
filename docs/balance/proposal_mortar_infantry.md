# Mortar infantry rebalance proposal

Anchor spec: HP=30000, Speed=50, Range=10000, eff-DPS=409, Cost=500

Converter law: cost pinned, range clamped to band + made unique, eff-DPS trimmed to Δ≤1 via 2000-grid warhead Damage × 1% FirepowerMultiplier.

| actor | faction | HP | spd | rng | cost | dmg/wh×n | rl | burst | FP% | eff DPS | price | Δ | flags |
|---|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|---|
| `ordos_mortartrooper` | d2k_ordos | 20000 | 40 | 10000 | 600 | 40000×4 | 80 | 1 | 100 | 500.0 | 409 | -191.3 | verifier |
| `latinsyndicate_mortarbike` | redalert2mod_syndicate | 28000 | 60 | 9000 | 500 | 4000×6 | 67 | 1 | 108 | 403.0 | 500 | +0.2 |  |
| `ra1_soviets_mortarsoldier` | redalert_soviets | 16000 | 48 | 10830 | 500 | 14000×4 | 88 | 1 | 107 | 638.4 | 500 | +0.1 |  |
| `forgotten_mutantmortarman` | tiberiansun_forgotten | 30000 | 50 | 10000 | 500 | 32000×2 | 88 | 1 | 100 | 409.1 | 500 | +0.1 | anchor |
| `wc2_humans_mortarteam` | warcraft2_humans | 40000 | 60 | 10990 | 800 | 24000×4 | 200 | 1 | 103 | 494.4 | 800 | +0.0 |  |

**Worst |Δ| among non-anchor members: 0.2** (goal ≤1).

## Uniqueness check (5 raw stats — soft/protected excluded)

- **Speed duplicates**: {60: ['latinsyndicate_mortarbike', 'wc2_humans_mortarteam']}

## Required YAML edits (per unit)

- `latinsyndicate_mortarbike`: HP 28000, Speed 60, Range 9000, each offensive warhead Damage 4000 (×6 = SUM 24000), ReloadDelay 67, Burst 1, FirepowerMultiplier@LATINSYNDICATEMORTARBIKE 108
- `ra1_soviets_mortarsoldier`: HP 16000, Speed 48, Range 10830, each offensive warhead Damage 14000 (×4 = SUM 56000), ReloadDelay 88, Burst 1, FirepowerMultiplier@RA1SOVIETSMORTARSOLDIER 107
- `wc2_humans_mortarteam`: HP 40000, Speed 60, Range 10990, each offensive warhead Damage 24000 (×4 = SUM 96000), ReloadDelay 200, Burst 1, FirepowerMultiplier@WC2HUMANSMORTARTEAM 103
