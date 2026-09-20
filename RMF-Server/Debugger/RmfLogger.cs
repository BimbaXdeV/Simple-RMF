using Microsoft.Extensions.Logging;
using RMF_Server.Logic;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace RMF_Server.Debugger
{
    internal class RmfLogger : ILogger
    {
        private readonly IThemeManager _themeManager;
        private readonly IConsoleSynchronizer _consoleSync;
        private readonly bool _wordWrapIndent;

        private static readonly Lock _lengthLock = new();
        private readonly string _categoryName;
        private readonly ConcurrentQueue<string> _queue;
        private const int _defaultConsoleWidth = 80;
        private const int _reservedConsoleWidth = 2;

        public static int MaxCategoryNameLength { get; private set; }
        public const int FixedHeaderLength = 20;

        public RmfLogger(
            string categoryName,
            ConcurrentQueue<string> queue,
            IThemeManager themeManager,
            IConsoleSynchronizer consoleSync,
            bool wordWrapIndent = false
        )
        {
            _themeManager = themeManager;
            _consoleSync = consoleSync;
            _wordWrapIndent = wordWrapIndent;

            int lastDotIndex = categoryName.LastIndexOf('.');
            _categoryName = lastDotIndex >= 0 ? categoryName.Substring(lastDotIndex + 1) : categoryName;
            _queue = queue;

            lock (_lengthLock)
            {
                MaxCategoryNameLength = Math.Max(MaxCategoryNameLength, _categoryName.Length);
            }
        }

        private static int GetVisibleHeaderLength()
        {
            return FixedHeaderLength + MaxCategoryNameLength;
        }

        private static int GetVisibleMessageLength(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return 0;
            }

            int visibleLength = 0;
            bool isInAnsiSequence = false;

            for (int i = 0; i < message.Length; i++)
            {
                char c = message[i];
                if (c == '\x1b')
                {
                    isInAnsiSequence = true;
                }
                if (isInAnsiSequence)
                {
                    if (char.IsAsciiLetter(c))
                    {
                        isInAnsiSequence = false;
                    }
                    continue;
                }
                visibleLength++;
            }
            return visibleLength;
        }

        private static string IndentLineBreaks(string message, int indentLevel)
        {
            int consoleWidth = Console.WindowWidth > 0 ? Console.WindowWidth : _defaultConsoleWidth;
            int maxTextWidth = consoleWidth - indentLevel - _reservedConsoleWidth;
            if (maxTextWidth <= 0 || message.Length <= maxTextWidth)
            {
                return message;
            }

            string[] words = message.Split(' ');
            StringBuilder sb = new();
            int currentLineLength = 0;

            for (int i = 0; i < words.Length; i++)
            {
                string word = words[i];
                int visibleWordLength = GetVisibleMessageLength(word);
                if (currentLineLength + visibleWordLength + 1 > maxTextWidth)
                {
                    sb.AppendLine();
                    sb.Append(new string(' ', indentLevel));
                    currentLineLength = 0;
                }
                else if (i > 0)
                {
                    sb.Append(' ');
                    currentLineLength++;
                }
                sb.Append(word);
                currentLineLength += visibleWordLength;
            }
            return sb.ToString();
        }

        private string Format(string message, LogLevel logLevel)
        {
            string datetimeStr = $"[ {DateTime.Now.ToString(RmfConstants.TimeSpanFormatHms)} ]";
            string logLevelStr = "(" + logLevel.ToString().First() + ")";
            string categoryStr = _categoryName.PadRight(MaxCategoryNameLength);

            ThemeColor logColor = _themeManager.GetColor("Logging" + logLevel.ToString());
            string formattedLog = logLevel == LogLevel.Information || logLevel == LogLevel.Debug || logLevel == LogLevel.Trace
            ? $"{logColor}{datetimeStr} {logLevelStr} {categoryStr}{ThemeColor.AnsiReset} : {message}"  // The base console output levels color only the left part of the metadata;
                : $"{logColor}{datetimeStr} {logLevelStr} {categoryStr} : {message}{ThemeColor.AnsiReset}"; // Other important alerts regarding unpredictable behavior are fully highlighted in color

            // If you don`t like this specific solution, you can use this declaration instead:
            // [*] string formattedLog = $"{logColorFormat}{datetimeStr} {logLevelStr} {categoryStr} : {message}{Colorist.ResetColor()}";
            return formattedLog;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            string message = formatter(state, exception);

            // Default to the original message if logLevel is None
            // Formatting and adaptive line breaks are available only for specific logging levels.
            // [!] If you wish to create custom extensions with unique functionality, use outputs
            // that explicitly specify "LogLevel.None" to avoid unnecessary formatting and line breaks
            string finalLog = message;
            if (logLevel != LogLevel.None)
            {
                string processedMessage = _wordWrapIndent
                ? IndentLineBreaks(message, GetVisibleHeaderLength())
                : message;

                finalLog = Format(processedMessage, logLevel);
            }

            if (_consoleSync.IsLoggingRunning)
            {
                _queue.Enqueue(finalLog);
            }
            else
            {
                Console.WriteLine(finalLog);
            }
        }

        public bool IsEnabled(LogLevel logLevel) => true;
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    }
}
