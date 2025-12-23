using System.Threading.Tasks;
using WassControlSys.Models;

namespace WassControlSys.Core
{
    public interface IPerformanceProfileService
    {
        Task<ApplyProfileResult> ApplyProfileAsync(PerformanceMode mode);
    }
}
