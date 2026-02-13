using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF.Core.Packets.Client
{
    internal class HeartbeatPacket : Packet
    {
        public override short ID => 100;
        public long Timestamp { get; set; }

        public override void Deserialize(BinaryReader reader)
        {
            this.Timestamp = reader.ReadInt64();
        }

        protected override void WriteBody(BinaryWriter writer)
        {
            writer.Write(this.Timestamp);
        }
    }
}
