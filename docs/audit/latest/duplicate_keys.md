# audit_duplicate_keys — duplicate keys in one node (ambiguous merges)

Files scanned: **700** — D1 ambiguous inheritance labels: **0**, D2 merged duplicates: **222**


## D1 — duplicate inheritance labels with different parent values

_none found_


## D2 — duplicate keys by key name (top 40)

| key | occurrences |
|---|---|
| Projectile | 67 |
| Range | 17 |
| ReloadDelay | 14 |
| ValidTargets | 9 |
| Warhead@Effect | 7 |
| Warhead@Bullet_Medium_Flat | 7 |
| Warhead@ShieldHit | 6 |
| Report | 6 |
| Burst | 6 |
| Warhead@Concrete | 5 |
| Warhead@EffectAir | 4 |
| Warhead@ShieldHitEffect | 4 |
| BurstDelays | 3 |
| Warhead@EffectWater | 3 |
| Warhead@Plasma_Heavy_Flat | 3 |
| Warhead@Smudge | 3 |
| Warhead@DuneRock | 3 |
| Warhead@DuneSand | 3 |
| Warhead@CannonHE_Medium | 3 |
| Warhead@Shrapnel | 2 |
| Warhead@Demolition_Light | 2 |
| Warhead@GrenadePercentage | 2 |
| Warhead@ChaingunPercentage | 2 |
| Warhead@Glow | 2 |
| Warhead@RA2Crater | 2 |
| Defaults | 2 |
| Voiced | 2 |
| AutoTarget | 2 |
| RenderSprites | 2 |
| Warhead@1Dam | 1 |
| Warhead@Bullet_Medium | 1 |
| Warhead@OwnerChange | 1 |
| Warhead@TankDestroyerCannonPercentage | 1 |
| Warhead@LightChemicalWeaponPercentage | 1 |
| Warhead@FlakWeaponPercentage | 1 |
| Warhead@MediumMissilePercentage | 1 |
| Warhead@HeavyBombPercentage | 1 |
| Warhead@Heal | 1 |
| Warhead@MediumChemicalWeaponPercentage | 1 |
| Warhead@MissileAP_Light | 1 |


## D2 — full list

