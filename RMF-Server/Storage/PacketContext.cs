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
        public byte[] Payload { get; }
        
        public PacketContext(string endPoint, byte[] payload)
        {
            this.EndPoint = endPoint;
            this.Payload = payload;
        }
    }
}
