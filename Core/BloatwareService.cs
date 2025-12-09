using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Win32; // For Registry access
using WassControlSys.Models;

namespace WassControlSys.Core
{
    public class BloatwareService : IBloatwareService
    {
        private readonly ILogService _log;
        private readonly IDialogService _dialogService; // For warnings/confirmations

        public BloatwareService(ILogService log, IDialogService dialogService)
        {
            _log = log;
            _dialogService = dialogService;
        }

        public async Task<IEnumerable<BloatwareApp>> GetBloatwareAppsAsync()
        {
            return await Task.Run(() =>
            {
                var bloatwareList = new List<BloatwareApp>();

                // Common uninstall registry paths
                string[] uninstallPaths = new string[]
                {
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
                    @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
                };

                foreach (string uninstallPath in uninstallPaths)
                {
                    using (RegistryKey baseKey = Registry.LocalMachine.OpenSubKey(uninstallPath))
                    {
                        if (baseKey != null)
                        {
                            foreach (string subKeyName in baseKey.GetSubKeyNames())
                            {
                                using (RegistryKey subKey = baseKey.OpenSubKey(subKeyName))
                                {
                                    if (subKey != null)
                                    {
                                        var appName = subKey.GetValue("DisplayName")?.ToString();
                                        var publisher = subKey.GetValue("Publisher")?.ToString();
                                        var installLocation = subKey.GetValue("InstallLocation")?.ToString();
                                        var uninstallString = subKey.GetValue("UninstallString")?.ToString();

                                        if (!string.IsNullOrEmpty(appName) && !string.IsNullOrEmpty(uninstallString))
                                        {
                                            // Basic bloatware filtering - this needs refinement
                                            // Example: Filter out known system components or essential Microsoft software
                                            if (IsPotentialBloatware(appName, publisher))
                                            {
                                                bloatwareList.Add(new BloatwareApp
                                                {
                                                    Name = appName,
                                                    Publisher = publisher,
                                                    InstallLocation = installLocation,
                                                    UninstallCommand = uninstallString,
                                                    IsSystemApp = IsWindowsSystemApp(publisher)
                                                });
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                return bloatwareList.OrderBy(a => a.Name);
            });
        }

        public async Task<bool> UninstallBloatwareAppAsync(BloatwareApp app)
        {
            if (app == null || string.IsNullOrEmpty(app.UninstallCommand))
            {
                _log.Warn("Attempted to uninstall a null or invalid bloatware app.");
                return false;
            }

            // Always ask for confirmation before uninstalling
            bool confirm = await _dialogService.ShowConfirmation($"¿Está seguro de que desea desinstalar '{app.Name}'? Esta acción no se puede deshacer.", "Confirmar Desinstalación");
            if (!confirm) return false;

            try
            {
                _log.Info($"Attempting to uninstall bloatware app: {app.Name} with command: {app.UninstallCommand}");
                
                // Uninstall commands can vary wildly (msiexec, setup.exe, custom EXEs)
                // This is a basic attempt to handle common cases.
                // More robust parsing and execution might be needed.
                ProcessStartInfo psi;
                if (app.UninstallCommand.StartsWith("MsiExec.exe", StringComparison.OrdinalIgnoreCase))
                {
                    psi = new ProcessStartInfo("msiexec.exe", app.UninstallCommand.Replace("MsiExec.exe", "").Trim())
                    {
                        UseShellExecute = true,
                        Verb = "runas" // Request UAC elevation
                    };
                }
                else
                {
                    // Attempt to parse the command and arguments
                    string command = app.UninstallCommand;
                    string arguments = "";

                    if (command.Contains(" "))
                    {
                        arguments = command.Substring(command.IndexOf(" ") + 1);
                        command = command.Substring(0, command.IndexOf(" "));
                    }

                    psi = new ProcessStartInfo(command, arguments)
                    {
                        UseShellExecute = true,
                        Verb = "runas" // Request UAC elevation
                    };
                }

                using (Process process = Process.Start(psi))
                {
                    if (process == null)
                    {
                        _log.Error($"Failed to start uninstall process for {app.Name}");
                        await _dialogService.ShowMessage($"No se pudo iniciar el proceso de desinstalación para '{app.Name}'.", "Error de Desinstalación");
                        return false;
                    }
                    await Task.Run(() => process.WaitForExit()); // Wait for the uninstall process to complete
                    _log.Info($"Uninstall process for {app.Name} exited with code {process.ExitCode}");
                    
                    if (process.ExitCode == 0) // Typically 0 means success
                    {
                         await _dialogService.ShowMessage($"'{app.Name}' desinstalado correctamente. Es posible que necesite reiniciar el sistema.", "Desinstalación Completa");
                        return true;
                    }
                    else
                    {
                         await _dialogService.ShowMessage($"La desinstalación de '{app.Name}' falló con código {process.ExitCode}.", "Error de Desinstalación");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error($"Error uninstalling bloatware app {app.Name}", ex);
                await _dialogService.ShowMessage($"Error al desinstalar '{app.Name}': {ex.Message}", "Error de Desinstalación");
                return false;
            }
        }

        private bool IsPotentialBloatware(string appName, string publisher)
        {
            // This is a very basic heuristic and will need significant refinement.
            // Consider expanding with a curated list, or more complex logic.
            if (string.IsNullOrEmpty(appName)) return false;

            // Simple exclusions
            if (appName.Contains("Microsoft", StringComparison.OrdinalIgnoreCase) && !appName.Contains("Store", StringComparison.OrdinalIgnoreCase)) return false;
            if (appName.Contains("Windows", StringComparison.OrdinalIgnoreCase) && !appName.Contains("Edge", StringComparison.OrdinalIgnoreCase)) return false;
            if (appName.Contains("Visual Studio", StringComparison.OrdinalIgnoreCase)) return false;
            if (appName.Contains("Office", StringComparison.OrdinalIgnoreCase)) return false;
            if (appName.Contains("Driver", StringComparison.OrdinalIgnoreCase)) return false;
            if (appName.Contains("Security", StringComparison.OrdinalIgnoreCase)) return false;

            // Simple inclusions (examples - these might be too aggressive)
            if (publisher != null && publisher.Contains("Dell", StringComparison.OrdinalIgnoreCase)) return true;
            if (publisher != null && publisher.Contains("HP", StringComparison.OrdinalIgnoreCase)) return true;
            if (publisher != null && publisher.Contains("Lenovo", StringComparison.OrdinalIgnoreCase)) return true;
            if (appName.Contains("Candy Crush", StringComparison.OrdinalIgnoreCase)) return true;
            if (appName.Contains("Xbox", StringComparison.OrdinalIgnoreCase) && !appName.Contains("Game Bar", StringComparison.OrdinalIgnoreCase)) return true; // Most Xbox apps are bloat for non-gamers

            return false; // Default to not bloatware
        }

        private bool IsWindowsSystemApp(string publisher)
        {
            if (string.IsNullOrEmpty(publisher)) return false;
            return publisher.Contains("Microsoft", StringComparison.OrdinalIgnoreCase) || publisher.Contains("Windows", StringComparison.OrdinalIgnoreCase);
        }
    }
}
