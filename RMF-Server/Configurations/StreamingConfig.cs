using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.Configurations
{
    internal class StreamingConfig
    {
        public int ScreenshotFrameFormat = 0;
        public int ScreenshotQualityPercentage = 100;
        public int StreamingFrameFormat = 0;
        public int StreamingQualityPercentage = 100;
        public int StreamingFrameUpdateRate = 1;
        public int StreamingTargetFPS = 1;
        public bool EnableStreamingStatsOverlay = false;
    }
}
