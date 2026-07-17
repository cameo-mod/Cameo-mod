$patterns = @("warcraft_humans_", "warcraft_orcs_", "consortium_", "syndicate_")
foreach ($pat in $patterns) {
    $files = Get-ChildItem -Path "mods\cameo" -Recurse -File | Where-Object { $_.Name -match $pat }
    Write-Output "${pat}: $($files.Count) asset files"
}
