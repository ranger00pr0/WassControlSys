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
        private PerformanceCounter[] _cpuCoreCounters = Array.Empty<PerformanceCounter>();
        private PerformanceCounter? _diskReadsPerSec;
        private PerformanceCounter? _diskWritesPerSec;
        private PerformanceCounter? _diskAvgQueueLen;
        private PerformanceCounter? _diskSecPerRead;
        private PerformanceCounter? _diskSecPerWrite;
        private PerformanceCounter[] _netSentCounters = Array.Empty<PerformanceCounter>();
        private PerformanceCounter[] _netRecvCounters = Array.Empty<PerformanceCounter>();
        private bool _cpuCounterAvailable;

        public MonitoringService()
        {
            try
            {
                _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total", true);
                // Primera lectura de cebado
                _ = _cpuCounter.NextValue();
                _cpuCounterAvailable = true;
                try
                {
                    var category = new PerformanceCounterCategory("Processor");
                    var instances = category.GetInstanceNames().Where(n => n != "_Total").ToArray();
                    _cpuCoreCounters = instances.Select(n => new PerformanceCounter("Processor", "% Processor Time", n, true)).ToArray();
                    foreach (var c in _cpuCoreCounters) _ = c.NextValue();
                }
                catch { _cpuCoreCounters = Array.Empty<PerformanceCounter>(); }
                try
                {
                    _diskReadsPerSec = new PerformanceCounter("PhysicalDisk", "Disk Reads/sec", "_Total", true);
                    _diskWritesPerSec = new PerformanceCounter("PhysicalDisk", "Disk Writes/sec", "_Total", true);
                    _diskAvgQueueLen = new PerformanceCounter("PhysicalDisk", "Avg. Disk Queue Length", "_Total", true);
                    _diskSecPerRead = new PerformanceCounter("PhysicalDisk", "Avg. Disk sec/Read", "_Total", true);
                    _diskSecPerWrite = new PerformanceCounter("PhysicalDisk", "Avg. Disk sec/Write", "_Total", true);
                    _ = _diskReadsPerSec.NextValue();
                    _ = _diskWritesPerSec.NextValue();
                    _ = _diskAvgQueueLen.NextValue();
                    _ = _diskSecPerRead.NextValue();
                    _ = _diskSecPerWrite.NextValue();
                }
                catch { }
                try
                {
                    var nic = new PerformanceCounterCategory("Network Interface");
                    var nics = nic.GetInstanceNames();
                    _netSentCounters = nics.Select(n => new PerformanceCounter("Network Interface", "Bytes Sent/sec", n, true)).ToArray();
                    _netRecvCounters = nics.Select(n => new PerformanceCounter("Network Interface", "Bytes Received/sec", n, true)).ToArray();
                    foreach (var c in _netSentCounters) _ = c.NextValue();
                    foreach (var c in _netRecvCounters) _ = c.NextValue();
                }
                catch { _netSentCounters = Array.Empty<PerformanceCounter>(); _netRecvCounters = Array.Empty<PerformanceCounter>(); }
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

            var usage = new SystemUsage { CpuUsage = cpu, RamUsage = ram, DiskUsage = disk };
            try
            {
                if (_cpuCoreCounters.Length > 0)
                {
                    usage.CpuPerCore = _cpuCoreCounters.Select(c => (double)Math.Clamp(c.NextValue(), 0, 100)).ToArray();
                }
            }
            catch { }
            try
            {
                usage.DiskReadsPerSec = _diskReadsPerSec?.NextValue() ?? 0;
                usage.DiskWritesPerSec = _diskWritesPerSec?.NextValue() ?? 0;
                usage.DiskAvgQueueLength = _diskAvgQueueLen?.NextValue() ?? 0;
                usage.DiskReadLatencyMs = (_diskSecPerRead?.NextValue() ?? 0) * 1000.0;
                usage.DiskWriteLatencyMs = (_diskSecPerWrite?.NextValue() ?? 0) * 1000.0;
            }
            catch { }
            try
            {
                usage.NetBytesSentPerSec = _netSentCounters.Sum(c => c.NextValue());
                usage.NetBytesReceivedPerSec = _netRecvCounters.Sum(c => c.NextValue());
                usage.ActiveTcpConnections = GetActiveTcpConnectionsCount();
            }
            catch { }
            return usage;
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
            foreach (var c in _cpuCoreCounters) c?.Dispose();
            _diskReadsPerSec?.Dispose();
            _diskWritesPerSec?.Dispose();
            _diskAvgQueueLen?.Dispose();
            _diskSecPerRead?.Dispose();
            _diskSecPerWrite?.Dispose();
            foreach (var c in _netSentCounters) c?.Dispose();
            foreach (var c in _netRecvCounters) c?.Dispose();
        }

        public TimeSpan GetIdleTime()
        {
            LASTINPUTINFO lastInputInfo = new LASTINPUTINFO();
            lastInputInfo.cbSize = (uint)Marshal.SizeOf(lastInputInfo);
            lastInputInfo.dwTime = 0;

            if (GetLastInputInfo(ref lastInputInfo))
            {
                uint lastInputTick = lastInputInfo.dwTime;
                uint idleTicks = (uint)Environment.TickCount - lastInputTick;
                return TimeSpan.FromMilliseconds(idleTicks);
            }
            return TimeSpan.Zero;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct LASTINPUTINFO
        {
            public uint cbSize;
            public uint dwTime;
        }

        [DllImport("user32.dll")]
        private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        private int GetActiveTcpConnectionsCount()
        {
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "netstat",
                    Arguments = "-an",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };
                using var p = System.Diagnostics.Process.Start(psi);
                if (p == null) return 0;
                string output = p.StandardOutput.ReadToEnd();
                p.WaitForExit();
                int count = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Count(l => l.Contains("TCP") && l.Contains("ESTABLISHED"));
                return count;
            }
            catch { return 0; }
        }
    }
}
