# Magic Keyboard Suite (Diseño Unificado: SwiftUI en macOS + WinUI 3 en Windows 11)

Suite multiplataforma con **fuente única de diseño y textos** (`shared/`) para **macOS (SwiftUI)** y **Windows 11 (WinUI 3 / Windows App SDK)**. Ambas aplicaciones comparten exactamente las mismas medidas, colores, iconografía, traducciones (`es` / `en`), nombres de componentes (`SettingsCard` y `KeyboardCard`) y arquitectura en 3 capas (`Models`, `Services`, `Views`).

---

## 🎨 1. Coherencia Visual y Recursos Compartidos ([shared/](./shared))

- **[shared/design/tokens.json](./shared/design/tokens.json)**: Paleta semántica (claro/oscuro), radios (`8 px`), espaciados, tamaños (`268 px` barra lateral, `920 px` ancho máximo, `44×44` mosaico de icono), tipografía y mapa de iconos equivalentes (SF Symbols ↔ Segoe Fluent Icons).
- **[shared/strings/es.json](./shared/strings/es.json)**, **[shared/strings/ca.json](./shared/strings/ca.json)** y **[shared/strings/en.json](./shared/strings/en.json)**: Textos unificados en español, catalán e inglés, con variantes por plataforma cuando hace falta.
- **[shared/settings.json](./shared/settings.json)**: Esquema único de preferencias compartido entre `UserDefaults` (`com.magickeyboardsuite`) y el Registro de Windows (`HKCU\Software\MagicKeyboardSuite`).
- **[shared/assets/](./shared/assets)**: Icono maestro (`icon-1024.png`) del que salen `AppIcon.icns` (macOS) y `AppIcon.ico` (Windows y el instalador `.msi`).
- **[shared/tools/generate.py](./shared/tools/generate.py)**: Generador automático de `Theme.swift`, `L10n.swift`, `Settings.swift`, `Theme.xaml`, `L10n.cs` y `Settings.cs`.
- **[VERSION](./VERSION)**: Archivo único de versión (`3.1.0`) leído por ambas apps y sus instaladores.

---

## 🍎 2. Aplicación Nativa para macOS ([macOS/](./macOS))

Compilada en **SwiftUI + `IOBluetooth` + `Carbon` + `ServiceManagement`**:

- **Detección por eventos (sin polling)**:
  - Notificaciones nativas de conexión/desconexión de `IOBluetoothDevice` y de despertar del sistema (`NSWorkspace.didWakeNotification`).
  - Observador en tiempo real del idioma/disposición activa mediante `kTISNotifySelectedKeyboardInputSourceChanged`.
- **Icono en la barra de menús (`MenuBarExtra`)**:
  - Paridad completa con el icono de la bandeja del sistema de Windows: estado de la disposición activa, conexión/liberación rápida de teclados, inicio automático al iniciar sesión (`SMAppService`) y cierre.
- **Compilación reproducible**:
  ```bash
  ./macOS/build.sh          # Compila y firma Magic Keyboard Suite.app
  ./macOS/build.sh --dmg    # Genera además MagicKeyboardSuite-macOS.dmg
  ```

---

## 🪟 3. Aplicación Nativa WinUI 3 e Instalador `.msi` para Windows 11 ([Windows/](./Windows))

- **Bluetooth nativo WinRT (sin scripts de PowerShell ni avisos UAC)**:
  - [BluetoothService.cs](./Windows/WinUI3App/Services/BluetoothService.cs) usa `DeviceWatcher` en tiempo real, `DeviceInformationPairing.Custom` y `UnpairAsync` directamente desde C#.
- **Auto-configuración en el primer arranque**:
  - [MagicKeyboardAgent.cs](./Windows/Agent/MagicKeyboardAgent.cs) detecta el idioma de Windows en el primer inicio y activa automáticamente la disposición oficial de Apple correspondiente.
- **Generar el instalador `.msi`** (en Windows 10/11 x64 con .NET 8 SDK o superior):
  ```powershell
  powershell -ExecutionPolicy Bypass -File .\Windows\build.ps1
  ```
  Resultado: `Windows\MagicKeyboardSuite-Setup.msi` (también se compila automáticamente en GitHub Actions con [.github/workflows/build.yml](../.github/workflows/build.yml)).

---

## 📁 Estructura del Proyecto

- **[VERSION](./VERSION)**: Versión única del proyecto.
- **[shared/](./shared)**: Tokens de diseño, traducciones (`es`/`ca`/`en`), esquema de ajustes, iconos y generador [generate.py](./shared/tools/generate.py).
- **[macOS/](./macOS)**:
  - **[Package.swift](./macOS/Package.swift)** y **[build.sh](./macOS/build.sh)**.
  - **[Sources/MagicKeyboardSuite/](./macOS/Sources/MagicKeyboardSuite)**: `Models/`, `Services/`, `Views/` y `Generated/`.
- **[Windows/](./Windows)**:
  - **[build.ps1](./Windows/build.ps1)**: Compila la app WinUI 3, el agente y el instalador `.msi`.
  - **[WinUI3App/](./Windows/WinUI3App)**: App nativa WinUI 3 (`Models/`, `Services/`, `Views/`, `Generated/`).
  - **[Agent/](./Windows/Agent)**: Agente en segundo plano ([MagicKeyboardAgent.cs](./Windows/Agent/MagicKeyboardAgent.cs)).
  - **[Installer/](./Windows/Installer)**: Proyecto WiX v5 ([Package.wxs](./Windows/Installer/Package.wxs)).
