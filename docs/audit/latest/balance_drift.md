# audit_balance_drift — yaml vs committed balance ledger

**6 ledger(s) drifted** — balance numbers were hand-edited in yaml, or a sanctioned apply run was not followed by re-extraction. Fix via the pipeline, never by hand:

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

## shared_d2k

```diff
        "^D2KMissile",
-       "^Effect_MissileAP_Heavy"
+       "^d2k_shared_fremen_rpg"
       ],
```

