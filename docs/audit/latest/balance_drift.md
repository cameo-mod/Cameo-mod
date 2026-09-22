# audit_balance_drift — yaml vs committed balance ledger

**6 ledger(s) drifted** — balance numbers were hand-edited in yaml, or a sanctioned apply run was not followed by re-extraction. Fix via the pipeline, never by hand:

## d2k_atreides

```diff
       ],
-      "defined_in": "mods/cameo/ContentPacks/D2k/Atreides/yaml/weapons.yaml",
+      "defined_in": "mods/cameo/ContentPacks/D2k/Shared/yaml/weapons.yaml",
       "design_weapon_class": null,
@@ -1280,3 +1280,3 @@
       ],
-      "defined_in": "mods/cameo/weapons/d2k.yaml",
+      "defined_in": "mods/cameo/ContentPacks/D2k/Shared/yaml/weapons.yaml",
       "design_weapon_class": 1.0,
@@ -1309,3 +1309,3 @@
       ],
-      "defined_in": "mods/cameo/weapons/d2k.yaml",
```

## d2k_corrino

```diff
       ],
-      "defined_in": "mods/cameo/ContentPacks/D2k/Atreides/yaml/weapons.yaml",
+      "defined_in": "mods/cameo/ContentPacks/D2k/Shared/yaml/weapons.yaml",
       "design_weapon_class": null,
@@ -1059,3 +1059,3 @@
       ],
-      "defined_in": "mods/cameo/weapons/d2k.yaml",
+      "defined_in": "mods/cameo/ContentPacks/D2k/Shared/yaml/weapons.yaml",
       "design_weapon_class": 1.0,
@@ -1088,3 +1088,3 @@
       ],
-      "defined_in": "mods/cameo/weapons/d2k.yaml",
```

## d2k_harkonnen

```diff
       ],
-      "defined_in": "mods/cameo/ContentPacks/D2k/Atreides/yaml/weapons.yaml",
+      "defined_in": "mods/cameo/ContentPacks/D2k/Shared/yaml/weapons.yaml",
       "design_weapon_class": null,
@@ -1334,3 +1334,3 @@
       ],
-      "defined_in": "mods/cameo/weapons/d2k.yaml",
+      "defined_in": "mods/cameo/ContentPacks/D2k/Shared/yaml/weapons.yaml",
       "design_weapon_class": 1.0,
@@ -1363,3 +1363,3 @@
       ],
-      "defined_in": "mods/cameo/weapons/d2k.yaml",
```

## d2k_ixian

```diff
      {
-      "burst": "4",
-      "burstdelays": "5",
-      "damage_warheads": [
-       {
-        "damage": "12000",
-        "falloff": "100, 0",
-        "spread": "85",
-        "tag": "MissileAP_Heavy_Flat",
-        "type": "AreaDamage"
-       },
-       {
```

## d2k_ordos

```diff
       ],
-      "defined_in": "mods/cameo/ContentPacks/D2k/Ordos/yaml/weapons.yaml",
+      "defined_in": "mods/cameo/ContentPacks/D2k/Shared/yaml/weapons.yaml",
       "design_weapon_class": null,
@@ -2927,3 +2927,3 @@
       ],
-      "defined_in": "mods/cameo/weapons/d2k.yaml",
+      "defined_in": "mods/cameo/ContentPacks/D2k/Shared/yaml/weapons.yaml",
       "design_weapon_class": 1.0,
@@ -2995,3 +2995,3 @@
       ],
-      "defined_in": "mods/cameo/weapons/d2k.yaml",
```

## shared_d2k

```diff
       ],
-      "defined_in": "mods/cameo/weapons/d2k.yaml",
+      "defined_in": "mods/cameo/ContentPacks/D2k/Shared/yaml/weapons.yaml",
       "design_weapon_class": 1.0,
@@ -646,3 +646,3 @@
       ],
-      "defined_in": "mods/cameo/weapons/d2k.yaml",
+      "defined_in": "mods/cameo/ContentPacks/D2k/Shared/yaml/weapons.yaml",
       "design_weapon_class": 1.25,
@@ -724,3 +724,3 @@
       ],
-      "defined_in": "mods/cameo/weapons/d2k.yaml",
```

