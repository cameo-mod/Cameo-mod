# Remaining reachable weapon decisions

This report compresses the honest unreviewed backlog into inheritance
families. It is a review queue, not conversion authority. Reviewed exact
composites remain in the raw structural count but are excluded here.
Three independent reviews found no mechanically exact fold in this
remaining set; each row requires an armor, geometry, targeting, state,
or progression decision before its live behavior can be changed.
The player-facing recommendation for every family is maintained in
`docs/design/WEAPON_REDESIGN_RECOMMENDATIONS.md`.

- Raw reachable stacked definitions: **287**
- Exact reviewed composites: **212**
- Unreviewed reachable definitions: **75**
- Unreviewed inheritance families: **56**

The buckets describe why automatic consolidation is unsafe. They do not
decide the eventual damage family.
A bold family label is an unreviewed planning root; only the definitions
listed after the colon remain open decisions.

| decision bucket | families | definitions |
|---|---:|---:|
| target and state routing | 2 | 2 |
| target routing | 16 | 21 |
| state delivery | 10 | 17 |
| legacy compatibility | 1 | 1 |
| numbered warhead key | 1 | 1 |
| no special mechanical signal | 26 | 33 |

## Target And State Routing (2 families)

- **`RA160mmE_rad_elite`** (1; route mixed, state or integrity): `RA160mmE_rad_elite`
  - active users: RedAlert2 / Soviets: Siege Chopper (`ra2_soviets_siegechopper`)
  - mains `Chemical_Light + Concussion_Medium + Demolition_Light + Nuclear_Super`: `RA160mmE_rad_elite`
- **`SandmarineTuskFire`** (1; route mixed, state or integrity): `SandmarineTuskFire`
  - active users: RedAlert2Mod / TKM: Big Shiee (`tkm_bigshiee`); RedAlert2Mod / TKM: Sand Marine (`tkm_sandmarine`)
  - mains `Concussion_Medium + Flame_Light + MissileAP_Light + MissileHE_Heavy`: `SandmarineTuskFire`

## Target Routing (16 families)

- **`SteelVulcan`** (4; route mixed): `SteelVulcan`, `SteelVulcanResonance`, `SteelVulcanResonanceBounce1`, `SteelVulcanResonanceBounce2`
  - active users: RedAlert2Mod / Consortium: Federation Cougar (`cougar.steel`); RedAlert2Mod / Consortium: Consortium Sentry Turret (`steelconsortium_consortiumsentryturret`)
  - transitive delivery: `SteelVulcanResonance`, `SteelVulcanResonanceBounce1`
  - mains `Bullet_Light + Bullet_Medium + CannonHE_Heavy`: `SteelVulcan`, `SteelVulcanResonance`, `SteelVulcanResonanceBounce1`, `SteelVulcanResonanceBounce2`
- **`ArmoredCarMG_AA`** (2; legacy bridge, route mixed): `ArmoredCarMGAAWaveforce`, `ArmoredCarMG_AA`
  - active users: RedAlert / Japan: Armored Car (`japan_armoredcar`)
  - mains `ArmoredCarGroundCompatibility + Bullet_Medium`: `ArmoredCarMG_AA`
  - mains `ArmoredCarGroundCompatibility + Bullet_Medium + Railgun_Heavy`: `ArmoredCarMGAAWaveforce`
- **`GradRockets`** (2; legacy bridge, route mixed): `GradHeavyRockets`, `GradRockets`
  - active users: RedAlert / Soviets: Grad (`ra1_soviets_grad`)
  - mains `Concussion_Medium + MissileHE_Heavy`: `GradRockets`
  - mains `Concussion_Medium + MissileHE_Heavy + MissileHE_HeavyFlatCompatibility`: `GradHeavyRockets`
