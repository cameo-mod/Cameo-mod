# Weapon suffix audit (DESIGN.md º1)

X1 elite weapons not ending _elite: **234**
X2 EMP weapons not ending _EMP: **157**
X3 AA weapons not ending _AA: **52**
X4 deprecated E suffix (informational): **123**

## X1 ù Elite weapons not following _elite convention
| File | Line | Actor | Trait | Weapon |
|---|---|---|---|---|
| ContentPacks/RedAlert2/Allies/yaml/infantry.yaml | 274 | ra2_allies_guardiangi | Armament@ELITE | GuardianGIMGE |
| ContentPacks/RedAlert2/Allies/yaml/infantry.yaml | 278 | ra2_allies_guardiangi | Armament@ELITE2 | GuardianGIMG2E |
| ContentPacks/RedAlert2/Allies/yaml/infantry.yaml | 495 | ra2_allies_sniper | Armament@ELITE | RA2AWPE |
| ContentPacks/RedAlert2/Allies/yaml/infantry.yaml | 503 | ra2_allies_sniper | Armament@GARRISONEDELITE | RA2AWPE |
| ContentPacks/RedAlert2/Allies/yaml/infantry.yaml | 572 | ra2_allies_tanyaii | Armament@ELITE | RA2DoublePistolsE |
| ContentPacks/RedAlert2/Allies/yaml/infantry.yaml | 592 | ra2_allies_tanyaii | Armament@GARRISONEDELITE | RA2DoublePistolsE |
| ContentPacks/RedAlert2/Allies/yaml/infantry.yaml | 649 | ra2_allies_seal | Armament@ELITE | RA2MP5E |
| ContentPacks/RedAlert2/Allies/yaml/infantry.yaml | 657 | ra2_allies_seal | Armament@GARRISONEDELITE | RA2MP5E |
| ContentPacks/RedAlert2/Allies/yaml/vehicles.yaml | 681 | ra2_allies_prismtank | Armament@EliteCharge | PrismTankChargeE |
| ContentPacks/RedAlert2/Shared/yaml/misc.yaml | 2160 | ra2_c_ifv | Armament@elite | RA2GattlingMG2 |
| ContentPacks/RedAlert2/Shared/yaml/misc.yaml | 2178 | ra2_c_ifv | Armament@eliteAA | RA2GattlingMG2AA |
| ContentPacks/RedAlert2/Shared/yaml/misc.yaml | 2240 | ra2_c_hum | Armament@elite | RA2GattlingMG2 |
| ContentPacks/RedAlert2/Soviets/yaml/aircraft.yaml | 58 | ra2_soviets_kirovairship | Armament@PRIMARYNuclearELITE | RA2KirovBomb_nuclear_E |
| ContentPacks/RedAlert2/Soviets/yaml/aircraft.yaml | 161 | ra2_soviets_siegechopper | Armament@DeployedEliteRad | RA160mmE_elite_rad |
| ContentPacks/RedAlert2/Soviets/yaml/aircraft.yaml | 167 | ra2_soviets_siegechopper | Armament@DeployedEliteFire | RA160mmE_elite_fire |
| ContentPacks/RedAlert2/Soviets/yaml/aircraft.yaml | 173 | ra2_soviets_siegechopper | Armament@DeployedEliteTesla | RA160mmE_elite_tesla |
| ContentPacks/RedAlert2/Soviets/yaml/aircraft.yaml | 309 | ra2_soviets_migbomber | Armament@PRIMARYELITE | MigMissilesE |
| ContentPacks/RedAlert2/Soviets/yaml/aircraft.yaml | 317 | ra2_soviets_migbomber | Armament@PRIMARYELITERad | MigMissilesE_rad |
| ContentPacks/RedAlert2/Soviets/yaml/aircraft.yaml | 325 | ra2_soviets_migbomber | Armament@PRIMARYELITERad | MigMissilesE_fire |
| ContentPacks/RedAlert2/Soviets/yaml/aircraft.yaml | 333 | ra2_soviets_migbomber | Armament@PRIMARYELITERad | MigMissilesE_tesla |
| ContentPacks/RedAlert2/Soviets/yaml/aircraft.yaml | 349 | ra2_soviets_migbomber | Armament@AAELITE | MigMissiles_AA_ELITE |
| ContentPacks/RedAlert2/Soviets/yaml/infantry.yaml | 153 | ra2_soviets_conscript | Armament@ELITE | RA2M1CarbineE |
| ContentPacks/RedAlert2/Soviets/yaml/infantry.yaml | 462 | ra2_soviets_desolator | Armament@ELITE | RA2RadBeamWeaponE |
| ContentPacks/RedAlert2/Soviets/yaml/infantry.yaml | 545 | ra2_soviets_boris | Armament@ELITE | BorisAKME |
| ContentPacks/RedAlert2/Soviets/yaml/infantry.yaml | 563 | ra2_soviets_boris | Armament@GARRISONEDELITE | BorisAKME |
| ContentPacks/RedAlert2/Soviets/yaml/vehicles.yaml | 118 | ra2_soviets_rhinoheavytank | Armament@PRIMARYELITERad | RA2120mm_elite_rad |
| ContentPacks/RedAlert2/Soviets/yaml/vehicles.yaml | 127 | ra2_soviets_rhinoheavytank | Armament@PRIMARYELITEFire | RA2120mm_elite_fire |
| ContentPacks/RedAlert2/Soviets/yaml/vehicles.yaml | 136 | ra2_soviets_rhinoheavytank | Armament@PRIMARYELITETesla | RA2120mm_elite_tesla |
| ContentPacks/RedAlert2/Soviets/yaml/vehicles.yaml | 340 | ra2_soviets_apocalypsetank | Armament@PRIMARYELITERad | RA2120xmm_elite_rad |
| ContentPacks/RedAlert2/Soviets/yaml/vehicles.yaml | 349 | ra2_soviets_apocalypsetank | Armament@PRIMARYELITEFire | RA2120xmm_elite_fire |
| ContentPacks/RedAlert2/Soviets/yaml/vehicles.yaml | 358 | ra2_soviets_apocalypsetank | Armament@PRIMARYELITETesla | RA2120xmm_elite_tesla |
| ContentPacks/RedAlert2/Soviets/yaml/vehicles.yaml | 429 | ra2_soviets_v3rocketlauncher | Armament@PRIMARYELITE | V3LaunchE |
| ContentPacks/RedAlert2/Soviets/yaml/vehicles.yaml | 654 | ra2_soviets_terrordrone | Armament@Elite | RA2DroneJumpE |
| ContentPacks/RedAlert2/Yuri/yaml/infantry.yaml | 116 | yuri_brute | Armament@ELITE | RA2BrutePunchE |
| ContentPacks/RedAlert2/Yuri/yaml/infantry.yaml | 184 | yuri_virus | Armament@ELITE | RA2VirusgunE |
| ContentPacks/RedAlert2/Yuri/yaml/infantry.yaml | 200 | yuri_virus | Armament@GARRISONEDELITE | RA2VirusgunE |
| ContentPacks/RedAlert2/Yuri/yaml/infantry.yaml | 611 | yuri_biotrooper | Armament@ELITE | RA2ChemsprayE |
| ContentPacks/RedAlert2/Yuri/yaml/infantry.yaml | 623 | yuri_biotrooper | Armament@GARRISONEDELITE | RA2ChemsprayE |
| ContentPacks/RedAlert2Mod/AsianAlliance/yaml/aircraft.yaml | 128 | asianalliance_pelican | Armament@PRIMARYELITE | AsianPelicanMissileE |
| ContentPacks/RedAlert2Mod/AsianAlliance/yaml/infantry.yaml | 36 | asianalliance_asianmilitia | Armament@ELITE | RA2AsianShotgunE |
| ContentPacks/RedAlert2Mod/AsianAlliance/yaml/infantry.yaml | 44 | asianalliance_asianmilitia | Armament@GRENADEELITE | AsianGrenadeE |
| ContentPacks/RedAlert2Mod/AsianAlliance/yaml/infantry.yaml | 53 | asianalliance_asianmilitia | Armament@GARRISONEDELITE | RA2AsianShotgunE |
| ContentPacks/RedAlert2Mod/AsianAlliance/yaml/infantry.yaml | 118 | asianalliance_asiantankkiller | Armament@PRIMARYELITEUpgrade | AsianTankKillerRocket_elite_upgrade |
| ContentPacks/RedAlert2Mod/AsianAlliance/yaml/infantry.yaml | 135 | asianalliance_asiantankkiller | Armament@GARRISONEDELITEUpgrade | AsianTankKillerRocket_elite_upgrade |
| ContentPacks/RedAlert2Mod/AsianAlliance/yaml/infantry.yaml | 180 | asianalliance_plasmatrooper | Armament@ELITE | AsianSinglePlasmaE |
| ContentPacks/RedAlert2Mod/AsianAlliance/yaml/infantry.yaml | 189 | asianalliance_plasmatrooper | Armament@GARRISONEDELITE | AsianSinglePlasmaE |
| ContentPacks/RedAlert2Mod/AsianAlliance/yaml/infantry.yaml | 238 | asianalliance_asianflametrooper | Armament@ELITE | AsianFlamerTurret |
| ContentPacks/RedAlert2Mod/AsianAlliance/yaml/infantry.yaml | 301 | asianalliance_fanatic | Armament@ELITE | RA2AsianShotgunFanatic3 |
| ContentPacks/RedAlert2Mod/AsianAlliance/yaml/infantry.yaml | 315 | asianalliance_fanatic | Armament@GARRISONEDELITE | RA2AsianShotgunFanatic3 |
| ContentPacks/RedAlert2Mod/AsianAlliance/yaml/infantry.yaml | 402 | asianalliance_veteranarcher | Armament@ELITE | AsianMaidenBowE |
| ContentPacks/RedAlert2Mod/AsianAlliance/yaml/infantry.yaml | 411 | asianalliance_veteranarcher | Armament@GARRISONEDELITE | AsianMaidenBowE |
| ContentPacks/RedAlert2Mod/AsianAlliance/yaml/infantry.yaml | 455 | asianalliance_shinobi | Armament@ELITE | AsianNinjaStarE |
| ContentPacks/RedAlert2Mod/AsianAlliance/yaml/infantry.yaml | 464 | asianalliance_shinobi | Armament@GARRISONEDELITE | AsianNinjaStarE |
| ContentPacks/RedAlert2Mod/AsianAlliance/yaml/infantry.yaml | 535 | asianalliance_alligator | Armament@ELITE | AsianCrocBiteE |
| ContentPacks/RedAlert2Mod/AsianAlliance/yaml/naval.yaml | 196 | gunb.asian | Armament@AntiSubElite | DepthCharge |
| ContentPacks/RedAlert2Mod/AsianAlliance/yaml/vehicles.yaml | 466 | ptnk.asian | Armament@ELITE | AsianTwinPlasmaE |
| ContentPacks/RedAlert2Mod/AsianAlliance/yaml/vehicles.yaml | 523 | asianalliance_viper | Armament@ELITE | AsianChemicalE |
| ContentPacks/RedAlert2Mod/AsianAlliance/yaml/vehicles.yaml | 641 | asianalliance_howitzer | Armament@ELITE | AsianHowitzerCannonE |
| ContentPacks/RedAlert2Mod/AsianAlliance/yaml/vehicles.yaml | 711 | asianalliance_asianflametank | Armament@ELITE | AsianFlamerTankE |
| ContentPacks/RedAlert2Mod/AsianAlliance/yaml/vehicles.yaml | 841 | asianalliance_railguntank | Armament@ELITE | AsianRailTank2 |
| ContentPacks/RedAlert2Mod/AsianAlliance/yaml/vehicles.yaml | 886 | asianalliance_heavyrailguntank | Armament@ELITE | AsianRailTank3 |
| ContentPacks/RedAlert2Mod/Consortium/yaml/aircraft.yaml | 70 | steelconsortium_twister | Armament@PRIMARYELITE | SteelTwisterMissilesE |
| ContentPacks/RedAlert2Mod/Consortium/yaml/aircraft.yaml | 402 | steelconsortium_skyhammer | Armament@elite | SteelAirTurretE |
| ContentPacks/RedAlert2Mod/Consortium/yaml/aircraft.yaml | 491 | steelconsortium_cloudbreaker | Armament@PRIMARYELITE | SteelCruiserCannonsE |
| ContentPacks/RedAlert2Mod/Consortium/yaml/aircraft.yaml | 503 | steelconsortium_cloudbreaker | Armament@CANNONELITE | SteelCruiserArtilleryE |
| ContentPacks/RedAlert2Mod/Consortium/yaml/infantry.yaml | 31 | steelconsortium_clonetrooper | Armament@ELITE | SteelCloneGunE |
| ContentPacks/RedAlert2Mod/Consortium/yaml/infantry.yaml | 40 | steelconsortium_clonetrooper | Armament@GARRISONEDELITE | SteelCloneGunE |
| ContentPacks/RedAlert2Mod/Consortium/yaml/infantry.yaml | 49 | steelconsortium_clonetrooper | Armament@UpgradeELITE | SteelCloneGunEResonance |
| ContentPacks/RedAlert2Mod/Consortium/yaml/infantry.yaml | 58 | steelconsortium_clonetrooper | Armament@UpgradeGARRISONEDELITE | SteelCloneGunEResonance |
| ContentPacks/RedAlert2Mod/Consortium/yaml/infantry.yaml | 110 | steelconsortium_hoverboardgrenadier | Armament@ELITE | SteelMakoGunE |
| ContentPacks/RedAlert2Mod/Consortium/yaml/infantry.yaml | 120 | steelconsortium_hoverboardgrenadier | Armament@GARRISONEDELITE | SteelMakoGunE |
| ContentPacks/RedAlert2Mod/Consortium/yaml/infantry.yaml | 130 | steelconsortium_hoverboardgrenadier | Armament@UpgradeELITE | SteelMakoGunEMPE |
| ContentPacks/RedAlert2Mod/Consortium/yaml/infantry.yaml | 140 | steelconsortium_hoverboardgrenadier | Armament@UpgradeGARRISONEDELITE | SteelMakoGunEMPE |
| ContentPacks/RedAlert2Mod/Consortium/yaml/infantry.yaml | 208 | steelconsortium_quantummissiletrooper | Armament@PRIMARYELITE | SteelInfRailgunE |
| ContentPacks/RedAlert2Mod/Consortium/yaml/infantry.yaml | 216 | steelconsortium_quantummissiletrooper | Armament@UpgradePRIMARYELITE | SteelInfRailgunEMPE |
| ContentPacks/RedAlert2Mod/Consortium/yaml/infantry.yaml | 230 | steelconsortium_quantummissiletrooper | Armament@DEPLOYEDELITE | SteelInfRailgunE |
| ContentPacks/RedAlert2Mod/Consortium/yaml/infantry.yaml | 242 | steelconsortium_quantummissiletrooper | Armament@UpgradeDEPLOYEDELITE | SteelInfRailgunEMPE |
| ContentPacks/RedAlert2Mod/Consortium/yaml/infantry.yaml | 252 | steelconsortium_quantummissiletrooper | Armament@GARRISONEDELITE | SteelInfRailgunE |
| ContentPacks/RedAlert2Mod/Consortium/yaml/infantry.yaml | 260 | steelconsortium_quantummissiletrooper | Armament@UpgradeGARRISONEDELITE | SteelInfRailgunEMPE |
| ContentPacks/RedAlert2Mod/Consortium/yaml/infantry.yaml | 372 | steelconsortium_steelrunner | Armament@ELITE | SteelRunnerPistolsE |
| ContentPacks/RedAlert2Mod/Consortium/yaml/infantry.yaml | 380 | steelconsortium_steelrunner | Armament@GARRISONEDELITE | SteelRunnerPistolsE |
| ContentPacks/RedAlert2Mod/Consortium/yaml/infantry.yaml | 387 | steelconsortium_steelrunner | Armament@UpgradeELITE | SteelRunnerPistolsEResonance |
| ContentPacks/RedAlert2Mod/Consortium/yaml/infantry.yaml | 396 | steelconsortium_steelrunner | Armament@UpgradeGARRISONEDELITE | SteelRunnerPistolsEResonance |
| ContentPacks/RedAlert2Mod/Consortium/yaml/vehicles.yaml | 58 | steelconsortium_katytank | Armament@ELITE | SteelKatyCannonsE |
| ContentPacks/RedAlert2Mod/Consortium/yaml/vehicles.yaml | 76 | steelconsortium_katytank | Armament@UpgradeELITE | SteelKatyCannonsEMPE |
| ContentPacks/RedAlert2Mod/Consortium/yaml/vehicles.yaml | 154 | steelconsortium_defenderbot | Armament@PRIMARYELITE | SteelInfRailgunE |
| ContentPacks/RedAlert2Mod/Consortium/yaml/vehicles.yaml | 162 | steelconsortium_defenderbot | Armament@UpgradePRIMARYELITE | SteelInfRailgunEMPE |
| ContentPacks/RedAlert2Mod/Consortium/yaml/vehicles.yaml | 261 | steelconsortium_stalker | Armament@elite | SteelStalkerRailgunE |
| ContentPacks/RedAlert2Mod/Consortium/yaml/vehicles.yaml | 836 | steelconsortium_mako | Armament@ELITE | SteelMakoGunE |
| ContentPacks/RedAlert2Mod/Consortium/yaml/vehicles.yaml | 848 | steelconsortium_mako | Armament@UpgradeELITE | SteelMakoGunEMPE |
| ContentPacks/RedAlert2Mod/Consortium/yaml/vehicles.yaml | 1230 | steelconsortium_megalodon | Armament@ELITE | SteelMegaSwordEMP |
| ContentPacks/RedAlert2Mod/Consortium/yaml/vehicles.yaml | 1233 | steelconsortium_megalodon | Armament@elite | SteelMegaSwordE |
| ContentPacks/RedAlert2Mod/Consortium/yaml/vehicles.yaml | 1477 | steelconsortium_dagger | Armament@ELITE | SteelDaggerCannonE |
| ContentPacks/RedAlert2Mod/FutureTech/yaml/aircraft.yaml | 43 | futuretech_twister | Armament@PRIMARYELITE | SteelTwisterMissilesE |
| ContentPacks/RedAlert2Mod/FutureTech/yaml/aircraft.yaml | 224 | futuretech_harbingergunship | Armament@PRIMARYELITE | FutureHarbingerCannonE |
| ContentPacks/RedAlert2Mod/FutureTech/yaml/aircraft.yaml | 236 | futuretech_harbingergunship | Armament@AAELITE | HarbingerChaingunsE |
| ContentPacks/RedAlert2Mod/FutureTech/yaml/infantry.yaml | 49 | futuretech_enforcer | Armament@ELITE | FutureEnforcerShotgunE |
| ContentPacks/RedAlert2Mod/FutureTech/yaml/infantry.yaml | 53 | futuretech_enforcer | Armament@ELITE2 | FutureEnforcerShotgunDeployedE |
| ContentPacks/RedAlert2Mod/FutureTech/yaml/infantry.yaml | 59 | futuretech_enforcer | Armament@GARRISONEDELITE | FutureEnforcerShotgunDeployedE |
| ContentPacks/RedAlert2Mod/FutureTech/yaml/infantry.yaml | 175 | futuretech_javelinsoldier | Armament@ELITE | FutureJavelinRocketsE |
| ContentPacks/RedAlert2Mod/FutureTech/yaml/infantry.yaml | 179 | futuretech_javelinsoldier | Armament@ELITE2 | FutureJavelinRocketsDeployedE |
| ContentPacks/RedAlert2Mod/FutureTech/yaml/infantry.yaml | 185 | futuretech_javelinsoldier | Armament@GARRISONEDELITE | FutureJavelinRocketsDeployedE |
| ContentPacks/RedAlert2Mod/FutureTech/yaml/infantry.yaml | 342 | futuretech_blackwidow | Armament@ELITE | BlackWidowPistolsE |
| ContentPacks/RedAlert2Mod/FutureTech/yaml/infantry.yaml | 350 | futuretech_blackwidow | Armament@GARRISONEDELITE | BlackWidowPistolsE |
| ContentPacks/RedAlert2Mod/FutureTech/yaml/infantry.yaml | 551 | futuretech_scoutdroid | Armament@ELITE | Future_Wheel_MGE |
| ContentPacks/RedAlert2Mod/FutureTech/yaml/infantry.yaml | 623 | futuretech_shotgundroid | Armament@ELITE | ShotgunAttackRobotGunE |
| ContentPacks/RedAlert2Mod/FutureTech/yaml/infantry.yaml | 635 | futuretech_shotgundroid | Armament@GARRISONEDELITE | ShotgunAttackRobotGunE |
| ContentPacks/RedAlert2Mod/FutureTech/yaml/infantry.yaml | 709 | futuretech_cannondroid | Armament@ELITE | CannonAttackRobotGunE |
| ContentPacks/RedAlert2Mod/FutureTech/yaml/infantry.yaml | 783 | futuretech_missiledroid | Armament@ELITE | MissileAttackRobotGunE |
| ContentPacks/RedAlert2Mod/FutureTech/yaml/vehicles.yaml | 124 | futuretech_plasmastrider | Armament@ELITE | FutureMechPlasmaE |
| ContentPacks/RedAlert2Mod/FutureTech/yaml/vehicles.yaml | 322 | futuretech_guardiantank | Armament@elite | GuardianTankCannonE |
| ContentPacks/RedAlert2Mod/FutureTech/yaml/vehicles.yaml | 336 | futuretech_guardiantank | Armament@debuffelite | Future_MBT_DebuffLaserE |
| ContentPacks/RedAlert2Mod/FutureTech/yaml/vehicles.yaml | 471 | futuretech_phalanxwip | Armament@PRIMARYELITE | RA2RTruckRocket |
| ContentPacks/RedAlert2Mod/FutureTech/yaml/vehicles.yaml | 635 | futuretech_oriontank | Armament@elite | OrionRailgunE |
| ContentPacks/RedAlert2Mod/FutureTech/yaml/vehicles.yaml | 940 | futuretech_futuretank | Armament@elite | FutureTankCannonsE |
| ContentPacks/RedAlert2Mod/Naxis/yaml/aircraft.yaml | 149 | naxis_bf109 | Armament@ELITE | NaxPlanegunE |
| ContentPacks/RedAlert2Mod/Naxis/yaml/aircraft.yaml | 238 | naxis_me262 | Armament@RocketsELITE | NaxPlaneRocketsE |
| ContentPacks/RedAlert2Mod/Naxis/yaml/aircraft.yaml | 252 | naxis_me262 | Armament@ELITE | NaxPlanegunE |
| ContentPacks/RedAlert2Mod/Naxis/yaml/aircraft.yaml | 341 | naxis_transportzeppelin | Armament@elite | NaxQuadCannonE |
| ContentPacks/RedAlert2Mod/Naxis/yaml/aircraft.yaml | 355 | naxis_transportzeppelin | Armament@eliteAA | NaxQuadCannonAAE |
| ContentPacks/RedAlert2Mod/Naxis/yaml/infantry.yaml | 31 | naxis_naxiriflesoldier | Armament@ELITE | NaxiRifleE |
| ContentPacks/RedAlert2Mod/Naxis/yaml/infantry.yaml | 41 | naxis_naxiriflesoldier | Armament@GARRISONEDELITE | NaxiRifleE |
| ContentPacks/RedAlert2Mod/Naxis/yaml/infantry.yaml | 84 | naxis_naxiriflerecruit | Armament@ELITE | NaxiRifleConsE |
| ContentPacks/RedAlert2Mod/Naxis/yaml/infantry.yaml | 94 | naxis_naxiriflerecruit | Armament@GARRISONEDELITE | NaxiRifleConsE |
| ContentPacks/RedAlert2Mod/Naxis/yaml/infantry.yaml | 137 | naxis_sssoldier | Armament@ELITE | NaxiMP40E |
| ContentPacks/RedAlert2Mod/Naxis/yaml/infantry.yaml | 147 | naxis_sssoldier | Armament@GARRISONEDELITE | NaxiMP40E |
| ContentPacks/RedAlert2Mod/Naxis/yaml/infantry.yaml | 190 | naxis_naxiflamer | Armament@ELITE | NaxiFlamerTroopE |
| ContentPacks/RedAlert2Mod/Naxis/yaml/infantry.yaml | 200 | naxis_naxiflamer | Armament@GARRISONEDELITE | NaxiFlamerTroopE |
| ContentPacks/RedAlert2Mod/Naxis/yaml/infantry.yaml | 246 | naxis_naximercenarysniper | Armament@ELITE | NaxiSniperE |
| ContentPacks/RedAlert2Mod/Naxis/yaml/infantry.yaml | 256 | naxis_naximercenarysniper | Armament@GARRISONEDELITE | NaxiSniperE |
| ContentPacks/RedAlert2Mod/Naxis/yaml/infantry.yaml | 298 | naxis_panzerschreck | Armament@ELITE | NaxiShrekE |
| ContentPacks/RedAlert2Mod/Naxis/yaml/infantry.yaml | 350 | naxis_panzerfausttrooper | Armament@ELITE | NaxiShrekConsE |
| ContentPacks/RedAlert2Mod/Naxis/yaml/infantry.yaml | 360 | naxis_panzerfausttrooper | Armament@GARRISONEDELITE | NaxiShrekConsE |
| ContentPacks/RedAlert2Mod/Naxis/yaml/infantry.yaml | 401 | naxis_skymage | Armament@ELITE | SkyMageCannonE |
| ContentPacks/RedAlert2Mod/Naxis/yaml/infantry.yaml | 407 | naxis_skymage | Armament@ELITEAA | SkyMageCannonAAE |
| ContentPacks/RedAlert2Mod/Naxis/yaml/infantry.yaml | 483 | naxis_slaveoverseer | Armament@ELITE | NaxiRifleE |
| ContentPacks/RedAlert2Mod/Naxis/yaml/infantry.yaml | 495 | naxis_slaveoverseer | Armament@GARRISONEDELITE | NaxiRifleE |
| ContentPacks/RedAlert2Mod/Naxis/yaml/infantry.yaml | 563 | frank.nax | Armament@ELITE | RA2BrutePunchE |
| ContentPacks/RedAlert2Mod/Naxis/yaml/infantry.yaml | 626 | naxis_naximachinegunners | Armament@DEPLOYEDELITE | NaxiWW2MachinegunnerE |
| ContentPacks/RedAlert2Mod/Naxis/yaml/infantry.yaml | 637 | naxis_naximachinegunners | Armament@GARRISONEDELITE | NaxiWW2MachinegunnerE |
| ContentPacks/RedAlert2Mod/Naxis/yaml/infantry.yaml | 741 | naxis_portableflak | Armament@DEPLOYEDELITE | PortableFlakE |
| ContentPacks/RedAlert2Mod/Naxis/yaml/infantry.yaml | 752 | naxis_portableflak | Armament@GARRISONEDELITE | PortableFlakE |
| ContentPacks/RedAlert2Mod/Naxis/yaml/infantry.yaml | 838 | undead.nax | Armament@ELITE | NaxiRifleE |
| ContentPacks/RedAlert2Mod/Naxis/yaml/infantry.yaml | 848 | undead.nax | Armament@GARRISONEDELITE | NaxiRifleE |
| ContentPacks/RedAlert2Mod/Naxis/yaml/infantry.yaml | 897 | naxis_coneheadsknights | Armament@GARRISONEDELITE | NaxiRifleConsE |
| ContentPacks/RedAlert2Mod/Naxis/yaml/infantry.yaml | 942 | conehead2.nax | Armament@ELITE | RA2PortaTesla |
| ContentPacks/RedAlert2Mod/Naxis/yaml/infantry.yaml | 952 | conehead2.nax | Armament@GARRISONEDELITE | RA2PortaTesla |
| ContentPacks/RedAlert2Mod/Naxis/yaml/infantry.yaml | 1007 | naxis_antitankcannon | Armament@ELITE | NaxiAntiTankCannonE |
| ContentPacks/RedAlert2Mod/Naxis/yaml/infantry.yaml | 1130 | alien.nax | Armament@ELITE | NaxiAlienPistolE |
| ContentPacks/RedAlert2Mod/Naxis/yaml/infantry.yaml | 1140 | alien.nax | Armament@GARRISONEDELITE | NaxiAlienPistolE |
| ContentPacks/RedAlert2Mod/Naxis/yaml/vehicles.yaml | 215 | wirbelwind.nax | Armament@elite | NaxQuadCannonE |
| ContentPacks/RedAlert2Mod/Naxis/yaml/vehicles.yaml | 233 | wirbelwind.nax | Armament@eliteAA | NaxQuadCannonAAE |
| ContentPacks/RedAlert2Mod/Naxis/yaml/vehicles.yaml | 589 | naxis_grille | Armament@elite | NaxGrilleArtyE |
| ContentPacks/RedAlert2Mod/Naxis/yaml/vehicles.yaml | 641 | naxis_brummbr | Armament@elite | NaxBrummbarArtyE |
| ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/aircraft.yaml | 150 | schwarzermond_spacezeppelin | Armament@elite | NaxiBeetleLaserE |
| ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/aircraft.yaml | 164 | schwarzermond_spacezeppelin | Armament@eliteAA | NaxiBeetleLaserAAE |
| ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/aircraft.yaml | 178 | schwarzermond_spacezeppelin | Armament@eliteUP | Lunar_YellowBeetleLaser |
| ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/aircraft.yaml | 192 | schwarzermond_spacezeppelin | Armament@eliteAA_UP | Lunar_YellowBeetleLaserAA |
| ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/aircraft.yaml | 206 | schwarzermond_spacezeppelin | Armament@eliteAMP | Lunar_AmplifiedBeetleLaser |
| ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/aircraft.yaml | 220 | schwarzermond_spacezeppelin | Armament@eliteAA_AMP | Lunar_AmplifiedBeetleLaserAA |
| ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/aircraft.yaml | 423 | schwarzermond_haunebuii | Armament@ELITE | NaxHaenebuQuadCannonE |
| ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/aircraft.yaml | 511 | schwarzermond_haunebuiii | Armament@ELITE | NaxHaenebuQuadCannonE |
| ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/infantry.yaml | 35 | schwarzermond_bermensch | Armament@ELITE | ▄bermenschLaserE |
| ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/infantry.yaml | 43 | schwarzermond_bermensch | Armament@GARRISONEDELITE | ▄bermenschLaserE |
| ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/infantry.yaml | 51 | schwarzermond_bermensch | Armament@ELITE_UP | Lunar_YellowUbermenschLaserE |
| ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/infantry.yaml | 59 | schwarzermond_bermensch | Armament@GARRISONEDELITE_UP | Lunar_YellowUbermenschLaserE |
| ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/infantry.yaml | 67 | schwarzermond_bermensch | Armament@ELITE_AMP | Lunar_AmplifiedUbermenschLaserE |
| ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/infantry.yaml | 75 | schwarzermond_bermensch | Armament@GARRISONEDELITE_AMP | Lunar_AmplifiedUbermenschLaserE |
| ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/infantry.yaml | 119 | schwarzermond_parzival | Armament@ELITE | BlackHoleMakerE |
| ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/infantry.yaml | 129 | schwarzermond_parzival | Armament@GARRISONEDELITE | BlackHoleMakerE |
| ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/infantry.yaml | 185 | schwarzermond_noidmgarmor | Armament@ELITE | NaxiMP40LaserE |
| ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/infantry.yaml | 193 | schwarzermond_noidmgarmor | Armament@ELITE_UP | Lunar_YellowMP40LaserE |
| ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/infantry.yaml | 201 | schwarzermond_noidmgarmor | Armament@ELITE_AMP | Lunar_AmplifiedMP40LaserE |
| ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/infantry.yaml | 264 | schwarzermond_noidharvester | Armament@ELITE | NaxiMP40LaserE |
| ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/infantry.yaml | 412 | schwarzermond_lunarsoldier | Armament@ELITE | NaxiRifleLaserE |
| ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/infantry.yaml | 420 | schwarzermond_lunarsoldier | Armament@ELITE_UP | Lunar_YellowRifleLaserE |
| ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/infantry.yaml | 428 | schwarzermond_lunarsoldier | Armament@ELITE_AMP | Lunar_AmplifiedRifleLaserE |
| ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/infantry.yaml | 436 | schwarzermond_lunarsoldier | Armament@GARRISONEDELITE | NaxiRifleLaserE |
| ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/infantry.yaml | 444 | schwarzermond_lunarsoldier | Armament@GARRISONEDELITE_UP | Lunar_YellowRifleLaserE |
| ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/infantry.yaml | 452 | schwarzermond_lunarsoldier | Armament@GARRISONEDELITE_AMP | Lunar_AmplifiedRifleLaserE |
| ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/infantry.yaml | 492 | schwarzermond_lunarrocket | Armament@ELITE | NaxCorrosionRocketTrooperE |
| ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/infantry.yaml | 500 | schwarzermond_lunarrocket | Armament@GARRISONEDELITE | NaxCorrosionRocketTrooperE |
| ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/vehicles.yaml | 68 | schwarzermond_dalek | Armament@ELITE | DalekCannonE |
| ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/vehicles.yaml | 234 | schwarzermond_lunarpanzer | Armament@elite | LunarPanzerCannonE |
| ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/vehicles.yaml | 321 | schwarzermond_lunartiger | Armament@elite | LunarTigerCannonE |
| ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/vehicles.yaml | 403 | schwarzermond_neojagdpanzer | Armament@elite | LunarNaxiJadgDestroyerE |
| ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/vehicles.yaml | 477 | schwarzermond_lunargrille | Armament@elite | NaxGrilleArtyE |
| ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/vehicles.yaml | 549 | schwarzermond_korruptesbiest | Armament@ELITE | NaxCorrosionBeastE |
| ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/vehicles.yaml | 612 | schwarzermond_crystaltank | Armament@ELITE | NaxCrystalLeechE |
| ContentPacks/RedAlert2Mod/Syndicate/yaml/aircraft.yaml | 127 | latinsyndicate_yakovlev | Armament@ELITE | YakovlevCannonE |
| ContentPacks/RedAlert2Mod/Syndicate/yaml/aircraft.yaml | 238 | latinsyndicate_mig21 | Armament@PRIMARYELITE | MigMissilesE |
| ContentPacks/RedAlert2Mod/Syndicate/yaml/aircraft.yaml | 254 | latinsyndicate_mig21 | Armament@AAELITE | MigMissiles_AA_ELITE |
| ContentPacks/RedAlert2Mod/Syndicate/yaml/buildings.yaml | 586 | latinsyndicate_latinsentrygun | Armament@ELITE | LatinSentryMGE |
| ContentPacks/RedAlert2Mod/Syndicate/yaml/infantry.yaml | 29 | latinsyndicate_latinmilitia | Armament@ELITE | RA2MiliAK47E |
| ContentPacks/RedAlert2Mod/Syndicate/yaml/infantry.yaml | 35 | latinsyndicate_latinmilitia | Armament@MolotovELITE | RA2MolotovE |
| ContentPacks/RedAlert2Mod/Syndicate/yaml/infantry.yaml | 44 | latinsyndicate_latinmilitia | Armament@GARRISONEDELITE | RA2MiliAK47E |
| ContentPacks/RedAlert2Mod/Syndicate/yaml/infantry.yaml | 56 | latinsyndicate_latinmilitia | Armament@MolotovGARRISONEDELITE | RA2MolotovE |
| ContentPacks/RedAlert2Mod/Syndicate/yaml/infantry.yaml | 163 | latinsyndicate_grenademonkey | Armament@ELITE | LatinMonkeyGrenade3 |
| ContentPacks/RedAlert2Mod/Syndicate/yaml/infantry.yaml | 175 | latinsyndicate_grenademonkey | Armament@GARRISONEDELITE | LatinMonkeyGrenade3 |
| ContentPacks/RedAlert2Mod/Syndicate/yaml/infantry.yaml | 241 | latinsyndicate_latinflametrooper | Armament@ELITE | SyndicateFireballLauncherE |
| ContentPacks/RedAlert2Mod/Syndicate/yaml/infantry.yaml | 251 | latinsyndicate_latinflametrooper | Armament@GARRISONEDELITE | SyndicateFireballLauncherE |
| ContentPacks/RedAlert2Mod/Syndicate/yaml/infantry.yaml | 308 | latinsyndicate_narco | Armament@GrenadeELITE | NarcoGrenadeE |
| ContentPacks/RedAlert2Mod/Syndicate/yaml/infantry.yaml | 320 | latinsyndicate_narco | Armament@PistolELITE | RA2NarcoPistolsE |
| ContentPacks/RedAlert2Mod/Syndicate/yaml/infantry.yaml | 332 | latinsyndicate_narco | Armament@GrenadeGARRISONEDELITE | NarcoGrenadeE |
| ContentPacks/RedAlert2Mod/Syndicate/yaml/infantry.yaml | 344 | latinsyndicate_narco | Armament@PistolGARRISONEDELITE | RA2NarcoPistolsE |
| ContentPacks/RedAlert2Mod/Syndicate/yaml/infantry.yaml | 412 | latinsyndicate_freedomfighter | Armament@ELITE | RA2FreedomAK47E |
| ContentPacks/RedAlert2Mod/Syndicate/yaml/infantry.yaml | 421 | latinsyndicate_freedomfighter | Armament@ROCKETELITE | RA2FreedomRocketE |
| ContentPacks/RedAlert2Mod/Syndicate/yaml/infantry.yaml | 432 | latinsyndicate_freedomfighter | Armament@GARRISONEDELITE | RA2FreedomAK47E |
| ContentPacks/RedAlert2Mod/Syndicate/yaml/infantry.yaml | 442 | latinsyndicate_freedomfighter | Armament@RocketGARRISONEDELITE | RA2FreedomRocketE |
| ContentPacks/RedAlert2Mod/Syndicate/yaml/naval.yaml | 37 | triton.latin | Armament@ELITE | LatinSentryMGE |
| ContentPacks/RedAlert2Mod/Syndicate/yaml/naval.yaml | 175 | rammax.latin | Armament@ELITE | Rammax_Sabot |
| ContentPacks/RedAlert2Mod/Syndicate/yaml/vehicles.yaml | 34 | latinsyndicate_mortarbike | Armament@ELITE | RA2MortarBikeE |
| ContentPacks/RedAlert2Mod/Syndicate/yaml/vehicles.yaml | 200 | latinsyndicate_raiderbuggy | Armament@Elite | LatinBuggyMGE |
| ContentPacks/RedAlert2Mod/Syndicate/yaml/vehicles.yaml | 212 | latinsyndicate_raiderbuggy | Armament@GatlingElite | LatinBuggyChaingunE |
| ContentPacks/RedAlert2Mod/Syndicate/yaml/vehicles.yaml | 224 | latinsyndicate_raiderbuggy | Armament@RocketElite | LatinBuggyRocketE |
| ContentPacks/RedAlert2Mod/Syndicate/yaml/vehicles.yaml | 288 | latinsyndicate_tortugatank | Armament@Elite | LatinBuggyMGE |
| ContentPacks/RedAlert2Mod/Syndicate/yaml/vehicles.yaml | 300 | latinsyndicate_tortugatank | Armament@GatlingElite | LatinBuggyChaingunE |
| ContentPacks/RedAlert2Mod/Syndicate/yaml/vehicles.yaml | 367 | latinsyndicate_rushertank | Armament@elite | RA2Gren60mmE |
| ContentPacks/RedAlert2Mod/Syndicate/yaml/vehicles.yaml | 456 | latinsyndicate_smokertank | Armament@elite | LatinSmokerCannonE |
| ContentPacks/RedAlert2Mod/Syndicate/yaml/vehicles.yaml | 562 | latinsyndicate_diablo | Armament@Elite | DiabloCannonE |
| ContentPacks/RedAlert2Mod/Syndicate/yaml/vehicles.yaml | 680 | latinsyndicate_latinapc | Armament@FlakELITE | RA2APCFlakCannonE |
| ContentPacks/RedAlert2Mod/Syndicate/yaml/vehicles.yaml | 694 | latinsyndicate_latinapc | Armament@FlakAAELITE | RA2APCFlakCannonAAE |
| ContentPacks/RedAlert2Mod/Syndicate/yaml/vehicles.yaml | 708 | latinsyndicate_latinapc | Armament@MachinegunELITE | RA2APCMachineGunE |
| ContentPacks/RedAlert2Mod/Syndicate/yaml/vehicles.yaml | 722 | latinsyndicate_latinapc | Armament@MachinegunAAELITE | RA2APCMachineGunAAE |
| ContentPacks/RedAlert2Mod/Syndicate/yaml/vehicles.yaml | 736 | latinsyndicate_latinapc | Armament@RocketsELITE | RA2APCRocketE |
| ContentPacks/RedAlert2Mod/Syndicate/yaml/vehicles.yaml | 750 | latinsyndicate_latinapc | Armament@RocketsAAELITE | RA2APCRocketAAE |
| ContentPacks/RedAlert2Mod/Syndicate/yaml/vehicles.yaml | 990 | latinsyndicate_missiletruck | Armament@PRIMARYELITE | RA2RTruckRocket |
| ContentPacks/RedAlert2Mod/Syndicate/yaml/vehicles.yaml | 1063 | latinsyndicate_burrito | Armament@PRIMARYELITE | RA2RBurritoRocket |
| ContentPacks/RedAlert2Mod/Syndicate/yaml/vehicles.yaml | 1134 | latinsyndicate_lars | Armament@PRIMARYELITE | RA2LarsRocket |
| rules/heroes.yaml | 1117 | TSNASHWABIKE | Armament@ELITE | TSBikeMissileNashwaE |
| rules/redalert2.yaml | 2576 | ra2_c_ifv | Armament@elite | RA2GattlingMG2 |
| rules/redalert2.yaml | 2594 | ra2_c_ifv | Armament@eliteAA | RA2GattlingMG2AA |
| rules/redalert2.yaml | 2656 | ra2_c_hum | Armament@elite | RA2GattlingMG2 |
| rules/valentine.yaml | 1917 | cute_kirov | Armament@PRIMARYELITE | CuteKirovBombE |

