# Light Tank infantry rebalance proposal

Anchor spec: HP=100000, Speed=125, Range=5000, eff-DPS=200, Cost=400

Converter law: cost pinned, range clamped to band + made unique, eff-DPS trimmed to Δ≤1 via 100-grid warhead Damage; unconditional FirepowerMultiplier is retired.

Uniqueness separates **raw warhead Damage (the 5-stat law as written)**. The levers run coarsest-first — Damage (100 grid) → Speed (1) → Range (10) — because one Damage step is a whole shot and running it last threw away everything the fine levers had achieved.

| actor | faction | HP | spd | rng | cost | dmg/wh×n | rl | burst | legacy FP% | eff DPS | price | Δ | flags |
|---|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|---|
| `ixian_shockraider` | d2k_ixian | 40000 | 120 | 5030 | 1300 | 100×1 | 0 | 1 | 100 | 0.0 | 85 | -1215.4 |  |
| `ordos_combattank` | d2k_ordos | 82500 | 100 | 5230 | 650 | 23300×1 | 44 | 1 | 100 | 529.5 | 650 | -0.4 |  |
| `yuri_lashertank` | redalert2_yuri | 70000 | 150 | 5200 | 600 | 27700×1 | 68 | 1 | 100 | 407.4 | 600 | +0.4 |  |
| `asianalliance_quasar` | redalert2mod_asianalliance | 57500 | 125 | 5040 | 900 | 200×1 | 0 | 1 | 100 | 0.0 | 124 | -775.6 |  |
| `steelconsortium_manta` | redalert2mod_consortium | 47500 | 145 | 4980 | 850 | 300×1 | 0 | 1 | 50 | 0.0 | 124 | -725.6 | fp-debt |
| `futuretech_robottank` | redalert2mod_futuretech | 55000 | 135 | 5790 | 1600 | 34900×3 | 48 | 1 | 100 | 2181.2 | 1601 | +0.8 | shared-wpn? |
| `schwarzermond_lunarpanzer` | redalert2mod_schwarzermond | 90000 | 110 | 5120 | 650 | 28800×1 | 60 | 1 | 100 | 480.0 | 650 | -0.4 |  |
| `latinsyndicate_rushertank` | redalert2mod_syndicate | 72500 | 115 | 5010 | 650 | 20800×1 | 38 | 1 | 100 | 547.4 | 651 | +0.6 |  |
| `ra1_allies_alliedlighttank` | redalert_allies | 50000 | 120 | 5000 | 500 | 12000×1 | 37 | 1 | 100 | 324.3 | 380 | -120.1 | anchor |
| `ra1_allies_sheridanassaulttank` | redalert_allies | 85000 | 105 | 4990 | 600 | 17600×2 | 64 | 1 | 25 | 550.0 | 600 | +0.3 | fp-debt |
| `japan_shrineminitank` | redalert_japan | 60000 | 140 | 6000 | 1600 | 77000×1 | 50 | 1 | 100 | 1540.0 | 1600 | +0.2 |  |
| `terran_vulture` | starcraft_terran | 75000 | 145 | 4790 | 900 | 17900×1 | 25 | 1 | 100 | 716.0 | 899 | -0.6 |  |
| `td_nod_lighttank` | tiberiandawn_nod | 77500 | 150 | 4970 | 600 | 17700×1 | 45 | 1 | 100 | 393.3 | 599 | -0.7 |  |
| `td_nod_lighttankmkii` | tiberiandawn_nod | 80000 | 150 | 4900 | 800 | 21400×1 | 35 | 1 | 100 | 611.4 | 800 | +0.0 |  |
| `cabal_ravager` | tiberiansun_cabal | 67500 | 130 | 4000 | 1500 | 50900×1 | 30 | 1 | 100 | 1696.7 | 1501 | +1.1 |  |

**Worst |Δ| among non-anchor members: 1215.4** (goal ≤1).

## Uniqueness check (5 raw stats — soft/protected excluded)

- **Speed duplicates**: {150: ['yuri_lashertank', 'td_nod_lighttank', 'td_nod_lighttankmkii'], 145: ['steelconsortium_manta', 'terran_vulture']}
- **raw ReloadDelay duplicates** (design choice — retune coarse Damage/reload by hand): {0: ['ixian_shockraider', 'asianalliance_quasar', 'steelconsortium_manta']}

## Required YAML edits (per unit)

- `ixian_shockraider`: HP 40000, Speed 120, Range 5030, each offensive warhead Damage 100 (×1 = SUM 100), ReloadDelay 0, Burst 1, residual Δ -1215.4 (cost pinned at 1300)
- `ordos_combattank`: HP 82500, Speed 100, Range 5230, each offensive warhead Damage 23300 (×1 = SUM 23300), ReloadDelay 44, Burst 1
- `yuri_lashertank`: HP 70000, Speed 150, Range 5200, each offensive warhead Damage 27700 (×1 = SUM 27700), ReloadDelay 68, Burst 1
- `asianalliance_quasar`: HP 57500, Speed 125, Range 5040, each offensive warhead Damage 200 (×1 = SUM 200), ReloadDelay 0, Burst 1, residual Δ -775.6 (cost pinned at 900)
- `steelconsortium_manta`: HP 47500, Speed 145, Range 4980, each offensive warhead Damage 300 (×1 = SUM 300), ReloadDelay 0, Burst 1, **DELETE the unconditional FirepowerMultiplier (50%)** — the Damage above already includes it (W17), residual Δ -725.6 (cost pinned at 850)
- `futuretech_robottank`: HP 55000, Speed 135, Range 5790, each offensive warhead Damage 34900 (×3 = SUM 104700), ReloadDelay 48, Burst 1
- `schwarzermond_lunarpanzer`: HP 90000, Speed 110, Range 5120, each offensive warhead Damage 28800 (×1 = SUM 28800), ReloadDelay 60, Burst 1
- `latinsyndicate_rushertank`: HP 72500, Speed 115, Range 5010, each offensive warhead Damage 20800 (×1 = SUM 20800), ReloadDelay 38, Burst 1
- `ra1_allies_sheridanassaulttank`: HP 85000, Speed 105, Range 4990, each offensive warhead Damage 17600 (×2 = SUM 35200), ReloadDelay 64, Burst 1, **DELETE the unconditional FirepowerMultiplier (25%)** — the Damage above already includes it (W17)
- `japan_shrineminitank`: HP 60000, Speed 140, Range 6000, each offensive warhead Damage 77000 (×1 = SUM 77000), ReloadDelay 50, Burst 1
- `terran_vulture`: HP 75000, Speed 145, Range 4790, each offensive warhead Damage 17900 (×1 = SUM 17900), ReloadDelay 25, Burst 1
- `td_nod_lighttank`: HP 77500, Speed 150, Range 4970, each offensive warhead Damage 17700 (×1 = SUM 17700), ReloadDelay 45, Burst 1
- `td_nod_lighttankmkii`: HP 80000, Speed 150, Range 4900, each offensive warhead Damage 21400 (×1 = SUM 21400), ReloadDelay 35, Burst 1
- `cabal_ravager`: HP 67500, Speed 130, Range 4000, each offensive warhead Damage 50900 (×1 = SUM 50900), ReloadDelay 30, Burst 1, residual Δ +1.1 (cost pinned at 1500)
