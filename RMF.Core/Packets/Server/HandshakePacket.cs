using RMF.Core.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF.Core.Packets.Server
{
    public class HandshakePacket : Packet
    {
        public override short ID => 302;

        public long ConnectionTimestamp;
        public int SessionID;
        public string? RemoteIP;
        public int RemotePort;
        public int SendBufferSize;
        public int ReceiveBufferSize;

        public override void Deserialize(ref SpanReader reader)
        {
            this.ConnectionTimestamp = reader.ReadInt64();
            this.SessionID = reader.ReadInt32();
            this.RemoteIP = reader.ReadString();
            this.RemotePort = reader.ReadInt32();
            this.SendBufferSize = reader.ReadInt32();
            this.ReceiveBufferSize = reader.ReadInt32();
        }

        protected override void WriteBody(BinaryWriter writer)
        {
            writer.Write(this.ConnectionTimestamp); 
            writer.Write(this.SessionID);
            writer.Write(this.RemoteIP ?? "0.0.0.0");
            writer.Write(this.RemotePort);
            writer.Write(this.SendBufferSize);
            writer.Write(this.ReceiveBufferSize);
        }
    }
}
