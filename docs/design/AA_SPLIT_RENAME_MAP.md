# AA split rename map - `X_AG` + `X_AA`

Maintainer ruling, 2026-09-14: *"The pair should have the _AA and the _AG pair where the AA
variant is the one with 1.5x range and anti air right?"* Both halves of a split carry an
explicit suffix, so the convention is machine-readable and the balance formula can drop the
`_AA` half by name (DESIGN 3a.8).

GENERATED, NOT HAND-WRITTEN. Regenerate with the scratchpad script recorded in the PR.
NOT YET APPLIED - this is the reviewable proposal; executing it is a `safe_rename.py`
migration that needs a boot gate and a quiet tree.

| old | new | why |
|---|---|---|
| `ArmoredCarMG` | `ArmoredCarMG_AG` | ground half of ArmoredCarMG_AA |
| `AsianQuasarAG` | `AsianQuasar_AG` | AG-ish spelling normalised; partner AsianQuasar_AA |
| `AsianQuasarBoatAG` | `AsianQuasarBoat_AG` | AG-ish spelling normalised; partner AsianQuasarBoat_AA |
| `CabalHeavyReaperMissiles` | `CabalHeavyReaperMissiles_AG` | ground half of CabalHeavyReaperMissiles_AA |
| `CabalReaperMissiles` | `CabalReaperMissiles_AG` | ground half of CabalReaperMissiles_AA |
| `D2K_APC_Rocket` | `D2K_APC_Rocket_AG` | ground half of D2K_APC_Rocket_AA |
| `D2K_Annihilator` | `D2K_Annihilator_AG` | ground half of D2K_Annihilator_AA |
| `D2K_Rocket` | `D2K_Rocket_AG` | ground half of D2K_Rocket_AA |
| `D2K_Rocket_Trooper` | `D2K_Rocket_Trooper_AG` | ground half of D2K_Rocket_Trooper_AA |
| `DiabloCannon` | `DiabloCannon_AG` | ground half of DiabloCannon_AA |
| `DiabloCannon_elite` | `DiabloCannon_AG_elite` | state variant of the ground half DiabloCannon |
| `Lunar_AmplifiedBeetleLaser` | `Lunar_AmplifiedBeetleLaser_AG` | ground half of Lunar_AmplifiedBeetleLaser_AA |
| `Lunar_AmplifiedTank2Laser` | `Lunar_AmplifiedTank2Laser_AG` | ground half of Lunar_AmplifiedTank2Laser_AA |
| `Lunar_YellowBeetleLaser` | `Lunar_YellowBeetleLaser_AG` | ground half of Lunar_YellowBeetleLaser_AA |
| `Lunar_YellowTank2Laser` | `Lunar_YellowTank2Laser_AG` | ground half of Lunar_YellowTank2Laser_AA |
| `MachineGunHumvee2` | `MachineGunHumvee2_AG` | ground half of MachineGunHumvee2_AA |
| `MachineGunHumvee2AP` | `MachineGunHumvee2AP_AG` | ground half of MachineGunHumvee2AP_AA |
| `ManifoldMG` | `ManifoldMG_AG` | ground half of ManifoldMG_AA |
| `MigMissiles` | `MigMissiles_AG` | ground half of MigMissiles_AA |
| `MigMissiles_elite` | `MigMissiles_AG_elite` | state variant of the ground half MigMissiles |
| `MigMissiles_fire` | `MigMissiles_AG_fire` | state variant of the ground half MigMissiles |
| `MigMissiles_fire_elite` | `MigMissiles_AG_fire_elite` | state variant of the ground half MigMissiles |
| `MigMissiles_rad` | `MigMissiles_AG_rad` | state variant of the ground half MigMissiles |
| `MigMissiles_rad_elite` | `MigMissiles_AG_rad_elite` | state variant of the ground half MigMissiles |
| `MigMissiles_tesla` | `MigMissiles_AG_tesla` | state variant of the ground half MigMissiles |
| `MigMissiles_tesla_elite` | `MigMissiles_AG_tesla_elite` | state variant of the ground half MigMissiles |
| `NaxQuadCannon` | `NaxQuadCannon_AG` | ground half of NaxQuadCannon_AA |
| `NaxQuadCannon_elite` | `NaxQuadCannon_AG_elite` | state variant of the ground half NaxQuadCannon |
| `NaxiTank2Laser` | `NaxiTank2Laser_AG` | ground half of NaxiTank2Laser_AA |
| `NaxiWW2Machinegun` | `NaxiWW2Machinegun_AG` | ground half of NaxiWW2Machinegun_AA |
| `NaxiWW2MachinegunSmall` | `NaxiWW2MachinegunSmall_AG` | ground half of NaxiWW2MachinegunSmall_AA |
| `NaxiWW2MachinegunTop` | `NaxiWW2MachinegunTop_AG` | ground half of NaxiWW2MachinegunTop_AA |
| `Naxis_Komet` | `Naxis_Komet_AG` | ground half of Naxis_Komet_AA |
| `RA2APCMachineGun` | `RA2APCMachineGun_AG` | ground half of RA2APCMachineGun_AA |
| `RA2APCMachineGun_elite` | `RA2APCMachineGun_AG_elite` | state variant of the ground half RA2APCMachineGun |
| `RA2APCRocket` | `RA2APCRocket_AG` | ground half of RA2APCRocket_AA |
| `RA2APCRocket_elite` | `RA2APCRocket_AG_elite` | state variant of the ground half RA2APCRocket |
| `RA2GattlingMG1` | `RA2GattlingMG1_AG` | ground half of RA2GattlingMG1_AA |
| `RA2GattlingMG2` | `RA2GattlingMG2_AG` | ground half of RA2GattlingMG2_AA |
| `RA2GattlingMG3` | `RA2GattlingMG3_AG` | ground half of RA2GattlingMG3_AA |
| `RA2HoverMissile` | `RA2HoverMissile_AG` | ground half of RA2HoverMissile_AA |
| `RA2HoverMissile_elite` | `RA2HoverMissile_AG_elite` | state variant of the ground half RA2HoverMissile |
| `RA2MedusaAG` | `RA2Medusa_AG` | AG-ish spelling normalised; partner RA2Medusa_AA |
| `RA2MultiHoverMissile` | `RA2MultiHoverMissile_AG` | ground half of RA2MultiHoverMissile_AA |
| `RA2MultiHoverMissile_elite` | `RA2MultiHoverMissile_AG_elite` | state variant of the ground half RA2MultiHoverMissile |
| `RA2MultiThunderboltMissile` | `RA2MultiThunderboltMissile_AG` | ground half of RA2MultiThunderboltMissile_AA |
| `RA2MultiThunderboltMissile_elite` | `RA2MultiThunderboltMissile_AG_elite` | state variant of the ground half RA2MultiThunderboltMissile |
| `RA2ThunderboltMissile` | `RA2ThunderboltMissile_AG` | ground half of RA2ThunderboltMissile_AA |
| `RA2ThunderboltMissile_elite` | `RA2ThunderboltMissile_AG_elite` | state variant of the ground half RA2ThunderboltMissile |
| `Rocket_stealth` | `Rocket_stealth_AG` | ground half of Rocket_stealth_AA |
| `SkyMageCannon` | `SkyMageCannon_AG` | ground half of SkyMageCannon_AA |
| `SkyMageCannon_elite` | `SkyMageCannon_AG_elite` | state variant of the ground half SkyMageCannon |
| `Spit` | `Spit_AG` | ground half of Spit_AA |
| `SteelMantaAG` | `SteelManta_AG` | AG-ish spelling normalised; partner SteelManta_AA |
| `SteelMantaHunterCannons` | `SteelMantaHunterCannons_AG` | ground half of SteelMantaHunterCannons_AA |
| `TSAdatsMissile` | `TSAdatsMissile_AG` | ground half of TSAdatsMissile_AA |
| `YuriGatlingCannonMG1` | `YuriGatlingCannonMG1_AG` | ground half of YuriGatlingCannonMG1_AA |
| `YuriGatlingCannonMG2` | `YuriGatlingCannonMG2_AG` | ground half of YuriGatlingCannonMG2_AA |
| `YuriGatlingCannonMG3` | `YuriGatlingCannonMG3_AG` | ground half of YuriGatlingCannonMG3_AA |
| `bowFire` | `bowFire_AG` | ground half of bowFire_AA |
| `d2k_basq` | `d2k_basq_AG` | ground half of d2k_basq_AA |
| `edenTiger_EMP` | `edenTiger_EMP_AG` | ground half of edenTiger_EMP_AA |
| `eden_EMP` | `eden_EMP_AG` | ground half of eden_EMP_AA |
| `plymouth_EMP` | `plymouth_EMP_AG` | ground half of plymouth_EMP_AA |
| `ra1_allies_alliedapc_gun` | `ra1_allies_alliedapc_gun_AG` | ground half of ra1_allies_alliedapc_gun_AA |
| `ra1_allies_alliedheavyaatank_cannon` | `ra1_allies_alliedheavyaatank_cannon_AG` | ground half of ra1_allies_alliedheavyaatank_cannon_AA |
| `ra1_soviets_btr80_machinegun` | `ra1_soviets_btr80_machinegun_AG` | ground half of ra1_soviets_btr80_machinegun_AA |
| `ra1_soviets_btr80_machinegun_tesla` | `ra1_soviets_btr80_machinegun_tesla_AG` | ground half of ra1_soviets_btr80_machinegun_tesla_AA |
| `ra1_soviets_btr80_machinegun_tesla_arc` | `ra1_soviets_btr80_machinegun_tesla_arc_AG` | ground half of ra1_soviets_btr80_machinegun_tesla_arc_AA |
| `ra1_soviets_flaktruck_flak_cannon` | `ra1_soviets_flaktruck_flak_cannon_AG` | ground half of ra1_soviets_flaktruck_flak_cannon_AA |
| `ra1_soviets_gatlingtank_incendiaryragatlingtankcannon` | `ra1_soviets_gatlingtank_incendiaryragatlingtankcannon_AG` | ground half of ra1_soviets_gatlingtank_incendiaryragatlingtankcannon_AA |
| `ra1_soviets_gatlingtank_ragatlingtankcannon` | `ra1_soviets_gatlingtank_ragatlingtankcannon_AG` | ground half of ra1_soviets_gatlingtank_ragatlingtankcannon_AA |
| `ra1_soviets_gatlingtank_teslaragatlingtankcannon` | `ra1_soviets_gatlingtank_teslaragatlingtankcannon_AG` | ground half of ra1_soviets_gatlingtank_teslaragatlingtankcannon_AA |
| `ra1_soviets_gatlingtank_teslaragatlingtankcannonarc` | `ra1_soviets_gatlingtank_teslaragatlingtankcannonarc_AG` | ground half of ra1_soviets_gatlingtank_teslaragatlingtankcannonarc_AA |
| `td_gdi_apc_apcgun` | `td_gdi_apc_apcgun_AG` | ground half of td_gdi_apc_apcgun_AA |
| `td_gdi_assaultapc_machinegunhumvee2` | `td_gdi_assaultapc_machinegunhumvee2_AG` | ground half of td_gdi_assaultapc_machinegunhumvee2_AA |
| `td_gdi_assaultapc_machinegunhumvee2ap` | `td_gdi_assaultapc_machinegunhumvee2ap_AG` | ground half of td_gdi_assaultapc_machinegunhumvee2ap_AA |
| `td_gdi_boxer_boxercannonag` | `td_gdi_boxer_boxercannon_AG` | AG-ish spelling normalised; partner td_gdi_boxer_boxercannon_AA |
| `td_gdi_humveemkii_machinegunhumvee2` | `td_gdi_humveemkii_machinegunhumvee2_AG` | ground half of td_gdi_humveemkii_machinegunhumvee2_AA |
| `td_gdi_humveemkii_machinegunhumvee2ap` | `td_gdi_humveemkii_machinegunhumvee2ap_AG` | ground half of td_gdi_humveemkii_machinegunhumvee2ap_AA |
| `td_gdi_humveemkii_rocketshumvee2` | `td_gdi_humveemkii_rocketshumvee2_AG` | ground half of td_gdi_humveemkii_rocketshumvee2_AA |
| `td_gdi_humveemkii_rocketshumvee2amt` | `td_gdi_humveemkii_rocketshumvee2amt_AG` | ground half of td_gdi_humveemkii_rocketshumvee2amt_AA |
| `td_nod_buggymkii_laserbuggy2` | `td_nod_buggymkii_laserbuggy2_AG` | ground half of td_nod_buggymkii_laserbuggy2_AA |
| `td_nod_buggymkii_machinegunbuggy2` | `td_nod_buggymkii_machinegunbuggy2_AG` | ground half of td_nod_buggymkii_machinegunbuggy2_AA |

**84 weapons**, from 63 split pairs.

## Collisions - these need a decision, the target name is already taken

| old | wanted | why |
|---|---|---|
| `D2K_Rocket_Trooper_AGOnly` | `D2K_Rocket_Trooper_AG` | AG-ish spelling normalised; partner D2K_Rocket_Trooper_AA |
