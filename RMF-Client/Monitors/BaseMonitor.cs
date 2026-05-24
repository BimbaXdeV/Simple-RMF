using RMF.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Client.Monitors
{
    internal abstract class BaseMonitor : IHardwareMonitor
    {
        public abstract string CPUName();
        public abstract string GPUName();
        public abstract double RAMCapacity();
        public abstract double VRAMCapacity();

        public string MachineName()
        {
            return Environment.MachineName;
        }

        public string Username()
        {
            return Environment.UserName;
        }

        public string OSName()
        {
            return RuntimeInformation.OSDescription;
        }

        public string CPUArchitecture()
        {
            return RuntimeInformation.ProcessArchitecture.ToString();
        }
    }
}
