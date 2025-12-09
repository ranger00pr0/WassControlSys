using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Win32;
using WassControlSys.Models;

namespace WassControlSys.Core
{
    public class PrivacyService : IPrivacyService
    {
        private readonly ILogService _log;
        private readonly IDialogService _dialogService;

        public PrivacyService(ILogService log, IDialogService dialogService)
        {
            _log = log;
            _dialogService = dialogService;
        }

        public async Task<IEnumerable<PrivacySetting>> GetPrivacySettingsAsync()
        {
            return await Task.Run(() =>
            {
                var settings = new List<PrivacySetting>();

                // Example: Telemetry
                settings.Add(GetTelemetrySetting());
                settings.Add(GetAdvertisingIdSetting());

                // Add more settings here
                
                return settings;
            });
        }

        public async Task<bool> UpdatePrivacySettingAsync(PrivacySetting setting, bool newValue)
        {
            return await Task.Run(async () =>
            {
                if (setting == null) return false;

                try
                {
                    _log.Info($"Updating privacy setting '{setting.Name}' to '{newValue}'");

                    // A warning for critical settings
                    if (setting.Type == PrivacySettingType.Telemetry && newValue == true)
                    {
                        bool confirm = await _dialogService.ShowConfirmation($"Habilitar la telemetría podría enviar datos de uso a Microsoft. ¿Está seguro?", "Confirmar");
                        if (!confirm) return false;
                    }
                    if (setting.Type == PrivacySettingType.AdvertisingId && newValue == true)
                    {
                        bool confirm = await _dialogService.ShowConfirmation($"Habilitar el ID de publicidad permitirá que las aplicaciones utilicen su actividad para anuncios personalizados. ¿Está seguro?", "Confirmar");
                        if (!confirm) return false;
                    }

                    using (RegistryKey key = Registry.CurrentUser.CreateSubKey(setting.RegistryPath, true))
                    {
                        if (key != null)
                        {
                            key.SetValue(setting.RegistryValueName, newValue ? 1 : 0, RegistryValueKind.DWord);
                            setting.CurrentValue = newValue; // Update model for UI
                            _log.Info($"Privacy setting '{setting.Name}' updated successfully.");
                            return true;
                        }
                    }
                    _log.Warn($"Could not open/create registry key for setting '{setting.Name}' at '{setting.RegistryPath}'");
                    return false;
                }
                catch (Exception ex)
                {
                    _log.Error($"Error updating privacy setting '{setting.Name}'", ex);
                    return false;
                }
            });
        }

        private PrivacySetting GetTelemetrySetting()
        {
            string path = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Privacy";
            string valueName = "AllowTelemetry"; // Simplified example
            int currentValue = 1; // Default to on

            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(path))
                {
                    if (key != null)
                    {
                        currentValue = (int)(key.GetValue(valueName) ?? 1);
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Warn($"Could not read Telemetry setting: {ex.Message}");
            }

            return new PrivacySetting
            {
                Name = "Telemetría de Windows",
                Description = "Controla la cantidad de datos de diagnóstico y uso enviados a Microsoft.",
                CurrentValue = currentValue == 1,
                RecommendedValue = false, // Recommend disabling for privacy
                Type = PrivacySettingType.Telemetry,
                RegistryPath = path,
                RegistryValueName = valueName
            };
        }

        private PrivacySetting GetAdvertisingIdSetting()
        {
            string path = @"SOFTWARE\Microsoft\Windows\CurrentVersion\AdvertisingInfo";
            string valueName = "Enabled"; // Simplified example
            int currentValue = 1; // Default to on

            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(path))
                {
                    if (key != null)
                    {
                        currentValue = (int)(key.GetValue(valueName) ?? 1);
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Warn($"Could not read Advertising ID setting: {ex.Message}");
            }

            return new PrivacySetting
            {
                Name = "ID de Publicidad Personalizada",
                Description = "Permite que las aplicaciones usen su actividad para mostrarle anuncios personalizados.",
                CurrentValue = currentValue == 1,
                RecommendedValue = false, // Recommend disabling for privacy
                Type = PrivacySettingType.AdvertisingId,
                RegistryPath = path,
                RegistryValueName = valueName
            };
        }
    }
}
