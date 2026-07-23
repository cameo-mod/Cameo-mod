# Rocket Trooper infantry rebalance proposal

Anchor spec: HP=10000, Speed=55, Range=6500, eff-DPS=200, Cost=300

Converter law: cost pinned, range clamped to band + made unique, eff-DPS trimmed to Δ≤1 via 2000-grid warhead Damage × 1% FirepowerMultiplier.

| actor | faction | HP | spd | rng | cost | dmg/wh×n | rl | burst | FP% | eff DPS | price | Δ | flags |
|---|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|---|
| `ixian_rockettrooper` | d2k_ixian | 22000 | 48 | 6230 | 300 | 2000×3 | 64 | 1 | 98 | 80.4 | 300 | +0.1 | shared-wpn? |
| `ixian_twinrockettrooper` | d2k_ixian | 23000 | 49 | 6510 | 600 | 6000×3 | 64 | 1 | 114 | 280.5 | 600 | +0.5 |  |
| `ordos_antiairtrooper` | d2k_ordos | 15000 | 44 | 6980 | 450 | 8000×3 | 75 | 1 | 105 | 294.0 | 449 | -0.5 |  |
| `ordos_rockettrooper` | d2k_ordos | 12000 | 47 | 6150 | 300 | 4000×3 | 64 | 1 | 125 | 205.1 | 300 | -0.1 | shared-wpn? |
| `ra2_allies_guardiangi` | redalert2_allies | 44000 | 46 | 6050 | 400 | 2000×2 | 20 | 3 | 5 | 21.9 | 400 | -0.1 |  |
| `yuri_initiate` | redalert2_yuri | 25000 | 60 | 6000 | 200 | 2000×4 | 15 | 1 | 5 | 24.4 | 289 | +88.7 | OVERPRICED@min-dps |
| `asianalliance_asiantankkiller` | redalert2mod_asianalliance | 31000 | 51 | 6470 | 300 | 2000×1 | 75 | 1 | 85 | 17.0 | 300 | -0.0 |  |
| `asianalliance_veteranarcher` | redalert2mod_asianalliance | 30000 | 66 | 6580 | 450 | 2000×6 | 68 | 3 | 12 | 63.5 | 450 | +0.1 |  |
| `steelconsortium_clonetrooper` | redalert2mod_consortium | 14000 | 57 | 6460 | 143 | 2000×3 | 25 | 1 | 5 | 12.0 | 171 | +28.4 | OVERPRICED@min-dps |
| `futuretech_javelinsoldier` | redalert2mod_futuretech | 24000 | 52 | 6080 | 400 | 2000×5 | 25 | 1 | 27 | 101.2 | 403 | +2.9 |  |
| `futuretech_missiledroid` | redalert2mod_futuretech | 67500 | 55 | 6990 | 700 | 2000×2 | 65 | 2 | 35 | 35.0 | 699 | -1.1 |  |
| `schwarzermond_lunarrocket` | redalert2mod_schwarzermond | 13000 | 60 | 6040 | 350 | 2000×3 | 50 | 1 | 82 | 106.6 | 351 | +0.9 |  |
| `latinsyndicate_latintankkiller` | redalert2mod_syndicate | 17000 | 53 | 6680 | 270 | 6000×1 | 66 | 1 | 115 | 78.4 | 270 | +0.1 |  |
| `tkm_rocketeer` | redalert2mod_tkm | 7000 | 64 | 6190 | 200 | 10000×1 | 64 | 1 | 101 | 118.4 | 200 | +0.0 |  |
| `ra1_soviets_firerocketsoldier` | redalert_soviets | 11000 | 54 | 6070 | 400 | 4000×4 | 53 | 1 | 119 | 314.3 | 401 | +0.6 |  |
| `ra1_soviets_rocketsoldier` | redalert_soviets | 9000 | 56 | 6660 | 300 | 6000×2 | 50 | 1 | 101 | 212.1 | 300 | -0.2 |  |
| `trooper` | shared_d2k | 6000 | 65 | 6180 | 300 | 6000×3 | 64 | 1 | 115 | 283.0 | 302 | +1.8 | shared-wpn? |
| `ra1_allies_alliedrocketsoldier` | shared_redalert | 10000 | 55 | 6500 | 300 | 20000×2 | 50 | 1 | 100 | 350.0 | 431 | +131.2 | anchor |
| `terran_madcap` | starcraft_terran | 60000 | 62 | 6020 | 1003 | 2000×3 | 25 | 1 | 44 | 105.6 | 1003 | -0.5 |  |
| `terran_marine` | starcraft_terran | 41000 | 63 | 6250 | 689 | 2000×3 | 26 | 3 | 19 | 114.0 | 689 | +0.0 |  |
| `zerg_hydralisk` | starcraft_zerg | 80000 | 65 | 6030 | 3314 | 2000×4 | 15 | 1 | 137 | 548.0 | 3324 | +10.3 |  |
| `td_gdi_rocketsoldier` | tiberiandawn_gdi | 8000 | 58 | 6380 | 200 | 10000×1 | 63 | 1 | 93 | 110.7 | 200 | -0.1 | shared-wpn? |
| `td_nod_chemicalrocketsoldier` | tiberiandawn_nod | 16000 | 61 | 6010 | 400 | 4000×3 | 54 | 1 | 102 | 188.9 | 402 | +1.8 |  |
| `td_nod_rocketsoldier` | tiberiandawn_nod | 18000 | 59 | 6360 | 200 | 2000×1 | 63 | 1 | 23 | 5.5 | 200 | +0.0 | shared-wpn? |
| `cabal_ascended` | tiberiansun_cabal | 70000 | 45 | 7000 | 900 | 2000×6 | 60 | 2 | 24 | 88.9 | 899 | -0.6 |  |
| `cabal_rocketcyborg` | tiberiansun_cabal | 45000 | 40 | 6500 | 650 | 12000×2 | 52 | 3 | 100 | 463.2 | 1476 | +825.8 | verifier |
| `forgotten_rocketinfantry` | tiberiansun_forgotten | 26000 | 50 | 6710 | 300 | 2000×1 | 52 | 1 | 148 | 42.7 | 300 | +0.2 | shared-wpn? |
| `ts_nod_rocketinfantry` | tiberiansun_nod | 19000 | 66 | 6690 | 300 | 4000×1 | 52 | 1 | 92 | 53.1 | 300 | -0.2 | shared-wpn? |
| `wc2_humans_elvenarcher` | warcraft2_humans | 20000 | 66 | 6970 | 600 | 2000×6 | 25 | 1 | 46 | 220.8 | 598 | -2.3 | shared-wpn? |
| `wc2_humans_elvenranger` | warcraft2_humans | 21000 | 64 | 6960 | 600 | 2000×6 | 25 | 1 | 45 | 216.0 | 598 | -2.0 | shared-wpn? |
| `wc2_humans_highelvenarcher` | warcraft2_humans | 35000 | 65 | 6950 | 1100 | 2000×9 | 35 | 1 | 52 | 286.5 | 1097 | -3.4 | shared-wpn? |
| `wc2_orcs_kodobeast` | warcraft2_orcs | 125000 | 65 | 6440 | 1000 | 2000×6 | 38 | 1 | 5 | 15.8 | 1263 | +262.9 | OVERPRICED@min-dps |
| `wc2_orcs_trollaxethrower` | warcraft2_orcs | 27000 | 64 | 6490 | 500 | 2000×6 | 38 | 1 | 36 | 113.7 | 500 | -0.5 |  |
| `wc2_orcs_trollberserker` | warcraft2_orcs | 28000 | 62 | 6410 | 500 | 2000×6 | 38 | 1 | 37 | 116.8 | 506 | +5.9 |  |
| `wc2_orcs_trollheadhunter` | warcraft2_orcs | 40000 | 62 | 6930 | 1000 | 2000×9 | 40 | 1 | 46 | 221.8 | 1000 | +0.2 |  |

