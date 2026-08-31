# Remaining reachable weapon decisions

This report compresses the honest unreviewed backlog into inheritance
families. It is a review queue, not conversion authority. Reviewed exact
composites remain in the raw structural count but are excluded here.

- Raw reachable stacked definitions: **289**
- Exact reviewed composites: **126**
- Unreviewed reachable definitions: **163**
- Unreviewed inheritance families: **106**

The buckets describe why automatic consolidation is unsafe. They do not
decide the eventual damage family.
A bold family label is an unreviewed planning root; only the definitions
listed after the colon remain open decisions.

| decision bucket | families | definitions |
|---|---:|---:|
| target and state routing | 18 | 39 |
| target routing | 24 | 36 |
| state delivery | 31 | 47 |
| legacy compatibility | 5 | 5 |
| numbered warhead key | 1 | 1 |
| no special mechanical signal | 27 | 35 |

## Target And State Routing (18 families)

- **`VolkovMagneticWeapon`** (10; legacy bridge, route mixed, state or integrity): `VolkovMagneticWeapon`, `VolkovMagneticWeaponIncendiary`, `VolkovMagneticWeaponIncendiaryNuclearShells`, `VolkovMagneticWeaponIncendiaryTesla`, `VolkovMagneticWeaponIncendiaryTeslaFragment1`, `VolkovMagneticWeaponIncendiaryTeslaFragment2`, `VolkovMagneticWeaponNuclearShells`, `VolkovMagneticWeaponTesla`, `VolkovMagneticWeaponTeslaFragment1`, `VolkovMagneticWeaponTeslaFragment2`
  - mains `CannonHE_Heavy + CannonNuke_HeavyFlatCompatibility + Grenade + MediumChemicalWeapon`: `VolkovMagneticWeaponIncendiaryNuclearShells`
  - mains `CannonHE_Heavy + Flame_Medium + Railgun_HeavyFlatCompatibility`: `VolkovMagneticWeaponIncendiary`
  - mains `CannonHE_Heavy + Grenade + MediumChemicalWeapon + Quantum_HeavyFlatCompatibility`: `VolkovMagneticWeaponIncendiaryTesla`
  - mains `CannonHE_Heavy + MediumFlameWeapon + Railgun_HeavyFlatCompatibility + Tesla_Heavy`: `VolkovMagneticWeaponIncendiaryTeslaFragment1`, `VolkovMagneticWeaponIncendiaryTeslaFragment2`
  - mains `CannonHE_Heavy + Nuclear_Super + Railgun_HeavyFlatCompatibility`: `VolkovMagneticWeaponNuclearShells`
  - mains `CannonHE_Heavy + Railgun_HeavyFlatCompatibility`: `VolkovMagneticWeapon`
  - mains `CannonHE_Heavy + Railgun_HeavyFlatCompatibility + Tesla_Heavy`: `VolkovMagneticWeaponTesla`, `VolkovMagneticWeaponTeslaFragment1`, `VolkovMagneticWeaponTeslaFragment2`
- **`RA2SCUD`** (7; legacy bridge, route mixed, state or integrity): `DredMissile`, `RA2SCUD`, `RA2SCUDELITE`, `RA2SCUD_fire`, `RA2SCUD_rad`, `RA2SCUD_tesla`, `V3Explode`
  - mains `Demolition_Light + MissileAP_Heavy + MissileChem_HeavyFlatCompatibility + RA2SCUDMissileAP_Heavy_NoWall`: `RA2SCUD_rad`
  - mains `Demolition_Light + MissileAP_Heavy + Nuclear_Super + RA2SCUDMissileAP_Heavy_NoWall`: `RA2SCUDELITE`
  - mains `Demolition_Light + MissileAP_Heavy + RA2SCUDMissileAP_Heavy_NoWall`: `DredMissile`, `RA2SCUD`, `RA2SCUD_fire`, `RA2SCUD_tesla`, `V3Explode`
