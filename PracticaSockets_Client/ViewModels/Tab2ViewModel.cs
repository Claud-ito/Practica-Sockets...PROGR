using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PracticaSockets_Client.Services;
using System.Threading.Tasks;

namespace PracticaSockets_Client.ViewModels
{
    public partial class Tab2ViewModel : ObservableObject
    {
        private readonly TcpClientService _service = new TcpClientService();

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

        public IAsyncRelayCommand TestAsyncCommand { get; }

        public Tab2ViewModel()
        {
            TestAsyncCommand = new AsyncRelayCommand(TestAsync);
        }

        private async Task TestAsync()
        {
            IsBusy = true;
            Log += "\nIniciando conexión asíncrona...";
            
            // Simular una carga lenta para demostrar que no bloquea la UI
            await Task.Delay(2000); 

            var result = await _service.SendMessageAsync("127.0.0.1", 8880, "Prueba Async");
            
            Log += $"\n[Completado] {result}";
            IsBusy = false;
        }
    }
}
