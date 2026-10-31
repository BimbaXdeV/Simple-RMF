using RMF.Core.Loaders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Server.Configurations
{
    internal class ControllerConfig : RmfConfig
    {
        public bool EnableCollectingSessionStats = false;
        public bool EnableWelcomeHandshake = false;
        public bool EnableBuildComparison = false;
        public bool EnableCollectingClientInfo = false;
        public bool EnableClientHeartbeat = false;
        public int ClientHeartbeatIntervalSecs = int.MaxValue - 1;
        public bool EnableRelativeParting = false;
        public int PartingTimeoutSecs = 1;

        public override bool ValidateFields(out string? exceptionMessage)
        {
            exceptionMessage = null;
            if (ClientHeartbeatIntervalSecs < 0)
            {
                exceptionMessage = "ClientHeartbeatIntervalSecs must be greater than or equal to 0";
                return false;
            }
            if (PartingTimeoutSecs < 0)
            {
                exceptionMessage = "PartingTimeoutSecs must be greater than or equal to 0";
                return false;
            }
            return true;
        }

        public override void LoadFields(IReadOnlyDictionary<string, string> configDict)
        {
            ApplyValue<bool>(configDict, nameof(EnableCollectingSessionStats), v => EnableCollectingSessionStats = v);
            ApplyValue<bool>(configDict, nameof(EnableWelcomeHandshake), v => EnableWelcomeHandshake = v);
            ApplyValue<bool>(configDict, nameof(EnableBuildComparison), v => EnableBuildComparison = v);
            ApplyValue<bool>(configDict, nameof(EnableCollectingClientInfo), v => EnableCollectingClientInfo = v);
            ApplyValue<bool>(configDict, nameof(EnableClientHeartbeat), v => EnableClientHeartbeat = v);
            ApplyValue<int>(configDict, nameof(ClientHeartbeatIntervalSecs), v => ClientHeartbeatIntervalSecs = v);
            ApplyValue<bool>(configDict, nameof(EnableRelativeParting), v => EnableRelativeParting = v);
            ApplyValue<int>(configDict, nameof(PartingTimeoutSecs), v => PartingTimeoutSecs = v);
        }
    }
}
