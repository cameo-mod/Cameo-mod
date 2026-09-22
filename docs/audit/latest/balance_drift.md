# audit_balance_drift — yaml vs committed balance ledger

**5 ledger(s) drifted** — balance numbers were hand-edited in yaml, or a sanctioned apply run was not followed by re-extraction. Fix via the pipeline, never by hand:

## d2k_atreides

```diff
     },
-    "name": "Atreides Advanced Carryall",
+    "name": "actor_atreides_advancedcarryall.name",
     "prerequisites": [
@@ -126,3 +126,3 @@
     },
-    "name": "Atreides Air Drone",
+    "name": "actor_atreides_airdrone.name",
     "prerequisites": [
@@ -277,3 +277,3 @@
     },
-    "name": "Ornithopter",
```

## d2k_corrino

```diff
     },
-    "name": "Corrino Advanced Carryall",
+    "name": "actor_corrino_advancedcarryall.name",
     "prerequisites": [
@@ -89,3 +89,3 @@
     },
-    "name": "Corrino Carryall",
+    "name": "actor_corrino_carryall.name",
     "prerequisites": [
@@ -172,3 +172,3 @@
     },
-    "name": "Corrino Gunship",
```

## d2k_harkonnen

```diff
     },
-    "name": "Harkonnen Advanced Carryall",
+    "name": "actor_harkonnen_advancedcarryall.name",
     "prerequisites": [
@@ -90,3 +90,3 @@
     },
-    "name": "Harkonnen Carryall",
+    "name": "actor_harkonnen_carryall.name",
     "prerequisites": [
@@ -173,3 +173,3 @@
     },
-    "name": "Harkonnen Gunship",
```

## d2k_ixian

```diff
     },
-    "name": "Ixian Air Drone",
+    "name": "actor_ixian_airdrone.name",
     "prerequisites": [
@@ -385,3 +385,3 @@
     },
-    "name": "Ixian EMP Bomber",
+    "name": "actor_ixian_empbomber.name",
     "prerequisites": [
@@ -596,3 +596,3 @@
     },
-    "name": "Farasha",
```

## d2k_ordos

```diff
     },
-    "name": "Advanced Carryall",
+    "name": "actor_ordos_advancedcarryall.name",
     "prerequisites": [
@@ -371,3 +371,3 @@
     },
-    "name": "Air Mine",
+    "name": "actor_ordos_airmine.name",
     "prerequisites": [
@@ -585,3 +585,3 @@
     },
-    "name": "Banshee",
```

