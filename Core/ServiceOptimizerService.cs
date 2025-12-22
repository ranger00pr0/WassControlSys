using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess; // Para ServiceController
using System.Threading.Tasks;
using WassControlSys.Models;

namespace WassControlSys.Core
{
    public class ServiceOptimizerService : IServiceOptimizerService
    {
        private readonly ILogService _log;

        public ServiceOptimizerService(ILogService log)
        {
            _log = log;
        }

        public async Task<IEnumerable<WindowsService>> GetWindowsServicesAsync()
        {
            return await Task.Run(() =>
            {
                var services = new List<WindowsService>();
                try
                {
                    ServiceController[] scServices = ServiceController.GetServices();
                    foreach (ServiceController sc in scServices)
                    {
                        try
                        {
                            services.Add(new WindowsService
                            {
                                Name = sc.ServiceName,
                                DisplayName = sc.DisplayName,
                                Description = GetServiceDescription(sc.ServiceName), // Requiere WMI
                                Status = (ServiceStatus)Enum.Parse(typeof(ServiceStatus), sc.Status.ToString()),
                                CanBeStopped = sc.CanStop,
                                CanBePaused = sc.CanPauseAndContinue,
                                StartType = GetServiceStartType(sc.ServiceName) // Requiere WMI
                                // RecommendedAction será determinado por la UI/lógica
                            });
                        }
                        catch (Exception ex)
                        {
                            _log.Warn($"Error getting info for service {sc.ServiceName}: {ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _log.Error("Error getting list of Windows services", ex);
                }
                return services.OrderBy(s => s.DisplayName);
            });
        }

        public async Task<bool> StartServiceAsync(string serviceName)
        {
            return await Task.Run(async () =>
            {
                try
                {
                    using (ServiceController sc = new ServiceController(serviceName))
                    {
                        if (sc.Status == ServiceControllerStatus.Stopped || sc.Status == ServiceControllerStatus.StopPending)
                        {
                            _log.Info($"Starting service: {serviceName}");
                            sc.Start();
                            await Task.Delay(1000); // Darle un momento para iniciar
                            sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));
                            _log.Info($"Service {serviceName} started. Status: {sc.Status}");
                            return sc.Status == ServiceControllerStatus.Running;
                        }
                        _log.Info($"Service {serviceName} is already running or in pending state. Status: {sc.Status}");
                        return true; // Ya en ejecución, considerarlo un éxito
                    }
                }
                catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 5)
                {
                    _log.Error($"Error starting service {serviceName}: Acceso denegado. Se requieren privilegios de administrador.");
                    return false;
                }
                catch (Exception ex)
                {
                    _log.Error($"Error starting service {serviceName}", ex);
                    return false;
                }
            });
        }

        public async Task<bool> StopServiceAsync(string serviceName)
        {
            return await Task.Run(async () =>
            {
                try
                {
                    using (ServiceController sc = new ServiceController(serviceName))
                    {
                        if (sc.Status == ServiceControllerStatus.Running || sc.Status == ServiceControllerStatus.StartPending)
                        {
                            _log.Info($"Stopping service: {serviceName}");
                            sc.Stop();
                            await Task.Delay(1000); // Darle un momento para detenerse
                            sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
                            _log.Info($"Service {serviceName} stopped. Status: {sc.Status}");
                            return sc.Status == ServiceControllerStatus.Stopped;
                        }
                        _log.Info($"Service {serviceName} is already stopped or in pending state. Status: {sc.Status}");
                        return true; // Ya detenido, considerarlo un éxito
                    }
                }
                catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 5)
                {
                    _log.Error($"Error stopping service {serviceName}: Acceso denegado. Se requieren privilegios de administrador.");
                    return false;
                }
                catch (Exception ex)
                {
                    _log.Error($"Error stopping service {serviceName}", ex);
                    return false;
                }
            });
        }

        public async Task<bool> SetServiceStartTypeAsync(string serviceName, ServiceStartType startType)
        {
            return await Task.Run(() =>
            {
                try
                {
                    // Esto normalmente requiere privilegios administrativos.
                    // Usando WMI para establecer el tipo de inicio
                    string managementPath = $"Win32_Service.Name='{serviceName}'";
                    using (var service = new System.Management.ManagementObject(managementPath))
                    {
                        object[] wmiParams = new object[1];
                        wmiParams[0] = (uint)GetWmiStartType(startType); // Convertir a la enumeración de modo de inicio de WMI
                        
                        _log.Info($"Setting start type for service {serviceName} to {startType}");
                        uint returnValue = (uint)service.InvokeMethod("ChangeStartMode", wmiParams);
                        
                        return returnValue == 0; // 0 indica éxito
                    }
                }
                catch (Exception ex)
                {
                    _log.Error($"Error setting start type for service {serviceName} to {startType}", ex);
                    return false;
                }
            });
        }

        private ServiceStartType GetServiceStartType(string serviceName)
        {
            try
            {
                using (var service = new System.Management.ManagementObject($"Win32_Service.Name='{serviceName}'"))
                {
                    var startMode = service["StartMode"]?.ToString();
                    return startMode switch
                    {
                        "Automatic" => ServiceStartType.Automatic,
                        "Boot" => ServiceStartType.Boot,
                        "System" => ServiceStartType.System,
                        "Manual" => ServiceStartType.Manual,
                        "Disabled" => ServiceStartType.Disabled,
                        _ => ServiceStartType.Unknown // No debería ocurrir a menudo
                    };
                }
            }
            catch (Exception ex)
            {
                _log.Warn($"Error getting start type for service {serviceName}: {ex.Message}");
                return ServiceStartType.Unknown;
            }
        }

        private string GetServiceDescription(string serviceName)
        {
            try
            {
                using (var service = new System.Management.ManagementObject($"Win32_Service.Name='{serviceName}'"))
                {
                    return service["Description"]?.ToString() ?? "Descripción no disponible.";
                }
            }
            catch (Exception ex)
            {
                _log.Warn($"Error getting description for service {serviceName}: {ex.Message}");
                return "Error al recuperar la descripción.";
            }
        }

        // Ayudante para convertir el enum interno al modo de inicio numérico de WMI
        private uint GetWmiStartType(ServiceStartType startType)
        {
            return startType switch
            {
                ServiceStartType.Automatic => 2, // Automático
                ServiceStartType.Manual => 3,    // Manual
                ServiceStartType.Disabled => 4,   // Deshabilitado
                ServiceStartType.Boot => 0,      // Arranque
                ServiceStartType.System => 1,    // Sistema
                _ => 2 // Por defecto a Automático
            };
        }
    }
}
