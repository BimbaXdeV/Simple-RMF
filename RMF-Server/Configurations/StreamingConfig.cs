using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.Configurations
{
    internal class StreamingConfig
    {
        public int ScreenshotFrameFormat;
        public int ScreenshotQualityPercentage;
        public int StreamingFrameFormat;
        public int StreamingQualityPercentage;
        public int StreamingFrameUpdateRate;
        public int StreamingTargetFPS;
        public bool EnableStreamingStatsOverlay;
    }
}
