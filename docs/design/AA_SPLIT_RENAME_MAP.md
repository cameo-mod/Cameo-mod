# AA split rename map - `X_AG` + `X_AA`, and no suffix otherwise

Maintainer, 2026-09-14: *"Twin _AG and _AA weapons, no suffix if they can attack both like
the Rapier Jumpjet. And also no Suffix if it's the only weapon like with the Sam Site! So then
only the _AA ones will be discarded from the formula while the rest stays."*

After this map the `_AA` suffix means exactly ONE thing - the anti-air half of a twin whose
`_AG` partner sits on the same actor - so the balance formula can drop it BY NAME with no
further test, and the 1.5x range check belongs to the audit alone (DESIGN 3a.8).

GENERATED, NOT HAND-WRITTEN. NOT YET APPLIED: executing it is a `safe_rename.py` migration
that needs a boot gate and a quiet tree.

**78 renames** - 53 proven split pairs, 3 suffixes dropped.

## Renames

| old | new | why |
|---|---|---|
| `ArmoredCarMG` | `ArmoredCarMG_AG` | ground half of ArmoredCarMG_AA (same actor) |
| `AsianQuasarAG` | `AsianQuasar_AG` | AG-ish spelling normalised; partner AsianQuasar_AA |
| `AsianQuasarBoatAG` | `AsianQuasarBoat_AG` | AG-ish spelling normalised; partner AsianQuasarBoat_AA |
| `CabalHeavyReaperMissiles` | `CabalHeavyReaperMissiles_AG` | ground half of CabalHeavyReaperMissiles_AA (same actor) |
| `CabalReaperMissiles` | `CabalReaperMissiles_AG` | ground half of CabalReaperMissiles_AA (same actor) |
| `D2K_Rocket_Trooper_AGOnly` | `D2K_Rocket_Trooper_AG` | AG-ish spelling normalised; partner D2K_Rocket_Trooper_AA |
| `DiabloCannon` | `DiabloCannon_AG` | ground half of DiabloCannon_AA (same actor) |
| `DiabloCannon_elite` | `DiabloCannon_AG_elite` | state variant of the ground half DiabloCannon |
| `Lunar_AmplifiedBeetleLaser` | `Lunar_AmplifiedBeetleLaser_AG` | ground half of Lunar_AmplifiedBeetleLaser_AA (same actor) |
| `Lunar_AmplifiedTank2Laser` | `Lunar_AmplifiedTank2Laser_AG` | ground half of Lunar_AmplifiedTank2Laser_AA (same actor) |
| `Lunar_YellowBeetleLaser` | `Lunar_YellowBeetleLaser_AG` | ground half of Lunar_YellowBeetleLaser_AA (same actor) |
| `Lunar_YellowTank2Laser` | `Lunar_YellowTank2Laser_AG` | ground half of Lunar_YellowTank2Laser_AA (same actor) |
| `ManifoldMG` | `ManifoldMG_AG` | ground half of ManifoldMG_AA (same actor) |
| `MigMissiles` | `MigMissiles_AG` | ground half of MigMissiles_AA (same actor) |
| `MigMissiles_elite` | `MigMissiles_AG_elite` | state variant of the ground half MigMissiles |
| `MigMissiles_fire` | `MigMissiles_AG_fire` | state variant of the ground half MigMissiles |
| `MigMissiles_fire_elite` | `MigMissiles_AG_fire_elite` | state variant of the ground half MigMissiles |
| `MigMissiles_rad` | `MigMissiles_AG_rad` | state variant of the ground half MigMissiles |
| `MigMissiles_rad_elite` | `MigMissiles_AG_rad_elite` | state variant of the ground half MigMissiles |
| `MigMissiles_tesla` | `MigMissiles_AG_tesla` | state variant of the ground half MigMissiles |
| `MigMissiles_tesla_elite` | `MigMissiles_AG_tesla_elite` | state variant of the ground half MigMissiles |
| `NaxQuadCannon` | `NaxQuadCannon_AG` | ground half of NaxQuadCannon_AA (same actor) |
| `NaxQuadCannon_elite` | `NaxQuadCannon_AG_elite` | state variant of the ground half NaxQuadCannon |
| `NaxiTank2Laser` | `NaxiTank2Laser_AG` | ground half of NaxiTank2Laser_AA (same actor) |
| `NaxiWW2Machinegun` | `NaxiWW2Machinegun_AG` | ground half of NaxiWW2Machinegun_AA (same actor) |
| `NaxiWW2MachinegunSmall` | `NaxiWW2MachinegunSmall_AG` | ground half of NaxiWW2MachinegunSmall_AA (same actor) |
| `NaxiWW2MachinegunTop` | `NaxiWW2MachinegunTop_AG` | ground half of NaxiWW2MachinegunTop_AA (same actor) |
| `Naxis_Komet` | `Naxis_Komet_AG` | ground half of Naxis_Komet_AA (same actor) |
| `RA2APCMachineGun` | `RA2APCMachineGun_AG` | ground half of RA2APCMachineGun_AA (same actor) |
| `RA2APCMachineGun_elite` | `RA2APCMachineGun_AG_elite` | state variant of the ground half RA2APCMachineGun |
| `RA2APCRocket` | `RA2APCRocket_AG` | ground half of RA2APCRocket_AA (same actor) |
| `RA2APCRocket_elite` | `RA2APCRocket_AG_elite` | state variant of the ground half RA2APCRocket |
| `RA2GattlingMG1` | `RA2GattlingMG1_AG` | ground half of RA2GattlingMG1_AA (same actor) |
| `RA2GattlingMG2` | `RA2GattlingMG2_AG` | ground half of RA2GattlingMG2_AA (same actor) |
| `RA2GattlingMG3` | `RA2GattlingMG3_AG` | ground half of RA2GattlingMG3_AA (same actor) |
| `RA2HoverMissile` | `RA2HoverMissile_AG` | ground half of RA2HoverMissile_AA (same actor) |
| `RA2HoverMissile_elite` | `RA2HoverMissile_AG_elite` | state variant of the ground half RA2HoverMissile |
| `RA2MedusaAG` | `RA2Medusa_AG` | AG-ish spelling normalised; partner RA2Medusa_AA |
| `RA2MultiHoverMissile` | `RA2MultiHoverMissile_AG` | ground half of RA2MultiHoverMissile_AA (same actor) |
| `RA2MultiHoverMissile_elite` | `RA2MultiHoverMissile_AG_elite` | state variant of the ground half RA2MultiHoverMissile |
| `RA2MultiThunderboltMissile` | `RA2MultiThunderboltMissile_AG` | ground half of RA2MultiThunderboltMissile_AA (same actor) |
| `RA2MultiThunderboltMissile_elite` | `RA2MultiThunderboltMissile_AG_elite` | state variant of the ground half RA2MultiThunderboltMissile |
| `RA2ThunderboltMissile` | `RA2ThunderboltMissile_AG` | ground half of RA2ThunderboltMissile_AA (same actor) |
| `RA2ThunderboltMissile_elite` | `RA2ThunderboltMissile_AG_elite` | state variant of the ground half RA2ThunderboltMissile |
| `SkyMageCannon` | `SkyMageCannon_AG` | ground half of SkyMageCannon_AA (same actor) |
| `SkyMageCannon_elite` | `SkyMageCannon_AG_elite` | state variant of the ground half SkyMageCannon |
| `SteelMantaAG` | `SteelManta_AG` | AG-ish spelling normalised; partner SteelManta_AA |
| `SteelMantaHunterCannons` | `SteelMantaHunterCannons_AG` | ground half of SteelMantaHunterCannons_AA (same actor) |
| `TSAdatsMissile` | `TSAdatsMissile_AG` | ground half of TSAdatsMissile_AA (same actor) |
| `YuriGatlingCannonMG1` | `YuriGatlingCannonMG1_AG` | ground half of YuriGatlingCannonMG1_AA (same actor) |
| `YuriGatlingCannonMG2` | `YuriGatlingCannonMG2_AG` | ground half of YuriGatlingCannonMG2_AA (same actor) |
| `YuriGatlingCannonMG3` | `YuriGatlingCannonMG3_AG` | ground half of YuriGatlingCannonMG3_AA (same actor) |
| `d2k_basq` | `d2k_basq_AG` | ground half of d2k_basq_AA (same actor) |
| `edenTiger_EMP` | `edenTiger_EMP_AG` | ground half of edenTiger_EMP_AA (same actor) |
| `eden_EMP` | `eden_EMP_AG` | ground half of eden_EMP_AA (same actor) |
| `ra1_allies_alliedapc_gun` | `ra1_allies_alliedapc_gun_AG` | ground half of ra1_allies_alliedapc_gun_AA (same actor) |
| `ra1_allies_alliedheavyaatank_cannon` | `ra1_allies_alliedheavyaatank_cannon_AG` | ground half of ra1_allies_alliedheavyaatank_cannon_AA (same actor) |
| `ra1_allies_rapierjumpjet_missile_AA` | `ra1_allies_rapierjumpjet_missile` | it attacks air AND ground (air, ground, water) |
| `ra1_allies_rapierjumpjet_missile_cryo_AA` | `ra1_allies_rapierjumpjet_missile_cryo` | it attacks air AND ground (air, ground, water) |
| `ra1_soviets_btr80_machinegun` | `ra1_soviets_btr80_machinegun_AG` | ground half of ra1_soviets_btr80_machinegun_AA (same actor) |
| `ra1_soviets_btr80_machinegun_tesla` | `ra1_soviets_btr80_machinegun_tesla_AG` | ground half of ra1_soviets_btr80_machinegun_tesla_AA (same actor) |
| `ra1_soviets_btr80_machinegun_tesla_arc` | `ra1_soviets_btr80_machinegun_tesla_arc_AG` | ground half of ra1_soviets_btr80_machinegun_tesla_arc_AA (same actor) |
| `ra1_soviets_flaktruck_flak_cannon` | `ra1_soviets_flaktruck_flak_cannon_AG` | ground half of ra1_soviets_flaktruck_flak_cannon_AA (same actor) |
| `ra1_soviets_gatlingtank_incendiaryragatlingtankcannon` | `ra1_soviets_gatlingtank_incendiaryragatlingtankcannon_AG` | ground half of ra1_soviets_gatlingtank_incendiaryragatlingtankcannon_AA (same actor) |
| `ra1_soviets_gatlingtank_ragatlingtankcannon` | `ra1_soviets_gatlingtank_ragatlingtankcannon_AG` | ground half of ra1_soviets_gatlingtank_ragatlingtankcannon_AA (same actor) |
| `ra1_soviets_gatlingtank_teslaragatlingtankcannon` | `ra1_soviets_gatlingtank_teslaragatlingtankcannon_AG` | ground half of ra1_soviets_gatlingtank_teslaragatlingtankcannon_AA (same actor) |
| `ra1_soviets_gatlingtank_teslaragatlingtankcannonarc` | `ra1_soviets_gatlingtank_teslaragatlingtankcannonarc_AG` | ground half of ra1_soviets_gatlingtank_teslaragatlingtankcannonarc_AA (same actor) |
| `ra1_soviets_samsite_missile_AA` | `ra1_soviets_samsite_missile` | it is the actor's only weapon |
| `td_gdi_apc_apcgun` | `td_gdi_apc_apcgun_AG` | ground half of td_gdi_apc_apcgun_AA (same actor) |
| `td_gdi_assaultapc_machinegunhumvee2` | `td_gdi_assaultapc_machinegunhumvee2_AG` | ground half of td_gdi_assaultapc_machinegunhumvee2_AA (same actor) |
| `td_gdi_assaultapc_machinegunhumvee2ap` | `td_gdi_assaultapc_machinegunhumvee2ap_AG` | ground half of td_gdi_assaultapc_machinegunhumvee2ap_AA (same actor) |
| `td_gdi_boxer_boxercannonag` | `td_gdi_boxer_boxercannon_AG` | AG-ish spelling normalised; partner td_gdi_boxer_boxercannon_AA |
| `td_gdi_humveemkii_machinegunhumvee2` | `td_gdi_humveemkii_machinegunhumvee2_AG` | ground half of td_gdi_humveemkii_machinegunhumvee2_AA (same actor) |
| `td_gdi_humveemkii_machinegunhumvee2ap` | `td_gdi_humveemkii_machinegunhumvee2ap_AG` | ground half of td_gdi_humveemkii_machinegunhumvee2ap_AA (same actor) |
| `td_gdi_humveemkii_rocketshumvee2` | `td_gdi_humveemkii_rocketshumvee2_AG` | ground half of td_gdi_humveemkii_rocketshumvee2_AA (same actor) |
| `td_gdi_humveemkii_rocketshumvee2amt` | `td_gdi_humveemkii_rocketshumvee2amt_AG` | ground half of td_gdi_humveemkii_rocketshumvee2amt_AA (same actor) |
| `td_nod_buggymkii_laserbuggy2` | `td_nod_buggymkii_laserbuggy2_AG` | ground half of td_nod_buggymkii_laserbuggy2_AA (same actor) |
| `td_nod_buggymkii_machinegunbuggy2` | `td_nod_buggymkii_machinegunbuggy2_AG` | ground half of td_nod_buggymkii_machinegunbuggy2_AA (same actor) |

