$ErrorActionPreference = 'Stop'
$logDir = "$env:APPDATA\OpenRA\Logs"
$before = @(Get-ChildItem "$logDir\exception-*.log" -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Name)
Write-Output "exceptions_before=$($before.Count)"
$p = Start-Process -FilePath 'C:\cameo-wt\nova-raidgate\engine\bin\OpenRA.exe' -ArgumentList 'Game.Mod=cameo','Engine.EngineDir=..','Engine.LaunchPath=launch-game.cmd','Engine.ModSearchPaths=C:\cameo-wt\nova-raidgate\mods,./mods','Engine.SupportDir=C:\cameo-wt\_support_lc5claim','--render','fast' -PassThru -WorkingDirectory 'C:\cameo-wt\nova-raidgate\engine'
Write-Output "pid=$($p.Id)"
$deadline = (Get-Date).AddSeconds(150)
$ok = $false
while ((Get-Date) -lt $deadline) {
    $perf = "$logDir\..\..\..\..\cameo-wt\_support_lc5claim\Logs\perf.log"
    $perf = 'C:\cameo-wt\_support_lc5claim\Logs\perf.log'
    if (Test-Path $perf) {
        $tail = Get-Content $perf -Tail 40 -ErrorAction SilentlyContinue
        if ($tail | Select-String 'MenuPostProcessEffect.PostWorldLoaded') { $ok = $true; break }
    }
    Start-Sleep -Seconds 3
}
Write-Output "menu_reached=$ok"
$after = @(Get-ChildItem "$logDir\exception-*.log" -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Name)
$supExc = @(Get-ChildItem 'C:\cameo-wt\_support_lc5claim\Logs\exception-*.log' -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Name)
$new = @($after | Where-Object { $_ -notin $before }) + $supExc
Write-Output "new_exceptions=$($new -join ',')"
Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue
Write-Output "killed=$($p.Id) result=$(if ($ok -and $new.Count -eq 0) { 'PASS' } else { 'FAIL' })"
