# Scout Vehicle infantry rebalance proposal

Anchor spec: HP=30000, Speed=200, Range=4500, eff-DPS=450, Cost=300

Converter law: cost pinned, range clamped to band + made unique, eff-DPS trimmed to Δ≤1 via 100-grid warhead Damage; unconditional FirepowerMultiplier is retired.

Uniqueness separates **raw warhead Damage (the 5-stat law as written)**. The levers run coarsest-first — Damage (100 grid) → Speed (1) → Range (10) — because one Damage step is a whole shot and running it last threw away everything the fine levers had achieved.

| actor | faction | HP | spd | rng | cost | dmg/wh×n | rl | burst | legacy FP% | eff DPS | price | Δ | flags |
|---|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|---|
| `ordos_raider` | d2k_ordos | 59000 | 190 | 4540 | 1200 | 100×1 | 0 | 1 | 100 | 0.0 | 192 | -1008.4 |  |
| `ordos_stealthraider` | d2k_ordos | 60000 | 195 | 5400 | 1300 | 200×1 | 0 | 1 | 100 | 0.0 | 209 | -1090.6 |  |
| `ra2_allies_ifv` | redalert2_allies | 31000 | 210 | 4500 | 500 | 300×1 | 0 | 1 | 100 | 0.0 | 113 | -386.5 |  |
| `ra2_allies_ifv_chrono` | redalert2_allies | 30000 | 215 | 4470 | 500 | 400×1 | 0 | 1 | 100 | 0.0 | 69 | -430.8 |  |
| `ra2_allies_ifv_hmg` | redalert2_allies | 29000 | 220 | 4480 | 500 | 500×1 | 0 | 1 | 100 | 0.0 | 101 | -398.6 |  |
| `ra2_allies_ifv_mg` | redalert2_allies | 28000 | 225 | 4490 | 500 | 600×1 | 0 | 1 | 100 | 0.0 | 129 | -371.1 |  |
| `ra2_allies_ifv_missile` | redalert2_allies | 27000 | 230 | 4460 | 500 | 700×1 | 0 | 1 | 100 | 0.0 | 128 | -372.2 |  |
| `ra2_allies_ifv_repair` | redalert2_allies | 26000 | 235 | 4510 | 500 | 800×1 | 0 | 1 | 100 | 0.0 | 91 | -409.3 |  |
| `ra2_soviets_terrordrone` | redalert2_soviets | 10000 | 240 | 4000 | 600 | 900×1 | 40 | 1 | 100 | 0.0 | 75 | -524.9 |  |
| `futuretech_salamanderifv` | redalert2mod_futuretech | 50000 | 170 | 4560 | 950 | 1000×1 | 0 | 1 | 150 | 0.0 | 159 | -790.9 | fp-debt |
| `tkm_as42` | redalert2mod_tkm | 19000 | 230 | 4530 | 400 | 1100×1 | 0 | 1 | 100 | 0.0 | 96 | -304.3 |  |
| `tkm_technical` | redalert2mod_tkm | 18000 | 235 | 4120 | 400 | 6800×1 | 6 | 1 | 100 | 1133.3 | 400 | -0.3 |  |
| `ra1_allies_ranger` | redalert_allies | 34000 | 205 | 4400 | 300 | 3000×1 | 10 | 4 | 100 | 480.0 | 300 | +0.1 |  |
| `japan_grenadebuggy` | redalert_japan | 58000 | 175 | 5180 | 900 | 69800×1 | 60 | 1 | 100 | 1163.3 | 900 | +0.4 |  |
| `japan_scoutcar` | redalert_japan | 32000 | 185 | 4940 | 300 | 2900×1 | 15 | 5 | 100 | 414.3 | 300 | -0.2 |  |
| `protoss_positron` | starcraft_protoss | 61000 | 200 | 5050 | 1200 | 22000×1 | 30 | 1 | 50 | 733.3 | 1201 | +0.9 | fp-debt |
| `td_gdi_humvee` | tiberiandawn_gdi | 24000 | 240 | 4800 | 400 | 4800×1 | 9 | 3 | 100 | 685.7 | 400 | +0.1 |  |
| `td_gdi_humveemkii` | tiberiandawn_gdi | 38000 | 240 | 5350 | 600 | 9000×1 | 30 | 4 | 50 | 857.1 | 600 | -0.2 | fp-debt |
| `td_nod_buggy` | tiberiandawn_nod | 20000 | 200 | 4500 | 300 | 4000×1 | 10 | 3 | 100 | 600.0 | 289 | -11.1 | anchor |
| `td_nod_buggymkii` | tiberiandawn_nod | 22000 | 235 | 5270 | 500 | 22700×1 | 60 | 3 | 50 | 1001.5 | 500 | +0.3 | fp-debt |
| `forgotten_bowler` | tiberiansun_forgotten | 40000 | 160 | 5060 | 850 | 8000×1 | 5 | 1 | 100 | 1600.0 | 850 | +0.0 |  |
| `forgotten_raidercar` | tiberiansun_forgotten | 33000 | 180 | 4740 | 300 | 2800×1 | 20 | 4 | 100 | 430.8 | 300 | +0.0 |  |
| `forgotten_ruiner` | tiberiansun_forgotten | 35000 | 165 | 5390 | 500 | 20900×2 | 50 | 1 | 100 | 836.0 | 500 | -0.3 |  |
| `ts_gdi_pitbull` | tiberiansun_gdi | 21000 | 225 | 5360 | 400 | 21600×1 | 55 | 2 | 100 | 720.0 | 400 | +0.1 |  |
| `ts_nod_attackbuggy` | tiberiansun_nod | 23000 | 230 | 4820 | 450 | 5600×1 | 20 | 4 | 100 | 861.5 | 450 | -0.0 |  |

