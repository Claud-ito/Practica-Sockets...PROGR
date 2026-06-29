using PracticaSockets_Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace PracticaSockets_Core.Helpers
{
    public static class SocketHelpers
    {
        public const string DefaultHost = "127.0.0.1";
        public const int PortBasico = 8880;   // Tabs 1 y 2 (configuración + no bloqueante)
        public const int PortMultiHilos = 8882;   // Tab 3
        public const int PortArchivos = 8883;   // Tab 4
        public const int PortSsl = 8884;   // Tab 5
        public const int PortChat = 8885;   // Tab 6

        public const int ChunkSize = 32_768; // 32 KB por fragmento de archivo

        // ── Validación ─────────────────────────────────────────────────────
        public static bool IsValidIp(string ip)
            => IPAddress.TryParse(ip, out _);

        public static bool IsValidPort(string port)
            => int.TryParse(port, out int n) && n >= 1 && n <= 65535;

        /// <summary>
        /// Devuelve la primera dirección IPv4 local (no loopback) del equipo.
        /// </summary>
        public static string GetLocalIp()
        {
            try
            {
                foreach (var ip in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                        return ip.ToString();
            }
            catch { /* ignored */ }
            return "127.0.0.1";
        }

        // ── Envío ──────────────────────────────────────────────────────────

        /// <summary>
        /// Serializa el mensaje y lo escribe en el NetworkStream con el prefijo de longitud.
        /// </summary>
        public static async Task SendMessageAsync(Stream stream, SocketMessage msg)
        {
            var data = msg.Serialize();
            await stream.WriteAsync(data, 0, data.Length);
            await stream.FlushAsync();
        }

        // ── Recepción ──────────────────────────────────────────────────────

        /// <summary>
        /// Lee exactamente <paramref name="count"/> bytes del stream.
        /// Necesario porque TCP puede entregar los datos en múltiples paquetes.
        /// Devuelve null si la conexión se cierra antes de completar la lectura.
        /// </summary>
        public static async Task<byte[]> ReadExactAsync(Stream stream, int count)
        {
            var buffer = new byte[count];
            int received = 0;

            while (received < count)
            {
                int read = await stream.ReadAsync(buffer, received, count - received);
                if (read == 0) return null;   // Stream cerrado por el otro extremo
                received += read;
            }
            return buffer;
        }

        /// <summary>
        /// Recibe un SocketMessage completo:
        ///   1. Lee 4 bytes → longitud del payload
        ///   2. Lee exactamente esa cantidad → deserializa
        /// Devuelve null si la conexión se cierra o si el mensaje está malformado.
        /// </summary>
        public static async Task<SocketMessage> ReceiveMessageAsync(Stream stream)
        {
            try
            {
                var lengthBytes = await ReadExactAsync(stream, 4);
                if (lengthBytes == null) return null;

                int length = BitConverter.ToInt32(lengthBytes, 0);
                if (length <= 0 || length > 10_000_000) return null; // sanity: máx 10 MB

                var payload = await ReadExactAsync(stream, length);
                return payload == null ? null : SocketMessage.Deserialize(payload);
            }
            catch
            {
                return null;
            }
        }
    }
}
