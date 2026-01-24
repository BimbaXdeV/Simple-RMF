using RMF_Server.Storage;
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

        // Output colors and settings
        public static byte[] DatetimeColorRGB = { 193, 255, 128 };
        public static byte[] WarningColorRGB = { 255, 187, 51 };
        public static byte[] ErrorColorRGB = { 255, 94, 94 };
        public static string? DefaultLogEnding = "";
        public static char ConsoleSeparator = '-';
        public static int ConsoleSeparatorLength = 32;

        // Logging queue and executor control
        private static readonly ConcurrentQueue<string> LogQueue = [];
        private static bool IsExecutorRunning = false;
        public static bool IsAdminTyping = false;

        private static int GetMaxMethodNameLength()
        {
            var methodNames = typeof(Logging).GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(m => m.ReturnType == typeof(void) &&
                            m.GetParameters().Length == 1 &&
                            m.GetParameters()[0].ParameterType == typeof(string))
                .Select(m => m.Name.Length);
            return methodNames.Any() ? methodNames.Max() : 10;
        }

        // The logs will not be in the sump until the executor is started
        private static void TryLogEnqueue(string message)
        {
            if (IsExecutorRunning)
            {
                LogQueue.Enqueue(message);
            }
            else
            {
                Console.WriteLine(message);
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
            catch (TaskCanceledException)
            {
            }
            finally
            {
                IsExecutorRunning = false;
            }
        }

        // All types of logs
        public static void Output(string message)
        {
            TryLogEnqueue($"{Colorist.ColoredFilterRGB(DatetimeColorRGB[0], DatetimeColorRGB[1], DatetimeColorRGB[2])}[ {DateTime.Now.ToString()} ] {String.Format($"{{0,-{MaxMethodNameLength}}}", System.Reflection.MethodBase.GetCurrentMethod()?.Name.ToUpper() ?? "UNKNOWN")} : {Colorist.ResetColor()}{message}{DefaultLogEnding}");
        }

        public static void Warning(string message)
        {
            TryLogEnqueue($"{Colorist.ColoredFilterRGB(WarningColorRGB[0], WarningColorRGB[1], WarningColorRGB[2])}[ {DateTime.Now.ToString()} ] {String.Format($"{{0,-{MaxMethodNameLength}}}", System.Reflection.MethodBase.GetCurrentMethod()?.Name.ToUpper() ?? "UNKNOWN")} : {message}{DefaultLogEnding}{Colorist.ResetColor()}");
        }

        public static void Error(string message)
        {
            TryLogEnqueue($"{Colorist.ColoredFilterRGB(ErrorColorRGB[0], ErrorColorRGB[1], ErrorColorRGB[2])}[ {DateTime.Now.ToString()} ] {String.Format($"{{0,-{MaxMethodNameLength}}}", System.Reflection.MethodBase.GetCurrentMethod()?.Name.ToUpper() ?? "UNKNOWN")} : {message}{DefaultLogEnding}{Colorist.ResetColor()}");
        }

        public static void Separator()
        {
            TryLogEnqueue(string.Join("", Enumerable.Repeat(ConsoleSeparator.ToString(), ConsoleSeparatorLength)));
        }

        public static void ClearConsole()
        {
            TryLogEnqueue("\u001b[2J\u001b[H");
        }
    }
}
