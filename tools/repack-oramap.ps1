param([string]$dir, [string]$oramap)
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

# Resolve to absolute paths first: Get-ChildItem's .FullName is always
# absolute, so a substring computed from a relative $dir's length silently
# produces garbage entry names (e.g. leftover drive/parent-dir prefix)
# instead of failing loudly -- this previously corrupted repacked .oramap
# files into unloadable packages when $dir was passed as a relative path.
$dirFull = (Resolve-Path $dir).Path.TrimEnd('\', '/')
$oramapFull = (Resolve-Path $oramap).Path

$tempZip = $oramapFull + ".tmp"
if (Test-Path $tempZip) { Remove-Item $tempZip -Force }

$zip = [System.IO.Compression.ZipFile]::Open($tempZip, [System.IO.Compression.ZipArchiveMode]::Create)
$files = Get-ChildItem $dirFull -Recurse -File
foreach ($f in $files) {
    $relPath = $f.FullName.Substring($dirFull.Length + 1).Replace("\", "/")
    [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $f.FullName, $relPath) | Out-Null
}
$zip.Dispose()

Copy-Item $tempZip $oramapFull -Force
Remove-Item $tempZip -Force
Write-Output "Repacked $oramapFull successfully"
