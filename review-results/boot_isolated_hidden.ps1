# Boot gate with an isolated support dir: no shared %APPDATA% logs, so no stale menu marker can false-pass.
# Usage: powershell -File C:\cameo-wt\boot_isolated.ps1 -Worktree C:\cameo-wt\<wt>
param([Parameter(Mandatory = $true)][string]$Worktree, [int]$TimeoutSec = 150)

$sd = Join-Path 'C:\cameo-wt' ('_boot_' + (Split-Path $Worktree -Leaf) + '_' + (Get-Date -Format 'yyyyMMddHHmmss'))
New-Item -ItemType Directory $sd | Out-Null
$p = Start-Process -FilePath "$Worktree\engine\bin\OpenRA.exe" -WorkingDirectory "$Worktree\engine" -PassThru -WindowStyle Hidden -ArgumentList @(
    'Game.Mod=cameo', 'Engine.EngineDir=..', "Engine.LaunchPath=$Worktree\launch-game.cmd",
    "Engine.ModSearchPaths=$Worktree\mods,./mods", "Engine.SupportDir=$sd")
Write-Output "PID=$($p.Id) SUPPORTDIR=$sd"

$deadline = (Get-Date).AddSeconds($TimeoutSec); $menu = $false
while ((Get-Date) -lt $deadline) {
    Start-Sleep -Seconds 5
    $pl = "$sd\Logs\perf.log"
    if ((Test-Path $pl) -and (Select-String -Path $pl -Pattern 'MenuPostProcessEffect.PostWorldLoaded' -Quiet)) { $menu = $true; break }
    if ($p.HasExited) { break }
}
$exitedEarly = $p.HasExited
Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 2

$pkg = Select-String -Path "$sd\Logs\debug.log" -Pattern 'MATCH INFO' -ErrorAction SilentlyContinue | Select-Object -First 1
$inTree = $false
if ($pkg) { $inTree = $pkg.Line.Replace('\\', '\').Contains($Worktree) }
$ex = @(Get-ChildItem "$sd\Logs\exception-*.log" -ErrorAction SilentlyContinue)

Write-Output "MENU_REACHED=$menu EXITED_EARLY=$exitedEarly PACKAGE_IN_WORKTREE=$inTree EXCEPTIONS=$($ex.Count)"
if ($menu -and $inTree -and -not $exitedEarly -and $ex.Count -eq 0) { Write-Output 'BOOT_GATE=PASS' } else { Write-Output 'BOOT_GATE=FAIL' }

