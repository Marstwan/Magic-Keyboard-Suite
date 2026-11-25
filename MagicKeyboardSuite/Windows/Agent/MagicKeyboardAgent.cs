// ============================================================================
//  MagicKeyboardAgent.cs
//  Servicio en segundo plano para Apple Magic Keyboard en Windows 11
//  (Sin ventanas Windows Forms — toda la interfaz visual es WinUI 3 nativa
//   en MagicKeyboardSuite.exe).
// ============================================================================

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using MagicKeyboardSuite.Shared;
using Microsoft.Win32;

namespace MagicKeyboardSuite
{
    internal static class Program
    {
        private static Mutex _mutex;

        [STAThread]
        private static void Main(string[] args)
        {
            if (args != null)
            {
                foreach (string arg in args)
                {
                    if (string.Equals(arg, "--diag-fn", StringComparison.OrdinalIgnoreCase))
                    {
                        Application.EnableVisualStyles();
                        Application.SetCompatibleTextRenderingDefault(false);
                        Application.Run(new FnDiagnosticForm());
                        return;
                    }
                }
            }

            bool createdNew;
            _mutex = new Mutex(true, "Global\\MagicKeyboardSuite_Agent_Hook_v3", out createdNew);
            if (!createdNew)
            {
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MagicKeyboardContext());
        }
    }

    public sealed class MagicKeyboardContext : ApplicationContext
    {
        private const string RegPath = SettingsKeys.RegistryPath;
        private const string RunRegPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string RunValueName = "MagicKeyboardSuite";

        private static readonly (string Klid, string Name, string LangRegex)[] AppleLayouts =
        {
            ("a000040a", "Español / Català (Apple)", "^(es|ca)"),
            ("a0020409", "United States-International (Apple)", "^en-INTL"),
            ("a0000409", "United States (Apple)", "^en-US"),
            ("a0000809", "United Kingdom (Apple)", "^en-GB"),
            ("a000040c", "Français (Apple)", "^fr"),
            ("a0000407", "Deutsch (Apple)", "^de"),
            ("a0000410", "Italiano (Apple)", "^it"),
            ("a0000416", "Português (Apple)", "^pt"),
            ("a0000413", "Nederlands (Apple)", "^nl"),
            ("a000041d", "Svenska (Apple)", "^sv"),
            ("a0000406", "Dansk (Apple)", "^da"),
            ("a0000414", "Norsk (Apple)", "^(nb|nn|no)"),
            ("a000040b", "Suomi (Apple)", "^fi"),
            ("a000100c", "Swiss (Apple)", "^(de|fr|it)-CH"),
            ("a0000813", "Belgian (Apple)", "^(nl|fr)-BE"),
            ("a0000415", "Polski (Apple)", "^pl"),
            ("a0000419", "Russian (Apple)", "^ru")
        };

        public int ModifierMode = 0;
        public bool SwapIsoKeysInAgent = true;
        public bool LeftOptionAsAltGr = true;
        public bool MacNavigationShortcuts = true;
        public bool MediaKeysDefault = true;
        public bool RightOptionAsFn = false;
        public bool OnlyAppleHardware = true;
        public string PreferredKlid = "a000040a";

        private IntPtr _hookId = IntPtr.Zero;
        private readonly LowLevelKeyboardProc _hookProc;
        private NotifyIcon _trayIcon;
        private Font _trayBoldFont;
        private RawInputReceiverWindow _rawWindow;

        private bool _lCtrlDown;
        private bool _rCtrlDown;
        private bool _lAltDown;
        private bool _rAltDown;
        private bool _lWinDown;
        private bool _rWinDown;
        private bool _lShiftDown;
        private bool _rShiftDown;
        private volatile bool _fnDown;
        private volatile bool _lastKeyWasApple = true;
        private long _lastRegistryHeartbeatTicks;

        private static readonly IntPtr InjectedMarker = new IntPtr(0x4D4B5332);

        private static readonly HashSet<int> CmdToCtrlVks = new HashSet<int>
        {
            0x41, 0x42, 0x43, 0x46, 0x47, 0x48, 0x4E, 0x4F,
            0x50, 0x52, 0x53, 0x54, 0x56, 0x57, 0x58, 0x59, 0x5A,
            0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x30,
            0xBB, 0xBD, 0x08, 0x25, 0x26, 0x27, 0x28
        };

        private static readonly HashSet<int> SpanishAltGrVks = new HashSet<int>
        {
            0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x45,
            0xDC, 0xBA, 0xBB, 0xBF, 0xDE, 0xC0
        };

        public MagicKeyboardContext()
        {
            EnsureFirstRunAppleLayoutConfigured();
            LoadSettings();
            ActivatePreferredAppleLayout();

            _rawWindow = new RawInputReceiverWindow(this);
            _hookProc = HookCallback;
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                _hookId = SetWindowsHookEx(WH_KEYBOARD_LL, _hookProc, GetModuleHandle(curModule.ModuleName), 0);
            }

            BuildTrayMenu();
        }

