# audit_inherits — §10.3 invariant violations (B2)

Actors+templates scanned: **3841**

| violation | meaning | count |
|---|---|---|
| V1 | concrete actor inherits from concrete actor | 330 |
| V2 | inherit crosses faction ownership | 23 |
| V3 | dangling inherit target (BLOCKING) | 0 |
| V4 | chain depth > 3 | 1641 |
| V5 | > 2 -Trait removals (warning) | 91 |


## V3 — dangling inherit targets (blocking)

_none found_


## V2 — cross-faction inherits (concrete targets)

| actor | actor faction | target | target faction | file |
|---|---|---|---|---|
| TSGTSILOCABAL | cabal | TSGTSILO | tsgdi | mods/cameo/rules/tiberiansun.yaml |
| TSGTSILONOD | tsnod | TSGTSILO | tsgdi | mods/cameo/rules/tiberiansun.yaml |
| awall.asian | redalert2mod/asianalliance | BRIK | tiberiandawn/shared | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/rules/buildings.yaml |
| carryall.ordos | d2k/ordos | carryall | d2k/shared | mods/cameo/ContentPacks/D2k/Ordos/rules/aircraft.yaml |
| carryall_reinforce.ordos | d2k/ordos | carryall.reinforce | d2k/shared | mods/cameo/ContentPacks/D2k/Ordos/rules/aircraft.yaml |
| engineer | d2k/shared | E6 | tiberiandawn/shared | mods/cameo/ContentPacks/D2k/Shared/rules/infantry.yaml |
| heavy_factory.ixian | d2k/ixian | heavy_factory | d2k/shared | mods/cameo/ContentPacks/D2k/Ixian/rules/buildings.yaml |
| heavy_factory.ordos | d2k/ordos | heavy_factory | d2k/shared | mods/cameo/ContentPacks/D2k/Ordos/rules/buildings.yaml |
| light_factory.ordos | d2k/ordos | light_factory | d2k/shared | mods/cameo/ContentPacks/D2k/Ordos/rules/buildings.yaml |
| tran.gdi | tiberiandawn/gdi | TRAN | tiberiandawn/shared | mods/cameo/ContentPacks/TiberianDawn/GDI/rules/aircraft.yaml |
| tran.nod | tiberiandawn/nod | TRAN | tiberiandawn/shared | mods/cameo/ContentPacks/TiberianDawn/Nod/rules/aircraft.yaml |
| tscabaltech | cabal | tsgttech | tsgdi | mods/cameo/rules/tiberiansun.yaml |
| tsgtdeptcabal | cabal | tsgtdeptgdi | tsgdi | mods/cameo/rules/tiberiansun.yaml |
| tsgtdeptmutant | forgotten | tsgtdeptgdi | tsgdi | mods/cameo/rules/tiberiansun.yaml |
| tsgtdeptnod | tsnod | tsgtdeptgdi | tsgdi | mods/cameo/rules/tiberiansun.yaml |
| tsgthpadmutant | forgotten | tsgthpad | tsgdi | mods/cameo/rules/tiberiansun.yaml |
| tsnthpad2 | cabal | tsnthpad | tsnod | mods/cameo/rules/tiberiansun.yaml |
| tsntlasrcabal | cabal | tsntlasr | tsnod | mods/cameo/rules/tiberiansun.yaml |
| tsntmislcabal | cabal | tsntmisl | tsnod | mods/cameo/rules/tiberiansun.yaml |
| tsntobelcabal | cabal | tsntobel | tsnod | mods/cameo/rules/tiberiansun.yaml |
| tsntradrcabal | cabal | tsntradr | tsnod | mods/cameo/rules/tiberiansun.yaml |
| tsntstlhcabal | cabal | tsntstlh | tsnod | mods/cameo/rules/tiberiansun.yaml |
| tsprocnod | tsnod | tsprocgdi | tsgdi | mods/cameo/rules/tiberiansun.yaml |


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
| CNCSPEN | RASPEN | tiberiandawn/nod | ? | mods/cameo/ContentPacks/TiberianDawn/Nod/rules/buildings.yaml |
| CNCSYRD | RA1SYRD | tiberiandawn/gdi | ? | mods/cameo/ContentPacks/TiberianDawn/GDI/rules/buildings.yaml |
| ChronoVortexFade | ChronoVortex | ? | ? | mods/cameo/rules/redalert.yaml |
| E1 | E1.GDI | tiberiandawn/gdi | tiberiandawn/gdi | mods/cameo/ContentPacks/TiberianDawn/GDI/rules/infantry.yaml |
| E3 | E3.GDI | tiberiandawn/gdi | tiberiandawn/gdi | mods/cameo/ContentPacks/TiberianDawn/GDI/rules/infantry.yaml |
| EDEN_TIGER_ACIDCLOUD | EDEN_LYNX_ACIDCLOUD | ? | ? | mods/cameo/rules/outpost2.yaml |
| ForceShieldDrainer | CAMERA.small | ? | ? | mods/cameo/rules/shared.yaml |
| INVISIBLEPLANE | BADR | ? | ? | mods/cameo/rules/tiberiansun.yaml |
| MODCORE1 | MODCORE | ? | ? | mods/cameo/rules/redalert.yaml |
| MODCORE2 | MODCORE | ? | ? | mods/cameo/rules/redalert.yaml |
| MODCORE3 | MODCORE | ? | ? | mods/cameo/rules/redalert.yaml |
| MODCORE4 | MODCORE | ? | ? | mods/cameo/rules/redalert.yaml |
| MODCORE5 | MODCORE | ? | ? | mods/cameo/rules/redalert.yaml |
| MODCORE6 | MODCORE | ? | ? | mods/cameo/rules/redalert.yaml |
| MODCORE7 | MODCORE | ? | ? | mods/cameo/rules/redalert.yaml |
| MODRAAFLD | RAAFLD | ? | ? | mods/cameo/rules/redalert.yaml |
| MODRAWEAPJ | RAWEAP | ? | ? | mods/cameo/rules/redalert.yaml |
| MONEYCRATE.LARGE | MONEYCRATE | ? | ? | mods/cameo/rules/misc.yaml |
| OILB.TS | OILB.Building | ? | ? | mods/cameo/rules/tiberiansun.yaml |
| OILB.TS.MUTANT | OILB.TS | forgotten | ? | mods/cameo/rules/tiberiansun.yaml |
| OILB.d2k | OILB.Building | d2k/shared | ? | mods/cameo/ContentPacks/D2k/Shared/rules/buildings.yaml |
| PLYMOUTH_TIGER_EMP | PLYMOUTH_LYNX_EMP | ? | ? | mods/cameo/rules/outpost2.yaml |
| PLYMOUTH_TIGER_ESG | PLYMOUTH_LYNX_ESG | ? | ? | mods/cameo/rules/outpost2.yaml |
| PLYMOUTH_TIGER_MICROWAVE | PLYMOUTH_LYNX_MICROWAVE | ? | ? | mods/cameo/rules/outpost2.yaml |
| PLYMOUTH_TIGER_RPG | PLYMOUTH_LYNX_RPG | ? | ? | mods/cameo/rules/outpost2.yaml |
| PLYMOUTH_TIGER_STARFLARE | PLYMOUTH_LYNX_STARFLARE | ? | ? | mods/cameo/rules/outpost2.yaml |
| PLYMOUTH_TIGER_STICKYFOAM | PLYMOUTH_LYNX_STICKYFOAM | ? | ? | mods/cameo/rules/outpost2.yaml |
| PLYMOUTH_TIGER_SUPERNOVA | PLYMOUTH_LYNX_SUPERNOVA | ? | ? | mods/cameo/rules/outpost2.yaml |
| RA2BRIK | BRIK | ? | tiberiandawn/shared | mods/cameo/rules/redalert2.yaml |
| RA2ENGINEER | E6 | ? | tiberiandawn/shared | mods/cameo/rules/redalert2.yaml |
| RA2FVBOTCHRONO | RA2FVBOTMG | ? | ? | mods/cameo/rules/redalert2.yaml |
| RA2FVBOTHMG | RA2FV | ? | ? | mods/cameo/rules/redalert2.yaml |
| RA2FVBOTMG | RA2FV | ? | ? | mods/cameo/rules/redalert2.yaml |
| RA2FVBOTMISS | RA2FV | ? | ? | mods/cameo/rules/redalert2.yaml |
| RA2FVBOTREP | RA2FVBOTMG | ? | ? | mods/cameo/rules/redalert2.yaml |
| RABIO | bio | ? | ? | mods/cameo/rules/tech.yaml |
| RAE6 | E6 | ? | tiberiandawn/shared | mods/cameo/rules/redalert.yaml |
| RAMAID | RAJE3 | ? | ? | mods/cameo/rules/redalert.yaml |
| RAMISS | MISS | ? | ? | mods/cameo/rules/tech.yaml |
| SCBARRACKSM | SCCOMMANDCENTERM | ? | ? | mods/cameo/rules/starcraft.yaml |
| SCCREEPCOLONYDEFENSE | SCCREEPCOLONY | ? | ? | mods/cameo/rules/starcraft.yaml |
| SCENGINEERINGBAYM | SCCOMMANDCENTERM | ? | ? | mods/cameo/rules/starcraft.yaml |
| SCFACTORYM | SCCOMMANDCENTERM | ? | ? | mods/cameo/rules/starcraft.yaml |
| SCSCIENCEFACILITYM | SCCOMMANDCENTERM | ? | ? | mods/cameo/rules/starcraft.yaml |
| SCSCOURGEDRONE | SCSCOURGE | ? | ? | mods/cameo/rules/starcraft.yaml |
| SCSENTINELM | SCCOMMANDCENTERM | ? | ? | mods/cameo/rules/starcraft.yaml |
| SCSTARPORTM | SCCOMMANDCENTERM | ? | ? | mods/cameo/rules/starcraft.yaml |
| TECHBCANNON2 | TECHBCANNON | ? | ? | mods/cameo/rules/tech.yaml |
| TSCYC2 | TSCYBORG | ? | ? | mods/cameo/rules/tiberiansun.yaml |
| TSDOGGIEW | TSDOGGIE | ? | ? | mods/cameo/rules/tiberiansun.yaml |
| TSE1.GDI | TSE1 | ? | ? | mods/cameo/rules/tiberiansun.yaml |
| TSE1.NOD | TSE1 | ? | ? | mods/cameo/rules/tiberiansun.yaml |
| TSE1PARA | TSE1 | ? | ? | mods/cameo/rules/tiberiansun.yaml |
| TSE2PARA | TSE2 | ? | ? | mods/cameo/rules/tiberiansun.yaml |
| TSE3.Nod | TSE3 | ? | ? | mods/cameo/rules/tiberiansun.yaml |
| TSENGINEECABAL | TSENGINEER | ? | ? | mods/cameo/rules/tiberiansun.yaml |
| TSENGINEER | E6 | ? | tiberiandawn/shared | mods/cameo/rules/tiberiansun.yaml |
| TSENGINEER.GDI | TSENGINEER | ? | ? | mods/cameo/rules/tiberiansun.yaml |
| TSENGINEER2 | TSENGINEER | ? | ? | mods/cameo/rules/tiberiansun.yaml |
| TSENGINEERMUTANT | TSENGINEER | ? | ? | mods/cameo/rules/tiberiansun.yaml |
| TSGHOSTSP | TSGHOST | ? | ? | mods/cameo/rules/tiberiansun.yaml |
| TSGTSILOCABAL | TSGTSILO | cabal | tsgdi | mods/cameo/rules/tiberiansun.yaml |
| TSGTSILONOD | TSGTSILO | tsnod | tsgdi | mods/cameo/rules/tiberiansun.yaml |
| TSMCVCABAL | TSMCVGDI | ? | ? | mods/cameo/rules/tiberiansun.yaml |
| TSMCVMUTANT | TSMCVGDI | ? | ? | mods/cameo/rules/tiberiansun.yaml |
| TSMCVNOD | TSMCVGDI | ? | ? | mods/cameo/rules/tiberiansun.yaml |
| TSMEDIC | MEDI | ? | ? | mods/cameo/rules/tiberiansun.yaml |
| TSMUTANTSP | TSMUTANT | ? | ? | mods/cameo/rules/tiberiansun.yaml |
| TSMUTANTW | TSMUTANT | ? | ? | mods/cameo/rules/tiberiansun.yaml |
| TSMWMNSP | TSMWMN | ? | ? | mods/cameo/rules/tiberiansun.yaml |
| TSREPAIRCABAL | TSREPAIR | ? | ? | mods/cameo/rules/tiberiansun.yaml |
| TSUMAGON | TSMUTANT | ? | ? | mods/cameo/rules/tiberiansun.yaml |
| TSUMAGONSP | TSUMAGON | ? | ? | mods/cameo/rules/tiberiansun.yaml |
| U3 | U2 | ? | ? | mods/cameo/rules/redalert.yaml |
| WWCRATE | CRATE | ? | ? | mods/cameo/rules/misc.yaml |
| ambiance_battle | ambiance_wind | ? | ? | mods/cameo/rules/misc.yaml |
| ambiance_bird | ambiance_wind | ? | ? | mods/cameo/rules/misc.yaml |
| ambiance_bird_robin | ambiance_wind | ? | ? | mods/cameo/rules/misc.yaml |
| ambiance_ocean_calm | ambiance_wind | ? | ? | mods/cameo/rules/misc.yaml |
| ambiance_ocean_waves | ambiance_wind | ? | ? | mods/cameo/rules/misc.yaml |
| ambiance_rumbling | ambiance_wind | ? | ? | mods/cameo/rules/misc.yaml |
| awall.asian | BRIK | redalert2mod/asianalliance | tiberiandawn/shared | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/rules/buildings.yaml |
| bbomb2_husk.nax2 | bbomb_husk.nax2 | redalert2mod/schwarzermond | redalert2mod/schwarzermond | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/rules/aircraft.yaml |
| bbomb3_husk.nax2 | bbomb_husk.nax2 | redalert2mod/schwarzermond | redalert2mod/schwarzermond | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/rules/aircraft.yaml |
| bomber_husk.asian | BADR.Husk | redalert2mod/asianalliance | ? | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/rules/aircraft.yaml |
| bomber_minebomb.asian | BADR | redalert2mod/asianalliance | ? | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/rules/aircraft.yaml |
| bomber_minebomb2.asian | bomber_minebomb.asian | redalert2mod/asianalliance | redalert2mod/asianalliance | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/rules/aircraft.yaml |
| camera.paradrop | RACAMERA | ? | ? | mods/cameo/rules/misc.yaml |
| camera.placeholderhack | CAMERA.small | ? | ? | mods/cameo/rules/misc.yaml |
| camera.psireveal | camera.scan | ? | ? | mods/cameo/rules/misc.yaml |
| camera.ra2spy | CAMERA.small | ? | ? | mods/cameo/rules/shared.yaml |
| camera.radarvan | camera.scan | ? | ? | mods/cameo/rules/misc.yaml |
| camera.sathack | camera.paradrop | ? | ? | mods/cameo/rules/misc.yaml |
| camera.spyplane | camera.scan | ? | ? | mods/cameo/rules/misc.yaml |
| camera.spysat | camera.scan | ? | ? | mods/cameo/rules/misc.yaml |
| carryall | carryall.reinforce | d2k/shared | d2k/shared | mods/cameo/ContentPacks/D2k/Shared/rules/aircraft.yaml |
| carryall.ordos | carryall | d2k/ordos | d2k/shared | mods/cameo/ContentPacks/D2k/Ordos/rules/aircraft.yaml |
| carryall.paradrop | carryall.reinforce | d2k/shared | d2k/shared | mods/cameo/ContentPacks/D2k/Shared/rules/aircraft.yaml |
| carryall_reinforce.ordos | carryall.reinforce | d2k/ordos | d2k/shared | mods/cameo/ContentPacks/D2k/Ordos/rules/aircraft.yaml |
| cgcnst.latin | ra2gacnst | redalert2mod/syndicate | ? | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/rules/buildings.yaml |
| concreteadefense | concreteabuilding | d2k/shared | d2k/shared | mods/cameo/ContentPacks/D2k/Shared/rules/buildings.yaml |
| concretebbuilding | concreteabuilding | d2k/shared | d2k/shared | mods/cameo/ContentPacks/D2k/Shared/rules/buildings.yaml |
| concretebdefense | concretebbuilding | d2k/shared | d2k/shared | mods/cameo/ContentPacks/D2k/Shared/rules/buildings.yaml |
| corpse_big.nax | corpse.nax | redalert2mod/naxis | redalert2mod/naxis | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/rules/vehicles.yaml |
| deathcash.latin | RACAMERA | redalert2mod/syndicate | ? | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/rules/upgrades.yaml |
| deathcash_small.latin | RACAMERA | redalert2mod/syndicate | ? | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/rules/upgrades.yaml |
| engineer | E6 | d2k/shared | tiberiandawn/shared | mods/cameo/ContentPacks/D2k/Shared/rules/infantry.yaml |
| fedeng.futu | RA2ENGINEER | redalert2mod/futuretech | ? | mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/rules/infantry.yaml |
| fedeng.steel | RA2ENGINEER | redalert2mod/consortium | ? | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/rules/infantry.yaml |
| frigate.paradrop | frigate | d2k/shared | d2k/shared | mods/cameo/ContentPacks/D2k/Shared/rules/aircraft.yaml |
| gdihumvee | JEEP | tiberiandawn/gdi | tiberiandawn/gdi | mods/cameo/ContentPacks/TiberianDawn/GDI/rules/vehicles.yaml |
| heavy_factory.ixian | heavy_factory | d2k/ixian | d2k/shared | mods/cameo/ContentPacks/D2k/Ixian/rules/buildings.yaml |
| heavy_factory.ordos | heavy_factory | d2k/ordos | d2k/shared | mods/cameo/ContentPacks/D2k/Ordos/rules/buildings.yaml |
| hole_small.nax2 | hole.nax2 | redalert2mod/schwarzermond | redalert2mod/schwarzermond | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/rules/infantry.yaml |
| horten_bomber.nax | BADR.Soviet | redalert2mod/naxis | ? | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/rules/aircraft.yaml |
| ifv.futu | RA2FV | redalert2mod/futuretech | ? | mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/rules/vehicles.yaml |
| jsuperbomber | BADR | ? | ? | mods/cameo/rules/redalert.yaml |
| jsuperbomber.Husk | BADR.Husk | ? | ? | mods/cameo/rules/husks.yaml |
| kami_asdf.asian | kami.asian | redalert2mod/asianalliance | redalert2mod/asianalliance | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/rules/aircraft.yaml |
| kami_chemical.asian | kami.asian | redalert2mod/asianalliance | redalert2mod/asianalliance | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/rules/aircraft.yaml |
| landcarr_drone.futu | ra2hornet | redalert2mod/futuretech | ? | mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/rules/aircraft.yaml |
| light_factory.ordos | light_factory | d2k/ordos | d2k/shared | mods/cameo/ContentPacks/D2k/Ordos/rules/buildings.yaml |
| modbomber.Husk | YAK.Husk | ? | ? | mods/cameo/rules/husks.yaml |
| modkami.Husk | YAK.Husk | ? | ? | mods/cameo/rules/husks.yaml |
| modkamimini.Husk | YAK.Husk | ? | ? | mods/cameo/rules/husks.yaml |
| nodbuggy2 | BGGY | tiberiandawn/nod | tiberiandawn/nod | mods/cameo/ContentPacks/TiberianDawn/Nod/rules/vehicles.yaml |
| nuketruk.latin | DTRK | redalert2mod/syndicate | ? | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/rules/vehicles.yaml |
| oilt.asian | DTRK | redalert2mod/asianalliance | ? | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/rules/vehicles.yaml |
| qacst.steel | ra2gacnst | redalert2mod/consortium | ? | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/rules/buildings.yaml |
| ra2_awall | BRIK | ? | tiberiandawn/shared | mods/cameo/rules/redalert2.yaml |
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
| ra2_swall | BRIK | ? | tiberiandawn/shared | mods/cameo/rules/redalert2.yaml |
| ra2_ywall | BRIK | ? | tiberiandawn/shared | mods/cameo/rules/redalert2.yaml |
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
| ra2dtruck.latin | DTRK | redalert2mod/syndicate | ? | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/rules/vehicles.yaml |
| ra2e2.black | RA2E2 | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2engineer.asian | RA2ENGINEER | redalert2mod/asianalliance | ? | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/rules/infantry.yaml |
| ra2engineer.latin | RA2ENGINEER | redalert2mod/syndicate | ? | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/rules/infantry.yaml |
| ra2engineer.soviet | RA2ENGINEER | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2engineer.yuri | RA2ENGINEER | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2nacnst | ra2gacnst | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2shk.bot | ra2shk | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2shkhero | ra2shk | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra2v3rocketelite | ra2v3rocket | ? | ? | mods/cameo/rules/redalert2.yaml |
| ra_gigafactory | RAWEAP | ? | ? | mods/cameo/rules/redalert.yaml |
| ra_industrialminer | RAHARV.SOVIET | ? | ? | mods/cameo/rules/redalert.yaml |
| ra_largeairfield | RAAFLD | ? | ? | mods/cameo/rules/redalert.yaml |
| railt2.asian | railt.asian | redalert2mod/asianalliance | redalert2mod/asianalliance | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/rules/vehicles.yaml |
| scadept.shade | SCADEPT | ? | ? | mods/cameo/rules/starcraft.yaml |
| scsporecolony | SCCREEPCOLONY | ? | ? | mods/cameo/rules/starcraft.yaml |
| scsunkencolony | SCCREEPCOLONY | ? | ? | mods/cameo/rules/starcraft.yaml |
| sietch_creep_disabled | sietch_creep | d2k/shared | d2k/shared | mods/cameo/ContentPacks/D2k/Shared/rules/buildings.yaml |
| slav.nax | YRSLAV | redalert2mod/naxis | ? | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/rules/infantry.yaml |
| sonar | camera.spyplane | ? | ? | mods/cameo/rules/misc.yaml |
| stealth_raider.ordos | raider.ordos | d2k/ordos | d2k/ordos | mods/cameo/ContentPacks/D2k/Ordos/rules/vehicles.yaml |
| tkmabramspoint | tkmabrams | ? | ? | mods/cameo/rules/tkm.yaml |
| tkmengineer | E6 | ? | tiberiandawn/shared | mods/cameo/rules/tkm.yaml |
| tkmworker | YRSLAV | ? | ? | mods/cameo/rules/tkm.yaml |
| tran.gdi | TRAN | tiberiandawn/gdi | tiberiandawn/shared | mods/cameo/ContentPacks/TiberianDawn/GDI/rules/aircraft.yaml |
| tran.nod | TRAN | tiberiandawn/nod | tiberiandawn/shared | mods/cameo/ContentPacks/TiberianDawn/Nod/rules/aircraft.yaml |
| ts_crate | CRATE | ? | ? | mods/cameo/rules/misc.yaml |
| tsart2cabal_backup | TSART2CABAL | ? | ? | mods/cameo/rules/tiberiansun.yaml |
| tscabaltech | tsgttech | cabal | tsgdi | mods/cameo/rules/tiberiansun.yaml |
| tsccommando | TSCYBORG | ? | ? | mods/cameo/rules/tiberiansun.yaml |
| tscheavyspider_backup | tscheavyspider | ? | ? | mods/cameo/rules/tiberiansun.yaml |
| tse3.mutant | TSE3 | ? | ? | mods/cameo/rules/tiberiansun.yaml |
| tsfsmoker.bomber | tsfsmoker | ? | ? | mods/cameo/rules/tiberiansun.yaml |
| tsghost.r4 | TSGHOST | ? | ? | mods/cameo/rules/tiberiansun.yaml |
| tsgtdeptcabal | tsgtdeptgdi | cabal | tsgdi | mods/cameo/rules/tiberiansun.yaml |
| tsgtdeptmutant | tsgtdeptgdi | forgotten | tsgdi | mods/cameo/rules/tiberiansun.yaml |
| tsgtdeptnod | tsgtdeptgdi | tsnod | tsgdi | mods/cameo/rules/tiberiansun.yaml |
| tsgthpadmutant | tsgthpad | forgotten | tsgdi | mods/cameo/rules/tiberiansun.yaml |
| tsmonstermaker1 | VICE | ? | ? | mods/cameo/rules/tiberiansun.yaml |
| tsnthpad2 | tsnthpad | cabal | tsnod | mods/cameo/rules/tiberiansun.yaml |
| tsntlasrcabal | tsntlasr | cabal | tsnod | mods/cameo/rules/tiberiansun.yaml |
| tsntmislcabal | tsntmisl | cabal | tsnod | mods/cameo/rules/tiberiansun.yaml |
| tsntobelcabal | tsntobel | cabal | tsnod | mods/cameo/rules/tiberiansun.yaml |
| tsntradrcabal | tsntradr | cabal | tsnod | mods/cameo/rules/tiberiansun.yaml |
| tsntstlhcabal | tsntstlh | cabal | tsnod | mods/cameo/rules/tiberiansun.yaml |
| tsprocnod | tsprocgdi | tsnod | tsgdi | mods/cameo/rules/tiberiansun.yaml |
| tssapc.mut | TSSAPC | ? | ? | mods/cameo/rules/tiberiansun.yaml |
| tssgencabal | tssgen | ? | ? | mods/cameo/rules/tiberiansun.yaml |
| tsttnkcabal_backup | TSTTNKCABAL | ? | ? | mods/cameo/rules/tiberiansun.yaml |
| tsumagon.r4 | TSUMAGON | ? | ? | mods/cameo/rules/tiberiansun.yaml |
| wc2_camera_scanner | camera.scan | ? | ? | mods/cameo/rules/warcraft2.yaml |
| wc2_human_cannon_tower | wc2_human_scout_tower | ? | ? | mods/cameo/rules/warcraft2.yaml |
| wc2_human_elven_ranger | wc2_human_elven_archer | ? | ? | mods/cameo/rules/warcraft2.yaml |
| wc2_human_goldmine.bot | wc2_human_goldmine | ? | ? | mods/cameo/rules/warcraft2.yaml |
| wc2_human_guard_tower | wc2_human_scout_tower | ? | ? | mods/cameo/rules/warcraft2.yaml |
| wc2_human_paladin | wc2_human_knight | ? | ? | mods/cameo/rules/warcraft2.yaml |
| wc2_orc_cannon_tower | wc2_orc_watch_tower | ? | ? | mods/cameo/rules/warcraft2.yaml |
| wc2_orc_goldmine.bot | wc2_orc_goldmine | ? | ? | mods/cameo/rules/warcraft2.yaml |
| wc2_orc_guard_tower | wc2_orc_watch_tower | ? | ? | mods/cameo/rules/warcraft2.yaml |
| wc2_orc_ogremage | wc2_orc_ogre | ? | ? | mods/cameo/rules/warcraft2.yaml |
| wc2_orc_skeleton | wc2_orc_grunt | ? | ? | mods/cameo/rules/warcraft2.yaml |
| wc2_orc_troll_berserker | wc2_orc_troll_axethrower | ? | ? | mods/cameo/rules/warcraft2.yaml |
| wc2_orc_wall | wc2_human_wall | ? | ? | mods/cameo/rules/warcraft2.yaml |
| yakarmored.Husk | YAK.Husk | ? | ? | mods/cameo/rules/husks.yaml |
| yaktesla.Husk | YAK.Husk | ? | ? | mods/cameo/rules/husks.yaml |
| yrbfrt.bot | yrbfrt | ? | ? | mods/cameo/rules/redalert2.yaml |
| yrbfrt.bot2 | yrbfrt | ? | ? | mods/cameo/rules/redalert2.yaml |
| yrlunr.husk | ra2rock.husk | ? | ? | mods/cameo/rules/redalert2.yaml |
| yrnacnst | ra2gacnst | ? | ? | mods/cameo/rules/redalert2.yaml |
| yrsmin.empy | YRSMIN | ? | ? | mods/cameo/rules/redalert2.yaml |
| yuriinvisibleplane | U2 | ? | ? | mods/cameo/rules/redalert2.yaml |


