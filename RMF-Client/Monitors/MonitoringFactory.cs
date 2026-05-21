using RMF.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Client.Monitors
{
    internal static class MonitoringFactory
    {
        private static BaseMonitor? Monitor;

        public static void CheckForUpdates()
        {
            // The denser the forest... If else, if else :D
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && Monitor?.GetType() != typeof(WindowsMonitor))
            {
                Monitor = new WindowsMonitor();
                return;
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && Monitor?.GetType() != typeof(LinuxMonitor))
            {
                Monitor = new LinuxMonitor();
                return;
            }
        }
        public static IHardwareMonitor? GetActualMonitor(bool updateIfNullable = false)
        {
            if (updateIfNullable && Monitor == null)
            {
                CheckForUpdates();  // If you are writing a looping periodic checker, you do not need this call
            }
            return Monitor;
        }
    }
}
