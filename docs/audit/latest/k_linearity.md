# audit_k_linearity — the flat K must not move when Damage moves

Analysed **2148** concrete weapons.

## L0 — every positive offensive runtime percentage application is modeled

_clean_ — modeled 1701 folded and 2523 standalone applications.

## L1 — `k_flat` is invariant under a change of flat Damage

_clean_ — `k_flat` held to within 1e-09 across 3 scalings of every weapon.

## L2 — the scalable/absolute split decomposes the published `k`

`k == k_flat + (pct_absolute + folded_rounding) / damage_total`. The standalone term is a floor; the folded term is the current runtime quantisation residual.

_clean_ — the identity holds for every analysed weapon; 11 percentage-only weapon(s) correctly have no flat-Damage denominator.

## L3 — weapons with a standalone percentage DPS floor

703 weapon(s) carry a standalone percentage hit; **190** have a floor at or above 25% of output.

A price target below the floor is UNREACHABLE by lowering flat Damage — `required_damage()` returns None rather than a wrong positive number. To price these lower, the standalone percentage hit has to shrink.

| weapon | floor as share of output |
|---|--:|
| `DTMutate` | 100.0% |
| `MADTankThump` | 100.0% |
| `TSShadowTeamBomb` | 100.0% |
| `RA2Mutate` | 100.0% |
| `WormSwallow` | 100.0% |
| `D2KGomJabbar` | 100.0% |
| `d2k_aircraft_eater` | 100.0% |
| `C4` | 100.0% |
| `d2k_chaos_lightning` | 100.0% |
| `TSTacticalMissile` | 100.0% |
| `TSTacticalChemMissile` | 100.0% |
| `BlackEagleThunderboltMissiles_elite` | 97.7% |
| `BlackEagleThunderboltMissiles` | 97.7% |
| `TSTacticalMissileDamage` | 95.4% |
| `TSTacticalChemMissileDamage` | 95.4% |
| `NaxiMissileUboat` | 93.3% |
| `BlackEagleMissiles` | 93.0% |
| `BlackEagleMissiles_elite` | 93.0% |
| `RocketAngelRockets` | 88.7% |
| `PhobosLaser` | 87.1% |
| `TSHSeekerBomb` | 85.7% |
| `LunarNaxiDroneMissile` | 85.3% |
| `120mm_python_deploy` | 84.5% |
| `SCTyrAA` | 84.2% |
| `D2K_RocketsCymek` | 84.2% |
| `HarrierMissiles_elite` | 84.0% |
| `120mm_cobra_deploy` | 83.6% |
| `BallistaMultiShotEnergized` | 83.1% |
| `BallistaTowerMultiShotEnergized` | 82.5% |
| `SteelTwisterMissiles_elite` | 81.4% |

_... and 160 more._

## L4 — folded runtime quantisation residual

630 weapon(s) have a non-zero current folded runtime residual.
This residual is included in measured output but excluded from `k_flat` and `dps_floor`; recompute it after snapping a proposed Damage value.

| weapon | context-adjusted residual per shot |
|---|--:|
| `AsianTurretPlasma` | +2.3923 |
| `AsianTwinPlasma_elite` | +2.1408 |
| `AsianTwinPlasma` | +2.0596 |
| `Tentacle` | +2.0029 |
| `FutureMechPlasma_elite` | +1.9248 |
| `ra1_soviets_migattackbomber_thermobaricmaverick` | +1.9223 |
| `AsianSinglePlasma_elite` | +1.8970 |
| `FutureMechPlasma` | +1.8539 |
| `AsianSinglePlasma` | +1.8512 |
| `ra1_soviets_btr80_machinegun_tesla` | +1.7744 |
| `ra1_soviets_btr80_machinegun_tesla_arc` | +1.7744 |
| `edenMobileDefenceLaser` | +1.7576 |
| `Napalm` | +1.7426 |
| `CabalMantisGun` | +1.7167 |
| `RA2LasherLaser` | +1.7156 |
| `AsianChemicalBombs` | +1.6987 |
| `ra1_allies_alliedgunturret_cannon` | +1.6789 |
| `TSTurretLaser` | +1.6582 |
| `TSCABALPlasmaFire` | +1.6582 |
| `d2kChainGun_upgrade` | +1.6548 |
| `NapalmA10Carrier` | +1.6546 |
| `schwarzermond_lunarsoldier_rifle_yellow` | +1.6421 |
| `schwarzermond_lunarsoldier_rifle_amplified` | +1.6421 |
| `TSLaserTurretLaser` | +1.6250 |
| `Lunar_YellowUbermenschLaser_elite` | +1.6125 |
| `Lunar_AmplifiedUbermenschLaser_elite` | +1.6125 |
| `schwarzermond_lunarsoldier_rifle_yellow_elite` | +1.6076 |
| `schwarzermond_lunarsoldier_rifle_amplified_elite` | +1.6076 |
| `TSScoopDualTur` | +1.5869 |
| `JHighVWaveforce` | +1.5740 |
