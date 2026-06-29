using PracticaSockets_Core.Helpers;
using PracticaSockets_Core.Models;
using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace PracticaSockets_Client.Services
{
    public class TcpClientService
    {
        public async Task<string> SendMessageAsync(string ip, int port, string content)
        {
            try
            {
                using (var client = new TcpClient())
                {
                    await client.ConnectAsync(ip, port);
                    using (var stream = client.GetStream())
                    {
                        var msg = new SocketMessage
                        {
                            Sender = "ClientWPF",
                            Content = content
                        };

                        await SocketHelpers.SendMessageAsync(stream, msg);

                        var response = await SocketHelpers.ReceiveMessageAsync(stream);
                        if (response != null)
                        {
                            return $"Respuesta del servidor:\n{response.Content}";
                        }
                        return "Conexión cerrada por el servidor.";
                    }
                }
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }
    }
}
