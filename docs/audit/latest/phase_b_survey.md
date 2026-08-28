# Phase B Mixed-Weapon Survey

Generated: 2026-08-28
Total concrete weapons on old families: 155
Single old-family with new inherits (Phase B completion): 0
Pure single old-family (mechanical Phase A candidates): 1
Mixed old-family (Phase B maintainer sign-off): 154 in 129 groups

## Pure single old-family (mechanical Phase A candidates)
- `RA2CRM60H` (ContentPacks\RedAlert2\Shared\yaml\weapons.yaml) | old: SniperWeapon | CannonHE_Heavy=2000, Bullet_Medium=2000, SniperWeapon=2000, SniperWeaponExtraDamage=2000, SniperWeaponPercentage=1

## Single old-family with new inherits (finish conversion)
## Mixed-inherit (Phase B) — dominant-damage analysis for maintainer sign-off
### Grenade, HeavyBomb, HeavyCannon, HeavyChemicalWeapon, HeavyFlameWeapon, LightChemicalWeapon, LightFlameWeapon, LightMissile, MediumCannon, MediumChemicalWeapon, MediumFlameWeapon, NuclearWarhead, ShrapnelWeapon, TankDestroyerCannon (2 weapons)
- `SiegeEngineCannon` (weapons\warcraft2.yaml) | dominant: NuclearWarhead(10000) | NuclearWarhead=10000, NuclearWarheadPercentage=5, LightMissile=10000, LightMissilePercentage=5, LightChemicalWeapon=10000, LightChemicalWeaponPercentage=5, MediumChemicalWeapon=10000, MediumChemicalWeaponPercentage=5, HeavyChemicalWeapon=10000, HeavyChemicalWeaponPercentage=5, LightFlameWeapon=10000, LightFlameWeaponPercentage=5, MediumFlameWeapon=10000, MediumFlameWeaponPercentage=5, HeavyFlameWeapon=10000, HeavyFlameWeaponPercentage=5, Grenade=10000, GrenadeFriendlyFire=5000, GrenadePercentage=5, ShrapnelWeapon=10000, ShrapnelWeaponFriendlyFire=5000, ShrapnelWeaponPercentage=5, HeavyBomb=10000, HeavyBombPercentage=5, HeavyCannon=10000, HeavyCannonPercentage=5, MediumCannon=10000, MediumCannonPercentage=5, TankDestroyerCannon=10000, TankDestroyerCannonPercentage=5, Effect=0 | → collapse to NuclearWarhead
- `SiegeTankSiegeCannon` (ContentPacks\StarCraft\Terran\yaml\weapons.yaml) | dominant: NuclearWarhead(10000) | NuclearWarhead=10000, NuclearWarheadPercentage=5, LightMissile=10000, LightMissilePercentage=5, LightChemicalWeapon=10000, LightChemicalWeaponPercentage=5, MediumChemicalWeapon=10000, MediumChemicalWeaponPercentage=5, HeavyChemicalWeapon=10000, HeavyChemicalWeaponPercentage=5, LightFlameWeapon=10000, LightFlameWeaponPercentage=5, MediumFlameWeapon=10000, MediumFlameWeaponPercentage=5, HeavyFlameWeapon=10000, HeavyFlameWeaponPercentage=5, Grenade=10000, GrenadeFriendlyFire=5000, GrenadePercentage=5, ShrapnelWeapon=10000, ShrapnelWeaponFriendlyFire=5000, ShrapnelWeaponPercentage=5, HeavyBomb=10000, HeavyBombPercentage=5, HeavyCannon=10000, HeavyCannonPercentage=5, MediumCannon=10000, MediumCannonPercentage=5, TankDestroyerCannon=10000, TankDestroyerCannonPercentage=5, Effect=0 | → collapse to NuclearWarhead

### ArrowWeapon, Chaingun, FlakWeapon, Grenade, HeavyAAWeapon, HeavyMissile, LightMissile, MediumChemicalWeapon, MediumMissile (1 weapons)
- `EpigraphMG` (ContentPacks\StarCraft\Protoss\yaml\weapons.yaml) | dominant: Grenade(2000) | Grenade=2000, GrenadeFriendlyFire=1000, GrenadePercentage=1, ArrowWeapon=2000, ArrowWeaponPercentage=1, LightMissile=2000, LightMissilePercentage=1, MediumMissile=2000, MediumMissilePercentage=1, HeavyMissile=2000, HeavyMissilePercentage=1, MediumChemicalWeapon=2000, MediumChemicalWeaponPercentage=1, HeavyAAWeapon=2000, HeavyAAWeaponPercentage=1, CannonHE_Heavy=2000, FlakWeapon=2000, FlakWeaponPercentage=1, Chaingun=2000, ChaingunPercentage=1, Effect=0, EffectWater=0, EffectAir=0 | → collapse to Grenade

### Grenade, HeavyBomb, HeavyCannon, HeavyChemicalWeapon, LightChemicalWeapon, MediumCannon, MediumChemicalWeapon, ShrapnelWeapon, TankDestroyerCannon (1 weapons)
- `RA2LasherToxicMortar` (ContentPacks\RedAlert2\Yuri\yaml\weapons.yaml) | dominant: Grenade(2000) | Grenade=2000, GrenadeFriendlyFire=1000, GrenadePercentage=1, ShrapnelWeapon=2000, ShrapnelWeaponFriendlyFire=1000, ShrapnelWeaponPercentage=1, HeavyBomb=2000, HeavyBombPercentage=1, LightChemicalWeapon=2000, LightChemicalWeaponPercentage=1, MediumChemicalWeapon=2000, MediumChemicalWeaponPercentage=1, HeavyChemicalWeapon=2000, HeavyChemicalWeaponPercentage=1, TankDestroyerCannon=2000, TankDestroyerCannonPercentage=1, MediumCannon=2000, MediumCannonPercentage=1, HeavyCannon=2000, HeavyCannonPercentage=1, Effect=0, Cloud=0 | → collapse to Grenade

### HeavyBomb, HeavyChemicalWeapon, HeavyFlameWeapon, HeavyMissile, LaserWeapon, MagicWeapon, RailgunWeapon, TeslaChargedWeapon, TeslaWeapon (1 weapons)
- `FutureHarbingerCannon` (ContentPacks\RedAlert2Mod\FutureTech\yaml\weapons.yaml) | dominant: HeavyChemicalWeapon(4000) | HeavyChemicalWeapon=4000, HeavyChemicalWeaponPercentage=2, HeavyFlameWeapon=4000, HeavyFlameWeaponPercentage=2, HeavyBomb=4000, HeavyBombPercentage=2, TeslaChargedWeapon=4000, TeslaChargedExtraDamage=2000, TeslaChargedWeaponPercentage=2, TeslaWeapon=4000, TeslaExtraDamage=2000, TeslaWeaponPercentage=2, MagicWeapon=4000, MagicWeaponPercentage=2, LaserWeapon=4000, LaserWeaponPercentage=2, RailgunWeapon=4000, RailgunWeaponPercentage=2, HeavyMissile=4000, HeavyMissilePercentage=2, CannonHE_Heavy=4000, Effect1=0, Effect2=0, Effect=0 | → collapse to HeavyChemicalWeapon

### Chaingun, FlakWeapon, Grenade, HeavyBomb, MediumChemicalWeapon, MediumFlameWeapon, MediumMissile, ShrapnelWeapon (1 weapons)
- `SwarmlingShoot` (ContentPacks\StarCraft\Zerg\yaml\weapons.yaml) | dominant: MediumChemicalWeapon(2000) | MediumChemicalWeapon=2000, MediumChemicalWeaponPercentage=1, MediumFlameWeapon=2000, MediumFlameWeaponPercentage=1, MediumMissile=2000, MediumMissilePercentage=1, Chaingun=2000, ChaingunPercentage=1, FlakWeapon=2000, FlakWeaponPercentage=1, Grenade=2000, GrenadeFriendlyFire=1000, GrenadePercentage=1, ShrapnelWeapon=2000, ShrapnelWeaponFriendlyFire=1000, ShrapnelWeaponPercentage=1, HeavyBomb=2000, HeavyBombPercentage=1, Effect=0, EffectAir=0 | → collapse to MediumChemicalWeapon

### Grenade, HeavyCannon, MagicWeapon, MediumCannon, MediumChemicalWeapon, RailgunWeapon, ShrapnelWeapon, TeslaChargedWeapon (1 weapons)
- `CabalArtilleryWalkerShellUpgraded` (ContentPacks\TiberianSun\CABAL\yaml\weapons.yaml) | dominant: Grenade(42000) | Grenade=42000, GrenadeFriendlyFire=21000, GrenadePercentage=21, ShrapnelWeapon=42000, ShrapnelWeaponFriendlyFire=21000, ShrapnelWeaponPercentage=21, MediumCannon=42000, MediumCannonPercentage=21, HeavyCannon=42000, HeavyCannonPercentage=21, MediumChemicalWeapon=42000, MediumChemicalWeaponPercentage=21, TeslaChargedWeapon=42000, TeslaChargedWeaponPercentage=21, MagicWeapon=42000, MagicWeaponPercentage=21, RailgunWeapon=42000, RailgunWeaponPercentage=21, Effect=0 | → collapse to Grenade

### HeavyAAWeapon, HeavyBomb, HeavyChemicalWeapon, HeavyFlameWeapon, HeavyMissile, LaserWeapon, NuclearWarhead, RailgunWeapon (1 weapons)
- `BCLaser` (ContentPacks\StarCraft\Terran\yaml\weapons.yaml) | dominant: NuclearWarhead(4000) | NuclearWarhead=4000, NuclearWarheadPercentage=2, RailgunWeapon=4000, RailgunWeaponPercentage=2, HeavyMissile=4000, HeavyMissilePercentage=2, CannonHE_Heavy=4000, HeavyAAWeapon=4000, HeavyAAWeaponPercentage=2, HeavyBomb=4000, HeavyBombPercentage=2, HeavyChemicalWeapon=4000, HeavyChemicalWeaponPercentage=2, HeavyFlameWeapon=4000, HeavyFlameWeaponPercentage=2, LaserWeapon=4000, LaserWeaponPercentage=2 | → collapse to NuclearWarhead

### Grenade, HeavyBomb, LightFlameWeapon, LightMissile, MediumChemicalWeapon, MediumMissile, ShrapnelWeapon (1 weapons)
- `facedancer_grenade` (ContentPacks\D2k\Ordos\yaml\weapons.yaml) | dominant: LightFlameWeapon(20000) | LightFlameWeapon=20000, LightFlameWeaponPercentage=10, MediumChemicalWeapon=20000, MediumChemicalWeaponPercentage=10, HeavyBomb=20000, HeavyBombPercentage=10, Grenade=20000, GrenadeFriendlyFire=10000, GrenadePercentage=10, ShrapnelWeapon=20000, ShrapnelWeaponFriendlyFire=10000, ShrapnelWeaponPercentage=10, CannonHE_Heavy=20000, LightMissile=20000, LightMissilePercentage=10, MediumMissile=20000, MediumMissilePercentage=10, MissileAP_Heavy=20000, Smudge=0, Effect=0 | → collapse to LightFlameWeapon

### HeavyAAWeapon, HeavyBomb, HeavyChemicalWeapon, HeavyFlameWeapon, HeavyMissile, LaserWeapon, RailgunWeapon (1 weapons)
- `PhobosLaser` (ContentPacks\StarCraft\Terran\yaml\weapons.yaml) | dominant: RailgunWeapon(6000) | RailgunWeapon=6000, RailgunWeaponPercentage=3, HeavyMissile=6000, HeavyMissilePercentage=3, CannonHE_Heavy=6000, HeavyAAWeapon=6000, HeavyAAWeaponPercentage=3, HeavyBomb=6000, HeavyBombPercentage=3, HeavyChemicalWeapon=6000, HeavyChemicalWeaponPercentage=3, HeavyFlameWeapon=6000, HeavyFlameWeaponPercentage=3, LaserWeapon=6000, LaserWeaponPercentage=3, EffectAir=0 | → collapse to RailgunWeapon

