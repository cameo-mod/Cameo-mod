# Scout infantry rebalance proposal

Anchor spec: HP=20000, Speed=60, Range=5000, eff-DPS=60, Cost=100

Converter law: cost pinned, range clamped to band + made unique, eff-DPS trimmed to Δ≤1 via 100-grid warhead Damage; unconditional FirepowerMultiplier is retired.

| actor | faction | HP | spd | rng | cost | dmg/wh×n | rl | burst | legacy FP% | eff DPS | price | Δ | flags |
|---|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|---|
| `ixian_lightinfantry` | d2k_ixian | 32000 | 69 | 5480 | 150 | 100×1 | 20 | 1 | 113 | 0.0 | 63 | -87.3 | shared-wpn? fp-debt |
| `ordos_lightinfantry` | d2k_ordos | 31000 | 68 | 5470 | 120 | 200×1 | 20 | 1 | 67 | 0.0 | 61 | -59.2 | shared-wpn? fp-debt |
| `ra2_allies_gi` | redalert2_allies | 50000 | 50 | 5210 | 200 | 400×1 | 15 | 3 | 25 | 63.2 | 200 | +0.2 | fp-debt |
| `ra2_soviets_conscript` | redalert2_soviets | 26000 | 62 | 4530 | 100 | 300×1 | 18 | 1 | 19 | 0.0 | 49 | -50.6 | fp-debt |
| `asianalliance_asianmilitia` | redalert2mod_asianalliance | 28000 | 58 | 4550 | 110 | 500×1 | 50 | 1 | 83 | 0.0 | 50 | -60.1 | fp-debt |
| `futuretech_scoutdroid` | redalert2mod_futuretech | 33000 | 70 | 5490 | 200 | 1800×1 | 40 | 4 | 100 | 156.5 | 201 | +1.0 |  |
| `naxis_coneheadsknights` | redalert2mod_naxis | 22000 | 72 | 4540 | 1000 | 26100×1 | 18 | 1 | 100 | 1450.0 | 1000 | -0.3 | shared-wpn? |
| `naxis_naxiriflerecruit` | redalert2mod_naxis | 21000 | 57 | 5220 | 75 | 600×1 | 100 | 1 | 67 | 0.0 | 42 | -33.0 | fp-debt |
| `naxis_naxiriflesoldier` | redalert2mod_naxis | 20000 | 60 | 5000 | 100 | 0×1 | 50 | 1 | 100 | 0.0 | 42 | -58.3 | anchor |
| `undead.nax` | redalert2mod_naxis | 14000 | 52 | 5430 | 100 | 100×1 | 75 | 1 | 100 | 0.0 | 32 | -67.8 | soft shared-wpn? |
| `latinsyndicate_latinmilitia` | redalert2mod_syndicate | 29000 | 56 | 5400 | 130 | 700×1 | 22 | 3 | 39 | 0.0 | 51 | -78.6 | fp-debt |
| `tkm_marine` | redalert2mod_tkm | 18000 | 60 | 5190 | 300 | 1100×1 | 16 | 5 | 100 | 275.0 | 300 | -0.3 |  |
| `tkm_rifleman` | redalert2mod_tkm | 23000 | 61 | 5040 | 120 | 800×1 | 75 | 1 | 89 | 0.0 | 46 | -74.1 | fp-debt |
| `ra1_soviets_ak47conscript` | redalert_soviets | 43000 | 71 | 4820 | 200 | 900×1 | 11 | 3 | 14 | 180.0 | 397 | +196.7 | fp-debt |
| `ra1_soviets_rifleinfantry` | redalert_soviets | 34000 | 54 | 4670 | 100 | 1000×1 | 50 | 3 | 42 | 0.0 | 55 | -45.0 | fp-debt |
| `ra1_allies_rifleinfantry` | shared_redalert | 30000 | 55 | 5450 | 100 | 1200×1 | 50 | 3 | 47 | 0.0 | 52 | -47.9 | fp-debt |
| `zerg_spithid` | starcraft_zerg | 39000 | 72 | 4500 | 300 | 2200×1 | 15 | 1 | 100 | 146.7 | 301 | +1.4 |  |
| `td_gdi_minigunner` | tiberiandawn_gdi | 25000 | 63 | 5440 | 100 | 1300×1 | 50 | 4 | 24 | 0.0 | 50 | -49.9 | fp-debt |
| `td_nod_minigunner` | tiberiandawn_nod | 24000 | 67 | 4610 | 100 | 1400×1 | 50 | 4 | 29 | 0.0 | 49 | -50.7 | fp-debt |
| `forgotten_mutant` | tiberiansun_forgotten | 46000 | 65 | 5230 | 160 | 1500×1 | 18 | 2 | 30 | 130.4 | 323 | +163.3 | shared-wpn? fp-debt |
| `forgotten_mutant_wild` | tiberiansun_forgotten | 44000 | 66 | 5500 | 160 | 1600×1 | 18 | 2 | 100 | 139.1 | 345 | +184.6 | shared-wpn? |
| `forgotten_mutantsoldier` | tiberiansun_forgotten | 40000 | 60 | 5000 | 250 | 8000×1 | 50 | 1 | 100 | 160.0 | 311 | +61.1 | verifier |
| `ts_gdi_lightinfantry` | tiberiansun_gdi | 17000 | 64 | 4520 | 120 | 1700×1 | 12 | 1 | 100 | 0.0 | 39 | -81.4 | shared-wpn? |
| `ts_nod_lightinfantry` | tiberiansun_nod | 15000 | 59 | 4510 | 120 | 1900×1 | 12 | 1 | 100 | 0.0 | 34 | -85.7 | shared-wpn? |

