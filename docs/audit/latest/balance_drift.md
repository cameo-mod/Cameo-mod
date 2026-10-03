# audit_balance_drift — yaml vs committed balance ledger

**33 ledger(s) drifted** — balance numbers were hand-edited in yaml, or a sanctioned apply run was not followed by re-extraction. Fix via the pipeline, never by hand:

## d2k_atreides

```diff
     ],
-    "resolved_firepower_modifiers": [],
+    "resolved_firepower_modifiers": [
+     {
+      "modifier": 50,
+      "src": "inherited",
+      "trait": "FirepowerMultiplier@GlobalBuffs",
+      "types": []
+     }
+    ],
     "self_heal_step": {
@@ -1514,2 +1521,8 @@
```

## d2k_corrino

```diff
     ],
-    "resolved_firepower_modifiers": [],
+    "resolved_firepower_modifiers": [
+     {
+      "modifier": 50,
+      "src": "inherited",
+      "trait": "FirepowerMultiplier@GlobalBuffs",
+      "types": []
+     }
+    ],
     "self_heal_step": {
@@ -1362,3 +1369,10 @@
```

## d2k_harkonnen

```diff
     ],
-    "resolved_firepower_modifiers": [],
+    "resolved_firepower_modifiers": [
+     {
+      "modifier": 50,
+      "src": "inherited",
+      "trait": "FirepowerMultiplier@GlobalBuffs",
+      "types": []
+     }
+    ],
     "self_heal_step": {
@@ -474,3 +481,10 @@
```

## d2k_ixian

```diff
     "name": "Alfayrus",
-    "resolved_firepower_modifiers": [],
+    "resolved_firepower_modifiers": [
+     {
+      "modifier": 50,
+      "src": "inherited",
+      "trait": "FirepowerMultiplier@GlobalBuffs",
+      "types": []
+     }
+    ],
     "self_heal_step": {
@@ -276,3 +283,10 @@
```

## d2k_ordos

```diff
     ],
-    "resolved_firepower_modifiers": [],
+    "resolved_firepower_modifiers": [
+     {
+      "modifier": 50,
+      "src": "inherited",
+      "trait": "FirepowerMultiplier@GlobalBuffs",
+      "types": []
+     }
+    ],
     "self_heal_step": {
@@ -454,3 +461,10 @@
```

## redalert2_allies

```diff
     ],
-    "resolved_firepower_modifiers": [],
+    "resolved_firepower_modifiers": [
+     {
+      "modifier": 50,
+      "src": "inherited",
+      "trait": "FirepowerMultiplier@GlobalBuffs",
+      "types": []
+     }
+    ],
     "self_heal_step": {
@@ -557,2 +564,8 @@
```

## redalert2_soviets

```diff
     ],
-    "resolved_firepower_modifiers": [],
+    "resolved_firepower_modifiers": [
+     {
+      "modifier": 50,
+      "src": "inherited",
+      "trait": "FirepowerMultiplier@GlobalBuffs",
+      "types": []
+     }
+    ],
     "self_heal_step": {
@@ -883,3 +890,10 @@
```

## redalert2_yuri

```diff
     },
-    "resolved_firepower_modifiers": [],
+    "resolved_firepower_modifiers": [
+     {
+      "modifier": 50,
+      "src": "inherited",
+      "trait": "FirepowerMultiplier@GlobalBuffs",
+      "types": []
+     }
+    ],
     "self_heal_step": {
@@ -1010,3 +1017,10 @@
```

## redalert2mod_asianalliance

```diff
     },
-    "resolved_firepower_modifiers": [],
+    "resolved_firepower_modifiers": [
+     {
+      "modifier": 50,
+      "src": "inherited",
+      "trait": "FirepowerMultiplier@GlobalBuffs",
+      "types": []
+     }
+    ],
     "self_heal_step": {
@@ -321,3 +328,10 @@
```

## redalert2mod_consortium

```diff
      {
+      "modifier": 50,
+      "src": "inherited",
+      "trait": "FirepowerMultiplier@GlobalBuffs",
+      "types": []
+     },
+     {
       "modifier": 10,
@@ -327,3 +333,10 @@
     "name": "Anti Air Scalpel",
-    "resolved_firepower_modifiers": [],
+    "resolved_firepower_modifiers": [
```

## redalert2mod_futuretech

