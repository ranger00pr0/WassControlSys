using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using WassControlSys.Models;

namespace WassControlSys.Core
{
    public class MonitoringService : IMonitoringService
    {
        private PerformanceCounter? _cpuCounter;
        private bool _cpuCounterAvailable;

        public MonitoringService()
        {
            try
            {
                _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total", true);
                // Primera lectura de cebado
                _ = _cpuCounter.NextValue();
                _cpuCounterAvailable = true;
            }
            catch
            {
                _cpuCounterAvailable = false;
                _cpuCounter?.Dispose();
                _cpuCounter = null;
            }
        }

        public SystemUsage GetSystemUsage()
        {
            double cpu = 0;
            try
            {
                if (_cpuCounterAvailable && _cpuCounter != null)
                {
                    cpu = Math.Clamp(_cpuCounter.NextValue(), 0, 100);
                }
            }
            catch
            {
                cpu = 0;
            }

            double ram = GetMemoryUsagePercent();
            double disk = GetSystemDriveUsagePercent();

            return new SystemUsage { CpuUsage = cpu, RamUsage = ram, DiskUsage = disk };
        }

        private static double GetSystemDriveUsagePercent()
        {
            try
            {
                string systemRoot = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
                var root = Path.GetPathRoot(systemRoot);
                if (string.IsNullOrWhiteSpace(root)) return 0;
                DriveInfo di = new DriveInfo(root);
                long total = di.TotalSize;
                long free = di.AvailableFreeSpace;
                long used = total - free;
                if (total <= 0) return 0;
                return used * 100.0 / total;
            }
            catch
            {
                return 0;
            }
        }

        private static double GetMemoryUsagePercent()
        {
            try
            {
                MEMORYSTATUSEX mem = new MEMORYSTATUSEX();
                mem.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
                if (GlobalMemoryStatusEx(ref mem))
                {
                    ulong total = mem.ullTotalPhys;
                    ulong avail = mem.ullAvailPhys;
                    if (total == 0) return 0;
                    double used = (total - avail) * 100.0 / total;
                    return used;
                }
            }
            catch { }
            return 0;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

        public void Dispose()
        {
            _cpuCounter?.Dispose();
        }
    }
}
