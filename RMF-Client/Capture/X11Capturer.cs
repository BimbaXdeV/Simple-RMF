using RMF.Core.Screen;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Client.Capture
{
    internal class X11Capturer : BaseCapturer
    {
        protected override void Initialize()
        {
            throw new NotImplementedException();
        }

        protected override void UpdateBitmapMetrics()
        {
            throw new NotImplementedException();
        }

        protected override void UpdateBitmapFrame()
        {
            throw new NotImplementedException();
        }

        protected override ScreenPatch GetActualFrame()
        {
            throw new NotImplementedException();
        }

        protected override Span<ScreenPatch> GetFrameUpdates()
        {
            throw new NotImplementedException();
        }
    }
}
