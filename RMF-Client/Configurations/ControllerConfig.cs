using RMF.Core.Loaders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Client.Configurations
{
    internal class ControllerConfig : RmfConfig
    {
        public bool EnableCollectingSessionStats = false;

        public override bool ValidateFields(out string? exceptionMessage)
        {
            exceptionMessage = null;
            return true;
        }

        public override void LoadFields(IReadOnlyDictionary<string, string> configDict)
        {
            ApplyValue<bool>(configDict, nameof(EnableCollectingSessionStats), v => EnableCollectingSessionStats = v);
        }
    }
}
