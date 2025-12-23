using System.Collections.Generic;
using System.Threading.Tasks;
using WassControlSys.Models;

namespace WassControlSys.Core
{
    public interface IDiskHealthService
    {
        Task<IEnumerable<DiskHealthInfo>> GetDiskHealthAsync();
    }
}

