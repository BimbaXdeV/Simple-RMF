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
        public override short ID => 101;

        public byte Format { get; set; }  // 0 - JPEG, 1 - PNG
        public int Width { get; set; }
        public int Height { get; set; }
        public int ImageLength { get; set; }
        public byte[]? ImageData { get; set; }

        public override void Deserialize(BinaryReader reader)
        {
            Format = reader.ReadByte();
            Width = reader.ReadInt32();
            Height = reader.ReadInt32();
            ImageLength = reader.ReadInt32();
            if (ImageLength > PacketConfigurations.MaxPacketLengthKB || ImageLength <= 0)
            {
                throw new Exception("Invalid image length");
            }
            ImageData = reader.ReadBytes(ImageLength);
        }

        protected override void WriteBody(BinaryWriter writer)
        {
            writer.Write(Format);
            writer.Write(Width);
            writer.Write(Height);
            writer.Write(ImageLength);
            if (ImageData != null)
            {
                writer.Write(ImageData);
            }
        }
    }
}
