using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using MagicKeyboardSuite.Hid;

namespace MagicKeyboardSuite.Diagnostics
{
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

            _logBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Both,
                WordWrap = false,
                Font = new Font("Consolas", 9.5f)
            };

            var btnCopy = new Button { Text = "Copiar registro", Width = 120, Height = 26 };
            btnCopy.Click += (_, _) =>
            {
                try { Clipboard.SetText(_logBox.Text); } catch { }
            };

            var btnRescan = new Button { Text = "Reescanear dispositivos", Width = 160, Height = 26 };
            btnRescan.Click += (_, _) =>
            {
                ResetCache();
                AppleHidRawInput.RegisterConnectedAppleHidCollections(Handle, _registeredPages, _registeredPairs);
                DumpConnectedAppleDevices();
            };

            var btnClear = new Button { Text = "Limpiar", Width = 80, Height = 26 };
            btnClear.Click += (_, _) => _logBox.Clear();

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
                            var header = (AppleHidRawInput.RAWINPUTHEADER)Marshal.PtrToStructure(buffer, typeof(AppleHidRawInput.RAWINPUTHEADER))!;
                            if (header.hDevice != IntPtr.Zero)
                            {
                                CachedRawDevice? dev = GetOrCacheDevice(header.hDevice, header.dwType);
                                if (dev != null && dev.IsApple)
                                {
                                    if (header.dwType == AppleHidRawInput.RIM_TYPEKEYBOARD && dwSize >= headerSize + Marshal.SizeOf(typeof(AppleHidRawInput.RAWKEYBOARD)))
                                    {
                                        var kb = (AppleHidRawInput.RAWKEYBOARD)Marshal.PtrToStructure(
                                            IntPtr.Add(buffer, (int)headerSize),
                                            typeof(AppleHidRawInput.RAWKEYBOARD))!;
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
