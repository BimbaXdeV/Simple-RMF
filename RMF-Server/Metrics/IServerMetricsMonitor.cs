using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.Metrics
{
    internal interface IServerMetricsMonitor
    {
        DateTime ProcessStartTime { get; }
        int CoresCount { get; }

        RmfRamUsage GetRamUsage();
        double GetCpuLoadPercentage();
    }
}
