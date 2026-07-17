# Phase 1: Rename sprite/asset files
Write-Output "=== Renaming asset files ==="

# schwarzer_mond_ -> schwarzermond_
Get-ChildItem -Path "mods\cameo" -Recurse -File | Where-Object { $_.Name -match "schwarzer_mond" } | ForEach-Object {
    $newName = $_.Name -replace "schwarzer_mond", "schwarzermond"
    $newPath = Join-Path $_.DirectoryName $newName
    Rename-Item $_.FullName $newPath
    Write-Output "  $($_.Name) -> $newName"
}

# steel_consortium_ -> steelconsortium_
Get-ChildItem -Path "mods\cameo" -Recurse -File | Where-Object { $_.Name -match "steel_consortium" } | ForEach-Object {
    $newName = $_.Name -replace "steel_consortium", "steelconsortium"
    $newPath = Join-Path $_.DirectoryName $newName
    Rename-Item $_.FullName $newPath
    Write-Output "  $($_.Name) -> $newName"
}

# latin_syndicate_ -> latinsyndicate_
Get-ChildItem -Path "mods\cameo" -Recurse -File | Where-Object { $_.Name -match "latin_syndicate" } | ForEach-Object {
    $newName = $_.Name -replace "latin_syndicate", "latinsyndicate"
    $newPath = Join-Path $_.DirectoryName $newName
    Rename-Item $_.FullName $newPath
    Write-Output "  $($_.Name) -> $newName"
}

# Sidebar files
if (Test-Path "mods\cameo\uibits\sidebar_RA2Soviets.png") {
    Rename-Item "mods\cameo\uibits\sidebar_RA2Soviets.png" "sidebar_ra2soviets.png"
    Write-Output "  sidebar_RA2Soviets.png -> sidebar_ra2soviets.png"
}
if (Test-Path "mods\cameo\uibits\sidebar_modjapan.png") {
    Rename-Item "mods\cameo\uibits\sidebar_modjapan.png" "sidebar_japan.png"
    Write-Output "  sidebar_modjapan.png -> sidebar_japan.png"
}

# Phase 2: Text replacements in YAML and FTL files
Write-Output "`n=== Replacing text in YAML and FTL files ==="

$files = Get-ChildItem -Path "mods\cameo" -Recurse -Include "*.yaml", "*.ftl" -File
foreach ($f in $files) {
    $c = Get-Content $f.FullName -Raw
    $modified = $false
    
    # Check if any pattern matches
    if ($c -match "schwarzer_mond|steel_consortium|latin_syndicate|orcs_|humans_|lnaxis|orc2|human2|modjapan|RA2Soviets|faction_ts_gdi|faction_ts_nod") {
        # Order matters! Do actor prefix renames first, then faction internal names
        $c = $c -replace "schwarzer_mond", "schwarzermond"
        $c = $c -replace "steel_consortium", "steelconsortium"
        $c = $c -replace "latin_syndicate", "latinsyndicate"
        $c = $c -replace "orcs_", "warcraft_orcs_"
        $c = $c -replace "humans_", "warcraft_humans_"
        $c = $c -replace "lnaxis", "schwarzermond"
        $c = $c -replace "orc2", "warcraft_orcs"
        $c = $c -replace "human2", "warcraft_humans"
        $c = $c -replace "modjapan", "japan"
        $c = $c -replace "RA2Soviets", "ra2soviets"
        $c = $c -replace "faction_ts_gdi", "faction_tsgdi"
        $c = $c -replace "faction_ts_nod", "faction_tsnod"
        $modified = $true
    }
    
    if ($modified) {
        [System.IO.File]::WriteAllText($f.FullName, $c)
        Write-Output "  Modified: $($f.FullName)"
    }
}

Write-Output "`n=== Done ==="
