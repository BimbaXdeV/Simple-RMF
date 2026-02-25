using RMF.Core.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF.Core.Packets.Server
{
    public class ClientPingRequest : Packet
    {
        public override short ID => 300;

        public int IntervalSecs;

        public override void Deserialize(ref SpanReader reader)
        {
            this.IntervalSecs = reader.ReadInt32();
        }

        protected override void WriteBody(BinaryWriter writer)
        {
            writer.Write(this.IntervalSecs);
        }
    }
}
