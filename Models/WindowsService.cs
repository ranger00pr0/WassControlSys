namespace WassControlSys.Models
{
    public class WindowsService
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public ServiceStatus Status { get; set; }
        public ServiceStartType StartType { get; set; }
        public bool CanBeStopped { get; set; }
        public bool CanBePaused { get; set; }
        public string RecommendedAction { get; set; } = string.Empty; // por ejemplo, "Mantener Automático", "Establecer en Manual", "Deshabilitar"
        
        public bool IsRunning => Status == ServiceStatus.Running;
    }

    public enum ServiceStatus
    {
        Running,
        Stopped,
        Paused,
        StartPending,
        StopPending,
        ContinuePending,
        PausePending,
        Unknown
    }

    public enum ServiceStartType
    {
        Automatic,
        AutomaticDelayed,
        Manual,
        Disabled,
        Boot,
        System,
        Unknown
    }
}
