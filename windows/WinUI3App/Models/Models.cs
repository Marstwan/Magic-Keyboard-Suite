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
}