**Worst |Δ| among non-anchor members: 196.7** (goal ≤1).

## Uniqueness check (5 raw stats — soft/protected excluded)

- **Speed duplicates**: {72: ['naxis_coneheadsknights', 'zerg_spithid']}
- **raw ReloadDelay duplicates** (design choice — retune coarse Damage/reload by hand): {20: ['ixian_lightinfantry', 'ordos_lightinfantry'], 15: ['ra2_allies_gi', 'zerg_spithid'], 18: ['ra2_soviets_conscript', 'naxis_coneheadsknights', 'forgotten_mutant', 'forgotten_mutant_wild'], 50: ['asianalliance_asianmilitia', 'ra1_soviets_rifleinfantry', 'ra1_allies_rifleinfantry', 'td_gdi_minigunner', 'td_nod_minigunner'], 12: ['ts_gdi_lightinfantry', 'ts_nod_lightinfantry']}

## Required YAML edits (per unit)

- `ixian_lightinfantry`: HP 32000, Speed 69, Range 5480, each offensive warhead Damage 100 (×1 = SUM 100), ReloadDelay 20, Burst 1, **DELETE the unconditional FirepowerMultiplier (113%)** — the Damage above already includes it (W17), residual Δ -87.3 (cost pinned at 150)
- `ordos_lightinfantry`: HP 31000, Speed 68, Range 5470, each offensive warhead Damage 200 (×1 = SUM 200), ReloadDelay 20, Burst 1, **DELETE the unconditional FirepowerMultiplier (67%)** — the Damage above already includes it (W17), residual Δ -59.2 (cost pinned at 120)
- `ra2_allies_gi`: HP 50000, Speed 50, Range 5210, each offensive warhead Damage 400 (×1 = SUM 400), ReloadDelay 15, Burst 3, **DELETE the unconditional FirepowerMultiplier (25%)** — the Damage above already includes it (W17)
- `ra2_soviets_conscript`: HP 26000, Speed 62, Range 4530, each offensive warhead Damage 300 (×1 = SUM 300), ReloadDelay 18, Burst 1, **DELETE the unconditional FirepowerMultiplier (19%)** — the Damage above already includes it (W17), residual Δ -50.6 (cost pinned at 100)
- `asianalliance_asianmilitia`: HP 28000, Speed 58, Range 4550, each offensive warhead Damage 500 (×1 = SUM 500), ReloadDelay 50, Burst 1, **DELETE the unconditional FirepowerMultiplier (83%)** — the Damage above already includes it (W17), residual Δ -60.1 (cost pinned at 110)
- `futuretech_scoutdroid`: HP 33000, Speed 70, Range 5490, each offensive warhead Damage 1800 (×1 = SUM 1800), ReloadDelay 40, Burst 4, residual Δ +1.0 (cost pinned at 200)
- `naxis_coneheadsknights`: HP 22000, Speed 72, Range 4540, each offensive warhead Damage 26100 (×1 = SUM 26100), ReloadDelay 18, Burst 1
- `naxis_naxiriflerecruit`: HP 21000, Speed 57, Range 5220, each offensive warhead Damage 600 (×1 = SUM 600), ReloadDelay 100, Burst 1, **DELETE the unconditional FirepowerMultiplier (67%)** — the Damage above already includes it (W17), residual Δ -33.0 (cost pinned at 75)
- `undead.nax`: HP 14000, Speed 52, Range 5430, each offensive warhead Damage 100 (×1 = SUM 100), ReloadDelay 75, Burst 1, residual Δ -67.8 (cost pinned at 100)
- `latinsyndicate_latinmilitia`: HP 29000, Speed 56, Range 5400, each offensive warhead Damage 700 (×1 = SUM 700), ReloadDelay 22, Burst 3, **DELETE the unconditional FirepowerMultiplier (39%)** — the Damage above already includes it (W17), residual Δ -78.6 (cost pinned at 130)
- `tkm_marine`: HP 18000, Speed 60, Range 5190, each offensive warhead Damage 1100 (×1 = SUM 1100), ReloadDelay 16, Burst 5
- `tkm_rifleman`: HP 23000, Speed 61, Range 5040, each offensive warhead Damage 800 (×1 = SUM 800), ReloadDelay 75, Burst 1, **DELETE the unconditional FirepowerMultiplier (89%)** — the Damage above already includes it (W17), residual Δ -74.1 (cost pinned at 120)
- `ra1_soviets_ak47conscript`: HP 43000, Speed 71, Range 4820, each offensive warhead Damage 900 (×1 = SUM 900), ReloadDelay 11, Burst 3, **DELETE the unconditional FirepowerMultiplier (14%)** — the Damage above already includes it (W17), residual Δ +196.7 (cost pinned at 200)
- `ra1_soviets_rifleinfantry`: HP 34000, Speed 54, Range 4670, each offensive warhead Damage 1000 (×1 = SUM 1000), ReloadDelay 50, Burst 3, **DELETE the unconditional FirepowerMultiplier (42%)** — the Damage above already includes it (W17), residual Δ -45.0 (cost pinned at 100)
- `ra1_allies_rifleinfantry`: HP 30000, Speed 55, Range 5450, each offensive warhead Damage 1200 (×1 = SUM 1200), ReloadDelay 50, Burst 3, **DELETE the unconditional FirepowerMultiplier (47%)** — the Damage above already includes it (W17), residual Δ -47.9 (cost pinned at 100)
- `zerg_spithid`: HP 39000, Speed 72, Range 4500, each offensive warhead Damage 2200 (×1 = SUM 2200), ReloadDelay 15, Burst 1, residual Δ +1.4 (cost pinned at 300)
- `td_gdi_minigunner`: HP 25000, Speed 63, Range 5440, each offensive warhead Damage 1300 (×1 = SUM 1300), ReloadDelay 50, Burst 4, **DELETE the unconditional FirepowerMultiplier (24%)** — the Damage above already includes it (W17), residual Δ -49.9 (cost pinned at 100)
- `td_nod_minigunner`: HP 24000, Speed 67, Range 4610, each offensive warhead Damage 1400 (×1 = SUM 1400), ReloadDelay 50, Burst 4, **DELETE the unconditional FirepowerMultiplier (29%)** — the Damage above already includes it (W17), residual Δ -50.7 (cost pinned at 100)
- `forgotten_mutant`: HP 46000, Speed 65, Range 5230, each offensive warhead Damage 1500 (×1 = SUM 1500), ReloadDelay 18, Burst 2, **DELETE the unconditional FirepowerMultiplier (30%)** — the Damage above already includes it (W17), residual Δ +163.3 (cost pinned at 160)
- `forgotten_mutant_wild`: HP 44000, Speed 66, Range 5500, each offensive warhead Damage 1600 (×1 = SUM 1600), ReloadDelay 18, Burst 2, residual Δ +184.6 (cost pinned at 160)
- `ts_gdi_lightinfantry`: HP 17000, Speed 64, Range 4520, each offensive warhead Damage 1700 (×1 = SUM 1700), ReloadDelay 12, Burst 1, residual Δ -81.4 (cost pinned at 120)
- `ts_nod_lightinfantry`: HP 15000, Speed 59, Range 4510, each offensive warhead Damage 1900 (×1 = SUM 1900), ReloadDelay 12, Burst 1, residual Δ -85.7 (cost pinned at 120)
