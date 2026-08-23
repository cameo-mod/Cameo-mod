# audit_balance_drift — yaml vs committed balance ledger

**9 ledger(s) drifted** — balance numbers were hand-edited in yaml, or a sanctioned apply run was not followed by re-extraction. Fix via the pipeline, never by hand:

## redalert2_allies

```diff
        "^MediumChemicalWeapon",
-       "^RA2TankDestroyerCannon",
+       "^Warhead_CannonAP_Light",
+       "^Projectile_Shell_Light",
        "^Effect_Iron_Fx_RA2"
@@ -5822,3 +5823,4 @@
        "^MediumChemicalWeapon",
-       "^RA2TankDestroyerCannon",
+       "^Warhead_CannonAP_Light",
+       "^Projectile_Shell_Light",
        "^Effect_Iron_Fx_RA2"
@@ -7667,3 +7669,4 @@
```

## redalert2mod_futuretech

```diff
        "^MediumChemicalWeapon",
-       "^RA2TankDestroyerCannon",
+       "^Warhead_CannonAP_Light",
+       "^Projectile_Shell_Light",
        "^Effect_Iron_Fx_RA2"
```

## redalert_allies

```diff
        "^Projectile_Missile_Heavy",
-       "^Effect_MissileHE_Heavy",
-       "^ImpactGlow"
+       "^Effect_MissileHE_Heavy"
       ],
```

## redalert_japan

```diff
        "^Warhead_Demolition_Heavy",
-       "^Effect_Demolition_Heavy",
-       "^ImpactGlow"
+       "^Effect_Demolition_Heavy"
       ],
```

## shared_redalert

```diff
        "^Warhead_Flame_Heavy",
-       "^Effect_Flame_Heavy",
-       "^ImpactGlow"
+       "^Effect_Flame_Heavy"
       ],
@@ -468,4 +467,3 @@
        "^Warhead_Flame_Heavy",
-       "^Effect_Flame_Heavy",
-       "^ImpactGlow"
+       "^Effect_Flame_Heavy"
       ],
@@ -3136,4 +3134,3 @@
```

## tiberiandawn_gdi

```diff
        "^Projectile_Missile_Heavy",
-       "^Effect_MissileAP_Heavy",
-       "^ImpactGlow"
+       "^Effect_MissileAP_Heavy"
       ],
@@ -532,4 +531,3 @@
        "^Projectile_Missile_Heavy",
-       "^Effect_MissileAP_Heavy",
-       "^ImpactGlow"
+       "^Effect_MissileAP_Heavy"
       ],
@@ -571,4 +569,3 @@
```

## tiberiandawn_nod

```diff
        "^Projectile_Missile_Heavy",
-       "^Effect_MissileAP_Heavy",
-       "^ImpactGlow"
+       "^Effect_MissileAP_Heavy"
       ],
@@ -6643,4 +6642,3 @@
        "^Projectile_Grenade_Light",
-       "^Effect_Concussion_Medium",
-       "^ImpactGlow"
+       "^Effect_Concussion_Medium"
       ],
```

## tiberiansun_forgotten

```diff
       "versus_templates": [
-       "^ImpactGlow",
        "^Warhead_MissileAP_Heavy",
```

## tiberiansun_gdi

```diff
       "versus_templates": [
-       "^ImpactGlow",
        "^Warhead_MissileAP_Heavy",
@@ -6607,3 +6606,2 @@
       "versus_templates": [
-       "^ImpactGlow",
        "^Warhead_MissileAP_Heavy",
```

