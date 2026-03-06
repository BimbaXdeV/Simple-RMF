using RMF_Client.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Client.Capture
{
    internal interface IScreenCapturer
    {
        public CapturedFrame? Capture(ScreenFormats format, byte quality);
    }
}
