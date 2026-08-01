# audit_balance_drift — yaml vs committed balance ledger

**31 ledger(s) drifted** — balance numbers were hand-edited in yaml, or a sanctioned apply run was not followed by re-extraction. Fix via the pipeline, never by hand:

## d2k_atreides

```diff
       "weapon": "80mm_A",
-      "weapon_class_source": "template",
+      "weapon_class_source": "versus_shield",
       "weapon_types": [
```

## d2k_harkonnen

```diff
       "weapon": "harkonnen_autogunturret",
-      "weapon_class_source": "template",
+      "weapon_class_source": "versus_shield",
       "weapon_types": [
@@ -625,3 +625,3 @@
       "weapon": "D2K_TowerMissile",
-      "weapon_class_source": "template",
+      "weapon_class_source": "versus_shield",
       "weapon_types": [
@@ -804,3 +804,3 @@
       "weapon": "80mm_H",
-      "weapon_class_source": "template",
```

## d2k_ixian

```diff
   "aircraft": {
-   "farasha_drone.ixian": {
+   "farasha_drone_ixian": {
     "armaments": [
@@ -12,3 +12,3 @@
       "defined_in": "mods/cameo/ContentPacks/D2k/Ixian/yaml/weapons.yaml",
-      "design_weapon_class": 0.9166666666666666,
+      "design_weapon_class": 1.0,
       "pricing": true,
@@ -69,4 +69,4 @@
       ],
-      "weapon": "farasha_drone.ixian",
```

## d2k_ordos

```diff
       "weapon": "d2kCarryallChainGun",
-      "weapon_class_source": "template",
+      "weapon_class_source": "versus_shield",
       "weapon_types": [
@@ -81,3 +81,3 @@
       "weapon": "d2kCarryallChainGun_upgrade",
-      "weapon_class_source": "template",
+      "weapon_class_source": "versus_shield",
       "weapon_types": [
@@ -155,3 +155,3 @@
       "weapon": "d2kCarryallChainGun",
-      "weapon_class_source": "template",
```

## redalert2_allies

```diff
       "weapon": "BlackEagleMissiles",
-      "weapon_class_source": "template",
+      "weapon_class_source": "versus_shield",
       "weapon_types": [
@@ -121,3 +121,3 @@
       "weapon": "BlackEagleMissiles_elite",
-      "weapon_class_source": "template",
+      "weapon_class_source": "versus_shield",
       "weapon_types": [
@@ -253,3 +253,3 @@
       "weapon": "BlackEagleThunderboltMissiles",
-      "weapon_class_source": "template",
```

## redalert2_soviets

```diff
       "weapon": "RA2KirovBomb",
-      "weapon_class_source": "template",
+      "weapon_class_source": "versus_shield",
       "weapon_types": [
@@ -88,3 +88,3 @@
       "weapon": "RA2KirovBomb_rad",
-      "weapon_class_source": "template",
+      "weapon_class_source": "versus_shield",
       "weapon_types": [
@@ -124,3 +124,3 @@
       "weapon": "RA2KirovBomb_fire",
-      "weapon_class_source": "template",
```

## redalert2_yuri

```diff
       "weapon": "RA2DiskLaser",
-      "weapon_class_source": "template",
+      "weapon_class_source": "versus_shield",
       "weapon_types": [
@@ -73,3 +73,3 @@
       "weapon": "RA2DiskLaser_elite",
-      "weapon_class_source": "template",
+      "weapon_class_source": "versus_shield",
       "weapon_types": [
@@ -124,3 +124,3 @@
       "weapon": "RA2DiskDrain",
-      "weapon_class_source": "template",
```

## redalert2mod_asianalliance

```diff
   "aircraft": {
+   "asianalliance_harbinger": {
+    "armaments": [
+     {
+      "armament_name": "primary",
+      "burst": "2",
+      "burstdelays": "5",
+      "defined_in": "mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml",
+      "design_weapon_class": 0.9642857142857143,
+      "pricing": true,
+      "range": "7777",
+      "reloaddelay": "60",
```

