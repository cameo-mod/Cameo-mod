# audit_stat_formulas — house stat formulas

Violations: **761** across 1881 roster actors (reference-clean units: gdiarcher, raider.ordos)


## F1 — Repairable.HpPerStep ≠ HP/20  (40)

| actor | actual | expected |
|---|---|---|
| forgotten_scoopertank | HpPerStep 10000 | expected 12500 (HP 250000/20) |
| futuretech_beehivedronecarrier | HpPerStep 6500 | expected 6250 (HP 125000/20) |
| ixian_empbomber | HpPerStep 5555 | expected 5550 (HP 111000/20) |
| japan_coreairfield | HpPerStep 15000 | expected 2500 (HP 50000/20) |
| japan_corebarracks | HpPerStep 15000 | expected 2500 (HP 50000/20) |
| japan_corepowerplant | HpPerStep 15000 | expected 2500 (HP 50000/20) |
| japan_coreradar | HpPerStep 15000 | expected 2500 (HP 50000/20) |
| japan_corerefinery | HpPerStep 15000 | expected 2500 (HP 50000/20) |
| japan_coreservicedepot | HpPerStep 15000 | expected 2500 (HP 50000/20) |
| japan_coretechcenter | HpPerStep 15000 | expected 2500 (HP 50000/20) |
| japan_corewarfactory | HpPerStep 15000 | expected 2500 (HP 50000/20) |
| naxis_bmwbike | HpPerStep 4125 | expected 1100 (HP 22000/20) |
| naxis_imperialturbotank | HpPerStep 3125 | expected 4125 (HP 82500/20) |
| naxis_nokana | HpPerStep 3125 | expected 22500 (HP 450000/20) |
| naxis_oldtank | HpPerStep 7875 | expected 5500 (HP 110000/20) |
| naxis_shoekarn | HpPerStep 10000 | expected 7500 (HP 150000/20) |
| naxis_sturmtiger | HpPerStep 2000 | expected 12500 (HP 250000/20) |
| naxis_transportzeppelin | HpPerStep 2625 | expected 62500 (HP 1250000/20) |
| ra1_soviets_stalinfist | HpPerStep 15000 | expected 5000 (HP 100000/20) |
| schwarzermond_gravitycoretank | HpPerStep 3125 | expected 15000 (HP 300000/20) |
| schwarzermond_laserbeetle | HpPerStep 4375 | expected 2375 (HP 47500/20) |
| schwarzermond_spacezeppelin | HpPerStep 2625 | expected 67500 (HP 1350000/20) |
| tkm_abrams | HpPerStep 4800 | expected 6000 (HP 120000/20) |
| tkm_bigshiee | HpPerStep 6000 | expected 25000 (HP 500000/20) |
| tkm_dronepodtruck | HpPerStep 1375 | expected 3000 (HP 60000/20) |
| tkm_flakbus | HpPerStep 1375 | expected 6000 (HP 120000/20) |
| tkm_medictruck | HpPerStep 1375 | expected 2500 (HP 50000/20) |
| tkm_quadtruck | HpPerStep 1375 | expected 3250 (HP 65000/20) |
| tkm_radartruck | HpPerStep 1375 | expected 3750 (HP 75000/20) |
| tkm_repairtruck | HpPerStep 1375 | expected 2500 (HP 50000/20) |
| tkm_sandmarine | HpPerStep 6000 | expected 40000 (HP 800000/20) |
| tkm_stryker | HpPerStep 1375 | expected 4000 (HP 80000/20) |
| tkm_t30 | HpPerStep 2637 | expected 20000 (HP 400000/20) |
| tkm_t72m | HpPerStep 2637 | expected 5000 (HP 100000/20) |
| tkm_trenchtank | HpPerStep 2637 | expected 10000 (HP 200000/20) |
| tkm_trenchtruck | HpPerStep 2637 | expected 5000 (HP 100000/20) |
| ts_gdi_mobilesensorarray | HpPerStep 2637 | expected 3000 (HP 60000/20) |
| ts_nod_attackcycle | HpPerStep 100 | expected 1000 (HP 20000/20) |
| ts_nod_mobilestealthgenerator | HpPerStep 2637 | expected 1000 (HP 20000/20) |
| ts_nod_subterraneanapc | HpPerStep 2637 | expected 875 (HP 17500/20) |


## F2 — SelfHealing Step ≠ HP/2500 (inf: HP/1000)  (92)

| actor | actual | expected |
|---|---|---|
| asianalliance_pulverizermecha | Step 114 | expected 285 (HP 285000/1000) |
| cabal_beholder | Step 50 | expected 125 (HP 125000/1000) |
| combat_tank.harkonnen | Step 10 | expected 28 (HP 70000/2500) |
| eden_tiger_acidcloud | Step 10 | expected 24 (HP 60000/2500) |
| forgotten_mutanthijacker | Step 10 | expected 25 (HP 25000/1000) |
| forgotten_scoopertank | Step 80 | expected 100 (HP 250000/2500) |
| futuretech_athenacannon | Step 10 | expected 25 (HP 62500/2500) |
| futuretech_beehivedronecarrier | Step 52 | expected 50 (HP 125000/2500) |
| futuretech_phalanxwip | Step 18 | expected 30 (HP 75000/2500) |
| futuretech_plasmastrider | Step 96 | expected 240 (HP 240000/1000) |
| futuretech_prospectormk2 | Step 60 | expected 48 (HP 120000/2500) |
| futuretech_shotgundroid | Step 22 | expected 55 (HP 55000/1000) |
| futuretech_spyfutu | Step 10 | expected 5 (HP 5000/1000) |
| futuretech_twister | Step 50 | expected 20 (HP 50000/2500) |
| japan_chihaheavytank | Step 104 | expected 52 (HP 130000/2500) |
| japan_coreairfield | Step 120 | expected 20 (HP 50000/2500) |
| japan_corebarracks | Step 120 | expected 20 (HP 50000/2500) |
| japan_corepowerplant | Step 120 | expected 20 (HP 50000/2500) |
| japan_coreradar | Step 120 | expected 20 (HP 50000/2500) |
| japan_corerefinery | Step 120 | expected 20 (HP 50000/2500) |
| japan_coreservicedepot | Step 120 | expected 20 (HP 50000/2500) |
| japan_coretechcenter | Step 120 | expected 20 (HP 50000/2500) |
| japan_corewarfactory | Step 120 | expected 20 (HP 50000/2500) |
| japan_hovercraftflametank | Step 96 | expected 48 (HP 120000/2500) |
| japan_zerofighter | Step 30 | expected 12 (HP 30000/2500) |
| latinsyndicate_nuketruck | Step 10 | expected 24 (HP 60000/2500) |
| latinsyndicate_yakovlev | Step 40 | expected 16 (HP 40000/2500) |
| naxis_bf109 | Step 30 | expected 24 (HP 60000/2500) |
| naxis_bmwbike | Step 33 | expected 9 (HP 22000/2500) |
| naxis_coneheadsknights | Step 10 | expected 20 (HP 20000/1000) |
| naxis_imperialturbotank | Step 10 | expected 33 (HP 82500/2500) |
| naxis_me262 | Step 75 | expected 30 (HP 75000/2500) |
| naxis_nokana | Step 10 | expected 180 (HP 450000/2500) |
| naxis_nop03sarubia | Step 10 | expected 25 (HP 62500/2500) |
| naxis_oldtank | Step 63 | expected 44 (HP 110000/2500) |
| naxis_shoekarn | Step 80 | expected 60 (HP 150000/2500) |
| naxis_transportzeppelin | Step 21 | expected 500 (HP 1250000/2500) |
| ordos_swarmerdrone | Step 20 | expected 8 (HP 20000/2500) |
| plymouth_tiger_emp | Step 10 | expected 24 (HP 60000/2500) |
| plymouth_tiger_esg | Step 10 | expected 24 (HP 60000/2500) |
| plymouth_tiger_microwave | Step 10 | expected 24 (HP 60000/2500) |
| plymouth_tiger_rpg | Step 10 | expected 24 (HP 60000/2500) |
| plymouth_tiger_starflare | Step 10 | expected 36 (HP 90000/2500) |
| plymouth_tiger_stickyfoam | Step 10 | expected 24 (HP 60000/2500) |
| plymouth_tiger_supernova | Step 10 | expected 48 (HP 120000/2500) |
| ra1_allies_chronotank | Step 60 | expected 30 (HP 75000/2500) |
| ra1_allies_mechanic | Step 10 | expected 8 (HP 7500/1000) |
| ra1_allies_raspy | Step 10 | expected 5 (HP 5000/1000) |
| ra1_soviets_armoredyak | Step 80 | expected 32 (HP 80000/2500) |
| ra1_soviets_nuclearyak | Step 64 | expected 26 (HP 64000/2500) |
| ra1_soviets_stalinfist | Step 120 | expected 40 (HP 100000/2500) |
| ra1_soviets_su57attackbomber | Step 52 | expected 26 (HP 65000/2500) |
| ra1_soviets_teslayak | Step 64 | expected 26 (HP 64000/2500) |
| ra1_soviets_yakscoutplane | Step 32 | expected 13 (HP 32000/2500) |
| ra2_allies_attackdog | Step 2 | expected 5 (HP 5000/1000) |
| ra2_allies_harrier | Step 72 | expected 29 (HP 72000/2500) |
| ra2_allies_ra2spy | Step 10 | expected 5 (HP 5000/1000) |
| ra2_soviets_attackdog | Step 2 | expected 5 (HP 5000/1000) |
| schwarzermond_blackbomb | Step 7 | expected 15 (HP 37500/2500) |
| schwarzermond_corruptorpiercer | Step 7 | expected 15 (HP 37500/2500) |
| schwarzermond_dieglocke | Step 50 | expected 1500 (HP 3750000/2500) |
| schwarzermond_gravitycoretank | Step 10 | expected 120 (HP 300000/2500) |
| schwarzermond_laserbeetle | Step 35 | expected 19 (HP 47500/2500) |
| schwarzermond_spacezeppelin | Step 21 | expected 540 (HP 1350000/2500) |
| steelconsortium_katytank | Step 220 | expected 110 (HP 275000/2500) |
| steelconsortium_megalodon | Step 180 | expected 450 (HP 450000/1000) |
| steelconsortium_poseidontank | Step 50 | expected 125 (HP 125000/1000) |
| steelconsortium_twister | Step 50 | expected 20 (HP 50000/2500) |
| tkm_bigshiee | Step 48 | expected 200 (HP 500000/2500) |
| tkm_dronepodtruck | Step 11 | expected 24 (HP 60000/2500) |
| tkm_flakbus | Step 10 | expected 48 (HP 120000/2500) |
| tkm_juggernaut | Step 16 | expected 36 (HP 36000/1000) |
| tkm_medictruck | Step 11 | expected 20 (HP 50000/2500) |
| tkm_quadtruck | Step 11 | expected 26 (HP 65000/2500) |
| tkm_radartruck | Step 11 | expected 30 (HP 75000/2500) |
| tkm_repairtruck | Step 11 | expected 20 (HP 50000/2500) |
| tkm_sandmarine | Step 48 | expected 320 (HP 800000/2500) |
| tkm_stryker | Step 80 | expected 32 (HP 80000/2500) |
| tkm_t30 | Step 10 | expected 160 (HP 400000/2500) |
| tkm_t72m | Step 10 | expected 40 (HP 100000/2500) |
| tkm_technicaltank | Step 35 | expected 28 (HP 70000/2500) |
| tkm_trenchtank | Step 10 | expected 80 (HP 200000/2500) |
| tkm_trenchtruck | Step 10 | expected 40 (HP 100000/2500) |
| tkm_zaza | Step 11 | expected 50 (HP 125000/2500) |
| ts_gdi_zonetrooper | Step 10 | expected 80 (HP 80000/1000) |
| ts_nod_chameleonspy | Step 10 | expected 30 (HP 30000/1000) |
| ts_nod_subterraneanapc | Step 10 | expected 7 (HP 17500/2500) |
| wc2_humans_knight | Step 67 | expected 168 (HP 167500/1000) |
| wc2_humans_militiapeasant | Step 8 | expected 20 (HP 20000/1000) |
| wc2_orcs_ogre | Step 80 | expected 200 (HP 200000/1000) |
| zerg_gorekraken | Step 150 | expected 140 (HP 350000/2500) |
| zerg_ultralisk | Step 160 | expected 400 (HP 400000/1000) |


