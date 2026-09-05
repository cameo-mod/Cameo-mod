# Scout infantry rebalance proposal

Anchor spec: HP=20000, Speed=60, Range=5000, eff-DPS=60, Cost=100

Converter law: cost pinned, range clamped to band + made unique, eff-DPS trimmed to Δ≤1 via 100-grid warhead Damage; unconditional FirepowerMultiplier is retired.

Uniqueness separates **raw warhead Damage (the 5-stat law as written)**. The levers run coarsest-first — Damage (100 grid) → Speed (1) → Range (10) — because one Damage step is a whole shot and running it last threw away everything the fine levers had achieved.

| actor | faction | HP | spd | rng | cost | dmg/wh×n | rl | burst | legacy FP% | eff DPS | price | Δ | flags |
|---|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|---|
| `ixian_lightinfantry` | d2k_ixian | 32000 | 56 | 4540 | 150 | 1700×1 | 20 | 1 | 113 | 85.0 | 151 | +0.8 | shared-wpn? fp-debt |
| `ordos_lightinfantry` | d2k_ordos | 31000 | 49 | 4860 | 120 | 1500×1 | 20 | 1 | 67 | 75.0 | 131 | +10.9 | shared-wpn? fp-debt |
| `ra2_allies_gi` | redalert2_allies | 50000 | 69 | 4740 | 200 | 300×1 | 15 | 3 | 25 | 47.4 | 200 | +0.0 | fp-debt |
| `ra2_soviets_conscript` | redalert2_soviets | 26000 | 50 | 4520 | 100 | 1200×1 | 18 | 1 | 19 | 66.7 | 106 | +5.6 | fp-debt |
| `asianalliance_asianmilitia` | redalert2mod_asianalliance | 28000 | 53 | 4550 | 110 | 3100×1 | 50 | 1 | 83 | 62.0 | 110 | +0.3 | fp-debt |
| `futuretech_scoutdroid` | redalert2mod_futuretech | 33000 | 70 | 5450 | 200 | 1800×1 | 40 | 4 | 100 | 156.5 | 200 | -0.1 |  |
| `naxis_coneheadsknights` | redalert2mod_naxis | 22000 | 72 | 4530 | 1000 | 26200×1 | 18 | 1 | 100 | 1455.6 | 1002 | +1.6 | shared-wpn? |
| `naxis_naxiriflerecruit` | redalert2mod_naxis | 21000 | 48 | 5130 | 75 | 4100×1 | 100 | 1 | 67 | 41.0 | 75 | -0.0 | fp-debt |
| `naxis_naxiriflesoldier` | redalert2mod_naxis | 20000 | 60 | 5000 | 100 | 4000×1 | 50 | 1 | 100 | 80.0 | 119 | +19.4 | anchor |
| `undead.nax` | redalert2mod_naxis | 14000 | 52 | 5430 | 100 | 6300×1 | 75 | 1 | 100 | 84.0 | 100 | -0.0 | soft shared-wpn? |
| `latinsyndicate_latinmilitia` | redalert2mod_syndicate | 29000 | 51 | 4640 | 130 | 800×1 | 22 | 3 | 39 | 80.0 | 130 | +0.0 | fp-debt |
| `tkm_marine` | redalert2mod_tkm | 18000 | 57 | 4880 | 300 | 1300×1 | 16 | 5 | 100 | 325.0 | 322 | +21.7 |  |
| `tkm_rifleman` | redalert2mod_tkm | 23000 | 64 | 5070 | 120 | 4900×1 | 75 | 1 | 89 | 65.3 | 120 | -0.0 | fp-debt |
| `ra1_soviets_ak47conscript` | redalert_soviets | 43000 | 62 | 4850 | 200 | 400×1 | 11 | 3 | 14 | 80.0 | 200 | +0.1 | fp-debt |
| `ra1_soviets_rifleinfantry` | redalert_soviets | 34000 | 54 | 4680 | 100 | 900×1 | 50 | 3 | 42 | 45.0 | 109 | +8.7 | fp-debt |
| `ra1_allies_rifleinfantry` | shared_redalert | 30000 | 55 | 4980 | 100 | 1000×1 | 50 | 3 | 47 | 51.7 | 112 | +12.2 | fp-debt |
| `zerg_spithid` | starcraft_zerg | 40000 | 72 | 4560 | 300 | 2100×1 | 15 | 1 | 100 | 140.0 | 299 | -0.6 |  |
| `td_gdi_minigunner` | tiberiandawn_gdi | 25000 | 63 | 4670 | 100 | 700×1 | 50 | 4 | 24 | 47.5 | 100 | +0.0 | fp-debt |
| `td_nod_minigunner` | tiberiandawn_nod | 24000 | 58 | 4610 | 100 | 1100×1 | 50 | 4 | 29 | 78.6 | 123 | +22.8 | fp-debt |
| `forgotten_mutant` | tiberiansun_forgotten | 46000 | 68 | 4910 | 160 | 500×1 | 18 | 2 | 30 | 43.5 | 160 | -0.0 | shared-wpn? fp-debt |
| `forgotten_mutant_wild` | tiberiansun_forgotten | 44000 | 66 | 4580 | 160 | 600×1 | 18 | 2 | 100 | 52.2 | 160 | +0.2 | shared-wpn? |
| `forgotten_mutantsoldier` | tiberiansun_forgotten | 39000 | 60 | 4960 | 250 | 6200×1 | 50 | 1 | 100 | 124.0 | 250 | -0.0 |  |
| `ts_gdi_lightinfantry` | tiberiansun_gdi | 17000 | 59 | 4510 | 120 | 1400×1 | 12 | 1 | 100 | 116.7 | 131 | +11.0 | shared-wpn? |
| `ts_nod_lightinfantry` | tiberiansun_nod | 15000 | 61 | 4500 | 120 | 1600×1 | 12 | 1 | 100 | 133.3 | 138 | +17.6 | shared-wpn? |

