# Weapon shape — the ONE-WARHEAD / THREE-INHERIT law

**Maintainer ruling, 2026-09-06.** Every concrete weapon ends with exactly three inherits — `^Warhead_*`, `^Projectile_*`, `^Effect_*` — one main warhead, and no effect warheads of its own. Mechanic warheads (`FireShrapnel`, `GrantExternalCondition`) and the `*Percentage` / `*FriendlyFire` / `*ExtraDamage` halves of one main are NOT violations.

⛔ This **repeals the exemption** in `tools/audit/intentional_composites.py`. Its 224 entries are no longer 'reviewed, keep' — they are the worklist. The registry data stays useful: it says which mains someone chose on purpose.

concrete weapons with inherits: **2152**

W5 counts structural flat-damage nodes, including zero/healing/ally-only nodes; the split audit counts positive non-companion damage. Both resolve the full concrete weapon corpus. Use `--compare-split` for exact differences.

| check | what | count | ratchet |
|---|---|--:|--:|
| W1 | more than 3 inherits | **286** (13.29% of 2152) | 26.16% |
| W2 | two or more `^Warhead_*` inherits | **122** | 122 |
| W3 | two or more `^Projectile_*` inherits | **7** | 7 |
| W4 | two or more `^Effect_*` inherits | **41** | 41 |
| W5 | more than one resolved MAIN warhead | **167** | 389 |
| W6 | effect warheads declared LOCALLY | **466** | 466 |
| W7 | inherits from ANOTHER WEAPON, not a template | **869** | 869 |
| W8 | inherits a `^Template` that is not one of the three kinds | **360** | 360 |

| I7 informational — missing template | weapons |
|---|--:|
| no `^Effect_*` inherit | 898 |
| no `^Projectile_*` inherit | 1261 |
| no `^Warhead_*` inherit | 933 |

_I7 is a REVIEW QUEUE, not a defect count — an instant or utility weapon may legitimately have no projectile. Do not ratchet it without a per-weapon pass._


## W7 — inherits from ANOTHER WEAPON, not a template (869 vs ratchet 869)

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
| `AsianPhoenixRocket_elite` | 1 | `AsianPhoenixRocket` |
| `AsianPhotonCannon_EMP` | 1 | `AsianPhotonCannon` |
| `AsianPulverizerMechaGatling` | 1 | `AsianPulverizerGatling` |
| `AsianPunisherAG` | 1 | `AsianPhotonCannon` |
| `AsianPunisherAG_EMP` | 1 | `AsianPunisherAG` |


_... and 829 more._


## W8 — inherits a `^Template` that is not one of the three kinds (360 vs ratchet 360)

