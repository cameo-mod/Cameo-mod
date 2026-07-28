# Scout infantry rebalance proposal (corrected for uniqueness)

Anchor spec: HP=20000, Speed=60, Range=5000, eff-DPS=60, Cost=100

| actor | faction | HP | spd | rng | cost | dmg | burst | rl | FP% | wc | eff DPS | formula price | Δ | note |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| `naxis_naxiriflesoldier` | naxis | 29000 | 52 | 5495 | 100 | 6000 | 1 | 75 | 70 | 0.75 | 42.0 | 100 | +0 | anchor-ish baseline |
| `forgotten_mutantsoldier` | forgotten | 40000 | 59 | 5000 | 250 | 8000 | 1 | 50 | 100 | 0.75 | 120.0 | 247 | -3 | verifier |
| `asianalliance_asianmilitia` | asianalliance | 24000 | 57 | 4570 | 110 | 6000 | 1 | 50 | 70 | 0.75 | 63.0 | 106 | -4 |  |
| `ixian_lightinfantry` | ixian | 35000 | 55 | 4530 | 150 | 4000 | 1 | 20 | 54 | 0.75 | 81.0 | 154 | +4 |  |
| `ordos_lightinfantry` | ordos | 37000 | 51 | 4500 | 150 | 4000 | 1 | 20 | 55 | 0.75 | 82.5 | 153 | +3 |  |
| `light_inf` | d2k_shared | 36000 | 54 | 4510 | 150 | 4000 | 1 | 20 | 53 | 0.75 | 79.5 | 153 | +3 |  |
| `latinsyndicate_latinmilitia` | latinsyndicate | 25000 | 56 | 4520 | 160 | 2000 | 3 | 22 | 60 | 0.75 | 122.7 | 165 | +5 |  |
| `naxis_naxiriflerecruit` | naxis | 20000 | 53 | 5485 | 75 | 8000 | 1 | 100 | 81 | 0.75 | 48.6 | 87 | +12 |  |
| `ra1_soviets_ak47conscript` | ra1_soviets | 44000 | 71 | 4505 | 200 | 2000 | 3 | 11 | 20 | 0.875 | 95.5 | 241 | +41 |  |
| `ra2_allies_gi` | ra2_allies | 50000 | 48 | 4515 | 200 | 2000 | 3 | 15 | 33 | 0.875 | 115.5 | 229 | +29 |  |
| `ra2_soviets_conscript` | ra2_soviets | 26000 | 58 | 4525 | 100 | 2000 | 1 | 18 | 63 | 0.75 | 52.5 | 101 | +1 |  |
| `tkm_rifleman` | tkm | 32000 | 60 | 5500 | 120 | 6000 | 1 | 75 | 73 | 0.75 | 43.8 | 120 | -0 |  |
| `tkm_trooper` | tkm | 33000 | 61 | 5490 | 220 | 2000 | 5 | 31 | 40 | 0.875 | 112.9 | 225 | +5 |  |
| `td_gdi_minigunner` | td_gdi | 31000 | 63 | 4750 | 100 | 2000 | 4 | 50 | 30 | 0.75 | 36.0 | 102 | +2 |  |
| `td_nod_minigunner` | td_nod | 30000 | 66 | 4535 | 100 | 2000 | 4 | 50 | 27 | 0.75 | 32.4 | 96 | -4 |  |
| `ra1_allies_rifleinfantry` | ra1_allies | 28000 | 50 | 5250 | 100 | 2000 | 3 | 50 | 50 | 0.75 | 45.0 | 97 | -3 |  |
| `ra1_soviets_rifleinfantry` | ra1_soviets | 34000 | 49 | 4600 | 100 | 2000 | 3 | 50 | 55 | 0.75 | 49.5 | 106 | +6 |  |

## Uniqueness check

> **Note:** This check was run with the original 4-field uniqueness
> definition (HP, Speed, Range, effective DPS). The binding rule was
> later expanded to 5 fields in `FORMULA_V2.md` §3d (2026-07-21):
> HP, Speed, effective damage per shot, raw ReloadDelay, Range —
> checked separately. Re-run with the 5-field definition before
> applying.

- All uniqueness checks passed (HP, Speed, Range, effective DPS).

## Out-of-scope units (maintainer decisions applied)

