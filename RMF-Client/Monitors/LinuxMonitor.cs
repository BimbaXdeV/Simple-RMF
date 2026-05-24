using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Client.Monitors
{
    [SupportedOSPlatform("linux")]
    internal class LinuxMonitor : BaseMonitor
    {
        public override string CPUName()
        {
            throw new NotImplementedException();
        }

        public override string GPUName()
        {
            throw new NotImplementedException();
        }

        public override double RAMCapacity()
        {
            throw new NotImplementedException();
        }

        public override double VRAMCapacity()
        {
            throw new NotImplementedException();
        }
    }
}
