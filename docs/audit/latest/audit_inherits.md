EXIT CODE: 0

# audit_inherits — §10.3 invariant violations (B2)

Actors+templates scanned: **3890**

| violation | meaning | count |
|---|---|---|
| V1 | concrete actor inherits from concrete actor | 317 |
| V2 | inherit crosses faction ownership | 16 |
| V3 | dangling inherit target (BLOCKING) | 0 |
| V4 | chain depth > 3 | 1658 |
| V5 | > 2 -Trait removals (warning) | 95 |


## V3 — dangling inherit targets (blocking)

_none found_


## V2 — cross-faction inherits (concrete targets)

| actor | actor faction | target | target faction | file |
|---|---|---|---|---|
| asian_alliance_concretebarrier | redalert2mod/asianalliance | BRIK | tiberiandawn/shared | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/buildings.yaml |
| carryall_reinforce.ordos | d2k/ordos | carryall.reinforce | d2k/shared | mods/cameo/ContentPacks/D2k/Ordos/yaml/aircraft.yaml |
| engineer | d2k/shared | E6 | tiberiandawn/shared | mods/cameo/ContentPacks/D2k/Shared/yaml/infantry.yaml |
| forgotten_helipad | tiberiansun/forgotten | ts_gdi_helipad | tiberiansun/gdi | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/buildings.yaml |
| forgotten_mobileconstructionvehicle | tiberiansun/forgotten | ts_gdi_mobileconstructionvehicle | tiberiansun/gdi | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/vehicles.yaml |
| forgotten_scarabapc | tiberiansun/forgotten | ts_nod_subterraneanapc | tiberiansun/nod | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/vehicles.yaml |
| forgotten_servicedepot | tiberiansun/forgotten | ts_gdi_servicedepot | tiberiansun/gdi | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/buildings.yaml |
| ixian_advancedheavyfactory | d2k/ixian | heavy_factory | d2k/shared | mods/cameo/ContentPacks/D2k/Ixian/yaml/buildings.yaml |
| ordos_advancedcarryall | d2k/ordos | ixian_autonomouscarryall | d2k/shared | mods/cameo/ContentPacks/D2k/Ordos/yaml/aircraft.yaml |
| ordos_heavyfactory | d2k/ordos | heavy_factory | d2k/shared | mods/cameo/ContentPacks/D2k/Ordos/yaml/buildings.yaml |
| ordos_lightfactory | d2k/ordos | light_factory | d2k/shared | mods/cameo/ContentPacks/D2k/Ordos/yaml/buildings.yaml |
| td_gdi_chinooktransport | tiberiandawn/gdi | TRAN | tiberiandawn/shared | mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/aircraft.yaml |
| td_nod_chinooktransport | tiberiandawn/nod | TRAN | tiberiandawn/shared | mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/aircraft.yaml |
| ts_nod_mobileconstructionvehicle | tiberiansun/nod | ts_gdi_mobileconstructionvehicle | tiberiansun/gdi | mods/cameo/ContentPacks/TiberianSun/Nod/yaml/vehicles.yaml |
| ts_nod_servicedepot | tiberiansun/nod | ts_gdi_servicedepot | tiberiansun/gdi | mods/cameo/ContentPacks/TiberianSun/Nod/yaml/buildings.yaml |
| ts_nod_tiberiumrefinery | tiberiansun/nod | ts_gdi_tiberiumrefinery | tiberiansun/gdi | mods/cameo/ContentPacks/TiberianSun/Nod/yaml/buildings.yaml |


## V1 — concrete → concrete inherits

| actor | target | actor faction | target faction | file |
|---|---|---|---|---|
| A10.Husk | MIG.Husk | ? | ? | mods/cameo/rules/husks.yaml |
| A10Carrier.Husk | A10.Husk | ? | ? | mods/cameo/rules/husks.yaml |
| BADR.Allies | BADR | ? | ? | mods/cameo/rules/redalert.yaml |
| BADR.Bomber | BADR.Soviet | ? | ? | mods/cameo/rules/redalert.yaml |
| BADR.Japan | BADR | ? | ? | mods/cameo/rules/redalert.yaml |
| BADR.Soviet | BADR | ? | ? | mods/cameo/rules/redalert.yaml |
| C17.Bomber | BADR.Bomber | ? | ? | mods/cameo/rules/redalert.yaml |
| C17.Paradrop | BADR | ? | ? | mods/cameo/rules/redalert.yaml |
| CAMERA.sw | CAMERA.small | ? | ? | mods/cameo/rules/misc.yaml |
| CNCSPEN | RASPEN | tiberiandawn/nod | ? | mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/buildings.yaml |
| CNCSYRD | RA1SYRD | tiberiandawn/gdi | ? | mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/buildings.yaml |
| ChronoVortexFade | ChronoVortex | ? | ? | mods/cameo/rules/redalert.yaml |
| E1 | td_gdi_minigunner | tiberiandawn/gdi | tiberiandawn/gdi | mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/infantry.yaml |
| E3 | td_gdi_rocketsoldier | tiberiandawn/gdi | tiberiandawn/gdi | mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/infantry.yaml |
| EDEN_TIGER_ACIDCLOUD | EDEN_LYNX_ACIDCLOUD | ? | ? | mods/cameo/rules/outpost2.yaml |
| ForceShieldDrainer | CAMERA.small | ? | ? | mods/cameo/rules/shared.yaml |
| INVISIBLEPLANE | BADR | ? | ? | mods/cameo/rules/tiberiansun.yaml |
| MONEYCRATE.LARGE | MONEYCRATE | ? | ? | mods/cameo/rules/misc.yaml |
| OILB.TS | OILB.Building | ? | ? | mods/cameo/rules/tiberiansun.yaml |
| OILB.d2k | OILB.Building | d2k/shared | ? | mods/cameo/ContentPacks/D2k/Shared/yaml/buildings.yaml |
| PLYMOUTH_TIGER_EMP | PLYMOUTH_LYNX_EMP | ? | ? | mods/cameo/rules/outpost2.yaml |
| PLYMOUTH_TIGER_ESG | PLYMOUTH_LYNX_ESG | ? | ? | mods/cameo/rules/outpost2.yaml |
| PLYMOUTH_TIGER_MICROWAVE | PLYMOUTH_LYNX_MICROWAVE | ? | ? | mods/cameo/rules/outpost2.yaml |
| PLYMOUTH_TIGER_RPG | PLYMOUTH_LYNX_RPG | ? | ? | mods/cameo/rules/outpost2.yaml |
| PLYMOUTH_TIGER_STARFLARE | PLYMOUTH_LYNX_STARFLARE | ? | ? | mods/cameo/rules/outpost2.yaml |
| PLYMOUTH_TIGER_STICKYFOAM | PLYMOUTH_LYNX_STICKYFOAM | ? | ? | mods/cameo/rules/outpost2.yaml |
| PLYMOUTH_TIGER_SUPERNOVA | PLYMOUTH_LYNX_SUPERNOVA | ? | ? | mods/cameo/rules/outpost2.yaml |
| RABIO | bio | ? | ? | mods/cameo/rules/tech.yaml |
| RAE6 | E6 | ? | tiberiandawn/shared | mods/cameo/rules/redalert.yaml |
| RAMISS | MISS | ? | ? | mods/cameo/rules/tech.yaml |
| SCBARRACKSM | SCCOMMANDCENTERM | ? | ? | mods/cameo/rules/starcraft.yaml |
| SCENGINEERINGBAYM | SCCOMMANDCENTERM | ? | ? | mods/cameo/rules/starcraft.yaml |
| SCFACTORYM | SCCOMMANDCENTERM | ? | ? | mods/cameo/rules/starcraft.yaml |
| SCSCIENCEFACILITYM | SCCOMMANDCENTERM | ? | ? | mods/cameo/rules/starcraft.yaml |
| SCSCOURGEDRONE | zerg_scourge | ? | ? | mods/cameo/rules/starcraft.yaml |
| SCSENTINELM | SCCOMMANDCENTERM | ? | ? | mods/cameo/rules/starcraft.yaml |
| SCSTARPORTM | SCCOMMANDCENTERM | ? | ? | mods/cameo/rules/starcraft.yaml |
| TECHBCANNON2 | TECHBCANNON | ? | ? | mods/cameo/rules/tech.yaml |
| TSE1PARA | TSE1 | ? | ? | mods/cameo/rules/tiberiansun.yaml |
| TSE2PARA | ts_gdi_discthrower | ? | tiberiansun/gdi | mods/cameo/rules/tiberiansun.yaml |
| TSENGINEER | E6 | ? | tiberiandawn/shared | mods/cameo/rules/tiberiansun.yaml |
| U3 | U2 | ? | ? | mods/cameo/rules/redalert.yaml |
| WWCRATE | CRATE | ? | ? | mods/cameo/rules/misc.yaml |
| ambiance_battle | ambiance_wind | ? | ? | mods/cameo/rules/misc.yaml |
| ambiance_bird | ambiance_wind | ? | ? | mods/cameo/rules/misc.yaml |
| ambiance_bird_robin | ambiance_wind | ? | ? | mods/cameo/rules/misc.yaml |
| ambiance_ocean_calm | ambiance_wind | ? | ? | mods/cameo/rules/misc.yaml |
| ambiance_ocean_waves | ambiance_wind | ? | ? | mods/cameo/rules/misc.yaml |
| ambiance_rumbling | ambiance_wind | ? | ? | mods/cameo/rules/misc.yaml |
| asian_alliance_concretebarrier | BRIK | redalert2mod/asianalliance | tiberiandawn/shared | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/buildings.yaml |
| asian_alliance_engineer | ra2_allies_engineer | redalert2mod/asianalliance | ? | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/infantry.yaml |
| asian_alliance_heavyrailguntank | asian_alliance_railguntank | redalert2mod/asianalliance | redalert2mod/asianalliance | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/vehicles.yaml |
| asian_alliance_oiltruck | ra1_soviet_nukedemotruck | redalert2mod/asianalliance | ? | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/vehicles.yaml |
| bbomb2_husk.nax2 | bbomb_husk.nax2 | redalert2mod/schwarzermond | redalert2mod/schwarzermond | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/aircraft.yaml |
| bbomb3_husk.nax2 | bbomb_husk.nax2 | redalert2mod/schwarzermond | redalert2mod/schwarzermond | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/aircraft.yaml |
| bomber_husk.asian | BADR.Husk | redalert2mod/asianalliance | ? | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/aircraft.yaml |
| bomber_minebomb.asian | BADR | redalert2mod/asianalliance | ? | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/aircraft.yaml |
| bomber_minebomb2.asian | bomber_minebomb.asian | redalert2mod/asianalliance | redalert2mod/asianalliance | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/aircraft.yaml |
| cabal_artilleryspider_backup | cabal_artilleryspider | ? | tiberiansun/cabal | mods/cameo/rules/tiberiansun.yaml |
| cabal_avatar_backup | cabal_avatar | ? | tiberiansun/cabal | mods/cameo/rules/tiberiansun.yaml |
| cabal_legion_backup | cabal_legion | ? | tiberiansun/cabal | mods/cameo/rules/tiberiansun.yaml |
| cabal_manticore_backup | cabal_manticore | ? | tiberiansun/cabal | mods/cameo/rules/tiberiansun.yaml |
| cabal_tarantula_backup | cabal_tarantula | ? | tiberiansun/cabal | mods/cameo/rules/tiberiansun.yaml |
| camera.paradrop | RACAMERA | ? | ? | mods/cameo/rules/misc.yaml |
| camera.placeholderhack | CAMERA.small | ? | ? | mods/cameo/rules/misc.yaml |
| camera.psireveal | camera.scan | ? | ? | mods/cameo/rules/misc.yaml |
| camera.ra2spy | CAMERA.small | ? | ? | mods/cameo/rules/shared.yaml |
| camera.radarvan | camera.scan | ? | ? | mods/cameo/rules/misc.yaml |
| camera.sathack | camera.paradrop | ? | ? | mods/cameo/rules/misc.yaml |
| camera.spyplane | camera.scan | ? | ? | mods/cameo/rules/misc.yaml |
| camera.spysat | camera.scan | ? | ? | mods/cameo/rules/misc.yaml |
| carryall.paradrop | carryall.reinforce | d2k/shared | d2k/shared | mods/cameo/ContentPacks/D2k/Shared/yaml/aircraft.yaml |
| carryall_reinforce.ordos | carryall.reinforce | d2k/ordos | d2k/shared | mods/cameo/ContentPacks/D2k/Ordos/yaml/aircraft.yaml |
| concreteadefense | concreteabuilding | d2k/shared | d2k/shared | mods/cameo/ContentPacks/D2k/Shared/yaml/buildings.yaml |
| concretebbuilding | concreteabuilding | d2k/shared | d2k/shared | mods/cameo/ContentPacks/D2k/Shared/yaml/buildings.yaml |
| concretebdefense | concretebbuilding | d2k/shared | d2k/shared | mods/cameo/ContentPacks/D2k/Shared/yaml/buildings.yaml |
| corpse_big.nax | corpse.nax | redalert2mod/naxis | redalert2mod/naxis | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/vehicles.yaml |
| deathcash.latin | RACAMERA | redalert2mod/syndicate | ? | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/upgrades.yaml |
| deathcash_small.latin | RACAMERA | redalert2mod/syndicate | ? | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/upgrades.yaml |
| engineer | E6 | d2k/shared | tiberiandawn/shared | mods/cameo/ContentPacks/D2k/Shared/yaml/infantry.yaml |
| forgotten_engineer | TSENGINEER | tiberiansun/forgotten | ? | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/infantry.yaml |
| forgotten_ghoststalker_r4 | forgotten_ghoststalker | tiberiansun/forgotten | tiberiansun/forgotten | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/infantry.yaml |
| forgotten_ghoststalker_sp | forgotten_ghoststalker | tiberiansun/forgotten | tiberiansun/forgotten | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/infantry.yaml |
| forgotten_helipad | ts_gdi_helipad | tiberiansun/forgotten | tiberiansun/gdi | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/buildings.yaml |
| forgotten_mobileconstructionvehicle | ts_gdi_mobileconstructionvehicle | tiberiansun/forgotten | tiberiansun/gdi | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/vehicles.yaml |
| forgotten_mutant_sp | forgotten_mutant | tiberiansun/forgotten | tiberiansun/forgotten | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/infantry.yaml |
| forgotten_mutant_wild | forgotten_mutant | ? | tiberiansun/forgotten | mods/cameo/rules/tiberiansun.yaml |
| forgotten_mutantsniper | forgotten_mutant | tiberiansun/forgotten | tiberiansun/forgotten | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/infantry.yaml |
| forgotten_mutantsniper_r4 | forgotten_mutantsniper | tiberiansun/forgotten | tiberiansun/forgotten | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/infantry.yaml |
| forgotten_mutantsniper_sp | forgotten_mutantsniper | tiberiansun/forgotten | tiberiansun/forgotten | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/infantry.yaml |
| forgotten_mutantsoldier_sp | forgotten_mutantsoldier | tiberiansun/forgotten | tiberiansun/forgotten | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/infantry.yaml |
| forgotten_rocketinfantry | TSE3 | tiberiansun/forgotten | ? | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/infantry.yaml |
| forgotten_scarabapc | ts_nod_subterraneanapc | tiberiansun/forgotten | tiberiansun/nod | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/vehicles.yaml |
| forgotten_servicedepot | ts_gdi_servicedepot | tiberiansun/forgotten | tiberiansun/gdi | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/buildings.yaml |
| forgotten_tiberianfiend_wild | forgotten_tiberianfiend | tiberiansun/forgotten | tiberiansun/forgotten | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/infantry.yaml |
| forgotten_tiberiumspike | OILB.TS | tiberiansun/forgotten | ? | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/buildings.yaml |
| frigate.paradrop | frigate | d2k/shared | d2k/shared | mods/cameo/ContentPacks/D2k/Shared/yaml/aircraft.yaml |
| futuretech_concretebarrier | BRIK | ? | tiberiandawn/shared | mods/cameo/rules/redalert2.yaml |
| futuretech_engineer | ra2_allies_engineer | redalert2mod/futuretech | ? | mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/infantry.yaml |
| futuretech_salamanderifv | ra2_allies_ifv | redalert2mod/futuretech | ? | mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/vehicles.yaml |
| hole_small.nax2 | hole.nax2 | redalert2mod/schwarzermond | redalert2mod/schwarzermond | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/infantry.yaml |
| horten_bomber.nax | BADR.Soviet | redalert2mod/naxis | ? | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/aircraft.yaml |
| humans_cannontower | humans_humanscouttower | ? | ? | mods/cameo/rules/warcraft2.yaml |
| humans_elvenranger | humans_elvenarcher | ? | ? | mods/cameo/rules/warcraft2.yaml |
| humans_guardtower | humans_humanscouttower | ? | ? | mods/cameo/rules/warcraft2.yaml |
| humans_humangoldmine_2 | humans_humangoldmine | ? | ? | mods/cameo/rules/warcraft2.yaml |
| humans_paladin | humans_knight | ? | ? | mods/cameo/rules/warcraft2.yaml |
| ixian_advancedheavyfactory | heavy_factory | d2k/ixian | d2k/shared | mods/cameo/ContentPacks/D2k/Ixian/yaml/buildings.yaml |
| ixian_autonomouscarryall | carryall.reinforce | d2k/shared | d2k/shared | mods/cameo/ContentPacks/D2k/Shared/yaml/aircraft.yaml |
| japan_archermaiden | japan_tankbuster | ? | ? | mods/cameo/rules/redalert.yaml |
| japan_coreairfield | japan_corewarfactory | ? | ? | mods/cameo/rules/redalert.yaml |
| japan_corebarracks | japan_corewarfactory | ? | ? | mods/cameo/rules/redalert.yaml |
| japan_corepowerplant | japan_corewarfactory | ? | ? | mods/cameo/rules/redalert.yaml |
| japan_coreradar | japan_corewarfactory | ? | ? | mods/cameo/rules/redalert.yaml |
| japan_corerefinery | japan_corewarfactory | ? | ? | mods/cameo/rules/redalert.yaml |
| japan_coreservicedepot | japan_corewarfactory | ? | ? | mods/cameo/rules/redalert.yaml |
| japan_coretechcenter | japan_corewarfactory | ? | ? | mods/cameo/rules/redalert.yaml |
| japan_japaneseairfield | ra1_soviet_airfield | ? | ? | mods/cameo/rules/redalert.yaml |
| japan_japanesewarfactory | ra1_soviet_warfactory | ? | ? | mods/cameo/rules/redalert.yaml |
| jsuperbomber | BADR | ? | ? | mods/cameo/rules/redalert.yaml |
| jsuperbomber.Husk | BADR.Husk | ? | ? | mods/cameo/rules/husks.yaml |
| kami_asdf.asian | kami.asian | redalert2mod/asianalliance | redalert2mod/asianalliance | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/aircraft.yaml |
| kami_chemical.asian | kami.asian | redalert2mod/asianalliance | redalert2mod/asianalliance | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/aircraft.yaml |
| landcarr_drone.futu | ra2hornet | redalert2mod/futuretech | ? | mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/aircraft.yaml |
| latin_syndicate_demolitiontruck | ra1_soviet_nukedemotruck | redalert2mod/syndicate | ? | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/vehicles.yaml |
| latin_syndicate_engineer | ra2_allies_engineer | redalert2mod/syndicate | ? | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/infantry.yaml |
| latin_syndicate_nuketruck | ra1_soviet_nukedemotruck | redalert2mod/syndicate | ? | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/vehicles.yaml |
| latin_syndicate_syndicateconstructionyard | ra2_allies_alliedconstructionyard | redalert2mod/syndicate | ? | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/buildings.yaml |
| modbomber.Husk | YAK.Husk | ? | ? | mods/cameo/rules/husks.yaml |
| modkami.Husk | YAK.Husk | ? | ? | mods/cameo/rules/husks.yaml |
| modkamimini.Husk | YAK.Husk | ? | ? | mods/cameo/rules/husks.yaml |
| naxis_slave | YRSLAV | redalert2mod/naxis | ? | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/infantry.yaml |
| orcs_cannontower | orcs_orcwatchtower | ? | ? | mods/cameo/rules/warcraft2.yaml |
| orcs_guardtower | orcs_orcwatchtower | ? | ? | mods/cameo/rules/warcraft2.yaml |
| orcs_ogremage | orcs_ogre | ? | ? | mods/cameo/rules/warcraft2.yaml |
| orcs_orcgoldmine_2 | orcs_orcgoldmine | ? | ? | mods/cameo/rules/warcraft2.yaml |
| orcs_trollberserker | orcs_trollaxethrower | ? | ? | mods/cameo/rules/warcraft2.yaml |
| orcs_wall | humans_wall | ? | ? | mods/cameo/rules/warcraft2.yaml |
| ordos_advancedcarryall | ixian_autonomouscarryall | d2k/ordos | d2k/shared | mods/cameo/ContentPacks/D2k/Ordos/yaml/aircraft.yaml |
| ordos_heavyfactory | heavy_factory | d2k/ordos | d2k/shared | mods/cameo/ContentPacks/D2k/Ordos/yaml/buildings.yaml |
| ordos_lightfactory | light_factory | d2k/ordos | d2k/shared | mods/cameo/ContentPacks/D2k/Ordos/yaml/buildings.yaml |
| ordos_stealthraider | ordos_raider | d2k/ordos | d2k/ordos | mods/cameo/ContentPacks/D2k/Ordos/yaml/vehicles.yaml |
| ra1_soviet_heavyindustrialminer | ra1_soviet_oretruck | ? | ? | mods/cameo/rules/redalert.yaml |
| ra1_soviet_largefactory | ra1_soviet_warfactory | ? | ? | mods/cameo/rules/redalert.yaml |
| ra1_soviet_largesovietairfield | ra1_soviet_airfield | ? | ? | mods/cameo/rules/redalert.yaml |
| ra2_allies_battlefortress_2 | ra2_allies_battlefortress | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2_allies_battlefortress_3 | ra2_allies_battlefortress | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2_allies_concretebarrier | BRIK | ? | tiberiandawn/shared | mods/cameo/rules/redalert2.yaml |
| ra2_allies_engineer | E6 | ? | tiberiandawn/shared | mods/cameo/rules/redalert2.yaml |
| ra2_allies_ifv_chrono | ra2_allies_ifv_mg | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2_allies_ifv_hmg | ra2_allies_ifv | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2_allies_ifv_mg | ra2_allies_ifv | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2_allies_ifv_missile | ra2_allies_ifv | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2_allies_ifv_repair | ra2_allies_ifv_mg | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2_c_hum2 | ra2_c_hum | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2_city01 | ra2ctchig01 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2_city02 | ra2ctchig01 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2_city03 | ra2ctchig01 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2_city04 | ra2ctchig01 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2_city06 | ra2ctchig01 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2_crate | CRATE | ? | ? | mods/cameo/rules/misc.yaml |
| ra2_ctfrmb | ra2ctbunk01 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2_ctgard01 | ra2ctbunk01 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2_ctgard03 | ra2ctbunk01 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2_ctnwy09 | ra2ctchig01 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2_ctnwy22 | ra2ctchig01 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2_ctnwy23 | ra2ctchig01 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2_ctnwy24 | ra2ctchig01 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2_ctnwy25 | ra2ctchig01 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2_ctpars02 | ra2ctchig01 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2_ctpars04 | ra2ctchig01 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2_ctpars05 | ra2ctchig01 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2_ctpars06 | ra2ctchig01 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2_ctpars07 | ra2ctbunk01 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2_ctpars08 | ra2ctchig01 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2_ctpars09 | ra2ctchig01 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2_ctpars10 | ra2ctchig01 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2_ctpars12 | ra2ctchig01 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2_ctpars13 | ra2ctchig01 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2_ctpars14 | ra2ctchig01 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2_ctrus03 | ra2ctchig01 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2_ctrus04 | ra2ctchig01 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2_ctrus05 | ra2ctchig01 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2_ctrus06 | ra2ctchig01 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2_ctrus07 | ra2ctchig01 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2_ctrus08 | ra2ctchig01 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2_ctrus09 | ra2ctchig01 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2_ctrus10 | ra2ctchig01 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2_ctrus11 | ra2ctchig01 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2_ctsanf01 | ra2ctchig01 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2_ctsanf02 | ra2ctchig01 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2_ctsanf03 | ra2ctchig01 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2_ctsanf05 | ra2ctchig01 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2_ctsanf06 | ra2ctchig01 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2_ctsanf07 | ra2ctchig01 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2_ctsanf08 | ra2ctchig01 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2_ctsanf16 | ra2ctchig01 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2_ctsanf17 | ra2ctchig01 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2_ctsanf18 | ra2ctchig01 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2_cttexs01 | ra2ctchig01 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2_cttexs02 | ra2ctchig01 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2_cttexs03 | ra2ctchig01 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2_cttexs04 | ra2ctchig01 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2_cttexs05 | ra2ctchig01 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2_cttexs06 | ra2ctchig01 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2_cttexs07 | ra2ctchig01 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2_cttexs08 | ra2ctchig01 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2_ctwash03 | ra2ctchig01 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2_ctwash04 | ra2ctchig01 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2_ctwash05 | ra2ctchig01 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2_ctwash06 | ra2ctchig01 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2_ctwash07 | ra2ctchig01 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2_ctwash08 | ra2ctchig01 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2_ctwash09 | ra2ctchig01 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2_ctwash10 | ra2ctchig01 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2_ctwash11 | ra2ctchig01 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2_ctwash13 | ra2ctchig01 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2_ctwash17 | ra2ctchig01 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2_soviets_concretebarrier | BRIK | ? | tiberiandawn/shared | mods/cameo/rules/redalert2.yaml |
| ra2_soviets_constructionyard | ra2_allies_alliedconstructionyard | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2_soviets_engineer | ra2_allies_engineer | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2caairpv | ra2caairp | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2ctarmy01 | ra2ctbunk01 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2ctarmy02 | ra2ctbunk01 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2ctarmy03 | ra2ctarmy02 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2ctarmy04 | ra2ctarmy02 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2ctbarn02 | ra2ctbunk01 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2ctbunk02 | ra2ctbunk01 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2ctchig01 | ra2ctbunk01 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2ctchig02 | ra2ctchig01 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2ctchig03 | ra2ctchig01 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2cteur01 | ra2ctbunk01 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2cteur02 | ra2ctbunk01 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2cteur04 | ra2ctchig01 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2ctfarm01 | ra2ctbunk01 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2ctfarm06 | ra2ctbunk01 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2ctfrma | ra2ctbunk01 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2ctgas01 | ra2ctchig01 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2cthse01 | ra2ctbunk01 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2cthse02 | ra2ctbunk01 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2cthse03 | ra2ctchig01 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2cthse04 | ra2ctbunk01 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2cthse05 | ra2ctbunk01 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2cthse06 | ra2cthse05 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2cthse07 | ra2ctbunk01 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2ctind01 | ra2ctchig01 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2ctlab | ra2ctchig01 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2ctmiam01 | ra2ctchig01 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2ctmiam02 | ra2ctchig01 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2ctmiam03 | ra2ctchig01 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2ctmiam04 | ra2ctbunk01 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2ctmiam05 | ra2ctchig01 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2ctmiam06 | ra2ctmiam05 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2ctmiam07 | ra2ctmiam05 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2ctmiam08 | ra2ctbunk01 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2ctmsc07 | ra2ctbunk01 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2ctmsc08 | ra2ctarmy02 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2ctmsc09 | ra2ctmsc08 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2ctmsc10 | ra2ctchig01 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2ctnewy01 | ra2ctchig01 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2ctnewy06 | ra2ctchig01 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2ctnewy07 | ra2ctchig01 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2ctnewy08 | ra2ctchig01 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2ctnewy10 | ra2ctchig01 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2ctnewy11 | ra2ctchig01 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2ctnewy12 | ra2ctnewy08 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2ctnewy13 | ra2ctnewy08 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2ctnewy14 | ra2ctnewy08 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2ctnewy15 | ra2ctnewy07 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2ctnewy16 | ra2ctnewy08 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2ctnewy17 | ra2ctnewy16 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2ctnewy18 | ra2ctnewy17 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2ctnewy20 | ra2ctchig01 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2ctnewy21 | ra2ctnewy20 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2ctnewy26 | ra2ctchig01 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2e2.black | ra2_soviets_conscript | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2shk.bot | ra2_soviets_teslatrooper | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2shkhero | ra2_soviets_teslatrooper | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2v3rocketelite | ra2v3rocket | ? | ? | mods/cameo/rules/redalert2.yaml |
| scadept.shade | protoss_adept | ? | ? | mods/cameo/rules/starcraft.yaml |
| sietch_creep_disabled | sietch_creep | d2k/shared | d2k/shared | mods/cameo/ContentPacks/D2k/Shared/yaml/buildings.yaml |
| sonar | camera.spyplane | ? | ? | mods/cameo/rules/misc.yaml |
| steel_consortium_consortiumconstructionyard | ra2_allies_alliedconstructionyard | redalert2mod/consortium | ? | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/buildings.yaml |
| steel_consortium_engineer | ra2_allies_engineer | redalert2mod/consortium | ? | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/infantry.yaml |
| td_gdi_chinooktransport | TRAN | tiberiandawn/gdi | tiberiandawn/shared | mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/aircraft.yaml |
| td_gdi_humveemkii | td_gdi_humvee | tiberiandawn/gdi | tiberiandawn/gdi | mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/vehicles.yaml |
| td_nod_buggymkii | td_nod_buggy | tiberiandawn/nod | tiberiandawn/nod | mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/vehicles.yaml |
| td_nod_chinooktransport | TRAN | tiberiandawn/nod | tiberiandawn/shared | mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/aircraft.yaml |
| tkm_engineer | E6 | ? | tiberiandawn/shared | mods/cameo/rules/tkm.yaml |
| tkmabramspoint | tkm_abrams | ? | ? | mods/cameo/rules/tkm.yaml |
| tkmworker | YRSLAV | ? | ? | mods/cameo/rules/tkm.yaml |
| ts_crate | CRATE | ? | ? | mods/cameo/rules/misc.yaml |
| ts_gdi_engineer | TSENGINEER | tiberiansun/gdi | ? | mods/cameo/ContentPacks/TiberianSun/GDI/yaml/infantry.yaml |
| ts_gdi_lightinfantry | TSE1 | tiberiansun/gdi | ? | mods/cameo/ContentPacks/TiberianSun/GDI/yaml/infantry.yaml |
| ts_gdi_medic | ra1_allies_medic | tiberiansun/gdi | ? | mods/cameo/ContentPacks/TiberianSun/GDI/yaml/infantry.yaml |
| ts_nod_engineer | TSENGINEER | tiberiansun/nod | ? | mods/cameo/ContentPacks/TiberianSun/Nod/yaml/infantry.yaml |
| ts_nod_lightinfantry | TSE1 | tiberiansun/nod | ? | mods/cameo/ContentPacks/TiberianSun/Nod/yaml/infantry.yaml |
| ts_nod_mobileconstructionvehicle | ts_gdi_mobileconstructionvehicle | tiberiansun/nod | tiberiansun/gdi | mods/cameo/ContentPacks/TiberianSun/Nod/yaml/vehicles.yaml |
| ts_nod_rocketinfantry | TSE3 | tiberiansun/nod | ? | mods/cameo/ContentPacks/TiberianSun/Nod/yaml/infantry.yaml |
| ts_nod_servicedepot | ts_gdi_servicedepot | tiberiansun/nod | tiberiansun/gdi | mods/cameo/ContentPacks/TiberianSun/Nod/yaml/buildings.yaml |
| ts_nod_silo | ts_gdi_silo | tiberiansun/nod | ? | mods/cameo/ContentPacks/TiberianSun/Nod/yaml/defenses.yaml |
| ts_nod_tiberiumrefinery | ts_gdi_tiberiumrefinery | tiberiansun/nod | tiberiansun/gdi | mods/cameo/ContentPacks/TiberianSun/Nod/yaml/buildings.yaml |
| tsfsmoker.bomber | tsfsmoker | ? | ? | mods/cameo/rules/tiberiansun.yaml |
| tsmonstermaker1 | VICE | ? | ? | mods/cameo/rules/tiberiansun.yaml |
| wc2_camera_scanner | camera.scan | ? | ? | mods/cameo/rules/warcraft2.yaml |
| wc2_orc_skeleton | orcs_grunt | ? | ? | mods/cameo/rules/warcraft2.yaml |
| yakarmored.Husk | YAK.Husk | ? | ? | mods/cameo/rules/husks.yaml |
| yaktesla.Husk | YAK.Husk | ? | ? | mods/cameo/rules/husks.yaml |
| yrlunr.husk | ra2rock.husk | ? | ? | mods/cameo/rules/redalert2.yaml |
| yrsmin.empy | yuri_slaveminer | ? | ? | mods/cameo/rules/redalert2.yaml |
| yuri_concretebarrier | BRIK | ? | tiberiandawn/shared | mods/cameo/rules/redalert2.yaml |
| yuri_constructionyard | ra2_allies_alliedconstructionyard | ? | ? | mods/cameo/rules/redalert2.yaml |
| yuri_engineer | ra2_allies_engineer | ? | ? | mods/cameo/rules/redalert2.yaml |
| yuriinvisibleplane | U2 | ? | ? | mods/cameo/rules/redalert2.yaml |
| zerg_creepcolony_2 | zerg_creepcolony | ? | ? | mods/cameo/rules/starcraft.yaml |
| zerg_sporecolony | zerg_creepcolony | ? | ? | mods/cameo/rules/starcraft.yaml |
| zerg_sunkencolony_2 | zerg_creepcolony | ? | ? | mods/cameo/rules/starcraft.yaml |


