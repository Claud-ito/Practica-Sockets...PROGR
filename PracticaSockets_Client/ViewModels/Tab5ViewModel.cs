using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PracticaSockets_Client.Services;
using System.Threading.Tasks;

namespace PracticaSockets_Client.ViewModels
{
    public partial class Tab5ViewModel : ObservableObject
    {
        private readonly SslClientService _service = new SslClientService();

        private string _mensaje = "Secreto protegido con TLS";
        public string Mensaje
        {
            get => _mensaje;
            set => SetProperty(ref _mensaje, value);
        }

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

        public IAsyncRelayCommand ConectarSslCommand { get; }

        public Tab5ViewModel()
        {
            ConectarSslCommand = new AsyncRelayCommand(ConectarSslAsync);
        }

        private async Task ConectarSslAsync()
        {
            IsBusy = true;
            Log = $"Iniciando Handshake TLS en el puerto 8884...\n";
            
            var result = await _service.SendSecureMessageAsync("127.0.0.1", 8884, Mensaje);
            
            Log += $"\n{result}";
            IsBusy = false;
        }
    }
}
