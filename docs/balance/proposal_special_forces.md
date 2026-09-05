# Special Forces infantry rebalance proposal

Anchor spec: HP=15000, Speed=50, Range=6000, eff-DPS=240, Cost=200

Converter law: cost pinned, range clamped to band + made unique, eff-DPS trimmed to Δ≤1 via 100-grid warhead Damage; unconditional FirepowerMultiplier is retired.

Uniqueness separates **raw warhead Damage (the 5-stat law as written)**. The levers run coarsest-first — Damage (100 grid) → Speed (1) → Range (10) — because one Damage step is a whole shot and running it last threw away everything the fine levers had achieved.

| actor | faction | HP | spd | rng | cost | dmg/wh×n | rl | burst | legacy FP% | eff DPS | price | Δ | flags |
|---|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|---|
| `ra2_allies_seal` | redalert2_allies | 31000 | 59 | 6410 | 1162 | 5700×1 | 10 | 4 | 93 | 1200.0 | 1162 | -0.2 | fp-debt |
| `ra2_soviets_flaktrooper` | redalert2_soviets | 10000 | 44 | 5640 | 416 | 17100×1 | 17 | 1 | 107 | 1005.9 | 416 | +0.3 | fp-debt |
| `yuri_gatlingtrooper` | redalert2_yuri | 36000 | 45 | 5510 | 431 | 7000×1 | 15 | 1 | 108 | 466.7 | 431 | -0.2 | fp-debt |
| `schwarzermond_lunarsoldier` | redalert2mod_schwarzermond | 30000 | 50 | 6000 | 500 | 8000×3 | 50 | 1 | 100 | 480.0 | 500 | +0.0 |  |
| `tkm_trooper` | redalert2mod_tkm | 33000 | 60 | 5810 | 200 | 300×1 | 31 | 5 | 37 | 42.9 | 200 | -0.0 | fp-debt |
| `ra1_allies_machinegunner` | redalert_allies | 19000 | 49 | 6500 | 557 | 9300×1 | 48 | 5 | 93 | 775.0 | 557 | -0.2 | fp-debt |
| `japan_imperialscoutsman` | redalert_japan | 15000 | 50 | 6000 | 200 | 13000×3 | 50 | 1 | 100 | 260.0 | 210 | +9.7 | anchor |
| `terran_ghost` | starcraft_terran | 44000 | 59 | 6370 | 1176 | 8100×3 | 22 | 1 | 103 | 1104.5 | 1177 | +0.5 | fp-debt |
| `terran_specter` | starcraft_terran | 50000 | 58 | 6470 | 1744 | 16800×3 | 33 | 1 | 106 | 1527.3 | 1744 | +0.1 | fp-debt |
| `td_gdi_officer` | tiberiandawn_gdi | 32000 | 60 | 5600 | 1532 | 14200×1 | 20 | 4 | 108 | 1775.0 | 1533 | +0.9 | fp-debt |
| `td_nod_lasertrooper` | tiberiandawn_nod | 59000 | 51 | 5530 | 750 | 25900×2 | 50 | 1 | 108 | 1036.0 | 750 | +0.2 | fp-debt |
| `td_nod_stealthsoldier` | tiberiandawn_nod | 25000 | 60 | 6490 | 753 | 18900×1 | 90 | 4 | 81 | 720.0 | 752 | -0.9 | fp-debt |
| `cabal_eliminator800` | tiberiansun_cabal | 85000 | 40 | 5850 | 1450 | 4100×1 | 5 | 1 | 105 | 820.0 | 1450 | -0.1 | fp-debt |
| `forgotten_mutantsergeant` | tiberiansun_forgotten | 40000 | 60 | 5670 | 1154 | 5200×2 | 8 | 1 | 108 | 1300.0 | 1155 | +0.8 | fp-debt |
| `ts_gdi_falconenforcer` | tiberiansun_gdi | 45000 | 59 | 6120 | 1322 | 9200×1 | 26 | 3 | 95 | 920.0 | 1322 | -0.4 | fp-debt |
| `ts_nod_elitecadre` | tiberiansun_nod | 21000 | 55 | 6210 | 435 | 8600×1 | 52 | 5 | 93 | 716.7 | 435 | +0.3 | fp-debt |

**Worst |Δ| among non-anchor members: 0.9** (goal ≤1).

## Uniqueness check (5 raw stats — soft/protected excluded)

