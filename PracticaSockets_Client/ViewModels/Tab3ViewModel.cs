using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PracticaSockets_Client.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PracticaSockets_Client.ViewModels
{
    public partial class Tab3ViewModel : ObservableObject
    {
        private readonly TcpClientService _service = new TcpClientService();

        private int _numThreads = 10;
        public int NumThreads
        {
            get => _numThreads;
            set => SetProperty(ref _numThreads, value);
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

        public IAsyncRelayCommand StartTestCommand { get; }

        public Tab3ViewModel()
        {
            StartTestCommand = new AsyncRelayCommand(StartTestAsync);
        }

        private async Task StartTestAsync()
        {
            IsBusy = true;
            Log = $"Iniciando {NumThreads} conexiones concurrentes al puerto 8882...\n";

            var tasks = new List<Task<string>>();
            for (int i = 0; i < NumThreads; i++)
            {
                int taskId = i + 1;
                tasks.Add(Task.Run(async () =>
                {
                    // Simulamos un retraso aleatorio para que lleguen desordenados
                    await Task.Delay(new Random().Next(10, 500));
                    var res = await _service.SendMessageAsync("127.0.0.1", 8882, $"Mensaje desde Hilo #{taskId}");
                    return $"Hilo #{taskId}: {res.Replace("\n", " ")}";
                }));
            }

            var results = await Task.WhenAll(tasks);
            
            foreach(var r in results)
            {
                Log += r + "\n";
            }

            Log += "\n✅ Prueba concurrente finalizada.";
            IsBusy = false;
        }
    }
}
