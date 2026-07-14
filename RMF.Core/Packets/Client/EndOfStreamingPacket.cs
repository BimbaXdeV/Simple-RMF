using RMF.Core.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF.Core.Packets.Client
{
    public class EndOfStreamingPacket : Packet
    {
        public override short ID => 104;
        public string Reason { get; set; } = string.Empty;

        public override void Deserialize(ref SpanReader reader)
        {
            this.Reason = reader.ReadString();
        }

        protected override void WriteBody(BinaryWriter writer)
        {
            writer.Write(this.Reason);
        }
    }
}
