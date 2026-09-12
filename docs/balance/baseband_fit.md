# Baseband feasibility and where the baseline has to go

band ratio = class price / cost0.  cost0 CANCELS — only hp0/speed0/range0/dps0 move it.
the baseline sits at exactly 100%; the 2x-HP/2x-DPS verifier at exactly 250%.

recomputation cross-check against check_band: 404/404 ratios identical

| class | members | in band now | best single-k | k* | min% | max% | span | verdict |
|---|--:|--:|--:|--:|--:|--:|--:|---|
| `melee` | 35 | 15 (43%) | 21 (60%) | 1.111 | 24% | 680% | 33.7x | CANNOT FIT — span > 2.5 |
| `heavy_infantry` | 35 | 19 (54%) | 22 (63%) | 0.745 | 81% | 4079% | 31.6x | CANNOT FIT — span > 2.5 |
| `pure_sniper` | 9 | 2 (22%) | 6 (67%) | 1.284 | 38% | 861% | 31.5x | CANNOT FIT — span > 2.5 |
| `artillery` | 30 | 9 (30%) | 20 (67%) | 0.752 | 26% | 956% | 24.3x | CANNOT FIT — span > 2.5 |
| `rocket_trooper` | 39 | 15 (38%) | 21 (54%) | 1.168 | 78% | 1401% | 22.0x | CANNOT FIT — span > 2.5 |
| `special_forces` | 16 | 3 (19%) | 10 (62%) | 1.215 | 68% | 960% | 18.3x | CANNOT FIT — span > 2.5 |
| `scout_vehicle` | 16 | 11 (69%) | 11 (69%) | 0.887 | 36% | 673% | 15.2x | CANNOT FIT — span > 2.5 |
| `fire_support` | 28 | 1 (4%) | 17 (61%) | 0.475 | 38% | 1138% | 13.6x | CANNOT FIT — span > 2.5 |
| `scout` | 27 | 13 (48%) | 14 (52%) | 1.145 | 53% | 560% | 12.6x | CANNOT FIT — span > 2.5 |
| `grenadier` | 7 | 2 (29%) | 4 (57%) | 1.433 | 101% | 895% | 12.6x | CANNOT FIT — span > 2.5 |
| `mbt` | 42 | 1 (2%) | 32 (76%) | 0.580 | 48% | 1164% | 11.9x | CANNOT FIT — span > 2.5 |
| `missile_vehicle` | 14 | 1 (7%) | 9 (64%) | 0.432 | 64% | 1231% | 10.7x | CANNOT FIT — span > 2.5 |
| `artillery_tank` | 7 | 1 (14%) | 5 (71%) | 0.667 | 47% | 487% | 8.3x | CANNOT FIT — span > 2.5 |
| `closecombat` | 4 | 1 (25%) | 3 (75%) | 0.811 | 101% | 526% | 4.6x | CANNOT FIT — span > 2.5 |
| `anti_air_vehicle` | 9 | 0 (0%) | 7 (78%) | 0.458 | 46% | 372% | 4.5x | CANNOT FIT — span > 2.5 |
| `mortar` | 5 | 2 (40%) | 4 (80%) | 0.896 | 58% | 249% | 3.9x | CANNOT FIT — span > 2.5 |
| `high_tech_tank` | 24 | 3 (12%) | 21 (88%) | 0.657 | 89% | 442% | 3.9x | CANNOT FIT — span > 2.5 |
| `light_tank` | 15 | 6 (40%) | 12 (80%) | 0.839 | 96% | 413% | 3.8x | CANNOT FIT — span > 2.5 |
| `line_breaker` | 16 | 0 (0%) | 9 (56%) | 0.423 | 81% | 948% | 3.7x | CANNOT FIT — span > 2.5 |
| `archer` | 4 | 2 (50%) | 3 (75%) | 1.384 | 101% | 287% | 3.6x | CANNOT FIT — span > 2.5 |
| `flying_infantry` | 9 | 6 (67%) | 7 (78%) | 1.057 | 98% | 330% | 3.6x | CANNOT FIT — span > 2.5 |
| `heavy_sniper` | 3 | 2 (67%) | 3 (100%) | 0.942 | 101% | 231% | 2.2x | fits fully |
| `dreadnought` | 5 | 0 (0%) | 5 (100%) | 0.440 | 100% | 231% | 1.7x | fits fully |
| `tank_destroyer` | 5 | 0 (0%) | 5 (100%) | 0.795 | 101% | 154% | 1.6x | fits fully |

