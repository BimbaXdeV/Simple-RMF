using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.Packets
{
    public class PacketContext
    {
        public IPEndPoint EndPoint { get; }

        public short ID { get; }
        public int Length { get; }
        public byte[] Payload { get; }

        public PacketContext(IPEndPoint endPoint, short id, int length, byte[] payload)
        {
            EndPoint = endPoint;
            ID = id;
            Length = length;
            Payload = payload;
        }
    }
}
