# Magic Keyboard Suite

<p align="center">
  <img src="MagicKeyboardSuite/shared/assets/icon-1024.png" alt="Magic Keyboard Suite Icon" width="108" height="108" />
</p>

<p align="center">
  <strong>Utilidad nativa para teclados Apple Magic Keyboard en macOS y Windows 11</strong><br/>
  <em>Cross-platform native utility for Apple Magic Keyboards on macOS &amp; Windows 11</em><br/>
  <em>Utilitat nativa per a teclats Apple Magic Keyboard a macOS i Windows 11</em>
</p>

<p align="center">
  <a href="#español">Español</a> •
  <a href="#català">Català</a> •
  <a href="#english">English</a> •
  <a href="CONTRIBUTING.md">Contributing</a> •
  <a href="SECURITY.md">Security</a> •
  <a href="LICENSE">GPLv3</a>
</p>

---

## Español

**Magic Keyboard Suite** unifica la experiencia de tu **Apple Magic Keyboard** entre **macOS** y **Windows 11**: cambia de equipo en un clic y mantén exactamente la misma disposición física, símbolos con `Option (⌥)` y atajos con `Command (⌘)`.

### Características principales

- **Bluetooth y cable USB (Lightning / USB-C):** Funciona tanto de forma inalámbrica (liberando y reconectando el emparejamiento Bluetooth entre Mac y PC en un clic) como conectado por cable USB.
- **Indicador de batería en tiempo real:** Lee el porcentaje de batería de tus teclados conectados (`IORegistry` en macOS y PnP/GATT en Windows).
- **Cada tecla coincide con su serigrafía:**
  - 17 disposiciones oficiales de Apple (con importador automático de Boot Camp desde la bienvenida en Windows) e intercambio ISO automático (`[º ª \]` junto al `1` y `[< >]` junto a `Mayús` izquierdo).
  - `Option (⌥)` izquierda escribe `@ # € [ ] { } \ | ~` igual en macOS y en Windows.
  - Cambio directo de disposición activa en macOS (`Carbon TIS`) y en Windows.
- **Atajos de Mac en Windows:** `⌘ + C/V/X/Z/A/S/F/T/W`, `⌘ + Tab`, `⌘ + Q`, `⌘ + Espacio`, `⌘ + Flechas` (`Inicio / Fin / RePág / AvPág`) y modo opcional de `Option (⌥)` derecho como `Fn` (`Supr`, navegación y `F1–F12`), aplicados solo a hardware de Apple (`VID_05AC` / `004C`).
- **Diseño nativo unificado y multiidioma (`Español`, `Català`, `English`):**
  - **macOS:** Swift + SwiftUI nativo (`macOS 13+`).
  - **Windows 11:** C# + WinUI 3 (.NET 8) + Agente en bandeja del sistema + Instalador `.msi` (WiX v5).

### Descarga o compilación

1. **Instaladores listos para usar (GitHub Releases):**
   - **macOS:** Descarga `MagicKeyboardSuite-macOS.dmg` desde la sección **Releases** de GitHub.
   - **Windows 10/11 (x64):** Descarga `MagicKeyboardSuite-Setup.msi` desde la sección **Releases** de GitHub.
2. **Compilar desde el código fuente:**

```bash
# macOS (.app + .dmg)
./MagicKeyboardSuite/macOS/build.sh --dmg
```

```powershell
# Windows 10/11 x64 (.msi)
powershell -ExecutionPolicy Bypass -File .\MagicKeyboardSuite\Windows\build.ps1
```

---

## Català

**Magic Keyboard Suite** unifica l'experiència del teu **Apple Magic Keyboard** entre **macOS** i **Windows 11**: passa el teclat d'un ordinador a un altre en un clic i mantén exactament la mateixa disposició física, símbols amb `Option (⌥)` i dreceres amb `Command (⌘)`.

- **Canvi Mac ↔ Windows en 1 clic:** Allibera l'aparellament Bluetooth en un equip i connecta'l a l'altre.
- **Percentatge de bateria en temps real** a les targetes i a la barra de menús.
- **Disposició d'Apple i correcció ISO:** `[º ª \]` al costat de l'`1`, `[< >]` al costat de `Majúscules` esquerre i `Option (⌥)` esquerra per a `@ # € [ ] { } \ | ~`.
- **Interfície en Català, Español i English** canviable en viu des de l'aplicació.

---

## English

**Magic Keyboard Suite** brings a unified **Apple Magic Keyboard** experience to **macOS** and **Windows 11**: switch computers in one click while keeping identical keycap mapping, `Option (⌥)` symbols, and `Command (⌘)` shortcuts.

- **1-Click Computer Handover (Mac ↔ PC):** Release the active Bluetooth pairing on one computer and reconnect on the other in one click.
- **Live Battery Level Badge:** Displays real-time battery percentage on macOS and Windows.
- **True Apple Layout & ISO Fix:** Matches every printed keycap (`[º ª \]` next to `1`, `[< >]` next to left `Shift`, and left `Option (⌥)` for `@ # € [ ] { } \ | ~`), plus instant macOS layout switching via Carbon TIS.
- **Native SwiftUI & WinUI 3 Apps:** Powered by a single source of truth in [`MagicKeyboardSuite/shared/`](MagicKeyboardSuite/shared/).

---

## Aviso Legal / Disclaimer

Proyecto independiente de código abierto bajo licencia [GPLv3](LICENSE). No está afiliado, patrocinado ni respaldado por Apple Inc. ni por Microsoft Corporation. *"Apple"*, *"Magic Keyboard"*, *"macOS"* y *"Boot Camp"* son marcas registradas de Apple Inc. *"Windows"* es marca registrada de Microsoft Corporation.
