# Tier-chain validation report

## What changed

The original `tier_gates.py` used `name.split('_')[0]` as the faction key and allowed a unit to pick the cheapest building from **any** faction for a generic token. That produced impossible build orders such as a TD Nod unit buying a GDI Construction Yard. This validator restricts each unit to buildings from its own ContentPack faction plus the same game's Shared pack.

## Corrected tier bucket medians

| tier | units | median $ | mean $ | min | max |
|---|---|---:|---:|---:|---:|
| T0 no chain | 109 | 0 | 0 | 0 | 0 |
| T1 production | 628 | 9,500 | 9,334 | 800 | 19,700 |
| T2 radar | 239 | 11,600 | 11,767 | 5,600 | 24,000 |
| T3 tech center | 1 | 15,000 | 15,000 | 15,000 | 15,000 |
| T4 tech+extra | 354 | 17,750 | 18,014 | 10,800 | 30,100 |

## Functional forms (recalibrated)

- B (T1 median) = $9,500
- S = (T4 median - B) = $8,250
- alpha (power law) = 1.1089

| tier | median $ | rational x | power x |
|---|---:|---:|---:|
| T0 no chain | 0 | 1.000 | 1.000 |
| T1 production | 9,500 | 1.000 | 1.000 |
| T2 radar | 11,600 | 0.797 | 0.801 |
| T3 tech center | 15,000 | 0.600 | 0.603 |
| T4 tech+extra | 17,750 | 0.500 | 0.500 |

Extrapolation:

- tortuga $21,500: rational x0.407, power x0.404
- deepest $30,100: rational x0.286, power x0.278
- hypothetical $50,000: rational x0.169, power x0.159

## Top 10 most expensive corrected chains

- `ra2_soviets_upgrade_kirovatomicbombs`: `$30,100` — ra2_soviets_battlelab, ra2_soviets_constructionyard, ra2_soviets_industrialplant, ra2_soviets_nuclearmissilesilo, ra2_soviets_orerefinery, ra2_soviets_radar, ra2_soviets_teslareactor
- `futuretech_harbingergunship`: `$29,800` — futuretech_battlelab, futuretech_constructionyard, futuretech_hypercore, futuretech_launchpad, futuretech_refinery, futuretech_robotcontrolcenter, futuretech_thermalpowerplant, futuretech_transmissioncenter, futuretech_warpgate
- `futuretech_cryolegionnaire`: `$29,300` — futuretech_battlelab, futuretech_constructionyard, futuretech_hypercore, futuretech_refinery, futuretech_robotcontrolcenter, futuretech_thermalpowerplant, futuretech_transmissioncenter, futuretech_troopgate, futuretech_warpgate
- `latinsyndicate_tortugatank`: `$28,500` — latinsyndicate_defensebureau, latinsyndicate_latinempradar, latinsyndicate_powerstation, latinsyndicate_recyclingcenter, latinsyndicate_recyclingrefinery, latinsyndicate_spycenter, latinsyndicate_syndicateconstructionyard, latinsyndicate_syndicatefactory
- `futuretech_beehivedronecarrier`: `$28,300` — futuretech_battlelab, futuretech_constructionyard, futuretech_hypercore, futuretech_refinery, futuretech_robotcontrolcenter, futuretech_thermalpowerplant, futuretech_transmissioncenter, futuretech_warpgate
- `futuretech_doctrine_equalizerx3`: `$28,300` — futuretech_battlelab, futuretech_constructionyard, futuretech_hypercore, futuretech_refinery, futuretech_robotcontrolcenter, futuretech_thermalpowerplant, futuretech_transmissioncenter, futuretech_warpgate
- `futuretech_energizer`: `$28,300` — futuretech_battlelab, futuretech_constructionyard, futuretech_hypercore, futuretech_refinery, futuretech_robotcontrolcenter, futuretech_thermalpowerplant, futuretech_transmissioncenter, futuretech_warpgate
- `futuretech_futuretank`: `$28,300` — futuretech_battlelab, futuretech_constructionyard, futuretech_hypercore, futuretech_refinery, futuretech_robotcontrolcenter, futuretech_thermalpowerplant, futuretech_transmissioncenter, futuretech_warpgate
- `futuretech_plasmastrider`: `$28,300` — futuretech_battlelab, futuretech_constructionyard, futuretech_hypercore, futuretech_refinery, futuretech_robotcontrolcenter, futuretech_thermalpowerplant, futuretech_transmissioncenter, futuretech_warpgate
- `ordos_deviatorartillery`: `$28,000` — ordos_constructionyard, ordos_heavyfactory, ordos_ixresearchcenter, ordos_outpost, ordos_palace, ordos_refineryordos, ordos_windtrap