## V4 — inherit chains deeper than 3

| actor | depth | file |
|---|---|---|
| A10 | 4 | mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/aircraft.yaml |
| A10.Husk | 4 | mods/cameo/rules/husks.yaml |
| A10Carrier | 4 | mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/aircraft.yaml |
| A10Carrier.Husk | 5 | mods/cameo/rules/husks.yaml |
| AMMOBOX1 | 4 | mods/cameo/rules/civilian.yaml |
| AMMOBOX2 | 4 | mods/cameo/rules/civilian.yaml |
| AMMOBOX3 | 4 | mods/cameo/rules/civilian.yaml |
| APC.Husk | 4 | mods/cameo/rules/husks.yaml |
| APWR | 4 | mods/cameo/rules/redalert.yaml |
| BADR.Allies | 4 | mods/cameo/rules/redalert.yaml |
| BADR.Bomber | 5 | mods/cameo/rules/redalert.yaml |
| BADR.Japan | 4 | mods/cameo/rules/redalert.yaml |
| BADR.Soviet | 4 | mods/cameo/rules/redalert.yaml |
| BGGY.Husk | 4 | mods/cameo/rules/husks.yaml |
| BIKE.Husk | 4 | mods/cameo/rules/husks.yaml |
| C1 | 5 | mods/cameo/rules/civilian.yaml |
| C10 | 5 | mods/cameo/rules/civilian.yaml |
| C17.Bomber | 6 | mods/cameo/rules/redalert.yaml |
| C17.Paradrop | 4 | mods/cameo/rules/redalert.yaml |
| C2 | 5 | mods/cameo/rules/civilian.yaml |
| C3 | 5 | mods/cameo/rules/civilian.yaml |
| C4 | 5 | mods/cameo/rules/civilian.yaml |
| C5 | 5 | mods/cameo/rules/civilian.yaml |
| C6 | 5 | mods/cameo/rules/civilian.yaml |
| C7 | 5 | mods/cameo/rules/civilian.yaml |
| C8 | 5 | mods/cameo/rules/civilian.yaml |
| C9 | 5 | mods/cameo/rules/civilian.yaml |
| CA | 4 | mods/cameo/rules/redalert.yaml |
| CHAN | 5 | mods/cameo/rules/civilian.yaml |
| CNCCA | 4 | mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/naval.yaml |
| CNCPT | 4 | mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/naval.yaml |
| CNCRSS | 4 | mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/naval.yaml |
| CNCSPEN | 4 | mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/buildings.yaml |
| CNCSS | 4 | mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/naval.yaml |
| CNCSYRD | 4 | mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/buildings.yaml |
| DD | 4 | mods/cameo/rules/redalert.yaml |
| DELPHI | 5 | mods/cameo/rules/redalert.yaml |
| E1 | 7 | mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/infantry.yaml |
| E3 | 7 | mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/infantry.yaml |
| E6 | 5 | mods/cameo/ContentPacks/TiberianDawn/Shared/yaml/infantry.yaml |
| EDEN_AGRIDOME | 4 | mods/cameo/rules/outpost2.yaml |
| EDEN_CARGOTRUCK_EMPTY | 4 | mods/cameo/rules/outpost2.yaml |
| EDEN_CONVEC_STRUCTURE_FACTORY | 4 | mods/cameo/rules/outpost2.yaml |
| EDEN_DIRT | 4 | mods/cameo/rules/outpost2.yaml |
| EDEN_FACTORY_STRUCTURE | 4 | mods/cameo/rules/outpost2.yaml |
| EDEN_FACTORY_VEHICLE | 4 | mods/cameo/rules/outpost2.yaml |
| EDEN_GARAGE | 4 | mods/cameo/rules/outpost2.yaml |
| EDEN_GORF | 4 | mods/cameo/rules/outpost2.yaml |
| EDEN_GP_EMP | 4 | mods/cameo/rules/outpost2.yaml |
| EDEN_GP_LASER | 4 | mods/cameo/rules/outpost2.yaml |
| EDEN_GP_RAILGUN | 4 | mods/cameo/rules/outpost2.yaml |
| EDEN_LAB_ADVANCED | 4 | mods/cameo/rules/outpost2.yaml |
| EDEN_LAB_BASIC | 4 | mods/cameo/rules/outpost2.yaml |
| EDEN_LAB_STANDARD | 4 | mods/cameo/rules/outpost2.yaml |
| EDEN_LIGHT_TOWER | 4 | mods/cameo/rules/outpost2.yaml |
| EDEN_LYNX_ACIDCLOUD | 5 | mods/cameo/rules/outpost2.yaml |
| EDEN_LYNX_EMP | 4 | mods/cameo/rules/outpost2.yaml |
| EDEN_LYNX_LASER | 5 | mods/cameo/rules/outpost2.yaml |
| EDEN_LYNX_RAILGUN | 5 | mods/cameo/rules/outpost2.yaml |
| EDEN_LYNX_STARFLARE | 5 | mods/cameo/rules/outpost2.yaml |
| EDEN_LYNX_THORSHAMMER | 5 | mods/cameo/rules/outpost2.yaml |
| EDEN_MINE_COMMON | 4 | mods/cameo/rules/outpost2.yaml |
| EDEN_NURSERY | 4 | mods/cameo/rules/outpost2.yaml |
| EDEN_RCC | 4 | mods/cameo/rules/outpost2.yaml |
| EDEN_RESIDENCE | 4 | mods/cameo/rules/outpost2.yaml |
| EDEN_SCOUT | 4 | mods/cameo/rules/outpost2.yaml |
| EDEN_SMELTER_COMMON | 4 | mods/cameo/rules/outpost2.yaml |
| EDEN_SMELTER_RARE | 4 | mods/cameo/rules/outpost2.yaml |
| EDEN_SOLAR_ARRAY | 4 | mods/cameo/rules/outpost2.yaml |
| EDEN_SPACEPORT | 6 | mods/cameo/rules/outpost2.yaml |
| EDEN_STORAGE_COMMON | 4 | mods/cameo/rules/outpost2.yaml |
| EDEN_TIGER_ACIDCLOUD | 6 | mods/cameo/rules/outpost2.yaml |
| EDEN_TIGER_EMP | 4 | mods/cameo/rules/outpost2.yaml |
| EDEN_TIGER_LASER | 5 | mods/cameo/rules/outpost2.yaml |
| EDEN_TIGER_RAILGUN | 5 | mods/cameo/rules/outpost2.yaml |
| EDEN_TIGER_STARFLARE | 5 | mods/cameo/rules/outpost2.yaml |
| EDEN_TIGER_THORSHAMMER | 5 | mods/cameo/rules/outpost2.yaml |
| EDEN_TOKAMAK | 5 | mods/cameo/rules/outpost2.yaml |
| EDEN_UNIVERSITY | 4 | mods/cameo/rules/outpost2.yaml |
| EINSTEIN | 5 | mods/cameo/rules/redalert.yaml |
| FCOM | 4 | mods/cameo/rules/tech.yaml |
| FCOM.Husk | 4 | mods/cameo/rules/tech.yaml |
| GNRL | 5 | mods/cameo/rules/redalert.yaml |
| HIND.Husk | 4 | mods/cameo/rules/husks.yaml |
| HOSP.Husk | 4 | mods/cameo/rules/tech.yaml |
| INVISIBLEPLANE | 4 | mods/cameo/rules/tiberiansun.yaml |
| JEEP.Husk | 4 | mods/cameo/rules/husks.yaml |
| JHIND.Husk | 4 | mods/cameo/rules/husks.yaml |
| KENN | 4 | mods/cameo/rules/redalert.yaml |
| LST | 4 | mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/naval.yaml |
| MISS | 4 | mods/cameo/rules/tech.yaml |
| MISS.Husk | 4 | mods/cameo/rules/tech.yaml |
| MOEBIUS | 5 | mods/cameo/rules/civilian.yaml |
| MSUB | 4 | mods/cameo/rules/redalert.yaml |
| NUK2 | 4 | mods/cameo/ContentPacks/TiberianDawn/Shared/yaml/buildings.yaml |
| NUKE | 4 | mods/cameo/ContentPacks/TiberianDawn/Shared/yaml/buildings.yaml |
| OILB.Building | 4 | mods/cameo/rules/shared.yaml |
| OILB.Husk | 4 | mods/cameo/rules/tech.yaml |
| OILB.RA2 | 4 | mods/cameo/rules/redalert2.yaml |
| OILB.TS | 5 | mods/cameo/rules/tiberiansun.yaml |
| OILB.d2k | 5 | mods/cameo/ContentPacks/D2k/Shared/yaml/buildings.yaml |
| PLYMOUTH_AGRIDOME | 4 | mods/cameo/rules/outpost2.yaml |
| PLYMOUTH_BASIC_LAB | 4 | mods/cameo/rules/outpost2.yaml |
| PLYMOUTH_CARGOTRUCK_EMPTY | 4 | mods/cameo/rules/outpost2.yaml |
| PLYMOUTH_DIRT | 4 | mods/cameo/rules/outpost2.yaml |
| PLYMOUTH_FACTORY_VEHICLE | 4 | mods/cameo/rules/outpost2.yaml |
| PLYMOUTH_GARAGE | 4 | mods/cameo/rules/outpost2.yaml |
| PLYMOUTH_GORF | 4 | mods/cameo/rules/outpost2.yaml |
| PLYMOUTH_GP_MICROWAVE | 4 | mods/cameo/rules/outpost2.yaml |
| PLYMOUTH_GP_RPG | 4 | mods/cameo/rules/outpost2.yaml |
| PLYMOUTH_GP_STICKYFOAM | 4 | mods/cameo/rules/outpost2.yaml |
| PLYMOUTH_LAB_ADVANCED | 4 | mods/cameo/rules/outpost2.yaml |
| PLYMOUTH_LAB_STANDARD | 4 | mods/cameo/rules/outpost2.yaml |
| PLYMOUTH_LIGHT_TOWER | 4 | mods/cameo/rules/outpost2.yaml |
| PLYMOUTH_LYNX_EMP | 5 | mods/cameo/rules/outpost2.yaml |
| PLYMOUTH_LYNX_ESG | 5 | mods/cameo/rules/outpost2.yaml |
| PLYMOUTH_LYNX_MICROWAVE | 5 | mods/cameo/rules/outpost2.yaml |
| PLYMOUTH_LYNX_RPG | 5 | mods/cameo/rules/outpost2.yaml |
| PLYMOUTH_LYNX_STARFLARE | 5 | mods/cameo/rules/outpost2.yaml |
| PLYMOUTH_LYNX_STICKYFOAM | 5 | mods/cameo/rules/outpost2.yaml |
| PLYMOUTH_LYNX_SUPERNOVA | 5 | mods/cameo/rules/outpost2.yaml |
| PLYMOUTH_MINE_COMMON | 4 | mods/cameo/rules/outpost2.yaml |
| PLYMOUTH_NURSERY | 4 | mods/cameo/rules/outpost2.yaml |
| PLYMOUTH_RCC | 4 | mods/cameo/rules/outpost2.yaml |
| PLYMOUTH_RESIDENCE | 4 | mods/cameo/rules/outpost2.yaml |
| PLYMOUTH_SCORPION | 4 | mods/cameo/rules/outpost2.yaml |
| PLYMOUTH_SCOUT | 4 | mods/cameo/rules/outpost2.yaml |
| PLYMOUTH_SMELTER_COMMON | 4 | mods/cameo/rules/outpost2.yaml |
| PLYMOUTH_SMELTER_RARE | 4 | mods/cameo/rules/outpost2.yaml |
| PLYMOUTH_SOLARARRAY | 4 | mods/cameo/rules/outpost2.yaml |
| PLYMOUTH_SPACEPORT | 6 | mods/cameo/rules/outpost2.yaml |
| PLYMOUTH_SPIDER | 4 | mods/cameo/rules/outpost2.yaml |
| PLYMOUTH_STORAGE_COMMON | 4 | mods/cameo/rules/outpost2.yaml |
| PLYMOUTH_STRUCTURE_FACTORY | 4 | mods/cameo/rules/outpost2.yaml |
| PLYMOUTH_TIGER_EMP | 6 | mods/cameo/rules/outpost2.yaml |
| PLYMOUTH_TIGER_ESG | 6 | mods/cameo/rules/outpost2.yaml |
| PLYMOUTH_TIGER_MICROWAVE | 6 | mods/cameo/rules/outpost2.yaml |
| PLYMOUTH_TIGER_RPG | 6 | mods/cameo/rules/outpost2.yaml |
| PLYMOUTH_TIGER_STARFLARE | 6 | mods/cameo/rules/outpost2.yaml |
| PLYMOUTH_TIGER_STICKYFOAM | 6 | mods/cameo/rules/outpost2.yaml |
| PLYMOUTH_TIGER_SUPERNOVA | 6 | mods/cameo/rules/outpost2.yaml |
| PLYMOUTH_TOKAMAK | 5 | mods/cameo/rules/outpost2.yaml |
| PLYMOUTH_UNIVERSITY | 4 | mods/cameo/rules/outpost2.yaml |
| POWR | 4 | mods/cameo/rules/redalert.yaml |
| PT | 4 | mods/cameo/rules/redalert.yaml |
| RAAPC | 4 | mods/cameo/rules/redalert.yaml |
| RABIO | 4 | mods/cameo/rules/tech.yaml |
| RACHAN | 5 | mods/cameo/rules/redalert.yaml |
| RAE1 | 6 | mods/cameo/rules/redalert.yaml |
| RAE3 | 6 | mods/cameo/rules/redalert.yaml |
| RAE6 | 6 | mods/cameo/rules/redalert.yaml |
| RALST | 4 | mods/cameo/rules/redalert.yaml |
| RAMISS | 5 | mods/cameo/rules/tech.yaml |
| RAPT | 4 | mods/cameo/rules/tiberiansun.yaml |
| RATRAN.Husk | 4 | mods/cameo/rules/redalert.yaml |
| ROCKETANGEL.husk | 4 | mods/cameo/rules/redalert.yaml |
| SCBARRACKSM | 5 | mods/cameo/rules/starcraft.yaml |
| SCBROODLING | 5 | mods/cameo/rules/starcraft.yaml |
| SCCOMMANDCENTERM | 4 | mods/cameo/rules/starcraft.yaml |
| SCENGINEERINGBAYM | 5 | mods/cameo/rules/starcraft.yaml |
| SCFACTORYM | 5 | mods/cameo/rules/starcraft.yaml |
| SCINTERCEPTOR | 4 | mods/cameo/rules/starcraft.yaml |
| SCSCIENCEFACILITYM | 5 | mods/cameo/rules/starcraft.yaml |
| SCSCOURGEDRONE | 6 | mods/cameo/rules/starcraft.yaml |
| SCSENTINELM | 5 | mods/cameo/rules/starcraft.yaml |
| SCSPIDERMINE | 4 | mods/cameo/rules/starcraft.yaml |
| SCSTARPORTM | 5 | mods/cameo/rules/starcraft.yaml |
| SCWRAITHDRONE | 4 | mods/cameo/rules/starcraft.yaml |
| SILO | 4 | mods/cameo/ContentPacks/TiberianDawn/Shared/yaml/buildings.yaml |
| SS | 4 | mods/cameo/rules/redalert.yaml |
| TECH1 | 5 | mods/cameo/rules/redalert.yaml |
| TECHBCANNON | 4 | mods/cameo/rules/tech.yaml |
| TECHBCANNON2 | 5 | mods/cameo/rules/tech.yaml |
| TECN | 5 | mods/cameo/rules/civilian.yaml |
| TRAN | 5 | mods/cameo/ContentPacks/TiberianDawn/Shared/yaml/aircraft.yaml |
| TRAN.Husk | 4 | mods/cameo/ContentPacks/TiberianDawn/Shared/yaml/aircraft.yaml |
| TSE1 | 5 | mods/cameo/rules/tiberiansun.yaml |
| TSE1PARA | 6 | mods/cameo/rules/tiberiansun.yaml |
| TSE2PARA | 6 | mods/cameo/rules/tiberiansun.yaml |
| TSE3 | 5 | mods/cameo/rules/tiberiansun.yaml |
| TSENGINEER | 6 | mods/cameo/rules/tiberiansun.yaml |
| U3 | 4 | mods/cameo/rules/redalert.yaml |
| V19.Husk | 4 | mods/cameo/rules/tech.yaml |
| VICE | 4 | mods/cameo/rules/civilian.yaml |
| YRDISK.Husk | 4 | mods/cameo/rules/redalert2.yaml |
| YRSLAV | 5 | mods/cameo/rules/redalert2.yaml |
| alien.nax | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/infantry.yaml |
| alliedcybertank | 5 | mods/cameo/rules/redalert.yaml |
| apparition.ixian | 5 | mods/cameo/ContentPacks/D2k/Ixian/yaml/vehicles.yaml |
| asian_alliance_advancedcommunicationcenter | 5 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/buildings.yaml |
| asian_alliance_alligator | 5 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/infantry.yaml |
| asian_alliance_asdf | 5 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/infantry.yaml |
| asian_alliance_asianairforcecommand | 4 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/buildings.yaml |
| asian_alliance_asianbarracks | 4 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/buildings.yaml |
| asian_alliance_asianbattlelab | 4 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/buildings.yaml |
| asian_alliance_asiancommando | 5 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/infantry.yaml |
| asian_alliance_asianconstructionyard | 4 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/buildings.yaml |
| asian_alliance_asianflametank | 5 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/vehicles.yaml |
| asian_alliance_asianflametrooper | 5 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/infantry.yaml |
| asian_alliance_asianmilitia | 5 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/infantry.yaml |
| asian_alliance_asianmobileconstructionvehicle | 4 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/vehicles.yaml |
| asian_alliance_asianorerefinery | 4 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/buildings.yaml |
| asian_alliance_asianpetrolplant | 4 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/buildings.yaml |
| asian_alliance_asianradar | 4 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/buildings.yaml |
| asian_alliance_asiansentryflamer | 5 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/buildings.yaml |
| asian_alliance_asianservicedepot | 4 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/buildings.yaml |
| asian_alliance_asiantankkiller | 5 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/infantry.yaml |
| asian_alliance_asianwarfactory | 5 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/buildings.yaml |
| asian_alliance_chaosstorminductor | 4 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/buildings.yaml |
| asian_alliance_chaostower | 5 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/buildings.yaml |
| asian_alliance_dragonfly | 4 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/vehicles.yaml |
| asian_alliance_droneminer | 4 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/vehicles.yaml |
| asian_alliance_engineer | 7 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/infantry.yaml |
| asian_alliance_fanatic | 5 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/infantry.yaml |
| asian_alliance_heavyrailguntank | 6 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/vehicles.yaml |
| asian_alliance_howitzer | 5 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/vehicles.yaml |
| asian_alliance_hyperionprojector | 5 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/buildings.yaml |
| asian_alliance_japanesesamurai | 5 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/infantry.yaml |
| asian_alliance_lynxtank | 5 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/vehicles.yaml |
| asian_alliance_militaryacademy | 5 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/buildings.yaml |
| asian_alliance_oiltruck | 4 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/vehicles.yaml |
| asian_alliance_pelican | 4 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/aircraft.yaml |
| asian_alliance_phoenix | 4 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/aircraft.yaml |
| asian_alliance_plasmacannon | 5 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/buildings.yaml |
| asian_alliance_plasmatrooper | 5 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/infantry.yaml |
| asian_alliance_pulsar | 5 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/buildings.yaml |
| asian_alliance_pulverizer | 5 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/vehicles.yaml |
| asian_alliance_pulverizermecha | 5 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/vehicles.yaml |
| asian_alliance_quasar | 4 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/vehicles.yaml |
| asian_alliance_railguntank | 5 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/vehicles.yaml |
| asian_alliance_railtower | 5 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/buildings.yaml |
| asian_alliance_shinobi | 5 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/infantry.yaml |
| asian_alliance_tankreactor | 4 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/buildings.yaml |
| asian_alliance_type89mlrs | 5 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/vehicles.yaml |
| asian_alliance_veteranarcher | 5 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/infantry.yaml |
| asian_alliance_viper | 5 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/vehicles.yaml |
| asian_alliance_warturtle | 5 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/vehicles.yaml |
| assault.nax | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/vehicles.yaml |
| bbomb2_husk.nax2 | 4 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/aircraft.yaml |
| bbomb3_husk.nax2 | 4 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/aircraft.yaml |
| bio.Husk | 4 | mods/cameo/rules/tech.yaml |
| bomber_husk.asian | 4 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/aircraft.yaml |
| bomber_minebomb.asian | 4 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/aircraft.yaml |
| bomber_minebomb2.asian | 5 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/aircraft.yaml |
| cabal_artilleryspider | 5 | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/vehicles.yaml |
| cabal_artilleryspider_backup | 6 | mods/cameo/rules/tiberiansun.yaml |
| cabal_ascended | 5 | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/infantry.yaml |
| cabal_avatar | 5 | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/vehicles.yaml |
| cabal_avatar_backup | 6 | mods/cameo/rules/tiberiansun.yaml |
| cabal_beholder | 5 | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/vehicles.yaml |
| cabal_berserker | 5 | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/vehicles.yaml |
| cabal_constructionyard | 4 | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/vehicles.yaml |
| cabal_core | 5 | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/buildings.yaml |
| cabal_coredefender | 4 | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/vehicles.yaml |
| cabal_cyborg_assassin | 4 | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/aircraft.yaml |
| cabal_cyborgcommando | 5 | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/infantry.yaml |
| cabal_cyborgcommandov2 | 5 | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/infantry.yaml |
| cabal_cyborginfantry | 5 | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/infantry.yaml |
| cabal_cyborgreaper | 4 | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/vehicles.yaml |
| cabal_devout | 5 | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/infantry.yaml |
| cabal_dissolver | 5 | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/infantry.yaml |
| cabal_eliminator800 | 5 | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/infantry.yaml |
| cabal_engineer | 5 | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/infantry.yaml |
| cabal_hackercyborg | 5 | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/infantry.yaml |
| cabal_heavycabalobelisk | 4 | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/defenses.yaml |
| cabal_heavyreaper | 4 | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/vehicles.yaml |
| cabal_hunter_drone | 4 | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/aircraft.yaml |
| cabal_hunter_drone_carrier | 4 | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/aircraft.yaml |
| cabal_hunterkillermk1 | 4 | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/aircraft.yaml |
| cabal_hunterkillermk1_elite | 4 | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/aircraft.yaml |
| cabal_laserspider | 5 | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/vehicles.yaml |
| cabal_legion | 5 | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/vehicles.yaml |
| cabal_legion_backup | 6 | mods/cameo/rules/tiberiansun.yaml |
| cabal_manticore | 4 | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/vehicles.yaml |
| cabal_manticore_backup | 5 | mods/cameo/rules/tiberiansun.yaml |
| cabal_mantis | 5 | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/vehicles.yaml |
| cabal_mobileconstructionvehicle | 4 | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/vehicles.yaml |
| cabal_mobilestealthgenerator | 4 | mods/cameo/rules/tiberiansun.yaml |
| cabal_mothership | 4 | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/aircraft.yaml |
| cabal_obeliskofdarkness | 4 | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/defenses.yaml |
| cabal_orb_drone | 4 | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/aircraft.yaml |
| cabal_overkill_gunship | 4 | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/aircraft.yaml |
| cabal_pillbox | 5 | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/defenses.yaml |
| cabal_plasmaturret | 4 | mods/cameo/rules/tiberiansun.yaml |
| cabal_powerplant | 4 | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/buildings.yaml |
| cabal_radar | 4 | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/buildings.yaml |
| cabal_ravager | 5 | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/vehicles.yaml |
| cabal_repair_drone | 4 | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/aircraft.yaml |
| cabal_rocketcyborg | 5 | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/infantry.yaml |
| cabal_scarabapc | 4 | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/vehicles.yaml |
| cabal_spidercnc4 | 5 | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/vehicles.yaml |
| cabal_tarantula | 5 | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/vehicles.yaml |
| cabal_tarantula_backup | 6 | mods/cameo/rules/tiberiansun.yaml |
| cabal_tiberiumharvester | 5 | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/vehicles.yaml |
| cabal_widow | 5 | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/vehicles.yaml |
| car.nax | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/vehicles.yaml |
| carryall.paradrop | 4 | mods/cameo/ContentPacks/D2k/Shared/yaml/aircraft.yaml |
| carryall_reinforce.ordos | 4 | mods/cameo/ContentPacks/D2k/Ordos/yaml/aircraft.yaml |
| cgpnch.asian | 5 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/buildings.yaml |
| cgyard.asian | 4 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/buildings.yaml |
| cgyard.latin | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/buildings.yaml |
| cobra.steel | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/vehicles.yaml |
| combat_tank.atreides | 6 | mods/cameo/ContentPacks/D2k/Atreides/yaml/vehicles.yaml |
| combat_tank.harkonnen | 5 | mods/cameo/ContentPacks/D2k/Harkonnen/yaml/vehicles.yaml |
| combat_tank.ixian | 5 | mods/cameo/ContentPacks/D2k/Ixian/yaml/vehicles.yaml |
| combat_tank_husk.atreides | 5 | mods/cameo/ContentPacks/D2k/Atreides/yaml/vehicles.yaml |
| combat_tank_husk.harkonnen | 5 | mods/cameo/ContentPacks/D2k/Harkonnen/yaml/vehicles.yaml |
| combat_tank_husk.ordos | 5 | mods/cameo/ContentPacks/D2k/Ordos/yaml/vehicles.yaml |
| conehead2.nax | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/infantry.yaml |
| cougar.steel | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/vehicles.yaml |
| cruiser_f.steel | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/aircraft.yaml |
| d2k_mcv.husk | 4 | mods/cameo/ContentPacks/D2k/Shared/yaml/vehicles.yaml |
| d2k_silo.atreides | 5 | mods/cameo/ContentPacks/D2k/Atreides/yaml/buildings.yaml |
| d2k_tyrant.husk | 4 | mods/cameo/ContentPacks/D2k/Shared/yaml/vehicles.yaml |
| devastator_husk.harkonnen | 4 | mods/cameo/ContentPacks/D2k/Harkonnen/yaml/vehicles.yaml |
| deviator_husk.ordos | 4 | mods/cameo/ContentPacks/D2k/Ordos/yaml/vehicles.yaml |
| dieglocke_husk.nax2 | 4 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/aircraft.yaml |
| drone.nax | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/aircraft.yaml |
| drone_husk.ixian | 4 | mods/cameo/ContentPacks/D2k/Ixian/yaml/aircraft.yaml |
| duelist_tank.ixian | 5 | mods/cameo/ContentPacks/D2k/Ixian/yaml/vehicles.yaml |
| duelist_tank_husk.ixian | 4 | mods/cameo/ContentPacks/D2k/Ixian/yaml/vehicles.yaml |
| engineer | 6 | mods/cameo/ContentPacks/D2k/Shared/yaml/infantry.yaml |
| farasha_drone.ixian | 4 | mods/cameo/ContentPacks/D2k/Ixian/yaml/aircraft.yaml |
| forgotten_apache | 4 | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/aircraft.yaml |
| forgotten_apctruck | 4 | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/vehicles.yaml |
| forgotten_bowler | 4 | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/vehicles.yaml |
| forgotten_brokenrattytankturret | 4 | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/defenses.yaml |
| forgotten_brokenscoopertankturret | 4 | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/defenses.yaml |
| forgotten_brokenwarriortankturret | 4 | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/defenses.yaml |
| forgotten_carryall | 4 | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/aircraft.yaml |
| forgotten_chemicalmammothtank | 4 | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/vehicles.yaml |
| forgotten_chemsprayinfantry | 5 | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/infantry.yaml |
| forgotten_chinook | 4 | mods/cameo/rules/tiberiansun.yaml |
| forgotten_closhtank | 5 | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/vehicles.yaml |
| forgotten_cobracopter | 4 | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/aircraft.yaml |
| forgotten_cropplane | 4 | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/aircraft.yaml |
| forgotten_crystalpowerextractor | 4 | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/buildings.yaml |
| forgotten_engineer | 7 | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/infantry.yaml |
| forgotten_experimentalmammothtank | 4 | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/vehicles.yaml |
| forgotten_flametank | 5 | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/vehicles.yaml |
| forgotten_ghoststalker | 5 | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/infantry.yaml |
| forgotten_ghoststalker_r4 | 6 | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/infantry.yaml |
| forgotten_ghoststalker_sp | 6 | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/infantry.yaml |
| forgotten_helipad | 4 | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/buildings.yaml |
| forgotten_juggerflakwall | 4 | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/defenses.yaml |
| forgotten_locustbomber | 4 | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/aircraft.yaml |
| forgotten_m113adats | 4 | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/vehicles.yaml |
| forgotten_machineguntower | 4 | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/defenses.yaml |
| forgotten_missilevan | 5 | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/vehicles.yaml |
| forgotten_mlrs | 5 | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/vehicles.yaml |
| forgotten_mobileconstructionvehicle | 5 | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/vehicles.yaml |
| forgotten_mutant | 5 | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/infantry.yaml |
| forgotten_mutant_sp | 6 | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/infantry.yaml |
| forgotten_mutant_wild | 6 | mods/cameo/rules/tiberiansun.yaml |
| forgotten_mutanthijacker | 5 | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/infantry.yaml |
| forgotten_mutantmortarman | 5 | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/infantry.yaml |
| forgotten_mutantsergeant | 5 | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/infantry.yaml |
| forgotten_mutantsniper | 6 | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/infantry.yaml |
| forgotten_mutantsniper_r4 | 7 | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/infantry.yaml |
| forgotten_mutantsniper_sp | 7 | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/infantry.yaml |
| forgotten_mutantsoldier | 5 | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/infantry.yaml |
| forgotten_mutantsoldier_sp | 6 | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/infantry.yaml |
| forgotten_nomadbarracks | 4 | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/vehicles.yaml |
| forgotten_radar | 4 | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/buildings.yaml |
| forgotten_raidercar | 4 | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/vehicles.yaml |
| forgotten_rattytank | 5 | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/vehicles.yaml |
| forgotten_rocketinfantry | 6 | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/infantry.yaml |
| forgotten_ruiner | 4 | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/vehicles.yaml |
| forgotten_runnershotgal | 5 | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/infantry.yaml |
| forgotten_scarabapc | 5 | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/vehicles.yaml |
| forgotten_scoopertank | 5 | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/vehicles.yaml |
| forgotten_servicedepot | 4 | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/buildings.yaml |
| forgotten_tankkiller | 5 | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/vehicles.yaml |
| forgotten_thumperbus | 5 | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/vehicles.yaml |
| forgotten_tiberianfiend | 5 | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/infantry.yaml |
| forgotten_tiberianfiend_wild | 6 | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/infantry.yaml |
| forgotten_tiberiumharvester | 5 | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/vehicles.yaml |
| forgotten_tiberiumspike | 6 | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/buildings.yaml |
| forgotten_veinhole | 5 | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/defenses.yaml |
| forgotten_viniferafiend | 5 | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/infantry.yaml |
| forgotten_visceroid | 4 | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/infantry.yaml |
| forgotten_warriortank | 5 | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/vehicles.yaml |
| forgotten_wasp | 4 | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/aircraft.yaml |
| forgotten_zombiemutant | 5 | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/infantry.yaml |
| frank.nax | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/infantry.yaml |
| fremen_creep | 5 | mods/cameo/ContentPacks/D2k/Shared/yaml/infantry.yaml |
| frigate.paradrop | 4 | mods/cameo/ContentPacks/D2k/Shared/yaml/aircraft.yaml |
| futuretech_athenacannon | 5 | mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/vehicles.yaml |
| futuretech_battlelab | 4 | mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/buildings.yaml |
| futuretech_beehivedronecarrier | 5 | mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/vehicles.yaml |
| futuretech_blackwidow | 5 | mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/infantry.yaml |
| futuretech_cannondroid | 5 | mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/infantry.yaml |
| futuretech_constructionyard | 4 | mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/buildings.yaml |
| futuretech_cryocopter | 4 | mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/aircraft.yaml |
| futuretech_cryolegionnaire | 5 | mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/infantry.yaml |
| futuretech_energizer | 5 | mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/vehicles.yaml |
| futuretech_enforcer | 5 | mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/infantry.yaml |
| futuretech_engineer | 7 | mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/infantry.yaml |
| futuretech_frostbiteturret | 5 | mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/buildings.yaml |
| futuretech_futuretank | 4 | mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/vehicles.yaml |
| futuretech_guardiantank | 5 | mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/vehicles.yaml |
| futuretech_gunstrider | 5 | mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/vehicles.yaml |
| futuretech_harbingergunship | 4 | mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/aircraft.yaml |
| futuretech_hypercore | 5 | mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/buildings.yaml |
| futuretech_javelinsoldier | 5 | mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/infantry.yaml |
| futuretech_launchpad | 4 | mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/buildings.yaml |
| futuretech_missiledroid | 5 | mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/infantry.yaml |
| futuretech_mobileconstructionvehicle | 4 | mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/vehicles.yaml |
| futuretech_multiturretsystem | 5 | mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/buildings.yaml |
| futuretech_oriontank | 5 | mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/vehicles.yaml |
| futuretech_phalanxwip | 4 | mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/vehicles.yaml |
| futuretech_plasmastrider | 5 | mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/vehicles.yaml |
| futuretech_prospector | 4 | mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/vehicles.yaml |
| futuretech_prospectormk2 | 4 | mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/vehicles.yaml |
| futuretech_refinery | 4 | mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/buildings.yaml |
| futuretech_repairdroid | 4 | mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/infantry.yaml |
| futuretech_riptideacv | 4 | mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/vehicles.yaml |
| futuretech_robotcontrolcenter | 4 | mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/buildings.yaml |
| futuretech_robottank | 5 | mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/vehicles.yaml |
| futuretech_salamanderifv | 5 | mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/vehicles.yaml |
| futuretech_scoutdroid | 4 | mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/infantry.yaml |
| futuretech_shotgundroid | 5 | mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/infantry.yaml |
| futuretech_spyfutu | 5 | mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/infantry.yaml |
| futuretech_thermalpowerplant | 4 | mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/buildings.yaml |
| futuretech_transmissioncenter | 4 | mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/buildings.yaml |
| futuretech_troopgate | 4 | mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/buildings.yaml |
| futuretech_twister | 4 | mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/aircraft.yaml |
| futuretech_warpgate | 5 | mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/buildings.yaml |
| gdicarrier | 4 | mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/naval.yaml |
| gdirigdrone | 4 | mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/aircraft.yaml |
| gunb.asian | 4 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/naval.yaml |
| harvester_husk.ixian | 4 | mods/cameo/ContentPacks/D2k/Ixian/yaml/vehicles.yaml |
| haunebu2_husk.nax2 | 4 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/aircraft.yaml |
| haunebu_husk.nax2 | 4 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/aircraft.yaml |
| heavy_factory | 4 | mods/cameo/ContentPacks/D2k/Shared/yaml/buildings.yaml |
| heavy_inf.ixian | 5 | mods/cameo/ContentPacks/D2k/Ixian/yaml/infantry.yaml |
| heavy_rocket_raider.ixian | 4 | mods/cameo/ContentPacks/D2k/Ixian/yaml/vehicles.yaml |
| heavydrone_husk.ixian | 4 | mods/cameo/ContentPacks/D2k/Ixian/yaml/aircraft.yaml |
| hole_small.nax2 | 4 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/infantry.yaml |
| horten_bomber.nax | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/aircraft.yaml |
| humans_archmage | 6 | mods/cameo/rules/warcraft2.yaml |
| humans_ballista | 5 | mods/cameo/rules/warcraft2.yaml |
| humans_barracks | 4 | mods/cameo/rules/warcraft2.yaml |
| humans_blacksmith | 4 | mods/cameo/rules/warcraft2.yaml |
| humans_cannontower | 5 | mods/cameo/rules/warcraft2.yaml |
| humans_church | 4 | mods/cameo/rules/warcraft2.yaml |
| humans_demolitionsquad | 6 | mods/cameo/rules/warcraft2.yaml |
| humans_dwarvenrifleman | 6 | mods/cameo/rules/warcraft2.yaml |
| humans_elvenarcher | 6 | mods/cameo/rules/warcraft2.yaml |
| humans_elvenlumbermill | 4 | mods/cameo/rules/warcraft2.yaml |
| humans_elvenranger | 7 | mods/cameo/rules/warcraft2.yaml |
| humans_farm | 5 | mods/cameo/rules/warcraft2.yaml |
| humans_flyingmachine | 5 | mods/cameo/rules/warcraft2.yaml |
| humans_footman | 6 | mods/cameo/rules/warcraft2.yaml |
| humans_gnomishinventor | 4 | mods/cameo/rules/warcraft2.yaml |
| humans_gryphonaviary | 4 | mods/cameo/rules/warcraft2.yaml |
| humans_gryphonrider | 5 | mods/cameo/rules/warcraft2.yaml |
| humans_guardtower | 5 | mods/cameo/rules/warcraft2.yaml |
| humans_gyrocoptermachine | 5 | mods/cameo/rules/warcraft2.yaml |
| humans_highelfpriest | 6 | mods/cameo/rules/warcraft2.yaml |
| humans_highelfsorceress | 6 | mods/cameo/rules/warcraft2.yaml |
| humans_highelvenarcher | 6 | mods/cameo/rules/warcraft2.yaml |
| humans_humangoldmine | 4 | mods/cameo/rules/warcraft2.yaml |
| humans_humangoldmine_2 | 5 | mods/cameo/rules/warcraft2.yaml |
| humans_humanscouttower | 4 | mods/cameo/rules/warcraft2.yaml |
| humans_knight | 5 | mods/cameo/rules/warcraft2.yaml |
| humans_mage | 6 | mods/cameo/rules/warcraft2.yaml |
| humans_magetower | 6 | mods/cameo/rules/warcraft2.yaml |
| humans_militiapeasant | 6 | mods/cameo/rules/warcraft2.yaml |
| humans_mobileconstructionvehiclehuman | 5 | mods/cameo/rules/warcraft2.yaml |
| humans_mortarteam | 6 | mods/cameo/rules/warcraft2.yaml |
| humans_paladin | 6 | mods/cameo/rules/warcraft2.yaml |
| humans_peasant | 6 | mods/cameo/rules/warcraft2.yaml |
| humans_siegeengine | 5 | mods/cameo/rules/warcraft2.yaml |
| humans_stables | 4 | mods/cameo/rules/warcraft2.yaml |
| humans_sunwell | 4 | mods/cameo/rules/warcraft2.yaml |
| humans_warcraft3footman | 6 | mods/cameo/rules/warcraft2.yaml |
| humans_warcraft3knight | 5 | mods/cameo/rules/warcraft2.yaml |
| hummer.steel | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/vehicles.yaml |
| interceptor.nax | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/aircraft.yaml |
| ixian_advancedheavyfactory | 5 | mods/cameo/ContentPacks/D2k/Ixian/yaml/buildings.yaml |
| ixian_airdrone | 4 | mods/cameo/ContentPacks/D2k/Ixian/yaml/aircraft.yaml |
| ixian_autonomouscarryall | 4 | mods/cameo/ContentPacks/D2k/Shared/yaml/aircraft.yaml |
| ixian_barracks | 4 | mods/cameo/ContentPacks/D2k/Ixian/yaml/buildings.yaml |
| ixian_constructionyard | 5 | mods/cameo/ContentPacks/D2k/Ixian/yaml/buildings.yaml |
| ixian_empbomber | 4 | mods/cameo/ContentPacks/D2k/Ixian/yaml/aircraft.yaml |
| ixian_farasha | 4 | mods/cameo/ContentPacks/D2k/Ixian/yaml/aircraft.yaml |
| ixian_gunturret | 5 | mods/cameo/ContentPacks/D2k/Ixian/yaml/buildings.yaml |
| ixian_hightechfactory | 5 | mods/cameo/ContentPacks/D2k/Ixian/yaml/buildings.yaml |
| ixian_ixcombatsiege | 5 | mods/cameo/ContentPacks/D2k/Ixian/yaml/vehicles.yaml |
| ixian_ixmissiletank | 5 | mods/cameo/ContentPacks/D2k/Ixian/yaml/vehicles.yaml |
| ixian_ixprojector | 5 | mods/cameo/ContentPacks/D2k/Ixian/yaml/vehicles.yaml |
| ixian_ixresearchcenter | 5 | mods/cameo/ContentPacks/D2k/Ixian/yaml/buildings.yaml |
| ixian_ixsiegetank | 5 | mods/cameo/ContentPacks/D2k/Ixian/yaml/vehicles.yaml |
| ixian_lightinfantry | 5 | mods/cameo/ContentPacks/D2k/Ixian/yaml/infantry.yaml |
| ixian_machinegunturret | 5 | mods/cameo/ContentPacks/D2k/Ixian/yaml/buildings.yaml |
| ixian_mobileconstructionvehicle | 5 | mods/cameo/ContentPacks/D2k/Ixian/yaml/vehicles.yaml |
| ixian_mongoose | 5 | mods/cameo/ContentPacks/D2k/Ixian/yaml/vehicles.yaml |
| ixian_munitionssilo | 4 | mods/cameo/ContentPacks/D2k/Ixian/yaml/buildings.yaml |
| ixian_neocymek | 5 | mods/cameo/ContentPacks/D2k/Ixian/yaml/vehicles.yaml |
| ixian_outpost | 4 | mods/cameo/ContentPacks/D2k/Ixian/yaml/buildings.yaml |
| ixian_railgundrone | 4 | mods/cameo/ContentPacks/D2k/Ixian/yaml/aircraft.yaml |
| ixian_refineryixian | 5 | mods/cameo/ContentPacks/D2k/Ixian/yaml/buildings.yaml |
| ixian_repairpad | 4 | mods/cameo/ContentPacks/D2k/Ixian/yaml/buildings.yaml |
| ixian_resonancedrone | 4 | mods/cameo/ContentPacks/D2k/Ixian/yaml/aircraft.yaml |
| ixian_rockettrooper | 5 | mods/cameo/ContentPacks/D2k/Ixian/yaml/infantry.yaml |
| ixian_rocketturret | 5 | mods/cameo/ContentPacks/D2k/Ixian/yaml/buildings.yaml |
| ixian_shockinfantry | 5 | mods/cameo/ContentPacks/D2k/Ixian/yaml/infantry.yaml |
| ixian_shockraider | 5 | mods/cameo/ContentPacks/D2k/Ixian/yaml/vehicles.yaml |
| ixian_spiceharvester | 5 | mods/cameo/ContentPacks/D2k/Ixian/yaml/vehicles.yaml |
| ixian_starport | 6 | mods/cameo/ContentPacks/D2k/Ixian/yaml/buildings.yaml |
| ixian_storagesilo | 5 | mods/cameo/ContentPacks/D2k/Ixian/yaml/buildings.yaml |
| ixian_storminfantry | 5 | mods/cameo/ContentPacks/D2k/Ixian/yaml/infantry.yaml |
| ixian_stormlasher | 5 | mods/cameo/ContentPacks/D2k/Ixian/yaml/buildings.yaml |
| ixian_stormraider | 5 | mods/cameo/ContentPacks/D2k/Ixian/yaml/vehicles.yaml |
| ixian_supercomputer | 5 | mods/cameo/ContentPacks/D2k/Ixian/yaml/buildings.yaml |
| ixian_twinrockettrooper | 5 | mods/cameo/ContentPacks/D2k/Ixian/yaml/infantry.yaml |
| ixian_windtrap | 5 | mods/cameo/ContentPacks/D2k/Ixian/yaml/buildings.yaml |
| japan_archermaiden | 6 | mods/cameo/rules/redalert.yaml |
| japan_armoredcar | 4 | mods/cameo/rules/redalert.yaml |
| japan_ballista | 5 | mods/cameo/rules/redalert.yaml |
| japan_ballistatower | 4 | mods/cameo/rules/redalert.yaml |
| japan_chihaheavytank | 5 | mods/cameo/rules/redalert.yaml |
| japan_coreairfield | 4 | mods/cameo/rules/redalert.yaml |
| japan_corebarracks | 4 | mods/cameo/rules/redalert.yaml |
| japan_corepowerplant | 4 | mods/cameo/rules/redalert.yaml |
| japan_coreradar | 4 | mods/cameo/rules/redalert.yaml |
| japan_corerefinery | 4 | mods/cameo/rules/redalert.yaml |
| japan_coreservicedepot | 4 | mods/cameo/rules/redalert.yaml |
| japan_coretechcenter | 4 | mods/cameo/rules/redalert.yaml |
| japan_exorcist | 5 | mods/cameo/rules/redalert.yaml |
| japan_exorcistoitank | 5 | mods/cameo/rules/redalert.yaml |
| japan_grenadebuggy | 4 | mods/cameo/rules/redalert.yaml |
| japan_hovercraftflametank | 5 | mods/cameo/rules/redalert.yaml |
| japan_hovercrafttransport | 4 | mods/cameo/rules/redalert.yaml |
| japan_igomediumtank | 5 | mods/cameo/rules/redalert.yaml |
| japan_imperialscoutsman | 6 | mods/cameo/rules/redalert.yaml |
| japan_japaneseairfield | 4 | mods/cameo/rules/redalert.yaml |
| japan_japanesebomber | 4 | mods/cameo/rules/redalert.yaml |
| japan_japaneseconstructionyard | 4 | mods/cameo/rules/redalert.yaml |
| japan_japaneseflamethrower | 5 | mods/cameo/rules/redalert.yaml |
| japan_japanesemgnest | 4 | mods/cameo/rules/redalert.yaml |
| japan_japanesemobileconstructionvehicle | 5 | mods/cameo/rules/redalert.yaml |
| japan_japaneseorerefinery | 4 | mods/cameo/rules/redalert.yaml |
| japan_japaneseoretruck | 5 | mods/cameo/rules/redalert.yaml |
| japan_japaneseradararray | 4 | mods/cameo/rules/redalert.yaml |
| japan_japaneseservicedepot | 4 | mods/cameo/rules/redalert.yaml |
| japan_japaneseshrine | 5 | mods/cameo/rules/redalert.yaml |
| japan_japanesewarfactory | 4 | mods/cameo/rules/redalert.yaml |
| japan_nanodronebuggy | 5 | mods/cameo/rules/redalert.yaml |
| japan_oitank | 5 | mods/cameo/rules/redalert.yaml |
| japan_rocketangel | 4 | mods/cameo/rules/redalert.yaml |
| japan_samurai | 5 | mods/cameo/rules/redalert.yaml |
| japan_scoutcar | 4 | mods/cameo/rules/redalert.yaml |
| japan_shogunexecutioner | 4 | mods/cameo/rules/redalert.yaml |
| japan_shrine_minitank | 5 | mods/cameo/rules/redalert.yaml |
| japan_skyhawk | 4 | mods/cameo/rules/redalert.yaml |
| japan_tankbuster | 5 | mods/cameo/rules/redalert.yaml |
| japan_waveforceartillery | 5 | mods/cameo/rules/redalert.yaml |
| japan_waveforcereactor | 4 | mods/cameo/rules/redalert.yaml |
| japan_waveforcetank | 5 | mods/cameo/rules/redalert.yaml |
| japan_waveforceturret | 4 | mods/cameo/rules/redalert.yaml |
| japan_zerofighter | 4 | mods/cameo/rules/redalert.yaml |
| japancarrier | 4 | mods/cameo/rules/redalert.yaml |
| japanspeedboat | 4 | mods/cameo/rules/redalert.yaml |
| jsuperbomber | 4 | mods/cameo/rules/redalert.yaml |
| jsuperbomber.Husk | 4 | mods/cameo/rules/husks.yaml |
| kami.asian | 4 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/aircraft.yaml |
| kami_asdf.asian | 5 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/aircraft.yaml |
| kami_chemical.asian | 5 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/aircraft.yaml |
| karrier.asian | 4 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/naval.yaml |
| ksub.asian | 4 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/naval.yaml |
| landcarr_drone.futu | 5 | mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/aircraft.yaml |
| latin_syndicate_airstation | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/buildings.yaml |
| latin_syndicate_bunkertower | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/buildings.yaml |
| latin_syndicate_burrito | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/vehicles.yaml |
| latin_syndicate_carteltruck | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/vehicles.yaml |
| latin_syndicate_collectiontruck | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/vehicles.yaml |
| latin_syndicate_combatbarracks | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/buildings.yaml |
| latin_syndicate_defensebureau | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/buildings.yaml |
| latin_syndicate_demolitiontruck | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/vehicles.yaml |
| latin_syndicate_diablo | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/vehicles.yaml |
| latin_syndicate_engineer | 7 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/infantry.yaml |
| latin_syndicate_freedomfighter | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/infantry.yaml |
| latin_syndicate_grenademonkey | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/infantry.yaml |
| latin_syndicate_hindtransport | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/aircraft.yaml |
| latin_syndicate_lars | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/vehicles.yaml |
| latin_syndicate_latinaadefender | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/buildings.yaml |
| latin_syndicate_latinapc | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/vehicles.yaml |
| latin_syndicate_latinempradar | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/buildings.yaml |
| latin_syndicate_latinflametrooper | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/infantry.yaml |
| latin_syndicate_latinmilitia | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/infantry.yaml |
| latin_syndicate_latinsentrygun | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/buildings.yaml |
| latin_syndicate_latintankkiller | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/infantry.yaml |
| latin_syndicate_mig21 | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/aircraft.yaml |
| latin_syndicate_missiletruck | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/vehicles.yaml |
| latin_syndicate_mortarbike | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/vehicles.yaml |
| latin_syndicate_narco | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/infantry.yaml |
| latin_syndicate_narcohummer | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/vehicles.yaml |
| latin_syndicate_nuketruck | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/vehicles.yaml |
| latin_syndicate_powerstation | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/buildings.yaml |
| latin_syndicate_raiderbuggy | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/vehicles.yaml |
| latin_syndicate_recyclingcenter | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/buildings.yaml |
| latin_syndicate_recyclingrefinery | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/buildings.yaml |
| latin_syndicate_rushertank | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/vehicles.yaml |
| latin_syndicate_smlturret | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/buildings.yaml |
| latin_syndicate_smokertank | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/vehicles.yaml |
| latin_syndicate_spycenter | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/buildings.yaml |
| latin_syndicate_syndicateconstructionyard | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/buildings.yaml |
| latin_syndicate_syndicatefactory | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/buildings.yaml |
| latin_syndicate_syndicatemobileconstructionvehicle | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/vehicles.yaml |
| latin_syndicate_syndicateservicedepot | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/buildings.yaml |
| latin_syndicate_terrorist | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/infantry.yaml |
| latin_syndicate_topolm | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/vehicles.yaml |
| latin_syndicate_topolsilo | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/buildings.yaml |
| latin_syndicate_tortugatank | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/vehicles.yaml |
| latin_syndicate_yakovlev | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/aircraft.yaml |
| light_factory | 4 | mods/cameo/ContentPacks/D2k/Shared/yaml/buildings.yaml |
| light_inf | 5 | mods/cameo/ContentPacks/D2k/Shared/yaml/infantry.yaml |
| lsub.asian | 4 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/naval.yaml |
| mammothbunker.husk | 4 | mods/cameo/rules/tech.yaml |
| missile_tank_husk.ixian | 4 | mods/cameo/ContentPacks/D2k/Ixian/yaml/vehicles.yaml |
| modbomber.Husk | 4 | mods/cameo/rules/husks.yaml |
| modhip.husk | 4 | mods/cameo/rules/redalert.yaml |
| modkami.Husk | 4 | mods/cameo/rules/husks.yaml |
| modkamimini.Husk | 4 | mods/cameo/rules/husks.yaml |
| muboat.nax | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/naval.yaml |
| naval.nax | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/buildings.yaml |
| naxis_academy | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/buildings.yaml |
| naxis_airfield | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/buildings.yaml |
| naxis_antitankcannon | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/infantry.yaml |
| naxis_barracks | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/buildings.yaml |
| naxis_beerfactory | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/buildings.yaml |
| naxis_bf109 | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/aircraft.yaml |
| naxis_bmwbike | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/infantry.yaml |
| naxis_brummbr | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/vehicles.yaml |
| naxis_coneheadsknights | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/infantry.yaml |
| naxis_constructionyard | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/buildings.yaml |
| naxis_donnerschlag | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/vehicles.yaml |
| naxis_engineeringtruck | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/vehicles.yaml |
| naxis_flak88 | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/buildings.yaml |
| naxis_grille | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/vehicles.yaml |
| naxis_halftrack | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/vehicles.yaml |
| naxis_hetzer | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/vehicles.yaml |
| naxis_imperialturbotank | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/vehicles.yaml |
| naxis_jagdpanzer | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/vehicles.yaml |
| naxis_kbelwagen | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/vehicles.yaml |
| naxis_kingtigerheavytank | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/vehicles.yaml |
| naxis_maus | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/vehicles.yaml |
| naxis_me262 | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/aircraft.yaml |
| naxis_naxibunker | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/buildings.yaml |
| naxis_naxiflamer | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/infantry.yaml |
| naxis_naximachinegunners | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/infantry.yaml |
| naxis_naximercenarysniper | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/infantry.yaml |
| naxis_naximobileconstructionvehicle | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/vehicles.yaml |
| naxis_naxiriflerecruit | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/infantry.yaml |
| naxis_naxiriflesoldier | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/infantry.yaml |
| naxis_naxirocketsilo | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/buildings.yaml |
| naxis_naxpetrolplant | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/buildings.yaml |
| naxis_nokana | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/vehicles.yaml |
| naxis_nop03sarubia | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/vehicles.yaml |
| naxis_oldtank | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/vehicles.yaml |
| naxis_orerefinery | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/buildings.yaml |
| naxis_panzerfausttrooper | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/infantry.yaml |
| naxis_panzerschreck | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/infantry.yaml |
| naxis_portableflak | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/infantry.yaml |
| naxis_radar | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/buildings.yaml |
| naxis_ratte | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/vehicles.yaml |
| naxis_rifletower | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/buildings.yaml |
| naxis_sausagefactory | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/buildings.yaml |
| naxis_shoekarn | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/vehicles.yaml |
| naxis_skymage | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/infantry.yaml |
| naxis_slave | 6 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/infantry.yaml |
| naxis_slaveoverseer | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/infantry.yaml |
| naxis_sssoldier | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/infantry.yaml |
| naxis_sturmtiger | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/vehicles.yaml |
| naxis_techcenter | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/buildings.yaml |
| naxis_transportzeppelin | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/aircraft.yaml |
| naxis_warfactory | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/buildings.yaml |
| nodlasercorvette | 4 | mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/naval.yaml |
| nodvenom.husk | 4 | mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/aircraft.yaml |
| oldqtnk.steel | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/vehicles.yaml |
| orcs_altarofstorms | 4 | mods/cameo/rules/warcraft2.yaml |
| orcs_barracks | 4 | mods/cameo/rules/warcraft2.yaml |
| orcs_blacksmith | 4 | mods/cameo/rules/warcraft2.yaml |
| orcs_cannontower | 5 | mods/cameo/rules/warcraft2.yaml |
| orcs_catapult | 5 | mods/cameo/rules/warcraft2.yaml |
| orcs_deathknight | 6 | mods/cameo/rules/warcraft2.yaml |
| orcs_dragon | 5 | mods/cameo/rules/warcraft2.yaml |
| orcs_dragonroost | 4 | mods/cameo/rules/warcraft2.yaml |
| orcs_goblinsappers | 6 | mods/cameo/rules/warcraft2.yaml |
| orcs_goblinzeppelin | 5 | mods/cameo/rules/warcraft2.yaml |
| orcs_golbinalchemist | 4 | mods/cameo/rules/warcraft2.yaml |
| orcs_grunt | 6 | mods/cameo/rules/warcraft2.yaml |
| orcs_guardtower | 5 | mods/cameo/rules/warcraft2.yaml |
| orcs_kodobeast | 4 | mods/cameo/rules/warcraft2.yaml |
| orcs_mobileconstructionvehicleorc | 5 | mods/cameo/rules/warcraft2.yaml |
| orcs_ogre | 5 | mods/cameo/rules/warcraft2.yaml |
| orcs_ogremage | 6 | mods/cameo/rules/warcraft2.yaml |
| orcs_ogremound | 4 | mods/cameo/rules/warcraft2.yaml |
| orcs_orcgoldmine | 4 | mods/cameo/rules/warcraft2.yaml |
| orcs_orcgoldmine_2 | 5 | mods/cameo/rules/warcraft2.yaml |
| orcs_orcwatchtower | 4 | mods/cameo/rules/warcraft2.yaml |
| orcs_peon | 6 | mods/cameo/rules/warcraft2.yaml |
| orcs_pigfarm | 5 | mods/cameo/rules/warcraft2.yaml |
| orcs_siegeengine | 5 | mods/cameo/rules/warcraft2.yaml |
| orcs_templeofthedamned | 6 | mods/cameo/rules/warcraft2.yaml |
| orcs_trollaxethrower | 6 | mods/cameo/rules/warcraft2.yaml |
| orcs_trollberserker | 7 | mods/cameo/rules/warcraft2.yaml |
| orcs_trollheadhunter | 6 | mods/cameo/rules/warcraft2.yaml |
| orcs_trolllumbermill | 4 | mods/cameo/rules/warcraft2.yaml |
| orcs_warcraft3grunt | 6 | mods/cameo/rules/warcraft2.yaml |
| ordos_advancedcarryall | 5 | mods/cameo/ContentPacks/D2k/Ordos/yaml/aircraft.yaml |
| ordos_airmine | 4 | mods/cameo/ContentPacks/D2k/Ordos/yaml/aircraft.yaml |
| ordos_antiairtrooper | 5 | mods/cameo/ContentPacks/D2k/Ordos/yaml/infantry.yaml |
| ordos_apc | 5 | mods/cameo/ContentPacks/D2k/Ordos/yaml/vehicles.yaml |
| ordos_artilleryplatform | 5 | mods/cameo/ContentPacks/D2k/Ordos/yaml/buildings.yaml |
| ordos_autogunturret | 5 | mods/cameo/ContentPacks/D2k/Ordos/yaml/buildings.yaml |
| ordos_banshee | 4 | mods/cameo/ContentPacks/D2k/Ordos/yaml/aircraft.yaml |
| ordos_barracks | 4 | mods/cameo/ContentPacks/D2k/Ordos/yaml/buildings.yaml |
| ordos_chemicaltrooper | 5 | mods/cameo/ContentPacks/D2k/Ordos/yaml/infantry.yaml |
| ordos_cobratank | 5 | mods/cameo/ContentPacks/D2k/Ordos/yaml/vehicles.yaml |
| ordos_combatautoguntank | 5 | mods/cameo/ContentPacks/D2k/Ordos/yaml/vehicles.yaml |
| ordos_combattank | 5 | mods/cameo/ContentPacks/D2k/Ordos/yaml/vehicles.yaml |
| ordos_constructionyard | 5 | mods/cameo/ContentPacks/D2k/Ordos/yaml/buildings.yaml |
| ordos_contaminator | 5 | mods/cameo/ContentPacks/D2k/Ordos/yaml/infantry.yaml |
| ordos_deviatorartillery | 5 | mods/cameo/ContentPacks/D2k/Ordos/yaml/vehicles.yaml |
| ordos_deviatortank | 5 | mods/cameo/ContentPacks/D2k/Ordos/yaml/vehicles.yaml |
| ordos_dustdrone | 5 | mods/cameo/ContentPacks/D2k/Ordos/yaml/vehicles.yaml |
| ordos_eyeinthesky | 4 | mods/cameo/ContentPacks/D2k/Ordos/yaml/aircraft.yaml |
| ordos_facedancer | 5 | mods/cameo/ContentPacks/D2k/Ordos/yaml/infantry.yaml |
| ordos_heavyautoguntank | 5 | mods/cameo/ContentPacks/D2k/Ordos/yaml/vehicles.yaml |
| ordos_heavycombattank | 5 | mods/cameo/ContentPacks/D2k/Ordos/yaml/vehicles.yaml |
| ordos_heavyfactory | 5 | mods/cameo/ContentPacks/D2k/Ordos/yaml/buildings.yaml |
| ordos_hightechfactory | 5 | mods/cameo/ContentPacks/D2k/Ordos/yaml/buildings.yaml |
| ordos_ixresearchcenter | 5 | mods/cameo/ContentPacks/D2k/Ordos/yaml/buildings.yaml |
| ordos_laboratorycrawler | 5 | mods/cameo/ContentPacks/D2k/Ordos/yaml/vehicles.yaml |
| ordos_lasertank | 5 | mods/cameo/ContentPacks/D2k/Ordos/yaml/vehicles.yaml |
| ordos_leech | 5 | mods/cameo/ContentPacks/D2k/Ordos/yaml/infantry.yaml |
| ordos_lightfactory | 5 | mods/cameo/ContentPacks/D2k/Ordos/yaml/buildings.yaml |
| ordos_lightinfantry | 5 | mods/cameo/ContentPacks/D2k/Ordos/yaml/infantry.yaml |
| ordos_mobileconstructionvehicle | 5 | mods/cameo/ContentPacks/D2k/Ordos/yaml/vehicles.yaml |
| ordos_mortartrooper | 5 | mods/cameo/ContentPacks/D2k/Ordos/yaml/infantry.yaml |
| ordos_outpost | 4 | mods/cameo/ContentPacks/D2k/Ordos/yaml/buildings.yaml |
| ordos_palace | 5 | mods/cameo/ContentPacks/D2k/Ordos/yaml/buildings.yaml |
| ordos_pythontank | 5 | mods/cameo/ContentPacks/D2k/Ordos/yaml/vehicles.yaml |
| ordos_raider | 4 | mods/cameo/ContentPacks/D2k/Ordos/yaml/vehicles.yaml |
| ordos_refineryordos | 5 | mods/cameo/ContentPacks/D2k/Ordos/yaml/buildings.yaml |
| ordos_repairpad | 4 | mods/cameo/ContentPacks/D2k/Ordos/yaml/buildings.yaml |
| ordos_rockettrooper | 5 | mods/cameo/ContentPacks/D2k/Ordos/yaml/infantry.yaml |
| ordos_saboteur | 5 | mods/cameo/ContentPacks/D2k/Ordos/yaml/infantry.yaml |
| ordos_spiceharvester | 5 | mods/cameo/ContentPacks/D2k/Ordos/yaml/vehicles.yaml |
| ordos_starport | 6 | mods/cameo/ContentPacks/D2k/Ordos/yaml/buildings.yaml |
| ordos_stealthraider | 5 | mods/cameo/ContentPacks/D2k/Ordos/yaml/vehicles.yaml |
| ordos_storagesilo | 5 | mods/cameo/ContentPacks/D2k/Ordos/yaml/buildings.yaml |
| ordos_swarmerdrone | 4 | mods/cameo/ContentPacks/D2k/Ordos/yaml/aircraft.yaml |
| ordos_tankdestroyer | 5 | mods/cameo/ContentPacks/D2k/Ordos/yaml/vehicles.yaml |
| ordos_windtrap | 5 | mods/cameo/ContentPacks/D2k/Ordos/yaml/buildings.yaml |
| ordos_wraith | 4 | mods/cameo/ContentPacks/D2k/Ordos/yaml/aircraft.yaml |
| panth.asian | 4 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/naval.yaml |
| panzer.nax | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/vehicles.yaml |
| protoss_adept | 5 | mods/cameo/rules/starcraft.yaml |
| protoss_amaranth | 5 | mods/cameo/rules/starcraft.yaml |
| protoss_analogue | 6 | mods/cameo/rules/starcraft.yaml |
| protoss_arbiter | 5 | mods/cameo/rules/starcraft.yaml |
| protoss_arbitertribunal | 4 | mods/cameo/rules/starcraft.yaml |
| protoss_archon | 6 | mods/cameo/rules/starcraft.yaml |
| protoss_assimilator | 4 | mods/cameo/rules/starcraft.yaml |
| protoss_atreus | 6 | mods/cameo/rules/starcraft.yaml |
| protoss_carrier | 5 | mods/cameo/rules/starcraft.yaml |
| protoss_citadelofadun | 4 | mods/cameo/rules/starcraft.yaml |
| protoss_corsair | 5 | mods/cameo/rules/starcraft.yaml |
| protoss_cyberneticscore | 4 | mods/cameo/rules/starcraft.yaml |
| protoss_darktemplar | 5 | mods/cameo/rules/starcraft.yaml |
| protoss_dragoon | 6 | mods/cameo/rules/starcraft.yaml |
| protoss_epigraph | 5 | mods/cameo/rules/starcraft.yaml |
| protoss_fleetbeacon | 5 | mods/cameo/rules/starcraft.yaml |
| protoss_forge | 4 | mods/cameo/rules/starcraft.yaml |
| protoss_gateway | 4 | mods/cameo/rules/starcraft.yaml |
| protoss_gladius | 5 | mods/cameo/rules/starcraft.yaml |
| protoss_hightemplar | 5 | mods/cameo/rules/starcraft.yaml |
| protoss_idol | 6 | mods/cameo/rules/starcraft.yaml |
| protoss_legionnaire | 5 | mods/cameo/rules/starcraft.yaml |
| protoss_manifold | 6 | mods/cameo/rules/starcraft.yaml |
| protoss_mobilenexus | 4 | mods/cameo/rules/starcraft.yaml |
| protoss_nexus | 4 | mods/cameo/rules/starcraft.yaml |
| protoss_observatory | 4 | mods/cameo/rules/starcraft.yaml |
| protoss_observer | 5 | mods/cameo/rules/starcraft.yaml |
| protoss_patriarch | 5 | mods/cameo/rules/starcraft.yaml |
| protoss_photoncannon | 4 | mods/cameo/rules/starcraft.yaml |
| protoss_positron | 6 | mods/cameo/rules/starcraft.yaml |
| protoss_probe | 4 | mods/cameo/rules/starcraft.yaml |
| protoss_pylon | 4 | mods/cameo/rules/starcraft.yaml |
| protoss_reaver | 6 | mods/cameo/rules/starcraft.yaml |
| protoss_roboticsfacility | 4 | mods/cameo/rules/starcraft.yaml |
| protoss_roboticssupportbay | 4 | mods/cameo/rules/starcraft.yaml |
| protoss_scout | 5 | mods/cameo/rules/starcraft.yaml |
| protoss_shieldbattery | 4 | mods/cameo/rules/starcraft.yaml |
| protoss_shuttle | 5 | mods/cameo/rules/starcraft.yaml |
| protoss_stargate | 4 | mods/cameo/rules/starcraft.yaml |
| protoss_starshipsovereign | 5 | mods/cameo/rules/starcraft.yaml |
| protoss_templararchives | 4 | mods/cameo/rules/starcraft.yaml |
| protoss_voidray | 5 | mods/cameo/rules/starcraft.yaml |
| protoss_zealot | 5 | mods/cameo/rules/starcraft.yaml |
| protoss_zeratul | 5 | mods/cameo/rules/starcraft.yaml |
| ptnk.asian | 5 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/vehicles.yaml |
| quasfrig.asian | 4 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/naval.yaml |
| ra1_allies_alliedaagun | 4 | mods/cameo/rules/redalert.yaml |
| ra1_allies_alliedartillery | 5 | mods/cameo/rules/redalert.yaml |
| ra1_allies_alliedchinooktransport | 5 | mods/cameo/rules/redalert.yaml |
| ra1_allies_alliedconstructionyard | 4 | mods/cameo/rules/redalert.yaml |
| ra1_allies_alliedgunturret | 4 | mods/cameo/rules/redalert.yaml |
| ra1_allies_alliedheavyaatank | 4 | mods/cameo/rules/redalert.yaml |
| ra1_allies_alliedlighttank | 5 | mods/cameo/rules/redalert.yaml |
| ra1_allies_alliedmediumtank | 5 | mods/cameo/rules/redalert.yaml |
| ra1_allies_alliedmobileconstructionvehicle | 5 | mods/cameo/rules/redalert.yaml |
| ra1_allies_alliedorerefinery | 4 | mods/cameo/rules/redalert.yaml |
| ra1_allies_alliedoretruck | 5 | mods/cameo/rules/redalert.yaml |
| ra1_allies_alliedradardome | 4 | mods/cameo/rules/redalert.yaml |
| ra1_allies_alliedservicedepot | 4 | mods/cameo/rules/redalert.yaml |
| ra1_allies_alliedsniper | 5 | mods/cameo/rules/redalert.yaml |
| ra1_allies_alliedtankdestroyer | 5 | mods/cameo/rules/redalert.yaml |
| ra1_allies_alliedtigerheavytank | 5 | mods/cameo/rules/redalert.yaml |
| ra1_allies_bastionartillerybunker | 4 | mods/cameo/rules/redalert.yaml |
| ra1_allies_blackhawk | 4 | mods/cameo/rules/redalert.yaml |
| ra1_allies_camopillbox | 4 | mods/cameo/rules/redalert.yaml |
| ra1_allies_chronosphere | 5 | mods/cameo/rules/redalert.yaml |
| ra1_allies_chronotank | 4 | mods/cameo/rules/redalert.yaml |
| ra1_allies_gapgenerator | 4 | mods/cameo/rules/redalert.yaml |
| ra1_allies_longbow | 4 | mods/cameo/rules/redalert.yaml |
| ra1_allies_machinegunner | 5 | mods/cameo/rules/redalert.yaml |
| ra1_allies_mechanic | 5 | mods/cameo/rules/redalert.yaml |
| ra1_allies_medic | 5 | mods/cameo/rules/redalert.yaml |
| ra1_allies_minelayer | 4 | mods/cameo/rules/redalert.yaml |
| ra1_allies_mobilegapgenerator | 4 | mods/cameo/rules/redalert.yaml |
| ra1_allies_mobileradarjammer | 4 | mods/cameo/rules/redalert.yaml |
| ra1_allies_phasetransport | 4 | mods/cameo/rules/redalert.yaml |
| ra1_allies_pillbox | 4 | mods/cameo/rules/redalert.yaml |
| ra1_allies_ranger | 4 | mods/cameo/rules/redalert.yaml |
| ra1_allies_rapierjumpjet | 4 | mods/cameo/rules/redalert.yaml |
| ra1_allies_raspy | 5 | mods/cameo/rules/redalert.yaml |
| ra1_allies_reconranger | 4 | mods/cameo/rules/redalert.yaml |
| ra1_allies_reinforcementpad | 5 | mods/cameo/rules/redalert.yaml |
| ra1_allies_sheridanassaulttank | 5 | mods/cameo/rules/redalert.yaml |
| ra1_allies_tanya | 5 | mods/cameo/rules/redalert.yaml |
| ra1_soviet_ak47conscript | 5 | mods/cameo/rules/redalert.yaml |
| ra1_soviet_armoredyak | 4 | mods/cameo/rules/redalert.yaml |
| ra1_soviet_attackdog | 5 | mods/cameo/rules/redalert.yaml |
| ra1_soviet_btr80 | 4 | mods/cameo/rules/redalert.yaml |
| ra1_soviet_commissar | 5 | mods/cameo/rules/redalert.yaml |
| ra1_soviet_constructionyard | 4 | mods/cameo/rules/redalert.yaml |
| ra1_soviet_cyberdog | 5 | mods/cameo/rules/redalert.yaml |
| ra1_soviet_dragunovantimaterialsniper | 5 | mods/cameo/rules/redalert.yaml |
| ra1_soviet_firerocketsoldier | 6 | mods/cameo/rules/redalert.yaml |
| ra1_soviet_flaktruck | 4 | mods/cameo/rules/redalert.yaml |
| ra1_soviet_flamethrower | 5 | mods/cameo/rules/redalert.yaml |
| ra1_soviet_flametower | 4 | mods/cameo/rules/redalert.yaml |
| ra1_soviet_gatlingtank | 4 | mods/cameo/rules/redalert.yaml |
| ra1_soviet_gorynychtank | 4 | mods/cameo/rules/redalert.yaml |
| ra1_soviet_grad | 5 | mods/cameo/rules/redalert.yaml |
| ra1_soviet_grenadier | 5 | mods/cameo/rules/redalert.yaml |
| ra1_soviet_hammertank | 5 | mods/cameo/rules/redalert.yaml |
| ra1_soviet_heatraytank | 5 | mods/cameo/rules/redalert.yaml |
| ra1_soviet_heavyindustrialminer | 6 | mods/cameo/rules/redalert.yaml |
| ra1_soviet_heavytank | 5 | mods/cameo/rules/redalert.yaml |
| ra1_soviet_heavyteslatank | 5 | mods/cameo/rules/redalert.yaml |
| ra1_soviet_hindattackhelicopter | 4 | mods/cameo/rules/redalert.yaml |
| ra1_soviet_hiptransport | 5 | mods/cameo/rules/redalert.yaml |
| ra1_soviet_ironcurtain | 4 | mods/cameo/rules/redalert.yaml |
| ra1_soviet_kamovattackhelicopter | 4 | mods/cameo/rules/redalert.yaml |
| ra1_soviet_kotinnucleartank | 5 | mods/cameo/rules/redalert.yaml |
| ra1_soviet_largefactory | 4 | mods/cameo/rules/redalert.yaml |
| ra1_soviet_largesovietairfield | 4 | mods/cameo/rules/redalert.yaml |
| ra1_soviet_madtank | 5 | mods/cameo/rules/redalert.yaml |
| ra1_soviet_mammothtank | 5 | mods/cameo/rules/redalert.yaml |
| ra1_soviet_migattackbomber | 4 | mods/cameo/rules/redalert.yaml |
| ra1_soviet_missilesilo | 5 | mods/cameo/rules/redalert.yaml |
| ra1_soviet_mobileconstructionvehicle | 5 | mods/cameo/rules/redalert.yaml |
| ra1_soviet_monstertank | 4 | mods/cameo/rules/redalert.yaml |
| ra1_soviet_mortarsoldier | 5 | mods/cameo/rules/redalert.yaml |
| ra1_soviet_nuclearv2launcher | 5 | mods/cameo/rules/redalert.yaml |
| ra1_soviet_nuclearyak | 4 | mods/cameo/rules/redalert.yaml |
| ra1_soviet_orerefinery | 4 | mods/cameo/rules/redalert.yaml |
| ra1_soviet_oretruck | 5 | mods/cameo/rules/redalert.yaml |
| ra1_soviet_radardome | 4 | mods/cameo/rules/redalert.yaml |
| ra1_soviet_rifleinfantry | 6 | mods/cameo/rules/redalert.yaml |
| ra1_soviet_rocketsoldier | 6 | mods/cameo/rules/redalert.yaml |
| ra1_soviet_samsite | 4 | mods/cameo/rules/redalert.yaml |
| ra1_soviet_servicedepot | 4 | mods/cameo/rules/redalert.yaml |
| ra1_soviet_shocktrooper | 5 | mods/cameo/rules/redalert.yaml |
| ra1_soviet_siegemammothtank | 5 | mods/cameo/rules/redalert.yaml |
| ra1_soviet_stalinfist | 5 | mods/cameo/rules/redalert.yaml |
| ra1_soviet_su57attackbomber | 4 | mods/cameo/rules/redalert.yaml |
| ra1_soviet_supersonicnuclearbomber | 4 | mods/cameo/rules/redalert.yaml |
| ra1_soviet_teslacoil | 4 | mods/cameo/rules/redalert.yaml |
| ra1_soviet_teslatank | 5 | mods/cameo/rules/redalert.yaml |
| ra1_soviet_teslayak | 4 | mods/cameo/rules/redalert.yaml |
| ra1_soviet_v1rockettruck | 5 | mods/cameo/rules/redalert.yaml |
| ra1_soviet_v2rocketlauncher | 5 | mods/cameo/rules/redalert.yaml |
| ra1_soviet_volkov | 5 | mods/cameo/rules/redalert.yaml |
| ra1_soviet_yakscoutplane | 4 | mods/cameo/rules/redalert.yaml |
| ra1_soviet_zapper | 5 | mods/cameo/rules/redalert.yaml |
| ra2_allies_aegiscruiser | 4 | mods/cameo/rules/redalert2.yaml |
| ra2_allies_airforcecommandhq | 4 | mods/cameo/rules/redalert2.yaml |
| ra2_allies_alliedbarracks | 4 | mods/cameo/rules/redalert2.yaml |
| ra2_allies_alliedbattlelab | 4 | mods/cameo/rules/redalert2.yaml |
| ra2_allies_alliedconstructionyard | 4 | mods/cameo/rules/redalert2.yaml |
| ra2_allies_alliedmobileconstructionvehicle | 4 | mods/cameo/rules/redalert2.yaml |
| ra2_allies_alliedorerefinery | 4 | mods/cameo/rules/redalert2.yaml |
| ra2_allies_alliedpowerplant | 4 | mods/cameo/rules/redalert2.yaml |
| ra2_allies_alliedservicedepot | 4 | mods/cameo/rules/redalert2.yaml |
| ra2_allies_alliedwarfactory | 5 | mods/cameo/rules/redalert2.yaml |
| ra2_allies_attackdog | 5 | mods/cameo/rules/redalert2.yaml |
| ra2_allies_battlefortress | 5 | mods/cameo/rules/redalert2.yaml |
| ra2_allies_battlefortress_2 | 6 | mods/cameo/rules/redalert2.yaml |
| ra2_allies_battlefortress_3 | 6 | mods/cameo/rules/redalert2.yaml |
| ra2_allies_blackeagle | 4 | mods/cameo/rules/redalert2.yaml |
| ra2_allies_chronolegionnaire | 5 | mods/cameo/rules/redalert2.yaml |
| ra2_allies_chronominer | 4 | mods/cameo/rules/redalert2.yaml |
| ra2_allies_chronosphere | 4 | mods/cameo/rules/redalert2.yaml |
| ra2_allies_engineer | 6 | mods/cameo/rules/redalert2.yaml |
| ra2_allies_gapgenerator | 5 | mods/cameo/rules/redalert2.yaml |
| ra2_allies_gi | 5 | mods/cameo/rules/redalert2.yaml |
| ra2_allies_grandcannon | 5 | mods/cameo/rules/redalert2.yaml |
| ra2_allies_grizzlytank | 5 | mods/cameo/rules/redalert2.yaml |
| ra2_allies_guardiangi | 5 | mods/cameo/rules/redalert2.yaml |
| ra2_allies_harrier | 4 | mods/cameo/rules/redalert2.yaml |
| ra2_allies_heavymiragetank | 5 | mods/cameo/rules/redalert2.yaml |
| ra2_allies_ifv | 4 | mods/cameo/rules/redalert2.yaml |
| ra2_allies_ifv_chrono | 6 | mods/cameo/rules/redalert2.yaml |
| ra2_allies_ifv_hmg | 5 | mods/cameo/rules/redalert2.yaml |
| ra2_allies_ifv_mg | 5 | mods/cameo/rules/redalert2.yaml |
| ra2_allies_ifv_missile | 5 | mods/cameo/rules/redalert2.yaml |
| ra2_allies_ifv_repair | 6 | mods/cameo/rules/redalert2.yaml |
| ra2_allies_miragetank | 5 | mods/cameo/rules/redalert2.yaml |
| ra2_allies_nighthawk | 4 | mods/cameo/rules/redalert2.yaml |
| ra2_allies_orepurifier | 5 | mods/cameo/rules/redalert2.yaml |
| ra2_allies_patriotmissilesystem | 5 | mods/cameo/rules/redalert2.yaml |
| ra2_allies_pillbox | 5 | mods/cameo/rules/redalert2.yaml |
| ra2_allies_prismtank | 5 | mods/cameo/rules/redalert2.yaml |
| ra2_allies_prismtower | 5 | mods/cameo/rules/redalert2.yaml |
| ra2_allies_ra2spy | 5 | mods/cameo/rules/redalert2.yaml |
| ra2_allies_rocketeer | 4 | mods/cameo/rules/redalert2.yaml |
| ra2_allies_seal | 5 | mods/cameo/rules/redalert2.yaml |
| ra2_allies_sniper | 5 | mods/cameo/rules/redalert2.yaml |
| ra2_allies_spysatelliteuplink | 4 | mods/cameo/rules/redalert2.yaml |
| ra2_allies_tankdestroyer | 5 | mods/cameo/rules/redalert2.yaml |
| ra2_allies_tanyaii | 5 | mods/cameo/rules/redalert2.yaml |
| ra2_allies_weathercontrolcenter | 5 | mods/cameo/rules/redalert2.yaml |
| ra2_ambu | 4 | mods/cameo/rules/redalert2.yaml |
| ra2_ambu_demo | 4 | mods/cameo/rules/redalert2.yaml |
| ra2_ambu_driveby | 5 | mods/cameo/rules/redalert2.yaml |
| ra2_bcab | 4 | mods/cameo/rules/redalert2.yaml |
| ra2_bcab_demo | 4 | mods/cameo/rules/redalert2.yaml |
| ra2_bcab_driveby | 5 | mods/cameo/rules/redalert2.yaml |
| ra2_bus | 4 | mods/cameo/rules/redalert2.yaml |
| ra2_bus_demo | 4 | mods/cameo/rules/redalert2.yaml |
| ra2_bus_driveby | 5 | mods/cameo/rules/redalert2.yaml |
| ra2_c_abram | 5 | mods/cameo/rules/redalert2.yaml |
| ra2_c_hum | 4 | mods/cameo/rules/redalert2.yaml |
| ra2_c_hum2 | 5 | mods/cameo/rules/redalert2.yaml |
| ra2_c_ifv | 5 | mods/cameo/rules/redalert2.yaml |
| ra2_car | 4 | mods/cameo/rules/redalert2.yaml |
| ra2_car_demo | 4 | mods/cameo/rules/redalert2.yaml |
| ra2_car_driveby | 5 | mods/cameo/rules/redalert2.yaml |
| ra2_cgcloa.husk | 4 | mods/cameo/rules/redalert2.yaml |
| ra2_city01 | 6 | mods/cameo/rules/redalert2.yaml |
| ra2_city02 | 6 | mods/cameo/rules/redalert2.yaml |
| ra2_city03 | 6 | mods/cameo/rules/redalert2.yaml |
| ra2_city04 | 6 | mods/cameo/rules/redalert2.yaml |
| ra2_city06 | 6 | mods/cameo/rules/redalert2.yaml |
| ra2_cona | 4 | mods/cameo/rules/redalert2.yaml |
| ra2_cona_demo | 4 | mods/cameo/rules/redalert2.yaml |
| ra2_cona_driveby | 5 | mods/cameo/rules/redalert2.yaml |
| ra2_cop | 4 | mods/cameo/rules/redalert2.yaml |
| ra2_cop_demo | 4 | mods/cameo/rules/redalert2.yaml |
| ra2_cop_driveby | 5 | mods/cameo/rules/redalert2.yaml |
| ra2_ctfrmb | 5 | mods/cameo/rules/redalert2.yaml |
| ra2_ctgard01 | 5 | mods/cameo/rules/redalert2.yaml |
| ra2_ctgard03 | 5 | mods/cameo/rules/redalert2.yaml |
| ra2_ctnwy09 | 6 | mods/cameo/rules/redalert2.yaml |
| ra2_ctnwy22 | 6 | mods/cameo/rules/redalert2.yaml |
| ra2_ctnwy23 | 6 | mods/cameo/rules/redalert2.yaml |
| ra2_ctnwy24 | 6 | mods/cameo/rules/redalert2.yaml |
| ra2_ctnwy25 | 6 | mods/cameo/rules/redalert2.yaml |
| ra2_ctoutp.husk | 4 | mods/cameo/rules/redalert2.yaml |
| ra2_ctpars02 | 6 | mods/cameo/rules/redalert2.yaml |
| ra2_ctpars04 | 6 | mods/cameo/rules/redalert2.yaml |
| ra2_ctpars05 | 6 | mods/cameo/rules/redalert2.yaml |
| ra2_ctpars06 | 6 | mods/cameo/rules/redalert2.yaml |
| ra2_ctpars07 | 5 | mods/cameo/rules/redalert2.yaml |
| ra2_ctpars08 | 6 | mods/cameo/rules/redalert2.yaml |
| ra2_ctpars09 | 6 | mods/cameo/rules/redalert2.yaml |
| ra2_ctpars10 | 6 | mods/cameo/rules/redalert2.yaml |
| ra2_ctpars12 | 6 | mods/cameo/rules/redalert2.yaml |
| ra2_ctpars13 | 6 | mods/cameo/rules/redalert2.yaml |
| ra2_ctpars14 | 6 | mods/cameo/rules/redalert2.yaml |
| ra2_ctrus03 | 6 | mods/cameo/rules/redalert2.yaml |
| ra2_ctrus04 | 6 | mods/cameo/rules/redalert2.yaml |
| ra2_ctrus05 | 6 | mods/cameo/rules/redalert2.yaml |
| ra2_ctrus06 | 6 | mods/cameo/rules/redalert2.yaml |
| ra2_ctrus07 | 6 | mods/cameo/rules/redalert2.yaml |
| ra2_ctrus08 | 6 | mods/cameo/rules/redalert2.yaml |
| ra2_ctrus09 | 6 | mods/cameo/rules/redalert2.yaml |
| ra2_ctrus10 | 6 | mods/cameo/rules/redalert2.yaml |
| ra2_ctrus11 | 6 | mods/cameo/rules/redalert2.yaml |
| ra2_ctsanf01 | 6 | mods/cameo/rules/redalert2.yaml |
| ra2_ctsanf02 | 6 | mods/cameo/rules/redalert2.yaml |
| ra2_ctsanf03 | 6 | mods/cameo/rules/redalert2.yaml |
| ra2_ctsanf05 | 6 | mods/cameo/rules/redalert2.yaml |
| ra2_ctsanf06 | 6 | mods/cameo/rules/redalert2.yaml |
| ra2_ctsanf07 | 6 | mods/cameo/rules/redalert2.yaml |
| ra2_ctsanf08 | 6 | mods/cameo/rules/redalert2.yaml |
| ra2_ctsanf16 | 6 | mods/cameo/rules/redalert2.yaml |
| ra2_ctsanf17 | 6 | mods/cameo/rules/redalert2.yaml |
| ra2_ctsanf18 | 6 | mods/cameo/rules/redalert2.yaml |
| ra2_cttexs01 | 6 | mods/cameo/rules/redalert2.yaml |
| ra2_cttexs02 | 6 | mods/cameo/rules/redalert2.yaml |
| ra2_cttexs03 | 6 | mods/cameo/rules/redalert2.yaml |
| ra2_cttexs04 | 6 | mods/cameo/rules/redalert2.yaml |
| ra2_cttexs05 | 6 | mods/cameo/rules/redalert2.yaml |
| ra2_cttexs06 | 6 | mods/cameo/rules/redalert2.yaml |
| ra2_cttexs07 | 6 | mods/cameo/rules/redalert2.yaml |
| ra2_cttexs08 | 6 | mods/cameo/rules/redalert2.yaml |
| ra2_ctwash03 | 6 | mods/cameo/rules/redalert2.yaml |
| ra2_ctwash04 | 6 | mods/cameo/rules/redalert2.yaml |
| ra2_ctwash05 | 6 | mods/cameo/rules/redalert2.yaml |
| ra2_ctwash06 | 6 | mods/cameo/rules/redalert2.yaml |
| ra2_ctwash07 | 6 | mods/cameo/rules/redalert2.yaml |
| ra2_ctwash08 | 6 | mods/cameo/rules/redalert2.yaml |
| ra2_ctwash09 | 6 | mods/cameo/rules/redalert2.yaml |
| ra2_ctwash10 | 6 | mods/cameo/rules/redalert2.yaml |
| ra2_ctwash11 | 6 | mods/cameo/rules/redalert2.yaml |
| ra2_ctwash13 | 6 | mods/cameo/rules/redalert2.yaml |
| ra2_ctwash17 | 6 | mods/cameo/rules/redalert2.yaml |
| ra2_ddbx | 4 | mods/cameo/rules/redalert2.yaml |
| ra2_ddbx_demo | 4 | mods/cameo/rules/redalert2.yaml |
| ra2_ddbx_driveby | 5 | mods/cameo/rules/redalert2.yaml |
| ra2_euroc | 4 | mods/cameo/rules/redalert2.yaml |
| ra2_euroc_demo | 4 | mods/cameo/rules/redalert2.yaml |
| ra2_euroc_driveby | 5 | mods/cameo/rules/redalert2.yaml |
| ra2_jeep | 4 | mods/cameo/rules/redalert2.yaml |
| ra2_jeep_demo | 4 | mods/cameo/rules/redalert2.yaml |
| ra2_jeep_driveby | 5 | mods/cameo/rules/redalert2.yaml |
| ra2_limo | 4 | mods/cameo/rules/redalert2.yaml |
| ra2_limo_demo | 4 | mods/cameo/rules/redalert2.yaml |
| ra2_limo_driveby | 5 | mods/cameo/rules/redalert2.yaml |
| ra2_ptruck | 4 | mods/cameo/rules/redalert2.yaml |
| ra2_ptruck_demo | 4 | mods/cameo/rules/redalert2.yaml |
| ra2_ptruck_driveby | 5 | mods/cameo/rules/redalert2.yaml |
| ra2_soviets_airfield | 4 | mods/cameo/rules/redalert2.yaml |
| ra2_soviets_apocalypsetank | 5 | mods/cameo/rules/redalert2.yaml |
| ra2_soviets_attackdog | 5 | mods/cameo/rules/redalert2.yaml |
| ra2_soviets_barracks | 4 | mods/cameo/rules/redalert2.yaml |
| ra2_soviets_battlebunker | 5 | mods/cameo/rules/redalert2.yaml |
| ra2_soviets_battlelab | 4 | mods/cameo/rules/redalert2.yaml |
| ra2_soviets_boris | 5 | mods/cameo/rules/redalert2.yaml |
| ra2_soviets_conscript | 5 | mods/cameo/rules/redalert2.yaml |
| ra2_soviets_constructionyard | 5 | mods/cameo/rules/redalert2.yaml |
| ra2_soviets_crazyivan | 5 | mods/cameo/rules/redalert2.yaml |
| ra2_soviets_desolator | 5 | mods/cameo/rules/redalert2.yaml |
| ra2_soviets_engineer | 7 | mods/cameo/rules/redalert2.yaml |
| ra2_soviets_flakcannon | 5 | mods/cameo/rules/redalert2.yaml |
| ra2_soviets_flaktrack | 4 | mods/cameo/rules/redalert2.yaml |
| ra2_soviets_flaktrooper | 5 | mods/cameo/rules/redalert2.yaml |
| ra2_soviets_industrialplant | 5 | mods/cameo/rules/redalert2.yaml |
| ra2_soviets_ironcurtain | 4 | mods/cameo/rules/redalert2.yaml |
| ra2_soviets_kirovairship | 4 | mods/cameo/rules/redalert2.yaml |
| ra2_soviets_migbomber | 4 | mods/cameo/rules/redalert2.yaml |
| ra2_soviets_mobileconstructionvehicle | 4 | mods/cameo/rules/redalert2.yaml |
| ra2_soviets_nuclearmissilesilo | 5 | mods/cameo/rules/redalert2.yaml |
| ra2_soviets_nuclearreactor | 4 | mods/cameo/rules/redalert2.yaml |
| ra2_soviets_orerefinery | 4 | mods/cameo/rules/redalert2.yaml |
| ra2_soviets_radar | 4 | mods/cameo/rules/redalert2.yaml |
| ra2_soviets_rhinoheavytank | 5 | mods/cameo/rules/redalert2.yaml |
| ra2_soviets_seascorpion | 4 | mods/cameo/rules/redalert2.yaml |
| ra2_soviets_sentrygun | 5 | mods/cameo/rules/redalert2.yaml |
| ra2_soviets_servicedepot | 4 | mods/cameo/rules/redalert2.yaml |
| ra2_soviets_siegechopper | 4 | mods/cameo/rules/redalert2.yaml |
| ra2_soviets_terrordrone | 4 | mods/cameo/rules/redalert2.yaml |
| ra2_soviets_teslacoil | 5 | mods/cameo/rules/redalert2.yaml |
| ra2_soviets_teslareactor | 4 | mods/cameo/rules/redalert2.yaml |
| ra2_soviets_teslatank | 5 | mods/cameo/rules/redalert2.yaml |
| ra2_soviets_teslatrooper | 5 | mods/cameo/rules/redalert2.yaml |
| ra2_soviets_transportkirov | 4 | mods/cameo/rules/redalert2.yaml |
| ra2_soviets_v3rocketlauncher | 5 | mods/cameo/rules/redalert2.yaml |
| ra2_soviets_warfactory | 5 | mods/cameo/rules/redalert2.yaml |
| ra2_soviets_warminer | 4 | mods/cameo/rules/redalert2.yaml |
| ra2_stang | 4 | mods/cameo/rules/redalert2.yaml |
| ra2_stang_demo | 4 | mods/cameo/rules/redalert2.yaml |
| ra2_stang_driveby | 5 | mods/cameo/rules/redalert2.yaml |
| ra2_suvb | 4 | mods/cameo/rules/redalert2.yaml |
| ra2_suvb_demo | 4 | mods/cameo/rules/redalert2.yaml |
| ra2_suvb_driveby | 5 | mods/cameo/rules/redalert2.yaml |
| ra2_suvw | 4 | mods/cameo/rules/redalert2.yaml |
| ra2_suvw_demo | 4 | mods/cameo/rules/redalert2.yaml |
| ra2_suvw_driveby | 5 | mods/cameo/rules/redalert2.yaml |
| ra2_taxi | 4 | mods/cameo/rules/redalert2.yaml |
| ra2_taxi_demo | 4 | mods/cameo/rules/redalert2.yaml |
| ra2_taxi_driveby | 5 | mods/cameo/rules/redalert2.yaml |
| ra2_tractor | 4 | mods/cameo/rules/redalert2.yaml |
| ra2_tractor_demo | 4 | mods/cameo/rules/redalert2.yaml |
| ra2_tractor_driveby | 5 | mods/cameo/rules/redalert2.yaml |
| ra2_trucka | 4 | mods/cameo/rules/redalert2.yaml |
| ra2_trucka_demo | 4 | mods/cameo/rules/redalert2.yaml |
| ra2_trucka_driveby | 5 | mods/cameo/rules/redalert2.yaml |
| ra2_truckb | 4 | mods/cameo/rules/redalert2.yaml |
| ra2_truckb_demo | 4 | mods/cameo/rules/redalert2.yaml |
| ra2_truckb_driveby | 5 | mods/cameo/rules/redalert2.yaml |
| ra2_ycab | 4 | mods/cameo/rules/redalert2.yaml |
| ra2_ycab_demo | 4 | mods/cameo/rules/redalert2.yaml |
| ra2_ycab_driveby | 5 | mods/cameo/rules/redalert2.yaml |
| ra2asw | 4 | mods/cameo/rules/redalert2.yaml |
| ra2caairp.husk | 4 | mods/cameo/rules/redalert2.yaml |
| ra2caairpv | 4 | mods/cameo/rules/redalert2.yaml |
| ra2caairpv.husk | 4 | mods/cameo/rules/redalert2.yaml |
| ra2caoild.husk | 4 | mods/cameo/rules/redalert2.yaml |
| ra2carrier | 4 | mods/cameo/rules/redalert2.yaml |
| ra2cpower.husk | 4 | mods/cameo/rules/redalert2.yaml |
| ra2ctarmy01 | 5 | mods/cameo/rules/redalert2.yaml |
| ra2ctarmy02 | 5 | mods/cameo/rules/redalert2.yaml |
| ra2ctarmy03 | 6 | mods/cameo/rules/redalert2.yaml |
| ra2ctarmy04 | 6 | mods/cameo/rules/redalert2.yaml |
| ra2ctbarn02 | 5 | mods/cameo/rules/redalert2.yaml |
| ra2ctbunk01 | 4 | mods/cameo/rules/redalert2.yaml |
| ra2ctbunk02 | 5 | mods/cameo/rules/redalert2.yaml |
| ra2ctchig01 | 5 | mods/cameo/rules/redalert2.yaml |
| ra2ctchig02 | 6 | mods/cameo/rules/redalert2.yaml |
| ra2ctchig03 | 6 | mods/cameo/rules/redalert2.yaml |
| ra2cteur01 | 5 | mods/cameo/rules/redalert2.yaml |
| ra2cteur02 | 5 | mods/cameo/rules/redalert2.yaml |
| ra2cteur04 | 6 | mods/cameo/rules/redalert2.yaml |
| ra2ctfarm01 | 5 | mods/cameo/rules/redalert2.yaml |
| ra2ctfarm06 | 5 | mods/cameo/rules/redalert2.yaml |
| ra2ctfrma | 5 | mods/cameo/rules/redalert2.yaml |
| ra2ctgas01 | 6 | mods/cameo/rules/redalert2.yaml |
| ra2cthse01 | 5 | mods/cameo/rules/redalert2.yaml |
| ra2cthse02 | 5 | mods/cameo/rules/redalert2.yaml |
| ra2cthse03 | 6 | mods/cameo/rules/redalert2.yaml |
| ra2cthse04 | 5 | mods/cameo/rules/redalert2.yaml |
| ra2cthse05 | 5 | mods/cameo/rules/redalert2.yaml |
| ra2cthse06 | 6 | mods/cameo/rules/redalert2.yaml |
| ra2cthse07 | 5 | mods/cameo/rules/redalert2.yaml |
| ra2ctind01 | 6 | mods/cameo/rules/redalert2.yaml |
| ra2ctlab | 6 | mods/cameo/rules/redalert2.yaml |
| ra2ctmiam01 | 6 | mods/cameo/rules/redalert2.yaml |
| ra2ctmiam02 | 6 | mods/cameo/rules/redalert2.yaml |
| ra2ctmiam03 | 6 | mods/cameo/rules/redalert2.yaml |
| ra2ctmiam04 | 5 | mods/cameo/rules/redalert2.yaml |
| ra2ctmiam05 | 6 | mods/cameo/rules/redalert2.yaml |
| ra2ctmiam06 | 7 | mods/cameo/rules/redalert2.yaml |
| ra2ctmiam07 | 7 | mods/cameo/rules/redalert2.yaml |
| ra2ctmiam08 | 5 | mods/cameo/rules/redalert2.yaml |
| ra2ctmsc07 | 5 | mods/cameo/rules/redalert2.yaml |
| ra2ctmsc08 | 6 | mods/cameo/rules/redalert2.yaml |
| ra2ctmsc09 | 7 | mods/cameo/rules/redalert2.yaml |
| ra2ctmsc10 | 6 | mods/cameo/rules/redalert2.yaml |
| ra2ctnewy01 | 6 | mods/cameo/rules/redalert2.yaml |
| ra2ctnewy06 | 6 | mods/cameo/rules/redalert2.yaml |
| ra2ctnewy07 | 6 | mods/cameo/rules/redalert2.yaml |
| ra2ctnewy08 | 6 | mods/cameo/rules/redalert2.yaml |
| ra2ctnewy10 | 6 | mods/cameo/rules/redalert2.yaml |
| ra2ctnewy11 | 6 | mods/cameo/rules/redalert2.yaml |
| ra2ctnewy12 | 7 | mods/cameo/rules/redalert2.yaml |
| ra2ctnewy13 | 7 | mods/cameo/rules/redalert2.yaml |
| ra2ctnewy14 | 7 | mods/cameo/rules/redalert2.yaml |
| ra2ctnewy15 | 7 | mods/cameo/rules/redalert2.yaml |
| ra2ctnewy16 | 7 | mods/cameo/rules/redalert2.yaml |
| ra2ctnewy17 | 8 | mods/cameo/rules/redalert2.yaml |
| ra2ctnewy18 | 9 | mods/cameo/rules/redalert2.yaml |
| ra2ctnewy20 | 6 | mods/cameo/rules/redalert2.yaml |
| ra2ctnewy21 | 7 | mods/cameo/rules/redalert2.yaml |
| ra2ctnewy26 | 6 | mods/cameo/rules/redalert2.yaml |
| ra2dest | 4 | mods/cameo/rules/redalert2.yaml |
| ra2dlph | 4 | mods/cameo/rules/redalert2.yaml |
| ra2dred | 4 | mods/cameo/rules/redalert2.yaml |
| ra2e2.black | 6 | mods/cameo/rules/redalert2.yaml |
| ra2gayard | 4 | mods/cameo/rules/redalert2.yaml |
| ra2hind_husk.latin | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/aircraft.yaml |
| ra2hornet | 4 | mods/cameo/rules/redalert2.yaml |
| ra2hospt.husk | 4 | mods/cameo/rules/redalert2.yaml |
| ra2lcrf | 4 | mods/cameo/rules/redalert2.yaml |
| ra2leopard | 5 | mods/cameo/rules/redalert2.yaml |
| ra2machshop.husk | 4 | mods/cameo/rules/redalert2.yaml |
| ra2nayard | 4 | mods/cameo/rules/redalert2.yaml |
| ra2sapc | 4 | mods/cameo/rules/redalert2.yaml |
| ra2shk.bot | 6 | mods/cameo/rules/redalert2.yaml |
| ra2shkhero | 6 | mods/cameo/rules/redalert2.yaml |
| ra2sidewind | 5 | mods/cameo/rules/redalert2.yaml |
| ra2sqd | 4 | mods/cameo/rules/redalert2.yaml |
| ra2sub | 4 | mods/cameo/rules/redalert2.yaml |
| ra_cons_molo | 5 | mods/cameo/rules/redalert.yaml |
| ra_kamov.Husk | 4 | mods/cameo/rules/husks.yaml |
| rammax.latin | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/naval.yaml |
| refinery.atreides | 5 | mods/cameo/ContentPacks/D2k/Atreides/yaml/buildings.yaml |
| resonance_drone_husk.ixian | 4 | mods/cameo/ContentPacks/D2k/Ixian/yaml/aircraft.yaml |
| rocket_raider.ixian | 4 | mods/cameo/ContentPacks/D2k/Ixian/yaml/vehicles.yaml |
| sc_zerg_larva | 5 | mods/cameo/rules/starcraft.yaml |
| scadept.shade | 6 | mods/cameo/rules/starcraft.yaml |
| scalpelAA.steel | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/aircraft.yaml |
| scalpelMG.steel | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/aircraft.yaml |
| scalpelQuantumCannon.steel | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/aircraft.yaml |
| schwarzer_mond_airfield | 5 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/buildings.yaml |
| schwarzer_mond_barracks | 4 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/buildings.yaml |
| schwarzer_mond_bermensch | 5 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/infantry.yaml |
| schwarzer_mond_blackbomb | 4 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/aircraft.yaml |
| schwarzer_mond_constructionyard | 4 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/buildings.yaml |
| schwarzer_mond_corruptorpiercer | 4 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/aircraft.yaml |
| schwarzer_mond_crystaltank | 5 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/vehicles.yaml |
| schwarzer_mond_dalek | 4 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/vehicles.yaml |
| schwarzer_mond_dieglocke | 4 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/aircraft.yaml |
| schwarzer_mond_engineeringarmor | 4 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/infantry.yaml |
| schwarzer_mond_gravitycore | 5 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/buildings.yaml |
| schwarzer_mond_gravitycoretank | 4 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/vehicles.yaml |
| schwarzer_mond_haunebuii | 4 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/aircraft.yaml |
| schwarzer_mond_haunebuiii | 4 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/aircraft.yaml |
| schwarzer_mond_hydrogenplant | 4 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/buildings.yaml |
| schwarzer_mond_korruptesbiest | 5 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/vehicles.yaml |
| schwarzer_mond_laserbeetle | 4 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/vehicles.yaml |
| schwarzer_mond_lasertank | 4 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/vehicles.yaml |
| schwarzer_mond_lasertower | 5 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/buildings.yaml |
| schwarzer_mond_lunargrille | 5 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/vehicles.yaml |
| schwarzer_mond_lunarpanzer | 5 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/vehicles.yaml |
| schwarzer_mond_lunarrocket | 5 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/infantry.yaml |
| schwarzer_mond_lunarsoldier | 5 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/infantry.yaml |
| schwarzer_mond_lunartiger | 5 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/vehicles.yaml |
| schwarzer_mond_m200bjagerline | 4 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/vehicles.yaml |
| schwarzer_mond_mars | 5 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/vehicles.yaml |
| schwarzer_mond_meteortractionray | 5 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/buildings.yaml |
| schwarzer_mond_moondairyfarm | 4 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/buildings.yaml |
| schwarzer_mond_naxismobileconstructionvehicle | 4 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/vehicles.yaml |
| schwarzer_mond_neojagdpanzer | 5 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/vehicles.yaml |
| schwarzer_mond_noidharvester | 4 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/infantry.yaml |
| schwarzer_mond_noidmgarmor | 4 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/infantry.yaml |
| schwarzer_mond_orerefinery | 4 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/buildings.yaml |
| schwarzer_mond_parzival | 5 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/infantry.yaml |
| schwarzer_mond_radar | 4 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/buildings.yaml |
| schwarzer_mond_spacezeppelin | 4 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/aircraft.yaml |
| schwarzer_mond_sturmcannon | 5 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/buildings.yaml |
| schwarzer_mond_techcenter | 4 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/buildings.yaml |
| schwarzer_mond_warfactory | 5 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/buildings.yaml |
| scrapcar.latin | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/vehicles.yaml |
| scrapcar2.latin | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/vehicles.yaml |
| scrapcar2_demo.latin | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/vehicles.yaml |
| scrapcar2_driveby.latin | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/vehicles.yaml |
| scrapcar_demo.latin | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/vehicles.yaml |
| scrapcar_driveby.latin | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/vehicles.yaml |
| siege_tank | 5 | mods/cameo/ContentPacks/D2k/Shared/yaml/vehicles.yaml |
| siege_tank.husk | 4 | mods/cameo/ContentPacks/D2k/Shared/yaml/vehicles.yaml |
| sietch_creep | 4 | mods/cameo/ContentPacks/D2k/Shared/yaml/buildings.yaml |
| sietch_creep_disabled | 5 | mods/cameo/ContentPacks/D2k/Shared/yaml/buildings.yaml |
| sonic_tank_husk.atreides | 4 | mods/cameo/ContentPacks/D2k/Atreides/yaml/vehicles.yaml |
| ssmsub | 4 | mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/naval.yaml |
| steel_consortium_antiairquantummissileturret | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/buildings.yaml |
| steel_consortium_barracuda | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/vehicles.yaml |
| steel_consortium_bfg10000 | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/buildings.yaml |
| steel_consortium_cargoship | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/aircraft.yaml |
| steel_consortium_clonetrooper | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/infantry.yaml |
| steel_consortium_cloningvats | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/buildings.yaml |
| steel_consortium_cloudbreaker | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/aircraft.yaml |
| steel_consortium_consortiumairpad | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/buildings.yaml |
| steel_consortium_consortiumbattlelab | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/buildings.yaml |
| steel_consortium_consortiumconstructionyard | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/buildings.yaml |
| steel_consortium_consortiumminer | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/vehicles.yaml |
| steel_consortium_consortiummobileconstructionvehicle | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/vehicles.yaml |
| steel_consortium_consortiumpowerplant | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/buildings.yaml |
| steel_consortium_consortiumradar | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/buildings.yaml |
| steel_consortium_consortiumrefinery | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/buildings.yaml |
| steel_consortium_consortiumsentryturret | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/buildings.yaml |
| steel_consortium_consortiumwarfactory | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/buildings.yaml |
| steel_consortium_dagger | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/vehicles.yaml |
| steel_consortium_defenderbot | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/vehicles.yaml |
| steel_consortium_empressstation | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/aircraft.yaml |
| steel_consortium_engineer | 7 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/infantry.yaml |
| steel_consortium_geothermalreactor | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/buildings.yaml |
| steel_consortium_hammerheadartillerytank | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/vehicles.yaml |
| steel_consortium_hoverboardgrenadier | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/infantry.yaml |
| steel_consortium_katytank | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/vehicles.yaml |
| steel_consortium_mako | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/vehicles.yaml |
| steel_consortium_manta | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/vehicles.yaml |
| steel_consortium_megalodon | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/vehicles.yaml |
| steel_consortium_orbitalcannonactivator | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/buildings.yaml |
| steel_consortium_poseidontank | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/vehicles.yaml |
| steel_consortium_quantumcannon | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/buildings.yaml |
| steel_consortium_quantummissiletrooper | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/infantry.yaml |
| steel_consortium_quantumtank | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/vehicles.yaml |
| steel_consortium_skyhammer | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/aircraft.yaml |
| steel_consortium_stalker | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/vehicles.yaml |
| steel_consortium_steelbarracks | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/buildings.yaml |
| steel_consortium_steelrunner | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/infantry.yaml |
| steel_consortium_supportshieldgenerator | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/vehicles.yaml |
| steel_consortium_twister | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/aircraft.yaml |
| steel_consortium_whiterabbit | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/vehicles.yaml |
| sub.latin | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/naval.yaml |
| td_gdi_advancedcommunicationscenter | 5 | mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/buildings.yaml |
| td_gdi_advancedguardtower | 5 | mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/buildings.yaml |
| td_gdi_apc | 4 | mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/vehicles.yaml |
| td_gdi_archerartillery | 5 | mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/vehicles.yaml |
| td_gdi_assaultapc | 5 | mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/vehicles.yaml |
| td_gdi_barracks | 4 | mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/buildings.yaml |
| td_gdi_battletank | 5 | mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/vehicles.yaml |
| td_gdi_boxer | 4 | mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/vehicles.yaml |
| td_gdi_chinooktransport | 6 | mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/aircraft.yaml |
| td_gdi_commando | 6 | mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/infantry.yaml |
| td_gdi_communicationscenter | 4 | mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/buildings.yaml |
| td_gdi_constructionyard | 5 | mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/buildings.yaml |
| td_gdi_defenserig | 4 | mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/vehicles.yaml |
| td_gdi_empgrenadier | 5 | mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/infantry.yaml |
| td_gdi_exosuit | 5 | mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/vehicles.yaml |
| td_gdi_firehawk | 4 | mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/aircraft.yaml |
| td_gdi_grenadier | 5 | mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/infantry.yaml |
| td_gdi_guardtower | 5 | mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/buildings.yaml |
| td_gdi_havoc | 5 | mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/infantry.yaml |
| td_gdi_heavysniper | 5 | mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/infantry.yaml |
| td_gdi_helipad | 4 | mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/buildings.yaml |
| td_gdi_humvee | 4 | mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/vehicles.yaml |
| td_gdi_humveemkii | 5 | mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/vehicles.yaml |
| td_gdi_mammothtank | 5 | mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/vehicles.yaml |
| td_gdi_mammothtankmkiii | 5 | mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/vehicles.yaml |
| td_gdi_minigunner | 6 | mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/infantry.yaml |
| td_gdi_mlrs | 5 | mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/vehicles.yaml |
| td_gdi_mobileconstructionvehicle | 5 | mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/vehicles.yaml |
| td_gdi_officer | 5 | mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/infantry.yaml |
| td_gdi_orca | 4 | mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/aircraft.yaml |
| td_gdi_predatortank | 5 | mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/vehicles.yaml |
| td_gdi_repairfacility | 4 | mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/buildings.yaml |
| td_gdi_rocketsoldier | 6 | mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/infantry.yaml |
| td_gdi_shotgunner | 5 | mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/infantry.yaml |
| td_gdi_skyshield | 4 | mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/buildings.yaml |
| td_gdi_sonicmissilesoldier | 5 | mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/infantry.yaml |
| td_gdi_tiberiumharvester | 5 | mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/vehicles.yaml |
| td_gdi_tiberiumrefinery | 5 | mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/buildings.yaml |
| td_gdi_weaponsfactory | 4 | mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/buildings.yaml |
| td_nod_airstrip | 4 | mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/buildings.yaml |
| td_nod_apacheattackhelicopter | 4 | mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/aircraft.yaml |
| td_nod_artillery | 5 | mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/vehicles.yaml |
| td_nod_blackhandflamer | 5 | mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/infantry.yaml |
| td_nod_buggy | 4 | mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/vehicles.yaml |
| td_nod_buggymkii | 5 | mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/vehicles.yaml |
| td_nod_chemicalattackbike | 4 | mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/vehicles.yaml |
| td_nod_chemicalrocketsoldier | 5 | mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/infantry.yaml |
| td_nod_chemicalssmlauncher | 5 | mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/vehicles.yaml |
| td_nod_chemicalstealthtank | 5 | mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/vehicles.yaml |
| td_nod_chemicalwarrior | 5 | mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/infantry.yaml |
| td_nod_chinooktransport | 6 | mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/aircraft.yaml |
| td_nod_commando | 6 | mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/infantry.yaml |
| td_nod_communicationscenter | 4 | mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/buildings.yaml |
| td_nod_constructionyard | 5 | mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/buildings.yaml |
| td_nod_flametank | 5 | mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/vehicles.yaml |
| td_nod_flametankmkii | 5 | mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/vehicles.yaml |
| td_nod_flamethrower | 5 | mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/infantry.yaml |
| td_nod_gunturret | 5 | mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/buildings.yaml |
| td_nod_handofnod | 4 | mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/buildings.yaml |
| td_nod_helipad | 4 | mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/buildings.yaml |
| td_nod_lasercommando | 5 | mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/infantry.yaml |
| td_nod_lasertrooper | 5 | mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/infantry.yaml |
| td_nod_laserturret | 5 | mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/buildings.yaml |
| td_nod_lighttank | 5 | mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/vehicles.yaml |
| td_nod_lighttankmkii | 5 | mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/vehicles.yaml |
| td_nod_minigunner | 6 | mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/infantry.yaml |
| td_nod_mobileconstructionvehicle | 5 | mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/vehicles.yaml |
| td_nod_obeliskoflight | 5 | mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/buildings.yaml |
| td_nod_reconbike | 4 | mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/vehicles.yaml |
| td_nod_repairfacility | 4 | mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/buildings.yaml |
| td_nod_rocketsoldier | 6 | mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/infantry.yaml |
| td_nod_samsite | 5 | mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/buildings.yaml |
| td_nod_specterartillery | 5 | mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/vehicles.yaml |
| td_nod_ssmlauncher | 5 | mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/vehicles.yaml |
| td_nod_stealthharvester | 5 | mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/vehicles.yaml |
| td_nod_stealthsoldier | 5 | mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/infantry.yaml |
| td_nod_stealthtank | 5 | mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/vehicles.yaml |
| td_nod_templeofnod | 5 | mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/buildings.yaml |
| td_nod_templeprime | 5 | mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/buildings.yaml |
| td_nod_tiberiumharvester | 5 | mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/vehicles.yaml |
| td_nod_tiberiumrefinery | 5 | mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/buildings.yaml |
| td_nod_venom | 4 | mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/aircraft.yaml |
| terran_battlecruiser | 4 | mods/cameo/rules/starcraft.yaml |
| terran_bunker | 4 | mods/cameo/rules/starcraft.yaml |
| terran_commandcenter | 4 | mods/cameo/rules/starcraft.yaml |
| terran_cyclone | 5 | mods/cameo/rules/starcraft.yaml |
| terran_dropship | 5 | mods/cameo/rules/starcraft.yaml |
| terran_firebat | 5 | mods/cameo/rules/starcraft.yaml |
| terran_ghost | 5 | mods/cameo/rules/starcraft.yaml |
| terran_goliath | 5 | mods/cameo/rules/starcraft.yaml |
| terran_goliathmk2 | 5 | mods/cameo/rules/starcraft.yaml |
| terran_harakan | 5 | mods/cameo/rules/starcraft.yaml |
| terran_jimraynor | 5 | mods/cameo/rules/starcraft.yaml |
| terran_madcap | 5 | mods/cameo/rules/starcraft.yaml |
| terran_marauder | 5 | mods/cameo/rules/starcraft.yaml |
| terran_marine | 5 | mods/cameo/rules/starcraft.yaml |
| terran_matador | 5 | mods/cameo/rules/starcraft.yaml |
| terran_medic | 5 | mods/cameo/rules/starcraft.yaml |
| terran_medivac | 5 | mods/cameo/rules/starcraft.yaml |
| terran_missilesilo | 5 | mods/cameo/rules/starcraft.yaml |
| terran_missileturret | 4 | mods/cameo/rules/starcraft.yaml |
| terran_mobilecommandcenter | 4 | mods/cameo/rules/starcraft.yaml |
| terran_phobos | 4 | mods/cameo/rules/starcraft.yaml |
| terran_pythean | 4 | mods/cameo/rules/starcraft.yaml |
| terran_raven | 4 | mods/cameo/rules/starcraft.yaml |
| terran_reaper | 5 | mods/cameo/rules/starcraft.yaml |
| terran_sciencevessel | 4 | mods/cameo/rules/starcraft.yaml |
| terran_scv | 4 | mods/cameo/rules/starcraft.yaml |
| terran_sentinel | 4 | mods/cameo/rules/starcraft.yaml |
| terran_siegetank | 5 | mods/cameo/rules/starcraft.yaml |
| terran_specter | 5 | mods/cameo/rules/starcraft.yaml |
| terran_sundog | 4 | mods/cameo/rules/starcraft.yaml |
| terran_supplydepot | 4 | mods/cameo/rules/starcraft.yaml |
| terran_valkyrie | 4 | mods/cameo/rules/starcraft.yaml |
| terran_vulture | 4 | mods/cameo/rules/starcraft.yaml |
| terran_warhound | 5 | mods/cameo/rules/starcraft.yaml |
| terran_wraith | 4 | mods/cameo/rules/starcraft.yaml |
| terran_wyvern | 4 | mods/cameo/rules/starcraft.yaml |
| tiger.nax | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/vehicles.yaml |
| tkm_abrams | 5 | mods/cameo/rules/tkm.yaml |
| tkm_as42 | 4 | mods/cameo/rules/tkm.yaml |
| tkm_barracks | 4 | mods/cameo/rules/tkm.yaml |
| tkm_battlebus | 5 | mods/cameo/rules/tkm.yaml |
| tkm_bigshiee | 4 | mods/cameo/rules/tkm.yaml |
| tkm_bunker | 4 | mods/cameo/rules/tkm.yaml |
| tkm_dronepodtruck | 5 | mods/cameo/rules/tkm.yaml |
| tkm_engineer | 6 | mods/cameo/rules/tkm.yaml |
| tkm_flakbus | 4 | mods/cameo/rules/tkm.yaml |
| tkm_iroquois | 4 | mods/cameo/rules/tkm.yaml |
| tkm_juggernaut | 5 | mods/cameo/rules/tkm.yaml |
| tkm_marine | 5 | mods/cameo/rules/tkm.yaml |
| tkm_medictruck | 4 | mods/cameo/rules/tkm.yaml |
| tkm_mobileconstructionvehicletkm | 4 | mods/cameo/rules/tkm.yaml |
| tkm_observationvan | 4 | mods/cameo/rules/tkm.yaml |
| tkm_orerefinery | 4 | mods/cameo/rules/tkm.yaml |
| tkm_powerplant | 4 | mods/cameo/rules/tkm.yaml |
| tkm_quadtruck | 4 | mods/cameo/rules/tkm.yaml |
| tkm_quadturretbunker | 4 | mods/cameo/rules/tkm.yaml |
| tkm_radartruck | 4 | mods/cameo/rules/tkm.yaml |
| tkm_repairtruck | 4 | mods/cameo/rules/tkm.yaml |
| tkm_rifleman | 5 | mods/cameo/rules/tkm.yaml |
| tkm_rocketeer | 5 | mods/cameo/rules/tkm.yaml |
| tkm_sandmarine | 4 | mods/cameo/rules/tkm.yaml |
| tkm_sniper | 5 | mods/cameo/rules/tkm.yaml |
| tkm_spetsnaz | 5 | mods/cameo/rules/tkm.yaml |
| tkm_stryker | 5 | mods/cameo/rules/tkm.yaml |
| tkm_t30 | 4 | mods/cameo/rules/tkm.yaml |
| tkm_t72m | 5 | mods/cameo/rules/tkm.yaml |
| tkm_tankturretbunker | 4 | mods/cameo/rules/tkm.yaml |
| tkm_technical | 4 | mods/cameo/rules/tkm.yaml |
| tkm_technicaltank | 5 | mods/cameo/rules/tkm.yaml |
| tkm_templateharvesterraname | 4 | mods/cameo/rules/tkm.yaml |
| tkm_thermonaut | 5 | mods/cameo/rules/tkm.yaml |
| tkm_tornadoglauncher | 5 | mods/cameo/rules/tkm.yaml |
| tkm_trenchtank | 5 | mods/cameo/rules/tkm.yaml |
| tkm_trenchtruck | 4 | mods/cameo/rules/tkm.yaml |
| tkm_trooper | 5 | mods/cameo/rules/tkm.yaml |
| tkm_viper | 4 | mods/cameo/rules/tkm.yaml |
| tkm_von | 5 | mods/cameo/rules/tkm.yaml |
| tkm_zaza | 4 | mods/cameo/rules/tkm.yaml |
| tkmabramspoint | 6 | mods/cameo/rules/tkm.yaml |
| tkmdrone | 4 | mods/cameo/rules/tkm.yaml |
| tkmhuey.husk | 4 | mods/cameo/rules/tkm.yaml |
| tkmratflakdeployed | 4 | mods/cameo/rules/tkm.yaml |
| tkmsuicidedrone | 4 | mods/cameo/rules/tkm.yaml |
| tkmtrenchtankdeployed | 4 | mods/cameo/rules/tkm.yaml |
| tkmvan | 5 | mods/cameo/rules/tkm.yaml |
| tkmworker | 6 | mods/cameo/rules/tkm.yaml |
| triton.latin | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/naval.yaml |
| trooper | 5 | mods/cameo/ContentPacks/D2k/Shared/yaml/infantry.yaml |
| ts_gdi_amphibiousapc | 4 | mods/cameo/ContentPacks/TiberianSun/GDI/yaml/vehicles.yaml |
| ts_gdi_carryall | 4 | mods/cameo/ContentPacks/TiberianSun/GDI/yaml/aircraft.yaml |
| ts_gdi_discthrower | 5 | mods/cameo/ContentPacks/TiberianSun/GDI/yaml/infantry.yaml |
| ts_gdi_disruptor | 5 | mods/cameo/ContentPacks/TiberianSun/GDI/yaml/vehicles.yaml |
| ts_gdi_empulsecannon | 4 | mods/cameo/ContentPacks/TiberianSun/GDI/yaml/defenses.yaml |
| ts_gdi_engineer | 7 | mods/cameo/ContentPacks/TiberianSun/GDI/yaml/infantry.yaml |
| ts_gdi_falconenforcer | 5 | mods/cameo/ContentPacks/TiberianSun/GDI/yaml/infantry.yaml |
| ts_gdi_hammerhead | 4 | mods/cameo/ContentPacks/TiberianSun/GDI/yaml/aircraft.yaml |
| ts_gdi_hovermlrs | 5 | mods/cameo/ContentPacks/TiberianSun/GDI/yaml/vehicles.yaml |
| ts_gdi_juggernaut | 5 | mods/cameo/ContentPacks/TiberianSun/GDI/yaml/vehicles.yaml |
| ts_gdi_juggernautmkii | 5 | mods/cameo/ContentPacks/TiberianSun/GDI/yaml/vehicles.yaml |
| ts_gdi_jumpjetinfantry | 4 | mods/cameo/ContentPacks/TiberianSun/GDI/yaml/infantry.yaml |
| ts_gdi_kodiakcommandship | 4 | mods/cameo/ContentPacks/TiberianSun/GDI/yaml/aircraft.yaml |
| ts_gdi_lightinfantry | 6 | mods/cameo/ContentPacks/TiberianSun/GDI/yaml/infantry.yaml |
| ts_gdi_mammothmkii | 4 | mods/cameo/ContentPacks/TiberianSun/GDI/yaml/vehicles.yaml |
| ts_gdi_mammothprototype | 4 | mods/cameo/ContentPacks/TiberianSun/GDI/yaml/vehicles.yaml |
| ts_gdi_medic | 6 | mods/cameo/ContentPacks/TiberianSun/GDI/yaml/infantry.yaml |
| ts_gdi_mobileconstructionvehicle | 4 | mods/cameo/ContentPacks/TiberianSun/GDI/yaml/vehicles.yaml |
| ts_gdi_mobileemp | 5 | mods/cameo/ContentPacks/TiberianSun/GDI/yaml/vehicles.yaml |
| ts_gdi_mobilesensorarray | 4 | mods/cameo/ContentPacks/TiberianSun/GDI/yaml/vehicles.yaml |
| ts_gdi_orcabomber | 4 | mods/cameo/ContentPacks/TiberianSun/GDI/yaml/aircraft.yaml |
| ts_gdi_orcafighter | 4 | mods/cameo/ContentPacks/TiberianSun/GDI/yaml/aircraft.yaml |
| ts_gdi_pitbull | 4 | mods/cameo/ContentPacks/TiberianSun/GDI/yaml/vehicles.yaml |
| ts_gdi_powerplant | 4 | mods/cameo/ContentPacks/TiberianSun/GDI/yaml/buildings.yaml |
| ts_gdi_radar | 4 | mods/cameo/ContentPacks/TiberianSun/GDI/yaml/buildings.yaml |
| ts_gdi_railguncommando | 5 | mods/cameo/ContentPacks/TiberianSun/GDI/yaml/infantry.yaml |
| ts_gdi_riottrooper | 5 | mods/cameo/ContentPacks/TiberianSun/GDI/yaml/infantry.yaml |
| ts_gdi_rpgtower | 4 | mods/cameo/ContentPacks/TiberianSun/GDI/yaml/defenses.yaml |
| ts_gdi_samtower | 4 | mods/cameo/ContentPacks/TiberianSun/GDI/yaml/defenses.yaml |
| ts_gdi_tiberiumharvester | 5 | mods/cameo/ContentPacks/TiberianSun/GDI/yaml/vehicles.yaml |
| ts_gdi_titan | 5 | mods/cameo/ContentPacks/TiberianSun/GDI/yaml/vehicles.yaml |
| ts_gdi_titanmkii | 5 | mods/cameo/ContentPacks/TiberianSun/GDI/yaml/vehicles.yaml |
| ts_gdi_upgradecenter | 5 | mods/cameo/ContentPacks/TiberianSun/GDI/yaml/defenses.yaml |
| ts_gdi_vulcantower | 4 | mods/cameo/ContentPacks/TiberianSun/GDI/yaml/defenses.yaml |
| ts_gdi_wolverine | 5 | mods/cameo/ContentPacks/TiberianSun/GDI/yaml/vehicles.yaml |
| ts_gdi_wolverinemkii | 5 | mods/cameo/ContentPacks/TiberianSun/GDI/yaml/vehicles.yaml |
| ts_gdi_zoneorcafighter | 4 | mods/cameo/ContentPacks/TiberianSun/GDI/yaml/aircraft.yaml |
| ts_gdi_zonetrooper | 5 | mods/cameo/ContentPacks/TiberianSun/GDI/yaml/infantry.yaml |
| ts_nod_advancedpowerplant | 4 | mods/cameo/ContentPacks/TiberianSun/Nod/yaml/buildings.yaml |
| ts_nod_artillery | 5 | mods/cameo/ContentPacks/TiberianSun/Nod/yaml/vehicles.yaml |
| ts_nod_attackbuggy | 4 | mods/cameo/ContentPacks/TiberianSun/Nod/yaml/vehicles.yaml |
| ts_nod_attackcycle | 4 | mods/cameo/rules/tiberiansun.yaml |
| ts_nod_bansheefighter | 4 | mods/cameo/ContentPacks/TiberianSun/Nod/yaml/aircraft.yaml |
| ts_nod_chameleonspy | 5 | mods/cameo/ContentPacks/TiberianSun/Nod/yaml/infantry.yaml |
| ts_nod_devilstongue | 5 | mods/cameo/ContentPacks/TiberianSun/Nod/yaml/vehicles.yaml |
| ts_nod_elitecadre | 5 | mods/cameo/ContentPacks/TiberianSun/Nod/yaml/infantry.yaml |
| ts_nod_engineer | 7 | mods/cameo/ContentPacks/TiberianSun/Nod/yaml/infantry.yaml |
| ts_nod_harpy | 4 | mods/cameo/ContentPacks/TiberianSun/Nod/yaml/aircraft.yaml |
| ts_nod_laserturret | 4 | mods/cameo/ContentPacks/TiberianSun/Nod/yaml/defenses.yaml |
| ts_nod_lightinfantry | 6 | mods/cameo/ContentPacks/TiberianSun/Nod/yaml/infantry.yaml |
| ts_nod_missilesilo | 5 | mods/cameo/ContentPacks/TiberianSun/Nod/yaml/defenses.yaml |
| ts_nod_mobileconstructionvehicle | 5 | mods/cameo/ContentPacks/TiberianSun/Nod/yaml/vehicles.yaml |
| ts_nod_mobilerepairvehicle | 4 | mods/cameo/ContentPacks/TiberianSun/Nod/yaml/vehicles.yaml |
| ts_nod_mobilestealthgenerator | 4 | mods/cameo/ContentPacks/TiberianSun/Nod/yaml/vehicles.yaml |
| ts_nod_obeliskoflight | 4 | mods/cameo/ContentPacks/TiberianSun/Nod/yaml/defenses.yaml |
| ts_nod_powerplant | 4 | mods/cameo/ContentPacks/TiberianSun/Nod/yaml/buildings.yaml |
| ts_nod_radar | 4 | mods/cameo/ContentPacks/TiberianSun/Nod/yaml/buildings.yaml |
| ts_nod_rocketinfantry | 6 | mods/cameo/ContentPacks/TiberianSun/Nod/yaml/infantry.yaml |
| ts_nod_samsite | 4 | mods/cameo/ContentPacks/TiberianSun/Nod/yaml/defenses.yaml |
| ts_nod_servicedepot | 4 | mods/cameo/ContentPacks/TiberianSun/Nod/yaml/buildings.yaml |
| ts_nod_shadowteam | 5 | mods/cameo/ContentPacks/TiberianSun/Nod/yaml/infantry.yaml |
| ts_nod_shadowteam_air | 4 | mods/cameo/ContentPacks/TiberianSun/Nod/yaml/infantry.yaml |
| ts_nod_shotguncommando | 5 | mods/cameo/ContentPacks/TiberianSun/Nod/yaml/infantry.yaml |
| ts_nod_silo | 4 | mods/cameo/ContentPacks/TiberianSun/Nod/yaml/defenses.yaml |
| ts_nod_stealthtank | 5 | mods/cameo/ContentPacks/TiberianSun/Nod/yaml/vehicles.yaml |
| ts_nod_subterraneanapc | 4 | mods/cameo/ContentPacks/TiberianSun/Nod/yaml/vehicles.yaml |
| ts_nod_tiberiumharvester | 5 | mods/cameo/ContentPacks/TiberianSun/Nod/yaml/vehicles.yaml |
| ts_nod_tiberiumrefinery | 4 | mods/cameo/ContentPacks/TiberianSun/Nod/yaml/buildings.yaml |
| ts_nod_ticktank | 5 | mods/cameo/rules/tiberiansun.yaml |
| ts_nod_toxintrooper | 5 | mods/cameo/ContentPacks/TiberianSun/Nod/yaml/infantry.yaml |
| tsaegis | 4 | mods/cameo/rules/tiberiansun.yaml |
| tsfloater | 4 | mods/cameo/rules/tiberiansun.yaml |
| tsfsmoker.bomber | 4 | mods/cameo/rules/tiberiansun.yaml |
| tsmonstermaker1 | 5 | mods/cameo/rules/tiberiansun.yaml |
| tsprobe | 4 | mods/cameo/rules/tiberiansun.yaml |
| tsun.asian | 4 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/naval.yaml |
| tsvislrg | 4 | mods/cameo/rules/tiberiansun.yaml |
| tuboat.nax | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/naval.yaml |
| undead.nax | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/infantry.yaml |
| wc2_critter_boar | 7 | mods/cameo/rules/warcraft2.yaml |
| wc2_critter_helboar | 7 | mods/cameo/rules/warcraft2.yaml |
| wc2_critter_seal | 7 | mods/cameo/rules/warcraft2.yaml |
| wc2_critter_sheep | 7 | mods/cameo/rules/warcraft2.yaml |
| wc2_human_battleship | 5 | mods/cameo/rules/warcraft2.yaml |
| wc2_human_elven_destroyer | 5 | mods/cameo/rules/warcraft2.yaml |
| wc2_human_foundry | 4 | mods/cameo/rules/warcraft2.yaml |
| wc2_human_gnomish_submarine | 5 | mods/cameo/rules/warcraft2.yaml |
| wc2_human_oil_platform | 6 | mods/cameo/rules/warcraft2.yaml |
| wc2_human_oil_refinery | 4 | mods/cameo/rules/warcraft2.yaml |
| wc2_human_oil_tanker | 5 | mods/cameo/rules/warcraft2.yaml |
| wc2_human_shipyard | 4 | mods/cameo/rules/warcraft2.yaml |
| wc2_human_transport | 5 | mods/cameo/rules/warcraft2.yaml |
| wc2_neutral_daemon | 5 | mods/cameo/rules/warcraft2.yaml |
| wc2_orc_eye_of_kilrogg | 5 | mods/cameo/rules/warcraft2.yaml |
| wc2_orc_foundry | 4 | mods/cameo/rules/warcraft2.yaml |
| wc2_orc_giant_turtle | 5 | mods/cameo/rules/warcraft2.yaml |
| wc2_orc_ogre_juggernaught | 5 | mods/cameo/rules/warcraft2.yaml |
| wc2_orc_oil_platform | 6 | mods/cameo/rules/warcraft2.yaml |
| wc2_orc_oil_refinery | 4 | mods/cameo/rules/warcraft2.yaml |
| wc2_orc_oil_tanker | 5 | mods/cameo/rules/warcraft2.yaml |
| wc2_orc_shipyard | 4 | mods/cameo/rules/warcraft2.yaml |
| wc2_orc_skeleton | 7 | mods/cameo/rules/warcraft2.yaml |
| wc2_orc_transport | 5 | mods/cameo/rules/warcraft2.yaml |
| wc2_orc_trolldestroyer | 5 | mods/cameo/rules/warcraft2.yaml |
| wind_trap.atreides | 5 | mods/cameo/ContentPacks/D2k/Atreides/yaml/buildings.yaml |
| wirbelwind.nax | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/vehicles.yaml |
| wraith_husk.ordos | 4 | mods/cameo/ContentPacks/D2k/Ordos/yaml/aircraft.yaml |
| yakarmored.Husk | 4 | mods/cameo/rules/husks.yaml |
| yaktesla.Husk | 4 | mods/cameo/rules/husks.yaml |
| yamatobattleship | 4 | mods/cameo/rules/redalert.yaml |
| yrbpln | 4 | mods/cameo/rules/redalert2.yaml |
| yrhovr | 4 | mods/cameo/rules/redalert2.yaml |
| yrlunr.husk | 4 | mods/cameo/rules/redalert2.yaml |
| yrrobo | 5 | mods/cameo/rules/redalert2.yaml |
| yrsmin.empy | 5 | mods/cameo/rules/redalert2.yaml |
| yrygyard | 4 | mods/cameo/rules/redalert2.yaml |
| yuri_barracks | 4 | mods/cameo/rules/redalert2.yaml |
| yuri_battlelab | 4 | mods/cameo/rules/redalert2.yaml |
| yuri_bioreactor | 4 | mods/cameo/rules/redalert2.yaml |
| yuri_biotrooper | 5 | mods/cameo/rules/redalert2.yaml |
| yuri_boomersubmarine | 4 | mods/cameo/rules/redalert2.yaml |
| yuri_brute | 5 | mods/cameo/rules/redalert2.yaml |
| yuri_chaosdrone | 4 | mods/cameo/rules/redalert2.yaml |
| yuri_clone | 5 | mods/cameo/rules/redalert2.yaml |
| yuri_cloningvats | 5 | mods/cameo/rules/redalert2.yaml |
| yuri_constructionyard | 5 | mods/cameo/rules/redalert2.yaml |
| yuri_cosmonaut | 4 | mods/cameo/rules/redalert2.yaml |
| yuri_engineer | 7 | mods/cameo/rules/redalert2.yaml |
| yuri_floatingdisk | 4 | mods/cameo/rules/redalert2.yaml |
| yuri_gatlingcannon | 5 | mods/cameo/rules/redalert2.yaml |
| yuri_gatlingtank | 4 | mods/cameo/rules/redalert2.yaml |
| yuri_gatlingtrooper | 5 | mods/cameo/rules/redalert2.yaml |
| yuri_geneticmutator | 4 | mods/cameo/rules/redalert2.yaml |
| yuri_grinder | 4 | mods/cameo/rules/redalert2.yaml |
| yuri_initiate | 5 | mods/cameo/rules/redalert2.yaml |
| yuri_lashertank | 5 | mods/cameo/rules/redalert2.yaml |
| yuri_lunarcommandcenter | 5 | mods/cameo/rules/redalert2.yaml |
| yuri_magnetron | 5 | mods/cameo/rules/redalert2.yaml |
| yuri_mastermind | 5 | mods/cameo/rules/redalert2.yaml |
| yuri_mobileconstructionvehicle | 4 | mods/cameo/rules/redalert2.yaml |
| yuri_psychicdominator | 5 | mods/cameo/rules/redalert2.yaml |
| yuri_psychicsensor | 4 | mods/cameo/rules/redalert2.yaml |
| yuri_psychictower | 5 | mods/cameo/rules/redalert2.yaml |
| yuri_slaveminer | 4 | mods/cameo/rules/redalert2.yaml |
| yuri_slaveminer_2 | 5 | mods/cameo/rules/redalert2.yaml |
| yuri_tankbunker | 5 | mods/cameo/rules/redalert2.yaml |
| yuri_virus | 5 | mods/cameo/rules/redalert2.yaml |
| yuri_warfactory | 5 | mods/cameo/rules/redalert2.yaml |
| yuri_yurix | 5 | mods/cameo/rules/redalert2.yaml |
| yuriinvisibleplane | 4 | mods/cameo/rules/redalert2.yaml |
| zerg_behemoth | 5 | mods/cameo/rules/starcraft.yaml |
| zerg_broodweaver | 5 | mods/cameo/rules/starcraft.yaml |
| zerg_corruptor | 5 | mods/cameo/rules/starcraft.yaml |
| zerg_creepcolony | 4 | mods/cameo/rules/starcraft.yaml |
| zerg_creepcolony_2 | 5 | mods/cameo/rules/starcraft.yaml |
| zerg_defiler | 6 | mods/cameo/rules/starcraft.yaml |
| zerg_defilermound | 4 | mods/cameo/rules/starcraft.yaml |
| zerg_devourer | 5 | mods/cameo/rules/starcraft.yaml |
| zerg_dreadshroud | 5 | mods/cameo/rules/starcraft.yaml |
| zerg_drone | 5 | mods/cameo/rules/starcraft.yaml |
| zerg_evolutionchamber | 4 | mods/cameo/rules/starcraft.yaml |
| zerg_extractor | 4 | mods/cameo/rules/starcraft.yaml |
| zerg_gorekraken | 5 | mods/cameo/rules/starcraft.yaml |
| zerg_goremaw | 7 | mods/cameo/rules/starcraft.yaml |
| zerg_guardian | 5 | mods/cameo/rules/starcraft.yaml |
| zerg_hatchery | 4 | mods/cameo/rules/starcraft.yaml |
| zerg_hatcherydrone | 4 | mods/cameo/rules/starcraft.yaml |
| zerg_hermit | 7 | mods/cameo/rules/starcraft.yaml |
| zerg_hydralisk | 6 | mods/cameo/rules/starcraft.yaml |
| zerg_hydraliskden | 4 | mods/cameo/rules/starcraft.yaml |
| zerg_infestedcommandcenter | 4 | mods/cameo/rules/starcraft.yaml |
| zerg_infestedterranbomber | 5 | mods/cameo/rules/starcraft.yaml |
| zerg_kerrigan | 6 | mods/cameo/rules/starcraft.yaml |
| zerg_lurker | 7 | mods/cameo/rules/starcraft.yaml |
| zerg_mutalisk | 5 | mods/cameo/rules/starcraft.yaml |
| zerg_nyduscanal | 4 | mods/cameo/rules/starcraft.yaml |
| zerg_overlord | 5 | mods/cameo/rules/starcraft.yaml |
| zerg_overmind | 5 | mods/cameo/rules/starcraft.yaml |
| zerg_queen | 5 | mods/cameo/rules/starcraft.yaml |
| zerg_queensnest | 4 | mods/cameo/rules/starcraft.yaml |
| zerg_scourge | 5 | mods/cameo/rules/starcraft.yaml |
| zerg_shriek | 5 | mods/cameo/rules/starcraft.yaml |
| zerg_spawningpool | 4 | mods/cameo/rules/starcraft.yaml |
| zerg_spire | 4 | mods/cameo/rules/starcraft.yaml |
| zerg_spithid | 5 | mods/cameo/rules/starcraft.yaml |
| zerg_sporecolony | 5 | mods/cameo/rules/starcraft.yaml |
| zerg_sporemaw | 6 | mods/cameo/rules/starcraft.yaml |
| zerg_sunkencolony_2 | 5 | mods/cameo/rules/starcraft.yaml |
| zerg_swarmling | 5 | mods/cameo/rules/starcraft.yaml |
| zerg_talon | 5 | mods/cameo/rules/starcraft.yaml |
| zerg_ultralisk | 7 | mods/cameo/rules/starcraft.yaml |
| zerg_ultraliskcavern | 4 | mods/cameo/rules/starcraft.yaml |
| zerg_zergling | 5 | mods/cameo/rules/starcraft.yaml |
| zerofighter | 4 | mods/cameo/rules/redalert.yaml |