## X2 ù EMP weapons not following _EMP convention
| File | Line | Weapon |
|---|---|---|
| ContentPacks/D2k/Ixian/yaml/aircraft.yaml | 401 | ixian_empbomber |
| ContentPacks/D2k/Ixian/yaml/promotions.yaml | 134 | ixian_promotion_unlockixianempbomber |
| ContentPacks/D2k/Ixian/yaml/sequences.yaml | 1489 | ixian_empbomber |
| ContentPacks/RedAlert/Shared/yaml/sequences.yaml | 375 | raharvempty |
| ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 731 | PortaTeslaEMP |
| ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 812 | TTankZapEMP |
| ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 826 | TTankZapEMPArcTeslaFragment1 |
| ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 844 | TTankZapEMPArcTeslaFragment2 |
| ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 869 | TTankZap2EMP |
| ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 883 | TTankZap2EMPArcTeslaFragment1 |
| ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 901 | TTankZap2EMPArcTeslaFragment2 |
| ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 926 | TeslaZapEMP |
| ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 940 | TeslaZapEMPArcTeslaFragment1 |
| ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 958 | TeslaZapEMPArcTeslaFragment2 |
| ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 546 | AsianPhotonCannonEMP |
| ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 564 | AsianQuasarAGEMP |
| ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 582 | AsianQuasarAAEMP |
| ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 600 | AsianPunisherAGEMP |
| ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 620 | AsianQuasarBoatAGEMP |
| ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 640 | AsianQuasarBoatAAEMP |
| ContentPacks/RedAlert2Mod/Consortium/yaml/aircraft.yaml | 281 | steelconsortium_empressstation |
| ContentPacks/RedAlert2Mod/Consortium/yaml/sequences.yaml | 1286 | steelconsortium_empressstation |
| ContentPacks/RedAlert2Mod/Consortium/yaml/upgrades.yaml | 62 | steelconsortium_upgrade_unlockempcannon |
| ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 162 | ConsortiumMissileSystemEMP |
| ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 212 | SteelQuantumCannonEMP |
| ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 435 | SteelMakoGunEMP |
| ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 491 | SteelMakoGunEMPE |
| ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 831 | SteelInfRailgunEMP |
| ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 944 | SteelInfRailgunEMPE |
| ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 949 | SteelScalpelRailgunEMPAA |
| ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 1050 | SteelMegaSwordEMP |
| ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 1152 | SteelAirTurretEMP |
| ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 1317 | SteelQuantumTurretRailEMP |
| ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 1531 | SteelKatyCannonsEMP |
| ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 1601 | SteelKatyCannonsEMPE |
| ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 1634 | SteelStalkerRailgunEMP |
| ContentPacks/RedAlert2Mod/Syndicate/yaml/buildings.yaml | 135 | latinsyndicate_latinempradar |
| ContentPacks/RedAlert2Mod/Syndicate/yaml/sequences.yaml | 358 | latinsyndicate_latinempradar |
| ContentPacks/RedAlert2Mod/Syndicate/yaml/upgrades.yaml | 64 | latinsyndicate_upgrade_unlockempcannon |
| ContentPacks/StarCraft/Protoss/yaml/buildings.yaml | 638 | protoss_templararchives |
| ContentPacks/StarCraft/Protoss/yaml/infantry.yaml | 106 | protoss_hightemplar |
| ContentPacks/StarCraft/Protoss/yaml/infantry.yaml | 210 | protoss_darktemplar |
| ContentPacks/StarCraft/Protoss/yaml/sequences.yaml | 189 | protoss_templararchives |
| ContentPacks/StarCraft/Protoss/yaml/sequences.yaml | 327 | protoss_hightemplar |
| ContentPacks/StarCraft/Protoss/yaml/sequences.yaml | 359 | protoss_darktemplar |
| ContentPacks/StarCraft/Protoss/yaml/weapons.yaml | 597 | CorsairEMP |
| ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 1731 | ScienceVesselEMP |
| ContentPacks/TKM/TKM/yaml/sequences.yaml | 812 | tkm_templateharvesterraname |
| ContentPacks/TKM/TKM/yaml/vehicles.yaml | 28 | tkm_templateharvesterraname |
| ContentPacks/TiberianDawn/GDI/yaml/infantry.yaml | 165 | td_gdi_empgrenadier |
| ContentPacks/TiberianDawn/GDI/yaml/promotions.yaml | 5 | td_gdi_promotion_unlockempgrenadier |
| ContentPacks/TiberianDawn/GDI/yaml/sequences.yaml | 926 | td_gdi_empgrenadier |
| ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 763 | EMPGrenade |
| ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 855 | EMPGrenadeExplode |
| ContentPacks/TiberianDawn/Nod/yaml/buildings.yaml | 227 | td_nod_templeofnod |
| ContentPacks/TiberianDawn/Nod/yaml/buildings.yaml | 350 | td_nod_templeprime |
| ContentPacks/TiberianDawn/Nod/yaml/sequences.yaml | 229 | td_nod_templeofnod |
| ContentPacks/TiberianDawn/Nod/yaml/sequences.yaml | 266 | td_nod_templeprime |
| ContentPacks/TiberianSun/GDI/yaml/defenses.yaml | 260 | ts_gdi_empulsecannon |
| ContentPacks/TiberianSun/GDI/yaml/sequences.yaml | 425 | ts_gdi_mobileemp |
| ContentPacks/TiberianSun/GDI/yaml/vehicles.yaml | 182 | ts_gdi_mobileemp |
| ContentPacks/TiberianSun/GDI/yaml/weapons.yaml | 385 | TSEMPZapWeapon |
| ContentPacks/TiberianSun/GDI/yaml/weapons.yaml | 420 | TSEMPMine |
| ContentPacks/TiberianSun/GDI/yaml/weapons.yaml | 817 | TSMobileEMP |
| ContentPacks/Warcraft2/Orcs/yaml/buildings.yaml | 431 | wc2_orcs_templeofthedamned |
| ContentPacks/Warcraft2/Orcs/yaml/sequences.yaml | 114 | wc2_orcs_templeofthedamned |
| audio/darkreign.yaml | 502 | DREMPDeviceSound |
| audio/redalert2mod.yaml | 238 | SteelEmpressVoice |
| audio/voices.yaml | 695 | SCHighTemplarVoice |
| audio/voices.yaml | 706 | SCDarkTemplarVoice |
| audio/warcraft1.yaml | 128 | orctemplevoice |
| audio/warcraft2.yaml | 441 | wc2orctempleofthedamnedvoice |
| bits/d2k/arrakis.yaml | 135 | Templates |
| chrome.yaml | 917 | progressbar-thumb-empty |
| chrome.yaml | 2658 | sidebar-swempire |
| chrome.yaml | 2661 | sidebar-button-swempire |
| chrome.yaml | 2664 | sidebar-button-swempire-hover |
| chrome.yaml | 2667 | sidebar-button-swempire-pressed |
| chrome.yaml | 2669 | sidebar-button-swempire-highlighted |
| chrome.yaml | 2672 | sidebar-button-swempire-highlighted-hover |
| chrome.yaml | 2675 | sidebar-button-swempire-highlighted-pressed |
| chrome.yaml | 2677 | sidebar-button-swempire-disabled |
| chrome.yaml | 2680 | sidebar-button-swempire-highlighted-disabled |
| rules/darkreign.yaml | 5439 | drempdisable |
| rules/generals.yaml | 9560 | upchemp |
| rules/generals.yaml | 14329 | leangchemp |
| rules/heroes.yaml | 2642 | Temple |
| rules/heroes.yaml | 2695 | TempleBot |
| rules/iok.yaml | 844 | iokscudtemple |
| rules/mcvmarket.yaml | 302 | MMSWEMPIREMCV |
| rules/outpost2.yaml | 1443 | EDEN_CARGOTRUCK_EMPTY |
| rules/outpost2.yaml | 2544 | PLYMOUTH_CARGOTRUCK_EMPTY |
| rules/shockwave.yaml | 3225 | susaempmissiledefender |
| rules/shockwave.yaml | 9060 | sglmobilesupplytruck_empty |
| rules/shockwave.yaml | 9066 | sglmobilesupplytruck_deployed_empty |
| rules/shockwave.yaml | 13199 | schempmig |
| rules/warcraft1.yaml | 1088 | wc_o_damnedtemple |
| rules/wh40k.yaml | 2169 | wh40kupguardemperor |
| sequences/actiblizz.yaml | 1 | actitemplararchives |
| sequences/d2k.yaml | 5435 | d2k_emperor_worm |
| sequences/darkreign.yaml | 4597 | drempdisable |
| sequences/generals.yaml | 1490 | chempicon |
| sequences/heroes.yaml | 458 | temple |
| sequences/iok.yaml | 375 | iokscudtemple |
| sequences/lostunits.yaml | 734 | bhreddragonemp |
| sequences/outpost2.yaml | 712 | eden_cargotruck_empty |
| sequences/outpost2.yaml | 1643 | plymouth_cargotruck_empty |
| sequences/redalert.yaml | 379 | raharvempty |
| sequences/shockwave.yaml | 3656 | schempmig |
| sequences/starcraft.yaml | 8 | scxelnagatemple |
| sequences/structures.yaml | 322 | td_nod_templeofnod |
| sequences/tiberiandawn.yaml | 906 | bhreddragonemp |
| sequences/warcraft1.yaml | 375 | wc_o_damnedtemple |
| sequences/wh40k.yaml | 3867 | wh40kupguardemperor |
| tilesets/arrakis.yaml | 124 | Templates |
| tilesets/arrakis2.yaml | 112 | Templates |
| tilesets/barren.yaml | 151 | Templates |
| tilesets/cameo.yaml | 181 | Templates |
| tilesets/caribic.yaml | 136 | Templates |
| tilesets/desert.yaml | 144 | Templates |
| tilesets/jungle.yaml | 140 | Templates |
| tilesets/outpost2.yaml | 136 | Templates |
| tilesets/ra2_temperat.yaml | 150 | Templates |
| tilesets/ra_desert.yaml | 147 | Templates |
| tilesets/ra_interior.yaml | 107 | Templates |
| tilesets/ra_snow.yaml | 144 | Templates |
| tilesets/ra_temperat.yaml | 144 | Templates |
| tilesets/snow.yaml | 144 | Templates |
| tilesets/temperat.yaml | 144 | Templates |
| tilesets/tibfields.yaml | 136 | Templates |
| tilesets/wc2_summer.yaml | 143 | Templates |
| tilesets/wc2_sunset.yaml | 143 | Templates |
| tilesets/wc2_swamp.yaml | 143 | Templates |
| tilesets/winter.yaml | 140 | Templates |
| weapons/d2k.yaml | 183 | Emperor_Sardaukar_E |
| weapons/d2k.yaml | 1699 | emperor_sardaukar_chief_c4 |
| weapons/darkreign.yaml | 1322 | DREMPDevice |
| weapons/explosions.yaml | 101 | UnitExplodeHarvEmpty |
| weapons/generals.yaml | 1589 | USAEMPPatriotMissAG |
| weapons/generals.yaml | 1614 | USAEMPPatriotMissAA |
| weapons/monsters.yaml | 884 | Empty |
| weapons/other.yaml | 1273 | Empty |
| weapons/outpost2.yaml | 174 | edenEMP |
| weapons/outpost2.yaml | 284 | edenEMPAA |
| weapons/outpost2.yaml | 290 | edenTigerEMP |
| weapons/outpost2.yaml | 296 | edenTigerEMPAA |
| weapons/outpost2.yaml | 302 | edenEMPGP |
| weapons/outpost2.yaml | 620 | plymouthEMP |
| weapons/outpost2.yaml | 730 | plymouthEMPAA |
| weapons/outpost2.yaml | 737 | plymouthEMPTiger |
| weapons/shockwave.yaml | 193 | SUSAMLRSEMP |
| weapons/shockwave.yaml | 1194 | SUSAEMPMissileDefenderAG |
| weapons/shockwave.yaml | 1203 | SUSAEMPMissileDefenderStructure |
| weapons/shockwave.yaml | 1212 | SUSAEMPMissileDefenderAA |
| weapons/shockwave.yaml | 1222 | SUSAEMPMissileDefenderGarrisoned |
| weapons/weapons.yaml | 3661 | UnitExplodeHarvEmpty |
| weapons/weapons.yaml | 4736 | TSEMPulseCannon |

