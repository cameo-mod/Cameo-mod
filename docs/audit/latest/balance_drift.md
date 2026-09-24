# audit_balance_drift — yaml vs committed balance ledger

**25 ledger(s) drifted** — balance numbers were hand-edited in yaml, or a sanctioned apply run was not followed by re-extraction. Fix via the pipeline, never by hand:

## d2k_atreides

```diff
       "versus_templates": [
-       "^MG"
+       "^MG",
+       "^d2k_fremen_s"
       ],
@@ -1204,5 +1205,3 @@
       "slot": "Armament@SECONDARY",
-      "versus_templates": [
-       "M_HMG"
-      ],
+      "versus_templates": [],
       "warheads": [],
```

## d2k_corrino

```diff
        "^Projectile_Laser_Heavy",
-       "^Effect_Laser_Heavy"
+       "^d2k_laser_heavy"
       ],
@@ -1703,3 +1703,3 @@
        "^Projectile_Laser_Heavy",
-       "^Effect_Laser_Heavy"
+       "^d2k_laser_heavy"
       ],
```

## redalert2_allies

```diff
        {
-        "damage": "48000",
-        "falloff": "100, 0",
-        "spread": "64",
-        "tag": "MissileAP_Medium_Flat",
-        "type": "AreaDamage"
-       },
-       {
         "damage": "800",
@@ -43,2 +36,9 @@
         "type": "AreaDamagePercentage"
+       },
```

## redalert2_soviets

```diff
        "^Effect_Chem_Heavy",
-       "RA2KirovBomb",
-       "^RA2RadShell"
+       "RA2KirovBomb"
       ],
@@ -273,2 +272,17 @@
        {
+        "damage": "8000",
+        "falloff": "100, 0",
+        "spread": "64",
+        "tag": "MissileAP_Medium_GroundShipExtraDamage",
+        "type": "AreaDamage"
```

## redalert2_yuri

```diff
       "versus_templates": [
-       "^Warhead_Tesla_Heavy",
        "^Warhead_Magic_Heavy",
@@ -109,3 +108,2 @@
       "warheads": [
-       "^Warhead_Tesla_Heavy",
        "^Warhead_Magic_Heavy"
@@ -822,3 +820,3 @@
       "defined_in": "mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml",
-      "design_weapon_class": 0.875,
+      "design_weapon_class": 1.0,
       "pricing": true,
```

## redalert2mod_asianalliance

```diff
       "defined_in": "mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml",
-      "design_weapon_class": null,
+      "design_weapon_class": 1.0,
       "pricing": true,
@@ -200,14 +200,4 @@
       "versus_templates": [
-       "^Warhead_Plasma_Medium_Flat",
-       "^Warhead_Plasma_Medium",
-       "^Warhead_CannonHE_Medium",
-       "^Projectile_Shell_Medium",
-       "^Effect_CannonHE_Medium",
-       "^LightFlameWeapon",
```

## redalert2mod_consortium

```diff
        {
+        "damage": "1",
+        "falloff": null,
+        "spread": "75",
+        "tag": "LaserWeaponPercentage",
+        "type": "AreaDamagePercentage"
+       },
+       {
         "damage": "10000",
@@ -20,16 +27,2 @@
        {
-        "damage": "600",
```

## redalert2mod_futuretech

```diff
       "defined_in": "mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/weapons.yaml",
-      "design_weapon_class": 1.125,
+      "design_weapon_class": 1.0,
       "pricing": true,
@@ -42,10 +42,7 @@
       "versus_templates": [
-       "^Warhead_MissileAP_Medium_Flat",
-       "^Grenade",
-       "^D2KRocket",
-       "^SteelMediumMissile",
-       "^FutureCryocopterRocketMissileCompatibility"
+       "^Warhead_MissileAP_Medium",
```

## redalert2mod_naxis

