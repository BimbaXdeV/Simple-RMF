using RMF.Core.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF.Core.Packets.Server
{
    public class ScreenshotRequest : Packet
    {
        public override short ID => 303;

        public byte FormatID;
        public byte QualityPercent;
        public int SendingIntervalMsecs;

        public override void Deserialize(ref SpanReader reader)
        {
            this.FormatID = reader.ReadByte();
            this.QualityPercent = reader.ReadByte();
            this.SendingIntervalMsecs = reader.ReadInt32();
        }

        protected override void WriteBody(BinaryWriter writer)
        {
            writer.Write(this.FormatID);
            writer.Write(this.QualityPercent);
            writer.Write(this.SendingIntervalMsecs);
        }
    }
}
