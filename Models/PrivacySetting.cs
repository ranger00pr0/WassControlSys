namespace WassControlSys.Models
{
    public class PrivacySetting
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public bool CurrentValue { get; set; }
        public bool RecommendedValue { get; set; } // Based on a "privacy-focused" profile
        public PrivacySettingType Type { get; set; } // e.g., Telemetry, AdvertisingId, Location
        public string RegistryPath { get; set; } // Where the setting is stored (for internal use)
        public string RegistryValueName { get; set; } // The name of the registry value
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
