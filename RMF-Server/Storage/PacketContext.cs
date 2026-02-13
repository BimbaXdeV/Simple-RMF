using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.Storage
{
    internal class PacketContext
    {
        public string EndPoint { get; }
        
        public short ID { get; }
        public int Length { get; }
        public byte[] Payload { get; }
        
        public PacketContext(string endPoint, short id, int length, byte[] payload)
        {
            this.EndPoint = endPoint;
            this.ID = id;
            this.Length = length;
            this.Payload = payload;
        }
    }
}