## F3 — infantry with Repairable  (10)

| actor | actual | expected |
|---|---|---|
| asianalliance_pulverizermecha | infantry declares Repairable locally |  |
| cabal_beholder | infantry declares Repairable locally |  |
| futuretech_plasmastrider | infantry declares Repairable locally |  |
| futuretech_shotgundroid | infantry declares Repairable locally |  |
| latinsyndicate_mortarbike | infantry declares Repairable locally |  |
| plymouth_spider | infantry declares Repairable locally |  |
| schwarzermond_noidmgarmor | infantry declares Repairable locally |  |
| steelconsortium_megalodon | infantry declares Repairable locally |  |
| steelconsortium_poseidontank | infantry declares Repairable locally |  |
| wc2_humans_militiapeasant | infantry declares Repairable locally |  |


_242 further infantry inherit Repairable from the infantry base template (^DefaultInfantry RepairActors: drfghosp… — unloaded Dark Reign hospitals). One template-line fix covers them all._


## F4 — upgrade shield RegenAmount ≠ 2×SelfHealing Step  (59)

| actor | actual | expected |
|---|---|---|
| asianalliance_droneminer | RegenAmount 10 | expected 20 (2 x SelfHealing 10) |
| duelist_tank.ixian | RegenAmount 158 | expected 192 (2 x SelfHealing 96) |
| eden_cargotruck_empty | RegenAmount 10 | expected 88 (2 x SelfHealing 44) |
| forgotten_engineer | RegenAmount 25 | expected 20 (2 x SelfHealing 10) |
| forgotten_tiberiumharvester | RegenAmount 10 | expected 120 (2 x SelfHealing 60) |
| futuretech_prospector | RegenAmount 10 | expected 80 (2 x SelfHealing 40) |
| futuretech_prospectormk2 | RegenAmount 10 | expected 120 (2 x SelfHealing 60) |
| heavy_inf.ixian | RegenAmount 10 | expected 64 (2 x SelfHealing 32) |
| ixian_empbomber | RegenAmount 76 | expected 88 (2 x SelfHealing 44) |
| ixian_lightinfantry | RegenAmount 10 | expected 36 (2 x SelfHealing 18) |
| ixian_rockettrooper | RegenAmount 10 | expected 24 (2 x SelfHealing 12) |
| ixian_shockinfantry | RegenAmount 10 | expected 72 (2 x SelfHealing 36) |
| ixian_storminfantry | RegenAmount 10 | expected 88 (2 x SelfHealing 44) |
| ixian_stormlasher | RegenAmount 160 | expected 20 (2 x SelfHealing 10) |
| ixian_twinrockettrooper | RegenAmount 10 | expected 48 (2 x SelfHealing 24) |
| japan_japaneseoretruck | RegenAmount 10 | expected 60 (2 x SelfHealing 30) |
| latinsyndicate_collectiontruck | RegenAmount 10 | expected 68 (2 x SelfHealing 34) |
| light_inf | RegenAmount 10 | expected 36 (2 x SelfHealing 18) |
| naxis_slave | RegenAmount 10 | expected 20 (2 x SelfHealing 10) |
| ordos_chemicaltrooper | RegenAmount 10 | expected 60 (2 x SelfHealing 30) |
| ordos_combatautoguntank | RegenAmount 48 | expected 76 (2 x SelfHealing 38) |
| ordos_contaminator | RegenAmount 10 | expected 150 (2 x SelfHealing 75) |
| ordos_facedancer | RegenAmount 10 | expected 180 (2 x SelfHealing 90) |
| ordos_heavyautoguntank | RegenAmount 96 | expected 128 (2 x SelfHealing 64) |
| ordos_leech | RegenAmount 10 | expected 40 (2 x SelfHealing 20) |
| ordos_lightinfantry | RegenAmount 10 | expected 36 (2 x SelfHealing 18) |
| ordos_rockettrooper | RegenAmount 10 | expected 24 (2 x SelfHealing 12) |
| plymouth_cargotruck_empty | RegenAmount 10 | expected 96 (2 x SelfHealing 48) |
| ra1_allies_alliedoretruck | RegenAmount 10 | expected 80 (2 x SelfHealing 40) |
| ra1_soviets_heavyindustrialminer | RegenAmount 10 | expected 108 (2 x SelfHealing 54) |
| ra1_soviets_oretruck | RegenAmount 10 | expected 80 (2 x SelfHealing 40) |
| ra2_allies_chronominer | RegenAmount 10 | expected 80 (2 x SelfHealing 40) |
| ra2_soviets_warminer | RegenAmount 10 | expected 100 (2 x SelfHealing 50) |
| schwarzermond_noidharvester | RegenAmount 10 | expected 60 (2 x SelfHealing 30) |
| steelconsortium_consortiumminer | RegenAmount 10 | expected 80 (2 x SelfHealing 40) |
| td_gdi_tiberiumharvester | RegenAmount 10 | expected 120 (2 x SelfHealing 60) |
| td_nod_blackhandflamer | RegenAmount 25 | expected 72 (2 x SelfHealing 36) |
| td_nod_chemicalrocketsoldier | RegenAmount 25 | expected 36 (2 x SelfHealing 18) |
| td_nod_chemicalwarrior | RegenAmount 25 | expected 96 (2 x SelfHealing 48) |
| td_nod_commando | RegenAmount 25 | expected 160 (2 x SelfHealing 80) |
| td_nod_flamethrower | RegenAmount 25 | expected 40 (2 x SelfHealing 20) |
| td_nod_lasercommando | RegenAmount 25 | expected 114 (2 x SelfHealing 57) |
| td_nod_lasertrooper | RegenAmount 25 | expected 120 (2 x SelfHealing 60) |
| td_nod_minigunner | RegenAmount 25 | expected 32 (2 x SelfHealing 16) |
| td_nod_rocketsoldier | RegenAmount 25 | expected 18 (2 x SelfHealing 9) |
| td_nod_stealthharvester | RegenAmount 10 | expected 100 (2 x SelfHealing 50) |
| td_nod_stealthsoldier | RegenAmount 25 | expected 50 (2 x SelfHealing 25) |
| td_nod_tiberiumharvester | RegenAmount 10 | expected 120 (2 x SelfHealing 60) |
| terran_scv | RegenAmount 10 | expected 36 (2 x SelfHealing 18) |
| tkm_templateharvesterraname | RegenAmount 10 | expected 80 (2 x SelfHealing 40) |
| trooper | RegenAmount 10 | expected 24 (2 x SelfHealing 12) |
| ts_gdi_engineer | RegenAmount 25 | expected 20 (2 x SelfHealing 10) |
| ts_gdi_tiberiumharvester | RegenAmount 10 | expected 120 (2 x SelfHealing 60) |
| ts_nod_engineer | RegenAmount 25 | expected 20 (2 x SelfHealing 10) |
| ts_nod_tiberiumharvester | RegenAmount 10 | expected 120 (2 x SelfHealing 60) |
| wc2_humans_militiapeasant | RegenAmount 10 | expected 16 (2 x SelfHealing 8) |
| wc2_humans_peasant | RegenAmount 10 | expected 32 (2 x SelfHealing 16) |
| wc2_orcs_peon | RegenAmount 10 | expected 32 (2 x SelfHealing 16) |
| zerg_drone | RegenAmount 10 | expected 36 (2 x SelfHealing 18) |


## F5 — defense RevealsShroud.Range ≠ weapon range  (41)

