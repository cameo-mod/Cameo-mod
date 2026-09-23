# Weapon shape — the ONE-WARHEAD / THREE-INHERIT law

**Maintainer ruling, 2026-09-06.** Every concrete weapon ends with exactly three inherits — `^Warhead_*`, `^Projectile_*`, `^Effect_*` — one main warhead, and no effect warheads of its own. Mechanic warheads (`FireShrapnel`, `GrantExternalCondition`) and the `*Percentage` / `*FriendlyFire` / `*ExtraDamage` halves of one main are NOT violations.

⛔ This **repeals the exemption** in `tools/audit/intentional_composites.py`. Its 224 entries are no longer 'reviewed, keep' — they are the worklist. The registry data stays useful: it says which mains someone chose on purpose.

concrete weapons with inherits: **2152**

W5 counts structural flat-damage nodes, including zero/healing/ally-only nodes; the split audit counts positive non-companion damage. Both resolve the full concrete weapon corpus. Use `--compare-split` for exact differences.

| check | what | count | ratchet |
|---|---|--:|--:|
| W1 | more than 3 inherits | **506** (23.52% of 2152) | 26.16% |
| W2 | two or more `^Warhead_*` inherits | **281** ⛔ | 177 |
| W3 | two or more `^Projectile_*` inherits | **12** | 12 |
| W4 | two or more `^Effect_*` inherits | **50** | 51 |
| W5 | more than one resolved MAIN warhead | **167** | 389 |
| W6 | effect warheads declared LOCALLY | **691** | 692 |
| W7 | inherits from ANOTHER WEAPON, not a template | **963** ⛔ | 957 |
| W8 | inherits a `^Template` that is not one of the three kinds | **675** | 858 |

| I7 informational — missing template | weapons |
|---|--:|
| no `^Effect_*` inherit | 1261 |
| no `^Projectile_*` inherit | 1442 |
| no `^Warhead_*` inherit | 1036 |

_I7 is a REVIEW QUEUE, not a defect count — an instant or utility weapon may legitimately have no projectile. Do not ratchet it without a per-weapon pass._


## W7 — inherits from ANOTHER WEAPON, not a template (963 vs ratchet 957)

| weapon | weapon-parents | first four |
|---|---|---|
| `105mmirak` | 1 | `105mm` |
| `120mm_cobra_deploy` | 1 | `120mm_cobra` |
| `120mm_python` | 1 | `120mm_cobra` |
| `120mm_python_deploy` | 1 | `120mm_python` |
| `155mmCryo` | 1 | `155mm` |
| `25mmWaveforce` | 1 | `25mm` |
| `AAGunBoatCannon_elite` | 1 | `RA2155mm` |
| `AAGunBoatFlak` | 1 | `RA2FlakTrackGun` |
| `AAGunBoatFlak_elite` | 1 | `AAGunBoatFlak` |
| `ASDFGun2` | 1 | `ASDFGun` |
| `AnthraxCloudBlue` | 1 | `AnthraxCloud` |
| `AnthraxCloudBlueLarge` | 1 | `AnthraxCloudBlue` |
| `AnthraxCloudLarge` | 1 | `AnthraxCloud` |
| `AnthraxCloudPurple` | 1 | `AnthraxCloud` |
| `AnthraxCloudPurpleLarge` | 1 | `AnthraxCloudPurple` |
| `ArbiterCannon` | 1 | `PhotonCannon` |
| `ArmoredCarMGAAWaveforce` | 1 | `ArmoredCarMG_AA` |
| `ArmoredCarMGWaveforce` | 1 | `ArmoredCarMG` |
| `ArmoredCarMG_AA` | 1 | `ArmoredCarMG` |
| `Arrakis_Tanya_Guns` | 1 | `Fremen_Upg` |
| `ArtilleryExplode` | 1 | `155mm` |
| `AsianChaosMine` | 2 | `AsianChaosTurret` · `AsianTankMine` |
| `AsianChemical_elite` | 1 | `AsianChemical` |
| `AsianCrocBite_elite` | 1 | `AsianCrocBite` |
| `AsianFlameFragment2` | 1 | `AsianFlameFragment` |
| `AsianFlamerTank` | 1 | `AsianFlamerTurret` |
| `AsianFlamerTank_elite` | 1 | `AsianFlamerTank` |
| `AsianFlamerTroop` | 1 | `AsianFlamerTurret` |
| `AsianFlamerTroop2` | 1 | `AsianFlamerTroop` |
| `AsianFlamerTroopExplode` | 1 | `AsianFlamerTurret` |
| `AsianGrenade_elite` | 1 | `AsianGrenade` |
| `AsianHowitzerCannon_elite` | 1 | `AsianHowitzerCannon` |
| `AsianLynxMG_elite` | 1 | `AsianLynxMG` |
| `AsianLynxTankCannon_elite` | 1 | `AsianLynxTankCannon` |
| `AsianMaidenBow` | 1 | `AsianPhotonCannon` |
| `AsianMaidenBow_elite` | 1 | `AsianMaidenBow` |
| `AsianNinjaStar_elite` | 1 | `AsianNinjaStar` |
| `AsianOilBombFragments` | 1 | `AsianFlameFragment` |
| `AsianPelicanMG_elite` | 1 | `AsianPelicanMG` |
| `AsianPelicanMissile_elite` | 1 | `AsianPelicanMissile` |