## Hand-validated cases

### td_nod_lasertrooper
- `td_nod_lasertrooper`: `$27,000`
  - file: `mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/infantry.yaml`
  - prerequisites: td_nod_handofnod, td_nod_templeprime, td_nod_promotion_lasertrooper
  - `td_nod_lasertrooper` needs `td_nod_handofnod` ($1,000) for token `td_nod_handofnod`
  - `td_nod_handofnod` needs `td_nod_constructionyard` ($5,000) for token `td_nod_constructionyard`
  - `td_nod_handofnod` needs `NUKE` ($500) for token `nuke`
  - `NUKE` needs `td_nod_constructionyard` ($5,000) for token `fact`
  - `td_nod_lasertrooper` needs `td_nod_templeprime` ($5,000) for token `td_nod_templeprime`
  - `td_nod_templeprime` needs `td_nod_constructionyard` ($5,000) for token `td_nod_constructionyard`
  - `td_nod_templeprime` needs `td_nod_templeofnod` ($10,000) for token `td_nod_templeofnod`
  - `td_nod_templeofnod` needs `td_nod_constructionyard` ($5,000) for token `td_nod_constructionyard`
  - `td_nod_templeofnod` needs `td_nod_communicationscenter` ($2,500) for token `td_nod_communicationscenter`
  - `td_nod_communicationscenter` needs `td_nod_constructionyard` ($5,000) for token `td_nod_constructionyard`
  - `td_nod_communicationscenter` needs `td_nod_tiberiumrefinery` ($3,000) for token `td_nod_tiberiumrefinery`
  - `td_nod_tiberiumrefinery` needs `td_nod_constructionyard` ($5,000) for token `td_nod_constructionyard`
  - `td_nod_tiberiumrefinery` needs `NUKE` ($500) for token `nuke`
  - `NUKE` needs `td_nod_constructionyard` ($5,000) for token `fact`
  - buildings: NUKE, td_nod_communicationscenter, td_nod_constructionyard, td_nod_handofnod, td_nod_templeofnod, td_nod_templeprime, td_nod_tiberiumrefinery

