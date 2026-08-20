# Melee infantry rebalance proposal

Anchor spec: HP=27000, Speed=90, Range=1500, eff-DPS=300, Cost=280

Converter law: cost pinned, range clamped to band + made unique, eff-DPS trimmed to Δ≤1 via 2000-grid warhead Damage × 1% FirepowerMultiplier.

| actor | faction | HP | spd | rng | cost | dmg/wh×n | rl | burst | FP% | eff DPS | price | Δ | flags |
|---|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|---|
| `heavy_inf.ixian` | d2k_ixian | 33000 | 77 | 1750 | 400 | 2000×4 | 69 | 4 | 106 | 430.1 | 399 | -0.7 |  |
| `ordos_contaminator` | d2k_ordos | 75000 | 81 | 1670 | 500 | 4000×1 | 20 | 1 | 124 | 186.0 | 491 | -9.0 |  |
| `ra2_allies_attackdog` | redalert2_allies | 5000 | 102 | 1650 | 200 | 2000×1 | 10 | 1 | 5 | 0.0 | 66 | -133.8 |  |
| `ra2_soviets_attackdog` | redalert2_soviets | 6000 | 103 | 1630 | 200 | 2000×1 | 10 | 1 | 6 | 0.0 | 69 | -130.9 |  |
| `yuri_brute` | redalert2_yuri | 45000 | 80 | 1500 | 400 | 20000×1 | 37 | 1 | 100 | 405.4 | 434 | +33.5 | verifier |
| `asianalliance_alligator` | redalert2mod_asianalliance | 27000 | 108 | 1500 | 300 | 16000×1 | 39 | 1 | 100 | 307.7 | 317 | +17.3 | anchor |
| `asianalliance_japanesesamurai` | redalert2mod_asianalliance | 39000 | 72 | 1750 | 350 | 2000×1 | 0 | 1 | 7 | 0.0 | 134 | -216.5 |  |
| `futuretech_blackwidow` | redalert2mod_futuretech | 25000 | 75 | 1740 | 1200 | 28000×1 | 16 | 2 | 103 | 2276.8 | 1196 | -4.5 |  |
| `futuretech_enforcer` | redalert2mod_futuretech | 31000 | 76 | 1710 | 300 | 2000×6 | 40 | 1 | 105 | 299.2 | 298 | -2.4 |  |
| `frank.nax` | redalert2mod_naxis | 85000 | 74 | 1730 | 500 | 10000×1 | 37 | 1 | 108 | 218.9 | 500 | -0.0 | soft |
| `naxis_slave` | redalert2mod_naxis | 10000 | 80 | 1690 | 250 | 22000×1 | 30 | 1 | 97 | 533.5 | 268 | +17.6 |  |
| `latinsyndicate_terrorist` | redalert2mod_syndicate | 15000 | 79 | 1690 | 200 | 2000×1 | 0 | 1 | 8 | 0.0 | 82 | -117.5 | shared-wpn? |
| `tkm_spetsnaz` | redalert2mod_tkm | 49000 | 104 | 1580 | 900 | 4000×1 | 10 | 5 | 88 | 733.3 | 873 | -27.1 |  |
| `tkm_thermonaut` | redalert2mod_tkm | 60000 | 91 | 1570 | 500 | 2000×1 | 32 | 13 | 62 | 287.9 | 485 | -15.4 |  |
| `japan_samurai` | redalert_japan | 35000 | 78 | 1710 | 300 | 6000×1 | 20 | 1 | 116 | 261.0 | 300 | +0.1 |  |
| `ra1_soviets_attackdog` | redalert_soviets | 4000 | 101 | 1750 | 200 | 2000×1 | 10 | 1 | 9 | 0.0 | 65 | -135.4 |  |
| `ra1_soviets_cyberdog` | redalert_soviets | 48000 | 99 | 1650 | 1000 | 2000×1 | 10 | 1 | 10 | 0.0 | 184 | -815.9 |  |
| `protoss_amaranth` | starcraft_protoss | 70000 | 82 | 1730 | 1200 | 20000×1 | 20 | 1 | 97 | 727.5 | 1216 | +16.1 |  |
| `protoss_darktemplar` | starcraft_protoss | 53000 | 83 | 1670 | 600 | 12000×1 | 25 | 1 | 106 | 381.6 | 596 | -4.3 |  |
| `protoss_legionnaire` | starcraft_protoss | 59000 | 84 | 1370 | 700 | 22000×1 | 26 | 1 | 101 | 641.0 | 702 | +1.6 |  |
| `protoss_zealot` | starcraft_protoss | 40000 | 86 | 1360 | 300 | 6000×1 | 30 | 2 | 102 | 255.0 | 302 | +1.7 |  |
| `terran_firebat` | starcraft_terran | 51000 | 89 | 1600 | 500 | 2000×1 | 0 | 1 | 11 | 0.0 | 179 | -320.8 |  |
| `terran_harakan` | starcraft_terran | 78000 | 90 | 1590 | 700 | 2000×1 | 0 | 1 | 12 | 0.0 | 250 | -449.7 |  |
| `zerg_infestedterranbomber` | starcraft_zerg | 61000 | 107 | 1490 | 400 | 2000×1 | 0 | 1 | 13 | 0.0 | 229 | -171.0 | shared-wpn? |
| `zerg_talon` | starcraft_zerg | 28000 | 108 | 1480 | 300 | 6000×1 | 15 | 1 | 85 | 255.0 | 290 | -10.3 |  |
| `zerg_ultralisk` | starcraft_zerg | 400000 | 105 | 1470 | 4400 | 16000×1 | 15 | 1 | 99 | 792.0 | 4169 | -231.3 |  |
| `zerg_zergling` | starcraft_zerg | 11000 | 108 | 1310 | 200 | 6000×1 | 11 | 1 | 84 | 343.6 | 200 | -0.2 |  |
| `td_nod_chemicalwarrior` | tiberiandawn_nod | 47000 | 87 | 1630 | 500 | 38000×1 | 48 | 1 | 97 | 575.9 | 478 | -22.4 | shared-wpn? |
| `td_nod_flamethrower` | tiberiandawn_nod | 19000 | 88 | 1610 | 200 | 16000×1 | 60 | 1 | 100 | 200.0 | 191 | -8.7 | shared-wpn? |
| `forgotten_chemsprayinfantry` | tiberiansun_forgotten | 64000 | 73 | 1750 | 700 | 16000×2 | 55 | 1 | 104 | 529.5 | 698 | -2.1 |  |
| `forgotten_runnershotgal` | tiberiansun_forgotten | 30000 | 85 | 1730 | 750 | 6000×6 | 32 | 1 | 91 | 972.6 | 740 | -9.8 |  |
| `forgotten_zombiemutant` | tiberiansun_forgotten | 46000 | 100 | 1250 | 500 | 12000×1 | 20 | 1 | 104 | 468.0 | 500 | +0.2 |  |
| `ts_gdi_riottrooper` | tiberiansun_gdi | 54000 | 92 | 1560 | 700 | 4000×6 | 46 | 1 | 109 | 540.3 | 677 | -23.0 |  |
| `ts_nod_chameleonspy` | tiberiansun_nod | 32000 | 93 | 1550 | 500 | 40000×1 | 52 | 1 | 101 | 582.7 | 501 | +1.4 |  |
| `ts_nod_shadowteam` | tiberiansun_nod | 26000 | 94 | 1540 | 900 | 14000×1 | 12 | 2 | 96 | 1344.0 | 869 | -31.3 |  |
| `wc2_humans_footman` | warcraft2_humans | 50000 | 95 | 1340 | 500 | 8000×1 | 15 | 1 | 105 | 420.0 | 499 | -0.9 | shared-wpn? |
| `wc2_humans_knight` | warcraft2_humans | 167500 | 108 | 1420 | 1600 | 10000×1 | 12 | 1 | 105 | 656.2 | 1597 | -3.4 |  |
| `wc2_humans_militiapeasant` | warcraft2_humans | 20000 | 96 | 1330 | 300 | 10000×1 | 15 | 1 | 92 | 460.0 | 299 | -0.8 | shared-wpn? |
| `wc2_humans_warcraft3footman` | warcraft2_humans | 80000 | 97 | 1530 | 900 | 10000×1 | 15 | 1 | 103 | 515.0 | 911 | +10.8 |  |
| `wc2_orcs_grunt` | warcraft2_orcs | 65000 | 98 | 1520 | 600 | 8000×1 | 18 | 1 | 103 | 343.3 | 592 | -7.9 | shared-wpn? |
| `wc2_orcs_ogre` | warcraft2_orcs | 200000 | 105 | 1510 | 1800 | 16000×1 | 20 | 1 | 94 | 564.0 | 1714 | -85.9 |  |
| `wc2_orcs_warcraft3grunt` | warcraft2_orcs | 120000 | 106 | 1500 | 1100 | 8000×1 | 18 | 1 | 107 | 356.7 | 1063 | -37.1 |  |

