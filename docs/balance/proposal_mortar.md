# Mortar infantry rebalance proposal

Anchor spec: HP=30000, Speed=50, Range=10000, eff-DPS=400, Cost=500

Converter law: cost pinned, range clamped to band + made unique, eff-DPS trimmed to Δ≤1 via 100-grid warhead Damage; unconditional FirepowerMultiplier is retired.

Uniqueness separates **raw warhead Damage (the 5-stat law as written)**. The levers run coarsest-first — Damage (100 grid) → Speed (1) → Range (10) — because one Damage step is a whole shot and running it last threw away everything the fine levers had achieved.

| actor | faction | HP | spd | rng | cost | dmg/wh×n | rl | burst | legacy FP% | eff DPS | price | Δ | flags |
|---|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|---|
| `ordos_mortartrooper` | d2k_ordos | 20000 | 40 | 9890 | 600 | 17100×4 | 80 | 1 | 100 | 855.0 | 600 | -0.1 |  |
| `latinsyndicate_mortarbike` | redalert2mod_syndicate | 27500 | 60 | 9030 | 500 | 13400×2 | 67 | 1 | 50 | 400.0 | 500 | -0.0 | fp-debt |
| `ra1_soviets_mortarsoldier` | redalert_soviets | 16000 | 48 | 10830 | 500 | 54900×1 | 88 | 1 | 75 | 623.9 | 500 | -0.1 | fp-debt |
| `forgotten_mutantmortarman` | tiberiansun_forgotten | 30000 | 50 | 10000 | 500 | 32000×2 | 88 | 1 | 100 | 363.6 | 473 | -26.5 | anchor |
| `wc2_humans_mortarteam` | warcraft2_humans | 40000 | 60 | 11000 | 800 | 96600×1 | 200 | 1 | 100 | 483.0 | 800 | -0.1 |  |

**Worst |Δ| among non-anchor members: 0.1** (goal ≤1).

## Uniqueness check (5 raw stats — soft/protected excluded)

- **Speed duplicates**: {60: ['latinsyndicate_mortarbike', 'wc2_humans_mortarteam']}

## Required YAML edits (per unit)

- `ordos_mortartrooper`: HP 20000, Speed 40, Range 9890, each offensive warhead Damage 17100 (×4 = SUM 68400), ReloadDelay 80, Burst 1
- `latinsyndicate_mortarbike`: HP 27500, Speed 60, Range 9030, each offensive warhead Damage 13400 (×2 = SUM 26800), ReloadDelay 67, Burst 1, **DELETE the unconditional FirepowerMultiplier (50%)** — the Damage above already includes it (W17)
- `ra1_soviets_mortarsoldier`: HP 16000, Speed 48, Range 10830, each offensive warhead Damage 54900 (×1 = SUM 54900), ReloadDelay 88, Burst 1, **DELETE the unconditional FirepowerMultiplier (75%)** — the Damage above already includes it (W17)
- `wc2_humans_mortarteam`: HP 40000, Speed 60, Range 11000, each offensive warhead Damage 96600 (×1 = SUM 96600), ReloadDelay 200, Burst 1
