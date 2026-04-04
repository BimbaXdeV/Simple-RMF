using RMF.Core.Interfaces;
using RMF.Core.Network;
using RMF.Core.Screen;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF.Core.Packets.Client
{
    public class StreamFramePacket : Packet, IReleasable
    {
        public override short ID => 201;

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
            if (this.ImageLength <= 0)
            {
                throw new Exception("Invalid image length");
            }
            this.ImageData = ArrayPool<byte>.Shared.Rent(this.ImageLength);
            reader.ReadBytes(this.ImageLength).CopyTo(this.ImageData);
        }

        protected override void WriteBody(BinaryWriter writer)
        {
            writer.Write(this.FormatID);
            writer.Write(this.Width);
            writer.Write(this.Height);
            writer.Write(this.ImageLength);
            if (this.ImageData != null)
            {
                writer.Write(this.ImageData, 0, this.ImageLength);
            }
        }

        public void Release()
        {
            if (this.ImageData != null)
            {
                ArrayPool<byte>.Shared.Return(this.ImageData);
            }
        }
    }
}
