using System.Collections.Generic;
using System.Threading.Tasks;
using WassControlSys.Models;

namespace WassControlSys.Core
{
    public interface IStartupService
    {
        Task<IEnumerable<StartupItem>> GetStartupItemsAsync();
        Task<bool> EnableStartupItemAsync(StartupItem item);
        Task<bool> DisableStartupItemAsync(StartupItem item);
    }
}
