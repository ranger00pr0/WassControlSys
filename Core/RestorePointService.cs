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
    }
}
