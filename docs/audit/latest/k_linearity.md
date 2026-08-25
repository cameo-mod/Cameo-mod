# audit_k_linearity — the flat K must not move when Damage moves

Analysed **2028** concrete weapons.

## L1 — `k_flat` is invariant under a change of flat Damage

_clean_ — `k_flat` held to within 1e-09 across 3 scalings of every weapon.

## L2 — the affine split decomposes the published `k`

`k == k_flat + pct_absolute / flat_total`, so nothing about the measured `effective_*` numbers changes — only the invertible form is new.

_clean_ — the identity holds for every analysed weapon.

## L3 — weapons with a %-twin DPS floor

65 weapon(s) carry a %-twin; **33** have a floor at or above 25% of output.

A price target below the floor is UNREACHABLE by lowering flat Damage — `required_damage()` returns None rather than a wrong positive number. To price these lower, the TWIN has to shrink.

| weapon | floor as share of output |
|---|--:|
| `PlasBullet` | 76.7% |
| `RA2SCUDELITE` | 44.8% |
| `RA160mmE_rad_elite` | 37.9% |
| `TSShadowTeamPistols` | 36.5% |
| `RA2KirovBomb_nuclear` | 33.0% |
| `VolkovMagneticWeaponNuclearShells` | 32.1% |
| `RA2KirovBomb_nuclear_elite` | 29.3% |
| `td_gdi_commando_sniper` | 28.7% |
| `CommissarPistol` | 28.7% |
| `ChamBlade` | 28.7% |
| `TSSniper` | 28.7% |
| `RA2AWP` | 28.7% |
| `RA2DoublePistols_elite` | 28.7% |
| `RA2AWP_elite` | 28.7% |
| `RA2NarcoPistol` | 28.7% |
| `NaxiSniper` | 28.7% |
| `BlackWidowPistols` | 28.7% |
| `tkmawp` | 28.7% |
| `ReaperPistols` | 28.7% |
| `ra1_allies_alliedsniper` | 28.7% |
| `TSEngineerPistol` | 28.7% |
| `TSSniper_elite` | 28.7% |
| `LightSniper` | 28.7% |
| `RA2DoublePistols` | 28.7% |
| `RA2DoublePistolsIFV` | 28.7% |
| `RA2NarcoPistols_elite` | 28.7% |
| `NaxiSniper_elite` | 28.7% |
| `BlackWidowPistols_elite` | 28.7% |
| `tkmsmg` | 28.7% |
| `VanSniper` | 28.7% |

_... and 3 more._