**Worst |Δ| among non-anchor members: 1090.6** (goal ≤1).

## Uniqueness check (5 raw stats — soft/protected excluded)

- **Speed duplicates**: {225: ['ra2_allies_ifv_mg', 'ts_gdi_pitbull'], 230: ['ra2_allies_ifv_missile', 'tkm_as42', 'ts_nod_attackbuggy'], 235: ['ra2_allies_ifv_repair', 'tkm_technical', 'td_nod_buggymkii'], 240: ['ra2_soviets_terrordrone', 'td_gdi_humvee', 'td_gdi_humveemkii']}
- **raw ReloadDelay duplicates** (design choice — retune coarse Damage/reload by hand): {0: ['ordos_raider', 'ordos_stealthraider', 'ra2_allies_ifv', 'ra2_allies_ifv_chrono', 'ra2_allies_ifv_hmg', 'ra2_allies_ifv_mg', 'ra2_allies_ifv_missile', 'ra2_allies_ifv_repair', 'futuretech_salamanderifv', 'tkm_as42'], 60: ['japan_grenadebuggy', 'td_nod_buggymkii'], 30: ['protoss_positron', 'td_gdi_humveemkii'], 20: ['forgotten_raidercar', 'ts_nod_attackbuggy']}

## Required YAML edits (per unit)

