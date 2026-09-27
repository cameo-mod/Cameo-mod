# audit_balance_drift — yaml vs committed balance ledger

**17 ledger(s) drifted** — balance numbers were hand-edited in yaml, or a sanctioned apply run was not followed by re-extraction. Fix via the pipeline, never by hand:

## redalert2_yuri

```diff
       "defined_in": "mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml",
-      "design_weapon_class": 0.875,
+      "design_weapon_class": 1.0,
       "pricing": true,
@@ -2545,8 +2545,6 @@
        "^Projectile_Flak_Medium",
-       "^Effect_Flak_Medium",
-       "RA2Virusgun"
+       "^Effect_Flak_Medium"
       ],
       "warheads": [
-       "^Warhead_Flak_Medium",
```

## redalert2mod_asianalliance

```diff
        {
-        "damage": "888",
-        "falloff": "111, 88, 44, 22, 11",
-        "spread": "444",
-        "tag": "PreservedFlat_MagicExtraDamage",
-        "type": "SpreadDamage"
-       },
-       {
-        "damage": "1000",
-        "falloff": "100, 75, 50, 25",
-        "spread": "200",
-        "tag": "PreservedFlat_TeslaExtraDamage",
```

## redalert2mod_consortium

```diff
        {
-        "damage": "13000",
+        "damage": "14000",
         "falloff": "100, 52, 0",
@@ -88,9 +88,2 @@
         "type": "AreaDamagePercentage"
-       },
-       {
-        "damage": "1000",
-        "falloff": "100, 75, 50, 25",
-        "spread": "200",
-        "tag": "Railgun_Heavy_ExtraDamage",
```

## redalert2mod_futuretech

```diff
        {
-        "damage": "8000",
+        "damage": "9000",
         "falloff": "100, 50, 20, 0",
@@ -2028,9 +2028,2 @@
         "type": "AreaDamagePercentage"
-       },
-       {
-        "damage": "1000",
-        "falloff": "100, 75, 50, 25",
-        "spread": "200",
-        "tag": "Railgun_Heavy_ExtraDamage",
```

## redalert2mod_naxis

```diff
        {
-        "damage": "24000",
+        "damage": "32000",
         "falloff": "100, 0",
@@ -2313,9 +2313,2 @@
         "type": "AreaDamage"
-       },
-       {
-        "damage": "8000",
-        "falloff": "100, 75, 50, 25",
-        "spread": "200",
-        "tag": "Tesla_Heavy_ExtraDamage",
```

## redalert2mod_schwarzermond

```diff
        {
-        "damage": "4000",
+        "damage": "4600",
         "falloff": "100, 0",
@@ -921,9 +921,2 @@
        {
-        "damage": "600",
-        "falloff": null,
-        "spread": "300",
-        "tag": "LegacyLaserExtraDamage",
-        "type": "SpreadDamage"
-       },
```

## redalert2mod_syndicate

```diff
       "defined_in": "mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml",
-      "design_weapon_class": 1.25,
+      "design_weapon_class": null,
       "pricing": true,
@@ -1976,10 +1976,7 @@
       "versus_templates": [
-       "RA2FreedomAK47",
        "^Effect_Apoc_AP_RA2"
       ],
-      "warheads": [
-       "^Warhead_CannonHE_Heavy"
-      ],
```

## redalert_soviets

```diff
        {
-        "damage": "206000",
+        "damage": "207030",
         "falloff": "100, 0",
@@ -4517,9 +4517,2 @@
        {
-        "damage": "1030",
-        "falloff": "100, 0",
-        "spread": "1",
-        "tag": "LegacyRailgunExtraDamage",
-        "type": "SpreadDamage"
-       },
```

## shared_redalert2

```diff
        {
-        "damage": "24000",
+        "damage": "30000",
         "falloff": "100, 0",
@@ -8214,23 +8214,2 @@
         "type": "AreaDamage"
-       },
-       {
-        "damage": "1000",
-        "falloff": "100, 75, 50, 25",
-        "spread": "200",
-        "tag": "Railgun_Heavy_ExtraDamage",
```

