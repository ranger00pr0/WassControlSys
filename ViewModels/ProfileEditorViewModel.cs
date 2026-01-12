using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using WassControlSys.Core;
using WassControlSys.Models;

#nullable enable

namespace WassControlSys.ViewModels
{
    public class ProfileEditorViewModel : INotifyPropertyChanged
    {
        private readonly ISettingsService _settingsService;
        private readonly ILogService _log;
        private readonly IDialogService _dialogService;
        private AppSettings? _currentSettings;
        private bool _isBusy;
        public bool IsBusy { get => _isBusy; set { _isBusy = value; OnPropertyChanged(); } }
        private string _statusMessage = "";
        public string StatusMessage { get => _statusMessage; set { _statusMessage = value; OnPropertyChanged(); } }

        private PerformanceMode _profileToEdit = PerformanceMode.Gamer;
        public PerformanceMode ProfileToEdit
        {
            get => _profileToEdit;
            set 
            { 
                if (_profileToEdit != value) 
                { 
                    _profileToEdit = value; 
                    OnPropertyChanged(); 
                    OnPropertyChanged(nameof(CurrentProfileTitle));
                    OnPropertyChanged(nameof(CurrentProfileIcon));
                    OnPropertyChanged(nameof(ProfileThemeBrush));
                    OnPropertyChanged(nameof(ProfileHeaderGradient));
                    OnPropertyChanged(nameof(IsEditingAllowed)); // Notify change
                    LoadProfileConfig(value); 
                } 
            }
        }

        public string ProfileThemeBrush => ProfileToEdit switch
        {
            PerformanceMode.Gamer => "#EF4444", // Rojo Gamer
            PerformanceMode.Dev => "#8B5CF6",   // Violeta Dev
            PerformanceMode.Oficina => "#10B981", // Esmeralda Oficina
            _ => "#3B82F6" // Azul Personalizado
        };

        public string ProfileHeaderGradient => ProfileToEdit switch
        {
            PerformanceMode.Gamer => "Linear #33EF4444, #00EF4444",
            PerformanceMode.Dev => "Linear #338B5CF6, #008B5CF6",
            PerformanceMode.Oficina => "Linear #3310B981, #0010B981",
            _ => "Linear #333B82F6, #003B82F6"
        };

        public string CurrentProfileTitle => ProfileToEdit switch
        {
            PerformanceMode.Gamer => "Optimización para Juegos",
            PerformanceMode.Dev => "Entorno de Desarrollo",
            PerformanceMode.Oficina => "Modo Oficina / Productividad",
            PerformanceMode.Personalizado => "Configuración Personalizada",
            _ => "Perfil de Rendimiento"
        };

        public string CurrentProfileIcon => ProfileToEdit switch
        {
            PerformanceMode.Gamer => "🎮",
            PerformanceMode.Dev => "👨‍💻",
            PerformanceMode.Oficina => "📊",
            PerformanceMode.Personalizado => "⚙️",
            _ => "📄"
        };
        public bool IsEditingAllowed => ProfileToEdit == PerformanceMode.Personalizado;

        private bool _hasUnsavedChanges;
        public bool HasUnsavedChanges
        {
            get => _hasUnsavedChanges;
            set { if (_hasUnsavedChanges != value) { _hasUnsavedChanges = value; OnPropertyChanged(); } }
        }

        private ProfileConfig _editingConfig = new();
        public ProfileConfig EditingConfig
        {
            get => _editingConfig;
            set { _editingConfig = value; OnPropertyChanged(); }
        }

        // Propiedades para la UI vinculadas a EditingConfig
        public bool SysMainEnabled
        {
            get => EditingConfig.ServicesToStop.Contains("SysMain");
            set { UpdateService("SysMain", value); OnPropertyChanged(); HasUnsavedChanges = true; }
        }

        public bool WindowsSearchEnabled
        {
            get => EditingConfig.ServicesToStop.Contains("WSearch");
            set { UpdateService("WSearch", value); OnPropertyChanged(); HasUnsavedChanges = true; }
        }

        public bool TelemetryEnabled
        {
            get => EditingConfig.DisableTelemetry;
            set { EditingConfig.DisableTelemetry = value; OnPropertyChanged(); HasUnsavedChanges = true; }
        }

        public bool ReduceBackgroundPriority
        {
            get => EditingConfig.ReduceBackgroundPriority;
            set { EditingConfig.ReduceBackgroundPriority = value; OnPropertyChanged(); HasUnsavedChanges = true; }
        }

        public bool AutoCleanRam
        {
            get => EditingConfig.AutoCleanRam;
            set { EditingConfig.AutoCleanRam = value; OnPropertyChanged(); HasUnsavedChanges = true; }
        }

        public bool OptimizeVisualEffects
        {
            get => EditingConfig.OptimizeVisualEffects;
            set { EditingConfig.OptimizeVisualEffects = value; OnPropertyChanged(); HasUnsavedChanges = true; }
        }

        public bool DisableNetworkThrottling
        {
            get => EditingConfig.DisableNetworkThrottling;
            set { EditingConfig.DisableNetworkThrottling = value; OnPropertyChanged(); HasUnsavedChanges = true; }
        }

        public bool PauseWindowsUpdate
        {
            get => EditingConfig.PauseWindowsUpdate;
            set { EditingConfig.PauseWindowsUpdate = value; OnPropertyChanged(); HasUnsavedChanges = true; }
        }

        public ICommand ShowHelpCommand { get; }

        private void UpdateService(string serviceName, bool stop)
        {
            if (stop)
            {
                if (!EditingConfig.ServicesToStop.Contains(serviceName))
                    EditingConfig.ServicesToStop.Add(serviceName);
            }
            else
            {
                EditingConfig.ServicesToStop.Remove(serviceName);
            }
        }

