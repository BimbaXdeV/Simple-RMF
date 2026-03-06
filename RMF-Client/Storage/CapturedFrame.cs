using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Client.Storage
{
    internal class CapturedFrame
    {
        public byte[]? Buffer;
        public int Length;

        public void Release()
        {
            if (Buffer != null)
            {
                ArrayPool<byte>.Shared.Return(this.Buffer);
            }
        }
    }
}
