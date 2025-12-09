using System.Threading.Tasks;
using WassControlSys.Models;

namespace WassControlSys.Core
{
    public interface ISystemMaintenanceService
    {
        Task<CleanResult> CleanTemporaryFilesAsync(CleaningOptions options);
        Task<CleanResult> OptimizeMemoryAsync();
        bool IsAdministrator();
        ProcessLaunchResult LaunchSystemFileChecker();
        ProcessLaunchResult LaunchDISMHealthRestore();
        ProcessLaunchResult LaunchCHKDSK();
    }
}
