# Missile Vehicle infantry rebalance proposal

Anchor spec: HP=160000, Speed=100, Range=8000, eff-DPS=1200, Cost=1200

Converter law: cost pinned, range clamped to band + made unique, eff-DPS trimmed to Δ≤1 via 100-grid warhead Damage; unconditional FirepowerMultiplier is retired.

Uniqueness separates **raw warhead Damage (the 5-stat law as written)**. The levers run coarsest-first — Damage (100 grid) → Speed (1) → Range (10) — because one Damage step is a whole shot and running it last threw away everything the fine levers had achieved.

| actor | faction | HP | spd | rng | cost | dmg/wh×n | rl | burst | legacy FP% | eff DPS | price | Δ | flags |
|---|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|---|
| `missile_tank` | d2k_harkonnen | 47500 | 95 | 9580 | 750 | 86900×2 | 150 | 4 | 100 | 3089.8 | 750 | -0.0 | shared-wpn? |
| `ixian_ixmissiletank` | d2k_ixian | 52500 | 85 | 9600 | 2250 | 28300×1 | 5 | 1 | 100 | 5660.0 | 2250 | -0.3 |  |
| `ordos_dustdrone` | d2k_ordos | 32500 | 120 | 6510 | 1400 | 55500×3 | 42 | 1 | 100 | 3964.3 | 1401 | +0.6 |  |
| `asianalliance_type89mlrs` | redalert2mod_asianalliance | 40000 | 80 | 8520 | 1200 | 17900×3 | 16 | 1 | 100 | 3356.2 | 1200 | +0.3 |  |
| `latinsyndicate_lars` | redalert2mod_syndicate | 45000 | 90 | 9590 | 1300 | 65800×1 | 139 | 10 | 125 | 3760.0 | 1300 | +0.1 | fp-debt |
| `tkm_stryker` | redalert2mod_tkm | 80000 | 105 | 7890 | 1600 | 22600×1 | 5 | 1 | 100 | 4520.0 | 1599 | -0.6 |  |
| `terran_cyclone` | starcraft_terran | 90000 | 120 | 7310 | 2300 | 26500×1 | 35 | 6 | 100 | 3533.3 | 2301 | +0.6 |  |
| `td_gdi_mlrs` | tiberiandawn_gdi | 27500 | 100 | 9570 | 1000 | 47000×1 | 111 | 6 | 100 | 2073.5 | 1000 | -0.5 | shared-wpn? |
| `td_nod_chemicalattackbike` | tiberiandawn_nod | 25000 | 120 | 6410 | 750 | 74400×1 | 70 | 2 | 100 | 1860.0 | 750 | -0.2 |  |
| `td_nod_reconbike` | tiberiandawn_nod | 22500 | 115 | 6400 | 500 | 16200×2 | 55 | 2 | 100 | 996.9 | 500 | +0.4 |  |
| `td_nod_stealthtank` | tiberiandawn_nod | 17500 | 115 | 7430 | 900 | 247100×1 | 58 | 2 | 130 | 7487.9 | 900 | +0.1 | fp-debt |
| `ts_gdi_hovermlrs` | tiberiansun_gdi | 30000 | 80 | 8000 | 900 | 12000×1 | 60 | 2 | 100 | 352.9 | 335 | -565.4 | anchor |
| `ts_nod_attackcycle` | tiberiansun_nod | 15000 | 110 | 6540 | 550 | 14900×2 | 40 | 2 | 100 | 1324.4 | 550 | -0.2 | shared-wpn? |

**Worst |Δ| among non-anchor members: 0.6** (goal ≤1).

## Uniqueness check (5 raw stats — soft/protected excluded)

- **Speed duplicates**: {120: ['ordos_dustdrone', 'terran_cyclone', 'td_nod_chemicalattackbike'], 115: ['td_nod_reconbike', 'td_nod_stealthtank']}
- **raw ReloadDelay duplicates** (design choice — retune coarse Damage/reload by hand): {5: ['ixian_ixmissiletank', 'tkm_stryker']}

## Required YAML edits (per unit)

- `missile_tank`: HP 47500, Speed 95, Range 9580, each offensive warhead Damage 86900 (×2 = SUM 173800), ReloadDelay 150, Burst 4
- `ixian_ixmissiletank`: HP 52500, Speed 85, Range 9600, each offensive warhead Damage 28300 (×1 = SUM 28300), ReloadDelay 5, Burst 1
- `ordos_dustdrone`: HP 32500, Speed 120, Range 6510, each offensive warhead Damage 55500 (×3 = SUM 166500), ReloadDelay 42, Burst 1
- `asianalliance_type89mlrs`: HP 40000, Speed 80, Range 8520, each offensive warhead Damage 17900 (×3 = SUM 53700), ReloadDelay 16, Burst 1
- `latinsyndicate_lars`: HP 45000, Speed 90, Range 9590, each offensive warhead Damage 65800 (×1 = SUM 65800), ReloadDelay 139, Burst 10, **DELETE the unconditional FirepowerMultiplier (125%)** — the Damage above already includes it (W17)
- `tkm_stryker`: HP 80000, Speed 105, Range 7890, each offensive warhead Damage 22600 (×1 = SUM 22600), ReloadDelay 5, Burst 1
- `terran_cyclone`: HP 90000, Speed 120, Range 7310, each offensive warhead Damage 26500 (×1 = SUM 26500), ReloadDelay 35, Burst 6
- `td_gdi_mlrs`: HP 27500, Speed 100, Range 9570, each offensive warhead Damage 47000 (×1 = SUM 47000), ReloadDelay 111, Burst 6
- `td_nod_chemicalattackbike`: HP 25000, Speed 120, Range 6410, each offensive warhead Damage 74400 (×1 = SUM 74400), ReloadDelay 70, Burst 2
- `td_nod_reconbike`: HP 22500, Speed 115, Range 6400, each offensive warhead Damage 16200 (×2 = SUM 32400), ReloadDelay 55, Burst 2
- `td_nod_stealthtank`: HP 17500, Speed 115, Range 7430, each offensive warhead Damage 247100 (×1 = SUM 247100), ReloadDelay 58, Burst 2, **DELETE the unconditional FirepowerMultiplier (130%)** — the Damage above already includes it (W17)
- `ts_nod_attackcycle`: HP 15000, Speed 110, Range 6540, each offensive warhead Damage 14900 (×2 = SUM 29800), ReloadDelay 40, Burst 2
