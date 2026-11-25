import SwiftUI
import AppKit

// MARK: - Vista Principal (Estructura y Tokens idénticos a Windows WinUI 3)

struct MainView: View {
    @ObservedObject var bluetooth: BluetoothService
    @ObservedObject var settings: SettingsService
    @ObservedObject var inputSources: InputSourceService

    var body: some View {
        NavigationSplitView {
            VStack(spacing: 0) {
                List(SidebarSection.allCases, selection: $bluetooth.selectedSection) { section in
                    NavigationLink(value: section) {
                        Label(section.title, systemImage: section.icon)
                            .font(Theme.Typography.button)
                            .padding(.vertical, Theme.Spacing.xs)
                    }
                }
                .listStyle(.sidebar)

                // Footer idéntico al PaneFooter de WinUI 3
                VStack(alignment: .leading, spacing: Theme.Spacing.xs) {
                    HStack(spacing: Theme.Spacing.s) {
                        Circle()
                            .fill(Theme.Colors.stateConnected)
                            .frame(width: Theme.Size.statusDot, height: Theme.Size.statusDot)
                        Text(L10n.footerEngineActive)
                            .font(Theme.Typography.badge)
                    }
                    Text(L10n.footerCurrentLayout(inputSources.currentLayoutName))
                        .font(Theme.Typography.caption)
                        .foregroundColor(.secondary)
                        .lineLimit(1)
                }
                .frame(maxWidth: .infinity, alignment: .leading)
                .padding(.horizontal, 14)
                .padding(.vertical, Theme.Spacing.m)
                .background(
                    RoundedRectangle(cornerRadius: Theme.Radius.card, style: .continuous)
                        .fill(Theme.Colors.cardBackground)
                )
                .overlay(
                    RoundedRectangle(cornerRadius: Theme.Radius.card, style: .continuous)
                        .stroke(Theme.Colors.cardStroke, lineWidth: 1)
                )
                .padding(Theme.Spacing.m)
            }
            .navigationSplitViewColumnWidth(
                min: Theme.Size.sidebarWidth - 20,
                ideal: Theme.Size.sidebarWidth,
                max: Theme.Size.sidebarWidth + 24
            )
        } detail: {
            ScrollView {
                VStack(alignment: .leading, spacing: Theme.Spacing.l) {
                    switch bluetooth.selectedSection {
                    case .bluetooth:
                        bluetoothSectionView
                    case .layout:
                        layoutSectionView
                    case .shortcuts:
                        shortcutsSectionView
                    case .guide:
                        guideSectionView
                    }
                }
                .frame(maxWidth: Theme.Size.contentMaxWidth, alignment: .leading)
                .padding(.horizontal, Theme.Spacing.pagePaddingH)
                .padding(.vertical, Theme.Spacing.pagePaddingV)
                .frame(maxWidth: .infinity, alignment: .leading)
            }
            .frame(maxWidth: .infinity, maxHeight: .infinity, alignment: .topLeading)
            .background(Color(NSColor.windowBackgroundColor))
            .navigationSubtitle(L10n.appVersion(AppInfo.version))
        }
        .frame(minWidth: Theme.Size.windowMinWidth, minHeight: Theme.Size.windowMinHeight)
        .overlay {
            if settings.isShowingOnboarding {
                OnboardingModalView(settings: settings) {
                    withAnimation(.easeInOut(duration: 0.2)) {
                        settings.hasSeenOnboarding = true
                        settings.isShowingOnboarding = false
                    }
                }
                .transition(.opacity)
            }
        }
    }

