using RMF.Core.Interfaces;
using RMF_Server.Configurations;
using RMF_Server.Storage;
using Splat;
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
    internal class Logging : ILoggingEngine
    {
        private readonly IThemeManager? _themeManager;
        private readonly LoggingConfig? _loggingConfig;

        // Inilialization things
        private readonly ushort _maxMethodNameLength;
        public ushort LogHeaderLength { get; private set; }
        public readonly string ServerLogo = @"
 .|'''.|   ||                      '||             '||''|.   '||    ||' '||''''| 
 ||..  '  ...  .. .. ..   ... ...   ||    ....      ||   ||   |||  |||   ||  .   
  ''|||.   ||   || || ||   ||'  ||  ||  .|...||     ||''|'    |'|..'||   ||''|   
.     '||  ||   || || ||   ||    |  ||  ||          ||   |.   | '|' ||   ||      
|'....|'  .||. .|| || ||.  ||...'  .||.  '|...'    .||.  '|' .|. | .||. .||.     
                           ||                                                    
                          ''''                                                   
";

        // Output colors and settings
        public string DefaultLogEnding;
        public char ConsoleSeparator;
        public ushort ConsoleSeparatorLength;

        // Circular logging buffer
        private string[]? _history;
        private int _nextHistoryIndex;

        // Logging queue and executor control
        private readonly ConcurrentQueue<string> _logQueue;
        private bool _isExecutorRunning;
        private bool _isAdminTyping;

        // Backup utils
        private readonly Regex _ansiRegex = new(@"\x1B\[[0-9;]*[a-zA-Z]", RegexOptions.Compiled);

        public Logging(
            IThemeManager? themeManager = null,
            LoggingConfig? loggingConfig = null,
            string? defaultLogEnding = null,
            char consoleSeparator = char.MinValue,
            ushort consoleSeparatorLength = 0
        )
        {
            this._themeManager = themeManager;
            this._loggingConfig = loggingConfig;

            this._maxMethodNameLength = GetMaxMethodNameLength();
            this.LogHeaderLength = (ushort)(_maxMethodNameLength + 27);  // "[ {datetime} ] {methodname} : ".Length

            this.DefaultLogEnding = defaultLogEnding ?? string.Empty;
            this.ConsoleSeparator = consoleSeparator != char.MinValue ? consoleSeparator : '-';
            this.ConsoleSeparatorLength = consoleSeparatorLength > 0 ? consoleSeparatorLength : (ushort)(LogHeaderLength + 16);

            this._logQueue = new ConcurrentQueue<string>();
            this._isExecutorRunning = false;
            this._isAdminTyping = false;
        }

        private static ushort GetMaxMethodNameLength()
        {
            var methodNames = typeof(Logging).GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.ReturnType == typeof(void) &&
                            m.GetParameters().Length == 2 &&
                            m.GetParameters()[0].ParameterType == typeof(string) &&
                            m.GetParameters()[1].ParameterType == typeof(bool))
                .Select(m => m.Name.Length);
            return (ushort)(methodNames.Any() ? methodNames.Max() : 7);
        }

        // The logs will not be in the sump until the executor is started
        private void TryLogEnqueue(string message, bool toHistory)
        {
            if (_isExecutorRunning)
            {
                _logQueue.Enqueue(message);
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

        public void CreateHistory(int bufferLength)
        {
            _history = new string[bufferLength];
            _nextHistoryIndex = 0;
        }

        private void AddToHistory(string message)
        {
            if (_history == null)
            {
                return;  // Well, maybe this story isn't really needed...
            }

            _history[_nextHistoryIndex] = message;
            _nextHistoryIndex = (_nextHistoryIndex + 1) % _history.Length;
            if (_nextHistoryIndex == 0)
            {
                Output("The log history buffer is full, older logs will be overwritten", toHistory: false);
            }
        }

        public bool GetAdminTyping()
        {
            return this._isAdminTyping;
        }

        public void SetAdminTyping(bool status)
        {
            this._isAdminTyping = status;
        }

        public async Task RunExecutor(CancellationToken token)
        {
            if (_isExecutorRunning)
            {
                Warning("The logging executor has already been launched previously, a duplicate cannot be started");
                return;
            }

            _isExecutorRunning = true;
            try
            {
                while (!token.IsCancellationRequested || !_logQueue.IsEmpty)
                {
                    if (!_isAdminTyping && _logQueue.TryDequeue(out string? log))
                    {
                        Console.WriteLine(log);
                    }
                    else
                    {
                        try
                        {
                            await Task.Delay(this._loggingConfig?.LoggingHandlerDelayMsecs ?? 250, CancellationToken.None);
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
            }
            finally
            {
                _isExecutorRunning = false;
                Output("Logging output executor has been stopped, subsequent logs will be output out of order");
            }
        }

        // All types of logs
        public void Output(string message, bool toHistory = true)
        {
            ThemeColor color = this._themeManager?.GetColor("OutputDatetime") ?? new ThemeColor(255, 255, 255, 255);
            TryLogEnqueue($"{Colorist.ColoredFilterRGB(color.R, color.G, color.B)}[ {DateTime.Now:yyyy-MM-dd HH:mm:ss} ] {string.Format($"{{0,-{_maxMethodNameLength}}}", MethodBase.GetCurrentMethod()?.Name.ToUpper() ?? "U")} : {Colorist.ResetColor()}{message}{DefaultLogEnding}", toHistory);
        }

        public void Warning(string message, bool toHistory = true)
        {
            ThemeColor color = this._themeManager?.GetColor("WarningLog") ?? new ThemeColor(255, 255, 255, 255);
            TryLogEnqueue($"{Colorist.ColoredFilterRGB(color.R, color.G, color.B)}[ {DateTime.Now:yyyy-MM-dd HH:mm:ss} ] {string.Format($"{{0,-{_maxMethodNameLength}}}", MethodBase.GetCurrentMethod()?.Name.ToUpper() ?? "U")} : {message}{DefaultLogEnding}{Colorist.ResetColor()}", toHistory);
        }

        public void Error(string message, bool toHistory = true)
        {
            ThemeColor color = this._themeManager?.GetColor("ErrorLog") ?? new ThemeColor(255, 255, 255, 255);
            TryLogEnqueue($"{Colorist.ColoredFilterRGB(color.R, color.G, color.B)}[ {DateTime.Now:yyyy-MM-dd HH:mm:ss} ] {string.Format($"{{0,-{_maxMethodNameLength}}}", MethodBase.GetCurrentMethod()?.Name.ToUpper() ?? "U")} : {message}{DefaultLogEnding}{Colorist.ResetColor()}", toHistory);
        }

        public void Message(string message, int leftOffset = 0, bool toHistory = true)
        {
            leftOffset = Math.Max(0, leftOffset);
            if (leftOffset > 0)
            {
                message = $"{new string(' ', leftOffset)}{message}";
            }
            TryLogEnqueue($"{message}", toHistory);
        }

        public void Separator()
        {
            ThemeColor color = this._themeManager?.GetColor("Separator") ?? new ThemeColor(255, 255, 255, 255);
            string colorPref = Colorist.ColoredFilterRGB(color.R, color.G, color.B);
            TryLogEnqueue(colorPref + string.Join("", Enumerable.Repeat(ConsoleSeparator.ToString(), ConsoleSeparatorLength)) + Colorist.ResetColor(), false);
        }

        public void ClearConsole()
        {
            TryLogEnqueue("\u001b[2J\u001b[H", false);
        }

        // Other utils
        public void SaveBackup(string path, bool appendBelow = false)
        {
            if (_history == null || _history.Length == 0)
            {
                Output("The log history is empty, nothing to do");
                return;
            }

            try
            {
                string[] validLines = _history.Where(l => l != null)
                                             .Select(l => _ansiRegex.Replace(l, string.Empty))
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

                string backupTitle = $"* Backup from {DateTime.Now:yyyy-MM-dd HH:mm:ss} [{validLines.Length} / {_history.Length} lines]:";
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
                long maxAllowedSize = (this._loggingConfig?.MaxLogFileCapacityMB ?? 1) * 1024 * 1024;

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