        public ICommand SaveCommand { get; }
        public ICommand RestoreDefaultsCommand { get; }
        public ICommand AddAutoBoostCommand { get; }
        public ICommand RemoveAutoBoostCommand { get; }

        private ObservableCollection<string> _autoBoostList = new();
        public ObservableCollection<string> AutoBoostList
        {
            get => _autoBoostList;
            set { _autoBoostList = value; OnPropertyChanged(); }
        }

        public ProfileEditorViewModel(ISettingsService settingsService, ILogService log, IDialogService dialogService)
        {
            _settingsService = settingsService;
            _log = log;
            _dialogService = dialogService;

            SaveCommand = new RelayCommand(async _ => await SaveChangesAsync());
            RestoreDefaultsCommand = new RelayCommand(_ => RestoreDefaults());
            AddAutoBoostCommand = new RelayCommand(p => 
            {
                if (p is string proc && !string.IsNullOrWhiteSpace(proc))
                {
                    if (!EditingConfig.AutoBoostProcesses.Contains(proc))
                    {
                        EditingConfig.AutoBoostProcesses.Add(proc);
                        AutoBoostList.Add(proc);
                        HasUnsavedChanges = true;
                    }
                }
            });
            RemoveAutoBoostCommand = new RelayCommand(p => 
            {
                if (p is string proc)
                {
                    EditingConfig.AutoBoostProcesses.Remove(proc);
                    AutoBoostList.Remove(proc);
                    HasUnsavedChanges = true;
                }
            });

            ShowHelpCommand = new RelayCommand(p => 
            {
                if (p is string msg)
                {
                    _dialogService.ShowMessage(msg, "Información de Ajuste");
                }
            });

            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            _currentSettings = await _settingsService.LoadAsync();
            LoadProfileConfig(ProfileToEdit);
        }

        private void LoadProfileConfig(PerformanceMode mode)
        {
            if (_currentSettings == null) return;

            string key = mode.ToString();
            if (_currentSettings.PerformanceProfiles.TryGetValue(key, out var config))
            {
                // Clonamos para no editar directamente los ajustes hasta guardar
                EditingConfig = new ProfileConfig
                {
                    Mode = config.Mode,
                    ServicesToStop = new List<string>(config.ServicesToStop),
                    DisableTelemetry = config.DisableTelemetry,
                    DisableIndexing = config.DisableIndexing,
                    ReduceBackgroundPriority = config.ReduceBackgroundPriority,
                    ProcessesToKill = new List<string>(config.ProcessesToKill),
                    PowerPlanGuid = config.PowerPlanGuid,
                    AutoBoostProcesses = new List<string>(config.AutoBoostProcesses ?? new List<string>()),
                    AutoCleanRam = config.AutoCleanRam,
                    OptimizeVisualEffects = config.OptimizeVisualEffects,
                    DisableNetworkThrottling = config.DisableNetworkThrottling,
                    PauseWindowsUpdate = config.PauseWindowsUpdate
                };

                AutoBoostList = new ObservableCollection<string>(EditingConfig.AutoBoostProcesses);
                
                // Notificar cambios en las propiedades vinculadas
                OnPropertyChanged(nameof(SysMainEnabled));
                OnPropertyChanged(nameof(WindowsSearchEnabled));
                OnPropertyChanged(nameof(TelemetryEnabled));
                OnPropertyChanged(nameof(ReduceBackgroundPriority));
                OnPropertyChanged(nameof(AutoCleanRam));
                OnPropertyChanged(nameof(OptimizeVisualEffects));
                OnPropertyChanged(nameof(DisableNetworkThrottling));
                OnPropertyChanged(nameof(PauseWindowsUpdate));
                HasUnsavedChanges = false;
            }
        }

        private async Task SaveChangesAsync()
        {
            if (_currentSettings == null || IsBusy) return;

            try
            {
                IsBusy = true;
                StatusMessage = "Guardando Configuración...";
                
                string key = ProfileToEdit.ToString();
                _currentSettings.PerformanceProfiles[key] = EditingConfig;
                
                await _settingsService.SaveAsync(_currentSettings);
                
                // Mostrar éxito visualmente
                StatusMessage = "¡Guardado!";
                await Task.Delay(800);
                
                HasUnsavedChanges = false;
                _log.Info($"Configuración guardada para el perfil: {key}");
            }
            catch (Exception ex)
            {
                _log.Error("Error al guardar perfil", ex);
                _dialogService.ShowMessage($"No se pudo guardar: {ex.Message}", "Error");
            }
            finally
            {
                IsBusy = false;
                StatusMessage = "";
            }
        }

        private void RestoreDefaults()
        {
            ProfileConfig defaultConfig = ProfileToEdit switch
            {
                PerformanceMode.Gamer => ProfileConfig.DefaultGamer(),
                PerformanceMode.Dev => ProfileConfig.DefaultDev(),
                PerformanceMode.Oficina => ProfileConfig.DefaultOficina(),
                _ => new ProfileConfig { Mode = PerformanceMode.Personalizado }
            };

            EditingConfig = defaultConfig;
            HasUnsavedChanges = true;
            
            OnPropertyChanged(nameof(SysMainEnabled));
            OnPropertyChanged(nameof(WindowsSearchEnabled));
            OnPropertyChanged(nameof(TelemetryEnabled));
            OnPropertyChanged(nameof(ReduceBackgroundPriority));
            OnPropertyChanged(nameof(AutoCleanRam));
            OnPropertyChanged(nameof(OptimizeVisualEffects));
            OnPropertyChanged(nameof(DisableNetworkThrottling));
            OnPropertyChanged(nameof(PauseWindowsUpdate));
            
            AutoBoostList = new ObservableCollection<string>(EditingConfig.AutoBoostProcesses);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
