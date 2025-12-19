namespace WassControlSys.Models
{
    public class BloatwareApp
    {
        public string Name { get; set; } = string.Empty;
        public string Publisher { get; set; } = string.Empty;
        public string InstallLocation { get; set; } = string.Empty;
        public string UninstallCommand { get; set; } = string.Empty;
        public bool IsSystemApp { get; set; } // Para diferenciar de las aplicaciones instaladas por el usuario
    }
}