- `forgotten_mutant` → reclassified to closecombat infantry (was range 3132).
- `schwarzermond_lunarsoldier` → already moved to special forces; excluded from this scout pass.
- `alien.nax` → civilian variant spawned from asteroids/dead aircraft; set Cost to **1000** (stats unchanged).
- Spies, civilian Naxis variants, casters, and units priced outside the scout envelope remain for a future pass.
- Raw Damage values are kept in 2000-step increments; effective-DPS uniqueness is enforced via per-actor FirepowerMultiplier.

## Required YAML edits (per unit)

- `naxis_naxiriflesoldier`: HP 29000, Speed 52, Range 5495, weapon Damage 6000, ReloadDelay 75, Burst 1, FirepowerMultiplier@NAXISNAXIRIFLESOLDIER 70
- `forgotten_mutantsoldier`: HP 40000, Speed 59, Range 5000, weapon Damage 8000, ReloadDelay 50, Burst 1, FirepowerMultiplier@FORGOTTENMUTANTSOLDIER 100
- `asianalliance_asianmilitia`: HP 24000, Speed 57, Range 4570, weapon Damage 6000, ReloadDelay 50, Burst 1, FirepowerMultiplier@ASIANALLIANCEASIANMILITIA 70
- `ixian_lightinfantry`: HP 35000, Speed 55, Range 4530, weapon Damage 4000, ReloadDelay 20, Burst 1, FirepowerMultiplier@IXIANLIGHTINFANTRY 54
- `ordos_lightinfantry`: HP 37000, Speed 51, Range 4500, weapon Damage 4000, ReloadDelay 20, Burst 1, FirepowerMultiplier@ORDOSLIGHTINFANTRY 55
- `light_inf`: HP 36000, Speed 54, Range 4510, weapon Damage 4000, ReloadDelay 20, Burst 1, FirepowerMultiplier@LIGHTINF 53
- `latinsyndicate_latinmilitia`: HP 25000, Speed 56, Range 4520, weapon Damage 2000, ReloadDelay 22, Burst 3, FirepowerMultiplier@LATINSYNDICATELATINMILITIA 60
- `naxis_naxiriflerecruit`: HP 20000, Speed 53, Range 5485, weapon Damage 8000, ReloadDelay 100, Burst 1, FirepowerMultiplier@NAXISNAXIRIFLERECRUIT 81, formula price delta +12 (informational; cost pinned at 75)
- `ra1_soviets_ak47conscript`: HP 44000, Speed 71, Range 4505, weapon Damage 2000, ReloadDelay 11, Burst 3, FirepowerMultiplier@RA1SOVIETSAK47CONSCRIPT 20, formula price delta +41 (informational; cost pinned at 200)
- `ra2_allies_gi`: HP 50000, Speed 48, Range 4515, weapon Damage 2000, ReloadDelay 15, Burst 3, FirepowerMultiplier@RA2ALLIESGI 33, formula price delta +29 (informational; cost pinned at 200)
- `ra2_soviets_conscript`: HP 26000, Speed 58, Range 4525, weapon Damage 2000, ReloadDelay 18, Burst 1, FirepowerMultiplier@RA2SOVIETSCONSCRIPT 63
- `tkm_rifleman`: HP 32000, Speed 60, Range 5500, weapon Damage 6000, ReloadDelay 75, Burst 1, FirepowerMultiplier@TKMRIFLEMAN 73
- `tkm_trooper`: HP 33000, Speed 61, Range 5490, weapon Damage 2000, ReloadDelay 31, Burst 5, FirepowerMultiplier@TKMTROOPER 40
- `td_gdi_minigunner`: HP 31000, Speed 63, Range 4750, weapon Damage 2000, ReloadDelay 50, Burst 4, FirepowerMultiplier@TDGDIMINIGUNNER 30
- `td_nod_minigunner`: HP 30000, Speed 66, Range 4535, weapon Damage 2000, ReloadDelay 50, Burst 4, FirepowerMultiplier@TDNODMINIGUNNER 27
- `ra1_allies_rifleinfantry`: HP 28000, Speed 50, Range 5250, weapon Damage 2000, ReloadDelay 50, Burst 3, FirepowerMultiplier@RA1ALLIESRIFLEINFANTRY 50
- `ra1_soviets_rifleinfantry`: HP 34000, Speed 49, Range 4600, weapon Damage 2000, ReloadDelay 50, Burst 3, FirepowerMultiplier@RA1SOVIETSRIFLEINFANTRY 55, formula price delta +6 (informational; cost pinned at 100)