### Chaingun, FlakWeapon, LaserWeapon, LightMissile, SmallArms, TeslaWeapon (5 weapons)
- `AthenaLaser` (ContentPacks\RedAlert2Mod\FutureTech\yaml\weapons.yaml) | dominant: LightMissile(32000) | LightMissile=32000, LightMissilePercentage=16, SmallArms=32000, SmallArmsPercentage=16, Chaingun=32000, ChaingunPercentage=16, FlakWeapon=32000, FlakWeaponPercentage=16, TeslaWeapon=32000, TeslaExtraDamage=1000, TeslaWeaponPercentage=16, LaserWeapon=32000, LaserWeaponPercentage=16, Effect=0, Effect2=0, Effect3=0, Effect4=0, Effect5=0, Effect6=0, Effect7=0, GlowPlayer=0, GlowCore=0 | → collapse to LightMissile
- `CryoLegionnaireAttack` (ContentPacks\RedAlert2Mod\FutureTech\yaml\weapons.yaml) | dominant: LightMissile(2000) | LightMissile=2000, LightMissilePercentage=1, SmallArms=2000, SmallArmsPercentage=1, Chaingun=2000, ChaingunPercentage=1, FlakWeapon=2000, FlakWeaponPercentage=1, TeslaWeapon=2000, TeslaExtraDamage=1000, TeslaWeaponPercentage=1, LaserWeapon=2000, LaserWeaponPercentage=1, PhysicalStateCryo1=0, PhysicalStateCryo2=0, PhysicalStateCryo3=0, PhysicalStateCryo4=0, Effect=0 | → collapse to LightMissile
- `Future_Cryocopter_Cryo` (ContentPacks\RedAlert2Mod\FutureTech\yaml\weapons.yaml) | dominant: SmallArms(2000) | LightMissilePercentage=1, SmallArms=2000, SmallArmsPercentage=1, Chaingun=2000, ChaingunPercentage=1, FlakWeapon=2000, FlakWeaponPercentage=1, TeslaWeapon=2000, TeslaExtraDamage=1000, TeslaWeaponPercentage=1, LaserWeapon=2000, LaserWeaponPercentage=1, PhysicalStateCryo1=0, PhysicalStateCryo2=0, PhysicalStateCryo3=0, PhysicalStateCryo4=0, Effect=0, EffectAir=0 | → collapse to SmallArms
- `StarshipSovereignBeam` (ContentPacks\StarCraft\Protoss\yaml\weapons.yaml) | dominant: LightMissile(2000) | LightMissile=2000, LightMissilePercentage=1, SmallArms=2000, SmallArmsPercentage=1, Chaingun=2000, ChaingunPercentage=1, FlakWeapon=2000, FlakWeaponPercentage=1, TeslaWeapon=2000, TeslaExtraDamage=1000, TeslaWeaponPercentage=1, LaserWeapon=2000, LaserWeaponPercentage=1, PhysicalStateCryo1=0, PhysicalStateCryo2=0, PhysicalStateCryo3=0, Effect=0, EffectAir=0 | → collapse to LightMissile
- `VoidRayBeam` (ContentPacks\StarCraft\Protoss\yaml\weapons.yaml) | dominant: LightMissile(2000) | LightMissile=2000, LightMissilePercentage=1, SmallArms=2000, SmallArmsPercentage=1, Chaingun=2000, ChaingunPercentage=1, FlakWeapon=2000, FlakWeaponPercentage=1, TeslaWeapon=2000, TeslaExtraDamage=1000, TeslaWeaponPercentage=1, LaserWeapon=2000, LaserWeaponPercentage=1, PhysicalStateCryo1=0, PhysicalStateCryo2=0, PhysicalStateCryo3=0, Effect=0, EffectAir=0 | → collapse to LightMissile

### ArrowWeapon, Grenade, HeavyMissile, MediumChemicalWeapon, MediumFlameWeapon, TeslaWeapon (2 weapons)
- `CabalMothershipRockets` (ContentPacks\TiberianSun\CABAL\yaml\weapons.yaml) | dominant: EMPUnit(30000) | TeslaWeapon=10000, TeslaExtraDamage=5000, TeslaWeaponPercentage=5, HeavyMissile=10000, HeavyMissilePercentage=5, MediumChemicalWeapon=10000, MediumChemicalWeaponPercentage=5, MediumFlameWeapon=10000, MediumFlameWeaponPercentage=5, Grenade=10000, GrenadeFriendlyFire=5000, GrenadePercentage=5, ArrowWeapon=10000, ArrowWeaponPercentage=5, EMPUnit=30000, Effect=0, EffectAir=0 | → collapse to EMPUnit
- `RocketAngelRockets` (ContentPacks\RedAlert\Japan\yaml\weapons.yaml) | dominant: EMPUnit(6000) | TeslaWeapon=2000, TeslaExtraDamage=1000, TeslaWeaponPercentage=1, HeavyMissile=2000, HeavyMissilePercentage=1, MediumChemicalWeapon=2000, MediumChemicalWeaponPercentage=1, MediumFlameWeapon=2000, MediumFlameWeaponPercentage=1, Grenade=2000, GrenadeFriendlyFire=1000, GrenadePercentage=1, ArrowWeapon=2000, ArrowWeaponPercentage=1, EMPUnit=6000, Effect=0, EffectAir=0 | → collapse to EMPUnit

### HeavyBomb, LightChemicalWeapon, LightFlameWeapon, MediumChemicalWeapon, MediumFlameWeapon, ShrapnelWeapon (2 weapons)
- `AsianHarbingerPlasma` (ContentPacks\RedAlert2Mod\AsianAlliance\yaml\weapons.yaml) | dominant: LightFlameWeapon(2000) | LightFlameWeapon=2000, LightFlameWeaponPercentage=1, LightChemicalWeapon=2000, LightChemicalWeaponPercentage=1, MediumFlameWeapon=2000, MediumFlameWeaponPercentage=1, MediumChemicalWeapon=2000, MediumChemicalWeaponPercentage=1, CannonHE_Medium=2000, ShrapnelWeapon=2000, ShrapnelWeaponFriendlyFire=1000, ShrapnelWeaponPercentage=1, HeavyBomb=2000, HeavyBombPercentage=1, MissileAP_Medium=2000, FireShrapnel=0, Effect=0 | → collapse to LightFlameWeapon
- `AsianSinglePlasma` (ContentPacks\RedAlert2Mod\AsianAlliance\yaml\weapons.yaml) | dominant: LightFlameWeapon(2000) | LightFlameWeapon=2000, LightFlameWeaponPercentage=1, LightChemicalWeapon=2000, LightChemicalWeaponPercentage=1, MediumFlameWeapon=2000, MediumFlameWeaponPercentage=1, MediumChemicalWeapon=2000, MediumChemicalWeaponPercentage=1, CannonHE_Medium=2000, ShrapnelWeapon=2000, ShrapnelWeaponFriendlyFire=1000, ShrapnelWeaponPercentage=1, HeavyBomb=2000, HeavyBombPercentage=1, CannonHE_Heavy=2000, FireShrapnel=0 | → collapse to LightFlameWeapon

### HeavyCannon, MagicWeapon, MediumChemicalWeapon, MediumFlameWeapon, RailgunWeapon, TeslaWeapon (2 weapons)
- `CabalCommandoPlasmaMk2Neutron` (ContentPacks\TiberianSun\CABAL\yaml\weapons.yaml) | dominant: HeavyCannon(50000) | HeavyCannon=50000, HeavyCannonPercentage=25, MediumFlameWeapon=50000, MediumFlameWeaponPercentage=25, MediumChemicalWeapon=50000, MediumChemicalWeaponPercentage=25, TeslaWeapon=50000, TeslaWeaponPercentage=25, MagicWeapon=50000, MagicWeaponPercentage=25, RailgunWeapon=50000, RailgunWeaponPercentage=25, Effect=0 | → collapse to HeavyCannon
- `CabalCommandoPlasmaNeutron` (ContentPacks\TiberianSun\CABAL\yaml\weapons.yaml) | dominant: HeavyCannon(50000) | HeavyCannon=50000, HeavyCannonPercentage=25, MediumFlameWeapon=50000, MediumFlameWeaponPercentage=25, MediumChemicalWeapon=50000, MediumChemicalWeaponPercentage=25, TeslaWeapon=50000, TeslaWeaponPercentage=25, MagicWeapon=50000, MagicWeaponPercentage=25, RailgunWeapon=50000, RailgunWeaponPercentage=25, Effect=0 | → collapse to HeavyCannon

### Chaingun, FlakWeapon, Grenade, LightMissile, MediumCannon, SmallArms (1 weapons)
- `BoxerCannonAG` (ContentPacks\TiberianDawn\GDI\yaml\weapons.yaml) | dominant: SmallArms(2000) | SmallArms=2000, SmallArmsPercentage=1, LightMissile=2000, LightMissilePercentage=1, Grenade=2000, GrenadeFriendlyFire=1000, GrenadePercentage=1, MediumCannon=2000, MediumCannonPercentage=1, Chaingun=2000, ChaingunPercentage=1, FlakWeapon=2000, FlakWeaponPercentage=1 | → collapse to SmallArms

### Chaingun, FlakWeapon, HeavyCannon, LaserWeapon, LightFlameWeapon, SmallArms (1 weapons)
- `TSTurretLaserFire` (weapons\weapons.yaml) | dominant: SmallArms(1000) | SmallArms=1000, SmallArmsPercentage=1, Chaingun=1000, ChaingunPercentage=1, HeavyCannon=1000, HeavyCannonPercentage=1, LightFlameWeapon=1000, LightFlameWeaponPercentage=1, FlakWeapon=1000, FlakWeaponPercentage=1, LaserWeapon=1000, LaserWeaponPercentage=1 | → collapse to SmallArms

### Chaingun, Grenade, MediumCannon, ShrapnelWeapon, SmallArms, TankDestroyerCannon (1 weapons)
- `HovercraftCannon` (ContentPacks\RedAlert\Japan\yaml\weapons.yaml) | dominant: SmallArms(2000) | SmallArms=2000, SmallArmsPercentage=1, Chaingun=2000, ChaingunPercentage=1, Grenade=2000, GrenadeFriendlyFire=1000, GrenadePercentage=1, ShrapnelWeapon=2000, ShrapnelWeaponFriendlyFire=1000, ShrapnelWeaponPercentage=1, TankDestroyerCannon=2000, TankDestroyerCannonPercentage=1, MediumCannon=2000, MediumCannonPercentage=1, Effect=0 | → collapse to SmallArms

### FlakWeapon, HeavyAAWeapon, HeavyBomb, MediumChemicalWeapon, MediumFlameWeapon, MediumMissile (1 weapons)
- `WyvernRockets` (ContentPacks\StarCraft\Terran\yaml\weapons.yaml) | dominant: MediumFlameWeapon(2000) | MediumFlameWeapon=2000, MediumFlameWeaponPercentage=1, MediumChemicalWeapon=2000, MediumChemicalWeaponPercentage=1, CannonHE_Heavy=2000, HeavyBomb=2000, HeavyBombPercentage=1, FlakWeapon=2000, FlakWeaponPercentage=1, MediumMissile=2000, MediumMissilePercentage=1, HeavyAAWeapon=2000, HeavyAAWeaponPercentage=1, MissileAP_Heavy=2000 | → collapse to MediumFlameWeapon

### FlakWeapon, LaserWeapon, LightFlameWeapon, MediumChemicalWeapon, MediumMissile, ShrapnelWeapon (1 weapons)
- `DreadshroudSpore` (ContentPacks\StarCraft\Zerg\yaml\weapons.yaml) | dominant: LightFlameWeapon(2000) | LightFlameWeapon=2000, LightFlameWeaponPercentage=1, MediumMissile=2000, MediumMissilePercentage=1, ShrapnelWeapon=2000, ShrapnelWeaponFriendlyFire=1000, ShrapnelWeaponPercentage=1, MediumChemicalWeapon=2000, MediumChemicalWeaponPercentage=1, LaserWeapon=2000, LaserWeaponPercentage=1, Condition=0, Effect=0, EffectAir=0 | → collapse to LightFlameWeapon

### Grenade, HeavyBomb, HeavyMissile, LightFlameWeapon, MediumChemicalWeapon, ShrapnelWeapon (1 weapons)
- `BehemothShoot` (ContentPacks\StarCraft\Zerg\yaml\weapons.yaml) | dominant: LightFlameWeapon(2000) | LightFlameWeapon=2000, LightFlameWeaponPercentage=1, MediumChemicalWeapon=2000, MediumChemicalWeaponPercentage=1, HeavyMissile=2000, HeavyMissilePercentage=1, Grenade=2000, GrenadeFriendlyFire=1000, GrenadePercentage=1, ShrapnelWeapon=2000, ShrapnelWeaponFriendlyFire=1000, ShrapnelWeaponPercentage=1, HeavyBomb=2000, HeavyBombPercentage=1, Effect=0, Effect2=0, EffectAir=0 | → collapse to LightFlameWeapon

