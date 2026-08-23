# audit_three_way_split — 1190 of 2325 weapons fire more than ONE main warhead

    927  correct — exactly one main warhead
    208  none — utility / effect-only weapons
   1190  VIOLATIONS — stacked mains

  mains  weapons
      2    434
      3    249
      4    192
      5     86
      6     64
      7     59
      8     53
      9     22
     10     14
     11      7
     12      4
     13      1
     14      1
     15      2
     16      2

459 distinct stacked combinations; the 20 most common:

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
| 10 | Chaingun + FlakWeapon + Grenade + GrenadeFriendlyFire + HeavyBomb + MediumMissile + ShrapnelWeapon + ShrapnelWeaponFriendlyFire |
| 10 | Chaingun + LaserWeapon + MediumMissile + SmallArms |
| 9 | 1Dam + CannonHE_Medium |
| 9 | HeavyCannon + HeavyMissile + MediumCannon + MediumFlameWeapon + MediumMissile + ShrapnelWeapon + ShrapnelWeaponFriendlyFire + TeslaChargedWeapon + TeslaWeapon |
| 9 | 1Dam + Bullet_Light + Bullet_Medium |
| 8 | Concussion_Medium + MissileHE_Heavy |
| 8 | Concussion_Medium + Demolition_Light |
| 8 | Chaingun + Flak_Medium + LightMissile + TankDestroyerCannon |

WARN 1190 violating weapons (ratchet 1190)
Lower `SPLIT_BASELINE` as W24 converts weapons; never raise it.
