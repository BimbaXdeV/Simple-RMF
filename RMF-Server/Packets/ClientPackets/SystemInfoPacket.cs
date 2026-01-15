using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.Packets.ClientPackets
{
    internal class SystemInfoPacket : Packet
    {
        public override short ID => 100;

        public string OS { get; set; } = "Unknown";
        public string CPU { get; set; } = "Unknown";
        public string GPU { get; set; } = "Unknown";
        public string Username { get; set; } = "Noname";

        protected override byte[] SerializePayload()
        {
            using MemoryStream ms = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(ms);

            writer.Write(this.OS);
            writer.Write(this.CPU);
            writer.Write(this.GPU);
            writer.Write(this.Username);
            return ms.ToArray();
        }

        public override void Deserialize(BinaryReader reader)
        {
            this.OS = reader.ReadString();
            this.CPU = reader.ReadString();
            this.GPU = reader.ReadString();
            this.Username = reader.ReadString();
        }
    }
}
