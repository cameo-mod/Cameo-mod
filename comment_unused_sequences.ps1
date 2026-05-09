param(
    [string]$SeqFile,
    [string[]]$UnusedActors
)

$unusedSet = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
foreach ($a in $UnusedActors) { $null = $unusedSet.Add($a) }

$lines = Get-Content $SeqFile
$output = [System.Collections.Generic.List[string]]::new()

$i = 0
$skippedBlocks = 0
while ($i -lt $lines.Count) {
    $line = $lines[$i]

    # Detect a top-level key line (no leading whitespace, not a comment, not empty)
    if ($line -match '^([A-Za-z0-9\^][^:]*):') {
        $actorName = $Matches[1].Trim()
        if ($unusedSet.Contains($actorName)) {
            # Collect this entire block and comment it out
            $blockLines = [System.Collections.Generic.List[string]]::new()
            $blockLines.Add($line)
            $i++
            # Consume all indented lines (the block body)
            while ($i -lt $lines.Count -and ($lines[$i] -match '^[\t ]' -or $lines[$i] -eq '')) {
                $blockLines.Add($lines[$i])
                $i++
            }
            # Trim trailing blank lines from block, keep one for spacing
            while ($blockLines.Count -gt 1 -and $blockLines[$blockLines.Count - 1] -eq '') {
                $blockLines.RemoveAt($blockLines.Count - 1)
            }
            # Write commented-out block
            $output.Add("# --- COMMENTED OUT (no matching actor) ---")
            foreach ($bl in $blockLines) {
                if ($bl -eq '') { $output.Add('#') }
                else { $output.Add("# $bl") }
            }
            $output.Add('')
            $skippedBlocks++
            continue
        }
    }

    $output.Add($line)
    $i++
}

$output | Set-Content $SeqFile -Encoding UTF8
Write-Host "Done. Commented out $skippedBlocks blocks in $SeqFile" -ForegroundColor Green
