using System.Threading.Tasks;

namespace WassControlSys.Core
{
    public interface ITemperatureMonitorService
    {
        Task<double?> GetCpuTemperatureCAsync();
    }
}

