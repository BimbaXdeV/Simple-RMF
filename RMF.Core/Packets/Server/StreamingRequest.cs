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
        public override short ID => 302;

        public bool IsActive { get; set; }        // 0 - no, 1 - yes
        public byte FormatID { get; set; }        // Check ScreenFormats enum for supported formats
        public byte Quality { get; set; }         // 1-100% of source screenshot quality
        public int FrameUpdateRate { get; set; }  // How many frames does it take to refresh the entire screen (this is necessary for synchronization)
        public short TargetFPS { get; set; }      // Maximum screen refresh rate

        public override void Deserialize(ref SpanReader reader)
        {
            this.IsActive = reader.ReadBoolean();
            this.FormatID = reader.ReadByte();
            this.Quality = reader.ReadByte();
            this.FrameUpdateRate = reader.ReadInt32();
            this.TargetFPS = reader.ReadInt16();
        }

        protected override void WriteBody(BinaryWriter writer)
        {
            writer.Write(this.IsActive);
            writer.Write(this.FormatID);
            writer.Write(this.Quality);
            writer.Write(this.FrameUpdateRate);
            writer.Write(this.TargetFPS);
        }
    }
}
