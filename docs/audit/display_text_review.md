# Display-text migration repair manifest

This manifest was generated and approved before repair. `FIX` means the exact pre-migration value was recovered through a rename-only blame chain and has now been restored. `KEEP` is a technical reference inside a display-named field. `REVIEW` is never changed automatically.

Findings: **429**; FIX: **428**; KEEP: **1**; REVIEW: **0**.

Active findings — FIX: **223**; KEEP: **1**; REVIEW: **0**.

## Identifier-family separation

| internal id | FIX | KEEP | REVIEW |
|---|---:|---:|---:|
| `ixian_autonomouscarryall` | 16 | 0 | 0 |
| `ixian_concretewall` | 20 | 0 | 0 |
| `ixian_ixresearchcenter` | 0 | 1 | 0 |
| `ra1_allies_alliedbarracks` | 3 | 0 | 0 |
| `ra1_allies_alliedsniper` | 47 | 0 | 0 |
| `ra1_allies_gapgenerator` | 10 | 0 | 0 |
| `ra1_allies_mechanic` | 7 | 0 | 0 |
| `ra1_allies_sheridanassaulttank` | 6 | 0 | 0 |
| `ra1_soviet_attackdog` | 8 | 0 | 0 |
| `ra1_soviet_cyberdog` | 11 | 0 | 0 |
| `ra1_soviet_hindattackhelicopter` | 15 | 0 | 0 |
| `ra1_soviet_ironcurtain` | 16 | 0 | 0 |
| `ra1_soviet_kotinnucleartank` | 7 | 0 | 0 |
| `ra1_soviet_migattackbomber` | 24 | 0 | 0 |
| `ra1_soviet_shocktrooper` | 1 | 0 | 0 |
| `ra1_soviet_volkov` | 10 | 0 | 0 |
| `ra1_soviet_yakscoutplane` | 13 | 0 | 0 |
| `td_gdi_advancedcommunicationscenter` | 7 | 0 | 0 |
| `td_gdi_apc` | 29 | 0 | 0 |
| `td_gdi_grenadier` | 1 | 0 | 0 |
| `td_gdi_humvee` | 6 | 0 | 0 |
| `td_gdi_mlrs` | 15 | 0 | 0 |
| `td_gdi_orca` | 11 | 0 | 0 |
| `td_nod_gunturret` | 89 | 0 | 0 |
| `td_nod_handofnod` | 22 | 0 | 0 |
| `td_nod_reconbike` | 23 | 0 | 0 |
| `td_nod_samsite` | 17 | 0 | 0 |
| `tkm_t30` | 1 | 0 | 0 |

## KEEP (1)

| active | location | field | current value | historical value | provenance | reason |
|---|---|---|---|---|---|---|
| yes | `mods/cameo/ContentPacks/D2k/Ixian/yaml/upgrades.yaml:112` | `Description` | upgrade_d2k_advanced_ixian_technology.description, ~ixian_ixresearchcenter | upgrade_d2k_advanced_ixian_technology.description, ~research_centre.ixian | `d763966177` | Description contains an explicit hidden prerequisite reference |

## REVIEW (0)

| active | location | field | current value | historical value | provenance | reason |
|---|---|---|---|---|---|---|

## FIX (428)