    // MARK: - Página 1: Teclados Bluetooth
    private var bluetoothSectionView: some View {
        VStack(alignment: .leading, spacing: Theme.Spacing.l) {
            HStack(alignment: .center, spacing: Theme.Spacing.l) {
                VStack(alignment: .leading, spacing: Theme.Spacing.xs) {
                    Text(L10n.btTitle)
                        .font(Theme.Typography.pageTitle)
                    Text(L10n.btSubtitle)
                        .font(Theme.Typography.pageSubtitle)
                        .foregroundColor(.secondary)
                        .fixedSize(horizontal: false, vertical: true)
                }
                .frame(maxWidth: .infinity, alignment: .leading)

                HStack(spacing: Theme.Spacing.s) {
                    Button(action: { bluetooth.refreshKeyboards() }) {
                        Label(L10n.btRefresh, systemImage: Theme.Icons.refresh)
                            .lineLimit(1)
                    }
                    .buttonStyle(.bordered)
                    .fixedSize(horizontal: true, vertical: false)

                    Button(action: { bluetooth.scanNearbyDevices() }) {
                        if bluetooth.isScanning {
                            HStack(spacing: 6) {
                                ProgressView().controlSize(.small)
                                Text(L10n.btScanning)
                                    .lineLimit(1)
                            }
                        } else {
                            Label(L10n.btScan, systemImage: Theme.Icons.scan)
                                .lineLimit(1)
                        }
                    }
                    .buttonStyle(.borderedProminent)
                    .fixedSize(horizontal: true, vertical: false)
                    .disabled(bluetooth.isScanning)
                }
                .fixedSize(horizontal: true, vertical: false)
                .layoutPriority(1)
            }
            .padding(.bottom, Theme.Spacing.xs)

            // InfoBar unificada
            HStack(alignment: .top, spacing: Theme.Spacing.m) {
                Image(systemName: bluetooth.statusIsSuccess ? Theme.Icons.success : Theme.Icons.info)
                    .font(.system(size: 16))
                    .foregroundColor(bluetooth.statusIsSuccess ? Theme.Colors.stateConnected : .accentColor)
                    .padding(.top, 1)

                VStack(alignment: .leading, spacing: 2) {
                    Text(bluetooth.statusTitle)
                        .font(Theme.Typography.cardTitle)
                    Text(bluetooth.statusMessage)
                        .font(Theme.Typography.cardBody)
                        .foregroundColor(.secondary)
                        .fixedSize(horizontal: false, vertical: true)
                }
                Spacer()
            }
            .padding(.horizontal, Theme.Spacing.l)
            .padding(.vertical, Theme.Spacing.m)
            .background(
                RoundedRectangle(cornerRadius: Theme.Radius.card, style: .continuous)
                    .fill((bluetooth.statusIsSuccess ? Theme.Colors.stateConnected : Color.accentColor).opacity(0.10))
            )
            .overlay(
                RoundedRectangle(cornerRadius: Theme.Radius.card, style: .continuous)
                    .stroke((bluetooth.statusIsSuccess ? Theme.Colors.stateConnected : Color.accentColor).opacity(0.28), lineWidth: 1)
            )

            // Lista de teclados o estado vacío
            if bluetooth.keyboards.isEmpty {
                VStack(spacing: Theme.Spacing.s) {
                    Image(systemName: Theme.Icons.keyboard)
                        .font(.system(size: 30, weight: .light))
                        .foregroundColor(.secondary)
                    Text(L10n.btEmptyTitle)
                        .font(Theme.Typography.cardTitle)
                    Text(L10n.btEmptyBody)
                        .font(Theme.Typography.cardBody)
                        .foregroundColor(.secondary)
                        .multilineTextAlignment(.center)
                        .frame(maxWidth: 540)
                }
                .frame(maxWidth: .infinity)
                .padding(.vertical, 36)
                .background(
                    RoundedRectangle(cornerRadius: Theme.Radius.card, style: .continuous)
                        .fill(Theme.Colors.cardBackground)
                )
                .overlay(
                    RoundedRectangle(cornerRadius: Theme.Radius.card, style: .continuous)
                        .stroke(Theme.Colors.cardStroke, lineWidth: 1)
                )
            } else {
                VStack(spacing: 10) {
                    ForEach(bluetooth.keyboards) { item in
                        KeyboardCard(
                            item: item,
                            isBusy: !item.isUsb && bluetooth.busyMacAddress == item.macAddress,
                            onRelease: { bluetooth.releaseForPeer(item) },
                            onDisconnect: { bluetooth.disconnectOnly(item) },
                            onConnect: { bluetooth.connectHere(item) }
                        )
                    }
                }
            }

            // Filtro unificado (mismo selector de 3 opciones en macOS y Windows)
            VStack(spacing: 10) {
                SettingsCard(
                    icon: Theme.Icons.filter,
                    title: L10n.btFilterTitle,
                    subtitle: L10n.btFilterSubtitle
                ) {
                    Picker("", selection: $settings.deviceFilter) {
                        ForEach(DeviceFilterMode.allCases) { mode in
                            Text(mode.label).tag(mode)
                        }
                    }
                    .labelsHidden()
                    .frame(width: Theme.Size.comboWidth)
                }
            }
            .padding(.top, Theme.Spacing.xs)
        }
    }