### ra2_soviets_upgrade_kirovatomicbombs
- `ra2_soviets_upgrade_kirovatomicbombs`: `$30,100`
  - file: `mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/upgrades.yaml`
  - prerequisites: ra2_soviets_battlelab, ra2_soviets_industrialplant, ra2_soviets_nuclearmissilesilo
  - `ra2_soviets_upgrade_kirovatomicbombs` needs `ra2_soviets_battlelab` ($5,000) for token `ra2_soviets_battlelab`
  - `ra2_soviets_battlelab` needs `ra2_soviets_constructionyard` ($5,000) for token `ra2_soviets_constructionyard`
  - `ra2_soviets_battlelab` needs `ra2_soviets_radar` ($2,500) for token `ra2_soviets_radar`
  - `ra2_soviets_radar` needs `ra2_soviets_constructionyard` ($5,000) for token `ra2_soviets_constructionyard`
  - `ra2_soviets_radar` needs `ra2_soviets_orerefinery` ($2,000) for token `ra2_soviets_orerefinery`
  - `ra2_soviets_orerefinery` needs `ra2_soviets_constructionyard` ($5,000) for token `ra2_soviets_constructionyard`
  - `ra2_soviets_orerefinery` needs `ra2_soviets_teslareactor` ($600) for token `ra2_soviets_teslareactor`
  - `ra2_soviets_teslareactor` needs `ra2_soviets_constructionyard` ($5,000) for token `ra2_soviets_constructionyard`
  - `ra2_soviets_upgrade_kirovatomicbombs` needs `ra2_soviets_industrialplant` ($5,000) for token `ra2_soviets_industrialplant`
  - `ra2_soviets_industrialplant` needs `ra2_soviets_constructionyard` ($5,000) for token `ra2_soviets_constructionyard`
  - `ra2_soviets_industrialplant` needs `ra2_soviets_battlelab` ($5,000) for token `ra2_soviets_battlelab`
  - `ra2_soviets_battlelab` needs `ra2_soviets_constructionyard` ($5,000) for token `ra2_soviets_constructionyard`
  - `ra2_soviets_battlelab` needs `ra2_soviets_radar` ($2,500) for token `ra2_soviets_radar`
  - `ra2_soviets_radar` needs `ra2_soviets_constructionyard` ($5,000) for token `ra2_soviets_constructionyard`
  - `ra2_soviets_radar` needs `ra2_soviets_orerefinery` ($2,000) for token `ra2_soviets_orerefinery`
  - `ra2_soviets_orerefinery` needs `ra2_soviets_constructionyard` ($5,000) for token `ra2_soviets_constructionyard`
  - `ra2_soviets_orerefinery` needs `ra2_soviets_teslareactor` ($600) for token `ra2_soviets_teslareactor`
  - `ra2_soviets_teslareactor` needs `ra2_soviets_constructionyard` ($5,000) for token `ra2_soviets_constructionyard`
  - `ra2_soviets_upgrade_kirovatomicbombs` needs `ra2_soviets_nuclearmissilesilo` ($10,000) for token `ra2_soviets_nuclearmissilesilo`
  - `ra2_soviets_nuclearmissilesilo` needs `ra2_soviets_constructionyard` ($5,000) for token `ra2_soviets_constructionyard`
  - `ra2_soviets_nuclearmissilesilo` needs `ra2_soviets_battlelab` ($5,000) for token `ra2_soviets_battlelab`
  - `ra2_soviets_battlelab` needs `ra2_soviets_constructionyard` ($5,000) for token `ra2_soviets_constructionyard`
  - `ra2_soviets_battlelab` needs `ra2_soviets_radar` ($2,500) for token `ra2_soviets_radar`
  - `ra2_soviets_radar` needs `ra2_soviets_constructionyard` ($5,000) for token `ra2_soviets_constructionyard`
  - `ra2_soviets_radar` needs `ra2_soviets_orerefinery` ($2,000) for token `ra2_soviets_orerefinery`
  - `ra2_soviets_orerefinery` needs `ra2_soviets_constructionyard` ($5,000) for token `ra2_soviets_constructionyard`
  - `ra2_soviets_orerefinery` needs `ra2_soviets_teslareactor` ($600) for token `ra2_soviets_teslareactor`
  - `ra2_soviets_teslareactor` needs `ra2_soviets_constructionyard` ($5,000) for token `ra2_soviets_constructionyard`
  - buildings: ra2_soviets_battlelab, ra2_soviets_constructionyard, ra2_soviets_industrialplant, ra2_soviets_nuclearmissilesilo, ra2_soviets_orerefinery, ra2_soviets_radar, ra2_soviets_teslareactor