| active | location | field | current value | historical value | provenance | reason |
|---|---|---|---|---|---|---|
| yes | `mods/cameo/ContentPacks/D2k/Ixian/yaml/buildings.yaml:552` | `Name` | Machine td_nod_gunturret Turret | Machine Gun Turret | `c9b73aeb4c` | exact pre-migration display value recovered |
| yes | `mods/cameo/ContentPacks/D2k/Ixian/yaml/buildings.yaml:617` | `Name` | td_nod_gunturret Turret | Gun Turret | `c9b73aeb4c` | exact pre-migration display value recovered |
| yes | `mods/cameo/ContentPacks/D2k/Ordos/yaml/aircraft.yaml:166` | `Name` | td_gdi_advancedcommunicationscenter In The Sky | Eye In The Sky | `90158ee2e8` | exact pre-migration display value recovered |
| yes | `mods/cameo/ContentPacks/D2k/Ordos/yaml/aircraft.yaml:554` | `Name` | td_gdi_advancedcommunicationscenter on the Sky | Eye on the Sky | `90158ee2e8` | exact pre-migration display value recovered |
| yes | `mods/cameo/ContentPacks/D2k/Ordos/yaml/aircraft.yaml:777` | `Name` | Advanced ixian_autonomouscarryall | Advanced Carryall | `d763966177` | exact pre-migration display value recovered |
| yes | `mods/cameo/ContentPacks/D2k/Ordos/yaml/aircraft.yaml:808` | `Name` | Advanced ixian_autonomouscarryall | Advanced Carryall | `d763966177` | exact pre-migration display value recovered |
| yes | `mods/cameo/ContentPacks/D2k/Ordos/yaml/aircraft.yaml:833` | `Name` | Advanced ixian_autonomouscarryall | Advanced Carryall | `d763966177` | exact pre-migration display value recovered |
| yes | `mods/cameo/ContentPacks/D2k/Ordos/yaml/aircraft.yaml:848` | `Name` | Advanced ixian_autonomouscarryall | Advanced Carryall | `d763966177` | exact pre-migration display value recovered |
| yes | `mods/cameo/ContentPacks/D2k/Ordos/yaml/vehicles.yaml:22` | `Name` | Ordos td_gdi_apc | Ordos APC | `90158ee2e8` | exact pre-migration display value recovered |
| yes | `mods/cameo/ContentPacks/D2k/Shared/yaml/aircraft.yaml:7` | `Name` | Autonomous ixian_autonomouscarryall | Autonomous Carryall | `d763966177` | exact pre-migration display value recovered |
| yes | `mods/cameo/ContentPacks/D2k/Shared/yaml/aircraft.yaml:155` | `Name` | ixian_autonomouscarryall | Carryall | `d763966177` | exact pre-migration display value recovered |
| yes | `mods/cameo/ContentPacks/D2k/Shared/yaml/aircraft.yaml:170` | `Name` | ixian_autonomouscarryall | Carryall | `d763966177` | exact pre-migration display value recovered |
| yes | `mods/cameo/ContentPacks/D2k/Shared/yaml/buildings.yaml:245` | `Name` | Concrete ixian_concretewall | Concrete Wall | `d763966177` | exact pre-migration display value recovered |
| yes | `mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/naval.yaml:149` | `Name` | Asian td_nod_gunturret Boat | Asian Gun Boat | `c9b73aeb4c` | exact pre-migration display value recovered |
| yes | `mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/promotions.yaml:72` | `Name` | Unlock td_gdi_mlrs | Unlock MLRS | `90158ee2e8` | exact pre-migration display value recovered |
| yes | `mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/vehicles.yaml:557` | `Name` | Type 89 td_gdi_mlrs | Type 89 MLRS | `90158ee2e8` | exact pre-migration display value recovered |
| yes | `mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/vehicles.yaml:39` | `Name` | td_nod_gunturret Strider | Gun Strider | `c9b73aeb4c` | exact pre-migration display value recovered |
| yes | `mods/cameo/ContentPacks/RedAlert2Mod/Naxis/translations/en.ftl:45` | `FluentValue` | Machine td_nod_gunturret crew. | Machine gun crew. | `c9b73aeb4c` | exact pre-migration display value recovered |
| yes | `mods/cameo/ContentPacks/RedAlert2Mod/Naxis/translations/en.ftl:56` | `FluentValue` | Commando armed with a td_nod_gunturret that creates black holes. | Commando armed with a gun that creates black holes. | `c9b73aeb4c` | exact pre-migration display value recovered |
| yes | `mods/cameo/ContentPacks/RedAlert2Mod/Naxis/translations/en.ftl:60` | `FluentValue` | ra1_allies_alliedsniper Infantry with long range. | Sniper Infantry with long range. | `5d4341a51b` | exact pre-migration display value recovered |
| yes | `mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/infantry.yaml:230` | `Name` | Naxi Mercenary ra1_allies_alliedsniper | Naxi Mercenary Sniper | `5d4341a51b` | exact pre-migration display value recovered |
| yes | `mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/infantry.yaml:1036` | `Name` | BMW td_nod_reconbike | BMW Bike | `c9b73aeb4c` | exact pre-migration display value recovered |
| yes | `mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/promotions.yaml:47` | `Description` | Enables construction of the ra1_soviet_ironcurtain Nokana. | Enables construction of the Iron Nokana. | `53fb107252` | exact pre-migration display value recovered |
| yes | `mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/translations/en.ftl:66` | `FluentValue` | ra1_soviet_hindattackhelicopter Transport | Hind Transport | `53fb107252` | exact pre-migration display value recovered |
| yes | `mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/translations/en.ftl:67` | `FluentValue` | ixian_autonomouscarryall helicopter armed with a machine gun. | Carryall helicopter armed with a machine gun. | `d763966177` | exact pre-migration display value recovered |
| yes | `mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/aircraft.yaml:12` | `Name` | ra1_soviet_hindattackhelicopter Transport | Hind Transport | `53fb107252` | exact pre-migration display value recovered |
| yes | `mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/aircraft.yaml:76` | `Name` | ra1_soviet_hindattackhelicopter Transport | Hind Transport | `53fb107252` | exact pre-migration display value recovered |
| yes | `mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/aircraft.yaml:204` | `Name` | ra1_soviet_migattackbomber-21 | MiG-21 | `53fb107252` | exact pre-migration display value recovered |
| yes | `mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/aircraft.yaml:285` | `Name` | ra1_soviet_migattackbomber-21 | MiG-21 | `53fb107252` | exact pre-migration display value recovered |
| yes | `mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/buildings.yaml:561` | `Name` | Latin Sentry td_nod_gunturret | Latin Sentry Gun | `c9b73aeb4c` | exact pre-migration display value recovered |
| yes | `mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/vehicles.yaml:18` | `Name` | Mortar td_nod_reconbike | Mortar Bike | `c9b73aeb4c` | exact pre-migration display value recovered |
| yes | `mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/vehicles.yaml:649` | `Name` | Latin td_gdi_apc | Latin APC | `90158ee2e8` | exact pre-migration display value recovered |
| yes | `mods/cameo/ContentPacks/TiberianDawn/GDI/translations/en.ftl:9` | `FluentValue` | GDI td_gdi_apc | GDI APC | `90158ee2e8` | exact pre-migration display value recovered |
| yes | `mods/cameo/ContentPacks/TiberianDawn/GDI/translations/en.ftl:10` | `FluentValue` | Allied td_gdi_apc | Allied APC | `90158ee2e8` | exact pre-migration display value recovered |
| yes | `mods/cameo/ContentPacks/TiberianDawn/GDI/translations/en.ftl:44` | `FluentValue` | td_gdi_orca | Orca | `90158ee2e8` | exact pre-migration display value recovered |
| yes | `mods/cameo/ContentPacks/TiberianDawn/GDI/translations/en.ftl:70` | `FluentValue` | GDI ra1_allies_alliedsniper | GDI Sniper | `5d4341a51b` | exact pre-migration display value recovered |
| yes | `mods/cameo/ContentPacks/TiberianDawn/GDI/translations/en.ftl:71` | `FluentValue` | ra1_allies_alliedsniper armed with an anti-materiel rifle. | Sniper armed with an anti-materiel rifle. | `5d4341a51b` | exact pre-migration display value recovered |
| yes | `mods/cameo/ContentPacks/TiberianDawn/GDI/translations/en.ftl:131` | `FluentContinuation` | Increases GDI ra1_allies_alliedsniper and A10 damage by 10%. | Increases GDI Sniper and A10 damage by 10%. | `5d4341a51b` | exact pre-migration display value recovered |
| yes | `mods/cameo/ContentPacks/TiberianDawn/GDI/translations/en.ftl:132` | `FluentContinuation` | Also adds a machine td_nod_gunturret to the Battle and Predator Tank and increases damage by 5%. | Also adds a machine gun to the Battle and Predator Tank and increases damage by 5%. | `c9b73aeb4c` | exact pre-migration display value recovered |
| yes | `mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/aircraft.yaml:46` | `Name` | td_gdi_orca | Orca | `90158ee2e8` | exact pre-migration display value recovered |
| yes | `mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/aircraft.yaml:122` | `Name` | td_nod_gunturret | gun | `c9b73aeb4c` | exact pre-migration display value recovered |
| yes | `mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/aircraft.yaml:147` | `Name` | td_gdi_orca | Orca | `90158ee2e8` | exact pre-migration display value recovered |
| yes | `mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/infantry.yaml:277` | `Name` | GDI Heavy ra1_allies_alliedsniper | GDI Heavy Sniper | `5d4341a51b` | exact pre-migration display value recovered |
| yes | `mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/promotions.yaml:36` | `Name` | Unlock ra1_allies_alliedsniper | Unlock Sniper | `5d4341a51b` | exact pre-migration display value recovered |
| yes | `mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/promotions.yaml:50` | `Name` | Unlock Assault td_gdi_apc | Unlock Assault APC | `90158ee2e8` | exact pre-migration display value recovered |
| yes | `mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/vehicles.yaml:45` | `Name` | GDI td_gdi_apc | GDI APC | `90158ee2e8` | exact pre-migration display value recovered |
| yes | `mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/vehicles.yaml:415` | `Name` | GDI td_gdi_mlrs | GDI MLRS | `90158ee2e8` | exact pre-migration display value recovered |
| yes | `mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/vehicles.yaml:549` | `Name` | Assault td_gdi_apc | Assault APC | `90158ee2e8` | exact pre-migration display value recovered |
| yes | `mods/cameo/ContentPacks/TiberianDawn/Nod/translations/en.ftl:35` | `FluentValue` | Recon td_nod_reconbike | Recon Bike | `c9b73aeb4c` | exact pre-migration display value recovered |
| yes | `mods/cameo/ContentPacks/TiberianDawn/Nod/translations/en.ftl:85` | `FluentValue` | Nod td_nod_gunturret Turret | Nod Gun Turret | `c9b73aeb4c` | exact pre-migration display value recovered |
| yes | `mods/cameo/ContentPacks/TiberianDawn/Nod/translations/en.ftl:86` | `FluentValue` | Allied td_nod_gunturret Turret | Allied Gun Turret | `c9b73aeb4c` | exact pre-migration display value recovered |
| yes | `mods/cameo/ContentPacks/TiberianDawn/Nod/translations/en.ftl:97` | `FluentValue` | Nod td_nod_samsite Site | Nod SAM Site | `c9b73aeb4c` | exact pre-migration display value recovered |
| yes | `mods/cameo/ContentPacks/TiberianDawn/Nod/translations/en.ftl:98` | `FluentValue` | Soviet td_nod_samsite Site | Soviet SAM Site | `c9b73aeb4c` | exact pre-migration display value recovered |
| yes | `mods/cameo/ContentPacks/TiberianDawn/Nod/translations/en.ftl:136` | `FluentValue` | Black td_nod_handofnod Flamer | Black Hand Flamer | `c9b73aeb4c` | exact pre-migration display value recovered |
| yes | `mods/cameo/ContentPacks/TiberianDawn/Nod/translations/en.ftl:195` | `FluentContinuation` | Increases Range of td_nod_gunturret Turrets and the SSM by 10%. | Increases Range of Gun Turrets and the SSM by 10%. | `c9b73aeb4c` | exact pre-migration display value recovered |
| yes | `mods/cameo/ContentPacks/TiberianDawn/Nod/translations/en.ftl:215` | `FluentContinuation` | Recon td_nod_reconbike: Adds a Point Defense Laser. | Recon Bike: Adds a Point Defense Laser. | `c9b73aeb4c` | exact pre-migration display value recovered |
| yes | `mods/cameo/ContentPacks/TiberianDawn/Nod/translations/en.ftl:219` | `FluentContinuation` | td_nod_gunturret Turret and Attack Submarine: Increases damage and spread of the warhead. | Gun Turret and Attack Submarine: Increases damage and spread of the warhead. | `c9b73aeb4c` | exact pre-migration display value recovered |
| yes | `mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/buildings.yaml:46` | `Name` | td_nod_handofnod of Nod | Hand of Nod | `c9b73aeb4c` | exact pre-migration display value recovered |
| yes | `mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/buildings.yaml:395` | `Name` | Nod td_nod_gunturret Turret | Nod Gun Turret | `c9b73aeb4c` | exact pre-migration display value recovered |
| yes | `mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/buildings.yaml:497` | `Name` | Nod td_nod_samsite Site | Nod SAM Site | `c9b73aeb4c` | exact pre-migration display value recovered |
| yes | `mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/infantry.yaml:321` | `Name` | Black td_nod_handofnod Flamer | Black Hand Flamer | `c9b73aeb4c` | exact pre-migration display value recovered |
| yes | `mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/promotions.yaml:22` | `Name` | Unlock Chemical Attack td_nod_reconbike | Unlock Chemical Attack Bike | `c9b73aeb4c` | exact pre-migration display value recovered |
| yes | `mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/promotions.yaml:120` | `Name` | Unlock Black td_nod_handofnod Flamer | Unlock Black Hand Flamer | `c9b73aeb4c` | exact pre-migration display value recovered |
| yes | `mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/promotions.yaml:124` | `Description` | Allows training of Black td_nod_handofnod Flamers. | Allows training of Black Hand Flamers. | `c9b73aeb4c` | exact pre-migration display value recovered |
| yes | `mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/vehicles.yaml:274` | `Name` | Recon td_nod_reconbike | Recon Bike | `c9b73aeb4c` | exact pre-migration display value recovered |
| yes | `mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/vehicles.yaml:555` | `Name` | Chemical Attack td_nod_reconbike | Chemical Attack Bike | `c9b73aeb4c` | exact pre-migration display value recovered |
| yes | `mods/cameo/ContentPacks/TiberianSun/CABAL/translations/en.ftl:66` | `FluentValue` | Heavy cybernetic infantry armed with a gatling td_nod_gunturret and anti-air missiles. | Heavy cybernetic infantry armed with a gatling gun and anti-air missiles. | `c9b73aeb4c` | exact pre-migration display value recovered |
| yes | `mods/cameo/ContentPacks/TiberianSun/CABAL/translations/en.ftl:134` | `FluentValue` | Scarab td_gdi_apc | Scarab APC | `90158ee2e8` | exact pre-migration display value recovered |
| yes | `mods/cameo/ContentPacks/TiberianSun/CABAL/translations/en.ftl:250` | `FluentValue` | td_nod_handofnod of CABAL | Hand of CABAL | `c9b73aeb4c` | exact pre-migration display value recovered |
| yes | `mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/upgrades.yaml:95` | `Name` | td_nod_handofnod of CABAL | Hand of CABAL | `c9b73aeb4c` | exact pre-migration display value recovered |
| yes | `mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/vehicles.yaml:544` | `Name` | Scarab td_gdi_apc | Scarab APC | `90158ee2e8` | exact pre-migration display value recovered |
| yes | `mods/cameo/ContentPacks/TiberianSun/GDI/yaml/aircraft.yaml:13` | `Name` | td_gdi_orca Fighter | Orca Fighter | `90158ee2e8` | exact pre-migration display value recovered |
| yes | `mods/cameo/ContentPacks/TiberianSun/GDI/yaml/aircraft.yaml:87` | `Name` | Zone td_gdi_orca Fighter | Zone Orca Fighter | `90158ee2e8` | exact pre-migration display value recovered |
| yes | `mods/cameo/ContentPacks/TiberianSun/GDI/yaml/aircraft.yaml:259` | `Name` | td_gdi_orca Bomber | Orca Bomber | `90158ee2e8` | exact pre-migration display value recovered |
| yes | `mods/cameo/ContentPacks/TiberianSun/GDI/yaml/aircraft.yaml:332` | `Name` | ixian_autonomouscarryall | Carryall | `d763966177` | exact pre-migration display value recovered |
| yes | `mods/cameo/ContentPacks/TiberianSun/GDI/yaml/defenses.yaml:222` | `Name` | td_nod_samsite Tower | SAM Tower | `c9b73aeb4c` | exact pre-migration display value recovered |
| yes | `mods/cameo/ContentPacks/TiberianSun/GDI/yaml/husks.yaml:5` | `Name` | td_gdi_orca Fighter | Orca Fighter | `90158ee2e8` | exact pre-migration display value recovered |
| yes | `mods/cameo/ContentPacks/TiberianSun/GDI/yaml/husks.yaml:21` | `Name` | td_gdi_orca Bomber | Orca Bomber | `90158ee2e8` | exact pre-migration display value recovered |
| yes | `mods/cameo/ContentPacks/TiberianSun/GDI/yaml/husks.yaml:41` | `Name` | ixian_autonomouscarryall | Carryall | `d763966177` | exact pre-migration display value recovered |
| yes | `mods/cameo/ContentPacks/TiberianSun/GDI/yaml/promotions.yaml:60` | `Name` | Unlock Zone td_gdi_orca | Unlock Zone Orca | `90158ee2e8` | exact pre-migration display value recovered |
| yes | `mods/cameo/ContentPacks/TiberianSun/GDI/yaml/upgrades.yaml:64` | `Name` | ra1_allies_mechanic Engineering | Mech Engineering | `5d4341a51b` | exact pre-migration display value recovered |
| yes | `mods/cameo/ContentPacks/TiberianSun/GDI/yaml/vehicles.yaml:122` | `Name` | Amphibious td_gdi_apc | Amphibious APC | `90158ee2e8` | exact pre-migration display value recovered |
| yes | `mods/cameo/ContentPacks/TiberianSun/GDI/yaml/vehicles.yaml:564` | `Name` | Hover td_gdi_mlrs | Hover MLRS | `90158ee2e8` | exact pre-migration display value recovered |
| yes | `mods/cameo/ContentPacks/TiberianSun/Nod/yaml/buildings.yaml:244` | `Name` | td_nod_handofnod of Nod | Hand of Nod | `c9b73aeb4c` | exact pre-migration display value recovered |
| yes | `mods/cameo/ContentPacks/TiberianSun/Nod/yaml/defenses.yaml:219` | `Name` | td_nod_samsite Site | SAM Site | `c9b73aeb4c` | exact pre-migration display value recovered |
| yes | `mods/cameo/ContentPacks/TiberianSun/Nod/yaml/upgrades.yaml:85` | `Description` | Procure tiberium core missile technology to Rocket Soldier, td_nod_samsite Site, Attack Cycle and Stealth Tank, increasing their firepower. | Procure tiberium core missile technology to Rocket Soldier, SAM Site, Attack Cycle and Stealth Tank, increasing their firepower. | `c9b73aeb4c` | exact pre-migration display value recovered |
| yes | `mods/cameo/ContentPacks/TiberianSun/Nod/yaml/vehicles.yaml:74` | `Name` | Subterranean td_gdi_apc | Subterranean APC | `90158ee2e8` | exact pre-migration display value recovered |
| yes | `mods/cameo/fluent/rules/en.ftl:29` | `FluentValue` | Attack ra1_soviet_attackdog | Attack Dog | `53fb107252` | exact pre-migration display value recovered |
| yes | `mods/cameo/fluent/rules/en.ftl:34` | `FluentValue` | Allied ra1_allies_alliedsniper | Allied Sniper | `703cfd08e2` | exact pre-migration display value recovered |
| yes | `mods/cameo/fluent/rules/en.ftl:129` | `FluentValue` | Mobile ra1_allies_gapgenerator Generator | Mobile Gap Generator | `703cfd08e2` | exact pre-migration display value recovered |
| yes | `mods/cameo/fluent/rules/en.ftl:158` | `FluentValue` | ra1_allies_sheridanassaulttank | Sheridan | `703cfd08e2` | exact pre-migration display value recovered |
| yes | `mods/cameo/fluent/rules/en.ftl:166` | `FluentValue` | Allows construction of the ra1_allies_gapgenerator Generator | Allows construction of the Gap Generator | `703cfd08e2` | exact pre-migration display value recovered |
| yes | `mods/cameo/fluent/rules/en.ftl:167` | `FluentContinuation` | and the Mobile ra1_allies_gapgenerator Generator and the Phase Transport. | and the Mobile Gap Generator and the Phase Transport. | `703cfd08e2` | exact pre-migration display value recovered |
| yes | `mods/cameo/fluent/rules/en.ftl:168` | `FluentValue` | Unlock ra1_allies_gapgenerator Generator Technology | Unlock Gap Generator Technology | `703cfd08e2` | exact pre-migration display value recovered |
| yes | `mods/cameo/fluent/rules/en.ftl:187` | `FluentValue` | ra1_soviet_volkov | Volkov | `53fb107252` | exact pre-migration display value recovered |
| yes | `mods/cameo/fluent/rules/en.ftl:218` | `FluentValue` | ra1_soviet_kotinnucleartank Nuclear Tank | Kotin Nuclear Tank | `53fb107252` | exact pre-migration display value recovered |
| yes | `mods/cameo/fluent/rules/en.ftl:229` | `FluentValue` | ra1_soviet_migattackbomber | MiG | `53fb107252` | exact pre-migration display value recovered |
| yes | `mods/cameo/fluent/rules/en.ftl:236` | `FluentValue` | ra1_soviet_yakscoutplane | Yak | `53fb107252` | exact pre-migration display value recovered |
| yes | `mods/cameo/fluent/rules/en.ftl:255` | `FluentValue` | ra1_soviet_hindattackhelicopter | Hind | `53fb107252` | exact pre-migration display value recovered |
| yes | `mods/cameo/fluent/rules/en.ftl:306` | `FluentValue` | ra1_soviet_ironcurtain Curtain | Iron Curtain | `53fb107252` | exact pre-migration display value recovered |
| yes | `mods/cameo/fluent/rules/en.ftl:335` | `FluentValue` | AA td_nod_gunturret | AA Gun | `c9b73aeb4c` | exact pre-migration display value recovered |
| yes | `mods/cameo/fluent/rules/en.ftl:338` | `FluentValue` | ra1_allies_gapgenerator Generator | Gap Generator | `703cfd08e2` | exact pre-migration display value recovered |
| yes | `mods/cameo/fluent/rules/en.ftl:542` | `FluentValue` | ra1_allies_alliedsniper | Sniper | `703cfd08e2` | exact pre-migration display value recovered |
| yes | `mods/cameo/fluent/rules/en.ftl:698` | `FluentValue` | ra1_soviet_migattackbomber Bomber | MiG Bomber | `53fb107252` | exact pre-migration display value recovered |
| yes | `mods/cameo/fluent/rules/en.ftl:980` | `FluentValue` | Mutant ra1_allies_alliedsniper | Mutant Sniper | `703cfd08e2` | exact pre-migration display value recovered |
| yes | `mods/cameo/fluent/rules/en.ftl:1002` | `FluentValue` | Elite mutant officer whose td_nod_gunturret also reaches aircraft. | Elite mutant officer whose gun also reaches aircraft. | `c9b73aeb4c` | exact pre-migration display value recovered |
| yes | `mods/cameo/fluent/rules/en.ftl:1056` | `FluentValue` | Fast scout car whose machine td_nod_gunturret can also strafe aircraft. | Fast scout car whose machine gun can also strafe aircraft. | `c9b73aeb4c` | exact pre-migration display value recovered |
| yes | `mods/cameo/fluent/rules/en.ftl:1109` | `FluentValue` | td_gdi_apc Truck | APC Truck | `90158ee2e8` | exact pre-migration display value recovered |
| yes | `mods/cameo/fluent/rules/en.ftl:1119` | `FluentContinuation` | Replaced by the td_gdi_mlrs after that promotion. | Replaced by the MLRS after that promotion. | `90158ee2e8` | exact pre-migration display value recovered |
| yes | `mods/cameo/fluent/rules/en.ftl:1148` | `FluentValue` | Forgotten td_gdi_mlrs | Forgotten MLRS | `90158ee2e8` | exact pre-migration display value recovered |
| yes | `mods/cameo/fluent/rules/en.ftl:1151` | `FluentContinuation` | Requires the td_gdi_mlrs promotion and replaces the Missile Van. | Requires the MLRS promotion and replaces the Missile Van. | `90158ee2e8` | exact pre-migration display value recovered |
| yes | `mods/cameo/fluent/rules/en.ftl:1182` | `FluentContinuation` | Nine garrisoned infantry fire from inside; its own td_nod_gunturret covers the approaches. | Nine garrisoned infantry fire from inside; its own gun covers the approaches. | `c9b73aeb4c` | exact pre-migration display value recovered |
| yes | `mods/cameo/fluent/rules/en.ftl:1191` | `FluentValue` | ixian_autonomouscarryall | Carryall | `d763966177` | exact pre-migration display value recovered |
| yes | `mods/cameo/fluent/rules/en.ftl:1300` | `FluentValue` | Scrap-built machine td_nod_gunturret tower. | Scrap-built machine gun tower. | `c9b73aeb4c` | exact pre-migration display value recovered |
| yes | `mods/cameo/fluent/rules/en.ftl:1315` | `FluentValue` | Juggerflak ixian_concretewall | Juggerflak Wall | `d763966177` | exact pre-migration display value recovered |
| yes | `mods/cameo/fluent/rules/en.ftl:1316` | `FluentValue` | ixian_concretewall-mounted Juggernaut flak battery. | Wall-mounted Juggernaut flak battery. | `d763966177` | exact pre-migration display value recovered |
| yes | `mods/cameo/fluent/rules/en.ftl:1437` | `FluentValue` | Unlock td_gdi_mlrs | Unlock MLRS | `90158ee2e8` | exact pre-migration display value recovered |
| yes | `mods/cameo/fluent/rules/en.ftl:1439` | `FluentContinuation` | Allows construction of the Forgotten td_gdi_mlrs rocket artillery, replacing the Missile Van. | Allows construction of the Forgotten MLRS rocket artillery, replacing the Missile Van. | `90158ee2e8` | exact pre-migration display value recovered |
| yes | `mods/cameo/fluent/rules/en.ftl:1446` | `FluentContinuation` | Follows the td_gdi_mlrs promotion. | Follows the MLRS promotion. | `90158ee2e8` | exact pre-migration display value recovered |
| yes | `mods/cameo/fluent/rules/en.ftl:1463` | `FluentValue` | td_nod_samsite Site | SAM Site | `c9b73aeb4c` | exact pre-migration display value recovered |
| yes | `mods/cameo/fluent/rules/en.ftl:1634` | `FluentContinuation` | Nuclear V2 Launcher, ra1_soviet_migattackbomber and Su-57: 15% | Nuclear V2 Launcher, Mig and Su-57: 15% | `53fb107252` | exact pre-migration display value recovered |
| yes | `mods/cameo/fluent/rules/en.ftl:1635` | `FluentContinuation` | Missile Submarine, td_nod_samsite Site and ra1_soviet_hindattackhelicopter: 10% | Missile Submarine, SAM Site and Hind: 10% | `c9b73aeb4c`, `53fb107252` | exact pre-migration display value recovered |
| yes | `mods/cameo/fluent/rules/en.ftl:1636` | `FluentContinuation` | Mammoth Tank, Monster Tank and ra1_soviet_volkov: 5% | Mammoth Tank, Monster Tank and Volkov: 5% | `53fb107252` | exact pre-migration display value recovered |
| yes | `mods/cameo/fluent/rules/en.ftl:1640` | `FluentContinuation` | Adds Napalm Warheads to the V1 Truck, ra1_soviet_migattackbomber, Su-57 and all tanks. | Adds Napalm Warheads to the V1 Truck, Mig, Su-57 and all tanks. | `53fb107252` | exact pre-migration display value recovered |
| yes | `mods/cameo/fluent/rules/en.ftl:1642` | `FluentContinuation` | Adds Incendiary Bullets to the Rifle Infantry, ra1_soviet_yakscoutplane, ra1_soviet_hindattackhelicopter and Gatling Tank. | Adds Incendiary Bullets to the Rifle Infantry, Yak, Hind and Gatling Tank. | `53fb107252` | exact pre-migration display value recovered |
| yes | `mods/cameo/fluent/rules/en.ftl:1651` | `FluentValue` | Reduces the Reload Delay of Tanks, Hinds and ra1_soviet_volkov by 40%. | Reduces the Reload Delay of Tanks, Hinds and Volkov by 40%. | `53fb107252` | exact pre-migration display value recovered |
| yes | `mods/cameo/fluent/rules/en.ftl:1742` | `FluentValue` | All Tanks, td_nod_gunturret Turrets and Artillery gain increased range and damage. | All Tanks, Gun Turrets and Artillery gain increased range and damage. | `c9b73aeb4c` | exact pre-migration display value recovered |
| yes | `mods/cameo/fluent/rules/en.ftl:1743` | `FluentContinuation` | td_nod_gunturret Turret, Ix Combat Tanks and Duelist Tanks: 10% higher range and 25% more damage. | Gun Turret, Ix Combat Tanks and Duelist Tanks: 10% higher range and 25% more damage. | `c9b73aeb4c` | exact pre-migration display value recovered |
| yes | `mods/cameo/fluent/rules/en.ftl:1845` | `FluentContinuation` | Replaces ra1_soviet_hindattackhelicopter with Kamov | Replaces Hind with Kamov | `53fb107252` | exact pre-migration display value recovered |
| yes | `mods/cameo/fluent/rules/en.ftl:1864` | `FluentContinuation` | Unlocks ra1_soviet_kotinnucleartank Nuclear Tank | Unlocks Kotin Nuclear Tank | `53fb107252` | exact pre-migration display value recovered |
| yes | `mods/cameo/fluent/rules/en.ftl:1918` | `FluentContinuation` | Equips ra1_soviet_volkov with arcing Tesla Bombs. | Equips Volkov with arcing Tesla Bombs. | `53fb107252` | exact pre-migration display value recovered |
| yes | `mods/cameo/fluent/rules/en.ftl:1922` | `FluentContinuation` | Heavy Rockets such as of the Mammoth Tank, Kamov, ra1_soviet_migattackbomber and V2 Launcher | Heavy Rockets such as of the Mammoth Tank, Kamov, Mig and V2 Launcher | `53fb107252` | exact pre-migration display value recovered |
| yes | `mods/cameo/fluent/rules/en.ftl:1936` | `FluentContinuation` | Heavy Rockets such as of the Siege Mammoth Tank, ra1_soviet_hindattackhelicopter, Su-57, Grad and V2 Launcher | Heavy Rockets such as of the Siege Mammoth Tank, Hind, Su-57, Grad and V2 Launcher | `53fb107252` | exact pre-migration display value recovered |
| yes | `mods/cameo/fluent/rules/en.ftl:1946` | `FluentContinuation` | Increases Speed of ra1_soviet_kotinnucleartank Nuclear Tanks by 40%. | Increases Speed of Kotin Nuclear Tanks by 40%. | `53fb107252` | exact pre-migration display value recovered |
| yes | `mods/cameo/fluent/rules/en.ftl:1950` | `FluentContinuation` | Heavy Rockets such as of the Mammoth Tank, ra1_soviet_hindattackhelicopter, ra1_soviet_migattackbomber and Nuclear V2 Launcher | Heavy Rockets such as of the Mammoth Tank, Hind, Mig and Nuclear V2 Launcher | `53fb107252` | exact pre-migration display value recovered |
| yes | `mods/cameo/fluent/rules/en.ftl:1958` | `FluentContinuation` | Equips ra1_soviet_volkov with a Nuclear Cannon. | Equips Volkov with a Nuclear Cannon. | `53fb107252` | exact pre-migration display value recovered |
| yes | `mods/cameo/fluent/rules/en.ftl:1959` | `FluentContinuation` | Increases Spread and Radiation Damage of the ra1_soviet_kotinnucleartank Nuclear Tank. | Increases Spread and Radiation Damage of the Kotin Nuclear Tank. | `53fb107252` | exact pre-migration display value recovered |
| yes | `mods/cameo/fluent/rules/en.ftl:1972` | `FluentContinuation` | Doubles the amount of Missiles that ra1_soviet_hindattackhelicopter and Kamov can fire. | Doubles the amount of Missiles that Hind and Kamov can fire. | `53fb107252` | exact pre-migration display value recovered |
| yes | `mods/cameo/fluent/rules/en.ftl:2004` | `FluentContinuation` | Increases Damage and Range of Enforcers, Pitbulls, Hover td_gdi_mlrs and td_nod_samsite Towers by 20% | Increases Damage and Range of Enforcers, Pitbulls, Hover MLRS and SAM Towers by 20% | `90158ee2e8`, `c9b73aeb4c` | exact pre-migration display value recovered |
| yes | `mods/cameo/fluent/rules/en.ftl:2007` | `FluentValue` | ra1_allies_mechanic Engineering | Mech Engineering | `703cfd08e2` | exact pre-migration display value recovered |
| yes | `mods/cameo/fluent/rules/en.ftl:2057` | `FluentValue` | td_nod_handofnod of Nod | Hand of Nod | `c9b73aeb4c` | exact pre-migration display value recovered |
| yes | `mods/cameo/fluent/rules/en.ftl:2201` | `FluentContinuation` | Support powers: Parabombs, ra1_soviet_ironcurtain Curtain, Atomic Bomb | Support powers: Parabombs, Iron Curtain, Atomic Bomb | `53fb107252` | exact pre-migration display value recovered |
| yes | `mods/cameo/fluent/rules/en.ftl:2246` | `FluentContinuation` | Support powers: ra1_soviet_ironcurtain Curtain, Nuclear Missile | Support powers: Iron Curtain, Nuclear Missile | `53fb107252` | exact pre-migration display value recovered |
| yes | `mods/cameo/fluent/rules/ftech_en.ftl:45` | `FluentValue` | Large walking ra1_allies_mechanic armed with autoguns. | Large walking mech armed with autoguns. | `703cfd08e2` | exact pre-migration display value recovered |
| yes | `mods/cameo/fluent/rules/ftech_en.ftl:51` | `FluentValue` | Large walking ra1_allies_mechanic armed with twin plasma cannons. | Large walking mech armed with twin plasma cannons. | `703cfd08e2` | exact pre-migration display value recovered |
| yes | `mods/cameo/fluent/rules/outpost2_en.ftl:231` | `FluentValue` | Fast scout armed with a machine td_nod_gunturret | Fast scout armed with a machine gun | `c9b73aeb4c` | exact pre-migration display value recovered |
| yes | `mods/cameo/fluent/rules/outpost2_en.ftl:280` | `FluentValue` | Fast scout armed with a machine td_nod_gunturret | Fast scout armed with a machine gun | `c9b73aeb4c` | exact pre-migration display value recovered |
| yes | `mods/cameo/music.yaml:115` | `MusicTitle` | TD CO ra1_soviet_ironcurtain Fist | TD CO Iron Fist | `53fb107252` | exact pre-migration display value recovered |
| yes | `mods/cameo/music.yaml:203` | `MusicTitle` | RA Counterstrike -  The Second td_nod_handofnod | RA Counterstrike -  The Second Hand | `c9b73aeb4c` | exact pre-migration display value recovered |
| yes | `mods/cameo/music.yaml:405` | `MusicTitle` | Renegade - ra1_allies_alliedsniper | Renegade - Sniper | `703cfd08e2` | exact pre-migration display value recovered |
| yes | `mods/cameo/music.yaml:419` | `MusicTitle` | Renegade - Packing ra1_soviet_ironcurtain | Renegade - Packing Iron | `53fb107252` | exact pre-migration display value recovered |
| yes | `mods/cameo/rules/defaults.yaml:1253` | `Description` | ra1_allies_alliedsniper | Sniper | `703cfd08e2` | exact pre-migration display value recovered |
| yes | `mods/cameo/rules/husks.yaml:22` | `Name` | td_gdi_apc (Destroyed) | APC (Destroyed) | `90158ee2e8` | exact pre-migration display value recovered |
| yes | `mods/cameo/rules/husks.yaml:58` | `Name` | Recon td_nod_reconbike (Destroyed) | Recon Bike (Destroyed) | `c9b73aeb4c` | exact pre-migration display value recovered |
| yes | `mods/cameo/rules/husks.yaml:276` | `Name` | ra1_soviet_migattackbomber Attack Plane | MiG Attack Plane | `53fb107252` | exact pre-migration display value recovered |
| yes | `mods/cameo/rules/husks.yaml:322` | `Name` | ra1_soviet_yakscoutplane Attack Plane | Yak Attack Plane | `53fb107252` | exact pre-migration display value recovered |
| yes | `mods/cameo/rules/husks.yaml:419` | `Name` | ra1_soviet_hindattackhelicopter | Hind | `53fb107252` | exact pre-migration display value recovered |
| yes | `mods/cameo/rules/husks.yaml:440` | `Name` | ra1_soviet_hindattackhelicopter | Hind | `53fb107252` | exact pre-migration display value recovered |
| yes | `mods/cameo/rules/redalert.yaml:1324` | `Name` | Dragunov Anti Material ra1_allies_alliedsniper | Dragunov Anti Material Sniper | `703cfd08e2` | exact pre-migration display value recovered |
| yes | `mods/cameo/rules/redalert.yaml:1855` | `Name` | Allied ra1_allies_alliedsniper | Allied Sniper | `703cfd08e2` | exact pre-migration display value recovered |
| yes | `mods/cameo/rules/redalert.yaml:2946` | `Name` | ra1_soviet_kotinnucleartank Nuclear Tank | Kotin Nuclear Tank | `53fb107252` | exact pre-migration display value recovered |
| yes | `mods/cameo/rules/redalert.yaml:3701` | `Name` | Allied td_gdi_apc | Allied APC | `90158ee2e8` | exact pre-migration display value recovered |
| yes | `mods/cameo/rules/redalert.yaml:4523` | `Name` | ra1_soviet_yakscoutplane Scout Plane | Yak Scout Plane | `53fb107252` | exact pre-migration display value recovered |
| yes | `mods/cameo/rules/redalert.yaml:4622` | `Name` | Tesla ra1_soviet_yakscoutplane | Tesla Yak | `53fb107252` | exact pre-migration display value recovered |
| yes | `mods/cameo/rules/redalert.yaml:4749` | `Name` | Armored ra1_soviet_yakscoutplane | Armored Yak | `53fb107252` | exact pre-migration display value recovered |
| yes | `mods/cameo/rules/redalert.yaml:4840` | `Name` | Nuclear ra1_soviet_yakscoutplane | Nuclear Yak | `53fb107252` | exact pre-migration display value recovered |
| yes | `mods/cameo/rules/redalert.yaml:5163` | `Name` | ra1_soviet_migattackbomber Attack Bomber | Mig Attack Bomber | `53fb107252` | exact pre-migration display value recovered |
| yes | `mods/cameo/rules/redalert.yaml:5403` | `Name` | ra1_soviet_hindattackhelicopter Attack Helicopter | Hind Attack Helicopter | `53fb107252` | exact pre-migration display value recovered |
| yes | `mods/cameo/rules/redalert.yaml:8460` | `Names[]` | Soviet Paratroopers (Mortar Soldier, ra1_soviet_cyberdog, Conscription, Tesla Tech) | Soviet Paratroopers (Mortar Soldier, Cyberdog, Conscription, Tesla Tech) | `53fb107252` | exact pre-migration display value recovered |
| yes | `mods/cameo/rules/redalert.yaml:8461` | `Names[]` | Soviet Paratroopers (Mortar Soldier, ra1_soviet_cyberdog, Conscription, Heavy Armor) | Soviet Paratroopers (Mortar Soldier, Cyberdog, Conscription, Heavy Armor) | `53fb107252` | exact pre-migration display value recovered |
| yes | `mods/cameo/rules/redalert.yaml:8462` | `Names[]` | Soviet Paratroopers (Mortar Soldier, ra1_soviet_cyberdog, Conscription, Nuclear War) | Soviet Paratroopers (Mortar Soldier, Cyberdog, Conscription, Nuclear War) | `53fb107252` | exact pre-migration display value recovered |
| yes | `mods/cameo/rules/redalert.yaml:8463` | `Names[]` | Soviet Paratroopers (Mortar Soldier, ra1_soviet_cyberdog, Industrial, Tesla Tech) | Soviet Paratroopers (Mortar Soldier, Cyberdog, Industrial, Tesla Tech) | `53fb107252` | exact pre-migration display value recovered |
| yes | `mods/cameo/rules/redalert.yaml:8464` | `Names[]` | Soviet Paratroopers (Mortar Soldier, ra1_soviet_cyberdog, Industrial, Heavy Armor) | Soviet Paratroopers (Mortar Soldier, Cyberdog, Industrial, Heavy Armor) | `53fb107252` | exact pre-migration display value recovered |
| yes | `mods/cameo/rules/redalert.yaml:8465` | `Names[]` | Soviet Paratroopers (Mortar Soldier, ra1_soviet_cyberdog, Industrial, Nuclear War) | Soviet Paratroopers (Mortar Soldier, Cyberdog, Industrial, Nuclear War) | `53fb107252` | exact pre-migration display value recovered |
| yes | `mods/cameo/rules/redalert.yaml:8466` | `Names[]` | Soviet Paratroopers (Mortar Soldier, ra1_soviet_cyberdog, Inferno, Tesla Tech) | Soviet Paratroopers (Mortar Soldier, Cyberdog, Inferno, Tesla Tech) | `53fb107252` | exact pre-migration display value recovered |
| yes | `mods/cameo/rules/redalert.yaml:8467` | `Names[]` | Soviet Paratroopers (Mortar Soldier, ra1_soviet_cyberdog, Inferno, Heavy Armor) | Soviet Paratroopers (Mortar Soldier, Cyberdog, Inferno, Heavy Armor) | `53fb107252` | exact pre-migration display value recovered |
| yes | `mods/cameo/rules/redalert.yaml:8468` | `Names[]` | Soviet Paratroopers (Mortar Soldier, ra1_soviet_cyberdog, Inferno, Nuclear War) | Soviet Paratroopers (Mortar Soldier, Cyberdog, Inferno, Nuclear War) | `53fb107252` | exact pre-migration display value recovered |
| yes | `mods/cameo/rules/redalert.yaml:9156` | `Name` | ra1_soviet_ironcurtain Curtain | Iron Curtain | `53fb107252` | exact pre-migration display value recovered |
| yes | `mods/cameo/rules/redalert.yaml:9459` | `Name` | Allied td_nod_gunturret Turret | Allied Gun Turret | `c9b73aeb4c` | exact pre-migration display value recovered |
| yes | `mods/cameo/rules/redalert.yaml:9507` | `Name` | Allied AA td_nod_gunturret | Allied AA Gun | `c9b73aeb4c` | exact pre-migration display value recovered |
| yes | `mods/cameo/rules/redalert.yaml:9688` | `Name` | Soviet td_nod_samsite Site | Soviet SAM Site | `c9b73aeb4c` | exact pre-migration display value recovered |
| yes | `mods/cameo/rules/redalert.yaml:10090` | `Name` | Unlock ra1_allies_sheridanassaulttank | Unlock Sheridan | `703cfd08e2` | exact pre-migration display value recovered |
| yes | `mods/cameo/rules/redalert.yaml:10094` | `Description` | Allows construction of the ra1_allies_sheridanassaulttank Light Tank. | Allows construction of the Sheridan Light Tank. | `703cfd08e2` | exact pre-migration display value recovered |
| yes | `mods/cameo/rules/redalert.yaml:10230` | `Name` | Unlock ra1_allies_gapgenerator Generator and Radar Jammer | Unlock Gap Generator and Radar Jammer | `703cfd08e2` | exact pre-migration display value recovered |
| yes | `mods/cameo/rules/redalert.yaml:10234` | `Description` | Allows construction of the ra1_allies_gapgenerator Generator and Mobile Radar Jammer. | Allows construction of the Gap Generator and Mobile Radar Jammer. | `703cfd08e2` | exact pre-migration display value recovered |
| yes | `mods/cameo/rules/redalert.yaml:10317` | `Name` | ra1_allies_sheridanassaulttank Assault Tank | Sheridan Assault Tank | `703cfd08e2` | exact pre-migration display value recovered |
| yes | `mods/cameo/rules/redalert.yaml:11247` | `Name` | ra1_allies_gapgenerator Generator | Gap Generator | `703cfd08e2` | exact pre-migration display value recovered |
| yes | `mods/cameo/rules/redalert.yaml:11324` | `Name` | Mobile ra1_allies_gapgenerator Generator | Mobile Gap Generator | `703cfd08e2` | exact pre-migration display value recovered |
| yes | `mods/cameo/rules/redalert.yaml:12085` | `Name` | ra1_soviet_kotinnucleartank Nuclear Tank Upgrade | Kotin Nuclear Tank Upgrade | `53fb107252` | exact pre-migration display value recovered |
| yes | `mods/cameo/rules/redalert.yaml:12087` | `Description` | Upgrades the Hammer Tank to the ra1_soviet_kotinnucleartank Nuclear Tank | Upgrades the Hammer Tank to the Kotin Nuclear Tank | `53fb107252` | exact pre-migration display value recovered |
| yes | `mods/cameo/rules/redalert.yaml:12115` | `Name` | Unlock Tesla ra1_soviet_yakscoutplane | Unlock Tesla Yak | `53fb107252` | exact pre-migration display value recovered |
| yes | `mods/cameo/rules/redalert.yaml:12117` | `Description` | Upgrades the ra1_soviet_yakscoutplane into the Tesla Yak. | Upgrades the Yak into the Tesla Yak. | `53fb107252` | exact pre-migration display value recovered |
| yes | `mods/cameo/rules/redalert.yaml:12125` | `Name` | Unlock Armored ra1_soviet_yakscoutplane | Unlock Armored Yak | `53fb107252` | exact pre-migration display value recovered |
| yes | `mods/cameo/rules/redalert.yaml:12127` | `Description` | Upgrades the ra1_soviet_yakscoutplane into the Armored Yak. | Upgrades the Yak into the Armored Yak. | `53fb107252` | exact pre-migration display value recovered |
| yes | `mods/cameo/rules/redalert.yaml:12135` | `Name` | Unlock Nuclear ra1_soviet_yakscoutplane | Unlock Nuclear Yak | `53fb107252` | exact pre-migration display value recovered |
| yes | `mods/cameo/rules/redalert.yaml:12137` | `Description` | Upgrades the ra1_soviet_yakscoutplane into the Nuclear Yak. | Upgrades the Yak into the Nuclear Yak. | `53fb107252` | exact pre-migration display value recovered |
| yes | `mods/cameo/rules/redalert.yaml:12314` | `Name` | Unlock ra1_soviet_cyberdog | Unlock Cyberdog | `53fb107252` | exact pre-migration display value recovered |
| yes | `mods/cameo/rules/redalert.yaml:12328` | `Name` | Unlock ra1_soviet_volkov | Unlock Volkov | `53fb107252` | exact pre-migration display value recovered |
| yes | `mods/cameo/rules/redalert.yaml:12471` | `Description` | Cybernetically modified ra1_soviet_attackdog that is far more durable and can attack vehicles. | Cybernetically modified dog that is far more durable and can attack vehicles. | `53fb107252` | exact pre-migration display value recovered |
| yes | `mods/cameo/rules/redalert.yaml:12475` | `Name` | ra1_soviet_cyberdog | Cyberdog | `53fb107252` | exact pre-migration display value recovered |
| yes | `mods/cameo/rules/redalert.yaml:12589` | `Name` | ra1_soviet_volkov | Volkov | `53fb107252` | exact pre-migration display value recovered |
| yes | `mods/cameo/rules/redalert2.yaml:990` | `Name` | ra1_allies_gapgenerator Generator | Gap Generator | `703cfd08e2` | exact pre-migration display value recovered |
| yes | `mods/cameo/rules/redalert2.yaml:2003` | `Name` | ra1_soviet_ironcurtain Curtain | Iron Curtain | `53fb107252` | exact pre-migration display value recovered |
| yes | `mods/cameo/rules/redalert2.yaml:2121` | `Name` | Sentry td_nod_gunturret | Sentry Gun | `c9b73aeb4c` | exact pre-migration display value recovered |
| yes | `mods/cameo/rules/redalert2.yaml:3265` | `Name` | Attack ra1_soviet_attackdog | Attack Dog | `53fb107252` | exact pre-migration display value recovered |
| yes | `mods/cameo/rules/redalert2.yaml:3723` | `Name` | ra1_allies_alliedsniper | Sniper | `703cfd08e2` | exact pre-migration display value recovered |
| yes | `mods/cameo/rules/redalert2.yaml:4028` | `Name` | Attack ra1_soviet_attackdog | Attack Dog | `53fb107252` | exact pre-migration display value recovered |
| yes | `mods/cameo/rules/redalert2.yaml:5879` | `Name` | Machine td_nod_gunturret IFV | Machine Gun IFV | `c9b73aeb4c` | exact pre-migration display value recovered |
| yes | `mods/cameo/rules/redalert2.yaml:5882` | `Name` | Heavy Machine td_nod_gunturret IFV | Heavy Machine Gun IFV | `c9b73aeb4c` | exact pre-migration display value recovered |
| yes | `mods/cameo/rules/redalert2.yaml:5891` | `Name` | ra1_allies_alliedsniper IFV | Sniper IFV | `703cfd08e2` | exact pre-migration display value recovered |
| yes | `mods/cameo/rules/redalert2.yaml:8824` | `Name` | ra1_soviet_migattackbomber Bomber | MiG Bomber | `53fb107252` | exact pre-migration display value recovered |
| yes | `mods/cameo/rules/redalert2.yaml:8928` | `Name` | ra1_soviet_migattackbomber Bomber | MiG Bomber | `53fb107252` | exact pre-migration display value recovered |
| yes | `mods/cameo/rules/redalert2.yaml:11842` | `Name` | Hot ra1_soviet_attackdog Cart | Hot Dog Cart | `53fb107252` | exact pre-migration display value recovered |
| yes | `mods/cameo/rules/redalert2.yaml:12399` | `Name` | ra1_soviet_migattackbomber Bomber | MiG Bomber | `53fb107252` | exact pre-migration display value recovered |
| yes | `mods/cameo/rules/redalert2.yaml:14461` | `Description` | Adds a powerful corrosive effect to the Virus ra1_allies_alliedsniper that slows down enemy vehicles and deals damage over time. Increases Firepower by 33%. | Adds a powerful corrosive effect to the Virus Sniper that slows down enemy vehicles and deals damage over time. Increases Firepower by 33%. | `703cfd08e2` | exact pre-migration display value recovered |
| yes | `mods/cameo/rules/tkm.yaml:1340` | `Name` | ra1_allies_alliedsniper | Sniper | `703cfd08e2` | exact pre-migration display value recovered |
| yes | `mods/cameo/rules/tkm.yaml:2517` | `Name` | ra1_soviet_hindattackhelicopter | Hind | `53fb107252` | exact pre-migration display value recovered |
| yes | `mods/cameo/rules/tkm.yaml:2652` | `Name` | TKM ra1_soviet_migattackbomber | TKM Mig | `53fb107252` | exact pre-migration display value recovered |
| yes | `mods/cameo/rules/tkm.yaml:3897` | `Name` | Unlock tkm_t30 | Unlock T30 | `bbb9bd132a` | exact pre-migration display value recovered |
| yes | `mods/cameo/rules/warcraft2.yaml:38` | `Description` | Orc\nLeaders of the Horde\nBrutish and crafty.\nSupport Powers: td_gdi_advancedcommunicationscenter of Kilrogg, Death and Decay | Orc\nLeaders of the Horde\nBrutish and crafty.\nSupport Powers: Eye of Kilrogg, Death and Decay | `90158ee2e8` | exact pre-migration display value recovered |
| yes | `mods/cameo/rules/warcraft2.yaml:2903` | `Name` | ixian_concretewall | Wall | `d763966177` | exact pre-migration display value recovered |
| yes | `mods/cameo/rules/warcraft2.yaml:6577` | `Name` | td_gdi_advancedcommunicationscenter of Kilrogg | Eye of Kilrogg | `90158ee2e8` | exact pre-migration display value recovered |
| yes | `mods/cameo/rules/warcraft2.yaml:6618` | `Name` | td_gdi_advancedcommunicationscenter of Kilrogg | Eye of Kilrogg | `90158ee2e8` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/advancewars.yaml:4866` | `Name` | ixian_concretewall Pipe | Wall Pipe | `d763966177` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/advancewars.yaml:4994` | `Name` | Talon td_nod_gunturret | Talon Gun | `c9b73aeb4c` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/advancewars.yaml:7743` | `Name` | td_nod_reconbike | Bike | `c9b73aeb4c` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/advancewars.yaml:7880` | `Name` | Attack td_gdi_apc | Attack APC | `90158ee2e8` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/advancewars.yaml:9959` | `Name` | ra1_allies_mechanic Infantry | Mech Infantry | `703cfd08e2` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/advancewars.yaml:12071` | `Name` | Durable ixian_concretewall Research | Durable Wall Research | `d763966177` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/classicdoom.yaml:1045` | `Name` | Attack ra1_soviet_attackdog | Attack Dog | `53fb107252` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/classicdoom.yaml:1883` | `Name` | ra1_soviet_attackdog Food | Dog Food | `53fb107252` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/darkreign.yaml:860` | `Name` | Concrete ixian_concretewall | Concrete Wall | `d763966177` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/darkreign.yaml:885` | `Name` | Concrete ixian_concretewall | Concrete Wall | `d763966177` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/darkreign.yaml:3705` | `Description` | Recruited from outside the Freedom Guard ranks, these paid guns carry a massive shoulder mounted rail gun.\nHighly trained and ruthless, the Mercenary is tougher and deadlier than the Raider. The Mercenary can phase\nonce the Phasing Facility has been built\n\nType: Walker\nSpeed: SLow\nWeapon: Rail td_nod_gunturret\nRange: Short\nArmor: Medium | Recruited from outside the Freedom Guard ranks, these paid guns carry a massive shoulder mounted rail gun.\nHighly trained and ruthless, the Mercenary is tougher and deadlier than the Raider. The Mercenary can phase\nonce the Phasing Facility has been built\n\nType: Walker\nSpeed: SLow\nWeapon: Rail Gun\nRange: Short\nArmor: Medium | `c9b73aeb4c` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/darkreign.yaml:3754` | `Name` | ra1_allies_alliedsniper | Sniper | `703cfd08e2` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/darkreign.yaml:3761` | `Description` | With a long range electro-magnetic needle td_nod_gunturret, the ra1_allies_alliedsniper is lethal against\ninfantry targets. Like the Scout, the ra1_allies_alliedsniper can morph into objects for camouflage.\n\nType: Walker\nSpeed: Medium\nWeapon: ra1_allies_alliedsniper Rail\nRange: Long\nArmor: Light | With a long range electro-magnetic needle gun, the Sniper is lethal against\ninfantry targets. Like the Scout, the Sniper can morph into objects for camouflage.\n\nType: Walker\nSpeed: Medium\nWeapon: Sniper Rail\nRange: Long\nArmor: Light | `c9b73aeb4c`, `703cfd08e2` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/darkreign.yaml:4024` | `Name` | Spider td_nod_reconbike | Spider Bike | `c9b73aeb4c` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/darkreign.yaml:4031` | `Description` | This all terrain vehicle is the cornerstone of\nthe Freedom Guard ground force. Fast and inexpensive, it\nis able to tackle all types of terrain. Armed with a double\nrail td_nod_gunturret, it is fairly effective against armour but somewhat\nvulnerable to infantry\n\nType: Special Wheeled\nSpeed: Fast\nWeapon: Rail td_nod_gunturret\nRange: Medium\nArmor: Medium | This all terrain vehicle is the cornerstone of\nthe Freedom Guard ground force. Fast and inexpensive, it\nis able to tackle all types of terrain. Armed with a double\nrail gun, it is fairly effective against armour but somewhat\nvulnerable to infantry\n\nType: Special Wheeled\nSpeed: Fast\nWeapon: Rail Gun\nRange: Medium\nArmor: Medium | `c9b73aeb4c` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/darkreign.yaml:4171` | `Description` | The pinnacle of Freedom Guard\narmour, the Triple Rail uses hover technology stolen from\nthe Imperium to allow movement over a broader range\nof terrain types. Hauling three electro-magnetic projectile\naccelerator cannons, or rail guns, this tank is a savage\nconsumer of Imperium armour.\n\nType: Hover\nSpeed: Medium\nWeapon: Triple Rail td_nod_gunturret\nRange: Long\nArmor: Heavy | The pinnacle of Freedom Guard\narmour, the Triple Rail uses hover technology stolen from\nthe Imperium to allow movement over a broader range\nof terrain types. Hauling three electro-magnetic projectile\naccelerator cannons, or rail guns, this tank is a savage\nconsumer of Imperium armour.\n\nType: Hover\nSpeed: Medium\nWeapon: Triple Rail Gun\nRange: Long\nArmor: Heavy | `c9b73aeb4c` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/darkreign.yaml:4588` | `Name` | Sky td_nod_reconbike | Sky Bike | `c9b73aeb4c` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/darkreign.yaml:4595` | `Description` | Using a modified Spider td_nod_reconbike chassis, the\nFreedom Guard were able to produce cheap, fast aerial\nunits which could attack enemy ground troops and other\nflyers. Although quicker than the Imperium Cyclone, the\nSky td_nod_reconbike is not as heavily armoured and is outgunned in\neven combat. The speed of the Sky td_nod_reconbike, however, allows\nit to dictate the circumstance of conflict. This unit fires\nhigh-velocity mini-missiles and, like the Outrider, must rearm at the Re-Arming Deck\n\nType: Flyer\nSpeed: Fast\nWeapon: Mini Missiles\nRange: Short\nArmor: Light | Using a modified Spider Bike chassis, the\nFreedom Guard were able to produce cheap, fast aerial\nunits which could attack enemy ground troops and other\nflyers. Although quicker than the Imperium Cyclone, the\nSky Bike is not as heavily armoured and is outgunned in\neven combat. The speed of the Sky Bike, however, allows\nit to dictate the circumstance of conflict. This unit fires\nhigh-velocity mini-missiles and, like the Outrider, must rearm at the Re-Arming Deck\n\nType: Flyer\nSpeed: Fast\nWeapon: Mini Missiles\nRange: Short\nArmor: Light | `c9b73aeb4c` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/darkreign.yaml:4648` | `Description` | This ground attack aerial unit is slower and less\nmanoeuvrable than the Sky td_nod_reconbike, but considerably tougher\nand fires air to ground missiles. Deadly effective against\nImperium armour, it cannot engage other air units and\nshould be escorted by air defence units. The Outrider has\nlimited ammunition and must re-arm at the Re-Arming Deck\n\nType: Flyer\nSpeed: Fast\nWeapon: Guided Missile\nRange: Long\nArmor: Heavy | This ground attack aerial unit is slower and less\nmanoeuvrable than the Sky Bike, but considerably tougher\nand fires air to ground missiles. Deadly effective against\nImperium armour, it cannot engage other air units and\nshould be escorted by air defence units. The Outrider has\nlimited ammunition and must re-arm at the Re-Arming Deck\n\nType: Flyer\nSpeed: Fast\nWeapon: Guided Missile\nRange: Long\nArmor: Heavy | `c9b73aeb4c` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/darkreign.yaml:5189` | `Description` | Shock ra1_allies_alliedsniper Infantry.\n  Strong vs Light Vehicles and Creatures \n  Weak vs Infantry | Shock Sniper Infantry.\n  Strong vs Light Vehicles and Creatures \n  Weak vs Infantry | `703cfd08e2` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/darkreign.yaml:6763` | `Name` | ra1_soviet_shocktrooper Trooper | Shok Trooper | `53fb107252` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/darkreign.yaml:7872` | `Description` | The Low Rank Infantry\nThe Second Veterancy adquire Grenade Launcher\nThe Next Veterancy adquire Better Laser td_nod_gunturret\nThe Ultimate Veterancy transform into Jail Breaker\nThe Jail Breaker with veterancy can sabotage and attach C4 in vehicles.\n\nType: Infantry\nSpeed: Slow\nWeapon: Small Laser, Gremades, C4s\nRange: Medium\nArmor: None | The Low Rank Infantry\nThe Second Veterancy adquire Grenade Launcher\nThe Next Veterancy adquire Better Laser Gun\nThe Ultimate Veterancy transform into Jail Breaker\nThe Jail Breaker with veterancy can sabotage and attach C4 in vehicles.\n\nType: Infantry\nSpeed: Slow\nWeapon: Small Laser, Gremades, C4s\nRange: Medium\nArmor: None | `c9b73aeb4c` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/darkreign.yaml:10572` | `Name` | Support ra1_allies_alliedbarracks | Support Tent | `703cfd08e2` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/dune2.yaml:2008` | `Description` | Spawns a ixian_autonomouscarryall with Vehicles. | Spawns a Carryall with Vehicles. | `d763966177` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/dune2.yaml:2063` | `Name` | Death td_nod_handofnod | Death Hand | `c9b73aeb4c` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/dune2.yaml:2152` | `Name` | ixian_autonomouscarryall | Carryall | `d763966177` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/dune2.yaml:2184` | `Name` | Autonomous ixian_autonomouscarryall | Autonomous Carryall | `d763966177` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/dune2.yaml:2274` | `Name` | ixian_autonomouscarryall | Carryall | `d763966177` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/dune2.yaml:2291` | `Name` | ixian_autonomouscarryall | Carryall | `d763966177` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/generals.yaml:8695` | `Name` | ra1_soviet_migattackbomber Armor | MiG Armor | `53fb107252` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/generals.yaml:8747` | `Name` | Tactical Nuke ra1_soviet_migattackbomber | Tactical Nuke MiG | `53fb107252` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/generals.yaml:8807` | `Name` | ra1_soviet_migattackbomber | MiG | `53fb107252` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/generals.yaml:8916` | `Name` | Tactical Nuke ra1_soviet_migattackbomber | Tactical Nuke MiG | `53fb107252` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/generals.yaml:8998` | `Name` | ra1_soviet_migattackbomber | MiG | `53fb107252` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/generals.yaml:9827` | `Description` | Stealthed ra1_allies_alliedsniper infantry.\n  Strong vs Infantry\n  Weak vs Vehicles | Stealthed sniper infantry.\n  Strong vs Infantry\n  Weak vs Vehicles | `703cfd08e2` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/generals.yaml:12984` | `Name` | Sentry Drone td_nod_gunturret | Sentry Drone Gun | `c9b73aeb4c` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/generals.yaml:13496` | `Description` | Transports cash other players.\n Armed with self-defense machine td_nod_gunturret | Transports cash other players.\n Armed with self-defense machine gun | `c9b73aeb4c` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/halloween.yaml:267` | `Name` | Clown ra1_allies_alliedbarracks | Clown Tent | `703cfd08e2` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/heroes.yaml:640` | `Description` | Controls and provides ra1_soviet_volkov\nBuilding must be sold and rebuilt for another ra1_soviet_volkov to appear | Controls and provides Volkov\nBuilding must be sold and rebuilt for another Volkov to appear | `53fb107252` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/heroes.yaml:646` | `Name` | ra1_soviet_volkov Control Center | Volkov Control Center | `53fb107252` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/heroes.yaml:682` | `Name` | ra1_soviet_volkov | Volkov | `53fb107252` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/heroes.yaml:977` | `Description` | Permanently Stealthed until her weapon is fully charged up\nCan call Nod td_nod_reconbike | Permanently Stealthed until her weapon is fully charged up\nCan call Nod Bike | `c9b73aeb4c` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/heroes.yaml:1078` | `Description` | Autonomous td_nod_reconbike that helps Nashwa get over long distances\nShoots flame rockets | Autonomous Bike that helps Nashwa get over long distances\nShoots flame rockets | `c9b73aeb4c` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/heroes.yaml:1094` | `Name` | Nod td_nod_reconbike | Nod Bike | `c9b73aeb4c` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/heroes.yaml:1141` | `Name` | Nod td_nod_reconbike (Destroyed) | Nod Bike (Destroyed) | `c9b73aeb4c` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/heroes.yaml:1332` | `Description` | Unable to use his left td_nod_handofnod and full of hatred - he is equipped with Tiberium Flachette SMG and\npowerful aura that spreads radiation which hurts enemies but heals allies | Unable to use his left hand and full of hatred - he is equipped with Tiberium Flachette SMG and\npowerful aura that spreads radiation which hurts enemies but heals allies | `c9b73aeb4c` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/heroes.yaml:1419` | `Description` | Unable to use his left td_nod_handofnod and full of hatred - he is equipped with Super Shotgun and\npowerful aura that spreads radiation which hurts enemies but heals allies | Unable to use his left hand and full of hatred - he is equipped with Super Shotgun and\npowerful aura that spreads radiation which hurts enemies but heals allies | `c9b73aeb4c` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/heroes.yaml:2857` | `Name` | Multi-direction photon td_nod_gunturret | Multi-direction photon gun | `c9b73aeb4c` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/heroes.yaml:2921` | `Name` | Machine td_nod_gunturret firing remote | Machine gun firing remote | `c9b73aeb4c` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/heroes.yaml:2925` | `Description` | Machine td_nod_gunturret firing remote.\n Increases attack range by 1 tile. | Machine gun firing remote.\n Increases attack range by 1 tile. | `c9b73aeb4c` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/heroes.yaml:3120` | `Name` | Mortar ra1_allies_alliedsniper | Mortar Sniper | `703cfd08e2` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/heroes.yaml:3124` | `Description` | Kurt uses Mortar aganist buildings in ra1_allies_alliedsniper Mode | Kurt uses Mortar aganist buildings in Sniper Mode | `703cfd08e2` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/heroes.yaml:3135` | `Name` | Mortar ra1_allies_alliedsniper | Mortar Sniper | `703cfd08e2` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/heroes.yaml:3139` | `Description` | Kurt uses Missiles aganist vehicles in ra1_allies_alliedsniper Mode | Kurt uses Missiles aganist vehicles in Sniper Mode | `703cfd08e2` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/keeper.yaml:8` | `Name` | Carvable Dirt ixian_concretewall | Carvable Dirt Wall | `d763966177` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/keeper.yaml:33` | `Name` | Dungeon ixian_concretewall | Dungeon Wall | `d763966177` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/keeper.yaml:58` | `Name` | Rock ixian_concretewall | Rock Wall | `d763966177` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/lostunits.yaml:4` | `Name` | Black td_nod_handofnod Factory | Black Hand Factory | `c9b73aeb4c` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/lostunits.yaml:75` | `Name` | Nod td_gdi_apc (Destroyed) | Nod APC (Destroyed) | `90158ee2e8` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/lostunits.yaml:143` | `Name` | Black td_nod_handofnod Biological Lab | Black Hand Biological Lab | `c9b73aeb4c` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/lostunits.yaml:163` | `Name` | Black td_nod_handofnod Biological Lab (Destroyed) | Black Hand Biological Lab (Destroyed) | `c9b73aeb4c` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/lostunits.yaml:347` | `Name` | GDI Pistol td_gdi_orca | GDI Pistol Orca | `90158ee2e8` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/lostunits.yaml:858` | `Name` | td_nod_gunturret | gun | `c9b73aeb4c` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/lostunits.yaml:989` | `Name` | Black td_nod_reconbike | Black Bike | `c9b73aeb4c` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/lostunits.yaml:1007` | `Name` | Nod td_gdi_apc | Nod APC | `90158ee2e8` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/mcvmarket.yaml:507` | `Name` | Construction Rig (Shadow td_nod_handofnod) | Construction Rig (Shadow Hand) | `c9b73aeb4c` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/mcvmarket.yaml:552` | `Name` | Construction Rig (Shadow td_nod_handofnod) | Construction Rig (Shadow Hand) | `c9b73aeb4c` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/mindustry.yaml:197` | `Description` | Twin td_nod_gunturret defense.\n  Uses stone munition | Twin gun defense.\n  Uses stone munition | `c9b73aeb4c` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/mindustry.yaml:264` | `Description` | Basic cheap defense.\n  Uses ra1_soviet_ironcurtain munition | Basic cheap defense.\n  Uses iron munition | `53fb107252` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/mindustry.yaml:331` | `Description` | Twin td_nod_gunturret defense.\n  Uses ra1_soviet_ironcurtain munition | Twin gun defense.\n  Uses iron munition | `c9b73aeb4c`, `53fb107252` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/mindustry.yaml:398` | `Description` | Twin td_nod_gunturret defense.\n  Uses coal munition | Twin gun defense.\n  Uses coal munition | `c9b73aeb4c` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/mindustry.yaml:462` | `Description` | Twin td_nod_gunturret defense.\n  Uses steel munition | Twin gun defense.\n  Uses steel munition | `c9b73aeb4c` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/mindustry.yaml:708` | `Name` | ra1_soviet_ironcurtain Drill | Iron Drill | `53fb107252` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/mindustry.yaml:713` | `Description` | Produces Resources and ra1_soviet_ironcurtain | Produces Resources and Iron | `53fb107252` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/mindustry.yaml:882` | `Description` | Produces Resources and Steel .\n Requires Coal and ra1_soviet_ironcurtain connection to work | Produces Resources and Steel .\n Requires Coal and Iron connection to work | `53fb107252` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/n64.yaml:542` | `Name` | td_nod_handofnod of Nod | Hand of Nod | `c9b73aeb4c` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/n64.yaml:1408` | `Name` | td_nod_samsite site | SAM site | `c9b73aeb4c` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/n64.yaml:2125` | `Name` | Recon td_nod_reconbike | Recon Bike | `c9b73aeb4c` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/n64.yaml:2314` | `Name` | td_gdi_apc | APC | `90158ee2e8` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/n64.yaml:2730` | `Name` | td_gdi_orca | Orca | `90158ee2e8` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/shockwave.yaml:1388` | `Description` | Calls ra1_soviet_ironcurtain Dragon Squadron to bombard an area with Napalm rockets | Calls Iron Dragon Squadron to bombard an area with Napalm rockets | `53fb107252` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/shockwave.yaml:2401` | `Description` | Burton gets HE Ammunition for ra1_allies_alliedsniper Rifle\nBurton's rifle will be able to hit multiple targets | Burton gets HE Ammunition for Sniper Rifle\nBurton's rifle will be able to hit multiple targets | `703cfd08e2` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/shockwave.yaml:2665` | `Description` | Stealthed ra1_allies_alliedsniper infantry.\n  Strong vs Infantry\n  Weak vs Vehicles | Stealthed sniper infantry.\n  Strong vs Infantry\n  Weak vs Vehicles | `703cfd08e2` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/shockwave.yaml:2757` | `Description` | Elite commando equipped with ra1_allies_alliedsniper Rifle.\nCan switch between close and long range modes\n  Strong vs Infantry, Buildings, Vehicles\n  Weak vs Air | Elite commando equipped with Sniper Rifle.\nCan switch between close and long range modes\n  Strong vs Infantry, Buildings, Vehicles\n  Weak vs Air | `703cfd08e2` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/shockwave.yaml:3891` | `Name` | Lockdown td_gdi_mlrs | Lockdown MLRS | `90158ee2e8` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/shockwave.yaml:4912` | `Name` | ra1_allies_sheridanassaulttank | Sheridan | `703cfd08e2` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/shockwave.yaml:4999` | `Description` | Equips ra1_allies_sheridanassaulttank Tanks with Anti-Tank Rockets | Equips Sheridan Tanks with Anti-Tank Rockets | `703cfd08e2` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/shockwave.yaml:8072` | `Name` | Quad td_nod_reconbike | Quad Bike | `c9b73aeb4c` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/shockwave.yaml:8524` | `Name` | SA2 td_nod_samsite Site | SA2 SAM Site | `c9b73aeb4c` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/shockwave.yaml:8915` | `Description` | td_gdi_apc armed with toxin td_nod_gunturret and phosphorus rockets.\nCan carry 2 passengers.\n  Strong vs Infantry\n  Weak vs Vehicles | APC armed with toxin gun and phosphorus rockets.\nCan carry 2 passengers.\n  Strong vs Infantry\n  Weak vs Vehicles | `90158ee2e8`, `c9b73aeb4c` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/shockwave.yaml:10985` | `Name` | Tarantula td_gdi_apc | Tarantula APC | `90158ee2e8` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/shockwave.yaml:10992` | `Description` | td_gdi_apc armed with toxins.\nCan carry 2 passengers.\n  Strong vs Infantry\n  Weak vs Vehicles | APC armed with toxins.\nCan carry 2 passengers.\n  Strong vs Infantry\n  Weak vs Vehicles | `90158ee2e8` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/shockwave.yaml:11329` | `Description` | ra1_allies_alliedsniper infantry.\nStealthed when not attacking\n  Strong vs Infantry\n  Weak vs Vehicles | Sniper infantry.\nStealthed when not attacking\n  Strong vs Infantry\n  Weak vs Vehicles | `703cfd08e2` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/shockwave.yaml:11676` | `Name` | ra1_allies_alliedsniper Quad Cannon | Sniper Quad Cannon | `703cfd08e2` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/shockwave.yaml:12300` | `Description` | td_gdi_humvee GLA Variant.\nDelete carry because has a grenadier in the back.\n  Strong vs Infantry\n  Weak vs Vehicles, Aircraft | Jeep GLA Variant.\nDelete carry because has a grenadier in the back.\n  Strong vs Infantry\n  Weak vs Vehicles, Aircraft | `90158ee2e8` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/shockwave.yaml:12750` | `Description` | Calls squadron of ra1_soviet_ironcurtain Dragons to bombard an area with napalm rockets | Calls squadron of Iron Dragons to bombard an area with napalm rockets | `53fb107252` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/shockwave.yaml:13215` | `Name` | EMP ra1_soviet_migattackbomber | EMP MiG | `53fb107252` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/shockwave.yaml:13291` | `Name` | EMP ra1_soviet_migattackbomber | EMP MiG | `53fb107252` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/shockwave.yaml:13418` | `Name` | ra1_soviet_migattackbomber Afterburner | MiG Afterburner | `53fb107252` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/shockwave.yaml:13422` | `Description` | ra1_soviet_migattackbomber fighters gain more fuel efficient engines gaining up to 25% movement speed | MiG fighters gain more fuel efficient engines gaining up to 25% movement speed | `53fb107252` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/shockwave.yaml:14754` | `Name` | Berzerker td_gdi_mlrs | Berzerker MLRS | `90158ee2e8` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/shockwave.yaml:14836` | `Name` | Berzerker td_gdi_mlrs | Berzerker MLRS | `90158ee2e8` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/shockwave.yaml:14840` | `Description` | Allows production of the Berzerker td_gdi_mlrs | Allows production of the Berzerker MLRS | `90158ee2e8` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/shockwave.yaml:14860` | `Name` | Gattling td_gdi_apc | Gattling APC | `90158ee2e8` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/shockwave.yaml:14867` | `Description` | td_gdi_apc armed with Gattling gun.\nCan be mounted with infantry\n  Strong vs Infantry, Aircraft\n  Weak vs Tanks | APC armed with Gattling gun.\nCan be mounted with infantry\n  Strong vs Infantry, Aircraft\n  Weak vs Tanks | `90158ee2e8` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/shockwave.yaml:15917` | `Name` | Nuclear ra1_soviet_migattackbomber | Nuclear MiG | `53fb107252` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/simcity.yaml:1402` | `Description` | Provides Railway td_nod_gunturret support power. | Provides Railway Gun support power. | `c9b73aeb4c` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/simcity.yaml:1431` | `Name` | Railway td_nod_gunturret | Railway Gun | `c9b73aeb4c` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/simcity.yaml:1709` | `Description` | Law enforcement armed with a pistol.\nCan be upgraded to have pistol replaced with Submachine td_nod_gunturret\n Strong vs Infantry\n Weak vs Vehicles, Aircraft | Law enforcement armed with a pistol.\nCan be upgraded to have pistol replaced with Submachine gun\n Strong vs Infantry\n Weak vs Vehicles, Aircraft | `c9b73aeb4c` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/simcity.yaml:2126` | `Description` | Stealth Elite law enforcement equipped with silenced ra1_allies_alliedsniper Rifle\nCan he upgraded to get Grenade Launcher\nStrong vs Infantry, Air | Stealth Elite law enforcement equipped with silenced Sniper Rifle\nCan he upgraded to get Grenade Launcher\nStrong vs Infantry, Air | `703cfd08e2` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/sow.yaml:1529` | `Name` | td_nod_gunturret Turret | Gun Turret | `c9b73aeb4c` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/starwars.yaml:3871` | `Name` | td_nod_samsite Site | SAM Site | `c9b73aeb4c` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/starwars.yaml:5201` | `Description` | Elite ra1_allies_alliedsniper infantry unit.\n  Strong vs Infantry, Buildings\n  Weak vs Vehicles | Elite sniper infantry unit.\n  Strong vs Infantry, Buildings\n  Weak vs Vehicles | `703cfd08e2` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/starwars.yaml:5224` | `Description` | Elite ra1_allies_alliedsniper infantry unit.\n  Strong vs Infantry, Buildings\n  Weak vs Vehicles | Elite sniper infantry unit.\n  Strong vs Infantry, Buildings\n  Weak vs Vehicles | `703cfd08e2` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/starwars.yaml:5256` | `Name` | Speeder td_nod_reconbike | Speeder Bike | `c9b73aeb4c` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/starwars.yaml:5728` | `Description` | Aircraft Gunship with AG Laser.\n  Strong vs Tanks, Walker ra1_allies_mechanic\n  Weak vs Infantry | Aircraft Gunship with AG Laser.\n  Strong vs Tanks, Walker Mech\n  Weak vs Infantry | `703cfd08e2` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/starwars.yaml:8063` | `Name` | Loaded Lambda (td_gdi_grenadier) | Loaded Lambda (E2) | `90158ee2e8` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/starwars.yaml:8441` | `Description` | Elite ra1_allies_alliedsniper infantry unit.\n  Strong vs Infantry, Buildings\n  Weak vs Vehicles | Elite sniper infantry unit.\n  Strong vs Infantry, Buildings\n  Weak vs Vehicles | `703cfd08e2` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/test.yaml:415` | `Description` | A Bio ra1_allies_mechanic unit that\n electrifies its victims with a Shock Shell\nStrong vs Infantry, Buildings\nWeak vs Vehicles | A Bio Mech unit that\n electrifies its victims with a Shock Shell\nStrong vs Infantry, Buildings\nWeak vs Vehicles | `703cfd08e2` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/test.yaml:694` | `Name` | Mobile ra1_allies_alliedbarracks | Mobile Tent | `703cfd08e2` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/tiberiaalliances.yaml:253` | `Name` | Black td_nod_handofnod | Black Hand | `c9b73aeb4c` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/tiberiaalliances.yaml:258` | `Description` | The Black td_nod_handofnod is a step up from the Rocket Squad, being a better-armoured anti-tank unit. Equipped with flamethrowers, they will cook a tank until it bursts, allowing the rest of your units to pass. | The Black Hand is a step up from the Rocket Squad, being a better-armoured anti-tank unit. Equipped with flamethrowers, they will cook a tank until it bursts, allowing the rest of your units to pass. | `c9b73aeb4c` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/tiberiaalliances.yaml:281` | `Description` | Similar to the GDI ra1_allies_alliedsniper Team, the Nod Confessors are a lightly-armoured infantry unit capable of eliminating enemy infantry quickly and efficiently while maintaining a safe distance. Unfortunately, they’re pretty much weak against anything else. | Similar to the GDI Sniper Team, the Nod Confessors are a lightly-armoured infantry unit capable of eliminating enemy infantry quickly and efficiently while maintaining a safe distance. Unfortunately, they’re pretty much weak against anything else. | `703cfd08e2` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/tiberiaalliances.yaml:347` | `Name` | ra1_allies_alliedsniper | Sniper | `703cfd08e2` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/tiberiaalliances.yaml:395` | `Name` | Mutant ra1_allies_alliedsniper | Mutant Sniper | `703cfd08e2` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/tiberiaalliances.yaml:460` | `Name` | Guardian td_gdi_apc | Guardian APC | `90158ee2e8` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/tiberiaalliances.yaml:1665` | `Description` | Increase ra1_allies_alliedsniper Shoot Range | Increase Sniper Shoot Range | `703cfd08e2` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/tiberiaalliances.yaml:1824` | `Description` | Upgrade the Armor of Attack td_nod_reconbike | Upgrade the Armor of Attack Bike | `c9b73aeb4c` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/tiberiaalliances.yaml:1966` | `Description` | Adds Chemical Rockets to ra1_soviet_ironcurtain Fist | Adds Chemical Rockets to Iron Fist | `53fb107252` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/tiberiaalliances.yaml:1985` | `Description` | GDI Flak Bunker, adds a little td_nod_samsite Launcher | GDI Flak Bunker, adds a little SAM Launcher | `c9b73aeb4c` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/tiberiaalliances.yaml:3100` | `Name` | td_nod_samsite lvl 5 | SAM lvl 5 | `c9b73aeb4c` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/tiberiaalliances.yaml:3119` | `Name` | td_nod_samsite lvl 10 | SAM lvl 10 | `c9b73aeb4c` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/tiberiaalliances.yaml:3138` | `Name` | td_nod_samsite lvl 20 | SAM lvl 20 | `c9b73aeb4c` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/tiberiaalliances.yaml:3955` | `Name` | Nod td_nod_handofnod | Nod Hand | `c9b73aeb4c` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/tiberiaalliances.yaml:4966` | `Name` | td_gdi_advancedcommunicationscenter of Kane | Eye of Kane | `90158ee2e8` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/tiberiaalliances.yaml:5006` | `Name` | td_gdi_advancedcommunicationscenter of Kane AA Launcher | Eye of Kane AA Launcher | `90158ee2e8` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/tiberiaalliances.yaml:5377` | `Name` | Machine td_nod_gunturret Nest | Machine Gun Nest | `c9b73aeb4c` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/tiberiaalliances.yaml:5567` | `Name` | Shredder Machine td_nod_gunturret | Shredder Machine Gun | `c9b73aeb4c` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/tiberiaalliances.yaml:5819` | `Name` | Reaper Machine td_nod_gunturret | Reaper Machine Gun | `c9b73aeb4c` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/tomorrow.yaml:2618` | `Name` | Living ixian_concretewall | Living Wall | `d763966177` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/tomorrow.yaml:2818` | `Description` | Elite ra1_allies_alliedsniper infantry unit.\n  Strong vs Infantry, Buildings\n  Weak vs Vehicles | Elite sniper infantry unit.\n  Strong vs Infantry, Buildings\n  Weak vs Vehicles | `703cfd08e2` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/tomorrow.yaml:2869` | `Name` | Rocket td_gdi_apc | Rocket APC | `90158ee2e8` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/tomorrow.yaml:3376` | `Name` | ra1_soviet_hindattackhelicopter | Hind | `53fb107252` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/tomorrow.yaml:3595` | `Name` | Tear Gas td_gdi_apc | Tear Gas APC | `90158ee2e8` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/tomorrow.yaml:3738` | `Name` | Flame td_nod_reconbike | Flame Bike | `c9b73aeb4c` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/wh40k.yaml:5800` | `Name` | ra1_allies_alliedsniper Scout | Sniper Scout | `703cfd08e2` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/wh40k.yaml:6084` | `Name` | Assault td_nod_gunturret Terminator Marine | Assault Gun Terminator Marine | `c9b73aeb4c` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/wh40k.yaml:6528` | `Name` | Rhino td_gdi_apc | Rhino APC | `90158ee2e8` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/wh40k.yaml:10916` | `Name` | Rhino td_gdi_apc | Rhino APC | `90158ee2e8` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/wh40k.yaml:11439` | `Name` | Plasma td_nod_gunturret Cultist | Plasma Gun Cultist | `c9b73aeb4c` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/win98.yaml:671` | `Name` | CRT ixian_concretewall | CRT Wall | `d763966177` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/worms.yaml:1299` | `Description` | Elite ra1_allies_alliedsniper infantry unit.\n  Strong vs Infantry, Buildings\n  Weak vs Vehicles | Elite sniper infantry unit.\n  Strong vs Infantry, Buildings\n  Weak vs Vehicles | `703cfd08e2` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/wz2100.yaml:1105` | `Name` | ixian_concretewall | Wall | `d763966177` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/wz2100.yaml:1198` | `Name` | ixian_concretewall with Machinegun | Wall with Machinegun | `d763966177` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/wz2100.yaml:1219` | `Name` | ixian_concretewall with Light Cannon | Wall with Light Cannon | `d763966177` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/wz2100.yaml:1237` | `Name` | ixian_concretewall with Medium Cannon | Wall with Medium Cannon | `d763966177` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/wz2100.yaml:1257` | `Name` | ixian_concretewall with Heavy Cannon | Wall with Heavy Cannon | `d763966177` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/wz2100.yaml:2092` | `Description` | td_nod_gunturret Choppers attack enemies along a line\nwith chainguns. | Gun Choppers attack enemies along a line\nwith chainguns. | `c9b73aeb4c` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/wz2100.yaml:2250` | `Name` | ixian_concretewall | Wall | `d763966177` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/wz2100.yaml:4403` | `Name` | Needle td_nod_gunturret Cobra Half-tracks | Needle Gun Cobra Half-tracks | `c9b73aeb4c` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/wz2100.yaml:4476` | `Name` | Twin Assault td_nod_gunturret Python Tracks | Twin Assault Gun Python Tracks | `c9b73aeb4c` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/wz2100.yaml:4488` | `Name` | Assault td_nod_gunturret Python Tracks | Assault Gun Python Tracks | `c9b73aeb4c` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/wz2100.yaml:4610` | `Name` | Twin Assault td_nod_gunturret Python Hovercraft | Twin Assault Gun Python Hovercraft | `c9b73aeb4c` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/wz2100.yaml:5117` | `Name` | Needle td_nod_gunturret Scorpion Half-tracks | Needle Gun Scorpion Half-tracks | `c9b73aeb4c` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/wz2100.yaml:5154` | `Name` | Twin Assault td_nod_gunturret Mantis Tracks | Twin Assault Gun Mantis Tracks | `c9b73aeb4c` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/wz2100.yaml:5166` | `Name` | Assault td_nod_gunturret Mantis Tracks | Assault Gun Mantis Tracks | `c9b73aeb4c` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/wz2100.yaml:5277` | `Name` | Twin Assault td_nod_gunturret Mantis Hovercraft | Twin Assault Gun Mantis Hovercraft | `c9b73aeb4c` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/wz2100.yaml:5338` | `Name` | BabaMG td_gdi_humvee Wheels | BabaMG Jeep Wheels | `90158ee2e8` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/wz2100.yaml:5357` | `Name` | BabaRK td_gdi_humvee Wheels | BabaRK Jeep Wheels | `90158ee2e8` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/wz2100.yaml:5669` | `Name` | Needle td_nod_gunturret Panther Half-tracks | Needle Gun Panther Half-tracks | `c9b73aeb4c` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/wz2100.yaml:5706` | `Name` | Twin Assault td_nod_gunturret Tiger Tracks | Twin Assault Gun Tiger Tracks | `c9b73aeb4c` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/wz2100.yaml:5718` | `Name` | Assault td_nod_gunturret Tiger Tracks | Assault Gun Tiger Tracks | `c9b73aeb4c` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/wz2100.yaml:5816` | `Name` | Twin Assault td_nod_gunturret Tiger Hovercraft | Twin Assault Gun Tiger Hovercraft | `c9b73aeb4c` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/wz2100.yaml:6104` | `Name` | Needle td_nod_gunturret Retribution Half-tracks | Needle Gun Retribution Half-tracks | `c9b73aeb4c` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/wz2100.yaml:6129` | `Name` | Twin Assault td_nod_gunturret Vengeance Tracks | Twin Assault Gun Vengeance Tracks | `c9b73aeb4c` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/wz2100.yaml:6141` | `Name` | Assault td_nod_gunturret Vengeance Tracks | Assault Gun Vengeance Tracks | `c9b73aeb4c` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/wz2100.yaml:6264` | `Name` | Twin Assault td_nod_gunturret Vengeance Hovercraft | Twin Assault Gun Vengeance Hovercraft | `c9b73aeb4c` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/wz2100.yaml:6987` | `Name` | PK-105 td_gdi_humvee Barbarian MG | PK-105 Jeep Barbarian MG | `90158ee2e8` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/wz2100.yaml:7012` | `Name` | PK-105 td_gdi_humvee Rocket Array | PK-105 Jeep Rocket Array | `90158ee2e8` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/wz2100.yaml:7121` | `Name` | B-155 Camper Needle td_nod_gunturret | B-155 Camper Needle Gun | `c9b73aeb4c` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/wz2100.yaml:7633` | `Description` | Transports tins of ra1_soviet_attackdog food to other players.\n Armed with self-defense cannon | Transports tins of dog food to other players.\n Armed with self-defense cannon | `53fb107252` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/wz2100.yaml:7864` | `Name` | Bp-200 td_nod_gunturret Chopper | Bp-200 Gun Chopper | `c9b73aeb4c` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/wz2100.yaml:7947` | `Name` | td_nod_gunturret | gun | `c9b73aeb4c` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/wz2100.yaml:8099` | `Name` | td_nod_gunturret | gun | `c9b73aeb4c` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/wz2100.yaml:9109` | `Name` | ixian_concretewall Research | Wall Research | `d763966177` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/wz2100.yaml:9427` | `Name` | Needle td_nod_gunturret Research | Needle Gun Research | `c9b73aeb4c` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/wz2100.yaml:9431` | `Description` | Unlock Needle td_nod_gunturret Weapon | Unlock Needle Gun Weapon | `c9b73aeb4c` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/xcom.yaml:200` | `Name` | Shard td_nod_gunturret Upgrade | Shard Gun Upgrade | `c9b73aeb4c` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/xcom.yaml:205` | `Description` | Upgrades the Shotgun to the Shard td_nod_gunturret | Upgrades the Shotgun to the Shard Gun | `c9b73aeb4c` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/xcom.yaml:222` | `Names[]` | Shard td_nod_gunturret Upgrade | Shard Gun Upgrade | `c9b73aeb4c` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/xcom.yaml:224` | `Descriptions[]` | Upgrades the Shotgun to the Shard td_nod_gunturret | Upgrades the Shotgun to the Shard Gun | `c9b73aeb4c` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/xcom.yaml:552` | `Name` | Auto ra1_allies_alliedsniper Upgrade | Auto Sniper Upgrade | `703cfd08e2` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/xcom.yaml:557` | `Description` | Gives the ra1_allies_alliedsniper an automatic ra1_allies_alliedsniper Rifle | Gives the Sniper an automatic Sniper Rifle | `703cfd08e2` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/xcom.yaml:574` | `Names[]` | Auto ra1_allies_alliedsniper Upgrade | Auto Sniper Upgrade | `703cfd08e2` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/xcom.yaml:576` | `Descriptions[]` | Gives the ra1_allies_alliedsniper an automatic ra1_allies_alliedsniper Rifle | Gives the Sniper an automatic Sniper Rifle | `703cfd08e2` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/xcom.yaml:883` | `Name` | td_nod_gunturret Turret | Gun Turret | `c9b73aeb4c` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/xmas.yaml:792` | `Description` | Light infantry equipped with td_nod_gunturret | Light infantry equipped with gun | `c9b73aeb4c` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/z.yaml:216` | `Name` | ra1_allies_alliedsniper | Sniper | `703cfd08e2` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/z.yaml:317` | `Name` | td_gdi_humvee | Jeep | `90158ee2e8` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/z.yaml:551` | `Name` | td_gdi_apc | APC | `90158ee2e8` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/z.yaml:760` | `Name` | td_nod_gunturret Cannon | Gun Cannon | `c9b73aeb4c` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/z.yaml:858` | `Name` | Missile td_nod_gunturret | Missile Gun | `c9b73aeb4c` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/z.yaml:1247` | `Name` | td_nod_gunturret Lascannon | Gun Lascannon | `c9b73aeb4c` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/z.yaml:1252` | `Description` | Adds Gattling td_nod_gunturret to Fort | Adds Gattling Gun to Fort | `c9b73aeb4c` | exact pre-migration display value recovered |
| no | `mods/cameo/rules/z.yaml:1281` | `Name` | Missiles td_nod_gunturret | Missiles Gun | `c9b73aeb4c` | exact pre-migration display value recovered |
