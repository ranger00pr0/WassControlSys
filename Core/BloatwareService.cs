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
        private readonly IDialogService _dialogService; // Para advertencias/confirmaciones

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

                // Rutas de registro tanto de Máquina Local (HKLM) como de Usuario Actual (HKCU)
                var rootKeys = new[] { Registry.LocalMachine, Registry.CurrentUser };
                var uninstallPaths = new[]
                {
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
                    @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
                };

                foreach (var rootKey in rootKeys)
                {
                    foreach (var uninstallPath in uninstallPaths)
                    {
                        using (var baseKey = rootKey.OpenSubKey(uninstallPath))
                        {
                            if (baseKey == null) continue;

                            foreach (string subKeyName in baseKey.GetSubKeyNames())
                            {
                                using (var subKey = baseKey.OpenSubKey(subKeyName))
                                {
                                    if (subKey == null) continue;

                                    var appName = (subKey.GetValue("DisplayName") as string) ?? string.Empty;
                                    var publisher = (subKey.GetValue("Publisher") as string) ?? string.Empty;
                                    var installLocation = (subKey.GetValue("InstallLocation") as string) ?? string.Empty;
                                    var uninstallString = (subKey.GetValue("UninstallString") as string) ?? string.Empty;

                                    if (!string.IsNullOrEmpty(appName) && !string.IsNullOrEmpty(uninstallString))
                                    {
                                        // RELAJAR FILTRADO: Si la heurística es muy estricta, no saldrá nada
                                        // Por ahora, incluiremos MÁS apps para probar, pero marcaremos si es sospechoso
                                        
                                        // bool isBloat = IsPotentialBloatware(appName, publisher);
                                        // Para que el usuario vea ALGO, vamos a listar todo lo que no sea esencial del sistema
                                        // y dejaremos que el usuario decida (con cuidado).
                                        
                                        // Filtrado básico de seguridad (no listar drivers ni updates de seguridad críticos)
                                        if (!IsCriticalSystemComponent(appName, publisher)) 
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
                
                // Eliminar duplicados por nombre
                return bloatwareList.GroupBy(x => x.Name).Select(g => g.First()).OrderBy(a => a.Name);
            });
        }

        public async Task<bool> UninstallBloatwareAppAsync(BloatwareApp app)
        {
            if (app == null || string.IsNullOrEmpty(app.UninstallCommand))
            {
                _log.Warn("Attempted to uninstall a null or invalid bloatware app.");
                return false;
            }

            // Siempre pedir confirmación antes de desinstalar
            bool confirm = await _dialogService.ShowConfirmation($"¿Está seguro de que desea desinstalar '{app.Name}'? Esta acción no se puede deshacer.", "Confirmar Desinstalación");
            if (!confirm) return false;

            try
            {
                _log.Info($"Attempting to uninstall bloatware app: {app.Name} with command: {app.UninstallCommand}");
                
                ProcessStartInfo psi;
                string command = app.UninstallCommand.Trim();
                string arguments = "";

                // Handle quotes in the command path
                if (command.StartsWith("\""))
                {
                    int endQuoteIndex = command.IndexOf('"', 1);
                    if (endQuoteIndex > 0)
                    {
                        arguments = command.Substring(endQuoteIndex + 1).Trim();
                        command = command.Substring(1, endQuoteIndex - 1);
                    }
                }
                else if (command.Contains(" "))
                {
                    arguments = command.Substring(command.IndexOf(" ") + 1).Trim();
                    command = command.Substring(0, command.IndexOf(" ")).Trim();
                }

                psi = new ProcessStartInfo(command, arguments)
                {
                    UseShellExecute = true,
                    Verb = "runas" // Request UAC elevation
                };
                
                using (var process = Process.Start(psi))
                {
                    if (process == null)
                    {
                        _log.Error($"Failed to start uninstall process for {app.Name}");
                        await _dialogService.ShowMessage($"No se pudo iniciar el proceso de desinstalación para '{app.Name}'.", "Error de Desinstalación");
                        return false;
                    }
                    await Task.Run(() => process.WaitForExit());
                    _log.Info($"Uninstall process for {app.Name} exited with code {process.ExitCode}");
                    
                    // A common success code is 0. Some uninstallers might use other codes.
                    // For now, we'll consider 0 as the main success indicator.
                    if (process.ExitCode == 0)
                    {
                         await _dialogService.ShowMessage($"'{app.Name}' parece haberse desinstalado. Algunos cambios pueden requerir un reinicio.", "Desinstalación Finalizada");
                        return true;
                    }
                    else
                    {
                         await _dialogService.ShowMessage($"El desinstalador de '{app.Name}' finalizó con el código {process.ExitCode}. Es posible que no se haya desinstalado correctamente.", "Proceso Finalizado");
                        return false;
                    }
                }
            }
            catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1223) // ERROR_CANCELLED
            {
                // User cancelled the UAC prompt. This is not an error.
                _log.Warn($"UAC prompt was cancelled by the user for app: {app.Name}");
                return false;
            }
            catch (Exception ex)
            {
                _log.Error($"Error uninstalling bloatware app {app.Name}", ex);
                await _dialogService.ShowMessage($"Error al intentar desinstalar '{app.Name}': {ex.Message}", "Error Crítico de Desinstalación");
                return false;
            }
        }

        private bool IsCriticalSystemComponent(string appName, string publisher)
        {
            if (string.IsNullOrEmpty(appName)) return true; // Skip unknown

            // Exclusiones críticas (Drivers, .NET, C++, Windows Updates)
            if (appName.Contains("Microsoft Visual C++", StringComparison.OrdinalIgnoreCase)) return true;
            if (appName.Contains(".NET", StringComparison.OrdinalIgnoreCase)) return true;
            if (appName.Contains("Driver", StringComparison.OrdinalIgnoreCase)) return true;
            if (appName.Contains("Intel", StringComparison.OrdinalIgnoreCase)) return true;
            if (appName.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase)) return true;
            if (appName.Contains("AMD", StringComparison.OrdinalIgnoreCase)) return true;
            if (appName.Contains("Realtek", StringComparison.OrdinalIgnoreCase)) return true;
            if (appName.Contains("Update for Windows", StringComparison.OrdinalIgnoreCase)) return true;
            
            return false; 
        }

        private bool IsWindowsSystemApp(string publisher)
        {
            if (string.IsNullOrEmpty(publisher)) return false;
            return publisher.Contains("Microsoft", StringComparison.OrdinalIgnoreCase) || publisher.Contains("Windows", StringComparison.OrdinalIgnoreCase);
        }
    }
}
