# audit_k_linearity — the flat K must not move when Damage moves

Analysed **2039** concrete weapons.

## L0 — every positive offensive runtime percentage application is modeled

_clean_ — modeled 2195 folded and 2292 standalone applications.

## L1 — `k_flat` is invariant under a change of flat Damage

_clean_ — `k_flat` held to within 1e-09 across 3 scalings of every weapon.

## L2 — the scalable/absolute split decomposes the published `k`

`k == k_flat + (pct_absolute + folded_rounding) / damage_total`. The standalone term is a floor; the folded term is the current runtime residual, including Int32 wrap where present.

_clean_ — the identity holds for every analysed weapon; 11 percentage-only weapon(s) correctly have no flat-Damage denominator.

## L3 — weapons with a standalone percentage DPS floor

651 weapon(s) carry a standalone percentage hit; **57** have a floor at or above 25% of output.

A price target below the floor is UNREACHABLE by lowering flat Damage — `required_damage()` returns None rather than a wrong positive number. To price these lower, the standalone percentage hit has to shrink.

| weapon | floor as share of output |
|---|--:|
| `DTMutate` | 100.0% |
| `MADTankThump` | 100.0% |
| `TSShadowTeamBomb` | 100.0% |
| `RA2Mutate` | 100.0% |
| `C4` | 100.0% |
| `WormSwallow` | 100.0% |
| `d2k_chaos_lightning` | 100.0% |
| `D2KGomJabbar` | 100.0% |
| `d2k_aircraft_eater` | 100.0% |
| `TSTacticalMissile` | 100.0% |
| `TSTacticalChemMissile` | 100.0% |
| `TSTacticalMissileDamage` | 95.3% |
| `TSTacticalChemMissileDamage` | 95.3% |
| `TSHSeekerBomb` | 85.7% |
| `Spit` | 78.8% |
| `PlasBullet` | 74.0% |
| `bowFire_AA` | 72.7% |
| `bowFire` | 72.7% |
| `wc_tower_fire` | 72.3% |
| `Spit_AA` | 69.1% |
| `wc2lightshipFire` | 67.0% |
| `wc2submarineFire` | 66.7% |
| `wc2tornadoTest` | 66.7% |
| `wc2daemonFire` | 66.7% |
| `wc2heavyshipFire` | 66.5% |
| `d2k_quake_thump` | 65.8% |
| `d2k_quake_boom` | 65.8% |
| `TeslaArmorDischargeFragment2` | 62.5% |
| `MADTankDetonate` | 46.6% |
| `TeslaArmorDischargeFragment1` | 45.4% |

_... and 27 more._

## L4 — folded runtime residual (rounding or Int32 wrap)

43 weapon(s) have a non-zero current folded runtime residual.
This residual is included in measured output but excluded from `k_flat` and `dps_floor`; recompute it after snapping a proposed Damage value.

| weapon | context-adjusted residual per shot |
|---|--:|
| `d2kStormLasher` | -548372.8868 |
| `ExecutionerSword` | -196272.8356 |
| `OIHakureiring2` | -3474.2528 |
| `Hakureiring2` | -2683.8183 |
| `12MissilesSpawnerScud` | -1.5158 |
| `SteelStalkerRailgunEScatter` | -0.3485 |
| `HammerheadArtillery` | +0.2215 |
| `SteelAirTurretEScatter` | -0.1435 |
| `TSZoneRailgunRail` | +0.1178 |
| `MagicOrb` | -0.0928 |
| `ThermobaricNuclearMaverick` | -0.0797 |
| `MarineMG` | -0.0677 |
| `MadcapGun` | -0.0666 |
| `SteelQuantumCannonScatter_elite` | -0.0651 |
| `HarkonnenFlameTurret` | -0.0548 |
| `Lunar_GreenSturmArty` | +0.0433 |
| `NaxSturmArty` | +0.0433 |
| `ExplosiveDebris` | +0.0266 |
| `SandmarineTuskFire` | -0.0153 |
| `TSSBoatTusk` | +0.0143 |
| `oDevBullet` | +0.0130 |
| `o110mm_Gun` | +0.0104 |
| `o80mm_A` | +0.0095 |
| `o80mm_H` | +0.0095 |
| `o80mm_O` | +0.0095 |
| `ZeroFighterChainGunWaveforce` | -0.0057 |
| `tkm_rifleman_rifle` | +0.0047 |
| `RA28Inch` | +0.0030 |
| `o155mm` | +0.0026 |
| `ViperMissilesFire` | -0.0014 |
