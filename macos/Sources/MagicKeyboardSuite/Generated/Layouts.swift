// Archivo generado por shared/tools/generate.py. NO EDITAR: cambia shared/ y vuelve a generar.
import Foundation

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
        ),
    ]
}