- **`CabalHeavyReaperMissiles`** (1; route mixed): `CabalHeavyReaperMissiles`
  - active users: TiberianSun / CABAL: Heavy Reaper (`cabal_heavyreaper`)
  - mains `Concussion_Medium + Demolition_Light + MissileHE_Heavy + MissileHE_Medium`: `CabalHeavyReaperMissiles`
- **`CabalReaperMissiles`** (1; route mixed): `CabalReaperMissiles`
  - active users: TiberianSun / CABAL: Cyborg Reaper (`cabal_cyborgreaper`)
  - mains `Concussion_Medium + Demolition_Light + MissileHE_Light + MissileHE_Medium`: `CabalReaperMissiles`
- **`Future_Cryocopter_Rocket`** (1; route mixed): `Future_Cryocopter_Rocket`
  - active users: RedAlert2Mod / FutureTech: Cryocopter (`futuretech_cryocopter`)
  - mains `FutureCryocopterMissileAP_Medium + MissileAP_Heavy + MissileAP_Medium`: `Future_Cryocopter_Rocket`
- **`GLBarrelExplode`** (1; numbered, route mixed): `GLBarrelExplode`
  - active users: shared rules: Ammo Box (`AMMOBOX1`); shared rules: Ammo Box (`AMMOBOX2`); shared rules: Ammo Box (`AMMOBOX3`); shared rules: Explosive Barrel (`BARL`); shared rules: Explosive Barrel (`BRL3`); shared rules: Fire Department 2000 (`C2KFIREDEPARTMENT`); shared rules: Industrial 2000 (`C2KINDUSTRY`); shared rules: Nuclear Power Plant 2000 (`C2KNUKE`); and 552 more actors
  - mains `1Dam + Concussion_Medium + Demolition_Heavy`: `GLBarrelExplode`
- **`GuardianShoot`** (1; route mixed): `GuardianShoot`
  - active users: StarCraft / Zerg: Guardian (`zerg_guardian`)
  - mains `Concussion_Light + Concussion_Medium`: `GuardianShoot`
- **`HMG_fremen`** (1; numbered, route mixed): `HMG_fremen`
  - active users: D2k / Shared: Fremen Warrior (`fremen_creep`)
  - mains `1Dam + Bullet_Light + Bullet_Medium`: `HMG_fremen`
- **`NaxCorrosionRocketTrooper_elite`** (1; legacy bridge, route mixed): `NaxCorrosionRocketTrooper_elite`
  - active users: RedAlert2Mod / SchwarzerMond: Lunar Rocket (`schwarzermond_lunarrocket`)
  - mains `PreservedFlat_Concussion_Light + PreservedFlat_HeavyMissile + PreservedFlat_MissileAP_Heavy + PreservedFlat_MissileAP_Medium`: `NaxCorrosionRocketTrooper_elite`
- **`RashidanGun_upgrade`** (1; legacy bridge, route mixed): `RashidanGun_upgrade`
  - active users: D2k / Ixian: Rashidan (`heavy_inf.ixian`)
  - mains `Bullet_Light + Bullet_Medium + CannonHE_Heavy + MissileHE_Heavy + RashidanGroundCompatibility`: `RashidanGun_upgrade`
- **`SteelHoverMissile_elite`** (1; route mixed): `SteelHoverMissile_elite`
  - active users: RedAlert2Mod / Consortium: Federation Hummer (`hummer.steel`)
  - mains `ArrowWeapon + MissileAP_Light`: `SteelHoverMissile_elite`
- **`TS30mmRail`** (1; legacy bridge, route mixed): `TS30mmRail`
  - active users: TiberianSun / GDI: Falcon Enforcer (`ts_gdi_falconenforcer`)
  - mains `Flak_Medium + TS30mmRailUnscopedCompatibility`: `TS30mmRail`
- **`TSAegisMissile`** (1; numbered, route mixed): `TSAegisMissile`
  - active users: shared rules: Aegis Cruiser (`tsaegis`)
  - mains `1Dam + LightMissile + MediumMissile`: `TSAegisMissile`
