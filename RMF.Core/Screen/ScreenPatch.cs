using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF.Core.Screen
{
    public readonly record struct ScreenPatch
    {
        public byte[] Source { get; init; }
        public int X { get; init; }
        public int Y { get; init; }
        public int Width { get; init; }
        public int Height { get; init; }

        public ScreenPatch()
        {
            this.Source = [];
            this.X = 0;
            this.Y = 0;
            this.Width = 0;
            this.Height = 0;
        }

        public ScreenPatch(byte[] source, int width, int height)
        {
            this.Source = source;
            this.X = 0;
            this.Y = 0;
            this.Width = width;
            this.Height = height;
        }

        public ScreenPatch(byte[] source, int x, int y, int width, int height)
        {
            this.Source = source;
            this.X = x;
            this.Y = y;
            this.Width = width;
            this.Height = height;
        }
    }
}