_... and 923 more._


## W8 — inherits a `^Template` that is not one of the three kinds (675 vs ratchet 858)

| weapon | legacy templates | first four |
|---|---|---|
| `110mm_Gun` | 1 | `^D2K_Cannon` |
| `120mm_cobra` | 1 | `^D2K_Cannon` |
| `120mm_td` | 1 | `^D2K_Cannon` |
| `12MissilesSpawnerScud` | 2 | `^RA2Grenade` · `^RA2HeavyMissile` |
| `155mm` | 3 | `^HeavyCannon` · `^ShrapnelWeapon` · `^Grenade` |
| `155mmCryo` | 1 | `^CryoMissileProjectile` |
| `25mm` | 5 | `^Grenade` · `^ShrapnelWeapon` · `^LightFlameWeapon` · `^MediumChemicalWeapon` |
| `25mmWaveforce` | 2 | `^WaveforceBulletWarhead` · `^WaveforceBulletProjectile` |
| `80mm_A` | 1 | `^D2K_Cannon` |
| `80mm_H` | 1 | `^D2K_Cannon` |
| `AAGunBoatCannon` | 1 | `^RA2HeavyCannon` |
| `AAGunBoatCannon_elite` | 1 | `^RA2EliteEffects` |
| `AAHyperionMagnet` | 1 | `^RA2LaserWeapon` |
| `ACV_Machinegun` | 2 | `^RA2SmallArms` · `^RA2Chaingun` |
| `ASDFGun` | 2 | `^RA2SmallArms` · `^RA2Chaingun` |
| `ATMine` | 3 | `^DamagingExplosionHE` · `^HeavyMissile` · `^ATMineDemolitionCompatibility` |
| `AnthraxCloud` | 1 | `^ToxicWeapon` |
| `ArmoredCarMG` | 8 | `^ArrowWeapon` · `^TankDestroyerCannon` · `^SmallArms` · `^Grenade` |
| `ArmoredCarMGAAWaveforce` | 2 | `^WaveforceBulletWarhead` · `^WaveforceBulletProjectile` |
| `ArmoredCarMGWaveforce` | 2 | `^WaveforceBulletWarhead` · `^WaveforceBulletProjectile` |
| `AsianChemical` | 6 | `^LightChemicalWeapon` · `^MediumChemicalWeapon` · `^HeavyChemicalWeapon` · `^HeavyBomb` |
| `AsianChemicalBombs` | 1 | `^RA2MediumCannon` |
| `AsianGrenade` | 1 | `^RA2MediumCannon` |
| `AsianHarbingerPlasma` | 7 | `^LightFlameWeapon` · `^LightChemicalWeapon` · `^MediumFlameWeapon` · `^MediumChemicalWeapon` |
| `AsianHowitzerCannon` | 1 | `^RA2HeavyCannon` |
| `AsianHowitzerSplash` | 1 | `^AsianHowitzerSplashLegacy` |
| `AsianIonBeamMini` | 1 | `^AsianIonBeam` |
| `AsianIonBeamMiniStart` | 1 | `^AsianIonBeam` |
| `AsianIonbeam` | 1 | `^AsianIonBeam` |
| `AsianKamikazeChaingun` | 2 | `^RA2SmallArms` · `^RA2Chaingun` |
| `AsianLynxMG` | 2 | `^RA2SmallArms` · `^RA2Chaingun` |
| `AsianLynxMG_elite` | 1 | `^RA2EliteEffects` |
| `AsianLynxTankCannon` | 6 | `^Grenade` · `^ShrapnelWeapon` · `^LightFlameWeapon` · `^MediumChemicalWeapon` |
| `AsianLynxTankCannon_elite` | 1 | `^RA2EliteEffects` |
| `AsianMLRS` | 4 | `^HeavyMissile` · `^FlakWeapon` · `^RA2Grenade` · `^AsianRA2MediumMissile` |
| `AsianMaidenBow` | 2 | `^Grenade` · `^ArrowWeapon` |
| `AsianPelicanMG` | 2 | `^RA2HeavyCannon` · `^RA2Chaingun` |
| `AsianPelicanMissile` | 1 | `^RA2MediumMissile` |
| `AsianPhoenixRocket` | 1 | `^RA2HeavyMissile` |
| `AsianPhotonCannon` | 4 | `^MediumMissile` · `^FlakWeapon` · `^TeslaWeapon` · `^MagicWeapon` |


_... and 635 more._


## W1 — more than 3 inherits (506 vs ratchet 576)

