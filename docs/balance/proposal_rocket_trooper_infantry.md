# Rocket Trooper infantry rebalance proposal

Anchor spec: HP=10000, Speed=55, Range=6500, eff-DPS=200, Cost=300

Converter law: cost pinned, range clamped to band + made unique, eff-DPS trimmed to Δ≤1 via 2000-grid warhead Damage × 1% FirepowerMultiplier.

| actor | faction | HP | spd | rng | cost | dmg/wh×n | rl | burst | FP% | eff DPS | price | Δ | flags |
|---|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|---|
| `ixian_rockettrooper` | d2k_ixian | 19000 | 48 | 6200 | 300 | 2000×3 | 64 | 1 | 131 | 107.5 | 300 | +0.1 | shared-wpn? |
| `ixian_twinrockettrooper` | d2k_ixian | 24000 | 49 | 6470 | 600 | 6000×3 | 64 | 1 | 109 | 268.2 | 600 | -0.2 |  |
| `ordos_antiairtrooper` | d2k_ordos | 16000 | 44 | 6970 | 450 | 8000×3 | 75 | 1 | 99 | 277.2 | 450 | +0.0 |  |
| `ordos_rockettrooper` | d2k_ordos | 14000 | 47 | 6160 | 300 | 4000×3 | 64 | 1 | 105 | 172.3 | 300 | -0.1 | shared-wpn? |
| `ra2_allies_guardiangi` | redalert2_allies | 44000 | 46 | 6070 | 400 | 2000×2 | 20 | 3 | 5 | 21.9 | 400 | +0.2 |  |
| `yuri_initiate` | redalert2_yuri | 22000 | 62 | 6010 | 200 | 2000×4 | 15 | 1 | 5 | 24.4 | 267 | +67.0 | OVERPRICED@min-dps |
| `asianalliance_asiantankkiller` | redalert2mod_asianalliance | 27000 | 51 | 6490 | 300 | 4000×1 | 75 | 1 | 91 | 36.4 | 300 | -0.0 |  |
| `steelconsortium_clonetrooper` | redalert2mod_consortium | 12000 | 57 | 6460 | 143 | 2000×3 | 25 | 1 | 5 | 12.0 | 155 | +11.8 |  |
| `futuretech_javelinsoldier` | redalert2mod_futuretech | 25000 | 52 | 6050 | 400 | 2000×5 | 25 | 1 | 25 | 93.8 | 401 | +1.3 |  |
| `futuretech_missiledroid` | redalert2mod_futuretech | 67500 | 55 | 6990 | 700 | 2000×2 | 65 | 2 | 35 | 35.0 | 699 | -1.1 |  |
| `schwarzermond_lunarrocket` | redalert2mod_schwarzermond | 15000 | 60 | 6040 | 350 | 2000×3 | 50 | 1 | 67 | 87.1 | 349 | -0.6 |  |
| `latinsyndicate_latintankkiller` | redalert2mod_syndicate | 17000 | 53 | 6680 | 270 | 6000×1 | 66 | 1 | 115 | 78.4 | 270 | +0.1 |  |
| `tkm_rocketeer` | redalert2mod_tkm | 7000 | 64 | 6190 | 200 | 10000×1 | 64 | 1 | 101 | 118.4 | 200 | +0.0 |  |
| `ra1_soviets_firerocketsoldier` | redalert_soviets | 13000 | 54 | 6060 | 400 | 4000×4 | 53 | 1 | 102 | 269.4 | 401 | +1.0 |  |
| `ra1_soviets_rocketsoldier` | redalert_soviets | 9000 | 56 | 6670 | 300 | 6000×2 | 50 | 1 | 101 | 212.1 | 300 | +0.1 |  |
| `trooper` | shared_d2k | 6000 | 65 | 6170 | 300 | 6000×3 | 64 | 1 | 114 | 280.5 | 300 | -0.2 | shared-wpn? |
| `ra1_allies_alliedrocketsoldier` | shared_redalert | 10000 | 55 | 6500 | 300 | 20000×2 | 50 | 1 | 100 | 350.0 | 431 | +131.2 | anchor |
| `terran_madcap` | starcraft_terran | 60000 | 62 | 6020 | 1003 | 2000×3 | 25 | 1 | 44 | 105.6 | 1003 | -0.5 |  |
| `terran_marine` | starcraft_terran | 41000 | 63 | 6250 | 689 | 2000×3 | 26 | 3 | 19 | 114.0 | 689 | +0.0 |  |
| `zerg_hydralisk` | starcraft_zerg | 80000 | 60 | 6000 | 3314 | 2000×4 | 15 | 1 | 150 | 600.0 | 3317 | +3.0 |  |
| `td_gdi_rocketsoldier` | tiberiandawn_gdi | 8000 | 58 | 6380 | 200 | 10000×1 | 63 | 1 | 93 | 110.7 | 200 | -0.1 | shared-wpn? |
| `td_nod_chemicalrocketsoldier` | tiberiandawn_nod | 18000 | 61 | 6030 | 400 | 4000×3 | 54 | 1 | 86 | 159.3 | 400 | +0.1 |  |
| `td_nod_rocketsoldier` | tiberiandawn_nod | 11000 | 59 | 6360 | 200 | 6000×1 | 63 | 1 | 92 | 65.7 | 200 | +0.0 | shared-wpn? |
| `cabal_ascended` | tiberiansun_cabal | 70000 | 45 | 7000 | 900 | 2000×6 | 60 | 2 | 24 | 88.9 | 899 | -0.6 |  |
| `cabal_rocketcyborg` | tiberiansun_cabal | 45000 | 40 | 6500 | 650 | 12000×2 | 52 | 3 | 100 | 463.2 | 1476 | +825.8 | verifier |
| `forgotten_rocketinfantry` | tiberiansun_forgotten | 23000 | 50 | 6710 | 300 | 4000×1 | 52 | 1 | 106 | 61.2 | 300 | +0.0 | shared-wpn? |
| `ts_nod_rocketinfantry` | tiberiansun_nod | 20000 | 66 | 6650 | 300 | 4000×1 | 52 | 1 | 80 | 46.2 | 300 | -0.2 | shared-wpn? |
| `wc2_humans_elvenranger` | warcraft2_humans | 21000 | 66 | 6900 | 600 | 2000×6 | 25 | 1 | 44 | 211.2 | 600 | -0.0 | shared-wpn? |
| `wc2_orcs_kodobeast` | warcraft2_orcs | 125000 | 65 | 6440 | 1000 | 2000×6 | 38 | 1 | 5 | 15.8 | 1263 | +262.9 | OVERPRICED@min-dps |
| `wc2_orcs_trollaxethrower` | warcraft2_orcs | 29000 | 64 | 6390 | 500 | 2000×6 | 38 | 1 | 32 | 101.1 | 500 | -0.0 |  |
| `wc2_orcs_trollberserker` | warcraft2_orcs | 30000 | 66 | 6430 | 500 | 2000×6 | 38 | 1 | 28 | 88.4 | 500 | +0.1 |  |
| `wc2_orcs_trollheadhunter` | warcraft2_orcs | 40000 | 64 | 6950 | 1000 | 2000×9 | 40 | 1 | 44 | 212.1 | 1000 | -0.3 |  |

