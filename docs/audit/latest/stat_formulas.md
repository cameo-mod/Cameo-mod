# audit_stat_formulas — house stat formulas

Violations: **787** across 1836 roster actors (reference-clean units: gdiarcher, raider.ordos)


## F1 — Repairable.HpPerStep ≠ HP/20  (42)

| actor | actual | expected |
|---|---|---|
| beetle.nax2 | HpPerStep 4375 | expected 2375 (HP 47500/20) |
| bmwbike.nax | HpPerStep 4125 | expected 1100 (HP 22000/20) |
| bomber.ixian | HpPerStep 5555 | expected 5550 (HP 111000/20) |
| cabal_heavyspider | HpPerStep 1500 | expected 4000 (HP 80000/20) |
| cabal_scarabapc | HpPerStep 2637 | expected 1500 (HP 30000/20) |
| forgotten_scoopertank | HpPerStep 10000 | expected 12500 (HP 250000/20) |
| gravity.nax2 | HpPerStep 3125 | expected 15000 (HP 300000/20) |
| imperial.nax | HpPerStep 3125 | expected 4125 (HP 82500/20) |
| landcarr.futu | HpPerStep 6500 | expected 6250 (HP 125000/20) |
| modcore | HpPerStep 15000 | expected 2500 (HP 50000/20) |
| modcore1 | HpPerStep 15000 | expected 2500 (HP 50000/20) |
| modcore2 | HpPerStep 15000 | expected 2500 (HP 50000/20) |
| modcore3 | HpPerStep 15000 | expected 2500 (HP 50000/20) |
| modcore4 | HpPerStep 15000 | expected 2500 (HP 50000/20) |
| modcore5 | HpPerStep 15000 | expected 2500 (HP 50000/20) |
| modcore6 | HpPerStep 15000 | expected 2500 (HP 50000/20) |
| modcore7 | HpPerStep 15000 | expected 2500 (HP 50000/20) |
| nokana.nax | HpPerStep 3125 | expected 22500 (HP 450000/20) |
| shoe.nax | HpPerStep 10000 | expected 7500 (HP 150000/20) |
| sovstalinfist | HpPerStep 15000 | expected 5000 (HP 100000/20) |
| sturmtiger.nax | HpPerStep 2000 | expected 12500 (HP 250000/20) |
| t30 | HpPerStep 2637 | expected 20000 (HP 400000/20) |
| t72 | HpPerStep 2637 | expected 5000 (HP 100000/20) |
| tkmabrams | HpPerStep 4800 | expected 6000 (HP 120000/20) |
| tkmbigshiee | HpPerStep 6000 | expected 25000 (HP 500000/20) |
| tkmdronepodtruck | HpPerStep 1375 | expected 3000 (HP 60000/20) |
| tkmmedictruck | HpPerStep 1375 | expected 2500 (HP 50000/20) |
| tkmquadtruck | HpPerStep 1375 | expected 3250 (HP 65000/20) |
| tkmradartruck | HpPerStep 1375 | expected 3750 (HP 75000/20) |
| tkmratflak | HpPerStep 1375 | expected 6000 (HP 120000/20) |
| tkmrepairtruck | HpPerStep 1375 | expected 2500 (HP 50000/20) |
| tkmsandmarine | HpPerStep 6000 | expected 40000 (HP 800000/20) |
| tkmstryker | HpPerStep 1375 | expected 4000 (HP 80000/20) |
| tkmtrenchtank | HpPerStep 2637 | expected 10000 (HP 200000/20) |
| tkmtrenchtruck | HpPerStep 2637 | expected 5000 (HP 100000/20) |
| ts_gdi_mobilesensorarray | HpPerStep 2637 | expected 3000 (HP 60000/20) |
| ts_nod_mobilestealthgenerator | HpPerStep 2637 | expected 1000 (HP 20000/20) |
| ts_nod_subterraneanapc | HpPerStep 2637 | expected 875 (HP 17500/20) |
| tsbike | HpPerStep 100 | expected 1000 (HP 20000/20) |
| zep.nax | HpPerStep 2625 | expected 62500 (HP 1250000/20) |
| zep.nax2 | HpPerStep 2625 | expected 67500 (HP 1350000/20) |
| zombietank.nax | HpPerStep 7875 | expected 5500 (HP 110000/20) |


## F2 — SelfHealing Step ≠ HP/2500 (inf: HP/1000)  (90)

| actor | actual | expected |
|---|---|---|
| athena.futu | Step 10 | expected 25 (HP 62500/2500) |
| bbomb.nax2 | Step 7 | expected 15 (HP 37500/2500) |
| beetle.nax2 | Step 35 | expected 19 (HP 47500/2500) |
| bf109.nax | Step 30 | expected 24 (HP 60000/2500) |
| bmwbike.nax | Step 33 | expected 9 (HP 22000/2500) |
| cabal_ascended | Step 25 | expected 60 (HP 60000/1000) |
| cabal_constructionyard | Step 10 | expected 400 (HP 1000000/2500) |
| cabal_devout | Step 25 | expected 60 (HP 60000/1000) |
| cabal_eliminator1000 | Step 10 | expected 100 (HP 250000/2500) |
| cabal_eliminator800 | Step 10 | expected 85 (HP 85000/1000) |
| cabal_heavyspider | Step 12 | expected 32 (HP 80000/2500) |
| cabal_platedarmorcyborg | Step 25 | expected 60 (HP 60000/1000) |
| combat_tank.harkonnen | Step 10 | expected 28 (HP 70000/2500) |
| conehead.nax | Step 10 | expected 20 (HP 20000/1000) |
| ctnk | Step 60 | expected 30 (HP 75000/2500) |
| dieglocke.nax2 | Step 50 | expected 1500 (HP 3750000/2500) |
| eden_tiger_acidcloud | Step 10 | expected 24 (HP 60000/2500) |
| forgotten_mutanthijacker | Step 10 | expected 25 (HP 25000/1000) |
| forgotten_scoopertank | Step 80 | expected 100 (HP 250000/2500) |
| gravity.nax2 | Step 10 | expected 120 (HP 300000/2500) |
| harv2.futu | Step 60 | expected 48 (HP 120000/2500) |
| imperial.nax | Step 10 | expected 33 (HP 82500/2500) |
| katy.steel | Step 220 | expected 110 (HP 275000/2500) |
| landcarr.futu | Step 52 | expected 50 (HP 125000/2500) |
| me262.nax | Step 75 | expected 30 (HP 75000/2500) |
| mech | Step 10 | expected 8 (HP 7500/1000) |
| modcore | Step 120 | expected 20 (HP 50000/2500) |
| modcore1 | Step 120 | expected 20 (HP 50000/2500) |
| modcore2 | Step 120 | expected 20 (HP 50000/2500) |
| modcore3 | Step 120 | expected 20 (HP 50000/2500) |
| modcore4 | Step 120 | expected 20 (HP 50000/2500) |
| modcore5 | Step 120 | expected 20 (HP 50000/2500) |
| modcore6 | Step 120 | expected 20 (HP 50000/2500) |
| modcore7 | Step 120 | expected 20 (HP 50000/2500) |
| modhovert | Step 96 | expected 48 (HP 120000/2500) |
| modkami | Step 30 | expected 12 (HP 30000/2500) |
| nokana.nax | Step 10 | expected 180 (HP 450000/2500) |
| nuketruk.latin | Step 10 | expected 24 (HP 60000/2500) |
| phal.futu | Step 18 | expected 30 (HP 75000/2500) |
| piercer.nax2 | Step 7 | expected 15 (HP 37500/2500) |
| plymouth_tiger_emp | Step 10 | expected 24 (HP 60000/2500) |
| plymouth_tiger_esg | Step 10 | expected 24 (HP 60000/2500) |
| plymouth_tiger_microwave | Step 10 | expected 24 (HP 60000/2500) |
| plymouth_tiger_rpg | Step 10 | expected 24 (HP 60000/2500) |
| plymouth_tiger_starflare | Step 10 | expected 36 (HP 90000/2500) |
| plymouth_tiger_stickyfoam | Step 10 | expected 24 (HP 60000/2500) |
| plymouth_tiger_supernova | Step 10 | expected 48 (HP 120000/2500) |
| ra2adog | Step 2 | expected 5 (HP 5000/1000) |
| ra2dog | Step 2 | expected 5 (HP 5000/1000) |
| ra2falc | Step 72 | expected 29 (HP 72000/2500) |
| ra2spy | Step 10 | expected 5 (HP 5000/1000) |
| raspy | Step 10 | expected 5 (HP 5000/1000) |
| sarubia.nax | Step 10 | expected 25 (HP 62500/2500) |
| scgorekraken | Step 150 | expected 140 (HP 350000/2500) |
| shoe.nax | Step 80 | expected 60 (HP 150000/2500) |
| sovstalinfist | Step 120 | expected 40 (HP 100000/2500) |
| spy.futu | Step 10 | expected 5 (HP 5000/1000) |
| su57 | Step 52 | expected 26 (HP 65000/2500) |
| swarmer.ordos | Step 20 | expected 8 (HP 20000/2500) |
| t30 | Step 10 | expected 160 (HP 400000/2500) |
| t72 | Step 10 | expected 40 (HP 100000/2500) |
| tkmbigshiee | Step 48 | expected 200 (HP 500000/2500) |
| tkmdronepodtruck | Step 11 | expected 24 (HP 60000/2500) |
| tkmjug | Step 16 | expected 36 (HP 36000/1000) |
| tkmmedictruck | Step 11 | expected 20 (HP 50000/2500) |
| tkmquadtruck | Step 11 | expected 26 (HP 65000/2500) |
| tkmradartruck | Step 11 | expected 30 (HP 75000/2500) |
| tkmratflak | Step 10 | expected 48 (HP 120000/2500) |
| tkmrepairtruck | Step 11 | expected 20 (HP 50000/2500) |
| tkmsandmarine | Step 48 | expected 320 (HP 800000/2500) |
| tkmstryker | Step 80 | expected 32 (HP 80000/2500) |
| tkmtechnicaltank | Step 35 | expected 28 (HP 70000/2500) |
| tkmtrenchtank | Step 10 | expected 80 (HP 200000/2500) |
| tkmtrenchtruck | Step 10 | expected 40 (HP 100000/2500) |
| tkmzaza | Step 11 | expected 50 (HP 125000/2500) |
| ts_gdi_zonetrooper | Step 10 | expected 80 (HP 80000/1000) |
| ts_nod_chameleonspy | Step 10 | expected 30 (HP 30000/1000) |
| ts_nod_subterraneanapc | Step 10 | expected 7 (HP 17500/2500) |
| twister.futu | Step 50 | expected 20 (HP 50000/2500) |
| twister.steel | Step 50 | expected 20 (HP 50000/2500) |
| typechiha | Step 104 | expected 52 (HP 130000/2500) |
| wc2_human_militia2 | Step 8 | expected 20 (HP 20000/1000) |
| yak | Step 32 | expected 13 (HP 32000/2500) |
| yakarmored | Step 80 | expected 32 (HP 80000/2500) |
| yaknuclear | Step 64 | expected 26 (HP 64000/2500) |
| yakolev.latin | Step 40 | expected 16 (HP 40000/2500) |
| yaktesla | Step 64 | expected 26 (HP 64000/2500) |
| zep.nax | Step 21 | expected 500 (HP 1250000/2500) |
| zep.nax2 | Step 21 | expected 540 (HP 1350000/2500) |
| zombietank.nax | Step 63 | expected 44 (HP 110000/2500) |


