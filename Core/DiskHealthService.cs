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
                var smartEntries = GetSmartEntries(); // Se obtienen los datos SMART una sola vez

                try
                {
                    using var drives = new System.Management.ManagementObjectSearcher("SELECT DeviceID, Model, SerialNumber, PNPDeviceID, Index, Size FROM Win32_DiskDrive");
                    foreach (var d in drives.Get())
                    {
                        if (d == null) continue;

                        try
                        {
                            var id = d["DeviceID"]?.ToString() ?? "";
                            var model = d["Model"]?.ToString() ?? "";
                            var serial = d["SerialNumber"]?.ToString() ?? "";
                            var pnpDeviceId = d["PNPDeviceID"]?.ToString();
                            int? index = d["Index"] != null ? Convert.ToInt32(d["Index"]) : (int?)null;
                            long? sizeBytes = d["Size"] != null ? Convert.ToInt64(d["Size"]) : (long?)null;

                            var status = GetSmartStatus(smartEntries, index, pnpDeviceId);
                            list.Add(new DiskHealthInfo
                            {
                                DeviceId = id,
                                Model = model,
                                Serial = serial,
                                Capacity = sizeBytes.HasValue ? FormatBytes(sizeBytes.Value) : "",
                                SmartOk = status.HasValue && status.Value,
                                SmartStatusKnown = status.HasValue,
                                SmartStatus = status.HasValue ? (status.Value ? "OK" : "FALLA") : "N/D",
                                Temperature = GetDiskTemperature(index), // Populate temperature
                                PnpDeviceId = pnpDeviceId, // Populate new property
                                PhysicalDiskIndex = index // Populate new property
                            });
                        }
                        catch
                        {
                            // Error procesando un disco individual, lo ignoramos y continuamos con el siguiente
                            // Se podría añadir un log: Debug.WriteLine($"Error processing disk {d["DeviceID"]}: {ex.Message}");
                        }
                    }
                }
                catch
                {
                    // Error crítico al obtener la lista de discos
                    // Se podría añadir un log: Debug.WriteLine($"Critical error getting disk drives: {ex.Message}");
                }

                return list;
            });
        }

        private static int GetDiskTemperature(int? diskIndex)
        {
            if (!diskIndex.HasValue) return 0; // Return 0 if no disk index

            try
            {
                // Query for temperature from SMART data.
                // This typically involves querying a class like MSStorageDriver_ATAPISmartData
                // or a vendor-specific WMI class.
                // For simplicity, let's assume we can find a common way, or iterate to find relevant SMART attributes.

                // WMI path for SMART data (often specific to vendors or drivers)
                string query = $"SELECT CurrentTemperature FROM MSAcpi_ThermalZoneTemperature WHERE InstanceName LIKE '%PhysicalDisk{diskIndex.Value}%'";
                using var searcher = new System.Management.ManagementObjectSearcher(@"root\WMI", query);

                foreach (var mo in searcher.Get())
                {
                    if (mo["CurrentTemperature"] != null)
                    {
                        // Temperature is often returned in Kelvin by WMI, convert to Celsius
                        // Kelvin to Celsius: K - 273.15
                        return (int)(Convert.ToDouble(mo["CurrentTemperature"]) - 273.15);
                    }
                }
            }
            catch
            {
                // Fallback or log error
            }

            // Fallback to query Win32_TemperatureProbe (usually for CPU, but can sometimes show other temps)
            try
            {
                string query = $"SELECT CurrentReading FROM Win32_TemperatureProbe WHERE InstanceName LIKE '%Disk{diskIndex.Value}%'";
                using var searcher = new System.Management.ManagementObjectSearcher(@"root\CIMV2", query);
                 foreach (var mo in searcher.Get())
                {
                    if (mo["CurrentReading"] != null)
                    {
                        // Temperature is often returned in Celsius directly
                        return Convert.ToInt32(mo["CurrentReading"]);
                    }
                }
            }
            catch
            {
                // Fallback or log error
            }

            return 0; // Default to 0 if temperature cannot be found
        }

        private static List<(string InstanceName, bool? SmartOk)> GetSmartEntries()
        {
            var list = new List<(string InstanceName, bool? SmartOk)>();
            
            // Primer intento: MSStorageDriver_FailurePredictStatus (más común)
            try
            {
                using var searcher = new System.Management.ManagementObjectSearcher(@"root\WMI", "SELECT PredictFailure, InstanceName FROM MSStorageDriver_FailurePredictStatus");
                foreach (var mo in searcher.Get())
                {
                    var instance = mo["InstanceName"]?.ToString() ?? "";
                    bool? ok = null;
                    if (mo["PredictFailure"] != null)
                    {
                        ok = Convert.ToInt32(mo["PredictFailure"]) == 0;
                    }
                    if (!string.IsNullOrEmpty(instance)) list.Add((instance, ok));
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error al consultar MSStorageDriver_FailurePredictStatus. Esto puede indicar problemas con el repositorio WMI o drivers de disco. Detalle: " + ex.Message, ex);
            }

            // Si el primero no devolvió nada, segundo intento: MSStorageDriver_FailurePredictData
            if (list.Count == 0)
            {
                try
                {
                    using var searcher = new System.Management.ManagementObjectSearcher(@"root\WMI", "SELECT InstanceName, PredictFailure, VendorSpecific FROM MSStorageDriver_FailurePredictData");
                    foreach (var mo in searcher.Get())
                    {
                        var instance = mo["InstanceName"]?.ToString() ?? "";
                        bool? ok = null;
                        if (mo["PredictFailure"] != null)
                        {
                            ok = Convert.ToInt32(mo["PredictFailure"]) == 0;
                        }
                        if (!string.IsNullOrEmpty(instance)) list.Add((instance, ok));
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("Error al consultar MSStorageDriver_FailurePredictData. Detalle: " + ex.Message, ex);
                }
            }
            
            return list;
        }

        private static bool? GetSmartStatus(List<(string InstanceName, bool? SmartOk)> entries, int? diskIndex, string? pnpDeviceId)
        {
            if (entries.Count == 0) return null;

            try
            {
                // Intenta encontrar una coincidencia usando una consulta LINQ más clara.
                var entry = entries.FirstOrDefault(e =>
                {
                    // Criterio 1: Coincidencia por índice de disco (más fiable cuando está disponible)
                    if (diskIndex.HasValue && e.InstanceName.EndsWith($"_{diskIndex.Value}", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }

                    // Criterio 2: Coincidencia por PnpDeviceID (como fallback)
                    if (!string.IsNullOrWhiteSpace(pnpDeviceId))
                    {
                        string pnpNorm = NormalizeForMatch(pnpDeviceId);
                        if (!string.IsNullOrEmpty(pnpNorm) && NormalizeForMatch(e.InstanceName).Contains(pnpNorm, StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                    
                    return false;
                });

                // Si se encontró una entrada (incluso si es el default, que sería null), devuelve su estado.
                // Si no se encontró ninguna coincidencia, `entry` será el valor por defecto de la tupla, y `entry.SmartOk` será null.
                return entry.SmartOk;
            }
            catch
            {
                return null;
            }
        }

        private static string NormalizeForMatch(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            // Elimina caracteres no alfanuméricos para una coincidencia más flexible.
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
