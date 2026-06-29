using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PracticaSockets_Core.Enums
{

    public enum MessageType
    {
        Text = 0,  // Texto plano
        FileInfo = 1,  // Cabecera de transferencia (nombre, tamaño, chunks)
        FileChunk = 2,  // Fragmento binario de un archivo
        FileComplete = 3,  // Confirmación de recepción completa
        Ping = 4,  // Prueba de conectividad
        Pong = 5,  // Respuesta a Ping
        ChatMessage = 6,  // Mensaje de chat
        UserConnected = 7,  // Notificación de entrada de usuario
        UserDisconnected = 8,  // Notificación de salida de usuario
        ServerInfo = 9,  // Información del servidor (IP, puerto, protocolo)
        Error = 10    // Error
    }
}
