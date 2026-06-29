using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PracticaSockets_Client.Services;
using PracticaSockets_Core.Helpers;
using System.Threading.Tasks;

namespace PracticaSockets_Client.ViewModels
{
    public partial class Tab1ViewModel : ObservableObject
    {
        private readonly TcpClientService _service = new TcpClientService();

        private string _ip = "127.0.0.1";
        public string Ip
        {
            get => _ip;
            set => SetProperty(ref _ip, value);
        }

        private string _puerto = "8880";
        public string Puerto
        {
            get => _puerto;
            set => SetProperty(ref _puerto, value);
        }

        private string _mensaje = "Hola Servidor!";
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

        public IAsyncRelayCommand ConectarCommand { get; }

        public Tab1ViewModel()
        {
            ConectarCommand = new AsyncRelayCommand(ConectarAsync);
        }

        private async Task ConectarAsync()
        {
            if (!int.TryParse(Puerto, out int port))
            {
                Log = "Puerto inválido.";
                return;
            }

            IsBusy = true;
            Log = $"Conectando a {Ip}:{port}...";
            
            var result = await _service.SendMessageAsync(Ip, port, Mensaje);
            
            Log = $"[{System.DateTime.Now:HH:mm:ss}] {result}";
            IsBusy = false;
        }
    }
}
