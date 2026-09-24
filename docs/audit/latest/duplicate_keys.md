# audit_duplicate_keys — duplicate keys in one node (ambiguous merges)

Files scanned: **661** — D1 ambiguous inheritance labels: **0**, D2 merged duplicates: **3963**


## D1 — duplicate inheritance labels with different parent values

_none found_


## D2 — duplicate keys by key name (top 40)

| key | occurrences |
|---|---|
| Projectile | 273 |
| Warhead@Effect | 242 |
| Warhead@EffectWater | 186 |
| Warhead@DuneRock | 172 |
| Warhead@DuneSand | 172 |
| Warhead@EffectAir | 168 |
| Warhead@RA2Crater | 154 |
| Warhead@Smudge | 150 |
| Warhead@Bullet_Medium_Flat | 106 |
| Warhead@ShrapnelWeaponPercentage | 83 |
| Warhead@RA2Scorch | 83 |
| Warhead@GrenadePercentage | 82 |
| Warhead@Concrete | 78 |
| Warhead@GroundFire | 78 |
| Warhead@Glow | 72 |
| Warhead@TankDestroyerCannonPercentage | 68 |
| Warhead@ShieldHit | 65 |
| Warhead@FlakWeaponPercentage | 64 |
| Warhead@MediumMissilePercentage | 63 |
| Warhead@MediumChemicalWeaponPercentage | 63 |
| Warhead@ChaingunPercentage | 53 |
| Warhead@MediumFlameWeaponPercentage | 52 |
| Warhead@Smudge1 | 50 |
| Warhead@Smudge2 | 50 |
| Warhead@Smudge2RA2 | 45 |
| Warhead@Effect2 | 45 |
| Warhead@SmallArmsPercentage | 43 |
| Warhead@HeavyBombPercentage | 41 |
| Warhead@LightChemicalWeaponPercentage | 39 |
| Warhead@ShieldHitEffect | 39 |
| Warhead@HeavyMissilePercentage | 37 |
| Warhead@Effect1 | 35 |
| RenderSprites | 33 |
| Warhead@HeavyCannonPercentage | 30 |
| Warhead@MissileAP_Medium_Flat | 26 |
| Warhead@MediumCannonPercentage | 25 |
| Warhead@1Dam | 25 |
| Warhead@LightMissilePercentage | 24 |
| Warhead@HeavyChemicalWeaponPercentage | 24 |
| Warhead@EMPUnit | 23 |


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
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 64, 68, 69 | ra1_allies_alliedgunturret_cannon | Warhead@CannonHE_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 169, 220 | ra1_allies_longbow_missile | Warhead@MissileAP_Heavy |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 232, 283, 284 | ra1_allies_longbow_missile_cryo | Warhead@BlastCryo_Medium |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 366, 412, 413 | ra1_allies_sheridanassaulttank_cannon | Warhead@CannonAP_Light |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 486, 491, 492 | ra1_allies_sheridanassaulttank_chaingun | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 614, 664, 665 | ra1_allies_rapierjumpjet_missile_AA | Warhead@Concussion_Medium |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 694, 741, 742 | ra1_allies_rapierjumpjet_missile_cryo_AA | Warhead@BlastCryo_Medium |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 761, 812 | ra1_allies_reconranger_recoillessgun | Projectile |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 802, 885 | ra1_allies_reconranger_recoillessgun | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 803, 850 | ra1_allies_reconranger_recoillessgun | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 804, 853 | ra1_allies_reconranger_recoillessgun | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 805, 856 | ra1_allies_reconranger_recoillessgun | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 806, 937 | ra1_allies_reconranger_recoillessgun | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 807, 826 | ra1_allies_reconranger_recoillessgun | Warhead@ChaingunPercentage |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 808, 861 | ra1_allies_reconranger_recoillessgun | Warhead@FlakWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 809, 962 | ra1_allies_reconranger_recoillessgun | Warhead@TankDestroyerCannonPercentage |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 810, 934 | ra1_allies_reconranger_recoillessgun | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 811, 910 | ra1_allies_reconranger_recoillessgun | Warhead@MediumMissilePercentage |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 989, 1009 | ra1_allies_reconranger_recoillessgun_cryo | Projectile |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 997, 1130 | ra1_allies_reconranger_recoillessgun_cryo | Warhead@PhysicalStateCryo |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 999, 1081 | ra1_allies_reconranger_recoillessgun_cryo | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 1000, 1042 | ra1_allies_reconranger_recoillessgun_cryo | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 1001, 1045 | ra1_allies_reconranger_recoillessgun_cryo | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 1002, 1052, 1187 | ra1_allies_reconranger_recoillessgun_cryo | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 1003, 1138 | ra1_allies_reconranger_recoillessgun_cryo | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 1004, 1018 | ra1_allies_reconranger_recoillessgun_cryo | Warhead@ChaingunPercentage |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 1005, 1057 | ra1_allies_reconranger_recoillessgun_cryo | Warhead@FlakWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 1006, 1163 | ra1_allies_reconranger_recoillessgun_cryo | Warhead@TankDestroyerCannonPercentage |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 1007, 1135 | ra1_allies_reconranger_recoillessgun_cryo | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 1008, 1106 | ra1_allies_reconranger_recoillessgun_cryo | Warhead@MediumMissilePercentage |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 1207, 1224 | ra1_allies_phasetransport_missile | Projectile |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 1209, 1363 | ra1_allies_phasetransport_missile | Warhead@TankDestroyerCannonPercentage |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 1210, 1360 | ra1_allies_phasetransport_missile | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 1211, 1251 | ra1_allies_phasetransport_missile | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 1212, 1254 | ra1_allies_phasetransport_missile | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 1213, 1349 | ra1_allies_phasetransport_missile | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 1214, 1257 | ra1_allies_phasetransport_missile | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 1215, 1267 | ra1_allies_phasetransport_missile | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 1216, 1262 | ra1_allies_phasetransport_missile | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 1217, 1296 | ra1_allies_phasetransport_missile | Warhead@Glow |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 1218, 1352 | ra1_allies_phasetransport_missile | Warhead@ShieldHit |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 1219, 1249 | ra1_allies_phasetransport_missile | Warhead@Concrete |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 1220, 1357 | ra1_allies_phasetransport_missile | Warhead@ShieldHitEffect |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 1221, 1300 | ra1_allies_phasetransport_missile | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 1222, 1272 | ra1_allies_phasetransport_missile | Warhead@FlakWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 1223, 1325 | ra1_allies_phasetransport_missile | Warhead@MediumMissilePercentage |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 1391, 1537 | ra1_allies_phasetransport_missile_cryo | Warhead@PhysicalStateCryo |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 1395, 1412 | ra1_allies_phasetransport_missile_cryo | Projectile |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 1397, 1555 | ra1_allies_phasetransport_missile_cryo | Warhead@TankDestroyerCannonPercentage |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 1398, 1552 | ra1_allies_phasetransport_missile_cryo | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 1399, 1439 | ra1_allies_phasetransport_missile_cryo | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 1400, 1442 | ra1_allies_phasetransport_missile_cryo | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 1401, 1541 | ra1_allies_phasetransport_missile_cryo | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 1402, 1445 | ra1_allies_phasetransport_missile_cryo | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 1403, 1455 | ra1_allies_phasetransport_missile_cryo | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 1404, 1450 | ra1_allies_phasetransport_missile_cryo | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 1405, 1484 | ra1_allies_phasetransport_missile_cryo | Warhead@Glow |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 1406, 1544 | ra1_allies_phasetransport_missile_cryo | Warhead@ShieldHit |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 1407, 1437 | ra1_allies_phasetransport_missile_cryo | Warhead@Concrete |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 1408, 1549 | ra1_allies_phasetransport_missile_cryo | Warhead@ShieldHitEffect |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 1409, 1488 | ra1_allies_phasetransport_missile_cryo | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 1410, 1460 | ra1_allies_phasetransport_missile_cryo | Warhead@FlakWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 1411, 1513 | ra1_allies_phasetransport_missile_cryo | Warhead@MediumMissilePercentage |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 1589, 1615 | ra1_allies_chronotank_missile | Projectile |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 1600, 1711 | ra1_allies_chronotank_missile | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 1601, 1795 | ra1_allies_chronotank_missile | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 1602, 1666 | ra1_allies_chronotank_missile | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 1603, 1669 | ra1_allies_chronotank_missile | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 1604, 1672 | ra1_allies_chronotank_missile | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 1605, 1682 | ra1_allies_chronotank_missile | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 1606, 1787 | ra1_allies_chronotank_missile | Warhead@ShieldHit |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 1607, 1664 | ra1_allies_chronotank_missile | Warhead@Concrete |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 1608, 1792 | ra1_allies_chronotank_missile | Warhead@ShieldHitEffect |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 1609, 1687 | ra1_allies_chronotank_missile | Warhead@FlakWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 1610, 1677 | ra1_allies_chronotank_missile | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 1611, 1640 | ra1_allies_chronotank_missile | Warhead@ChaingunPercentage |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 1612, 1736 | ra1_allies_chronotank_missile | Warhead@HeavyMissilePercentage |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 1613, 1784 | ra1_allies_chronotank_missile | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 1614, 1760 | ra1_allies_chronotank_missile | Warhead@MediumMissilePercentage |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 1803, 2001 | ra1_allies_chronotank_missile_cryo | Warhead@PhysicalStateCryo |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 1806, 1832 | ra1_allies_chronotank_missile_cryo | Projectile |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 1817, 1928 | ra1_allies_chronotank_missile_cryo | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 1818, 2016 | ra1_allies_chronotank_missile_cryo | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 1819, 1883 | ra1_allies_chronotank_missile_cryo | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 1820, 1886 | ra1_allies_chronotank_missile_cryo | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 1821, 1889 | ra1_allies_chronotank_missile_cryo | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 1822, 1899 | ra1_allies_chronotank_missile_cryo | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 1823, 2008 | ra1_allies_chronotank_missile_cryo | Warhead@ShieldHit |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 1824, 1881 | ra1_allies_chronotank_missile_cryo | Warhead@Concrete |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 1825, 2013 | ra1_allies_chronotank_missile_cryo | Warhead@ShieldHitEffect |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 1826, 1904 | ra1_allies_chronotank_missile_cryo | Warhead@FlakWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 1827, 1894 | ra1_allies_chronotank_missile_cryo | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 1828, 1857 | ra1_allies_chronotank_missile_cryo | Warhead@ChaingunPercentage |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 1829, 1953 | ra1_allies_chronotank_missile_cryo | Warhead@HeavyMissilePercentage |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 1830, 2005 | ra1_allies_chronotank_missile_cryo | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 1831, 1977 | ra1_allies_chronotank_missile_cryo | Warhead@MediumMissilePercentage |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 2023, 2033 | 155mmCryo | Projectile |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 2030, 2041 | 155mmCryo | Warhead@PhysicalStateCryo |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 2063, 2092 | 25mm | Projectile |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 2084, 2100 | 25mm | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 2085, 2191 | 25mm | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 2087, 2131 | 25mm | Warhead@LightFlameWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 2088, 2185 | 25mm | Warhead@RA2Scorch |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 2089, 2125 | 25mm | Warhead@GroundFire |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 2090, 2158 | 25mm | Warhead@MediumChemicalWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 2091, 2218 | 25mm | Warhead@TankDestroyerCannonPercentage |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 2360, 2375 | ra1_allies_alliedheavyaatank_cannon | Projectile |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 2361, 2471 | ra1_allies_alliedheavyaatank_cannon | Warhead@LightMissilePercentage |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 2362, 2532 | ra1_allies_alliedheavyaatank_cannon | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 2363, 2427 | ra1_allies_alliedheavyaatank_cannon | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 2364, 2496 | ra1_allies_alliedheavyaatank_cannon | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 2365, 2430 | ra1_allies_alliedheavyaatank_cannon | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 2366, 2433 | ra1_allies_alliedheavyaatank_cannon | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 2367, 2437 | ra1_allies_alliedheavyaatank_cannon | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 2368, 2499 | ra1_allies_alliedheavyaatank_cannon | Warhead@ShieldHit |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 2369, 2425 | ra1_allies_alliedheavyaatank_cannon | Warhead@Concrete |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 2370, 2504 | ra1_allies_alliedheavyaatank_cannon | Warhead@ShieldHitEffect |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 2371, 2446 | ra1_allies_alliedheavyaatank_cannon | Warhead@FlakWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 2372, 2442, 2536 | ra1_allies_alliedheavyaatank_cannon | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 2373, 2507 | ra1_allies_alliedheavyaatank_cannon | Warhead@SmallArmsPercentage |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 2374, 2400 | ra1_allies_alliedheavyaatank_cannon | Warhead@ChaingunPercentage |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 2593, 2595, 2596 | ra1_allies_alliedrocketsoldier_rocketsra | Warhead@MissileAP_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 2663, 2664 | ra1_allies_cargoplanebomber_parabomb | Warhead@Demolition_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml | 2728, 2729 | ra1_allies_cruiser_8inch | Warhead@Demolition_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 17, 21, 22 | CHGuardRifle | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 96, 100 | japan_imperialscoutsman_rifle | Projectile |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 97, 109 | japan_imperialscoutsman_rifle | Warhead@ChaingunPercentage |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 98, 138 | japan_imperialscoutsman_rifle | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 99, 146 | japan_imperialscoutsman_rifle | Warhead@SmallArmsPercentage |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 176, 233 | japan_imperialscoutsman_rifle_waveforce | Warhead@Railgun_Heavy |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 178, 183 | japan_imperialscoutsman_rifle_waveforce | Projectile |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 179, 194 | japan_imperialscoutsman_rifle_waveforce | Warhead@ChaingunPercentage |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 180, 228, 271 | japan_imperialscoutsman_rifle_waveforce | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 181, 239 | japan_imperialscoutsman_rifle_waveforce | Warhead@SmallArmsPercentage |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 182, 224 | japan_imperialscoutsman_rifle_waveforce | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 220, 268 | japan_imperialscoutsman_rifle_waveforce | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 263, 264 | japan_imperialscoutsman_rifle_waveforce | Warhead@Glow |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 281, 383 | TankBusterBeamCannon | Projectile |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 297, 471, 499 | TankBusterBeamCannon | Warhead@Railgun_Heavy |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 345, 472, 507 | TankBusterBeamCannon | Warhead@Railgun_Heavy_ExtraDamage |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 370, 473, 474 | TankBusterBeamCannon | Warhead@RailgunExtraDamage_Auxiliary |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 372, 468 | TankBusterBeamCannon | Warhead@ShieldHit |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 374, 438 | TankBusterBeamCannon | Warhead@TankDestroyerCannonPercentage |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 375, 435 | TankBusterBeamCannon | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 376, 387 | TankBusterBeamCannon | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 377, 390 | TankBusterBeamCannon | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 378, 432 | TankBusterBeamCannon | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 379, 398 | TankBusterBeamCannon | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 380, 393 | TankBusterBeamCannon | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 381, 403 | TankBusterBeamCannon | Warhead@Glow |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 382, 407 | TankBusterBeamCannon | Warhead@MediumCannonPercentage |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 547, 565 | PlasmaFlamer | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 554, 604 | PlasmaFlamer | Warhead@MediumFlameWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 555, 642 | PlasmaFlamer | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 556, 631 | PlasmaFlamer | Warhead@RA2Scorch |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 557, 634 | PlasmaFlamer | Warhead@ShieldHit |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 558, 639 | PlasmaFlamer | Warhead@ShieldHitEffect |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 559, 571 | PlasmaFlamer | Warhead@GroundFire |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 561, 577 | PlasmaFlamer | Warhead@MediumChemicalWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 562, 563 | PlasmaFlamer | Warhead@Concrete |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 691, 736, 737 | HeavyPlasmaFlamer | Warhead@Flame_Heavy |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 853, 861 | SamuraiBladeCharged | Warhead@ShieldHit |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 857, 865 | SamuraiBladeCharged | Warhead@SwordWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 858, 859 | SamuraiBladeCharged | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 929, 978 | JapanMaidenBowEnergized | Projectile |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 950, 993 | JapanMaidenBowEnergized | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 966, 1035 | JapanMaidenBowEnergized | Warhead@MediumFlameWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 967, 1117 | JapanMaidenBowEnergized | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 968, 1089 | JapanMaidenBowEnergized | Warhead@RA2Scorch |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 969, 1002 | JapanMaidenBowEnergized | Warhead@GroundFire |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 970, 1008 | JapanMaidenBowEnergized | Warhead@MediumChemicalWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 971, 985 | JapanMaidenBowEnergized | Warhead@Concrete |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 972, 1092 | JapanMaidenBowEnergized | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 973, 987 | JapanMaidenBowEnergized | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 974, 990 | JapanMaidenBowEnergized | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 975, 997 | JapanMaidenBowEnergized | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 976, 1062 | JapanMaidenBowEnergized | Warhead@MediumMissilePercentage |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 977, 1086 | JapanMaidenBowEnergized | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 1242, 1284 | BallistaMultiShot | Projectile |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 1258, 1321 | BallistaMultiShot | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 1269, 1333 | BallistaMultiShot | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 1270, 1433 | BallistaMultiShot | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 1271, 1315 | BallistaMultiShot | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 1272, 1318 | BallistaMultiShot | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 1273, 1328 | BallistaMultiShot | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 1274, 1425 | BallistaMultiShot | Warhead@ShieldHit |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 1275, 1313 | BallistaMultiShot | Warhead@Concrete |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 1276, 1430 | BallistaMultiShot | Warhead@ShieldHitEffect |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 1278, 1393 | BallistaMultiShot | Warhead@LightFlameWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 1279, 1421 | BallistaMultiShot | Warhead@RA2Scorch |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 1280, 1359 | BallistaMultiShot | Warhead@GroundFire |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 1281, 1365 | BallistaMultiShot | Warhead@LightChemicalWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 1282, 1287 | BallistaMultiShot | Warhead@ArrowWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 1283, 1323 | BallistaMultiShot | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 1448, 1497 | BallistaMultiShotEnergized | Projectile |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 1474, 1527 | BallistaMultiShotEnergized | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 1489, 1571 | BallistaMultiShotEnergized | Warhead@MediumFlameWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 1490, 1606 | BallistaMultiShotEnergized | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 1491, 1599 | BallistaMultiShotEnergized | Warhead@RA2Scorch |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 1492, 1537 | BallistaMultiShotEnergized | Warhead@GroundFire |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 1493, 1543 | BallistaMultiShotEnergized | Warhead@MediumChemicalWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 1494, 1525 | BallistaMultiShotEnergized | Warhead@Concrete |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 1495, 1499 | BallistaMultiShotEnergized | Warhead@ArrowWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 1496, 1532 | BallistaMultiShotEnergized | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 1654, 1765 | RocketAngelRockets | Projectile |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 1689, 1807 | RocketAngelRockets | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 1693, 1812 | RocketAngelRockets | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 1700, 1942 | RocketAngelRockets | Warhead@MissileAP_Light |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 1751, 1850 | RocketAngelRockets | Warhead@HeavyMissilePercentage |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 1752, 1938 | RocketAngelRockets | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 1753, 1801 | RocketAngelRockets | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 1754, 1804 | RocketAngelRockets | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 1755, 1928 | RocketAngelRockets | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 1756, 1799 | RocketAngelRockets | Warhead@Concrete |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 1758, 1901 | RocketAngelRockets | Warhead@MediumFlameWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 1759, 1931 | RocketAngelRockets | Warhead@RA2Scorch |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 1760, 1844 | RocketAngelRockets | Warhead@GroundFire |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 1761, 1874 | RocketAngelRockets | Warhead@MediumChemicalWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 1762, 1819 | RocketAngelRockets | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 1763, 1814 | RocketAngelRockets | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 1764, 1773 | RocketAngelRockets | Warhead@ArrowWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 2130, 2210, 2211 | NambuMGWaveforce | Warhead@Waveforce_Heavy |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 2177, 2179 | NambuMGWaveforce | Projectile |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 2178, 2190 | NambuMGWaveforce | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 2186, 2205 | NambuMGWaveforce | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 2194, 2208 | NambuMGWaveforce | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 2197, 2201 | NambuMGWaveforce | Warhead@Glow |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 2198, 2199 | NambuMGWaveforce | Warhead@Concrete |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 2235, 2239, 2240 | ZeroFighterArrows | Warhead@Arrow_Light_Flat |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 2352, 2372 | SkyHawkChainGunWaveforce | Warhead@Railgun_Heavy |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 2354, 2365 | SkyHawkChainGunWaveforce | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 2361, 2379 | SkyHawkChainGunWaveforce | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 2369, 2382 | SkyHawkChainGunWaveforce | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 2374, 2375 | SkyHawkChainGunWaveforce | Warhead@Glow |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 2423, 2506, 2507 | SkyHawkPlasmaCannon | Warhead@Tesla_Heavy |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 2468, 2516 | SkyHawkPlasmaCannon | Warhead@Tesla_Heavy_ExtraDamage |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 2553, 2557, 2558 | SkyHawkArrows | Warhead@Arrow_Light_Flat |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 2637, 2659 | SkyHawkArrowsEnergized | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 2655, 2669 | SkyHawkArrowsEnergized | Warhead@MediumFlameWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 2656, 2699 | SkyHawkArrowsEnergized | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 2657, 2696 | SkyHawkArrowsEnergized | Warhead@RA2Scorch |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 2658, 2663 | SkyHawkArrowsEnergized | Warhead@GroundFire |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 2709, 2738 | 155mm | Projectile |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 2719, 2749 | 155mm | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 2725, 2791 | 155mm | Warhead@HeavyCannonPercentage |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 2726, 2850 | 155mm | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 2727, 2743 | 155mm | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 2728, 2746 | 155mm | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 2729, 2814 | 155mm | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 2730, 2757 | 155mm | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 2731, 2752 | 155mm | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 2732, 2762 | 155mm | Warhead@Glow |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 2733, 2817 | 155mm | Warhead@ShieldHit |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 2734, 2741 | 155mm | Warhead@Concrete |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 2735, 2822 | 155mm | Warhead@ShieldHitEffect |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 2736, 2825 | 155mm | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 2737, 2766 | 155mm | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 2860, 2892 | HovercraftCannon | Projectile |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 2869, 2932 | HovercraftCannon | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 2875, 3035 | HovercraftCannon | Warhead@SmallArmsPercentage |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 2876, 2941 | HovercraftCannon | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 2877, 3002 | HovercraftCannon | Warhead@ShieldHit |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 2878, 3007 | HovercraftCannon | Warhead@ShieldHitEffect |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 2879, 2900 | HovercraftCannon | Warhead@ChaingunPercentage |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 2880, 2924 | HovercraftCannon | Warhead@Concrete |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 2882, 2950 | HovercraftCannon | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 2883, 3059 | HovercraftCannon | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 2884, 2926 | HovercraftCannon | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 2885, 2929 | HovercraftCannon | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 2886, 3010 | HovercraftCannon | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 2887, 3062 | HovercraftCannon | Warhead@TankDestroyerCannonPercentage |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 2888, 2999 | HovercraftCannon | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 2889, 2936 | HovercraftCannon | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 2890, 2946 | HovercraftCannon | Warhead@Glow |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 2891, 2975 | HovercraftCannon | Warhead@MediumCannonPercentage |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 3092, 3193 | HovercraftPlasmaCannon | Projectile |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 3115, 3250 | HovercraftPlasmaCannon | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 3127, 3349 | HovercraftPlasmaCannon | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 3159, 3233 | HovercraftPlasmaCannon | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 3176, 3272 | HovercraftPlasmaCannon | Warhead@HeavyBombPercentage |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 3177, 3398 | HovercraftPlasmaCannon | Warhead@Smudge1 |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 3178, 3344 | HovercraftPlasmaCannon | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 3179, 3401 | HovercraftPlasmaCannon | Warhead@Smudge2 |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 3180, 3406 | HovercraftPlasmaCannon | Warhead@Smudge2RA2 |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 3181, 3241 | HovercraftPlasmaCannon | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 3182, 3225 | HovercraftPlasmaCannon | Warhead@Concrete |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 3183, 3297 | HovercraftPlasmaCannon | Warhead@HeavyCannonPercentage |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 3184, 3395 | HovercraftPlasmaCannon | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 3185, 3227 | HovercraftPlasmaCannon | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 3186, 3230 | HovercraftPlasmaCannon | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 3187, 3236 | HovercraftPlasmaCannon | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 3188, 3246 | HovercraftPlasmaCannon | Warhead@Glow |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 3189, 3371 | HovercraftPlasmaCannon | Warhead@SmallArmsPercentage |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 3190, 3201 | HovercraftPlasmaCannon | Warhead@ChaingunPercentage |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 3191, 3411 | HovercraftPlasmaCannon | Warhead@TankDestroyerCannonPercentage |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 3192, 3320 | HovercraftPlasmaCannon | Warhead@MediumCannonPercentage |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 3452, 3456, 3457 | JapaneseHovercraftFlak | Warhead@Flak_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 3516, 3520, 3521 | JapaneseHovercraftFlakAA | Warhead@Flak_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 3586, 3587 | JapaneseHovercraftFlakWaveforce | Projectile |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 3608, 3614 | JapaneseHovercraftFlakAAkWaveforce | Warhead@Railgun_Heavy |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 3618, 3619 | JapaneseHovercraftFlakAAkWaveforce | Projectile |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 3655, 3704 | Type89PlasmaCannon | Projectile |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 3679, 3716 | Type89PlasmaCannon | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 3694, 3763 | Type89PlasmaCannon | Warhead@TankDestroyerCannonPercentage |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 3695, 3760 | Type89PlasmaCannon | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 3696, 3710 | Type89PlasmaCannon | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 3697, 3713 | Type89PlasmaCannon | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 3698, 3757 | Type89PlasmaCannon | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 3699, 3724 | Type89PlasmaCannon | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 3700, 3719 | Type89PlasmaCannon | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 3701, 3729 | Type89PlasmaCannon | Warhead@Glow |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 3702, 3708 | Type89PlasmaCannon | Warhead@Concrete |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 3703, 3733 | Type89PlasmaCannon | Warhead@HeavyCannonPercentage |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 3817, 3898, 3929 | Type97PlasmaCannon | Warhead@Tesla_Heavy |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 3861, 3939 | Type97PlasmaCannon | Warhead@Tesla_Heavy_ExtraDamage |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 3899, 3923 | Type97PlasmaCannon | Warhead@Railgun_Heavy_ExtraDamage |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 4004, 4064, 4065 | WaveforceCannon | Warhead@MissileHE_Heavy |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 4095, 4146 | OISmallPlasmaCannon | Projectile |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 4119, 4158 | OISmallPlasmaCannon | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 4131, 4232 | OISmallPlasmaCannon | Warhead@ShieldHit |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 4137, 4201 | OISmallPlasmaCannon | Warhead@HeavyChemicalWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 4138, 4177 | OISmallPlasmaCannon | Warhead@HeavyCannonPercentage |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 4139, 4234 | OISmallPlasmaCannon | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 4140, 4152 | OISmallPlasmaCannon | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 4141, 4155 | OISmallPlasmaCannon | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 4142, 4229 | OISmallPlasmaCannon | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 4143, 4168 | OISmallPlasmaCannon | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 4144, 4163 | OISmallPlasmaCannon | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 4145, 4173 | OISmallPlasmaCannon | Warhead@Glow |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 4266, 4347, 4378 | OIBigPlasmaCannon | Warhead@Tesla_Heavy |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 4310, 4388 | OIBigPlasmaCannon | Warhead@Tesla_Heavy_ExtraDamage |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 4348, 4372 | OIBigPlasmaCannon | Warhead@Railgun_Heavy_ExtraDamage |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 4431, 4476, 4477 | OIPlasmaFlamer | Warhead@Flame_Heavy |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 4545, 4566 | BuggyPlasmaGrenade | Warhead@HeavyBombPercentage |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 4546, 4619 | BuggyPlasmaGrenade | Warhead@Smudge1 |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 4547, 4591 | BuggyPlasmaGrenade | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 4548, 4622 | BuggyPlasmaGrenade | Warhead@Smudge2 |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 4549, 4627 | BuggyPlasmaGrenade | Warhead@Smudge2RA2 |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 4550, 4553 | BuggyPlasmaGrenade | Warhead@Effect1 |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 4551, 4558 | BuggyPlasmaGrenade | Warhead@Effect2 |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 4552, 4594 | BuggyPlasmaGrenade | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 4685, 4698 | JapanesePlasmaBomb | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 4695, 4712 | JapanesePlasmaBomb | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 4696, 4709 | JapanesePlasmaBomb | Warhead@RA2Scorch |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 4697, 4703 | JapanesePlasmaBomb | Warhead@GroundFire |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 4729, 4733, 4734 | JHighV | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 4797, 4874, 4875 | JHighVWaveforce | Warhead@Waveforce_Heavy |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 4844, 4846 | JHighVWaveforce | Projectile |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 4845, 4857 | JHighVWaveforce | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 4853, 4869 | JHighVWaveforce | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 4861, 4872 | JHighVWaveforce | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 4864, 4865 | JHighVWaveforce | Warhead@Glow |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 4919, 4961 | NanoArtilleryAG | Projectile |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 4928, 4971 | NanoArtilleryAG | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 4948, 5012 | NanoArtilleryAG | Warhead@HeavyCannonPercentage |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 4949, 5071 | NanoArtilleryAG | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 4950, 4965 | NanoArtilleryAG | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 4951, 4968 | NanoArtilleryAG | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 4952, 5035 | NanoArtilleryAG | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 4953, 4978 | NanoArtilleryAG | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 4954, 4973 | NanoArtilleryAG | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 4955, 4983 | NanoArtilleryAG | Warhead@Glow |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 4956, 5038 | NanoArtilleryAG | Warhead@ShieldHit |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 4957, 4963 | NanoArtilleryAG | Warhead@Concrete |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 4958, 5043 | NanoArtilleryAG | Warhead@ShieldHitEffect |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 4959, 5046 | NanoArtilleryAG | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 4960, 4987 | NanoArtilleryAG | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 5092, 5124 | NanoSmokeAG | Projectile |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 5100, 5138 | NanoSmokeAG | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 5114, 5177 | NanoSmokeAG | Warhead@MagicWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 5115, 5243 | NanoSmokeAG | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 5116, 5235 | NanoSmokeAG | Warhead@ShieldHit |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 5117, 5240 | NanoSmokeAG | Warhead@ShieldHitEffect |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 5119, 5204 | NanoSmokeAG | Warhead@MediumChemicalWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 5120, 5136 | NanoSmokeAG | Warhead@Concrete |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 5121, 5150 | NanoSmokeAG | Warhead@LightFlameWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 5122, 5231 | NanoSmokeAG | Warhead@RA2Scorch |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 5123, 5144 | NanoSmokeAG | Warhead@GroundFire |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 5365, 5391 | ArmoredCarMG | Projectile |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 5366, 5476 | ArmoredCarMG | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 5373, 5416 | ArmoredCarMG | Warhead@ArrowWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 5374, 5480 | ArmoredCarMG | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 5375, 5598 | ArmoredCarMG | Warhead@ShieldHit |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 5376, 5603 | ArmoredCarMG | Warhead@ShieldHitEffect |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 5377, 5634 | ArmoredCarMG | Warhead@TankDestroyerCannonPercentage |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 5378, 5631 | ArmoredCarMG | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 5379, 5470 | ArmoredCarMG | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 5380, 5473 | ArmoredCarMG | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 5381, 5595 | ArmoredCarMG | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 5382, 5485 | ArmoredCarMG | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 5383, 5515 | ArmoredCarMG | Warhead@Glow |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 5384, 5468 | ArmoredCarMG | Warhead@Concrete |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 5385, 5606 | ArmoredCarMG | Warhead@SmallArmsPercentage |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 5386, 5519 | ArmoredCarMG | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 5387, 5545 | ArmoredCarMG | Warhead@MediumCannonPercentage |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 5388, 5570 | ArmoredCarMG | Warhead@MediumMissilePercentage |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 5389, 5490 | ArmoredCarMG | Warhead@FlakWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 5390, 5443 | ArmoredCarMG | Warhead@ChaingunPercentage |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 5698, 5971 | ArmoredCarMGWaveforce | Warhead@Railgun_Heavy |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 5702, 5732 | ArmoredCarMGWaveforce | Warhead@Railgun_Heavy_ExtraDamage |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 5734, 5973 | ArmoredCarMGWaveforce | Warhead@ShieldHit |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 5736, 5753, 5793 | ArmoredCarMGWaveforce | Warhead@ArrowWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 5738, 5755, 6005 | ArmoredCarMGWaveforce | Warhead@TankDestroyerCannonPercentage |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 5740, 5761, 5977 | ArmoredCarMGWaveforce | Warhead@SmallArmsPercentage |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 5742, 5762, 5892 | ArmoredCarMGWaveforce | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 5744, 5763, 5918 | ArmoredCarMGWaveforce | Warhead@MediumCannonPercentage |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 5746, 5764, 5943 | ArmoredCarMGWaveforce | Warhead@MediumMissilePercentage |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 5748, 5765, 5867 | ArmoredCarMGWaveforce | Warhead@FlakWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 5750, 5766, 5819 | ArmoredCarMGWaveforce | Warhead@ChaingunPercentage |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 5754, 5857 | ArmoredCarMGWaveforce | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 5756, 6002 | ArmoredCarMGWaveforce | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 5757, 5846 | ArmoredCarMGWaveforce | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 5758, 5849 | ArmoredCarMGWaveforce | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 5759, 5968 | ArmoredCarMGWaveforce | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 5760, 5862 | ArmoredCarMGWaveforce | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 6039, 6067 | ArmoredCarMGAAWaveforce | Warhead@Railgun_Heavy_ExtraDamage |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 6069, 6079, 6318 | ArmoredCarMGAAWaveforce | Warhead@TankDestroyerCannonPercentage |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 6071, 6085, 6291 | ArmoredCarMGAAWaveforce | Warhead@SmallArmsPercentage |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 6073, 6086, 6236 | ArmoredCarMGAAWaveforce | Warhead@MediumCannonPercentage |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 6075, 6087, 6140 | ArmoredCarMGAAWaveforce | Warhead@ChaingunPercentage |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 6078, 6177 | ArmoredCarMGAAWaveforce | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 6080, 6315 | ArmoredCarMGAAWaveforce | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 6081, 6166 | ArmoredCarMGAAWaveforce | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 6082, 6169 | ArmoredCarMGAAWaveforce | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 6083, 6284 | ArmoredCarMGAAWaveforce | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 6084, 6182 | ArmoredCarMGAAWaveforce | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 202, 208, 209 | RocketsRA | Warhead@MissileAP_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 323, 335 | DragunovSniper | Projectile |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 324, 467 | DragunovSniper | Warhead@TankDestroyerCannonPercentage |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 325, 464 | DragunovSniper | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 326, 361 | DragunovSniper | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 327, 364 | DragunovSniper | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 328, 457 | DragunovSniper | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 329, 374 | DragunovSniper | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 330, 369 | DragunovSniper | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 331, 404 | DragunovSniper | Warhead@Glow |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 332, 432 | DragunovSniper | Warhead@LightMissilePercentage |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 333, 408 | DragunovSniper | Warhead@HeavyCannonPercentage |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 334, 379 | DragunovSniper | Warhead@FlakWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 367, 492 | DragunovSniper | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 761, 846, 847 | MagicOrb | Warhead@Tesla_Heavy |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 807, 855 | MagicOrb | Warhead@Tesla_Heavy_ExtraDamage |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 884, 967, 968 | MagicOrb2 | Warhead@Tesla_Heavy |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 929, 977 | MagicOrb2 | Warhead@Tesla_Heavy_ExtraDamage |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 996, 1000, 1001 | ra1_allies_alliedapc_gun | Warhead@Flak_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 1060, 1064, 1065 | ra1_allies_alliedapc_gun_AA | Warhead@Flak_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 1128, 1136, 1137 | ra1_allies_machinegunner_machinegun | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 1291, 1298 | ZeroFighterChainGunWaveforce | Projectile |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 1292, 1324 | ZeroFighterChainGunWaveforce | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 1293, 1349 | ZeroFighterChainGunWaveforce | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 1294, 1306 | ZeroFighterChainGunWaveforce | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 1295, 1309 | ZeroFighterChainGunWaveforce | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 1296, 1317, 1384 | ZeroFighterChainGunWaveforce | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 1297, 1352 | ZeroFighterChainGunWaveforce | Warhead@ZeroFighterBullet_MediumFriendlyFire |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 1449, 1468 | 25mmWaveforce | Warhead@Railgun_Heavy |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 1493, 1554, 1555 | WaveforceCannonChargedLaser | Warhead@MissileHE_Heavy |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 1570, 1619 | WaveforceCannonDistortedBeam1 | Projectile |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 1601, 1631 | WaveforceCannonDistortedBeam1 | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 1612, 1637 | WaveforceCannonDistortedBeam1 | Warhead@HeavyCannonPercentage |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 1613, 1663 | WaveforceCannonDistortedBeam1 | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 1614, 1625 | WaveforceCannonDistortedBeam1 | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 1615, 1628 | WaveforceCannonDistortedBeam1 | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 1616, 1660 | WaveforceCannonDistortedBeam1 | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 1617, 1633 | WaveforceCannonDistortedBeam1 | Warhead@Glow |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 1618, 1623 | WaveforceCannonDistortedBeam1 | Warhead@Concrete |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 1685, 1748, 1749 | WaveforceCannonDistortedBeam2 | Warhead@Chemical_Heavy |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 1767, 1791 | ra120mm | Projectile |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 1769, 1803 | ra120mm | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 1777, 1819 | ra120mm | Warhead@HeavyCannonPercentage |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 1778, 1902 | ra120mm | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 1779, 1797 | ra120mm | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 1780, 1800 | ra120mm | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 1781, 1866 | ra120mm | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 1782, 1810 | ra120mm | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 1783, 1805 | ra120mm | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 1784, 1815 | ra120mm | Warhead@Glow |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 1785, 1869 | ra120mm | Warhead@ShieldHit |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 1786, 1795 | ra120mm | Warhead@Concrete |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 1787, 1874 | ra120mm | Warhead@ShieldHitEffect |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 1788, 1842 | ra120mm | Warhead@MediumCannonPercentage |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 1789, 1877 | ra120mm | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 1790, 1905 | ra120mm | Warhead@TankDestroyerCannonPercentage |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 1987, 2007, 2008 | ParaBomb | Warhead@Demolition_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 1994, 1997 | ParaBomb | Projectile |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 2065, 2066 | ra1_allies_cargoplanebomber_parabombcryo | Projectile |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 2081, 2116 | JapanSuperBomb | Projectile |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 2092, 2100, 2275 | JapanSuperBomb | Warhead@ShieldHit |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 2094, 2155 | JapanSuperBomb | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 2095, 2309 | JapanSuperBomb | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 2096, 2123 | JapanSuperBomb | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 2097, 2126 | JapanSuperBomb | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 2098, 2129, 2327 | JapanSuperBomb | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 2099, 2150, 2329 | JapanSuperBomb | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 2101, 2121 | JapanSuperBomb | Warhead@Concrete |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 2102, 2280 | JapanSuperBomb | Warhead@ShieldHitEffect |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 2103, 2283 | JapanSuperBomb | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 2105, 2213 | JapanSuperBomb | Warhead@MediumChemicalWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 2106, 2241 | JapanSuperBomb | Warhead@MediumFlameWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 2107, 2272 | JapanSuperBomb | Warhead@RA2Scorch |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 2108, 2181 | JapanSuperBomb | Warhead@GroundFire |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 2109, 2187 | JapanSuperBomb | Warhead@HeavyBombPercentage |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 2110, 2312 | JapanSuperBomb | Warhead@Smudge1 |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 2111, 2269 | JapanSuperBomb | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 2112, 2315 | JapanSuperBomb | Warhead@Smudge2 |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 2113, 2320 | JapanSuperBomb | Warhead@Smudge2RA2 |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 2114, 2137 | JapanSuperBomb | Warhead@Effect1 |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 2115, 2142 | JapanSuperBomb | Warhead@Effect2 |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 2366, 2418 | RAAtomic | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 2368, 2424 | RAAtomic | Warhead@Effect2 |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 2372, 2430 | RAAtomic | Warhead@GroundFireArea |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 2373, 2414 | RAAtomic | Warhead@Distort |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 2374, 2443 | RAAtomic | Warhead@Shock |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 2375, 2396 | RAAtomic | Warhead@4Res_areanukea |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 2376, 2400 | RAAtomic | Warhead@5Smu_areanukea |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 2377, 2405 | RAAtomic | Warhead@8Res_areanukeb |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 2378, 2409 | RAAtomic | Warhead@9Smu_areanukeb |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 2379, 2383 | RAAtomic | Warhead@11Res_areanukec |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 2380, 2387 | RAAtomic | Warhead@12Smu_areanukec |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 2381, 2392 | RAAtomic | Warhead@13Shake |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 2382, 2427 | RAAtomic | Warhead@FlashEffect |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 2447, 2525, 2527 | RAAtomic | Warhead@Tesla_Super |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 2495, 2526, 2534 | RAAtomic | Warhead@Tesla_Super_ExtraDamage |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 2828, 2870 | V2ExplodeIrak | Warhead@MissileHE_Heavy |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 2896, 2957 | BarrelExplode | Warhead@Demolition_Light |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 2938, 2949 | BarrelExplode | Projectile |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 2939, 2984 | BarrelExplode | Warhead@Glow |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 2940, 2999 | BarrelExplode | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 2941, 2968 | BarrelExplode | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 2942, 2971 | BarrelExplode | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 2943, 2974 | BarrelExplode | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 2944, 2979 | BarrelExplode | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 2945, 2991 | BarrelExplode | Warhead@ShieldHit |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 2946, 2955 | BarrelExplode | Warhead@Concrete |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 2947, 2996 | BarrelExplode | Warhead@ShieldHitEffect |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 2948, 2988 | BarrelExplode | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 3040, 3066, 3088 | ATMine | Warhead@ATMineDemolition_Light |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 3056, 3067 | ATMine | Projectile |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 3057, 3136 | ATMine | Warhead@Glow |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 3058, 3120 | ATMine | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 3059, 3123 | ATMine | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 3060, 3131 | ATMine | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 3061, 3168 | ATMine | Warhead@ShieldHit |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 3062, 3173 | ATMine | Warhead@ShieldHitEffect |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 3063, 3165 | ATMine | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 3064, 3140 | ATMine | Warhead@HeavyMissilePercentage |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 3065, 3126 | ATMine | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 3482, 3524 | SCUDIrak | Warhead@MissileHE_Heavy |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 3536, 3560 | PsionicShells | Warhead@1Dam |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 3539, 3573 | PsionicShells | Warhead@3Eff |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 3550, 3556 | PsionicShells | Projectile |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 3551, 3570 | PsionicShells | Warhead@2Smu |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 3552, 3580 | PsionicShells | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 3553, 3583 | PsionicShells | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 3554, 3586 | PsionicShells | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 3555, 3575 | PsionicShells | Warhead@4EffWater |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 3627, 3684, 3685 | ra1_soviets_submarine_torpedo | Warhead@Concussion_Light |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 3745, 3752 | ra1_soviets_submarine_torpedo_thermobaric | Warhead@Smudge1 |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 3746, 3757 | ra1_soviets_submarine_torpedo_thermobaric | Warhead@Smudge2 |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 3747, 3762 | ra1_soviets_submarine_torpedo_thermobaric | Warhead@Smudge3 |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 3748, 3749 | ra1_soviets_submarine_torpedo_thermobaric | Warhead@ShieldHitEffectNuclear |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 3859, 3863, 3864 | 8Inch | Warhead@Demolition_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 3992, 3996, 3997 | JapanSpeedBoatGun | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 4057, 4134, 4135 | JapanSpeedBoatGunWaveforce | Warhead@Waveforce_Heavy |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 4104, 4106 | JapanSpeedBoatGunWaveforce | Projectile |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 4105, 4117 | JapanSpeedBoatGunWaveforce | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 4113, 4129 | JapanSpeedBoatGunWaveforce | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 4121, 4132 | JapanSpeedBoatGunWaveforce | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 4124, 4125 | JapanSpeedBoatGunWaveforce | Warhead@Glow |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 4172, 4176, 4177 | YamatoCannon | Warhead@CannonHE_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 4257, 4351 | ra1_allies_chronovortex | Projectile |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 4340, 4383 | ra1_allies_chronovortex | Warhead@HeavyBombPercentage |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 4341, 4445 | ra1_allies_chronovortex | Warhead@Smudge1 |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 4342, 4434 | ra1_allies_chronovortex | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 4343, 4448 | ra1_allies_chronovortex | Warhead@Smudge2 |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 4344, 4453 | ra1_allies_chronovortex | Warhead@Smudge2RA2 |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 4345, 4371 | ra1_allies_chronovortex | Warhead@Effect2 |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 4346, 4379 | ra1_allies_chronovortex | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 4347, 4437 | ra1_allies_chronovortex | Warhead@ShieldHit |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 4348, 4442 | ra1_allies_chronovortex | Warhead@ShieldHitEffect |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 4349, 4369 | ra1_allies_chronovortex | Warhead@Concrete |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 4350, 4408 | ra1_allies_chronovortex | Warhead@MagicWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/buildings.yaml | 597, 600 | ra1_soviets_largesovietairfield | RenderSprites |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 13, 27, 28 | IncendiaryM1Carbine | Warhead@Flame_Light_Flat |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 19, 20 | IncendiaryM1Carbine | Projectile |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 94, 104, 105 | ra1_soviets_ak47conscript_rifle | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 215, 230, 231 | ra1_soviets_grenadier_grenaderaexplode | Warhead@Flame_Light_Flat |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 287, 350 | ra1_soviets_grenadier_grenadethermobaric | Projectile |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 294, 456, 457 | ra1_soviets_grenadier_grenadethermobaric | Warhead@Thermobaric_Light |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 343, 400 | ra1_soviets_grenadier_grenadethermobaric | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 344, 357 | ra1_soviets_grenadier_grenadethermobaric | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 345, 360 | ra1_soviets_grenadier_grenadethermobaric | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 346, 368, 425 | ra1_soviets_grenadier_grenadethermobaric | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 347, 355 | ra1_soviets_grenadier_grenadethermobaric | Warhead@Concrete |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 349, 373 | ra1_soviets_grenadier_grenadethermobaric | Warhead@MediumFlameWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 363, 426, 429 | ra1_soviets_grenadier_grenadethermobaric | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 427, 439 | ra1_soviets_grenadier_grenadethermobaric | Warhead@RA2Scorch |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 428, 433 | ra1_soviets_grenadier_grenadethermobaric | Warhead@GroundFire |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 503, 582, 583 | ra1_soviets_grenadier_grenadethermobaricexplode | Warhead@Thermobaric_Light |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 554, 557 | ra1_soviets_grenadier_grenadethermobaricexplode | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 555, 567 | ra1_soviets_grenadier_grenadethermobaricexplode | Warhead@RA2Scorch |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 556, 561 | ra1_soviets_grenadier_grenadethermobaricexplode | Warhead@GroundFire |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 1123, 1128, 1129 | ra1_soviets_v1rockettruck_v1rockets | Warhead@MissileHE_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 1189, 1228 | ra1_soviets_v1rockettruck_v1rocketsthermobaric | Projectile |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 1209, 1218, 1358 | ra1_soviets_v1rockettruck_v1rocketsthermobaric | Warhead@ShieldHit |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 1211, 1327 | ra1_soviets_v1rockettruck_v1rocketsthermobaric | Warhead@MediumMissilePercentage |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 1212, 1366 | ra1_soviets_v1rockettruck_v1rocketsthermobaric | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 1213, 1242 | ra1_soviets_v1rockettruck_v1rocketsthermobaric | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 1214, 1245 | ra1_soviets_v1rockettruck_v1rocketsthermobaric | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 1215, 1352 | ra1_soviets_v1rockettruck_v1rocketsthermobaric | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 1216, 1248 | ra1_soviets_v1rockettruck_v1rocketsthermobaric | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 1217, 1256 | ra1_soviets_v1rockettruck_v1rocketsthermobaric | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 1219, 1240 | ra1_soviets_v1rockettruck_v1rocketsthermobaric | Warhead@Concrete |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 1220, 1363 | ra1_soviets_v1rockettruck_v1rocketsthermobaric | Warhead@ShieldHitEffect |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 1222, 1266 | ra1_soviets_v1rockettruck_v1rocketsthermobaric | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 1223, 1261 | ra1_soviets_v1rockettruck_v1rocketsthermobaric | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 1225, 1298 | ra1_soviets_v1rockettruck_v1rocketsthermobaric | Warhead@HeavyFlameWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 1226, 1355 | ra1_soviets_v1rockettruck_v1rocketsthermobaric | Warhead@RA2Scorch |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 1227, 1292 | ra1_soviets_v1rockettruck_v1rocketsthermobaric | Warhead@GroundFire |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 1390, 1395, 1396 | ra1_soviets_grad_rocket | Warhead@MissileHE_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 1456, 1567, 1568 | ra1_soviets_grad_rocket_heavy | Warhead@MissileHE_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 1461, 1511 | ra1_soviets_grad_rocket_heavy | Warhead@MediumChemicalWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 1462, 1486 | ra1_soviets_grad_rocket_heavy | Warhead@HeavyBombPercentage |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 1463, 1541 | ra1_soviets_grad_rocket_heavy | Warhead@Smudge1 |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 1464, 1538 | ra1_soviets_grad_rocket_heavy | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 1465, 1544 | ra1_soviets_grad_rocket_heavy | Warhead@Smudge2 |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 1466, 1549 | ra1_soviets_grad_rocket_heavy | Warhead@Smudge2RA2 |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 1467, 1473 | ra1_soviets_grad_rocket_heavy | Warhead@Effect1 |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 1468, 1478 | ra1_soviets_grad_rocket_heavy | Warhead@Effect2 |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 1469, 1554, 1558 | ra1_soviets_grad_rocket_heavy | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 1634, 1678, 1679 | SCUD | Warhead@MissileHE_Heavy |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 1753, 1766 | ra1_soviets_v2rocketlauncher_scudtesla | Warhead@EMPUnit |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 1853, 1859, 1860 | ra1_soviets_flaktruck_flak_cannon | Warhead@Flak_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 1917, 1923, 1924 | ra1_soviets_flaktruck_flak_cannon_AA | Warhead@Flak_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 1988, 1995, 1996 | ra1_soviets_btr80_machinegun | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 2053, 2060, 2061 | ra1_soviets_btr80_machinegun_AA | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 2137, 2144, 2145 | ra1_soviets_btr80_machinegun_tesla | Warhead@Tesla_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 2204, 2215, 2216 | ra1_soviets_btr80_machinegun_tesla_arc | Warhead@Tesla_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 2269, 2281, 2282 | ra1_soviets_btr80_btrteslamachinegunarcfragment1 | Warhead@Tesla_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 2336, 2348, 2349 | ra1_soviets_btr80_machinegun_tesla_AA | Warhead@Tesla_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 2409, 2418, 2419 | ra1_soviets_btr80_machinegun_tesla_arc_AA | Warhead@Tesla_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 2472, 2482, 2483 | ra1_soviets_btr80_btrteslamachinegunarcfragment1aa | Warhead@Tesla_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 2557, 2571, 2572 | IncendiaryChainGun | Warhead@Flame_Light_Flat |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 2645, 2655, 2656 | HindMissiles | Warhead@MissileAP_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 2714, 2794 | ra1_soviets_hindattackhelicopter_hindmissilesthermobaric | Projectile |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 2732, 2808 | ra1_soviets_hindattackhelicopter_hindmissilesthermobaric | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 2740, 2931, 2932 | ra1_soviets_hindattackhelicopter_hindmissilesthermobaric | Warhead@Thermobaric_Medium |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 2787, 2815 | ra1_soviets_hindattackhelicopter_hindmissilesthermobaric | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 2788, 2905 | ra1_soviets_hindattackhelicopter_hindmissilesthermobaric | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 2790, 2847 | ra1_soviets_hindattackhelicopter_hindmissilesthermobaric | Warhead@MediumFlameWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 2791, 2900 | ra1_soviets_hindattackhelicopter_hindmissilesthermobaric | Warhead@RA2Scorch |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 2792, 2841 | ra1_soviets_hindattackhelicopter_hindmissilesthermobaric | Warhead@GroundFire |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 2793, 2875 | ra1_soviets_hindattackhelicopter_hindmissilesthermobaric | Warhead@MediumMissilePercentage |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 2948, 2994 | ra1_soviets_hindattackhelicopter_hindmissilesnuclear | Projectile |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 2963, 3013 | ra1_soviets_hindattackhelicopter_hindmissilesnuclear | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 2973, 3152 | ra1_soviets_hindattackhelicopter_hindmissilesnuclear | Warhead@TankDestroyerCannonPercentage |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 2974, 3134 | ra1_soviets_hindattackhelicopter_hindmissilesnuclear | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 2975, 3007 | ra1_soviets_hindattackhelicopter_hindmissilesnuclear | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 2976, 3010 | ra1_soviets_hindattackhelicopter_hindmissilesnuclear | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 2977, 3117 | ra1_soviets_hindattackhelicopter_hindmissilesnuclear | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 2978, 3022 | ra1_soviets_hindattackhelicopter_hindmissilesnuclear | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 2979, 3017 | ra1_soviets_hindattackhelicopter_hindmissilesnuclear | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 2980, 3027 | ra1_soviets_hindattackhelicopter_hindmissilesnuclear | Warhead@Glow |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 2981, 3123 | ra1_soviets_hindattackhelicopter_hindmissilesnuclear | Warhead@ShieldHit |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 2982, 3005 | ra1_soviets_hindattackhelicopter_hindmissilesnuclear | Warhead@Concrete |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 2983, 3128 | ra1_soviets_hindattackhelicopter_hindmissilesnuclear | Warhead@ShieldHitEffect |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 2985, 3037 | ra1_soviets_hindattackhelicopter_hindmissilesnuclear | Warhead@MediumFlameWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 2986, 3120 | ra1_soviets_hindattackhelicopter_hindmissilesnuclear | Warhead@RA2Scorch |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 2987, 3031 | ra1_soviets_hindattackhelicopter_hindmissilesnuclear | Warhead@GroundFire |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 2988, 3090 | ra1_soviets_hindattackhelicopter_hindmissilesnuclear | Warhead@NuclearWarheadPercentage |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 2989, 3137 | ra1_soviets_hindattackhelicopter_hindmissilesnuclear | Warhead@Smudge1 |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 2990, 3142 | ra1_soviets_hindattackhelicopter_hindmissilesnuclear | Warhead@Smudge2 |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 2991, 3147 | ra1_soviets_hindattackhelicopter_hindmissilesnuclear | Warhead@Smudge3 |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 2992, 3131 | ra1_soviets_hindattackhelicopter_hindmissilesnuclear | Warhead@ShieldHitEffectNuclear |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 2993, 3065 | ra1_soviets_hindattackhelicopter_hindmissilesnuclear | Warhead@MediumMissilePercentage |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 3187, 3243 | ra1_soviets_kamovattackhelicopter_kamovmissilestesla | Projectile |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 3231, 3272 | ra1_soviets_kamovattackhelicopter_kamovmissilestesla | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 3232, 3389 | ra1_soviets_kamovattackhelicopter_kamovmissilestesla | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 3233, 3257 | ra1_soviets_kamovattackhelicopter_kamovmissilestesla | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 3234, 3260 | ra1_soviets_kamovattackhelicopter_kamovmissilestesla | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 3235, 3255 | ra1_soviets_kamovattackhelicopter_kamovmissilestesla | Warhead@Concrete |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 3236, 3363 | ra1_soviets_kamovattackhelicopter_kamovmissilestesla | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 3238, 3304 | ra1_soviets_kamovattackhelicopter_kamovmissilestesla | Warhead@MediumFlameWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 3239, 3360 | ra1_soviets_kamovattackhelicopter_kamovmissilestesla | Warhead@RA2Scorch |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 3240, 3298 | ra1_soviets_kamovattackhelicopter_kamovmissilestesla | Warhead@GroundFire |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 3241, 3332 | ra1_soviets_kamovattackhelicopter_kamovmissilestesla | Warhead@MediumMissilePercentage |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 3242, 3357 | ra1_soviets_kamovattackhelicopter_kamovmissilestesla | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 3263, 3392 | ra1_soviets_kamovattackhelicopter_kamovmissilestesla | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 3269, 3395 | ra1_soviets_kamovattackhelicopter_kamovmissilestesla | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 3432, 3439, 3440 | ra1_soviets_kamovattackhelicopter_kamovtesla | Warhead@Tesla_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 3501, 3508, 3509 | ra1_soviets_kamovattackhelicopter_kamovteslaarc | Warhead@Tesla_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 3587, 3594, 3595 | ra1_soviets_kamovattackhelicopter_kamovteslaarcfragment1 | Warhead@Tesla_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 3664, 3672, 3673 | ra1_soviets_kamovattackhelicopter_kamovteslaarcfragment2 | Warhead@Tesla_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 3765, 3770, 3771 | ra1_soviets_yakscoutplane_chaingun_incendiary | Warhead@Flame_Light_Flat |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 3852, 3857, 3858 | ra1_soviets_teslayak_chaingun_incendiary | Warhead@Flame_Light_Flat |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 3939, 3944, 3945 | ra1_soviets_nuclearyak_chaingun_incendiary | Warhead@Flame_Light_Flat |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 4026, 4031, 4032 | ra1_soviets_su57attackbomber_chaingun_incendiary | Warhead@Flame_Light_Flat |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 4111, 4115, 4116 | ra1_soviets_armoredyak_chaingun_incendiary | Warhead@Flame_Light_Flat |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 4196, 4203, 4204 | ra1_soviets_teslayak_yakteslagun | Warhead@Tesla_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 4264, 4274, 4275 | ra1_soviets_teslayak_yakteslagunarc | Warhead@Tesla_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 4354, 4361, 4362 | ra1_soviets_teslayak_yakteslaarcfragment1 | Warhead@Tesla_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 4434, 4442, 4443 | ra1_soviets_teslayak_yakteslaarcfragment2 | Warhead@Tesla_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 4502, 4534 | ra1_soviets_yakscoutplane_napalm_bomb | Projectile |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 4507, 4544 | ra1_soviets_yakscoutplane_napalm_bomb | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 4515, 4664 | ra1_soviets_yakscoutplane_napalm_bomb | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 4516, 4690 | ra1_soviets_yakscoutplane_napalm_bomb | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 4517, 4538 | ra1_soviets_yakscoutplane_napalm_bomb | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 4518, 4541 | ra1_soviets_yakscoutplane_napalm_bomb | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 4519, 4556 | ra1_soviets_yakscoutplane_napalm_bomb | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 4520, 4656 | ra1_soviets_yakscoutplane_napalm_bomb | Warhead@ShieldHit |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 4521, 4536 | ra1_soviets_yakscoutplane_napalm_bomb | Warhead@Concrete |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 4522, 4661 | ra1_soviets_yakscoutplane_napalm_bomb | Warhead@ShieldHitEffect |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 4523, 4567 | ra1_soviets_yakscoutplane_napalm_bomb | Warhead@HeavyBombPercentage |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 4524, 4693 | ra1_soviets_yakscoutplane_napalm_bomb | Warhead@Smudge1 |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 4525, 4650 | ra1_soviets_yakscoutplane_napalm_bomb | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 4526, 4696 | ra1_soviets_yakscoutplane_napalm_bomb | Warhead@Smudge2 |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 4527, 4701 | ra1_soviets_yakscoutplane_napalm_bomb | Warhead@Smudge2RA2 |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 4528, 4548 | ra1_soviets_yakscoutplane_napalm_bomb | Warhead@Effect2 |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 4530, 4622 | ra1_soviets_yakscoutplane_napalm_bomb | Warhead@MediumFlameWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 4531, 4653 | ra1_soviets_yakscoutplane_napalm_bomb | Warhead@RA2Scorch |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 4532, 4561 | ra1_soviets_yakscoutplane_napalm_bomb | Warhead@GroundFire |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 4533, 4593 | ra1_soviets_yakscoutplane_napalm_bomb | Warhead@HeavyFlameWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 4716, 4807 | ra1_soviets_teslayak_tesla_bomb | Projectile |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 4798, 4943 | ra1_soviets_teslayak_tesla_bomb | Warhead@ShieldHit |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 4800, 4820 | ra1_soviets_teslayak_tesla_bomb | Warhead@HeavyBombPercentage |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 4802, 4847 | ra1_soviets_teslayak_tesla_bomb | Warhead@HeavyChemicalWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 4803, 4876 | ra1_soviets_teslayak_tesla_bomb | Warhead@HeavyFlameWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 4804, 4909 | ra1_soviets_teslayak_tesla_bomb | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 4805, 4906 | ra1_soviets_teslayak_tesla_bomb | Warhead@RA2Scorch |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 4806, 4814 | ra1_soviets_teslayak_tesla_bomb | Warhead@GroundFire |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 4809, 4921 | ra1_soviets_teslayak_tesla_bomb | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 4912, 4947 | ra1_soviets_teslayak_tesla_bomb | Warhead@Smudge1 |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 4913, 4940 | ra1_soviets_teslayak_tesla_bomb | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 4914, 4950 | ra1_soviets_teslayak_tesla_bomb | Warhead@Smudge2 |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 4915, 4955 | ra1_soviets_teslayak_tesla_bomb | Warhead@Smudge2RA2 |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 4916, 4926 | ra1_soviets_teslayak_tesla_bomb | Warhead@Effect2 |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 4917, 4934 | ra1_soviets_teslayak_tesla_bomb | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 4918, 4919 | ra1_soviets_teslayak_tesla_bomb | Warhead@Concrete |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 4971, 5004 | ra1_soviets_nuclearyak_yaknuclearbomb | Projectile |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 4976, 5008 | ra1_soviets_nuclearyak_yaknuclearbomb | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 4984, 5035 | ra1_soviets_nuclearyak_yaknuclearbomb | Warhead@HeavyBombPercentage |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 4985, 5165 | ra1_soviets_nuclearyak_yaknuclearbomb | Warhead@Smudge1 |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 4986, 5145 | ra1_soviets_nuclearyak_yaknuclearbomb | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 4987, 5170 | ra1_soviets_nuclearyak_yaknuclearbomb | Warhead@Smudge2 |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 4988, 5175 | ra1_soviets_nuclearyak_yaknuclearbomb | Warhead@Smudge2RA2 |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 4989, 5012 | ra1_soviets_nuclearyak_yaknuclearbomb | Warhead@Effect1 |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 4990, 5017 | ra1_soviets_nuclearyak_yaknuclearbomb | Warhead@Effect2 |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 4991, 5025 | ra1_soviets_nuclearyak_yaknuclearbomb | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 4992, 5151 | ra1_soviets_nuclearyak_yaknuclearbomb | Warhead@ShieldHit |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 4993, 5156 | ra1_soviets_nuclearyak_yaknuclearbomb | Warhead@ShieldHitEffect |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 4994, 5006 | ra1_soviets_nuclearyak_yaknuclearbomb | Warhead@Concrete |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 4996, 5061 | ra1_soviets_nuclearyak_yaknuclearbomb | Warhead@HeavyChemicalWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 4997, 5089 | ra1_soviets_nuclearyak_yaknuclearbomb | Warhead@HeavyFlameWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 4998, 5162 | ra1_soviets_nuclearyak_yaknuclearbomb | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 4999, 5148 | ra1_soviets_nuclearyak_yaknuclearbomb | Warhead@RA2Scorch |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 5000, 5029 | ra1_soviets_nuclearyak_yaknuclearbomb | Warhead@GroundFire |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 5001, 5118 | ra1_soviets_nuclearyak_yaknuclearbomb | Warhead@NuclearWarheadPercentage |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 5002, 5180 | ra1_soviets_nuclearyak_yaknuclearbomb | Warhead@Smudge3 |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 5003, 5159 | ra1_soviets_nuclearyak_yaknuclearbomb | Warhead@ShieldHitEffectNuclear |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 5250, 5256, 5257 | ra1_soviets_migattackbomber_teslamaverick | Warhead@Tesla_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 5335, 5341, 5342 | ra1_soviets_migattackbomber_teslamaverickfragment1 | Warhead@Tesla_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 5405, 5416, 5417 | ra1_soviets_migattackbomber_teslamaverickfragment2 | Warhead@Tesla_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 5487, 5513 | ra1_soviets_migattackbomber_thermobaricmaverick | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 5497, 5621, 5622 | ra1_soviets_migattackbomber_thermobaricmaverick | Warhead@Thermobaric_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 5503, 5574 | ra1_soviets_migattackbomber_thermobaricmaverick | Warhead@NuclearWarheadPercentage |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 5504, 5606 | ra1_soviets_migattackbomber_thermobaricmaverick | Warhead@Smudge1 |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 5505, 5611 | ra1_soviets_migattackbomber_thermobaricmaverick | Warhead@Smudge2 |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 5506, 5616 | ra1_soviets_migattackbomber_thermobaricmaverick | Warhead@Smudge3 |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 5507, 5603 | ra1_soviets_migattackbomber_thermobaricmaverick | Warhead@ShieldHitEffectNuclear |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 5508, 5523 | ra1_soviets_migattackbomber_thermobaricmaverick | Warhead@HeavyMissilePercentage |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 5510, 5547 | ra1_soviets_migattackbomber_thermobaricmaverick | Warhead@MediumFlameWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 5511, 5600 | ra1_soviets_migattackbomber_thermobaricmaverick | Warhead@RA2Scorch |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 5512, 5517 | ra1_soviets_migattackbomber_thermobaricmaverick | Warhead@GroundFire |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 5784, 5793, 5794 | ra1_soviets_heavytank_105mmthermobaric | Warhead@Flame_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 5890, 5975 | ShtoraLaser | Projectile |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 5921, 6057, 6082 | ShtoraLaser | Warhead@Laser_Light |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 5971, 6022 | ShtoraLaser | Warhead@LightFlameWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 5972, 6016 | ShtoraLaser | Warhead@GroundFire |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 5973, 5992 | ShtoraLaser | Warhead@FlakWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 5974, 5988 | ShtoraLaser | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 5985, 6053 | ShtoraLaser | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 6058, 6092 | ShtoraLaser | Warhead@Laser_Light_ExtraDamage |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 6140, 6248 | ra120mmThermobaric | Projectile |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 6234, 6292, 6293 | ra120mmThermobaric | Warhead@Thermobaric_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 6242, 6255 | ra120mmThermobaric | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 6243, 6258 | ra120mmThermobaric | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 6244, 6271 | ra120mmThermobaric | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 6245, 6266 | ra120mmThermobaric | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 6246, 6261 | ra120mmThermobaric | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 6247, 6253 | ra120mmThermobaric | Warhead@Concrete |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 6274, 6282 | ra120mmThermobaric | Warhead@RA2Scorch |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 6275, 6276 | ra120mmThermobaric | Warhead@GroundFire |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 6435, 6493, 6494 | ra120mmThermobaricTargetingComputer | Warhead@Thermobaric_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 6442, 6455 | ra120mmThermobaricTargetingComputer | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 6443, 6458 | ra120mmThermobaricTargetingComputer | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 6444, 6471 | ra120mmThermobaricTargetingComputer | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 6445, 6466 | ra120mmThermobaricTargetingComputer | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 6446, 6461 | ra120mmThermobaricTargetingComputer | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 6447, 6453 | ra120mmThermobaricTargetingComputer | Warhead@Concrete |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 6475, 6483 | ra120mmThermobaricTargetingComputer | Warhead@RA2Scorch |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 6476, 6477 | ra120mmThermobaricTargetingComputer | Warhead@GroundFire |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 6560, 6608 | MammothTuskThermobaric | Projectile |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 6572, 6634 | MammothTuskThermobaric | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 6578, 6637 | MammothTuskThermobaric | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 6590, 6752 | MammothTuskThermobaric | Warhead@LightFlameWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 6591, 6870 | MammothTuskThermobaric | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 6592, 6833 | MammothTuskThermobaric | Warhead@RA2Scorch |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 6593, 6837 | MammothTuskThermobaric | Warhead@ShieldHit |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 6594, 6842 | MammothTuskThermobaric | Warhead@ShieldHitEffect |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 6595, 6694 | MammothTuskThermobaric | Warhead@GroundFire |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 6596, 6779 | MammothTuskThermobaric | Warhead@MediumFlameWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 6597, 6700 | MammothTuskThermobaric | Warhead@HeavyFlameWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 6598, 6669 | MammothTuskThermobaric | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 6599, 6628 | MammothTuskThermobaric | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 6600, 6631 | MammothTuskThermobaric | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 6601, 6640 | MammothTuskThermobaric | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 6602, 6626 | MammothTuskThermobaric | Warhead@Concrete |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 6603, 6645 | MammothTuskThermobaric | Warhead@FlakWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 6604, 6845 | MammothTuskThermobaric | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 6605, 6806 | MammothTuskThermobaric | Warhead@MediumMissilePercentage |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 6606, 6830 | MammothTuskThermobaric | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 6607, 6728 | MammothTuskThermobaric | Warhead@HeavyMissilePercentage |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 6893, 6944 | MammothTuskTesla | Projectile |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 6933, 6973 | MammothTuskTesla | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 6934, 7062 | MammothTuskTesla | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 6935, 6961 | MammothTuskTesla | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 6936, 6964 | MammothTuskTesla | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 6937, 6959 | MammothTuskTesla | Warhead@Concrete |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 6939, 7028 | MammothTuskTesla | Warhead@LightFlameWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 6940, 7058 | MammothTuskTesla | Warhead@RA2Scorch |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 6941, 6998 | MammothTuskTesla | Warhead@GroundFire |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 6942, 7004 | MammothTuskTesla | Warhead@HeavyMissilePercentage |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 6943, 7055 | MammothTuskTesla | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 6967, 7066, 7070 | MammothTuskTesla | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 6970, 7073 | MammothTuskTesla | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 7104, 7105 | MammothTuskTeslaFragment1Ground | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 7136, 7137 | MammothTuskTeslaFragment2Ground | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 7210, 7211 | MammothTuskTeslaFragment1 | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 7274, 7275 | MammothTuskTeslaFragment2 | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 7301, 7302 | MammothTuskTeslaTargetingComputer | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 7332, 7343 | ra1_soviets_siegemammothtank_ra120mm2 | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 7337, 7354 | ra1_soviets_siegemammothtank_ra120mm2 | Warhead@LightChemicalWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 7338, 7381 | ra1_soviets_siegemammothtank_ra120mm2 | Warhead@MediumFlameWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 7339, 7408 | ra1_soviets_siegemammothtank_ra120mm2 | Warhead@RA2Scorch |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 7340, 7348 | ra1_soviets_siegemammothtank_ra120mm2 | Warhead@GroundFire |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 7435, 7543 | ra1_soviets_siegemammothtank_ra120mm2thermobaric | Projectile |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 7529, 7587, 7588 | ra1_soviets_siegemammothtank_ra120mm2thermobaric | Warhead@Thermobaric_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 7537, 7550 | ra1_soviets_siegemammothtank_ra120mm2thermobaric | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 7538, 7553 | ra1_soviets_siegemammothtank_ra120mm2thermobaric | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 7539, 7566 | ra1_soviets_siegemammothtank_ra120mm2thermobaric | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 7540, 7561 | ra1_soviets_siegemammothtank_ra120mm2thermobaric | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 7541, 7556 | ra1_soviets_siegemammothtank_ra120mm2thermobaric | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 7542, 7548 | ra1_soviets_siegemammothtank_ra120mm2thermobaric | Warhead@Concrete |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 7569, 7577 | ra1_soviets_siegemammothtank_ra120mm2thermobaric | Warhead@RA2Scorch |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 7570, 7571 | ra1_soviets_siegemammothtank_ra120mm2thermobaric | Warhead@GroundFire |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 7731, 7789, 7790 | ra1_soviets_siegemammothtank_ra120mm2thermobarictargetingcomputer | Warhead@Thermobaric_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 7738, 7751 | ra1_soviets_siegemammothtank_ra120mm2thermobarictargetingcomputer | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 7739, 7754 | ra1_soviets_siegemammothtank_ra120mm2thermobarictargetingcomputer | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 7740, 7767 | ra1_soviets_siegemammothtank_ra120mm2thermobarictargetingcomputer | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 7741, 7762 | ra1_soviets_siegemammothtank_ra120mm2thermobarictargetingcomputer | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 7742, 7757 | ra1_soviets_siegemammothtank_ra120mm2thermobarictargetingcomputer | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 7743, 7749 | ra1_soviets_siegemammothtank_ra120mm2thermobarictargetingcomputer | Warhead@Concrete |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 7771, 7779 | ra1_soviets_siegemammothtank_ra120mm2thermobarictargetingcomputer | Warhead@RA2Scorch |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 7772, 7773 | ra1_soviets_siegemammothtank_ra120mm2thermobarictargetingcomputer | Warhead@GroundFire |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 7849, 7875 | ra1_soviets_siegemammothtank_mammothtusk2 | Projectile |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 7852, 7901 | ra1_soviets_siegemammothtank_mammothtusk2 | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 7861, 7945 | ra1_soviets_siegemammothtank_mammothtusk2 | Warhead@LightFlameWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 7862, 8014 | ra1_soviets_siegemammothtank_mammothtusk2 | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 7863, 8002 | ra1_soviets_siegemammothtank_mammothtusk2 | Warhead@RA2Scorch |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 7864, 8006 | ra1_soviets_siegemammothtank_mammothtusk2 | Warhead@ShieldHit |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 7865, 8011 | ra1_soviets_siegemammothtank_mammothtusk2 | Warhead@ShieldHitEffect |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 7866, 7915 | ra1_soviets_siegemammothtank_mammothtusk2 | Warhead@GroundFire |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 7867, 7972 | ra1_soviets_siegemammothtank_mammothtusk2 | Warhead@MediumChemicalWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 7868, 7893 | ra1_soviets_siegemammothtank_mammothtusk2 | Warhead@Concrete |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 7869, 7921 | ra1_soviets_siegemammothtank_mammothtusk2 | Warhead@HeavyMissilePercentage |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 7870, 7895 | ra1_soviets_siegemammothtank_mammothtusk2 | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 7871, 7898 | ra1_soviets_siegemammothtank_mammothtusk2 | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 7872, 7999 | ra1_soviets_siegemammothtank_mammothtusk2 | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 7873, 7906 | ra1_soviets_siegemammothtank_mammothtusk2 | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 7874, 7911 | ra1_soviets_siegemammothtank_mammothtusk2 | Warhead@Glow |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 8045, 8093 | ra1_soviets_siegemammothtank_mammothtusk2thermobaric | Projectile |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 8057, 8115 | ra1_soviets_siegemammothtank_mammothtusk2thermobaric | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 8063, 8131 | ra1_soviets_siegemammothtank_mammothtusk2thermobaric | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 8075, 8169 | ra1_soviets_siegemammothtank_mammothtusk2thermobaric | Warhead@HeavyFlameWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 8076, 8235 | ra1_soviets_siegemammothtank_mammothtusk2thermobaric | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 8077, 8224 | ra1_soviets_siegemammothtank_mammothtusk2thermobaric | Warhead@RA2Scorch |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 8078, 8227 | ra1_soviets_siegemammothtank_mammothtusk2thermobaric | Warhead@ShieldHit |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 8079, 8232 | ra1_soviets_siegemammothtank_mammothtusk2thermobaric | Warhead@ShieldHitEffect |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 8080, 8138 | ra1_soviets_siegemammothtank_mammothtusk2thermobaric | Warhead@GroundFire |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 8081, 8144 | ra1_soviets_siegemammothtank_mammothtusk2thermobaric | Warhead@HeavyBombPercentage |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 8082, 8238 | ra1_soviets_siegemammothtank_mammothtusk2thermobaric | Warhead@Smudge1 |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 8083, 8221 | ra1_soviets_siegemammothtank_mammothtusk2thermobaric | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 8084, 8241 | ra1_soviets_siegemammothtank_mammothtusk2thermobaric | Warhead@Smudge2 |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 8085, 8246 | ra1_soviets_siegemammothtank_mammothtusk2thermobaric | Warhead@Smudge2RA2 |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 8086, 8118 | ra1_soviets_siegemammothtank_mammothtusk2thermobaric | Warhead@Effect1 |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 8087, 8123 | ra1_soviets_siegemammothtank_mammothtusk2thermobaric | Warhead@Effect2 |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 8088, 8134 | ra1_soviets_siegemammothtank_mammothtusk2thermobaric | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 8089, 8107 | ra1_soviets_siegemammothtank_mammothtusk2thermobaric | Warhead@Concrete |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 8090, 8197 | ra1_soviets_siegemammothtank_mammothtusk2thermobaric | Warhead@HeavyMissilePercentage |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 8091, 8109 | ra1_soviets_siegemammothtank_mammothtusk2thermobaric | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 8092, 8112 | ra1_soviets_siegemammothtank_mammothtusk2thermobaric | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 8280, 8299 | ra1_soviets_monstertank_120mm_cannon | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 8286, 8317 | ra1_soviets_monstertank_120mm_cannon | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 8287, 8293 | ra1_soviets_monstertank_120mm_cannon | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 8288, 8296 | ra1_soviets_monstertank_120mm_cannon | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 8289, 8311 | ra1_soviets_monstertank_120mm_cannon | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 8290, 8306 | ra1_soviets_monstertank_120mm_cannon | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 8291, 8301 | ra1_soviets_monstertank_120mm_cannon | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 8292, 8314 | ra1_soviets_monstertank_120mm_cannon | Warhead@ShieldHitEffect |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 8338, 8346 | ra1_soviets_monstertank_120mm_cannon_inferno | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 8339, 8349 | ra1_soviets_monstertank_120mm_cannon_inferno | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 8340, 8364 | ra1_soviets_monstertank_120mm_cannon_inferno | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 8341, 8359 | ra1_soviets_monstertank_120mm_cannon_inferno | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 8342, 8354 | ra1_soviets_monstertank_120mm_cannon_inferno | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 8343, 8344 | ra1_soviets_monstertank_120mm_cannon_inferno | Warhead@Concrete |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 8352, 8369 | ra1_soviets_monstertank_120mm_cannon_inferno | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 8367, 8382, 8392 | ra1_soviets_monstertank_120mm_cannon_inferno | Warhead@RA2Scorch |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 8368, 8376 | ra1_soviets_monstertank_120mm_cannon_inferno | Warhead@GroundFire |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 8414, 8466 | ra1_soviets_monstertank_missile | Warhead@MissileAP_Heavy |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 8480, 8532 | ra1_soviets_monstertank_missile_tesla | Projectile |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 8521, 8561 | ra1_soviets_monstertank_missile_tesla | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 8522, 8653 | ra1_soviets_monstertank_missile_tesla | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 8523, 8549 | ra1_soviets_monstertank_missile_tesla | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 8524, 8552 | ra1_soviets_monstertank_missile_tesla | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 8525, 8547 | ra1_soviets_monstertank_missile_tesla | Warhead@Concrete |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 8527, 8618 | ra1_soviets_monstertank_missile_tesla | Warhead@LightFlameWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 8528, 8649 | ra1_soviets_monstertank_missile_tesla | Warhead@RA2Scorch |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 8529, 8587 | ra1_soviets_monstertank_missile_tesla | Warhead@GroundFire |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 8530, 8593 | ra1_soviets_monstertank_missile_tesla | Warhead@HeavyMissilePercentage |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 8531, 8646 | ra1_soviets_monstertank_missile_tesla | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 8555, 8657, 8661 | ra1_soviets_monstertank_missile_tesla | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 8558, 8664 | ra1_soviets_monstertank_missile_tesla | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 8685, 8733 | ra1_soviets_monstertank_missile_thermobaric | Projectile |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 8697, 8759 | ra1_soviets_monstertank_missile_thermobaric | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 8703, 8762 | ra1_soviets_monstertank_missile_thermobaric | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 8715, 8881 | ra1_soviets_monstertank_missile_thermobaric | Warhead@LightFlameWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 8716, 9003 | ra1_soviets_monstertank_missile_thermobaric | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 8717, 8965 | ra1_soviets_monstertank_missile_thermobaric | Warhead@RA2Scorch |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 8718, 8969 | ra1_soviets_monstertank_missile_thermobaric | Warhead@ShieldHit |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 8719, 8974 | ra1_soviets_monstertank_missile_thermobaric | Warhead@ShieldHitEffect |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 8720, 8821 | ra1_soviets_monstertank_missile_thermobaric | Warhead@GroundFire |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 8721, 8909 | ra1_soviets_monstertank_missile_thermobaric | Warhead@MediumFlameWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 8722, 8827 | ra1_soviets_monstertank_missile_thermobaric | Warhead@HeavyFlameWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 8723, 8795 | ra1_soviets_monstertank_missile_thermobaric | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 8724, 8753 | ra1_soviets_monstertank_missile_thermobaric | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 8725, 8756 | ra1_soviets_monstertank_missile_thermobaric | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 8726, 8765 | ra1_soviets_monstertank_missile_thermobaric | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 8727, 8751 | ra1_soviets_monstertank_missile_thermobaric | Warhead@Concrete |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 8728, 8770 | ra1_soviets_monstertank_missile_thermobaric | Warhead@FlakWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 8729, 8977 | ra1_soviets_monstertank_missile_thermobaric | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 8730, 8937 | ra1_soviets_monstertank_missile_thermobaric | Warhead@MediumMissilePercentage |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 8731, 8962 | ra1_soviets_monstertank_missile_thermobaric | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 8732, 8856 | ra1_soviets_monstertank_missile_thermobaric | Warhead@HeavyMissilePercentage |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 9018, 9031 | ra1_soviets_volkov_volkovmagneticweapon | Projectile |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 9025, 9036 | ra1_soviets_volkov_volkovmagneticweapon | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 9029, 9068 | ra1_soviets_volkov_volkovmagneticweapon | Warhead@MediumChemicalWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 9030, 9043 | ra1_soviets_volkov_volkovmagneticweapon | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 9298, 9338 | ra1_soviets_volkov_volkovmagneticweaponincendiary | Warhead@MediumChemicalWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 9299, 9313 | ra1_soviets_volkov_volkovmagneticweaponincendiary | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 9376, 9432 | ra1_soviets_volkov_volkovmagneticweaponincendiarytesla | Projectile |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 9378, 9472 | ra1_soviets_volkov_volkovmagneticweaponincendiarytesla | Warhead@MediumChemicalWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 9388, 9444 | ra1_soviets_volkov_volkovmagneticweaponincendiarytesla | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 9429, 9496 | ra1_soviets_volkov_volkovmagneticweaponincendiarytesla | Warhead@MediumFlameWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 9430, 9524 | ra1_soviets_volkov_volkovmagneticweaponincendiarytesla | Warhead@RA2Scorch |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 9431, 9466 | ra1_soviets_volkov_volkovmagneticweaponincendiarytesla | Warhead@GroundFire |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 9438, 9530 | ra1_soviets_volkov_volkovmagneticweaponincendiarytesla | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 9642, 9686 | ra1_soviets_volkov_volkovmagneticweaponincendiarynuclearshells | Projectile |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 9652, 9725 | ra1_soviets_volkov_volkovmagneticweaponincendiarynuclearshells | Warhead@MediumChemicalWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 9660, 9696 | ra1_soviets_volkov_volkovmagneticweaponincendiarynuclearshells | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 9664, 9690 | ra1_soviets_volkov_volkovmagneticweaponincendiarynuclearshells | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 9675, 9808 | ra1_soviets_volkov_volkovmagneticweaponincendiarynuclearshells | Warhead@ShieldHit |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 9678, 9750 | ra1_soviets_volkov_volkovmagneticweaponincendiarynuclearshells | Warhead@MediumFlameWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 9679, 9805 | ra1_soviets_volkov_volkovmagneticweaponincendiarynuclearshells | Warhead@RA2Scorch |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 9680, 9719 | ra1_soviets_volkov_volkovmagneticweaponincendiarynuclearshells | Warhead@GroundFire |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 9681, 9778 | ra1_soviets_volkov_volkovmagneticweaponincendiarynuclearshells | Warhead@NuclearWarheadPercentage |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 9682, 9813 | ra1_soviets_volkov_volkovmagneticweaponincendiarynuclearshells | Warhead@Smudge1 |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 9683, 9818 | ra1_soviets_volkov_volkovmagneticweaponincendiarynuclearshells | Warhead@Smudge2 |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 9684, 9823 | ra1_soviets_volkov_volkovmagneticweaponincendiarynuclearshells | Warhead@Smudge3 |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 9685, 9810 | ra1_soviets_volkov_volkovmagneticweaponincendiarynuclearshells | Warhead@ShieldHitEffectNuclear |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 9835, 9868 | ParaBombNuke | Projectile |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 9841, 9872 | ParaBombNuke | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 9848, 9901 | ParaBombNuke | Warhead@HeavyBombPercentage |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 9849, 10027 | ParaBombNuke | Warhead@Smudge1 |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 9850, 10007 | ParaBombNuke | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 9851, 10032 | ParaBombNuke | Warhead@Smudge2 |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 9852, 10037 | ParaBombNuke | Warhead@Smudge2RA2 |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 9853, 9878 | ParaBombNuke | Warhead@Effect1 |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 9854, 9883 | ParaBombNuke | Warhead@Effect2 |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 9855, 9891 | ParaBombNuke | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 9856, 10013 | ParaBombNuke | Warhead@ShieldHit |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 9857, 10018 | ParaBombNuke | Warhead@ShieldHitEffect |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 9858, 9870 | ParaBombNuke | Warhead@Concrete |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 9860, 9926 | ParaBombNuke | Warhead@HeavyChemicalWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 9861, 9953 | ParaBombNuke | Warhead@HeavyFlameWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 9862, 10024 | ParaBombNuke | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 9863, 10010 | ParaBombNuke | Warhead@RA2Scorch |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 9864, 9895 | ParaBombNuke | Warhead@GroundFire |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 9865, 9981 | ParaBombNuke | Warhead@NuclearWarheadPercentage |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 9866, 10042 | ParaBombNuke | Warhead@Smudge3 |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 9867, 10021 | ParaBombNuke | Warhead@ShieldHitEffectNuclear |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 10065, 10099 | ra1_soviets_samsite_missile_AA | Projectile |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 10072, 10131 | ra1_soviets_samsite_missile_AA | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 10086, 10137 | ra1_soviets_samsite_missile_AA | Warhead@FlakWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 10087, 10127 | ra1_soviets_samsite_missile_AA | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 10088, 10133 | ra1_soviets_samsite_missile_AA | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 10089, 10240 | ra1_soviets_samsite_missile_AA | Warhead@ShieldHit |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 10090, 10119 | ra1_soviets_samsite_missile_AA | Warhead@Concrete |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 10091, 10245 | ra1_soviets_samsite_missile_AA | Warhead@ShieldHitEffect |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 10092, 10162 | ra1_soviets_samsite_missile_AA | Warhead@HeavyMissilePercentage |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 10093, 10248 | ra1_soviets_samsite_missile_AA | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 10094, 10121 | ra1_soviets_samsite_missile_AA | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 10095, 10124 | ra1_soviets_samsite_missile_AA | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 10096, 10237 | ra1_soviets_samsite_missile_AA | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 10097, 10212 | ra1_soviets_samsite_missile_AA | Warhead@MediumMissilePercentage |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 10098, 10187 | ra1_soviets_samsite_missile_AA | Warhead@LightMissilePercentage |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 10282, 10296, 10297 | ra1_soviets_gatlingtank_incendiaryragatlingtankcannon | Warhead@Flame_Light_Flat |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 10354, 10359, 10360 | ra1_soviets_gatlingtank_incendiaryragatlingtankcannon_AA | Warhead@Flame_Light_Flat |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 10576, 10592, 10593 | ra1_soviets_rifleinfantry_carbine_incendiary | Warhead@Flame_Light_Flat |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 10584, 10585 | ra1_soviets_rifleinfantry_carbine_incendiary | Projectile |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 10667, 10687 | ra1_soviets_kotinnucleartank_kotincannonnuclearshell | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 10674, 10705 | ra1_soviets_kotinnucleartank_kotincannonnuclearshell | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 10675, 10681 | ra1_soviets_kotinnucleartank_kotincannonnuclearshell | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 10676, 10684 | ra1_soviets_kotinnucleartank_kotincannonnuclearshell | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 10677, 10699 | ra1_soviets_kotinnucleartank_kotincannonnuclearshell | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 10678, 10694 | ra1_soviets_kotinnucleartank_kotincannonnuclearshell | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 10679, 10689 | ra1_soviets_kotinnucleartank_kotincannonnuclearshell | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 10680, 10702 | ra1_soviets_kotinnucleartank_kotincannonnuclearshell | Warhead@ShieldHitEffect |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 10716, 10720, 10721 | ra1_soviets_hindattackhelicopter_hindmissiles | Warhead@MissileAP_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 10778, 10782, 10783 | ra1_soviets_hindattackhelicopter_incendiarychaingun | Warhead@Flame_Light_Flat |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 10836, 10840, 10841 | ra1_soviets_kamovattackhelicopter_hindmissiles | Warhead@MissileAP_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 10898, 10902, 10903 | ra1_soviets_kamovattackhelicopter_incendiarychaingun | Warhead@Flame_Light_Flat |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 10975, 10977, 10978 | ra1_soviets_rocketsoldier_rocketsra | Warhead@MissileAP_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 11063, 11109, 11110 | ra1_soviets_v2rocketlauncher_scud | Warhead@MissileHE_Heavy |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 11136, 11149 | ra1_soviets_mammothtank_ra120mmthermobaric | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 11137, 11152 | ra1_soviets_mammothtank_ra120mmthermobaric | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 11138, 11165 | ra1_soviets_mammothtank_ra120mmthermobaric | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 11139, 11160 | ra1_soviets_mammothtank_ra120mmthermobaric | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 11140, 11155 | ra1_soviets_mammothtank_ra120mmthermobaric | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 11141, 11147 | ra1_soviets_mammothtank_ra120mmthermobaric | Warhead@Concrete |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 11169, 11177 | ra1_soviets_mammothtank_ra120mmthermobaric | Warhead@RA2Scorch |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 11170, 11171 | ra1_soviets_mammothtank_ra120mmthermobaric | Warhead@GroundFire |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 11187, 11188 | ra1_soviets_mammothtank_ra120mmthermobaric | Warhead@Thermobaric_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 11250, 11251 | ra1_soviets_mammothtank_mammothtusktesla | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 11286, 11298, 11299 | ra1_soviets_kamovattackhelicopter_kamovmissileteslaarcfragment1 | Warhead@Tesla_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 11353, 11365, 11366 | ra1_soviets_kamovattackhelicopter_kamovmissileteslaarcfragment2 | Warhead@Tesla_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 11424, 11425 | MammothTuskTeslaInfantryFragment1 | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 11459, 11460 | MammothTuskTeslaInfantryFragment2 | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 11485, 11486 | ra1_soviets_v2rocketlauncher_scudteslafragment1 | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 11508, 11509 | ra1_soviets_v2rocketlauncher_scudteslafragment2 | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 11541, 11542 | MammothTuskTeslaInfantryFragment1_ExplicitDamage21of20 | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 11590, 11591 | MammothTuskTeslaFragment1Ground_ExplicitDamage1of4 | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 11623, 11624 | MammothTuskTeslaFragment2Ground_ExplicitDamage1of4 | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/sequences.yaml | 11, 37 | ra2_allies_constructionyard | dead |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 11, 67 | RA2Patriot | Projectile |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 17, 72 | RA2Patriot | Warhead@MissileAA_Medium |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 120, 194 | HarrierMissiles | Projectile |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 127, 340, 341 | HarrierMissiles | Warhead@MissileAP_Medium |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 188, 305 | HarrierMissiles | Warhead@LightFlameWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 189, 333 | HarrierMissiles | Warhead@RA2Scorch |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 190, 226 | HarrierMissiles | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 191, 277 | HarrierMissiles | Warhead@HeavyChemicalWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 192, 252 | HarrierMissiles | Warhead@HeavyAAWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 193, 201 | HarrierMissiles | Warhead@FlakWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 363, 405 | HarrierMissiles_elite | Warhead@MissileAP_Medium |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 423, 532, 541 | BlackEagleMissiles | Projectile |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 528, 607, 608 | BlackEagleMissiles | Warhead@MissileAP_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 536, 556 | BlackEagleMissiles | Warhead@Smudge1 |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 537, 559 | BlackEagleMissiles | Warhead@Smudge2 |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 538, 564 | BlackEagleMissiles | Warhead@Smudge2RA2 |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 539, 543 | BlackEagleMissiles | Warhead@Effect1 |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 540, 548 | BlackEagleMissiles | Warhead@Effect2 |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 569, 591 | BlackEagleMissiles | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 570, 602 | BlackEagleMissiles | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 571, 576 | BlackEagleMissiles | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 572, 579 | BlackEagleMissiles | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 573, 586 | BlackEagleMissiles | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 582, 605 | BlackEagleMissiles | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 662, 765 | BlackEagleMissiles_elite | Projectile |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 756, 831, 832 | BlackEagleMissiles_elite | Warhead@MissileAP_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 760, 780 | BlackEagleMissiles_elite | Warhead@Smudge1 |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 761, 783 | BlackEagleMissiles_elite | Warhead@Smudge2 |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 762, 788 | BlackEagleMissiles_elite | Warhead@Smudge2RA2 |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 763, 767 | BlackEagleMissiles_elite | Warhead@Effect1 |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 764, 772 | BlackEagleMissiles_elite | Warhead@Effect2 |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 793, 815 | BlackEagleMissiles_elite | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 794, 826 | BlackEagleMissiles_elite | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 795, 800 | BlackEagleMissiles_elite | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 796, 803 | BlackEagleMissiles_elite | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 797, 810 | BlackEagleMissiles_elite | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 806, 829 | BlackEagleMissiles_elite | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 893, 994 | BlackEagleThunderboltMissiles | Projectile |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 908, 1197, 1198 | BlackEagleThunderboltMissiles | Warhead@MissileAP_Light |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 985, 1050 | BlackEagleThunderboltMissiles | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 986, 1172 | BlackEagleThunderboltMissiles | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 987, 1099 | BlackEagleThunderboltMissiles | Warhead@HeavyBombPercentage |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 988, 1124 | BlackEagleThunderboltMissiles | Warhead@HeavyMissilePercentage |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 989, 1075 | BlackEagleThunderboltMissiles | Warhead@HeavyAAWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 990, 1148 | BlackEagleThunderboltMissiles | Warhead@MediumMissilePercentage |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 991, 1026 | BlackEagleThunderboltMissiles | Warhead@FlakWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 993, 1000 | BlackEagleThunderboltMissiles | Warhead@ArrowWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 1216, 1258 | BlackEagleThunderboltMissiles_elite | Warhead@MissileAP_Light |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 1385, 1458 | RA2Comet | Projectile |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 1396, 1508, 1509 | RA2Comet | Warhead@Demolition_Light |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 1453, 1469, 1478, 1483, 1500 | RA2Comet | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 1455, 1463 | RA2Comet | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 1456, 1466 | RA2Comet | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 1457, 1472, 1477 | RA2Comet | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 1479, 1493 | RA2Comet | Warhead@RA2Scorch |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 1480, 1485 | RA2Comet | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 1496, 1506 | RA2Comet | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 1522, 1533 | RA2Comet_elite | Projectile |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 1530, 1538 | RA2Comet_elite | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 1531, 1541 | RA2Comet_elite | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 1532, 1547, 1552 | RA2Comet_elite | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 1544, 1553, 1558, 1574, 1575 | RA2Comet_elite | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 1554, 1568 | RA2Comet_elite | Warhead@RA2Scorch |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 1555, 1560 | RA2Comet_elite | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 1571, 1579 | RA2Comet_elite | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 1583, 1625 | RA2Comet_elite | Warhead@Demolition_Light |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 1642, 1826 | RA2GrandCannonWeapon | Projectile |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 1667, 1832, 1833 | RA2GrandCannonWeapon | Warhead@CannonHE_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 1671, 1706 | RA2GrandCannonWeapon | Warhead@HeavyBombPercentage |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 1672, 1813 | RA2GrandCannonWeapon | Warhead@Smudge1 |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 1673, 1816 | RA2GrandCannonWeapon | Warhead@Smudge2 |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 1674, 1821 | RA2GrandCannonWeapon | Warhead@Smudge2RA2 |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 1675, 1687 | RA2GrandCannonWeapon | Warhead@Effect1 |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 1676, 1692 | RA2GrandCannonWeapon | Warhead@Effect2 |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 1678, 1758 | RA2GrandCannonWeapon | Warhead@MediumFlameWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 1679, 1785 | RA2GrandCannonWeapon | Warhead@RA2Scorch |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 1680, 1700 | RA2GrandCannonWeapon | Warhead@GroundFire |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 1681, 1788 | RA2GrandCannonWeapon | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 1682, 1731 | RA2GrandCannonWeapon | Warhead@LightChemicalWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 1683, 1830 | RA2GrandCannonWeapon | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 1902, 1903 | RA2MirageGun_elite | Warhead@CannonAP_Light_Flat |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 1960, 1981 | RA2HeavyMirageGun | Projectile |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 1972, 2040, 2041 | RA2HeavyMirageGun | Warhead@CannonAP_Light_Flat |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 1978, 2015 | RA2HeavyMirageGun | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 1980, 1988 | RA2HeavyMirageGun | Warhead@MediumChemicalWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 2094, 2145 | RA2HeavyMirageGun_elite | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 2096, 2116 | RA2HeavyMirageGun_elite | Warhead@MediumChemicalWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 2170, 2171 | RA2HeavyMirageGun_elite | Warhead@CannonAP_Light_Flat |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 2246, 2303 | RA2Medusa_AA | Projectile |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 2253, 2307 | RA2Medusa_AA | Warhead@MissileAA_Medium |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 2372, 2374, 2375 | RA2vulcan2 | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 2437, 2442, 2443 | GuardianGIMG | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 2514, 2518, 2519 | GuardianGIMG_elite | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 2639, 2652 | ra2roktgun | Projectile |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 2640, 2772, 2773 | ra2roktgun | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 2645, 2701 | ra2roktgun | Warhead@LightMissilePercentage |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 2646, 2752 | ra2roktgun | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 2647, 2669 | ra2roktgun | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 2648, 2725 | ra2roktgun | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 2649, 2672 | ra2roktgun | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 2650, 2728 | ra2roktgun | Warhead@SmallArmsPercentage |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 2651, 2677 | ra2roktgun | Warhead@FlakWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 2675, 2757, 2760 | ra2roktgun | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 2756, 2764, 2770 | ra2roktgun | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/sequences.yaml | 3775, 3781 | yrslav | cheer |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/sequences.yaml | 3914, 3918 | ra2howi | muzzle |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/sequences.yaml | 3927, 3931 | ra2arty | muzzle |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 873, 883 | RA2GIRocketsG | Projectile |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 995, 1015 | RA2MirageGun | Projectile |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 1007, 1048, 1049 | RA2MirageGun | Warhead@CannonAP_Light_Flat |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 1014, 1021 | RA2MirageGun | Warhead@MediumChemicalWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 1119, 1176 | RA2HoverMissile | Warhead@MissileHE_Light |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 1189, 1240 | RA2HoverMissile_elite | Warhead@MissileHE_Light |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 1251, 1302 | RA2HoverMissile_AA | Warhead@MissileAA_Light |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 1313, 1364 | RA2HoverMissile_AA_elite | Warhead@MissileAA_Light |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 1389, 1443 | RA2ThunderboltMissile | Warhead@MissileAP_Light |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 1458, 1511 | RA2ThunderboltMissile_elite | Warhead@MissileAP_Light |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 1573, 1625 | RA2MultiHoverMissile | Warhead@MissileAP_Light |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 1639, 1691 | RA2MultiHoverMissile_elite | Warhead@MissileAP_Light |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 1705, 1757 | RA2MultiHoverMissile_AA | Warhead@MissileAA_Light |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 1771, 1823 | RA2MultiHoverMissile_AA_elite | Warhead@MissileAA_Light |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 1848, 1902 | RA2MultiThunderboltMissile | Warhead@MissileAP_Light |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 1917, 1970 | RA2MultiThunderboltMissile_elite | Warhead@MissileAP_Light |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 2064, 2095 | RA2TRIPODPLAZMA | Projectile |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 2086, 2104 | RA2TRIPODPLAZMA | Warhead@1Dam |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 2087, 2140 | RA2TRIPODPLAZMA | Warhead@Percentage |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 2088, 2116 | RA2TRIPODPLAZMA | Warhead@2Smu |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 2089, 2134 | RA2TRIPODPLAZMA | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 2090, 2137 | RA2TRIPODPLAZMA | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 2091, 2152 | RA2TRIPODPLAZMA | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 2092, 2119 | RA2TRIPODPLAZMA | Warhead@3Eff |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 2093, 2124 | RA2TRIPODPLAZMA | Warhead@4EffAir |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 2094, 2129 | RA2TRIPODPLAZMA | Warhead@4EffWater |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 2187, 2220 | RA2REVENANTAA | Projectile |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 2211, 2226 | RA2REVENANTAA | Warhead@1Dam |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 2212, 2262 | RA2REVENANTAA | Warhead@Percentage |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 2213, 2238 | RA2REVENANTAA | Warhead@2Smu |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 2214, 2256 | RA2REVENANTAA | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 2215, 2259 | RA2REVENANTAA | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 2216, 2274 | RA2REVENANTAA | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 2217, 2241 | RA2REVENANTAA | Warhead@3Eff |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 2218, 2246 | RA2REVENANTAA | Warhead@4EffAir |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 2219, 2251 | RA2REVENANTAA | Warhead@4EffWater |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 2649, 2711, 2712 | TeslaArmorDischargeDummy | Warhead@MissileAP_Light |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 2743, 2785 | TeslaArmorDischargeArc | Warhead@MissileAP_Light |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 2810, 2852 | TeslaArmorDischargeFragment1 | Warhead@MissileAP_Light |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 2876, 2918 | TeslaArmorDischargeFragment2 | Warhead@MissileAP_Light |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 3194, 3215, 3221 | RA2HornetMissile | Projectile |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 3211, 3282, 3283 | RA2HornetMissile | Warhead@MissileAP_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 3217, 3231 | RA2HornetMissile | Warhead@Smudge1 |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 3218, 3234 | RA2HornetMissile | Warhead@Smudge2 |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 3219, 3239 | RA2HornetMissile | Warhead@Smudge2RA2 |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 3220, 3223 | RA2HornetMissile | Warhead@Effect2 |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 3244, 3266 | RA2HornetMissile | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 3245, 3277 | RA2HornetMissile | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 3246, 3251 | RA2HornetMissile | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 3247, 3254 | RA2HornetMissile | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 3248, 3261 | RA2HornetMissile | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 3257, 3280 | RA2HornetMissile | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 3337, 3345 | RA2GIRockets | Projectile |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 3361, 3449 | MigMissiles | Projectile |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 3442, 3512 | MigMissiles | Warhead@HeavyBombPercentage |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 3443, 3538 | MigMissiles | Warhead@Smudge1 |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 3444, 3541 | MigMissiles | Warhead@Smudge2 |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 3445, 3546 | MigMissiles | Warhead@Smudge2RA2 |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 3446, 3454 | MigMissiles | Warhead@Effect2 |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 3447, 3487 | MigMissiles | Warhead@HeavyAAWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 3448, 3462 | MigMissiles | Warhead@FlakWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 3552, 3575 | MigMissiles | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 3553, 3570 | MigMissiles | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 3554, 3586 | MigMissiles | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 3555, 3559 | MigMissiles | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 3556, 3562 | MigMissiles | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 3596, 3646 | MigMissiles_rad | Projectile |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 3608, 3729 | MigMissiles_rad | Warhead@Radiation |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 3612, 3759 | MigMissiles_rad | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 3617, 3639, 3705 | MigMissiles_rad | Warhead@HeavyBombPercentage |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 3620, 3644, 3682 | MigMissiles_rad | Warhead@HeavyAAWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 3623, 3645, 3659 | MigMissiles_rad | Warhead@FlakWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 3640, 3733 | MigMissiles_rad | Warhead@Smudge1 |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 3641, 3736 | MigMissiles_rad | Warhead@Smudge2 |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 3642, 3741 | MigMissiles_rad | Warhead@Smudge2RA2 |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 3643, 3651 | MigMissiles_rad | Warhead@Effect2 |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 3746, 3767 | MigMissiles_rad | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 3747, 3762 | MigMissiles_rad | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 3748, 3778 | MigMissiles_rad | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 3749, 3753 | MigMissiles_rad | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 3750, 3756 | MigMissiles_rad | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 3820, 3832 | MigMissiles_fire | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 3821, 3837 | MigMissiles_fire | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 3822, 3824 | MigMissiles_fire | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 3823, 3827 | MigMissiles_fire | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 3830, 3840 | MigMissiles_fire | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 3882, 3894 | MigMissiles_tesla | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 3883, 3899 | MigMissiles_tesla | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 3884, 3886 | MigMissiles_tesla | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 3885, 3889 | MigMissiles_tesla | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 3892, 3902 | MigMissiles_tesla | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 3943, 3966 | MigMissiles_elite | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 3944, 3961 | MigMissiles_elite | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 3945, 3977 | MigMissiles_elite | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 3946, 3950 | MigMissiles_elite | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 3947, 3953 | MigMissiles_elite | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 3986, 3993 | MigMissiles_rad_elite | Warhead@Chemical_Medium |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 3996, 4050 | MigMissiles_rad_elite | Warhead@ShieldHit |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 4020, 4041 | MigMissiles_rad_elite | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 4021, 4036 | MigMissiles_rad_elite | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 4022, 4052 | MigMissiles_rad_elite | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 4023, 4027 | MigMissiles_rad_elite | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 4024, 4030 | MigMissiles_rad_elite | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 4088, 4100 | MigMissiles_fire_elite | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 4089, 4105 | MigMissiles_fire_elite | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 4090, 4092 | MigMissiles_fire_elite | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 4091, 4095 | MigMissiles_fire_elite | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 4098, 4108 | MigMissiles_fire_elite | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 4142, 4165 | MigMissiles_AA | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 4143, 4160 | MigMissiles_AA | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 4144, 4176 | MigMissiles_AA | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 4145, 4149 | MigMissiles_AA | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 4146, 4152 | MigMissiles_AA | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 4210, 4233 | MigMissiles_AA_elite | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 4211, 4228 | MigMissiles_AA_elite | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 4212, 4244 | MigMissiles_AA_elite | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 4213, 4217 | MigMissiles_AA_elite | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 4214, 4220 | MigMissiles_AA_elite | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 4310, 4451 | RA2SCUD | Projectile |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 4321, 4455, 4456 | RA2SCUD | Warhead@Demolition_Light |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 4368, 4376, 4453 | RA2SCUD | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 4372, 4386 | RA2SCUD | Warhead@MediumFlameWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 4373, 4448 | RA2SCUD | Warhead@RA2Scorch |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 4374, 4380 | RA2SCUD | Warhead@GroundFire |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 4375, 4413 | RA2SCUD | Warhead@RA2SCUDMissileAP_Heavy_NoWall |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 4469, 4629 | RA2SCUD_rad | Warhead@MediumFlameWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 4473, 4698, 4699 | RA2SCUD_rad | Warhead@Demolition_Light |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 4518, 4535, 4653 | RA2SCUD_rad | Warhead@RA2SCUDMissileAP_Heavy_NoWall |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 4520, 4690 | RA2SCUD_rad | Warhead@Radiation |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 4530, 4575 | RA2SCUD_rad | Warhead@LightChemicalWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 4531, 4602 | RA2SCUD_rad | Warhead@MediumChemicalWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 4532, 4548 | RA2SCUD_rad | Warhead@HeavyChemicalWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 4533, 4687 | RA2SCUD_rad | Warhead@RA2Scorch |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 4534, 4542 | RA2SCUD_rad | Warhead@GroundFire |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 4536, 4696 | RA2SCUD_rad | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 4719, 4761 | RA2SCUD_fire | Warhead@Demolition_Light |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 4781, 4823 | RA2SCUD_tesla | Warhead@Demolition_Light |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 4863, 4914, 4915 | V3Explode | Warhead@Demolition_Light |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 4933, 4975 | DredMissile | Warhead@Demolition_Light |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 4989, 5011 | YRBoomerSCUD | Projectile |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 5000, 5013 | YRBoomerSCUD | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 5006, 5023, 5024 | YRBoomerSCUD | Warhead@MissileAP_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 5010, 5018 | YRBoomerSCUD | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 5105, 5114 | RA2TorpTube | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 5110, 5118, 5119 | RA2TorpTube | Warhead@MissileAP_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 5178, 5186, 5187 | RA2TorpTube_elite | Warhead@MissileAP_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 5270, 5274, 5275 | RA2vulcan3 | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 5345, 5349, 5350 | BlackHawkCannon | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 5447, 5451, 5452 | RA2CRM60 | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 5514, 5548 | RA2CRM60H | Projectile |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 5537, 5583, 5584 | RA2CRM60H | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 5543, 5566 | RA2CRM60H | Warhead@Glow |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 5544, 5573 | RA2CRM60H | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 5545, 5552 | RA2CRM60H | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 5546, 5555 | RA2CRM60H | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 5547, 5570 | RA2CRM60H | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 5558, 5578 | RA2CRM60H | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 5560, 5577, 5580 | RA2CRM60H | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 5564, 5576 | RA2CRM60H | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 5657, 5720 | RA2FlakTrackGun | Warhead@SmallArmsPercentage |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 5658, 5695 | RA2FlakTrackGun | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 5659, 5744 | RA2FlakTrackGun | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 5660, 5687 | RA2FlakTrackGun | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 5661, 5690 | RA2FlakTrackGun | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 5662, 5663 | RA2FlakTrackGun | Warhead@ChaingunPercentage |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 5831, 5835, 5836 | RA2GattlingMG1 | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 5893, 5897, 5898 | RA2GattlingMG1_AA | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 5952, 5953 | RA2GattlingMG2 | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 6011, 6015, 6016 | RA2GattlingMG2_AA | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 6079, 6084 | RA2CloudSafe | Warhead@Toxic_Light |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 6139, 6143, 6144 | RA220mmrapid | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 6276, 6280, 6281 | RA2Terrorist | Warhead@Demolition_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 6365, 6373 | RA2MODTrackMG | Warhead@Bullet_Light |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 6370, 6371 | RA2MODTrackMG | Projectile |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 6578, 6582, 6583 | IvanBomb | Warhead@Demolition_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 6840, 6844, 6845 | TanyaBomb | Warhead@Demolition_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 6922, 6926, 6927 | SealBomb | Warhead@Demolition_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 6996, 7000, 7001 | BorisAKM | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 7159, 7209 | RA2UnitExplode | Warhead@Demolition_Light |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 7201, 7203 | RA2UnitExplode | Projectile |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 7202, 7220 | RA2UnitExplode | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 7223, 7226 | RA2UnitExplode | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 7232, 7240 | RA2UnitExplode | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 7249, 7301 | RA2UnitExplodeBig | Warhead@Demolition_Light |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 7291, 7295 | RA2UnitExplodeBig | Projectile |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 7292, 7320 | RA2UnitExplodeBig | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 7293, 7312 | RA2UnitExplodeBig | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 7294, 7315 | RA2UnitExplodeBig | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 7318, 7323, 7332 | RA2UnitExplodeBig | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 7324, 7356 | RA2UnitExplodeBig | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 7325, 7361 | RA2UnitExplodeBig | Warhead@Smudge1 |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 7326, 7364 | RA2UnitExplodeBig | Warhead@Smudge2 |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 7327, 7369 | RA2UnitExplodeBig | Warhead@Smudge2RA2 |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 7328, 7337 | RA2UnitExplodeBig | Warhead@Effect1 |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 7329, 7342 | RA2UnitExplodeBig | Warhead@Effect2 |
| mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/weapons.yaml | 50, 56 | RA2KirovBomb_rad | Warhead@Radiation |
| mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/weapons.yaml | 180, 194, 195 | RA2120mm_rad | Warhead@Chemical_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/weapons.yaml | 184, 187 | RA2120mm_rad | Warhead@Radiation |
| mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/weapons.yaml | 267, 294, 295 | RA2120mm_rad_elite | Warhead@Chemical_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/weapons.yaml | 271, 285 | RA2120mm_rad_elite | Warhead@Radiation |
| mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/weapons.yaml | 419, 440, 441 | RA160mm | Warhead@Concussion_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/weapons.yaml | 601, 632, 633 | RA160mm_rad | Warhead@Chemical_Light_Flat |
| mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/weapons.yaml | 609, 625 | RA160mm_rad | Warhead@Radiation |
| mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/weapons.yaml | 726, 771, 772 | RA160mmE_elite | Warhead@Concussion_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/weapons.yaml | 733, 764 | RA160mmE_elite | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/weapons.yaml | 734, 753 | RA160mmE_elite | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/weapons.yaml | 750, 769 | RA160mmE_elite | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/weapons.yaml | 827, 996, 997 | RA160mmE_rad_elite | Warhead@Nuclear_Super |
| mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/weapons.yaml | 830, 1027 | RA160mmE_rad_elite | Warhead@Nuclear_Super_Percentage |
| mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/weapons.yaml | 833, 949 | RA160mmE_rad_elite | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/weapons.yaml | 926, 972 | RA160mmE_rad_elite | Warhead@ShieldHit |
| mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/weapons.yaml | 931, 965 | RA160mmE_rad_elite | Warhead@Radiation |
| mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/weapons.yaml | 932, 962 | RA160mmE_rad_elite | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/weapons.yaml | 933, 951 | RA160mmE_rad_elite | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/weapons.yaml | 974, 981 | RA160mmE_rad_elite | Warhead@Smudge1 |
| mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/weapons.yaml | 975, 986 | RA160mmE_rad_elite | Warhead@Smudge2 |
| mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/weapons.yaml | 976, 991 | RA160mmE_rad_elite | Warhead@Smudge3 |
| mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/weapons.yaml | 977, 978 | RA160mmE_rad_elite | Warhead@ShieldHitEffectNuclear |
| mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/weapons.yaml | 1098, 1109 | RA160mmE_tesla_elite | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/weapons.yaml | 1114, 1143 | RA160mmE_tesla_elite | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/weapons.yaml | 1115, 1148 | RA160mmE_tesla_elite | Warhead@Smudge1 |
| mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/weapons.yaml | 1116, 1151 | RA160mmE_tesla_elite | Warhead@Smudge2 |
| mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/weapons.yaml | 1117, 1156 | RA160mmE_tesla_elite | Warhead@Smudge2RA2 |
| mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/weapons.yaml | 1118, 1125 | RA160mmE_tesla_elite | Warhead@Effect1 |
| mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/weapons.yaml | 1119, 1130 | RA160mmE_tesla_elite | Warhead@Effect2 |
| mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/weapons.yaml | 1332, 1335 | RA2120xmm_rad | Warhead@Radiation |
| mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/weapons.yaml | 1401, 1408 | RA2120xmm_rad_elite | Warhead@Radiation |
| mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/weapons.yaml | 1424, 1432 | RA2120xmm_fire_elite | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/weapons.yaml | 1425, 1435 | RA2120xmm_fire_elite | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/weapons.yaml | 1426, 1457 | RA2120xmm_fire_elite | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/weapons.yaml | 1427, 1443 | RA2120xmm_fire_elite | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/weapons.yaml | 1428, 1430 | RA2120xmm_fire_elite | Warhead@Concrete |
| mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/weapons.yaml | 1429, 1448 | RA2120xmm_fire_elite | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/weapons.yaml | 1474, 1505 | RA2120xmm_tesla_elite | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/weapons.yaml | 1475, 1481 | RA2120xmm_tesla_elite | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/weapons.yaml | 1476, 1484 | RA2120xmm_tesla_elite | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/weapons.yaml | 1477, 1500 | RA2120xmm_tesla_elite | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/weapons.yaml | 1478, 1479 | RA2120xmm_tesla_elite | Warhead@Concrete |
| mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/weapons.yaml | 1748, 1760 | MigMissiles_tesla_elite | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/weapons.yaml | 1749, 1765 | MigMissiles_tesla_elite | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/weapons.yaml | 1750, 1752 | MigMissiles_tesla_elite | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/weapons.yaml | 1751, 1755 | MigMissiles_tesla_elite | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/weapons.yaml | 1758, 1768 | MigMissiles_tesla_elite | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/weapons.yaml | 1783, 1837 | RA2MammothTusk_AA | Projectile |
| mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/weapons.yaml | 1787, 1842 | RA2MammothTusk_AA | Warhead@MissileAA_Medium |
| mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/weapons.yaml | 1851, 1901 | RA2MammothTusk_AA_elite | Warhead@MissileAA_Medium |
| mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/weapons.yaml | 1925, 1933, 1934 | RA2vulcan | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/weapons.yaml | 2116, 2120, 2121 | IvanBombAir | Warhead@Demolition_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/weapons.yaml | 2174, 2178, 2179 | BorisAKM_elite | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/weapons.yaml | 2234, 2238, 2239 | BorisAKM2 | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/sequences.yaml | 4, 45 | yuri_constructionyard | build |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 9, 24 | RA2LasherCannon | Projectile |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 16, 32 | RA2LasherCannon | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 17, 121 | RA2LasherCannon | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 19, 63 | RA2LasherCannon | Warhead@LightFlameWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 20, 117 | RA2LasherCannon | Warhead@RA2Scorch |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 21, 57 | RA2LasherCannon | Warhead@GroundFire |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 22, 90 | RA2LasherCannon | Warhead@MediumChemicalWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 23, 148 | RA2LasherCannon | Warhead@TankDestroyerCannonPercentage |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 27, 186 | RA2LasherCannon | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 146, 210 | RA2LasherCannon | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 172, 180 | RA2LasherCannon | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 173, 183 | RA2LasherCannon | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 174, 196 | RA2LasherCannon | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 175, 178 | RA2LasherCannon | Warhead@Concrete |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 176, 205 | RA2LasherCannon | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 177, 191 | RA2LasherCannon | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 220, 248 | RA2LasherCannon_elite | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 221, 339 | RA2LasherCannon_elite | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 223, 279 | RA2LasherCannon_elite | Warhead@LightFlameWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 224, 333 | RA2LasherCannon_elite | Warhead@RA2Scorch |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 225, 273 | RA2LasherCannon_elite | Warhead@GroundFire |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 226, 306 | RA2LasherCannon_elite | Warhead@MediumChemicalWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 227, 366 | RA2LasherCannon_elite | Warhead@TankDestroyerCannonPercentage |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 231, 393 | RA2LasherCannon_elite | Warhead@Concrete |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 233, 402 | RA2LasherCannon_elite | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 240, 395, 405 | RA2LasherCannon_elite | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 242, 392, 409 | RA2LasherCannon_elite | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 364, 418 | RA2LasherCannon_elite | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 390, 396 | RA2LasherCannon_elite | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 391, 399 | RA2LasherCannon_elite | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 394, 413 | RA2LasherCannon_elite | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 427, 485 | RA2LasherToxicMortar | Projectile |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 443, 496 | RA2LasherToxicMortar | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 461, 524 | RA2LasherToxicMortar | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 462, 738 | RA2LasherToxicMortar | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 463, 490 | RA2LasherToxicMortar | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 464, 493 | RA2LasherToxicMortar | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 465, 515 | RA2LasherToxicMortar | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 466, 705 | RA2LasherToxicMortar | Warhead@ShieldHit |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 467, 488 | RA2LasherToxicMortar | Warhead@Concrete |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 468, 710 | RA2LasherToxicMortar | Warhead@ShieldHitEffect |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 469, 713 | RA2LasherToxicMortar | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 470, 549 | RA2LasherToxicMortar | Warhead@HeavyBombPercentage |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 471, 741 | RA2LasherToxicMortar | Warhead@Smudge1 |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 472, 702 | RA2LasherToxicMortar | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 473, 744 | RA2LasherToxicMortar | Warhead@Smudge2 |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 474, 749 | RA2LasherToxicMortar | Warhead@Smudge2RA2 |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 475, 502 | RA2LasherToxicMortar | Warhead@Effect2 |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 477, 624 | RA2LasherToxicMortar | Warhead@LightChemicalWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 478, 675 | RA2LasherToxicMortar | Warhead@MediumChemicalWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 479, 597 | RA2LasherToxicMortar | Warhead@HeavyChemicalWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 480, 754 | RA2LasherToxicMortar | Warhead@TankDestroyerCannonPercentage |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 481, 510 | RA2LasherToxicMortar | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 482, 520 | RA2LasherToxicMortar | Warhead@Glow |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 483, 651 | RA2LasherToxicMortar | Warhead@MediumCannonPercentage |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 484, 574 | RA2LasherToxicMortar | Warhead@HeavyCannonPercentage |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 872, 877, 878 | RA2LasherLaser | Warhead@Laser_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 945, 950, 951 | RA2LasherLaser_elite | Warhead@Laser_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 1052, 1125, 1126 | RA2DiskDrain | Warhead@Tesla_Heavy |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 1097, 1135 | RA2DiskDrain | Warhead@Tesla_Heavy_ExtraDamage |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 1223, 1252 | RA2PsychicJab | Projectile |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 1236, 1390, 1391 | RA2PsychicJab | Warhead@CannonHE_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 1243, 1326 | RA2PsychicJab | Warhead@TankDestroyerCannonPercentage |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 1244, 1259 | RA2PsychicJab | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 1245, 1262 | RA2PsychicJab | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 1246, 1323 | RA2PsychicJab | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 1247, 1267 | RA2PsychicJab | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 1248, 1257 | RA2PsychicJab | Warhead@Concrete |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 1250, 1296 | RA2PsychicJab | Warhead@LightFlameWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 1251, 1272 | RA2PsychicJab | Warhead@FlakWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 1265, 1353, 1384, 1388 | RA2PsychicJab | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 1350, 1357 | RA2PsychicJab | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 1351, 1372 | RA2PsychicJab | Warhead@RA2Scorch |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 1352, 1366 | RA2PsychicJab | Warhead@GroundFire |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 1444, 1451 | RA2PsychicJab_elite | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 1445, 1466 | RA2PsychicJab_elite | Warhead@RA2Scorch |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 1446, 1460 | RA2PsychicJab_elite | Warhead@GroundFire |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 1447, 1478, 1479 | RA2PsychicJab_elite | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 1481, 1482 | RA2PsychicJab_elite | Warhead@CannonHE_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 1676, 1685 | YRBoomerTorpedo | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 1681, 1689, 1690 | YRBoomerTorpedo | Warhead@MissileAP_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 1758, 1772, 1773 | YuriGatlingCannonMG1 | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 1826, 1840, 1841 | YuriGatlingCannonMG1_AA | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 1894, 1908, 1909 | YuriGatlingCannonMG2 | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 1962, 1976, 1977 | YuriGatlingCannonMG2_AA | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 2032, 2033 | RA2GattlingMG3 | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 2091, 2095, 2096 | RA2GattlingMG3_AA | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 2148, 2162, 2163 | YuriGatlingCannonMG3 | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 2216, 2230, 2231 | YuriGatlingCannonMG3_AA | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 2284, 2286, 2287 | YuriGatlingTankMG1 | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 2340, 2342, 2343 | YuriGatlingTankMG1_AA | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 2396, 2398, 2399 | YuriGatlingTankMG2 | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 2452, 2454, 2455 | YuriGatlingTankMG2_AA | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 2508, 2510, 2511 | YuriGatlingTankMG3 | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 2564, 2566, 2567 | YuriGatlingTankMG3_AA | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 2639, 2643, 2644 | RA2GattlingInf | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 2727, 2750 | RA2Virusgun | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 2749, 2754 | RA2Virusgun | Warhead@HeavyChemicalWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/sequences.yaml | 11, 31 | asianalliance_constructionyard | dead |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/sequences.yaml | 1275, 1304 | asianalliance_flametrooper | shoot |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/sequences.yaml | 1288, 1301 | asianalliance_flametrooper | cheer |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 31, 37, 38 | AsianGrenade | Warhead@Concussion_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 94, 95 | AsianGrenade_elite | Warhead@Concussion_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 175, 181, 182 | asianalliance_asianmilitia_grenade | Warhead@Concussion_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 238, 239 | asianalliance_asianmilitia_grenade_elite | Warhead@Concussion_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 296, 304 | AsianTankKillerRocket | Projectile |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 393, 495, 496 | RA2AsianShotgunFanatic1 | Warhead@Demolition_Light |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 443, 450 | RA2AsianShotgunFanatic1 | Projectile |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 446, 465 | RA2AsianShotgunFanatic1 | Warhead@Glow |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 447, 469 | RA2AsianShotgunFanatic1 | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 448, 454 | RA2AsianShotgunFanatic1 | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 449, 457 | RA2AsianShotgunFanatic1 | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 460, 473, 477 | RA2AsianShotgunFanatic1 | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 463, 472, 484 | RA2AsianShotgunFanatic1 | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 474, 481, 491 | RA2AsianShotgunFanatic1 | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 515, 520 | RA2AsianShotgunFanatic2 | Projectile |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 516, 535 | RA2AsianShotgunFanatic2 | Warhead@Glow |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 517, 539 | RA2AsianShotgunFanatic2 | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 518, 524 | RA2AsianShotgunFanatic2 | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 519, 527 | RA2AsianShotgunFanatic2 | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 530, 543, 547 | RA2AsianShotgunFanatic2 | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 533, 542, 554 | RA2AsianShotgunFanatic2 | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 544, 551, 561 | RA2AsianShotgunFanatic2 | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 565, 607 | RA2AsianShotgunFanatic2 | Warhead@Demolition_Light |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 626, 631 | RA2AsianShotgunFanatic3 | Projectile |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 627, 646 | RA2AsianShotgunFanatic3 | Warhead@Glow |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 628, 650 | RA2AsianShotgunFanatic3 | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 629, 635 | RA2AsianShotgunFanatic3 | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 630, 638 | RA2AsianShotgunFanatic3 | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 641, 654, 658 | RA2AsianShotgunFanatic3 | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 644, 653, 665 | RA2AsianShotgunFanatic3 | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 655, 662, 672 | RA2AsianShotgunFanatic3 | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 676, 718 | RA2AsianShotgunFanatic3 | Warhead@Demolition_Light |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 757, 975 | AsianMaidenBow | Projectile |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 952, 964, 1102 | AsianMaidenBow | Warhead@MediumMissilePercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 954, 969, 1025 | AsianMaidenBow | Warhead@FlakWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 961, 971, 1076 | AsianMaidenBow | Warhead@MagicWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 965, 1014 | AsianMaidenBow | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 966, 1017 | AsianMaidenBow | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 967, 1127 | AsianMaidenBow | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 968, 1012 | AsianMaidenBow | Warhead@Concrete |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 970, 1020 | AsianMaidenBow | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 972, 1050 | AsianMaidenBow | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 974, 985 | AsianMaidenBow | Warhead@ArrowWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 1370, 1385 | AsianLynxTankCannon | Projectile |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 1377, 1393 | AsianLynxTankCannon | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 1378, 1482 | AsianLynxTankCannon | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 1380, 1424 | AsianLynxTankCannon | Warhead@LightFlameWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 1381, 1478 | AsianLynxTankCannon | Warhead@RA2Scorch |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 1382, 1418 | AsianLynxTankCannon | Warhead@GroundFire |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 1383, 1451 | AsianLynxTankCannon | Warhead@MediumChemicalWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 1384, 1509 | AsianLynxTankCannon | Warhead@TankDestroyerCannonPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 1388, 1547 | AsianLynxTankCannon | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 1507, 1571 | AsianLynxTankCannon | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 1533, 1541 | AsianLynxTankCannon | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 1534, 1544 | AsianLynxTankCannon | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 1535, 1557 | AsianLynxTankCannon | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 1536, 1539 | AsianLynxTankCannon | Warhead@Concrete |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 1537, 1566 | AsianLynxTankCannon | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 1538, 1552 | AsianLynxTankCannon | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 1580, 1608 | AsianLynxTankCannon_elite | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 1581, 1699 | AsianLynxTankCannon_elite | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 1583, 1639 | AsianLynxTankCannon_elite | Warhead@LightFlameWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 1584, 1693 | AsianLynxTankCannon_elite | Warhead@RA2Scorch |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 1585, 1633 | AsianLynxTankCannon_elite | Warhead@GroundFire |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 1586, 1666 | AsianLynxTankCannon_elite | Warhead@MediumChemicalWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 1587, 1726 | AsianLynxTankCannon_elite | Warhead@TankDestroyerCannonPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 1591, 1753 | AsianLynxTankCannon_elite | Warhead@Concrete |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 1593, 1762 | AsianLynxTankCannon_elite | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 1600, 1755, 1765 | AsianLynxTankCannon_elite | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 1602, 1752, 1769 | AsianLynxTankCannon_elite | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 1724, 1778 | AsianLynxTankCannon_elite | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 1750, 1756 | AsianLynxTankCannon_elite | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 1751, 1759 | AsianLynxTankCannon_elite | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 1754, 1773 | AsianLynxTankCannon_elite | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 1796, 1800, 1801 | AsianLynxMG | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 1855, 1881 | AsianLynxMG_elite | Warhead@Glow |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 1856, 1892 | AsianLynxMG_elite | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 1857, 1862 | AsianLynxMG_elite | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 1858, 1865 | AsianLynxMG_elite | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 1859, 1885 | AsianLynxMG_elite | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 1895, 1896 | AsianLynxMG_elite | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 1961, 1965, 1966 | AsianKamikazeChaingun | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 2023, 2239 | AsianPhotonCannon | Projectile |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 2230, 2328 | AsianPhotonCannon | Warhead@MediumMissilePercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 2231, 2260 | AsianPhotonCannon | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 2232, 2263 | AsianPhotonCannon | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 2233, 2354 | AsianPhotonCannon | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 2234, 2266 | AsianPhotonCannon | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 2235, 2258 | AsianPhotonCannon | Warhead@Concrete |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 2236, 2275 | AsianPhotonCannon | Warhead@FlakWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 2237, 2271 | AsianPhotonCannon | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 2238, 2301 | AsianPhotonCannon | Warhead@MagicWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 2561, 2606 | AsianPunisherAG | Warhead@MissileHE_Medium |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 2813, 2842 | AsianChemical | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 2827, 2993, 2994 | AsianChemical | Warhead@Chemical_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 2833, 2898 | AsianChemical | Warhead@LightChemicalWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 2834, 2925 | AsianChemical | Warhead@MediumChemicalWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 2835, 2871 | AsianChemical | Warhead@HeavyChemicalWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 2836, 2846 | AsianChemical | Warhead@HeavyBombPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 2837, 2980 | AsianChemical | Warhead@Smudge1 |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 2838, 2952 | AsianChemical | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 2839, 2983 | AsianChemical | Warhead@Smudge2 |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 2840, 2988 | AsianChemical | Warhead@Smudge2RA2 |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 2841, 2955 | AsianChemical | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 3051, 3052 | AsianChemical_elite | Warhead@Chemical_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 3150, 3195, 3196 | AsianChemicalBombs | Warhead@Chemical_Heavy |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 3323, 3349 | AsianPulverizerGatling | Projectile |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 3340, 3395, 3396 | AsianPulverizerGatling | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 3344, 3367 | AsianPulverizerGatling | Warhead@Glow |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 3345, 3374 | AsianPulverizerGatling | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 3346, 3353 | AsianPulverizerGatling | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 3347, 3356 | AsianPulverizerGatling | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 3348, 3371 | AsianPulverizerGatling | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 3359, 3378, 3382 | AsianPulverizerGatling | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 3361, 3379, 3386 | AsianPulverizerGatling | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 3365, 3377, 3389 | AsianPulverizerGatling | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 3455, 3583, 3584 | AsianPulverizerMechaGatling | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 3460, 3481 | AsianPulverizerMechaGatling | Warhead@MediumChemicalWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 3461, 3508 | AsianPulverizerMechaGatling | Warhead@MediumMissilePercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 3462, 3537 | AsianPulverizerMechaGatling | Projectile |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 3477, 3547, 3566, 3570 | AsianPulverizerMechaGatling | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 3532, 3562 | AsianPulverizerMechaGatling | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 3533, 3541 | AsianPulverizerMechaGatling | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 3534, 3544 | AsianPulverizerMechaGatling | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 3535, 3559 | AsianPulverizerMechaGatling | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 3536, 3555 | AsianPulverizerMechaGatling | Warhead@Glow |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 3549, 3567, 3574 | AsianPulverizerMechaGatling | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 3553, 3565, 3577 | AsianPulverizerMechaGatling | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 3864, 4115, 4116 | AsianSinglePlasma | Warhead@Plasma_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 3869, 3940 | AsianSinglePlasma | Warhead@LightFlameWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 3870, 4021 | AsianSinglePlasma | Warhead@RA2Scorch |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 3871, 3882 | AsianSinglePlasma | Warhead@GroundFire |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 3872, 3913 | AsianSinglePlasma | Warhead@LightChemicalWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 3873, 3994 | AsianSinglePlasma | Warhead@MediumFlameWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 3874, 3967 | AsianSinglePlasma | Warhead@MediumChemicalWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 3875, 4025 | AsianSinglePlasma | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 3876, 3888 | AsianSinglePlasma | Warhead@HeavyBombPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 3877, 4077 | AsianSinglePlasma | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 4050, 4052, 4067 | AsianSinglePlasma | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 4053, 4056 | AsianSinglePlasma | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 4054, 4059 | AsianSinglePlasma | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 4055, 4062 | AsianSinglePlasma | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 4070, 4102 | AsianSinglePlasma | Warhead@Smudge1 |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 4071, 4105 | AsianSinglePlasma | Warhead@Smudge2 |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 4072, 4110 | AsianSinglePlasma | Warhead@Smudge2RA2 |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 4073, 4081 | AsianSinglePlasma | Warhead@Effect1 |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 4074, 4086 | AsianSinglePlasma | Warhead@Effect2 |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 4177, 4248, 4249 | AsianSinglePlasma_elite | Warhead@Plasma_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 4181, 4196 | AsianSinglePlasma_elite | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 4182, 4185 | AsianSinglePlasma_elite | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 4183, 4188 | AsianSinglePlasma_elite | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 4184, 4191 | AsianSinglePlasma_elite | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 4199, 4230 | AsianSinglePlasma_elite | Warhead@Smudge1 |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 4200, 4233 | AsianSinglePlasma_elite | Warhead@Smudge2 |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 4201, 4238 | AsianSinglePlasma_elite | Warhead@Smudge2RA2 |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 4202, 4211 | AsianSinglePlasma_elite | Warhead@Effect1 |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 4203, 4216 | AsianSinglePlasma_elite | Warhead@Effect2 |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 4206, 4243 | AsianSinglePlasma_elite | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 4316, 4387, 4388 | AsianTwinPlasma | Warhead@Plasma_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 4320, 4335 | AsianTwinPlasma | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 4321, 4324 | AsianTwinPlasma | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 4322, 4327 | AsianTwinPlasma | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 4323, 4330 | AsianTwinPlasma | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 4338, 4369 | AsianTwinPlasma | Warhead@Smudge1 |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 4339, 4372 | AsianTwinPlasma | Warhead@Smudge2 |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 4340, 4377 | AsianTwinPlasma | Warhead@Smudge2RA2 |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 4341, 4350 | AsianTwinPlasma | Warhead@Effect1 |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 4342, 4355 | AsianTwinPlasma | Warhead@Effect2 |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 4345, 4382 | AsianTwinPlasma | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 4449, 4520, 4521 | AsianTwinPlasma_elite | Warhead@Plasma_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 4453, 4468 | AsianTwinPlasma_elite | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 4454, 4457 | AsianTwinPlasma_elite | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 4455, 4460 | AsianTwinPlasma_elite | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 4456, 4463 | AsianTwinPlasma_elite | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 4471, 4502 | AsianTwinPlasma_elite | Warhead@Smudge1 |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 4472, 4505 | AsianTwinPlasma_elite | Warhead@Smudge2 |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 4473, 4510 | AsianTwinPlasma_elite | Warhead@Smudge2RA2 |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 4474, 4483 | AsianTwinPlasma_elite | Warhead@Effect1 |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 4475, 4488 | AsianTwinPlasma_elite | Warhead@Effect2 |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 4478, 4515 | AsianTwinPlasma_elite | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 4591, 4613, 4614 | AsianTurretPlasma | Warhead@Plasma_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 4595, 4610 | AsianTurretPlasma | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 4596, 4599 | AsianTurretPlasma | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 4597, 4602 | AsianTurretPlasma | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 4598, 4605 | AsianTurretPlasma | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 4677, 4722, 4903 | AsianHarbingerPlasma | Projectile |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 4698, 4725 | AsianHarbingerPlasma | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 4709, 4920, 4921 | AsianHarbingerPlasma | Warhead@Plasma_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 4714, 4787 | AsianHarbingerPlasma | Warhead@LightFlameWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 4715, 4868 | AsianHarbingerPlasma | Warhead@RA2Scorch |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 4716, 4729 | AsianHarbingerPlasma | Warhead@GroundFire |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 4717, 4760 | AsianHarbingerPlasma | Warhead@LightChemicalWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 4718, 4841 | AsianHarbingerPlasma | Warhead@MediumFlameWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 4719, 4814 | AsianHarbingerPlasma | Warhead@MediumChemicalWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 4720, 4872 | AsianHarbingerPlasma | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 4721, 4735 | AsianHarbingerPlasma | Warhead@HeavyBombPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 4897, 4899, 4917 | AsianHarbingerPlasma | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 4900, 4906 | AsianHarbingerPlasma | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 4901, 4909 | AsianHarbingerPlasma | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 4902, 4912 | AsianHarbingerPlasma | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 5203, 5210 | AsianIonBeamMiniStart | Projectile |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 5226, 5238 | AsianIonBeamMini | Projectile |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 5249, 5261 | AsianIonbeam | Projectile |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 5377, 5388, 5389 | AsianPelicanMissile | Warhead@MissileAP_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 5381, 5383 | AsianPelicanMissile | Projectile |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 5454, 5458, 5459 | AsianPelicanMissile_elite | Warhead@MissileAP_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 5527, 5539 | AsianPelicanMG | Projectile |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 5530, 5585, 5586 | AsianPelicanMG | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 5534, 5558 | AsianPelicanMG | Warhead@Glow |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 5535, 5565 | AsianPelicanMG | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 5536, 5543 | AsianPelicanMG | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 5537, 5546 | AsianPelicanMG | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 5538, 5562 | AsianPelicanMG | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 5549, 5568, 5572 | AsianPelicanMG | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 5551, 5569, 5576 | AsianPelicanMG | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 5555, 5579 | AsianPelicanMG | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 5641, 5651 | AsianPelicanMG_elite | Projectile |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 5642, 5697, 5698 | AsianPelicanMG_elite | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 5646, 5670 | AsianPelicanMG_elite | Warhead@Glow |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 5647, 5677 | AsianPelicanMG_elite | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 5648, 5655 | AsianPelicanMG_elite | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 5649, 5658 | AsianPelicanMG_elite | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 5650, 5674 | AsianPelicanMG_elite | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 5661, 5680, 5684 | AsianPelicanMG_elite | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 5663, 5681, 5688 | AsianPelicanMG_elite | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 5667, 5691 | AsianPelicanMG_elite | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 5862, 5878 | AsianSubmarineBomb | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 5865, 5883, 5884 | AsianSubmarineBomb | Warhead@Demolition_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 5869, 5880 | AsianSubmarineBomb | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 5870, 5872 | AsianSubmarineBomb | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 5871, 5875 | AsianSubmarineBomb | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 5955, 6067, 6068 | AsianPhoenixRocket | Warhead@Flame_Medium |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 6006, 6018, 6064 | AsianPhoenixRocket | Warhead@RA2Scorch |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 6007, 6012 | AsianPhoenixRocket | Warhead@GroundFire |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 6008, 6035, 6065 | AsianPhoenixRocket | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 6021, 6029 | AsianPhoenixRocket | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 6022, 6032 | AsianPhoenixRocket | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 6023, 6054 | AsianPhoenixRocket | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 6024, 6040 | AsianPhoenixRocket | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 6025, 6027 | AsianPhoenixRocket | Warhead@Concrete |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 6026, 6045 | AsianPhoenixRocket | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 6096, 6108, 6154 | AsianPhoenixRocket_elite | Warhead@RA2Scorch |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 6097, 6102 | AsianPhoenixRocket_elite | Warhead@GroundFire |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 6098, 6125, 6155 | AsianPhoenixRocket_elite | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 6111, 6119 | AsianPhoenixRocket_elite | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 6112, 6122 | AsianPhoenixRocket_elite | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 6113, 6144 | AsianPhoenixRocket_elite | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 6114, 6130 | AsianPhoenixRocket_elite | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 6115, 6117 | AsianPhoenixRocket_elite | Warhead@Concrete |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 6116, 6135 | AsianPhoenixRocket_elite | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 6157, 6199 | AsianPhoenixRocket_elite | Warhead@Flame_Medium |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 6225, 6229, 6230 | ASDFGun | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 6293, 6300, 6301 | ASDFGun2 | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 6388, 6414 | AsianSniper | Projectile |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 6403, 6459 | AsianSniper | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 6410, 6491 | AsianSniper | Warhead@MediumMissilePercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 6411, 6520 | AsianSniper | Warhead@SmallArmsPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 6412, 6466 | AsianSniper | Warhead@FlakWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 6413, 6432 | AsianSniper | Warhead@ChaingunPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 6696, 6762, 6817 | AsianMLRS | Projectile |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 6710, 6821 | AsianMLRS | Warhead@MissileAP_Medium |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 6760, 6793 | AsianMLRS | Warhead@HeavyMissilePercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 6761, 6769 | AsianMLRS | Warhead@FlakWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 6852, 6904 | AsianSpitfireRockets | Projectile |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 6854, 6906 | AsianSpitfireRockets | Warhead@MissileAP_Medium |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 6941, 6946, 6947 | asianalliance_fanatic_shotgun | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 6999, 7007, 7008 | asianalliance_fanatic_shotgun_upgrade | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 7060, 7067, 7068 | asianalliance_fanatic_shotgun_elite | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 20, 99, 100 | SteelCloneGun | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 24, 26, 43 | SteelCloneGun | Projectile |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 32, 85, 87 | SteelCloneGun | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 35, 69, 86, 90 | SteelCloneGun | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 37, 75 | SteelCloneGun | Warhead@Glow |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 38, 82 | SteelCloneGun | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 39, 63 | SteelCloneGun | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 40, 66 | SteelCloneGun | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 41, 79 | SteelCloneGun | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 42, 61 | SteelCloneGun | Warhead@Concrete |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 72, 93 | SteelCloneGun | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 155, 226, 227 | SteelCloneGun_elite | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 159, 197 | SteelCloneGun_elite | Warhead@Glow |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 160, 204 | SteelCloneGun_elite | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 161, 185 | SteelCloneGun_elite | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 162, 188 | SteelCloneGun_elite | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 163, 201 | SteelCloneGun_elite | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 164, 183 | SteelCloneGun_elite | Warhead@Concrete |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 191, 208, 212, 224 | SteelCloneGun_elite | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 194, 215 | SteelCloneGun_elite | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 207, 209, 221 | SteelCloneGun_elite | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 287, 358, 359 | SteelCloneGunResonance | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 291, 329 | SteelCloneGunResonance | Warhead@Glow |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 292, 336 | SteelCloneGunResonance | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 293, 317 | SteelCloneGunResonance | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 294, 320 | SteelCloneGunResonance | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 295, 333 | SteelCloneGunResonance | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 296, 315 | SteelCloneGunResonance | Warhead@Concrete |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 323, 340, 344, 356 | SteelCloneGunResonance | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 326, 347 | SteelCloneGunResonance | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 339, 341, 353 | SteelCloneGunResonance | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 415, 486, 487 | SteelCloneGunResonanceBounce1 | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 419, 457 | SteelCloneGunResonanceBounce1 | Warhead@Glow |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 420, 464 | SteelCloneGunResonanceBounce1 | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 421, 445 | SteelCloneGunResonanceBounce1 | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 422, 448 | SteelCloneGunResonanceBounce1 | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 423, 461 | SteelCloneGunResonanceBounce1 | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 424, 443 | SteelCloneGunResonanceBounce1 | Warhead@Concrete |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 451, 468, 472, 484 | SteelCloneGunResonanceBounce1 | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 454, 475 | SteelCloneGunResonanceBounce1 | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 467, 469, 481 | SteelCloneGunResonanceBounce1 | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 542, 613, 614 | SteelCloneGunResonanceBounce2 | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 546, 584 | SteelCloneGunResonanceBounce2 | Warhead@Glow |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 547, 591 | SteelCloneGunResonanceBounce2 | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 548, 572 | SteelCloneGunResonanceBounce2 | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 549, 575 | SteelCloneGunResonanceBounce2 | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 550, 588 | SteelCloneGunResonanceBounce2 | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 551, 570 | SteelCloneGunResonanceBounce2 | Warhead@Concrete |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 578, 595, 599, 611 | SteelCloneGunResonanceBounce2 | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 581, 602 | SteelCloneGunResonanceBounce2 | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 594, 596, 608 | SteelCloneGunResonanceBounce2 | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 674, 745, 746 | SteelCloneGunResonance_elite | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 678, 716 | SteelCloneGunResonance_elite | Warhead@Glow |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 679, 723 | SteelCloneGunResonance_elite | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 680, 704 | SteelCloneGunResonance_elite | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 681, 707 | SteelCloneGunResonance_elite | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 682, 720 | SteelCloneGunResonance_elite | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 683, 702 | SteelCloneGunResonance_elite | Warhead@Concrete |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 710, 727, 731, 743 | SteelCloneGunResonance_elite | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 713, 734 | SteelCloneGunResonance_elite | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 726, 728, 740 | SteelCloneGunResonance_elite | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 802, 873, 874 | SteelCloneGunResonanceBounce1_elite | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 806, 844 | SteelCloneGunResonanceBounce1_elite | Warhead@Glow |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 807, 851 | SteelCloneGunResonanceBounce1_elite | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 808, 832 | SteelCloneGunResonanceBounce1_elite | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 809, 835 | SteelCloneGunResonanceBounce1_elite | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 810, 848 | SteelCloneGunResonanceBounce1_elite | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 811, 830 | SteelCloneGunResonanceBounce1_elite | Warhead@Concrete |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 838, 855, 859, 871 | SteelCloneGunResonanceBounce1_elite | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 841, 862 | SteelCloneGunResonanceBounce1_elite | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 854, 856, 868 | SteelCloneGunResonanceBounce1_elite | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 929, 1000, 1001 | SteelCloneGunResonanceBounce2_elite | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 933, 971 | SteelCloneGunResonanceBounce2_elite | Warhead@Glow |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 934, 978 | SteelCloneGunResonanceBounce2_elite | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 935, 959 | SteelCloneGunResonanceBounce2_elite | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 936, 962 | SteelCloneGunResonanceBounce2_elite | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 937, 975 | SteelCloneGunResonanceBounce2_elite | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 938, 957 | SteelCloneGunResonanceBounce2_elite | Warhead@Concrete |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 965, 982, 986, 998 | SteelCloneGunResonanceBounce2_elite | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 968, 989 | SteelCloneGunResonanceBounce2_elite | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 981, 983, 995 | SteelCloneGunResonanceBounce2_elite | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 1059, 1068, 1080 | SteelVulcan | Projectile |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 1061, 1107, 1108 | SteelVulcan | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 1066, 1073 | SteelVulcan | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 1075, 1097 | SteelVulcan | Warhead@Glow |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 1076, 1104 | SteelVulcan | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 1077, 1084 | SteelVulcan | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 1078, 1087 | SteelVulcan | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 1079, 1101 | SteelVulcan | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 1168, 1190 | SteelVulcanResonance | Warhead@Glow |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 1169, 1197 | SteelVulcanResonance | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 1170, 1177 | SteelVulcanResonance | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 1171, 1180 | SteelVulcanResonance | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 1172, 1194 | SteelVulcanResonance | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 1187, 1202 | SteelVulcanResonance | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 1208, 1209 | SteelVulcanResonance | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 1265, 1287 | SteelVulcanResonanceBounce1 | Warhead@Glow |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 1266, 1294 | SteelVulcanResonanceBounce1 | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 1267, 1274 | SteelVulcanResonanceBounce1 | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 1268, 1277 | SteelVulcanResonanceBounce1 | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 1269, 1291 | SteelVulcanResonanceBounce1 | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 1284, 1299 | SteelVulcanResonanceBounce1 | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 1305, 1306 | SteelVulcanResonanceBounce1 | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 1361, 1383 | SteelVulcanResonanceBounce2 | Warhead@Glow |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 1362, 1390 | SteelVulcanResonanceBounce2 | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 1363, 1370 | SteelVulcanResonanceBounce2 | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 1364, 1373 | SteelVulcanResonanceBounce2 | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 1365, 1387 | SteelVulcanResonanceBounce2 | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 1380, 1395 | SteelVulcanResonanceBounce2 | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 1401, 1402 | SteelVulcanResonanceBounce2 | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 1463, 1489, 1529 | ConsortiumMissileSystem | Projectile |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 1481, 1539, 1540 | ConsortiumMissileSystem | Warhead@MissileAA_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 1488, 1496 | ConsortiumMissileSystem | Warhead@ArrowWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 1526, 1537 | ConsortiumMissileSystem | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 1616, 1618 | ConsortiumMissileSystem_EMP | Projectile |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 1652, 1685, 1693 | SteelQuantumCannon | Warhead@Quantum_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 1658, 1686, 1687 | SteelQuantumCannon | Warhead@Laser_Heavy_ExtraDamage |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 1784, 1817, 1825 | SteelQuantumCannon_EMP | Warhead@Quantum_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 1790, 1818, 1819 | SteelQuantumCannon_EMP | Warhead@Laser_Heavy_ExtraDamage |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 1915, 1948, 1956 | SteelQuantumCannon_elite | Warhead@Quantum_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 1921, 1949, 1950 | SteelQuantumCannon_elite | Warhead@Laser_Heavy_ExtraDamage |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 2044, 2077, 2085 | SteelQuantumCannonScatter_elite | Warhead@Quantum_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 2050, 2078, 2079 | SteelQuantumCannonScatter_elite | Warhead@Laser_Heavy_ExtraDamage |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 2159, 2221 | bfg10kCannon | Projectile |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 2220, 2237 | bfg10kCannon | Warhead@MagicWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 2319, 2343, 2393 | SteelMakoGun | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 2325, 2395, 2426 | SteelMakoGun | Warhead@Railgun_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 2333, 2337, 2384 | SteelMakoGun | Projectile |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 2334, 2354 | SteelMakoGun | Warhead@MediumFlameWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 2335, 2381 | SteelMakoGun | Warhead@RA2Scorch |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 2336, 2348 | SteelMakoGun | Warhead@GroundFire |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 2396, 2420 | SteelMakoGun | Warhead@Railgun_Heavy_ExtraDamage |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 2480, 2498, 2529 | SteelMakoGun_elite | Warhead@Railgun_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 2499, 2523 | SteelMakoGun_elite | Warhead@Railgun_Heavy_ExtraDamage |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 2586, 2703 | SteelMakoGun_EMP | Warhead@MediumFlameWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 2628, 2740, 2741 | SteelMakoGun_EMP | Warhead@Railgun_Heavy_ExtraDamage |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 2654, 2658, 2729 | SteelMakoGun_EMP | Projectile |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 2655, 2675 | SteelMakoGun_EMP | Warhead@LightChemicalWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 2656, 2726 | SteelMakoGun_EMP | Warhead@RA2Scorch |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 2657, 2669 | SteelMakoGun_EMP | Warhead@GroundFire |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 2664, 2738 | SteelMakoGun_EMP | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 2768, 2804, 2805 | SteelMakoGun_EMP_elite | Warhead@Railgun_Heavy_ExtraDamage |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 2819, 2845, 2854 | SteelMantaAG | Projectile |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 2840, 2910, 2911 | SteelMantaAG | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 2847, 2875, 2894, 2898 | SteelMantaAG | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 2849, 2884 | SteelMantaAG | Warhead@Glow |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 2850, 2891 | SteelMantaAG | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 2851, 2869 | SteelMantaAG | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 2852, 2872 | SteelMantaAG | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 2853, 2888 | SteelMantaAG | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 2877, 2895, 2901 | SteelMantaAG | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 2881, 2904 | SteelMantaAG | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 2966, 3003 | SteelManta_AA | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 2975, 3009 | SteelManta_AA | Warhead@Glow |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 2976, 3016 | SteelManta_AA | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 2977, 2995 | SteelManta_AA | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 2978, 2998 | SteelManta_AA | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 2979, 3013 | SteelManta_AA | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 3001, 3019, 3022 | SteelManta_AA | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 3006, 3025 | SteelManta_AA | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 3043, 3109, 3110 | SteelMantaAGResonance | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 3046, 3081 | SteelMantaAGResonance | Warhead@Glow |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 3047, 3088 | SteelMantaAGResonance | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 3048, 3066 | SteelMantaAGResonance | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 3049, 3069 | SteelMantaAGResonance | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 3050, 3085 | SteelMantaAGResonance | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 3072, 3091, 3095, 3107 | SteelMantaAGResonance | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 3074, 3092, 3098 | SteelMantaAGResonance | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 3078, 3101 | SteelMantaAGResonance | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 3166, 3232, 3233 | SteelMantaAGResonanceBounce1 | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 3169, 3204 | SteelMantaAGResonanceBounce1 | Warhead@Glow |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 3170, 3211 | SteelMantaAGResonanceBounce1 | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 3171, 3189 | SteelMantaAGResonanceBounce1 | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 3172, 3192 | SteelMantaAGResonanceBounce1 | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 3173, 3208 | SteelMantaAGResonanceBounce1 | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 3195, 3214, 3218, 3230 | SteelMantaAGResonanceBounce1 | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 3197, 3215, 3221 | SteelMantaAGResonanceBounce1 | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 3201, 3224 | SteelMantaAGResonanceBounce1 | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 3288, 3354, 3355 | SteelMantaAGResonanceBounce2 | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 3291, 3326 | SteelMantaAGResonanceBounce2 | Warhead@Glow |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 3292, 3333 | SteelMantaAGResonanceBounce2 | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 3293, 3311 | SteelMantaAGResonanceBounce2 | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 3294, 3314 | SteelMantaAGResonanceBounce2 | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 3295, 3330 | SteelMantaAGResonanceBounce2 | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 3317, 3336, 3340, 3352 | SteelMantaAGResonanceBounce2 | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 3319, 3337, 3343 | SteelMantaAGResonanceBounce2 | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 3323, 3346 | SteelMantaAGResonanceBounce2 | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 3417, 3451 | SteelMantaAAResonance_AA | Warhead@Glow |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 3418, 3458 | SteelMantaAAResonance_AA | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 3419, 3437 | SteelMantaAAResonance_AA | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 3420, 3440 | SteelMantaAAResonance_AA | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 3421, 3455 | SteelMantaAAResonance_AA | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 3443, 3461, 3464, 3473 | SteelMantaAAResonance_AA | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 3445, 3475 | SteelMantaAAResonance_AA | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 3448, 3467 | SteelMantaAAResonance_AA | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 3486, 3520 | SteelMantaAAResonanceBounce1 | Warhead@Glow |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 3487, 3527 | SteelMantaAAResonanceBounce1 | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 3488, 3506 | SteelMantaAAResonanceBounce1 | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 3489, 3509 | SteelMantaAAResonanceBounce1 | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 3490, 3524 | SteelMantaAAResonanceBounce1 | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 3512, 3530, 3533, 3542 | SteelMantaAAResonanceBounce1 | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 3514, 3544 | SteelMantaAAResonanceBounce1 | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 3517, 3536 | SteelMantaAAResonanceBounce1 | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 3554, 3588 | SteelMantaAAResonanceBounce2 | Warhead@Glow |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 3555, 3595 | SteelMantaAAResonanceBounce2 | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 3556, 3574 | SteelMantaAAResonanceBounce2 | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 3557, 3577 | SteelMantaAAResonanceBounce2 | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 3558, 3592 | SteelMantaAAResonanceBounce2 | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 3580, 3598, 3601, 3610 | SteelMantaAAResonanceBounce2 | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 3582, 3612 | SteelMantaAAResonanceBounce2 | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 3585, 3604 | SteelMantaAAResonanceBounce2 | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 3622, 3644 | SteelMantaHunterCannons | Projectile |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 3639, 3688, 3689 | SteelMantaHunterCannons | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 3646, 3649, 3661, 3670 | SteelMantaHunterCannons | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 3648, 3657 | SteelMantaHunterCannons | Warhead@Glow |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 3651, 3664, 3673 | SteelMantaHunterCannons | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 3654, 3663, 3676 | SteelMantaHunterCannons | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 3665, 3680 | SteelMantaHunterCannons | Warhead@ShieldHit |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 3666, 3668 | SteelMantaHunterCannons | Warhead@Concrete |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 3667, 3685 | SteelMantaHunterCannons | Warhead@ShieldHitEffect |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 3751, 3777, 3778 | SteelMantaHunterCannons_AA | Warhead@Flak_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 3759, 3761, 3763, 3766 | SteelMantaHunterCannons_AA | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 3837, 3883, 3884 | SteelMantaHunterCannonsResonance | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 3842, 3851 | SteelMantaHunterCannonsResonance | Warhead@Glow |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 3843, 3855, 3864 | SteelMantaHunterCannonsResonance | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 3845, 3858, 3868 | SteelMantaHunterCannonsResonance | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 3848, 3857, 3871 | SteelMantaHunterCannonsResonance | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 3859, 3875 | SteelMantaHunterCannonsResonance | Warhead@ShieldHit |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 3860, 3862 | SteelMantaHunterCannonsResonance | Warhead@Concrete |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 3861, 3880 | SteelMantaHunterCannonsResonance | Warhead@ShieldHitEffect |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 3940, 3986, 3987 | SteelMantaHunterCannonsResonanceBounce1 | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 3945, 3954 | SteelMantaHunterCannonsResonanceBounce1 | Warhead@Glow |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 3946, 3958, 3967 | SteelMantaHunterCannonsResonanceBounce1 | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 3948, 3961, 3971 | SteelMantaHunterCannonsResonanceBounce1 | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 3951, 3960, 3974 | SteelMantaHunterCannonsResonanceBounce1 | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 3962, 3978 | SteelMantaHunterCannonsResonanceBounce1 | Warhead@ShieldHit |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 3963, 3965 | SteelMantaHunterCannonsResonanceBounce1 | Warhead@Concrete |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 3964, 3983 | SteelMantaHunterCannonsResonanceBounce1 | Warhead@ShieldHitEffect |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 4042, 4088, 4089 | SteelMantaHunterCannonsResonanceBounce2 | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 4047, 4056 | SteelMantaHunterCannonsResonanceBounce2 | Warhead@Glow |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 4048, 4060, 4069 | SteelMantaHunterCannonsResonanceBounce2 | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 4050, 4063, 4073 | SteelMantaHunterCannonsResonanceBounce2 | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 4053, 4062, 4076 | SteelMantaHunterCannonsResonanceBounce2 | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 4064, 4080 | SteelMantaHunterCannonsResonanceBounce2 | Warhead@ShieldHit |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 4065, 4067 | SteelMantaHunterCannonsResonanceBounce2 | Warhead@Concrete |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 4066, 4085 | SteelMantaHunterCannonsResonanceBounce2 | Warhead@ShieldHitEffect |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 4148, 4168, 4169 | SteelMantaHunterCannonsAAResonance_AA | Warhead@Flak_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 4152, 4154, 4157 | SteelMantaHunterCannonsAAResonance_AA | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 4224, 4244, 4245 | SteelMantaHunterCannonsAAResonanceBounce1 | Warhead@Flak_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 4228, 4230, 4233 | SteelMantaHunterCannonsAAResonanceBounce1 | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 4299, 4319, 4320 | SteelMantaHunterCannonsAAResonanceBounce2 | Warhead@Flak_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 4303, 4305, 4308 | SteelMantaHunterCannonsAAResonanceBounce2 | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 4668, 4695, 4705 | SteelInfRailgun | Projectile |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 4691, 4719, 4750 | SteelInfRailgun | Warhead@Railgun_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 4702, 4717 | SteelInfRailgun | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 4720, 4744 | SteelInfRailgun | Warhead@Railgun_Heavy_ExtraDamage |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 4804, 4822, 4853 | SteelInfRailgun_elite | Warhead@Railgun_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 4823, 4847 | SteelInfRailgun_elite | Warhead@Railgun_Heavy_ExtraDamage |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 4917, 4954, 4955 | SteelScalpelRailgunAA | Warhead@Railgun_Heavy_ExtraDamage |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 5366, 5390 | SteelInfRailgun_EMP | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 5368, 5472 | SteelInfRailgun_EMP | Warhead@MediumFlameWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 5369, 5499 | SteelInfRailgun_EMP | Warhead@RA2Scorch |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 5370, 5415 | SteelInfRailgun_EMP | Warhead@GroundFire |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 5371, 5445 | SteelInfRailgun_EMP | Warhead@MediumChemicalWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 5372, 5502 | SteelInfRailgun_EMP | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 5373, 5421 | SteelInfRailgun_EMP | Warhead@MediumCannonPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 5374, 5527 | SteelInfRailgun_EMP | Projectile |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 5387, 5539 | SteelInfRailgun_EMP | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 5541, 5565 | SteelInfRailgun_EMP | Warhead@Railgun_Heavy_ExtraDamage |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 5710, 5734 | SteelInfRailgun_EMP_elite | Warhead@Railgun_Heavy_ExtraDamage |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 5785, 5828, 5829 | SteelScalpelRailgun_EMP_AA | Warhead@Railgun_Heavy_ExtraDamage |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 5845, 5914, 5945 | SteelFighterRailgun | Projectile |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 5883, 5952, 5980 | SteelFighterRailgun | Warhead@Laser_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 5888, 5954, 6031 | SteelFighterRailgun | Warhead@Railgun_Heavy_ExtraDamage |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 5913, 5921 | SteelFighterRailgun | Warhead@TankDestroyerCannonPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 5953, 5955 | SteelFighterRailgun | Warhead@LaserExtraDamage_Auxiliary |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 6042, 6072 | SteelMegaSword | Projectile |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 6065, 6102 | SteelMegaSword | Warhead@ShieldHit |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 6070, 6074 | SteelMegaSword | Warhead@LightChemicalWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 6071, 6106 | SteelMegaSword | Warhead@SwordWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 6139, 6176 | SteelMegaSword_EMP | Projectile |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 6160, 6178 | SteelMegaSword_EMP | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 6175, 6182 | SteelMegaSword_EMP | Warhead@HeavyChemicalWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 6402, 6413 | SteelFortressWeapons | Projectile |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 6448, 6479 | SteelQuantumTurretRail | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 6458, 6546, 6547 | SteelQuantumTurretRail | Warhead@Quantum_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 6467, 6472 | SteelQuantumTurretRail | Projectile |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 6469, 6489 | SteelQuantumTurretRail | Warhead@HeavyChemicalWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 6470, 6517 | SteelQuantumTurretRail | Warhead@HeavyFlameWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 6471, 6483 | SteelQuantumTurretRail | Warhead@GroundFire |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 6650, 6659, 6660 | SteelQuantumTurretRail_EMP | Warhead@Quantum_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 6778, 6832 | SteelInspectorIonCannonDamage | Warhead@Tesla_Heavy_ExtraDamage |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 6849, 6852 | SteelHoverMissile | Projectile |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 6899, 6903, 6904 | SteelKatyCannons | Warhead@CannonFire_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 6962, 6966, 6967 | SteelKatyCannons_elite | Warhead@CannonFire_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 7029, 7099 | SteelKatyCannons_EMP | Projectile |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 7098, 7101 | SteelKatyCannons_EMP | Warhead@TankDestroyerCannonPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 7298, 7320 | WhiteRabbitGatling | Projectile |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 7311, 7378 | WhiteRabbitGatling | Warhead@HeavyCannonPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 7312, 7452 | WhiteRabbitGatling | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 7313, 7336 | WhiteRabbitGatling | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 7314, 7339 | WhiteRabbitGatling | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 7315, 7425 | WhiteRabbitGatling | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 7316, 7374 | WhiteRabbitGatling | Warhead@Glow |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 7317, 7428 | WhiteRabbitGatling | Warhead@SmallArmsPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 7318, 7401 | WhiteRabbitGatling | Warhead@MediumMissilePercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 7319, 7350 | WhiteRabbitGatling | Warhead@FlakWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 7342, 7456, 7460 | WhiteRabbitGatling | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 7344, 7457, 7464 | WhiteRabbitGatling | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 7348, 7455, 7467 | WhiteRabbitGatling | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 7584, 7588, 7589 | HammerheadArtillery | Warhead@CannonHE_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 7650, 7678, 7748 | SteelDaggerCannon | Projectile |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 7662, 7683, 7750 | SteelDaggerCannon | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 7669, 7752, 7753 | SteelDaggerCannon | Warhead@CannonHE_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 7674, 7693 | SteelDaggerCannon | Warhead@MediumFlameWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 7675, 7720 | SteelDaggerCannon | Warhead@RA2Scorch |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 7676, 7687 | SteelDaggerCannon | Warhead@GroundFire |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 7677, 7723 | SteelDaggerCannon | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 7811, 7812 | SteelDaggerCannon_elite | Warhead@CannonHE_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 7870, 7889, 7896 | SteelCruiserArtillery | Projectile |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 7885, 7908, 7909 | SteelCruiserArtillery | Warhead@CannonHE_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 7894, 7905 | SteelCruiserArtillery | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 7895, 7900 | SteelCruiserArtillery | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 7966, 7995, 7996 | SteelCruiserArtillery_elite | Warhead@CannonHE_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 7970, 7981 | SteelCruiserArtillery_elite | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 7971, 7976 | SteelCruiserArtillery_elite | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 8054, 8074 | SteelCruiserCannons | Projectile |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 8067, 8128 | SteelCruiserCannons | Warhead@LightMissilePercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 8068, 8179 | SteelCruiserCannons | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 8069, 8091 | SteelCruiserCannons | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 8070, 8152 | SteelCruiserCannons | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 8071, 8094 | SteelCruiserCannons | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 8072, 8155 | SteelCruiserCannons | Warhead@SmallArmsPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 8073, 8104 | SteelCruiserCannons | Warhead@FlakWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 8097, 8184, 8188 | SteelCruiserCannons | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 8100, 8185, 8191 | SteelCruiserCannons | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 8183, 8194, 8200 | SteelCruiserCannons | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 8207, 8218, 8224 | SteelCruiserCannons_elite | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 8208, 8212 | SteelCruiserCannons_elite | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 8209, 8215 | SteelCruiserCannons_elite | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 8234, 8254 | SteelCargoshipCannons | Projectile |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 8250, 8258, 8259 | SteelCargoshipCannons | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 15, 19, 20 | ACV_Machinegun | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 144, 254, 255 | Future_Cryocopter_Rocket | Warhead@MissileAP_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 166, 252 | Future_Cryocopter_Rocket | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 179, 183, 247 | Future_Cryocopter_Rocket | Projectile |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 181, 222 | Future_Cryocopter_Rocket | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 182, 191 | Future_Cryocopter_Rocket | Warhead@FutureCryocopterMissileAP_MediumFriendly |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 314, 403 | Future_Cryocopter_Cryo | Projectile |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 395, 487 | Future_Cryocopter_Cryo | Warhead@LightMissilePercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 396, 452 | Future_Cryocopter_Cryo | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 397, 512 | Future_Cryocopter_Cryo | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 398, 455 | Future_Cryocopter_Cryo | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 399, 515 | Future_Cryocopter_Cryo | Warhead@SmallArmsPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 400, 458 | Future_Cryocopter_Cryo | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 401, 427 | Future_Cryocopter_Cryo | Warhead@ChaingunPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 402, 462 | Future_Cryocopter_Cryo | Warhead@FlakWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 570, 668 | CryoLegionnaireAttack | Projectile |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 660, 753 | CryoLegionnaireAttack | Warhead@LightMissilePercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 661, 717 | CryoLegionnaireAttack | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 662, 779 | CryoLegionnaireAttack | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 663, 720 | CryoLegionnaireAttack | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 664, 782 | CryoLegionnaireAttack | Warhead@SmallArmsPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 665, 723 | CryoLegionnaireAttack | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 666, 691 | CryoLegionnaireAttack | Warhead@ChaingunPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 667, 727 | CryoLegionnaireAttack | Warhead@FlakWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 843, 852 | FutureMicrotorpedos | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 848, 856, 857 | FutureMicrotorpedos | Warhead@MissileAP_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 914, 1012 | AthenaLaser | Projectile |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 1004, 1098 | AthenaLaser | Warhead@LightMissilePercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 1005, 1062 | AthenaLaser | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 1006, 1124 | AthenaLaser | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 1007, 1065 | AthenaLaser | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 1008, 1127 | AthenaLaser | Warhead@SmallArmsPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 1009, 1068 | AthenaLaser | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 1010, 1036 | AthenaLaser | Warhead@ChaingunPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 1011, 1072 | AthenaLaser | Warhead@FlakWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 1243, 1255, 1256 | Future_MultiMissile | Warhead@MissileAP_Light_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 1247, 1250 | Future_MultiMissile | Projectile |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 1415, 1465 | Future_MultiMissile_Sigma | Projectile |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 1454, 1523 | Future_MultiMissile_Sigma | Warhead@MagicWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 1455, 1603 | Future_MultiMissile_Sigma | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 1456, 1499 | Future_MultiMissile_Sigma | Warhead@FlakWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 1457, 1494 | Future_MultiMissile_Sigma | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 1458, 1489 | Future_MultiMissile_Sigma | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 1459, 1481 | Future_MultiMissile_Sigma | Warhead@Concrete |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 1460, 1578 | Future_MultiMissile_Sigma | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 1461, 1483 | Future_MultiMissile_Sigma | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 1462, 1486 | Future_MultiMissile_Sigma | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 1463, 1549 | Future_MultiMissile_Sigma | Warhead@MediumMissilePercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 1464, 1573 | Future_MultiMissile_Sigma | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 1619, 1629, 1712 | FutureEnforcerShotgun | Warhead@ShotgunGrenadeAlly |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 1621, 1630, 1743 | FutureEnforcerShotgun | Warhead@ShotgunShrapnelAlly |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 1623, 1631 | FutureEnforcerShotgun | Projectile |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 1624, 1682 | FutureEnforcerShotgun | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 1625, 1774 | FutureEnforcerShotgun | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 1626, 1825 | FutureEnforcerShotgun | Warhead@TankDestroyerCannonPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 1627, 1800 | FutureEnforcerShotgun | Warhead@SmallArmsPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 1628, 1643 | FutureEnforcerShotgun | Warhead@ChaingunPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 2020, 2024, 2025 | Future_Wheel_MG | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 2082, 2089 | Future_Wheel_MG_elite | Projectile |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 2085, 2096, 2097 | Future_Wheel_MG_elite | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 2159, 2169, 2286 | ShotgunAttackRobotGun | Projectile |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 2160, 2328, 2329 | ShotgunAttackRobotGun | Warhead@Bullet_Light_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 2165, 2179 | ShotgunAttackRobotGun | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 2166, 2205 | ShotgunAttackRobotGun | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 2167, 2256 | ShotgunAttackRobotGun | Warhead@TankDestroyerCannonPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 2168, 2231 | ShotgunAttackRobotGun | Warhead@SmallArmsPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 2173, 2311, 2315 | ShotgunAttackRobotGun | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 2176, 2281, 2302 | ShotgunAttackRobotGun | Warhead@Glow |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 2282, 2307 | ShotgunAttackRobotGun | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 2283, 2290 | ShotgunAttackRobotGun | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 2284, 2293 | ShotgunAttackRobotGun | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 2285, 2304 | ShotgunAttackRobotGun | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 2296, 2312, 2319 | ShotgunAttackRobotGun | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 2300, 2310, 2322 | ShotgunAttackRobotGun | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 2389, 2565, 2566 | ShotgunAttackRobotGun_elite | Warhead@Bullet_Light_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 2392, 2406, 2425 | ShotgunAttackRobotGun_elite | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 2395, 2407, 2449 | ShotgunAttackRobotGun_elite | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 2398, 2408, 2496 | ShotgunAttackRobotGun_elite | Warhead@TankDestroyerCannonPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 2401, 2409, 2473 | ShotgunAttackRobotGun_elite | Warhead@SmallArmsPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 2405, 2410, 2524 | ShotgunAttackRobotGun_elite | Projectile |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 2418, 2549, 2553 | ShotgunAttackRobotGun_elite | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 2422, 2519, 2540 | ShotgunAttackRobotGun_elite | Warhead@Glow |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 2520, 2545 | ShotgunAttackRobotGun_elite | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 2521, 2528 | ShotgunAttackRobotGun_elite | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 2522, 2531 | ShotgunAttackRobotGun_elite | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 2523, 2542 | ShotgunAttackRobotGun_elite | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 2534, 2550, 2556 | ShotgunAttackRobotGun_elite | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 2538, 2548, 2559 | ShotgunAttackRobotGun_elite | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 2622, 2644, 2716 | CannonAttackRobotGun | Projectile |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 2629, 2650 | CannonAttackRobotGun | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 2635, 2723, 2748 | CannonAttackRobotGun | Warhead@CannonHE_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 2639, 2692 | CannonAttackRobotGun | Warhead@TankDestroyerCannonPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 2641, 2662 | CannonAttackRobotGun | Warhead@MediumFlameWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 2642, 2689 | CannonAttackRobotGun | Warhead@RA2Scorch |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 2643, 2656 | CannonAttackRobotGun | Warhead@GroundFire |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 2724, 2797 | CannonAttackRobotGun | Warhead@Railgun_Heavy_ExtraDamage |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 2815, 2840 | CannonAttackRobotGun_elite | Warhead@CannonHE_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 2816, 2889 | CannonAttackRobotGun_elite | Warhead@Railgun_Heavy_ExtraDamage |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 2908, 2914, 2915 | MissileAttackRobotGun | Warhead@MissileAP_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 2976, 2980, 2981 | MissileAttackRobotGun_elite | Warhead@MissileAP_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 3039, 3061 | FutureMechGatling | Projectile |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 3053, 3149 | FutureMechGatling | Warhead@TankDestroyerCannonPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 3054, 3146 | FutureMechGatling | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 3055, 3077 | FutureMechGatling | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 3056, 3080 | FutureMechGatling | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 3057, 3143 | FutureMechGatling | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 3058, 3115 | FutureMechGatling | Warhead@Glow |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 3059, 3119 | FutureMechGatling | Warhead@LightMissilePercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 3060, 3091 | FutureMechGatling | Warhead@FlakWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 3083, 3174, 3178 | FutureMechGatling | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 3085, 3175, 3182 | FutureMechGatling | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 3089, 3173, 3185 | FutureMechGatling | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 3201, 3234 | FutureMechPlasma | Projectile |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 3214, 3238 | FutureMechPlasma | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 3227, 3294, 3295 | FutureMechPlasma | Warhead@Plasma_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 3231, 3242 | FutureMechPlasma | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 3233, 3267 | FutureMechPlasma | Warhead@MediumChemicalWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 3353, 3357, 3358 | FutureMechPlasma_elite | Warhead@Plasma_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 3477, 3525 | FutureTankCannons | Projectile |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 3505, 3537 | FutureTankCannons | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 3511, 3656, 3657 | FutureTankCannons | Warhead@CannonHE_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 3519, 3601 | FutureTankCannons | Warhead@MediumFlameWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 3520, 3628 | FutureTankCannons | Warhead@RA2Scorch |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 3521, 3542 | FutureTankCannons | Warhead@GroundFire |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 3522, 3574 | FutureTankCannons | Warhead@MediumChemicalWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 3523, 3631 | FutureTankCannons | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 3524, 3548 | FutureTankCannons | Warhead@MagicWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 3712, 3714, 3715 | FutureTankCannons_elite | Warhead@CannonHE_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 3778, 3844 | FutureHarbingerCannon | Projectile |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 3816, 3840, 3867 | FutureHarbingerCannon | Warhead@Effect1 |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 3818, 3841, 3871 | FutureHarbingerCannon | Warhead@Effect2 |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 3820, 3862 | FutureHarbingerCannon | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 3833, 3911 | FutureHarbingerCannon | Warhead@HeavyChemicalWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 3834, 3938 | FutureHarbingerCannon | Warhead@HeavyFlameWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 3835, 3880 | FutureHarbingerCannon | Warhead@GroundFire |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 3836, 3886 | FutureHarbingerCannon | Warhead@HeavyBombPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 3837, 4020 | FutureHarbingerCannon | Warhead@Smudge1 |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 3838, 4023 | FutureHarbingerCannon | Warhead@Smudge2 |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 3839, 4028 | FutureHarbingerCannon | Warhead@Smudge2RA2 |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 3842, 3990 | FutureHarbingerCannon | Warhead@MagicWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 3843, 3966 | FutureHarbingerCannon | Warhead@HeavyMissilePercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 4103, 4124 | OrionRailgun | Projectile |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 4120, 4130, 4161 | OrionRailgun | Warhead@Railgun_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 4131, 4155 | OrionRailgun | Warhead@Railgun_Heavy_ExtraDamage |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 4214, 4248 | OrionRailgun_elite | Projectile |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 4229, 4254, 4285 | OrionRailgun_elite | Warhead@Railgun_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml | 4255, 4279 | OrionRailgun_elite | Warhead@Railgun_Heavy_ExtraDamage |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/sequences.yaml | 926, 932 | naxis_slave | cheer |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 28, 34, 35 | NaxBrummbarArty | Warhead@CannonHE_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 89, 103, 104 | NaxBrummbarArty_elite | Warhead@CannonHE_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 93, 101 | NaxBrummbarArty_elite | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 206, 210, 211 | NaxiWW2Machinegun | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 267, 271, 272 | NaxiWW2Machinegun_AA | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 330, 334, 335 | NaxiWW2MachinegunSmall | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 391, 395, 396 | NaxiWW2MachinegunSmall_AA | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 449, 450 | NaxiWW2MachinegunTop | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 508, 512, 513 | NaxiWW2MachinegunTop_AA | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 569, 570 | NaxiWW2Machinegunner | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 629, 630 | NaxiWW2Machinegunner_elite | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 806, 824 | LunarNaxiDroneLaser | Projectile |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 817, 912 | LunarNaxiDroneLaser | Warhead@SmallArmsPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 818, 881, 937 | LunarNaxiDroneLaser | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 819, 848 | LunarNaxiDroneLaser | Warhead@ChaingunPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 820, 885 | LunarNaxiDroneLaser | Warhead@MediumMissilePercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 821, 872 | LunarNaxiDroneLaser | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 822, 875 | LunarNaxiDroneLaser | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 823, 909 | LunarNaxiDroneLaser | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 956, 1029 | LunarNaxiDroneMissile | Projectile |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 972, 1157, 1158 | LunarNaxiDroneMissile | Warhead@MissileAP_Light |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 1023, 1108 | LunarNaxiDroneMissile | Warhead@HeavyMissilePercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 1024, 1084 | LunarNaxiDroneMissile | Warhead@HeavyAAWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 1025, 1132 | LunarNaxiDroneMissile | Warhead@MediumMissilePercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 1026, 1060 | LunarNaxiDroneMissile | Warhead@FlakWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 1028, 1034 | LunarNaxiDroneMissile | Warhead@ArrowWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 1174, 1193 | NaxiMP40_elite | Warhead@Glow |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 1175, 1200 | NaxiMP40_elite | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 1176, 1179 | NaxiMP40_elite | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 1177, 1182 | NaxiMP40_elite | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 1178, 1197 | NaxiMP40_elite | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 1185, 1204, 1208 | NaxiMP40_elite | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 1187, 1205, 1212 | NaxiMP40_elite | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 1191, 1203, 1215 | NaxiMP40_elite | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 1221, 1315 | NaxiMP40_elite | Warhead@Concussion_Light |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 1263, 1305 | NaxiMP40_elite | Warhead@CannonHE_Heavy |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 1377, 1455 | NaxiShrek | Projectile |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 1392, 1650 | NaxiShrek | Warhead@MissileHE_Medium |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 1442, 1509 | NaxiShrek | Warhead@HeavyBombPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 1443, 1592 | NaxiShrek | Warhead@Smudge1 |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 1444, 1595 | NaxiShrek | Warhead@Smudge2 |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 1445, 1600 | NaxiShrek | Warhead@Smudge2RA2 |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 1446, 1465 | NaxiShrek | Warhead@Effect1 |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 1447, 1470 | NaxiShrek | Warhead@Effect2 |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 1449, 1534 | NaxiShrek | Warhead@HeavyFlameWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 1450, 1589, 1647 | NaxiShrek | Warhead@RA2Scorch |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 1451, 1503 | NaxiShrek | Warhead@GroundFire |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 1452, 1478 | NaxiShrek | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 1453, 1605 | NaxiShrek | Warhead@TankDestroyerCannonPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 1454, 1562 | NaxiShrek | Warhead@MediumChemicalWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 1460, 1631 | NaxiShrek | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 1667, 1735 | NaxiShrek_elite | Warhead@MissileHE_Medium |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 1747, 1825 | NaxiShrekCons | Projectile |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 1762, 2017 | NaxiShrekCons | Warhead@MissileHE_Medium |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 1812, 1879 | NaxiShrekCons | Warhead@HeavyBombPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 1813, 1962 | NaxiShrekCons | Warhead@Smudge1 |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 1814, 1965 | NaxiShrekCons | Warhead@Smudge2 |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 1815, 1970 | NaxiShrekCons | Warhead@Smudge2RA2 |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 1816, 1835 | NaxiShrekCons | Warhead@Effect1 |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 1817, 1840 | NaxiShrekCons | Warhead@Effect2 |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 1819, 1904 | NaxiShrekCons | Warhead@HeavyFlameWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 1820, 1959 | NaxiShrekCons | Warhead@RA2Scorch |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 1821, 1873 | NaxiShrekCons | Warhead@GroundFire |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 1822, 1848 | NaxiShrekCons | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 1823, 1975 | NaxiShrekCons | Warhead@TankDestroyerCannonPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 1824, 1932 | NaxiShrekCons | Warhead@MediumChemicalWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 1830, 2001 | NaxiShrekCons | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 2034, 2102 | NaxiShrekCons_elite | Warhead@MissileHE_Medium |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 2157, 2163, 2164 | NaxiJadgDestroyer | Warhead@CannonAP_Light_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 2219, 2240, 2241 | NaxiJadgDestroyer_elite | Warhead@CannonAP_Light_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 2225, 2238 | NaxiJadgDestroyer_elite | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 2322, 2356 | NaxMausCannon | Projectile |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 2338, 2547, 2548 | NaxMausCannon | Warhead@CannonHE_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 2344, 2435 | NaxMausCannon | Warhead@LightChemicalWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 2345, 2407 | NaxMausCannon | Warhead@HeavyFlameWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 2346, 2462, 2544 | NaxMausCannon | Warhead@RA2Scorch |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 2347, 2376 | NaxMausCannon | Warhead@GroundFire |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 2348, 2465 | NaxMausCannon | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 2349, 2382 | NaxMausCannon | Warhead@HeavyBombPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 2350, 2490 | NaxMausCannon | Warhead@Smudge1 |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 2351, 2493 | NaxMausCannon | Warhead@Smudge2 |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 2352, 2498 | NaxMausCannon | Warhead@Smudge2RA2 |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 2353, 2363 | NaxMausCannon | Warhead@Effect1 |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 2354, 2368 | NaxMausCannon | Warhead@Effect2 |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 2355, 2503 | NaxMausCannon | Warhead@TankDestroyerCannonPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 2358, 2529 | NaxMausCannon | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 2605, 2708 | NaxMausCannon_elite | Warhead@LightChemicalWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 2606, 2680 | NaxMausCannon_elite | Warhead@HeavyFlameWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 2607, 2735 | NaxMausCannon_elite | Warhead@RA2Scorch |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 2608, 2649 | NaxMausCannon_elite | Warhead@GroundFire |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 2609, 2740 | NaxMausCannon_elite | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 2610, 2655 | NaxMausCannon_elite | Warhead@HeavyBombPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 2611, 2765 | NaxMausCannon_elite | Warhead@Smudge1 |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 2612, 2768 | NaxMausCannon_elite | Warhead@Smudge2 |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 2613, 2773 | NaxMausCannon_elite | Warhead@Smudge2RA2 |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 2614, 2628 | NaxMausCannon_elite | Warhead@Effect1 |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 2615, 2633 | NaxMausCannon_elite | Warhead@Effect2 |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 2616, 2778 | NaxMausCannon_elite | Warhead@TankDestroyerCannonPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 2807, 2808 | NaxMausCannon_elite | Warhead@CannonHE_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 2884, 3013, 3014 | NaxRatteCannon | Warhead@CannonHE_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 2890, 2935 | NaxRatteCannon | Warhead@LightChemicalWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 2891, 2910 | NaxRatteCannon | Warhead@HeavyBombPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 2892, 2962 | NaxRatteCannon | Warhead@Smudge1 |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 2893, 2965 | NaxRatteCannon | Warhead@Smudge2 |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 2894, 2970 | NaxRatteCannon | Warhead@Smudge2RA2 |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 2895, 2897 | NaxRatteCannon | Warhead@Effect1 |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 2896, 2902 | NaxRatteCannon | Warhead@Effect2 |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 2975, 2997 | NaxRatteCannon | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 2976, 3008 | NaxRatteCannon | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 2977, 2982 | NaxRatteCannon | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 2978, 2985 | NaxRatteCannon | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 2979, 2992 | NaxRatteCannon | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 2988, 3011 | NaxRatteCannon | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 3070, 3128 | NaxRatteCannon_elite | Warhead@LightChemicalWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 3071, 3103 | NaxRatteCannon_elite | Warhead@HeavyBombPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 3072, 3157 | NaxRatteCannon_elite | Warhead@Smudge1 |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 3073, 3160 | NaxRatteCannon_elite | Warhead@Smudge2 |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 3074, 3165 | NaxRatteCannon_elite | Warhead@Smudge2RA2 |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 3075, 3082 | NaxRatteCannon_elite | Warhead@Effect1 |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 3076, 3087 | NaxRatteCannon_elite | Warhead@Effect2 |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 3079, 3181, 3194 | NaxRatteCannon_elite | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 3095, 3174, 3183 | NaxRatteCannon_elite | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 3097, 3170, 3187 | NaxRatteCannon_elite | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 3171, 3191 | NaxRatteCannon_elite | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 3172, 3175 | NaxRatteCannon_elite | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 3173, 3178 | NaxRatteCannon_elite | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 3196, 3197 | NaxRatteCannon_elite | Warhead@CannonHE_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 3352, 3369 | NaxQuadCannon | Projectile |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 3360, 3420 | NaxQuadCannon | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 3361, 3533 | NaxQuadCannon | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 3362, 3410 | NaxQuadCannon | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 3363, 3413 | NaxQuadCannon | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 3364, 3445 | NaxQuadCannon | Warhead@HeavyMissilePercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 3365, 3530 | NaxQuadCannon | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 3366, 3386 | NaxQuadCannon | Warhead@ChaingunPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 3367, 3469 | NaxQuadCannon | Warhead@NaxFlakAllyCounted |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 3368, 3499 | NaxQuadCannon | Warhead@NaxFlakAllyUncounted |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 3416, 3536, 3540 | NaxQuadCannon | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 3418, 3537, 3545 | NaxQuadCannon | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 3550, 3560 | NaxQuadCannon | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 3567, 3571 | NaxQuadCannon_elite | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 3568, 3576 | NaxQuadCannon_elite | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 3581, 3591 | NaxQuadCannon_elite | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 3599, 3687 | NaxQuadCannon_AA | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 3601, 3711 | NaxQuadCannon_AA | Warhead@HeavyMissilePercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 3611, 3635, 3734 | NaxQuadCannon_AA | Warhead@NaxFlakAllyCounted |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 3614, 3636, 3762 | NaxQuadCannon_AA | Warhead@NaxFlakAllyUncounted |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 3619, 3685, 3805 | NaxQuadCannon_AA | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 3628, 3634, 3654 | NaxQuadCannon_AA | Warhead@ChaingunPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 3630, 3794 | NaxQuadCannon_AA | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 3631, 3677 | NaxQuadCannon_AA | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 3632, 3680 | NaxQuadCannon_AA | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 3633, 3791 | NaxQuadCannon_AA | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 3683, 3797, 3800 | NaxQuadCannon_AA | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 3809, 3819 | NaxQuadCannon_AA | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 3829, 3832 | NaxQuadCannon_AA_elite | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 3841, 3851 | NaxQuadCannon_AA_elite | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 3859, 3863 | SkyMageCannon | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 3860, 3868 | SkyMageCannon | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 3873, 3883 | SkyMageCannon | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 3891, 3895 | SkyMageCannon_elite | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 3892, 3900 | SkyMageCannon_elite | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 3905, 3915 | SkyMageCannon_elite | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 3927, 3930 | SkyMageCannon_AA | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 3939, 3949 | SkyMageCannon_AA | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 3959, 3962 | SkyMageCannon_AA_elite | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 3971, 3981 | SkyMageCannon_AA_elite | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 3988, 3989 | NaxQuadCannonTargeting | Warhead@1Dam |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 3998, 4001 | PortableFlak | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 4010, 4020 | PortableFlak | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 4028, 4031 | PortableFlak_elite | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 4040, 4050 | PortableFlak_elite | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 4069, 4073 | NaxFlakAG | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 4070, 4078 | NaxFlakAG | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 4083, 4093 | NaxFlakAG | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 4116, 4119 | NaxFlakAA | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 4128, 4138 | NaxFlakAA | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 4172, 4223, 4224 | NaxPlanegun_elite | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 4176, 4195 | NaxPlanegun_elite | Warhead@Glow |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 4177, 4202 | NaxPlanegun_elite | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 4178, 4181 | NaxPlanegun_elite | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 4179, 4184 | NaxPlanegun_elite | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 4180, 4199 | NaxPlanegun_elite | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 4187, 4206, 4210 | NaxPlanegun_elite | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 4189, 4207, 4214 | NaxPlanegun_elite | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 4193, 4205, 4217 | NaxPlanegun_elite | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 4282, 4304 | NaxPlaneRockets_elite | Projectile |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 4297, 4333, 4334 | NaxPlaneRockets_elite | Warhead@MissileAP_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 4301, 4315 | NaxPlaneRockets_elite | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 4302, 4318 | NaxPlaneRockets_elite | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 4303, 4321 | NaxPlaneRockets_elite | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 4400, 4406 | NaxInterceptorRockets | Projectile |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 4403, 4417 | NaxInterceptorRockets | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 4404, 4420 | NaxInterceptorRockets | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 4405, 4423 | NaxInterceptorRockets | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 4446, 4497 | NaxiInterceptorGun | Projectile |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 4483, 4592, 4593 | NaxiInterceptorGun | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 4494, 4538 | NaxiInterceptorGun | Warhead@FlakWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 4495, 4502 | NaxiInterceptorGun | Warhead@ChaingunPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 4496, 4567 | NaxiInterceptorGun | Warhead@SmallArmsPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 4660, 4768 | NaxShoeRocket | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 4754, 4778, 4779 | NaxShoeRocket | Warhead@MissileAP_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 4758, 4775 | NaxShoeRocket | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 4759, 4762 | NaxShoeRocket | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 4760, 4765 | NaxShoeRocket | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 4761, 4770 | NaxShoeRocket | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 4927, 4928 | NaxiAlienPistol_elite | Warhead@EMPUnit |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 4970, 5078, 5087 | NaxiMissileUboat | Projectile |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 5074, 5153, 5154 | NaxiMissileUboat | Warhead@MissileAP_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 5082, 5102 | NaxiMissileUboat | Warhead@Smudge1 |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 5083, 5105 | NaxiMissileUboat | Warhead@Smudge2 |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 5084, 5110 | NaxiMissileUboat | Warhead@Smudge2RA2 |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 5085, 5089 | NaxiMissileUboat | Warhead@Effect1 |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 5086, 5094 | NaxiMissileUboat | Warhead@Effect2 |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 5115, 5137 | NaxiMissileUboat | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 5116, 5148 | NaxiMissileUboat | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 5117, 5122 | NaxiMissileUboat | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 5118, 5125 | NaxiMissileUboat | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 5119, 5132 | NaxiMissileUboat | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 5128, 5151 | NaxiMissileUboat | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 30, 57, 58 | schwarzermond_lunarsoldier_rifle | Warhead@Laser_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 47, 51 | schwarzermond_lunarsoldier_rifle | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 113, 129, 130 | schwarzermond_lunarsoldier_rifle_elite | Warhead@Laser_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 118, 119 | schwarzermond_lunarsoldier_rifle_elite | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 120, 123 | schwarzermond_lunarsoldier_rifle_elite | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 192, 218 | NaxiBeetleLaser_elite | Projectile |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 211, 308 | NaxiBeetleLaser_elite | Warhead@SmallArmsPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 212, 276, 334 | NaxiBeetleLaser_elite | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 213, 242 | NaxiBeetleLaser_elite | Warhead@ChaingunPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 214, 280 | NaxiBeetleLaser_elite | Warhead@MediumMissilePercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 215, 267 | NaxiBeetleLaser_elite | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 216, 270 | NaxiBeetleLaser_elite | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 217, 305 | NaxiBeetleLaser_elite | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 424, 450 | NaxiTank2Laser | Projectile |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 443, 540 | NaxiTank2Laser | Warhead@SmallArmsPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 444, 508, 566 | NaxiTank2Laser | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 445, 474 | NaxiTank2Laser | Warhead@ChaingunPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 446, 512 | NaxiTank2Laser | Warhead@MediumMissilePercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 447, 499 | NaxiTank2Laser | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 448, 502 | NaxiTank2Laser | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 449, 537 | NaxiTank2Laser | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 786, 831, 832 | NaxiMP40Laser | Warhead@Laser_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 797, 800 | NaxiMP40Laser | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 798, 803 | NaxiMP40Laser | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 799, 808 | NaxiMP40Laser | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 806, 811, 817 | NaxiMP40Laser | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 812, 819, 830 | NaxiMP40Laser | Warhead@RA2Scorch |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 824, 828 | NaxiMP40Laser | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 1035, 1086, 1087 | NaxiMP40Laser_elite | Warhead@Laser_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 1042, 1045 | NaxiMP40Laser_elite | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 1043, 1048 | NaxiMP40Laser_elite | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 1044, 1053 | NaxiMP40Laser_elite | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 1051, 1056, 1063 | NaxiMP40Laser_elite | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 1057, 1073, 1078 | NaxiMP40Laser_elite | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 1058, 1065 | NaxiMP40Laser_elite | Warhead@RA2Scorch |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 1070, 1084 | NaxiMP40Laser_elite | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 1154, 1160, 1161 | LunarNaxiJadgDestroyer | Warhead@CannonAP_Light_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 1216, 1237, 1238 | LunarNaxiJadgDestroyer_elite | Warhead@CannonAP_Light_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 1222, 1235 | LunarNaxiJadgDestroyer_elite | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 1302, 1361 | LunarTigerCannon | Warhead@LightChemicalWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 1303, 1336 | LunarTigerCannon | Warhead@HeavyBombPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 1304, 1418 | LunarTigerCannon | Warhead@Smudge1 |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 1305, 1421 | LunarTigerCannon | Warhead@Smudge2 |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 1306, 1426 | LunarTigerCannon | Warhead@Smudge2RA2 |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 1307, 1317 | LunarTigerCannon | Warhead@Effect1 |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 1308, 1322 | LunarTigerCannon | Warhead@Effect2 |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 1309, 1388 | LunarTigerCannon | Warhead@MediumFlameWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 1310, 1415 | LunarTigerCannon | Warhead@RA2Scorch |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 1311, 1330 | LunarTigerCannon | Warhead@GroundFire |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 1312, 1433 | LunarTigerCannon | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 1503, 1551, 1552 | NaxHaenebuQuadCannon | Warhead@Flak_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 1507, 1522 | NaxHaenebuQuadCannon | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 1508, 1511 | NaxHaenebuQuadCannon | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 1509, 1514 | NaxHaenebuQuadCannon | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 1510, 1519 | NaxHaenebuQuadCannon | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 1517, 1526, 1534 | NaxHaenebuQuadCannon | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 1525, 1529 | NaxHaenebuQuadCannon | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 1539, 1549 | NaxHaenebuQuadCannon | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 1609, 1657, 1658 | NaxHaenebuQuadCannon_elite | Warhead@Flak_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 1613, 1628 | NaxHaenebuQuadCannon_elite | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 1614, 1617 | NaxHaenebuQuadCannon_elite | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 1615, 1620 | NaxHaenebuQuadCannon_elite | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 1616, 1625 | NaxHaenebuQuadCannon_elite | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 1623, 1632, 1640 | NaxHaenebuQuadCannon_elite | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 1631, 1635 | NaxHaenebuQuadCannon_elite | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 1645, 1655 | NaxHaenebuQuadCannon_elite | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 1722, 1870 | DalekCannon | Warhead@Tesla_Heavy_ExtraDamage |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 1769, 1813, 1821 | DalekCannon | Warhead@Railgun_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 1777, 1814, 1815 | DalekCannon | Warhead@Laser_Heavy_ExtraDamage |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 1802, 1806, 1809, 1810 | DalekCannon | Warhead@EMPUnit |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 1882, 2019 | DalekCannon_elite | Warhead@Tesla_Heavy_ExtraDamage |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 1919, 1962, 1970 | DalekCannon_elite | Warhead@Railgun_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 1927, 1963, 1964 | DalekCannon_elite | Warhead@Laser_Heavy_ExtraDamage |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 1952, 1958, 1959 | DalekCannon_elite | Warhead@EMPUnit |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 2034, 2175 | DalekCannonScatter | Warhead@Tesla_Heavy_ExtraDamage |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 2077, 2118, 2126 | DalekCannonScatter | Warhead@Railgun_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 2085, 2119, 2120 | DalekCannonScatter | Warhead@Laser_Heavy_ExtraDamage |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 2110, 2114, 2115 | DalekCannonScatter | Warhead@EMPUnit |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 2184, 2250, 2258 | DalekCannonScatterE | Warhead@Railgun_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 2192, 2307 | DalekCannonScatterE | Warhead@Tesla_Heavy_ExtraDamage |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 2217, 2251, 2252 | DalekCannonScatterE | Warhead@Laser_Heavy_ExtraDamage |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 2242, 2246, 2247 | DalekCannonScatterE | Warhead@EMPUnit |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 2397, 2440 | NaxCorrosionRocket | Projectile |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 2436, 2444, 2445 | NaxCorrosionRocket | Warhead@MissileAP_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 2657, 2665, 2666 | NaxCorrosionRocketTrooper_elite | Warhead@MissileAP_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 2725, 2726 | NaxCorrosionBeast | Warhead@MissileAP_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 2782, 2783 | NaxCorrosionBeast_elite | Warhead@MissileAP_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 2900, 2938 | UbermenschLaser | Warhead@Tesla_Heavy_ExtraDamage |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 2934, 2935 | UbermenschLaser | Warhead@EMPUnit |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 2950, 2951 | UbermenschLaser_elite | Warhead@EMPUnit |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 3013, 3025 | NaxiCowDrop | Warhead@Smudge1 |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 3014, 3028 | NaxiCowDrop | Warhead@Smudge2 |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 3015, 3033 | NaxiCowDrop | Warhead@Smudge2RA2 |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 3016, 3017 | NaxiCowDrop | Warhead@Effect2 |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 3045, 3053, 3082 | Lunar_Green105mm_elite | Warhead@Tesla_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 3049, 3050 | Lunar_Green105mm_elite | Warhead@EMPUnit |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 3146, 3212 | Lunar_GreenTigerCannon | Warhead@LightChemicalWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 3147, 3187 | Lunar_GreenTigerCannon | Warhead@HeavyBombPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 3148, 3269 | Lunar_GreenTigerCannon | Warhead@Smudge1 |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 3149, 3272 | Lunar_GreenTigerCannon | Warhead@Smudge2 |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 3150, 3277 | Lunar_GreenTigerCannon | Warhead@Smudge2RA2 |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 3151, 3168 | Lunar_GreenTigerCannon | Warhead@Effect1 |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 3152, 3173 | Lunar_GreenTigerCannon | Warhead@Effect2 |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 3153, 3239 | Lunar_GreenTigerCannon | Warhead@MediumFlameWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 3154, 3266 | Lunar_GreenTigerCannon | Warhead@RA2Scorch |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 3155, 3181 | Lunar_GreenTigerCannon | Warhead@GroundFire |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 3282, 3285 | Lunar_GreenTigerCannon | Warhead@EMPUnit |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 3314, 3380 | Lunar_GreenTigerCannon_elite | Warhead@LightChemicalWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 3315, 3355 | Lunar_GreenTigerCannon_elite | Warhead@HeavyBombPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 3316, 3437 | Lunar_GreenTigerCannon_elite | Warhead@Smudge1 |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 3317, 3440 | Lunar_GreenTigerCannon_elite | Warhead@Smudge2 |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 3318, 3445 | Lunar_GreenTigerCannon_elite | Warhead@Smudge2RA2 |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 3319, 3336 | Lunar_GreenTigerCannon_elite | Warhead@Effect1 |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 3320, 3341 | Lunar_GreenTigerCannon_elite | Warhead@Effect2 |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 3321, 3407 | Lunar_GreenTigerCannon_elite | Warhead@MediumFlameWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 3322, 3434 | Lunar_GreenTigerCannon_elite | Warhead@RA2Scorch |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 3323, 3349 | Lunar_GreenTigerCannon_elite | Warhead@GroundFire |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 3450, 3453 | Lunar_GreenTigerCannon_elite | Warhead@EMPUnit |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 3595, 3609, 3610 | Lunar_GreenJadgDestroyer | Warhead@Tesla_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 3604, 3607 | Lunar_GreenJadgDestroyer | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 3787, 3813, 3814 | Lunar_GreenJadgDestroyer_elite | Warhead@Tesla_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 3798, 3811 | Lunar_GreenJadgDestroyer_elite | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 3882, 3902 | Lunar_GreenGrilleArty | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 3885, 3892 | Lunar_GreenGrilleArty | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 3886, 3887 | Lunar_GreenGrilleArty | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 3895, 3899 | Lunar_GreenGrilleArty | Warhead@EMPUnit |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 3896, 3906 | Lunar_GreenGrilleArty | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 3935, 3953 | Lunar_GreenGrilleArty_elite | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 3938, 3945 | Lunar_GreenGrilleArty_elite | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 3939, 3940 | Lunar_GreenGrilleArty_elite | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 3948, 3950 | Lunar_GreenGrilleArty_elite | Warhead@EMPUnit |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 3949, 3957 | Lunar_GreenGrilleArty_elite | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 3978, 4006 | Lunar_GreenSturmArty | Warhead@Smudge1 |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 3979, 4003 | Lunar_GreenSturmArty | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 3980, 4009 | Lunar_GreenSturmArty | Warhead@Smudge2 |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 3981, 4014 | Lunar_GreenSturmArty | Warhead@Smudge2RA2 |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 3982, 3985 | Lunar_GreenSturmArty | Warhead@Effect1 |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 3983, 3990 | Lunar_GreenSturmArty | Warhead@Effect2 |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 3984, 3998 | Lunar_GreenSturmArty | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 4019, 4022 | Lunar_GreenSturmArty | Warhead@EMPUnit |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 4038, 4054, 4055 | schwarzermond_lunarsoldier_rifle_yellow | Warhead@Laser_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 4043, 4044 | schwarzermond_lunarsoldier_rifle_yellow | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 4045, 4048 | schwarzermond_lunarsoldier_rifle_yellow | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 4112, 4128, 4129 | schwarzermond_lunarsoldier_rifle_yellow_elite | Warhead@Laser_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 4117, 4118 | schwarzermond_lunarsoldier_rifle_yellow_elite | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 4119, 4122 | schwarzermond_lunarsoldier_rifle_yellow_elite | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 4186, 4202, 4203 | schwarzermond_lunarsoldier_rifle_amplified | Warhead@Laser_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 4191, 4192 | schwarzermond_lunarsoldier_rifle_amplified | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 4193, 4196 | schwarzermond_lunarsoldier_rifle_amplified | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 4260, 4276, 4277 | schwarzermond_lunarsoldier_rifle_amplified_elite | Warhead@Laser_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 4265, 4266 | schwarzermond_lunarsoldier_rifle_amplified_elite | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 4267, 4270 | schwarzermond_lunarsoldier_rifle_amplified_elite | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 4366, 4383 | Lunar_YellowBeetleLaser | Warhead@ShieldHit |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 4432, 4449 | Lunar_AmplifiedBeetleLaser | Warhead@ShieldHit |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 4643, 4694, 4695 | Lunar_YellowMP40Laser | Warhead@Laser_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 4650, 4653 | Lunar_YellowMP40Laser | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 4651, 4656 | Lunar_YellowMP40Laser | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 4652, 4661 | Lunar_YellowMP40Laser | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 4659, 4664, 4671 | Lunar_YellowMP40Laser | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 4665, 4681, 4686 | Lunar_YellowMP40Laser | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 4666, 4673 | Lunar_YellowMP40Laser | Warhead@RA2Scorch |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 4678, 4692 | Lunar_YellowMP40Laser | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 4897, 4948, 4949 | Lunar_YellowMP40Laser_elite | Warhead@Laser_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 4904, 4907 | Lunar_YellowMP40Laser_elite | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 4905, 4910 | Lunar_YellowMP40Laser_elite | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 4906, 4915 | Lunar_YellowMP40Laser_elite | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 4913, 4918, 4925 | Lunar_YellowMP40Laser_elite | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 4919, 4935, 4940 | Lunar_YellowMP40Laser_elite | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 4920, 4927 | Lunar_YellowMP40Laser_elite | Warhead@RA2Scorch |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 4932, 4946 | Lunar_YellowMP40Laser_elite | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 5155, 5206, 5207 | Lunar_AmplifiedMP40Laser | Warhead@Laser_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 5162, 5165 | Lunar_AmplifiedMP40Laser | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 5163, 5168 | Lunar_AmplifiedMP40Laser | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 5164, 5173 | Lunar_AmplifiedMP40Laser | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 5171, 5176, 5183 | Lunar_AmplifiedMP40Laser | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 5177, 5193, 5198 | Lunar_AmplifiedMP40Laser | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 5178, 5185 | Lunar_AmplifiedMP40Laser | Warhead@RA2Scorch |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 5190, 5204 | Lunar_AmplifiedMP40Laser | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 5409, 5460, 5461 | Lunar_AmplifiedMP40Laser_elite | Warhead@Laser_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 5416, 5419 | Lunar_AmplifiedMP40Laser_elite | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 5417, 5422 | Lunar_AmplifiedMP40Laser_elite | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 5418, 5427 | Lunar_AmplifiedMP40Laser_elite | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 5425, 5430, 5437 | Lunar_AmplifiedMP40Laser_elite | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 5431, 5447, 5452 | Lunar_AmplifiedMP40Laser_elite | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 5432, 5439 | Lunar_AmplifiedMP40Laser_elite | Warhead@RA2Scorch |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 5444, 5458 | Lunar_AmplifiedMP40Laser_elite | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 5522, 5539 | Lunar_YellowTank2Laser | Warhead@ShieldHit |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 5588, 5605 | Lunar_AmplifiedTank2Laser | Warhead@ShieldHit |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 5651, 5652 | Lunar_YellowUbermenschLaser | Warhead@EMPUnit |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 5690, 5691 | Lunar_YellowUbermenschLaser_elite | Warhead@EMPUnit |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 5729, 5730 | Lunar_AmplifiedUbermenschLaser | Warhead@EMPUnit |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 5772, 5773 | Lunar_AmplifiedUbermenschLaser_elite | Warhead@EMPUnit |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 5822, 5854 | Naxis_Komet | Projectile |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 5832, 5881 | Naxis_Komet | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 5841, 5893 | Naxis_Komet | Warhead@FlakWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 5842, 5888 | Naxis_Komet | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 5843, 5883 | Naxis_Komet | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 5844, 5997 | Naxis_Komet | Warhead@ShieldHit |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 5845, 5873 | Naxis_Komet | Warhead@Concrete |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 5846, 6002 | Naxis_Komet | Warhead@ShieldHitEffect |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 5847, 5918 | Naxis_Komet | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 5848, 6005 | Naxis_Komet | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 5849, 5875 | Naxis_Komet | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 5850, 5878 | Naxis_Komet | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 5851, 5944 | Naxis_Komet | Warhead@HeavyAAWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 5852, 5969 | Naxis_Komet | Warhead@MediumMissilePercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 5853, 5994 | Naxis_Komet | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/sequences.yaml | 525, 583 | latinsyndicate_topolsilo | critical-idle |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 40, 45, 46 | latinsyndicate_latinmilitia_molotov | Warhead@Flame_Light_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 102, 107, 108 | latinsyndicate_latinmilitia_molotov_elite | Warhead@Flame_Light_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 166, 174 | LatinTankKillerRocket | Projectile |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 265, 269, 270 | RA2NarcoAKM | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 324, 325 | RA2NarcoAKM_elite | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 422, 499, 500 | RA2FreedomAK47 | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 427, 428 | RA2FreedomAK47 | Warhead@ChaingunPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 452, 471 | RA2FreedomAK47 | Warhead@Glow |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 453, 478 | RA2FreedomAK47 | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 454, 457 | RA2FreedomAK47 | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 455, 460 | RA2FreedomAK47 | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 456, 475 | RA2FreedomAK47 | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 463, 485, 497 | RA2FreedomAK47 | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 465, 482, 488 | RA2FreedomAK47 | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 469, 481, 491 | RA2FreedomAK47 | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 558, 630, 631 | RA2FreedomAK47_elite | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 562, 581 | RA2FreedomAK47_elite | Warhead@Glow |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 563, 588 | RA2FreedomAK47_elite | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 564, 567 | RA2FreedomAK47_elite | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 565, 570 | RA2FreedomAK47_elite | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 566, 585 | RA2FreedomAK47_elite | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 573, 596, 611, 622 | RA2FreedomAK47_elite | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 575, 593, 599 | RA2FreedomAK47_elite | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 579, 591, 602 | RA2FreedomAK47_elite | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 613, 624 | RA2FreedomAK47_elite | Warhead@Effect1 |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 616, 627 | RA2FreedomAK47_elite | Warhead@Effect2 |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 689, 701 | RA2FreedomRocket | Projectile |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 699, 710 | RA2FreedomRocket | Warhead@FlakWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 700, 734 | RA2FreedomRocket | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 820, 862 | SyndicateFireballLauncher_elite | Warhead@Flame_Heavy |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 1145, 1150, 1151 | LatinMonkeyGrenade1 | Warhead@Demolition_Light_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 1209, 1214, 1215 | LatinMonkeyGrenade2 | Warhead@Demolition_Light_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 1273, 1281, 1282 | LatinMonkeyGrenade3 | Warhead@Demolition_Light_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 1342, 1347, 1348 | LatinMonkeyGrenadeExplode | Warhead@Demolition_Light_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 1430, 1583, 1584 | RA2RBurritoRocket | Warhead@CannonHE_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 1438, 1576 | RA2RBurritoRocket | Warhead@ShieldHit |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 1440, 1525 | RA2RBurritoRocket | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 1442, 1494 | RA2RBurritoRocket | Warhead@MediumFlameWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 1443, 1522 | RA2RBurritoRocket | Warhead@RA2Scorch |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 1444, 1462 | RA2RBurritoRocket | Warhead@GroundFire |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 1445, 1468 | RA2RBurritoRocket | Warhead@HeavyBombPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 1446, 1551 | RA2RBurritoRocket | Warhead@Smudge1 |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 1447, 1554 | RA2RBurritoRocket | Warhead@Smudge2 |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 1448, 1559 | RA2RBurritoRocket | Warhead@Smudge2RA2 |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 1449, 1454 | RA2RBurritoRocket | Warhead@Effect2 |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 1450, 1567 | RA2RBurritoRocket | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 1644, 1674, 1753 | RA2LarsRocket | Projectile |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 1667, 1757, 1758 | RA2LarsRocket | Warhead@MissileAP_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 1671, 1681 | RA2LarsRocket | Warhead@HeavyAAWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 1672, 1705 | RA2LarsRocket | Warhead@LightMissilePercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 1673, 1729 | RA2LarsRocket | Warhead@TankDestroyerCannonPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 1825, 1829, 1830 | RA2Gren60mm | Warhead@CannonAP_Light_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 1884, 1888, 1889 | RA2Gren60mm_elite | Warhead@CannonAP_Light_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 1947, 1955 | LatinRusherRocket | Projectile |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 1997, 2143, 2144 | LatinSmokerCannon | Warhead@CannonHE_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 2002, 2061 | LatinSmokerCannon | Warhead@MediumFlameWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 2003, 2088 | LatinSmokerCannon | Warhead@RA2Scorch |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 2004, 2030 | LatinSmokerCannon | Warhead@GroundFire |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 2005, 2036 | LatinSmokerCannon | Warhead@HeavyBombPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 2006, 2116 | LatinSmokerCannon | Warhead@Smudge1 |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 2007, 2119 | LatinSmokerCannon | Warhead@Smudge2 |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 2008, 2124 | LatinSmokerCannon | Warhead@Smudge2RA2 |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 2009, 2017 | LatinSmokerCannon | Warhead@Effect1 |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 2010, 2022 | LatinSmokerCannon | Warhead@Effect2 |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 2011, 2091 | LatinSmokerCannon | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 2012, 2131 | LatinSmokerCannon | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 2200, 2264 | LatinSmokerCannon_elite | Warhead@MediumFlameWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 2201, 2291 | LatinSmokerCannon_elite | Warhead@RA2Scorch |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 2202, 2233 | LatinSmokerCannon_elite | Warhead@GroundFire |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 2203, 2239 | LatinSmokerCannon_elite | Warhead@HeavyBombPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 2204, 2319 | LatinSmokerCannon_elite | Warhead@Smudge1 |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 2205, 2322 | LatinSmokerCannon_elite | Warhead@Smudge2 |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 2206, 2327 | LatinSmokerCannon_elite | Warhead@Smudge2RA2 |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 2207, 2217 | LatinSmokerCannon_elite | Warhead@Effect1 |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 2208, 2222 | LatinSmokerCannon_elite | Warhead@Effect2 |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 2209, 2294 | LatinSmokerCannon_elite | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 2341, 2342 | LatinSmokerCannon_elite | Warhead@CannonHE_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 2420, 2507, 2508 | RA2GrenadePack | Warhead@Concussion_Light |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 2465, 2476 | RA2GrenadePack | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 2466, 2468 | RA2GrenadePack | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 2467, 2471 | RA2GrenadePack | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 2474, 2479, 2483 | RA2GrenadePack | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 2480, 2488, 2501 | RA2GrenadePack | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 2521, 2537 | RA2GrenadePack_elite | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 2524, 2553, 2558, 2560 | RA2GrenadePack_elite | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 2527, 2559, 2563 | RA2GrenadePack_elite | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 2531, 2566 | RA2GrenadePack_elite | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 2544, 2555 | RA2GrenadePack_elite | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 2545, 2547 | RA2GrenadePack_elite | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 2546, 2550 | RA2GrenadePack_elite | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 2568, 2610 | RA2GrenadePack_elite | Warhead@Concussion_Light |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 2626, 2634 | LatinSmokerRocket | Projectile |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 2668, 2677 | DiabloCannon | Projectile |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 2670, 2727 | DiabloCannon | Warhead@LightMissilePercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 2671, 2755 | DiabloCannon | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 2672, 2719 | DiabloCannon | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 2673, 2752 | DiabloCannon | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 2674, 2722 | DiabloCannon | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 2675, 2758 | DiabloCannon | Warhead@TankDestroyerCannonPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 2676, 2694 | DiabloCannon | Warhead@ChaingunPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 2725, 2784, 2792 | DiabloCannon | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 2783, 2787 | DiabloCannon | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 2797, 2807 | DiabloCannon | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 2823, 2854 | DiabloCannon_elite | Warhead@ShieldHit |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 2834, 2838 | DiabloCannon_elite | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 2835, 2843 | DiabloCannon_elite | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 2848, 2858 | DiabloCannon_elite | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 2865, 2892 | DiabloCannon_AA | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 2884, 2887 | DiabloCannon_AA | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 2896, 2906 | DiabloCannon_AA | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 2927, 2930 | DiabloCannonAAE_AA | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 2939, 2949 | DiabloCannonAAE_AA | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 2959, 2998 | RA2MortarBike | Projectile |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 2973, 3001 | RA2MortarBike | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 2981, 3172, 3173 | RA2MortarBike | Warhead@Concussion_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 2985, 3018 | RA2MortarBike | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 2986, 3132 | RA2MortarBike | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 2987, 3049 | RA2MortarBike | Warhead@HeavyBombPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 2988, 3159 | RA2MortarBike | Warhead@Smudge1 |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 2989, 3162 | RA2MortarBike | Warhead@Smudge2 |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 2990, 3167 | RA2MortarBike | Warhead@Smudge2RA2 |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 2991, 3005 | RA2MortarBike | Warhead@Effect1 |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 2992, 3010 | RA2MortarBike | Warhead@Effect2 |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 2994, 3074 | RA2MortarBike | Warhead@LightFlameWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 2995, 3128 | RA2MortarBike | Warhead@RA2Scorch |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 2996, 3043 | RA2MortarBike | Warhead@GroundFire |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 2997, 3101 | RA2MortarBike | Warhead@MediumChemicalWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 3228, 3232, 3233 | RA2MortarBike_elite | Warhead@Concussion_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 3306, 3310, 3311 | LatinBuggyMG | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 3366, 3367 | LatinBuggyMG_elite | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 3425, 3556 | LatinBuggyChaingun | Projectile |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 3547, 3602, 3603 | LatinBuggyChaingun | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 3551, 3574 | LatinBuggyChaingun | Warhead@Glow |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 3552, 3581 | LatinBuggyChaingun | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 3553, 3560 | LatinBuggyChaingun | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 3554, 3563 | LatinBuggyChaingun | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 3555, 3578 | LatinBuggyChaingun | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 3566, 3585, 3589 | LatinBuggyChaingun | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 3568, 3586, 3593 | LatinBuggyChaingun | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 3572, 3584, 3596 | LatinBuggyChaingun | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 3657, 3680 | LatinBuggyChaingun_elite | Warhead@Glow |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 3658, 3687 | LatinBuggyChaingun_elite | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 3659, 3666 | LatinBuggyChaingun_elite | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 3660, 3669 | LatinBuggyChaingun_elite | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 3661, 3684 | LatinBuggyChaingun_elite | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 3672, 3691, 3695 | LatinBuggyChaingun_elite | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 3674, 3692, 3699 | LatinBuggyChaingun_elite | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 3678, 3690, 3702 | LatinBuggyChaingun_elite | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 3708, 3709 | LatinBuggyChaingun_elite | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 3770, 3900 | YakovlevCannon | Projectile |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 3891, 3946, 3947 | YakovlevCannon | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 3895, 3918 | YakovlevCannon | Warhead@Glow |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 3896, 3925 | YakovlevCannon | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 3897, 3904 | YakovlevCannon | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 3898, 3907 | YakovlevCannon | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 3899, 3922 | YakovlevCannon | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 3910, 3929, 3933 | YakovlevCannon | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 3912, 3930, 3937 | YakovlevCannon | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 3916, 3928, 3940 | YakovlevCannon | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 4005, 4135 | YakovlevCannon_elite | Projectile |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 4126, 4181, 4182 | YakovlevCannon_elite | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 4130, 4153 | YakovlevCannon_elite | Warhead@Glow |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 4131, 4160 | YakovlevCannon_elite | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 4132, 4139 | YakovlevCannon_elite | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 4133, 4142 | YakovlevCannon_elite | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 4134, 4157 | YakovlevCannon_elite | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 4145, 4164, 4168 | YakovlevCannon_elite | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 4147, 4165, 4172 | YakovlevCannon_elite | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 4151, 4163, 4175 | YakovlevCannon_elite | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 4241, 4267 | LatinSentryMG | Projectile |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 4258, 4313, 4314 | LatinSentryMG | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 4262, 4285 | LatinSentryMG | Warhead@Glow |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 4263, 4292 | LatinSentryMG | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 4264, 4271 | LatinSentryMG | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 4265, 4274 | LatinSentryMG | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 4266, 4289 | LatinSentryMG | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 4277, 4296, 4300 | LatinSentryMG | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 4279, 4297, 4304 | LatinSentryMG | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 4283, 4295, 4307 | LatinSentryMG | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 4368, 4423, 4424 | LatinSentryMG_elite | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 4372, 4395 | LatinSentryMG_elite | Warhead@Glow |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 4373, 4402 | LatinSentryMG_elite | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 4374, 4381 | LatinSentryMG_elite | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 4375, 4384 | LatinSentryMG_elite | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 4376, 4399 | LatinSentryMG_elite | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 4387, 4406, 4410 | LatinSentryMG_elite | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 4389, 4407, 4414 | LatinSentryMG_elite | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 4393, 4405, 4417 | LatinSentryMG_elite | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 4484, 4509 | LatinAADefenderCannon | Projectile |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 4497, 4531 | LatinAADefenderCannon | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 4501, 4539, 4540 | LatinAADefenderCannon | Warhead@MissileAP_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 4505, 4536 | LatinAADefenderCannon | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 4506, 4525 | LatinAADefenderCannon | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 4507, 4528 | LatinAADefenderCannon | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 4508, 4533 | LatinAADefenderCannon | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 4605, 4669 | RA2TOPOLCuba | Projectile |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 4616, 4683 | RA2TOPOLCuba | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 4642, 4649, 4851 | RA2TOPOLCuba | Warhead@ShieldHit |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 4645, 4818 | RA2TOPOLCuba | Warhead@NuclearWarheadPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 4646, 4891 | RA2TOPOLCuba | Warhead@Smudge1 |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 4647, 4896 | RA2TOPOLCuba | Warhead@Smudge2 |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 4648, 4906 | RA2TOPOLCuba | Warhead@Smudge3 |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 4650, 4859 | RA2TOPOLCuba | Warhead@ShieldHitEffectNuclear |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 4651, 4675 | RA2TOPOLCuba | Warhead@Concrete |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 4652, 4736 | RA2TOPOLCuba | Warhead@HeavyBombPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 4653, 4845 | RA2TOPOLCuba | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 4654, 4901 | RA2TOPOLCuba | Warhead@Smudge2RA2 |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 4655, 4686 | RA2TOPOLCuba | Warhead@Effect1 |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 4656, 4691 | RA2TOPOLCuba | Warhead@Effect2 |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 4657, 4699 | RA2TOPOLCuba | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 4658, 4856 | RA2TOPOLCuba | Warhead@ShieldHitEffect |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 4660, 4790 | RA2TOPOLCuba | Warhead@MediumFlameWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 4661, 4888 | RA2TOPOLCuba | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 4662, 4848 | RA2TOPOLCuba | Warhead@RA2Scorch |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 4663, 4730 | RA2TOPOLCuba | Warhead@GroundFire |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 4664, 4862 | RA2TOPOLCuba | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 4665, 4677 | RA2TOPOLCuba | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 4666, 4680 | RA2TOPOLCuba | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 4667, 4762 | RA2TOPOLCuba | Warhead@LightChemicalWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 4668, 4704 | RA2TOPOLCuba | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 4927, 4936 | RA2APCFlakCannon | Projectile |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 4929, 4986 | RA2APCFlakCannon | Warhead@LightMissilePercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 4930, 5014 | RA2APCFlakCannon | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 4931, 4978 | RA2APCFlakCannon | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 4932, 5011 | RA2APCFlakCannon | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 4933, 4981 | RA2APCFlakCannon | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 4934, 5017 | RA2APCFlakCannon | Warhead@TankDestroyerCannonPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 4935, 4953 | RA2APCFlakCannon | Warhead@ChaingunPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 4984, 5043, 5051 | RA2APCFlakCannon | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 5042, 5046 | RA2APCFlakCannon | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 5056, 5066 | RA2APCFlakCannon | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 5082, 5113 | RA2APCFlakCannon_elite | Warhead@ShieldHit |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 5093, 5097 | RA2APCFlakCannon_elite | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 5094, 5102 | RA2APCFlakCannon_elite | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 5107, 5117 | RA2APCFlakCannon_elite | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 5124, 5151 | RA2APCFlakCannonAA | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 5143, 5146 | RA2APCFlakCannonAA | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 5155, 5165 | RA2APCFlakCannonAA | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 5186, 5189 | RA2APCFlakCannonAA_elite | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 5198, 5208 | RA2APCFlakCannonAA_elite | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 5234, 5239, 5240 | RA2APCMachineGun | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 5296, 5301, 5302 | RA2APCMachineGun_elite | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 5356, 5359, 5360 | RA2APCMachineGun_AA | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 5415, 5417, 5418 | RA2APCMachineGun_AA_elite | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 5477, 5576 | RA2APCRocket | Projectile |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 5521, 5657 | RA2APCRocket | Warhead@MissileHE_Medium |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 5573, 5631 | RA2APCRocket | Warhead@HeavyBombPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 5574, 5606 | RA2APCRocket | Warhead@HeavyAAWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 5575, 5581 | RA2APCRocket | Warhead@FlakWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 5682, 5734 | RA2APCRocket_elite | Warhead@MissileHE_Medium |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 5828, 5948 | RA2AkulaRockets | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 5830, 5935 | RA2AkulaRockets | Warhead@Radiation |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 5931, 5958, 5959 | RA2AkulaRockets | Warhead@MissileAP_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 5938, 5955 | RA2AkulaRockets | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 5939, 5942 | RA2AkulaRockets | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 5940, 5945 | RA2AkulaRockets | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 5941, 5950 | RA2AkulaRockets | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 6016, 6120 | Rammax_Sabot | Projectile |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 6118, 6130 | Rammax_Sabot | Warhead@ChaingunPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 6119, 6157 | Rammax_Sabot | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 6155, 6161 | Rammax_Sabot | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 66, 70, 71 | tkmjuggap | Warhead@CannonAP_Light_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 135, 139, 140 | tkmbunkmg | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 221, 222 | tkmtechnicalmgap | Warhead@CannonAP_Light_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 351, 400, 401 | tkmakap | Warhead@Bullet_Light |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 501, 547, 548 | tkmm203 | Warhead@Demolition_Light |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 633, 637, 638 | TKMQuadCannonAG | Warhead@Flak_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 690, 694, 695 | TKMQuadCannonAA | Warhead@Flak_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 759, 765, 766 | tkmturretcannon | Warhead@CannonAP_Light_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 860, 864, 865 | TKMZazaCannonAG | Warhead@Flak_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 917, 921, 922 | TKMZazaCannonAA | Warhead@Flak_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 988, 992, 993 | TKMAATurretCannon | Warhead@Flak_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 1129, 1134, 1135 | t30shell | Warhead@Railgun_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 1224, 1228, 1229 | HueyCryoMissiles | Warhead@Cryo_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 1307, 1313, 1314 | HueyTwinMissiles | Warhead@MissileAP_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 1389, 1453, 1454 | sandmarinemortar | Warhead@Demolition_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 1395, 1407 | sandmarinemortar | Warhead@RA2Scorch |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 1396, 1401 | sandmarinemortar | Warhead@GroundFire |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 1397, 1424 | sandmarinemortar | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 1410, 1418 | sandmarinemortar | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 1411, 1421 | sandmarinemortar | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 1412, 1443 | sandmarinemortar | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 1413, 1429 | sandmarinemortar | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 1414, 1416 | sandmarinemortar | Warhead@Concrete |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 1415, 1434 | sandmarinemortar | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 1528, 1592, 1593 | bigshieemortar | Warhead@Demolition_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 1534, 1546 | bigshieemortar | Warhead@RA2Scorch |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 1535, 1540 | bigshieemortar | Warhead@GroundFire |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 1536, 1563 | bigshieemortar | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 1549, 1557 | bigshieemortar | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 1550, 1560 | bigshieemortar | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 1551, 1582 | bigshieemortar | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 1552, 1568 | bigshieemortar | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 1553, 1555 | bigshieemortar | Warhead@Concrete |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 1554, 1573 | bigshieemortar | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 1699, 1707 | tkmkatyushalalauncherrocketscryo | Warhead@PhysicalStateCryo |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 1771, 1780, 1781 | SandmarineTusk | Warhead@MissileHE_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 1848, 1852, 1853 | BigShieeTusk | Warhead@MissileHE_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 1943, 1946 | SandmarineTuskTwin | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 1944, 1967 | SandmarineTuskTwin | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 1951, 1976 | SandmarineTuskTwin | Warhead@Glow |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 1952, 1991 | SandmarineTuskTwin | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 1953, 1961 | SandmarineTuskTwin | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 1954, 1964 | SandmarineTuskTwin | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 1955, 1983 | SandmarineTuskTwin | Warhead@ShieldHit |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 1956, 1959 | SandmarineTuskTwin | Warhead@Concrete |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 1957, 1988 | SandmarineTuskTwin | Warhead@ShieldHitEffect |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 1958, 1980 | SandmarineTuskTwin | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 2004, 2015 | SandmarineTuskCryo | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 2005, 2019 | SandmarineTuskCryo | Warhead@PhysicalStateCryo |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 2033, 2139, 2140 | SandmarineTuskFire | Warhead@Flame_Light |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 2081, 2127 | SandmarineTuskFire | Warhead@ShieldHit |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 2088, 2096, 2136 | SandmarineTuskFire | Warhead@RA2Scorch |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 2089, 2090 | SandmarineTuskFire | Warhead@GroundFire |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 2100, 2131 | SandmarineTuskFire | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 2102, 2115, 2137 | SandmarineTuskFire | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 2103, 2109 | SandmarineTuskFire | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 2104, 2112 | SandmarineTuskFire | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 2105, 2119 | SandmarineTuskFire | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 2106, 2107 | SandmarineTuskFire | Warhead@Concrete |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 2184, 2211, 2212 | ViperMissiles | Warhead@MissileAP_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 2264, 2268, 2269 | ViperMissilesTwin | Warhead@MissileAP_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 2320, 2344, 2345 | ViperMissilesCryo | Warhead@MissileAP_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 2324, 2338 | ViperMissilesCryo | Warhead@PhysicalStateCryo |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 2398, 2454 | ViperMissilesFire | Projectile |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 2402, 2530, 2531 | ViperMissilesFire | Warhead@Flame_Light |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 2469, 2479, 2527 | ViperMissilesFire | Warhead@RA2Scorch |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 2470, 2473 | ViperMissilesFire | Warhead@GroundFire |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 2471, 2485, 2500 | ViperMissilesFire | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 2483, 2524 | ViperMissilesFire | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 2486, 2494 | ViperMissilesFire | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 2487, 2497 | ViperMissilesFire | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 2488, 2510 | ViperMissilesFire | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 2489, 2492 | ViperMissilesFire | Warhead@Concrete |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 2490, 2519 | ViperMissilesFire | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 2491, 2505 | ViperMissilesFire | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 2565, 2591 | VonSniper | Projectile |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 2580, 2636 | VonSniper | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 2587, 2668 | VonSniper | Warhead@MediumMissilePercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 2588, 2697 | VonSniper | Warhead@SmallArmsPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 2589, 2643 | VonSniper | Warhead@FlakWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 2590, 2609 | VonSniper | Warhead@ChaingunPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 2969, 2973, 2974 | tkmquadcannonmg | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 3036, 3051 | HeavyAATankCannontkm | Projectile |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 3037, 3145 | HeavyAATankCannontkm | Warhead@LightMissilePercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 3038, 3204 | HeavyAATankCannontkm | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 3039, 3102 | HeavyAATankCannontkm | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 3040, 3169 | HeavyAATankCannontkm | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 3041, 3105 | HeavyAATankCannontkm | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 3042, 3108 | HeavyAATankCannontkm | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 3043, 3112 | HeavyAATankCannontkm | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 3044, 3172 | HeavyAATankCannontkm | Warhead@ShieldHit |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 3045, 3100 | HeavyAATankCannontkm | Warhead@Concrete |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 3046, 3177 | HeavyAATankCannontkm | Warhead@ShieldHitEffect |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 3047, 3121 | HeavyAATankCannontkm | Warhead@FlakWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 3048, 3117, 3208 | HeavyAATankCannontkm | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 3049, 3180 | HeavyAATankCannontkm | Warhead@SmallArmsPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 3050, 3076 | HeavyAATankCannontkm | Warhead@ChaingunPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 3224, 3239 | tkmheavyaaturret | Projectile |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 3225, 3333 | tkmheavyaaturret | Warhead@LightMissilePercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 3226, 3392 | tkmheavyaaturret | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 3227, 3290 | tkmheavyaaturret | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 3228, 3357 | tkmheavyaaturret | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 3229, 3293 | tkmheavyaaturret | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 3230, 3296 | tkmheavyaaturret | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 3231, 3300 | tkmheavyaaturret | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 3232, 3360 | tkmheavyaaturret | Warhead@ShieldHit |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 3233, 3288 | tkmheavyaaturret | Warhead@Concrete |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 3234, 3365 | tkmheavyaaturret | Warhead@ShieldHitEffect |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 3235, 3309 | tkmheavyaaturret | Warhead@FlakWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 3236, 3305, 3396 | tkmheavyaaturret | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 3237, 3368 | tkmheavyaaturret | Warhead@SmallArmsPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 3238, 3264 | tkmheavyaaturret | Warhead@ChaingunPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 3425, 3429, 3430 | tkmtrenchcannon | Warhead@CannonHE_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 3483, 3487, 3488 | tkmtrenchdepcannon | Warhead@CannonHE_Heavy_Flat |
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
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 1129, 1167 | VultureGrenade | Projectile |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 1142, 1228 | VultureGrenade | Warhead@SmallArmsPercentage |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 1145, 1176 | VultureGrenade | Warhead@FlakWeaponPercentage |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 1148, 1202 | VultureGrenade | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 1153, 1251 | VultureGrenade | Warhead@TankDestroyerCannonPercentage |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 1156, 1171 | VultureGrenade | Warhead@Effect |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 1165, 1226 | VultureGrenade | Warhead@ShieldHit |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 1300, 1465 | SiegeTankSiegeCannon | Projectile |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 1311, 1604 | SiegeTankSiegeCannon | Warhead@LightMissilePercentage |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 1321, 1579 | SiegeTankSiegeCannon | Warhead@LightChemicalWeaponPercentage |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 1331, 1648 | SiegeTankSiegeCannon | Warhead@MediumChemicalWeaponPercentage |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 1341, 1530 | SiegeTankSiegeCannon | Warhead@HeavyChemicalWeaponPercentage |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 1367, 1673 | SiegeTankSiegeCannon | Warhead@MediumFlameWeaponPercentage |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 1381, 1555 | SiegeTankSiegeCannon | Warhead@HeavyFlameWeaponPercentage |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 1393, 1486 | SiegeTankSiegeCannon | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 1403, 1696 | SiegeTankSiegeCannon | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 1417, 1509 | SiegeTankSiegeCannon | Warhead@HeavyCannonPercentage |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 1424, 1626 | SiegeTankSiegeCannon | Warhead@MediumCannonPercentage |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 1436, 1482 | SiegeTankSiegeCannon | Warhead@Effect |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 1782, 1805 | GoliathMk2MG | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 1788, 1828 | GoliathMk2MG | Warhead@TankDestroyerCannonPercentage |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 1795, 1798 | GoliathMk2MG | Projectile |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 2075, 2112 | ValkyrieRockets | Projectile |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 2087, 2127 | ValkyrieRockets | Warhead@FlakWeaponPercentage |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 2090, 2171 | ValkyrieRockets | Warhead@MediumMissilePercentage |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 2093, 2149 | ValkyrieRockets | Warhead@HeavyAAWeaponPercentage |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 2096, 2125 | ValkyrieRockets | Warhead@EffectWater |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 2208, 2286 | WyvernRockets | Projectile |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 2240, 2341 | WyvernRockets | Warhead@MediumChemicalWeaponPercentage |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 2255, 2297 | WyvernRockets | Warhead@FlakWeaponPercentage |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 2261, 2366 | WyvernRockets | Warhead@MediumMissilePercentage |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 2267, 2319 | WyvernRockets | Warhead@HeavyAAWeaponPercentage |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 2401, 2479 | BCLaser | Projectile |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 2421, 2581 | BCLaser | Warhead@HeavyMissilePercentage |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 2430, 2510 | BCLaser | Warhead@HeavyAAWeaponPercentage |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 2445, 2532 | BCLaser | Warhead@HeavyChemicalWeaponPercentage |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 2456, 2557 | BCLaser | Warhead@HeavyFlameWeaponPercentage |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 2691, 2779 | PhobosLaser | Projectile |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 2705, 2881 | PhobosLaser | Warhead@HeavyMissilePercentage |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 2714, 2810 | PhobosLaser | Warhead@HeavyAAWeaponPercentage |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 2729, 2832 | PhobosLaser | Warhead@HeavyChemicalWeaponPercentage |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 2740, 2857 | PhobosLaser | Warhead@HeavyFlameWeaponPercentage |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 2959, 3006 | MedicFlare | Projectile |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 2980, 3043 | MedicFlare | Warhead@LightFlameWeaponPercentage |
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
| mods/cameo/weapons/outpost2.yaml | 47, 75 | edenMobileLaser | Projectile |
| mods/cameo/weapons/outpost2.yaml | 56, 139 | edenMobileLaser | Warhead@TankDestroyerCannonPercentage |
| mods/cameo/weapons/outpost2.yaml | 59, 86 | edenMobileLaser | Warhead@ChaingunPercentage |
| mods/cameo/weapons/outpost2.yaml | 62, 110 | edenMobileLaser | Warhead@FlakWeaponPercentage |
| mods/cameo/weapons/outpost2.yaml | 224, 336 | edenRailgun | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/weapons/outpost2.yaml | 231, 282 | edenRailgun | Warhead@LightChemicalWeaponPercentage |
| mods/cameo/weapons/outpost2.yaml | 235, 360 | edenRailgun | Warhead@TankDestroyerCannonPercentage |
| mods/cameo/weapons/outpost2.yaml | 239, 308 | edenRailgun | Warhead@MediumCannonPercentage |
| mods/cameo/weapons/outpost2.yaml | 243, 260 | edenRailgun | Warhead@HeavyCannonPercentage |
| mods/cameo/weapons/outpost2.yaml | 828, 937 | eden_EMP | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/weapons/outpost2.yaml | 837, 896 | eden_EMP | Warhead@HeavyCannonPercentage |
| mods/cameo/weapons/outpost2.yaml | 855, 916 | eden_EMP | Warhead@MediumMissilePercentage |
| mods/cameo/weapons/outpost2.yaml | 966, 997 | eden_EMP_AA | Projectile |
| mods/cameo/weapons/outpost2.yaml | 969, 991 | eden_EMP_AA | Warhead@Tesla_Super |
| mods/cameo/weapons/outpost2.yaml | 971, 1016 | eden_EMP_AA | Warhead@TeslaChargedWeaponPercentage |
| mods/cameo/weapons/outpost2.yaml | 973, 1045 | eden_EMP_AA | Warhead@TeslaWeaponPercentage |
| mods/cameo/weapons/outpost2.yaml | 975, 1070 | eden_EMP_AA | Warhead@MediumFlameWeaponPercentage |
| mods/cameo/weapons/outpost2.yaml | 977, 1148 | eden_EMP_AA | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/weapons/outpost2.yaml | 979, 1172 | eden_EMP_AA | Warhead@HeavyCannonPercentage |
| mods/cameo/weapons/outpost2.yaml | 981, 1102 | eden_EMP_AA | Warhead@MediumCannonPercentage |
| mods/cameo/weapons/outpost2.yaml | 983, 1194 | eden_EMP_AA | Warhead@MediumMissilePercentage |
| mods/cameo/weapons/outpost2.yaml | 985, 1125 | eden_EMP_AA | Warhead@HeavyMissilePercentage |
| mods/cameo/weapons/outpost2.yaml | 987, 1217 | eden_EMP_AA | Warhead@EMPCompatibility |
| mods/cameo/weapons/outpost2.yaml | 1229, 1259 | edenTiger_EMP | Warhead@Tesla_Super |
| mods/cameo/weapons/outpost2.yaml | 1232, 1285 | edenTiger_EMP | Warhead@TeslaChargedWeaponPercentage |
| mods/cameo/weapons/outpost2.yaml | 1235, 1314 | edenTiger_EMP | Warhead@TeslaWeaponPercentage |
| mods/cameo/weapons/outpost2.yaml | 1238, 1339 | edenTiger_EMP | Warhead@MediumFlameWeaponPercentage |
| mods/cameo/weapons/outpost2.yaml | 1241, 1417 | edenTiger_EMP | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/weapons/outpost2.yaml | 1244, 1441 | edenTiger_EMP | Warhead@HeavyCannonPercentage |
| mods/cameo/weapons/outpost2.yaml | 1247, 1371 | edenTiger_EMP | Warhead@MediumCannonPercentage |
| mods/cameo/weapons/outpost2.yaml | 1250, 1463 | edenTiger_EMP | Warhead@MediumMissilePercentage |
| mods/cameo/weapons/outpost2.yaml | 1253, 1394 | edenTiger_EMP | Warhead@HeavyMissilePercentage |
| mods/cameo/weapons/outpost2.yaml | 1256, 1486 | edenTiger_EMP | Warhead@EMPCompatibility |
| mods/cameo/weapons/outpost2.yaml | 1498, 1529 | edenTiger_EMP_AA | Projectile |
| mods/cameo/weapons/outpost2.yaml | 1501, 1523 | edenTiger_EMP_AA | Warhead@Tesla_Super |
| mods/cameo/weapons/outpost2.yaml | 1503, 1548 | edenTiger_EMP_AA | Warhead@TeslaChargedWeaponPercentage |
| mods/cameo/weapons/outpost2.yaml | 1505, 1577 | edenTiger_EMP_AA | Warhead@TeslaWeaponPercentage |
| mods/cameo/weapons/outpost2.yaml | 1507, 1602 | edenTiger_EMP_AA | Warhead@MediumFlameWeaponPercentage |
| mods/cameo/weapons/outpost2.yaml | 1509, 1680 | edenTiger_EMP_AA | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/weapons/outpost2.yaml | 1511, 1704 | edenTiger_EMP_AA | Warhead@HeavyCannonPercentage |
| mods/cameo/weapons/outpost2.yaml | 1513, 1634 | edenTiger_EMP_AA | Warhead@MediumCannonPercentage |
| mods/cameo/weapons/outpost2.yaml | 1515, 1726 | edenTiger_EMP_AA | Warhead@MediumMissilePercentage |
| mods/cameo/weapons/outpost2.yaml | 1517, 1657 | edenTiger_EMP_AA | Warhead@HeavyMissilePercentage |
| mods/cameo/weapons/outpost2.yaml | 1519, 1749 | edenTiger_EMP_AA | Warhead@EMPCompatibility |
| mods/cameo/weapons/outpost2.yaml | 1765, 1798 | eden_GP_EMP | Warhead@Tesla_Super |
| mods/cameo/weapons/outpost2.yaml | 1768, 1824 | eden_GP_EMP | Warhead@TeslaChargedWeaponPercentage |
| mods/cameo/weapons/outpost2.yaml | 1771, 1848 | eden_GP_EMP | Warhead@ShieldHit |
| mods/cameo/weapons/outpost2.yaml | 1773, 1853 | eden_GP_EMP | Warhead@TeslaWeaponPercentage |
| mods/cameo/weapons/outpost2.yaml | 1776, 1878 | eden_GP_EMP | Warhead@MediumFlameWeaponPercentage |
| mods/cameo/weapons/outpost2.yaml | 1779, 1957 | eden_GP_EMP | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/weapons/outpost2.yaml | 1782, 1981 | eden_GP_EMP | Warhead@HeavyCannonPercentage |
| mods/cameo/weapons/outpost2.yaml | 1785, 1910 | eden_GP_EMP | Warhead@MediumCannonPercentage |
| mods/cameo/weapons/outpost2.yaml | 1788, 2003 | eden_GP_EMP | Warhead@MediumMissilePercentage |
| mods/cameo/weapons/outpost2.yaml | 1791, 1933 | eden_GP_EMP | Warhead@HeavyMissilePercentage |
| mods/cameo/weapons/outpost2.yaml | 1794, 2026 | eden_GP_EMP | Warhead@EMPCompatibility |
| mods/cameo/weapons/outpost2.yaml | 2158, 2164 | plymouthTigerRPG | Projectile |
| mods/cameo/weapons/outpost2.yaml | 2192, 2196 | plymouthDefenceRPG | Projectile |
| mods/cameo/weapons/outpost2.yaml | 2232, 2261 | plymouthSticky | Projectile |
| mods/cameo/weapons/outpost2.yaml | 2237, 2265 | plymouthSticky | Warhead@LightChemicalWeaponPercentage |
| mods/cameo/weapons/outpost2.yaml | 2240, 2295 | plymouthSticky | Warhead@TankDestroyerCannonPercentage |
| mods/cameo/weapons/outpost2.yaml | 2322, 2340 | plymouthStickyTiger | Warhead@LightChemicalWeaponPercentage |
| mods/cameo/weapons/outpost2.yaml | 2331, 2365 | plymouthStickyTiger | Warhead@MediumChemicalWeaponPercentage |
| mods/cameo/weapons/outpost2.yaml | 2332, 2397 | plymouthStickyTiger | Warhead@TankDestroyerCannonPercentage |
| mods/cameo/weapons/outpost2.yaml | 2333, 2423 | plymouthStickyTiger | Projectile |
| mods/cameo/weapons/outpost2.yaml | 2448, 2496 | plymouthStickyDefence | Warhead@LightChemicalWeaponPercentage |
| mods/cameo/weapons/outpost2.yaml | 2454, 2521 | plymouthStickyDefence | Warhead@MediumChemicalWeaponPercentage |
| mods/cameo/weapons/outpost2.yaml | 2460, 2469 | plymouthStickyDefence | Warhead@HeavyChemicalWeaponPercentage |
| mods/cameo/weapons/outpost2.yaml | 2461, 2548 | plymouthStickyDefence | Warhead@TankDestroyerCannonPercentage |
| mods/cameo/weapons/outpost2.yaml | 2462, 2573 | plymouthStickyDefence | Projectile |
| mods/cameo/weapons/outpost2.yaml | 2742, 2851 | plymouth_EMP | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/weapons/outpost2.yaml | 2751, 2810 | plymouth_EMP | Warhead@HeavyCannonPercentage |
| mods/cameo/weapons/outpost2.yaml | 2769, 2830 | plymouth_EMP | Warhead@MediumMissilePercentage |
| mods/cameo/weapons/outpost2.yaml | 2881, 2903 | plymouth_EMP_AA | Warhead@Tesla_Super |
| mods/cameo/weapons/outpost2.yaml | 2883, 2929 | plymouth_EMP_AA | Warhead@TeslaChargedWeaponPercentage |
| mods/cameo/weapons/outpost2.yaml | 2885, 2958 | plymouth_EMP_AA | Warhead@TeslaWeaponPercentage |
| mods/cameo/weapons/outpost2.yaml | 2887, 2983 | plymouth_EMP_AA | Warhead@MediumFlameWeaponPercentage |
| mods/cameo/weapons/outpost2.yaml | 2889, 3061 | plymouth_EMP_AA | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/weapons/outpost2.yaml | 2891, 3085 | plymouth_EMP_AA | Warhead@HeavyCannonPercentage |
| mods/cameo/weapons/outpost2.yaml | 2893, 3015 | plymouth_EMP_AA | Warhead@MediumCannonPercentage |
| mods/cameo/weapons/outpost2.yaml | 2895, 3107 | plymouth_EMP_AA | Warhead@MediumMissilePercentage |
| mods/cameo/weapons/outpost2.yaml | 2897, 3038 | plymouth_EMP_AA | Warhead@HeavyMissilePercentage |
| mods/cameo/weapons/outpost2.yaml | 2899, 3130 | plymouth_EMP_AA | Warhead@EMPCompatibility |
| mods/cameo/weapons/outpost2.yaml | 3146, 3179 | plymouth_Tiger_EMP | Warhead@Tesla_Super |
| mods/cameo/weapons/outpost2.yaml | 3149, 3205 | plymouth_Tiger_EMP | Warhead@TeslaChargedWeaponPercentage |
| mods/cameo/weapons/outpost2.yaml | 3152, 3229 | plymouth_Tiger_EMP | Warhead@ShieldHit |
| mods/cameo/weapons/outpost2.yaml | 3154, 3234 | plymouth_Tiger_EMP | Warhead@TeslaWeaponPercentage |
| mods/cameo/weapons/outpost2.yaml | 3157, 3259 | plymouth_Tiger_EMP | Warhead@MediumFlameWeaponPercentage |
| mods/cameo/weapons/outpost2.yaml | 3160, 3338 | plymouth_Tiger_EMP | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/weapons/outpost2.yaml | 3163, 3362 | plymouth_Tiger_EMP | Warhead@HeavyCannonPercentage |
| mods/cameo/weapons/outpost2.yaml | 3166, 3291 | plymouth_Tiger_EMP | Warhead@MediumCannonPercentage |
| mods/cameo/weapons/outpost2.yaml | 3169, 3384 | plymouth_Tiger_EMP | Warhead@MediumMissilePercentage |
| mods/cameo/weapons/outpost2.yaml | 3172, 3314 | plymouth_Tiger_EMP | Warhead@HeavyMissilePercentage |
| mods/cameo/weapons/outpost2.yaml | 3175, 3407 | plymouth_Tiger_EMP | Warhead@EMPCompatibility |
| mods/cameo/weapons/redalert2.yaml | 2723, 2731 | LightningBolt | Warhead@TeslaChargedExtraDamage |
| mods/cameo/weapons/redalert2mod.yaml | 18, 63, 64 | AsianHowitzerSplash | Warhead@Concussion_Medium |
| mods/cameo/weapons/redalert2mod.yaml | 77, 123, 124 | RA2KirovHowitzerSplash | Warhead@Concussion_Medium |
| mods/cameo/weapons/redalert2mod.yaml | 267, 276 | AsianSmallTorpedo | Warhead@Effect |
| mods/cameo/weapons/redalert2mod.yaml | 272, 280, 281 | AsianSmallTorpedo | Warhead@MissileAP_Heavy_Flat |
| mods/cameo/weapons/redalert2mod.yaml | 567, 590 | SteelTwisterMissiles | Projectile |
| mods/cameo/weapons/redalert2mod.yaml | 574, 600 | SteelTwisterMissiles | Warhead@Effect |
| mods/cameo/weapons/redalert2mod.yaml | 583, 630 | SteelTwisterMissiles | Warhead@GrenadePercentage |
| mods/cameo/weapons/redalert2mod.yaml | 585, 687 | SteelTwisterMissiles | Warhead@MediumFlameWeaponPercentage |
| mods/cameo/weapons/redalert2mod.yaml | 586, 715 | SteelTwisterMissiles | Warhead@RA2Scorch |
| mods/cameo/weapons/redalert2mod.yaml | 587, 656 | SteelTwisterMissiles | Warhead@GroundFire |
| mods/cameo/weapons/redalert2mod.yaml | 588, 605 | SteelTwisterMissiles | Warhead@FlakWeaponPercentage |
| mods/cameo/weapons/redalert2mod.yaml | 589, 662 | SteelTwisterMissiles | Warhead@HeavyAAWeaponPercentage |
| mods/cameo/weapons/redalert2mod.yaml | 739, 793 | Support_EMP_Bomb | Warhead@Tesla_Heavy_ExtraDamage |
| mods/cameo/weapons/redalert2mod.yaml | 891, 965, 966 | NaxGrilleArty | Warhead@CannonHE_Heavy |
| mods/cameo/weapons/redalert2mod.yaml | 935, 942 | NaxGrilleArty | Warhead@RA2Crater |
| mods/cameo/weapons/redalert2mod.yaml | 936, 937 | NaxGrilleArty | Warhead@EffectAir |
| mods/cameo/weapons/redalert2mod.yaml | 945, 954 | NaxGrilleArty | Warhead@EffectWater |
| mods/cameo/weapons/redalert2mod.yaml | 984, 1006 | NaxGrilleArty_elite | Warhead@Effect |
| mods/cameo/weapons/redalert2mod.yaml | 987, 1005, 1010 | NaxGrilleArty_elite | Warhead@EffectWater |
| mods/cameo/weapons/redalert2mod.yaml | 995, 1002 | NaxGrilleArty_elite | Warhead@RA2Crater |
| mods/cameo/weapons/redalert2mod.yaml | 996, 997 | NaxGrilleArty_elite | Warhead@EffectAir |
| mods/cameo/weapons/redalert2mod.yaml | 1014, 1056 | NaxGrilleArty_elite | Warhead@CannonHE_Heavy |
| mods/cameo/weapons/redalert2mod.yaml | 1141, 1159 | NaxLaserT | Projectile |
| mods/cameo/weapons/redalert2mod.yaml | 1152, 1247 | NaxLaserT | Warhead@SmallArmsPercentage |
| mods/cameo/weapons/redalert2mod.yaml | 1153, 1216, 1272 | NaxLaserT | Warhead@EffectWater |
| mods/cameo/weapons/redalert2mod.yaml | 1154, 1183 | NaxLaserT | Warhead@ChaingunPercentage |
| mods/cameo/weapons/redalert2mod.yaml | 1155, 1220 | NaxLaserT | Warhead@MediumMissilePercentage |
| mods/cameo/weapons/redalert2mod.yaml | 1156, 1207 | NaxLaserT | Warhead@DuneRock |
| mods/cameo/weapons/redalert2mod.yaml | 1157, 1210 | NaxLaserT | Warhead@DuneSand |
| mods/cameo/weapons/redalert2mod.yaml | 1158, 1244 | NaxLaserT | Warhead@RA2Crater |
| mods/cameo/weapons/redalert2mod.yaml | 1301, 1436, 1448 | NaxiMP40 | Warhead@Concussion_Light |
| mods/cameo/weapons/redalert2mod.yaml | 1344, 1437, 1438 | NaxiMP40 | Warhead@CannonHE_Heavy |
| mods/cameo/weapons/redalert2mod.yaml | 1389, 1408 | NaxiMP40 | Warhead@Glow |
| mods/cameo/weapons/redalert2mod.yaml | 1390, 1415 | NaxiMP40 | Warhead@Smudge |
| mods/cameo/weapons/redalert2mod.yaml | 1391, 1394 | NaxiMP40 | Warhead@DuneRock |
| mods/cameo/weapons/redalert2mod.yaml | 1392, 1397 | NaxiMP40 | Warhead@DuneSand |
| mods/cameo/weapons/redalert2mod.yaml | 1393, 1412 | NaxiMP40 | Warhead@RA2Crater |
| mods/cameo/weapons/redalert2mod.yaml | 1400, 1419, 1423 | NaxiMP40 | Warhead@Effect |
| mods/cameo/weapons/redalert2mod.yaml | 1402, 1420, 1427 | NaxiMP40 | Warhead@EffectAir |
| mods/cameo/weapons/redalert2mod.yaml | 1406, 1418, 1430 | NaxiMP40 | Warhead@EffectWater |
| mods/cameo/weapons/redalert2mod.yaml | 1482, 1580, 1581 | NaxSturmArty | Warhead@Demolition_Heavy |
| mods/cameo/weapons/redalert2mod.yaml | 1527, 1555 | NaxSturmArty | Warhead@Smudge1 |
| mods/cameo/weapons/redalert2mod.yaml | 1528, 1552 | NaxSturmArty | Warhead@RA2Crater |
| mods/cameo/weapons/redalert2mod.yaml | 1529, 1558 | NaxSturmArty | Warhead@Smudge2 |
| mods/cameo/weapons/redalert2mod.yaml | 1530, 1563 | NaxSturmArty | Warhead@Smudge2RA2 |
| mods/cameo/weapons/redalert2mod.yaml | 1531, 1534 | NaxSturmArty | Warhead@Effect1 |
| mods/cameo/weapons/redalert2mod.yaml | 1532, 1539 | NaxSturmArty | Warhead@Effect2 |
| mods/cameo/weapons/redalert2mod.yaml | 1533, 1547 | NaxSturmArty | Warhead@EffectAir |
| mods/cameo/weapons/redalert2mod.yaml | 1704, 1705 | NaxTorpTube | Warhead@MissileAP_Heavy_Flat |
| mods/cameo/weapons/redalert2mod.yaml | 1774, 1775 | NaxiJadgDestroyerCorrosion | Warhead@CannonAP_Light_Flat |
| mods/cameo/weapons/redalert2mod.yaml | 1845, 1885 | NaxiAlienPistol | Warhead@Tesla_Heavy_ExtraDamage |
| mods/cameo/weapons/redalert2mod.yaml | 1881, 1882 | NaxiAlienPistol | Warhead@EMPUnit |
| mods/cameo/weapons/redalert2mod.yaml | 1987, 2030 | NaxiMeteor | Projectile |
| mods/cameo/weapons/redalert2mod.yaml | 2015, 2049 | NaxiMeteor | Warhead@GrenadePercentage |
| mods/cameo/weapons/redalert2mod.yaml | 2017, 2105 | NaxiMeteor | Warhead@MediumFlameWeaponPercentage |
| mods/cameo/weapons/redalert2mod.yaml | 2018, 2158 | NaxiMeteor | Warhead@RA2Scorch |
| mods/cameo/weapons/redalert2mod.yaml | 2019, 2074 | NaxiMeteor | Warhead@GroundFire |
| mods/cameo/weapons/redalert2mod.yaml | 2020, 2164 | NaxiMeteor | Warhead@ShrapnelWeaponPercentage |
| mods/cameo/weapons/redalert2mod.yaml | 2021, 2080 | NaxiMeteor | Warhead@HeavyBombPercentage |
| mods/cameo/weapons/redalert2mod.yaml | 2022, 2189 | NaxiMeteor | Warhead@Smudge1 |
| mods/cameo/weapons/redalert2mod.yaml | 2023, 2194 | NaxiMeteor | Warhead@Smudge2 |
| mods/cameo/weapons/redalert2mod.yaml | 2024, 2199 | NaxiMeteor | Warhead@Smudge2RA2 |
| mods/cameo/weapons/redalert2mod.yaml | 2025, 2036 | NaxiMeteor | Warhead@Effect1 |
| mods/cameo/weapons/redalert2mod.yaml | 2026, 2041 | NaxiMeteor | Warhead@Effect2 |
| mods/cameo/weapons/redalert2mod.yaml | 2027, 2132 | NaxiMeteor | Warhead@NuclearWarheadPercentage |
| mods/cameo/weapons/redalert2mod.yaml | 2028, 2204 | NaxiMeteor | Warhead@Smudge3 |
| mods/cameo/weapons/redalert2mod.yaml | 2029, 2161 | NaxiMeteor | Warhead@ShieldHitEffectNuclear |
| mods/cameo/weapons/redalert2mod.yaml | 2282, 2414, 2415 | NaxisBlackBombSmaller | Warhead@Demolition_Medium |
| mods/cameo/weapons/redalert2mod.yaml | 2329, 2374 | NaxisBlackBombSmaller | Warhead@LightChemicalWeaponPercentage |
| mods/cameo/weapons/redalert2mod.yaml | 2330, 2349 | NaxisBlackBombSmaller | Warhead@HeavyBombPercentage |
| mods/cameo/weapons/redalert2mod.yaml | 2331, 2401 | NaxisBlackBombSmaller | Warhead@Smudge1 |
| mods/cameo/weapons/redalert2mod.yaml | 2332, 2404 | NaxisBlackBombSmaller | Warhead@Smudge2 |
| mods/cameo/weapons/redalert2mod.yaml | 2333, 2409 | NaxisBlackBombSmaller | Warhead@Smudge2RA2 |
| mods/cameo/weapons/redalert2mod.yaml | 2334, 2336 | NaxisBlackBombSmaller | Warhead@Effect1 |
| mods/cameo/weapons/redalert2mod.yaml | 2335, 2341 | NaxisBlackBombSmaller | Warhead@Effect2 |
| mods/cameo/weapons/redalert2mod.yaml | 2443, 2480 | Lunar_Green105mm | Projectile |
| mods/cameo/weapons/redalert2mod.yaml | 2448, 2493 | Lunar_Green105mm | Warhead@Tesla_Heavy_ExtraDamage |
| mods/cameo/weapons/redalert2mod.yaml | 2476, 2492, 2498 | Lunar_Green105mm | Warhead@Tesla_Heavy_Flat |
| mods/cameo/weapons/redalert2mod.yaml | 2488, 2489 | Lunar_Green105mm | Warhead@EMPUnit |
| mods/cameo/weapons/redalert2mod.yaml | 2563, 2608, 2609 | SyndicateFireballLauncher | Warhead@Flame_Heavy |
| mods/cameo/weapons/redalert2mod.yaml | 2644, 2776, 2783 | LatinBuggyRocket | Projectile |
| mods/cameo/weapons/redalert2mod.yaml | 2772, 2785, 2786 | LatinBuggyRocket | Warhead@MissileAP_Medium_Flat |
| mods/cameo/weapons/redalert2mod.yaml | 2842, 2844 | LatinBuggyRocket_elite | Projectile |
| mods/cameo/weapons/redalert2mod.yaml | 2846, 2847 | LatinBuggyRocket_elite | Warhead@MissileAP_Medium_Flat |
| mods/cameo/weapons/redalert2mod.yaml | 2973, 3146 | 12MissilesSpawnerScud | Projectile |
| mods/cameo/weapons/redalert2mod.yaml | 2985, 3177, 3180 | 12MissilesSpawnerScud | Warhead@Demolition_Heavy |
| mods/cameo/weapons/redalert2mod.yaml | 3031, 3179, 3187 | 12MissilesSpawnerScud | Warhead@Demolition_Light |
| mods/cameo/weapons/redalert2mod.yaml | 3077, 3178, 3194 | 12MissilesSpawnerScud | Warhead@Flame_Medium |
| mods/cameo/weapons/redalert2mod.yaml | 3128, 3154 | 12MissilesSpawnerScud | Warhead@Effect |
| mods/cameo/weapons/redalert2mod.yaml | 3140, 3174 | 12MissilesSpawnerScud | Warhead@Smudge |
| mods/cameo/weapons/redalert2mod.yaml | 3141, 3171 | 12MissilesSpawnerScud | Warhead@RA2Scorch |
| mods/cameo/weapons/redalert2mod.yaml | 3142, 3165 | 12MissilesSpawnerScud | Warhead@GroundFire |
| mods/cameo/weapons/redalert2mod.yaml | 3143, 3148 | 12MissilesSpawnerScud | Warhead@DuneRock |
| mods/cameo/weapons/redalert2mod.yaml | 3144, 3151 | 12MissilesSpawnerScud | Warhead@DuneSand |
| mods/cameo/weapons/redalert2mod.yaml | 3145, 3157 | 12MissilesSpawnerScud | Warhead@EffectAir |
| mods/cameo/weapons/redalert2mod.yaml | 3227, 3301 | RA2Robotmm | Warhead@Tesla_Heavy_ExtraDamage |
| mods/cameo/weapons/redalert2mod.yaml | 3258, 3294, 3295 | RA2Robotmm | Warhead@Laser_Heavy_ExtraDamage |
| mods/cameo/weapons/redalert2mod.yaml | 3283, 3287, 3290, 3291 | RA2Robotmm | Warhead@EMPUnit |
| mods/cameo/weapons/redalert2mod.yaml | 3323, 3390 | RA2Robotmm_elite | Warhead@Tesla_Heavy_ExtraDamage |
| mods/cameo/weapons/redalert2mod.yaml | 3348, 3383, 3384 | RA2Robotmm_elite | Warhead@Laser_Heavy_ExtraDamage |
| mods/cameo/weapons/redalert2mod.yaml | 3373, 3379, 3380 | RA2Robotmm_elite | Warhead@EMPUnit |
| mods/cameo/weapons/redalert2mod.yaml | 3401, 3481 | RA2RobotmmScatter_elite | Warhead@Tesla_Heavy_ExtraDamage |
| mods/cameo/weapons/redalert2mod.yaml | 3441, 3474, 3475 | RA2RobotmmScatter_elite | Warhead@Laser_Heavy_ExtraDamage |
| mods/cameo/weapons/redalert2mod.yaml | 3466, 3470, 3471 | RA2RobotmmScatter_elite | Warhead@EMPUnit |
| mods/cameo/weapons/redalert2mod.yaml | 3501, 3506, 3507 | naxis_sssoldier_smg | Warhead@Bullet_Medium_Flat |
| mods/cameo/weapons/redalert2mod.yaml | 3560, 3564, 3565 | naxis_sssoldier_smg_elite | Warhead@Bullet_Medium_Flat |
| mods/cameo/weapons/shockwave.yaml | 1907, 1922 | SGLAngryMobMolotov | Warhead@3Eff |
| mods/cameo/weapons/sow.yaml | 28, 36 | ^SowFlame | ValidTargets |
| mods/cameo/weapons/starcraft2.yaml | 7, 18 | zealotPsionicBlades > Warhead@1Dam | Spread |
| mods/cameo/weapons/starcraft2.yaml | 8, 19 | zealotPsionicBlades > Warhead@1Dam | Damage |
| mods/cameo/weapons/starcraft2.yaml | 9, 21 | zealotPsionicBlades > Warhead@1Dam | Versus |
| mods/cameo/weapons/starwars.yaml | 814, 818 | SWNapalm | Burst |
| mods/cameo/weapons/starwars.yaml | 843, 847 | SWNapalm2 | Burst |
| mods/cameo/weapons/starwars.yaml | 867, 871 | SWNapalm3 | Burst |
| mods/cameo/weapons/warcraft2.yaml | 760, 772 | wc2_tower_axe | Projectile |
| mods/cameo/weapons/weapons.yaml | 11896, 11910 | RockDebris2 | Projectile |
| mods/cameo/weapons/weapons.yaml | 11902, 11921 | RockDebris2 | Warhead@Effect |
| mods/cameo/weapons/weapons.yaml | 11925, 11933 | RockDebris3 | Projectile |
| mods/cameo/weapons/weapons.yaml | 11953, 11961 | RockDebris4 | Projectile |
| mods/cameo/weapons/weapons.yaml | 12621, 12641 | GLBarrelExplode | Warhead@1Dam |
| mods/cameo/weapons/weapons.yaml | 12695, 12759 | GLTerroristExplosive | Warhead@1Dam |
| mods/cameo/weapons/weapons.yaml | 12699, 12770 | GLTerroristExplosive | Warhead@3Smu |
| mods/cameo/weapons/weapons.yaml | 12777, 12837 | GLTerroristExplosive2 | Warhead@1Dam |
| mods/cameo/weapons/weapons.yaml | 13017, 13021 | GLAnthraxBlue | Warhead@Cloud |
| mods/cameo/weapons/weapons.yaml | 13030, 13034 | GLAnthraxPurple | Warhead@Cloud |
| mods/cameo/weapons/weapons.yaml | 13043, 13047 | GLAnthraxLarge | Warhead@Cloud |
| mods/cameo/weapons/weapons.yaml | 13056, 13060 | GLAnthraxBlueLarge | Warhead@Cloud |
| mods/cameo/weapons/weapons.yaml | 13069, 13073 | GLAnthraxPurpleLarge | Warhead@Cloud |
| mods/cameo/weapons/weapons.yaml | 13086, 13090 | AnthraxCloudLarge | Warhead@Toxic_Light |
| mods/cameo/weapons/weapons.yaml | 13098, 13102 | AnthraxCloudBlue | Warhead@Toxic_Light |
| mods/cameo/weapons/weapons.yaml | 13111, 13115 | AnthraxCloudBlueLarge | Warhead@Toxic_Light |
| mods/cameo/weapons/weapons.yaml | 13123, 13127 | AnthraxCloudPurple | Warhead@Toxic_Light |
| mods/cameo/weapons/weapons.yaml | 13136, 13140 | AnthraxCloudPurpleLarge | Warhead@Toxic_Light |
| mods/cameo/weapons/weapons.yaml | 13171, 13181 | GiantBowlingTurret | Projectile |
| mods/cameo/weapons/weapons.yaml | 13175, 13187 | GiantBowlingTurret | Warhead@1Dam |
| mods/cameo/weapons/weapons.yaml | 13466, 13501 | Spit_AA | Projectile |
| mods/cameo/weapons/weapons.yaml | 13470, 13476 | Spit_AA | Warhead@1Dam |
| mods/cameo/weapons/weapons.yaml | 13872, 13937 | GLASCUDPOWER2 | Warhead@3Eff |
| mods/cameo/weapons/weapons.yaml | 13874, 13949 | GLASCUDPOWER2 | Warhead@4EffWater |
| mods/cameo/weapons/weapons.yaml | 13876, 13935 | GLASCUDPOWER2 | Warhead@1Dam |
| mods/cameo/weapons/weapons.yaml | 13966, 14035 | GLASCUDPOWER3 | Warhead@3Eff |
| mods/cameo/weapons/weapons.yaml | 13968, 14047 | GLASCUDPOWER3 | Warhead@4EffWater |
| mods/cameo/weapons/weapons.yaml | 13970, 14033 | GLASCUDPOWER3 | Warhead@1Dam |
| mods/cameo/weapons/weapons.yaml | 14187, 14196 | GLToxinExplodeClust2 | Warhead@1Dam |
| mods/cameo/weapons/weapons.yaml | 14189, 14198 | GLToxinExplodeClust2 | Warhead@3Clu |
| mods/cameo/weapons/weapons.yaml | 14202, 14211 | GLToxinExplodeClust3 | Warhead@1Dam |
| mods/cameo/weapons/weapons.yaml | 14204, 14213 | GLToxinExplodeClust3 | Warhead@3Clu |
| mods/cameo/weapons/weapons.yaml | 14388, 14472 | GLToxinBombBlue | Warhead@Cluster |
| mods/cameo/weapons/weapons.yaml | 14482, 14566 | GLToxinBombPurple | Warhead@Cluster |
| mods/cameo/weapons/weapons.yaml | 14570, 14572 | TractorGLAnthraxGreen | Warhead@Cloud |
| mods/cameo/weapons/weapons.yaml | 14583, 14585 | TractorGLAnthraxBlue | Warhead@Cloud |
| mods/cameo/weapons/weapons.yaml | 14596, 14598 | TractorGLAnthraxPurple | Warhead@Cloud |
| mods/cameo/weapons/weapons.yaml | 14614, 14618 | TSCyCannonCluster | Warhead@Cloud |
| mods/cameo/weapons/weapons.yaml | 14639, 14648 | ThermobaricFlame | Warhead@1Dam |
| mods/cameo/weapons/weapons.yaml | 14718, 14774 | Short8Inch | Projectile |
| mods/cameo/weapons/weapons.yaml | 14830, 14844 | bowFire | Warhead@1Dam |
| mods/cameo/weapons/weapons.yaml | 14835, 14854 | bowFire | Warhead@Percentage |
| mods/cameo/weapons/weapons.yaml | 14868, 14895 | wc_tower_fire | Projectile |
| mods/cameo/weapons/weapons.yaml | 14872, 14898 | wc_tower_fire | Warhead@1Dam |
| mods/cameo/weapons/weapons.yaml | 14882, 14903 | wc_tower_fire | Warhead@Percentage |
| mods/cameo/weapons/weapons.yaml | 15024, 15053 | SWGreenLaserG | Warhead@Percentage |
| mods/cameo/weapons/weapons.yaml | 15103, 15133 | SWGBigRedLaserG | Warhead@Percentage |
| mods/cameo/weapons/weapons.yaml | 15211, 15232 | GLRebelToxinGarrison | Warhead@1Dam |
| mods/cameo/weapons/weapons.yaml | 15259, 15278 | GLTrooperRPGMissileG | Warhead@1Dam |
| mods/cameo/weapons/weapons.yaml | 15380, 15397 | Fremen_L | Warhead@1Dam |
| mods/cameo/weapons/weapons.yaml | 15391, 15409 | Fremen_L | Warhead@2Eff |
| mods/cameo/weapons/wh40k.yaml | 354, 357 | WH40KShootaBoyzGun | Warhead@1Dam |


**FAIL** — D2 count 3963 exceeds the baseline 260: a new duplicate key was introduced.