## X3 ù AA-only weapons not following _AA convention
| File | Line | Weapon | ValidTargets |
|---|---|---|---|
| ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 49 | zsu_23 | Air |
| ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 365 | BallistaSingleShotAir | Air |
| ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 375 | BallistaSingleShotAirEnergized | Air |
| ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 369 | ReimuOrbLauncher | Air |
| ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 441 | MagicOrbHailstormSpawner | Air |
| ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 3720 | Nike | Air |
| ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 703 | RA2Medusa | Air |
| ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 640 | RA2TRIPODLAZER | Air |
| ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 2609 | AccurateCloudSpawner | Air |
| ContentPacks/RedAlert2/Soviets/yaml/weapons.yaml | 709 | RA2MammothTusk | Air |
| ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 458 | AsianPhotonCannon | Air |
| ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 120 | ConsortiumMissileSystem | Air |
| ContentPacks/StarCraft/Protoss/yaml/weapons.yaml | 461 | ScoutRockets | Air |
| ContentPacks/StarCraft/Protoss/yaml/weapons.yaml | 548 | CorsairFlash | Air |
| ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 984 | GoliathRockets | Air |
| ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 1100 | WraithRockets | Air |
| ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 1218 | ValkyrieRockets | Air |
| ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 1555 | MissileTurret | Air |
| ContentPacks/StarCraft/Zerg/yaml/weapons.yaml | 444 | SCScourgeExplosion | Air |
| ContentPacks/StarCraft/Zerg/yaml/weapons.yaml | 1173 | Spore | Air |
| ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 172 | SkyshieldCannon | Air |
| ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 545 | A10CarrierMissiles | Air |
| ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 1581 | FirehawkMissiles | Air |
| ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 485 | Dragon | Air |
| ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 64 | DarkObeliskLaser | Air |
| ContentPacks/TiberianSun/GDI/yaml/weapons.yaml | 603 | TSMammothTusk2 | Air |
| ContentPacks/TiberianSun/GDI/yaml/weapons.yaml | 630 | TSMammothTusk2II | Air |
| ContentPacks/TiberianSun/GDI/yaml/weapons.yaml | 709 | TSGDIRedEye | Air |
| ContentPacks/TiberianSun/Nod/yaml/weapons.yaml | 184 | TSNODRedEye | Air |
| weapons/advacewars.yaml | 700 | AWOlafFreeze1 | Chronobeamable, Air |
| weapons/advacewars.yaml | 945 | AWEaglePower1 | Air |
| weapons/advancewars.yaml | 696 | AWOlafFreeze1 | Chronobeamable, Air |
| weapons/advancewars.yaml | 930 | AWEaglePower1 | Air |
| weapons/d2k.yaml | 2089 | d2k_aircraft_eater | Air |
| weapons/darkreign.yaml | 727 | DRMetalFragments | Air |
| weapons/generals.yaml | 687 | GLQuadCannonAir | Air |
| weapons/heroes.yaml | 404 | angelarrow | Air |
| weapons/missiles.yaml | 48 | Dragon | Air |
| weapons/monsters.yaml | 72 | ZBruteRock | Air |
| weapons/redalert.yaml | 369 | ReimuOrbLauncher | Air |
| weapons/redalert.yaml | 441 | MagicOrbHailstormSpawner | Air |
| weapons/redalert2.yaml | 645 | RA2TRIPODLAZER | Air |
| weapons/redalert2.yaml | 2614 | AccurateCloudSpawner | Air |
| weapons/redalert2mod.yaml | 963 | NaxiMeteorSpawner | Air |
| weapons/shockwave.yaml | 1861 | SGLGadFlyMissile | Air |
| weapons/sow.yaml | 413 | sow_mech_avenger | Air |
| weapons/sow.yaml | 1046 | satelliteprotection | Air |
| weapons/sow.yaml | 1223 | sow_antiair_tower | Air |
| weapons/sow.yaml | 1379 | sow_vulcano_tower | Air |
| weapons/tiberiansun.yaml | 932 | TSRedEye2 | Air |
| weapons/valentine.yaml | 247 | planBFire | Air |
| weapons/win98.yaml | 59 | paintbrushfire | Air |