**Worst |Δ| among non-anchor members: 262.9** (goal ≤1).

## Uniqueness check (5 raw stats — soft/protected excluded)

- **Speed duplicates**: {62: ['yuri_initiate', 'terran_madcap'], 60: ['schwarzermond_lunarrocket', 'zerg_hydralisk'], 64: ['tkm_rocketeer', 'wc2_orcs_trollaxethrower', 'wc2_orcs_trollheadhunter'], 65: ['trooper', 'wc2_orcs_kodobeast'], 66: ['ts_nod_rocketinfantry', 'wc2_humans_elvenranger', 'wc2_orcs_trollberserker']}
- **raw ReloadDelay duplicates** (design choice — retune coarse Damage/reload by hand): {64: ['ixian_rockettrooper', 'ixian_twinrockettrooper', 'ordos_rockettrooper', 'tkm_rocketeer', 'trooper'], 75: ['ordos_antiairtrooper', 'asianalliance_asiantankkiller'], 15: ['yuri_initiate', 'zerg_hydralisk'], 25: ['steelconsortium_clonetrooper', 'futuretech_javelinsoldier', 'terran_madcap', 'wc2_humans_elvenranger'], 50: ['schwarzermond_lunarrocket', 'ra1_soviets_rocketsoldier'], 63: ['td_gdi_rocketsoldier', 'td_nod_rocketsoldier'], 52: ['forgotten_rocketinfantry', 'ts_nod_rocketinfantry'], 38: ['wc2_orcs_kodobeast', 'wc2_orcs_trollaxethrower', 'wc2_orcs_trollberserker']}