- **`SCUD`** (3; route mixed, state or integrity): `SCUD`, `SCUDTesla`, `SCUDThermobaric`
  - mains `Demolition_Heavy + Flame_Heavy + HeavyFlameWeapon + HeavyMissile + MissileHE_Heavy`: `SCUDThermobaric`
  - mains `Flame_Heavy + HeavyFlameWeapon + HeavyMissile + MissileHE_Heavy + Tesla_Heavy`: `SCUDTesla`
  - mains `Flame_Heavy + MissileHE_Heavy`: `SCUD`
- **`AsianPhoenixRocket`** (2; route mixed, state or integrity): `AsianPhoenixRocket`, `AsianPhoenixRocket_elite`
  - mains `Demolition_Light + Flame_Medium + MissileAP_Heavy`: `AsianPhoenixRocket`, `AsianPhoenixRocket_elite`
- **`ConsortiumMissileSystem`** (2; legacy bridge, route mixed, state or integrity): `ConsortiumMissileSystem`, `ConsortiumMissileSystem_EMP`
  - mains `Flak_Medium + MissileAA_MediumFlatCompatibility + MissileAP_Medium`: `ConsortiumMissileSystem`
  - mains `Flak_Medium + MissileAP_Medium + MissileQuantum_MediumFlatCompatibility`: `ConsortiumMissileSystem_EMP`
- **`FutureHarbingerCannon`** (2; legacy bridge, route mixed, state or integrity): `FutureHarbingerCannon`, `FutureHarbingerCannon_elite`
  - mains `CannonHE_Heavy + Plasma_HeavyFlatCompatibility`: `FutureHarbingerCannon`, `FutureHarbingerCannon_elite`
- **`RA2Comet`** (2; route mixed, state or integrity): `RA2Comet`, `RA2Comet_elite`
  - mains `Demolition_Light + Flame_Medium + Laser_Heavy`: `RA2Comet`, `RA2Comet_elite`
- **`WaveTurretImpact`** (1; route mixed, state or integrity): `WaveTurretImpact`
  - mains `RailgunWeapon + Railgun_Heavy + Tesla_Heavy`: `WaveTurretImpact`
- **`JapanesePlasmaBomb`** (1; route mixed, state or integrity): `JapanesePlasmaBomb`
  - mains `Chemical_Heavy + Demolition_Heavy + Flame_Heavy`: `JapanesePlasmaBomb`
- **`MedicFlare`** (1; legacy bridge, route mixed, state or integrity): `MedicFlare`
  - mains `MediumChemicalWeapon + PreservedFlat_FlakWeapon + PreservedFlat_LaserWeapon + PreservedFlat_LightFlameWeapon`: `MedicFlare`
- **`RA160mmE_rad_elite`** (1; route mixed, state or integrity): `RA160mmE_rad_elite`
  - mains `Chemical_Light + Concussion_Medium + Demolition_Light + Nuclear_Super`: `RA160mmE_rad_elite`
- **`SandmarineTuskFire`** (1; route mixed, state or integrity): `SandmarineTuskFire`
  - mains `Concussion_Medium + Flame_Light + MissileAP_Light + MissileHE_Heavy`: `SandmarineTuskFire`
- **`TSLaserObeliskLaserFire`** (1; route mixed, state or integrity): `TSLaserObeliskLaserFire`
  - mains `CannonAP_Light + Laser_Heavy`: `TSLaserObeliskLaserFire`
- **`TSObeliskLaserFire`** (1; route mixed, state or integrity): `TSObeliskLaserFire`
  - mains `CannonAP_Light + Laser_Heavy`: `TSObeliskLaserFire`
- **`YakTeslaBomb`** (1; legacy bridge, route mixed, state or integrity): `YakTeslaBomb`
  - mains `PreservedFlat_HeavyBomb + PreservedFlat_HeavyChemicalWeapon + PreservedFlat_HeavyFlameWeapon + PreservedFlat_TeslaWeapon`: `YakTeslaBomb`