```diff
       "modifier": 50,
+      "src": "inherited",
+      "trait": "FirepowerMultiplier@GlobalBuffs",
+      "types": []
+     },
+     {
+      "modifier": 50,
       "src": "mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/aircraft.yaml#FirepowerMultiplier@ra2hornet.Modifier",
@@ -266,2 +272,8 @@
      {
+      "modifier": 50,
+      "src": "inherited",
```

## redalert2mod_naxis

```diff
     },
-    "resolved_firepower_modifiers": [],
+    "resolved_firepower_modifiers": [
+     {
+      "modifier": 50,
+      "src": "inherited",
+      "trait": "FirepowerMultiplier@GlobalBuffs",
+      "types": []
+     }
+    ],
     "self_heal_step": {
@@ -428,2 +435,8 @@
```

## redalert2mod_schwarzermond

```diff
     ],
-    "resolved_firepower_modifiers": [],
+    "resolved_firepower_modifiers": [
+     {
+      "modifier": 50,
+      "src": "inherited",
+      "trait": "FirepowerMultiplier@GlobalBuffs",
+      "types": []
+     }
+    ],
     "self_heal_step": {
@@ -155,3 +162,10 @@
```

## redalert2mod_syndicate

```diff
      {
+      "modifier": 50,
+      "src": "inherited",
+      "trait": "FirepowerMultiplier@GlobalBuffs",
+      "types": []
+     },
+     {
       "modifier": 75,
@@ -388,3 +394,10 @@
     },
-    "resolved_firepower_modifiers": [],
+    "resolved_firepower_modifiers": [
```

## redalert2mod_tkm

```diff
       "modifier": 50,
+      "src": "inherited",
+      "trait": "FirepowerMultiplier@GlobalBuffs",
+      "types": []
+     },
+     {
+      "modifier": 50,
       "src": "mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/aircraft.yaml#FirepowerMultiplier@DualWeapon.Modifier",
@@ -390,3 +396,10 @@
     },
-    "resolved_firepower_modifiers": [],
+    "resolved_firepower_modifiers": [
```

## redalert_allies

```diff
     },
-    "resolved_firepower_modifiers": [],
+    "resolved_firepower_modifiers": [
+     {
+      "modifier": 50,
+      "src": "inherited",
+      "trait": "FirepowerMultiplier@GlobalBuffs",
+      "types": []
+     }
+    ],
     "self_heal_step": {
@@ -366,3 +373,10 @@
```

## redalert_japan

```diff
     },
-    "resolved_firepower_modifiers": [],
+    "resolved_firepower_modifiers": [
+     {
+      "modifier": 50,
+      "src": "inherited",
+      "trait": "FirepowerMultiplier@GlobalBuffs",
+      "types": []
+     }
+    ],
     "self_heal_step": {
@@ -478,3 +485,10 @@
```

## redalert_soviets

```diff
     },
-    "resolved_firepower_modifiers": [],
+    "resolved_firepower_modifiers": [
+     {
+      "modifier": 50,
+      "src": "inherited",
+      "trait": "FirepowerMultiplier@GlobalBuffs",
+      "types": []
+     }
+    ],
     "self_heal_step": {
@@ -810,3 +817,10 @@
```

## shared_d2k

```diff
      {
+      "modifier": 50,
+      "src": "inherited",
+      "trait": "FirepowerMultiplier@GlobalBuffs",
+      "types": []
+     },
+     {
       "modifier": 100,
@@ -1034,3 +1040,10 @@
     ],
-    "resolved_firepower_modifiers": [],
+    "resolved_firepower_modifiers": [
```

## shared_redalert

```diff
     ],
-    "resolved_firepower_modifiers": [],
+    "resolved_firepower_modifiers": [
+     {
+      "modifier": 50,
+      "src": "inherited",
+      "trait": "FirepowerMultiplier@GlobalBuffs",
+      "types": []
+     }
+    ],
     "self_heal_step": {
@@ -1492,3 +1499,10 @@
```

## shared_redalert2

```diff
     ],
-    "resolved_firepower_modifiers": [],
+    "resolved_firepower_modifiers": [
+     {
+      "modifier": 50,
+      "src": "inherited",
+      "trait": "FirepowerMultiplier@GlobalBuffs",
+      "types": []
+     }
+    ],
     "self_heal_step": {
@@ -303,3 +310,10 @@
```

## shared_tiberiansun

```diff
     },
-    "resolved_firepower_modifiers": [],
+    "resolved_firepower_modifiers": [
+     {
+      "modifier": 50,
+      "src": "inherited",
+      "trait": "FirepowerMultiplier@GlobalBuffs",
+      "types": []
+     }
+    ],
     "self_heal_step": {
@@ -613,3 +620,10 @@
```

