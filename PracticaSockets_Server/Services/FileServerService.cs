using System;
using System.Collections.Generic;
using System.IO;
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
    public class FileServerService
    {

        private readonly int _port;
        private readonly Action<string> _log;
        private readonly string _saveFolder;
        private TcpListener _listener;
        private CancellationTokenSource _cts;

        public FileServerService(int port, Action<string> log)
        {
            _port = port;
            _log = log;
            _saveFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                "Recibidos_Servidor");
            Directory.CreateDirectory(_saveFolder);
        }

        public Task StartAsync()
        {
            _cts = new CancellationTokenSource();
            _listener = new TcpListener(IPAddress.Any, _port);
            _listener.Start(backlog: 5);
            _log($"[Archivos] ✅ Escuchando en 0.0.0.0:{_port}");
            _log($"[Archivos] 📁 Carpeta de destino: {_saveFolder}");

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
                    _log($"[Archivos] 🔗 Cliente: {client.Client.RemoteEndPoint}");
                    _ = Task.Run(() => ReceiveFileAsync(client));
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex) when (!_cts.Token.IsCancellationRequested)
                {
                    _log($"[Archivos] ⚠ {ex.Message}");
                }
            }
        }

        private async Task ReceiveFileAsync(TcpClient client)
        {
            try
            {
                using (client)
                using (var stream = client.GetStream())
                {

                // ── Paso 1: leer cabecera FileInfo ─────────────────────────────
                    var infoMsg = await SocketHelpers.ReceiveMessageAsync(stream);
                    if (infoMsg?.Type != MessageType.FileInfo)
                    {
                        _log("[Archivos] ⚠ Primer mensaje no es FileInfo, abortando.");
                        return;
                    }

                    var header = JsonSerializer.Deserialize<FileTransferPacket>(infoMsg.Content);
                    if (header is null) return;

                    _log($"[Archivos] 📂 Recibiendo: {header.FileName} " +
                         $"({header.TotalSize:N0} bytes · {header.TotalChunks} chunks)");

                    // ── Paso 2: recibir chunks ─────────────────────────────────────
                    using (var ms = new MemoryStream((int)Math.Min(header.TotalSize, int.MaxValue)))
                    {
                        int lastPercent = 0;

                        for (int i = 0; i < header.TotalChunks; i++)
                        {
                            var chunkMsg = await SocketHelpers.ReceiveMessageAsync(stream);
                            if (chunkMsg?.Type != MessageType.FileChunk || chunkMsg.BinaryData is null)
                            {
                                _log($"[Archivos] ⚠ Chunk {i} inválido, abortando.");
                                return;
                            }

                            ms.Write(chunkMsg.BinaryData, 0, chunkMsg.BinaryData.Length);

                            int pct = (int)((double)(i + 1) / header.TotalChunks * 100);
                            if (pct >= lastPercent + 10)
                            {
                                _log($"[Archivos] ⏳ {pct}% ({i + 1}/{header.TotalChunks})");
                                lastPercent = pct;
                            }
                        }

                        // ── Paso 3: guardar en disco ───────────────────────────────────
                        var savePath = Path.Combine(_saveFolder, Path.GetFileName(header.FileName));
                        await File.WriteAllBytesAsync(savePath, ms.ToArray());

                        _log($"[Archivos] ✅ Guardado: {savePath}");

                        // ── Paso 4: confirmar al cliente ───────────────────────────────
                        await SocketHelpers.SendMessageAsync(stream, new SocketMessage
                        {
                            Type = MessageType.FileComplete,
                            Sender = "Servidor",
                            Content = $"✅ Archivo recibido correctamente\n" +
                                      $"Nombre  : {header.FileName}\n" +
                                      $"Tamaño  : {header.TotalSize:N0} bytes\n" +
                                      $"Guardado: {savePath}"
                        });
                    }
                }
            }


            catch (Exception ex)
            {
                _log($"[FILE] ✅ Archivo {header.FileName} recibido exitosamente en: {finalPath}");
            }
                
            
        }

        public void Stop()
        {
            _cts?.Cancel();
            _listener?.Stop();
            _log($"[Archivos] ⛔ Detenido (puerto {_port})");
        }
    }
}
