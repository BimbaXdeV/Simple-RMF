using RMF_Server.Logic;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.Debugger
{
    internal static class Logging
    {
        // Inilialization things
        private static readonly int MaxMethodNameLength = GetMaxMethodNameLength();
        public static readonly int LogHeaderLength = MaxMethodNameLength + 27;  // "[ {datetime} ] {methodname} : ".Length
        public static readonly string ServerLogo = @"
 .|'''.|   ||                      '||             '||''|.   '||    ||' '||''''| 
 ||..  '  ...  .. .. ..   ... ...   ||    ....      ||   ||   |||  |||   ||  .   
  ''|||.   ||   || || ||   ||'  ||  ||  .|...||     ||''|'    |'|..'||   ||''|   
.     '||  ||   || || ||   ||    |  ||  ||          ||   |.   | '|' ||   ||      
|'....|'  .||. .|| || ||.  ||...'  .||.  '|...'    .||.  '|' .|. | .||. .||.     
                           ||                                                    
                          ''''                                                   
";

        // Output colors and settings
        public static byte[] DatetimeColorRGB = { 193, 255, 128 };
        public static byte[] WarningColorRGB = { 255, 187, 51 };
        public static byte[] ErrorColorRGB = { 255, 94, 94 };
        public static string? DefaultLogEnding = "";
        public static char ConsoleSeparator = '-';
        public static int ConsoleSeparatorLength = 50;

        // Circular logging buffer
        private static string[] History = new string[100];
        private static int NextHistoryIndex = 0;

        // Logging queue and executor control
        private static readonly ConcurrentQueue<string> LogQueue = [];
        private static bool IsExecutorRunning = false;
        public static bool IsAdminTyping = false;

        private static int GetMaxMethodNameLength()
        {
            var methodNames = typeof(Logging).GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(m => m.ReturnType == typeof(void) &&
                            m.GetParameters().Length == 2 &&
                            m.GetParameters()[0].ParameterType == typeof(string) &&
                            m.GetParameters()[1].ParameterType == typeof(bool))
                .Select(m => m.Name.Length);
            return methodNames.Any() ? methodNames.Max() : 10;
        }

        private static void AddToHistory(string message)
        {
            History[NextHistoryIndex] = message;
            NextHistoryIndex = (NextHistoryIndex + 1) % History.Length;
            if (NextHistoryIndex == 0)
            {
                Logging.Output("The log history buffer is full, older logs will be overwritten", toHistory: false);
            }
        }

        // The logs will not be in the sump until the executor is started
        private static void TryLogEnqueue(string message, bool toHistory)
        {
            if (IsExecutorRunning)
            {
                LogQueue.Enqueue(message);
            }
            else
            {
                Console.WriteLine(message);
            }

            if (toHistory)
            {
                AddToHistory(message);
            }
        }

        public static async Task RunExecutor(CancellationToken token)
        {
            if (IsExecutorRunning)
            {
                Logging.Warning("The logging executor has already been launched previously, a duplicate cannot be started");
                return;
            }

            IsExecutorRunning = true;
            try
            {
                while (!token.IsCancellationRequested)
                {
                    if (!IsAdminTyping && LogQueue.TryDequeue(out string? log))
                    {
                        Console.WriteLine(log);
                    }
                    else
                    {
                        await Task.Delay(ConfigurationManager.LoggingHandlerDelayMsecs);
                    }
                }
            }
            finally
            {
                IsExecutorRunning = false;
            }
        }

        // All types of logs
        public static void Output(string message, bool toHistory = true)
        {
            TryLogEnqueue($"{Colorist.ColoredFilterRGB(DatetimeColorRGB[0], DatetimeColorRGB[1], DatetimeColorRGB[2])}[ {DateTime.Now} ] {String.Format($"{{0,-{MaxMethodNameLength}}}", MethodBase.GetCurrentMethod()?.Name.ToUpper() ?? "U")} : {Colorist.ResetColor()}{message}{DefaultLogEnding}", toHistory);
        }

        public static void Warning(string message, bool toHistory = true)
        {
            TryLogEnqueue($"{Colorist.ColoredFilterRGB(WarningColorRGB[0], WarningColorRGB[1], WarningColorRGB[2])}[ {DateTime.Now} ] {String.Format($"{{0,-{MaxMethodNameLength}}}", MethodBase.GetCurrentMethod()?.Name.ToUpper() ?? "U")} : {message}{DefaultLogEnding}{Colorist.ResetColor()}", toHistory);
        }

        public static void Error(string message, bool toHistory = true)
        {
            TryLogEnqueue($"{Colorist.ColoredFilterRGB(ErrorColorRGB[0], ErrorColorRGB[1], ErrorColorRGB[2])}[ {DateTime.Now} ] {String.Format($"{{0,-{MaxMethodNameLength}}}", MethodBase.GetCurrentMethod()?.Name.ToUpper() ?? "U")} : {message}{DefaultLogEnding}{Colorist.ResetColor()}", toHistory);
        }

        public static void Message(string message, int leftOffset = 0, bool toHistory = true)
        {
            leftOffset = Math.Max(0, leftOffset);
            if (leftOffset > 0)
            {
                message = $"{new string(' ', leftOffset)}{message}";
            }
            TryLogEnqueue($"{message}", toHistory);
        }

        public static void Separator()
        {
            TryLogEnqueue(string.Join("", Enumerable.Repeat(ConsoleSeparator.ToString(), ConsoleSeparatorLength)), false);
        }

        public static void ClearConsole()
        {
            TryLogEnqueue("\u001b[2J\u001b[H", false);
        }
    }
}
