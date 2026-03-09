using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF.Core.Screen
{
    public class CapturedFrame
    {
        public ScreenFormats Format;
        public int Width;
        public int Height;
        public int Length;
        public byte[]? Buffer;

        public void Release()
        {
            if (Buffer != null)
            {
                ArrayPool<byte>.Shared.Return(this.Buffer);
            }
        }
    }
}
