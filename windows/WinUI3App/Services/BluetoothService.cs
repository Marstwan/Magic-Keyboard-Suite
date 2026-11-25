using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MagicKeyboardSuite.Shared;
using MagicKeyboardSuite.WinUI3.Models;
using Microsoft.Win32;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Storage.Streams;

namespace MagicKeyboardSuite.WinUI3.Services
{
    /// <summary>
    /// Servicio Bluetooth nativo WinRT con caché en memoria, deduplicación por MAC/ContainerId (BR/EDR + BLE)
    /// y triple verificación del estado conectado (AEP + BluetoothConnectionStatus + RawInput HID).
    /// </summary>
    public sealed class BluetoothService : IDisposable
    {
        private const string BtClassicProtocolId = "{e0cbf06c-cd8b-4647-bb8a-263b43f0f974}";
        private const string BtLeProtocolId = "{bb7bb05e-5972-42b5-94fc-76eaa7084d49}";

        private const string AepBluetoothSelector =
            "(System.Devices.Aep.ProtocolId:=\"" + BtClassicProtocolId + "\" OR " +
            "System.Devices.Aep.ProtocolId:=\"" + BtLeProtocolId + "\")";

        private static readonly string[] RequestedProperties =
        {
            "System.Devices.Aep.DeviceAddress",
            "System.Devices.Aep.IsConnected",
            "System.Devices.Aep.IsPaired",
            "System.Devices.Aep.IsPresent",
            "System.Devices.Aep.ProtocolId",
            "System.Devices.Aep.ContainerId",
            "{104ea319-6ee2-4701-bd47-8ddbF425bbe5} 2" // DEVPKEY_Bluetooth_Battery (0..100)
        };

        private const string BatteryPropertyKey = "{104ea319-6ee2-4701-bd47-8ddbF425bbe5} 2";

        private sealed class CachedAepEndpoint
        {
            public string Id { get; set; } = "";
            public string Name { get; set; } = "";
            public string MacAddress { get; set; } = "";
            public string ContainerId { get; set; } = "";
            public bool IsClassic { get; set; }
            public bool IsPaired { get; set; }
            public bool IsConnected { get; set; }
            public int? BatteryPercent { get; set; }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RAWINPUTDEVICELIST
        {
            public IntPtr hDevice;
            public uint dwType;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetRawInputDeviceList([In, Out] RAWINPUTDEVICELIST[]? ridList, ref uint numDevices, uint cbSize);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern uint GetRawInputDeviceInfo(IntPtr hDevice, uint uiCommand, StringBuilder? pData, ref uint pcbSize);

        private static readonly Regex MacInIdRegex = new(@"([0-9A-Fa-f]{2}(?:[:\-][0-9A-Fa-f]{2}){5})", RegexOptions.Compiled);

        private readonly SettingsService _settings;
        private readonly ConcurrentDictionary<string, CachedAepEndpoint> _cache = new(StringComparer.OrdinalIgnoreCase);
        private readonly object _debounceLock = new();
        private DeviceWatcher? _watcher;
        private Timer? _debounceTimer;
        private volatile bool _initialEnumerationDone;
        private volatile bool _disposed;

        public event Action? DevicesChanged;
        public event Action<string, string, bool>? StatusUpdated;

        private string KnownKeyboardsPath
        {
            get
            {
                string dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "MagicKeyboardSuite");
                Directory.CreateDirectory(dir);
                return Path.Combine(dir, "known_keyboards.tsv");
            }
        }

        public BluetoothService(SettingsService settings)
        {
            _settings = settings;
            StartWatcher();
        }

        private void StartWatcher()
        {
            try
            {
                _watcher = DeviceInformation.CreateWatcher(
                    AepBluetoothSelector,
                    RequestedProperties,
                    DeviceInformationKind.AssociationEndpoint);

                _watcher.Added += OnWatcherAdded;
                _watcher.Updated += OnWatcherUpdated;
                _watcher.Removed += OnWatcherRemoved;
                _watcher.EnumerationCompleted += OnWatcherEnumerationCompleted;
                _watcher.Start();
            }
            catch { }
        }

