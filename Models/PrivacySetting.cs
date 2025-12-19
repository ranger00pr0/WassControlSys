namespace WassControlSys.Models
{
    public class PrivacySetting
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool CurrentValue { get; set; }
        public bool RecommendedValue { get; set; } // Based on a "privacy-focused" profile
        public PrivacySettingType Type { get; set; } // por ejemplo, Telemetría, ID de publicidad, Ubicación
        public string RegistryPath { get; set; } = string.Empty; // Where the setting is stored (for internal use)
        public string RegistryValueName { get; set; } = string.Empty; // The name of the registry value
    }

    public enum PrivacySettingType
    {
        Telemetry,
        AdvertisingId,
        LocationService,
        DiagnosticData,
        ActivityHistory,
        AppPermissions,
        Cortana,
        Other
    }
}
