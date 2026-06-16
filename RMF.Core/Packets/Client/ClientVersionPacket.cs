using RMF.Core.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF.Core.Packets.Client
{
    public class ClientVersionPacket : Packet
    {
        public override short ID => 102;
        public short AppMajorVersion { get; set; }
        public short AppMinorVersion { get; set; }
        public short AppBuildVersion { get; set; }
        public short CoreMajorVersion { get; set; }
        public short CoreMinorVersion { get; set; }
        public short CoreBuildVersion { get; set; }

        public override void Deserialize(ref SpanReader reader)
        {
            this.AppMajorVersion = reader.ReadInt16();
            this.AppMinorVersion = reader.ReadInt16();
            this.AppBuildVersion = reader.ReadInt16();
            this.CoreMajorVersion = reader.ReadInt16();
            this.CoreMinorVersion = reader.ReadInt16();
            this.CoreBuildVersion = reader.ReadInt16();
        }

        protected override void WriteBody(BinaryWriter writer)
        {
            writer.Write(this.AppMajorVersion);
            writer.Write(this.AppMinorVersion);
            writer.Write(this.AppBuildVersion);
            writer.Write(this.CoreMajorVersion);
            writer.Write(this.CoreMinorVersion);
            writer.Write(this.CoreBuildVersion);
        }
    }
}
