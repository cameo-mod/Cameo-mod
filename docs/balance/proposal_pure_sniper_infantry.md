# Pure Sniper infantry rebalance proposal

Anchor spec: HP=11000, Speed=60, Range=10000, eff-DPS=300, Cost=320

Converter law: cost pinned, range clamped to band + made unique, eff-DPS trimmed to Δ≤1 via 2000-grid warhead Damage × 1% FirepowerMultiplier.

| actor | faction | HP | spd | rng | cost | dmg/wh×n | rl | burst | FP% | eff DPS | price | Δ | flags |
|---|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|---|
| `ra2_allies_sniper` | redalert2_allies | 22000 | 56 | 10000 | 800 | 44000×1 | 54 | 1 | 100 | 611.1 | 773 | -27.0 | verifier |
| `asianalliance_asdf` | redalert2mod_asianalliance | 39000 | 58 | 9190 | 357 | 2000×2 | 16 | 3 | 16 | 84.0 | 357 | -0.0 |  |
| `asianalliance_shinobi` | redalert2mod_asianalliance | 19000 | 60 | 9120 | 750 | 24000×1 | 25 | 1 | 96 | 691.2 | 750 | -0.2 |  |
| `naxis_naximercenarysniper` | redalert2mod_naxis | 8000 | 60 | 10000 | 250 | 24000×1 | 120 | 1 | 100 | 150.0 | 190 | -59.7 | anchor |
| `tkm_sniper` | redalert2mod_tkm | 20000 | 59 | 10040 | 600 | 32000×1 | 55 | 1 | 102 | 445.1 | 600 | -0.1 |  |
| `ra1_allies_alliedsniper` | redalert_allies | 10000 | 61 | 9200 | 500 | 74000×1 | 100 | 1 | 99 | 549.5 | 500 | +0.1 |  |
| `ra1_soviets_commissar` | redalert_soviets | 30000 | 71 | 9090 | 700 | 8000×1 | 20 | 1 | 103 | 309.0 | 700 | +0.3 |  |
| `terran_reaper` | starcraft_terran | 60000 | 72 | 9070 | 600 | 2000×1 | 22 | 1 | 8 | 5.5 | 600 | +0.2 |  |
| `td_gdi_heavysniper` | tiberiandawn_gdi | 25000 | 72 | 9080 | 700 | 8000×4 | 75 | 1 | 97 | 388.0 | 701 | +1.0 |  |
| `forgotten_mutantsniper` | tiberiansun_forgotten | 9000 | 70 | 9580 | 650 | 30000×1 | 25 | 1 | 99 | 891.0 | 650 | +0.0 |  |
| `wc2_humans_archmage` | warcraft2_humans | 48000 | 65 | 9050 | 1000 | 14000×1 | 40 | 1 | 93 | 325.5 | 998 | -2.1 | shared-wpn? |
| `wc2_humans_highelfpriest` | warcraft2_humans | 49000 | 66 | 9030 | 1000 | 12000×1 | 40 | 1 | 104 | 312.0 | 1002 | +2.2 | shared-wpn? |
| `wc2_humans_highelfsorceress` | warcraft2_humans | 50000 | 64 | 9010 | 1000 | 12000×1 | 40 | 1 | 105 | 315.0 | 1000 | -0.0 | shared-wpn? |
| `wc2_humans_mage` | warcraft2_humans | 51000 | 67 | 9040 | 1000 | 12000×1 | 40 | 1 | 95 | 285.0 | 999 | -1.0 | shared-wpn? |
| `wc2_orcs_deathknight` | warcraft2_orcs | 52000 | 68 | 9000 | 1000 | 4000×2 | 40 | 1 | 121 | 272.2 | 1001 | +0.5 |  |

**Worst |Δ| among non-anchor members: 2.2** (goal ≤1).

## Uniqueness check (5 raw stats — soft/protected excluded)

