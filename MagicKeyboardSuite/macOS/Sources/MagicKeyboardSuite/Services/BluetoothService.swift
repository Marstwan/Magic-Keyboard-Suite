import Foundation
import AppKit
import IOBluetooth
import IOKit
import Combine
import ServiceManagement

// MARK: - Delegado de Emparejamiento Bluetooth (IOBluetooth)
final class AutoPairDelegate: NSObject, IOBluetoothDevicePairDelegate {
    var onStatus: ((String) -> Void)?
    var onFinished: ((IOReturn) -> Void)?

    func devicePairingConnecting(_ sender: Any!) {
        onStatus?(L10n.statusConnectingBody)
    }

    func devicePairingPINCodeRequest(_ sender: Any!) {
        guard let pair = sender as? IOBluetoothDevicePair else { return }
        onStatus?(L10n.statusPairingPin("000000"))
        var pin = BluetoothPINCode()
        pin.data.0 = 0x30; pin.data.1 = 0x30; pin.data.2 = 0x30
        pin.data.3 = 0x30; pin.data.4 = 0x30; pin.data.5 = 0x30
        pair.replyPINCode(6, pinCode: &pin)
    }

    func devicePairingUserPasskeyNotification(_ sender: Any!, passkey: BluetoothPasskey) {
        let code = String(format: "%06u", passkey)
        onStatus?(L10n.statusPairingPin(code))
    }

    func devicePairingUserConfirmationRequest(_ sender: Any!, numericValue: BluetoothNumericValue) {
        guard let pair = sender as? IOBluetoothDevicePair else { return }
        pair.replyUserConfirmation(true)
    }

    func devicePairingFinished(_ sender: Any!, error: IOReturn) {
        onFinished?(error)
    }
}

// MARK: - Delegado de Escaneo Bluetooth Cercano
final class InquiryDelegate: NSObject, IOBluetoothDeviceInquiryDelegate {
    var foundDevices: [IOBluetoothDevice] = []
    var onComplete: (([IOBluetoothDevice]) -> Void)?

    func deviceInquiryDeviceFound(_ sender: IOBluetoothDeviceInquiry!, device: IOBluetoothDevice!) {
        if let dev = device { foundDevices.append(dev) }
    }

    func deviceInquiryComplete(_ sender: IOBluetoothDeviceInquiry!, error: IOReturn, aborted: Bool) {
        onComplete?(foundDevices)
    }
}

// MARK: - Observador por Eventos de IOBluetooth (sin polling corto)
private final class BluetoothNotificationBridge: NSObject {
    var onEvent: (() -> Void)?
    private var connectNotification: IOBluetoothUserNotification?
    private var disconnectNotifications: [String: IOBluetoothUserNotification] = [:]

    override init() {
        super.init()
        connectNotification = IOBluetoothDevice.register(
            forConnectNotifications: self,
            selector: #selector(handleDeviceConnected(_:device:))
        )
    }

    deinit {
        connectNotification?.unregister()
        disconnectNotifications.values.forEach { $0.unregister() }
        disconnectNotifications.removeAll()
    }

    @objc private func handleDeviceConnected(_ notification: IOBluetoothUserNotification?, device: IOBluetoothDevice?) {
        if let dev = device {
            let key = dev.addressString?.uppercased() ?? ObjectIdentifier(dev).debugDescription
            if let old = disconnectNotifications.removeValue(forKey: key) {
                old.unregister()
            }
            if let disc = dev.register(forDisconnectNotification: self, selector: #selector(handleDeviceDisconnected(_:device:))) {
                disconnectNotifications[key] = disc
            }
        }
        onEvent?()
    }

    @objc private func handleDeviceDisconnected(_ notification: IOBluetoothUserNotification?, device: IOBluetoothDevice?) {
        notification?.unregister()
        if let dev = device {
            let key = dev.addressString?.uppercased() ?? ObjectIdentifier(dev).debugDescription
            if let existing = disconnectNotifications.removeValue(forKey: key) {
                existing.unregister()
            }
        }
        onEvent?()
    }
}

