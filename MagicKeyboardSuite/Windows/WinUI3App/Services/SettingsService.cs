using System;
using System.Runtime.InteropServices;
using MagicKeyboardSuite.Shared;
using MagicKeyboardSuite.WinUI3.Models;
using Microsoft.Win32;

namespace MagicKeyboardSuite.WinUI3.Services
{
    public sealed class SettingsService
    {
        private readonly uint _reloadMsgId;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr LoadKeyboardLayout(string pwszKLID, uint Flags);

        [DllImport("user32.dll")]
        private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, ref IntPtr pvParam, uint fWinIni);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern uint RegisterWindowMessage(string lpString);

        [DllImport("user32.dll")]
        private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        private const uint WM_INPUTLANGCHANGEREQUEST = 0x0050;
        private const uint SPI_SETDEFAULTINPUTLANG = 0x005A;
        private const uint KLF_ACTIVATE = 0x00000001;
        private const uint KLF_SUBSTITUTE_OK = 0x00000002;

        public DeviceFilterMode DeviceFilter { get; set; } = DeviceFilterMode.Apple;
        public bool RememberReleased { get; set; } = true;
        public int ModifierMode { get; set; } = 0;
        public bool SwapIsoKeys { get; set; } = true;
        public bool LeftOptionAsAltGr { get; set; } = true;
        public bool OnlyAppleHardware { get; set; } = true;
        public bool MacNavigationShortcuts { get; set; } = true;
        public bool MediaKeysDefault { get; set; } = true;
        public bool RightOptionAsFn { get; set; } = false;
        public string PreferredKlid { get; set; } = AppleLayoutDefinition.All[0].Klid;
        public string AppLanguage { get; set; } = "auto";
        public bool HasSeenOnboarding { get; set; } = false;

        public SettingsService()
        {
            _reloadMsgId = RegisterWindowMessage("MagicKeyboardSuite_ReloadSettings");
            Load();
        }

        public void Load()
        {
            try
            {
                using var key = Registry.CurrentUser.CreateSubKey(SettingsKeys.RegistryPath);
                string? rawFilter = Convert.ToString(key.GetValue(SettingsKeys.DeviceFilter, "apple"));
                DeviceFilter = DeviceFilterModeExtensions.FromSettingValue(rawFilter);

                RememberReleased = Convert.ToInt32(key.GetValue(SettingsKeys.RememberReleased, 1)) != 0;
                ModifierMode = Math.Clamp(Convert.ToInt32(key.GetValue(SettingsKeys.ModifierMode, 0)), 0, 2);
                SwapIsoKeys = Convert.ToInt32(key.GetValue(SettingsKeys.SwapIsoKeys, 1)) != 0;
                LeftOptionAsAltGr = Convert.ToInt32(key.GetValue(SettingsKeys.LeftOptionAsAltGr, 1)) != 0;
                OnlyAppleHardware = Convert.ToInt32(key.GetValue(SettingsKeys.OnlyAppleHardware, 1)) != 0;
                MacNavigationShortcuts = Convert.ToInt32(key.GetValue(SettingsKeys.MacNavigationShortcuts, 1)) != 0;
                MediaKeysDefault = Convert.ToInt32(key.GetValue(SettingsKeys.MediaKeysDefault, 1)) != 0;
                RightOptionAsFn = Convert.ToInt32(key.GetValue(SettingsKeys.RightOptionAsFn, 0)) != 0;

                string? klid = Convert.ToString(key.GetValue(SettingsKeys.PreferredKlid, AppleLayoutDefinition.All[0].Klid));
                PreferredKlid = string.IsNullOrWhiteSpace(klid) ? AppleLayoutDefinition.All[0].Klid : klid!;

                string? lang = Convert.ToString(key.GetValue(SettingsKeys.AppLanguage, "auto"))?.ToLowerInvariant();
                AppLanguage = (lang == "ca" || lang == "es" || lang == "en") ? lang! : "auto";
                L10n.OverrideLanguage = AppLanguage == "auto" ? null : AppLanguage;

                HasSeenOnboarding = Convert.ToInt32(key.GetValue(SettingsKeys.HasSeenOnboarding, 0)) != 0;
            }
            catch { }
        }