| weapon | legacy templates | first four |
|---|---|---|
| `110mm_Gun` | 1 | `^D2K_Cannon` |
| `120mm_cobra` | 1 | `^D2K_Cannon` |
| `120mm_td` | 1 | `^D2K_Cannon` |
| `80mm_A` | 1 | `^D2K_Cannon` |
| `80mm_H` | 1 | `^D2K_Cannon` |
| `AnthraxCloud` | 1 | `^ToxicWeapon` |
| `AsianPhotonCannon` | 1 | `^TeslaWeapon` |
| `AthenaLaser` | 2 | `^TeslaWeapon` · `^LaserWeapon` |
| `Atomic` | 1 | `^AtomicCore` |
| `AtreusMG` | 4 | `^Grenade` · `^MediumMissile` · `^FlakWeapon` · `^RA2Chaingun` |
| `BCLaser` | 4 | `^NuclearWarhead` · `^RailgunWeapon` · `^HeavyBomb` · `^LaserWeapon` |
| `BHBombs` | 1 | `^FlameWeapon` |
| `BallistaMultiShotEnergized` | 1 | `^TeslaWeapon` |
| `BehemothShoot` | 6 | `^LightFlameWeapon` · `^MediumChemicalWeapon` · `^HeavyMissile` · `^Grenade` |
| `BigChemSpray` | 1 | `^FlameWeapon` |
| `BoatMissile` | 1 | `^MissileWeapon` |
| `BroodweaverLeech` | 1 | `^HealingWeapon` |
| `BuildingExplode` | 1 | `^DamagingExplosionHE` |
| `CabalArtilleryWalkerShellUpgraded` | 2 | `^TeslaChargedWeapon` · `^RailgunWeapon` |
| `CabalAscendedRockets` | 1 | `^CabalMissileMedium` |
| `CabalBeholderLaser` | 3 | `^TeslaWeapon` · `^RailgunWeapon` · `^LaserWeapon` |
| `CabalCommandoPlasma` | 3 | `^HeavyCannon` · `^MediumFlameWeapon` · `^MediumChemicalWeapon` |
| `CabalCommandoPlasmaMk2` | 3 | `^HeavyCannon` · `^MediumFlameWeapon` · `^MediumChemicalWeapon` |
| `CabalCommandoPlasmaMk2Neutron` | 5 | `^HeavyCannon` · `^MediumFlameWeapon` · `^TeslaWeapon` · `^MagicWeapon` |
| `CabalCommandoPlasmaNeutron` | 5 | `^HeavyCannon` · `^MediumFlameWeapon` · `^TeslaWeapon` · `^MagicWeapon` |
| `CabalEngineerRepairBeam` | 1 | `^RepairWeapon` |
| `CabalHeavyReaperMissiles` | 1 | `^CabalMissileLight` |
| `CabalHeavyReaperMissiles_AA` | 1 | `^CabalMissileLight` |
| `CabalHunterKillerLasers` | 1 | `^LaserWeapon` |
| `CabalHunterKillerLasers_elite` | 2 | `^LaserWeapon` · `^RailgunWeapon` |
| `CabalManticoreMissiles_AA` | 1 | `^CabalManticoreMissiles` |
| `CabalMothershipRockets` | 4 | `^TeslaWeapon` · `^HeavyMissile` · `^MediumFlameWeapon` · `^Grenade` |
| `CabalReaperMissiles` | 1 | `^CabalMissileLight` |
| `CabalReaperMissiles_AA` | 1 | `^CabalMissileLight` |
| `CabalRocketCyborgRockets` | 1 | `^CabalMissileLight` |
| `CabalRocketCyborgRocketsUpgraded` | 1 | `^CabalMissileLight` |
| `CabalSubmarinePlasma` | 3 | `^HeavyCannon` · `^MediumFlameWeapon` · `^MediumChemicalWeapon` |
| `Claw` | 1 | `^DinoWeapon` |
| `ConsortiumMissileSystem` | 1 | `^TeslaWeapon` |
| `CryoLegionnaireAttack` | 2 | `^TeslaWeapon` · `^LaserWeapon` |


_... and 320 more._


## W1 — more than 3 inherits (286 vs ratchet 576)

