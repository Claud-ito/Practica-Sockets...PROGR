using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PracticaSockets_Core.Helpers;
using PracticaSockets_Server.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace PracticaSockets_Server.ViewModel
{
    public partial class MainViewModel
    {
        private readonly TcpServerService _basicSvc;
        private readonly MultiThreadServerService _multiSvc;
        private readonly FileServerService _fileSvc;
        private readonly SslServerService _sslSvc;
        private readonly ChatServerService _chatSvc;

        // ── [ObservableProperty] genera automáticamente la propiedad pública
        //    con INotifyPropertyChanged incorporado ──────────────────────────────────
        [ObservableProperty] private bool _basicRunning;
        [ObservableProperty] private bool _multiThreadRunning;
        [ObservableProperty] private bool _fileServerRunning;
        [ObservableProperty] private bool _sslRunning;
        [ObservableProperty] private bool _chatRunning;
        [ObservableProperty] private int _connectedClients;
        [ObservableProperty] private string _localIp = SocketHelpers.GetLocalIp();

        // ── Log (observable collection: la UI se actualiza sola) ───────────────────
        public ObservableCollection<string> LogEntries { get; } = new ObservableCollection<string>();

        public MainViewModel()
        {
            _basicSvc = new TcpServerService(SocketHelpers.PortBasico, Log);
            _multiSvc = new MultiThreadServerService(SocketHelpers.PortMultiHilos, Log);
            _fileSvc = new FileServerService(SocketHelpers.PortArchivos, Log);
            _sslSvc = new SslServerService(SocketHelpers.PortSsl, Log);
            _chatSvc = new ChatServerService(SocketHelpers.PortChat, Log,
                            count => ConnectedClients = count);

            Log("Panel listo. Inicia los servicios que necesites.");
            Log($"IP local detectada: {LocalIp}");
        }

        // ── Comandos TCP Básico ────────────────────────────────────────────────────
        // [RelayCommand] genera BasicRunningCommand, StopBasicCommand, etc.
        [RelayCommand]
        private async Task StartBasic()
        {
            await _basicSvc.StartAsync();
            BasicRunning = true;
        }

        [RelayCommand]
        private void StopBasic()
        {
            _basicSvc.Stop();
            BasicRunning = false;
        }

        // ── Comandos Multi-Hilos ───────────────────────────────────────────────────
        [RelayCommand]
        private async Task StartMultiThread()
        {
            await _multiSvc.StartAsync();
            MultiThreadRunning = true;
        }

        [RelayCommand]
        private void StopMultiThread()
        {
            _multiSvc.Stop();
            MultiThreadRunning = false;
        }

        // ── Comandos Archivos ──────────────────────────────────────────────────────
        [RelayCommand]
        private async Task StartFileServer()
        {
            await _fileSvc.StartAsync();
            FileServerRunning = true;
        }

        [RelayCommand]
        private void StopFileServer()
        {
            _fileSvc.Stop();
            FileServerRunning = false;
        }

        // ── Comandos SSL ───────────────────────────────────────────────────────────
        [RelayCommand]
        private async Task StartSsl()
        {
            await _sslSvc.StartAsync();
            SslRunning = true;
        }

        [RelayCommand]
        private void StopSsl()
        {
            _sslSvc.Stop();
            SslRunning = false;
        }

        // ── Comandos Chat ──────────────────────────────────────────────────────────
        [RelayCommand]
        private async Task StartChat()
        {
            await _chatSvc.StartAsync();
            ChatRunning = true;
        }

        [RelayCommand]
        private void StopChat()
        {
            _chatSvc.Stop();
            ChatRunning = false;
        }

        // ── Globales ───────────────────────────────────────────────────────────────
        [RelayCommand]
        private async Task StartAll()
        {
            await StartBasic();
            await StartMultiThread();
            await StartFileServer();
            await StartSsl();
            await StartChat();
        }

        [RelayCommand]
        private void StopAll()
        {
            StopBasic(); StopMultiThread();
            StopFileServer(); StopSsl(); StopChat();
        }

        [RelayCommand]
        private void ClearLog() => LogEntries.Clear();

        // ── Helper: siempre escribe en el hilo UI ──────────────────────────────────
        private void Log(string msg)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                LogEntries.Insert(0, $"[{DateTime.Now:HH:mm:ss}]  {msg}");
                if (LogEntries.Count > 400)
                    LogEntries.RemoveAt(LogEntries.Count - 1);
            });
        }

    }
}
