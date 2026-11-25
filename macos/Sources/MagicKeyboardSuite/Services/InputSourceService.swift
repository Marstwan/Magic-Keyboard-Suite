import Foundation
import Carbon
import AppKit

// MARK: - Servicio de Detección y Cambio de Disposición de Teclado (Carbon TIS)

@MainActor
final class InputSourceService: ObservableObject {
    @Published private(set) var currentLayoutName: String = AppleLayoutDefinition.all[0].name
    @Published var currentKlid: String = AppleLayoutDefinition.all[0].klid

    private var observer: NSObjectProtocol?
    private var isSwitching: Bool = false

    init() {
        refreshCurrentInputSource()
        observer = DistributedNotificationCenter.default().addObserver(
            forName: NSNotification.Name(kTISNotifySelectedKeyboardInputSourceChanged as String),
            object: nil,
            queue: .main
        ) { [weak self] _ in
            Task { @MainActor in
                guard let self = self, !self.isSwitching else { return }
                self.refreshCurrentInputSource()
            }
        }
    }

    deinit {
        if let observer = observer {
            DistributedNotificationCenter.default().removeObserver(observer)
        }
    }

    private func stringProperty(_ src: TISInputSource, _ key: CFString) -> String? {
        guard let ptr = TISGetInputSourceProperty(src, key) else { return nil }
        return Unmanaged<CFString>.fromOpaque(ptr).takeUnretainedValue() as String
    }

    private func languagesProperty(_ src: TISInputSource) -> [String] {
        guard let ptr = TISGetInputSourceProperty(src, kTISPropertyInputSourceLanguages) else { return [] }
        let arr = Unmanaged<CFArray>.fromOpaque(ptr).takeUnretainedValue() as NSArray
        return arr.compactMap { $0 as? String }
    }

    func refreshCurrentInputSource() {
        guard let src = TISCopyCurrentKeyboardInputSource()?.takeRetainedValue() else {
            return
        }
        if let name = stringProperty(src, kTISPropertyLocalizedName), !name.isEmpty {
            currentLayoutName = name
        }

        guard !isSwitching else { return }

        let sourceID = stringProperty(src, kTISPropertyInputSourceID) ?? ""
        if let exact = AppleLayoutDefinition.all.first(where: { $0.macInputSourceIDs.contains(sourceID) }) {
            if currentKlid != exact.klid {
                currentKlid = exact.klid
            }
            SettingsService.shared.syncSelectedLayoutFromSystem(exact.klid)
            return
        }

        // Solo comparar el idioma principal (el primero de la lista), ya que macOS incluye decenas de idiomas secundarios en cada layout
        let langs = languagesProperty(src)
        if let primary = langs.first?.lowercased(),
           let byLang = AppleLayoutDefinition.all.first(where: {
               let target = $0.primaryLang.lowercased()
               return primary == target || primary.hasPrefix(target + "-")
           }) {
            if currentKlid != byLang.klid {
                currentKlid = byLang.klid
            }
            SettingsService.shared.syncSelectedLayoutFromSystem(byLang.klid)
        }
    }

    /// Activa en macOS la disposición de teclado correspondiente al KLID elegido.
    func selectLayout(klid: String) {
        guard let def = AppleLayoutDefinition.all.first(where: { $0.klid == klid }) else { return }
        isSwitching = true

        if let target = findMatchingInputSource(for: def) {
            TISEnableInputSource(target)

            // Re-obtener la referencia ya habilitada para asegurar que TISSelectInputSource surta efecto inmediato
            let enabledTarget = findExactInputSource(byID: stringProperty(target, kTISPropertyInputSourceID) ?? "", includeAllInstalled: false) ?? target
            let status = TISSelectInputSource(enabledTarget)

            currentKlid = klid
            if let name = stringProperty(enabledTarget, kTISPropertyLocalizedName), !name.isEmpty {
                currentLayoutName = name
            } else if status == noErr {
                currentLayoutName = def.name
            }
        }

        Task { @MainActor [weak self] in
            try? await Task.sleep(nanoseconds: 450_000_000)
            guard let self = self else { return }
            self.isSwitching = false
            self.refreshCurrentInputSource()
        }
    }

    private func findExactInputSource(byID sourceID: String, includeAllInstalled: Bool) -> TISInputSource? {
        guard !sourceID.isEmpty else { return nil }
        let filter: [CFString: Any] = [
            kTISPropertyInputSourceCategory: kTISCategoryKeyboardInputSource as Any,
            kTISPropertyInputSourceType: kTISTypeKeyboardLayout as Any,
            kTISPropertyInputSourceID: sourceID as Any
        ]
        guard let cfList = TISCreateInputSourceList(filter as CFDictionary, includeAllInstalled)?.takeRetainedValue() as? [TISInputSource] else {
            return nil
        }
        return cfList.first
    }

    private func findMatchingInputSource(for def: AppleLayoutDefinition) -> TISInputSource? {
        // 1. Buscar primero entre los layouts ya habilitados por coincidencia exacta de ID
        for candidateID in def.macInputSourceIDs {
            if let match = findExactInputSource(byID: candidateID, includeAllInstalled: false) {
                return match
            }
        }

        // 2. Buscar en todos los layouts instalados del sistema por coincidencia exacta de ID
        for candidateID in def.macInputSourceIDs {
            if let match = findExactInputSource(byID: candidateID, includeAllInstalled: true) {
                return match
            }
        }

        // 3. Respaldo: buscar en todos los instalados cuyo primer idioma coincida con primaryLang
        let filter: [CFString: Any] = [
            kTISPropertyInputSourceCategory: kTISCategoryKeyboardInputSource as Any,
            kTISPropertyInputSourceType: kTISTypeKeyboardLayout as Any
        ]
        guard let allList = TISCreateInputSourceList(filter as CFDictionary, true)?.takeRetainedValue() as? [TISInputSource] else {
            return nil
        }

        let targetLang = def.primaryLang.lowercased()
        return allList.first(where: { src in
            guard let firstLang = languagesProperty(src).first?.lowercased() else { return false }
            return firstLang == targetLang || firstLang.hasPrefix(targetLang + "-")
        })
    }
}
