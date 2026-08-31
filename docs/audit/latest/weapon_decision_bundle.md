# Remaining reachable weapon decisions

This report compresses the honest unreviewed backlog into inheritance
families. It is a review queue, not conversion authority. Reviewed exact
composites remain in the raw structural count but are excluded here.
Three independent reviews found no mechanically exact fold in this
remaining set; each row requires an armor, geometry, targeting, state,
or progression decision before its live behavior can be changed.

- Raw reachable stacked definitions: **287**
- Exact reviewed composites: **210**
- Unreviewed reachable definitions: **77**
- Unreviewed inheritance families: **58**

The buckets describe why automatic consolidation is unsafe. They do not
decide the eventual damage family.
A bold family label is an unreviewed planning root; only the definitions
listed after the colon remain open decisions.

| decision bucket | families | definitions |
|---|---:|---:|
| target and state routing | 2 | 2 |
| target routing | 16 | 21 |
| state delivery | 12 | 19 |
| legacy compatibility | 1 | 1 |
| numbered warhead key | 1 | 1 |
| no special mechanical signal | 26 | 33 |

## Target And State Routing (2 families)

- **`RA160mmE_rad_elite`** (1; route mixed, state or integrity): `RA160mmE_rad_elite`
  - mains `Chemical_Light + Concussion_Medium + Demolition_Light + Nuclear_Super`: `RA160mmE_rad_elite`
- **`SandmarineTuskFire`** (1; route mixed, state or integrity): `SandmarineTuskFire`
  - mains `Concussion_Medium + Flame_Light + MissileAP_Light + MissileHE_Heavy`: `SandmarineTuskFire`

## Target Routing (16 families)

- **`SteelVulcan`** (4; route mixed): `SteelVulcan`, `SteelVulcanResonance`, `SteelVulcanResonanceBounce1`, `SteelVulcanResonanceBounce2`
  - mains `Bullet_Light + Bullet_Medium + CannonHE_Heavy`: `SteelVulcan`, `SteelVulcanResonance`, `SteelVulcanResonanceBounce1`, `SteelVulcanResonanceBounce2`
- **`ArmoredCarMG_AA`** (2; legacy bridge, route mixed): `ArmoredCarMGAAWaveforce`, `ArmoredCarMG_AA`
  - mains `ArmoredCarGroundCompatibility + Bullet_Medium`: `ArmoredCarMG_AA`
  - mains `ArmoredCarGroundCompatibility + Bullet_Medium + Railgun_Heavy`: `ArmoredCarMGAAWaveforce`
- **`GradRockets`** (2; legacy bridge, route mixed): `GradHeavyRockets`, `GradRockets`
  - mains `Concussion_Medium + MissileHE_Heavy`: `GradRockets`
  - mains `Concussion_Medium + MissileHE_Heavy + MissileHE_HeavyFlatCompatibility`: `GradHeavyRockets`
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

## State Delivery (12 families)

- **`NaxGrilleArty`** (4; state or integrity): `Lunar_GreenGrilleArty`, `Lunar_GreenGrilleArty_elite`, `NaxGrilleArty`, `NaxGrilleArty_elite`
  - mains `CannonHE_Heavy + Concussion_Medium + Demolition_Light`: `NaxGrilleArty`, `NaxGrilleArty_elite`
  - mains `CannonHE_Heavy + Concussion_Medium + Demolition_Light + Tesla_Heavy`: `Lunar_GreenGrilleArty`, `Lunar_GreenGrilleArty_elite`
- **`HammerTankCannon`** (2; state or integrity): `HammerTankCannon`, `HammerTankCannonThermobaric`
  - mains `CannonHE_Heavy + CannonHE_Medium`: `HammerTankCannon`
  - mains `CannonHE_Heavy + CannonHE_Medium + Demolition_Heavy + Flame_Medium`: `HammerTankCannonThermobaric`
- **`KotinCannon`** (2; state or integrity): `KotinCannon`, `KotinCannonThermobaric`
  - mains `CannonHE_Heavy + CannonHE_Medium`: `KotinCannon`
  - mains `CannonHE_Heavy + CannonHE_Medium + Demolition_Heavy + Flame_Medium`: `KotinCannonThermobaric`
