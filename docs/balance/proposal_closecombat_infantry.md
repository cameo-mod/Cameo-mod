# Closecombat infantry rebalance proposal

Anchor spec: HP=50000, Speed=75, Range=3500, eff-DPS=250, Cost=200

| actor | faction | HP | spd | rng | cost | dmg | dmg_filter | burst | rl | FP% | wc | eff DPS | formula price | Δ | note |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| `asianalliance_fanatic` | redalert2mod_asianalliance | 100000 | 75 | 3500 | 500 | 4000 | all | 10 | 70 | 100 | 0.875 | 500.0 | 500 | +0 | verifier |
| `alien.nax` | redalert2mod_naxis | 15000 | 40 | 2500 | 110 | 16000 | all | 2 | 54 | 125 | 1.000 | 701.8 | 166 | +56 |  |
| `naxis_sssoldier` | redalert2mod_naxis | 63000 | 55 | 4500 | 240 | 4000 | all | 10 | 75 | 88 | 0.875 | 256.7 | 171 | -69 |  |
| `td_gdi_shotgunner` | tiberiandawn_gdi | 50000 | 75 | 3500 | 200 | 4000 | all | 5 | 70 | 100 | 0.875 | 250.0 | 200 | +0 | anchor |

## Uniqueness check (5 raw stats — maintainer law 2026-07-22)

- **effective damage-per-shot duplicates**: {4000.0: ['asianalliance_fanatic', 'td_gdi_shotgunner']}

## Required YAML edits (per unit)

- `alien.nax`: HP 15000, Speed 40, Range 2500, weapon Damage 16000 (all), ReloadDelay 54, Burst 2, FirepowerMultiplier@ALIEN.NAX 125, formula price delta +56 (informational; cost pinned at 110)
- `naxis_sssoldier`: HP 63000, Speed 55, Range 4500, weapon Damage 4000 (all), ReloadDelay 75, Burst 10, FirepowerMultiplier@NAXISSSSOLDIER 88, formula price delta -69 (informational; cost pinned at 240)
