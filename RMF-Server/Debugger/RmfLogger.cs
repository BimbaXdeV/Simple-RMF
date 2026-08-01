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

        private readonly string _categoryName;
        private readonly ConcurrentQueue<string> _queue;

        private static int _maxLogLevelNameLength = Enum.GetValues<LogLevel>().Max(l => l.ToString().Length);
        private static int _maxCategoryNameLength = 0;
        private static readonly Lock _lengthLock = new();

        public RmfLogger(string categoryName, ConcurrentQueue<string> queue, IThemeManager themeManager)
        {
            this._themeManager = themeManager;

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

            string logColorFormat = Colorist.ColoredFilterRGB(this._themeManager.GetColor("Logging" + logLevel.ToString()));
            string formattedLog = logLevel == LogLevel.Trace || logLevel == LogLevel.Debug || logLevel == LogLevel.Information
                ? $"{logColorFormat}{datetimeStr} {logLevelStr} {categoryStr}{Colorist.ResetColor()} : {message}"
                : $"{logColorFormat}{datetimeStr} {logLevelStr} {categoryStr} : {message}{Colorist.ResetColor()}";

            //string formattedLog = $"[ {DateTime.Now:HH:mm:ss} ] ({logLevel}) {string.Format("{0,-20}", _categoryName)} : {message}";
            this._queue.Enqueue(formattedLog);
        }

        public bool IsEnabled(LogLevel logLevel) => true;
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    }
}
