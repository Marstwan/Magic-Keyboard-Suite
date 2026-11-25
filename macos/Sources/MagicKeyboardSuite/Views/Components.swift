import SwiftUI
import AppKit

// MARK: - Componentes Visuales Unificados (Mismo nombre y diseño que en Windows WinUI 3)

struct SettingsCard<RightContent: View>: View {
    let icon: String
    let title: String
    let subtitle: String
    @ViewBuilder let rightContent: () -> RightContent

    var body: some View {
        HStack(spacing: Theme.Spacing.l) {
            Image(systemName: icon)
                .font(.system(size: Theme.Size.cardIcon, weight: .regular))
                .foregroundColor(.primary)
                .frame(width: 28, alignment: .center)

            VStack(alignment: .leading, spacing: Theme.Spacing.xs) {
                Text(title)
                    .font(Theme.Typography.cardTitle)
                    .foregroundColor(.primary)
                    .fixedSize(horizontal: false, vertical: true)
                Text(subtitle)
                    .font(Theme.Typography.cardBody)
                    .foregroundColor(.secondary)
                    .fixedSize(horizontal: false, vertical: true)
            }
            .frame(maxWidth: .infinity, alignment: .leading)

            rightContent()
                .fixedSize(horizontal: true, vertical: false)
                .layoutPriority(1)
        }
        .padding(.horizontal, Theme.Spacing.cardPaddingH)
        .padding(.vertical, Theme.Spacing.cardPaddingV)
        .background(
            RoundedRectangle(cornerRadius: Theme.Radius.card, style: .continuous)
                .fill(Theme.Colors.cardBackground)
        )
        .overlay(
            RoundedRectangle(cornerRadius: Theme.Radius.card, style: .continuous)
                .stroke(Theme.Colors.cardStroke, lineWidth: 1)
        )
    }
}

struct KeyboardCard: View {
    let item: BluetoothKeyboardItem
    let isBusy: Bool
    let onRelease: () -> Void
    let onDisconnect: () -> Void
    let onConnect: () -> Void

    private var badgeColor: Color {
        switch item.state {
        case .connected: return Theme.Colors.stateConnected
        case .pairedDisconnected: return Theme.Colors.statePaired
        case .releasedToPeer: return Theme.Colors.stateReleased
        case .nearbyVisible: return Theme.Colors.stateNearby
        }
    }

    private func batteryIconName(for pct: Int) -> String {
        if pct <= 20 { return Theme.Icons.batteryLow }
        if pct <= 60 { return Theme.Icons.batteryMid }
        return Theme.Icons.batteryFull
    }

    private func batteryBadgeColor(for pct: Int) -> Color {
        if pct <= 15 { return Theme.Colors.stateError }
        if pct <= 35 { return Theme.Colors.statePaired }
        return Theme.Colors.stateConnected
    }

