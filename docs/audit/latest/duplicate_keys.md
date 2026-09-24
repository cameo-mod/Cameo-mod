# audit_duplicate_keys — duplicate keys in one node (ambiguous merges)

Files scanned: **660** — D1 ambiguous inheritance labels: **1**, D2 merged duplicates: **681**


## D1 — duplicate inheritance labels with different parent values

| file | lines | node | key | values |
|---|---|---|---|---|
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
| Warhead@Effect | 3 |
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
| mods/cameo/ContentPacks/D2k/Ixian/yaml/weapons.yaml | 40, 87 | DuelistTankCannon | Projectile |
| mods/cameo/ContentPacks/D2k/Ixian/yaml/weapons.yaml | 66, 90 | DuelistTankCannon | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/D2k/Ixian/yaml/weapons.yaml | 74, 114 | DuelistTankCannon | Warhead@TankDestroyerCannonPercentage |
| mods/cameo/ContentPacks/D2k/Ixian/yaml/weapons.yaml | 170, 207 | D2K_155mm2 | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/D2k/Ixian/yaml/weapons.yaml | 483, 520 | IxRailgunDroneBullet | Projectile |
| mods/cameo/ContentPacks/D2k/Ixian/yaml/weapons.yaml | 518, 525 | IxRailgunDroneBullet | Warhead@FlakWeaponPercentage |
| mods/cameo/ContentPacks/D2k/Ixian/yaml/weapons.yaml | 519, 549 | IxRailgunDroneBullet | Warhead@TankDestroyerCannonPercentage |
| mods/cameo/ContentPacks/D2k/Ixian/yaml/weapons.yaml | 717, 783 | RashidanGun | Warhead@LightMissilePercentage |
| mods/cameo/ContentPacks/D2k/Ixian/yaml/weapons.yaml | 720, 805 | RashidanGun | Warhead@MediumMissilePercentage |
| mods/cameo/ContentPacks/D2k/Ixian/yaml/weapons.yaml | 723, 834 | RashidanGun | Warhead@SmallArmsPercentage |
| mods/cameo/ContentPacks/D2k/Ixian/yaml/weapons.yaml | 726, 759 | RashidanGun | Warhead@ChaingunPercentage |
| mods/cameo/ContentPacks/D2k/Ixian/yaml/weapons.yaml | 1034, 1059 | HMG_Duelist | Projectile |
| mods/cameo/ContentPacks/D2k/Ixian/yaml/weapons.yaml | 1037, 1153 | HMG_Duelist | Warhead@LightMissilePercentage |
| mods/cameo/ContentPacks/D2k/Ixian/yaml/weapons.yaml | 1040, 1130 | HMG_Duelist | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/D2k/Ixian/yaml/weapons.yaml | 1043, 1204 | HMG_Duelist | Warhead@SmallArmsPercentage |
| mods/cameo/ContentPacks/D2k/Ixian/yaml/weapons.yaml | 1046, 1175 | HMG_Duelist | Warhead@MediumMissilePercentage |
| mods/cameo/ContentPacks/D2k/Ixian/yaml/weapons.yaml | 1049, 1108 | HMG_Duelist | Warhead@FlakWeaponPercentage |
| mods/cameo/ContentPacks/D2k/Ixian/yaml/weapons.yaml | 1052, 1084 | HMG_Duelist | Warhead@ChaingunPercentage |
| mods/cameo/ContentPacks/D2k/Ixian/yaml/weapons.yaml | 1253, 1351 | HMG_Duelist_upgrade | Warhead@HeavyMissilePercentage |
| mods/cameo/ContentPacks/D2k/Ixian/yaml/weapons.yaml | 1254, 1375 | HMG_Duelist_upgrade | Warhead@LightMissilePercentage |
| mods/cameo/ContentPacks/D2k/Ixian/yaml/weapons.yaml | 1255, 1326 | HMG_Duelist_upgrade | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/D2k/Ixian/yaml/weapons.yaml | 1256, 1427 | HMG_Duelist_upgrade | Warhead@SmallArmsPercentage |
| mods/cameo/ContentPacks/D2k/Ixian/yaml/weapons.yaml | 1257, 1399 | HMG_Duelist_upgrade | Warhead@MediumMissilePercentage |
| mods/cameo/ContentPacks/D2k/Ixian/yaml/weapons.yaml | 1258, 1302 | HMG_Duelist_upgrade | Warhead@FlakWeaponPercentage |
| mods/cameo/ContentPacks/D2k/Ixian/yaml/weapons.yaml | 1259, 1278 | HMG_Duelist_upgrade | Warhead@ChaingunPercentage |
| mods/cameo/ContentPacks/D2k/Ixian/yaml/weapons.yaml | 1584, 1608 | D2K_RocketsCymek | Projectile |
| mods/cameo/ContentPacks/D2k/Ixian/yaml/weapons.yaml | 1605, 1635 | D2K_RocketsCymek | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/D2k/Ixian/yaml/weapons.yaml | 1606, 1611 | D2K_RocketsCymek | Warhead@FlakWeaponPercentage |
| mods/cameo/ContentPacks/D2k/Ixian/yaml/weapons.yaml | 1607, 1660 | D2K_RocketsCymek | Warhead@HeavyMissilePercentage |
| mods/cameo/ContentPacks/D2k/Ixian/yaml/weapons.yaml | 1754, 1817 | ixian_farasha | Projectile |
| mods/cameo/ContentPacks/D2k/Ixian/yaml/weapons.yaml | 1786, 1827 | ixian_farasha | Warhead@HeavyFlameWeaponPercentage |
| mods/cameo/ContentPacks/D2k/Ordos/yaml/weapons.yaml | 63, 75 | 120mm_td | Projectile |
| mods/cameo/ContentPacks/D2k/Ordos/yaml/weapons.yaml | 72, 109 | 120mm_td | Warhead@MediumChemicalWeaponPercentage |
| mods/cameo/ContentPacks/D2k/Ordos/yaml/weapons.yaml | 73, 82 | 120mm_td | Warhead@LightChemicalWeaponPercentage |
| mods/cameo/ContentPacks/D2k/Ordos/yaml/weapons.yaml | 74, 141 | 120mm_td | Warhead@TankDestroyerCannonPercentage |
| mods/cameo/ContentPacks/D2k/Ordos/yaml/weapons.yaml | 718, 778 | eye_bomberguy | Projectile |
| mods/cameo/ContentPacks/D2k/Ordos/yaml/weapons.yaml | 733, 780 | eye_bomberguy | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/D2k/Ordos/yaml/weapons.yaml | 740, 804 | eye_bomberguy | Warhead@MediumChemicalWeaponPercentage |
| mods/cameo/ContentPacks/D2k/Ordos/yaml/weapons.yaml | 1434, 1436 | DeviatorMissile | Warhead@MediumChemicalWeaponPercentage |
| mods/cameo/ContentPacks/D2k/Ordos/yaml/weapons.yaml | 1435, 1463 | DeviatorMissile | Warhead@MediumMissilePercentage |
| mods/cameo/ContentPacks/D2k/Ordos/yaml/weapons.yaml | 1507, 1577 | DeviatorMissile_Artillery | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/ContentPacks/D2k/Ordos/yaml/weapons.yaml | 1519, 1549 | DeviatorMissile_Artillery | Warhead@HeavyFlameWeaponPercentage |
| mods/cameo/ContentPacks/D2k/Ordos/yaml/weapons.yaml | 1743, 1793 | Laboratory_Bioball | Warhead@MediumChemicalWeaponPercentage |
| mods/cameo/ContentPacks/D2k/Ordos/yaml/weapons.yaml | 1757, 1817 | Laboratory_Bioball | Warhead@MediumFlameWeaponPercentage |
| mods/cameo/ContentPacks/D2k/Ordos/yaml/weapons.yaml | 1781, 1839 | Laboratory_Bioball | Warhead@ShieldHit |
| mods/cameo/ContentPacks/D2k/Ordos/yaml/weapons.yaml | 1852, 1906 | facedancer_grenade | Projectile |
| mods/cameo/ContentPacks/D2k/Ordos/yaml/weapons.yaml | 1871, 1953 | facedancer_grenade | Warhead@MediumChemicalWeaponPercentage |
| mods/cameo/ContentPacks/D2k/Ordos/yaml/weapons.yaml | 1879, 1908 | facedancer_grenade | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/D2k/Ordos/yaml/weapons.yaml | 1883, 2000 | facedancer_grenade | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/ContentPacks/D2k/Ordos/yaml/weapons.yaml | 1887, 1931 | facedancer_grenade | Warhead@LightMissilePercentage |
| mods/cameo/ContentPacks/D2k/Ordos/yaml/weapons.yaml | 1891, 1978 | facedancer_grenade | Warhead@MediumMissilePercentage |
| mods/cameo/ContentPacks/D2k/Ordos/yaml/weapons.yaml | 2189, 2194 | autogun_tank | Projectile |
| mods/cameo/ContentPacks/D2k/Ordos/yaml/weapons.yaml | 2190, 2262 | autogun_tank | Warhead@HeavyAAWeaponPercentage |
| mods/cameo/ContentPacks/D2k/Ordos/yaml/weapons.yaml | 2191, 2238 | autogun_tank | Warhead@FlakWeaponPercentage |
| mods/cameo/ContentPacks/D2k/Ordos/yaml/weapons.yaml | 2192, 2286 | autogun_tank | Warhead@MediumMissilePercentage |
| mods/cameo/ContentPacks/D2k/Ordos/yaml/weapons.yaml | 2193, 2212 | autogun_tank | Warhead@ChaingunPercentage |
| mods/cameo/ContentPacks/D2k/Ordos/yaml/weapons.yaml | 2360, 2452 | ordos_airmine | Projectile |
| mods/cameo/ContentPacks/D2k/Ordos/yaml/weapons.yaml | 2382, 2552 | ordos_airmine | Warhead@TankDestroyerCannonPercentage |
| mods/cameo/ContentPacks/D2k/Ordos/yaml/weapons.yaml | 2394, 2503 | ordos_airmine | Warhead@LightChemicalWeaponPercentage |
| mods/cameo/ContentPacks/D2k/Ordos/yaml/weapons.yaml | 2404, 2460 | ordos_airmine | Warhead@FlakWeaponPercentage |
| mods/cameo/ContentPacks/D2k/Ordos/yaml/weapons.yaml | 2414, 2527 | ordos_airmine | Warhead@MediumMissilePercentage |
| mods/cameo/ContentPacks/D2k/Ordos/yaml/weapons.yaml | 2424, 2481 | ordos_airmine | Warhead@HeavyBombPercentage |
| mods/cameo/ContentPacks/D2k/Ordos/yaml/weapons.yaml | 2583, 2663 | ordos_lasertank | Projectile |
| mods/cameo/ContentPacks/D2k/Ordos/yaml/weapons.yaml | 2588, 2688 | ordos_lasertank | Warhead@FlakWeaponPercentage |
| mods/cameo/ContentPacks/D2k/Ordos/yaml/weapons.yaml | 2592, 2709 | ordos_lasertank | Warhead@MediumMissilePercentage |
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
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 164, 370 | td_gdi_boxer_boxercannonag | Warhead@SmallArmsPercentage |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 171, 319 | td_gdi_boxer_boxercannonag | Warhead@LightMissilePercentage |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 181, 296 | td_gdi_boxer_boxercannonag | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 188, 341 | td_gdi_boxer_boxercannonag | Warhead@MediumCannonPercentage |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 195, 246 | td_gdi_boxer_boxercannonag | Warhead@ChaingunPercentage |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 202, 270 | td_gdi_boxer_boxercannonag | Warhead@FlakWeaponPercentage |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 457, 588 | td_gdi_skyshield_skyshieldcannon | Warhead@SmallArmsPercentage |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 463, 559 | td_gdi_skyshield_skyshieldcannon | Warhead@LightMissilePercentage |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 469, 513 | td_gdi_skyshield_skyshieldcannon | Warhead@ChaingunPercentage |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 475, 537 | td_gdi_skyshield_skyshieldcannon | Warhead@FlakWeaponPercentage |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 641, 677 | td_gdi_archerartillery_archerartilleryshell | Warhead@HeavyCannonPercentage |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 840, 946 | td_gdi_orca_orcamissiles | Warhead@HeavyCannonPercentage |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 846, 1039 | td_gdi_orca_orcamissiles | Warhead@TankDestroyerCannonPercentage |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 855, 1016 | td_gdi_orca_orcamissiles | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 864, 923 | td_gdi_orca_orcamissiles | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 870, 967 | td_gdi_orca_orcamissiles | Warhead@HeavyMissilePercentage |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 876, 989 | td_gdi_orca_orcamissiles | Warhead@MediumMissilePercentage |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 887, 899 | td_gdi_orca_orcamissiles | Projectile |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 1195, 1283 | td_gdi_advancedguardtower_towermissile | Projectile |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 1204, 1446 | td_gdi_advancedguardtower_towermissile | Warhead@TankDestroyerCannonPercentage |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 1216, 1329 | td_gdi_advancedguardtower_towermissile | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 1228, 1423 | td_gdi_advancedguardtower_towermissile | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 1236, 1307 | td_gdi_advancedguardtower_towermissile | Warhead@FlakWeaponPercentage |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 1244, 1352 | td_gdi_advancedguardtower_towermissile | Warhead@HeavyAAWeaponPercentage |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 1252, 1374 | td_gdi_advancedguardtower_towermissile | Warhead@HeavyMissilePercentage |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 1260, 1396 | td_gdi_advancedguardtower_towermissile | Warhead@MediumMissilePercentage |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 1492, 1556 | td_gdi_sonicmissilesoldier_missilesoldierweapon | Projectile |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 1510, 1640 | td_gdi_sonicmissilesoldier_missilesoldierweapon | Warhead@TankDestroyerCannonPercentage |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 1519, 1587 | td_gdi_sonicmissilesoldier_missilesoldierweapon | Warhead@LightChemicalWeaponPercentage |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 1528, 1617 | td_gdi_sonicmissilesoldier_missilesoldierweapon | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 1540, 1565 | td_gdi_sonicmissilesoldier_missilesoldierweapon | Warhead@HeavyMissilePercentage |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 1697, 1779 | td_gdi_empgrenadier_grenade_emp | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 1701, 1737 | td_gdi_empgrenadier_grenade_emp | Warhead@HeavyCannonPercentage |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 1709, 1757 | td_gdi_empgrenadier_grenade_emp | Warhead@MediumMissilePercentage |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 1840, 1910 | td_gdi_heavysniper_rifle | Projectile |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 1849, 1988 | td_gdi_heavysniper_rifle | Warhead@SmallArmsPercentage |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 1854, 1960 | td_gdi_heavysniper_rifle | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 1859, 1939 | td_gdi_heavysniper_rifle | Warhead@FlakWeaponPercentage |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 1864, 1916 | td_gdi_heavysniper_rifle | Warhead@ChaingunPercentage |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 2154, 2217 | TDShotgun | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 2156, 2271 | TDShotgun | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 2158, 2321 | TDShotgun | Warhead@TankDestroyerCannonPercentage |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 2160, 2241 | TDShotgun | Warhead@MediumCannonPercentage |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 2162, 2295 | TDShotgun | Warhead@SmallArmsPercentage |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 2164, 2188 | TDShotgun | Warhead@ChaingunPercentage |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 2565, 2591 | td_gdi_defenserig_gdirigphalanx | Projectile |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 2569, 2728 | td_gdi_defenserig_gdirigphalanx | Warhead@TankDestroyerCannonPercentage |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 2572, 2703 | td_gdi_defenserig_gdirigphalanx | Warhead@SmallArmsPercentage |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 2575, 2651 | td_gdi_defenserig_gdirigphalanx | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 2578, 2674 | td_gdi_defenserig_gdirigphalanx | Warhead@MediumCannonPercentage |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 2581, 2625 | td_gdi_defenserig_gdirigphalanx | Warhead@FlakWeaponPercentage |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 2584, 2601 | td_gdi_defenserig_gdirigphalanx | Warhead@ChaingunPercentage |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 2836, 2890 | td_gdi_havoc_sniper | Projectile |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 2878, 2893 | td_gdi_havoc_sniper | Warhead@ChaingunPercentage |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 2959, 3010 | td_gdi_havoc_grenade | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 2962, 2986 | td_gdi_havoc_grenade | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 3045, 3091 | td_gdi_havoc_rifle | Projectile |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 3083, 3094 | td_gdi_havoc_rifle | Warhead@ChaingunPercentage |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 9, 25 | M16Laser | Projectile |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 12, 90 | M16Laser | Warhead@SmallArmsPercentage |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 14, 35 | M16Laser | Warhead@ChaingunPercentage |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 290, 354 | td_nod_artillery_artilleryshellupgrade | Projectile |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 305, 356 | td_nod_artillery_artilleryshellupgrade | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 311, 406 | td_nod_artillery_artilleryshellupgrade | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 318, 380 | td_nod_artillery_artilleryshellupgrade | Warhead@MediumChemicalWeaponPercentage |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 467, 502 | td_nod_reconbike_rocket | Projectile |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 473, 604 | td_nod_reconbike_rocket | Warhead@TankDestroyerCannonPercentage |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 480, 581 | td_nod_reconbike_rocket | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 485, 527 | td_nod_reconbike_rocket | Warhead@FlakWeaponPercentage |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 489, 553 | td_nod_reconbike_rocket | Warhead@LightMissilePercentage |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 779, 794 | td_nod_gunturret_turretgun | Projectile |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 782, 848 | td_nod_gunturret_turretgun | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 784, 801 | td_nod_gunturret_turretgun | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 786, 825 | td_nod_gunturret_turretgun | Warhead@MediumCannonPercentage |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 788, 872 | td_nod_gunturret_turretgun | Warhead@TankDestroyerCannonPercentage |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 902, 945 | td_nod_gunturret_turretgunblackmarket | Warhead@HeavyBombPercentage |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 906, 969 | td_nod_gunturret_turretgunblackmarket | Warhead@HeavyCannonPercentage |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 1168, 1190 | td_nod_chemicalrocketsoldier_chemrockets | Projectile |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 1171, 1210 | td_nod_chemicalrocketsoldier_chemrockets | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 1175, 1232 | td_nod_chemicalrocketsoldier_chemrockets | Warhead@LightChemicalWeaponPercentage |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 1179, 1256 | td_nod_chemicalrocketsoldier_chemrockets | Warhead@LightMissilePercentage |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 1309, 1336 | td_nod_chemicalattackbike_chemicalbikerockets | Projectile |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 1314, 1432 | td_nod_chemicalattackbike_chemicalbikerockets | Warhead@TankDestroyerCannonPercentage |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 1318, 1382 | td_nod_chemicalattackbike_chemicalbikerockets | Warhead@LightChemicalWeaponPercentage |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 1322, 1361 | td_nod_chemicalattackbike_chemicalbikerockets | Warhead@FlakWeaponPercentage |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 1326, 1406 | td_nod_chemicalattackbike_chemicalbikerockets | Warhead@MediumMissilePercentage |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 1470, 1492 | td_nod_chemicalstealthtank_chemicalstealthtankmissiles | Projectile |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 1474, 1559 | td_nod_chemicalstealthtank_chemicalstealthtankmissiles | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 1478, 1509 | td_nod_chemicalstealthtank_chemicalstealthtankmissiles | Warhead@MediumChemicalWeaponPercentage |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 1482, 1533 | td_nod_chemicalstealthtank_chemicalstealthtankmissiles | Warhead@MediumMissilePercentage |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 1664, 1723 | td_nod_buggymkii_machinegunbuggy2 | Warhead@HeavyCannonPercentage |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 1668, 1751 | td_nod_buggymkii_machinegunbuggy2 | Warhead@SmallArmsPercentage |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 1672, 1695 | td_nod_buggymkii_machinegunbuggy2 | Warhead@ChaingunPercentage |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 1958, 1978 | td_nod_lighttankmkii_lighttank2cannon | Projectile |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 1965, 1984 | td_nod_lighttankmkii_lighttank2cannon | Warhead@LightChemicalWeaponPercentage |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 1969, 2010 | td_nod_lighttankmkii_lighttank2cannon | Warhead@MediumCannonPercentage |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 2090, 2143 | td_nod_lasertrooper_blackhandlaser | Projectile |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 2100, 2186 | td_nod_lasertrooper_blackhandlaser | Warhead@TankDestroyerCannonPercentage |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 2104, 2156 | td_nod_lasertrooper_blackhandlaser | Warhead@FlakWeaponPercentage |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 2240, 2290 | td_nod_stealthsoldier_bhreddarts | Projectile |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 2254, 2381 | td_nod_stealthsoldier_bhreddarts | Warhead@TankDestroyerCannonPercentage |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 2259, 2309 | td_nod_stealthsoldier_bhreddarts | Warhead@ChaingunPercentage |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 2264, 2359 | td_nod_stealthsoldier_bhreddarts | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 2269, 2333 | td_nod_stealthsoldier_bhreddarts | Warhead@MediumMissilePercentage |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 2462, 2550 | td_nod_specterartillery_specterartilleryshellupgrade | Warhead@MediumChemicalWeaponPercentage |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 2485, 2527 | td_nod_specterartillery_specterartilleryshellupgrade | Warhead@MediumCannonPercentage |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 2489, 2505 | td_nod_specterartillery_specterartilleryshellupgrade | Warhead@HeavyCannonPercentage |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 2800, 2831 | td_nod_minigunner_minigun_laser | Projectile |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 2803, 2892 | td_nod_minigunner_minigun_laser | Warhead@SmallArmsPercentage |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 2807, 2841 | td_nod_minigunner_minigun_laser | Warhead@ChaingunPercentage |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 2822, 2888 | td_nod_minigunner_minigun_laser | Warhead@ShieldHit |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 2828, 2864 | td_nod_minigunner_minigun_laser | Warhead@LegacyLaserExtraDamage |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/aircraft.yaml | 230, 268 | cabal_hunterdrone | AttackAircraft |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 179, 248 | TSCABALObeliskLaserFire | Projectile |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 196, 319 | TSCABALObeliskLaserFire | Warhead@TankDestroyerCannonPercentage |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 211, 288 | TSCABALObeliskLaserFire | Warhead@MediumFlameWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 221, 262 | TSCABALObeliskLaserFire | Warhead@MediumChemicalWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 241, 312 | TSCABALObeliskLaserFire | Warhead@ShieldHit |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 354, 459 | TSHellfireTwin | Warhead@HeavyCannonPercentage |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 360, 552 | TSHellfireTwin | Warhead@TankDestroyerCannonPercentage |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 369, 529 | TSHellfireTwin | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 378, 436 | TSHellfireTwin | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 384, 480 | TSHellfireTwin | Warhead@HeavyMissilePercentage |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 390, 502 | TSHellfireTwin | Warhead@MediumMissilePercentage |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 401, 413 | TSHellfireTwin | Projectile |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 585, 651 | TSCABALEnlightedLaser | Projectile |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 595, 722 | TSCABALEnlightedLaser | Warhead@TankDestroyerCannonPercentage |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 610, 691 | TSCABALEnlightedLaser | Warhead@MediumFlameWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 620, 665 | TSCABALEnlightedLaser | Warhead@MediumChemicalWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 644, 715 | TSCABALEnlightedLaser | Warhead@ShieldHit |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 1490, 1531 | CabalArtilleryWalkerShell | Projectile |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 1505, 1535 | CabalArtilleryWalkerShell | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 1511, 1609 | CabalArtilleryWalkerShell | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 1515, 1581 | CabalArtilleryWalkerShell | Warhead@MediumCannonPercentage |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 1519, 1559 | CabalArtilleryWalkerShell | Warhead@HeavyCannonPercentage |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 1643, 1711 | CabalArtilleryWalkerShellUpgraded | Projectile |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 1658, 1726 | CabalArtilleryWalkerShellUpgraded | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 1664, 1851 | CabalArtilleryWalkerShellUpgraded | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 1668, 1797 | CabalArtilleryWalkerShellUpgraded | Warhead@MediumCannonPercentage |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 1672, 1750 | CabalArtilleryWalkerShellUpgraded | Warhead@HeavyCannonPercentage |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 1679, 1820 | CabalArtilleryWalkerShellUpgraded | Warhead@MediumChemicalWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 1687, 1772 | CabalArtilleryWalkerShellUpgraded | Warhead@MagicWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 1884, 1934 | CabalHunterKillerLasers | Projectile |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 1896, 1955 | CabalHunterKillerLasers | Warhead@MediumMissilePercentage |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 1992, 2070 | CabalHunterKillerLasers_elite | Projectile |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 2062, 2089 | CabalHunterKillerLasers_elite | Warhead@HeavyMissilePercentage |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 2222, 2239 | CabalDissolverSpray | Warhead@LightChemicalWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 2224, 2265 | CabalDissolverSpray | Warhead@TankDestroyerCannonPercentage |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 2807, 2879 | CabalMothershipRockets | Warhead@MediumChemicalWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 2835, 2855 | CabalMothershipRockets | Warhead@ArrowWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 2978, 3069 | CabalAscendedRockets | Warhead@LightMissilePercentage |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 2981, 3091 | CabalAscendedRockets | Warhead@MediumMissilePercentage |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 2984, 3047 | CabalAscendedRockets | Warhead@HeavyMissilePercentage |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 2987, 3118 | CabalAscendedRockets | Warhead@TankDestroyerCannonPercentage |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 2990, 3003 | CabalAscendedRockets | Warhead@FlakWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 2993, 3025 | CabalAscendedRockets | Warhead@HeavyAAWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 3747, 3784 | CabalCommandoPlasmaNeutron | Warhead@MediumChemicalWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 3849, 3886 | CabalCommandoPlasmaMk2Neutron | Warhead@MediumChemicalWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 3938, 3997 | CabalBeholderLaser | Projectile |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 3948, 4003 | CabalBeholderLaser | Warhead@HeavyCannonPercentage |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 3989, 4024 | CabalBeholderLaser | Warhead@ShieldHit |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/sequences.yaml | 62, 66 | forgotten_chemsprayinfantry | prone-shoot |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/sequences.yaml | 334, 375 | forgotten_zombiemutant | standup |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/sequences.yaml | 369, 384 | forgotten_zombiemutant | die-crushed |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 77, 100 | TS70mmTurChem | Projectile |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 98, 107 | TS70mmTurChem | Warhead@LightChemicalWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 99, 139 | TS70mmTurChem | Warhead@TankDestroyerCannonPercentage |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 192, 249 | TSChem120mmx | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 194, 218 | TSChem120mmx | Warhead@MediumChemicalWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 372, 376 | TSScoopDualTurChem | Warhead@MediumChemicalWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 373, 403 | TSScoopDualTurChem | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 447, 469 | TSHighVelocityChem | Projectile |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 449, 500 | TSHighVelocityChem | Warhead@TankDestroyerCannonPercentage |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 451, 474 | TSHighVelocityChem | Warhead@LightChemicalWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 542, 564 | TSHighVelocity2Chem | Projectile |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 544, 596 | TSHighVelocity2Chem | Warhead@TankDestroyerCannonPercentage |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 668, 776 | TSBusMortarChem | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 670, 693 | TSBusMortarChem | Warhead@HeavyBombPercentage |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 672, 743 | TSBusMortarChem | Warhead@MediumChemicalWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 674, 717 | TSBusMortarChem | Warhead@HeavyChemicalWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 1075, 1089 | TSChemBoatcannon | Warhead@HeavyBombPercentage |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 1078, 1112 | TSChemBoatcannon | Warhead@MediumChemicalWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 1324, 1363 | TSMammothTuskChem | Projectile |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 1341, 1398 | TSMammothTuskChem | Warhead@HeavyMissilePercentage |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 1344, 1373 | TSMammothTuskChem | Warhead@HeavyChemicalWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 1486, 1518 | TSChemRuinerMissile | Projectile |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 1491, 1560 | TSChemRuinerMissile | Warhead@LightChemicalWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 1494, 1585 | TSChemRuinerMissile | Warhead@MediumChemicalWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 1497, 1537 | TSChemRuinerMissile | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 1500, 1610 | TSChemRuinerMissile | Warhead@MediumMissilePercentage |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 1811, 1878 | MutFlamerChem | Warhead@MediumFlameWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 1820, 1852 | MutFlamerChem | Warhead@MediumChemicalWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 1918, 1959 | MutHFlamer | Warhead@MediumFlameWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 1922, 1934 | MutHFlamer | Warhead@HeavyFlameWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 1999, 2137 | MutHFlamerChem | Warhead@MediumFlameWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 2010, 2086 | MutHFlamerChem | Warhead@HeavyFlameWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 2019, 2111 | MutHFlamerChem | Warhead@MediumChemicalWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 2026, 2060 | MutHFlamerChem | Warhead@HeavyChemicalWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 2199, 2250 | TSFiendShardUP | Warhead@LightChemicalWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 2202, 2275 | TSFiendShardUP | Warhead@MediumChemicalWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 2205, 2225 | TSFiendShardUP | Warhead@HeavyChemicalWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 2312, 2335 | TSFiendShardBlue | Projectile |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 2317, 2339 | TSFiendShardBlue | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 2320, 2419 | TSFiendShardBlue | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 2323, 2362 | TSFiendShardBlue | Warhead@LightChemicalWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 2326, 2387 | TSFiendShardBlue | Warhead@MediumChemicalWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 2453, 2492 | TSFiendShardBlueUP | Projectile |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 2458, 2496 | TSFiendShardBlueUP | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 2461, 2617 | TSFiendShardBlueUP | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 2464, 2567 | TSFiendShardBlueUP | Warhead@LightChemicalWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 2467, 2592 | TSFiendShardBlueUP | Warhead@MediumChemicalWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 2470, 2542 | TSFiendShardBlueUP | Warhead@HeavyChemicalWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 2473, 2519 | TSFiendShardBlueUP | Warhead@HeavyBombPercentage |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 2668, 2719 | TSChemsprayUP | Warhead@LightChemicalWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 2670, 2745 | TSChemsprayUP | Warhead@MediumChemicalWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 2672, 2693 | TSChemsprayUP | Warhead@HeavyChemicalWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 2786, 2837 | TSVisceroidSprayUP | Warhead@LightChemicalWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 2788, 2863 | TSVisceroidSprayUP | Warhead@MediumChemicalWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 2790, 2811 | TSVisceroidSprayUP | Warhead@HeavyChemicalWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 2967, 2987, 3061 | TSMutShotgun | Warhead@ShotgunGrenadeAlly |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 2970, 2988, 3091 | TSMutShotgun | Warhead@ShotgunShrapnelAlly |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 2975, 3057 | TSMutShotgun | Warhead@ShieldHit |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 2981, 2989 | TSMutShotgun | Projectile |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 2982, 3031 | TSMutShotgun | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 2983, 3121 | TSMutShotgun | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 2984, 3172 | TSMutShotgun | Warhead@TankDestroyerCannonPercentage |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 2985, 3147 | TSMutShotgun | Warhead@SmallArmsPercentage |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 2986, 3001 | TSMutShotgun | Warhead@ChaingunPercentage |
| mods/cameo/ContentPacks/TiberianSun/GDI/yaml/weapons.yaml | 31, 65 | TS30mmRail | Projectile |
| mods/cameo/ContentPacks/TiberianSun/GDI/yaml/weapons.yaml | 42, 111 | TS30mmRail | Warhead@MediumMissilePercentage |
| mods/cameo/ContentPacks/TiberianSun/GDI/yaml/weapons.yaml | 46, 88 | TS30mmRail | Warhead@FlakWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/GDI/yaml/weapons.yaml | 232, 233 | TSRPGTowerRail | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/GDI/yaml/weapons.yaml | 327, 381 | KodiakCannon | Projectile |
| mods/cameo/ContentPacks/TiberianSun/GDI/yaml/weapons.yaml | 335, 398 | KodiakCannon | Warhead@HeavyChemicalWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/GDI/yaml/weapons.yaml | 349, 423 | KodiakCannon | Warhead@HeavyMissilePercentage |
| mods/cameo/ContentPacks/TiberianSun/GDI/yaml/weapons.yaml | 720, 760 | TSHoverMissile | Projectile |
| mods/cameo/ContentPacks/TiberianSun/GDI/yaml/weapons.yaml | 728, 783 | TSHoverMissile | Warhead@FlakWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/GDI/yaml/weapons.yaml | 734, 805 | TSHoverMissile | Warhead@MediumMissilePercentage |
| mods/cameo/ContentPacks/TiberianSun/GDI/yaml/weapons.yaml | 743, 832 | TSHoverMissile | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/GDI/yaml/weapons.yaml | 865, 905 | TSDestroyerMissiles | Projectile |
| mods/cameo/ContentPacks/TiberianSun/GDI/yaml/weapons.yaml | 873, 928 | TSDestroyerMissiles | Warhead@FlakWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/GDI/yaml/weapons.yaml | 879, 950 | TSDestroyerMissiles | Warhead@MediumMissilePercentage |
| mods/cameo/ContentPacks/TiberianSun/GDI/yaml/weapons.yaml | 888, 977 | TSDestroyerMissiles | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/GDI/yaml/weapons.yaml | 1229, 1249, 1323 | TSShotgun | Warhead@ShotgunGrenadeAlly |
| mods/cameo/ContentPacks/TiberianSun/GDI/yaml/weapons.yaml | 1232, 1250, 1353 | TSShotgun | Warhead@ShotgunShrapnelAlly |
| mods/cameo/ContentPacks/TiberianSun/GDI/yaml/weapons.yaml | 1237, 1319 | TSShotgun | Warhead@ShieldHit |
| mods/cameo/ContentPacks/TiberianSun/GDI/yaml/weapons.yaml | 1243, 1251 | TSShotgun | Projectile |
| mods/cameo/ContentPacks/TiberianSun/GDI/yaml/weapons.yaml | 1244, 1293 | TSShotgun | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/TiberianSun/GDI/yaml/weapons.yaml | 1245, 1383 | TSShotgun | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/GDI/yaml/weapons.yaml | 1246, 1434 | TSShotgun | Warhead@TankDestroyerCannonPercentage |
| mods/cameo/ContentPacks/TiberianSun/GDI/yaml/weapons.yaml | 1247, 1409 | TSShotgun | Warhead@SmallArmsPercentage |
| mods/cameo/ContentPacks/TiberianSun/GDI/yaml/weapons.yaml | 1248, 1263 | TSShotgun | Warhead@ChaingunPercentage |
| mods/cameo/ContentPacks/TiberianSun/Nod/yaml/weapons.yaml | 75, 100 | TSProton | Warhead@MediumFlameWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/Nod/yaml/weapons.yaml | 93, 94 | TSProton | Warhead@GroundFire |
| mods/cameo/ContentPacks/TiberianSun/Nod/yaml/weapons.yaml | 221, 246 | TSStankTibTusk | Projectile |
| mods/cameo/ContentPacks/TiberianSun/Nod/yaml/weapons.yaml | 226, 288 | TSStankTibTusk | Warhead@MediumMissilePercentage |
| mods/cameo/ContentPacks/TiberianSun/Nod/yaml/weapons.yaml | 229, 263 | TSStankTibTusk | Warhead@MediumChemicalWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/Nod/yaml/weapons.yaml | 408, 485 | TSCommandoShotgun | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/TiberianSun/Nod/yaml/weapons.yaml | 411, 573 | TSCommandoShotgun | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/ContentPacks/TiberianSun/Nod/yaml/weapons.yaml | 414, 620 | TSCommandoShotgun | Warhead@TankDestroyerCannonPercentage |
| mods/cameo/ContentPacks/TiberianSun/Nod/yaml/weapons.yaml | 420, 597 | TSCommandoShotgun | Warhead@SmallArmsPercentage |
| mods/cameo/ContentPacks/TiberianSun/Nod/yaml/weapons.yaml | 423, 457 | TSCommandoShotgun | Warhead@ChaingunPercentage |
| mods/cameo/ContentPacks/TiberianSun/Nod/yaml/weapons.yaml | 428, 443, 513 | TSCommandoShotgun | Warhead@ShotgunGrenadeAlly |
| mods/cameo/ContentPacks/TiberianSun/Nod/yaml/weapons.yaml | 431, 444, 543 | TSCommandoShotgun | Warhead@ShotgunShrapnelAlly |
| mods/cameo/ContentPacks/TiberianSun/Nod/yaml/weapons.yaml | 436, 509 | TSCommandoShotgun | Warhead@ShieldHit |
| mods/cameo/ContentPacks/TiberianSun/Nod/yaml/weapons.yaml | 442, 445 | TSCommandoShotgun | Projectile |
| mods/cameo/ContentPacks/TiberianSun/Nod/yaml/weapons.yaml | 669, 693 | laserelitecadregun | Projectile |
| mods/cameo/ContentPacks/TiberianSun/Nod/yaml/weapons.yaml | 680, 758 | laserelitecadregun | Warhead@SmallArmsPercentage |
| mods/cameo/ContentPacks/TiberianSun/Nod/yaml/weapons.yaml | 682, 703 | laserelitecadregun | Warhead@ChaingunPercentage |
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


**FAIL** — D1 count 1 exceeds the baseline 0: a new ambiguous inheritance label was introduced.