## Required YAML edits (per unit)

- `ixian_rockettrooper`: HP 19000, Speed 48, Range 6200, each offensive warhead Damage 2000 (×3 = SUM 6000), ReloadDelay 64, Burst 1, FirepowerMultiplier@IXIANROCKETTROOPER 131
- `ixian_twinrockettrooper`: HP 24000, Speed 49, Range 6470, each offensive warhead Damage 6000 (×3 = SUM 18000), ReloadDelay 64, Burst 1, FirepowerMultiplier@IXIANTWINROCKETTROOPER 109
- `ordos_antiairtrooper`: HP 16000, Speed 44, Range 6970, each offensive warhead Damage 8000 (×3 = SUM 24000), ReloadDelay 75, Burst 1, FirepowerMultiplier@ORDOSANTIAIRTROOPER 99
- `ordos_rockettrooper`: HP 14000, Speed 47, Range 6160, each offensive warhead Damage 4000 (×3 = SUM 12000), ReloadDelay 64, Burst 1, FirepowerMultiplier@ORDOSROCKETTROOPER 105
- `ra2_allies_guardiangi`: HP 44000, Speed 46, Range 6070, each offensive warhead Damage 2000 (×2 = SUM 4000), ReloadDelay 20, Burst 3, FirepowerMultiplier@RA2ALLIESGUARDIANGI 5
- `yuri_initiate`: HP 22000, Speed 62, Range 6010, each offensive warhead Damage 2000 (×4 = SUM 8000), ReloadDelay 15, Burst 1, FirepowerMultiplier@YURIINITIATE 5, residual Δ +67.0 (cost pinned at 200)
- `asianalliance_asiantankkiller`: HP 27000, Speed 51, Range 6490, each offensive warhead Damage 4000 (×1 = SUM 4000), ReloadDelay 75, Burst 1, FirepowerMultiplier@ASIANALLIANCEASIANTANKKILLER 91
- `steelconsortium_clonetrooper`: HP 12000, Speed 57, Range 6460, each offensive warhead Damage 2000 (×3 = SUM 6000), ReloadDelay 25, Burst 1, FirepowerMultiplier@STEELCONSORTIUMCLONETROOPER 5, residual Δ +11.8 (cost pinned at 143)
- `futuretech_javelinsoldier`: HP 25000, Speed 52, Range 6050, each offensive warhead Damage 2000 (×5 = SUM 10000), ReloadDelay 25, Burst 1, FirepowerMultiplier@FUTURETECHJAVELINSOLDIER 25, residual Δ +1.3 (cost pinned at 400)
- `futuretech_missiledroid`: HP 67500, Speed 55, Range 6990, each offensive warhead Damage 2000 (×2 = SUM 4000), ReloadDelay 65, Burst 2, FirepowerMultiplier@FUTURETECHMISSILEDROID 35, residual Δ -1.1 (cost pinned at 700)
- `schwarzermond_lunarrocket`: HP 15000, Speed 60, Range 6040, each offensive warhead Damage 2000 (×3 = SUM 6000), ReloadDelay 50, Burst 1, FirepowerMultiplier@SCHWARZERMONDLUNARROCKET 67
- `latinsyndicate_latintankkiller`: HP 17000, Speed 53, Range 6680, each offensive warhead Damage 6000 (×1 = SUM 6000), ReloadDelay 66, Burst 1, FirepowerMultiplier@LATINSYNDICATELATINTANKKILLER 115
- `tkm_rocketeer`: HP 7000, Speed 64, Range 6190, each offensive warhead Damage 10000 (×1 = SUM 10000), ReloadDelay 64, Burst 1, FirepowerMultiplier@TKMROCKETEER 101
- `ra1_soviets_firerocketsoldier`: HP 13000, Speed 54, Range 6060, each offensive warhead Damage 4000 (×4 = SUM 16000), ReloadDelay 53, Burst 1, FirepowerMultiplier@RA1SOVIETSFIREROCKETSOLDIER 102
- `ra1_soviets_rocketsoldier`: HP 9000, Speed 56, Range 6670, each offensive warhead Damage 6000 (×2 = SUM 12000), ReloadDelay 50, Burst 1, FirepowerMultiplier@RA1SOVIETSROCKETSOLDIER 101
- `trooper`: HP 6000, Speed 65, Range 6170, each offensive warhead Damage 6000 (×3 = SUM 18000), ReloadDelay 64, Burst 1, FirepowerMultiplier@TROOPER 114
- `terran_madcap`: HP 60000, Speed 62, Range 6020, each offensive warhead Damage 2000 (×3 = SUM 6000), ReloadDelay 25, Burst 1, FirepowerMultiplier@TERRANMADCAP 44
- `terran_marine`: HP 41000, Speed 63, Range 6250, each offensive warhead Damage 2000 (×3 = SUM 6000), ReloadDelay 26, Burst 3, FirepowerMultiplier@TERRANMARINE 19
- `zerg_hydralisk`: HP 80000, Speed 60, Range 6000, each offensive warhead Damage 2000 (×4 = SUM 8000), ReloadDelay 15, Burst 1, FirepowerMultiplier@ZERGHYDRALISK 150, residual Δ +3.0 (cost pinned at 3314)
- `td_gdi_rocketsoldier`: HP 8000, Speed 58, Range 6380, each offensive warhead Damage 10000 (×1 = SUM 10000), ReloadDelay 63, Burst 1, FirepowerMultiplier@TDGDIROCKETSOLDIER 93
- `td_nod_chemicalrocketsoldier`: HP 18000, Speed 61, Range 6030, each offensive warhead Damage 4000 (×3 = SUM 12000), ReloadDelay 54, Burst 1, FirepowerMultiplier@TDNODCHEMICALROCKETSOLDIER 86
- `td_nod_rocketsoldier`: HP 11000, Speed 59, Range 6360, each offensive warhead Damage 6000 (×1 = SUM 6000), ReloadDelay 63, Burst 1, FirepowerMultiplier@TDNODROCKETSOLDIER 92
- `cabal_ascended`: HP 70000, Speed 45, Range 7000, each offensive warhead Damage 2000 (×6 = SUM 12000), ReloadDelay 60, Burst 2, FirepowerMultiplier@CABALASCENDED 24
- `forgotten_rocketinfantry`: HP 23000, Speed 50, Range 6710, each offensive warhead Damage 4000 (×1 = SUM 4000), ReloadDelay 52, Burst 1, FirepowerMultiplier@FORGOTTENROCKETINFANTRY 106
- `ts_nod_rocketinfantry`: HP 20000, Speed 66, Range 6650, each offensive warhead Damage 4000 (×1 = SUM 4000), ReloadDelay 52, Burst 1, FirepowerMultiplier@TSNODROCKETINFANTRY 80
- `wc2_humans_elvenranger`: HP 21000, Speed 66, Range 6900, each offensive warhead Damage 2000 (×6 = SUM 12000), ReloadDelay 25, Burst 1, FirepowerMultiplier@WC2HUMANSELVENRANGER 44
- `wc2_orcs_kodobeast`: HP 125000, Speed 65, Range 6440, each offensive warhead Damage 2000 (×6 = SUM 12000), ReloadDelay 38, Burst 1, FirepowerMultiplier@WC2ORCSKODOBEAST 5, residual Δ +262.9 (cost pinned at 1000)
- `wc2_orcs_trollaxethrower`: HP 29000, Speed 64, Range 6390, each offensive warhead Damage 2000 (×6 = SUM 12000), ReloadDelay 38, Burst 1, FirepowerMultiplier@WC2ORCSTROLLAXETHROWER 32
- `wc2_orcs_trollberserker`: HP 30000, Speed 66, Range 6430, each offensive warhead Damage 2000 (×6 = SUM 12000), ReloadDelay 38, Burst 1, FirepowerMultiplier@WC2ORCSTROLLBERSERKER 28
- `wc2_orcs_trollheadhunter`: HP 40000, Speed 64, Range 6950, each offensive warhead Damage 2000 (×9 = SUM 18000), ReloadDelay 40, Burst 1, FirepowerMultiplier@WC2ORCSTROLLHEADHUNTER 44
