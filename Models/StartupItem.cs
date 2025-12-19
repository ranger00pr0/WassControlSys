namespace WassControlSys.Models
{
    public class StartupItem
    {
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public bool IsEnabled { get; set; }
        public StartupItemType Type { get; set; }
        public string SourceKeyPath { get; set; } = string.Empty;
        public bool IsMachineWide { get; set; }
        public double ImpactScore { get; set; }
    }

    public enum StartupItemType
    {
        RegistryRun,
        StartupFolder,
        TaskScheduler
    }
}
