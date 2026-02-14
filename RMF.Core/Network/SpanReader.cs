using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF.Core.Network
{
    public ref struct SpanReader
    {
        private ReadOnlySpan<byte> Buffer;
        private int Position;

        public SpanReader(ReadOnlySpan<byte> buffer)
        {
            this.Buffer = buffer;
            this.Position = 0;
        }

        public byte ReadByte()
        {
            byte result = this.Buffer[this.Position];
            this.Position++;
            return result;
        }

        public byte[] ReadBytes(int count)
        {
            byte[] result = this.Buffer.Slice(this.Position, count).ToArray();
            this.Position += count;
            return result;
        }

        public bool ReadBoolean()
        {
            bool result = this.Buffer[this.Position] != 0;
            this.Position++;
            return result;
        }

        public short ReadInt16()
        {
            short result = BinaryPrimitives.ReadInt16LittleEndian(this.Buffer.Slice(this.Position));
            this.Position += 2;
            return result;
        }

        public int ReadInt32()
        {
            int result = BinaryPrimitives.ReadInt32LittleEndian(this.Buffer.Slice(this.Position));
            this.Position += 4;
            return result;
        }

        public long ReadInt64()
        {
            long result = BinaryPrimitives.ReadInt64LittleEndian(this.Buffer.Slice(this.Position));
            this.Position += 8;
            return result;
        }

        public string ReadString()
        {
            int length = ReadInt32();
            string result = Encoding.UTF8.GetString(Buffer.Slice(this.Position, length));
            this.Position += length;
            return result;
        }
    }
}
