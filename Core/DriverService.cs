using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
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

        public async Task<(bool Success, string Message)> ExportDriversAsync(string destinationPath, IProgress<(int, string)> progress)
        {
            try
            {
                if (!Directory.Exists(destinationPath))
                {
                    Directory.CreateDirectory(destinationPath);
                }

                _log.Info($"Iniciando exportación de drivers a: {destinationPath}");
                progress?.Report((0, "Iniciando DISM..."));

                var psi = new ProcessStartInfo("dism.exe", $"/online /export-driver /destination:\"{destinationPath}\"")
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    Verb = "runas"
                };

                using (var process = new Process { StartInfo = psi })
                {
                    int lastPercentage = 0;
                    process.OutputDataReceived += (sender, args) =>
                    {
                        if (args.Data != null)
                        {
                            _log.Info($"[DISM] {args.Data}");
                            // Expresión regular para capturar el porcentaje. Ej: "[  25.0%]" o "[==========================100.0%==========================]"
                            var match = Regex.Match(args.Data, @"(\d{1,3})\s*(\.\d+)?%");
                            if (match.Success && int.TryParse(match.Groups[1].Value, out int percentage))
                            {
                                lastPercentage = percentage;
                                progress?.Report((percentage, $"Exportando... {percentage}%"));
                            }
                            else if (args.Data.StartsWith("Exporting driver package"))
                            {
                                progress?.Report((lastPercentage, "Exportando paquete de controladores..."));
                            }
                        }
                    };
                    process.ErrorDataReceived += (sender, args) =>
                    {
                        if (args.Data != null) _log.Error($"[DISM Error] {args.Data}");
                    };

                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    await process.WaitForExitAsync();

                    if (process.ExitCode == 0)
                    {
                        _log.Info("Drivers exportados correctamente.");
                        progress?.Report((100, "Exportación completada."));
                        return (true, "Todos los controladores de terceros han sido exportados con éxito.");
                    }
                    else
                    {
                        _log.Error($"DISM falló con código: {process.ExitCode}");
                        return (false, $"Error al exportar drivers. Código de salida: {process.ExitCode}");
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error("Error en exportación de drivers", ex);
                return (false, $"Error crítico: {ex.Message}");
            }
        }
    }
}