**Worst |Δ| among non-anchor members: 815.9** (goal ≤1).

## Uniqueness check (5 raw stats — soft/protected excluded)

- **Speed duplicates**: {108: ['zerg_talon', 'zerg_zergling', 'wc2_humans_knight'], 105: ['zerg_ultralisk', 'wc2_orcs_ogre']}
- **Range duplicates**: {1750: ['heavy_inf.ixian', 'asianalliance_japanesesamurai', 'ra1_soviets_attackdog', 'forgotten_chemsprayinfantry'], 1670: ['ordos_contaminator', 'protoss_darktemplar'], 1650: ['ra2_allies_attackdog', 'ra1_soviets_cyberdog'], 1630: ['ra2_soviets_attackdog', 'td_nod_chemicalwarrior'], 1710: ['futuretech_enforcer', 'japan_samurai'], 1690: ['naxis_slave', 'latinsyndicate_terrorist'], 1730: ['protoss_amaranth', 'forgotten_runnershotgal']}
- **raw ReloadDelay duplicates** (design choice — retune coarse Damage/reload by hand): {20: ['ordos_contaminator', 'japan_samurai', 'protoss_amaranth', 'forgotten_zombiemutant', 'wc2_orcs_ogre'], 10: ['ra2_allies_attackdog', 'ra2_soviets_attackdog', 'tkm_spetsnaz', 'ra1_soviets_attackdog', 'ra1_soviets_cyberdog'], 0: ['asianalliance_japanesesamurai', 'latinsyndicate_terrorist', 'terran_firebat', 'terran_harakan', 'zerg_infestedterranbomber'], 30: ['naxis_slave', 'protoss_zealot'], 32: ['tkm_thermonaut', 'forgotten_runnershotgal'], 15: ['zerg_talon', 'zerg_ultralisk', 'wc2_humans_footman', 'wc2_humans_militiapeasant', 'wc2_humans_warcraft3footman'], 12: ['ts_nod_shadowteam', 'wc2_humans_knight'], 18: ['wc2_orcs_grunt', 'wc2_orcs_warcraft3grunt']}

