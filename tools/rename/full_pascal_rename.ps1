# Complete PascalCase rename for all remaining lowercase-starting inherits templates
# Order matters: longer/more specific patterns first to avoid partial matches

$renameMap = [ordered]@{
    # === Remaining dotted template (advancewars, not loaded but fix for consistency) ===
    'default_angry_mob.aw' = 'DefaultAngryMobAW'

    # === RA2 Soviet camelCase ===
    'ra2sovietshockTrooperTraining' = 'RA2SovietsShockTrooperTraining'
    'ra2sovietsArmorPlatings' = 'RA2SovietsArmorPlatings'
    'ra2sovietsConscription' = 'RA2SovietsConscription'
    'ra2sovietsFireShells' = 'RA2SovietsFireShells'
    'ra2sovietsInfantryConditioning' = 'RA2SovietsInfantryConditioning'
    'ra2sovietsRadShells' = 'RA2SovietsRadShells'
    'ra2sovietsReactiveArmor' = 'RA2SovietsReactiveArmor'
    'ra2sovietsTeslaDischargeArmor' = 'RA2SovietsTeslaDischargeArmor'
    'ra2sovietsTeslaShells' = 'RA2SovietsTeslaShells'

    # === CABAL snake_case ===
    'cabal_upgrade_overchargedservos_vehicle' = 'CabalUpgradeOverchargedServosVehicle'
    'cabal_upgrade_backupsystems' = 'CabalUpgradeBackupSystems'
    'cabal_upgrade_cyberneticplating' = 'CabalUpgradeCyberneticPlating'
    'cabal_upgrade_darkarmament' = 'CabalUpgradeDarkArmament'
    'cabal_upgrade_handof' = 'CabalUpgradeHandOf'
    'cabal_upgrade_mobilitymatrix' = 'CabalUpgradeMobilityMatrix'
    'cabal_upgrade_neuraluplink' = 'CabalUpgradeNeuralUplink'
    'cabal_upgrade_neutronnuclearcatalyst' = 'CabalUpgradeNeutronNuclearCatalyst'
    'cabal_upgrade_overchargedservos' = 'CabalUpgradeOverchargedServos'
    'cabal_upgrade_radarhack' = 'CabalUpgradeRadarHack'
    'cabal_upgrade_reclamationprotocols' = 'CabalUpgradeReclamationProtocols'
    'cabal_upgrade_reinforcedchassis' = 'CabalUpgradeReinforcedChassis'

    # === SOW (Outpost 2) ===
    'sow_advancer_upgradebeam' = 'SowAdvancerUpgradeBeam'
    'sow_antiair_mk1' = 'SowAntiAirMk1'
    'sow_antiair' = 'SowAntiAir'
    'sow_cannon' = 'SowCannon'
    'sow_flame' = 'SowFlame'
    'sow_no_cd' = 'SowNoCD'
    'sow_power_boost' = 'SowPowerBoost'
    'sow_siege' = 'SowSiege'
    'sow_special' = 'SowSpecial'
    'sow_unit_lowpower' = 'SowUnitLowPower'
    'sow_upgradeable' = 'SowUpgradeable'
    'sowgeneric' = 'SowGeneric'
    'sowprodupgrade' = 'SowProdUpgrade'
    'sowresize_walker' = 'SowResizeWalker'
    'sowresize' = 'SowResize'

    # === TKM ===
    'tkm_upgrade_heavytitanplating' = 'TKMUpgradeHeavyTitanPlating'
    'tkm_upgrade_incendiaryrocketsupgrade' = 'TKMUpgradeIncendiaryRocketsUpgrade'
    'tkm_upgrade_infantryupgrade' = 'TKMUpgradeInfantryUpgrade'
    'tkm_upgrade_natoarsenalupgrade' = 'TKMUpgradeNATOArsenalUpgrade'
    'tkm_upgrade_pointdefensesystem' = 'TKMUpgradePointDefenseSystem'
    'tkm_upgrade_semiautoriflesupgrade' = 'TKMUpgradeSemiAutoRiflesUpgrade'
    'tkm_upgrade_titanarmorpiercingbulletsupgrade' = 'TKMUpgradeTitanArmorPiercingBulletsUpgrade'
    'tkm_upgrade_twinrocketsupgrade' = 'TKMUpgradeTwinRocketsUpgrade'

    # === USA ===
    'usa_composite_armor_upgrade' = 'USACompositeArmorUpgrade'
    'usa_switchable_upgrades' = 'USASwitchableUpgrades'

    # === Outpost 2 base templates ===
    'op2base_factory_vehicle' = 'OP2BaseFactoryVehicle'
    'op2base_lab_advanced' = 'OP2BaseLabAdvanced'
    'op2base_lab_standard' = 'OP2BaseLabStandard'
    'op2base_light_tower' = 'OP2BaseLightTower'
    'op2base_mine_common' = 'OP2BaseMineCommon'
    'op2base_smelter_common' = 'OP2BaseSmelterCommon'
    'op2base_smelter_rare' = 'OP2BaseSmelterRare'
    'op2base_solararray' = 'OP2BaseSolarArray'
    'op2base_spaceport' = 'OP2BaseSpaceport'
    'op2base_storage_common' = 'OP2BaseStorageCommon'
    'op2base_agridome' = 'OP2BaseAgridome'
    'op2base_basic_lab' = 'OP2BaseBasicLab'
    'op2base_garage' = 'OP2BaseGarage'
    'op2base_nursery' = 'OP2BaseNursery'
    'op2base_residence' = 'OP2BaseResidence'
    'op2base_university' = 'OP2BaseUniversity'
    'op2base_tokamak' = 'OP2BaseTokamak'
    'op2base_rcc' = 'OP2BaseRCC'
    'op2base_gorf' = 'OP2BaseGorf'
    'op2base_dirt' = 'OP2BaseDirt'
    'op2_engineer' = 'OP2Engineer'
    'op2_supplier' = 'OP2Supplier'

    # === RA1 Allies ===
    'ra1_allies_alliedrocketsoldier' = 'RA1AlliesAlliedRocketSoldier'
    'ra1_allies_rifleinfantry' = 'RA1AlliesRifleInfantry'

    # === Generic templates ===
    'researched_upgrade_template' = 'ResearchedUpgradeTemplate'
    'promotion_upgrade_template' = 'PromotionUpgradeTemplate'
    'upgrade_template' = 'UpgradeTemplate'
    'unit_upgrade_template' = 'UnitUpgradeTemplate'
    'team_upgrade_proxy_actor' = 'TeamUpgradeProxyActor'
    'team_upgrade_template' = 'TeamUpgradeTemplate'
    'tech_upgrade_template' = 'TechUpgradeTemplate'
    'doctrine_template' = 'DoctrineTemplate'
    'satelliteprotection_upgrade' = 'SatelliteProtectionUpgrade'
    'construction_yard' = 'ConstructionYard'
    'heavy_factory' = 'HeavyFactory'
    'high_tech_factory' = 'HighTechFactory'
    'light_factory' = 'LightFactory'
    'combat_tank' = 'CombatTank'
    'research_centre' = 'ResearchCentre'
    'turret_destroyed' = 'TurretDestroyed'
    'walker_stomping' = 'WalkerStomping'
    'wind_trap' = 'WindTrap'
    'default_angry_mob' = 'DefaultAngryMob'
    'default_alien_mob' = 'DefaultAlienMob'

    # === Single-word lowercase templates ===
    'starport' = 'Starport'
    'refinery' = 'Refinery'
    'concrete' = 'Concrete'
    'wall' = 'Wall'

    # === Faction-specific lowercase templates ===
    'aadeploytargeting' = 'AADeployTargeting'
    'actiblizzprotoss' = 'ActiBlizzProtoss'
    'd2k_silo' = 'D2KSilo'
    'deploytargeting' = 'DeployTargeting'
    'dune2concrete' = 'Dune2Concrete'
    'generalsdrone' = 'GeneralsDrone'
    'susaintelligence' = 'SUSAIntelligence'
    'upberezkacloakupgrade' = 'UPBerezkaCloakUpgrade'
    'upschnucleartanks' = 'UPSCHNuclearTanks'
    'upsusafocusingcrystal' = 'UPSUSAFocusingCrystal'

    # === Single-letter prefix templates (oCannon, oMG, etc.) ===
    'oCannon' = 'OCannon'
    'oMissile' = 'OMissile'
    'oRocket' = 'ORocket'
    'oMG' = 'OMG'
}