| weapon | inherits | first four |
|---|---|---|
| `110mm_Gun` | 8 | `^Warhead_CannonAP_Light_Flat` · `^Warhead_CannonHE_Heavy` · `^Projectile_Shell_Heavy` · `^Effect_CannonHE_Heavy` |
| `120mm_cobra` | 4 | `^Warhead_CannonAP` · `^Projectile_Shell_Light` · `^Effect_CannonAP_Light` · `^D2K_Cannon` |
| `25mmWaveforce` | 4 | `^Warhead_Railgun_Heavy` · `^Projectile_Railgun_Heavy` · `^Effect_Railgun_Heavy` · `25mm` |
| `APCGun` | 5 | `^Warhead_Flak_Medium_Flat` · `^Warhead_Bullet_Medium` · `^Warhead_Flak_Medium` · `^Projectile_Flak_Medium` |
| `ASDFGun2` | 4 | `^Warhead_Railgun_Heavy` · `^Projectile_Railgun_Heavy` · `^Effect_Railgun_Heavy` · `ASDFGun` |
| `ArmoredCarMGAAWaveforce` | 4 | `^Warhead_Railgun_Heavy` · `^Projectile_Railgun_Heavy` · `^Effect_AlliedTigerCannon` · `ArmoredCarMG_AA` |
| `ArmoredCarMGWaveforce` | 4 | `^Warhead_Railgun_Heavy` · `^Projectile_Railgun_Heavy` · `^Effect_AlliedTigerCannon` · `ArmoredCarMG` |
| `ArtilleryShell` | 5 | `^Warhead_Concussion_Medium_Flat` · `^Warhead_Demolition_Light` · `^Warhead_Concussion_Medium` · `^Projectile_Grenade_Light` |
| `AsianChaosMine` | 5 | `AsianChaosTurret` · `AsianTankMine` · `^Warhead_Chemical_Heavy` · `^Projectile_Chem_Heavy` |
| `AsianLynxTankCannon` | 4 | `^Warhead_CannonHE_Medium` · `^Projectile_Shell_Medium` · `^Effect_AlliedTigerCannon` · `^Effect_Flame_Heavy` |
| `AsianSinglePlasma` | 4 | `^Warhead_CannonHE_Heavy` · `^Projectile_Shell_Heavy` · `^Effect_Apoc_AP_RA2` · `^Effect_Flame_Heavy` |
| `AsianSniperLockdown` | 4 | `^Warhead_Tesla_Super` · `^Projectile_Lightning_Super` · `^Effect_Tesla_Super` · `AsianSniperAP` |
| `AtreusMG` | 8 | `^Warhead_Bullet_Medium_Flat` · `^Warhead_CannonHE_Heavy` · `^Projectile_Shell_Heavy` · `^Effect_CannonHE_Heavy` |
| `BCLaser` | 8 | `^Warhead_Laser_Heavy_Flat` · `^Warhead_CannonHE_Heavy` · `^Projectile_Shell_Heavy` · `^Effect_CannonHE_Heavy` |
| `BallistaSingleShotAirEnergized_AA` | 4 | `^Warhead_MissileAP_Light` · `^Projectile_Missile_Light` · `^Effect_MissileAP_Light` · `JapanMaidenBowEnergized` |
| `BehemothShoot` | 7 | `^Warhead_MissileHE_Heavy` · `^LightFlameWeapon` · `^MediumChemicalWeapon` · `^HeavyMissile` |
| `CabalArtilleryWalkerShellUpgraded` | 4 | `^Warhead_CannonHE_Heavy_Flat` · `^TeslaChargedWeapon` · `^RailgunWeapon` · `^ts_cabal_cabalartillerywalkershellupgraded` |
| `CabalBeholderLaser` | 5 | `^Warhead_Laser_Heavy_Flat` · `^TeslaWeapon` · `^RailgunWeapon` · `^LaserWeapon` |
| `CabalCommandoPlasma` | 5 | `^Warhead_Plasma_Heavy` · `^HeavyCannon` · `^MediumFlameWeapon` · `^MediumChemicalWeapon` |
| `CabalCommandoPlasmaMk2` | 5 | `^Warhead_Plasma_Heavy` · `^HeavyCannon` · `^MediumFlameWeapon` · `^MediumChemicalWeapon` |
| `CabalCommandoPlasmaMk2Neutron` | 7 | `^Warhead_Plasma_Heavy_Flat` · `^HeavyCannon` · `^MediumFlameWeapon` · `^TeslaWeapon` |
| `CabalCommandoPlasmaNeutron` | 7 | `^Warhead_Plasma_Heavy_Flat` · `^HeavyCannon` · `^MediumFlameWeapon` · `^TeslaWeapon` |
| `CabalCyborgChaingun` | 5 | `^Warhead_Bullet_Medium_Flat` · `^Warhead_Bullet_Light` · `^Warhead_Bullet_Medium` · `^Projectile_Bullet_Medium` |
| `CabalHeavyReaperMissiles` | 7 | `^Warhead_MissileHE_Heavy_Flat` · `^Warhead_MissileHE_Medium` · `^Warhead_MissileHE_Heavy` · `^Warhead_Demolition_Light` |
| `CabalHeavyReaperMissiles_AA` | 7 | `^Warhead_MissileAA_Heavy_Flat` · `^Warhead_MissileHE_Medium` · `^Warhead_MissileHE_Heavy` · `^Warhead_Demolition_Light` |
| `CabalHunterKillerLasers_elite` | 4 | `^Warhead_Laser_Heavy` · `^LaserWeapon` · `^RailgunWeapon` · `^ts_cabal_cabalhunterkillerlasers_elite` |
| `CabalLegionGun` | 5 | `^Warhead_Laser_Heavy_Flat` · `^Warhead_Bullet_Light` · `^Warhead_Laser_Heavy` · `^Projectile_Bullet_Light` |
| `CabalMagicNuke` | 4 | `^Warhead_Tesla_Heavy` · `^Warhead_Tesla_Super` · `^Projectile_Lightning_Super` · `^ts_cabal_cabalmagicnuke` |
| `CabalMantisGun` | 5 | `^Warhead_Laser_Heavy_Flat` · `^Warhead_Bullet_Light` · `^Warhead_Laser_Heavy` · `^Projectile_Bullet_Light` |
| `CabalMothershipRockets` | 6 | `^Warhead_MissileTesla_Heavy` · `^TeslaWeapon` · `^HeavyMissile` · `^MediumFlameWeapon` |
| `CabalOverkillDroneLaser` | 5 | `^Warhead_Laser_Heavy_Flat` · `^Warhead_Bullet_Light` · `^Warhead_Laser_Heavy` · `^Projectile_Bullet_Light` |
| `CabalReaperMissiles` | 7 | `^Warhead_MissileHE_Medium_Flat` · `^Warhead_MissileHE_Light` · `^Warhead_MissileHE_Medium` · `^Warhead_Demolition_Light` |
| `CabalReaperMissiles_AA` | 7 | `^Warhead_MissileAA_Medium_Flat` · `^Warhead_MissileHE_Light` · `^Warhead_MissileHE_Medium` · `^Warhead_Demolition_Light` |
| `CabalRocketCyborgRockets` | 4 | `^Warhead_MissileAP_Medium` · `^Warhead_MissileHE_Medium_Flat` · `^CabalMissileLight` · `^CabalMissileEffect` |
| `CabalRocketCyborgRocketsUpgraded` | 4 | `^Warhead_MissileAP_Medium` · `^Warhead_MissileHE_Medium_Flat` · `^CabalMissileLight` · `^CabalMissileEffect` |
| `CabalSubmarinePlasma` | 5 | `^Warhead_Plasma_Heavy` · `^HeavyCannon` · `^MediumFlameWeapon` · `^MediumChemicalWeapon` |
| `ConsortiumMissileSystem` | 4 | `^TeslaWeapon` · `^Warhead_MissileAP_Medium` · `^Projectile_Missile_Medium` · `^Effect_Clsn_Medium_RA2` |
| `CorsairFlash` | 5 | `^Warhead_Flak_Medium_Flat` · `^Warhead_Flak_Medium` · `^Warhead_Demolition_Light` · `^Projectile_Flak_Medium` |
| `CycloneRockets` | 4 | `^Warhead_MissileAP_Light` · `^Warhead_MissileHE_Light_Flat` · `^Projectile_Missile_Light` · `^Effect_MissileHE_Light` |
| `D2K_155mm2` | 6 | `^Warhead_CannonHE_Heavy` · `^MediumFlameWeapon` · `^ShrapnelWeapon` · `^HeavyBomb` |


