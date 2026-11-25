$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot

dotnet build "$root\src\TownTrainer" -c Release -v q --nologo
if ($LASTEXITCODE -ne 0) { throw "dotnet build 失败" }

$out = Join-Path $root 'dist\TownTrainer'
New-Item -ItemType Directory -Force $out | Out-Null
Copy-Item "$root\src\TownTrainer\bin\Release\netstandard2.0\TownTrainer.dll" $out -Force
Copy-Item "$root\src\TownTrainer\TownTrainer.json" $out -Force
Copy-Item "$root\icon.png" $out -Force
$ver = '1.3.6'
$zip = Join-Path $root "dist\TownTrainer-$ver.zip"
if (Test-Path $zip) { Remove-Item $zip -Force }
Compress-Archive -Path "$out\*" -DestinationPath $zip

Write-Host "OK -> $out"
Get-ChildItem $out | Select-Object Name, Length | Format-Table -AutoSize
Get-ChildItem $zip | Select-Object Name, Length | Format-Table -AutoSize