- **`tkmfirerockets`** (1; route mixed, state or integrity): `tkmfirerockets`
  - mains `Flame_Light + MissileAP_Light`: `tkmfirerockets`
- **`tkmkatyushalalauncherrocketsfire`** (1; route mixed, state or integrity): `tkmkatyushalalauncherrocketsfire`
  - mains `Concussion_Medium + Flame_Light + MissileAP_Light`: `tkmkatyushalalauncherrocketsfire`
- **`tkmstrykerfirerockets`** (1; route mixed, state or integrity): `tkmstrykerfirerockets`
  - mains `Flame_Medium + MissileAP_Medium`: `tkmstrykerfirerockets`

## Target Routing (24 families)

- **`SteelVulcan`** (4; route mixed): `SteelVulcan`, `SteelVulcanResonance`, `SteelVulcanResonanceBounce1`, `SteelVulcanResonanceBounce2`
  - mains `Bullet_Light + Bullet_Medium + CannonHE_Heavy`: `SteelVulcan`, `SteelVulcanResonance`, `SteelVulcanResonanceBounce1`, `SteelVulcanResonanceBounce2`
- **`RA2Virusgun3`** (2; legacy bridge, route mixed): `RA2Virusgun3`, `RA2Virusgun_elite`
  - mains `Flak_Medium + MissileAP_Medium + Sniper_LightFlatCompatibility`: `RA2Virusgun3`, `RA2Virusgun_elite`
- **`AAGunBoatFlak`** (2; legacy bridge, route mixed): `AAGunBoatFlak`, `AAGunBoatFlak_elite`
  - mains `Flak_Medium + Flak_MediumFlatCompatibility`: `AAGunBoatFlak`, `AAGunBoatFlak_elite`
- **`ArmoredCarMG_AA`** (2; legacy bridge, route mixed): `ArmoredCarMGAAWaveforce`, `ArmoredCarMG_AA`
  - mains `ArmoredCarGroundCompatibility + Bullet_Medium`: `ArmoredCarMG_AA`
  - mains `ArmoredCarGroundCompatibility + Bullet_Medium + Railgun_Heavy`: `ArmoredCarMGAAWaveforce`
- **`FutureTankCannons`** (2; legacy bridge, route mixed): `FutureTankCannons`, `FutureTankCannons_elite`
  - mains `CannonHE_Heavy + CannonHE_HeavyFlatCompatibility`: `FutureTankCannons`, `FutureTankCannons_elite`
- **`GradRockets`** (2; legacy bridge, route mixed): `GradHeavyRockets`, `GradRockets`
  - mains `Concussion_Medium + MissileHE_Heavy`: `GradRockets`
  - mains `Concussion_Medium + MissileHE_Heavy + MissileHE_HeavyFlatCompatibility`: `GradHeavyRockets`
- **`JapanMaidenBowEnergized`** (2; legacy bridge, route mixed): `BallistaSingleShotAirEnergized`, `JapanMaidenBowEnergized`
  - mains `Arrow_Light + Arrow_LightFlatCompatibility + CannonHE_Medium`: `JapanMaidenBowEnergized`
  - mains `Arrow_Light + Arrow_LightFlatCompatibility + CannonHE_Medium + MissileAP_Light`: `BallistaSingleShotAirEnergized`
- **`ShotgunAttackRobotGun`** (2; legacy bridge, route mixed): `ShotgunAttackRobotGun`, `ShotgunAttackRobotGun_elite`
  - mains `Bullet_LightFlatCompatibility + Bullet_Medium + CannonHE_Medium`: `ShotgunAttackRobotGun`, `ShotgunAttackRobotGun_elite`
- **`SkyHawkChainGun`** (2; route mixed): `SkyHawkChainGun`, `SkyHawkChainGunWaveforce`
  - mains `Bullet_Medium + Demolition_Light`: `SkyHawkChainGun`
  - mains `Bullet_Medium + Demolition_Light + Railgun_Heavy`: `SkyHawkChainGunWaveforce`
