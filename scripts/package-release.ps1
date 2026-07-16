param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',

    [switch]$AllowDirty
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$publishDir = Join-Path $root "publish\$Configuration"
$zipPath = Join-Path $root "publish\ShortcutMaster-$Configuration.zip"

Push-Location $root
try {
    $dirty = git status --porcelain
    if ($dirty -and -not $AllowDirty) {
        Write-Error @"
作業ツリーが dirty です。コミットするか -AllowDirty を付けてください。

$dirty
"@
    }

    Write-Host "dotnet test ($Configuration)..."
    dotnet test (Join-Path $root 'ShortcutMaster.slnx') -c $Configuration --nologo
    if ($LASTEXITCODE -ne 0) {
        Write-Error "テスト失敗のためパッケージを中止しました。"
    }

    Write-Host "dotnet publish ($Configuration)..."
    dotnet publish (Join-Path $root 'src\ShortcutMaster\ShortcutMaster.csproj') `
        -c $Configuration -r win-x64 --self-contained false `
        -o $publishDir /p:PublishSingleFile=false

    if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
    Compress-Archive -Path (Join-Path $publishDir '*') -DestinationPath $zipPath -Force

    Write-Host "ZIP: $zipPath"
    Get-Item $zipPath | Select-Object FullName, Length
}
finally {
    Pop-Location
}
