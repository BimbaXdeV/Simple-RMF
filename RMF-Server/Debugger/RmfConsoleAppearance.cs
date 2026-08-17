using Microsoft.Extensions.Logging;
using RMF_Server.Configurations;
using RMF_Server.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.Debugger
{
    internal class RmfConsoleAppearance : IConsoleExtensions
    {
        private readonly IThemeManager _themeManager;
        private readonly LoggingConfig _loggingConfig;

        public const string ClearConsoleAnsi = "\u001b[2J\u001b[H";

        public RmfConsoleAppearance(IThemeManager themeManager, LoggingConfig loggingConfig)
        {
            this._themeManager = themeManager;
            this._loggingConfig = loggingConfig;
        }

        public void DrawLogo(ILogger logger)
        {
            ThemeColor logoColor = this._themeManager.GetColor("Logo");
            logger.Log(
                LogLevel.None,
                "{StartColor}{Logo}{EndColor}",
                logoColor,
                RmfConstants.ServerLogo,
                ThemeColor.AnsiReset
            );
        }

        public void LogSeparator(ILogger logger)
        {
            ThemeColor separatorColor = this._themeManager.GetColor("Separator");
            logger.Log(
                LogLevel.None,
                "{StartColor}{Separator}{EndColor}",
                separatorColor,
                string.Join("", Enumerable.Repeat(this._loggingConfig.LoggingSeparatorChar, this._loggingConfig.LoggingSeparatorLength)),
                ThemeColor.AnsiReset
            );
        }

        public void LogInitialization(ILogger logger, string category, int loaded, int total)
        {
            string colorKey;
            if (loaded <= 0)
            {
                colorKey = "FailedToLoadCounter";
            }
            else if (loaded < total)
            {
                colorKey = "PartiallyLoadedCounter";
            }
            else
            {
                colorKey = "SuccessfullyLoadedCounter";
            }

            int indentLevel = logger is RmfLogger
                ? RmfLogger.MaxCategoryNameLength + RmfLogger.FixedHeaderLength
                : 0;
            string indent = new(' ', indentLevel);

            ThemeColor initColor = this._themeManager.GetColor(colorKey);
            logger.Log(
                LogLevel.None,
                RmfConstants.InitComponentLogTemplate,
                indent,
                category,
                initColor,
                loaded,
                total,
                ThemeColor.AnsiReset
            );
        }

        public void ClearConsole(ILogger logger)
        {
            logger.Log(
                LogLevel.None,
                "{ClearConsoleAnsi}",
                ClearConsoleAnsi
            );
        }
    }
}
