using System.Diagnostics;

namespace WassControlSys.Models
{
    public class ProcessLaunchResult
    {
        public bool Started { get; set; }
        public int? ExitCode { get; set; }
        public string? Message { get; set; }
        public string? StandardOutput { get; set; }
        public string? StandardError { get; set; }

        public static ProcessLaunchResult From(Process? p, string? message = null)
        {
            return new ProcessLaunchResult
            {
                Started = p != null,
                ExitCode = p?.HasExited == true ? p.ExitCode : null,
                Message = message
            };
        }
    }
}
