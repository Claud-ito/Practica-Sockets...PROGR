using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PracticaSockets_Core.Enums;
using PracticaSockets_Core.Helpers;
using PracticaSockets_Core.Models;

namespace PracticaSockets_Server.Services
{
    public class SslServerService
    {

        private readonly int _port;
        private readonly Action<string> _log;
        private TcpListener _listener;
        private CancellationTokenSource _cts;

        // Certificado generado una sola vez al construir el servicio
        private readonly System.Security.Cryptography.X509Certificates.X509Certificate2 _cert;

        public SslServerService(int port, Action<string> log)
        {
            _port = port;
            _log = log;
            _cert = CertificateHelper.GenerateSelfSignedCertificate();
            _log($"[SSL] 🔐 Certificado listo: {_cert.Subject}  " +
                 $"Válido hasta: {_cert.NotAfter:dd/MM/yyyy}");
        }

        public Task StartAsync()
        {
            _cts = new CancellationTokenSource();
            _listener = new TcpListener(IPAddress.Any, _port);
            _listener.Start(backlog: 10);
            _log($"[SSL] ✅ Escuchando en 0.0.0.0:{_port}  (TLS 1.2 / TLS 1.3)");

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
                    _log($"[SSL] 🔗 TCP conectado: {ep} — iniciando handshake…");
                    _ = Task.Run(() => HandleSslClientAsync(client, ep));
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex) when (!_cts.Token.IsCancellationRequested)
                {
                    _log($"[SSL] ⚠ {ex.Message}");
                }
            }
        }

        private async Task HandleSslClientAsync(TcpClient client, string endpoint)
        {
            try
            {
                using (client)
                using (var tcpStream = client.GetStream())
                using (var ssl = new SslStream(tcpStream, leaveInnerStreamOpen: false))
                {

                await ssl.AuthenticateAsServerAsync(
                    serverCertificate: _cert,
                    clientCertificateRequired: false,
                    enabledSslProtocols: SslProtocols.Tls12 | SslProtocols.Tls13,
                    checkCertificateRevocation: false);

                _log($"[SSL] 🔒 Handshake OK con {endpoint}\n" +
                     $"       Protocolo : {ssl.SslProtocol}\n" +
                     $"       Cifrado   : {ssl.CipherAlgorithm} {ssl.CipherStrength} bits\n" +
                     $"       Hash      : {ssl.HashAlgorithm}");

                // ── Leer mensaje (a través del canal cifrado) ─────────────────────────
                // SslStream no es NetworkStream, así que leemos manualmente con el mismo
                // protocolo length-prefixed que usa SocketHelper

                var lenBuf = new byte[4];
                int r = 0;
                while (r < 4) { int n = await ssl.ReadAsync(lenBuf, r, 4 - r); if (n == 0) return; r += n; }

                int len = BitConverter.ToInt32(lenBuf, 0);
                if (len is <= 0 or > 10_000_000) return;

                var payload = new byte[len];
                r = 0;
                while (r < len) { int n = await ssl.ReadAsync(payload, r, len - r); if (n == 0) return; r += n; }

                var msg = SocketMessage.Deserialize(payload);
                _log($"[SSL] 📨 De {msg.Sender}: \"{msg.Content}\"");

                // ── Enviar respuesta cifrada ───────────────────────────────────────────
                var response = new SocketMessage
                {
                    Type = MessageType.ServerInfo,
                    Sender = "Servidor",
                    Content = $"✅ Canal SSL/TLS activo\n" +
                              $"Protocolo  : {ssl.SslProtocol}\n" +
                              $"Algoritmo  : {ssl.CipherAlgorithm} {ssl.CipherStrength} bits\n" +
                              $"Hash       : {ssl.HashAlgorithm}\n" +
                              $"Mensaje    : «{msg.Content}»"
                };
                var data = response.Serialize();
                await ssl.WriteAsync(data, 0, data.Length);
                await ssl.FlushAsync();

                _log($"[SSL] 📤 Respuesta enviada a {endpoint}");
                }
            }
            catch (AuthenticationException ex)
            {
                _log($"[SSL] ❌ Handshake fallido con {endpoint}: {ex.Message}");
            }
            catch (Exception ex)
            {
                _log($"[SSL] ⚠ Error con {endpoint}: {ex.Message}");
            }
        }

        public void Stop()
        {
            _cts?.Cancel();
            _listener?.Stop();
            _log($"[SSL] ⛔ Detenido (puerto {_port})");
        }
    }
}
