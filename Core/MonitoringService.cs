using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using WassControlSys.Models;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace WassControlSys.Core
{
    public class MonitoringService : IMonitoringService
    {
        private PerformanceCounter? _cpuCounter;
        private PerformanceCounter[] _cpuCoreCounters = Array.Empty<PerformanceCounter>();
        private PerformanceCounter? _diskReadsPerSecTotal; // Renamed to avoid confusion with per-disk counters
        private PerformanceCounter? _diskWritesPerSecTotal; // Renamed
        private PerformanceCounter? _diskAvgQueueLen;
        private PerformanceCounter? _diskSecPerRead;
        private PerformanceCounter? _diskSecPerWrite;
        private PerformanceCounter[] _netSentCounters = Array.Empty<PerformanceCounter>();
        private PerformanceCounter[] _netRecvCounters = Array.Empty<PerformanceCounter>();
        private bool _cpuCounterAvailable;

        // New fields for per-disk performance counters
        private Dictionary<string, PerformanceCounter>? _perDiskReadsPerSec;
        private Dictionary<string, PerformanceCounter>? _perDiskWritesPerSec;

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
                    _diskReadsPerSecTotal = new PerformanceCounter("PhysicalDisk", "Disk Read Bytes/sec", "_Total", true);
                    _diskWritesPerSecTotal = new PerformanceCounter("PhysicalDisk", "Disk Write Bytes/sec", "_Total", true);
                    _diskAvgQueueLen = new PerformanceCounter("PhysicalDisk", "Avg. Disk Queue Length", "_Total", true);
                    _diskSecPerRead = new PerformanceCounter("PhysicalDisk", "Avg. Disk sec/Read", "_Total", true);
                    _diskSecPerWrite = new PerformanceCounter("PhysicalDisk", "Avg. Disk sec/Write", "_Total", true);
                    _ = _diskReadsPerSecTotal.NextValue();
                    _ = _diskWritesPerSecTotal.NextValue();
                    _ = _diskAvgQueueLen.NextValue();
                    _ = _diskSecPerRead.NextValue();
                    _ = _diskSecPerWrite.NextValue();

                    // Initialize per-disk performance counters
                    _perDiskReadsPerSec = new Dictionary<string, PerformanceCounter>();
                    _perDiskWritesPerSec = new Dictionary<string, PerformanceCounter>();
                    var physicalDiskCategory = new PerformanceCounterCategory("PhysicalDisk");
                    foreach (var instanceName in physicalDiskCategory.GetInstanceNames().Where(n => n != "_Total"))
                    {
                        string driveLetter = ExtractDriveLetterFromPhysicalDiskInstance(instanceName);

                        if (!string.IsNullOrEmpty(driveLetter) && !_perDiskReadsPerSec.ContainsKey(driveLetter)) // Check if already added
                        {
                            try
                            {
                                var readCounter = new PerformanceCounter("PhysicalDisk", "Disk Read Bytes/sec", instanceName, true);
                                var writeCounter = new PerformanceCounter("PhysicalDisk", "Disk Write Bytes/sec", instanceName, true);

                                _perDiskReadsPerSec[driveLetter] = readCounter;
                                _perDiskWritesPerSec[driveLetter] = writeCounter;

                                // Prime the counters
                                _ = readCounter.NextValue();
                                _ = writeCounter.NextValue();
                            }
                            catch (Exception ex)
                            {
                                // A proper logging mechanism should be used here, this is a placeholder
                                Debug.WriteLine($"Error initializing per-disk counters for {instanceName}: {ex.Message}");
                            }
                        }
                    }
                }
                catch (Exception ex) {
                     Debug.WriteLine($"Error initializing total disk counters: {ex.Message}");
                }
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
            catch (Exception ex)
            {
                // General error during monitoring service initialization
                Debug.WriteLine($"Error initializing MonitoringService: {ex.Message}");
                _cpuCounterAvailable = false;
                _cpuCounter?.Dispose();
                _cpuCounter = null;
            }
        }

        public async Task<SystemUsage> GetSystemUsageAsync()
        {
            return await Task.Run(() =>
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
                    usage.DiskReadsPerSec = _diskReadsPerSecTotal?.NextValue() ?? 0;
                    usage.DiskWritesPerSec = _diskWritesPerSecTotal?.NextValue() ?? 0;
                    usage.DiskAvgQueueLength = _diskAvgQueueLen?.NextValue() ?? 0;
                    usage.DiskReadLatencyMs = (_diskSecPerRead?.NextValue() ?? 0) * 1000.0;
                    usage.DiskWriteLatencyMs = (_diskSecPerWrite?.NextValue() ?? 0) * 1000.0;

                    // Populate per-disk performance infos
                    if (_perDiskReadsPerSec != null && _perDiskWritesPerSec != null)
                    {
                        foreach (var entry in _perDiskReadsPerSec)
                        {
                            var driveLetter = entry.Key;
                            var readCounter = entry.Value;
                            if (_perDiskWritesPerSec.TryGetValue(driveLetter, out var writeCounter))
                            {
                                try
                                {
                                    usage.DiskPerformanceInfos.Add(new DiskPerformanceInfo
                                    {
                                        DriveLetter = driveLetter,
                                        InstanceName = readCounter.InstanceName, // Store instance name for potential future use
                                        ReadBytesPerSec = readCounter.NextValue(),
                                        WriteBytesPerSec = writeCounter.NextValue()
                                    });
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine($"Error getting per-disk performance for {driveLetter}: {ex.Message}");
                                }
                            }
                        }
                    }
                }
                catch (Exception ex) {
                     Debug.WriteLine($"Error getting total disk counters: {ex.Message}");
                }
                try
                {
                    usage.NetBytesSentPerSec = _netSentCounters.Sum(c => c.NextValue());
                    usage.NetBytesReceivedPerSec = _netRecvCounters.Sum(c => c.NextValue());
                    usage.ActiveTcpConnections = GetActiveTcpConnectionsCount();
                }
                catch { }
                return usage;
            });
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

        private string ExtractDriveLetterFromPhysicalDiskInstance(string instanceName)
        {
            // PhysicalDisk instance names can be like "0 C:", "1 D:", "_Total"
            // We need to extract "C:", "D:"
            // Regex to find patterns like "X:" (where X is a letter)
            var match = Regex.Match(instanceName, @"\b([A-Z]):");
            if (match.Success)
            {
                return match.Groups[1].Value + ":"; // Return "C:", "D:"
            }

            // Fallback for cases where instance name might not contain drive letter
            // This happens for physical disks that don't have a drive letter assigned (e.g., system reserved, EFI, recovery partitions)
            // Or for physical disk instances that are not logical drives (e.g., Raid arrays, storage spaces)
            // For now, return empty. We might need more sophisticated WMI queries to map physical disk to logical drives if this becomes an issue.
            return string.Empty;
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
            _diskReadsPerSecTotal?.Dispose();
            _diskWritesPerSecTotal?.Dispose();
            _diskAvgQueueLen?.Dispose();
            _diskSecPerRead?.Dispose();
            _diskSecPerWrite?.Dispose();
            
            if (_perDiskReadsPerSec != null)
            {
                foreach (var counter in _perDiskReadsPerSec.Values) counter?.Dispose();
                _perDiskReadsPerSec.Clear();
            }
            if (_perDiskWritesPerSec != null)
            {
                foreach (var counter in _perDiskWritesPerSec.Values) counter?.Dispose();
                _perDiskWritesPerSec.Clear();
            }

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
                // Usar IPGlobalProperties es MUCHO más eficiente que netstat
                var properties = System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties();
                var connections = properties.GetActiveTcpConnections();
                return connections.Count(c => c.State == System.Net.NetworkInformation.TcpState.Established);
            }
            catch { return 0; }
        }
    }
}
