# audit_k_linearity — the flat K must not move when Damage moves

Analysed **2018** concrete weapons.

## L1 — `k_flat` is invariant under a change of flat Damage

_clean_ — `k_flat` held to within 1e-09 across 3 scalings of every weapon.

## L2 — the affine split decomposes the published `k`

`k == k_flat + pct_absolute / flat_total`, so nothing about the measured `effective_*` numbers changes — only the invertible form is new.

_clean_ — the identity holds for every analysed weapon.

## L3 — weapons with a %-twin DPS floor

1568 weapon(s) carry a %-twin; **52** have a floor at or above 25% of output.

A price target below the floor is UNREACHABLE by lowering flat Damage — `required_damage()` returns None rather than a wrong positive number. To price these lower, the TWIN has to shrink.

| weapon | floor as share of output |
|---|--:|
| `PlasBullet` | 76.7% |
| `AnthraxCloudLarge` | 75.1% |
| `AnthraxCloudBlueLarge` | 73.1% |
| `AnthraxCloudPurpleLarge` | 71.5% |
| `D2K_StormGunInf` | 68.0% |
| `D2K_StormGunCymek` | 68.0% |
| `TSSmoke` | 54.3% |
| `RA2Cloud` | 54.3% |
| `RA2CloudSafe` | 54.3% |
| `AnthraxCloud` | 54.3% |
| `AnthraxCloudBlue` | 51.7% |
| `AnthraxCloudPurple` | 49.7% |
| `NodCommandoLaser` | 47.8% |
| `LightningBolt` | 44.7% |
| `CabalEliminatorGatling` | 41.9% |
| `Corsair_EMP` | 40.4% |
| `D2K_StormGun` | 37.3% |
| `d2kStormLasher` | 37.3% |
| `TSShadowTeamPistols` | 36.5% |
| `D2K_ShockGunInf` | 36.2% |
| `RA2KirovBomb_nuclear` | 35.6% |
| `d2k_munitions_explosion` | 32.9% |
| `ReimuDreamSeal` | 32.4% |
| `RA2KirovBomb_nuclear_elite` | 32.0% |
| `RA2SCUDELITE` | 31.1% |
| `BlackWidowPistols_elite` | 28.7% |
| `td_gdi_commando_sniper` | 28.7% |
| `LightSniper` | 28.7% |
| `CryoLightSniper` | 28.7% |
| `ChamBlade` | 28.7% |

_... and 22 more._