- **Speed duplicates**: {59: ['ra2_allies_seal', 'terran_ghost', 'ts_gdi_falconenforcer'], 60: ['tkm_trooper', 'td_gdi_officer', 'td_nod_stealthsoldier', 'forgotten_mutantsergeant']}
- **raw ReloadDelay duplicates** (design choice — retune coarse Damage/reload by hand): {50: ['schwarzermond_lunarsoldier', 'td_nod_lasertrooper']}

## Required YAML edits (per unit)

- `ra2_allies_seal`: HP 31000, Speed 59, Range 6410, each offensive warhead Damage 5700 (×1 = SUM 5700), ReloadDelay 10, Burst 4, **DELETE the unconditional FirepowerMultiplier (93%)** — the Damage above already includes it (W17)
- `ra2_soviets_flaktrooper`: HP 10000, Speed 44, Range 5640, each offensive warhead Damage 17100 (×1 = SUM 17100), ReloadDelay 17, Burst 1, **DELETE the unconditional FirepowerMultiplier (107%)** — the Damage above already includes it (W17)
- `yuri_gatlingtrooper`: HP 36000, Speed 45, Range 5510, each offensive warhead Damage 7000 (×1 = SUM 7000), ReloadDelay 15, Burst 1, **DELETE the unconditional FirepowerMultiplier (108%)** — the Damage above already includes it (W17)
- `schwarzermond_lunarsoldier`: HP 30000, Speed 50, Range 6000, each offensive warhead Damage 8000 (×3 = SUM 24000), ReloadDelay 50, Burst 1
- `tkm_trooper`: HP 33000, Speed 60, Range 5810, each offensive warhead Damage 300 (×1 = SUM 300), ReloadDelay 31, Burst 5, **DELETE the unconditional FirepowerMultiplier (37%)** — the Damage above already includes it (W17)
- `ra1_allies_machinegunner`: HP 19000, Speed 49, Range 6500, each offensive warhead Damage 9300 (×1 = SUM 9300), ReloadDelay 48, Burst 5, **DELETE the unconditional FirepowerMultiplier (93%)** — the Damage above already includes it (W17)
- `terran_ghost`: HP 44000, Speed 59, Range 6370, each offensive warhead Damage 8100 (×3 = SUM 24300), ReloadDelay 22, Burst 1, **DELETE the unconditional FirepowerMultiplier (103%)** — the Damage above already includes it (W17)
- `terran_specter`: HP 50000, Speed 58, Range 6470, each offensive warhead Damage 16800 (×3 = SUM 50400), ReloadDelay 33, Burst 1, **DELETE the unconditional FirepowerMultiplier (106%)** — the Damage above already includes it (W17)
- `td_gdi_officer`: HP 32000, Speed 60, Range 5600, each offensive warhead Damage 14200 (×1 = SUM 14200), ReloadDelay 20, Burst 4, **DELETE the unconditional FirepowerMultiplier (108%)** — the Damage above already includes it (W17)
- `td_nod_lasertrooper`: HP 59000, Speed 51, Range 5530, each offensive warhead Damage 25900 (×2 = SUM 51800), ReloadDelay 50, Burst 1, **DELETE the unconditional FirepowerMultiplier (108%)** — the Damage above already includes it (W17)
- `td_nod_stealthsoldier`: HP 25000, Speed 60, Range 6490, each offensive warhead Damage 18900 (×1 = SUM 18900), ReloadDelay 90, Burst 4, **DELETE the unconditional FirepowerMultiplier (81%)** — the Damage above already includes it (W17)
- `cabal_eliminator800`: HP 85000, Speed 40, Range 5850, each offensive warhead Damage 4100 (×1 = SUM 4100), ReloadDelay 5, Burst 1, **DELETE the unconditional FirepowerMultiplier (105%)** — the Damage above already includes it (W17)
- `forgotten_mutantsergeant`: HP 40000, Speed 60, Range 5670, each offensive warhead Damage 5200 (×2 = SUM 10400), ReloadDelay 8, Burst 1, **DELETE the unconditional FirepowerMultiplier (108%)** — the Damage above already includes it (W17)
- `ts_gdi_falconenforcer`: HP 45000, Speed 59, Range 6120, each offensive warhead Damage 9200 (×1 = SUM 9200), ReloadDelay 26, Burst 3, **DELETE the unconditional FirepowerMultiplier (95%)** — the Damage above already includes it (W17)
- `ts_nod_elitecadre`: HP 21000, Speed 55, Range 6210, each offensive warhead Damage 8600 (×1 = SUM 8600), ReloadDelay 52, Burst 5, **DELETE the unconditional FirepowerMultiplier (93%)** — the Damage above already includes it (W17)
