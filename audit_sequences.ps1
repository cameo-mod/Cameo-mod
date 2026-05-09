param([string]$SeqFile, [string]$Label)

$modDir = "C:\Users\Blackrobe\repo\Cameo-mod\mods\cameo"
$rulesDir = "$modDir\rules"
$weaponsDir = "$modDir\weapons"

# Collect all relevant names from rules AND weapons files:
#   - top-level actor/weapon names
#   - Image: values (actors and projectiles can reference a different sequence name)
$allActors = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
$scanDirs = @($rulesDir, $weaponsDir)
foreach ($dir in $scanDirs) {
    Get-ChildItem $dir -Filter "*.yaml" | ForEach-Object {
        Get-Content $_.FullName | ForEach-Object {
            # Top-level names (no leading whitespace)
            if ($_ -match '^([A-Za-z0-9\^][^:]*):') {
                $null = $allActors.Add($Matches[1].Trim())
            }
            # Any property whose name contains Image or ends in Sequence/Sequences
            # Covers: Image, MissileImage, TrailImage, ImageByFullness, SmokeImage,
            #         Sequence, TrailSequences, SmokeSequences, IdleSequences, etc.
            # Excludes *SequencePalette (palette names, not sequence names)
            if ($_ -match '^\s+\w*(?:Image)\w*:\s+(.+)' -or
                ($_ -match '^\s+\w+Sequences?:\s+(.+)' -and $_ -notmatch 'Palette:')) {
                # Split comma-separated values (e.g. ImageByFullness, SmokeSequences)
                $Matches[1] -split ',' | ForEach-Object {
                    $null = $allActors.Add($_.Trim())
                }
            }
        }
    }
}
# Also add lowercase versions of all names (default image = lowercase actor name)
$extraLower = @($allActors) | ForEach-Object { $_.ToLowerInvariant() }
foreach ($n in $extraLower) { $null = $allActors.Add($n) }

# Also scan all OTHER sequence files for cross-references (an actor in misc.yaml may use a seq defined in ra2)
$allSeqActors = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
$seqDir = "C:\Users\Blackrobe\repo\Cameo-mod\mods\cameo\sequences"
Get-ChildItem $seqDir -Filter "*.yaml" | Where-Object { $_.FullName -ne (Resolve-Path $SeqFile) } | ForEach-Object {
    Get-Content $_.FullName | Where-Object { $_ -match '^[A-Za-z0-9]' -and $_ -match ':' } |
        ForEach-Object { $null = $allSeqActors.Add((($_ -split ':')[0].Trim())) }
}

# Get sequence definitions from target file
$seqLines = Get-Content $SeqFile
$seqActors = $seqLines | Where-Object { $_ -match '^[A-Za-z0-9]' -and $_ -match ':' } |
    ForEach-Object { ($_ -split ':')[0].Trim() }

# Also collect all Inherits: values within this sequence file itself —
# a block may be used only as a parent template by other blocks in the same file.
$seqInherits = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
$seqLines | Where-Object { $_ -match '^\s+Inherits(?:@\S+)?:\s+(\S+)' } |
    ForEach-Object { $null = $seqInherits.Add($Matches[1].Trim()) }

$unused = $seqActors | Where-Object {
    -not $allActors.Contains($_) -and -not $allSeqActors.Contains($_) -and -not $seqInherits.Contains($_)
} | Sort-Object

Write-Host "=== $Label ===" -ForegroundColor Cyan
Write-Host "Sequence definitions: $($seqActors.Count)  |  Unused: $($unused.Count)"
Write-Host ""
$unused | ForEach-Object { Write-Host "  $_" }
