// Archivo generado por shared/tools/generate.py. NO EDITAR: cambia shared/ y vuelve a generar.
import Foundation

enum L10n {
    static let supported = "es,ca,en".split(separator: ",").map(String.init)
    /// Idioma forzado por el usuario ("es", "ca", "en"), o nil para usar el idioma del sistema.
    static var overrideLanguage: String? = nil
    /// Idioma detectado del sistema.
    static let systemLanguage: String = {
        for pref in Locale.preferredLanguages {
            let code = String(pref.prefix(2)).lowercased()
            if supported.contains(code) { return code }
        }
        return "es"
    }()
    /// Idioma activo.
    static var language: String {
        if let over = overrideLanguage, supported.contains(over) { return over }
        return systemLanguage
    }

    private static let tables: [String: [String: String]] = [
        "es": [
            "app.name": "Magic Keyboard Suite",
            "app.version": "Versión {0}",
            "nav.bluetooth": "Teclados Bluetooth",
            "nav.layout": "Disposición e idioma",
            "nav.shortcuts": "Atajos y multimedia",
            "nav.guide": "Guía Mac ↔ Windows",
            "footer.engineActive": "Motor activo",
            "footer.engineInactive": "Motor detenido",
            "footer.currentLayout": "Disposición: {0}",
            "bt.title": "Teclados Bluetooth",
            "bt.subtitle": "Conecta tu teclado a este Mac o libéralo para usarlo en Windows",
            "bt.refresh": "Actualizar",
            "bt.scan": "Buscar teclado cercano",
            "bt.scanning": "Buscando…",
            "bt.empty.title": "No se ha encontrado ningún teclado",
            "bt.empty.body": "Apaga y enciende el interruptor del teclado y pulsa «Buscar teclado cercano», o amplía el filtro de abajo.",
            "bt.mac": "MAC {0}",
            "bt.appleKeyboard": "Teclado Apple",
            "bt.battery": "{0}%",
            "bt.filter.title": "Dispositivos mostrados",
            "bt.filter.subtitle": "Elige qué dispositivos Bluetooth aparecen en la lista",
            "bt.filter.apple": "Solo teclados Apple",
            "bt.filter.keyboards": "Todos los teclados",
            "bt.filter.all": "Todos los dispositivos",
            "state.connected": "Conectado a este Mac",
            "state.paired": "Emparejado · desconectado",
            "state.released": "Liberado para Windows",
            "state.nearby": "Visible cerca",
            "action.connect": "Conectar en este Mac",
            "action.release": "Liberar para Windows",
            "action.disconnect": "Desconectar",
            "status.idle.title": "Cambia de equipo en un clic",
            "status.idle.body": "Pulsa «Liberar para Windows» para llevarte el teclado al PC. Para volver, apaga y enciende el teclado y pulsa «Conectar en este Mac».",
            "status.releasing.title": "Liberando «{0}»…",
            "status.releasing.body": "Cerrando la conexión y olvidando el emparejamiento en este equipo.",
            "status.released.title": "«{0}» liberado",
            "status.released.body": "Apaga y enciende el teclado y pulsa «Conectar en este PC» en la app de Windows.",
            "status.disconnecting.title": "Desconectando «{0}»…",
            "status.disconnected.title": "«{0}» desconectado",
            "status.disconnected.body": "Sigue emparejado, pero no está conectado.",
            "status.connecting.title": "Conectando «{0}»…",
            "status.connecting.body": "Asegúrate de haber apagado y encendido el interruptor del teclado.",
            "status.connected.title": "«{0}» conectado",
            "status.connected.body": "El teclado está listo para usar.",
            "status.pairing.pin": "Escribe {0} en el teclado y pulsa Intro.",
            "status.pairing.failed.title": "No se ha podido emparejar «{0}»",
            "status.pairing.failed.body": "Abriendo los ajustes de Bluetooth del sistema para terminar el emparejamiento.",
            "status.scanning.title": "Buscando teclados cercanos…",
            "status.scanning.body": "Apaga y vuelve a encender el teclado para que sea visible.",
            "status.scanDone.title": "Búsqueda completada",
            "status.scanDone.body": "Se ha actualizado la lista de teclados.",
            "status.notFound.title": "No se ha encontrado el dispositivo {0}",
            "status.error.title": "Algo ha fallado",
            "layout.title": "Disposición e idioma",
            "layout.subtitle": "Disposición oficial de Apple para que cada tecla coincida con lo que tiene impreso",
            "layout.active.title": "Disposición de Apple",
            "layout.active.subtitle": "Disposición activa en macOS: {0}",
            "layout.drivers.title": "Drivers oficiales de Apple (Boot Camp)",
            "layout.drivers.installed": "Instalados en el sistema (17 disposiciones oficiales de Apple listas para usar)",
            "layout.drivers.missing": "No están instalados. Descarga el paquete Boot Camp de Apple y selecciona la carpeta extraída o el archivo .zip: la app buscará sola los archivos que necesita.",
            "layout.drivers.badgeInstalled": "Instalados",
            "layout.drivers.badgeMissing": "Pendiente",
            "layout.drivers.selectFolder": "Elegir carpeta…",
            "layout.drivers.selectZip": "Elegir .zip…",
            "layout.drivers.installing": "Buscando e instalando las disposiciones de Apple…",
            "layout.drivers.success": "Se han instalado correctamente {0} disposiciones oficiales de Apple.",
            "layout.drivers.notFound": "No se ha encontrado BootCamp.msi ni los drivers de teclado (*A.dll) en la ruta seleccionada.",
            "layout.iso.title": "Teclas ISO [º ª \\] y [< >]",
            "layout.iso.subtitle": "[º ª \\] junto al 1 y [< >] junto al Mayús izquierdo, igual en macOS y en Windows",
            "layout.option.title": "Option (⌥) izquierda para símbolos (@ # € [ ] { } \\ | ~)",
            "layout.option.subtitle": "Escribe los símbolos de la misma forma en macOS y en Windows",
            "layout.appleOnly.title": "Aplicar solo a teclados Apple",
            "layout.appleOnly.subtitle": "Si conectas otro teclado a la vez, ese mantiene el comportamiento estándar",
            "layout.remember.title": "Recordar teclados liberados",
            "layout.remember.subtitle": "Guarda los teclados liberados para reconectarlos en un clic al volver",
            "layout.appLanguage.title": "Idioma de la aplicación",
            "layout.appLanguage.subtitle": "Elige el idioma de la interfaz (Català, Español o English)",
            "layout.appLanguage.auto": "Automático (del sistema)",
            "layout.appLanguage.ca": "Català",
            "layout.appLanguage.es": "Español",
            "layout.appLanguage.en": "English",
            "layout.native": "Nativo en macOS",
            "shortcuts.title": "Atajos y multimedia",
            "shortcuts.subtitle": "Command (⌘), Supr, Inicio/Fin y acceso a los ajustes de teclas de función",
            "shortcuts.modifiers.title": "Command (⌘) y Control (⌃)",
            "shortcuts.modifiers.subtitle": "⌘+C/V/X/Z/A/S/F/T/W, ⌘+Tab para cambiar de app, ⌘+Q para cerrar y ⌘+Espacio para buscar",
            "shortcuts.modifiers.mac": "Modo Mac (recomendado)",
            "shortcuts.modifiers.swap": "Intercambio total (⌘ ↔ Ctrl)",
            "shortcuts.modifiers.windows": "Estándar de Windows (⌘ = Win)",
            "shortcuts.modifiers.macNative": "Nativo de macOS",
            "shortcuts.modifiers.macCustom": "Personalizar en Ajustes…",
            "shortcuts.nav.title": "Supr y navegación con ⌘ + flechas",
            "shortcuts.nav.subtitle": "Fn + Retroceso = Supr  •  ⌘ + ←/→ = Inicio/Fin  •  ⌘ + ↑/↓ = RePág/AvPág",
            "shortcuts.media.title": "Teclas de función (F1–F12) y ajustes del teclado",
            "shortcuts.media.subtitle": "El comportamiento de la fila superior depende del modelo de teclado y de la opción «Usar F1, F2, etc. como teclas de función estándar» en Ajustes del Sistema",
            "shortcuts.rightOptionFn.title": "Usar Option (⌥) derecho como tecla Fn",
            "shortcuts.rightOptionFn.subtitle": "Windows no permite leer la tecla Fn física del Magic Keyboard sin un driver de kernel. Actívalo para usar Option (⌥) derecho como Fn (Supr, Inicio/Fin y F1–F12); los símbolos siguen disponibles con Option izquierdo",
            "shortcuts.openKeyboardSettings": "Abrir ajustes de teclado…",
            "guide.title": "Guía Mac ↔ Windows",
            "guide.subtitle": "Cómo pasar el teclado de un equipo a otro y mapa rápido de símbolos",
            "guide.toWindows.title": "Del Mac a Windows",
            "guide.toWindows.1": "En el Mac, abre Magic Keyboard Suite y pulsa «Liberar para Windows» en tu teclado.",
            "guide.toWindows.2": "Apaga el interruptor del teclado, espera 2 segundos y vuelve a encenderlo.",
            "guide.toWindows.3": "En Windows, abre Magic Keyboard Suite y pulsa «Conectar en este PC».",
            "guide.toMac.title": "De Windows al Mac",
            "guide.toMac.1": "En Windows, abre Magic Keyboard Suite y pulsa «Liberar para el Mac» en tu teclado.",
            "guide.toMac.2": "Apaga el interruptor del teclado, espera 2 segundos y vuelve a encenderlo.",
            "guide.toMac.3": "En el Mac, abre Magic Keyboard Suite y pulsa «Conectar en este Mac».",
            "guide.symbols.title": "Mapa de símbolos (Español / Català ISO de Apple)",
            "guide.symbols.1": "Option (⌥) + 2 = @    •    Option (⌥) + 3 = #    •    Option (⌥) + E = €    •    Option (⌥) + 1 = |",
            "guide.symbols.2": "Option (⌥) + º = \\    •    Option (⌥) + ` = [    •    Option (⌥) + + = ]    •    Option (⌥) + ´ = {",
            "guide.symbols.3": "Option (⌥) + Ç = }    •    Option (⌥) + Ñ = ~    •    Mayús + 3 = · (punt volat)",
            "uninstall.title": "Desinstalar Magic Keyboard Suite",
            "uninstall.subtitle": "Mueve la app a la Papelera y borra sus ajustes",
            "uninstall.button": "Desinstalar…",
            "toggle.on": "Activado",
            "toggle.off": "Desactivado",
            "tray.open": "Abrir Magic Keyboard Suite",
            "tray.keyboards": "Teclados",
            "tray.noKeyboards": "Ningún teclado",
            "tray.launchAtLogin": "Abrir al iniciar sesión",
            "tray.quit": "Salir",
            "onboarding.setup.badge": "Configuración inicial",
            "onboarding.setup.title": "Bienvenido a Magic Keyboard Suite",
            "onboarding.setup.subtitle": "Elige el idioma de la aplicación antes de continuar.",
            "onboarding.nextButton": "Continuar →",
            "onboarding.backButton": "← Atrás",
            "onboarding.badge": "Guía de inicio rápido",
            "onboarding.title": "Cómo funciona Magic Keyboard Suite",
            "onboarding.subtitle": "Cómo usar la aplicación y sacarle todo el partido a tu Magic Keyboard en macOS y Windows",
            "onboarding.step1.title": "1. Cambia el teclado entre Mac y Windows en un clic",
            "onboarding.step1.body": "El Magic Keyboard solo guarda un emparejamiento Bluetooth activo a la vez. Antes de llevártelo al otro equipo, pulsa «Liberar» en su tarjeta, apaga y enciende el interruptor físico del teclado y pulsa «Conectar» en el otro ordenador.",
            "onboarding.step2.title": "2. Cada tecla coincide con lo que tiene impreso",
            "onboarding.step2.body": "La disposición oficial de Apple y la corrección ISO colocan [º ª \\] junto al 1 y [< >] junto al Mayús izquierdo. Además, Option (⌥) izquierda escribe @ # € [ ] { } \\ | ~ exactamente igual en ambos sistemas.",
            "onboarding.step3.title": "3. Tus atajos de Mac funcionan igual en ambos equipos",
            "onboarding.step3.body": "Usa Command (⌘) + C/V/X/Z/A/S/F/T/W, ⌘ + Tab para cambiar de aplicación, Fn + Retroceso para Supr (borrar hacia delante), ⌘ + flechas para Inicio/Fin y F7–F12 como controles multimedia.",
            "onboarding.reopenHint": "Puedes volver a ver esta guía cuando quieras desde el botón «Ver bienvenida» en la pestaña Guía.",
            "onboarding.startButton": "Empezar a usar Magic Keyboard Suite",
            "onboarding.reopenButton": "Ver guía de bienvenida…",
        ],
        "ca": [
            "app.name": "Magic Keyboard Suite",
            "app.version": "Versió {0}",
            "nav.bluetooth": "Teclats Bluetooth",
            "nav.layout": "Disposició i idioma",
            "nav.shortcuts": "Dreceres i multimèdia",
            "nav.guide": "Guia Mac ↔ Windows",
            "footer.engineActive": "Motor actiu",
            "footer.engineInactive": "Motor aturat",
            "footer.currentLayout": "Disposició: {0}",
            "bt.title": "Teclats Bluetooth",
            "bt.subtitle": "Connecta el teu teclat a aquest Mac o allibera'l per utilitzar-lo a Windows",
            "bt.refresh": "Actualitzar",
            "bt.scan": "Cercar teclat proper",
            "bt.scanning": "Cercant…",
            "bt.empty.title": "No s'ha trobat cap teclat",
            "bt.empty.body": "Apaga i encén l'interruptor del teclat i prem «Cercar teclat proper», o amplia el filtre de sota.",
            "bt.mac": "MAC {0}",
            "bt.appleKeyboard": "Teclat Apple",
            "bt.battery": "{0}%",
            "bt.filter.title": "Dispositius mostrats",
            "bt.filter.subtitle": "Tria quins dispositius Bluetooth apareixen a la llista",
            "bt.filter.apple": "Només teclats Apple",
            "bt.filter.keyboards": "Tots els teclats",
            "bt.filter.all": "Tots els dispositius",
            "state.connected": "Connectat a aquest Mac",
            "state.paired": "Aparellat · desconnectat",
            "state.released": "Alliberat per a Windows",
            "state.nearby": "Visible a prop",
            "action.connect": "Connectar en aquest Mac",
            "action.release": "Alliberar per a Windows",
            "action.disconnect": "Desconnectar",
            "status.idle.title": "Canvia d'equip en un clic",
            "status.idle.body": "Prem «Alliberar per a Windows» per endur-te el teclat al PC. Per tornar, apaga i encén el teclat i prem «Connectar en aquest Mac».",
            "status.releasing.title": "Alliberant «{0}»…",
            "status.releasing.body": "Tancant la connexió i oblidant l'aparellament en aquest equip.",
            "status.released.title": "«{0}» alliberat",
            "status.released.body": "Apaga i encén el teclat i prem «Connectar en aquest PC» a l'app de Windows.",
            "status.disconnecting.title": "Desconnectant «{0}»…",
            "status.disconnected.title": "«{0}» desconnectat",
            "status.disconnected.body": "Segueix aparellat, però no està connectat.",
            "status.connecting.title": "Connectant «{0}»…",
            "status.connecting.body": "Assegura't d'haver apagat i encès l'interruptor del teclat.",
            "status.connected.title": "«{0}» connectat",
            "status.connected.body": "El teclat està a punt per utilitzar-se.",
            "status.pairing.pin": "Escriu {0} al teclat i prem Retorn.",
            "status.pairing.failed.title": "No s'ha pogut aparellar «{0}»",
            "status.pairing.failed.body": "Obrint la configuració de Bluetooth del sistema per acabar l'aparellament.",
            "status.scanning.title": "Cercant teclats propers…",
            "status.scanning.body": "Apaga i torna a encendre el teclat perquè sigui visible.",
            "status.scanDone.title": "Cerca completada",
            "status.scanDone.body": "S'ha actualitzat la llista de teclats.",
            "status.notFound.title": "No s'ha trobat el dispositiu {0}",
            "status.error.title": "Alguna cosa ha fallat",
            "layout.title": "Disposició i idioma",
            "layout.subtitle": "Disposició oficial d'Apple perquè cada tecla coincideixi amb el que hi té imprès",
            "layout.active.title": "Disposició d'Apple",
            "layout.active.subtitle": "Disposició activa a macOS: {0}",
            "layout.drivers.title": "Drivers oficials d'Apple (Boot Camp)",
            "layout.drivers.installed": "Instal·lats al sistema (17 disposicions oficials d'Apple a punt per utilitzar)",
            "layout.drivers.missing": "No estan instal·lats. Baixa el paquet Boot Camp d'Apple i selecciona la carpeta extreta o el fitxer .zip: l'app cercarà sola els fitxers que necessita.",
            "layout.drivers.badgeInstalled": "Instal·lats",
            "layout.drivers.badgeMissing": "Pendent",
            "layout.drivers.selectFolder": "Triar carpeta…",
            "layout.drivers.selectZip": "Triar .zip…",
            "layout.drivers.installing": "Cercant i instal·lant les disposicions d'Apple…",
            "layout.drivers.success": "S'han instal·lat correctament {0} disposicions oficials d'Apple.",
            "layout.drivers.notFound": "No s'ha trobat BootCamp.msi ni els drivers de teclat (*A.dll) a la ruta seleccionada.",
            "layout.iso.title": "Tecles ISO [º ª \\] i [< >]",
            "layout.iso.subtitle": "[º ª \\] al costat de l'1 i [< >] al costat de Majúscules esquerre, igual a macOS i a Windows",
            "layout.option.title": "Option (⌥) esquerra per a símbols (@ # € [ ] { } \\ | ~)",
            "layout.option.subtitle": "Escriu els símbols de la mateixa manera a macOS i a Windows",
            "layout.appleOnly.title": "Aplicar només a teclats Apple",
            "layout.appleOnly.subtitle": "Si connectes un altre teclat alhora, aquell manté el comportament estàndard",
            "layout.remember.title": "Recordar teclats alliberats",
            "layout.remember.subtitle": "Desa els teclats alliberats per tornar-los a connectar en un clic",
            "layout.appLanguage.title": "Idioma de l'aplicació",
            "layout.appLanguage.subtitle": "Tria l'idioma de la interfície (Català, Español o English)",
            "layout.appLanguage.auto": "Automàtic (del sistema)",
            "layout.appLanguage.ca": "Català",
            "layout.appLanguage.es": "Español",
            "layout.appLanguage.en": "English",
            "layout.native": "Nadiu a macOS",
            "shortcuts.title": "Dreceres i multimèdia",
            "shortcuts.subtitle": "Command (⌘), Supr, Inici/Fi i accés als ajustos de tecles de funció",
            "shortcuts.modifiers.title": "Command (⌘) i Control (⌃)",
            "shortcuts.modifiers.subtitle": "⌘+C/V/X/Z/A/S/F/T/W, ⌘+Tab per canviar d'app, ⌘+Q per tancar i ⌘+Espai per cercar",
            "shortcuts.modifiers.mac": "Mode Mac (recomanat)",
            "shortcuts.modifiers.swap": "Intercanvi total (⌘ ↔ Ctrl)",
            "shortcuts.modifiers.windows": "Estàndard de Windows (⌘ = Win)",
            "shortcuts.modifiers.macNative": "Nadiu de macOS",
            "shortcuts.modifiers.macCustom": "Personalitzar a Configuració…",
            "shortcuts.nav.title": "Supr i navegació amb ⌘ + fletxes",
            "shortcuts.nav.subtitle": "Fn + Retrocés = Supr  •  ⌘ + ←/→ = Inici/Fi  •  ⌘ + ↑/↓ = RePàg/AvPàg",
            "shortcuts.media.title": "Tecles de funció (F1–F12) i configuració del teclat",
            "shortcuts.media.subtitle": "El comportament de la fila superior depèn del model de teclat i de l'opció «Utilitzar F1, F2, etc. com a tecles de funció estàndard» a Configuració del Sistema",
            "shortcuts.rightOptionFn.title": "Utilitzar Option (⌥) dret com a tecla Fn",
            "shortcuts.rightOptionFn.subtitle": "Windows no permet llegir la tecla Fn física del Magic Keyboard sense un driver de nucli. Activa-ho per utilitzar Option (⌥) dret com a Fn (Supr, Inici/Fi i F1–F12); els símbols continuen disponibles amb Option esquerre",
            "shortcuts.openKeyboardSettings": "Obrir configuració del teclat…",
            "guide.title": "Guia Mac ↔ Windows",
            "guide.subtitle": "Com passar el teclat d'un equip a un altre i mapa ràpid de símbols",
            "guide.toWindows.title": "Del Mac a Windows",
            "guide.toWindows.1": "Al Mac, obre Magic Keyboard Suite i prem «Alliberar per a Windows» al teu teclat.",
            "guide.toWindows.2": "Apaga l'interruptor del teclat, espera 2 segons i torna'l a encendre.",
            "guide.toWindows.3": "A Windows, obre Magic Keyboard Suite i prem «Connectar en aquest PC».",
            "guide.toMac.title": "De Windows al Mac",
            "guide.toMac.1": "A Windows, obre Magic Keyboard Suite i prem «Alliberar per al Mac» al teu teclat.",
            "guide.toMac.2": "Apaga l'interruptor del teclat, espera 2 segons i torna'l a encendre.",
            "guide.toMac.3": "Al Mac, obre Magic Keyboard Suite i prem «Connectar en aquest Mac».",
            "guide.symbols.title": "Mapa de símbols (Espanyol / Català ISO d'Apple)",
            "guide.symbols.1": "Option (⌥) + 2 = @    •    Option (⌥) + 3 = #    •    Option (⌥) + E = €    •    Option (⌥) + 1 = |",
            "guide.symbols.2": "Option (⌥) + º = \\    •    Option (⌥) + ` = [    •    Option (⌥) + + = ]    •    Option (⌥) + ´ = {",
            "guide.symbols.3": "Option (⌥) + Ç = }    •    Option (⌥) + Ñ = ~    •    Majúscules + 3 = · (punt volat)",
            "uninstall.title": "Desinstal·lar Magic Keyboard Suite",
            "uninstall.subtitle": "Mou l'app a la Paperera i n'esborra la configuració",
            "uninstall.button": "Desinstal·lar…",
            "toggle.on": "Activat",
            "toggle.off": "Desactivat",
            "tray.open": "Obrir Magic Keyboard Suite",
            "tray.keyboards": "Teclats",
            "tray.noKeyboards": "Cap teclat",
            "tray.launchAtLogin": "Obrir en iniciar sessió",
            "tray.quit": "Sortir",
            "onboarding.setup.badge": "Configuració inicial",
            "onboarding.setup.title": "Benvingut a Magic Keyboard Suite",
            "onboarding.setup.subtitle": "Tria l'idioma de l'aplicació abans de continuar.",
            "onboarding.nextButton": "Continuar →",
            "onboarding.backButton": "← Enrere",
            "onboarding.badge": "Guia d'inici ràpid",
            "onboarding.title": "Com funciona Magic Keyboard Suite",
            "onboarding.subtitle": "Com utilitzar l'aplicació i treure tot el partit al teu Magic Keyboard a macOS i Windows",
            "onboarding.step1.title": "1. Canvia el teclat entre Mac i Windows en un clic",
            "onboarding.step1.body": "El Magic Keyboard només desa un aparellament Bluetooth actiu alhora. Abans d'endur-te'l a l'altre equip, prem «Alliberar» a la seva targeta, apaga i encén l'interruptor físic del teclat i prem «Connectar» a l'altre ordinador.",
            "onboarding.step2.title": "2. Cada tecla coincideix amb el que hi té imprès",
            "onboarding.step2.body": "La disposició oficial d'Apple i la correcció ISO col·loquen [º ª \\] al costat de l'1 i [< >] al costat de Majúscules esquerre. A més, Option (⌥) esquerra escriu @ # € [ ] { } \\ | ~ exactament igual en tots dos sistemes.",
            "onboarding.step3.title": "3. Les teves dreceres de Mac funcionen igual en tots dos equips",
            "onboarding.step3.body": "Utilitza Command (⌘) + C/V/X/Z/A/S/F/T/W, ⌘ + Tab per canviar d'aplicació, Fn + Retrocés per a Supr (esborrar cap endavant) i ⌘ + fletxes per a Inici/Fi.",
            "onboarding.reopenHint": "Pots tornar a veure aquesta guia quan vulguis des del botó «Veure guia de benvinguda» a la pestanya Guia.",
            "onboarding.startButton": "Començar a utilitzar Magic Keyboard Suite",
            "onboarding.reopenButton": "Veure guia de benvinguda…",
        ],
        "en": [
            "app.name": "Magic Keyboard Suite",
            "app.version": "Version {0}",
            "nav.bluetooth": "Bluetooth keyboards",
            "nav.layout": "Layout & language",
            "nav.shortcuts": "Shortcuts & media",
            "nav.guide": "Mac ↔ Windows guide",
            "footer.engineActive": "Engine running",
            "footer.engineInactive": "Engine stopped",
            "footer.currentLayout": "Layout: {0}",
            "bt.title": "Bluetooth keyboards",
            "bt.subtitle": "Connect your keyboard to this Mac or release it to use it on Windows",
            "bt.refresh": "Refresh",
            "bt.scan": "Find nearby keyboard",
            "bt.scanning": "Searching…",
            "bt.empty.title": "No keyboards found",
            "bt.empty.body": "Turn the keyboard off and on, then click “Find nearby keyboard”, or widen the filter below.",
            "bt.mac": "MAC {0}",
            "bt.appleKeyboard": "Apple keyboard",
            "bt.battery": "{0}%",
            "bt.filter.title": "Devices shown",
            "bt.filter.subtitle": "Choose which Bluetooth devices appear in the list",
            "bt.filter.apple": "Apple keyboards only",
            "bt.filter.keyboards": "All keyboards",
            "bt.filter.all": "All devices",
            "state.connected": "Connected to this Mac",
            "state.paired": "Paired · disconnected",
            "state.released": "Released for Windows",
            "state.nearby": "Nearby",
            "action.connect": "Connect to this Mac",
            "action.release": "Release for Windows",
            "action.disconnect": "Disconnect",
            "status.idle.title": "Switch computers in one click",
            "status.idle.body": "Click “Release for Windows” to take the keyboard to your PC. To come back, turn the keyboard off and on and click “Connect to this Mac”.",
            "status.releasing.title": "Releasing “{0}”…",
            "status.releasing.body": "Closing the connection and forgetting the pairing on this computer.",
            "status.released.title": "“{0}” released",
            "status.released.body": "Turn the keyboard off and on and click “Connect to this PC” in the Windows app.",
            "status.disconnecting.title": "Disconnecting “{0}”…",
            "status.disconnected.title": "“{0}” disconnected",
            "status.disconnected.body": "It is still paired, but not connected.",
            "status.connecting.title": "Connecting “{0}”…",
            "status.connecting.body": "Make sure you turned the keyboard off and on.",
            "status.connected.title": "“{0}” connected",
            "status.connected.body": "The keyboard is ready to use.",
            "status.pairing.pin": "Type {0} on the keyboard and press Return.",
            "status.pairing.failed.title": "Couldn't pair “{0}”",
            "status.pairing.failed.body": "Opening the system Bluetooth settings to finish pairing.",
            "status.scanning.title": "Searching for nearby keyboards…",
            "status.scanning.body": "Turn the keyboard off and on so it becomes visible.",
            "status.scanDone.title": "Search finished",
            "status.scanDone.body": "The keyboard list has been updated.",
            "status.notFound.title": "Device {0} not found",
            "status.error.title": "Something went wrong",
            "layout.title": "Layout & language",
            "layout.subtitle": "Official Apple layout so every key matches what is printed on it",
            "layout.active.title": "Apple layout",
            "layout.active.subtitle": "Active layout in macOS: {0}",
            "layout.drivers.title": "Official Apple drivers (Boot Camp)",
            "layout.drivers.installed": "Installed on this PC (17 official Apple keyboard layouts ready to use)",
            "layout.drivers.missing": "Not installed yet. Download Apple's Boot Camp package and select the extracted folder or .zip file: the app will automatically find and install the required files.",
            "layout.drivers.badgeInstalled": "Installed",
            "layout.drivers.badgeMissing": "Missing",
            "layout.drivers.selectFolder": "Select folder…",
            "layout.drivers.selectZip": "Select .zip…",
            "layout.drivers.installing": "Finding and installing Apple keyboard layouts…",
            "layout.drivers.success": "Successfully installed {0} official Apple keyboard layouts.",
            "layout.drivers.notFound": "Could not find BootCamp.msi or keyboard driver files (*A.dll) in the selected location.",
            "layout.iso.title": "ISO keys [º ª \\] and [< >]",
            "layout.iso.subtitle": "[º ª \\] next to 1 and [< >] next to left Shift, the same on macOS and Windows",
            "layout.option.title": "Left Option (⌥) for symbols (@ # € [ ] { } \\ | ~)",
            "layout.option.subtitle": "Type symbols the same way on macOS and Windows",
            "layout.appleOnly.title": "Apply to Apple keyboards only",
            "layout.appleOnly.subtitle": "Other keyboards connected at the same time keep their standard behaviour",
            "layout.remember.title": "Remember released keyboards",
            "layout.remember.subtitle": "Keeps released keyboards so you can reconnect them in one click",
            "layout.appLanguage.title": "App language",
            "layout.appLanguage.subtitle": "Choose the interface language (Català, Español or English)",
            "layout.appLanguage.auto": "Automatic (system)",
            "layout.appLanguage.ca": "Català",
            "layout.appLanguage.es": "Español",
            "layout.appLanguage.en": "English",
            "layout.native": "Native on macOS",
            "shortcuts.title": "Shortcuts & media",
            "shortcuts.subtitle": "Command (⌘), Delete, Home/End and quick access to function key settings",
            "shortcuts.modifiers.title": "Command (⌘) and Control (⌃)",
            "shortcuts.modifiers.subtitle": "⌘+C/V/X/Z/A/S/F/T/W, ⌘+Tab to switch apps, ⌘+Q to quit and ⌘+Space to search",
            "shortcuts.modifiers.mac": "Mac mode (recommended)",
            "shortcuts.modifiers.swap": "Full swap (⌘ ↔ Ctrl)",
            "shortcuts.modifiers.windows": "Windows standard (⌘ = Win)",
            "shortcuts.modifiers.macNative": "macOS native",
            "shortcuts.modifiers.macCustom": "Customize in Settings…",
            "shortcuts.nav.title": "Delete and ⌘ + arrow navigation",
            "shortcuts.nav.subtitle": "Fn + Backspace = Delete  •  ⌘ + ←/→ = Home/End  •  ⌘ + ↑/↓ = Page Up/Down",
            "shortcuts.media.title": "Function keys (F1–F12) & keyboard settings",
            "shortcuts.media.subtitle": "Top-row key behavior depends on your keyboard model and the “Use F1, F2, etc. keys as standard function keys” option in System Settings",
            "shortcuts.rightOptionFn.title": "Use Right Option (⌥) as Fn key",
            "shortcuts.rightOptionFn.subtitle": "Windows blocks the Magic Keyboard's physical Fn key without a kernel driver. Enable this to use Right Option (⌥) as Fn (Delete, Home/End and F1–F12); symbols remain available via Left Option",
            "shortcuts.openKeyboardSettings": "Open keyboard settings…",
            "guide.title": "Mac ↔ Windows guide",
            "guide.subtitle": "How to move the keyboard between computers, plus a quick symbol map",
            "guide.toWindows.title": "From Mac to Windows",
            "guide.toWindows.1": "On the Mac, open Magic Keyboard Suite and click “Release for Windows” on your keyboard.",
            "guide.toWindows.2": "Turn the keyboard off, wait 2 seconds and turn it back on.",
            "guide.toWindows.3": "On Windows, open Magic Keyboard Suite and click “Connect to this PC”.",
            "guide.toMac.title": "From Windows to Mac",
            "guide.toMac.1": "On Windows, open Magic Keyboard Suite and click “Release for the Mac” on your keyboard.",
            "guide.toMac.2": "Turn the keyboard off, wait 2 seconds and turn it back on.",
            "guide.toMac.3": "On the Mac, open Magic Keyboard Suite and click “Connect to this Mac”.",
            "guide.symbols.title": "Symbol map (Spanish / Catalan Apple ISO)",
            "guide.symbols.1": "Option (⌥) + 2 = @    •    Option (⌥) + 3 = #    •    Option (⌥) + E = €    •    Option (⌥) + 1 = |",
            "guide.symbols.2": "Option (⌥) + º = \\    •    Option (⌥) + ` = [    •    Option (⌥) + + = ]    •    Option (⌥) + ´ = {",
            "guide.symbols.3": "Option (⌥) + Ç = }    •    Option (⌥) + Ñ = ~    •    Shift + 3 = · (Catalan middle dot)",
            "uninstall.title": "Uninstall Magic Keyboard Suite",
            "uninstall.subtitle": "Moves the app to the Trash and deletes its settings",
            "uninstall.button": "Uninstall…",
            "toggle.on": "On",
            "toggle.off": "Off",
            "tray.open": "Open Magic Keyboard Suite",
            "tray.keyboards": "Keyboards",
            "tray.noKeyboards": "No keyboards",
            "tray.launchAtLogin": "Open at login",
            "tray.quit": "Quit",
            "onboarding.setup.badge": "Initial setup",
            "onboarding.setup.title": "Welcome to Magic Keyboard Suite",
            "onboarding.setup.subtitle": "Choose the application language before continuing.",
            "onboarding.nextButton": "Continue →",
            "onboarding.backButton": "← Back",
            "onboarding.badge": "Quick start guide",
            "onboarding.title": "How Magic Keyboard Suite works",
            "onboarding.subtitle": "How to use the app and get the most out of your Magic Keyboard on macOS and Windows",
            "onboarding.step1.title": "1. Switch your keyboard between Mac and Windows in one click",
            "onboarding.step1.body": "Magic Keyboard only keeps one active Bluetooth pairing at a time. Before moving to your other computer, click “Release” on its card, turn the physical switch off and on, and click “Connect” on the other computer.",
            "onboarding.step2.title": "2. Every key matches what is printed on it",
            "onboarding.step2.body": "The official Apple layout and ISO fix keep [º ª \\] next to 1 and [< >] next to left Shift. Left Option (⌥) types @ # € [ ] { } \\ | ~ the exact same way on both systems.",
            "onboarding.step3.title": "3. Your Mac shortcuts work the same on both computers",
            "onboarding.step3.body": "Use Command (⌘) + C/V/X/Z/A/S/F/T/W, ⌘ + Tab to switch apps, Fn + Backspace for Forward Delete, ⌘ + arrows for Home/End, and F7–F12 as media keys.",
            "onboarding.reopenHint": "You can reopen this guide anytime from the “View welcome guide” button in the Guide tab.",
            "onboarding.startButton": "Start using Magic Keyboard Suite",
            "onboarding.reopenButton": "View welcome guide…",
        ],
    ]

