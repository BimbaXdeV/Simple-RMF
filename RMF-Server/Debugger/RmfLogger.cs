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
        private readonly IThemeManager? _themeManager;

        private readonly string _categoryName;
        private readonly ConcurrentQueue<string> _queue;

        public RmfLogger(string categoryName, ConcurrentQueue<string> queue, IThemeManager? themeManager)
        {
            this._themeManager = themeManager;
            this._categoryName = categoryName;
            this._queue = queue;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            string message = formatter(state, exception);

            if (logLevel == LogLevel.None)
            {
                this._queue.Enqueue(message);
                return;
            }

            string formattedLog = $"[ {DateTime.Now:HH:mm:ss} ] ({logLevel}) {string.Format("{0,-20}", _categoryName)} : {message}";
            this._queue.Enqueue(formattedLog);
        }

        public bool IsEnabled(LogLevel logLevel) => true;
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    }
}