- `ordos_raider`: HP 59000, Speed 190, Range 4540, each offensive warhead Damage 100 (×1 = SUM 100), ReloadDelay 0, Burst 1, residual Δ -1008.4 (cost pinned at 1200)
- `ordos_stealthraider`: HP 60000, Speed 195, Range 5400, each offensive warhead Damage 200 (×1 = SUM 200), ReloadDelay 0, Burst 1, residual Δ -1090.6 (cost pinned at 1300)
- `ra2_allies_ifv`: HP 31000, Speed 210, Range 4500, each offensive warhead Damage 300 (×1 = SUM 300), ReloadDelay 0, Burst 1, residual Δ -386.5 (cost pinned at 500)
- `ra2_allies_ifv_chrono`: HP 30000, Speed 215, Range 4470, each offensive warhead Damage 400 (×1 = SUM 400), ReloadDelay 0, Burst 1, residual Δ -430.8 (cost pinned at 500)
- `ra2_allies_ifv_hmg`: HP 29000, Speed 220, Range 4480, each offensive warhead Damage 500 (×1 = SUM 500), ReloadDelay 0, Burst 1, residual Δ -398.6 (cost pinned at 500)
- `ra2_allies_ifv_mg`: HP 28000, Speed 225, Range 4490, each offensive warhead Damage 600 (×1 = SUM 600), ReloadDelay 0, Burst 1, residual Δ -371.1 (cost pinned at 500)
- `ra2_allies_ifv_missile`: HP 27000, Speed 230, Range 4460, each offensive warhead Damage 700 (×1 = SUM 700), ReloadDelay 0, Burst 1, residual Δ -372.2 (cost pinned at 500)
- `ra2_allies_ifv_repair`: HP 26000, Speed 235, Range 4510, each offensive warhead Damage 800 (×1 = SUM 800), ReloadDelay 0, Burst 1, residual Δ -409.3 (cost pinned at 500)
- `ra2_soviets_terrordrone`: HP 10000, Speed 240, Range 4000, each offensive warhead Damage 900 (×1 = SUM 900), ReloadDelay 40, Burst 1, residual Δ -524.9 (cost pinned at 600)
- `futuretech_salamanderifv`: HP 50000, Speed 170, Range 4560, each offensive warhead Damage 1000 (×1 = SUM 1000), ReloadDelay 0, Burst 1, **DELETE the unconditional FirepowerMultiplier (150%)** — the Damage above already includes it (W17), residual Δ -790.9 (cost pinned at 950)
- `tkm_as42`: HP 19000, Speed 230, Range 4530, each offensive warhead Damage 1100 (×1 = SUM 1100), ReloadDelay 0, Burst 1, residual Δ -304.3 (cost pinned at 400)
- `tkm_technical`: HP 18000, Speed 235, Range 4120, each offensive warhead Damage 6800 (×1 = SUM 6800), ReloadDelay 6, Burst 1
- `ra1_allies_ranger`: HP 34000, Speed 205, Range 4400, each offensive warhead Damage 3000 (×1 = SUM 3000), ReloadDelay 10, Burst 4
- `japan_grenadebuggy`: HP 58000, Speed 175, Range 5180, each offensive warhead Damage 69800 (×1 = SUM 69800), ReloadDelay 60, Burst 1
- `japan_scoutcar`: HP 32000, Speed 185, Range 4940, each offensive warhead Damage 2900 (×1 = SUM 2900), ReloadDelay 15, Burst 5
- `protoss_positron`: HP 61000, Speed 200, Range 5050, each offensive warhead Damage 22000 (×1 = SUM 22000), ReloadDelay 30, Burst 1, **DELETE the unconditional FirepowerMultiplier (50%)** — the Damage above already includes it (W17)
- `td_gdi_humvee`: HP 24000, Speed 240, Range 4800, each offensive warhead Damage 4800 (×1 = SUM 4800), ReloadDelay 9, Burst 3
- `td_gdi_humveemkii`: HP 38000, Speed 240, Range 5350, each offensive warhead Damage 9000 (×1 = SUM 9000), ReloadDelay 30, Burst 4, **DELETE the unconditional FirepowerMultiplier (50%)** — the Damage above already includes it (W17)
- `td_nod_buggymkii`: HP 22000, Speed 235, Range 5270, each offensive warhead Damage 22700 (×1 = SUM 22700), ReloadDelay 60, Burst 3, **DELETE the unconditional FirepowerMultiplier (50%)** — the Damage above already includes it (W17)
- `forgotten_bowler`: HP 40000, Speed 160, Range 5060, each offensive warhead Damage 8000 (×1 = SUM 8000), ReloadDelay 5, Burst 1
- `forgotten_raidercar`: HP 33000, Speed 180, Range 4740, each offensive warhead Damage 2800 (×1 = SUM 2800), ReloadDelay 20, Burst 4
- `forgotten_ruiner`: HP 35000, Speed 165, Range 5390, each offensive warhead Damage 20900 (×2 = SUM 41800), ReloadDelay 50, Burst 1
- `ts_gdi_pitbull`: HP 21000, Speed 225, Range 5360, each offensive warhead Damage 21600 (×1 = SUM 21600), ReloadDelay 55, Burst 2
- `ts_nod_attackbuggy`: HP 23000, Speed 230, Range 4820, each offensive warhead Damage 5600 (×1 = SUM 5600), ReloadDelay 20, Burst 4