    // MARK: - Página 2: Disposición e Idioma
    private var layoutSectionView: some View {
        VStack(alignment: .leading, spacing: Theme.Spacing.l) {
            VStack(alignment: .leading, spacing: Theme.Spacing.xs) {
                Text(L10n.layoutTitle)
                    .font(Theme.Typography.pageTitle)
                Text(L10n.layoutSubtitle)
                    .font(Theme.Typography.pageSubtitle)
                    .foregroundColor(.secondary)
            }
            .padding(.bottom, Theme.Spacing.xs)

            SettingsCard(
                icon: Theme.Icons.globe,
                title: L10n.layoutActiveTitle,
                subtitle: L10n.layoutActiveSubtitle(inputSources.currentLayoutName)
            ) {
                Picker("", selection: $settings.selectedLayoutKlid) {
                    ForEach(AppleLayoutDefinition.all) { layout in
                        Text(layout.name).tag(layout.klid)
                    }
                }
                .labelsHidden()
                .frame(width: Theme.Size.comboWidth)
                .onChange(of: settings.selectedLayoutKlid) { newKlid in
                    if !settings.isExternalLayoutSync {
                        inputSources.selectLayout(klid: newKlid)
                    }
                }
            }

            SettingsCard(
                icon: Theme.Icons.globe,
                title: L10n.layoutAppLanguageTitle,
                subtitle: L10n.layoutAppLanguageSubtitle
            ) {
                Picker("", selection: $settings.appLanguage) {
                    ForEach(AppLanguageMode.allCases) { lang in
                        Text(lang.label).tag(lang)
                    }
                }
                .labelsHidden()
                .frame(width: Theme.Size.comboWidth)
            }

            SettingsCard(
                icon: Theme.Icons.swap,
                title: L10n.layoutIsoTitle,
                subtitle: L10n.layoutIsoSubtitle
            ) {
                nativeBadge
            }

            SettingsCard(
                icon: Theme.Icons.option,
                title: L10n.layoutOptionTitle,
                subtitle: L10n.layoutOptionSubtitle
            ) {
                nativeBadge
            }

            SettingsCard(
                icon: Theme.Icons.memory,
                title: L10n.layoutRememberTitle,
                subtitle: L10n.layoutRememberSubtitle
            ) {
                Toggle("", isOn: $settings.rememberReleased)
                    .labelsHidden()
                    .toggleStyle(.switch)
            }
        }
    }

    private var nativeBadge: some View {
        Text(L10n.layoutNative)
            .font(Theme.Typography.badge)
            .foregroundColor(Theme.Colors.stateConnected)
            .lineLimit(1)
            .padding(.horizontal, 10)
            .padding(.vertical, 4)
            .background(
                RoundedRectangle(cornerRadius: Theme.Radius.badge, style: .continuous)
                    .fill(Theme.Colors.stateConnected.opacity(Theme.Colors.badgeFillOpacity))
            )
    }

    // MARK: - Página 3: Atajos y Multimedia
    private var shortcutsSectionView: some View {
        VStack(alignment: .leading, spacing: Theme.Spacing.l) {
            VStack(alignment: .leading, spacing: Theme.Spacing.xs) {
                Text(L10n.shortcutsTitle)
                    .font(Theme.Typography.pageTitle)
                Text(L10n.shortcutsSubtitle)
                    .font(Theme.Typography.pageSubtitle)
                    .foregroundColor(.secondary)
            }
            .padding(.bottom, Theme.Spacing.xs)

            SettingsCard(
                icon: Theme.Icons.command,
                title: L10n.shortcutsModifiersTitle,
                subtitle: L10n.shortcutsModifiersSubtitle
            ) {
                Picker("", selection: $settings.modifierProfileIndex) {
                    Text(L10n.shortcutsModifiersMacNative).tag(0)
                    Text(L10n.shortcutsModifiersMacCustom).tag(1)
                }
                .labelsHidden()
                .frame(width: Theme.Size.comboWidth)
                .onChange(of: settings.modifierProfileIndex) { newVal in
                    if newVal == 1 {
                        bluetooth.openMacKeyboardSettings()
                        settings.modifierProfileIndex = 0
                    }
                }
            }

            SettingsCard(
                icon: Theme.Icons.delete,
                title: L10n.shortcutsNavTitle,
                subtitle: L10n.shortcutsNavSubtitle
            ) {
                nativeBadge
            }

            SettingsCard(
                icon: Theme.Icons.media,
                title: L10n.shortcutsMediaTitle,
                subtitle: L10n.shortcutsMediaSubtitle
            ) {
                Button(L10n.shortcutsOpenKeyboardSettings) {
                    bluetooth.openMacKeyboardSettings()
                }
                .buttonStyle(.bordered)
                .fixedSize(horizontal: true, vertical: false)
            }
        }
    }

