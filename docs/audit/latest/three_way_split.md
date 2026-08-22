# audit_three_way_split — 393 weapons break ONE-warhead/ONE-projectile/ONE-effect

    237  >1 ^Warhead_*
    204  bundle mixed with layers
     50  >1 ^Effect_*
     12  >1 ^Projectile_*

    119  legacy bundle templates in play

| weapon | problem |
|---|---|
| 110mm_Gun | 2x ^Warhead_*; 2x ^Projectile_*; 2x ^Effect_*; bundle: ^D2K_Cannon |
| 120mmDualHV | bundle: ^HVProjectile |
| 120mmHV | bundle: ^HVProjectile |
| 120mm_cobra | 3x ^Warhead_*; bundle: ^D2K_Cannon |
| 12MissilesSpawnerScud | 2x ^Warhead_*; 2x ^Effect_*; bundle: ^RA2Grenade, ^RA2HeavyMissile |
| 25mm | bundle: ^Grenade, ^LightFlameWeapon, ^MediumChemicalWeapon |
| 8Inch | 2x ^Warhead_* |
| APCGun | 2x ^Warhead_* |
| APCGunAllies | 2x ^Warhead_* |
| ASDFKamikazeExplosion | 2x ^Warhead_* |
| AlliedTankDestroyerCannon | 2x ^Warhead_* |
| Aphid_AA | 2x ^Warhead_* |
| ArtilleryShell | 2x ^Warhead_* |
| AsianChemicalBombs | bundle: ^RA2MediumCannon |
| AsianGrenade | bundle: ^RA2MediumCannon |
| AsianHarbingerPlasma | 2x ^Effect_*; bundle: ^HeavyBomb, ^LightChemicalWeapon, ^LightFlameWeapon |
| AsianHowitzerSplash | 2x ^Warhead_* |
| AsianNinjaStar | 2x ^Effect_* |
| AsianPelicanMissile | 2x ^Warhead_*; 2x ^Effect_*; bundle: ^RA2MediumMissile |
| AsianPhoenixRocket | 2x ^Warhead_*; 2x ^Effect_*; bundle: ^RA2HeavyMissile |
| AsianPulverizerGatling | 2x ^Warhead_*; bundle: ^RA2Chaingun |
| AsianRailTank | 2x ^Effect_* |
| AsianRailgun | 2x ^Effect_* |
| AsianSinglePlasma | 2x ^Effect_*; bundle: ^HeavyBomb, ^LightChemicalWeapon, ^LightFlameWeapon |
| AsianSmallTorpedo | bundle: ^RA2Grenade, ^RA2HeavyMissile |
| AsianSniper | bundle: ^Chaingun, ^FlakWeapon, ^MediumMissile |
| AsianSubmarineBomb | 2x ^Effect_*; bundle: ^RA2Grenade |
| AsianTankMine | bundle: ^RA2TankDestroyerCannon |
| AtreusMG | bundle: ^FlakWeapon, ^Grenade, ^MediumMissile |
| BCLaser | bundle: ^HeavyAAWeapon, ^HeavyBomb, ^HeavyChemicalWeapon |
| BTRMachineGun | 2x ^Warhead_* |
| BTRTeslaMachineGun | 2x ^Warhead_* |
| BigShieeTusk | 2x ^Warhead_* |
| BlackEagleMissiles | 2x ^Warhead_*; bundle: ^RA2MediumMissile |
| CHGuardRifle | 2x ^Warhead_* |
| CabalCyborgChaingun | 2x ^Warhead_* |
| CabalHeavyReaperMissiles | 4x ^Warhead_*; bundle: ^CabalMissileEffect, ^CabalMissileLight |
| CabalHeavyReaperMissiles_AA | 4x ^Warhead_*; bundle: ^CabalMissileEffect, ^CabalMissileLight |
| CabalLegionGun | 2x ^Warhead_* |
| CabalMagicNuke | 2x ^Warhead_* |

_(353 more)_

WARN 393 violating weapons (ratchet 393)
Lower `SPLIT_BASELINE` as W24 converts weapons; never raise it.
