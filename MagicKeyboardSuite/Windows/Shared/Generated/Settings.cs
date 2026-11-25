// Archivo generado por shared/tools/generate.py. NO EDITAR: cambia shared/ y vuelve a generar.
using System.Collections.Generic;

namespace MagicKeyboardSuite.Shared
{
    /// <summary>Claves compartidas con macOS (UserDefaults). Se guardan en HKCU\Software\MagicKeyboardSuite.</summary>
    public static class SettingsKeys
    {
        public const string RegistryPath = @"Software\MagicKeyboardSuite";
        public const string DeviceFilter = "DeviceFilter";
        public const string RememberReleased = "RememberReleased";
        public const string ModifierMode = "ModifierMode";
        public const string SwapIsoKeys = "SwapIsoKeys";
        public const string LeftOptionAsAltGr = "LeftOptionAsAltGr";
        public const string OnlyAppleHardware = "OnlyAppleHardware";
        public const string MacNavigationShortcuts = "MacNavigationShortcuts";
        public const string MediaKeysDefault = "MediaKeysDefault";
        public const string RightOptionAsFn = "RightOptionAsFn";
        public const string PreferredKlid = "PreferredKlid";
        public const string LayoutAutoConfigured = "LayoutAutoConfigured";
        public const string LaunchAtLogin = "LaunchAtLogin";
        public const string HasSeenOnboarding = "HasSeenOnboarding";
        public const string AppLanguage = "AppLanguage";

        /// <summary>Valores por defecto (bool se guarda como DWORD 0/1).</summary>
        public static readonly Dictionary<string, object> Defaults = new Dictionary<string, object>
        {
            [DeviceFilter] = "apple",
            [RememberReleased] = 1,
            [ModifierMode] = 0,
            [SwapIsoKeys] = 1,
            [LeftOptionAsAltGr] = 1,
            [OnlyAppleHardware] = 1,
            [MacNavigationShortcuts] = 1,
            [MediaKeysDefault] = 1,
            [RightOptionAsFn] = 0,
            [PreferredKlid] = "",
            [LayoutAutoConfigured] = 0,
            [LaunchAtLogin] = 1,
            [HasSeenOnboarding] = 0,
            [AppLanguage] = "auto",
        };
    }
}
