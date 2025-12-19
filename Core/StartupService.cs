using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Win32; // Para acceso al Registro
using WassControlSys.Models;

namespace WassControlSys.Core
{
    public class StartupService : IStartupService
    {
        private readonly ILogService _log;

        public StartupService(ILogService log)
        {
            _log = log;
        }

        public async Task<IEnumerable<StartupItem>> GetStartupItemsAsync()
        {
            return await Task.Run(() =>
            {
                var items = new List<StartupItem>();
                items.AddRange(GetRegistryStartupItems());
                items.AddRange(GetStartupFolderItems());
                return items;
            });
        }

        public async Task<bool> EnableStartupItemAsync(StartupItem item)
        {
            return await Task.Run(() =>
            {
                _log.Info($"Attempting to enable startup item: {item.Name} ({item.Type})");
                switch (item.Type)
                {
                    case StartupItemType.RegistryRun:
                        return SetRegistryStartupItemState(item, true);
                    case StartupItemType.StartupFolder:
                        return SetStartupFolderItemState(item, true);
                    //case StartupItemType.TaskScheduler:
                    // Pendiente de implementar
                    //    _log.Warn($"Task Scheduler item enabling not yet supported: {item.Name}");
                    //    return false;
                    default:
                        _log.Warn($"Unsupported startup item type for enabling: {item.Type}");
                        return false;
                }
            });
        }

        public async Task<bool> DisableStartupItemAsync(StartupItem item)
        {
            return await Task.Run(() =>
            {
                _log.Info($"Attempting to disable startup item: {item.Name} ({item.Type})");
                switch (item.Type)
                {
                    case StartupItemType.RegistryRun:
                        return SetRegistryStartupItemState(item, false);
                    case StartupItemType.StartupFolder:
                        return SetStartupFolderItemState(item, false);
                    //case StartupItemType.TaskScheduler:
                    // Pendiente de implementar
                    //    _log.Warn($"Task Scheduler item disabling not yet supported: {item.Name}");
                    //    return false;
                    default:
                        _log.Warn($"Unsupported startup item type for disabling: {item.Type}");
                        return false;
                }
            });
        }

