using System;
using System.Management;
using System.Threading.Tasks;
using WassControlSys.Models;

namespace WassControlSys.Core
{
    public class BatteryService : IBatteryService
    {
        private readonly ILogService _log;

        public BatteryService(ILogService log)
        {
            _log = log;
        }

        public async Task<BatteryInfo> GetBatteryStatusAsync()
        {
            return await Task.Run(() =>
            {
                var info = new BatteryInfo { IsPresent = false };
                try
                {
                    // WMI Win32_Battery for basic info
                    using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Battery"))
                    {
                        foreach (var obj in searcher.Get())
                        {
                            info.IsPresent = true;
                            info.EstimatedChargeRemaining = Convert.ToInt32(obj["EstimatedChargeRemaining"]);
                            info.Status = GetBatteryStatusString(Convert.ToUInt16(obj["BatteryStatus"]));
                        }
                    }

                    // WMI root/WMI BatteryFullChargedCapacity for health (Windows 8+)
                    if (info.IsPresent)
                    {
                        using (var searcher = new ManagementObjectSearcher(@"root\WMI", "SELECT * FROM BatteryFullChargedCapacity"))
                        {
                            foreach (var obj in searcher.Get())
                            {
                                info.FullChargeCapacity = Convert.ToUInt32(obj["FullChargedCapacity"]);
                            }
                        }

                        using (var searcher = new ManagementObjectSearcher(@"root\WMI", "SELECT * FROM BatteryStaticData"))
                        {
                            foreach (var obj in searcher.Get())
                            {
                                info.DesignCapacity = Convert.ToUInt32(obj["DesignedCapacity"]);
                            }
                        }

                        if (info.DesignCapacity > 0)
                        {
                            info.HealthPercentage = (uint)((info.FullChargeCapacity * 100) / info.DesignCapacity);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _log.Warn($"No se pudo obtener información detallada de la batería: {ex.Message}");
                }
                return info;
            });
        }

        private string GetBatteryStatusString(ushort status)
        {
            return status switch
            {
                1 => "Descargando",
                2 => "Conectada (AC)",
                3 => "Cargando",
                4 => "Crítica",
                5 => "Baja",
                6 => "Alta",
                _ => "Desconocido"
            };
        }
    }
}