- **`japan_imperialscoutsman_rifle`** (2; legacy bridge, route mixed): `japan_imperialscoutsman_rifle`, `japan_imperialscoutsman_rifle_waveforce`
  - mains `Bullet_Medium + RailgunCompatibility + RailgunShieldCompatibility`: `japan_imperialscoutsman_rifle`
  - mains `Bullet_Medium + RailgunCompatibility + RailgunShieldCompatibility + Railgun_Heavy`: `japan_imperialscoutsman_rifle_waveforce`
- **`CabalHeavyReaperMissiles`** (1; route mixed): `CabalHeavyReaperMissiles`
  - mains `Concussion_Medium + Demolition_Light + MissileHE_Heavy + MissileHE_Medium`: `CabalHeavyReaperMissiles`
- **`CabalReaperMissiles`** (1; route mixed): `CabalReaperMissiles`
  - mains `Concussion_Medium + Demolition_Light + MissileHE_Light + MissileHE_Medium`: `CabalReaperMissiles`
- **`Future_Cryocopter_Rocket`** (1; route mixed): `Future_Cryocopter_Rocket`
  - mains `FutureCryocopterMissileAP_Medium + MissileAP_Heavy + MissileAP_Medium`: `Future_Cryocopter_Rocket`
- **`GLBarrelExplode`** (1; numbered, route mixed): `GLBarrelExplode`
  - mains `1Dam + Concussion_Medium + Demolition_Heavy`: `GLBarrelExplode`
- **`GuardianShoot`** (1; route mixed): `GuardianShoot`
  - mains `Concussion_Light + Concussion_Medium`: `GuardianShoot`
- **`HMG_fremen`** (1; numbered, route mixed): `HMG_fremen`
  - mains `1Dam + Bullet_Light + Bullet_Medium`: `HMG_fremen`
- **`NaxCorrosionRocketTrooper_elite`** (1; legacy bridge, route mixed): `NaxCorrosionRocketTrooper_elite`
  - mains `PreservedFlat_Concussion_Light + PreservedFlat_HeavyMissile + PreservedFlat_MissileAP_Heavy + PreservedFlat_MissileAP_Medium`: `NaxCorrosionRocketTrooper_elite`
- **`RashidanGun_upgrade`** (1; legacy bridge, route mixed): `RashidanGun_upgrade`
  - mains `Bullet_Light + Bullet_Medium + CannonHE_Heavy + MissileHE_Heavy + RashidanGroundCompatibility`: `RashidanGun_upgrade`
- **`SamuraiBladeCharged`** (1; legacy bridge, route mixed): `SamuraiBladeCharged`
  - mains `PreservedFlat_SwordWeapon + PreservedFlat_TeslaWeapon`: `SamuraiBladeCharged`
- **`SteelHoverMissile_elite`** (1; route mixed): `SteelHoverMissile_elite`
  - mains `ArrowWeapon + MissileAP_Light`: `SteelHoverMissile_elite`
- **`TS30mmRail`** (1; legacy bridge, route mixed): `TS30mmRail`
  - mains `Flak_Medium + TS30mmRailUnscopedCompatibility`: `TS30mmRail`
- **`TSAegisMissile`** (1; numbered, route mixed): `TSAegisMissile`
  - mains `1Dam + LightMissile + MediumMissile`: `TSAegisMissile`
- **`Tentacle`** (1; legacy bridge, route mixed): `Tentacle`
  - mains `CannonHE_Heavy + Melee_HeavyFlatCompatibility`: `Tentacle`
- **`v1rockets`** (1; route mixed): `v1rockets`
  - mains `Demolition_Light + MissileHE_Medium`: `v1rockets`

## State Delivery (31 families)

- **`NaxGrilleArty`** (4; state or integrity): `Lunar_GreenGrilleArty`, `Lunar_GreenGrilleArty_elite`, `NaxGrilleArty`, `NaxGrilleArty_elite`
  - mains `CannonHE_Heavy + Concussion_Medium + Demolition_Light`: `NaxGrilleArty`, `NaxGrilleArty_elite`
  - mains `CannonHE_Heavy + Concussion_Medium + Demolition_Light + Tesla_Heavy`: `Lunar_GreenGrilleArty`, `Lunar_GreenGrilleArty_elite`
