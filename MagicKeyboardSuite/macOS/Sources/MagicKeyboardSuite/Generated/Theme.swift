// Archivo generado por shared/tools/generate.py. NO EDITAR: cambia shared/ y vuelve a generar.
import SwiftUI
import AppKit

enum Theme {
    enum Colors {
        static let stateConnected = Color(light: NSColor(srgbRed: 0.0588, green: 0.4824, blue: 0.0588, alpha: 1.0000), dark: NSColor(srgbRed: 0.4235, green: 0.7961, blue: 0.3725, alpha: 1.0000))
        static let statePaired = Color(light: NSColor(srgbRed: 0.6157, green: 0.3647, blue: 0.0000, alpha: 1.0000), dark: NSColor(srgbRed: 0.9882, green: 0.8824, blue: 0.0000, alpha: 1.0000))
        static let stateReleased = Color(light: NSColor(srgbRed: 0.4784, green: 0.2471, blue: 0.7686, alpha: 1.0000), dark: NSColor(srgbRed: 0.7608, green: 0.6078, blue: 1.0000, alpha: 1.0000))
        static let stateNearby = Color(light: NSColor(srgbRed: 0.0000, green: 0.3725, blue: 0.7216, alpha: 1.0000), dark: NSColor(srgbRed: 0.3765, green: 0.8039, blue: 1.0000, alpha: 1.0000))
        static let stateError = Color(light: NSColor(srgbRed: 0.7686, green: 0.1686, blue: 0.1098, alpha: 1.0000), dark: NSColor(srgbRed: 1.0000, green: 0.6000, blue: 0.6431, alpha: 1.0000))
        static let cardBackground = Color(light: NSColor(srgbRed: 1.0000, green: 1.0000, blue: 1.0000, alpha: 0.7020), dark: NSColor(srgbRed: 1.0000, green: 1.0000, blue: 1.0000, alpha: 0.0510))
        static let cardStroke = Color(light: NSColor(srgbRed: 0.0000, green: 0.0000, blue: 0.0000, alpha: 0.0588), dark: NSColor(srgbRed: 0.0000, green: 0.0000, blue: 0.0000, alpha: 0.1020))
        static let iconTile = Color(light: NSColor(srgbRed: 0.0000, green: 0.0000, blue: 0.0000, alpha: 0.0392), dark: NSColor(srgbRed: 1.0000, green: 1.0000, blue: 1.0000, alpha: 0.0588))
        static let badgeFillOpacity: Double = 0.14
    }
    enum Radius {
        static let card: CGFloat = 8
        static let control: CGFloat = 4
        static let badge: CGFloat = 4
        static let iconTile: CGFloat = 8
    }
    enum Spacing {
        static let xs: CGFloat = 4
        static let s: CGFloat = 8
        static let m: CGFloat = 12
        static let l: CGFloat = 16
        static let xl: CGFloat = 24
        static let cardPaddingH: CGFloat = 20
        static let cardPaddingV: CGFloat = 16
        static let pagePaddingH: CGFloat = 36
        static let pagePaddingV: CGFloat = 24
    }
    enum Size {
        static let iconTile: CGFloat = 44
        static let cardIcon: CGFloat = 20
        static let tileIcon: CGFloat = 20
        static let statusDot: CGFloat = 8
        static let sidebarWidth: CGFloat = 268
        static let contentMaxWidth: CGFloat = 920
        static let windowMinWidth: CGFloat = 980
        static let windowMinHeight: CGFloat = 660
        static let comboWidth: CGFloat = 280
    }
    enum Typography {
        static let pageTitle = Font.system(size: 28, weight: .semibold)
        static let pageSubtitle = Font.system(size: 14, weight: .regular)
        static let sectionTitle = Font.system(size: 16, weight: .semibold)
        static let cardTitle = Font.system(size: 14, weight: .semibold)
        static let cardBody = Font.system(size: 12, weight: .regular)
        static let deviceName = Font.system(size: 15, weight: .semibold)
        static let badge = Font.system(size: 11, weight: .medium)
        static let caption = Font.system(size: 11, weight: .regular)
        static let button = Font.system(size: 13, weight: .medium)
    }
    /// SF Symbols (mismo concepto que los glifos Fluent de Windows).
    enum Icons {
        static let navBluetooth = "dot.radiowaves.left.and.right"
        static let navLayout = "keyboard"
        static let navShortcuts = "command"
        static let navGuide = "info.circle"
        static let keyboard = "keyboard"
        static let refresh = "arrow.clockwise"
        static let scan = "dot.radiowaves.left.and.right"
        static let connect = "link"
        static let release = "arrow.up.right.square"
        static let disconnect = "bolt.slash"
        static let filter = "line.3.horizontal.decrease.circle"
        static let globe = "globe"
        static let swap = "arrow.left.arrow.right.square"
        static let option = "option"
        static let memory = "memorychip"
        static let command = "command"
        static let delete = "delete.right"
        static let media = "speaker.wave.2"
        static let info = "info.circle.fill"
        static let success = "checkmark.circle.fill"
        static let uninstall = "trash"
        static let settings = "gearshape"
        static let quit = "power"
        static let batteryFull = "battery.100"
        static let batteryMid = "battery.50"
        static let batteryLow = "battery.25"
    }
}

extension Color {
    /// Color que cambia automáticamente entre modo claro y oscuro.
    init(light: NSColor, dark: NSColor) {
        self.init(nsColor: NSColor(name: nil) { appearance in
            appearance.bestMatch(from: [.darkAqua, .vibrantDark, .accessibilityHighContrastDarkAqua]) != nil ? dark : light
        })
    }
}
