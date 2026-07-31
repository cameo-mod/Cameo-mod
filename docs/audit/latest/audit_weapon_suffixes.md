# Weapon suffix audit (DESIGN.md §1)

X1 elite weapons not ending _elite: **30**
X2 EMP weapons not ending _EMP: **10**
X3 AA weapons not ending _AA: **0**
X4 deprecated E suffix (informational): **19**
X5 suffix ordering violations: **0**

## X1 — Elite weapons not following _elite convention
| File | Line | Actor | Trait | Weapon |
|---|---|---|---|---|
| ContentPacks/RedAlert2/Shared/yaml/misc.yaml | 2293 | ra2_c_ifv | Armament@elite | RA2GattlingMG2 |
| ContentPacks/RedAlert2/Shared/yaml/misc.yaml | 2311 | ra2_c_ifv | Armament@eliteAA | RA2GattlingMG2_AA |
| ContentPacks/RedAlert2/Shared/yaml/misc.yaml | 2372 | ra2_c_hum | Armament@elite | RA2GattlingMG2 |
| ContentPacks/RedAlert2/Soviets/yaml/aircraft.yaml | 58 | ra2_soviets_kirovairship | Armament@PRIMARYNuclearELITE | RA2KirovBomb_nuclear_E |
| ContentPacks/RedAlert2/Soviets/yaml/aircraft.yaml | 317 | ra2_soviets_migbomber | Armament@PRIMARYELITERad | MigMissilesE_rad |
| ContentPacks/RedAlert2/Soviets/yaml/aircraft.yaml | 325 | ra2_soviets_migbomber | Armament@PRIMARYELITERad | MigMissilesE_fire |
| ContentPacks/RedAlert2/Soviets/yaml/aircraft.yaml | 333 | ra2_soviets_migbomber | Armament@PRIMARYELITERad | MigMissilesE_tesla |
| ContentPacks/RedAlert2/Soviets/yaml/aircraft.yaml | 349 | ra2_soviets_migbomber | Armament@AAELITE | MigMissiles_AA_ELITE |
| ContentPacks/RedAlert2Mod/AsianAlliance/yaml/infantry.yaml | 244 | asianalliance_asianflametrooper | Armament@ELITE | AsianFlamerTurret |
| ContentPacks/RedAlert2Mod/AsianAlliance/yaml/naval.yaml | 196 | gunb.asian | Armament@AntiSubElite | DepthCharge |
| ContentPacks/RedAlert2Mod/AsianAlliance/yaml/vehicles.yaml | 869 | asianalliance_railguntank | Armament@ELITE | AsianRailTank2 |
| ContentPacks/RedAlert2Mod/AsianAlliance/yaml/vehicles.yaml | 914 | asianalliance_heavyrailguntank | Armament@ELITE | AsianRailTank3 |
| ContentPacks/RedAlert2Mod/Consortium/yaml/vehicles.yaml | 1244 | steelconsortium_megalodon | Armament@ELITE | SteelMegaSword_EMP |
| ContentPacks/RedAlert2Mod/FutureTech/yaml/vehicles.yaml | 475 | futuretech_phalanxwip | Armament@PRIMARYELITE | RA2RTruckRocket |
| ContentPacks/RedAlert2Mod/Naxis/yaml/infantry.yaml | 1056 | conehead2.nax | Armament@ELITE | RA2PortaTesla |
| ContentPacks/RedAlert2Mod/Naxis/yaml/infantry.yaml | 1066 | conehead2.nax | Armament@GARRISONEDELITE | RA2PortaTesla |
| ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/aircraft.yaml | 176 | schwarzermond_spacezeppelin | Armament@eliteUP | Lunar_YellowBeetleLaser |
| ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/aircraft.yaml | 190 | schwarzermond_spacezeppelin | Armament@eliteAA_UP | Lunar_YellowBeetleLaser_AA |
| ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/aircraft.yaml | 204 | schwarzermond_spacezeppelin | Armament@eliteAMP | Lunar_AmplifiedBeetleLaser |
| ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/aircraft.yaml | 218 | schwarzermond_spacezeppelin | Armament@eliteAA_AMP | Lunar_AmplifiedBeetleLaser_AA |
| ContentPacks/RedAlert2Mod/Syndicate/yaml/aircraft.yaml | 254 | latinsyndicate_mig21 | Armament@AAELITE | MigMissiles_AA_ELITE |
| ContentPacks/RedAlert2Mod/Syndicate/yaml/infantry.yaml | 169 | latinsyndicate_grenademonkey | Armament@ELITE | LatinMonkeyGrenade3 |
| ContentPacks/RedAlert2Mod/Syndicate/yaml/infantry.yaml | 181 | latinsyndicate_grenademonkey | Armament@GARRISONEDELITE | LatinMonkeyGrenade3 |
| ContentPacks/RedAlert2Mod/Syndicate/yaml/naval.yaml | 175 | rammax.latin | Armament@ELITE | Rammax_Sabot |
| ContentPacks/RedAlert2Mod/Syndicate/yaml/vehicles.yaml | 990 | latinsyndicate_missiletruck | Armament@PRIMARYELITE | RA2RTruckRocket |
| ContentPacks/RedAlert2Mod/Syndicate/yaml/vehicles.yaml | 1063 | latinsyndicate_burrito | Armament@PRIMARYELITE | RA2RBurritoRocket |
| ContentPacks/RedAlert2Mod/Syndicate/yaml/vehicles.yaml | 1134 | latinsyndicate_lars | Armament@PRIMARYELITE | RA2LarsRocket |
| rules/redalert2.yaml | 2599 | ra2_c_ifv | Armament@elite | RA2GattlingMG2 |
| rules/redalert2.yaml | 2617 | ra2_c_ifv | Armament@eliteAA | RA2GattlingMG2_AA |
| rules/redalert2.yaml | 2678 | ra2_c_hum | Armament@elite | RA2GattlingMG2 |

