using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Runtime.InteropServices; // Interopservices
using WassControlSys.Models;

namespace WassControlSys.Core
{
    public class SystemMaintenanceService : ISystemMaintenanceService
    {
        [DllImport("psapi.dll")]
        private static extern int EmptyWorkingSet(IntPtr hwProc);

        [DllImport("shell32.dll")]
        static extern int SHEmptyRecycleBin(IntPtr hwnd, string pszRootPath, RecycleFlags dwFlags);

        [Flags]
        enum RecycleFlags : int
        {
            SHERB_NOCONFIRMATION = 0x00000001,  // Don't ask for confirmation
            SHERB_NOPROGRESSUI = 0x00000002,    // Don't display a progress box
            SHERB_NOSOUND = 0x00000004          // Don't play recycle sound
        }

        private static readonly string[] TempDirectories = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.InternetCache), // legacy IE cache
            Path.GetTempPath(),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Temp"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp"),
            // Browser caches
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Google\\Chrome\\User Data\\Default\\Cache"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Mozilla\\Firefox\\Profiles"), // Needs deeper search
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft\\Edge\\User Data\\Default\\Cache"),
            // Windows Update temp files
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "SoftwareDistribution\\Download"),
        };

        private static readonly string[] TempFilePatterns = new[] { "*.tmp", "*.temp", "*.log", "*.bak", "*.old", "*.gid", "*.chk" };

        public async Task<CleanResult> CleanTemporaryFilesAsync(CleaningOptions options)
        {
            return await Task.Run(() => CleanTemporaryFiles(options));
        }

        public CleanResult CleanTemporaryFiles(CleaningOptions options)
        {
            var result = new CleanResult();
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Conditional cleaning based on options
            if (options.CleanSystemTemp)
            {
                foreach (var baseDir in TempDirectories)
                {
                    if (string.IsNullOrWhiteSpace(baseDir) || !Directory.Exists(baseDir)) continue;
                    if (!visited.Add(baseDir)) continue;

                    try
                    {
                        result = Aggregate(result, CleanDirectory(baseDir));
                    }
                    catch
                    {
                        // ignore per directory errors to continue
                    }
                }

                // Also try per-user recent temp dirs under users profiles
                var usersDir = Path.Combine(Path.GetPathRoot(Environment.SystemDirectory)!, "Users");
                if (Directory.Exists(usersDir))
                {
                    foreach (var user in Directory.GetDirectories(usersDir))
                    {
                        foreach (var suffix in new[] { "AppData\\Local\\Temp", "AppData\\Local\\Microsoft\\Windows\\INetCache", "AppData\\Local\\Microsoft\\Windows\\INetCookies" })
                        {
                            var dir = Path.Combine(user, suffix);
                            if (Directory.Exists(dir) && visited.Add(dir))
                            {
                                try { result = Aggregate(result, CleanDirectory(dir)); } catch { }
                            }
                        }
                    }
                }
            }

            if (options.CleanRecycleBin)
            {
                result = Aggregate(result, CleanRecycleBin()); // Clean Recycle Bin
            }

            // For browser cache, we need to be more specific. Current TempDirectories include some,
            // but a more thorough cleaning would involve more targeted paths and potentially stopping browsers.
            // For now, assume general temp directory cleaning covers some browser cache.
            // A dedicated browser cache cleaning method could be added later.
            return result;
        }

        private CleanResult CleanRecycleBin()
        {
            var result = new CleanResult();
            try
            {
                SHEmptyRecycleBin(IntPtr.Zero, null, RecycleFlags.SHERB_NOCONFIRMATION | RecycleFlags.SHERB_NOPROGRESSUI | RecycleFlags.SHERB_NOSOUND);
                result.Notes += "Papelera de reciclaje vaciada.";
                // We cannot easily get bytes freed for recycle bin, so we'll just report success.
            }
            catch (Exception ex)
            {
                // Log and continue, do not fail entire cleaning
                // _log?.Error($"Error cleaning Recycle Bin: {ex.Message}");
                result.FilesFailed++; // Indicate an issue
                result.Notes += $"Error al vaciar papelera: {ex.Message}";
            }
            return result;
        }

        public async Task<CleanResult> OptimizeMemoryAsync()
        {
             return await Task.Run(() => OptimizeMemory());
        }

        public CleanResult OptimizeMemory()
        {
            try 
            {
                // We will just execute the optimization.
                
                // We will just execute the optimization.
                var processes = Process.GetProcesses();
                foreach (var p in processes)
                {
                    try 
                    {
                        if (!p.HasExited)
                        {
                            EmptyWorkingSet(p.Handle);
                        }
                    } 
                    catch { } // Ignore access denied etc
                }

                // As a placeholder for bytes freed, we can't easily calculate it without checking before/after global mem.
                // We'll return 0 bytes but set a success message.
                return new CleanResult { BytesFreed = 0, Notes = "Procesos optimizados." };
            }
            catch (Exception ex)
            {
                 return new CleanResult { FilesFailed = 1, Notes = ex.Message };
            }
        }

        private CleanResult Aggregate(CleanResult r1, CleanResult r2)
        {
            return new CleanResult
            {
                FilesDeleted = r1.FilesDeleted + r2.FilesDeleted,
                FoldersDeleted = r1.FoldersDeleted + r2.FoldersDeleted,
                BytesFreed = r1.BytesFreed + r2.BytesFreed,
                FilesFailed = r1.FilesFailed + r2.FilesFailed,
                FoldersFailed = r1.FoldersFailed + r2.FoldersFailed
            };
        }

        private CleanResult CleanDirectory(string dir)
        {
            var result = new CleanResult();

            try
            {
                foreach (var pattern in TempFilePatterns)
                {
                    foreach (var file in SafeEnumerateFiles(dir, pattern, SearchOption.AllDirectories))
                    {
                        try
                        {
                            var fi = new FileInfo(file);
                            long size = fi.Exists ? fi.Length : 0;
                            File.SetAttributes(file, FileAttributes.Normal);
                            File.Delete(file);
                            result.FilesDeleted++;
                            result.BytesFreed += size;
                        }
                        catch
                        {
                            result.FilesFailed++;
                        }
                    }
                }

                // Remove empty directories
                foreach (var sub in SafeEnumerateDirectories(dir, "*", SearchOption.AllDirectories).OrderByDescending(d => d.Length))
                {
                    try
                    {
                        if (Directory.Exists(sub) && !Directory.EnumerateFileSystemEntries(sub).Any())
                        {
                            Directory.Delete(sub, false);
                            result.FoldersDeleted++;
                        }
                    }
                    catch
                    {
                        result.FoldersFailed++;
                    }
                }
            }
            catch
            {
                // non-fatal
            }

            return result;
        }

        private static IEnumerable<string> SafeEnumerateFiles(string path, string searchPattern, SearchOption option)
        {
            var pending = new Stack<string>();
            pending.Push(path);
            while (pending.Count > 0)
            {
                string current = pending.Pop();
                IEnumerable<string> files = Enumerable.Empty<string>();
                try { files = Directory.EnumerateFiles(current, searchPattern); } catch { }
                foreach (var f in files) yield return f;
                if (option == SearchOption.AllDirectories)
                {
                    IEnumerable<string> dirs = Enumerable.Empty<string>();
                    try { dirs = Directory.EnumerateDirectories(current); } catch { }
                    foreach (var d in dirs) pending.Push(d);
                }
            }
        }

        private static IEnumerable<string> SafeEnumerateDirectories(string path, string searchPattern, SearchOption option)
        {
            var pending = new Stack<string>();
            pending.Push(path);
            while (pending.Count > 0)
            {
                string current = pending.Pop();
                IEnumerable<string> dirs = Enumerable.Empty<string>();
                try { dirs = Directory.EnumerateDirectories(current, searchPattern); } catch { }
                foreach (var d in dirs)
                {
                    yield return d;
                    if (option == SearchOption.AllDirectories)
                    {
                        pending.Push(d);
                    }
                }
            }
        }

        public bool IsAdministrator()
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        public ProcessLaunchResult LaunchSystemFileChecker()
        {
            // sfc /scannow must be elevated; we open an elevated console if not admin
            return LaunchElevated("cmd.exe", "/c sfc /scannow");
        }

        public ProcessLaunchResult LaunchDISMHealthRestore()
        {
            // DISM restore health
            return LaunchElevated("cmd.exe", "/c DISM /Online /Cleanup-Image /RestoreHealth");
        }

        public ProcessLaunchResult LaunchCHKDSK()
        {
            // Schedule chkdsk on next boot for system drive
            string sysDrive = Path.GetPathRoot(Environment.SystemDirectory)!;
            return LaunchElevated("cmd.exe", $"/c chkdsk {sysDrive.TrimEnd('\\')} /F /R");
        }

        private ProcessLaunchResult LaunchElevated(string fileName, string arguments)
        {
            // Note: Redirecting streams requires UseShellExecute = false.
            // This means 'Verb = "runas"' won't work directly to elevate.
            // For commands requiring elevation with output capture, a different approach (e.g.,
            // launching powershell with Start-Process -Verb RunAs and then capturing its output)
            // or ensuring the main app is already elevated would be needed.
            // For now, we capture output without automatic elevation via 'runas'.
            // Commands like SFC/DISM will still require the user to launch the app as admin.

            Process p = null;
            string standardOutput = string.Empty;
            string standardError = string.Empty;
            int? exitCode = null;

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    UseShellExecute = false, // Required to redirect streams
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true, // Don't show a command window
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                p = Process.Start(psi);
                if (p == null)
                {
                    return new ProcessLaunchResult { Started = false, Message = "No se pudo iniciar el proceso." };
                }

                standardOutput = p.StandardOutput.ReadToEnd();
                standardError = p.StandardError.ReadToEnd();
                p.WaitForExit();
                exitCode = p.ExitCode;

                return new ProcessLaunchResult
                {
                    Started = true,
                    ExitCode = exitCode,
                    Message = $"Proceso completado. Código de salida: {exitCode}.",
                    StandardOutput = standardOutput,
                    StandardError = standardError
                };
            }
            catch (Exception ex)
            {
                return new ProcessLaunchResult { Started = false, Message = $"Error al iniciar o ejecutar el proceso: {ex.Message}", StandardError = ex.ToString() };
            }
            finally
            {
                p?.Dispose();
            }
        }
    }
}
