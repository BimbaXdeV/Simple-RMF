using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.Debugger
{
    internal enum RmfLoggerPaintType : byte
    {
        None = 0,
        OnlyTime = 1,
        TimeAndLevel = 2,
        OnlyMessage = 3,
        Full = 4
    }
}
