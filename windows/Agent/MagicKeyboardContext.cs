using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using MagicKeyboardSuite.Hid;
using MagicKeyboardSuite.Shared;
using Microsoft.Win32;

namespace MagicKeyboardSuite
{
    /// <summary>
    /// Contexto principal del servicio en segundo plano en la bandeja del sistema de Windows.
    /// Coordina los ajustes del Registro, el activador de disposiciones, RawInput HID y el motor WH_KEYBOARD_LL.
    /// </summary>
    public sealed class MagicKeyboardContext : ApplicationContext
    {
        private const string RegPath = SettingsKeys.RegistryPath;
        private const string RunRegPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string RunValueName = "MagicKeyboardSuite";

        public string PreferredKlid { get; private set; } = AppleLayoutDefinition.All[0].Klid;

        private readonly KeyboardHookEngine _hookEngine;
        private readonly RawInputReceiverWindow _rawWindow;
        private NotifyIcon? _trayIcon;
        private Font? _trayBoldFont;
        private long _lastRegistryHeartbeatTicks;

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        public MagicKeyboardContext()
        {
            _hookEngine = new KeyboardHookEngine();
            EnsureFirstRunAppleLayoutConfigured();
            LoadSettings();
            ActivatePreferredAppleLayout();

            _rawWindow = new RawInputReceiverWindow(this);
            _hookEngine.Start();
            SystemEvents.SessionSwitch += OnSessionSwitch;

            BuildTrayMenu();
        }

        private void OnSessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            // Al bloquear/desbloquear con Win+L Windows no entrega WM_KEYUP al hook de usuario
            _hookEngine.ResetModifierState();
        }

        private void EnsureFirstRunAppleLayoutConfigured()
        {
            try
            {
                using RegistryKey? key = Registry.CurrentUser.CreateSubKey(RegPath);
                if (key == null) return;

                bool alreadyConfigured = Convert.ToInt32(key.GetValue(SettingsKeys.LayoutAutoConfigured, 0)) != 0;
                string existingKlid = Convert.ToString(key.GetValue(SettingsKeys.PreferredKlid, "")) ?? "";

                if (!alreadyConfigured || string.IsNullOrWhiteSpace(existingKlid))
                {
                    string matchedKlid = AppleHardwareMatcher.MatchAppleLayoutKlidForCulture(CultureInfo.CurrentUICulture.Name);

                    if (string.IsNullOrWhiteSpace(existingKlid))
                    {
                        key.SetValue(SettingsKeys.PreferredKlid, matchedKlid, RegistryValueKind.String);
                    }
                    key.SetValue(SettingsKeys.LayoutAutoConfigured, 1, RegistryValueKind.DWord);
                    KeyboardLayoutActivator.EnsurePreloadedLayout(matchedKlid);
                }
            }
            catch
            {
            }
        }

        public void LoadSettings()
        {
            try
            {
                using RegistryKey? key = Registry.CurrentUser.CreateSubKey(RegPath);
                if (key == null) return;

                _hookEngine.ModifierMode = Math.Min(2, Math.Max(0, Convert.ToInt32(key.GetValue(SettingsKeys.ModifierMode, 0))));
                _hookEngine.SwapIsoKeys = Convert.ToInt32(key.GetValue(SettingsKeys.SwapIsoKeys, 1)) != 0;
                _hookEngine.LeftOptionAsAltGr = Convert.ToInt32(key.GetValue(SettingsKeys.LeftOptionAsAltGr, 1)) != 0;
                _hookEngine.MacNavigationShortcuts = Convert.ToInt32(key.GetValue(SettingsKeys.MacNavigationShortcuts, 1)) != 0;
                _hookEngine.MediaKeysDefault = Convert.ToInt32(key.GetValue(SettingsKeys.MediaKeysDefault, 1)) != 0;
                _hookEngine.RightOptionAsFn = Convert.ToInt32(key.GetValue(SettingsKeys.RightOptionAsFn, 0)) != 0;
                _hookEngine.OnlyAppleHardware = Convert.ToInt32(key.GetValue(SettingsKeys.OnlyAppleHardware, 1)) != 0;

                string klid = Convert.ToString(key.GetValue(SettingsKeys.PreferredKlid, AppleLayoutDefinition.All[0].Klid)) ?? "";
                PreferredKlid = string.IsNullOrWhiteSpace(klid) ? AppleLayoutDefinition.All[0].Klid : klid;

                string lang = (Convert.ToString(key.GetValue(SettingsKeys.AppLanguage, "auto")) ?? "auto").ToLowerInvariant();
                L10n.OverrideLanguage = (lang == "ca" || lang == "es" || lang == "en") ? lang : null;
            }
            catch
            {
            }
        }

