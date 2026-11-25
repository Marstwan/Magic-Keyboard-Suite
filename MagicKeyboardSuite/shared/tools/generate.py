#!/usr/bin/env python3
"""
Generador de recursos compartidos de Magic Keyboard Suite.

Lee:
  VERSION
  shared/design/tokens.json
  shared/strings/<lang>.json
  shared/settings.json

Escribe (NO editar a mano):
  macOS/Sources/MagicKeyboardSuite/Generated/{Theme,L10n,Settings}.swift
  Windows/WinUI3App/Generated/Theme.xaml
  Windows/Shared/Generated/{L10n,Settings}.cs    (lo usan la app WinUI 3 y el agente)

Uso:  python3 shared/tools/generate.py           (genera)
      python3 shared/tools/generate.py --check   (falla si algo está desactualizado; para CI)
"""
import json, re, sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SHARED = ROOT / "shared"
LANGS = ["es", "ca", "en"]    # el primero es el idioma por defecto
PLATFORMS = ["mac", "win"]

HEADER = "Archivo generado por shared/tools/generate.py. NO EDITAR: cambia shared/ y vuelve a generar."


def load(p):
    return json.loads(p.read_text(encoding="utf-8"))


def strip_meta(d):
    return {k: v for k, v in d.items() if not k.startswith("$")}


# ---------------------------------------------------------------- strings
def load_strings():
    data = {l: strip_meta(load(SHARED / "strings" / f"{l}.json")) for l in LANGS}
    base = set(data[LANGS[0]])
    for l in LANGS[1:]:
        missing, extra = base - set(data[l]), set(data[l]) - base
        if missing or extra:
            sys.exit(f"[strings] {l}.json no coincide con {LANGS[0]}.json. Faltan: {sorted(missing)} Sobran: {sorted(extra)}")
    return data


def resolve(value, platform):
    if isinstance(value, dict):
        return value[platform]
    return value


def arity(text):
    idx = [int(m) for m in re.findall(r"\{(\d+)\}", text)]
    return max(idx) + 1 if idx else 0


def ident(key, pascal):
    parts = re.split(r"[._\-]", key)
    out = "".join(p[:1].upper() + p[1:] for p in parts if p)
    return out if pascal else out[:1].lower() + out[1:]


def swift_str(s):
    return '"' + s.replace("\\", "\\\\").replace('"', '\\"').replace("\n", "\\n") + '"'


def cs_str(s):
    return '"' + s.replace("\\", "\\\\").replace('"', '\\"').replace("\n", "\\n") + '"'


# ---------------------------------------------------------------- colors
def hex_rgba(h):
    h = h.lstrip("#")
    if len(h) == 6:
        h += "FF"
    r, g, b, a = (int(h[i:i + 2], 16) for i in (0, 2, 4, 6))
    return r, g, b, a


def xaml_color(h):
    r, g, b, a = hex_rgba(h)
    return f"#{a:02X}{r:02X}{g:02X}{b:02X}"


SWIFT_WEIGHT = {"regular": ".regular", "medium": ".medium", "semibold": ".semibold", "bold": ".bold"}
XAML_WEIGHT = {"regular": "Normal", "medium": "Medium", "semibold": "SemiBold", "bold": "Bold"}


def num(v):
    return str(int(v)) if float(v).is_integer() else str(v)


# ---------------------------------------------------------------- Swift
def gen_swift_theme(t):
    L = [f"// {HEADER}", "import SwiftUI", "import AppKit", "", "enum Theme {"]
    L.append("    enum Colors {")
    for k, v in t["colors"].items():
        if isinstance(v, dict):
            lr, lg, lb, la = hex_rgba(v["light"]); dr, dg, db, da = hex_rgba(v["dark"])
            L.append(f"        static let {k} = Color(light: NSColor(srgbRed: {lr/255:.4f}, green: {lg/255:.4f}, blue: {lb/255:.4f}, alpha: {la/255:.4f}),"
                     f" dark: NSColor(srgbRed: {dr/255:.4f}, green: {dg/255:.4f}, blue: {db/255:.4f}, alpha: {da/255:.4f}))")
        else:
            L.append(f"        static let {k}: Double = {v}")
    L.append("    }")
    for group, name in [("radius", "Radius"), ("spacing", "Spacing"), ("size", "Size")]:
        L.append(f"    enum {name} {{")
        for k, v in t[group].items():
            L.append(f"        static let {k}: CGFloat = {num(v)}")
        L.append("    }")
    L.append("    enum Typography {")
    for k, v in t["typography"].items():
        L.append(f"        static let {k} = Font.system(size: {num(v['size'])}, weight: {SWIFT_WEIGHT[v['weight']]})")
    L.append("    }")
    L.append("    /// SF Symbols (mismo concepto que los glifos Fluent de Windows).")
    L.append("    enum Icons {")
    for k, v in strip_meta(t["icons"]).items():
        L.append(f"        static let {k} = {swift_str(v['sf'])}")
    L.append("    }")
    L.append("}")
    L.append("")
    L.append("extension Color {")
    L.append("    /// Color que cambia automáticamente entre modo claro y oscuro.")
    L.append("    init(light: NSColor, dark: NSColor) {")
    L.append("        self.init(nsColor: NSColor(name: nil) { appearance in")
    L.append("            appearance.bestMatch(from: [.darkAqua, .vibrantDark, .accessibilityHighContrastDarkAqua]) != nil ? dark : light")
    L.append("        })")
    L.append("    }")
    L.append("}")
    return "\n".join(L) + "\n"