- **`Tentacle`** (1; legacy bridge, route mixed): `Tentacle`
  - active users: StarCraft / Zerg: Creep Colony (`zerg_creepcolony`); StarCraft / Zerg: Creep Colony (`zerg_creepcolony_defense`); StarCraft / Zerg: Spore Colony (`zerg_sporecolony`); StarCraft / Zerg: Sunken Colony (`zerg_sunkencolony_defense`)
  - mains `CannonHE_Heavy + Melee_HeavyFlatCompatibility`: `Tentacle`
- **`v1rockets`** (1; route mixed): `v1rockets`
  - active users: RedAlert / Soviets: V1 Rocket Truck (`ra1_soviets_v1rockettruck`)
  - mains `Demolition_Light + MissileHE_Medium`: `v1rockets`

## State Delivery (10 families)

- **`NaxGrilleArty`** (4; state or integrity): `Lunar_GreenGrilleArty`, `Lunar_GreenGrilleArty_elite`, `NaxGrilleArty`, `NaxGrilleArty_elite`
  - active users: RedAlert2Mod / Naxis: Grille (`naxis_grille`); RedAlert2Mod / Naxis: Naxi Bunker (`naxis_naxibunker`); RedAlert2Mod / Naxis: Shoe Karn (`naxis_shoekarn`); RedAlert2Mod / SchwarzerMond: Lunar Grille (`schwarzermond_lunargrille`)
  - mains `CannonHE_Heavy + Concussion_Medium + Demolition_Light`: `NaxGrilleArty`, `NaxGrilleArty_elite`
  - mains `CannonHE_Heavy + Concussion_Medium + Demolition_Light + Tesla_Heavy`: `Lunar_GreenGrilleArty`, `Lunar_GreenGrilleArty_elite`
- **`HammerTankCannon`** (2; state or integrity): `HammerTankCannon`, `HammerTankCannonThermobaric`
  - active users: RedAlert / Soviets: Hammer Tank (`ra1_soviets_hammertank`)
  - mains `CannonHE_Heavy + CannonHE_Medium`: `HammerTankCannon`
  - mains `CannonHE_Heavy + CannonHE_Medium + Demolition_Heavy + Flame_Medium`: `HammerTankCannonThermobaric`
- **`KotinCannon`** (2; state or integrity): `KotinCannon`, `KotinCannonThermobaric`
  - active users: RedAlert / Soviets: Kotin Nuclear Tank (`ra1_soviets_kotinnucleartank`)
  - mains `CannonHE_Heavy + CannonHE_Medium`: `KotinCannon`
  - mains `CannonHE_Heavy + CannonHE_Medium + Demolition_Heavy + Flame_Medium`: `KotinCannonThermobaric`
- **`NaxSturmArty`** (2; state or integrity): `Lunar_GreenSturmArty`, `NaxSturmArty`
  - active users: RedAlert2Mod / Naxis: Sturm Tiger (`naxis_sturmtiger`); RedAlert2Mod / SchwarzerMond: Sturm Cannon (`schwarzermond_sturmcannon`)
  - mains `CannonHE_Medium + Demolition_Heavy + Demolition_Light`: `NaxSturmArty`
  - mains `CannonHE_Medium + Demolition_Heavy + Demolition_Light + Tesla_Heavy`: `Lunar_GreenSturmArty`
- **`SkyHawkCannon`** (2; state or integrity): `SkyHawkCannon`, `SkyHawkPlasmaCannon`
  - active users: RedAlert / Japan: Sky Hawk (`japan_skyhawk`)
  - mains `CannonAP_Light + Concussion_Medium`: `SkyHawkCannon`
  - mains `CannonAP_Light + Concussion_Medium + MissileAP_Medium + Tesla_Heavy`: `SkyHawkPlasmaCannon`