## F3 — infantry with Repairable  (4)

| actor | actual | expected |
|---|---|---|
| armor_mg.nax2 | infantry declares Repairable locally |  |
| mortarbike.latin | infantry declares Repairable locally |  |
| plymouth_spider | infantry declares Repairable locally |  |
| wc2_human_militia2 | infantry declares Repairable locally |  |


_232 further infantry inherit Repairable from the infantry base template (^DefaultInfantry RepairActors: drfghosp… — unloaded Dark Reign hospitals). One template-line fix covers them all._


## F4 — upgrade shield RegenAmount ≠ 2×SelfHealing Step  (64)

| actor | actual | expected |
|---|---|---|
| armor_harv.nax2 | RegenAmount 10 | expected 60 (2 x SelfHealing 30) |
| autogun_tank.ordos | RegenAmount 96 | expected 128 (2 x SelfHealing 64) |
| autogun_tank_small.ordos | RegenAmount 48 | expected 76 (2 x SelfHealing 38) |
| bomber.ixian | RegenAmount 76 | expected 88 (2 x SelfHealing 44) |
| cabal_engineer | RegenAmount 25 | expected 40 (2 x SelfHealing 20) |
| cabal_tiberiumharvester | RegenAmount 10 | expected 120 (2 x SelfHealing 60) |
| chem_troop.ordos | RegenAmount 10 | expected 60 (2 x SelfHealing 30) |
| cheme3 | RegenAmount 25 | expected 36 (2 x SelfHealing 18) |
| contaminator.ordos | RegenAmount 10 | expected 150 (2 x SelfHealing 75) |
| drmn.asian | RegenAmount 10 | expected 20 (2 x SelfHealing 10) |
| duelist_tank.ixian | RegenAmount 158 | expected 192 (2 x SelfHealing 96) |
| e1.nod | RegenAmount 25 | expected 32 (2 x SelfHealing 16) |
| e3.nod | RegenAmount 25 | expected 18 (2 x SelfHealing 9) |
| e4 | RegenAmount 25 | expected 40 (2 x SelfHealing 20) |
| e5 | RegenAmount 25 | expected 96 (2 x SelfHealing 48) |
| eden_cargotruck_empty | RegenAmount 10 | expected 88 (2 x SelfHealing 44) |
| face_dancer.ordos | RegenAmount 10 | expected 180 (2 x SelfHealing 90) |
| forgotten_engineer | RegenAmount 25 | expected 20 (2 x SelfHealing 10) |
| forgotten_tiberiumharvester | RegenAmount 10 | expected 120 (2 x SelfHealing 60) |
| harv.futu | RegenAmount 10 | expected 80 (2 x SelfHealing 40) |
| harv.gdi | RegenAmount 10 | expected 120 (2 x SelfHealing 60) |
| harv.latin | RegenAmount 10 | expected 68 (2 x SelfHealing 34) |
| harv.nod | RegenAmount 10 | expected 120 (2 x SelfHealing 60) |
| harv2.futu | RegenAmount 10 | expected 120 (2 x SelfHealing 60) |
| heavy_inf.ixian | RegenAmount 10 | expected 64 (2 x SelfHealing 32) |
| leech.ordos | RegenAmount 10 | expected 40 (2 x SelfHealing 20) |
| light_inf | RegenAmount 10 | expected 36 (2 x SelfHealing 18) |
| nodlasercommando | RegenAmount 25 | expected 114 (2 x SelfHealing 57) |
| plymouth_cargotruck_empty | RegenAmount 10 | expected 96 (2 x SelfHealing 48) |
| qmin.steel | RegenAmount 10 | expected 80 (2 x SelfHealing 40) |
| ra2cmin | RegenAmount 10 | expected 80 (2 x SelfHealing 40) |
| ra2harv | RegenAmount 10 | expected 100 (2 x SelfHealing 50) |
| ra_industrialminer | RegenAmount 10 | expected 108 (2 x SelfHealing 54) |
| raharv.allies | RegenAmount 10 | expected 80 (2 x SelfHealing 40) |
| raharv.japan | RegenAmount 10 | expected 60 (2 x SelfHealing 30) |
| raharv.soviet | RegenAmount 10 | expected 80 (2 x SelfHealing 40) |
| rmbo.nod | RegenAmount 25 | expected 160 (2 x SelfHealing 80) |
| scdrone | RegenAmount 10 | expected 36 (2 x SelfHealing 18) |
| scscv | RegenAmount 10 | expected 36 (2 x SelfHealing 18) |
| shock_infantry.ixian | RegenAmount 10 | expected 72 (2 x SelfHealing 36) |
| slav.nax | RegenAmount 10 | expected 20 (2 x SelfHealing 10) |
| storm_infantry.ixian | RegenAmount 10 | expected 88 (2 x SelfHealing 44) |
| storm_lasher.ixian | RegenAmount 160 | expected 20 (2 x SelfHealing 10) |
| tanodharv | RegenAmount 10 | expected 100 (2 x SelfHealing 50) |
| tkmharv | RegenAmount 10 | expected 80 (2 x SelfHealing 40) |
| trooper | RegenAmount 10 | expected 24 (2 x SelfHealing 12) |
| ts_gdi_engineer | RegenAmount 25 | expected 20 (2 x SelfHealing 10) |
| ts_gdi_tiberiumharvester | RegenAmount 10 | expected 120 (2 x SelfHealing 60) |
| ts_nod_engineer | RegenAmount 25 | expected 20 (2 x SelfHealing 10) |
| ts_nod_tiberiumharvester | RegenAmount 10 | expected 120 (2 x SelfHealing 60) |
| tsblackhandflamer | RegenAmount 25 | expected 72 (2 x SelfHealing 36) |
| tsblackhandlaser | RegenAmount 25 | expected 120 (2 x SelfHealing 60) |
| tsstealthsoldier | RegenAmount 25 | expected 50 (2 x SelfHealing 25) |
| twin_rocket_trooper.ixian | RegenAmount 10 | expected 48 (2 x SelfHealing 24) |
| wc2_human_militia2 | RegenAmount 10 | expected 16 (2 x SelfHealing 8) |
| wc2_human_peasant | RegenAmount 10 | expected 32 (2 x SelfHealing 16) |
| wc2_orc_peon | RegenAmount 10 | expected 32 (2 x SelfHealing 16) |
| yrltnk | RegenAmount 10 | expected 56 (2 x SelfHealing 28) |
| yrmind | RegenAmount 10 | expected 80 (2 x SelfHealing 40) |
| yrpcv | RegenAmount 10 | expected 240 (2 x SelfHealing 120) |
| yrsmin | RegenAmount 10 | expected 160 (2 x SelfHealing 80) |
| yrtele | RegenAmount 10 | expected 52 (2 x SelfHealing 26) |
| yryarefn | RegenAmount 10 | expected 160 (2 x SelfHealing 80) |
| yrytnk | RegenAmount 10 | expected 36 (2 x SelfHealing 18) |


## F5 — defense RevealsShroud.Range ≠ weapon range  (42)

| actor | actual | expected |
|---|---|---|
| bfg10k.steel | RevealsShroud 25000 | weapon range 10238976 |
| bunk.nax | RevealsShroud 6666 | weapon range 12345 |
| cabal_heavycabalobelisk | RevealsShroud 7168 | weapon range 10435 |
| cabal_missilesilo | RevealsShroud 5120 | weapon range 10238976 |
| cabal_obeliskofdarkness | RevealsShroud 7168 | weapon range 12288 |
| eden_gp_emp | RevealsShroud 6144 | weapon range 5500 |
| eden_gp_laser | RevealsShroud 6144 | weapon range 6656 |
| eden_gp_railgun | RevealsShroud 6144 | weapon range 7168 |
| fedaa.steel | RevealsShroud 12000 | weapon range 15000 |
| fedturret.steel | RevealsShroud 6666 | weapon range 15000 |
| flak88.nax | RevealsShroud 6666 | weapon range 13200 |
| forgotten_brokenrattytankturret | RevealsShroud 7168 | weapon range 6574 |
| forgotten_brokenscoopertankturret | RevealsShroud 7168 | weapon range 6404 |
| forgotten_brokenwarriortankturret | RevealsShroud 7168 | weapon range 9483 |
| forgotten_machineguntower | RevealsShroud 7168 | weapon range 6272 |
| ngdshktur.latin | RevealsShroud 6666 | weapon range 7777 |
| plymouth_gp_microwave | RevealsShroud 6144 | weapon range 6656 |
| plymouth_gp_rpg | RevealsShroud 6144 | weapon range 7168 |
| plymouth_gp_stickyfoam | RevealsShroud 6144 | weapon range 6656 |
| qcannon.steel | RevealsShroud 8888 | weapon range 15000 |
| ra2tesla | RevealsShroud 10000 | weapon range 8842 |
| ragun | RevealsShroud 8683 | weapon range 7685 |
| sam | RevealsShroud 12588 | weapon range 12193 |
| sccreepcolonydefense | RevealsShroud 5000 | weapon range 10160 |
| scsporecolony | RevealsShroud 5000 | weapon range 10160 |
| scsunkencolony | RevealsShroud 5000 | weapon range 10160 |
| sturmcann.nax2 | RevealsShroud 6666 | weapon range 14000 |
| tkmbunkerquadturret | RevealsShroud 6720 | weapon range 11604 |
| ts_gdi_empulsecannon | RevealsShroud 7168 | weapon range 10205 |
| ts_gdi_rpgtower | RevealsShroud 7168 | weapon range 8544 |
| ts_gdi_vulcantower | RevealsShroud 7168 | weapon range 6809 |
| ts_nod_laserturret | RevealsShroud 7168 | weapon range 6992 |
| ts_nod_missilesilo | RevealsShroud 5120 | weapon range 10238976 |
| ts_nod_obeliskoflight | RevealsShroud 7168 | weapon range 10435 |
| twr.nax | RevealsShroud 6666 | weapon range 8100 |
| wc2_human_cannon_tower | RevealsShroud 5000 | weapon range 10500 |
| wc2_human_guard_tower | RevealsShroud 5000 | weapon range 10500 |
| wc2_human_scout_tower | RevealsShroud 5000 | weapon range 10500 |
| wc2_orc_cannon_tower | RevealsShroud 5000 | weapon range 10500 |
| wc2_orc_guard_tower | RevealsShroud 5000 | weapon range 10500 |
| wc2_orc_watch_tower | RevealsShroud 5000 | weapon range 10500 |
| yrygpsyt | RevealsShroud 10000 | weapon range 8000 |


