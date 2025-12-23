using System.Threading.Tasks;
using WassControlSys.Models;

namespace WassControlSys.Core
{
    public interface IBatteryService
    {
        Task<BatteryInfo> GetBatteryStatusAsync();
    }
}
