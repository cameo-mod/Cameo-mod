# W11 — K comparison for class `mbt`

Anchor `tiger.nax`, cost0 800. Every member priced twice: on RAW
damage/reload, and on K-adjusted `effective_dps` from the derived sidecar
(accuracy, spread, falloff, range, dead zone, reachable targets).

The anchor is re-fitted in each mode, so each column is internally
consistent; only the SHAPE of the class changes between them.

| estimator | raw | K |
|---|---|---|
| O0 | 800.00 | 814.79 |
| P0 | 800.00 | 829.58 |
| Q0 | 800.00 | 859.16 |

| unit | cost | raw price | K price | raw Δ | K Δ | K vs raw |
|---|---|---|---|---|---|---|
| `protoss_dragoon` | 1200 | 1150 | 569 | -4% | -53% | -51% ❗ |
| `tkm_trenchtank` | 2500 | 3016 | 4323 | +21% | +73% | +43% ❗ |
| `ixian_mongoose` | 1300 | 1300 | 945 | +0% | -27% | -27% ⚠ |
| `cabal_widow` | 3500 | 7956 | 6127 | +127% | +75% | -23% ⚠ |
| `asianalliance_lynxtank` | 850 | 921 | 713 | +8% | -16% | -23% ⚠ |
| `ra1_soviets_hammertank` | 1500 | 1382 | 1664 | -8% | +11% | +20% ⚠ |
| `ordos_combatautoguntank` | 1500 | 1270 | 1022 | -15% | -32% | -20% ⚠ |
| `ra1_soviets_kotinnucleartank` | 1800 | 1658 | 1970 | -8% | +9% | +19% ⚠ |
| `steelconsortium_mako` | 900 | 901 | 735 | +0% | -18% | -18% ⚠ |
| `ra1_allies_alliedcybertank` | 1300 | 1199 | 1401 | -8% | +8% | +17% ⚠ |
| `ra1_allies_alliedtigerheavytank` | 1300 | 1199 | 1401 | -8% | +8% | +17% ⚠ |
| `japan_chihaheavytank` | 1200 | 960 | 1101 | -20% | -8% | +15% ⚠ |
| `ixian_heavykodatank` | 1100 | 1162 | 1325 | +6% | +20% | +14% ⚠ |
| `steelconsortium_quantumtank` | 1600 | 1356 | 1182 | -15% | -26% | -13% ⚠ |
| `tkm_t72m` | 900 | 1396 | 1223 | +55% | +36% | -12% ⚠ |
| `ixian_kodatank` | 800 | 800 | 893 | -0% | +12% | +12% ⚠ |
| `ordos_heavycombattank` | 950 | 950 | 1034 | +0% | +9% | +9% |
| `futuretech_guardiantank` | 850 | 850 | 909 | -0% | +7% | +7% |
| `ts_gdi_titanmkii` | 1600 | 1604 | 1706 | +0% | +7% | +6% |
| `ts_gdi_titan` | 950 | 954 | 1005 | +0% | +6% | +5% |
| `schwarzermond_lunartiger` | 950 | 950 | 902 | +0% | -5% | -5% |
| `oldqtnk.steel` | 2400 | 3062 | 2979 | +28% | +24% | -3% |
| `latinsyndicate_smokertank` | 1800 | 1734 | 1771 | -4% | -2% | +2% |
| `tkm_abrams` | 1000 | 1000 | 1020 | -0% | +2% | +2% |
| `td_gdi_battletank` | 900 | 928 | 910 | +3% | +1% | -2% |
| `combat_tank.atreides` | 600 | 938 | 956 | +56% | +59% | +2% |
| `assault.nax` | 900 | 3229 | 3291 | +259% | +266% | +2% |
| `ra1_soviets_heavytank` | 1000 | 1000 | 1015 | +0% | +2% | +2% |
| `cabal_tarantula` | 1000 | 1000 | 1014 | -0% | +1% | +1% |
| `ra2_allies_grizzlytank` | 750 | 1004 | 1016 | +34% | +36% | +1% |
| `forgotten_rattytank` | 600 | 600 | 607 | -0% | +1% | +1% |
| `combat_tank.harkonnen` | 600 | 457 | 462 | -24% | -23% | +1% |
| `td_gdi_predatortank` | 1250 | 1292 | 1279 | +3% | +2% | -1% |
| `tkm_technicaltank` | 700 | 700 | 706 | -0% | +1% | +1% |
| `ra2_soviets_rhinoheavytank` | 850 | 850 | 856 | -0% | +1% | +1% |
| `japan_igomediumtank` | 800 | 800 | 805 | -0% | +1% | +1% |
| `ra1_allies_alliedmediumtank` | 700 | 700 | 702 | +0% | +0% | +0% |
| `naxis_kingtigerheavytank` | 2000 | 2000 | 2002 | +0% | +0% | +0% |
| `ptnk.asian` | 2400 | 3210 | 3207 | +34% | +34% | -0% |
| `tiger.nax` | 800 | 800 | 800 | +0% | +0% | +0% |

## What the switch would do

- **40 units** priced both ways.
- Median price shift: **+1.2%**; range **-50.5% … +43.4%**.
- Moves AWAY from the current cost for **30/40** units, towards it for **10**.

A K switch is worth taking when it moves prices TOWARDS current costs for
units the maintainer already considers correctly priced — that is evidence
the coefficient is capturing something real rather than adding noise.
It is not a target to optimise: a weapon that genuinely is inaccurate
SHOULD price below its raw damage.

