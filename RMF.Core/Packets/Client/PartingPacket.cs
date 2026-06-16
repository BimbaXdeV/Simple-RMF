using RMF.Core.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF.Core.Packets.Client
{
    public class PartingPacket : Packet
    {
        public override short ID => 103;

        public byte StatusCode { get; set; }  // All possible IDs check in RMF.Core.Packets.PartingStatusCodes
        public long UptimeSecs { get; set; }  // How many seconds the client was connected before sending this packet
        public long ReceivedPackets { get; set; }
        public long SentPackets { get; set; }
        public long LastTransferedTimestamp { get; set; }

        public override void Deserialize(ref SpanReader reader)
        {
            this.StatusCode = reader.ReadByte();
            this.UptimeSecs = reader.ReadInt64();
            this.ReceivedPackets = reader.ReadInt64();
            this.SentPackets = reader.ReadInt64();
            this.LastTransferedTimestamp = reader.ReadInt64();
        }

        protected override void WriteBody(BinaryWriter writer)
        {
            writer.Write(this.StatusCode);
            writer.Write(this.UptimeSecs);
            writer.Write(this.ReceivedPackets);
            writer.Write(this.SentPackets);
            writer.Write(this.LastTransferedTimestamp);
        }
    }
}
