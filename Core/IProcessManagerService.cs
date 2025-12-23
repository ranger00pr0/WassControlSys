using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using WassControlSys.Models;

namespace WassControlSys.Core
{
    public interface IProcessManagerService
    {
        Task<IEnumerable<ProcessInfoDto>> GetProcessesAsync();
        Task<bool> SetPriorityAsync(int pid, ProcessPriorityClass priority);
        Task<bool> KillProcessAsync(int pid);
        Task<ProcessImpactStats> ComputeImpactAsync();
        Task<int> ReduceBackgroundProcessesAsync(ProcessPriorityClass targetPriority);
    }
}

