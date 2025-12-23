using System.Threading.Tasks;
using WassControlSys.Models;

namespace WassControlSys.Core
{
    public interface ISystemMaintenanceService
    {
        Task<CleanResult> CleanTemporaryFilesAsync(CleaningOptions options);
        Task<CleanResult> OptimizeMemoryAsync();
        bool IsAdministrator();
        Task<ProcessLaunchResult> LaunchSystemFileCheckerAsync();
        Task<ProcessLaunchResult> LaunchDISMHealthRestoreAsync();
        Task<ProcessLaunchResult> LaunchCHKDSKAsync();
        Task<ProcessLaunchResult> FlushDnsAsync();
        Task<ProcessLaunchResult> AnalyzeDiskAsync();
        Task<ProcessLaunchResult> CleanPrefetchAsync();
        Task<ProcessLaunchResult> RebuildSearchIndexAsync();
        Task<ProcessLaunchResult> ResetNetworkAsync();
    }
}
