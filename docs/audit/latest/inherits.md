# audit_inherits — §10.3 invariant violations (B2)

Actors+templates scanned: **3958**

| violation | meaning | count |
|---|---|---|
| V1 | concrete actor inherits from concrete actor | 281 |
| V2 | inherit crosses faction ownership | 0 |
| V3 | dangling inherit target (BLOCKING) | 0 |
| V4 | chain depth > 3 | 1659 |
| V5 | > 2 -Trait removals (warning) | 94 |


## V3 — dangling inherit targets (blocking)

_none found_


## V2 — cross-faction inherits (concrete targets)

_none found_


## V1 — concrete → concrete inherits

| actor | target | actor faction | target faction | file |
|---|---|---|---|---|
| A10.Husk | MIG.Husk | ? | ? | mods/cameo/rules/husks.yaml |
| A10Carrier.Husk | A10.Husk | ? | ? | mods/cameo/rules/husks.yaml |
| CAMERA.sw | CAMERA.small | ? | ? | mods/cameo/rules/misc.yaml |
| E1 | td_gdi_minigunner | tiberiandawn/gdi | tiberiandawn/gdi | mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/infantry.yaml |
| E3 | td_gdi_rocketsoldier | tiberiandawn/gdi | tiberiandawn/gdi | mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/infantry.yaml |
| EDEN_TIGER_ACIDCLOUD | EDEN_LYNX_ACIDCLOUD | ? | ? | mods/cameo/rules/outpost2.yaml |
| ForceShieldDrainer | CAMERA.small | ? | ? | mods/cameo/rules/shared.yaml |
| INVISIBLEPLANE | ra1_badger | ? | redalert/shared | mods/cameo/rules/tiberiansun.yaml |
| MONEYCRATE.LARGE | MONEYCRATE | ? | ? | mods/cameo/rules/misc.yaml |
| OILB.TS | OILB.Building | ? | ? | mods/cameo/rules/tiberiansun.yaml |
| PLYMOUTH_TIGER_EMP | PLYMOUTH_LYNX_EMP | ? | ? | mods/cameo/rules/outpost2.yaml |
| PLYMOUTH_TIGER_ESG | PLYMOUTH_LYNX_ESG | ? | ? | mods/cameo/rules/outpost2.yaml |
| PLYMOUTH_TIGER_MICROWAVE | PLYMOUTH_LYNX_MICROWAVE | ? | ? | mods/cameo/rules/outpost2.yaml |
| PLYMOUTH_TIGER_RPG | PLYMOUTH_LYNX_RPG | ? | ? | mods/cameo/rules/outpost2.yaml |
| PLYMOUTH_TIGER_STARFLARE | PLYMOUTH_LYNX_STARFLARE | ? | ? | mods/cameo/rules/outpost2.yaml |
| PLYMOUTH_TIGER_STICKYFOAM | PLYMOUTH_LYNX_STICKYFOAM | ? | ? | mods/cameo/rules/outpost2.yaml |
| PLYMOUTH_TIGER_SUPERNOVA | PLYMOUTH_LYNX_SUPERNOVA | ? | ? | mods/cameo/rules/outpost2.yaml |
| RABIO | bio | ? | ? | mods/cameo/rules/tech.yaml |
| RAMISS | MISS | ? | ? | mods/cameo/rules/tech.yaml |
| SCBARRACKSM | SCCOMMANDCENTERM | ? | ? | mods/cameo/rules/starcraft.yaml |
| SCENGINEERINGBAYM | SCCOMMANDCENTERM | ? | ? | mods/cameo/rules/starcraft.yaml |
| SCFACTORYM | SCCOMMANDCENTERM | ? | ? | mods/cameo/rules/starcraft.yaml |
| SCSCIENCEFACILITYM | SCCOMMANDCENTERM | ? | ? | mods/cameo/rules/starcraft.yaml |
| SCSCOURGEDRONE | zerg_scourge | ? | starcraft/zerg | mods/cameo/rules/starcraft.yaml |
| SCSENTINELM | SCCOMMANDCENTERM | ? | ? | mods/cameo/rules/starcraft.yaml |
| SCSTARPORTM | SCCOMMANDCENTERM | ? | ? | mods/cameo/rules/starcraft.yaml |
| TECHBCANNON2 | TECHBCANNON | ? | ? | mods/cameo/rules/tech.yaml |
| TSDPODE1 | TSDPOD | tiberiansun/gdi | tiberiansun/gdi | mods/cameo/ContentPacks/TiberianSun/GDI/yaml/aircraft.yaml |
| TSDPODE2 | TSDPOD | tiberiansun/gdi | tiberiansun/gdi | mods/cameo/ContentPacks/TiberianSun/GDI/yaml/aircraft.yaml |
| TSE1PARA | TSE1 | ? | ? | mods/cameo/rules/tiberiansun.yaml |
| TSE2PARA | ts_gdi_discthrower | ? | tiberiansun/gdi | mods/cameo/rules/tiberiansun.yaml |
| TSENGINEER | E6 | ? | tiberiandawn/shared | mods/cameo/rules/tiberiansun.yaml |
| VT01 | T01 | ? | ? | mods/cameo/rules/trees.yaml |
| VT02 | T02 | ? | ? | mods/cameo/rules/trees.yaml |
| VT03 | T03 | ? | ? | mods/cameo/rules/trees.yaml |
| VT05 | T05 | ? | ? | mods/cameo/rules/trees.yaml |
| VT06 | T06 | ? | ? | mods/cameo/rules/trees.yaml |
| VT07 | T07 | ? | ? | mods/cameo/rules/trees.yaml |
| VT08 | T08 | ? | ? | mods/cameo/rules/trees.yaml |
| VT10 | T10 | ? | ? | mods/cameo/rules/trees.yaml |
| VT11 | T11 | ? | ? | mods/cameo/rules/trees.yaml |
| VT12 | T12 | ? | ? | mods/cameo/rules/trees.yaml |
| VT13 | T13 | ? | ? | mods/cameo/rules/trees.yaml |
| VT14 | T14 | ? | ? | mods/cameo/rules/trees.yaml |
| VT15 | T15 | ? | ? | mods/cameo/rules/trees.yaml |
| VT16 | T16 | ? | ? | mods/cameo/rules/trees.yaml |
| VT17 | T17 | ? | ? | mods/cameo/rules/trees.yaml |
| VTC01 | TC01 | ? | ? | mods/cameo/rules/trees.yaml |
| VTC02 | TC02 | ? | ? | mods/cameo/rules/trees.yaml |
| VTC03 | TC03 | ? | ? | mods/cameo/rules/trees.yaml |
| VTC04 | TC04 | ? | ? | mods/cameo/rules/trees.yaml |
| VTC05 | TC05 | ? | ? | mods/cameo/rules/trees.yaml |
| WWCRATE | CRATE | ? | ? | mods/cameo/rules/misc.yaml |
| ambiance_battle | ambiance_wind | ? | ? | mods/cameo/rules/misc.yaml |
| ambiance_bird | ambiance_wind | ? | ? | mods/cameo/rules/misc.yaml |
| ambiance_bird_robin | ambiance_wind | ? | ? | mods/cameo/rules/misc.yaml |
| ambiance_ocean_calm | ambiance_wind | ? | ? | mods/cameo/rules/misc.yaml |
| ambiance_ocean_waves | ambiance_wind | ? | ? | mods/cameo/rules/misc.yaml |
| ambiance_rumbling | ambiance_wind | ? | ? | mods/cameo/rules/misc.yaml |
| asianalliance_heavyrailguntank | asianalliance_railguntank | redalert2mod/asianalliance | redalert2mod/asianalliance | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/vehicles.yaml |
| bbomb2_husk.nax2 | bbomb_husk.nax2 | redalert2mod/schwarzermond | redalert2mod/schwarzermond | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/aircraft.yaml |
| bbomb3_husk.nax2 | bbomb_husk.nax2 | redalert2mod/schwarzermond | redalert2mod/schwarzermond | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/aircraft.yaml |
| bomber_husk.asian | BADR.Husk | redalert2mod/asianalliance | ? | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/aircraft.yaml |
| bomber_minebomb2.asian | bomber_minebomb.asian | redalert2mod/asianalliance | redalert2mod/asianalliance | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/aircraft.yaml |
| cabal_artilleryspider_backup | cabal_artilleryspider | tiberiansun/cabal | tiberiansun/cabal | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/husks.yaml |
| cabal_avatar_backup | cabal_avatar | tiberiansun/cabal | tiberiansun/cabal | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/husks.yaml |
| cabal_manticore_backup | cabal_manticore | tiberiansun/cabal | tiberiansun/cabal | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/husks.yaml |
| cabal_orbdrone_slave | cabal_orbdrone | tiberiansun/cabal | tiberiansun/cabal | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/aircraft.yaml |
| cabal_tarantula_backup | cabal_tarantula | tiberiansun/cabal | tiberiansun/cabal | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/husks.yaml |
| cabal_widow_backup | cabal_widow | tiberiansun/cabal | tiberiansun/cabal | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/husks.yaml |
| camera.paradrop | RACAMERA | ? | ? | mods/cameo/rules/misc.yaml |
| camera.placeholderhack | CAMERA.small | ? | ? | mods/cameo/rules/misc.yaml |
| camera.psireveal | camera.scan | ? | ? | mods/cameo/rules/misc.yaml |
| camera.ra2spy | CAMERA.small | ? | ? | mods/cameo/rules/shared.yaml |
| camera.radarvan | camera.scan | ? | ? | mods/cameo/rules/misc.yaml |
| camera.sathack | camera.paradrop | ? | ? | mods/cameo/rules/misc.yaml |
| camera.spyplane | camera.scan | ? | ? | mods/cameo/rules/misc.yaml |
| camera.spysat | camera.scan | ? | ? | mods/cameo/rules/misc.yaml |
| carryall | carryall.reinforce | d2k/shared | d2k/shared | mods/cameo/ContentPacks/D2k/Shared/yaml/aircraft.yaml |
| carryall.paradrop | carryall.reinforce | d2k/shared | d2k/shared | mods/cameo/ContentPacks/D2k/Shared/yaml/aircraft.yaml |
| corpse_big.nax | corpse.nax | redalert2mod/naxis | redalert2mod/naxis | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/vehicles.yaml |
| deathcash.latin | RACAMERA | redalert2mod/syndicate | ? | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/upgrades.yaml |
| deathcash_small.latin | RACAMERA | redalert2mod/syndicate | ? | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/upgrades.yaml |
| forgotten_engineer | TSENGINEER | tiberiansun/forgotten | ? | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/infantry.yaml |
| forgotten_ghoststalker_r4 | forgotten_ghoststalker | tiberiansun/forgotten | tiberiansun/forgotten | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/infantry.yaml |
| forgotten_ghoststalker_sp | forgotten_ghoststalker | tiberiansun/forgotten | tiberiansun/forgotten | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/infantry.yaml |
| forgotten_mutant_sp | forgotten_mutant | tiberiansun/forgotten | tiberiansun/forgotten | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/infantry.yaml |
| forgotten_mutant_wild | forgotten_mutant | tiberiansun/forgotten | tiberiansun/forgotten | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/infantry.yaml |
| forgotten_mutantsniper | forgotten_mutant | tiberiansun/forgotten | tiberiansun/forgotten | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/infantry.yaml |
| forgotten_mutantsniper_r4 | forgotten_mutantsniper | tiberiansun/forgotten | tiberiansun/forgotten | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/infantry.yaml |
| forgotten_mutantsniper_sp | forgotten_mutantsniper | tiberiansun/forgotten | tiberiansun/forgotten | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/infantry.yaml |
| forgotten_mutantsoldier_sp | forgotten_mutantsoldier | tiberiansun/forgotten | tiberiansun/forgotten | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/infantry.yaml |
| forgotten_rocketinfantry | TSE3 | tiberiansun/forgotten | ? | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/infantry.yaml |
| forgotten_tiberianfiend_wild | forgotten_tiberianfiend | tiberiansun/forgotten | tiberiansun/forgotten | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/infantry.yaml |
| forgotten_tiberiumspike | OILB.TS | tiberiansun/forgotten | ? | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/buildings.yaml |
| frigate.paradrop | frigate | d2k/shared | d2k/shared | mods/cameo/ContentPacks/D2k/Shared/yaml/aircraft.yaml |
| hole_small.nax2 | hole.nax2 | redalert2mod/schwarzermond | redalert2mod/schwarzermond | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/infantry.yaml |
| japan_archermaiden | japan_tankbuster | redalert/japan | redalert/japan | mods/cameo/ContentPacks/RedAlert/Japan/yaml/infantry.yaml |
| japan_badger | ra1_badger | redalert/shared | redalert/shared | mods/cameo/ContentPacks/RedAlert/Shared/yaml/aircraft.yaml |
| japan_coreairfield | japan_corewarfactory | redalert/japan | redalert/japan | mods/cameo/ContentPacks/RedAlert/Japan/yaml/vehicles.yaml |
| japan_corebarracks | japan_corewarfactory | redalert/japan | redalert/japan | mods/cameo/ContentPacks/RedAlert/Japan/yaml/vehicles.yaml |
| japan_corepowerplant | japan_corewarfactory | redalert/japan | redalert/japan | mods/cameo/ContentPacks/RedAlert/Japan/yaml/vehicles.yaml |
| japan_coreradar | japan_corewarfactory | redalert/japan | redalert/japan | mods/cameo/ContentPacks/RedAlert/Japan/yaml/vehicles.yaml |
| japan_corerefinery | japan_corewarfactory | redalert/japan | redalert/japan | mods/cameo/ContentPacks/RedAlert/Japan/yaml/vehicles.yaml |
| japan_coreservicedepot | japan_corewarfactory | redalert/japan | redalert/japan | mods/cameo/ContentPacks/RedAlert/Japan/yaml/vehicles.yaml |
| japan_coretechcenter | japan_corewarfactory | redalert/japan | redalert/japan | mods/cameo/ContentPacks/RedAlert/Japan/yaml/vehicles.yaml |
| japan_japanesesuperbomber | ra1_badger | redalert/shared | redalert/shared | mods/cameo/ContentPacks/RedAlert/Shared/yaml/aircraft.yaml |
| jsuperbomber.Husk | BADR.Husk | ? | ? | mods/cameo/rules/husks.yaml |
| kami_asdf.asian | kami.asian | redalert2mod/asianalliance | redalert2mod/asianalliance | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/aircraft.yaml |
| kami_chemical.asian | kami.asian | redalert2mod/asianalliance | redalert2mod/asianalliance | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/aircraft.yaml |
| modbomber.Husk | YAK.Husk | ? | ? | mods/cameo/rules/husks.yaml |
| modkami.Husk | YAK.Husk | ? | ? | mods/cameo/rules/husks.yaml |
| modkamimini.Husk | YAK.Husk | ? | ? | mods/cameo/rules/husks.yaml |
| ordos_stealthraider | ordos_raider | d2k/ordos | d2k/ordos | mods/cameo/ContentPacks/D2k/Ordos/yaml/vehicles.yaml |
| ra1_allies_badger | ra1_badger | redalert/shared | redalert/shared | mods/cameo/ContentPacks/RedAlert/Shared/yaml/aircraft.yaml |
| ra1_allies_cargoplanebomber | ra1_badger_bomber | redalert/shared | redalert/shared | mods/cameo/ContentPacks/RedAlert/Shared/yaml/aircraft.yaml |
| ra1_allies_cargoplaneparadrop | ra1_badger | redalert/shared | redalert/shared | mods/cameo/ContentPacks/RedAlert/Shared/yaml/aircraft.yaml |
| ra1_allies_chronovortexfade | ra1_allies_chronovortex | redalert/shared | redalert/shared | mods/cameo/ContentPacks/RedAlert/Shared/yaml/misc.yaml |
| ra1_badger_bomber | ra1_badger | redalert/shared | redalert/shared | mods/cameo/ContentPacks/RedAlert/Shared/yaml/aircraft.yaml |
| ra1_soviets_badger | ra1_badger | redalert/shared | redalert/shared | mods/cameo/ContentPacks/RedAlert/Shared/yaml/aircraft.yaml |
| ra1_soviets_heavyindustrialminer | ra1_soviets_oretruck | redalert/soviets | redalert/soviets | mods/cameo/ContentPacks/RedAlert/Soviets/yaml/vehicles.yaml |
| ra1_soviets_largefactory | ra1_soviets_warfactory | redalert/soviets | redalert/soviets | mods/cameo/ContentPacks/RedAlert/Soviets/yaml/buildings.yaml |
| ra1_soviets_largesovietairfield | ra1_soviets_airfield | redalert/soviets | redalert/soviets | mods/cameo/ContentPacks/RedAlert/Soviets/yaml/buildings.yaml |
| ra1_soviets_mammothtank.colorpicker | ra1_soviets_mammothtank | ? | redalert/soviets | mods/cameo/rules/misc.yaml |
| ra1_soviets_superspyplane | ra1_soviets_spyplane | redalert/shared | redalert/shared | mods/cameo/ContentPacks/RedAlert/Shared/yaml/aircraft.yaml |
| ra2_allies_battlefortress_chrono | ra2_allies_battlefortress | redalert2/allies | redalert2/allies | mods/cameo/ContentPacks/RedAlert2/Allies/yaml/vehicles.yaml |
| ra2_allies_battlefortress_empty | ra2_allies_battlefortress | redalert2/allies | redalert2/allies | mods/cameo/ContentPacks/RedAlert2/Allies/yaml/vehicles.yaml |
| ra2_allies_ifv_chrono | ra2_allies_ifv_mg | redalert2/allies | redalert2/allies | mods/cameo/ContentPacks/RedAlert2/Allies/yaml/vehicles.yaml |
| ra2_allies_ifv_hmg | ra2_allies_ifv | redalert2/allies | redalert2/allies | mods/cameo/ContentPacks/RedAlert2/Allies/yaml/vehicles.yaml |
| ra2_allies_ifv_mg | ra2_allies_ifv | redalert2/allies | redalert2/allies | mods/cameo/ContentPacks/RedAlert2/Allies/yaml/vehicles.yaml |
| ra2_allies_ifv_missile | ra2_allies_ifv | redalert2/allies | redalert2/allies | mods/cameo/ContentPacks/RedAlert2/Allies/yaml/vehicles.yaml |
| ra2_allies_ifv_repair | ra2_allies_ifv_mg | redalert2/allies | redalert2/allies | mods/cameo/ContentPacks/RedAlert2/Allies/yaml/vehicles.yaml |
| ra2_c_hum2 | ra2_c_hum | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_city01 | ra2ctchig01 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_city02 | ra2ctchig01 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_city03 | ra2ctchig01 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_city04 | ra2ctchig01 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_city06 | ra2ctchig01 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_crate | CRATE | ? | ? | mods/cameo/rules/misc.yaml |
| ra2_ctfrmb | ra2ctbunk01 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ctgard01 | ra2ctbunk01 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ctgard03 | ra2ctbunk01 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ctnwy09 | ra2ctchig01 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ctnwy22 | ra2ctchig01 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ctnwy23 | ra2ctchig01 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ctnwy24 | ra2ctchig01 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ctnwy25 | ra2ctchig01 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ctpars02 | ra2ctchig01 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ctpars04 | ra2ctchig01 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ctpars05 | ra2ctchig01 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ctpars06 | ra2ctchig01 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ctpars07 | ra2ctbunk01 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ctpars08 | ra2ctchig01 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ctpars09 | ra2ctchig01 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ctpars10 | ra2ctchig01 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ctpars12 | ra2ctchig01 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ctpars13 | ra2ctchig01 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ctpars14 | ra2ctchig01 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ctrus03 | ra2ctchig01 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ctrus04 | ra2ctchig01 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ctrus05 | ra2ctchig01 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ctrus06 | ra2ctchig01 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ctrus07 | ra2ctchig01 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ctrus08 | ra2ctchig01 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ctrus09 | ra2ctchig01 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ctrus10 | ra2ctchig01 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ctrus11 | ra2ctchig01 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ctsanf01 | ra2ctchig01 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ctsanf02 | ra2ctchig01 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ctsanf03 | ra2ctchig01 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ctsanf05 | ra2ctchig01 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ctsanf06 | ra2ctchig01 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ctsanf07 | ra2ctchig01 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ctsanf08 | ra2ctchig01 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ctsanf16 | ra2ctchig01 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ctsanf17 | ra2ctchig01 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ctsanf18 | ra2ctchig01 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_cttexs01 | ra2ctchig01 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_cttexs02 | ra2ctchig01 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_cttexs03 | ra2ctchig01 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_cttexs04 | ra2ctchig01 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_cttexs05 | ra2ctchig01 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_cttexs06 | ra2ctchig01 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_cttexs07 | ra2ctchig01 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_cttexs08 | ra2ctchig01 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ctwash03 | ra2ctchig01 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ctwash04 | ra2ctchig01 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ctwash05 | ra2ctchig01 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ctwash06 | ra2ctchig01 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ctwash07 | ra2ctchig01 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ctwash08 | ra2ctchig01 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ctwash09 | ra2ctchig01 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ctwash10 | ra2ctchig01 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ctwash11 | ra2ctchig01 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ctwash13 | ra2ctchig01 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ctwash17 | ra2ctchig01 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2caairpv | ra2caairp | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2ctarmy01 | ra2ctbunk01 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2ctarmy02 | ra2ctbunk01 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2ctarmy03 | ra2ctarmy02 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2ctarmy04 | ra2ctarmy02 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2ctbarn02 | ra2ctbunk01 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2ctbunk02 | ra2ctbunk01 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2ctchig01 | ra2ctbunk01 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2ctchig02 | ra2ctchig01 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2ctchig03 | ra2ctchig01 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2cteur01 | ra2ctbunk01 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2cteur02 | ra2ctbunk01 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2cteur04 | ra2ctchig01 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2ctfarm01 | ra2ctbunk01 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2ctfarm06 | ra2ctbunk01 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2ctfrma | ra2ctbunk01 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2ctgas01 | ra2ctchig01 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2cthse01 | ra2ctbunk01 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2cthse02 | ra2ctbunk01 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2cthse03 | ra2ctchig01 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2cthse04 | ra2ctbunk01 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2cthse05 | ra2ctbunk01 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2cthse06 | ra2cthse05 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2cthse07 | ra2ctbunk01 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2ctind01 | ra2ctchig01 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2ctlab | ra2ctchig01 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2ctmiam01 | ra2ctchig01 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2ctmiam02 | ra2ctchig01 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2ctmiam03 | ra2ctchig01 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2ctmiam04 | ra2ctbunk01 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2ctmiam05 | ra2ctchig01 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2ctmiam06 | ra2ctmiam05 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2ctmiam07 | ra2ctmiam05 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2ctmiam08 | ra2ctbunk01 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2ctmsc07 | ra2ctbunk01 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2ctmsc08 | ra2ctarmy02 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2ctmsc09 | ra2ctmsc08 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2ctmsc10 | ra2ctchig01 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2ctnewy01 | ra2ctchig01 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2ctnewy06 | ra2ctchig01 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2ctnewy07 | ra2ctchig01 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2ctnewy08 | ra2ctchig01 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2ctnewy10 | ra2ctchig01 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2ctnewy11 | ra2ctchig01 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2ctnewy12 | ra2ctnewy08 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2ctnewy13 | ra2ctnewy08 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2ctnewy14 | ra2ctnewy08 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2ctnewy15 | ra2ctnewy07 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2ctnewy16 | ra2ctnewy08 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2ctnewy17 | ra2ctnewy16 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2ctnewy18 | ra2ctnewy17 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2ctnewy20 | ra2ctchig01 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2ctnewy21 | ra2ctnewy20 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2ctnewy26 | ra2ctchig01 | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2v3rocketelite | ra2v3rocket | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| scadept.shade | protoss_adept | ? | starcraft/protoss | mods/cameo/rules/starcraft.yaml |
| sonar | camera.spyplane | ? | ? | mods/cameo/rules/misc.yaml |
| td_gdi_humveemkii | td_gdi_humvee | tiberiandawn/gdi | tiberiandawn/gdi | mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/vehicles.yaml |
| td_nod_buggymkii | td_nod_buggy | tiberiandawn/nod | tiberiandawn/nod | mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/vehicles.yaml |
| ts_crate | CRATE | ? | ? | mods/cameo/rules/misc.yaml |
| ts_gdi_engineer | TSENGINEER | tiberiansun/gdi | ? | mods/cameo/ContentPacks/TiberianSun/GDI/yaml/infantry.yaml |
| ts_gdi_lightinfantry | TSE1 | tiberiansun/gdi | ? | mods/cameo/ContentPacks/TiberianSun/GDI/yaml/infantry.yaml |
| ts_nod_engineer | TSENGINEER | tiberiansun/nod | ? | mods/cameo/ContentPacks/TiberianSun/Nod/yaml/infantry.yaml |
| ts_nod_lightinfantry | TSE1 | tiberiansun/nod | ? | mods/cameo/ContentPacks/TiberianSun/Nod/yaml/infantry.yaml |
| ts_nod_rocketinfantry | TSE3 | tiberiansun/nod | ? | mods/cameo/ContentPacks/TiberianSun/Nod/yaml/infantry.yaml |
| tsfsmoker.bomber | tsfsmoker | ? | ? | mods/cameo/rules/tiberiansun.yaml |
| tsmonstermaker1 | VICE | ? | ? | mods/cameo/rules/tiberiansun.yaml |
| wc2_camera_scanner | camera.scan | ? | ? | mods/cameo/rules/warcraft2.yaml |
| wc2_humans_cannontower | wc2_humans_humanscouttower | warcraft2/humans | warcraft2/humans | mods/cameo/ContentPacks/Warcraft2/Humans/yaml/defenses.yaml |
| wc2_humans_elvenranger | wc2_humans_elvenarcher | warcraft2/humans | warcraft2/humans | mods/cameo/ContentPacks/Warcraft2/Humans/yaml/infantry.yaml |
| wc2_humans_guardtower | wc2_humans_humanscouttower | warcraft2/humans | warcraft2/humans | mods/cameo/ContentPacks/Warcraft2/Humans/yaml/defenses.yaml |
| wc2_humans_humangoldmine_bot | wc2_humans_humangoldmine | warcraft2/humans | warcraft2/humans | mods/cameo/ContentPacks/Warcraft2/Humans/yaml/buildings.yaml |
| wc2_humans_paladin | wc2_humans_knight | warcraft2/humans | warcraft2/humans | mods/cameo/ContentPacks/Warcraft2/Humans/yaml/vehicles.yaml |
| wc2_orc_skeleton | wc2_orcs_grunt | ? | warcraft2/orcs | mods/cameo/rules/warcraft2.yaml |
| wc2_orcs_cannontower | wc2_orcs_orcwatchtower | warcraft2/orcs | warcraft2/orcs | mods/cameo/ContentPacks/Warcraft2/Orcs/yaml/defenses.yaml |
| wc2_orcs_guardtower | wc2_orcs_orcwatchtower | warcraft2/orcs | warcraft2/orcs | mods/cameo/ContentPacks/Warcraft2/Orcs/yaml/defenses.yaml |
| wc2_orcs_ogremage | wc2_orcs_ogre | warcraft2/orcs | warcraft2/orcs | mods/cameo/ContentPacks/Warcraft2/Orcs/yaml/vehicles.yaml |
| wc2_orcs_orcgoldmine_bot | wc2_orcs_orcgoldmine | warcraft2/orcs | warcraft2/orcs | mods/cameo/ContentPacks/Warcraft2/Orcs/yaml/buildings.yaml |
| wc2_orcs_trollberserker | wc2_orcs_trollaxethrower | warcraft2/orcs | warcraft2/orcs | mods/cameo/ContentPacks/Warcraft2/Orcs/yaml/infantry.yaml |
| yakarmored.Husk | YAK.Husk | ? | ? | mods/cameo/rules/husks.yaml |
| yaktesla.Husk | YAK.Husk | ? | ? | mods/cameo/rules/husks.yaml |
| yrlunr.husk | ra2rock.husk | redalert2/shared | redalert2/shared | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| zerg_creepcolony_defense | zerg_creepcolony | starcraft/zerg | starcraft/zerg | mods/cameo/ContentPacks/StarCraft/Zerg/yaml/defenses.yaml |
| zerg_sporecolony | zerg_creepcolony | starcraft/zerg | starcraft/zerg | mods/cameo/ContentPacks/StarCraft/Zerg/yaml/defenses.yaml |
| zerg_sunkencolony_defense | zerg_creepcolony | starcraft/zerg | starcraft/zerg | mods/cameo/ContentPacks/StarCraft/Zerg/yaml/defenses.yaml |


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
| C1 | 5 | mods/cameo/rules/civilian.yaml |
| C10 | 5 | mods/cameo/rules/civilian.yaml |
| C2 | 5 | mods/cameo/rules/civilian.yaml |
| C3 | 5 | mods/cameo/rules/civilian.yaml |
| C4 | 5 | mods/cameo/rules/civilian.yaml |
| C5 | 5 | mods/cameo/rules/civilian.yaml |
| C6 | 5 | mods/cameo/rules/civilian.yaml |
| C7 | 5 | mods/cameo/rules/civilian.yaml |
| C8 | 5 | mods/cameo/rules/civilian.yaml |
| C9 | 5 | mods/cameo/rules/civilian.yaml |
| CHAN | 5 | mods/cameo/rules/civilian.yaml |
| CNCCA | 4 | mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/naval.yaml |
| CNCPT | 4 | mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/naval.yaml |
| CNCRSS | 4 | mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/naval.yaml |
| CNCSS | 4 | mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/naval.yaml |
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
| FCOM | 4 | mods/cameo/rules/tech.yaml |
| FCOM.Husk | 4 | mods/cameo/rules/tech.yaml |
| HIND.Husk | 4 | mods/cameo/rules/husks.yaml |
| HOSP.Husk | 4 | mods/cameo/rules/tech.yaml |
| INVISIBLEPLANE | 4 | mods/cameo/rules/tiberiansun.yaml |
| JHIND.Husk | 4 | mods/cameo/rules/husks.yaml |
| LST | 4 | mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/naval.yaml |
| MISS | 4 | mods/cameo/rules/tech.yaml |
| MISS.Husk | 4 | mods/cameo/rules/tech.yaml |
| MOEBIUS | 5 | mods/cameo/rules/civilian.yaml |
| NUK2 | 4 | mods/cameo/ContentPacks/TiberianDawn/Shared/yaml/buildings.yaml |
| NUKE | 4 | mods/cameo/ContentPacks/TiberianDawn/Shared/yaml/buildings.yaml |
| OILB.Building | 4 | mods/cameo/rules/shared.yaml |
| OILB.Husk | 4 | mods/cameo/rules/tech.yaml |
| OILB.RA2 | 4 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| OILB.TS | 5 | mods/cameo/rules/tiberiansun.yaml |
| OILB.d2k | 4 | mods/cameo/ContentPacks/D2k/Shared/yaml/buildings.yaml |
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
| RABIO | 4 | mods/cameo/rules/tech.yaml |
| RAMISS | 5 | mods/cameo/rules/tech.yaml |
| RAPT | 4 | mods/cameo/rules/tiberiansun.yaml |
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
| V19.Husk | 4 | mods/cameo/rules/tech.yaml |
| VICE | 4 | mods/cameo/rules/civilian.yaml |
| YRDISK.Husk | 4 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| YRSLAV | 5 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| alien.nax | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/infantry.yaml |
| apparition.ixian | 5 | mods/cameo/ContentPacks/D2k/Ixian/yaml/vehicles.yaml |
| asianalliance_advancedcommunicationcenter | 5 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/buildings.yaml |
| asianalliance_alligator | 5 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/infantry.yaml |
| asianalliance_asdf | 5 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/infantry.yaml |
| asianalliance_asianairforcecommand | 4 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/buildings.yaml |
| asianalliance_asianbarracks | 4 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/buildings.yaml |
| asianalliance_asianbattlelab | 4 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/buildings.yaml |
| asianalliance_asiancommando | 5 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/infantry.yaml |
| asianalliance_asianconstructionyard | 4 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/buildings.yaml |
| asianalliance_asianflametank | 5 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/vehicles.yaml |
| asianalliance_asianflametrooper | 5 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/infantry.yaml |
| asianalliance_asianmilitia | 5 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/infantry.yaml |
| asianalliance_asianmobileconstructionvehicle | 4 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/vehicles.yaml |
| asianalliance_asianorerefinery | 4 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/buildings.yaml |
| asianalliance_asianpetrolplant | 4 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/buildings.yaml |
| asianalliance_asianradar | 4 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/buildings.yaml |
| asianalliance_asiansentryflamer | 5 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/buildings.yaml |
| asianalliance_asianservicedepot | 4 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/buildings.yaml |
| asianalliance_asiantankkiller | 5 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/infantry.yaml |
| asianalliance_asianwarfactory | 5 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/buildings.yaml |
| asianalliance_chaosstorminductor | 4 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/buildings.yaml |
| asianalliance_chaostower | 5 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/buildings.yaml |
| asianalliance_dragonfly | 4 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/vehicles.yaml |
| asianalliance_droneminer | 4 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/vehicles.yaml |
| asianalliance_engineer | 5 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/infantry.yaml |
| asianalliance_fanatic | 5 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/infantry.yaml |
| asianalliance_harbinger | 4 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/aircraft.yaml |
| asianalliance_heavyrailguntank | 6 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/vehicles.yaml |
| asianalliance_howitzer | 5 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/vehicles.yaml |
| asianalliance_hyperionprojector | 5 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/buildings.yaml |
| asianalliance_japanesesamurai | 5 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/infantry.yaml |
| asianalliance_lynxtank | 5 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/vehicles.yaml |
| asianalliance_militaryacademy | 5 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/buildings.yaml |
| asianalliance_pelican | 4 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/aircraft.yaml |
| asianalliance_phoenix | 4 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/aircraft.yaml |
| asianalliance_plasmacannon | 5 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/buildings.yaml |
| asianalliance_plasmatrooper | 5 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/infantry.yaml |
| asianalliance_pulsar | 5 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/buildings.yaml |
| asianalliance_pulverizer | 4 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/vehicles.yaml |
| asianalliance_pulverizermecha | 5 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/vehicles.yaml |
| asianalliance_quasar | 5 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/vehicles.yaml |
| asianalliance_railguntank | 5 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/vehicles.yaml |
| asianalliance_railtower | 5 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/buildings.yaml |
| asianalliance_shinobi | 5 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/infantry.yaml |
| asianalliance_spitfire | 5 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/buildings.yaml |
| asianalliance_tankreactor | 4 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/buildings.yaml |
| asianalliance_type89mlrs | 4 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/vehicles.yaml |
| asianalliance_veteranarcher | 5 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/infantry.yaml |
| asianalliance_viper | 5 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/vehicles.yaml |
| asianalliance_warturtle | 5 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/vehicles.yaml |
| assault.nax | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/vehicles.yaml |
| atreides_constructionyard | 5 | mods/cameo/ContentPacks/D2k/Atreides/yaml/buildings.yaml |
| atreides_heavyfactory | 5 | mods/cameo/ContentPacks/D2k/Atreides/yaml/buildings.yaml |
| atreides_mobileconstructionvehicle | 5 | mods/cameo/ContentPacks/D2k/Atreides/yaml/vehicles.yaml |
| bbomb2_husk.nax2 | 4 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/aircraft.yaml |
| bbomb3_husk.nax2 | 4 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/aircraft.yaml |
| bio.Husk | 4 | mods/cameo/rules/tech.yaml |
| bomber_husk.asian | 4 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/aircraft.yaml |
| bomber_minebomb2.asian | 4 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/aircraft.yaml |
| cabal_artilleryspider | 5 | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/vehicles.yaml |
| cabal_artilleryspider_backup | 6 | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/husks.yaml |
| cabal_ascended | 5 | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/infantry.yaml |
| cabal_avatar | 5 | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/vehicles.yaml |
| cabal_avatar_backup | 6 | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/husks.yaml |
| cabal_beholder | 5 | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/vehicles.yaml |
| cabal_berserker | 5 | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/vehicles.yaml |
| cabal_constructionyard | 4 | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/vehicles.yaml |
| cabal_core | 5 | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/buildings.yaml |
| cabal_coredefender | 4 | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/vehicles.yaml |
| cabal_cyborgassassin | 4 | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/aircraft.yaml |
| cabal_cyborgcommando | 5 | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/infantry.yaml |
| cabal_cyborgcommandov2 | 5 | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/infantry.yaml |
| cabal_cyborginfantry | 5 | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/infantry.yaml |
| cabal_cyborgreaper | 4 | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/vehicles.yaml |
| cabal_devout | 5 | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/infantry.yaml |
| cabal_dissolver | 5 | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/infantry.yaml |
| cabal_eliminator800 | 5 | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/infantry.yaml |
| cabal_engineer | 5 | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/infantry.yaml |
| cabal_enlighted | 5 | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/infantry.yaml |
| cabal_hackercyborg | 5 | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/infantry.yaml |
| cabal_heavycabalobelisk | 4 | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/defenses.yaml |
| cabal_heavyreaper | 4 | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/vehicles.yaml |
| cabal_hunterdrone | 4 | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/aircraft.yaml |
| cabal_hunterdronecarrier | 4 | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/aircraft.yaml |
| cabal_hunterkillermk1 | 4 | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/aircraft.yaml |
| cabal_hunterkillermk1_elite | 4 | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/aircraft.yaml |
| cabal_laserspider | 5 | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/vehicles.yaml |
| cabal_lazerboat | 4 | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/naval.yaml |
| cabal_lcraft | 4 | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/naval.yaml |
| cabal_manticore | 4 | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/vehicles.yaml |
| cabal_manticore_backup | 5 | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/husks.yaml |
| cabal_mantis | 5 | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/vehicles.yaml |
| cabal_mobileconstructionvehicle | 4 | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/vehicles.yaml |
| cabal_mothership | 4 | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/aircraft.yaml |
| cabal_navalyard | 4 | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/buildings.yaml |
| cabal_obeliskofdarkness | 4 | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/defenses.yaml |
| cabal_orbdrone | 4 | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/aircraft.yaml |
| cabal_orbdrone_slave | 5 | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/aircraft.yaml |
| cabal_overkillgunship | 4 | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/aircraft.yaml |
| cabal_pillbox | 5 | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/defenses.yaml |
| cabal_plasmasub | 4 | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/naval.yaml |
| cabal_plasmaturret | 4 | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/defenses.yaml |
| cabal_powerplant | 4 | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/buildings.yaml |
| cabal_radar | 4 | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/buildings.yaml |
| cabal_radar_cruiser | 4 | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/naval.yaml |
| cabal_ravager | 5 | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/vehicles.yaml |
| cabal_repairdrone | 4 | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/aircraft.yaml |
| cabal_rocketcyborg | 5 | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/infantry.yaml |
| cabal_scarabapc | 4 | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/vehicles.yaml |
| cabal_spidercnc4 | 5 | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/vehicles.yaml |
| cabal_tarantula | 5 | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/vehicles.yaml |
| cabal_tarantula_backup | 6 | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/husks.yaml |
| cabal_tiberiumharvester | 5 | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/vehicles.yaml |
| cabal_widow | 5 | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/vehicles.yaml |
| cabal_widow_backup | 6 | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/husks.yaml |
| car.nax | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/vehicles.yaml |
| carryall | 4 | mods/cameo/ContentPacks/D2k/Shared/yaml/aircraft.yaml |
| carryall.paradrop | 4 | mods/cameo/ContentPacks/D2k/Shared/yaml/aircraft.yaml |
| cgpnch.asian | 5 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/buildings.yaml |
| cgyard.asian | 4 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/buildings.yaml |
| cgyard.latin | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/buildings.yaml |
| cobra.steel | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/vehicles.yaml |
| combat_tank.atreides | 6 | mods/cameo/ContentPacks/D2k/Atreides/yaml/vehicles.yaml |
| combat_tank.harkonnen | 5 | mods/cameo/ContentPacks/D2k/Harkonnen/yaml/vehicles.yaml |
| conehead2.nax | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/infantry.yaml |
| cougar.steel | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/vehicles.yaml |
| cruiser_f.steel | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/aircraft.yaml |
| d2k_silo.atreides | 5 | mods/cameo/ContentPacks/D2k/Atreides/yaml/buildings.yaml |
| devastator | 5 | mods/cameo/ContentPacks/D2k/Harkonnen/yaml/vehicles.yaml |
| dieglocke_husk.nax2 | 4 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/aircraft.yaml |
| drone_husk.ixian | 4 | mods/cameo/ContentPacks/D2k/Ixian/yaml/aircraft.yaml |
| duelist_tank.ixian | 5 | mods/cameo/ContentPacks/D2k/Ixian/yaml/vehicles.yaml |
| engineer | 5 | mods/cameo/ContentPacks/D2k/Shared/yaml/infantry.yaml |
| farasha_drone_ixian | 4 | mods/cameo/ContentPacks/D2k/Ixian/yaml/aircraft.yaml |
| forgotten_apache | 4 | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/aircraft.yaml |
| forgotten_apctruck | 4 | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/vehicles.yaml |
| forgotten_bowler | 4 | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/vehicles.yaml |
| forgotten_brokenrattytankturret | 4 | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/defenses.yaml |
| forgotten_brokenscoopertankturret | 4 | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/defenses.yaml |
| forgotten_brokenwarriortankturret | 4 | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/defenses.yaml |
| forgotten_cannonboat | 4 | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/naval.yaml |
| forgotten_carryall | 4 | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/aircraft.yaml |
| forgotten_chemicalmammothtank | 4 | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/vehicles.yaml |
| forgotten_chemsprayinfantry | 5 | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/infantry.yaml |
| forgotten_chinook | 4 | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/aircraft.yaml |
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
| forgotten_juggerboat | 4 | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/naval.yaml |
| forgotten_juggerflakwall | 4 | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/defenses.yaml |
| forgotten_lcraft | 4 | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/naval.yaml |
| forgotten_locustbomber | 4 | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/aircraft.yaml |
| forgotten_m113adats | 4 | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/vehicles.yaml |
| forgotten_machineguntower | 4 | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/defenses.yaml |
| forgotten_missilevan | 5 | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/vehicles.yaml |
| forgotten_mlrs | 5 | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/vehicles.yaml |
| forgotten_mobileconstructionvehicle | 4 | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/vehicles.yaml |
| forgotten_mutant | 5 | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/infantry.yaml |
| forgotten_mutant_sp | 6 | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/infantry.yaml |
| forgotten_mutant_wild | 6 | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/infantry.yaml |
| forgotten_mutanthijacker | 5 | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/infantry.yaml |
| forgotten_mutantmortarman | 5 | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/infantry.yaml |
| forgotten_mutantsergeant | 5 | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/infantry.yaml |
| forgotten_mutantsniper | 6 | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/infantry.yaml |
| forgotten_mutantsniper_r4 | 7 | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/infantry.yaml |
| forgotten_mutantsniper_sp | 7 | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/infantry.yaml |
| forgotten_mutantsoldier | 5 | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/infantry.yaml |
| forgotten_mutantsoldier_sp | 6 | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/infantry.yaml |
| forgotten_navalyard | 4 | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/buildings.yaml |
| forgotten_nomadbarracks | 4 | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/vehicles.yaml |
| forgotten_radar | 4 | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/buildings.yaml |
| forgotten_raidercar | 4 | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/vehicles.yaml |
| forgotten_rattytank | 5 | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/vehicles.yaml |
| forgotten_rocketinfantry | 6 | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/infantry.yaml |
| forgotten_ruiner | 4 | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/vehicles.yaml |
| forgotten_runnershotgal | 5 | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/infantry.yaml |
| forgotten_scarabapc | 4 | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/vehicles.yaml |
| forgotten_scoopertank | 5 | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/vehicles.yaml |
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
| futuretech_engineer | 5 | mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/infantry.yaml |
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
| harkonnen_autogunturret | 5 | mods/cameo/ContentPacks/D2k/Harkonnen/yaml/buildings.yaml |
| harkonnen_barracks | 4 | mods/cameo/ContentPacks/D2k/Harkonnen/yaml/buildings.yaml |
| harkonnen_constructionyard | 5 | mods/cameo/ContentPacks/D2k/Harkonnen/yaml/buildings.yaml |
| harkonnen_flameturret | 5 | mods/cameo/ContentPacks/D2k/Harkonnen/yaml/buildings.yaml |
| harkonnen_heavyfactory | 4 | mods/cameo/ContentPacks/D2k/Harkonnen/yaml/buildings.yaml |
| harkonnen_hightechfactory | 5 | mods/cameo/ContentPacks/D2k/Harkonnen/yaml/buildings.yaml |
| harkonnen_ixresearchcenter | 5 | mods/cameo/ContentPacks/D2k/Harkonnen/yaml/buildings.yaml |
| harkonnen_lightfactory | 4 | mods/cameo/ContentPacks/D2k/Harkonnen/yaml/buildings.yaml |
| harkonnen_mobileconstructionvehicle | 5 | mods/cameo/ContentPacks/D2k/Harkonnen/yaml/vehicles.yaml |
| harkonnen_outpost | 4 | mods/cameo/ContentPacks/D2k/Harkonnen/yaml/buildings.yaml |
| harkonnen_palace | 5 | mods/cameo/ContentPacks/D2k/Harkonnen/yaml/buildings.yaml |
| harkonnen_refinery | 5 | mods/cameo/ContentPacks/D2k/Harkonnen/yaml/buildings.yaml |
| harkonnen_repairpad | 4 | mods/cameo/ContentPacks/D2k/Harkonnen/yaml/buildings.yaml |
| harkonnen_rocketturret | 5 | mods/cameo/ContentPacks/D2k/Harkonnen/yaml/buildings.yaml |
| harkonnen_starport | 6 | mods/cameo/ContentPacks/D2k/Harkonnen/yaml/buildings.yaml |
| harkonnen_storagesilo | 5 | mods/cameo/ContentPacks/D2k/Harkonnen/yaml/buildings.yaml |
| harkonnen_windtrap | 5 | mods/cameo/ContentPacks/D2k/Harkonnen/yaml/buildings.yaml |
| haunebu2_husk.nax2 | 4 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/aircraft.yaml |
| haunebu_husk.nax2 | 4 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/aircraft.yaml |
| heavy_inf.ixian | 5 | mods/cameo/ContentPacks/D2k/Ixian/yaml/infantry.yaml |
| heavy_rocket_raider.ixian | 4 | mods/cameo/ContentPacks/D2k/Ixian/yaml/vehicles.yaml |
| heavydrone_husk.ixian | 4 | mods/cameo/ContentPacks/D2k/Ixian/yaml/aircraft.yaml |
| hole_small.nax2 | 4 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/infantry.yaml |
| hummer.steel | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/vehicles.yaml |
| ixian_advancedheavyfactory | 5 | mods/cameo/ContentPacks/D2k/Ixian/yaml/buildings.yaml |
| ixian_airdrone | 4 | mods/cameo/ContentPacks/D2k/Ixian/yaml/aircraft.yaml |
| ixian_barracks | 4 | mods/cameo/ContentPacks/D2k/Ixian/yaml/buildings.yaml |
| ixian_constructionyard | 5 | mods/cameo/ContentPacks/D2k/Ixian/yaml/buildings.yaml |
| ixian_empbomber | 4 | mods/cameo/ContentPacks/D2k/Ixian/yaml/aircraft.yaml |
| ixian_farasha | 4 | mods/cameo/ContentPacks/D2k/Ixian/yaml/aircraft.yaml |
| ixian_gunturret | 5 | mods/cameo/ContentPacks/D2k/Ixian/yaml/buildings.yaml |
| ixian_heavykodatank | 5 | mods/cameo/ContentPacks/D2k/Ixian/yaml/vehicles.yaml |
| ixian_hightechfactory | 5 | mods/cameo/ContentPacks/D2k/Ixian/yaml/buildings.yaml |
| ixian_ixcombatsiege | 5 | mods/cameo/ContentPacks/D2k/Ixian/yaml/vehicles.yaml |
| ixian_ixmissiletank | 5 | mods/cameo/ContentPacks/D2k/Ixian/yaml/vehicles.yaml |
| ixian_ixprojector | 5 | mods/cameo/ContentPacks/D2k/Ixian/yaml/vehicles.yaml |
| ixian_ixresearchcenter | 5 | mods/cameo/ContentPacks/D2k/Ixian/yaml/buildings.yaml |
| ixian_ixsiegetank | 5 | mods/cameo/ContentPacks/D2k/Ixian/yaml/vehicles.yaml |
| ixian_kodatank | 5 | mods/cameo/ContentPacks/D2k/Ixian/yaml/vehicles.yaml |
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
| japan_archermaiden | 6 | mods/cameo/ContentPacks/RedAlert/Japan/yaml/infantry.yaml |
| japan_armoredcar | 4 | mods/cameo/ContentPacks/RedAlert/Japan/yaml/vehicles.yaml |
| japan_badger | 4 | mods/cameo/ContentPacks/RedAlert/Shared/yaml/aircraft.yaml |
| japan_ballista | 5 | mods/cameo/ContentPacks/RedAlert/Japan/yaml/vehicles.yaml |
| japan_ballistatower | 4 | mods/cameo/ContentPacks/RedAlert/Japan/yaml/defenses.yaml |
| japan_chihaheavytank | 5 | mods/cameo/ContentPacks/RedAlert/Japan/yaml/vehicles.yaml |
| japan_coreairfield | 4 | mods/cameo/ContentPacks/RedAlert/Japan/yaml/vehicles.yaml |
| japan_corebarracks | 4 | mods/cameo/ContentPacks/RedAlert/Japan/yaml/vehicles.yaml |
| japan_corepowerplant | 4 | mods/cameo/ContentPacks/RedAlert/Japan/yaml/vehicles.yaml |
| japan_coreradar | 4 | mods/cameo/ContentPacks/RedAlert/Japan/yaml/vehicles.yaml |
| japan_corerefinery | 4 | mods/cameo/ContentPacks/RedAlert/Japan/yaml/vehicles.yaml |
| japan_coreservicedepot | 4 | mods/cameo/ContentPacks/RedAlert/Japan/yaml/vehicles.yaml |
| japan_coretechcenter | 4 | mods/cameo/ContentPacks/RedAlert/Japan/yaml/vehicles.yaml |
| japan_exorcist | 5 | mods/cameo/ContentPacks/RedAlert/Japan/yaml/infantry.yaml |
| japan_exorcistoitank | 4 | mods/cameo/ContentPacks/RedAlert/Japan/yaml/vehicles.yaml |
| japan_grenadebuggy | 4 | mods/cameo/ContentPacks/RedAlert/Japan/yaml/vehicles.yaml |
| japan_hovercraftflametank | 5 | mods/cameo/ContentPacks/RedAlert/Japan/yaml/vehicles.yaml |
| japan_hovercrafttransport | 4 | mods/cameo/ContentPacks/RedAlert/Japan/yaml/vehicles.yaml |
| japan_igomediumtank | 5 | mods/cameo/ContentPacks/RedAlert/Japan/yaml/vehicles.yaml |
| japan_imperialscoutsman | 6 | mods/cameo/ContentPacks/RedAlert/Japan/yaml/infantry.yaml |
| japan_japanesebomber | 4 | mods/cameo/ContentPacks/RedAlert/Japan/yaml/aircraft.yaml |
| japan_japanesecarrier | 4 | mods/cameo/ContentPacks/RedAlert/Shared/yaml/naval.yaml |
| japan_japaneseconstructionyard | 4 | mods/cameo/ContentPacks/RedAlert/Japan/yaml/buildings.yaml |
| japan_japaneseflamethrower | 5 | mods/cameo/ContentPacks/RedAlert/Japan/yaml/infantry.yaml |
| japan_japanesemgnest | 4 | mods/cameo/ContentPacks/RedAlert/Japan/yaml/defenses.yaml |
| japan_japanesemobileconstructionvehicle | 5 | mods/cameo/ContentPacks/RedAlert/Japan/yaml/vehicles.yaml |
| japan_japaneseorerefinery | 4 | mods/cameo/ContentPacks/RedAlert/Japan/yaml/buildings.yaml |
| japan_japaneseoretruck | 5 | mods/cameo/ContentPacks/RedAlert/Japan/yaml/vehicles.yaml |
| japan_japaneseradararray | 4 | mods/cameo/ContentPacks/RedAlert/Japan/yaml/buildings.yaml |
| japan_japaneseservicedepot | 4 | mods/cameo/ContentPacks/RedAlert/Japan/yaml/buildings.yaml |
| japan_japaneseshrine | 5 | mods/cameo/ContentPacks/RedAlert/Japan/yaml/defenses.yaml |
| japan_japanesespeedboat | 4 | mods/cameo/ContentPacks/RedAlert/Shared/yaml/naval.yaml |
| japan_japanesesuperbomber | 4 | mods/cameo/ContentPacks/RedAlert/Shared/yaml/aircraft.yaml |
| japan_nanodronebuggy | 5 | mods/cameo/ContentPacks/RedAlert/Japan/yaml/vehicles.yaml |
| japan_oitank | 5 | mods/cameo/ContentPacks/RedAlert/Japan/yaml/vehicles.yaml |
| japan_rocketangel | 4 | mods/cameo/ContentPacks/RedAlert/Japan/yaml/infantry.yaml |
| japan_rocketangel_husk | 4 | mods/cameo/ContentPacks/RedAlert/Shared/yaml/vehicles.yaml |
| japan_samurai | 5 | mods/cameo/ContentPacks/RedAlert/Japan/yaml/infantry.yaml |
| japan_scoutcar | 4 | mods/cameo/ContentPacks/RedAlert/Japan/yaml/vehicles.yaml |
| japan_shogunexecutioner | 4 | mods/cameo/ContentPacks/RedAlert/Japan/yaml/vehicles.yaml |
| japan_shrineminitank | 5 | mods/cameo/ContentPacks/RedAlert/Japan/yaml/vehicles.yaml |
| japan_skyhawk | 4 | mods/cameo/ContentPacks/RedAlert/Japan/yaml/aircraft.yaml |
| japan_tankbuster | 5 | mods/cameo/ContentPacks/RedAlert/Japan/yaml/infantry.yaml |
| japan_waveforceartillery | 5 | mods/cameo/ContentPacks/RedAlert/Japan/yaml/vehicles.yaml |
| japan_waveforcereactor | 4 | mods/cameo/ContentPacks/RedAlert/Japan/yaml/buildings.yaml |
| japan_waveforcetank | 5 | mods/cameo/ContentPacks/RedAlert/Japan/yaml/vehicles.yaml |
| japan_waveforceturret | 4 | mods/cameo/ContentPacks/RedAlert/Japan/yaml/defenses.yaml |
| japan_yamatobattleship | 4 | mods/cameo/ContentPacks/RedAlert/Shared/yaml/naval.yaml |
| japan_zerofighter | 4 | mods/cameo/ContentPacks/RedAlert/Japan/yaml/aircraft.yaml |
| japan_zerofighter_slave | 4 | mods/cameo/ContentPacks/RedAlert/Shared/yaml/naval.yaml |
| jsuperbomber.Husk | 4 | mods/cameo/rules/husks.yaml |
| kami.asian | 4 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/aircraft.yaml |
| kami_asdf.asian | 5 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/aircraft.yaml |
| kami_chemical.asian | 5 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/aircraft.yaml |
| karrier.asian | 4 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/naval.yaml |
| ksub.asian | 4 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/naval.yaml |
| landcarr_drone.futu | 4 | mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/aircraft.yaml |
| latinsyndicate_airstation | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/buildings.yaml |
| latinsyndicate_bunkertower | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/buildings.yaml |
| latinsyndicate_burrito | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/vehicles.yaml |
| latinsyndicate_carteltruck | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/vehicles.yaml |
| latinsyndicate_collectiontruck | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/vehicles.yaml |
| latinsyndicate_combatbarracks | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/buildings.yaml |
| latinsyndicate_defensebureau | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/buildings.yaml |
| latinsyndicate_diablo | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/vehicles.yaml |
| latinsyndicate_engineer | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/infantry.yaml |
| latinsyndicate_freedomfighter | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/infantry.yaml |
| latinsyndicate_grenademonkey | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/infantry.yaml |
| latinsyndicate_hindtransport | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/aircraft.yaml |
| latinsyndicate_lars | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/vehicles.yaml |
| latinsyndicate_latinaadefender | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/buildings.yaml |
| latinsyndicate_latinapc | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/vehicles.yaml |
| latinsyndicate_latinempradar | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/buildings.yaml |
| latinsyndicate_latinflametrooper | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/infantry.yaml |
| latinsyndicate_latinmilitia | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/infantry.yaml |
| latinsyndicate_latinsentrygun | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/buildings.yaml |
| latinsyndicate_mig21 | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/aircraft.yaml |
| latinsyndicate_missiletruck | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/vehicles.yaml |
| latinsyndicate_mortarbike | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/vehicles.yaml |
| latinsyndicate_narco | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/infantry.yaml |
| latinsyndicate_narcohummer | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/vehicles.yaml |
| latinsyndicate_nuketruck | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/vehicles.yaml |
| latinsyndicate_powerstation | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/buildings.yaml |
| latinsyndicate_raiderbuggy | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/vehicles.yaml |
| latinsyndicate_recyclingcenter | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/buildings.yaml |
| latinsyndicate_recyclingrefinery | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/buildings.yaml |
| latinsyndicate_rushertank | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/vehicles.yaml |
| latinsyndicate_smlturret | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/buildings.yaml |
| latinsyndicate_smokertank | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/vehicles.yaml |
| latinsyndicate_spycenter | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/buildings.yaml |
| latinsyndicate_syndicateconstructionyard | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/buildings.yaml |
| latinsyndicate_syndicatefactory | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/buildings.yaml |
| latinsyndicate_syndicatemobileconstructionvehicle | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/vehicles.yaml |
| latinsyndicate_syndicateservicedepot | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/buildings.yaml |
| latinsyndicate_tankkiller | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/infantry.yaml |
| latinsyndicate_terrorist | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/infantry.yaml |
| latinsyndicate_topolm | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/vehicles.yaml |
| latinsyndicate_topolsilo | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/buildings.yaml |
| latinsyndicate_tortugatank | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/vehicles.yaml |
| latinsyndicate_yakovlev | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/aircraft.yaml |
| light_inf | 5 | mods/cameo/ContentPacks/D2k/Shared/yaml/infantry.yaml |
| lsub.asian | 4 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/naval.yaml |
| mammothbunker.husk | 4 | mods/cameo/rules/tech.yaml |
| missile_tank | 5 | mods/cameo/ContentPacks/D2k/Harkonnen/yaml/vehicles.yaml |
| modbomber.Husk | 4 | mods/cameo/rules/husks.yaml |
| modkami.Husk | 4 | mods/cameo/rules/husks.yaml |
| modkamimini.Husk | 4 | mods/cameo/rules/husks.yaml |
| muboat.nax | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/naval.yaml |
| naval.nax | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/buildings.yaml |
| nax_bitsmark | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/naval.yaml |
| naxis_academy | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/buildings.yaml |
| naxis_airfield | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/buildings.yaml |
| naxis_antitankcannon | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/infantry.yaml |
| naxis_barracks | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/buildings.yaml |
| naxis_beerfactory | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/buildings.yaml |
| naxis_bf109 | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/aircraft.yaml |
| naxis_bmwbike | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/infantry.yaml |
| naxis_brummbar | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/vehicles.yaml |
| naxis_coneheadsknights | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/infantry.yaml |
| naxis_constructionyard | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/buildings.yaml |
| naxis_donnerschlag | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/vehicles.yaml |
| naxis_engineeringtruck | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/vehicles.yaml |
| naxis_flak88 | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/buildings.yaml |
| naxis_grille | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/vehicles.yaml |
| naxis_halftrack | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/vehicles.yaml |
| naxis_hetzer | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/vehicles.yaml |
| naxis_imperialturbotank | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/vehicles.yaml |
| naxis_interceptor | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/aircraft.yaml |
| naxis_jagdpanzer | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/vehicles.yaml |
| naxis_kingtigerheavytank | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/vehicles.yaml |
| naxis_kubelwagen | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/vehicles.yaml |
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
| naxis_nokana | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/vehicles.yaml |
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
| naxis_slave | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/infantry.yaml |
| naxis_slaveoverseer | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/infantry.yaml |
| naxis_sssoldier | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/infantry.yaml |
| naxis_sturmtiger | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/vehicles.yaml |
| naxis_techcenter | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/buildings.yaml |
| naxis_transportzeppelin | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/aircraft.yaml |
| naxis_warfactory | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/buildings.yaml |
| nodlasercorvette | 4 | mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/naval.yaml |
| nodvenom.husk | 4 | mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/aircraft.yaml |
| oldqtnk.steel | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/vehicles.yaml |
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
| ordos_dustdrone | 4 | mods/cameo/ContentPacks/D2k/Ordos/yaml/vehicles.yaml |
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
| protoss_adept | 5 | mods/cameo/ContentPacks/StarCraft/Protoss/yaml/infantry.yaml |
| protoss_amaranth | 5 | mods/cameo/ContentPacks/StarCraft/Protoss/yaml/infantry.yaml |
| protoss_analogue | 6 | mods/cameo/ContentPacks/StarCraft/Protoss/yaml/vehicles.yaml |
| protoss_arbiter | 5 | mods/cameo/ContentPacks/StarCraft/Protoss/yaml/aircraft.yaml |
| protoss_arbitertribunal | 4 | mods/cameo/ContentPacks/StarCraft/Protoss/yaml/buildings.yaml |
| protoss_archon | 6 | mods/cameo/ContentPacks/StarCraft/Protoss/yaml/vehicles.yaml |
| protoss_assimilator | 4 | mods/cameo/ContentPacks/StarCraft/Protoss/yaml/buildings.yaml |
| protoss_atreus | 6 | mods/cameo/ContentPacks/StarCraft/Protoss/yaml/vehicles.yaml |
| protoss_carrier | 5 | mods/cameo/ContentPacks/StarCraft/Protoss/yaml/aircraft.yaml |
| protoss_citadelofadun | 4 | mods/cameo/ContentPacks/StarCraft/Protoss/yaml/buildings.yaml |
| protoss_corsair | 5 | mods/cameo/ContentPacks/StarCraft/Protoss/yaml/aircraft.yaml |
| protoss_cyberneticscore | 4 | mods/cameo/ContentPacks/StarCraft/Protoss/yaml/buildings.yaml |
| protoss_darktemplar | 5 | mods/cameo/ContentPacks/StarCraft/Protoss/yaml/infantry.yaml |
| protoss_dragoon | 6 | mods/cameo/ContentPacks/StarCraft/Protoss/yaml/vehicles.yaml |
| protoss_epigraph | 5 | mods/cameo/ContentPacks/StarCraft/Protoss/yaml/aircraft.yaml |
| protoss_fleetbeacon | 5 | mods/cameo/ContentPacks/StarCraft/Protoss/yaml/buildings.yaml |
| protoss_forge | 4 | mods/cameo/ContentPacks/StarCraft/Protoss/yaml/buildings.yaml |
| protoss_gateway | 4 | mods/cameo/ContentPacks/StarCraft/Protoss/yaml/buildings.yaml |
| protoss_gladius | 5 | mods/cameo/ContentPacks/StarCraft/Protoss/yaml/aircraft.yaml |
| protoss_hightemplar | 5 | mods/cameo/ContentPacks/StarCraft/Protoss/yaml/infantry.yaml |
| protoss_idol | 6 | mods/cameo/ContentPacks/StarCraft/Protoss/yaml/vehicles.yaml |
| protoss_legionnaire | 5 | mods/cameo/ContentPacks/StarCraft/Protoss/yaml/infantry.yaml |
| protoss_manifold | 6 | mods/cameo/ContentPacks/StarCraft/Protoss/yaml/vehicles.yaml |
| protoss_mobilenexus | 4 | mods/cameo/ContentPacks/StarCraft/Protoss/yaml/vehicles.yaml |
| protoss_nexus | 4 | mods/cameo/ContentPacks/StarCraft/Protoss/yaml/buildings.yaml |
| protoss_observatory | 4 | mods/cameo/ContentPacks/StarCraft/Protoss/yaml/buildings.yaml |
| protoss_observer | 5 | mods/cameo/ContentPacks/StarCraft/Protoss/yaml/aircraft.yaml |
| protoss_patriarch | 5 | mods/cameo/ContentPacks/StarCraft/Protoss/yaml/infantry.yaml |
| protoss_photoncannon | 4 | mods/cameo/ContentPacks/StarCraft/Protoss/yaml/defenses.yaml |
| protoss_positron | 6 | mods/cameo/ContentPacks/StarCraft/Protoss/yaml/vehicles.yaml |
| protoss_probe | 4 | mods/cameo/ContentPacks/StarCraft/Protoss/yaml/vehicles.yaml |
| protoss_pylon | 4 | mods/cameo/ContentPacks/StarCraft/Protoss/yaml/buildings.yaml |
| protoss_reaver | 6 | mods/cameo/ContentPacks/StarCraft/Protoss/yaml/vehicles.yaml |
| protoss_roboticsfacility | 4 | mods/cameo/ContentPacks/StarCraft/Protoss/yaml/buildings.yaml |
| protoss_roboticssupportbay | 4 | mods/cameo/ContentPacks/StarCraft/Protoss/yaml/buildings.yaml |
| protoss_scout | 5 | mods/cameo/ContentPacks/StarCraft/Protoss/yaml/aircraft.yaml |
| protoss_shieldbattery | 4 | mods/cameo/ContentPacks/StarCraft/Protoss/yaml/buildings.yaml |
| protoss_shuttle | 5 | mods/cameo/ContentPacks/StarCraft/Protoss/yaml/aircraft.yaml |
| protoss_stargate | 4 | mods/cameo/ContentPacks/StarCraft/Protoss/yaml/buildings.yaml |
| protoss_starshipsovereign | 5 | mods/cameo/ContentPacks/StarCraft/Protoss/yaml/aircraft.yaml |
| protoss_templararchives | 4 | mods/cameo/ContentPacks/StarCraft/Protoss/yaml/buildings.yaml |
| protoss_voidray | 5 | mods/cameo/ContentPacks/StarCraft/Protoss/yaml/aircraft.yaml |
| protoss_zealot | 5 | mods/cameo/ContentPacks/StarCraft/Protoss/yaml/infantry.yaml |
| protoss_zeratul | 5 | mods/cameo/ContentPacks/StarCraft/Protoss/yaml/infantry.yaml |
| ptnk.asian | 5 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/vehicles.yaml |
| quasfrig.asian | 4 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/naval.yaml |
| ra1_advancedpowerplant | 4 | mods/cameo/ContentPacks/RedAlert/Shared/yaml/buildings.yaml |
| ra1_agentdelphi | 5 | mods/cameo/ContentPacks/RedAlert/Shared/yaml/infantry.yaml |
| ra1_allies_alliedaagun | 4 | mods/cameo/ContentPacks/RedAlert/Allies/yaml/defenses.yaml |
| ra1_allies_alliedapc | 4 | mods/cameo/ContentPacks/RedAlert/Shared/yaml/vehicles.yaml |
| ra1_allies_alliedartillery | 5 | mods/cameo/ContentPacks/RedAlert/Allies/yaml/vehicles.yaml |
| ra1_allies_alliedchinooktransport | 5 | mods/cameo/ContentPacks/RedAlert/Allies/yaml/aircraft.yaml |
| ra1_allies_alliedchinooktransport_husk | 4 | mods/cameo/ContentPacks/RedAlert/Shared/yaml/vehicles.yaml |
| ra1_allies_alliedconstructionyard | 4 | mods/cameo/ContentPacks/RedAlert/Allies/yaml/buildings.yaml |
| ra1_allies_alliedcybertank | 5 | mods/cameo/ContentPacks/RedAlert/Shared/yaml/vehicles.yaml |
| ra1_allies_alliedgunturret | 4 | mods/cameo/ContentPacks/RedAlert/Allies/yaml/defenses.yaml |
| ra1_allies_alliedheavyaatank | 4 | mods/cameo/ContentPacks/RedAlert/Allies/yaml/vehicles.yaml |
| ra1_allies_alliedlighttank | 5 | mods/cameo/ContentPacks/RedAlert/Allies/yaml/vehicles.yaml |
| ra1_allies_alliedmediumtank | 5 | mods/cameo/ContentPacks/RedAlert/Allies/yaml/vehicles.yaml |
| ra1_allies_alliedmobileconstructionvehicle | 5 | mods/cameo/ContentPacks/RedAlert/Allies/yaml/vehicles.yaml |
| ra1_allies_alliedorerefinery | 4 | mods/cameo/ContentPacks/RedAlert/Allies/yaml/buildings.yaml |
| ra1_allies_alliedoretruck | 5 | mods/cameo/ContentPacks/RedAlert/Allies/yaml/vehicles.yaml |
| ra1_allies_alliedradardome | 4 | mods/cameo/ContentPacks/RedAlert/Allies/yaml/buildings.yaml |
| ra1_allies_alliedrocketsoldier | 6 | mods/cameo/ContentPacks/RedAlert/Shared/yaml/infantry.yaml |
| ra1_allies_alliedservicedepot | 4 | mods/cameo/ContentPacks/RedAlert/Allies/yaml/buildings.yaml |
| ra1_allies_alliedsniper | 5 | mods/cameo/ContentPacks/RedAlert/Allies/yaml/infantry.yaml |
| ra1_allies_alliedtankdestroyer | 5 | mods/cameo/ContentPacks/RedAlert/Allies/yaml/vehicles.yaml |
| ra1_allies_alliedtigerheavytank | 5 | mods/cameo/ContentPacks/RedAlert/Allies/yaml/vehicles.yaml |
| ra1_allies_badger | 4 | mods/cameo/ContentPacks/RedAlert/Shared/yaml/aircraft.yaml |
| ra1_allies_bastionartillerybunker | 4 | mods/cameo/ContentPacks/RedAlert/Allies/yaml/defenses.yaml |
| ra1_allies_blackhawk | 4 | mods/cameo/ContentPacks/RedAlert/Allies/yaml/aircraft.yaml |
| ra1_allies_camopillbox | 4 | mods/cameo/ContentPacks/RedAlert/Allies/yaml/defenses.yaml |
| ra1_allies_cargoplanebomber | 5 | mods/cameo/ContentPacks/RedAlert/Shared/yaml/aircraft.yaml |
| ra1_allies_cargoplaneparadrop | 4 | mods/cameo/ContentPacks/RedAlert/Shared/yaml/aircraft.yaml |
| ra1_allies_chronosphere | 5 | mods/cameo/ContentPacks/RedAlert/Allies/yaml/defenses.yaml |
| ra1_allies_chronotank | 4 | mods/cameo/ContentPacks/RedAlert/Allies/yaml/vehicles.yaml |
| ra1_allies_cruiser | 4 | mods/cameo/ContentPacks/RedAlert/Shared/yaml/naval.yaml |
| ra1_allies_destroyer | 4 | mods/cameo/ContentPacks/RedAlert/Shared/yaml/naval.yaml |
| ra1_allies_gapgenerator | 4 | mods/cameo/ContentPacks/RedAlert/Allies/yaml/defenses.yaml |
| ra1_allies_gunboat | 4 | mods/cameo/ContentPacks/RedAlert/Shared/yaml/naval.yaml |
| ra1_allies_longbow | 4 | mods/cameo/ContentPacks/RedAlert/Allies/yaml/aircraft.yaml |
| ra1_allies_machinegunner | 5 | mods/cameo/ContentPacks/RedAlert/Allies/yaml/infantry.yaml |
| ra1_allies_mechanic | 5 | mods/cameo/ContentPacks/RedAlert/Allies/yaml/infantry.yaml |
| ra1_allies_medic | 5 | mods/cameo/ContentPacks/RedAlert/Allies/yaml/infantry.yaml |
| ra1_allies_minelayer | 4 | mods/cameo/ContentPacks/RedAlert/Allies/yaml/vehicles.yaml |
| ra1_allies_mobilegapgenerator | 4 | mods/cameo/ContentPacks/RedAlert/Allies/yaml/vehicles.yaml |
| ra1_allies_mobileradarjammer | 4 | mods/cameo/ContentPacks/RedAlert/Allies/yaml/vehicles.yaml |
| ra1_allies_phasetransport | 4 | mods/cameo/ContentPacks/RedAlert/Allies/yaml/vehicles.yaml |
| ra1_allies_pillbox | 4 | mods/cameo/ContentPacks/RedAlert/Allies/yaml/defenses.yaml |
| ra1_allies_ranger | 4 | mods/cameo/ContentPacks/RedAlert/Allies/yaml/vehicles.yaml |
| ra1_allies_rapierjumpjet | 4 | mods/cameo/ContentPacks/RedAlert/Allies/yaml/aircraft.yaml |
| ra1_allies_raspy | 5 | mods/cameo/ContentPacks/RedAlert/Allies/yaml/infantry.yaml |
| ra1_allies_reconranger | 4 | mods/cameo/ContentPacks/RedAlert/Allies/yaml/vehicles.yaml |
| ra1_allies_reinforcementpad | 5 | mods/cameo/ContentPacks/RedAlert/Allies/yaml/buildings.yaml |
| ra1_allies_rifleinfantry | 6 | mods/cameo/ContentPacks/RedAlert/Shared/yaml/infantry.yaml |
| ra1_allies_sheridanassaulttank | 5 | mods/cameo/ContentPacks/RedAlert/Allies/yaml/vehicles.yaml |
| ra1_allies_tanya | 5 | mods/cameo/ContentPacks/RedAlert/Allies/yaml/infantry.yaml |
| ra1_badger_bomber | 4 | mods/cameo/ContentPacks/RedAlert/Shared/yaml/aircraft.yaml |
| ra1_einstein | 5 | mods/cameo/ContentPacks/RedAlert/Shared/yaml/infantry.yaml |
| ra1_engineer | 5 | mods/cameo/ContentPacks/RedAlert/Shared/yaml/infantry.yaml |
| ra1_general | 5 | mods/cameo/ContentPacks/RedAlert/Shared/yaml/infantry.yaml |
| ra1_navaltransport | 4 | mods/cameo/ContentPacks/RedAlert/Shared/yaml/naval.yaml |
| ra1_powerplant | 4 | mods/cameo/ContentPacks/RedAlert/Shared/yaml/buildings.yaml |
| ra1_scientist | 5 | mods/cameo/ContentPacks/RedAlert/Shared/yaml/infantry.yaml |
| ra1_soviets_ak47conscript | 5 | mods/cameo/ContentPacks/RedAlert/Soviets/yaml/infantry.yaml |
| ra1_soviets_armoredyak | 4 | mods/cameo/ContentPacks/RedAlert/Soviets/yaml/aircraft.yaml |
| ra1_soviets_attackdog | 5 | mods/cameo/ContentPacks/RedAlert/Soviets/yaml/infantry.yaml |
| ra1_soviets_badger | 4 | mods/cameo/ContentPacks/RedAlert/Shared/yaml/aircraft.yaml |
| ra1_soviets_btr80 | 4 | mods/cameo/ContentPacks/RedAlert/Soviets/yaml/vehicles.yaml |
| ra1_soviets_commissar | 5 | mods/cameo/ContentPacks/RedAlert/Soviets/yaml/infantry.yaml |
| ra1_soviets_constructionyard | 4 | mods/cameo/ContentPacks/RedAlert/Soviets/yaml/buildings.yaml |
| ra1_soviets_cyberdog | 5 | mods/cameo/ContentPacks/RedAlert/Soviets/yaml/infantry.yaml |
| ra1_soviets_dragunovantimaterialsniper | 5 | mods/cameo/ContentPacks/RedAlert/Soviets/yaml/infantry.yaml |
| ra1_soviets_firerocketsoldier | 6 | mods/cameo/ContentPacks/RedAlert/Soviets/yaml/infantry.yaml |
| ra1_soviets_flaktruck | 4 | mods/cameo/ContentPacks/RedAlert/Soviets/yaml/vehicles.yaml |
| ra1_soviets_flamethrower | 5 | mods/cameo/ContentPacks/RedAlert/Soviets/yaml/infantry.yaml |
| ra1_soviets_flametower | 4 | mods/cameo/ContentPacks/RedAlert/Soviets/yaml/defenses.yaml |
| ra1_soviets_gatlingtank | 4 | mods/cameo/ContentPacks/RedAlert/Soviets/yaml/vehicles.yaml |
| ra1_soviets_gorynychtank | 4 | mods/cameo/ContentPacks/RedAlert/Soviets/yaml/vehicles.yaml |
| ra1_soviets_grad | 5 | mods/cameo/ContentPacks/RedAlert/Soviets/yaml/vehicles.yaml |
| ra1_soviets_grenadier | 5 | mods/cameo/ContentPacks/RedAlert/Soviets/yaml/infantry.yaml |
| ra1_soviets_hammertank | 5 | mods/cameo/ContentPacks/RedAlert/Soviets/yaml/vehicles.yaml |
| ra1_soviets_heatraytank | 5 | mods/cameo/ContentPacks/RedAlert/Soviets/yaml/vehicles.yaml |
| ra1_soviets_heavyindustrialminer | 6 | mods/cameo/ContentPacks/RedAlert/Soviets/yaml/vehicles.yaml |
| ra1_soviets_heavytank | 5 | mods/cameo/ContentPacks/RedAlert/Soviets/yaml/vehicles.yaml |
| ra1_soviets_heavyteslatank | 5 | mods/cameo/ContentPacks/RedAlert/Soviets/yaml/vehicles.yaml |
| ra1_soviets_hindattackhelicopter | 4 | mods/cameo/ContentPacks/RedAlert/Soviets/yaml/aircraft.yaml |
| ra1_soviets_hiptransport | 5 | mods/cameo/ContentPacks/RedAlert/Soviets/yaml/aircraft.yaml |
| ra1_soviets_hiptransport_husk | 4 | mods/cameo/ContentPacks/RedAlert/Shared/yaml/vehicles.yaml |
| ra1_soviets_ironcurtain | 4 | mods/cameo/ContentPacks/RedAlert/Soviets/yaml/defenses.yaml |
| ra1_soviets_kamovattackhelicopter | 4 | mods/cameo/ContentPacks/RedAlert/Soviets/yaml/aircraft.yaml |
| ra1_soviets_kennel | 4 | mods/cameo/ContentPacks/RedAlert/Shared/yaml/buildings.yaml |
| ra1_soviets_kotinnucleartank | 5 | mods/cameo/ContentPacks/RedAlert/Soviets/yaml/vehicles.yaml |
| ra1_soviets_largefactory | 4 | mods/cameo/ContentPacks/RedAlert/Soviets/yaml/buildings.yaml |
| ra1_soviets_largesovietairfield | 4 | mods/cameo/ContentPacks/RedAlert/Soviets/yaml/buildings.yaml |
| ra1_soviets_madtank | 4 | mods/cameo/ContentPacks/RedAlert/Soviets/yaml/vehicles.yaml |
| ra1_soviets_mammothtank | 5 | mods/cameo/ContentPacks/RedAlert/Soviets/yaml/vehicles.yaml |
| ra1_soviets_mammothtank.colorpicker | 6 | mods/cameo/rules/misc.yaml |
| ra1_soviets_migattackbomber | 4 | mods/cameo/ContentPacks/RedAlert/Soviets/yaml/aircraft.yaml |
| ra1_soviets_missilesilo | 5 | mods/cameo/ContentPacks/RedAlert/Soviets/yaml/defenses.yaml |
| ra1_soviets_missilesubmarine | 4 | mods/cameo/ContentPacks/RedAlert/Shared/yaml/naval.yaml |
| ra1_soviets_mobileconstructionvehicle | 5 | mods/cameo/ContentPacks/RedAlert/Soviets/yaml/vehicles.yaml |
| ra1_soviets_molotovconscript | 5 | mods/cameo/ContentPacks/RedAlert/Shared/yaml/infantry.yaml |
| ra1_soviets_monstertank | 4 | mods/cameo/ContentPacks/RedAlert/Soviets/yaml/vehicles.yaml |
| ra1_soviets_mortarsoldier | 5 | mods/cameo/ContentPacks/RedAlert/Soviets/yaml/infantry.yaml |
| ra1_soviets_nuclearv2launcher | 5 | mods/cameo/ContentPacks/RedAlert/Soviets/yaml/vehicles.yaml |
| ra1_soviets_nuclearyak | 4 | mods/cameo/ContentPacks/RedAlert/Soviets/yaml/aircraft.yaml |
| ra1_soviets_orerefinery | 4 | mods/cameo/ContentPacks/RedAlert/Soviets/yaml/buildings.yaml |
| ra1_soviets_oretruck | 5 | mods/cameo/ContentPacks/RedAlert/Soviets/yaml/vehicles.yaml |
| ra1_soviets_radardome | 4 | mods/cameo/ContentPacks/RedAlert/Soviets/yaml/buildings.yaml |
| ra1_soviets_rifleinfantry | 6 | mods/cameo/ContentPacks/RedAlert/Soviets/yaml/infantry.yaml |
| ra1_soviets_rocketsoldier | 6 | mods/cameo/ContentPacks/RedAlert/Soviets/yaml/infantry.yaml |
| ra1_soviets_samsite | 4 | mods/cameo/ContentPacks/RedAlert/Soviets/yaml/defenses.yaml |
| ra1_soviets_servicedepot | 4 | mods/cameo/ContentPacks/RedAlert/Soviets/yaml/buildings.yaml |
| ra1_soviets_shocktrooper | 5 | mods/cameo/ContentPacks/RedAlert/Soviets/yaml/infantry.yaml |
| ra1_soviets_siegemammothtank | 5 | mods/cameo/ContentPacks/RedAlert/Soviets/yaml/vehicles.yaml |
| ra1_soviets_stalinfist | 5 | mods/cameo/ContentPacks/RedAlert/Soviets/yaml/vehicles.yaml |
| ra1_soviets_su57attackbomber | 4 | mods/cameo/ContentPacks/RedAlert/Soviets/yaml/aircraft.yaml |
| ra1_soviets_submarine | 4 | mods/cameo/ContentPacks/RedAlert/Shared/yaml/naval.yaml |
| ra1_soviets_supersonicnuclearbomber | 4 | mods/cameo/ContentPacks/RedAlert/Soviets/yaml/aircraft.yaml |
| ra1_soviets_superspyplane | 4 | mods/cameo/ContentPacks/RedAlert/Shared/yaml/aircraft.yaml |
| ra1_soviets_teslacoil | 4 | mods/cameo/ContentPacks/RedAlert/Soviets/yaml/defenses.yaml |
| ra1_soviets_teslatank | 5 | mods/cameo/ContentPacks/RedAlert/Soviets/yaml/vehicles.yaml |
| ra1_soviets_teslayak | 4 | mods/cameo/ContentPacks/RedAlert/Soviets/yaml/aircraft.yaml |
| ra1_soviets_v1rockettruck | 5 | mods/cameo/ContentPacks/RedAlert/Soviets/yaml/vehicles.yaml |
| ra1_soviets_v2rocketlauncher | 5 | mods/cameo/ContentPacks/RedAlert/Soviets/yaml/vehicles.yaml |
| ra1_soviets_volkov | 5 | mods/cameo/ContentPacks/RedAlert/Soviets/yaml/infantry.yaml |
| ra1_soviets_yakscoutplane | 4 | mods/cameo/ContentPacks/RedAlert/Soviets/yaml/aircraft.yaml |
| ra1_soviets_zapper | 5 | mods/cameo/ContentPacks/RedAlert/Soviets/yaml/infantry.yaml |
| ra1_technician | 5 | mods/cameo/ContentPacks/RedAlert/Shared/yaml/infantry.yaml |
| ra2_allies_aegiscruiser | 4 | mods/cameo/ContentPacks/RedAlert2/Allies/yaml/naval.yaml |
| ra2_allies_airforcecommandhq | 4 | mods/cameo/ContentPacks/RedAlert2/Allies/yaml/buildings.yaml |
| ra2_allies_alliedbarracks | 4 | mods/cameo/ContentPacks/RedAlert2/Allies/yaml/buildings.yaml |
| ra2_allies_alliedbattlelab | 4 | mods/cameo/ContentPacks/RedAlert2/Allies/yaml/buildings.yaml |
| ra2_allies_alliedconstructionyard | 4 | mods/cameo/ContentPacks/RedAlert2/Allies/yaml/buildings.yaml |
| ra2_allies_alliedmobileconstructionvehicle | 4 | mods/cameo/ContentPacks/RedAlert2/Allies/yaml/vehicles.yaml |
| ra2_allies_alliedorerefinery | 4 | mods/cameo/ContentPacks/RedAlert2/Allies/yaml/buildings.yaml |
| ra2_allies_alliedpowerplant | 4 | mods/cameo/ContentPacks/RedAlert2/Allies/yaml/buildings.yaml |
| ra2_allies_alliedservicedepot | 4 | mods/cameo/ContentPacks/RedAlert2/Allies/yaml/buildings.yaml |
| ra2_allies_alliedwarfactory | 5 | mods/cameo/ContentPacks/RedAlert2/Allies/yaml/buildings.yaml |
| ra2_allies_attackdog | 5 | mods/cameo/ContentPacks/RedAlert2/Allies/yaml/infantry.yaml |
| ra2_allies_battlefortress | 5 | mods/cameo/ContentPacks/RedAlert2/Allies/yaml/vehicles.yaml |
| ra2_allies_battlefortress_chrono | 6 | mods/cameo/ContentPacks/RedAlert2/Allies/yaml/vehicles.yaml |
| ra2_allies_battlefortress_empty | 6 | mods/cameo/ContentPacks/RedAlert2/Allies/yaml/vehicles.yaml |
| ra2_allies_blackeagle | 4 | mods/cameo/ContentPacks/RedAlert2/Allies/yaml/aircraft.yaml |
| ra2_allies_chronolegionnaire | 5 | mods/cameo/ContentPacks/RedAlert2/Allies/yaml/infantry.yaml |
| ra2_allies_chronominer | 4 | mods/cameo/ContentPacks/RedAlert2/Allies/yaml/vehicles.yaml |
| ra2_allies_chronosphere | 4 | mods/cameo/ContentPacks/RedAlert2/Allies/yaml/defenses.yaml |
| ra2_allies_engineer | 5 | mods/cameo/ContentPacks/RedAlert2/Allies/yaml/infantry.yaml |
| ra2_allies_gapgenerator | 5 | mods/cameo/ContentPacks/RedAlert2/Allies/yaml/defenses.yaml |
| ra2_allies_gi | 5 | mods/cameo/ContentPacks/RedAlert2/Allies/yaml/infantry.yaml |
| ra2_allies_grandcannon | 5 | mods/cameo/ContentPacks/RedAlert2/Allies/yaml/defenses.yaml |
| ra2_allies_grizzlytank | 5 | mods/cameo/ContentPacks/RedAlert2/Allies/yaml/vehicles.yaml |
| ra2_allies_guardiangi | 5 | mods/cameo/ContentPacks/RedAlert2/Allies/yaml/infantry.yaml |
| ra2_allies_harrier | 4 | mods/cameo/ContentPacks/RedAlert2/Allies/yaml/aircraft.yaml |
| ra2_allies_heavymiragetank | 5 | mods/cameo/ContentPacks/RedAlert2/Allies/yaml/vehicles.yaml |
| ra2_allies_ifv | 5 | mods/cameo/ContentPacks/RedAlert2/Allies/yaml/vehicles.yaml |
| ra2_allies_ifv_chrono | 7 | mods/cameo/ContentPacks/RedAlert2/Allies/yaml/vehicles.yaml |
| ra2_allies_ifv_hmg | 6 | mods/cameo/ContentPacks/RedAlert2/Allies/yaml/vehicles.yaml |
| ra2_allies_ifv_mg | 6 | mods/cameo/ContentPacks/RedAlert2/Allies/yaml/vehicles.yaml |
| ra2_allies_ifv_missile | 6 | mods/cameo/ContentPacks/RedAlert2/Allies/yaml/vehicles.yaml |
| ra2_allies_ifv_repair | 7 | mods/cameo/ContentPacks/RedAlert2/Allies/yaml/vehicles.yaml |
| ra2_allies_miragetank | 5 | mods/cameo/ContentPacks/RedAlert2/Allies/yaml/vehicles.yaml |
| ra2_allies_nighthawk | 4 | mods/cameo/ContentPacks/RedAlert2/Allies/yaml/aircraft.yaml |
| ra2_allies_orepurifier | 5 | mods/cameo/ContentPacks/RedAlert2/Allies/yaml/buildings.yaml |
| ra2_allies_patriotmissilesystem | 5 | mods/cameo/ContentPacks/RedAlert2/Allies/yaml/defenses.yaml |
| ra2_allies_pillbox | 5 | mods/cameo/ContentPacks/RedAlert2/Allies/yaml/defenses.yaml |
| ra2_allies_prismtank | 5 | mods/cameo/ContentPacks/RedAlert2/Allies/yaml/vehicles.yaml |
| ra2_allies_prismtower | 5 | mods/cameo/ContentPacks/RedAlert2/Allies/yaml/defenses.yaml |
| ra2_allies_ra2spy | 5 | mods/cameo/ContentPacks/RedAlert2/Allies/yaml/infantry.yaml |
| ra2_allies_rocketeer | 4 | mods/cameo/ContentPacks/RedAlert2/Allies/yaml/infantry.yaml |
| ra2_allies_seal | 5 | mods/cameo/ContentPacks/RedAlert2/Allies/yaml/infantry.yaml |
| ra2_allies_sniper | 5 | mods/cameo/ContentPacks/RedAlert2/Allies/yaml/infantry.yaml |
| ra2_allies_spysatelliteuplink | 4 | mods/cameo/ContentPacks/RedAlert2/Allies/yaml/buildings.yaml |
| ra2_allies_tankdestroyer | 5 | mods/cameo/ContentPacks/RedAlert2/Allies/yaml/vehicles.yaml |
| ra2_allies_tanyaii | 5 | mods/cameo/ContentPacks/RedAlert2/Allies/yaml/infantry.yaml |
| ra2_allies_weathercontrolcenter | 5 | mods/cameo/ContentPacks/RedAlert2/Allies/yaml/defenses.yaml |
| ra2_ambu | 4 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ambu_demo | 4 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ambu_driveby | 5 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_bcab | 4 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_bcab_demo | 4 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_bcab_driveby | 5 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_bus | 4 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_bus_demo | 4 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_bus_driveby | 5 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_c_abram | 5 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_c_hum | 4 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_c_hum2 | 5 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_c_ifv | 5 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_car | 4 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_car_demo | 4 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_car_driveby | 5 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_cgcloa.husk | 4 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_city01 | 6 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_city02 | 6 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_city03 | 6 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_city04 | 6 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_city06 | 6 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_cona | 4 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_cona_demo | 4 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_cona_driveby | 5 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_cop | 4 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_cop_demo | 4 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_cop_driveby | 5 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ctfrmb | 5 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ctgard01 | 5 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ctgard03 | 5 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ctnwy09 | 6 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ctnwy22 | 6 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ctnwy23 | 6 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ctnwy24 | 6 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ctnwy25 | 6 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ctoutp.husk | 4 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ctpars02 | 6 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ctpars04 | 6 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ctpars05 | 6 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ctpars06 | 6 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ctpars07 | 5 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ctpars08 | 6 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ctpars09 | 6 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ctpars10 | 6 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ctpars12 | 6 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ctpars13 | 6 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ctpars14 | 6 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ctrus03 | 6 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ctrus04 | 6 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ctrus05 | 6 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ctrus06 | 6 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ctrus07 | 6 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ctrus08 | 6 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ctrus09 | 6 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ctrus10 | 6 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ctrus11 | 6 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ctsanf01 | 6 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ctsanf02 | 6 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ctsanf03 | 6 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ctsanf05 | 6 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ctsanf06 | 6 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ctsanf07 | 6 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ctsanf08 | 6 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ctsanf16 | 6 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ctsanf17 | 6 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ctsanf18 | 6 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_cttexs01 | 6 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_cttexs02 | 6 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_cttexs03 | 6 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_cttexs04 | 6 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_cttexs05 | 6 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_cttexs06 | 6 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_cttexs07 | 6 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_cttexs08 | 6 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ctwash03 | 6 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ctwash04 | 6 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ctwash05 | 6 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ctwash06 | 6 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ctwash07 | 6 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ctwash08 | 6 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ctwash09 | 6 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ctwash10 | 6 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ctwash11 | 6 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ctwash13 | 6 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ctwash17 | 6 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ddbx | 4 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ddbx_demo | 4 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ddbx_driveby | 5 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_euroc | 4 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_euroc_demo | 4 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_euroc_driveby | 5 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_jeep | 4 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_jeep_demo | 4 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_jeep_driveby | 5 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_limo | 4 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_limo_demo | 4 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_limo_driveby | 5 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ptruck | 4 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ptruck_demo | 4 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ptruck_driveby | 5 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_soviets_airfield | 4 | mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/buildings.yaml |
| ra2_soviets_apocalypsetank | 5 | mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/vehicles.yaml |
| ra2_soviets_attackdog | 5 | mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/infantry.yaml |
| ra2_soviets_barracks | 4 | mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/buildings.yaml |
| ra2_soviets_battlebunker | 5 | mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/defenses.yaml |
| ra2_soviets_battlelab | 4 | mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/buildings.yaml |
| ra2_soviets_boris | 5 | mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/infantry.yaml |
| ra2_soviets_conscript | 5 | mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/infantry.yaml |
| ra2_soviets_constructionyard | 4 | mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/buildings.yaml |
| ra2_soviets_crazyivan | 5 | mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/infantry.yaml |
| ra2_soviets_desolator | 5 | mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/infantry.yaml |
| ra2_soviets_engineer | 5 | mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/infantry.yaml |
| ra2_soviets_flakcannon | 5 | mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/defenses.yaml |
| ra2_soviets_flaktrack | 4 | mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/vehicles.yaml |
| ra2_soviets_flaktrooper | 5 | mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/infantry.yaml |
| ra2_soviets_industrialplant | 5 | mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/buildings.yaml |
| ra2_soviets_ironcurtain | 4 | mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/defenses.yaml |
| ra2_soviets_kirovairship | 4 | mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/aircraft.yaml |
| ra2_soviets_migbomber | 4 | mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/aircraft.yaml |
| ra2_soviets_mobileconstructionvehicle | 4 | mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/vehicles.yaml |
| ra2_soviets_nuclearmissilesilo | 5 | mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/defenses.yaml |
| ra2_soviets_nuclearreactor | 4 | mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/buildings.yaml |
| ra2_soviets_orerefinery | 4 | mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/buildings.yaml |
| ra2_soviets_radar | 4 | mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/buildings.yaml |
| ra2_soviets_rhinoheavytank | 5 | mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/vehicles.yaml |
| ra2_soviets_seascorpion | 4 | mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/naval.yaml |
| ra2_soviets_sentrygun | 5 | mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/defenses.yaml |
| ra2_soviets_servicedepot | 4 | mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/buildings.yaml |
| ra2_soviets_siegechopper | 4 | mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/aircraft.yaml |
| ra2_soviets_terrordrone | 4 | mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/vehicles.yaml |
| ra2_soviets_teslacoil | 5 | mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/defenses.yaml |
| ra2_soviets_teslareactor | 4 | mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/buildings.yaml |
| ra2_soviets_teslatank | 5 | mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/vehicles.yaml |
| ra2_soviets_teslatrooper | 5 | mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/infantry.yaml |
| ra2_soviets_transportkirov | 4 | mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/aircraft.yaml |
| ra2_soviets_v3rocketlauncher | 5 | mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/vehicles.yaml |
| ra2_soviets_warfactory | 5 | mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/buildings.yaml |
| ra2_soviets_warminer | 4 | mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/vehicles.yaml |
| ra2_stang | 4 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_stang_demo | 4 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_stang_driveby | 5 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_suvb | 4 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_suvb_demo | 4 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_suvb_driveby | 5 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_suvw | 4 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_suvw_demo | 4 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_suvw_driveby | 5 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_taxi | 4 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_taxi_demo | 4 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_taxi_driveby | 5 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_tractor | 4 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_tractor_demo | 4 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_tractor_driveby | 5 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_trucka | 4 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_trucka_demo | 4 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_trucka_driveby | 5 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_truckb | 4 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_truckb_demo | 4 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_truckb_driveby | 5 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ycab | 4 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ycab_demo | 4 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2_ycab_driveby | 5 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2asw | 4 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2caairp.husk | 4 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2caairpv | 4 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2caairpv.husk | 4 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2caoild.husk | 4 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2carrier | 4 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2cpower.husk | 4 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2ctarmy01 | 5 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2ctarmy02 | 5 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2ctarmy03 | 6 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2ctarmy04 | 6 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2ctbarn02 | 5 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2ctbunk01 | 4 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2ctbunk02 | 5 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2ctchig01 | 5 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2ctchig02 | 6 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2ctchig03 | 6 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2cteur01 | 5 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2cteur02 | 5 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2cteur04 | 6 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2ctfarm01 | 5 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2ctfarm06 | 5 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2ctfrma | 5 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2ctgas01 | 6 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2cthse01 | 5 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2cthse02 | 5 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2cthse03 | 6 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2cthse04 | 5 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2cthse05 | 5 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2cthse06 | 6 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2cthse07 | 5 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2ctind01 | 6 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2ctlab | 6 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2ctmiam01 | 6 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2ctmiam02 | 6 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2ctmiam03 | 6 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2ctmiam04 | 5 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2ctmiam05 | 6 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2ctmiam06 | 7 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2ctmiam07 | 7 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2ctmiam08 | 5 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2ctmsc07 | 5 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2ctmsc08 | 6 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2ctmsc09 | 7 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2ctmsc10 | 6 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2ctnewy01 | 6 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2ctnewy06 | 6 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2ctnewy07 | 6 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2ctnewy08 | 6 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2ctnewy10 | 6 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2ctnewy11 | 6 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2ctnewy12 | 7 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2ctnewy13 | 7 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2ctnewy14 | 7 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2ctnewy15 | 7 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2ctnewy16 | 7 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2ctnewy17 | 8 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2ctnewy18 | 9 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2ctnewy20 | 6 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2ctnewy21 | 7 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2ctnewy26 | 6 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2dest | 4 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2dlph | 4 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2dred | 4 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2e2.black | 5 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2gayard | 4 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2hind_husk.latin | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/aircraft.yaml |
| ra2hornet | 4 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2hospt.husk | 4 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2lcrf | 4 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2leopard | 5 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2machshop.husk | 4 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2nayard | 4 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2sapc | 4 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2shk.bot | 5 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2shkhero | 5 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2sidewind | 5 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2sqd | 4 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2sub | 4 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
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
| schwarzermond_airfield | 5 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/buildings.yaml |
| schwarzermond_barracks | 4 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/buildings.yaml |
| schwarzermond_blackbomb | 4 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/aircraft.yaml |
| schwarzermond_constructionyard | 4 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/buildings.yaml |
| schwarzermond_corruptorpiercer | 4 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/aircraft.yaml |
| schwarzermond_crystaltank | 5 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/vehicles.yaml |
| schwarzermond_dalek | 4 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/vehicles.yaml |
| schwarzermond_dieglocke | 4 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/aircraft.yaml |
| schwarzermond_drone | 4 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/aircraft.yaml |
| schwarzermond_engineeringarmor | 4 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/infantry.yaml |
| schwarzermond_gravitycore | 5 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/buildings.yaml |
| schwarzermond_gravitycoretank | 4 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/vehicles.yaml |
| schwarzermond_haunebuii | 4 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/aircraft.yaml |
| schwarzermond_haunebuiii | 4 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/aircraft.yaml |
| schwarzermond_hydrogenplant | 4 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/buildings.yaml |
| schwarzermond_komet | 4 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/vehicles.yaml |
| schwarzermond_korruptesbiest | 5 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/vehicles.yaml |
| schwarzermond_laserbeetle | 4 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/vehicles.yaml |
| schwarzermond_lasertank | 4 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/vehicles.yaml |
| schwarzermond_lasertower | 5 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/buildings.yaml |
| schwarzermond_lunargrille | 5 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/vehicles.yaml |
| schwarzermond_lunarpanzer | 5 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/vehicles.yaml |
| schwarzermond_lunarrocket | 5 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/infantry.yaml |
| schwarzermond_lunarsoldier | 5 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/infantry.yaml |
| schwarzermond_lunartiger | 5 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/vehicles.yaml |
| schwarzermond_mars | 5 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/vehicles.yaml |
| schwarzermond_meteortractionray | 5 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/buildings.yaml |
| schwarzermond_moondairyfarm | 4 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/buildings.yaml |
| schwarzermond_naxismobileconstructionvehicle | 4 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/vehicles.yaml |
| schwarzermond_neojagdpanzer | 5 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/vehicles.yaml |
| schwarzermond_noidharvester | 4 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/infantry.yaml |
| schwarzermond_noidmgarmor | 4 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/infantry.yaml |
| schwarzermond_orerefinery | 4 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/buildings.yaml |
| schwarzermond_parzival | 5 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/infantry.yaml |
| schwarzermond_radar | 4 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/buildings.yaml |
| schwarzermond_spacezeppelin | 4 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/aircraft.yaml |
| schwarzermond_sturmcannon | 5 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/buildings.yaml |
| schwarzermond_techcenter | 4 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/buildings.yaml |
| schwarzermond_ubermensch | 5 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/infantry.yaml |
| schwarzermond_warfactory | 5 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/buildings.yaml |
| scrapcar.latin | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/vehicles.yaml |
| scrapcar2.latin | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/vehicles.yaml |
| scrapcar2_demo.latin | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/vehicles.yaml |
| scrapcar2_driveby.latin | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/vehicles.yaml |
| scrapcar_demo.latin | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/vehicles.yaml |
| scrapcar_driveby.latin | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/vehicles.yaml |
| siege_tank | 5 | mods/cameo/ContentPacks/D2k/Shared/yaml/vehicles.yaml |
| sietch_creep | 4 | mods/cameo/ContentPacks/D2k/Shared/yaml/buildings.yaml |
| sietch_creep_disabled | 4 | mods/cameo/ContentPacks/D2k/Shared/yaml/buildings.yaml |
| ssmsub | 4 | mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/naval.yaml |
| steelconsortium_antiairquantummissileturret | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/buildings.yaml |
| steelconsortium_barracuda | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/vehicles.yaml |
| steelconsortium_bfg10000 | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/buildings.yaml |
| steelconsortium_cargoship | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/aircraft.yaml |
| steelconsortium_clonetrooper | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/infantry.yaml |
| steelconsortium_cloningvats | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/buildings.yaml |
| steelconsortium_cloudbreaker | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/aircraft.yaml |
| steelconsortium_consortiumairpad | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/buildings.yaml |
| steelconsortium_consortiumbattlelab | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/buildings.yaml |
| steelconsortium_consortiumconstructionyard | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/buildings.yaml |
| steelconsortium_consortiumminer | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/vehicles.yaml |
| steelconsortium_consortiummobileconstructionvehicle | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/vehicles.yaml |
| steelconsortium_consortiumpowerplant | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/buildings.yaml |
| steelconsortium_consortiumradar | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/buildings.yaml |
| steelconsortium_consortiumrefinery | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/buildings.yaml |
| steelconsortium_consortiumsentryturret | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/buildings.yaml |
| steelconsortium_consortiumwarfactory | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/buildings.yaml |
| steelconsortium_dagger | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/vehicles.yaml |
| steelconsortium_defenderbot | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/vehicles.yaml |
| steelconsortium_empressstation | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/aircraft.yaml |
| steelconsortium_engineer | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/infantry.yaml |
| steelconsortium_geothermalreactor | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/buildings.yaml |
| steelconsortium_hammerheadartillerytank | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/vehicles.yaml |
| steelconsortium_hoverboardgrenadier | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/infantry.yaml |
| steelconsortium_katytank | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/vehicles.yaml |
| steelconsortium_mako | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/vehicles.yaml |
| steelconsortium_manta | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/vehicles.yaml |
| steelconsortium_megalodon | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/vehicles.yaml |
| steelconsortium_orbitalcannonactivator | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/buildings.yaml |
| steelconsortium_poseidontank | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/vehicles.yaml |
| steelconsortium_quantumcannon | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/buildings.yaml |
| steelconsortium_quantummissiletrooper | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/infantry.yaml |
| steelconsortium_quantumtank | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/vehicles.yaml |
| steelconsortium_skyhammer | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/aircraft.yaml |
| steelconsortium_stalker | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/vehicles.yaml |
| steelconsortium_steelbarracks | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/buildings.yaml |
| steelconsortium_steelrunner | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/infantry.yaml |
| steelconsortium_supportshieldgenerator | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/vehicles.yaml |
| steelconsortium_twister | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/aircraft.yaml |
| steelconsortium_whiterabbit | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/vehicles.yaml |
| sub.latin | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/naval.yaml |
| td_gdi_advancedcommunicationscenter | 5 | mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/buildings.yaml |
| td_gdi_advancedguardtower | 5 | mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/buildings.yaml |
| td_gdi_apc | 4 | mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/vehicles.yaml |
| td_gdi_archerartillery | 5 | mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/vehicles.yaml |
| td_gdi_assaultapc | 5 | mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/vehicles.yaml |
| td_gdi_barracks | 4 | mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/buildings.yaml |
| td_gdi_battletank | 5 | mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/vehicles.yaml |
| td_gdi_boxer | 4 | mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/vehicles.yaml |
| td_gdi_chinooktransport | 5 | mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/aircraft.yaml |
| td_gdi_commando | 5 | mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/infantry.yaml |
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
| td_gdi_mlrs | 4 | mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/vehicles.yaml |
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
| td_nod_chinooktransport | 5 | mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/aircraft.yaml |
| td_nod_commando | 5 | mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/infantry.yaml |
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
| td_nod_stealthtank | 4 | mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/vehicles.yaml |
| td_nod_templeofnod | 5 | mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/buildings.yaml |
| td_nod_templeprime | 5 | mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/buildings.yaml |
| td_nod_tiberiumharvester | 5 | mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/vehicles.yaml |
| td_nod_tiberiumrefinery | 5 | mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/buildings.yaml |
| td_nod_venom | 4 | mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/aircraft.yaml |
| terran_battlecruiser | 4 | mods/cameo/ContentPacks/StarCraft/Terran/yaml/aircraft.yaml |
| terran_bunker | 4 | mods/cameo/ContentPacks/StarCraft/Terran/yaml/defenses.yaml |
| terran_commandcenter | 4 | mods/cameo/ContentPacks/StarCraft/Terran/yaml/buildings.yaml |
| terran_cyclone | 4 | mods/cameo/ContentPacks/StarCraft/Terran/yaml/vehicles.yaml |
| terran_dropship | 5 | mods/cameo/ContentPacks/StarCraft/Terran/yaml/aircraft.yaml |
| terran_firebat | 5 | mods/cameo/ContentPacks/StarCraft/Terran/yaml/infantry.yaml |
| terran_ghost | 5 | mods/cameo/ContentPacks/StarCraft/Terran/yaml/infantry.yaml |
| terran_goliath | 5 | mods/cameo/ContentPacks/StarCraft/Terran/yaml/vehicles.yaml |
| terran_goliathmk2 | 5 | mods/cameo/ContentPacks/StarCraft/Terran/yaml/vehicles.yaml |
| terran_harakan | 5 | mods/cameo/ContentPacks/StarCraft/Terran/yaml/infantry.yaml |
| terran_jimraynor | 5 | mods/cameo/ContentPacks/StarCraft/Terran/yaml/infantry.yaml |
| terran_madcap | 5 | mods/cameo/ContentPacks/StarCraft/Terran/yaml/infantry.yaml |
| terran_marauder | 5 | mods/cameo/ContentPacks/StarCraft/Terran/yaml/infantry.yaml |
| terran_marine | 5 | mods/cameo/ContentPacks/StarCraft/Terran/yaml/infantry.yaml |
| terran_matador | 5 | mods/cameo/ContentPacks/StarCraft/Terran/yaml/vehicles.yaml |
| terran_medic | 5 | mods/cameo/ContentPacks/StarCraft/Terran/yaml/infantry.yaml |
| terran_medivac | 5 | mods/cameo/ContentPacks/StarCraft/Terran/yaml/aircraft.yaml |
| terran_missilesilo | 5 | mods/cameo/ContentPacks/StarCraft/Terran/yaml/defenses.yaml |
| terran_missileturret | 4 | mods/cameo/ContentPacks/StarCraft/Terran/yaml/defenses.yaml |
| terran_mobilecommandcenter | 4 | mods/cameo/ContentPacks/StarCraft/Terran/yaml/vehicles.yaml |
| terran_phobos | 4 | mods/cameo/ContentPacks/StarCraft/Terran/yaml/aircraft.yaml |
| terran_pythean | 4 | mods/cameo/ContentPacks/StarCraft/Terran/yaml/aircraft.yaml |
| terran_raven | 4 | mods/cameo/ContentPacks/StarCraft/Terran/yaml/aircraft.yaml |
| terran_reaper | 5 | mods/cameo/ContentPacks/StarCraft/Terran/yaml/infantry.yaml |
| terran_sciencevessel | 4 | mods/cameo/ContentPacks/StarCraft/Terran/yaml/aircraft.yaml |
| terran_scv | 4 | mods/cameo/ContentPacks/StarCraft/Terran/yaml/vehicles.yaml |
| terran_sentinel | 4 | mods/cameo/ContentPacks/StarCraft/Terran/yaml/defenses.yaml |
| terran_siegetank | 5 | mods/cameo/ContentPacks/StarCraft/Terran/yaml/vehicles.yaml |
| terran_specter | 5 | mods/cameo/ContentPacks/StarCraft/Terran/yaml/infantry.yaml |
| terran_sundog | 4 | mods/cameo/ContentPacks/StarCraft/Terran/yaml/aircraft.yaml |
| terran_supplydepot | 4 | mods/cameo/ContentPacks/StarCraft/Terran/yaml/buildings.yaml |
| terran_valkyrie | 4 | mods/cameo/ContentPacks/StarCraft/Terran/yaml/aircraft.yaml |
| terran_vulture | 5 | mods/cameo/ContentPacks/StarCraft/Terran/yaml/vehicles.yaml |
| terran_warhound | 5 | mods/cameo/ContentPacks/StarCraft/Terran/yaml/vehicles.yaml |
| terran_wraith | 4 | mods/cameo/ContentPacks/StarCraft/Terran/yaml/aircraft.yaml |
| terran_wyvern | 4 | mods/cameo/ContentPacks/StarCraft/Terran/yaml/aircraft.yaml |
| tiger.nax | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/vehicles.yaml |
| tkm_abrams | 5 | mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/vehicles.yaml |
| tkm_as42 | 4 | mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/vehicles.yaml |
| tkm_barracks | 4 | mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/buildings.yaml |
| tkm_battlebus | 5 | mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/vehicles.yaml |
| tkm_bigshiee | 4 | mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/vehicles.yaml |
| tkm_bunker | 4 | mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/defenses.yaml |
| tkm_dronepodtruck | 5 | mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/vehicles.yaml |
| tkm_engineer | 5 | mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/infantry.yaml |
| tkm_flakbus | 4 | mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/vehicles.yaml |
| tkm_iroquois | 4 | mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/aircraft.yaml |
| tkm_juggernaut | 5 | mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/infantry.yaml |
| tkm_marine | 5 | mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/infantry.yaml |
| tkm_medictruck | 4 | mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/vehicles.yaml |
| tkm_mobileconstructionvehicletkm | 4 | mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/vehicles.yaml |
| tkm_observationvan | 4 | mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/buildings.yaml |
| tkm_orerefinery | 4 | mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/buildings.yaml |
| tkm_powerplant | 4 | mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/buildings.yaml |
| tkm_quadtruck | 4 | mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/vehicles.yaml |
| tkm_quadturretbunker | 4 | mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/defenses.yaml |
| tkm_radartruck | 4 | mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/vehicles.yaml |
| tkm_repairtruck | 4 | mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/vehicles.yaml |
| tkm_rifleman | 5 | mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/infantry.yaml |
| tkm_rocketeer | 5 | mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/infantry.yaml |
| tkm_sandmarine | 4 | mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/vehicles.yaml |
| tkm_sniper | 5 | mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/infantry.yaml |
| tkm_spetsnaz | 5 | mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/infantry.yaml |
| tkm_stryker | 4 | mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/vehicles.yaml |
| tkm_t30 | 4 | mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/vehicles.yaml |
| tkm_t72m | 5 | mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/vehicles.yaml |
| tkm_tankturretbunker | 4 | mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/defenses.yaml |
| tkm_technical | 4 | mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/vehicles.yaml |
| tkm_technicaltank | 5 | mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/vehicles.yaml |
| tkm_templateharvesterraname | 4 | mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/vehicles.yaml |
| tkm_thermonaut | 5 | mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/infantry.yaml |
| tkm_tornadoglauncher | 5 | mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/vehicles.yaml |
| tkm_trenchtank | 5 | mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/vehicles.yaml |
| tkm_trenchtruck | 4 | mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/vehicles.yaml |
| tkm_trooper | 5 | mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/infantry.yaml |
| tkm_viper | 4 | mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/aircraft.yaml |
| tkm_von | 5 | mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/infantry.yaml |
| tkm_zaza | 4 | mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/vehicles.yaml |
| tkmdrone | 4 | mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/aircraft.yaml |
| tkmhuey.husk | 4 | mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/husks.yaml |
| tkmratflakdeployed | 4 | mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/defenses.yaml |
| tkmsuicidedrone | 4 | mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/aircraft.yaml |
| tkmtrenchtankdeployed | 4 | mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/defenses.yaml |
| tkmvan | 5 | mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/infantry.yaml |
| tkmworker | 5 | mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/infantry.yaml |
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
| ts_gdi_hovermlrs | 4 | mods/cameo/ContentPacks/TiberianSun/GDI/yaml/vehicles.yaml |
| ts_gdi_juggernaut | 5 | mods/cameo/ContentPacks/TiberianSun/GDI/yaml/vehicles.yaml |
| ts_gdi_juggernautmkii | 5 | mods/cameo/ContentPacks/TiberianSun/GDI/yaml/vehicles.yaml |
| ts_gdi_jumpjetinfantry | 4 | mods/cameo/ContentPacks/TiberianSun/GDI/yaml/infantry.yaml |
| ts_gdi_kodiakcommandship | 4 | mods/cameo/ContentPacks/TiberianSun/GDI/yaml/aircraft.yaml |
| ts_gdi_lightinfantry | 6 | mods/cameo/ContentPacks/TiberianSun/GDI/yaml/infantry.yaml |
| ts_gdi_mammothmkii | 4 | mods/cameo/ContentPacks/TiberianSun/GDI/yaml/vehicles.yaml |
| ts_gdi_mammothprototype | 4 | mods/cameo/ContentPacks/TiberianSun/GDI/yaml/vehicles.yaml |
| ts_gdi_medic | 5 | mods/cameo/ContentPacks/TiberianSun/GDI/yaml/infantry.yaml |
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
| ts_gdi_upgradecenter | 5 | mods/cameo/ContentPacks/TiberianSun/GDI/yaml/buildings.yaml |
| ts_gdi_vulcantower | 4 | mods/cameo/ContentPacks/TiberianSun/GDI/yaml/defenses.yaml |
| ts_gdi_wolverine | 5 | mods/cameo/ContentPacks/TiberianSun/GDI/yaml/vehicles.yaml |
| ts_gdi_wolverinemkii | 5 | mods/cameo/ContentPacks/TiberianSun/GDI/yaml/vehicles.yaml |
| ts_gdi_zoneorcafighter | 4 | mods/cameo/ContentPacks/TiberianSun/GDI/yaml/aircraft.yaml |
| ts_gdi_zonetrooper | 5 | mods/cameo/ContentPacks/TiberianSun/GDI/yaml/infantry.yaml |
| ts_nod_advancedpowerplant | 4 | mods/cameo/ContentPacks/TiberianSun/Nod/yaml/buildings.yaml |
| ts_nod_artillery | 5 | mods/cameo/ContentPacks/TiberianSun/Nod/yaml/vehicles.yaml |
| ts_nod_attackbuggy | 4 | mods/cameo/ContentPacks/TiberianSun/Nod/yaml/vehicles.yaml |
| ts_nod_attackcycle | 4 | mods/cameo/ContentPacks/TiberianSun/Nod/yaml/vehicles.yaml |
| ts_nod_bansheefighter | 4 | mods/cameo/ContentPacks/TiberianSun/Nod/yaml/aircraft.yaml |
| ts_nod_chameleonspy | 5 | mods/cameo/ContentPacks/TiberianSun/Nod/yaml/infantry.yaml |
| ts_nod_devilstongue | 5 | mods/cameo/ContentPacks/TiberianSun/Nod/yaml/vehicles.yaml |
| ts_nod_elitecadre | 5 | mods/cameo/ContentPacks/TiberianSun/Nod/yaml/infantry.yaml |
| ts_nod_engineer | 7 | mods/cameo/ContentPacks/TiberianSun/Nod/yaml/infantry.yaml |
| ts_nod_harpy | 4 | mods/cameo/ContentPacks/TiberianSun/Nod/yaml/aircraft.yaml |
| ts_nod_laserturret | 4 | mods/cameo/ContentPacks/TiberianSun/Nod/yaml/defenses.yaml |
| ts_nod_lightinfantry | 6 | mods/cameo/ContentPacks/TiberianSun/Nod/yaml/infantry.yaml |
| ts_nod_missilesilo | 5 | mods/cameo/ContentPacks/TiberianSun/Nod/yaml/defenses.yaml |
| ts_nod_mobileconstructionvehicle | 4 | mods/cameo/ContentPacks/TiberianSun/Nod/yaml/vehicles.yaml |
| ts_nod_mobilerepairvehicle | 4 | mods/cameo/ContentPacks/TiberianSun/Nod/yaml/vehicles.yaml |
| ts_nod_mobilestealthgenerator | 4 | mods/cameo/ContentPacks/TiberianSun/Nod/yaml/vehicles.yaml |
| ts_nod_obeliskoflight | 4 | mods/cameo/ContentPacks/TiberianSun/Nod/yaml/defenses.yaml |
| ts_nod_powerplant | 4 | mods/cameo/ContentPacks/TiberianSun/Nod/yaml/buildings.yaml |
| ts_nod_radar | 4 | mods/cameo/ContentPacks/TiberianSun/Nod/yaml/buildings.yaml |
| ts_nod_rocketinfantry | 6 | mods/cameo/ContentPacks/TiberianSun/Nod/yaml/infantry.yaml |
| ts_nod_samsite | 4 | mods/cameo/ContentPacks/TiberianSun/Nod/yaml/defenses.yaml |
| ts_nod_shadowteam | 5 | mods/cameo/ContentPacks/TiberianSun/Nod/yaml/infantry.yaml |
| ts_nod_shadowteam_air | 4 | mods/cameo/ContentPacks/TiberianSun/Nod/yaml/infantry.yaml |
| ts_nod_shotguncommando | 5 | mods/cameo/ContentPacks/TiberianSun/Nod/yaml/infantry.yaml |
| ts_nod_stealthtank | 5 | mods/cameo/ContentPacks/TiberianSun/Nod/yaml/vehicles.yaml |
| ts_nod_subterraneanapc | 4 | mods/cameo/ContentPacks/TiberianSun/Nod/yaml/vehicles.yaml |
| ts_nod_tiberiumharvester | 5 | mods/cameo/ContentPacks/TiberianSun/Nod/yaml/vehicles.yaml |
| ts_nod_ticktank | 5 | mods/cameo/ContentPacks/TiberianSun/Nod/yaml/vehicles.yaml |
| ts_nod_toxintrooper | 5 | mods/cameo/ContentPacks/TiberianSun/Nod/yaml/infantry.yaml |
| tsaegis | 4 | mods/cameo/rules/tiberiansun.yaml |
| tsfloater | 4 | mods/cameo/rules/tiberiansun.yaml |
| tsfsmoker.bomber | 4 | mods/cameo/rules/tiberiansun.yaml |
| tsmonstermaker1 | 5 | mods/cameo/rules/tiberiansun.yaml |
| tsprobe | 4 | mods/cameo/rules/tiberiansun.yaml |
| tsun.asian | 4 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/naval.yaml |
| tsvislrg | 4 | mods/cameo/rules/tiberiansun.yaml |
| tsvissml | 4 | mods/cameo/rules/tiberiansun.yaml |
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
| wc2_humans_archmage | 6 | mods/cameo/ContentPacks/Warcraft2/Humans/yaml/infantry.yaml |
| wc2_humans_ballista | 5 | mods/cameo/ContentPacks/Warcraft2/Humans/yaml/vehicles.yaml |
| wc2_humans_barracks | 4 | mods/cameo/ContentPacks/Warcraft2/Humans/yaml/buildings.yaml |
| wc2_humans_blacksmith | 4 | mods/cameo/ContentPacks/Warcraft2/Humans/yaml/buildings.yaml |
| wc2_humans_cannontower | 5 | mods/cameo/ContentPacks/Warcraft2/Humans/yaml/defenses.yaml |
| wc2_humans_church | 4 | mods/cameo/ContentPacks/Warcraft2/Humans/yaml/buildings.yaml |
| wc2_humans_demolitionsquad | 6 | mods/cameo/ContentPacks/Warcraft2/Humans/yaml/vehicles.yaml |
| wc2_humans_dwarvenrifleman | 6 | mods/cameo/ContentPacks/Warcraft2/Humans/yaml/infantry.yaml |
| wc2_humans_elvenarcher | 6 | mods/cameo/ContentPacks/Warcraft2/Humans/yaml/infantry.yaml |
| wc2_humans_elvenlumbermill | 4 | mods/cameo/ContentPacks/Warcraft2/Humans/yaml/buildings.yaml |
| wc2_humans_elvenranger | 7 | mods/cameo/ContentPacks/Warcraft2/Humans/yaml/infantry.yaml |
| wc2_humans_farm | 5 | mods/cameo/ContentPacks/Warcraft2/Humans/yaml/buildings.yaml |
| wc2_humans_flyingmachine | 5 | mods/cameo/ContentPacks/Warcraft2/Humans/yaml/aircraft.yaml |
| wc2_humans_footman | 6 | mods/cameo/ContentPacks/Warcraft2/Humans/yaml/infantry.yaml |
| wc2_humans_gnomishinventor | 4 | mods/cameo/ContentPacks/Warcraft2/Humans/yaml/buildings.yaml |
| wc2_humans_gryphonaviary | 4 | mods/cameo/ContentPacks/Warcraft2/Humans/yaml/buildings.yaml |
| wc2_humans_gryphonrider | 5 | mods/cameo/ContentPacks/Warcraft2/Humans/yaml/aircraft.yaml |
| wc2_humans_guardtower | 5 | mods/cameo/ContentPacks/Warcraft2/Humans/yaml/defenses.yaml |
| wc2_humans_gyrocoptermachine | 5 | mods/cameo/ContentPacks/Warcraft2/Humans/yaml/aircraft.yaml |
| wc2_humans_highelfpriest | 6 | mods/cameo/ContentPacks/Warcraft2/Humans/yaml/infantry.yaml |
| wc2_humans_highelfsorceress | 6 | mods/cameo/ContentPacks/Warcraft2/Humans/yaml/infantry.yaml |
| wc2_humans_highelvenarcher | 6 | mods/cameo/ContentPacks/Warcraft2/Humans/yaml/infantry.yaml |
| wc2_humans_humangoldmine | 4 | mods/cameo/ContentPacks/Warcraft2/Humans/yaml/buildings.yaml |
| wc2_humans_humangoldmine_bot | 5 | mods/cameo/ContentPacks/Warcraft2/Humans/yaml/buildings.yaml |
| wc2_humans_humanscouttower | 4 | mods/cameo/ContentPacks/Warcraft2/Humans/yaml/defenses.yaml |
| wc2_humans_knight | 5 | mods/cameo/ContentPacks/Warcraft2/Humans/yaml/infantry.yaml |
| wc2_humans_mage | 6 | mods/cameo/ContentPacks/Warcraft2/Humans/yaml/infantry.yaml |
| wc2_humans_magetower | 6 | mods/cameo/ContentPacks/Warcraft2/Humans/yaml/buildings.yaml |
| wc2_humans_militiapeasant | 6 | mods/cameo/ContentPacks/Warcraft2/Humans/yaml/infantry.yaml |
| wc2_humans_mobileconstructionvehiclehuman | 5 | mods/cameo/ContentPacks/Warcraft2/Humans/yaml/vehicles.yaml |
| wc2_humans_mortarteam | 6 | mods/cameo/ContentPacks/Warcraft2/Humans/yaml/infantry.yaml |
| wc2_humans_paladin | 6 | mods/cameo/ContentPacks/Warcraft2/Humans/yaml/vehicles.yaml |
| wc2_humans_peasant | 6 | mods/cameo/ContentPacks/Warcraft2/Humans/yaml/vehicles.yaml |
| wc2_humans_siegeengine | 5 | mods/cameo/ContentPacks/Warcraft2/Humans/yaml/vehicles.yaml |
| wc2_humans_stables | 4 | mods/cameo/ContentPacks/Warcraft2/Humans/yaml/buildings.yaml |
| wc2_humans_sunwell | 4 | mods/cameo/ContentPacks/Warcraft2/Humans/yaml/buildings.yaml |
| wc2_humans_warcraft3footman | 6 | mods/cameo/ContentPacks/Warcraft2/Humans/yaml/infantry.yaml |
| wc2_humans_warcraft3knight | 5 | mods/cameo/ContentPacks/Warcraft2/Humans/yaml/vehicles.yaml |
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
| wc2_orcs_altarofstorms | 4 | mods/cameo/ContentPacks/Warcraft2/Orcs/yaml/buildings.yaml |
| wc2_orcs_barracks | 4 | mods/cameo/ContentPacks/Warcraft2/Orcs/yaml/buildings.yaml |
| wc2_orcs_blacksmith | 4 | mods/cameo/ContentPacks/Warcraft2/Orcs/yaml/buildings.yaml |
| wc2_orcs_cannontower | 5 | mods/cameo/ContentPacks/Warcraft2/Orcs/yaml/defenses.yaml |
| wc2_orcs_catapult | 5 | mods/cameo/ContentPacks/Warcraft2/Orcs/yaml/vehicles.yaml |
| wc2_orcs_deathknight | 6 | mods/cameo/ContentPacks/Warcraft2/Orcs/yaml/infantry.yaml |
| wc2_orcs_dragon | 5 | mods/cameo/ContentPacks/Warcraft2/Orcs/yaml/aircraft.yaml |
| wc2_orcs_dragonroost | 4 | mods/cameo/ContentPacks/Warcraft2/Orcs/yaml/buildings.yaml |
| wc2_orcs_goblinsappers | 6 | mods/cameo/ContentPacks/Warcraft2/Orcs/yaml/vehicles.yaml |
| wc2_orcs_goblinzeppelin | 5 | mods/cameo/ContentPacks/Warcraft2/Orcs/yaml/aircraft.yaml |
| wc2_orcs_golbinalchemist | 4 | mods/cameo/ContentPacks/Warcraft2/Orcs/yaml/buildings.yaml |
| wc2_orcs_grunt | 6 | mods/cameo/ContentPacks/Warcraft2/Orcs/yaml/infantry.yaml |
| wc2_orcs_guardtower | 5 | mods/cameo/ContentPacks/Warcraft2/Orcs/yaml/defenses.yaml |
| wc2_orcs_kodobeast | 4 | mods/cameo/ContentPacks/Warcraft2/Orcs/yaml/infantry.yaml |
| wc2_orcs_mobileconstructionvehicleorc | 5 | mods/cameo/ContentPacks/Warcraft2/Orcs/yaml/vehicles.yaml |
| wc2_orcs_ogre | 5 | mods/cameo/ContentPacks/Warcraft2/Orcs/yaml/infantry.yaml |
| wc2_orcs_ogremage | 6 | mods/cameo/ContentPacks/Warcraft2/Orcs/yaml/vehicles.yaml |
| wc2_orcs_ogremound | 4 | mods/cameo/ContentPacks/Warcraft2/Orcs/yaml/buildings.yaml |
| wc2_orcs_orcgoldmine | 4 | mods/cameo/ContentPacks/Warcraft2/Orcs/yaml/buildings.yaml |
| wc2_orcs_orcgoldmine_bot | 5 | mods/cameo/ContentPacks/Warcraft2/Orcs/yaml/buildings.yaml |
| wc2_orcs_orcwatchtower | 4 | mods/cameo/ContentPacks/Warcraft2/Orcs/yaml/defenses.yaml |
| wc2_orcs_peon | 6 | mods/cameo/ContentPacks/Warcraft2/Orcs/yaml/vehicles.yaml |
| wc2_orcs_pigfarm | 5 | mods/cameo/ContentPacks/Warcraft2/Orcs/yaml/buildings.yaml |
| wc2_orcs_siegeengine | 5 | mods/cameo/ContentPacks/Warcraft2/Orcs/yaml/vehicles.yaml |
| wc2_orcs_templeofthedamned | 6 | mods/cameo/ContentPacks/Warcraft2/Orcs/yaml/buildings.yaml |
| wc2_orcs_trollaxethrower | 6 | mods/cameo/ContentPacks/Warcraft2/Orcs/yaml/infantry.yaml |
| wc2_orcs_trollberserker | 7 | mods/cameo/ContentPacks/Warcraft2/Orcs/yaml/infantry.yaml |
| wc2_orcs_trollheadhunter | 6 | mods/cameo/ContentPacks/Warcraft2/Orcs/yaml/infantry.yaml |
| wc2_orcs_trolllumbermill | 4 | mods/cameo/ContentPacks/Warcraft2/Orcs/yaml/buildings.yaml |
| wc2_orcs_warcraft3grunt | 6 | mods/cameo/ContentPacks/Warcraft2/Orcs/yaml/infantry.yaml |
| wind_trap.atreides | 5 | mods/cameo/ContentPacks/D2k/Atreides/yaml/buildings.yaml |
| wirbelwind.nax | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/vehicles.yaml |
| wraith_husk.ordos | 4 | mods/cameo/ContentPacks/D2k/Ordos/yaml/aircraft.yaml |
| yakarmored.Husk | 4 | mods/cameo/rules/husks.yaml |
| yaktesla.Husk | 4 | mods/cameo/rules/husks.yaml |
| yrbpln | 4 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| yrhovr | 4 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| yrlunr.husk | 4 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| yrrobo | 5 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| yrygyard | 4 | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| yuri_barracks | 4 | mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/buildings.yaml |
| yuri_battlelab | 4 | mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/buildings.yaml |
| yuri_bioreactor | 4 | mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/buildings.yaml |
| yuri_biotrooper | 5 | mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/infantry.yaml |
| yuri_boomersubmarine | 4 | mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/naval.yaml |
| yuri_brute | 5 | mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/infantry.yaml |
| yuri_chaosdrone | 4 | mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/vehicles.yaml |
| yuri_clone | 5 | mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/infantry.yaml |
| yuri_cloningvats | 5 | mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/buildings.yaml |
| yuri_constructionyard | 4 | mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/buildings.yaml |
| yuri_cosmonaut | 4 | mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/infantry.yaml |
| yuri_engineer | 5 | mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/infantry.yaml |
| yuri_floatingdisk | 4 | mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/aircraft.yaml |
| yuri_gatlingcannon | 5 | mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/defenses.yaml |
| yuri_gatlingtank | 4 | mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/vehicles.yaml |
| yuri_gatlingtrooper | 5 | mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/infantry.yaml |
| yuri_geneticmutator | 4 | mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/defenses.yaml |
| yuri_grinder | 4 | mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/buildings.yaml |
| yuri_initiate | 5 | mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/infantry.yaml |
| yuri_lashertank | 5 | mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/vehicles.yaml |
| yuri_lunarcommandcenter | 5 | mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/buildings.yaml |
| yuri_magnetron | 5 | mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/vehicles.yaml |
| yuri_mastermind | 5 | mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/vehicles.yaml |
| yuri_mobileconstructionvehicle | 4 | mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/vehicles.yaml |
| yuri_psychicdominator | 5 | mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/defenses.yaml |
| yuri_psychicsensor | 4 | mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/buildings.yaml |
| yuri_psychictower | 5 | mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/defenses.yaml |
| yuri_slaveminer | 4 | mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/vehicles.yaml |
| yuri_slaveminer_deployed | 5 | mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/defenses.yaml |
| yuri_tankbunker | 5 | mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/defenses.yaml |
| yuri_virus | 5 | mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/infantry.yaml |
| yuri_warfactory | 5 | mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/buildings.yaml |
| yuri_yurix | 5 | mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/infantry.yaml |
| zerg_behemoth | 5 | mods/cameo/ContentPacks/StarCraft/Zerg/yaml/aircraft.yaml |
| zerg_broodweaver | 5 | mods/cameo/ContentPacks/StarCraft/Zerg/yaml/aircraft.yaml |
| zerg_corruptor | 5 | mods/cameo/ContentPacks/StarCraft/Zerg/yaml/aircraft.yaml |
| zerg_creepcolony | 4 | mods/cameo/ContentPacks/StarCraft/Zerg/yaml/defenses.yaml |
| zerg_creepcolony_defense | 5 | mods/cameo/ContentPacks/StarCraft/Zerg/yaml/defenses.yaml |
| zerg_defiler | 6 | mods/cameo/ContentPacks/StarCraft/Zerg/yaml/infantry.yaml |
| zerg_defilermound | 4 | mods/cameo/ContentPacks/StarCraft/Zerg/yaml/buildings.yaml |
| zerg_devourer | 5 | mods/cameo/ContentPacks/StarCraft/Zerg/yaml/aircraft.yaml |
| zerg_dreadshroud | 5 | mods/cameo/ContentPacks/StarCraft/Zerg/yaml/aircraft.yaml |
| zerg_drone | 5 | mods/cameo/ContentPacks/StarCraft/Zerg/yaml/vehicles.yaml |
| zerg_evolutionchamber | 4 | mods/cameo/ContentPacks/StarCraft/Zerg/yaml/buildings.yaml |
| zerg_extractor | 4 | mods/cameo/ContentPacks/StarCraft/Zerg/yaml/buildings.yaml |
| zerg_gorekraken | 5 | mods/cameo/ContentPacks/StarCraft/Zerg/yaml/aircraft.yaml |
| zerg_goremaw | 7 | mods/cameo/ContentPacks/StarCraft/Zerg/yaml/vehicles.yaml |
| zerg_guardian | 5 | mods/cameo/ContentPacks/StarCraft/Zerg/yaml/aircraft.yaml |
| zerg_hatchery | 4 | mods/cameo/ContentPacks/StarCraft/Zerg/yaml/buildings.yaml |
| zerg_hatcherydrone | 4 | mods/cameo/ContentPacks/StarCraft/Zerg/yaml/vehicles.yaml |
| zerg_hermit | 7 | mods/cameo/ContentPacks/StarCraft/Zerg/yaml/vehicles.yaml |
| zerg_hydralisk | 6 | mods/cameo/ContentPacks/StarCraft/Zerg/yaml/infantry.yaml |
| zerg_hydraliskden | 4 | mods/cameo/ContentPacks/StarCraft/Zerg/yaml/buildings.yaml |
| zerg_infestedcommandcenter | 4 | mods/cameo/ContentPacks/StarCraft/Zerg/yaml/buildings.yaml |
| zerg_infestedterranbomber | 5 | mods/cameo/ContentPacks/StarCraft/Zerg/yaml/infantry.yaml |
| zerg_kerrigan | 6 | mods/cameo/ContentPacks/StarCraft/Zerg/yaml/infantry.yaml |
| zerg_lurker | 7 | mods/cameo/ContentPacks/StarCraft/Zerg/yaml/vehicles.yaml |
| zerg_mutalisk | 5 | mods/cameo/ContentPacks/StarCraft/Zerg/yaml/aircraft.yaml |
| zerg_nyduscanal | 4 | mods/cameo/ContentPacks/StarCraft/Zerg/yaml/buildings.yaml |
| zerg_overlord | 5 | mods/cameo/ContentPacks/StarCraft/Zerg/yaml/aircraft.yaml |
| zerg_overmind | 5 | mods/cameo/ContentPacks/StarCraft/Zerg/yaml/buildings.yaml |
| zerg_queen | 5 | mods/cameo/ContentPacks/StarCraft/Zerg/yaml/aircraft.yaml |
| zerg_queensnest | 4 | mods/cameo/ContentPacks/StarCraft/Zerg/yaml/buildings.yaml |
| zerg_scourge | 5 | mods/cameo/ContentPacks/StarCraft/Zerg/yaml/aircraft.yaml |
| zerg_shriek | 5 | mods/cameo/ContentPacks/StarCraft/Zerg/yaml/infantry.yaml |
| zerg_spawningpool | 4 | mods/cameo/ContentPacks/StarCraft/Zerg/yaml/buildings.yaml |
| zerg_spire | 4 | mods/cameo/ContentPacks/StarCraft/Zerg/yaml/buildings.yaml |
| zerg_spithid | 5 | mods/cameo/ContentPacks/StarCraft/Zerg/yaml/infantry.yaml |
| zerg_sporecolony | 5 | mods/cameo/ContentPacks/StarCraft/Zerg/yaml/defenses.yaml |
| zerg_sporemaw | 6 | mods/cameo/ContentPacks/StarCraft/Zerg/yaml/vehicles.yaml |
| zerg_sunkencolony_defense | 5 | mods/cameo/ContentPacks/StarCraft/Zerg/yaml/defenses.yaml |
| zerg_swarmling | 5 | mods/cameo/ContentPacks/StarCraft/Zerg/yaml/infantry.yaml |
| zerg_talon | 5 | mods/cameo/ContentPacks/StarCraft/Zerg/yaml/infantry.yaml |
| zerg_ultralisk | 7 | mods/cameo/ContentPacks/StarCraft/Zerg/yaml/infantry.yaml |
| zerg_ultraliskcavern | 4 | mods/cameo/ContentPacks/StarCraft/Zerg/yaml/buildings.yaml |
| zerg_zergling | 5 | mods/cameo/ContentPacks/StarCraft/Zerg/yaml/infantry.yaml |


