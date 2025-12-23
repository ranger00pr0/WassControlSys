using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WassControlSys.Models;

namespace WassControlSys.Core
{
    public class DiskHealthService : IDiskHealthService
    {
        public async Task<IEnumerable<DiskHealthInfo>> GetDiskHealthAsync()
        {
            return await Task.Run(() =>
            {
                var list = new List<DiskHealthInfo>();
                try
                {
                    var smartEntries = GetSmartEntries();
                    using var drives = new System.Management.ManagementObjectSearcher("SELECT DeviceID, Model, SerialNumber, PNPDeviceID, Index, Size FROM Win32_DiskDrive");
                    foreach (var d in drives.Get())
                    {
                        var id = d["DeviceID"]?.ToString() ?? "";
                        var model = d["Model"]?.ToString() ?? "";
                        var serial = d["SerialNumber"]?.ToString() ?? "";
                        var pnpDeviceId = d["PNPDeviceID"]?.ToString();
                        int? index = null;
                        try { if (d["Index"] != null) index = Convert.ToInt32(d["Index"]); } catch { }
                        long? sizeBytes = null;
                        try { if (d["Size"] != null) sizeBytes = Convert.ToInt64(d["Size"]); } catch { }

                        var status = GetSmartStatus(smartEntries, index, pnpDeviceId);
                        list.Add(new DiskHealthInfo
                        {
                            DeviceId = id,
                            Model = model,
                            Serial = serial,
                            Capacity = sizeBytes.HasValue ? FormatBytes(sizeBytes.Value) : "",
                            SmartOk = status.HasValue && status.Value,
                            SmartStatusKnown = status.HasValue,
                            SmartStatus = status.HasValue ? (status.Value ? "OK" : "FALLA") : "N/D"
                        });
                    }
                }
                catch { }
                return list;
            });
        }

        private static List<(string InstanceName, bool? SmartOk)> GetSmartEntries()
        {
            try
            {
                using var searcher = new System.Management.ManagementObjectSearcher(@"root\WMI", "SELECT PredictFailure, InstanceName FROM MSStorageDriver_FailurePredictStatus");
                var list = new List<(string InstanceName, bool? SmartOk)>();
                foreach (var mo in searcher.Get())
                {
                    var instance = mo["InstanceName"]?.ToString() ?? "";
                    bool? ok = null;
                    try
                    {
                        var pf = mo["PredictFailure"];
                        if (pf != null) ok = Convert.ToInt32(pf) == 0;
                    }
                    catch { }
                    list.Add((instance, ok));
                }
                return list;
            }
            catch
            {
                return new List<(string InstanceName, bool? SmartOk)>();
            }
        }

        private static bool? GetSmartStatus(List<(string InstanceName, bool? SmartOk)> entries, int? diskIndex, string? pnpDeviceId)
        {
            try
            {
                if (entries.Count == 0) return null;

                if (diskIndex.HasValue)
                {
                    string suffix = "_" + diskIndex.Value.ToString();
                    foreach (var e in entries)
                    {
                        if (e.InstanceName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                        {
                            return e.SmartOk;
                        }
                    }
                }

                if (!string.IsNullOrWhiteSpace(pnpDeviceId))
                {
                    string pnpNorm = NormalizeForMatch(pnpDeviceId);
                    foreach (var e in entries)
                    {
                        if (NormalizeForMatch(e.InstanceName).Contains(pnpNorm, StringComparison.OrdinalIgnoreCase))
                        {
                            return e.SmartOk;
                        }
                    }
                }
            }
            catch { }
            return null;
        }

        private static string NormalizeForMatch(string s)
        {
            return new string(s.Where(char.IsLetterOrDigit).ToArray());
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
