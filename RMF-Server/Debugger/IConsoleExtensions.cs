using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.Debugger
{
    internal interface IConsoleExtensions
    {
        void DrawLogo(ILogger logger);
        void LogSeparator(ILogger logger);
        void LogInitialization(ILogger logger, string category, int loaded, int total);
        void ClearConsole(ILogger logger);
    }
}