## F6 — AA/advanced defense DetectCloaked.Range ≠ weapon range/2  (19)

| actor | actual | expected |
|---|---|---|
| bfg10k.steel | DetectCloaked 12500 | expected 5119488 (range/2) |
| cabal_heavycabalobelisk | DetectCloaked 5120 | expected 5217 (range/2) |
| cabal_obeliskofdarkness | DetectCloaked 7168 | expected 6144 (range/2) |
| fedaa.steel | DetectCloaked 6000 | expected 7500 (range/2) |
| forgotten_brokenscoopertankturret | DetectCloaked 3072 | expected 3202 (range/2) |
| forgotten_brokenwarriortankturret | DetectCloaked 3072 | expected 4741 (range/2) |
| forgotten_juggerflakwall | DetectCloaked 4096 | expected 5617 (range/2) |
| qcannon.steel | DetectCloaked 4444 | expected 7500 (range/2) |
| ra2tesla | DetectCloaked 5000 | expected 4421 (range/2) |
| sam | DetectCloaked 6294 | expected 6096 (range/2) |
| scphotoncannon | DetectCloaked 4224 | expected 4114 (range/2) |
| sml.latin | DetectCloaked 7000 | expected 7500 (range/2) |
| tkmbunkerquadturret | DetectCloaked 6000 | expected 5802 (range/2) |
| ts_gdi_empulsecannon | DetectCloaked missing | expected 5102 |
| ts_gdi_rpgtower | DetectCloaked 3072 | expected 4272 (range/2) |
| ts_gdi_samtower | DetectCloaked 4096 | expected 6220 (range/2) |
| ts_nod_obeliskoflight | DetectCloaked 5120 | expected 5217 (range/2) |
| ts_nod_samsite | DetectCloaked 4096 | expected 6588 (range/2) |
| yrygpsyt | DetectCloaked 5000 | expected 4000 (range/2) |


## F7 — defense Power.Amount ≠ -Cost/20  (96)

| actor | actual | expected |
|---|---|---|
| awall.asian | Power missing | expected -10 |
| bfg10k.steel | Power -1000 | expected -500 (-Cost/20) |
| brik | Power missing | expected -10 |
| bunk.nax | Power missing | expected -50 |
| ca12hit.latin | Power -200 | expected -500 (-Cost/20) |
| cabal_heavycabalobelisk | Power -100 | expected -120 (-Cost/20) |
| cabal_missilesilo | Power -150 | expected -500 (-Cost/20) |
| cabal_obeliskofdarkness | Power -75 | expected -60 (-Cost/20) |
| cabal_pillbox | Power -25 | expected -40 (-Cost/20) |
| cabal_silo | Power -10 | expected -7 (-Cost/20) |
| cabal_stealthgenerator | Power -150 | expected -125 (-Cost/20) |
| cgchao.asian | Power -200 | expected -250 (-Cost/20) |
| cgflam.asian | Power -25 | expected -40 (-Cost/20) |
| cgionc.asian | Power -200 | expected -500 (-Cost/20) |
| cycl | Power missing | expected -3 |
| d2k_munitions.ixian | Power -10 | expected -25 (-Cost/20) |
| d2k_silo.ixian | Power -10 | expected -7 (-Cost/20) |
| d2k_silo.ordos | Power -10 | expected -7 (-Cost/20) |
| eden_gp_emp | Power -10 | expected -30 (-Cost/20) |
| eden_gp_laser | Power -10 | expected -30 (-Cost/20) |
| eden_gp_railgun | Power -10 | expected -30 (-Cost/20) |
| eden_light_tower | Power -5 | expected -2 (-Cost/20) |
| eden_mine_common | Power -50 | expected -40 (-Cost/20) |
| eden_storage_common | Power -10 | expected -5 (-Cost/20) |
| fedaa.steel | Power -45 | expected -50 (-Cost/20) |
| fenc | Power missing | expected -1 |
| flak88.nax | Power -40 | expected -60 (-Cost/20) |
| forgotten_brokenrattytankturret | Power 0 | expected -40 (-Cost/20) |
| forgotten_brokenscoopertankturret | Power 0 | expected -87 (-Cost/20) |
| forgotten_brokenwarriortankturret | Power 0 | expected -75 (-Cost/20) |
| forgotten_juggerflakwall | Power -40 | expected -50 (-Cost/20) |
| forgotten_machineguntower | Power -20 | expected -30 (-Cost/20) |
| forgotten_silo | Power -10 | expected -15 (-Cost/20) |
| forgotten_veinhole | Power -150 | expected -500 (-Cost/20) |
| iron | Power -200 | expected -250 (-Cost/20) |
| jshrine | Power -200 | expected -500 (-Cost/20) |
| meteorray.nax2 | Power -200 | expected -500 (-Cost/20) |
| mslo | Power -200 | expected -500 (-Cost/20) |
| ngbunk2.latin | Power missing | expected -30 |
| ngdshktur.latin | Power -30 | expected -35 (-Cost/20) |
| pdox | Power -200 | expected -500 (-Cost/20) |
| plymouth_gp_microwave | Power -10 | expected -30 (-Cost/20) |
| plymouth_gp_rpg | Power -10 | expected -30 (-Cost/20) |
| plymouth_gp_stickyfoam | Power -10 | expected -30 (-Cost/20) |
| plymouth_light_tower | Power -5 | expected -2 (-Cost/20) |
| plymouth_mine_common | Power -50 | expected -40 (-Cost/20) |
| plymouth_storage_common | Power -10 | expected -5 (-Cost/20) |
| qaorbit.steel | Power -200 | expected -500 (-Cost/20) |
| ra2_awall | Power missing | expected -10 |
| ra2_swall | Power missing | expected -10 |
| ra2_ywall | Power missing | expected -10 |
| ra2brik | Power missing | expected -10 |
| ra2gacsph | Power -200 | expected -250 (-Cost/20) |
| ra2gaweat | Power -200 | expected -500 (-Cost/20) |
| ra2gtgcan | Power -200 | expected -250 (-Cost/20) |
| ra2nairon | Power -200 | expected -250 (-Cost/20) |
| ra2namisl | Power -200 | expected -500 (-Cost/20) |
| rasilo | Power -10 | expected -7 (-Cost/20) |
| rocket.nax | Power -200 | expected -500 (-Cost/20) |
| sbag | Power missing | expected -2 |
| scbunker | Power 0 | expected -60 (-Cost/20) |
| sccreepcolonydefense | Power missing | expected -50 |
| scsporecolony | Power missing | expected -62 |
| scsunkencolony | Power missing | expected -62 |
| scterranmslo | Power 0 | expected -500 (-Cost/20) |
| silo | Power -10 | expected -5 (-Cost/20) |
| sturmcann.nax2 | Power -50 | expected -60 (-Cost/20) |
| supercomputer.ixian | Power -200 | expected -500 (-Cost/20) |
| tkmbunker | Power missing | expected -30 |
| tkmbunkerquadturret | Power -25 | expected -45 (-Cost/20) |
| tkmbunkertankturret | Power -25 | expected -40 (-Cost/20) |
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
| twr.nax | Power -25 | expected -32 (-Cost/20) |
| wall | Power missing | expected -6 |
| wc2_human_cannon_tower | Power missing | expected -75 |
| wc2_human_guard_tower | Power missing | expected -75 |
| wc2_human_scout_tower | Power missing | expected -60 |
| wc2_human_wall | Power missing | expected -15 |
| wc2_orc_cannon_tower | Power missing | expected -80 |
| wc2_orc_guard_tower | Power missing | expected -80 |
| wc2_orc_wall | Power missing | expected -15 |
| wc2_orc_watch_tower | Power missing | expected -60 |
| yrngbnkr | Power missing | expected -40 |
| yrngtbnk | Power missing | expected -50 |
| yryggntc | Power -200 | expected -250 (-Cost/20) |
| yrygppet | Power -200 | expected -500 (-Cost/20) |


## F8 — vehicle TurnSpeed ≠ Speed/5  (34)

