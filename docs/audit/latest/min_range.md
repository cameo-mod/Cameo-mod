# Minimum range audit


## Weapons with MinRange != round(Range/5) to nearest step of 5

| Weapon | Range | MinRange | Expected MinRange |
|---|---|---|---|
| td_gdi_mlrs_227mmamt | 9993 | 1999 | 2000 |
| td_gdi_mlrs_227mm | 9993 | 1999 | 2000 |
| td_nod_artillery_artilleryshell | 11291 | 2258 | 2260 |
| td_nod_artillery_artilleryshellupgrade | 11291 | 2258 | 2260 |
| ra1_allies_alliedartillery_155mm | 11813 | 2670 | 2365 |
| ra1_allies_alliedartillery_155mmcryo | 11813 | 2670 | 2365 |
| ordos_chemturret | 14000 | 1985 | 2800 |

