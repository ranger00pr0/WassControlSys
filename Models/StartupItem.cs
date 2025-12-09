namespace WassControlSys.Models
{
    public class StartupItem
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public bool IsEnabled { get; set; }
        public StartupItemType Type { get; set; }
    }

    public enum StartupItemType
    {
        RegistryRun,
        StartupFolder,
        TaskScheduler
    }
}