**Worst |Δ| among non-anchor members: 262.9** (goal ≤1).

## Uniqueness check (5 raw stats — soft/protected excluded)

- **Speed duplicates**: {60: ['yuri_initiate', 'schwarzermond_lunarrocket'], 66: ['asianalliance_veteranarcher', 'ts_nod_rocketinfantry', 'wc2_humans_elvenarcher'], 64: ['tkm_rocketeer', 'wc2_humans_elvenranger', 'wc2_orcs_trollaxethrower'], 65: ['trooper', 'zerg_hydralisk', 'wc2_humans_highelvenarcher', 'wc2_orcs_kodobeast'], 62: ['terran_madcap', 'wc2_orcs_trollberserker', 'wc2_orcs_trollheadhunter']}
- **raw ReloadDelay duplicates** (design choice — retune coarse Damage/reload by hand): {64: ['ixian_rockettrooper', 'ixian_twinrockettrooper', 'ordos_rockettrooper', 'tkm_rocketeer', 'trooper'], 75: ['ordos_antiairtrooper', 'asianalliance_asiantankkiller'], 15: ['yuri_initiate', 'zerg_hydralisk'], 25: ['steelconsortium_clonetrooper', 'futuretech_javelinsoldier', 'terran_madcap', 'wc2_humans_elvenarcher', 'wc2_humans_elvenranger'], 50: ['schwarzermond_lunarrocket', 'ra1_soviets_rocketsoldier'], 63: ['td_gdi_rocketsoldier', 'td_nod_rocketsoldier'], 52: ['forgotten_rocketinfantry', 'ts_nod_rocketinfantry'], 38: ['wc2_orcs_kodobeast', 'wc2_orcs_trollaxethrower', 'wc2_orcs_trollberserker']}