### Grenade, HeavyBomb, LightChemicalWeapon, MediumFlameWeapon, NuclearWarhead, ShrapnelWeapon (1 weapons)
- `RA2TOPOLCuba` (ContentPacks\RedAlert2Mod\Syndicate\yaml\weapons.yaml) | dominant: NuclearWarhead(20000) | NuclearWarhead=20000, NuclearWarheadPercentage=10, HeavyBomb=20000, HeavyBombPercentage=10, MediumFlameWeapon=20000, MediumFlameWeaponPercentage=10, ShrapnelWeapon=20000, ShrapnelWeaponFriendlyFire=10000, ShrapnelWeaponPercentage=10, LightChemicalWeapon=20000, LightChemicalWeaponPercentage=10, Grenade=20000, GrenadeFriendlyFire=10000, GrenadePercentage=10, Effect=0, GroundFireArea=0, Radiation=0 | → collapse to NuclearWarhead

### Grenade, HeavyBomb, MediumCannon, MediumChemicalWeapon, MediumFlameWeapon, ShrapnelWeapon (1 weapons)
- `ArtilleryShellUpgrade` (ContentPacks\TiberianDawn\Nod\yaml\weapons.yaml) | dominant: Grenade(6000) | Grenade=6000, GrenadeFriendlyFire=3000, GrenadePercentage=3, ShrapnelWeapon=6000, ShrapnelWeaponFriendlyFire=3000, ShrapnelWeaponPercentage=3, MediumChemicalWeapon=6000, MediumChemicalWeaponPercentage=3, MediumFlameWeapon=6000, MediumFlameWeaponPercentage=3, MediumCannon=6000, MediumCannonPercentage=3, HeavyBomb=6000, HeavyBombPercentage=3 | → collapse to Grenade

### Grenade, MediumCannon, MediumChemicalWeapon, MediumFlameWeapon, ShrapnelWeapon, TeslaWeapon (1 weapons)
- `SteelInfRailgun_EMP` (ContentPacks\RedAlert2Mod\Consortium\yaml\weapons.yaml) | dominant: EMPUnit(10000) | Grenade=2000, GrenadeFriendlyFire=1000, GrenadePercentage=1, MediumFlameWeapon=2000, MediumFlameWeaponPercentage=1, MediumChemicalWeapon=2000, MediumChemicalWeaponPercentage=1, ShrapnelWeapon=2000, ShrapnelWeaponFriendlyFire=1000, ShrapnelWeaponPercentage=1, MediumCannon=2000, MediumCannonPercentage=1, TeslaWeapon=2000, TeslaExtraDamage=1000, TeslaWeaponPercentage=1, Railgun_Heavy=2000, Flak_Medium=2000, Bullet_Medium=2000, MissileAP_Light=2000, EMPUnit=10000 | → collapse to EMPUnit

### HeavyBomb, HeavyCannon, MediumCannon, MediumChemicalWeapon, MediumFlameWeapon, TankDestroyerCannon (1 weapons)
- `SpecterArtilleryShellUpgrade` (ContentPacks\TiberianDawn\Nod\yaml\weapons.yaml) | dominant: Demolition_Light(4000) | Demolition_Light=4000, Concussion_Medium=4000, MediumChemicalWeapon=4000, MediumChemicalWeaponPercentage=2, MediumFlameWeapon=4000, MediumFlameWeaponPercentage=2, HeavyBomb=4000, HeavyBombPercentage=2, TankDestroyerCannon=4000, TankDestroyerCannonPercentage=2, MediumCannon=4000, MediumCannonPercentage=2, HeavyCannon=4000, HeavyCannonPercentage=2 | → collapse to Demolition_Light

### HeavyCannon, LightChemicalWeapon, MediumCannon, RailgunWeapon, ShrapnelWeapon, TankDestroyerCannon (1 weapons)
- `edenRailgun` (weapons\outpost2.yaml) | dominant: RailgunWeapon(4000) | RailgunWeapon=4000, RailgunWeaponPercentage=2, ShrapnelWeapon=4000, ShrapnelWeaponFriendlyFire=2000, ShrapnelWeaponPercentage=2, LightChemicalWeapon=4000, LightChemicalWeaponPercentage=2, TankDestroyerCannon=4000, TankDestroyerCannonPercentage=2, MediumCannon=4000, MediumCannonPercentage=2, HeavyCannon=4000, HeavyCannonPercentage=2 | → collapse to RailgunWeapon

### Grenade, HeavyBomb, HeavyFlameWeapon, MediumChemicalWeapon, TankDestroyerCannon (2 weapons)
- `NaxiShrek` (ContentPacks\RedAlert2Mod\Naxis\yaml\weapons.yaml) | dominant: HeavyBomb(8000) | HeavyBomb=8000, HeavyBombPercentage=4, HeavyFlameWeapon=8000, HeavyFlameWeaponPercentage=4, Grenade=8000, GrenadeFriendlyFire=4000, GrenadePercentage=4, TankDestroyerCannon=8000, TankDestroyerCannonPercentage=4, MediumChemicalWeapon=8000, MediumChemicalWeaponPercentage=4, MissileAP_Medium=8000 | → collapse to HeavyBomb
- `NaxiShrekCons` (ContentPacks\RedAlert2Mod\Naxis\yaml\weapons.yaml) | dominant: HeavyBomb(6000) | HeavyBomb=6000, HeavyBombPercentage=3, HeavyFlameWeapon=6000, HeavyFlameWeaponPercentage=3, Grenade=6000, GrenadeFriendlyFire=3000, GrenadePercentage=3, TankDestroyerCannon=6000, TankDestroyerCannonPercentage=3, MediumChemicalWeapon=6000, MediumChemicalWeaponPercentage=3, MissileAP_Medium=6000 | → collapse to HeavyBomb

### ArrowWeapon, Chaingun, FlakWeapon, Grenade, TankDestroyerCannon (1 weapons)
- `wc2arrowFire` (weapons\warcraft2.yaml) | dominant: FlakWeapon(4000) | FlakWeapon=4000, FlakWeaponPercentage=2, Chaingun=4000, ChaingunPercentage=2, CannonHE_Medium=4000, Grenade=4000, GrenadeFriendlyFire=2000, GrenadePercentage=2, TankDestroyerCannon=4000, TankDestroyerCannonPercentage=2, ArrowWeapon=4000, ArrowWeaponPercentage=2, Effect=0 | → collapse to FlakWeapon

### FlakWeapon, HeavyBomb, LightChemicalWeapon, MediumMissile, TankDestroyerCannon (1 weapons)
- `ixian_airdrone` (weapons\d2k.yaml) | dominant: TankDestroyerCannon(2000) | TankDestroyerCannon=2000, TankDestroyerCannonPercentage=1, LightChemicalWeapon=2000, LightChemicalWeaponPercentage=1, FlakWeapon=2000, FlakWeaponPercentage=1, MediumMissile=2000, MediumMissilePercentage=1, HeavyBomb=2000, HeavyBombPercentage=1, MissileAP_Heavy=2000, Shrapnel=0 | → collapse to TankDestroyerCannon

### FlakWeapon, MagicWeapon, MediumMissile, ShrapnelWeapon, TeslaChargedWeapon (1 weapons)
- `Future_MultiMissile_Sigma` (ContentPacks\RedAlert2Mod\FutureTech\yaml\weapons.yaml) | dominant: EMPUnit(25000) | TeslaChargedWeapon=10000, TeslaChargedExtraDamage=5000, TeslaChargedWeaponPercentage=5, MagicWeapon=10000, MagicWeaponPercentage=5, FlakWeapon=10000, FlakWeaponPercentage=5, ShrapnelWeapon=10000, ShrapnelWeaponFriendlyFire=5000, ShrapnelWeaponPercentage=5, MediumMissile=10000, MediumMissilePercentage=5, EMPUnit=25000, Effect=0 | → collapse to EMPUnit

### Grenade, HeavyBomb, LightFlameWeapon, MediumChemicalWeapon, ShrapnelWeapon (1 weapons)
- `RA2MortarBike` (ContentPacks\RedAlert2Mod\Syndicate\yaml\weapons.yaml) | dominant: Grenade(6000) | Grenade=6000, GrenadeFriendlyFire=3000, GrenadePercentage=3, ShrapnelWeapon=6000, ShrapnelWeaponFriendlyFire=3000, ShrapnelWeaponPercentage=3, HeavyBomb=6000, HeavyBombPercentage=3, LightFlameWeapon=6000, LightFlameWeaponPercentage=3, MediumChemicalWeapon=6000, MediumChemicalWeaponPercentage=3, CannonHE_Heavy=6000, Effect=0 | → collapse to Grenade

### Grenade, HeavyBomb, MediumChemicalWeapon, MediumFlameWeapon, ShrapnelWeapon (1 weapons)
- `eye_bomberguy` (ContentPacks\D2k\Ordos\yaml\weapons.yaml) | dominant: Grenade(20000) | Grenade=20000, GrenadeFriendlyFire=10000, GrenadePercentage=10, MediumChemicalWeapon=20000, MediumChemicalWeaponPercentage=10, MediumFlameWeapon=20000, MediumFlameWeaponPercentage=10, ShrapnelWeapon=20000, ShrapnelWeaponFriendlyFire=10000, ShrapnelWeaponPercentage=10, HeavyBomb=20000, HeavyBombPercentage=10, Effect=0, Concrete=1875 | → collapse to Grenade

### Grenade, HeavyBomb, MediumFlameWeapon, NuclearWarhead, ShrapnelWeapon (1 weapons)
- `NaxiMeteor` (weapons\redalert2mod.yaml) | dominant: Grenade(20000) | Grenade=20000, GrenadeFriendlyFire=10000, GrenadePercentage=5, MediumFlameWeapon=20000, MediumFlameWeaponPercentage=5, ShrapnelWeapon=20000, ShrapnelWeaponFriendlyFire=10000, ShrapnelWeaponPercentage=5, HeavyBomb=20000, HeavyBombPercentage=5, NuclearWarhead=20000, NuclearWarheadPercentage=5, Effect=0, MeteorFlameFragment=0 | → collapse to Grenade

### Grenade, MediumFlameWeapon, MediumMissile, ShrapnelWeapon, TeslaWeapon (1 weapons)
- `KamovMissilesTesla` (ContentPacks\RedAlert\Soviets\yaml\weapons.yaml) | dominant: EMPUnit(5000) | Grenade=2000, GrenadeFriendlyFire=1000, GrenadePercentage=1, ShrapnelWeapon=2000, ShrapnelWeaponFriendlyFire=1000, ShrapnelWeaponPercentage=1, MediumFlameWeapon=2000, MediumFlameWeaponPercentage=1, TeslaWeapon=2000, TeslaExtraDamage=1000, TeslaWeaponPercentage=1, MediumMissile=2000, MediumMissilePercentage=1, EMPUnit=5000, TeslaArc=0 | → collapse to EMPUnit

### HeavyBomb, HeavyCannon, MediumCannon, MediumFlameWeapon, ShrapnelWeapon (1 weapons)
- `ArcherArtilleryShell` (ContentPacks\TiberianDawn\GDI\yaml\weapons.yaml) | dominant: MediumCannon(14000) | MediumCannon=14000, MediumCannonPercentage=7, HeavyCannon=14000, HeavyCannonPercentage=7, MediumFlameWeapon=14000, MediumFlameWeaponPercentage=7, ShrapnelWeapon=14000, ShrapnelWeaponFriendlyFire=7000, ShrapnelWeaponPercentage=7, HeavyBomb=14000, HeavyBombPercentage=7 | → collapse to MediumCannon

### HeavyBomb, HeavyChemicalWeapon, LightChemicalWeapon, MediumChemicalWeapon, ShrapnelWeapon (1 weapons)
- `AsianChemical` (ContentPacks\RedAlert2Mod\AsianAlliance\yaml\weapons.yaml) | dominant: Demolition_Light(4000) | Demolition_Light=4000, ShrapnelWeapon=4000, ShrapnelWeaponFriendlyFire=2000, ShrapnelWeaponPercentage=2, HeavyBomb=4000, HeavyBombPercentage=2, LightChemicalWeapon=4000, LightChemicalWeaponPercentage=2, MediumChemicalWeapon=4000, MediumChemicalWeaponPercentage=2, HeavyChemicalWeapon=4000, HeavyChemicalWeaponPercentage=2, Effect=0, Cloud=0 | → collapse to Demolition_Light