**115/404 (28%) of priced members sit in the sweet spot today; re-scaling each class baseline alone reaches 271/404 (67%).**

**21 of 24 classes cannot fit the band as currently constituted** — their own spread exceeds the 2.5x baseline-to-verifier envelope.
But the spread is concentrated, not general: below is each class's CORE (the largest set that can share one baseline) and the members that cannot join it.

| class | core | core span | members that cannot share the core baseline |
|---|--:|--:|---|
| `melee` | 21/35 | 2.4x | `naxis_slave` (27%), `zerg_zergling` (64%), `td_nod_flamethrower` (92%), `asianalliance_japanesesamurai` (120%), `asianalliance_alligator` (127%) +9 more |
| `heavy_infantry` | 24/35 | 2.4x | `naxis_naxiflamer` (50%), `ixian_shockinfantry` (55%), `ra1_soviets_flamethrower` (57%), `ixian_storminfantry` (60%), `tkm_juggernaut` (66%) +6 more |
| `pure_sniper` | 6/9 | 2.4x | `naxis_naximercenarysniper` (60%), `forgotten_mutantsniper` (129%), `terran_reaper` (1883%) |
| `artillery` | 21/30 | 2.5x | `harkonnen_inkvine` (17%), `harkonnen_buzzsaw` (26%), `tkm_dronepodtruck` (29%), `ra1_soviets_nuclearv2launcher` (136%), `ts_gdi_juggernautmkii` (174%) +4 more |
| `rocket_trooper` | 20/39 | 2.3x | `td_gdi_rocketsoldier` (110%), `td_nod_rocketsoldier` (110%), `tkm_rocketeer` (119%), `futuretech_missiledroid` (411%), `ra2_allies_guardiangi` (453%) +14 more |
| `special_forces` | 10/16 | 1.8x | `japan_imperialscoutsman` (105%), `tkm_trooper` (122%), `cabal_eliminator800` (725%), `td_gdi_officer` (1144%), `terran_ghost` (1274%) +1 more |
| `scout_vehicle` | 12/16 | 2.5x | `atreides_sandbike` (30%), `naxis_bmwbike` (40%), `protoss_positron` (277%), `ordos_raider` (460%) |
| `fire_support` | 22/28 | 2.3x | `futuretech_energizer` (11%), `td_gdi_exosuit` (19%), `naxis_antitankcannon` (22%), `schwarzermond_korruptesbiest` (77%), `schwarzermond_crystaltank` (97%) +1 more |
| `scout` | 13/27 | 2.3x | `forgotten_mutant` (173%), `ordos_lightinfantry` (231%), `ra1_soviets_ak47conscript` (281%), `naxis_coneheadsknights` (303%), `forgotten_mutantsoldier` (311%) +9 more |
| `grenadier` | 4/7 | 1.4x | `latinsyndicate_grenademonkey` (890%), `td_gdi_empgrenadier` (1012%), `steelconsortium_hoverboardgrenadier` (3080%) |
| `mbt` | 36/42 | 2.5x | `corrino_buggy` (23%), `atreides_sonictank` (24%), `combat_tank.harkonnen` (27%), `naxis_kingtigerheavytank` (83%), `harkonnen_flametank` (151%) +1 more |
| `missile_vehicle` | 11/14 | 2.1x | `td_nod_stealthtank` (14%), `missile_tank` (16%), `ixian_ixmissiletank` (145%) |
| `artillery_tank` | 5/7 | 1.6x | `asianalliance_howitzer` (22%), `forgotten_mlrs` (184%) |
| `closecombat` | 3/4 | 1.8x | `asianalliance_fanatic` (276%) |
| `anti_air_vehicle` | 8/9 | 2.1x | `harkonnen_adp` (13%) |
| `mortar` | 4/5 | 2.2x | `ra1_soviets_mortarsoldier` (46%) |
| `high_tech_tank` | 21/24 | 2.1x | `naxis_maus` (104%), `japan_oitank` (147%), `cabal_avatar` (160%) |
| `light_tank` | 13/15 | 2.4x | `cabal_ravager` (232%), `terran_vulture` (256%) |
| `line_breaker` | 13/16 | 2.4x | `ts_gdi_disruptor` (24%), `ordos_heavyautoguntank` (84%), `protoss_archon` (88%) |
| `archer` | 3/4 | 1.9x | `wc2_humans_highelvenarcher` (744%) |
| `flying_infantry` | 7/9 | 2.2x | `zerg_shriek` (110%), `naxis_skymage` (390%) |

