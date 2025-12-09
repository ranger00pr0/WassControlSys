using System;
using System.Management;
using Microsoft.Win32;
using System.Threading.Tasks;

namespace WassControlSys.Core
{
    public class SecurityStatus
    {
        public string AntivirusName { get; set; } = "Desconocido";
        public bool IsAntivirusEnabled { get; set; } = false;
        public bool IsFirewallEnabled { get; set; } = false;
        public bool IsUacEnabled { get; set; } = false;
        
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
                    IsUacEnabled = CheckUac()
                };
                return status;
            });
        }

        private bool CheckAntivirus(out string name)
        {
            name = "No detectado";
            bool enabled = false;
            try
            {
                // WMI query to SecurityCenter2
                using var searcher = new ManagementObjectSearcher(@"root\SecurityCenter2", "SELECT * FROM AntivirusProduct");
                foreach (var result in searcher.Get())
                {
                    name = result["displayName"]?.ToString() ?? "Desconocido";
                    
                    // productState is a bitmask. 
                    // Usually, if bit 12 (0x1000) is set, it's ON. 
                    // Simply checking if it exists is a good first step, but let's try to parse status.
                    // Note: This is a simplification.
                    string hexState = "";
                    if (result["productState"] != null)
                    {
                        int state = Convert.ToInt32(result["productState"]);
                        // Standard check for "On" (heuristic)
                        // 0x1000 = On, 0x0000 = Off. 
                        // But different vendors use different flags. 
                        // We will assume if ANY AV is reported here, it is likely the active one.
                        // A more robust check might be needed for specific vendors.
                        enabled = true; 
                        hexState = state.ToString("X");
                    }
                    
                    // Just take the first one found
                    break;
                }
            }
            catch 
            {
                // Fallback or permission issue
            }
            return enabled;
        }

        private bool CheckFirewall()
        {
            // Simple check via Registry or WMI. 
            // Using WMI "root\StandardCimv2" -> MSFT_NetFirewallProfile is cleaner but requires newer Win versions.
            // Let's stick to Registry for broader compatibility or assumptions.
            // Check Domain, Public, Standard profiles in registry is complex.
            // A simpler heuristic: Check if the service 'MpsSvc' (Windows Firewall) is running.
            try
            {
                 using var sc = new System.ServiceProcess.ServiceController("MpsSvc");
                 return sc.Status == System.ServiceProcess.ServiceControllerStatus.Running;
            }
            catch
            {
                return false;
            }
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