### HeavyBomb, HeavyFlameWeapon, LightChemicalWeapon, ShrapnelWeapon, TankDestroyerCannon (1 weapons)
- `NaxMausCannon` (ContentPacks\RedAlert2Mod\Naxis\yaml\weapons.yaml) | dominant: LightChemicalWeapon(10000) | LightChemicalWeapon=10000, LightChemicalWeaponPercentage=5, HeavyFlameWeapon=10000, HeavyFlameWeaponPercentage=5, ShrapnelWeapon=10000, ShrapnelWeaponFriendlyFire=5000, ShrapnelWeaponPercentage=5, HeavyBomb=10000, HeavyBombPercentage=5, TankDestroyerCannon=10000, TankDestroyerCannonPercentage=5, CannonHE_Medium=10000 | → collapse to LightChemicalWeapon

### HeavyChemicalWeapon, HeavyFlameWeapon, LaserWeapon, RailgunWeapon, TeslaWeapon (1 weapons)
- `SteelQuantumTurretRail` (ContentPacks\RedAlert2Mod\Consortium\yaml\weapons.yaml) | dominant: TeslaWeapon(2000) | TeslaWeapon=2000, TeslaExtraDamage=1000, TeslaWeaponPercentage=1, HeavyChemicalWeapon=2000, HeavyChemicalWeaponPercentage=1, HeavyFlameWeapon=2000, HeavyFlameWeaponPercentage=1, LaserWeapon=2000, LaserWeaponPercentage=1, RailgunWeapon=2000, RailgunWeaponPercentage=1, CannonHE_Heavy=2000, Effect=0 | → collapse to TeslaWeapon

### MagicWeapon, MediumChemicalWeapon, MediumFlameWeapon, ShrapnelWeapon, TeslaWeapon (1 weapons)
- `FutureTankCannons` (ContentPacks\RedAlert2Mod\FutureTech\yaml\weapons.yaml) | dominant: MediumChemicalWeapon(100000) | MediumChemicalWeapon=100000, MediumChemicalWeaponPercentage=50, MediumFlameWeapon=100000, MediumFlameWeaponPercentage=50, ShrapnelWeapon=100000, ShrapnelWeaponFriendlyFire=50000, ShrapnelWeaponPercentage=50, TeslaWeapon=100000, TeslaExtraDamage=50000, TeslaWeaponPercentage=50, MagicWeapon=100000, MagicWeaponPercentage=50, CannonHE_Heavy=100000, EMPUnit=100000, Effect=0 | → collapse to MediumChemicalWeapon

### MediumChemicalWeapon, MediumFlameWeapon, MediumMissile, ShrapnelWeapon, TeslaWeapon (1 weapons)
- `JapanMaidenBowEnergized` (ContentPacks\RedAlert\Japan\yaml\weapons.yaml) | dominant: EMPUnit(14000) | Arrow_Light=6000, MediumMissile=6000, MediumMissilePercentage=3, CannonHE_Medium=6000, CannonHE_Medium_Percentage=3, MediumChemicalWeapon=6000, MediumChemicalWeaponPercentage=3, MediumFlameWeapon=6000, MediumFlameWeaponPercentage=3, ShrapnelWeapon=6000, ShrapnelWeaponFriendlyFire=3000, ShrapnelWeaponPercentage=3, TeslaWeapon=6000, TeslaExtraDamage=3000, TeslaWeaponPercentage=3, EMPUnit=14000, Effect=0, EffectAir=0 | → collapse to EMPUnit

### FlakWeapon, Grenade, SmallArms, TankDestroyerCannon (2 weapons)
- `PositronGrenade` (ContentPacks\StarCraft\Protoss\yaml\weapons.yaml) | dominant: SmallArms(8000) | SmallArms=8000, SmallArmsPercentage=1, FlakWeapon=8000, FlakWeaponPercentage=1, Grenade=8000, GrenadeFriendlyFire=4000, GrenadePercentage=1, CannonHE_Medium=8000, TankDestroyerCannon=8000, TankDestroyerCannonPercentage=1, Effect=0, shrapnel=0 | → collapse to SmallArms
- `VultureGrenade` (ContentPacks\StarCraft\Terran\yaml\weapons.yaml) | dominant: SmallArms(4000) | SmallArms=4000, SmallArmsPercentage=2, FlakWeapon=4000, FlakWeaponPercentage=2, Grenade=4000, GrenadeFriendlyFire=2000, GrenadePercentage=2, CannonHE_Medium=4000, TankDestroyerCannon=4000, TankDestroyerCannonPercentage=2, Effect=0 | → collapse to SmallArms

### Grenade, HeavyBomb, MediumFlameWeapon, ShrapnelWeapon (2 weapons)
- `D2K_155mm2` (ContentPacks\D2k\Ixian\yaml\weapons.yaml) | dominant: Grenade(12000) | Grenade=12000, GrenadeFriendlyFire=6000, GrenadePercentage=6, MediumFlameWeapon=12000, MediumFlameWeaponPercentage=6, ShrapnelWeapon=12000, ShrapnelWeaponFriendlyFire=6000, ShrapnelWeaponPercentage=6, HeavyBomb=12000, HeavyBombPercentage=6, Effect=0, Glow=0 | → collapse to Grenade
- `wc2catapultFire` (ContentPacks\Warcraft2\Orcs\yaml\weapons.yaml) | dominant: Grenade(30000) | Grenade=30000, GrenadeFriendlyFire=15000, GrenadePercentage=15, MediumFlameWeapon=30000, MediumFlameWeaponPercentage=15, ShrapnelWeapon=30000, ShrapnelWeaponFriendlyFire=15000, ShrapnelWeaponPercentage=15, HeavyBomb=30000, HeavyBombPercentage=15, Effect=0 | → collapse to Grenade

### Grenade, HeavyMissile, LightFlameWeapon, TeslaWeapon (2 weapons)
- `MammothTuskTesla` (ContentPacks\RedAlert\Soviets\yaml\weapons.yaml) | dominant: EMPUnit(16000) | Grenade=8000, GrenadeFriendlyFire=4000, GrenadePercentage=4, LightFlameWeapon=8000, LightFlameWeaponPercentage=4, TeslaWeapon=8000, TeslaExtraDamage=4000, TeslaWeaponPercentage=4, HeavyMissile=8000, HeavyMissilePercentage=4, EMPUnit=16000, TeslaArc=0 | → collapse to EMPUnit
- `MonsterTankTuskTesla` (ContentPacks\RedAlert\Soviets\yaml\weapons.yaml) | dominant: EMPUnit(53250) | Grenade=26750, GrenadeFriendlyFire=13250, GrenadePercentage=13, LightFlameWeapon=26750, LightFlameWeaponPercentage=13, TeslaWeapon=26750, TeslaExtraDamage=13250, TeslaWeaponPercentage=13, HeavyMissile=26750, HeavyMissilePercentage=13, EMPUnit=53250, TeslaArc=0 | → collapse to EMPUnit

### HeavyBomb, HeavyChemicalWeapon, HeavyFlameWeapon, NuclearWarhead (2 weapons)
- `ParaBombNuke` (ContentPacks\RedAlert\Soviets\yaml\weapons.yaml) | dominant: HeavyBomb(100000) | HeavyBomb=100000, HeavyBombPercentage=50, HeavyChemicalWeapon=100000, HeavyChemicalWeaponPercentage=50, HeavyFlameWeapon=100000, HeavyFlameWeaponPercentage=50, NuclearWarhead=100000, NuclearWarheadPercentage=50, Effect=0 | → collapse to HeavyBomb
- `YakNuclearBomb` (ContentPacks\RedAlert\Soviets\yaml\weapons.yaml) | dominant: HeavyBomb(50000) | HeavyBomb=50000, HeavyBombPercentage=25, HeavyChemicalWeapon=50000, HeavyChemicalWeaponPercentage=25, HeavyFlameWeapon=50000, HeavyFlameWeaponPercentage=25, NuclearWarhead=50000, NuclearWarheadPercentage=25, Effect=0 | → collapse to HeavyBomb

### LaserWeapon, MediumChemicalWeapon, MediumFlameWeapon, TankDestroyerCannon (2 weapons)
- `TSCABALEnlightedLaser` (ContentPacks\TiberianSun\CABAL\yaml\weapons.yaml) | dominant: TankDestroyerCannon(70000) | TankDestroyerCannon=70000, TankDestroyerCannonPercentage=35, LaserWeapon=70000, LaserWeaponPercentage=35, MediumFlameWeapon=70000, MediumFlameWeaponPercentage=35, MediumChemicalWeapon=70000, MediumChemicalWeaponPercentage=35, Effect=0 | → collapse to TankDestroyerCannon
- `TSCABALObeliskLaserFire` (ContentPacks\TiberianSun\CABAL\yaml\weapons.yaml) | dominant: TankDestroyerCannon(70000) | TankDestroyerCannon=70000, TankDestroyerCannonPercentage=35, LaserWeapon=70000, LaserWeaponPercentage=35, MediumFlameWeapon=70000, MediumFlameWeaponPercentage=35, MediumChemicalWeapon=70000, MediumChemicalWeaponPercentage=35, Effect=0 | → collapse to TankDestroyerCannon

### ArrowWeapon, Grenade, LightChemicalWeapon, LightFlameWeapon (1 weapons)
- `BallistaMultiShot` (ContentPacks\RedAlert\Japan\yaml\weapons.yaml) | dominant: Grenade(10000) | Grenade=10000, GrenadeFriendlyFire=5000, GrenadePercentage=5, LightFlameWeapon=10000, LightFlameWeaponPercentage=5, LightChemicalWeapon=10000, LightChemicalWeaponPercentage=5, ArrowWeapon=10000, ArrowWeaponPercentage=5, Effect=0 | → collapse to Grenade

### ArrowWeapon, MediumChemicalWeapon, MediumFlameWeapon, TeslaWeapon (1 weapons)
- `BallistaMultiShotEnergized` (ContentPacks\RedAlert\Japan\yaml\weapons.yaml) | dominant: TeslaWeapon(10000) | TeslaWeapon=10000, TeslaExtraDamage=5000, TeslaWeaponPercentage=5, MediumChemicalWeapon=10000, MediumChemicalWeaponPercentage=5, MediumFlameWeapon=10000, MediumFlameWeaponPercentage=5, ArrowWeapon=10000, ArrowWeaponPercentage=5, Effect=0 | → collapse to TeslaWeapon

### Chaingun, FlakWeapon, HeavyAAWeapon, MediumMissile (1 weapons)
- `autogun_tank` (ContentPacks\D2k\Ordos\yaml\weapons.yaml) | dominant: MissileAP_Heavy(2000) | MissileAP_Heavy=2000, CannonHE_Heavy=2000, HeavyAAWeapon=2000, HeavyAAWeaponPercentage=1, FlakWeapon=2000, FlakWeaponPercentage=1, MediumMissile=2000, MediumMissilePercentage=1, Chaingun=2000, ChaingunPercentage=1, Effect=0 | → collapse to MissileAP_Heavy

### Chaingun, FlakWeapon, LightMissile, SmallArms (1 weapons)
- `HeavyAATankCannonAG` (ContentPacks\RedAlert\Allies\yaml\weapons.yaml) | dominant: LightMissile(2000) | LightMissile=2000, LightMissilePercentage=1, FlakWeapon=2000, FlakWeaponPercentage=1, SmallArms=2000, SmallArmsPercentage=1, Chaingun=2000, ChaingunPercentage=1 | → collapse to LightMissile

### FlakWeapon, HeavyCannon, MediumCannon, TankDestroyerCannon (1 weapons)
- `PhotonCannon` (ContentPacks\StarCraft\Protoss\yaml\weapons.yaml) | dominant: FlakWeapon(10000) | FlakWeapon=10000, FlakWeaponPercentage=5, HeavyCannon=10000, HeavyCannonPercentage=5, MediumCannon=10000, MediumCannonPercentage=5, TankDestroyerCannon=10000, TankDestroyerCannonPercentage=5, Effect=0 | → collapse to FlakWeapon