        private void ScheduleDevicesChanged()
        {
            if (!_initialEnumerationDone || _disposed) return;
            lock (_debounceLock)
            {
                if (_disposed) return;
                if (_debounceTimer == null)
                {
                    _debounceTimer = new Timer(_ =>
                    {
                        if (!_disposed) DevicesChanged?.Invoke();
                    }, null, 120, Timeout.Infinite);
                }
                else
                {
                    _debounceTimer.Change(120, Timeout.Infinite);
                }
            }
        }

        private void OnWatcherAdded(DeviceWatcher sender, DeviceInformation dev)
        {
            UpsertFromDeviceInformation(dev);
            ScheduleDevicesChanged();
        }

        private void OnWatcherUpdated(DeviceWatcher sender, DeviceInformationUpdate update)
        {
            if (_cache.TryGetValue(update.Id, out var existing))
            {
                if (update.Properties.TryGetValue("System.ItemNameDisplay", out var nameObj))
                {
                    string n = Convert.ToString(nameObj) ?? "";
                    if (!string.IsNullOrWhiteSpace(n)) existing.Name = n;
                }
                if (update.Properties.TryGetValue("System.Devices.Aep.IsConnected", out var cObj) && cObj is bool bC)
                {
                    existing.IsConnected = bC;
                }
                if (update.Properties.TryGetValue("System.Devices.Aep.IsPaired", out var pObj) && pObj is bool bP)
                {
                    existing.IsPaired = bP;
                }
                if (update.Properties.TryGetValue(BatteryPropertyKey, out var batObj) && ParseBatteryValue(batObj) is int bVal)
                {
                    existing.BatteryPercent = bVal;
                }
                ScheduleDevicesChanged();
            }
        }

        private void OnWatcherRemoved(DeviceWatcher sender, DeviceInformationUpdate update)
        {
            _cache.TryRemove(update.Id, out _);
            ScheduleDevicesChanged();
        }

        private void OnWatcherEnumerationCompleted(DeviceWatcher sender, object args)
        {
            _initialEnumerationDone = true;
            DevicesChanged?.Invoke();
        }

        private void UpsertFromDeviceInformation(DeviceInformation dev)
        {
            string rawAddr = dev.Properties.TryGetValue("System.Devices.Aep.DeviceAddress", out var addrObj)
                ? Convert.ToString(addrObj) ?? ""
                : "";
            string mac = AppleHardwareMatcher.NormalizeMac(rawAddr);
            if (string.IsNullOrEmpty(mac))
            {
                var m = MacInIdRegex.Match(dev.Id);
                if (m.Success) mac = AppleHardwareMatcher.NormalizeMac(m.Groups[1].Value);
            }
            if (string.IsNullOrEmpty(mac)) return;

            bool isPaired = dev.Pairing?.IsPaired == true ||
                            (dev.Properties.TryGetValue("System.Devices.Aep.IsPaired", out var pObj) && pObj is bool bP && bP);
            bool isConnected = dev.Properties.TryGetValue("System.Devices.Aep.IsConnected", out var cObj) && cObj is bool bC && bC;
            string protocol = dev.Properties.TryGetValue("System.Devices.Aep.ProtocolId", out var prObj)
                ? Convert.ToString(prObj) ?? ""
                : "";
            string containerId = dev.Properties.TryGetValue("System.Devices.Aep.ContainerId", out var cidObj)
                ? AppleHardwareMatcher.NormalizeContainerId(Convert.ToString(cidObj))
                : "";
            int? battery = dev.Properties.TryGetValue(BatteryPropertyKey, out var batObj)
                ? ParseBatteryValue(batObj)
                : (_cache.TryGetValue(dev.Id, out var prevEp) ? prevEp.BatteryPercent : null);

            _cache[dev.Id] = new CachedAepEndpoint
            {
                Id = dev.Id,
                Name = dev.Name ?? "",
                MacAddress = mac,
                ContainerId = containerId,
                IsClassic = protocol.IndexOf(BtClassicProtocolId, StringComparison.OrdinalIgnoreCase) >= 0,
                IsPaired = isPaired,
                IsConnected = isConnected,
                BatteryPercent = battery
            };
        }