## Required YAML edits (per unit)

- `ixian_rockettrooper`: HP 22000, Speed 48, Range 6230, each offensive warhead Damage 2000 (×3 = SUM 6000), ReloadDelay 64, Burst 1, FirepowerMultiplier@IXIANROCKETTROOPER 98
- `ixian_twinrockettrooper`: HP 23000, Speed 49, Range 6510, each offensive warhead Damage 6000 (×3 = SUM 18000), ReloadDelay 64, Burst 1, FirepowerMultiplier@IXIANTWINROCKETTROOPER 114
- `ordos_antiairtrooper`: HP 15000, Speed 44, Range 6980, each offensive warhead Damage 8000 (×3 = SUM 24000), ReloadDelay 75, Burst 1, FirepowerMultiplier@ORDOSANTIAIRTROOPER 105
- `ordos_rockettrooper`: HP 12000, Speed 47, Range 6150, each offensive warhead Damage 4000 (×3 = SUM 12000), ReloadDelay 64, Burst 1, FirepowerMultiplier@ORDOSROCKETTROOPER 125
- `ra2_allies_guardiangi`: HP 44000, Speed 46, Range 6050, each offensive warhead Damage 2000 (×2 = SUM 4000), ReloadDelay 20, Burst 3, FirepowerMultiplier@RA2ALLIESGUARDIANGI 5
- `yuri_initiate`: HP 25000, Speed 60, Range 6000, each offensive warhead Damage 2000 (×4 = SUM 8000), ReloadDelay 15, Burst 1, FirepowerMultiplier@YURIINITIATE 5, residual Δ +88.7 (cost pinned at 200)
- `asianalliance_asiantankkiller`: HP 31000, Speed 51, Range 6470, each offensive warhead Damage 2000 (×1 = SUM 2000), ReloadDelay 75, Burst 1, FirepowerMultiplier@ASIANALLIANCEASIANTANKKILLER 85
- `asianalliance_veteranarcher`: HP 30000, Speed 66, Range 6580, each offensive warhead Damage 2000 (×6 = SUM 12000), ReloadDelay 68, Burst 3, FirepowerMultiplier@ASIANALLIANCEVETERANARCHER 12
- `steelconsortium_clonetrooper`: HP 14000, Speed 57, Range 6460, each offensive warhead Damage 2000 (×3 = SUM 6000), ReloadDelay 25, Burst 1, FirepowerMultiplier@STEELCONSORTIUMCLONETROOPER 5, residual Δ +28.4 (cost pinned at 143)
- `futuretech_javelinsoldier`: HP 24000, Speed 52, Range 6080, each offensive warhead Damage 2000 (×5 = SUM 10000), ReloadDelay 25, Burst 1, FirepowerMultiplier@FUTURETECHJAVELINSOLDIER 27, residual Δ +2.9 (cost pinned at 400)
- `futuretech_missiledroid`: HP 67500, Speed 55, Range 6990, each offensive warhead Damage 2000 (×2 = SUM 4000), ReloadDelay 65, Burst 2, FirepowerMultiplier@FUTURETECHMISSILEDROID 35, residual Δ -1.1 (cost pinned at 700)
- `schwarzermond_lunarrocket`: HP 13000, Speed 60, Range 6040, each offensive warhead Damage 2000 (×3 = SUM 6000), ReloadDelay 50, Burst 1, FirepowerMultiplier@SCHWARZERMONDLUNARROCKET 82
- `latinsyndicate_latintankkiller`: HP 17000, Speed 53, Range 6680, each offensive warhead Damage 6000 (×1 = SUM 6000), ReloadDelay 66, Burst 1, FirepowerMultiplier@LATINSYNDICATELATINTANKKILLER 115
- `tkm_rocketeer`: HP 7000, Speed 64, Range 6190, each offensive warhead Damage 10000 (×1 = SUM 10000), ReloadDelay 64, Burst 1, FirepowerMultiplier@TKMROCKETEER 101
- `ra1_soviets_firerocketsoldier`: HP 11000, Speed 54, Range 6070, each offensive warhead Damage 4000 (×4 = SUM 16000), ReloadDelay 53, Burst 1, FirepowerMultiplier@RA1SOVIETSFIREROCKETSOLDIER 119
- `ra1_soviets_rocketsoldier`: HP 9000, Speed 56, Range 6660, each offensive warhead Damage 6000 (×2 = SUM 12000), ReloadDelay 50, Burst 1, FirepowerMultiplier@RA1SOVIETSROCKETSOLDIER 101
- `trooper`: HP 6000, Speed 65, Range 6180, each offensive warhead Damage 6000 (×3 = SUM 18000), ReloadDelay 64, Burst 1, FirepowerMultiplier@TROOPER 115, residual Δ +1.8 (cost pinned at 300)
- `terran_madcap`: HP 60000, Speed 62, Range 6020, each offensive warhead Damage 2000 (×3 = SUM 6000), ReloadDelay 25, Burst 1, FirepowerMultiplier@TERRANMADCAP 44
- `terran_marine`: HP 41000, Speed 63, Range 6250, each offensive warhead Damage 2000 (×3 = SUM 6000), ReloadDelay 26, Burst 3, FirepowerMultiplier@TERRANMARINE 19
- `zerg_hydralisk`: HP 80000, Speed 65, Range 6030, each offensive warhead Damage 2000 (×4 = SUM 8000), ReloadDelay 15, Burst 1, FirepowerMultiplier@ZERGHYDRALISK 137, residual Δ +10.3 (cost pinned at 3314)
- `td_gdi_rocketsoldier`: HP 8000, Speed 58, Range 6380, each offensive warhead Damage 10000 (×1 = SUM 10000), ReloadDelay 63, Burst 1, FirepowerMultiplier@TDGDIROCKETSOLDIER 93
- `td_nod_chemicalrocketsoldier`: HP 16000, Speed 61, Range 6010, each offensive warhead Damage 4000 (×3 = SUM 12000), ReloadDelay 54, Burst 1, FirepowerMultiplier@TDNODCHEMICALROCKETSOLDIER 102, residual Δ +1.8 (cost pinned at 400)
- `td_nod_rocketsoldier`: HP 18000, Speed 59, Range 6360, each offensive warhead Damage 2000 (×1 = SUM 2000), ReloadDelay 63, Burst 1, FirepowerMultiplier@TDNODROCKETSOLDIER 23
- `cabal_ascended`: HP 70000, Speed 45, Range 7000, each offensive warhead Damage 2000 (×6 = SUM 12000), ReloadDelay 60, Burst 2, FirepowerMultiplier@CABALASCENDED 24
- `forgotten_rocketinfantry`: HP 26000, Speed 50, Range 6710, each offensive warhead Damage 2000 (×1 = SUM 2000), ReloadDelay 52, Burst 1, FirepowerMultiplier@FORGOTTENROCKETINFANTRY 148
- `ts_nod_rocketinfantry`: HP 19000, Speed 66, Range 6690, each offensive warhead Damage 4000 (×1 = SUM 4000), ReloadDelay 52, Burst 1, FirepowerMultiplier@TSNODROCKETINFANTRY 92
- `wc2_humans_elvenarcher`: HP 20000, Speed 66, Range 6970, each offensive warhead Damage 2000 (×6 = SUM 12000), ReloadDelay 25, Burst 1, FirepowerMultiplier@WC2HUMANSELVENARCHER 46, residual Δ -2.3 (cost pinned at 600)
- `wc2_humans_elvenranger`: HP 21000, Speed 64, Range 6960, each offensive warhead Damage 2000 (×6 = SUM 12000), ReloadDelay 25, Burst 1, FirepowerMultiplier@WC2HUMANSELVENRANGER 45, residual Δ -2.0 (cost pinned at 600)
- `wc2_humans_highelvenarcher`: HP 35000, Speed 65, Range 6950, each offensive warhead Damage 2000 (×9 = SUM 18000), ReloadDelay 35, Burst 1, FirepowerMultiplier@WC2HUMANSHIGHELVENARCHER 52, residual Δ -3.4 (cost pinned at 1100)
- `wc2_orcs_kodobeast`: HP 125000, Speed 65, Range 6440, each offensive warhead Damage 2000 (×6 = SUM 12000), ReloadDelay 38, Burst 1, FirepowerMultiplier@WC2ORCSKODOBEAST 5, residual Δ +262.9 (cost pinned at 1000)
- `wc2_orcs_trollaxethrower`: HP 27000, Speed 64, Range 6490, each offensive warhead Damage 2000 (×6 = SUM 12000), ReloadDelay 38, Burst 1, FirepowerMultiplier@WC2ORCSTROLLAXETHROWER 36
- `wc2_orcs_trollberserker`: HP 28000, Speed 62, Range 6410, each offensive warhead Damage 2000 (×6 = SUM 12000), ReloadDelay 38, Burst 1, FirepowerMultiplier@WC2ORCSTROLLBERSERKER 37, residual Δ +5.9 (cost pinned at 500)
- `wc2_orcs_trollheadhunter`: HP 40000, Speed 62, Range 6930, each offensive warhead Damage 2000 (×9 = SUM 18000), ReloadDelay 40, Burst 1, FirepowerMultiplier@WC2ORCSTROLLHEADHUNTER 46
