using RMF.Core.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF.Core.Packets.Server
{
    public class EndOfEventsPacket : Packet
    {
        public override short ID => 304;

        public override void Deserialize(ref SpanReader reader)
        {
        }

        protected override void WriteBody(BinaryWriter writer)
        {
        }
    }
}
