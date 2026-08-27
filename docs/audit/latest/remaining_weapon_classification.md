# Remaining Weapon Classification

This report is read-only triage. A suggested destination is not approval to edit YAML;
each group still needs a proposed resolved diff and the full behavior comparator.

Active concrete roots still using retired flat families: **197**.

| review bucket | roots | meaning |
|---|--:|---|
| one inherited destination | 0 | one family and tier appears in the actual inheritance chain without conflicting evidence |
| corroborated suggestion | 0 | weapon-name and weighted legacy evidence agree |
| legacy-only suggestion | 4 | one weighted legacy signal exists, but the name does not confirm it |
| human decision required | 193 | conflicting, exceptional, or missing destination evidence |

The machine-readable JSON includes every flat hit's targets, exclusions, relationships,
score flag, friendly-fire modifiers, physical-state bindings, full percentage hits, descendant
closure, and descendant overrides of retired flat keys.

## One Inherited Destination (0)

| weapon | proposed family | retired families | descendants | old-key child overrides | evidence |
|---|---|---|--:|--:|---|

## Corroborated Suggestion (0)

| weapon | proposed family | retired families | descendants | old-key child overrides | evidence |
|---|---|---|--:|--:|---|

## Legacy-Only Suggestion (4)

| weapon | proposed family | retired families | descendants | old-key child overrides | evidence |
|---|---|---|--:|--:|---|
| `AsianMaidenBow` | Concussion | ArrowWeapon, Grenade | 1 | 1 | legacy score Concussion=2 |
| `SamuraiBladeCharged` | Tesla | SwordWeapon, TeslaWeapon | 0 | 0 | legacy score Tesla=4 |
| `SteelAirTurret` | Laser | LaserWeapon, RailgunWeapon | 3 | 3 | legacy score Laser=4 |
| `TS30mmRail` | Flak | FlakWeapon, MediumMissile, RailgunWeapon | 0 | 0 | legacy score Flak=3 |

## Human Decision Required (193)

