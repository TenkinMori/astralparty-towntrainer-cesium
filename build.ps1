$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot

dotnet build "$root\src\TownTrainer" -c Release -v q --nologo
if ($LASTEXITCODE -ne 0) { throw "dotnet build 失败" }

$out = Join-Path $root 'dist\TownTrainer'
New-Item -ItemType Directory -Force $out | Out-Null
Copy-Item "$root\src\TownTrainer\bin\Release\netstandard2.0\TownTrainer.dll" $out -Force
Copy-Item "$root\src\TownTrainer\TownTrainer.json" $out -Force
Copy-Item "$root\icon.png" $out -Force
Write-Host "OK -> $out"
Get-ChildItem $out | Select-Object Name, Length | Format-Table -AutoSize
