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

        public override int LoadFields(Dictionary<string, object> configDict)
        {
            int loadedFields = 0;
            ApplyValue<int>(configDict, nameof(MaxProcessorCores), ref loadedFields, v => MaxProcessorCores = v);
            ApplyValue<int>(configDict, nameof(MetricsUpdateRate), ref loadedFields, v => MetricsUpdateRate = v);
            return loadedFields;
        }
    }
}