- **`GrenadeRA`** (1; state or integrity): `GrenadeRA`
  - active users: RedAlert / Soviets: Soviet Grenadier (`ra1_soviets_grenadier`)
  - mains `Demolition_Light + Flame_Light`: `GrenadeRA`
- **`LightTank2Missiles`** (1; state or integrity): `LightTank2Missiles`
  - active users: TiberianDawn / Nod: Light Tank Mk. II (`td_nod_lighttankmkii`)
  - mains `Flame_Light + MissileAP_Medium`: `LightTank2Missiles`
- **`TSChem120mmx`** (1; legacy bridge, numbered, state or integrity): `TSChem120mmx`
  - active users: TiberianSun / Forgotten: forgotten_experimentalmammothtank
  - mains `1Dam + CannonChem_HeavyFlatCompatibility + CannonHE_Medium`: `TSChem120mmx`
- **`TSSonicZapWeapon`** (1; state or integrity): `TSSonicZapWeapon`
  - active users: TiberianSun / GDI: Disruptor (`ts_gdi_disruptor`)
  - mains `Magic_Heavy + Tesla_Heavy`: `TSSonicZapWeapon`
- **`Type97PlasmaCannon`** (1; state or integrity): `Type97PlasmaCannon`
  - active users: RedAlert / Japan: Chi-Ha Heavy Tank (`japan_chihaheavytank`)
  - mains `CannonHE_Heavy + Railgun_Heavy + Tesla_Heavy`: `Type97PlasmaCannon`

## Legacy Compatibility (1 family)

- **`facedancer_grenade`** (1; legacy bridge): `facedancer_grenade`
  - active users: D2k / Ordos: Face Dancer (`ordos_facedancer`)
  - mains `CannonHE_Heavy + MissileAP_HeavyFlatCompatibility`: `facedancer_grenade`

## Numbered Warhead Key (1 family)

- **`TS120mmx`** (1; numbered): `TS120mmx`
  - active users: TiberianSun / Forgotten: forgotten_experimentalmammothtank
  - mains `1Dam + CannonHE_Medium + Concussion_Medium`: `TS120mmx`

## No Special Mechanical Signal (26 families)

- **`LatinBuggyChaingun`** (2; none detected): `LatinBuggyChaingun`, `LatinBuggyChaingun_elite`
  - active users: RedAlert2Mod / Syndicate: Raider Buggy (`latinsyndicate_raiderbuggy`); RedAlert2Mod / Syndicate: Tortuga Tank (`latinsyndicate_tortugatank`)
  - mains `Bullet_Light + Bullet_Medium + CannonAP_Light + Flak_Medium`: `LatinBuggyChaingun`, `LatinBuggyChaingun_elite`
- **`LatinBuggyRocket`** (2; none detected): `LatinBuggyRocket`, `LatinBuggyRocket_elite`
  - active users: RedAlert2Mod / Syndicate: Raider Buggy (`latinsyndicate_raiderbuggy`); RedAlert2Mod / Naxis: Nokana (`naxis_nokana`)
  - mains `Concussion_Medium + Demolition_Light + MissileAP_Light + MissileAP_Medium`: `LatinBuggyRocket`, `LatinBuggyRocket_elite`
- **`SCScourgeDroneExplosion`** (2; none detected): `SCScourgeDroneExplosion`, `ScourgeDroneExplosion`
  - active users: shared rules: Scourge Drone (`SCSCOURGEDRONE`)
  - mains `Concussion_Medium + Demolition_Heavy`: `SCScourgeDroneExplosion`, `ScourgeDroneExplosion`
- **`SCScourgeExplosion`** (2; air only): `SCScourgeExplosion`, `ScourgeExplosion`
  - active users: StarCraft / Zerg: Scourge (`zerg_scourge`)
  - mains `Concussion_Medium + Demolition_Heavy`: `SCScourgeExplosion`, `ScourgeExplosion`
