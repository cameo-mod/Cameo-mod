# Pure Sniper infantry rebalance proposal

Anchor spec: HP=11000, Speed=56, Range=10000, eff-DPS=305, Cost=320

Converter law: cost pinned, range clamped to band + made unique, eff-DPS trimmed to Δ≤1 via 2000-grid warhead Damage × 1% FirepowerMultiplier.

| actor | faction | HP | spd | rng | cost | dmg/wh×n | rl | burst | FP% | eff DPS | price | Δ | flags |
|---|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|---|
| `ra2_allies_sniper` | redalert2_allies | 22000 | 56 | 10000 | 800 | 44000×1 | 54 | 1 | 100 | 611.1 | 801 | +1.1 | verifier |
| `asianalliance_asdf` | redalert2mod_asianalliance | 39000 | 58 | 9070 | 357 | 2000×2 | 16 | 3 | 14 | 73.5 | 357 | +0.2 |  |
| `asianalliance_shinobi` | redalert2mod_asianalliance | 19000 | 60 | 9130 | 750 | 22000×1 | 25 | 1 | 100 | 660.0 | 751 | +0.8 |  |
| `naxis_naximercenarysniper` | redalert2mod_naxis | 8000 | 60 | 10000 | 250 | 24000×1 | 120 | 1 | 100 | 150.0 | 196 | -53.6 | anchor |
| `tkm_sniper` | redalert2mod_tkm | 21000 | 59 | 10000 | 600 | 30000×1 | 55 | 1 | 98 | 400.9 | 600 | +0.1 |  |
| `ra1_allies_alliedsniper` | redalert_allies | 10000 | 61 | 9180 | 500 | 70000×1 | 100 | 1 | 101 | 530.2 | 500 | +0.2 |  |
| `japan_archermaiden` | redalert_japan | 20000 | 67 | 9120 | 500 | 40000×1 | 100 | 1 | 99 | 297.0 | 500 | +0.1 |  |
| `ra1_soviets_commissar` | redalert_soviets | 30000 | 66 | 9100 | 700 | 8000×1 | 20 | 1 | 105 | 315.0 | 700 | -0.3 |  |
| `terran_reaper` | starcraft_terran | 60000 | 67 | 9050 | 600 | 2000×1 | 22 | 1 | 9 | 6.1 | 602 | +1.5 |  |
| `td_gdi_heavysniper` | tiberiandawn_gdi | 25000 | 66 | 9060 | 700 | 8000×4 | 75 | 1 | 101 | 404.0 | 701 | +1.3 |  |
| `forgotten_mutantsniper` | tiberiansun_forgotten | 9000 | 67 | 9560 | 650 | 30000×1 | 25 | 1 | 99 | 891.0 | 650 | -0.1 |  |
| `wc2_humans_archmage` | warcraft2_humans | 48000 | 65 | 9040 | 1000 | 12000×1 | 40 | 1 | 101 | 303.0 | 1001 | +1.1 | shared-wpn? |
| `wc2_humans_highelfpriest` | warcraft2_humans | 49000 | 65 | 9030 | 1000 | 12000×1 | 40 | 1 | 98 | 294.0 | 1002 | +1.8 | shared-wpn? |
| `wc2_humans_highelfsorceress` | warcraft2_humans | 50000 | 64 | 9020 | 1000 | 12000×1 | 40 | 1 | 97 | 291.0 | 1001 | +1.0 | shared-wpn? |
| `wc2_humans_mage` | warcraft2_humans | 51000 | 67 | 9010 | 1000 | 10000×1 | 40 | 1 | 105 | 262.5 | 999 | -1.5 | shared-wpn? |
| `wc2_orcs_deathknight` | warcraft2_orcs | 52000 | 65 | 9000 | 1000 | 4000×2 | 40 | 1 | 119 | 267.8 | 1002 | +1.6 |  |

**Worst |Δ| among non-anchor members: 1.8** (goal ≤1).

## Uniqueness check (5 raw stats — soft/protected excluded)

- **Speed duplicates**: {67: ['japan_archermaiden', 'terran_reaper', 'forgotten_mutantsniper', 'wc2_humans_mage'], 66: ['ra1_soviets_commissar', 'td_gdi_heavysniper'], 65: ['wc2_humans_archmage', 'wc2_humans_highelfpriest', 'wc2_orcs_deathknight']}
- **raw ReloadDelay duplicates** (design choice — retune coarse Damage/reload by hand): {25: ['asianalliance_shinobi', 'forgotten_mutantsniper'], 100: ['ra1_allies_alliedsniper', 'japan_archermaiden'], 40: ['wc2_humans_archmage', 'wc2_humans_highelfpriest', 'wc2_humans_highelfsorceress', 'wc2_humans_mage', 'wc2_orcs_deathknight']}

