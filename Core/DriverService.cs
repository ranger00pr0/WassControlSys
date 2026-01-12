using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Management;

namespace WassControlSys.Core
{
    public class DriverService : IDriverService
    {
        private readonly ILogService _log;

        public DriverService(ILogService log)
        {
            _log = log;
        }

        public async Task<List<DriverInfo>> GetDriversWithProblemsAsync()
        {
            return await Task.Run(() =>
            {
                var list = new List<DriverInfo>();
                try
                {
                    using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE ConfigManagerErrorCode <> 0");
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        uint errorCode = (uint)(obj["ConfigManagerErrorCode"] ?? 0);
                        list.Add(new DriverInfo
                        {
                            Name = obj["Name"]?.ToString() ?? "Dispositivo Desconocido",
                            Status = obj["Status"]?.ToString() ?? "",
                            DeviceId = obj["DeviceID"]?.ToString() ?? "",
                            ErrorDescription = GetErrorMessage(errorCode)
                        });
                    }
                }
                catch (Exception ex)
                {
                    _log.Error("Error al buscar drivers con problemas", ex);
                }
                return list;
            });
        }

        private string GetErrorMessage(uint code)
        {
            return code switch
            {
                1 => "El dispositivo no está configurado correctamente.",
                3 => "El controlador para este dispositivo puede estar dañado.",
                10 => "Este dispositivo no puede iniciar. Intente actualizar los controladores.",
                12 => "Este dispositivo no puede encontrar suficientes recursos libres para funcionar.",
                14 => "Este dispositivo no puede funcionar correctamente hasta que reinicie su equipo.",
                18 => "Vuelva a instalar los controladores para este dispositivo.",
                21 => "Windows está quitando este dispositivo.",
                22 => "El dispositivo se ha deshabilitado.",
                24 => "El dispositivo no está presente, no funciona correctamente o no tiene todos sus controladores instalados.",
                28 => "Los controladores para este dispositivo no están instalados.",
                29 => "El dispositivo se ha deshabilitado porque el firmware del dispositivo no le asignó los recursos necesarios.",
                31 => "Este dispositivo no funciona correctamente porque Windows no puede cargar los controladores requeridos.",
                39 => "Windows no puede cargar el controlador de dispositivo para este hardware. Es posible que el controlador esté dañado o no se encuentre.",
                43 => "Windows detuvo este dispositivo porque informó de problemas.",
                45 => "Actualmente, este dispositivo de hardware no está conectado al equipo.",
                _ => $"Error de configuración (Código {code})"
            };
        }

        public async Task<(bool Success, string Message)> ExportDriversAsync(string destinationPath, IProgress<(int, string)> progress, System.Threading.CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

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

                    await process.WaitForExitAsync(cancellationToken);

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
            catch (OperationCanceledException)
            {
                _log.Info("Exportación de drivers cancelada por el usuario.");
                return (false, "Exportación de drivers cancelada.");
            }
            catch (Exception ex)
            {
                _log.Error("Error en exportación de drivers", ex);
                return (false, $"Error crítico: {ex.Message}");
            }
        }
    }
}
