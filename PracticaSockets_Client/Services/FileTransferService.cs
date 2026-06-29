using PracticaSockets_Core.Helpers;
using PracticaSockets_Core.Models;
using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace PracticaSockets_Client.Services
{
    public class FileTransferService
    {
        public async Task SendFileAsync(string ip, int port, string filePath, Action<double> onProgress, Action<string> onLog)
        {
            try
            {
                using (var client = new TcpClient())
                {
                    await client.ConnectAsync(ip, port);
                    using (var stream = client.GetStream())
                    {
                        var fileInfo = new FileInfo(filePath);
                        long totalSize = fileInfo.Length;
                        int totalChunks = (int)Math.Ceiling((double)totalSize / SocketHelpers.ChunkSize);
                        
                        onLog($"Iniciando envío de {fileInfo.Name} ({totalSize} bytes en {totalChunks} chunks)");

                        using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                        {
                            byte[] buffer = new byte[SocketHelpers.ChunkSize];
                            int bytesRead;
                            int currentChunk = 0;

                            while ((bytesRead = await fs.ReadAsync(buffer, 0, buffer.Length)) > 0)
                            {
                                var packet = new FileTransferPacket
                                {
                                    FileName = fileInfo.Name,
                                    TotalSize = totalSize,
                                    ChunkIndex = currentChunk,
                                    TotalChunks = totalChunks
                                };

                                // Creamos una copia exacta del tamaño leído para el BinaryData
                                byte[] actualData = new byte[bytesRead];
                                Buffer.BlockCopy(buffer, 0, actualData, 0, bytesRead);

                                var msg = new SocketMessage
                                {
                                    Type = PracticaSockets_Core.Enums.MessageType.FileChunk,
                                    Sender = "ClientWPF",
                                    Content = new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(packet),
                                    BinaryData = actualData
                                };

                                await SocketHelpers.SendMessageAsync(stream, msg);
                                
                                onProgress(packet.ProgressPercent);
                                currentChunk++;
                            }
                        }

                        var response = await SocketHelpers.ReceiveMessageAsync(stream);
                        if (response != null)
                        {
                            onLog($"Servidor respondió: {response.Content}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                onLog($"Error al enviar archivo: {ex.Message}");
            }
        }
    }
}
