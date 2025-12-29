using System.Collections.Generic;

namespace WassControlSys.Models
{
    public class ProfileConfig
    {
        public PerformanceMode Mode { get; set; }
        
        // Servicios
        public List<string> ServicesToStop { get; set; } = new List<string>();
        public bool DisableTelemetry { get; set; } = true;
        public bool DisableIndexing { get; set; } = false;
        
        // Procesos
        public bool ReduceBackgroundPriority { get; set; } = true;
        public List<string> ProcessesToKill { get; set; } = new List<string>();
        
        // Energía
        public string PowerPlanGuid { get; set; } = "381b4222-f694-41f0-9685-ff5bb260df2e"; // Equilibrado

        public static ProfileConfig DefaultGamer() => new ProfileConfig
        {
            Mode = PerformanceMode.Gamer,
            ServicesToStop = new List<string> { "SysMain", "WSearch" },
            ReduceBackgroundPriority = true,
            PowerPlanGuid = "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c" // Alto rendimiento
        };

        public static ProfileConfig DefaultDev() => new ProfileConfig
        {
            Mode = PerformanceMode.Dev,
            ServicesToStop = new List<string> { "WSearch" },
            ReduceBackgroundPriority = false,
            PowerPlanGuid = "381b4222-f694-41f0-9685-ff5bb260df2e"
        };
    }
}
