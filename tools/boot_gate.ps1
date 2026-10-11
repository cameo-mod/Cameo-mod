# Boot gate (docs/WORKFLOW.md §5, CLAUDE.md rule 1): launch engine\bin\OpenRA.exe directly (never launch-game.cmd from
# Git Bash), snapshot exception-*.log first, wait for MenuPostProcessEffect.PostWorldLoaded in perf.log, then kill only our
# own PID. The support dir is an ISOLATED copy of %APPDATA%\OpenRA (content + settings, own Logs/) because every launch
# truncates the shared perf.log — another agent booting in parallel voided two verdicts on 2026-10-03.
#   powershell -ExecutionPolicy Bypass -File tools\boot_gate.ps1 -Tree C:\cameo-wt\<task> [-SupportDir C:\cameo-wt\_support_<task>]
# The support dir is created on first use (Logs/ and Replays/ are not copied). Exit 0 = PASS, 1 = FAIL, 2 = machine at the cap.
param([Parameter(Mandatory = $true)][string]$Tree, [int]$TimeoutSec = 600, [string]$SupportDir = "")

if (-not $SupportDir) { $SupportDir = Join-Path (Split-Path $Tree -Parent) ("_support_" + (Split-Path $Tree -Leaf)) }
if (-not (Test-Path (Join-Path $SupportDir "Content"))) {
	robocopy (Join-Path $env:APPDATA "OpenRA") $SupportDir /E /XD Logs Replays /NFL /NDL /NJH /NJS /NP | Out-Null
	if ($LASTEXITCODE -ge 8) { Write-Output "ABORT: could not create the support dir $SupportDir (robocopy $LASTEXITCODE)"; exit 2 }
}

$running = @(Get-Process -Name OpenRA -ErrorAction SilentlyContinue).Count
if ($running -ge 3) { Write-Output "ABORT: $running OpenRA games already running (cap 3)"; exit 2 }

$base = $SupportDir
$logDir = Join-Path $base "Logs"
New-Item -ItemType Directory -Force $logDir | Out-Null
$before = @(Get-ChildItem (Join-Path $logDir "exception-*.log") -ErrorAction SilentlyContinue | ForEach-Object Name)
$perfPath = Join-Path $logDir "perf.log"
# OpenRA truncates perf.log at launch, so detect a fresh write by time, not by size.
$launchTime = Get-Date

$engine = Join-Path $Tree "engine"
$mods = (Join-Path $Tree "mods") + ",./mods"
$launchArgs = @("Game.Mod=cameo", "Engine.EngineDir=..", "Engine.ModSearchPaths=$mods")
$launchArgs += "Engine.SupportDir=$SupportDir"
$proc = Start-Process -FilePath (Join-Path $engine "bin\OpenRA.exe") -WorkingDirectory $engine -PassThru -ArgumentList $launchArgs
Write-Output "PID=$($proc.Id) tree=$Tree"

$reached = $false
$deadline = (Get-Date).AddSeconds($TimeoutSec)
while ((Get-Date) -lt $deadline -and -not $proc.HasExited) {
	Start-Sleep -Seconds 5
	if ((Test-Path $perfPath) -and (Get-Item $perfPath).LastWriteTime -gt $launchTime) {
		$fs = [System.IO.File]::Open($perfPath, 'Open', 'Read', 'ReadWrite')
		$text = (New-Object System.IO.StreamReader($fs)).ReadToEnd(); $fs.Close()
		if ($text -match "MenuPostProcessEffect\.PostWorldLoaded") { $reached = $true; break }
	}
}
$exited = $proc.HasExited
if (-not $exited) { Stop-Process -Id $proc.Id -Force; $proc.WaitForExit(10000) | Out-Null }

# A kill command returning is not proof — 2026-10-09 incident: a launched
# OpenRA.exe survived Stop-Process and blocked other lanes for ~1h.
# Verify the exact PID is gone; escalate to taskkill on the same PID;
# never sweep by image name (other agents' drivers share this machine).
if (Get-Process -Id $proc.Id -ErrorAction SilentlyContinue) {
	taskkill /PID $proc.Id /F /T | Out-Null
	$proc.WaitForExit(10000) | Out-Null
}
$stillAlive = Get-Process -Id $proc.Id -ErrorAction SilentlyContinue
if ($stillAlive) { Write-Output "FAIL: game process $($proc.Id) is still running"; exit 1 }

Start-Sleep -Seconds 2
$new = @(Get-ChildItem (Join-Path $logDir "exception-*.log") -ErrorAction SilentlyContinue | ForEach-Object Name | Where-Object { $_ -notin $before })
Write-Output ("MENU " + $(if ($reached) { "REACHED" } else { "NOT REACHED" }) + $(if ($exited) { " (process exited by itself)" } else { "" }))
Write-Output ("NEW EXCEPTIONS: " + $(if ($new.Count) { $new -join ", " } else { "none" }))
if ($reached -and $new.Count -eq 0) { Write-Output "BOOT GATE PASS"; exit 0 } else { Write-Output "BOOT GATE FAIL"; exit 1 }
