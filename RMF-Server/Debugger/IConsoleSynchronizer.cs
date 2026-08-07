using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.Debugger
{
    internal interface IConsoleSynchronizer
    {
        bool IsLoggingRunning { get; set; }
        bool IsAdminTyping { get; set; }
    }
}
