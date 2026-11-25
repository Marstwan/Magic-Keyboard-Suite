using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;

namespace MagicKeyboardSuite
{
    /// <summary>
    /// Motor de traducción en tiempo real de teclado (WH_KEYBOARD_LL + SendInput) para Apple Magic Keyboard:
    /// - Intercambio físico ISO ([º ª \] &lt;-&gt; [&lt; &gt;])
    /// - Command (⌘) como atajos estilo Mac (incluyendo ⌘+Tab persistente y respeto de Shift físico)
    /// - Option (⌥) izquierdo como AltGr dinámico para las 17 disposiciones oficiales
    /// - Option (⌥) derecho opcional como tecla Fn
    /// - Paso transparente en ventanas elevadas (UIPI) cuando el agente no corre como Administrador
    /// </summary>
    internal sealed class KeyboardHookEngine : IDisposable
    {
        public int ModifierMode { get; set; } = 0;
        public bool SwapIsoKeys { get; set; } = true;
        public bool LeftOptionAsAltGr { get; set; } = true;
        public bool MacNavigationShortcuts { get; set; } = true;
        public bool MediaKeysDefault { get; set; } = true;
        public bool RightOptionAsFn { get; set; } = false;
        public bool OnlyAppleHardware { get; set; } = true;

        private IntPtr _hookId = IntPtr.Zero;
        private readonly LowLevelKeyboardProc _hookProc;
        private readonly bool _isSelfElevated;

        private IntPtr _cachedFgHwnd = IntPtr.Zero;
        private bool _cachedFgElevated;

        private bool _lCtrlDown;
        private bool _rCtrlDown;
        private bool _lAltDown;
        private bool _rAltDown;
        private bool _lWinDown;
        private bool _rWinDown;
        private bool _lShiftDown;
        private bool _rShiftDown;
        private bool _cmdTabActive;
        private volatile bool _fnDown;
        private volatile bool _lastKeyWasApple = true;

        private static readonly IntPtr InjectedMarker = new IntPtr(0x4D4B5332);

        private static readonly HashSet<int> CmdToCtrlVks = new HashSet<int>
        {
            0x41, 0x42, 0x43, 0x44, 0x46, 0x47, 0x48, 0x4B, 0x4C, 0x4E, 0x4F,
            0x50, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58, 0x59, 0x5A,
            0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x30,
            0xBB, 0xBD, 0xBC, 0xBE, 0xBF, 0x08, 0x25, 0x26, 0x27, 0x28
        };

        private static readonly HashSet<int> FastAltGrVks = new HashSet<int>
        {
            0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x45,
            0xDC, 0xBA, 0xBB, 0xBF, 0xDE, 0xC0
        };

        public KeyboardHookEngine()
        {
            _hookProc = HookCallback;
            _isSelfElevated = CheckSelfElevated();
        }

        public void Start()
        {
            if (_hookId != IntPtr.Zero) return;
            using Process curProcess = Process.GetCurrentProcess();
            using ProcessModule? curModule = curProcess.MainModule;
            IntPtr hMod = curModule != null ? GetModuleHandle(curModule.ModuleName) : IntPtr.Zero;
            _hookId = SetWindowsHookEx(WH_KEYBOARD_LL, _hookProc, hMod, 0);
        }

        public void SetAppleFnState(bool pressed)
        {
            _fnDown = pressed;
            if (pressed)
            {
                _lastKeyWasApple = true;
            }
        }

        public void SetLastKeyWasApple(bool isApple)
        {
            _lastKeyWasApple = isApple;
        }

        public void ResetModifierState()
        {
            if (_cmdTabActive)
            {
                SendKeyVk(VK_LMENU, 0x38, false, keyUp: true);
                _cmdTabActive = false;
            }
            _lCtrlDown = false;
            _rCtrlDown = false;
            _lAltDown = false;
            _rAltDown = false;
            _lWinDown = false;
            _rWinDown = false;
            _lShiftDown = false;
            _rShiftDown = false;
            _fnDown = false;
        }

