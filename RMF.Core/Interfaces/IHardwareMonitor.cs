using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF.Core.Interfaces
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
