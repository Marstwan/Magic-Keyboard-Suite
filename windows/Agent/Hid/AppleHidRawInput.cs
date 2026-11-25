using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using MagicKeyboardSuite.Shared;

namespace MagicKeyboardSuite.Hid
{
    internal sealed class CachedRawDevice : IDisposable
    {
        public IntPtr Handle;
        public uint Type;
        public string DeviceName = "";
        public bool IsApple;
        public uint VendorId;
        public uint ProductId;
        public ushort UsagePage;
        public ushort Usage;
        public IntPtr PreparsedData = IntPtr.Zero;
        public bool HasFnCap;
        public ushort FnUsagePage;
        public byte FnReportId;

        public void Dispose()
        {
            if (PreparsedData != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(PreparsedData);
                PreparsedData = IntPtr.Zero;
            }
        }
    }

    internal static class AppleHidRawInput
    {
        public const int WM_INPUT_DEVICE_CHANGE = 0x00FE;
        public const int WM_INPUT = 0x00FF;
        public const int GIDC_ARRIVAL = 1;
        public const int GIDC_REMOVAL = 2;

        public const uint RIDEV_PAGEONLY = 0x00000020;
        public const uint RIDEV_INPUTSINK = 0x00000100;
        public const uint RIDEV_DEVNOTIFY = 0x00002000;

        public const uint RID_INPUT = 0x10000003;
        public const uint RIM_TYPEKEYBOARD = 1;
        public const uint RIM_TYPEHID = 2;

        public const uint RIDI_PREPARSEDDATA = 0x20000005;
        public const uint RIDI_DEVICENAME = 0x20000007;
        public const uint RIDI_DEVICEINFO = 0x2000000B;

        public const int HidP_Input = 0;
        public const int HIDP_STATUS_SUCCESS = 0x00110000;
        public const ushort AppleFnUsageId = 0x0003;
        public static readonly ushort[] AppleFnUsagePages = { 0x00FF, 0xFF01, 0xFF00 };
        public static readonly ushort[] DiagnosticUsagePages = { 0x00FF, 0xFF01, 0xFF00, 0x000C, 0x0007 };

