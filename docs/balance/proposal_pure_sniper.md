# Pure Sniper infantry rebalance proposal

Anchor spec: HP=11000, Speed=60, Range=10000, eff-DPS=300, Cost=320

Converter law: cost pinned, range clamped to band + made unique, eff-DPS trimmed to Δ≤1 via 100-grid warhead Damage; unconditional FirepowerMultiplier is retired.

Uniqueness separates **raw warhead Damage (the 5-stat law as written)**. The levers run coarsest-first — Damage (100 grid) → Speed (1) → Range (10) — because one Damage step is a whole shot and running it last threw away everything the fine levers had achieved.

| actor | faction | HP | spd | rng | cost | dmg/wh×n | rl | burst | legacy FP% | eff DPS | price | Δ | flags |
|---|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|---|
| `ra2_allies_sniper` | redalert2_allies | 22000 | 56 | 10010 | 800 | 33100×1 | 54 | 1 | 100 | 613.0 | 800 | +0.2 |  |
| `asianalliance_asdf` | redalert2mod_asianalliance | 39000 | 58 | 9090 | 357 | 100×1 | 16 | 3 | 109 | 15.0 | 523 | +166.2 | OVERPRICED@min-dps fp-debt |
| `asianalliance_shinobi` | redalert2mod_asianalliance | 19000 | 60 | 9060 | 750 | 6100×1 | 25 | 1 | 100 | 244.0 | 751 | +0.5 |  |
| `naxis_naximercenarysniper` | redalert2mod_naxis | 8000 | 60 | 10000 | 250 | 24000×1 | 120 | 1 | 100 | 200.0 | 217 | -33.4 | anchor |
| `tkm_sniper` | redalert2mod_tkm | 20000 | 59 | 10020 | 600 | 16800×1 | 55 | 1 | 100 | 305.5 | 600 | +0.1 |  |
| `ra1_allies_alliedsniper` | redalert_allies | 10000 | 61 | 9170 | 500 | 18800×1 | 100 | 1 | 100 | 188.0 | 500 | +0.1 |  |
| `ra1_soviets_commissar` | redalert_soviets | 30000 | 71 | 9200 | 700 | 900×1 | 20 | 1 | 100 | 45.0 | 700 | +0.0 |  |
| `terran_reaper` | starcraft_terran | 60000 | 72 | 9040 | 600 | 200×1 | 22 | 1 | 100 | 9.1 | 1200 | +599.9 | OVERPRICED@min-dps |
| `td_gdi_heavysniper` | tiberiandawn_gdi | 25000 | 67 | 10060 | 700 | 4600×3 | 75 | 1 | 100 | 184.0 | 700 | +0.0 |  |
| `forgotten_mutantsniper` | tiberiansun_forgotten | 9000 | 70 | 9530 | 650 | 8800×1 | 25 | 1 | 100 | 352.0 | 650 | -0.1 |  |
| `wc2_humans_archmage` | warcraft2_humans | 48000 | 65 | 9080 | 1000 | 9800×1 | 40 | 1 | 100 | 245.0 | 1000 | +0.2 | shared-wpn? |
| `wc2_humans_highelfpriest` | warcraft2_humans | 49000 | 66 | 9030 | 1000 | 9300×1 | 40 | 1 | 100 | 232.5 | 1001 | +0.7 | shared-wpn? |
| `wc2_humans_highelfsorceress` | warcraft2_humans | 50000 | 64 | 9020 | 1000 | 9400×1 | 40 | 1 | 100 | 235.0 | 1000 | -0.4 | shared-wpn? |
| `wc2_humans_mage` | warcraft2_humans | 51000 | 63 | 10100 | 1000 | 8300×1 | 40 | 1 | 100 | 207.5 | 1000 | -0.0 | shared-wpn? |
| `wc2_orcs_deathknight` | warcraft2_orcs | 52000 | 62 | 10370 | 1000 | 4000×2 | 40 | 1 | 100 | 200.0 | 1000 | -0.0 |  |

**Worst |Δ| among non-anchor members: 599.9** (goal ≤1).

## Uniqueness check (5 raw stats — soft/protected excluded)

- **raw ReloadDelay duplicates** (design choice — retune coarse Damage/reload by hand): {25: ['asianalliance_shinobi', 'forgotten_mutantsniper'], 40: ['wc2_humans_archmage', 'wc2_humans_highelfpriest', 'wc2_humans_highelfsorceress', 'wc2_humans_mage', 'wc2_orcs_deathknight']}

## Required YAML edits (per unit)

- `ra2_allies_sniper`: HP 22000, Speed 56, Range 10010, each offensive warhead Damage 33100 (×1 = SUM 33100), ReloadDelay 54, Burst 1
- `asianalliance_asdf`: HP 39000, Speed 58, Range 9090, each offensive warhead Damage 100 (×1 = SUM 100), ReloadDelay 16, Burst 3, **DELETE the unconditional FirepowerMultiplier (109%)** — the Damage above already includes it (W17), residual Δ +166.2 (cost pinned at 357)
- `asianalliance_shinobi`: HP 19000, Speed 60, Range 9060, each offensive warhead Damage 6100 (×1 = SUM 6100), ReloadDelay 25, Burst 1
- `tkm_sniper`: HP 20000, Speed 59, Range 10020, each offensive warhead Damage 16800 (×1 = SUM 16800), ReloadDelay 55, Burst 1
- `ra1_allies_alliedsniper`: HP 10000, Speed 61, Range 9170, each offensive warhead Damage 18800 (×1 = SUM 18800), ReloadDelay 100, Burst 1
- `ra1_soviets_commissar`: HP 30000, Speed 71, Range 9200, each offensive warhead Damage 900 (×1 = SUM 900), ReloadDelay 20, Burst 1
- `terran_reaper`: HP 60000, Speed 72, Range 9040, each offensive warhead Damage 200 (×1 = SUM 200), ReloadDelay 22, Burst 1, residual Δ +599.9 (cost pinned at 600)
- `td_gdi_heavysniper`: HP 25000, Speed 67, Range 10060, each offensive warhead Damage 4600 (×3 = SUM 13800), ReloadDelay 75, Burst 1
- `forgotten_mutantsniper`: HP 9000, Speed 70, Range 9530, each offensive warhead Damage 8800 (×1 = SUM 8800), ReloadDelay 25, Burst 1
- `wc2_humans_archmage`: HP 48000, Speed 65, Range 9080, each offensive warhead Damage 9800 (×1 = SUM 9800), ReloadDelay 40, Burst 1
- `wc2_humans_highelfpriest`: HP 49000, Speed 66, Range 9030, each offensive warhead Damage 9300 (×1 = SUM 9300), ReloadDelay 40, Burst 1
- `wc2_humans_highelfsorceress`: HP 50000, Speed 64, Range 9020, each offensive warhead Damage 9400 (×1 = SUM 9400), ReloadDelay 40, Burst 1
- `wc2_humans_mage`: HP 51000, Speed 63, Range 10100, each offensive warhead Damage 8300 (×1 = SUM 8300), ReloadDelay 40, Burst 1
- `wc2_orcs_deathknight`: HP 52000, Speed 62, Range 10370, each offensive warhead Damage 4000 (×2 = SUM 8000), ReloadDelay 40, Burst 1
