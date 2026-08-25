# audit_balance_drift — yaml vs committed balance ledger

**5 ledger(s) drifted** — balance numbers were hand-edited in yaml, or a sanctioned apply run was not followed by re-extraction. Fix via the pipeline, never by hand:

## d2k_atreides

```diff
  "sections": {
+  "aircraft": {
+   "atreides_ornithopter": {
+    "armaments": [
+     {
+      "burst": "5",
+      "burstdelays": "5",
+      "damage_warheads": [
+       {
+        "damage": "7500",
+        "falloff": "100, 50, 25, 0",
+        "spread": "1250",
```

## d2k_corrino

```diff
+{
+ "ledger": "d2k_corrino",
+ "pack": "mods/cameo/ContentPacks/D2k/Corrino",
+ "schema": 2,
+ "sections": {
+  "aircraft": {
+   "corrino_carryall": {
+    "armor": {
+     "src": "mods/cameo/ContentPacks/D2k/Corrino/yaml/aircraft.yaml#Armor.Type",
+     "v": "Light"
+    },
+    "buildable": true,
```

## d2k_harkonnen

```diff
  "sections": {
+  "aircraft": {
+   "harkonnen_carryall": {
+    "armor": {
+     "src": "mods/cameo/ContentPacks/D2k/Harkonnen/yaml/aircraft.yaml#Armor.Type",
+     "v": "Light"
+    },
+    "buildable": true,
+    "cost": {
+     "src": "mods/cameo/ContentPacks/D2k/Harkonnen/yaml/aircraft.yaml#Valued.Cost",
+     "v": "600"
+    },
```

## d2k_ordos

```diff
    },
+   "ordos_chemturret": {
+    "armaments": [
+     {
+      "damage_warheads": [
+       {
+        "damage": "2000",
+        "falloff": "100, 88, 72, 50, 0",
+        "spread": "183",
+        "tag": "Chemical_Light",
+        "type": "AreaDamage"
+       }
```

## tiberiandawn_gdi

```diff
        {
-        "damage": "8000",
-        "falloff": "100, 0",
-        "spread": "67",
-        "tag": "Bullet_Light",
-        "type": "AreaDamage"
-       },
-       {
-        "damage": "8000",
+        "damage": "16000",
         "falloff": "100, 0",
@@ -4006,3 +3999,3 @@
```