def gen_swift_l10n(strings, version):
    P = "mac"
    L = [f"// {HEADER}", "import Foundation", "", "enum L10n {"]
    L.append(f"    static let supported = {swift_str(','.join(LANGS))}.split(separator: \",\").map(String.init)")
    L.append("    /// Idioma forzado por el usuario (\"es\", \"ca\", \"en\"), o nil para usar el idioma del sistema.")
    L.append("    static var overrideLanguage: String? = nil")
    L.append("    /// Idioma detectado del sistema.")
    L.append("    static let systemLanguage: String = {")
    L.append("        for pref in Locale.preferredLanguages {")
    L.append("            let code = String(pref.prefix(2)).lowercased()")
    L.append("            if supported.contains(code) { return code }")
    L.append("        }")
    L.append(f"        return {swift_str(LANGS[0])}")
    L.append("    }()")
    L.append("    /// Idioma activo.")
    L.append("    static var language: String {")
    L.append("        if let over = overrideLanguage, supported.contains(over) { return over }")
    L.append("        return systemLanguage")
    L.append("    }")
    L.append("")
    L.append("    private static let tables: [String: [String: String]] = [")
    for l in LANGS:
        L.append(f"        {swift_str(l)}: [")
        for k, v in strings[l].items():
            L.append(f"            {swift_str(k)}: {swift_str(resolve(v, P))},")
        L.append("        ],")
    L.append("    ]")
    L.append("")
    L.append("    static func t(_ key: String, _ args: CVarArg...) -> String {")
    L.append(f"        var s = tables[language]?[key] ?? tables[{swift_str(LANGS[0])}]?[key] ?? key")
    L.append("        for (i, a) in args.enumerated() { s = s.replacingOccurrences(of: \"{\\(i)}\", with: \"\\(a)\") }")
    L.append("        return s")
    L.append("    }")
    L.append("")
    for k, v in strings[LANGS[0]].items():
        n = arity(resolve(v, P))
        name = ident(k, False)
        if n == 0:
            L.append(f"    static var {name}: String {{ t({swift_str(k)}) }}")
        else:
            params = ", ".join(f"_ a{i}: CVarArg" for i in range(n))
            args = ", ".join(f"a{i}" for i in range(n))
            L.append(f"    static func {name}({params}) -> String {{ t({swift_str(k)}, {args}) }}")
    L.append("}")
    L.append("")
    L.append("enum AppInfo {")
    L.append(f"    static let version = {swift_str(version)}")
    L.append("}")
    return "\n".join(L) + "\n"


def gen_swift_settings(settings):
    L = [f"// {HEADER}", "import Foundation", "", "/// Claves compartidas con Windows (HKCU\\\\Software\\\\MagicKeyboardSuite).", "enum SettingsKey {"]
    defaults = []
    for k, v in settings.items():
        if "mac" not in v["platforms"]:
            continue
        L.append(f"    static let {ident(k, False)} = {swift_str(k)}")
        d = v["default"]
        lit = ("true" if d else "false") if v["type"] == "bool" else (swift_str(d) if v["type"] == "string" else str(d))
        defaults.append(f"        {swift_str(k)}: {lit},")
    L.append("")
    L.append("    static let defaults: [String: Any] = [")
    L += defaults
    L.append("    ]")
    L.append("}")
    return "\n".join(L) + "\n"


