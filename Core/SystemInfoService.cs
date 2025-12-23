using System;
using System.Management;
using System.Linq;
using System.Threading.Tasks;
using System.Net.NetworkInformation;

namespace WassControlSys.Core
{
    public class SystemInfo
    {
        public string MachineName { get; set; } = "";
        public string OsVersion { get; set; } = "";
        public string Processor { get; set; } = "";
        public string TotalRam { get; set; } = "";
        public string Gpu { get; set; } = "";
        public string SystemDisk { get; set; } = "";
        public string BiosVersion { get; set; } = "";
        public string NetworkInfo { get; set; } = "";
        public string Uptime { get; set; } = "";

        public System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, string>> GeneralItems
        {
            get
            {
                yield return new System.Collections.Generic.KeyValuePair<string, string>("Equipo", MachineName);
                yield return new System.Collections.Generic.KeyValuePair<string, string>("S.O.", OsVersion);
                yield return new System.Collections.Generic.KeyValuePair<string, string>("Procesador", Processor);
                yield return new System.Collections.Generic.KeyValuePair<string, string>("Memoria RAM", TotalRam);
                yield return new System.Collections.Generic.KeyValuePair<string, string>("Gráficos", Gpu);
                yield return new System.Collections.Generic.KeyValuePair<string, string>("Disco", SystemDisk);
                yield return new System.Collections.Generic.KeyValuePair<string, string>("BIOS", BiosVersion);
                yield return new System.Collections.Generic.KeyValuePair<string, string>("Red", NetworkInfo);
                yield return new System.Collections.Generic.KeyValuePair<string, string>("Tiempo Activo", Uptime);
            }
        }
    }

    public class SystemInfoService : ISystemInfoService
    {
        public async Task<SystemInfo> GetSystemInfoAsync()
        {
            return await Task.Run(() =>
            {
                var info = new SystemInfo
                {
                    MachineName = Environment.MachineName,
                    OsVersion = $"{Environment.OSVersion} ({(Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit")})",
                    Processor = GetCpuName(),
                    TotalRam = GetTotalMemory(),
                    Gpu = GetGpuName(),
                    SystemDisk = GetSystemDiskInfo(),
                    BiosVersion = GetBiosVersion(),
                    NetworkInfo = GetNetworkInfo(),
                    Uptime = GetUptime()
                };
                return info;
            });
        }

        private string GetCpuName()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_Processor");
                foreach (var item in searcher.Get())
                {
                    return item["Name"]?.ToString() ?? "Desconocido";
                }
            }
            catch { }
            return Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER") ?? "Desconocido";
        }

        private string GetTotalMemory()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT Capacity FROM Win32_PhysicalMemory");
                long totalCapacity = 0;
                foreach (var item in searcher.Get())
                {
                    if (long.TryParse(item["Capacity"]?.ToString(), out long cap))
                    {
                        totalCapacity += cap;
                    }
                }
                if (totalCapacity > 0) return FormatBytes(totalCapacity);
            }
            catch { }
            return "Desconocido";
        }

        private string GetGpuName()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_VideoController");
                foreach (var item in searcher.Get())
                {
                    return item["Name"]?.ToString() ?? "Desconocido";
                    // Por ahora, solo devolver la primera GPU encontrada
                }
            }
            catch { }
            return "Desconocido";
        }

        private string GetSystemDiskInfo()
        {
            try
            {
                string systemDrive = System.IO.Path.GetPathRoot(Environment.SystemDirectory) ?? "C:\\";
                var di = new System.IO.DriveInfo(systemDrive);
                return $"{di.Name} {FormatBytes(di.AvailableFreeSpace)} libres de {FormatBytes(di.TotalSize)}";
            }
            catch { }
            return "Desconocido";
        }

        private string GetBiosVersion()
        {
             try
            {
                using var searcher = new ManagementObjectSearcher("SELECT SMBIOSBIOSVersion, Manufacturer FROM Win32_BIOS");
                foreach (var item in searcher.Get())
                {
                    string ver = item["SMBIOSBIOSVersion"]?.ToString() ?? "";
                    string man = item["Manufacturer"]?.ToString() ?? "";
                    return $"{man} {ver}".Trim();
                }
            }
            catch { }
            return "N/A";
        }

        private string GetNetworkInfo()
        {
            try
            {
                if (!NetworkInterface.GetIsNetworkAvailable()) return "Desconectado";
                
                var interfaces = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(i => i.OperationalStatus == OperationalStatus.Up && i.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                    .OrderByDescending(i => i.GetIPProperties().GatewayAddresses.Count > 0) // Priorizar los que tienen gateway
                    .FirstOrDefault();

                if (interfaces != null)
                {
                    var props = interfaces.GetIPProperties();
                    var ip = props.UnicastAddresses.FirstOrDefault(a => a.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
                    return $"{interfaces.Name} ({ip?.Address})";
                }
            }
            catch { }
            return "Desconocido";
        }

        private string GetUptime()
        {
            try
            {
                var uptime = TimeSpan.FromMilliseconds(Environment.TickCount64);
                return $"{uptime.Days}d {uptime.Hours}h {uptime.Minutes}m";
            }
            catch { }
            return "N/A";
        }

        private static string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }
}