```diff
       "defined_in": "mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml",
-      "design_weapon_class": null,
+      "design_weapon_class": 1.0,
       "pricing": true,
@@ -28,11 +28,7 @@
       "versus_templates": [
-       "^Warhead_Bullet_Medium_Flat",
-       "^Warhead_Concussion_Light",
-       "^Warhead_CannonHE_Heavy",
-       "^Effect_CannonHE_Heavy",
-       "^RA2Chaingun"
-      ],
```

## redalert2mod_schwarzermond

```diff
       "defined_in": "mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/weapons.yaml",
-      "design_weapon_class": null,
+      "design_weapon_class": 1.0,
       "minrange": "1600",
@@ -109,11 +109,7 @@
       "versus_templates": [
-       "^Warhead_MissileAP_Medium_Flat",
-       "^Warhead_Concussion_Light",
-       "^Warhead_MissileAP_Heavy",
-       "^Effect_MissileAP_Heavy",
-       "^RA2MediumMissile"
-      ],
```

## redalert2mod_syndicate

```diff
       "defined_in": "mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml",
-      "design_weapon_class": 0.875,
+      "design_weapon_class": 1.0,
       "pricing": true,
@@ -25,8 +25,7 @@
       "versus_templates": [
-       "^Warhead_Bullet_Medium_Flat",
-       "^RA2SmallArms",
-       "^RA2Chaingun"
-      ],
-      "warheads": [
-       "^Warhead_Bullet_Light",
```

## redalert2mod_tkm

```diff
       "defined_in": "mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml",
-      "design_weapon_class": null,
+      "design_weapon_class": 1.0,
       "minrange": "1105",
@@ -82,5 +82,2 @@
       "versus_templates": [
-       "^Warhead_Cryo_Medium_Flat",
-       "^Warhead_Demolition_Light",
-       "^Warhead_MissileAP_Medium",
        "^Warhead_Concussion_Medium",
@@ -90,4 +87,2 @@
       "warheads": [
```

## redalert_allies

```diff
       "defined_in": "mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml",
-      "design_weapon_class": 1.25,
+      "design_weapon_class": null,
       "minrange": "1310",
@@ -290,3 +290,2 @@
       "versus_templates": [
-       "^Warhead_MissileAP_Heavy",
        "^Warhead_MissileHE_Heavy_Flat",
@@ -295,7 +294,5 @@
       ],
-      "warheads": [
-       "^Warhead_MissileAP_Heavy"
```

## redalert_japan

```diff
       "versus_templates": [
-       "^WaveforceBulletWarhead",
-       "^Grenade",
-       "ZeroFighterChainGun",
-       "^WaveforceBulletProjectile",
-       "^ZeroFighterWaveforceBulletCompatibility"
+       "^Warhead_Railgun_Heavy",
+       "^Projectile_Railgun_Heavy",
+       "^Effect_MissileAP_Heavy",
+       "ZeroFighterChainGun"
       ],
@@ -149,3 +148,2 @@
```

## redalert_soviets

```diff
       "defined_in": "mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml",
-      "design_weapon_class": null,
+      "design_weapon_class": 1.0,
       "pricing": true,
@@ -231,4 +231,2 @@
       "warheads": [
-       "^Warhead_Demolition_Light",
-       "^Warhead_MissileAP_Medium",
        "^Warhead_Concussion_Medium"
@@ -236,3 +234,3 @@
       "weapon": "ra1_soviets_hindattackhelicopter_hindmissiles",
-      "weapon_class_source": "illegal_mix"
```

## shared_redalert

```diff
        "^Warhead_Demolition_Heavy",
-       "^Grenade",
-       "^ShrapnelWeapon",
-       "^MediumChemicalWeapon",
-       "^MediumFlameWeapon",
-       "^HeavyBomb"
+       "^Effect_Apoc_AP_RA2",
+       "^Effect_Flame_Heavy"
       ],
@@ -253,3 +250,2 @@
       "warheads": [
-       "^Warhead_Demolition_Heavy",
```

## shared_redalert2

