param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$publishDir = Join-Path $root "publish\$Configuration"
$zipPath = Join-Path $root "publish\ShortcutMaster-$Configuration.zip"

dotnet publish (Join-Path $root 'src\ShortcutMaster\ShortcutMaster.csproj') `
    -c $Configuration -r win-x64 --self-contained false `
    -o $publishDir /p:PublishSingleFile=false

if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
Compress-Archive -Path (Join-Path $publishDir '*') -DestinationPath $zipPath -Force

Write-Host "ZIP: $zipPath"
Get-Item $zipPath | Select-Object FullName, Length