| actor | actual | expected |
|---|---|---|
| cabal_heavycabalobelisk | RevealsShroud 7168 | weapon range 5120 |
| cabal_plasmaturret | RevealsShroud 7168 | weapon range 6487 |
| eden_gp_emp | RevealsShroud 6144 | weapon range 5500 |
| eden_gp_laser | RevealsShroud 6144 | weapon range 6656 |
| eden_gp_railgun | RevealsShroud 6144 | weapon range 7168 |
| forgotten_brokenrattytankturret | RevealsShroud 7168 | weapon range 6574 |
| forgotten_brokenscoopertankturret | RevealsShroud 7168 | weapon range 6404 |
| forgotten_brokenwarriortankturret | RevealsShroud 7168 | weapon range 9483 |
| forgotten_machineguntower | RevealsShroud 7168 | weapon range 6272 |
| latinsyndicate_latinsentrygun | RevealsShroud 6666 | weapon range 7777 |
| naxis_flak88 | RevealsShroud 6666 | weapon range 13200 |
| naxis_naxibunker | RevealsShroud 6666 | weapon range 12345 |
| naxis_rifletower | RevealsShroud 6666 | weapon range 8100 |
| plymouth_gp_microwave | RevealsShroud 6144 | weapon range 6656 |
| plymouth_gp_rpg | RevealsShroud 6144 | weapon range 7168 |
| plymouth_gp_stickyfoam | RevealsShroud 6144 | weapon range 6656 |
| ra1_allies_alliedgunturret | RevealsShroud 8683 | weapon range 7685 |
| ra2_soviets_teslacoil | RevealsShroud 10000 | weapon range 8842 |
| schwarzermond_sturmcannon | RevealsShroud 6666 | weapon range 14000 |
| steelconsortium_antiairquantummissileturret | RevealsShroud 12000 | weapon range 15000 |
| steelconsortium_bfg10000 | RevealsShroud 25000 | weapon range 10238976 |
| steelconsortium_consortiumsentryturret | RevealsShroud 6666 | weapon range 15000 |
| steelconsortium_quantumcannon | RevealsShroud 8888 | weapon range 15000 |
| td_nod_samsite | RevealsShroud 12588 | weapon range 12193 |
| tkm_quadturretbunker | RevealsShroud 6720 | weapon range 11604 |
| ts_gdi_empulsecannon | RevealsShroud 7168 | weapon range 10205 |
| ts_gdi_rpgtower | RevealsShroud 7168 | weapon range 8544 |
| ts_gdi_vulcantower | RevealsShroud 7168 | weapon range 6809 |
| ts_nod_laserturret | RevealsShroud 7168 | weapon range 6992 |
| ts_nod_missilesilo | RevealsShroud 5120 | weapon range 10238976 |
| ts_nod_obeliskoflight | RevealsShroud 7168 | weapon range 10435 |
| wc2_humans_cannontower | RevealsShroud 5000 | weapon range 10500 |
| wc2_humans_guardtower | RevealsShroud 5000 | weapon range 10500 |
| wc2_humans_humanscouttower | RevealsShroud 5000 | weapon range 10500 |
| wc2_orcs_cannontower | RevealsShroud 5000 | weapon range 10500 |
| wc2_orcs_guardtower | RevealsShroud 5000 | weapon range 10500 |
| wc2_orcs_orcwatchtower | RevealsShroud 5000 | weapon range 10500 |
| yuri_psychictower | RevealsShroud 10000 | weapon range 8000 |
| zerg_creepcolony_2 | RevealsShroud 5000 | weapon range 10160 |
| zerg_sporecolony | RevealsShroud 5000 | weapon range 10160 |
| zerg_sunkencolony_2 | RevealsShroud 5000 | weapon range 10160 |


## F6 — AA/advanced defense DetectCloaked.Range ≠ weapon range/2  (17)

| actor | actual | expected |
|---|---|---|
| forgotten_brokenscoopertankturret | DetectCloaked 3072 | expected 3202 (range/2) |
| forgotten_brokenwarriortankturret | DetectCloaked 3072 | expected 4741 (range/2) |
| forgotten_juggerflakwall | DetectCloaked 4096 | expected 5617 (range/2) |
| latinsyndicate_smlturret | DetectCloaked 7000 | expected 7500 (range/2) |
| protoss_photoncannon | DetectCloaked 4224 | expected 4114 (range/2) |
| ra2_soviets_teslacoil | DetectCloaked 5000 | expected 4421 (range/2) |
| steelconsortium_antiairquantummissileturret | DetectCloaked 6000 | expected 7500 (range/2) |
| steelconsortium_bfg10000 | DetectCloaked 12500 | expected 5119488 (range/2) |
| steelconsortium_quantumcannon | DetectCloaked 4444 | expected 7500 (range/2) |
| td_nod_samsite | DetectCloaked 6294 | expected 6096 (range/2) |
| tkm_quadturretbunker | DetectCloaked 6000 | expected 5802 (range/2) |
| ts_gdi_empulsecannon | DetectCloaked missing | expected 5102 |
| ts_gdi_rpgtower | DetectCloaked 3072 | expected 4272 (range/2) |
| ts_gdi_samtower | DetectCloaked 4096 | expected 6220 (range/2) |
| ts_nod_obeliskoflight | DetectCloaked 5120 | expected 5217 (range/2) |
| ts_nod_samsite | DetectCloaked 4096 | expected 6588 (range/2) |
| yuri_psychictower | DetectCloaked 5000 | expected 4000 (range/2) |


## F7 — defense Power.Amount ≠ -Cost/20  (90)

| actor | actual | expected |
|---|---|---|
| asianalliance_advancedcommunicationcenter | Power -200 | expected -500 (-Cost/20) |
| asianalliance_asiansentryflamer | Power -25 | expected -40 (-Cost/20) |
| asianalliance_chaosstorminductor | Power -200 | expected -250 (-Cost/20) |
| asianalliance_concretebarrier | Power missing | expected -10 |
| brik | Power missing | expected -10 |
| cycl | Power missing | expected -3 |
| eden_gp_emp | Power -10 | expected -30 (-Cost/20) |
| eden_gp_laser | Power -10 | expected -30 (-Cost/20) |
| eden_gp_railgun | Power -10 | expected -30 (-Cost/20) |
| eden_light_tower | Power -5 | expected -2 (-Cost/20) |
| eden_mine_common | Power -50 | expected -40 (-Cost/20) |
| eden_storage_common | Power -10 | expected -5 (-Cost/20) |
| fenc | Power missing | expected -1 |
| forgotten_brokenrattytankturret | Power 0 | expected -40 (-Cost/20) |
| forgotten_brokenscoopertankturret | Power 0 | expected -87 (-Cost/20) |
| forgotten_brokenwarriortankturret | Power 0 | expected -75 (-Cost/20) |
| forgotten_juggerflakwall | Power -40 | expected -50 (-Cost/20) |
| forgotten_machineguntower | Power -20 | expected -30 (-Cost/20) |
| forgotten_silo | Power -10 | expected -15 (-Cost/20) |
| forgotten_veinhole | Power -150 | expected -500 (-Cost/20) |
| ixian_munitionssilo | Power -10 | expected -25 (-Cost/20) |
| ixian_storagesilo | Power -10 | expected -7 (-Cost/20) |
| ixian_supercomputer | Power -200 | expected -500 (-Cost/20) |
| japan_japaneseshrine | Power -200 | expected -500 (-Cost/20) |
| latinsyndicate_bunkertower | Power missing | expected -30 |
| latinsyndicate_latinsentrygun | Power -30 | expected -35 (-Cost/20) |
| latinsyndicate_topolsilo | Power -200 | expected -500 (-Cost/20) |
| naxis_flak88 | Power -40 | expected -60 (-Cost/20) |
| naxis_naxibunker | Power missing | expected -50 |
| naxis_naxirocketsilo | Power -200 | expected -500 (-Cost/20) |
| naxis_rifletower | Power -25 | expected -32 (-Cost/20) |
| ordos_storagesilo | Power -10 | expected -7 (-Cost/20) |
| plymouth_gp_microwave | Power -10 | expected -30 (-Cost/20) |
| plymouth_gp_rpg | Power -10 | expected -30 (-Cost/20) |
| plymouth_gp_stickyfoam | Power -10 | expected -30 (-Cost/20) |
| plymouth_light_tower | Power -5 | expected -2 (-Cost/20) |
| plymouth_mine_common | Power -50 | expected -40 (-Cost/20) |
| plymouth_storage_common | Power -10 | expected -5 (-Cost/20) |
| ra1_allies_chronosphere | Power -200 | expected -500 (-Cost/20) |
| ra1_oresilo | Power -10 | expected -7 (-Cost/20) |
| ra1_soviets_ironcurtain | Power -200 | expected -250 (-Cost/20) |
| ra1_soviets_missilesilo | Power -200 | expected -500 (-Cost/20) |
| ra2_allies_chronosphere | Power -200 | expected -250 (-Cost/20) |
| ra2_allies_concretebarrier | Power missing | expected -10 |
| ra2_allies_grandcannon | Power -200 | expected -250 (-Cost/20) |
| ra2_allies_weathercontrolcenter | Power -200 | expected -500 (-Cost/20) |
| ra2_soviets_battlebunker | Power missing | expected -40 |
| ra2_soviets_concretebarrier | Power missing | expected -10 |
| ra2_soviets_ironcurtain | Power -200 | expected -250 (-Cost/20) |
| ra2_soviets_nuclearmissilesilo | Power -200 | expected -500 (-Cost/20) |
| ra2brik | Power missing | expected -10 |
| sbag | Power missing | expected -2 |
| schwarzermond_meteortractionray | Power -200 | expected -500 (-Cost/20) |
| schwarzermond_sturmcannon | Power -50 | expected -60 (-Cost/20) |
| silo | Power -10 | expected -5 (-Cost/20) |
| steelconsortium_antiairquantummissileturret | Power -45 | expected -50 (-Cost/20) |
| steelconsortium_bfg10000 | Power -1000 | expected -500 (-Cost/20) |
| steelconsortium_orbitalcannonactivator | Power -200 | expected -500 (-Cost/20) |
| terran_bunker | Power 0 | expected -60 (-Cost/20) |
| terran_missilesilo | Power 0 | expected -500 (-Cost/20) |
| tkm_bunker | Power missing | expected -30 |
| tkm_quadturretbunker | Power -25 | expected -45 (-Cost/20) |
| tkm_tankturretbunker | Power -25 | expected -40 (-Cost/20) |
| ts_gdi_rpgtower | Power -20 | expected -70 (-Cost/20) |
| ts_gdi_samtower | Power -30 | expected -40 (-Cost/20) |
| ts_gdi_upgradecenter | Power -200 | expected -500 (-Cost/20) |
| ts_gdi_vulcantower | Power -20 | expected -30 (-Cost/20) |
| ts_nod_laserfence | Power -25 | expected -10 (-Cost/20) |
| ts_nod_laserturret | Power -20 | expected -40 (-Cost/20) |
| ts_nod_missilesilo | Power -150 | expected -500 (-Cost/20) |
| ts_nod_obeliskoflight | Power -100 | expected -110 (-Cost/20) |
| ts_nod_samsite | Power -30 | expected -40 (-Cost/20) |
| ts_nod_silo | Power -10 | expected -7 (-Cost/20) |
| ts_nod_stealthgenerator | Power -150 | expected -125 (-Cost/20) |
| wall | Power missing | expected -6 |
| wc2_humans_cannontower | Power missing | expected -75 |
| wc2_humans_guardtower | Power missing | expected -75 |
| wc2_humans_humanscouttower | Power missing | expected -60 |
| wc2_humans_wall | Power missing | expected -15 |
| wc2_orcs_cannontower | Power missing | expected -80 |
| wc2_orcs_guardtower | Power missing | expected -80 |
| wc2_orcs_orcwatchtower | Power missing | expected -60 |
| wc2_orcs_wall | Power missing | expected -15 |
| yuri_concretebarrier | Power missing | expected -10 |
| yuri_geneticmutator | Power -200 | expected -250 (-Cost/20) |
| yuri_psychicdominator | Power -200 | expected -500 (-Cost/20) |
| yuri_tankbunker | Power missing | expected -50 |
| zerg_creepcolony_2 | Power missing | expected -50 |
| zerg_sporecolony | Power missing | expected -62 |
| zerg_sunkencolony_2 | Power missing | expected -62 |


