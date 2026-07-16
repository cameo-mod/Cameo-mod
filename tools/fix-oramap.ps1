param([string]$oramapPath)
Add-Type -AssemblyName System.IO.Compression.FileSystem

# Read the oramap
$zip = [System.IO.Compression.ZipFile]::OpenRead($oramapPath)
$entry = $zip.Entries | Where-Object { $_.Name -eq "map.yaml" }
$reader = New-Object System.IO.StreamReader($entry.Open())
$content = $reader.ReadToEnd()
$reader.Close()
$zip.Dispose()

$modified = $false

# Faction InternalName renames (old -> new)
$renames = @(
    @("warcraft_humans", "wc2_humans"),
    @("warcraft_orcs", "wc2_orcs"),
    @("asian_alliance", "asianalliance"),
    @("steel_consortium", "steelconsortium"),
    @("latin_syndicate", "latinsyndicate")
)

# Also fix old InternalNames used as faction references in map.yaml
# These are word-boundary replacements to avoid partial matches.
# CASE-SENSITIVE (-creplace) is mandatory: the old internal names are all
# lowercase, while PlayerReference FIELD KEYS ("Allies:", "Enemies:") and
# display player names ("Allies", "GDI") are capitalized. A case-insensitive
# replace corrupted shellmap_v2's "Allies:" alliance fields into
# "ra1_allies:" (invalid field -> map excluded -> "No valid shellmaps
# available", 2026-07-16). Display names are NOT internal ids and must
# never be renamed here (lua scripts reference them).
$internalRenames = @(
    @("\bgdi\b", "td_gdi"),
    @("\bnod\b", "td_nod"),
    @("\ballies\b", "ra1_allies"),
    @("\bsoviet\b", "ra1_soviets"),
    @("\bsoviets\b", "ra1_soviets"),
    @("\bmodjapan\b", "japan"),
    @("\btsgdi\b", "ts_gdi"),
    @("\btsnod\b", "ts_nod"),
    @("\bra2allies\b", "ra2_allies"),
    @("\bra2soviets\b", "ra2_soviets"),
    @("\bconsortium\b", "steelconsortium"),
    @("\bsyndicate\b", "latinsyndicate")
)

foreach ($pair in $renames) {
    $old = $pair[0]
    $new = $pair[1]
    if ($content -cmatch [regex]::Escape($old)) {
        $content = $content -creplace [regex]::Escape($old), $new
        $modified = $true
        Write-Output "  Replaced $old -> $new"
    }
}

foreach ($pair in $internalRenames) {
    $pattern = $pair[0]
    $new = $pair[1]
    if ($content -cmatch $pattern) {
        $content = $content -creplace $pattern, $new
        $modified = $true
        Write-Output "  Replaced $pattern -> $new"
    }
}

# Legacy: ra1_soviet_ -> ra1_soviets_ (singular to plural)
if ($content -match "ra1_soviet_") {
    $content = $content -replace "ra1_soviet_", "ra1_soviets_"
    $modified = $true
    Write-Output "  Replaced ra1_soviet_ -> ra1_soviets_"
}

if ($modified) {
    # Create temp directory
    $tempDir = Join-Path $env:TEMP "oramap_fix_$(Get-Random)"
    New-Item -ItemType Directory -Path $tempDir -Force | Out-Null

    # Extract all entries
    $zip = [System.IO.Compression.ZipFile]::OpenRead($oramapPath)
    foreach ($e in $zip.Entries) {
        $outPath = Join-Path $tempDir $e.FullName
        $dir = Split-Path $outPath -Parent
        if (!(Test-Path $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }
        if (!$e.FullName.EndsWith("/")) {
            [System.IO.Compression.ZipFileExtensions]::ExtractToFile($e, $outPath, $true)
        }
    }
    $zip.Dispose()

    # Write modified map.yaml
    $mapYamlPath = Join-Path $tempDir "map.yaml"
    [System.IO.File]::WriteAllText($mapYamlPath, $content, (New-Object System.Text.UTF8Encoding $false))

    # Repack
    $tempZip = $oramapPath + ".tmp"
    if (Test-Path $tempZip) { Remove-Item $tempZip -Force }
    $newZip = [System.IO.Compression.ZipFile]::Open($tempZip, [System.IO.Compression.ZipArchiveMode]::Create)
    $files = Get-ChildItem $tempDir -Recurse -File
    foreach ($f in $files) {
        $relPath = $f.FullName.Substring($tempDir.Length + 1).Replace("\", "/")
        [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($newZip, $f.FullName, $relPath) | Out-Null
    }
    $newZip.Dispose()

    # Replace original
    Copy-Item $tempZip $oramapPath -Force
    Remove-Item $tempZip -Force
    Remove-Item $tempDir -Recurse -Force

    Write-Output "  Updated: $oramapPath"
} else {
    Write-Output "  No changes needed: $oramapPath"
}
