# R1 weapon-stat targets

**Read-only diagnostic.** The source damage coordinate (legacy armament-profile burst aggregate; no per-shot claim), reload, and burst are reported as separate reference targets. DPS is retained as an independent verifier; it is never decomposed into those inputs and no ledger or YAML value is written.

Rows with reference families: **348**; source rows scanned: **4367**.

The current reference rows do not expose a common `w_burst_delays` target. Therefore every composed DPS check is explicitly withheld rather than mixing a reference target with Cameo's live burst-delay value.

## Status counts

| status | rows |
|---|---:|
| `NO_REFERENCE` | 545 |
| `WITHHELD_MISSING_BURST_DELAYS` | 217 |
| `WITHHELD_MISSING_SEPARATE_INPUT` | 131 |

## Sample target rows

| actor | damage coordinate | reload | burst | DPS verifier | status |
|---|---:|---:|---:|---:|---|
| `asianalliance_alligator` | 35678.8 | 34.6139 | — | 813.638 | `WITHHELD_MISSING_SEPARATE_INPUT` |
| `asianalliance_asianflametank` | 25015.3 | 16.6407 | 2 | 963.372 | `WITHHELD_MISSING_BURST_DELAYS` |
| `asianalliance_asianflametrooper` | 29202.7 | 51.1935 | — | 397.404 | `WITHHELD_MISSING_SEPARATE_INPUT` |
| `asianalliance_asianmobileconstructionvehicle` | 55486.3 | 62.9106 | 1 | 701.177 | `WITHHELD_MISSING_BURST_DELAYS` |
| `asianalliance_asiansentryflamer` | 6389.01 | 44.0521 | — | 120.284 | `WITHHELD_MISSING_SEPARATE_INPUT` |
| `asianalliance_asiantankkiller` | 36547.7 | 75.6628 | 1 | 389.002 | `WITHHELD_MISSING_BURST_DELAYS` |
| `asianalliance_harbinger` | 53349.2 | 27.9662 | — | 1507.8 | `WITHHELD_MISSING_SEPARATE_INPUT` |
| `asianalliance_heavyrailguntank` | 56318.9 | 84.942 | — | 531.483 | `WITHHELD_MISSING_SEPARATE_INPUT` |
| `asianalliance_howitzer` | 68902.9 | 131.733 | — | 417.92 | `WITHHELD_MISSING_SEPARATE_INPUT` |
| `asianalliance_lynxtank` | 29888.4 | 55.1404 | 1 | 437.581 | `WITHHELD_MISSING_BURST_DELAYS` |
| `asianalliance_oiltruck` | 89562 | 69.324 | 1 | 952.273 | `WITHHELD_MISSING_BURST_DELAYS` |
| `asianalliance_phoenix` | 41685.9 | 38.9893 | 1 | 983.41 | `WITHHELD_MISSING_BURST_DELAYS` |
| `asianalliance_plasmatrooper` | 18270.6 | 26.5369 | — | 495.099 | `WITHHELD_MISSING_SEPARATE_INPUT` |
| `asianalliance_pulverizer` | 8780.3 | 14.1671 | — | 513.661 | `WITHHELD_MISSING_SEPARATE_INPUT` |
| `asianalliance_quasar` | 15724.8 | 23.7699 | — | 572.38 | `WITHHELD_MISSING_SEPARATE_INPUT` |
| `asianalliance_railguntank` | 13232.6 | 25.3381 | 1 | 525.527 | `WITHHELD_MISSING_BURST_DELAYS` |
| `asianalliance_railtower` | 32921.5 | 34.8661 | — | 850.851 | `WITHHELD_MISSING_SEPARATE_INPUT` |
| `asianalliance_viper` | 22320.1 | 38.687 | — | 481.728 | `WITHHELD_MISSING_SEPARATE_INPUT` |
| `atreides_combattank` | — | — | — | — | `WITHHELD_MISSING_SEPARATE_INPUT` |
| `atreides_missiletank` | — | — | — | — | `WITHHELD_MISSING_SEPARATE_INPUT` |
| `atreides_ornithopter` | 21929.3 | 58.5399 | 1 | 464.557 | `WITHHELD_MISSING_BURST_DELAYS` |
| `atreides_sonictank` | 5934.67 | 78.6339 | 1 | 70.134 | `WITHHELD_MISSING_BURST_DELAYS` |
| `cabal_artilleryspider` | 100024 | 86.2949 | — | 1023.93 | `WITHHELD_MISSING_SEPARATE_INPUT` |
| `cabal_ascended` | 46104.8 | 57.0014 | 2 | 614.493 | `WITHHELD_MISSING_BURST_DELAYS` |
| `cabal_cyborginfantry` | 16044.1 | 39.3038 | 3 | 436.844 | `WITHHELD_MISSING_BURST_DELAYS` |
| `cabal_cyborgreaper` | 25853.1 | 34.22 | 4 | 886.891 | `WITHHELD_MISSING_BURST_DELAYS` |
| `cabal_devout` | 11850.5 | 32.7959 | — | 275.682 | `WITHHELD_MISSING_SEPARATE_INPUT` |
| `cabal_hackercyborg` | 7065.71 | 18.0463 | — | 319.648 | `WITHHELD_MISSING_SEPARATE_INPUT` |
| `cabal_mantis` | 15369.2 | 55.7927 | 3 | 258.112 | `WITHHELD_MISSING_BURST_DELAYS` |
| `cabal_plasmaturret` | 27292.8 | 51.2075 | 5 | 985.149 | `WITHHELD_MISSING_BURST_DELAYS` |
| `cabal_radar_cruiser` | 6127.42 | 9.99992 | 2 | 447.1 | `WITHHELD_MISSING_BURST_DELAYS` |
| `cabal_ravager` | 27713.2 | 30.3522 | — | 782.741 | `WITHHELD_MISSING_SEPARATE_INPUT` |
| `cabal_rocketcyborg` | 23912.4 | 32.6809 | 3 | 505.651 | `WITHHELD_MISSING_BURST_DELAYS` |
| `cabal_scarabapc` | — | — | — | — | `WITHHELD_MISSING_SEPARATE_INPUT` |
| `cabal_spidercnc4` | — | — | — | — | `WITHHELD_MISSING_SEPARATE_INPUT` |
| `cabal_tiberiumharvester` | — | — | — | — | `WITHHELD_MISSING_SEPARATE_INPUT` |
| `forgotten_carryall` | — | — | — | — | `WITHHELD_MISSING_SEPARATE_INPUT` |
| `forgotten_flametank` | 46111.3 | 66.6674 | — | 615.989 | `WITHHELD_MISSING_SEPARATE_INPUT` |
| `forgotten_mlrs` | 44290.4 | 58.5703 | 8 | 1649.87 | `WITHHELD_MISSING_BURST_DELAYS` |
| `forgotten_mutant` | 13776.2 | 24.6691 | 1.5 | 413.562 | `WITHHELD_MISSING_BURST_DELAYS` |
