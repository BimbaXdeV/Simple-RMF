using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Client.Monitors
{
    public interface IHardwareMonitor
    {
        string CPUName();
        string GPUName();
        double RAMCapacity();
        double VRAMCapacity();

        string MachineName();
        string Username();
        string OSName();
        string CPUArchitecture();
    }
}
