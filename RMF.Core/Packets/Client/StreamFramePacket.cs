using RMF.Core.Interfaces;
using RMF.Core.Network;
using RMF.Core.Screen;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace RMF.Core.Packets.Client
{
    public class StreamFramePacket : Packet, IReleasable
    {
        public override short ID => 201;

        public bool IsFullFrame { get; set; }
        public byte FormatID { get; set; }
        public short PatchesCount { get; set; }
        public ScreenPatch[]? Patches { get; set; }

        public override void Deserialize(ref SpanReader reader)
        {
            this.IsFullFrame = reader.ReadBoolean();
            this.FormatID = reader.ReadByte();
            this.PatchesCount = reader.ReadInt16();

            ScreenPatch[] patches = ArrayPool<ScreenPatch>.Shared.Rent(this.PatchesCount);
            for (int i = 0; i < this.PatchesCount; i++)
            {
                short x = reader.ReadInt16();
                short y = reader.ReadInt16();
                short width = reader.ReadInt16();
                short height = reader.ReadInt16();
                int length = reader.ReadInt32();

                byte[] data = ArrayPool<byte>.Shared.Rent(length);
                reader.ReadBytes(length).CopyTo(data);

                patches[i] = new ScreenPatch(
                    data,
                    length,
                    x,
                    y,
                    width,
                    height
                );
            }

            this.Patches = patches;
        }

        protected override void WriteBody(BinaryWriter writer)
        {
            writer.Write(this.IsFullFrame);
            writer.Write(this.FormatID);
            writer.Write(this.PatchesCount);

            for (int i = 0; i < this.PatchesCount; i++)
            {
                ScreenPatch patch = this.Patches![i];
                writer.Write(patch.X);
                writer.Write(patch.Y);
                writer.Write(patch.Width);
                writer.Write(patch.Height);
                writer.Write(patch.Length);
                if (patch.Data != null)
                {
                    writer.Write(patch.Data, 0, patch.Length);
                }
            }
        }

        public void Release()
        {
            if (this.Patches == null)
            {
                return;
            }

            for (int i = 0; i < this.PatchesCount; i++)
            {
                if (this.Patches[i].Data != null && this.Patches[i] is IReleasable releasable)
                {
                    releasable.Release();
                }
            }
            ArrayPool<ScreenPatch>.Shared.Return(this.Patches);
            this.Patches = null;
            this.PatchesCount = 0;
        }
    }
}
