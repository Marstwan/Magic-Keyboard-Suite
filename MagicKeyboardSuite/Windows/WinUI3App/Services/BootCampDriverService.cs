using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace MagicKeyboardSuite.WinUI3.Services
{
    public sealed record BootCampLayoutDriver(
        string Klid,
        string DllName,
        string LayoutText,
        string LayoutId,
        string ComponentId);

    public sealed class BootCampDriverService
    {
        public static readonly IReadOnlyList<BootCampLayoutDriver> Drivers = new[]
        {
            new BootCampLayoutDriver("a000040a", "SpanishA.dll", "Español / Català (Apple)",            "00c5", "C3364C7C44BC444A88A50459135D35B5"),
            new BootCampLayoutDriver("a0020409", "IntlEngA.dll", "United States-International (Apple)", "00d0", "241A34D0-06DB-405e-8B4E-8CA2FC34D1C7"),
            new BootCampLayoutDriver("a0000409", "USA.dll",      "United States (Apple)",               "00d1", "4E82857C983B44E5A9352052127677D5"),
            new BootCampLayoutDriver("a0000809", "BritishA.dll", "United Kingdom (Apple)",              "00c0", "1A4D378083AD454BB4FE02F208614EB6"),
            new BootCampLayoutDriver("a000040c", "FrenchA.dll",  "Français (Apple)",                    "00c2", "2ECD3C77364749B18E910F9196B420FA"),
            new BootCampLayoutDriver("a0000407", "GermanA.dll",  "Deutsch (Apple)",                     "00c3", "B616E2191BF048D4A554E5C6BE224AB4"),
            new BootCampLayoutDriver("a0000410", "ItalianA.dll", "Italiano (Apple)",                    "00c4", "6401AAA6058F431181B445C26BEF22D9"),
            new BootCampLayoutDriver("a0000416", "PortuguA.dll", "Português (Apple)",                   "00ce", "326773935C8C4597B0738FE2084D44AD"),
            new BootCampLayoutDriver("a0000413", "DutchA.dll",   "Nederlands (Apple)",                  "00c1", "3844B95343FB43D68E9695D6E88F016E"),
            new BootCampLayoutDriver("a000041d", "SwedishA.dll", "Svenska (Apple)",                     "00c7", "8CC8067A1BFF4A0FAD38708DE4CD4BF1"),
            new BootCampLayoutDriver("a0000406", "DanishA.dll",  "Dansk (Apple)",                       "00cc", "C3996498F423440FB9CE2732A821E7D9"),
            new BootCampLayoutDriver("a0000414", "NorwayA.dll",  "Norsk (Apple)",                       "00c9", "74BE397ABD8143E4960D38111394D1A3"),
            new BootCampLayoutDriver("a000040b", "FinnishA.dll", "Suomi (Apple)",                       "00cb", "ECE9937799D242F5AE0CAA446EDEDC62"),
            new BootCampLayoutDriver("a000100c", "SwissA.dll",   "Swiss (Apple)",                       "00c6", "CE4C7E2419DE400B8A553E1A5C3DCD04"),
            new BootCampLayoutDriver("a0000813", "BelgiumA.dll", "Belgian (Apple)",                     "00cd", "D70C1682E8F24ED4B5B70AAD37B1BA42"),
            new BootCampLayoutDriver("a0000415", "PolishA.dll",  "Polski (Apple)",                      "00cf", "D3D2841618E34D09ABBCA0DA34A60FAE"),
            new BootCampLayoutDriver("a0000419", "RussianA.dll", "Russian (Apple)",                     "00c8", "B0F62A69BE9446488ED502E800DBC36C")
        };

        private static readonly HashSet<string> KnownDllNames = new(
            Drivers.Select(d => d.DllName),
            StringComparer.OrdinalIgnoreCase);

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

        public Task<int> ImportFromFolderAsync(string rootFolder)
        {
            return Task.Run(() =>
            {
                string tempDir = Path.Combine(Path.GetTempPath(), "MKS_BootCamp_" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(tempDir);
                try
                {
                    var foundDlls = SearchAndExtractDriversFromDirectory(rootFolder, tempDir);
                    if (foundDlls.Count == 0)
                    {
                        return 0;
                    }
                    return InstallDriversElevated(foundDlls, tempDir);
                }
                finally
                {
                    try { Directory.Delete(tempDir, true); } catch { }
                }
            });
        }

        public Task<int> ImportFromZipAsync(string zipFilePath)
        {
            return Task.Run(() =>
            {
                string tempDir = Path.Combine(Path.GetTempPath(), "MKS_BootCamp_" + Guid.NewGuid().ToString("N"));
                string zipExtractDir = Path.Combine(tempDir, "from_zip");
                Directory.CreateDirectory(zipExtractDir);
                try
                {
                    using (var archive = ZipFile.OpenRead(zipFilePath))
                    {
                        foreach (var entry in archive.Entries)
                        {
                            if (string.IsNullOrEmpty(entry.Name)) continue;

                            if (KnownDllNames.Contains(entry.Name) ||
                                string.Equals(entry.Name, "BootCamp.msi", StringComparison.OrdinalIgnoreCase))
                            {
                                string destPath = Path.Combine(zipExtractDir, entry.Name);
                                entry.ExtractToFile(destPath, overwrite: true);
                            }
                        }
                    }

                    var foundDlls = SearchAndExtractDriversFromDirectory(zipExtractDir, tempDir);
                    if (foundDlls.Count == 0)
                    {
                        return 0;
                    }
                    return InstallDriversElevated(foundDlls, tempDir);
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

            if (foundDlls.Count < Drivers.Count && bootCampMsiPath != null && File.Exists(bootCampMsiPath))
            {
                string cabOutDir = Path.Combine(tempDir, "cab_extracted");
                Directory.CreateDirectory(cabOutDir);

                ExtractEmbeddedCabFromMsi(bootCampMsiPath, cabOutDir);

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

        private static void ExtractEmbeddedCabFromMsi(string msiPath, string outputDir)
        {
            try
            {
                byte[] data = File.ReadAllBytes(msiPath);
                byte[] sig = { (byte)'M', (byte)'S', (byte)'C', (byte)'F', 0, 0, 0, 0 };

                int offset = IndexOfBytes(data, sig);
                if (offset >= 0 && offset + 12 <= data.Length)
                {
                    int cabSize = BitConverter.ToInt32(data, offset + 8);
                    if (cabSize > 0 && offset + cabSize <= data.Length)
                    {
                        string cabPath = Path.Combine(outputDir, "BootCampEmbedded.cab");
                        using (var fs = new FileStream(cabPath, FileMode.Create, FileAccess.Write))
                        {
                            fs.Write(data, offset, cabSize);
                        }

                        var psi = new ProcessStartInfo
                        {
                            FileName = "expand.exe",
                            Arguments = $"\"{cabPath}\" -F:*A.dll \"{outputDir}\"",
                            CreateNoWindow = true,
                            UseShellExecute = false
                        };
                        using var proc = Process.Start(psi);
                        proc?.WaitForExit(15000);
                    }
                }
            }
            catch { }
        }

        private static int IndexOfBytes(byte[] buffer, byte[] pattern)
        {
            int limit = buffer.Length - pattern.Length;
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
                if (match) return i;
            }
            return -1;
        }

        private static int InstallDriversElevated(Dictionary<string, string> dllFiles, string tempDir)
        {
            string stageDir = Path.Combine(tempDir, "stage_dlls");
            Directory.CreateDirectory(stageDir);

            var matchedDrivers = new List<BootCampLayoutDriver>();
            foreach (var drv in Drivers)
            {
                if (dllFiles.TryGetValue(drv.DllName, out string? srcPath) && File.Exists(srcPath))
                {
                    File.Copy(srcPath, Path.Combine(stageDir, drv.DllName), overwrite: true);
                    matchedDrivers.Add(drv);
                }
            }

            if (matchedDrivers.Count == 0) return 0;

            var sb = new StringBuilder();
            sb.AppendLine("$ErrorActionPreference = 'Stop'");
            sb.AppendLine("$sys32 = Join-Path $env:SystemRoot 'System32'");
            foreach (var drv in matchedDrivers)
            {
                string src = Path.Combine(stageDir, drv.DllName).Replace("'", "''");
                string text = drv.LayoutText.Replace("'", "''");
                sb.AppendLine($"Copy-Item -LiteralPath '{src}' -Destination (Join-Path $sys32 '{drv.DllName}') -Force");
                sb.AppendLine($"$regPath = 'HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Keyboard Layouts\\{drv.Klid}'");
                sb.AppendLine("if (-not (Test-Path $regPath)) { New-Item -Path $regPath -Force | Out-Null }");
                sb.AppendLine($"Set-ItemProperty -Path $regPath -Name 'Layout Text' -Value '{text}' -Type String");
                sb.AppendLine($"Set-ItemProperty -Path $regPath -Name 'Layout File' -Value '{drv.DllName}' -Type String");
                sb.AppendLine($"Set-ItemProperty -Path $regPath -Name 'Layout Id' -Value '{drv.LayoutId}' -Type String");
                sb.AppendLine($"Set-ItemProperty -Path $regPath -Name 'Layout Component ID' -Value '{drv.ComponentId}' -Type String");
            }

            string scriptPath = Path.Combine(tempDir, "install_bootcamp_layouts.ps1");
            File.WriteAllText(scriptPath, sb.ToString(), Encoding.UTF8);

            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File \"{scriptPath}\"",
                Verb = "runas",
                UseShellExecute = true
            };

            using var proc = Process.Start(psi);
            proc?.WaitForExit(30000);
            if (proc != null && proc.ExitCode == 0)
            {
                return matchedDrivers.Count;
            }
            return 0;
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