## F8 — vehicle TurnSpeed ≠ Speed/5  (33)

| actor | actual | expected |
|---|---|---|
| eden_lynx_acidcloud | TurnSpeed 10 (Speed 90) | expected 18 = Speed/5 |
| futuretech_beehivedronecarrier | TurnSpeed 12 (Speed 45) | expected 9 = Speed/5 |
| futuretech_energizer | TurnSpeed 20 (Speed 50) | expected 10 = Speed/5 |
| ixian_neocymek | TurnSpeed 18 (Speed 45) | expected 9 = Speed/5 |
| ixian_shockraider | TurnSpeed 48 (Speed 120) | expected 24 = Speed/5 |
| japan_nanodronebuggy | TurnSpeed 17 (Speed 77) | expected 15 = Speed/5 |
| naxis_bmwbike | TurnSpeed 16 (Speed 125) | expected 25 = Speed/5 |
| naxis_shoekarn | TurnSpeed 30 (Speed 75) | expected 15 = Speed/5 |
| ordos_cobratank | TurnSpeed 18 (Speed 45) | expected 9 = Speed/5 |
| ordos_heavyautoguntank | TurnSpeed 30 (Speed 75) | expected 15 = Speed/5 |
| ordos_pythontank | TurnSpeed 16 (Speed 40) | expected 8 = Speed/5 |
| plymouth_lynx_emp | TurnSpeed 10 (Speed 90) | expected 18 = Speed/5 |
| plymouth_lynx_esg | TurnSpeed 10 (Speed 90) | expected 18 = Speed/5 |
| plymouth_lynx_microwave | TurnSpeed 10 (Speed 90) | expected 18 = Speed/5 |
| plymouth_lynx_rpg | TurnSpeed 10 (Speed 90) | expected 18 = Speed/5 |
| plymouth_lynx_starflare | TurnSpeed 10 (Speed 90) | expected 18 = Speed/5 |
| plymouth_lynx_stickyfoam | TurnSpeed 10 (Speed 90) | expected 18 = Speed/5 |
| plymouth_lynx_supernova | TurnSpeed 10 (Speed 90) | expected 18 = Speed/5 |
| ra1_allies_minelayer | TurnSpeed 40 (Speed 128) | expected 26 = Speed/5 |
| ra1_soviets_gorynychtank | TurnSpeed 12 (Speed 70) | expected 14 = Speed/5 |
| ra1_soviets_heatraytank | TurnSpeed 24 (Speed 60) | expected 12 = Speed/5 |
| ra1_soviets_teslatank | TurnSpeed 32 (Speed 80) | expected 16 = Speed/5 |
| ra2_soviets_terrordrone | TurnSpeed 200 (Speed 200) | expected 40 = Speed/5 |
| steelconsortium_consortiummobileconstructionvehicle | TurnSpeed 15 (Speed 60) | expected 12 = Speed/5 |
| td_nod_chemicalattackbike | TurnSpeed 70 (Speed 175) | expected 35 = Speed/5 |
| td_nod_reconbike | TurnSpeed 80 (Speed 200) | expected 40 = Speed/5 |
| ts_gdi_hovermlrs | TurnSpeed 40 (Speed 80) | expected 16 = Speed/5 |
| ts_gdi_juggernaut | TurnSpeed 20 (Speed 71) | expected 14 = Speed/5 |
| ts_gdi_mobileemp | TurnSpeed 40 (Speed 100) | expected 20 = Speed/5 |
| ts_gdi_mobilesensorarray | TurnSpeed 40 (Speed 85) | expected 17 = Speed/5 |
| ts_nod_artillery | TurnSpeed 24 (Speed 60) | expected 12 = Speed/5 |
| ts_nod_mobilestealthgenerator | TurnSpeed 40 (Speed 56) | expected 11 = Speed/5 |
| ts_nod_ticktank | TurnSpeed 32 (Speed 90) | expected 18 = Speed/5 |


## F9 — Turreted.TurnSpeed ≠ Mobile.TurnSpeed  (50)

| actor | actual | expected |
|---|---|---|
| asianalliance_pulverizer | Turreted 26 vs Mobile 13 | must match |
| eden_tiger_thorshammer | Turreted 18 vs Mobile 16 | must match |
| futuretech_phalanxwip | Turreted 12 vs Mobile 19 | must match |
| japan_armoredcar | Turreted 52 vs Mobile 26 | must match |
| latinsyndicate_diablo | Turreted 36 vs Mobile 25 | must match |
| latinsyndicate_latinapc | Turreted 36 vs Mobile 18 | must match |
| latinsyndicate_smokertank | Turreted 21 vs Mobile 19 | must match |
| naxis_shoekarn | Turreted 15 vs Mobile 30 | must match |
| ordos_apc | Turreted 42 vs Mobile 21 | must match |
| ordos_heavyautoguntank | Turreted 12 vs Mobile 30 | must match |
| ra1_allies_alliedapc | Turreted 42 vs Mobile 21 | must match |
| ra1_allies_alliedheavyaatank | Turreted 30 vs Mobile 15 | must match |
| ra1_soviets_btr80 | Turreted 36 vs Mobile 18 | must match |
| ra1_soviets_flaktruck | Turreted 48 vs Mobile 24 | must match |
| ra1_soviets_gatlingtank | Turreted 30 vs Mobile 15 | must match |
| ra1_soviets_gorynychtank | Turreted 20 vs Mobile 12 | must match |
| ra1_soviets_v1rockettruck | Turreted 32 vs Mobile 20 | must match |
| ra2_allies_ifv | Turreted 60 vs Mobile 30 | must match |
| ra2_allies_ifv_chrono | Turreted 60 vs Mobile 30 | must match |
| ra2_allies_ifv_hmg | Turreted 60 vs Mobile 30 | must match |
| ra2_allies_ifv_mg | Turreted 60 vs Mobile 30 | must match |
| ra2_allies_ifv_missile | Turreted 60 vs Mobile 30 | must match |
| ra2_allies_ifv_repair | Turreted 60 vs Mobile 30 | must match |
| ra2_soviets_flaktrack | Turreted 38 vs Mobile 19 | must match |
| schwarzermond_laserbeetle | Turreted 34 vs Mobile 17 | must match |
| schwarzermond_lasertank | Turreted 28 vs Mobile 14 | must match |
| schwarzermond_lunarpanzer | Turreted 20 vs Mobile 18 | must match |
| schwarzermond_lunartiger | Turreted 20 vs Mobile 16 | must match |
| schwarzermond_m200bjagerline | Turreted 24 vs Mobile 12 | must match |
| steelconsortium_barracuda | Turreted 12 vs Mobile 16 | must match |
| td_gdi_apc | Turreted 40 vs Mobile 20 | must match |
| td_gdi_assaultapc | Turreted 25 vs Mobile 20 | must match |
| td_gdi_boxer | Turreted 32 vs Mobile 16 | must match |
| td_gdi_humveemkii | Turreted 46 vs Mobile 23 | must match |
| td_nod_buggymkii | Turreted 48 vs Mobile 24 | must match |
| td_nod_chemicalstealthtank | Turreted 25 vs Mobile 24 | must match |
| terran_cyclone | Turreted 46 vs Mobile 23 | must match |
| terran_goliath | Turreted 36 vs Mobile 18 | must match |
| terran_goliathmk2 | Turreted 30 vs Mobile 15 | must match |
| terran_matador | Turreted 15 vs Mobile 20 | must match |
| tkm_medictruck | Turreted 20 vs Mobile 15 | must match |
| tkm_t72m | Turreted 20 vs Mobile 16 | must match |
| tkm_trenchtruck | Turreted 15 vs Mobile 12 | must match |
| tkm_zaza | Turreted 30 vs Mobile 15 | must match |
| ts_gdi_hovermlrs | Turreted 16 vs Mobile 40 | must match |
| wirbelwind.nax | Turreted 34 vs Mobile 17 | must match |
| yuri_chaosdrone | Turreted 2000 vs Mobile 28 | must match |
| yuri_gatlingtank | Turreted 36 vs Mobile 18 | must match |
| yuri_lashertank | Turreted 23 vs Mobile 21 | must match |
| yuri_mastermind | Turreted 2000 vs Mobile 24 | must match |


## F10 — turretless TurnSpeed ≠ 2×Speed/5 (artillery: Speed/5)  (45)

