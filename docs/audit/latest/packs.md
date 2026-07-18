# audit_packs — content-pack conversion & placement (DESIGN §2)

## P1 — conversion coverage (faction prefixes with actors OUTSIDE packs)

| prefix | in packs | outside packs | sample outside file |
|---|---|---|---|
| ra2 | 163 | 164 | mods\cameo\rules\misc.yaml |
| wc | 0 | 79 | mods\cameo\rules\warcraft1.yaml |
| halloween | 0 | 39 | mods\cameo\rules\halloween.yaml |
| sow | 0 | 38 | mods\cameo\rules\sow.yaml |
| xmas | 0 | 22 | mods\cameo\rules\xmas.yaml |
| mindclass | 0 | 17 | mods\cameo\rules\mindustry.yaml |
| valentine | 0 | 15 | mods\cameo\rules\valentine.yaml |
| xcom | 0 | 14 | mods\cameo\rules\xcom.yaml |
| valentines | 0 | 13 | mods\cameo\rules\valentine.yaml |
| wc2_orc | 0 | 11 | mods\cameo\rules\warcraft2.yaml |
| ra2_lt | 10 | 10 | mods\cameo\rules\redalert2.yaml |
| shockwave | 0 | 9 | mods\cameo\rules\shockwave.yaml |
| wc2_human | 0 | 9 | mods\cameo\rules\warcraft2.yaml |
| sc2k | 0 | 8 | mods\cameo\rules\sc2k.yaml |
| ambiance | 0 | 7 | mods\cameo\rules\misc.yaml |
| d2 | 0 | 7 | mods\cameo\rules\dune2.yaml |
| wc2_critter | 0 | 4 | mods\cameo\rules\warcraft2.yaml |
| factory | 0 | 4 | mods\cameo\rules\sow.yaml |
| simcity | 0 | 4 | mods\cameo\rules\simcity.yaml |
| ra2_c | 4 | 4 | mods\cameo\rules\redalert2.yaml |
| sglmobilesupplytruck | 0 | 3 | mods\cameo\rules\shockwave.yaml |
| eden | 0 | 3 | mods\cameo\rules\outpost2.yaml |
| ra2_stang | 2 | 2 | mods\cameo\rules\redalert2.yaml |
| ra2_jeep | 2 | 2 | mods\cameo\rules\redalert2.yaml |
| sglrpgtrooper | 0 | 2 | mods\cameo\rules\shockwave.yaml |
| ra1_soviets | 121 | 2 | mods\cameo\rules\heroes.yaml |
| ra2_truckb | 2 | 2 | mods\cameo\rules\redalert2.yaml |
| ra2_ycab | 2 | 2 | mods\cameo\rules\redalert2.yaml |
| schsupply | 0 | 2 | mods\cameo\rules\shockwave.yaml |
| susacannon | 0 | 2 | mods\cameo\rules\shockwave.yaml |
| ra2_bcab | 2 | 2 | mods\cameo\rules\redalert2.yaml |
| ra2_ptruck | 2 | 2 | mods\cameo\rules\redalert2.yaml |
| ra2_euroc | 2 | 2 | mods\cameo\rules\redalert2.yaml |
| ra2_cop | 2 | 2 | mods\cameo\rules\redalert2.yaml |
| susasupply | 0 | 2 | mods\cameo\rules\shockwave.yaml |
| ra2_car | 2 | 2 | mods\cameo\rules\redalert2.yaml |
| ra2_tractor | 2 | 2 | mods\cameo\rules\redalert2.yaml |
| win98 | 0 | 2 | mods\cameo\rules\win98.yaml |
| ra2_trucka | 2 | 2 | mods\cameo\rules\redalert2.yaml |
| ra2_taxi | 2 | 2 | mods\cameo\rules\redalert2.yaml |
| ra2_ddbx | 2 | 2 | mods\cameo\rules\redalert2.yaml |
| ra2_cona | 2 | 2 | mods\cameo\rules\redalert2.yaml |
| wc2_o | 0 | 2 | mods\cameo\rules\warcraft2.yaml |
| op2 | 0 | 2 | mods\cameo\rules\outpost2.yaml |
| ra2_ambu | 2 | 2 | mods\cameo\rules\redalert2.yaml |
| susapower | 0 | 2 | mods\cameo\rules\shockwave.yaml |
| mmwc | 0 | 2 | mods\cameo\rules\mcvmarket.yaml |
| satelliteprotection | 0 | 2 | mods\cameo\rules\sow.yaml |
| ra2_suvb | 2 | 2 | mods\cameo\rules\redalert2.yaml |
| ra2_suvw | 2 | 2 | mods\cameo\rules\redalert2.yaml |
| ra2_limo | 2 | 2 | mods\cameo\rules\redalert2.yaml |
| wc2_h | 0 | 2 | mods\cameo\rules\warcraft2.yaml |
| ra2_bus | 2 | 2 | mods\cameo\rules\redalert2.yaml |
| sgltoxinrebel | 0 | 1 | mods\cameo\rules\shockwave.yaml |
| htd | 0 | 1 | mods\cameo\rules\sow.yaml |
| sowtripler | 0 | 1 | mods\cameo\rules\sow.yaml |
| worms | 0 | 1 | mods\cameo\rules\worms.yaml |
| sglamob | 0 | 1 | mods\cameo\rules\shockwave.yaml |
| mtb | 0 | 1 | mods\cameo\rules\sow.yaml |
| sowfighter | 0 | 1 | mods\cameo\rules\sow.yaml |
| schredguard | 0 | 1 | mods\cameo\rules\shockwave.yaml |
| hta | 0 | 1 | mods\cameo\rules\sow.yaml |
| mtd | 0 | 1 | mods\cameo\rules\sow.yaml |
| upusaleaflet | 0 | 1 | mods\cameo\rules\generals.yaml |
| htc | 0 | 1 | mods\cameo\rules\sow.yaml |
| medium | 0 | 1 | mods\cameo\rules\xcom.yaml |
| wh40kkarssquad | 0 | 1 | mods\cameo\rules\wh40k.yaml |
| sglaangrymob1 | 0 | 1 | mods\cameo\rules\shockwave.yaml |
| wh40kscoutsquad | 0 | 1 | mods\cameo\rules\wh40k.yaml |
| schtankhunter | 0 | 1 | mods\cameo\rules\shockwave.yaml |
| upsusagunship3 | 0 | 1 | mods\cameo\rules\shockwave.yaml |
| wc2_camera | 0 | 1 | mods\cameo\rules\warcraft2.yaml |
| mmworms | 0 | 1 | mods\cameo\rules\mcvmarket.yaml |
| swdroidheli | 0 | 1 | mods\cameo\rules\starwars.yaml |
| wc2_neutral | 0 | 1 | mods\cameo\rules\warcraft2.yaml |
| ra | 0 | 1 | mods\cameo\rules\husks.yaml |
| wh40kguardsquad | 0 | 1 | mods\cameo\rules\wh40k.yaml |
| td_nod | 64 | 1 | mods\cameo\rules\tiberiaalliances.yaml |
| cute | 0 | 1 | mods\cameo\rules\valentine.yaml |
| dummy | 1 | 1 | mods\cameo\rules\redalert2.yaml |
| sglterrorist | 0 | 1 | mods\cameo\rules\shockwave.yaml |
| mta | 0 | 1 | mods\cameo\rules\sow.yaml |
| wh40kscoutmobslave | 0 | 1 | mods\cameo\rules\wh40k.yaml |
| swlaat | 0 | 1 | mods\cameo\rules\starwars.yaml |
| schlotus | 0 | 1 | mods\cameo\rules\shockwave.yaml |
| mtc | 0 | 1 | mods\cameo\rules\sow.yaml |
| wh40kcommisair | 0 | 1 | mods\cameo\rules\wh40k.yaml |
| wh40kogrynsquad | 0 | 1 | mods\cameo\rules\wh40k.yaml |
| wc2 | 0 | 1 | mods\cameo\rules\warcraft2.yaml |
| wh40kogrynmobslave | 0 | 1 | mods\cameo\rules\wh40k.yaml |
| zmcv | 0 | 1 | mods\cameo\rules\z.yaml |
| wh40ktechpriest | 0 | 1 | mods\cameo\rules\wh40k.yaml |
| wh40kmarine | 0 | 1 | mods\cameo\rules\wh40k.yaml |
| hte | 0 | 1 | mods\cameo\rules\sow.yaml |
| sglaangrymob2 | 0 | 1 | mods\cameo\rules\shockwave.yaml |
| camea | 0 | 1 | mods\cameo\rules\camea.yaml |
| schsiegesoldier | 0 | 1 | mods\cameo\rules\shockwave.yaml |
| wh40kpsyker | 0 | 1 | mods\cameo\rules\wh40k.yaml |
| sowbomber | 0 | 1 | mods\cameo\rules\sow.yaml |
| sc | 0 | 1 | mods\cameo\rules\starcraft.yaml |
| schhacker | 0 | 1 | mods\cameo\rules\shockwave.yaml |
| mte | 0 | 1 | mods\cameo\rules\sow.yaml |
| ts | 0 | 1 | mods\cameo\rules\misc.yaml |
| swa10 | 0 | 1 | mods\cameo\rules\starwars.yaml |
| wh40kguardmobslave | 0 | 1 | mods\cameo\rules\wh40k.yaml |
| ra1 | 11 | 1 | mods\cameo\rules\civilian.yaml |
| large | 0 | 1 | mods\cameo\rules\xcom.yaml |
| wc2_support | 0 | 1 | mods\cameo\rules\warcraft2.yaml |
| wh40kkarsmobslave | 0 | 1 | mods\cameo\rules\wh40k.yaml |
| ra2_ctmisc06 | 1 | 1 | mods\cameo\rules\redalert2.yaml |
| swxwing | 0 | 1 | mods\cameo\rules\starwars.yaml |
| htb | 0 | 1 | mods\cameo\rules\sow.yaml |

