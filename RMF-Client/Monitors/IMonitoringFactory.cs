using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Client.Monitors
{
    internal interface IMonitoringFactory
    {
        void CheckForUpdates();
        IHardwareMonitor? GetActualMonitor(bool updateIfNullable = false);
    }
}
