using System.Collections.Generic;
using System.Threading.Tasks;
using WassControlSys.Models;

namespace WassControlSys.Core
{
    public interface IServiceOptimizerService
    {
        Task<IEnumerable<WindowsService>> GetWindowsServicesAsync();
        Task<bool> StartServiceAsync(string serviceName);
        Task<bool> StopServiceAsync(string serviceName);
        Task<bool> SetServiceStartTypeAsync(string serviceName, ServiceStartType startType);
        
        // Profiles functionality (to be implemented later)
        // Task<IEnumerable<ServiceProfile>> GetAvailableProfilesAsync();
        // Task<bool> ApplyProfileAsync(ServiceProfile profile);
    }
}