Fully converted prefixes (93): air, asianalliance, banshee, bbomb, bbomb2, bbomb3, bf109, bomber, cabal, cargoship, carryall, cgcnst, combat, conyard, corpse, cplane, cruiser, cryo, d2k, deathcash, devastator, deviator, dieglocke, drone, duelist, egcnst, eye, farasha, forgotten, fremen, futuretech, grun, harbinger, harvester, haunebu, haunebu2, heavy, heavydrone, hole, horten, inspect, ixian, japan, kami, landcarr, latinsyndicate, light, litt, me262, mig, missile, naxis, ordos, ornithopter, pelican, phoenix, piercer, protoss, qacst, ra1_allies, ra1_badger, ra2_allies, ra2_soviets, ra2_yuri, ra2hind, resonance, rocket, sarubia, schwarzermond, scrapcar, scrapcar2, siege, sietch, sonic, steelconsortium, swarmer, td_gdi, team, terran, tkm, ts_gdi, ts_nod, twister, up, upgrade, wc2_humans, wc2_orcs, wind, wraith, yakolev, yuri, zep, zerg

## P2 — actors whose id does not match the pack's dominant prefix

| pack | actor | dominant prefix |
|---|---|---|
| D2k/Atreides | ornithopter_husk.atreides | combat |
| D2k/Atreides | wind_trap.atreides | combat |
| D2k/Atreides | d2k_silo.atreides | combat |
| D2k/Atreides | sonic_tank_husk.atreides | combat |
| D2k/Harkonnen | devastator_husk.harkonnen | combat |
| D2k/Ixian | air_drone_husk.ixian | ixian |
| D2k/Ixian | drone_husk.ixian | ixian |
| D2k/Ixian | resonance_drone_husk.ixian | ixian |
| D2k/Ixian | farasha_husk.ixian | ixian |
| D2k/Ixian | heavydrone_husk.ixian | ixian |
| D2k/Ixian | farasha_drone.ixian | ixian |
| D2k/Ixian | farasha_drone_husk.ixian | ixian |
| D2k/Ixian | heavy_inf.ixian | ixian |
| D2k/Ixian | team_upgrade.d2k_advanced_ixian_technology | ixian |
| D2k/Ixian | rocket_raider.ixian | ixian |
| D2k/Ixian | harvester_husk.ixian | ixian |
| D2k/Ixian | missile_tank_husk.ixian | ixian |
| D2k/Ixian | heavy_rocket_raider.ixian | ixian |
| D2k/Ixian | duelist_tank.ixian | ixian |
| D2k/Ixian | duelist_tank_husk.ixian | ixian |
| D2k/Ordos | wraith_husk.ordos | ordos |
| D2k/Ordos | swarmer_husk.ordos | ordos |
| D2k/Ordos | banshee_husk.ordos | ordos |
| D2k/Ordos | eye_husk.ordos | ordos |
| D2k/Ordos | carryall_reinforce.ordos | ordos |
| D2k/Ordos | carryall_husk.ordos | ordos |
| D2k/Ordos | carryall_huskvtol.ordos | ordos |
| D2k/Ordos | deviator_husk.ordos | ordos |
| D2k/Ordos | combat_tank_husk.ordos | ordos |
| RedAlert2Mod/AsianAlliance | kami_chemical.asian | asianalliance |
| RedAlert2Mod/AsianAlliance | kami_asdf.asian | asianalliance |
| RedAlert2Mod/AsianAlliance | kami_husk.asian | asianalliance |
| RedAlert2Mod/AsianAlliance | kami_chemical_husk.asian | asianalliance |
| RedAlert2Mod/AsianAlliance | phoenix_husk.asian | asianalliance |
| RedAlert2Mod/AsianAlliance | pelican_husk.asian | asianalliance |
| RedAlert2Mod/AsianAlliance | bomber_minebomb.asian | asianalliance |
| RedAlert2Mod/AsianAlliance | bomber_minebomb2.asian | asianalliance |
| RedAlert2Mod/AsianAlliance | bomber_husk.asian | asianalliance |
| RedAlert2Mod/AsianAlliance | cgcnst_infiltrated.asian | asianalliance |
| RedAlert2Mod/AsianAlliance | up_tsunami.asian | asianalliance |
| RedAlert2Mod/AsianAlliance | up_dragonway_proxy_actor.asian | asianalliance |
| RedAlert2Mod/AsianAlliance | up_team_diplomacy_proxy_actor.asian | asianalliance |
| RedAlert2Mod/Consortium | inspect_husk.steel | steelconsortium |
| RedAlert2Mod/Consortium | twister_husk.steel | steelconsortium |
| RedAlert2Mod/Consortium | grun_husk.steel | steelconsortium |
| RedAlert2Mod/Consortium | cargoship_husk.steel | steelconsortium |
| RedAlert2Mod/Consortium | cruiser_husk.steel | steelconsortium |
| RedAlert2Mod/Consortium | cruiser_f_husk.steel | steelconsortium |
| RedAlert2Mod/Consortium | cruiser_f.steel | steelconsortium |
| RedAlert2Mod/Consortium | qacst_infiltrated.steel | steelconsortium |
| RedAlert2Mod/Consortium | up_team_shielresistance.steel | steelconsortium |
| RedAlert2Mod/FutureTech | cryo_husk.futu | futuretech |
| RedAlert2Mod/FutureTech | harbinger_husk.futu | futuretech |
| RedAlert2Mod/FutureTech | landcarr_drone.futu | futuretech |
| RedAlert2Mod/FutureTech | egcnst_infiltrated.futu | futuretech |
| RedAlert2Mod/Naxis | bf109_husk.nax | naxis |
| RedAlert2Mod/Naxis | me262_husk.nax | naxis |
| RedAlert2Mod/Naxis | zep_husk.nax | naxis |
| RedAlert2Mod/Naxis | horten_bomber.nax | naxis |
| RedAlert2Mod/Naxis | horten_husk.nax | naxis |
| RedAlert2Mod/Naxis | cplane_husk.nax | naxis |
| RedAlert2Mod/Naxis | litt_husk.nax | naxis |
| RedAlert2Mod/Naxis | conyard_infiltrated.nax | naxis |
| RedAlert2Mod/Naxis | up_resurrection.nax | naxis |
| RedAlert2Mod/Naxis | sarubia_bomb.nax | naxis |
| RedAlert2Mod/Naxis | up_team_blitzkrieg.nax | naxis |
| RedAlert2Mod/Naxis | corpse_big.nax | naxis |
| RedAlert2Mod/SchwarzerMond | zep_husk.nax2 | schwarzermond |
| RedAlert2Mod/SchwarzerMond | bbomb_husk.nax2 | schwarzermond |
| RedAlert2Mod/SchwarzerMond | bbomb2_husk.nax2 | schwarzermond |
| RedAlert2Mod/SchwarzerMond | bbomb3_husk.nax2 | schwarzermond |
| RedAlert2Mod/SchwarzerMond | haunebu_husk.nax2 | schwarzermond |
| RedAlert2Mod/SchwarzerMond | haunebu2_husk.nax2 | schwarzermond |
| RedAlert2Mod/SchwarzerMond | piercer_husk.nax2 | schwarzermond |
| RedAlert2Mod/SchwarzerMond | dieglocke_husk.nax2 | schwarzermond |
| RedAlert2Mod/SchwarzerMond | hole_small.nax2 | schwarzermond |
| RedAlert2Mod/SchwarzerMond | conyard_infiltrated.nax2 | schwarzermond |
| RedAlert2Mod/Syndicate | ra2hind_husk.latin | latinsyndicate |
| RedAlert2Mod/Syndicate | yakolev_husk.latin | latinsyndicate |
| RedAlert2Mod/Syndicate | mig_husk.latin | latinsyndicate |
| RedAlert2Mod/Syndicate | cgcnst_infiltrated.latin | latinsyndicate |
| RedAlert2Mod/Syndicate | up_team_ngbunk2.latin | latinsyndicate |
| RedAlert2Mod/Syndicate | up_team_cashrecover.latin | latinsyndicate |
| RedAlert2Mod/Syndicate | deathcash_small.latin | latinsyndicate |
| RedAlert2Mod/Syndicate | scrapcar_demo.latin | latinsyndicate |
| RedAlert2Mod/Syndicate | scrapcar_driveby.latin | latinsyndicate |
| RedAlert2Mod/Syndicate | scrapcar2_demo.latin | latinsyndicate |
| RedAlert2Mod/Syndicate | scrapcar2_driveby.latin | latinsyndicate |
| TiberianDawn/GDI | team_upgrade.up_lightweightarmorplating | td_gdi |
| TiberianDawn/Nod | team_upgrade.up_advancedguerillatactics | td_nod |

## P3 — content.yaml manifest vs disk / nonstandard filenames

_clean_

## P4 — naming summary (counts; details via gen_rename_maps)

- actor ids violating the lowercase grammar: **1559** (e.g. 1TNK.Husk, 1TNK.camea, 2100A2MAT, 2100A2PT, 2100A2TIT, 2100A2VET, 2100AA, 2100AACH)

Total findings: 695
