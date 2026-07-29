param([string]$dir, [string]$oramap)
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

$tempZip = $oramap + ".tmp"
if (Test-Path $tempZip) { Remove-Item $tempZip -Force }

$zip = [System.IO.Compression.ZipFile]::Open($tempZip, [System.IO.Compression.ZipArchiveMode]::Create)
$files = Get-ChildItem $dir -Recurse -File
foreach ($f in $files) {
    $relPath = $f.FullName.Substring($dir.Length + 1).Replace("\", "/")
    [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $f.FullName, $relPath) | Out-Null
}
$zip.Dispose()

Copy-Item $tempZip $oramap -Force
Remove-Item $tempZip -Force
Write-Output "Repacked $oramap successfully"