        private void SyncPhysicalModifiers(int currentVk)
        {
            if (_lShiftDown && currentVk != VK_LSHIFT && (GetAsyncKeyState(VK_LSHIFT) & 0x8000) == 0) _lShiftDown = false;
            if (_rShiftDown && currentVk != VK_RSHIFT && (GetAsyncKeyState(VK_RSHIFT) & 0x8000) == 0) _rShiftDown = false;
            if (ModifierMode != 1)
            {
                if (_lCtrlDown && currentVk != VK_LCONTROL && (GetAsyncKeyState(VK_LCONTROL) & 0x8000) == 0) _lCtrlDown = false;
                if (_rCtrlDown && currentVk != VK_RCONTROL && (GetAsyncKeyState(VK_RCONTROL) & 0x8000) == 0) _rCtrlDown = false;
            }
            if (_lAltDown && currentVk != VK_LMENU && (GetAsyncKeyState(VK_LMENU) & 0x8000) == 0) _lAltDown = false;
        }

        private static bool CheckSelfElevated()
        {
            try
            {
                using WindowsIdentity identity = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Si este agente corre sin elevar y la ventana en primer plano es de Administrador,
        /// Windows bloquea SendInput por UIPI. En ese caso no debemos suprimir la tecla original.
        /// </summary>
        private bool IsForegroundBlockedByUipi()
        {
            if (_isSelfElevated) return false;

            IntPtr fg = GetForegroundWindow();
            if (fg == IntPtr.Zero) return false;
            if (fg == _cachedFgHwnd) return _cachedFgElevated;

            _cachedFgHwnd = fg;
            _cachedFgElevated = false;

            try
            {
                GetWindowThreadProcessId(fg, out uint pid);
                if (pid != 0)
                {
                    IntPtr hProc = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, pid);
                    if (hProc != IntPtr.Zero)
                    {
                        try
                        {
                            if (OpenProcessToken(hProc, TOKEN_QUERY, out IntPtr hToken))
                            {
                                try
                                {
                                    int elevation = 0;
                                    if (GetTokenInformation(hToken, TokenElevation, ref elevation, sizeof(int), out _))
                                    {
                                        _cachedFgElevated = elevation != 0;
                                    }
                                }
                                finally
                                {
                                    CloseHandle(hToken);
                                }
                            }
                        }
                        finally
                        {
                            CloseHandle(hProc);
                        }
                    }
                }
            }
            catch
            {
            }

            return _cachedFgElevated;
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                KBDLLHOOKSTRUCT kb = (KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT))!;
                if (kb.dwExtraInfo == InjectedMarker)
                {
                    return CallNextHookEx(_hookId, nCode, wParam, lParam);
                }

                if (OnlyAppleHardware && !_lastKeyWasApple)
                {
                    return CallNextHookEx(_hookId, nCode, wParam, lParam);
                }

                if (IsForegroundBlockedByUipi())
                {
                    return CallNextHookEx(_hookId, nCode, wParam, lParam);
                }

                int msg = wParam.ToInt32();
                bool isDown = (msg == WM_KEYDOWN || msg == WM_SYSKEYDOWN);
                bool isUp = (msg == WM_KEYUP || msg == WM_SYSKEYUP);
                int vk = (int)kb.vkCode;
                int sc = (int)kb.scanCode;
                bool isExtended = (kb.flags & LLKHF_EXTENDED) != 0;

                SyncPhysicalModifiers(vk);

                if (vk == VK_LSHIFT) _lShiftDown = isDown;
                if (vk == VK_RSHIFT) _rShiftDown = isDown;

                // 1. Intercambio físico ISO: [º ª \] (SC 0x56) <-> [< >] (SC 0x29)
                if (SwapIsoKeys && !isExtended)
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

                // 2. Intercambio total Command <-> Control (ambos lados)
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
                    if (vk == VK_RCONTROL)
                    {
                        _rWinDown = isDown;
                        SendKeyVk(VK_RWIN, 0x5C, true, isUp);
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
                        if (!isDown && !_rWinDown && _cmdTabActive)
                        {
                            SendKeyVk(VK_LMENU, 0x38, false, keyUp: true);
                            _cmdTabActive = false;
                        }
                        if (ModifierMode == 0) return (IntPtr)1;
                    }
                    if (vk == VK_RWIN)
                    {
                        _rWinDown = isDown;
                        if (!isDown && !_lWinDown && _cmdTabActive)
                        {
                            SendKeyVk(VK_LMENU, 0x38, false, keyUp: true);
                            _cmdTabActive = false;
                        }
                        if (ModifierMode == 0) return (IntPtr)1;
                    }
                }

