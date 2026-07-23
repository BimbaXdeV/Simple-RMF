using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.Configurations
{
    internal class LoggingConfig
    {
        public bool EnableLogSaving;
        public bool EnableMultipleBackup;
        public int MaxLogFileCapacityMB;
        public int LoggingHistoryLength;
        public int LoggingHandlerDelayMsecs;
    }
}
