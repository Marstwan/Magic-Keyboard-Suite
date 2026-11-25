# Magic Keyboard Suite

<p align="center">
  <img src="shared/assets/icon-1024.png" alt="Magic Keyboard Suite Icon" width="108" height="108" />
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
  - 17 disposicions oficials d'Apple (amb importador automàtic verificat amb signatura Authenticode des de Boot Camp a Windows) i intercanvi ISO automàtic (`[º ª \]` al costat de l'`1` i `[< >]` al costat de `Majúscules` esquerre).
  - `Option (⌥)` esquerra escriu els símbols de tercer nivell (`@ # € [ ] { } \ | ~`) igual a macOS i a Windows en qualsevol de les 17 disposicions.
  - Canvi directe de disposició activa a macOS (`Carbon TIS`) i a Windows.
- **Dreceres de Mac a Windows:** `⌘ + C/V/X/Z/A/S/F/T/W`, `⌘ + Tab` (mantenint `⌘` per navegar entre apps), `⌘ + Q`, `⌘ + Espai`, `⌘ + Fletxes` (`Inici / Fi / RePàg / AvPàg`) i mode opcional d'`Option (⌥)` dret com a `Fn` (`Supr`, navegació i `F1–F12`), aplicats només a maquinari d'Apple (`VID_05AC` / `004C`).
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
./macos/build.sh --dmg
```

```powershell
# Windows 10/11 x64 (.msi)
powershell -ExecutionPolicy Bypass -File .\windows\build.ps1
```

### Disposicions oficials d'Apple a Windows (drivers de Boot Camp)

A **macOS** les 17 disposicions ja venen integrades de sèrie. A **Windows**, el teclat, les dreceres `⌘` i l'intercanvi ISO funcionen des del primer segon amb la disposició estàndard del sistema, però per tenir **exactament la mateixa distribució impresa per Apple al 100 %** (com ara el punt volat `·` a `Majúscules + 3` o tots els símbols exactes d'`Option`) cal importar les disposicions oficials de **Boot Camp** (no es redistribueixen dins del repositori per ser programari propietari d'Apple):

1. Baixa el paquet `.zip` oficial de **Boot Camp Support Software** directament des del Suport d'Apple: **[support.apple.com/106378](https://support.apple.com/106378)** *(alternativament, pots obtenir la carpeta amb l'Assistent Boot Camp en un Mac Intel o amb [Brigadier](https://github.com/timsutton/brigadier))*.
2. **Important:** **No descomprimeixis ni executis el `setup.exe` de Boot Camp** al teu PC amb Windows (aquell instal·lador és exclusiu per a ordinadors Mac).
3. Obre **Magic Keyboard Suite** a Windows (a la benvinguda inicial o a la pestanya *Disposició i idioma*), prem **«Triar .zip…»** i selecciona el fitxer `.zip` descarregat: l'aplicació n'extraurà només les 17 llibreries de teclat (`*A.dll`), en comprovarà la signatura digital oficial d'Apple i les instal·larà en segons.

---

## English

**Magic Keyboard Suite** brings a unified **Apple Magic Keyboard** experience to **macOS** and **Windows 11**: switch computers in one click while keeping identical keycap mapping, `Option (⌥)` symbols, and `Command (⌘)` shortcuts.

### Key features

- **Bluetooth & USB Cable (Lightning / USB-C):** Works wirelessly (1-click Bluetooth pairing handover between Mac and PC) as well as plugged in via USB.
- **Live Battery Level Badge:** Displays real-time battery percentage on macOS (`IORegistry`) and Windows (PnP/GATT).
- **True Apple Layout & ISO Fix:**
  - 17 official Apple layouts (with Authenticode-verified Boot Camp layout importer during Windows onboarding) and automatic ISO key swap (`[º ª \]` next to `1` and `[< >]` next to left `Shift`).
  - Left `Option (⌥)` types third-level symbols (`@ # € [ ] { } \ | ~`) identically on macOS and Windows across all 17 layouts.
  - Instant layout switching on macOS (`Carbon TIS`) and Windows.
- **Mac Shortcuts on Windows:** `⌘ + C/V/X/Z/A/S/F/T/W`, `⌘ + Tab` (persistent app switcher while holding `⌘`), `⌘ + Q`, `⌘ + Space`, `⌘ + Arrows` (`Home / End / Page Up / Page Down`), plus an optional **Right `Option (⌥)` as `Fn`** mode (`Delete`, navigation, and `F1–F12`), scoped exclusively to Apple hardware (`VID_05AC` / `004C`).
- **Native SwiftUI & WinUI 3 Apps (`Català`, `English`, `Español`):** Powered by a single source of truth in [`shared/`](shared/).

### Official Apple Keyboard Layouts on Windows (Boot Camp Drivers)

On **macOS**, all 17 layouts are built into the OS. On **Windows**, Bluetooth handover, `⌘` shortcuts, and ISO key swapping work right out of the box using Windows' built-in layouts as a fallback, but to get **100% pixel-accurate Apple keycap mappings** you should import Apple's official **Boot Camp** keyboard layout DLLs (not bundled in this repository because they are proprietary Apple software):

