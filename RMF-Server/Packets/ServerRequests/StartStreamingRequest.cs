using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.Packets.ServerRequests
{
    internal class StartStreamingRequest : Packet
    {
        public override short ID => 200;

        public bool IsActive { get; set; }       // 0 - no, 1 - yes
        public byte Quality { get; set; }        // 1-100% of source screenshot quality
        public short IntervalMsecs { get; set; } // Interval between sending screenshots in milliseconds

        protected override byte[] SerializePayload()
        {
            using MemoryStream ms = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(ms);
            
            writer.Write(this.IsActive);
            writer.Write(this.Quality);
            writer.Write(this.IntervalMsecs);
            return ms.ToArray();
        }

        public override void Deserialize(BinaryReader reader) { }
    }
}
