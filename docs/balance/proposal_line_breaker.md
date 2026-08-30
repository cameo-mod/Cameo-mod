# Line Breaker infantry rebalance proposal

Anchor spec: HP=750000, Speed=80, Range=2500, eff-DPS=1600, Cost=1600

Converter law: cost pinned, range clamped to band + made unique, eff-DPS trimmed to Δ≤1 via 100-grid warhead Damage; unconditional FirepowerMultiplier is retired.

Uniqueness separates **raw warhead Damage (the 5-stat law as written)**. The levers run coarsest-first — Damage (100 grid) → Speed (1) → Range (10) — because one Damage step is a whole shot and running it last threw away everything the fine levers had achieved.

| actor | faction | HP | spd | rng | cost | dmg/wh×n | rl | burst | legacy FP% | eff DPS | price | Δ | flags |
|---|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|---|
| `ordos_heavyautoguntank` | d2k_ordos | 75000 | 85 | 2840 | 2800 | 22700×3 | 24 | 4 | 40 | 9080.0 | 2801 | +1.2 | fp-debt |
| `ra2_allies_battlefortress` | redalert2_allies | 70000 | 75 | 2930 | 4000 | 184800×2 | 15 | 1 | 100 | 24640.0 | 4001 | +0.8 |  |
| `ra2_allies_battlefortress_chrono` | redalert2_allies | 67500 | 90 | 2920 | 4000 | 158500×2 | 15 | 1 | 100 | 21133.3 | 3999 | -1.0 |  |
| `ra2_allies_battlefortress_empty` | redalert2_allies | 100000 | 75 | 2910 | 4000 | 154800×2 | 15 | 1 | 100 | 20640.0 | 4001 | +1.0 |  |
| `asianalliance_asianflametank` | redalert2mod_asianalliance | 100000 | 85 | 2550 | 1300 | 100×1 | 0 | 1 | 75 | 0.0 | 289 | -1010.6 | fp-debt |
| `asianalliance_warturtle` | redalert2mod_asianalliance | 100000 | 75 | 3000 | 5000 | 1747200×1 | 92 | 1 | 100 | 18991.3 | 5000 | -0.1 |  |
| `steelconsortium_megalodon` | redalert2mod_consortium | 97500 | 80 | 2040 | 4600 | 550200×1 | 24 | 1 | 100 | 22925.0 | 4600 | -0.3 |  |
| `steelconsortium_poseidontank` | redalert2mod_consortium | 95000 | 65 | 2480 | 4000 | 200×1 | 0 | 1 | 100 | 0.0 | 214 | -3786.3 |  |
| `futuretech_plasmastrider` | redalert2mod_futuretech | 85000 | 65 | 2980 | 2600 | 773800×1 | 50 | 2 | 100 | 25793.3 | 2600 | -0.1 |  |
| `naxis_oldtank` | redalert2mod_naxis | 80000 | 90 | 2960 | 2000 | 290300×2 | 75 | 1 | 100 | 7741.3 | 1981 | -19.1 |  |
| `latinsyndicate_carteltruck` | redalert2mod_syndicate | 85000 | 70 | 2490 | 6000 | 300×1 | 0 | 1 | 100 | 0.0 | 218 | -5781.7 |  |
| `latinsyndicate_tortugatank` | redalert2mod_syndicate | 80000 | 95 | 2970 | 3000 | 187000×1 | 35 | 5 | 100 | 17000.0 | 3000 | +0.1 |  |
| `tkm_battlebus` | redalert2mod_tkm | 50000 | 95 | 2890 | 1250 | 200×2 | 5 | 1 | 100 | 80.0 | 182 | -1068.2 |  |
| `protoss_archon` | starcraft_protoss | 75000 | 85 | 2940 | 5600 | 370600×1 | 20 | 1 | 100 | 18530.0 | 5571 | -28.9 |  |
| `td_gdi_assaultapc` | tiberiandawn_gdi | 100000 | 80 | 2900 | 4500 | 181000×1 | 30 | 4 | 100 | 17238.1 | 4499 | -0.6 |  |
| `td_nod_flametank` | tiberiandawn_nod | 100000 | 80 | 2500 | 800 | 14000×1 | 50 | 2 | 100 | 466.7 | 457 | -342.6 | anchor shared-wpn? |
| `td_nod_flametankmkii` | tiberiandawn_nod | 95000 | 95 | 2510 | 1300 | 151300×1 | 54 | 2 | 100 | 4728.1 | 1300 | -0.3 |  |
| `cabal_beholder` | tiberiansun_cabal | 95000 | 95 | 2530 | 2500 | 226800×1 | 50 | 2 | 100 | 8247.3 | 2500 | -0.1 |  |
| `forgotten_closhtank` | tiberiansun_forgotten | 95000 | 80 | 2390 | 1000 | 121600×1 | 50 | 1 | 100 | 2432.0 | 1000 | -0.2 |  |
| `forgotten_flametank` | tiberiansun_forgotten | 90000 | 90 | 2420 | 1300 | 328700×1 | 65 | 1 | 100 | 5056.9 | 1300 | +0.1 |  |
| `forgotten_thumperbus` | tiberiansun_forgotten | 90000 | 95 | 2990 | 5200 | 1195500×2 | 120 | 1 | 100 | 19925.0 | 5187 | -12.9 |  |
| `ts_gdi_disruptor` | tiberiansun_gdi | 95000 | 70 | 2880 | 2400 | 320100×2 | 60 | 1 | 100 | 10670.0 | 2400 | -0.1 |  |
| `ts_gdi_mobileemp` | tiberiansun_gdi | 100000 | 70 | 2030 | 1400 | 900100×1 | 200 | 1 | 100 | 4500.5 | 1400 | +0.0 |  |
| `ts_nod_devilstongue` | tiberiansun_nod | 65000 | 65 | 2850 | 1150 | 60000×1 | 52 | 5 | 100 | 5000.0 | 1150 | -0.1 |  |
| `wc2_humans_demolitionsquad` | warcraft2_humans | 37500 | 90 | 2500 | 800 | 500×1 | 0 | 1 | 100 | 0.0 | 305 | -495.0 |  |
| `wc2_humans_paladin` | warcraft2_humans | 62500 | 85 | 2020 | 1600 | 70100×1 | 12 | 1 | 100 | 5841.7 | 1600 | +0.0 |  |
| `wc2_humans_warcraft3knight` | warcraft2_humans | 60000 | 85 | 2010 | 2200 | 95800×1 | 12 | 1 | 100 | 7983.3 | 2199 | -0.9 |  |
| `wc2_orcs_goblinsappers` | warcraft2_orcs | 40000 | 90 | 2520 | 800 | 600×1 | 0 | 1 | 100 | 0.0 | 308 | -492.5 |  |
| `wc2_orcs_ogremage` | warcraft2_orcs | 57500 | 75 | 2000 | 1800 | 139800×1 | 20 | 1 | 100 | 6990.0 | 1800 | +0.5 |  |

