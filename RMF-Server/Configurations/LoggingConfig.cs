using RMF.Core.Loaders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.Configurations
{
    internal class LoggingConfig : RmfConfig
    {
        private const int _minLogSavingLimitSecs = 2;
        private const int _minHandlerDelayMsecs = 10;

        public bool EnableWordWrapIndent = false;
        public bool EnableLogSaving = false;
        public int MaxLogSavingDurationSecs = int.MaxValue;
        public string LoggingFilePath = string.Empty;
        public bool EnableMultipleBackup = false;
        public int MaxLogFileCapacityKB = int.MaxValue;
        public int LoggingHistoryLength = 10;
        public char LoggingSeparatorChar = char.MinValue;
        public int LoggingSeparatorLength = 0;
        public int LoggingHandlerDelayMsecs = 100;

        public override bool ValidateFields(out string? exceptionMessage)
        {
            exceptionMessage = null;
            if (MaxLogSavingDurationSecs < _minLogSavingLimitSecs)
            {
                exceptionMessage = $"MaxLogSavingDurationSecs must be greater than or equal to {_minLogSavingLimitSecs}";
                return false;
            }
            if (MaxLogFileCapacityKB <= 0)
            {
                exceptionMessage = "MaxLogFileCapacityKB must be greater than 0";
                return false;
            }
            if (LoggingHistoryLength <= 0)
            {
                exceptionMessage = "LoggingHistoryLength must be greater than 0";
                return false;
            }
            if (LoggingSeparatorLength < 0)
            {
                exceptionMessage = "LoggingSeparatorLength must be greater than or equal to 0";
                return false;
            }
            if (LoggingHandlerDelayMsecs < _minHandlerDelayMsecs)
            {
                exceptionMessage = $"LoggingHandlerDelayMsecs must be greater than or equal to {_minHandlerDelayMsecs}";
                return false;
            }
            return true;
        }

        public override void LoadFields(IReadOnlyDictionary<string, string> configDict)
        {
            ApplyValue<bool>(configDict, nameof(EnableWordWrapIndent), v => EnableWordWrapIndent = v);
            ApplyValue<bool>(configDict, nameof(EnableLogSaving), v => EnableLogSaving = v);
            ApplyValue<int>(configDict, nameof(MaxLogSavingDurationSecs), v => MaxLogSavingDurationSecs = v);
            ApplyValue<string>(configDict, nameof(LoggingFilePath), v => LoggingFilePath = v);
            ApplyValue<bool>(configDict, nameof(EnableMultipleBackup), v => EnableMultipleBackup = v);
            ApplyValue<int>(configDict, nameof(MaxLogFileCapacityKB), v => MaxLogFileCapacityKB = v);
            ApplyValue<int>(configDict, nameof(LoggingHistoryLength), v => LoggingHistoryLength = v);
            ApplyValue<char>(configDict, nameof(LoggingSeparatorChar), v => LoggingSeparatorChar = v);
            ApplyValue<int>(configDict, nameof(LoggingSeparatorLength), v => LoggingSeparatorLength = v);
            ApplyValue<int>(configDict, nameof(LoggingHandlerDelayMsecs), v => LoggingHandlerDelayMsecs = v);
        }
    }
}
