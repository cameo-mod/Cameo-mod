# audit_duplicate_keys — duplicate keys in one node (ambiguous merges)

Files scanned: **657** — D1 ambiguous inheritance labels: **8**, D2 merged duplicates: **698**


## D1 — duplicate inheritance labels with different parent values

| file | lines | node | key | values |
|---|---|---|---|---|
| mods/cameo/ContentPacks/D2k/Ixian/yaml/weapons.yaml | 1865, 1867 | ixian_farasha | Inherits | ^TeslaWeapon vs ^Effect_Laser_Heavy |
| mods/cameo/ContentPacks/D2k/Ordos/yaml/weapons.yaml | 1757, 1758 | Laboratory_Bioball | Inherits | d2k_grenade vs ^Effect_Flame_Heavy |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 2352, 2353 | td_nod_stealthsoldier_bhreddarts | Inherits | ^TeslaChargedWeapon vs ^Effect_CannonAP_Medium |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 1958, 1959 | CabalHunterKillerLasers | Inherits | ^LaserWeapon vs ^Effect_Watersplash_Huge_RA2 |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 2067, 2069 | CabalHunterKillerLasers_elite | Inherits | ^LaserWeapon vs ^Effect_Watersplash_Huge_RA2 |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 470, 471 | TSHighVelocityChem | Inherits | ^TSCannonEffect vs ^Effect_AlliedTigerCannon |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 567, 568 | TSHighVelocity2Chem | Inherits | ^TSCannonEffect vs ^Effect_AlliedTigerCannon |
| mods/cameo/weapons/outpost2.yaml | 223, 224 | edenRailgun | Inherits | ^RailgunWeapon vs ^Effect_AlliedTigerCannon |


## D2 — duplicate keys by key name (top 40)

| key | occurrences |
|---|---|
| Projectile | 66 |
| Warhead@TankDestroyerCannonPercentage | 33 |
| RenderSprites | 33 |
| Warhead@ShrapnelWeaponPercentage | 32 |
| Warhead@MediumChemicalWeaponPercentage | 31 |
| Warhead@GrenadePercentage | 30 |
| Warhead@MediumMissilePercentage | 27 |
| Warhead@FlakWeaponPercentage | 24 |
| Warhead@ChaingunPercentage | 21 |
| Warhead@LightChemicalWeaponPercentage | 20 |
| Warhead@Effect | 19 |
| Warhead@SmallArmsPercentage | 16 |
| Voiced | 15 |
| Warhead@HeavyCannonPercentage | 14 |
| RevealsShroud | 14 |
| Warhead@HeavyMissilePercentage | 13 |
| Warhead@HeavyChemicalWeaponPercentage | 12 |
| Prerequisites | 11 |
| Warhead@LightMissilePercentage | 10 |
| Warhead@MediumCannonPercentage | 10 |
| Warhead@MediumFlameWeaponPercentage | 9 |
| Warhead@ShieldHit | 9 |
| Defaults | 9 |
| Warhead@HeavyFlameWeaponPercentage | 8 |
| HitShape | 8 |
| Warhead@HeavyAAWeaponPercentage | 7 |
| Selectable | 7 |
| muzzle | 6 |
| AttackAircraft | 6 |
| Warhead@1Dam | 6 |
| Warhead@HeavyBombPercentage | 5 |
| AutoTarget | 5 |
| ProvidesPrerequisite@buildingname | 5 |
| cheer | 4 |
| SpawnActorOnDeath | 4 |
| WithAmmoPipsDecoration | 4 |
| Warhead@ShotgunGrenadeAlly | 3 |
| Warhead@ShotgunShrapnelAlly | 3 |
| GrantConditionOnPrerequisite@2 | 3 |
| ProvidesPrerequisite | 3 |


## D2 — full list