    static func t(_ key: String, _ args: CVarArg...) -> String {
        var s = tables[language]?[key] ?? tables["es"]?[key] ?? key
        for (i, a) in args.enumerated() { s = s.replacingOccurrences(of: "{\(i)}", with: "\(a)") }
        return s
    }

    static var appName: String { t("app.name") }
    static func appVersion(_ a0: CVarArg) -> String { t("app.version", a0) }
    static var navBluetooth: String { t("nav.bluetooth") }
    static var navLayout: String { t("nav.layout") }
    static var navShortcuts: String { t("nav.shortcuts") }
    static var navGuide: String { t("nav.guide") }
    static var footerEngineActive: String { t("footer.engineActive") }
    static var footerEngineInactive: String { t("footer.engineInactive") }
    static func footerCurrentLayout(_ a0: CVarArg) -> String { t("footer.currentLayout", a0) }
    static var btTitle: String { t("bt.title") }
    static var btSubtitle: String { t("bt.subtitle") }
    static var btRefresh: String { t("bt.refresh") }
    static var btScan: String { t("bt.scan") }
    static var btScanning: String { t("bt.scanning") }
    static var btEmptyTitle: String { t("bt.empty.title") }
    static var btEmptyBody: String { t("bt.empty.body") }
    static func btMac(_ a0: CVarArg) -> String { t("bt.mac", a0) }
    static var btAppleKeyboard: String { t("bt.appleKeyboard") }
    static func btBattery(_ a0: CVarArg) -> String { t("bt.battery", a0) }
    static var btFilterTitle: String { t("bt.filter.title") }
    static var btFilterSubtitle: String { t("bt.filter.subtitle") }
    static var btFilterApple: String { t("bt.filter.apple") }
    static var btFilterKeyboards: String { t("bt.filter.keyboards") }
    static var btFilterAll: String { t("bt.filter.all") }
    static var stateConnected: String { t("state.connected") }
    static var statePaired: String { t("state.paired") }
    static var stateReleased: String { t("state.released") }
    static var stateNearby: String { t("state.nearby") }
    static var actionConnect: String { t("action.connect") }
    static var actionRelease: String { t("action.release") }
    static var actionDisconnect: String { t("action.disconnect") }
    static var statusIdleTitle: String { t("status.idle.title") }
    static var statusIdleBody: String { t("status.idle.body") }
    static func statusReleasingTitle(_ a0: CVarArg) -> String { t("status.releasing.title", a0) }
    static var statusReleasingBody: String { t("status.releasing.body") }
    static func statusReleasedTitle(_ a0: CVarArg) -> String { t("status.released.title", a0) }
    static var statusReleasedBody: String { t("status.released.body") }
    static func statusDisconnectingTitle(_ a0: CVarArg) -> String { t("status.disconnecting.title", a0) }
    static func statusDisconnectedTitle(_ a0: CVarArg) -> String { t("status.disconnected.title", a0) }
    static var statusDisconnectedBody: String { t("status.disconnected.body") }
    static func statusConnectingTitle(_ a0: CVarArg) -> String { t("status.connecting.title", a0) }
    static var statusConnectingBody: String { t("status.connecting.body") }
    static func statusConnectedTitle(_ a0: CVarArg) -> String { t("status.connected.title", a0) }
    static var statusConnectedBody: String { t("status.connected.body") }
    static func statusPairingPin(_ a0: CVarArg) -> String { t("status.pairing.pin", a0) }
    static func statusPairingFailedTitle(_ a0: CVarArg) -> String { t("status.pairing.failed.title", a0) }
    static var statusPairingFailedBody: String { t("status.pairing.failed.body") }
    static var statusScanningTitle: String { t("status.scanning.title") }
    static var statusScanningBody: String { t("status.scanning.body") }
    static var statusScanDoneTitle: String { t("status.scanDone.title") }
    static var statusScanDoneBody: String { t("status.scanDone.body") }
    static func statusNotFoundTitle(_ a0: CVarArg) -> String { t("status.notFound.title", a0) }
    static var statusErrorTitle: String { t("status.error.title") }
    static var layoutTitle: String { t("layout.title") }
    static var layoutSubtitle: String { t("layout.subtitle") }
    static var layoutActiveTitle: String { t("layout.active.title") }
    static func layoutActiveSubtitle(_ a0: CVarArg) -> String { t("layout.active.subtitle", a0) }
    static var layoutDriversTitle: String { t("layout.drivers.title") }
    static var layoutDriversInstalled: String { t("layout.drivers.installed") }
    static var layoutDriversMissing: String { t("layout.drivers.missing") }
    static var layoutDriversBadgeInstalled: String { t("layout.drivers.badgeInstalled") }
    static var layoutDriversBadgeMissing: String { t("layout.drivers.badgeMissing") }
    static var layoutDriversSelectFolder: String { t("layout.drivers.selectFolder") }
    static var layoutDriversSelectZip: String { t("layout.drivers.selectZip") }
    static var layoutDriversInstalling: String { t("layout.drivers.installing") }
    static func layoutDriversSuccess(_ a0: CVarArg) -> String { t("layout.drivers.success", a0) }
    static var layoutDriversNotFound: String { t("layout.drivers.notFound") }
    static var layoutIsoTitle: String { t("layout.iso.title") }
    static var layoutIsoSubtitle: String { t("layout.iso.subtitle") }
    static var layoutOptionTitle: String { t("layout.option.title") }
    static var layoutOptionSubtitle: String { t("layout.option.subtitle") }
    static var layoutAppleOnlyTitle: String { t("layout.appleOnly.title") }
    static var layoutAppleOnlySubtitle: String { t("layout.appleOnly.subtitle") }
    static var layoutRememberTitle: String { t("layout.remember.title") }
    static var layoutRememberSubtitle: String { t("layout.remember.subtitle") }
    static var layoutAppLanguageTitle: String { t("layout.appLanguage.title") }
    static var layoutAppLanguageSubtitle: String { t("layout.appLanguage.subtitle") }
    static var layoutAppLanguageAuto: String { t("layout.appLanguage.auto") }
    static var layoutAppLanguageCa: String { t("layout.appLanguage.ca") }
    static var layoutAppLanguageEs: String { t("layout.appLanguage.es") }
    static var layoutAppLanguageEn: String { t("layout.appLanguage.en") }
    static var layoutNative: String { t("layout.native") }
    static var shortcutsTitle: String { t("shortcuts.title") }
    static var shortcutsSubtitle: String { t("shortcuts.subtitle") }
    static var shortcutsModifiersTitle: String { t("shortcuts.modifiers.title") }
    static var shortcutsModifiersSubtitle: String { t("shortcuts.modifiers.subtitle") }
    static var shortcutsModifiersMac: String { t("shortcuts.modifiers.mac") }
    static var shortcutsModifiersSwap: String { t("shortcuts.modifiers.swap") }
    static var shortcutsModifiersWindows: String { t("shortcuts.modifiers.windows") }
    static var shortcutsModifiersMacNative: String { t("shortcuts.modifiers.macNative") }
    static var shortcutsModifiersMacCustom: String { t("shortcuts.modifiers.macCustom") }
    static var shortcutsNavTitle: String { t("shortcuts.nav.title") }
    static var shortcutsNavSubtitle: String { t("shortcuts.nav.subtitle") }
    static var shortcutsMediaTitle: String { t("shortcuts.media.title") }
    static var shortcutsMediaSubtitle: String { t("shortcuts.media.subtitle") }
    static var shortcutsRightOptionFnTitle: String { t("shortcuts.rightOptionFn.title") }
    static var shortcutsRightOptionFnSubtitle: String { t("shortcuts.rightOptionFn.subtitle") }
    static var shortcutsOpenKeyboardSettings: String { t("shortcuts.openKeyboardSettings") }
    static var guideTitle: String { t("guide.title") }
    static var guideSubtitle: String { t("guide.subtitle") }
    static var guideToWindowsTitle: String { t("guide.toWindows.title") }
    static var guideToWindows1: String { t("guide.toWindows.1") }
    static var guideToWindows2: String { t("guide.toWindows.2") }
    static var guideToWindows3: String { t("guide.toWindows.3") }
    static var guideToMacTitle: String { t("guide.toMac.title") }
    static var guideToMac1: String { t("guide.toMac.1") }
    static var guideToMac2: String { t("guide.toMac.2") }
    static var guideToMac3: String { t("guide.toMac.3") }
    static var guideSymbolsTitle: String { t("guide.symbols.title") }
    static var guideSymbols1: String { t("guide.symbols.1") }
    static var guideSymbols2: String { t("guide.symbols.2") }
    static var guideSymbols3: String { t("guide.symbols.3") }
    static var uninstallTitle: String { t("uninstall.title") }
    static var uninstallSubtitle: String { t("uninstall.subtitle") }
    static var uninstallButton: String { t("uninstall.button") }
    static var toggleOn: String { t("toggle.on") }
    static var toggleOff: String { t("toggle.off") }
    static var trayOpen: String { t("tray.open") }
    static var trayKeyboards: String { t("tray.keyboards") }
    static var trayNoKeyboards: String { t("tray.noKeyboards") }
    static var trayLaunchAtLogin: String { t("tray.launchAtLogin") }
    static var trayQuit: String { t("tray.quit") }
    static var onboardingSetupBadge: String { t("onboarding.setup.badge") }
    static var onboardingSetupTitle: String { t("onboarding.setup.title") }
    static var onboardingSetupSubtitle: String { t("onboarding.setup.subtitle") }
    static var onboardingNextButton: String { t("onboarding.nextButton") }
    static var onboardingBackButton: String { t("onboarding.backButton") }
    static var onboardingBadge: String { t("onboarding.badge") }
    static var onboardingTitle: String { t("onboarding.title") }
    static var onboardingSubtitle: String { t("onboarding.subtitle") }
    static var onboardingStep1Title: String { t("onboarding.step1.title") }
    static var onboardingStep1Body: String { t("onboarding.step1.body") }
    static var onboardingStep2Title: String { t("onboarding.step2.title") }
    static var onboardingStep2Body: String { t("onboarding.step2.body") }
    static var onboardingStep3Title: String { t("onboarding.step3.title") }
    static var onboardingStep3Body: String { t("onboarding.step3.body") }
    static var onboardingReopenHint: String { t("onboarding.reopenHint") }
    static var onboardingStartButton: String { t("onboarding.startButton") }
    static var onboardingReopenButton: String { t("onboarding.reopenButton") }
}

enum AppInfo {
    static let version = "3.1.0"
}
