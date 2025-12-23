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
            var detectedAVs = new System.Collections.Generic.List<string>();
            bool anyActive = false;
            try
            {
                // WMI query to SecurityCenter2
                using (var searcher = new ManagementObjectSearcher(@"root\SecurityCenter2", "SELECT * FROM AntivirusProduct"))
                {
                    foreach (var result in searcher.Get())
                    {
                        string avName = result["displayName"]?.ToString() ?? "Desconocido";
                        detectedAVs.Add(avName);
                        
                        if (result["productState"] != null)
                        {
                            if (int.TryParse(result["productState"].ToString(), out int state))
                            {
                                // The second byte of productState indicates the security status
                                // 0x10 means "On"
                                int secondByte = (state >> 8) & 0xFF;
                                if (secondByte == 0x10)
                                {
                                    anyActive = true;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the exception. For now, we'll just write to a file.
                System.IO.File.WriteAllText("error_antivirus.txt", ex.ToString());
            }

            if (detectedAVs.Count > 0)
            {
                name = string.Join(" + ", detectedAVs);
                return anyActive;
            }

            name = "No detectado";
            return false;
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
                                // The second byte of productState indicates the security status
                                // 0x10 means "On"
                                int secondByte = (state >> 8) & 0xFF;
                                if (secondByte == 0x10)
                                {
                                    return true; // Found an active Firewall
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the exception. For now, we'll just write to a file.
                System.IO.File.WriteAllText("error_firewall.txt", ex.ToString());
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