### futuretech_harbingergunship
- `futuretech_harbingergunship`: `$29,800`
  - file: `mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/aircraft.yaml`
  - prerequisites: futuretech_launchpad, futuretech_hypercore, futuretech_promotion_harbingergunship
  - `futuretech_harbingergunship` needs `futuretech_launchpad` ($1,500) for token `futuretech_launchpad`
  - `futuretech_launchpad` needs `futuretech_constructionyard` ($5,000) for token `futuretech_constructionyard`
  - `futuretech_launchpad` needs `futuretech_transmissioncenter` ($2,500) for token `futuretech_transmissioncenter`
  - `futuretech_transmissioncenter` needs `futuretech_constructionyard` ($5,000) for token `futuretech_constructionyard`
  - `futuretech_transmissioncenter` needs `futuretech_robotcontrolcenter` ($2,500) for token `futuretech_robotcontrolcenter`
  - `futuretech_robotcontrolcenter` needs `futuretech_constructionyard` ($5,000) for token `futuretech_constructionyard`
  - `futuretech_robotcontrolcenter` needs `futuretech_warpgate` ($2,000) for token `futuretech_warpgate`
  - `futuretech_warpgate` needs `futuretech_constructionyard` ($5,000) for token `futuretech_constructionyard`
  - `futuretech_warpgate` needs `futuretech_refinery` ($3,000) for token `futuretech_refinery`
  - `futuretech_refinery` needs `futuretech_constructionyard` ($5,000) for token `futuretech_constructionyard`
  - `futuretech_refinery` needs `futuretech_thermalpowerplant` ($800) for token `futuretech_thermalpowerplant`
  - `futuretech_thermalpowerplant` needs `futuretech_constructionyard` ($5,000) for token `futuretech_constructionyard`
  - `futuretech_harbingergunship` needs `futuretech_hypercore` ($7,500) for token `futuretech_hypercore`
  - `futuretech_hypercore` needs `futuretech_constructionyard` ($5,000) for token `futuretech_constructionyard`
  - `futuretech_hypercore` needs `futuretech_battlelab` ($5,000) for token `futuretech_battlelab`
  - `futuretech_battlelab` needs `futuretech_constructionyard` ($5,000) for token `futuretech_constructionyard`
  - `futuretech_battlelab` needs `futuretech_transmissioncenter` ($2,500) for token `futuretech_transmissioncenter`
  - `futuretech_transmissioncenter` needs `futuretech_constructionyard` ($5,000) for token `futuretech_constructionyard`
  - `futuretech_transmissioncenter` needs `futuretech_robotcontrolcenter` ($2,500) for token `futuretech_robotcontrolcenter`
  - `futuretech_robotcontrolcenter` needs `futuretech_constructionyard` ($5,000) for token `futuretech_constructionyard`
  - `futuretech_robotcontrolcenter` needs `futuretech_warpgate` ($2,000) for token `futuretech_warpgate`
  - `futuretech_warpgate` needs `futuretech_constructionyard` ($5,000) for token `futuretech_constructionyard`
  - `futuretech_warpgate` needs `futuretech_refinery` ($3,000) for token `futuretech_refinery`
  - `futuretech_refinery` needs `futuretech_constructionyard` ($5,000) for token `futuretech_constructionyard`
  - `futuretech_refinery` needs `futuretech_thermalpowerplant` ($800) for token `futuretech_thermalpowerplant`
  - `futuretech_thermalpowerplant` needs `futuretech_constructionyard` ($5,000) for token `futuretech_constructionyard`
  - buildings: futuretech_battlelab, futuretech_constructionyard, futuretech_hypercore, futuretech_launchpad, futuretech_refinery, futuretech_robotcontrolcenter, futuretech_thermalpowerplant, futuretech_transmissioncenter, futuretech_warpgate

