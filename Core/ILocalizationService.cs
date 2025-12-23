using System.Threading.Tasks;
namespace WassControlSys.Core
{
    public interface ILocalizationService
    {
        Task SetLanguageAsync(string language);
        string CurrentLanguage { get; }
    }
}
