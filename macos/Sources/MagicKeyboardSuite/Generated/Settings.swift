// Archivo generado por shared/tools/generate.py. NO EDITAR: cambia shared/ y vuelve a generar.
import Foundation

/// Claves compartidas con Windows (HKCU\\Software\\MagicKeyboardSuite).
enum SettingsKey {
    static let deviceFilter = "DeviceFilter"
    static let rememberReleased = "RememberReleased"
    static let preferredKlid = "PreferredKlid"
    static let launchAtLogin = "LaunchAtLogin"
    static let hasSeenOnboarding = "HasSeenOnboarding"
    static let appLanguage = "AppLanguage"

    static let defaults: [String: Any] = [
        "DeviceFilter": "apple",
        "RememberReleased": true,
        "PreferredKlid": "a000040a",
        "LaunchAtLogin": true,
        "HasSeenOnboarding": false,
        "AppLanguage": "auto",
    ]
}