## V5 — actors with > 2 trait removals

| actor | removals | keys | file |
|---|---|---|---|
| A10Carrier | 4 | -UpdatesPlayerStatistics, -ActorLostNotification, -WithShadow, -MapEditorData | mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/aircraft.yaml |
| BARL | 11 | -Selectable, -ShakeOnDeath, -SoundOnDamageTransition, -Demolishable, -CaptureManager, -Capturable | mods/cameo/rules/tech.yaml |
| BRL3 | 11 | -Selectable, -ShakeOnDeath, -SoundOnDamageTransition, -Demolishable, -CaptureManager, -Capturable | mods/cameo/rules/tech.yaml |
| ChronoVortexFade | 3 | -SpawnActorOnDeath, -PeriodicExplosion, -AmbientSound | mods/cameo/rules/redalert.yaml |
| SCCOMMANDCENTERM | 4 | -AttackAircraft, -SpawnActorOnDeath, -Hovers@CRUISING, -Voiced | mods/cameo/rules/starcraft.yaml |
| SILO | 3 | -GivesBuildableArea, -WithSpriteBody, -AcceptsDeliveredCash | mods/cameo/ContentPacks/TiberianDawn/Shared/yaml/buildings.yaml |
| cabal_ascended | 3 | -DamagedByTerrain, -SpawnActorOnDeath, -TakeCover | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/infantry.yaml |
| cabal_berserker | 3 | -DamagedByTerrain, -SpawnActorOnDeath, -TakeCover | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/vehicles.yaml |
| cabal_cyborgcommando | 3 | -DamagedByTerrain, -SpawnActorOnDeath, -TakeCover | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/infantry.yaml |
| cabal_cyborgcommandov2 | 3 | -DamagedByTerrain, -SpawnActorOnDeath, -TakeCover | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/infantry.yaml |
| cabal_cyborginfantry | 3 | -DamagedByTerrain, -SpawnActorOnDeath, -TakeCover | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/infantry.yaml |
| cabal_devout | 3 | -DamagedByTerrain, -SpawnActorOnDeath, -TakeCover | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/infantry.yaml |
| cabal_engineer | 4 | -TemporaryOwnerManager, -TakeCover, -DamagedByTerrain, -SpawnActorOnDeath | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/infantry.yaml |
| cabal_hackercyborg | 3 | -DamagedByTerrain, -SpawnActorOnDeath, -TakeCover | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/infantry.yaml |
| cabal_hunter_drone | 3 | -ActorLostNotification, -UpdatesPlayerStatistics, -MapEditorData | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/aircraft.yaml |
| cabal_mobilestealthgenerator | 5 | -ExternalCondition@CLOAK, -ExternalCondition@TSCLO, -Cloak@TDcloak, -Cloak@TScloak, -ActorStatValues | mods/cameo/rules/tiberiansun.yaml |
| cabal_plasmaturret | 3 | -WithVoxelBody, -Cloak@TDcloak, -ActorStatValues | mods/cameo/rules/tiberiansun.yaml |
| cabal_rocketcyborg | 3 | -DamagedByTerrain, -SpawnActorOnDeath, -TakeCover | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/infantry.yaml |
| cruiser_f.steel | 5 | -Selectable, -UpdatesPlayerStatistics, -ActorLostNotification, -WithShadow, -MapEditorData | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/aircraft.yaml |
| drone.nax | 4 | -UpdatesPlayerStatistics, -ActorLostNotification, -MapEditorData, -Voiced | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/aircraft.yaml |
| farasha_drone.ixian | 3 | -ActorLostNotification, -UpdatesPlayerStatistics, -MapEditorData | mods/cameo/ContentPacks/D2k/Ixian/yaml/aircraft.yaml |
| forgotten_apache_husk | 3 | -WithShadow, -Cloak@TDcloak, -Cloak@TScloak | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/husks.yaml |
| forgotten_ghoststalker_sp | 4 | -Buildable, -MapEditorData, -Voiced, -Armament@c4 | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/infantry.yaml |
| forgotten_mutant_sp | 3 | -Buildable, -MapEditorData, -Voiced | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/infantry.yaml |
| forgotten_mutantsniper_sp | 3 | -Buildable, -MapEditorData, -Voiced | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/infantry.yaml |
| forgotten_mutantsoldier_sp | 3 | -Buildable, -MapEditorData, -Voiced | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/infantry.yaml |
| fremen_creep | 3 | -MustBeDestroyed, -RevealsShroud@base-reve, -GrantConditionOnPrerequ | mods/cameo/ContentPacks/D2k/Shared/yaml/infantry.yaml |
| futuretech_spyfutu | 3 | -Guard, -WithInfantryBody, -AttackFrontal | mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/infantry.yaml |
| gdirigdrone | 5 | -Targetable@SpecialRepai, -SpawnActorOnDeath, -ActorLostNotification, -UpdatesPlayerStatistics, -MapEditorData | mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/aircraft.yaml |
| hole.nax2 | 3 | -UpdatesPlayerStatistics, -ActorLostNotification, -MapEditorData | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/infantry.yaml |
| humans_ballista | 3 | -Integrity, -GrantCondition@electron, -WithDeathAnimation | mods/cameo/rules/warcraft2.yaml |
| humans_humanscouttower | 3 | -WithTurretSearchlight, -WithDeathAnimation, -WithMakeAnimation | mods/cameo/rules/warcraft2.yaml |
| humans_knight | 5 | -Integrity, -GrantCondition@electron, -WithFacingSpriteBody, -WithMoveAnimation, -WithAttackAnimation | mods/cameo/rules/warcraft2.yaml |
| humans_mobileconstructionvehiclehuman | 3 | -WithFacingSpriteBody, -WithMoveAnimation, -WithAttackAnimation | mods/cameo/rules/warcraft2.yaml |
| humans_siegeengine | 4 | -AttackFrontal, -Integrity, -GrantCondition@electron, -WithDeathAnimation | mods/cameo/rules/warcraft2.yaml |
| interceptor.nax | 4 | -UpdatesPlayerStatistics, -ActorLostNotification, -MapEditorData, -Voiced | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/aircraft.yaml |
| ixian_stormlasher | 3 | -WithDeathAnimation, -WithWallSpriteBody, -WithSpriteTurret | mods/cameo/ContentPacks/D2k/Ixian/yaml/buildings.yaml |
| kami.asian | 4 | -UpdatesPlayerStatistics, -ActorLostNotification, -MapEditorData, -Voiced | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/aircraft.yaml |
| kami_asdf.asian | 4 | -CarrierSlave, -AutoTarget, -AmmoPool, -WithAmmoPipsDecoration | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/aircraft.yaml |
| latin_syndicate_defensebureau | 3 | -WithTurretSearchlight, -WithDeathAnimation, -QuantizeFacingsFromSequ | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/buildings.yaml |
| latin_syndicate_narco | 4 | -ReloadAmmoDelayMultipli, -ReloadAmmoDelayMultipli, -ReloadAmmoDelayMultipli, -ReloadAmmoDelayMultipli | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/infantry.yaml |
| latin_syndicate_nuketruck | 3 | -RenderRangeCircle, -DamagedByTintedCells@ra, -DamagedByTintedCells@ge | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/vehicles.yaml |
| orcs_catapult | 3 | -Integrity, -GrantCondition@electron, -WithDeathAnimation | mods/cameo/rules/warcraft2.yaml |
| orcs_mobileconstructionvehicleorc | 3 | -WithFacingSpriteBody, -WithMoveAnimation, -WithAttackAnimation | mods/cameo/rules/warcraft2.yaml |
| orcs_ogre | 5 | -Integrity, -GrantCondition@electron, -WithFacingSpriteBody, -WithMoveAnimation, -WithAttackAnimation | mods/cameo/rules/warcraft2.yaml |
| orcs_orcwatchtower | 3 | -WithTurretSearchlight, -WithDeathAnimation, -WithMakeAnimation | mods/cameo/rules/warcraft2.yaml |
| orcs_siegeengine | 4 | -AttackFrontal, -Integrity, -GrantCondition@electron, -WithDeathAnimation | mods/cameo/rules/warcraft2.yaml |
| protoss_analogue | 3 | -WithInfantryBody, -Targetable@disguise, -WithDeathAnimation | mods/cameo/rules/starcraft.yaml |
| protoss_archon | 5 | -WithInfantryBody, -Targetable@disguise, -HitShape, -WithDeathAnimation, -Crushable | mods/cameo/rules/starcraft.yaml |
| protoss_idol | 4 | -ExternalCondition@Propa, -WithInfantryBody, -Targetable@disguise, -WithDeathAnimation | mods/cameo/rules/starcraft.yaml |
| protoss_manifold | 3 | -WithInfantryBody, -Targetable@disguise, -WithDeathAnimation | mods/cameo/rules/starcraft.yaml |
| protoss_photoncannon | 3 | -WithTurretSearchlight, -WithSpriteBody, -WithDeathAnimation | mods/cameo/rules/starcraft.yaml |
| protoss_shieldbattery | 3 | -WithSpriteBody, -GivesBuildableArea, -WithDeathAnimation | mods/cameo/rules/starcraft.yaml |
| ra1_allies_camopillbox | 3 | -WithTurretSearchlight, -QuantizeFacingsFromSequ, -MustBeDestroyed | mods/cameo/rules/redalert.yaml |
| ra1_allies_gapgenerator | 10 | -AutoTarget, -RenderRangeCircle, -ExternalCondition@shrou, -ExternalCondition@locko, -RangeMultiplier@ra1_all, -RevealsShroudMultiplier | mods/cameo/rules/redalert.yaml |
| ra1_soviet_cyberdog | 3 | -Targetable@disguise, -InaccuracyMultiplier@ar, -InaccuracyMultiplier@Pr | mods/cameo/rules/redalert.yaml |
| ra1_soviet_kotinnucleartank | 3 | -DamagedByTintedCells@ra, -DamagedByTintedCells@ge, -DamagedByTintedCells@ti | mods/cameo/rules/redalert.yaml |
| ra2_allies_gapgenerator | 10 | -AutoTarget, -RenderRangeCircle, -ExternalCondition@shrou, -ExternalCondition@locko, -RangeMultiplier@ra1_all, -RevealsShroudMultiplier | mods/cameo/rules/redalert2.yaml |
| ra2_allies_ra2spy | 3 | -Guard, -WithInfantryBody, -AttackFrontal | mods/cameo/rules/redalert2.yaml |
| ra2_soviets_kirovairship | 3 | -DamagedByTintedCells@ra, -DamagedByTintedCells@ge, -DamagedByTintedCells@ti | mods/cameo/rules/redalert2.yaml |
| ra2_soviets_migbomber | 3 | -DamagedByTintedCells@ra, -DamagedByTintedCells@ge, -DamagedByTintedCells@ti | mods/cameo/rules/redalert2.yaml |
| ra2_soviets_siegechopper | 5 | -AttackAircraft, -WithShadow, -DamagedByTintedCells@ra, -DamagedByTintedCells@ge, -DamagedByTintedCells@ti | mods/cameo/rules/redalert2.yaml |
| ra2_soviets_transportkirov | 3 | -DamagedByTintedCells@ra, -DamagedByTintedCells@ge, -DamagedByTintedCells@ti | mods/cameo/rules/redalert2.yaml |
| ra2asw | 4 | -UpdatesPlayerStatistics, -ActorLostNotification, -WithShadow, -MapEditorData | mods/cameo/rules/redalert2.yaml |
| ra2cplanesov | 3 | -DamagedByTintedCells@ra, -DamagedByTintedCells@ge, -DamagedByTintedCells@ti | mods/cameo/rules/redalert2.yaml |
| ra2hornet | 4 | -UpdatesPlayerStatistics, -ActorLostNotification, -WithShadow, -MapEditorData | mods/cameo/rules/redalert2.yaml |
| ra2shk.bot | 6 | -MapEditorData, -UpdatesPlayerStatistics, -Armament@PRIMARY, -Armament@PRIMARY2, -Armament@PRIMARY3, -AttackFrontal | mods/cameo/rules/redalert2.yaml |
| ra2v3rocket | 8 | -FireWarheadsOnDeath, -DamagedByTintedCells@ra, -DamagedByTintedCells@ge, -DamagedByTintedCells@ti, -SpeedMultiplier@ra2_sov, -SpeedMultiplier@ra2_sov | mods/cameo/rules/redalert2.yaml |
| sc_zerg_larva | 9 | -DeathSounds@NORMAL, -SpawnActorOnDeath@zerg_, -WithDeathAnimation, -DamagedByTerrain, -Crushable, -TakeCover | mods/cameo/rules/starcraft.yaml |
| scadept.shade | 11 | -UpdatesPlayerStatistics, -MapEditorData, -ActorLostNotification, -GrantTimedConditionOnDe, -ShadeMaster, -Passenger | mods/cameo/rules/starcraft.yaml |
| sietch_creep | 10 | -RevealsShroud@base-reve, -GrantConditionOnPrerequ, -DamagedByTerrain, -GivesBuildableArea, -Sellable, -RepairableBuilding | mods/cameo/ContentPacks/D2k/Shared/yaml/buildings.yaml |
| sietch_creep_disabled | 16 | -Targetable, -FireProjectilesOnDeath, -Selectable, -Targetable@ivan, -Targetable@trappable, -Targetable@chrono | mods/cameo/ContentPacks/D2k/Shared/yaml/buildings.yaml |
| terran_battlecruiser | 4 | -ReloadAmmoDelayMultipli, -ReloadAmmoDelayMultipli, -ReloadAmmoDelayMultipli, -ReloadAmmoDelayMultipli | mods/cameo/rules/starcraft.yaml |
| terran_missileturret | 3 | -WithTurretSearchlight, -WithSpriteBody, -ActorPreviewPlaceBuildi | mods/cameo/rules/starcraft.yaml |
| terran_phobos | 5 | -AttackAircraft, -ReloadAmmoDelayMultipli, -ReloadAmmoDelayMultipli, -ReloadAmmoDelayMultipli, -ReloadAmmoDelayMultipli | mods/cameo/rules/starcraft.yaml |
| ts_gdi_carryall_husk | 3 | -WithShadow, -Cloak@TDcloak, -Cloak@TScloak | mods/cameo/ContentPacks/TiberianSun/GDI/yaml/husks.yaml |
| ts_gdi_orcabomber_husk | 3 | -WithShadow, -Cloak@TDcloak, -Cloak@TScloak | mods/cameo/ContentPacks/TiberianSun/GDI/yaml/husks.yaml |
| ts_gdi_orcafighter_husk | 3 | -WithShadow, -Cloak@TDcloak, -Cloak@TScloak | mods/cameo/ContentPacks/TiberianSun/GDI/yaml/husks.yaml |
| ts_nod_bansheefighter_husk | 3 | -WithShadow, -Cloak@TDcloak, -Cloak@TScloak | mods/cameo/ContentPacks/TiberianSun/Nod/yaml/husks.yaml |
| ts_nod_harpy_husk | 3 | -WithShadow, -Cloak@TDcloak, -Cloak@TScloak | mods/cameo/rules/tiberiansun.yaml |
| ts_nod_laserfence_segment | 5 | -Crushable, -Sellable, -Targetable, -Building, -WithWallSpriteBody | mods/cameo/rules/tiberiansun.yaml |
| ts_nod_mobilestealthgenerator | 4 | -ExternalCondition@CLOAK, -ExternalCondition@TSCLO, -Cloak@TDcloak, -Cloak@TScloak | mods/cameo/ContentPacks/TiberianSun/Nod/yaml/vehicles.yaml |
| tsprobe | 6 | -ActorLostNotification, -UpdatesPlayerStatistics, -RenderVoxels, -WithVoxelBody, -WithShadow, -SpawnActorOnDeath | mods/cameo/rules/tiberiansun.yaml |
| wc2_support_orc_eye_of_kilrogg | 4 | -Selectable, -Voiced, -Targetable@AIRBORNE, -SpawnActorOnDeath | mods/cameo/rules/warcraft2.yaml |
| yrbpln | 3 | -DamagedByTintedCells@ra, -DamagedByTintedCells@ge, -DamagedByTintedCells@ti | mods/cameo/rules/redalert2.yaml |
| yrschp.Husk | 3 | -WithShadow, -Cloak@TDcloak, -Cloak@TScloak | mods/cameo/rules/redalert2.yaml |
| yuri_biotrooper | 3 | -DamagedByTerrain, -DamagedByTintedCells@ra, -DamagedByTintedCells@ge | mods/cameo/rules/redalert2.yaml |
| yuri_constructionyard | 3 | -WithIdleOverlay@fans, -WithBuildingPlacedOverl, -ProvidesPrerequisite@ra | mods/cameo/rules/redalert2.yaml |
| yuri_gatlingcannon | 3 | -RenderRangeCircle, -WithVoxelBody, -Cloak@TDcloak | mods/cameo/rules/redalert2.yaml |
| zerg_creepcolony | 3 | -WithTurretSearchlight, -WithDeathAnimation, -WithMakeAnimation | mods/cameo/rules/starcraft.yaml |
| zerg_drone | 5 | -WithMakeAnimation, -WithFacingSpriteBody, -WithInfantryBody, -Targetable@disguise, -WithSpriteBody@deployed | mods/cameo/rules/starcraft.yaml |
| zerg_lurker | 3 | -HitShape, -WithMakeAnimation, -AttackFrontal | mods/cameo/rules/starcraft.yaml |
| zerg_overlord | 4 | -Targetable@infiltrate, -AttackAircraft, -AutoTarget, -RenderRangeCircle | mods/cameo/rules/starcraft.yaml |
| zerg_overmind | 3 | -WithMakeAnimation, -WithDeathAnimation, -ToggleConditionOnOrder | mods/cameo/rules/starcraft.yaml |
| zerofighter | 4 | -UpdatesPlayerStatistics, -ActorLostNotification, -WithShadow, -MapEditorData | mods/cameo/rules/redalert.yaml |

