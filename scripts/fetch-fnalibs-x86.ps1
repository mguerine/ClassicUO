$ErrorActionPreference = "Stop"
$baseUrl = "https://raw.githubusercontent.com/kg/fnalibs/master/x86"
$targetDir = Join-Path $PSScriptRoot "..\external\x86"

if (!(Test-Path $targetDir)) { New-Item -ItemType Directory -Path $targetDir -Force | Out-Null }

$files = @("FAudio.dll", "FNA3D.dll", "SDL3.dll", "libtheorafile.dll")
foreach ($f in $files) {
    $url = "$baseUrl/$f"
    $dest = Join-Path $targetDir $f
    Write-Host "Downloading $f..."
    Invoke-WebRequest -Uri $url -OutFile $dest -UseBasicParsing
}
Write-Host "Done. $($files.Count) files saved to $targetDir"
Write-Host "zlib: Not needed for x86 - ClassicUO uses managed implementation when Is64BitProcess=false."
