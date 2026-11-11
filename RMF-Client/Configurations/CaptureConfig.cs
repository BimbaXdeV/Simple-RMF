using RMF.Core.Loaders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Client.Configurations
{
    internal class CaptureConfig : RmfConfig
    {
        public int MaxProcessorCores = Environment.ProcessorCount / 2;
        public int MetricsUpdateRate = 60;

        public override bool ValidateFields(out string? exceptionMessage)
        {
            exceptionMessage = null;
            if (MaxProcessorCores <= 0 || MaxProcessorCores > Environment.ProcessorCount)
            {
                exceptionMessage = $"MaxProcessorCores must be between 1 and {Environment.ProcessorCount}, where \"{Environment.ProcessorCount}\" is the number of cores in your processor";
                return false;
            }
            if (MetricsUpdateRate <= 0)
            {
                exceptionMessage = "MetricsUpdateRate must be greater than 0";
                return false;
            }
            return true;
        }

        public override void LoadFields(IReadOnlyDictionary<string, string> configDict)
        {
            ApplyValue<int>(configDict, nameof(MaxProcessorCores), v => MaxProcessorCores = v);
            ApplyValue<int>(configDict, nameof(MetricsUpdateRate), v => MetricsUpdateRate = v);
        }
    }
}