```diff
       "defined_in": "mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml",
-      "design_weapon_class": 0.875,
+      "design_weapon_class": 1.0,
       "pricing": true,
@@ -311,8 +311,7 @@
       "versus_templates": [
-       "^Warhead_Bullet_Medium_Flat",
-       "^RA2SmallArms",
-       "^RA2Chaingun"
+       "^Warhead_Bullet_Medium",
+       "^Projectile_Bullet_Medium",
+       "^Effect_Bullet_Medium_RA2"
```

## shared_tiberiansun

```diff
       "defined_in": "mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml",
-      "design_weapon_class": 0.875,
+      "design_weapon_class": 1.0,
       "pricing": true,
@@ -143,8 +143,7 @@
       "versus_templates": [
-       "^Warhead_Bullet_Medium_Flat",
-       "^RA2SmallArms",
-       "^RA2Chaingun"
+       "^Warhead_Bullet_Medium",
+       "^Projectile_Bullet_Medium",
+       "^Effect_Bullet_Medium_RA2"
```

## starcraft_protoss

```diff
        {
+        "damage": "1000",
+        "falloff": "100, 75, 50, 25",
+        "spread": "200",
+        "tag": "Railgun_Heavy_ExtraDamage",
+        "type": "SpreadDamage"
+       },
+       {
         "damage": "20000",
@@ -5144,9 +5151,2 @@
         "type": "AreaDamage"
-       },
```

## starcraft_terran

```diff
         "falloff": null,
-        "spread": "50",
-        "tag": "SmallArmsPercentage",
-        "type": "AreaDamagePercentage"
-       },
-       {
-        "damage": "1",
-        "falloff": null,
-        "spread": "100",
-        "tag": "ChaingunPercentage",
-        "type": "AreaDamagePercentage"
-       },
```

## tiberiansun_cabal

```diff
       "slot": "Armament@Suicide",
-      "versus_templates": [
-       "DemoTruckTargeting"
-      ],
+      "versus_templates": [],
       "warheads": [],
```

## tiberiansun_gdi

```diff
       "versus_templates": [
-       "^Warhead_Demolition_Heavy_Flat",
        "^Warhead_Demolition_Heavy",
@@ -3969,3 +3968,2 @@
       "versus_templates": [
-       "^Warhead_Demolition_Heavy_Flat",
        "^Warhead_Demolition_Heavy",
@@ -3999,3 +3997,2 @@
       "versus_templates": [
-       "^Warhead_Demolition_Heavy_Flat",
        "^Warhead_Demolition_Heavy",
@@ -4029,3 +4026,2 @@
```

## tiberiansun_nod

```diff
       "versus_templates": [
-       "^Warhead_Demolition_Heavy_Flat",
        "^Warhead_Demolition_Heavy",
@@ -3311,3 +3310,2 @@
       "versus_templates": [
-       "^Warhead_Demolition_Heavy_Flat",
        "^Warhead_Demolition_Heavy",
@@ -3341,3 +3339,2 @@
       "versus_templates": [
-       "^Warhead_Demolition_Heavy_Flat",
        "^Warhead_Demolition_Heavy",
@@ -3371,3 +3368,2 @@
```

## warcraft2_humans

```diff
       "versus_templates": [
-       "wc2footmanslice"
+       "^Warhead_Melee_Medium",
+       "^Projectile_InstantHit",
+       "^Effect_Melee_Medium"
       ],
@@ -5324,3 +5326,5 @@
       "versus_templates": [
-       "wc2footmanslice"
+       "^Warhead_Melee_Medium",
+       "^Projectile_InstantHit",
+       "^Effect_Melee_Medium"
```

## warcraft2_orcs

```diff
       "versus_templates": [
-       "wc2footmanslice"
+       "^Warhead_Melee_Medium",
+       "^Projectile_InstantHit",
+       "^Effect_Melee_Medium"
       ],
@@ -4404,3 +4406,5 @@
       "versus_templates": [
-       "wc2footmanslice"
+       "^Warhead_Melee_Medium",
+       "^Projectile_InstantHit",
+       "^Effect_Melee_Medium"
```