| weapon | inherits | first four |
|---|---|---|
| `110mm_Gun` | 8 | `^Warhead_CannonAP_Light_Flat` · `^Warhead_CannonHE_Heavy` · `^Projectile_Shell_Heavy` · `^Effect_CannonHE_Heavy` |
| `120mm_cobra` | 4 | `^Warhead_CannonAP` · `^Projectile_Shell_Light` · `^Effect_CannonAP_Light` · `^D2K_Cannon` |
| `12MissilesSpawnerScud` | 7 | `^Warhead_Demolition_Heavy` · `^Warhead_Flame_Medium` · `^Projectile_Flame_Medium` · `^Effect_Flame_Medium` |
| `155mm` | 4 | `^Warhead_Concussion_Heavy` · `^HeavyCannon` · `^ShrapnelWeapon` · `^Grenade` |
| `25mm` | 9 | `^Warhead_CannonHE_Medium_Flat` · `^Warhead_CannonHE_Medium` · `^Projectile_Shell_Medium` · `^Effect_CannonHE_Medium` |
| `8Inch` | 4 | `^Warhead_Demolition_Heavy_Flat` · `^Warhead_Demolition_Heavy` · `^Projectile_Grenade_Light` · `^Effect_Demolition_Light` |
| `APCGun` | 5 | `^Warhead_Flak_Medium_Flat` · `^Warhead_Bullet_Medium` · `^Warhead_Flak_Medium` · `^Projectile_Flak_Medium` |
| `ASDFGun2` | 4 | `^Warhead_Railgun_Heavy` · `^Projectile_Railgun_Heavy` · `^Effect_Railgun_Heavy` · `ASDFGun` |
| `ArmoredCarMG` | 9 | `^Warhead_Bullet_Medium` · `^ArrowWeapon` · `^TankDestroyerCannon` · `^SmallArms` |
| `ArtilleryShell` | 5 | `^Warhead_Concussion_Medium_Flat` · `^Warhead_Demolition_Light` · `^Warhead_Concussion_Medium` · `^Projectile_Grenade_Light` |
| `AsianChaosMine` | 5 | `AsianChaosTurret` · `AsianTankMine` · `^Warhead_Chemical_Heavy` · `^Projectile_Chem_Heavy` |
| `AsianChemical` | 7 | `^Warhead_Chemical_Medium_Flat` · `^LightChemicalWeapon` · `^MediumChemicalWeapon` · `^HeavyChemicalWeapon` |
| `AsianGrenade` | 4 | `^Warhead_Concussion_Medium_Flat` · `^Warhead_Concussion_Medium` · `^Effect_Concussion_Medium` · `^RA2MediumCannon` |
| `AsianHarbingerPlasma` | 13 | `^Warhead_Plasma_Medium_Flat` · `^Warhead_Plasma_Medium` · `^Warhead_CannonHE_Medium` · `^Projectile_Shell_Medium` |
| `AsianLynxTankCannon` | 7 | `^Warhead_CannonHE_Medium_Flat` · `^Grenade` · `^ShrapnelWeapon` · `^LightFlameWeapon` |
| `AsianMLRS` | 6 | `^Warhead_MissileAP_Medium` · `^Warhead_MissileAA_Medium_Flat` · `^HeavyMissile` · `^FlakWeapon` |
| `AsianMaidenBow` | 4 | `^Warhead_Arrow_Light_Flat` · `AsianPhotonCannon` · `^Grenade` · `^ArrowWeapon` |
| `AsianNinjaStar` | 4 | `^Warhead_Melee_Medium` · `^Projectile_InstantHit` · `^Effect_Melee_Medium` · `^Effect_Bullet_Medium_RA2` |
| `AsianPelicanMissile` | 7 | `^Warhead_MissileAP_Heavy_Flat` · `^Warhead_Concussion_Light` · `^Warhead_MissileAP_Heavy` · `^Projectile_Missile_Heavy` |
| `AsianPhoenixRocket` | 6 | `^Warhead_Flame_Medium` · `^Warhead_Demolition_Light` · `^Projectile_Flame_Medium` · `^Effect_Flame_Medium` |
| `AsianPhotonCannon` | 5 | `^Warhead_Plasma_Heavy_Flat` · `^MediumMissile` · `^FlakWeapon` · `^TeslaWeapon` |
| `AsianPulverizerGatling` | 6 | `^Warhead_Bullet_Medium_Flat` · `^Warhead_Bullet_Light` · `^Warhead_CannonHE_Heavy` · `^Projectile_Shell_Heavy` |
| `AsianRailTank` | 4 | `^Warhead_Railgun_Heavy` · `^Projectile_Railgun_Heavy` · `^Effect_Railgun_Heavy` · `^Effect_Clsn_Medium_RA2` |
| `AsianRailgun` | 4 | `^Warhead_Railgun_Heavy` · `^Projectile_Railgun_Heavy` · `^Effect_Railgun_Heavy` · `^Effect_Explosion_Medium_RA2` |
| `AsianSinglePlasma` | 12 | `^Warhead_Plasma_Heavy_Flat` · `^Warhead_CannonHE_Medium` · `^Projectile_Shell_Medium` · `^Effect_CannonHE_Medium` |
| `AsianSmallTorpedo` | 4 | `^Warhead_MissileAP_Heavy_Flat` · `^RA2Grenade` · `^RA2HeavyMissile` · `^Effect_Watersplash_Large_RA2` |
| `AsianSniper` | 8 | `^Warhead_Bullet_Heavy` · `^Projectile_Shell_Heavy` · `^Effect_CannonHE_Heavy` · `^MediumMissile` |
| `AsianSniperLockdown` | 4 | `^Warhead_Tesla_Super` · `^Projectile_Lightning_Super` · `^Effect_Tesla_Super` · `AsianSniperAP` |
| `AsianSubmarineBomb` | 5 | `^Warhead_Demolition_Heavy_Flat` · `^Warhead_Demolition_Heavy` · `^Effect_Demolition_Heavy` · `^RA2Grenade` |
| `AthenaLaser` | 7 | `^Warhead_Laser_Heavy_Flat` · `^LightMissile` · `^SmallArms` · `^Chaingun` |
| `AtreusMG` | 8 | `^Warhead_Bullet_Medium_Flat` · `^Warhead_CannonHE_Heavy` · `^Projectile_Shell_Heavy` · `^Effect_CannonHE_Heavy` |
| `BCLaser` | 8 | `^Warhead_Laser_Heavy_Flat` · `^Warhead_CannonHE_Heavy` · `^Projectile_Shell_Heavy` · `^Effect_CannonHE_Heavy` |
| `BallistaMultiShot` | 5 | `^Warhead_Arrow_Medium` · `^Grenade` · `^LightFlameWeapon` · `^LightChemicalWeapon` |
| `BallistaMultiShotEnergized` | 5 | `^Warhead_Arrow_Medium` · `^TeslaWeapon` · `^MediumFlameWeapon` · `^MediumChemicalWeapon` |
| `BallistaSingleShotAirEnergized_AA` | 4 | `^Warhead_MissileAP_Light` · `^Projectile_Missile_Light` · `^Effect_MissileAP_Light` · `JapanMaidenBowEnergized` |
| `BehemothShoot` | 7 | `^Warhead_MissileHE_Heavy` · `^LightFlameWeapon` · `^MediumChemicalWeapon` · `^HeavyMissile` |
| `BigShieeTusk` | 5 | `^Warhead_MissileHE_Heavy_Flat` · `^Warhead_MissileHE_Heavy` · `^Warhead_Concussion_Medium` · `^Projectile_Missile_Heavy` |
| `BlackEagleMissiles` | 6 | `^Warhead_MissileAP_Medium_Flat` · `^Warhead_Demolition_Light` · `^Warhead_Demolition_Heavy` · `^Projectile_Grenade_Light` |
| `BlackEagleThunderboltMissiles` | 10 | `^Grenade` · `^ShrapnelWeapon` · `^HeavyBomb` · `^HeavyMissile` |
| `BuggyPlasmaGrenade` | 4 | `^Warhead_Plasma_Light` · `^HeavyBomb` · `^ShrapnelWeapon` · `BuggyGrenade` |