        private static int? ParseBatteryValue(object? raw)
        {
            if (raw == null) return null;
            try
            {
                int val = Convert.ToInt32(raw);
                return (val >= 1 && val <= 100) ? val : null;
            }
            catch
            {
                return null;
            }
        }

        private static async Task<int?> ProbeGattBatteryAsync(CachedAepEndpoint ep)
        {
            if (ep.BatteryPercent.HasValue) return ep.BatteryPercent;
            if (ep.IsClassic) return null;

            try
            {
                using var ble = await BluetoothLEDevice.FromIdAsync(ep.Id);
                if (ble == null) return null;

                var svcResult = await ble.GetGattServicesForUuidAsync(GattServiceUuids.Battery, BluetoothCacheMode.Cached);
                var svc = svcResult?.Services?.FirstOrDefault();
                if (svc == null) return null;

                using (svc)
                {
                    var charResult = await svc.GetCharacteristicsForUuidAsync(GattCharacteristicUuids.BatteryLevel, BluetoothCacheMode.Cached);
                    var ch = charResult?.Characteristics?.FirstOrDefault();
                    if (ch == null) return null;

                    var read = await ch.ReadValueAsync(BluetoothCacheMode.Cached);
                    if (read?.Status == GattCommunicationStatus.Success && read.Value != null && read.Value.Length > 0)
                    {
                        using var reader = DataReader.FromBuffer(read.Value);
                        byte pct = reader.ReadByte();
                        if (pct >= 1 && pct <= 100)
                        {
                            ep.BatteryPercent = pct;
                            return pct;
                        }
                    }
                }
            }
            catch { }

            return null;
        }

        private static async Task<bool> ProbeLiveConnectionAsync(CachedAepEndpoint ep)
        {
            if (ep.IsConnected) return true;
            if (!ep.IsPaired) return false;

            try
            {
                if (ep.IsClassic)
                {
                    using var bt = await BluetoothDevice.FromIdAsync(ep.Id);
                    if (bt != null && bt.ConnectionStatus == BluetoothConnectionStatus.Connected)
                    {
                        ep.IsConnected = true;
                        return true;
                    }
                }
                else
                {
                    using var ble = await BluetoothLEDevice.FromIdAsync(ep.Id);
                    if (ble != null && ble.ConnectionStatus == BluetoothConnectionStatus.Connected)
                    {
                        ep.IsConnected = true;
                        return true;
                    }
                }
            }
            catch { }

            return false;
        }