### FlakWeapon, LightMissile, ShrapnelWeapon, TankDestroyerCannon (1 weapons)
- `BikeRockets` (ContentPacks\TiberianDawn\Nod\yaml\weapons.yaml) | dominant: TankDestroyerCannon(4000) | TankDestroyerCannon=4000, TankDestroyerCannonPercentage=2, ShrapnelWeapon=4000, ShrapnelWeaponFriendlyFire=2000, ShrapnelWeaponPercentage=2, FlakWeapon=4000, FlakWeaponPercentage=2, LightMissile=4000, LightMissilePercentage=2 | → collapse to TankDestroyerCannon

### FlakWeapon, MagicWeapon, MediumMissile, TeslaWeapon (1 weapons)
- `AsianPhotonCannon` (ContentPacks\RedAlert2Mod\AsianAlliance\yaml\weapons.yaml) | dominant: EMPUnit(10000) | MediumMissile=2000, MediumMissilePercentage=1, FlakWeapon=2000, FlakWeaponPercentage=1, MagicWeapon=2000, MagicWeaponPercentage=1, TeslaWeapon=2000, TeslaExtraDamage=1000, TeslaWeaponPercentage=1, EMPUnit=10000, Effect=0, Smudge=0 | → collapse to EMPUnit

### Grenade, HeavyBomb, MediumFlameWeapon, TankDestroyerCannon (1 weapons)
- `DuelistTankCannon` (ContentPacks\D2k\Ixian\yaml\weapons.yaml) | dominant: MediumFlameWeapon(14000) | MediumFlameWeapon=14000, MediumFlameWeaponPercentage=7, Grenade=14000, GrenadeFriendlyFire=7000, GrenadePercentage=7, HeavyBomb=14000, HeavyBombPercentage=7, TankDestroyerCannon=14000, TankDestroyerCannonPercentage=7, CannonHE_Heavy=14000, CannonHE_Medium=14000, Effect=0 | → collapse to MediumFlameWeapon

### Grenade, LightFlameWeapon, MediumMissile, ShrapnelWeapon (1 weapons)
- `HueyFireMissiles` (ContentPacks\RedAlert2Mod\TKM\yaml\weapons.yaml) | dominant: LightMissile(6000) | Grenade=2000, GrenadeFriendlyFire=1000, GrenadePercentage=1, ShrapnelWeapon=2000, ShrapnelWeaponFriendlyFire=1000, ShrapnelWeaponPercentage=1, CannonHE_Heavy=2000, MediumMissile=2000, MediumMissilePercentage=0, LightFlameWeaponPercentage=2, LightMissile=6000, LightMissilePercentage=2, Smudge=0, Effect=0, EffectAir=0 | → collapse to LightMissile

### Grenade, MediumFlameWeapon, MediumMissile, ShrapnelWeapon (1 weapons)
- `HindMissilesThermobaric` (ContentPacks\RedAlert\Soviets\yaml\weapons.yaml) | dominant: Grenade(2000) | Grenade=2000, GrenadeFriendlyFire=1000, GrenadePercentage=1, ShrapnelWeapon=2000, ShrapnelWeaponFriendlyFire=1000, ShrapnelWeaponPercentage=1, MediumFlameWeapon=2000, MediumFlameWeaponPercentage=1, CannonHE_Heavy=2000, MediumMissile=2000, MediumMissilePercentage=1, Effect=0 | → collapse to Grenade

### Grenade, ShrapnelWeapon, SmallArms, TankDestroyerCannon (1 weapons)
- `ShotgunAttackRobotGun` (ContentPacks\RedAlert2Mod\FutureTech\yaml\weapons.yaml) | dominant: Grenade(2000) | Grenade=2000, GrenadeFriendlyFire=1000, GrenadePercentage=1, ShrapnelWeapon=2000, ShrapnelWeaponFriendlyFire=1000, ShrapnelWeaponPercentage=1, TankDestroyerCannon=2000, TankDestroyerCannonPercentage=1, CannonHE_Medium=2000, SmallArms=2000, SmallArmsPercentage=1, Bullet_Medium=2000 | → collapse to Grenade

### HeavyBomb, HeavyChemicalWeapon, HeavyFlameWeapon, TeslaWeapon (1 weapons)
- `YakTeslaBomb` (ContentPacks\RedAlert\Soviets\yaml\weapons.yaml) | dominant: TeslaWeapon(40000) | TeslaWeapon=40000, TeslaExtraDamage=20000, TeslaWeaponPercentage=20, HeavyBomb=40000, HeavyBombPercentage=20, HeavyChemicalWeapon=40000, HeavyChemicalWeaponPercentage=20, HeavyFlameWeapon=40000, HeavyFlameWeaponPercentage=20, EMPUnit=40000, TeslaArc=0 | → collapse to TeslaWeapon

### HeavyBomb, HeavyChemicalWeapon, HeavyMissile, RailgunWeapon (1 weapons)
- `KodiakCannon` (ContentPacks\TiberianSun\GDI\yaml\weapons.yaml) | dominant: HeavyChemicalWeapon(8000) | HeavyChemicalWeapon=8000, HeavyChemicalWeaponPercentage=4, RailgunWeapon=8000, RailgunWeaponPercentage=4, HeavyMissile=8000, HeavyMissilePercentage=4, CannonHE_Heavy=8000, HeavyBomb=8000, HeavyBombPercentage=4 | → collapse to HeavyChemicalWeapon

### HeavyBomb, LightChemicalWeapon, MediumFlameWeapon, ShrapnelWeapon (1 weapons)
- `RA2GrandCannonWeapon` (ContentPacks\RedAlert2\Allies\yaml\weapons.yaml) | dominant: CannonHE_Heavy(50000) | CannonHE_Heavy=50000, HeavyBomb=50000, HeavyBombPercentage=25, MediumFlameWeapon=50000, MediumFlameWeaponPercentage=25, ShrapnelWeapon=50000, ShrapnelWeaponFriendlyFire=25000, ShrapnelWeaponPercentage=25, LightChemicalWeapon=50000, LightChemicalWeaponPercentage=25, Demolition_Light=50000, EffectWater=0 | → collapse to CannonHE_Heavy

### HeavyBomb, LightMissile, MediumFlameWeapon, TeslaChargedWeapon (1 weapons)
- `wc2gryphonFireVisible` (ContentPacks\Warcraft2\Humans\yaml\weapons.yaml) | dominant: LightMissile(10000) | LightMissile=10000, LightMissilePercentage=5, MediumFlameWeapon=10000, MediumFlameWeaponPercentage=5, HeavyBomb=10000, HeavyBombPercentage=5, TeslaChargedWeapon=10000, TeslaChargedExtraDamage=5000, TeslaChargedWeaponPercentage=5, EMPUnit=10000, Effect=0, Effect2=0 | → collapse to LightMissile

### HeavyBomb, MediumFlameWeapon, NuclearWarhead, ShrapnelWeapon (1 weapons)
- `FirehawkBomb` (ContentPacks\TiberianDawn\GDI\yaml\weapons.yaml) | dominant: HeavyBomb(10000) | HeavyBomb=10000, HeavyBombPercentage=5, NuclearWarhead=10000, NuclearWarheadPercentage=5, ShrapnelWeapon=10000, ShrapnelWeaponFriendlyFire=5000, ShrapnelWeaponPercentage=5, MediumFlameWeapon=10000, MediumFlameWeaponPercentage=5 | → collapse to HeavyBomb

### HeavyBomb, MediumFlameWeapon, RailgunWeapon, ShrapnelWeapon (1 weapons)
- `LatinSmokerCannon` (ContentPacks\RedAlert2Mod\Syndicate\yaml\weapons.yaml) | dominant: RailgunWeapon(2000) | RailgunWeapon=2000, RailgunWeaponPercentage=1, MediumFlameWeapon=2000, MediumFlameWeaponPercentage=1, HeavyBomb=2000, HeavyBombPercentage=1, ShrapnelWeapon=2000, ShrapnelWeaponFriendlyFire=1000, ShrapnelWeaponPercentage=1, CannonHE_Heavy=2000, CannonHE_Medium=2000 | → collapse to RailgunWeapon

### HeavyCannon, HeavyChemicalWeapon, RailgunWeapon, TeslaWeapon (1 weapons)
- `OISmallPlasmaCannon` (ContentPacks\RedAlert\Japan\yaml\weapons.yaml) | dominant: HeavyChemicalWeapon(2000) | HeavyChemicalWeapon=2000, HeavyChemicalWeaponPercentage=1, TeslaWeapon=2000, TeslaExtraDamage=1000, TeslaWeaponPercentage=1, RailgunWeapon=2000, RailgunWeaponPercentage=1, HeavyCannon=2000, HeavyCannonPercentage=1, Effect=0 | → collapse to HeavyChemicalWeapon

### HeavyCannon, LaserWeapon, RailgunWeapon, TeslaWeapon (1 weapons)
- `CabalBeholderLaser` (ContentPacks\TiberianSun\CABAL\yaml\weapons.yaml) | dominant: HeavyCannon(10000) | HeavyCannon=10000, HeavyCannonPercentage=5, TeslaWeapon=10000, TeslaExtraDamage=5000, TeslaWeaponPercentage=5, RailgunWeapon=10000, RailgunWeaponPercentage=5, LaserWeapon=10000, LaserWeaponPercentage=5, Effect=0 | → collapse to HeavyCannon

### HeavyChemicalWeapon, HeavyFlameWeapon, MediumChemicalWeapon, MediumFlameWeapon (1 weapons)
- `MutHFlamerChem` (ContentPacks\TiberianSun\Forgotten\yaml\weapons.yaml) | dominant: MediumFlameWeapon(20000) | MediumFlameWeapon=20000, MediumFlameWeaponPercentage=10, HeavyFlameWeapon=20000, HeavyFlameWeaponPercentage=10, MediumChemicalWeapon=10000, MediumChemicalWeaponPercentage=5, HeavyChemicalWeapon=10000, HeavyChemicalWeaponPercentage=5, Cloud=0 | → collapse to MediumFlameWeapon

### HeavyFlameWeapon, LightFlameWeapon, MediumChemicalWeapon, ShrapnelWeapon (1 weapons)
- `GladiusCannon` (ContentPacks\StarCraft\Protoss\yaml\weapons.yaml) | dominant: LightFlameWeapon(10000) | LightFlameWeapon=10000, LightFlameWeaponPercentage=5, MediumChemicalWeapon=10000, MediumChemicalWeaponPercentage=5, HeavyFlameWeapon=10000, HeavyFlameWeaponPercentage=5, ShrapnelWeapon=10000, ShrapnelWeaponFriendlyFire=5000, ShrapnelWeaponPercentage=5, FlakWeapon=10000, FlakWeaponPercentage=5, CannonHE_Heavy=10000, CannonHE_Heavy_Percentage=5, CannonHE_Medium=10000, CannonHE_Medium_Percentage=5, CannonAP_Light=10000, CannonAP_Light_Percentage=5, Effect=0 | → collapse to LightFlameWeapon

### LaserWeapon, MagicWeapon, RailgunWeapon, TeslaChargedWeapon (1 weapons)
- `bfg10kCannon` (ContentPacks\RedAlert2Mod\Consortium\yaml\weapons.yaml) | dominant: TeslaChargedWeapon(250000) | TeslaChargedWeapon=250000, TeslaChargedExtraDamage=125000, TeslaChargedWeaponPercentage=125, MagicWeapon=250000, MagicWeaponPercentage=125, LaserWeapon=250000, LaserWeaponPercentage=125, RailgunWeapon=250000, RailgunWeaponPercentage=125, Effect=0 | → collapse to TeslaChargedWeapon

### MediumFlameWeapon, MediumMissile, NuclearWarhead, TankDestroyerCannon (1 weapons)
- `HindMissilesNuclear` (ContentPacks\RedAlert\Soviets\yaml\weapons.yaml) | dominant: TankDestroyerCannon(2000) | TankDestroyerCannon=2000, TankDestroyerCannonPercentage=1, MediumFlameWeapon=2000, MediumFlameWeaponPercentage=1, NuclearWarhead=2000, NuclearWarheadPercentage=1, MediumMissile=2000, MediumMissilePercentage=1, Effect=0 | → collapse to TankDestroyerCannon

