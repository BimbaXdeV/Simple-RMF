using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.Packets.ClientPackets
{
    internal class RemoteDesktopPacket : Packet
    {
        public override short ID => 101;
        
        public byte Format { get; set; }  // 0 - JPEG, 1 - PNG
        public int Width { get; set; }
        public int Height { get; set; }
        public byte[]? ImageData { get; set; }

        protected override byte[] SerializePayload() => [];

        public override void Deserialize(BinaryReader reader)
        {
            this.Format = reader.ReadByte();
            this.Width = reader.ReadInt32();
            this.Height = reader.ReadInt32();
            int imageLength = reader.ReadInt32();
            this.ImageData = reader.ReadBytes(imageLength);
        }
    }
}
