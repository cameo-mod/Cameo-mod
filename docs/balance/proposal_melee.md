# Melee infantry rebalance proposal

Anchor spec: HP=27000, Speed=90, Range=1500, eff-DPS=300, Cost=280

Converter law: cost pinned, range clamped to band + made unique, eff-DPS trimmed to Δ≤1 via 100-grid warhead Damage; unconditional FirepowerMultiplier is retired.

Uniqueness separates **raw warhead Damage (the 5-stat law as written)**. The levers run coarsest-first — Damage (100 grid) → Speed (1) → Range (10) — because one Damage step is a whole shot and running it last threw away everything the fine levers had achieved.

| actor | faction | HP | spd | rng | cost | dmg/wh×n | rl | burst | legacy FP% | eff DPS | price | Δ | flags |
|---|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|---|
| `heavy_inf.ixian` | d2k_ixian | 33000 | 77 | 1730 | 400 | 7400×1 | 69 | 4 | 100 | 429.0 | 395 | -4.5 |  |
| `ordos_contaminator` | d2k_ordos | 75000 | 81 | 1710 | 500 | 3800×1 | 20 | 1 | 100 | 190.0 | 503 | +3.2 |  |
| `ra2_allies_attackdog` | redalert2_allies | 5000 | 102 | 1660 | 200 | 100×1 | 10 | 1 | 100 | 0.0 | 66 | -133.6 |  |
| `ra2_soviets_attackdog` | redalert2_soviets | 6000 | 103 | 1650 | 200 | 200×1 | 10 | 1 | 100 | 0.0 | 69 | -130.6 |  |
| `yuri_brute` | redalert2_yuri | 45000 | 106 | 1540 | 400 | 9000×1 | 37 | 1 | 100 | 243.2 | 392 | -7.8 |  |
| `asianalliance_alligator` | redalert2mod_asianalliance | 27000 | 108 | 1500 | 300 | 16000×1 | 39 | 1 | 100 | 410.3 | 380 | +79.6 | anchor |
| `asianalliance_japanesesamurai` | redalert2mod_asianalliance | 39000 | 72 | 1750 | 350 | 300×1 | 0 | 1 | 100 | 0.0 | 100 | -249.9 |  |
| `futuretech_blackwidow` | redalert2mod_futuretech | 25000 | 75 | 1730 | 1200 | 21800×1 | 16 | 2 | 100 | 2294.7 | 1198 | -1.5 |  |
| `futuretech_enforcer` | redalert2mod_futuretech | 31000 | 76 | 1740 | 300 | 1700×7 | 40 | 1 | 100 | 297.5 | 300 | -0.3 |  |
| `frank.nax` | redalert2mod_naxis | 85000 | 74 | 1750 | 500 | 8100×1 | 37 | 1 | 100 | 218.9 | 503 | +3.1 | soft |
| `naxis_slave` | redalert2mod_naxis | 10000 | 80 | 1490 | 250 | 17200×1 | 30 | 1 | 100 | 573.3 | 250 | +0.3 |  |
| `latinsyndicate_terrorist` | redalert2mod_syndicate | 15000 | 79 | 1750 | 200 | 400×1 | 0 | 1 | 200 | 0.0 | 83 | -116.6 | fp-debt |
| `tkm_spetsnaz` | redalert2mod_tkm | 49000 | 104 | 1600 | 900 | 4400×1 | 10 | 5 | 100 | 1222.2 | 874 | -25.7 |  |
| `tkm_thermonaut` | redalert2mod_tkm | 60000 | 91 | 1590 | 500 | 2400×1 | 32 | 13 | 100 | 557.1 | 491 | -8.8 |  |
| `japan_samurai` | redalert_japan | 35000 | 78 | 1710 | 300 | 5200×1 | 20 | 1 | 100 | 260.0 | 299 | -0.6 |  |
| `ra1_soviets_attackdog` | redalert_soviets | 4000 | 101 | 1680 | 200 | 500×1 | 10 | 1 | 100 | 0.0 | 64 | -136.5 |  |
| `ra1_soviets_cyberdog` | redalert_soviets | 48000 | 99 | 1670 | 1000 | 600×1 | 10 | 1 | 100 | 0.0 | 184 | -815.6 |  |
| `protoss_amaranth` | starcraft_protoss | 70000 | 82 | 1730 | 1200 | 14600×1 | 20 | 1 | 100 | 730.0 | 1220 | +19.5 |  |
| `protoss_darktemplar` | starcraft_protoss | 53000 | 83 | 1690 | 600 | 9500×1 | 25 | 1 | 100 | 380.0 | 599 | -1.1 |  |
| `protoss_legionnaire` | starcraft_protoss | 59000 | 84 | 1370 | 700 | 16700×1 | 26 | 1 | 100 | 642.3 | 703 | +2.7 |  |
| `protoss_zealot` | starcraft_protoss | 40000 | 86 | 1360 | 300 | 4600×1 | 30 | 2 | 100 | 255.6 | 302 | +2.0 |  |
| `terran_firebat` | starcraft_terran | 51000 | 89 | 1620 | 500 | 700×1 | 0 | 1 | 50 | 0.0 | 180 | -320.5 | fp-debt |
| `terran_harakan` | starcraft_terran | 78000 | 90 | 1610 | 700 | 800×1 | 0 | 1 | 25 | 0.0 | 251 | -449.4 | fp-debt |
| `zerg_infestedterranbomber` | starcraft_zerg | 61000 | 107 | 1470 | 400 | 900×1 | 0 | 1 | 100 | 0.0 | 175 | -224.5 |  |
| `zerg_talon` | starcraft_zerg | 28000 | 108 | 1530 | 300 | 3900×1 | 15 | 1 | 100 | 260.0 | 298 | -1.8 |  |
| `zerg_zergling` | starcraft_zerg | 11000 | 108 | 1320 | 200 | 3700×1 | 11 | 1 | 150 | 336.4 | 198 | -1.9 | fp-debt |
| `td_nod_chemicalwarrior` | tiberiandawn_nod | 47000 | 87 | 1640 | 500 | 27500×1 | 48 | 1 | 100 | 572.9 | 478 | -22.2 | shared-wpn? |
| `td_nod_flamethrower` | tiberiandawn_nod | 19000 | 88 | 1630 | 200 | 12800×1 | 60 | 1 | 100 | 213.3 | 199 | -1.0 | shared-wpn? |
| `forgotten_chemsprayinfantry` | tiberiansun_forgotten | 64000 | 73 | 1750 | 700 | 37000×1 | 55 | 1 | 100 | 672.7 | 699 | -0.6 |  |
| `forgotten_runnershotgal` | tiberiansun_forgotten | 30000 | 85 | 1730 | 750 | 4500×7 | 32 | 1 | 100 | 984.4 | 748 | -2.3 |  |
| `forgotten_zombiemutant` | tiberiansun_forgotten | 44000 | 100 | 1250 | 500 | 9800×1 | 20 | 1 | 100 | 490.0 | 500 | -0.5 |  |
| `ts_gdi_riottrooper` | tiberiansun_gdi | 54000 | 92 | 1580 | 700 | 3600×7 | 46 | 1 | 100 | 547.8 | 690 | -10.1 |  |
| `ts_nod_chameleonspy` | tiberiansun_nod | 32000 | 93 | 1520 | 500 | 30700×1 | 52 | 1 | 100 | 590.4 | 499 | -0.5 |  |
| `ts_nod_shadowteam` | tiberiansun_nod | 26000 | 94 | 1570 | 900 | 14500×1 | 12 | 2 | 100 | 1933.3 | 884 | -15.7 |  |
| `wc2_humans_footman` | warcraft2_humans | 50000 | 95 | 1340 | 500 | 6300×1 | 15 | 1 | 130 | 420.0 | 499 | -0.9 | shared-wpn? fp-debt |
| `wc2_humans_militiapeasant` | warcraft2_humans | 20000 | 96 | 1330 | 300 | 6900×1 | 15 | 1 | 100 | 460.0 | 299 | -0.8 | shared-wpn? |
| `wc2_humans_warcraft3footman` | warcraft2_humans | 80000 | 97 | 1510 | 900 | 7700×1 | 15 | 1 | 130 | 513.3 | 901 | +0.5 | fp-debt |
| `wc2_orcs_grunt` | warcraft2_orcs | 65000 | 98 | 1560 | 600 | 6200×1 | 18 | 1 | 130 | 344.4 | 603 | +2.8 | shared-wpn? fp-debt |
| `wc2_orcs_warcraft3grunt` | warcraft2_orcs | 100000 | 105 | 1550 | 1100 | 8200×1 | 18 | 1 | 130 | 455.6 | 1081 | -19.1 | fp-debt |

