using Microsoft.Extensions.Logging;
using RMF_Server.Configurations;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RMF_Server.Debugger
{
    internal class RmfLoggerProvider : ILoggerProvider
    {
        private readonly LoggingConfig _loggingConfig;
        private readonly IThemeManager? _themeManager;

        private readonly ConcurrentQueue<string> _logQueue;
        private readonly string[] _history;
        private bool _isExecutorRunning;
        private readonly bool _isAdminTyping;

        private readonly string _logFormat;
        private readonly Regex _ansiRegex;

        private readonly CancellationTokenSource _cts;
        private readonly Task _executorTask;

        public RmfLoggerProvider(int historyLength, LoggingConfig loggingConfig, IThemeManager? themeManager)
        {
            this._loggingConfig = loggingConfig;
            this._themeManager = themeManager;

            this._logQueue = new ConcurrentQueue<string>();
            this._history = new string[historyLength];
            this._isExecutorRunning = false;
            this._isAdminTyping = false;

            this._logFormat = "{colorStart}[ {dateTime} ]{datetimeColorEnd} ({logLevel}){logLevelColorEnd} {categoryName} : {message}{colorEnd}";
            this._ansiRegex = new(@"\x1B\[[0-9;]*[a-zA-Z]", RegexOptions.Compiled);

            this._cts = new CancellationTokenSource();
            this._executorTask = Task.Run(() => RunExecutor(this._cts.Token));
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new RmfLogger(categoryName, this._logQueue, this._themeManager);
        }

        public async Task RunExecutor(CancellationToken token)
        {
            if (this._isExecutorRunning)
            {
                Console.WriteLine("The logging executor has already been launched previously, a duplicate cannot be started");
                return;
            }

            this._isExecutorRunning = true;
            try
            {
                while (!token.IsCancellationRequested || !this._logQueue.IsEmpty)
                {
                    if (!this._isAdminTyping && this._logQueue.TryDequeue(out string? log))
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
                this._isExecutorRunning = false;
                Console.WriteLine("Logging output executor has been stopped, subsequent logs will be output out of order");
            }
        }

        public void Dispose()
        {
            this._cts.Cancel();

            try
            {
                this._executorTask.Wait(TimeSpan.FromSeconds(3));
            }
            catch (AggregateException)
            {
            }

            if (this._loggingConfig.EnableLogSaving)
            {
                SaveBackup("");
            }
        }

        private void SaveBackup(string path)
        {
            if (this._history == null || this._history.Length == 0)
            {
                Console.WriteLine("The log history is empty, nothing to do");
                return;
            }

            try
            {
                string[] validLines = this._history.Where(l => l != null)
                                             .Select(l => this._ansiRegex.Replace(l, string.Empty))
                                             .ToArray();

                if (validLines.Length == 0)
                {
                    Console.WriteLine("The log history contains only nulls, nothing to do");
                    return;
                }

                string? directoryPath = Path.GetDirectoryName(path);
                if (!string.IsNullOrWhiteSpace(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                string backupTitle = $"* Backup from {DateTime.Now:yyyy-MM-dd HH:mm:ss} [{validLines.Length} / {this._history.Length} lines]:";
                string contentToWrite = backupTitle + Environment.NewLine + string.Join(Environment.NewLine, validLines);
                bool isNewFile = !File.Exists(path);

                if (!isNewFile && this._loggingConfig.EnableMultipleBackup)
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

                    Console.WriteLine($"The log file has reached maximum capacity and has been archived as: {archievedFileName}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to write log history to file: {ex}");
            }
        }
    }
}