## redalert2mod_consortium

```diff
       "weapon": "SteelFighterRailgun",
-      "weapon_class_source": "template",
+      "weapon_class_source": "versus_shield",
       "weapon_types": [
@@ -196,4 +196,4 @@
       ],
-      "weapon": "SteelMakoGunEMP",
-      "weapon_class_source": "template",
+      "weapon": "SteelMakoGun_EMP",
+      "weapon_class_source": "versus_shield",
       "weapon_types": [
@@ -215,3 +215,3 @@
```

## redalert2mod_futuretech

```diff
       "weapon": "Future_Cryocopter_Rocket",
-      "weapon_class_source": "template",
+      "weapon_class_source": "versus_shield",
       "weapon_types": [
@@ -156,3 +156,3 @@
       "weapon": "Future_Cryocopter_Cryo",
-      "weapon_class_source": "template",
+      "weapon_class_source": "versus_shield",
       "weapon_types": [
@@ -377,3 +377,3 @@
       "weapon": "FutureHarbingerCannon",
-      "weapon_class_source": "template",
```

## redalert2mod_naxis

```diff
       "weapon": "JapanSuperBomb",
-      "weapon_class_source": "template",
+      "weapon_class_source": "versus_shield",
       "weapon_types": [
@@ -222,4 +222,4 @@
       ],
-      "weapon": "NaxPlanegunE",
-      "weapon_class_source": "template",
+      "weapon": "NaxPlanegun_elite",
+      "weapon_class_source": "versus_shield",
       "weapon_types": [
@@ -283,4 +283,4 @@
```

## redalert2mod_schwarzermond

```diff
       "weapon": "NaxCorrosionRocket",
-      "weapon_class_source": "template",
+      "weapon_class_source": "versus_shield",
       "weapon_types": [
@@ -196,3 +196,3 @@
       "weapon": "NaxDieGlocke",
-      "weapon_class_source": "template",
+      "weapon_class_source": "versus_shield",
       "weapon_types": [
@@ -312,3 +312,3 @@
       "weapon": "LunarNaxiDroneLaser",
-      "weapon_class_source": "template",
```

## redalert2mod_syndicate

```diff
       "weapon": "BlackHawkCannon",
-      "weapon_class_source": "template",
+      "weapon_class_source": "versus_shield",
       "weapon_types": [
@@ -154,3 +154,3 @@
       "weapon": "MigMissiles",
-      "weapon_class_source": "template",
+      "weapon_class_source": "versus_shield",
       "weapon_types": [
@@ -218,4 +218,4 @@
       ],
-      "weapon": "MigMissilesE",
```

## redalert2mod_tkm

```diff
       "weapon": "HueyGun",
-      "weapon_class_source": "template",
+      "weapon_class_source": "versus_shield",
       "weapon_types": [
@@ -145,3 +145,3 @@
       "weapon": "HueyCryoMissiles",
-      "weapon_class_source": "template",
+      "weapon_class_source": "versus_shield",
       "weapon_types": [
@@ -222,3 +222,3 @@
       "weapon": "HueyTwinMissiles",
-      "weapon_class_source": "template",
```

## redalert_allies

```diff
       "weapon": "ChainGunMH60",
-      "weapon_class_source": "template",
+      "weapon_class_source": "versus_shield",
       "weapon_types": [
@@ -178,3 +178,3 @@
       "weapon": "ChainGunMH60",
-      "weapon_class_source": "template",
+      "weapon_class_source": "versus_shield",
       "weapon_types": [
@@ -273,3 +273,3 @@
       "weapon": "Hellfire",
-      "weapon_class_source": "template",
```

## redalert_japan

```diff
       "weapon": "ZeroFighterChainGun",
-      "weapon_class_source": "template",
+      "weapon_class_source": "versus_shield",
       "weapon_types": [
@@ -102,3 +102,3 @@
       "weapon": "ZeroFighterChainGunWaveforce",
-      "weapon_class_source": "template",
+      "weapon_class_source": "versus_shield",
       "weapon_types": [
@@ -138,3 +138,3 @@
       "weapon": "JapaneseHeavyBomb",
-      "weapon_class_source": "template",
```

