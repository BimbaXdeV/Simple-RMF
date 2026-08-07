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

        private static readonly int _maxLogLevelNameLength = Enum.GetValues<LogLevel>().Max(l => l.ToString().Length);
        private static int _maxCategoryNameLength = 0;
        private static readonly Lock _lengthLock = new();

        private readonly string _categoryName;
        private readonly ConcurrentQueue<string> _queue;

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
                _maxCategoryNameLength = Math.Max(_maxCategoryNameLength, this._categoryName.Length);
            }
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            string message = formatter(state, exception);

            if (logLevel == LogLevel.None)
            {
                this._queue.Enqueue(message);
                return;
            }

            string datetimeStr = $"[ {DateTime.Now:HH:mm:ss} ]";
            string logLevelStr = "(" + logLevel.ToString().PadRight(_maxLogLevelNameLength) + ")";
            string categoryStr = _categoryName.PadRight(_maxCategoryNameLength);

            string logColorFormat = Colorist.GetColoredFilterRGB(this._themeManager.GetColor("Logging" + logLevel.ToString()));
            string formattedLog = logLevel == LogLevel.Information || logLevel == LogLevel.Debug || logLevel == LogLevel.Trace
                ? $"{logColorFormat}{datetimeStr} {logLevelStr} {categoryStr}{Colorist.ResetColor()} : {message}"  // The base console output levels color only the left part of the metadata;
                : $"{logColorFormat}{datetimeStr} {logLevelStr} {categoryStr} : {message}{Colorist.ResetColor()}"; // Other important alerts regarding unpredictable behavior are fully highlighted in color

            // If you don't like this specific solution, you can use this declaration instead:
            // string formattedLog = $"{logColorFormat}{datetimeStr} {logLevelStr} {categoryStr} : {message}{Colorist.ResetColor()}";

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