| actor | actual | expected |
|---|---|---|
| asianalliance_heavyrailguntank | TurnSpeed 10 (Speed 50) | expected 20 = 2 x Speed/5 (turretless) |
| asianalliance_viper | TurnSpeed 25 (Speed 125) | expected 50 = 2 x Speed/5 (turretless) |
| asianalliance_warturtle | TurnSpeed 15 (Speed 75) | expected 30 = 2 x Speed/5 (turretless) |
| forgotten_thumperbus | TurnSpeed 18 (Speed 90) | expected 36 = 2 x Speed/5 (turretless) |
| futuretech_athenacannon | TurnSpeed 12 (Speed 60) | expected 24 = 2 x Speed/5 (turretless) |
| ixian_ixsiegetank | TurnSpeed 12 (Speed 60) | expected 24 = 2 x Speed/5 (turretless) |
| japan_ballista | TurnSpeed 13 (Speed 65) | expected 26 = 2 x Speed/5 (turretless) |
| latinsyndicate_burrito | TurnSpeed 16 (Speed 80) | expected 32 = 2 x Speed/5 (turretless) |
| latinsyndicate_topolm | TurnSpeed 15 (Speed 75) | expected 30 = 2 x Speed/5 (turretless) |
| naxis_brummbr | TurnSpeed 12 (Speed 60) | expected 24 = 2 x Speed/5 (turretless) |
| naxis_grille | TurnSpeed 16 (Speed 80) | expected 32 = 2 x Speed/5 (turretless) |
| naxis_nokana | TurnSpeed 12 (Speed 60) | expected 24 = 2 x Speed/5 (turretless) |
| naxis_oldtank | TurnSpeed 12 (Speed 50) | expected 20 = 2 x Speed/5 (turretless) |
| naxis_sturmtiger | TurnSpeed 6 (Speed 30) | expected 12 = 2 x Speed/5 (turretless) |
| protoss_dragoon | TurnSpeed 1023 (Speed 80) | expected 32 = 2 x Speed/5 (turretless) |
| protoss_probe | TurnSpeed 100 (Speed 100) | expected 40 = 2 x Speed/5 (turretless) |
| ra1_allies_alliedartillery | TurnSpeed 12 (Speed 60) | expected 24 = 2 x Speed/5 (turretless) |
| ra1_allies_mobilegapgenerator | TurnSpeed 15 (Speed 75) | expected 30 = 2 x Speed/5 (turretless) |
| ra1_allies_mobileradarjammer | TurnSpeed 20 (Speed 100) | expected 40 = 2 x Speed/5 (turretless) |
| ra1_soviets_madtank | TurnSpeed 12 (Speed 60) | expected 24 = 2 x Speed/5 (turretless) |
| ra1_soviets_nuclearv2launcher | TurnSpeed 16 (Speed 80) | expected 32 = 2 x Speed/5 (turretless) |
| ra1_soviets_v2rocketlauncher | TurnSpeed 17 (Speed 85) | expected 34 = 2 x Speed/5 (turretless) |
| ra2_allies_battlefortress | TurnSpeed 12 (Speed 60) | expected 24 = 2 x Speed/5 (turretless) |
| ra2_allies_battlefortress_2 | TurnSpeed 12 (Speed 60) | expected 24 = 2 x Speed/5 (turretless) |
| ra2_allies_battlefortress_3 | TurnSpeed 12 (Speed 60) | expected 24 = 2 x Speed/5 (turretless) |
| ra2_soviets_v3rocketlauncher | TurnSpeed 15 (Speed 75) | expected 30 = 2 x Speed/5 (turretless) |
| steelconsortium_dagger | TurnSpeed 20 (Speed 100) | expected 40 = 2 x Speed/5 (turretless) |
| steelconsortium_hammerheadartillerytank | TurnSpeed 20 (Speed 100) | expected 40 = 2 x Speed/5 (turretless) |
| steelconsortium_supportshieldgenerator | TurnSpeed 15 (Speed 75) | expected 30 = 2 x Speed/5 (turretless) |
| td_nod_artillery | TurnSpeed 11 (Speed 55) | expected 22 = 2 x Speed/5 (turretless) |
| td_nod_chemicalssmlauncher | TurnSpeed 15 (Speed 75) | expected 30 = 2 x Speed/5 (turretless) |
| td_nod_specterartillery | TurnSpeed 20 (Speed 100) | expected 40 = 2 x Speed/5 (turretless) |
| terran_scv | TurnSpeed 100 (Speed 100) | expected 40 = 2 x Speed/5 (turretless) |
| tkm_battlebus | TurnSpeed 20 (Speed 100) | expected 40 = 2 x Speed/5 (turretless) |
| tkm_radartruck | TurnSpeed 15 (Speed 75) | expected 30 = 2 x Speed/5 (turretless) |
| tkm_repairtruck | TurnSpeed 15 (Speed 75) | expected 30 = 2 x Speed/5 (turretless) |
| tkm_tornadoglauncher | TurnSpeed 16 (Speed 80) | expected 32 = 2 x Speed/5 (turretless) |
| ts_gdi_juggernautmkii | TurnSpeed 14 (Speed 70) | expected 28 = 2 x Speed/5 (turretless) |
| ts_nod_subterraneanapc | TurnSpeed 40 (Speed 60) | expected 24 = 2 x Speed/5 (turretless) |
| wc2_humans_mobileconstructionvehiclehuman | TurnSpeed 15 (Speed 75) | expected 30 = 2 x Speed/5 (turretless) |
| wc2_humans_paladin | TurnSpeed 92 (Speed 115) | expected 46 = 2 x Speed/5 (turretless) |
| wc2_humans_warcraft3knight | TurnSpeed 96 (Speed 120) | expected 48 = 2 x Speed/5 (turretless) |
| wc2_orcs_mobileconstructionvehicleorc | TurnSpeed 15 (Speed 75) | expected 30 = 2 x Speed/5 (turretless) |
| wc2_orcs_ogremage | TurnSpeed 68 (Speed 85) | expected 34 = 2 x Speed/5 (turretless) |
| zerg_drone | TurnSpeed 100 (Speed 100) | expected 40 = 2 x Speed/5 (turretless) |


## F11 — turreted artillery missing/incorrect firing-slow (Archer pattern)  (36)

| actor | actual | expected |
|---|---|---|
| asianalliance_pulverizer | firing-slow pattern missing | see gdiarcher (GrantConditionOnAttack + 50% multipliers) |
| asianalliance_type89mlrs | firing-slow pattern missing | see gdiarcher (GrantConditionOnAttack + 50% multipliers) |
| eden_lynx_railgun | firing-slow pattern missing | see gdiarcher (GrantConditionOnAttack + 50% multipliers) |
| eden_lynx_thorshammer | firing-slow pattern missing | see gdiarcher (GrantConditionOnAttack + 50% multipliers) |
| eden_tiger_railgun | firing-slow pattern missing | see gdiarcher (GrantConditionOnAttack + 50% multipliers) |
| eden_tiger_thorshammer | firing-slow pattern missing | see gdiarcher (GrantConditionOnAttack + 50% multipliers) |
| forgotten_missilevan | firing-slow pattern missing | see gdiarcher (GrantConditionOnAttack + 50% multipliers) |
| forgotten_mlrs | firing-slow pattern missing | see gdiarcher (GrantConditionOnAttack + 50% multipliers) |
| forgotten_warriortank | firing-slow pattern missing | see gdiarcher (GrantConditionOnAttack + 50% multipliers) |
| futuretech_beehivedronecarrier | firing-slow pattern missing | see gdiarcher (GrantConditionOnAttack + 50% multipliers) |
| futuretech_energizer | firing-slow pattern missing | see gdiarcher (GrantConditionOnAttack + 50% multipliers) |
| ixian_ixcombatsiege | firing-slow pattern missing | see gdiarcher (GrantConditionOnAttack + 50% multipliers) |
| ixian_shockraider | firing-slow pattern missing | see gdiarcher (GrantConditionOnAttack + 50% multipliers) |
| ixian_stormraider | firing-slow pattern missing | see gdiarcher (GrantConditionOnAttack + 50% multipliers) |
| japan_nanodronebuggy | firing-slow pattern missing | see gdiarcher (GrantConditionOnAttack + 50% multipliers) |
| japan_waveforceartillery | firing-slow pattern missing | see gdiarcher (GrantConditionOnAttack + 50% multipliers) |
| ordos_cobratank | firing-slow pattern missing | see gdiarcher (GrantConditionOnAttack + 50% multipliers) |
| ordos_pythontank | firing-slow pattern missing | see gdiarcher (GrantConditionOnAttack + 50% multipliers) |
| ra1_soviets_grad | RevokeDelay 5 | expected 57 (ReloadDelay 115/2) |
| ra1_soviets_heatraytank | firing-slow pattern missing | see gdiarcher (GrantConditionOnAttack + 50% multipliers) |
| ra1_soviets_teslatank | firing-slow pattern missing | see gdiarcher (GrantConditionOnAttack + 50% multipliers) |
| ra1_soviets_v1rockettruck | firing-slow pattern missing | see gdiarcher (GrantConditionOnAttack + 50% multipliers) |
| ra2_allies_prismtank | firing-slow pattern missing | see gdiarcher (GrantConditionOnAttack + 50% multipliers) |
| ra2_soviets_teslatank | firing-slow pattern missing | see gdiarcher (GrantConditionOnAttack + 50% multipliers) |
| schwarzermond_crystaltank | firing-slow pattern missing | see gdiarcher (GrantConditionOnAttack + 50% multipliers) |
| schwarzermond_lunargrille | firing-slow pattern missing | see gdiarcher (GrantConditionOnAttack + 50% multipliers) |
| schwarzermond_mars | firing-slow pattern missing | see gdiarcher (GrantConditionOnAttack + 50% multipliers) |
| td_gdi_mlrs | RevokeDelay 5 | expected 55 (ReloadDelay 111/2) |
| td_nod_ssmlauncher | firing-slow pattern missing | see gdiarcher (GrantConditionOnAttack + 50% multipliers) |
| terran_cyclone | firing-slow pattern missing | see gdiarcher (GrantConditionOnAttack + 50% multipliers) |
| tkm_stryker | firing-slow pattern missing | see gdiarcher (GrantConditionOnAttack + 50% multipliers) |
| ts_gdi_hovermlrs | firing-slow pattern missing | see gdiarcher (GrantConditionOnAttack + 50% multipliers) |
| ts_gdi_juggernaut | firing-slow pattern missing | see gdiarcher (GrantConditionOnAttack + 50% multipliers) |
| ts_nod_artillery | firing-slow pattern missing | see gdiarcher (GrantConditionOnAttack + 50% multipliers) |
| yuri_magnetron | firing-slow pattern missing | see gdiarcher (GrantConditionOnAttack + 50% multipliers) |
| zerg_lurker | firing-slow pattern missing | see gdiarcher (GrantConditionOnAttack + 50% multipliers) |


## F12 — anti-air defense not gated by the faction's radar tier  (0)

_none found_


## F13 — advanced defense not gated by the faction's tech tier  (3)

| actor | actual | expected |
|---|---|---|
| ordos: ordos_autogunturret | prereqs: ordos_barracks, ordos_constructionyard (gate 2, radar tier 3) | DEFERRED: valid, but faction's only pre-radar defense — add a Tier-1 defense before regating |
| ordos: ordos_artilleryplatform | prereqs: ordos_barracks, ordos_constructionyard (gate 2, radar tier 3) | DEFERRED: valid, but faction's only pre-radar defense — add a Tier-1 defense before regating |
| schwarzermond: schwarzermond_lasertower | prereqs: schwarzermond_barracks, schwarzermond_constructionyard (gate 2, radar tier 3) | DEFERRED: valid, but faction's only pre-radar defense — add a Tier-1 defense before regating |


## F14 — StartingUnits referencing nonexistent actors (crash class)  (0)

_none found_


## F15 — Light Support composition (Tier-1 only, ~2000, 5:1 inf:veh)  (63)

