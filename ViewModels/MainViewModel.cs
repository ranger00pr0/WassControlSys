using System.Collections.ObjectModel; // Added for ObservableCollection
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input; // Añadido para ICommand
using WassControlSys.Core; // Añadido para RelayCommand
using WassControlSys.Models; // Añadido para Models
using System.Windows.Threading; // Añadido para DispatcherTimer
using System.Diagnostics;
using System;
using System.Linq; // Added for FirstOrDefault
using System.IO; // Added for File and Directory operations
using System.IO; // Added for File and Directory operations
using System.Security.Principal; // Added for administrator check
using System.Runtime.InteropServices; // Added for P/Invoke

#nullable enable

namespace WassControlSys.ViewModels
{
    public class CpuCoreInfo : INotifyPropertyChanged
    {
        private double _usage;
        public int Index { get; set; }
        public double Usage 
        { 
            get => _usage; 
            set { if (Math.Abs(_usage - value) > 0.1) { _usage = value; OnPropertyChanged(); } } 
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class UnifiedDiskViewModel : INotifyPropertyChanged
    {
        public string DriveLetter { get; set; } = "";
        public string Label { get; set; } = "";
        public string Model { get; set; } = "Desconocido";
        public long TotalSize { get; set; }
        public long UsedSpace { get; set; }
        public long FreeSpace { get; set; }
        
        public double UsagePercentage => TotalSize > 0 ? (double)UsedSpace / TotalSize * 100 : 0;
        public string FormattedSize => $"{TotalSize / (1024.0 * 1024 * 1024):F1} GB";
        public string FormattedFree => $"{FreeSpace / (1024.0 * 1024 * 1024):F1} GB";
        public string FormattedUsed => $"{UsedSpace / (1024.0 * 1024 * 1024):F1} GB";

        public string HealthStatus { get; set; } = "Desconocido";
        public string Temperature { get; set; } = "--";
        public string? PnpDeviceId { get; set; } // Added for more precise disk matching
        public int? PhysicalDiskIndex { get; set; } // Added for more precise disk matching
        
        // Real-time stats (updates frequently)
        private double _readSpeed;
        public double ReadSpeed 
        { 
            get => _readSpeed; 
            set { if(_readSpeed != value) { _readSpeed = value; OnPropertyChanged(); } } 
        }
        
        private double _writeSpeed;
        public double WriteSpeed 
        { 
            get => _writeSpeed; 
            set { if(_writeSpeed != value) { _writeSpeed = value; OnPropertyChanged(); } } 
        }

        public ICommand? AnalyzeCommand { get; set; }
        public ICommand? OptimizeCommand { get; set; }

        public ObservableCollection<FolderSizeInfo> DiskAnalysisResult { get; set; } = new();
        public ObservableCollection<FolderSizeInfo> LargeFilesResult { get; set; } = new();

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly ISystemMaintenanceService _maintenance;
        private readonly IMonitoringService _monitoringService;
        private readonly DispatcherTimer _monitoringTimer;
        private readonly DispatcherTimer _processTimer;
        private readonly IPerformanceProfileService _profiles;
        private readonly ISettingsService _settings;
        private readonly ILogService _log;
        private readonly ISystemInfoService _systemInfoService;
        private readonly ISecurityService _securityService;
        private readonly IDialogService _dialogService; // Added
        private readonly IStartupService _startupService; // Added
        private readonly IServiceOptimizerService _serviceOptimizerService; // Added
        private readonly IBloatwareService _bloatwareService; // Added
        private readonly IPrivacyService _privacyService; // Added
        private readonly IProcessManagerService _processManagerService;
        private readonly ILocalizationService _localizationService;
        private readonly IRestorePointService _restorePointService;
        private readonly IBatteryService _batteryService;
        private readonly IWingetService _wingetService;
        private readonly IDriverService _driverService;
        private readonly IDiskAnalyzerService _diskAnalyzerService;
        public ProfileEditorViewModel ProfileEditor { get; } // Added
        private CancellationTokenSource? _updateSearchCts;
        private CancellationTokenSource? _driverExportCancellationTokenSource; // Added for driver export cancellation
        private readonly Dictionary<string, CancellationTokenSource> _appUpdateCts = new();
        private DispatcherTimer? _idleTimer;
        private readonly DispatcherTimer _autoBoostTimer;
        private PerformanceMode _lastManualMode = PerformanceMode.General;
        private bool _isAutoActivated = false;

        public ICommand RefreshDisksCommand { get; private set; }

        // Constructor del ViewModel principal
        public MainViewModel(ISystemMaintenanceService maintenance, IMonitoringService monitoringService, IPerformanceProfileService profiles, ISettingsService settings, ILogService log, ISystemInfoService systemInfoService, ISecurityService securityService, IDialogService dialogService, IStartupService startupService, IServiceOptimizerService serviceOptimizerService, IBloatwareService bloatwareService, IPrivacyService privacyService, IProcessManagerService processManagerService, ITemperatureMonitorService temperatureMonitorService, IDiskHealthService diskHealthService, ILocalizationService localizationService, IRestorePointService restorePointService, IBatteryService batteryService, IWingetService wingetService, IDriverService driverService, IDiskAnalyzerService diskAnalyzerService)
        {
            // Lógica de inicialización para el ViewModel principal
            _maintenance = maintenance;
            _monitoringService = monitoringService;
            _profiles = profiles;
            _settings = settings;
            _log = log;
            _systemInfoService = systemInfoService;
            _securityService = securityService;
            _dialogService = dialogService; // Added
            _startupService = startupService; // Added
            _serviceOptimizerService = serviceOptimizerService; // Added
            _bloatwareService = bloatwareService; // Added
            _privacyService = privacyService; // Added
            _processManagerService = processManagerService;
            _temperatureMonitorService = temperatureMonitorService;
            _diskHealthService = diskHealthService;
            _localizationService = localizationService;
            _restorePointService = restorePointService;
            _batteryService = batteryService;
            _wingetService = wingetService;
            _driverService = driverService;
            _diskAnalyzerService = diskAnalyzerService;
            
            // Inicializar sub-ViewModel
            ProfileEditor = new ProfileEditorViewModel(settings, log, dialogService);
            
            // Inicializar Comandos
            CleanTempFilesCommand = new RelayCommand(async _ => await ExecuteCleanTempFilesAsync()); 
            RunSfcCommand = new RelayCommand(async _ => await ExecuteRunSfc());
            RunDismCommand = new RelayCommand(async _ => await ExecuteRunDism());
            RunChkdskCommand = new RelayCommand(async _ => await ExecuteRunChkdsk());
            ApplyPerformanceModeCommand = new RelayCommand(async p =>
            {
                PerformanceMode mode = PerformanceMode.General;
                if (p is PerformanceMode pm) mode = pm;
                else if (p is string s && Enum.TryParse(s, true, out PerformanceMode parsed)) mode = parsed;
                
                // Update the property to reflect selection if triggered by command
                if (CurrentMode != mode) CurrentMode = mode;

                await ExecuteApplyModeAsync(mode);
            });
            OptimizeRamCommand = new RelayCommand(async _ => await ExecuteOptimizeRamAsync());
            FlushDnsCommand = new RelayCommand(async _ => await ExecuteFlushDnsAsync());
            AnalyzeDiskCommand = new RelayCommand(async _ => await ExecuteAnalyzeDiskAsync());
            RebuildSearchIndexCommand = new RelayCommand(async _ => await ExecuteRebuildSearchIndexAsync());
            CleanPrefetchCommand = new RelayCommand(async _ => await ExecuteCleanPrefetchAsync());
            CustomSpaceLiberatorCommand = new RelayCommand(async _ => await ExecuteCustomSpaceLiberatorAsync()); // New
            ResetNetworkCommand = new RelayCommand(async _ => await ExecuteResetNetworkAsync());
            RefreshDisksCommand = new RelayCommand(async _ => await LoadDisksAsync());

            NavigateCommand = new RelayCommand(ExecuteNavigate);
            ChangeAccentColorCommand = new RelayCommand(p => { if (p is string hex) AccentColor = hex; });
            EnableStartupItemCommand = new RelayCommand<StartupItem>(async item => await ExecuteEnableStartupItemAsync(item));
            DisableStartupItemCommand = new RelayCommand<StartupItem>(async item => await ExecuteDisableStartupItemAsync(item));
            RefreshStartupItemsCommand = new RelayCommand(async _ => await LoadStartupItemsAsync());
            StartServiceCommand = new RelayCommand<string>(async serviceName => await ExecuteStartServiceAsync(serviceName));
            StopServiceCommand = new RelayCommand<string>(async serviceName => await ExecuteStopServiceAsync(serviceName));
            RefreshServicesCommand = new RelayCommand(async _ => await LoadWindowsServicesAsync());
            UninstallBloatwareAppCommand = new RelayCommand<BloatwareApp>(async app => await ExecuteUninstallBloatwareAppAsync(app));
            RefreshBloatwareAppsCommand = new RelayCommand(async _ => await LoadBloatwareAppsAsync());
            UpdatePrivacySettingCommand = new RelayCommand<PrivacySetting>(async setting => await ExecuteUpdatePrivacySettingAsync(setting));
            RefreshPrivacySettingsCommand = new RelayCommand(async _ => await LoadPrivacySettingsAsync());
            RefreshProcessesCommand = new RelayCommand(async _ => await LoadProcessesAsync());
            ReduceBackgroundProcessesCommand = new RelayCommand(async _ => await ExecuteReduceBackgroundAsync());
            SetProcessPriorityHighCommand = new RelayCommand<ProcessInfoDto>(async p => await ExecuteSetProcessPriorityAsync(p, System.Diagnostics.ProcessPriorityClass.High));
            SetProcessPriorityNormalCommand = new RelayCommand<ProcessInfoDto>(async p => await ExecuteSetProcessPriorityAsync(p, System.Diagnostics.ProcessPriorityClass.Normal));
            
            OpenLogFolderCommand = new RelayCommand(_ => 
            {
                string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WassControlSys", "logs");
                if (Directory.Exists(path))
                {
                    Process.Start("explorer.exe", path);
                }
                else
                {
                    _dialogService.ShowMessage("La carpeta de logs aún no existe.", "Información");
                }
            });
            SetProcessPriorityBelowNormalCommand = new RelayCommand<ProcessInfoDto>(async p => await ExecuteSetProcessPriorityAsync(p, System.Diagnostics.ProcessPriorityClass.BelowNormal));
            KillProcessCommand = new RelayCommand<ProcessInfoDto>(async p => await ExecuteKillProcessAsync(p));
            RefreshThermalCommand = new RelayCommand(async _ => await UpdateThermalAsync());
            RefreshDiskHealthCommand = new RelayCommand(async _ => await LoadDiskHealthAsync());
            RefreshDriversCommand = new RelayCommand(async _ => await LoadDriversWithProblemsAsync());
            
            CreateRestorePointCommand = new RelayCommand(async _ => await ExecuteCreateRestorePointAsync());
            UpdateAppCommand = new RelayCommand<string>(async id => await ExecuteUpdateAppAsync(id));
            CancelAppUpdateCommand = new RelayCommand<string>(id => ExecuteCancelAppUpdate(id));
            UpdateAllAppsCommand = new RelayCommand(async _ => await ExecuteUpdateAllAppsAsync());
            ExportDriversCommand = new RelayCommand(async _ => await ExecuteExportDriversAsync());
            CancelDriverExportCommand = new RelayCommand(_ => ExecuteCancelDriverExport(), _ => IsExportingDrivers);
            AnalyzeDiskSpaceCommand = new RelayCommand<string>(async path => await ExecuteAnalyzeDiskSpaceAsync(path));
            RefreshBatteryCommand = new RelayCommand(async _ => await LoadBatteryInfoAsync());
            RefreshUpdatableAppsCommand = new RelayCommand(async _ => await LoadUpdatableAppsAsync());
            CancelUpdateSearchCommand = new RelayCommand(_ => { _updateSearchCts?.Cancel(); IsSearchingUpdates = false; StatusMessage = "Búsqueda cancelada"; });
            ClearProcessSearchCommand = new RelayCommand(_ => ProcessSearchText = string.Empty);
            ClearServiceSearchCommand = new RelayCommand(_ => ServiceSearchText = string.Empty);
            ToggleServiceCommand = new RelayCommand<WindowsService>(async s => await ExecuteToggleServiceAsync(s));
            FreeUpDiskSpaceCommand = new RelayCommand(_ => ExecuteFreeUpDiskSpace(), _ => SelectedDriveForCleanup != null);
            PcBoostCommand = new RelayCommand(async _ => await ExecutePcBoostAsync());
            DeepScanCommand = new RelayCommand(async _ => await ExecuteDeepScanAsync());
            OpenDiscordCommand = new RelayCommand(ExecuteOpenDiscord);
            OpenDiscordCommand = new RelayCommand(ExecuteOpenDiscord);
            OpenCleanmgrCommand = new RelayCommand(_ => ExecuteOpenCleanmgr());
            
            CleanDownloadsCommand = new RelayCommand<string>(async period => await ExecuteCleanDownloadsAsync(period));
            DeleteSelectedLargeFilesCommand = new RelayCommand(async _ => await ExecuteDeleteSelectedLargeFilesAsync());

            RunToolCommand = new RelayCommand(p => 
            {
                if (p is string tool)
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo(tool) { UseShellExecute = true });
                        _log?.Info($"Herramienta iniciada: {tool}");
                    }
                    catch (Exception ex)
                    {
                        _log?.Error($"Error al iniciar herramienta: {tool}", ex);
                    }
                }
            });





