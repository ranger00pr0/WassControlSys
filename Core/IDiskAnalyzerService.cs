using System.Collections.Generic;
using System.Threading.Tasks;
using WassControlSys.Models;

namespace WassControlSys.Core
{
    public interface IDiskAnalyzerService
    {
        Task<IEnumerable<FolderSizeInfo>> AnalyzeDirectoryAsync(string path);
        Task<IEnumerable<FolderSizeInfo>> FindLargeFilesAsync(string path, long minSizeInBytes);

    }
}
