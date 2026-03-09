using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Client.Capture
{
    internal class LinuxCapturer : BaseCapturer
    {
        protected override void UpdateScreenMetrics()
        {
            throw new NotImplementedException();
        }

        protected override SKBitmap GetScreenBitmap()
        {
            throw new NotImplementedException();
        }
    }
}