1. Download the official **Boot Camp Support Software** `.zip` package directly from Apple Support: **[support.apple.com/106378](https://support.apple.com/106378)** *(alternatively, fetch a Boot Camp folder via Boot Camp Assistant on an Intel Mac or [Brigadier](https://github.com/timsutton/brigadier))*.
2. **Important:** **Do NOT unzip or run Boot Camp's `setup.exe`** on a standard Windows PC (that full installer is meant only for Mac hardware).
3. Open **Magic Keyboard Suite** on Windows (during initial onboarding or in the *Layout & language* tab), click **“Select .zip…”**, and pick the downloaded `.zip` file: the app automatically extracts only the 17 keyboard layout files (`*A.dll`), verifies Apple's Authenticode signature, and registers them in seconds.

---

## Español

**Magic Keyboard Suite** unifica la experiencia de tu **Apple Magic Keyboard** entre **macOS** y **Windows 11**: cambia de equipo en un clic y mantén exactamente la misma disposición física, símbolos con `Option (⌥)` y atajos con `Command (⌘)`.

### Características principales

- **Bluetooth y cable USB (Lightning / USB-C):** Funciona tanto de forma inalámbrica (liberando y reconectando el emparejamiento Bluetooth entre Mac y PC en un clic) como conectado por cable USB.
- **Indicador de batería en tiempo real:** Lee el porcentaje de batería de tus teclados conectados (`IORegistry` en macOS y PnP/GATT en Windows).
- **Cada tecla coincide con su serigrafía:**
  - 17 disposiciones oficiales de Apple (con importador automático verificado mediante firma Authenticode desde Boot Camp en Windows) e intercambio ISO automático (`[º ª \]` junto al `1` y `[< >]` junto a `Mayús` izquierdo).
  - `Option (⌥)` izquierda escribe los símbolos de tercer nivel (`@ # € [ ] { } \ | ~`) igual en macOS y en Windows en cualquiera de las 17 disposiciones.
  - Cambio directo de disposición activa en macOS (`Carbon TIS`) y en Windows.
- **Atajos de Mac en Windows:** `⌘ + C/V/X/Z/A/S/F/T/W`, `⌘ + Tab` (manteniendo `⌘` para cambiar entre varias apps), `⌘ + Q`, `⌘ + Espacio`, `⌘ + Flechas` (`Inicio / Fin / RePág / AvPág`) y modo opcional de `Option (⌥)` derecho como `Fn` (`Supr`, navegación y `F1–F12`), aplicados solo a hardware de Apple (`VID_05AC` / `004C`).
- **Diseño nativo unificado y multiidioma (`Català`, `English`, `Español`):**
  - **macOS:** Swift + SwiftUI nativo (`macOS 13+`).
  - **Windows 11:** C# + WinUI 3 (.NET 8) + Agente en bandeja del sistema + Instalador `.msi` (WiX v5).

### Disposiciones oficiales de Apple en Windows (drivers de Boot Camp)

En **macOS** las 17 disposiciones ya vienen incluidas de serie. En **Windows**, el teclado, los atajos `⌘` y el intercambio ISO funcionan desde el primer segundo usando las disposiciones estándar de Windows como respaldo, pero para tener **exactamente el 100 % de la serigrafía oficial de Apple** (por ejemplo `·` en `Mayús + 3` y todos los caracteres terciarios exactos) conviene importar las disposiciones oficiales de **Boot Camp** (no se redistribuyen en el repositorio por ser software propietario de Apple):

1. Descarga el paquete `.zip` oficial de **Boot Camp Support Software** directamente desde la web de Soporte de Apple: **[support.apple.com/106378](https://support.apple.com/106378)** *(también puedes obtener la carpeta desde el Asistente Boot Camp en un Mac Intel o con [Brigadier](https://github.com/timsutton/brigadier))*.
2. **Importante:** **No descomprimas ni ejecutes el `setup.exe` de Boot Camp** en un PC normal con Windows (ese instalador completo es solo para ordenadores Mac).
3. Abre **Magic Keyboard Suite** en Windows (en la pantalla de bienvenida o en la pestaña *Disposición e idioma*), pulsa **«Elegir .zip…»** y selecciona directamente el `.zip` descargado: la aplicación extraerá únicamente las 17 librerías de teclado (`*A.dll`), verificará la firma digital oficial de Apple y las instalará en segundos.

---

## Avís legal / Disclaimer / Aviso legal

Projecte independent de codi obert sota llicència [GPLv3](LICENSE). No està afiliat, patrocinat ni avalat per Apple Inc. ni per Microsoft Corporation. *"Apple"*, *"Magic Keyboard"*, *"macOS"* i *"Boot Camp"* són marques registrades d'Apple Inc. *"Windows"* és marca registrada de Microsoft Corporation.

*Independent open-source project licensed under [GPLv3](LICENSE). Not affiliated with, sponsored by, or endorsed by Apple Inc. or Microsoft Corporation.*
