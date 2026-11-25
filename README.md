# Magic Keyboard Suite

<p align="center">
  <img src="MagicKeyboardSuite/shared/assets/icon-1024.png" alt="Magic Keyboard Suite Icon" width="108" height="108" />
</p>

<p align="center">
  <strong>Utilitat nativa per a teclats Apple Magic Keyboard a macOS i Windows 11</strong><br/>
  <em>Cross-platform native utility for Apple Magic Keyboards on macOS &amp; Windows 11</em><br/>
  <em>Utilidad nativa para teclados Apple Magic Keyboard en macOS y Windows 11</em>
</p>

<p align="center">
  <a href="#català">Català</a> •
  <a href="#english">English</a> •
  <a href="#español">Español</a> •
  <a href="CONTRIBUTING.md">Contributing</a> •
  <a href="SECURITY.md">Security</a> •
  <a href="LICENSE">GPLv3</a>
</p>

---

## Català

**Magic Keyboard Suite** unifica l'experiència del teu **Apple Magic Keyboard** entre **macOS** i **Windows 11**: passa el teclat d'un ordinador a un altre en un clic i mantén exactament la mateixa disposició física, símbols amb `Option (⌥)` i dreceres amb `Command (⌘)`.

### Característiques principals

- **Bluetooth i cable USB (Lightning / USB-C):** Funciona tant sense fils (alliberant i reconnectant l'aparellament Bluetooth entre Mac i PC en un clic) com connectat per cable USB.
- **Indicador de bateria en temps real:** Llegeix el percentatge de bateria dels teclats connectats (`IORegistry` a macOS i PnP/GATT a Windows).
- **Cada tecla coincideix amb la seva serigrafia:**
  - 17 disposicions oficials d'Apple (amb importador automàtic de Boot Camp des de la benvinguda a Windows) i intercanvi ISO automàtic (`[º ª \]` al costat de l'`1` i `[< >]` al costat de `Majúscules` esquerre).
  - `Option (⌥)` esquerra escriu `@ # € [ ] { } \ | ~` igual a macOS i a Windows.
  - Canvi directe de disposició activa a macOS (`Carbon TIS`) i a Windows.
- **Dreceres de Mac a Windows:** `⌘ + C/V/X/Z/A/S/F/T/W`, `⌘ + Tab`, `⌘ + Q`, `⌘ + Espai`, `⌘ + Fletxes` (`Inici / Fi / RePàg / AvPàg`) i mode opcional d'`Option (⌥)` dret com a `Fn` (`Supr`, navegació i `F1–F12`), aplicats només a maquinari d'Apple (`VID_05AC` / `004C`).
- **Disseny natiu unificat i multiidioma (`Català`, `English`, `Español`):**
  - **macOS:** Swift + SwiftUI natiu (`macOS 13+`).
  - **Windows 11:** C# + WinUI 3 (.NET 8) + Servei en segon pla a la safata del sistema + Instal·lador `.msi` (WiX v5).

### Descàrrega o compilació

1. **Instal·ladors a punt per utilitzar (GitHub Releases):**
   - **macOS:** Baixa `MagicKeyboardSuite-macOS.dmg` des de la secció **Releases** de GitHub.
   - **Windows 10/11 (x64):** Baixa `MagicKeyboardSuite-Setup.msi` des de la secció **Releases** de GitHub.
2. **Compilar des del codi font:**

```bash
# macOS (.app + .dmg)
./MagicKeyboardSuite/macOS/build.sh --dmg
```

```powershell
# Windows 10/11 x64 (.msi)
powershell -ExecutionPolicy Bypass -File .\MagicKeyboardSuite\Windows\build.ps1
```

---

## English

**Magic Keyboard Suite** brings a unified **Apple Magic Keyboard** experience to **macOS** and **Windows 11**: switch computers in one click while keeping identical keycap mapping, `Option (⌥)` symbols, and `Command (⌘)` shortcuts.

### Key features

- **Bluetooth & USB Cable (Lightning / USB-C):** Works wirelessly (1-click Bluetooth pairing handover between Mac and PC) as well as plugged in via USB.
- **Live Battery Level Badge:** Displays real-time battery percentage on macOS (`IORegistry`) and Windows (PnP/GATT).
- **True Apple Layout & ISO Fix:**
  - 17 official Apple layouts (with automatic Boot Camp layout importer during Windows onboarding) and automatic ISO key swap (`[º ª \]` next to `1` and `[< >]` next to left `Shift`).
  - Left `Option (⌥)` types `@ # € [ ] { } \ | ~` identically on macOS and Windows.
  - Instant layout switching on macOS (`Carbon TIS`) and Windows.
- **Mac Shortcuts on Windows:** `⌘ + C/V/X/Z/A/S/F/T/W`, `⌘ + Tab`, `⌘ + Q`, `⌘ + Space`, `⌘ + Arrows` (`Home / End / Page Up / Page Down`), plus an optional **Right `Option (⌥)` as `Fn`** mode (`Delete`, navigation, and `F1–F12`), scoped exclusively to Apple hardware (`VID_05AC` / `004C`).
- **Native SwiftUI & WinUI 3 Apps (`Català`, `English`, `Español`):** Powered by a single source of truth in [`MagicKeyboardSuite/shared/`](MagicKeyboardSuite/shared/).

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
- **Diseño nativo unificado y multiidioma (`Català`, `English`, `Español`):**
  - **macOS:** Swift + SwiftUI nativo (`macOS 13+`).
  - **Windows 11:** C# + WinUI 3 (.NET 8) + Agente en bandeja del sistema + Instalador `.msi` (WiX v5).

---

## Avís legal / Disclaimer / Aviso legal

Projecte independent de codi obert sota llicència [GPLv3](LICENSE). No està afiliat, patrocinat ni avalat per Apple Inc. ni per Microsoft Corporation. *"Apple"*, *"Magic Keyboard"*, *"macOS"* i *"Boot Camp"* són marques registrades d'Apple Inc. *"Windows"* és marca registrada de Microsoft Corporation.

*Independent open-source project licensed under [GPLv3](LICENSE). Not affiliated with, sponsored by, or endorsed by Apple Inc. or Microsoft Corporation.*
