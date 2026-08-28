# Remaining Weapon Classification

This report is read-only triage. A suggested destination is not approval to edit YAML;
each group still needs a proposed resolved diff and the full behavior comparator.

Active concrete roots still using retired flat families: **99**.

| review bucket | roots | meaning |
|---|--:|---|
| one inherited destination | 0 | one family and tier appears in the actual inheritance chain without conflicting evidence |
| corroborated suggestion | 0 | weapon-name and weighted legacy evidence agree |
| legacy-only suggestion | 3 | one weighted legacy signal exists, but the name does not confirm it |
| human decision required | 96 | conflicting, exceptional, or missing destination evidence |

The machine-readable JSON includes every flat hit's targets, exclusions, relationships,
score flag, friendly-fire modifiers, physical-state bindings, full percentage hits, descendant
closure, and descendant overrides of retired flat keys.

## One Inherited Destination (0)

| weapon | proposed family | retired families | descendants | old-key child overrides | evidence |
|---|---|---|--:|--:|---|

## Corroborated Suggestion (0)

| weapon | proposed family | retired families | descendants | old-key child overrides | evidence |
|---|---|---|--:|--:|---|

## Legacy-Only Suggestion (3)

| weapon | proposed family | retired families | descendants | old-key child overrides | evidence |
|---|---|---|--:|--:|---|
| `AsianMaidenBow` | Concussion | ArrowWeapon, Grenade | 1 | 1 | legacy score Concussion=2 |
| `SamuraiBladeCharged` | Tesla | SwordWeapon, TeslaWeapon | 0 | 0 | legacy score Tesla=4 |
| `SteelAirTurret` | Laser | LaserWeapon, RailgunWeapon | 3 | 3 | legacy score Laser=4 |

## Human Decision Required (96)

