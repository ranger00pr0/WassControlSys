using System.Collections.Generic;
using System.Threading.Tasks;
using WassControlSys.Models;

namespace WassControlSys.Core
{
    public interface IWingetService
    {
        Task<IEnumerable<WingetApp>> GetUpdatableAppsAsync();
        Task<bool> UpdateAppAsync(string appId);
        Task<bool> UpdateAllAppsAsync();
    }
}
