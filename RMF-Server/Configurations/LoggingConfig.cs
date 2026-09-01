using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.Configurations
{
    internal class LoggingConfig
    {
        public bool EnableLogSaving = false;
        public int MaxLogSavingDurationSecs = int.MaxValue;
        public string LoggingFilePath = string.Empty;
        public bool EnableMultipleBackup = false;
        public int MaxLogFileCapacityKB = int.MaxValue;
        public int LoggingHistoryLength = 10;
        public char LoggingSeparatorChar = char.MinValue;
        public int LoggingSeparatorLength = 0;
        public int LoggingHandlerDelayMsecs = 100;
    }
}
