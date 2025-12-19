using System;
using System.Management;
using Microsoft.Win32;
using System.Threading.Tasks;

namespace WassControlSys.Core
{
    public class SecurityStatus
    {
        public string AntivirusName { get; set; } = "No detectado";
        public bool IsAntivirusEnabled { get; set; } = false;
        public bool IsFirewallEnabled { get; set; } = false;
        public bool IsUacEnabled { get; set; } = false;
        public bool IsWindowsUpdateEnabled { get; set; } = false; // Added
        
        // Friendly properties for UI
        public string Antivirus => IsAntivirusEnabled ? AntivirusName : "Desactivado o No detectado";
        public string Firewall => IsFirewallEnabled ? "Activado" : "Desactivado";
        public string Uac => IsUacEnabled ? "Activado" : "Desactivado";
        public string WindowsUpdate => IsWindowsUpdateEnabled ? "Al día" : "Atención necesaria";
        
        public string OverallStatus => (IsAntivirusEnabled && IsFirewallEnabled && IsUacEnabled) ? "Seguro" : "Riesgo";
    }

    public class SecurityService : ISecurityService
    {
        public async Task<SecurityStatus> GetSecurityStatusAsync()
        {
            return await Task.Run(() =>
            {
                var status = new SecurityStatus
                {
                    IsAntivirusEnabled = CheckAntivirus(out string avName),
                    AntivirusName = avName,
                    IsFirewallEnabled = CheckFirewall(),
                    IsUacEnabled = CheckUac(),
                    IsWindowsUpdateEnabled = CheckWindowsUpdate()
                };
                return status;
            });
        }

        private bool CheckWindowsUpdate()
        {
            try
            {
                using var sc = new System.ServiceProcess.ServiceController("wuauserv");
                return sc.Status == System.ServiceProcess.ServiceControllerStatus.Running || 
                       sc.Status == System.ServiceProcess.ServiceControllerStatus.StartPending ||
                       sc.Status == System.ServiceProcess.ServiceControllerStatus.Stopped; // Windows Update service can be stopped but still "enabled"
                // Actually, if it's not disabled, it's "enabled".
            }
            catch { return false; }
        }

        private bool CheckAntivirus(out string name)
        {
            name = "No detectado";
            try
            {
                // WMI query to SecurityCenter2
                using (var searcher = new ManagementObjectSearcher(@"root\SecurityCenter2", "SELECT * FROM AntivirusProduct"))
                {
                    foreach (var result in searcher.Get())
                    {
                        name = result["displayName"]?.ToString() ?? "Desconocido";
                        
                        // productState is a bitmask. A state of 266240 (0x41000) is ON. A state of 262144 (0x40000) is OFF.
                        // The 12th bit (0x1000) often indicates enabled status.
                        if (result["productState"] != null)
                        {
                            if (int.TryParse(result["productState"].ToString(), out int state))
                            {
                                // Check if the second byte from the right is 16 (0x10), which means enabled and up to date.
                                if ((state & 0xFF00) == 0x1000)
                                {
                                    return true; // Found an active and up-to-date AV
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                // WMI query might fail on some systems. Silently fail for now.
                // _log.Warn("Could not query WMI for Antivirus status.", ex);
            }
            return false; // No active AV found
        }

        private bool CheckFirewall()
        {
            try
            {
                // First, try the modern WMI approach for Firewall status
                using (var searcher = new ManagementObjectSearcher(@"root\SecurityCenter2", "SELECT * FROM FirewallProduct"))
                {
                    foreach (var result in searcher.Get())
                    {
                        if (result["productState"] != null)
                        {
                             if (int.TryParse(result["productState"].ToString(), out int state))
                            {
                                if ((state & 0xFF00) == 0x1000)
                                {
                                    return true; // Found an active Firewall
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                // Fallback for older systems or if WMI fails: check the service status.
                try
                {
                     using var sc = new System.ServiceProcess.ServiceController("MpsSvc"); // Windows Defender Firewall service
                     return sc.Status == System.ServiceProcess.ServiceControllerStatus.Running;
                }
                catch { /* Service check also failed */ }
            }
            return false;
        }

        private bool CheckUac()
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", false);
                if (key != null)
                {
                    var val = key.GetValue("EnableLUA");
                    if (val is int i) return i == 1;
                }
            }
            catch { }
            return false;
        }
    }
}