| actor | actual | expected |
|---|---|---|
| autogun_tank.ordos | TurnSpeed 30 (Speed 75) | expected 15 = Speed/5 |
| bike | TurnSpeed 80 (Speed 200) | expected 40 = Speed/5 |
| bmwbike.nax | TurnSpeed 16 (Speed 125) | expected 25 = Speed/5 |
| cabal_scarabapc | TurnSpeed 40 (Speed 75) | expected 15 = Speed/5 |
| chembike | TurnSpeed 70 (Speed 175) | expected 35 = Speed/5 |
| cobra.ordos | TurnSpeed 18 (Speed 45) | expected 9 = Speed/5 |
| coiler.futu | TurnSpeed 20 (Speed 50) | expected 10 = Speed/5 |
| eden_lynx_acidcloud | TurnSpeed 10 (Speed 90) | expected 18 = Speed/5 |
| landcarr.futu | TurnSpeed 12 (Speed 45) | expected 9 = Speed/5 |
| mnly | TurnSpeed 40 (Speed 128) | expected 26 = Speed/5 |
| modnano | TurnSpeed 17 (Speed 77) | expected 15 = Speed/5 |
| neocymek.ixian | TurnSpeed 18 (Speed 45) | expected 9 = Speed/5 |
| plymouth_lynx_emp | TurnSpeed 10 (Speed 90) | expected 18 = Speed/5 |
| plymouth_lynx_esg | TurnSpeed 10 (Speed 90) | expected 18 = Speed/5 |
| plymouth_lynx_microwave | TurnSpeed 10 (Speed 90) | expected 18 = Speed/5 |
| plymouth_lynx_rpg | TurnSpeed 10 (Speed 90) | expected 18 = Speed/5 |
| plymouth_lynx_starflare | TurnSpeed 10 (Speed 90) | expected 18 = Speed/5 |
| plymouth_lynx_stickyfoam | TurnSpeed 10 (Speed 90) | expected 18 = Speed/5 |
| plymouth_lynx_supernova | TurnSpeed 10 (Speed 90) | expected 18 = Speed/5 |
| python.ordos | TurnSpeed 16 (Speed 40) | expected 8 = Speed/5 |
| qmcv.steel | TurnSpeed 15 (Speed 60) | expected 12 = Speed/5 |
| ra2dron | TurnSpeed 200 (Speed 200) | expected 40 = Speed/5 |
| ra_heatraytank | TurnSpeed 24 (Speed 60) | expected 12 = Speed/5 |
| shock_raider.ixian | TurnSpeed 48 (Speed 120) | expected 24 = Speed/5 |
| shoe.nax | TurnSpeed 30 (Speed 75) | expected 15 = Speed/5 |
| sovgorynych | TurnSpeed 12 (Speed 70) | expected 14 = Speed/5 |
| ts_gdi_hovermlrs | TurnSpeed 40 (Speed 80) | expected 16 = Speed/5 |
| ts_gdi_juggernaut | TurnSpeed 20 (Speed 71) | expected 14 = Speed/5 |
| ts_gdi_mobileemp | TurnSpeed 40 (Speed 100) | expected 20 = Speed/5 |
| ts_gdi_mobilesensorarray | TurnSpeed 40 (Speed 85) | expected 17 = Speed/5 |
| ts_nod_artillery | TurnSpeed 24 (Speed 60) | expected 12 = Speed/5 |
| ts_nod_mobilestealthgenerator | TurnSpeed 40 (Speed 56) | expected 11 = Speed/5 |
| tsttnk | TurnSpeed 32 (Speed 90) | expected 18 = Speed/5 |
| ttnk | TurnSpeed 32 (Speed 80) | expected 16 = Speed/5 |


## F9 — Turreted.TurnSpeed ≠ Mobile.TurnSpeed  (51)

| actor | actual | expected |
|---|---|---|
| aatank.nax2 | Turreted 24 vs Mobile 12 | must match |
| apc | Turreted 40 vs Mobile 20 | must match |
| apc.latin | Turreted 36 vs Mobile 18 | must match |
| apc.ordos | Turreted 42 vs Mobile 21 | must match |
| autogun_tank.ordos | Turreted 12 vs Mobile 30 | must match |
| beetle.nax2 | Turreted 34 vs Mobile 17 | must match |
| cabal_scarabapc | Turreted 20 vs Mobile 40 | must match |
| chemstnk | Turreted 25 vs Mobile 24 | must match |
| diablo.latin | Turreted 36 vs Mobile 25 | must match |
| eden_tiger_thorshammer | Turreted 18 vs Mobile 16 | must match |
| ftrk | Turreted 48 vs Mobile 24 | must match |
| gdiassaultapc | Turreted 25 vs Mobile 20 | must match |
| gdihumvee | Turreted 46 vs Mobile 23 | must match |
| heavyaatank | Turreted 30 vs Mobile 15 | must match |
| lasert.nax2 | Turreted 28 vs Mobile 14 | must match |
| manta_hunt.steel | Turreted 12 vs Mobile 16 | must match |
| modbtr | Turreted 36 vs Mobile 18 | must match |
| modcarr | Turreted 52 vs Mobile 26 | must match |
| modgtnk | Turreted 30 vs Mobile 15 | must match |
| mtnk.latin | Turreted 21 vs Mobile 19 | must match |
| nodbuggy2 | Turreted 48 vs Mobile 24 | must match |
| panzer.nax2 | Turreted 20 vs Mobile 18 | must match |
| phal.futu | Turreted 12 vs Mobile 19 | must match |
| pulv.asian | Turreted 26 vs Mobile 13 | must match |
| ra2fv | Turreted 60 vs Mobile 30 | must match |
| ra2fvbotchrono | Turreted 60 vs Mobile 30 | must match |
| ra2fvbothmg | Turreted 60 vs Mobile 30 | must match |
| ra2fvbotmg | Turreted 60 vs Mobile 30 | must match |
| ra2fvbotmiss | Turreted 60 vs Mobile 30 | must match |
| ra2fvbotrep | Turreted 60 vs Mobile 30 | must match |
| ra2htk | Turreted 38 vs Mobile 19 | must match |
| raapc | Turreted 42 vs Mobile 21 | must match |
| sccyclone | Turreted 46 vs Mobile 23 | must match |
| scgoliath | Turreted 36 vs Mobile 18 | must match |
| scgoliath2 | Turreted 30 vs Mobile 15 | must match |
| scmatador | Turreted 15 vs Mobile 20 | must match |
| shoe.nax | Turreted 15 vs Mobile 30 | must match |
| sovgorynych | Turreted 20 vs Mobile 12 | must match |
| t72 | Turreted 20 vs Mobile 16 | must match |
| td_gdi_boxer | Turreted 32 vs Mobile 16 | must match |
| tiger.nax2 | Turreted 20 vs Mobile 16 | must match |
| tkmmedictruck | Turreted 20 vs Mobile 15 | must match |
| tkmtrenchtruck | Turreted 15 vs Mobile 12 | must match |
| tkmzaza | Turreted 30 vs Mobile 15 | must match |
| ts_gdi_hovermlrs | Turreted 16 vs Mobile 40 | must match |
| v1truck | Turreted 32 vs Mobile 20 | must match |
| wirbelwind.nax | Turreted 34 vs Mobile 17 | must match |
| yrcaos | Turreted 2000 vs Mobile 28 | must match |
| yrltnk | Turreted 23 vs Mobile 21 | must match |
| yrmind | Turreted 2000 vs Mobile 24 | must match |
| yrytnk | Turreted 36 vs Mobile 18 | must match |


## F10 — turretless TurnSpeed ≠ 2×Speed/5 (artillery: Speed/5)  (54)

