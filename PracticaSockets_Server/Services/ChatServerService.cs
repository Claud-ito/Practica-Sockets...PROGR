using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PracticaSockets_Core.Enums;
using PracticaSockets_Core.Helpers;
using PracticaSockets_Core.Models;

namespace PracticaSockets_Server.Services
{
    public class ChatServerService
    {

        private readonly int _port;
        private readonly Action<string> _log;
        private readonly Action<int> _updateCount;   // callback → actualiza UI

        private TcpListener _listener;
        private CancellationTokenSource _cts;

        // ConcurrentDictionary: acceso seguro desde múltiples hilos simultáneamente
        private readonly ConcurrentDictionary<string, (TcpClient Client, NetworkStream Stream, string Username)>
            _clients = new ConcurrentDictionary<string, (TcpClient, NetworkStream, string)>();

        public int ConnectedCount => _clients.Count;

        public ChatServerService(int port, Action<string> log, Action<int> updateCount)
        {
            _port = port;
            _log = log;
            _updateCount = updateCount;
        }

        public Task StartAsync()
        {
            _cts = new CancellationTokenSource();
            _listener = new TcpListener(IPAddress.Any, _port);
            _listener.Start(backlog: 100);
            _log($"[Chat] ✅ Servidor de chat listo en 0.0.0.0:{_port}");

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
                    _ = Task.Run(() => HandleClientAsync(client));
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex) when (!_cts.Token.IsCancellationRequested)
                {
                    _log($"[Chat] ⚠ {ex.Message}");
                }
            }
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            string username = null;
            string clientKey = Guid.NewGuid().ToString("N").Substring(0, 8);
            NetworkStream stream = null;

            try
            {
                stream = client.GetStream();

                // El primer mensaje debe ser UserConnected con el nombre del usuario
                var firstMsg = await SocketHelpers.ReceiveMessageAsync(stream);
                if (firstMsg?.Type != MessageType.UserConnected)
                {
                    client.Close();
                    return;
                }

                username = firstMsg.Sender;
                _clients.TryAdd(clientKey, (client, stream, username));
                _updateCount(_clients.Count);
                _log($"[Chat] 👤 {username} conectado · Total: {_clients.Count}");

                // Avisar a todos los demás
                await BroadcastAsync(new SocketMessage
                {
                    Type = MessageType.UserConnected,
                    Sender = "Servidor",
                    Content = $"👋 {username} se unió al chat"
                }, excludeKey: clientKey);

                // ── Bucle de mensajes ──────────────────────────────────────────────
                while (!_cts.Token.IsCancellationRequested)
                {
                    var msg = await SocketHelpers.ReceiveMessageAsync(stream);
                    if (msg is null) break;   // Conexión cerrada

                    if (msg.Type == MessageType.ChatMessage)
                    {
                        _log($"[Chat] 💬 {username}: {msg.Content}");
                        await BroadcastAsync(msg, excludeKey: clientKey);
                    }
                }
            }
            catch when (_cts?.IsCancellationRequested == false)
            {
                // Desconexión abrupta: normal en chat
            }
            finally
            {
                _clients.TryRemove(clientKey, out _);
                client.Close();
                _updateCount(_clients.Count);

                if (username is not null)
                {
                    _log($"[Chat] 🔌 {username} desconectado · Total: {_clients.Count}");
                    await BroadcastAsync(new SocketMessage
                    {
                        Type = MessageType.UserDisconnected,
                        Sender = "Servidor",
                        Content = $"👋 {username} salió del chat"
                    }, excludeKey: null);
                }
            }
        }

        /// <summary>
        /// Envía el mensaje a todos los clientes conectados, opcionalmente excluyendo al emisor.
        /// </summary>
        private async Task BroadcastAsync(SocketMessage msg, string excludeKey)
        {
            foreach (var (key, (_, stream, _)) in _clients)
            {
                if (key == excludeKey) continue;
                try { await SocketHelpers.SendMessageAsync(stream, msg); }
                catch { /* El cliente se limpiará en su propio bucle */ }
            }
        }

        public void Stop()
        {
            _cts?.Cancel();
            _listener?.Stop();
            foreach (var (_, (c, _, _)) in _clients) c.Close();
            _clients.Clear();
            _updateCount(0);
            _log($"[Chat] ⛔ Detenido (puerto {_port})");
        }
    }
}