**Worst |Δ| among non-anchor members: 5781.7** (goal ≤1).

## Uniqueness check (5 raw stats — soft/protected excluded)

- **HP duplicates**: {75000: ['ordos_heavyautoguntank', 'protoss_archon'], 100000: ['ra2_allies_battlefortress_empty', 'asianalliance_asianflametank', 'asianalliance_warturtle', 'td_gdi_assaultapc', 'ts_gdi_mobileemp'], 95000: ['steelconsortium_poseidontank', 'td_nod_flametankmkii', 'cabal_beholder', 'forgotten_closhtank', 'ts_gdi_disruptor'], 85000: ['futuretech_plasmastrider', 'latinsyndicate_carteltruck'], 80000: ['naxis_oldtank', 'latinsyndicate_tortugatank'], 90000: ['forgotten_flametank', 'forgotten_thumperbus']}
- **Speed duplicates**: {85: ['ordos_heavyautoguntank', 'asianalliance_asianflametank', 'protoss_archon', 'wc2_humans_paladin', 'wc2_humans_warcraft3knight'], 75: ['ra2_allies_battlefortress', 'ra2_allies_battlefortress_empty', 'asianalliance_warturtle', 'wc2_orcs_ogremage'], 90: ['ra2_allies_battlefortress_chrono', 'naxis_oldtank', 'forgotten_flametank', 'wc2_humans_demolitionsquad', 'wc2_orcs_goblinsappers'], 80: ['steelconsortium_megalodon', 'td_gdi_assaultapc', 'forgotten_closhtank'], 65: ['steelconsortium_poseidontank', 'futuretech_plasmastrider', 'ts_nod_devilstongue'], 70: ['latinsyndicate_carteltruck', 'ts_gdi_disruptor', 'ts_gdi_mobileemp'], 95: ['latinsyndicate_tortugatank', 'tkm_battlebus', 'td_nod_flametankmkii', 'cabal_beholder', 'forgotten_thumperbus']}
- **raw ReloadDelay duplicates** (design choice — retune coarse Damage/reload by hand): {24: ['ordos_heavyautoguntank', 'steelconsortium_megalodon'], 15: ['ra2_allies_battlefortress', 'ra2_allies_battlefortress_chrono', 'ra2_allies_battlefortress_empty'], 0: ['asianalliance_asianflametank', 'steelconsortium_poseidontank', 'latinsyndicate_carteltruck', 'wc2_humans_demolitionsquad', 'wc2_orcs_goblinsappers'], 50: ['futuretech_plasmastrider', 'cabal_beholder', 'forgotten_closhtank'], 20: ['protoss_archon', 'wc2_orcs_ogremage'], 12: ['wc2_humans_paladin', 'wc2_humans_warcraft3knight']}

