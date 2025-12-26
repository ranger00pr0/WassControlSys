using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using WassControlSys.Models;

#nullable enable

namespace WassControlSys.ViewModels
{
    public class DiskAnalyzerViewModel : INotifyPropertyChanged
    {
        private string _driveLetter = string.Empty;
        public string DriveLetter
        {
            get => _driveLetter;
            set { _driveLetter = value; OnPropertyChanged(); }
        }

        private ObservableCollection<FolderSizeInfo> _analysisResult = new();
        public ObservableCollection<FolderSizeInfo> AnalysisResult
        {
            get => _analysisResult;
            set { _analysisResult = value; OnPropertyChanged(); }
        }

        private ObservableCollection<FolderSizeInfo> _largeFilesResult = new();
        public ObservableCollection<FolderSizeInfo> LargeFilesResult
        {
            get => _largeFilesResult;
            set { _largeFilesResult = value; OnPropertyChanged(); }
        }


        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
