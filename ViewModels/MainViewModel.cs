using System.Collections.ObjectModel; // Added for ObservableCollection
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input; // Añadido para ICommand
using WassControlSys.Core; // Añadido para RelayCommand
using WassControlSys.Models; // Añadido para Models
using System.Windows.Threading; // Añadido para DispatcherTimer
using System;
using System.Linq; // Added for FirstOrDefault
using System.IO; // Added for File and Directory operations

namespace WassControlSys.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly ISystemMaintenanceService _maintenance;
        private readonly IMonitoringService _monitoringService;
        private readonly DispatcherTimer _monitoringTimer;
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

        // Constructor del ViewModel principal
        public MainViewModel(ISystemMaintenanceService maintenance, IMonitoringService monitoringService, IPerformanceProfileService profiles, ISettingsService settings, ILogService log, ISystemInfoService systemInfoService, ISecurityService securityService, IDialogService dialogService, IStartupService startupService, IServiceOptimizerService serviceOptimizerService, IBloatwareService bloatwareService, IPrivacyService privacyService)
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

            WelcomeMessage = "¡Bienvenido a WassControlSys! (Fase 1: Núcleo de Mantenimiento)";
            
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
            ResetNetworkCommand = new RelayCommand(async _ => await ExecuteResetNetworkAsync());

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


            // Establecer modo por defecto antes de cargar ajustes
            CurrentMode = PerformanceMode.General;

            // Cargar ajustes
            _ = LoadSettingsAsync();

            // Inicializar Timer de Monitoreo
            _monitoringTimer = new DispatcherTimer();
            _monitoringTimer.Interval = TimeSpan.FromSeconds(2);
            _monitoringTimer.Tick += (s, e) => UpdateSystemUsage();
            _monitoringTimer.Start();

            // Cargar información del sistema
            _ = LoadSystemInfoAsync();
            _ = LoadSecurityStatusAsync();
            _ = LoadStartupItemsAsync(); // Load startup items on startup
            _ = LoadWindowsServicesAsync(); // Load Windows services on startup
            _ = LoadBloatwareAppsAsync(); // Load bloatware apps on startup
            _ = LoadPrivacySettingsAsync(); // Load privacy settings on startup
        }

        // Implementación de la interfaz INotifyPropertyChanged
        // Esto permite que la interfaz de usuario se actualice automáticamente cuando cambian las propiedades del ViewModel.
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Ejemplo de propiedad enlazable (bindable property)
        private string _welcomeMessage;
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

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set { if (_isBusy != value) { _isBusy = value; OnPropertyChanged(); } }
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

        private SecurityStatus _securityStatus;
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

        private SystemInfo _systemInformation;
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

        private ObservableCollection<StartupItem> _startupItems;
        public ObservableCollection<StartupItem> StartupItems
        {
            get => _startupItems;
            set { if (_startupItems != value) { _startupItems = value; OnPropertyChanged(); } }
        }

        private ObservableCollection<WindowsService> _windowsServices;
        public ObservableCollection<WindowsService> WindowsServices
        {
            get => _windowsServices;
            set { if (_windowsServices != value) { _windowsServices = value; OnPropertyChanged(); } }
        }

        private ObservableCollection<BloatwareApp> _bloatwareApps;
        public ObservableCollection<BloatwareApp> BloatwareApps
        {
            get => _bloatwareApps;
            set { if (_bloatwareApps != value) { _bloatwareApps = value; OnPropertyChanged(); } }
        }

        private ObservableCollection<PrivacySetting> _privacySettings;
        public ObservableCollection<PrivacySetting> PrivacySettings
        {
            get => _privacySettings;
            set { if (_privacySettings != value) { _privacySettings = value; OnPropertyChanged(); } }
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
                    _ = ToggleRunOnStartupAsync(value);
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
        public ICommand UninstallBloatwareAppCommand { get; private set; }
        public ICommand RefreshBloatwareAppsCommand { get; private set; }
        public ICommand UpdatePrivacySettingCommand { get; private set; }
        public ICommand RefreshPrivacySettingsCommand { get; private set; }


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
                // Add other options as they are implemented
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
            var r = _maintenance.LaunchSystemFileChecker();
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
            var r = _maintenance.LaunchDISMHealthRestore();
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
        private AppSection _currentSection;
        public AppSection CurrentSection
        {
            get => _currentSection;
            set
            {
                if (_currentSection != value)
                {
                    _currentSection = value;
                    OnPropertyChanged();
                    _ = SaveSettingsAsync();
                    _log?.Info($"Sección cambiada a: {value}");
                }
            }
        }

        public ICommand NavigateCommand { get; private set; }

        private void ExecuteNavigate(object parameter)
        {
            if (parameter is AppSection section)
            {
                CurrentSection = section;
            }
            else if (parameter is string s && Enum.TryParse(s, true, out AppSection parsed))
            {
                CurrentSection = parsed;
            }
        }
        
        // ... existing methods like ExecuteRunChkdsk ...
        private async Task ExecuteRunChkdsk()
        {
            _log?.Info("Lanzando CHKDSK");
            var r = _maintenance.LaunchCHKDSK();
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

        private void UpdateSystemUsage()
        {
            var usage = _monitoringService.GetSystemUsage();
            CpuUsage = usage.CpuUsage;
            RamUsage = usage.RamUsage;
            DiskUsage = usage.DiskUsage;
        }

        private async Task LoadSettingsAsync()
        {
            try
            {
                var s = await _settings.LoadAsync();
                CurrentMode = s.SelectedMode;
                RunOnStartup = s.RunOnStartup; // This triggers ToggleRunOnStartupAsync, careful not to loop or be redundant.
                AccentColor = s.AccentColor;
                if (Enum.TryParse<AppSection>(s.CurrentSection, true, out var section))
                {
                    CurrentSection = section;
                }
                _log?.Info($"Settings cargados. Modo={s.SelectedMode}, Sección={s.CurrentSection}, Start={s.RunOnStartup}, Color={s.AccentColor}");
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
                    AccentColor = AccentColor
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
                        string path = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
                        if (path != null) key?.SetValue("WassControlSys", path);
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
                _log?.Info($"Aplicando perfil: {mode}");
                var r = await _profiles.ApplyProfileAsync(mode);
                _log?.Info($"Perfil resultado: {r.Success} - {r.Message}");
                await _dialogService.ShowMessage(r.Message ?? (r.Success ? "Perfil aplicado." : "No se pudo aplicar el perfil."), "Selector de Modo");
            }
            catch (System.Exception ex)
            {
                _log?.Error("Error al aplicar el perfil", ex);
                await _dialogService.ShowMessage($"Error al aplicar el perfil: {ex.Message}", "Error");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoadSystemInfoAsync()
        {
            try
            {
                SystemInformation = await _systemInfoService.GetSystemInfoAsync();
            }
            catch (Exception ex)
            {
                _log?.Error("Error cargando info del sistema", ex);
                SystemInformation = new SystemInfo { MachineName = "Error al cargar" };
            }
        }

        private async Task LoadSecurityStatusAsync()
        {
            try
            {
                SecurityStatus = await _securityService.GetSecurityStatusAsync();
            }
            catch (Exception ex)
            {
                _log?.Error("Error cargando estado de seguridad", ex);
                SecurityStatus = new SecurityStatus { AntivirusName = "Error de carga" };
            }
        }

        private async Task LoadStartupItemsAsync()
        {
            try
            {
                IsBusy = true;
                _log?.Info("Cargando elementos de inicio...");
                var items = await _startupService.GetStartupItemsAsync();
                StartupItems = new ObservableCollection<StartupItem>(items);
                _log?.Info($"Elementos de inicio cargados: {StartupItems.Count}");
            }
            catch (Exception ex)
            {
                _log?.Error("Error cargando elementos de inicio", ex);
                await _dialogService.ShowMessage($"Error cargando elementos de inicio: {ex.Message}", "Error");
            }
            finally
            {
                IsBusy = false;
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
            try
            {
                IsBusy = true;
                _log?.Info("Cargando servicios de Windows...");
                var services = await _serviceOptimizerService.GetWindowsServicesAsync();
                WindowsServices = new ObservableCollection<WindowsService>(services);
                _log?.Info($"Servicios de Windows cargados: {WindowsServices.Count}");
            }
            catch (Exception ex)
            {
                _log?.Error("Error cargando servicios de Windows", ex);
                await _dialogService.ShowMessage($"Error cargando servicios de Windows: {ex.Message}", "Error");
            }
            finally
            {
                IsBusy = false;
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

        private async Task LoadBloatwareAppsAsync()
        {
            try
            {
                IsBusy = true;
                _log?.Info("Cargando aplicaciones de bloatware...");
                var apps = await _bloatwareService.GetBloatwareAppsAsync();
                BloatwareApps = new ObservableCollection<BloatwareApp>(apps);
                _log?.Info($"Aplicaciones de bloatware cargadas: {BloatwareApps.Count}");
            }
            catch (Exception ex)
            {
                _log?.Error("Error cargando aplicaciones de bloatware", ex);
                await _dialogService.ShowMessage($"Error cargando aplicaciones de bloatware: {ex.Message}", "Error");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ExecuteUninstallBloatwareAppAsync(BloatwareApp app)
        {
            if (app == null || IsBusy) return;

            bool confirm = await _dialogService.ShowConfirmation($"¿Está seguro de que desea desinstalar '{app.Name}'? Esta acción no se puede deshacer.", "Confirmar Desinstalación");
            if (!confirm) return;

            try
            {
                IsBusy = true;
                _log?.Info($"Desinstalando aplicación de bloatware: {app.Name}");
                bool success = await _bloatwareService.UninstallBloatwareAppAsync(app);
                if (success)
                {
                    BloatwareApps.Remove(app); // Remove from UI list
                    await _dialogService.ShowMessage($"'{app.Name}' desinstalado correctamente.", "Éxito");
                }
                else
                {
                    await _dialogService.ShowMessage($"No se pudo desinstalar '{app.Name}'.", "Error");
                }
            }
            catch (Exception ex)
            {
                _log?.Error($"Error al desinstalar la aplicación de bloatware '{app.Name}'", ex);
                await _dialogService.ShowMessage($"Error al desinstalar '{app.Name}': {ex.Message}", "Error");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoadPrivacySettingsAsync()
        {
            try
            {
                IsBusy = true;
                _log?.Info("Cargando configuraciones de privacidad...");
                var settings = await _privacyService.GetPrivacySettingsAsync();
                PrivacySettings = new ObservableCollection<PrivacySetting>(settings);
                _log?.Info($"Configuraciones de privacidad cargadas: {PrivacySettings.Count}");
            }
            catch (Exception ex)
            {
                _log?.Error("Error cargando configuraciones de privacidad", ex);
                await _dialogService.ShowMessage($"Error cargando configuraciones de privacidad: {ex.Message}", "Error");
            }
            finally
            {
                IsBusy = false;
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
        
        private async Task ExecuteFlushDnsAsync()
        {
            if (IsBusy) return;
            try
            {
                IsBusy = true;
                _log?.Info("Limpiando caché DNS...");
                
                await Task.Run(() =>
                {
                    var psi = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "ipconfig",
                        Arguments = "/flushdns",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    };
                    using var process = System.Diagnostics.Process.Start(psi);
                    process?.WaitForExit();
                });
                
                await _dialogService.ShowMessage("Caché DNS limpiada correctamente.", "Éxito");
            }
            catch (Exception ex)
            {
                _log?.Error("Error limpiando DNS", ex);
                await _dialogService.ShowMessage($"Error limpiando DNS: {ex.Message}", "Error");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ExecuteAnalyzeDiskAsync()
        {
            if (IsBusy) return;
            
            bool confirm = await _dialogService.ShowConfirmation(
                "El análisis de disco puede tardar varios minutos. ¿Desea continuar?", 
                "Confirmar Análisis");
            if (!confirm) return;

            try
            {
                IsBusy = true;
                _log?.Info("Analizando disco C:...");
                
                await Task.Run(() =>
                {
                    var psi = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "defrag",
                        Arguments = "C: /A",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    };
                    using var process = System.Diagnostics.Process.Start(psi);
                    process?.WaitForExit();
                });
                
                await _dialogService.ShowMessage("Análisis de disco completado. Revise los logs del sistema para más detalles.", "Análisis Completado");
            }
            catch (Exception ex)
            {
                _log?.Error("Error analizando disco", ex);
                await _dialogService.ShowMessage($"Error analizando disco: {ex.Message}", "Error");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ExecuteRebuildSearchIndexAsync()
        {
            if (IsBusy) return;
            
            bool confirm = await _dialogService.ShowConfirmation(
                "Reconstruir el índice de búsqueda puede tardar varias horas y afectará el rendimiento del sistema. ¿Continuar?", 
                "Confirmar Reconstrucción");
            if (!confirm) return;

            try
            {
                IsBusy = true;
                _log?.Info("Reconstruyendo índice de búsqueda...");
                
                await Task.Run(() =>
                {
                    // Stop Windows Search service
                    var stopPsi = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "net",
                        Arguments = "stop WSearch",
                        UseShellExecute = true,
                        Verb = "runas",
                        CreateNoWindow = true
                    };
                    using (var stopProcess = System.Diagnostics.Process.Start(stopPsi))
                    {
                        stopProcess?.WaitForExit();
                    }

                    System.Threading.Thread.Sleep(2000);

                    // Start Windows Search service
                    var startPsi = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "net",
                        Arguments = "start WSearch",
                        UseShellExecute = true,
                        Verb = "runas",
                        CreateNoWindow = true
                    };
                    using (var startProcess = System.Diagnostics.Process.Start(startPsi))
                    {
                        startProcess?.WaitForExit();
                    }
                });
                
                await _dialogService.ShowMessage("Servicio de búsqueda reiniciado. El índice se reconstruirá automáticamente.", "Éxito");
            }
            catch (Exception ex)
            {
                _log?.Error("Error reconstruyendo índice", ex);
                await _dialogService.ShowMessage($"Error reconstruyendo índice: {ex.Message}\nPuede que necesite ejecutar la aplicación como administrador.", "Error");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ExecuteCleanPrefetchAsync()
        {
            if (IsBusy) return;
            
            bool confirm = await _dialogService.ShowConfirmation(
                "¿Desea limpiar los archivos prefetch? Esto es seguro pero puede hacer que las aplicaciones tarden un poco más en iniciar la primera vez.", 
                "Confirmar Limpieza");
            if (!confirm) return;

            try
            {
                IsBusy = true;
                _log?.Info("Limpiando prefetch...");
                
                int filesDeleted = await Task.Run(() =>
                {
                    string prefetchPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Prefetch");
                    int count = 0;
                    
                    if (Directory.Exists(prefetchPath))
                    {
                        foreach (var file in Directory.GetFiles(prefetchPath, "*.pf"))
                        {
                            try
                            {
                                File.Delete(file);
                                count++;
                            }
                            catch { }
                        }
                    }
                    return count;
                });
                
                await _dialogService.ShowMessage($"Limpieza completada. {filesDeleted} archivos eliminados.", "Éxito");
            }
            catch (Exception ex)
            {
                _log?.Error("Error limpiando prefetch", ex);
                await _dialogService.ShowMessage($"Error limpiando prefetch: {ex.Message}\nPuede que necesite ejecutar la aplicación como administrador.", "Error");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ExecuteResetNetworkAsync()
        {
            if (IsBusy) return;
            
            bool confirm = await _dialogService.ShowConfirmation(
                "Esto reiniciará la configuración de red y puede desconectarlo temporalmente. ¿Continuar?", 
                "Confirmar Reinicio de Red");
            if (!confirm) return;

            try
            {
                IsBusy = false;
                _log?.Info("Reiniciando configuración de red...");
                
                await Task.Run(() =>
                {
                    // Reset Winsock
                    var winsockPsi = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "netsh",
                        Arguments = "winsock reset",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    };
                    using (var process = System.Diagnostics.Process.Start(winsockPsi))
                    {
                        process?.WaitForExit();
                    }

                    // Reset IP
                    var ipPsi = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "netsh",
                        Arguments = "int ip reset",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    };
                    using (var process = System.Diagnostics.Process.Start(ipPsi))
                    {
                        process?.WaitForExit();
                    }
                });
                
                await _dialogService.ShowMessage("Red reiniciada. Se recomienda reiniciar el sistema para aplicar todos los cambios.", "Éxito");
            }
            catch (Exception ex)
            {
                _log?.Error("Error reiniciando red", ex);
                await _dialogService.ShowMessage($"Error reiniciando red: {ex.Message}", "Error");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
