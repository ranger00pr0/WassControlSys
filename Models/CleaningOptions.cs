namespace WassControlSys.Models
{
    public class CleaningOptions
    {
        public bool CleanSystemTemp { get; set; } = true;
        public bool CleanRecycleBin { get; set; } = true;
        public bool CleanBrowserCache { get; set; } = true;
        public bool CleanWindowsUpdate { get; set; } = false; // More aggressive
        public bool CleanThumbnails { get; set; } = false;
        public bool CleanEventLogs { get; set; } = false; // Requires admin
    }
}