| actor | actual | expected |
|---|---|---|
| arty | TurnSpeed 11 (Speed 55) | expected 22 = 2 x Speed/5 (turretless) |
| arty.steel | TurnSpeed 20 (Speed 100) | expected 40 = 2 x Speed/5 (turretless) |
| athena.futu | TurnSpeed 12 (Speed 60) | expected 24 = 2 x Speed/5 (turretless) |
| brummbar.nax | TurnSpeed 12 (Speed 60) | expected 24 = 2 x Speed/5 (turretless) |
| burrito.latin | TurnSpeed 16 (Speed 80) | expected 32 = 2 x Speed/5 (turretless) |
| cabal_artilleryspider | TurnSpeed 14 (Speed 70) | expected 28 = 2 x Speed/5 (turretless) |
| cabal_cyborgreaper | TurnSpeed 13 (Speed 65) | expected 26 = 2 x Speed/5 (turretless) |
| cabal_heavyspider | TurnSpeed 64 (Speed 80) | expected 32 = 2 x Speed/5 (turretless) |
| cabal_laserspider | TurnSpeed 14 (Speed 70) | expected 28 = 2 x Speed/5 (turretless) |
| cabal_mantis | TurnSpeed 24 (Speed 120) | expected 48 = 2 x Speed/5 (turretless) |
| cabal_spidertankdrone | TurnSpeed 24 (Speed 120) | expected 48 = 2 x Speed/5 (turretless) |
| chemssm | TurnSpeed 15 (Speed 75) | expected 30 = 2 x Speed/5 (turretless) |
| dagger.steel | TurnSpeed 20 (Speed 100) | expected 40 = 2 x Speed/5 (turretless) |
| forgotten_thumperbus | TurnSpeed 18 (Speed 90) | expected 36 = 2 x Speed/5 (turretless) |
| grille.nax | TurnSpeed 16 (Speed 80) | expected 32 = 2 x Speed/5 (turretless) |
| jballista | TurnSpeed 13 (Speed 65) | expected 26 = 2 x Speed/5 (turretless) |
| mrj | TurnSpeed 20 (Speed 100) | expected 40 = 2 x Speed/5 (turretless) |
| nodspecter | TurnSpeed 20 (Speed 100) | expected 40 = 2 x Speed/5 (turretless) |
| nokana.nax | TurnSpeed 12 (Speed 60) | expected 24 = 2 x Speed/5 (turretless) |
| potnk.steel | TurnSpeed 10 (Speed 50) | expected 20 = 2 x Speed/5 (turretless) |
| qtnk | TurnSpeed 12 (Speed 60) | expected 24 = 2 x Speed/5 (turretless) |
| ra2v3 | TurnSpeed 15 (Speed 75) | expected 30 = 2 x Speed/5 (turretless) |
| raarty | TurnSpeed 12 (Speed 60) | expected 24 = 2 x Speed/5 (turretless) |
| railt2.asian | TurnSpeed 10 (Speed 50) | expected 20 = 2 x Speed/5 (turretless) |
| ramgg | TurnSpeed 15 (Speed 75) | expected 30 = 2 x Speed/5 (turretless) |
| savi.steel | TurnSpeed 15 (Speed 75) | expected 30 = 2 x Speed/5 (turretless) |
| scdragoon | TurnSpeed 1023 (Speed 80) | expected 32 = 2 x Speed/5 (turretless) |
| scdrone | TurnSpeed 100 (Speed 100) | expected 40 = 2 x Speed/5 (turretless) |
| scprobe | TurnSpeed 100 (Speed 100) | expected 40 = 2 x Speed/5 (turretless) |
| scscv | TurnSpeed 100 (Speed 100) | expected 40 = 2 x Speed/5 (turretless) |
| siege_tank.ixian | TurnSpeed 12 (Speed 60) | expected 24 = 2 x Speed/5 (turretless) |
| sturmtiger.nax | TurnSpeed 6 (Speed 30) | expected 12 = 2 x Speed/5 (turretless) |
| tkmbattlebus | TurnSpeed 20 (Speed 100) | expected 40 = 2 x Speed/5 (turretless) |
| tkmkatyushalauncher | TurnSpeed 16 (Speed 80) | expected 32 = 2 x Speed/5 (turretless) |
| tkmradartruck | TurnSpeed 15 (Speed 75) | expected 30 = 2 x Speed/5 (turretless) |
| tkmrepairtruck | TurnSpeed 15 (Speed 75) | expected 30 = 2 x Speed/5 (turretless) |
| topol.latin | TurnSpeed 15 (Speed 75) | expected 30 = 2 x Speed/5 (turretless) |
| ts_gdi_juggernautmkii | TurnSpeed 14 (Speed 70) | expected 28 = 2 x Speed/5 (turretless) |
| ts_nod_subterraneanapc | TurnSpeed 40 (Speed 60) | expected 24 = 2 x Speed/5 (turretless) |
| v2rl | TurnSpeed 17 (Speed 85) | expected 34 = 2 x Speed/5 (turretless) |
| v2rlnuke | TurnSpeed 16 (Speed 80) | expected 32 = 2 x Speed/5 (turretless) |
| viper.asian | TurnSpeed 25 (Speed 125) | expected 50 = 2 x Speed/5 (turretless) |
| wc2_human_knight | TurnSpeed 92 (Speed 115) | expected 46 = 2 x Speed/5 (turretless) |
| wc2_human_knight2 | TurnSpeed 96 (Speed 120) | expected 48 = 2 x Speed/5 (turretless) |
| wc2_human_mcv | TurnSpeed 15 (Speed 75) | expected 30 = 2 x Speed/5 (turretless) |
| wc2_human_paladin | TurnSpeed 92 (Speed 115) | expected 46 = 2 x Speed/5 (turretless) |
| wc2_orc_mcv | TurnSpeed 15 (Speed 75) | expected 30 = 2 x Speed/5 (turretless) |
| wc2_orc_ogre | TurnSpeed 68 (Speed 85) | expected 34 = 2 x Speed/5 (turretless) |
| wc2_orc_ogremage | TurnSpeed 68 (Speed 85) | expected 34 = 2 x Speed/5 (turretless) |
| wtrt.asian | TurnSpeed 15 (Speed 75) | expected 30 = 2 x Speed/5 (turretless) |
| yrbfrt | TurnSpeed 12 (Speed 60) | expected 24 = 2 x Speed/5 (turretless) |
| yrbfrt.bot | TurnSpeed 12 (Speed 60) | expected 24 = 2 x Speed/5 (turretless) |
| yrbfrt.bot2 | TurnSpeed 12 (Speed 60) | expected 24 = 2 x Speed/5 (turretless) |
| zombietank.nax | TurnSpeed 12 (Speed 50) | expected 20 = 2 x Speed/5 (turretless) |


## F11 — turreted artillery missing/incorrect firing-slow (Archer pattern)  (36)

| actor | actual | expected |
|---|---|---|
| bradley.nax2 | firing-slow pattern missing | see gdiarcher (GrantConditionOnAttack + 50% multipliers) |
| cobra.ordos | firing-slow pattern missing | see gdiarcher (GrantConditionOnAttack + 50% multipliers) |
| coiler.futu | firing-slow pattern missing | see gdiarcher (GrantConditionOnAttack + 50% multipliers) |
| combat_siege_tank.ixian | firing-slow pattern missing | see gdiarcher (GrantConditionOnAttack + 50% multipliers) |
| crystal.nax2 | firing-slow pattern missing | see gdiarcher (GrantConditionOnAttack + 50% multipliers) |
| eden_lynx_railgun | firing-slow pattern missing | see gdiarcher (GrantConditionOnAttack + 50% multipliers) |
| eden_lynx_thorshammer | firing-slow pattern missing | see gdiarcher (GrantConditionOnAttack + 50% multipliers) |
| eden_tiger_railgun | firing-slow pattern missing | see gdiarcher (GrantConditionOnAttack + 50% multipliers) |
| eden_tiger_thorshammer | firing-slow pattern missing | see gdiarcher (GrantConditionOnAttack + 50% multipliers) |
| forgotten_missilevan | firing-slow pattern missing | see gdiarcher (GrantConditionOnAttack + 50% multipliers) |
| forgotten_mlrs | firing-slow pattern missing | see gdiarcher (GrantConditionOnAttack + 50% multipliers) |
| forgotten_warriortank | firing-slow pattern missing | see gdiarcher (GrantConditionOnAttack + 50% multipliers) |
| grille.nax2 | firing-slow pattern missing | see gdiarcher (GrantConditionOnAttack + 50% multipliers) |
| landcarr.futu | firing-slow pattern missing | see gdiarcher (GrantConditionOnAttack + 50% multipliers) |
| mlrs | RevokeDelay 5 | expected 55 (ReloadDelay 111/2) |
| mlrs.asian | firing-slow pattern missing | see gdiarcher (GrantConditionOnAttack + 50% multipliers) |
| modnano | firing-slow pattern missing | see gdiarcher (GrantConditionOnAttack + 50% multipliers) |
| modwave | firing-slow pattern missing | see gdiarcher (GrantConditionOnAttack + 50% multipliers) |
| mssm | firing-slow pattern missing | see gdiarcher (GrantConditionOnAttack + 50% multipliers) |
| pulv.asian | firing-slow pattern missing | see gdiarcher (GrantConditionOnAttack + 50% multipliers) |
| python.ordos | firing-slow pattern missing | see gdiarcher (GrantConditionOnAttack + 50% multipliers) |
| ra2sref | firing-slow pattern missing | see gdiarcher (GrantConditionOnAttack + 50% multipliers) |
| ra2ttnk | firing-slow pattern missing | see gdiarcher (GrantConditionOnAttack + 50% multipliers) |
| ra_grad | RevokeDelay 5 | expected 57 (ReloadDelay 115/2) |
| ra_heatraytank | firing-slow pattern missing | see gdiarcher (GrantConditionOnAttack + 50% multipliers) |
| sccyclone | firing-slow pattern missing | see gdiarcher (GrantConditionOnAttack + 50% multipliers) |
| sclurker | firing-slow pattern missing | see gdiarcher (GrantConditionOnAttack + 50% multipliers) |
| shock_raider.ixian | firing-slow pattern missing | see gdiarcher (GrantConditionOnAttack + 50% multipliers) |
| storm_raider.ixian | firing-slow pattern missing | see gdiarcher (GrantConditionOnAttack + 50% multipliers) |
| tkmstryker | firing-slow pattern missing | see gdiarcher (GrantConditionOnAttack + 50% multipliers) |
| ts_gdi_hovermlrs | firing-slow pattern missing | see gdiarcher (GrantConditionOnAttack + 50% multipliers) |
| ts_gdi_juggernaut | firing-slow pattern missing | see gdiarcher (GrantConditionOnAttack + 50% multipliers) |
| ts_nod_artillery | firing-slow pattern missing | see gdiarcher (GrantConditionOnAttack + 50% multipliers) |
| ttnk | firing-slow pattern missing | see gdiarcher (GrantConditionOnAttack + 50% multipliers) |
| v1truck | firing-slow pattern missing | see gdiarcher (GrantConditionOnAttack + 50% multipliers) |
| yrtele | firing-slow pattern missing | see gdiarcher (GrantConditionOnAttack + 50% multipliers) |


## F12 — anti-air defense not gated by the faction's radar tier  (0)

_none found_


## F13 — advanced defense not gated by the faction's tech tier  (3)

| actor | actual | expected |
|---|---|---|
| lnaxis: twr.nax2 | prereqs: barr.nax2, conyard.nax2 (gate 2, radar tier 3) | DEFERRED: valid, but faction's only pre-radar defense — add a Tier-1 defense before regating |
| ordos: autogun_turret.ordos | prereqs: construction_yard.ordos, d2k_barracks.ordos (gate 2, radar tier 3) | DEFERRED: valid, but faction's only pre-radar defense — add a Tier-1 defense before regating |
| ordos: artillery_platform.ordos | prereqs: construction_yard.ordos, d2k_barracks.ordos (gate 2, radar tier 3) | DEFERRED: valid, but faction's only pre-radar defense — add a Tier-1 defense before regating |


## F14 — StartingUnits referencing nonexistent actors (crash class)  (0)

_none found_


## F15 — Light Support composition (Tier-1 only, ~2000, 5:1 inf:veh)  (63)

