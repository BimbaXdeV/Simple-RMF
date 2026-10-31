using RMF.Core.Loaders;
using RMF.Core.Screen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.Configurations
{
    internal class StreamingConfig : RmfConfig
    {
        public string ScreenshotsFilePath = string.Empty;
        public ScreenFormats ScreenshotFrameFormat = ScreenFormats.Raw;
        public int ScreenshotQualityPercentage = 100;
        public ScreenFormats StreamingFrameFormat = ScreenFormats.Raw;
        public int StreamingQualityPercentage = 100;
        public int StreamingFrameUpdateRate = 10;
        public int StreamingTargetFPS = 30;
        public bool EnableStreamingStatsOverlay = false;

        public override bool ValidateFields(out string? exceptionMessage)
        {
            exceptionMessage = null;
            if (ScreenshotQualityPercentage < 1 || ScreenshotQualityPercentage > 100)
            {
                exceptionMessage = "ScreenshotQualityPercentage must be between 1 and 100";
                return false;
            }
            if (StreamingQualityPercentage < 1 || StreamingQualityPercentage > 100)
            {
                exceptionMessage = "StreamingQualityPercentage must be between 1 and 100";
                return false;
            }
            if (StreamingFrameUpdateRate < 0)
            {
                exceptionMessage = "StreamingFrameUpdateRate must be greater than or equal to 0  (0 = only full frames)";
                return false;
            }
            if (StreamingTargetFPS <= 0)
            {
                exceptionMessage = "StreamingTargetFPS must be greater than 0";
                return false;
            }
            return true;
        }

        public override void LoadFields(IReadOnlyDictionary<string, string> configDict)
        {
            ApplyValue<string>(configDict, nameof(ScreenshotsFilePath), v => ScreenshotsFilePath = v);
            ApplyValue<ScreenFormats>(configDict, nameof(ScreenshotFrameFormat), v => ScreenshotFrameFormat = v);
            ApplyValue<int>(configDict, nameof(ScreenshotQualityPercentage), v => ScreenshotQualityPercentage = v);
            ApplyValue<ScreenFormats>(configDict, nameof(StreamingFrameFormat), v => StreamingFrameFormat = v);
            ApplyValue<int>(configDict, nameof(StreamingQualityPercentage), v => StreamingQualityPercentage = v);
            ApplyValue<int>(configDict, nameof(StreamingFrameUpdateRate), v => StreamingFrameUpdateRate = v);
            ApplyValue<int>(configDict, nameof(StreamingTargetFPS), v => StreamingTargetFPS = v);
            ApplyValue<bool>(configDict, nameof(EnableStreamingStatsOverlay), v => EnableStreamingStatsOverlay = v);
        }
    }
}