    var body: some View {
        HStack(spacing: Theme.Spacing.l) {
            ZStack {
                RoundedRectangle(cornerRadius: Theme.Radius.iconTile, style: .continuous)
                    .fill(Theme.Colors.iconTile)
                    .frame(width: Theme.Size.iconTile, height: Theme.Size.iconTile)
                Image(systemName: Theme.Icons.keyboard)
                    .font(.system(size: Theme.Size.tileIcon, weight: .medium))
                    .foregroundColor(.primary)
            }

            VStack(alignment: .leading, spacing: Theme.Spacing.xs) {
                HStack(spacing: Theme.Spacing.s) {
                    Text(item.name)
                        .font(Theme.Typography.deviceName)
                        .foregroundColor(.primary)
                        .lineLimit(1)

                    Text(item.state.label)
                        .font(Theme.Typography.badge)
                        .foregroundColor(badgeColor)
                        .lineLimit(1)
                        .fixedSize(horizontal: true, vertical: false)
                        .padding(.horizontal, Theme.Spacing.s)
                        .padding(.vertical, 2)
                        .background(
                            RoundedRectangle(cornerRadius: Theme.Radius.badge, style: .continuous)
                                .fill(badgeColor.opacity(Theme.Colors.badgeFillOpacity))
                        )

                    if let pct = item.batteryPercent {
                        let bColor = batteryBadgeColor(for: pct)
                        HStack(spacing: 4) {
                            Image(systemName: batteryIconName(for: pct))
                                .font(.system(size: 11, weight: .medium))
                            Text(L10n.btBattery(pct))
                                .font(Theme.Typography.badge)
                        }
                        .foregroundColor(bColor)
                        .lineLimit(1)
                        .fixedSize(horizontal: true, vertical: false)
                        .padding(.horizontal, Theme.Spacing.s)
                        .padding(.vertical, 2)
                        .background(
                            RoundedRectangle(cornerRadius: Theme.Radius.badge, style: .continuous)
                                .fill(bColor.opacity(Theme.Colors.badgeFillOpacity))
                        )
                    }
                }

                let macOrUsb = item.isUsb ? "USB" : L10n.btMac(item.macAddress)
                Text(macOrUsb + (item.isAppleKeyboard ? "  •  \(L10n.btAppleKeyboard)" : ""))
                    .font(Theme.Typography.cardBody)
                    .foregroundColor(.secondary)
                    .lineLimit(1)
            }
            .frame(maxWidth: .infinity, alignment: .leading)

            if isBusy {
                ProgressView()
                    .controlSize(.small)
                    .padding(.horizontal, Theme.Spacing.l)
            } else if !item.isUsb {
                HStack(spacing: Theme.Spacing.s) {
                    if item.state == .connected || item.state == .pairedDisconnected {
                        if item.state == .connected {
                            Button(action: onDisconnect) {
                                Label(L10n.actionDisconnect, systemImage: Theme.Icons.disconnect)
                                    .font(Theme.Typography.button)
                                    .lineLimit(1)
                            }
                            .buttonStyle(.bordered)
                            .fixedSize(horizontal: true, vertical: false)
                        }

                        Button(action: onRelease) {
                            Label(L10n.actionRelease, systemImage: Theme.Icons.release)
                                .font(Theme.Typography.button)
                                .lineLimit(1)
                        }
                        .buttonStyle(.borderedProminent)
                        .tint(.accentColor)
                        .fixedSize(horizontal: true, vertical: false)
                    }

                    if item.state != .connected {
                        Button(action: onConnect) {
                            Label(L10n.actionConnect, systemImage: Theme.Icons.connect)
                                .font(Theme.Typography.button)
                                .lineLimit(1)
                        }
                        .buttonStyle(.borderedProminent)
                        .tint(Theme.Colors.stateConnected)
                        .fixedSize(horizontal: true, vertical: false)
                    }
                }
                .fixedSize(horizontal: true, vertical: false)
                .layoutPriority(1)
            }
        }
        .padding(.horizontal, Theme.Spacing.cardPaddingH)
        .padding(.vertical, Theme.Spacing.cardPaddingV)
        .background(
            RoundedRectangle(cornerRadius: Theme.Radius.card, style: .continuous)
                .fill(Theme.Colors.cardBackground)
        )
        .overlay(
            RoundedRectangle(cornerRadius: Theme.Radius.card, style: .continuous)
                .stroke(Theme.Colors.cardStroke, lineWidth: 1)
        )
    }
}

// MARK: - Modal de Bienvenida en 2 Pasos (Diseño unificado con Windows WinUI 3)

struct OnboardingModalView: View {
    @ObservedObject var settings: SettingsService
    let onDismiss: () -> Void
    @State private var step: Int = 0

    var body: some View {
        ZStack {
            Color.black.opacity(0.45)
                .ignoresSafeArea()

            VStack(alignment: .leading, spacing: Theme.Spacing.l) {
                if step == 0 {
                    setupStepView
                } else {
                    guideStepView
                }
            }
            .padding(28)
            .frame(maxWidth: 640)
            .background(
                RoundedRectangle(cornerRadius: 12, style: .continuous)
                    .fill(Color(NSColor.windowBackgroundColor))
            )
            .overlay(
                RoundedRectangle(cornerRadius: 12, style: .continuous)
                    .stroke(Theme.Colors.cardStroke, lineWidth: 1)
            )
            .shadow(color: Color.black.opacity(0.28), radius: 24, x: 0, y: 10)
            .padding(24)
        }
    }

    private var setupStepView: some View {
        VStack(alignment: .leading, spacing: Theme.Spacing.l) {
            HStack(spacing: Theme.Spacing.m) {
                ZStack {
                    RoundedRectangle(cornerRadius: Theme.Radius.iconTile, style: .continuous)
                        .fill(Color.accentColor)
                        .frame(width: Theme.Size.iconTile, height: Theme.Size.iconTile)
                    Image(systemName: Theme.Icons.keyboard)
                        .font(.system(size: Theme.Size.tileIcon, weight: .semibold))
                        .foregroundColor(.white)
                }

                VStack(alignment: .leading, spacing: 2) {
                    Text(L10n.onboardingSetupBadge)
                        .font(Theme.Typography.badge)
                        .foregroundColor(.accentColor)
                    Text(L10n.onboardingSetupTitle)
                        .font(.system(size: 20, weight: .semibold))
                        .foregroundColor(.primary)
                }
                Spacer()
            }

            Text(L10n.onboardingSetupSubtitle)
                .font(Theme.Typography.cardBody)
                .foregroundColor(.secondary)
                .fixedSize(horizontal: false, vertical: true)

            SettingsCard(
                icon: Theme.Icons.globe,
                title: L10n.layoutAppLanguageTitle,
                subtitle: L10n.layoutAppLanguageSubtitle
            ) {
                Picker("", selection: $settings.appLanguage) {
                    ForEach(AppLanguageMode.allCases) { mode in
                        Text(mode.label).tag(mode)
                    }
                }
                .labelsHidden()
                .frame(width: 200)
            }

            HStack {
                Spacer()
                Button(action: { step = 1 }) {
                    Text(L10n.onboardingNextButton)
                        .font(Theme.Typography.button)
                        .lineLimit(1)
                        .padding(.horizontal, Theme.Spacing.m)
                        .padding(.vertical, 4)
                }
                .buttonStyle(.borderedProminent)
                .keyboardShortcut(.defaultAction)
            }
            .padding(.top, 4)
        }
    }