## starcraft_protoss

```diff
       ],
-      "defined_in": "mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml",
+      "defined_in": "mods/cameo/ContentPacks/StarCraft/Protoss/yaml/weapons.yaml",
       "design_weapon_class": null,
@@ -2377,7 +2377,5 @@
       "slot": "Armament",
-      "versus_templates": [
-       "^HealingWeapon"
-      ],
+      "versus_templates": [],
       "warheads": [],
-      "weapon": "Heal",
```

## starcraft_terran

```diff
        "^Effect_CannonHE_Heavy",
-       "^RA2Chaingun"
+       "^SCRA2Chaingun"
       ],
@@ -5695,3 +5695,3 @@
        "^Effect_CannonHE_Heavy",
-       "^RA2Chaingun"
+       "^SCRA2Chaingun"
       ],
@@ -5879,3 +5879,3 @@
        "^Effect_CannonHE_Heavy",
-       "^RA2Chaingun"
```

## tiberiandawn_gdi

```diff
       "damage_warheads": [],
-      "defined_in": "mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml",
+      "defined_in": "mods/cameo/ContentPacks/TiberianDawn/Shared/yaml/weapons.yaml",
       "design_weapon_class": null,
@@ -2222,7 +2222,5 @@
       "slot": "Armament@c4",
-      "versus_templates": [
-       "IvanAttach"
-      ],
+      "versus_templates": [],
       "warheads": [],
-      "weapon": "TanyaAttach",
```

## tiberiandawn_nod

```diff
       "damage_warheads": [],
-      "defined_in": "mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml",
+      "defined_in": "mods/cameo/ContentPacks/TiberianDawn/Shared/yaml/weapons.yaml",
       "design_weapon_class": null,
@@ -1978,7 +1978,5 @@
       "slot": "Armament@c4",
-      "versus_templates": [
-       "IvanAttach"
-      ],
+      "versus_templates": [],
       "warheads": [],
-      "weapon": "TanyaAttach",
```

## tiberiansun_cabal

```diff
       "defined_in": "mods/cameo/weapons/tiberiansun.yaml",
-      "design_weapon_class": 1.125,
+      "design_weapon_class": null,
       "pricing": true,
@@ -2278,10 +2278,7 @@
       "versus_templates": [
-       "TSTurretLaser"
-      ],
-      "warheads": [
-       "^Warhead_Bullet_Medium",
-       "^Warhead_Laser_Heavy"
-      ],
```

## tiberiansun_forgotten

```diff
       ],
-      "defined_in": "mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml",
+      "defined_in": "mods/cameo/ContentPacks/TiberianSun/Shared/yaml/weapons.yaml",
       "design_weapon_class": 0.75,
@@ -6019,3 +6019,3 @@
       ],
-      "weapon": "DepthCharge",
+      "weapon": "TSDepthCharge",
       "weapon_class_source": "template"
@@ -6032,3 +6032,3 @@
       ],
-      "defined_in": "mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml",
```

## tiberiansun_gdi

```diff
       "defined_in": "mods/cameo/weapons/tiberiansun.yaml",
-      "design_weapon_class": 1.0,
+      "design_weapon_class": null,
       "pricing": false,
@@ -2158,10 +2158,7 @@
       "versus_templates": [
-       "TSGrenade"
-      ],
-      "warheads": [
-       "^Warhead_CannonHE_Medium",
-       "^Warhead_Concussion_Medium"
-      ],
```

## tiberiansun_nod

```diff
       "versus_templates": [
-       "TSTacticalMissile"
+       "^ts_tiberiansun_tstacticalchemmissile"
       ],
@@ -3266,3 +3266,3 @@
       ],
-      "defined_in": "mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml",
+      "defined_in": "mods/cameo/ContentPacks/TiberianSun/Shared/yaml/weapons.yaml",
       "design_weapon_class": 1.25,
@@ -3281,3 +3281,3 @@
       ],
-      "weapon": "8Inch",
```