## ⛔ NOT renamed - the two halves never appear on the same actor

Name agreement is not evidence of a pair. `D2K_Rocket_Trooper` is the Atreides and
Harkonnen rocket troopers' ONLY weapon and is dual-purpose; `D2K_Rocket_Trooper_AA`
belongs to the Ordos anti-air trooper. They are different weapons that happen to
share a prefix, and suffixing the first would have hidden a live gun from pricing.

| candidate | supposed AA twin | candidate seen on | twin seen on |
|---|---|---|---|
| `D2K_APC_Rocket` | `D2K_APC_Rocket_AA` | ordos_apc, ordos_dustdrone | ordos_banshee, ordos_laboratorycrawler |
| `D2K_Annihilator` | `D2K_Annihilator_AA` | _(no resolvable actor)_ | _(no resolvable actor)_ |
| `D2K_Rocket` | `D2K_Rocket_AA` | _(no resolvable actor)_ | harkonnen_adp |
| `D2K_Rocket_Trooper` | `D2K_Rocket_Trooper_AA` | atreides_rockettrooper, corrino_sardaukar_bazooka, corrino_trooper | ordos_antiairtrooper |
| `MachineGunHumvee2AP` | `MachineGunHumvee2AP_AA` | _(no resolvable actor)_ | _(no resolvable actor)_ |
| `MachineGunHumvee2` | `MachineGunHumvee2_AA` | _(no resolvable actor)_ | _(no resolvable actor)_ |
| `Rocket_stealth` | `Rocket_stealth_AA` | _(no resolvable actor)_ | _(no resolvable actor)_ |
| `Spit` | `Spit_AA` | _(no resolvable actor)_ | _(no resolvable actor)_ |
| `bowFire` | `bowFire_AA` | _(no resolvable actor)_ | _(no resolvable actor)_ |
| `plymouth_EMP` | `plymouth_EMP_AA` | PLYMOUTH_LYNX_EMP | _(no resolvable actor)_ |
| `d2k_APC_AG` | `d2k_APC_AA` | _(no resolvable actor)_ | _(no resolvable actor)_ |
| `d2k_APCo_AG` | `d2k_APCo_AA` | _(no resolvable actor)_ | _(no resolvable actor)_ |
