<#
.SYNOPSIS
    Compila Magic Keyboard Suite y genera el instalador MagicKeyboardSuite-Setup.msi.

.DESCRIPTION
    Ejecutar en Windows 10/11 x64 con el .NET 8 SDK (o superior) y Python 3 instalados:

        powershell -ExecutionPolicy Bypass -File .\build.ps1

    Pasos:
      0. Lee la versión única desde ..\VERSION y regenera los recursos compartidos.
      1. Publica la app WinUI 3 (autocontenida, win-x64).
      2. Compila el agente en segundo plano (.NET Framework 4.8).
      3. Genera el .msi con WiX Toolset v5.
      4. (Opcional) Firma los ejecutables y el .msi si $env:SIGN_THUMBPRINT está definido.
#>
param(
    [string]$Version = ""
)

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot
$repoRoot = Split-Path -Parent $root
$art  = Join-Path $root "artifacts"

if ([string]::IsNullOrWhiteSpace($Version)) {
    $Version = (Get-Content (Join-Path $repoRoot "VERSION") -Raw).Trim()
}

function Invoke-Step([string]$title, [scriptblock]$cmd) {
    Write-Host "==> $title" -ForegroundColor Cyan
    & $cmd
    if ($LASTEXITCODE -ne 0) { throw "Falló: $title (código $LASTEXITCODE)" }
}

function Invoke-CodeSign([string]$filePath) {
    if (-not [string]::IsNullOrWhiteSpace($env:SIGN_THUMBPRINT)) {
        $signtool = Get-Command "signtool.exe" -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Source -First 1
        if ($signtool) {
            & $signtool sign /sha1 $env:SIGN_THUMBPRINT /fd SHA256 /tr "http://timestamp.digicert.com" /td SHA256 "$filePath"
        }
    }
}

if (Get-Command "python" -ErrorAction SilentlyContinue) {
    Invoke-Step "Sincronizando tokens y textos compartidos (v$Version)" {
        python "$repoRoot\shared\tools\generate.py"
    }
}

if (Test-Path $art) { Remove-Item $art -Recurse -Force }

Invoke-Step "Publicando app WinUI 3 (win-x64, v$Version)" {
    dotnet publish "$root\WinUI3App\MagicKeyboardSuite.WinUI3.csproj" `
        -c Release -r win-x64 -p:Platform=x64 --self-contained true `
        -p:Version=$Version -o "$art\app" --nologo
}

Invoke-Step "Limpiando idiomas MUI no usados y símbolos (.pdb) para acelerar el MSI" {
    $keepLocales = @("en", "en-us", "es", "es-es", "ca", "ca-es")
    $localeRegex = '^(?:[a-z]{2,3}(?:-[a-z]{4})?-(?:[a-z]{2}|[0-9]{3})|cs|de|fr|it|ja|ko|pl|pt|ru|tr|zh-hans|zh-hant)$'
    $removedDirs = 0
    Get-ChildItem -Path "$art\app" -Directory -Recurse | Where-Object {
        $name = $_.Name.ToLowerInvariant()
        ($name -match $localeRegex) -and (-not ($keepLocales -contains $name)) -and (-not $name.StartsWith("win-"))
    } | ForEach-Object {
        if (Test-Path $_.FullName) {
            Remove-Item -LiteralPath $_.FullName -Recurse -Force -ErrorAction SilentlyContinue
            $removedDirs++
        }
    }
    Get-ChildItem -Path "$art\app" -Filter *.pdb -File -Recurse | Remove-Item -Force -ErrorAction SilentlyContinue
    Write-Host "    Carpetas de idiomas eliminadas: $removedDirs" -ForegroundColor DarkGray
}

Invoke-Step "Compilando agente en segundo plano (v$Version)" {
    dotnet build "$root\Agent\MagicKeyboardAgent.csproj" `
        -c Release -p:Version=$Version -o "$art\agent" --nologo
}

Invoke-CodeSign "$art\app\MagicKeyboardSuite.exe"
Invoke-CodeSign "$art\agent\MagicKeyboardAgent.exe"

Invoke-Step "Generando instalador MSI (WiX v5)" {
    dotnet build "$root\Installer\MagicKeyboardSuite.Installer.wixproj" `
        -c Release -p:ProductVersion=$Version `
        -p:AppPublishDir="$art\app" -p:AgentDir="$art\agent" `
        -o "$art\msi" --nologo
}

$msi = Get-ChildItem "$art\msi" -Filter *.msi | Select-Object -First 1
$finalMsi = Join-Path $root "MagicKeyboardSuite-Setup.msi"
Copy-Item $msi.FullName $finalMsi -Force
Invoke-CodeSign $finalMsi

# Limpiar carpetas intermedias de compilación para mantener el directorio ordenado
if (Test-Path $art) { Remove-Item $art -Recurse -Force -ErrorAction SilentlyContinue }

Write-Host ""
Write-Host "Instalador listo: $finalMsi (v$Version)" -ForegroundColor Green
