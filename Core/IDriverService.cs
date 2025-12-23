using System;
using System.Threading.Tasks;

namespace WassControlSys.Core
{
    public interface IDriverService
    {
        Task<(bool Success, string Message)> ExportDriversAsync(string destinationPath, IProgress<(int, string)> progress);
    }
}