| actor | actual | expected |
|---|---|---|
| td_gdi: defaultgdia | total cost 1300 | target ~2000 (±15%) |
| td_gdi: defaultgdia | e3 (cost 200) x2 vs e1 (cost 100) x1 | pricier units must not outnumber cheaper ones |
| td_nod: defaultnoda | total cost 1300 | target ~2000 (±15%) |
| td_nod: defaultnoda | td_nod_buggy | light support must be Tier-1 only (producer-building prereqs only) |
| ra1_allies: defaultallies | 5 infantry : 2 vehicles | want ~5 infantry per vehicle |
| ra1_soviets: defaultsoviet | total cost 3000 | target ~2000 (±15%) |
| ra1_soviets: defaultsoviet | 5 infantry : 2 vehicles | want ~5 infantry per vehicle |
| japan: defaultjapan | 5 infantry : 2 vehicles | want ~5 infantry per vehicle |
| ts_gdi: defaulttsgdi | total cost 2460 | target ~2000 (±15%) |
| ts_gdi: defaulttsgdi | 5 infantry : 2 vehicles | want ~5 infantry per vehicle |
| ts_nod: defaulttsnod | 5 infantry : 2 vehicles | want ~5 infantry per vehicle |
| forgotten: defaultforgotten | total cost 2890 | target ~2000 (±15%) |
| forgotten: defaultforgotten | 5 infantry : 2 vehicles | want ~5 infantry per vehicle |
| forgotten: defaultforgotten | forgotten_mutantsergeant (cost 750) x2 vs forgotten_mutantsoldier (cost 250) x1 | pricier units must not outnumber cheaper ones |
| forgotten: defaultforgotten | forgotten_mutantsergeant (cost 750) x2 vs forgotten_raidercar (cost 300) x1 | pricier units must not outnumber cheaper ones |
| forgotten: defaultforgotten | forgotten_mutantsergeant (cost 750) x2 vs forgotten_rattytank (cost 600) x1 | pricier units must not outnumber cheaper ones |
| forgotten: defaultforgotten | forgotten_mutantsergeant, forgotten_mutantsoldier | light support must be Tier-1 only (producer-building prereqs only) |
| ra2_allies: defaultra2allies | total cost 2650 | target ~2000 (±15%) |
| ra2_allies: defaultra2allies | 5 infantry : 2 vehicles | want ~5 infantry per vehicle |
| ra2_soviets: defaultra2soviets | total cost 2650 | target ~2000 (±15%) |
| ra2_soviets: defaultra2soviets | 5 infantry : 2 vehicles | want ~5 infantry per vehicle |
| yuri: defaultyuri | total cost 3350 | target ~2000 (±15%) |
| yuri: defaultyuri | 6 infantry : 2 vehicles | want ~5 infantry per vehicle |
| yuri: defaultyuri | yuri_brute (cost 400) x2 vs yrslav (cost 250) x1 | pricier units must not outnumber cheaper ones |
| asianalliance: defaultasianalliance | total cost 2650 | target ~2000 (±15%) |
| asianalliance: defaultasianalliance | 5 infantry : 2 vehicles | want ~5 infantry per vehicle |
| steelconsortium: defaultconsortium | total cost 4750 | target ~2000 (±15%) |
| steelconsortium: defaultconsortium | 5 infantry : 2 vehicles | want ~5 infantry per vehicle |
| steelconsortium: defaultconsortium | steelconsortium_quantummissiletrooper (cost 1150) x2 vs steelconsortium_manta (cost 850) x1 | pricier units must not outnumber cheaper ones |
| steelconsortium: defaultconsortium | steelconsortium_quantummissiletrooper (cost 1150) x2 vs steelconsortium_hammerheadartillerytank (cost 1000) x1 | pricier units must not outnumber cheaper ones |
| steelconsortium: defaultconsortium | steelconsortium_quantummissiletrooper | light support must be Tier-1 only (producer-building prereqs only) |
| latinsyndicate: defaultsyndicate | total cost 8990 | target ~2000 (±15%) |
| latinsyndicate: defaultsyndicate | 5 infantry : 2 vehicles | want ~5 infantry per vehicle |
| latinsyndicate: defaultsyndicate | latinsyndicate_freedomfighter (cost 3000) x2 vs wirbelwind.nax (cost 1800) x1 | pricier units must not outnumber cheaper ones |
| latinsyndicate: defaultsyndicate | latinsyndicate_freedomfighter (cost 3000) x2 vs tiger.nax (cost 800) x1 | pricier units must not outnumber cheaper ones |
| latinsyndicate: defaultsyndicate | latinsyndicate_freedomfighter | light support must be Tier-1 only (producer-building prereqs only) |
| naxis: defaultnaxis | total cost 3650 | target ~2000 (±15%) |
| naxis: defaultnaxis | 5 infantry : 2 vehicles | want ~5 infantry per vehicle |
| naxis: defaultnaxis | naxis_sssoldier | light support must be Tier-1 only (producer-building prereqs only) |
| schwarzermond: defaultschwarzermond | total cost 2710 | target ~2000 (±15%) |
| schwarzermond: defaultschwarzermond | 5 infantry : 2 vehicles | want ~5 infantry per vehicle |
| futuretech: defaultfuturetech | total cost 3050 | target ~2000 (±15%) |
| futuretech: defaultfuturetech | 0 infantry : 7 vehicles | want ~5 infantry per vehicle |
| futuretech: defaultfuturetech | futuretech_cannondroid, futuretech_missiledroid, futuretech_scoutdroid | light support must be Tier-1 only (producer-building prereqs only) |
| tkm: defaulttstkm | 5 infantry : 2 vehicles | want ~5 infantry per vehicle |
| ordos: ordos_L | total cost 3300 | target ~2000 (±15%) |
| ordos: ordos_L | 5 infantry : 2 vehicles | want ~5 infantry per vehicle |
| atreides: ixian_L | total cost 3300 | target ~2000 (±15%) |
| atreides: ixian_L | 5 infantry : 2 vehicles | want ~5 infantry per vehicle |
| ixian: ixian_only_L | total cost 3300 | target ~2000 (±15%) |
| ixian: ixian_only_L | 5 infantry : 2 vehicles | want ~5 infantry per vehicle |
| terran: defaultterran | total cost 2600 | target ~2000 (±15%) |
| protoss: defaultprotoss | total cost 1500 | target ~2000 (±15%) |
| protoss: defaultprotoss | 1 infantry : 1 vehicles | want ~5 infantry per vehicle |
| protoss: defaultprotoss | protoss_zealot | light support must be Tier-1 only (producer-building prereqs only) |
| plymouth: defaultplymouth | total cost 3050 | target ~2000 (±15%) |
| plymouth: defaultplymouth | 0 infantry : 6 vehicles | want ~5 infantry per vehicle |
| plymouth: defaultplymouth | plymouth_lynx_microwave (cost 500) x3 vs plymouth_scout (cost 350) x1 | pricier units must not outnumber cheaper ones |
| plymouth: defaultplymouth | plymouth_lynx_rpg (cost 600) x2 vs plymouth_scout (cost 350) x1 | pricier units must not outnumber cheaper ones |
| eden: defaulteden | total cost 4350 | target ~2000 (±15%) |
| eden: defaulteden | 0 infantry : 6 vehicles | want ~5 infantry per vehicle |
| eden: defaulteden | eden_lynx_laser (cost 750) x3 vs eden_scout (cost 300) x1 | pricier units must not outnumber cheaper ones |
| eden: defaulteden | eden_lynx_railgun (cost 900) x2 vs eden_scout (cost 300) x1 | pricier units must not outnumber cheaper ones |


## F16 — Heavy Support composition (all tiers, ~10000, 5:1 inf:veh)  (110)

