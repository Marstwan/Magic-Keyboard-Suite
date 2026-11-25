#nullable enable
using System;
using System.Runtime.InteropServices;
#if NET5_0_OR_GREATER
using System.Runtime.Versioning;
#endif
using Microsoft.Win32;

namespace MagicKeyboardSuite.Shared
{
    /// <summary>
    /// Activa y registra en Preload la disposición de teclado oficial de Apple (o su equivalente estándar si Boot Camp aún no está instalado).
    /// Compartido entre la app WinUI 3 y el Agente en segundo plano.
    /// </summary>
#if NET5_0_OR_GREATER
    [SupportedOSPlatform("windows")]
#endif
    public static class KeyboardLayoutActivator
    {
        private const uint WM_INPUTLANGCHANGEREQUEST = 0x0050;
        private const uint SPI_SETDEFAULTINPUTLANG = 0x005A;
        private const uint KLF_ACTIVATE = 0x00000001;
        private const uint KLF_SUBSTITUTE_OK = 0x00000002;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr LoadKeyboardLayout(string pwszKLID, uint Flags);

        [DllImport("user32.dll")]
        private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, ref IntPtr pvParam, uint fWinIni);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        public static void Activate(string? preferredKlid)
        {
            if (string.IsNullOrWhiteSpace(preferredKlid)) return;

            try
            {
                string klidToLoad = preferredKlid!;
                using (var layoutKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Keyboard Layouts\" + klidToLoad, false))
                {
                    if (layoutKey == null && klidToLoad.Length == 8 && (klidToLoad[0] == 'a' || klidToLoad[0] == 'A'))
                    {
                        // Si los drivers de Boot Camp aún no están instalados, usar la disposición estándar de Windows equivalente
                        klidToLoad = "0" + klidToLoad.Substring(1);
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
            catch
            {
            }
        }

        public static void EnsurePreloadedLayout(string klid)
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
            catch
            {
            }
        }
    }
}
