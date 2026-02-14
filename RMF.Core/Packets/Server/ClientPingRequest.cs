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

        public string Message { get; set; } = "Just hello";

        public override void Deserialize(ref SpanReader reader)
        {
            this.Message = reader.ReadString();
        }

        protected override void WriteBody(BinaryWriter writer)
        {
            writer.Write(this.Message);
        }
    }
}
