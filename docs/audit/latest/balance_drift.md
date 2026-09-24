# audit_balance_drift — yaml vs committed balance ledger

**14 ledger(s) drifted** — balance numbers were hand-edited in yaml, or a sanctioned apply run was not followed by re-extraction. Fix via the pipeline, never by hand:

## d2k_atreides

```diff
       "slot": "Armament@GUN",
-      "versus_templates": [],
+      "versus_templates": [
+       "^d2k_shared_ornigun"
+      ],
       "warheads": [],
@@ -911,3 +913,3 @@
        "^Projectile_Missile_Heavy_D2K",
-       "^Effect_MissileAP_Heavy_D2K"
+       "^d2k_shared_d2k_towermissile"
       ],
@@ -1182,3 +1184,4 @@
```

## d2k_corrino

```diff
       "slot": "Armament",
-      "versus_templates": [],
+      "versus_templates": [
+       "^d2k_shared_ornigun"
+      ],
       "warheads": [],
@@ -807,3 +809,3 @@
        "^Projectile_Missile_Heavy_D2K",
-       "^Effect_MissileAP_Heavy_D2K"
+       "^d2k_shared_d2k_towermissile"
       ],
@@ -1259,3 +1261,3 @@
```

## d2k_harkonnen

```diff
       "slot": "Armament",
-      "versus_templates": [],
+      "versus_templates": [
+       "^d2k_shared_ornigun"
+      ],
       "warheads": [],
@@ -1049,3 +1051,3 @@
        "^Projectile_Missile_Heavy_D2K",
-       "^Effect_MissileAP_Heavy_D2K"
+       "^d2k_shared_d2k_towermissile"
       ],
@@ -1711,3 +1713,3 @@
```

## d2k_ixian

```diff
        "^Projectile_Bullet_Medium",
-       "^Effect_Bullet_Medium"
+       "^d2k_ixian_d2k_air_drone_guns"
       ],
@@ -296,3 +296,4 @@
        "^HeavyBomb",
-       "^TeslaChargedWeapon"
+       "^TeslaChargedWeapon",
+       "^d2k_ixian_ixianbomb_emp"
       ],
@@ -452,3 +453,3 @@
        "^RailgunWeapon",
```

## d2k_ordos

```diff
        "^Projectile_Bullet_Medium",
-       "^Effect_Bullet_Medium"
+       "^d2k_ordos_bullet_medium"
       ],
@@ -68,3 +68,3 @@
        "^Projectile_Laser_Heavy",
-       "^Effect_Laser_Heavy",
+       "^d2k_ordos_laser_heavy",
        "d2kCarryallChainGun"
@@ -159,3 +159,3 @@
        "^Projectile_Bullet_Medium",
-       "^Effect_Bullet_Medium"
```

## redalert2_allies

```diff
        "^Projectile_Flame_Medium",
-       "^Effect_Flame_Medium"
+       "^td_tiberiandawn_bigflamer"
       ],
@@ -6862,3 +6862,3 @@
        "^Projectile_Flame_Medium",
-       "^Effect_Flame_Medium"
+       "^td_tiberiandawn_bigflamer"
       ],
@@ -8266,3 +8266,3 @@
        "^Projectile_Flame_Medium",
-       "^Effect_Flame_Medium"
```

## redalert2mod_futuretech

```diff
        "^Projectile_Flame_Medium",
-       "^Effect_Flame_Medium"
+       "^td_tiberiandawn_bigflamer"
       ],
```

## shared_d2k

```diff
        "^D2KMissile",
-       "^Effect_MissileAP_Heavy"
+       "^d2k_shared_fremen_rpg"
       ],
```

## tiberiandawn_gdi

```diff
        "^Warhead_Demolition_Heavy",
-       "^Effect_Flame_Heavy"
+       "^td_gdi_set3"
       ],
@@ -172,3 +172,3 @@
        "^Warhead_Demolition_Heavy",
-       "^Effect_Flame_Heavy"
+       "^td_gdi_set3"
       ],
@@ -204,3 +204,3 @@
        "^Projectile_Missile_Heavy",
-       "^Effect_MissileAP_Heavy"
```

## tiberiandawn_nod

```diff
        "^Warhead_Concussion_Medium",
-       "^Effect_AlliedTigerCannon"
+       "^td_nod_td_nod_gunturret_turretgun"
       ],
@@ -710,3 +710,3 @@
        "td_nod_gunturret_turretgun",
-       "^Effect_Apoc_AP_RA2"
+       "^td_nod_td_nod_gunturret_turretgunblackmarket"
       ],
@@ -1723,3 +1723,3 @@
        "^Warhead_Chemical_Light",
-       "^Effect_CannonAP_Light"
```

## tiberiansun_cabal

```diff
        "^Projectile_Laser_Heavy",
-       "^Effect_Laser_Heavy"
+       "^ts_cabal_set11"
       ],
@@ -149,3 +149,3 @@
        "^Projectile_Bullet_Light",
-       "^Effect_Bullet_Light"
+       "^ts_cabal_cabaloverkilldronelaser"
       ],
@@ -258,3 +258,3 @@
        "^Projectile_Lightning_Heavy",
-       "^Effect_Tesla_Heavy"
```

## tiberiansun_forgotten

```diff
        "^Projectile_Missile_Medium",
-       "^Effect_MissileAP_Medium"
+       "^ts_forgotten_set12"
       ],
@@ -56,3 +56,3 @@
        "^Projectile_Missile_Medium",
-       "^Effect_MissileAP_Medium"
+       "^ts_forgotten_set15"
       ],
@@ -283,3 +283,3 @@
        "^Projectile_Missile_Medium",
-       "^Effect_MissileAP_Medium"
```

## tiberiansun_gdi

```diff
        "^Projectile_Shell_Heavy",
-       "^Effect_CannonHE_Heavy",
        "^RailgunWeapon",
-       "^HeavyBomb"
+       "^HeavyBomb",
+       "^ts_gdi_kodiakcannon"
       ],
@@ -432,3 +432,3 @@
        "^Warhead_Demolition_Heavy",
-       "^Effect_Demolition_Heavy"
+       "^ts_gdi_tsbomb"
       ],
```

## tiberiansun_nod

```diff
        "^TSEnergyBlast",
-       "^LaserWeapon"
+       "^LaserWeapon",
+       "^ts_nod_tsproton"
       ],
@@ -1288,3 +1289,4 @@
       "versus_templates": [
-       "^TSDefaultMissile"
+       "^TSDefaultMissile",
+       "^ts_tiberiansun_tstacticalmissile"
       ],
@@ -1415,4 +1417,4 @@
```