// MARK: - Servicio de Teclados Bluetooth en macOS
@MainActor
final class BluetoothService: ObservableObject {
    @Published var selectedSection: SidebarSection = .bluetooth
    @Published var keyboards: [BluetoothKeyboardItem] = []

    @Published var isScanning: Bool = false
    @Published var busyMacAddress: String? = nil
    @Published var statusTitle: String = L10n.statusIdleTitle
    @Published var statusMessage: String = L10n.statusIdleBody
    @Published var statusIsSuccess: Bool = false

    let settings: SettingsService
    let inputSources: InputSourceService

    private var scannedNearby: [IOBluetoothDevice] = []
    private var inquiry: IOBluetoothDeviceInquiry?
    private var inquiryDelegate: InquiryDelegate?
    private var pairInstance: IOBluetoothDevicePair?
    private var pairDelegate: AutoPairDelegate?
    private let notificationBridge = BluetoothNotificationBridge()
    private var wakeObserver: NSObjectProtocol?
    private var cancellables = Set<AnyCancellable>()

    private var knownKeyboardsPath: String {
        let dir = (NSHomeDirectory() as NSString).appendingPathComponent(".config/magic-keyboard-suite")
        try? FileManager.default.createDirectory(atPath: dir, withIntermediateDirectories: true)
        return (dir as NSString).appendingPathComponent("known_keyboards.tsv")
    }

    init(settings: SettingsService, inputSources: InputSourceService) {
        self.settings = settings
        self.inputSources = inputSources

        notificationBridge.onEvent = { [weak self] in
            Task { @MainActor in
                self?.refreshKeyboards()
            }
        }

        settings.$deviceFilter
            .dropFirst()
            .receive(on: RunLoop.main)
            .sink { [weak self] _ in self?.refreshKeyboards() }
            .store(in: &cancellables)

        settings.$rememberReleased
            .dropFirst()
            .receive(on: RunLoop.main)
            .sink { [weak self] _ in self?.refreshKeyboards() }
            .store(in: &cancellables)

        settings.$appLanguage
            .dropFirst()
            .receive(on: RunLoop.main)
            .sink { [weak self] _ in
                guard let self = self else { return }
                if !self.isScanning && self.busyMacAddress == nil {
                    self.statusTitle = L10n.statusIdleTitle
                    self.statusMessage = L10n.statusIdleBody
                    self.statusIsSuccess = false
                }
                self.objectWillChange.send()
            }
            .store(in: &cancellables)

        wakeObserver = NSWorkspace.shared.notificationCenter.addObserver(
            forName: NSWorkspace.didWakeNotification,
            object: nil,
            queue: .main
        ) { [weak self] _ in
            Task { @MainActor in
                self?.refreshKeyboards()
            }
        }

        refreshKeyboards()
    }

    deinit {
        if let wakeObserver = wakeObserver {
            NSWorkspace.shared.notificationCenter.removeObserver(wakeObserver)
        }
    }

    private func normalizeMac(_ addr: String?) -> String {
        guard let addr = addr, !addr.isEmpty else { return "" }
        return addr.uppercased().replacingOccurrences(of: ":", with: "-")
    }

    private func isNonKeyboardAccessoryName(_ name: String) -> Bool {
        let lower = name.lowercased()
        let excluded = [
            "iphone", "ipad", "ipod", "watch", "airpods", "buds", "pixel buds",
            "beats", "homepod", "apple tv", "mouse", "ratón", "raton", "trackpad",
            "headphone", "auriculares", "speaker", "altavoz", "pencil", "airtag"
        ]
        return excluded.contains { lower.contains($0) }
    }