## redalert_soviets

```diff
       "weapon": "ArmoredYakChainGun",
-      "weapon_class_source": "template",
+      "weapon_class_source": "versus_shield",
       "weapon_types": [
@@ -68,3 +68,3 @@
       "weapon": "ArmoredYakChainGun",
-      "weapon_class_source": "template",
+      "weapon_class_source": "versus_shield",
       "weapon_types": [
@@ -115,3 +115,3 @@
       "weapon": "IncendiaryArmoredYakChainGun",
-      "weapon_class_source": "template",
```

## shared_d2k

```diff
       "weapon": "HMG_fremen",
-      "weapon_class_source": "template",
+      "weapon_class_source": "versus_shield",
       "weapon_types": [
@@ -471,3 +471,3 @@
       "weapon": "Fremen_RPG",
-      "weapon_class_source": "template",
+      "weapon_class_source": "versus_shield",
       "weapon_types": [
@@ -545,3 +545,3 @@
       "weapon": "light_inf_lmg",
-      "weapon_class_source": "template",
```

## shared_redalert

```diff
       "weapon": "JapanSuperBomb",
-      "weapon_class_source": "template",
+      "weapon_class_source": "versus_shield",
       "weapon_types": [
@@ -242,3 +242,3 @@
       "weapon": "ParaBomb",
-      "weapon_class_source": "template",
+      "weapon_class_source": "versus_shield",
       "weapon_types": [
@@ -385,3 +385,3 @@
       "weapon": "ParaBomb",
-      "weapon_class_source": "template",
```

## shared_redalert2

```diff
       "weapon": "RA2Shovel",
-      "weapon_class_source": "template",
+      "weapon_class_source": "versus_shield",
       "weapon_types": [
@@ -110,3 +110,3 @@
       "weapon": "RA2Shovel",
-      "weapon_class_source": "template",
+      "weapon_class_source": "versus_shield",
       "weapon_types": [
@@ -235,3 +235,3 @@
       "weapon": "RA220mmrapid",
-      "weapon_class_source": "template",
```

## starcraft_protoss

```diff
       "weapon": "ArbiterCannon",
-      "weapon_class_source": "template",
+      "weapon_class_source": "versus_shield",
       "weapon_types": [
@@ -217,3 +217,3 @@
       ],
-      "weapon": "CorsairEMP",
+      "weapon": "Corsair_EMP",
       "weapon_class_source": "template",
@@ -285,3 +285,3 @@
       "weapon": "CorsairFlash",
-      "weapon_class_source": "template",
```

## starcraft_terran

```diff
       "weapon": "BCLaser",
-      "weapon_class_source": "template",
+      "weapon_class_source": "versus_shield",
       "weapon_types": [
@@ -278,3 +278,3 @@
       "weapon": "BCYamatoCannon",
-      "weapon_class_source": "template",
+      "weapon_class_source": "versus_shield",
       "weapon_types": [
@@ -401,3 +401,3 @@
       "weapon": "MedicHeal",
-      "weapon_class_source": "template",
```

## starcraft_zerg

```diff
       "weapon": "BehemothShoot",
-      "weapon_class_source": "template",
+      "weapon_class_source": "versus_shield",
       "weapon_types": [
@@ -191,3 +191,3 @@
       "weapon": "BroodweaverLeech",
-      "weapon_class_source": "template",
+      "weapon_class_source": "versus_shield",
       "weapon_types": [
@@ -265,3 +265,3 @@
       "weapon": "CorruptorSpore",
-      "weapon_class_source": "template",
```

## tiberiandawn_gdi

