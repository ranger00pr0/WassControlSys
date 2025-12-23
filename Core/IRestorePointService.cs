using System.Threading.Tasks;

namespace WassControlSys.Core
{
    public interface IRestorePointService
    {
        Task<(bool Success, string Message)> CreateRestorePointAsync(string description);
        Task<(string Name, DateTime? Date)> GetLastRestorePointAsync();
    }
}