- **`TSTacticalMissileDamage`** (2; none detected): `TSTacticalChemMissileDamage`, `TSTacticalMissileDamage`
  - active users: shared rules: Casino Crate (`casinocrate`); TiberianSun / Nod: Missile Silo (`ts_nod_missilesilo`)
  - transitive delivery: `TSTacticalChemMissile`, `TSTacticalMissile`
  - mains `LightMissile + MediumMissile`: `TSTacticalChemMissileDamage`, `TSTacticalMissileDamage`
- **`d2k_air_drone_guns`** (2; none detected): `d2k_air_drone_guns`, `d2k_air_drone_guns_upgrade`
  - active users: D2k / Ixian: Ixian Air Drone (`ixian_airdrone`)
  - mains `Bullet_Light + Bullet_Medium + CannonHE_Heavy + MissileAP_Heavy`: `d2k_air_drone_guns_upgrade`
  - mains `Bullet_Light + Bullet_Medium + MissileAP_Heavy`: `d2k_air_drone_guns`
- **`tkmjuggap`** (2; none detected): `tkmjuggap`, `tkmtechnicalmgap`
  - active users: RedAlert2Mod / TKM: Juggernaut (`tkm_juggernaut`); RedAlert2Mod / TKM: TKM Technical (`tkm_technical`)
  - mains `Bullet_Light + Demolition_Light`: `tkmjuggap`, `tkmtechnicalmgap`
- **`110mm_Gun`** (1; none detected): `110mm_Gun`
  - active users: D2k / Ixian: Gun Turret (`ixian_gunturret`)
  - mains `CannonAP_Light + CannonHE_Heavy + CannonHE_Medium`: `110mm_Gun`
- **`AlliedTankDestroyerCannon`** (1; none detected): `AlliedTankDestroyerCannon`
  - active users: RedAlert / Allies: Allied Tank Destroyer (`ra1_allies_alliedtankdestroyer`)
  - mains `CannonAP_Light + CannonHE_Medium`: `AlliedTankDestroyerCannon`
- **`Aphid_AA`** (1; none detected): `Aphid_AA`
  - active users: RedAlert / Allies: Rapier Jumpjet (`ra1_allies_rapierjumpjet`)
  - mains `Concussion_Medium + MissileHE_Heavy`: `Aphid_AA`
- **`GlaveCanon`** (1; none detected): `GlaveCanon`
  - active users: StarCraft / Protoss: Adept (`protoss_adept`)
  - mains `Demolition_Light + Railgun_Heavy`: `GlaveCanon`
- **`JimRaynorMachineGun`** (1; none detected): `JimRaynorMachineGun`
  - active users: StarCraft / Terran: Jim Raynor (`terran_jimraynor`); StarCraft / Terran: Pythean (`terran_pythean`)
  - mains `CannonHE_Heavy + MissileHE_Heavy`: `JimRaynorMachineGun`
- **`RA2Terrorist`** (1; none detected): `RA2Terrorist`
  - active users: shared rules: Eden Starflare Lynx (`EDEN_LYNX_STARFLARE`); shared rules: Eden Starflare Tiger (`EDEN_TIGER_STARFLARE`); TiberianSun / CABAL: Enlighted (`cabal_enlighted`); RedAlert2Mod / Syndicate: Terrorist (`latinsyndicate_terrorist`); RedAlert2 / Shared: Bomb Car (`ra2_ambu_demo`); RedAlert2 / Shared: Bomb Car (`ra2_bcab_demo`); RedAlert2 / Shared: Bomb Car (`ra2_bus_demo`); RedAlert2 / Shared: Bomb Car (`ra2_car_demo`); and 17 more actors
  - mains `Concussion_Medium + Demolition_Heavy`: `RA2Terrorist`
- **`SandmarineTuskTwin`** (1; none detected): `SandmarineTuskTwin`
  - active users: RedAlert2Mod / TKM: Big Shiee (`tkm_bigshiee`); RedAlert2Mod / TKM: Sand Marine (`tkm_sandmarine`)
  - mains `Bullet_Medium + Concussion_Medium + Grenade + MissileAP_Medium + MissileHE_Heavy`: `SandmarineTuskTwin`
