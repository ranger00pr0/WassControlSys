using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace WassControlSys.Core
{
    public class DriverService : IDriverService
    {
        private readonly ILogService _log;

        public DriverService(ILogService log)
        {
            _log = log;
        }

        public async Task<(bool Success, string Message)> ExportDriversAsync(string destinationPath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (!Directory.Exists(destinationPath))
                    {
                        Directory.CreateDirectory(destinationPath);
                    }

                    _log.Info($"Iniciando exportación de drivers a: {destinationPath}");
                    
                    var psi = new ProcessStartInfo("dism.exe", $"/online /export-driver /destination:\"{destinationPath}\"")
                    {
                        UseShellExecute = true,
                        Verb = "runas", // Requerido para DISM
                        CreateNoWindow = false
                    };

                    using (var process = Process.Start(psi))
                    {
                        process?.WaitForExit();
                        if (process?.ExitCode == 0)
                        {
                            _log.Info("Drivers exportados correctamente.");
                            return (true, "Todos los controladores de terceros han sido exportados con éxito.");
                        }
                        else
                        {
                            _log.Error($"DISM falló con código: {process?.ExitCode}");
                            return (false, $"Error al exportar drivers. Código de salida: {process?.ExitCode}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _log.Error("Error en exportación de drivers", ex);
                    return (false, $"Error crítico: {ex.Message}");
                }
            });
        }
    }
}
