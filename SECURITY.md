# Política de Seguridad y Privacidad / Security Policy

## Privacidad por Diseño (0% Telemetría)

- **Sin conexión a red:** Ni la aplicación de macOS, ni la aplicación WinUI 3 de Windows, ni el agente en segundo plano realizan peticiones a Internet.
- **Sin registro de pulsaciones:** En Windows, `KeyboardHookEngine.cs` y `AppleHidRawInput.cs` operan únicamente en memoria volátil para traducir en tiempo real los modificadores (`Command ⌘`, `Option ⌥`), las teclas ISO (`[º ª \]` ↔ `[< >]`) y navegación cuando provienen de un teclado Apple (`VID_05AC` / `004C`). Ninguna pulsación se almacena en disco ni en el Registro.

## Integridad en la Importación de Drivers Boot Camp

- **Verificación criptográfica Authenticode:** En Windows, `BootCampDriverService.cs` verifica con `WinVerifyTrust` y `Get-AuthenticodeSignature` que cada librería de teclado (`*A.dll`) cuente con una firma digital Authenticode válida de Apple Inc. o Microsoft Corporation antes de instalarla en `System32`.
- **Protección frente a condiciones de carrera (Anti-TOCTOU):** El proceso elevado copia los archivos a un directorio temporal protegido en `%SystemRoot%\Temp` y revalida sus hashes `SHA-256` exactos y su firma digital antes de registrar las disposiciones en `HKLM`.

## Compilación Transparente

Los instaladores de cada versión (`.dmg` y `.msi`) se compilan automáticamente en GitHub Actions (`.github/workflows/build.yml`) con permisos de mínimo privilegio (`contents: read` durante la compilación) a partir del código fuente público.

## Reporte de Vulnerabilidades

Si encuentras algún fallo de seguridad, infórmalo de forma privada mediante **GitHub Security Advisories** en la pestaña *Security* del repositorio.
