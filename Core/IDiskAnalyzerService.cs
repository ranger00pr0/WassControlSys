using System.Collections.Generic;
using System.Threading.Tasks;
using WassControlSys.Models;

namespace WassControlSys.Core
{
    public interface IDiskAnalyzerService
    {
        Task<IEnumerable<FolderSizeInfo>> AnalyzeDirectoryAsync(string path);
    }
}
