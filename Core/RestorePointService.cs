using System;
using System.Management;
using System.Threading.Tasks;

namespace WassControlSys.Core
{
    public class RestorePointService : IRestorePointService
    {
        private readonly ILogService _log;

        public RestorePointService(ILogService log)
        {
            _log = log;
        }

        public async Task<(bool Success, string Message)> CreateRestorePointAsync(string description)
        {
            return await Task.Run(() =>
            {
                try
                {
                    _log.Info($"Intentando crear punto de restauración: {description}");
                    
                    var mScope = new ManagementScope("\\\\.\\root\\default");
                    var mPath = new ManagementPath("SystemRestore");
                    var options = new ObjectGetOptions();
                    
                    using (var mClass = new ManagementClass(mScope, mPath, options))
                    {
                        using (var inputArgs = mClass.GetMethodParameters("CreateRestorePoint"))
                        {
                            inputArgs["Description"] = description;
                            inputArgs["RestorePointType"] = 12; // MANUAL_CHECKPOINT
                            inputArgs["EventType"] = 100;       // BEGIN_SYSTEM_CHANGE

                            using (var outArgs = mClass.InvokeMethod("CreateRestorePoint", inputArgs, null))
                            {
                                uint result = (uint)outArgs["ReturnValue"];
                                if (result == 0)
                                {
                                    _log.Info("Punto de restauración creado con éxito.");
                                    return (true, "Punto de restauración creado con éxito.");
                                }
                                else
                                {
                                    _log.Warn($"No se pudo crear el punto de restauración. Código: {result}");
                                    return (false, $"Error al crear punto de restauración (Código: {result}). Asegúrese de que la Protección del Sistema esté activada.");
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _log.Error("Error creando punto de restauración", ex);
                    return (false, $"Error crítico: {ex.Message}");
                }
            });
        }
        public async Task<(string Name, DateTime? Date)> GetLastRestorePointAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    var mScope = new ManagementScope("\\\\.\\root\\default");
                    var query = new SelectQuery("SELECT * FROM SystemRestore");
                    using (var searcher = new ManagementObjectSearcher(mScope, query))
                    {
                        ManagementObject? latest = null;
                        foreach (ManagementObject rp in searcher.Get())
                        {
                            if (latest == null)
                            {
                                latest = rp;
                                continue;
                            }

                            string? latestDateStr = latest["CreationTime"]?.ToString();
                            string? currentDateStr = rp["CreationTime"]?.ToString();

                            if (latestDateStr != null && currentDateStr != null)
                            {
                                if (string.Compare(currentDateStr, latestDateStr) > 0)
                                {
                                    latest = rp;
                                }
                            }
                        }

                        if (latest != null)
                        {
                            string name = latest["Description"]?.ToString() ?? "Sin nombre";
                            string? dateStr = latest["CreationTime"]?.ToString();
                            DateTime? date = null;
                            if (dateStr != null)
                            {
                                try
                                {
                                    // WMI dates are in format yyyymmddHHMMSS.mmmmmmsUUU
                                    date = ManagementDateTimeConverter.ToDateTime(dateStr);
                                }
                                catch { }
                            }
                            return (name, date);
                        }
                    }
                }
                catch (ManagementException ex) when (ex.ErrorCode == ManagementStatus.AccessDenied)
                {
                    _log.Warn("Acceso denegado al consultar puntos de restauración. Ejecute la aplicación como Administrador para ver esta información.");
                }
                catch (Exception ex)
                {
                    _log.Error("Error obteniendo último punto de restauración", ex);
                }
                return ("No detectado", null);
            });
        }
    }
}
