namespace WassControlSys.Models
{
    public class BloatwareApp
    {
        public string Name { get; set; }
        public string Publisher { get; set; }
        public string InstallLocation { get; set; }
        public string UninstallCommand { get; set; }
        public bool IsSystemApp { get; set; } // To differentiate from user-installed apps
    }
}
