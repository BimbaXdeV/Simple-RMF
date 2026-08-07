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
        // These are simply command bridges: the core logic for outputting extensions resides in the provider worker.
        // The implementation can be found in class "RmfLoggerProvider"
        public const string LogoCommand = "<LOGO>";
        public const string SeparatorCommand = "<SEPARATOR>";
        public const string ClearConsoleCommand = "<CLEAR_CONSOLE>";

        public static void InsertLogo(this ILogger logger)
        {
            logger.Log(LogLevel.None, LogoCommand);
        }

        public static void InsertSeparator(this ILogger logger)
        {
            logger.Log(LogLevel.None, SeparatorCommand);
        }

        public static void ClearConsole(this ILogger logger)
        {
            logger.Log(LogLevel.None, ClearConsoleCommand);
        }
    }
}