| file | lines | node | key |
|---|---|---|---|
| mods/cameo/ContentPacks/D2k/Atreides/yaml/weapons.yaml | 202, 210 | PhoenixRocket | Warhead@1Dam |
| mods/cameo/ContentPacks/D2k/Atreides/yaml/weapons.yaml | 203, 226 | PhoenixRocket | Warhead@Shrapnel |
| mods/cameo/ContentPacks/D2k/Harkonnen/yaml/weapons.yaml | 180, 191 | ExplosiveDebris | Projectile |
| mods/cameo/ContentPacks/D2k/Harkonnen/yaml/weapons.yaml | 183, 198 | ExplosiveDebris | Warhead@Demolition_Light |
| mods/cameo/ContentPacks/D2k/Harkonnen/yaml/weapons.yaml | 258, 276 | D2K_Rocket_AA | Projectile |
| mods/cameo/ContentPacks/D2k/Ixian/yaml/weapons.yaml | 1731, 1739 | HMG_Duelist_upgrade | Warhead@ShieldHit |
| mods/cameo/ContentPacks/D2k/Ixian/yaml/weapons.yaml | 2360, 2372 | d2k_air_drone_guns | Warhead@Bullet_Medium |
| mods/cameo/ContentPacks/D2k/Ordos/yaml/weapons.yaml | 1972, 1991 | D2K_Rocket_Trooper_AA | Projectile |
| mods/cameo/ContentPacks/D2k/Ordos/yaml/weapons.yaml | 2222, 2390 | D2K_APC_Rocket_AA | Projectile |
| mods/cameo/ContentPacks/D2k/Ordos/yaml/weapons.yaml | 2502, 2519 | DeviatorMissile | Warhead@OwnerChange |
| mods/cameo/ContentPacks/D2k/Ordos/yaml/weapons.yaml | 4019, 4049 | ordos_airmine | Warhead@TankDestroyerCannonPercentage |
| mods/cameo/ContentPacks/D2k/Ordos/yaml/weapons.yaml | 4020, 4089 | ordos_airmine | Warhead@LightChemicalWeaponPercentage |
| mods/cameo/ContentPacks/D2k/Ordos/yaml/weapons.yaml | 4021, 4132 | ordos_airmine | Warhead@FlakWeaponPercentage |
| mods/cameo/ContentPacks/D2k/Ordos/yaml/weapons.yaml | 4022, 4172 | ordos_airmine | Warhead@MediumMissilePercentage |
| mods/cameo/ContentPacks/D2k/Ordos/yaml/weapons.yaml | 4023, 4212 | ordos_airmine | Warhead@HeavyBombPercentage |
| mods/cameo/ContentPacks/D2k/Ordos/yaml/weapons.yaml | 4024, 4253 | ordos_airmine | Warhead@Shrapnel |
| mods/cameo/ContentPacks/D2k/Ordos/yaml/weapons.yaml | 5126, 5137 | D2K_Annihilator_AA | Projectile |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 2497, 2536 | Hakureiring2 | ValidTargets |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 6325, 6327 | NanoReturnHeal | Warhead@Heal |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 11625, 11736 | ra1_soviets_volkov_volkovmagneticweaponincendiarytesla | ReloadDelay |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 11626, 11737 | ra1_soviets_volkov_volkovmagneticweaponincendiarytesla | Range |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 11628, 11738 | ra1_soviets_volkov_volkovmagneticweaponincendiarytesla | BurstDelays |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 11630, 11739 | ra1_soviets_volkov_volkovmagneticweaponincendiarytesla | Projectile |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 11640, 11744 | ra1_soviets_volkov_volkovmagneticweaponincendiarytesla | Warhead@MediumChemicalWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 11682, 11792 | ra1_soviets_volkov_volkovmagneticweaponincendiarytesla | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 11722, 11912 | ra1_soviets_volkov_volkovmagneticweaponincendiarytesla | Warhead@Concrete |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 11724, 11914 | ra1_soviets_volkov_volkovmagneticweaponincendiarytesla | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 11729, 11928 | ra1_soviets_volkov_volkovmagneticweaponincendiarytesla | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 11731, 11858 | ra1_soviets_volkov_volkovmagneticweaponincendiarytesla | Warhead@ShieldHit |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 13, 106 | RA2Patriot | Projectile |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 3013, 3106 | RA2Medusa_AA | Projectile |
| mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml | 3120, 3135 | RA2MedusaAG | Projectile |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 1395, 1457 | RA2HoverMissile_AA | Projectile |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 1472, 1530 | RA2HoverMissile_AA_elite | Projectile |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 1753, 1767 | RA2ThunderboltMissile_AA | Projectile |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 1787, 1801 | RA2ThunderboltMissile_AA_elite | Projectile |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 1923, 2026 | RA2MultiHoverMissile_elite | Range |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 1927, 2027 | RA2MultiHoverMissile_elite | Projectile |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 1940, 2033 | RA2MultiHoverMissile_elite | Warhead@MissileAP_Light |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 2125, 2195 | RA2MultiHoverMissile_AA | Projectile |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 2206, 2265 | RA2MultiHoverMissile_AA_elite | Projectile |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 2488, 2502 | RA2MultiThunderboltMissile_AA | Projectile |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 2522, 2536 | RA2MultiThunderboltMissile_AA_elite | Projectile |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 2553, 2573 | RA2DepthCharge | Report |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 2554, 2574 | RA2DepthCharge | ReloadDelay |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 2555, 2575 | RA2DepthCharge | Range |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 2557, 2578 | RA2DepthCharge | Projectile |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 4943, 5000 | MigMissiles_AA | Projectile |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 5016, 5073 | MigMissiles_AA_elite | Projectile |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 6414, 6624 | DredMissile | Projectile |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 6426, 6628 | DredMissile | Warhead@Demolition_Light |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 6512, 6626 | DredMissile | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 9500, 9593 | TanyaBomb | Report |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 9501, 9594 | TanyaBomb | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 9507, 9858 | TanyaBomb | Warhead@Demolition_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 10563, 10571 | RA2LargeDebris | Projectile |
| mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/weapons.yaml | 945, 950 | RA160mmE_elite | Warhead@Concussion_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/weapons.yaml | 1693, 1709 | RA2120xmm_fire_elite | Range |
| mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/weapons.yaml | 1695, 1710 | RA2120xmm_fire_elite | Burst |
| mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/weapons.yaml | 1696, 1711 | RA2120xmm_fire_elite | BurstDelays |
| mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/weapons.yaml | 1754, 1770 | RA2120xmm_tesla_elite | Range |
| mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/weapons.yaml | 1756, 1771 | RA2120xmm_tesla_elite | Burst |
| mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/weapons.yaml | 1757, 1772 | RA2120xmm_tesla_elite | BurstDelays |
| mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/weapons.yaml | 2163, 2254 | RA2MammothTusk_AA | Projectile |
| mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/weapons.yaml | 2267, 2352 | RA2MammothTusk_AA_elite | Projectile |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 3061, 3066 | YuriGatlingTankMG1 | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 3089, 3094 | YuriGatlingTankMG1_AA | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 3117, 3122 | YuriGatlingTankMG2 | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 3145, 3150 | YuriGatlingTankMG2_AA | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 3173, 3178 | YuriGatlingTankMG3 | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 3201, 3206 | YuriGatlingTankMG3_AA | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 3312, 3321 | RA2Virusgun | ValidTargets |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 3313, 3360 | RA2Virusgun | Warhead@Toxic_Light |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 3372, 3381 | RA2Virusgun2 | ValidTargets |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 3428, 3485 | RA2Virusgun3 | Warhead@Toxic_Medium |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 3442, 3498 | RA2Virusgun3 | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 3443, 3505 | RA2Virusgun3 | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 3444, 3510 | RA2Virusgun3 | Warhead@ShieldHit |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 3445, 3512 | RA2Virusgun3 | Warhead@Concrete |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 3446, 3514 | RA2Virusgun3 | Warhead@ShieldHitEffect |
| mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml | 3452, 3491 | RA2Virusgun3 | Warhead@Cloud |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 3133, 3166 | AsianQuasarAG | Projectile |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 3139, 3194 | AsianQuasarAG | Warhead@Plasma_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 3901, 3934 | AsianQuasarBoatAG | Projectile |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 3907, 3962 | AsianQuasarBoatAG | Warhead@Plasma_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 6947, 7409 | AsianTurretPlasma | Range |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 6948, 7411 | AsianTurretPlasma | ReloadDelay |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 6949, 7414 | AsianTurretPlasma | Report |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 6950, 7415 | AsianTurretPlasma | Projectile |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 6956, 7421 | AsianTurretPlasma | Warhead@FireShrapnel |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 7004, 7423 | AsianTurretPlasma | Warhead@Plasma_Heavy_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 7349, 7419 | AsianTurretPlasma | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 7357, 7520 | AsianTurretPlasma | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 7361, 7509 | AsianTurretPlasma | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 7364, 7512 | AsianTurretPlasma | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 7367, 7515 | AsianTurretPlasma | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 8369, 8389 | AsianChaosMine | Report |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 8920, 9070 | AAGunBoatFlak | ValidTargets |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 8923, 9067 | AAGunBoatFlak | Range |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 8924, 9072 | AAGunBoatFlak | Warhead@Flak_Medium |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 8928, 9077 | AAGunBoatFlak | Warhead@SmallArmsPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 8967, 9079 | AAGunBoatFlak | Warhead@GrenadePercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 9016, 9081 | AAGunBoatFlak | Warhead@ChaingunPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 9057, 9083 | AAGunBoatFlak | Warhead@Concrete |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 9059, 9085 | AAGunBoatFlak | Warhead@Glow |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 9062, 9088 | AAGunBoatFlak | Warhead@ShieldHit |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 9064, 9090 | AAGunBoatFlak | Warhead@ShieldHitEffect |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 1688, 1847 | ConsortiumMissileSystem | Projectile |
| mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 1957, 1970 | ConsortiumMissileSystem_EMP | Projectile |
| mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml | 5843, 5874 | NaxInterceptorRockets | Projectile |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 5, 29 | schwarzermond_lunarsoldier_rifle | ReloadDelay |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 6, 30 | schwarzermond_lunarsoldier_rifle | Range |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 7, 31 | schwarzermond_lunarsoldier_rifle | Report |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 9, 32 | schwarzermond_lunarsoldier_rifle | Projectile |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 755, 917 | NaxiMP40Laser | ReloadDelay |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 756, 918 | NaxiMP40Laser | Burst |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 758, 916 | NaxiMP40Laser | Range |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 759, 915 | NaxiMP40Laser | Report |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 760, 919 | NaxiMP40Laser | Projectile |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 881, 1258 | NaxiMP40Laser | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 884, 1237 | NaxiMP40Laser | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 887, 1240 | NaxiMP40Laser | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 890, 1243 | NaxiMP40Laser | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 893, 1232 | NaxiMP40Laser | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 898, 1254 | NaxiMP40Laser | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 904, 1246 | NaxiMP40Laser | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 907, 1252 | NaxiMP40Laser | Warhead@Concrete |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 909, 1256 | NaxiMP40Laser | Warhead@ShieldHit |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 911, 1261 | NaxiMP40Laser | Warhead@ShieldHitEffect |
| mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml | 8214, 8232 | Naxis_Komet_AA | Projectile |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 728, 906 | RA2FreedomAK47_elite | Warhead@SniperWeaponExtraDamage |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 729, 945 | RA2FreedomAK47_elite | Warhead@SniperWeaponPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 730, 864 | RA2FreedomAK47_elite | Warhead@Effect |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 731, 889 | RA2FreedomAK47_elite | Warhead@Effect1 |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 732, 894 | RA2FreedomAK47_elite | Warhead@Effect2 |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 733, 876 | RA2FreedomAK47_elite | Warhead@EffectWater |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 734, 882 | RA2FreedomAK47_elite | Warhead@ShieldHit |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 735, 880 | RA2FreedomAK47_elite | Warhead@Concrete |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 736, 884 | RA2FreedomAK47_elite | Warhead@ShieldHitEffect |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 737, 848 | RA2FreedomAK47_elite | Warhead@Glow |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 738, 852 | RA2FreedomAK47_elite | Warhead@Smudge |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 739, 855 | RA2FreedomAK47_elite | Warhead@DuneRock |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 740, 858 | RA2FreedomAK47_elite | Warhead@DuneSand |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 741, 861 | RA2FreedomAK47_elite | Warhead@RA2Crater |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 742, 870 | RA2FreedomAK47_elite | Warhead@EffectAir |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 743, 765 | RA2FreedomAK47_elite | Warhead@Bullet_Medium_Flat |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 744, 985 | RA2FreedomAK47_elite | Warhead@ChaingunPercentage |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 7591, 7615 | RA2APCRocket_AA | Projectile |
| mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml | 7632, 7658 | RA2APCRocket_AA_elite | Projectile |
| mods/cameo/ContentPacks/StarCraft/Protoss/yaml/weapons.yaml | 966, 984 | ScoutRockets_AA | Projectile |
| mods/cameo/ContentPacks/StarCraft/Protoss/yaml/weapons.yaml | 1545, 1552 | ManifoldMG | ValidTargets |
| mods/cameo/ContentPacks/StarCraft/Protoss/yaml/weapons.yaml | 1546, 1554 | ManifoldMG | ReloadDelay |
| mods/cameo/ContentPacks/StarCraft/Protoss/yaml/weapons.yaml | 1547, 1553 | ManifoldMG | Range |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 2132, 2150 | GoliathRockets_AA | Projectile |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 2291, 2435 | GoliathMk2Rockets_AA | Projectile |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 2463, 2481 | WraithRockets_AA | Projectile |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 2577, 2733 | ValkyrieRockets | Projectile |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 2749, 2759 | WyvernRockets | ReloadDelay |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 2750, 2758 | WyvernRockets | Range |
| mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 3561, 3580 | MissileTurret | Projectile |
| mods/cameo/ContentPacks/StarCraft/Zerg/yaml/weapons.yaml | 145, 152 | MutaliskSpore | ReloadDelay |
| mods/cameo/ContentPacks/StarCraft/Zerg/yaml/weapons.yaml | 146, 153 | MutaliskSpore | Range |
| mods/cameo/ContentPacks/StarCraft/Zerg/yaml/weapons.yaml | 467, 473 | ScourgeExplosion | Projectile |
| mods/cameo/ContentPacks/StarCraft/Zerg/yaml/weapons.yaml | 595, 601 | GuardianShoot | ReloadDelay |
| mods/cameo/ContentPacks/StarCraft/Zerg/yaml/weapons.yaml | 596, 602 | GuardianShoot | Range |
| mods/cameo/ContentPacks/StarCraft/Zerg/yaml/weapons.yaml | 1296, 1334 | Spore_AA | Projectile |
| mods/cameo/ContentPacks/StarCraft/Zerg/yaml/weapons.yaml | 1445, 1453 | Tentacle | ReloadDelay |
| mods/cameo/ContentPacks/StarCraft/Zerg/yaml/weapons.yaml | 1446, 1454 | Tentacle | Range |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 1566, 1594 | A10CarrierMissiles_AA | Projectile |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 3062, 3097 | td_gdi_humveemkii_rocketshumvee2_AA | Projectile |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 3288, 3329 | td_gdi_humveemkii_rocketshumvee2amt_AA | Projectile |
| mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml | 4868, 4896 | td_gdi_firehawk_firehawkmissiles_AA | Projectile |
| mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 125, 142 | td_nod_samsite_dragon | Projectile |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 950, 962 | CabalReaperMissiles | Projectile |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 1158, 1170, 1354 | CabalReaperMissiles_AA | Projectile |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 1452, 1464 | CabalHeavyReaperMissiles | Projectile |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 1661, 1673, 1858 | CabalHeavyReaperMissiles_AA | Projectile |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 3338, 3353 | CabalRocketCyborgRockets | Projectile |
| mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 3373, 3388 | CabalRocketCyborgRocketsUpgraded | Projectile |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 46, 54 | TS70mmTur | ReloadDelay |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 47, 55 | TS70mmTur | Range |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 370, 377 | TSScoopDualTur | ReloadDelay |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 371, 380 | TSScoopDualTur | Range |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 2153, 2297 | TSAdatsMissile_AA | ValidTargets |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 2157, 2199 | TSAdatsMissile_AA | Projectile |
| mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml | 4186, 4194 | TSMutApcCannon | ValidTargets |
| mods/cameo/ContentPacks/TiberianSun/GDI/yaml/weapons.yaml | 1078, 1102 | TSMammothTusk2_AA | Projectile |
| mods/cameo/ContentPacks/TiberianSun/GDI/yaml/weapons.yaml | 1115, 1139 | TSMammothTusk2II_AA | Projectile |
| mods/cameo/ContentPacks/TiberianSun/GDI/yaml/weapons.yaml | 1216, 1234 | TSGDIRedEye | Projectile |
| mods/cameo/ContentPacks/TiberianSun/GDI/yaml/weapons.yaml | 1261, 1273 | TSBombSonic | ValidTargets |
| mods/cameo/ContentPacks/TiberianSun/GDI/yaml/weapons.yaml | 1262, 1268 | TSBombSonic | ReloadDelay |
| mods/cameo/ContentPacks/TiberianSun/GDI/yaml/weapons.yaml | 1263, 1271 | TSBombSonic | Range |
| mods/cameo/ContentPacks/TiberianSun/GDI/yaml/weapons.yaml | 1787, 1795 | TSAAPCCannon | ValidTargets |
| mods/cameo/ContentPacks/TiberianSun/Nod/yaml/weapons.yaml | 332, 350 | TSNODRedEye | Projectile |
| mods/cameo/ContentPacks/TiberianSun/Nod/yaml/weapons.yaml | 362, 386 | TSNODTibRedEye | Projectile |
| mods/cameo/ContentPacks/TiberiumWars/Scrin/yaml/weapons.yaml | 656, 881 | ScrinRiftDamage | ReloadDelay |
| mods/cameo/ContentPacks/TiberiumWars/Scrin/yaml/weapons.yaml | 659, 883 | ScrinRiftDamage | Projectile |
| mods/cameo/ContentPacks/Warcraft2/Humans/yaml/sequences.yaml | 150, 153 | wc2_humans_guardtower | Defaults |
| mods/cameo/ContentPacks/Warcraft2/Humans/yaml/sequences.yaml | 158, 161 | wc2_humans_cannontower | Defaults |
| mods/cameo/rules/ants.yaml | 71, 80 | QANT | Voiced |
| mods/cameo/rules/ants.yaml | 755, 776 | defenseant | AutoTarget |
| mods/cameo/rules/generals.yaml | 4912, 4945 | glworker | AutoTarget |
| mods/cameo/rules/generals.yaml | 7398, 7413 | charty | Explodes |
| mods/cameo/rules/lostunits.yaml | 1215, 1324 | dalek | AttackFrontal |
| mods/cameo/rules/shockwave.yaml | 3107, 3144 | susadecoydrone | Disguise |
| mods/cameo/rules/shockwave.yaml | 8287, 8298 | sglkatyusha | RenderSprites |
| mods/cameo/rules/shockwave.yaml | 9288, 9303 | sglbadger | RenderSprites |
| mods/cameo/rules/simcity.yaml | 1668, 1675 | CITYFIREFIGHTER | Voiced |
| mods/cameo/rules/starwars.yaml | 4304, 4312 | swindustrialplant | HitShape |
| mods/cameo/rules/starwars.yaml | 8599, 8628 | swjabbaparty | Cargo |
| mods/cameo/weapons/d2k.yaml | 1092, 1104 | DeathHandCluster | Projectile |
| mods/cameo/weapons/d2k.yaml | 1893, 1901 | o110mm_Gun | Projectile |
| mods/cameo/weapons/d2k.yaml | 1896, 1904 | o110mm_Gun | Warhead@CannonHE_Medium |
| mods/cameo/weapons/d2k.yaml | 1996, 2005 | oDevBullet | Warhead@CannonHE_Medium |
| mods/cameo/weapons/d2k.yaml | 2015, 2029 | o155mm | Warhead@CannonHE_Medium |
| mods/cameo/weapons/d2k.yaml | 2120, 2132 | oDeviatorMissile | Warhead@MissileAP_Heavy |
| mods/cameo/weapons/d2k.yaml | 2267, 2278 | oDeathHandCluster | Projectile |
| mods/cameo/weapons/starwars.yaml | 814, 818 | SWNapalm | Burst |
| mods/cameo/weapons/starwars.yaml | 843, 847 | SWNapalm2 | Burst |
| mods/cameo/weapons/starwars.yaml | 867, 871 | SWNapalm3 | Burst |
| mods/cameo/weapons/tiberiansun.yaml | 2033, 2049 | TSAegisMissile | Projectile |
| mods/cameo/weapons/tiberiansun.yaml | 2153, 2175 | TSMammothTusk_elite | Projectile |


**FAIL** — D2 count 222 exceeds the baseline 158: a new duplicate key was introduced.

