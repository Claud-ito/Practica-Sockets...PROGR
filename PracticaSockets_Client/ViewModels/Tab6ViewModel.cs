using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PracticaSockets_Client.Services;
using System;
using System.Threading.Tasks;

namespace PracticaSockets_Client.ViewModels
{
    public partial class Tab6ViewModel : ObservableObject
    {
        private readonly ChatClientService _service;

        private string _username = "Usuario" + new Random().Next(100, 999);
        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        private string _mensaje = "";
        public string Mensaje
        {
            get => _mensaje;
            set => SetProperty(ref _mensaje, value);
        }

        private string _chatLog = "";
        public string ChatLog
        {
            get => _chatLog;
            set => SetProperty(ref _chatLog, value);
        }

        private bool _isConnected;
        public bool IsConnected
        {
            get => _isConnected;
            set => SetProperty(ref _isConnected, value);
        }

        public IAsyncRelayCommand ConnectCommand { get; }
        public IAsyncRelayCommand SendMessageCommand { get; }

        public Tab6ViewModel()
        {
            _service = new ChatClientService();
            _service.OnMessageReceived += (msg) => 
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    ChatLog += msg + "\n";
                });
            };
            _service.OnDisconnected += (msg) => 
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    ChatLog += $"[Sistema] {msg}\n";
                    IsConnected = false;
                });
            };

            ConnectCommand = new AsyncRelayCommand(ConnectAsync);
            SendMessageCommand = new AsyncRelayCommand(SendMessageAsync);
        }

        private async Task ConnectAsync()
        {
            if (string.IsNullOrWhiteSpace(Username)) return;

            ChatLog += "[Sistema] Conectando al puerto 8885...\n";
            bool success = await _service.ConnectAsync("127.0.0.1", 8885, Username);
            if (success)
            {
                IsConnected = true;
            }
        }

        private async Task SendMessageAsync()
        {
            if (string.IsNullOrWhiteSpace(Mensaje)) return;

            await _service.SendMessageAsync(Username, Mensaje);
            Mensaje = ""; // Limpiar el input
        }
    }
}