**Worst |Δ| among non-anchor members: 22.8** (goal ≤1).

## Uniqueness check (5 raw stats — soft/protected excluded)

- **Speed duplicates**: {72: ['naxis_coneheadsknights', 'zerg_spithid']}
- **raw ReloadDelay duplicates** (design choice — retune coarse Damage/reload by hand): {20: ['ixian_lightinfantry', 'ordos_lightinfantry'], 15: ['ra2_allies_gi', 'zerg_spithid'], 18: ['ra2_soviets_conscript', 'naxis_coneheadsknights', 'forgotten_mutant', 'forgotten_mutant_wild'], 50: ['asianalliance_asianmilitia', 'ra1_soviets_rifleinfantry', 'ra1_allies_rifleinfantry', 'td_gdi_minigunner', 'td_nod_minigunner', 'forgotten_mutantsoldier'], 12: ['ts_gdi_lightinfantry', 'ts_nod_lightinfantry']}

## Required YAML edits (per unit)

- `ixian_lightinfantry`: HP 32000, Speed 56, Range 4540, each offensive warhead Damage 1700 (×1 = SUM 1700), ReloadDelay 20, Burst 1, **DELETE the unconditional FirepowerMultiplier (113%)** — the Damage above already includes it (W17)
- `ordos_lightinfantry`: HP 31000, Speed 49, Range 4860, each offensive warhead Damage 1500 (×1 = SUM 1500), ReloadDelay 20, Burst 1, **DELETE the unconditional FirepowerMultiplier (67%)** — the Damage above already includes it (W17), residual Δ +10.9 (cost pinned at 120)
- `ra2_allies_gi`: HP 50000, Speed 69, Range 4740, each offensive warhead Damage 300 (×1 = SUM 300), ReloadDelay 15, Burst 3, **DELETE the unconditional FirepowerMultiplier (25%)** — the Damage above already includes it (W17)
- `ra2_soviets_conscript`: HP 26000, Speed 50, Range 4520, each offensive warhead Damage 1200 (×1 = SUM 1200), ReloadDelay 18, Burst 1, **DELETE the unconditional FirepowerMultiplier (19%)** — the Damage above already includes it (W17), residual Δ +5.6 (cost pinned at 100)
- `asianalliance_asianmilitia`: HP 28000, Speed 53, Range 4550, each offensive warhead Damage 3100 (×1 = SUM 3100), ReloadDelay 50, Burst 1, **DELETE the unconditional FirepowerMultiplier (83%)** — the Damage above already includes it (W17)
- `futuretech_scoutdroid`: HP 33000, Speed 70, Range 5450, each offensive warhead Damage 1800 (×1 = SUM 1800), ReloadDelay 40, Burst 4
- `naxis_coneheadsknights`: HP 22000, Speed 72, Range 4530, each offensive warhead Damage 26200 (×1 = SUM 26200), ReloadDelay 18, Burst 1, residual Δ +1.6 (cost pinned at 1000)
- `naxis_naxiriflerecruit`: HP 21000, Speed 48, Range 5130, each offensive warhead Damage 4100 (×1 = SUM 4100), ReloadDelay 100, Burst 1, **DELETE the unconditional FirepowerMultiplier (67%)** — the Damage above already includes it (W17)
- `undead.nax`: HP 14000, Speed 52, Range 5430, each offensive warhead Damage 6300 (×1 = SUM 6300), ReloadDelay 75, Burst 1
- `latinsyndicate_latinmilitia`: HP 29000, Speed 51, Range 4640, each offensive warhead Damage 800 (×1 = SUM 800), ReloadDelay 22, Burst 3, **DELETE the unconditional FirepowerMultiplier (39%)** — the Damage above already includes it (W17)
- `tkm_marine`: HP 18000, Speed 57, Range 4880, each offensive warhead Damage 1300 (×1 = SUM 1300), ReloadDelay 16, Burst 5, residual Δ +21.7 (cost pinned at 300)
- `tkm_rifleman`: HP 23000, Speed 64, Range 5070, each offensive warhead Damage 4900 (×1 = SUM 4900), ReloadDelay 75, Burst 1, **DELETE the unconditional FirepowerMultiplier (89%)** — the Damage above already includes it (W17)
- `ra1_soviets_ak47conscript`: HP 43000, Speed 62, Range 4850, each offensive warhead Damage 400 (×1 = SUM 400), ReloadDelay 11, Burst 3, **DELETE the unconditional FirepowerMultiplier (14%)** — the Damage above already includes it (W17)
- `ra1_soviets_rifleinfantry`: HP 34000, Speed 54, Range 4680, each offensive warhead Damage 900 (×1 = SUM 900), ReloadDelay 50, Burst 3, **DELETE the unconditional FirepowerMultiplier (42%)** — the Damage above already includes it (W17), residual Δ +8.7 (cost pinned at 100)
- `ra1_allies_rifleinfantry`: HP 30000, Speed 55, Range 4980, each offensive warhead Damage 1000 (×1 = SUM 1000), ReloadDelay 50, Burst 3, **DELETE the unconditional FirepowerMultiplier (47%)** — the Damage above already includes it (W17), residual Δ +12.2 (cost pinned at 100)
- `zerg_spithid`: HP 40000, Speed 72, Range 4560, each offensive warhead Damage 2100 (×1 = SUM 2100), ReloadDelay 15, Burst 1
- `td_gdi_minigunner`: HP 25000, Speed 63, Range 4670, each offensive warhead Damage 700 (×1 = SUM 700), ReloadDelay 50, Burst 4, **DELETE the unconditional FirepowerMultiplier (24%)** — the Damage above already includes it (W17)
- `td_nod_minigunner`: HP 24000, Speed 58, Range 4610, each offensive warhead Damage 1100 (×1 = SUM 1100), ReloadDelay 50, Burst 4, **DELETE the unconditional FirepowerMultiplier (29%)** — the Damage above already includes it (W17), residual Δ +22.8 (cost pinned at 100)
- `forgotten_mutant`: HP 46000, Speed 68, Range 4910, each offensive warhead Damage 500 (×1 = SUM 500), ReloadDelay 18, Burst 2, **DELETE the unconditional FirepowerMultiplier (30%)** — the Damage above already includes it (W17)
- `forgotten_mutant_wild`: HP 44000, Speed 66, Range 4580, each offensive warhead Damage 600 (×1 = SUM 600), ReloadDelay 18, Burst 2
- `forgotten_mutantsoldier`: HP 39000, Speed 60, Range 4960, each offensive warhead Damage 6200 (×1 = SUM 6200), ReloadDelay 50, Burst 1
- `ts_gdi_lightinfantry`: HP 17000, Speed 59, Range 4510, each offensive warhead Damage 1400 (×1 = SUM 1400), ReloadDelay 12, Burst 1, residual Δ +11.0 (cost pinned at 120)
- `ts_nod_lightinfantry`: HP 15000, Speed 61, Range 4500, each offensive warhead Damage 1600 (×1 = SUM 1600), ReloadDelay 12, Burst 1, residual Δ +17.6 (cost pinned at 120)