### latinsyndicate_tortugatank
- `latinsyndicate_tortugatank`: `$28,500`
  - file: `mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/vehicles.yaml`
  - prerequisites: latinsyndicate_syndicatefactory, latinsyndicate_recyclingcenter
  - `latinsyndicate_tortugatank` needs `latinsyndicate_syndicatefactory` ($2,000) for token `latinsyndicate_syndicatefactory`
  - `latinsyndicate_syndicatefactory` needs `latinsyndicate_syndicateconstructionyard` ($5,000) for token `latinsyndicate_syndicateconstructionyard`
  - `latinsyndicate_syndicatefactory` needs `latinsyndicate_recyclingrefinery` ($3,000) for token `latinsyndicate_recyclingrefinery`
  - `latinsyndicate_recyclingrefinery` needs `latinsyndicate_syndicateconstructionyard` ($5,000) for token `latinsyndicate_syndicateconstructionyard`
  - `latinsyndicate_recyclingrefinery` needs `latinsyndicate_powerstation` ($1,000) for token `latinsyndicate_powerstation`
  - `latinsyndicate_powerstation` needs `latinsyndicate_syndicateconstructionyard` ($5,000) for token `latinsyndicate_syndicateconstructionyard`
  - `latinsyndicate_tortugatank` needs `latinsyndicate_recyclingcenter` ($5,000) for token `latinsyndicate_recyclingcenter`
  - `latinsyndicate_recyclingcenter` needs `latinsyndicate_syndicateconstructionyard` ($5,000) for token `latinsyndicate_syndicateconstructionyard`
  - `latinsyndicate_recyclingcenter` needs `latinsyndicate_defensebureau` ($5,000) for token `latinsyndicate_defensebureau`
  - `latinsyndicate_defensebureau` needs `latinsyndicate_syndicateconstructionyard` ($5,000) for token `latinsyndicate_syndicateconstructionyard`
  - `latinsyndicate_defensebureau` needs `latinsyndicate_spycenter` ($5,000) for token `latinsyndicate_spycenter`
  - `latinsyndicate_spycenter` needs `latinsyndicate_syndicateconstructionyard` ($5,000) for token `latinsyndicate_syndicateconstructionyard`
  - `latinsyndicate_spycenter` needs `latinsyndicate_latinempradar` ($2,500) for token `latinsyndicate_latinempradar`
  - `latinsyndicate_latinempradar` needs `latinsyndicate_syndicateconstructionyard` ($5,000) for token `latinsyndicate_syndicateconstructionyard`
  - `latinsyndicate_latinempradar` needs `latinsyndicate_recyclingrefinery` ($3,000) for token `latinsyndicate_recyclingrefinery`
  - `latinsyndicate_recyclingrefinery` needs `latinsyndicate_syndicateconstructionyard` ($5,000) for token `latinsyndicate_syndicateconstructionyard`
  - `latinsyndicate_recyclingrefinery` needs `latinsyndicate_powerstation` ($1,000) for token `latinsyndicate_powerstation`
  - `latinsyndicate_powerstation` needs `latinsyndicate_syndicateconstructionyard` ($5,000) for token `latinsyndicate_syndicateconstructionyard`
  - buildings: latinsyndicate_defensebureau, latinsyndicate_latinempradar, latinsyndicate_powerstation, latinsyndicate_recyclingcenter, latinsyndicate_recyclingrefinery, latinsyndicate_spycenter, latinsyndicate_syndicateconstructionyard, latinsyndicate_syndicatefactory

### ordos_deviatorartillery
- `ordos_deviatorartillery`: `$28,000`
  - file: `mods/cameo/ContentPacks/D2k/Ordos/yaml/vehicles.yaml`
  - prerequisites: ordos_heavy_vehicle_production, ordos_palace
  - `ordos_deviatorartillery` needs `ordos_heavyfactory` ($2,000) for token `ordos_heavy_vehicle_production`
  - `ordos_heavyfactory` needs `ordos_constructionyard` ($5,000) for token `ordos_constructionyard`
  - `ordos_heavyfactory` needs `ordos_refineryordos` ($3,000) for token `ordos_refineryordos`
  - `ordos_refineryordos` needs `ordos_constructionyard` ($5,000) for token `ordos_constructionyard`
  - `ordos_refineryordos` needs `ordos_windtrap` ($500) for token `ordos_windtrap`
  - `ordos_windtrap` needs `ordos_constructionyard` ($5,000) for token `ordos_constructionyard`
  - `ordos_deviatorartillery` needs `ordos_palace` ($10,000) for token `ordos_palace`
  - `ordos_palace` needs `ordos_constructionyard` ($5,000) for token `ordos_constructionyard`
  - `ordos_palace` needs `ordos_ixresearchcenter` ($5,000) for token `ordos_ixresearchcenter`
  - `ordos_ixresearchcenter` needs `ordos_constructionyard` ($5,000) for token `ordos_constructionyard`
  - `ordos_ixresearchcenter` needs `ordos_outpost` ($2,500) for token `ordos_outpost`
  - `ordos_outpost` needs `ordos_constructionyard` ($5,000) for token `ordos_constructionyard`
  - `ordos_outpost` needs `ordos_refineryordos` ($3,000) for token `ordos_refineryordos`
  - `ordos_refineryordos` needs `ordos_constructionyard` ($5,000) for token `ordos_constructionyard`
  - `ordos_refineryordos` needs `ordos_windtrap` ($500) for token `ordos_windtrap`
  - `ordos_windtrap` needs `ordos_constructionyard` ($5,000) for token `ordos_constructionyard`
  - buildings: ordos_constructionyard, ordos_heavyfactory, ordos_ixresearchcenter, ordos_outpost, ordos_palace, ordos_refineryordos, ordos_windtrap

