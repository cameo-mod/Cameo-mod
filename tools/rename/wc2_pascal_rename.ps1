# WC2/WC1 inherits template rename map
# Order matters: longer/more specific patterns first to avoid partial matches
# Key = old name (without ^), Value = new name (without ^)

$renameMap = [ordered]@{
    # WC2 Humans upgrade templates (wc2_humans_ prefix)
    'wc2_humans_upgrade_armorstrength' = 'WC2HumansUpgradeArmorStrength'
    'wc2_humans_upgrade_arrowstrength' = 'WC2HumansUpgradeArrowStrength'
    'wc2_humans_upgrade_ballistastrength' = 'WC2HumansUpgradeBallistaStrength'
    'wc2_humans_upgrade_cannondamage' = 'WC2HumansUpgradeCannonDamage'
    'wc2_humans_upgrade_paladin' = 'WC2HumansUpgradePaladin'
    'wc2_humans_upgrade_rangerlongbow' = 'WC2HumansUpgradeRangerLongbow'
    'wc2_humans_upgrade_rangermarksman' = 'WC2HumansUpgradeRangerMarksman'
    'wc2_humans_upgrade_rangerscouting' = 'WC2HumansUpgradeRangerScouting'
    'wc2_humans_upgrade_ranger' = 'WC2HumansUpgradeRanger'
    'wc2_humans_upgrade_swordstrength' = 'WC2HumansUpgradeSwordStrength'

    # WC2 Humans spell/status templates (wc2_h_ prefix -> WC2Humans)
    'wc2_h_invisibility_status' = 'WC2HumansInvisibilityStatus'
    'wc2_h_mage_blizzard' = 'WC2HumansMageBlizzard'
    'wc2_h_mage_polymorph' = 'WC2HumansMagePolymorph'
    'wc2_h_mage_slow' = 'WC2HumansMageSlow'
    'wc2_h_paladin_exorcism' = 'WC2HumansPaladinExorcism'
    'wc2_h_paladin_healing' = 'WC2HumansPaladinHealing'
    'wc2_h_polymorph_status' = 'WC2HumansPolymorphStatus'
    'wc2_h_slow_status' = 'WC2HumansSlowStatus'
    'wc2_h_str_navyshield' = 'WC2HumansStrNavyshield'

    # WC2 Orcs upgrade templates (wc2_orcs_ prefix)
    'wc2_orcs_upgrade_armorstrength' = 'WC2OrcsUpgradeArmorStrength'
    'wc2_orcs_upgrade_axestrength' = 'WC2OrcsUpgradeAxeStrength'
    'wc2_orcs_upgrade_berserkerlightaxes' = 'WC2OrcsUpgradeBerserkerLightAxes'
    'wc2_orcs_upgrade_berserkerregeneration' = 'WC2OrcsUpgradeBerserkerRegeneration'
    'wc2_orcs_upgrade_berserkerscouting' = 'WC2OrcsUpgradeBerserkerScouting'
    'wc2_orcs_upgrade_berserker' = 'WC2OrcsUpgradeBerserker'
    'wc2_orcs_upgrade_cannondamage' = 'WC2OrcsUpgradeCannonDamage'
    'wc2_orcs_upgrade_catapultstrength' = 'WC2OrcsUpgradeCatapultStrength'
    'wc2_orcs_upgrade_ogremage' = 'WC2OrcsUpgradeOgreMage'
    'wc2_orcs_upgrade_throwingaxestrength' = 'WC2OrcsUpgradeThrowingAxeStrength'

    # WC2 Orcs spell/status templates (wc2_o_ prefix -> WC2Orcs)
    'wc2_o_bloodlust_status' = 'WC2OrcsBloodlustStatus'
    'wc2_o_deathknight_deathanddecay' = 'WC2OrcsDeathKnightDeathAndDecay'
    'wc2_o_deathknight_haste' = 'WC2OrcsDeathKnightHaste'
    'wc2_o_deathknight_raisedead' = 'WC2OrcsDeathKnightRaiseDead'
    'wc2_o_haste_status' = 'WC2OrcsHasteStatus'
    'wc2_o_ogremage_bloodlust' = 'WC2OrcsOgreMageBloodlust'
    'wc2_o_ogremage_runes' = 'WC2OrcsOgreMageRunes'
    'wc2_o_str_navyshield' = 'WC2OrcsStrNavyshield'

    # WC2 shared templates (wc2_ prefix, no faction)
    'wc2_knight_paladin_attacks_spells' = 'WC2KnightPaladinAttacksSpells'
    'wc2_ogre_ogremage_attacks_spells' = 'WC2OgreOgreMageAttacksSpells'
    'wc2_oil_platform' = 'WC2OilPlatform'
    'wc2_oil_refinery' = 'WC2OilRefinery'
    'wc2_oil_tanker' = 'WC2OilTanker'
    'wc2_lumber_mill' = 'WC2LumberMill'
    'wc2_watch_tower' = 'WC2WatchTower'
    'wc2_status_icons' = 'WC2StatusIcons'
    'wc2_airscout' = 'WC2AirScout'
    'wc2_barracks' = 'WC2Barracks'
    'wc2_battleship' = 'WC2Battleship'
    'wc2_blacksmith' = 'WC2Blacksmith'
    'wc2_church' = 'WC2Church'
    'wc2_demolitioner' = 'WC2Demolitioner'
    'wc2_destroyer' = 'WC2Destroyer'
    'wc2_engineer' = 'WC2Engineer'
    'wc2_farm' = 'WC2Farm'
    'wc2_foundry' = 'WC2Foundry'
    'wc2_goldmine' = 'WC2Goldmine'
    'wc2_inventor' = 'WC2Inventor'
    'wc2_mage' = 'WC2Mage'
    'wc2_mcv' = 'WC2MCV'
    'wc2_nest' = 'WC2Nest'
    'wc2_peasant' = 'WC2Peasant'
    'wc2_shipyard' = 'WC2Shipyard'
    'wc2_stables' = 'WC2Stables'
    'wc2_submarine' = 'WC2Submarine'
    'wc2_supplier' = 'WC2Supplier'
    'wc2_temple' = 'WC2Temple'
    'wc2_transport' = 'WC2Transport'

    # WC1 Humans templates (wc_h_ prefix -> WCHumans)
    'wc_h_horses' = 'WCHumansHorses'
    'wc_h_str_arrow' = 'WCHumansStrArrow'
    'wc_h_str_shield' = 'WCHumansStrShield'
    'wc_h_str_sword' = 'WCHumansStrSword'
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
        # Match ^oldName as a template reference (in Inherits lines and definitions)
        # Use word boundary to avoid partial matches
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
