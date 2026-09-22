# audit_three_way_split — 142 weapons with MORE THAN ONE main warhead

_The `intentional_composites` exemption was DELETED 2026-09-06 (DESIGN §11b.1). Nothing is subtracted — every stack is debt._

   1992  correct — exactly one main warhead
    320  none — utility / effect-only weapons
    142  RAW STACKS — structural inventory
    142  STACKS — all debt under §11b.1

  mains  weapons
      2     78
      3     37
      4     11
      5      7
      6      1
      7      7
      8      1

71 distinct stacked combinations; the 20 most common:

| count | combination |
|---|---|
| 8 | 1Dam + MissileAP_Heavy |
| 8 | TemperatureCompatibility + Tesla_Super |
| 7 | 10Dam_areanuke3 + 11Dam_areanuke3 + 1Dam_impact + 4Dam_areanuke1 + 7Dam_areanuke2 + 8Dam_areanuke2 + Damage |
| 7 | CannonHE_Medium + ShotgunChaingun + ShotgunShrapnelEnemy + ShotgunSmallArms + ShotgunTankDestroyer |
| 6 | Nuclear_Super + Tesla_Super |
| 5 | Demolition_Light + MissileAP_Heavy + RA2SCUDMissileAP_Heavy_NoWall |
| 5 | 1Dam + Concussion_Medium + Demolition_Heavy |
| 4 | 1Dam + MissileHE_Heavy |
| 4 | 1Dam + Bullet_Light |
| 3 | 1Dam + Flame_Medium |
| 3 | 1Dam + Demolition_Heavy + Flame_Heavy |
| 3 | IonCannon + Tesla_Super |
| 3 | Laser_Heavy + Railgun_Heavy + Tesla_Heavy |
| 3 | Quantum_Heavy_Flat + Tesla_Heavy |
| 2 | IonCannon + TeslaChargedWeapon + TeslaWeapon + Tesla_Super |
| 2 | 1Dam + Demolition_Light + Flame_Light |
| 2 | CannonHE_Heavy + Plasma_Heavy_Flat |
| 2 | CannonHE_Heavy + CannonHE_Heavy_Flat |
| 2 | 1Dam + Clear |
| 2 | 1Dam + Demolition_Light |

WARN raw 142/322; (cross-check audit_weapon_shape W5)
Lower `RAW_SPLIT_BASELINE` as weapons are collapsed; never raise it. W5 also resolves inheritance, but includes zero/healing/ally-only flat nodes and has narrower type/name rules. Use audit_weapon_shape.py --compare-split for the exact set difference; neither count is subtracted or reclassified.
