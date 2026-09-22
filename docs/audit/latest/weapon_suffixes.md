# Weapon suffix audit (DESIGN.md §1)

X1 elite weapons not ending _elite: **2** (+25 exempt shared-rung weapons, Ruling 2)
X2 EMP weapons not ending _EMP: **0**
X3 AA weapons not ending _AA: **11**
X4 deprecated E suffix (informational): **0**
X5 suffix ordering violations: **0**

## X1 — Elite weapons not following _elite convention
| File | Line | Actor | Trait | Weapon |
|---|---|---|---|---|
| ContentPacks/RedAlert2/Shared/yaml/misc.yaml | 2298 | ra2_c_ifv | Armament@eliteAA | RA2GattlingMG2_AA |
| rules/redalert2.yaml | 2608 | ra2_c_ifv | Armament@eliteAA | RA2GattlingMG2_AA |

## X3 — AA-only weapons not following _AA convention
| File | Line | Weapon | ValidTargets |
|---|---|---|---|
| ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 495 | BallistaSingleShotAir | Air |
| ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 504 | BallistaSingleShotAirEnergized | Air |
| ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml | 141 | ConsortiumMissileSystem | Air |
| ContentPacks/StarCraft/Terran/yaml/weapons.yaml | 1087 | GoliathMk2Rockets | Air |
| ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 1101 | td_nod_buggymkii_laserbuggy2_AAinferno | Air |
| ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml | 1110 | td_nod_buggymkii_laserbuggy2_AAburning | Air |
| ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 1569 | CabalLaserBoatLaserAA | Air |
| ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml | 1917 | CabalManticoreMissilesAA | Air |
| ContentPacks/TiberianSun/GDI/yaml/weapons.yaml | 663 | TSMammothTusk2 | Air |
| weapons/darkreign.yaml | 404 | DRBionWeaponAA | Air |
| weapons/tiberiansun.yaml | 1255 | TSChemAdatsMissileAA |  |