**114 members across those classes sit outside their own class core.** Each is one of three things, and only a maintainer can say which:
  * misclassified — `futuretech_blackwidow` is in `melee` with a range of 9000, and `corrino_buggy` is in `mbt`;
  * a legitimate higher tier that needs a tech-tier gate rather than a wider band;
  * genuinely mis-stated, which is what the pipeline exists to fix.

⛔ Until that triage happens, no baseline for these classes can be signed: the band would be fitted to a population that does not belong together.

## The fitted baselines, on the grid

| class | hp0 | speed0 | range0_wdist | dps0 | (cost0 unchanged — it cannot move the band) |
|---|--:|--:|--:|--:|---|
| `anti_air_vehicle` | 78000 | 50 | 2750 | 573 | 1000 |
| `archer` | 28000 | 97 | 9690 | 277 | 500 |
| `artillery` | 45000 | 56 | 11280 | 376 | 500 |
| `artillery_tank` | 93000 | 57 | 8000 | 350 | 700 |
| `closecombat` | 41000 | 61 | 2840 | 203 | 200 |
| `dreadnought` | 506000 | 22 | 3080 | 1652 | 3000 |
| `fire_support` | 57000 | 43 | 4750 | 997 | 1400 |
| `flying_infantry` | 19000 | 85 | 5280 | 264 | 600 |
| `grenadier` | 11000 | 107 | 7880 | 172 | 200 |
| `heavy_infantry` | 37000 | 37 | 3720 | 745 | 800 |
| `heavy_sniper` | 24000 | 75 | 7530 | 377 | 700 |
| `high_tech_tank` | 460000 | 43 | 4270 | 1314 | 2000 |
| `light_tank` | 84000 | 105 | 4200 | 168 | 400 |
| `line_breaker` | 317000 | 34 | 1060 | 677 | 1600 |
| `mbt` | 139000 | 55 | 3190 | 348 | 800 |
| `melee` | 30000 | 100 | 1670 | 333 | 280 |
| `missile_vehicle` | 69000 | 43 | 3450 | 518 | 1200 |
| `mortar` | 27000 | 45 | 8960 | 358 | 500 |
| `pure_sniper` | 14000 | 77 | 12840 | 385 | 320 |
| `rocket_trooper` | 12000 | 64 | 7590 | 234 | 300 |
| `scout` | 23000 | 69 | 5720 | 69 | 100 |
| `scout_vehicle` | 27000 | 177 | 3990 | 399 | 300 |
| `special_forces` | 18000 | 61 | 7290 | 292 | 200 |
| `tank_destroyer` | 119000 | 56 | 5960 | 715 | 600 |
