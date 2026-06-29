using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using PracticaSockets_Client.Services;
using System.Threading.Tasks;

namespace PracticaSockets_Client.ViewModels
{
    public partial class Tab4ViewModel : ObservableObject
    {
        private readonly FileTransferService _service = new FileTransferService();

        private string _selectedFilePath = "";
        
        private string _selectedFileName = "Ningún archivo seleccionado";
        public string SelectedFileName
        {
            get => _selectedFileName;
            set => SetProperty(ref _selectedFileName, value);
        }

        private double _progress;
        public double Progress
        {
            get => _progress;
            set
            {
                SetProperty(ref _progress, value);
                OnPropertyChanged(nameof(ProgressText));
            }
        }

        public string ProgressText => $"{Progress:0.0}%";

        private string _log = "";
        public string Log
        {
            get => _log;
            set => SetProperty(ref _log, value);
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        public IRelayCommand BrowseFileCommand { get; }
        public IAsyncRelayCommand SendFileCommand { get; }

        public Tab4ViewModel()
        {
            BrowseFileCommand = new RelayCommand(BrowseFile);
            SendFileCommand = new AsyncRelayCommand(SendFileAsync);
        }

        private void BrowseFile()
        {
            var ofd = new OpenFileDialog();
            if (ofd.ShowDialog() == true)
            {
                _selectedFilePath = ofd.FileName;
                SelectedFileName = ofd.SafeFileName;
                Progress = 0;
            }
        }

        private async Task SendFileAsync()
        {
            if (string.IsNullOrEmpty(_selectedFilePath))
            {
                Log += "\nSelecciona un archivo primero.";
                return;
            }

            IsBusy = true;
            Log += $"\nEnviando al puerto 8883...";
            
            await _service.SendFileAsync("127.0.0.1", 8883, _selectedFilePath, 
                onProgress: (p) => { Progress = p; },
                onLog: (m) => { Log += $"\n{m}"; });
            
            IsBusy = false;
        }
    }
}
