using RMF.Core.Network;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF.Core.Packets.Client
{
    public class ClientInfoPacket : Packet
    {
        public override short ID => 101;

        public string MachineName { get; set; } = "Unknown";
        public string Username { get; set; } = "Noname";
        public string OSName { get; set; } = "Unknown";
        public string CPUName { get; set; } = "Unknown";
        public string CPUArchitecture { get; set; } = "Unknown";
        public string GPUName { get; set; } = "Unknown";
        public long RAMCapacity { get; set; }
        public long VRAMCapacity { get; set; }

        public override void Deserialize(ref SpanReader reader)
        {
            this.MachineName = reader.ReadString();
            this.Username = reader.ReadString();
            this.OSName = reader.ReadString();
            this.CPUName = reader.ReadString();
            this.CPUArchitecture = reader.ReadString();
            this.GPUName = reader.ReadString();
            this.RAMCapacity = reader.ReadInt64();
            this.VRAMCapacity = reader.ReadInt64();
        }
        protected override void WriteBody(BinaryWriter writer)
        {
            writer.Write(this.MachineName);
            writer.Write(this.Username);
            writer.Write(this.OSName);
            writer.Write(this.CPUName);
            writer.Write(this.CPUArchitecture);
            writer.Write(this.GPUName);
            writer.Write(this.RAMCapacity);
            writer.Write(this.VRAMCapacity);
        }
    }
}
