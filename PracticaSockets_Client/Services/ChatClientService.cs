using PracticaSockets_Core.Enums;
using PracticaSockets_Core.Helpers;
using PracticaSockets_Core.Models;
using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace PracticaSockets_Client.Services
{
    public class ChatClientService
    {
        private TcpClient _client;
        private NetworkStream _stream;
        private CancellationTokenSource _cts;

        public event Action<string> OnMessageReceived;
        public event Action<string> OnDisconnected;

        public async Task<bool> ConnectAsync(string ip, int port, string username)
        {
            try
            {
                _client = new TcpClient();
                await _client.ConnectAsync(ip, port);
                _stream = _client.GetStream();
                _cts = new CancellationTokenSource();

                // Send login message
                var loginMsg = new SocketMessage
                {
                    Type = MessageType.UserConnected,
                    Sender = username,
                    Content = "Ha entrado al chat"
                };
                await SocketHelpers.SendMessageAsync(_stream, loginMsg);

                // Start listening loop
                _ = Task.Run(() => ReceiveLoopAsync(_cts.Token));
                
                return true;
            }
            catch (Exception ex)
            {
                OnDisconnected?.Invoke(ex.Message);
                return false;
            }
        }

        public async Task SendMessageAsync(string username, string content)
        {
            if (_stream == null || !_client.Connected) return;

            var msg = new SocketMessage
            {
                Type = MessageType.Text,
                Sender = username,
                Content = content
            };

            await SocketHelpers.SendMessageAsync(_stream, msg);
        }

        private async Task ReceiveLoopAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    var msg = await SocketHelpers.ReceiveMessageAsync(_stream);
                    if (msg == null) break;

                    string display = $"[{msg.Timestamp:HH:mm:ss}] {msg.Sender}: {msg.Content}";
                    OnMessageReceived?.Invoke(display);
                }
            }
            catch
            {
                // Ignored
            }
            finally
            {
                Disconnect();
            }
        }

        public void Disconnect()
        {
            _cts?.Cancel();
            _stream?.Close();
            _client?.Close();
            OnDisconnected?.Invoke("Desconectado.");
        }
    }
}