### yuri_biotrooper
- `yuri_biotrooper`: `$26,600`
  - file: `mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/infantry.yaml`
  - prerequisites: yuri_barracks, yuri_cloningvats
  - `yuri_biotrooper` needs `yuri_barracks` ($1,000) for token `yuri_barracks`
  - `yuri_barracks` needs `yuri_constructionyard` ($5,000) for token `yuri_constructionyard`
  - `yuri_barracks` needs `yuri_bioreactor` ($600) for token `yuri_bioreactor`
  - `yuri_bioreactor` needs `yuri_constructionyard` ($5,000) for token `yuri_constructionyard`
  - `yuri_biotrooper` needs `yuri_cloningvats` ($5,000) for token `yuri_cloningvats`
  - `yuri_cloningvats` needs `yuri_constructionyard` ($5,000) for token `yuri_constructionyard`
  - `yuri_cloningvats` needs `yuri_battlelab` ($5,000) for token `yuri_battlelab`
  - `yuri_battlelab` needs `yuri_constructionyard` ($5,000) for token `yuri_constructionyard`
  - `yuri_battlelab` needs `yuri_psychicsensor` ($2,500) for token `yuri_psychicsensor`
  - `yuri_psychicsensor` needs `yuri_constructionyard` ($5,000) for token `yuri_constructionyard`
  - `yuri_psychicsensor` needs `yuri_slaveminer_deployed` ($2,500) for token `yuri_slaveminer_deployed`
  - `yuri_slaveminer_deployed` needs `yuri_constructionyard` ($5,000) for token `yuri_constructionyard`
  - `yuri_slaveminer_deployed` needs `yuri_bioreactor` ($600) for token `yuri_bioreactor`
  - `yuri_bioreactor` needs `yuri_constructionyard` ($5,000) for token `yuri_constructionyard`
  - `yuri_cloningvats` needs `yuri_lunarcommandcenter` ($5,000) for token `yuri_lunarcommandcenter`
  - `yuri_lunarcommandcenter` needs `yuri_constructionyard` ($5,000) for token `yuri_constructionyard`
  - `yuri_lunarcommandcenter` needs `yuri_battlelab` ($5,000) for token `yuri_battlelab`
  - `yuri_battlelab` needs `yuri_constructionyard` ($5,000) for token `yuri_constructionyard`
  - `yuri_battlelab` needs `yuri_psychicsensor` ($2,500) for token `yuri_psychicsensor`
  - `yuri_psychicsensor` needs `yuri_constructionyard` ($5,000) for token `yuri_constructionyard`
  - `yuri_psychicsensor` needs `yuri_slaveminer_deployed` ($2,500) for token `yuri_slaveminer_deployed`
  - `yuri_slaveminer_deployed` needs `yuri_constructionyard` ($5,000) for token `yuri_constructionyard`
  - `yuri_slaveminer_deployed` needs `yuri_bioreactor` ($600) for token `yuri_bioreactor`
  - `yuri_bioreactor` needs `yuri_constructionyard` ($5,000) for token `yuri_constructionyard`
  - buildings: yuri_barracks, yuri_battlelab, yuri_bioreactor, yuri_cloningvats, yuri_constructionyard, yuri_lunarcommandcenter, yuri_psychicsensor, yuri_slaveminer_deployed

### wc2_orcs_deathknight
- `wc2_orcs_deathknight`: `$15,000`
  - file: `mods/cameo/ContentPacks/Warcraft2/Orcs/yaml/infantry.yaml`
  - prerequisites: wc2_orcs_templeofthedamned
  - `wc2_orcs_deathknight` needs `wc2_orcs_templeofthedamned` ($10,000) for token `wc2_orcs_templeofthedamned`
  - `wc2_orcs_templeofthedamned` needs `wc2_orcs_greathall` ($5,000) for token `wc2_orcs_greathall`
  - `wc2_orcs_templeofthedamned` needs `wc2_orcs_greathall` ($5,000) for token `wc2_orcs_fortress`
  - buildings: wc2_orcs_greathall, wc2_orcs_templeofthedamned

