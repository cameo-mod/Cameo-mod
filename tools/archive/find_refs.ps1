$lines = Get-Content 'docs\Cameo_Knowledge_Base_Manual.md'
for ($i = 0; $i -lt $lines.Count; $i++) {
    if ($lines[$i] -match 'Appendix [Q-Y]' -and $i -lt 41038) {
        $lineNum = $i + 1
        $trimmed = $lines[$i].Trim()
        if ($trimmed.Length -gt 120) { $trimmed = $trimmed.Substring(0, 120) + '...' }
        Write-Output ("{0}: {1}" -f $lineNum, $trimmed)
    }
}
