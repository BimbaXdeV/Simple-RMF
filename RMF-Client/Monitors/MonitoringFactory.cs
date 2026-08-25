using RMF.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Client.Monitors
{
    internal class MonitoringFactory : IMonitoringFactory
    {
        private BaseMonitor? _monitor;

        public void CheckForUpdates()
        {
            // The denser the forest... If else, if else :D
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && this._monitor?.GetType() != typeof(WindowsMonitor))
            {
                this._monitor = new WindowsMonitor();
                return;
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && this._monitor?.GetType() != typeof(LinuxMonitor))
            {
                this._monitor = new LinuxMonitor();
                return;
            }
        }
        public IHardwareMonitor? GetActualMonitor(bool updateIfNullable = false)
        {
            if (updateIfNullable && this._monitor == null)
            {
                CheckForUpdates();  // If you are writing a looping periodic checker, you do not need this call
            }
            return this._monitor;
        }
    }
}