## V5 — actors with > 2 trait removals

| actor | removals | keys | file |
|---|---|---|---|
| A10Carrier | 4 | -UpdatesPlayerStatistics, -ActorLostNotification, -WithShadow, -MapEditorData | mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/aircraft.yaml |
| BARL | 11 | -Selectable, -ShakeOnDeath, -SoundOnDamageTransition, -Demolishable, -CaptureManager, -Capturable | mods/cameo/rules/tech.yaml |
| BRL3 | 11 | -Selectable, -ShakeOnDeath, -SoundOnDamageTransition, -Demolishable, -CaptureManager, -Capturable | mods/cameo/rules/tech.yaml |
| SCCOMMANDCENTERM | 4 | -AttackAircraft, -SpawnActorOnDeath, -Hovers@CRUISING, -Voiced | mods/cameo/rules/starcraft.yaml |
| SILO | 3 | -GivesBuildableArea, -WithSpriteBody, -AcceptsDeliveredCash | mods/cameo/ContentPacks/TiberianDawn/Shared/yaml/buildings.yaml |
| cabal_ascended | 3 | -DamagedByTerrain, -SpawnActorOnDeath, -TakeCover | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/infantry.yaml |
| cabal_berserker | 3 | -DamagedByTerrain, -SpawnActorOnDeath, -TakeCover | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/vehicles.yaml |
| cabal_cyborgcommando | 3 | -DamagedByTerrain, -SpawnActorOnDeath, -TakeCover | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/infantry.yaml |
| cabal_cyborgcommandov2 | 3 | -DamagedByTerrain, -SpawnActorOnDeath, -TakeCover | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/infantry.yaml |
| cabal_cyborginfantry | 3 | -DamagedByTerrain, -SpawnActorOnDeath, -TakeCover | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/infantry.yaml |
| cabal_devout | 3 | -DamagedByTerrain, -SpawnActorOnDeath, -TakeCover | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/infantry.yaml |
| cabal_engineer | 4 | -TemporaryOwnerManager, -TakeCover, -DamagedByTerrain, -SpawnActorOnDeath | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/infantry.yaml |
| cabal_enlighted | 3 | -DamagedByTerrain, -SpawnActorOnDeath, -TakeCover | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/infantry.yaml |
| cabal_hackercyborg | 3 | -DamagedByTerrain, -SpawnActorOnDeath, -TakeCover | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/infantry.yaml |
| cabal_hunterdrone | 3 | -ActorLostNotification, -UpdatesPlayerStatistics, -MapEditorData | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/aircraft.yaml |
| cabal_orbdrone_slave | 3 | -UpdatesPlayerStatistics, -ActorLostNotification, -MapEditorData | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/aircraft.yaml |
| cabal_plasmaturret | 3 | -WithVoxelBody, -Cloak@TDcloak, -ActorStatValues | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/defenses.yaml |
| cabal_rocketcyborg | 3 | -DamagedByTerrain, -SpawnActorOnDeath, -TakeCover | mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/infantry.yaml |
| cruiser_f.steel | 5 | -Selectable, -UpdatesPlayerStatistics, -ActorLostNotification, -WithShadow, -MapEditorData | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/aircraft.yaml |
| farasha_drone_ixian | 3 | -ActorLostNotification, -UpdatesPlayerStatistics, -MapEditorData | mods/cameo/ContentPacks/D2k/Ixian/yaml/aircraft.yaml |
| forgotten_apache_husk | 3 | -WithShadow, -Cloak@TDcloak, -Cloak@TScloak | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/husks.yaml |
| forgotten_ghoststalker_sp | 4 | -Buildable, -MapEditorData, -Voiced, -Armament@c4 | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/infantry.yaml |
| forgotten_mutant_sp | 3 | -Buildable, -MapEditorData, -Voiced | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/infantry.yaml |
| forgotten_mutantsniper_sp | 3 | -Buildable, -MapEditorData, -Voiced | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/infantry.yaml |
| forgotten_mutantsoldier_sp | 3 | -Buildable, -MapEditorData, -Voiced | mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/infantry.yaml |
| fremen_creep | 3 | -MustBeDestroyed, -RevealsShroud@base-reve, -GrantConditionOnPrerequ | mods/cameo/ContentPacks/D2k/Shared/yaml/infantry.yaml |
| futuretech_spyfutu | 3 | -Guard, -WithInfantryBody, -AttackFrontal | mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/infantry.yaml |
| gdirigdrone | 5 | -Targetable@SpecialRepai, -SpawnActorOnDeath, -ActorLostNotification, -UpdatesPlayerStatistics, -MapEditorData | mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/aircraft.yaml |
| hole.nax2 | 3 | -UpdatesPlayerStatistics, -ActorLostNotification, -MapEditorData | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/infantry.yaml |
| ixian_stormlasher | 3 | -WithDeathAnimation, -WithWallSpriteBody, -WithSpriteTurret | mods/cameo/ContentPacks/D2k/Ixian/yaml/buildings.yaml |
| japan_zerofighter_slave | 4 | -UpdatesPlayerStatistics, -ActorLostNotification, -WithShadow, -MapEditorData | mods/cameo/ContentPacks/RedAlert/Shared/yaml/naval.yaml |
| kami.asian | 4 | -UpdatesPlayerStatistics, -ActorLostNotification, -MapEditorData, -Voiced | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/aircraft.yaml |
| landcarr_drone.futu | 4 | -AutoTarget, -UpdatesPlayerStatistics, -MapEditorData, -ActorLostNotification | mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/aircraft.yaml |
| latinsyndicate_defensebureau | 3 | -WithTurretSearchlight, -WithDeathAnimation, -QuantizeFacingsFromSequ | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/buildings.yaml |
| latinsyndicate_narco | 4 | -ReloadAmmoDelayMultipli, -ReloadAmmoDelayMultipli, -ReloadAmmoDelayMultipli, -ReloadAmmoDelayMultipli | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/infantry.yaml |
| latinsyndicate_nuketruck | 3 | -RenderRangeCircle, -DamagedByTintedCells@ra, -DamagedByTintedCells@ge | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/vehicles.yaml |
| naxis_interceptor | 4 | -UpdatesPlayerStatistics, -ActorLostNotification, -MapEditorData, -Voiced | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/aircraft.yaml |
| protoss_analogue | 3 | -WithInfantryBody, -Targetable@disguise, -WithDeathAnimation | mods/cameo/ContentPacks/StarCraft/Protoss/yaml/vehicles.yaml |
| protoss_archon | 5 | -WithInfantryBody, -Targetable@disguise, -HitShape, -WithDeathAnimation, -Crushable | mods/cameo/ContentPacks/StarCraft/Protoss/yaml/vehicles.yaml |
| protoss_idol | 4 | -ExternalCondition@Propa, -WithInfantryBody, -Targetable@disguise, -WithDeathAnimation | mods/cameo/ContentPacks/StarCraft/Protoss/yaml/vehicles.yaml |
| protoss_manifold | 3 | -WithInfantryBody, -Targetable@disguise, -WithDeathAnimation | mods/cameo/ContentPacks/StarCraft/Protoss/yaml/vehicles.yaml |
| protoss_photoncannon | 3 | -WithTurretSearchlight, -WithSpriteBody, -WithDeathAnimation | mods/cameo/ContentPacks/StarCraft/Protoss/yaml/defenses.yaml |
| protoss_shieldbattery | 3 | -WithSpriteBody, -GivesBuildableArea, -WithDeathAnimation | mods/cameo/ContentPacks/StarCraft/Protoss/yaml/buildings.yaml |
| ra1_allies_camopillbox | 3 | -WithTurretSearchlight, -QuantizeFacingsFromSequ, -MustBeDestroyed | mods/cameo/ContentPacks/RedAlert/Allies/yaml/defenses.yaml |
| ra1_allies_chronovortexfade | 3 | -SpawnActorOnDeath, -PeriodicExplosion, -AmbientSound | mods/cameo/ContentPacks/RedAlert/Shared/yaml/misc.yaml |
| ra1_allies_gapgenerator | 10 | -AutoTarget, -RenderRangeCircle, -ExternalCondition@shrou, -ExternalCondition@locko, -RangeMultiplier@ra1_all, -RevealsShroudMultiplier | mods/cameo/ContentPacks/RedAlert/Allies/yaml/defenses.yaml |
| ra1_soviets_cyberdog | 3 | -Targetable@disguise, -InaccuracyMultiplier@ar, -InaccuracyMultiplier@Pr | mods/cameo/ContentPacks/RedAlert/Soviets/yaml/infantry.yaml |
| ra1_soviets_kotinnucleartank | 3 | -DamagedByTintedCells@ra, -DamagedByTintedCells@ge, -DamagedByTintedCells@ti | mods/cameo/ContentPacks/RedAlert/Soviets/yaml/vehicles.yaml |
| ra2_allies_gapgenerator | 10 | -AutoTarget, -RenderRangeCircle, -ExternalCondition@shrou, -ExternalCondition@locko, -RangeMultiplier@ra1_all, -RevealsShroudMultiplier | mods/cameo/ContentPacks/RedAlert2/Allies/yaml/defenses.yaml |
| ra2_allies_ra2spy | 3 | -Guard, -WithInfantryBody, -AttackFrontal | mods/cameo/ContentPacks/RedAlert2/Allies/yaml/infantry.yaml |
| ra2_soviets_kirovairship | 3 | -DamagedByTintedCells@ra, -DamagedByTintedCells@ge, -DamagedByTintedCells@ti | mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/aircraft.yaml |
| ra2_soviets_migbomber | 3 | -DamagedByTintedCells@ra, -DamagedByTintedCells@ge, -DamagedByTintedCells@ti | mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/aircraft.yaml |
| ra2_soviets_siegechopper | 5 | -AttackAircraft, -WithShadow, -DamagedByTintedCells@ra, -DamagedByTintedCells@ge, -DamagedByTintedCells@ti | mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/aircraft.yaml |
| ra2_soviets_transportkirov | 3 | -DamagedByTintedCells@ra, -DamagedByTintedCells@ge, -DamagedByTintedCells@ti | mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/aircraft.yaml |
| ra2asw | 4 | -UpdatesPlayerStatistics, -ActorLostNotification, -WithShadow, -MapEditorData | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2cplanesov | 3 | -DamagedByTintedCells@ra, -DamagedByTintedCells@ge, -DamagedByTintedCells@ti | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2hornet | 4 | -UpdatesPlayerStatistics, -ActorLostNotification, -WithShadow, -MapEditorData | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| ra2v3rocket | 8 | -FireWarheadsOnDeath, -DamagedByTintedCells@ra, -DamagedByTintedCells@ge, -DamagedByTintedCells@ti, -SpeedMultiplier@ra2_sov, -SpeedMultiplier@ra2_sov | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| sc_zerg_larva | 9 | -DeathSounds@NORMAL, -SpawnActorOnDeath@zerg_, -WithDeathAnimation, -DamagedByTerrain, -Crushable, -TakeCover | mods/cameo/rules/starcraft.yaml |
| scadept.shade | 11 | -UpdatesPlayerStatistics, -MapEditorData, -ActorLostNotification, -GrantTimedConditionOnDe, -ShadeMaster, -Passenger | mods/cameo/rules/starcraft.yaml |
| schwarzermond_drone | 4 | -UpdatesPlayerStatistics, -ActorLostNotification, -MapEditorData, -Voiced | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/yaml/aircraft.yaml |
| sietch_creep | 10 | -RevealsShroud@base-reve, -GrantConditionOnPrerequ, -DamagedByTerrain, -GivesBuildableArea, -Sellable, -RepairableBuilding | mods/cameo/ContentPacks/D2k/Shared/yaml/buildings.yaml |
| sietch_creep_disabled | 11 | -Targetable, -Selectable, -Targetable@ivan, -Targetable@trappable, -Targetable@chrono, -RevealsShroud@base-reve | mods/cameo/ContentPacks/D2k/Shared/yaml/buildings.yaml |
| terran_battlecruiser | 4 | -ReloadAmmoDelayMultipli, -ReloadAmmoDelayMultipli, -ReloadAmmoDelayMultipli, -ReloadAmmoDelayMultipli | mods/cameo/ContentPacks/StarCraft/Terran/yaml/aircraft.yaml |
| terran_missileturret | 3 | -WithTurretSearchlight, -WithSpriteBody, -ActorPreviewPlaceBuildi | mods/cameo/ContentPacks/StarCraft/Terran/yaml/defenses.yaml |
| terran_phobos | 5 | -AttackAircraft, -ReloadAmmoDelayMultipli, -ReloadAmmoDelayMultipli, -ReloadAmmoDelayMultipli, -ReloadAmmoDelayMultipli | mods/cameo/ContentPacks/StarCraft/Terran/yaml/aircraft.yaml |
| ts_gdi_carryall_husk | 3 | -WithShadow, -Cloak@TDcloak, -Cloak@TScloak | mods/cameo/ContentPacks/TiberianSun/GDI/yaml/husks.yaml |
| ts_gdi_orcabomber_husk | 3 | -WithShadow, -Cloak@TDcloak, -Cloak@TScloak | mods/cameo/ContentPacks/TiberianSun/GDI/yaml/husks.yaml |
| ts_gdi_orcafighter_husk | 3 | -WithShadow, -Cloak@TDcloak, -Cloak@TScloak | mods/cameo/ContentPacks/TiberianSun/GDI/yaml/husks.yaml |
| ts_nod_bansheefighter_husk | 3 | -WithShadow, -Cloak@TDcloak, -Cloak@TScloak | mods/cameo/ContentPacks/TiberianSun/Nod/yaml/husks.yaml |
| ts_nod_harpy_husk | 3 | -WithShadow, -Cloak@TDcloak, -Cloak@TScloak | mods/cameo/ContentPacks/TiberianSun/Nod/yaml/husks.yaml |
| ts_nod_laserfence_segment | 5 | -Crushable, -Sellable, -Targetable, -Building, -WithWallSpriteBody | mods/cameo/ContentPacks/TiberianSun/Nod/yaml/defenses.yaml |
| ts_nod_mobilestealthgenerator | 4 | -ExternalCondition@CLOAK, -ExternalCondition@TSCLO, -Cloak@TDcloak, -Cloak@TScloak | mods/cameo/ContentPacks/TiberianSun/Nod/yaml/vehicles.yaml |
| tsprobe | 6 | -ActorLostNotification, -UpdatesPlayerStatistics, -RenderVoxels, -WithVoxelBody, -WithShadow, -SpawnActorOnDeath | mods/cameo/rules/tiberiansun.yaml |
| wc2_humans_ballista | 3 | -Integrity, -GrantCondition@electron, -WithDeathAnimation | mods/cameo/ContentPacks/Warcraft2/Humans/yaml/vehicles.yaml |
| wc2_humans_humanscouttower | 3 | -WithTurretSearchlight, -WithDeathAnimation, -WithMakeAnimation | mods/cameo/ContentPacks/Warcraft2/Humans/yaml/defenses.yaml |
| wc2_humans_knight | 5 | -Integrity, -GrantCondition@electron, -WithFacingSpriteBody, -WithMoveAnimation, -WithAttackAnimation | mods/cameo/ContentPacks/Warcraft2/Humans/yaml/infantry.yaml |
| wc2_humans_mobileconstructionvehiclehuman | 3 | -WithFacingSpriteBody, -WithMoveAnimation, -WithAttackAnimation | mods/cameo/ContentPacks/Warcraft2/Humans/yaml/vehicles.yaml |
| wc2_humans_siegeengine | 4 | -AttackFrontal, -Integrity, -GrantCondition@electron, -WithDeathAnimation | mods/cameo/ContentPacks/Warcraft2/Humans/yaml/vehicles.yaml |
| wc2_orcs_catapult | 3 | -Integrity, -GrantCondition@electron, -WithDeathAnimation | mods/cameo/ContentPacks/Warcraft2/Orcs/yaml/vehicles.yaml |
| wc2_orcs_mobileconstructionvehicleorc | 3 | -WithFacingSpriteBody, -WithMoveAnimation, -WithAttackAnimation | mods/cameo/ContentPacks/Warcraft2/Orcs/yaml/vehicles.yaml |
| wc2_orcs_ogre | 5 | -Integrity, -GrantCondition@electron, -WithFacingSpriteBody, -WithMoveAnimation, -WithAttackAnimation | mods/cameo/ContentPacks/Warcraft2/Orcs/yaml/infantry.yaml |
| wc2_orcs_orcwatchtower | 3 | -WithTurretSearchlight, -WithDeathAnimation, -WithMakeAnimation | mods/cameo/ContentPacks/Warcraft2/Orcs/yaml/defenses.yaml |
| wc2_orcs_siegeengine | 4 | -AttackFrontal, -Integrity, -GrantCondition@electron, -WithDeathAnimation | mods/cameo/ContentPacks/Warcraft2/Orcs/yaml/vehicles.yaml |
| wc2_support_orc_eye_of_kilrogg | 4 | -Selectable, -Voiced, -Targetable@AIRBORNE, -SpawnActorOnDeath | mods/cameo/rules/warcraft2.yaml |
| yrbpln | 3 | -DamagedByTintedCells@ra, -DamagedByTintedCells@ge, -DamagedByTintedCells@ti | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| yrschp.Husk | 3 | -WithShadow, -Cloak@TDcloak, -Cloak@TScloak | mods/cameo/ContentPacks/RedAlert2/Shared/yaml/misc.yaml |
| yuri_biotrooper | 3 | -DamagedByTerrain, -DamagedByTintedCells@ra, -DamagedByTintedCells@ge | mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/infantry.yaml |
| yuri_gatlingcannon | 3 | -RenderRangeCircle, -WithVoxelBody, -Cloak@TDcloak | mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/defenses.yaml |
| zerg_creepcolony | 3 | -WithTurretSearchlight, -WithDeathAnimation, -WithMakeAnimation | mods/cameo/ContentPacks/StarCraft/Zerg/yaml/defenses.yaml |
| zerg_drone | 5 | -WithMakeAnimation, -WithFacingSpriteBody, -WithInfantryBody, -Targetable@disguise, -WithSpriteBody@deployed | mods/cameo/ContentPacks/StarCraft/Zerg/yaml/vehicles.yaml |
| zerg_lurker | 3 | -HitShape, -WithMakeAnimation, -AttackFrontal | mods/cameo/ContentPacks/StarCraft/Zerg/yaml/vehicles.yaml |
| zerg_overlord | 4 | -Targetable@infiltrate, -AttackAircraft, -AutoTarget, -RenderRangeCircle | mods/cameo/ContentPacks/StarCraft/Zerg/yaml/aircraft.yaml |
| zerg_overmind | 3 | -WithMakeAnimation, -WithDeathAnimation, -ToggleConditionOnOrder | mods/cameo/ContentPacks/StarCraft/Zerg/yaml/buildings.yaml |

