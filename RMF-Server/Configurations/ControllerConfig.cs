using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.Configurations
{
    internal class ControllerConfig
    {
        public bool EnableCollectingSessionStats = false;
        public bool EnableWelcomeHandshake = false;
        public bool EnableBuildComparison = false;
        public bool EnableCollectingClientInfo = false;
        public bool EnableClientHeartbeat = false;
        public int ClientHeartbeatIntervalSecs = int.MaxValue - 1;
        public bool EnableRelativeParting = false;
        public int PartingTimeoutSecs = 1;
    }
}
