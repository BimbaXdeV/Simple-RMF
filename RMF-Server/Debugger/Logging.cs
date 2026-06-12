using RMF_Server.Logic;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
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
        public static string? DefaultLogEnding = "";
        public static char ConsoleSeparator = '-';
        public static int ConsoleSeparatorLength = 50;

        // Circular logging buffer
        private static string[]? History;
        private static int NextHistoryIndex;

        // Logging queue and executor control
        private static readonly ConcurrentQueue<string> LogQueue = [];
        private static bool IsExecutorRunning = false;
        public static bool IsAdminTyping = false;

        // Backup utils
        private static readonly Regex AnsiRegex = new(@"\x1B\[[0-9;]*[a-zA-Z]", RegexOptions.Compiled);

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
            if (History == null)
            {
                return;  // Well, maybe this story isn't really needed...
            }

            History[NextHistoryIndex] = message;
            NextHistoryIndex = (NextHistoryIndex + 1) % History.Length;
            if (NextHistoryIndex == 0)
            {
                Output("The log history buffer is full, older logs will be overwritten", toHistory: false);
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

        public static void CreateHistory(int bufferLength)
        {
            History = new string[bufferLength];
            NextHistoryIndex = 0;
        }

        public static async Task RunExecutor(CancellationToken token)
        {
            if (IsExecutorRunning)
            {
                Warning("The logging executor has already been launched previously, a duplicate cannot be started");
                return;
            }

            IsExecutorRunning = true;
            try
            {
                while (!token.IsCancellationRequested || !LogQueue.IsEmpty)
                {
                    if (!IsAdminTyping && LogQueue.TryDequeue(out string? log))
                    {
                        Console.WriteLine(log);
                    }
                    else
                    {
                        try
                        {
                            await Task.Delay(ConfigurationManager.LoggingHandlerDelayMsecs, CancellationToken.None);
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
            }
            finally
            {
                IsExecutorRunning = false;
                Output("Logging output executor has been stopped, subsequent logs will be output out of order");
            }
        }

        // All types of logs
        public static void Output(string message, bool toHistory = true)
        {
            TryLogEnqueue($"{Colorist.ColoredFilterRGB(ThemeManager.OutputDatetime[0], ThemeManager.OutputDatetime[1], ThemeManager.OutputDatetime[2])}[ {DateTime.Now:yyyy-MM-dd HH:mm:ss} ] {string.Format($"{{0,-{MaxMethodNameLength}}}", MethodBase.GetCurrentMethod()?.Name.ToUpper() ?? "U")} : {Colorist.ResetColor()}{message}{DefaultLogEnding}", toHistory);
        }

        public static void Warning(string message, bool toHistory = true)
        {
            TryLogEnqueue($"{Colorist.ColoredFilterRGB(ThemeManager.WarningLog[0], ThemeManager.WarningLog[1], ThemeManager.WarningLog[2])}[ {DateTime.Now:yyyy-MM-dd HH:mm:ss} ] {string.Format($"{{0,-{MaxMethodNameLength}}}", MethodBase.GetCurrentMethod()?.Name.ToUpper() ?? "U")} : {message}{DefaultLogEnding}{Colorist.ResetColor()}", toHistory);
        }

        public static void Error(string message, bool toHistory = true)
        {
            TryLogEnqueue($"{Colorist.ColoredFilterRGB(ThemeManager.ErrorLog[0], ThemeManager.ErrorLog[1], ThemeManager.ErrorLog[2])}[ {DateTime.Now:yyyy-MM-dd HH:mm:ss} ] {string.Format($"{{0,-{MaxMethodNameLength}}}", MethodBase.GetCurrentMethod()?.Name.ToUpper() ?? "U")} : {message}{DefaultLogEnding}{Colorist.ResetColor()}", toHistory);
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
            string colorPref = Colorist.ColoredFilterRGB(ThemeManager.Separator[0], ThemeManager.Separator[1], ThemeManager.Separator[2]);
            TryLogEnqueue(colorPref + string.Join("", Enumerable.Repeat(ConsoleSeparator.ToString(), ConsoleSeparatorLength)) + Colorist.ResetColor(), false);
        }

        public static void ClearConsole()
        {
            TryLogEnqueue("\u001b[2J\u001b[H", false);
        }

        // Other utils
        public static void SaveBackup(string path, bool appendBelow = false)
        {
            if (History == null || History.Length == 0)
            {
                Output("The log history is empty, nothing to do");
                return;
            }

            try
            {
                string[] validLines = History.Where(l => l != null)
                                             .Select(l => AnsiRegex.Replace(l, string.Empty))
                                             .ToArray();

                if (validLines.Length == 0)
                {
                    Output("The log history contains only nulls, nothing to do");
                    return;
                }

                string? directoryPath = Path.GetDirectoryName(path);
                if (!string.IsNullOrWhiteSpace(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                string backupTitle = $"* Backup from {DateTime.Now:yyyy-MM-dd HH:mm:ss} [{validLines.Length} / {History.Length} lines]:";
                string contentToWrite = backupTitle + Environment.NewLine + string.Join(Environment.NewLine, validLines);
                bool isNewFile = !File.Exists(path);

                if (!isNewFile && appendBelow)
                {
                    File.AppendAllText(path, Environment.NewLine + Environment.NewLine + contentToWrite);
                } 
                else
                {
                    File.WriteAllText(path, contentToWrite);
                }

                // Log rotation if the file exceeds the maximum allowed size after writing the backup
                long currentFileSize = new FileInfo(path).Length;
                long maxAllowedSize = ConfigurationManager.MaxLogFileCapacityMB * 1024 * 1024;

                if (currentFileSize >= maxAllowedSize)
                {
                    string baseFileName = Path.GetFileNameWithoutExtension(path);
                    string archievedFileName = $"{baseFileName}_{DateTime.Now:yyyyMMddHHmmss}.bak";
                    string archievedPath = Path.Combine(directoryPath ?? string.Empty, archievedFileName);

                    File.Move(path, archievedPath, overwrite: true);

                    Output($"The log file has reached maximum capacity and has been archived as: {archievedFileName}");
                }
            }
            catch (Exception ex)
            {
                Warning($"Failed to write log history to file: {ex}");
            }
        }
    }
}