_... and 466 more._


## W2 — two or more `^Warhead_*` inherits (281 vs ratchet 177)

| weapon | warhead templates |
|---|---|
| `110mm_Gun` | `^Warhead_CannonAP_Light_Flat` · `^Warhead_CannonHE_Heavy` · `^Warhead_CannonAP_Light` |
| `12MissilesSpawnerScud` | `^Warhead_Demolition_Heavy` · `^Warhead_Flame_Medium` |
| `25mm` | `^Warhead_CannonHE_Medium_Flat` · `^Warhead_CannonHE_Medium` |
| `8Inch` | `^Warhead_Demolition_Heavy_Flat` · `^Warhead_Demolition_Heavy` |
| `APCGun` | `^Warhead_Flak_Medium_Flat` · `^Warhead_Bullet_Medium` · `^Warhead_Flak_Medium` |
| `ArtilleryShell` | `^Warhead_Concussion_Medium_Flat` · `^Warhead_Demolition_Light` · `^Warhead_Concussion_Medium` |
| `AsianGrenade` | `^Warhead_Concussion_Medium_Flat` · `^Warhead_Concussion_Medium` |
| `AsianHarbingerPlasma` | `^Warhead_Plasma_Medium_Flat` · `^Warhead_Plasma_Medium` · `^Warhead_CannonHE_Medium` |
| `AsianMLRS` | `^Warhead_MissileAP_Medium` · `^Warhead_MissileAA_Medium_Flat` |
| `AsianPelicanMissile` | `^Warhead_MissileAP_Heavy_Flat` · `^Warhead_Concussion_Light` · `^Warhead_MissileAP_Heavy` |
| `AsianPhoenixRocket` | `^Warhead_Flame_Medium` · `^Warhead_Demolition_Light` |
| `AsianPulverizerGatling` | `^Warhead_Bullet_Medium_Flat` · `^Warhead_Bullet_Light` · `^Warhead_CannonHE_Heavy` |
| `AsianPunisherAG` | `^Warhead_MissileHE_Medium` · `^Warhead_MissileAP_Medium_Flat` |
| `AsianSinglePlasma` | `^Warhead_Plasma_Heavy_Flat` · `^Warhead_CannonHE_Medium` |
| `AsianSubmarineBomb` | `^Warhead_Demolition_Heavy_Flat` · `^Warhead_Demolition_Heavy` |
| `AtreusMG` | `^Warhead_Bullet_Medium_Flat` · `^Warhead_CannonHE_Heavy` |
| `BCLaser` | `^Warhead_Laser_Heavy_Flat` · `^Warhead_CannonHE_Heavy` |
| `BigShieeTusk` | `^Warhead_MissileHE_Heavy_Flat` · `^Warhead_MissileHE_Heavy` · `^Warhead_Concussion_Medium` |
| `BlackEagleMissiles` | `^Warhead_MissileAP_Medium_Flat` · `^Warhead_Demolition_Light` · `^Warhead_Demolition_Heavy` |
| `CHGuardRifle` | `^Warhead_Bullet_Medium_Flat` · `^Warhead_Bullet_Light` · `^Warhead_Bullet_Medium` |
| `CabalCyborgChaingun` | `^Warhead_Bullet_Medium_Flat` · `^Warhead_Bullet_Light` · `^Warhead_Bullet_Medium` |
| `CabalHeavyReaperMissiles` | `^Warhead_MissileHE_Heavy_Flat` · `^Warhead_MissileHE_Medium` · `^Warhead_MissileHE_Heavy` · `^Warhead_Demolition_Light` · `^Warhead_Concussion_Medium` |
| `CabalHeavyReaperMissiles_AA` | `^Warhead_MissileAA_Heavy_Flat` · `^Warhead_MissileHE_Medium` · `^Warhead_MissileHE_Heavy` · `^Warhead_Demolition_Light` · `^Warhead_Concussion_Medium` |
| `CabalLegionGun` | `^Warhead_Laser_Heavy_Flat` · `^Warhead_Bullet_Light` · `^Warhead_Laser_Heavy` |
| `CabalMagicNuke` | `^Warhead_Tesla_Heavy` · `^Warhead_Tesla_Super` |
| `CabalMantisGun` | `^Warhead_Laser_Heavy_Flat` · `^Warhead_Bullet_Light` · `^Warhead_Laser_Heavy` |
| `CabalOverkillDroneLaser` | `^Warhead_Laser_Heavy_Flat` · `^Warhead_Bullet_Light` · `^Warhead_Laser_Heavy` |
| `CabalReaperMissiles` | `^Warhead_MissileHE_Medium_Flat` · `^Warhead_MissileHE_Light` · `^Warhead_MissileHE_Medium` · `^Warhead_Demolition_Light` · `^Warhead_Concussion_Medium` |
| `CabalReaperMissiles_AA` | `^Warhead_MissileAA_Medium_Flat` · `^Warhead_MissileHE_Light` · `^Warhead_MissileHE_Medium` · `^Warhead_Demolition_Light` · `^Warhead_Concussion_Medium` |
| `CabalRocketCyborgRockets` | `^Warhead_MissileAP_Medium` · `^Warhead_MissileHE_Medium_Flat` |
| `CabalRocketCyborgRocketsUpgraded` | `^Warhead_MissileAP_Medium` · `^Warhead_MissileHE_Medium_Flat` |
| `CorsairFlash` | `^Warhead_Flak_Medium_Flat` · `^Warhead_Flak_Medium` · `^Warhead_Demolition_Light` |
| `CycloneRockets` | `^Warhead_MissileAP_Light` · `^Warhead_MissileHE_Light_Flat` |
| `D2K_155mm3` | `^Warhead_Concussion_Medium_Flat` · `^Warhead_Demolition_Light` · `^Warhead_Demolition_Heavy` · `^Warhead_Concussion_Medium` |
| `D2K_155mm_turret` | `^Warhead_CannonHE_Medium_Flat` · `^Warhead_Demolition_Light` · `^Warhead_Demolition_Heavy` · `^Warhead_Concussion_Medium` |
| `D2K_APC_Rocket` | `^Warhead_MissileAP_Medium_Flat` · `^Warhead_MissileAP_Light` · `^Warhead_MissileAP_Medium` |
| `D2K_Rocket_Trooper` | `^Warhead_MissileAP_Heavy_Flat` · `^Warhead_MissileAP_Light` · `^Warhead_MissileAP_Medium` · `^Warhead_MissileAP_Heavy` |
| `D2K_Rocket_Trooper1` | `^Warhead_Flak_Medium` · `^Warhead_MissileAP_Light` · `^Warhead_MissileAP_Heavy` · `^Warhead_MissileHE_Heavy` |
| `D2K_Rocket_Trooper2` | `^Warhead_Demolition_Light` · `^Warhead_Railgun_Heavy` · `^Warhead_CannonHE_Medium` · `^Warhead_MissileHE_Heavy` |
| `DalekCannon` | `^Warhead_Railgun_Heavy_Flat` · `^Warhead_Tesla_Heavy` · `^Warhead_Laser_Heavy` |