_... and 246 more._


## W2 — two or more `^Warhead_*` inherits (122 vs ratchet 122)

| weapon | warhead templates |
|---|---|
| `110mm_Gun` | `^Warhead_CannonAP_Light_Flat` · `^Warhead_CannonHE_Heavy` · `^Warhead_CannonAP_Light` |
| `APCGun` | `^Warhead_Flak_Medium_Flat` · `^Warhead_Bullet_Medium` · `^Warhead_Flak_Medium` |
| `ArtilleryShell` | `^Warhead_Concussion_Medium_Flat` · `^Warhead_Demolition_Light` · `^Warhead_Concussion_Medium` |
| `AtreusMG` | `^Warhead_Bullet_Medium_Flat` · `^Warhead_CannonHE_Heavy` |
| `BCLaser` | `^Warhead_Laser_Heavy_Flat` · `^Warhead_CannonHE_Heavy` |
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
| `DeviatorMissile` | `^Warhead_MissileAP_Heavy_Flat` · `^Warhead_CannonHE_Heavy` |
| `DreadshroudSpore` | `^Warhead_Chemical_Medium` · `^Warhead_Laser_ExtraDamage` |
| `Dune_SiegeMortar` | `^Warhead_Concussion_Medium_Flat` · `^Warhead_Demolition_Light` · `^Warhead_Concussion_Medium` · `^Warhead_CannonAP_Light` |
| `EpigraphMG` | `^Warhead_Bullet_Medium_Flat` · `^Warhead_CannonHE_Heavy` |
| `Flamethrower` | `^Warhead_Flame_Light` · `^Warhead_Flame_Light` |
| `GlaveCanon` | `^Warhead_Railgun_Heavy_Flat` · `^Warhead_Demolition_Light` · `^Warhead_Railgun_Heavy` |
| `GoliathMG` | `^Warhead_Concussion_Light` · `^Warhead_CannonHE_Heavy` |
| `GoliathMk2MG` | `^Warhead_Bullet_Medium_Flat` · `^Warhead_CannonHE_Heavy` |
| `GuardianShoot` | `^Warhead_Concussion_Medium_Flat` · `^Warhead_Concussion_Medium` · `^Warhead_Concussion_Light` |
| `HMG_Duelist_upgrade` | `^Warhead_Bullet_Medium_Flat` · `^Warhead_Laser_ExtraDamage` · `^Warhead_CannonHE_Heavy` |
| `HMG_turret` | `^Warhead_Bullet_Medium_Flat` · `^Warhead_Bullet_Light` · `^Warhead_Bullet_Medium` |
| `HMGo_upgrade` | `^Warhead_Laser_Heavy` · `^Warhead_Bullet_Medium` |
| `HMGstealth` | `^Warhead_Bullet_Medium_Flat` · `^Warhead_Bullet_Light` · `^Warhead_Bullet_Medium` |
| `InfestedExplosion` | `^Warhead_Demolition_Heavy_Flat` · `^Warhead_Demolition_Light` · `^Warhead_Demolition_Heavy` · `^Warhead_Concussion_Medium` |
| `IonCannon` | `^Warhead_Tesla_Heavy` · `^Warhead_Tesla_Super` |
| `IxRailgunDroneBullet` | `^Warhead_Railgun_Heavy_Flat` · `^Warhead_Railgun_Heavy` · `^Warhead_Railgun_ExtraDamage` |