- **`wc2deathknightFire`** (4; state or integrity): `wc2deathknightDeathCoil`, `wc2deathknightDeathCoilScatter_Left`, `wc2deathknightDeathCoilScatter_Right`, `wc2deathknightFire`
  - mains `Flame_Heavy + Tesla_Super`: `wc2deathknightDeathCoil`, `wc2deathknightDeathCoilScatter_Left`, `wc2deathknightDeathCoilScatter_Right`, `wc2deathknightFire`
- **`RA2Robotmm`** (3; state or integrity): `RA2Robotmm`, `RA2RobotmmScatter_elite`, `RA2Robotmm_elite`
  - mains `Laser_Heavy + Railgun_Heavy + Tesla_Heavy`: `RA2Robotmm`, `RA2RobotmmScatter_elite`, `RA2Robotmm_elite`
- **`BCLaser`** (2; legacy bridge, state or integrity): `BCLaser`, `BCYamatoCannon`
  - mains `CannonHE_Heavy + Laser_HeavyFlatCompatibility`: `BCLaser`
  - mains `CannonHE_Heavy + Plasma_HeavyFlatCompatibility`: `BCYamatoCannon`
- **`HammerTankCannon`** (2; state or integrity): `HammerTankCannon`, `HammerTankCannonThermobaric`
  - mains `CannonHE_Heavy + CannonHE_Medium`: `HammerTankCannon`
  - mains `CannonHE_Heavy + CannonHE_Medium + Demolition_Heavy + Flame_Medium`: `HammerTankCannonThermobaric`
- **`KotinCannon`** (2; state or integrity): `KotinCannon`, `KotinCannonThermobaric`
  - mains `CannonHE_Heavy + CannonHE_Medium`: `KotinCannon`
  - mains `CannonHE_Heavy + CannonHE_Medium + Demolition_Heavy + Flame_Medium`: `KotinCannonThermobaric`
- **`NaxSturmArty`** (2; state or integrity): `Lunar_GreenSturmArty`, `NaxSturmArty`
  - mains `CannonHE_Medium + Demolition_Heavy + Demolition_Light`: `NaxSturmArty`
  - mains `CannonHE_Medium + Demolition_Heavy + Demolition_Light + Tesla_Heavy`: `Lunar_GreenSturmArty`
- **`RA2120xmm_rad`** (2; legacy bridge, state or integrity): `RA2120xmm_rad`, `RA2120xmm_rad_elite`
  - mains `CannonAP_Light + CannonChem_HeavyFlatCompatibility + CannonHE_Heavy`: `RA2120xmm_rad`, `RA2120xmm_rad_elite`
- **`SkyHawkCannon`** (2; state or integrity): `SkyHawkCannon`, `SkyHawkPlasmaCannon`
  - mains `CannonAP_Light + Concussion_Medium`: `SkyHawkCannon`
  - mains `CannonAP_Light + Concussion_Medium + MissileAP_Medium + Tesla_Heavy`: `SkyHawkPlasmaCannon`
- **`TTankZap2ArcTeslaFragment1_EMP`** (2; state or integrity): `TTankZap2ArcTeslaFragment1_EMP`, `TTankZap2ArcTeslaFragment2_EMP`
  - mains `TeslaWeapon + Tesla_Super`: `TTankZap2ArcTeslaFragment1_EMP`, `TTankZap2ArcTeslaFragment2_EMP`
- **`plymouthStickyTiger`** (2; legacy bridge, state or integrity): `plymouthStickyDefence`, `plymouthStickyTiger`
  - mains `CannonHE_Heavy + CannonHE_Medium + Chemical_Light + StickyWildcardCompatibility`: `plymouthStickyDefence`
  - mains `CannonHE_Medium + Chemical_Light + StickyWildcardCompatibility`: `plymouthStickyTiger`
