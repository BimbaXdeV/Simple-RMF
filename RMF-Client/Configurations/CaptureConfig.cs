using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Client.Configurations
{
    internal class CaptureConfig
    {
        public int MaxProcessorCores = Environment.ProcessorCount;
        public int MetricsUpdateRate = 60;
    }
}
