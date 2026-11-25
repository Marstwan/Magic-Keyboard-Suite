using System;
using System.Threading;
using System.Windows.Forms;
using MagicKeyboardSuite.Diagnostics;
using MagicKeyboardSuite.Shared;

namespace MagicKeyboardSuite
{
    internal static class Program
    {
        private static Mutex? _mutex;

        [STAThread]
        private static void Main(string[]? args)
        {
            bool isAutoStart = false;
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
                    if (string.Equals(arg, "--autostart", StringComparison.OrdinalIgnoreCase))
                    {
                        isAutoStart = true;
                    }
                }
            }

            // Si fue invocado por HKLM\...\Run al iniciar sesión pero el usuario desactivó "Abrir al iniciar sesión" en HKCU, salir en silencio.
            if (isAutoStart && !MagicKeyboardContext.IsLaunchAtLoginEnabled())
            {
                return;
            }

            _mutex = new Mutex(true, AppleHardwareMatcher.AgentMutexName, out bool createdNew);
            if (!createdNew)
            {
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MagicKeyboardContext());
        }
    }
}
