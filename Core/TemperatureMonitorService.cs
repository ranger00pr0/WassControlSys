using System;
using System.Linq;
using System.Threading.Tasks;

namespace WassControlSys.Core
{
    public class TemperatureMonitorService : ITemperatureMonitorService
    {
        public async Task<double?> GetCpuTemperatureCAsync()
        {
            return await Task.Run<double?>(() =>
            {
                try
                {
                    using var searcher = new System.Management.ManagementObjectSearcher(@"root\WMI", "SELECT CurrentTemperature FROM MSAcpi_ThermalZoneTemperature");
                    var temps = searcher.Get().Cast<System.Management.ManagementObject>().Select(mo => ConvertToCelsius(mo["CurrentTemperature"]));
                    var valid = temps.Where(t => t.HasValue).Select(t => t!.Value).ToList();
                    if (valid.Count == 0) return null;
                    return valid.Average();
                }
                catch
                {
                    return null;
                }
            });
        }

        private static double? ConvertToCelsius(object val)
        {
            try
            {
                if (val == null) return null;
                int raw = Convert.ToInt32(val);
                double kelvin = raw / 10.0;
                double celsius = kelvin - 273.15;
                if (celsius < -50 || celsius > 150) return null;
                return celsius;
            }
            catch { return null; }
        }
    }
}

