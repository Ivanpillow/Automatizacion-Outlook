param([switch]$Publish)
$ErrorActionPreference = 'Stop'
$raiz = Split-Path -Parent $PSScriptRoot
Push-Location $raiz
try {
    $sdkLocal = Join-Path $raiz '.tools/dotnet/dotnet.exe'
    $dotnet = if (Test-Path -LiteralPath $sdkLocal) { $sdkLocal } else { 'dotnet' }
    $env:DOTNET_CLI_HOME = Join-Path $raiz '.dotnet-home'
    $env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
    $env:NUGET_PACKAGES = Join-Path $raiz '.nuget/packages'
    & $dotnet restore CorreosOutlook.sln --packages .nuget/packages
    if ($LASTEXITCODE -ne 0) { throw 'No se pudieron restaurar las dependencias.' }
    & $dotnet build CorreosOutlook.sln -c Release --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'Falló la compilación.' }
    & $dotnet test CorreosOutlook.sln -c Release --no-build --logger 'trx;LogFileName=resultados.trx' --results-directory artifacts/tests
    if ($LASTEXITCODE -ne 0) { throw 'Fallaron las pruebas.' }
    if ($Publish) {
        & $dotnet publish src/CorreosOutlook.WinForms -c Release -r win-x64 --self-contained true -o artifacts/publish/win-x64
        if ($LASTEXITCODE -ne 0) { throw 'Falló la publicación.' }
    }
}
finally { Pop-Location }