    private func isDeviceKeyboard(_ dev: IOBluetoothDevice) -> Bool {
        let name = dev.name ?? ""
        if isNonKeyboardAccessoryName(name) {
            return false
        }

        let major = dev.deviceClassMajor
        let minor = dev.deviceClassMinor

        if major == kBluetoothDeviceClassMajorAudio ||
           major == kBluetoothDeviceClassMajorPhone ||
           major == kBluetoothDeviceClassMajorWearable ||
           major == kBluetoothDeviceClassMajorImaging ||
           major == kBluetoothDeviceClassMajorComputer {
            return false
        }

        if major == kBluetoothDeviceClassMajorPeripheral && (minor & UInt32(kBluetoothDeviceClassMinorPeripheral1Keyboard)) != 0 {
            return true
        }

        let lower = name.lowercased()
        let keywords = ["keyboard", "teclado", "teclat", "clavier", "tastatur", "magic keyboard", "keychron", "k380", "mx keys", "nuphy", "hhkb"]
        return keywords.contains { lower.contains($0) }
    }

    private func isDeviceAppleKeyboard(_ dev: IOBluetoothDevice, name: String) -> Bool {
        guard isDeviceKeyboard(dev) else { return false }

        let lower = name.lowercased()
        let thirdParty = [
            "logi", "logitech", "keychron", "razer", "corsair", "steelseries",
            "redragon", "asus", "dell", "hp", "lenovo", "microsoft", "nuphy",
            "hhkb", "anne pro", "epomaker", "akko", "royal kludge", "rk61"
        ]
        if thirdParty.contains(where: { lower.contains($0) }) {
            return false
        }

        let major = dev.deviceClassMajor
        let minor = dev.deviceClassMinor
        if major == kBluetoothDeviceClassMajorPeripheral && (minor & UInt32(kBluetoothDeviceClassMinorPeripheral1Keyboard)) != 0 {
            return true
        }

        return lower.contains("magic keyboard") || lower.contains("apple") || lower.contains("teclat") || lower.contains("teclado") || lower.contains("keyboard")
    }

    private func loadKnownKeyboards() -> [String: String] {
        var dict: [String: String] = [:]
        guard let content = try? String(contentsOfFile: knownKeyboardsPath, encoding: .utf8) else { return dict }
        for line in content.components(separatedBy: .newlines) {
            let parts = line.components(separatedBy: "\t")
            if parts.count >= 2 {
                let mac = normalizeMac(parts[0])
                let name = parts[1].trimmingCharacters(in: .whitespacesAndNewlines)
                if !mac.isEmpty && !name.isEmpty {
                    dict[mac] = name
                }
            }
        }
        return dict
    }

    private func saveKnownKeyboards(_ dict: [String: String]) {
        guard settings.rememberReleased else { return }
        let lines = dict.map { "\($0.key)\t\($0.value)" }.joined(separator: "\n") + "\n"
        try? lines.write(toFile: knownKeyboardsPath, atomically: true, encoding: .utf8)
    }

    private func passesFilter(isApple: Bool, isKeyboard: Bool) -> Bool {
        switch settings.deviceFilter {
        case .apple: return isApple
        case .keyboards: return isKeyboard
        case .all: return true
        }
    }

    private func normalizeDeviceName(_ name: String) -> String {
        name.trimmingCharacters(in: .whitespacesAndNewlines)
            .replacingOccurrences(of: "\\s+", with: " ", options: .regularExpression)
            .uppercased()
    }

