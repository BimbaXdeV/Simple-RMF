using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RMF_Server.Configurations;
using RMF_Server.Logic;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RMF_Server.Debugger
{
    internal class RmfLoggerProvider : BackgroundService, ILoggerProvider
    {
        private readonly IThemeManager _themeManager;
        private readonly IConsoleSynchronizer _consoleSync;
        private readonly LoggingConfig _loggingConfig;

        private readonly ConcurrentQueue<string> _logQueue;
        private readonly string[] _history;
        private bool _isExecutorRunning;

        private readonly Regex _ansiRegex;

        public RmfLoggerProvider(
            IThemeManager themeManager,
            IConsoleSynchronizer consoleSync,
            LoggingConfig loggingConfig
        )
        {
            this._themeManager = themeManager;
            this._consoleSync = consoleSync;
            this._loggingConfig = loggingConfig;

            this._logQueue = new ConcurrentQueue<string>();
            this._history = new string[this._loggingConfig.LoggingHistoryLength];
            this._isExecutorRunning = false;

            this._ansiRegex = new(@"\x1B\[[0-9;]*[a-zA-Z]", RegexOptions.Compiled);
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new RmfLogger(
                categoryName,
                this._logQueue,
                this._themeManager,
                this._consoleSync
            );
        }

        protected override async Task ExecuteAsync(CancellationToken token)
        {
            if (this._isExecutorRunning)
            {
                Console.WriteLine("The logging executor has already been launched previously, a duplicate cannot be started");
                return;
            }

            this._isExecutorRunning = true;
            this._consoleSync.IsLoggingRunning = true;
            try
            {
                // If the loggers have managed to dump a zillion logs into the queue,
                // it won`t hand control over to the DI container builder until it has output them all
                await Task.Yield();
                while (!token.IsCancellationRequested || !this._logQueue.IsEmpty)
                {
                    if (!this._consoleSync.IsAdminTyping && this._logQueue.TryDequeue(out string? log))
                    {
                        // Just a standard logger output. Ya, completely standard...
                        Console.WriteLine(log);
                    }
                    else
                    {
                        try
                        {
                            await Task.Delay(this._loggingConfig.LoggingHandlerDelayMsecs, token);
                        }
                        catch
                        {
                        }
                    }
                }
            }
            finally
            {
                this._isExecutorRunning = false;
                this._consoleSync.IsLoggingRunning = false;

                Console.WriteLine("Logging output executor has been stopped, subsequent logs will be output out of order");
            }
        }

        private void SaveBackup()
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

                string path = PathResolver.GetResolvedPath(
                    this._loggingConfig.LoggingFilePath,
                    fileName: "rmf-server",
                    fileFormat: "log"
                );

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

        public override void Dispose()
        {
            if (this._loggingConfig.EnableLogSaving)
            {
                SaveBackup();
            }
            base.Dispose();
        }
    }
}
