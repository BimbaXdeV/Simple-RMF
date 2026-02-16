using RMF.Core.Network;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF.Core.Packets.Client
{
    public class SystemInfoPacket : Packet
    {
        public override short ID => 101;

        public string MachineName { get; set; } = "Unknown";
        public string Username { get; set; } = "Noname";
        public string OS { get; set; } = "Unknown";
        public string Architecture { get; set; } = "Unknown";

        public override void Deserialize(ref SpanReader reader)
        {
            this.MachineName = reader.ReadString();
            this.Username = reader.ReadString();
            this.OS = reader.ReadString();
            this.Architecture = reader.ReadString();
        }
        protected override void WriteBody(BinaryWriter writer)
        {
            writer.Write(this.MachineName);
            writer.Write(this.Username);
            writer.Write(this.OS);
            writer.Write(this.Architecture);
        }
    }
}
