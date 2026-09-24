# audit_balance_drift — yaml vs committed balance ledger

**32 ledger(s) drifted** — balance numbers were hand-edited in yaml, or a sanctioned apply run was not followed by re-extraction. Fix via the pipeline, never by hand:

## d2k_atreides

```diff
     "prerequisites": [
-     "~hightech.atreides",
+     "~atreides_hightech",
      "~heavy.atreides_combat"
@@ -124,3 +124,3 @@
     "prerequisites": [
-     "~hightech.atreides",
+     "~atreides_hightech",
      "research_centre",
@@ -238,3 +238,5 @@
       "slot": "Armament@GUN",
-      "versus_templates": [],
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
     "prerequisites": [
-     "~hightech.harkonnen",
+     "~harkonnen_hightech",
      "~harkonnen_promotion_advancedcarryall"
@@ -143,3 +143,5 @@
       "slot": "Armament",
-      "versus_templates": [],
+      "versus_templates": [
+       "^d2k_shared_ornigun"
+      ],
       "warheads": [],
@@ -175,3 +177,3 @@
```

## d2k_ixian

```diff
      {
-      "damage_warheads": [],
+      "burst": "4",
+      "burstdelays": "5",
+      "damage_warheads": [
+       {
+        "damage": "12000",
+        "falloff": "100, 0",
+        "spread": "85",
+        "tag": "MissileAP_Heavy_Flat",
+        "type": "AreaDamage"
+       },
```

## d2k_ordos

```diff
   "aircraft": {
-   "carryall_reinforce.ordos": {
+   "ordos_advancedcarryall": {
     "armaments": [
@@ -30,3 +30,3 @@
        "^Projectile_Bullet_Medium",
-       "^Effect_Bullet_Medium"
+       "^d2k_ordos_bullet_medium"
       ],
@@ -68,3 +68,3 @@
        "^Projectile_Laser_Heavy",
-       "^Effect_Laser_Heavy",
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
   "aircraft": {
-   "cruiser_f.steel": {
+   "steel_cruiser_f": {
     "armaments": [
@@ -12,2 +12,9 @@
       "damage_warheads": [
+       {
+        "damage": "1",
+        "falloff": null,
+        "spread": "75",
+        "tag": "LaserWeaponPercentage",
+        "type": "AreaDamagePercentage"
```

## redalert2mod_futuretech

```diff
   "aircraft": {
+   "futu_landcarr_drone": {
+    "armaments": [
+     {
+      "damage_warheads": [
+       {
+        "damage": "6000",
+        "falloff": "100, 0",
+        "spread": "64",
+        "tag": "MissileAP_Medium_Flat",
+        "type": "AreaDamage"
+       }
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

## shared_d2k

```diff
   "buildings": {
-   "OILB.d2k": {
-    "armor": {
-     "src": "inherited",
-     "v": "Wood"
-    },
-    "build_duration": {
-     "src": "inherited",
-     "v": "1500"
-    },
-    "buildable": true,
-    "cost": {
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
-        "damage": "5000",
+        "damage": "7500",
         "falloff": "100, 75, 50, 25",
@@ -261,9 +261,2 @@
         "type": "AreaDamage"
-       },
-       {
-        "damage": "2500",
-        "falloff": "100, 75, 50, 25",
-        "spread": "500",
-        "tag": "Tesla_Super_ExtraDamage",
```

## starcraft_terran

```diff
        {
-        "damage": "100000",
+        "damage": "150000",
         "falloff": "100, 55, 0",
@@ -974,9 +974,2 @@
         "type": "AreaDamage"
-       },
-       {
-        "damage": "50000",
-        "falloff": "100, 75, 50, 25",
-        "spread": "400",
-        "tag": "Tesla_Super_ExtraDamage",
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
       "versus_templates": [
-       "td_nod_venom_venomlaserinferno"
+       "^Warhead_Inferno_Medium",
+       "^Projectile_Laser_Heavy",
+       "^Effect_Laser_Heavy"
       ],
@@ -640,3 +642,3 @@
        "^Warhead_Concussion_Medium",
-       "^Effect_AlliedTigerCannon"
+       "^td_nod_td_nod_gunturret_turretgun"
       ],
@@ -710,3 +712,3 @@
```

## tiberiansun_cabal

```diff
        {
-        "damage": "4000",
+        "damage": "6000",
         "falloff": "100, 0",
@@ -24,9 +24,2 @@
         "tag": "Laser_Heavy_ExtraDamage",
-        "type": "SpreadDamage"
-       },
-       {
-        "damage": "2000",
-        "falloff": null,
-        "spread": null,
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
@@ -188,3 +189,5 @@
       "versus_templates": [
-       "TSHarpyClaw"
+       "^Warhead_Bullet_Medium",
+       "^Projectile_Bullet_Medium",
+       "^Effect_Bullet_Medium"
       ],
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