| actor | actual | expected |
|---|---|---|
| gdi: defaultgdia | total cost 1300 | target ~2000 (±15%) |
| gdi: defaultgdia | e3 (cost 200) x2 vs e1 (cost 100) x1 | pricier units must not outnumber cheaper ones |
| nod: defaultnoda | total cost 1300 | target ~2000 (±15%) |
| nod: defaultnoda | bggy | light support must be Tier-1 only (producer-building prereqs only) |
| allies: defaultallies | 5 infantry : 2 vehicles | want ~5 infantry per vehicle |
| soviet: defaultsoviet | total cost 3000 | target ~2000 (±15%) |
| soviet: defaultsoviet | 5 infantry : 2 vehicles | want ~5 infantry per vehicle |
| modjapan: defaultmodjapan | 5 infantry : 2 vehicles | want ~5 infantry per vehicle |
| tsgdi: defaulttsgdi | total cost 2460 | target ~2000 (±15%) |
| tsgdi: defaulttsgdi | 5 infantry : 2 vehicles | want ~5 infantry per vehicle |
| tsnod: defaulttsnod | 5 infantry : 2 vehicles | want ~5 infantry per vehicle |
| forgotten: defaultforgotten | total cost 2890 | target ~2000 (±15%) |
| forgotten: defaultforgotten | 5 infantry : 2 vehicles | want ~5 infantry per vehicle |
| forgotten: defaultforgotten | forgotten_mutantsergeant (cost 750) x2 vs forgotten_mutantsoldier (cost 250) x1 | pricier units must not outnumber cheaper ones |
| forgotten: defaultforgotten | forgotten_mutantsergeant (cost 750) x2 vs forgotten_raidercar (cost 300) x1 | pricier units must not outnumber cheaper ones |
| forgotten: defaultforgotten | forgotten_mutantsergeant (cost 750) x2 vs forgotten_rattytank (cost 600) x1 | pricier units must not outnumber cheaper ones |
| forgotten: defaultforgotten | forgotten_mutantsergeant, forgotten_mutantsoldier | light support must be Tier-1 only (producer-building prereqs only) |
| cabal: defaultcabal | total cost 2850 | target ~2000 (±15%) |
| cabal: defaultcabal | 3 infantry : 2 vehicles | want ~5 infantry per vehicle |
| ra2america: defaultra2allies | total cost 2650 | target ~2000 (±15%) |
| ra2america: defaultra2allies | 5 infantry : 2 vehicles | want ~5 infantry per vehicle |
| ra2russia: defaultra2soviet | total cost 2650 | target ~2000 (±15%) |
| ra2russia: defaultra2soviet | 5 infantry : 2 vehicles | want ~5 infantry per vehicle |
| yuri: defaultyuri | total cost 3350 | target ~2000 (±15%) |
| yuri: defaultyuri | 6 infantry : 2 vehicles | want ~5 infantry per vehicle |
| yuri: defaultyuri | yrbrute (cost 400) x2 vs yrslav (cost 250) x1 | pricier units must not outnumber cheaper ones |
| asianalliance: defaultasianalliance | total cost 2650 | target ~2000 (±15%) |
| asianalliance: defaultasianalliance | 5 infantry : 2 vehicles | want ~5 infantry per vehicle |
| consortium: defaultconsortium | total cost 4750 | target ~2000 (±15%) |
| consortium: defaultconsortium | 5 infantry : 2 vehicles | want ~5 infantry per vehicle |
| consortium: defaultconsortium | qinf.steel (cost 1150) x2 vs manta.steel (cost 850) x1 | pricier units must not outnumber cheaper ones |
| consortium: defaultconsortium | qinf.steel (cost 1150) x2 vs arty.steel (cost 1000) x1 | pricier units must not outnumber cheaper ones |
| consortium: defaultconsortium | qinf.steel | light support must be Tier-1 only (producer-building prereqs only) |
| syndicate: defaultsyndicate | total cost 8990 | target ~2000 (±15%) |
| syndicate: defaultsyndicate | 5 infantry : 2 vehicles | want ~5 infantry per vehicle |
| syndicate: defaultsyndicate | fftr.latin (cost 3000) x2 vs wirbelwind.nax (cost 1800) x1 | pricier units must not outnumber cheaper ones |
| syndicate: defaultsyndicate | fftr.latin (cost 3000) x2 vs tiger.nax (cost 800) x1 | pricier units must not outnumber cheaper ones |
| syndicate: defaultsyndicate | fftr.latin | light support must be Tier-1 only (producer-building prereqs only) |
| naxis: defaultnaxis | total cost 3650 | target ~2000 (±15%) |
| naxis: defaultnaxis | 5 infantry : 2 vehicles | want ~5 infantry per vehicle |
| naxis: defaultnaxis | mp40.nax | light support must be Tier-1 only (producer-building prereqs only) |
| lnaxis: defaultlnaxis | total cost 2710 | target ~2000 (±15%) |
| lnaxis: defaultlnaxis | 5 infantry : 2 vehicles | want ~5 infantry per vehicle |
| futuretech: defaultfuturetech | total cost 2450 | target ~2000 (±15%) |
| futuretech: defaultfuturetech | 0 infantry : 7 vehicles | want ~5 infantry per vehicle |
| futuretech: defaultfuturetech | robot_cannon.futu, robot_missiles.futu, wheel.futu | light support must be Tier-1 only (producer-building prereqs only) |
| tkm: defaulttstkm | 5 infantry : 2 vehicles | want ~5 infantry per vehicle |
| ordos: ordos_L | total cost 3300 | target ~2000 (±15%) |
| ordos: ordos_L | 5 infantry : 2 vehicles | want ~5 infantry per vehicle |
| ixian: ixian_L | total cost 3300 | target ~2000 (±15%) |
| ixian: ixian_L | 5 infantry : 2 vehicles | want ~5 infantry per vehicle |
| terran: defaultterran | total cost 2600 | target ~2000 (±15%) |
| protoss: defaultprotoss | total cost 1500 | target ~2000 (±15%) |
| protoss: defaultprotoss | 1 infantry : 1 vehicles | want ~5 infantry per vehicle |
| protoss: defaultprotoss | sczealot | light support must be Tier-1 only (producer-building prereqs only) |
| plymouthl: defaultplymouthl | total cost 3050 | target ~2000 (±15%) |
| plymouthl: defaultplymouthl | 0 infantry : 6 vehicles | want ~5 infantry per vehicle |
| plymouthl: defaultplymouthl | plymouth_lynx_microwave (cost 500) x3 vs plymouth_scout (cost 350) x1 | pricier units must not outnumber cheaper ones |
| plymouthl: defaultplymouthl | plymouth_lynx_rpg (cost 600) x2 vs plymouth_scout (cost 350) x1 | pricier units must not outnumber cheaper ones |
| edenl: defaultedenl | total cost 4350 | target ~2000 (±15%) |
| edenl: defaultedenl | 0 infantry : 6 vehicles | want ~5 infantry per vehicle |
| edenl: defaultedenl | eden_lynx_laser (cost 750) x3 vs eden_scout (cost 300) x1 | pricier units must not outnumber cheaper ones |
| edenl: defaultedenl | eden_lynx_railgun (cost 900) x2 vs eden_scout (cost 300) x1 | pricier units must not outnumber cheaper ones |


## F16 — Heavy Support composition (all tiers, ~10000, 5:1 inf:veh)  (113)

