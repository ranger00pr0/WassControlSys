using System;
using System.Management;
using System.Linq;
using System.Threading.Tasks;

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
                    SystemDisk = GetSystemDiskInfo()
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
                    // Just return the first GPU found for now
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