### FlakWeapon, LaserWeapon, LightFlameWeapon (2 weapons)
- `MedicFlare` (ContentPacks\StarCraft\Terran\yaml\weapons.yaml) | dominant: LightFlameWeapon(2000) | LightFlameWeapon=2000, LightFlameWeaponPercentage=1, MediumChemicalWeapon=2000, MediumChemicalWeaponPercentage=1, LaserWeapon=2000, LaserWeaponPercentage=1, Condition=0 | → collapse to LightFlameWeapon
- `ShtoraLaser` (ContentPacks\RedAlert\Soviets\yaml\weapons.yaml) | dominant: LightFlameWeapon(2000) | LightFlameWeapon=2000, LightFlameWeaponPercentage=1, MediumChemicalWeapon=2000, MediumChemicalWeaponPercentage=1, LaserWeapon=2000, LaserWeaponPercentage=1, Condition=0 | → collapse to LightFlameWeapon

### HeavyChemicalWeapon, LightChemicalWeapon, MediumChemicalWeapon (2 weapons)
- `RA2120xmm_rad` (ContentPacks\RedAlert2\Soviets\yaml\weapons.yaml) | dominant: LightChemicalWeapon(2000) | LightChemicalWeapon=2000, LightChemicalWeaponPercentage=1, MediumChemicalWeapon=2000, MediumChemicalWeaponPercentage=1, HeavyChemicalWeapon=2000, HeavyChemicalWeaponPercentage=1, CannonAP_Light=2000, CannonAP_Light_Percentage=1, ShrapnelWeapon=2000, ShrapnelWeaponFriendlyFire=1000, ShrapnelWeaponPercentage=1, CannonHE_Heavy=2000 | → collapse to LightChemicalWeapon
- `RA2SCUD_rad` (ContentPacks\RedAlert2\Shared\yaml\weapons.yaml) | dominant: LightChemicalWeapon(18000) | LightChemicalWeapon=18000, LightChemicalWeaponPercentage=9, MediumChemicalWeapon=18000, MediumChemicalWeaponPercentage=9, HeavyChemicalWeapon=18000, HeavyChemicalWeaponPercentage=9, MediumFlameWeaponPercentage=9, Demolition_Light=18000, MissileAP_Heavy=18000, RA2SCUDMissileAP_Heavy_NoWall=18000, Radiation=0 | → collapse to LightChemicalWeapon

### Chaingun, FlakWeapon, SmallArms (1 weapons)
- `NaxiInterceptorGun` (ContentPacks\RedAlert2Mod\Naxis\yaml\weapons.yaml) | dominant: CannonHE_Heavy(6000) | CannonHE_Heavy=6000, FlakWeapon=6000, FlakWeaponPercentage=3, Chaingun=6000, ChaingunPercentage=3, SmallArms=6000, SmallArmsPercentage=3 | → collapse to CannonHE_Heavy

### Chaingun, Grenade, SmallArms (1 weapons)
- `RA2FlakTrackGun` (ContentPacks\RedAlert2\Shared\yaml\weapons.yaml) | dominant: SmallArms(2000) | SmallArms=2000, SmallArmsPercentage=1, Grenade=2000, GrenadeFriendlyFire=1000, GrenadePercentage=1, Chaingun=2000, ChaingunPercentage=1, Flak_Medium=2000 | → collapse to SmallArms

### Chaingun, LaserWeapon, TeslaWeapon (1 weapons)
- `Rammax_Sabot` (ContentPacks\RedAlert2Mod\Syndicate\yaml\weapons.yaml) | dominant: LaserWeapon(10000) | Chaingun=2000, ChaingunPercentage=5, LaserWeapon=10000, LaserWeaponPercentage=5 | → collapse to LaserWeapon

### Chaingun, LightFlameWeapon, ShrapnelWeapon (1 weapons)
- `SCTyr` (ContentPacks\StarCraft\Terran\yaml\weapons.yaml) | dominant: LightFlameWeapon(4000) | LightFlameWeapon=4000, LightFlameWeaponPercentage=2, Chaingun=4000, ChaingunPercentage=2, ShrapnelWeapon=4000, ShrapnelWeaponFriendlyFire=2000, ShrapnelWeaponPercentage=2, CannonHE_Heavy=4000, Effect=0 | → collapse to LightFlameWeapon

### Chaingun, RailgunWeapon, SwordWeapon (1 weapons)
- `Tentacle` (ContentPacks\StarCraft\Zerg\yaml\weapons.yaml) | dominant: RailgunWeapon(6000) | RailgunWeapon=6000, RailgunWeaponPercentage=3, Chaingun=6000, ChaingunPercentage=3, CannonHE_Heavy=6000, SwordWeapon=6000, SwordWeaponFriendlyFire=3000, SwordWeaponPercentage=3, Effect=0 | → collapse to RailgunWeapon

### FlakWeapon, Grenade, MediumMissile (1 weapons)
- `AtreusMG` (ContentPacks\StarCraft\Protoss\yaml\weapons.yaml) | dominant: Grenade(2000) | Grenade=2000, GrenadeFriendlyFire=1000, GrenadePercentage=1, CannonHE_Heavy=2000, MediumMissile=2000, MediumMissilePercentage=1, FlakWeapon=2000, FlakWeaponPercentage=1, Bullet_Medium=2000, Effect=0 | → collapse to Grenade

### FlakWeapon, LightFlameWeapon, TankDestroyerCannon (1 weapons)
- `RA2PsychicJab` (ContentPacks\RedAlert2\Yuri\yaml\weapons.yaml) | dominant: TankDestroyerCannon(2000) | TankDestroyerCannon=2000, TankDestroyerCannonPercentage=1, CannonHE_Medium=2000, LightFlameWeapon=2000, LightFlameWeaponPercentage=1, FlakWeapon=2000, FlakWeaponPercentage=1 | → collapse to TankDestroyerCannon

### FlakWeapon, LightMissile, SmallArms (1 weapons)
- `ra2roktgun` (ContentPacks\RedAlert2\Allies\yaml\weapons.yaml) | dominant: LightMissile(2000) | LightMissile=2000, LightMissilePercentage=1, SmallArms=2000, SmallArmsPercentage=1, FlakWeapon=2000, FlakWeaponPercentage=1, Bullet_Medium=2000 | → collapse to LightMissile

### FlakWeapon, MediumMissile, RailgunWeapon (1 weapons)
- `TS30mmRail` (ContentPacks\TiberianSun\GDI\yaml\weapons.yaml) | dominant: MediumMissile(2000) | MediumMissile=2000, MediumMissilePercentage=1, FlakWeapon=2000, FlakWeaponPercentage=1, RailgunWeapon=2000, RailgunWeaponPercentage=1 | → collapse to MediumMissile

### FlakWeapon, RailgunWeapon, TankDestroyerCannon (1 weapons)
- `IxRailgunDroneBullet` (ContentPacks\D2k\Ixian\yaml\weapons.yaml) | dominant: FlakWeapon(6000) | FlakWeapon=6000, FlakWeaponPercentage=3, TankDestroyerCannon=6000, TankDestroyerCannonPercentage=3, RailgunWeapon=6000, RailgunWeaponPercentage=3, CannonHE_Medium=6000 | → collapse to FlakWeapon

### Grenade, HeavyFlameWeapon, MediumMissile (1 weapons)
- `v1rocketsThermobaric` (ContentPacks\RedAlert\Soviets\yaml\weapons.yaml) | dominant: MediumMissile(8000) | MediumMissile=8000, MediumMissilePercentage=4, Grenade=8000, GrenadeFriendlyFire=4000, GrenadePercentage=4, HeavyFlameWeapon=8000, HeavyFlameWeaponPercentage=4 | → collapse to MediumMissile

### HeavyAAWeapon, HeavyMissile, LaserWeapon (1 weapons)
- `wc2highArrowFire` (weapons\warcraft2.yaml) | dominant: HeavyMissile(4000) | LaserWeaponPercentage=2, HeavyMissile=4000, HeavyMissilePercentage=2, HeavyAAWeapon=4000, HeavyAAWeaponPercentage=2 | → collapse to HeavyMissile

### HeavyAAWeapon, LightMissile, TankDestroyerCannon (1 weapons)
- `RA2LarsRocket` (ContentPacks\RedAlert2Mod\Syndicate\yaml\weapons.yaml) | dominant: MissileAP_Heavy(2000) | MissileAP_Heavy=2000, HeavyAAWeapon=2000, HeavyAAWeaponPercentage=1, LightMissile=2000, LightMissilePercentage=1, TankDestroyerCannon=2000, TankDestroyerCannonPercentage=1, MissileAP_Medium=2000 | → collapse to MissileAP_Heavy

### HeavyBomb, HeavyCannon, TeslaWeapon (1 weapons)
- `HovercraftPlasmaCannon` (ContentPacks\RedAlert\Japan\yaml\weapons.yaml) | dominant: Bullet_Light(2000) | Bullet_Light=2000, Bullet_Light_Percentage=1, Bullet_Medium=2000, Bullet_Medium_Percentage=1, Grenade=2000, GrenadeFriendlyFire=1000, GrenadePercentage=1, ShrapnelWeapon=2000, ShrapnelWeaponFriendlyFire=1000, ShrapnelWeaponPercentage=1, CannonAP_Light=2000, CannonAP_Light_Percentage=1, CannonHE_Medium=2000, CannonHE_Medium_Percentage=1, TeslaWeapon=2000, TeslaExtraDamage=1000, TeslaWeaponPercentage=1, HeavyBomb=2000, HeavyBombPercentage=1, HeavyCannon=2000, HeavyCannonPercentage=1, Effect=0 | → collapse to Bullet_Light

### HeavyBomb, LightChemicalWeapon, MediumFlameWeapon (1 weapons)
- `LunarTigerCannon` (ContentPacks\RedAlert2Mod\SchwarzerMond\yaml\weapons.yaml) | dominant: LightChemicalWeapon(4000) | LightChemicalWeapon=4000, LightChemicalWeaponPercentage=2, HeavyBomb=4000, HeavyBombPercentage=2, MediumFlameWeapon=4000, MediumFlameWeaponPercentage=2, CannonHE_Medium=4000 | → collapse to LightChemicalWeapon

### HeavyBomb, MediumFlameWeapon, ShrapnelWeapon (1 weapons)
- `RA2RBurritoRocket` (ContentPacks\RedAlert2Mod\Syndicate\yaml\weapons.yaml) | dominant: Demolition_Light(4000) | Demolition_Light=4000, ShrapnelWeapon=4000, ShrapnelWeaponFriendlyFire=2000, ShrapnelWeaponPercentage=2, MediumFlameWeapon=4000, MediumFlameWeaponPercentage=2, HeavyBomb=4000, HeavyBombPercentage=2, MissileAP_Heavy=4000, FireShrapnel=0 | → collapse to Demolition_Light

### HeavyBomb, MediumFlameWeapon, TeslaChargedWeapon (1 weapons)
- `IxianBomb_EMP` (ContentPacks\D2k\Ixian\yaml\weapons.yaml) | dominant: EMPUnit(120000) | MediumFlameWeapon=30000, MediumFlameWeaponPercentage=15, HeavyBomb=30000, HeavyBombPercentage=15, TeslaChargedWeapon=30000, TeslaChargedExtraDamage=15000, TeslaChargedWeaponPercentage=15, EMPUnit=120000, Effect=0 | → collapse to EMPUnit

### HeavyBomb, MediumMissile, ShrapnelWeapon (1 weapons)
- `ScarabLaunch` (ContentPacks\StarCraft\Protoss\yaml\weapons.yaml) | dominant: HeavyBomb(50000) | HeavyBomb=50000, HeavyBombPercentage=25, ShrapnelWeapon=50000, ShrapnelWeaponFriendlyFire=25000, ShrapnelWeaponPercentage=25, CannonHE_Heavy=50000, MediumMissile=50000, MediumMissilePercentage=25, Effect=0 | → collapse to HeavyBomb

### HeavyCannon, TankDestroyerCannon, TeslaWeapon (1 weapons)
- `Type89PlasmaCannon` (ContentPacks\RedAlert\Japan\yaml\weapons.yaml) | dominant: TeslaWeapon(4000) | TeslaWeapon=4000, TeslaExtraDamage=2000, TeslaWeaponPercentage=2, CannonHE_Medium=4000, CannonHE_Medium_Percentage=2, TankDestroyerCannon=4000, TankDestroyerCannonPercentage=2, Effect=0 | → collapse to TeslaWeapon

### HeavyChemicalWeapon, LaserWeapon, TeslaWeapon (1 weapons)
- `SteelMegaSword_EMP` (ContentPacks\RedAlert2Mod\Consortium\yaml\weapons.yaml) | dominant: EMPUnit(90000) | HeavyChemicalWeapon=30000, HeavyChemicalWeaponPercentage=15, LaserWeapon=30000, LaserWeaponPercentage=15, TeslaWeapon=30000, TeslaExtraDamage=15000, TeslaWeaponPercentage=15, EMPUnit=90000, Effect=0 | → collapse to EMPUnit

