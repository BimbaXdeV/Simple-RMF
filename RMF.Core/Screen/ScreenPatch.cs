using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF.Core.Screen
{
    public readonly record struct ScreenPatch
    {
        public short X { get; init; }
        public short Y { get; init; }
        public short Width { get; init; }
        public short Height { get; init; }
        public int Length { get; init; }
        public byte[] Data { get; init; }

        public ScreenPatch(byte[] data, int length, short x, short y, short w, short h)
        {
            this.X = x;
            this.Y = y;
            this.Width = w;
            this.Height = h;
            this.Length = length;
            this.Data = data;
        }
    }
}