## X2 — EMP weapons not following _EMP convention
| File | Line | Weapon |
|---|---|---|
| ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 763 | EMPGrenade |
| ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 855 | EMPGrenadeExplode |
| weapons/darkreign.yaml | 1322 | DR_EMP_Device |
| weapons/generals.yaml | 1589 | USA_EMP_PatriotMissAG |
| weapons/generals.yaml | 1614 | USA_EMP_PatriotMissAA |
| weapons/outpost2.yaml | 304 | eden_EMP_GP |
| weapons/outpost2.yaml | 740 | plymouth_EMP_Tiger |
| weapons/shockwave.yaml | 1194 | SUSA_EMP_MissileDefenderAG |
| weapons/shockwave.yaml | 1203 | SUSA_EMP_MissileDefenderStructure |
| weapons/shockwave.yaml | 1222 | SUSA_EMP_MissileDefenderGarrisoned |

## X4 — Weapons with deprecated E suffix (informational)
| File | Line | Weapon |
|---|---|---|
| ContentPacks/RedAlert2/Soviets/yaml/weapons.yaml | 794 | RA2FlakTrackGunE |
| ContentPacks/RedAlert2/Soviets/yaml/weapons.yaml | 830 | RA2FlakTrackAAGunE |
| ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 14 | RA2AsianShotgunE |
| ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 39 | AsianGrenadeE |
| ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 397 | NaxiMP40E |
| ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 833 | Lunar_GreenTigerCannonE |
| ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 857 | Lunar_GreenJadgDestroyerE |
| ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 881 | Lunar_GreenGrilleArtyE |
| ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 171 | RA2NarcoAKME |
| ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 189 | RA2Narco60mmE |
| ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 626 | LatinRusherRocketE |
| ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 733 | RA2GrenadePackE |
| ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 754 | LatinSmokerRocketE |
| ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 37 | td_gdi_commando_sniperE |
| ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 764 | CabalHunterKillerLasersE |
| weapons/shockwave.yaml | 76 | SUSABurtonSniperHE |
| weapons/shockwave.yaml | 230 | SUSAMLRSHE |
| weapons/tiberiansun.yaml | 1265 | TSSniperE |
| weapons/tiberiansun.yaml | 1292 | MutAPRifleE |