- **`ExplosiveDebris`** (1; state or integrity): `ExplosiveDebris`
  - mains `Demolition_Light + Flame_Light`: `ExplosiveDebris`
- **`GrenadeRA`** (1; state or integrity): `GrenadeRA`
  - mains `Demolition_Light + Flame_Light`: `GrenadeRA`
- **`HMGo_upgrade`** (1; state or integrity): `HMGo_upgrade`
  - mains `Bullet_Light + Bullet_Medium + Laser_Heavy`: `HMGo_upgrade`
- **`HMGstealth_upgrade`** (1; legacy bridge, state or integrity): `HMGstealth_upgrade`
  - mains `Bullet_MediumFlatCompatibility + Laser_Heavy`: `HMGstealth_upgrade`
- **`Laboratory_Bioball`** (1; state or integrity): `Laboratory_Bioball`
  - mains `CannonHE_Heavy + Chemical_Medium + Concussion_Medium + Demolition_Light`: `Laboratory_Bioball`
- **`LightTank2Missiles`** (1; state or integrity): `LightTank2Missiles`
  - mains `Flame_Light + MissileAP_Medium`: `LightTank2Missiles`
- **`Lunar_GreenTigerCannon`** (1; state or integrity): `Lunar_GreenTigerCannon`
  - mains `CannonHE_Medium + Tesla_Heavy`: `Lunar_GreenTigerCannon`
- **`Lunar_GreenTigerCannon_elite`** (1; state or integrity): `Lunar_GreenTigerCannon_elite`
  - mains `CannonHE_Medium + Tesla_Heavy`: `Lunar_GreenTigerCannon_elite`
- **`OIBigPlasmaCannon`** (1; state or integrity): `OIBigPlasmaCannon`
  - mains `CannonHE_Heavy + Railgun_Heavy + Tesla_Heavy`: `OIBigPlasmaCannon`
- **`PositronBounce1`** (1; legacy bridge, state or integrity): `PositronBounce1`
  - mains `CannonAP_Light + CannonHE_Medium + Quantum_MediumFlatCompatibility`: `PositronBounce1`
- **`PositronBounce2`** (1; legacy bridge, state or integrity): `PositronBounce2`
  - mains `CannonAP_Light + CannonHE_Medium + Quantum_MediumFlatCompatibility`: `PositronBounce2`
- **`RA2DiskDrain`** (1; state or integrity): `RA2DiskDrain`
  - mains `Magic_Heavy + Tesla_Heavy`: `RA2DiskDrain`
- **`SteelMegaSword_elite`** (1; legacy bridge, state or integrity): `SteelMegaSword_elite`
  - mains `Quantum_HeavyFlatCompatibility + Railgun_Heavy`: `SteelMegaSword_elite`
- **`SyndicateFireballLauncherExplode`** (1; legacy bridge, state or integrity): `SyndicateFireballLauncherExplode`
  - mains `PreservedFlat_Flame_Heavy + PreservedFlat_Flame_Light + PreservedFlat_Flame_Medium + PreservedFlat_HeavyFlameWeapon + PreservedFlat_LightFlameWeapon + PreservedFlat_MediumFlameWeapon`: `SyndicateFireballLauncherExplode`
- **`TSChem120mmx`** (1; legacy bridge, numbered, state or integrity): `TSChem120mmx`
  - mains `1Dam + CannonChem_HeavyFlatCompatibility + CannonHE_Medium`: `TSChem120mmx`
- **`TSSonicZapWeapon`** (1; state or integrity): `TSSonicZapWeapon`
  - mains `Magic_Heavy + Tesla_Heavy`: `TSSonicZapWeapon`
- **`Type97PlasmaCannon`** (1; state or integrity): `Type97PlasmaCannon`
  - mains `CannonHE_Heavy + Railgun_Heavy + Tesla_Heavy`: `Type97PlasmaCannon`
- **`ViperMissilesFire`** (1; state or integrity): `ViperMissilesFire`
  - mains `Concussion_Medium + Flame_Light + MissileAP_Light + MissileAP_Medium`: `ViperMissilesFire`
