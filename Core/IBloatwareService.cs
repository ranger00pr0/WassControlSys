using System.Collections.Generic;
using System.Threading.Tasks;
using WassControlSys.Models;

namespace WassControlSys.Core
{
    public interface IBloatwareService
    {
        Task<IEnumerable<BloatwareApp>> GetBloatwareAppsAsync();
        Task<bool> UninstallBloatwareAppAsync(BloatwareApp app);
    }
}
