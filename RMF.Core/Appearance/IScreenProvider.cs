using RMF.Core.Screen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF.Core.Appearance
{
    public interface IScreenProvider
    {
        public CapturedFrame? Capture(ScreenFormats format, byte quality, int frameUpdateRate = 0);
    }
}
