using System.Threading.Tasks;

namespace WassControlSys.Core
{
    public interface ISecurityService
    {
        Task<SecurityStatus> GetSecurityStatusAsync();
    }
}