### HeavyFlameWeapon, RailgunWeapon, TeslaWeapon (1 weapons)
- `ixian_farasha` (ContentPacks\D2k\Ixian\yaml\weapons.yaml) | dominant: TeslaWeapon(2000) | TeslaWeapon=2000, TeslaExtraDamage=1000, TeslaWeaponPercentage=1, HeavyFlameWeapon=2000, HeavyFlameWeaponPercentage=1, RailgunWeapon=2000, RailgunWeaponPercentage=1, Effect=0, Smudge=0 | → collapse to TeslaWeapon

### HeavyMissile, MediumFlameWeapon, NuclearWarhead (1 weapons)
- `ThermobaricMaverick` (ContentPacks\RedAlert\Soviets\yaml\weapons.yaml) | dominant: NuclearWarhead(12000) | NuclearWarhead=12000, NuclearWarheadPercentage=6, MediumFlameWeapon=12000, MediumFlameWeaponPercentage=6, HeavyMissile=12000, HeavyMissilePercentage=6, MissileAP_Medium=12000, Effect=0 | → collapse to NuclearWarhead

### LaserWeapon, LightChemicalWeapon, SwordWeapon (1 weapons)
- `SteelMegaSword` (ContentPacks\RedAlert2Mod\Consortium\yaml\weapons.yaml) | dominant: EMPUnit(60000) | LightChemicalWeapon=20000, LightChemicalWeaponPercentage=10, LaserWeapon=20000, LaserWeaponPercentage=10, SwordWeapon=20000, SwordWeaponFriendlyFire=10000, SwordWeaponPercentage=10, EMPUnit=60000, Effect=0 | → collapse to EMPUnit

### LaserWeapon, RailgunWeapon, TeslaWeapon (1 weapons)
- `SteelRunnerPistols` (ContentPacks\RedAlert2Mod\Consortium\yaml\weapons.yaml) | dominant: EMPUnit(12000) | TeslaWeapon=4000, TeslaExtraDamage=2000, TeslaWeaponPercentage=2, LaserWeapon=4000, LaserWeaponPercentage=2, RailgunWeapon=4000, RailgunWeaponPercentage=2, EMPUnit=12000 | → collapse to EMPUnit

### LightFlameWeapon, MagicWeapon, MediumChemicalWeapon (1 weapons)
- `NanoSmokeAG` (ContentPacks\RedAlert\Japan\yaml\weapons.yaml) | dominant: MagicWeapon(3333) | MagicWeapon=3333, MagicWeaponPercentage=3, MediumChemicalWeapon=3333, MediumChemicalWeaponPercentage=3, LightFlameWeapon=3333, LightFlameWeaponPercentage=3, Effect=0, BackEffect=0 | → collapse to MagicWeapon

### MediumCannon, RailgunWeapon, TankDestroyerCannon (1 weapons)
- `TankBusterBeamCannon` (ContentPacks\RedAlert\Japan\yaml\weapons.yaml) | dominant: TankDestroyerCannon(8000) | TankDestroyerCannon=8000, TankDestroyerCannonPercentage=4, MediumCannon=8000, MediumCannonPercentage=4, RailgunWeapon=8000, RailgunWeaponPercentage=4 | → collapse to TankDestroyerCannon

### MediumChemicalWeapon, RailgunWeapon, ShrapnelWeapon (1 weapons)
- `RA2HeavyMirageGun` (ContentPacks\RedAlert2\Allies\yaml\weapons.yaml) | dominant: RailgunWeapon(8000) | RailgunWeapon=8000, RailgunWeaponPercentage=4, ShrapnelWeapon=8000, ShrapnelWeaponFriendlyFire=4000, ShrapnelWeaponPercentage=4, MediumChemicalWeapon=8000, MediumChemicalWeaponPercentage=4, CannonAP_Light=8000 | → collapse to RailgunWeapon

### MediumMissile, RailgunWeapon, TankDestroyerCannon (1 weapons)
- `wc2_dwarf_Rifle` (ContentPacks\Warcraft2\Humans\yaml\weapons.yaml) | dominant: RailgunWeapon(8000) | RailgunWeapon=8000, RailgunWeaponPercentage=4, MediumMissile=8000, MediumMissilePercentage=4, TankDestroyerCannon=8000, TankDestroyerCannonPercentage=4, Effect=0 | → collapse to RailgunWeapon

### LaserWeapon, RailgunWeapon (3 weapons)
- `RA2CosmonautLaser` (ContentPacks\RedAlert2\Yuri\yaml\weapons.yaml) | dominant: RailgunWeapon(2000) | LightMissile=0, LightMissilePercentage=0, Bullet_Light=0, Bullet_Light_Percentage=0, FlakWeapon=0, FlakWeaponPercentage=0, Bullet_Medium=0, RailgunWeapon=2000, RailgunWeaponPercentage=1, LaserWeapon=2000, LaserWeaponPercentage=1, Smudge=0 | → collapse to RailgunWeapon
- `SteelAirTurret` (ContentPacks\RedAlert2Mod\Consortium\yaml\weapons.yaml) | dominant: LaserWeapon(24000) | LaserWeapon=24000, LaserWeaponPercentage=12, RailgunWeapon=24000, RailgunWeaponPercentage=12, Effect=0 | → collapse to LaserWeapon
- `SteelStalkerRailgun` (ContentPacks\RedAlert2Mod\Consortium\yaml\weapons.yaml) | dominant: LaserWeapon(60000) | LaserWeapon=60000, LaserWeaponPercentage=30, RailgunWeapon=60000, RailgunWeaponPercentage=30, Effect=0 | → collapse to LaserWeapon

### MediumChemicalWeapon, MediumMissile (3 weapons)
- `AsianPulverizerMechaGatling` (ContentPacks\RedAlert2Mod\AsianAlliance\yaml\weapons.yaml) | dominant: MediumChemicalWeapon(2000) | MediumChemicalWeapon=2000, MediumChemicalWeaponPercentage=1, MediumMissile=2000, MediumMissilePercentage=1 | → collapse to MediumChemicalWeapon
- `DeviatorMissile` (ContentPacks\D2k\Ordos\yaml\weapons.yaml) | dominant: MediumChemicalWeapon(10000) | MediumChemicalWeapon=10000, MediumChemicalWeaponPercentage=5, CannonHE_Heavy=10000, MediumMissile=10000, MediumMissilePercentage=5, MissileAP_Heavy=10000, Effect=0, OwnerChange=0, Concrete=1000 | → collapse to MediumChemicalWeapon
- `TSStankTibTusk` (ContentPacks\TiberianSun\Nod\yaml\weapons.yaml) | dominant: MediumMissile(10000) | MediumMissile=10000, MediumMissilePercentage=5, MediumChemicalWeapon=10000, MediumChemicalWeaponPercentage=5, CannonHE_Medium=10000, MediumCannonFriendlyFire=5000 | → collapse to MediumMissile

### Grenade, MediumChemicalWeapon (2 weapons)
- `FutureMechPlasma` (ContentPacks\RedAlert2Mod\FutureTech\yaml\weapons.yaml) | dominant: Grenade(10000) | Grenade=10000, GrenadeFriendlyFire=5000, GrenadePercentage=5, MediumChemicalWeapon=10000, MediumChemicalWeaponPercentage=5, CannonHE_Heavy=10000, Effect=0 | → collapse to Grenade
- `VolkovMagneticWeapon` (ContentPacks\RedAlert\Soviets\yaml\weapons.yaml) | dominant: CannonHE_Heavy(10000) | CannonHE_Heavy=10000, MediumChemicalWeapon=10000, MediumChemicalWeaponPercentage=5, Grenade=10000, GrenadeFriendlyFire=5000, GrenadePercentage=5, Effect=0 | → collapse to CannonHE_Heavy

### HeavyBomb, LightChemicalWeapon (2 weapons)
- `NaxRatteCannon` (ContentPacks\RedAlert2Mod\Naxis\yaml\weapons.yaml) | dominant: LightChemicalWeapon(50000) | LightChemicalWeapon=50000, LightChemicalWeaponPercentage=25, HeavyBomb=50000, HeavyBombPercentage=25, CannonHE_Medium=50000 | → collapse to LightChemicalWeapon
- `NaxisBlackBombSmaller` (weapons\redalert2mod.yaml) | dominant: LightChemicalWeapon(25000) | LightChemicalWeapon=25000, LightChemicalWeaponPercentage=10, HeavyBomb=25000, HeavyBombPercentage=10, CannonHE_Medium=25000, Effect=0, Radiation=0 | → collapse to LightChemicalWeapon

### MediumChemicalWeapon, MediumFlameWeapon (2 weapons)
- `Laboratory_Bioball` (ContentPacks\D2k\Ordos\yaml\weapons.yaml) | dominant: Demolition_Light(10000) | Demolition_Light=10000, Concussion_Medium=10000, CannonHE_Heavy=10000, MediumChemicalWeapon=10000, MediumChemicalWeaponPercentage=6, MediumFlameWeapon=10000, MediumFlameWeaponPercentage=5, Effect=0, Concrete=1875, Cloud=0 | → collapse to Demolition_Light
- `MutFlamerChem` (ContentPacks\TiberianSun\Forgotten\yaml\weapons.yaml) | dominant: MediumFlameWeapon(28000) | MediumFlameWeapon=28000, MediumFlameWeaponPercentage=14, MediumChemicalWeapon=14000, MediumChemicalWeaponPercentage=7, Cloud=0 | → collapse to MediumFlameWeapon

### MediumFlameWeapon, ShrapnelWeapon (2 weapons)
- `GrenadeThermobaric` (ContentPacks\RedAlert\Soviets\yaml\weapons.yaml) | dominant: ShrapnelWeapon(4000) | ShrapnelWeapon=4000, ShrapnelWeaponFriendlyFire=2000, ShrapnelWeaponPercentage=2, Demolition_Light=4000, MediumFlameWeapon=4000, MediumFlameWeaponPercentage=2, Flame_Light=4000 | → collapse to ShrapnelWeapon
- `SteelDaggerCannon` (ContentPacks\RedAlert2Mod\Consortium\yaml\weapons.yaml) | dominant: MediumFlameWeapon(2000) | MediumFlameWeapon=2000, MediumFlameWeaponPercentage=1, ShrapnelWeapon=2000, ShrapnelWeaponFriendlyFire=1000, ShrapnelWeaponPercentage=1, Demolition_Light=2000, CannonHE_Heavy=2000, Effect=0 | → collapse to MediumFlameWeapon

### MediumFlameWeapon, TeslaWeapon (2 weapons)
- `SkyHawkArrowsEnergized` (ContentPacks\RedAlert\Japan\yaml\weapons.yaml) | dominant: Arrow_Light(16000) | Arrow_Light=16000, MissileHE_Medium=16000, MediumFlameWeapon=16000, MediumFlameWeaponPercentage=8, TeslaWeapon=16000, TeslaExtraDamage=8000, TeslaWeaponPercentage=8, EMPUnit=16000, Effect=0, EffectAir=0 | → collapse to Arrow_Light
- `VolkovMagneticWeaponIncendiaryTesla` (ContentPacks\RedAlert\Soviets\yaml\weapons.yaml) | dominant: EMPUnit(20000) | CannonHE_Heavy=8000, MediumChemicalWeapon=8000, MediumChemicalWeaponPercentage=4, Grenade=8000, GrenadeFriendlyFire=4000, GrenadePercentage=4, MediumFlameWeapon=8000, MediumFlameWeaponPercentage=4, TeslaWeapon=8000, TeslaExtraDamage=4000, TeslaWeaponPercentage=4, EMPUnit=20000, TeslaArc=0 | → collapse to EMPUnit

### ArrowWeapon, Grenade (1 weapons)
- `AsianMaidenBow` (ContentPacks\RedAlert2Mod\AsianAlliance\yaml\weapons.yaml) | dominant: Grenade(2000) | Grenade=2000, GrenadeFriendlyFire=1000, GrenadePercentage=1, ArrowWeapon=2000, ArrowWeaponPercentage=1, Effect=0, EffectAir=0 | → collapse to Grenade