| weapon | proposed family | retired families | descendants | old-key child overrides | evidence |
|---|---|---|--:|--:|---|
| `AsianChemical` | Demolition_Light | HeavyBomb, HeavyChemicalWeapon, LightChemicalWeapon, MediumChemicalWeapon, ShrapnelWeapon | 1 | 0 | name and canonical destination disagree; canonical and legacy signals disagree |
| `AsianMLRS` | ? | FlakWeapon, HeavyMissile | 1 | 1 | multiple inherited family/tier destinations; canonical and legacy signals disagree |
| `AsianPhotonCannon` | ? | FlakWeapon, MagicWeapon, MediumMissile, TeslaWeapon | 13 | 6 | exception-bearing retired family; legacy tie: Magic/Tesla |
| `AsianSinglePlasma` | ? | HeavyBomb, LightChemicalWeapon, LightFlameWeapon, MediumChemicalWeapon, MediumFlameWeapon, ShrapnelWeapon | 4 | 0 | exception-bearing retired family; multiple inherited family/tier destinations; legacy tie: Chemical/Flame; name and canonical destination disagree |
| `AthenaLaser` | ? | Chaingun, FlakWeapon, LaserWeapon, LightMissile, SmallArms, TeslaWeapon | 0 | 0 | name and legacy signals disagree |
| `BCLaser` | CannonHE_Heavy | HeavyAAWeapon, HeavyBomb, HeavyChemicalWeapon, HeavyFlameWeapon, HeavyMissile, LaserWeapon, NuclearWarhead, RailgunWeapon | 1 | 1 | exception-bearing retired family; name and canonical destination disagree; canonical and legacy signals disagree; name and legacy signals disagree |
| `BallistaMultiShotEnergized` | ? | ArrowWeapon, MediumChemicalWeapon, MediumFlameWeapon, TeslaWeapon | 1 | 0 | legacy tie: Chemical/Flame/Tesla |
| `BoxerCannonAG` | ? | Chaingun, FlakWeapon, Grenade, LightMissile, MediumCannon, SmallArms | 1 | 1 | name and legacy signals disagree |
| `BuggyPlasmaGrenade` | Demolition_Light | HeavyBomb, ShrapnelWeapon | 0 | 0 | legacy tie: Demolition/Concussion; name and canonical destination disagree |
| `CabalArtilleryWalkerShellUpgraded` | ? | Grenade, HeavyCannon, MagicWeapon, MediumCannon, MediumChemicalWeapon, RailgunWeapon, ShrapnelWeapon, TeslaChargedWeapon | 0 | 0 | exception-bearing retired family; legacy tie: Concussion/CannonHE/Magic/Chemical/Tesla |
| `CabalBeholderLaser` | ? | HeavyCannon, LaserWeapon, RailgunWeapon, TeslaWeapon | 0 | 0 | legacy tie: Laser/Tesla |
| `CabalCommandoPlasmaMk2Neutron` | ? | HeavyCannon, MagicWeapon, MediumChemicalWeapon, MediumFlameWeapon, RailgunWeapon, TeslaWeapon | 0 | 0 | exception-bearing retired family; legacy tie: Magic/Chemical/Flame/Tesla |
| `CabalCommandoPlasmaNeutron` | ? | HeavyCannon, MagicWeapon, MediumChemicalWeapon, MediumFlameWeapon, RailgunWeapon, TeslaWeapon | 0 | 0 | exception-bearing retired family; legacy tie: Magic/Chemical/Flame/Tesla |
| `CabalMothershipRockets` | ? | ArrowWeapon, Grenade, HeavyMissile, MediumChemicalWeapon, MediumFlameWeapon, TeslaWeapon | 0 | 0 | legacy tie: Chemical/Flame/Tesla |
| `ConsortiumMissileSystem` | ? | ArrowWeapon, TeslaWeapon | 1 | 1 | multiple inherited family/tier destinations; name and canonical destination disagree; canonical and legacy signals disagree; name and legacy signals disagree |
| `CryoLegionnaireAttack` | ? | Chaingun, FlakWeapon, LaserWeapon, LightMissile, SmallArms, TeslaWeapon | 0 | 0 | name and legacy signals disagree |
| `DuelistTankCannon` | ? | Grenade, HeavyBomb, MediumFlameWeapon, TankDestroyerCannon | 0 | 0 | multiple inherited family/tier destinations; canonical and legacy signals disagree; name and legacy signals disagree |
| `FutureHarbingerCannon` | CannonHE_Heavy | HeavyBomb, HeavyChemicalWeapon, HeavyFlameWeapon, HeavyMissile, LaserWeapon, MagicWeapon, RailgunWeapon, TeslaChargedWeapon, TeslaWeapon | 1 | 0 | exception-bearing retired family; canonical and legacy signals disagree; name and legacy signals disagree |
| `FutureTankCannons` | CannonHE_Heavy | MagicWeapon, MediumChemicalWeapon, MediumFlameWeapon, ShrapnelWeapon, TeslaWeapon | 1 | 0 | exception-bearing retired family; legacy tie: Magic/Chemical/Flame/Tesla |
| `Future_Cryocopter_Cryo` | ? | Chaingun, FlakWeapon, LaserWeapon, LightMissile, SmallArms, TeslaWeapon | 1 | 0 | name and legacy signals disagree |
| `Future_MultiMissile_Sigma` | ? | FlakWeapon, MagicWeapon, MediumMissile, ShrapnelWeapon, TeslaChargedWeapon | 0 | 0 | exception-bearing retired family; legacy tie: Magic/Tesla |
| `GladiusCannon` | ? | HeavyFlameWeapon, LightFlameWeapon, MediumChemicalWeapon, ShrapnelWeapon | 0 | 0 | exception-bearing retired family; name and legacy signals disagree |
| `GradHeavyRockets` | ? | HeavyBomb, MediumChemicalWeapon | 0 | 0 | multiple inherited family/tier destinations; canonical and legacy signals disagree; name and legacy signals disagree |
| `GrenadeThermobaric` | ? | MediumFlameWeapon, ShrapnelWeapon | 1 | 1 | multiple inherited family/tier destinations; name and canonical destination disagree; name and legacy signals disagree |
| `HeavyAATankCannonAG` | ? | Chaingun, FlakWeapon, LightMissile, SmallArms | 1 | 1 | name and legacy signals disagree |
| `HovercraftCannon` | ? | Chaingun, Grenade, MediumCannon, ShrapnelWeapon, SmallArms, TankDestroyerCannon | 1 | 1 | name and legacy signals disagree |
| `HovercraftPlasmaCannon` | ? | HeavyBomb, HeavyCannon, TeslaWeapon | 0 | 0 | name and legacy signals disagree |
| `HueyFireMissiles` | CannonHE_Heavy | Grenade, LightFlameWeapon, MediumMissile, ShrapnelWeapon | 0 | 0 | exception-bearing retired family; legacy tie: Concussion/Flame; name and canonical destination disagree |
| `IxianBomb_EMP` | ? | HeavyBomb, MediumFlameWeapon, TeslaChargedWeapon | 0 | 0 | legacy tie: Flame/Tesla |
| `JapanMaidenBowEnergized` | Arrow_Light | MediumChemicalWeapon, MediumFlameWeapon, MediumMissile, ShrapnelWeapon, TeslaWeapon | 1 | 1 | legacy tie: Chemical/Flame/Tesla |
| `KamovMissilesTesla` | ? | Grenade, MediumFlameWeapon, MediumMissile, ShrapnelWeapon, TeslaWeapon | 0 | 0 | legacy tie: Concussion/Flame/Tesla |
| `LatinSmokerCannon` | ? | HeavyBomb, MediumFlameWeapon, RailgunWeapon, ShrapnelWeapon | 1 | 0 | multiple inherited family/tier destinations; canonical and legacy signals disagree; name and legacy signals disagree |
| `LunarTigerCannon` | CannonHE_Medium | HeavyBomb, LightChemicalWeapon, MediumFlameWeapon | 3 | 0 | legacy tie: Chemical/Flame |
| `MammothTuskTesla` | ? | Grenade, HeavyMissile, LightFlameWeapon, TeslaWeapon | 3 | 2 | exception-bearing retired family; legacy tie: Flame/Tesla |
| `MedicFlare` | ? | FlakWeapon, LaserWeapon, LightFlameWeapon | 0 | 0 | exception-bearing retired family; legacy tie: Laser/Flame |
| `MonsterTankTuskTesla` | ? | Grenade, HeavyMissile, LightFlameWeapon, TeslaWeapon | 0 | 0 | exception-bearing retired family; legacy tie: Flame/Tesla |
| `NanoSmokeAG` | ? | LightFlameWeapon, MagicWeapon, MediumChemicalWeapon | 0 | 0 | exception-bearing retired family; legacy tie: Flame/Magic/Chemical |
| `NaxMausCannon` | CannonHE_Medium | HeavyBomb, HeavyFlameWeapon, LightChemicalWeapon, ShrapnelWeapon, TankDestroyerCannon | 1 | 0 | legacy tie: Flame/Chemical |
| `NaxRatteCannon` | CannonHE_Medium | HeavyBomb, LightChemicalWeapon | 1 | 0 | canonical and legacy signals disagree; name and legacy signals disagree |
| `NaxiShrek` | MissileAP_Medium | Grenade, HeavyBomb, HeavyFlameWeapon, MediumChemicalWeapon, TankDestroyerCannon | 1 | 0 | legacy tie: Flame/Chemical |
| `NaxiShrekCons` | MissileAP_Medium | Grenade, HeavyBomb, HeavyFlameWeapon, MediumChemicalWeapon, TankDestroyerCannon | 1 | 0 | legacy tie: Flame/Chemical |
| `NaxisBlackBombSmaller` | CannonHE_Medium | HeavyBomb, LightChemicalWeapon | 0 | 0 | name and canonical destination disagree; canonical and legacy signals disagree; name and legacy signals disagree |
| `OISmallPlasmaCannon` | ? | HeavyCannon, HeavyChemicalWeapon, RailgunWeapon, TeslaWeapon | 0 | 0 | legacy tie: Chemical/Tesla |
| `ParaBombNuke` | ? | HeavyBomb, HeavyChemicalWeapon, HeavyFlameWeapon, NuclearWarhead | 0 | 0 | exception-bearing retired family |
| `PhotonCannon` | ? | FlakWeapon, HeavyCannon, MediumCannon, TankDestroyerCannon | 4 | 1 | name and legacy signals disagree |
| `PositronGrenade` | CannonHE_Medium | FlakWeapon, Grenade, SmallArms, TankDestroyerCannon | 2 | 2 | legacy tie: Flak/Bullet/CannonAP; name and canonical destination disagree |
| `RA2120xmm` | CannonHE_Heavy | ShrapnelWeapon, TankDestroyerCannon | 7 | 1 | canonical and legacy signals disagree |
| `RA2120xmm_rad` | CannonHE_Heavy | HeavyChemicalWeapon, LightChemicalWeapon, MediumChemicalWeapon | 1 | 0 | canonical and legacy signals disagree |
| `RA2CRM60H` | ? | SniperWeapon | 0 | 0 | multiple inherited family/tier destinations; no mapped legacy signal |
| `RA2CosmonautLaser` | Bullet_Medium | LaserWeapon, RailgunWeapon | 0 | 0 | name and canonical destination disagree; canonical and legacy signals disagree |
| `RA2FlakTrackGun` | Flak_Medium | Chaingun, Grenade, SmallArms | 5 | 1 | canonical and legacy signals disagree; name and legacy signals disagree |
| `RA2FreedomAK47` | CannonHE_Heavy | Chaingun, SniperWeapon | 1 | 0 | canonical and legacy signals disagree |
| `RA2FreedomRocket` | MissileAP_Medium | FlakWeapon, ShrapnelWeapon | 1 | 1 | name and canonical destination disagree; canonical and legacy signals disagree; name and legacy signals disagree |
| `RA2GrandCannonWeapon` | ? | HeavyBomb, LightChemicalWeapon, MediumFlameWeapon, ShrapnelWeapon | 0 | 0 | multiple inherited family/tier destinations; legacy tie: Chemical/Flame |
| `RA2HeavyMirageGun` | CannonAP_Light | MediumChemicalWeapon, RailgunWeapon, ShrapnelWeapon | 1 | 0 | canonical and legacy signals disagree |
| `RA2LasherToxicMortar` | ? | Grenade, HeavyBomb, HeavyCannon, HeavyChemicalWeapon, LightChemicalWeapon, MediumCannon, MediumChemicalWeapon, ShrapnelWeapon, TankDestroyerCannon | 1 | 1 | name and legacy signals disagree |
| `RA2MirageGun` | CannonAP_Light | MediumChemicalWeapon, RailgunWeapon | 1 | 0 | canonical and legacy signals disagree |
| `RA2MortarBike` | CannonHE_Heavy | Grenade, HeavyBomb, LightFlameWeapon, MediumChemicalWeapon, ShrapnelWeapon | 1 | 0 | exception-bearing retired family; legacy tie: Concussion/Flame/Chemical; name and canonical destination disagree |
| `RA2SCUD_rad` | ? | HeavyChemicalWeapon, LightChemicalWeapon, MediumChemicalWeapon | 0 | 0 | multiple inherited family/tier destinations; name and canonical destination disagree; canonical and legacy signals disagree; name and legacy signals disagree |
| `RA2Virusgun` | ? | HeavyChemicalWeapon, SniperWeapon | 3 | 1 | name and legacy signals disagree |
| `Rammax_Sabot` | ? | Chaingun, LaserWeapon, TeslaWeapon | 0 | 0 | legacy tie: Laser/Tesla |
| `RocketAngelRockets` | ? | ArrowWeapon, Grenade, HeavyMissile, MediumChemicalWeapon, MediumFlameWeapon, TeslaWeapon | 0 | 0 | legacy tie: Chemical/Flame/Tesla |
| `ShotgunAttackRobotGun` | ? | Grenade, ShrapnelWeapon, SmallArms, TankDestroyerCannon | 1 | 0 | multiple inherited family/tier destinations; canonical and legacy signals disagree; name and legacy signals disagree |
| `SiegeEngineCannon` | ? | Grenade, HeavyBomb, HeavyCannon, HeavyChemicalWeapon, HeavyFlameWeapon, LightChemicalWeapon, LightFlameWeapon, LightMissile, MediumCannon, MediumChemicalWeapon, MediumFlameWeapon, NuclearWarhead, ShrapnelWeapon, TankDestroyerCannon | 0 | 0 | exception-bearing retired family; legacy tie: Chemical/Flame |
| `SiegeTankSiegeCannon` | ? | Grenade, HeavyBomb, HeavyCannon, HeavyChemicalWeapon, HeavyFlameWeapon, LightChemicalWeapon, LightFlameWeapon, LightMissile, MediumCannon, MediumChemicalWeapon, MediumFlameWeapon, NuclearWarhead, ShrapnelWeapon, TankDestroyerCannon | 0 | 0 | exception-bearing retired family; legacy tie: Chemical/Flame |
| `SkyHawkArrowsEnergized` | ? | MediumFlameWeapon, TeslaWeapon | 1 | 1 | multiple inherited family/tier destinations; legacy tie: Flame/Tesla |
| `SpecterArtilleryShellUpgrade` | ? | HeavyBomb, HeavyCannon, MediumCannon, MediumChemicalWeapon, MediumFlameWeapon, TankDestroyerCannon | 0 | 0 | multiple inherited family/tier destinations; legacy tie: CannonHE/Chemical/Flame |
| `StarshipSovereignBeam` | ? | Chaingun, FlakWeapon, LaserWeapon, LightMissile, SmallArms, TeslaWeapon | 0 | 0 | name and legacy signals disagree |
| `SteelInfRailgun_EMP` | ? | Grenade, MediumCannon, MediumChemicalWeapon, MediumFlameWeapon, ShrapnelWeapon, TeslaWeapon | 2 | 0 | multiple inherited family/tier destinations; legacy tie: Concussion/Chemical/Flame/Tesla |
| `SteelKatyCannons_EMP` | ? | TankDestroyerCannon, TeslaWeapon | 1 | 0 | multiple inherited family/tier destinations; name and canonical destination disagree; canonical and legacy signals disagree |
| `SteelMakoGun_EMP` | ? | LightChemicalWeapon, TeslaWeapon | 1 | 0 | multiple inherited family/tier destinations; legacy tie: Chemical/Tesla; name and canonical destination disagree |
| `SteelMegaSword` | ? | LaserWeapon, LightChemicalWeapon, SwordWeapon | 0 | 0 | legacy tie: Laser/Chemical |
| `SteelMegaSword_EMP` | ? | HeavyChemicalWeapon, LaserWeapon, TeslaWeapon | 1 | 1 | legacy tie: Chemical/Laser/Tesla |
| `SteelQuantumTurretRail` | CannonHE_Heavy | HeavyChemicalWeapon, HeavyFlameWeapon, LaserWeapon, RailgunWeapon, TeslaWeapon | 1 | 1 | legacy tie: Chemical/Flame/Laser/Tesla; name and canonical destination disagree |
| `SteelRunnerPistols` | ? | LaserWeapon, RailgunWeapon, TeslaWeapon | 7 | 0 | legacy tie: Laser/Tesla |
| `SteelStalkerRailgun` | ? | LaserWeapon, RailgunWeapon | 3 | 3 | name and legacy signals disagree |
| `TSChem120mmx` | CannonHE_Medium | MediumChemicalWeapon, ShrapnelWeapon | 0 | 0 | name and canonical destination disagree; canonical and legacy signals disagree |
| `TSRPGTowerRail` | CannonHE_Heavy | RailgunWeapon, ShrapnelWeapon | 0 | 0 | canonical and legacy signals disagree |
| `TSTurretLaserFire` | ? | Chaingun, FlakWeapon, HeavyCannon, LaserWeapon, LightFlameWeapon, SmallArms | 0 | 0 | exception-bearing retired family; name and legacy signals disagree |
| `Tentacle` | CannonHE_Heavy | Chaingun, RailgunWeapon, SwordWeapon | 0 | 0 | canonical and legacy signals disagree |
| `TurretGunBlackMarket` | Concussion_Medium | HeavyBomb, HeavyCannon | 0 | 0 | legacy tie: Demolition/CannonHE |
| `Type89PlasmaCannon` | ? | HeavyCannon, TankDestroyerCannon, TeslaWeapon | 0 | 0 | name and legacy signals disagree |
| `VoidRayBeam` | ? | Chaingun, FlakWeapon, LaserWeapon, LightMissile, SmallArms, TeslaWeapon | 0 | 0 | name and legacy signals disagree |
| `VolkovMagneticWeapon` | CannonHE_Heavy | Grenade, MediumChemicalWeapon | 9 | 8 | canonical and legacy signals disagree |
| `VolkovMagneticWeaponIncendiaryNuclearShells` | CannonHE_Heavy | MediumFlameWeapon, NuclearWarhead | 0 | 0 | exception-bearing retired family; name and canonical destination disagree; canonical and legacy signals disagree |
| `VolkovMagneticWeaponIncendiaryTesla` | CannonHE_Heavy | MediumFlameWeapon, TeslaWeapon | 0 | 0 | legacy tie: Flame/Tesla; name and canonical destination disagree |
| `VultureGrenade` | CannonHE_Medium | FlakWeapon, Grenade, SmallArms, TankDestroyerCannon | 0 | 0 | legacy tie: Flak/Bullet/CannonAP; name and canonical destination disagree |
| `WaveforceCannonDistortedBeam1` | ? | HeavyCannon, TeslaWeapon | 0 | 0 | name and legacy signals disagree |
| `YakNuclearBomb` | ? | HeavyBomb, HeavyChemicalWeapon, HeavyFlameWeapon, NuclearWarhead | 0 | 0 | exception-bearing retired family |
| `YakTeslaBomb` | ? | HeavyBomb, HeavyChemicalWeapon, HeavyFlameWeapon, TeslaWeapon | 0 | 0 | legacy tie: Chemical/Flame/Tesla |
| `bfg10kCannon` | ? | LaserWeapon, MagicWeapon, RailgunWeapon, TeslaChargedWeapon | 1 | 0 | exception-bearing retired family; legacy tie: Laser/Magic/Tesla |
| `edenRailgun` | ? | HeavyCannon, LightChemicalWeapon, MediumCannon, RailgunWeapon, ShrapnelWeapon, TankDestroyerCannon | 2 | 0 | legacy tie: CannonHE/Chemical |
| `ixian_farasha` | ? | HeavyFlameWeapon, RailgunWeapon, TeslaWeapon | 1 | 1 | legacy tie: Flame/Tesla |
| `ra1_allies_chronovortex` | ? | HeavyBomb, MagicWeapon | 0 | 0 | exception-bearing retired family |
| `ra2roktgun` | Bullet_Medium | FlakWeapon, LightMissile, SmallArms | 1 | 1 | legacy tie: Flak/Bullet |
| `wc2gryphonFireVisible` | ? | HeavyBomb, LightMissile, MediumFlameWeapon, TeslaChargedWeapon | 0 | 0 | legacy tie: Flame/Tesla |
