# Guía de Contribución y Arquitectura / Contributing & Architecture

## Estructura del Repositorio

```text
Magic-Keyboard-Suite/
├── VERSION                  # Versión única leída por macOS, Windows e instaladores
├── shared/                  # Fuente única de verdad multiplataforma
│   ├── assets/              # Icono maestro (icon-1024.png, AppIcon.icns, AppIcon.ico)
│   ├── design/tokens.json   # Paleta claro/oscuro, radios, espaciados, tamaños e iconos
│   ├── layouts.json         # Las 17 disposiciones oficiales de Apple (macOS + Windows)
│   ├── settings.json        # Esquema compartido (UserDefaults <-> HKCU\Software\MagicKeyboardSuite)
│   ├── strings/             # Traducciones sincronizadas (ca.json, en.json, es.json)
│   └── tools/generate.py    # Generador de código Swift, XAML y C#
├── macos/                   # App nativa SwiftUI (IOBluetooth + IOKit + Carbon TIS + SMAppService)
├── windows/                 # Solución Windows 11 (MagicKeyboardSuite.sln)
│   ├── Shared/              # Utilidades comunes WinUI 3 + Agente y código generado
│   ├── WinUI3App/           # Interfaz nativa WinUI 3 (.NET 8, Mica, SettingsCard reutilizable)
│   ├── Agent/               # Servicio de bandeja y motor WH_KEYBOARD_LL + RawInput (.NET 4.8)
│   └── Installer/           # Instalador Windows Installer .msi (WiX Toolset v5)
├── tests/                   # Tests unitarios Python (generador/XAML) y xUnit (.NET 8)
└── docs/                    # Sitio web estático para GitHub Pages
```

## Fuente Única (`shared/`)

Para mantener paridad visual y funcional entre **macOS (SwiftUI)** y **Windows 11 (WinUI 3)**, nunca edites a mano los archivos dentro de `Generated/`. Tras modificar `VERSION` o cualquier archivo en `shared/`, regenera el código compartido:

```bash
python3 shared/tools/generate.py
```

Para comprobar que los archivos generados están sincronizados y ejecutar las pruebas unitarias:

```bash
python3 shared/tools/generate.py --check
python3 -m unittest discover -s tests -v
dotnet test tests/MagicKeyboardSuite.Tests/MagicKeyboardSuite.Tests.csproj -c Release
```

## Compilación

- **macOS (SwiftUI `.app` + `.dmg`):**
  ```bash
  ./macos/build.sh --dmg
  ```
- **Windows 11 (WinUI 3 + Agente + Instalador `.msi`):**
  ```powershell
  powershell -ExecutionPolicy Bypass -File .\windows\build.ps1
  ```