        public void ActivatePreferredAppleLayout()
        {
            KeyboardLayoutActivator.Activate(PreferredKlid);
        }

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
                string[] candidates =
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
            catch
            {
            }
        }

        private void BuildTrayMenu()
        {
            _trayBoldFont = new Font(SystemFonts.MenuFont ?? SystemFonts.DefaultFont, FontStyle.Bold);
            var menu = new ContextMenuStrip();
            menu.Opening += (_, _) => PopulateTrayMenu(menu);
            PopulateTrayMenu(menu);

            _trayIcon = new NotifyIcon
            {
                Icon = CreateKeyboardIcon(),
                Text = L10n.AppName,
                ContextMenuStrip = menu,
                Visible = true
            };

            _trayIcon.Click += (_, e) =>
            {
                if (e is MouseEventArgs me && me.Button == MouseButtons.Left)
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
            itemOpenGui.Click += (_, _) => LaunchWinUI3App();
            menu.Items.Add(itemOpenGui);

            string layoutName = AppleLayoutDefinition.All[0].Name;
            foreach (var lay in AppleLayoutDefinition.All)
            {
                if (string.Equals(lay.Klid, PreferredKlid, StringComparison.OrdinalIgnoreCase))
                {
                    layoutName = lay.Name;
                    break;
                }
            }
            menu.Items.Add(new ToolStripMenuItem(L10n.FooterCurrentLayout(layoutName)) { Enabled = false });
            menu.Items.Add(new ToolStripSeparator());

            var itemLaunchLogin = new ToolStripMenuItem(L10n.TrayLaunchAtLogin)
            {
                Checked = IsLaunchAtLoginEnabled(),
                CheckOnClick = true
            };
            itemLaunchLogin.Click += (_, _) => SetLaunchAtLogin(itemLaunchLogin.Checked);
            menu.Items.Add(itemLaunchLogin);

            menu.Items.Add(new ToolStripSeparator());

            var itemExit = new ToolStripMenuItem(L10n.TrayQuit);
            itemExit.Click += (_, _) => ShutdownAgent();
            menu.Items.Add(itemExit);
        }

        private void ShutdownAgent()
        {
            SystemEvents.SessionSwitch -= OnSessionSwitch;
            if (_trayIcon != null)
            {
                _trayIcon.Visible = false;
                _trayIcon.Icon?.Dispose();
                _trayIcon.Dispose();
                _trayIcon = null;
            }
            _trayBoldFont?.Dispose();
            _hookEngine.Dispose();
            _rawWindow.Dispose();
            Application.Exit();
        }

        public static bool IsLaunchAtLoginEnabled()
        {
            try
            {
                using (RegistryKey? mks = Registry.CurrentUser.OpenSubKey(RegPath, false))
                {
                    object? pref = mks?.GetValue(SettingsKeys.LaunchAtLogin);
                    if (pref != null)
                    {
                        return Convert.ToInt32(pref) != 0;
                    }
                }

                using (RegistryKey? cuRun = Registry.CurrentUser.OpenSubKey(RunRegPath, false))
                {
                    if (cuRun?.GetValue(RunValueName) != null) return true;
                }

                using (RegistryKey? lmRun = Registry.LocalMachine.OpenSubKey(RunRegPath, false))
                {
                    return lmRun?.GetValue(RunValueName) != null;
                }
            }
            catch
            {
                return true;
            }
        }

        public static void SetLaunchAtLogin(bool enabled)
        {
            try
            {
                using (RegistryKey? mks = Registry.CurrentUser.CreateSubKey(RegPath))
                {
                    mks?.SetValue(SettingsKeys.LaunchAtLogin, enabled ? 1 : 0, RegistryValueKind.DWord);
                }

                bool hasMachineRun = false;
                using (RegistryKey? lmRun = Registry.LocalMachine.OpenSubKey(RunRegPath, false))
                {
                    hasMachineRun = lmRun?.GetValue(RunValueName) != null;
                }

                using (RegistryKey? cuRun = Registry.CurrentUser.CreateSubKey(RunRegPath))
                {
                    if (enabled && !hasMachineRun)
                    {
                        cuRun?.SetValue(RunValueName, $"\"{Application.ExecutablePath}\" --autostart", RegistryValueKind.String);
                    }
                    else
                    {
                        cuRun?.DeleteValue(RunValueName, false);
                    }
                }
            }
            catch
            {
            }
        }

        private static Icon CreateKeyboardIcon()
        {
            using Bitmap bmp = new Bitmap(32, 32);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);
                using (LinearGradientBrush bg = new LinearGradientBrush(
                    new Rectangle(2, 4, 28, 24),
                    Color.FromArgb(0, 120, 212),
                    Color.FromArgb(70, 50, 210),
                    45f))
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
                using Icon temp = Icon.FromHandle(hIcon);
                return (Icon)temp.Clone();
            }
            finally
            {
                DestroyIcon(hIcon);
            }
        }

        public void SetAppleFnState(bool pressed)
        {
            _hookEngine.SetAppleFnState(pressed);
            if (!pressed)
            {
                // Si se desconectó el dispositivo HID, asegurarse de limpiar posibles modificadores colgados
            }
        }

        public void OnAppleDeviceRemoved()
        {
            _hookEngine.ResetModifierState();
        }

        public void NotifyRawKeyboardDevice(string deviceName)
        {
            if (string.IsNullOrEmpty(deviceName)) return;
            bool isApple = AppleHardwareMatcher.IsAppleDevicePath(deviceName);
            _hookEngine.SetLastKeyWasApple(isApple);

            if (isApple)
            {
                long nowTicks = DateTime.UtcNow.Ticks;
                if (nowTicks - _lastRegistryHeartbeatTicks > TimeSpan.FromSeconds(4).Ticks)
                {
                    _lastRegistryHeartbeatTicks = nowTicks;
                    try
                    {
                        using RegistryKey? key = Registry.CurrentUser.CreateSubKey(RegPath);
                        key?.SetValue("LastAppleKeyUtcTicks", nowTicks, RegistryValueKind.QWord);
                        key?.SetValue("LastAppleDevicePath", deviceName, RegistryValueKind.String);
                    }
                    catch
                    {
                    }
                }
            }
        }
    }
}
