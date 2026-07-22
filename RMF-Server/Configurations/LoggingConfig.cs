using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.Configurations
{
    internal class LoggingConfig
    {
        public static bool EnableLogSaving;
        public static bool EnableMultipleBackup;
        public static int MaxLogFileCapacityMB;
        public static int LoggingHistoryLength;
        public static int LoggingHandlerDelayMsecs;
        public static int InputListenerDelayMsecs;
    }
}
