$files = Get-ChildItem -Path "mods\cameo" -Recurse -Filter "*.yaml" -File
foreach ($f in $files) {
    $c = Get-Content $f.FullName -Raw
    if ($c -match "ra2america|ra2russia|edenl|plymouthl|RA2Soviet[^s]|ra2soviet[^s]") {
        # Order matters: do singular soviet replacements BEFORE faction renames
        # to avoid touching the new ra2soviets faction name
        $c = $c -replace "RA2Soviet([^s])", 'RA2Soviets$1'
        $c = $c -replace "ra2soviet([^s])", 'ra2soviets$1'
        $c = $c -replace "ra2america", "ra2allies"
        $c = $c -replace "ra2russia", "ra2soviets"
        $c = $c -replace "edenl", "eden"
        $c = $c -replace "plymouthl", "plymouth"
        [System.IO.File]::WriteAllText($f.FullName, $c)
        Write-Output "Modified: $($f.FullName)"
    }
}
