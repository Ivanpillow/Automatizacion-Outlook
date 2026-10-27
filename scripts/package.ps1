$ErrorActionPreference = 'Stop'
$raiz = Split-Path -Parent $PSScriptRoot
$publicacion = Join-Path $raiz 'artifacts/publish/win-x64'
$entrega = Join-Path $raiz 'artifacts/entrega'
foreach ($relativo in @('CorreosOutlook.WinForms.exe', 'coreclr.dll', 'config/evento.json', 'templates/catalogo.json')) {
    if (-not (Test-Path -LiteralPath (Join-Path $publicacion $relativo))) {
        throw 'Falta la publicacion autocontenida. Ejecute scripts/build.ps1 -Publish primero.'
    }
}
Copy-Item -LiteralPath (Join-Path $raiz 'Iniciar Expo Joya.cmd') -Destination $publicacion -Force
Copy-Item -LiteralPath (Join-Path $raiz 'INICIO_RAPIDO.md') -Destination $publicacion -Force
New-Item -ItemType Directory -Path $entrega -Force | Out-Null
$zip = Join-Path $entrega 'ExpoJoya-Demo-win-x64.zip'
Compress-Archive -Path (Join-Path $publicacion '*') -DestinationPath $zip -CompressionLevel Fastest -Force
Write-Output "Paquete listo: $zip"