        [StructLayout(LayoutKind.Sequential)]
        public struct RAWINPUTDEVICE
        {
            public ushort usUsagePage;
            public ushort usUsage;
            public uint dwFlags;
            public IntPtr hwndTarget;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RAWINPUTDEVICELIST
        {
            public IntPtr hDevice;
            public uint dwType;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RAWINPUTHEADER
        {
            public uint dwType;
            public uint dwSize;
            public IntPtr hDevice;
            public IntPtr wParam;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RAWKEYBOARD
        {
            public ushort MakeCode;
            public ushort Flags;
            public ushort Reserved;
            public ushort VKey;
            public uint Message;
            public uint ExtraInformation;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RID_DEVICE_INFO_KEYBOARD
        {
            public uint dwType;
            public uint dwSubType;
            public uint dwKeyboardMode;
            public uint dwNumberOfFunctionKeys;
            public uint dwNumberOfIndicators;
            public uint dwNumberOfKeysTotal;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RID_DEVICE_INFO_HID
        {
            public uint dwVendorId;
            public uint dwProductId;
            public uint dwVersionNumber;
            public ushort usUsagePage;
            public ushort usUsage;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct RID_DEVICE_INFO
        {
            [FieldOffset(0)] public uint cbSize;
            [FieldOffset(4)] public uint dwType;
            [FieldOffset(8)] public RID_DEVICE_INFO_KEYBOARD keyboard;
            [FieldOffset(8)] public RID_DEVICE_INFO_HID hid;
        }

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool RegisterRawInputDevices(RAWINPUTDEVICE[] pRawInputDevices, uint uiNumDevices, uint cbSize);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetRawInputData(IntPtr hRawInput, uint uiCommand, IntPtr pData, ref uint pcbSize, uint cbSizeHeader);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetRawInputDeviceList(IntPtr pRawInputDeviceList, ref uint puiNumDevices, uint cbSize);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern uint GetRawInputDeviceInfo(IntPtr hDevice, uint uiCommand, StringBuilder? pData, ref uint pcbSize);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetRawInputDeviceInfo(IntPtr hDevice, uint uiCommand, IntPtr pData, ref uint pcbSize);

        [DllImport("hid.dll", SetLastError = false)]
        public static extern int HidP_GetUsages(
            int reportType,
            ushort usagePage,
            ushort linkCollection,
            [Out] ushort[] usageList,
            ref uint usageLength,
            IntPtr preparsedData,
            IntPtr report,
            uint reportLength);

        [DllImport("hid.dll", SetLastError = false)]
        public static extern int HidP_GetSpecificButtonCaps(
            int reportType,
            ushort usagePage,
            ushort linkCollection,
            ushort usage,
            [Out] byte[] buttonCaps,
            ref ushort buttonCapsLength,
            IntPtr preparsedData);

        public static bool RegisterSingle(IntPtr hwnd, ushort usagePage, ushort usage, uint flags)
        {
            RAWINPUTDEVICE[] rid = new RAWINPUTDEVICE[1];
            rid[0].usUsagePage = usagePage;
            rid[0].usUsage = usage;
            rid[0].dwFlags = flags;
            rid[0].hwndTarget = hwnd;
            return RegisterRawInputDevices(rid, 1, (uint)Marshal.SizeOf(typeof(RAWINPUTDEVICE)));
        }

        public static void RegisterDefaultAppleInputs(
            IntPtr hwnd,
            HashSet<ushort> registeredPages,
            HashSet<uint> registeredPairs)
        {
            RegisterSingle(hwnd, 0x01, 0x06, RIDEV_INPUTSINK | RIDEV_DEVNOTIFY);
            registeredPairs.Add((0x01u << 16) | 0x06u);

            if (RegisterSingle(hwnd, 0x0C, 0x01, RIDEV_INPUTSINK))
            {
                registeredPairs.Add((0x0Cu << 16) | 0x01u);
            }

            foreach (ushort page in AppleFnUsagePages)
            {
                if (RegisterSingle(hwnd, page, 0x0000, RIDEV_INPUTSINK | RIDEV_PAGEONLY))
                {
                    registeredPages.Add(page);
                }
            }

            RegisterConnectedAppleHidCollections(hwnd, registeredPages, registeredPairs);
        }

        public static List<CachedRawDevice> EnumerateDevices(bool onlyApple)
        {
            var result = new List<CachedRawDevice>();
            uint count = 0;
            uint itemSize = (uint)Marshal.SizeOf(typeof(RAWINPUTDEVICELIST));
            if (GetRawInputDeviceList(IntPtr.Zero, ref count, itemSize) != 0 || count == 0)
            {
                return result;
            }

            IntPtr listPtr = Marshal.AllocHGlobal((int)(count * itemSize));
            try
            {
                uint fetched = count;
                if (GetRawInputDeviceList(listPtr, ref fetched, itemSize) == uint.MaxValue)
                {
                    return result;
                }

                for (int i = 0; i < fetched; i++)
                {
                    IntPtr itemPtr = IntPtr.Add(listPtr, (int)(i * itemSize));
                    RAWINPUTDEVICELIST item = (RAWINPUTDEVICELIST)Marshal.PtrToStructure(itemPtr, typeof(RAWINPUTDEVICELIST))!;
                    if (item.dwType != RIM_TYPEKEYBOARD && item.dwType != RIM_TYPEHID) continue;

                    CachedRawDevice? dev = InspectDevice(item.hDevice, item.dwType);
                    if (dev == null) continue;
                    if (!onlyApple || dev.IsApple)
                    {
                        result.Add(dev);
                    }
                    else
                    {
                        dev.Dispose();
                    }
                }
            }
            finally
            {
                Marshal.FreeHGlobal(listPtr);
            }

            return result;
        }

        public static void RegisterConnectedAppleHidCollections(
            IntPtr hwnd,
            HashSet<ushort> registeredPages,
            HashSet<uint> registeredPairs)
        {
            List<CachedRawDevice> devices = EnumerateDevices(onlyApple: true);
            foreach (CachedRawDevice dev in devices)
            {
                try
                {
                    if (dev.Type != RIM_TYPEHID || dev.UsagePage == 0) continue;
                    if (dev.UsagePage == 0x01 && (dev.Usage == 0x06 || dev.Usage == 0x02)) continue;
                    if (registeredPages.Contains(dev.UsagePage)) continue;

                    uint pairKey = ((uint)dev.UsagePage << 16) | dev.Usage;
                    if (registeredPairs.Contains(pairKey)) continue;

                    if (RegisterSingle(hwnd, dev.UsagePage, dev.Usage, RIDEV_INPUTSINK))
                    {
                        registeredPairs.Add(pairKey);
                    }
                }
                finally
                {
                    dev.Dispose();
                }
            }
        }

        public static CachedRawDevice? InspectDevice(IntPtr hDevice, uint rawType)
        {
            if (hDevice == IntPtr.Zero) return null;

            var dev = new CachedRawDevice
            {
                Handle = hDevice,
                Type = rawType,
                DeviceName = GetDeviceName(hDevice)
            };

            if (TryGetDeviceInfo(hDevice, out RID_DEVICE_INFO info))
            {
                dev.Type = info.dwType;
                if (info.dwType == RIM_TYPEHID)
                {
                    dev.VendorId = info.hid.dwVendorId;
                    dev.ProductId = info.hid.dwProductId;
                    dev.UsagePage = info.hid.usUsagePage;
                    dev.Usage = info.hid.usUsage;
                }
            }

            dev.IsApple = AppleHardwareMatcher.IsAppleDevicePath(dev.DeviceName) ||
                          AppleHardwareMatcher.IsAppleVendorId(dev.VendorId);

            if (dev.IsApple && dev.Type == RIM_TYPEHID)
            {
                dev.PreparsedData = GetPreparsedData(hDevice);
                if (dev.PreparsedData != IntPtr.Zero)
                {
                    DetectAppleFnCapability(dev);
                }
            }

            return dev;
        }

        private static string GetDeviceName(IntPtr hDevice)
        {
            uint nameLen = 0;
            GetRawInputDeviceInfo(hDevice, RIDI_DEVICENAME, null, ref nameLen);
            if (nameLen == 0) return "";

            StringBuilder sb = new StringBuilder((int)nameLen + 1);
            if (GetRawInputDeviceInfo(hDevice, RIDI_DEVICENAME, sb, ref nameLen) > 0)
            {
                return sb.ToString();
            }
            return "";
        }

        private static bool TryGetDeviceInfo(IntPtr hDevice, out RID_DEVICE_INFO info)
        {
            info = new RID_DEVICE_INFO();
            uint size = (uint)Marshal.SizeOf(typeof(RID_DEVICE_INFO));
            info.cbSize = size;

            IntPtr ptr = Marshal.AllocHGlobal((int)size);
            try
            {
                Marshal.StructureToPtr(info, ptr, false);
                uint res = GetRawInputDeviceInfo(hDevice, RIDI_DEVICEINFO, ptr, ref size);
                if (res != uint.MaxValue && res > 0)
                {
                    info = (RID_DEVICE_INFO)Marshal.PtrToStructure(ptr, typeof(RID_DEVICE_INFO))!;
                    return true;
                }
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
            return false;
        }

        private static IntPtr GetPreparsedData(IntPtr hDevice)
        {
            uint size = 0;
            if (GetRawInputDeviceInfo(hDevice, RIDI_PREPARSEDDATA, IntPtr.Zero, ref size) != 0 || size == 0)
            {
                return IntPtr.Zero;
            }

            IntPtr ptr = Marshal.AllocHGlobal((int)size);
            uint copied = size;
            if (GetRawInputDeviceInfo(hDevice, RIDI_PREPARSEDDATA, ptr, ref copied) == uint.MaxValue)
            {
                Marshal.FreeHGlobal(ptr);
                return IntPtr.Zero;
            }
            return ptr;
        }

        private static void DetectAppleFnCapability(CachedRawDevice dev)
        {
            byte[] capsBuf = new byte[72 * 8];
            foreach (ushort page in AppleFnUsagePages)
            {
                ushort capsLen = 8;
                int status = HidP_GetSpecificButtonCaps(
                    HidP_Input,
                    page,
                    0,
                    AppleFnUsageId,
                    capsBuf,
                    ref capsLen,
                    dev.PreparsedData);

                if (status == HIDP_STATUS_SUCCESS && capsLen > 0)
                {
                    dev.HasFnCap = true;
                    dev.FnUsagePage = page;
                    dev.FnReportId = capsBuf[2];
                    return;
                }
            }
        }

        public static bool TryParseFnState(CachedRawDevice? dev, IntPtr reportPtr, uint reportLength, out bool fnPressed)
        {
            fnPressed = false;
            if (dev == null || !dev.HasFnCap || dev.PreparsedData == IntPtr.Zero || reportLength == 0)
            {
                return false;
            }

            if (dev.FnReportId != 0)
            {
                byte reportId = Marshal.ReadByte(reportPtr, 0);
                if (reportId != dev.FnReportId)
                {
                    return false;
                }
            }

            ushort[] usages = new ushort[32];
            uint usageLen = (uint)usages.Length;
            int status = HidP_GetUsages(
                HidP_Input,
                dev.FnUsagePage,
                0,
                usages,
                ref usageLen,
                dev.PreparsedData,
                reportPtr,
                reportLength);

            if (status != HIDP_STATUS_SUCCESS)
            {
                return false;
            }

            for (int i = 0; i < usageLen; i++)
            {
                if (usages[i] == AppleFnUsageId)
                {
                    fnPressed = true;
                    break;
                }
            }
            return true;
        }

        public static string DescribeActiveUsages(IntPtr preparsedData, IntPtr reportPtr, uint reportLength)
        {
            if (preparsedData == IntPtr.Zero || reportLength == 0) return "";
            var parts = new List<string>();
            ushort[] usages = new ushort[32];

            foreach (ushort page in DiagnosticUsagePages)
            {
                uint usageLen = (uint)usages.Length;
                int status = HidP_GetUsages(HidP_Input, page, 0, usages, ref usageLen, preparsedData, reportPtr, reportLength);
                if (status == HIDP_STATUS_SUCCESS)
                {
                    if (usageLen == 0)
                    {
                        parts.Add($"0x{page:X4}:[]");
                    }
                    else
                    {
                        var list = new List<string>();
                        for (int i = 0; i < usageLen; i++)
                        {
                            list.Add($"0x{usages[i]:X4}");
                        }
                        parts.Add($"0x{page:X4}:[{string.Join(",", list)}]");
                    }
                }
            }

            return string.Join(" ", parts);
        }
    }

    internal sealed class RawInputReceiverWindow : NativeWindow, IDisposable
    {
        private readonly MagicKeyboardContext _ctx;
        private readonly uint _reloadMsgId;
        private readonly Dictionary<IntPtr, CachedRawDevice> _deviceCache = new Dictionary<IntPtr, CachedRawDevice>();
        private readonly HashSet<ushort> _registeredPages = new HashSet<ushort>();
        private readonly HashSet<uint> _registeredPairs = new HashSet<uint>();
        private bool _disposed;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern uint RegisterWindowMessage(string lpString);

        public RawInputReceiverWindow(MagicKeyboardContext ctx)
        {
            _ctx = ctx;
            _reloadMsgId = RegisterWindowMessage(AppleHardwareMatcher.ReloadSettingsMessageName);
            CreateHandle(new CreateParams());
            AppleHidRawInput.RegisterDefaultAppleInputs(Handle, _registeredPages, _registeredPairs);
        }

        private CachedRawDevice? GetOrCacheDevice(IntPtr hDevice, uint rawType)
        {
            if (_deviceCache.TryGetValue(hDevice, out CachedRawDevice existing))
            {
                return existing;
            }

            CachedRawDevice? inspected = AppleHidRawInput.InspectDevice(hDevice, rawType);
            if (inspected != null)
            {
                _deviceCache[hDevice] = inspected;
            }
            return inspected;
        }

        private void ClearDeviceCacheEntry(IntPtr hDevice)
        {
            if (_deviceCache.TryGetValue(hDevice, out CachedRawDevice existing))
            {
                bool wasApple = existing.IsApple;
                existing.Dispose();
                _deviceCache.Remove(hDevice);
                if (wasApple)
                {
                    _ctx.OnAppleDeviceRemoved();
                }
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (_reloadMsgId != 0 && m.Msg == _reloadMsgId)
            {
                _ctx.LoadSettings();
                _ctx.ActivatePreferredAppleLayout();
                return;
            }

            if (m.Msg == AppleHidRawInput.WM_INPUT_DEVICE_CHANGE)
            {
                int change = m.WParam.ToInt32();
                if (change == AppleHidRawInput.GIDC_REMOVAL)
                {
                    ClearDeviceCacheEntry(m.LParam);
                    _ctx.SetAppleFnState(false);
                }
                else if (change == AppleHidRawInput.GIDC_ARRIVAL)
                {
                    AppleHidRawInput.RegisterConnectedAppleHidCollections(Handle, _registeredPages, _registeredPairs);
                }
                base.WndProc(ref m);
                return;
            }

            if (m.Msg == AppleHidRawInput.WM_INPUT)
            {
                uint dwSize = 0;
                uint headerSize = (uint)Marshal.SizeOf(typeof(AppleHidRawInput.RAWINPUTHEADER));
                AppleHidRawInput.GetRawInputData(m.LParam, AppleHidRawInput.RID_INPUT, IntPtr.Zero, ref dwSize, headerSize);
                if (dwSize > 0)
                {
                    IntPtr buffer = Marshal.AllocHGlobal((int)dwSize);
                    try
                    {
                        if (AppleHidRawInput.GetRawInputData(m.LParam, AppleHidRawInput.RID_INPUT, buffer, ref dwSize, headerSize) == dwSize)
                        {
                            var header = (AppleHidRawInput.RAWINPUTHEADER)Marshal.PtrToStructure(buffer, typeof(AppleHidRawInput.RAWINPUTHEADER))!;
                            if (header.hDevice != IntPtr.Zero)
                            {
                                if (header.dwType == AppleHidRawInput.RIM_TYPEKEYBOARD)
                                {
                                    CachedRawDevice? dev = GetOrCacheDevice(header.hDevice, header.dwType);
                                    if (dev != null && !string.IsNullOrEmpty(dev.DeviceName))
                                    {
                                        _ctx.NotifyRawKeyboardDevice(dev.DeviceName);
                                    }
                                }
                                else if (header.dwType == AppleHidRawInput.RIM_TYPEHID)
                                {
                                    CachedRawDevice? dev = GetOrCacheDevice(header.hDevice, header.dwType);
                                    if (dev != null && dev.IsApple && dev.HasFnCap && dwSize >= headerSize + 8)
                                    {
                                        uint sizeHid = (uint)Marshal.ReadInt32(buffer, (int)headerSize);
                                        uint count = (uint)Marshal.ReadInt32(buffer, (int)headerSize + 4);
                                        long totalPayload = (long)sizeHid * count;
                                        if (sizeHid > 0 && count > 0 && (long)headerSize + 8 + totalPayload <= dwSize)
                                        {
                                            IntPtr rawDataStart = IntPtr.Add(buffer, (int)headerSize + 8);
                                            for (int i = 0; i < count; i++)
                                            {
                                                IntPtr reportPtr = IntPtr.Add(rawDataStart, (int)(i * sizeHid));
                                                if (AppleHidRawInput.TryParseFnState(dev, reportPtr, sizeHid, out bool fnPressed))
                                                {
                                                    _ctx.SetAppleFnState(fnPressed);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(buffer);
                    }
                }
            }
            base.WndProc(ref m);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            foreach (CachedRawDevice dev in _deviceCache.Values)
            {
                dev.Dispose();
            }
            _deviceCache.Clear();
            if (Handle != IntPtr.Zero)
            {
                DestroyHandle();
            }
        }
    }
}