| weapon | proposed family | retired families | descendants | old-key child overrides | evidence |
|---|---|---|--:|--:|---|
| `120mm_td` | CannonHE_Medium | LightChemicalWeapon, MediumChemicalWeapon, TankDestroyerCannon | 0 | 0 | canonical and legacy signals disagree |
| `25mm` | CannonHE_Medium | Grenade, LightFlameWeapon, MediumChemicalWeapon, ShrapnelWeapon, TankDestroyerCannon | 1 | 0 | exception-bearing retired family; legacy tie: Concussion/Flame/Chemical |
| `APTusk` | ? | FlakWeapon, Grenade, MediumMissile, TankDestroyerCannon | 1 | 0 | legacy tie: Flak/CannonAP |
| `ArcherArtilleryShell` | ? | HeavyBomb, HeavyCannon, MediumCannon, MediumFlameWeapon, ShrapnelWeapon | 0 | 0 | legacy tie: CannonHE/Flame |
| `ArtilleryShellUpgrade` | ? | Grenade, HeavyBomb, MediumCannon, MediumChemicalWeapon, MediumFlameWeapon, ShrapnelWeapon | 0 | 0 | legacy tie: Concussion/Chemical/Flame |
| `AsianChemical` | Demolition_Light | HeavyBomb, HeavyChemicalWeapon, LightChemicalWeapon, MediumChemicalWeapon, ShrapnelWeapon | 1 | 0 | name and canonical destination disagree; canonical and legacy signals disagree |
| `AsianHarbingerPlasma` | ? | HeavyBomb, LightChemicalWeapon, LightFlameWeapon, MediumChemicalWeapon, MediumFlameWeapon, ShrapnelWeapon | 0 | 0 | exception-bearing retired family; multiple inherited family/tier destinations; legacy tie: Chemical/Flame; name and canonical destination disagree |
| `AsianLynxTankCannon` | CannonHE_Medium | Grenade, LightFlameWeapon, MediumChemicalWeapon, ShrapnelWeapon, TankDestroyerCannon | 1 | 0 | exception-bearing retired family; legacy tie: Concussion/Flame/Chemical |
| `AsianMLRS` | ? | FlakWeapon, HeavyMissile | 1 | 1 | multiple inherited family/tier destinations; canonical and legacy signals disagree |
| `AsianPhotonCannon` | ? | FlakWeapon, MagicWeapon, MediumMissile, TeslaWeapon | 13 | 6 | exception-bearing retired family; legacy tie: Magic/Tesla |
| `AsianPulverizerMechaGatling` | ? | MediumChemicalWeapon, MediumMissile | 0 | 0 | multiple inherited family/tier destinations; canonical and legacy signals disagree; name and legacy signals disagree |
| `AsianSinglePlasma` | ? | HeavyBomb, LightChemicalWeapon, LightFlameWeapon, MediumChemicalWeapon, MediumFlameWeapon, ShrapnelWeapon | 4 | 0 | exception-bearing retired family; multiple inherited family/tier destinations; legacy tie: Chemical/Flame; name and canonical destination disagree |
| `AthenaLaser` | ? | Chaingun, FlakWeapon, LaserWeapon, LightMissile, SmallArms, TeslaWeapon | 0 | 0 | name and legacy signals disagree |
| `AtreusMG` | ? | FlakWeapon, Grenade, MediumMissile | 0 | 0 | multiple inherited family/tier destinations; canonical and legacy signals disagree; name and legacy signals disagree |
| `BCLaser` | CannonHE_Heavy | HeavyAAWeapon, HeavyBomb, HeavyChemicalWeapon, HeavyFlameWeapon, HeavyMissile, LaserWeapon, NuclearWarhead, RailgunWeapon | 1 | 1 | exception-bearing retired family; name and canonical destination disagree; canonical and legacy signals disagree; name and legacy signals disagree |
| `BallistaMultiShot` | ? | ArrowWeapon, Grenade, LightChemicalWeapon, LightFlameWeapon | 1 | 0 | exception-bearing retired family; legacy tie: Chemical/Flame |
| `BallistaMultiShotEnergized` | ? | ArrowWeapon, MediumChemicalWeapon, MediumFlameWeapon, TeslaWeapon | 1 | 0 | legacy tie: Chemical/Flame/Tesla |
| `BehemothShoot` | ? | Grenade, HeavyBomb, HeavyMissile, LightFlameWeapon, MediumChemicalWeapon, ShrapnelWeapon | 0 | 0 | exception-bearing retired family; legacy tie: Concussion/Flame/Chemical |
| `BikeRockets` | ? | FlakWeapon, LightMissile, ShrapnelWeapon, TankDestroyerCannon | 0 | 0 | legacy tie: Flak/CannonAP |
| `BoxerCannonAG` | ? | Chaingun, FlakWeapon, Grenade, LightMissile, MediumCannon, SmallArms | 1 | 1 | name and legacy signals disagree |
| `BuggyPlasmaGrenade` | Demolition_Light | HeavyBomb, ShrapnelWeapon | 0 | 0 | legacy tie: Demolition/Concussion; name and canonical destination disagree |
| `CabalArtilleryWalkerShell` | ? | Grenade, HeavyCannon, MediumCannon, ShrapnelWeapon | 0 | 0 | legacy tie: Concussion/CannonHE |
| `CabalArtilleryWalkerShellUpgraded` | ? | Grenade, HeavyCannon, MagicWeapon, MediumCannon, MediumChemicalWeapon, RailgunWeapon, ShrapnelWeapon, TeslaChargedWeapon | 0 | 0 | exception-bearing retired family; legacy tie: Concussion/CannonHE/Magic/Chemical/Tesla |
| `CabalBeholderLaser` | ? | HeavyCannon, LaserWeapon, RailgunWeapon, TeslaWeapon | 0 | 0 | legacy tie: Laser/Tesla |
| `CabalCommandoPlasma` | ? | HeavyCannon, MediumChemicalWeapon, MediumFlameWeapon | 0 | 0 | legacy tie: Chemical/Flame |
| `CabalCommandoPlasmaMk2` | ? | HeavyCannon, MediumChemicalWeapon, MediumFlameWeapon | 0 | 0 | legacy tie: Chemical/Flame |
| `CabalCommandoPlasmaMk2Neutron` | ? | HeavyCannon, MagicWeapon, MediumChemicalWeapon, MediumFlameWeapon, RailgunWeapon, TeslaWeapon | 0 | 0 | exception-bearing retired family; legacy tie: Magic/Chemical/Flame/Tesla |
| `CabalCommandoPlasmaNeutron` | ? | HeavyCannon, MagicWeapon, MediumChemicalWeapon, MediumFlameWeapon, RailgunWeapon, TeslaWeapon | 0 | 0 | exception-bearing retired family; legacy tie: Magic/Chemical/Flame/Tesla |
| `CabalMothershipRockets` | ? | ArrowWeapon, Grenade, HeavyMissile, MediumChemicalWeapon, MediumFlameWeapon, TeslaWeapon | 0 | 0 | legacy tie: Chemical/Flame/Tesla |
| `CabalSubmarinePlasma` | ? | HeavyCannon, MediumChemicalWeapon, MediumFlameWeapon | 0 | 0 | legacy tie: Chemical/Flame |
| `CannonAttackRobotGun` | ? | MediumFlameWeapon, TankDestroyerCannon | 1 | 0 | multiple inherited family/tier destinations; canonical and legacy signals disagree; name and legacy signals disagree |
| `ChemicalHonestJohn` | ? | HeavyBomb, HeavyChemicalWeapon, HeavyFlameWeapon, HeavyMissile | 0 | 0 | legacy tie: Chemical/Flame |
| `ConsortiumMissileSystem` | ? | ArrowWeapon, TeslaWeapon | 1 | 1 | multiple inherited family/tier destinations; name and canonical destination disagree; canonical and legacy signals disagree; name and legacy signals disagree |
| `CryoLegionnaireAttack` | ? | Chaingun, FlakWeapon, LaserWeapon, LightMissile, SmallArms, TeslaWeapon | 0 | 0 | name and legacy signals disagree |
| `D2K_155mm2` | ? | Grenade, HeavyBomb, MediumFlameWeapon, ShrapnelWeapon | 0 | 0 | legacy tie: Concussion/Flame |
| `DeviatorMissile` | ? | MediumChemicalWeapon, MediumMissile | 2 | 0 | multiple inherited family/tier destinations; name and canonical destination disagree; canonical and legacy signals disagree; name and legacy signals disagree |
| `DeviatorMissile_Artillery` | ? | HeavyFlameWeapon, ShrapnelWeapon | 1 | 0 | multiple inherited family/tier destinations; name and canonical destination disagree; canonical and legacy signals disagree; name and legacy signals disagree |
| `DiabloCannon` | Flak_Medium | Chaingun, LightMissile, TankDestroyerCannon | 3 | 0 | legacy tie: Bullet/CannonAP; name and canonical destination disagree |
| `DreadshroudSpore` | ? | FlakWeapon, LaserWeapon, LightFlameWeapon, MediumChemicalWeapon, MediumMissile, ShrapnelWeapon | 0 | 0 | exception-bearing retired family; legacy tie: Laser/Flame/Chemical |
| `DuelistTankCannon` | ? | Grenade, HeavyBomb, MediumFlameWeapon, TankDestroyerCannon | 0 | 0 | multiple inherited family/tier destinations; canonical and legacy signals disagree; name and legacy signals disagree |
| `EpigraphMG` | CannonHE_Heavy | ArrowWeapon, Chaingun, FlakWeapon, Grenade, HeavyAAWeapon, HeavyMissile, LightMissile, MediumChemicalWeapon, MediumMissile | 0 | 0 | name and canonical destination disagree; canonical and legacy signals disagree; name and legacy signals disagree |
| `FirehawkBomb` | ? | HeavyBomb, MediumFlameWeapon, NuclearWarhead, ShrapnelWeapon | 0 | 0 | exception-bearing retired family; name and legacy signals disagree |
| `FutureHarbingerCannon` | CannonHE_Heavy | HeavyBomb, HeavyChemicalWeapon, HeavyFlameWeapon, HeavyMissile, LaserWeapon, MagicWeapon, RailgunWeapon, TeslaChargedWeapon, TeslaWeapon | 1 | 0 | exception-bearing retired family; canonical and legacy signals disagree; name and legacy signals disagree |
| `FutureMechGatling` | Bullet_Medium | FlakWeapon, LightMissile, TankDestroyerCannon | 0 | 0 | legacy tie: Flak/CannonAP |
| `FutureMechPlasma` | CannonHE_Heavy | Grenade, MediumChemicalWeapon | 1 | 0 | name and canonical destination disagree; canonical and legacy signals disagree; name and legacy signals disagree |
| `FutureTankCannons` | CannonHE_Heavy | MagicWeapon, MediumChemicalWeapon, MediumFlameWeapon, ShrapnelWeapon, TeslaWeapon | 1 | 0 | exception-bearing retired family; legacy tie: Magic/Chemical/Flame/Tesla |
| `Future_Cryocopter_Cryo` | ? | Chaingun, FlakWeapon, LaserWeapon, LightMissile, SmallArms, TeslaWeapon | 1 | 0 | name and legacy signals disagree |
| `Future_MultiMissile_Sigma` | ? | FlakWeapon, MagicWeapon, MediumMissile, ShrapnelWeapon, TeslaChargedWeapon | 0 | 0 | exception-bearing retired family; legacy tie: Magic/Tesla |
| `GladiusCannon` | ? | HeavyFlameWeapon, LightFlameWeapon, MediumChemicalWeapon, ShrapnelWeapon | 0 | 0 | exception-bearing retired family; name and legacy signals disagree |
| `GoliathMk2MG` | ? | Grenade, TankDestroyerCannon | 0 | 0 | multiple inherited family/tier destinations; canonical and legacy signals disagree; name and legacy signals disagree |
| `GradHeavyRockets` | ? | HeavyBomb, MediumChemicalWeapon | 0 | 0 | multiple inherited family/tier destinations; canonical and legacy signals disagree; name and legacy signals disagree |
| `GrenadeThermobaric` | ? | MediumFlameWeapon, ShrapnelWeapon | 1 | 1 | multiple inherited family/tier destinations; name and canonical destination disagree; name and legacy signals disagree |
| `HMG_Duelist_upgrade` | ? | HeavyMissile, LaserWeapon | 0 | 0 | multiple inherited family/tier destinations; canonical and legacy signals disagree; name and legacy signals disagree |
| `HeavyAATankCannonAG` | ? | Chaingun, FlakWeapon, LightMissile, SmallArms | 1 | 1 | name and legacy signals disagree |
| `HeavyAATankCannontkm` | ? | Chaingun, FlakWeapon, LightMissile, SmallArms | 0 | 0 | name and legacy signals disagree |
| `HindMissilesNuclear` | ? | MediumFlameWeapon, MediumMissile, NuclearWarhead, TankDestroyerCannon | 0 | 0 | exception-bearing retired family |
| `HindMissilesThermobaric` | CannonHE_Heavy | Grenade, MediumFlameWeapon, MediumMissile, ShrapnelWeapon | 0 | 0 | legacy tie: Concussion/Flame; name and canonical destination disagree |
| `HovercraftCannon` | ? | Chaingun, Grenade, MediumCannon, ShrapnelWeapon, SmallArms, TankDestroyerCannon | 1 | 1 | name and legacy signals disagree |
| `HovercraftPlasmaCannon` | ? | HeavyBomb, HeavyCannon, TeslaWeapon | 0 | 0 | name and legacy signals disagree |
| `HueyFireMissiles` | CannonHE_Heavy | Grenade, LightFlameWeapon, MediumMissile, ShrapnelWeapon | 0 | 0 | exception-bearing retired family; legacy tie: Concussion/Flame; name and canonical destination disagree |
| `IxRailgunDroneBullet` | CannonHE_Medium | FlakWeapon, RailgunWeapon, TankDestroyerCannon | 0 | 0 | legacy tie: Flak/CannonAP; name and canonical destination disagree |
| `IxianBomb_EMP` | ? | HeavyBomb, MediumFlameWeapon, TeslaChargedWeapon | 0 | 0 | legacy tie: Flame/Tesla |
| `JapanMaidenBowEnergized` | Arrow_Light | MediumChemicalWeapon, MediumFlameWeapon, MediumMissile, ShrapnelWeapon, TeslaWeapon | 1 | 1 | legacy tie: Chemical/Flame/Tesla |
| `JapanSuperBomb` | ? | Grenade, HeavyBomb, MediumChemicalWeapon, MediumFlameWeapon, ShrapnelWeapon | 0 | 0 | legacy tie: Concussion/Chemical/Flame |
| `KamovMissilesTesla` | ? | Grenade, MediumFlameWeapon, MediumMissile, ShrapnelWeapon, TeslaWeapon | 0 | 0 | legacy tie: Concussion/Flame/Tesla |
| `KodiakCannon` | CannonHE_Heavy | HeavyBomb, HeavyChemicalWeapon, HeavyMissile, RailgunWeapon | 0 | 0 | canonical and legacy signals disagree; name and legacy signals disagree |
| `Laboratory_Bioball` | ? | MediumChemicalWeapon, MediumFlameWeapon | 0 | 0 | multiple inherited family/tier destinations; legacy tie: Chemical/Flame |
| `LatinSmokerCannon` | ? | HeavyBomb, MediumFlameWeapon, RailgunWeapon, ShrapnelWeapon | 1 | 0 | multiple inherited family/tier destinations; canonical and legacy signals disagree; name and legacy signals disagree |
| `LightTank2Cannon` | ? | LightChemicalWeapon, MediumCannon | 0 | 0 | name and legacy signals disagree |
| `LunarTigerCannon` | CannonHE_Medium | HeavyBomb, LightChemicalWeapon, MediumFlameWeapon | 3 | 0 | legacy tie: Chemical/Flame |
| `MammothTusk2` | ? | HeavyMissile, LightFlameWeapon, MediumChemicalWeapon | 1 | 0 | exception-bearing retired family; legacy tie: Flame/Chemical |
| `MammothTusk2Thermobaric` | ? | HeavyBomb, HeavyFlameWeapon, HeavyMissile | 1 | 0 | name and legacy signals disagree |
| `MammothTuskTesla` | ? | Grenade, HeavyMissile, LightFlameWeapon, TeslaWeapon | 3 | 2 | exception-bearing retired family; legacy tie: Flame/Tesla |
| `MammothTuskThermobaric` | ? | FlakWeapon, Grenade, HeavyFlameWeapon, HeavyMissile, LightFlameWeapon, MediumFlameWeapon, MediumMissile, ShrapnelWeapon | 1 | 0 | exception-bearing retired family; name and legacy signals disagree |
| `MarauderMissiles` | CannonHE_Heavy | MediumMissile, TankDestroyerCannon | 0 | 0 | name and canonical destination disagree; canonical and legacy signals disagree; name and legacy signals disagree |
| `MedicFlare` | ? | FlakWeapon, LaserWeapon, LightFlameWeapon | 0 | 0 | exception-bearing retired family; legacy tie: Laser/Flame |
| `MissileSoldierWeapon` | ? | HeavyMissile, LightChemicalWeapon, RailgunWeapon, ShrapnelWeapon, TankDestroyerCannon | 0 | 0 | name and legacy signals disagree |
| `MonsterTankTuskTesla` | ? | Grenade, HeavyMissile, LightFlameWeapon, TeslaWeapon | 0 | 0 | exception-bearing retired family; legacy tie: Flame/Tesla |
| `MonsterTankTuskThermobaric` | ? | FlakWeapon, Grenade, HeavyFlameWeapon, HeavyMissile, LightFlameWeapon, MediumFlameWeapon, MediumMissile, ShrapnelWeapon | 0 | 0 | exception-bearing retired family; name and legacy signals disagree |
| `MortarTeamArtilleryShell` | CannonHE_Medium | MediumChemicalWeapon, MediumFlameWeapon, ShrapnelWeapon | 0 | 0 | legacy tie: Chemical/Flame; name and canonical destination disagree |
| `MutFlamerChem` | ? | MediumChemicalWeapon, MediumFlameWeapon | 0 | 0 | legacy tie: Chemical/Flame |
| `MutHFlamerChem` | ? | HeavyChemicalWeapon, HeavyFlameWeapon, MediumChemicalWeapon, MediumFlameWeapon | 0 | 0 | legacy tie: Chemical/Flame |
| `NanoSmokeAG` | ? | LightFlameWeapon, MagicWeapon, MediumChemicalWeapon | 0 | 0 | exception-bearing retired family; legacy tie: Flame/Magic/Chemical |
| `NaxMausCannon` | CannonHE_Medium | HeavyBomb, HeavyFlameWeapon, LightChemicalWeapon, ShrapnelWeapon, TankDestroyerCannon | 1 | 0 | legacy tie: Flame/Chemical |
| `NaxRatteCannon` | CannonHE_Medium | HeavyBomb, LightChemicalWeapon | 1 | 0 | canonical and legacy signals disagree; name and legacy signals disagree |
| `NaxiInterceptorGun` | CannonHE_Heavy | Chaingun, FlakWeapon, SmallArms | 0 | 0 | canonical and legacy signals disagree |
| `NaxiMeteor` | ? | Grenade, HeavyBomb, MediumFlameWeapon, NuclearWarhead, ShrapnelWeapon | 0 | 0 | exception-bearing retired family |
| `NaxiShrek` | MissileAP_Medium | Grenade, HeavyBomb, HeavyFlameWeapon, MediumChemicalWeapon, TankDestroyerCannon | 1 | 0 | legacy tie: Flame/Chemical |
| `NaxiShrekCons` | MissileAP_Medium | Grenade, HeavyBomb, HeavyFlameWeapon, MediumChemicalWeapon, TankDestroyerCannon | 1 | 0 | legacy tie: Flame/Chemical |
| `NaxisBlackBombSmaller` | CannonHE_Medium | HeavyBomb, LightChemicalWeapon | 0 | 0 | name and canonical destination disagree; canonical and legacy signals disagree; name and legacy signals disagree |
| `Naxis_Komet` | ? | FlakWeapon, Grenade, HeavyAAWeapon, MediumMissile | 1 | 0 | legacy tie: Flak/MissileAA |
| `OISmallPlasmaCannon` | ? | HeavyCannon, HeavyChemicalWeapon, RailgunWeapon, TeslaWeapon | 0 | 0 | legacy tie: Chemical/Tesla |
| `OrcaMissiles` | ? | Grenade, HeavyCannon, HeavyMissile, MediumMissile, ShrapnelWeapon, TankDestroyerCannon | 1 | 0 | legacy tie: Concussion/MissileHE |
| `ParaBombNuke` | ? | HeavyBomb, HeavyChemicalWeapon, HeavyFlameWeapon, NuclearWarhead | 0 | 0 | exception-bearing retired family |
| `PhobosLaser` | CannonHE_Heavy | HeavyAAWeapon, HeavyBomb, HeavyChemicalWeapon, HeavyFlameWeapon, HeavyMissile, LaserWeapon, RailgunWeapon | 0 | 0 | legacy tie: Chemical/Flame/Laser; name and canonical destination disagree |
| `PhotonCannon` | ? | FlakWeapon, HeavyCannon, MediumCannon, TankDestroyerCannon | 4 | 1 | name and legacy signals disagree |
| `PlasmaFlamer` | ? | MediumChemicalWeapon, MediumFlameWeapon | 0 | 0 | legacy tie: Chemical/Flame |
| `PositronGrenade` | CannonHE_Medium | FlakWeapon, Grenade, SmallArms, TankDestroyerCannon | 2 | 2 | legacy tie: Flak/Bullet/CannonAP; name and canonical destination disagree |
| `RA2120xmm` | CannonHE_Heavy | ShrapnelWeapon, TankDestroyerCannon | 7 | 1 | canonical and legacy signals disagree |
| `RA2120xmm_rad` | CannonHE_Heavy | HeavyChemicalWeapon, LightChemicalWeapon, MediumChemicalWeapon | 1 | 0 | canonical and legacy signals disagree |
| `RA2APCFlakCannon` | Flak_Medium | Chaingun, LightMissile, TankDestroyerCannon | 3 | 0 | legacy tie: Bullet/CannonAP |
| `RA2CRM60H` | ? | SniperWeapon | 0 | 0 | multiple inherited family/tier destinations; no mapped legacy signal |
| `RA2CosmonautLaser` | Bullet_Medium | LaserWeapon, RailgunWeapon | 0 | 0 | name and canonical destination disagree; canonical and legacy signals disagree |
| `RA2FlakTrackGun` | Flak_Medium | Chaingun, Grenade, SmallArms | 5 | 1 | canonical and legacy signals disagree; name and legacy signals disagree |
| `RA2FreedomAK47` | CannonHE_Heavy | Chaingun, SniperWeapon | 1 | 0 | canonical and legacy signals disagree |
| `RA2FreedomRocket` | MissileAP_Medium | FlakWeapon, ShrapnelWeapon | 1 | 1 | name and canonical destination disagree; canonical and legacy signals disagree; name and legacy signals disagree |
| `RA2GrandCannonWeapon` | ? | HeavyBomb, LightChemicalWeapon, MediumFlameWeapon, ShrapnelWeapon | 0 | 0 | multiple inherited family/tier destinations; legacy tie: Chemical/Flame |
| `RA2HeavyMirageGun` | CannonAP_Light | MediumChemicalWeapon, RailgunWeapon, ShrapnelWeapon | 1 | 0 | canonical and legacy signals disagree |
| `RA2LarsRocket` | ? | HeavyAAWeapon, LightMissile, TankDestroyerCannon | 0 | 0 | multiple inherited family/tier destinations; legacy tie: MissileAA/CannonAP; name and canonical destination disagree |
| `RA2LasherCannon` | CannonHE_Medium | Grenade, LightFlameWeapon, MediumChemicalWeapon, ShrapnelWeapon, TankDestroyerCannon | 1 | 0 | exception-bearing retired family; legacy tie: Concussion/Flame/Chemical |
| `RA2LasherToxicMortar` | ? | Grenade, HeavyBomb, HeavyCannon, HeavyChemicalWeapon, LightChemicalWeapon, MediumCannon, MediumChemicalWeapon, ShrapnelWeapon, TankDestroyerCannon | 1 | 1 | name and legacy signals disagree |
| `RA2MirageGun` | CannonAP_Light | MediumChemicalWeapon, RailgunWeapon | 1 | 0 | canonical and legacy signals disagree |
| `RA2MortarBike` | CannonHE_Heavy | Grenade, HeavyBomb, LightFlameWeapon, MediumChemicalWeapon, ShrapnelWeapon | 1 | 0 | exception-bearing retired family; legacy tie: Concussion/Flame/Chemical; name and canonical destination disagree |
| `RA2PsychicJab` | CannonHE_Medium | FlakWeapon, LightFlameWeapon, TankDestroyerCannon | 1 | 0 | exception-bearing retired family; canonical and legacy signals disagree |
| `RA2RBurritoRocket` | ? | HeavyBomb, MediumFlameWeapon, ShrapnelWeapon | 0 | 0 | multiple inherited family/tier destinations; name and canonical destination disagree; canonical and legacy signals disagree; name and legacy signals disagree |
| `RA2SCUD_rad` | ? | HeavyChemicalWeapon, LightChemicalWeapon, MediumChemicalWeapon | 0 | 0 | multiple inherited family/tier destinations; name and canonical destination disagree; canonical and legacy signals disagree; name and legacy signals disagree |
| `RA2TOPOLCuba` | ? | Grenade, HeavyBomb, LightChemicalWeapon, MediumFlameWeapon, NuclearWarhead, ShrapnelWeapon | 0 | 0 | exception-bearing retired family |
| `RA2Virusgun` | ? | HeavyChemicalWeapon, SniperWeapon | 3 | 1 | name and legacy signals disagree |
| `Rammax_Sabot` | ? | Chaingun, LaserWeapon, TeslaWeapon | 0 | 0 | legacy tie: Laser/Tesla |
| `ReconRangerRecoillessGun` | MissileAP_Heavy | Chaingun, FlakWeapon, Grenade, MediumMissile, RailgunWeapon, ShrapnelWeapon, TankDestroyerCannon | 1 | 0 | canonical and legacy signals disagree |
| `RocketAngelRockets` | ? | ArrowWeapon, Grenade, HeavyMissile, MediumChemicalWeapon, MediumFlameWeapon, TeslaWeapon | 0 | 0 | legacy tie: Chemical/Flame/Tesla |
| `SCTyr` | CannonHE_Heavy | Chaingun, LightFlameWeapon, ShrapnelWeapon | 0 | 0 | exception-bearing retired family; canonical and legacy signals disagree |
| `ScarabLaunch` | CannonHE_Heavy | HeavyBomb, MediumMissile, ShrapnelWeapon | 0 | 0 | legacy tie: Demolition/MissileHE/Concussion |
| `ShotgunAttackRobotGun` | ? | Grenade, ShrapnelWeapon, SmallArms, TankDestroyerCannon | 1 | 0 | multiple inherited family/tier destinations; canonical and legacy signals disagree; name and legacy signals disagree |
| `ShtoraLaser` | ? | FlakWeapon, LaserWeapon, LightFlameWeapon | 0 | 0 | exception-bearing retired family; legacy tie: Laser/Flame |
| `SiegeEngineCannon` | ? | Grenade, HeavyBomb, HeavyCannon, HeavyChemicalWeapon, HeavyFlameWeapon, LightChemicalWeapon, LightFlameWeapon, LightMissile, MediumCannon, MediumChemicalWeapon, MediumFlameWeapon, NuclearWarhead, ShrapnelWeapon, TankDestroyerCannon | 0 | 0 | exception-bearing retired family; legacy tie: Chemical/Flame |
| `SiegeTankSiegeCannon` | ? | Grenade, HeavyBomb, HeavyCannon, HeavyChemicalWeapon, HeavyFlameWeapon, LightChemicalWeapon, LightFlameWeapon, LightMissile, MediumCannon, MediumChemicalWeapon, MediumFlameWeapon, NuclearWarhead, ShrapnelWeapon, TankDestroyerCannon | 0 | 0 | exception-bearing retired family; legacy tie: Chemical/Flame |
| `SkyHawkArrowsEnergized` | ? | MediumFlameWeapon, TeslaWeapon | 1 | 1 | multiple inherited family/tier destinations; legacy tie: Flame/Tesla |
| `SkyshieldCannon` | ? | Chaingun, FlakWeapon, LightMissile, SmallArms | 0 | 0 | name and legacy signals disagree |
| `SpecterArtilleryShellUpgrade` | ? | HeavyBomb, HeavyCannon, MediumCannon, MediumChemicalWeapon, MediumFlameWeapon, TankDestroyerCannon | 0 | 0 | multiple inherited family/tier destinations; legacy tie: CannonHE/Chemical/Flame |
| `Spore_AA` | ? | Chaingun, FlakWeapon, MediumMissile | 0 | 0 | legacy tie: Bullet/Flak |
| `StarshipSovereignBeam` | ? | Chaingun, FlakWeapon, LaserWeapon, LightMissile, SmallArms, TeslaWeapon | 0 | 0 | name and legacy signals disagree |
| `SteelCruiserCannons` | Bullet_Medium | FlakWeapon, LightMissile, SmallArms | 1 | 0 | legacy tie: Flak/Bullet; name and canonical destination disagree |
| `SteelDaggerCannon` | ? | MediumFlameWeapon, ShrapnelWeapon | 1 | 0 | multiple inherited family/tier destinations; canonical and legacy signals disagree; name and legacy signals disagree |
| `SteelFighterRailgun` | ? | LaserWeapon, TankDestroyerCannon | 0 | 0 | multiple inherited family/tier destinations; canonical and legacy signals disagree; name and legacy signals disagree |
| `SteelInfRailgun_EMP` | ? | Grenade, MediumCannon, MediumChemicalWeapon, MediumFlameWeapon, ShrapnelWeapon, TeslaWeapon | 2 | 0 | multiple inherited family/tier destinations; legacy tie: Concussion/Chemical/Flame/Tesla |
| `SteelKatyCannons_EMP` | ? | TankDestroyerCannon, TeslaWeapon | 1 | 0 | multiple inherited family/tier destinations; name and canonical destination disagree; canonical and legacy signals disagree |
| `SteelMakoGun_EMP` | ? | LightChemicalWeapon, TeslaWeapon | 1 | 0 | multiple inherited family/tier destinations; legacy tie: Chemical/Tesla; name and canonical destination disagree |
| `SteelMegaSword` | ? | LaserWeapon, LightChemicalWeapon, SwordWeapon | 0 | 0 | legacy tie: Laser/Chemical |
| `SteelMegaSword_EMP` | ? | HeavyChemicalWeapon, LaserWeapon, TeslaWeapon | 1 | 1 | legacy tie: Chemical/Laser/Tesla |
| `SteelQuantumTurretRail` | CannonHE_Heavy | HeavyChemicalWeapon, HeavyFlameWeapon, LaserWeapon, RailgunWeapon, TeslaWeapon | 1 | 1 | legacy tie: Chemical/Flame/Laser/Tesla; name and canonical destination disagree |
| `SteelRunnerPistols` | ? | LaserWeapon, RailgunWeapon, TeslaWeapon | 7 | 0 | legacy tie: Laser/Tesla |
| `SteelStalkerRailgun` | ? | LaserWeapon, RailgunWeapon | 3 | 3 | name and legacy signals disagree |
| `SteelTwisterMissiles` | MissileAP_Medium | FlakWeapon, Grenade, HeavyAAWeapon, MediumFlameWeapon | 1 | 0 | name and canonical destination disagree; canonical and legacy signals disagree; name and legacy signals disagree |
| `SwarmlingShoot` | ? | Chaingun, FlakWeapon, Grenade, HeavyBomb, MediumChemicalWeapon, MediumFlameWeapon, MediumMissile, ShrapnelWeapon | 0 | 0 | legacy tie: Concussion/Chemical/Flame |
| `TS70mmTurChem` | CannonHE_Medium | LightChemicalWeapon, TankDestroyerCannon | 0 | 0 | name and canonical destination disagree; canonical and legacy signals disagree |
| `TSBikeTibMissile` | ? | LightChemicalWeapon, LightMissile, MediumChemicalWeapon, MediumMissile | 0 | 0 | name and legacy signals disagree |
| `TSCABALEnlightedLaser` | ? | LaserWeapon, MediumChemicalWeapon, MediumFlameWeapon, TankDestroyerCannon | 0 | 0 | legacy tie: Laser/Chemical/Flame |
| `TSCABALObeliskLaserFire` | ? | LaserWeapon, MediumChemicalWeapon, MediumFlameWeapon, TankDestroyerCannon | 0 | 0 | legacy tie: Laser/Chemical/Flame |
| `TSChem120mmx` | CannonHE_Medium | MediumChemicalWeapon, ShrapnelWeapon | 0 | 0 | name and canonical destination disagree; canonical and legacy signals disagree |
| `TSDestroyerMissiles` | ? | FlakWeapon, MediumMissile, ShrapnelWeapon | 0 | 0 | name and legacy signals disagree |
| `TSHellfireTwin` | ? | Grenade, HeavyCannon, HeavyMissile, MediumMissile, ShrapnelWeapon, TankDestroyerCannon | 0 | 0 | legacy tie: Concussion/MissileHE |
| `TSHoverMissile` | ? | FlakWeapon, MediumMissile, ShrapnelWeapon | 0 | 0 | name and legacy signals disagree |
| `TSProton` | ? | LaserWeapon, MediumFlameWeapon | 0 | 0 | legacy tie: Laser/Flame |
| `TSRPGTowerRail` | CannonHE_Heavy | RailgunWeapon, ShrapnelWeapon | 0 | 0 | canonical and legacy signals disagree |
| `TSSAPCMissiles` | ? | Grenade, LightMissile | 1 | 0 | legacy tie: Concussion/MissileHE |
| `TSScoopDualTurChem` | CannonHE_Heavy | MediumChemicalWeapon, ShrapnelWeapon | 0 | 0 | name and canonical destination disagree; canonical and legacy signals disagree |
| `TSStankTibTusk` | CannonHE_Medium | MediumChemicalWeapon, MediumMissile | 0 | 0 | canonical and legacy signals disagree |
| `TSTurretLaserFire` | ? | Chaingun, FlakWeapon, HeavyCannon, LaserWeapon, LightFlameWeapon, SmallArms | 0 | 0 | exception-bearing retired family; name and legacy signals disagree |
| `TankBusterBeamCannon` | ? | MediumCannon, RailgunWeapon, TankDestroyerCannon | 0 | 0 | name and legacy signals disagree |
| `Tentacle` | CannonHE_Heavy | Chaingun, RailgunWeapon, SwordWeapon | 0 | 0 | canonical and legacy signals disagree |
| `ThermobaricMaverick` | MissileAP_Medium | HeavyMissile, MediumFlameWeapon, NuclearWarhead | 0 | 0 | exception-bearing retired family; name and canonical destination disagree; canonical and legacy signals disagree; name and legacy signals disagree |
| `TowerMissile` | ? | FlakWeapon, Grenade, HeavyAAWeapon, HeavyMissile, MediumMissile, ShrapnelWeapon, TankDestroyerCannon | 1 | 0 | legacy tie: Concussion/MissileHE |
| `TurretGunBlackMarket` | Concussion_Medium | HeavyBomb, HeavyCannon | 0 | 0 | legacy tie: Demolition/CannonHE |
| `Type89PlasmaCannon` | ? | HeavyCannon, TankDestroyerCannon, TeslaWeapon | 0 | 0 | name and legacy signals disagree |
| `VoidRayBeam` | ? | Chaingun, FlakWeapon, LaserWeapon, LightMissile, SmallArms, TeslaWeapon | 0 | 0 | name and legacy signals disagree |
| `VolkovMagneticWeapon` | CannonHE_Heavy | Grenade, MediumChemicalWeapon | 9 | 8 | canonical and legacy signals disagree |
| `VolkovMagneticWeaponIncendiaryNuclearShells` | CannonHE_Heavy | MediumFlameWeapon, NuclearWarhead | 0 | 0 | exception-bearing retired family; name and canonical destination disagree; canonical and legacy signals disagree |
| `VolkovMagneticWeaponIncendiaryTesla` | CannonHE_Heavy | MediumFlameWeapon, TeslaWeapon | 0 | 0 | legacy tie: Flame/Tesla; name and canonical destination disagree |
| `VultureGrenade` | CannonHE_Medium | FlakWeapon, Grenade, SmallArms, TankDestroyerCannon | 0 | 0 | legacy tie: Flak/Bullet/CannonAP; name and canonical destination disagree |
| `WaveforceCannonDistortedBeam1` | ? | HeavyCannon, TeslaWeapon | 0 | 0 | name and legacy signals disagree |
| `WhiteRabbitGatling` | Bullet_Medium | FlakWeapon, HeavyCannon, MediumMissile, SmallArms | 0 | 0 | legacy tie: Flak/Bullet |
| `WyvernRockets` | ? | FlakWeapon, HeavyAAWeapon, HeavyBomb, MediumChemicalWeapon, MediumFlameWeapon, MediumMissile | 0 | 0 | multiple inherited family/tier destinations; legacy tie: Chemical/Flame; name and canonical destination disagree |
| `YakNuclearBomb` | ? | HeavyBomb, HeavyChemicalWeapon, HeavyFlameWeapon, NuclearWarhead | 0 | 0 | exception-bearing retired family |
| `YakTeslaBomb` | ? | HeavyBomb, HeavyChemicalWeapon, HeavyFlameWeapon, TeslaWeapon | 0 | 0 | legacy tie: Chemical/Flame/Tesla |
| `autogun_tank` | ? | Chaingun, FlakWeapon, HeavyAAWeapon, MediumMissile | 1 | 0 | multiple inherited family/tier destinations; legacy tie: Bullet/Flak/MissileAA |
| `bfg10kCannon` | ? | LaserWeapon, MagicWeapon, RailgunWeapon, TeslaChargedWeapon | 1 | 0 | exception-bearing retired family; legacy tie: Laser/Magic/Tesla |
| `edenRailgun` | ? | HeavyCannon, LightChemicalWeapon, MediumCannon, RailgunWeapon, ShrapnelWeapon, TankDestroyerCannon | 2 | 0 | legacy tie: CannonHE/Chemical |
| `eye_bomberguy` | ? | Grenade, HeavyBomb, MediumChemicalWeapon, MediumFlameWeapon, ShrapnelWeapon | 0 | 0 | legacy tie: Concussion/Chemical/Flame |
| `facedancer_grenade` | ? | Grenade, HeavyBomb, LightFlameWeapon, LightMissile, MediumChemicalWeapon, MediumMissile, ShrapnelWeapon | 0 | 0 | exception-bearing retired family; multiple inherited family/tier destinations; legacy tie: Concussion/Flame/MissileHE/Chemical; name and canonical destination disagree |
| `ixian_airdrone` | MissileAP_Heavy | FlakWeapon, HeavyBomb, LightChemicalWeapon, MediumMissile, TankDestroyerCannon | 1 | 0 | canonical and legacy signals disagree |
| `ixian_farasha` | ? | HeavyFlameWeapon, RailgunWeapon, TeslaWeapon | 1 | 1 | legacy tie: Flame/Tesla |
| `ra120mm2` | CannonHE_Heavy | LightChemicalWeapon, MediumFlameWeapon | 1 | 0 | legacy tie: Chemical/Flame |
| `ra1_allies_chronovortex` | ? | HeavyBomb, MagicWeapon | 0 | 0 | exception-bearing retired family |
| `ra2roktgun` | Bullet_Medium | FlakWeapon, LightMissile, SmallArms | 1 | 1 | legacy tie: Flak/Bullet |
| `v1rocketsThermobaric` | ? | Grenade, HeavyFlameWeapon, MediumMissile | 0 | 0 | name and legacy signals disagree |
| `wc2_dwarf_Rifle` | ? | MediumMissile, RailgunWeapon, TankDestroyerCannon | 0 | 0 | name and legacy signals disagree |
| `wc2_tower_arrow` | ? | ArrowWeapon, Chaingun, RailgunWeapon | 1 | 0 | name and legacy signals disagree |
| `wc2arrowFire` | CannonHE_Medium | ArrowWeapon, Chaingun, FlakWeapon, Grenade, TankDestroyerCannon | 3 | 0 | legacy tie: Bullet/Flak/CannonAP; name and canonical destination disagree |
| `wc2catapultFire` | ? | Grenade, HeavyBomb, MediumFlameWeapon, ShrapnelWeapon | 0 | 0 | legacy tie: Concussion/Flame |
| `wc2gryphonFireVisible` | ? | HeavyBomb, LightMissile, MediumFlameWeapon, TeslaChargedWeapon | 0 | 0 | legacy tie: Flame/Tesla |
| `wc2highArrowFire` | CannonHE_Medium | HeavyAAWeapon, HeavyMissile, LaserWeapon | 1 | 0 | name and canonical destination disagree; canonical and legacy signals disagree; name and legacy signals disagree |
| `wc2mageBlizzard_Projectile` | ? | FlakWeapon, LightChemicalWeapon, LightFlameWeapon, ShrapnelWeapon | 0 | 0 | exception-bearing retired family; legacy tie: Chemical/Flame |
