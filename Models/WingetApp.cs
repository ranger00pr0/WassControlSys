using System.ComponentModel;
using System.Runtime.CompilerServices;

#nullable enable

namespace WassControlSys.Models
{
    public class WingetApp : INotifyPropertyChanged
    {
        public string Name { get; set; } = string.Empty;
        public string Id { get; set; } = string.Empty;
        public string CurrentVersion { get; set; } = string.Empty;
        public string AvailableVersion { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;

        private bool _isUpdating;
        public bool IsUpdating
        {
            get => _isUpdating;
            set { if (_isUpdating != value) { _isUpdating = value; OnPropertyChanged(); } }
        }

        private int _updateProgress;
        public int UpdateProgress
        {
            get => _updateProgress;
            set { if (_updateProgress != value) { _updateProgress = value; OnPropertyChanged(); } }
        }

        private string _updateStatusMessage = "";
        public string UpdateStatusMessage
        {
            get => _updateStatusMessage;
            set { if (_updateStatusMessage != value) { _updateStatusMessage = value; OnPropertyChanged(); } }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