            // Establecer modo por defecto antes de cargar ajustes
            CurrentMode = PerformanceMode.General;

            // Cargar ajustes
            _ = LoadSettingsAsync();

            // Inicializar Timer de Monitoreo (Global)
            _monitoringTimer = new DispatcherTimer();
            _monitoringTimer.Interval = TimeSpan.FromSeconds(3);
            _monitoringTimer.Tick += (s, e) => 
            {
                // Si la ventana NO es visible, NO actualizamos nada de UI global (CPU/RAM Sidebar)
                if (!IsWindowVisible) return;
                UpdateSystemUsage();
            };
            _monitoringTimer.Tick += async (s, e) => 
            {
                if (!IsWindowVisible) return;
                
                // Thermal solo si estamos en Rendimiento o Hardware (donde se ve la temp)
                await UpdateThermalAsync();
                
                // Actualizar stats del disco seleccionado si estamos en Hardware
                if (CurrentSection == AppSection.Hardware)
                {
                    await UpdateUnifiedDiskStatsAsync();
                }
            };
            _monitoringTimer.Start();

            OpenFolderCommand = new RelayCommand(p => ExecuteOpenFolder(p as string));
            
            // Timer para procesos (30 seg)
            _processTimer = new DispatcherTimer();
            _processTimer.Interval = TimeSpan.FromSeconds(30);
            _processTimer.Tick += (s, e) => 
            {
                // Solo actualizar lista de procesos si la App es Visible Y estamos en la sección Rendimiento
                if (!IsWindowVisible) return;
                if (CurrentSection != AppSection.Rendimiento) return;

                _ = LoadProcessesAsync(silent: true);
            };
            _processTimer.Start();

            // Cargar info inicial
            _ = LoadLastRestorePointAsync();
            _ = LoadDisksAsync();
            
            SetupIdleMaintenance();

            // Timer para Auto-Boost (cada 10 seg está bien para no saturar)
            _autoBoostTimer = new DispatcherTimer();
            _autoBoostTimer.Interval = TimeSpan.FromSeconds(10);
            _autoBoostTimer.Tick += async (s, e) => 
            {
                // AutoBoost debe funcionar en segundo plano? 
                // El usuario dijo: "si esta en segundo plano la app no se actualiza... sabra que lo que quiero es que la app consuma lo menos posible"
                // PERO AutoBoost es una función "core". Si lanzo un juego y la app está minimizada, ¿debería activarse el modo juego?
                // Generalmente SÍ. Pero el usuario pidió "consuma lo menos posible".
                // Dejaremos AutoBoost activo porque es una característica funcional crítica, pero podemos reducir la frecuencia si está minimizado?
                // Por ahora lo dejamos igual, ya que checkear procesos es rápido.
                await CheckAutoBoostAsync(); 
            };
            _autoBoostTimer.Start();
        }

        private void SetupIdleMaintenance()
        {
            _idleTimer = new DispatcherTimer();
            _idleTimer.Interval = TimeSpan.FromMinutes(1);
            _idleTimer.Tick += async (s, e) => await CheckIdleStateAsync();
            _idleTimer.Start();
        }

        private async Task CheckIdleStateAsync()
        {
            if (!OptimizeOnIdle) return;

            var idleTime = _monitoringService.GetIdleTime();
            
            // Si el PC ha estado inactivo por más de 10 minutos
            if (idleTime.TotalMinutes >= 10)
            {
                // Evitar ejecuciones múltiples seguidas (ej. una vez por hora máximo en idle)
                if ((DateTime.Now - _lastAutoMaintenance).TotalHours >= 1)
                {
                    _log?.Info($"PC Inactivo por {idleTime.TotalMinutes:F1} min. Iniciando mantenimiento automático.");
                    await ExecuteSilentMaintenanceAsync();
                    _lastAutoMaintenance = DateTime.Now;
                }
            }
        }

        private DateTime _lastAutoMaintenance = DateTime.MinValue;

        private async Task CheckAutoBoostAsync()
        {
            var settings = await _settings.LoadAsync();
            if (!settings.EnableAutoBoost) return;

            // Buscamos si algún proceso configurado en algún perfil está corriendo
            foreach (var profilePair in settings.PerformanceProfiles)
            {
                var config = profilePair.Value;
                if (config.AutoBoostProcesses == null || config.AutoBoostProcesses.Count == 0) continue;

                bool processFound = false;
                foreach (var procName in config.AutoBoostProcesses)
                {
                    // Comprobamos si el proceso existe (sin .exe si el usuario se lo puso)
                    string searchName = procName.Replace(".exe", "", StringComparison.OrdinalIgnoreCase);
                    if (Process.GetProcessesByName(searchName).Length > 0)
                    {
                        processFound = true;
                        break;
                    }
                }

                if (processFound)
                {
                    if (CurrentMode != config.Mode)
                    {
                        _log?.Info($"Auto-Boost: Detectado proceso de {profilePair.Key}. Activando perfil.");
                        if (!_isAutoActivated) _lastManualMode = CurrentMode;
                        _isAutoActivated = true;
                        await ExecuteApplyModeAsync(config.Mode);
                    }
                    return; // Ya activamos uno, salimos
                }
            }

            // Si llegamos aquí y estábamos en modo Auto-Activado, pero ya no hay procesos, restauramos
            if (_isAutoActivated)
            {
                _log?.Info("Auto-Boost: Ya no se detectan procesos objetivos. Restaurando modo manual.");
                _isAutoActivated = false;
                await ExecuteApplyModeAsync(_lastManualMode);
            }
        }

        private async Task ExecuteSilentMaintenanceAsync()
        {
            try
            {
                // Limpieza rápida y optimización de RAM
                await _maintenance.OptimizeMemoryAsync();
                var options = new CleaningOptions { CleanSystemTemp = true, CleanRecycleBin = true };
                await _maintenance.CleanTemporaryFilesAsync(options);
            }
            catch (Exception ex)
            {
                _log?.Error("Error en mantenimiento automático (Idle)", ex);
            }
        }

