using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using MagicKeyboardSuite.Shared;
using Microsoft.Win32;

namespace MagicKeyboardSuite.WinUI3.Services
{
    public enum BootCampImportStatus
    {
        Success = 0,
        NotFound = 1,
        InvalidSignature = 2
    }

    public sealed record BootCampImportResult(BootCampImportStatus Status, int InstalledCount);

    public sealed class BootCampDriverService
    {
        private const long MaxArchiveEntryBytes = 512L * 1024L * 1024L;
        private const int MaxEmbeddedCabBytes = 256 * 1024 * 1024;
        private const long MaxLayoutDllBytes = 16L * 1024L * 1024L;
        private const long MinLayoutDllBytes = 2048L;

        private static readonly HashSet<string> KnownDllNames = new(
            AppleLayoutDefinition.All.Select(d => d.DllName),
            StringComparer.OrdinalIgnoreCase);

        private static readonly Guid WinTrustActionGenericVerifyV2 = new("{00AAC56B-CD44-11d0-8CC2-00C04FC295EE}");

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct WINTRUST_FILE_INFO
        {
            public uint cbStruct;
            public IntPtr pcwszFilePath;
            public IntPtr hFile;
            public IntPtr pgKnownSubject;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct WINTRUST_DATA
        {
            public uint cbStruct;
            public IntPtr pPolicyCallbackData;
            public IntPtr pSIPClientData;
            public uint dwUIChoice;
            public uint fdwRevocationChecks;
            public uint dwUnionChoice;
            public IntPtr pFile;
            public uint dwStateAction;
            public IntPtr hWVTStateData;
            public IntPtr pwszURLReference;
            public uint dwProvFlags;
            public uint dwUIContext;
            public IntPtr pSignatureSettings;
        }

        [DllImport("wintrust.dll", ExactSpelling = true, PreserveSig = true, CharSet = CharSet.Unicode)]
        private static extern int WinVerifyTrust(IntPtr hwnd, [MarshalAs(UnmanagedType.LPStruct)] Guid pgActionID, ref WINTRUST_DATA pWVTData);

        public bool AreDriversInstalled()
        {
            try
            {
                string sys32 = Environment.GetFolderPath(Environment.SpecialFolder.System);
                if (!File.Exists(Path.Combine(sys32, "SpanishA.dll")) ||
                    !File.Exists(Path.Combine(sys32, "USA.dll")))
                {
                    return false;
                }

                using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Keyboard Layouts\a000040a", false);
                return key != null;
            }
            catch
            {
                return false;
            }
        }

        public Task<BootCampImportResult> ImportFromFolderAsync(string rootFolder)
        {
            return Task.Run(() =>
            {
                string runId = Guid.NewGuid().ToString("N");
                string tempDir = Path.Combine(Path.GetTempPath(), "MKS_BootCamp_" + runId);
                Directory.CreateDirectory(tempDir);
                try
                {
                    var foundDlls = SearchAndExtractDriversFromDirectory(rootFolder, tempDir);
                    if (foundDlls.Count == 0)
                    {
                        return new BootCampImportResult(BootCampImportStatus.NotFound, 0);
                    }
                    return InstallDriversElevated(foundDlls, tempDir, runId);
                }
                finally
                {
                    try { Directory.Delete(tempDir, true); } catch { }
                }
            });
        }

        public Task<BootCampImportResult> ImportFromZipAsync(string zipFilePath)
        {
            return Task.Run(() =>
            {
                string runId = Guid.NewGuid().ToString("N");
                string tempDir = Path.Combine(Path.GetTempPath(), "MKS_BootCamp_" + runId);
                string zipExtractDir = Path.Combine(tempDir, "from_zip");
                Directory.CreateDirectory(zipExtractDir);
                try
                {
                    using (var archive = ZipFile.OpenRead(zipFilePath))
                    {
                        foreach (var entry in archive.Entries)
                        {
                            if (string.IsNullOrEmpty(entry.Name)) continue;
                            if (entry.Length <= 0 || entry.Length > MaxArchiveEntryBytes) continue;

                            string safeFileName = Path.GetFileName(entry.Name);
                            if (KnownDllNames.Contains(safeFileName) ||
                                string.Equals(safeFileName, "BootCamp.msi", StringComparison.OrdinalIgnoreCase))
                            {
                                string destPath = Path.GetFullPath(Path.Combine(zipExtractDir, safeFileName));
                                if (!destPath.StartsWith(Path.GetFullPath(zipExtractDir) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                                {
                                    continue;
                                }
                                entry.ExtractToFile(destPath, overwrite: true);
                            }
                        }
                    }

                    var foundDlls = SearchAndExtractDriversFromDirectory(zipExtractDir, tempDir);
                    if (foundDlls.Count == 0)
                    {
                        return new BootCampImportResult(BootCampImportStatus.NotFound, 0);
                    }
                    return InstallDriversElevated(foundDlls, tempDir, runId);
                }
                finally
                {
                    try { Directory.Delete(tempDir, true); } catch { }
                }
            });
        }

        private Dictionary<string, string> SearchAndExtractDriversFromDirectory(string searchRoot, string tempDir)
        {
            var foundDlls = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string? bootCampMsiPath = null;

            foreach (string file in EnumerateFilesSafe(searchRoot))
            {
                string name = Path.GetFileName(file);
                if (KnownDllNames.Contains(name) && !foundDlls.ContainsKey(name))
                {
                    foundDlls[name] = file;
                }
                else if (bootCampMsiPath == null && string.Equals(name, "BootCamp.msi", StringComparison.OrdinalIgnoreCase))
                {
                    bootCampMsiPath = file;
                }
            }

            if (foundDlls.Count < AppleLayoutDefinition.All.Count && bootCampMsiPath != null && File.Exists(bootCampMsiPath))
            {
                string cabOutDir = Path.Combine(tempDir, "cab_extracted");
                Directory.CreateDirectory(cabOutDir);

                ExtractEmbeddedCabFromMsiStreaming(bootCampMsiPath, cabOutDir);

                foreach (string file in EnumerateFilesSafe(cabOutDir))
                {
                    string name = Path.GetFileName(file);
                    if (KnownDllNames.Contains(name) && !foundDlls.ContainsKey(name))
                    {
                        foundDlls[name] = file;
                    }
                }
            }

            return foundDlls;
        }

        /// <summary>
        /// Busca y extrae el gabinete MSCF incrustado en BootCamp.msi por bloques de 64 KB sin cargar todo el MSI en RAM.
        /// </summary>
        private static void ExtractEmbeddedCabFromMsiStreaming(string msiPath, string outputDir)
        {
            try
            {
                byte[] sig = { (byte)'M', (byte)'S', (byte)'C', (byte)'F', 0, 0, 0, 0 };
                using var msiStream = new FileStream(msiPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                long fileLen = msiStream.Length;
                if (fileLen < 64) return;

                long cabOffset = FindSignatureOffset(msiStream, sig);
                if (cabOffset < 0 || cabOffset + 12 > fileLen) return;

                msiStream.Position = cabOffset + 8;
                byte[] lenBytes = new byte[4];
                if (msiStream.Read(lenBytes, 0, 4) != 4) return;

                int cabSize = BitConverter.ToInt32(lenBytes, 0);
                if (cabSize <= 64 || cabSize > MaxEmbeddedCabBytes || cabOffset + cabSize > fileLen)
                {
                    return;
                }

                string cabPath = Path.Combine(outputDir, "BootCampEmbedded.cab");
                msiStream.Position = cabOffset;
                using (var cabOut = new FileStream(cabPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    byte[] copyBuf = new byte[65536];
                    int remaining = cabSize;
                    while (remaining > 0)
                    {
                        int toRead = Math.Min(copyBuf.Length, remaining);
                        int read = msiStream.Read(copyBuf, 0, toRead);
                        if (read <= 0) break;
                        cabOut.Write(copyBuf, 0, read);
                        remaining -= read;
                    }
                }

                string expandExe = Path.Combine(Environment.SystemDirectory, "expand.exe");
                if (!File.Exists(expandExe)) return;

                var psi = new ProcessStartInfo
                {
                    FileName = expandExe,
                    Arguments = $"\"{cabPath}\" -F:*A.dll \"{outputDir}\"",
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                using var proc = Process.Start(psi);
                proc?.WaitForExit(15000);
            }
            catch
            {
            }
        }

        private static long FindSignatureOffset(Stream stream, byte[] pattern)
        {
            const int chunkSize = 65536;
            byte[] buffer = new byte[chunkSize + pattern.Length];
            long basePos = 0;
            int carry = 0;

            stream.Position = 0;
            while (true)
            {
                int bytesRead = stream.Read(buffer, carry, chunkSize);
                if (bytesRead <= 0) break;

                int total = carry + bytesRead;
                int limit = total - pattern.Length;
                for (int i = 0; i <= limit; i++)
                {
                    bool match = true;
                    for (int j = 0; j < pattern.Length; j++)
                    {
                        if (buffer[i + j] != pattern[j])
                        {
                            match = false;
                            break;
                        }
                    }
                    if (match)
                    {
                        return basePos - carry + i;
                    }
                }

                if (total >= pattern.Length - 1)
                {
                    carry = pattern.Length - 1;
                    Buffer.BlockCopy(buffer, total - carry, buffer, 0, carry);
                }
                else
                {
                    carry = total;
                }
                basePos += bytesRead;
            }
            return -1;
        }

        /// <summary>
        /// Verifica que el archivo sea un PE firmado válidamente con Authenticode por Apple Inc. o Microsoft Corporation.
        /// </summary>
        private static bool VerifyTrustedLayoutDll(string filePath, out string sha256Hex)
        {
            sha256Hex = "";
            try
            {
                var info = new FileInfo(filePath);
                if (!info.Exists || info.Length < MinLayoutDllBytes || info.Length > MaxLayoutDllBytes)
                {
                    return false;
                }

                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    int b0 = fs.ReadByte();
                    int b1 = fs.ReadByte();
                    if (b0 != 'M' || b1 != 'Z') return false;

                    fs.Position = 0;
                    using var sha = SHA256.Create();
                    sha256Hex = Convert.ToHexString(sha.ComputeHash(fs));
                }

                if (!VerifyWinTrustSignature(filePath))
                {
                    return false;
                }

                using X509Certificate rawCert = X509Certificate.CreateFromSignedFile(filePath);
                using var cert2 = new X509Certificate2(rawCert);
                string subject = cert2.Subject ?? "";
                bool trustedVendor =
                    subject.IndexOf("Apple Inc.", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    subject.IndexOf("Microsoft Corporation", StringComparison.OrdinalIgnoreCase) >= 0;

                return trustedVendor;
            }
            catch
            {
                return false;
            }
        }

        private static bool VerifyWinTrustSignature(string filePath)
        {
            IntPtr pathPtr = Marshal.StringToCoTaskMemUni(filePath);
            IntPtr fileInfoPtr = IntPtr.Zero;
            try
            {
                var fileInfo = new WINTRUST_FILE_INFO
                {
                    cbStruct = (uint)Marshal.SizeOf<WINTRUST_FILE_INFO>(),
                    pcwszFilePath = pathPtr,
                    hFile = IntPtr.Zero,
                    pgKnownSubject = IntPtr.Zero
                };

                fileInfoPtr = Marshal.AllocCoTaskMem(Marshal.SizeOf<WINTRUST_FILE_INFO>());
                Marshal.StructureToPtr(fileInfo, fileInfoPtr, false);

                var data = new WINTRUST_DATA
                {
                    cbStruct = (uint)Marshal.SizeOf<WINTRUST_DATA>(),
                    dwUIChoice = 2,          // WTD_UI_NONE
                    fdwRevocationChecks = 0, // WTD_REVOKE_NONE (funciona sin conexión a Internet)
                    dwUnionChoice = 1,       // WTD_CHOICE_FILE
                    pFile = fileInfoPtr,
                    dwStateAction = 0,       // WTD_STATEACTION_IGNORE
                    dwProvFlags = 0x00000010 // WTD_CACHE_ONLY_URL_RETRIEVAL
                };

                Guid action = WinTrustActionGenericVerifyV2;
                int hr = WinVerifyTrust(IntPtr.Zero, action, ref data);
                return hr == 0;
            }
            finally
            {
                if (fileInfoPtr != IntPtr.Zero) Marshal.FreeCoTaskMem(fileInfoPtr);
                if (pathPtr != IntPtr.Zero) Marshal.FreeCoTaskMem(pathPtr);
            }
        }

        private static BootCampImportResult InstallDriversElevated(Dictionary<string, string> dllFiles, string tempDir, string runId)
        {
            string stageDir = Path.Combine(tempDir, "stage_dlls");
            Directory.CreateDirectory(stageDir);

            var verifiedDrivers = new List<(AppleLayoutDefinition Driver, string StagedPath, string ExpectedSha256)>();
            foreach (var drv in AppleLayoutDefinition.All)
            {
                if (dllFiles.TryGetValue(drv.DllName, out string? srcPath) && File.Exists(srcPath))
                {
                    string stagedPath = Path.Combine(stageDir, drv.DllName);
                    File.Copy(srcPath, stagedPath, overwrite: true);

                    if (!VerifyTrustedLayoutDll(stagedPath, out string sha256))
                    {
                        return new BootCampImportResult(BootCampImportStatus.InvalidSignature, 0);
                    }
                    verifiedDrivers.Add((drv, stagedPath, sha256));
                }
            }

            if (verifiedDrivers.Count == 0)
            {
                return new BootCampImportResult(BootCampImportStatus.NotFound, 0);
            }

            // El script elevado copia primero las DLL a %SystemRoot%\Temp (protegido contra procesos no elevados),
            // vuelve a verificar tanto el SHA-256 exacto como la firma Authenticode antes de instalarlas en System32,
            // eliminando cualquier ventana de carrera TOCTOU en %TEMP% mientras el aviso UAC está en pantalla.
            var sb = new StringBuilder();
            sb.AppendLine("$ErrorActionPreference = 'Stop'");
            sb.AppendLine("$sys32 = Join-Path $env:SystemRoot 'System32'");
            sb.AppendLine($"$secDir = Join-Path $env:SystemRoot 'Temp\\MKS_Stage_{runId}'");
            sb.AppendLine("New-Item -ItemType Directory -Path $secDir -Force | Out-Null");
            sb.AppendLine("try {");

            foreach (var (drv, stagedPath, expectedSha256) in verifiedDrivers)
            {
                string srcEsc = stagedPath.Replace("'", "''");
                string textEsc = drv.Name.Replace("'", "''");
                sb.AppendLine($"    $secDll = Join-Path $secDir '{drv.DllName}'");
                sb.AppendLine($"    Copy-Item -LiteralPath '{srcEsc}' -Destination $secDll -Force");
                sb.AppendLine($"    if ((Get-FileHash -LiteralPath $secDll -Algorithm SHA256).Hash -ne '{expectedSha256}') {{ throw 'Hash mismatch for {drv.DllName}' }}");
                sb.AppendLine("    $sig = Get-AuthenticodeSignature -LiteralPath $secDll");
                sb.AppendLine("    if ($sig.Status -ne 'Valid') { throw 'Invalid Authenticode signature' }");
                sb.AppendLine($"    Copy-Item -LiteralPath $secDll -Destination (Join-Path $sys32 '{drv.DllName}') -Force");
                sb.AppendLine($"    $regPath = 'HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Keyboard Layouts\\{drv.Klid}'");
                sb.AppendLine("    if (-not (Test-Path $regPath)) { New-Item -Path $regPath -Force | Out-Null }");
                sb.AppendLine($"    Set-ItemProperty -Path $regPath -Name 'Layout Text' -Value '{textEsc}' -Type String");
                sb.AppendLine($"    Set-ItemProperty -Path $regPath -Name 'Layout File' -Value '{drv.DllName}' -Type String");
                sb.AppendLine($"    Set-ItemProperty -Path $regPath -Name 'Layout Id' -Value '{drv.LayoutId}' -Type String");
                sb.AppendLine($"    Set-ItemProperty -Path $regPath -Name 'Layout Component ID' -Value '{drv.ComponentId}' -Type String");
            }

            sb.AppendLine("} finally {");
            sb.AppendLine("    Remove-Item -LiteralPath $secDir -Recurse -Force -ErrorAction SilentlyContinue");
            sb.AppendLine("}");

            string scriptPath = Path.Combine(tempDir, "install_bootcamp_layouts.ps1");
            byte[] scriptBytes = Encoding.UTF8.GetBytes(sb.ToString());
            File.WriteAllBytes(scriptPath, scriptBytes);
            string expectedScriptHash = Convert.ToHexString(SHA256.HashData(scriptBytes));

            string escapedScriptPath = scriptPath.Replace("'", "''");
            string bootstrapCmd =
                "$ErrorActionPreference='Stop';" +
                $"$secPs1=Join-Path $env:SystemRoot 'Temp\\MKS_{runId}.ps1';" +
                $"Copy-Item -LiteralPath '{escapedScriptPath}' -Destination $secPs1 -Force;" +
                "try{" +
                $"if((Get-FileHash -LiteralPath $secPs1 -Algorithm SHA256).Hash -ne '{expectedScriptHash}'){{exit 3}};" +
                "& $secPs1" +
                "}finally{Remove-Item -LiteralPath $secPs1 -Force -ErrorAction SilentlyContinue}";

            string encodedBootstrap = Convert.ToBase64String(Encoding.Unicode.GetBytes(bootstrapCmd));

            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -EncodedCommand {encodedBootstrap}",
                Verb = "runas",
                UseShellExecute = true
            };

            using var proc = Process.Start(psi);
            proc?.WaitForExit(30000);
            if (proc != null && proc.ExitCode == 0)
            {
                return new BootCampImportResult(BootCampImportStatus.Success, verifiedDrivers.Count);
            }
            return new BootCampImportResult(BootCampImportStatus.NotFound, 0);
        }

        private static IEnumerable<string> EnumerateFilesSafe(string root)
        {
            var stack = new Stack<string>();
            stack.Push(root);

            while (stack.Count > 0)
            {
                string current = stack.Pop();
                string[] files = Array.Empty<string>();
                try { files = Directory.GetFiles(current); } catch { }
                foreach (string f in files) yield return f;

                string[] dirs = Array.Empty<string>();
                try { dirs = Directory.GetDirectories(current); } catch { }
                foreach (string d in dirs) stack.Push(d);
            }
        }
    }
}