## Required YAML edits (per unit)

- `asianalliance_asdf`: HP 39000, Speed 58, Range 9070, each offensive warhead Damage 2000 (×2 = SUM 4000), ReloadDelay 16, Burst 3, FirepowerMultiplier@ASIANALLIANCEASDF 14
- `asianalliance_shinobi`: HP 19000, Speed 60, Range 9130, each offensive warhead Damage 22000 (×1 = SUM 22000), ReloadDelay 25, Burst 1, FirepowerMultiplier@ASIANALLIANCESHINOBI 100
- `tkm_sniper`: HP 21000, Speed 59, Range 10000, each offensive warhead Damage 30000 (×1 = SUM 30000), ReloadDelay 55, Burst 1, FirepowerMultiplier@TKMSNIPER 98
- `ra1_allies_alliedsniper`: HP 10000, Speed 61, Range 9180, each offensive warhead Damage 70000 (×1 = SUM 70000), ReloadDelay 100, Burst 1, FirepowerMultiplier@RA1ALLIESALLIEDSNIPER 101
- `japan_archermaiden`: HP 20000, Speed 67, Range 9120, each offensive warhead Damage 40000 (×1 = SUM 40000), ReloadDelay 100, Burst 1, FirepowerMultiplier@JAPANARCHERMAIDEN 99
- `ra1_soviets_commissar`: HP 30000, Speed 66, Range 9100, each offensive warhead Damage 8000 (×1 = SUM 8000), ReloadDelay 20, Burst 1, FirepowerMultiplier@RA1SOVIETSCOMMISSAR 105
- `terran_reaper`: HP 60000, Speed 67, Range 9050, each offensive warhead Damage 2000 (×1 = SUM 2000), ReloadDelay 22, Burst 1, FirepowerMultiplier@TERRANREAPER 9, residual Δ +1.5 (cost pinned at 600)
- `td_gdi_heavysniper`: HP 25000, Speed 66, Range 9060, each offensive warhead Damage 8000 (×4 = SUM 32000), ReloadDelay 75, Burst 1, FirepowerMultiplier@TDGDIHEAVYSNIPER 101, residual Δ +1.3 (cost pinned at 700)
- `forgotten_mutantsniper`: HP 9000, Speed 67, Range 9560, each offensive warhead Damage 30000 (×1 = SUM 30000), ReloadDelay 25, Burst 1, FirepowerMultiplier@FORGOTTENMUTANTSNIPER 99
- `wc2_humans_archmage`: HP 48000, Speed 65, Range 9040, each offensive warhead Damage 12000 (×1 = SUM 12000), ReloadDelay 40, Burst 1, FirepowerMultiplier@WC2HUMANSARCHMAGE 101, residual Δ +1.1 (cost pinned at 1000)
- `wc2_humans_highelfpriest`: HP 49000, Speed 65, Range 9030, each offensive warhead Damage 12000 (×1 = SUM 12000), ReloadDelay 40, Burst 1, FirepowerMultiplier@WC2HUMANSHIGHELFPRIEST 98, residual Δ +1.8 (cost pinned at 1000)
- `wc2_humans_highelfsorceress`: HP 50000, Speed 64, Range 9020, each offensive warhead Damage 12000 (×1 = SUM 12000), ReloadDelay 40, Burst 1, FirepowerMultiplier@WC2HUMANSHIGHELFSORCERESS 97, residual Δ +1.0 (cost pinned at 1000)
- `wc2_humans_mage`: HP 51000, Speed 67, Range 9010, each offensive warhead Damage 10000 (×1 = SUM 10000), ReloadDelay 40, Burst 1, FirepowerMultiplier@WC2HUMANSMAGE 105, residual Δ -1.5 (cost pinned at 1000)
- `wc2_orcs_deathknight`: HP 52000, Speed 65, Range 9000, each offensive warhead Damage 4000 (×2 = SUM 8000), ReloadDelay 40, Burst 1, FirepowerMultiplier@WC2ORCSDEATHKNIGHT 119, residual Δ +1.6 (cost pinned at 1000)