## V4 — inherit chains deeper than 3

| actor | depth | file |
|---|---|---|
| 1TNK | 5 | mods/cameo/rules/redalert.yaml |
| 2TNK | 5 | mods/cameo/rules/redalert.yaml |
| 3TNK | 5 | mods/cameo/rules/redalert.yaml |
| 4TNK | 5 | mods/cameo/rules/redalert.yaml |
| 5TNK | 5 | mods/cameo/rules/redalert.yaml |
| A10 | 4 | mods/cameo/ContentPacks/TiberianDawn/GDI/rules/aircraft.yaml |
| A10.Husk | 4 | mods/cameo/rules/husks.yaml |
| A10Carrier | 4 | mods/cameo/ContentPacks/TiberianDawn/GDI/rules/aircraft.yaml |
| A10Carrier.Husk | 5 | mods/cameo/rules/husks.yaml |
| AFLD | 4 | mods/cameo/ContentPacks/TiberianDawn/Nod/rules/buildings.yaml |
| AGUN | 4 | mods/cameo/rules/redalert.yaml |
| AMMOBOX1 | 4 | mods/cameo/rules/civilian.yaml |
| AMMOBOX2 | 4 | mods/cameo/rules/civilian.yaml |
| AMMOBOX3 | 4 | mods/cameo/rules/civilian.yaml |
| APC | 4 | mods/cameo/ContentPacks/TiberianDawn/GDI/rules/vehicles.yaml |
| APC.Husk | 4 | mods/cameo/rules/husks.yaml |
| APWR | 4 | mods/cameo/rules/redalert.yaml |
| ARTY | 5 | mods/cameo/ContentPacks/TiberianDawn/Nod/rules/vehicles.yaml |
| ATWR | 5 | mods/cameo/ContentPacks/TiberianDawn/GDI/rules/buildings.yaml |
| BADR.Allies | 4 | mods/cameo/rules/redalert.yaml |
| BADR.Bomber | 5 | mods/cameo/rules/redalert.yaml |
| BADR.Japan | 4 | mods/cameo/rules/redalert.yaml |
| BADR.Soviet | 4 | mods/cameo/rules/redalert.yaml |
| BGGY | 4 | mods/cameo/ContentPacks/TiberianDawn/Nod/rules/vehicles.yaml |
| BGGY.Husk | 4 | mods/cameo/rules/husks.yaml |
| BIKE | 4 | mods/cameo/ContentPacks/TiberianDawn/Nod/rules/vehicles.yaml |
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
| CNCCA | 4 | mods/cameo/ContentPacks/TiberianDawn/GDI/rules/naval.yaml |
| CNCPT | 4 | mods/cameo/ContentPacks/TiberianDawn/GDI/rules/naval.yaml |
| CNCRSS | 4 | mods/cameo/ContentPacks/TiberianDawn/Nod/rules/naval.yaml |
| CNCSPEN | 4 | mods/cameo/ContentPacks/TiberianDawn/Nod/rules/buildings.yaml |
| CNCSS | 4 | mods/cameo/ContentPacks/TiberianDawn/Nod/rules/naval.yaml |
| CNCSYRD | 4 | mods/cameo/ContentPacks/TiberianDawn/GDI/rules/buildings.yaml |
| CTNK | 4 | mods/cameo/rules/redalert.yaml |
| DD | 4 | mods/cameo/rules/redalert.yaml |
| DELPHI | 5 | mods/cameo/rules/redalert.yaml |
| DOG | 5 | mods/cameo/rules/redalert.yaml |
| DOME.Allies | 4 | mods/cameo/rules/redalert.yaml |
| DOME.Japan | 4 | mods/cameo/rules/redalert.yaml |
| DOME.Soviet | 4 | mods/cameo/rules/redalert.yaml |
| E1 | 7 | mods/cameo/ContentPacks/TiberianDawn/GDI/rules/infantry.yaml |
| E1.GDI | 6 | mods/cameo/ContentPacks/TiberianDawn/GDI/rules/infantry.yaml |
| E1.NOD | 6 | mods/cameo/ContentPacks/TiberianDawn/Nod/rules/infantry.yaml |
| E2 | 5 | mods/cameo/ContentPacks/TiberianDawn/GDI/rules/infantry.yaml |
| E3 | 7 | mods/cameo/ContentPacks/TiberianDawn/GDI/rules/infantry.yaml |
| E3.GDI | 6 | mods/cameo/ContentPacks/TiberianDawn/GDI/rules/infantry.yaml |
| E3.Nod | 6 | mods/cameo/ContentPacks/TiberianDawn/Nod/rules/infantry.yaml |
| E4 | 5 | mods/cameo/ContentPacks/TiberianDawn/Nod/rules/infantry.yaml |
| E5 | 5 | mods/cameo/ContentPacks/TiberianDawn/Nod/rules/infantry.yaml |
| E6 | 5 | mods/cameo/ContentPacks/TiberianDawn/Shared/rules/infantry.yaml |
| E7 | 5 | mods/cameo/rules/redalert.yaml |
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
| EYE | 5 | mods/cameo/ContentPacks/TiberianDawn/GDI/rules/buildings.yaml |
| FACT.GDI | 5 | mods/cameo/ContentPacks/TiberianDawn/GDI/rules/buildings.yaml |
| FACT.NOD | 5 | mods/cameo/ContentPacks/TiberianDawn/Nod/rules/buildings.yaml |
| FCOM | 4 | mods/cameo/rules/tech.yaml |
| FCOM.Husk | 4 | mods/cameo/rules/tech.yaml |
| FIX.GDI | 4 | mods/cameo/ContentPacks/TiberianDawn/GDI/rules/buildings.yaml |
| FIX.Nod | 4 | mods/cameo/ContentPacks/TiberianDawn/Nod/rules/buildings.yaml |
| FTNK | 5 | mods/cameo/ContentPacks/TiberianDawn/Nod/rules/vehicles.yaml |
| FTRK | 4 | mods/cameo/rules/redalert.yaml |
| FTUR | 4 | mods/cameo/rules/redalert.yaml |
| GAP | 4 | mods/cameo/rules/redalert.yaml |
| GNRL | 5 | mods/cameo/rules/redalert.yaml |
| GTWR | 5 | mods/cameo/ContentPacks/TiberianDawn/GDI/rules/buildings.yaml |
| GUN | 5 | mods/cameo/ContentPacks/TiberianDawn/Nod/rules/buildings.yaml |
| HAND | 4 | mods/cameo/ContentPacks/TiberianDawn/Nod/rules/buildings.yaml |
| HARV.GDI | 5 | mods/cameo/ContentPacks/TiberianDawn/GDI/rules/vehicles.yaml |
| HARV.NOD | 5 | mods/cameo/ContentPacks/TiberianDawn/Nod/rules/vehicles.yaml |
| HBOX | 4 | mods/cameo/rules/redalert.yaml |
| HELI | 4 | mods/cameo/ContentPacks/TiberianDawn/Nod/rules/aircraft.yaml |
| HIND | 4 | mods/cameo/rules/redalert.yaml |
| HIND.Husk | 4 | mods/cameo/rules/husks.yaml |
| HOSP.Husk | 4 | mods/cameo/rules/tech.yaml |
| HPAD.GDI | 4 | mods/cameo/ContentPacks/TiberianDawn/GDI/rules/buildings.yaml |
| HPAD.NOD | 4 | mods/cameo/ContentPacks/TiberianDawn/Nod/rules/buildings.yaml |
| HQ.GDI | 4 | mods/cameo/ContentPacks/TiberianDawn/GDI/rules/buildings.yaml |
| HQ.Nod | 4 | mods/cameo/ContentPacks/TiberianDawn/Nod/rules/buildings.yaml |
| HTNK | 5 | mods/cameo/ContentPacks/TiberianDawn/GDI/rules/vehicles.yaml |
| INVISIBLEPLANE | 4 | mods/cameo/rules/tiberiansun.yaml |
| IRON | 4 | mods/cameo/rules/redalert.yaml |
| JEEP | 4 | mods/cameo/ContentPacks/TiberianDawn/GDI/rules/vehicles.yaml |
| JEEP.Husk | 4 | mods/cameo/rules/husks.yaml |
| JHIND | 4 | mods/cameo/rules/redalert.yaml |
| JHIND.Husk | 4 | mods/cameo/rules/husks.yaml |
| JPROC | 4 | mods/cameo/rules/redalert.yaml |
| JPWR | 4 | mods/cameo/rules/redalert.yaml |
| JSHRINE | 5 | mods/cameo/rules/redalert.yaml |
| KENN | 4 | mods/cameo/rules/redalert.yaml |
| Kotin | 5 | mods/cameo/rules/redalert.yaml |
| LST | 4 | mods/cameo/ContentPacks/TiberianDawn/GDI/rules/naval.yaml |
| LTNK | 5 | mods/cameo/ContentPacks/TiberianDawn/Nod/rules/vehicles.yaml |
| MCV.GDI | 5 | mods/cameo/ContentPacks/TiberianDawn/GDI/rules/vehicles.yaml |
| MCV.NOD | 5 | mods/cameo/ContentPacks/TiberianDawn/Nod/rules/vehicles.yaml |
| MECH | 5 | mods/cameo/rules/redalert.yaml |
| MEDI | 5 | mods/cameo/rules/redalert.yaml |
| MH60 | 4 | mods/cameo/rules/redalert.yaml |
| MIG | 4 | mods/cameo/rules/redalert.yaml |
| MIGNUKE | 4 | mods/cameo/rules/redalert.yaml |
| MISS | 4 | mods/cameo/rules/tech.yaml |
| MISS.Husk | 4 | mods/cameo/rules/tech.yaml |
| MLRS | 5 | mods/cameo/ContentPacks/TiberianDawn/GDI/rules/vehicles.yaml |
| MNLY | 4 | mods/cameo/rules/redalert.yaml |
| MODBGGY | 4 | mods/cameo/rules/redalert.yaml |
| MODBOMBER | 4 | mods/cameo/rules/redalert.yaml |
| MODBTR | 4 | mods/cameo/rules/redalert.yaml |
| MODCARR | 4 | mods/cameo/rules/redalert.yaml |
| MODCORE1 | 4 | mods/cameo/rules/redalert.yaml |
| MODCORE2 | 4 | mods/cameo/rules/redalert.yaml |
| MODCORE3 | 4 | mods/cameo/rules/redalert.yaml |
| MODCORE4 | 4 | mods/cameo/rules/redalert.yaml |
| MODCORE5 | 4 | mods/cameo/rules/redalert.yaml |
| MODCORE6 | 4 | mods/cameo/rules/redalert.yaml |
| MODCORE7 | 4 | mods/cameo/rules/redalert.yaml |
| MODGTNK | 4 | mods/cameo/rules/redalert.yaml |
| MODHOVER | 4 | mods/cameo/rules/redalert.yaml |
| MODKAMI | 4 | mods/cameo/rules/redalert.yaml |
| MODKUBEL | 4 | mods/cameo/rules/redalert.yaml |
| MODNANO | 5 | mods/cameo/rules/redalert.yaml |
| MODRAAFLD | 4 | mods/cameo/rules/redalert.yaml |
| MODRAWEAPJ | 4 | mods/cameo/rules/redalert.yaml |
| MODSPACEPORT | 5 | mods/cameo/rules/redalert.yaml |
| MODTIGER | 5 | mods/cameo/rules/redalert.yaml |
| MODTNKD | 5 | mods/cameo/rules/redalert.yaml |
| MODTYPEJ | 5 | mods/cameo/rules/redalert.yaml |
| MODWAVE | 5 | mods/cameo/rules/redalert.yaml |
| MOEBIUS | 5 | mods/cameo/rules/civilian.yaml |
| MRJ | 4 | mods/cameo/rules/redalert.yaml |
| MSLO | 5 | mods/cameo/rules/redalert.yaml |
| MSSM | 5 | mods/cameo/ContentPacks/TiberianDawn/Nod/rules/vehicles.yaml |
| MSUB | 4 | mods/cameo/rules/redalert.yaml |
| MTNK | 5 | mods/cameo/ContentPacks/TiberianDawn/GDI/rules/vehicles.yaml |
| MonsterTank | 4 | mods/cameo/rules/redalert.yaml |
| NUK2 | 4 | mods/cameo/ContentPacks/TiberianDawn/Shared/rules/buildings.yaml |
| NUKE | 4 | mods/cameo/ContentPacks/TiberianDawn/Shared/rules/buildings.yaml |
| NodLaserTurret | 5 | mods/cameo/ContentPacks/TiberianDawn/Nod/rules/buildings.yaml |
| OBLI | 5 | mods/cameo/ContentPacks/TiberianDawn/Nod/rules/buildings.yaml |
| OILB.Building | 4 | mods/cameo/rules/shared.yaml |
| OILB.Husk | 4 | mods/cameo/rules/tech.yaml |
| OILB.RA2 | 4 | mods/cameo/rules/redalert2.yaml |
| OILB.TS | 5 | mods/cameo/rules/tiberiansun.yaml |
| OILB.TS.MUTANT | 6 | mods/cameo/rules/tiberiansun.yaml |
| OILB.d2k | 5 | mods/cameo/ContentPacks/D2k/Shared/rules/buildings.yaml |
| ORCA | 4 | mods/cameo/ContentPacks/TiberianDawn/GDI/rules/aircraft.yaml |
| PBOX | 4 | mods/cameo/rules/redalert.yaml |
| PDOX | 5 | mods/cameo/rules/redalert.yaml |
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
| PROC.GDI | 5 | mods/cameo/ContentPacks/TiberianDawn/GDI/rules/buildings.yaml |
| PROC.NOD | 5 | mods/cameo/ContentPacks/TiberianDawn/Nod/rules/buildings.yaml |
| PT | 4 | mods/cameo/rules/redalert.yaml |
| PYLE | 4 | mods/cameo/ContentPacks/TiberianDawn/GDI/rules/buildings.yaml |
| QTNK | 5 | mods/cameo/rules/redalert.yaml |
| RA2ADOG | 5 | mods/cameo/rules/redalert2.yaml |
| RA2AMCV | 4 | mods/cameo/rules/redalert2.yaml |
| RA2APOC | 5 | mods/cameo/rules/redalert2.yaml |
| RA2BEAG | 4 | mods/cameo/rules/redalert2.yaml |
| RA2CLEG | 5 | mods/cameo/rules/redalert2.yaml |
| RA2CMIN | 4 | mods/cameo/rules/redalert2.yaml |
| RA2DESO | 5 | mods/cameo/rules/redalert2.yaml |
| RA2DOG | 5 | mods/cameo/rules/redalert2.yaml |
| RA2E1 | 5 | mods/cameo/rules/redalert2.yaml |
| RA2E2 | 5 | mods/cameo/rules/redalert2.yaml |
| RA2ENGINEER | 6 | mods/cameo/rules/redalert2.yaml |
| RA2FALC | 4 | mods/cameo/rules/redalert2.yaml |
| RA2FV | 4 | mods/cameo/rules/redalert2.yaml |
| RA2FVBOTCHRONO | 6 | mods/cameo/rules/redalert2.yaml |
| RA2FVBOTHMG | 5 | mods/cameo/rules/redalert2.yaml |
| RA2FVBOTMG | 5 | mods/cameo/rules/redalert2.yaml |
| RA2FVBOTMISS | 5 | mods/cameo/rules/redalert2.yaml |
| RA2FVBOTREP | 6 | mods/cameo/rules/redalert2.yaml |
| RA2HARV | 4 | mods/cameo/rules/redalert2.yaml |
| RA2HMGTK | 5 | mods/cameo/rules/redalert2.yaml |
| RA2HTK | 4 | mods/cameo/rules/redalert2.yaml |
| RA2HTNK | 5 | mods/cameo/rules/redalert2.yaml |
| RA2IVAN | 5 | mods/cameo/rules/redalert2.yaml |
| RA2MGTK | 5 | mods/cameo/rules/redalert2.yaml |
| RA2MTNK | 5 | mods/cameo/rules/redalert2.yaml |
| RA2SEAL | 5 | mods/cameo/rules/redalert2.yaml |
| RA2SMCV | 4 | mods/cameo/rules/redalert2.yaml |
| RA2SNIPE | 5 | mods/cameo/rules/redalert2.yaml |
| RA2SPY | 5 | mods/cameo/rules/redalert2.yaml |
| RA2SREF | 5 | mods/cameo/rules/redalert2.yaml |
| RA2TANY | 5 | mods/cameo/rules/redalert2.yaml |
| RA2TNKD | 5 | mods/cameo/rules/redalert2.yaml |
| RA2TTNK | 5 | mods/cameo/rules/redalert2.yaml |
| RA2V3 | 5 | mods/cameo/rules/redalert2.yaml |
| RA2ZEP | 4 | mods/cameo/rules/redalert2.yaml |
| RA2flakt | 5 | mods/cameo/rules/redalert2.yaml |
| RAAPC | 4 | mods/cameo/rules/redalert.yaml |
| RAARTY | 5 | mods/cameo/rules/redalert.yaml |
| RABIO | 4 | mods/cameo/rules/tech.yaml |
| RACHAN | 5 | mods/cameo/rules/redalert.yaml |
| RAE1 | 6 | mods/cameo/rules/redalert.yaml |
| RAE2 | 5 | mods/cameo/rules/redalert.yaml |
| RAE3 | 6 | mods/cameo/rules/redalert.yaml |
| RAE4 | 5 | mods/cameo/rules/redalert.yaml |
| RAE6 | 6 | mods/cameo/rules/redalert.yaml |
| RAFACT.ALLIES | 4 | mods/cameo/rules/redalert.yaml |
| RAFACT.Japan | 4 | mods/cameo/rules/redalert.yaml |
| RAFACT.SOVIET | 4 | mods/cameo/rules/redalert.yaml |
| RAFIX.Allies | 4 | mods/cameo/rules/redalert.yaml |
| RAFIX.Japan | 4 | mods/cameo/rules/redalert.yaml |
| RAFIX.Soviet | 4 | mods/cameo/rules/redalert.yaml |
| RAGUN | 4 | mods/cameo/rules/redalert.yaml |
| RAHARV.ALLIES | 5 | mods/cameo/rules/redalert.yaml |
| RAHARV.JAPAN | 5 | mods/cameo/rules/redalert.yaml |
| RAHARV.SOVIET | 5 | mods/cameo/rules/redalert.yaml |
| RAHELI | 4 | mods/cameo/rules/redalert.yaml |
| RAJE1 | 6 | mods/cameo/rules/redalert.yaml |
| RAJE3 | 5 | mods/cameo/rules/redalert.yaml |
| RAJE4 | 5 | mods/cameo/rules/redalert.yaml |
| RAJEEP | 4 | mods/cameo/rules/redalert.yaml |
| RALST | 4 | mods/cameo/rules/redalert.yaml |
| RAMAID | 6 | mods/cameo/rules/redalert.yaml |
| RAMCV.ALLIES | 5 | mods/cameo/rules/redalert.yaml |
| RAMCV.JAPAN | 5 | mods/cameo/rules/redalert.yaml |
| RAMCV.SOVIET | 5 | mods/cameo/rules/redalert.yaml |
| RAMGG | 4 | mods/cameo/rules/redalert.yaml |
| RAMISS | 5 | mods/cameo/rules/tech.yaml |
| RAPROC.ALLIES | 4 | mods/cameo/rules/redalert.yaml |
| RAPROC.SOVIET | 4 | mods/cameo/rules/redalert.yaml |
| RAPT | 4 | mods/cameo/rules/tiberiansun.yaml |
| RARE1 | 6 | mods/cameo/rules/redalert.yaml |
| RARE3 | 6 | mods/cameo/rules/redalert.yaml |
| RASAM | 4 | mods/cameo/rules/redalert.yaml |
| RASAMURAI | 5 | mods/cameo/rules/redalert.yaml |
| RASPY | 5 | mods/cameo/rules/redalert.yaml |
| RATRAN | 5 | mods/cameo/rules/redalert.yaml |
| RATRAN.Husk | 4 | mods/cameo/rules/redalert.yaml |
| RMBO.GDI | 6 | mods/cameo/ContentPacks/TiberianDawn/GDI/rules/infantry.yaml |
| RMBO.NOD | 6 | mods/cameo/ContentPacks/TiberianDawn/Nod/rules/infantry.yaml |
| ROCKETANGEL | 4 | mods/cameo/rules/redalert.yaml |
| ROCKETANGEL.husk | 4 | mods/cameo/rules/redalert.yaml |
| SAM | 5 | mods/cameo/ContentPacks/TiberianDawn/Nod/rules/buildings.yaml |
| SAPC | 4 | mods/cameo/rules/redalert.yaml |
| SCADEPT | 5 | mods/cameo/rules/starcraft.yaml |
| SCAMARANTH | 5 | mods/cameo/rules/starcraft.yaml |
| SCANALOGUE | 6 | mods/cameo/rules/starcraft.yaml |
| SCARBITER | 5 | mods/cameo/rules/starcraft.yaml |
| SCARBITERTRIBUNAL | 4 | mods/cameo/rules/starcraft.yaml |
| SCARCHON | 6 | mods/cameo/rules/starcraft.yaml |
| SCASSIMILATOR | 4 | mods/cameo/rules/starcraft.yaml |
| SCATREUS | 6 | mods/cameo/rules/starcraft.yaml |
| SCBARRACKSM | 5 | mods/cameo/rules/starcraft.yaml |
| SCBATTLECRUISER | 4 | mods/cameo/rules/starcraft.yaml |
| SCBEHEMOTH | 5 | mods/cameo/rules/starcraft.yaml |
| SCBROODLING | 5 | mods/cameo/rules/starcraft.yaml |
| SCBROODWEAVER | 5 | mods/cameo/rules/starcraft.yaml |
| SCBUNKER | 4 | mods/cameo/rules/starcraft.yaml |
| SCCARRIER | 5 | mods/cameo/rules/starcraft.yaml |
| SCCITADELOFADUN | 4 | mods/cameo/rules/starcraft.yaml |
| SCCOMMANDCENTER | 4 | mods/cameo/rules/starcraft.yaml |
| SCCOMMANDCENTERM | 4 | mods/cameo/rules/starcraft.yaml |
| SCCORRUPTOR | 5 | mods/cameo/rules/starcraft.yaml |
| SCCORSAIR | 5 | mods/cameo/rules/starcraft.yaml |
| SCCREEPCOLONY | 4 | mods/cameo/rules/starcraft.yaml |
| SCCREEPCOLONYDEFENSE | 5 | mods/cameo/rules/starcraft.yaml |
| SCCYBERNETICSCORE | 4 | mods/cameo/rules/starcraft.yaml |
| SCCYCLONE | 5 | mods/cameo/rules/starcraft.yaml |
| SCDARKTEMPLAR | 5 | mods/cameo/rules/starcraft.yaml |
| SCDEFILER | 6 | mods/cameo/rules/starcraft.yaml |
| SCDEFILERMOUND | 4 | mods/cameo/rules/starcraft.yaml |
| SCDEVOURER | 5 | mods/cameo/rules/starcraft.yaml |
| SCDRAGOON | 6 | mods/cameo/rules/starcraft.yaml |
| SCDREADSHROUD | 5 | mods/cameo/rules/starcraft.yaml |
| SCDRONE | 5 | mods/cameo/rules/starcraft.yaml |
| SCDROPSHIP | 5 | mods/cameo/rules/starcraft.yaml |
| SCENGINEERINGBAYM | 5 | mods/cameo/rules/starcraft.yaml |
| SCEPIGRAPH | 5 | mods/cameo/rules/starcraft.yaml |
| SCEVOLUTIONCHAMBER | 4 | mods/cameo/rules/starcraft.yaml |
| SCEXTRACTOR | 4 | mods/cameo/rules/starcraft.yaml |
| SCFACTORYM | 5 | mods/cameo/rules/starcraft.yaml |
| SCFIREBAT | 5 | mods/cameo/rules/starcraft.yaml |
| SCFLEETBEACON | 5 | mods/cameo/rules/starcraft.yaml |
| SCFORGE | 4 | mods/cameo/rules/starcraft.yaml |
| SCGATEWAY | 4 | mods/cameo/rules/starcraft.yaml |
| SCGHOST | 5 | mods/cameo/rules/starcraft.yaml |
| SCGLADIUS | 5 | mods/cameo/rules/starcraft.yaml |
| SCGOLIATH | 5 | mods/cameo/rules/starcraft.yaml |
| SCGOLIATH2 | 5 | mods/cameo/rules/starcraft.yaml |
| SCGOREKRAKEN | 5 | mods/cameo/rules/starcraft.yaml |
| SCGOREMAW | 7 | mods/cameo/rules/starcraft.yaml |
| SCGUARDIAN | 5 | mods/cameo/rules/starcraft.yaml |
| SCHARAKAN | 5 | mods/cameo/rules/starcraft.yaml |
| SCHATCHERY | 4 | mods/cameo/rules/starcraft.yaml |
| SCHERMIT | 7 | mods/cameo/rules/starcraft.yaml |
| SCHIGHTEMPLAR | 5 | mods/cameo/rules/starcraft.yaml |
| SCHYDRALISK | 6 | mods/cameo/rules/starcraft.yaml |
| SCHYDRALISKDEN | 4 | mods/cameo/rules/starcraft.yaml |
| SCIDOL | 6 | mods/cameo/rules/starcraft.yaml |
| SCINFESTEDCOMMANDCENTER | 4 | mods/cameo/rules/starcraft.yaml |
| SCINFESTEDTERRAN | 5 | mods/cameo/rules/starcraft.yaml |
| SCINTERCEPTOR | 4 | mods/cameo/rules/starcraft.yaml |
| SCJIMRAYNOR | 5 | mods/cameo/rules/starcraft.yaml |
| SCKERRIGANZERG | 6 | mods/cameo/rules/starcraft.yaml |
| SCLEGIONNAIRE | 5 | mods/cameo/rules/starcraft.yaml |
| SCLURKER | 7 | mods/cameo/rules/starcraft.yaml |
| SCMADCAP | 5 | mods/cameo/rules/starcraft.yaml |
| SCMANIFOLD | 6 | mods/cameo/rules/starcraft.yaml |
| SCMARINE | 5 | mods/cameo/rules/starcraft.yaml |
| SCMATADOR | 5 | mods/cameo/rules/starcraft.yaml |
| SCMEDIC | 5 | mods/cameo/rules/starcraft.yaml |
| SCMEDIVAC | 5 | mods/cameo/rules/starcraft.yaml |
| SCMISSILETURRET | 4 | mods/cameo/rules/starcraft.yaml |
| SCMUTALISK | 5 | mods/cameo/rules/starcraft.yaml |
| SCNEXUS | 4 | mods/cameo/rules/starcraft.yaml |
| SCNYDUSCANAL | 4 | mods/cameo/rules/starcraft.yaml |
| SCOBSERVATORY | 4 | mods/cameo/rules/starcraft.yaml |
| SCOBSERVER | 5 | mods/cameo/rules/starcraft.yaml |
| SCOVERLORD | 5 | mods/cameo/rules/starcraft.yaml |
| SCOVERMIND | 5 | mods/cameo/rules/starcraft.yaml |
| SCPATRIARCH | 5 | mods/cameo/rules/starcraft.yaml |
| SCPHOBOS | 4 | mods/cameo/rules/starcraft.yaml |
| SCPHOTONCANNON | 4 | mods/cameo/rules/starcraft.yaml |
| SCPMCV | 4 | mods/cameo/rules/starcraft.yaml |
| SCPOSITRON | 6 | mods/cameo/rules/starcraft.yaml |
| SCPROBE | 4 | mods/cameo/rules/starcraft.yaml |
| SCPYLON | 4 | mods/cameo/rules/starcraft.yaml |
| SCPYTHEAN | 4 | mods/cameo/rules/starcraft.yaml |
| SCQUEEN | 5 | mods/cameo/rules/starcraft.yaml |
| SCQUEENSNEST | 4 | mods/cameo/rules/starcraft.yaml |
| SCRAVEN | 4 | mods/cameo/rules/starcraft.yaml |
| SCREAPER | 5 | mods/cameo/rules/starcraft.yaml |
| SCREAVER | 6 | mods/cameo/rules/starcraft.yaml |
| SCROBOTICSFACILITY | 4 | mods/cameo/rules/starcraft.yaml |
| SCROBOTICSSUPPORTBAY | 4 | mods/cameo/rules/starcraft.yaml |
| SCSCIENCEFACILITYM | 5 | mods/cameo/rules/starcraft.yaml |
| SCSCIENCEVESSEL | 4 | mods/cameo/rules/starcraft.yaml |
| SCSCOURGE | 5 | mods/cameo/rules/starcraft.yaml |
| SCSCOURGEDRONE | 6 | mods/cameo/rules/starcraft.yaml |
| SCSCOUT | 5 | mods/cameo/rules/starcraft.yaml |
| SCSCV | 4 | mods/cameo/rules/starcraft.yaml |
| SCSENTINEL | 4 | mods/cameo/rules/starcraft.yaml |
| SCSENTINELM | 5 | mods/cameo/rules/starcraft.yaml |
| SCSHIELDBATTERY | 4 | mods/cameo/rules/starcraft.yaml |
| SCSHRIEK | 5 | mods/cameo/rules/starcraft.yaml |
| SCSHUTTLE | 5 | mods/cameo/rules/starcraft.yaml |
| SCSIEGETANK | 5 | mods/cameo/rules/starcraft.yaml |
| SCSILVERTONGUE | 5 | mods/cameo/rules/starcraft.yaml |
| SCSPAWNINGPOOL | 4 | mods/cameo/rules/starcraft.yaml |
| SCSPIRE | 4 | mods/cameo/rules/starcraft.yaml |
| SCSPITHID | 5 | mods/cameo/rules/starcraft.yaml |
| SCSPOREMAW | 6 | mods/cameo/rules/starcraft.yaml |
| SCSTARGATE | 4 | mods/cameo/rules/starcraft.yaml |
| SCSTARPORTM | 5 | mods/cameo/rules/starcraft.yaml |
| SCSTARSHIPSOVEREIGN | 5 | mods/cameo/rules/starcraft.yaml |
| SCSUNDOG | 4 | mods/cameo/rules/starcraft.yaml |
| SCSUPPLYDEPOT | 4 | mods/cameo/rules/starcraft.yaml |
| SCSWARMLING | 5 | mods/cameo/rules/starcraft.yaml |
| SCTALON | 5 | mods/cameo/rules/starcraft.yaml |
| SCTEMPLARARCHIVES | 4 | mods/cameo/rules/starcraft.yaml |
| SCTMCV | 4 | mods/cameo/rules/starcraft.yaml |
| SCTerranMSLO | 5 | mods/cameo/rules/starcraft.yaml |
| SCULTRALISK | 7 | mods/cameo/rules/starcraft.yaml |
| SCULTRALISKCAVERN | 4 | mods/cameo/rules/starcraft.yaml |
| SCVALKYRIE | 4 | mods/cameo/rules/starcraft.yaml |
| SCVULTURE | 4 | mods/cameo/rules/starcraft.yaml |
| SCWRAITH | 4 | mods/cameo/rules/starcraft.yaml |
| SCWRAITHDRONE | 4 | mods/cameo/rules/starcraft.yaml |
| SCWYVERN | 4 | mods/cameo/rules/starcraft.yaml |
| SCZEALOT | 5 | mods/cameo/rules/starcraft.yaml |
| SCZERATUL | 5 | mods/cameo/rules/starcraft.yaml |
| SCZERGLING | 5 | mods/cameo/rules/starcraft.yaml |
| SCZMCV | 4 | mods/cameo/rules/starcraft.yaml |
| SCvoidray | 5 | mods/cameo/rules/starcraft.yaml |
| SHOK | 5 | mods/cameo/rules/redalert.yaml |
| SILO | 4 | mods/cameo/ContentPacks/TiberianDawn/Shared/rules/buildings.yaml |
| SNIPER | 5 | mods/cameo/rules/redalert.yaml |
| SS | 4 | mods/cameo/rules/redalert.yaml |
| STNK | 5 | mods/cameo/ContentPacks/TiberianDawn/Nod/rules/vehicles.yaml |
| TECH1 | 5 | mods/cameo/rules/redalert.yaml |
| TECHBCANNON | 4 | mods/cameo/rules/tech.yaml |
| TECHBCANNON2 | 5 | mods/cameo/rules/tech.yaml |
| TECN | 5 | mods/cameo/rules/civilian.yaml |
| TMPL | 5 | mods/cameo/ContentPacks/TiberianDawn/Nod/rules/buildings.yaml |
| TRAN | 5 | mods/cameo/ContentPacks/TiberianDawn/Shared/rules/aircraft.yaml |
| TRAN.Husk | 4 | mods/cameo/ContentPacks/TiberianDawn/Shared/rules/aircraft.yaml |
| TS1TNK | 5 | mods/cameo/rules/tiberiansun.yaml |
| TS2TNK | 5 | mods/cameo/rules/tiberiansun.yaml |
| TS3TNK | 5 | mods/cameo/rules/tiberiansun.yaml |
| TS4TNK | 4 | mods/cameo/rules/tiberiansun.yaml |
| TS4TNK2 | 4 | mods/cameo/rules/tiberiansun.yaml |
| TSAPACHECABAL | 4 | mods/cameo/rules/tiberiansun.yaml |
| TSAPACHEMUTANT | 4 | mods/cameo/rules/tiberiansun.yaml |
| TSAPACHENOD | 4 | mods/cameo/rules/tiberiansun.yaml |
| TSAPC | 4 | mods/cameo/rules/tiberiansun.yaml |
| TSARND | 5 | mods/cameo/rules/tiberiansun.yaml |
| TSART2 | 5 | mods/cameo/rules/tiberiansun.yaml |
| TSART2CABAL | 4 | mods/cameo/rules/tiberiansun.yaml |
| TSARTY | 5 | mods/cameo/rules/tiberiansun.yaml |
| TSBGGY | 4 | mods/cameo/rules/tiberiansun.yaml |
| TSBIKE | 4 | mods/cameo/rules/tiberiansun.yaml |
| TSCAR2 | 4 | mods/cameo/rules/tiberiansun.yaml |
| TSCHAMSPY | 5 | mods/cameo/rules/tiberiansun.yaml |
| TSCYBORG | 5 | mods/cameo/rules/tiberiansun.yaml |
| TSCYC2 | 6 | mods/cameo/rules/tiberiansun.yaml |
| TSDEFENDER | 4 | mods/cameo/rules/tiberiansun.yaml |
| TSDOGGIE | 5 | mods/cameo/rules/tiberiansun.yaml |
| TSDOGGIEW | 6 | mods/cameo/rules/tiberiansun.yaml |
| TSDissolver | 4 | mods/cameo/rules/tiberiansun.yaml |
| TSE1 | 5 | mods/cameo/rules/tiberiansun.yaml |
| TSE1.GDI | 6 | mods/cameo/rules/tiberiansun.yaml |
| TSE1.NOD | 6 | mods/cameo/rules/tiberiansun.yaml |
| TSE1PARA | 6 | mods/cameo/rules/tiberiansun.yaml |
| TSE2 | 5 | mods/cameo/rules/tiberiansun.yaml |
| TSE2PARA | 6 | mods/cameo/rules/tiberiansun.yaml |
| TSE3 | 5 | mods/cameo/rules/tiberiansun.yaml |
| TSE3.Nod | 6 | mods/cameo/rules/tiberiansun.yaml |
| TSENFORCER | 5 | mods/cameo/rules/tiberiansun.yaml |
| TSENGINEECABAL | 7 | mods/cameo/rules/tiberiansun.yaml |
| TSENGINEER | 6 | mods/cameo/rules/tiberiansun.yaml |
| TSENGINEER.GDI | 7 | mods/cameo/rules/tiberiansun.yaml |
| TSENGINEER2 | 7 | mods/cameo/rules/tiberiansun.yaml |
| TSENGINEERMUTANT | 7 | mods/cameo/rules/tiberiansun.yaml |
| TSFTNK | 5 | mods/cameo/rules/tiberiansun.yaml |
| TSGHOST | 5 | mods/cameo/rules/tiberiansun.yaml |
| TSGHOSTSP | 6 | mods/cameo/rules/tiberiansun.yaml |
| TSGTCNSTCABAL | 4 | mods/cameo/rules/tiberiansun.yaml |
| TSGTPOWR | 4 | mods/cameo/rules/tiberiansun.yaml |
| TSGTSILOCABAL | 4 | mods/cameo/rules/tiberiansun.yaml |
| TSGTSILONOD | 4 | mods/cameo/rules/tiberiansun.yaml |
| TSHAMMERHEAD | 4 | mods/cameo/rules/tiberiansun.yaml |
| TSHARV | 5 | mods/cameo/rules/tiberiansun.yaml |
| TSHARVCABAL | 5 | mods/cameo/rules/tiberiansun.yaml |
| TSHARVMUTANT | 5 | mods/cameo/rules/tiberiansun.yaml |
| TSHARVNOD | 5 | mods/cameo/rules/tiberiansun.yaml |
| TSHELI | 4 | mods/cameo/rules/tiberiansun.yaml |
| TSHIND | 4 | mods/cameo/rules/tiberiansun.yaml |
| TSHMEC | 4 | mods/cameo/rules/tiberiansun.yaml |
| TSHVR | 5 | mods/cameo/rules/tiberiansun.yaml |
| TSJUGG | 5 | mods/cameo/rules/tiberiansun.yaml |
| TSJUGG2 | 5 | mods/cameo/rules/tiberiansun.yaml |
| TSKODK | 4 | mods/cameo/rules/tiberiansun.yaml |
| TSLA | 4 | mods/cameo/rules/redalert.yaml |
| TSLASSPID | 4 | mods/cameo/rules/tiberiansun.yaml |
| TSMCVCABAL | 5 | mods/cameo/rules/tiberiansun.yaml |
| TSMCVGDI | 4 | mods/cameo/rules/tiberiansun.yaml |
| TSMCVMUTANT | 5 | mods/cameo/rules/tiberiansun.yaml |
| TSMCVNOD | 5 | mods/cameo/rules/tiberiansun.yaml |
| TSMEDIC | 6 | mods/cameo/rules/tiberiansun.yaml |
| TSMHIJACK | 5 | mods/cameo/rules/tiberiansun.yaml |
| TSMKII | 4 | mods/cameo/rules/tiberiansun.yaml |
| TSMMCH | 5 | mods/cameo/rules/tiberiansun.yaml |
| TSMUTANT | 5 | mods/cameo/rules/tiberiansun.yaml |
| TSMUTANT3 | 5 | mods/cameo/rules/tiberiansun.yaml |
| TSMUTANT4 | 5 | mods/cameo/rules/tiberiansun.yaml |
| TSMUTANTSP | 6 | mods/cameo/rules/tiberiansun.yaml |
| TSMUTANTW | 6 | mods/cameo/rules/tiberiansun.yaml |
| TSMWMN | 5 | mods/cameo/rules/tiberiansun.yaml |
| TSMWMNSP | 6 | mods/cameo/rules/tiberiansun.yaml |
| TSMutVisceroid | 4 | mods/cameo/rules/tiberiansun.yaml |
| TSNTAPWR | 4 | mods/cameo/rules/tiberiansun.yaml |
| TSNTPOWR | 4 | mods/cameo/rules/tiberiansun.yaml |
| TSNTPOWRCABAL | 4 | mods/cameo/rules/tiberiansun.yaml |
| TSNTPOWRMUTANT | 4 | mods/cameo/rules/tiberiansun.yaml |
| TSORCA | 4 | mods/cameo/rules/tiberiansun.yaml |
| TSORCAB | 4 | mods/cameo/rules/tiberiansun.yaml |
| TSPITBULL | 4 | mods/cameo/rules/tiberiansun.yaml |
| TSRAILCOM | 5 | mods/cameo/rules/tiberiansun.yaml |
| TSREAPER | 4 | mods/cameo/rules/tiberiansun.yaml |
| TSREPAIR | 4 | mods/cameo/rules/tiberiansun.yaml |
| TSREPAIRCABAL | 5 | mods/cameo/rules/tiberiansun.yaml |
| TSRIOTT | 5 | mods/cameo/rules/tiberiansun.yaml |
| TSSAPC | 4 | mods/cameo/rules/tiberiansun.yaml |
| TSSAPCCABAL | 4 | mods/cameo/rules/tiberiansun.yaml |
| TSSCRIN | 4 | mods/cameo/rules/tiberiansun.yaml |
| TSSCRINCABAL | 4 | mods/cameo/rules/tiberiansun.yaml |
| TSSHOTGUNCOM | 5 | mods/cameo/rules/tiberiansun.yaml |
| TSSMECH | 5 | mods/cameo/rules/tiberiansun.yaml |
| TSSONIC | 5 | mods/cameo/rules/tiberiansun.yaml |
| TSSTNK | 5 | mods/cameo/rules/tiberiansun.yaml |
| TSSUBTANK | 5 | mods/cameo/rules/tiberiansun.yaml |
| TST1000 | 5 | mods/cameo/rules/tiberiansun.yaml |
| TSTALTITAN | 5 | mods/cameo/rules/tiberiansun.yaml |
| TSTALWOLV | 5 | mods/cameo/rules/tiberiansun.yaml |
| TSTOXINTROOP | 5 | mods/cameo/rules/tiberiansun.yaml |
| TSTRAN | 4 | mods/cameo/rules/tiberiansun.yaml |
| TSTRNSPORT | 4 | mods/cameo/rules/tiberiansun.yaml |
| TSTRNSPORTMUTANT | 4 | mods/cameo/rules/tiberiansun.yaml |
| TSTTNK | 5 | mods/cameo/rules/tiberiansun.yaml |
| TSTTNKCABAL | 5 | mods/cameo/rules/tiberiansun.yaml |
| TSUMAGON | 6 | mods/cameo/rules/tiberiansun.yaml |
| TSUMAGONSP | 7 | mods/cameo/rules/tiberiansun.yaml |
| TSWEEDGUY | 5 | mods/cameo/rules/tiberiansun.yaml |
| TSWINI2 | 5 | mods/cameo/rules/tiberiansun.yaml |
| TSZONEORCA | 4 | mods/cameo/rules/tiberiansun.yaml |
| TSZONETROOPER | 5 | mods/cameo/rules/tiberiansun.yaml |
| TTNK | 5 | mods/cameo/rules/redalert.yaml |
| TTNK2 | 5 | mods/cameo/rules/redalert.yaml |
| U3 | 4 | mods/cameo/rules/redalert.yaml |
| V19.Husk | 4 | mods/cameo/rules/tech.yaml |
| V2RL | 5 | mods/cameo/rules/redalert.yaml |
| V2RLNUKE | 5 | mods/cameo/rules/redalert.yaml |
| VICE | 4 | mods/cameo/rules/civilian.yaml |
| VOLKOV | 5 | mods/cameo/rules/redalert.yaml |
| WEAP | 4 | mods/cameo/ContentPacks/TiberianDawn/GDI/rules/buildings.yaml |
| YAK | 4 | mods/cameo/rules/redalert.yaml |
| YRBORIS | 5 | mods/cameo/rules/redalert2.yaml |
| YRBRUTE | 5 | mods/cameo/rules/redalert2.yaml |
| YRCAOS | 4 | mods/cameo/rules/redalert2.yaml |
| YRDISK | 4 | mods/cameo/rules/redalert2.yaml |
| YRDISK.Husk | 4 | mods/cameo/rules/redalert2.yaml |
| YRGGI | 5 | mods/cameo/rules/redalert2.yaml |
| YRGTRP | 5 | mods/cameo/rules/redalert2.yaml |
| YRINIT | 5 | mods/cameo/rules/redalert2.yaml |
| YRLTNK | 5 | mods/cameo/rules/redalert2.yaml |
| YRMIND | 5 | mods/cameo/rules/redalert2.yaml |
| YRPCV | 4 | mods/cameo/rules/redalert2.yaml |
| YRSLAV | 5 | mods/cameo/rules/redalert2.yaml |
| YRSMIN | 4 | mods/cameo/rules/redalert2.yaml |
| YRTELE | 5 | mods/cameo/rules/redalert2.yaml |
| YRVIRUS | 5 | mods/cameo/rules/redalert2.yaml |
| YRYTNK | 4 | mods/cameo/rules/redalert2.yaml |
| YRYURI | 5 | mods/cameo/rules/redalert2.yaml |
| YRYURIX | 5 | mods/cameo/rules/redalert2.yaml |
| aa_mine.ordos | 4 | mods/cameo/ContentPacks/D2k/Ordos/rules/aircraft.yaml |
| aatank.nax2 | 4 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/rules/vehicles.yaml |
| aatrooper.ordos | 5 | mods/cameo/ContentPacks/D2k/Ordos/rules/infantry.yaml |
| aatur.latin | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/rules/buildings.yaml |
| acv.asian | 4 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/rules/vehicles.yaml |
| air_drone.ixian | 4 | mods/cameo/ContentPacks/D2k/Ixian/rules/aircraft.yaml |
| airfield.nax | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/rules/buildings.yaml |
| airfield.nax2 | 5 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/rules/buildings.yaml |
| alien.nax | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/rules/infantry.yaml |
| alliedcybertank | 5 | mods/cameo/rules/redalert.yaml |
| alliedmachinegunner | 5 | mods/cameo/rules/redalert.yaml |
| alligator.asian | 5 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/rules/infantry.yaml |
| apc.latin | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/rules/vehicles.yaml |
| apc.ordos | 5 | mods/cameo/ContentPacks/D2k/Ordos/rules/vehicles.yaml |
| apparition.ixian | 5 | mods/cameo/ContentPacks/D2k/Ixian/rules/vehicles.yaml |
| archer.asian | 5 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/rules/infantry.yaml |
| armor_harv.nax2 | 4 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/rules/infantry.yaml |
| armor_mg.nax2 | 4 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/rules/infantry.yaml |
| artillery_platform.ordos | 5 | mods/cameo/ContentPacks/D2k/Ordos/rules/buildings.yaml |
| arty.steel | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/rules/vehicles.yaml |
| asdf.asian | 5 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/rules/infantry.yaml |
| assault.nax | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/rules/vehicles.yaml |
| atankcann.nax | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/rules/infantry.yaml |
| athena.futu | 5 | mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/rules/vehicles.yaml |
| autogun_tank.ordos | 5 | mods/cameo/ContentPacks/D2k/Ordos/rules/vehicles.yaml |
| autogun_tank_small.ordos | 5 | mods/cameo/ContentPacks/D2k/Ordos/rules/vehicles.yaml |
| autogun_turret.ordos | 5 | mods/cameo/ContentPacks/D2k/Ordos/rules/buildings.yaml |
| banshee.ordos | 4 | mods/cameo/ContentPacks/D2k/Ordos/rules/aircraft.yaml |
| barr.nax | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/rules/buildings.yaml |
| barr.nax2 | 4 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/rules/buildings.yaml |
| bbomb.nax2 | 4 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/rules/aircraft.yaml |
| bbomb2_husk.nax2 | 4 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/rules/aircraft.yaml |
| bbomb3_husk.nax2 | 4 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/rules/aircraft.yaml |
| beer.nax | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/rules/buildings.yaml |
| beetle.nax2 | 4 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/rules/vehicles.yaml |
| beholder.steel | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/rules/vehicles.yaml |
| bf109.nax | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/rules/aircraft.yaml |
| bfg10k.steel | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/rules/buildings.yaml |
| bio.Husk | 4 | mods/cameo/rules/tech.yaml |
| blackwidow.futu | 5 | mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/rules/infantry.yaml |
| bmwbike.nax | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/rules/infantry.yaml |
| board_inf.steel | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/rules/infantry.yaml |
| bomber.ixian | 4 | mods/cameo/ContentPacks/D2k/Ixian/rules/aircraft.yaml |
| bomber_husk.asian | 4 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/rules/aircraft.yaml |
| bomber_minebomb.asian | 4 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/rules/aircraft.yaml |
| bomber_minebomb2.asian | 5 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/rules/aircraft.yaml |
| brad.nax | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/rules/vehicles.yaml |
| bradley.nax2 | 5 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/rules/vehicles.yaml |
| brummbar.nax | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/rules/vehicles.yaml |
| buggy.latin | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/rules/vehicles.yaml |
| bunk.nax | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/rules/buildings.yaml |
| burrito.latin | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/rules/vehicles.yaml |
| ca12hit.latin | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/rules/buildings.yaml |
| car.nax | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/rules/vehicles.yaml |
| cargoship.steel | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/rules/aircraft.yaml |
| carryall | 4 | mods/cameo/ContentPacks/D2k/Shared/rules/aircraft.yaml |
| carryall.ordos | 5 | mods/cameo/ContentPacks/D2k/Ordos/rules/aircraft.yaml |
| carryall.paradrop | 4 | mods/cameo/ContentPacks/D2k/Shared/rules/aircraft.yaml |
| carryall_reinforce.ordos | 4 | mods/cameo/ContentPacks/D2k/Ordos/rules/aircraft.yaml |
| cartel.latin | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/rules/vehicles.yaml |
| cgair.latin | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/rules/buildings.yaml |
| cgairf.asian | 4 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/rules/buildings.yaml |
| cgapwr.asian | 4 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/rules/buildings.yaml |
| cgauto.latin | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/rules/buildings.yaml |
| cgbrck.asian | 4 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/rules/buildings.yaml |
| cgchao.asian | 4 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/rules/buildings.yaml |
| cgchtw.asian | 5 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/rules/buildings.yaml |
| cgcnst.asian | 4 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/rules/buildings.yaml |
| cgcnst.latin | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/rules/buildings.yaml |
| cgdept.asian | 4 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/rules/buildings.yaml |
| cgdept.latin | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/rules/buildings.yaml |
| cgflam.asian | 5 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/rules/buildings.yaml |
| cghype.asian | 5 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/rules/buildings.yaml |
| cgionc.asian | 5 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/rules/buildings.yaml |
| cgmiac.asian | 5 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/rules/buildings.yaml |
| cgpile.latin | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/rules/buildings.yaml |
| cgplas.asian | 5 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/rules/buildings.yaml |
| cgpnch.asian | 5 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/rules/buildings.yaml |
| cgpow.latin | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/rules/buildings.yaml |
| cgpuls.asian | 5 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/rules/buildings.yaml |
| cgradr.asian | 4 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/rules/buildings.yaml |
| cgrail.asian | 5 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/rules/buildings.yaml |
| cgrefn.asian | 4 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/rules/buildings.yaml |
| cgspy.latin | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/rules/buildings.yaml |
| cgte.latin | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/rules/buildings.yaml |
| cgtech.asian | 4 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/rules/buildings.yaml |
| cgtnkr.asian | 4 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/rules/buildings.yaml |
| cgup.latin | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/rules/buildings.yaml |
| cgweap.asian | 5 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/rules/buildings.yaml |
| cgweap.latin | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/rules/buildings.yaml |
| cgyard.asian | 4 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/rules/buildings.yaml |
| cgyard.latin | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/rules/buildings.yaml |
| chem_troop.ordos | 5 | mods/cameo/ContentPacks/D2k/Ordos/rules/infantry.yaml |
| chembike | 4 | mods/cameo/ContentPacks/TiberianDawn/Nod/rules/vehicles.yaml |
| cheme3 | 5 | mods/cameo/ContentPacks/TiberianDawn/Nod/rules/infantry.yaml |
| chemssm | 5 | mods/cameo/ContentPacks/TiberianDawn/Nod/rules/vehicles.yaml |
| chemstnk | 5 | mods/cameo/ContentPacks/TiberianDawn/Nod/rules/vehicles.yaml |
| cobra.ordos | 5 | mods/cameo/ContentPacks/D2k/Ordos/rules/vehicles.yaml |
| cobra.steel | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/rules/vehicles.yaml |
| coiler.futu | 5 | mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/rules/vehicles.yaml |
| combat_siege_tank.ixian | 5 | mods/cameo/ContentPacks/D2k/Ixian/rules/vehicles.yaml |
| combat_tank.atreides | 6 | mods/cameo/ContentPacks/D2k/Atreides/rules/vehicles.yaml |
| combat_tank.harkonnen | 5 | mods/cameo/ContentPacks/D2k/Harkonnen/rules/vehicles.yaml |
| combat_tank.ixian | 5 | mods/cameo/ContentPacks/D2k/Ixian/rules/vehicles.yaml |
| combat_tank.ordos | 5 | mods/cameo/ContentPacks/D2k/Ordos/rules/vehicles.yaml |
| combat_tank_husk.atreides | 5 | mods/cameo/ContentPacks/D2k/Atreides/rules/vehicles.yaml |
| combat_tank_husk.harkonnen | 5 | mods/cameo/ContentPacks/D2k/Harkonnen/rules/vehicles.yaml |
| combat_tank_husk.ordos | 5 | mods/cameo/ContentPacks/D2k/Ordos/rules/vehicles.yaml |
| combat_tank_stealth.ordos | 5 | mods/cameo/ContentPacks/D2k/Ordos/rules/vehicles.yaml |
| commando.asian | 5 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/rules/infantry.yaml |
| conehead.nax | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/rules/infantry.yaml |
| conehead2.nax | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/rules/infantry.yaml |
| conpad.steel | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/rules/buildings.yaml |
| cons.nax | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/rules/infantry.yaml |
| construction_yard.ixian | 5 | mods/cameo/ContentPacks/D2k/Ixian/rules/buildings.yaml |
| construction_yard.ordos | 5 | mods/cameo/ContentPacks/D2k/Ordos/rules/buildings.yaml |
| contaminator.ordos | 5 | mods/cameo/ContentPacks/D2k/Ordos/rules/infantry.yaml |
| conyard.nax | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/rules/buildings.yaml |
| conyard.nax2 | 4 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/rules/buildings.yaml |
| cougar.steel | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/rules/vehicles.yaml |
| cruiser.steel | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/rules/aircraft.yaml |
| cruiser_f.steel | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/rules/aircraft.yaml |
| cryo.futu | 4 | mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/rules/aircraft.yaml |
| cryoleg.futu | 5 | mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/rules/infantry.yaml |
| crystal.nax2 | 5 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/rules/vehicles.yaml |
| cyberdog | 5 | mods/cameo/rules/redalert.yaml |
| d2k_barracks.ixian | 4 | mods/cameo/ContentPacks/D2k/Ixian/rules/buildings.yaml |
| d2k_barracks.ordos | 4 | mods/cameo/ContentPacks/D2k/Ordos/rules/buildings.yaml |
| d2k_mcv.husk | 4 | mods/cameo/ContentPacks/D2k/Shared/rules/vehicles.yaml |
| d2k_mcv.ixian | 5 | mods/cameo/ContentPacks/D2k/Ixian/rules/vehicles.yaml |
| d2k_mcv.ordos | 5 | mods/cameo/ContentPacks/D2k/Ordos/rules/vehicles.yaml |
| d2k_munitions.ixian | 4 | mods/cameo/ContentPacks/D2k/Ixian/rules/buildings.yaml |
| d2k_silo.atreides | 5 | mods/cameo/ContentPacks/D2k/Atreides/rules/buildings.yaml |
| d2k_silo.ixian | 5 | mods/cameo/ContentPacks/D2k/Ixian/rules/buildings.yaml |
| d2k_silo.ordos | 5 | mods/cameo/ContentPacks/D2k/Ordos/rules/buildings.yaml |
| d2k_tyrant.husk | 4 | mods/cameo/ContentPacks/D2k/Shared/rules/vehicles.yaml |
| dagger.steel | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/rules/vehicles.yaml |
| dairy.nax2 | 4 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/rules/buildings.yaml |
| dalek.nax2 | 4 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/rules/vehicles.yaml |
| defender.steel | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/rules/vehicles.yaml |
| devastator_husk.harkonnen | 4 | mods/cameo/ContentPacks/D2k/Harkonnen/rules/vehicles.yaml |
| deviator.ordos | 5 | mods/cameo/ContentPacks/D2k/Ordos/rules/vehicles.yaml |
| deviator_husk.ordos | 4 | mods/cameo/ContentPacks/D2k/Ordos/rules/vehicles.yaml |
| deviatormk2.ordos | 5 | mods/cameo/ContentPacks/D2k/Ordos/rules/vehicles.yaml |
| diablo.latin | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/rules/vehicles.yaml |
| dieglocke.nax2 | 4 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/rules/aircraft.yaml |
| dieglocke_husk.nax2 | 4 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/rules/aircraft.yaml |
| dragonfly.asian | 4 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/rules/vehicles.yaml |
| drmn.asian | 4 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/rules/vehicles.yaml |
| drone.ixian | 4 | mods/cameo/ContentPacks/D2k/Ixian/rules/aircraft.yaml |
| drone.nax | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/rules/aircraft.yaml |
| drone_husk.ixian | 4 | mods/cameo/ContentPacks/D2k/Ixian/rules/aircraft.yaml |
| duelist_tank.ixian | 5 | mods/cameo/ContentPacks/D2k/Ixian/rules/vehicles.yaml |
| duelist_tank_husk.ixian | 4 | mods/cameo/ContentPacks/D2k/Ixian/rules/vehicles.yaml |
| e3flamer | 6 | mods/cameo/rules/redalert.yaml |
| egcnst.futu | 4 | mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/rules/buildings.yaml |
| egpile.futu | 4 | mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/rules/buildings.yaml |
| egrefn.futu | 4 | mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/rules/buildings.yaml |
| egshock.futu | 5 | mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/rules/buildings.yaml |
| egtech.futu | 4 | mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/rules/buildings.yaml |
| egtf.futu | 4 | mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/rules/buildings.yaml |
| egweap2.futu | 5 | mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/rules/buildings.yaml |
| elitcadre | 5 | mods/cameo/rules/tiberiansun.yaml |
| enforcer.futu | 5 | mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/rules/infantry.yaml |
| eng.nax | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/rules/vehicles.yaml |
| engi.futu | 4 | mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/rules/infantry.yaml |
| engi.nax2 | 4 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/rules/infantry.yaml |
| engineer | 6 | mods/cameo/ContentPacks/D2k/Shared/rules/infantry.yaml |
| exorcistoitank | 5 | mods/cameo/rules/redalert.yaml |
| eye.ordos | 4 | mods/cameo/ContentPacks/D2k/Ordos/rules/aircraft.yaml |
| face_dancer.ordos | 5 | mods/cameo/ContentPacks/D2k/Ordos/rules/infantry.yaml |
| farasha.ixian | 4 | mods/cameo/ContentPacks/D2k/Ixian/rules/aircraft.yaml |
| farasha_drone.ixian | 4 | mods/cameo/ContentPacks/D2k/Ixian/rules/aircraft.yaml |
| fcons.nax | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/rules/infantry.yaml |
| fedaa.steel | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/rules/buildings.yaml |
| fedeng.futu | 7 | mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/rules/infantry.yaml |
| fedeng.steel | 7 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/rules/infantry.yaml |
| fedinf.steel | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/rules/infantry.yaml |
| fedturret.steel | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/rules/buildings.yaml |
| fftr.latin | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/rules/infantry.yaml |
| flak88.nax | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/rules/buildings.yaml |
| flam.asian | 5 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/rules/infantry.yaml |
| forgot_htnk_tur | 4 | mods/cameo/rules/tiberiansun.yaml |
| forgot_ltnk_tur | 4 | mods/cameo/rules/tiberiansun.yaml |
| forgot_mtnk_tur | 4 | mods/cameo/rules/tiberiansun.yaml |
| forgot_tower | 4 | mods/cameo/rules/tiberiansun.yaml |
| frank.nax | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/rules/infantry.yaml |
| fremen_creep | 5 | mods/cameo/ContentPacks/D2k/Shared/rules/infantry.yaml |
| frigate.paradrop | 4 | mods/cameo/ContentPacks/D2k/Shared/rules/aircraft.yaml |
| fthrow.latin | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/rules/infantry.yaml |
| ftnk.asian | 5 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/rules/vehicles.yaml |
| futu.futu | 4 | mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/rules/vehicles.yaml |
| gcore.nax2 | 5 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/rules/buildings.yaml |
| gdiarcher | 5 | mods/cameo/ContentPacks/TiberianDawn/GDI/rules/vehicles.yaml |
| gdiassaultapc | 5 | mods/cameo/ContentPacks/TiberianDawn/GDI/rules/vehicles.yaml |
| gdicarrier | 4 | mods/cameo/ContentPacks/TiberianDawn/GDI/rules/naval.yaml |
| gdiempgrenadier | 5 | mods/cameo/ContentPacks/TiberianDawn/GDI/rules/infantry.yaml |
| gdiexosuit | 5 | mods/cameo/ContentPacks/TiberianDawn/GDI/rules/vehicles.yaml |
| gdifirehawk | 4 | mods/cameo/ContentPacks/TiberianDawn/GDI/rules/aircraft.yaml |
| gdihavoc | 5 | mods/cameo/ContentPacks/TiberianDawn/GDI/rules/infantry.yaml |
| gdihumvee | 5 | mods/cameo/ContentPacks/TiberianDawn/GDI/rules/vehicles.yaml |
| gdimammoth3 | 5 | mods/cameo/ContentPacks/TiberianDawn/GDI/rules/vehicles.yaml |
| gdimissilesoldier | 5 | mods/cameo/ContentPacks/TiberianDawn/GDI/rules/infantry.yaml |
| gdiofficer | 5 | mods/cameo/ContentPacks/TiberianDawn/GDI/rules/infantry.yaml |
| gdipredator | 5 | mods/cameo/ContentPacks/TiberianDawn/GDI/rules/vehicles.yaml |
| gdirig | 4 | mods/cameo/ContentPacks/TiberianDawn/GDI/rules/vehicles.yaml |
| gdirigdrone | 4 | mods/cameo/ContentPacks/TiberianDawn/GDI/rules/aircraft.yaml |
| gdishotgunner | 5 | mods/cameo/ContentPacks/TiberianDawn/GDI/rules/infantry.yaml |
| gdisniper | 5 | mods/cameo/ContentPacks/TiberianDawn/GDI/rules/infantry.yaml |
| gggun.futu | 5 | mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/rules/buildings.yaml |
| ggmlt.futu | 5 | mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/rules/buildings.yaml |
| ggpowr.futu | 4 | mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/rules/buildings.yaml |
| giant_rk.nax2 | 5 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/rules/vehicles.yaml |
| gravity.nax2 | 4 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/rules/vehicles.yaml |
| gren.asian | 5 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/rules/infantry.yaml |
| grille.nax | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/rules/vehicles.yaml |
| grille.nax2 | 5 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/rules/vehicles.yaml |
| grun.steel | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/rules/aircraft.yaml |
| gunb.asian | 4 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/rules/naval.yaml |
| hakurei | 5 | mods/cameo/rules/redalert.yaml |
| halftrack.nax | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/rules/vehicles.yaml |
| hammertank | 5 | mods/cameo/rules/redalert.yaml |
| harbinger.futu | 4 | mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/rules/aircraft.yaml |
| harv.futu | 4 | mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/rules/vehicles.yaml |
| harv.latin | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/rules/vehicles.yaml |
| harv.ordos | 5 | mods/cameo/ContentPacks/D2k/Ordos/rules/vehicles.yaml |
| harv2.futu | 4 | mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/rules/vehicles.yaml |
| harvester.ixian | 5 | mods/cameo/ContentPacks/D2k/Ixian/rules/vehicles.yaml |
| harvester_husk.ixian | 4 | mods/cameo/ContentPacks/D2k/Ixian/rules/vehicles.yaml |
| haunebu.nax2 | 4 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/rules/aircraft.yaml |
| haunebu2.nax2 | 4 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/rules/aircraft.yaml |
| haunebu2_husk.nax2 | 4 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/rules/aircraft.yaml |
| haunebu_husk.nax2 | 4 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/rules/aircraft.yaml |
| heavy_factory | 4 | mods/cameo/ContentPacks/D2k/Shared/rules/buildings.yaml |
| heavy_factory.ixian | 5 | mods/cameo/ContentPacks/D2k/Ixian/rules/buildings.yaml |
| heavy_factory.ordos | 5 | mods/cameo/ContentPacks/D2k/Ordos/rules/buildings.yaml |
| heavy_inf.ixian | 5 | mods/cameo/ContentPacks/D2k/Ixian/rules/infantry.yaml |
| heavy_rocket_raider.ixian | 4 | mods/cameo/ContentPacks/D2k/Ixian/rules/vehicles.yaml |
| heavyaatank | 4 | mods/cameo/rules/redalert.yaml |
| heavycombattank.ordos | 5 | mods/cameo/ContentPacks/D2k/Ordos/rules/vehicles.yaml |
| heavydrone_husk.ixian | 4 | mods/cameo/ContentPacks/D2k/Ixian/rules/aircraft.yaml |
| hetzer.nax | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/rules/vehicles.yaml |
| high_tech_factory.ixian | 5 | mods/cameo/ContentPacks/D2k/Ixian/rules/buildings.yaml |
| high_tech_factory.ordos | 5 | mods/cameo/ContentPacks/D2k/Ordos/rules/buildings.yaml |
| hmg.nax | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/rules/infantry.yaml |
| hole_small.nax2 | 4 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/rules/infantry.yaml |
| horten_bomber.nax | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/rules/aircraft.yaml |
| hotwi.asian | 5 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/rules/vehicles.yaml |
| hummer.steel | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/rules/vehicles.yaml |
| humvee.latin | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/rules/vehicles.yaml |
| hvrt.futu | 4 | mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/rules/vehicles.yaml |
| ifv.futu | 5 | mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/rules/vehicles.yaml |
| imperial.nax | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/rules/vehicles.yaml |
| inspect.steel | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/rules/aircraft.yaml |
| interceptor.nax | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/rules/aircraft.yaml |
| jagdpanzer.nax | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/rules/vehicles.yaml |
| jager.nax2 | 5 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/rules/vehicles.yaml |
| jammer.ixian | 5 | mods/cameo/ContentPacks/D2k/Ixian/rules/vehicles.yaml |
| japan_shrine_minitank | 5 | mods/cameo/rules/redalert.yaml |
| japancarrier | 4 | mods/cameo/rules/redalert.yaml |
| japanspeedboat | 4 | mods/cameo/rules/redalert.yaml |
| japrif.asian | 5 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/rules/infantry.yaml |
| javelinsoldier.futu | 5 | mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/rules/infantry.yaml |
| jballista | 5 | mods/cameo/rules/redalert.yaml |
| jballistat | 4 | mods/cameo/rules/redalert.yaml |
| jmgnest | 4 | mods/cameo/rules/redalert.yaml |
| jsuperbomber | 4 | mods/cameo/rules/redalert.yaml |
| jsuperbomber.Husk | 4 | mods/cameo/rules/husks.yaml |
| kami.asian | 4 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/rules/aircraft.yaml |
| kami_asdf.asian | 5 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/rules/aircraft.yaml |
| kami_chemical.asian | 5 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/rules/aircraft.yaml |
| karrier.asian | 4 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/rules/naval.yaml |
| katy.steel | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/rules/vehicles.yaml |
| kingtiger.nax | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/rules/vehicles.yaml |
| ksub.asian | 4 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/rules/naval.yaml |
| kubel.nax | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/rules/vehicles.yaml |
| landcarr.futu | 5 | mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/rules/vehicles.yaml |
| landcarr_drone.futu | 5 | mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/rules/aircraft.yaml |
| large_gun_turret.ixian | 5 | mods/cameo/ContentPacks/D2k/Ixian/rules/buildings.yaml |
| lars.latin | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/rules/vehicles.yaml |
| laser_tank.ordos | 5 | mods/cameo/ContentPacks/D2k/Ordos/rules/vehicles.yaml |
| lasert.nax2 | 4 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/rules/vehicles.yaml |
| launchpad.futu | 4 | mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/rules/buildings.yaml |
| leech.ordos | 5 | mods/cameo/ContentPacks/D2k/Ordos/rules/infantry.yaml |
| light_factory | 4 | mods/cameo/ContentPacks/D2k/Shared/rules/buildings.yaml |
| light_factory.ordos | 5 | mods/cameo/ContentPacks/D2k/Ordos/rules/buildings.yaml |
| light_inf | 5 | mods/cameo/ContentPacks/D2k/Shared/rules/infantry.yaml |
| litt.nax | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/rules/infantry.yaml |
| lsub.asian | 4 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/rules/naval.yaml |
| ltnk.latin | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/rules/vehicles.yaml |
| lunar.nax2 | 5 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/rules/infantry.yaml |
| lunar2.nax2 | 5 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/rules/infantry.yaml |
| lynx.asian | 5 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/rules/vehicles.yaml |
| machine_gun_turret.ixian | 5 | mods/cameo/ContentPacks/D2k/Ixian/rules/buildings.yaml |
| mako.steel | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/rules/vehicles.yaml |
| mammothbunker.husk | 4 | mods/cameo/rules/tech.yaml |
| manta.steel | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/rules/vehicles.yaml |
| manta_hunt.steel | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/rules/vehicles.yaml |
| maus.nax | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/rules/vehicles.yaml |
| mbt.futu | 5 | mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/rules/vehicles.yaml |
| mcv.futu | 4 | mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/rules/vehicles.yaml |
| mcv.latin | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/rules/vehicles.yaml |
| mcv.nax | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/rules/vehicles.yaml |
| mcv.nax2 | 4 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/rules/vehicles.yaml |
| me262.nax | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/rules/aircraft.yaml |
| mech_machinegun.futu | 5 | mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/rules/vehicles.yaml |
| mech_plasma.futu | 5 | mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/rules/vehicles.yaml |
| mecha.asian | 5 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/rules/vehicles.yaml |
| medium_gun_turret.ixian | 5 | mods/cameo/ContentPacks/D2k/Ixian/rules/buildings.yaml |
| mega.steel | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/rules/vehicles.yaml |
| merc.nax | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/rules/infantry.yaml |
| meteorray.nax2 | 5 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/rules/buildings.yaml |
| mig.latin | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/rules/aircraft.yaml |
| mili.latin | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/rules/infantry.yaml |
| missile_tank.ixian | 5 | mods/cameo/ContentPacks/D2k/Ixian/rules/vehicles.yaml |
| missile_tank_husk.ixian | 4 | mods/cameo/ContentPacks/D2k/Ixian/rules/vehicles.yaml |
| mlrs.asian | 5 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/rules/vehicles.yaml |
| modartyturret | 4 | mods/cameo/rules/redalert.yaml |
| modbomber.Husk | 4 | mods/cameo/rules/husks.yaml |
| modhip | 5 | mods/cameo/rules/redalert.yaml |
| modhip.husk | 4 | mods/cameo/rules/redalert.yaml |
| modhovert | 5 | mods/cameo/rules/redalert.yaml |
| modkami.Husk | 4 | mods/cameo/rules/husks.yaml |
| modkamimini.Husk | 4 | mods/cameo/rules/husks.yaml |
| modoitank | 5 | mods/cameo/rules/redalert.yaml |
| modpillboxfort | 4 | mods/cameo/rules/redalert.yaml |
| mongoose.ixian | 5 | mods/cameo/ContentPacks/D2k/Ixian/rules/vehicles.yaml |
| monkey.latin | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/rules/infantry.yaml |
| mortar.ordos | 5 | mods/cameo/ContentPacks/D2k/Ordos/rules/infantry.yaml |
| mortarbike.latin | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/rules/vehicles.yaml |
| mortarsoldier | 5 | mods/cameo/rules/redalert.yaml |
| mp40.nax | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/rules/infantry.yaml |
| mtnk.latin | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/rules/vehicles.yaml |
| muboat.nax | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/rules/naval.yaml |
| narco.latin | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/rules/infantry.yaml |
| naval.nax | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/rules/buildings.yaml |
| neocymek.ixian | 5 | mods/cameo/ContentPacks/D2k/Ixian/rules/vehicles.yaml |
| ngbunk2.latin | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/rules/buildings.yaml |
| ngdshktur.latin | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/rules/buildings.yaml |
| nodbuggy2 | 5 | mods/cameo/ContentPacks/TiberianDawn/Nod/rules/vehicles.yaml |
| nodftnk2 | 5 | mods/cameo/ContentPacks/TiberianDawn/Nod/rules/vehicles.yaml |
| nodlasercommando | 5 | mods/cameo/ContentPacks/TiberianDawn/Nod/rules/infantry.yaml |
| nodlasercorvette | 4 | mods/cameo/ContentPacks/TiberianDawn/Nod/rules/naval.yaml |
| nodltnk2 | 5 | mods/cameo/ContentPacks/TiberianDawn/Nod/rules/vehicles.yaml |
| nodspecter | 5 | mods/cameo/ContentPacks/TiberianDawn/Nod/rules/vehicles.yaml |
| nodvenom | 4 | mods/cameo/ContentPacks/TiberianDawn/Nod/rules/aircraft.yaml |
| nodvenom.husk | 4 | mods/cameo/ContentPacks/TiberianDawn/Nod/rules/aircraft.yaml |
| nokana.nax | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/rules/vehicles.yaml |
| nuketruk.latin | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/rules/vehicles.yaml |
| oilt.asian | 4 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/rules/vehicles.yaml |
| oldqtnk.steel | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/rules/vehicles.yaml |
| orion.futu | 5 | mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/rules/vehicles.yaml |
| outpost.ixian | 4 | mods/cameo/ContentPacks/D2k/Ixian/rules/buildings.yaml |
| outpost.ordos | 4 | mods/cameo/ContentPacks/D2k/Ordos/rules/buildings.yaml |
| palace.ordos | 5 | mods/cameo/ContentPacks/D2k/Ordos/rules/buildings.yaml |
| panth.asian | 4 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/rules/naval.yaml |
| panzer.nax | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/rules/vehicles.yaml |
| panzer.nax2 | 5 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/rules/vehicles.yaml |
| parv.nax2 | 5 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/rules/infantry.yaml |
| pelican.asian | 4 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/rules/aircraft.yaml |
| phal.futu | 4 | mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/rules/vehicles.yaml |
| phoenix.asian | 4 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/rules/aircraft.yaml |
| piercer.nax2 | 4 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/rules/aircraft.yaml |
| plast.asian | 5 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/rules/infantry.yaml |
| potnk.steel | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/rules/vehicles.yaml |
| powr.nax | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/rules/buildings.yaml |
| powr.nax2 | 4 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/rules/buildings.yaml |
| ptnk.asian | 5 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/rules/vehicles.yaml |
| pulv.asian | 5 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/rules/vehicles.yaml |
| python.ordos | 5 | mods/cameo/ContentPacks/D2k/Ordos/rules/vehicles.yaml |
| qabarr.steel | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/rules/buildings.yaml |
| qaclone.steel | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/rules/buildings.yaml |
| qacst.steel | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/rules/buildings.yaml |
| qaorbit.steel | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/rules/buildings.yaml |
| qapowr.steel | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/rules/buildings.yaml |
| qatech.steel | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/rules/buildings.yaml |
| qaweap.steel | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/rules/buildings.yaml |
| qcannon.steel | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/rules/buildings.yaml |
| qinf.steel | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/rules/infantry.yaml |
| qmcv.steel | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/rules/vehicles.yaml |
| qmin.steel | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/rules/vehicles.yaml |
| qoil.steel | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/rules/buildings.yaml |
| qref.steel | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/rules/buildings.yaml |
| qtradr.steel | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/rules/buildings.yaml |
| quadflak.nax | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/rules/infantry.yaml |
| quantumtank.steel | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/rules/vehicles.yaml |
| quas.asian | 4 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/rules/vehicles.yaml |
| quasfrig.asian | 4 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/rules/naval.yaml |
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
| ra2_tzep | 4 | mods/cameo/rules/redalert2.yaml |
| ra2_ycab | 4 | mods/cameo/rules/redalert2.yaml |
| ra2_ycab_demo | 4 | mods/cameo/rules/redalert2.yaml |
| ra2_ycab_driveby | 5 | mods/cameo/rules/redalert2.yaml |
| ra2aegis | 4 | mods/cameo/rules/redalert2.yaml |
| ra2asw | 4 | mods/cameo/rules/redalert2.yaml |
| ra2atesla | 5 | mods/cameo/rules/redalert2.yaml |
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
| ra2dron | 4 | mods/cameo/rules/redalert2.yaml |
| ra2dtruck.latin | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/rules/vehicles.yaml |
| ra2e2.black | 6 | mods/cameo/rules/redalert2.yaml |
| ra2engineer.asian | 7 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/rules/infantry.yaml |
| ra2engineer.latin | 7 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/rules/infantry.yaml |
| ra2engineer.soviet | 7 | mods/cameo/rules/redalert2.yaml |
| ra2engineer.yuri | 7 | mods/cameo/rules/redalert2.yaml |
| ra2gaairc | 4 | mods/cameo/rules/redalert2.yaml |
| ra2gacnst | 4 | mods/cameo/rules/redalert2.yaml |
| ra2gacsph | 4 | mods/cameo/rules/redalert2.yaml |
| ra2gadept | 4 | mods/cameo/rules/redalert2.yaml |
| ra2gagap | 5 | mods/cameo/rules/redalert2.yaml |
| ra2gaorep | 5 | mods/cameo/rules/redalert2.yaml |
| ra2gapile | 4 | mods/cameo/rules/redalert2.yaml |
| ra2gapill | 5 | mods/cameo/rules/redalert2.yaml |
| ra2gapowr | 4 | mods/cameo/rules/redalert2.yaml |
| ra2garefn | 4 | mods/cameo/rules/redalert2.yaml |
| ra2gaspysat | 4 | mods/cameo/rules/redalert2.yaml |
| ra2gatech | 4 | mods/cameo/rules/redalert2.yaml |
| ra2gaweap | 5 | mods/cameo/rules/redalert2.yaml |
| ra2gaweat | 5 | mods/cameo/rules/redalert2.yaml |
| ra2gayard | 4 | mods/cameo/rules/redalert2.yaml |
| ra2gtgcan | 5 | mods/cameo/rules/redalert2.yaml |
| ra2hind.latin | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/rules/aircraft.yaml |
| ra2hind_husk.latin | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/rules/aircraft.yaml |
| ra2hornet | 4 | mods/cameo/rules/redalert2.yaml |
| ra2hospt.husk | 4 | mods/cameo/rules/redalert2.yaml |
| ra2hyd | 4 | mods/cameo/rules/redalert2.yaml |
| ra2lcrf | 4 | mods/cameo/rules/redalert2.yaml |
| ra2leopard | 5 | mods/cameo/rules/redalert2.yaml |
| ra2machshop.husk | 4 | mods/cameo/rules/redalert2.yaml |
| ra2naairf | 4 | mods/cameo/rules/redalert2.yaml |
| ra2nacnst | 5 | mods/cameo/rules/redalert2.yaml |
| ra2nadept | 4 | mods/cameo/rules/redalert2.yaml |
| ra2naflak | 5 | mods/cameo/rules/redalert2.yaml |
| ra2nahand | 4 | mods/cameo/rules/redalert2.yaml |
| ra2nairon | 4 | mods/cameo/rules/redalert2.yaml |
| ra2nalasr | 5 | mods/cameo/rules/redalert2.yaml |
| ra2namisl | 5 | mods/cameo/rules/redalert2.yaml |
| ra2nanrct | 4 | mods/cameo/rules/redalert2.yaml |
| ra2napowr | 4 | mods/cameo/rules/redalert2.yaml |
| ra2napsis | 4 | mods/cameo/rules/redalert2.yaml |
| ra2naradr | 4 | mods/cameo/rules/redalert2.yaml |
| ra2narefn | 4 | mods/cameo/rules/redalert2.yaml |
| ra2nasam | 5 | mods/cameo/rules/redalert2.yaml |
| ra2natech | 4 | mods/cameo/rules/redalert2.yaml |
| ra2naweap | 5 | mods/cameo/rules/redalert2.yaml |
| ra2nayard | 4 | mods/cameo/rules/redalert2.yaml |
| ra2rock | 4 | mods/cameo/rules/redalert2.yaml |
| ra2sapc | 4 | mods/cameo/rules/redalert2.yaml |
| ra2shad | 4 | mods/cameo/rules/redalert2.yaml |
| ra2shk | 5 | mods/cameo/rules/redalert2.yaml |
| ra2shk.bot | 6 | mods/cameo/rules/redalert2.yaml |
| ra2shkhero | 6 | mods/cameo/rules/redalert2.yaml |
| ra2sidewind | 5 | mods/cameo/rules/redalert2.yaml |
| ra2sqd | 4 | mods/cameo/rules/redalert2.yaml |
| ra2sub | 4 | mods/cameo/rules/redalert2.yaml |
| ra2terror.latin | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/rules/infantry.yaml |
| ra2tesla | 5 | mods/cameo/rules/redalert2.yaml |
| ra2yclon | 5 | mods/cameo/rules/redalert2.yaml |
| ra_commissar | 5 | mods/cameo/rules/redalert.yaml |
| ra_cons_molo | 5 | mods/cameo/rules/redalert.yaml |
| ra_conscript_ak | 5 | mods/cameo/rules/redalert.yaml |
| ra_dragunov | 5 | mods/cameo/rules/redalert.yaml |
| ra_gigafactory | 4 | mods/cameo/rules/redalert.yaml |
| ra_grad | 5 | mods/cameo/rules/redalert.yaml |
| ra_heatraytank | 5 | mods/cameo/rules/redalert.yaml |
| ra_industrialminer | 6 | mods/cameo/rules/redalert.yaml |
| ra_kamov | 4 | mods/cameo/rules/redalert.yaml |
| ra_kamov.Husk | 4 | mods/cameo/rules/husks.yaml |
| ra_largeairfield | 4 | mods/cameo/rules/redalert.yaml |
| ra_zapper | 5 | mods/cameo/rules/redalert.yaml |
| radar.latin | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/rules/buildings.yaml |
| radar.nax | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/rules/buildings.yaml |
| radar.nax2 | 4 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/rules/buildings.yaml |
| raider.ordos | 4 | mods/cameo/ContentPacks/D2k/Ordos/rules/vehicles.yaml |
| railt.asian | 5 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/rules/vehicles.yaml |
| railt2.asian | 6 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/rules/vehicles.yaml |
| rammax.latin | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/rules/naval.yaml |
| rapierjumpjet | 4 | mods/cameo/rules/redalert.yaml |
| ratte.nax | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/rules/vehicles.yaml |
| reconranger | 4 | mods/cameo/rules/redalert.yaml |
| refin.nax | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/rules/buildings.yaml |
| refin.nax2 | 4 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/rules/buildings.yaml |
| refinery.atreides | 5 | mods/cameo/ContentPacks/D2k/Atreides/rules/buildings.yaml |
| refinery.ixian | 5 | mods/cameo/ContentPacks/D2k/Ixian/rules/buildings.yaml |
| refinery.ordos | 5 | mods/cameo/ContentPacks/D2k/Ordos/rules/buildings.yaml |
| repair_pad.ixian | 4 | mods/cameo/ContentPacks/D2k/Ixian/rules/buildings.yaml |
| repair_pad.ordos | 4 | mods/cameo/ContentPacks/D2k/Ordos/rules/buildings.yaml |
| research_centre.ixian | 5 | mods/cameo/ContentPacks/D2k/Ixian/rules/buildings.yaml |
| research_centre.ordos | 5 | mods/cameo/ContentPacks/D2k/Ordos/rules/buildings.yaml |
| resonance_drone.ixian | 4 | mods/cameo/ContentPacks/D2k/Ixian/rules/aircraft.yaml |
| resonance_drone_husk.ixian | 4 | mods/cameo/ContentPacks/D2k/Ixian/rules/aircraft.yaml |
| rifle.nax | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/rules/infantry.yaml |
| robot_cannon.futu | 5 | mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/rules/infantry.yaml |
| robot_missiles.futu | 5 | mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/rules/infantry.yaml |
| robot_shotgun.futu | 5 | mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/rules/infantry.yaml |
| rocket.nax | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/rules/buildings.yaml |
| rocket_raider.ixian | 4 | mods/cameo/ContentPacks/D2k/Ixian/rules/vehicles.yaml |
| rtruck.latin | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/rules/vehicles.yaml |
| runner.steel | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/rules/infantry.yaml |
| saboteur.ordos | 5 | mods/cameo/ContentPacks/D2k/Ordos/rules/infantry.yaml |
| samurai.asian | 5 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/rules/infantry.yaml |
| sarubia.nax | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/rules/vehicles.yaml |
| sausage.nax | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/rules/buildings.yaml |
| savi.steel | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/rules/vehicles.yaml |
| sc_zerg_larva | 5 | mods/cameo/rules/starcraft.yaml |
| scadept.shade | 6 | mods/cameo/rules/starcraft.yaml |
| scalpelAA.steel | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/rules/aircraft.yaml |
| scalpelMG.steel | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/rules/aircraft.yaml |
| scalpelQuantumCannon.steel | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/rules/aircraft.yaml |
| scmarauder | 5 | mods/cameo/rules/starcraft.yaml |
| scoutdrone.ordos | 5 | mods/cameo/ContentPacks/D2k/Ordos/rules/vehicles.yaml |
| scrapcar.latin | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/rules/vehicles.yaml |
| scrapcar2.latin | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/rules/vehicles.yaml |
| scrapcar2_demo.latin | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/rules/vehicles.yaml |
| scrapcar2_driveby.latin | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/rules/vehicles.yaml |
| scrapcar_demo.latin | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/rules/vehicles.yaml |
| scrapcar_driveby.latin | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/rules/vehicles.yaml |
| scsporecolony | 5 | mods/cameo/rules/starcraft.yaml |
| scsunkencolony | 5 | mods/cameo/rules/starcraft.yaml |
| scwarhound | 5 | mods/cameo/rules/starcraft.yaml |
| sheridan | 5 | mods/cameo/rules/redalert.yaml |
| shinobi.asian | 5 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/rules/infantry.yaml |
| shock_infantry.ixian | 5 | mods/cameo/ContentPacks/D2k/Ixian/rules/infantry.yaml |
| shock_raider.ixian | 5 | mods/cameo/ContentPacks/D2k/Ixian/rules/vehicles.yaml |
| shoe.nax | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/rules/vehicles.yaml |
| shogunexecutioner | 4 | mods/cameo/rules/redalert.yaml |
| shrek.nax | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/rules/infantry.yaml |
| siege_tank | 5 | mods/cameo/ContentPacks/D2k/Shared/rules/vehicles.yaml |
| siege_tank.husk | 4 | mods/cameo/ContentPacks/D2k/Shared/rules/vehicles.yaml |
| siege_tank.ixian | 5 | mods/cameo/ContentPacks/D2k/Ixian/rules/vehicles.yaml |
| sietch_creep | 4 | mods/cameo/ContentPacks/D2k/Shared/rules/buildings.yaml |
| sietch_creep_disabled | 5 | mods/cameo/ContentPacks/D2k/Shared/rules/buildings.yaml |
| slav.nax | 6 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/rules/infantry.yaml |
| slavemaster.nax | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/rules/infantry.yaml |
| sml.latin | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/rules/buildings.yaml |
| sonic_tank_husk.atreides | 4 | mods/cameo/ContentPacks/D2k/Atreides/rules/vehicles.yaml |
| spy.futu | 5 | mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/rules/infantry.yaml |
| ssmsub | 4 | mods/cameo/ContentPacks/TiberianDawn/Nod/rules/naval.yaml |
| stalker.steel | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/rules/vehicles.yaml |
| starport.ixian | 6 | mods/cameo/ContentPacks/D2k/Ixian/rules/buildings.yaml |
| starport.ordos | 6 | mods/cameo/ContentPacks/D2k/Ordos/rules/buildings.yaml |
| stealth_raider.ordos | 5 | mods/cameo/ContentPacks/D2k/Ordos/rules/vehicles.yaml |
| storm_infantry.ixian | 5 | mods/cameo/ContentPacks/D2k/Ixian/rules/infantry.yaml |
| storm_lasher.ixian | 5 | mods/cameo/ContentPacks/D2k/Ixian/rules/buildings.yaml |
| storm_raider.ixian | 5 | mods/cameo/ContentPacks/D2k/Ixian/rules/vehicles.yaml |
| sturmcann.nax2 | 5 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/rules/buildings.yaml |
| sturmtiger.nax | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/rules/vehicles.yaml |
| su57 | 4 | mods/cameo/rules/redalert.yaml |
| sub.latin | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/rules/naval.yaml |
| supercomputer.ixian | 5 | mods/cameo/ContentPacks/D2k/Ixian/rules/buildings.yaml |
| swarmer.ordos | 4 | mods/cameo/ContentPacks/D2k/Ordos/rules/aircraft.yaml |
| t30 | 4 | mods/cameo/rules/tkm.yaml |
| t72 | 5 | mods/cameo/rules/tkm.yaml |
| tanodharv | 5 | mods/cameo/ContentPacks/TiberianDawn/Nod/rules/vehicles.yaml |
| tcons.nax | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/rules/infantry.yaml |
| td_gdi_boxer | 4 | mods/cameo/ContentPacks/TiberianDawn/GDI/rules/vehicles.yaml |
| td_gdi_skyshield | 4 | mods/cameo/ContentPacks/TiberianDawn/GDI/rules/buildings.yaml |
| td_nod_templeprime | 5 | mods/cameo/ContentPacks/TiberianDawn/Nod/rules/buildings.yaml |
| tech.nax | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/rules/buildings.yaml |
| tech.nax2 | 4 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/rules/buildings.yaml |
| tek.nax | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/rules/buildings.yaml |
| tiger.nax | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/rules/vehicles.yaml |
| tiger.nax2 | 5 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/rules/vehicles.yaml |
| tkiller.asian | 5 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/rules/infantry.yaml |
| tkiller.latin | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/rules/infantry.yaml |
| tkmabrams | 5 | mods/cameo/rules/tkm.yaml |
| tkmabramspoint | 6 | mods/cameo/rules/tkm.yaml |
| tkmas42 | 4 | mods/cameo/rules/tkm.yaml |
| tkmbarracks | 4 | mods/cameo/rules/tkm.yaml |
| tkmbattlebus | 5 | mods/cameo/rules/tkm.yaml |
| tkmbigshiee | 4 | mods/cameo/rules/tkm.yaml |
| tkmbunker | 4 | mods/cameo/rules/tkm.yaml |
| tkmbunkerquadturret | 4 | mods/cameo/rules/tkm.yaml |
| tkmbunkertankturret | 4 | mods/cameo/rules/tkm.yaml |
| tkmdrone | 4 | mods/cameo/rules/tkm.yaml |
| tkmdronepodtruck | 5 | mods/cameo/rules/tkm.yaml |
| tkmengineer | 6 | mods/cameo/rules/tkm.yaml |
| tkmharv | 4 | mods/cameo/rules/tkm.yaml |
| tkmhuey | 4 | mods/cameo/rules/tkm.yaml |
| tkmhuey.husk | 4 | mods/cameo/rules/tkm.yaml |
| tkmjug | 5 | mods/cameo/rules/tkm.yaml |
| tkmkatyushalauncher | 5 | mods/cameo/rules/tkm.yaml |
| tkmmarine | 5 | mods/cameo/rules/tkm.yaml |
| tkmmcv | 4 | mods/cameo/rules/tkm.yaml |
| tkmmedictruck | 4 | mods/cameo/rules/tkm.yaml |
| tkmpowerplant | 4 | mods/cameo/rules/tkm.yaml |
| tkmquadtruck | 4 | mods/cameo/rules/tkm.yaml |
| tkmradar | 4 | mods/cameo/rules/tkm.yaml |
| tkmradartruck | 4 | mods/cameo/rules/tkm.yaml |
| tkmratflak | 4 | mods/cameo/rules/tkm.yaml |
| tkmratflakdeployed | 4 | mods/cameo/rules/tkm.yaml |
| tkmrefinery | 4 | mods/cameo/rules/tkm.yaml |
| tkmrepairtruck | 4 | mods/cameo/rules/tkm.yaml |
| tkmrifleman | 5 | mods/cameo/rules/tkm.yaml |
| tkmrocketeer | 5 | mods/cameo/rules/tkm.yaml |
| tkmsandmarine | 4 | mods/cameo/rules/tkm.yaml |
| tkmsniper | 5 | mods/cameo/rules/tkm.yaml |
| tkmspetsnaz | 5 | mods/cameo/rules/tkm.yaml |
| tkmstryker | 5 | mods/cameo/rules/tkm.yaml |
| tkmsuicidedrone | 4 | mods/cameo/rules/tkm.yaml |
| tkmtechnical | 4 | mods/cameo/rules/tkm.yaml |
| tkmtechnicaltank | 5 | mods/cameo/rules/tkm.yaml |
| tkmthermonaut | 5 | mods/cameo/rules/tkm.yaml |
| tkmtrenchtank | 5 | mods/cameo/rules/tkm.yaml |
| tkmtrenchtankdeployed | 4 | mods/cameo/rules/tkm.yaml |
| tkmtrenchtruck | 4 | mods/cameo/rules/tkm.yaml |
| tkmtrooper | 5 | mods/cameo/rules/tkm.yaml |
| tkmvan | 5 | mods/cameo/rules/tkm.yaml |
| tkmviper | 4 | mods/cameo/rules/tkm.yaml |
| tkmvon | 5 | mods/cameo/rules/tkm.yaml |
| tkmworker | 6 | mods/cameo/rules/tkm.yaml |
| tkmzaza | 4 | mods/cameo/rules/tkm.yaml |
| tleilax_labcrawl.ordos | 5 | mods/cameo/ContentPacks/D2k/Ordos/rules/vehicles.yaml |
| topol.latin | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/rules/vehicles.yaml |
| tortuga.latin | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/rules/vehicles.yaml |
| tran.gdi | 6 | mods/cameo/ContentPacks/TiberianDawn/GDI/rules/aircraft.yaml |
| tran.nod | 6 | mods/cameo/ContentPacks/TiberianDawn/Nod/rules/aircraft.yaml |
| triton.latin | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/rules/naval.yaml |
| trooper | 5 | mods/cameo/ContentPacks/D2k/Shared/rules/infantry.yaml |
| tsaegis | 4 | mods/cameo/rules/tiberiansun.yaml |
| tsapctruck | 4 | mods/cameo/rules/tiberiansun.yaml |
| tsart2cabal_backup | 5 | mods/cameo/rules/tiberiansun.yaml |
| tsarty_bus | 5 | mods/cameo/rules/tiberiansun.yaml |
| tsascended | 5 | mods/cameo/rules/tiberiansun.yaml |
| tsblackhandflamer | 5 | mods/cameo/ContentPacks/TiberianDawn/Nod/rules/infantry.yaml |
| tsblackhandlaser | 5 | mods/cameo/ContentPacks/TiberianDawn/Nod/rules/infantry.yaml |
| tsbowler | 4 | mods/cameo/rules/tiberiansun.yaml |
| tscabalcobra | 4 | mods/cameo/rules/tiberiansun.yaml |
| tscabaltech | 4 | mods/cameo/rules/tiberiansun.yaml |
| tscarrycabal | 4 | mods/cameo/rules/tiberiansun.yaml |
| tscbunk | 5 | mods/cameo/rules/tiberiansun.yaml |
| tsccommando | 6 | mods/cameo/rules/tiberiansun.yaml |
| tscheavyspider | 4 | mods/cameo/rules/tiberiansun.yaml |
| tscheavyspider_backup | 5 | mods/cameo/rules/tiberiansun.yaml |
| tsclosh | 5 | mods/cameo/rules/tiberiansun.yaml |
| tscropplane | 4 | mods/cameo/rules/tiberiansun.yaml |
| tscyborgb | 5 | mods/cameo/rules/tiberiansun.yaml |
| tsdevout | 5 | mods/cameo/rules/tiberiansun.yaml |
| tsdoggieblue | 5 | mods/cameo/rules/tiberiansun.yaml |
| tse3.mutant | 6 | mods/cameo/rules/tiberiansun.yaml |
| tsfloater | 4 | mods/cameo/rules/tiberiansun.yaml |
| tsflocust | 4 | mods/cameo/rules/tiberiansun.yaml |
| tsfsmoker.bomber | 4 | mods/cameo/rules/tiberiansun.yaml |
| tsghost.r4 | 6 | mods/cameo/rules/tiberiansun.yaml |
| tsgtctwrmg | 4 | mods/cameo/rules/tiberiansun.yaml |
| tsgtctwrrpg | 4 | mods/cameo/rules/tiberiansun.yaml |
| tsgtctwrsam | 4 | mods/cameo/rules/tiberiansun.yaml |
| tsgtctwrsammutant | 4 | mods/cameo/rules/tiberiansun.yaml |
| tsgtdeptcabal | 4 | mods/cameo/rules/tiberiansun.yaml |
| tsgtdeptmutant | 4 | mods/cameo/rules/tiberiansun.yaml |
| tsgtdeptnod | 4 | mods/cameo/rules/tiberiansun.yaml |
| tsgthpadmutant | 4 | mods/cameo/rules/tiberiansun.yaml |
| tsgtplug | 5 | mods/cameo/rules/tiberiansun.yaml |
| tsgtradr | 4 | mods/cameo/rules/tiberiansun.yaml |
| tsgtradrmutant | 4 | mods/cameo/rules/tiberiansun.yaml |
| tshacker | 5 | mods/cameo/rules/tiberiansun.yaml |
| tsjumpjet2 | 4 | mods/cameo/rules/tiberiansun.yaml |
| tslpst | 4 | mods/cameo/rules/tiberiansun.yaml |
| tsm113 | 4 | mods/cameo/rules/tiberiansun.yaml |
| tsmemp | 5 | mods/cameo/rules/tiberiansun.yaml |
| tsmlrs | 5 | mods/cameo/rules/tiberiansun.yaml |
| tsmonstermaker1 | 5 | mods/cameo/rules/tiberiansun.yaml |
| tsnomad | 4 | mods/cameo/rules/tiberiansun.yaml |
| tsnthpad2 | 4 | mods/cameo/rules/tiberiansun.yaml |
| tsntlasr | 4 | mods/cameo/rules/tiberiansun.yaml |
| tsntlasrcabal | 5 | mods/cameo/rules/tiberiansun.yaml |
| tsntmisl | 5 | mods/cameo/rules/tiberiansun.yaml |
| tsntmislcabal | 6 | mods/cameo/rules/tiberiansun.yaml |
| tsntobel | 4 | mods/cameo/rules/tiberiansun.yaml |
| tsntobelcabal | 5 | mods/cameo/rules/tiberiansun.yaml |
| tsntpulsgdi | 4 | mods/cameo/rules/tiberiansun.yaml |
| tsntradr | 4 | mods/cameo/rules/tiberiansun.yaml |
| tsntradrcabal | 5 | mods/cameo/rules/tiberiansun.yaml |
| tsntsam | 4 | mods/cameo/rules/tiberiansun.yaml |
| tsntstlhcabal | 4 | mods/cameo/rules/tiberiansun.yaml |
| tsobl2 | 4 | mods/cameo/rules/tiberiansun.yaml |
| tsprobe | 4 | mods/cameo/rules/tiberiansun.yaml |
| tsprocnod | 4 | mods/cameo/rules/tiberiansun.yaml |
| tsruiner | 4 | mods/cameo/rules/tiberiansun.yaml |
| tssapc.mut | 5 | mods/cameo/rules/tiberiansun.yaml |
| tssgen | 4 | mods/cameo/rules/tiberiansun.yaml |
| tssgencabal | 5 | mods/cameo/rules/tiberiansun.yaml |
| tsshotmut | 5 | mods/cameo/rules/tiberiansun.yaml |
| tsspddrone | 4 | mods/cameo/rules/tiberiansun.yaml |
| tsstealthsoldier | 5 | mods/cameo/ContentPacks/TiberianDawn/Nod/rules/infantry.yaml |
| tsttnkcabal_backup | 6 | mods/cameo/rules/tiberiansun.yaml |
| tsumagon.r4 | 7 | mods/cameo/rules/tiberiansun.yaml |
| tsun.asian | 4 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/rules/naval.yaml |
| tsveinhole | 5 | mods/cameo/rules/tiberiansun.yaml |
| tsvislrg | 4 | mods/cameo/rules/tiberiansun.yaml |
| tuboat.nax | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/rules/naval.yaml |
| twin_rocket_trooper.ixian | 5 | mods/cameo/ContentPacks/D2k/Ixian/rules/infantry.yaml |
| twister.futu | 4 | mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/rules/aircraft.yaml |
| twister.steel | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/rules/aircraft.yaml |
| twr.nax | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/rules/buildings.yaml |
| twr.nax2 | 5 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/rules/buildings.yaml |
| typechiha | 5 | mods/cameo/rules/redalert.yaml |
| uber.nax2 | 5 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/rules/infantry.yaml |
| undead.nax | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/rules/infantry.yaml |
| v1truck | 5 | mods/cameo/rules/redalert.yaml |
| viper.asian | 5 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/rules/vehicles.yaml |
| waveforcetank | 5 | mods/cameo/rules/redalert.yaml |
| wc2_critter_boar | 7 | mods/cameo/rules/warcraft2.yaml |
| wc2_critter_helboar | 7 | mods/cameo/rules/warcraft2.yaml |
| wc2_critter_seal | 7 | mods/cameo/rules/warcraft2.yaml |
| wc2_critter_sheep | 7 | mods/cameo/rules/warcraft2.yaml |
| wc2_human_archmage | 6 | mods/cameo/rules/warcraft2.yaml |
| wc2_human_ballista | 5 | mods/cameo/rules/warcraft2.yaml |
| wc2_human_barracks | 4 | mods/cameo/rules/warcraft2.yaml |
| wc2_human_battleship | 5 | mods/cameo/rules/warcraft2.yaml |
| wc2_human_blacksmith | 4 | mods/cameo/rules/warcraft2.yaml |
| wc2_human_cannon_tower | 5 | mods/cameo/rules/warcraft2.yaml |
| wc2_human_church | 4 | mods/cameo/rules/warcraft2.yaml |
| wc2_human_demolitionsquad | 6 | mods/cameo/rules/warcraft2.yaml |
| wc2_human_elven_archer | 6 | mods/cameo/rules/warcraft2.yaml |
| wc2_human_elven_destroyer | 5 | mods/cameo/rules/warcraft2.yaml |
| wc2_human_elven_lumber_mill | 4 | mods/cameo/rules/warcraft2.yaml |
| wc2_human_elven_ranger | 7 | mods/cameo/rules/warcraft2.yaml |
| wc2_human_farm | 5 | mods/cameo/rules/warcraft2.yaml |
| wc2_human_footman | 6 | mods/cameo/rules/warcraft2.yaml |
| wc2_human_footman2 | 6 | mods/cameo/rules/warcraft2.yaml |
| wc2_human_foundry | 4 | mods/cameo/rules/warcraft2.yaml |
| wc2_human_gnomish_flying_machine | 5 | mods/cameo/rules/warcraft2.yaml |
| wc2_human_gnomish_inventor | 4 | mods/cameo/rules/warcraft2.yaml |
| wc2_human_gnomish_submarine | 5 | mods/cameo/rules/warcraft2.yaml |
| wc2_human_goldmine | 4 | mods/cameo/rules/warcraft2.yaml |
| wc2_human_goldmine.bot | 5 | mods/cameo/rules/warcraft2.yaml |
| wc2_human_gryphon_aviary | 4 | mods/cameo/rules/warcraft2.yaml |
| wc2_human_gryphon_rider | 5 | mods/cameo/rules/warcraft2.yaml |
| wc2_human_guard_tower | 5 | mods/cameo/rules/warcraft2.yaml |
| wc2_human_gyrocopter2 | 5 | mods/cameo/rules/warcraft2.yaml |
| wc2_human_high_elf_archer | 6 | mods/cameo/rules/warcraft2.yaml |
| wc2_human_high_elf_priest | 6 | mods/cameo/rules/warcraft2.yaml |
| wc2_human_high_elf_sorceress | 6 | mods/cameo/rules/warcraft2.yaml |
| wc2_human_knight | 5 | mods/cameo/rules/warcraft2.yaml |
| wc2_human_knight2 | 5 | mods/cameo/rules/warcraft2.yaml |
| wc2_human_mage | 6 | mods/cameo/rules/warcraft2.yaml |
| wc2_human_mage_tower | 6 | mods/cameo/rules/warcraft2.yaml |
| wc2_human_mcv | 5 | mods/cameo/rules/warcraft2.yaml |
| wc2_human_militia2 | 6 | mods/cameo/rules/warcraft2.yaml |
| wc2_human_mortarteam2 | 6 | mods/cameo/rules/warcraft2.yaml |
| wc2_human_oil_platform | 6 | mods/cameo/rules/warcraft2.yaml |
| wc2_human_oil_refinery | 4 | mods/cameo/rules/warcraft2.yaml |
| wc2_human_oil_tanker | 5 | mods/cameo/rules/warcraft2.yaml |
| wc2_human_paladin | 6 | mods/cameo/rules/warcraft2.yaml |
| wc2_human_peasant | 6 | mods/cameo/rules/warcraft2.yaml |
| wc2_human_rifleman | 6 | mods/cameo/rules/warcraft2.yaml |
| wc2_human_scout_tower | 4 | mods/cameo/rules/warcraft2.yaml |
| wc2_human_shipyard | 4 | mods/cameo/rules/warcraft2.yaml |
| wc2_human_siege_engine | 5 | mods/cameo/rules/warcraft2.yaml |
| wc2_human_stables | 4 | mods/cameo/rules/warcraft2.yaml |
| wc2_human_sunwell | 4 | mods/cameo/rules/warcraft2.yaml |
| wc2_human_transport | 5 | mods/cameo/rules/warcraft2.yaml |
| wc2_neutral_daemon | 5 | mods/cameo/rules/warcraft2.yaml |
| wc2_orc_altar_of_storms | 4 | mods/cameo/rules/warcraft2.yaml |
| wc2_orc_barracks | 4 | mods/cameo/rules/warcraft2.yaml |
| wc2_orc_blacksmith | 4 | mods/cameo/rules/warcraft2.yaml |
| wc2_orc_cannon_tower | 5 | mods/cameo/rules/warcraft2.yaml |
| wc2_orc_catapult | 5 | mods/cameo/rules/warcraft2.yaml |
| wc2_orc_deathknight | 6 | mods/cameo/rules/warcraft2.yaml |
| wc2_orc_dragon | 5 | mods/cameo/rules/warcraft2.yaml |
| wc2_orc_dragon_roost | 4 | mods/cameo/rules/warcraft2.yaml |
| wc2_orc_eye_of_kilrogg | 5 | mods/cameo/rules/warcraft2.yaml |
| wc2_orc_foundry | 4 | mods/cameo/rules/warcraft2.yaml |
| wc2_orc_giant_turtle | 5 | mods/cameo/rules/warcraft2.yaml |
| wc2_orc_goblin_alchemist | 4 | mods/cameo/rules/warcraft2.yaml |
| wc2_orc_goblin_sappers | 6 | mods/cameo/rules/warcraft2.yaml |
| wc2_orc_goblin_zeppelin | 5 | mods/cameo/rules/warcraft2.yaml |
| wc2_orc_goldmine | 4 | mods/cameo/rules/warcraft2.yaml |
| wc2_orc_goldmine.bot | 5 | mods/cameo/rules/warcraft2.yaml |
| wc2_orc_grunt | 6 | mods/cameo/rules/warcraft2.yaml |
| wc2_orc_grunt2 | 6 | mods/cameo/rules/warcraft2.yaml |
| wc2_orc_guard_tower | 5 | mods/cameo/rules/warcraft2.yaml |
| wc2_orc_kodo_beast | 4 | mods/cameo/rules/warcraft2.yaml |
| wc2_orc_mcv | 5 | mods/cameo/rules/warcraft2.yaml |
| wc2_orc_ogre | 5 | mods/cameo/rules/warcraft2.yaml |
| wc2_orc_ogre_juggernaught | 5 | mods/cameo/rules/warcraft2.yaml |
| wc2_orc_ogre_mound | 4 | mods/cameo/rules/warcraft2.yaml |
| wc2_orc_ogremage | 6 | mods/cameo/rules/warcraft2.yaml |
| wc2_orc_oil_platform | 6 | mods/cameo/rules/warcraft2.yaml |
| wc2_orc_oil_refinery | 4 | mods/cameo/rules/warcraft2.yaml |
| wc2_orc_oil_tanker | 5 | mods/cameo/rules/warcraft2.yaml |
| wc2_orc_peon | 6 | mods/cameo/rules/warcraft2.yaml |
| wc2_orc_pigfarm | 5 | mods/cameo/rules/warcraft2.yaml |
| wc2_orc_shipyard | 4 | mods/cameo/rules/warcraft2.yaml |
| wc2_orc_siege_engine | 5 | mods/cameo/rules/warcraft2.yaml |
| wc2_orc_skeleton | 7 | mods/cameo/rules/warcraft2.yaml |
| wc2_orc_temple_of_the_damned | 6 | mods/cameo/rules/warcraft2.yaml |
| wc2_orc_transport | 5 | mods/cameo/rules/warcraft2.yaml |
| wc2_orc_troll_axethrower | 6 | mods/cameo/rules/warcraft2.yaml |
| wc2_orc_troll_berserker | 7 | mods/cameo/rules/warcraft2.yaml |
| wc2_orc_troll_lumber_mill | 4 | mods/cameo/rules/warcraft2.yaml |
| wc2_orc_troll_spearthrower | 6 | mods/cameo/rules/warcraft2.yaml |
| wc2_orc_trolldestroyer | 5 | mods/cameo/rules/warcraft2.yaml |
| wc2_orc_watch_tower | 4 | mods/cameo/rules/warcraft2.yaml |
| weap.nax | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/rules/buildings.yaml |
| weap.nax2 | 5 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/rules/buildings.yaml |
| wheel.futu | 4 | mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/rules/infantry.yaml |
| white_rabbit.steel | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/rules/vehicles.yaml |
| wind_trap.atreides | 5 | mods/cameo/ContentPacks/D2k/Atreides/rules/buildings.yaml |
| wind_trap.ixian | 5 | mods/cameo/ContentPacks/D2k/Ixian/rules/buildings.yaml |
| wind_trap.ordos | 5 | mods/cameo/ContentPacks/D2k/Ordos/rules/buildings.yaml |
| wirbelwind.nax | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/rules/vehicles.yaml |
| wraith.ordos | 4 | mods/cameo/ContentPacks/D2k/Ordos/rules/aircraft.yaml |
| wraith_husk.ordos | 4 | mods/cameo/ContentPacks/D2k/Ordos/rules/aircraft.yaml |
| wtrt.asian | 5 | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/rules/vehicles.yaml |
| yakarmored | 4 | mods/cameo/rules/redalert.yaml |
| yakarmored.Husk | 4 | mods/cameo/rules/husks.yaml |
| yaknuclear | 4 | mods/cameo/rules/redalert.yaml |
| yakolev.latin | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/rules/aircraft.yaml |
| yaktesla | 4 | mods/cameo/rules/redalert.yaml |
| yaktesla.Husk | 4 | mods/cameo/rules/husks.yaml |
| yamatobattleship | 4 | mods/cameo/rules/redalert.yaml |
| yrbfrt | 5 | mods/cameo/rules/redalert2.yaml |
| yrbfrt.bot | 6 | mods/cameo/rules/redalert2.yaml |
| yrbfrt.bot2 | 6 | mods/cameo/rules/redalert2.yaml |
| yrbiot | 5 | mods/cameo/rules/redalert2.yaml |
| yrbpln | 4 | mods/cameo/rules/redalert2.yaml |
| yrbpln1 | 4 | mods/cameo/rules/redalert2.yaml |
| yrbsub | 4 | mods/cameo/rules/redalert2.yaml |
| yrgarobo.futu | 4 | mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/rules/buildings.yaml |
| yrhovr | 4 | mods/cameo/rules/redalert2.yaml |
| yrlunr | 4 | mods/cameo/rules/redalert2.yaml |
| yrlunr.husk | 4 | mods/cameo/rules/redalert2.yaml |
| yrnacnst | 5 | mods/cameo/rules/redalert2.yaml |
| yrngbnkr | 5 | mods/cameo/rules/redalert2.yaml |
| yrngindp | 5 | mods/cameo/rules/redalert2.yaml |
| yrngtbnk | 5 | mods/cameo/rules/redalert2.yaml |
| yrrobo | 5 | mods/cameo/rules/redalert2.yaml |
| yrrobo.futu | 5 | mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/rules/vehicles.yaml |
| yrschp | 4 | mods/cameo/rules/redalert2.yaml |
| yrsmin.empy | 5 | mods/cameo/rules/redalert2.yaml |
| yryabrck | 4 | mods/cameo/rules/redalert2.yaml |
| yryapowr | 4 | mods/cameo/rules/redalert2.yaml |
| yryarefn | 5 | mods/cameo/rules/redalert2.yaml |
| yrygcomd | 5 | mods/cameo/rules/redalert2.yaml |
| yrygggun | 5 | mods/cameo/rules/redalert2.yaml |
| yryggntc | 4 | mods/cameo/rules/redalert2.yaml |
| yryggrnd | 4 | mods/cameo/rules/redalert2.yaml |
| yrygppet | 5 | mods/cameo/rules/redalert2.yaml |
| yrygpsyt | 5 | mods/cameo/rules/redalert2.yaml |
| yrygtech | 4 | mods/cameo/rules/redalert2.yaml |
| yrygweap | 5 | mods/cameo/rules/redalert2.yaml |
| yrygyard | 4 | mods/cameo/rules/redalert2.yaml |
| yuriinvisibleplane | 4 | mods/cameo/rules/redalert2.yaml |
| zep.nax | 4 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/rules/aircraft.yaml |
| zep.nax2 | 4 | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/rules/aircraft.yaml |
| zerofighter | 4 | mods/cameo/rules/redalert.yaml |
| zombietank.nax | 5 | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/rules/vehicles.yaml |


