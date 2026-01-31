using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Packets.Shared.Server
{
    public class StreamingRequest : Packet
    {
        public override short ID => 201;

        public bool IsActive { get; set; }       // 0 - no, 1 - yes
        public byte Quality { get; set; }        // 1-100% of source screenshot quality
        public short IntervalMsecs { get; set; } // Interval between sending screenshots in milliseconds

        protected override byte[] Serialize()
        {
            using MemoryStream ms = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(ms);
            
            writer.Write(this.IsActive);
            writer.Write(this.Quality);
            writer.Write(this.IntervalMsecs);
            return ms.ToArray();
        }

        public override void Deserialize(BinaryReader reader)
        {
            this.IsActive = reader.ReadBoolean();
            this.Quality = reader.ReadByte();
            this.IntervalMsecs = reader.ReadInt16();
        }
    }
}
