param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$exe = Join-Path $root "src\ShortcutMaster\bin\$Configuration\net10.0-windows\ShortcutMaster.exe"

dotnet build (Join-Path $root 'ShortcutMaster.slnx') -c $Configuration
if (-not (Test-Path $exe)) { throw "ビルド後の exe が見つかりません: $exe" }
Start-Process -FilePath $exe
