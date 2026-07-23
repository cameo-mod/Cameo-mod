# Special Forces infantry rebalance proposal

Anchor spec: HP=15000, Speed=50, Range=6000, eff-DPS=240, Cost=200

| actor | faction | HP | spd | rng | cost | dmg | dmg_filter | burst | rl | FP% | wc | eff DPS | formula price | Δ | note |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| `ra2_allies_seal` | redalert2_allies | 31000 | 58 | 6500 | 1162 | 4000 | all | 4 | 10 | 93 | 0.750 | 587.4 | 628 | -534 |  |
| `ra2_soviets_flaktrooper` | redalert2_soviets | 10000 | 44 | 5630 | 416 | 16000 | all | 1 | 17 | 107 | 1.000 | 1007.1 | 416 | +0 |  |
| `yuri_gatlingtrooper` | redalert2_yuri | 36000 | 45 | 5510 | 431 | 16000 | all | 1 | 15 | 108 | 0.875 | 1008.0 | 803 | +372 |  |
| `schwarzermond_lunarsoldier` | redalert2mod_schwarzermond | 30000 | 50 | 6000 | 500 | 24000 | all | 1 | 50 | 100 | 1.000 | 480.0 | 500 | +0 | verifier |
| `tkm_trooper` | redalert2mod_tkm | 33000 | 59 | 5540 | 200 | 2000 | all | 5 | 31 | 37 | 1.000 | 105.7 | 250 | +50 |  |
| `ra1_allies_machinegunner` | redalert_allies | 19000 | 49 | 5610 | 557 | 16000 | all | 5 | 48 | 93 | 0.875 | 1085.0 | 660 | +103 |  |
| `japan_imperialscoutsman` | redalert_japan | 15000 | 50 | 6000 | 200 | 12000 | all | 1 | 50 | 100 | 1.000 | 240.0 | 200 | +0 | anchor |
| `ra1_soviets_dragunovantimaterialsniper` | redalert_soviets | 20000 | 40 | 5600 | 422 | 400000 | all | 1 | 85 | 103 | 1.062 | 5150.0 | 1902 | +1480 |  |
| `terran_ghost` | starcraft_terran | 44000 | 75 | 6490 | 1176 | 10000 | all | 1 | 22 | 103 | 1.000 | 468.2 | 720 | -456 |  |
| `terran_madcap` | starcraft_terran | 60000 | 60 | 5560 | 1003 | 36000 | all | 1 | 25 | 108 | 1.000 | 1555.2 | 3026 | +2023 |  |
| `terran_marine` | starcraft_terran | 41000 | 61 | 5550 | 689 | 36000 | all | 3 | 26 | 31 | 1.000 | 1116.0 | 1370 | +681 |  |
| `terran_specter` | starcraft_terran | 50000 | 80 | 6480 | 1744 | 20000 | all | 1 | 33 | 106 | 1.000 | 642.4 | 1085 | -659 |  |
| `zerg_hydralisk` | starcraft_zerg | 80000 | 76 | 5500 | 3314 | 72000 | all | 1 | 15 | 99 | 0.750 | 3564.0 | 8458 | +5144 |  |
| `td_gdi_officer` | tiberiandawn_gdi | 32000 | 79 | 5590 | 1532 | 16000 | all | 4 | 20 | 109 | 0.875 | 1907.5 | 2036 | +504 |  |
| `td_nod_lasertrooper` | tiberiandawn_nod | 59000 | 51 | 5580 | 750 | 144000 | all | 1 | 50 | 108 | 1.000 | 3110.4 | 2037 | +1287 |  |
| `td_nod_stealthsoldier` | tiberiandawn_nod | 25000 | 72 | 5570 | 753 | 110000 | all | 4 | 90 | 93 | 1.000 | 3897.1 | 3598 | +2845 |  |
| `cabal_eliminator800` | tiberiansun_cabal | 85000 | 39 | 5860 | 1450 | 4000 | all | 1 | 5 | 105 | 1.000 | 840.0 | 1451 | +1 |  |
| `forgotten_mutantsergeant` | tiberiansun_forgotten | 40000 | 74 | 5620 | 1154 | 16000 | all | 1 | 8 | 110 | 0.875 | 1925.0 | 1933 | +779 |  |
| `ts_gdi_falconenforcer` | tiberiansun_gdi | 45000 | 62 | 5530 | 1322 | 16000 | all | 3 | 26 | 95 | 1.000 | 1520.0 | 1958 | +636 |  |
| `ts_nod_elitecadre` | tiberiansun_nod | 21000 | 55 | 5520 | 435 | 16000 | all | 5 | 52 | 94 | 0.875 | 1096.7 | 567 | +132 |  |

## Uniqueness check (5 raw stats — maintainer law 2026-07-22)

- **raw ReloadDelay duplicates** (design choice — retune coarse Damage/reload by hand): {15: ['yuri_gatlingtrooper', 'zerg_hydralisk'], 26: ['terran_marine', 'ts_gdi_falconenforcer']}

