# Política de Seguridad y Privacidad / Security Policy

## Privacidad por Diseño (0% Telemetría)

- **Sin conexión a red:** Ni la app de macOS, ni la app WinUI 3 de Windows, ni el agente en segundo plano realizan peticiones a Internet.
- **Sin registro de pulsaciones:** En Windows, `MagicKeyboardAgent.cs` usa `SetWindowsHookEx(WH_KEYBOARD_LL)` y `RawInput` únicamente en memoria volátil para traducir en tiempo real los modificadores (`Command ⌘`, `Option ⌥`), las teclas ISO (`[º ª \]` ↔ `[< >]`) y navegación cuando provienen de un teclado Apple (`VID_05AC` / `004C`). Ninguna pulsación se almacena en disco ni registro.

## Compilación Transparente

Los instaladores de cada versión (`.dmg` y `.msi`) se compilan automáticamente en GitHub Actions (`.github/workflows/build.yml`) a partir del código fuente público.

## Reporte de Vulnerabilidades

Si encuentras algún fallo de seguridad, infórmalo de forma privada mediante **GitHub Security Advisories** en la pestaña *Security* del repositorio.