# ---------------------------------------------------------------- Windows
def gen_xaml_theme(t):
    L = ['<?xml version="1.0" encoding="utf-8"?>', f"<!-- {HEADER} -->",
         '<ResourceDictionary',
         '    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"',
         '    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">', "",
         "    <ResourceDictionary.ThemeDictionaries>"]
    colors = {k: v for k, v in t["colors"].items() if isinstance(v, dict)}
    for theme, key in [("Light", "light"), ("Dark", "dark"), ("HighContrast", "dark")]:
        L.append(f'        <ResourceDictionary x:Key="{theme}">')
        for k, v in colors.items():
            name = ident(k, True)
            L.append(f'            <Color x:Key="MKS{name}Color">{xaml_color(v[key])}</Color>')
            L.append(f'            <SolidColorBrush x:Key="MKS{name}Brush" Color="{xaml_color(v[key])}" />')
            r, g, b, _ = hex_rgba(v[key]); a = round(255 * t["colors"]["badgeFillOpacity"])
            if k.startswith("state"):
                L.append(f'            <SolidColorBrush x:Key="MKS{name}FillBrush" Color="#{a:02X}{r:02X}{g:02X}{b:02X}" />')
        L.append("        </ResourceDictionary>")
    L.append("    </ResourceDictionary.ThemeDictionaries>")
    L.append("")
    for k, v in t["radius"].items():
        L.append(f'    <CornerRadius x:Key="MKS{ident(k, True)}CornerRadius">{num(v)}</CornerRadius>')
    for k, v in t["spacing"].items():
        L.append(f'    <x:Double x:Key="MKSSpacing{ident(k, True)}">{num(v)}</x:Double>')
    s = t["spacing"]
    L.append(f'    <Thickness x:Key="MKSCardPadding">{num(s["cardPaddingH"])},{num(s["cardPaddingV"])},{num(s["cardPaddingH"])},{num(s["cardPaddingV"])}</Thickness>')
    L.append(f'    <Thickness x:Key="MKSPagePadding">{num(s["pagePaddingH"])},{num(s["pagePaddingV"])},{num(s["pagePaddingH"])},{num(s["pagePaddingV"])}</Thickness>')
    for k, v in t["size"].items():
        L.append(f'    <x:Double x:Key="MKSSize{ident(k, True)}">{num(v)}</x:Double>')
    L.append("")
    for k, v in t["typography"].items():
        L.append(f'    <Style x:Key="MKS{ident(k, True)}TextStyle" TargetType="TextBlock">')
        L.append(f'        <Setter Property="FontSize" Value="{num(v["size"])}" />')
        L.append(f'        <Setter Property="FontWeight" Value="{XAML_WEIGHT[v["weight"]]}" />')
        L.append('        <Setter Property="TextWrapping" Value="Wrap" />')
        L.append("    </Style>")
    L.append("")
    L.append("    <!-- Glifos Segoe Fluent Icons (mismo concepto que los SF Symbols de macOS) -->")
    for k, v in strip_meta(t["icons"]).items():
        L.append(f'    <x:String x:Key="MKSIcon{ident(k, True)}">&#x{v["fluent"]};</x:String>')
    L.append("</ResourceDictionary>")
    return "\n".join(L) + "\n"


