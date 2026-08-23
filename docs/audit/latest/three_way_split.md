# audit_three_way_split — 1178 of 2325 weapons fire more than ONE main warhead

    917  correct — exactly one main warhead
    230  none — utility / effect-only weapons
   1178  VIOLATIONS — stacked mains

  mains  weapons
      2    431
      3    277
      4    219
      5     60
      6    110
      7     21
      8     35
      9     10
     10      8
     11      1
     12      2
     13      1
     14      3

454 distinct stacked combinations; the 20 most common:

| count | combination |
|---|---|
| 79 | Bullet_Light + Bullet_Medium |
| 17 | Bullet_Light + Bullet_Medium + CannonHE_Heavy |
| 17 | 1Dam + MissileAP_Heavy |
| 14 | Concussion_Medium + Demolition_Heavy |
| 13 | Bullet_Medium + Flak_Medium |
| 12 | FlakWeapon + MagicWeapon + MediumMissile + TeslaWeapon |
| 12 | Demolition_Light + Flame_Light |
| 12 | FlakWeapon + HeavyAAWeapon + HeavyBomb + MissileAP_Medium |
| 10 | CannonAP_Light + CannonHE_Medium |
| 10 | MissileAP_Light + Tesla_Heavy |
| 10 | MissileHE_Light + MissileHE_Medium |
| 10 | Bullet_Light + Bullet_Medium + Laser_Heavy |
| 10 | Chaingun + FlakWeapon + Grenade + HeavyBomb + MediumMissile + ShrapnelWeapon |
| 10 | Chaingun + LaserWeapon + MediumMissile + SmallArms |
| 9 | 1Dam + CannonHE_Medium |
| 9 | HeavyCannon + HeavyMissile + MediumCannon + MediumFlameWeapon + MediumMissile + ShrapnelWeapon + TeslaChargedWeapon + TeslaWeapon |
| 9 | 1Dam + Bullet_Light + Bullet_Medium |
| 8 | Concussion_Medium + MissileHE_Heavy |
| 8 | Concussion_Medium + Demolition_Light |
| 8 | Chaingun + Flak_Medium + LightMissile + TankDestroyerCannon |

WARN 1178 violating weapons (ratchet 1178)
Lower `SPLIT_BASELINE` as W24 converts weapons; never raise it.
