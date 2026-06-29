using PracticaSockets_Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PracticaSockets_Core.Models
{
    public class SocketMessage
    {
        public MessageType Type { get; set; } = MessageType.Text;
        public string Sender { get; set; } = "Anónimo";
        public string Content { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public byte[] BinaryData { get; set; }   // Solo para transferencia de archivos
        public byte[] Serialize()
        {
            var serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
            var json = serializer.Serialize(this);
            var jsonBytes = Encoding.UTF8.GetBytes(json);
            var prefix = BitConverter.GetBytes(jsonBytes.Length);   // 4 bytes

            var result = new byte[4 + jsonBytes.Length];
            Buffer.BlockCopy(prefix, 0, result, 0, 4);
            Buffer.BlockCopy(jsonBytes, 0, result, 4, jsonBytes.Length);
            return result;
        }

        public static SocketMessage Deserialize(byte[] payload)
        {
            var json = Encoding.UTF8.GetString(payload);
            var serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
            var obj = serializer.Deserialize<SocketMessage>(json);
            if (obj == null) 
            {
                return new SocketMessage { Type = MessageType.Error, Content = "Desserialización fallida" };
            }
            return obj;
        }
    }
}
