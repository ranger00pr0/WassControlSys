using System.Collections.Generic;
using System.Threading.Tasks;

namespace WassControlSys.Models
{
    public class FolderSizeInfo
    {
        public string Path { get; set; } = string.Empty;
        public long SizeBytes { get; set; }
        public string FormattedSize { get; set; } = "0 B";
        public double Percentage { get; set; }
    }
}
