using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WassControlSys.Models
{
    public class BloatwareApp : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        public string Name
        {
            get => _name;
            set { if (_name != value) { _name = value; OnPropertyChanged(); } }
        }

        private string _publisher = string.Empty;
        public string Publisher
        {
            get => _publisher;
            set { if (_publisher != value) { _publisher = value; OnPropertyChanged(); } }
        }

        private string _installLocation = string.Empty;
        public string InstallLocation
        {
            get => _installLocation;
            set { if (_installLocation != value) { _installLocation = value; OnPropertyChanged(); } }
        }

        private string _uninstallCommand = string.Empty;
        public string UninstallCommand
        {
            get => _uninstallCommand;
            set { if (_uninstallCommand != value) { _uninstallCommand = value; OnPropertyChanged(); } }
        }

        private bool _isSystemApp;
        public bool IsSystemApp
        {
            get => _isSystemApp;
            set { if (_isSystemApp != value) { _isSystemApp = value; OnPropertyChanged(); } }
        }

        private bool _isUninstalling;
        public bool IsUninstalling
        {
            get => _isUninstalling;
            set
            {
                if (_isUninstalling != value)
                {
                    _isUninstalling = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