**Worst |Δ| among non-anchor members: 815.6** (goal ≤1).

## Uniqueness check (5 raw stats — soft/protected excluded)

- **Speed duplicates**: {108: ['zerg_talon', 'zerg_zergling']}
- **Range duplicates**: {1730: ['heavy_inf.ixian', 'futuretech_blackwidow', 'protoss_amaranth', 'forgotten_runnershotgal'], 1710: ['ordos_contaminator', 'japan_samurai'], 1750: ['asianalliance_japanesesamurai', 'latinsyndicate_terrorist', 'forgotten_chemsprayinfantry']}
- **raw ReloadDelay duplicates** (design choice — retune coarse Damage/reload by hand): {20: ['ordos_contaminator', 'japan_samurai', 'protoss_amaranth', 'forgotten_zombiemutant'], 10: ['ra2_allies_attackdog', 'ra2_soviets_attackdog', 'tkm_spetsnaz', 'ra1_soviets_attackdog', 'ra1_soviets_cyberdog'], 0: ['asianalliance_japanesesamurai', 'latinsyndicate_terrorist', 'terran_firebat', 'terran_harakan', 'zerg_infestedterranbomber'], 30: ['naxis_slave', 'protoss_zealot'], 32: ['tkm_thermonaut', 'forgotten_runnershotgal'], 15: ['zerg_talon', 'wc2_humans_footman', 'wc2_humans_militiapeasant', 'wc2_humans_warcraft3footman'], 18: ['wc2_orcs_grunt', 'wc2_orcs_warcraft3grunt']}

