using System.Threading.Tasks;

namespace WassControlSys.Core
{
    public interface IDialogService
    {
        Task ShowMessage(string message, string title);
        Task<bool> ShowConfirmation(string message, string title);
    }
}
