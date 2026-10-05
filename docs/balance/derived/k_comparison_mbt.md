# W11 — K comparison for class `mbt`

Anchor `naxis_tiger`, cost0 800. Every member priced twice: on RAW
damage/reload, and on K-adjusted `effective_dps` from the derived sidecar
(accuracy, spread, falloff, range, dead zone, reachable targets).

The anchor is re-fitted in each mode, so each column is internally
consistent; only the SHAPE of the class changes between them.

| estimator | raw | K |
|---|---|---|
| O0 | 700.00 | 704.25 |
| P0 | 600.00 | 608.50 |
| Q0 | 400.00 | 417.00 |

| unit | cost | raw price | K price | raw Δ | K Δ | K vs raw |
|---|---|---|---|---|---|---|
| `asianalliance_ptnk` | 2400 | 1668 | 3467 | -31% | +44% | +108% ❗ |
| `protoss_dragoon` | 1200 | 1207 | 2095 | +1% | +75% | +74% ❗ |
| `ixian_mongoose` | 1300 | 1168 | 415 | -10% | -68% | -64% ❗ |
| `harkonnen_flametank` | 700 | 3953 | 6249 | +465% | +793% | +58% ❗ |
| `steelconsortium_mako` | 900 | 1066 | 465 | +18% | -48% | -56% ❗ |
| `naxis_assault` | 900 | 3690 | 5488 | +310% | +510% | +49% ❗ |
| `ra2_c_ifv` | 600 | 1947 | 1173 | +225% | +96% | -40% ❗ |
| `ra2_c_abram` | 900 | 2846 | 1914 | +216% | +113% | -33% ❗ |
| `ra2leopard` | 950 | 3131 | 2107 | +230% | +122% | -33% ❗ |
| `td_gdi_battletank` | 1300 | 1354 | 930 | +4% | -28% | -31% ❗ |
| `corrino_buggy` | 300 | 261 | 185 | -13% | -38% | -29% ⚠ |
| `td_gdi_predatortank` | 1250 | 1232 | 896 | -1% | -28% | -27% ⚠ |
| `steelconsortium_quantumtank` | 1600 | 1183 | 873 | -26% | -45% | -26% ⚠ |
| `steel_oldqtnk` | 2400 | 3301 | 2491 | +38% | +4% | -25% ⚠ |
| `yrrobo` | 1200 | 1279 | 997 | +7% | -17% | -22% ⚠ |
| `atreides_sonictank` | 1000 | 309 | 242 | -69% | -76% | -22% ⚠ |
| `cabal_widow` | 3500 | 7615 | 8940 | +118% | +155% | +17% ⚠ |
| `tkm_trenchtank` | 2500 | 1398 | 1582 | -44% | -37% | +13% ⚠ |
| `tkm_t72m` | 900 | 905 | 1004 | +1% | +12% | +11% ⚠ |
| `latinsyndicate_smokertank` | 1800 | 1844 | 1645 | +2% | -9% | -11% ⚠ |
| `ordos_combatautoguntank` | 1500 | 818 | 895 | -45% | -40% | +9% |
| `ts_gdi_titanmkii` | 1600 | 1473 | 1603 | -8% | +0% | +9% |
| `asianalliance_lynxtank` | 850 | 916 | 840 | +8% | -1% | -8% |
| `ra1_soviets_hammertank` | 1500 | 1697 | 1826 | +13% | +22% | +8% |
| `ts_gdi_titan` | 950 | 902 | 966 | -5% | +2% | +7% |
| `ra1_soviets_kotinnucleartank` | 1800 | 1651 | 1759 | -8% | -2% | +6% |
| `atreides_combattank` | 600 | 784 | 734 | +31% | +22% | -6% |
| `futuretech_guardiantank` | 850 | 741 | 786 | -13% | -8% | +6% |
| `corrino_bmp` | 400 | 482 | 454 | +21% | +13% | -6% |
| `ra1_allies_cybertank` | 1300 | 1473 | 1545 | +13% | +19% | +5% |
| `ra1_allies_tigerheavytank` | 1300 | 1473 | 1545 | +13% | +19% | +5% |
| `corrino_combattank` | 600 | 524 | 500 | -13% | -17% | -5% |
| `harkonnen_combat_tank` | 600 | 398 | 380 | -34% | -37% | -4% |
| `harkonnen_assaulttank` | 600 | 220 | 210 | -63% | -65% | -4% |
| `japan_chihaheavytank` | 1200 | 830 | 861 | -31% | -28% | +4% |
| `ordos_heavycombattank` | 950 | 915 | 887 | -4% | -7% | -3% |
| `cabal_tarantula` | 1000 | 936 | 914 | -6% | -9% | -2% |
| `ra1_soviets_heavytank` | 1450 | 1587 | 1613 | +9% | +11% | +2% |
| `ixian_heavykodatank` | 1100 | 917 | 930 | -17% | -15% | +1% |
| `tkm_abrams` | 1000 | 687 | 693 | -31% | -31% | +1% |
| `schwarzermond_lunartiger` | 950 | 661 | 666 | -30% | -30% | +1% |
| `tkm_technicaltank` | 700 | 621 | 625 | -11% | -11% | +1% |
| `ra1_allies_mediumtank` | 1280 | 1317 | 1325 | +3% | +4% | +1% |
| `ra2_allies_grizzlytank` | 750 | 754 | 758 | +0% | +1% | +1% |
| `japan_igomediumtank` | 800 | 949 | 954 | +19% | +19% | +1% |
| `ixian_kodatank` | 800 | 631 | 634 | -21% | -21% | +0% |
| `forgotten_rattytank` | 600 | 610 | 612 | +2% | +2% | +0% |
| `ra2_soviets_rhinoheavytank` | 850 | 1014 | 1017 | +19% | +20% | +0% |
| `naxis_kingtigerheavytank` | 2000 | 1981 | 1982 | -1% | -1% | +0% |
| `naxis_tiger` | 800 | 800 | 800 | +0% | +0% | +0% |

## What the switch would do

- **50 units** priced both ways.
- Median price shift: **+0.4%**; range **-64.5% … +107.8%**.
- Moves AWAY from the current cost for **29/50** units, towards it for **21**.

A K switch is worth taking when it moves prices TOWARDS current costs for
units the maintainer already considers correctly priced — that is evidence
the coefficient is capturing something real rather than adding noise.
It is not a target to optimise: a weapon that genuinely is inaccurate
SHOULD price below its raw damage.

