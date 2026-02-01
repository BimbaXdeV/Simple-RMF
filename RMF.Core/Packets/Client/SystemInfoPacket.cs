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
        public override short ID => 100;

        public string OS { get; set; } = "Unknown";
        public string CPU { get; set; } = "Unknown";
        public string GPU { get; set; } = "Unknown";
        public string Username { get; set; } = "Noname";

        public override void Deserialize(BinaryReader reader)
        {
            OS = reader.ReadString();
            CPU = reader.ReadString();
            GPU = reader.ReadString();
            Username = reader.ReadString();
        }
        protected override void WriteBody(BinaryWriter writer)
        {
            writer.Write(OS);
            writer.Write(CPU);
            writer.Write(GPU);
            writer.Write(Username);
        }
    }
}
