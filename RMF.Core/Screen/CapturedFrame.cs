using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF.Core.Screen
{
    public readonly struct CapturedFrame
    {
        public readonly byte[]? Buffer;
        public readonly int Length;
        public readonly int Width;
        public readonly int Height;
        public readonly ScreenFormats Format;

        public CapturedFrame(byte[]? buffer, int length, int width, int height, ScreenFormats format)
        {
            this.Buffer = buffer;
            this.Length = length;
            this.Width = width;
            this.Height = height;
            this.Format = format;
        }
    }
}