### devastator
- `devastator`: `$18,000`
  - file: `mods/cameo/ContentPacks/D2k/Harkonnen/yaml/vehicles.yaml`
  - prerequisites: harkonnen_vehicle_production, heavy.harkonnen, research_centre
  - `devastator` needs `harkonnen_heavyfactory` ($2,000) for token `harkonnen_vehicle_production`
  - `harkonnen_heavyfactory` needs `harkonnen_constructionyard` ($5,000) for token `harkonnen_constructionyard`
  - `harkonnen_heavyfactory` needs `harkonnen_refinery` ($3,000) for token `harkonnen_refinery`
  - `harkonnen_refinery` needs `harkonnen_constructionyard` ($5,000) for token `harkonnen_constructionyard`
  - `harkonnen_refinery` needs `harkonnen_windtrap` ($500) for token `harkonnen_windtrap`
  - `harkonnen_windtrap` needs `harkonnen_constructionyard` ($5,000) for token `harkonnen_constructionyard`
  - `devastator` needs `harkonnen_heavyfactory` ($2,000) for token `heavy.harkonnen`
  - `harkonnen_heavyfactory` needs `harkonnen_constructionyard` ($5,000) for token `harkonnen_constructionyard`
  - `harkonnen_heavyfactory` needs `harkonnen_refinery` ($3,000) for token `harkonnen_refinery`
  - `harkonnen_refinery` needs `harkonnen_constructionyard` ($5,000) for token `harkonnen_constructionyard`
  - `harkonnen_refinery` needs `harkonnen_windtrap` ($500) for token `harkonnen_windtrap`
  - `harkonnen_windtrap` needs `harkonnen_constructionyard` ($5,000) for token `harkonnen_constructionyard`
  - `devastator` needs `harkonnen_ixresearchcenter` ($5,000) for token `research_centre`
  - `harkonnen_ixresearchcenter` needs `harkonnen_constructionyard` ($5,000) for token `harkonnen_constructionyard`
  - `harkonnen_ixresearchcenter` needs `harkonnen_outpost` ($2,500) for token `harkonnen_outpost`
  - `harkonnen_outpost` needs `harkonnen_constructionyard` ($5,000) for token `harkonnen_constructionyard`
  - `harkonnen_outpost` needs `harkonnen_refinery` ($3,000) for token `harkonnen_refinery`
  - `harkonnen_refinery` needs `harkonnen_constructionyard` ($5,000) for token `harkonnen_constructionyard`
  - `harkonnen_refinery` needs `harkonnen_windtrap` ($500) for token `harkonnen_windtrap`
  - `harkonnen_windtrap` needs `harkonnen_constructionyard` ($5,000) for token `harkonnen_constructionyard`
  - buildings: harkonnen_constructionyard, harkonnen_heavyfactory, harkonnen_ixresearchcenter, harkonnen_outpost, harkonnen_refinery, harkonnen_windtrap


## Per-faction maxima

