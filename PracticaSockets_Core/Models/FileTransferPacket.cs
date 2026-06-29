using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PracticaSockets_Core.Models
{
    public class FileTransferPacket
    {

        public string FileName { get; set; } = string.Empty;
        public long TotalSize { get; set; }            // Tamaño total en bytes
        public int ChunkIndex { get; set; }            // Índice del chunk actual (0-based)
        public int TotalChunks { get; set; }            // Total de chunks

        // ── Propiedades calculadas ─────────────────────────────────────────
        public bool IsLastChunk => ChunkIndex == TotalChunks - 1;
        public double ProgressPercent => TotalChunks > 0
            ? (double)(ChunkIndex + 1) / TotalChunks * 100.0
            : 0;
    }
}