### ArrowWeapon, TeslaWeapon (1 weapons)
- `ConsortiumMissileSystem` (ContentPacks\RedAlert2Mod\Consortium\yaml\weapons.yaml) | dominant: TeslaWeapon(6000) | TeslaWeapon=6000, TeslaExtraDamage=3000, TeslaWeaponPercentage=3, ArrowWeapon=6000, ArrowWeaponPercentage=3, Flak_Medium=6000, MissileAP_Medium=6000 | → collapse to TeslaWeapon

### Chaingun, SniperWeapon (1 weapons)
- `RA2FreedomAK47` (ContentPacks\RedAlert2Mod\Syndicate\yaml\weapons.yaml) | dominant: CannonHE_Heavy(6000) | CannonHE_Heavy=6000, Chaingun=6000, ChaingunPercentage=3, SniperWeapon=6000, SniperWeaponExtraDamage=6000, SniperWeaponPercentage=3 | → collapse to CannonHE_Heavy

### FlakWeapon, HeavyMissile (1 weapons)
- `AsianMLRS` (ContentPacks\RedAlert2Mod\AsianAlliance\yaml\weapons.yaml) | dominant: HeavyMissile(2000) | HeavyMissile=2000, HeavyMissilePercentage=1, FlakWeapon=2000, FlakWeaponPercentage=1, Demolition_Light=2000, MissileAP_Medium=2000 | → collapse to HeavyMissile

### FlakWeapon, ShrapnelWeapon (1 weapons)
- `RA2FreedomRocket` (ContentPacks\RedAlert2Mod\Syndicate\yaml\weapons.yaml) | dominant: FlakWeapon(60000) | FlakWeapon=60000, FlakWeaponPercentage=30, ShrapnelWeapon=60000, ShrapnelWeaponFriendlyFire=30000, ShrapnelWeaponPercentage=30, MissileAP_Medium=60000 | → collapse to FlakWeapon

### Grenade, TankDestroyerCannon (1 weapons)
- `GoliathMk2MG` (ContentPacks\StarCraft\Terran\yaml\weapons.yaml) | dominant: Grenade(2000) | Grenade=2000, GrenadeFriendlyFire=1000, GrenadePercentage=1, CannonHE_Heavy=2000, TankDestroyerCannon=2000, TankDestroyerCannonPercentage=1, Bullet_Medium=2000 | → collapse to Grenade

### HeavyBomb, HeavyCannon (1 weapons)
- `TurretGunBlackMarket` (ContentPacks\TiberianDawn\Nod\yaml\weapons.yaml) | dominant: HeavyBomb(6000) | HeavyBomb=6000, HeavyBombPercentage=3, HeavyCannon=6000, HeavyCannonPercentage=3 | → collapse to HeavyBomb

### HeavyBomb, MagicWeapon (1 weapons)
- `ra1_allies_chronovortex` (ContentPacks\RedAlert\Shared\yaml\weapons.yaml) | dominant: MagicWeapon(2000) | MagicWeapon=2000, MagicWeaponPercentage=1, HeavyBomb=2000, HeavyBombPercentage=1, PhysicalStateCryo1=0, PhysicalStateCryo2=0, PhysicalStateCryo3=0, PhysicalStateCryo4=0, PhysicalStateCryo5=0, PhysicalStateCryo6=0, PhysicalStateCryo7=0, PhysicalStateCryo8=0, PhysicalStateCryo9=0, PhysicalStateCryo10=0, PhysicalStateCryo11=0, PhysicalStateCryo12=0 | → collapse to MagicWeapon

### HeavyBomb, MediumChemicalWeapon (1 weapons)
- `GradHeavyRockets` (ContentPacks\RedAlert\Soviets\yaml\weapons.yaml) | dominant: MediumChemicalWeapon(4000) | MediumChemicalWeapon=4000, MediumChemicalWeaponPercentage=2, HeavyBomb=4000, HeavyBombPercentage=2, MissileHE_Heavy=4000, Concussion_Medium=4000 | → collapse to MediumChemicalWeapon

### HeavyBomb, ShrapnelWeapon (1 weapons)
- `BuggyPlasmaGrenade` (ContentPacks\RedAlert\Japan\yaml\weapons.yaml) | dominant: HeavyBomb(20000) | HeavyBomb=20000, HeavyBombPercentage=10, ShrapnelWeapon=20000, ShrapnelWeaponFriendlyFire=10000, ShrapnelWeaponPercentage=10, Demolition_Light=20000, Effect=0 | → collapse to HeavyBomb

### HeavyCannon, TeslaWeapon (1 weapons)
- `WaveforceCannonDistortedBeam1` (ContentPacks\RedAlert\Shared\yaml\weapons.yaml) | dominant: TeslaWeapon(10000) | TeslaWeapon=10000, TeslaExtraDamage=5000, TeslaWeaponPercentage=5, HeavyCannon=10000, HeavyCannonPercentage=5, Effect=0, EffectWater=0, EffectAir=0 | → collapse to TeslaWeapon

### HeavyChemicalWeapon, SniperWeapon (1 weapons)
- `RA2Virusgun` (ContentPacks\RedAlert2\Yuri\yaml\weapons.yaml) | dominant: SniperWeapon(12000) | SniperWeapon=12000, SniperWeaponExtraDamage=12000, SniperWeaponPercentage=6, HeavyChemicalWeapon=12000, HeavyChemicalWeaponPercentage=6, Effect=0, Cloud=0 | → collapse to SniperWeapon

### HeavyFlameWeapon, ShrapnelWeapon (1 weapons)
- `DeviatorMissile_Artillery` (ContentPacks\D2k\Ordos\yaml\weapons.yaml) | dominant: ShrapnelWeapon(10000) | ShrapnelWeapon=10000, ShrapnelWeaponFriendlyFire=5000, ShrapnelWeaponPercentage=5, HeavyFlameWeapon=10000, HeavyFlameWeaponPercentage=5, OwnerChange=0 | → collapse to ShrapnelWeapon

### HeavyMissile, LaserWeapon (1 weapons)
- `HMG_Duelist_upgrade` (ContentPacks\D2k\Ixian\yaml\weapons.yaml) | dominant: LaserWeapon(2000) | LaserWeapon=2000, LaserWeaponPercentage=1, HeavyMissile=2000, HeavyMissilePercentage=1, CannonHE_Heavy=2000 | → collapse to LaserWeapon

### LaserWeapon, MediumFlameWeapon (1 weapons)
- `TSProton` (ContentPacks\TiberianSun\Nod\yaml\weapons.yaml) | dominant: MediumFlameWeapon(30000) | MediumFlameWeapon=30000, MediumFlameWeaponPercentage=15, LaserWeapon=30000, LaserWeaponPercentage=15, 2Eff=0 | → collapse to MediumFlameWeapon

### LaserWeapon, TankDestroyerCannon (1 weapons)
- `SteelFighterRailgun` (ContentPacks\RedAlert2Mod\Consortium\yaml\weapons.yaml) | dominant: LaserWeapon(2000) | LaserWeapon=2000, LaserWeaponPercentage=1, Railgun_Heavy=2000, CannonHE_Medium=2000, TankDestroyerCannon=2000, TankDestroyerCannonPercentage=1, MissileAP_Light=2000, Effect=0, EffectAir=0 | → collapse to LaserWeapon

### LightChemicalWeapon, TeslaWeapon (1 weapons)
- `SteelMakoGun_EMP` (ContentPacks\RedAlert2Mod\Consortium\yaml\weapons.yaml) | dominant: EMPUnit(6000) | SteelMakoCannonHE_Medium_NoWall=2000, MediumFlameWeaponPercentage=1, Demolition_Light=2000, Railgun_Heavy=2000, CannonHE_Medium=2000, LightChemicalWeapon=2000, LightChemicalWeaponPercentage=1, TeslaWeapon=2000, TeslaExtraDamage=1000, TeslaWeaponPercentage=1, EMPUnit=6000 | → collapse to EMPUnit

### MediumChemicalWeapon, RailgunWeapon (1 weapons)
- `RA2MirageGun` (ContentPacks\RedAlert2\Shared\yaml\weapons.yaml) | dominant: RailgunWeapon(8000) | RailgunWeapon=8000, RailgunWeaponPercentage=4, MediumChemicalWeapon=8000, MediumChemicalWeaponPercentage=4, CannonAP_Light=8000 | → collapse to RailgunWeapon

### MediumChemicalWeapon, ShrapnelWeapon (1 weapons)
- `TSChem120mmx` (ContentPacks\TiberianSun\Forgotten\yaml\weapons.yaml) | dominant: ShrapnelWeapon(30000) | ShrapnelWeapon=30000, ShrapnelWeaponFriendlyFire=15000, ShrapnelWeaponPercentage=15, CannonHE_Medium=30000, MediumChemicalWeapon=30000, MediumChemicalWeaponPercentage=15, Cloud=0 | → collapse to ShrapnelWeapon

### MediumFlameWeapon, NuclearWarhead (1 weapons)
- `VolkovMagneticWeaponIncendiaryNuclearShells` (ContentPacks\RedAlert\Soviets\yaml\weapons.yaml) | dominant: MediumFlameWeapon(40000) | MediumFlameWeapon=40000, MediumFlameWeaponPercentage=20, CannonHE_Heavy=40000, MediumChemicalWeapon=40000, MediumChemicalWeaponPercentage=20, Grenade=40000, GrenadeFriendlyFire=20000, GrenadePercentage=20, NuclearWarhead=40000, NuclearWarheadPercentage=20, Effect=0 | → collapse to MediumFlameWeapon

### MediumFlameWeapon, TankDestroyerCannon (1 weapons)
- `CannonAttackRobotGun` (ContentPacks\RedAlert2Mod\FutureTech\yaml\weapons.yaml) | dominant: TankDestroyerCannon(2000) | TankDestroyerCannon=2000, TankDestroyerCannonPercentage=1, MediumFlameWeapon=2000, MediumFlameWeaponPercentage=1, Railgun_Heavy=2000, CannonHE_Medium=2000, Effect=0 | → collapse to TankDestroyerCannon

### MediumMissile, TankDestroyerCannon (1 weapons)
- `MarauderMissiles` (ContentPacks\StarCraft\Terran\yaml\weapons.yaml) | dominant: CannonHE_Heavy(10000) | CannonHE_Heavy=10000, TankDestroyerCannon=10000, TankDestroyerCannonPercentage=5, MediumMissile=10000, MediumMissilePercentage=5 | → collapse to CannonHE_Heavy

### RailgunWeapon, ShrapnelWeapon (1 weapons)
- `TSRPGTowerRail` (ContentPacks\TiberianSun\GDI\yaml\weapons.yaml) | dominant: ShrapnelWeapon(16000) | ShrapnelWeapon=16000, ShrapnelWeaponFriendlyFire=8000, ShrapnelWeaponPercentage=8, CannonHE_Heavy=16000, RailgunWeapon=16000, HRailgunWeaponPercentage=8 | → collapse to ShrapnelWeapon

### ShrapnelWeapon, TankDestroyerCannon (1 weapons)
- `RA2120xmm` (ContentPacks\RedAlert2\Soviets\yaml\weapons.yaml) | dominant: TankDestroyerCannon(4000) | TankDestroyerCannon=4000, TankDestroyerCannonPercentage=2, ShrapnelWeapon=4000, ShrapnelWeaponFriendlyFire=2000, ShrapnelWeaponPercentage=2, CannonHE_Heavy=4000 | → collapse to TankDestroyerCannon

### SwordWeapon, TeslaWeapon (1 weapons)
- `SamuraiBladeCharged` (ContentPacks\RedAlert\Japan\yaml\weapons.yaml) | dominant: TeslaWeapon(10000) | TeslaWeapon=10000, TeslaExtraDamage=5000, TeslaWeaponPercentage=5, SwordWeapon=10000, SwordWeaponFriendlyFire=5000, SwordWeaponPercentage=5 | → collapse to TeslaWeapon

### TankDestroyerCannon, TeslaWeapon (1 weapons)
- `SteelKatyCannons_EMP` (ContentPacks\RedAlert2Mod\Consortium\yaml\weapons.yaml) | dominant: EMPUnit(25000) | TankDestroyerCannon=10000, TankDestroyerCannonPercentage=5, TeslaWeapon=10000, TeslaExtraDamage=5000, TeslaWeaponPercentage=5, LightFlameWeapon=10000, LightFlameWeaponPercentage=5, MediumChemicalWeapon=10000, MediumChemicalWeaponPercentage=5, CannonHE_Heavy=10000, EMPUnit=25000, Effect=0 | → collapse to EMPUnit
