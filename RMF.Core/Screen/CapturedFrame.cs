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
        public readonly ScreenFormats Format;
        public readonly bool IsFullFrame;

        public CapturedFrame(ScreenPatch[] rects, ScreenFormats format, bool isFullFrame)
        {
            this.Rects = rects;
            this.Format = format;
            this.IsFullFrame = isFullFrame;
        }

        public void Release()
        {
            if (this.Rects == null)
            {
                return;
            }

            for (int i = 0; i < this.Rects.Length; i++)
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
