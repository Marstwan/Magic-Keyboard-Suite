using System;
using System.Collections.Generic;
using MagicKeyboardSuite.Shared;

namespace MagicKeyboardSuite.WinUI3.Models
{
    public enum DeviceFilterMode
    {
        Apple = 0,
        Keyboards = 1,
        All = 2
    }

    public static class DeviceFilterModeExtensions
    {
        public static string ToSettingValue(this DeviceFilterMode mode) => mode switch
        {
            DeviceFilterMode.Apple => "apple",
            DeviceFilterMode.Keyboards => "keyboards",
            DeviceFilterMode.All => "all",
            _ => "apple"
        };

        public static DeviceFilterMode FromSettingValue(string? raw) => (raw ?? "").ToLowerInvariant() switch
        {
            "keyboards" => DeviceFilterMode.Keyboards,
            "all" => DeviceFilterMode.All,
            _ => DeviceFilterMode.Apple
        };
    }

    public enum KeyboardConnectionState
    {
        Connected = 0,
        PairedDisconnected = 1,
        ReleasedToPeer = 2,
        NearbyVisible = 3
    }

    public sealed class BluetoothKeyboardItem
    {
        public string Name { get; init; } = "";
        public string MacAddress { get; init; } = "";
        public string DeviceId { get; init; } = "";
        public KeyboardConnectionState State { get; init; }
        public bool IsKeyboardClass { get; init; }
        public bool IsAppleKeyboard { get; init; }
        public int? BatteryPercent { get; init; }

        public string StateLabel => State switch
        {
            KeyboardConnectionState.Connected => L10n.StateConnected,
            KeyboardConnectionState.PairedDisconnected => L10n.StatePaired,
            KeyboardConnectionState.ReleasedToPeer => L10n.StateReleased,
            KeyboardConnectionState.NearbyVisible => L10n.StateNearby,
            _ => L10n.StatePaired
        };
    }

    public sealed record AppleLayoutDefinition(string Klid, string Name, string LangRegex)
    {
        public static readonly IReadOnlyList<AppleLayoutDefinition> All = new List<AppleLayoutDefinition>
        {
            new("a000040a", "Español / Català (Apple)", "^(es|ca)"),
            new("a0020409", "United States-International (Apple)", "^en-INTL"),
            new("a0000409", "United States (Apple)", "^en-US"),
            new("a0000809", "United Kingdom (Apple)", "^en-GB"),
            new("a000040c", "Français (Apple)", "^fr"),
            new("a0000407", "Deutsch (Apple)", "^de"),
            new("a0000410", "Italiano (Apple)", "^it"),
            new("a0000416", "Português (Apple)", "^pt"),
            new("a0000413", "Nederlands (Apple)", "^nl"),
            new("a000041d", "Svenska (Apple)", "^sv"),
            new("a0000406", "Dansk (Apple)", "^da"),
            new("a0000414", "Norsk (Apple)", "^(nb|nn|no)"),
            new("a000040b", "Suomi (Apple)", "^fi"),
            new("a000100c", "Swiss (Apple)", "^(de|fr|it)-CH"),
            new("a0000813", "Belgian (Apple)", "^(nl|fr)-BE"),
            new("a0000415", "Polski (Apple)", "^pl"),
            new("a0000419", "Russian (Apple)", "^ru")
        };
    }
}
