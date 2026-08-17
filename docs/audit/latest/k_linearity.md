# audit_k_linearity — the flat K must not move when Damage moves

Analysed **2016** concrete weapons.

## L1 — `k_flat` is invariant under a change of flat Damage

_clean_ — `k_flat` held to within 1e-09 across 3 scalings of every weapon.

## L2 — the affine split decomposes the published `k`

`k == k_flat + pct_absolute / flat_total`, so nothing about the measured `effective_*` numbers changes — only the invertible form is new.

_clean_ — the identity holds for every analysed weapon.

## L3 — weapons with a %-twin DPS floor

1537 weapon(s) carry a %-twin; **52** have a floor at or above 25% of output.

A price target below the floor is UNREACHABLE by lowering flat Damage — `required_damage()` returns None rather than a wrong positive number. To price these lower, the TWIN has to shrink.

| weapon | floor as share of output |
|---|--:|
| `AnthraxCloudLarge` | 75.1% |
| `AnthraxCloudBlueLarge` | 73.0% |
| `AnthraxCloudPurpleLarge` | 71.4% |
| `D2K_StormGunInf` | 68.3% |
| `D2K_StormGunCymek` | 68.3% |
| `TSSmoke` | 54.3% |
| `RA2Cloud` | 54.3% |
| `RA2CloudSafe` | 54.3% |
| `AnthraxCloud` | 54.3% |
| `AnthraxCloudBlue` | 51.6% |
| `AnthraxCloudPurple` | 49.6% |
| `NodCommandoLaser` | 48.1% |
| `LightningBolt` | 45.2% |
| `CabalEliminatorGatling` | 41.7% |
| `Corsair_EMP` | 40.8% |
| `D2K_StormGun` | 37.7% |
| `d2kStormLasher` | 37.7% |
| `TSShadowTeamPistols` | 36.6% |
| `D2K_ShockGunInf` | 36.5% |
| `RA2KirovBomb_nuclear` | 35.5% |
| `d2k_munitions_explosion` | 33.3% |
| `ReimuDreamSeal` | 32.9% |
| `RA2KirovBomb_nuclear_elite` | 32.0% |
| `RA2SCUDELITE` | 31.0% |
| `td_gdi_commando_sniper` | 28.8% |
| `ra1_allies_alliedsniper` | 28.8% |
| `LightSniper` | 28.8% |
| `CommissarPistol` | 28.8% |
| `CryoLightSniper` | 28.8% |
| `ChamBlade` | 28.8% |

_... and 22 more._
