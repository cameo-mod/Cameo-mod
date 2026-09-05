# Closecombat infantry rebalance proposal

Anchor spec: HP=50000, Speed=75, Range=3500, eff-DPS=250, Cost=200

Converter law: cost pinned, range clamped to band + made unique, eff-DPS trimmed to Δ≤1 via 100-grid warhead Damage; unconditional FirepowerMultiplier is retired.

Uniqueness separates **raw warhead Damage (the 5-stat law as written)**. The levers run coarsest-first — Damage (100 grid) → Speed (1) → Range (10) — because one Damage step is a whole shot and running it last threw away everything the fine levers had achieved.

| actor | faction | HP | spd | rng | cost | dmg/wh×n | rl | burst | legacy FP% | eff DPS | price | Δ | flags |
|---|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|---|
| `asianalliance_fanatic` | redalert2mod_asianalliance | 100000 | 75 | 3500 | 500 | 3500×1 | 70 | 10 | 100 | 500.0 | 500 | +0.0 |  |
| `futuretech_shotgundroid` | redalert2mod_futuretech | 55000 | 60 | 4110 | 400 | 20300×3 | 55 | 1 | 100 | 1107.3 | 400 | +0.1 |  |
| `naxis_sssoldier` | redalert2mod_naxis | 63000 | 61 | 4480 | 240 | 4700×1 | 75 | 10 | 95 | 391.7 | 240 | +0.0 | shared-wpn? fp-debt |
| `td_gdi_shotgunner` | tiberiandawn_gdi | 50000 | 75 | 3500 | 200 | 4000×1 | 70 | 5 | 100 | 285.7 | 217 | +16.7 | anchor |

**Worst |Δ| among non-anchor members: 0.1** (goal ≤1).

## Uniqueness check (5 raw stats — soft/protected excluded)

- All 5-stat uniqueness checks passed (HP, Speed, Range, raw ReloadDelay, effective damage-per-shot).

## Required YAML edits (per unit)

- `asianalliance_fanatic`: HP 100000, Speed 75, Range 3500, each offensive warhead Damage 3500 (×1 = SUM 3500), ReloadDelay 70, Burst 10
- `futuretech_shotgundroid`: HP 55000, Speed 60, Range 4110, each offensive warhead Damage 20300 (×3 = SUM 60900), ReloadDelay 55, Burst 1
- `naxis_sssoldier`: HP 63000, Speed 61, Range 4480, each offensive warhead Damage 4700 (×1 = SUM 4700), ReloadDelay 75, Burst 10, **DELETE the unconditional FirepowerMultiplier (95%)** — the Damage above already includes it (W17)