        private static (bool HasActiveAppleKeyboard, List<string> ActiveDevicePaths) GetActiveAppleHidStatus()
        {
            var paths = new List<string>();
            bool hasApple = false;

            try
            {
                uint count = 0;
                uint size = (uint)Marshal.SizeOf<RAWINPUTDEVICELIST>();
                GetRawInputDeviceList(null, ref count, size);
                if (count > 0)
                {
                    var list = new RAWINPUTDEVICELIST[count];
                    if (GetRawInputDeviceList(list, ref count, size) != uint.MaxValue)
                    {
                        foreach (var item in list)
                        {
                            if (item.dwType != 1 || item.hDevice == IntPtr.Zero) continue;
                            uint len = 0;
                            GetRawInputDeviceInfo(item.hDevice, 0x20000007, null, ref len);
                            if (len > 0)
                            {
                                var sb = new StringBuilder((int)len + 1);
                                if (GetRawInputDeviceInfo(item.hDevice, 0x20000007, sb, ref len) > 0)
                                {
                                    string path = sb.ToString().ToUpperInvariant();
                                    paths.Add(path);
                                    if (AppleHardwareMatcher.IsAppleDevicePath(path))
                                    {
                                        hasApple = true;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch { }

            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(SettingsKeys.RegistryPath, false);
                if (key != null)
                {
                    long ticks = Convert.ToInt64(key.GetValue("LastAppleKeyUtcTicks", 0L));
                    string lastPath = Convert.ToString(key.GetValue("LastAppleDevicePath", ""))?.ToUpperInvariant() ?? "";
                    if (!string.IsNullOrEmpty(lastPath) && !paths.Contains(lastPath))
                    {
                        paths.Add(lastPath);
                    }
                    if (ticks > 0 && (DateTime.UtcNow.Ticks - ticks) < TimeSpan.FromMinutes(10).Ticks &&
                        AppleHardwareMatcher.IsAppleDevicePath(lastPath))
                    {
                        hasApple = true;
                    }
                }
            }
            catch { }

            return (hasApple, paths);
        }

        public async Task<IReadOnlyList<BluetoothKeyboardItem>> GetKeyboardsAsync(bool forceRefresh = false, bool includeUnpairedNearby = false)
        {
            if (forceRefresh || _cache.IsEmpty)
            {
                try
                {
                    var devices = await DeviceInformation.FindAllAsync(
                        AepBluetoothSelector,
                        RequestedProperties,
                        DeviceInformationKind.AssociationEndpoint);
                    foreach (var dev in devices)
                    {
                        UpsertFromDeviceInformation(dev);
                    }
                    _initialEnumerationDone = true;
                }
                catch { }
            }

            var known = LoadKnownKeyboards();
            bool knownChanged = false;
            var (hasActiveAppleHid, activeHidPaths) = GetActiveAppleHidStatus();

            var candidates = new List<(BluetoothKeyboardItem Item, string ContainerId)>();
            var endpointsSnapshot = _cache.Values.ToList();

            foreach (var ep in endpointsSnapshot)
            {
                if (!ep.IsPaired && !includeUnpairedNearby) continue;

                string name = !string.IsNullOrWhiteSpace(ep.Name)
                    ? ep.Name
                    : (known.TryGetValue(ep.MacAddress, out var savedName) ? savedName : "");
                if (string.IsNullOrWhiteSpace(name)) continue;

                bool isKbd = AppleHardwareMatcher.IsKeyboardDevice(name, ep.Id);
                bool isApple = AppleHardwareMatcher.IsAppleKeyboardDevice(name, ep.Id, isKbd) || known.ContainsKey(ep.MacAddress);

                if (ep.IsPaired && isKbd && isApple)
                {
                    if (!known.TryGetValue(ep.MacAddress, out var prev) || prev != name)
                    {
                        known[ep.MacAddress] = name;
                        knownChanged = true;
                    }
                }
                else if (!isKbd && known.ContainsKey(ep.MacAddress))
                {
                    known.Remove(ep.MacAddress);
                    knownChanged = true;
                }

                if (!PassesFilter(isApple, isKbd)) continue;

                bool isConnected = ep.IsConnected;
                if (!isConnected && ep.IsPaired && isKbd)
                {
                    isConnected = await ProbeLiveConnectionAsync(ep);
                }

                if (!isConnected && ep.IsPaired && isKbd)
                {
                    string hexMac = ep.MacAddress.Replace("-", "");
                    string cleanCid = ep.ContainerId;
                    if (activeHidPaths.Any(p =>
                        (!string.IsNullOrEmpty(hexMac) && p.Contains(hexMac)) ||
                        (!string.IsNullOrEmpty(cleanCid) && p.IndexOf(cleanCid, StringComparison.OrdinalIgnoreCase) >= 0)))
                    {
                        isConnected = true;
                        ep.IsConnected = true;
                    }
                }

                var state = isConnected
                    ? KeyboardConnectionState.Connected
                    : (ep.IsPaired ? KeyboardConnectionState.PairedDisconnected : KeyboardConnectionState.NearbyVisible);

                int? battery = ep.BatteryPercent;
                if (!battery.HasValue && isConnected)
                {
                    var sibling = endpointsSnapshot.FirstOrDefault(s =>
                        s.BatteryPercent.HasValue &&
                        (string.Equals(s.MacAddress, ep.MacAddress, StringComparison.OrdinalIgnoreCase) ||
                         (!string.IsNullOrEmpty(ep.ContainerId) && string.Equals(s.ContainerId, ep.ContainerId, StringComparison.OrdinalIgnoreCase))));
                    battery = sibling?.BatteryPercent ?? await ProbeGattBatteryAsync(ep);
                }

                candidates.Add((new BluetoothKeyboardItem
                {
                    Name = name.Trim(),
                    MacAddress = ep.MacAddress,
                    DeviceId = ep.Id,
                    State = state,
                    IsKeyboardClass = isKbd,
                    IsAppleKeyboard = isApple,
                    BatteryPercent = state == KeyboardConnectionState.Connected ? battery : null
                }, ep.ContainerId));
            }

            // Deduplicar por MAC y por ContainerId (une canales BR/EDR + BLE del mismo teclado sin ocultar un segundo Magic Keyboard distinto)
            var deduplicated = new List<BluetoothKeyboardItem>();
            var seenMacs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var seenContainerIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var entry in candidates.OrderBy(c => (int)c.Item.State))
            {
                if (seenMacs.Contains(entry.Item.MacAddress)) continue;
                if (!string.IsNullOrEmpty(entry.ContainerId) && seenContainerIds.Contains(entry.ContainerId)) continue;

                seenMacs.Add(entry.Item.MacAddress);
                if (!string.IsNullOrEmpty(entry.ContainerId)) seenContainerIds.Add(entry.ContainerId);
                deduplicated.Add(entry.Item);
            }

            if (hasActiveAppleHid &&
                !deduplicated.Any(d => d.IsAppleKeyboard && d.State == KeyboardConnectionState.Connected))
            {
                int idx = deduplicated.FindIndex(d => d.IsAppleKeyboard && d.State == KeyboardConnectionState.PairedDisconnected);
                if (idx >= 0)
                {
                    var current = deduplicated[idx];
                    int? fallbackBat = endpointsSnapshot
                        .FirstOrDefault(s => s.BatteryPercent.HasValue &&
                            string.Equals(s.MacAddress, current.MacAddress, StringComparison.OrdinalIgnoreCase))
                        ?.BatteryPercent;
                    deduplicated[idx] = new BluetoothKeyboardItem
                    {
                        Name = current.Name,
                        MacAddress = current.MacAddress,
                        DeviceId = current.DeviceId,
                        State = KeyboardConnectionState.Connected,
                        IsKeyboardClass = current.IsKeyboardClass,
                        IsAppleKeyboard = current.IsAppleKeyboard,
                        BatteryPercent = current.BatteryPercent ?? fallbackBat
                    };
                }
                else if (PassesFilter(isApple: true, isKeyboard: true))
                {
                    deduplicated.Add(new BluetoothKeyboardItem
                    {
                        Name = "Magic Keyboard",
                        MacAddress = "USB",
                        DeviceId = "USB",
                        State = KeyboardConnectionState.Connected,
                        IsKeyboardClass = true,
                        IsAppleKeyboard = true
                    });
                }
            }

            if (_settings.RememberReleased && PassesFilter(isApple: true, isKeyboard: true))
            {
                foreach (var kv in known.ToList())
                {
                    if (AppleHardwareMatcher.IsNonKeyboardAccessory(kv.Value))
                    {
                        known.Remove(kv.Key);
                        knownChanged = true;
                        continue;
                    }

                    if (seenMacs.Contains(kv.Key)) continue;
                    seenMacs.Add(kv.Key);

                    deduplicated.Add(new BluetoothKeyboardItem
                    {
                        Name = kv.Value,
                        MacAddress = kv.Key,
                        DeviceId = "",
                        State = KeyboardConnectionState.ReleasedToPeer,
                        IsKeyboardClass = true,
                        IsAppleKeyboard = true
                    });
                }
            }

            if (knownChanged)
            {
                SaveKnownKeyboards(known);
            }

            return deduplicated
                .OrderBy(k => (int)k.State)
                .ThenBy(k => k.Name, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }

        public async Task<bool> ReleaseForPeerAsync(BluetoothKeyboardItem item)
        {
            StatusUpdated?.Invoke(L10n.StatusReleasingTitle(item.Name), L10n.StatusReleasingBody, false);

            var known = LoadKnownKeyboards();
            known[item.MacAddress] = item.Name;
            SaveKnownKeyboards(known);

            try
            {
                using var key = Registry.CurrentUser.CreateSubKey(SettingsKeys.RegistryPath);
                key?.SetValue("LastAppleKeyUtcTicks", 0L, RegistryValueKind.QWord);
                key?.SetValue("LastAppleDevicePath", "", RegistryValueKind.String);
            }
            catch { }

            try
            {
                string? targetContainerId = _cache.Values
                    .FirstOrDefault(e => string.Equals(e.MacAddress, item.MacAddress, StringComparison.OrdinalIgnoreCase))
                    ?.ContainerId;

                var matchingEndpoints = _cache.Values
                    .Where(e => string.Equals(e.MacAddress, item.MacAddress, StringComparison.OrdinalIgnoreCase) ||
                                (!string.IsNullOrEmpty(targetContainerId) && string.Equals(e.ContainerId, targetContainerId, StringComparison.OrdinalIgnoreCase)))
                    .ToList();

                foreach (var ep in matchingEndpoints)
                {
                    var dev = await FindDeviceAsync(ep.MacAddress, ep.Id);
                    if (dev?.Pairing != null && dev.Pairing.IsPaired)
                    {
                        await dev.Pairing.UnpairAsync();
                    }
                    ep.IsPaired = false;
                    ep.IsConnected = false;
                }
            }
            catch { }

            StatusUpdated?.Invoke(L10n.StatusReleasedTitle(item.Name), L10n.StatusReleasedBody, true);
            DevicesChanged?.Invoke();
            return true;
        }

        public async Task<bool> ConnectOrPairAsync(BluetoothKeyboardItem item)
        {
            StatusUpdated?.Invoke(L10n.StatusConnectingTitle(item.Name), L10n.StatusConnectingBody, false);

            try
            {
                var dev = await FindDeviceAsync(item.MacAddress, item.DeviceId);
                if (dev == null)
                {
                    StatusUpdated?.Invoke(L10n.StatusNotFoundTitle(item.MacAddress), L10n.StatusConnectingBody, false);
                    return false;
                }

                if (dev.Pairing.IsPaired)
                {
                    try { await dev.Pairing.UnpairAsync(); } catch { }
                    dev = await FindDeviceAsync(item.MacAddress, item.DeviceId) ?? dev;
                }

                var custom = dev.Pairing.Custom;
                TypedEventHandler<DeviceInformationCustomPairing, DevicePairingRequestedEventArgs> handler = (_, args) =>
                {
                    switch (args.PairingKind)
                    {
                        case DevicePairingKinds.ConfirmOnly:
                        case DevicePairingKinds.ConfirmPinMatch:
                            args.Accept();
                            break;
                        case DevicePairingKinds.DisplayPin:
                            StatusUpdated?.Invoke(L10n.StatusConnectingTitle(item.Name), L10n.StatusPairingPin(args.Pin), false);
                            args.Accept();
                            break;
                        case DevicePairingKinds.ProvidePin:
                            StatusUpdated?.Invoke(L10n.StatusConnectingTitle(item.Name), L10n.StatusPairingPin("000000"), false);
                            args.Accept("000000");
                            break;
                        default:
                            args.Accept();
                            break;
                    }
                };

                custom.PairingRequested += handler;
                DevicePairingResult result;
                try
                {
                    var kinds = DevicePairingKinds.ConfirmOnly |
                                DevicePairingKinds.DisplayPin |
                                DevicePairingKinds.ProvidePin |
                                DevicePairingKinds.ConfirmPinMatch;
                    result = await custom.PairAsync(kinds);
                }
                finally
                {
                    custom.PairingRequested -= handler;
                }

                if (result.Status == DevicePairingResultStatus.Paired ||
                    result.Status == DevicePairingResultStatus.AlreadyPaired)
                {
                    UpsertFromDeviceInformation(dev);
                    if (_cache.TryGetValue(dev.Id, out var ep))
                    {
                        ep.IsPaired = true;
                        ep.IsConnected = true;
                    }
                    StatusUpdated?.Invoke(L10n.StatusConnectedTitle(item.Name), L10n.StatusConnectedBody, true);
                    DevicesChanged?.Invoke();
                    return true;
                }
            }
            catch { }

            StatusUpdated?.Invoke(L10n.StatusPairingFailedTitle(item.Name), L10n.StatusPairingFailedBody, false);
            try { Process.Start(new ProcessStartInfo { FileName = "ms-settings:bluetooth", UseShellExecute = true }); } catch { }
            return false;
        }

        private async Task<DeviceInformation?> FindDeviceAsync(string mac, string deviceId)
        {
            if (!string.IsNullOrWhiteSpace(deviceId))
            {
                try
                {
                    var byId = await DeviceInformation.CreateFromIdAsync(deviceId, RequestedProperties, DeviceInformationKind.AssociationEndpoint);
                    if (byId != null) return byId;
                }
                catch { }
            }

            string targetMac = AppleHardwareMatcher.NormalizeMac(mac);
            var devices = await DeviceInformation.FindAllAsync(AepBluetoothSelector, RequestedProperties, DeviceInformationKind.AssociationEndpoint);
            foreach (var d in devices)
            {
                string addr = d.Properties.TryGetValue("System.Devices.Aep.DeviceAddress", out var a) ? Convert.ToString(a) ?? "" : "";
                if (!string.IsNullOrEmpty(targetMac) && string.Equals(AppleHardwareMatcher.NormalizeMac(addr), targetMac, StringComparison.OrdinalIgnoreCase))
                {
                    return d;
                }
            }
            return null;
        }

        private bool PassesFilter(bool isApple, bool isKeyboard) => _settings.DeviceFilter switch
        {
            DeviceFilterMode.Apple => isApple,
            DeviceFilterMode.Keyboards => isKeyboard,
            DeviceFilterMode.All => true,
            _ => isApple
        };

        private Dictionary<string, string> LoadKnownKeyboards()
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                if (!File.Exists(KnownKeyboardsPath)) return dict;
                foreach (string line in File.ReadAllLines(KnownKeyboardsPath))
                {
                    var parts = line.Split('\t');
                    if (parts.Length >= 2)
                    {
                        string mac = AppleHardwareMatcher.NormalizeMac(parts[0]);
                        string name = AppleHardwareMatcher.SanitizeTsvField(parts[1]);
                        if (!string.IsNullOrEmpty(mac) && !string.IsNullOrEmpty(name))
                        {
                            dict[mac] = name;
                        }
                    }
                }
            }
            catch { }
            return dict;
        }

        private void SaveKnownKeyboards(Dictionary<string, string> dict)
        {
            if (!_settings.RememberReleased) return;
            try
            {
                var lines = dict
                    .OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
                    .Select(kv => $"{kv.Key}\t{AppleHardwareMatcher.SanitizeTsvField(kv.Value)}");
                File.WriteAllLines(KnownKeyboardsPath, lines);
            }
            catch { }
        }

        public void Dispose()
        {
            _disposed = true;
            try
            {
                lock (_debounceLock)
                {
                    _debounceTimer?.Dispose();
                    _debounceTimer = null;
                }
                if (_watcher != null)
                {
                    _watcher.Added -= OnWatcherAdded;
                    _watcher.Updated -= OnWatcherUpdated;
                    _watcher.Removed -= OnWatcherRemoved;
                    _watcher.EnumerationCompleted -= OnWatcherEnumerationCompleted;
                    if (_watcher.Status == DeviceWatcherStatus.Started || _watcher.Status == DeviceWatcherStatus.EnumerationCompleted)
                    {
                        _watcher.Stop();
                    }
                }
            }
            catch { }
        }
    }
}
