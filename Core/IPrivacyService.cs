using System.Collections.Generic;
using System.Threading.Tasks;
using WassControlSys.Models;

namespace WassControlSys.Core
{
    public interface IPrivacyService
    {
        Task<IEnumerable<PrivacySetting>> GetPrivacySettingsAsync();
        Task<bool> UpdatePrivacySettingAsync(PrivacySetting setting, bool newValue);
    }
}