_... and 241 more._


## W3 — two or more `^Projectile_*` inherits (12 vs ratchet 12)

| weapon | projectile templates |
|---|---|
| `110mm_Gun` | `^Projectile_Shell_Heavy` · `^Projectile_Shell_Light` |
| `Flamethrower` | `^Projectile_Flame_Light` · `^Projectile_Flame_Light` |
| `HeavyIxianCombatTankCannon` | `^Projectile_Shell_Heavy` · `^Projectile_Shell_Light` |
| `IxianCombatTankCannon` | `^Projectile_Shell_Heavy` · `^Projectile_Shell_Light` |
| `LatinMonkeyGrenade1` | `^Projectile_Grenade_Light` · `^Projectile_Shell_Heavy` |
| `RashidanGun_upgrade` | `^Projectile_Shell_Heavy` · `^Projectile_Missile_Heavy` |
| `ReaperGrenade` | `^Projectile_Grenade_Light` · `^Projectile_Shell_Heavy` |
| `TS70mmTur` | `^Projectile_Shell_Medium` · `^Projectile_Shell_Light` |
| `YakovlevCannon` | `^Projectile_Shell_Heavy` · `^Projectile_Shell_Light` |
| `YakovlevCannon_elite` | `^Projectile_Shell_Heavy` · `^Projectile_Shell_Light` |
| `ra120mmThermobaric` | `^Projectile_Shell_Heavy` · `^Projectile_Flame_Heavy` |
| `ra1_soviets_siegemammothtank_ra120mm2thermobaric` | `^Projectile_Shell_Heavy` · `^Projectile_Flame_Heavy` |


