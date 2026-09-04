using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Client.Capture
{
    internal interface IDirtyRectsCapturer
    {
        // "AcquireUpdates()" is required to capture only the updated areas of the client screen.
        // -------------------------------------------------------------------------------------
        // - The method should not return any pixel data. The X structure stores only the metrics of the changed screen areas, which are then
        //   used by the engine to obtain raw bytes.
        // -------------------------------------------------------------------------------------
        // WARNING: Use the above array leases to store areas to avoid GC (Garbage Collector) load issues.
        // NOTICE: There is no need to return the array back, the engine already knows how to do this.
        RectsMetadata? AcquireUpdates();
    }
}