# Find all YAML files under mods/cameo
$files = @()
$files += (Get-ChildItem -Path "mods/cameo/rules" -Filter "*.yaml" -Recurse | ForEach-Object { $_.FullName })
$files += (Get-ChildItem -Path "mods/cameo/ContentPacks" -Filter "*.yaml" -Recurse | ForEach-Object { $_.FullName })
$files += (Get-ChildItem -Path "mods/cameo/weapons" -Filter "*.yaml" -Recurse | ForEach-Object { $_.FullName })
$files += (Get-ChildItem -Path "mods/cameo/sequences" -Filter "*.yaml" -Recurse | ForEach-Object { $_.FullName })
$files += (Get-ChildItem -Path "mods/cameo/ai" -Filter "*.yaml" -Recurse | ForEach-Object { $_.FullName })

$totalChanges = 0
$modifiedFiles = 0

foreach ($file in $files) {
    $content = Get-Content $file -Raw -Encoding UTF8
    if ($null -eq $content) { continue }
    $original = $content
    $fileChanges = 0

    foreach ($oldName in $renameMap.Keys) {
        $newName = $renameMap[$oldName]
        $pattern = "\^$([regex]::Escape($oldName))\b"
        $matches = [regex]::Matches($content, $pattern)
        if ($matches.Count -gt 0) {
            $content = [regex]::Replace($content, $pattern, "^$newName")
            $fileChanges += $matches.Count
        }
    }

    if ($content -ne $original) {
        [System.IO.File]::WriteAllText($file, $content, [System.Text.UTF8Encoding]::new($false))
        $totalChanges += $fileChanges
        $modifiedFiles++
        Write-Output "Modified: $(Split-Path $file -Leaf) ($fileChanges replacements)"
    }
}

Write-Output ""
Write-Output "Total: $totalChanges replacements across $modifiedFiles files"