- **`ScoutMG`** (1; none detected): `ScoutMG`
  - active users: StarCraft / Protoss: Scout (`protoss_scout`)
  - mains `Demolition_Light + Flak_Medium`: `ScoutMG`
- **`SheridanCannon`** (1; none detected): `SheridanCannon`
  - active users: RedAlert / Allies: Sheridan Assault Tank (`ra1_allies_sheridanassaulttank`)
  - mains `CannonAP_Light + CannonHE_Medium`: `SheridanCannon`
- **`SheridanMissiles`** (1; none detected): `SheridanMissiles`
  - active users: RedAlert / Allies: Sheridan Assault Tank (`ra1_allies_sheridanassaulttank`)
  - mains `MissileHE_Light + MissileHE_Medium`: `SheridanMissiles`
- **`SiegeTankCannon`** (1; none detected): `SiegeTankCannon`
  - active users: StarCraft / Terran: Siege Tank (`terran_siegetank`)
  - mains `CannonAP_Light + CannonHE_Heavy + CannonHE_Medium`: `SiegeTankCannon`
- **`TSBoatcannon`** (1; none detected): `TSBoatcannon`
  - active users: TiberianSun / Forgotten: Cannon Tug (`forgotten_cannonboat`)
  - mains `Concussion_Medium + Demolition_Heavy`: `TSBoatcannon`
- **`TSBomb`** (1; none detected): `TSBomb`
  - active users: TiberianSun / GDI: Orca Bomber (`ts_gdi_orcabomber`); TiberianSun / GDI: Strike Orca (`ts_gdi_strike_orca`)
  - mains `Concussion_Medium + Demolition_Heavy`: `TSBomb`
- **`TigerCannon`** (1; none detected): `TigerCannon`
  - active users: RedAlert / Shared: Allied Cyber Tank (`ra1_allies_alliedcybertank`); RedAlert / Allies: Allied Tiger Heavy Tank (`ra1_allies_alliedtigerheavytank`)
  - mains `CannonHE_Heavy + CannonHE_Medium`: `TigerCannon`
- **`Type97Cannon`** (1; none detected): `Type97Cannon`
  - active users: RedAlert / Japan: Chi-Ha Heavy Tank (`japan_chihaheavytank`)
  - mains `CannonHE_Heavy + CannonHE_Medium`: `Type97Cannon`
- **`YakovlevCannon`** (1; none detected): `YakovlevCannon`
  - active users: RedAlert2Mod / Syndicate: Yakovlev (`latinsyndicate_yakovlev`)
  - mains `Bullet_Medium + CannonAP_Light + CannonHE_Heavy + Flak_Medium`: `YakovlevCannon`
- **`YakovlevCannon_elite`** (1; none detected): `YakovlevCannon_elite`
  - active users: RedAlert2Mod / Syndicate: Yakovlev (`latinsyndicate_yakovlev`)
  - mains `Bullet_Medium + CannonAP_Light + CannonHE_Heavy + Flak_Medium`: `YakovlevCannon_elite`
- **`ordos_autogunturret`** (1; none detected): `ordos_autogunturret`
  - active users: D2k / Ordos: Autogun Turret (`ordos_autogunturret`)
  - mains `Bullet_Light + Bullet_Medium + CannonHE_Heavy`: `ordos_autogunturret`
- **`t30shell`** (1; none detected): `t30shell`
  - active users: RedAlert2Mod / TKM: T-30 (`tkm_t30`)
  - mains `Demolition_Heavy + Railgun_Heavy`: `t30shell`

## Maintainer decision shape

For each family, the eventual question is: which authored main defines the unit's role, and may its armor, splash, target route, and state delivery be applied to the full nominal damage? Paid replacements and mixed target routes must be reviewed as complete closures.