    /// Lee el porcentaje de batería (0..100) de todos los teclados Bluetooth/HID conectados desde IORegistry (`BatteryPercent`).
    private func readIORegistryBatteries() -> (byMac: [String: Int], byName: [String: Int]) {
        var byMac: [String: Int] = [:]
        var byName: [String: Int] = [:]

        // Consultamos solo clases HID relevantes en lugar de recorrer todo el árbol IOService del kernel
        let serviceClasses = [
            "AppleDeviceManagementHIDEventService",
            "IOHIDDevice",
            "IOBluetoothHIDDriver",
            "AppleBluetoothHIDKeyboard"
        ]

        for className in serviceClasses {
            var iterator: io_iterator_t = 0
            guard IOServiceGetMatchingServices(kIOMainPortDefault, IOServiceMatching(className), &iterator) == KERN_SUCCESS else {
                continue
            }
            defer { IOObjectRelease(iterator) }

            while case let service = IOIteratorNext(iterator), service != 0 {
                defer { IOObjectRelease(service) }
                guard let unmanagedProps = IORegistryEntryCreateCFProperty(service, "BatteryPercent" as CFString, kCFAllocatorDefault, 0),
                      let battery = unmanagedProps.takeRetainedValue() as? Int,
                      (0...100).contains(battery) else {
                    continue
                }

                if let addrProp = IORegistryEntryCreateCFProperty(service, "DeviceAddress" as CFString, kCFAllocatorDefault, 0),
                   let rawAddr = addrProp.takeRetainedValue() as? String {
                    let mac = normalizeMac(rawAddr)
                    if !mac.isEmpty { byMac[mac] = battery }
                }

                if let prodProp = IORegistryEntryCreateCFProperty(service, "Product" as CFString, kCFAllocatorDefault, 0),
                   let prodName = prodProp.takeRetainedValue() as? String {
                    let norm = normalizeDeviceName(prodName)
                    if !norm.isEmpty { byName[norm] = battery }
                }
            }
        }
        return (byMac, byName)
    }

    private func resolveBatteryPercent(dev: IOBluetoothDevice?, mac: String, name: String, ioReg: (byMac: [String: Int], byName: [String: Int])) -> Int? {
        if let b = ioReg.byMac[mac], (1...100).contains(b) { return b }
        let norm = normalizeDeviceName(name)
        if !norm.isEmpty, let b = ioReg.byName[norm], (1...100).contains(b) { return b }

        if let dev = dev {
            for selName in ["batteryPercent", "batteryPercentSingle"] {
                let sel = NSSelectorFromString(selName)
                if dev.responds(to: sel), let val = dev.value(forKey: selName) as? Int, (1...100).contains(val) {
                    return val
                }
            }
        }
        return nil
    }

    /// Detecta teclados externos de Apple conectados por cable USB (Lightning / USB-C) en IORegistry (`IOHIDDevice`).
    private func readWiredAppleKeyboards() -> [(name: String, battery: Int?)] {
        var found: [(name: String, battery: Int?)] = []
        var seen = Set<String>()

        var iterator: io_iterator_t = 0
        guard IOServiceGetMatchingServices(kIOMainPortDefault, IOServiceMatching("IOHIDDevice"), &iterator) == KERN_SUCCESS else {
            return found
        }
        defer { IOObjectRelease(iterator) }

        while case let service = IOIteratorNext(iterator), service != 0 {
            defer { IOObjectRelease(service) }

            let vendorId = (IORegistryEntryCreateCFProperty(service, "VendorID" as CFString, kCFAllocatorDefault, 0)?.takeRetainedValue() as? Int) ?? 0
            let usagePage = (IORegistryEntryCreateCFProperty(service, "PrimaryUsagePage" as CFString, kCFAllocatorDefault, 0)?.takeRetainedValue() as? Int) ?? 0
            let usage = (IORegistryEntryCreateCFProperty(service, "PrimaryUsage" as CFString, kCFAllocatorDefault, 0)?.takeRetainedValue() as? Int) ?? 0
            let transport = (IORegistryEntryCreateCFProperty(service, "Transport" as CFString, kCFAllocatorDefault, 0)?.takeRetainedValue() as? String) ?? ""
            let builtIn = (IORegistryEntryCreateCFProperty(service, "Built-In" as CFString, kCFAllocatorDefault, 0)?.takeRetainedValue() as? Bool) ?? false

            guard vendorId == 0x05AC && usagePage == 1 && usage == 6 && transport.uppercased() == "USB" && !builtIn else {
                continue
            }

            let rawName = (IORegistryEntryCreateCFProperty(service, "Product" as CFString, kCFAllocatorDefault, 0)?.takeRetainedValue() as? String) ?? "Magic Keyboard"
            if isNonKeyboardAccessoryName(rawName) || rawName.lowercased().contains("internal") {
                continue
            }

            let norm = normalizeDeviceName(rawName)
            guard !norm.isEmpty && !seen.contains(norm) else { continue }
            seen.insert(norm)

            let battery = IORegistryEntryCreateCFProperty(service, "BatteryPercent" as CFString, kCFAllocatorDefault, 0)?.takeRetainedValue() as? Int
            let validBattery = (battery != nil && (1...100).contains(battery!)) ? battery : nil
            found.append((name: rawName, battery: validBattery))
        }

        return found
    }