| actor | actual | expected |
|---|---|---|
| td_gdi: heavygdia | total cost 3000 | target ~10000 (±15%) |
| td_gdi: heavygdia | 6 infantry : 3 vehicles | want ~5 infantry per vehicle |
| td_gdi: heavygdia | td_gdi_battletank (cost 900) x2 vs td_gdi_humvee (cost 400) x1 | pricier units must not outnumber cheaper ones |
| td_gdi: heavygdia | all units are Tier 1 | heavy support should mix all tiers |
| td_gdi: heavygdib | total cost 3600 | target ~10000 (±15%) |
| td_gdi: heavygdib | all units are Tier 1 | heavy support should mix all tiers |
| td_nod: heavynoda | total cost 2800 | target ~10000 (±15%) |
| td_nod: heavynoda | 6 infantry : 3 vehicles | want ~5 infantry per vehicle |
| td_nod: heavynodb | total cost 2700 | target ~10000 (±15%) |
| ra1_allies: heavyallies | total cost 3800 | target ~10000 (±15%) |
| ra1_allies: heavyallies | 5 infantry : 5 vehicles | want ~5 infantry per vehicle |
| ra1_allies: heavyallies | ra1_allies_alliedmediumtank (cost 700) x3 vs ra1_allies_alliedrocketsoldier (cost 300) x2 | pricier units must not outnumber cheaper ones |
| ra1_allies: heavyallies | ra1_allies_alliedmediumtank (cost 700) x3 vs ra1_allies_ranger (cost 300) x1 | pricier units must not outnumber cheaper ones |
| ra1_allies: heavyallies | ra1_allies_alliedmediumtank (cost 700) x3 vs ra1_allies_alliedlighttank (cost 500) x1 | pricier units must not outnumber cheaper ones |
| ra1_allies: heavyallies | all units are Tier 1 | heavy support should mix all tiers |
| ra1_soviets: heavysoviet | total cost 5000 | target ~10000 (±15%) |
| ra1_soviets: heavysoviet | 5 infantry : 4 vehicles | want ~5 infantry per vehicle |
| ra1_soviets: heavysoviet | ra1_soviets_heavytank (cost 1000) x2 vs ra1_soviets_flaktruck (cost 800) x1 | pricier units must not outnumber cheaper ones |
| ra1_soviets: heavysoviet | all units are Tier 1 | heavy support should mix all tiers |
| japan: heavyjapan | total cost 6100 | target ~10000 (±15%) |
| japan: heavyjapan | 5 infantry : 6 vehicles | want ~5 infantry per vehicle |
| japan: heavyjapan | japan_igomediumtank (cost 800) x2 vs japan_scoutcar (cost 300) x1 | pricier units must not outnumber cheaper ones |
| japan: heavyjapan | japan_grenadebuggy (cost 900) x2 vs japan_scoutcar (cost 300) x1 | pricier units must not outnumber cheaper ones |
| japan: heavyjapan | all units are Tier 1 | heavy support should mix all tiers |
| ts_gdi: heavytsgdi | total cost 5310 | target ~10000 (±15%) |
| ts_gdi: heavytsgdi | 5 infantry : 5 vehicles | want ~5 infantry per vehicle |
| ts_gdi: heavytsgdi | ts_gdi_titan (cost 950) x4 vs tse1 (cost 120) x3 | pricier units must not outnumber cheaper ones |
| ts_gdi: heavytsgdi | ts_gdi_titan (cost 950) x4 vs ts_gdi_discthrower (cost 300) x2 | pricier units must not outnumber cheaper ones |
| ts_gdi: heavytsgdi | ts_gdi_titan (cost 950) x4 vs ts_gdi_wolverine (cost 550) x1 | pricier units must not outnumber cheaper ones |
| ts_gdi: heavytsgdi | all units are Tier 1 | heavy support should mix all tiers |
| ts_nod: heavytsnod | total cost 4610 | target ~10000 (±15%) |
| ts_nod: heavytsnod | 5 infantry : 5 vehicles | want ~5 infantry per vehicle |
| ts_nod: heavytsnod | ts_nod_ticktank (cost 800) x4 vs tse1 (cost 120) x3 | pricier units must not outnumber cheaper ones |
| ts_nod: heavytsnod | ts_nod_ticktank (cost 800) x4 vs ts_nod_rocketinfantry (cost 300) x2 | pricier units must not outnumber cheaper ones |
| ts_nod: heavytsnod | ts_nod_ticktank (cost 800) x4 vs ts_nod_attackbuggy (cost 450) x1 | pricier units must not outnumber cheaper ones |
| ts_nod: heavytsnod | all units are Tier 1 | heavy support should mix all tiers |
| forgotten: heavyforgotten | 5 infantry : 5 vehicles | want ~5 infantry per vehicle |
| forgotten: heavyforgotten | forgotten_mutantsergeant (cost 750) x2 vs forgotten_mutantsoldier (cost 250) x1 | pricier units must not outnumber cheaper ones |
| forgotten: heavyforgotten | forgotten_mutantsergeant (cost 750) x2 vs forgotten_raidercar (cost 300) x1 | pricier units must not outnumber cheaper ones |
| forgotten: heavyforgotten | forgotten_mutantsergeant (cost 750) x2 vs forgotten_rattytank (cost 600) x1 | pricier units must not outnumber cheaper ones |
| forgotten: heavyforgotten | forgotten_warriortank (cost 2000) x3 vs forgotten_mutant (cost 120) x2 | pricier units must not outnumber cheaper ones |
| forgotten: heavyforgotten | forgotten_warriortank (cost 2000) x3 vs forgotten_mutantsoldier (cost 250) x1 | pricier units must not outnumber cheaper ones |
| forgotten: heavyforgotten | forgotten_warriortank (cost 2000) x3 vs forgotten_mutantsergeant (cost 750) x2 | pricier units must not outnumber cheaper ones |
| forgotten: heavyforgotten | forgotten_warriortank (cost 2000) x3 vs forgotten_raidercar (cost 300) x1 | pricier units must not outnumber cheaper ones |
| forgotten: heavyforgotten | forgotten_warriortank (cost 2000) x3 vs forgotten_rattytank (cost 600) x1 | pricier units must not outnumber cheaper ones |
| ra2_allies: heavyra2allies | total cost 6150 | target ~10000 (±15%) |
| ra2_allies: heavyra2allies | 5 infantry : 6 vehicles | want ~5 infantry per vehicle |
| ra2_allies: heavyra2allies | ra2_allies_grizzlytank (cost 750) x3 vs ra2_allies_guardiangi (cost 400) x2 | pricier units must not outnumber cheaper ones |
| ra2_allies: heavyra2allies | ra2_allies_grizzlytank (cost 750) x3 vs ra2_allies_ifv (cost 500) x2 | pricier units must not outnumber cheaper ones |
| ra2_allies: heavyra2allies | all units are Tier 1 | heavy support should mix all tiers |
| ra2_soviets: heavyra2soviets | total cost 5250 | target ~10000 (±15%) |
| ra2_soviets: heavyra2soviets | 5 infantry : 4 vehicles | want ~5 infantry per vehicle |
| yuri: heavyyuri | total cost 6250 | target ~10000 (±15%) |
| yuri: heavyyuri | 6 infantry : 6 vehicles | want ~5 infantry per vehicle |
| yuri: heavyyuri | yuri_brute (cost 400) x2 vs yrslav (cost 250) x1 | pricier units must not outnumber cheaper ones |
| yuri: heavyyuri | yuri_gatlingtank (cost 1100) x2 vs yrslav (cost 250) x1 | pricier units must not outnumber cheaper ones |
| yuri: heavyyuri | yuri_lashertank (cost 600) x4 vs yuri_initiate (cost 200) x3 | pricier units must not outnumber cheaper ones |
| yuri: heavyyuri | yuri_lashertank (cost 600) x4 vs yuri_brute (cost 400) x2 | pricier units must not outnumber cheaper ones |
| yuri: heavyyuri | yuri_lashertank (cost 600) x4 vs yrslav (cost 250) x1 | pricier units must not outnumber cheaper ones |
| yuri: heavyyuri | all units are Tier 1 | heavy support should mix all tiers |
| asianalliance: heavyasianalliance | total cost 7650 | target ~10000 (±15%) |
| asianalliance: heavyasianalliance | 5 infantry : 6 vehicles | want ~5 infantry per vehicle |
| asianalliance: heavyasianalliance | asianalliance_lynxtank (cost 850) x3 vs asianalliance_asiantankkiller (cost 300) x2 | pricier units must not outnumber cheaper ones |
| steelconsortium: heavyconsortium | 5 infantry : 6 vehicles | want ~5 infantry per vehicle |
| steelconsortium: heavyconsortium | steelconsortium_quantumtank (cost 1600) x4 vs steelconsortium_clonetrooper (cost 200) x3 | pricier units must not outnumber cheaper ones |
| steelconsortium: heavyconsortium | steelconsortium_quantumtank (cost 1600) x4 vs steelconsortium_quantummissiletrooper (cost 1150) x2 | pricier units must not outnumber cheaper ones |
| steelconsortium: heavyconsortium | steelconsortium_quantumtank (cost 1600) x4 vs steelconsortium_manta (cost 850) x2 | pricier units must not outnumber cheaper ones |
| latinsyndicate: heavysyndicate | total cost 14790 | target ~10000 (±15%) |
| latinsyndicate: heavysyndicate | 5 infantry : 6 vehicles | want ~5 infantry per vehicle |
| latinsyndicate: heavysyndicate | latinsyndicate_freedomfighter (cost 3000) x2 vs ptnk.asian (cost 2400) x1 | pricier units must not outnumber cheaper ones |
| naxis: heavynaxis | 5 infantry : 6 vehicles | want ~5 infantry per vehicle |
| naxis: heavynaxis | wirbelwind.nax (cost 1800) x3 vs naxis_sssoldier (cost 375) x2 | pricier units must not outnumber cheaper ones |
| naxis: heavynaxis | tiger.nax (cost 800) x3 vs naxis_sssoldier (cost 375) x2 | pricier units must not outnumber cheaper ones |
| schwarzermond: heavyschwarzermond | total cost 5640 | target ~10000 (±15%) |
| schwarzermond: heavyschwarzermond | 5 infantry : 6 vehicles | want ~5 infantry per vehicle |
| schwarzermond: heavyschwarzermond | schwarzermond_lunarrocket (cost 350) x3 vs schwarzermond_lunarsoldier (cost 120) x2 | pricier units must not outnumber cheaper ones |
| schwarzermond: heavyschwarzermond | schwarzermond_laserbeetle (cost 700) x3 vs schwarzermond_lunarsoldier (cost 120) x2 | pricier units must not outnumber cheaper ones |
| schwarzermond: heavyschwarzermond | schwarzermond_laserbeetle (cost 700) x3 vs schwarzermond_lunarpanzer (cost 650) x2 | pricier units must not outnumber cheaper ones |
| schwarzermond: heavyschwarzermond | all units are Tier 1 | heavy support should mix all tiers |
| futuretech: heavyfuturetech | total cost 5325 | target ~10000 (±15%) |
| futuretech: heavyfuturetech | 0 infantry : 11 vehicles | want ~5 infantry per vehicle |
| futuretech: heavyfuturetech | futuretech_cannondroid (cost 525) x5 vs futuretech_scoutdroid (cost 200) x3 | pricier units must not outnumber cheaper ones |
| tkm: heavytstkm | total cost 3060 | target ~10000 (±15%) |
| tkm: heavytstkm | 5 infantry : 5 vehicles | want ~5 infantry per vehicle |
| tkm: heavytstkm | tkm_technical (cost 400) x4 vs tkm_rifleman (cost 120) x3 | pricier units must not outnumber cheaper ones |
| tkm: heavytstkm | tkm_technical (cost 400) x4 vs tkm_rocketeer (cost 200) x2 | pricier units must not outnumber cheaper ones |
| tkm: heavytstkm | all units are Tier 1 | heavy support should mix all tiers |
| ordos: ordos_h | total cost 7100 | target ~10000 (±15%) |
| ordos: ordos_h | 5 infantry : 4 vehicles | want ~5 infantry per vehicle |
| ordos: ordos_h | all units are Tier 1 | heavy support should mix all tiers |
| atreides: ixian_h | total cost 7100 | target ~10000 (±15%) |
| atreides: ixian_h | 5 infantry : 4 vehicles | want ~5 infantry per vehicle |
| ixian: ixian_only_h | total cost 7100 | target ~10000 (±15%) |
| ixian: ixian_only_h | 5 infantry : 4 vehicles | want ~5 infantry per vehicle |
| terran: heavyterran | total cost 8000 | target ~10000 (±15%) |
| terran: heavyterran | 4 infantry : 3 vehicles | want ~5 infantry per vehicle |
| terran: heavyterran | terran_siegetank (cost 2800) x2 vs terran_firebat (cost 500) x1 | pricier units must not outnumber cheaper ones |
| terran: heavyterran | terran_siegetank (cost 2800) x2 vs terran_medic (cost 600) x1 | pricier units must not outnumber cheaper ones |
| terran: heavyterran | terran_siegetank (cost 2800) x2 vs terran_vulture (cost 900) x1 | pricier units must not outnumber cheaper ones |
| protoss: heavyprotoss | total cost 3000 | target ~10000 (±15%) |
| protoss: heavyprotoss | 2 infantry : 2 vehicles | want ~5 infantry per vehicle |
| zerg: heavyzerg | total cost 4100 | target ~10000 (±15%) |
| plymouth: heavyplymouth | total cost 3650 | target ~10000 (±15%) |
| plymouth: heavyplymouth | 0 infantry : 7 vehicles | want ~5 infantry per vehicle |
| plymouth: heavyplymouth | plymouth_lynx_microwave (cost 500) x3 vs plymouth_scout (cost 350) x1 | pricier units must not outnumber cheaper ones |
| plymouth: heavyplymouth | plymouth_lynx_rpg (cost 600) x3 vs plymouth_scout (cost 350) x1 | pricier units must not outnumber cheaper ones |
| eden: heavyeden | total cost 5250 | target ~10000 (±15%) |
| eden: heavyeden | 0 infantry : 7 vehicles | want ~5 infantry per vehicle |
| eden: heavyeden | eden_lynx_laser (cost 750) x3 vs eden_scout (cost 300) x1 | pricier units must not outnumber cheaper ones |
| eden: heavyeden | eden_lynx_railgun (cost 900) x3 vs eden_scout (cost 300) x1 | pricier units must not outnumber cheaper ones |