## Required YAML edits (per unit)

- `heavy_inf.ixian`: HP 33000, Speed 77, Range 1730, each offensive warhead Damage 7400 (×1 = SUM 7400), ReloadDelay 69, Burst 4, residual Δ -4.5 (cost pinned at 400)
- `ordos_contaminator`: HP 75000, Speed 81, Range 1710, each offensive warhead Damage 3800 (×1 = SUM 3800), ReloadDelay 20, Burst 1, residual Δ +3.2 (cost pinned at 500)
- `ra2_allies_attackdog`: HP 5000, Speed 102, Range 1660, each offensive warhead Damage 100 (×1 = SUM 100), ReloadDelay 10, Burst 1, residual Δ -133.6 (cost pinned at 200)
- `ra2_soviets_attackdog`: HP 6000, Speed 103, Range 1650, each offensive warhead Damage 200 (×1 = SUM 200), ReloadDelay 10, Burst 1, residual Δ -130.6 (cost pinned at 200)
- `yuri_brute`: HP 45000, Speed 106, Range 1540, each offensive warhead Damage 9000 (×1 = SUM 9000), ReloadDelay 37, Burst 1, residual Δ -7.8 (cost pinned at 400)
- `asianalliance_japanesesamurai`: HP 39000, Speed 72, Range 1750, each offensive warhead Damage 300 (×1 = SUM 300), ReloadDelay 0, Burst 1, residual Δ -249.9 (cost pinned at 350)
- `futuretech_blackwidow`: HP 25000, Speed 75, Range 1730, each offensive warhead Damage 21800 (×1 = SUM 21800), ReloadDelay 16, Burst 2, residual Δ -1.5 (cost pinned at 1200)
- `futuretech_enforcer`: HP 31000, Speed 76, Range 1740, each offensive warhead Damage 1700 (×7 = SUM 11900), ReloadDelay 40, Burst 1
- `frank.nax`: HP 85000, Speed 74, Range 1750, each offensive warhead Damage 8100 (×1 = SUM 8100), ReloadDelay 37, Burst 1, residual Δ +3.1 (cost pinned at 500)
- `naxis_slave`: HP 10000, Speed 80, Range 1490, each offensive warhead Damage 17200 (×1 = SUM 17200), ReloadDelay 30, Burst 1
- `latinsyndicate_terrorist`: HP 15000, Speed 79, Range 1750, each offensive warhead Damage 400 (×1 = SUM 400), ReloadDelay 0, Burst 1, **DELETE the unconditional FirepowerMultiplier (200%)** — the Damage above already includes it (W17), residual Δ -116.6 (cost pinned at 200)
- `tkm_spetsnaz`: HP 49000, Speed 104, Range 1600, each offensive warhead Damage 4400 (×1 = SUM 4400), ReloadDelay 10, Burst 5, residual Δ -25.7 (cost pinned at 900)
- `tkm_thermonaut`: HP 60000, Speed 91, Range 1590, each offensive warhead Damage 2400 (×1 = SUM 2400), ReloadDelay 32, Burst 13, residual Δ -8.8 (cost pinned at 500)
- `japan_samurai`: HP 35000, Speed 78, Range 1710, each offensive warhead Damage 5200 (×1 = SUM 5200), ReloadDelay 20, Burst 1
- `ra1_soviets_attackdog`: HP 4000, Speed 101, Range 1680, each offensive warhead Damage 500 (×1 = SUM 500), ReloadDelay 10, Burst 1, residual Δ -136.5 (cost pinned at 200)
- `ra1_soviets_cyberdog`: HP 48000, Speed 99, Range 1670, each offensive warhead Damage 600 (×1 = SUM 600), ReloadDelay 10, Burst 1, residual Δ -815.6 (cost pinned at 1000)
- `protoss_amaranth`: HP 70000, Speed 82, Range 1730, each offensive warhead Damage 14600 (×1 = SUM 14600), ReloadDelay 20, Burst 1, residual Δ +19.5 (cost pinned at 1200)
- `protoss_darktemplar`: HP 53000, Speed 83, Range 1690, each offensive warhead Damage 9500 (×1 = SUM 9500), ReloadDelay 25, Burst 1, residual Δ -1.1 (cost pinned at 600)
- `protoss_legionnaire`: HP 59000, Speed 84, Range 1370, each offensive warhead Damage 16700 (×1 = SUM 16700), ReloadDelay 26, Burst 1, residual Δ +2.7 (cost pinned at 700)
- `protoss_zealot`: HP 40000, Speed 86, Range 1360, each offensive warhead Damage 4600 (×1 = SUM 4600), ReloadDelay 30, Burst 2, residual Δ +2.0 (cost pinned at 300)
- `terran_firebat`: HP 51000, Speed 89, Range 1620, each offensive warhead Damage 700 (×1 = SUM 700), ReloadDelay 0, Burst 1, **DELETE the unconditional FirepowerMultiplier (50%)** — the Damage above already includes it (W17), residual Δ -320.5 (cost pinned at 500)
- `terran_harakan`: HP 78000, Speed 90, Range 1610, each offensive warhead Damage 800 (×1 = SUM 800), ReloadDelay 0, Burst 1, **DELETE the unconditional FirepowerMultiplier (25%)** — the Damage above already includes it (W17), residual Δ -449.4 (cost pinned at 700)
- `zerg_infestedterranbomber`: HP 61000, Speed 107, Range 1470, each offensive warhead Damage 900 (×1 = SUM 900), ReloadDelay 0, Burst 1, residual Δ -224.5 (cost pinned at 400)
- `zerg_talon`: HP 28000, Speed 108, Range 1530, each offensive warhead Damage 3900 (×1 = SUM 3900), ReloadDelay 15, Burst 1, residual Δ -1.8 (cost pinned at 300)
- `zerg_zergling`: HP 11000, Speed 108, Range 1320, each offensive warhead Damage 3700 (×1 = SUM 3700), ReloadDelay 11, Burst 1, **DELETE the unconditional FirepowerMultiplier (150%)** — the Damage above already includes it (W17), residual Δ -1.9 (cost pinned at 200)
- `td_nod_chemicalwarrior`: HP 47000, Speed 87, Range 1640, each offensive warhead Damage 27500 (×1 = SUM 27500), ReloadDelay 48, Burst 1, residual Δ -22.2 (cost pinned at 500)
- `td_nod_flamethrower`: HP 19000, Speed 88, Range 1630, each offensive warhead Damage 12800 (×1 = SUM 12800), ReloadDelay 60, Burst 1, residual Δ -1.0 (cost pinned at 200)
- `forgotten_chemsprayinfantry`: HP 64000, Speed 73, Range 1750, each offensive warhead Damage 37000 (×1 = SUM 37000), ReloadDelay 55, Burst 1
- `forgotten_runnershotgal`: HP 30000, Speed 85, Range 1730, each offensive warhead Damage 4500 (×7 = SUM 31500), ReloadDelay 32, Burst 1, residual Δ -2.3 (cost pinned at 750)
- `forgotten_zombiemutant`: HP 44000, Speed 100, Range 1250, each offensive warhead Damage 9800 (×1 = SUM 9800), ReloadDelay 20, Burst 1
- `ts_gdi_riottrooper`: HP 54000, Speed 92, Range 1580, each offensive warhead Damage 3600 (×7 = SUM 25200), ReloadDelay 46, Burst 1, residual Δ -10.1 (cost pinned at 700)
- `ts_nod_chameleonspy`: HP 32000, Speed 93, Range 1520, each offensive warhead Damage 30700 (×1 = SUM 30700), ReloadDelay 52, Burst 1
- `ts_nod_shadowteam`: HP 26000, Speed 94, Range 1570, each offensive warhead Damage 14500 (×1 = SUM 14500), ReloadDelay 12, Burst 2, residual Δ -15.7 (cost pinned at 900)
- `wc2_humans_footman`: HP 50000, Speed 95, Range 1340, each offensive warhead Damage 6300 (×1 = SUM 6300), ReloadDelay 15, Burst 1, **DELETE the unconditional FirepowerMultiplier (130%)** — the Damage above already includes it (W17)
- `wc2_humans_militiapeasant`: HP 20000, Speed 96, Range 1330, each offensive warhead Damage 6900 (×1 = SUM 6900), ReloadDelay 15, Burst 1
- `wc2_humans_warcraft3footman`: HP 80000, Speed 97, Range 1510, each offensive warhead Damage 7700 (×1 = SUM 7700), ReloadDelay 15, Burst 1, **DELETE the unconditional FirepowerMultiplier (130%)** — the Damage above already includes it (W17)
- `wc2_orcs_grunt`: HP 65000, Speed 98, Range 1560, each offensive warhead Damage 6200 (×1 = SUM 6200), ReloadDelay 18, Burst 1, **DELETE the unconditional FirepowerMultiplier (130%)** — the Damage above already includes it (W17), residual Δ +2.8 (cost pinned at 600)
- `wc2_orcs_warcraft3grunt`: HP 100000, Speed 105, Range 1550, each offensive warhead Damage 8200 (×1 = SUM 8200), ReloadDelay 18, Burst 1, **DELETE the unconditional FirepowerMultiplier (130%)** — the Damage above already includes it (W17), residual Δ -19.1 (cost pinned at 1100)
