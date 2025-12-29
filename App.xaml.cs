using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Microsoft.Extensions.DependencyInjection;
using WassControlSys.Core;
using WassControlSys.ViewModels;
using System.Drawing;
using System.Windows.Forms;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;

namespace WassControlSys
{
    public partial class App : Application
    {
        private readonly ServiceProvider _serviceProvider;
        private NotifyIcon? _notifyIcon;
        private System.Threading.Mutex? _instanceMutex;
        public bool IsShuttingDown { get; private set; }

        public App()
        {
            // Verificar si ya existe una instancia
            bool createdNew;
            _instanceMutex = new System.Threading.Mutex(true, "WassControlSys_SingleInstance_Mutex", out createdNew);
            
            if (!createdNew)
            {
                // Ya existe una instancia, mostrar mensaje y cerrar
                MessageBox.Show("WassControlSys ya está en ejecución.", "Instancia Activa", MessageBoxButton.OK, MessageBoxImage.Information);
                Current.Shutdown();
                return;
            }

            DispatcherUnhandledException += App_DispatcherUnhandledException;
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
            _serviceProvider = serviceCollection.BuildServiceProvider();
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            var log = _serviceProvider?.GetService<ILogService>();
            log?.Error("Excepción NO controlada en UI Dispatcher", e.Exception);
            // Opcional: Mostrar mensaje al usuario
            // MessageBox.Show($"Error crítico: {e.Exception.Message}", "Error Inesperado", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true; // Evitar cierre si es posible, aunque riesgoso
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Registrar servicios
            services.AddSingleton<IMonitoringService, MonitoringService>();
            services.AddSingleton<IPerformanceProfileService, PerformanceProfileService>();
            services.AddSingleton<IProcessManagerService, ProcessManagerService>();
            services.AddSingleton<ITemperatureMonitorService, TemperatureMonitorService>();
            services.AddSingleton<IDiskHealthService, DiskHealthService>();
            services.AddSingleton<ISecurityService, SecurityService>();
            services.AddSingleton<ISettingsService, SettingsService>();
            services.AddSingleton<ILogService, FileLogService>();
            services.AddSingleton<ISystemInfoService, SystemInfoService>();
            services.AddSingleton<ISystemMaintenanceService, SystemMaintenanceService>();
            services.AddSingleton<IDialogService, DialogService>();
            services.AddSingleton<IStartupService, StartupService>();
            services.AddSingleton<IServiceOptimizerService, ServiceOptimizerService>();
            services.AddSingleton<IBloatwareService, BloatwareService>();
            services.AddSingleton<IPrivacyService, PrivacyService>();
            services.AddSingleton<ILocalizationService, LocalizationService>();
            services.AddSingleton<IRestorePointService, RestorePointService>();
            services.AddSingleton<IBatteryService, BatteryService>();
            services.AddSingleton<IWingetService, WingetService>();
            services.AddSingleton<IDriverService, DriverService>();
            services.AddSingleton<IDiskAnalyzerService, DiskAnalyzerService>();

            // Registrar ViewModel
            services.AddSingleton<MainViewModel>();

            // Registrar MainWindow
            services.AddSingleton<MainWindow>();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var settings = _serviceProvider.GetService<ISettingsService>();
            var loc = _serviceProvider.GetService<ILocalizationService>();
            var log = _serviceProvider.GetService<ILogService>();
            
            try
            {
                if (settings != null)
                {
                    var s = await settings.LoadAsync();
                    if (s != null && loc != null)
                    {
                        await loc.SetLanguageAsync(s.Language);
                        ChangeAccentColor(s.AccentColor);
                        ChangeTheme(s.IsDarkMode);
                    }
                }
                ValidateContrast(log);
            }
            catch { }

            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.DataContext = _serviceProvider.GetRequiredService<MainViewModel>();
            
            SetupTrayIcon();
            
            // Asegurar estado normal antes de mostrar
            mainWindow.WindowState = WindowState.Normal;
            mainWindow.Show();
            mainWindow.WindowState = WindowState.Normal; // Forzar de nuevo después de Show
            mainWindow.Activate();
            mainWindow.Focus();
        }

        private void SetupTrayIcon()
        {
            try
            {
                _notifyIcon = new NotifyIcon();
                
                // Intentar extraer el icono del ejecutable
                try
                {
                    string assemblyLocation = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "";
                    if (string.IsNullOrEmpty(assemblyLocation) || !System.IO.File.Exists(assemblyLocation))
                    {
                        // Fallback a icono de aplicación del sistema
                        _notifyIcon.Icon = SystemIcons.Application;
                    }
                    else
                    {
                        _notifyIcon.Icon = Icon.ExtractAssociatedIcon(assemblyLocation);
                    }
                }
                catch
                {
                    _notifyIcon.Icon = SystemIcons.Application;
                }

                _notifyIcon.Text = "WassControlSys";
                _notifyIcon.Visible = true;
                
                _notifyIcon.DoubleClick += (s, e) => ShowMainWindow();

                var contextMenu = new ContextMenuStrip();
                contextMenu.Items.Add("🚀 Optimizar PC", null, (s, e) => (MainWindow?.DataContext as MainViewModel)?.PcBoostCommand.Execute(null));
                contextMenu.Items.Add("🧹 Limpiar RAM", null, (s, e) => (MainWindow?.DataContext as MainViewModel)?.OptimizeRamCommand.Execute(null));
                contextMenu.Items.Add("-");
                
                var navMenu = new ToolStripMenuItem("Navegar a...");
                navMenu.DropDownItems.Add("Inicio", null, (s, e) => NavigateToSection("Dashboard"));
                navMenu.DropDownItems.Add("Protección", null, (s, e) => NavigateToSection("Proteccion"));
                navMenu.DropDownItems.Add("Rendimiento", null, (s, e) => NavigateToSection("Rendimiento"));
                navMenu.DropDownItems.Add("Hardware", null, (s, e) => NavigateToSection("Hardware"));
                contextMenu.Items.Add(navMenu);

                contextMenu.Items.Add("-");
                contextMenu.Items.Add("Restaurar App", null, (s, e) => ShowMainWindow());
                contextMenu.Items.Add("Salir", null, (s, e) => ShutdownApp());
                
                _notifyIcon.ContextMenuStrip = contextMenu;
            }
            catch (Exception ex)
            {
                var log = _serviceProvider?.GetService<ILogService>();
                log?.Error("Error al configurar el icono de bandeja", ex);
            }
        }

