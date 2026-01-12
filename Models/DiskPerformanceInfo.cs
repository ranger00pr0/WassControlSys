namespace WassControlSys.Models
{
    public class DiskPerformanceInfo
    {
        public string InstanceName { get; set; } = string.Empty; // e.g., "C:", "0 C:"
        public string DriveLetter { get; set; } = string.Empty; // e.g., "C:"
        public double ReadBytesPerSec { get; set; }
        public double WriteBytesPerSec { get; set; }
    }
}