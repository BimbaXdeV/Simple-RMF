using RMF.Core.Interfaces;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF.Core.Screen
{
    public readonly record struct CapturedFrame : IReleasable
    {
        public readonly ScreenPatch[] Rects;
        public readonly short RectsCount;
        public readonly ScreenFormats Format;
        public readonly bool IsFullFrame;

        public CapturedFrame(ScreenPatch[] rects, short rectsCount, ScreenFormats format, bool isFullFrame)
        {
            this.Rects = rects;
            this.RectsCount = rectsCount;
            this.Format = format;
            this.IsFullFrame = isFullFrame;
        }

        public void Release()
        {
            if (this.Rects == null)
            {
                return;
            }

            for (int i = 0; i < this.RectsCount; i++)
            {
                if (this.Rects[i].Data != null)
                {
                    ArrayPool<byte>.Shared.Return(this.Rects[i].Data);
                }
            }

            if (this.IsFullFrame)
            {
                ArrayPool<ScreenPatch>.Shared.Return(this.Rects);
            }
        }
    }
}