def gen_cs_l10n(strings, version):
    P = "win"
    L = [f"// {HEADER}", "#pragma warning disable CS8600, CS8604", "using System;", "using System.Collections.Generic;", "using System.Globalization;", "",
         "namespace MagicKeyboardSuite.Shared", "{", "    public static class L10n", "    {"]
    L.append(f'        private static readonly string[] Supported = {{ {", ".join(cs_str(l) for l in LANGS)} }};')
    L.append("        public static string? OverrideLanguage { get; set; }")
    L.append("        public static readonly string SystemLanguage = DetectLanguage();")
    L.append("        public static string Language => (!string.IsNullOrEmpty(OverrideLanguage) && Array.IndexOf(Supported, OverrideLanguage!) >= 0) ? OverrideLanguage! : SystemLanguage;")
    L.append("")
    L.append("        private static string DetectLanguage()")
    L.append("        {")
    L.append("            string code = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToLowerInvariant();")
    L.append("            return Array.IndexOf(Supported, code) >= 0 ? code : Supported[0];")
    L.append("        }")
    L.append("")
    L.append("        private static readonly Dictionary<string, Dictionary<string, string>> Tables = new Dictionary<string, Dictionary<string, string>>")
    L.append("        {")
    for l in LANGS:
        L.append(f"            [{cs_str(l)}] = new Dictionary<string, string>")
        L.append("            {")
        for k, v in strings[l].items():
            L.append(f"                [{cs_str(k)}] = {cs_str(resolve(v, P))},")
        L.append("            },")
    L.append("        };")
    L.append("")
    L.append("        public static string T(string key, params object[] args)")
    L.append("        {")
    L.append("            string s;")
    L.append(f"            if (!Tables[Language].TryGetValue(key, out s!) && !Tables[{cs_str(LANGS[0])}].TryGetValue(key, out s!)) s = key;")
    L.append("            for (int i = 0; i < args.Length; i++) s = s.Replace(\"{\" + i + \"}\", Convert.ToString(args[i], CultureInfo.CurrentCulture) ?? \"\");")
    L.append("            return s;")
    L.append("        }")
    L.append("")
    for k, v in strings[LANGS[0]].items():
        n = arity(resolve(v, P))
        name = ident(k, True)
        if n == 0:
            L.append(f"        public static string {name} => T({cs_str(k)});")
        else:
            params = ", ".join(f"object a{i}" for i in range(n))
            args = ", ".join(f"a{i}" for i in range(n))
            L.append(f"        public static string {name}({params}) => T({cs_str(k)}, {args});")
    L.append("    }")
    L.append("")
    L.append("    public static class AppInfo")
    L.append("    {")
    L.append(f"        public const string Version = {cs_str(version)};")
    L.append("    }")
    L.append("}")
    return "\n".join(L) + "\n"


def gen_cs_settings(settings):
    L = [f"// {HEADER}", "using System.Collections.Generic;", "", "namespace MagicKeyboardSuite.Shared", "{",
         "    /// <summary>Claves compartidas con macOS (UserDefaults). Se guardan en HKCU\\Software\\MagicKeyboardSuite.</summary>",
         "    public static class SettingsKeys", "    {",
         '        public const string RegistryPath = @"Software\\MagicKeyboardSuite";']
    defaults = []
    for k, v in settings.items():
        if "win" not in v["platforms"]:
            continue
        L.append(f"        public const string {k} = {cs_str(k)};")
        d = v["default"]
        lit = ("1" if d else "0") if v["type"] == "bool" else (cs_str(d) if v["type"] == "string" else str(d))
        defaults.append(f"            [{k}] = {lit},")
    L.append("")
    L.append("        /// <summary>Valores por defecto (bool se guarda como DWORD 0/1).</summary>")
    L.append("        public static readonly Dictionary<string, object> Defaults = new Dictionary<string, object>")
    L.append("        {")
    L += defaults
    L.append("        };")
    L.append("    }")
    L.append("}")
    return "\n".join(L) + "\n"


# ---------------------------------------------------------------- main
def main():
    check = "--check" in sys.argv
    version = (ROOT / "VERSION").read_text().strip()
    tokens = load(SHARED / "design" / "tokens.json")
    strings = load_strings()
    settings = load(SHARED / "settings.json")["settings"]

    mac_gen = ROOT / "macOS" / "Sources" / "MagicKeyboardSuite" / "Generated"
    win_app_gen = ROOT / "Windows" / "WinUI3App" / "Generated"
    win_shared_gen = ROOT / "Windows" / "Shared" / "Generated"

    outputs = {
        mac_gen / "Theme.swift": gen_swift_theme(tokens),
        mac_gen / "L10n.swift": gen_swift_l10n(strings, version),
        mac_gen / "Settings.swift": gen_swift_settings(settings),
        win_app_gen / "Theme.xaml": gen_xaml_theme(tokens),
        win_shared_gen / "L10n.cs": gen_cs_l10n(strings, version),
        win_shared_gen / "Settings.cs": gen_cs_settings(settings),
    }

    stale = []
    for path, content in outputs.items():
        current = path.read_text(encoding="utf-8") if path.exists() else None
        if current != content:
            stale.append(path)
            if not check:
                path.parent.mkdir(parents=True, exist_ok=True)
                path.write_text(content, encoding="utf-8")
    rel = [str(p.relative_to(ROOT)) for p in stale]
    if check:
        if stale:
            sys.exit("Recursos generados desactualizados (ejecuta python3 shared/tools/generate.py):\n  " + "\n  ".join(rel))
        print("Recursos generados al día.")
    else:
        print("Generados:" if rel else "Sin cambios.", *rel, sep="\n  ")


if __name__ == "__main__":
    main()