```diff
       "weapon": "Vulcan",
-      "weapon_class_source": "template",
+      "weapon_class_source": "versus_shield",
       "weapon_types": [
@@ -79,3 +79,3 @@
       "weapon": "Napalm",
-      "weapon_class_source": "template",
+      "weapon_class_source": "versus_shield",
       "weapon_types": [
@@ -145,3 +145,3 @@
       "weapon": "VulcanA10Carrier",
-      "weapon_class_source": "template",
```

## tiberiandawn_nod

```diff
       "weapon": "HeliGun",
-      "weapon_class_source": "template",
+      "weapon_class_source": "versus_shield",
       "weapon_types": [
@@ -64,3 +64,3 @@
       "weapon": "HeliMissiles",
-      "weapon_class_source": "template",
+      "weapon_class_source": "versus_shield",
       "weapon_types": [
@@ -215,3 +215,3 @@
       "weapon": "VenomLaser",
-      "weapon_class_source": "template",
```

## tiberiansun_cabal

```diff
       "weapon": "CabalWaspLaserStriker",
-      "weapon_class_source": "template",
+      "weapon_class_source": "versus_shield",
       "weapon_types": [
@@ -124,3 +124,3 @@
       "weapon": "CabalOverkillDroneLaser",
-      "weapon_class_source": "template",
+      "weapon_class_source": "versus_shield",
       "weapon_types": [
@@ -138,3 +138,3 @@
      "src": "mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/aircraft.yaml#Valued.Cost",
-     "v": "500"
```

## tiberiansun_forgotten

```diff
       "weapon": "TSApacheMissile",
-      "weapon_class_source": "template",
+      "weapon_class_source": "versus_shield",
       "weapon_types": [
@@ -212,3 +212,3 @@
       "weapon": "TSChemApacheMissile",
-      "weapon_class_source": "template",
+      "weapon_class_source": "versus_shield",
       "weapon_types": [
@@ -434,3 +434,3 @@
       "weapon": "TSCobraMissile",
-      "weapon_class_source": "template",
```

## tiberiansun_gdi

```diff
       "weapon": "TSHammerheadGun",
-      "weapon_class_source": "template",
+      "weapon_class_source": "versus_shield",
       "weapon_types": [
@@ -279,3 +279,3 @@
       "weapon": "KodiakCannon",
-      "weapon_class_source": "template",
+      "weapon_class_source": "versus_shield",
       "weapon_types": [
@@ -402,3 +402,3 @@
       "weapon": "KodiakCannonSonic",
-      "weapon_class_source": "template",
```

## tiberiansun_nod

```diff
       "weapon": "TSProton",
-      "weapon_class_source": "template",
+      "weapon_class_source": "versus_shield",
       "weapon_types": [
@@ -131,3 +131,3 @@
       "weapon": "TSHarpyClaw",
-      "weapon_class_source": "template",
+      "weapon_class_source": "versus_shield",
       "weapon_types": [
@@ -162,3 +162,3 @@
       "weapon": "TSHarpyMultiClaw",
-      "weapon_class_source": "template",
```

## warcraft2_humans

```diff
       "weapon": "wc2gryphonFireVisible",
-      "weapon_class_source": "template",
+      "weapon_class_source": "versus_shield",
       "weapon_types": [
@@ -867,3 +867,3 @@
       "weapon": "wc2_tower_arrow",
-      "weapon_class_source": "template",
+      "weapon_class_source": "versus_shield",
       "weapon_types": [
@@ -1094,3 +1094,3 @@
       "weapon": "wc2cannontowerFire",
-      "weapon_class_source": "template",
```

## warcraft2_orcs

```diff
       "weapon": "wc2dragonFireVisible",
-      "weapon_class_source": "template",
+      "weapon_class_source": "versus_shield",
       "weapon_types": [
@@ -453,3 +453,3 @@
       "weapon": "wc2dragonFireExplosion",
-      "weapon_class_source": "template",
+      "weapon_class_source": "versus_shield",
       "weapon_types": [
@@ -1178,3 +1178,3 @@
       "weapon": "wc2_tower_arrow",
-      "weapon_class_source": "template",
```

