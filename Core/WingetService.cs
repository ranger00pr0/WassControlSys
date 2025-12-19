using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using WassControlSys.Models;

namespace WassControlSys.Core
{
    public class WingetService : IWingetService
    {
        private readonly ILogService _log;

        public WingetService(ILogService log)
        {
            _log = log;
        }

        public async Task<IEnumerable<WingetApp>> GetUpdatableAppsAsync()
        {
            return await Task.Run(() =>
            {
                var apps = new List<WingetApp>();
                try
                {
                    var psi = new ProcessStartInfo("winget", "list --upgrade-available")
                    {
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        StandardOutputEncoding = System.Text.Encoding.UTF8
                    };

                    using (var process = Process.Start(psi))
                    {
                        if (process == null) return apps;
                        string output = process.StandardOutput.ReadToEnd();
                        process.WaitForExit();

                        // Parse output (skipping headers)
                        var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        bool headerFound = false;

                        foreach (var line in lines)
                        {
                            if (!headerFound)
                            {
                                if (line.Contains("Nombre") || line.Contains("Name")) headerFound = true;
                                continue;
                            }

                            if (line.StartsWith("-")) continue;

                            // Simplistic parser for winget columns
                            // Formato típico: Name [Id] Version Available Source
                            var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length >= 4)
                            {
                                apps.Add(new WingetApp
                                {
                                    Name = parts[0],
                                    Id = parts[1],
                                    CurrentVersion = parts[2],
                                    AvailableVersion = parts[3]
                                });
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _log.Warn($"Winget no disponible o error: {ex.Message}");
                }
                return apps;
            });
        }

        public async Task<bool> UpdateAppAsync(string appId)
        {
             return await Task.Run(() =>
             {
                try
                {
                    var psi = new ProcessStartInfo("winget", $"upgrade --id {appId} --silent")
                    {
                        UseShellExecute = true,
                        Verb = "runas",
                        CreateNoWindow = false
                    };
                    using (var process = Process.Start(psi))
                    {
                        process?.WaitForExit();
                        return process?.ExitCode == 0;
                    }
                }
                catch { return false; }
             });
        }

        public async Task<bool> UpdateAllAppsAsync()
        {
            return await Task.Run(() =>
             {
                try
                {
                    var psi = new ProcessStartInfo("winget", "upgrade --all --silent")
                    {
                        UseShellExecute = true,
                        Verb = "runas",
                        CreateNoWindow = false
                    };
                    using (var process = Process.Start(psi))
                    {
                        process?.WaitForExit();
                        return process?.ExitCode == 0;
                    }
                }
                catch { return false; }
             });
        }
    }
}