        private IEnumerable<StartupItem> GetRegistryStartupItems()
        {
            var items = new List<StartupItem>();
            string[] runKeys = {
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run",
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce"
            };

            foreach (var keyPath in runKeys)
            {
                // HKLM (Máquina Local) requiere administrador, HKCU (Usuario Actual) no
                // Leyendo de HKLM
                try
                {
                    using (var baseKey = Registry.LocalMachine.OpenSubKey(keyPath))
                    {
                        if (baseKey != null)
                        {
                            foreach (string valueName in baseKey.GetValueNames())
                            {
                                var value = baseKey.GetValue(valueName);
                                if (value is string path)
                                {
                                    items.Add(new StartupItem
                                    {
                                        Name = valueName,
                                        Path = path,
                                        IsEnabled = true, // Si está en Run, se considera habilitado
                                        Type = StartupItemType.RegistryRun,
                                        SourceKeyPath = keyPath,
                                        IsMachineWide = true,
                                        ImpactScore = EstimateImpact(path)
                                    });
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _log.Warn($"Could not read HKLM registry key {keyPath}: {ex.Message}");
                }

                // Leyendo de HKCU
                try
                {
                    using (var baseKey = Registry.CurrentUser.OpenSubKey(keyPath))
                    {
                        if (baseKey != null)
                        {
                            foreach (string valueName in baseKey.GetValueNames())
                            {
                                var value = baseKey.GetValue(valueName);
                                if (value is string path)
                                {
                                    items.Add(new StartupItem
                                    {
                                        Name = valueName,
                                        Path = path,
                                        IsEnabled = true, // Si está en Run, se considera habilitado
                                        Type = StartupItemType.RegistryRun,
                                        SourceKeyPath = keyPath,
                                        IsMachineWide = false,
                                        ImpactScore = EstimateImpact(path)
                                    });
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _log.Warn($"Could not read HKCU registry key {keyPath}: {ex.Message}");
                }
            }
            return items;
        }

        private bool SetRegistryStartupItemState(StartupItem item, bool enable)
        {
            try
            {
                RegistryKey root = item.IsMachineWide ? Registry.LocalMachine : Registry.CurrentUser;
                string runKey = item.SourceKeyPath;
                string disabledKey = runKey + "Disabled";
                using var enabled = root.OpenSubKey(runKey, true) ?? root.CreateSubKey(runKey, true);
                using var disabled = root.OpenSubKey(disabledKey, true) ?? root.CreateSubKey(disabledKey, true);
                if (enabled == null || disabled == null) return false;
                if (enable)
                {
                    var val = disabled.GetValue(item.Name) as string;
                    if (string.IsNullOrEmpty(val)) return false;
                    enabled.SetValue(item.Name, val);
                    disabled.DeleteValue(item.Name, false);
                    return true;
                }
                else
                {
                    var val = enabled.GetValue(item.Name) as string;
                    if (string.IsNullOrEmpty(val)) return false;
                    disabled.SetValue(item.Name, val);
                    enabled.DeleteValue(item.Name, false);
                    return true;
                }
            }
            catch (Exception ex)
            {
                _log.Error($"Error toggling registry startup item {item.Name}", ex);
                return false;
            }
        }

        private IEnumerable<StartupItem> GetStartupFolderItems()
        {
            var items = new List<StartupItem>();
            string[] startupPaths = {
                Environment.GetFolderPath(Environment.SpecialFolder.Startup), // Inicio por usuario
                Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup) // Inicio para todos los usuarios
            };

            foreach (var path in startupPaths)
            {
                if (Directory.Exists(path))
                {
                    try
                    {
                        foreach (var file in Directory.EnumerateFiles(path, "*.*", SearchOption.TopDirectoryOnly))
                        {
                            // Filtrar ejecutables, accesos directos, etc.
                            string extension = Path.GetExtension(file).ToLowerInvariant();
                            if (new[] { ".exe", ".lnk", ".bat", ".vbs", ".cmd" }.Contains(extension))
                            {
                                items.Add(new StartupItem
                                {
                                    Name = Path.GetFileNameWithoutExtension(file),
                                    Path = file,
                                    IsEnabled = true, // Si está en la carpeta y es un tipo válido, está habilitado
                                    Type = StartupItemType.StartupFolder,
                                    SourceKeyPath = path,
                                    IsMachineWide = path.Equals(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup), StringComparison.OrdinalIgnoreCase),
                                    ImpactScore = EstimateImpact(file)
                                });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _log.Warn($"Could not enumerate startup folder {path}: {ex.Message}");
                    }
                }
            }
            return items;
        }

        private bool SetStartupFolderItemState(StartupItem item, bool enable)
        {
            try
            {
                string folder = item.SourceKeyPath;
                if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder)) return false;
                string disabledFolder = Path.Combine(folder, "Disabled");
                Directory.CreateDirectory(disabledFolder);
                string src = item.Path;
                string dst = Path.Combine(disabledFolder, Path.GetFileName(item.Path));
                if (enable)
                {
                    if (File.Exists(dst))
                    {
                        string back = Path.Combine(folder, Path.GetFileName(item.Path));
                        File.Move(dst, back, true);
                        return true;
                    }
                    return false;
                }
                else
                {
                    if (File.Exists(src))
                    {
                        File.Move(src, dst, true);
                        return true;
                    }
                    return false;
                }
            }
            catch (Exception ex)
            {
                _log.Error($"Error toggling startup folder item {item.Name}", ex);
                return false;
            }
        }

        private static double EstimateImpact(string path)
        {
            try
            {
                var fi = new FileInfo(path);
                if (!fi.Exists) return 0.2;
                double sizeMb = fi.Length / (1024.0 * 1024.0);
                if (sizeMb > 100) return 0.9;
                if (sizeMb > 50) return 0.7;
                if (sizeMb > 10) return 0.5;
                return 0.3;
            }
            catch { return 0.2; }
        }
    }
}
