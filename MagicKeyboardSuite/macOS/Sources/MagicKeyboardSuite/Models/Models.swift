import Foundation

// MARK: - Modelos Unificados (Paridad con Windows WinUI 3)

enum AppLanguageMode: String, CaseIterable, Identifiable {
    case auto = "auto"
    case ca = "ca"
    case es = "es"
    case en = "en"

    var id: String { rawValue }

    var label: String {
        switch self {
        case .auto: return L10n.layoutAppLanguageAuto
        case .ca: return L10n.layoutAppLanguageCa
        case .es: return L10n.layoutAppLanguageEs
        case .en: return L10n.layoutAppLanguageEn
        }
    }
}

enum DeviceFilterMode: String, CaseIterable, Identifiable {
    case apple = "apple"
    case keyboards = "keyboards"
    case all = "all"

    var id: String { rawValue }

    var label: String {
        switch self {
        case .apple: return L10n.btFilterApple
        case .keyboards: return L10n.btFilterKeyboards
        case .all: return L10n.btFilterAll
        }
    }
}

struct BluetoothKeyboardItem: Identifiable, Equatable {
    enum ConnectionState: Equatable {
        case connected
        case pairedDisconnected
        case releasedToPeer
        case nearbyVisible

        var label: String {
            switch self {
            case .connected: return L10n.stateConnected
            case .pairedDisconnected: return L10n.statePaired
            case .releasedToPeer: return L10n.stateReleased
            case .nearbyVisible: return L10n.stateNearby
            }
        }

        var sortOrder: Int {
            switch self {
            case .connected: return 0
            case .pairedDisconnected: return 1
            case .releasedToPeer: return 2
            case .nearbyVisible: return 3
            }
        }
    }

    var id: String { macAddress }
    let name: String
    let macAddress: String
    let state: ConnectionState
    let isKeyboardClass: Bool
    let isAppleKeyboard: Bool
    let batteryPercent: Int?
}

enum SidebarSection: String, CaseIterable, Identifiable {
    case bluetooth = "bluetooth"
    case layout = "layout"
    case shortcuts = "shortcuts"
    case guide = "guide"

    var id: String { rawValue }

    var title: String {
        switch self {
        case .bluetooth: return L10n.navBluetooth
        case .layout: return L10n.navLayout
        case .shortcuts: return L10n.navShortcuts
        case .guide: return L10n.navGuide
        }
    }

    var icon: String {
        switch self {
        case .bluetooth: return Theme.Icons.navBluetooth
        case .layout: return Theme.Icons.navLayout
        case .shortcuts: return Theme.Icons.navShortcuts
        case .guide: return Theme.Icons.navGuide
        }
    }
}

struct AppleLayoutDefinition: Identifiable, Equatable {
    let klid: String
    let name: String
    let macInputSourceIDs: [String]
    let primaryLang: String
    var id: String { klid }

    static let all: [AppleLayoutDefinition] = [
        AppleLayoutDefinition(
            klid: "a000040a",
            name: "Español / Català (Apple)",
            macInputSourceIDs: ["com.apple.keylayout.Spanish-ISO", "com.apple.keylayout.Spanish", "com.apple.keylayout.Catalan"],
            primaryLang: "es"
        ),
        AppleLayoutDefinition(
            klid: "a0020409",
            name: "United States-International (Apple)",
            macInputSourceIDs: ["com.apple.keylayout.USInternational-PC", "com.apple.keylayout.USExtended"],
            primaryLang: "en"
        ),
        AppleLayoutDefinition(
            klid: "a0000409",
            name: "United States (Apple)",
            macInputSourceIDs: ["com.apple.keylayout.US", "com.apple.keylayout.ABC"],
            primaryLang: "en"
        ),
        AppleLayoutDefinition(
            klid: "a0000809",
            name: "United Kingdom (Apple)",
            macInputSourceIDs: ["com.apple.keylayout.British", "com.apple.keylayout.British-PC"],
            primaryLang: "en-GB"
        ),
        AppleLayoutDefinition(
            klid: "a000040c",
            name: "Français (Apple)",
            macInputSourceIDs: ["com.apple.keylayout.French-numerical", "com.apple.keylayout.French", "com.apple.keylayout.French-PC"],
            primaryLang: "fr"
        ),
        AppleLayoutDefinition(
            klid: "a0000407",
            name: "Deutsch (Apple)",
            macInputSourceIDs: ["com.apple.keylayout.German", "com.apple.keylayout.German-DIN-2137"],
            primaryLang: "de"
        ),
        AppleLayoutDefinition(
            klid: "a0000410",
            name: "Italiano (Apple)",
            macInputSourceIDs: ["com.apple.keylayout.Italian-Pro", "com.apple.keylayout.Italian"],
            primaryLang: "it"
        ),
        AppleLayoutDefinition(
            klid: "a0000416",
            name: "Português (Apple)",
            macInputSourceIDs: ["com.apple.keylayout.Portuguese", "com.apple.keylayout.Brazilian-Pro", "com.apple.keylayout.Brazilian"],
            primaryLang: "pt"
        ),
        AppleLayoutDefinition(
            klid: "a0000413",
            name: "Nederlands (Apple)",
            macInputSourceIDs: ["com.apple.keylayout.Dutch"],
            primaryLang: "nl"
        ),
        AppleLayoutDefinition(
            klid: "a000041d",
            name: "Svenska (Apple)",
            macInputSourceIDs: ["com.apple.keylayout.Swedish-Pro", "com.apple.keylayout.Swedish"],
            primaryLang: "sv"
        ),
        AppleLayoutDefinition(
            klid: "a0000406",
            name: "Dansk (Apple)",
            macInputSourceIDs: ["com.apple.keylayout.Danish"],
            primaryLang: "da"
        ),
        AppleLayoutDefinition(
            klid: "a0000414",
            name: "Norsk (Apple)",
            macInputSourceIDs: ["com.apple.keylayout.Norwegian", "com.apple.keylayout.NorwegianExtended"],
            primaryLang: "nb"
        ),
        AppleLayoutDefinition(
            klid: "a000040b",
            name: "Suomi (Apple)",
            macInputSourceIDs: ["com.apple.keylayout.Finnish", "com.apple.keylayout.FinnishExtended"],
            primaryLang: "fi"
        ),
        AppleLayoutDefinition(
            klid: "a000100c",
            name: "Swiss (Apple)",
            macInputSourceIDs: ["com.apple.keylayout.SwissGerman", "com.apple.keylayout.SwissFrench"],
            primaryLang: "de-CH"
        ),
        AppleLayoutDefinition(
            klid: "a0000813",
            name: "Belgian (Apple)",
            macInputSourceIDs: ["com.apple.keylayout.Belgian"],
            primaryLang: "nl-BE"
        ),
        AppleLayoutDefinition(
            klid: "a0000415",
            name: "Polski (Apple)",
            macInputSourceIDs: ["com.apple.keylayout.PolishPro", "com.apple.keylayout.Polish"],
            primaryLang: "pl"
        ),
        AppleLayoutDefinition(
            klid: "a0000419",
            name: "Russian (Apple)",
            macInputSourceIDs: ["com.apple.keylayout.Russian", "com.apple.keylayout.RussianWin", "com.apple.keylayout.Russian-Phonetic"],
            primaryLang: "ru"
        )
    ]
}
