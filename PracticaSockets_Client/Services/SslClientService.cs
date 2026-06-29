using PracticaSockets_Core.Helpers;
using PracticaSockets_Core.Models;
using System;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace PracticaSockets_Client.Services
{
    public class SslClientService
    {
        public async Task<string> SendSecureMessageAsync(string ip, int port, string content)
        {
            try
            {
                using (var client = new TcpClient())
                {
                    await client.ConnectAsync(ip, port);
                    using (var stream = client.GetStream())
                    {
                        // Callback que acepta cualquier certificado (solo para pruebas locales)
                        RemoteCertificateValidationCallback validator = 
                            (sender, cert, chain, errors) => true;

                        using (var sslStream = new SslStream(stream, false, validator, null))
                        {
                            // Autenticar como cliente apuntando al "commonName" del certificado
                            await sslStream.AuthenticateAsClientAsync("localhost", null, SslProtocols.Tls12, false);

                            var msg = new SocketMessage
                            {
                                Sender = "ClientWPF_SSL",
                                Content = content
                            };

                            await SocketHelpers.SendMessageAsync(sslStream, msg);

                            var response = await SocketHelpers.ReceiveMessageAsync(sslStream);
                            if (response != null)
                            {
                                return $"[SSL] Respuesta del servidor:\n{response.Content}";
                            }
                            return "[SSL] Conexión cerrada por el servidor.";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return $"Error SSL: {ex.Message}";
            }
        }
    }
}
