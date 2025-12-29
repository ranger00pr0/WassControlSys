using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using WassControlSys.Models;

namespace WassControlSys.Core
{
    public interface ISettingsService
    {
        Task<AppSettings> LoadAsync();
        Task SaveAsync(AppSettings settings);
    }

    public class SettingsService : ISettingsService
    {
        private readonly string _dir;
        private readonly string _file;
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            AllowTrailingCommas = true
        };

        public SettingsService(string? appFolderName = null)
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _dir = Path.Combine(appData, appFolderName ?? "WassControlSys");
            _file = Path.Combine(_dir, "settings.json");
        }

        public async Task<AppSettings> LoadAsync()
        {
            try
            {
                if (!File.Exists(_file))
                {
                    return AppSettings.Default();
                }
                string json = await File.ReadAllTextAsync(_file);
                var s = JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions);
                return s ?? AppSettings.Default();
            }
            catch
            {
                return AppSettings.Default();
            }
        }

        public async Task SaveAsync(AppSettings settings)
        {
            try
            {
                Directory.CreateDirectory(_dir);
                string json = JsonSerializer.Serialize(settings, _jsonOptions);
                await File.WriteAllTextAsync(_file, json);
            }
            catch
            {
                // Ignorar para evitar fallos por errores de E/S
            }
        }
    }

    public interface ILogService
    {
        void Info(string message);
        void Warn(string message);
        void Error(string message, Exception? ex = null);
    }

    public class FileLogService : ILogService
    {
        private readonly string _logFile;
        private readonly object _lock = new object();

        public FileLogService(string? appFolderName = null)
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string dir = Path.Combine(appData, appFolderName ?? "WassControlSys", "logs");
            Directory.CreateDirectory(dir);
            string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            _logFile = Path.Combine(dir, $"session_{stamp}.log");
            Info("Log inicializado");
        }

        public void Info(string message) => Write("INFO", message);
        public void Warn(string message) => Write("WARN", message);
        public void Error(string message, Exception? ex = null) => Write("ERROR", ex == null ? message : message + " | " + ex);

        private void Write(string level, string message)
        {
            string line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}";
            try
            {
                lock (_lock)
                {
                    File.AppendAllText(_logFile, line + Environment.NewLine);
                }
            }
            catch
            {
                // ignorar fallos de registro
            }
        }
    }
}

namespace WassControlSys.Models
{
    public class AppSettings
    {
        public PerformanceMode SelectedMode { get; set; }
        public string CurrentSection { get; set; } = "Dashboard";
        public bool RunOnStartup { get; set; } = false;
        public string AccentColor { get; set; } = "#3B82F6"; // Azul por defecto
        public bool AutoOptimizeRam { get; set; } = false;
        public double RamThresholdPercent { get; set; } = 85;
        public string Language { get; set; } = "es";
        public bool IsDarkMode { get; set; } = true;
        public bool OptimizeOnIdle { get; set; } = false;
        public bool MinimizeToTray { get; set; } = true;

        public static AppSettings Default() => new AppSettings
        {
            SelectedMode = PerformanceMode.General,
            CurrentSection = "Dashboard",
            RunOnStartup = false,
            AccentColor = "#3B82F6",
            AutoOptimizeRam = false,
            RamThresholdPercent = 85,
            Language = "es",
            IsDarkMode = true,
            OptimizeOnIdle = false,
            MinimizeToTray = true
        };
    }
}