        public void Save()
        {
            try
            {
                using var key = Registry.CurrentUser.CreateSubKey(SettingsKeys.RegistryPath);
                key.SetValue(SettingsKeys.DeviceFilter, DeviceFilter.ToSettingValue(), RegistryValueKind.String);
                key.SetValue(SettingsKeys.RememberReleased, RememberReleased ? 1 : 0, RegistryValueKind.DWord);
                key.SetValue(SettingsKeys.ModifierMode, ModifierMode, RegistryValueKind.DWord);
                key.SetValue(SettingsKeys.SwapIsoKeys, SwapIsoKeys ? 1 : 0, RegistryValueKind.DWord);
                key.SetValue(SettingsKeys.LeftOptionAsAltGr, LeftOptionAsAltGr ? 1 : 0, RegistryValueKind.DWord);
                key.SetValue(SettingsKeys.OnlyAppleHardware, OnlyAppleHardware ? 1 : 0, RegistryValueKind.DWord);
                key.SetValue(SettingsKeys.MacNavigationShortcuts, MacNavigationShortcuts ? 1 : 0, RegistryValueKind.DWord);
                key.SetValue(SettingsKeys.MediaKeysDefault, MediaKeysDefault ? 1 : 0, RegistryValueKind.DWord);
                key.SetValue(SettingsKeys.RightOptionAsFn, RightOptionAsFn ? 1 : 0, RegistryValueKind.DWord);
                key.SetValue(SettingsKeys.PreferredKlid, PreferredKlid, RegistryValueKind.String);
                key.SetValue(SettingsKeys.AppLanguage, AppLanguage, RegistryValueKind.String);
                key.SetValue(SettingsKeys.HasSeenOnboarding, HasSeenOnboarding ? 1 : 0, RegistryValueKind.DWord);

                L10n.OverrideLanguage = AppLanguage == "auto" ? null : AppLanguage;

                if (!string.IsNullOrWhiteSpace(PreferredKlid))
                {
                    ActivateKeyboardLayout(PreferredKlid);
                }

                if (_reloadMsgId != 0)
                {
                    PostMessage((IntPtr)0xFFFF, _reloadMsgId, IntPtr.Zero, IntPtr.Zero);
                }
            }
            catch { }
        }

        public static void ActivateKeyboardLayout(string preferredKlid)
        {
            try
            {
                string klidToLoad = preferredKlid;
                using (var layoutKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Keyboard Layouts\" + preferredKlid, false))
                {
                    if (layoutKey == null && preferredKlid.Length == 8 && (preferredKlid[0] == 'a' || preferredKlid[0] == 'A'))
                    {
                        klidToLoad = "0" + preferredKlid.Substring(1);
                    }
                }

                EnsurePreloadedLayout(klidToLoad);

                IntPtr hkl = LoadKeyboardLayout(klidToLoad, KLF_ACTIVATE | KLF_SUBSTITUTE_OK);
                if (hkl != IntPtr.Zero)
                {
                    SystemParametersInfo(SPI_SETDEFAULTINPUTLANG, 0, ref hkl, 0x02);
                    IntPtr fg = GetForegroundWindow();
                    if (fg != IntPtr.Zero)
                    {
                        PostMessage(fg, WM_INPUTLANGCHANGEREQUEST, IntPtr.Zero, hkl);
                    }
                    PostMessage((IntPtr)0xFFFF, WM_INPUTLANGCHANGEREQUEST, IntPtr.Zero, hkl);
                }
            }
            catch { }
        }

        private static void EnsurePreloadedLayout(string klid)
        {
            try
            {
                using var preload = Registry.CurrentUser.CreateSubKey(@"Keyboard Layout\Preload");
                if (preload == null) return;

                foreach (string name in preload.GetValueNames())
                {
                    string existing = Convert.ToString(preload.GetValue(name)) ?? "";
                    if (string.Equals(existing, klid, StringComparison.OrdinalIgnoreCase))
                    {
                        return;
                    }
                }

                for (int i = 1; i <= 32; i++)
                {
                    string slot = i.ToString();
                    if (preload.GetValue(slot) == null)
                    {
                        preload.SetValue(slot, klid, RegistryValueKind.String);
                        return;
                    }
                }
            }
            catch { }
        }
    }
}