## V5 — actors with > 2 trait removals

| actor | removals | keys | file |
|---|---|---|---|
| A10Carrier | 4 | -UpdatesPlayerStatistics, -ActorLostNotification, -WithShadow, -MapEditorData | mods/cameo/ContentPacks/TiberianDawn/GDI/rules/aircraft.yaml |
| BARL | 11 | -Selectable, -ShakeOnDeath, -SoundOnDamageTransition, -Demolishable, -CaptureManager, -Capturable | mods/cameo/rules/tech.yaml |
| BRL3 | 11 | -Selectable, -ShakeOnDeath, -SoundOnDamageTransition, -Demolishable, -CaptureManager, -Capturable | mods/cameo/rules/tech.yaml |
| ChronoVortexFade | 3 | -SpawnActorOnDeath, -PeriodicExplosion, -AmbientSound | mods/cameo/rules/redalert.yaml |
| GAP | 10 | -AutoTarget, -RenderRangeCircle, -ExternalCondition@shrou, -ExternalCondition@locko, -RangeMultiplier@up_gpss, -RevealsShroudMultiplier | mods/cameo/rules/redalert.yaml |
| HBOX | 3 | -WithTurretSearchlight, -QuantizeFacingsFromSequ, -MustBeDestroyed | mods/cameo/rules/redalert.yaml |
| Kotin | 3 | -DamagedByTintedCells@ra, -DamagedByTintedCells@ge, -DamagedByTintedCells@ti | mods/cameo/rules/redalert.yaml |
| RA2SPY | 4 | -Tooltip, -Guard, -WithInfantryBody, -AttackFrontal | mods/cameo/rules/redalert2.yaml |
| RA2ZEP | 3 | -DamagedByTintedCells@ra, -DamagedByTintedCells@ge, -DamagedByTintedCells@ti | mods/cameo/rules/redalert2.yaml |
| RASPY | 3 | -Tooltip, -Guard, -WithInfantryBody | mods/cameo/rules/redalert.yaml |
| SCANALOGUE | 3 | -WithInfantryBody, -Targetable@disguise, -WithDeathAnimation | mods/cameo/rules/starcraft.yaml |
| SCARCHON | 5 | -WithInfantryBody, -Targetable@disguise, -HitShape, -WithDeathAnimation, -Crushable | mods/cameo/rules/starcraft.yaml |
| SCBATTLECRUISER | 4 | -ReloadAmmoDelayMultipli, -ReloadAmmoDelayMultipli, -ReloadAmmoDelayMultipli, -ReloadAmmoDelayMultipli | mods/cameo/rules/starcraft.yaml |
| SCCOMMANDCENTERM | 4 | -AttackAircraft, -SpawnActorOnDeath, -Hovers@CRUISING, -Voiced | mods/cameo/rules/starcraft.yaml |
| SCCREEPCOLONY | 3 | -WithTurretSearchlight, -WithDeathAnimation, -WithMakeAnimation | mods/cameo/rules/starcraft.yaml |
| SCDRONE | 5 | -WithMakeAnimation, -WithFacingSpriteBody, -WithInfantryBody, -Targetable@disguise, -WithSpriteBody@deployed | mods/cameo/rules/starcraft.yaml |
| SCIDOL | 4 | -ExternalCondition@Propa, -WithInfantryBody, -Targetable@disguise, -WithDeathAnimation | mods/cameo/rules/starcraft.yaml |
| SCLURKER | 3 | -HitShape, -WithMakeAnimation, -AttackFrontal | mods/cameo/rules/starcraft.yaml |
| SCMANIFOLD | 3 | -WithInfantryBody, -Targetable@disguise, -WithDeathAnimation | mods/cameo/rules/starcraft.yaml |
| SCMISSILETURRET | 3 | -WithTurretSearchlight, -WithSpriteBody, -ActorPreviewPlaceBuildi | mods/cameo/rules/starcraft.yaml |
| SCOVERLORD | 4 | -Targetable@infiltrate, -AttackAircraft, -AutoTarget, -RenderRangeCircle | mods/cameo/rules/starcraft.yaml |
| SCOVERMIND | 3 | -WithMakeAnimation, -WithDeathAnimation, -ToggleConditionOnOrder | mods/cameo/rules/starcraft.yaml |
| SCPHOBOS | 5 | -AttackAircraft, -ReloadAmmoDelayMultipli, -ReloadAmmoDelayMultipli, -ReloadAmmoDelayMultipli, -ReloadAmmoDelayMultipli | mods/cameo/rules/starcraft.yaml |
| SCPHOTONCANNON | 3 | -WithTurretSearchlight, -WithSpriteBody, -WithDeathAnimation | mods/cameo/rules/starcraft.yaml |
| SCSHIELDBATTERY | 3 | -WithSpriteBody, -GivesBuildableArea, -WithDeathAnimation | mods/cameo/rules/starcraft.yaml |
| SILO | 3 | -GivesBuildableArea, -WithSpriteBody, -AcceptsDeliveredCash | mods/cameo/ContentPacks/TiberianDawn/Shared/rules/buildings.yaml |
| TSAPACHE.Husk | 3 | -WithShadow, -Cloak@TDcloak, -Cloak@TScloak | mods/cameo/rules/tiberiansun.yaml |
| TSCYBORG | 3 | -DamagedByTerrain, -SpawnActorOnDeath, -TakeCover | mods/cameo/rules/tiberiansun.yaml |
| TSENGINEECABAL | 4 | -TemporaryOwnerManager, -TakeCover, -DamagedByTerrain, -SpawnActorOnDeath | mods/cameo/rules/tiberiansun.yaml |
| TSGHOSTSP | 4 | -Buildable, -MapEditorData, -Voiced, -Armament@c4 | mods/cameo/rules/tiberiansun.yaml |
| TSHELI.Husk | 3 | -WithShadow, -Cloak@TDcloak, -Cloak@TScloak | mods/cameo/rules/tiberiansun.yaml |
| TSMUTANTSP | 3 | -Buildable, -MapEditorData, -Voiced | mods/cameo/rules/tiberiansun.yaml |
| TSMWMNSP | 3 | -Buildable, -MapEditorData, -Voiced | mods/cameo/rules/tiberiansun.yaml |
| TSORCA.Husk | 3 | -WithShadow, -Cloak@TDcloak, -Cloak@TScloak | mods/cameo/rules/tiberiansun.yaml |
| TSORCAB.Husk | 3 | -WithShadow, -Cloak@TDcloak, -Cloak@TScloak | mods/cameo/rules/tiberiansun.yaml |
| TSSCRIN.Husk | 3 | -WithShadow, -Cloak@TDcloak, -Cloak@TScloak | mods/cameo/rules/tiberiansun.yaml |
| TST1000 | 3 | -DamagedByTerrain, -SpawnActorOnDeath, -TakeCover | mods/cameo/rules/tiberiansun.yaml |
| TSTRNSPORT.Husk | 3 | -WithShadow, -Cloak@TDcloak, -Cloak@TScloak | mods/cameo/rules/tiberiansun.yaml |
| TSUMAGONSP | 3 | -Buildable, -MapEditorData, -Voiced | mods/cameo/rules/tiberiansun.yaml |
| cgup.latin | 3 | -WithTurretSearchlight, -WithDeathAnimation, -QuantizeFacingsFromSequ | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/rules/buildings.yaml |
| cruiser_f.steel | 5 | -Selectable, -UpdatesPlayerStatistics, -ActorLostNotification, -WithShadow, -MapEditorData | mods/cameo/ContentPacks/RedAlert2Mod/Consortium/rules/aircraft.yaml |
| cyberdog | 3 | -Targetable@disguise, -InaccuracyMultiplier@ar, -InaccuracyMultiplier@Pr | mods/cameo/rules/redalert.yaml |
| drone.nax | 4 | -UpdatesPlayerStatistics, -ActorLostNotification, -MapEditorData, -Voiced | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/rules/aircraft.yaml |
| farasha_drone.ixian | 3 | -ActorLostNotification, -UpdatesPlayerStatistics, -MapEditorData | mods/cameo/ContentPacks/D2k/Ixian/rules/aircraft.yaml |
| fremen_creep | 3 | -MustBeDestroyed, -RevealsShroud@base-reve, -GrantConditionOnPrerequ | mods/cameo/ContentPacks/D2k/Shared/rules/infantry.yaml |
| gdirigdrone | 5 | -Targetable@SpecialRepai, -SpawnActorOnDeath, -ActorLostNotification, -UpdatesPlayerStatistics, -MapEditorData | mods/cameo/ContentPacks/TiberianDawn/GDI/rules/aircraft.yaml |
| hole.nax2 | 3 | -UpdatesPlayerStatistics, -ActorLostNotification, -MapEditorData | mods/cameo/ContentPacks/RedAlert2Mod/SchwarzerMond/rules/infantry.yaml |
| interceptor.nax | 4 | -UpdatesPlayerStatistics, -ActorLostNotification, -MapEditorData, -Voiced | mods/cameo/ContentPacks/RedAlert2Mod/Naxis/rules/aircraft.yaml |
| kami.asian | 4 | -UpdatesPlayerStatistics, -ActorLostNotification, -MapEditorData, -Voiced | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/rules/aircraft.yaml |
| kami_asdf.asian | 4 | -CarrierSlave, -AutoTarget, -AmmoPool, -WithAmmoPipsDecoration | mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/rules/aircraft.yaml |
| narco.latin | 4 | -ReloadAmmoDelayMultipli, -ReloadAmmoDelayMultipli, -ReloadAmmoDelayMultipli, -ReloadAmmoDelayMultipli | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/rules/infantry.yaml |
| nuketruk.latin | 3 | -RenderRangeCircle, -DamagedByTintedCells@ra, -DamagedByTintedCells@ge | mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/rules/vehicles.yaml |
| ra2_tzep | 3 | -DamagedByTintedCells@ra, -DamagedByTintedCells@ge, -DamagedByTintedCells@ti | mods/cameo/rules/redalert2.yaml |
| ra2asw | 4 | -UpdatesPlayerStatistics, -ActorLostNotification, -WithShadow, -MapEditorData | mods/cameo/rules/redalert2.yaml |
| ra2cplanesov | 3 | -DamagedByTintedCells@ra, -DamagedByTintedCells@ge, -DamagedByTintedCells@ti | mods/cameo/rules/redalert2.yaml |
| ra2gagap | 10 | -AutoTarget, -RenderRangeCircle, -ExternalCondition@shrou, -ExternalCondition@locko, -RangeMultiplier@up_gpss, -RevealsShroudMultiplier | mods/cameo/rules/redalert2.yaml |
| ra2hornet | 4 | -UpdatesPlayerStatistics, -ActorLostNotification, -WithShadow, -MapEditorData | mods/cameo/rules/redalert2.yaml |
| ra2shk.bot | 6 | -MapEditorData, -UpdatesPlayerStatistics, -Armament@PRIMARY, -Armament@PRIMARY2, -Armament@PRIMARY3, -AttackFrontal | mods/cameo/rules/redalert2.yaml |
| ra2v3rocket | 8 | -FireWarheadsOnDeath, -DamagedByTintedCells@ra, -DamagedByTintedCells@ge, -DamagedByTintedCells@ti, -SpeedMultiplier@ra2_sov, -SpeedMultiplier@ra2_sov | mods/cameo/rules/redalert2.yaml |
| sc_zerg_larva | 9 | -DeathSounds@NORMAL, -SpawnActorOnDeath@SCSWA, -WithDeathAnimation, -DamagedByTerrain, -Crushable, -TakeCover | mods/cameo/rules/starcraft.yaml |
| scadept.shade | 11 | -UpdatesPlayerStatistics, -MapEditorData, -ActorLostNotification, -GrantTimedConditionOnDe, -ShadeMaster, -Passenger | mods/cameo/rules/starcraft.yaml |
| sietch_creep | 10 | -RevealsShroud@base-reve, -GrantConditionOnPrerequ, -DamagedByTerrain, -GivesBuildableArea, -Sellable, -RepairableBuilding | mods/cameo/ContentPacks/D2k/Shared/rules/buildings.yaml |
| sietch_creep_disabled | 16 | -Targetable, -FireProjectilesOnDeath, -Selectable, -Targetable@ivan, -Targetable@trappable, -Targetable@chrono | mods/cameo/ContentPacks/D2k/Shared/rules/buildings.yaml |
| spy.futu | 4 | -Tooltip, -Guard, -WithInfantryBody, -AttackFrontal | mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/rules/infantry.yaml |
| storm_lasher.ixian | 3 | -WithDeathAnimation, -WithWallSpriteBody, -WithSpriteTurret | mods/cameo/ContentPacks/D2k/Ixian/rules/buildings.yaml |
| tsascended | 3 | -DamagedByTerrain, -SpawnActorOnDeath, -TakeCover | mods/cameo/rules/tiberiansun.yaml |
| tscyborgb | 3 | -DamagedByTerrain, -SpawnActorOnDeath, -TakeCover | mods/cameo/rules/tiberiansun.yaml |
| tsdevout | 3 | -DamagedByTerrain, -SpawnActorOnDeath, -TakeCover | mods/cameo/rules/tiberiansun.yaml |
| tshacker | 3 | -DamagedByTerrain, -SpawnActorOnDeath, -TakeCover | mods/cameo/rules/tiberiansun.yaml |
| tsnafnce | 5 | -Crushable, -Sellable, -Targetable, -Building, -WithWallSpriteBody | mods/cameo/rules/tiberiansun.yaml |
| tsprobe | 4 | -RenderVoxels, -WithVoxelBody, -WithShadow, -SpawnActorOnDeath | mods/cameo/rules/tiberiansun.yaml |
| tssgen | 4 | -ExternalCondition@CLOAK, -ExternalCondition@TSCLO, -Cloak@TDcloak, -Cloak@TScloak | mods/cameo/rules/tiberiansun.yaml |
| wc2_human_ballista | 3 | -Integrity, -GrantCondition@electron, -WithDeathAnimation | mods/cameo/rules/warcraft2.yaml |
| wc2_human_knight | 5 | -Integrity, -GrantCondition@electron, -WithFacingSpriteBody, -WithMoveAnimation, -WithAttackAnimation | mods/cameo/rules/warcraft2.yaml |
| wc2_human_mcv | 3 | -WithFacingSpriteBody, -WithMoveAnimation, -WithAttackAnimation | mods/cameo/rules/warcraft2.yaml |
| wc2_human_scout_tower | 3 | -WithTurretSearchlight, -WithDeathAnimation, -WithMakeAnimation | mods/cameo/rules/warcraft2.yaml |
| wc2_human_siege_engine | 4 | -AttackFrontal, -Integrity, -GrantCondition@electron, -WithDeathAnimation | mods/cameo/rules/warcraft2.yaml |
| wc2_orc_catapult | 3 | -Integrity, -GrantCondition@electron, -WithDeathAnimation | mods/cameo/rules/warcraft2.yaml |
| wc2_orc_mcv | 3 | -WithFacingSpriteBody, -WithMoveAnimation, -WithAttackAnimation | mods/cameo/rules/warcraft2.yaml |
| wc2_orc_ogre | 5 | -Integrity, -GrantCondition@electron, -WithFacingSpriteBody, -WithMoveAnimation, -WithAttackAnimation | mods/cameo/rules/warcraft2.yaml |
| wc2_orc_siege_engine | 4 | -AttackFrontal, -Integrity, -GrantCondition@electron, -WithDeathAnimation | mods/cameo/rules/warcraft2.yaml |
| wc2_orc_watch_tower | 3 | -WithTurretSearchlight, -WithDeathAnimation, -WithMakeAnimation | mods/cameo/rules/warcraft2.yaml |
| wc2_support_orc_eye_of_kilrogg | 4 | -Selectable, -Voiced, -Targetable@AIRBORNE, -SpawnActorOnDeath | mods/cameo/rules/warcraft2.yaml |
| yrbiot | 3 | -DamagedByTerrain, -DamagedByTintedCells@ra, -DamagedByTintedCells@ge | mods/cameo/rules/redalert2.yaml |
| yrbpln | 3 | -DamagedByTintedCells@ra, -DamagedByTintedCells@ge, -DamagedByTintedCells@ti | mods/cameo/rules/redalert2.yaml |
| yrbpln1 | 3 | -DamagedByTintedCells@ra, -DamagedByTintedCells@ge, -DamagedByTintedCells@ti | mods/cameo/rules/redalert2.yaml |
| yrnacnst | 3 | -WithIdleOverlay@fans, -WithBuildingPlacedOverl, -ProvidesPrerequisite@ra | mods/cameo/rules/redalert2.yaml |
| yrschp | 5 | -AttackAircraft, -WithShadow, -DamagedByTintedCells@ra, -DamagedByTintedCells@ge, -DamagedByTintedCells@ti | mods/cameo/rules/redalert2.yaml |
| yrschp.Husk | 3 | -WithShadow, -Cloak@TDcloak, -Cloak@TScloak | mods/cameo/rules/redalert2.yaml |
| yrygggun | 3 | -RenderRangeCircle, -WithVoxelBody, -Cloak@TDcloak | mods/cameo/rules/redalert2.yaml |
| zerofighter | 4 | -UpdatesPlayerStatistics, -ActorLostNotification, -WithShadow, -MapEditorData | mods/cameo/rules/redalert.yaml |

