# Grenadier infantry rebalance proposal

Anchor spec: HP=8000, Speed=75, Range=5500, eff-DPS=120, Cost=200

Converter law: cost pinned, range clamped to band + made unique, eff-DPS trimmed to Δ≤1 via 100-grid warhead Damage; unconditional FirepowerMultiplier is retired.

Uniqueness separates **raw warhead Damage (the 5-stat law as written)**. The levers run coarsest-first — Damage (100 grid) → Speed (1) → Range (10) — because one Damage step is a whole shot and running it last threw away everything the fine levers had achieved.

| actor | faction | HP | spd | rng | cost | dmg/wh×n | rl | burst | legacy FP% | eff DPS | price | Δ | flags |
|---|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|---|
| `steelconsortium_hoverboardgrenadier` | redalert2mod_consortium | 22000 | 90 | 5830 | 650 | 1200×4 | 25 | 1 | 550 | 192.0 | 650 | -0.2 | fp-debt |
| `latinsyndicate_grenademonkey` | redalert2mod_syndicate | 30000 | 80 | 5880 | 400 | 1200×3 | 60 | 1 | 100 | 60.0 | 400 | +0.1 |  |
| `ra1_soviets_grenadier` | redalert_soviets | 9000 | 75 | 5820 | 200 | 2000×2 | 40 | 1 | 100 | 100.0 | 200 | -0.0 |  |
| `ra1_soviets_molotovconscript` | shared_redalert | 11000 | 67 | 5430 | 200 | 1900×2 | 50 | 1 | 100 | 76.0 | 200 | -0.1 |  |
| `td_gdi_empgrenadier` | tiberiandawn_gdi | 32000 | 60 | 5630 | 500 | 3900×3 | 111 | 1 | 120 | 105.4 | 500 | -0.1 | fp-debt |
| `td_gdi_grenadier` | tiberiandawn_gdi | 8000 | 75 | 5500 | 200 | 16000×1 | 42 | 1 | 100 | 381.0 | 454 | +253.7 | anchor shared-wpn? |
| `ts_gdi_discthrower` | tiberiansun_gdi | 12000 | 61 | 5980 | 300 | 4300×2 | 50 | 1 | 100 | 172.0 | 300 | -0.1 | shared-wpn? |

**Worst |Δ| among non-anchor members: 0.2** (goal ≤1).

## Uniqueness check (5 raw stats — soft/protected excluded)

- **raw ReloadDelay duplicates** (design choice — retune coarse Damage/reload by hand): {50: ['ra1_soviets_molotovconscript', 'ts_gdi_discthrower']}

## Required YAML edits (per unit)

- `steelconsortium_hoverboardgrenadier`: HP 22000, Speed 90, Range 5830, each offensive warhead Damage 1200 (×4 = SUM 4800), ReloadDelay 25, Burst 1, **DELETE the unconditional FirepowerMultiplier (550%)** — the Damage above already includes it (W17)
- `latinsyndicate_grenademonkey`: HP 30000, Speed 80, Range 5880, each offensive warhead Damage 1200 (×3 = SUM 3600), ReloadDelay 60, Burst 1
- `ra1_soviets_grenadier`: HP 9000, Speed 75, Range 5820, each offensive warhead Damage 2000 (×2 = SUM 4000), ReloadDelay 40, Burst 1
- `ra1_soviets_molotovconscript`: HP 11000, Speed 67, Range 5430, each offensive warhead Damage 1900 (×2 = SUM 3800), ReloadDelay 50, Burst 1
- `td_gdi_empgrenadier`: HP 32000, Speed 60, Range 5630, each offensive warhead Damage 3900 (×3 = SUM 11700), ReloadDelay 111, Burst 1, **DELETE the unconditional FirepowerMultiplier (120%)** — the Damage above already includes it (W17)
- `ts_gdi_discthrower`: HP 12000, Speed 61, Range 5980, each offensive warhead Damage 4300 (×2 = SUM 8600), ReloadDelay 50, Burst 1