        private void NavigateToSection(string section)
        {
            ShowMainWindow();
            (MainWindow?.DataContext as MainViewModel)?.NavigateCommand.Execute(section);
        }

        public void ShowMainWindow()
        {
            if (MainWindow != null)
            {
                if (MainWindow.WindowState == WindowState.Minimized)
                    MainWindow.WindowState = WindowState.Normal;
                
                MainWindow.Show();
                MainWindow.Activate();
            }
        }

        private void ShutdownApp()
        {
            IsShuttingDown = true;
            (MainWindow?.DataContext as MainViewModel)?.StopAllTasks();
            _notifyIcon?.Dispose();
            Shutdown();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _notifyIcon?.Dispose();
            _instanceMutex?.ReleaseMutex();
            _instanceMutex?.Dispose();
            base.OnExit(e);
        }

        private void ValidateContrast(ILogService? log)
        {
            try
            {
                var res = Current.Resources;
                var surface = (Color)res["SurfaceColor"];
                var text = (Color)res["TextColor"];
                var secondary = (Color)res["SecondaryTextColor"];
                double r1 = ContrastRatio(surface, text);
                double r2 = ContrastRatio(surface, secondary);
                if (r1 < 4.5 || r2 < 4.5)
                {
                    log?.Warn($"Contraste inferior a WCAG AA. Surface/Text={r1:F2}, Surface/Secondary={r2:F2}");
                }
                else
                {
                    log?.Info($"Contraste verificado. Surface/Text={r1:F2}, Surface/Secondary={r2:F2}");
                }
            }
            catch { }
        }

        private static double ContrastRatio(Color bg, Color fg)
        {
            static double L(Color c)
            {
                double srgb(double ch) => ch <= 0.03928 ? ch / 12.92 : Math.Pow((ch + 0.055) / 1.055, 2.4);
                double r = srgb(c.R / 255.0);
                double g = srgb(c.G / 255.0);
                double b = srgb(c.B / 255.0);
                return 0.2126 * r + 0.7152 * g + 0.0722 * b;
            }
            double l1 = L(fg);
            double l2 = L(bg);
            double hi = Math.Max(l1, l2);
            double lo = Math.Min(l1, l2);
            return (hi + 0.05) / (lo + 0.05);
        }

        public void ChangeAccentColor(string hexColor)
        {
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(hexColor);
                var solidBrush = new SolidColorBrush(color);
                
                // Cálculo simple para el efecto hover
                byte r = (byte)Math.Min(255, color.R + 30);
                byte g = (byte)Math.Min(255, color.G + 30);
                byte b = (byte)Math.Min(255, color.B + 30);
                var hoverColor = Color.FromRgb(r, g, b);
                var hoverBrush = new SolidColorBrush(hoverColor);

                Resources["PrimaryColor"] = color;
                Resources["PrimaryBrush"] = solidBrush;
                Resources["PrimaryHoverColor"] = hoverColor;
                Resources["PrimaryHoverBrush"] = hoverBrush;
                
                // Selección semitransparente basada en el color de acento
                var selectionColor = Color.FromArgb(40, color.R, color.G, color.B); // alpha ~16%
                Resources["SelectionColor"] = selectionColor;
                Resources["SelectionBrush"] = new SolidColorBrush(selectionColor);
            }
            catch { }
        }

        public void ChangeTheme(bool isDark)
        {
            try
            {
                // Preserve non-theme dictionaries (like language files)
                var nonThemeDictionaries = Resources.MergedDictionaries
                    .Where(d => d.Source == null || !d.Source.OriginalString.Contains("Theme."))
                    .ToList();

                Resources.MergedDictionaries.Clear();

                // Re-add the non-theme dictionaries
                foreach (var dict in nonThemeDictionaries)
                {
                    Resources.MergedDictionaries.Add(dict);
                }

                // Add the new theme dictionary
                var themeName = isDark ? "Theme.Dark.xaml" : "Theme.Light.xaml";
                var uri = new Uri($"Resources/{themeName}", UriKind.Relative);
                Resources.MergedDictionaries.Add(new ResourceDictionary { Source = uri });

                // Ajustar colores dependientes del tema (hover de superficie)
                if (isDark)
                {
                    Resources["SurfaceHoverColor"] = (Color)ColorConverter.ConvertFromString("#2A2A2A");
                }
                else
                {
                    Resources["SurfaceHoverColor"] = (Color)ColorConverter.ConvertFromString("#E5E7EB"); // tailwind gray-200 aprox
                }
                Resources["SurfaceHoverBrush"] = new SolidColorBrush((Color)Resources["SurfaceHoverColor"]);
            }
            catch { }
        }
    }
}