        public async Task LoadDisksAsync()
        {
            try
            {
                var unifiedList = new List<UnifiedDiskViewModel>();
                // Run heavy I/O on background thread
                await Task.Run(() => 
                {
                    try 
                    {
                        var drives = DriveInfo.GetDrives().Where(d => d.IsReady && (d.DriveType == DriveType.Fixed || d.DriveType == DriveType.Removable));
                        foreach (var drive in drives)
                        {
                            var physicalInfo = GetPhysicalDiskInfoForDrive(drive.Name);
                            // Basic info
                            var uDisk = new UnifiedDiskViewModel
                            {
                                DriveLetter = drive.Name,
                                Label = drive.VolumeLabel,
                                TotalSize = drive.TotalSize,
                                FreeSpace = drive.AvailableFreeSpace,
                                UsedSpace = drive.TotalSize - drive.AvailableFreeSpace,
                                AnalyzeCommand = AnalyzeDiskSpaceCommand,
                                OptimizeCommand = new RelayCommand(_ => Process.Start("dfrgui.exe")),
                                PnpDeviceId = physicalInfo.PnpDeviceId, // Populate new property
                                PhysicalDiskIndex = physicalInfo.PhysicalDiskIndex // Populate new property
                            };
                            unifiedList.Add(uDisk);
                        }
                    }
                    catch(Exception ex)
                    {
                         _log?.Error("Error iterating drives", ex);
                    }
                });

                // Ensure UI verification and update happens on the Dispatcher
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    UnifiedDisks = new ObservableCollection<UnifiedDiskViewModel>(unifiedList);
                    _log?.Info($"Carga de discos completada. Encontrados: {unifiedList.Count}");

                    if (UnifiedDisks.Any())
                    {
                        if (SelectedUnifiedDisk == null || !UnifiedDisks.Any(d => d.DriveLetter == SelectedUnifiedDisk.DriveLetter))
                        {
                            SelectedUnifiedDisk = UnifiedDisks.First();
                        }
                    }
                    else
                    {
                         _log?.Warn("No se encontraron discos fijos o extraíbles listos.");
                         StatusMessage = "No se detectaron discos compatibles.";
                    }
                });
            }
            catch (Exception ex)
            {
                _log?.Error("Error loading disk analyzers", ex);
                StatusMessage = "Error al detectar hardware de almacenamiento.";
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task PrepareForShutdownAsync()
        {
            _log?.Info("Preparando para el apagado de la aplicación...");
            StopAllTasks();
            
            if (CurrentMode != PerformanceMode.General)
            {
                _log?.Info("Restaurando estado del sistema antes de salir...");
                await _profiles.RestoreOriginalStateAsync();
            }
        }

        public void StopAllTasks()
        {
            _log?.Info("Deteniendo todas las tareas en segundo plano...");
            _monitoringTimer?.Stop();
            _processTimer?.Stop();
            _updateSearchCts?.Cancel();
            _updateSearchCts?.Dispose();
            _driverExportCancellationTokenSource?.Cancel();
            _driverExportCancellationTokenSource?.Dispose();
            _driverExportCancellationTokenSource = null;
        }

        // Implementación de la interfaz INotifyPropertyChanged
        // Esto permite que la interfaz de usuario se actualice automáticamente cuando cambian las propiedades del ViewModel.
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Ejemplo de propiedad enlazable (bindable property)
        private string _welcomeMessage = string.Empty;
        public string WelcomeMessage
        {
            get => _welcomeMessage;
            set
            {
                if (_welcomeMessage != value)
                {
                    _welcomeMessage = value;
                    OnPropertyChanged(); // Notifica a la interfaz de usuario que la propiedad ha cambiado
                }
            }
        }

        private string _generalStatusMessage = "Estado General: Listo para optimizar.";
        public string GeneralStatusMessage
        {
            get => _generalStatusMessage;
            set { if (_generalStatusMessage != value) { _generalStatusMessage = value; OnPropertyChanged(); } }
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set { if (_isBusy != value) { _isBusy = value; OnPropertyChanged(); } }
        }
        
        private string _statusMessage = "";
        public string StatusMessage
        {
            get => _statusMessage;
            set { if (_statusMessage != value) { _statusMessage = value; OnPropertyChanged(); } }
        }
        
        private string _selectedLanguage = "es";
        public string SelectedLanguage
        {
            get => _selectedLanguage;
            set
            {
                if (_selectedLanguage != value)
                {
                    _selectedLanguage = value;
                    OnPropertyChanged();
                    _ = _localizationService.SetLanguageAsync(value);
                    _ = SaveSettingsAsync();
                }
            }
        }

        // --- Propiedades para Selector de Modo (Task 02) ---
        private PerformanceMode _currentMode;
        public PerformanceMode CurrentMode
        {
            get => _currentMode;
            set
            {
                if (_currentMode != value)
                {
                    _currentMode = value;
                    OnPropertyChanged();
                    _ = SaveSettingsAsync();
                    _log?.Info($"Modo cambiado a: {value}");
                }
            }
        }

        private PerformanceMode _profileToEdit = PerformanceMode.Gamer;
        public PerformanceMode ProfileToEdit
        {
            get => _profileToEdit;
            set { if (_profileToEdit != value) { _profileToEdit = value; OnPropertyChanged(); LoadProfileConfig(value); } }
        }

        private void LoadProfileConfig(PerformanceMode mode)
        {
            // Placeholder
            _log?.Info($"Cargando configuración para editar perfil: {mode}");
        }

        // --- Propiedades para Ciclo de Vida y Eficiencia ---
        private bool _isWindowVisible = true;
        public bool IsWindowVisible
        {
            get => _isWindowVisible;
            set
            {
                if (_isWindowVisible != value)
                {
                    _isWindowVisible = value;
                    OnPropertyChanged();
                    _log?.Info($"Visibilidad de Ventana cambiada: {value}. {(value ? "Reanudando" : "Pausando")} actualizaciones en segundo plano.");
                    
                    // Forzar actualización inmediata al volver a mostrar
                    if (value)
                    {
                         UpdateSystemUsage();
                         _ = UpdateThermalAsync();
                         if (CurrentSection == AppSection.Rendimiento) _ = LoadProcessesAsync(silent: true);
                    }
                }
            }
        }

        private AppSection _currentSection = AppSection.Dashboard;
        public AppSection CurrentSection
        {
            get => _currentSection;
            set
            {
                if (_currentSection != value)
                {
                    _currentSection = value;
                    OnPropertyChanged();
                    // Al cambiar de sección, podriamos disparar actualizaciones especificas si fuera necesario
                }
            }
        }

        // --- Propiedades para Monitoreo (Task 04) ---
        private double _cpuUsage;
        public double CpuUsage
        {
            get => _cpuUsage;
            set { if (_cpuUsage != value) { _cpuUsage = value; OnPropertyChanged(); } }
        }

        private double _ramUsage;
        public double RamUsage
        {
            get => _ramUsage;
            set { if (_ramUsage != value) { _ramUsage = value; OnPropertyChanged(); } }
        }

        private double _diskUsage;
        public double DiskUsage
        {
            get => _diskUsage;
            set { if (_diskUsage != value) { _diskUsage = value; OnPropertyChanged(); } }
        }

        private ObservableCollection<CpuCoreInfo> _cpuPerCore = new();
        public ObservableCollection<CpuCoreInfo> CpuPerCore
        {
            get => _cpuPerCore;
            set { if (_cpuPerCore != value) { _cpuPerCore = value; OnPropertyChanged(); } }
        }

        private double _netSentMbps;
        public double NetSentMbps
        {
            get => _netSentMbps;
            set { if (_netSentMbps != value) { _netSentMbps = value; OnPropertyChanged(); } }
        }

        private double _netRecvMbps;
        public double NetRecvMbps
        {
            get => _netRecvMbps;
            set { if (_netRecvMbps != value) { _netRecvMbps = value; OnPropertyChanged(); } }
        }

        private int _activeTcpConnections;
        public int ActiveTcpConnections
        {
            get => _activeTcpConnections;
            set { if (_activeTcpConnections != value) { _activeTcpConnections = value; OnPropertyChanged(); } }
        }

        private double _diskReadsPerSec;
        public double DiskReadsPerSec
        {
            get => _diskReadsPerSec;
            set { if (_diskReadsPerSec != value) { _diskReadsPerSec = value; OnPropertyChanged(); } }
        }

        private double _diskWritesPerSec;
        public double DiskWritesPerSec
        {
            get => _diskWritesPerSec;
            set { if (_diskWritesPerSec != value) { _diskWritesPerSec = value; OnPropertyChanged(); } }
        }

        private double _diskAvgQueueLength;
        public double DiskAvgQueueLength
        {
            get => _diskAvgQueueLength;
            set { if (_diskAvgQueueLength != value) { _diskAvgQueueLength = value; OnPropertyChanged(); } }
        }

        private double _diskReadLatencyMs;
        public double DiskReadLatencyMs
        {
            get => _diskReadLatencyMs;
            set { if (_diskReadLatencyMs != value) { _diskReadLatencyMs = value; OnPropertyChanged(); } }
        }

        private double _diskWriteLatencyMs;
        public double DiskWriteLatencyMs
        {
            get => _diskWriteLatencyMs;
            set { if (_diskWriteLatencyMs != value) { _diskWriteLatencyMs = value; OnPropertyChanged(); } }
        }

        private SecurityStatus _securityStatus = new();
        public SecurityStatus SecurityStatus
        {
            get => _securityStatus;
            set
            {
                if (_securityStatus != value)
                {
                    _securityStatus = value;
                    OnPropertyChanged();
                }
            }
        }

        private SystemInfo _systemInformation = new();
        public SystemInfo SystemInformation
        {
            get => _systemInformation;
            set
            {
                if (_systemInformation != value)
                {
                    _systemInformation = value;
                    OnPropertyChanged();
                }
            }
        }

        private BatteryInfo _batteryInfo = new();
        public BatteryInfo BatteryInfo
        {
            get => _batteryInfo;
            set { if (_batteryInfo != value) { _batteryInfo = value; OnPropertyChanged(); } }
        }

        private ObservableCollection<WingetApp> _updatableApps = new();
        public ObservableCollection<WingetApp> UpdatableApps
        {
            get => _updatableApps;
            set { if (_updatableApps != value) { _updatableApps = value; OnPropertyChanged(); } }
        }

        private ObservableCollection<DiskAnalyzerViewModel> _diskAnalyzers = new();
        public ObservableCollection<DiskAnalyzerViewModel> DiskAnalyzers
        {
            get => _diskAnalyzers;
            set { _diskAnalyzers = value; OnPropertyChanged(); }
        }

        private ObservableCollection<UnifiedDiskViewModel> _unifiedDisks = new();
        public ObservableCollection<UnifiedDiskViewModel> UnifiedDisks
        {
            get => _unifiedDisks;
            set { _unifiedDisks = value; OnPropertyChanged(); }
        }

        private UnifiedDiskViewModel? _selectedUnifiedDisk;
        public UnifiedDiskViewModel? SelectedUnifiedDisk
        {
            get => _selectedUnifiedDisk;
            set
            { 
                _selectedUnifiedDisk = value; 
                OnPropertyChanged(); 
                // Reset stats when switching
               if(value != null) { value.ReadSpeed = 0; value.WriteSpeed = 0; }
            }
        }
        
        private async Task UpdateUnifiedDiskStatsAsync()
        {
            if (SelectedUnifiedDisk == null || !IsWindowVisible) return;

            var usage = await _monitoringService.GetSystemUsageAsync();
            // Sync Health Info if available
            if (DiskHealth != null && SelectedUnifiedDisk != null)
            {
                 // Find the DiskHealthInfo matching the selected unified disk using PnpDeviceId or PhysicalDiskIndex
                 var health = DiskHealth.FirstOrDefault(dh =>
                    (SelectedUnifiedDisk.PnpDeviceId != null && dh.PnpDeviceId != null && dh.PnpDeviceId.Equals(SelectedUnifiedDisk.PnpDeviceId, StringComparison.OrdinalIgnoreCase)) ||
                    (SelectedUnifiedDisk.PhysicalDiskIndex.HasValue && dh.PhysicalDiskIndex.HasValue && dh.PhysicalDiskIndex.Value == SelectedUnifiedDisk.PhysicalDiskIndex.Value));
                 
                 if(health != null)
                 {
                     SelectedUnifiedDisk.HealthStatus = health.SmartOk ? "Saludable" : "Riesgo";
                     SelectedUnifiedDisk.Temperature = health.Temperature > 0 ? $"{health.Temperature}°C" : "--";
                 }
                 else
                 {
                     SelectedUnifiedDisk.HealthStatus = "Desconocido";
                     SelectedUnifiedDisk.Temperature = "--";
                 }
            }
            
            // TODO: Implement per-drive counters in MonitoringService
            var diskPerf = usage.DiskPerformanceInfos.FirstOrDefault(dp => dp.DriveLetter.Equals(SelectedUnifiedDisk.DriveLetter, StringComparison.OrdinalIgnoreCase));
            if (diskPerf != null)
            {
                SelectedUnifiedDisk.ReadSpeed = diskPerf.ReadBytesPerSec / (1024.0 * 1024.0); // Convert to MB/s
                SelectedUnifiedDisk.WriteSpeed = diskPerf.WriteBytesPerSec / (1024.0 * 1024.0); // Convert to MB/s
            }
            else
            {
                SelectedUnifiedDisk.ReadSpeed = 0;
                SelectedUnifiedDisk.WriteSpeed = 0;
            }
        }
        
        private DiskAnalyzerViewModel? _selectedDriveForCleanup;
        public DiskAnalyzerViewModel? SelectedDriveForCleanup
        {
            get => _selectedDriveForCleanup;
            set { _selectedDriveForCleanup = value; OnPropertyChanged(); }
        }


        private bool _isSearchingUpdates;
        public bool IsSearchingUpdates
        {
            get => _isSearchingUpdates;
            set { if (_isSearchingUpdates != value) { _isSearchingUpdates = value; OnPropertyChanged(); } }
        }



        private bool _isExportingDrivers;
        public bool IsExportingDrivers
        {
            get => _isExportingDrivers;
            set { if (_isExportingDrivers != value) { _isExportingDrivers = value; OnPropertyChanged(); } }
        }

        private int _driverExportProgress;
        public int DriverExportProgress
        {
            get => _driverExportProgress;
            set { if (_driverExportProgress != value) { _driverExportProgress = value; OnPropertyChanged(); } }
        }

        private string _driverExportStatusMessage = "";
        public string DriverExportStatusMessage
        {
            get => _driverExportStatusMessage;
            set { if (_driverExportStatusMessage != value) { _driverExportStatusMessage = value; OnPropertyChanged(); } }
        }



        private ObservableCollection<StartupItem> _startupItems = new();
        public ObservableCollection<StartupItem> StartupItems
        {
            get => _startupItems;
            set { if (_startupItems != value) { _startupItems = value; OnPropertyChanged(); } }
        }

        private ObservableCollection<WindowsService> _windowsServices = new();
        public ObservableCollection<WindowsService> WindowsServices
        {
            get => _windowsServices;
            set { if (_windowsServices != value) { _windowsServices = value; OnPropertyChanged(); } }
        }

        private ObservableCollection<WindowsService> _allWindowsServices = new();

        private DispatcherTimer? _searchFilterTimer;

        private string _serviceSearchText = string.Empty;
        public string ServiceSearchText
        {
            get => _serviceSearchText;
            set
            {
                if (_serviceSearchText != value)
                {
                    _serviceSearchText = value;
                    OnPropertyChanged();
                    DebounceFilter(FilterServices);
                }
            }
        }

        private void DebounceFilter(Action filterAction)
        {
            _searchFilterTimer?.Stop();
            _searchFilterTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
            _searchFilterTimer.Tick += (s, e) => { _searchFilterTimer.Stop(); filterAction(); };
            _searchFilterTimer.Start();
        }

        private void FilterServices()
        {
            if (_allWindowsServices == null) return;

            if (string.IsNullOrWhiteSpace(ServiceSearchText))
            {
                WindowsServices = new ObservableCollection<WindowsService>(_allWindowsServices);
            }
            else
            {
                var filtered = _allWindowsServices.Where(s => 
                    (s.Name != null && s.Name.Contains(ServiceSearchText, StringComparison.OrdinalIgnoreCase)) ||
                    (s.DisplayName != null && s.DisplayName.Contains(ServiceSearchText, StringComparison.OrdinalIgnoreCase))
                ).ToList();
                WindowsServices = new ObservableCollection<WindowsService>(filtered);
            }
        }

        private ObservableCollection<BloatwareApp> _bloatwareApps = new();
        public ObservableCollection<BloatwareApp> BloatwareApps
        {
            get => _bloatwareApps;
            set { if (_bloatwareApps != value) { _bloatwareApps = value; OnPropertyChanged(); } }
        }

        private ObservableCollection<PrivacySetting> _privacySettings = new();
        public ObservableCollection<PrivacySetting> PrivacySettings
        {
            get => _privacySettings;
            set { if (_privacySettings != value) { _privacySettings = value; OnPropertyChanged(); } }
        }

        private ObservableCollection<FolderSizeInfo> _deepScanResult = new();
        public ObservableCollection<FolderSizeInfo> DeepScanResult
        {
            get => _deepScanResult;
            set { if (_deepScanResult != value) { _deepScanResult = value; OnPropertyChanged(); } }
        }

        private ObservableCollection<ProcessInfoDto> _processes = new();

        public ObservableCollection<ProcessInfoDto> Processes
        {
            get => _processes;
            set { if (_processes != value) { _processes = value; OnPropertyChanged(); } }
        }

        private ProcessImpactStats _processImpact = new();
        public ProcessImpactStats ProcessImpact
        {
            get => _processImpact;
            set { if (_processImpact != value) { _processImpact = value; OnPropertyChanged(); } }
        }

        private readonly ITemperatureMonitorService _temperatureMonitorService;
        private readonly IDiskHealthService _diskHealthService;
        private double? _cpuTempC;
        public double? CpuTempC
        {
            get => _cpuTempC;
            set { if (_cpuTempC != value) { _cpuTempC = value; OnPropertyChanged(); } }
        }

        private string _thermalAlert = string.Empty;
        public string ThermalAlert
        {
            get => _thermalAlert;
            set { if (_thermalAlert != value) { _thermalAlert = value; OnPropertyChanged(); } }
        }

        private string _lastRestorePointName = "Cargando...";
        public string LastRestorePointName
        {
            get => _lastRestorePointName;
            set { if (_lastRestorePointName != value) { _lastRestorePointName = value; OnPropertyChanged(); } }
        }

        private string _lastRestorePointDate = "";
        public string LastRestorePointDate
        {
            get => _lastRestorePointDate;
            set { if (_lastRestorePointDate != value) { _lastRestorePointDate = value; OnPropertyChanged(); } }
        }

        private ObservableCollection<DiskHealthInfo> _diskHealth = new();
        public ObservableCollection<DiskHealthInfo> DiskHealth
        {
            get => _diskHealth;
            set { if (_diskHealth != value) { _diskHealth = value; OnPropertyChanged(); } }
        }

        private ObservableCollection<DriverInfo> _driversWithProblems = new();
        public ObservableCollection<DriverInfo> DriversWithProblems
        {
            get => _driversWithProblems;
            set { if (_driversWithProblems != value) { _driversWithProblems = value; OnPropertyChanged(); } }
        }

        private string _processSearchText = string.Empty;
        public string ProcessSearchText
        {
            get => _processSearchText;
            set
            {
                if (_processSearchText != value)
                {
                    _processSearchText = value;
                    OnPropertyChanged();
                    DebounceFilter(FilterProcesses);
                }
            }
        }

        private ObservableCollection<ProcessInfoDto> _allProcesses = new();
        private ObservableCollection<ProcessInfoDto> _filteredProcesses = new();

        private async Task LoadProcessesAsync(bool silent = false)
        {
            try
            {
                if (!silent) System.Windows.Application.Current.Dispatcher.Invoke(() => IsBusy = true);
                
                var list = await _processManagerService.GetProcessesAsync();
                var impact = await _processManagerService.ComputeImpactAsync();

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    _allProcesses = new ObservableCollection<ProcessInfoDto>(list);
                    ProcessImpact = impact;
                    FilterProcesses(); // This updates the UI-bound `Processes` collection
                });
            }
            catch (Exception ex)
            {
                _log?.Error("Error cargando procesos", ex);
                if (!silent)
                {
                    await System.Windows.Application.Current.Dispatcher.Invoke(async () =>
                    {
                        await _dialogService.ShowMessage($"Error cargando procesos: {ex.Message}", "Error");
                    });
                }
            }
            finally
            {
                if (!silent) System.Windows.Application.Current.Dispatcher.Invoke(() => IsBusy = false);
            }
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        private void FilterProcesses()
        {
            if (_allProcesses == null) return;

            // 1. Obtener PID de la ventana en primer plano
            uint foregroundPid = 0;
            try
            {
                IntPtr handle = GetForegroundWindow();
                if (handle != IntPtr.Zero)
                {
                    GetWindowThreadProcessId(handle, out foregroundPid);
                }
            }
            catch (Exception ex) { _log?.Warn($"Error detectando proceso en primer plano: {ex.Message}"); }

            List<ProcessInfoDto> filtered;

            if (string.IsNullOrWhiteSpace(ProcessSearchText))
            {
                filtered = _allProcesses.ToList();
            }
            else
            {
                filtered = _allProcesses.Where(p => 
                    (p.Name != null && p.Name.Contains(ProcessSearchText, StringComparison.OrdinalIgnoreCase)) ||
                    p.Pid.ToString().Contains(ProcessSearchText)
                ).ToList();
            }

            // 2. Ordenar: Primer Plano SIEMPRE arriba, luego mantener orden original (o por uso)
            // Nota: Ordenamos descendente por el booleano (True > False)
            var sorted = filtered.OrderByDescending(p => p.Pid == foregroundPid).ToList();

            Processes = new ObservableCollection<ProcessInfoDto>(sorted);
        }

        public async Task ExecuteSetProcessPriorityAsync(ProcessInfoDto p, ProcessPriorityClass priority)
        {
            if (p == null || IsBusy) return;
            try
            {
                IsBusy = true;
                bool ok = await _processManagerService.SetPriorityAsync(p.Pid, priority);
                if (ok)
                {
                    p.Priority = priority;
                    await _dialogService.ShowMessage($"Prioridad actualizada: {p.Name}", "Éxito");
                }
                else
                {
                    await _dialogService.ShowMessage($"No se pudo actualizar prioridad: {p.Name}", "Error");
                }
            }
            catch (Exception ex)
            {
                _log?.Error("Error cambiando prioridad", ex);
                await _dialogService.ShowMessage($"Error cambiando prioridad: {ex.Message}", "Error");
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task ExecuteKillProcessAsync(ProcessInfoDto p)
        {
            if (p == null || IsBusy) return;
            bool confirm = await _dialogService.ShowConfirmation($"¿Desea finalizar {p.Name}?", "Confirmar");
            if (!confirm) return;
            try
            {
                IsBusy = true;
                bool ok = await _processManagerService.KillProcessAsync(p.Pid);
                if (ok)
                {
                    Processes.Remove(p);
                    await _dialogService.ShowMessage("Proceso finalizado", "Éxito");
                }
                else
                {
                    await _dialogService.ShowMessage("No se pudo finalizar el proceso", "Error");
                }
            }
            catch (Exception ex)
            {
                _log?.Error("Error finalizando proceso", ex);
                await _dialogService.ShowMessage($"Error finalizando proceso: {ex.Message}", "Error");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ExecuteReduceBackgroundAsync()
        {
            if (IsBusy) return;
            try
            {
                IsBusy = true;
                int changed = await _processManagerService.ReduceBackgroundProcessesAsync(ProcessPriorityClass.BelowNormal);
                await _dialogService.ShowMessage($"Procesos ajustados: {changed}", "Reducción de fondo");
                await LoadProcessesAsync();
            }
            catch (Exception ex)
            {
                _log?.Error("Error reduciendo procesos en segundo plano", ex);
                await _dialogService.ShowMessage($"Error reduciendo procesos: {ex.Message}", "Error");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task UpdateThermalAsync()
        {
            if (!IsWindowVisible) return; // Ahorrar recursos si no se ve la app

            try
            {
                var temp = await _temperatureMonitorService.GetCpuTemperatureCAsync();
                CpuTempC = temp;
                ThermalAlert = temp.HasValue && temp.Value >= 85 ? "Alerta: Temperatura alta" : "";
            }
            catch (Exception ex)
            {
                _log?.Warn($"No se pudo leer temperatura: {ex.Message}");
            }
        }

        private async Task LoadDiskHealthAsync()
        {
            if (!IsWindowVisible) return;

            try
            {
                IsBusy = true;
                
                // Implementar un timeout para evitar cuelgues por WMI
                var healthTask = _diskHealthService.GetDiskHealthAsync();
                var completedTask = await Task.WhenAny(healthTask, Task.Delay(TimeSpan.FromSeconds(15)));

                if (completedTask != healthTask)
                {
                    // Si el task que completó no es el de salud, fue el timeout
                    throw new TimeoutException("La consulta de estado de los discos (WMI) tardó demasiado y fue cancelada.");
                }

                var items = await healthTask; // Obtenemos el resultado de la tarea ya completada.
                
                // Asegurarse de que la actualización de la colección se haga en el hilo de la UI
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    DiskHealth = new ObservableCollection<DiskHealthInfo>(items);
                    if (!items.Any())
                    {
                        StatusMessage = "No se pudo obtener el estado S.M.A.R.T. de los discos.";
                    }
                });
            }
            catch (Exception ex)
            {
                _log?.Error("Error cargando salud de discos", ex);
                // También es seguro mostrar el diálogo desde el hilo de la UI
                System.Windows.Application.Current.Dispatcher.Invoke(async () =>
                {
                    await _dialogService.ShowMessage($"Error cargando salud de discos: {ex.Message}", "Error");
                });
            }
            finally
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => IsBusy = false);
            }
        }

        private async Task LoadDriversWithProblemsAsync()
        {
            try
            {
                IsBusy = true;
                var drivers = await _driverService.GetDriversWithProblemsAsync();
                DriversWithProblems = new ObservableCollection<DriverInfo>(drivers);
            }
            catch (Exception ex)
            {
                _log?.Error("Error cargando drivers con problemas", ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void ExecuteOpenFolder(string? filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return;
            try
            {
                // Si es un archivo, abrimos la carpeta y lo seleccionamos. 
                // Si es un directorio, simplemente lo abrimos.
                string argument = File.Exists(filePath) ? $"/select,\"{filePath}\"" : $"\"{filePath}\"";
                Process.Start("explorer.exe", argument);
            }
            catch (Exception ex)
            {
                _log?.Error($"Error al abrir ubicación: {filePath}", ex);
            }
        }

        private (string? PnpDeviceId, int? PhysicalDiskIndex) GetPhysicalDiskInfoForDrive(string driveLetter)
        {
            try
            {
                // Query Win32_LogicalDisk to get the device ID for the drive letter
                using var logicalDiskSearcher = new System.Management.ManagementObjectSearcher(
                    $"SELECT DeviceID, Caption FROM Win32_LogicalDisk WHERE Caption = '{driveLetter.TrimEnd('\\')}'"
                );
                foreach (var logicalDisk in logicalDiskSearcher.Get())
                {
                    string? logicalDeviceId = logicalDisk["DeviceID"]?.ToString(); // e.g., "C:"

                    // Use Win32_LogicalDiskToPartition to link logical disk to partition
                    using var logicalDiskToPartitionSearcher = new System.Management.ManagementObjectSearcher(
                        $"ASSOCIATORS OF {{Win32_LogicalDisk.DeviceID='{logicalDeviceId}'}} WHERE AssocClass = Win32_LogicalDiskToPartition"
                    );
                    foreach (var partition in logicalDiskToPartitionSearcher.Get())
                    {
                        // Use Win32_DiskDriveToDiskPartition to link partition to physical disk
                        using var diskDriveToPartitionSearcher = new System.Management.ManagementObjectSearcher(
                            $"ASSOCIATORS OF {{Win32_DiskPartition.DeviceID='{partition["DeviceID"]}'}} WHERE AssocClass = Win32_DiskDriveToDiskPartition"
                        );
                        foreach (var physicalDisk in diskDriveToPartitionSearcher.Get())
                        {
                            return (
                                PnpDeviceId: physicalDisk["PNPDeviceID"]?.ToString(),
                                PhysicalDiskIndex: (int?)(uint?)physicalDisk["Index"] // WMI returns uint
                            );
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _log?.Warn($"Error getting physical disk info for drive {driveLetter}: {ex.Message}");
            }
            return (null, null);
        }
        
        private bool _cleanRecycleBin = true;
        public bool CleanRecycleBin
        {
            get => _cleanRecycleBin;
            set { if (_cleanRecycleBin != value) { _cleanRecycleBin = value; OnPropertyChanged(); } }
        }

        private bool _cleanBrowserCache = true;
        public bool CleanBrowserCache
        {
            get => _cleanBrowserCache;
            set { if (_cleanBrowserCache != value) { _cleanBrowserCache = value; OnPropertyChanged(); } }
        }

        private bool _cleanSystemTemp = true;
        public bool CleanSystemTemp
        {
            get => _cleanSystemTemp;
            set { if (_cleanSystemTemp != value) { _cleanSystemTemp = value; OnPropertyChanged(); } }
        }

        // --- Propiedades para Configuración (Task 06) ---
        private bool _runOnStartup;
        public bool RunOnStartup
        {
            get => _runOnStartup;
            set
            {
                if (_runOnStartup != value)
                {
                    _runOnStartup = value;
                    OnPropertyChanged();
                    // Ejecutar el cambio de registro
                    _ = ToggleRunOnStartupAsync(value);
                    _ = SaveSettingsAsync();
                }
            }
        }

        private bool _isDarkMode = true;
        public bool IsDarkMode
        {
            get => _isDarkMode;
            set
            {
                if (_isDarkMode != value)
                {
                    _isDarkMode = value;
                    OnPropertyChanged();
                    
                    // Aplicar el tema sin diálogos bloqueantes en el setter
                    if (Application.Current is App app) 
                    {
                        app.ChangeTheme(value);
                    }
                    _ = SaveSettingsAsync();
                }
            }
        }

        private string _accentColor = "#3B82F6";
        public string AccentColor
        {
            get => _accentColor;
            set
            {
                if (_accentColor != value)
                {
                    _accentColor = value;
                    OnPropertyChanged();
                    // Change App Color
                    if (Application.Current is App app)
                    {
                        app.ChangeAccentColor(value);
                    }
                    _ = SaveSettingsAsync();
                }
            }
        }

        private bool _autoOptimizeRam;
        public bool AutoOptimizeRam
        {
            get => _autoOptimizeRam;
            set { if (_autoOptimizeRam != value) { _autoOptimizeRam = value; OnPropertyChanged(); _ = SaveSettingsAsync(); } }
        }

        private bool _optimizeOnIdle;
        public bool OptimizeOnIdle
        {
            get => _optimizeOnIdle;
            set { if (_optimizeOnIdle != value) { _optimizeOnIdle = value; OnPropertyChanged(); _ = SaveSettingsAsync(); } }
        }

        private bool _minimizeToTray;
        public bool MinimizeToTray
        {
            get => _minimizeToTray;
            set { if (_minimizeToTray != value) { _minimizeToTray = value; OnPropertyChanged(); _ = SaveSettingsAsync(); } }
        }


        private double _ramThresholdPercent = 85;
        public double RamThresholdPercent
        {
            get => _ramThresholdPercent;
            set { if (Math.Abs(_ramThresholdPercent - value) > 0.01) { _ramThresholdPercent = value; OnPropertyChanged(); _ = SaveSettingsAsync(); } }
        }

        private DateTime _lastAutoRamOptimization = DateTime.MinValue;

        // Variables para suavizar valores de red (promedio móvil)
        private readonly Queue<double> _netRecvHistory = new Queue<double>();
        private readonly Queue<double> _netSentHistory = new Queue<double>();
        private const int NetworkSampleSize = 3; // Promedio de las últimas 3 lecturas

        public ICommand ChangeAccentColorCommand { get; private set; }

        // Propiedad para el comando de limpiar archivos temporales
        public ICommand CleanTempFilesCommand { get; private set; }
        public ICommand RunSfcCommand { get; private set; }
        public ICommand RunDismCommand { get; private set; }
        public ICommand RunChkdskCommand { get; private set; }
        public ICommand ApplyPerformanceModeCommand { get; private set; }
        public ICommand OptimizeRamCommand { get; private set; }
        public ICommand FlushDnsCommand { get; private set; }
        public ICommand AnalyzeDiskCommand { get; private set; }
        public ICommand RebuildSearchIndexCommand { get; private set; }
        public ICommand CleanPrefetchCommand { get; private set; }
        public ICommand ResetNetworkCommand { get; private set; }
        public ICommand EnableStartupItemCommand { get; private set; }
        public ICommand DisableStartupItemCommand { get; private set; }
        public ICommand RefreshStartupItemsCommand { get; private set; }
        public ICommand StartServiceCommand { get; private set; }
        public ICommand StopServiceCommand { get; private set; }
        public ICommand RefreshServicesCommand { get; private set; }
        public ICommand RefreshDriversCommand { get; private set; }
        public ICommand OpenFolderCommand { get; private set; }
        public ICommand UninstallBloatwareAppCommand { get; private set; }
        public ICommand RefreshBloatwareAppsCommand { get; private set; }
        public ICommand UpdatePrivacySettingCommand { get; private set; }
        public ICommand RefreshPrivacySettingsCommand { get; private set; }
        public ICommand RefreshProcessesCommand { get; private set; }
        public ICommand ReduceBackgroundProcessesCommand { get; private set; }
        public ICommand SetProcessPriorityHighCommand { get; private set; }
        public ICommand SetProcessPriorityNormalCommand { get; private set; }
        public ICommand OpenLogFolderCommand { get; private set; }
        public ICommand SetProcessPriorityBelowNormalCommand { get; private set; }
        public ICommand KillProcessCommand { get; private set; }
        public ICommand RefreshThermalCommand { get; private set; }
        public ICommand RefreshDiskHealthCommand { get; private set; }
        public ICommand CreateRestorePointCommand { get; private set; }
        public ICommand UpdateAppCommand { get; private set; }
        public ICommand UpdateAllAppsCommand { get; private set; }
        public ICommand ExportDriversCommand { get; private set; }
        public ICommand CancelDriverExportCommand { get; private set; } // Added for cancellation
        public ICommand AnalyzeDiskSpaceCommand { get; private set; }
        public ICommand RefreshBatteryCommand { get; private set; }
        public ICommand RefreshUpdatableAppsCommand { get; private set; }
        public ICommand CancelUpdateSearchCommand { get; private set; }
        public ICommand CancelAppUpdateCommand { get; private set; }
        public ICommand OpenDiscordCommand { get; private set; } // Nuevo Comando Discord
        public ICommand ClearProcessSearchCommand { get; private set; }
        public ICommand ClearServiceSearchCommand { get; private set; }
        public ICommand ToggleServiceCommand { get; private set; }
        public ICommand FreeUpDiskSpaceCommand { get; private set; }
        public ICommand PcBoostCommand { get; private set; }
        public ICommand DeepScanCommand { get; private set; }
        public ICommand OpenCleanmgrCommand { get; private set; }
        public ICommand RunToolCommand { get; private set; }



        // Método que se ejecuta cuando se invoca el comando CleanTempFilesCommand
        private async Task ExecuteCleanTempFilesAsync()
        {
            if (IsBusy) return;

            // Build up the cleaning options based on checkboxes
            var cleaningOptions = new CleaningOptions
            {
                CleanRecycleBin = CleanRecycleBin,
                CleanBrowserCache = CleanBrowserCache,
                CleanSystemTemp = CleanSystemTemp
                // Añadir otras opciones a medida que se implementen
            };

            try
            {
                IsBusy = true;
                _log?.Info("Inicio de limpieza de temporales");
                var result = await _maintenance.CleanTemporaryFilesAsync(cleaningOptions); // Pass options
                _log?.Info($"Limpieza completada. {result}");
                await _dialogService.ShowMessage(result.ToString(), "Limpieza Completada");
            }
            catch (System.Exception ex)
            {
                _log?.Error("Error durante la limpieza", ex);
                await _dialogService.ShowMessage($"Error durante la limpieza: {ex.Message}", "Error");
            }
            finally
            {
                IsBusy = false;
            }

        }

        private void ExecuteOpenCleanmgr()
        {
            try
            {
                Process.Start("cleanmgr.exe");
                _log?.Info("Liberador de espacio iniciado.");
            }
            catch (Exception ex)
            {
                _log?.Error("Error al iniciar cleanmgr", ex);
            }
        }

        private void ExecuteOpenDiscord(object? parameter)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "https://discord.gg/9Z3Z3Z3", // Ejemplo de enlace
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                _log?.Error("No se pudo abrir Discord", ex);
                _dialogService.ShowMessage($"Error al abrir Discord: {ex.Message}", "Error");
            }
        }

        private async Task ExecutePcBoostAsync()
        {
            if (IsBusy) return;
            try
            {
                IsBusy = true;
                StatusMessage = "Iniciando PC Boost...";
                _log?.Info("PC Boost iniciado por el usuario.");

                // 1. Optimizar Memoria
                StatusMessage = "Optimizando memoria RAM...";
                await _maintenance.OptimizeMemoryAsync();
                await Task.Delay(500); // Pequeña pausa para feedback visual

                // 2. Limpiar Archivos Temporales (Opciones estándar)
                StatusMessage = "Limpiando archivos temporales...";
                var options = new CleaningOptions 
                { 
                    CleanSystemTemp = true, 
                    CleanRecycleBin = true, 
                    CleanBrowserCache = true 
                };
                await _maintenance.CleanTemporaryFilesAsync(options);
                await Task.Delay(500);

                // 3. Reducir procesos en segundo plano
                StatusMessage = "Reduciendo procesos innecesarios...";
                await _processManagerService.ReduceBackgroundProcessesAsync(System.Diagnostics.ProcessPriorityClass.BelowNormal);

                GeneralStatusMessage = "Estado General: ¡Sistema Optimizado!";
                StatusMessage = "PC Boost completado con éxito.";
                
                // Recargar info para reflejar cambios si es necesario
                _ = UpdateThermalAsync();
                UpdateSystemUsage();

                await _dialogService.ShowMessage("El sistema ha sido optimizado correctamente:\n- Memoria RAM liberada.\n- Archivos temporales eliminados.\n- Procesos de fondo ajustados.", "PC Boost Finalizado");
            }
            catch (Exception ex)
            {
                _log?.Error("Error durante PC Boost", ex);
                await _dialogService.ShowMessage($"Ocurrió un error: {ex.Message}", "Error en PC Boost");
            }
            finally
            {
                IsBusy = false;
                StatusMessage = "";
            }
        }

        private async Task ExecuteDeepScanAsync()
        {
            if (IsBusy) return;
            try
            {
                IsBusy = true;
                StatusMessage = "Iniciando Escaneo Profundo...";
                DeepScanResult.Clear();

                // Escanear carpeta Descargas
                string downloadsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
                if (Directory.Exists(downloadsPath))
                {
                    StatusMessage = "Escaneando carpeta de Descargas...";
                    var largeFiles = await _diskAnalyzerService.FindLargeFilesAsync(downloadsPath, 50 * 1024 * 1024); // > 50MB
                    foreach (var file in largeFiles)
                    {
                        DeepScanResult.Add(file);
                    }
                }

                // Escanear Escritorio por archivos grandes
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                if (Directory.Exists(desktopPath))
                {
                    StatusMessage = "Escaneando Escritorio...";
                    var largeFiles = await _diskAnalyzerService.FindLargeFilesAsync(desktopPath, 100 * 1024 * 1024); // > 100MB
                    foreach (var file in largeFiles)
                    {
                        DeepScanResult.Add(file);
                    }
                }

                StatusMessage = "Análisis completado.";
                await _dialogService.ShowMessage($"Escaneo profundo finalizado. Se encontraron {DeepScanResult.Count} archivos grandes.", "Limpieza Profunda");
            }
            catch (Exception ex)
            {
                _log?.Error("Error durante Escaneo Profundo", ex);
                await _dialogService.ShowMessage($"Error: {ex.Message}", "Error");
            }
            finally
            {
                IsBusy = false;
                StatusMessage = "";
            }
        }


        private async Task ExecuteOptimizeRamAsync()
        {
            if (IsBusy) return;
            try
            {
                IsBusy = true;
                _log?.Info("Optimizando RAM...");
                var result = await _maintenance.OptimizeMemoryAsync();
                _log?.Info($"RAM Optimizada. {result.Notes}");
                await _dialogService.ShowMessage("Memoria RAM optimizada correctamente.", "Optimización");
            }
            catch (Exception ex)
            {
                _log?.Error("Error optimizando RAM", ex);
                await _dialogService.ShowMessage($"Error optimizando RAM: {ex.Message}", "Error");
            }
            finally
            {
                IsBusy = false;
            }
        }


        private async Task ExecuteRunSfc()
        {
            _log?.Info("Lanzando SFC /scannow");
            var r = await _maintenance.LaunchSystemFileCheckerAsync();
            _log?.Info($"SFC resultado: {r.Started} - {r.Message}. Exit Code: {r.ExitCode}");
            
            string message = r.Message ?? (r.Started ? "SFC iniciado." : "No se pudo iniciar SFC.");
            if (!string.IsNullOrEmpty(r.StandardOutput))
            {
                message += Environment.NewLine + "Output: " + r.StandardOutput;
            }
            if (!string.IsNullOrEmpty(r.StandardError))
            {
                message += Environment.NewLine + "Error: " + r.StandardError;
            }
            await _dialogService.ShowMessage(message, "Reparador SFC");
        }

        private async Task ExecuteRunDism()
        {
            _log?.Info("Lanzando DISM /RestoreHealth");
            var r = await _maintenance.LaunchDISMHealthRestoreAsync();
            _log?.Info($"DISM resultado: {r.Started} - {r.Message}. Exit Code: {r.ExitCode}");

            string message = r.Message ?? (r.Started ? "DISM iniciado." : "No se pudo iniciar DISM.");
            if (!string.IsNullOrEmpty(r.StandardOutput))
            {
                message += Environment.NewLine + "Output: " + r.StandardOutput;
            }
            if (!string.IsNullOrEmpty(r.StandardError))
            {
                message += Environment.NewLine + "Error: " + r.StandardError;
            }
            await _dialogService.ShowMessage(message, "Reparador DISM");
        }

        // --- Navigation ---


        public ICommand NavigateCommand { get; private set; }

        private async void ExecuteNavigate(object? parameter)
        {
            if (parameter == null) return;
            try 
            {
                AppSection target;
                if (parameter is AppSection section) target = section;
                else if (parameter is string s && Enum.TryParse(s, true, out AppSection parsed)) target = parsed;
                else 
                {
                    _log?.Warn($"Navegación fallida: parámetro inválido '{parameter}'");
                    return;
                }

                if (CurrentSection == target) return;
                
                _log?.Info($"Navegando a sección: {target}");
                CurrentSection = target;
                IsBusy = true;

                // Carga inteligente: EN PARALELO para no bloquear la UI
                _ = Task.Run(async () =>
                {
                    try
                    {
                        switch (target)
                        {
                            case AppSection.Dashboard:
                                await LoadSecurityStatusAsync();
                                await LoadStartupItemsAsync();
                                await LoadBatteryInfoAsync();
                                break;
                            case AppSection.Proteccion:
                                await LoadSecurityStatusAsync();
                                break;
                            case AppSection.Rendimiento:
                                if (Processes == null || Processes.Count == 0) await LoadProcessesAsync();
                                _ = UpdateThermalAsync();
                                if (WindowsServices == null || WindowsServices.Count == 0) await LoadWindowsServicesAsync();
                                break;
                            case AppSection.Aplicaciones:
                                if (BloatwareApps == null || BloatwareApps.Count == 0) await LoadBloatwareAppsAsync();
                                _ = LoadUpdatableAppsAsync(); 
                                if (StartupItems == null || StartupItems.Count == 0) await LoadStartupItemsAsync();
                                break;
                            case AppSection.Herramientas:
                                await LoadLastRestorePointAsync();
                                break;
                            case AppSection.Hardware:
                                _log?.Info("Iniciando carga de SystemInfo para Hardware.");
                                var t1 = WithTimeout((SystemInformation == null || string.IsNullOrWhiteSpace(SystemInformation.MachineName)) ? LoadSystemInfoAsync() : Task.CompletedTask);
                                _log?.Info("Iniciando carga de DiskHealth para Hardware.");
                                var t2 = (DiskHealth == null || DiskHealth.Count == 0) ? LoadDiskHealthAsync() : Task.CompletedTask; // Ya tiene timeout interno
                                _log?.Info("Iniciando carga de UnifiedDisks para Hardware.");
                                var t3 = WithTimeout((UnifiedDisks == null || UnifiedDisks.Count == 0) ? LoadDisksAsync() : Task.CompletedTask);
                                _log?.Info("Iniciando carga de PrivacySettings para Hardware.");
                                var t4 = WithTimeout((PrivacySettings == null || PrivacySettings.Count == 0) ? LoadPrivacySettingsAsync() : Task.CompletedTask);
                                await Task.WhenAll(t1, t2, t3, t4);
                                _log?.Info("Finalizada la carga de todos los datos para Hardware.");
                                break;
                            case AppSection.Configuracion:
                                await LoadSettingsAsync();
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        _log?.Error($"Error durante la carga de {target}", ex);
                    }
                    finally
                    {
                        System.Windows.Application.Current.Dispatcher.Invoke(() => IsBusy = false);
                    }
                });
            }
            catch (Exception ex)
            {
                _log?.Error("Error crítico en navegación", ex);
                IsBusy = false;
            }
        }

        private async Task WithTimeout(Task task, int timeoutSeconds = 20)
        {
            var completedTask = await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(timeoutSeconds)));
            if (completedTask != task)
            {
                throw new TimeoutException($"Una operación de carga de datos excedió el límite de {timeoutSeconds} segundos.");
            }
            await task; // Re-lanza la excepción original si la hubo
        }
        
        // ... existing methods like ExecuteRunChkdsk ...
        private async Task ExecuteRunChkdsk()
        {
            _log?.Info("Lanzando CHKDSK");
            var r = await _maintenance.LaunchCHKDSKAsync();
            _log?.Info($"CHKDSK resultado: {r.Started} - {r.Message}. Exit Code: {r.ExitCode}");

            string message = r.Message ?? (r.Started ? "CHKDSK iniciado." : "No se pudo iniciar CHKDSK.");
            if (!string.IsNullOrEmpty(r.StandardOutput))
            {
                message += Environment.NewLine + "Output: " + r.StandardOutput;
            }
            if (!string.IsNullOrEmpty(r.StandardError))
            {
                message += Environment.NewLine + "Error: " + r.StandardError;
            }
            await _dialogService.ShowMessage(message, "Reparador CHKDSK");
        }

        private async Task ExecuteFlushDnsAsync()
        {
            if (IsBusy) return;
            try
            {
                IsBusy = true;
                _log?.Info("Lanzando Flush DNS");
                var r = await _maintenance.FlushDnsAsync();
                await _dialogService.ShowMessage(r.Message ?? "Flush DNS completado.", "Red");
            }
            catch (Exception ex)
            {
                _log?.Error("Error Flush DNS", ex);
                await _dialogService.ShowMessage(ex.Message, "Error");
            }
            finally { IsBusy = false; }
        }

        private async Task ExecuteAnalyzeDiskAsync()
        {
            if (IsBusy) return;
            try
            {
                IsBusy = true;
                _log?.Info("Lanzando Análisis de Disco");
                var r = await _maintenance.AnalyzeDiskAsync();
                await _dialogService.ShowMessage(r.Message ?? "Análisis completado.", "Disco");
            }
            catch (Exception ex)
            {
                _log?.Error("Error Análisis Disco", ex);
                await _dialogService.ShowMessage(ex.Message, "Error");
            }
            finally { IsBusy = false; }
        }

        private async Task ExecuteCleanPrefetchAsync()
        {
            if (IsBusy) return;

            if (!IsAdministrator())
            {
                await _dialogService.ShowMessage("Esta función requiere que la aplicación se ejecute con privilegios de administrador. Por favor, reinicie WassControlSys como Administrador.", "Privilegios Requeridos");
                return;
            }

            try
            {
                IsBusy = true;
                _log?.Info("Lanzando Limpieza de Prefetch");
                var r = await _maintenance.CleanPrefetchAsync();
                await _dialogService.ShowMessage(r.Message ?? "Limpieza completada.", "Mantenimiento");
            }
            catch (Exception ex)
            {
                _log?.Error("Error Prefetch", ex);
                await _dialogService.ShowMessage(ex.Message, "Error");
            }
            finally { IsBusy = false; }
        }

        private async Task ExecuteRebuildSearchIndexAsync()
        {
             _log?.Info("Lanzando Reconstrucción de Índice de Búsqueda");
             // No async wait needed for launching control panel usually
             var r = await _maintenance.RebuildSearchIndexAsync();
             if (!r.Started) await _dialogService.ShowMessage("No se pudo abrir el panel de opciones de indización.", "Error");
        }

        private async Task ExecuteResetNetworkAsync()
        {
             if (IsBusy) return;
             bool confirm = await _dialogService.ShowConfirmation("Esto reiniciará sus adaptadores de red y puede perder la conexión temporalmente. ¿Continuar?", "Reiniciar Red");
             if (!confirm) return;

            try
            {
                IsBusy = true;
                _log?.Info("Reiniciando Red...");
                var r = await _maintenance.ResetNetworkAsync();
                await _dialogService.ShowMessage(r.Message ?? "Comandos de red ejecutados.", "Red Reiniciada");
            }
            catch (Exception ex)
            {
                _log?.Error("Error Reset Network", ex);
                await _dialogService.ShowMessage(ex.Message, "Error");
            }
            finally { IsBusy = false; }
        }

        private async void UpdateSystemUsage()
        {
            if (!IsWindowVisible) return; // No monitorizar si la ventana está oculta

            var usage = await _monitoringService.GetSystemUsageAsync();
            CpuUsage = usage.CpuUsage;
            RamUsage = usage.RamUsage;
            DiskUsage = usage.DiskUsage;
            
            if (usage.CpuPerCore != null)
            {
                // Actualizar colección existente para evitar parpadeos y crashes en la UI
                if (CpuPerCore.Count != usage.CpuPerCore.Length)
                {
                    var list = new ObservableCollection<CpuCoreInfo>();
                    for (int i = 0; i < usage.CpuPerCore.Length; i++)
                    {
                        list.Add(new CpuCoreInfo { Index = i, Usage = usage.CpuPerCore[i] });
                    }
                    CpuPerCore = list;
                }
                else
                {
                    for (int i = 0; i < usage.CpuPerCore.Length; i++)
                    {
                        // Actualizar propiedad directamente sin recrear el objeto
                        CpuPerCore[i].Usage = usage.CpuPerCore[i];
                    }
                }
            }
            // Calcular valores instantáneos
            double instantRecv = usage.NetBytesReceivedPerSec / (1024.0 * 1024.0) * 8.0;
            double instantSent = usage.NetBytesSentPerSec / (1024.0 * 1024.0) * 8.0;
            
            // Agregar a historial
            _netRecvHistory.Enqueue(instantRecv);
            _netSentHistory.Enqueue(instantSent);
            
            // Mantener solo las últimas N lecturas
            if (_netRecvHistory.Count > NetworkSampleSize)
                _netRecvHistory.Dequeue();
            if (_netSentHistory.Count > NetworkSampleSize)
                _netSentHistory.Dequeue();
            
            // Calcular promedio móvil (Protección contra secuencias vacías)
            NetRecvMbps = _netRecvHistory.Any() ? _netRecvHistory.Average() : 0;
            NetSentMbps = _netSentHistory.Any() ? _netSentHistory.Average() : 0;
            
            ActiveTcpConnections = usage.ActiveTcpConnections;
            DiskReadsPerSec = usage.DiskReadsPerSec;
            DiskWritesPerSec = usage.DiskWritesPerSec;
            DiskAvgQueueLength = usage.DiskAvgQueueLength;
            DiskReadLatencyMs = usage.DiskReadLatencyMs;
            DiskWriteLatencyMs = usage.DiskWriteLatencyMs;
            if (AutoOptimizeRam && RamUsage >= RamThresholdPercent)
            {
                if ((DateTime.Now - _lastAutoRamOptimization) > TimeSpan.FromMinutes(5))
                {
                    _ = ExecuteOptimizeRamAsync();
                    _lastAutoRamOptimization = DateTime.Now;
                }
            }
        }

        private async Task LoadSettingsAsync()
        {
            try
            {
                var s = await _settings.LoadAsync();
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    CurrentMode = s.SelectedMode;
                    RunOnStartup = s.RunOnStartup;
                    AccentColor = s.AccentColor;
                    AutoOptimizeRam = s.AutoOptimizeRam;
                    RamThresholdPercent = s.RamThresholdPercent;
                    SelectedLanguage = s.Language;
                    IsDarkMode = s.IsDarkMode;
                    OptimizeOnIdle = s.OptimizeOnIdle;
                    MinimizeToTray = s.MinimizeToTray;
                });

                _log?.Info($"Settings cargados. Modo={s.SelectedMode}, Sección={s.CurrentSection}, Idle={s.OptimizeOnIdle}");
            }
            catch (Exception ex)
            {
                _log?.Error("Error cargando settings", ex);
            }
        }

        private async Task SaveSettingsAsync()
        {
            try

            {
                var s = new AppSettings 
                { 
                    SelectedMode = CurrentMode, 
                    CurrentSection = CurrentSection.ToString(),
                    RunOnStartup = RunOnStartup,
                    AccentColor = AccentColor,
                    AutoOptimizeRam = AutoOptimizeRam,
                    RamThresholdPercent = RamThresholdPercent,
                    Language = SelectedLanguage,
                    IsDarkMode = IsDarkMode,
                    OptimizeOnIdle = OptimizeOnIdle,
                    MinimizeToTray = MinimizeToTray
                };
                await _settings.SaveAsync(s);
            }
            catch (Exception ex)
            {
                _log?.Error("Error guardando settings", ex);
            }
        }

        private async Task ToggleRunOnStartupAsync(bool enable)
        {
            await Task.Run(() =>
            {
                try
                {
                    using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
                    if (enable)
                    {
                        var process = System.Diagnostics.Process.GetCurrentProcess();
                        string? path = process.MainModule?.FileName;
                        if (!string.IsNullOrEmpty(path)) key?.SetValue("WassControlSys", path);
                    }
                    else
                    {
                        key?.DeleteValue("WassControlSys", false);
                    }
                }
                catch (Exception ex)
                {
                    _log?.Error("Error changing startup registry", ex);
                }
            });
        }

        private async Task ExecuteApplyModeAsync(PerformanceMode mode)
        {
            if (IsBusy) return;
            try
            {
                IsBusy = true;
                StatusMessage = $"Estableciendo Modo {mode}...";
                _log?.Info($"Aplicando perfil: {mode}");
                
                // Pequeña pausa para que la animación sea visible
                await Task.Delay(400); 
                
                var r = await _profiles.ApplyProfileAsync(mode);
                
                _log?.Info($"Perfil resultado: {r.Success} - {r.Message}");
                
                if (r.Success)
                {
                    StatusMessage = "¡Optimización Aplicada!";
                    await Task.Delay(1000);
                }
                else
                {
                    await _dialogService.ShowMessage(r.Message ?? "No se pudo aplicar el perfil.", "Aviso");
                }
            }
            catch (System.Exception ex)
            {
                _log?.Error("Error al aplicar el perfil", ex);
                await _dialogService.ShowMessage($"Error al aplicar el perfil: {ex.Message}", "Error");
            }
            finally
            {
                IsBusy = false;
                StatusMessage = "";
            }
        }

        private async Task LoadSystemInfoAsync()
        {
            try
            {
                var sysInfo = await _systemInfoService.GetSystemInfoAsync();
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    SystemInformation = sysInfo;
                });
            }
            catch (Exception ex)
            {
                _log?.Error("Error cargando info del sistema", ex);
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    SystemInformation = new SystemInfo { MachineName = "Error al cargar" };
                });
            }
        }

        private async Task LoadSecurityStatusAsync()
        {
            try
            {
                var status = await _securityService.GetSecurityStatusAsync();
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    SecurityStatus = status;
                });
            }
            catch (Exception ex)
            {
                _log?.Error("Error cargando estado de seguridad", ex);
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    SecurityStatus = new SecurityStatus { AntivirusName = "Error de carga", IsAntivirusEnabled = false };
                });
            }
        }

        private async Task LoadStartupItemsAsync()
        {
            try
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => IsBusy = true);
                var items = await _startupService.GetStartupItemsAsync();
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    StartupItems = new ObservableCollection<StartupItem>(items);
                });
            }
            catch (Exception ex)
            {
                _log?.Error("Error cargando elementos de inicio", ex);
                await System.Windows.Application.Current.Dispatcher.Invoke(async () =>
                {
                    await _dialogService.ShowMessage("No se pudieron cargar los elementos de inicio.", "Error");
                });
            }
            finally
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => IsBusy = false);
            }
        }

        private async Task ExecuteEnableStartupItemAsync(StartupItem item)
        {
            if (item == null || IsBusy) return;
            try
            {
                IsBusy = true;
                _log?.Info($"Habilitando elemento de inicio: {item.Name}");
                bool success = await _startupService.EnableStartupItemAsync(item);
                if (success)
                {
                    item.IsEnabled = true; // Update UI
                    await _dialogService.ShowMessage($"Elemento '{item.Name}' habilitado.", "Éxito");
                }
                else
                {
                    await _dialogService.ShowMessage($"No se pudo habilitar el elemento '{item.Name}'.", "Error");
                }
            }
            catch (Exception ex)
            {
                _log?.Error($"Error al habilitar el elemento de inicio '{item.Name}'", ex);
                await _dialogService.ShowMessage($"Error al habilitar el elemento de inicio: {ex.Message}", "Error");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ExecuteDisableStartupItemAsync(StartupItem item)
        {
            if (item == null || IsBusy) return;
            // Confirmation before disabling potentially critical items
            bool confirm = await _dialogService.ShowConfirmation($"¿Está seguro de que desea deshabilitar '{item.Name}'? Esto podría afectar el funcionamiento de algunas aplicaciones.", "Confirmar Deshabilitación");
            if (!confirm) return;

            try
            {
                IsBusy = true;
                _log?.Info($"Deshabilitando elemento de inicio: {item.Name}");
                bool success = await _startupService.DisableStartupItemAsync(item);
                if (success)
                {
                    item.IsEnabled = false; // Update UI
                    await _dialogService.ShowMessage($"Elemento '{item.Name}' deshabilitado.", "Éxito");
                }
                else
                {
                    await _dialogService.ShowMessage($"No se pudo deshabilitar el elemento '{item.Name}'.", "Error");
                }
            }
            catch (Exception ex)
            {
                _log?.Error($"Error al deshabilitar el elemento de inicio '{item.Name}'", ex);
                await _dialogService.ShowMessage($"Error al deshabilitar el elemento de inicio: {ex.Message}", "Error");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoadWindowsServicesAsync()
        {
            if (IsBusy) return;
            try
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => IsBusy = true);
                _log?.Info("Cargando servicios de Windows...");
                var services = await _serviceOptimizerService.GetWindowsServicesAsync();
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    _allWindowsServices = new ObservableCollection<WindowsService>(services);
                    FilterServices();
                });
                _log?.Info($"Servicios de Windows cargados: {WindowsServices.Count}");
            }
            catch (Exception ex)
            {
                _log?.Error("Error cargando servicios de Windows", ex);
                await _dialogService.ShowMessage($"Error cargando servicios de Windows: {ex.Message}", "Error");
            }
            finally
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => IsBusy = false);
            }
        }

        private async Task ExecuteStartServiceAsync(string serviceName)
        {
            if (string.IsNullOrEmpty(serviceName) || IsBusy) return;

            // Optional: Confirmation dialog
            bool confirm = await _dialogService.ShowConfirmation($"¿Está seguro de que desea iniciar el servicio '{serviceName}'?", "Confirmar Inicio de Servicio");
            if (!confirm) return;

            try
            {
                IsBusy = true;
                _log?.Info($"Iniciando servicio: {serviceName}");
                bool success = await _serviceOptimizerService.StartServiceAsync(serviceName);
                if (success)
                {
                    // Update the status of the service in the collection
                    var service = WindowsServices.FirstOrDefault(s => s.Name == serviceName);
                    if (service != null) service.Status = ServiceStatus.Running; // Update UI directly
                    await _dialogService.ShowMessage($"Servicio '{serviceName}' iniciado correctamente.", "Éxito");
                }
                else
                {
                    await _dialogService.ShowMessage($"No se pudo iniciar el servicio '{serviceName}'.", "Error");
                    await TryRelaunchAsAdminAsync($"La operación para iniciar el servicio '{serviceName}' requiere permisos de administrador.");
                }
            }
            catch (Exception ex)
            {
                _log?.Error($"Error al iniciar el servicio '{serviceName}'", ex);
                await _dialogService.ShowMessage($"Error al iniciar el servicio '{serviceName}': {ex.Message}", "Error");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ExecuteToggleServiceAsync(WindowsService service)
        {
            if (service == null) return;
            if (service.IsRunning)
            {
                await ExecuteStopServiceAsync(service.Name);
            }
            else
            {
                await ExecuteStartServiceAsync(service.Name);
            }
        }

        private async Task ExecuteStopServiceAsync(string serviceName)
        {
            if (string.IsNullOrEmpty(serviceName) || IsBusy) return;

            // Optional: Confirmation dialog
            bool confirm = await _dialogService.ShowConfirmation($"¿Está seguro de que desea detener el servicio '{serviceName}'? Esto podría afectar la estabilidad del sistema.", "Confirmar Detención de Servicio");
            if (!confirm) return;

            try
            {
                IsBusy = true;
                _log?.Info($"Deteniendo servicio: {serviceName}");
                bool success = await _serviceOptimizerService.StopServiceAsync(serviceName);
                if (success)
                {
                    // Update the status of the service in the collection
                    var service = WindowsServices.FirstOrDefault(s => s.Name == serviceName);
                    if (service != null) service.Status = ServiceStatus.Stopped; // Update UI directly
                    await _dialogService.ShowMessage($"Servicio '{serviceName}' detenido correctamente.", "Éxito");
                }
                else
                {
                    await _dialogService.ShowMessage($"No se pudo detener el servicio '{serviceName}'.", "Error");
                    await TryRelaunchAsAdminAsync($"La operación para detener el servicio '{serviceName}' requiere permisos de administrador.");
                }
            }
            catch (Exception ex)
            {
                _log?.Error($"Error al detener el servicio '{serviceName}'", ex);
                await _dialogService.ShowMessage($"Error al detener el servicio '{serviceName}': {ex.Message}", "Error");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task TryRelaunchAsAdminAsync(string reason)
        {
            try
            {
                bool confirm = await _dialogService.ShowConfirmation($"{reason}\n\n¿Desea reiniciar la aplicación con privilegios de administrador?", "Permisos requeridos");
                if (!confirm) return;
                string exePath = System.IO.Path.Combine(AppContext.BaseDirectory, "WassControlSys.exe");
                if (!System.IO.File.Exists(exePath))
                {
                    using var proc = System.Diagnostics.Process.GetCurrentProcess();
                    exePath = proc.MainModule?.FileName ?? string.Empty;
                }
                if (string.IsNullOrWhiteSpace(exePath)) return;
                var psi = new System.Diagnostics.ProcessStartInfo(exePath)
                {
                    UseShellExecute = true,
                    Verb = "runas"
                };
                System.Diagnostics.Process.Start(psi);
                Environment.Exit(0);
            }
            catch (System.ComponentModel.Win32Exception)
            {
            }
            catch
            {
            }
        }

        private async Task LoadBloatwareAppsAsync()
        {
            if (IsBusy) return;
            try
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => IsBusy = true);
                var apps = await _bloatwareService.GetBloatwareAppsAsync();
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    BloatwareApps = new ObservableCollection<BloatwareApp>(apps);
                });
            }
            catch (Exception ex)
            {
                _log?.Error("Error cargando apps de bloatware", ex);
            }
            finally
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => IsBusy = false);
            }
        }

        private async Task ExecuteUninstallBloatwareAppAsync(BloatwareApp app)
        {
            if (app == null || app.IsUninstalling) return; // Check app-specific flag

            bool confirm = await _dialogService.ShowConfirmation($"¿Está seguro de que desea desinstalar '{app.Name}'? Esta acción no se puede deshacer.", "Confirmar Desinstalación");
            if (!confirm) return;

            try
            {
                app.IsUninstalling = true; // Set app-specific flag
                _log?.Info($"Desinstalando aplicación de bloatware: {app.Name}");
                bool success = await _bloatwareService.UninstallBloatwareAppAsync(app);
                if (success)
                {
                    BloatwareApps.Remove(app); // Remove from UI list
                    // The service now shows the final dialog.
                }
                // The service shows the error dialog on failure.
            }
            catch (Exception ex)
            {
                _log?.Error($"Error al desinstalar la aplicación de bloatware '{app.Name}'", ex);
                await _dialogService.ShowMessage($"Error al desinstalar '{app.Name}': {ex.Message}", "Error");
            }
            finally
            {
                app.IsUninstalling = false; // Reset app-specific flag
            }
        }

        private async Task LoadPrivacySettingsAsync()
        {
            if (IsBusy) return;
            try
            {
                IsBusy = true;
                var settings = await _privacyService.GetPrivacySettingsAsync();
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    PrivacySettings = new ObservableCollection<PrivacySetting>(settings);
                });
            }
            catch (Exception ex)
            {
                _log?.Error("Error cargando configuración de privacidad", ex);
            }
            finally
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => IsBusy = false);
            }
        }

        private async Task ExecuteUpdatePrivacySettingAsync(PrivacySetting setting)
        {
            if (setting == null || IsBusy) return;
            try
            {
                IsBusy = true;
                _log?.Info($"Actualizando configuración de privacidad: {setting.Name} a {setting.CurrentValue}");
                bool success = await _privacyService.UpdatePrivacySettingAsync(setting, setting.CurrentValue);
                if (success)
                {
                    await _dialogService.ShowMessage($"Configuración '{setting.Name}' actualizada.", "Éxito");
                }
                else
                {
                    await _dialogService.ShowMessage($"No se pudo actualizar la configuración '{setting.Name}'.", "Error");
                    // Revert UI change if update failed
                    setting.CurrentValue = !setting.CurrentValue;
                }
            }
            catch (Exception ex)
            {
                _log?.Error($"Error al actualizar la configuración de privacidad '{setting.Name}'", ex);
                await _dialogService.ShowMessage($"Error al actualizar la configuración de privacidad '{setting.Name}': {ex.Message}", "Error");
                 // Revert UI change if update failed
                setting.CurrentValue = !setting.CurrentValue;
            }
            finally
            {
                IsBusy = false;
            }
        }

        // ===== NEW OPTIMIZATION METHODS =====

        private bool IsAdministrator()
        {
            using (var identity = WindowsIdentity.GetCurrent())
            {
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }
        

        private async Task ExecuteCreateRestorePointAsync()
        {
            if (IsBusy) return;

            if (!IsAdministrator())
            {
                await _dialogService.ShowMessage("Esta función requiere que la aplicación se ejecute con privilegios de administrador. Por favor, reinicie WassControlSys como Administrador.", "Privilegios Requeridos");
                return;
            }

            try
            {
                IsBusy = true;
                StatusMessage = "Creando punto de restauración...";
                var r = await _restorePointService.CreateRestorePointAsync("Manual WassControlSys Checkpoint");
                if (r.Success)
                {
                    await LoadLastRestorePointAsync();
                }
                await _dialogService.ShowMessage(r.Message ?? "Operación finalizada.", r.Success ? "Éxito" : "Error");
            }
            finally { IsBusy = false; StatusMessage = ""; }
        }

        private async Task LoadLastRestorePointAsync()
        {
            try
            {
                var rp = await _restorePointService.GetLastRestorePointAsync();
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    if (!string.IsNullOrEmpty(rp.Name))
                    {
                        LastRestorePointName = rp.Name;
                        LastRestorePointDate = rp.Date?.ToString("g") ?? "";
                    }
                    else
                    {
                        LastRestorePointName = "No se encontraron puntos.";
                        LastRestorePointDate = "";
                    }
                });
            }
            catch (Exception ex)
            {
                _log?.Error("Error cargando el último punto de restauración", ex);
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    LastRestorePointName = "Error al cargar";
                    LastRestorePointDate = "";
                });
            }
        }

        private async Task LoadBatteryInfoAsync()
        {
            if (IsBusy) return;
            try
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => IsBusy = true);
                var info = await _batteryService.GetBatteryStatusAsync();
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    BatteryInfo = info;
                });
            }
            catch (Exception ex)
            {
                _log?.Error("Error cargando información de la batería", ex);
            }
            finally
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => IsBusy = false);
            }
        }

        private async Task LoadUpdatableAppsAsync()
        {
            try
            {
                _updateSearchCts?.Cancel(); // Cancelar búsqueda anterior si existe
                _updateSearchCts = new CancellationTokenSource();
                var token = _updateSearchCts.Token;

                IsSearchingUpdates = true;
                StatusMessage = "Buscando actualizaciones...";
                var apps = await _wingetService.GetUpdatableAppsAsync(token);
                
                if (!token.IsCancellationRequested)
                {
                    UpdatableApps = new ObservableCollection<WingetApp>(apps);
                    _log?.Info($"Actualizaciones winget encontradas: {UpdatableApps.Count}");
                }
            }
            catch (OperationCanceledException)
            {
                _log?.Info("Búsqueda de actualizaciones cancelada por el usuario.");
            }
            catch (Exception ex)
            {
                _log?.Error("Error cargando winget", ex);
                if (IsSearchingUpdates) await _dialogService.ShowMessage($"Error al buscar actualizaciones: {ex.Message}", "Error");
            }
            finally 
            { 
                IsSearchingUpdates = false; 
                StatusMessage = ""; 
                _updateSearchCts = null;
            }
        }

        private async Task ExecuteUpdateAppAsync(string id)
        {
            if (string.IsNullOrEmpty(id)) return;

            var appToUpdate = UpdatableApps.FirstOrDefault(app => app.Id == id);
            if (appToUpdate == null || appToUpdate.IsUpdating) return;

            // Check for confirmation
            if (!appToUpdate.Source.Equals("msstore", StringComparison.OrdinalIgnoreCase))
            {
                bool confirm = await _dialogService.ShowConfirmation($"¿Desea actualizar '{appToUpdate.Name}'? Esta aplicación no es de la Microsoft Store.", "Confirmar Actualización");
                if (!confirm) return;
            }

            var cts = new CancellationTokenSource();
            _appUpdateCts[id] = cts;

            try
            {
                appToUpdate.IsUpdating = true;
                appToUpdate.UpdateStatusMessage = "Iniciando...";
                appToUpdate.UpdateProgress = 0;

                var progress = new Progress<(int percentage, string message)>(report =>
                {
                    appToUpdate.UpdateProgress = report.percentage;
                    appToUpdate.UpdateStatusMessage = report.message;
                });

                bool success = await _wingetService.UpdateAppAsync(id, progress, cts.Token);
                
                if (success)
                {
                    UpdatableApps.Remove(appToUpdate);
                    _log?.Info($"App actualizada: {id}");
                }
                else if (!cts.Token.IsCancellationRequested)
                {
                    appToUpdate.UpdateStatusMessage = "Reintentar actualización.";
                }
            }
            catch (OperationCanceledException)
            {
                appToUpdate.UpdateStatusMessage = "Cancelado.";
            }
            catch (Exception ex)
            {
                _log?.Error($"Error al actualizar la aplicación {id}", ex);
                if (appToUpdate != null) appToUpdate.UpdateStatusMessage = "Error.";
            }
            finally
            {
                if (appToUpdate != null) appToUpdate.IsUpdating = false;
                _appUpdateCts.Remove(id);
            }
        }

        private void ExecuteCancelAppUpdate(string? id)
        {
            if (id != null && _appUpdateCts.TryGetValue(id, out var cts))
            {
                cts.Cancel();
                _log?.Info($"Actualización cancelada por usuario: {id}");
            }
        }

        private async Task ExecuteUpdateAllAppsAsync()
        {
            if (IsBusy) return;

            bool confirm = await _dialogService.ShowConfirmation("¿Está seguro de que desea actualizar todas las aplicaciones? Esta acción puede tardar varios minutos.", "Confirmar Actualización Masiva");
            if (!confirm) return;

            try
            {
                IsBusy = true;
                StatusMessage = "Actualizando aplicaciones...";
                
                var appsToUpdate = UpdatableApps.ToList();
                foreach (var app in appsToUpdate)
                {
                    StatusMessage = $"Actualizando {app.Name}...";
                    await ExecuteUpdateAppAsync(app.Id);
                }
            }
            finally { IsBusy = false; StatusMessage = ""; }
        }

        private async Task ExecuteExportDriversAsync()
        {
            if (IsExportingDrivers) return; // Prevent multiple exports

            if (!IsAdministrator())
            {
                await _dialogService.ShowMessage("Esta función requiere que la aplicación se ejecute con privilegios de administrador.", "Privilegios Requeridos");
                return;
            }

            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "WassControl_Drivers_Backup");
            
            bool confirm = await _dialogService.ShowConfirmation($"Los drivers se exportarán a:\n\n{path}\n\n¿Desea continuar?", "Confirmar Exportación");
            if (!confirm) return;
            
            _driverExportCancellationTokenSource = new CancellationTokenSource();
            var token = _driverExportCancellationTokenSource.Token;

            try
            {
                IsExportingDrivers = true;
                DriverExportProgress = 0;
                DriverExportStatusMessage = "Iniciando exportación...";

                var progress = new Progress<(int percentage, string message)>(report =>
                {
                    DriverExportProgress = report.percentage;
                    DriverExportStatusMessage = report.message;
                });

                var result = await _driverService.ExportDriversAsync(path, progress, token); // Pass the token
                
                if (result.Success)
                {
                    await _dialogService.ShowMessage((result.Message ?? "Drivers exportados.") + $"\n\nUbicación: {path}", "Copia de Seguridad Completada");
                }
                else
                {
                    if (token.IsCancellationRequested) // Check if it was cancelled
                    {
                        await _dialogService.ShowMessage("Exportación de drivers cancelada.", "Cancelado");
                    }
                    else
                    {
                        await _dialogService.ShowMessage((result.Message ?? "Error al exportar drivers."), "Error");
                    }
                }
            }
            catch (OperationCanceledException) // Handle explicit cancellation
            {
                _log?.Info("Exportación de drivers cancelada por el usuario en ViewModel.");
                await _dialogService.ShowMessage("Exportación de drivers cancelada.", "Cancelado");
            }
            catch (Exception ex)
            {
                _log?.Error("Error durante la exportación de drivers", ex);
                await _dialogService.ShowMessage($"Se produjo un error inesperado: {ex.Message}", "Error Crítico");
            }
            finally
            {
                IsExportingDrivers = false;
                DriverExportProgress = 0;
                DriverExportStatusMessage = "";
                _driverExportCancellationTokenSource?.Dispose(); // Dispose CancellationTokenSource
                _driverExportCancellationTokenSource = null;
            }
        }

        private void ExecuteCancelDriverExport()
        {
            _driverExportCancellationTokenSource?.Cancel();
            _log?.Info("Solicitud de cancelación de exportación de drivers.");
        }

        private void ExecuteFreeUpDiskSpace()
        {
            if (SelectedDriveForCleanup == null) return;

            string drive = SelectedDriveForCleanup.DriveLetter.TrimEnd('\\');
            _log?.Info($"Lanzando cleanmgr.exe para la unidad: {drive}");

            try
            {
                var psi = new ProcessStartInfo("cleanmgr.exe", $"/d {drive}")
                {
                    UseShellExecute = true,
                    Verb = "runas"
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                _log?.Error($"Error al lanzar cleanmgr.exe para {drive}", ex);
                _ = _dialogService.ShowMessage($"No se pudo iniciar la utilidad de limpieza de disco para {drive}.", "Error");
            }
        }

        private async Task ExecuteAnalyzeDiskSpaceAsync(string path)
        {
            if (IsBusy) return;
            if (string.IsNullOrEmpty(path)) return;

            // Find the UnifiedDiskViewModel for the given path
            var unifiedDisk = UnifiedDisks.FirstOrDefault(d => d.DriveLetter.Equals(path, StringComparison.OrdinalIgnoreCase));
            if (unifiedDisk == null) return;

            try
            {
                IsBusy = true;
                StatusMessage = $"Analizando {path}...";
                
                // Clear previous results
                unifiedDisk.DiskAnalysisResult.Clear();
                unifiedDisk.LargeFilesResult.Clear();

                var analysisResult = await _diskAnalyzerService.AnalyzeDirectoryAsync(path);
                foreach(var item in analysisResult)
                {
                    unifiedDisk.DiskAnalysisResult.Add(item);
                }

                var largeFilesResult = await _diskAnalyzerService.FindLargeFilesAsync(path, 1024 * 1024 * 50); // > 50 MB
                foreach(var item in largeFilesResult)
                {
                    unifiedDisk.LargeFilesResult.Add(item);
                }

                StatusMessage = $"Análisis de {path} completado.";
            }
            catch (Exception ex)
            {
                _log?.Error($"Error al analizar el disco {path}", ex);
                await _dialogService.ShowMessage($"Error al analizar: {ex.Message}", "Error");
            }
            finally { IsBusy = false; StatusMessage = ""; }
        }

        private async Task ExecuteCleanDownloadsAsync(string? period)
        {
            if (string.IsNullOrEmpty(period)) return;
            
            bool confirm = await _dialogService.ShowConfirmation($"¿Está seguro de limpiar Descargas ({period})? Esta acción no se puede deshacer.", "Confirmar Limpieza");
            if (!confirm) return;

            try
            {
                IsBusy = true;
                string downloadsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
                if (!Directory.Exists(downloadsPath)) return;

                var files = Directory.GetFiles(downloadsPath);
                DateTime threshold = DateTime.MaxValue;

                switch (period)
                {
                    case "1Day": threshold = DateTime.Now.AddDays(-1); break;
                    case "1Week": threshold = DateTime.Now.AddDays(-7); break;
                    case "1Month": threshold = DateTime.Now.AddMonths(-1); break;
                    case "All": threshold = DateTime.MaxValue; break;
                }

                int deletedCount = 0;
                long freedBytes = 0;

                await Task.Run(() =>
                {
                    foreach (var file in files)
                    {
                        var fi = new FileInfo(file);
                        if (period == "All" || fi.LastWriteTime < threshold)
                        {
                            try 
                            { 
                                freedBytes += fi.Length;
                                fi.Delete(); 
                                deletedCount++; 
                            }
                            catch (Exception ex) { _log?.Warn($"No se pudo borrar {file}: {ex.Message}"); }
                        }
                    }
                });

                await _dialogService.ShowMessage($"Limpieza completada.\nArchivos eliminados: {deletedCount}\nEspacio liberado: {freedBytes / 1024 / 1024} MB", "Éxito");
            }
            catch (Exception ex)
            {
                await _dialogService.ShowMessage($"Error al limpiar descargas: {ex.Message}", "Error");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ExecuteDeleteSelectedLargeFilesAsync()
        {
            var selected = DeepScanResult.Where(f => f.IsSelected).ToList();
            if (!selected.Any())
            {
                await _dialogService.ShowMessage("No hay archivos seleccionados.", "Información");
                return;
            }

            bool confirm = await _dialogService.ShowConfirmation($"¿Eliminar {selected.Count} archivo(s) permanentemente?", "Confirmar Eliminación");
            if (!confirm) return;

            try
            {
                IsBusy = true;
                int deleted = 0;
                await Task.Run(() =>
                {
                    foreach (var item in selected)
                    {
                        try
                        {
                            if (File.Exists(item.Path))
                            {
                                File.Delete(item.Path);
                                deleted++;
                            }
                        }
                        catch (Exception ex) { _log?.Error($"No se pudo eliminar {item.Path}", ex); }
                    }
                });

                await _dialogService.ShowMessage($"Se eliminaron {deleted} archivos.", "Éxito");
                await ExecuteDeepScanAsync();
            }
            finally 
            {
                IsBusy = false;
            }
        }
        
        private async Task ExecuteCustomSpaceLiberatorAsync()
        {
             bool confirm = await _dialogService.ShowConfirmation("Esto eliminará archivos temporales, prefetch y carpetas de caché comunes. ¿Continuar?", "Liberador de Espacio Mejorado");
             if (!confirm) return;
             
             try
             {
                 IsBusy = true;
                 StatusMessage = "Liberando espacio...";
                 await _maintenance.CleanTemporaryFilesAsync(new CleaningOptions { CleanSystemTemp = true, CleanRecycleBin = true });
                 await ExecuteCleanPrefetchAsync();
                 await _dialogService.ShowMessage("Limpieza profunda finalizada.", "Éxito");
             }
             catch(Exception ex)
             {
                 await _dialogService.ShowMessage($"Error: {ex.Message}", "Error");
             }
             finally
             {
                 IsBusy = false;
                 StatusMessage = "";
             }
        }

        public ICommand CleanDownloadsCommand { get; set; }
        public ICommand DeleteSelectedLargeFilesCommand { get; set; }
        public ICommand CustomSpaceLiberatorCommand { get; set; }
    }
}
