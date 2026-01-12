namespace WassControlSys.Models
{
    public class DiskHealthInfo
    {
        public string DeviceId { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string Serial { get; set; } = string.Empty;
        public string Capacity { get; set; } = string.Empty;
        public bool SmartOk { get; set; }
        public bool SmartStatusKnown { get; set; }
        public string SmartStatus { get; set; } = string.Empty;
        public int Temperature { get; set; } // Added for compatibility
    }
}