| actor | actual | expected |
|---|---|---|
| gdi: heavygdia | total cost 3000 | target ~10000 (±15%) |
| gdi: heavygdia | 6 infantry : 3 vehicles | want ~5 infantry per vehicle |
| gdi: heavygdia | mtnk (cost 900) x2 vs jeep (cost 400) x1 | pricier units must not outnumber cheaper ones |
| gdi: heavygdia | all units are Tier 1 | heavy support should mix all tiers |
| gdi: heavygdib | total cost 3600 | target ~10000 (±15%) |
| gdi: heavygdib | all units are Tier 1 | heavy support should mix all tiers |
| nod: heavynoda | total cost 2800 | target ~10000 (±15%) |
| nod: heavynoda | 6 infantry : 3 vehicles | want ~5 infantry per vehicle |
| nod: heavynodb | total cost 2700 | target ~10000 (±15%) |
| allies: heavyallies | total cost 3800 | target ~10000 (±15%) |
| allies: heavyallies | 5 infantry : 5 vehicles | want ~5 infantry per vehicle |
| allies: heavyallies | 2tnk (cost 700) x3 vs rae3 (cost 300) x2 | pricier units must not outnumber cheaper ones |
| allies: heavyallies | 2tnk (cost 700) x3 vs rajeep (cost 300) x1 | pricier units must not outnumber cheaper ones |
| allies: heavyallies | 2tnk (cost 700) x3 vs 1tnk (cost 500) x1 | pricier units must not outnumber cheaper ones |
| allies: heavyallies | all units are Tier 1 | heavy support should mix all tiers |
| soviet: heavysoviet | total cost 5000 | target ~10000 (±15%) |
| soviet: heavysoviet | 5 infantry : 4 vehicles | want ~5 infantry per vehicle |
| soviet: heavysoviet | 3tnk (cost 1000) x2 vs ftrk (cost 800) x1 | pricier units must not outnumber cheaper ones |
| soviet: heavysoviet | all units are Tier 1 | heavy support should mix all tiers |
| modjapan: heavymodjapan | total cost 6100 | target ~10000 (±15%) |
| modjapan: heavymodjapan | 5 infantry : 6 vehicles | want ~5 infantry per vehicle |
| modjapan: heavymodjapan | modtypej (cost 800) x2 vs modkubel (cost 300) x1 | pricier units must not outnumber cheaper ones |
| modjapan: heavymodjapan | modbggy (cost 900) x2 vs modkubel (cost 300) x1 | pricier units must not outnumber cheaper ones |
| modjapan: heavymodjapan | all units are Tier 1 | heavy support should mix all tiers |
| tsgdi: heavytsgdi | total cost 5310 | target ~10000 (±15%) |
| tsgdi: heavytsgdi | 5 infantry : 5 vehicles | want ~5 infantry per vehicle |
| tsgdi: heavytsgdi | ts_gdi_titan (cost 950) x4 vs tse1 (cost 120) x3 | pricier units must not outnumber cheaper ones |
| tsgdi: heavytsgdi | ts_gdi_titan (cost 950) x4 vs ts_gdi_discthrower (cost 300) x2 | pricier units must not outnumber cheaper ones |
| tsgdi: heavytsgdi | ts_gdi_titan (cost 950) x4 vs ts_gdi_wolverine (cost 550) x1 | pricier units must not outnumber cheaper ones |
| tsgdi: heavytsgdi | all units are Tier 1 | heavy support should mix all tiers |
| tsnod: heavytsnod | total cost 4610 | target ~10000 (±15%) |
| tsnod: heavytsnod | 5 infantry : 5 vehicles | want ~5 infantry per vehicle |
| tsnod: heavytsnod | tsttnk (cost 800) x4 vs tse1 (cost 120) x3 | pricier units must not outnumber cheaper ones |
| tsnod: heavytsnod | tsttnk (cost 800) x4 vs ts_nod_rocketinfantry (cost 300) x2 | pricier units must not outnumber cheaper ones |
| tsnod: heavytsnod | tsttnk (cost 800) x4 vs ts_nod_attackbuggy (cost 450) x1 | pricier units must not outnumber cheaper ones |
| tsnod: heavytsnod | all units are Tier 1 | heavy support should mix all tiers |
| forgotten: heavyforgotten | 5 infantry : 5 vehicles | want ~5 infantry per vehicle |
| forgotten: heavyforgotten | forgotten_mutantsergeant (cost 750) x2 vs forgotten_mutantsoldier (cost 250) x1 | pricier units must not outnumber cheaper ones |
| forgotten: heavyforgotten | forgotten_mutantsergeant (cost 750) x2 vs forgotten_raidercar (cost 300) x1 | pricier units must not outnumber cheaper ones |
| forgotten: heavyforgotten | forgotten_mutantsergeant (cost 750) x2 vs forgotten_rattytank (cost 600) x1 | pricier units must not outnumber cheaper ones |
| forgotten: heavyforgotten | forgotten_warriortank (cost 2000) x3 vs forgotten_mutant (cost 120) x2 | pricier units must not outnumber cheaper ones |
| forgotten: heavyforgotten | forgotten_warriortank (cost 2000) x3 vs forgotten_mutantsoldier (cost 250) x1 | pricier units must not outnumber cheaper ones |
| forgotten: heavyforgotten | forgotten_warriortank (cost 2000) x3 vs forgotten_mutantsergeant (cost 750) x2 | pricier units must not outnumber cheaper ones |
| forgotten: heavyforgotten | forgotten_warriortank (cost 2000) x3 vs forgotten_raidercar (cost 300) x1 | pricier units must not outnumber cheaper ones |
| forgotten: heavyforgotten | forgotten_warriortank (cost 2000) x3 vs forgotten_rattytank (cost 600) x1 | pricier units must not outnumber cheaper ones |
| cabal: heavycabal | total cost 5000 | target ~10000 (±15%) |
| cabal: heavycabal | 3 infantry : 5 vehicles | want ~5 infantry per vehicle |
| cabal: heavycabal | tsttnk (cost 800) x3 vs tsbike (cost 550) x2 | pricier units must not outnumber cheaper ones |
| cabal: heavycabal | all units are Tier 1 | heavy support should mix all tiers |
| ra2america: heavyra2allies | total cost 6150 | target ~10000 (±15%) |
| ra2america: heavyra2allies | 5 infantry : 6 vehicles | want ~5 infantry per vehicle |
| ra2america: heavyra2allies | ra2mtnk (cost 750) x3 vs yrggi (cost 400) x2 | pricier units must not outnumber cheaper ones |
| ra2america: heavyra2allies | ra2mtnk (cost 750) x3 vs ra2fv (cost 500) x2 | pricier units must not outnumber cheaper ones |
| ra2america: heavyra2allies | all units are Tier 1 | heavy support should mix all tiers |
| ra2russia: heavyra2soviet | total cost 5250 | target ~10000 (±15%) |
| ra2russia: heavyra2soviet | 5 infantry : 4 vehicles | want ~5 infantry per vehicle |
| yuri: heavyyuri | total cost 6250 | target ~10000 (±15%) |
| yuri: heavyyuri | 6 infantry : 6 vehicles | want ~5 infantry per vehicle |
| yuri: heavyyuri | yrbrute (cost 400) x2 vs yrslav (cost 250) x1 | pricier units must not outnumber cheaper ones |
| yuri: heavyyuri | yrytnk (cost 1100) x2 vs yrslav (cost 250) x1 | pricier units must not outnumber cheaper ones |
| yuri: heavyyuri | yrltnk (cost 600) x4 vs yrinit (cost 200) x3 | pricier units must not outnumber cheaper ones |
| yuri: heavyyuri | yrltnk (cost 600) x4 vs yrbrute (cost 400) x2 | pricier units must not outnumber cheaper ones |
| yuri: heavyyuri | yrltnk (cost 600) x4 vs yrslav (cost 250) x1 | pricier units must not outnumber cheaper ones |
| yuri: heavyyuri | all units are Tier 1 | heavy support should mix all tiers |
| asianalliance: heavyasianalliance | total cost 7650 | target ~10000 (±15%) |
| asianalliance: heavyasianalliance | 5 infantry : 6 vehicles | want ~5 infantry per vehicle |
| asianalliance: heavyasianalliance | lynx.asian (cost 850) x3 vs tkiller.asian (cost 300) x2 | pricier units must not outnumber cheaper ones |
| consortium: heavyconsortium | 5 infantry : 6 vehicles | want ~5 infantry per vehicle |
| consortium: heavyconsortium | quantumtank.steel (cost 1600) x4 vs fedinf.steel (cost 200) x3 | pricier units must not outnumber cheaper ones |
| consortium: heavyconsortium | quantumtank.steel (cost 1600) x4 vs qinf.steel (cost 1150) x2 | pricier units must not outnumber cheaper ones |
| consortium: heavyconsortium | quantumtank.steel (cost 1600) x4 vs manta.steel (cost 850) x2 | pricier units must not outnumber cheaper ones |
| syndicate: heavysyndicate | total cost 14790 | target ~10000 (±15%) |
| syndicate: heavysyndicate | 5 infantry : 6 vehicles | want ~5 infantry per vehicle |
| syndicate: heavysyndicate | fftr.latin (cost 3000) x2 vs ptnk.asian (cost 2400) x1 | pricier units must not outnumber cheaper ones |
| naxis: heavynaxis | 5 infantry : 6 vehicles | want ~5 infantry per vehicle |
| naxis: heavynaxis | wirbelwind.nax (cost 1800) x3 vs mp40.nax (cost 375) x2 | pricier units must not outnumber cheaper ones |
| naxis: heavynaxis | tiger.nax (cost 800) x3 vs mp40.nax (cost 375) x2 | pricier units must not outnumber cheaper ones |
| lnaxis: heavylnaxis | total cost 5640 | target ~10000 (±15%) |
| lnaxis: heavylnaxis | 5 infantry : 6 vehicles | want ~5 infantry per vehicle |
| lnaxis: heavylnaxis | lunar2.nax2 (cost 350) x3 vs lunar.nax2 (cost 120) x2 | pricier units must not outnumber cheaper ones |
| lnaxis: heavylnaxis | beetle.nax2 (cost 700) x3 vs lunar.nax2 (cost 120) x2 | pricier units must not outnumber cheaper ones |
| lnaxis: heavylnaxis | beetle.nax2 (cost 700) x3 vs panzer.nax2 (cost 650) x2 | pricier units must not outnumber cheaper ones |
| lnaxis: heavylnaxis | all units are Tier 1 | heavy support should mix all tiers |
| futuretech: heavyfuturetech | total cost 4425 | target ~10000 (±15%) |
| futuretech: heavyfuturetech | 0 infantry : 11 vehicles | want ~5 infantry per vehicle |
| futuretech: heavyfuturetech | robot_cannon.futu (cost 525) x5 vs wheel.futu (cost 200) x3 | pricier units must not outnumber cheaper ones |
| futuretech: heavyfuturetech | robot_cannon.futu (cost 525) x5 vs robot_missiles.futu (cost 400) x3 | pricier units must not outnumber cheaper ones |
| tkm: heavytstkm | total cost 3060 | target ~10000 (±15%) |
| tkm: heavytstkm | 5 infantry : 5 vehicles | want ~5 infantry per vehicle |
| tkm: heavytstkm | tkmtechnical (cost 400) x4 vs tkmrifleman (cost 120) x3 | pricier units must not outnumber cheaper ones |
| tkm: heavytstkm | tkmtechnical (cost 400) x4 vs tkmrocketeer (cost 200) x2 | pricier units must not outnumber cheaper ones |
| tkm: heavytstkm | all units are Tier 1 | heavy support should mix all tiers |
| ordos: ordos_h | total cost 7100 | target ~10000 (±15%) |
| ordos: ordos_h | 5 infantry : 4 vehicles | want ~5 infantry per vehicle |
| ordos: ordos_h | all units are Tier 1 | heavy support should mix all tiers |
| ixian: ixian_h | total cost 7100 | target ~10000 (±15%) |
| ixian: ixian_h | 5 infantry : 4 vehicles | want ~5 infantry per vehicle |
| terran: heavyterran | total cost 8000 | target ~10000 (±15%) |
| terran: heavyterran | 4 infantry : 3 vehicles | want ~5 infantry per vehicle |
| terran: heavyterran | scsiegetank (cost 2800) x2 vs scfirebat (cost 500) x1 | pricier units must not outnumber cheaper ones |
| terran: heavyterran | scsiegetank (cost 2800) x2 vs scmedic (cost 600) x1 | pricier units must not outnumber cheaper ones |
| terran: heavyterran | scsiegetank (cost 2800) x2 vs scvulture (cost 900) x1 | pricier units must not outnumber cheaper ones |
| protoss: heavyprotoss | total cost 3000 | target ~10000 (±15%) |
| protoss: heavyprotoss | 2 infantry : 2 vehicles | want ~5 infantry per vehicle |
| zerg: heavyzerg | total cost 4100 | target ~10000 (±15%) |
| plymouthl: heavyplymouthl | total cost 3650 | target ~10000 (±15%) |
| plymouthl: heavyplymouthl | 0 infantry : 7 vehicles | want ~5 infantry per vehicle |
| plymouthl: heavyplymouthl | plymouth_lynx_microwave (cost 500) x3 vs plymouth_scout (cost 350) x1 | pricier units must not outnumber cheaper ones |
| plymouthl: heavyplymouthl | plymouth_lynx_rpg (cost 600) x3 vs plymouth_scout (cost 350) x1 | pricier units must not outnumber cheaper ones |
| edenl: heavyedenl | total cost 5250 | target ~10000 (±15%) |
| edenl: heavyedenl | 0 infantry : 7 vehicles | want ~5 infantry per vehicle |
| edenl: heavyedenl | eden_lynx_laser (cost 750) x3 vs eden_scout (cost 300) x1 | pricier units must not outnumber cheaper ones |
| edenl: heavyedenl | eden_lynx_railgun (cost 900) x3 vs eden_scout (cost 300) x1 | pricier units must not outnumber cheaper ones |


## F17 — fighter/bomber TurnSpeed ≠ Speed/15 (frontal: 2×)  (7)

