import Foundation

// MARK: - Modelos Unificados (Paridad con Windows WinUI 3; AppleLayoutDefinition se genera en Generated/Layouts.swift)

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

    let id: String
    let name: String
    let macAddress: String
    let state: ConnectionState
    let isKeyboardClass: Bool
    let isAppleKeyboard: Bool
    let batteryPercent: Int?

    var isUsb: Bool { macAddress == "USB" }

    init(
        id: String? = nil,
        name: String,
        macAddress: String,
        state: ConnectionState,
        isKeyboardClass: Bool,
        isAppleKeyboard: Bool,
        batteryPercent: Int?
    ) {
        self.id = id ?? macAddress
        self.name = name
        self.macAddress = macAddress
        self.state = state
        self.isKeyboardClass = isKeyboardClass
        self.isAppleKeyboard = isAppleKeyboard
        self.batteryPercent = batteryPercent
    }
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
