import SwiftUI
import AppKit

// MARK: - Menú en la Barra de Menús (Paridad con el icono de bandeja de Windows)

struct MenuBarContentView: View {
    @Environment(\.openWindow) private var openWindow
    @ObservedObject var bluetooth: BluetoothService
    @ObservedObject var settings: SettingsService
    @ObservedObject var inputSources: InputSourceService

    private func openMainWindow() {
        NSApplication.shared.activate(ignoringOtherApps: true)
        for window in NSApplication.shared.windows where window.canBecomeMain {
            window.makeKeyAndOrderFront(nil)
            return
        }
        openWindow(id: "main")
    }

    var body: some View {
        Button(action: openMainWindow) {
            Label(L10n.trayOpen, systemImage: Theme.Icons.keyboard)
        }
        .keyboardShortcut("o", modifiers: [.command])

        Text(L10n.footerCurrentLayout(inputSources.currentLayoutName))

        Divider()

        Section(L10n.trayKeyboards) {
            if bluetooth.keyboards.isEmpty {
                Text(L10n.trayNoKeyboards)
            } else {
                ForEach(bluetooth.keyboards) { item in
                    let batSuffix = item.batteryPercent.map { " (\($0)%)" } ?? ""
                    Menu("\(item.name) — \(item.state.label)\(batSuffix)") {
                        if item.state != .connected {
                            Button(L10n.actionConnect) {
                                bluetooth.connectHere(item)
                            }
                        }
                        if item.state == .connected || item.state == .pairedDisconnected {
                            Button(L10n.actionRelease) {
                                bluetooth.releaseForPeer(item)
                            }
                        }
                        if item.state == .connected {
                            Button(L10n.actionDisconnect) {
                                bluetooth.disconnectOnly(item)
                            }
                        }
                    }
                }
            }
        }

        Button(L10n.btScan) {
            bluetooth.scanNearbyDevices()
        }

        Divider()

        Toggle(L10n.trayLaunchAtLogin, isOn: $settings.launchAtLogin)

        Divider()

        Button(L10n.trayQuit) {
            NSApplication.shared.terminate(nil)
        }
        .keyboardShortcut("q", modifiers: [.command])
    }
}

// MARK: - Punto de Entrada de la Aplicación macOS

final class AppDelegate: NSObject, NSApplicationDelegate {
    func applicationShouldTerminateAfterLastWindowClosed(_ sender: NSApplication) -> Bool {
        // Permanece disponible en la barra de menús igual que el agente en Windows
        return false
    }
}

@main
struct MagicKeyboardSuiteMacApp: App {
    @NSApplicationDelegateAdaptor(AppDelegate.self) var appDelegate
    @StateObject private var settings: SettingsService
    @StateObject private var inputSources: InputSourceService
    @StateObject private var bluetooth: BluetoothService

    init() {
        let s = SettingsService.shared
        let inp = InputSourceService()
        _settings = StateObject(wrappedValue: s)
        _inputSources = StateObject(wrappedValue: inp)
        _bluetooth = StateObject(wrappedValue: BluetoothService(settings: s, inputSources: inp))
    }

    var body: some Scene {
        WindowGroup(L10n.appName, id: "main") {
            MainView(bluetooth: bluetooth, settings: settings, inputSources: inputSources)
        }
        .windowStyle(.titleBar)
        .windowToolbarStyle(.unified(showsTitle: true))

        MenuBarExtra(L10n.appName, systemImage: Theme.Icons.keyboard) {
            MenuBarContentView(
                bluetooth: bluetooth,
                settings: settings,
                inputSources: inputSources
            )
        }
    }
}