| actor | actual | expected |
|---|---|---|
| aa_mine.ordos | TurnSpeed 8 (Speed 35) | expected 2 = Speed/15 |
| bbomb.nax2 | TurnSpeed 10 (Speed 75) | expected 5 = Speed/15 |
| cabal_hunterkillermk2 | TurnSpeed 10 (Speed 50) | expected 3 = Speed/15 |
| forgotten_cropplane | TurnSpeed 64 (Speed 160) | expected 11 = Speed/15 |
| mignuke | TurnSpeed 15 (Speed 200) | expected 13 = Speed/15 |
| scscourge | TurnSpeed 40 (Speed 200) | expected 13 = Speed/15 |
| tkmviper | TurnSpeed 25 (Speed 150) | expected 10 = Speed/15 |


## F18 — weapons targeting Air whose damage warheads can't hit Air  (21)

| actor | actual | expected |
|---|---|---|
| beehivecarriertarget | Warhead@1Dam | targets Air but no damage warhead hits Air (used by landcarr.futu) |
| boomerlaunch | Warhead@1Dam | targets Air but no damage warhead hits Air (used by yrbsub) |
| defilerplague | Warhead@HeavyChemicalWeapon, Warhead@HeavyChemicalWeaponFriendlyFire, Warhead@HeavyChemicalWeaponPercentage, Warhead@Concrete | targets Air but no damage warhead hits Air (used by scdefiler) |
| ivanattachair | Warhead@2 | targets Air but no damage warhead hits Air (used by ra2ivan) |
| naxdefensiveplanetarget | Warhead@1Dam | targets Air but no damage warhead hits Air (used by airfield.nax, airfield.nax2) |
| naxdieglocke | Warhead@HeavyChemicalWeapon, Warhead@HeavyChemicalWeaponFriendlyFire, Warhead@HeavyChemicalWeaponPercentage, Warhead@Concrete | targets Air but no damage warhead hits Air (used by dieglocke.nax2) |
| pdlaserbike | Warhead@1Dam | targets Air but no damage warhead hits Air (used by bike, chembike) |
| pdlaserltnk2 | Warhead@1Dam | targets Air but no damage warhead hits Air (used by nodltnk2) |
| sciencevesseldefensematrix | Warhead@1 | targets Air but no damage warhead hits Air (used by scsciencevessel) |
| tkmpdlaser | Warhead@1Dam | targets Air but no damage warhead hits Air (used by t72) |
| tsassaultcannon | Warhead@FlakWeapon, Warhead@FlakWeaponPercentage, Warhead@Concrete, Warhead@Chaingun | targets Air but no damage warhead hits Air (used by cabal_heavyspider, ts_gdi_wolverine) |
| tsassaultcannontal | Warhead@Chaingun, Warhead@ChaingunPercentage, Warhead@Concrete | targets Air but no damage warhead hits Air (used by ts_gdi_wolverinemkii) |
| tsfiendshard | Warhead@LightChemicalWeapon, Warhead@LightChemicalWeaponFriendlyFire, Warhead@LightChemicalWeaponPercentage, Warhead@Concrete | targets Air but no damage warhead hits Air (used by forgotten_tiberianfiend) |
| tsfiendshardblue | Warhead@Grenade, Warhead@GrenadeFriendlyFire, Warhead@GrenadePercentage, Warhead@Concrete | targets Air but no damage warhead hits Air (used by forgotten_viniferafiend) |
| tsfiendshardblueup | Warhead@Grenade, Warhead@GrenadeFriendlyFire, Warhead@GrenadePercentage, Warhead@Concrete | targets Air but no damage warhead hits Air (used by forgotten_viniferafiend) |
| tsfiendshardup | Warhead@LightChemicalWeapon, Warhead@LightChemicalWeaponFriendlyFire, Warhead@LightChemicalWeaponPercentage, Warhead@Concrete | targets Air but no damage warhead hits Air (used by forgotten_tiberianfiend) |
| tstacticalchemmissile | Warhead@Concrete | targets Air but no damage warhead hits Air (used by ts_nod_missilesilo) |
| tstacticalmissile | Warhead@Concrete | targets Air but no damage warhead hits Air (used by cabal_missilesilo, ts_nod_missilesilo) |
| tstacticalneutronmissile | Warhead@Concrete | targets Air but no damage warhead hits Air (used by cabal_missilesilo) |
| wc2deathknightdeathanddecay | Warhead@1Dam_impact | targets Air but no damage warhead hits Air (used by wc2_orc_deathknight) |
| wc2mageblizzard | Warhead@1Dam_impact | targets Air but no damage warhead hits Air (used by wc2_human_archmage, wc2_human_mage) |


## F19 — helicopter/spaceship TurnSpeed ≠ Speed/5  (32)

| actor | actual | expected |
|---|---|---|
| cruiser.steel | TurnSpeed 20 (Speed 50) | expected 10 = Speed/5 |
| dieglocke.nax2 | TurnSpeed 40 (Speed 40) | expected 8 = Speed/5 |
| farasha.ixian | TurnSpeed 16 (Speed 40) | expected 8 = Speed/5 |
| forgotten_apache | TurnSpeed 64 (Speed 160) | expected 32 = Speed/5 |
| forgotten_cobracopter | TurnSpeed 66 (Speed 165) | expected 33 = Speed/5 |
| forgotten_wasp | TurnSpeed 80 (Speed 200) | expected 40 = Speed/5 |
| harbinger.futu | TurnSpeed 8 (Speed 125) | expected 25 = Speed/5 |
| haunebu.nax2 | TurnSpeed 66 (Speed 66) | expected 13 = Speed/5 |
| haunebu2.nax2 | TurnSpeed 55 (Speed 55) | expected 11 = Speed/5 |
| inspect.steel | TurnSpeed 25 (Speed 25) | expected 5 = Speed/5 |
| modhip | TurnSpeed 10 (Speed 100) | expected 20 = Speed/5 |
| ra2_tzep | TurnSpeed 14 (Speed 35) | expected 7 = Speed/5 |
| ra2zep | TurnSpeed 12 (Speed 30) | expected 6 = Speed/5 |
| scarbiter | TurnSpeed 28 (Speed 75) | expected 15 = Speed/5 |
| scbattlecruiser | TurnSpeed 12 (Speed 30) | expected 6 = Speed/5 |
| scbehemoth | TurnSpeed 12 (Speed 30) | expected 6 = Speed/5 |
| sccarrier | TurnSpeed 18 (Speed 45) | expected 9 = Speed/5 |
| scphobos | TurnSpeed 10 (Speed 25) | expected 5 = Speed/5 |
| scpythean | TurnSpeed 16 (Speed 40) | expected 8 = Speed/5 |
| scsciencevessel | TurnSpeed 28 (Speed 66) | expected 13 = Speed/5 |
| scshuttle | TurnSpeed 20 (Speed 150) | expected 30 = Speed/5 |
| scstarshipsovereign | TurnSpeed 16 (Speed 40) | expected 8 = Speed/5 |
| tran.gdi | TurnSpeed 20 (Speed 150) | expected 30 = Speed/5 |
| tran.nod | TurnSpeed 20 (Speed 150) | expected 30 = Speed/5 |
| ts_nod_harpy | TurnSpeed 56 (Speed 140) | expected 28 = Speed/5 |
| wc2_human_gnomish_flying_machine | TurnSpeed 28 (Speed 165) | expected 33 = Speed/5 |
| wc2_human_gyrocopter2 | TurnSpeed 28 (Speed 165) | expected 33 = Speed/5 |
| wc2_orc_goblin_zeppelin | TurnSpeed 28 (Speed 165) | expected 33 = Speed/5 |
| wraith.ordos | TurnSpeed 45 (Speed 45) | expected 9 = Speed/5 |
| yrdisk | TurnSpeed 80 (Speed 80) | expected 16 = Speed/5 |
| zep.nax | TurnSpeed 20 (Speed 35) | expected 7 = Speed/5 |
| zep.nax2 | TurnSpeed 35 (Speed 35) | expected 7 = Speed/5 |


## F20 — AA support vehicle: air range ≠ 1.5 × ground range  (6)

| actor | actual | expected |
|---|---|---|
| apc.latin | AA range 9610 vs ground 6740 | expected 10110 = 1.5 x ground range |
| diablo.latin | AA range 10450 vs ground 7300 | expected 10950 = 1.5 x ground range |
| ra2htk | AA range 9292 vs ground 6528 | expected 9792 = 1.5 x ground range |
| scanalogue | AA range 2000 vs ground 1500 | expected 2250 = 1.5 x ground range |
| tleilax_labcrawl.ordos | AA range 6500 vs ground 6500 | expected 9750 = 1.5 x ground range |
| wirbelwind.nax | AA range 9052 vs ground 6368 | expected 9552 = 1.5 x ground range |


## F21 — RA2 XP elite weapon range ≠ regular + 1000  (0)

_none found_


## F22 — promotion tech gate ≠ unlocked unit's tech gate  (10)

| actor | actual | expected |
|---|---|---|
| consortium: cruiser.steel | unit tech tier 5 | promotion up_cruiser.steel tier 0 — must match |
| futuretech: up_cannon_droid.futu | unit tech tier 7 | promotion up_shotgun_droid.futu tier 0 — must match |
| futuretech: up_cryoleg.futu | unit tech tier 7 | promotion up_missile_droid.futu tier 0 — must match |
| futuretech: up_futuretank.futu | unit tech tier 7 | promotion up_orion.futu tier 0 — must match |
| futuretech: up_harbinger.futu | unit tech tier 7 | promotion up_cryocopter.futu tier 0 — must match |
| futuretech: up_missile_droid.futu | unit tech tier 0 | promotion up_cannon_droid.futu tier 7 — must match |
| syndicate: burrito.latin | unit tech tier 5 | promotion up_burrito.latin tier 0 — must match |
| syndicate: lars.latin | unit tech tier 5 | promotion up_lars.latin tier 0 — must match |
| syndicate: topol.latin | unit tech tier 5 | promotion up_topol.latin tier 0 — must match |
| tsgdi: ts_gdi_kodiakcommandship | unit tech tier 5 | promotion ts_gdi_promotion_unlockkodiak tier 0 — must match |