    func refreshKeyboards() {
        var known = loadKnownKeyboards()
        var knownChanged = false
        var seenMacs = Set<String>()
        var seenNames = Set<String>()
        var result: [BluetoothKeyboardItem] = []
        let ioRegBatteries = readIORegistryBatteries()

        for usbKbd in readWiredAppleKeyboards() {
            let normName = normalizeDeviceName(usbKbd.name)
            guard passesFilter(isApple: true, isKeyboard: true) else { continue }
            if !normName.isEmpty && !seenNames.contains(normName) {
                seenNames.insert(normName)
                result.append(BluetoothKeyboardItem(
                    name: usbKbd.name,
                    macAddress: "USB",
                    state: .connected,
                    isKeyboardClass: true,
                    isAppleKeyboard: true,
                    batteryPercent: usbKbd.battery ?? ioRegBatteries.byName[normName]
                ))
            }
        }

        if let paired = IOBluetoothDevice.pairedDevices() as? [IOBluetoothDevice] {
            for dev in paired {
                let mac = normalizeMac(dev.addressString)
                guard !mac.isEmpty else { continue }
                let name = dev.name ?? known[mac] ?? "Magic Keyboard"
                let isKbd = isDeviceKeyboard(dev)
                let isApple = isDeviceAppleKeyboard(dev, name: name)

                if isKbd {
                    if known[mac] != name {
                        known[mac] = name
                        knownChanged = true
                    }
                } else if known[mac] != nil {
                    known.removeValue(forKey: mac)
                    knownChanged = true
                }

                guard passesFilter(isApple: isApple, isKeyboard: isKbd) else { continue }

                let normName = normalizeDeviceName(name)
                guard !seenMacs.contains(mac) && (normName.isEmpty || !seenNames.contains(normName)) else { continue }
                seenMacs.insert(mac)
                if !normName.isEmpty { seenNames.insert(normName) }

                let connected = dev.isConnected()
                let battery = connected ? resolveBatteryPercent(dev: dev, mac: mac, name: name, ioReg: ioRegBatteries) : nil

                result.append(BluetoothKeyboardItem(
                    name: name,
                    macAddress: mac,
                    state: connected ? .connected : .pairedDisconnected,
                    isKeyboardClass: isKbd,
                    isAppleKeyboard: isApple,
                    batteryPercent: battery
                ))
            }
        }

        if settings.rememberReleased {
            for (mac, name) in known {
                if isNonKeyboardAccessoryName(name) {
                    known.removeValue(forKey: mac)
                    knownChanged = true
                    continue
                }
                let normName = normalizeDeviceName(name)
                if !seenMacs.contains(mac) && (normName.isEmpty || !seenNames.contains(normName)) {
                    seenMacs.insert(mac)
                    if !normName.isEmpty { seenNames.insert(normName) }
                    result.append(BluetoothKeyboardItem(
                        name: name,
                        macAddress: mac,
                        state: .releasedToPeer,
                        isKeyboardClass: true,
                        isAppleKeyboard: true,
                        batteryPercent: nil
                    ))
                }
            }
        }

        if knownChanged {
            saveKnownKeyboards(known)
        }

        for dev in scannedNearby {
            let mac = normalizeMac(dev.addressString)
            guard !mac.isEmpty else { continue }
            let name = dev.name ?? "Magic Keyboard"
            let isKbd = isDeviceKeyboard(dev)
            let isApple = isDeviceAppleKeyboard(dev, name: name)

            guard passesFilter(isApple: isApple, isKeyboard: isKbd) else { continue }

            let normName = normalizeDeviceName(name)
            guard !seenMacs.contains(mac) && (normName.isEmpty || !seenNames.contains(normName)) else { continue }
            seenMacs.insert(mac)
            if !normName.isEmpty { seenNames.insert(normName) }

            result.append(BluetoothKeyboardItem(
                name: name,
                macAddress: mac,
                state: .nearbyVisible,
                isKeyboardClass: isKbd,
                isAppleKeyboard: isApple,
                batteryPercent: nil
            ))
        }

        result.sort { a, b in
            if a.state.sortOrder != b.state.sortOrder {
                return a.state.sortOrder < b.state.sortOrder
            }
            return a.name.localizedCaseInsensitiveCompare(b.name) == .orderedAscending
        }

        if result != keyboards {
            keyboards = result
        }
    }

