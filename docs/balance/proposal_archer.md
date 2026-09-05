# Archer infantry rebalance proposal

Anchor spec: HP=20000, Speed=70, Range=7000, eff-DPS=200, Cost=500

Converter law: cost pinned, range clamped to band + made unique, eff-DPS trimmed to Δ≤1 via 100-grid warhead Damage; unconditional FirepowerMultiplier is retired.

Uniqueness separates **raw warhead Damage (the 5-stat law as written)**. The levers run coarsest-first — Damage (100 grid) → Speed (1) → Range (10) — because one Damage step is a whole shot and running it last threw away everything the fine levers had achieved.

| actor | faction | HP | spd | rng | cost | dmg/wh×n | rl | burst | legacy FP% | eff DPS | price | Δ | flags |
|---|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|---|
| `asianalliance_veteranarcher` | redalert2mod_asianalliance | 14000 | 68 | 6820 | 450 | 1100×5 | 68 | 3 | 100 | 242.6 | 450 | -0.1 |  |
| `japan_archermaiden` | redalert_japan | 20000 | 72 | 7000 | 500 | 40000×1 | 100 | 1 | 100 | 400.0 | 805 | +304.8 | anchor |
| `wc2_humans_elvenarcher` | warcraft2_humans | 25000 | 75 | 7040 | 600 | 5000×1 | 25 | 1 | 75 | 200.0 | 600 | +0.2 | shared-wpn? fp-debt |
| `wc2_humans_highelvenarcher` | warcraft2_humans | 35000 | 82 | 7320 | 1100 | 5500×2 | 35 | 1 | 100 | 314.3 | 1100 | -0.0 | shared-wpn? |

**Worst |Δ| among non-anchor members: 0.2** (goal ≤1).

## Uniqueness check (5 raw stats — soft/protected excluded)

- All 5-stat uniqueness checks passed (HP, Speed, Range, raw ReloadDelay, effective damage-per-shot).

## Required YAML edits (per unit)

- `asianalliance_veteranarcher`: HP 14000, Speed 68, Range 6820, each offensive warhead Damage 1100 (×5 = SUM 5500), ReloadDelay 68, Burst 3
- `wc2_humans_elvenarcher`: HP 25000, Speed 75, Range 7040, each offensive warhead Damage 5000 (×1 = SUM 5000), ReloadDelay 25, Burst 1, **DELETE the unconditional FirepowerMultiplier (75%)** — the Damage above already includes it (W17)
- `wc2_humans_highelvenarcher`: HP 35000, Speed 82, Range 7320, each offensive warhead Damage 5500 (×2 = SUM 11000), ReloadDelay 35, Burst 1
