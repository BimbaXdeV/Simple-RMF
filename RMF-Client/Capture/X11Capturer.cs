using RMF.Core.Screen;
using RMF_Client.Configurations;
using Silk.NET.Maths;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Client.Capture
{
    [SupportedOSPlatform("linux")]
    internal class X11Capturer : BaseCapturer
    {
        public X11Capturer(CaptureConfig captureConfig) : base(captureConfig)
        {
        }

        protected override void Initialize()
        {
            throw new NotImplementedException();
        }

        protected override void UpdateBitmapMetrics()
        {
            throw new NotImplementedException();
        }

        protected override ScreenPatch AcquireFrame()
        {
            throw new NotImplementedException();
        }

        protected override RectsMetadata? AcquireUpdates()
        {
            throw new NotImplementedException();
        }
    }
}