## W4 — two or more `^Effect_*` inherits (50 vs ratchet 51)

| weapon | effect templates |
|---|---|
| `110mm_Gun` | `^Effect_CannonHE_Heavy` · `^Effect_CannonAP_Light` |
| `12MissilesSpawnerScud` | `^Effect_Flame_Medium` · `^Effect_Demolition_Heavy` |
| `AsianHarbingerPlasma` | `^Effect_CannonHE_Medium` · `^Effect_Apoc_Explosion_RA2` |
| `AsianNinjaStar` | `^Effect_Melee_Medium` · `^Effect_Bullet_Medium_RA2` |
| `AsianPelicanMissile` | `^Effect_MissileAP_Heavy` · `^Effect_Grey_Explosion_Small_RA2` |
| `AsianPhoenixRocket` | `^Effect_Flame_Medium` · `^Effect_Explosion_Large_RA2` |
| `AsianRailTank` | `^Effect_Railgun_Heavy` · `^Effect_Clsn_Medium_RA2` |
| `AsianRailgun` | `^Effect_Railgun_Heavy` · `^Effect_Explosion_Medium_RA2` |
| `AsianSinglePlasma` | `^Effect_CannonHE_Medium` · `^Effect_Apoc_Explosion_RA2` |
| `AsianSubmarineBomb` | `^Effect_Demolition_Heavy` · `^Effect_Twlt_Large_RA2` |
| `Flamethrower` | `^Effect_Flame_Light` · `^Effect_Flame_Light` |
| `HeavyIxianCombatTankCannon` | `^Effect_CannonHE_Heavy` · `^Effect_CannonAP_Light` |
| `IxianCombatTankCannon` | `^Effect_CannonHE_Heavy` · `^Effect_CannonAP_Light` |
| `JapanesePlasmaBomb` | `^Effect_Flame_Heavy` · `^Effect_Demolition_Heavy` |
| `LatinMonkeyGrenade1` | `^Effect_Concussion_Medium` · `^Effect_CannonHE_Heavy` |
| `LunarNaxiJadgDestroyer` | `^Effect_CannonHE_Heavy` · `^Effect_Concussion_Medium` |
| `MissileAttackRobotGun` | `^Effect_MissileAP_Medium` · `^Effect_Grey_Explosion_Small_RA2` |
| `NaxBrummbarArty` | `^Effect_Concussion_Medium` · `^Effect_CannonHE_Heavy` |
| `NaxGrilleArty` | `^Effect_CannonHE_Heavy` · `^Effect_Concussion_Medium` |
| `NaxiCowDrop` | `^Effect_Demolition_Heavy` · `^Effect_Clsn_Medium_RA2` |
| `NaxiJadgDestroyer` | `^Effect_CannonHE_Heavy` · `^Effect_Concussion_Medium` |
| `OrionRailgun` | `^Effect_Railgun_Heavy` · `^Effect_Explosion_Large_RA2` |
| `RA2FreedomAK47` | `^Effect_CannonHE_Heavy` · `^Effect_Bullet_Light_RA2` |
| `RA2GrandCannonWeapon` | `^Effect_CannonHE_Heavy` · `^Effect_Clsn_Medium_RA2` |
| `RA2MortarBike` | `^Effect_CannonHE_Heavy` · `^Effect_Explosion_Large_RA2` |
| `RashidanGun_upgrade` | `^Effect_CannonHE_Heavy` · `^Effect_MissileHE_Heavy` |
| `ReaperGrenade` | `^Effect_Concussion_Medium` · `^Effect_CannonHE_Heavy` |
| `TS70mmTur` | `^Effect_CannonHE_Medium` · `^Effect_CannonAP_Light` |
| `TSGrenade` | `^Effect_CannonHE_Medium` · `^Effect_Concussion_Medium` |
| `TSScoopDualTur` | `^Effect_CannonHE_Heavy` · `^Effect_Concussion_Medium` |
| `YakovlevCannon` | `^Effect_CannonHE_Heavy` · `^Effect_CannonAP_Light` |
| `YakovlevCannon_elite` | `^Effect_CannonHE_Heavy` · `^Effect_CannonAP_Light` |
| `bigshieemortar` | `^Effect_Flame_Medium` · `^Effect_Explosion_Large_RA2` |
| `ra120mmThermobaric` | `^Effect_CannonHE_Heavy` · `^Effect_Flame_Heavy` |
| `ra1_soviets_kotinnucleartank_kotincannonnuclearshell` | `^Effect_CannonHE_Heavy` · `^Effect_Nuclear_Super` |
| `ra1_soviets_monstertank_120mm_cannon` | `^Effect_CannonHE_Heavy` · `^Effect_Nuclear_Super` |
| `ra1_soviets_monstertank_120mm_cannon_inferno` | `^Effect_CannonHE_Heavy` · `^Effect_Flame_Heavy` |
| `ra1_soviets_siegemammothtank_ra120mm2thermobaric` | `^Effect_CannonHE_Heavy` · `^Effect_Flame_Heavy` |
| `ra1_soviets_submarine_torpedo_thermobaric` | `^Effect_Nuclear_Super` · `^Effect_MissileAP_Heavy` |
| `ra1_soviets_v2rocketlauncher_scudtesla` | `^Effect_Tesla_Heavy` · `^Effect_Kirov_Tesla_RA2` |