    // MARK: - Página 4: Guía Mac ↔ Windows
    private var guideSectionView: some View {
        VStack(alignment: .leading, spacing: Theme.Spacing.l) {
            VStack(alignment: .leading, spacing: Theme.Spacing.xs) {
                Text(L10n.guideTitle)
                    .font(Theme.Typography.pageTitle)
                Text(L10n.guideSubtitle)
                    .font(Theme.Typography.pageSubtitle)
                    .foregroundColor(.secondary)
            }
            .padding(.bottom, Theme.Spacing.xs)

            HStack(alignment: .top, spacing: Theme.Spacing.l) {
                guideCard(
                    icon: Theme.Icons.release,
                    tint: .accentColor,
                    title: L10n.guideToWindowsTitle,
                    steps: [L10n.guideToWindows1, L10n.guideToWindows2, L10n.guideToWindows3]
                )

                guideCard(
                    icon: Theme.Icons.connect,
                    tint: Theme.Colors.stateConnected,
                    title: L10n.guideToMacTitle,
                    steps: [L10n.guideToMac1, L10n.guideToMac2, L10n.guideToMac3]
                )
            }

            VStack(alignment: .leading, spacing: Theme.Spacing.s) {
                Text(L10n.guideSymbolsTitle)
                    .font(Theme.Typography.sectionTitle)
                Text("• " + L10n.guideSymbols1)
                    .font(Theme.Typography.cardBody)
                    .foregroundColor(.secondary)
                Text("• " + L10n.guideSymbols2)
                    .font(Theme.Typography.cardBody)
                    .foregroundColor(.secondary)
                Text("• " + L10n.guideSymbols3)
                    .font(Theme.Typography.cardBody)
                    .foregroundColor(.secondary)
            }
            .frame(maxWidth: .infinity, alignment: .leading)
            .padding(Theme.Spacing.cardPaddingH)
            .background(
                RoundedRectangle(cornerRadius: Theme.Radius.card, style: .continuous)
                    .fill(Theme.Colors.cardBackground)
            )
            .overlay(
                RoundedRectangle(cornerRadius: Theme.Radius.card, style: .continuous)
                    .stroke(Theme.Colors.cardStroke, lineWidth: 1)
            )

            SettingsCard(
                icon: Theme.Icons.info,
                title: L10n.onboardingTitle,
                subtitle: L10n.onboardingSubtitle
            ) {
                Button(action: {
                    withAnimation(.easeInOut(duration: 0.2)) {
                        settings.isShowingOnboarding = true
                    }
                }) {
                    Label(L10n.onboardingReopenButton, systemImage: Theme.Icons.navGuide)
                        .lineLimit(1)
                }
                .buttonStyle(.bordered)
                .fixedSize(horizontal: true, vertical: false)
            }

            SettingsCard(
                icon: Theme.Icons.uninstall,
                title: L10n.uninstallTitle,
                subtitle: L10n.uninstallSubtitle
            ) {
                Button(role: .destructive, action: { bluetooth.uninstallApp() }) {
                    Label(L10n.uninstallButton, systemImage: Theme.Icons.uninstall)
                        .lineLimit(1)
                }
                .buttonStyle(.bordered)
                .fixedSize(horizontal: true, vertical: false)
            }
        }
    }

    private func guideCard(icon: String, tint: Color, title: String, steps: [String]) -> some View {
        VStack(alignment: .leading, spacing: 10) {
            HStack(spacing: Theme.Spacing.s) {
                Image(systemName: icon)
                    .foregroundColor(tint)
                Text(title)
                    .font(Theme.Typography.sectionTitle)
            }
            ForEach(Array(steps.enumerated()), id: \.offset) { idx, step in
                Text("\(idx + 1). \(step)")
                    .font(Theme.Typography.cardBody)
                    .foregroundColor(.secondary)
            }
        }
        .frame(maxWidth: .infinity, alignment: .leading)
        .padding(Theme.Spacing.cardPaddingH)
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
