using System;
using System.Threading.Tasks;

namespace WassControlSys.Core
{
    public interface IDriverService
    {
        Task<(bool Success, string Message)> ExportDriversAsync(string destinationPath, IProgress<(int, string)> progress);
        Task<System.Collections.Generic.List<DriverInfo>> GetDriversWithProblemsAsync();
    }

    public class DriverInfo
    {
        public string Name { get; set; } = "";
        public string Status { get; set; } = "";
        public string DeviceId { get; set; } = "";
        public string ErrorDescription { get; set; } = "";
    }
}
