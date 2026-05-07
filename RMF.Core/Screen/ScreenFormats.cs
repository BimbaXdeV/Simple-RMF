using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF.Core.Screen
{
    public enum ScreenFormats : byte
    {
        Raw = 0,   // Use ONLY for very small dirty rectangles, otherwise you may clog up your network bandwidth
        Jpeg = 1,  // Used by default to capture the client screen
        WebP = 2,  // It can be used for higher performance and FPS overclocking when streaming, but it will require more power
        Png = 3    // Do not use for hot paths such as streaming: one such screenshot will take up a large part of the buffer
    }
}
