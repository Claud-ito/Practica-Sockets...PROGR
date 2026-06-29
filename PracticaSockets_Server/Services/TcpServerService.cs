using PracticaSockets_Core.Enums;
using PracticaSockets_Core.Helpers;
using PracticaSockets_Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PracticaSockets_Server.Services
{
    public class TcpServerService
    {

        private readonly int _port;
        private readonly Action<string> _log;
        private TcpListener _listener;
        private CancellationTokenSource _cts;

        public TcpServerService(int port, Action<string> log)
        {
            _port = port;
            _log = log;
        }

        public Task StartAsync()
        {
            _cts = new CancellationTokenSource();
            _listener = new TcpListener(IPAddress.Any, _port);
            _listener.Start(backlog: 10);
            _log($"[TCP] ✅ Escuchando en 0.0.0.0:{_port}");

            _ = Task.Run(AcceptLoopAsync, _cts.Token);
            return Task.CompletedTask;
        }

        private async Task AcceptLoopAsync()
        {
            while (!_cts!.Token.IsCancellationRequested)
            {
                try
                {
                    var client = await _listener!.AcceptTcpClientAsync(_cts.Token);
                    var ep = client.Client.RemoteEndPoint?.ToString() ?? "?";
                    _log($"[TCP] 🔗 Cliente conectado desde {ep}");

                    // Manejar en background para no bloquear la aceptación de nuevos clientes
                    _ = Task.Run(() => HandleClientAsync(client, ep));
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex) when (!_cts.Token.IsCancellationRequested)
                {
                    _log($"[TCP] ⚠ Error en accept: {ex.Message}");
                }
            }
        }

        private async Task HandleClientAsync(TcpClient client, string endpoint)
        {
            try
            {
                using (client)
                using (var stream = client.GetStream())
                {
                stream.ReadTimeout = 10_000;
                stream.WriteTimeout = 10_000;

                var msg = await SocketHelpers.ReceiveMessageAsync(stream);
                if (msg is null) return;

                _log($"[TCP] 📨 De {msg.Sender} ({endpoint}): \"{msg.Content}\"");

                var response = new SocketMessage
                {
                    Type = MessageType.ServerInfo,
                    Sender = "Servidor",
                    Content = $"✅ Conexión aceptada\n" +
                              $"IP servidor : {SocketHelpers.GetLocalIp()}\n" +
                              $"Puerto      : {_port}\n" +
                              $"Protocolo   : TCP\n" +
                              $"Mensaje     : «{msg.Content}»"
                };
                await SocketHelpers.SendMessageAsync(stream, response);
                _log($"[TCP] 📤 Respuesta enviada a {endpoint}");
                }
            }
            catch (Exception ex)
            {
                _log($"[TCP] ⚠ Error con {endpoint}: {ex.Message}");
            }
        }

        public void Stop()
        {
            _cts?.Cancel();
            _listener?.Stop();
            _log($"[TCP] ⛔ Detenido (puerto {_port})");
        }
    }
}
