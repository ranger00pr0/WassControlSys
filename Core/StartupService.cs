using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Win32; // For Registry access
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
                    //    // To be implemented
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
                    //    // To be implemented
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
                // HKLM (Local Machine) requires admin, HKCU (Current User) does not
                // Reading from HKLM
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
                                        IsEnabled = true, // If it's in Run, it's considered enabled
                                        Type = StartupItemType.RegistryRun
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

                // Reading from HKCU
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
                                        IsEnabled = true, // If it's in Run, it's considered enabled
                                        Type = StartupItemType.RegistryRun
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
            // This is tricky. To "disable" a registry item without deleting it,
            // we'd typically move it to a "disabled" key or rename it.
            // For simplicity in this initial version, we'll assume enabling means ensuring it's in the Run key,
            // and disabling means removing it. This is destructive, so a warning would be needed in UI.
            _log.Warn($"SetRegistryStartupItemState for {item.Name} ({item.Type}): Currently not fully implemented/destructive.");
            _log.Warn("Enabling/disabling registry startup items currently involves adding/removing entries, which is a destructive action.");
            
            // This method would require administrative privileges to modify HKLM keys.
            // For now, return false as it's not fully implemented safely.
            return false;
        }

        private IEnumerable<StartupItem> GetStartupFolderItems()
        {
            var items = new List<StartupItem>();
            string[] startupPaths = {
                Environment.GetFolderPath(Environment.SpecialFolder.Startup), // Per-user Startup
                Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup) // All Users Startup
            };

            foreach (var path in startupPaths)
            {
                if (Directory.Exists(path))
                {
                    try
                    {
                        foreach (var file in Directory.EnumerateFiles(path, "*.*", SearchOption.TopDirectoryOnly))
                        {
                            // Filter for executables, shortcuts, etc.
                            string extension = Path.GetExtension(file).ToLowerInvariant();
                            if (new[] { ".exe", ".lnk", ".bat", ".vbs", ".cmd" }.Contains(extension))
                            {
                                items.Add(new StartupItem
                                {
                                    Name = Path.GetFileNameWithoutExtension(file),
                                    Path = file,
                                    IsEnabled = true, // If it's in the folder and a valid type, it's enabled
                                    Type = StartupItemType.StartupFolder
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
            // For simplicity, disabling would mean moving it out of the folder,
            // enabling would mean moving it back. This also requires careful handling
            // and potentially administrative privileges if in CommonStartup.
            _log.Warn($"SetStartupFolderItemState for {item.Name} ({item.Type}): Currently not fully implemented/destructive.");
            _log.Warn("Enabling/disabling startup folder items currently involves moving files, which is a destructive action.");
            return false;
        }
    }
}
