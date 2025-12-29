using System.Threading;
using System.Threading.Tasks;
using WassControlSys.Models;

namespace WassControlSys.Core
{
    public interface IWingetService
    {
        Task<IEnumerable<WingetApp>> GetUpdatableAppsAsync(CancellationToken ct = default);
        Task<bool> UpdateAppAsync(string appId, IProgress<(int, string)> progress, CancellationToken ct = default);
        Task<bool> UpdateAllAppsAsync(CancellationToken ct = default);
    }
}
