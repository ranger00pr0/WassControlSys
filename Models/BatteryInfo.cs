namespace WassControlSys.Models
{
    public class BatteryInfo
    {
        public int EstimatedChargeRemaining { get; set; }
        public uint DesignCapacity { get; set; }
        public uint FullChargeCapacity { get; set; }
        public uint HealthPercentage { get; set; }
        public string Status { get; set; } = "Unknown";
        public bool IsPresent { get; set; }
    }
}
