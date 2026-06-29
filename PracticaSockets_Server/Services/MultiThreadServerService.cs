using PracticaSockets_Core.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PracticaSockets_Core.Enums;
using PracticaSockets_Core.Models;

namespace PracticaSockets_Server.Services
{
    public class MultiThreadServerService
    {
        private readonly int _port;
        private readonly Action<string> _log;
        private TcpListener _listener;
        private CancellationTokenSource _cts;
        private int _clientCounter = 0;

        public MultiThreadServerService(int port, Action<string> log)
        {
            _port = port;
            _log = log;
        }

        public Task StartAsync()
        {
            _cts = new CancellationTokenSource();
            _listener = new TcpListener(IPAddress.Any, _port);
            _listener.Start(backlog: 50);
            _log($"[MultiThread] ✅ Escuchando en 0.0.0.0:{_port}");

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
                    int id = Interlocked.Increment(ref _clientCounter);
                    var ep = client.Client.RemoteEndPoint?.ToString() ?? "?";

                    _log($"[MultiThread] 🔗 Cliente #{id} conectado: {ep}");

                    // Thread clásico: cada conexión tiene su propio hilo del SO
                    var thread = new Thread(() => HandleClient(client, id, ep))
                    {
                        IsBackground = true,
                        Name = $"SocketClient-{id}"
                    };
                    thread.Start();
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex) when (!_cts.Token.IsCancellationRequested)
                {
                    _log($"[MultiThread] ⚠ {ex.Message}");
                }
            }
        }

        private void HandleClient(TcpClient client, int clientId, string endpoint)
        {
            int threadId = Thread.CurrentThread.ManagedThreadId;
            _log($"[MultiThread] 🧵 Thread ID={threadId} → Cliente #{clientId}");

            try
            {
                using (client)
                using (var stream = client.GetStream())
                {

                // Llamada síncrona (estamos en el hilo dedicado, no bloqueamos nada)
                var msg = SocketHelpers.ReceiveMessageAsync(stream).GetAwaiter().GetResult();
                if (msg is null) return;

                _log($"[MultiThread] 📨 Cliente #{clientId} (Thread {threadId}): \"{msg.Content}\"");

                // Simular trabajo para demostrar concurrencia real en el informe
                Thread.Sleep(800);

                var response = new SocketMessage
                {
                    Type = MessageType.ServerInfo,
                    Sender = "Servidor",
                    Content = $"✅ Procesado en Thread #{threadId}\n" +
                              $"Cliente  : #{clientId}\n" +
                              $"Mensaje  : «{msg.Content}»\n" +
                              $"Tiempo proceso: 800 ms simulados"
                };
                SocketHelpers.SendMessageAsync(stream, response).GetAwaiter().GetResult();
                }
            }
            catch (Exception ex)
            {
                _log($"[MultiThread] ⚠ Cliente #{clientId}: {ex.Message}");
            }
            finally
            {
                _log($"[MultiThread] 🔌 Cliente #{clientId} desconectado (Thread {threadId})");
            }
        }

        public void Stop()
        {
            _cts?.Cancel();
            _listener?.Stop();
            _clientCounter = 0;
            _log($"[MultiThread] ⛔ Detenido (puerto {_port})");
        }
    }
}