                if (vk == VK_LMENU) _lAltDown = isDown;
                if (vk == VK_RMENU) _rAltDown = isDown;

                bool cmdDown = _lWinDown || _rWinDown;
                bool ctrlDown = _lCtrlDown || _rCtrlDown;
                bool physicalShiftDown = _lShiftDown || _rShiftDown;
                bool optDown = _lAltDown || (!RightOptionAsFn && _rAltDown);
                bool effectiveFn = _fnDown || (RightOptionAsFn && _rAltDown);

                // 3. Option Izquierdo (LAlt) como AltGr en cualquiera de las 17 disposiciones de Apple
                if (LeftOptionAsAltGr && _lAltDown && !ctrlDown && !cmdDown && LayoutHasAltGrSymbol(vk, sc, physicalShiftDown))
                {
                    if (isDown)
                    {
                        SendAltGrKey((ushort)vk, (ushort)sc, physicalShiftDown);
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
                            SendModifiedKey(0x08, 0x0E, false, ctrl: true, alt: false, physicalShiftDown, isUp);
                            return (IntPtr)1;
                        }
                    }

                    if (effectiveFn || (cmdDown && ModifierMode == 0 && !_cmdTabActive))
                    {
                        if (vk == 0x25) { SendKeyVk(0x24, 0x47, true, isUp); return (IntPtr)1; }
                        if (vk == 0x27) { SendKeyVk(0x23, 0x4F, true, isUp); return (IntPtr)1; }
                        if (vk == 0x26) { SendKeyVk(0x21, 0x49, true, isUp); return (IntPtr)1; }
                        if (vk == 0x28) { SendKeyVk(0x22, 0x51, true, isUp); return (IntPtr)1; }
                    }

                    if (_lAltDown && !ctrlDown && !cmdDown && (vk == 0x25 || vk == 0x27))
                    {
                        SendModifiedKey((ushort)vk, (ushort)sc, true, ctrl: true, alt: false, physicalShiftDown, isUp);
                        return (IntPtr)1;
                    }
                }

                // 5. Atajos de Command (⌘) en Modo Mac (0)
                if (ModifierMode == 0 && cmdDown)
                {
                    // ⌘ + Q -> Alt + F4
                    if (vk == 0x51)
                    {
                        SendModifiedKey(0x73, 0x3E, false, ctrl: false, alt: true, physicalShiftAlreadyDown: true, isUp);
                        return (IntPtr)1;
                    }
                    // ⌘ + Tab -> Mantiene Alt pulsado mientras ⌘ siga pulsado para navegar entre múltiples apps
                    if (vk == 0x09)
                    {
                        if (isDown && !_cmdTabActive)
                        {
                            _cmdTabActive = true;
                            SendKeyVk(VK_LMENU, 0x38, false, keyUp: false);
                        }
                        SendKeyVk(0x09, 0x0F, false, isUp);
                        return (IntPtr)1;
                    }
                    // ⌘ + Espacio -> Búsqueda de Windows (Win + S)
                    if (vk == 0x20)
                    {
                        if (isDown) SendWinCombo(0x53);
                        return (IntPtr)1;
                    }
                    if (!_cmdTabActive && CmdToCtrlVks.Contains(vk))
                    {
                        SendModifiedKey((ushort)vk, (ushort)sc, isExtended, ctrl: true, alt: false, physicalShiftDown, isUp);
                        return (IntPtr)1;
                    }
                }

                // 6. Multimedia F1–F12
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