_... and 10 more._


## W5 — more than one resolved MAIN warhead (167 vs ratchet 389)

| weapon | mains | which |
|---|---|---|
| `12MissilesSpawnerScud` | 4 | `Demolition_Heavy` · `Demolition_Light` · `Flame_Medium` · `MissileAP_Heavy` |
| `AsianTSIonCannon` | 4 | `IonCannon` · `TeslaChargedWeapon` · `TeslaWeapon` · `Tesla_Super` |
| `Atomic` | 2 | `Nuclear_Super` · `Tesla_Super` |
| `BCLaser` | 2 | `CannonHE_Heavy` · `Laser_Heavy_Flat` |
| `BroodweaverLeech` | 2 | `ExtraHealing` · `HealingWeapon` |
| `CHFlameBlue` | 2 | `1Dam` · `Flame_Medium` |
| `CabalEngineerRepairBeam` | 2 | `ExtraRepair` · `RepairWeapon` |
| `CabalMagicNuke` | 8 | `10Dam_areanuke3` · `11Dam_areanuke3` · `1Dam_impact` · `4Dam_areanuke1` |
| `ChemTibAtomic` | 2 | `Nuclear_Super` · `Tesla_Super` |
| `Combat_Tank_F_Sound` | 2 | `1Dam` · `2Dam` |
| `CrateNuke` | 3 | `1Dam_impact` · `4Dam_areanuke1` · `TREEKILL` |
| `D2KRepair` | 3 | `1Dam` · `ExtraHealing` · `HealingWeapon` |
| `D2K_SiegeQuad` | 4 | `CannonHE_Medium` · `Concussion_Medium` · `Demolition_Heavy` · `Demolition_Light` |
| `DRPlasmaTankWeapon` | 2 | `1Dam` · `1DamBuildings` |
| `DTAtomic` | 2 | `Nuclear_Super` · `Tesla_Super` |
| `DeathHandCluster` | 3 | `1Dam` · `Demolition_Light` · `Flame_Light` |
| `DreadshroudSpore` | 2 | `Chemical_Medium` · `LaserExtraDamage_Auxiliary` |
| `DredMissile` | 3 | `Demolition_Light` · `MissileAP_Heavy` · `RA2SCUDMissileAP_Heavy_NoWall` |
| `ExecutionerDeath` | 7 | `10Dam_areanuke3` · `11Dam_areanuke3` · `1Dam_impact` · `4Dam_areanuke1` |
| `ExplosiveDebris` | 2 | `Demolition_Light` · `Flame_Light` |
| `FutureEnforcerShotgun` | 3 | `CannonHE_Medium` · `ShotgunGrenadeAlly` · `ShotgunShrapnelAlly` |
| `FutureEnforcerShotgunDeployed` | 3 | `CannonHE_Medium` · `ShotgunGrenadeAlly` · `ShotgunShrapnelAlly` |
| `FutureEnforcerShotgunDeployed_elite` | 3 | `CannonHE_Medium` · `ShotgunGrenadeAlly` · `ShotgunShrapnelAlly` |
| `FutureEnforcerShotgun_elite` | 3 | `CannonHE_Medium` · `ShotgunGrenadeAlly` · `ShotgunShrapnelAlly` |
| `Future_Cryocopter_Rocket` | 2 | `FutureCryocopterMissileAP_MediumFriendly` · `MissileAP_Medium_Flat` |
| `GDIRigDroneRepair` | 2 | `ExtraRepair` · `RepairWeapon` |
| `GDIRigDroneTargeting` | 2 | `ExtraRepair` · `RepairWeapon` |
| `GDIRigDroneTargetingTower` | 2 | `ExtraRepair` · `RepairWeapon` |
| `GLASCUD` | 2 | `1Dam` · `MissileHE_Heavy` |
| `GLASCUDPOWER` | 2 | `1Dam` · `MissileHE_Heavy` |
| `GLASCUDPOWER2` | 2 | `1Dam` · `MissileHE_Heavy` |
| `GLASCUDPOWER3` | 2 | `1Dam` · `MissileHE_Heavy` |
| `GLBarrelExplode` | 2 | `1Dam` · `Demolition_Heavy_Flat` |
| `GLBombTruckToxExplosive` | 3 | `1Dam` · `Concussion_Medium` · `Demolition_Heavy` |
| `GLBombTruckToxExplosive2` | 3 | `1Dam` · `Concussion_Medium` · `Demolition_Heavy` |
| `GLDemolitionExplode` | 3 | `1Dam` · `Concussion_Medium` · `Demolition_Heavy` |
| `GLRebelToxin` | 2 | `1Dam` · `Clear` |
| `GLRebelToxinGarrison` | 2 | `1Dam` · `Clear` |
| `GLTerroristExplosive` | 3 | `1Dam` · `Concussion_Medium` · `Demolition_Heavy` |
| `GLTerroristExplosive2` | 3 | `1Dam` · `Concussion_Medium` · `Demolition_Heavy` |


