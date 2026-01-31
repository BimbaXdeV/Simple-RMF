using FMF_Packets.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Packets.Shared.Client
{
    public class RemoteDesktopPacket : Packet
    {
        public override short ID => 101;
        
        public byte Format { get; set; }  // 0 - JPEG, 1 - PNG
        public int Width { get; set; }
        public int Height { get; set; }
        public int ImageLength { get; set; }
        public byte[]? ImageData { get; set; }

        protected override byte[] Serialize()
        {
            using MemoryStream ms = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(ms);

            writer.Write(this.Format);
            writer.Write(this.Width);
            writer.Write(this.Height);
            writer.Write(this.ImageLength);
            writer.Write(this.ImageData!);
            return ms.ToArray();
        }

        public override void Deserialize(BinaryReader reader)
        {
            this.Format = reader.ReadByte();
            this.Width = reader.ReadInt32();
            this.Height = reader.ReadInt32();
            this.ImageLength = reader.ReadInt32();
            if (this.ImageLength > PacketConfigurations.MaxPacketLengthMB || this.ImageLength <= 0)
            {
                throw new Exception("Invalid image length");
            }
            this.ImageData = reader.ReadBytes(this.ImageLength);
        }
    }
}