    private var guideStepView: some View {
        VStack(alignment: .leading, spacing: Theme.Spacing.l) {
            HStack(spacing: Theme.Spacing.m) {
                ZStack {
                    RoundedRectangle(cornerRadius: Theme.Radius.iconTile, style: .continuous)
                        .fill(Color.accentColor)
                        .frame(width: Theme.Size.iconTile, height: Theme.Size.iconTile)
                    Image(systemName: Theme.Icons.keyboard)
                        .font(.system(size: Theme.Size.tileIcon, weight: .semibold))
                        .foregroundColor(.white)
                }

                VStack(alignment: .leading, spacing: 2) {
                    Text(L10n.onboardingBadge)
                        .font(Theme.Typography.badge)
                        .foregroundColor(.accentColor)
                    Text(L10n.onboardingTitle)
                        .font(.system(size: 20, weight: .semibold))
                        .foregroundColor(.primary)
                }
                Spacer()
            }

            Text(L10n.onboardingSubtitle)
                .font(Theme.Typography.cardBody)
                .foregroundColor(.secondary)
                .fixedSize(horizontal: false, vertical: true)

            VStack(spacing: 10) {
                onboardingStepCard(
                    icon: Theme.Icons.navBluetooth,
                    tint: .accentColor,
                    title: L10n.onboardingStep1Title,
                    body: L10n.onboardingStep1Body
                )

                onboardingStepCard(
                    icon: Theme.Icons.globe,
                    tint: Theme.Colors.stateConnected,
                    title: L10n.onboardingStep2Title,
                    body: L10n.onboardingStep2Body
                )

                onboardingStepCard(
                    icon: Theme.Icons.command,
                    tint: Theme.Colors.stateReleased,
                    title: L10n.onboardingStep3Title,
                    body: L10n.onboardingStep3Body
                )
            }

            HStack(alignment: .center, spacing: Theme.Spacing.m) {
                Text(L10n.onboardingReopenHint)
                    .font(Theme.Typography.caption)
                    .foregroundColor(.secondary)
                    .fixedSize(horizontal: false, vertical: true)
                    .frame(maxWidth: .infinity, alignment: .leading)

                Button(action: { step = 0 }) {
                    Text(L10n.onboardingBackButton)
                        .font(Theme.Typography.button)
                        .lineLimit(1)
                }
                .buttonStyle(.bordered)
                .fixedSize(horizontal: true, vertical: false)

                Button(action: onDismiss) {
                    Text(L10n.onboardingStartButton)
                        .font(Theme.Typography.button)
                        .lineLimit(1)
                        .padding(.horizontal, Theme.Spacing.m)
                        .padding(.vertical, 4)
                }
                .buttonStyle(.borderedProminent)
                .fixedSize(horizontal: true, vertical: false)
                .layoutPriority(1)
                .keyboardShortcut(.defaultAction)
            }
            .padding(.top, 4)
        }
    }

    private func onboardingStepCard(icon: String, tint: Color, title: String, body: String) -> some View {
        HStack(alignment: .top, spacing: Theme.Spacing.l) {
            ZStack {
                RoundedRectangle(cornerRadius: Theme.Radius.iconTile, style: .continuous)
                    .fill(tint.opacity(Theme.Colors.badgeFillOpacity))
                    .frame(width: 38, height: 38)
                Image(systemName: icon)
                    .font(.system(size: 16, weight: .semibold))
                    .foregroundColor(tint)
            }

            VStack(alignment: .leading, spacing: 3) {
                Text(title)
                    .font(Theme.Typography.cardTitle)
                    .foregroundColor(.primary)
                Text(body)
                    .font(Theme.Typography.cardBody)
                    .foregroundColor(.secondary)
                    .fixedSize(horizontal: false, vertical: true)
            }
            Spacer(minLength: 0)
        }
        .padding(.horizontal, 16)
        .padding(.vertical, 12)
        .background(
            RoundedRectangle(cornerRadius: Theme.Radius.card, style: .continuous)
                .fill(Theme.Colors.cardBackground)
        )
        .overlay(
            RoundedRectangle(cornerRadius: Theme.Radius.card, style: .continuous)
                .stroke(Theme.Colors.cardStroke, lineWidth: 1)
        )
    }
}
