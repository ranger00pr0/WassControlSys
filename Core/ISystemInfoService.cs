using System.Threading.Tasks;

namespace WassControlSys.Core
{
    public interface ISystemInfoService
    {
        Task<SystemInfo> GetSystemInfoAsync();
    }
}
