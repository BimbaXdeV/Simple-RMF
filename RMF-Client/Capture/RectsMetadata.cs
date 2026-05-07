using RMF.Core.Interfaces;
using Silk.NET.Maths;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Client.Capture
{
    public readonly record struct RectsMetadata : IReleasable
    {
        public byte[] Data { get; init; }
        public int Count { get; init; }

        public RectsMetadata()
        {
            this.Data = [];
            this.Count = 0;
        }

        public RectsMetadata(byte[] data, int count)
        {
            this.Data = data;
            this.Count = count;
        }

        public unsafe Box2D<int> this[int index]
        {
            get
            {
                if (index < 0 || index >= Count || this.Data == null)
                {
                    throw new IndexOutOfRangeException();
                }

                fixed (byte* srcPtr = this.Data)
                {
                    return ((Box2D<int>*)srcPtr)[index];
                }
            }
        }

        public void Release()
        {
            if (this.Data != null && this.Count > 0)
            {
                ArrayPool<byte>.Shared.Return(this.Data);
            }
        }
    }
}