- **`WaveforceCannonDistortedBeam2`** (1; state or integrity): `WaveforceCannonDistortedBeam2`
  - mains `Chemical_Heavy + Flame_Heavy`: `WaveforceCannonDistortedBeam2`
- **`edenMobileLaserTiger`** (1; state or integrity): `edenMobileLaserTiger`
  - mains `CannonHE_Medium + Laser_Heavy`: `edenMobileLaserTiger`

## Legacy Compatibility (5 families)

- **`RA2FreedomRocket_elite`** (1; legacy bridge): `RA2FreedomRocket_elite`
  - mains `MissileAP_Medium + MissileAP_MediumFlatCompatibility`: `RA2FreedomRocket_elite`
- **`Rammax_Sabot`** (1; legacy bridge): `Rammax_Sabot`
  - mains `PreservedFlat_Chaingun + PreservedFlat_LaserWeapon + PreservedFlat_TeslaWeapon`: `Rammax_Sabot`
- **`TSRPGTowerRail`** (1; legacy bridge): `TSRPGTowerRail`
  - mains `CannonHE_Heavy + Railgun_HeavyFlatCompatibility`: `TSRPGTowerRail`
- **`TankBusterBeamCannon`** (1; legacy bridge): `TankBusterBeamCannon`
  - mains `Railgun_Heavy + TankBusterBeamUnscopedCompatibility`: `TankBusterBeamCannon`
- **`facedancer_grenade`** (1; legacy bridge): `facedancer_grenade`
  - mains `CannonHE_Heavy + MissileAP_Heavy + MissileAP_HeavyFlatCompatibility`: `facedancer_grenade`

## Numbered Warhead Key (1 family)

- **`TS120mmx`** (1; numbered): `TS120mmx`
  - mains `1Dam + CannonHE_Medium + Concussion_Medium`: `TS120mmx`

## No Special Mechanical Signal (27 families)

- **`LatinBuggyChaingun`** (2; none detected): `LatinBuggyChaingun`, `LatinBuggyChaingun_elite`
  - mains `Bullet_Light + Bullet_Medium + CannonAP_Light + Flak_Medium`: `LatinBuggyChaingun`, `LatinBuggyChaingun_elite`
- **`LatinBuggyRocket`** (2; none detected): `LatinBuggyRocket`, `LatinBuggyRocket_elite`
  - mains `Concussion_Medium + Demolition_Light + MissileAP_Light + MissileAP_Medium`: `LatinBuggyRocket`, `LatinBuggyRocket_elite`
- **`SCScourgeDroneExplosion`** (2; none detected): `SCScourgeDroneExplosion`, `ScourgeDroneExplosion`
  - mains `Concussion_Medium + Demolition_Heavy`: `SCScourgeDroneExplosion`, `ScourgeDroneExplosion`
- **`SCScourgeExplosion`** (2; air only): `SCScourgeExplosion`, `ScourgeExplosion`
  - mains `Concussion_Medium + Demolition_Heavy`: `SCScourgeExplosion`, `ScourgeExplosion`
- **`TSAdatsMissile`** (2; none detected): `TSAdatsMissile`, `TSAdatsMissile_AA`
  - mains `Flak_Medium + MissileHE_Light`: `TSAdatsMissile`, `TSAdatsMissile_AA`
- **`TSTacticalMissileDamage`** (2; none detected): `TSTacticalChemMissileDamage`, `TSTacticalMissileDamage`
  - mains `LightMissile + MediumMissile`: `TSTacticalChemMissileDamage`, `TSTacticalMissileDamage`
- **`d2k_air_drone_guns`** (2; none detected): `d2k_air_drone_guns`, `d2k_air_drone_guns_upgrade`
  - mains `Bullet_Light + Bullet_Medium + CannonHE_Heavy + MissileAP_Heavy`: `d2k_air_drone_guns_upgrade`
  - mains `Bullet_Light + Bullet_Medium + MissileAP_Heavy`: `d2k_air_drone_guns`