_... and 82 more._


## W3 — two or more `^Projectile_*` inherits (7 vs ratchet 7)

| weapon | projectile templates |
|---|---|
| `110mm_Gun` | `^Projectile_Shell_Heavy` · `^Projectile_Shell_Light` |
| `Flamethrower` | `^Projectile_Flame_Light` · `^Projectile_Flame_Light` |
| `HeavyIxianCombatTankCannon` | `^Projectile_Shell_Heavy` · `^Projectile_Shell_Light` |
| `IxianCombatTankCannon` | `^Projectile_Shell_Heavy` · `^Projectile_Shell_Light` |
| `RashidanGun_upgrade` | `^Projectile_Shell_Heavy` · `^Projectile_Missile_Heavy` |
| `ReaperGrenade` | `^Projectile_Grenade_Light` · `^Projectile_Shell_Heavy` |
| `TS70mmTur` | `^Projectile_Shell_Medium` · `^Projectile_Shell_Light` |


## W4 — two or more `^Effect_*` inherits (41 vs ratchet 41)

| weapon | effect templates |
|---|---|
| `110mm_Gun` | `^Effect_CannonHE_Heavy` · `^Effect_CannonAP_Light` |
| `AsianLynxTankCannon` | `^Effect_AlliedTigerCannon` · `^Effect_Flame_Heavy` |
| `AsianSinglePlasma` | `^Effect_Apoc_AP_RA2` · `^Effect_Flame_Heavy` |
| `AsianTwinPlasma` | `^Effect_Apoc_AP_RA2` · `^Effect_Flame_Heavy` |
| `AsianTwinPlasma_elite` | `^Effect_Apoc_AP_RA2` · `^Effect_Flame_Heavy` |
| `Flamethrower` | `^Effect_Flame_Light` · `^Effect_Flame_Light` |
| `HeavyIxianCombatTankCannon` | `^Effect_CannonHE_Heavy` · `^Effect_CannonAP_Light` |
| `IxianCombatTankCannon` | `^Effect_CannonHE_Heavy` · `^Effect_CannonAP_Light` |
| `JapanSuperBomb` | `^Effect_Apoc_AP_RA2` · `^Effect_Flame_Heavy` |
| `LatinSmokerCannon` | `^Effect_Apoc_AP_RA2` · `^Effect_Flame_Heavy` |
| `LunarTigerCannon` | `^Effect_Apoc_AP_RA2` · `^Effect_Flame_Heavy` |
| `Lunar_AmplifiedMP40Laser` | `^Effect_AlliedTigerCannon` · `^Effect_Flame_Heavy` |
| `Lunar_AmplifiedMP40Laser_elite` | `^Effect_AlliedTigerCannon` · `^Effect_Flame_Heavy` |
| `Lunar_YellowMP40Laser` | `^Effect_AlliedTigerCannon` · `^Effect_Flame_Heavy` |
| `Lunar_YellowMP40Laser_elite` | `^Effect_AlliedTigerCannon` · `^Effect_Flame_Heavy` |
| `MammothTuskTesla` | `^Effect_AlliedTigerCannon` · `^Effect_Flame_Heavy` |
| `NaxMausCannon_elite` | `^Effect_Apoc_AP_RA2` · `^Effect_Flame_Heavy` |
| `NaxiMP40Laser_elite` | `^Effect_AlliedTigerCannon` · `^Effect_Flame_Heavy` |
| `NaxiShrekCons` | `^Effect_Apoc_AP_RA2` · `^Effect_Flame_Heavy` |
| `RA2Comet` | `^Effect_AlliedTigerCannon` · `^Effect_Flame_Heavy` |
| `RA2LasherCannon` | `^Effect_AlliedTigerCannon` · `^Effect_Flame_Heavy` |
| `RA2LasherCannon_elite` | `^Effect_AlliedTigerCannon` · `^Effect_Flame_Heavy` |
| `RA2PsychicJab` | `^Effect_AlliedTigerCannon` · `^Effect_Flame_Heavy` |
| `RA2RBurritoRocket` | `^Effect_Apoc_AP_RA2` · `^Effect_Flame_Heavy` |
| `RashidanGun_upgrade` | `^Effect_CannonHE_Heavy` · `^Effect_MissileHE_Heavy` |
| `ReaperGrenade` | `^Effect_Concussion_Medium` · `^Effect_CannonHE_Heavy` |
| `TS70mmTur` | `^Effect_CannonHE_Medium` · `^Effect_CannonAP_Light` |
| `TSScoopDualTur` | `^Effect_CannonHE_Heavy` · `^Effect_Concussion_Medium` |
| `bigshieemortar` | `^Effect_AlliedTigerCannon` · `^Effect_Flame_Heavy` |
| `ra120mmThermobaric` | `^Effect_AlliedTigerCannon` · `^Effect_Flame_Heavy` |
| `ra120mmThermobaricTargetingComputer` | `^Effect_AlliedTigerCannon` · `^Effect_Flame_Heavy` |
| `ra1_soviets_grenadier_grenadethermobaric` | `^Effect_AlliedTigerCannon` · `^Effect_Flame_Heavy` |
| `ra1_soviets_kamovattackhelicopter_kamovmissilestesla` | `^Effect_AlliedTigerCannon` · `^Effect_Flame_Heavy` |
| `ra1_soviets_mammothtank_ra120mmthermobaric` | `^Effect_AlliedTigerCannon` · `^Effect_Flame_Heavy` |
| `ra1_soviets_monstertank_missile_tesla` | `^Effect_AlliedTigerCannon` · `^Effect_Flame_Heavy` |
| `ra1_soviets_siegemammothtank_ra120mm2thermobaric` | `^Effect_AlliedTigerCannon` · `^Effect_Flame_Heavy` |
| `ra1_soviets_siegemammothtank_ra120mm2thermobarictargetingcomputer` | `^Effect_AlliedTigerCannon` · `^Effect_Flame_Heavy` |
| `ra1_soviets_teslayak_tesla_bomb` | `^Effect_Apoc_AP_RA2` · `^Effect_Flame_Heavy` |
| `ra1_soviets_v1rockettruck_v1rocketsthermobaric` | `^Effect_AlliedTigerCannon` · `^Effect_Flame_Heavy` |
| `sandmarinemortar` | `^Effect_AlliedTigerCannon` · `^Effect_Flame_Heavy` |