## Required YAML edits (per unit)

- `ordos_heavyautoguntank`: HP 75000, Speed 85, Range 2840, each offensive warhead Damage 22700 (×3 = SUM 68100), ReloadDelay 24, Burst 4, **DELETE the unconditional FirepowerMultiplier (40%)** — the Damage above already includes it (W17), residual Δ +1.2 (cost pinned at 2800)
- `ra2_allies_battlefortress`: HP 70000, Speed 75, Range 2930, each offensive warhead Damage 184800 (×2 = SUM 369600), ReloadDelay 15, Burst 1
- `ra2_allies_battlefortress_chrono`: HP 67500, Speed 90, Range 2920, each offensive warhead Damage 158500 (×2 = SUM 317000), ReloadDelay 15, Burst 1, residual Δ -1.0 (cost pinned at 4000)
- `ra2_allies_battlefortress_empty`: HP 100000, Speed 75, Range 2910, each offensive warhead Damage 154800 (×2 = SUM 309600), ReloadDelay 15, Burst 1, residual Δ +1.0 (cost pinned at 4000)
- `asianalliance_asianflametank`: HP 100000, Speed 85, Range 2550, each offensive warhead Damage 100 (×1 = SUM 100), ReloadDelay 0, Burst 1, **DELETE the unconditional FirepowerMultiplier (75%)** — the Damage above already includes it (W17), residual Δ -1010.6 (cost pinned at 1300)
- `asianalliance_warturtle`: HP 100000, Speed 75, Range 3000, each offensive warhead Damage 1747200 (×1 = SUM 1747200), ReloadDelay 92, Burst 1
- `steelconsortium_megalodon`: HP 97500, Speed 80, Range 2040, each offensive warhead Damage 550200 (×1 = SUM 550200), ReloadDelay 24, Burst 1
- `steelconsortium_poseidontank`: HP 95000, Speed 65, Range 2480, each offensive warhead Damage 200 (×1 = SUM 200), ReloadDelay 0, Burst 1, residual Δ -3786.3 (cost pinned at 4000)
- `futuretech_plasmastrider`: HP 85000, Speed 65, Range 2980, each offensive warhead Damage 773800 (×1 = SUM 773800), ReloadDelay 50, Burst 2
- `naxis_oldtank`: HP 80000, Speed 90, Range 2960, each offensive warhead Damage 290300 (×2 = SUM 580600), ReloadDelay 75, Burst 1, residual Δ -19.1 (cost pinned at 2000)
- `latinsyndicate_carteltruck`: HP 85000, Speed 70, Range 2490, each offensive warhead Damage 300 (×1 = SUM 300), ReloadDelay 0, Burst 1, residual Δ -5781.7 (cost pinned at 6000)
- `latinsyndicate_tortugatank`: HP 80000, Speed 95, Range 2970, each offensive warhead Damage 187000 (×1 = SUM 187000), ReloadDelay 35, Burst 5
- `tkm_battlebus`: HP 50000, Speed 95, Range 2890, each offensive warhead Damage 200 (×2 = SUM 400), ReloadDelay 5, Burst 1, residual Δ -1068.2 (cost pinned at 1250)
- `protoss_archon`: HP 75000, Speed 85, Range 2940, each offensive warhead Damage 370600 (×1 = SUM 370600), ReloadDelay 20, Burst 1, residual Δ -28.9 (cost pinned at 5600)
- `td_gdi_assaultapc`: HP 100000, Speed 80, Range 2900, each offensive warhead Damage 181000 (×1 = SUM 181000), ReloadDelay 30, Burst 4
- `td_nod_flametankmkii`: HP 95000, Speed 95, Range 2510, each offensive warhead Damage 151300 (×1 = SUM 151300), ReloadDelay 54, Burst 2
- `cabal_beholder`: HP 95000, Speed 95, Range 2530, each offensive warhead Damage 226800 (×1 = SUM 226800), ReloadDelay 50, Burst 2
- `forgotten_closhtank`: HP 95000, Speed 80, Range 2390, each offensive warhead Damage 121600 (×1 = SUM 121600), ReloadDelay 50, Burst 1
- `forgotten_flametank`: HP 90000, Speed 90, Range 2420, each offensive warhead Damage 328700 (×1 = SUM 328700), ReloadDelay 65, Burst 1
- `forgotten_thumperbus`: HP 90000, Speed 95, Range 2990, each offensive warhead Damage 1195500 (×2 = SUM 2391000), ReloadDelay 120, Burst 1, residual Δ -12.9 (cost pinned at 5200)
- `ts_gdi_disruptor`: HP 95000, Speed 70, Range 2880, each offensive warhead Damage 320100 (×2 = SUM 640200), ReloadDelay 60, Burst 1
- `ts_gdi_mobileemp`: HP 100000, Speed 70, Range 2030, each offensive warhead Damage 900100 (×1 = SUM 900100), ReloadDelay 200, Burst 1
- `ts_nod_devilstongue`: HP 65000, Speed 65, Range 2850, each offensive warhead Damage 60000 (×1 = SUM 60000), ReloadDelay 52, Burst 5
- `wc2_humans_demolitionsquad`: HP 37500, Speed 90, Range 2500, each offensive warhead Damage 500 (×1 = SUM 500), ReloadDelay 0, Burst 1, residual Δ -495.0 (cost pinned at 800)
- `wc2_humans_paladin`: HP 62500, Speed 85, Range 2020, each offensive warhead Damage 70100 (×1 = SUM 70100), ReloadDelay 12, Burst 1
- `wc2_humans_warcraft3knight`: HP 60000, Speed 85, Range 2010, each offensive warhead Damage 95800 (×1 = SUM 95800), ReloadDelay 12, Burst 1
- `wc2_orcs_goblinsappers`: HP 40000, Speed 90, Range 2520, each offensive warhead Damage 600 (×1 = SUM 600), ReloadDelay 0, Burst 1, residual Δ -492.5 (cost pinned at 800)
- `wc2_orcs_ogremage`: HP 57500, Speed 75, Range 2000, each offensive warhead Damage 139800 (×1 = SUM 139800), ReloadDelay 20, Burst 1
