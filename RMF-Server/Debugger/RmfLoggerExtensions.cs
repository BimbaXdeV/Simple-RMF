using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.Debugger
{
    internal static class RmfLoggerExtensions
    {
        public const string ClearConsoleCommand = "<CLEAR_CONSOLE>";

        public static void ClearConsole(this ILogger logger)
        {
            logger.Log(LogLevel.None, ClearConsoleCommand);
        }
    }
}
