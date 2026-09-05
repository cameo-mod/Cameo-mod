# Rocket Trooper infantry rebalance proposal

Anchor spec: HP=10000, Speed=55, Range=6500, eff-DPS=200, Cost=300

Converter law: cost pinned, range clamped to band + made unique, eff-DPS trimmed to Δ≤1 via 100-grid warhead Damage; unconditional FirepowerMultiplier is retired.

Uniqueness separates **raw warhead Damage (the 5-stat law as written)**. The levers run coarsest-first — Damage (100 grid) → Speed (1) → Range (10) — because one Damage step is a whole shot and running it last threw away everything the fine levers had achieved.

| actor | faction | HP | spd | rng | cost | dmg/wh×n | rl | burst | legacy FP% | eff DPS | price | Δ | flags |
|---|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|---|
| `ixian_rockettrooper` | d2k_ixian | 19000 | 48 | 6170 | 300 | 2300×3 | 64 | 1 | 100 | 107.8 | 300 | -0.1 | shared-wpn? |
| `ixian_twinrockettrooper` | d2k_ixian | 24000 | 49 | 6500 | 600 | 5700×3 | 64 | 1 | 100 | 267.2 | 600 | +0.0 |  |
| `ordos_antiairtrooper` | d2k_ordos | 16000 | 46 | 6710 | 450 | 6900×3 | 75 | 1 | 100 | 276.0 | 450 | -0.0 |  |
| `ordos_rockettrooper` | d2k_ordos | 14000 | 47 | 6120 | 300 | 3700×3 | 64 | 1 | 100 | 173.4 | 300 | -0.1 | shared-wpn? |
| `ra2_allies_guardiangi` | redalert2_allies | 45000 | 44 | 6050 | 400 | 200×1 | 20 | 3 | 100 | 25.0 | 404 | +4.3 |  |
| `yuri_initiate` | redalert2_yuri | 22000 | 60 | 6000 | 200 | 100×1 | 15 | 1 | 50 | 6.7 | 235 | +35.1 | OVERPRICED@min-dps fp-debt |
| `asianalliance_asiantankkiller` | redalert2mod_asianalliance | 27000 | 51 | 6550 | 300 | 2700×1 | 75 | 1 | 100 | 36.0 | 300 | +0.0 |  |
| `steelconsortium_clonetrooper` | redalert2mod_consortium | 12000 | 58 | 6460 | 143 | 100×3 | 25 | 1 | 91 | 12.0 | 157 | +13.5 | OVERPRICED@min-dps fp-debt |
| `futuretech_javelinsoldier` | redalert2mod_futuretech | 25000 | 53 | 6200 | 400 | 2200×1 | 25 | 1 | 100 | 88.0 | 400 | +0.5 |  |
| `futuretech_missiledroid` | redalert2mod_futuretech | 68000 | 55 | 7000 | 700 | 5100×2 | 65 | 2 | 100 | 291.4 | 700 | +0.2 |  |
| `schwarzermond_lunarrocket` | redalert2mod_schwarzermond | 15000 | 60 | 6020 | 350 | 1100×4 | 50 | 1 | 100 | 88.0 | 350 | +0.5 |  |
| `latinsyndicate_tankkiller` | redalert2mod_syndicate | 17000 | 54 | 6670 | 270 | 5000×1 | 66 | 1 | 100 | 75.8 | 270 | -0.1 |  |
| `tkm_rocketeer` | redalert2mod_tkm | 7000 | 65 | 6210 | 200 | 7400×1 | 64 | 1 | 100 | 115.6 | 200 | +0.1 |  |
| `ra1_soviets_firerocketsoldier` | redalert_soviets | 13000 | 56 | 6070 | 400 | 13700×1 | 53 | 1 | 100 | 258.5 | 400 | -0.1 |  |
| `ra1_soviets_rocketsoldier` | redalert_soviets | 9000 | 57 | 6680 | 300 | 5200×2 | 50 | 1 | 100 | 208.0 | 300 | -0.1 |  |
| `trooper` | shared_d2k | 6000 | 66 | 6180 | 300 | 5900×3 | 64 | 1 | 100 | 276.6 | 300 | -0.3 | shared-wpn? |
| `ra1_allies_alliedrocketsoldier` | shared_redalert | 10000 | 55 | 6500 | 300 | 20000×2 | 50 | 1 | 100 | 400.0 | 475 | +175.0 | anchor |
| `terran_madcap` | starcraft_terran | 60000 | 63 | 6010 | 1003 | 1000×3 | 25 | 1 | 108 | 120.0 | 1077 | +74.4 | fp-debt |
| `terran_marine` | starcraft_terran | 41000 | 64 | 6100 | 689 | 400×3 | 26 | 3 | 31 | 120.0 | 705 | +16.4 | fp-debt |
| `zerg_hydralisk` | starcraft_zerg | 80000 | 60 | 6430 | 3314 | 8400×1 | 15 | 1 | 99 | 560.0 | 3314 | +0.2 | fp-debt |
| `td_gdi_rocketsoldier` | tiberiandawn_gdi | 8000 | 59 | 6370 | 300 | 14800×1 | 63 | 1 | 100 | 234.9 | 300 | -0.1 | shared-wpn? |
| `td_nod_chemicalrocketsoldier` | tiberiandawn_nod | 18000 | 61 | 6030 | 400 | 4300×2 | 54 | 1 | 100 | 159.3 | 400 | +0.1 |  |
| `td_nod_rocketsoldier` | tiberiandawn_nod | 11000 | 62 | 6290 | 300 | 10300×1 | 63 | 1 | 100 | 163.5 | 300 | -0.0 | shared-wpn? |
| `cabal_ascended` | tiberiansun_cabal | 70000 | 45 | 6640 | 900 | 1600×2 | 60 | 2 | 100 | 94.1 | 900 | -0.3 |  |
| `cabal_rocketcyborg` | tiberiansun_cabal | 44000 | 50 | 6990 | 650 | 1000×2 | 52 | 3 | 100 | 88.2 | 644 | -5.8 |  |
| `forgotten_rocketinfantry` | tiberiansun_forgotten | 23000 | 52 | 6730 | 300 | 2900×1 | 52 | 1 | 100 | 55.8 | 300 | -0.1 | shared-wpn? |
| `ts_nod_rocketinfantry` | tiberiansun_nod | 20000 | 66 | 6660 | 300 | 2400×1 | 52 | 1 | 100 | 46.2 | 300 | -0.0 | shared-wpn? |
| `wc2_humans_elvenranger` | warcraft2_humans | 21000 | 66 | 6390 | 600 | 5700×1 | 25 | 1 | 100 | 228.0 | 600 | +0.0 | shared-wpn? |
| `wc2_orcs_kodobeast` | warcraft2_orcs | 100000 | 60 | 6260 | 1000 | 800×1 | 38 | 1 | 100 | 21.1 | 1000 | +0.0 |  |
| `wc2_orcs_trollaxethrower` | warcraft2_orcs | 29000 | 65 | 6450 | 500 | 3700×1 | 38 | 1 | 100 | 97.4 | 500 | +0.1 |  |
| `wc2_orcs_trollberserker` | warcraft2_orcs | 30000 | 66 | 6540 | 500 | 3300×1 | 38 | 1 | 100 | 86.8 | 500 | +0.1 |  |
| `wc2_orcs_trollheadhunter` | warcraft2_orcs | 40000 | 66 | 6910 | 1000 | 4100×2 | 40 | 1 | 100 | 205.0 | 1000 | -0.3 |  |

