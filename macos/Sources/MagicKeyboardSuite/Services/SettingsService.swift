import Foundation
import ServiceManagement

// MARK: - Servicio de Ajustes Persistentes (Claves compartidas con Windows)

@MainActor
final class SettingsService: ObservableObject {
    static let shared = SettingsService()

    private let defaults: UserDefaults
    private var isSyncingLayoutFromSystem = false

    @Published var deviceFilter: DeviceFilterMode {
        didSet { defaults.set(deviceFilter.rawValue, forKey: SettingsKey.deviceFilter) }
    }

    @Published var rememberReleased: Bool {
        didSet { defaults.set(rememberReleased, forKey: SettingsKey.rememberReleased) }
    }

    @Published var launchAtLogin: Bool {
        didSet {
            defaults.set(launchAtLogin, forKey: SettingsKey.launchAtLogin)
            updateLoginItem(enabled: launchAtLogin)
        }
    }

    @Published var selectedLayoutKlid: String {
        didSet {
            defaults.set(selectedLayoutKlid, forKey: SettingsKey.preferredKlid)
        }
    }

    @Published var appLanguage: AppLanguageMode {
        didSet {
            defaults.set(appLanguage.rawValue, forKey: SettingsKey.appLanguage)
            L10n.overrideLanguage = (appLanguage == .auto) ? nil : appLanguage.rawValue
        }
    }

    @Published var hasSeenOnboarding: Bool {
        didSet { defaults.set(hasSeenOnboarding, forKey: SettingsKey.hasSeenOnboarding) }
    }

    @Published var isShowingOnboarding: Bool

    @Published var isSidebarVisible: Bool = true

    @Published var modifierProfileIndex: Int = 0

    private init() {
        let suite = UserDefaults(suiteName: "com.magickeyboardsuite") ?? .standard
        suite.register(defaults: SettingsKey.defaults)
        self.defaults = suite

        let rawLang = suite.string(forKey: SettingsKey.appLanguage) ?? AppLanguageMode.auto.rawValue
        let resolvedLang = AppLanguageMode(rawValue: rawLang) ?? .auto
        self.appLanguage = resolvedLang
        L10n.overrideLanguage = (resolvedLang == .auto) ? nil : resolvedLang.rawValue

        let rawFilter = suite.string(forKey: SettingsKey.deviceFilter) ?? DeviceFilterMode.apple.rawValue
        self.deviceFilter = DeviceFilterMode(rawValue: rawFilter) ?? .apple
        self.rememberReleased = suite.bool(forKey: SettingsKey.rememberReleased)
        self.launchAtLogin = suite.bool(forKey: SettingsKey.launchAtLogin)
        let savedKlid = suite.string(forKey: SettingsKey.preferredKlid) ?? ""
        self.selectedLayoutKlid = savedKlid.isEmpty ? AppleLayoutDefinition.all[0].klid : savedKlid
        let seen = suite.bool(forKey: SettingsKey.hasSeenOnboarding)
        self.hasSeenOnboarding = seen
        self.isShowingOnboarding = !seen
    }

    func syncSelectedLayoutFromSystem(_ klid: String) {
        guard selectedLayoutKlid != klid else { return }
        isSyncingLayoutFromSystem = true
        selectedLayoutKlid = klid
        isSyncingLayoutFromSystem = false
    }

    var isExternalLayoutSync: Bool { isSyncingLayoutFromSystem }

    private func updateLoginItem(enabled: Bool) {
        if #available(macOS 13.0, *) {
            do {
                if enabled {
                    try SMAppService.mainApp.register()
                } else {
                    try SMAppService.mainApp.unregister()
                }
            } catch {
                // Ignorar en modo desarrollo si no está dentro de /Applications
            }
        }
    }
}
