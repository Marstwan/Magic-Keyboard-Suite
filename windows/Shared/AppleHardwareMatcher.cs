#nullable enable
using System;
using System.Text.RegularExpressions;

namespace MagicKeyboardSuite.Shared
{
    /// <summary>
    /// Utilidades compartidas y deterministas para identificar hardware de Apple (USB/HID/Bluetooth),
    /// normalizar direcciones MAC y sanitizar nombres de dispositivos.
    /// </summary>
    public static class AppleHardwareMatcher
    {
        public const uint AppleUsbVendorId = 0x05AC;
        public const uint AppleBtVendorId = 0x004C;
        public const string AgentMutexName = @"Global\MagicKeyboardSuite_Agent_Hook_v3";
        public const string ReloadSettingsMessageName = "MagicKeyboardSuite_ReloadSettings";

        private static readonly Regex AppleVidInPathRegex = new Regex(
            @"(?:^|[\\#&_])VID(?:_|&[0-9A-Fa-f]{4})(?:05AC|004C)(?=$|[^0-9A-Fa-f])",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex WhitespaceRegex = new Regex(@"\s+", RegexOptions.Compiled);
        private static readonly Regex NonHexRegex = new Regex(@"[^0-9A-Fa-f]", RegexOptions.Compiled);

        private static readonly string[] ExcludedAccessories =
        {
            "iphone", "ipad", "ipod", "watch", "airpods", "buds", "pixel buds",
            "beats", "homepod", "apple tv", "mouse", "ratón", "raton", "trackpad",
            "headphone", "auriculares", "speaker", "altavoz", "pencil", "airtag"
        };

        private static readonly string[] KeyboardKeywords =
        {
            "keyboard", "teclado", "teclat", "clavier", "tastatur", "magic keyboard",
            "keychron", "k380", "mx keys", "nuphy", "hhkb"
        };

        private static readonly string[] ThirdPartyKeywords =
        {
            "logi", "logitech", "keychron", "razer", "corsair", "steelseries",
            "redragon", "asus", "dell", "hp", "lenovo", "microsoft", "nuphy",
            "hhkb", "anne pro", "epomaker", "akko", "royal kludge", "rk61",
            "satechi", "anker", "xiaomi", "trust", "rapoo"
        };

        public static bool IsAppleVendorId(uint vendorId)
        {
            return vendorId == AppleUsbVendorId || vendorId == AppleBtVendorId;
        }

        /// <summary>
        /// Comprueba si una ruta de dispositivo HID/USB/Bluetooth pertenece explícitamente al Vendor ID de Apple
        /// sin caer en falsos positivos por subcadenas hexadecimales dentro de GUIDs.
        /// </summary>
        public static bool IsAppleDevicePath(string? devicePath)
        {
            if (string.IsNullOrWhiteSpace(devicePath)) return false;
            return AppleVidInPathRegex.IsMatch(devicePath);
        }

        public static bool IsNonKeyboardAccessory(string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            string lower = name!.ToLowerInvariant();
            for (int i = 0; i < ExcludedAccessories.Length; i++)
            {
                if (lower.Contains(ExcludedAccessories[i])) return true;
            }
            return false;
        }

        public static bool IsKeyboardDevice(string? name, string? deviceId = null)
        {
            if (IsNonKeyboardAccessory(name)) return false;

            if (!string.IsNullOrWhiteSpace(name))
            {
                string lower = name!.ToLowerInvariant();
                for (int i = 0; i < KeyboardKeywords.Length; i++)
                {
                    if (lower.Contains(KeyboardKeywords[i])) return true;
                }
            }

            if (!string.IsNullOrWhiteSpace(deviceId) &&
                deviceId!.IndexOf("HID", StringComparison.OrdinalIgnoreCase) >= 0 &&
                IsAppleDevicePath(deviceId))
            {
                return true;
            }

            return false;
        }

        public static bool IsAppleKeyboardDevice(string? name, string? deviceId, bool isKeyboard)
        {
            if (!isKeyboard) return false;
            string lower = (name ?? "").ToLowerInvariant();
            for (int i = 0; i < ThirdPartyKeywords.Length; i++)
            {
                if (lower.Contains(ThirdPartyKeywords[i])) return false;
            }

            if (IsAppleDevicePath(deviceId)) return true;

            return lower.Contains("magic keyboard") || lower.Contains("apple");
        }

        public static string NormalizeMac(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "";
            string hex = NonHexRegex.Replace(raw!, "").ToUpperInvariant();
            if (hex.Length != 12 || hex == "000000000000") return "";

            var parts = new string[6];
            for (int i = 0; i < 6; i++)
            {
                parts[i] = hex.Substring(i * 2, 2);
            }
            return string.Join("-", parts);
        }

        public static string NormalizeDeviceName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "";
            return WhitespaceRegex.Replace(name!.Trim(), " ").ToUpperInvariant();
        }

        public static string NormalizeContainerId(string? containerId)
        {
            if (string.IsNullOrWhiteSpace(containerId)) return "";
            string trimmed = containerId!.Trim().Trim('{', '}').ToUpperInvariant();
            if (trimmed.Length == 0 || trimmed == "00000000-0000-0000-0000-000000000000")
            {
                return "";
            }
            return trimmed;
        }

        public static string SanitizeTsvField(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "";
            string cleaned = value!
                .Replace('\t', ' ')
                .Replace('\r', ' ')
                .Replace('\n', ' ');
            return WhitespaceRegex.Replace(cleaned.Trim(), " ");
        }

        public static string MatchAppleLayoutKlidForCulture(string? cultureName)
        {
            string target = cultureName ?? "";
            // 1. Comprobar primero variantes regionales específicas (ej. de-CH, fr-CH, nl-BE, en-GB)
            foreach (var lay in AppleLayoutDefinition.All)
            {
                if (lay.LangRegex.Contains("-") && Regex.IsMatch(target, lay.LangRegex, RegexOptions.IgnoreCase))
                {
                    return lay.Klid;
                }
            }

            // 2. Comprobar prefijos generales de idioma (ej. es, ca, fr, de, it)
            foreach (var lay in AppleLayoutDefinition.All)
            {
                if (!lay.LangRegex.Contains("-") && Regex.IsMatch(target, lay.LangRegex, RegexOptions.IgnoreCase))
                {
                    return lay.Klid;
                }
            }

            return AppleLayoutDefinition.All[0].Klid;
        }
    }
}