| file | lines | node | key |
|---|---|---|---|
| mods/cameo/chrome/ingame_observer.yaml | 316, 320 | Container@OBSERVER_WIDGETS > Children > Image@REPLAY_PLAYER | Visible |
| mods/cameo/chrome/settings_display.yaml | 26, 80, 110, 141, 165, 191, 206, 228, 283, 344, 386, 399, 431, 444, 457, 470, 483, 496, 509 | Container@DISPLAY_PANEL > Children > ScrollPanel@SETTINGS_SCROLLPANEL > Children | Container@ROW |
| mods/cameo/chrome/settings_display.yaml | 66, 269 | Container@DISPLAY_PANEL > Children > ScrollPanel@SETTINGS_SCROLLPANEL > Children | Container@SPACER |
| mods/cameo/ContentPacks/D2k/Ixian/yaml/weapons.yaml | 40, 91 | DuelistTankCannon | Projectile |
| mods/cameo/ContentPacks/D2k/Ixian/yaml/weapons.yaml | 66, 94 | DuelistTankCannon | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/D2k/Ixian/yaml/weapons.yaml | 74, 118 | DuelistTankCannon | Warhead@TankDestroyerCannonPercentage |
| mods/cameo/ContentPacks/D2k/Ixian/yaml/weapons.yaml | 168, 218 | D2K_155mm2 | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/D2k/Ixian/yaml/weapons.yaml | 530, 567 | IxRailgunDroneBullet | Projectile |
| mods/cameo/ContentPacks/D2k/Ixian/yaml/weapons.yaml | 565, 572 | IxRailgunDroneBullet | Warhead@FlakWeaponPercentage |
| mods/cameo/ContentPacks/D2k/Ixian/yaml/weapons.yaml | 566, 596 | IxRailgunDroneBullet | Warhead@TankDestroyerCannonPercentage |
| mods/cameo/ContentPacks/D2k/Ixian/yaml/weapons.yaml | 785, 862 | RashidanGun | Warhead@LightMissilePercentage |
| mods/cameo/ContentPacks/D2k/Ixian/yaml/weapons.yaml | 788, 884 | RashidanGun | Warhead@MediumMissilePercentage |
| mods/cameo/ContentPacks/D2k/Ixian/yaml/weapons.yaml | 791, 910 | RashidanGun | Warhead@SmallArmsPercentage |
| mods/cameo/ContentPacks/D2k/Ixian/yaml/weapons.yaml | 794, 827 | RashidanGun | Warhead@ChaingunPercentage |
| mods/cameo/ContentPacks/D2k/Ixian/yaml/weapons.yaml | 1110, 1135 | HMG_Duelist | Projectile |
| mods/cameo/ContentPacks/D2k/Ixian/yaml/weapons.yaml | 1113, 1239 | HMG_Duelist | Warhead@LightMissilePercentage |
| mods/cameo/ContentPacks/D2k/Ixian/yaml/weapons.yaml | 1116, 1216 | HMG_Duelist | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/D2k/Ixian/yaml/weapons.yaml | 1119, 1287 | HMG_Duelist | Warhead@SmallArmsPercentage |
| mods/cameo/ContentPacks/D2k/Ixian/yaml/weapons.yaml | 1122, 1261 | HMG_Duelist | Warhead@MediumMissilePercentage |
| mods/cameo/ContentPacks/D2k/Ixian/yaml/weapons.yaml | 1125, 1194 | HMG_Duelist | Warhead@FlakWeaponPercentage |
| mods/cameo/ContentPacks/D2k/Ixian/yaml/weapons.yaml | 1128, 1160 | HMG_Duelist | Warhead@ChaingunPercentage |
| mods/cameo/ContentPacks/D2k/Ixian/yaml/weapons.yaml | 1336, 1434 | HMG_Duelist_upgrade | Warhead@HeavyMissilePercentage |
| mods/cameo/ContentPacks/D2k/Ixian/yaml/weapons.yaml | 1337, 1458 | HMG_Duelist_upgrade | Warhead@LightMissilePercentage |
| mods/cameo/ContentPacks/D2k/Ixian/yaml/weapons.yaml | 1338, 1409 | HMG_Duelist_upgrade | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/D2k/Ixian/yaml/weapons.yaml | 1339, 1510 | HMG_Duelist_upgrade | Warhead@SmallArmsPercentage |
| mods/cameo/ContentPacks/D2k/Ixian/yaml/weapons.yaml | 1340, 1482 | HMG_Duelist_upgrade | Warhead@MediumMissilePercentage |
| mods/cameo/ContentPacks/D2k/Ixian/yaml/weapons.yaml | 1341, 1385 | HMG_Duelist_upgrade | Warhead@FlakWeaponPercentage |
| mods/cameo/ContentPacks/D2k/Ixian/yaml/weapons.yaml | 1342, 1361 | HMG_Duelist_upgrade | Warhead@ChaingunPercentage |
| mods/cameo/ContentPacks/D2k/Ixian/yaml/weapons.yaml | 1709, 1733 | D2K_RocketsCymek | Projectile |
| mods/cameo/ContentPacks/D2k/Ixian/yaml/weapons.yaml | 1730, 1760 | D2K_RocketsCymek | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/D2k/Ixian/yaml/weapons.yaml | 1731, 1736 | D2K_RocketsCymek | Warhead@FlakWeaponPercentage |
| mods/cameo/ContentPacks/D2k/Ixian/yaml/weapons.yaml | 1732, 1785 | D2K_RocketsCymek | Warhead@HeavyMissilePercentage |
| mods/cameo/ContentPacks/D2k/Ixian/yaml/weapons.yaml | 1879, 1950 | ixian_farasha | Projectile |
| mods/cameo/ContentPacks/D2k/Ixian/yaml/weapons.yaml | 1911, 1964 | ixian_farasha | Warhead@HeavyFlameWeaponPercentage |
| mods/cameo/ContentPacks/D2k/Ixian/yaml/weapons.yaml | 1924, 1954 | ixian_farasha | Warhead@Effect |
| mods/cameo/ContentPacks/D2k/Ordos/yaml/weapons.yaml | 66, 78 | 120mm_td | Projectile |
| mods/cameo/ContentPacks/D2k/Ordos/yaml/weapons.yaml | 75, 120 | 120mm_td | Warhead@MediumChemicalWeaponPercentage |
| mods/cameo/ContentPacks/D2k/Ordos/yaml/weapons.yaml | 76, 93 | 120mm_td | Warhead@LightChemicalWeaponPercentage |
| mods/cameo/ContentPacks/D2k/Ordos/yaml/weapons.yaml | 77, 152 | 120mm_td | Warhead@TankDestroyerCannonPercentage |
| mods/cameo/ContentPacks/D2k/Ordos/yaml/weapons.yaml | 728, 795 | eye_bomberguy | Projectile |
| mods/cameo/ContentPacks/D2k/Ordos/yaml/weapons.yaml | 743, 797 | eye_bomberguy | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/D2k/Ordos/yaml/weapons.yaml | 750, 821 | eye_bomberguy | Warhead@MediumChemicalWeaponPercentage |
| mods/cameo/ContentPacks/D2k/Ordos/yaml/weapons.yaml | 1436, 1458 | DeviatorMissile | Warhead@Effect |
| mods/cameo/ContentPacks/D2k/Ordos/yaml/weapons.yaml | 1456, 1462 | DeviatorMissile | Warhead@MediumChemicalWeaponPercentage |
| mods/cameo/ContentPacks/D2k/Ordos/yaml/weapons.yaml | 1457, 1489 | DeviatorMissile | Warhead@MediumMissilePercentage |
| mods/cameo/ContentPacks/D2k/Ordos/yaml/weapons.yaml | 1533, 1608 | DeviatorMissile_Artillery | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/ContentPacks/D2k/Ordos/yaml/weapons.yaml | 1545, 1580 | DeviatorMissile_Artillery | Warhead@HeavyFlameWeaponPercentage |
| mods/cameo/ContentPacks/D2k/Ordos/yaml/weapons.yaml | 1784, 1841 | Laboratory_Bioball | Warhead@MediumChemicalWeaponPercentage |
| mods/cameo/ContentPacks/D2k/Ordos/yaml/weapons.yaml | 1798, 1865 | Laboratory_Bioball | Warhead@MediumFlameWeaponPercentage |
| mods/cameo/ContentPacks/D2k/Ordos/yaml/weapons.yaml | 1805, 1835 | Laboratory_Bioball | Warhead@Effect |
| mods/cameo/ContentPacks/D2k/Ordos/yaml/weapons.yaml | 1830, 1887 | Laboratory_Bioball | Warhead@ShieldHit |
| mods/cameo/ContentPacks/D2k/Ordos/yaml/weapons.yaml | 1900, 1972 | facedancer_grenade | Projectile |
| mods/cameo/ContentPacks/D2k/Ordos/yaml/weapons.yaml | 1919, 2019 | facedancer_grenade | Warhead@MediumChemicalWeaponPercentage |
| mods/cameo/ContentPacks/D2k/Ordos/yaml/weapons.yaml | 1927, 1974 | facedancer_grenade | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/D2k/Ordos/yaml/weapons.yaml | 1931, 2066 | facedancer_grenade | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/ContentPacks/D2k/Ordos/yaml/weapons.yaml | 1935, 1997 | facedancer_grenade | Warhead@LightMissilePercentage |
| mods/cameo/ContentPacks/D2k/Ordos/yaml/weapons.yaml | 1939, 2044 | facedancer_grenade | Warhead@MediumMissilePercentage |
| mods/cameo/ContentPacks/D2k/Ordos/yaml/weapons.yaml | 2266, 2271 | autogun_tank | Projectile |
| mods/cameo/ContentPacks/D2k/Ordos/yaml/weapons.yaml | 2267, 2344 | autogun_tank | Warhead@HeavyAAWeaponPercentage |
| mods/cameo/ContentPacks/D2k/Ordos/yaml/weapons.yaml | 2268, 2320 | autogun_tank | Warhead@FlakWeaponPercentage |
| mods/cameo/ContentPacks/D2k/Ordos/yaml/weapons.yaml | 2269, 2368 | autogun_tank | Warhead@MediumMissilePercentage |
| mods/cameo/ContentPacks/D2k/Ordos/yaml/weapons.yaml | 2270, 2289 | autogun_tank | Warhead@ChaingunPercentage |
| mods/cameo/ContentPacks/D2k/Ordos/yaml/weapons.yaml | 2444, 2536 | ordos_airmine | Projectile |
| mods/cameo/ContentPacks/D2k/Ordos/yaml/weapons.yaml | 2466, 2646 | ordos_airmine | Warhead@TankDestroyerCannonPercentage |
| mods/cameo/ContentPacks/D2k/Ordos/yaml/weapons.yaml | 2478, 2597 | ordos_airmine | Warhead@LightChemicalWeaponPercentage |
| mods/cameo/ContentPacks/D2k/Ordos/yaml/weapons.yaml | 2488, 2554 | ordos_airmine | Warhead@FlakWeaponPercentage |
| mods/cameo/ContentPacks/D2k/Ordos/yaml/weapons.yaml | 2498, 2621 | ordos_airmine | Warhead@MediumMissilePercentage |
| mods/cameo/ContentPacks/D2k/Ordos/yaml/weapons.yaml | 2508, 2575 | ordos_airmine | Warhead@HeavyBombPercentage |
| mods/cameo/ContentPacks/D2k/Ordos/yaml/weapons.yaml | 2677, 2757 | ordos_lasertank | Projectile |
| mods/cameo/ContentPacks/D2k/Ordos/yaml/weapons.yaml | 2682, 2785 | ordos_lasertank | Warhead@FlakWeaponPercentage |
| mods/cameo/ContentPacks/D2k/Ordos/yaml/weapons.yaml | 2686, 2806 | ordos_lasertank | Warhead@MediumMissilePercentage |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 1655, 1661 | JapaneseHovercraftFlakAAkWaveforce | Warhead@Railgun_Heavy |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 2610, 2640 | ArmoredCarMGWaveforce | Warhead@Railgun_Heavy_ExtraDamage |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 2668, 2696 | ArmoredCarMGAAWaveforce | Warhead@Railgun_Heavy_ExtraDamage |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/buildings.yaml | 597, 600 | ra1_soviets_largesovietairfield | RenderSprites |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/sequences.yaml | 11, 37 | ra2_allies_constructionyard | dead |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/sequences.yaml | 3775, 3781 | yrslav | cheer |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/sequences.yaml | 3914, 3918 | ra2howi | muzzle |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/sequences.yaml | 3927, 3931 | ra2arty | muzzle |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 2526, 2533 | MigMissiles_rad_elite | Warhead@Chemical_Medium |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/sequences.yaml | 4, 45 | yuri_constructionyard | build |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/sequences.yaml | 11, 31 | asianalliance_constructionyard | dead |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/sequences.yaml | 1275, 1304 | asianalliance_flametrooper | shoot |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/sequences.yaml | 1288, 1301 | asianalliance_flametrooper | cheer |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/sequences.yaml | 926, 932 | naxis_slave | cheer |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/sequences.yaml | 525, 583 | latinsyndicate_topolsilo | critical-idle |
| mods/cameo/ContentPacks/StarCraft/Protoss/yaml/sequences.yaml | 503, 505 | protoss_arbiter | idle |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 120, 134 | HarakanF | Warhead@HeavyFlameWeaponPercentage |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 124, 159 | HarakanF | Warhead@MediumFlameWeaponPercentage |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 286, 314 | MarauderMissiles | Projectile |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 297, 362 | MarauderMissiles | Warhead@TankDestroyerCannonPercentage |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 300, 338 | MarauderMissiles | Warhead@MediumMissilePercentage |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 395, 447 | SCTyr | Projectile |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 413, 474 | SCTyr | Warhead@LightFlameWeaponPercentage |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 421, 449 | SCTyr | Warhead@ChaingunPercentage |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 430, 501 | SCTyr | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 535, 565 | SCTyrAA | Projectile |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 556, 590 | SCTyrAA | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 559, 568 | SCTyrAA | Warhead@FlakWeaponPercentage |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 562, 613 | SCTyrAA | Warhead@HeavyMissilePercentage |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 1141, 1179 | VultureGrenade | Projectile |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 1154, 1240 | VultureGrenade | Warhead@SmallArmsPercentage |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 1157, 1188 | VultureGrenade | Warhead@FlakWeaponPercentage |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 1160, 1214 | VultureGrenade | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 1165, 1263 | VultureGrenade | Warhead@TankDestroyerCannonPercentage |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 1168, 1183 | VultureGrenade | Warhead@Effect |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 1177, 1238 | VultureGrenade | Warhead@ShieldHit |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 1312, 1477 | SiegeTankSiegeCannon | Projectile |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 1323, 1616 | SiegeTankSiegeCannon | Warhead@LightMissilePercentage |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 1333, 1591 | SiegeTankSiegeCannon | Warhead@LightChemicalWeaponPercentage |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 1343, 1660 | SiegeTankSiegeCannon | Warhead@MediumChemicalWeaponPercentage |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 1353, 1542 | SiegeTankSiegeCannon | Warhead@HeavyChemicalWeaponPercentage |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 1379, 1685 | SiegeTankSiegeCannon | Warhead@MediumFlameWeaponPercentage |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 1393, 1567 | SiegeTankSiegeCannon | Warhead@HeavyFlameWeaponPercentage |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 1405, 1498 | SiegeTankSiegeCannon | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 1415, 1708 | SiegeTankSiegeCannon | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 1429, 1521 | SiegeTankSiegeCannon | Warhead@HeavyCannonPercentage |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 1436, 1638 | SiegeTankSiegeCannon | Warhead@MediumCannonPercentage |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 1448, 1494 | SiegeTankSiegeCannon | Warhead@Effect |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 1794, 1817 | GoliathMk2MG | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 1800, 1840 | GoliathMk2MG | Warhead@TankDestroyerCannonPercentage |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 1807, 1810 | GoliathMk2MG | Projectile |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 2087, 2124 | ValkyrieRockets | Projectile |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 2099, 2139 | ValkyrieRockets | Warhead@FlakWeaponPercentage |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 2102, 2183 | ValkyrieRockets | Warhead@MediumMissilePercentage |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 2105, 2161 | ValkyrieRockets | Warhead@HeavyAAWeaponPercentage |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 2108, 2137 | ValkyrieRockets | Warhead@EffectWater |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 2220, 2298 | WyvernRockets | Projectile |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 2252, 2353 | WyvernRockets | Warhead@MediumChemicalWeaponPercentage |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 2267, 2309 | WyvernRockets | Warhead@FlakWeaponPercentage |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 2273, 2378 | WyvernRockets | Warhead@MediumMissilePercentage |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 2279, 2331 | WyvernRockets | Warhead@HeavyAAWeaponPercentage |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 2413, 2491 | BCLaser | Projectile |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 2433, 2593 | BCLaser | Warhead@HeavyMissilePercentage |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 2442, 2522 | BCLaser | Warhead@HeavyAAWeaponPercentage |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 2457, 2544 | BCLaser | Warhead@HeavyChemicalWeaponPercentage |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 2468, 2569 | BCLaser | Warhead@HeavyFlameWeaponPercentage |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 2703, 2791 | PhobosLaser | Projectile |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 2717, 2893 | PhobosLaser | Warhead@HeavyMissilePercentage |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 2726, 2822 | PhobosLaser | Warhead@HeavyAAWeaponPercentage |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 2741, 2844 | PhobosLaser | Warhead@HeavyChemicalWeaponPercentage |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 2752, 2869 | PhobosLaser | Warhead@HeavyFlameWeaponPercentage |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 2971, 3043 | MedicFlare | Projectile |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 2992, 3080 | MedicFlare | Warhead@LightFlameWeaponPercentage |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/sequences.yaml | 388, 394 | td_gdi_advancedguardtower | muzzle |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 161, 373 | td_gdi_boxer_boxercannonag | Warhead@SmallArmsPercentage |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 168, 325 | td_gdi_boxer_boxercannonag | Warhead@LightMissilePercentage |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 178, 302 | td_gdi_boxer_boxercannonag | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 185, 347 | td_gdi_boxer_boxercannonag | Warhead@MediumCannonPercentage |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 192, 243 | td_gdi_boxer_boxercannonag | Warhead@ChaingunPercentage |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 199, 276 | td_gdi_boxer_boxercannonag | Warhead@FlakWeaponPercentage |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 460, 596 | td_gdi_skyshield_skyshieldcannon | Warhead@SmallArmsPercentage |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 466, 570 | td_gdi_skyshield_skyshieldcannon | Warhead@LightMissilePercentage |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 472, 516 | td_gdi_skyshield_skyshieldcannon | Warhead@ChaingunPercentage |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 478, 548 | td_gdi_skyshield_skyshieldcannon | Warhead@FlakWeaponPercentage |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 649, 685 | td_gdi_archerartillery_archerartilleryshell | Warhead@HeavyCannonPercentage |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 857, 969 | td_gdi_orca_orcamissiles | Warhead@HeavyCannonPercentage |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 863, 1059 | td_gdi_orca_orcamissiles | Warhead@TankDestroyerCannonPercentage |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 872, 1036 | td_gdi_orca_orcamissiles | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 881, 946 | td_gdi_orca_orcamissiles | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 887, 990 | td_gdi_orca_orcamissiles | Warhead@HeavyMissilePercentage |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 893, 1012 | td_gdi_orca_orcamissiles | Warhead@MediumMissilePercentage |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 904, 916 | td_gdi_orca_orcamissiles | Projectile |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 1228, 1321 | td_gdi_advancedguardtower_towermissile | Projectile |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 1237, 1492 | td_gdi_advancedguardtower_towermissile | Warhead@TankDestroyerCannonPercentage |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 1249, 1378 | td_gdi_advancedguardtower_towermissile | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 1261, 1469 | td_gdi_advancedguardtower_towermissile | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 1269, 1356 | td_gdi_advancedguardtower_towermissile | Warhead@FlakWeaponPercentage |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 1277, 1401 | td_gdi_advancedguardtower_towermissile | Warhead@HeavyAAWeaponPercentage |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 1285, 1423 | td_gdi_advancedguardtower_towermissile | Warhead@HeavyMissilePercentage |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 1293, 1445 | td_gdi_advancedguardtower_towermissile | Warhead@MediumMissilePercentage |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 1297, 1345 | td_gdi_advancedguardtower_towermissile | Warhead@Effect |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 1300, 1348 | td_gdi_advancedguardtower_towermissile | Warhead@EffectAir |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 1538, 1607 | td_gdi_sonicmissilesoldier_missilesoldierweapon | Projectile |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 1556, 1692 | td_gdi_sonicmissilesoldier_missilesoldierweapon | Warhead@TankDestroyerCannonPercentage |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 1565, 1642 | td_gdi_sonicmissilesoldier_missilesoldierweapon | Warhead@LightChemicalWeaponPercentage |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 1574, 1669 | td_gdi_sonicmissilesoldier_missilesoldierweapon | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 1586, 1620 | td_gdi_sonicmissilesoldier_missilesoldierweapon | Warhead@HeavyMissilePercentage |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 1589, 1616 | td_gdi_sonicmissilesoldier_missilesoldierweapon | Warhead@Effect |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 1748, 1835 | td_gdi_empgrenadier_grenade_emp | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 1752, 1793 | td_gdi_empgrenadier_grenade_emp | Warhead@HeavyCannonPercentage |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 1760, 1813 | td_gdi_empgrenadier_grenade_emp | Warhead@MediumMissilePercentage |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 1900, 1973 | td_gdi_heavysniper_rifle | Projectile |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 1909, 2057 | td_gdi_heavysniper_rifle | Warhead@SmallArmsPercentage |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 1914, 2032 | td_gdi_heavysniper_rifle | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 1919, 2011 | td_gdi_heavysniper_rifle | Warhead@FlakWeaponPercentage |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 1924, 1979 | td_gdi_heavysniper_rifle | Warhead@ChaingunPercentage |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 1968, 2002 | td_gdi_heavysniper_rifle | Warhead@Effect |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 2223, 2295 | TDShotgun | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 2225, 2346 | TDShotgun | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 2227, 2396 | TDShotgun | Warhead@TankDestroyerCannonPercentage |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 2229, 2319 | TDShotgun | Warhead@MediumCannonPercentage |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 2231, 2370 | TDShotgun | Warhead@SmallArmsPercentage |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 2233, 2257 | TDShotgun | Warhead@ChaingunPercentage |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 2649, 2677 | td_gdi_defenserig_gdirigphalanx | Projectile |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 2653, 2820 | td_gdi_defenserig_gdirigphalanx | Warhead@TankDestroyerCannonPercentage |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 2656, 2795 | td_gdi_defenserig_gdirigphalanx | Warhead@SmallArmsPercentage |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 2659, 2746 | td_gdi_defenserig_gdirigphalanx | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 2662, 2769 | td_gdi_defenserig_gdirigphalanx | Warhead@MediumCannonPercentage |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 2665, 2720 | td_gdi_defenserig_gdirigphalanx | Warhead@FlakWeaponPercentage |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 2668, 2687 | td_gdi_defenserig_gdirigphalanx | Warhead@ChaingunPercentage |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 2671, 2711 | td_gdi_defenserig_gdirigphalanx | Warhead@Effect |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 2937, 2994 | td_gdi_havoc_sniper | Projectile |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 2979, 2997 | td_gdi_havoc_sniper | Warhead@ChaingunPercentage |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 3068, 3120 | td_gdi_havoc_grenade | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 3071, 3096 | td_gdi_havoc_grenade | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 3155, 3201 | td_gdi_havoc_rifle | Projectile |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 3193, 3204 | td_gdi_havoc_rifle | Warhead@ChaingunPercentage |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 9, 25 | M16Laser | Projectile |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 12, 89 | M16Laser | Warhead@SmallArmsPercentage |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 14, 35 | M16Laser | Warhead@ChaingunPercentage |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 289, 353 | td_nod_artillery_artilleryshellupgrade | Projectile |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 304, 355 | td_nod_artillery_artilleryshellupgrade | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 310, 405 | td_nod_artillery_artilleryshellupgrade | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 317, 379 | td_nod_artillery_artilleryshellupgrade | Warhead@MediumChemicalWeaponPercentage |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 466, 501 | td_nod_reconbike_rocket | Projectile |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 472, 608 | td_nod_reconbike_rocket | Warhead@TankDestroyerCannonPercentage |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 479, 585 | td_nod_reconbike_rocket | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 484, 534 | td_nod_reconbike_rocket | Warhead@FlakWeaponPercentage |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 488, 560 | td_nod_reconbike_rocket | Warhead@LightMissilePercentage |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 797, 812 | td_nod_gunturret_turretgun | Projectile |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 800, 870 | td_nod_gunturret_turretgun | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 802, 823 | td_nod_gunturret_turretgun | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 804, 847 | td_nod_gunturret_turretgun | Warhead@MediumCannonPercentage |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 806, 894 | td_nod_gunturret_turretgun | Warhead@TankDestroyerCannonPercentage |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 924, 973 | td_nod_gunturret_turretgunblackmarket | Warhead@HeavyBombPercentage |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 928, 997 | td_nod_gunturret_turretgunblackmarket | Warhead@HeavyCannonPercentage |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 1226, 1254 | td_nod_chemicalrocketsoldier_chemrockets | Projectile |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 1229, 1279 | td_nod_chemicalrocketsoldier_chemrockets | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 1233, 1301 | td_nod_chemicalrocketsoldier_chemrockets | Warhead@LightChemicalWeaponPercentage |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 1237, 1325 | td_nod_chemicalrocketsoldier_chemrockets | Warhead@LightMissilePercentage |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 1241, 1274 | td_nod_chemicalrocketsoldier_chemrockets | Warhead@Effect |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 1375, 1410 | td_nod_chemicalattackbike_chemicalbikerockets | Projectile |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 1380, 1509 | td_nod_chemicalattackbike_chemicalbikerockets | Warhead@TankDestroyerCannonPercentage |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 1384, 1462 | td_nod_chemicalattackbike_chemicalbikerockets | Warhead@LightChemicalWeaponPercentage |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 1388, 1441 | td_nod_chemicalattackbike_chemicalbikerockets | Warhead@FlakWeaponPercentage |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 1392, 1486 | td_nod_chemicalattackbike_chemicalbikerockets | Warhead@MediumMissilePercentage |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 1396, 1435 | td_nod_chemicalattackbike_chemicalbikerockets | Warhead@Effect |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 1547, 1580 | td_nod_chemicalstealthtank_chemicalstealthtankmissiles | Projectile |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 1551, 1647 | td_nod_chemicalstealthtank_chemicalstealthtankmissiles | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 1555, 1600 | td_nod_chemicalstealthtank_chemicalstealthtankmissiles | Warhead@MediumChemicalWeaponPercentage |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 1559, 1624 | td_nod_chemicalstealthtank_chemicalstealthtankmissiles | Warhead@MediumMissilePercentage |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 1563, 1597 | td_nod_chemicalstealthtank_chemicalstealthtankmissiles | Warhead@Effect |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 1758, 1827 | td_nod_buggymkii_machinegunbuggy2 | Warhead@HeavyCannonPercentage |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 1762, 1852 | td_nod_buggymkii_machinegunbuggy2 | Warhead@SmallArmsPercentage |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 1766, 1789 | td_nod_buggymkii_machinegunbuggy2 | Warhead@ChaingunPercentage |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 2059, 2082 | td_nod_lighttankmkii_lighttank2cannon | Projectile |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 2066, 2090 | td_nod_lighttankmkii_lighttank2cannon | Warhead@LightChemicalWeaponPercentage |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 2070, 2116 | td_nod_lighttankmkii_lighttank2cannon | Warhead@MediumCannonPercentage |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 2206, 2259 | td_nod_lasertrooper_blackhandlaser | Projectile |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 2216, 2306 | td_nod_lasertrooper_blackhandlaser | Warhead@TankDestroyerCannonPercentage |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 2220, 2279 | td_nod_lasertrooper_blackhandlaser | Warhead@FlakWeaponPercentage |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 2360, 2418 | td_nod_stealthsoldier_bhreddarts | Projectile |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 2374, 2509 | td_nod_stealthsoldier_bhreddarts | Warhead@TankDestroyerCannonPercentage |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 2379, 2437 | td_nod_stealthsoldier_bhreddarts | Warhead@ChaingunPercentage |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 2384, 2487 | td_nod_stealthsoldier_bhreddarts | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 2389, 2464 | td_nod_stealthsoldier_bhreddarts | Warhead@MediumMissilePercentage |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 2409, 2458 | td_nod_stealthsoldier_bhreddarts | Warhead@Effect |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 2590, 2678 | td_nod_specterartillery_specterartilleryshellupgrade | Warhead@MediumChemicalWeaponPercentage |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 2613, 2655 | td_nod_specterartillery_specterartilleryshellupgrade | Warhead@MediumCannonPercentage |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 2617, 2633 | td_nod_specterartillery_specterartilleryshellupgrade | Warhead@HeavyCannonPercentage |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 2951, 2978 | td_nod_minigunner_minigun_laser | Projectile |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 2954, 3041 | td_nod_minigunner_minigun_laser | Warhead@SmallArmsPercentage |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 2958, 2988 | td_nod_minigunner_minigun_laser | Warhead@ChaingunPercentage |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 2973, 3037 | td_nod_minigunner_minigun_laser | Warhead@ShieldHit |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 2975, 3013 | td_nod_minigunner_minigun_laser | Warhead@LegacyLaserExtraDamage |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/aircraft.yaml | 230, 268 | cabal_hunterdrone | AttackAircraft |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 194, 264 | TSCABALObeliskLaserFire | Projectile |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 211, 343 | TSCABALObeliskLaserFire | Warhead@TankDestroyerCannonPercentage |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 226, 312 | TSCABALObeliskLaserFire | Warhead@MediumFlameWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 236, 286 | TSCABALObeliskLaserFire | Warhead@MediumChemicalWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 240, 272 | TSCABALObeliskLaserFire | Warhead@Effect |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 261, 336 | TSCABALObeliskLaserFire | Warhead@ShieldHit |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 378, 491 | TSHellfireTwin | Warhead@HeavyCannonPercentage |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 384, 581 | TSHellfireTwin | Warhead@TankDestroyerCannonPercentage |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 393, 558 | TSHellfireTwin | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 402, 468 | TSHellfireTwin | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 408, 512 | TSHellfireTwin | Warhead@HeavyMissilePercentage |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 414, 534 | TSHellfireTwin | Warhead@MediumMissilePercentage |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 417, 461 | TSHellfireTwin | Warhead@Effect |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 429, 441 | TSHellfireTwin | Projectile |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 614, 681 | TSCABALEnlightedLaser | Projectile |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 624, 760 | TSCABALEnlightedLaser | Warhead@TankDestroyerCannonPercentage |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 639, 729 | TSCABALEnlightedLaser | Warhead@MediumFlameWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 649, 703 | TSCABALEnlightedLaser | Warhead@MediumChemicalWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 653, 689 | TSCABALEnlightedLaser | Warhead@Effect |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 678, 753 | TSCABALEnlightedLaser | Warhead@ShieldHit |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 1558, 1602 | CabalArtilleryWalkerShell | Projectile |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 1573, 1610 | CabalArtilleryWalkerShell | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 1579, 1681 | CabalArtilleryWalkerShell | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 1583, 1656 | CabalArtilleryWalkerShell | Warhead@MediumCannonPercentage |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 1587, 1634 | CabalArtilleryWalkerShell | Warhead@HeavyCannonPercentage |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 1589, 1606 | CabalArtilleryWalkerShell | Warhead@Effect |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 1715, 1786 | CabalArtilleryWalkerShellUpgraded | Projectile |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 1730, 1808 | CabalArtilleryWalkerShellUpgraded | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 1736, 1930 | CabalArtilleryWalkerShellUpgraded | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 1740, 1879 | CabalArtilleryWalkerShellUpgraded | Warhead@MediumCannonPercentage |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 1744, 1832 | CabalArtilleryWalkerShellUpgraded | Warhead@HeavyCannonPercentage |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 1751, 1902 | CabalArtilleryWalkerShellUpgraded | Warhead@MediumChemicalWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 1759, 1854 | CabalArtilleryWalkerShellUpgraded | Warhead@MagicWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 1765, 1801 | CabalArtilleryWalkerShellUpgraded | Warhead@Effect |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 1963, 2017 | CabalHunterKillerLasers | Projectile |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 1975, 2041 | CabalHunterKillerLasers | Warhead@MediumMissilePercentage |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 2075, 2157 | CabalHunterKillerLasers_elite | Projectile |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 2145, 2176 | CabalHunterKillerLasers_elite | Warhead@HeavyMissilePercentage |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 2326, 2348 | CabalDissolverSpray | Warhead@LightChemicalWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 2328, 2374 | CabalDissolverSpray | Warhead@TankDestroyerCannonPercentage |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 2937, 3017 | CabalMothershipRockets | Warhead@MediumChemicalWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 2965, 2993 | CabalMothershipRockets | Warhead@ArrowWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 3125, 3223 | CabalAscendedRockets | Warhead@LightMissilePercentage |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 3128, 3245 | CabalAscendedRockets | Warhead@MediumMissilePercentage |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 3131, 3201 | CabalAscendedRockets | Warhead@HeavyMissilePercentage |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 3134, 3269 | CabalAscendedRockets | Warhead@TankDestroyerCannonPercentage |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 3137, 3157 | CabalAscendedRockets | Warhead@FlakWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 3140, 3179 | CabalAscendedRockets | Warhead@HeavyAAWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 3930, 3970 | CabalCommandoPlasmaNeutron | Warhead@MediumChemicalWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 4034, 4074 | CabalCommandoPlasmaMk2Neutron | Warhead@MediumChemicalWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 4129, 4188 | CabalBeholderLaser | Projectile |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 4139, 4198 | CabalBeholderLaser | Warhead@HeavyCannonPercentage |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 4184, 4219 | CabalBeholderLaser | Warhead@ShieldHit |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/sequences.yaml | 62, 66 | forgotten_chemsprayinfantry | prone-shoot |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/sequences.yaml | 334, 375 | forgotten_zombiemutant | standup |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/sequences.yaml | 369, 384 | forgotten_zombiemutant | die-crushed |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 82, 105 | TS70mmTurChem | Projectile |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 103, 117 | TS70mmTurChem | Warhead@LightChemicalWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 104, 146 | TS70mmTurChem | Warhead@TankDestroyerCannonPercentage |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 200, 261 | TSChem120mmx | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 202, 233 | TSChem120mmx | Warhead@MediumChemicalWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 390, 403 | TSScoopDualTurChem | Warhead@MediumChemicalWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 391, 430 | TSScoopDualTurChem | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 474, 496 | TSHighVelocityChem | Projectile |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 476, 529 | TSHighVelocityChem | Warhead@TankDestroyerCannonPercentage |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 478, 503 | TSHighVelocityChem | Warhead@LightChemicalWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 571, 593 | TSHighVelocity2Chem | Projectile |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 573, 627 | TSHighVelocity2Chem | Warhead@TankDestroyerCannonPercentage |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 699, 816 | TSBusMortarChem | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 701, 736 | TSBusMortarChem | Warhead@HeavyBombPercentage |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 703, 786 | TSBusMortarChem | Warhead@MediumChemicalWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 705, 760 | TSBusMortarChem | Warhead@HeavyChemicalWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 1136, 1162 | TSChemBoatcannon | Warhead@HeavyBombPercentage |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 1139, 1185 | TSChemBoatcannon | Warhead@MediumChemicalWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 1408, 1454 | TSMammothTuskChem | Projectile |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 1432, 1495 | TSMammothTuskChem | Warhead@HeavyMissilePercentage |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 1435, 1470 | TSMammothTuskChem | Warhead@HeavyChemicalWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 1582, 1614 | TSChemRuinerMissile | Projectile |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 1587, 1665 | TSChemRuinerMissile | Warhead@LightChemicalWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 1590, 1690 | TSChemRuinerMissile | Warhead@MediumChemicalWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 1593, 1642 | TSChemRuinerMissile | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 1596, 1715 | TSChemRuinerMissile | Warhead@MediumMissilePercentage |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 1963, 2036 | MutFlamerChem | Warhead@MediumFlameWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 1972, 2010 | MutFlamerChem | Warhead@MediumChemicalWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 2073, 2114 | MutHFlamer | Warhead@MediumFlameWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 2077, 2089 | MutHFlamer | Warhead@HeavyFlameWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 2154, 2298 | MutHFlamerChem | Warhead@MediumFlameWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 2165, 2247 | MutHFlamerChem | Warhead@HeavyFlameWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 2174, 2272 | MutHFlamerChem | Warhead@MediumChemicalWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 2181, 2221 | MutHFlamerChem | Warhead@HeavyChemicalWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 2364, 2419 | TSFiendShardUP | Warhead@LightChemicalWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 2367, 2444 | TSFiendShardUP | Warhead@MediumChemicalWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 2370, 2394 | TSFiendShardUP | Warhead@HeavyChemicalWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 2481, 2508 | TSFiendShardBlue | Projectile |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 2486, 2519 | TSFiendShardBlue | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 2489, 2596 | TSFiendShardBlue | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 2492, 2542 | TSFiendShardBlue | Warhead@LightChemicalWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 2495, 2567 | TSFiendShardBlue | Warhead@MediumChemicalWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 2630, 2673 | TSFiendShardBlueUP | Projectile |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 2635, 2689 | TSFiendShardBlueUP | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 2638, 2810 | TSFiendShardBlueUP | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 2641, 2760 | TSFiendShardBlueUP | Warhead@LightChemicalWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 2644, 2785 | TSFiendShardBlueUP | Warhead@MediumChemicalWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 2647, 2735 | TSFiendShardBlueUP | Warhead@HeavyChemicalWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 2650, 2712 | TSFiendShardBlueUP | Warhead@HeavyBombPercentage |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 2861, 2912 | TSChemsprayUP | Warhead@LightChemicalWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 2863, 2938 | TSChemsprayUP | Warhead@MediumChemicalWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 2865, 2886 | TSChemsprayUP | Warhead@HeavyChemicalWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 2979, 3030 | TSVisceroidSprayUP | Warhead@LightChemicalWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 2981, 3056 | TSVisceroidSprayUP | Warhead@MediumChemicalWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 2983, 3004 | TSVisceroidSprayUP | Warhead@HeavyChemicalWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 3193, 3209, 3292 | TSMutShotgun | Warhead@ShotgunGrenadeAlly |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 3196, 3210, 3322 | TSMutShotgun | Warhead@ShotgunShrapnelAlly |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 3201, 3288 | TSMutShotgun | Warhead@ShieldHit |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 3203, 3211 | TSMutShotgun | Projectile |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 3204, 3262 | TSMutShotgun | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 3205, 3352 | TSMutShotgun | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 3206, 3403 | TSMutShotgun | Warhead@TankDestroyerCannonPercentage |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 3207, 3378 | TSMutShotgun | Warhead@SmallArmsPercentage |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 3208, 3223 | TSMutShotgun | Warhead@ChaingunPercentage |
| mods/cameo/ContentPacks/TiberianSun/GDI/yaml/weapons.yaml | 31, 65 | TS30mmRail | Projectile |
| mods/cameo/ContentPacks/TiberianSun/GDI/yaml/weapons.yaml | 42, 119 | TS30mmRail | Warhead@MediumMissilePercentage |
| mods/cameo/ContentPacks/TiberianSun/GDI/yaml/weapons.yaml | 46, 96 | TS30mmRail | Warhead@FlakWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/GDI/yaml/weapons.yaml | 237, 242 | TSRPGTowerRail | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/GDI/yaml/weapons.yaml | 335, 385 | KodiakCannon | Projectile |
| mods/cameo/ContentPacks/TiberianSun/GDI/yaml/weapons.yaml | 343, 409 | KodiakCannon | Warhead@HeavyChemicalWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/GDI/yaml/weapons.yaml | 357, 434 | KodiakCannon | Warhead@HeavyMissilePercentage |
| mods/cameo/ContentPacks/TiberianSun/GDI/yaml/weapons.yaml | 741, 781 | TSHoverMissile | Projectile |
| mods/cameo/ContentPacks/TiberianSun/GDI/yaml/weapons.yaml | 749, 810 | TSHoverMissile | Warhead@FlakWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/GDI/yaml/weapons.yaml | 755, 832 | TSHoverMissile | Warhead@MediumMissilePercentage |
| mods/cameo/ContentPacks/TiberianSun/GDI/yaml/weapons.yaml | 764, 856 | TSHoverMissile | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/GDI/yaml/weapons.yaml | 889, 929 | TSDestroyerMissiles | Projectile |
| mods/cameo/ContentPacks/TiberianSun/GDI/yaml/weapons.yaml | 897, 958 | TSDestroyerMissiles | Warhead@FlakWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/GDI/yaml/weapons.yaml | 903, 980 | TSDestroyerMissiles | Warhead@MediumMissilePercentage |
| mods/cameo/ContentPacks/TiberianSun/GDI/yaml/weapons.yaml | 912, 1004 | TSDestroyerMissiles | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/GDI/yaml/weapons.yaml | 1288, 1304, 1387 | TSShotgun | Warhead@ShotgunGrenadeAlly |
| mods/cameo/ContentPacks/TiberianSun/GDI/yaml/weapons.yaml | 1291, 1305, 1417 | TSShotgun | Warhead@ShotgunShrapnelAlly |
| mods/cameo/ContentPacks/TiberianSun/GDI/yaml/weapons.yaml | 1296, 1383 | TSShotgun | Warhead@ShieldHit |
| mods/cameo/ContentPacks/TiberianSun/GDI/yaml/weapons.yaml | 1298, 1306 | TSShotgun | Projectile |
| mods/cameo/ContentPacks/TiberianSun/GDI/yaml/weapons.yaml | 1299, 1357 | TSShotgun | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/TiberianSun/GDI/yaml/weapons.yaml | 1300, 1447 | TSShotgun | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/GDI/yaml/weapons.yaml | 1301, 1498 | TSShotgun | Warhead@TankDestroyerCannonPercentage |
| mods/cameo/ContentPacks/TiberianSun/GDI/yaml/weapons.yaml | 1302, 1473 | TSShotgun | Warhead@SmallArmsPercentage |
| mods/cameo/ContentPacks/TiberianSun/GDI/yaml/weapons.yaml | 1303, 1318 | TSShotgun | Warhead@ChaingunPercentage |
| mods/cameo/ContentPacks/TiberianSun/Nod/yaml/weapons.yaml | 78, 110 | TSProton | Warhead@MediumFlameWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/Nod/yaml/weapons.yaml | 98, 104 | TSProton | Warhead@GroundFire |
| mods/cameo/ContentPacks/TiberianSun/Nod/yaml/weapons.yaml | 245, 270 | TSStankTibTusk | Projectile |
| mods/cameo/ContentPacks/TiberianSun/Nod/yaml/weapons.yaml | 250, 321 | TSStankTibTusk | Warhead@MediumMissilePercentage |
| mods/cameo/ContentPacks/TiberianSun/Nod/yaml/weapons.yaml | 253, 296 | TSStankTibTusk | Warhead@MediumChemicalWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/Nod/yaml/weapons.yaml | 441, 523 | TSCommandoShotgun | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/TiberianSun/Nod/yaml/weapons.yaml | 444, 611 | TSCommandoShotgun | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/Nod/yaml/weapons.yaml | 447, 658 | TSCommandoShotgun | Warhead@TankDestroyerCannonPercentage |
| mods/cameo/ContentPacks/TiberianSun/Nod/yaml/weapons.yaml | 453, 635 | TSCommandoShotgun | Warhead@SmallArmsPercentage |
| mods/cameo/ContentPacks/TiberianSun/Nod/yaml/weapons.yaml | 456, 486 | TSCommandoShotgun | Warhead@ChaingunPercentage |
| mods/cameo/ContentPacks/TiberianSun/Nod/yaml/weapons.yaml | 461, 472, 551 | TSCommandoShotgun | Warhead@ShotgunGrenadeAlly |
| mods/cameo/ContentPacks/TiberianSun/Nod/yaml/weapons.yaml | 464, 473, 581 | TSCommandoShotgun | Warhead@ShotgunShrapnelAlly |
| mods/cameo/ContentPacks/TiberianSun/Nod/yaml/weapons.yaml | 469, 547 | TSCommandoShotgun | Warhead@ShieldHit |
| mods/cameo/ContentPacks/TiberianSun/Nod/yaml/weapons.yaml | 471, 474 | TSCommandoShotgun | Projectile |
| mods/cameo/ContentPacks/TiberianSun/Nod/yaml/weapons.yaml | 707, 731 | laserelitecadregun | Projectile |
| mods/cameo/ContentPacks/TiberianSun/Nod/yaml/weapons.yaml | 718, 795 | laserelitecadregun | Warhead@SmallArmsPercentage |
| mods/cameo/ContentPacks/TiberianSun/Nod/yaml/weapons.yaml | 720, 741 | laserelitecadregun | Warhead@ChaingunPercentage |
| mods/cameo/ContentPacks/TiberianSun/Shared/yaml/misc.yaml | 990, 992 | ts_tree24.Husk | RenderSprites |
| mods/cameo/ContentPacks/Warcraft2/Humans/yaml/sequences.yaml | 150, 153 | wc2_humans_guardtower | Defaults |
| mods/cameo/ContentPacks/Warcraft2/Humans/yaml/sequences.yaml | 158, 161 | wc2_humans_cannontower | Defaults |
| mods/cameo/ContentPacks/Warcraft2/Humans/yaml/sequences.yaml | 166, 168 | wc2_humans_wall | Defaults |
| mods/cameo/ContentPacks/Warcraft2/Orcs/yaml/sequences.yaml | 165, 167 | wc2_orcs_wall | Defaults |
| mods/cameo/ContentPacks/Warcraft2/Orcs/yaml/sequences.yaml | 583, 587 | wc2_orcs_ogre | idle-ogremage |
| mods/cameo/rules/advancewars.yaml | 1068, 1071 | ^AdvanceWarsUniversalPowers | RevealsShroudMultiplier@sonjapower1 |
| mods/cameo/rules/advancewars.yaml | 1136, 1146 | ^AdvanceWarsVehicleAttack | SpeedMultiplier@jesspower1 |
| mods/cameo/rules/advancewars.yaml | 2389, 2394 | hq.orange | Production@Research |
| mods/cameo/rules/advancewars.yaml | 5118, 5152 | awfortress | Building |
| mods/cameo/rules/advancewars.yaml | 5638, 5643 | awyard.orange | HitShape |
| mods/cameo/rules/advancewars.yaml | 5972, 5975, 5978, 5981, 5984, 5987, 5990, 5993 | awlab > ClassicAirstrikePower@Duster > Squad | awdustersupport |
| mods/cameo/rules/advancewars.yaml | 10784, 10845 | awbomber | ProducibleWithLevel |
| mods/cameo/rules/advancewars.yaml | 10946, 10951 | awblackbomb | ProducibleWithLevel |
| mods/cameo/rules/advancewars.yaml | 12322, 12329 | awhydrosupport | RenderSprites |
| mods/cameo/rules/advancewars.yaml | 12346, 12353 | awdustersupport | RenderSprites |
| mods/cameo/rules/ants.yaml | 71, 80 | QANT | Voiced |
| mods/cameo/rules/ants.yaml | 754, 774 | defenseant | AutoTarget |
| mods/cameo/rules/camea.yaml | 583, 584 | mslo.camea > RevealsShroud | RequiresCondition |
| mods/cameo/rules/casino.yaml | 232, 253 | Casino_Regular_Crate_1 | GiveUnitCrateAction@e6 |
| mods/cameo/rules/challenge.yaml | 27, 33 | World > FactionCA@x_monsters | Name |
| mods/cameo/rules/challenge.yaml | 28, 34 | World > FactionCA@x_monsters | InternalName |
| mods/cameo/rules/challenge.yaml | 30, 37 | World > FactionCA@x_monsters | Side |
| mods/cameo/rules/challenge.yaml | 31, 38 | World > FactionCA@x_monsters | Description |
| mods/cameo/rules/darkreign.yaml | 3469, 3476 | drnavyard.freedomguard | HitShape |
| mods/cameo/rules/darkreign.yaml | 7100, 7111 | drvortextank | WithSpriteTurret |
| mods/cameo/rules/darkreign.yaml | 7699, 7706 | drnavyard.terror | HitShape |
| mods/cameo/rules/darkreign.yaml | 9116, 9123 | drnavyard.eodalien | HitShape |
| mods/cameo/rules/darkreign.yaml | 10864, 10866 | drshelter | RenderSprites |
| mods/cameo/rules/darkreign.yaml | 11017, 11024 | satanclawzcrate | GiveUnitCrateAction |
| mods/cameo/rules/darkreign.yaml | 11263, 11265 | drsubterrean | RenderSprites |
| mods/cameo/rules/darkreign.yaml | 11278, 11280 | drfarm | RenderSprites |
| mods/cameo/rules/darkreign.yaml | 11298, 11300 | drfarm2 | RenderSprites |
| mods/cameo/rules/darkreign.yaml | 11318, 11320 | drfarm3 | RenderSprites |
| mods/cameo/rules/darkreign.yaml | 11338, 11340 | drrural | RenderSprites |
| mods/cameo/rules/darkreign.yaml | 11353, 11355 | drcomercial | RenderSprites |
| mods/cameo/rules/darkreign.yaml | 11368, 11370 | drctech | RenderSprites |
| mods/cameo/rules/darkreign.yaml | 11383, 11385 | drconcessionaire | RenderSprites |
| mods/cameo/rules/darkreign.yaml | 11398, 11400 | drtents | RenderSprites |
| mods/cameo/rules/dune2.yaml | 621, 625 | dunemcv | RenderSprites |
| mods/cameo/rules/generals.yaml | 3618, 3654 | glbggy | RenderSprites |
| mods/cameo/rules/generals.yaml | 4905, 4937 | glworker | AutoTarget |
| mods/cameo/rules/generals.yaml | 7386, 7401 | charty | Explodes |
| mods/cameo/rules/generals.yaml | 7595, 7596 | choverlord > WithRangeCircle@propaganda | RequiresCondition |
| mods/cameo/rules/generals.yaml | 8125, 8140 | checm | Turreted |
| mods/cameo/rules/generals.yaml | 9157, 9175 | chhelix | ReloadDelayMultiplier@BUNKER |
| mods/cameo/rules/generals.yaml | 11423, 11425 | usasupply | CustomSellValue |
| mods/cameo/rules/generals.yaml | 11510, 11533 | usahook | SpawnActorOnDeath |
| mods/cameo/rules/generals.yaml | 12472, 12475 | usastealth | GrantConditionOnPrerequisite@selectusaairforce |
| mods/cameo/rules/generals.yaml | 12768, 12774 | usacomanche | ProductionCostMultiplier@selectusaairforce |
| mods/cameo/rules/generals.yaml | 12810, 12904 | usafirebase | RenderSprites |
| mods/cameo/rules/halloween.yaml | 247, 257 | halloween_crypto | Voiced |
| mods/cameo/rules/halloween.yaml | 585, 587 | halloween_spirittower | SpawnActorOnDeath@death1 |
| mods/cameo/rules/halloween.yaml | 999, 1013 | halloween_skeleton | Armament |
| mods/cameo/rules/halloween.yaml | 1046, 1051 | halloween_demon | RenderSprites |
| mods/cameo/rules/halloween.yaml | 1140, 1145 | halloween_cow | RenderSprites |
| mods/cameo/rules/halloween.yaml | 1325, 1327 | halloween_franky | RenderSprites |
| mods/cameo/rules/halloween.yaml | 1468, 1473 | halloween_clown3 | ChangesHealth |
| mods/cameo/rules/heroes.yaml | 636, 639 | volkovcc > Buildable | Prerequisites |
| mods/cameo/rules/heroes.yaml | 1053, 1057, 1060 | TSNASHWA | WithAmmoPipsDecoration |
| mods/cameo/rules/heroes.yaml | 1084, 1087 | TSNASHWABIKE | ProvidesPrerequisite@nashwabike |
| mods/cameo/rules/heroes.yaml | 1386, 1404 | mutantseverus | RevealsShroud |
| mods/cameo/rules/heroes.yaml | 1438, 1462 | severuscabal | PeriodicExplosion@circle2 |
| mods/cameo/rules/heroes.yaml | 1441, 1465 | severuscabal | PeriodicExplosion@circleheal2 |
| mods/cameo/rules/heroes.yaml | 1507, 1530 | severuscabal | RevealsShroud |
| mods/cameo/rules/heroes.yaml | 1679, 1683 | JACK | WithHarvesterPipsDecoration |
| mods/cameo/rules/infected.yaml | 220, 223 | zbio | Explodes@6 |
| mods/cameo/rules/iok.yaml | 562, 566 | IOKPROC | ProvidesPrerequisite@buildingname |
| mods/cameo/rules/iok.yaml | 571, 575 | IOKPROC | SpawnActorOnDeath@hole |
| mods/cameo/rules/iok.yaml | 1000, 1002 | IOKSITE > Buildable | Prerequisites |
| mods/cameo/rules/iok.yaml | 1192, 1202 | IOKJETPLANE | AttackAircraft |
| mods/cameo/rules/lostunits.yaml | 1214, 1323 | dalek | AttackFrontal |
| mods/cameo/rules/mindustry.yaml | 110, 115 | mindclass_core | RenderSprites |
| mods/cameo/rules/monsters.yaml | 158, 180 | trex | Buildable |
| mods/cameo/rules/monsters.yaml | 284, 289 | RA2DEMON | RenderSprites |
| mods/cameo/rules/monsters.yaml | 433, 447 | RA2SKELETON | Armament |
| mods/cameo/rules/monsters.yaml | 595, 601 | RA2TRIPOD | DeathSounds |
| mods/cameo/rules/redalert2.yaml | 1083, 1103 | ra2sqd | AttackFrontal |
| mods/cameo/rules/redalert2.yaml | 1196, 1236 | ra2dest | Selectable |
| mods/cameo/rules/redalert2.yaml | 1668, 1676 | yrbpln | Contrail@1 |
| mods/cameo/rules/redalert2.yaml | 1671, 1682 | yrbpln | Contrail@2 |
| mods/cameo/rules/redalert2.yaml | 1674, 1691 | yrbpln | SpawnActorOnDeath |
| mods/cameo/rules/redalert2.yaml | 2473, 2475 | ra2sidewind | Voiced |
| mods/cameo/rules/sc2k.yaml | 1006, 1013 | SC2KMARINA | HitShape |
| mods/cameo/rules/sc2k.yaml | 1296, 1303 | SC2KMISSILESILO | Building |
| mods/cameo/rules/sc2k.yaml | 1825, 1835 | SC2KJETPLANE | AttackAircraft |
| mods/cameo/rules/sc2k.yaml | 2026, 2062 | SC2KPOLICECAR | Voiced |
| mods/cameo/rules/sc2k.yaml | 2114, 2123 | SC2KFIRETRUCK | Voiced |
| mods/cameo/rules/shockwave.yaml | 1083, 1086, 1089, 1092, 1095, 1098, 1101, 1104, 1107, 1110, 1113, 1116, 1119 | ^ShockwaveUSASupportPowers > ClassicAirstrikePower@susaf16sp > Squad | susaf16 |
| mods/cameo/rules/shockwave.yaml | 1183, 1186 | ^ShockwaveUSASupportPowers > ClassicAirstrikePower@susaucav2 > Squad | susabomber.laser |
| mods/cameo/rules/shockwave.yaml | 1218, 1221, 1224 | ^ShockwaveUSASupportPowers > ClassicAirstrikePower@susaucav3 > Squad | susabomber.laser |
| mods/cameo/rules/shockwave.yaml | 3150, 3187 | susadecoydrone | Disguise |
| mods/cameo/rules/shockwave.yaml | 3833, 3876 | susaunstableeffects | RenderSprites |
| mods/cameo/rules/shockwave.yaml | 5370, 5377 | susaacolytedrone | RevealsShroud |
| mods/cameo/rules/shockwave.yaml | 5930, 5951 | susastarlifter | SpawnActorOnDeath |
| mods/cameo/rules/shockwave.yaml | 6621, 6624 | susaphalynx | Selectable |
| mods/cameo/rules/shockwave.yaml | 6992, 7003 | susamissilesilo | Selectable |
| mods/cameo/rules/shockwave.yaml | 7193, 7212, 7236 | susaamdggrenade.para | WithSpriteBody |
| mods/cameo/rules/shockwave.yaml | 7829, 7844 | sglairpad | Reservable |
| mods/cameo/rules/shockwave.yaml | 8336, 8347 | sglkatyusha | RenderSprites |
| mods/cameo/rules/shockwave.yaml | 8993, 9007 | sglmobilesupplytruck | RenderSprites |
| mods/cameo/rules/shockwave.yaml | 9337, 9352 | sglbadger | RenderSprites |
| mods/cameo/rules/shockwave.yaml | 12737, 12770 | schchaff | RenderSprites |
| mods/cameo/rules/shockwave.yaml | 14004, 14007 | schramjet | Selectable |
| mods/cameo/rules/shockwave.yaml | 15241, 15249 | schtankhunter_nuke | Valued |
| mods/cameo/rules/shockwave.yaml | 16452, 16474 | schsupplyhelicopterleang | SpawnActorOnDeath |
| mods/cameo/rules/simcity.yaml | 242, 245 | CITYTRUCK > Buildable | Prerequisites |
| mods/cameo/rules/simcity.yaml | 361, 370 | CITYFIRETRUCK | Voiced |
| mods/cameo/rules/simcity.yaml | 1669, 1676 | CITYFIREFIGHTER | Voiced |
| mods/cameo/rules/simcity.yaml | 1726, 1732 | CITYPOLICEOFFICER | Voiced |
| mods/cameo/rules/sow.yaml | 200, 203 | ^SowPowerBoost | ProductionTimeMultiplier@power5 |
| mods/cameo/rules/sow.yaml | 268, 276 | sowheadquarters | Armor |
| mods/cameo/rules/sow.yaml | 435, 438 | sowlightfactory | GrantConditionOnPrerequisite@2 |
| mods/cameo/rules/sow.yaml | 531, 542 | sowmediumfactory | GrantConditionOnDamageState |
| mods/cameo/rules/sow.yaml | 573, 576 | sowmediumfactory | GrantConditionOnPrerequisite@2 |
| mods/cameo/rules/sow.yaml | 640, 653 | sowheavyfactory | ProvidesPrerequisite@buildingname |
| mods/cameo/rules/sow.yaml | 695, 698 | sowheavyfactory | GrantConditionOnPrerequisite@2 |
| mods/cameo/rules/sow.yaml | 626, 631 | sowheavyfactory > Buildable | Prerequisites |
| mods/cameo/rules/sow.yaml | 762, 773 | sowabcfactory | ProvidesPrerequisite@buildingname |
| mods/cameo/rules/sow.yaml | 1042, 1045 | sowgoldmine | CashTricklerMultiplier@goldupgrade2 |
| mods/cameo/rules/sow.yaml | 1186, 1189 | sowpower | GrantConditionOnPrerequisite@sowpower |
| mods/cameo/rules/sow.yaml | 2690, 2700 | sow_ht_antiair | DetectCloaked |
| mods/cameo/rules/sow.yaml | 3070, 3084 | sow_mech_avenger | Voiced |
| mods/cameo/rules/sow.yaml | 3190, 3202 | sow_mech_kodiak | Voiced |
| mods/cameo/rules/sow.yaml | 3302, 3315 | sow_mech_gatling | Voiced |
| mods/cameo/rules/sow.yaml | 3422, 3434 | sow_mech_jaguar | Voiced |
| mods/cameo/rules/sow.yaml | 3547, 3559 | sow_mech_achilles | Voiced |
| mods/cameo/rules/sow.yaml | 4034, 4092 | sowfighter | RenderSprites |
| mods/cameo/rules/starcraft.yaml | 520, 562 | SCWRAITHDRONE | AttackAircraft |
| mods/cameo/rules/starcraft.yaml | 852, 901 | SCINTERCEPTOR | AttackAircraft |
| mods/cameo/rules/starwars.yaml | 207, 226 | ^SWFortressBuilding | Selectable |
| mods/cameo/rules/starwars.yaml | 3706, 3767 | swpalace | DetectCloaked |
| mods/cameo/rules/starwars.yaml | 3746, 3751 | swpalace | ProvidesPrerequisite@buildingname |
| mods/cameo/rules/starwars.yaml | 4307, 4315 | swindustrialplant | HitShape |
| mods/cameo/rules/starwars.yaml | 7116, 7123 | swexecutor.husk | RenderSprites |
| mods/cameo/rules/starwars.yaml | 8582, 8611 | swjabbaparty | Cargo |
| mods/cameo/rules/test.yaml | 547, 553 | TRIPOD | DeathSounds |
| mods/cameo/rules/tiberiaalliances.yaml | 1229, 1233 | tajugg | RenderVoxels |
| mods/cameo/rules/tiberiaalliances.yaml | 3468, 3470 | tagdiyard | RenderSprites |
| mods/cameo/rules/tiberiaalliances.yaml | 3661, 3667 | taelecyard | RenderSprites |
| mods/cameo/rules/tiberiaalliances.yaml | 4354, 4368 | tagdihq | RevealsShroud |
| mods/cameo/rules/tiberiaalliances.yaml | 4443, 4457 | tagdihq2 | RevealsShroud |
| mods/cameo/rules/tiberiaalliances.yaml | 4488, 4502 | tanodhq2 | RevealsShroud |
| mods/cameo/rules/tiberiaalliances.yaml | 4675, 4689 | tagdiskystrike | RevealsShroud |
| mods/cameo/rules/tiberiaalliances.yaml | 4742, 4756 | tagdifalcon | RevealsShroud |
| mods/cameo/rules/tiberiaalliances.yaml | 4827, 4841 | tagdiion | RevealsShroud |
| mods/cameo/rules/tiberiaalliances.yaml | 4899, 4913 | tanodbladeofkane | RevealsShroud |
| mods/cameo/rules/tiberiaalliances.yaml | 4966, 4980 | tanodeyeofkane | RevealsShroud |
| mods/cameo/rules/tiberiaalliances.yaml | 5051, 5065 | tafistofkane | RevealsShroud |
| mods/cameo/rules/tiberiaalliances.yaml | 5223, 5237 | tatacitus | RevealsShroud |
| mods/cameo/rules/tiberiaalliances.yaml | 5549, 5562 | tamgnod | RenderVoxels |
| mods/cameo/rules/valentine.yaml | 91, 94 | LOVECRATE | GiveCashCrateAction@1 |
| mods/cameo/rules/valentine.yaml | 742, 749 | valentines_sc2kmarina | HitShape |
| mods/cameo/rules/valentine.yaml | 1645, 1649 | valovecraft | WithAmmoPipsDecoration |
| mods/cameo/rules/valentine.yaml | 1830, 1834 | giantcupido | WithAmmoPipsDecoration |
| mods/cameo/rules/valentine.yaml | 2384, 2387 | valentines_teletubby_po | Explodes@6 |
| mods/cameo/rules/valentine.yaml | 2553, 2557 | valentines_dd | WithAmmoPipsDecoration |
| mods/cameo/rules/warcraft1.yaml | 261, 324 | wc_h_townhall | BaseBuilding |
| mods/cameo/rules/warcraft1.yaml | 568, 613 | wc_h_lumbermill | ProvidesPrerequisite |
| mods/cameo/rules/warcraft1.yaml | 1412, 1415 | wc_h_mcv.bot > Buildable | Prerequisites |
| mods/cameo/rules/warcraft1.yaml | 1742, 1756 | wc_h_cleric | AutoTarget |
| mods/cameo/rules/wh40k.yaml | 734, 737 | wh40kstrategyo | WithSpriteTurret@addon2 |
| mods/cameo/rules/wh40k.yaml | 2070, 2076 | ^WH40KGuardArmorUpgrade | DamageMultiplier@wh40kupguardarmor |
| mods/cameo/rules/wh40k.yaml | 2613, 2647 | wh40kcommisair | Health |
| mods/cameo/rules/wh40k.yaml | 3098, 3103 | wh40kpsyker | AutoTarget |
| mods/cameo/rules/wh40k.yaml | 8526, 8549 | wh40kgretchin | AutoTarget |
| mods/cameo/rules/wh40k.yaml | 11740, 11746 | wh40kraptor2 | WithDecoration@addon |
| mods/cameo/rules/win98.yaml | 75, 121 | WIN98_MYCOMPUTER | BaseBuilding |
| mods/cameo/rules/win98.yaml | 182, 188 | WIN98_BARRACKS > Buildable | Prerequisites |
| mods/cameo/rules/win98.yaml | 245, 258 | WIN98_RECYCLEBIN | Selectable |
| mods/cameo/rules/win98.yaml | 311, 365 | WIN98_INPUT_DEVICES | ProvidesPrerequisite |
| mods/cameo/rules/win98.yaml | 313, 319 | WIN98_INPUT_DEVICES > Buildable | Prerequisites |
| mods/cameo/rules/win98.yaml | 388, 390 | WIN98_POWERPLANTADVANCED > Buildable | Prerequisites |
| mods/cameo/rules/win98.yaml | 421, 428 | WIN98_AQUANET | HitShape |
| mods/cameo/rules/win98.yaml | 942, 946 | WIN98_HARDWARE | Voiced |
| mods/cameo/rules/win98.yaml | 1550, 1552 | WIN98_KEYBOARD > Buildable | Prerequisites |
| mods/cameo/rules/win98.yaml | 1583, 1590 | WIN98_BITCOIN | WithInfantryBody |
| mods/cameo/rules/win98.yaml | 1620, 1630 | WIN98_MSN_BUTTERFLY | AttackAircraft |
| mods/cameo/rules/worms.yaml | 1417, 1444 | WTRUCK | Voiced |
| mods/cameo/rules/wz2100.yaml | 1126, 1145 | 2100WALL | Selectable |
| mods/cameo/rules/wz2100.yaml | 1342, 1349 | 2100FB | AttackTurreted |
| mods/cameo/rules/wz2100.yaml | 1421, 1431 | 2100RADT | RevealsShroud |
| mods/cameo/rules/wz2100.yaml | 1963, 2015 | 2100CHOPSHOPADV | ProvidesPrerequisite |
| mods/cameo/rules/wz2100.yaml | 4144, 4148 | 2100MCV.ALPHA | RenderSprites |
| mods/cameo/rules/wz2100.yaml | 4117, 4119 | 2100MCV.ALPHA > Buildable | Prerequisites |
| mods/cameo/rules/wz2100.yaml | 4707, 4711 | 2100CYCAN > Buildable | Prerequisites |
| mods/cameo/rules/xcom.yaml | 985, 998 | large_gun_turret.xcom | AttackTurreted |
| mods/cameo/rules/xcom.yaml | 1181, 1203 | xcom_drmn | RenderSprites |
| mods/cameo/rules/xcom.yaml | 1183, 1199 | xcom_drmn | Mobile |
| mods/cameo/rules/xcom.yaml | 1191, 1205 | xcom_drmn | DockClientManager |
| mods/cameo/rules/xmas.yaml | 51, 54 | XMASCRATE | GiveCashCrateAction@1 |
| mods/cameo/rules/xmas.yaml | 90, 95 | EVILCRATE | GiveUnitCrateAction |
| mods/cameo/rules/z.yaml | 1072, 1079 | zfort | ProvidesPrerequisite@buildingname |
| mods/cameo/sequences/advancewars.yaml | 408, 411, 416 | awmegatnk | Scale |
| mods/cameo/sequences/d2k.yaml | 1323, 1324 | hightech.harkonnen | Defaults |
| mods/cameo/sequences/d2k.yaml | 2247, 2248 | d2k_editor-overlay | Defaults |
| mods/cameo/sequences/d2k.yaml | 2480, 2481 | d2k_shroud | Defaults |
| mods/cameo/sequences/generals.yaml | 139, 142 | glamob | stand |
| mods/cameo/sequences/infected.yaml | 26, 29 | civzombie | stand |
| mods/cameo/sequences/infected.yaml | 93, 97 | zombiee6 | idle |
| mods/cameo/sequences/infected.yaml | 100, 103 | zombiee6 | stand |
| mods/cameo/sequences/iok.yaml | 329, 331 | iokpalace | Defaults |
| mods/cameo/sequences/lostunits.yaml | 28, 31 | rathf | die5 |
| mods/cameo/sequences/misc.yaml | 1493, 1497 | resources | ra2gold18 |
| mods/cameo/sequences/misc.yaml | 3579, 3588 | overlay | target-select |
| mods/cameo/sequences/n64.yaml | 631, 633 | n64gtwr > make > Combine | gtwrmake |
| mods/cameo/sequences/redalert2.yaml | 3790, 3796 | yrslav | cheer |
| mods/cameo/sequences/redalert2.yaml | 3922, 3926 | ra2howi | muzzle |
| mods/cameo/sequences/redalert2.yaml | 3935, 3939 | ra2arty | muzzle |
| mods/cameo/sequences/shared_effects.yaml | 232, 237 | tscloud1 | Filename |
| mods/cameo/sequences/starwars.yaml | 1016, 1018 | swgtwr > make > Combine | gtwrmake |
| mods/cameo/sequences/starwars.yaml | 1143, 1145 | swtmpl | Defaults |
| mods/cameo/sequences/starwars.yaml | 2472, 2475 | swjedi | die5 |
| mods/cameo/sequences/structures.yaml | 501, 503 | td_gdi_guardtower > make > Combine | gtwrmake |
| mods/cameo/sequences/tiberiandawn.yaml | 1485, 1488 | gdirigtower | muzzle |
| mods/cameo/sequences/warcraft1.yaml | 1368, 1374, 1383 | wc_n_portal | Scale |
| mods/cameo/tilesets/arrakis.yaml | 8238, 8244 | MultiBrushCollections > Segmented | MultiBrush@161 |
| mods/cameo/tilesets/cameo.yaml | 13131, 13132 | Templates > Template@56086 > Tiles | 1 |
| mods/cameo/tilesets/snow.yaml | 2518, 2519 | Templates > Template@2086 > Tiles | 1 |
| mods/cameo/weapons/advacewars.yaml | 265, 267 | AWGarrisonMG | Warhead@2Eff |
| mods/cameo/weapons/advacewars.yaml | 1083, 1085 | AWTeslaCrystal | Range |
| mods/cameo/weapons/advacewars.yaml | 1150, 1152 | AWLaserTurretRailgun | Warhead@1Dam |
| mods/cameo/weapons/advancewars.yaml | 259, 261 | AWGarrisonMG | Warhead@2Eff |
| mods/cameo/weapons/advancewars.yaml | 1068, 1070 | AWTeslaCrystal | Range |
| mods/cameo/weapons/advancewars.yaml | 1136, 1138 | AWLaserTurretRailgun | Warhead@1Dam |
| mods/cameo/weapons/classicdoom.yaml | 74, 76 | WolfenGrooseMinigun | Warhead@1Dam |
| mods/cameo/weapons/classicdoom.yaml | 92, 94 | WolfenMechaHetlerMinigun | Warhead@1Dam |
| mods/cameo/weapons/classicdoom.yaml | 101, 103 | WolfenMechaHetlerMinigun2 | Warhead@1Dam |
| mods/cameo/weapons/classicdoom.yaml | 250, 255 | WolfenSchabbsMutate | Report |
| mods/cameo/weapons/generals.yaml | 1796, 1799 | USACrusaderCannon | Report |
| mods/cameo/weapons/generals.yaml | 1805, 1808 | USAPaladinCannon | Report |
| mods/cameo/weapons/lostunits.yaml | 278, 283 | InfantryExplode | Warhead@3Clust |
| mods/cameo/weapons/monsters.yaml | 792, 798 | MothershipExplosion | Warhead@11Dam_areanuke3 |
| mods/cameo/weapons/other.yaml | 1186, 1192 | MothershipExplosion | Warhead@11Dam_areanuke3 |
| mods/cameo/weapons/outpost2.yaml | 47, 100 | edenMobileLaser | Projectile |
| mods/cameo/weapons/outpost2.yaml | 56, 168 | edenMobileLaser | Warhead@TankDestroyerCannonPercentage |
| mods/cameo/weapons/outpost2.yaml | 59, 111 | edenMobileLaser | Warhead@ChaingunPercentage |
| mods/cameo/weapons/outpost2.yaml | 62, 142 | edenMobileLaser | Warhead@FlakWeaponPercentage |
| mods/cameo/weapons/outpost2.yaml | 253, 368 | edenRailgun | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/weapons/outpost2.yaml | 260, 317 | edenRailgun | Warhead@LightChemicalWeaponPercentage |
| mods/cameo/weapons/outpost2.yaml | 264, 392 | edenRailgun | Warhead@TankDestroyerCannonPercentage |
| mods/cameo/weapons/outpost2.yaml | 268, 343 | edenRailgun | Warhead@MediumCannonPercentage |
| mods/cameo/weapons/outpost2.yaml | 272, 295 | edenRailgun | Warhead@HeavyCannonPercentage |
| mods/cameo/weapons/outpost2.yaml | 499, 613 | eden_EMP | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/weapons/outpost2.yaml | 508, 572 | eden_EMP | Warhead@HeavyCannonPercentage |
| mods/cameo/weapons/outpost2.yaml | 526, 592 | eden_EMP | Warhead@MediumMissilePercentage |
| mods/cameo/weapons/outpost2.yaml | 930, 964 | plymouthSticky | Projectile |
| mods/cameo/weapons/outpost2.yaml | 935, 977 | plymouthSticky | Warhead@LightChemicalWeaponPercentage |
| mods/cameo/weapons/outpost2.yaml | 938, 1004 | plymouthSticky | Warhead@TankDestroyerCannonPercentage |
| mods/cameo/weapons/outpost2.yaml | 953, 968 | plymouthSticky | Warhead@Effect |
| mods/cameo/weapons/outpost2.yaml | 1032, 1057 | plymouthStickyTiger | Warhead@LightChemicalWeaponPercentage |
| mods/cameo/weapons/outpost2.yaml | 1042, 1082 | plymouthStickyTiger | Warhead@MediumChemicalWeaponPercentage |
| mods/cameo/weapons/outpost2.yaml | 1043, 1111 | plymouthStickyTiger | Warhead@TankDestroyerCannonPercentage |
| mods/cameo/weapons/outpost2.yaml | 1144, 1198 | plymouthStickyDefence | Warhead@LightChemicalWeaponPercentage |
| mods/cameo/weapons/outpost2.yaml | 1150, 1223 | plymouthStickyDefence | Warhead@MediumChemicalWeaponPercentage |
| mods/cameo/weapons/outpost2.yaml | 1156, 1171 | plymouthStickyDefence | Warhead@HeavyChemicalWeaponPercentage |
| mods/cameo/weapons/outpost2.yaml | 1157, 1250 | plymouthStickyDefence | Warhead@TankDestroyerCannonPercentage |
| mods/cameo/weapons/outpost2.yaml | 1403, 1517 | plymouth_EMP | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/weapons/outpost2.yaml | 1412, 1476 | plymouth_EMP | Warhead@HeavyCannonPercentage |
| mods/cameo/weapons/outpost2.yaml | 1430, 1496 | plymouth_EMP | Warhead@MediumMissilePercentage |
| mods/cameo/weapons/redalert2.yaml | 2723, 2731 | LightningBolt | Warhead@TeslaChargedExtraDamage |
| mods/cameo/weapons/shockwave.yaml | 1907, 1922 | SGLAngryMobMolotov | Warhead@3Eff |
| mods/cameo/weapons/sow.yaml | 28, 36 | ^SowFlame | ValidTargets |
| mods/cameo/weapons/starcraft2.yaml | 7, 18 | zealotPsionicBlades > Warhead@1Dam | Spread |
| mods/cameo/weapons/starcraft2.yaml | 8, 19 | zealotPsionicBlades > Warhead@1Dam | Damage |
| mods/cameo/weapons/starcraft2.yaml | 9, 21 | zealotPsionicBlades > Warhead@1Dam | Versus |
| mods/cameo/weapons/starwars.yaml | 814, 818 | SWNapalm | Burst |
| mods/cameo/weapons/starwars.yaml | 843, 847 | SWNapalm2 | Burst |
| mods/cameo/weapons/starwars.yaml | 867, 871 | SWNapalm3 | Burst |
| mods/cameo/weapons/wh40k.yaml | 354, 357 | WH40KShootaBoyzGun | Warhead@1Dam |


**FAIL** — D1 count 8 exceeds the baseline 0: a new ambiguous inheritance label was introduced.

