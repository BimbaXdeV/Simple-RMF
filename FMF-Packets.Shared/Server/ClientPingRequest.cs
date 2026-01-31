using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Packets.Shared.Server
{
    public class ClientPingRequest : Packet
    {
        public override short ID => 200;

        public string Message { get; set; } = "Just hello";

        protected override byte[] Serialize()
        {
            using MemoryStream ms = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(ms);

            byte[] byteMessage = Encoding.UTF8.GetBytes(this.Message);
            writer.Write(byteMessage);
            return ms.ToArray();
        }

        public override void Deserialize(BinaryReader reader) { }
    }
}