        private void EnsureFirstRunAppleLayoutConfigured()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RegPath))
                {
                    if (key == null) return;
                    bool alreadyConfigured = Convert.ToInt32(key.GetValue(SettingsKeys.LayoutAutoConfigured, 0)) != 0;
                    string existingKlid = Convert.ToString(key.GetValue(SettingsKeys.PreferredKlid, "")) ?? "";

                    if (!alreadyConfigured || string.IsNullOrWhiteSpace(existingKlid))
                    {
                        string uiLang = CultureInfo.CurrentUICulture.Name;
                        string matchedKlid = "a000040a";
                        foreach (var lay in AppleLayouts)
                        {
                            if (Regex.IsMatch(uiLang, lay.LangRegex, RegexOptions.IgnoreCase))
                            {
                                matchedKlid = lay.Klid;
                                break;
                            }
                        }

                        if (string.IsNullOrWhiteSpace(existingKlid))
                        {
                            key.SetValue(SettingsKeys.PreferredKlid, matchedKlid, RegistryValueKind.String);
                        }
                        key.SetValue(SettingsKeys.LayoutAutoConfigured, 1, RegistryValueKind.DWord);

                        using (RegistryKey preload = Registry.CurrentUser.CreateSubKey(@"Keyboard Layout\Preload"))
                        {
                            preload?.SetValue("1", matchedKlid, RegistryValueKind.String);
                        }
                    }
                }
            }
            catch { }
        }

        public void LoadSettings()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RegPath))
                {
                    if (key != null)
                    {
                        ModifierMode = Convert.ToInt32(key.GetValue(SettingsKeys.ModifierMode, 0));
                        SwapIsoKeysInAgent = Convert.ToInt32(key.GetValue(SettingsKeys.SwapIsoKeys, 1)) != 0;
                        LeftOptionAsAltGr = Convert.ToInt32(key.GetValue(SettingsKeys.LeftOptionAsAltGr, 1)) != 0;
                        MacNavigationShortcuts = Convert.ToInt32(key.GetValue(SettingsKeys.MacNavigationShortcuts, 1)) != 0;
                        MediaKeysDefault = Convert.ToInt32(key.GetValue(SettingsKeys.MediaKeysDefault, 1)) != 0;
                        RightOptionAsFn = Convert.ToInt32(key.GetValue(SettingsKeys.RightOptionAsFn, 0)) != 0;
                        OnlyAppleHardware = Convert.ToInt32(key.GetValue(SettingsKeys.OnlyAppleHardware, 1)) != 0;
                        string klid = Convert.ToString(key.GetValue(SettingsKeys.PreferredKlid, "a000040a"));
                        PreferredKlid = string.IsNullOrWhiteSpace(klid) ? "a000040a" : klid;
                        string lang = (Convert.ToString(key.GetValue(SettingsKeys.AppLanguage, "auto")) ?? "auto").ToLowerInvariant();
                        L10n.OverrideLanguage = (lang == "ca" || lang == "es" || lang == "en") ? lang : null;
                    }
                }
            }
            catch { }
        }

        public void ActivatePreferredAppleLayout()
        {
            try
            {
                string klidToLoad = PreferredKlid;
                using (RegistryKey layoutKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Keyboard Layouts\" + PreferredKlid, false))
                {
                    if (layoutKey == null && PreferredKlid.Length == 8 && (PreferredKlid[0] == 'a' || PreferredKlid[0] == 'A'))
                    {
                        // Si el usuario aún no ha importado los drivers de Boot Camp, usar la disposición estándar de Windows equivalente
                        klidToLoad = "0" + PreferredKlid.Substring(1);
                    }
                }

                try
                {
                    using (RegistryKey preload = Registry.CurrentUser.CreateSubKey(@"Keyboard Layout\Preload"))
                    {
                        if (preload != null)
                        {
                            bool found = false;
                            foreach (string name in preload.GetValueNames())
                            {
                                string existing = Convert.ToString(preload.GetValue(name)) ?? "";
                                if (string.Equals(existing, klidToLoad, StringComparison.OrdinalIgnoreCase))
                                {
                                    found = true;
                                    break;
                                }
                            }
                            if (!found)
                            {
                                for (int i = 1; i <= 32; i++)
                                {
                                    string slot = i.ToString();
                                    if (preload.GetValue(slot) == null)
                                    {
                                        preload.SetValue(slot, klidToLoad, RegistryValueKind.String);
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
                catch { }

                IntPtr hkl = LoadKeyboardLayout(klidToLoad, 0x00000001 | 0x00000002);
                if (hkl != IntPtr.Zero)
                {
                    SystemParametersInfo(0x005A, 0, ref hkl, 0x02);
                    IntPtr fg = GetForegroundWindow();
                    if (fg != IntPtr.Zero)
                    {
                        PostMessage(fg, 0x0050, IntPtr.Zero, hkl);
                    }
                    PostMessage((IntPtr)0xFFFF, 0x0050, IntPtr.Zero, hkl);
                }
            }
            catch { }
        }

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        public void LaunchWinUI3App()
        {
            try
            {
                foreach (Process existing in Process.GetProcessesByName("MagicKeyboardSuite"))
                {
                    if (existing.MainWindowHandle != IntPtr.Zero)
                    {
                        ShowWindow(existing.MainWindowHandle, 9); // SW_RESTORE
                        SetForegroundWindow(existing.MainWindowHandle);
                        return;
                    }
                }

                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string[] candidates = new[]
                {
                    Path.Combine(baseDir, "App", "MagicKeyboardSuite.exe"),
                    Path.Combine(baseDir, "MagicKeyboardSuite.exe"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "MagicKeyboardSuite", "App", "MagicKeyboardSuite.exe")
                };

                foreach (string candidate in candidates)
                {
                    if (File.Exists(candidate))
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = candidate,
                            WorkingDirectory = Path.GetDirectoryName(candidate),
                            UseShellExecute = true
                        });
                        return;
                    }
                }
            }
            catch { }
        }

        private void BuildTrayMenu()
        {
            _trayBoldFont = new Font(SystemFonts.MenuFont, FontStyle.Bold);
            var menu = new ContextMenuStrip();
            menu.Opening += (s, e) => PopulateTrayMenu(menu);
            PopulateTrayMenu(menu);

            _trayIcon = new NotifyIcon
            {
                Icon = CreateKeyboardIcon(),
                Text = L10n.AppName,
                ContextMenuStrip = menu,
                Visible = true
            };

            _trayIcon.Click += (s, e) =>
            {
                var me = e as MouseEventArgs;
                if (me != null && me.Button == MouseButtons.Left)
                {
                    LaunchWinUI3App();
                }
            };
        }

        private void PopulateTrayMenu(ContextMenuStrip menu)
        {
            menu.Items.Clear();

            var itemOpenGui = new ToolStripMenuItem(L10n.TrayOpen)
            {
                Font = _trayBoldFont
            };
            itemOpenGui.Click += (s, e) => LaunchWinUI3App();
            menu.Items.Add(itemOpenGui);

            string layoutName = AppleLayouts[0].Name;
            foreach (var lay in AppleLayouts)
            {
                if (string.Equals(lay.Klid, PreferredKlid, StringComparison.OrdinalIgnoreCase))
                {
                    layoutName = lay.Name;
                    break;
                }
            }
            var itemLayout = new ToolStripMenuItem(L10n.FooterCurrentLayout(layoutName)) { Enabled = false };
            menu.Items.Add(itemLayout);

            menu.Items.Add(new ToolStripSeparator());

            var itemLaunchLogin = new ToolStripMenuItem(L10n.TrayLaunchAtLogin)
            {
                Checked = IsLaunchAtLoginEnabled(),
                CheckOnClick = true
            };
            itemLaunchLogin.Click += (s, e) => SetLaunchAtLogin(itemLaunchLogin.Checked);
            menu.Items.Add(itemLaunchLogin);

            menu.Items.Add(new ToolStripSeparator());

            var itemExit = new ToolStripMenuItem(L10n.TrayQuit);
            itemExit.Click += (s, e) =>
            {
                _trayIcon.Visible = false;
                _trayIcon.Icon?.Dispose();
                _trayIcon.Dispose();
                _trayBoldFont?.Dispose();
                if (_hookId != IntPtr.Zero) UnhookWindowsHookEx(_hookId);
                _rawWindow?.Dispose();
                Application.Exit();
            };
            menu.Items.Add(itemExit);
        }

        private static bool IsLaunchAtLoginEnabled()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunRegPath, false))
                {
                    if (key?.GetValue(RunValueName) != null) return true;
                }
                using (RegistryKey lm = Registry.LocalMachine.OpenSubKey(RunRegPath, false))
                {
                    return lm?.GetValue(RunValueName) != null;
                }
            }
            catch { return true; }
        }

        private static void SetLaunchAtLogin(bool enabled)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RunRegPath))
                {
                    if (enabled)
                    {
                        key?.SetValue(RunValueName, $"\"{Application.ExecutablePath}\"", RegistryValueKind.String);
                    }
                    else
                    {
                        key?.DeleteValue(RunValueName, false);
                    }
                }
            }
            catch { }
        }

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        public Icon CreateKeyboardIcon()
        {
            using (Bitmap bmp = new Bitmap(32, 32))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.Clear(Color.Transparent);
                    using (LinearGradientBrush bg = new LinearGradientBrush(new Rectangle(2, 4, 28, 24),
                        Color.FromArgb(0, 120, 212), Color.FromArgb(70, 50, 210), 45f))
                    {
                        g.FillRectangle(bg, 2, 5, 28, 22);
                    }
                    using (Pen border = new Pen(Color.White, 2f))
                    {
                        g.DrawRectangle(border, 3, 6, 26, 20);
                        g.DrawLine(border, 7, 12, 10, 12);
                        g.DrawLine(border, 14, 12, 17, 12);
                        g.DrawLine(border, 21, 12, 24, 12);
                        g.DrawLine(border, 9, 19, 22, 19);
                    }
                }
                IntPtr hIcon = bmp.GetHicon();
                try
                {
                    using (Icon temp = Icon.FromHandle(hIcon))
                    {
                        return (Icon)temp.Clone();
                    }
                }
                finally
                {
                    DestroyIcon(hIcon);
                }
            }
        }

        public static bool IsAppleDevicePath(string deviceName)
        {
            if (string.IsNullOrEmpty(deviceName)) return false;
            string upper = deviceName.ToUpperInvariant();
            return upper.Contains("VID_05AC") || upper.Contains("VID&000105AC") ||
                   upper.Contains("VID_004C") || upper.Contains("VID&0002004C") ||
                   upper.Contains("05AC") || upper.Contains("004C");
        }

        public void SetAppleFnState(bool pressed)
        {
            _fnDown = pressed;
            if (pressed)
            {
                _lastKeyWasApple = true;
            }
        }

        public void NotifyRawKeyboardDevice(string deviceName)
        {
            if (string.IsNullOrEmpty(deviceName)) return;
            bool isApple = IsAppleDevicePath(deviceName);
            _lastKeyWasApple = isApple;

            if (isApple)
            {
                long nowTicks = DateTime.UtcNow.Ticks;
                if (nowTicks - _lastRegistryHeartbeatTicks > TimeSpan.FromSeconds(4).Ticks)
                {
                    _lastRegistryHeartbeatTicks = nowTicks;
                    try
                    {
                        using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RegPath))
                        {
                            key?.SetValue("LastAppleKeyUtcTicks", nowTicks, RegistryValueKind.QWord);
                            key?.SetValue("LastAppleDevicePath", deviceName, RegistryValueKind.String);
                        }
                    }
                    catch { }
                }
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                KBDLLHOOKSTRUCT kb = (KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT));
                if (kb.dwExtraInfo == InjectedMarker)
                {
                    return CallNextHookEx(_hookId, nCode, wParam, lParam);
                }

                if (OnlyAppleHardware && !_lastKeyWasApple)
                {
                    return CallNextHookEx(_hookId, nCode, wParam, lParam);
                }

                int msg = wParam.ToInt32();
                bool isDown = (msg == WM_KEYDOWN || msg == WM_SYSKEYDOWN);
                bool isUp = (msg == WM_KEYUP || msg == WM_SYSKEYUP);
                int vk = (int)kb.vkCode;
                int sc = (int)kb.scanCode;
                bool isExtended = (kb.flags & LLKHF_EXTENDED) != 0;

                if (vk == VK_LSHIFT) _lShiftDown = isDown;
                if (vk == VK_RSHIFT) _rShiftDown = isDown;

                // 1. Intercambio físico ISO: [º ª \] (SC 0x56) <-> [< >] (SC 0x29)
                if (SwapIsoKeysInAgent && !isExtended)
                {
                    if (sc == 0x56)
                    {
                        uint mappedVk = MapVirtualKey(0x29, 1);
                        if (mappedVk == 0) mappedVk = 0xDC;
                        SendKeyVk((ushort)mappedVk, 0x29, false, isUp);
                        return (IntPtr)1;
                    }
                    if (sc == 0x29)
                    {
                        uint mappedVk = MapVirtualKey(0x56, 1);
                        if (mappedVk == 0) mappedVk = 0xE2;
                        SendKeyVk((ushort)mappedVk, 0x56, false, isUp);
                        return (IntPtr)1;
                    }
                }

                // 2a. Option derecho (AltGr) como tecla Fn (opcional, desactivado por defecto)
                if (RightOptionAsFn)
                {
                    // En disposiciones con AltGr (p. ej. Español), Windows inyecta un VK_LCONTROL sintético
                    // con scanCode 0x21D antes de VK_RMENU. Lo suprimimos para evitar Ctrl+Alt+Supr o AltGr+F4.
                    if (vk == VK_LCONTROL && sc == 0x21D)
                    {
                        return (IntPtr)1;
                    }
                    if (vk == VK_RMENU)
                    {
                        _rAltDown = isDown;
                        return (IntPtr)1;
                    }
                }

                // 2. Intercambio total Command <-> Control
                if (ModifierMode == 1)
                {
                    if (vk == VK_LWIN)
                    {
                        _lCtrlDown = isDown;
                        SendKeyVk(VK_LCONTROL, 0x1D, false, isUp);
                        return (IntPtr)1;
                    }
                    if (vk == VK_RWIN)
                    {
                        _rCtrlDown = isDown;
                        SendKeyVk(VK_RCONTROL, 0x1D, true, isUp);
                        return (IntPtr)1;
                    }
                    if (vk == VK_LCONTROL)
                    {
                        _lWinDown = isDown;
                        SendKeyVk(VK_LWIN, 0x5B, true, isUp);
                        return (IntPtr)1;
                    }
                }
                else
                {
                    if (vk == VK_LCONTROL) _lCtrlDown = isDown;
                    if (vk == VK_RCONTROL) _rCtrlDown = isDown;
                    if (vk == VK_LWIN)
                    {
                        _lWinDown = isDown;
                        if (ModifierMode == 0) return (IntPtr)1;
                    }
                    if (vk == VK_RWIN)
                    {
                        _rWinDown = isDown;
                        if (ModifierMode == 0) return (IntPtr)1;
                    }
                }

                if (vk == VK_LMENU) _lAltDown = isDown;
                if (vk == VK_RMENU) _rAltDown = isDown;

                bool cmdDown = _lWinDown || _rWinDown;
                bool ctrlDown = _lCtrlDown || _rCtrlDown;
                bool shiftDown = _lShiftDown || _rShiftDown;
                bool optDown = _lAltDown || (!RightOptionAsFn && _rAltDown);
                bool effectiveFn = _fnDown || (RightOptionAsFn && _rAltDown);

                // 3. Option Izquierdo (LAlt) como AltGr
                if (LeftOptionAsAltGr && _lAltDown && !ctrlDown && !cmdDown && SpanishAltGrVks.Contains(vk))
                {
                    if (isDown)
                    {
                        SendAltGrKey((ushort)vk, (ushort)sc, shiftDown);
                    }
                    return (IntPtr)1;
                }

                // 4. Supr e Inicio/Fin/RePág/AvPág
                if (MacNavigationShortcuts)
                {
                    if (vk == 0x08)
                    {
                        if (effectiveFn || _rCtrlDown || _rWinDown)
                        {
                            SendKeyVk(0x2E, 0x53, true, isUp);
                            return (IntPtr)1;
                        }
                        if (_lAltDown && !ctrlDown && !cmdDown)
                        {
                            SendModifiedKey(0x08, 0x0E, false, true, false, shiftDown, isUp);
                            return (IntPtr)1;
                        }
                    }

                    if (effectiveFn || (cmdDown && ModifierMode == 0))
                    {
                        if (vk == 0x25) { SendKeyVk(0x24, 0x47, true, isUp); return (IntPtr)1; }
                        if (vk == 0x27) { SendKeyVk(0x23, 0x4F, true, isUp); return (IntPtr)1; }
                        if (vk == 0x26) { SendKeyVk(0x21, 0x49, true, isUp); return (IntPtr)1; }
                        if (vk == 0x28) { SendKeyVk(0x22, 0x51, true, isUp); return (IntPtr)1; }
                    }

                    if (_lAltDown && !ctrlDown && !cmdDown && (vk == 0x25 || vk == 0x27))
                    {
                        SendModifiedKey((ushort)vk, (ushort)sc, true, true, false, shiftDown, isUp);
                        return (IntPtr)1;
                    }
                }

                // 5. Atajos de Command (⌘) en Modo Híbrido
                if (ModifierMode == 0 && cmdDown)
                {
                    if (vk == 0x51)
                    {
                        SendModifiedKey(0x73, 0x3E, false, false, true, false, isUp);
                        return (IntPtr)1;
                    }
                    if (vk == 0x09)
                    {
                        SendModifiedKey(0x09, 0x0F, false, false, true, shiftDown, isUp);
                        return (IntPtr)1;
                    }
                    if (vk == 0x20)
                    {
                        if (isDown) SendWinCombo(0x53);
                        return (IntPtr)1;
                    }
                    if (CmdToCtrlVks.Contains(vk))
                    {
                        SendModifiedKey((ushort)vk, (ushort)sc, isExtended, true, false, shiftDown, isUp);
                        return (IntPtr)1;
                    }
                }

                // 6. Multimedia F7–F12
                if (vk >= 0x70 && vk <= 0x7B)
                {
                    bool fnModifier = effectiveFn || _rCtrlDown;
                    bool triggerMedia = MediaKeysDefault ? (!fnModifier && !optDown && !cmdDown) : fnModifier;
                    if (triggerMedia)
                    {
                        if (isDown) ExecuteAppleMediaKey(vk);
                        return (IntPtr)1;
                    }
                }
            }

            return CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        private void ExecuteAppleMediaKey(int vk)
        {
            switch (vk)
            {
                case 0x72: SendWinCombo(0x09); break;
                case 0x73: SendWinCombo(0x53); break;
                case 0x74: SendWinCombo(0x48); break;
                case 0x76: TapSingleVk(0xB1); break;
                case 0x77: TapSingleVk(0xB3); break;
                case 0x78: TapSingleVk(0xB0); break;
                case 0x79: TapSingleVk(0xAD); break;
                case 0x7A: TapSingleVk(0xAE); break;
                case 0x7B: TapSingleVk(0xAF); break;
            }
        }

        private static void SendKeyVk(ushort vk, ushort sc, bool extended, bool keyUp)
        {
            INPUT[] inputs = new INPUT[1];
            inputs[0].type = INPUT_KEYBOARD;
            inputs[0].U.ki.wVk = vk;
            inputs[0].U.ki.wScan = sc;
            uint flags = keyUp ? KEYEVENTF_KEYUP : 0u;
            if (extended) flags |= KEYEVENTF_EXTENDEDKEY;
            inputs[0].U.ki.dwFlags = flags;
            inputs[0].U.ki.dwExtraInfo = InjectedMarker;
            SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT)));
        }

        private static void TapSingleVk(ushort vk)
        {
            SendKeyVk(vk, 0, false, false);
            SendKeyVk(vk, 0, false, true);
        }

        private static void SendWinCombo(ushort vk)
        {
            SendKeyVk(VK_LWIN, 0x5B, true, false);
            SendKeyVk(vk, 0, false, false);
            SendKeyVk(vk, 0, false, true);
            SendKeyVk(VK_LWIN, 0x5B, true, true);
        }

        private static void SendAltGrKey(ushort vk, ushort sc, bool shift)
        {
            SendKeyVk(VK_LCONTROL, 0x1D, false, false);
            SendKeyVk(VK_RMENU, 0x38, true, false);
            if (shift) SendKeyVk(VK_LSHIFT, 0x2A, false, false);
            SendKeyVk(vk, sc, false, false);
            SendKeyVk(vk, sc, false, true);
            if (shift) SendKeyVk(VK_LSHIFT, 0x2A, false, true);
            SendKeyVk(VK_RMENU, 0x38, true, true);
            SendKeyVk(VK_LCONTROL, 0x1D, false, true);
        }

        private static void SendModifiedKey(ushort vk, ushort sc, bool extended, bool ctrl, bool alt, bool shift, bool isUp)
        {
            if (!isUp)
            {
                if (ctrl) SendKeyVk(VK_LCONTROL, 0x1D, false, false);
                if (alt) SendKeyVk(VK_LMENU, 0x38, false, false);
                if (shift) SendKeyVk(VK_LSHIFT, 0x2A, false, false);
                SendKeyVk(vk, sc, extended, false);
            }
            else
            {
                SendKeyVk(vk, sc, extended, true);
                if (shift) SendKeyVk(VK_LSHIFT, 0x2A, false, true);
                if (alt) SendKeyVk(VK_LMENU, 0x38, false, true);
                if (ctrl) SendKeyVk(VK_LCONTROL, 0x1D, false, true);
            }
        }

        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_SYSKEYUP = 0x0105;
        private const uint LLKHF_EXTENDED = 0x01;

        private const ushort VK_LSHIFT = 0xA0;
        private const ushort VK_RSHIFT = 0xA1;
        private const ushort VK_LCONTROL = 0xA2;
        private const ushort VK_RCONTROL = 0xA3;
        private const ushort VK_LMENU = 0xA4;
        private const ushort VK_RMENU = 0xA5;
        private const ushort VK_LWIN = 0x5B;
        private const ushort VK_RWIN = 0x5C;

        private const int INPUT_KEYBOARD = 1;
        private const uint KEYEVENTF_EXTENDEDKEY = 0x0001;
        private const uint KEYEVENTF_KEYUP = 0x0002;

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        private struct KBDLLHOOKSTRUCT
        {
            public uint vkCode;
            public uint scanCode;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public int type;
            public InputUnion U;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct InputUnion
        {
            [FieldOffset(0)] public KEYBDINPUT ki;
            [FieldOffset(0)] public MOUSEINPUT mi;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr LoadKeyboardLayout(string pwszKLID, uint Flags);

        [DllImport("user32.dll")]
        private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, ref IntPtr pvParam, uint fWinIni);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern uint MapVirtualKey(uint uCode, uint uMapType);
    }

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
        public static extern uint GetRawInputDeviceInfo(IntPtr hDevice, uint uiCommand, StringBuilder pData, ref uint pcbSize);

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
            // 1. Teclado estándar (para detectar si el evento proviene de hardware Apple + notificaciones connect/disconnect)
            RegisterSingle(hwnd, 0x01, 0x06, RIDEV_INPUTSINK | RIDEV_DEVNOTIFY);
            registeredPairs.Add((0x01u << 16) | 0x06u);

            // 2. Colecciones HID donde Apple expone Fn / teclas especiales cuando Windows las deja pasar a modo usuario
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
                    RAWINPUTDEVICELIST item = (RAWINPUTDEVICELIST)Marshal.PtrToStructure(itemPtr, typeof(RAWINPUTDEVICELIST));
                    if (item.dwType != RIM_TYPEKEYBOARD && item.dwType != RIM_TYPEHID) continue;

                    CachedRawDevice dev = InspectDevice(item.hDevice, item.dwType);
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
                    // No duplicar teclado/ratón estándar ni páginas ya cubiertas con RIDEV_PAGEONLY
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

        public static CachedRawDevice InspectDevice(IntPtr hDevice, uint rawType)
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

            dev.IsApple = MagicKeyboardContext.IsAppleDevicePath(dev.DeviceName) ||
                          dev.VendorId == 0x05AC || dev.VendorId == 0x004C;

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
                    info = (RID_DEVICE_INFO)Marshal.PtrToStructure(ptr, typeof(RID_DEVICE_INFO));
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
            // HIDP_BUTTON_CAPS mide 72 bytes; reservamos espacio para hasta 8 entradas.
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
                    dev.FnReportId = capsBuf[2]; // Offset 2 = UCHAR ReportID en HIDP_BUTTON_CAPS
                    return;
                }
            }
        }

        public static bool TryParseFnState(CachedRawDevice dev, IntPtr reportPtr, uint reportLength, out bool fnPressed)
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
            _reloadMsgId = RegisterWindowMessage("MagicKeyboardSuite_ReloadSettings");
            CreateHandle(new CreateParams());
            AppleHidRawInput.RegisterDefaultAppleInputs(Handle, _registeredPages, _registeredPairs);
        }

        private CachedRawDevice GetOrCacheDevice(IntPtr hDevice, uint rawType)
        {
            if (_deviceCache.TryGetValue(hDevice, out CachedRawDevice existing))
            {
                return existing;
            }

            CachedRawDevice inspected = AppleHidRawInput.InspectDevice(hDevice, rawType);
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
                existing.Dispose();
                _deviceCache.Remove(hDevice);
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
                            var header = (AppleHidRawInput.RAWINPUTHEADER)Marshal.PtrToStructure(buffer, typeof(AppleHidRawInput.RAWINPUTHEADER));
                            if (header.hDevice != IntPtr.Zero)
                            {
                                if (header.dwType == AppleHidRawInput.RIM_TYPEKEYBOARD)
                                {
                                    CachedRawDevice dev = GetOrCacheDevice(header.hDevice, header.dwType);
                                    if (dev != null && !string.IsNullOrEmpty(dev.DeviceName))
                                    {
                                        _ctx.NotifyRawKeyboardDevice(dev.DeviceName);
                                    }
                                }
                                else if (header.dwType == AppleHidRawInput.RIM_TYPEHID)
                                {
                                    CachedRawDevice dev = GetOrCacheDevice(header.hDevice, header.dwType);
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

    internal sealed class FnDiagnosticForm : Form
    {
        private readonly TextBox _logBox;
        private readonly Label _fnStatusLabel;
        private readonly Dictionary<IntPtr, CachedRawDevice> _deviceCache = new Dictionary<IntPtr, CachedRawDevice>();
        private readonly HashSet<ushort> _registeredPages = new HashSet<ushort>();
        private readonly HashSet<uint> _registeredPairs = new HashSet<uint>();

        public FnDiagnosticForm()
        {
            Text = "Magic Keyboard Suite — Diagnóstico HID de la tecla Fn";
            Width = 860;
            Height = 580;
            StartPosition = FormStartPosition.CenterScreen;

            var topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 84,
                Padding = new Padding(12, 10, 12, 8)
            };

            var infoLabel = new Label
            {
                Dock = DockStyle.Top,
                Height = 40,
                Text = "Pulsa la tecla Fn sola varias veces (primero por Bluetooth y luego por cable USB), y después Fn + Retroceso y Fn + F12.\r\nSi aparecen eventos [HID] al pulsar Fn, Windows sí entrega la tecla en modo usuario; si no aparece nada, kbdhid.sys la bloquea."
            };

            var btnRow = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 32,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };

            var btnCopy = new Button { Text = "Copiar registro", Width = 120, Height = 26 };
            btnCopy.Click += (s, e) =>
            {
                try { Clipboard.SetText(_logBox.Text); } catch { }
            };

            var btnRescan = new Button { Text = "Reescanear dispositivos", Width = 160, Height = 26 };
            btnRescan.Click += (s, e) =>
            {
                ResetCache();
                AppleHidRawInput.RegisterConnectedAppleHidCollections(Handle, _registeredPages, _registeredPairs);
                DumpConnectedAppleDevices();
            };

            var btnClear = new Button { Text = "Limpiar", Width = 80, Height = 26 };
            btnClear.Click += (s, e) => _logBox.Clear();

            _fnStatusLabel = new Label
            {
                AutoSize = true,
                Padding = new Padding(12, 6, 0, 0),
                Font = new Font(Font, FontStyle.Bold),
                Text = "Estado Fn detectado: —"
            };

            btnRow.Controls.Add(btnCopy);
            btnRow.Controls.Add(btnRescan);
            btnRow.Controls.Add(btnClear);
            btnRow.Controls.Add(_fnStatusLabel);

            topPanel.Controls.Add(infoLabel);
            topPanel.Controls.Add(btnRow);

            _logBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Both,
                WordWrap = false,
                Font = new Font("Consolas", 9.5f)
            };

            Controls.Add(_logBox);
            Controls.Add(topPanel);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            AppleHidRawInput.RegisterDefaultAppleInputs(Handle, _registeredPages, _registeredPairs);
            DumpConnectedAppleDevices();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            ResetCache();
            base.OnFormClosed(e);
        }

        private void ResetCache()
        {
            foreach (CachedRawDevice dev in _deviceCache.Values)
            {
                dev.Dispose();
            }
            _deviceCache.Clear();
        }

        private void AppendLog(string line)
        {
            if (_logBox.TextLength > 120000)
            {
                _logBox.Clear();
            }
            _logBox.AppendText($"[{DateTime.Now:HH:mm:ss.fff}] {line}\r\n");
        }

        private void DumpConnectedAppleDevices()
        {
            AppendLog("=== Dispositivos Raw Input de Apple detectados ===");
            List<CachedRawDevice> devices = AppleHidRawInput.EnumerateDevices(onlyApple: true);
            if (devices.Count == 0)
            {
                AppendLog("(Ningún dispositivo Apple conectado ahora mismo)");
            }
            else
            {
                foreach (CachedRawDevice dev in devices)
                {
                    try
                    {
                        string kind = dev.Type == AppleHidRawInput.RIM_TYPEKEYBOARD ? "KEYBOARD" : "HID";
                        string fnInfo = dev.HasFnCap
                            ? $"FnCap=SI (Page=0x{dev.FnUsagePage:X4}, ReportId=0x{dev.FnReportId:X2})"
                            : "FnCap=NO";
                        AppendLog($"  {kind} VID=0x{dev.VendorId:X4} PID=0x{dev.ProductId:X4} Page=0x{dev.UsagePage:X4} Usage=0x{dev.Usage:X4} {fnInfo}");
                        AppendLog($"    Path: {dev.DeviceName}");
                    }
                    finally
                    {
                        dev.Dispose();
                    }
                }
            }
            AppendLog("=== Esperando pulsaciones (pulsa Fn sola, Fn+Retroceso, Fn+F12)... ===");
        }

        private CachedRawDevice GetOrCacheDevice(IntPtr hDevice, uint rawType)
        {
            if (_deviceCache.TryGetValue(hDevice, out CachedRawDevice existing))
            {
                return existing;
            }
            CachedRawDevice inspected = AppleHidRawInput.InspectDevice(hDevice, rawType);
            if (inspected != null)
            {
                _deviceCache[hDevice] = inspected;
            }
            return inspected;
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == AppleHidRawInput.WM_INPUT_DEVICE_CHANGE)
            {
                int change = m.WParam.ToInt32();
                AppendLog(change == AppleHidRawInput.GIDC_ARRIVAL
                    ? "[DEVICE] Dispositivo HID conectado — reescaneando colecciones Apple..."
                    : "[DEVICE] Dispositivo HID desconectado");
                if (change == AppleHidRawInput.GIDC_ARRIVAL)
                {
                    AppleHidRawInput.RegisterConnectedAppleHidCollections(Handle, _registeredPages, _registeredPairs);
                    DumpConnectedAppleDevices();
                }
                else if (_deviceCache.TryGetValue(m.LParam, out CachedRawDevice removed))
                {
                    removed.Dispose();
                    _deviceCache.Remove(m.LParam);
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
                            var header = (AppleHidRawInput.RAWINPUTHEADER)Marshal.PtrToStructure(buffer, typeof(AppleHidRawInput.RAWINPUTHEADER));
                            if (header.hDevice != IntPtr.Zero)
                            {
                                CachedRawDevice dev = GetOrCacheDevice(header.hDevice, header.dwType);
                                if (dev != null && dev.IsApple)
                                {
                                    if (header.dwType == AppleHidRawInput.RIM_TYPEKEYBOARD && dwSize >= headerSize + Marshal.SizeOf(typeof(AppleHidRawInput.RAWKEYBOARD)))
                                    {
                                        var kb = (AppleHidRawInput.RAWKEYBOARD)Marshal.PtrToStructure(
                                            IntPtr.Add(buffer, (int)headerSize),
                                            typeof(AppleHidRawInput.RAWKEYBOARD));
                                        string dir = (kb.Flags & 0x01) != 0 ? "UP" : "DOWN";
                                        AppendLog($"[KBD] VK=0x{kb.VKey:X2} MakeCode=0x{kb.MakeCode:X2} Flags=0x{kb.Flags:X2} ({dir})");
                                    }
                                    else if (header.dwType == AppleHidRawInput.RIM_TYPEHID && dwSize >= headerSize + 8)
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
                                                byte[] bytes = new byte[sizeHid];
                                                Marshal.Copy(reportPtr, bytes, 0, (int)sizeHid);
                                                string hex = BitConverter.ToString(bytes);
                                                string usages = AppleHidRawInput.DescribeActiveUsages(dev.PreparsedData, reportPtr, sizeHid);
                                                string fnNote = "";
                                                if (AppleHidRawInput.TryParseFnState(dev, reportPtr, sizeHid, out bool fnPressed))
                                                {
                                                    fnNote = fnPressed ? " => FN=PULSADA" : " => FN=SOLTADA";
                                                    _fnStatusLabel.Text = fnPressed ? "Estado Fn detectado: PULSADA" : "Estado Fn detectado: suelta";
                                                }
                                                AppendLog($"[HID] Page=0x{dev.UsagePage:X4} Usage=0x{dev.Usage:X4} Bytes=[{hex}] Usages=({usages}){fnNote}");
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
    }
}
