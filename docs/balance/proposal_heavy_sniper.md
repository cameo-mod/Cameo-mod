# Heavy Sniper infantry rebalance proposal

Anchor spec: HP=25000, Speed=80, Range=8000, eff-DPS=400, Cost=700

Converter law: cost pinned, range clamped to band + made unique, eff-DPS trimmed to Δ≤1 via 100-grid warhead Damage; unconditional FirepowerMultiplier is retired.

Uniqueness separates **raw warhead Damage (the 5-stat law as written)**. The levers run coarsest-first — Damage (100 grid) → Speed (1) → Range (10) — because one Damage step is a whole shot and running it last threw away everything the fine levers had achieved.

| actor | faction | HP | spd | rng | cost | dmg/wh×n | rl | burst | legacy FP% | eff DPS | price | Δ | flags |
|---|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|---|
| `yuri_virus` | redalert2_yuri | 12000 | 65 | 9000 | 700 | 27200×1 | 74 | 1 | 100 | 367.6 | 700 | -0.2 |  |
| `ra1_soviets_dragunovantimaterialsniper` | redalert_soviets | 20000 | 64 | 7000 | 422 | 23700×1 | 85 | 1 | 103 | 278.8 | 422 | -0.1 | fp-debt |

**Worst |Δ| among non-anchor members: 0.2** (goal ≤1).

## Uniqueness check (5 raw stats — soft/protected excluded)

- All 5-stat uniqueness checks passed (HP, Speed, Range, raw ReloadDelay, effective damage-per-shot).

## Required YAML edits (per unit)

- `yuri_virus`: HP 12000, Speed 65, Range 9000, each offensive warhead Damage 27200 (×1 = SUM 27200), ReloadDelay 74, Burst 1
- `ra1_soviets_dragunovantimaterialsniper`: HP 20000, Speed 64, Range 7000, each offensive warhead Damage 23700 (×1 = SUM 23700), ReloadDelay 85, Burst 1, **DELETE the unconditional FirepowerMultiplier (103%)** — the Damage above already includes it (W17)
