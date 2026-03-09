using RMF.Core.Network;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF.Core.Packets.Client
{
    public class RemoteDesktopPacket : Packet
    {
        public override short ID => 200;

        public byte FormatID { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int ImageLength { get; set; }
        public byte[]? ImageData { get; set; }

        public override void Deserialize(ref SpanReader reader)
        {
            this.FormatID = reader.ReadByte();
            this.Width = reader.ReadInt32();
            this.Height = reader.ReadInt32();
            this.ImageLength = reader.ReadInt32();
            if (this.ImageLength > PacketConfigurations.MaxPacketLengthKB || this.ImageLength <= 0)
            {
                throw new Exception("Invalid image length");
            }
            this.ImageData = reader.ReadBytes(this.ImageLength).ToArray();
        }

        protected override void WriteBody(BinaryWriter writer)
        {
            writer.Write(this.FormatID);
            writer.Write(this.Width);
            writer.Write(this.Height);
            writer.Write(this.ImageLength);
            if (this.ImageData != null)
            {
                writer.Write(this.ImageData);
            }
        }
    }
}
