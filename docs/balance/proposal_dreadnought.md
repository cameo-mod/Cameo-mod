# Dreadnought infantry rebalance proposal

Anchor spec: HP=1150000, Speed=50, Range=7000, eff-DPS=3750, Cost=3000

Converter law: cost pinned, range clamped to band + made unique, eff-DPS trimmed to Δ≤1 via 100-grid warhead Damage; unconditional FirepowerMultiplier is retired.

Uniqueness separates **raw warhead Damage (the 5-stat law as written)**. The levers run coarsest-first — Damage (100 grid) → Speed (1) → Range (10) — because one Damage step is a whole shot and running it last threw away everything the fine levers had achieved.

| actor | faction | HP | spd | rng | cost | dmg/wh×n | rl | burst | legacy FP% | eff DPS | price | Δ | flags |
|---|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|---|
| `ixian_neocymek` | d2k_ixian | 97500 | 45 | 7010 | 4500 | 100×1 | 0 | 1 | 50 | 0.0 | 597 | -3902.7 | fp-debt |
| `asianalliance_pulverizermecha` | redalert2mod_asianalliance | 100000 | 55 | 7020 | 3000 | 200×1 | 0 | 1 | 100 | 0.0 | 658 | -2342.0 |  |
| `naxis_sturmtiger` | redalert2mod_naxis | 95000 | 40 | 6990 | 2500 | 300×1 | 0 | 1 | 100 | 0.0 | 325 | -2175.2 |  |
| `schwarzermond_neojagdpanzer` | redalert2mod_schwarzermond | 92500 | 50 | 8400 | 4500 | 400×1 | 0 | 1 | 100 | 0.0 | 610 | -3889.7 |  |
| `terran_warhound` | starcraft_terran | 300000 | 45 | 7000 | 4500 | 0×1 | 0 | 1 | 100 | 0.0 | 658 | -3842.4 | anchor |

**Worst |Δ| among non-anchor members: 3902.7** (goal ≤1).

## Uniqueness check (5 raw stats — soft/protected excluded)

- **raw ReloadDelay duplicates** (design choice — retune coarse Damage/reload by hand): {0: ['ixian_neocymek', 'asianalliance_pulverizermecha', 'naxis_sturmtiger', 'schwarzermond_neojagdpanzer']}

## Required YAML edits (per unit)

- `ixian_neocymek`: HP 97500, Speed 45, Range 7010, each offensive warhead Damage 100 (×1 = SUM 100), ReloadDelay 0, Burst 1, **DELETE the unconditional FirepowerMultiplier (50%)** — the Damage above already includes it (W17), residual Δ -3902.7 (cost pinned at 4500)
- `asianalliance_pulverizermecha`: HP 100000, Speed 55, Range 7020, each offensive warhead Damage 200 (×1 = SUM 200), ReloadDelay 0, Burst 1, residual Δ -2342.0 (cost pinned at 3000)
- `naxis_sturmtiger`: HP 95000, Speed 40, Range 6990, each offensive warhead Damage 300 (×1 = SUM 300), ReloadDelay 0, Burst 1, residual Δ -2175.2 (cost pinned at 2500)
- `schwarzermond_neojagdpanzer`: HP 92500, Speed 50, Range 8400, each offensive warhead Damage 400 (×1 = SUM 400), ReloadDelay 0, Burst 1, residual Δ -3889.7 (cost pinned at 4500)
