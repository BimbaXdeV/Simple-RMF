using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.Configurations
{
    internal class ControllerConfig
    {
        public bool EnableCollectingSessionStats;
        public bool EnableWelcomeHandshake;
        public bool EnableBuildComparison;
        public bool EnableCollectingClientInfo;
        public bool EnableClientHeartbeat;
        public int ClientHeartbeatIntervalSecs;
        public bool EnableRelativeParting;
        public int PartingTimeoutSecs;
    }
}
