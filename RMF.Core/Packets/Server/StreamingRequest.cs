using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF.Core.Packets.Server
{
    public class StreamingRequest : Packet
    {
        public override short ID => 201;

        public bool IsActive { get; set; }        // 0 - no, 1 - yes
        public byte Quality { get; set; }         // 1-100% of source screenshot quality
        public short IntervalMsecs { get; set; }  // Interval between sending screenshots in milliseconds

        public override void Deserialize(BinaryReader reader)
        {
            IsActive = reader.ReadBoolean();
            Quality = reader.ReadByte();
            IntervalMsecs = reader.ReadInt16();
        }

        protected override void WriteBody(BinaryWriter writer)
        {
            writer.Write(IsActive);
            writer.Write(Quality);
            writer.Write(IntervalMsecs);
        }
    }
}