**Worst |Δ| among non-anchor members: 74.4** (goal ≤1).

## Uniqueness check (5 raw stats — soft/protected excluded)

- **Speed duplicates**: {60: ['yuri_initiate', 'schwarzermond_lunarrocket', 'zerg_hydralisk', 'wc2_orcs_kodobeast'], 65: ['tkm_rocketeer', 'wc2_orcs_trollaxethrower'], 66: ['trooper', 'ts_nod_rocketinfantry', 'wc2_humans_elvenranger', 'wc2_orcs_trollberserker', 'wc2_orcs_trollheadhunter']}
- **raw ReloadDelay duplicates** (design choice — retune coarse Damage/reload by hand): {64: ['ixian_rockettrooper', 'ixian_twinrockettrooper', 'ordos_rockettrooper', 'tkm_rocketeer', 'trooper'], 75: ['ordos_antiairtrooper', 'asianalliance_asiantankkiller'], 15: ['yuri_initiate', 'zerg_hydralisk'], 25: ['steelconsortium_clonetrooper', 'futuretech_javelinsoldier', 'terran_madcap', 'wc2_humans_elvenranger'], 50: ['schwarzermond_lunarrocket', 'ra1_soviets_rocketsoldier'], 63: ['td_gdi_rocketsoldier', 'td_nod_rocketsoldier'], 52: ['cabal_rocketcyborg', 'forgotten_rocketinfantry', 'ts_nod_rocketinfantry'], 38: ['wc2_orcs_kodobeast', 'wc2_orcs_trollaxethrower', 'wc2_orcs_trollberserker']}