## starcraft_protoss

```diff
     },
-    "resolved_firepower_modifiers": [],
+    "resolved_firepower_modifiers": [
+     {
+      "modifier": 50,
+      "src": "inherited",
+      "trait": "FirepowerMultiplier@GlobalBuffs",
+      "types": []
+     }
+    ],
     "self_heal_step": {
@@ -212,3 +219,10 @@
```

## starcraft_terran

```diff
     },
-    "resolved_firepower_modifiers": [],
+    "resolved_firepower_modifiers": [
+     {
+      "modifier": 50,
+      "src": "inherited",
+      "trait": "FirepowerMultiplier@GlobalBuffs",
+      "types": []
+     }
+    ],
     "self_heal_step": {
@@ -420,3 +427,10 @@
```

## starcraft_zerg

```diff
     },
-    "resolved_firepower_modifiers": [],
+    "resolved_firepower_modifiers": [
+     {
+      "modifier": 50,
+      "src": "inherited",
+      "trait": "FirepowerMultiplier@GlobalBuffs",
+      "types": []
+     }
+    ],
     "self_heal_step": {
@@ -230,3 +237,10 @@
```

## tiberiandawn_gdi

```diff
     "name": "A10 Bomber",
-    "resolved_firepower_modifiers": [],
+    "resolved_firepower_modifiers": [
+     {
+      "modifier": 50,
+      "src": "inherited",
+      "trait": "FirepowerMultiplier@GlobalBuffs",
+      "types": []
+     }
+    ],
     "self_heal_step": {
@@ -235,3 +242,10 @@
```

## tiberiandawn_nod

```diff
     },
-    "resolved_firepower_modifiers": [],
+    "resolved_firepower_modifiers": [
+     {
+      "modifier": 50,
+      "src": "inherited",
+      "trait": "FirepowerMultiplier@GlobalBuffs",
+      "types": []
+     }
+    ],
     "self_heal_step": {
@@ -345,3 +352,10 @@
```

## tiberiansun_cabal

```diff
     "resolved_firepower_modifiers": [
+     {
+      "modifier": 50,
+      "src": "inherited",
+      "trait": "FirepowerMultiplier@GlobalBuffs",
+      "types": []
+     },
      {
@@ -175,3 +181,10 @@
     "name": "Hunter Drone",
-    "resolved_firepower_modifiers": [],
+    "resolved_firepower_modifiers": [
```

## tiberiansun_forgotten

```diff
     },
-    "resolved_firepower_modifiers": [],
+    "resolved_firepower_modifiers": [
+     {
+      "modifier": 50,
+      "src": "inherited",
+      "trait": "FirepowerMultiplier@GlobalBuffs",
+      "types": []
+     }
+    ],
     "self_heal_step": {
@@ -358,3 +365,10 @@
```

## tiberiansun_gdi

```diff
     },
-    "resolved_firepower_modifiers": [],
+    "resolved_firepower_modifiers": [
+     {
+      "modifier": 50,
+      "src": "inherited",
+      "trait": "FirepowerMultiplier@GlobalBuffs",
+      "types": []
+     }
+    ],
     "self_heal_step": {
@@ -368,3 +375,10 @@
```

## tiberiansun_nod

```diff
     },
-    "resolved_firepower_modifiers": [],
+    "resolved_firepower_modifiers": [
+     {
+      "modifier": 50,
+      "src": "inherited",
+      "trait": "FirepowerMultiplier@GlobalBuffs",
+      "types": []
+     }
+    ],
     "self_heal_step": {
@@ -390,3 +397,10 @@
```

## warcraft2_humans

```diff
     ],
-    "resolved_firepower_modifiers": [],
+    "resolved_firepower_modifiers": [
+     {
+      "modifier": 50,
+      "src": "inherited",
+      "trait": "FirepowerMultiplier@GlobalBuffs",
+      "types": []
+     }
+    ],
     "self_heal_step": {
@@ -1213,3 +1220,10 @@
```

## warcraft2_orcs

```diff
     ],
-    "resolved_firepower_modifiers": [],
+    "resolved_firepower_modifiers": [
+     {
+      "modifier": 50,
+      "src": "inherited",
+      "trait": "FirepowerMultiplier@GlobalBuffs",
+      "types": []
+     }
+    ],
     "self_heal_step": {
@@ -1094,3 +1101,10 @@
```