## X4 ù Weapons with deprecated E suffix (informational)
| File | Line | Weapon |
|---|---|---|
| ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 1277 | SCUDNUKE |
| ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 1300 | RA2BrutePunchE |
| ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 1644 | MigMissilesE |
| ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 1689 | MigMissiles_AA_ELITE |
| ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 1839 | RA2SCUDELITE |
| ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 2011 | RA2M1CarbineE |
| ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 2051 | RA2ZOMBIE |
| ContentPacks/RedAlert2/Soviets/yaml/weapons.yaml | 794 | RA2FlakTrackGunE |
| ContentPacks/RedAlert2/Soviets/yaml/weapons.yaml | 830 | RA2FlakTrackAAGunE |
| ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 824 | Lunar_GreenTigerCannonE |
| ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 848 | Lunar_GreenJadgDestroyerE |
| ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 872 | Lunar_GreenGrilleArtyE |
| ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 170 | RA2NarcoAKME |
| ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 188 | RA2Narco60mmE |
| ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 626 | LatinRusherRocketE |
| ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 733 | RA2GrenadePackE |
| ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 754 | LatinSmokerRocketE |
| ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 818 | DiabloCannonAAE |
| ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 37 | td_gdi_commando_sniperE |
| ContentPacks/TiberianDawn/Shared/yaml/buildings.yaml | 1 | NUKE |
| ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 704 | CabalHunterKillerLasersE |
| rules/advancewars.yaml | 1424 | MMAWBLACKHOLE |
| rules/challenge.yaml | 289 | XPYLE |
| rules/civilian.yaml | 601 | VICE |
| rules/classicdoom.yaml | 263 | WOLFDOME |
| rules/classicdoom.yaml | 694 | WOLFSTATUE |
| rules/heroes.yaml | 1064 | TSNASHWABIKE |
| rules/iok.yaml | 81 | IOKHOLE.IOKNUKE |
| rules/iok.yaml | 183 | IOKHOLE.IOKSITE |
| rules/iok.yaml | 234 | IOKHOLE.IOKPALACE |
| rules/iok.yaml | 384 | IOKNUKE |
| rules/iok.yaml | 985 | IOKSITE |
| rules/iok.yaml | 1171 | IOKJETPLANE |
| rules/lostunits.yaml | 54 | RNBIKE |
| rules/lostunits.yaml | 683 | BHNUKE |
| rules/lostunits.yaml | 876 | BHEYE |
| rules/lostunits.yaml | 986 | BHBIKE |
| rules/misc.yaml | 9 | CRATE |
| rules/misc.yaml | 201 | WWCRATE |
| rules/misc.yaml | 225 | WCRATE |
| rules/misc.yaml | 230 | SCRATE |
| rules/misc.yaml | 235 | MONEYCRATE |
| rules/misc.yaml | 287 | MONEYCRATE.LARGE |
| rules/misc.yaml | 489 | FLARE |
| rules/misc.yaml | 760 | MINE |
| rules/misc.yaml | 820 | SPLITBLUE |
| rules/misc.yaml | 882 | GMINE |
| rules/misc.yaml | 1016 | RAILMINE |
| rules/monsters.yaml | 196 | PVICE |
| rules/monsters.yaml | 216 | RA2ZOMBIE |
| rules/n64.yaml | 461 | N64PYLE |
| rules/n64.yaml | 1145 | N64EYE |
| rules/n64.yaml | 2117 | N64BIKE |
| rules/outpost2.yaml | 847 | EDEN_FACTORY_STRUCTURE |
| rules/outpost2.yaml | 900 | EDEN_AGRIDOME |
| rules/outpost2.yaml | 914 | EDEN_RESIDENCE |
| rules/outpost2.yaml | 942 | EDEN_FACTORY_VEHICLE |
| rules/outpost2.yaml | 954 | EDEN_GARAGE |
| rules/outpost2.yaml | 1161 | EDEN_SMELTER_RARE |
| rules/outpost2.yaml | 1842 | EDEN_LYNX_STARFLARE |
| rules/outpost2.yaml | 1894 | EDEN_TIGER_STARFLARE |
| rules/outpost2.yaml | 2109 | PLYMOUTH_AGRIDOME |
| rules/outpost2.yaml | 2123 | PLYMOUTH_RESIDENCE |
| rules/outpost2.yaml | 2151 | PLYMOUTH_FACTORY_VEHICLE |
| rules/outpost2.yaml | 2164 | PLYMOUTH_GARAGE |
| rules/outpost2.yaml | 2333 | PLYMOUTH_SMELTER_RARE |
| rules/outpost2.yaml | 2378 | PLYMOUTH_GP_MICROWAVE |
| rules/outpost2.yaml | 2786 | PLYMOUTH_LYNX_MICROWAVE |
| rules/outpost2.yaml | 2828 | PLYMOUTH_TIGER_MICROWAVE |
| rules/outpost2.yaml | 3135 | PLYMOUTH_LYNX_STARFLARE |
| rules/outpost2.yaml | 3190 | PLYMOUTH_TIGER_STARFLARE |
| rules/sc2k.yaml | 48 | SC2KABANDONED.FACTORYSTRUCTURE |
| rules/sc2k.yaml | 146 | SC2KABANDONED.POWERMICROWAVE |
| rules/sc2k.yaml | 208 | SC2KABANDONED.DOMICILE |
| rules/sc2k.yaml | 348 | SC2KABANDONED.BEACONPOLICE |
| rules/sc2k.yaml | 360 | SC2KABANDONED.BEACONFIRE |
| rules/sc2k.yaml | 403 | SC2KFACTORYSTRUCTURE |
| rules/sc2k.yaml | 701 | SC2KPOWERMICROWAVE |
| rules/sc2k.yaml | 910 | SC2KDOMICILE |
| rules/sc2k.yaml | 1553 | SC2KFIRE |
| rules/sc2k.yaml | 1798 | SC2KJETPLANE |
| rules/sc2k.yaml | 2131 | SC2KBEACONPOLICE |
| rules/sc2k.yaml | 2216 | SC2KBEACONFIRE |
| rules/simcity.yaml | 272 | CITYAMBULANCE |
| rules/simcity.yaml | 1013 | CITYNUKE |
| rules/simcity.yaml | 1560 | CITYAMUSE |
| rules/starcraft.yaml | 359 | SCSPIDERMINE |
| rules/starcraft.yaml | 514 | SCWRAITHDRONE |
| rules/starcraft.yaml | 1066 | SCSCOURGEDRONE |
| rules/tech.yaml | 488 | C2KNUKE |
| rules/test.yaml | 165 | REVE |
| rules/test.yaml | 605 | WEAPONSCRATE |
| rules/tiberiaalliances.yaml | 342 | TASNIPE |
| rules/tiberiaalliances.yaml | 390 | TAMUTSNIPE |
| rules/tiberiansun.yaml | 147 | INVISIBLEPLANE |
| rules/tomorrow.yaml | 1283 | DTNUKE |
| rules/tomorrow.yaml | 1429 | DTSTRIKE |
| rules/tomorrow.yaml | 1686 | DTPYLE |
| rules/tomorrow.yaml | 2382 | DTEYE |
| rules/trees.yaml | 488 | RUSHOUSE |
| rules/valentine.yaml | 84 | LOVECRATE |
| rules/win98.yaml | 891 | WIN98_HARDWARE |
| rules/win98.yaml | 1126 | WIN98_ERASE |
| rules/win98.yaml | 1429 | WIN98_MOUSE |
| rules/win98.yaml | 1471 | WIN98_MINE |
| rules/worms.yaml | 167 | WNUKE |
| rules/worms.yaml | 444 | WSHEEPSTATUE |
| rules/worms.yaml | 591 | WTIMEMACHINE |
| rules/worms.yaml | 872 | WORMS_AASITE |
| rules/worms.yaml | 914 | WORMS_CANDLE |
| rules/worms.yaml | 1107 | WORMGRENADE |
| rules/worms.yaml | 1282 | WORMDYNAMITE |
| rules/worms.yaml | 1444 | WORMS_MOLE |
| rules/worms.yaml | 1929 | WORMS_APACHE |
| rules/xmas.yaml | 44 | XMASCRATE |
| rules/xmas.yaml | 73 | EVILCRATE |
| weapons/redalert2.yaml | 1844 | RA2SCUDELITE |
| weapons/redalert2.yaml | 2056 | RA2ZOMBIE |
| weapons/shockwave.yaml | 76 | SUSABurtonSniperHE |
| weapons/shockwave.yaml | 230 | SUSAMLRSHE |
| weapons/tiberiansun.yaml | 1237 | TSSniperE |
| weapons/tiberiansun.yaml | 1259 | MutDualWieldE |
| weapons/tiberiansun.yaml | 1264 | MutAPRifleE |