- **`NaxSturmArty`** (2; state or integrity): `Lunar_GreenSturmArty`, `NaxSturmArty`
  - mains `CannonHE_Medium + Demolition_Heavy + Demolition_Light`: `NaxSturmArty`
  - mains `CannonHE_Medium + Demolition_Heavy + Demolition_Light + Tesla_Heavy`: `Lunar_GreenSturmArty`
- **`SkyHawkCannon`** (2; state or integrity): `SkyHawkCannon`, `SkyHawkPlasmaCannon`
  - mains `CannonAP_Light + Concussion_Medium`: `SkyHawkCannon`
  - mains `CannonAP_Light + Concussion_Medium + MissileAP_Medium + Tesla_Heavy`: `SkyHawkPlasmaCannon`
- **`ExplosiveDebris`** (1; state or integrity): `ExplosiveDebris`
  - mains `Demolition_Light + Flame_Light`: `ExplosiveDebris`
- **`GrenadeRA`** (1; state or integrity): `GrenadeRA`
  - mains `Demolition_Light + Flame_Light`: `GrenadeRA`
- **`LightTank2Missiles`** (1; state or integrity): `LightTank2Missiles`
  - mains `Flame_Light + MissileAP_Medium`: `LightTank2Missiles`
- **`SyndicateFireballLauncherExplode`** (1; legacy bridge, state or integrity): `SyndicateFireballLauncherExplode`
  - mains `PreservedFlat_Flame_Heavy + PreservedFlat_Flame_Light + PreservedFlat_Flame_Medium + PreservedFlat_HeavyFlameWeapon + PreservedFlat_LightFlameWeapon + PreservedFlat_MediumFlameWeapon`: `SyndicateFireballLauncherExplode`
- **`TSChem120mmx`** (1; legacy bridge, numbered, state or integrity): `TSChem120mmx`
  - mains `1Dam + CannonChem_HeavyFlatCompatibility + CannonHE_Medium`: `TSChem120mmx`
- **`TSSonicZapWeapon`** (1; state or integrity): `TSSonicZapWeapon`
  - mains `Magic_Heavy + Tesla_Heavy`: `TSSonicZapWeapon`
- **`Type97PlasmaCannon`** (1; state or integrity): `Type97PlasmaCannon`
  - mains `CannonHE_Heavy + Railgun_Heavy + Tesla_Heavy`: `Type97PlasmaCannon`

## Legacy Compatibility (1 family)

- **`facedancer_grenade`** (1; legacy bridge): `facedancer_grenade`
  - mains `CannonHE_Heavy + MissileAP_HeavyFlatCompatibility`: `facedancer_grenade`

## Numbered Warhead Key (1 family)

- **`TS120mmx`** (1; numbered): `TS120mmx`
  - mains `1Dam + CannonHE_Medium + Concussion_Medium`: `TS120mmx`

## No Special Mechanical Signal (26 families)

- **`LatinBuggyChaingun`** (2; none detected): `LatinBuggyChaingun`, `LatinBuggyChaingun_elite`
  - mains `Bullet_Light + Bullet_Medium + CannonAP_Light + Flak_Medium`: `LatinBuggyChaingun`, `LatinBuggyChaingun_elite`
- **`LatinBuggyRocket`** (2; none detected): `LatinBuggyRocket`, `LatinBuggyRocket_elite`
  - mains `Concussion_Medium + Demolition_Light + MissileAP_Light + MissileAP_Medium`: `LatinBuggyRocket`, `LatinBuggyRocket_elite`
- **`SCScourgeDroneExplosion`** (2; none detected): `SCScourgeDroneExplosion`, `ScourgeDroneExplosion`
  - mains `Concussion_Medium + Demolition_Heavy`: `SCScourgeDroneExplosion`, `ScourgeDroneExplosion`
- **`SCScourgeExplosion`** (2; air only): `SCScourgeExplosion`, `ScourgeExplosion`
  - mains `Concussion_Medium + Demolition_Heavy`: `SCScourgeExplosion`, `ScourgeExplosion`
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
