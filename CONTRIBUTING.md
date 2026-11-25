# Guía de Contribución / Contributing Guide

## Arquitectura de Fuente Única (`MagicKeyboardSuite/shared/`)

Para que la aplicación de **macOS (SwiftUI)** y la de **Windows 11 (WinUI 3)** mantengan siempre el mismo diseño, textos y ajustes, ambas plataformas generan su código compartido desde `MagicKeyboardSuite/shared/`:

1. **Diseño (`shared/design/tokens.json`)**: Colores (claro/oscuro), radios, espaciados, tamaños, tipografía e iconos (`SF Symbols` ↔ `Segoe Fluent Icons`).
2. **Textos (`shared/strings/{es,ca,en}.json`)**: Todas las claves deben estar presentes en los tres idiomas (`es`, `ca`, `en`).
3. **Ajustes (`shared/settings.json`)**: Preferencias compartidas entre `UserDefaults` (`com.magickeyboardsuite`) y el Registro (`HKCU\Software\MagicKeyboardSuite`).

### Regenerar código compartido

Tras editar cualquier archivo de `shared/` o `VERSION`, ejecuta:

```bash
python3 MagicKeyboardSuite/shared/tools/generate.py
```

Para validar en CI que los archivos generados están sincronizados:

```bash
python3 MagicKeyboardSuite/shared/tools/generate.py --check
```

## Compilación

- **macOS (SwiftUI):**
  ```bash
  ./MagicKeyboardSuite/macOS/build.sh --dmg
  ```
- **Windows 11 (WinUI 3 + Agente + Instalador `.msi`):**
  ```powershell
  powershell -ExecutionPolicy Bypass -File .\MagicKeyboardSuite\Windows\build.ps1
  ```
