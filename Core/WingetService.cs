using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
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
                    var psi = new ProcessStartInfo("winget", "upgrade")
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

                        var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        string? headerLine = lines.FirstOrDefault(l => l.Contains("Id") && (l.Contains("Version") || l.Contains("Versión")));
                        if (headerLine == null) return apps;

                        int idIndex = headerLine.IndexOf("Id");
                        int versionIndex = headerLine.IndexOf("Version");
                        if (versionIndex == -1) versionIndex = headerLine.IndexOf("Versión");

                        int availableIndex = headerLine.IndexOf("Available");
                        if (availableIndex == -1) availableIndex = headerLine.IndexOf("Disponible");
                        
                        int sourceIndex = headerLine.IndexOf("Source");
                        if (sourceIndex == -1) sourceIndex = headerLine.IndexOf("Origen");

                        if (idIndex == -1 || versionIndex == -1 || availableIndex == -1 || sourceIndex == -1) return apps;
                        
                        bool headerPassed = false;
                        foreach (var line in lines)
                        {
                            if (!headerPassed)
                            {
                                if (line.Trim().StartsWith("---")) headerPassed = true;
                                continue;
                            }

                            if (string.IsNullOrWhiteSpace(line)) continue;

                            try
                            {
                                // Asegurar que la línea tenga longitud suficiente antes de Substring
                                string name = line.Length > idIndex ? line.Substring(0, idIndex).Trim() : line.Trim();
                                string id = "";
                                if (line.Length > idIndex)
                                {
                                    int length = (line.Length > versionIndex ? versionIndex : line.Length) - idIndex;
                                    id = line.Substring(idIndex, length).Trim();
                                }
                                
                                string version = "";
                                if (line.Length > versionIndex)
                                {
                                    int length = (line.Length > availableIndex ? availableIndex : line.Length) - versionIndex;
                                    version = line.Substring(versionIndex, length).Trim();
                                }

                                string availableVersion = "";
                                if (line.Length > availableIndex)
                                {
                                    int length = (line.Length > sourceIndex ? sourceIndex : line.Length) - availableIndex;
                                    availableVersion = line.Substring(availableIndex, length).Trim();
                                }

                                string source = line.Length > sourceIndex ? line.Substring(sourceIndex).Trim() : "";

                                if (!string.IsNullOrEmpty(id))
                                {
                                    apps.Add(new WingetApp
                                    {
                                        Name = name,
                                        Id = id,
                                        CurrentVersion = version,
                                        AvailableVersion = availableVersion,
                                        Source = source
                                    });
                                }
                            }
                            catch (Exception ex)
                            {
                                _log.Warn($"Error parseando línea de winget: '{line}'. Error: {ex.Message}");
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

        public async Task<bool> UpdateAppAsync(string appId, IProgress<(int, string)> progress)
        {
            try
            {
                progress?.Report((0, "Iniciando actualización..."));
                var psi = new ProcessStartInfo("winget", $"upgrade --id {appId} --accept-source-agreements --accept-package-agreements")
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8,
                    Verb = "runas"
                };

                using (var process = new Process { StartInfo = psi })
                {
                    var stage = UpdateStage.None;

                    process.OutputDataReceived += (sender, args) =>
                    {
                        if (string.IsNullOrWhiteSpace(args.Data)) return;

                        _log.Info($"[Winget] {args.Data}");
                        string line = args.Data;

                        if (line.Contains("Downloading", StringComparison.OrdinalIgnoreCase))
                        {
                            stage = UpdateStage.Downloading;
                        }
                        else if (line.Contains("Installing", StringComparison.OrdinalIgnoreCase))
                        {
                            stage = UpdateStage.Installing;
                        }

                        var match = Regex.Match(line, @"(\d{1,3})%");
                        if (match.Success && int.TryParse(match.Groups[1].Value, out int percentage))
                        {
                            string message = stage == UpdateStage.Installing ? "Instalando..." : "Descargando...";
                            progress?.Report((percentage, $"{message} {percentage}%"));
                        }
                        else // Report status messages even without percentage
                        {
                            if (stage == UpdateStage.Downloading && line.Contains("Successfully verified", StringComparison.OrdinalIgnoreCase))
                            {
                                progress?.Report((100, "Paquete verificado."));
                            }
                            else if (stage == UpdateStage.None && line.Contains("Found", StringComparison.OrdinalIgnoreCase))
                            {
                                progress?.Report((0, "Aplicación encontrada..."));
                            }
                        }
                    };

                    process.ErrorDataReceived += (sender, args) =>
                    {
                        if (args.Data != null) _log.Error($"[Winget Error] {args.Data}");
                    };

                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    await process.WaitForExitAsync();

                    if (process.ExitCode == 0)
                    {
                        progress?.Report((100, "Actualización completada."));
                        return true;
                    }
                    else
                    {
                        _log.Warn($"Winget finalizó con código de salida: {process.ExitCode}");
                        progress?.Report((100, "Error en la actualización."));
                        // Código 1978335198 puede aparecer en algunas instalaciones exitosas.
                        // Código 9 podría requerir atención, pero a veces es solo una advertencia.
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error($"Error actualizando app {appId}", ex);
                progress?.Report((100, "Error crítico."));
                return false;
            }
        }

        private enum UpdateStage { None, Downloading, Installing }

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