## Required YAML edits (per unit)

- `ixian_rockettrooper`: HP 19000, Speed 48, Range 6170, each offensive warhead Damage 2300 (×3 = SUM 6900), ReloadDelay 64, Burst 1
- `ixian_twinrockettrooper`: HP 24000, Speed 49, Range 6500, each offensive warhead Damage 5700 (×3 = SUM 17100), ReloadDelay 64, Burst 1
- `ordos_antiairtrooper`: HP 16000, Speed 46, Range 6710, each offensive warhead Damage 6900 (×3 = SUM 20700), ReloadDelay 75, Burst 1
- `ordos_rockettrooper`: HP 14000, Speed 47, Range 6120, each offensive warhead Damage 3700 (×3 = SUM 11100), ReloadDelay 64, Burst 1
- `ra2_allies_guardiangi`: HP 45000, Speed 44, Range 6050, each offensive warhead Damage 200 (×1 = SUM 200), ReloadDelay 20, Burst 3, residual Δ +4.3 (cost pinned at 400)
- `yuri_initiate`: HP 22000, Speed 60, Range 6000, each offensive warhead Damage 100 (×1 = SUM 100), ReloadDelay 15, Burst 1, **DELETE the unconditional FirepowerMultiplier (50%)** — the Damage above already includes it (W17), residual Δ +35.1 (cost pinned at 200)
- `asianalliance_asiantankkiller`: HP 27000, Speed 51, Range 6550, each offensive warhead Damage 2700 (×1 = SUM 2700), ReloadDelay 75, Burst 1
- `steelconsortium_clonetrooper`: HP 12000, Speed 58, Range 6460, each offensive warhead Damage 100 (×3 = SUM 300), ReloadDelay 25, Burst 1, **DELETE the unconditional FirepowerMultiplier (91%)** — the Damage above already includes it (W17), residual Δ +13.5 (cost pinned at 143)
- `futuretech_javelinsoldier`: HP 25000, Speed 53, Range 6200, each offensive warhead Damage 2200 (×1 = SUM 2200), ReloadDelay 25, Burst 1
- `futuretech_missiledroid`: HP 68000, Speed 55, Range 7000, each offensive warhead Damage 5100 (×2 = SUM 10200), ReloadDelay 65, Burst 2
- `schwarzermond_lunarrocket`: HP 15000, Speed 60, Range 6020, each offensive warhead Damage 1100 (×4 = SUM 4400), ReloadDelay 50, Burst 1
- `latinsyndicate_tankkiller`: HP 17000, Speed 54, Range 6670, each offensive warhead Damage 5000 (×1 = SUM 5000), ReloadDelay 66, Burst 1
- `tkm_rocketeer`: HP 7000, Speed 65, Range 6210, each offensive warhead Damage 7400 (×1 = SUM 7400), ReloadDelay 64, Burst 1
- `ra1_soviets_firerocketsoldier`: HP 13000, Speed 56, Range 6070, each offensive warhead Damage 13700 (×1 = SUM 13700), ReloadDelay 53, Burst 1
- `ra1_soviets_rocketsoldier`: HP 9000, Speed 57, Range 6680, each offensive warhead Damage 5200 (×2 = SUM 10400), ReloadDelay 50, Burst 1
- `trooper`: HP 6000, Speed 66, Range 6180, each offensive warhead Damage 5900 (×3 = SUM 17700), ReloadDelay 64, Burst 1
- `terran_madcap`: HP 60000, Speed 63, Range 6010, each offensive warhead Damage 1000 (×3 = SUM 3000), ReloadDelay 25, Burst 1, **DELETE the unconditional FirepowerMultiplier (108%)** — the Damage above already includes it (W17), residual Δ +74.4 (cost pinned at 1003)
- `terran_marine`: HP 41000, Speed 64, Range 6100, each offensive warhead Damage 400 (×3 = SUM 1200), ReloadDelay 26, Burst 3, **DELETE the unconditional FirepowerMultiplier (31%)** — the Damage above already includes it (W17), residual Δ +16.4 (cost pinned at 689)
- `zerg_hydralisk`: HP 80000, Speed 60, Range 6430, each offensive warhead Damage 8400 (×1 = SUM 8400), ReloadDelay 15, Burst 1, **DELETE the unconditional FirepowerMultiplier (99%)** — the Damage above already includes it (W17)
- `td_gdi_rocketsoldier`: HP 8000, Speed 59, Range 6370, each offensive warhead Damage 14800 (×1 = SUM 14800), ReloadDelay 63, Burst 1
- `td_nod_chemicalrocketsoldier`: HP 18000, Speed 61, Range 6030, each offensive warhead Damage 4300 (×2 = SUM 8600), ReloadDelay 54, Burst 1
- `td_nod_rocketsoldier`: HP 11000, Speed 62, Range 6290, each offensive warhead Damage 10300 (×1 = SUM 10300), ReloadDelay 63, Burst 1
- `cabal_ascended`: HP 70000, Speed 45, Range 6640, each offensive warhead Damage 1600 (×2 = SUM 3200), ReloadDelay 60, Burst 2
- `cabal_rocketcyborg`: HP 44000, Speed 50, Range 6990, each offensive warhead Damage 1000 (×2 = SUM 2000), ReloadDelay 52, Burst 3, residual Δ -5.8 (cost pinned at 650)
- `forgotten_rocketinfantry`: HP 23000, Speed 52, Range 6730, each offensive warhead Damage 2900 (×1 = SUM 2900), ReloadDelay 52, Burst 1
- `ts_nod_rocketinfantry`: HP 20000, Speed 66, Range 6660, each offensive warhead Damage 2400 (×1 = SUM 2400), ReloadDelay 52, Burst 1
- `wc2_humans_elvenranger`: HP 21000, Speed 66, Range 6390, each offensive warhead Damage 5700 (×1 = SUM 5700), ReloadDelay 25, Burst 1
- `wc2_orcs_kodobeast`: HP 100000, Speed 60, Range 6260, each offensive warhead Damage 800 (×1 = SUM 800), ReloadDelay 38, Burst 1
- `wc2_orcs_trollaxethrower`: HP 29000, Speed 65, Range 6450, each offensive warhead Damage 3700 (×1 = SUM 3700), ReloadDelay 38, Burst 1
- `wc2_orcs_trollberserker`: HP 30000, Speed 66, Range 6540, each offensive warhead Damage 3300 (×1 = SUM 3300), ReloadDelay 38, Burst 1
- `wc2_orcs_trollheadhunter`: HP 40000, Speed 66, Range 6910, each offensive warhead Damage 4100 (×2 = SUM 8200), ReloadDelay 40, Burst 1