## F17 — fighter/bomber TurnSpeed ≠ Speed/15 (frontal: 2×)  (6)

| actor | actual | expected |
|---|---|---|
| forgotten_cropplane | TurnSpeed 64 (Speed 160) | expected 11 = Speed/15 |
| ordos_airmine | TurnSpeed 8 (Speed 35) | expected 2 = Speed/15 |
| ra1_soviets_supersonicnuclearbomber | TurnSpeed 15 (Speed 200) | expected 13 = Speed/15 |
| schwarzermond_blackbomb | TurnSpeed 10 (Speed 75) | expected 5 = Speed/15 |
| tkm_viper | TurnSpeed 25 (Speed 150) | expected 10 = Speed/15 |
| zerg_scourge | TurnSpeed 40 (Speed 200) | expected 13 = Speed/15 |


## F18 — weapons targeting Air whose damage warheads can't hit Air  (20)

| actor | actual | expected |
|---|---|---|
| beehivecarriertarget | Warhead@1Dam | targets Air but no damage warhead hits Air (used by futuretech_beehivedronecarrier) |
| boomerlaunch | Warhead@1Dam | targets Air but no damage warhead hits Air (used by yuri_boomersubmarine) |
| defilerplague | Warhead@HeavyChemicalWeapon, Warhead@HeavyChemicalWeaponFriendlyFire, Warhead@HeavyChemicalWeaponPercentage, Warhead@Concrete | targets Air but no damage warhead hits Air (used by zerg_defiler) |
| ivanattachair | Warhead@2 | targets Air but no damage warhead hits Air (used by ra2_soviets_crazyivan) |
| naxdefensiveplanetarget | Warhead@1Dam | targets Air but no damage warhead hits Air (used by naxis_airfield, schwarzermond_airfield) |
| naxdieglocke | Warhead@HeavyChemicalWeapon, Warhead@HeavyChemicalWeaponFriendlyFire, Warhead@HeavyChemicalWeaponPercentage, Warhead@Concrete | targets Air but no damage warhead hits Air (used by schwarzermond_dieglocke) |
| pdlaserbike | Warhead@1Dam | targets Air but no damage warhead hits Air (used by td_nod_chemicalattackbike, td_nod_reconbike) |
| pdlaserltnk2 | Warhead@1Dam | targets Air but no damage warhead hits Air (used by td_nod_lighttankmkii) |
| sciencevesseldefensematrix | Warhead@1 | targets Air but no damage warhead hits Air (used by terran_sciencevessel) |
| tkmpdlaser | Warhead@1Dam | targets Air but no damage warhead hits Air (used by tkm_t72m) |
| tsassaultcannon | Warhead@FlakWeapon, Warhead@FlakWeaponPercentage, Warhead@Concrete, Warhead@Chaingun | targets Air but no damage warhead hits Air (used by ts_gdi_wolverine) |
| tsassaultcannontal | Warhead@Chaingun, Warhead@ChaingunPercentage, Warhead@Concrete | targets Air but no damage warhead hits Air (used by ts_gdi_wolverinemkii) |
| tsfiendshard | Warhead@LightChemicalWeapon, Warhead@LightChemicalWeaponFriendlyFire, Warhead@LightChemicalWeaponPercentage, Warhead@Concrete | targets Air but no damage warhead hits Air (used by forgotten_tiberianfiend) |
| tsfiendshardblue | Warhead@Grenade, Warhead@GrenadeFriendlyFire, Warhead@GrenadePercentage, Warhead@Concrete | targets Air but no damage warhead hits Air (used by forgotten_viniferafiend) |
| tsfiendshardblueup | Warhead@Grenade, Warhead@GrenadeFriendlyFire, Warhead@GrenadePercentage, Warhead@Concrete | targets Air but no damage warhead hits Air (used by forgotten_viniferafiend) |
| tsfiendshardup | Warhead@LightChemicalWeapon, Warhead@LightChemicalWeaponFriendlyFire, Warhead@LightChemicalWeaponPercentage, Warhead@Concrete | targets Air but no damage warhead hits Air (used by forgotten_tiberianfiend) |
| tstacticalchemmissile | Warhead@Concrete | targets Air but no damage warhead hits Air (used by ts_nod_missilesilo) |
| tstacticalmissile | Warhead@Concrete | targets Air but no damage warhead hits Air (used by ts_nod_missilesilo) |
| wc2deathknightdeathanddecay | Warhead@1Dam_impact | targets Air but no damage warhead hits Air (used by wc2_orcs_deathknight) |
| wc2mageblizzard | Warhead@1Dam_impact | targets Air but no damage warhead hits Air (used by wc2_humans_archmage, wc2_humans_mage) |


## F19 — helicopter/spaceship TurnSpeed ≠ Speed/5  (32)

| actor | actual | expected |
|---|---|---|
| forgotten_apache | TurnSpeed 64 (Speed 160) | expected 32 = Speed/5 |
| forgotten_cobracopter | TurnSpeed 66 (Speed 165) | expected 33 = Speed/5 |
| forgotten_wasp | TurnSpeed 80 (Speed 200) | expected 40 = Speed/5 |
| futuretech_harbingergunship | TurnSpeed 8 (Speed 125) | expected 25 = Speed/5 |
| ixian_farasha | TurnSpeed 16 (Speed 40) | expected 8 = Speed/5 |
| naxis_transportzeppelin | TurnSpeed 20 (Speed 35) | expected 7 = Speed/5 |
| ordos_wraith | TurnSpeed 45 (Speed 45) | expected 9 = Speed/5 |
| protoss_arbiter | TurnSpeed 28 (Speed 75) | expected 15 = Speed/5 |
| protoss_carrier | TurnSpeed 18 (Speed 45) | expected 9 = Speed/5 |
| protoss_shuttle | TurnSpeed 20 (Speed 150) | expected 30 = Speed/5 |
| protoss_starshipsovereign | TurnSpeed 16 (Speed 40) | expected 8 = Speed/5 |
| ra1_soviets_hiptransport | TurnSpeed 10 (Speed 100) | expected 20 = Speed/5 |
| ra2_soviets_kirovairship | TurnSpeed 12 (Speed 30) | expected 6 = Speed/5 |
| ra2_soviets_transportkirov | TurnSpeed 14 (Speed 35) | expected 7 = Speed/5 |
| schwarzermond_dieglocke | TurnSpeed 40 (Speed 40) | expected 8 = Speed/5 |
| schwarzermond_haunebuii | TurnSpeed 66 (Speed 66) | expected 13 = Speed/5 |
| schwarzermond_haunebuiii | TurnSpeed 55 (Speed 55) | expected 11 = Speed/5 |
| schwarzermond_spacezeppelin | TurnSpeed 35 (Speed 35) | expected 7 = Speed/5 |
| steelconsortium_cloudbreaker | TurnSpeed 20 (Speed 50) | expected 10 = Speed/5 |
| steelconsortium_empressstation | TurnSpeed 25 (Speed 25) | expected 5 = Speed/5 |
| td_gdi_chinooktransport | TurnSpeed 20 (Speed 150) | expected 30 = Speed/5 |
| td_nod_chinooktransport | TurnSpeed 20 (Speed 150) | expected 30 = Speed/5 |
| terran_battlecruiser | TurnSpeed 12 (Speed 30) | expected 6 = Speed/5 |
| terran_phobos | TurnSpeed 10 (Speed 25) | expected 5 = Speed/5 |
| terran_pythean | TurnSpeed 16 (Speed 40) | expected 8 = Speed/5 |
| terran_sciencevessel | TurnSpeed 28 (Speed 66) | expected 13 = Speed/5 |
| ts_nod_harpy | TurnSpeed 56 (Speed 140) | expected 28 = Speed/5 |
| wc2_humans_flyingmachine | TurnSpeed 28 (Speed 165) | expected 33 = Speed/5 |
| wc2_humans_gyrocoptermachine | TurnSpeed 28 (Speed 165) | expected 33 = Speed/5 |
| wc2_orcs_goblinzeppelin | TurnSpeed 28 (Speed 165) | expected 33 = Speed/5 |
| yuri_floatingdisk | TurnSpeed 80 (Speed 80) | expected 16 = Speed/5 |
| zerg_behemoth | TurnSpeed 12 (Speed 30) | expected 6 = Speed/5 |


## F20 — AA support vehicle: air range ≠ 1.5 × ground range  (6)

| actor | actual | expected |
|---|---|---|
| latinsyndicate_diablo | AA range 10450 vs ground 7300 | expected 10950 = 1.5 x ground range |
| latinsyndicate_latinapc | AA range 9610 vs ground 6740 | expected 10110 = 1.5 x ground range |
| ordos_laboratorycrawler | AA range 6500 vs ground 6500 | expected 9750 = 1.5 x ground range |
| protoss_analogue | AA range 2000 vs ground 1500 | expected 2250 = 1.5 x ground range |
| ra2_soviets_flaktrack | AA range 9292 vs ground 6528 | expected 9792 = 1.5 x ground range |
| wirbelwind.nax | AA range 9052 vs ground 6368 | expected 9552 = 1.5 x ground range |


## F22 — promotion tech gate ≠ unlocked unit's tech gate  (8)

| actor | actual | expected |
|---|---|---|
| futuretech: futuretech_cryolegionnaire | unit tech tier 7 | promotion futuretech_promotion_unlockcryolegionnaire tier 0 — must match |
| futuretech: futuretech_futuretank | unit tech tier 7 | promotion futuretech_promotion_unlockfuturetank tier 0 — must match |
| futuretech: futuretech_harbingergunship | unit tech tier 7 | promotion futuretech_promotion_unlockharbingergunship tier 0 — must match |
| latinsyndicate: latinsyndicate_burrito | unit tech tier 5 | promotion latinsyndicate_promotion_unlockburritos tier 0 — must match |
| latinsyndicate: latinsyndicate_lars | unit tech tier 5 | promotion latinsyndicate_promotion_unlocklars tier 0 — must match |
| latinsyndicate: latinsyndicate_topolm | unit tech tier 5 | promotion latinsyndicate_promotion_unlocktopolm tier 0 — must match |
| steelconsortium: steelconsortium_cloudbreaker | unit tech tier 5 | promotion steelconsortium_promotion_unlockcloudbreaker tier 0 — must match |
| ts_gdi: ts_gdi_kodiakcommandship | unit tech tier 5 | promotion ts_gdi_promotion_unlockkodiak tier 0 — must match |