## Required YAML edits (per unit)

- `heavy_inf.ixian`: HP 33000, Speed 77, Range 1750, each offensive warhead Damage 2000 (×4 = SUM 8000), ReloadDelay 69, Burst 4, FirepowerMultiplier@HEAVYINF.IXIAN 106
- `ordos_contaminator`: HP 75000, Speed 81, Range 1670, each offensive warhead Damage 4000 (×1 = SUM 4000), ReloadDelay 20, Burst 1, FirepowerMultiplier@ORDOSCONTAMINATOR 124, residual Δ -9.0 (cost pinned at 500)
- `ra2_allies_attackdog`: HP 5000, Speed 102, Range 1650, each offensive warhead Damage 2000 (×1 = SUM 2000), ReloadDelay 10, Burst 1, FirepowerMultiplier@RA2ALLIESATTACKDOG 5, residual Δ -133.8 (cost pinned at 200)
- `ra2_soviets_attackdog`: HP 6000, Speed 103, Range 1630, each offensive warhead Damage 2000 (×1 = SUM 2000), ReloadDelay 10, Burst 1, FirepowerMultiplier@RA2SOVIETSATTACKDOG 6, residual Δ -130.9 (cost pinned at 200)
- `asianalliance_japanesesamurai`: HP 39000, Speed 72, Range 1750, each offensive warhead Damage 2000 (×1 = SUM 2000), ReloadDelay 0, Burst 1, FirepowerMultiplier@ASIANALLIANCEJAPANESESAMURAI 7, residual Δ -216.5 (cost pinned at 350)
- `futuretech_blackwidow`: HP 25000, Speed 75, Range 1740, each offensive warhead Damage 28000 (×1 = SUM 28000), ReloadDelay 16, Burst 2, FirepowerMultiplier@FUTURETECHBLACKWIDOW 103, residual Δ -4.5 (cost pinned at 1200)
- `futuretech_enforcer`: HP 31000, Speed 76, Range 1710, each offensive warhead Damage 2000 (×6 = SUM 12000), ReloadDelay 40, Burst 1, FirepowerMultiplier@FUTURETECHENFORCER 105, residual Δ -2.4 (cost pinned at 300)
- `frank.nax`: HP 85000, Speed 74, Range 1730, each offensive warhead Damage 10000 (×1 = SUM 10000), ReloadDelay 37, Burst 1, FirepowerMultiplier@FRANK.NAX 108
- `naxis_slave`: HP 10000, Speed 80, Range 1690, each offensive warhead Damage 22000 (×1 = SUM 22000), ReloadDelay 30, Burst 1, FirepowerMultiplier@NAXISSLAVE 97, residual Δ +17.6 (cost pinned at 250)
- `latinsyndicate_terrorist`: HP 15000, Speed 79, Range 1690, each offensive warhead Damage 2000 (×1 = SUM 2000), ReloadDelay 0, Burst 1, FirepowerMultiplier@LATINSYNDICATETERRORIST 8, residual Δ -117.5 (cost pinned at 200)
- `tkm_spetsnaz`: HP 49000, Speed 104, Range 1580, each offensive warhead Damage 4000 (×1 = SUM 4000), ReloadDelay 10, Burst 5, FirepowerMultiplier@TKMSPETSNAZ 88, residual Δ -27.1 (cost pinned at 900)
- `tkm_thermonaut`: HP 60000, Speed 91, Range 1570, each offensive warhead Damage 2000 (×1 = SUM 2000), ReloadDelay 32, Burst 13, FirepowerMultiplier@TKMTHERMONAUT 62, residual Δ -15.4 (cost pinned at 500)
- `japan_samurai`: HP 35000, Speed 78, Range 1710, each offensive warhead Damage 6000 (×1 = SUM 6000), ReloadDelay 20, Burst 1, FirepowerMultiplier@JAPANSAMURAI 116
- `ra1_soviets_attackdog`: HP 4000, Speed 101, Range 1750, each offensive warhead Damage 2000 (×1 = SUM 2000), ReloadDelay 10, Burst 1, FirepowerMultiplier@RA1SOVIETSATTACKDOG 9, residual Δ -135.4 (cost pinned at 200)
- `ra1_soviets_cyberdog`: HP 48000, Speed 99, Range 1650, each offensive warhead Damage 2000 (×1 = SUM 2000), ReloadDelay 10, Burst 1, FirepowerMultiplier@RA1SOVIETSCYBERDOG 10, residual Δ -815.9 (cost pinned at 1000)
- `protoss_amaranth`: HP 70000, Speed 82, Range 1730, each offensive warhead Damage 20000 (×1 = SUM 20000), ReloadDelay 20, Burst 1, FirepowerMultiplier@PROTOSSAMARANTH 97, residual Δ +16.1 (cost pinned at 1200)
- `protoss_darktemplar`: HP 53000, Speed 83, Range 1670, each offensive warhead Damage 12000 (×1 = SUM 12000), ReloadDelay 25, Burst 1, FirepowerMultiplier@PROTOSSDARKTEMPLAR 106, residual Δ -4.3 (cost pinned at 600)
- `protoss_legionnaire`: HP 59000, Speed 84, Range 1370, each offensive warhead Damage 22000 (×1 = SUM 22000), ReloadDelay 26, Burst 1, FirepowerMultiplier@PROTOSSLEGIONNAIRE 101, residual Δ +1.6 (cost pinned at 700)
- `protoss_zealot`: HP 40000, Speed 86, Range 1360, each offensive warhead Damage 6000 (×1 = SUM 6000), ReloadDelay 30, Burst 2, FirepowerMultiplier@PROTOSSZEALOT 102, residual Δ +1.7 (cost pinned at 300)
- `terran_firebat`: HP 51000, Speed 89, Range 1600, each offensive warhead Damage 2000 (×1 = SUM 2000), ReloadDelay 0, Burst 1, FirepowerMultiplier@TERRANFIREBAT 11, residual Δ -320.8 (cost pinned at 500)
- `terran_harakan`: HP 78000, Speed 90, Range 1590, each offensive warhead Damage 2000 (×1 = SUM 2000), ReloadDelay 0, Burst 1, FirepowerMultiplier@TERRANHARAKAN 12, residual Δ -449.7 (cost pinned at 700)
- `zerg_infestedterranbomber`: HP 61000, Speed 107, Range 1490, each offensive warhead Damage 2000 (×1 = SUM 2000), ReloadDelay 0, Burst 1, FirepowerMultiplier@ZERGINFESTEDTERRANBOMBER 13, residual Δ -171.0 (cost pinned at 400)
- `zerg_talon`: HP 28000, Speed 108, Range 1480, each offensive warhead Damage 6000 (×1 = SUM 6000), ReloadDelay 15, Burst 1, FirepowerMultiplier@ZERGTALON 85, residual Δ -10.3 (cost pinned at 300)
- `zerg_ultralisk`: HP 400000, Speed 105, Range 1470, each offensive warhead Damage 16000 (×1 = SUM 16000), ReloadDelay 15, Burst 1, FirepowerMultiplier@ZERGULTRALISK 99, residual Δ -231.3 (cost pinned at 4400)
- `zerg_zergling`: HP 11000, Speed 108, Range 1310, each offensive warhead Damage 6000 (×1 = SUM 6000), ReloadDelay 11, Burst 1, FirepowerMultiplier@ZERGZERGLING 84
- `td_nod_chemicalwarrior`: HP 47000, Speed 87, Range 1630, each offensive warhead Damage 38000 (×1 = SUM 38000), ReloadDelay 48, Burst 1, FirepowerMultiplier@TDNODCHEMICALWARRIOR 97, residual Δ -22.4 (cost pinned at 500)
- `td_nod_flamethrower`: HP 19000, Speed 88, Range 1610, each offensive warhead Damage 16000 (×1 = SUM 16000), ReloadDelay 60, Burst 1, FirepowerMultiplier@TDNODFLAMETHROWER 100, residual Δ -8.7 (cost pinned at 200)
- `forgotten_chemsprayinfantry`: HP 64000, Speed 73, Range 1750, each offensive warhead Damage 16000 (×2 = SUM 32000), ReloadDelay 55, Burst 1, FirepowerMultiplier@FORGOTTENCHEMSPRAYINFANTRY 104, residual Δ -2.1 (cost pinned at 700)
- `forgotten_runnershotgal`: HP 30000, Speed 85, Range 1730, each offensive warhead Damage 6000 (×6 = SUM 36000), ReloadDelay 32, Burst 1, FirepowerMultiplier@FORGOTTENRUNNERSHOTGAL 91, residual Δ -9.8 (cost pinned at 750)
- `forgotten_zombiemutant`: HP 46000, Speed 100, Range 1250, each offensive warhead Damage 12000 (×1 = SUM 12000), ReloadDelay 20, Burst 1, FirepowerMultiplier@FORGOTTENZOMBIEMUTANT 104
- `ts_gdi_riottrooper`: HP 54000, Speed 92, Range 1560, each offensive warhead Damage 4000 (×6 = SUM 24000), ReloadDelay 46, Burst 1, FirepowerMultiplier@TSGDIRIOTTROOPER 109, residual Δ -23.0 (cost pinned at 700)
- `ts_nod_chameleonspy`: HP 32000, Speed 93, Range 1550, each offensive warhead Damage 40000 (×1 = SUM 40000), ReloadDelay 52, Burst 1, FirepowerMultiplier@TSNODCHAMELEONSPY 101, residual Δ +1.4 (cost pinned at 500)
- `ts_nod_shadowteam`: HP 26000, Speed 94, Range 1540, each offensive warhead Damage 14000 (×1 = SUM 14000), ReloadDelay 12, Burst 2, FirepowerMultiplier@TSNODSHADOWTEAM 96, residual Δ -31.3 (cost pinned at 900)
- `wc2_humans_footman`: HP 50000, Speed 95, Range 1340, each offensive warhead Damage 8000 (×1 = SUM 8000), ReloadDelay 15, Burst 1, FirepowerMultiplier@WC2HUMANSFOOTMAN 105
- `wc2_humans_knight`: HP 167500, Speed 108, Range 1420, each offensive warhead Damage 10000 (×1 = SUM 10000), ReloadDelay 12, Burst 1, FirepowerMultiplier@WC2HUMANSKNIGHT 105, residual Δ -3.4 (cost pinned at 1600)
- `wc2_humans_militiapeasant`: HP 20000, Speed 96, Range 1330, each offensive warhead Damage 10000 (×1 = SUM 10000), ReloadDelay 15, Burst 1, FirepowerMultiplier@WC2HUMANSMILITIAPEASANT 92
- `wc2_humans_warcraft3footman`: HP 80000, Speed 97, Range 1530, each offensive warhead Damage 10000 (×1 = SUM 10000), ReloadDelay 15, Burst 1, FirepowerMultiplier@WC2HUMANSWARCRAFT3FOOTMAN 103, residual Δ +10.8 (cost pinned at 900)
- `wc2_orcs_grunt`: HP 65000, Speed 98, Range 1520, each offensive warhead Damage 8000 (×1 = SUM 8000), ReloadDelay 18, Burst 1, FirepowerMultiplier@WC2ORCSGRUNT 103, residual Δ -7.9 (cost pinned at 600)
- `wc2_orcs_ogre`: HP 200000, Speed 105, Range 1510, each offensive warhead Damage 16000 (×1 = SUM 16000), ReloadDelay 20, Burst 1, FirepowerMultiplier@WC2ORCSOGRE 94, residual Δ -85.9 (cost pinned at 1800)
- `wc2_orcs_warcraft3grunt`: HP 120000, Speed 106, Range 1500, each offensive warhead Damage 8000 (×1 = SUM 8000), ReloadDelay 18, Burst 1, FirepowerMultiplier@WC2ORCSWARCRAFT3GRUNT 107, residual Δ -37.1 (cost pinned at 1100)