| faction | buildable units | median chain | max chain | max unit(s) |
|---|---|---:|---:|---|
| D2k/Atreides | 2 | 10,500 | 10,500 | `atreides_mobileconstructionvehicle`, `combat_tank.atreides` |
| D2k/Harkonnen | 4 | 18,000 | 18,000 | `devastator`, `missile_tank` |
| D2k/Ixian | 35 | 13,000 | 18,000 | `duelist_tank.ixian`, `ixian_ixmissiletank`, `ixian_ixprojector` |
| D2k/Ordos | 44 | 12,500 | 28,000 | `ordos_deviatorartillery`, `ordos_deviatortank` |
| D2k/Shared | 17 | 0 | 0 | `carryall`, `carryall.paradrop`, `carryall.reinforce` |
| RedAlert/Allies | 34 | 12,500 | 18,000 | `ra1_allies_chronotank`, `ra1_allies_mobilegapgenerator`, `ra1_allies_mobileradarjammer` |
| RedAlert/Japan | 43 | 14,250 | 18,250 | `japan_ballista`, `japan_exorcistoitank`, `japan_oitank` |
| RedAlert/Shared | 16 | 800 | 3,000 | `japan_japanesecarrier`, `japan_japanesespeedboat`, `japan_yamatobattleship` |
| RedAlert/Soviets | 79 | 12,500 | 18,000 | `ra1_soviets_grad`, `ra1_soviets_madtank`, `ra1_soviets_mammothtank` |
| RedAlert2/Allies | 48 | 15,800 | 20,800 | `ra2_allies_upgrade_chronoengine` |
| RedAlert2/Shared | 42 | 0 | 3,000 | `ra2carrier`, `ra2dest`, `ra2dlph` |
| RedAlert2/Soviets | 38 | 10,600 | 30,100 | `ra2_soviets_upgrade_kirovatomicbombs` |
| RedAlert2/Yuri | 48 | 15,600 | 26,600 | `yuri_biotrooper` |
| RedAlert2Mod/AsianAlliance | 53 | 12,750 | 24,250 | `tsun.asian`, `up_tsunami.asian` |
| RedAlert2Mod/Consortium | 41 | 13,500 | 25,500 | `steelconsortium_cloudbreaker` |
| RedAlert2Mod/FutureTech | 33 | 16,800 | 29,800 | `futuretech_harbingergunship` |
| RedAlert2Mod/Naxis | 57 | 13,300 | 20,300 | `muboat.nax`, `nax_bitsmark` |
| RedAlert2Mod/SchwarzerMond | 35 | 14,300 | 19,300 | `schwarzermond_blackbomb`, `schwarzermond_corruptorpiercer`, `schwarzermond_dieglocke` |
| RedAlert2Mod/Syndicate | 42 | 13,000 | 28,500 | `latinsyndicate_tortugatank` |
| RedAlert2Mod/TKM | 51 | 12,900 | 19,900 | `tkm_viper` |
| StarCraft/Protoss | 43 | 9,000 | 19,000 | `protoss_carrier`, `protoss_starshipsovereign`, `protoss_upgrade_airarmorlevel2` |
| StarCraft/Terran | 49 | 10,500 | 14,500 | `terran_battlecruiser`, `terran_ghost`, `terran_jimraynor` |
| StarCraft/Zerg | 41 | 6,500 | 12,000 | `zerg_infestedterranbomber` |
| TiberianDawn/GDI | 45 | 11,500 | 24,000 | `gdicarrier` |
| TiberianDawn/Nod | 43 | 12,500 | 27,500 | `td_nod_chemicalssmlauncher`, `td_nod_venom` |
| TiberianDawn/Shared | 1 | 0 | 0 | `E6` |
| TiberianSun/CABAL | 62 | 6,800 | 22,800 | `cabal_coredefender`, `cabal_widow` |
| TiberianSun/Forgotten | 56 | 7,400 | 12,600 | `forgotten_chemicalmammothtank`, `forgotten_experimentalmammothtank`, `forgotten_flametank` |
| TiberianSun/GDI | 39 | 8,000 | 23,000 | `ts_gdi_kodiakcommandship` |
| TiberianSun/Nod | 30 | 8,900 | 16,600 | `ts_nod_mobilestealthgenerator` |
| Warcraft2/Humans | 48 | 9,700 | 19,700 | `wc2_humans_upgrade_blizzard`, `wc2_humans_upgrade_polymorph`, `wc2_humans_upgrade_slow` |
| Warcraft2/Orcs | 40 | 9,500 | 19,500 | `wc2_orcs_upgrade_deathanddecay`, `wc2_orcs_upgrade_haste`, `wc2_orcs_upgrade_raisedead` |
| other | 72 | 10,750 | 18,200 | `wc2_human_battleship` |

## Observations

- The TD Nod Laser Trooper chain is **$27,000**, not $32,000. The $5,000 inflation was a cross-faction GDI Construction Yard that the corrected resolver removes.
- The RA2 Soviets `kirovatomicbombs` promotion upgrade has the highest corrected chain at **$30,100**, driven by the Industrial Plant, Nuclear Missile Silo, Battle Lab, and Radar.
- FutureTech top units cluster around **$28,300–$29,800** and depend on the Warp Gate, Transmission Center, Battle Lab, and Robot Control Center.
- The T3 tech-center bucket contains exactly one unit, `wc2_orcs_deathknight`, with a real $15,000 chain (Great Hall $5,000 + Temple of the Damned $10,000).
- T2 radar now lands at ~0.80, not the old fixed-ladder 1.0, confirming radar is a real tier.
- The rational form and the power law still agree within ~0.02 at every measured tier; the rational form is simpler to explain and calibrate.

## Recommendation

Adopt the rational form `f(C) = 1 / (1 + (C - B) / S)` with B = T1 median chain and S = (T4 median chain - B). Use the corrected medians above for the next `tier` term calibration.