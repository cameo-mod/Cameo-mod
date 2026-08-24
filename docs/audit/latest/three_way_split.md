# audit_three_way_split — 1173 of 2345 weapons fire more than ONE main warhead

    936  correct — exactly one main warhead
    236  none — utility / effect-only weapons
   1173  VIOLATIONS — stacked mains

  mains  weapons
      2    427
      3    276
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

453 distinct stacked combinations; the 20 most common:

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
| 10 | Bullet_Light + Bullet_Medium + Laser_Heavy |
| 10 | Chaingun + FlakWeapon + Grenade + HeavyBomb + MediumMissile + ShrapnelWeapon |
| 10 | Chaingun + LaserWeapon + MediumMissile + SmallArms |
| 9 | 1Dam + CannonHE_Medium |
| 9 | HeavyCannon + HeavyMissile + MediumCannon + MediumFlameWeapon + MediumMissile + ShrapnelWeapon + TeslaChargedWeapon + TeslaWeapon |
| 9 | 1Dam + Bullet_Light + Bullet_Medium |
| 8 | Concussion_Medium + Demolition_Light |
| 8 | MissileHE_Light + MissileHE_Medium |
| 8 | Chaingun + Flak_Medium + LightMissile + TankDestroyerCannon |
| 8 | CannonHE_Heavy + Concussion_Medium + Demolition_Light |

WARN 1173 violating weapons (ratchet 1173)
Lower `SPLIT_BASELINE` as W24 converts weapons; never raise it.