_... and 127 more._


## W6 — effect warheads declared LOCALLY (691 vs ratchet 692)

| weapon | nodes | first three |
|---|---|---|
| `105mm` | 1 | `Warhead@Effect: CreateEffect` |
| `120mm` | 1 | `Warhead@Effect: CreateEffect` |
| `12MissilesSpawnerScud` | 1 | `Warhead@Effect: CreateEffect` |
| `155mm` | 1 | `Warhead@Effect: CreateEffect` |
| `155mmCryo` | 1 | `Warhead@Effect: CreateEffect` |
| `2100Tanktrap` | 1 | `Warhead@Smu: LeaveSmudge` |
| `227mm` | 2 | `Warhead@Effect: CreateEffect` · `Warhead@EffectWater: CreateEffect` |
| `25mm` | 2 | `Warhead@Effect: CreateEffect` · `Warhead@EffectAir: CreateEffect` |
| `A10CarrierMissiles_AA` | 1 | `Warhead@EffectAir: CreateEffect` |
| `AAGunBoatFlak` | 1 | `Warhead@EffectAir: CreateEffect` |
| `ASDFKamikazeExplosion` | 1 | `Warhead@Effect: CreateEffect` |
| `ATMine` | 3 | `Warhead@Effect: CreateEffect` · `Warhead@Smudge: LeaveSmudge` · `Warhead@Concrete: DamagesConcrete` |
| `ArmoredCarMG` | 1 | `Warhead@Effect: CreateEffect` |
| `ArtilleryExplode` | 1 | `Warhead@2Eff: CreateEffect` |
| `ArtilleryShell` | 1 | `Warhead@Effect: CreateEffect` |
| `AsianChaosSuperweapon` | 1 | `Warhead@1: CreateEffect` |
| `AsianChaosTurret` | 1 | `Warhead@Effect: CreateEffect` |
| `AsianChemical` | 1 | `Warhead@Effect: CreateEffect` |
| `AsianChemicalBombs` | 1 | `Warhead@Effect: CreateEffect` |
| `AsianHarbingerPlasma` | 1 | `Warhead@Effect: CreateEffect` |
| `AsianIonBeamMini` | 1 | `Warhead@Effect: CreateEffect` |
| `AsianMaidenBow` | 2 | `Warhead@Effect: CreateEffect` · `Warhead@EffectAir: CreateEffect` |
| `AsianOilBombFragments` | 1 | `Warhead@Effect: CreateEffect` |
| `AsianPhotonCannon` | 2 | `Warhead@Effect: CreateEffect` · `Warhead@Smudge: LeaveSmudge` |
| `AsianSmallOilBomb` | 1 | `Warhead@Effect: CreateEffect` |
| `AsianSmallTorpedo` | 1 | `Warhead@Effect: CreateEffect` |
| `AsianSniper` | 1 | `Warhead@Effect: CreateEffect` |
| `AsianSniperAP` | 1 | `Warhead@Effect: CreateEffect` |
| `AsianSubmarineBomb` | 1 | `Warhead@Effect: CreateEffect` |
| `AsianTSIonCannon` | 2 | `Warhead@3Smu_area: LeaveSmudge` · `Warhead@Effect: CreateEffect` |
| `AsianTurretPlasma` | 1 | `Warhead@Effect: CreateEffect` |
| `AthenaLaser` | 9 | `Warhead@Effect: CreateEffect` · `Warhead@Effect2: CreateEffect` · `Warhead@Effect3: CreateEffect` |
| `AtreusMG` | 1 | `Warhead@Effect: CreateEffect` |
| `BCYamatoCannon` | 1 | `Warhead@Effect: CreateEffect` |
| `BHBombs` | 1 | `Warhead@3Eff: CreateEffect` |
| `BallistaMultiShot` | 1 | `Warhead@Effect: CreateEffect` |
| `BallistaMultiShotEnergized` | 1 | `Warhead@Effect: CreateEffect` |
| `BarrelExplode` | 2 | `Warhead@2Eff: CreateEffect` · `Warhead@Smu: LeaveSmudge` |
| `BehemothShoot` | 3 | `Warhead@Effect: CreateEffect` · `Warhead@Effect2: CreateEffect` · `Warhead@EffectAir: CreateEffect` |
| `BigChemSpray` | 1 | `Warhead@3Eff: CreateEffect` |


_... and 651 more._


**FAIL — W2, W7 rose above baseline.** A weapon was given a second warhead, projectile or effect. The law allows exactly three inherits and one main.