        private static bool LayoutHasAltGrSymbol(int vk, int sc, bool shift)
        {
            if (FastAltGrVks.Contains(vk)) return true;

            // Solo consultar teclas imprimibles alfanuméricas u OEM (evita teclas de función, flechas o modificadores)
            bool isPrintableCandidate = (vk >= 0x30 && vk <= 0x5A) || (vk >= 0xBA && vk <= 0xE2);
            if (!isPrintableCandidate) return false;

            try
            {
                IntPtr fg = GetForegroundWindow();
                uint tid = fg != IntPtr.Zero ? GetWindowThreadProcessId(fg, out _) : 0;
                IntPtr hkl = GetKeyboardLayout(tid);

                byte[] keyState = new byte[256];
                keyState[0x11] = 0x80; // VK_CONTROL
                keyState[0x12] = 0x80; // VK_MENU (Ctrl + Alt == AltGr)
                if (shift) keyState[0x10] = 0x80; // VK_SHIFT

                StringBuilder sb = new StringBuilder(8);
                // uFlags = 0x04 evita alterar el estado de acentos muertos (dead keys) en Windows 10 1607+ / 11
                int rc = ToUnicodeEx((uint)vk, (uint)sc, keyState, sb, sb.Capacity, 0x04, hkl);
                if (rc < 0) return true; // Dead key de tercer nivel (p. ej. ~ en algunas disposiciones europeas)
                if (rc > 0 && sb.Length > 0 && !char.IsControl(sb[0]))
                {
                    return true;
                }
            }
            catch
            {
            }

            return false;
        }

        private static void ExecuteAppleMediaKey(int vk)
        {
            switch (vk)
            {
                case 0x72: SendWinCombo(0x09); break; // Mission Control -> Win + Tab
                case 0x73: SendWinCombo(0x53); break; // Spotlight -> Win + S
                case 0x74: SendWinCombo(0x48); break; // Dictation -> Win + H
                case 0x76: TapSingleVk(0xB1); break;  // Prev Track
                case 0x77: TapSingleVk(0xB3); break;  // Play / Pause
                case 0x78: TapSingleVk(0xB0); break;  // Next Track
                case 0x79: TapSingleVk(0xAD); break;  // Mute
                case 0x7A: TapSingleVk(0xAE); break;  // Volume Down
                case 0x7B: TapSingleVk(0xAF); break;  // Volume Up
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

        private static void SendAltGrKey(ushort vk, ushort sc, bool physicalShiftAlreadyDown)
        {
            SendKeyVk(VK_LCONTROL, 0x1D, false, false);
            SendKeyVk(VK_RMENU, 0x38, true, false);
            SendKeyVk(vk, sc, false, false);
            SendKeyVk(vk, sc, false, true);
            SendKeyVk(VK_RMENU, 0x38, true, true);
            SendKeyVk(VK_LCONTROL, 0x1D, false, true);
            // Nota: si el usuario mantiene Shift físico pulsado, Windows ya lo aplica sin tocar su estado.
            _ = physicalShiftAlreadyDown;
        }

        private static void SendModifiedKey(ushort vk, ushort sc, bool extended, bool ctrl, bool alt, bool physicalShiftAlreadyDown, bool isUp)
        {
            // No inyectamos ni soltamos VK_LSHIFT sintético cuando el usuario mantiene Shift físicamente,
            // evitando desincronizar atajos repetidos como ⌘+Shift+Z o ⌘+Shift+T.
            _ = physicalShiftAlreadyDown;
            if (!isUp)
            {
                if (ctrl) SendKeyVk(VK_LCONTROL, 0x1D, false, false);
                if (alt) SendKeyVk(VK_LMENU, 0x38, false, false);
                SendKeyVk(vk, sc, extended, false);
            }
            else
            {
                SendKeyVk(vk, sc, extended, true);
                if (alt) SendKeyVk(VK_LMENU, 0x38, false, true);
                if (ctrl) SendKeyVk(VK_LCONTROL, 0x1D, false, true);
            }
        }

        public void Dispose()
        {
            ResetModifierState();
            if (_hookId != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookId);
                _hookId = IntPtr.Zero;
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

        private const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;
        private const uint TOKEN_QUERY = 0x0008;
        private const int TokenElevation = 20;

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

        [DllImport("user32.dll")]
        private static extern uint MapVirtualKey(uint uCode, uint uMapType);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern IntPtr GetKeyboardLayout(uint idThread);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int ToUnicodeEx(
            uint wVirtKey,
            uint wScanCode,
            byte[] lpKeyState,
            [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pwszBuff,
            int cchBuff,
            uint wFlags,
            IntPtr dwhkl);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool OpenProcessToken(IntPtr ProcessHandle, uint DesiredAccess, out IntPtr TokenHandle);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool GetTokenInformation(IntPtr TokenHandle, int TokenInformationClass, ref int TokenInformation, int TokenInformationLength, out int ReturnLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);
    }
}
