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

        private void EnsureCapacity(int bytesCount)
        {
            if (this.Position + bytesCount > this.Buffer.Length)
            {
                throw new IndexOutOfRangeException("Not enough data to read!");
            }
        }

        public byte ReadByte()
        {
            EnsureCapacity(1);
            byte result = this.Buffer[this.Position];
            this.Position++;
            return result;
        }

        public ReadOnlySpan<byte> ReadBytes(int count)
        {
            ReadOnlySpan<byte> result = this.Buffer.Slice(this.Position, count);
            this.Position += count;
            return result;
        }

        public bool ReadBoolean()
        {
            EnsureCapacity(1);
            bool result = this.Buffer[this.Position] != 0;
            this.Position++;
            return result;
        }

        public int ReadInt7Encoded()
        {
            int result = 0;
            int shift = 0;
            byte intermByte;
            do
            {
                EnsureCapacity(1);
                intermByte = this.Buffer[this.Position];
                this.Position++;
                result |= (intermByte & 0x7F) << shift;
                shift += 7;
                if (shift > 35)
                {
                    throw new FormatException("Invalid 7-bit encoded integer format!");
                }
            } while ((intermByte & 0x80) != 0);
            
            return result;
        }

        public short ReadInt16()
        {
            EnsureCapacity(2);
            short result = BinaryPrimitives.ReadInt16LittleEndian(this.Buffer.Slice(this.Position));
            this.Position += 2;
            return result;
        }

        public int ReadInt32()
        {
            EnsureCapacity(4);
            int result = BinaryPrimitives.ReadInt32LittleEndian(this.Buffer.Slice(this.Position));
            this.Position += 4;
            return result;
        }

        public long ReadInt64()
        {
            EnsureCapacity(8);
            long result = BinaryPrimitives.ReadInt64LittleEndian(this.Buffer.Slice(this.Position));
            this.Position += 8;
            return result;
        }

        public string ReadString()
        {
            int length = ReadInt7Encoded();

            if (length < 0 || length > 1024 * 1024)
            {
                throw new InvalidDataException($"String length {length} is suspicious!");
            }
            EnsureCapacity(length);
            string result = Encoding.UTF8.GetString(Buffer.Slice(this.Position, length));
            this.Position += length;
            return result;
        }
    }
}
