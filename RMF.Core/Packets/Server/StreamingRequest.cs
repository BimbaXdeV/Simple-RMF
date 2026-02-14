using RMF.Core.Network;
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
        public override short ID => 301;

        public bool IsActive { get; set; }        // 0 - no, 1 - yes
        public byte Quality { get; set; }         // 1-100% of source screenshot quality
        public short IntervalMsecs { get; set; }  // Interval between sending screenshots in milliseconds

        public override void Deserialize(ref SpanReader reader)
        {
            this.IsActive = reader.ReadBoolean();
            this.Quality = reader.ReadByte();
            this.IntervalMsecs = reader.ReadInt16();
        }

        protected override void WriteBody(BinaryWriter writer)
        {
            writer.Write(this.IsActive);
            writer.Write(this.Quality);
            writer.Write(this.IntervalMsecs);
        }
    }
}
