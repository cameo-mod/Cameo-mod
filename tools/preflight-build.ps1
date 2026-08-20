param(
	[switch] $Quiet
)

$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
$modConfig = Join-Path $repoRoot 'mod.config'
$engineVersionFile = Join-Path $repoRoot 'engine\VERSION'

if (-not (Test-Path -LiteralPath $modConfig))
{
	throw "STOP: mod.config not found at '$modConfig'. Run this from a Cameo-mod checkout."
}

if (-not (Test-Path -LiteralPath $engineVersionFile))
{
	throw "STOP: engine\VERSION not found at '$engineVersionFile'. Do not build; ask Blackrobe before changing engine pins."
}

$pinLine = Select-String -Path $modConfig -Pattern '^ENGINE_VERSION="([^"]+)"' | Select-Object -First 1
if ($null -eq $pinLine -or $pinLine.Matches.Count -eq 0)
{
	throw "STOP: Could not read ENGINE_VERSION from mod.config. Do not build."
}

$engineVersion = (Get-Content -LiteralPath $engineVersionFile -Raw).Trim()
$requiredVersion = $pinLine.Matches[0].Groups[1].Value

if ($requiredVersion -ne $engineVersion)
{
	throw "STOP: mod.config ENGINE_VERSION=$requiredVersion but engine\VERSION=$engineVersion. Do not run ./make all; sync/ask Blackrobe first."
}

if (-not $Quiet)
{
	Write-Host "Preflight OK: mod.config ENGINE_VERSION and engine\VERSION both $requiredVersion."
}