- **Speed duplicates**: {72: ['terran_reaper', 'td_gdi_heavysniper']}
- **raw ReloadDelay duplicates** (design choice — retune coarse Damage/reload by hand): {25: ['asianalliance_shinobi', 'forgotten_mutantsniper'], 40: ['wc2_humans_archmage', 'wc2_humans_highelfpriest', 'wc2_humans_highelfsorceress', 'wc2_humans_mage', 'wc2_orcs_deathknight']}

## Required YAML edits (per unit)

- `asianalliance_asdf`: HP 39000, Speed 58, Range 9190, each offensive warhead Damage 2000 (×2 = SUM 4000), ReloadDelay 16, Burst 3, FirepowerMultiplier@ASIANALLIANCEASDF 16
- `asianalliance_shinobi`: HP 19000, Speed 60, Range 9120, each offensive warhead Damage 24000 (×1 = SUM 24000), ReloadDelay 25, Burst 1, FirepowerMultiplier@ASIANALLIANCESHINOBI 96
- `tkm_sniper`: HP 20000, Speed 59, Range 10040, each offensive warhead Damage 32000 (×1 = SUM 32000), ReloadDelay 55, Burst 1, FirepowerMultiplier@TKMSNIPER 102
- `ra1_allies_alliedsniper`: HP 10000, Speed 61, Range 9200, each offensive warhead Damage 74000 (×1 = SUM 74000), ReloadDelay 100, Burst 1, FirepowerMultiplier@RA1ALLIESALLIEDSNIPER 99
- `ra1_soviets_commissar`: HP 30000, Speed 71, Range 9090, each offensive warhead Damage 8000 (×1 = SUM 8000), ReloadDelay 20, Burst 1, FirepowerMultiplier@RA1SOVIETSCOMMISSAR 103
- `terran_reaper`: HP 60000, Speed 72, Range 9070, each offensive warhead Damage 2000 (×1 = SUM 2000), ReloadDelay 22, Burst 1, FirepowerMultiplier@TERRANREAPER 8
- `td_gdi_heavysniper`: HP 25000, Speed 72, Range 9080, each offensive warhead Damage 8000 (×4 = SUM 32000), ReloadDelay 75, Burst 1, FirepowerMultiplier@TDGDIHEAVYSNIPER 97, residual Δ +1.0 (cost pinned at 700)
- `forgotten_mutantsniper`: HP 9000, Speed 70, Range 9580, each offensive warhead Damage 30000 (×1 = SUM 30000), ReloadDelay 25, Burst 1, FirepowerMultiplier@FORGOTTENMUTANTSNIPER 99
- `wc2_humans_archmage`: HP 48000, Speed 65, Range 9050, each offensive warhead Damage 14000 (×1 = SUM 14000), ReloadDelay 40, Burst 1, FirepowerMultiplier@WC2HUMANSARCHMAGE 93, residual Δ -2.1 (cost pinned at 1000)
- `wc2_humans_highelfpriest`: HP 49000, Speed 66, Range 9030, each offensive warhead Damage 12000 (×1 = SUM 12000), ReloadDelay 40, Burst 1, FirepowerMultiplier@WC2HUMANSHIGHELFPRIEST 104, residual Δ +2.2 (cost pinned at 1000)
- `wc2_humans_highelfsorceress`: HP 50000, Speed 64, Range 9010, each offensive warhead Damage 12000 (×1 = SUM 12000), ReloadDelay 40, Burst 1, FirepowerMultiplier@WC2HUMANSHIGHELFSORCERESS 105
- `wc2_humans_mage`: HP 51000, Speed 67, Range 9040, each offensive warhead Damage 12000 (×1 = SUM 12000), ReloadDelay 40, Burst 1, FirepowerMultiplier@WC2HUMANSMAGE 95
- `wc2_orcs_deathknight`: HP 52000, Speed 68, Range 9000, each offensive warhead Damage 4000 (×2 = SUM 8000), ReloadDelay 40, Burst 1, FirepowerMultiplier@WC2ORCSDEATHKNIGHT 121