## Required YAML edits (per unit)

- `ra2_allies_seal`: HP 31000, Speed 58, Range 6500, weapon Damage 4000 (all), ReloadDelay 10, Burst 4, FirepowerMultiplier@RA2ALLIESSEAL 93, formula price delta -534 (informational; cost pinned at 1162)
- `ra2_soviets_flaktrooper`: HP 10000, Speed 44, Range 5630, weapon Damage 16000 (all), ReloadDelay 17, Burst 1, FirepowerMultiplier@RA2SOVIETSFLAKTROOPER 107
- `yuri_gatlingtrooper`: HP 36000, Speed 45, Range 5510, weapon Damage 16000 (all), ReloadDelay 15, Burst 1, FirepowerMultiplier@YURIGATLINGTROOPER 108, formula price delta +372 (informational; cost pinned at 431)
- `tkm_trooper`: HP 33000, Speed 59, Range 5540, weapon Damage 2000 (all), ReloadDelay 31, Burst 5, FirepowerMultiplier@TKMTROOPER 37, formula price delta +50 (informational; cost pinned at 200)
- `ra1_allies_machinegunner`: HP 19000, Speed 49, Range 5610, weapon Damage 16000 (all), ReloadDelay 48, Burst 5, FirepowerMultiplier@RA1ALLIESMACHINEGUNNER 93, formula price delta +103 (informational; cost pinned at 557)
- `ra1_soviets_dragunovantimaterialsniper`: HP 20000, Speed 40, Range 5600, weapon Damage 400000 (all), ReloadDelay 85, Burst 1, FirepowerMultiplier@RA1SOVIETSDRAGUNOVANTIMATERIALSNIPER 103, formula price delta +1480 (informational; cost pinned at 422)
- `terran_ghost`: HP 44000, Speed 75, Range 6490, weapon Damage 10000 (all), ReloadDelay 22, Burst 1, FirepowerMultiplier@TERRANGHOST 103, formula price delta -456 (informational; cost pinned at 1176)
- `terran_madcap`: HP 60000, Speed 60, Range 5560, weapon Damage 36000 (all), ReloadDelay 25, Burst 1, FirepowerMultiplier@TERRANMADCAP 108, formula price delta +2023 (informational; cost pinned at 1003)
- `terran_marine`: HP 41000, Speed 61, Range 5550, weapon Damage 36000 (all), ReloadDelay 26, Burst 3, FirepowerMultiplier@TERRANMARINE 31, formula price delta +681 (informational; cost pinned at 689)
- `terran_specter`: HP 50000, Speed 80, Range 6480, weapon Damage 20000 (all), ReloadDelay 33, Burst 1, FirepowerMultiplier@TERRANSPECTER 106, formula price delta -659 (informational; cost pinned at 1744)
- `zerg_hydralisk`: HP 80000, Speed 76, Range 5500, weapon Damage 72000 (all), ReloadDelay 15, Burst 1, FirepowerMultiplier@ZERGHYDRALISK 99, formula price delta +5144 (informational; cost pinned at 3314)
- `td_gdi_officer`: HP 32000, Speed 79, Range 5590, weapon Damage 16000 (all), ReloadDelay 20, Burst 4, FirepowerMultiplier@TDGDIOFFICER 109, formula price delta +504 (informational; cost pinned at 1532)
- `td_nod_lasertrooper`: HP 59000, Speed 51, Range 5580, weapon Damage 144000 (all), ReloadDelay 50, Burst 1, FirepowerMultiplier@TDNODLASERTROOPER 108, formula price delta +1287 (informational; cost pinned at 750)
- `td_nod_stealthsoldier`: HP 25000, Speed 72, Range 5570, weapon Damage 110000 (all), ReloadDelay 90, Burst 4, FirepowerMultiplier@TDNODSTEALTHSOLDIER 93, formula price delta +2845 (informational; cost pinned at 753)
- `cabal_eliminator800`: HP 85000, Speed 39, Range 5860, weapon Damage 4000 (all), ReloadDelay 5, Burst 1, FirepowerMultiplier@CABALELIMINATOR800 105
- `forgotten_mutantsergeant`: HP 40000, Speed 74, Range 5620, weapon Damage 16000 (all), ReloadDelay 8, Burst 1, FirepowerMultiplier@FORGOTTENMUTANTSERGEANT 110, formula price delta +779 (informational; cost pinned at 1154)
- `ts_gdi_falconenforcer`: HP 45000, Speed 62, Range 5530, weapon Damage 16000 (all), ReloadDelay 26, Burst 3, FirepowerMultiplier@TSGDIFALCONENFORCER 95, formula price delta +636 (informational; cost pinned at 1322)
- `ts_nod_elitecadre`: HP 21000, Speed 55, Range 5520, weapon Damage 16000 (all), ReloadDelay 52, Burst 5, FirepowerMultiplier@TSNODELITECADRE 94, formula price delta +132 (informational; cost pinned at 435)
