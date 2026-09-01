using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.Debugger
{
    internal class RmfLogger : ILogger
    {
        private readonly IThemeManager _themeManager;
        private readonly IConsoleSynchronizer _consoleSync;

        private static readonly Lock _lengthLock = new();

        private readonly string _categoryName;
        private readonly ConcurrentQueue<string> _queue;

        public const int FixedHeaderLength = 20;
        public static int MaxCategoryNameLength { get; private set; }

        public RmfLogger(
            string categoryName,
            ConcurrentQueue<string> queue,
            IThemeManager themeManager,
            IConsoleSynchronizer consoleSync
        )
        {
            this._themeManager = themeManager;
            this._consoleSync = consoleSync;

            int lastDotIndex = categoryName.LastIndexOf('.');
            this._categoryName = lastDotIndex >= 0 ? categoryName.Substring(lastDotIndex + 1) : categoryName;
            this._queue = queue;

            lock (_lengthLock)
            {
                MaxCategoryNameLength = Math.Max(MaxCategoryNameLength, this._categoryName.Length);
            }
        }

        private string Format(string message, LogLevel logLevel)
        {
            string datetimeStr = $"[ {DateTime.Now:HH:mm:ss} ]";
            string logLevelStr = "(" + logLevel.ToString().First() + ")";
            string categoryStr = _categoryName.PadRight(MaxCategoryNameLength);

            ThemeColor logColor = this._themeManager.GetColor("Logging" + logLevel.ToString());
            string formattedLog = logLevel == LogLevel.Information || logLevel == LogLevel.Debug || logLevel == LogLevel.Trace
            ? $"{logColor}{datetimeStr} {logLevelStr} {categoryStr}{ThemeColor.AnsiReset} : {message}"  // The base console output levels color only the left part of the metadata;
                : $"{logColor}{datetimeStr} {logLevelStr} {categoryStr} : {message}{ThemeColor.AnsiReset}"; // Other important alerts regarding unpredictable behavior are fully highlighted in color

            // If you don`t like this specific solution, you can use this declaration instead:
            // string formattedLog = $"{logColorFormat}{datetimeStr} {logLevelStr} {categoryStr} : {message}{Colorist.ResetColor()}";
            return formattedLog;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            string message = formatter(state, exception);

            string formattedLog = logLevel != LogLevel.None ? Format(message, logLevel) : message;
            if (this._consoleSync.IsLoggingRunning)
            {
                this._queue.Enqueue(formattedLog);
            }
            else
            {
                Console.WriteLine(formattedLog);
            }
        }

        public bool IsEnabled(LogLevel logLevel) => true;
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    }
}