    // MARK: - Acciones Bluetooth
    func releaseForPeer(_ item: BluetoothKeyboardItem) {
        busyMacAddress = item.macAddress
        statusIsSuccess = false
        statusTitle = L10n.statusReleasingTitle(item.name)
        statusMessage = L10n.statusReleasingBody

        var known = loadKnownKeyboards()
        known[item.macAddress] = item.name
        saveKnownKeyboards(known)

        Task.detached { [mac = item.macAddress, name = item.name] in
            if let dev = IOBluetoothDevice(addressString: mac) {
                if dev.isConnected() {
                    _ = dev.closeConnection()
                    try? await Task.sleep(nanoseconds: 500_000_000)
                }
                let removeSel = NSSelectorFromString("remove")
                if dev.responds(to: removeSel) {
                    _ = dev.perform(removeSel)
                }
            }
            await MainActor.run { [weak self] in
                self?.busyMacAddress = nil
                self?.statusIsSuccess = true
                self?.statusTitle = L10n.statusReleasedTitle(name)
                self?.statusMessage = L10n.statusReleasedBody
                self?.refreshKeyboards()
            }
        }
    }

    func disconnectOnly(_ item: BluetoothKeyboardItem) {
        busyMacAddress = item.macAddress
        statusIsSuccess = false
        statusTitle = L10n.statusDisconnectingTitle(item.name)
        statusMessage = L10n.statusDisconnectedBody

        Task.detached { [mac = item.macAddress, name = item.name] in
            if let dev = IOBluetoothDevice(addressString: mac), dev.isConnected() {
                _ = dev.closeConnection()
                try? await Task.sleep(nanoseconds: 500_000_000)
            }
            await MainActor.run { [weak self] in
                self?.busyMacAddress = nil
                self?.statusIsSuccess = true
                self?.statusTitle = L10n.statusDisconnectedTitle(name)
                self?.statusMessage = L10n.statusDisconnectedBody
                self?.refreshKeyboards()
            }
        }
    }

    func connectHere(_ item: BluetoothKeyboardItem) {
        busyMacAddress = item.macAddress
        statusIsSuccess = false
        statusTitle = L10n.statusConnectingTitle(item.name)
        statusMessage = L10n.statusConnectingBody

        guard let dev = IOBluetoothDevice(addressString: item.macAddress) else {
            busyMacAddress = nil
            statusTitle = L10n.statusNotFoundTitle(item.macAddress)
            return
        }

        if dev.isPaired() {
            Task.detached { [name = item.name] in
                let kr = dev.openConnection()
                let connected = (kr == kIOReturnSuccess) || dev.isConnected()
                await MainActor.run { [weak self] in
                    guard let self = self else { return }
                    if connected {
                        self.busyMacAddress = nil
                        self.statusIsSuccess = true
                        self.statusTitle = L10n.statusConnectedTitle(name)
                        self.statusMessage = L10n.statusConnectedBody
                        self.refreshKeyboards()
                    } else {
                        self.startPairing(device: dev, item: item)
                    }
                }
            }
        } else {
            startPairing(device: dev, item: item)
        }
    }

