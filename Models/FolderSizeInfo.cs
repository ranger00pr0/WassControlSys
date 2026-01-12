using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WassControlSys.Models
{
    public class FolderSizeInfo : INotifyPropertyChanged
    {
        public string Path { get; set; } = string.Empty;
        public long SizeBytes { get; set; }
        public string FormattedSize { get; set; } = "0 B";
        public double Percentage { get; set; }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set { if (_isSelected != value) { _isSelected = value; OnPropertyChanged(); } }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