_... and 1 more._


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


## W6 — effect warheads declared LOCALLY (466 vs ratchet 466)

| weapon | nodes | first three |
|---|---|---|
| `12MissilesSpawnerScud` | 6 | `Warhead@Effect: CreateEffect` · `Warhead@Smudge: LeaveSmudge` · `Warhead@RA2Scorch: LeaveSmudge` |
| `155mm` | 10 | `Warhead@Effect: CreateEffect` · `Warhead@Smudge: LeaveSmudge` · `Warhead@DuneRock: LeaveSmudge` |
| `155mmCryo` | 1 | `Warhead@Effect: CreateEffect` |
| `2100Tanktrap` | 1 | `Warhead@Smu: LeaveSmudge` |
| `25mm` | 3 | `Warhead@Effect: CreateEffect` · `Warhead@EffectAir: CreateEffect` · `Warhead@RA2Scorch: LeaveSmudge` |
| `AAGunBoatFlak` | 1 | `Warhead@EffectAir: CreateEffect` |
| `ATMine` | 10 | `Warhead@Effect: CreateEffect` · `Warhead@Smudge: LeaveSmudge` · `Warhead@Concrete: DamagesConcrete` |
| `ArmoredCarMG` | 10 | `Warhead@Effect: CreateEffect` · `Warhead@EffectAir: CreateEffect` · `Warhead@ShieldHitEffect: CreateEffect` |
| `ArtilleryExplode` | 1 | `Warhead@2Eff: CreateEffect` |
| `AsianChaosSuperweapon` | 1 | `Warhead@1: CreateEffect` |
| `AsianChaosTurret` | 1 | `Warhead@Effect: CreateEffect` |
| `AsianChemical` | 5 | `Warhead@Effect: CreateEffect` · `Warhead@Smudge1: LeaveSmudge` · `Warhead@RA2Crater: LeaveSmudge` |
| `AsianHarbingerPlasma` | 6 | `Warhead@Effect: CreateEffect` · `Warhead@RA2Scorch: LeaveSmudge` · `Warhead@Smudge: LeaveSmudge` |
| `AsianIonBeamMini` | 1 | `Warhead@Effect: CreateEffect` |
| `AsianMaidenBow` | 7 | `Warhead@Effect: CreateEffect` · `Warhead@EffectAir: CreateEffect` · `Warhead@DuneRock: LeaveSmudge` |
| `AsianOilBombFragments` | 1 | `Warhead@Effect: CreateEffect` |
| `AsianPhoenixRocket` | 1 | `Warhead@RA2Scorch: LeaveSmudge` |
| `AsianPhoenixRocket_elite` | 1 | `Warhead@RA2Scorch: LeaveSmudge` |
| `AsianPhotonCannon` | 8 | `Warhead@Effect: CreateEffect` · `Warhead@Smudge: LeaveSmudge` · `Warhead@DuneRock: LeaveSmudge` |
| `AsianSmallTorpedo` | 1 | `Warhead@Effect: CreateEffect` |
| `AsianSniperAP` | 1 | `Warhead@Effect: CreateEffect` |
| `AsianSubmarineBomb` | 4 | `Warhead@Effect: CreateEffect` · `Warhead@Smudge: LeaveSmudge` · `Warhead@DuneRock: LeaveSmudge` |
| `AsianTSIonCannon` | 2 | `Warhead@3Smu_area: LeaveSmudge` · `Warhead@Effect: CreateEffect` |
| `AsianTurretPlasma` | 5 | `Warhead@Effect: CreateEffect` · `Warhead@Smudge: LeaveSmudge` · `Warhead@DuneRock: LeaveSmudge` |
| `AthenaLaser` | 13 | `Warhead@Effect: CreateEffect` · `Warhead@Effect2: CreateEffect` · `Warhead@Effect3: CreateEffect` |
| `AtreusMG` | 1 | `Warhead@Effect: CreateEffect` |
| `BCYamatoCannon` | 1 | `Warhead@Effect: CreateEffect` |
| `BHBombs` | 1 | `Warhead@3Eff: CreateEffect` |
| `BallistaMultiShot` | 9 | `Warhead@Effect: CreateEffect` · `Warhead@Smudge: LeaveSmudge` · `Warhead@DuneRock: LeaveSmudge` |
| `BallistaMultiShotEnergized` | 5 | `Warhead@Effect: CreateEffect` · `Warhead@Smudge: LeaveSmudge` · `Warhead@RA2Scorch: LeaveSmudge` |
| `BarrelExplode` | 11 | `Warhead@2Eff: CreateEffect` · `Warhead@Smu: LeaveSmudge` · `Warhead@Glow: GlowImpact` |
| `BehemothShoot` | 3 | `Warhead@Effect: CreateEffect` · `Warhead@Effect2: CreateEffect` · `Warhead@EffectAir: CreateEffect` |
| `BigChemSpray` | 1 | `Warhead@3Eff: CreateEffect` |
| `BlackEagleThunderboltMissiles` | 6 | `Warhead@Effect: CreateEffect` · `Warhead@Smudge1: LeaveSmudge` · `Warhead@Smudge2: LeaveSmudge` |
| `BlackHoleSuck` | 1 | `Warhead@Effect: CreateEffect` |
| `BoatMissile` | 2 | `Warhead@3Eff: CreateEffect` · `Warhead@4EffAir: CreateEffect` |
| `BuggyPlasmaGrenade` | 7 | `Warhead@Effect: CreateEffect` · `Warhead@Smudge1: LeaveSmudge` · `Warhead@RA2Crater: LeaveSmudge` |
| `BuildingExplode` | 2 | `Warhead@Effect: CreateEffect` · `Warhead@Smudge: LeaveSmudge` |
| `BuildingExplodeProtoss` | 2 | `Warhead@Effect: CreateEffect` · `Warhead@2Smu: LeaveSmudge` |
| `C4` | 1 | `Warhead@2Eff: CreateEffect` |


_... and 426 more._


_all buckets at or below their ratchets_ — this is the pre-existing conversion backlog. **Lower each baseline as you convert; never raise one.**