    private func startPairing(device: IOBluetoothDevice, item: BluetoothKeyboardItem) {
        let delegate = AutoPairDelegate()
        delegate.onStatus = { [weak self] msg in
            Task { @MainActor in
                self?.statusMessage = msg
            }
        }
        delegate.onFinished = { [weak self] error in
            Task { @MainActor in
                guard let self = self else { return }
                self.busyMacAddress = nil
                if error == kIOReturnSuccess || device.isPaired() || device.isConnected() {
                    _ = device.openConnection()
                    self.statusIsSuccess = true
                    self.statusTitle = L10n.statusConnectedTitle(item.name)
                    self.statusMessage = L10n.statusConnectedBody
                } else {
                    self.statusIsSuccess = false
                    self.statusTitle = L10n.statusPairingFailedTitle(item.name)
                    self.statusMessage = L10n.statusPairingFailedBody
                    if let url = URL(string: "x-apple.systempreferences:com.apple.BluetoothSettings") {
                        NSWorkspace.shared.open(url)
                    }
                }
                self.refreshKeyboards()
            }
        }

        self.pairDelegate = delegate
        if let pair = IOBluetoothDevicePair(device: device) {
            pair.delegate = delegate
            self.pairInstance = pair
            let res = pair.start()
            if res != kIOReturnSuccess {
                busyMacAddress = nil
                if let url = URL(string: "x-apple.systempreferences:com.apple.BluetoothSettings") {
                    NSWorkspace.shared.open(url)
                }
            }
        }
    }

    func scanNearbyDevices() {
        guard !isScanning else { return }
        isScanning = true
        statusIsSuccess = false
        statusTitle = L10n.statusScanningTitle
        statusMessage = L10n.statusScanningBody

        let del = InquiryDelegate()
        del.onComplete = { [weak self] devices in
            Task { @MainActor in
                guard let self = self else { return }
                self.scannedNearby = devices
                self.isScanning = false
                self.refreshKeyboards()
                self.statusIsSuccess = true
                self.statusTitle = L10n.statusScanDoneTitle
                self.statusMessage = L10n.statusScanDoneBody
            }
        }
        self.inquiryDelegate = del
        if let inq = IOBluetoothDeviceInquiry(delegate: del) {
            inq.inquiryLength = 6
            inq.updateNewDeviceNames = true
            self.inquiry = inq
            inq.start()
        } else {
            isScanning = false
        }
    }

    func openMacKeyboardSettings() {
        if let url = URL(string: "x-apple.systempreferences:com.apple.Keyboard-Settings.extension") {
            NSWorkspace.shared.open(url)
        }
    }

    func uninstallApp() {
        if #available(macOS 13.0, *) {
            try? SMAppService.mainApp.unregister()
        }
        UserDefaults.standard.removePersistentDomain(forName: "com.magickeyboardsuite")
        let configDir = (NSHomeDirectory() as NSString).appendingPathComponent(".config/magic-keyboard-suite")
        try? FileManager.default.removeItem(atPath: configDir)

        let bundleURL = Bundle.main.bundleURL
        if bundleURL.pathExtension == "app" {
            NSWorkspace.shared.recycle([bundleURL]) { _, _ in
                DispatchQueue.main.async {
                    NSApplication.shared.terminate(nil)
                }
            }
        } else {
            NSApplication.shared.terminate(nil)
        }
    }
}
