$targets = @(
    @('mods\cameo\ContentPacks\Warcraft2\Humans\yaml\vehicles.yaml', 113),
    @('mods\cameo\ContentPacks\Warcraft2\Orcs\yaml\infantry.yaml', 324),
    @('mods\cameo\ContentPacks\Warcraft2\Humans\yaml\infantry.yaml', 394),
    @('mods\cameo\ContentPacks\StarCraft\Zerg\yaml\templates.yaml', 151),
    @('mods\cameo\ContentPacks\StarCraft\Zerg\yaml\templates.yaml', 164)
)
foreach ($t in $targets) {
    $path = $t[0]; $line = [int]$t[1]
    $m = Select-String -Path $path -Pattern '^[A-Za-z0-9_^]+:' | Where-Object { $_.LineNumber -lt $line } | Select-Object -Last 1
    Write-Output "$path :$line -> $($m.LineNumber): $($m.Line)"
}