- **`tkmjuggap`** (2; none detected): `tkmjuggap`, `tkmtechnicalmgap`
  - mains `Bullet_Light + Demolition_Light`: `tkmjuggap`, `tkmtechnicalmgap`
- **`110mm_Gun`** (1; none detected): `110mm_Gun`
  - mains `CannonAP_Light + CannonHE_Heavy + CannonHE_Medium`: `110mm_Gun`
- **`AlliedTankDestroyerCannon`** (1; none detected): `AlliedTankDestroyerCannon`
  - mains `CannonAP_Light + CannonHE_Medium`: `AlliedTankDestroyerCannon`
- **`Aphid_AA`** (1; none detected): `Aphid_AA`
  - mains `Concussion_Medium + MissileHE_Heavy`: `Aphid_AA`
- **`GlaveCanon`** (1; none detected): `GlaveCanon`
  - mains `Demolition_Light + Railgun_Heavy`: `GlaveCanon`
- **`JimRaynorMachineGun`** (1; none detected): `JimRaynorMachineGun`
  - mains `CannonHE_Heavy + MissileHE_Heavy`: `JimRaynorMachineGun`
- **`RA2Terrorist`** (1; none detected): `RA2Terrorist`
  - mains `Concussion_Medium + Demolition_Heavy`: `RA2Terrorist`
- **`SandmarineTuskTwin`** (1; none detected): `SandmarineTuskTwin`
  - mains `Bullet_Medium + Concussion_Medium + Grenade + MissileAP_Medium + MissileHE_Heavy`: `SandmarineTuskTwin`
- **`ScoutMG`** (1; none detected): `ScoutMG`
  - mains `Demolition_Light + Flak_Medium`: `ScoutMG`
- **`SheridanCannon`** (1; none detected): `SheridanCannon`
  - mains `CannonAP_Light + CannonHE_Medium`: `SheridanCannon`
- **`SheridanMissiles`** (1; none detected): `SheridanMissiles`
  - mains `MissileHE_Light + MissileHE_Medium`: `SheridanMissiles`
- **`SiegeTankCannon`** (1; none detected): `SiegeTankCannon`
  - mains `CannonAP_Light + CannonHE_Heavy + CannonHE_Medium`: `SiegeTankCannon`
- **`TSBoatcannon`** (1; none detected): `TSBoatcannon`
  - mains `Concussion_Medium + Demolition_Heavy`: `TSBoatcannon`
- **`TSBomb`** (1; none detected): `TSBomb`
  - mains `Concussion_Medium + Demolition_Heavy`: `TSBomb`
- **`TigerCannon`** (1; none detected): `TigerCannon`
  - mains `CannonHE_Heavy + CannonHE_Medium`: `TigerCannon`
- **`Type97Cannon`** (1; none detected): `Type97Cannon`
  - mains `CannonHE_Heavy + CannonHE_Medium`: `Type97Cannon`
- **`YakovlevCannon`** (1; none detected): `YakovlevCannon`
  - mains `Bullet_Medium + CannonAP_Light + CannonHE_Heavy + Flak_Medium`: `YakovlevCannon`
- **`YakovlevCannon_elite`** (1; none detected): `YakovlevCannon_elite`
  - mains `Bullet_Medium + CannonAP_Light + CannonHE_Heavy + Flak_Medium`: `YakovlevCannon_elite`
- **`ordos_autogunturret`** (1; none detected): `ordos_autogunturret`
  - mains `Bullet_Light + Bullet_Medium + CannonHE_Heavy`: `ordos_autogunturret`
- **`t30shell`** (1; none detected): `t30shell`
  - mains `Demolition_Heavy + Railgun_Heavy`: `t30shell`

## Maintainer decision shape

For each family, the eventual question is: which authored main defines the unit's role, and may its armor, splash, target route, and state delivery be applied to the full nominal damage? Paid replacements and mixed target routes must be reviewed as complete closures.
